using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AllViewsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AllViewsWindow : Window
    {
        public AllViewsWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ViewsManager _viewsManager;
        private string _sheetName = "Unnamed";
        private string _selectedTitleBlock;
        private ObservableCollection<ViewTreeNode> _viewTreeNodes;
        private ObservableCollection<string> _titleBlocks;

        public MainViewModel(Document doc)
        {
            _viewsManager = new ViewsManager(doc);
            LoadData();
            //OKCommand = new BaseBindingCommand(ExecuteOK, CanExecuteOK);
            OKCommand = new BaseBindingCommand(ExecuteOK);
        }

        private void LoadData()
        {
            ViewTreeNodes = _viewsManager.GetViewHierarchy();
            TitleBlocks = new ObservableCollection<string>(_viewsManager.GetTitleBlockNames());
            SelectedTitleBlock = TitleBlocks.FirstOrDefault();
        }

        public ObservableCollection<ViewTreeNode> ViewTreeNodes
        {
            get => _viewTreeNodes;
            set { _viewTreeNodes = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> TitleBlocks
        {
            get => _titleBlocks;
            set { _titleBlocks = value; OnPropertyChanged(); }
        }

        public string SelectedTitleBlock
        {
            get => _selectedTitleBlock;
            set { _selectedTitleBlock = value; OnPropertyChanged(); }
        }

        public string SheetName
        {
            get => _sheetName;
            set { _sheetName = value; OnPropertyChanged(); }
        }

        public ICommand OKCommand { get; }

        private void ExecuteOK(Object obj)
        {
            var selectedViews = GetSelectedViews();
            _viewsManager.GenerateSheet(selectedViews, SelectedTitleBlock, SheetName);
        }

        private bool CanExecuteOK() => SelectedTitleBlock != null && !string.IsNullOrWhiteSpace(SheetName);

        private List<string> GetSelectedViews()
        {
            var result = new List<string>();
            CollectCheckedViews(ViewTreeNodes, result);
            return result;
        }

        private void CollectCheckedViews(IEnumerable<ViewTreeNode> nodes, List<string> result)
        {
            foreach (var node in nodes)
            {
                if (node.IsChecked && !node.HasChildren)
                    result.Add(node.ViewName);
                CollectCheckedViews(node.Children, result);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class ViewsManager
    {
        private readonly Document _doc;
        private readonly Dictionary<string, List<string>> _viewsByCategory = new Dictionary<string, List<string>>();

        public ViewsManager(Document doc)
        {
            _doc = doc;
            LoadViews();
        }

        private void LoadViews()
        {
            var collector = new FilteredElementCollector(_doc);
            var views = collector.OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && !(v is ViewSchedule) && !(v is ViewSheet));

            foreach (var view in views)
            {
                var category = GetViewCategory(view);
                if (!_viewsByCategory.ContainsKey(category))
                    _viewsByCategory[category] = new List<string>();
                _viewsByCategory[category].Add(view.Name);
            }
        }

        private string GetViewCategory(View view)
        {
            var type = _doc.GetElement(view.GetTypeId()) as ElementType;
            string result=string.Empty;
            switch (type?.Name)
            {
                case "Building Elevation":
                    result = "Elevations";
                    break;
                case "Floor Plan":
                    result = "Floor Plans";
                    break;
                case "Ceiling Plan":
                    result = "Ceiling Plans";
                    break;
                case "Section":
                    result = "Sections";
                    break;
                case "ThreeD":
                    result = "3D Views";
                    break;
                case "Drafting":
                    result = "Drafting Views";
                    break;
                default:
                    result = "Other";
                    break;
            }
            return result;
        }

        public ObservableCollection<ViewTreeNode> GetViewHierarchy()
        {
            var root = new ObservableCollection<ViewTreeNode>();
            foreach (var category in _viewsByCategory.Keys.OrderBy(x => x))
            {
                var categoryNode = new ViewTreeNode
                {
                    ViewName = category,
                    Category = category,
                    IsChecked = false
                };
                foreach (var viewName in _viewsByCategory[category].OrderBy(x => x))
                {
                    categoryNode.Children.Add(new ViewTreeNode
                    {
                        ViewName = viewName,
                        Category = category,
                        IsChecked = false
                    });
                }
                root.Add(categoryNode);
            }
            return root;
        }

        public List<string> GetTitleBlockNames()
        {
            var collector = new FilteredElementCollector(_doc);
            var titleBlocks = collector.OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .ToList();

            if (!titleBlocks.Any())
                throw new InvalidOperationException("No title block found in document.");

            return titleBlocks.Select(tb => $"{tb.Family.Name}:{tb.Name}").ToList();
        }

        public void GenerateSheet(List<string> selectedViews, string titleBlockName, string sheetName)
        {
            using (var trans = new Transaction(_doc, "Generate Sheet"))
            {
                trans.Start();

                var titleBlock = FindTitleBlock(titleBlockName);
                if (titleBlock != null && !titleBlock.IsActive)
                    titleBlock.Activate();

                var sheet = ViewSheet.Create(_doc, titleBlock.Id);
                sheet.Name = sheetName;

                var viewsToPlace = GetViewsByName(selectedViews);
                PlaceViewsOnSheet(viewsToPlace, sheet);

                trans.Commit();
            }
        }

        private FamilySymbol FindTitleBlock(string fullName)
        {
            var collector = new FilteredElementCollector(_doc);
            return collector.OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .FirstOrDefault(tb => $"{tb.Family.Name}:{tb.Name}" == fullName);
        }

        private List<View> GetViewsByName(List<string> viewNames)
        {
            var collector = new FilteredElementCollector(_doc);
            return collector.OfClass(typeof(View))
                .Cast<View>()
                .Where(v => viewNames.Contains(v.Name))
                .ToList();
        }

        private void PlaceViewsOnSheet(List<View> views, ViewSheet sheet)
        {
            // 简化布局逻辑：网格排列
            double spacing = 10.0; // feet unit
            double startX = 5.0;
            double startY = 5.0;
            int columns = (int)Math.Ceiling(Math.Sqrt(views.Count));

            for (int i = 0; i < views.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                var location = new XYZ(startX + col * spacing, startY + row * spacing, 0);
                Viewport.Create(_doc, sheet.Id, views[i].Id, location);
            }
        }
    }
    public class ViewTreeNode : INotifyPropertyChanged
    {
        private bool _isChecked;
        public string ViewName { get; set; }
        public string Category { get; set; }
        public ObservableCollection<ViewTreeNode> Children { get; set; } = new ObservableCollection<ViewTreeNode>();

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public bool HasChildren => Children.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
