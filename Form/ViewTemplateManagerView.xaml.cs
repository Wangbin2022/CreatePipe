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
    /// ViewTemplateManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewTemplateManagerView : Window
    {
        public ViewTemplateManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ViewTemplateManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        //private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var dataGrid = sender as DataGrid;
        //    if (dataGrid != null)
        //    {
        //        foreach (ViewTemplateManagerViewModel item in e.AddedItems)
        //        {
        //            item.IsSelected = true;
        //        }
        //        foreach (ViewTemplateManagerViewModel item in e.RemovedItems)
        //        {
        //            item.IsSelected = false;
        //        }
        //    }
        //}
    }
    public class ViewTemplateManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<ViewEntity>
    {
        public Document Document { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        public ObservableCollection<ViewEntity> Collection { get; set; } = new ObservableCollection<ViewEntity>();
        private List<View> _rawViews = new List<View>();
        ////防止多选entity应用到视图，与Attached Property配合
        //// 从 Helper 获取 SelectedView
        //private ViewEntity _selectedView;
        //public ViewEntity SelectedView
        //{
        //    get => _selectedView;
        //    set
        //    {
        //        _selectedView = value;
        //        OnPropertyChanged();
        //        if (EnableCategoryList && value != null)
        //        {
        //            UpdateCategoryItems(value);
        //        }
        //    }
        //}
        private ViewEntity _selectedView;
        public ViewEntity SelectedView
        {
            get => _selectedView;
            set
            {
                _selectedView = value;
                OnPropertyChanged(nameof(SelectedView));
                // 只有在 SelectedView 改变时，才更新 CategoryItems
                // 同时，根据 EnableCategoryList 决定是否需要加载
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
        // 从 Helper 获取 SelectedItems (用于 MultiSelectListBox)
        private List<string> _selectedListBoxItems = new List<string>();
        public List<string> SelectedListBoxItems // 重命名以避免与DataGrid的SelectedItems混淆
        {
            get => _selectedListBoxItems;
            set => SetProperty(ref _selectedListBoxItems, value);
        }
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
        // 加载 MultiSelectListBox 的 ItemsSource
        private void UpdateCategoryItems(ViewEntity entity)
        {
            // 性能优化：如果已经加载过，不再重复查找 (可选)
            if (entity.CategoryItems != null && entity.CategoryItems.Count > 0) return;
            var viewType = entity.Viewe.ViewType;
            var names = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate && v.ViewType == viewType)
                .Select(v => v.Name).OrderBy(n => n).ToList();
            entity.CategoryItems = names;
        }
        // 计算属性，直接判断 RowCount
        public bool EnableCategoryList => RowCount == 1;
        public int ViewTemplateCount => Collection.Count;
        public ViewTemplateManagerViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            InitFunc();
        }
        public void InitFunc()
        {
            _rawViews.Clear();
            var views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate);
            _rawViews.AddRange(views);
            QueryElement(null);
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            // 1. 获取文档中所有的视图（包括非样板视图），用于统计
            var allViewsInDoc = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Views)
                .OfClass(typeof(View))
                .Cast<View>()
                .ToList();

            // 2. 创建一个字典：Key 是样板 ID，Value 是被使用的次数
            // 逻辑：找出所有设置了样板的视图，按样板 ID 分组并计数
            var usageStats = allViewsInDoc
                .Where(v => v.ViewTemplateId != ElementId.InvalidElementId)
                .GroupBy(v => v.ViewTemplateId)
                .ToDictionary(g => g.Key, g => g.Count());

            Collection.Clear();

            // 3. 过滤出样板视图，并赋予统计值
            var templateEntities = allViewsInDoc
                .Where(v => v.IsTemplate) // 只处理样板
                .Where(v => string.IsNullOrEmpty(text) || v.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(v =>
                {
                    // 从字典中获取次数，查不到则为 0
                    int count = usageStats.TryGetValue(v.Id, out int val) ? val : 0;
                    return new ViewEntity(v, ExternalHandler, true, count);
                });
            foreach (var entity in templateEntities)
            {
                Collection.Add(entity);
            }
            OnPropertyChanged(nameof(ViewTemplateCount));
            //// 1. 预计算：一次性统计所有视图对样板的使用情况
            //// Key 是样板的 ElementId, Value 是使用次数
            //var templateUsageMap = _rawViews
            //    .Where(v => v.ViewTemplateId != ElementId.InvalidElementId)
            //    .GroupBy(v => v.ViewTemplateId)
            //    .ToDictionary(g => g.Key, g => g.Count());
            //Collection.Clear();
            //// 2. 过滤样板并生成 Entity
            //var filtered = _rawViews
            //    .Where(v => v.IsTemplate && (string.IsNullOrEmpty(text) || v.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
            //    .Select(v =>
            //    {
            //        // 从预计算字典中获取次数，如果没有则为 0
            //        int usageCount = templateUsageMap.TryGetValue(v.Id, out int count) ? count : 0;
            //        return new ViewEntity(v, ExternalHandler, true, usageCount);
            //    });
            //foreach (var item in filtered) Collection.Add(item);
            ////Collection.Clear();
            ////var filtered = _rawViews
            ////    .Where(v => string.IsNullOrEmpty(text) || v.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
            ////    .Select(v => new ViewEntity(v, ExternalHandler, false, ));
            ////foreach (var item in filtered) Collection.Add(item);
        }
        public ICommand DeleteElementCommand => new RelayCommand<ViewEntity>(DeleteElement);
        public void DeleteElement(ViewEntity entity)
        {
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;

            // 1. 将所有选中项转换为 ViewEntity 列表
            var allSelectedViewEntities = selectedItems.Cast<ViewEntity>().ToList();

            if (!allSelectedViewEntities.Any()) // 如果没有选中任何项
            {
                TaskDialog.Show("提示", "未选中任何样板进行删除。");
                return;
            }

            // 2. 筛选出可以删除的样板 (templateUsageCount == 0)
            var viewEntitiesToDelete = allSelectedViewEntities.Where(ve => ve.templateUsageCount == 0).ToList();

            // 3. 统计因被使用而保留的样板数量
            var retainedCount = allSelectedViewEntities.Count - viewEntitiesToDelete.Count;

            // 4. 如果没有可删除的样板，直接提示并返回
            if (!viewEntitiesToDelete.Any())
            {
                string message = $"所选 {allSelectedViewEntities.Count} 个样板均已被使用，无法删除。";
                TaskDialog.Show("删除提示", message);
                return;
            }

            // 5. 提取待删除样板的 ElementId
            var idsToDelete = viewEntitiesToDelete.Select(ve => ve.Id).ToList();

            // 6. 执行 Revit 事务操作 (包括进度条)
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Document, "批量删除视图样板", (service) =>
                {
                    service.UpdateMax(idsToDelete.Count);
                    int index = 0;
                    foreach (var id in idsToDelete)
                    {
                        var element = Document.GetElement(id);
                        // 确保元素存在再获取名称，避免因已删除或无效ID导致NRE
                        string elementName = element?.Name ?? "未知样板";
                        service.Update(++index, elementName);
                        Document.Delete(id);
                    }
                });

                // 7. Revit 事务完成后，更新 UI (在 UI 线程执行)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    QueryElement(null); // 重新查询并刷新整个 Collection
                    string resultMessage = $"已成功删除 {viewEntitiesToDelete.Count} 个样板。";
                    if (retainedCount > 0) resultMessage += $"\n{retainedCount} 个样板因仍在使用中而被保留。";
                    TaskDialog.Show("删除完成", resultMessage);
                });
            });
        }
        public ICommand ApplyToViewCommand => new RelayCommand<List<string>>(ApplyToView);
        private void ApplyToView(List<string> selectedViewNames)
        {
            if (SelectedView == null || selectedViewNames == null || !selectedViewNames.Any()) return;
            ElementId templateId = SelectedView.Id;
            string templateName = SelectedView.ViewName;
            int successCount = 0;
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, $"应用样板: {templateName}", () =>
                {
                    var allTargetViews = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>()
                        .Where(v => !v.IsTemplate && v.ViewType == SelectedView.Viewe.ViewType).ToList();
                    foreach (string name in selectedViewNames) // 使用参数而不是 _selectedListBoxItems
                    {
                        var targetView = allTargetViews.FirstOrDefault(v => v.Name == name);
                        if (targetView != null && targetView.ViewTemplateId != templateId)
                        {
                            try
                            {
                                targetView.ViewTemplateId = templateId; successCount++;
                            }
                            catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"应用样板失败 - 视图: {name}, 错误: {ex.Message}");
                            }
                        }
                    }
                });
                // 更新计数并显示结果
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateUsageCount(SelectedView);
                    TaskDialog.Show("完成", $"已为 {successCount} 个视图应用样板 [{templateName}]");
                });
            });
        }
        private void UpdateUsageCount(ViewEntity entity)
        {
            int count = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>()
                .Count(v => v.ViewTemplateId == entity.Id);
            entity.templateUsageCount = count; // 这会触发 ViewEntity 的 OnPropertyChanged(nameof(hasView))
        }
        public ICommand FindViewsCommand => new RelayCommand<ViewEntity>(FindViews);
        private void FindViews(ViewEntity templateEntity)
        {
            if (templateEntity == null || !templateEntity.Viewe.IsTemplate) return;
            // 查找所有使用该样板的视图
            var usedViews = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Views)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.ViewTemplateId == templateEntity.Id)
                .ToDictionary(
                    v => v.Id.ToString(),  // Key: ElementId 字符串
                    v => v.Name            // Value: 视图名称
                );
            // 打开通用字典显示窗体
            var window = new UniversalDictionaryListView(
                usedViews,
                $"使用样板 [{templateEntity.ViewName}] 的视图"
            );
            window.ShowDialog();
        }
        ////抽出逻辑前最后完整功能版
        //// --- 修复 Attached Property ---
        //public static readonly DependencyProperty AttPropProperty =
        //    DependencyProperty.RegisterAttached("AttProp", typeof(bool), typeof(ViewTemplateManagerViewModel),
        //        new PropertyMetadata(false, OnAttPropChanged));
        //public static void SetAttProp(DependencyObject obj, bool value) => obj.SetValue(AttPropProperty, value);
        //public static bool GetAttProp(DependencyObject obj) => (bool)obj.GetValue(AttPropProperty);
        //private static void OnAttPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is DataGrid dg)
        //    {
        //        // 移除旧事件防止重复挂载
        //        dg.SelectionChanged -= DataGrid_SelectionChanged;
        //        if ((bool)e.NewValue)
        //        {
        //            dg.SelectionChanged += DataGrid_SelectionChanged;
        //        }
        //    }
        //}
        //private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var dg = sender as DataGrid;
        //    if (dg?.DataContext is ViewTemplateManagerViewModel vm)
        //    {
        //        // 核心修复：先更新 RowCount，再更新 SelectedView，确保逻辑顺序
        //        vm.RowCount = dg.SelectedItems.Count;

        //        // 手动同步 SelectedItem 到 VM
        //        if (dg.SelectedItems.Count > 0)
        //            vm.SelectedView = dg.SelectedItems[0] as ViewEntity;
        //        else
        //            vm.SelectedView = null;
        //    }
        //}

        ////public ICommand DeleteELementCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
        ////private void DeleteELements(IEnumerable<object> selectedElements)
        ////{
        ////    //List<ElementId> toRemove = new List<ElementId>();
        ////    //Document.NewTransaction(() =>
        ////    //{
        ////    //    var selectedItems = selectedElements.Cast<ViewTemplateEntity>().ToList();
        ////    //    if (selectedItems == null) return;
        ////    //    foreach (var item in selectedItems)
        ////    //    {
        ////    //        if (item.Num != 0) return;
        ////    //        toRemove.Add(item.Id);
        ////    //        ViewTemplates.Remove(item);
        ////    //    }
        ////    //    Document.Delete(toRemove);
        ////    //}, "删除视图样板");
        ////    ////List<ViewTemplateEntity> selectedItems = selectedElements.Cast<ViewTemplateEntity>().ToList();
        ////    ////if (selectedElements == null) return;
        ////    ////foreach (var item in selectedItems)
        ////    ////{
        ////    ////    DeleteElement(item);
        ////    ////}
        ////}

        //////public void DeleteElement(ViewTemplateEntity vt)
        //////{
        //////    _externalHandler.Run(app =>
        //////    {
        //////        if (vt.Num == 0)
        //////        {
        //////            Document.NewTransaction(() =>
        //////            {
        //////                Document.Delete(vt.Id);
        //////                ViewTemplates.Remove(vt);
        //////            }, "删除视图样板");
        //////            OnPropertyChanged(nameof(ViewTemplateCount));
        //////        }
        //////    });
        //////}

    }
}
