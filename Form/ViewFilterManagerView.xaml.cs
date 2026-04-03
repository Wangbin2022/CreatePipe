using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
    /// ViewFilterManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFilterManagerView : Window
    {
        UIApplication application;
        public ViewFilterManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ViewFilterManagerViewModel(uiApp);
            application = uiApp;
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.Filters);
            application.PostCommand(commandId);
            this.Close();
        }
    }
    public class ViewFilterManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<ViewFilterEntity>
    {
        public Document Document { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        private Dictionary<ElementId, List<View>> _filterUsageMap = new Dictionary<ElementId, List<View>>();
        List<View> _rawViews = new List<View>();
        private ViewFilterEntity _selectedFilter;
        public ViewFilterEntity SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                if (EnableCategoryList && value != null) // Ensure only one is selected and it's not null
                {
                    UpdateCategoryItems(value);
                }
                else if (value == null) // If no view is selected, clear its CategoryItems
                {
                    // Optionally clear CategoryItems of the previous selectedView if needed
                    // For now, let's assume it's fine to leave it as is if it's not currently bound
                }
            }
        }
        // 计算属性，直接判断 RowCount
        public bool EnableCategoryList => RowCount == 1;
        public int ViewFilterCount => Collection.Count;
        public ViewFilterManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            InitFunc();
            /// 收集所有符合条件的视图，并填充 _internalViewDisplayItems 和 AllViewItems
            /// 仅在初始化时或需要刷新时调用一次
            _internalViewDisplayItems.Clear(); // 清空内部列表
            AllViewItems.Clear(); // 清空绑定列表
            var allRevitViews = new FilteredElementCollector(Document).OfClass(typeof(View))
                .Cast<View>().Where(v => v != null && !v.IsTemplate) // 排除模板视图
                .Where(v =>
                    v.ViewType != ViewType.Schedule &&
                    v.ViewType != ViewType.DrawingSheet &&
                    v.ViewType != ViewType.Legend &&
                    v.ViewType != ViewType.AreaPlan) // 排除指定视图类型
                .OrderBy(v => v.Name).ToList();
            foreach (var view in allRevitViews)
            {
                ElementType objType = Document.GetElement(view.GetTypeId()) as ElementType;
                if (objType != null)
                {
                    var displayItem = new ViewEntity(view, null, true, 0);
                    _internalViewDisplayItems.Add(displayItem);
                    AllViewItems.Add(displayItem.DisplayName);
                }
            }
        }
        ////// 从 Helper 获取 SelectedItems (用于 MultiSelectListBox)
        ////private List<string> _selectedListBoxItems = new List<string>();
        ////public List<string> SelectedListBoxItems // 重命名以避免与DataGrid的SelectedItems混淆
        ////{
        ////    get => _selectedListBoxItems;
        ////    set => SetProperty(ref _selectedListBoxItems, value);
        ////}
        // 从 Helper 获取 RowCount
        private int _rowCount;
        public int RowCount
        {
            get => _rowCount;
            set
            {
                _rowCount = value;
                OnPropertyChanged(nameof(RowCount));
                OnPropertyChanged(nameof(EnableCategoryList));
            }
        }
        // 加载 MultiSelectListBox 的 ItemsSource 待修补
        private void UpdateCategoryItems(ViewFilterEntity entity)
        {
            ////// 性能优化：如果已经加载过，不再重复查找 (可选)
            ////if (entity.CategoryItems != null && entity.CategoryItems.Count > 0) return;
            ////var viewType = entity.Viewe.ViewType;
            ////var names = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>()
            ////    .Where(v => !v.IsTemplate && v.ViewType == viewType)
            ////    .Select(v => v.Name).OrderBy(n => n).ToList();
            ////entity.CategoryItems = names;
        }
        public void InitFunc()
        {
            /// <summary>
            /// 性能优化的核心：遍历所有视图一次，构建一个从过滤器ID到视图列表的映射。
            /// </summary>
            _filterUsageMap.Clear();
            var viewCollector = new FilteredElementCollector(Document).OfClass(typeof(View)).WhereElementIsNotElementType()
                .Cast<View>().Where(v => !v.IsTemplate && v.AreGraphicsOverridesAllowed());
            foreach (View view in viewCollector)
            {
                foreach (var filterId in view.GetFilters())
                {
                    if (!_filterUsageMap.ContainsKey(filterId))
                    {
                        _filterUsageMap[filterId] = new List<View>();
                    }
                    _filterUsageMap[filterId].Add(view);
                }
            }
            QueryElement(null);
        }
        public ICommand ApplyToViewCommand => new RelayCommand<List<string>>(ApplyToView);
        private void ApplyToView(List<string> selectedDisplayNames)
        {
            if (SelectedFilter == null)
            {
                TaskDialog.Show("提示", "请先选择一个过滤器。");
                return;
            }
            if (selectedDisplayNames == null || !selectedDisplayNames.Any())
            {
                TaskDialog.Show("提示", "请先选择至少一个要应用的视图。");
                return;
            }
            // 根据传入的 DisplayName 字符串，从 _internalViewDisplayItems 中查找对应的 ViewDisplayItem 对象
            List<ViewEntity> viewsToApply = new List<ViewEntity>();
            foreach (string displayName in selectedDisplayNames)
            {
                var viewItem = _internalViewDisplayItems.FirstOrDefault(item => item.DisplayName == displayName);
                if (viewItem != null)
                {
                    viewsToApply.Add(viewItem);
                }
            }
            if (!viewsToApply.Any())
            {
                TaskDialog.Show("提示", "未能找到匹配的视图进行应用。");
                return;
            }
            // 使用一个局部变量来捕获 SelectedFilter，防止在异步操作过程中它被用户改变
            var filterToApply = SelectedFilter;
            // 外部Handler调用，并包含进度条
            ExternalHandler.Run(app =>
            {
                // 优化：提前查找 FillPatternElement，只查找一次
                ElementId solidFillPatternId = new FilteredElementCollector(Document)
                    .OfClass(typeof(FillPatternElement))
                    .Cast<FillPatternElement>()
                    .FirstOrDefault(x => x.GetFillPattern().IsSolidFill)?.Id;
                TransactionWithProgressBarHelper.Execute(Document, "应用过滤器到视图", (service) =>
                {
                    service.UpdateMax(viewsToApply.Count);
                    int appliedCount = 0;
                    int index = 0;
                    foreach (var viewItem in viewsToApply)
                    {
                        // 更新进度条显示信息
                        service.Update(++index, viewItem.DisplayName);
                        if (Document.GetElement(viewItem.Id) is View view)
                        {
                            // 1. 添加过滤器
                            // 如果过滤器已存在，AddFilter 不会报错，但我们可能希望确保覆盖设置
                            // 保持只添加逻辑，不主动移除
                            if (!view.GetFilters().Contains(filterToApply.Id)) // 只有不存在才添加
                            {
                                view.AddFilter(filterToApply.Id);
                            }
                            // 2. 应用覆盖设置
                            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                            ogs.SetSurfaceTransparency(filterToApply.TransparencyNum);
                            ogs.SetProjectionLineWeight(1); // 固定的线宽设置
                            if (solidFillPatternId != null)
                            {
                                ogs.SetSurfaceForegroundPatternId(solidFillPatternId);
                            }
                            if (filterToApply.Color != null)
                            {
                                ogs.SetProjectionLineColor(filterToApply.Color);
                                ogs.SetSurfaceForegroundPatternColor(filterToApply.Color);
                            }
                            else
                            {
                                ogs.SetProjectionLineColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                                ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                            }
                            // SetFilterOverrides 会覆盖现有设置，所以即使过滤器已存在，这里也能更新其显示效果
                            view.SetFilterOverrides(filterToApply.Id, ogs);
                            appliedCount++;
                        }
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("操作成功", $"成功将过滤器应用到 {viewsToApply.Count} 个视图。");
                    });
                });
            });
        }
        // 内部存储 ViewDisplayItem 对象的列表，用于查找和管理
        private List<ViewEntity> _internalViewDisplayItems = new List<ViewEntity>();

        // 用于绑定到 MultiSelectListBox 的 ItemsSource (只能是 string)
        public List<string> AllViewItems { get; set; } = new List<string>();

        // 用于绑定到 MultiSelectListBox 的 SelectedItems (只能是 string)
        private List<string> _selectedViewItems = new List<string>();
        public List<string> SelectedListBoxItems
        {
            get => _selectedViewItems;
            set
            {
                _selectedViewItems = value;
                OnPropertyChanged();
            }
        }
        public ICommand SetColorCommand => new RelayCommand<ViewFilterEntity>(SetColor);
        private void SetColor(ViewFilterEntity entity)
        {
            ExternalHandler.Run(app =>
            {
                if (entity is null) return;
                Autodesk.Revit.DB.Color initialColor = entity.Color;
                var inColor = System.Windows.Media.Color.FromRgb(initialColor.Red, initialColor.Green, initialColor.Blue);
                var dialog = new ColorPickerDialog(inColor);
                if (dialog.ShowDialog() == true)
                {
                    entity.Color = dialog.SelectedColor.ConvertToRevitColor();
                }
            });
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> entities)
        {
            if (entities == null) return;
            if (!entities.Any()) // 如果没有选中任何项
            {
                TaskDialog.Show("提示", "未选中任何过滤器进行删除。");
                return;
            }
            // 1. 将接收到的 IEnumerable<object> 转换为我们需要的类型
            //    使用 AsEnumerable().OfType<ViewFilterEntity>() 更安全，避免非 ViewFilterEntity 类型的意外项
            var selectedItems = entities?.OfType<ViewFilterEntity>().ToList();
            // 6. 执行 Revit 事务操作 (包括进度条)
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Document, "批量删除视图过滤器", (service) =>
                {
                    service.UpdateMax(selectedItems.Count);
                    int index = 0;
                    foreach (var item in selectedItems)
                    {
                        service.Update(++index, item.ViewFilterName);
                        Document.Delete(item.Id);
                    }
                });
                // 7. Revit 事务完成后，更新 UI (在 UI 线程执行)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    QueryElement(null); // 重新查询并刷新整个 Collection
                    string resultMessage = $"已成功删除 {selectedItems.Count} 个过滤器。";
                    TaskDialog.Show("删除完成", resultMessage);
                });
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<ViewFilterEntity>(DeleteElement);
        public void DeleteElement(ViewFilterEntity entity)
        {
            // 3. 将 Revit API 操作委托给 ExternalHandler (Revit 主线程执行)
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(app.ActiveUIDocument.Document, "移除过滤器", (service) =>
                {
                    try
                    {
                        service.UpdateMax(entity.filterUsageCount);
                        int index = 0;
                        // 遍历 Entity 中记录的所有视图 ElementId
                        foreach (var viewId in entity.filterUsageMap.Keys)
                        {
                            // 获取视图元素
                            if (Document.GetElement(viewId) is View view)
                            {
                                // 移除过滤器
                                view.RemoveFilter(entity.Id);
                                service.Update(++index, view.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("错误", $"移除过滤器时发生错误:\n{ex.Message}");
                    }
                });
                // 7. Revit 事务完成后，更新 UI (在 UI 线程执行)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    InitFunc();
                });
            });


            //Document.NewTransaction(() =>
            //{
            //    for (int i = selectedElements.Count - 1; i >= 0; i--)
            //    {
            //        ViewFilterEntity delFilters = selectedElements[i] as ViewFilterEntity;
            //        Document.Delete(delFilters.Id);
            //        Collection.Remove(delFilters);
            //    }
            //}, "删除过滤器");
            //QueryElement(null);
        }
        //public bool CheckName(ICollection<string> names)
        //{
        //    bool result;
        //    //names = FilterModelNames;
        //    //////string newName = Keyword;
        //    //if (String.IsNullOrEmpty(newName))
        //    //{
        //    //    return result = false;
        //    //}
        //    ////// Check if filter name contains invalid characters
        //    ////// These character are different from Path.GetInvalidFileNameChars()
        //    //char[] invalidFileChars = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };
        //    //foreach (char invalidChr in invalidFileChars)
        //    //{
        //    //    if (newName.Contains(invalidChr))
        //    //    {
        //    //        return result = false;
        //    //    }
        //    //}
        //    //// Check if name is used
        //    //// check if name is already used by other filters
        //    //bool inUsed = names.Contains(newName, StringComparer.OrdinalIgnoreCase);
        //    //if (inUsed)
        //    //{
        //    //    return result = false;
        //    //}
        //    return result = true;
        //}
        public ICommand FindInViewCommand => new RelayCommand<ViewFilterEntity>(FindInView);
        private void FindInView(ViewFilterEntity entity)
        {
            if (entity == null || entity.filterUsageCount == 0 || entity.filterUsageMap == null)
                return;
            // 2. 格式转换：
            // 因为你的 UniversalDictionaryListView 看起来需要一个 Dictionary<string, string> (Key是Id的字符串)
            // 而我们实体里存的是 Dictionary<ElementId, string>，所以需要做一次简单的转换。
            var displayDict = entity.filterUsageMap.ToDictionary(
                kvp => kvp.Key.ToString(),  // Key: 将 ElementId 转换为字符串
                kvp => kvp.Value            // Value: 视图名称保持不变
            );
            // 3. 打开通用字典显示窗体
            var window = new UniversalDictionaryListView(
                displayDict, $"使用过滤器 [{entity.ViewFilterName}] 的视图" // 动态生成标题
            );
            // 阻塞式显示窗口
            window.ShowDialog();
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            // 每次查询都清空现有结果
            Collection.Clear();
            // 1. 根据名称从Revit文档中查找匹配的 ParameterFilterElement
            var matchingPfe = new FilteredElementCollector(Document).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>()
                .Where(pfe => string.IsNullOrEmpty(text) || pfe.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            // 2. 遍历每个匹配的过滤器，构建 ViewFilterEntity
            foreach (var pfe in matchingPfe)
            {
                int usageCount = 0;
                var usageViewsDict = new Dictionary<ElementId, string>();
                // 3. 利用预先构建的速查表快速获取使用情况
                if (_filterUsageMap.TryGetValue(pfe.Id, out List<View> viewsUsingThisFilter))
                {
                    usageCount = viewsUsingThisFilter.Count;
                    usageViewsDict = viewsUsingThisFilter.ToDictionary(v => v.Id, v => v.Name);
                }
                // 4. 获取其他元数据
                var categories = GetCategoryList(pfe);
                var ruleDescription = GetCombineRules(pfe);
                var ruleLogic = GetCombineType(pfe);
                // 5. 使用新的构造函数创建 Entity 实例
                var entity = new ViewFilterEntity(pfe, ExternalHandler, categories, ruleDescription, ruleLogic, usageViewsDict);
                // 6. 将创建的实例添加到UI集合中
                Collection.Add(entity);
            }
            OnPropertyChanged(nameof(ViewFilterCount));
        }
        private List<string> GetCategoryList(ParameterFilterElement pfe)
        {
            return pfe.GetCategories().Select(id => Category.GetCategory(Document, id)?.Name)
                      .Where(name => !string.IsNullOrEmpty(name)).ToList();
        }
        private string GetCombineType(ParameterFilterElement pfe)
        {
            var filter = pfe.GetElementFilter();
            if (filter is LogicalAndFilter) return "且";
            if (filter is LogicalOrFilter) return "或";
            return "单一规则";
        }
        private string GetCombineRules(ParameterFilterElement pfe)
        {
            var filter = pfe.GetElementFilter();
            if (filter == null) return "过滤器已损坏或为空";
            // 如果不是复合过滤器，而是单一规则的过滤器
            var rules = new List<FilterRule>();
            if (filter is ElementLogicalFilter logicalFilter)
            {
                rules.AddRange(logicalFilter.GetFilters().OfType<ElementParameterFilter>().SelectMany(epf => epf.GetRules()));
            }
            else if (filter is ElementParameterFilter singleParamFilter)
            {
                rules.AddRange(singleParamFilter.GetRules());
            }
            if (!rules.Any()) return "未设置规则";
            return string.Join(GetCombineType(pfe) == "单一规则" ? "" : $" {GetCombineType(pfe)} ", rules.Select(FormatRule));
        }
        // FormatRule 和其他辅助方法（GetStringComparator, GetNumericComparator等）
        // 也应该作为私有方法放在这个 ViewModel 中。

        // 将复杂的if-else拆分为一个专门格式化单个规则的方法
        private string FormatRule(FilterRule rule)
        {
            bool isInverse = rule is FilterInverseRule;
            FilterRule innerRule = isInverse ? (rule as FilterInverseRule).GetInnerRule() : rule;

            string paramName = LabelUtils.GetLabelFor((BuiltInParameter)innerRule.GetRuleParameter().IntegerValue);
            string comparator = "";
            string value = "";

            switch (innerRule)
            {
                case FilterStringRule fsr:
                    // 可以进一步封装一个方法来处理字符串比较符
                    comparator = GetStringComparator(fsr.GetEvaluator(), isInverse);
                    value = $"\"{fsr.RuleString}\"";
                    break;
                case FilterDoubleRule fdr:
                    comparator = GetNumericComparator(fdr.GetEvaluator(), isInverse);
                    value = (fdr.RuleValue * 304.8).ToString("F2"); // Revit 内部单位是英尺
                    break;
                case FilterIntegerRule fir:
                    comparator = GetNumericComparator(fir.GetEvaluator(), isInverse);
                    value = fir.RuleValue.ToString();
                    // Special case for enums
                    if ((BuiltInParameter)fir.GetRuleParameter().IntegerValue == BuiltInParameter.FUNCTION_PARAM)
                    {
                        value = ((WallFunction)fir.RuleValue).ToString();
                    }
                    break;
                case FilterElementIdRule fer:
                    comparator = isInverse ? "不是" : "是";
                    value = Document.GetElement(fer.RuleValue)?.Name ?? "无效ID";
                    break;
                case HasValueFilterRule _:
                    return $"“{paramName}” 有值";
                case HasNoValueFilterRule _:
                    return $"“{paramName}” 没有值";
                default:
                    return "未识别规则";
            }

            return $"“{paramName}” {comparator} {value}";
        }
        // 辅助方法，让代码更清晰
        private string GetStringComparator(FilterStringRuleEvaluator evaluator, bool inverse)
        {
            if (evaluator is FilterStringBeginsWith) return inverse ? "开始部分不是" : "开始部分是";
            if (evaluator is FilterStringEndsWith) return inverse ? "末尾不是" : "末尾是";
            if (evaluator is FilterStringEquals) return inverse ? "不等于" : "等于";
            if (evaluator is FilterStringContains) return inverse ? "不包含" : "包含";
            return "未知比较";
        }
        private string GetNumericComparator(FilterNumericRuleEvaluator evaluator, bool inverse)
        {
            if (evaluator is FilterNumericEquals) return inverse ? "≠" : "=";
            if (evaluator is FilterNumericGreater) return inverse ? "≤" : ">";
            if (evaluator is FilterNumericGreaterOrEqual) return inverse ? "<" : "≥";
            if (evaluator is FilterNumericLess) return inverse ? "≥" : "<";
            if (evaluator is FilterNumericLessOrEqual) return inverse ? ">" : "≤";
            return "未知比较";
        }
        public ObservableCollection<ViewFilterEntity> Collection { get; set; } = new ObservableCollection<ViewFilterEntity>();
    }
}
