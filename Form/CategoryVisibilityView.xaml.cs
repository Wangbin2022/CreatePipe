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
using System.Collections.Specialized;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// CategoryVisibilityView.xaml 的交互逻辑
    /// </summary>
    public partial class CategoryVisibilityView : Window
    {
        public CategoryVisibilityView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new CategoryVisibilityViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class CategoryVisibilityViewModel : ObserverableObject
    {
        private Document _doc;
        private View activeView;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        private Dictionary<string, ElementId> categoryDict = new Dictionary<string, ElementId>();
        public CategoryVisibilityViewModel(UIApplication uiApp)
        {
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            _doc = uiDoc.Document;
            activeView = _doc.ActiveView;
            ViewName = activeView.Name;

            InitLayers();
            //SelectedCategoryNames.CollectionChanged += OnSelectedCategoriesChanged;
        }
        //private void OnSelectedCategoriesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    // 将现有的集合传给更新方法
        //    UpdateCategoryVisibility(SelectedCategoryNames);
        //}
        private void InitLayers()
        {
            ICollection<ElementId> idsToExclude = new FilteredElementCollector(_doc, activeView.Id)
                .WhereElementIsNotElementType()
                .WherePasses(new LogicalOrFilter(
                    new ElementClassFilter(typeof(RevitLinkInstance)),
                    new ElementClassFilter(typeof(ImportInstance)))
                ).ToElementIds();
            ExclusionFilter exclusionFilter = new ExclusionFilter(idsToExclude);
            // 1. 收集当前视图所有可见元素排除掉链接元素
            FilteredElementCollector collector = new FilteredElementCollector(_doc, activeView.Id).WhereElementIsNotElementType().WherePasses(exclusionFilter);
            // 用于保存 显示名称 -> CategoryId 的映射
            categoryDict = new Dictionary<string, ElementId>();
            foreach (Element elem in collector)
            {
                Category cat = elem.Category;
                // 过滤无效类别，并确保该类别在当前视图中是允许被隐藏的
                if (cat != null && activeView.CanCategoryBeHidden(cat.Id))
                {
                    string catName = cat.Name;
                    if (string.IsNullOrWhiteSpace(catName)) continue;
                    // 2. 提取首字符并转拼音首字母
                    string firstChar = catName.Substring(0, 1);
                    string spell = ChineseToSpellService.GetChineseSpell(firstChar);
                    // 拼装显示名称，如 "Q-墙", "M-门", "W-Walls"
                    string displayName = $"{spell}-{catName}";
                    // 存入字典去重
                    if (!categoryDict.ContainsKey(displayName))
                    {
                        categoryDict.Add(displayName, cat.Id);
                        _rawCategoryNames.Add(displayName);
                    }
                }
            }
            if (categoryDict.Count == 0)
            {
                TaskDialog.Show("提示", "当前视图中没有可供隐藏的类别！");
                return;
            }
            QueryELement(null);
        }
        // 【修改】: 创建一个命令，用于响应控件的选择变化
        public ICommand SelectionChangedUpdateCommand => new RelayCommand<ObservableCollection<string>>(UpdateCategoryVisibility);

        private void UpdateCategoryVisibility(ObservableCollection<string> selectedNames)
        {
            if (selectedNames == null || categoryDict == null) return;

            // 1. 准备两个集合：一个存需要显示的，一个存需要隐藏的
            List<BuiltInCategory> categoriesToShow = new List<BuiltInCategory>();
            List<BuiltInCategory> categoriesToHide = new List<BuiltInCategory>();

            // 2. 遍历所有的类别字典 (假定 categoryDict 包含了列表中所有的待操作项)
            foreach (var kvp in categoryDict)
            {
                string categoryName = kvp.Key;
                ElementId catId = kvp.Value;
                BuiltInCategory bic = (BuiltInCategory)catId.IntegerValue;
                if (selectedNames.Contains(categoryName))
                {
                    categoriesToShow.Add(bic); // 在选中列表 -> 显示
                }
                else
                {
                    categoriesToHide.Add(bic); // 不在选中列表 -> 隐藏
                }
            }
            // 3. 将对 Revit 文档的操作交给外部事件处理器 (回到 Revit API 主线程)
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(app.ActiveUIDocument.Document, "修改类别可见性", () =>
                {
                    if (categoriesToShow.Count > 0)
                    {
                        CategoryVisibilityService.SetCategoriesVisibility(app.ActiveUIDocument.Document, activeView, categoriesToShow, true);
                    }
                    if (categoriesToHide.Count > 0)
                    {
                        CategoryVisibilityService.SetCategoriesVisibility(app.ActiveUIDocument.Document, activeView, categoriesToHide, false);
                    }
                });
            });
        }
        public ICommand ShowAllLayersCommand => new BaseBindingCommand(ShowAllLayers);
        private void ShowAllLayers(object obj)
        {
            _externalHandler.Run(app =>
            {
                List<BuiltInCategory> categoriesToShow = new List<BuiltInCategory>();
                foreach (var kvp in categoryDict)
                {
                    string categoryName = kvp.Key;
                    ElementId catId = kvp.Value;
                    BuiltInCategory bic = (BuiltInCategory)catId.IntegerValue;
                    categoriesToShow.Add(bic);
                }
                NewTransaction.Execute(app.ActiveUIDocument.Document, "恢复类别显示", () =>
                {
                    CategoryVisibilityService.SetCategoriesVisibility(app.ActiveUIDocument.Document, activeView, categoriesToShow, true);
                });
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string filterText)
        {
            AllCategoryNames.Clear();
            SelectedCategoryNames.Clear();
            var filtered = _rawCategoryNames.AsEnumerable();
            foreach (var item in filtered)
            {
                if (string.IsNullOrEmpty(filterText) || item.Contains(filterText) || item.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AllCategoryNames.Add(item);
                    SelectedCategoryNames.Add(item);
                }
            }
        }
        private ObservableCollection<string> _selectedCategoryNames = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedCategoryNames
        {
            get => _selectedCategoryNames;
            set
            {
                _selectedCategoryNames = value;
                OnPropertyChanged(); // 触发通知
            }
        }
        private ObservableCollection<string> _allCategoryNames = new ObservableCollection<string>();
        public ObservableCollection<string> AllCategoryNames
        {
            get => _allCategoryNames;
            set => SetProperty(ref _allCategoryNames, value);
        }
        private List<string> _rawCategoryNames = new List<string>();
        public string ViewName { get; set; } = string.Empty;
    }
}
