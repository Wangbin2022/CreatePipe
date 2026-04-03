using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// XrefCADLayerView.xaml 的交互逻辑
    /// </summary>
    public partial class XrefCADLayerView : Window
    {
        public XrefCADLayerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new XrefCADLayerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class XrefCADLayerViewModel : ObserverableObject
    {
        private ImportInstance cadInstance;
        private View activeView;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public XrefCADLayerViewModel(UIApplication uiApp)
        {
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document _doc = uiDoc.Document;
            activeView = _doc.ActiveView;
            try
            {
                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, new CadSelectionFilter(), "请选择一个 CAD 链接或导入文件");
                cadInstance = _doc.GetElement(pickedRef) as ImportInstance;
                InitLayers(_doc);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void InitLayers(Document _doc)
        {
            if (cadInstance == null) return;
            // 1. 设置文件名
            var typeID = cadInstance.GetTypeId();
            FileName = _doc.GetElement(typeID).Name;
            // 2. 准备容器
            _rawLayers.Clear();
            var initialSelectedNames = new List<string>(); // 临时存放选中的名称字符串
            CategoryNameMap subCategories = cadInstance.Category.SubCategories;
            foreach (Category subCat in subCategories)
            {
                if (!activeView.CanCategoryBeHidden(subCat.Id)) continue;

                bool isVisible = !activeView.GetCategoryHidden(subCat.Id);

                var item = new CadLayerItem
                {
                    Title = subCat.Name,
                    CategoryId = subCat.Id,
                    LayerColor = subCat.LineColor,
                    IsSelected = isVisible
                };

                // 修复点：添加的是 Title (string)，而不是 item 对象
                if (isVisible)
                {
                    initialSelectedNames.Add(item.Title);
                }
                _rawLayers.Add(item);
            }
            // 3. 先更新列表源
            QueryELement(null);
            // 4. 修复点：最后一次性赋值给 SelectedXrefLayers，触发控件的 SelectNodes
            this.SelectedXrefLayers = initialSelectedNames;
        }
        private void UpdateRevitVisibility(IList selectedNames)
        {
            // 增加防御性编程
            if (cadInstance == null || activeView == null || selectedNames == null) return;
            var selectedSet = new HashSet<string>(selectedNames.Cast<string>());
            _externalHandler.Run(app =>
            {
                // 再次检查 activeView 是否有效
                if (activeView == null || !activeView.IsValidObject) return;
                using (Transaction ts = new Transaction(app.ActiveUIDocument.Document, "显隐图层"))
                {
                    ts.Start();
                    foreach (var item in _rawLayers)
                    {
                        bool shouldBeVisible = selectedSet.Contains(item.Title);
                        bool isHidden = activeView.GetCategoryHidden(item.CategoryId);
                        // 只有状态不一致时才修改，减少视图刷新开销
                        if (shouldBeVisible && isHidden)
                            activeView.SetCategoryHidden(item.CategoryId, false);
                        else if (!shouldBeVisible && !isHidden)
                            activeView.SetCategoryHidden(item.CategoryId, true);
                    }
                    ts.Commit();
                }
            });
        }
        public ICommand ShowAllLayersCommand => new BaseBindingCommand(ShowAllLayers);
        private void ShowAllLayers(object obj)
        {
            // 检查基础对象是否依然有效
            if (cadInstance == null || activeView == null || !activeView.IsValidObject) return;
            _externalHandler.Run(app =>
            {
                // 再次确认文档和视图状态
                Document doc = app.ActiveUIDocument.Document;
                if (doc == null || !doc.IsValidObject) return;
                using (Transaction ts = new Transaction(doc, "打开所有CAD图层"))
                {
                    ts.Start();
                    // 遍历所有原始记录的图层（不受搜索过滤影响）
                    foreach (CadLayerItem item in _rawLayers)
                    {
                        // 检查该图层是否可以被隐藏/显示
                        if (activeView.CanCategoryBeHidden(item.CategoryId))
                        {
                            // 设置为可见 (Hidden = false)
                            activeView.SetCategoryHidden(item.CategoryId, false);
                        }
                    }
                    ts.Commit();
                }
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string filterText)
        {
            var filtered = _rawLayers.AsEnumerable();
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(x => x.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            AllXrefLayers = new ObservableCollection<CadLayerItem>(filtered.OrderBy(x => x.Title));
        }
        // 修改为 string 列表，以匹配自定义控件的 SetSelectedItems 输出
        private IList _selectedXrefLayers = new List<string>();
        public IList SelectedXrefLayers
        {
            get => _selectedXrefLayers;
            set
            {
                _selectedXrefLayers = value;
                OnPropertyChanged(); // 触发通知
                UpdateRevitVisibility(value); // 手动触发更新逻辑
            }
        }
        private ObservableCollection<CadLayerItem> _AllXrefLayers = new ObservableCollection<CadLayerItem>();
        public ObservableCollection<CadLayerItem> AllXrefLayers
        {
            get => _AllXrefLayers;
            set => SetProperty(ref _AllXrefLayers, value);
        }
        private List<CadLayerItem> _rawLayers = new List<CadLayerItem>();
        public string FileName { get; set; } = string.Empty;
    }
    public class CadLayerItem : ObserverableObject
    {
        public string Title { get; set; }
        public ElementId CategoryId { get; set; }
        public Autodesk.Revit.DB.Color LayerColor { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                SetProperty(ref _isSelected, value);
                OnIsSelectedChanged?.Invoke(this);
            }
        }
        // 回调：让 ViewModel 来决定当勾选改变时执行什么 Revit 操作
        public Action<CadLayerItem> OnIsSelectedChanged { get; set; }
    }
}
