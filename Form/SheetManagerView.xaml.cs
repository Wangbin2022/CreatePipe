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
    /// SheetManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class SheetManagerView : Window
    {
        public SheetManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new SheetManagerViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class SheetManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<SheetEntity>
    {
        public Document Document { get; }
        public UIDocument UIDoc { get; }
        // 动态获取当前激活视图，而不是只在构造时锁定
        public View ActiveView => UIDoc.ActiveView;
        // 4. 通用外部事件处理器
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        // 2. 核心数据集合 (取代原有的 AllSheets)
        private ObservableCollection<SheetEntity> _collection = new ObservableCollection<SheetEntity>();
        public ObservableCollection<SheetEntity> Collection { get => _collection; set => SetProperty(ref _collection, value); }
        public SheetManagerViewModel(UIApplication uiApp)
        {
            UIDoc = uiApp.ActiveUIDocument;
            Document = UIDoc.Document;
            InitFunc();
        }
        // 1. 初始化与数据加载
        public void InitFunc()
        {
            QueryElement(null);
            if (Collection.Count == 0)
            {
                TaskDialog.Show("提示", "当前模型中没有找到图纸");
            }
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        // 5. 逻辑实现方法: 查询
        public void QueryElement(string text)
        {
            Collection.Clear();
            List<ViewSheet> sheets = new FilteredElementCollector(Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            foreach (var item in sheets)
            {
                if (string.IsNullOrWhiteSpace(text) ||
                    item.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.SheetNumber.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Collection.Add(new SheetEntity(item, ExternalHandler));
                }
            }
        }
        public ICommand DeleteElementCommand => new RelayCommand<SheetEntity>(DeleteElement);
        // 5. 逻辑实现方法: 单个删除
        public void DeleteElement(SheetEntity entity)
        {
            if (entity == null) return;
            ICollection<ElementId> viewportIds = entity.Sheet.GetAllViewports();
            // 2. 获取图纸上的明细表实例 (ScheduleSheetInstance) Id
            // 注意：图纸上的明细表不属于 Viewport 范畴，需要单独过滤
            List<ElementId> scheduleIds = new FilteredElementCollector(Document, entity.Id)
                .OfClass(typeof(ScheduleSheetInstance)).ToElementIds().ToList();
            // 合并所有待删除的 ID
            List<ElementId> allIdsToDelete = viewportIds.Concat(scheduleIds).ToList();
            if (allIdsToDelete.Count > 0)
            {
                ExternalHandler.Run(app =>
                {
                    try
                    {
                        int count = allIdsToDelete.Count;
                        // 3. 执行删除事务
                        NewTransaction.Execute(Document, "移除图纸所有内容", () =>
                        {
                            Document.Delete(allIdsToDelete);
                        });
                        QueryElement(null);
                        TaskDialog.Show("提示", $"成功从图纸 [{entity.SheetName}] 中移除了 {count} 个项（含视口与明细表）！");
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            else
            {
                TaskDialog.Show("提示", "未发现图纸视口");
            }
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        // 5. 逻辑实现方法: 批量删除
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;
            // 必须转换为新 List，防止 UI 集合改变时引发枚举异常
            var itemsToDelete = selectedItems.Cast<SheetEntity>().ToList();
            if (itemsToDelete.Count == 0) return;
            ExternalHandler.Run(app =>
            {
                var idsToDelete = new List<ElementId>();
                foreach (var item in itemsToDelete)
                {
                    // 保护机制：不能删除当前正处于激活状态的图纸
                    if (item.Id == ActiveView.Id)
                    {
                        TaskDialog.Show("警告", $"禁止删除当前处于激活状态的图纸: {item.SheetName}");
                        continue;
                    }
                    idsToDelete.Add(item.Id);
                }
                if (idsToDelete.Count == 0) return;
                TransactionWithProgressBarHelper.Execute(Document, "批量删除图纸", (service) =>
                {
                    service.UpdateMax(itemsToDelete.Count);
                    int index = 0;
                    try
                    {
                        Document.Delete(idsToDelete);
                        // Revit 中删除成功后，再同步移除 UI 集合数据
                        foreach (var item in itemsToDelete)
                        {
                            if (idsToDelete.Contains(item.Id))
                            {
                                Collection.Remove(item);
                                service.Update(++index, item.SheetName.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("错误", $"删除失败: {ex.Message}");
                    }
                });
            });
        }
        public ICommand ListViewCommand => new RelayCommand<SheetEntity>(ListViews);
        private void ListViews(SheetEntity entity)
        {
            if (entity == null || entity.RelatedViews.Count == 0) return;

            UniversalDictionaryListView universalDictionaryList = new UniversalDictionaryListView(entity.RelatedViews, "视图统计");
            universalDictionaryList.ShowDialog();
        }
        public ICommand FindViewCommand => new RelayCommand<SheetEntity>(ActivateView);
        private void ActivateView(SheetEntity entity)
        {
            if (entity == null) return;

            // 切换 ActiveView 会修改 Revit UI 状态，必须放在 ExternalHandler 中
            ExternalHandler.Run(app =>
            {
                if (Document.GetElement(entity.Id) is View view)
                {
                    UIDoc.ActiveView = view;
                }
            });
        }
    }

    //public class SheetManagerViewModel : ObserverableObject
    //{
    //    public Document Document { get; set; }
    //    public UIDocument uIDoc { get; set; }
    //    public Autodesk.Revit.DB.View ActiveView { get; set; }
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public SheetManagerViewModel(UIApplication uiApp)
    //    {
    //        Document = uiApp.ActiveUIDocument.Document;
    //        uIDoc = uiApp.ActiveUIDocument;
    //        ActiveView = Document.ActiveView;

    //        List<ViewSheet> sheets = new FilteredElementCollector(Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
    //        if (sheets.Count() == 0)
    //        {
    //            TaskDialog.Show("tt", "当前模型中没有找到图纸");
    //            return;
    //        }
    //        QueryELement(null);
    //    }
    //    public ICommand ListViewCommand => new RelayCommand<SheetEntity>(ListViews);
    //    private void ListViews(SheetEntity entity)
    //    {
    //        if (entity == null) return;
    //        Dictionary<string, string> dataList = entity.RelatedViews;
    //        UniversalDictionaryListView universalDictionaryList = new UniversalDictionaryListView(dataList, "视图统计");
    //        universalDictionaryList.ShowDialog();
    //    }
    //    public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
    //    private void DeleteELements(IEnumerable<object> selectedElements)
    //    {
    //        _externalHandler.Run(app =>
    //            {
    //                Document.NewTransaction(() =>
    //                {
    //                    List<ElementId> toRemove = new List<ElementId>();
    //                    List<SheetEntity> selectedItems = selectedElements.Cast<SheetEntity>().ToList();
    //                    if (selectedItems == null) return;
    //                    foreach (var item in selectedItems)
    //                    {
    //                        if (item.Id == ActiveView.Id) return;
    //                        toRemove.Add(item.Id);
    //                        AllSheets.Remove(item);
    //                    }
    //                    Document.Delete(toRemove);
    //                }, "删除多个视图");
    //            });
    //    }
    //    public ICommand FindViewCommand => new RelayCommand<SheetEntity>(ActivateView);
    //    private void ActivateView(SheetEntity entity)
    //    {
    //        View view = Document.GetElement(entity.Id) as View;
    //        if (view != null) uIDoc.ActiveView = view;
    //    }
    //    public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
    //    private void QueryELement(string obj)
    //    {
    //        AllSheets.Clear();
    //        List<ViewSheet> sheets = new FilteredElementCollector(Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
    //        foreach (var item in sheets)
    //        {
    //            string sheetName = item.Name;
    //            if (string.IsNullOrEmpty(obj) || sheetName.Contains(obj) || sheetName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || item.SheetNumber.Contains(obj))
    //            {
    //                SheetEntity sheetEntity = new SheetEntity(item);
    //                AllSheets.Add(sheetEntity);
    //            }
    //        }
    //    }
    //    private ObservableCollection<SheetEntity> allSheets = new ObservableCollection<SheetEntity>();
    //    public ObservableCollection<SheetEntity> AllSheets
    //    {
    //        get => allSheets;
    //        set => SetProperty(ref allSheets, value);
    //    }
    //}
}
