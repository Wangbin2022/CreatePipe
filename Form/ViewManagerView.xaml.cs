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
    /// ViewManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewManagerView : Window
    {
        public ViewManagerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new ViewManagerViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class ViewManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<ViewEntity>
    {
        public Document Document { get; }
        public UIDocument UIDoc { get; }
        public View ActiveView { get; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        public ObservableCollection<ViewEntity> Collection { get; set; } = new ObservableCollection<ViewEntity>();
        private List<View> _rawRevitViews = new List<View>();
        private HashSet<ElementId> _viewsOnSheets = new HashSet<ElementId>();
        public ViewManagerViewModel(UIApplication application)
        {
            UIDoc = application.ActiveUIDocument;
            Document = UIDoc.Document;
            ActiveView = Document.ActiveView;
            InitFunc();
        }
        public void InitFunc()
        {
            _rawRevitViews.Clear();
            _viewsOnSheets.Clear();
            // 1. 性能优化：一次性获取所有 Viewport，记录哪些 View 已放在图纸上
            // 避免在 Entity 的循环中重复创建收集器
            var viewports = new FilteredElementCollector(Document).OfClass(typeof(Viewport)).Cast<Viewport>();
            foreach (var vp in viewports)
            {
                _viewsOnSheets.Add(vp.ViewId);
            }
            // 2. 获取当前文档所有非样板视图
            var views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate);
            _rawRevitViews.AddRange(views);
            // 3. 初始显示
            QueryElement(null);
        }
        public ICommand NewLevelViewCommand => new BaseBindingCommand(NewLevelView);
        private void NewLevelView(object para)
        {
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "新建标高视图", () =>
                {
                    var levels = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>();
                    var usedLevelIds = new FilteredElementCollector(Document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                        .Select(v => v.GenLevel?.Id).Where(id => id != null).ToHashSet();
                    var unusedLevels = levels.Where(l => !usedLevelIds.Contains(l.Id)).ToList();
                    if (unusedLevels.Count == 0)
                    {
                        TaskDialog.Show("完成", "所有标高都已有关联平面视图");
                        return;
                    }
                    int newViewCount = 0;
                    ElementId floorPlanTypeId = Document.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeFloorPlan);
                    foreach (var level in unusedLevels)
                    {
                        try
                        {
                            var floorPlan = ViewPlan.Create(Document, floorPlanTypeId, level.Id);
                            floorPlan.Name = $"{level.Name} - 楼层平面";
                            newViewCount++;
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("错误", $"为标高 {level.Name} 创建平面失败: {ex.Message}");
                        }
                    }
                    TaskDialog.Show("完成", $"新建平面标高视图 {newViewCount} 个");
                });
            });
        }
        public ICommand FindViewCommand => new RelayCommand<ViewEntity>(ActivateView);
        private void ActivateView(ViewEntity entity)
        {
            if (entity?.Viewe != null) UIDoc.ActiveView = entity.Viewe;
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            Collection.Clear();
            var filtered = _rawRevitViews
                .Where(v => string.IsNullOrEmpty(text) || v.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(v => new ViewEntity(v, ExternalHandler, _viewsOnSheets.Contains(v.Id), 0));
            foreach (var item in filtered) Collection.Add(item);
        }
        public ICommand DeleteElementCommand => new RelayCommand<ViewEntity>(DeleteElement);
        public void DeleteElement(ViewEntity entity)
        {
            if (entity == null || entity.Id == ActiveView.Id) return;
            ElementId idToDelete = entity.Id;
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(app.ActiveUIDocument.Document, "删除视图", () =>
                {
                    app.ActiveUIDocument.Document.Delete(idToDelete);
                });
                _rawRevitViews.Remove(entity.Viewe);  // 直接移除对象引用，不访问 Id
                System.Windows.Application.Current.Dispatcher.Invoke(() => Collection.Remove(entity));
            });
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;
            var toDeleteList = selectedItems.Cast<ViewEntity>().Where(e => e.Id != ActiveView.Id).ToList();
            if (toDeleteList.Count == 0) return;
            // 【关键】事务前缓存所有 ID
            var idsToDelete = toDeleteList.Select(e => e.Id).ToList();
            //需要加入批量处理进度条逻辑
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(app.ActiveUIDocument.Document, "批量删除视图", (service) =>
                {
                    service.UpdateMax(idsToDelete.Count());
                    int index = 0;
                    foreach (var id in idsToDelete)
                    {
                        service.Update(++index, app.ActiveUIDocument.Document.GetElement(id).Name);
                        Document.Delete(id);
                    }
                });
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in toDeleteList)
                    {
                        Collection.Remove(item);
                    }
                    string resultMessage = $"已成功删除 {idsToDelete.Count} 个视图。";
                    int remainedCount = selectedItems.Count() - idsToDelete.Count();
                    if (remainedCount > 0) resultMessage += $"\n{remainedCount} 个视图因在使用中而被保留。";
                    TaskDialog.Show("删除完成", resultMessage);
                });
            });
        }
        //public Document Document { get; }
        //public UIDocument uiDoc { get; }
        //public View ActiveView { get; }
        //// 接口实现：处理器与集合
        //public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        //public ViewManagerViewModel(UIApplication application)
        //{
        //    uiDoc = application.ActiveUIDocument;
        //    Document = uiDoc.Document;
        //    ActiveView = Document.ActiveView;
        //    // 执行初始化
        //    InitFunc();
        //}
        ///// <summary>
        ///// 接口实现：初始化数据。
        ///// 负责一次性收集所有视图及关联关系（如Viewport状态）
        ///// </summary>
        //public void InitFunc()
        //{
        //    _rawRevitViews.Clear();
        //    _viewsOnSheets.Clear();
        //    // 1. 性能优化：一次性获取所有 Viewport，记录哪些 View 已放在图纸上
        //    // 避免在 Entity 的循环中重复创建收集器
        //    var viewports = new FilteredElementCollector(Document).OfClass(typeof(Viewport)).Cast<Viewport>();
        //    foreach (var vp in viewports)
        //    {
        //        _viewsOnSheets.Add(vp.ViewId);
        //    }
        //    // 2. 获取当前文档所有非样板视图
        //    var views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate);
        //    _rawRevitViews.AddRange(views);
        //    // 3. 初始显示
        //    QueryELement(null);
        //}
        //public ICommand NewLevelViewCommand => new BaseBindingCommand(NewLevelView);
        //private void NewLevelView(Object para)
        //{
        //    ExternalHandler.Run(app =>
        //    {
        //        NewTransaction.Execute(Document, "新建标高视图", () =>
        //        {
        //            int newView = 0;
        //            var levels = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>().ToList();
        //            // 获取已有平面视图的标高ID
        //            var usedLevelIds = new FilteredElementCollector(Document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
        //                .Select(v => v.GenLevel?.Id)
        //                .Where(id => id != null).ToHashSet();
        //            // 筛选未使用的标高
        //            var unusedLevels = levels.Where(l => !usedLevelIds.Contains(l.Id)).ToList();
        //            if (unusedLevels.Count == 0)
        //            {
        //                TaskDialog.Show("完成", "所有标高都已有关联平面视图");
        //                return;
        //            }
        //            else
        //            {
        //                foreach (Level level in unusedLevels)
        //                {
        //                    try
        //                    {
        //                        // 创建楼层平面
        //                        ElementId floorPlanTypeId = Document.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeFloorPlan);
        //                        ViewPlan floorPlan = ViewPlan.Create(Document, floorPlanTypeId, level.Id);
        //                        floorPlan.Name = $"{level.Name} - 楼层平面";
        //                        newView++;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        TaskDialog.Show("错误", $"为标高 {level.Name} 创建平面失败: {ex.Message}");
        //                    }
        //                }
        //                TaskDialog.Show("tt", $"新建平面标高视图{newView}个");
        //            }
        //        });
        //    });
        //}
        //public ICommand FindViewCommand => new RelayCommand<ViewEntity>(ActivateView);
        //private void ActivateView(ViewEntity entity)
        //{
        //    View view = entity.Document.GetElement(entity.Id) as View;
        //    if (view != null) uiDoc.ActiveView = view;
        //}
        //private List<View> _rawRevitViews = new List<View>();
        //private HashSet<ElementId> _viewsOnSheets = new HashSet<ElementId>(); // 预存已上图纸的视图ID
        //// --- 接口实现：查询 ---
        //public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        //public void QueryELement(string text)
        //{
        //    Collection.Clear();
        //    // 根据关键字过滤原始视图数据
        //    var filtered = _rawRevitViews
        //        .Where(v => string.IsNullOrEmpty(text) || v.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
        //        .Select(v => new ViewEntity(v, ExternalHandler, _viewsOnSheets.Contains(v.Id)));
        //    foreach (var item in filtered)
        //    {
        //        Collection.Add(item);
        //    }
        //}
        //// --- 接口实现：单删 ---
        //public ICommand DeleteElementCommand => new RelayCommand<ViewEntity>(DeleteElement);
        //public void DeleteElement(ViewEntity entity)
        //{
        //    if (entity == null) return;
        //    ElementId idToDelete = entity.Id;
        //    if (idToDelete == ActiveView.Id) return;
        //    ExternalHandler.Run(app =>
        //    {
        //        NewTransaction.Execute(app.ActiveUIDocument.Document, "删除视图", () =>
        //        {
        //            app.ActiveUIDocument.Document.Delete(idToDelete);
        //        });
        //        _rawRevitViews.Remove(entity.Viewe);  // 直接移除对象引用，不访问 Id
        //        System.Windows.Application.Current.Dispatcher.Invoke(() => Collection.Remove(entity));
        //    });
        //}
        //// --- 接口实现：批删 ---
        //public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        //public void DeleteElements(IEnumerable<object> selectedItems)
        //{
        //    if (selectedItems == null) return;
        //    var toDeleteList = selectedItems.Cast<ViewEntity>().Where(e => e.Id != ActiveView.Id).ToList();
        //    if (toDeleteList.Count == 0) return;
        //    // 【关键】事务前缓存所有 ID
        //    var idsToDelete = toDeleteList.Select(e => e.Id).ToList();
        //    //需要加入批量处理进度条逻辑
        //    ExternalHandler.Run(app =>
        //    {
        //        TransactionWithProgressBarHelper.Execute(app.ActiveUIDocument.Document, "批量删除视图", (service) =>
        //        {
        //            service.UpdateMax(idsToDelete.Count());
        //            int index = 0;
        //            foreach (var id in idsToDelete)
        //            {
        //                service.Update(++index, app.ActiveUIDocument.Document.GetElement(id).Name);
        //                Document.Delete(id);
        //            }
        //        });
        //        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            foreach (var item in toDeleteList)
        //            {
        //                Collection.Remove(item);
        //            }
        //        });
        //    });
        //}
        //public ObservableCollection<ViewEntity> Collection { get; set; } = new ObservableCollection<ViewEntity>();
    }
}
