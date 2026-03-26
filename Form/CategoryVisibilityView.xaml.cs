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
        private ImportInstance cadInstance;
        private View activeView;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public CategoryVisibilityViewModel(UIApplication uiApp)
        {
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            activeView = doc.ActiveView;
            ViewName = activeView.Name;

            // 1. 收集当前视图所有可见元素
            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id).WhereElementIsNotElementType();
            // 用于保存 显示名称 -> CategoryId 的映射
            Dictionary<string, ElementId> categoryDict = new Dictionary<string, ElementId>();
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
                    }
                }
            }
            if (categoryDict.Count == 0)
            {
                TaskDialog.Show("提示", "当前视图中没有可供隐藏的类别！");
                return;
            }
            AllCategoryNames = new ObservableCollection<string>(categoryDict.Keys.OrderBy(k => k).ToList());


            //// 3. 将显示名称提取为 List 并按字母 A-Z 排序
            //List<string> displayNames = categoryDict.Keys.OrderBy(k => k).ToList();
            //// 4. 打开 WPF 窗口，传入选项
            //CategorySelectWindow window = new CategorySelectWindow(displayNames);
            //bool? result = window.ShowDialog();
            //// 5. 如果用户点击了确认
            //if (result == true)
            //{
            //    // 获取用户在界面勾选的字符串列表
            //    List<string> selectedNames = window.SelectedCategories;
            //    if (selectedNames != null && selectedNames.Count > 0)
            //    {
            //        // 将字符串反向映射回 CategoryId
            //        List<ElementId> targetCategoryIds = new List<ElementId>();
            //        foreach (string name in selectedNames)
            //        {
            //            if (categoryDict.TryGetValue(name, out ElementId catId))
            //            {
            //                targetCategoryIds.Add(catId);
            //            }
            //        }
            //        // 6. 执行批量隐藏（这里以“隐藏选中的类别”为例）
            //        CategoryVisibilityService.SetCategoriesVisibility(doc, activeView, targetCategoryIds, hide: true);
            //    }
            //}

        }
        private void InitLayers(Document _doc)
        {
            QueryELement(null);
        }
        private void UpdateCategoryVisibility(IList selectedNames)
        {
        }
        public ICommand ShowAllLayersCommand => new BaseBindingCommand(ShowAllLayers);
        private void ShowAllLayers(object obj)
        { }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string filterText)
        {

        }
        private IList _selectedCategoryNames = new List<string>();
        public IList SelectedCategoryNames
        {
            get => _selectedCategoryNames;
            set
            {
                _selectedCategoryNames = value;
                OnPropertyChanged(); // 触发通知
                UpdateCategoryVisibility(value); // 手动触发更新逻辑
            }
        }
        private ObservableCollection<string> _AllCategoryNames = new ObservableCollection<string>();
        public ObservableCollection<string> AllCategoryNames
        {
            get => _AllCategoryNames;
            set => SetProperty(ref _AllCategoryNames, value);
        }
        //private List<CadLayerItem> _rawLayers = new List<CadLayerItem>();
        public string ViewName { get; set; } = string.Empty;
    }
}
