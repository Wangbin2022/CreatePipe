using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// PipeSystemManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSystemManagerView : Window
    {
        Document Doc;
        public PipeSystemManagerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new PipeSystemViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class PipeSystemViewModel : ObserverableObject, IQueryViewModelWithDelete<PipeSystemEntity>
    {
        public Document Doc { get; set; }
        public UIDocument UIDocument { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();

        private List<PipeSystemEntity> _cachedPipeSystems = new List<PipeSystemEntity>();
        public ObservableCollection<PipeSystemEntity> Collection { get; set; } = new ObservableCollection<PipeSystemEntity>();
        public PipeSystemViewModel(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            UIDocument = uIApplication.ActiveUIDocument as UIDocument;
            InitFunc();
        }
        public List<Material> AllMaterials { get; set; }
        // 用于 ComboBox 显示的字符串列表
        public List<string> MaterialNames { get; set; }
        // 用于内部查询的字典（提升查找效率）
        private Dictionary<string, Material> _nameToMaterialMap;
        public void InitFunc()
        {
            AllMaterials = new FilteredElementCollector(Doc).OfClass(typeof(Material)).Cast<Material>().ToList();
            // 1. 生成字符串列表
            MaterialNames = AllMaterials.Select(m => m.Name).OrderBy(n => n).ToList();
            // 2. 生成映射字典，方便后续根据名称找到 ElementId
            _nameToMaterialMap = AllMaterials.ToDictionary(m => m.Name, m => m);
            //要预先给系统赋颜色
            NewTransaction.Execute(Doc, "初始化管道系统设置", () =>
            {
                var elements = new FilteredElementCollector(Doc).OfClass(typeof(PipingSystemType));
                List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
                var defaultMaterialId = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Materials).FirstOrDefault().Id;
                foreach (var pst in pipingSystemTypes)
                {
                    if (!pst.LineColor.IsValid)
                    {
                        pst.LineColor = new Autodesk.Revit.DB.Color(127, 127, 127);
                    }
                    if (pst.MaterialId.IntegerValue == -1)
                    {
                        pst.MaterialId = defaultMaterialId;
                    }
                }
            });
            var allSystemTypes = new FilteredElementCollector(Doc).OfClass(typeof(PipingSystemType))
                .OfType<PipingSystemType>().Select(pst => new PipeSystemEntity(pst, ExternalHandler)).ToList();
            _cachedPipeSystems = allSystemTypes;
            QueryElement(null);
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            List<PipeSystemEntity> filteredList = new List<PipeSystemEntity>();
            if (string.IsNullOrWhiteSpace(text))
            {
                filteredList = _cachedPipeSystems;
            }
            else
            {
                filteredList = _cachedPipeSystems.Where(e =>
                    e.SystemName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.Abbreviation != null && e.Abbreviation.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            }
            // 简单高效地更新UI集合
            Collection.Clear();
            foreach (var item in filteredList)
            {
                Collection.Add(item);
            }
        }
        public ICommand SelectSystemCommand => new RelayCommand<IEnumerable<object>>(SelectSystems);
        private void SelectSystems(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                TaskDialog.Show("tt", "未选择任何系统，请重试");
                return;
            }
            List<PipeSystemEntity> selectedItems = selectedElements.Cast<PipeSystemEntity>().ToList();
            if (selectedElements == null) return;
            List<ElementId> ids = new List<ElementId>();
            foreach (var system in selectedItems)
            {
                foreach (var item in system.selectedElements)
                {
                    if (item is ElementId id)
                    {
                        ids.Add(id);
                    }
                }
            }
            Selection select = UIDocument.Selection;
            select.SetElementIds(ids);
        }
        public ICommand PipingAddCommand => new RelayCommand<object>(param =>
        {
            if (param is string tabIndex)
                PipingAdd(tabIndex);
            else if (param is PipeSystemEntity entity)
                PipingAdd(entity);
            else
                TaskDialog.Show("tt", "Unsupported parameter type");
        });
        private void PipingAdd(object parameter)
        {
            // 根据参数类型创建不同的子窗口
            try
            {
                ExternalHandler.Run(app =>
                {
                    PipeSystemManagerSubView subView;
                    if (parameter is PipeSystemEntity entity)
                    {
                        subView = new PipeSystemManagerSubView(entity, UIDocument, ExternalHandler);
                        subView.SelectTab("EditTab"); // 修改材质默认打开编辑页
                    }
                    else if (parameter is string tabIndex)
                    {
                        subView = new PipeSystemManagerSubView(tabIndex, UIDocument, ExternalHandler);
                        if (!string.IsNullOrEmpty(tabIndex))
                        {
                            subView.SelectTab(tabIndex);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("参数必须是MaterialEntityModel或string类型");
                    }
                    // 公共处理逻辑
                    bool? result = subView.ShowDialog();
                    if (result == true && subView.DataContext is PipeSystemManagerSubViewModel vm)
                    {
                        InitFunc();
                    }
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message.ToString());
            }
        }
        //检查系统是否为空
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(PipeSystemEntity systemType)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), systemType.SystemName, false));
            IList<Element> allpipes = new FilteredElementCollector(systemType.Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            IList<Element> allfamily = new FilteredElementCollector(systemType.Document)
                 .WhereElementIsNotElementType()
                 .OfClass(typeof(FamilyInstance))
                 .WherePasses(filter)
                 .ToElements();

            int countEntity = allpipes.Count() + allfamily.Count();
            if (countEntity == 0) { return true; }
            else return false;
        }
        //检查系统唯一性
        private bool isLastSystemEntity { get; set; }
        public bool IsLastSystemEntity(PipeSystemEntity systemType)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfClass(typeof(MEPSystemType));
            List<MEPSystemType> systemTypes = elems.OfType<MEPSystemType>().ToList();
            int systemCount = 0;
            foreach (MEPSystemType item in systemTypes)
            {
                if (systemType.MEPSystemClassOrigin == item.SystemClassification)
                {
                    systemCount++;
                }
            }
            if (systemCount > 1)
            {
                return false;
            }
            return true;
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        //多选删除
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            List<PipeSystemEntity> selectedItems = selectedElements.Cast<PipeSystemEntity>().ToList();
            if (selectedElements == null) return;
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Doc, "批量删除系统", (service) =>
                {
                    service.UpdateMax(selectedElements.Count());
                    int index = 0;
                    int successId = 0;
                    foreach (var item in selectedItems)
                    {
                        isLastSystemEntity = IsLastSystemEntity(item);
                        isEmptySystem = IsEmptySystem(item);
                        if (!isLastSystemEntity)
                        {
                            if (isEmptySystem)
                            {
                                Doc.Delete(item.pipingSystemType.Id);
                                this.Collection.Remove(item);
                                successId++;
                            }
                            else
                            {
                                TaskDialog td = new TaskDialog("tt");
                                td.MainInstruction = "选定的系统类型正在使用，因此不能删除";
                                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                                td.Show();
                            }
                        }
                        else
                        {
                            TaskDialog td = new TaskDialog("tt");
                            td.MainInstruction = "不可删除该类型最后一个系统实例";
                            td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                            td.Show();
                        }
                        service.Update(++index, item.SystemName);
                    }
                    TaskDialog.Show("tt", $"已删除{successId}个系统");
                });
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<PipeSystemEntity>(DeleteElement);
        public void DeleteElement(PipeSystemEntity entity)
        {
            if (entity.selectedElements == null) return;
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "删除实例", () =>
                {
                    Doc.Delete(entity.selectedElements);
                });

                InitFunc();
            });
        }
    }
}
