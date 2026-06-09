using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public PartTypeSelectorViewModel ViewModel { get; }
        public TestWindow(UIApplication uiApp)
        {
            InitializeComponent();
            //this.DataContext = new TestWindowViewModel(uiApp);
            ViewModel = new PartTypeSelectorViewModel();
            this.DataContext = ViewModel;
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show("tt", ViewModel.SelectedPartType.ToString());
            this.Close();
        }
    }
    public class PartTypeSelectorViewModel : ObserverableObject
    {
        // 核心数据源：类别名称 -> 对应的零件类型集合
        private readonly Dictionary<string, List<PartType>> _categoryToPartTypesMap;

        public PartTypeSelectorViewModel()
        {
            // 1. 初始化 Revit 类别与对应 PartType 的字典映射 (按需增减)
            _categoryToPartTypesMap = new Dictionary<string, List<PartType>>
        {
            { "管件 (Pipe Fittings)", new List<PartType> { PartType.Normal, PartType.Elbow, PartType.Tee, PartType.Cross, PartType.Transition, PartType.Union, PartType.Cap } },
            { "管道附件 (Pipe Accessories)", new List<PartType> { PartType.Normal, PartType.ValveNormal, PartType.InlineSensor } },
            { "风管管件 (Duct Fittings)", new List<PartType> { PartType.Normal, PartType.Elbow, PartType.Tee, PartType.Cross, PartType.Transition } },
            { "桥架配件 (Cable Tray Fittings)", new List<PartType> { PartType.ChannelCableTrayCross, PartType.ChannelCableTrayMultiPort, PartType.ChannelCableTrayOffset, PartType.ChannelCableTrayTransition, PartType.LadderCableTrayUnion } }
        };

            //    // 2. 初始化时默认选中第一个类别
            SelectedCategory = CategoryList.FirstOrDefault();
        }

        // 第一个 ComboBox 的数据源（提取字典的所有 Key）
        public List<string> CategoryList => _categoryToPartTypesMap.Keys.ToList();
        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;

                    // 添加空值检查和 Key 存在性检查
                    if (!string.IsNullOrEmpty(_selectedCategory) && _categoryToPartTypesMap.ContainsKey(_selectedCategory))
                    {
                        AvailablePartTypes = _categoryToPartTypesMap[_selectedCategory];
                        SelectedPartType = AvailablePartTypes.FirstOrDefault();
                    }
                    else
                    {
                        AvailablePartTypes = null;
                        SelectedPartType = PartType.Normal; // 或默认值
                    }

                    // 触发属性变更通知
                    OnPropertyChanged();
                }
            }
        }
        //// 选中的类别
        //private string _selectedCategory;
        //public string SelectedCategory
        //{
        //    get => _selectedCategory;
        //    set
        //    {
        //        // 如果类别发生了变化
        //        if (_selectedCategory != value)
        //        {
        //            // 联动1：刷新第二个 ComboBox 的数据源
        //            AvailablePartTypes = _categoryToPartTypesMap[_selectedCategory];

        //            // 联动2：默认选中刷新后的第一个零件类型
        //            SelectedPartType = AvailablePartTypes.FirstOrDefault();
        //        }
        //    }
        //}

        // 第二个 ComboBox 的数据源
        private List<PartType> _availablePartTypes;
        public List<PartType> AvailablePartTypes
        {
            get => _availablePartTypes;
            set => SetProperty(ref _availablePartTypes, value);
        }

        // 最终选中的零件类型 (供 Revit API 调用的结果)
        private PartType _selectedPartType;
        public PartType SelectedPartType
        {
            get => _selectedPartType;
            set => SetProperty(ref _selectedPartType, value);
        }
    }
    public class TestWindowViewModel : ObserverableObject
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Document _doc;
        public TestWindowViewModel(UIApplication uiApp)
        {
            _uiapp = uiApp;
            _uidoc = uiApp.ActiveUIDocument;
            _doc = _uidoc.Document;

        }
        public string D { get; set; } = "0";
        public ICommand SaveConfigCommand => new BaseBindingCommand(SaveConfig);
        private void SaveConfig(object obj)
        {
        }
    }
}
