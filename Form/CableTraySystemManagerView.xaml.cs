using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace CreatePipe.Form
{
    /// <summary>
    /// CableTraySystemManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class CableTraySystemManagerView : Window
    {
        public CableTraySystemManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new CableTraySystemManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class CableTraySystemManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<CableTrayEntity>
    {
        public Document Doc { get; set; }
        public UIDocument uIDocument { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        // ✅ 1. 增加缓存层，提升搜索与加载速度
        private List<CableTrayEntity> _cachedCableSystems = new List<CableTrayEntity>();
        public ObservableCollection<CableTrayEntity> Collection { get; set; } = new ObservableCollection<CableTrayEntity>();
        public List<string> CableSystemNames { get; set; } = new List<string>();
        public CableTraySystemManagerViewModel(UIApplication uiApp)
        {
            Doc = uiApp.ActiveUIDocument.Document;
            uIDocument = uiApp.ActiveUIDocument;
            InitFunc();
        }
        public void InitFunc()
        {
            var cableSystemTypes = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType))
                .Cast<CableTrayType>().Where(t => t.IsWithFitting).ToList();
            _cachedCableSystems = cableSystemTypes.Select(t => new CableTrayEntity(t, ExternalHandler)).ToList();
            QueryElement(null);
        }
        public ICommand SelectSystemCommand => new RelayCommand<IEnumerable<object>>(SelectSystems);
        private void SelectSystems(IEnumerable<object> selectedElements)
        {
            if (selectedElements == null) return;
            var selectedItems = selectedElements.Cast<CableTrayEntity>().ToList();
            List<ElementId> ids = new List<ElementId>();
            foreach (var system in selectedItems)
            {
                if (system.selectedElements != null)
                    ids.AddRange(system.selectedElements);
            }
            if (ids.Any()) uIDocument.Selection.SetElementIds(ids);
        }
        public ICommand AddSystemCommand => new BaseBindingCommand(AddSystem);
        private void AddSystem(object obj)
        {
            try
            {
                // 1. 【必须在 UI 线程执行】获取用户输入
                UniversalNewString subView = new UniversalNewString("请输入新桥架名称");
                if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
                {
                    return; // 用户取消或未输入
                }
                string newName = vm.NewName.Trim();
                // 验证名称合法性
                if (!RevitNameHelper.IsValidName(CableSystemNames, newName, out string errorMessage))
                {
                    TaskDialog.Show("验证失败", errorMessage);
                    return;
                }
                // 2. 【进入外部事件线程】执行 Revit 数据库操作
                ExternalHandler.Run(app =>
                {
                    CableTrayType newType = null;
                    string filterName = newName + "过滤器";
                    using (Transaction t = new Transaction(Doc, "新建桥架系统与过滤器"))
                    {
                        t.Start();
                        // A. 找一个带配件的“模板类型”用来复制
                        CableTrayType sourceTrayType = new FilteredElementCollector(Doc)
                            .OfClass(typeof(CableTrayType))
                            .Cast<CableTrayType>()
                            .FirstOrDefault(x => x.IsWithFitting);

                        if (sourceTrayType == null)
                        {
                            TaskDialog.Show("错误", "当前项目中没有带配件的桥架类型，无法复制。");
                            t.RollBack();
                            return;
                        }
                        // B. 复制产生新类型
                        newType = sourceTrayType.Duplicate(newName) as CableTrayType;
                        // C. 【安全修复】直接通过内置枚举获取类别 ID（绝对不会报空指针）
                        List<ElementId> categoryIds = new List<ElementId>
                        {
                            new ElementId(BuiltInCategory.OST_CableTray),
                            new ElementId(BuiltInCategory.OST_CableTrayFitting)
                        };
                        // D. 创建过滤规则
                        ElementId paramId = new ElementId(BuiltInParameter.RBS_CTC_SERVICE_TYPE);
                        // 注意：如果你的 Revit 是 2023+，不要传第三个 false；如果是 2022 及以下，保持传 false
                        FilterRule fRule = ParameterFilterRuleFactory.CreateContainsRule(paramId, newName, false);
                        ElementParameterFilter pFilter = new ElementParameterFilter(fRule);
                        // E. 检查过滤器名称是否已存在，不存在才创建
                        ParameterFilterElement existingFilter = new FilteredElementCollector(Doc)
                            .OfClass(typeof(ParameterFilterElement))
                            .Cast<ParameterFilterElement>()
                            .FirstOrDefault(f => f.Name == filterName);
                        if (existingFilter == null)
                        {
                            ParameterFilterElement.Create(Doc, filterName, categoryIds, pFilter);
                        }
                        t.Commit();
                    }
                    // 3. 【回到 UI 线程】更新界面列表
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (newType != null)
                        {
                            // 实例化并初始化新对象
                            CableTrayEntity newEntity = new CableTrayEntity(newType, ExternalHandler)
                            {
                                FilterName = filterName // 自动绑定刚才生成的过滤器
                            };
                            // 更新 ViewModel 中的集合（不使用费时的 Clear 重新读取）
                            if (_cachedCableSystems != null)
                            {
                                _cachedCableSystems.Add(newEntity);
                            }
                            Collection.Add(newEntity);
                            CableSystemNames.Add(newName);
                            OnPropertyChanged(nameof(CableTrayCount));
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", "新建桥架出现异常: " + ex.Message);
            }
            //try
            //{
            //    ExternalHandler.Run(app =>
            //    {
            //        UniversalNewString subView = new UniversalNewString("请输入新桥架名称");
            //        if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            //        {
            //            TaskDialog.Show("tt", "输入属性遇到错误，请重试"); return;
            //        }
            //        string newName = vm.NewName;
            //        if (!CheckName(CableSystemNames, newName)) return;
            //        ////复制建立新CableTrayType及过滤器
            //        NewTransaction.Execute(Doc, "新建系统", () =>
            //         {

            //         });
            //    });
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //}
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            List<CableTrayEntity> selectedItems = selectedElements.Cast<CableTrayEntity>().ToList();
            if (selectedElements == null) return;
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Doc, "删除多个系统", (service) =>
                {
                    service.UpdateMax(selectedItems.Count);
                    int index = 0;
                    foreach (var cableTrayEntity in selectedItems)
                    {
                        if (IsLastSystemEntity())
                        {
                            TaskDialog.Show("警告", "不可删除该类型最后一个系统实例");
                            return;
                        }
                        if (!IsEmptySystem(cableTrayEntity))
                        {
                            TaskDialog.Show("警告", $"选定的系统类型[{cableTrayEntity.SystemName}]正在使用，因此不能删除");
                            return;
                        }
                        Doc.Delete(cableTrayEntity.cableTrayType.Id);
                        service.Update(++index, cableTrayEntity.SystemName);
                    }
                    InitFunc();
                });
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<CableTrayEntity>(DeleteElement);
        public void DeleteElement(CableTrayEntity cableTrayEntity)
        {
            if (cableTrayEntity.selectedElements == null) return;
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Doc, "删除实例", (service) =>
                {
                    service.UpdateMax(cableTrayEntity.selectedElements.Count);
                    int index = 0;
                    foreach (var item in cableTrayEntity.selectedElements)
                    {
                        Doc.Delete(item);
                        service.Update(++index, item.IntegerValue.ToString());
                    }
                });
                InitFunc();
            });
        }
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(CableTrayEntity cableTrayEntity)
        {
            ElementId typeId = cableTrayEntity.cableTrayType.Id;
            // 如果能找到任何使用该 TypeId 的实例，说明非空
            bool isUsed = new FilteredElementCollector(Doc).OfClass(typeof(CableTray))
                .WhereElementIsNotElementType().Any(e => e.GetTypeId() == typeId);
            return !isUsed;
        }
        //检查系统唯一性
        private bool isLastSystemEntity { get; set; }
        public bool IsLastSystemEntity()
        {
            int withFittingCount = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType))
                .Cast<CableTrayType>().Count(t => t.IsWithFitting);
            return withFittingCount <= 1;
        }
        public string CableTrayCount => Collection.Count.ToString();
        public ICommand SetLineColorCommand => new RelayCommand<CableTrayEntity>(SetLineColor);
        private void SetLineColor(CableTrayEntity entity)
        {
            ExternalHandler.Run(app =>
            {
                var initialRevitColor = entity.LineColor;
                var initialMediaColor = System.Windows.Media.Color.FromRgb(initialRevitColor.Red, initialRevitColor.Green, initialRevitColor.Blue);
                var dialog = new ColorPickerDialog(initialMediaColor);
                if (dialog.ShowDialog() == true)
                {
                    var newRevitColor = new Autodesk.Revit.DB.Color(dialog.SelectedColor.R, dialog.SelectedColor.G, dialog.SelectedColor.B);
                    entity.LineColor = newRevitColor;
                }
            });
        }
        ParameterFilterElement viewFilter { get; set; }
        public ICommand ApplySetupCommand => new RelayCommand<CableTrayEntity>(ApplySetup);
        private void ApplySetup(CableTrayEntity entity)
        {
            if (string.IsNullOrEmpty(entity.FilterName))
            {
                TaskDialog.Show("提示", "未选择对应的过滤器，请重设置！");
                return;
            }

            // ✅ 使用 ExternalHandler 安全开启事务
            ExternalHandler.Run(app =>
            {
                ParameterFilterElement viewFilter = new FilteredElementCollector(Doc)
                    .OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>()
                    .FirstOrDefault(f => f.Name == entity.FilterName);
                if (viewFilter == null) return;
                using (Transaction t = new Transaction(Doc, "所有视图附加过滤器"))
                {
                    t.Start();
                    List<View> allViews = new FilteredElementCollector(Doc).OfClass(typeof(View)).Cast<View>()
                    .Where(v => !v.IsTemplate &&
                            v.ViewType != ViewType.Schedule &&
                            v.ViewType != ViewType.DrawingSheet &&
                            v.ViewType != ViewType.Legend &&
                            v.ViewType != ViewType.AreaPlan &&
                            Doc.GetElement(v.GetTypeId()) is ElementType).ToList();
                    OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                    ogs.SetSurfaceTransparency(entity.TransparencyNum);
                    ogs.SetProjectionLineWeight(entity.SelectedLineWeight);
                    // ✅ 修复：直接使用选择的线型，不要去遍历收集整个文档的线型
                    if (entity.LinePatternElem != null)
                    {
                        ogs.SetProjectionLinePatternId(entity.LinePatternElem.Id);
                    }
                    // 取实体填充图案
                    ElementId solidFillId = new FilteredElementCollector(Doc).OfClass(typeof(FillPatternElement))
                        .Cast<FillPatternElement>().FirstOrDefault(x => x.GetFillPattern().IsSolidFill)?.Id;
                    if (solidFillId != null) ogs.SetSurfaceForegroundPatternId(solidFillId);
                    // 颜色设置
                    Autodesk.Revit.DB.Color color = entity.LineColor ?? new Autodesk.Revit.DB.Color(127, 127, 127);
                    ogs.SetProjectionLineColor(color);
                    ogs.SetSurfaceForegroundPatternColor(color);
                    foreach (View view in allViews)
                    {
                        if (!view.GetFilters().Contains(viewFilter.Id))
                        {
                            view.AddFilter(viewFilter.Id);
                        }
                        view.SetFilterOverrides(viewFilter.Id, ogs);
                    }
                    t.Commit();
                }
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            List<CableTrayEntity> filteredList = new List<CableTrayEntity>();
            if (string.IsNullOrWhiteSpace(text))
            {
                filteredList = _cachedCableSystems;
            }
            else
            {
                filteredList = _cachedCableSystems
                    .Where(e => e.SystemName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               (e.Abbreviation != null && e.Abbreviation.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }
            Collection.Clear();
            CableSystemNames.Clear();
            foreach (var item in filteredList)
            {
                Collection.Add(item);
                CableSystemNames.Add(item.SystemName);
            }
            OnPropertyChanged(nameof(CableTrayCount));
            //Collection.Clear();
            //FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
            //List<CableTrayType> cableSystemTypes = elements.OfType<CableTrayType>().ToList();
            //List<CableTrayEntity> cableSystems = cableSystemTypes
            //    .Select(cableSystemType => new CableTrayEntity(cableSystemType, ExternalHandler))
            //    .Where(e => e.IsWithFitting == true)
            //    .Where(e => string.IsNullOrEmpty(text)
            //    || e.SystemName.Contains(text)
            //    || e.Abbreviation.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
            //    .ToList();
            //foreach (var item in cableSystems)
            //{
            //    Collection.Add(item);
            //    CableSystemNames.Add(item.SystemName);
            //}
        }
    }
}
