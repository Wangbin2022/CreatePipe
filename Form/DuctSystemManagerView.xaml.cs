using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// DuctSystemManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class DuctSystemManagerView : Window
    {
        public UIApplication UIApplication { get; set; }
        public DuctSystemManagerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new DuctSystemManagerViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class DuctSystemManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<DuctSystemEntity>
    {
        public Document Doc { get; set; }
        public UIDocument UIDocument { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        private List<DuctSystemEntity> _cachedDuctSystems = new List<DuctSystemEntity>();
        public ObservableCollection<DuctSystemEntity> Collection { get; set; } = new ObservableCollection<DuctSystemEntity>();
        public DuctSystemManagerViewModel(UIApplication uIApplication)
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
            NewTransaction.Execute(Doc, "初始化风管系统设置", () =>
            {
                var elements = new FilteredElementCollector(Doc).OfClass(typeof(MechanicalSystemType));
                List<MechanicalSystemType> ductSystemTypes = elements.OfType<MechanicalSystemType>().ToList();
                var defaultMaterialId = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Materials).FirstOrDefault().Id;
                foreach (var pst in ductSystemTypes)
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
            var allSystemTypes = new FilteredElementCollector(Doc).OfClass(typeof(MechanicalSystemType))
                .OfType<MechanicalSystemType>().Select(pst => new DuctSystemEntity(pst, ExternalHandler)).ToList();
            _cachedDuctSystems = allSystemTypes;
            QueryElement(null);
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            List<DuctSystemEntity> filteredList = new List<DuctSystemEntity>();
            if (string.IsNullOrWhiteSpace(text))
            {
                filteredList = _cachedDuctSystems;
            }
            else
            {
                filteredList = _cachedDuctSystems.Where(e =>
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
        public ICommand DuctSystemAddCommand => new RelayCommand<object>(param =>
        {
            if (param is string tabIndex)
                DuctSystemAdd(tabIndex);
            else if (param is DuctSystemEntity entity)
                DuctSystemAdd(entity);
            else
                TaskDialog.Show("tt", "Unsupported parameter type");
        });
        private void DuctSystemAdd(object parameter)
        {
            // 根据参数类型创建不同的子窗口
            try
            {
                ExternalHandler.Run(app =>
                 {
                     DuctSystemManagerSubView subView;
                     if (parameter is DuctSystemEntity entity)
                     {
                         subView = new DuctSystemManagerSubView(entity, UIDocument);
                         subView.SelectTab("EditTab"); // 修改材质默认打开编辑页
                     }
                     else if (parameter is string tabIndex)
                     {
                         subView = new DuctSystemManagerSubView(tabIndex, UIDocument);
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
                     if (result == true && subView.DataContext is DuctSystemManagerSubViewModel vm)
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
        public ICommand SelectSystemCommand => new RelayCommand<IEnumerable<object>>(SelectSystems);
        private void SelectSystems(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                TaskDialog.Show("tt", "未选择任何系统，请重试");
                return;
            }
            List<DuctSystemEntity> selectedItems = selectedElements.Cast<DuctSystemEntity>().ToList();
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
        //子窗口逻辑
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(DuctSystemEntity systemType)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM), systemType.SystemName, false));
            IList<Element> allpipes = new FilteredElementCollector(systemType.Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_DuctCurves)
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
        public bool IsLastSystemEntity(DuctSystemEntity systemType)
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
            List<DuctSystemEntity> selectedItems = selectedElements.Cast<DuctSystemEntity>().ToList();
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
                                Doc.Delete(item.ductSystemType.Id);
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
        //单选删除
        public ICommand DeleteElementCommand => new RelayCommand<DuctSystemEntity>(DeleteElement);
        public void DeleteElement(DuctSystemEntity entity)
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
