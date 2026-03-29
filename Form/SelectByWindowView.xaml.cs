using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// SelectByWindowView.xaml 的交互逻辑
    /// </summary>
    public partial class SelectByWindowView : Window
    {
        public SelectByWindowView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new SelectByWindowViewModel(uIApplication);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class SelectByWindowViewModel : ObserverableObject,IQueryViewModel<CountableItem>
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;
        // 原始完整数据源（用于过滤逻辑）
        private List<CountableItem> _allSourceItems = new List<CountableItem>();
        // 【核心新增】：缓存类别名称与其对应的所有 ElementId，极大提升动态选择的性能
        private Dictionary<string, List<ElementId>> _categoryElementsCache = new Dictionary<string, List<ElementId>>();

        public SelectByWindowViewModel(UIApplication uIApplication)
        {
            _uiDoc = uIApplication.ActiveUIDocument;
            _doc = _uiDoc.Document;
            // 初始化数据
            InitLayers();
        }
        //// 辅助方法：获取当前选中的元素实例（供后续点击确定按钮时使用）
        //public List<Element> GetSelectedElements()
        //{
        //    if (SelectedNames == null || !SelectedNames.Any()) return new List<Element>();

        //    return new FilteredElementCollector(_doc, _doc.ActiveView.Id)
        //        .WhereElementIsNotElementType()
        //        .Where(e => e.Category != null && SelectedNames.Contains(e.Category.Name))
        //        .ToList();
        //}
        // 【按需求新增】：控件 CheckBox 选中状态改变时触发的命令
        public ICommand SelectionChangedUpdateCommand => new RelayCommand<object>(UpdateRevitSelection);
        /// <summary>
        /// 【核心逻辑】：根据当前勾选的类别，更新 Revit 视图中的高亮选择
        /// </summary>
        private void UpdateRevitSelection(object parameter)
        {
            if (SelectedNames == null) return;
            List<ElementId> idsToSelect = new List<ElementId>();
            // 遍历当前已选中的类别名称集合
            foreach (string categoryName in SelectedNames)
            {
                // 从缓存字典中快速取出对应的 ElementId 列表
                if (_categoryElementsCache.TryGetValue(categoryName, out List<ElementId> ids))
                {
                    idsToSelect.AddRange(ids);
                }
            }
            try
            {
                // 更新 Revit 中的实际选中状态（不需要 Transaction，SetElementIds 直接生效）
                // 如果 idsToSelect 为空，SetElementIds 会自动清空 Revit 的选择，完美符合“取消全选”的逻辑
                _uiDoc.Selection.SetElementIds(idsToSelect);
            }
            catch (Exception ex)
            {
                // 防止在特殊视图或不支持选择的状态下报错
                System.Diagnostics.Debug.WriteLine($"选择更新失败: {ex.Message}");
            }
        }
        public void InitLayers()
        {
            _categoryElementsCache.Clear();
            _allSourceItems.Clear();

            // 1. 获取当前视图所有可见实例，并排除链接和导入项
            var collector = new FilteredElementCollector(_doc, _doc.ActiveView.Id)
                .WhereElementIsNotElementType()
                // 【新增过滤】：排除 Revit 链接模型
                .Where(e => !(e is RevitLinkInstance))
                // 【新增过滤】：排除导入/链接的 CAD (DWG/DXF等)
                .Where(e => !(e is ImportInstance));

            // 2. 按类别名称分组
            var groupedElements = collector
                .Where(e => e.Category != null) // 确保有类别（CAD 导入有时类别为空或为“导入符号”）
                .GroupBy(e => e.Category.Name)
                .ToList();

            // 3. 统计数量并建立缓存
            foreach (var group in groupedElements)
            {
                string categoryName = group.Key;

                // 进一步过滤：有些插件生成的 CAD 备份可能躲过了上面的判断
                // 如果类别名称包含 ".dwg" 或 ".rvt"，也可以在此处跳过
                if (categoryName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                    categoryName.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<ElementId> ids = group.Select(e => e.Id).ToList();

                // 存入字典缓存
                _categoryElementsCache[categoryName] = ids;

                // 生成 UI 统计项
                _allSourceItems.Add(new CountableItem(categoryName, ids.Count));
            }

            // 4. 排序后更新显示
            _allSourceItems = _allSourceItems.OrderBy(x => x.Name).ToList();
            UpdateDisplayCollection(_allSourceItems);
        }
        private void UpdateDisplayCollection(IEnumerable<CountableItem> items)
        {
            Collection.Clear();
            foreach (var item in items)
            {
                Collection.Add(item);
            }
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        public void QueryELement(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                UpdateDisplayCollection(_allSourceItems);
            }
            else
            {
                var filtered = _allSourceItems
                    .Where(x => x.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                UpdateDisplayCollection(filtered);
            }
        }
        // 【按需求新增】：绑定到控件的 SelectedItems
        private ObservableCollection<string> _selectedNames = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedNames
        {
            get => _selectedNames;
            set
            {
                _selectedNames = value;
                OnPropertyChanged(); // 触发通知
            }
        }

        private ObservableCollection<CountableItem> _collection = new ObservableCollection<CountableItem>();
        public ObservableCollection<CountableItem> Collection
        {
            get => _collection;
            set { _collection = value; OnPropertyChanged(); }
        }
    }
}
