using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// ClipboardCatcher.xaml 的交互逻辑
    /// </summary>
    public partial class ClipboardCatcher : Window
    {
        public ClipboardCatcher(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new TestViewModel(uiApp);
            // 窗体初始化时，刷新一次显示状态，隐藏多余的行
            UpdateRowsVisibility();
        }
        // 记录当前显示了几行（默认显示1行）
        private int _visibleRowCount = 1;
        // 界面最大允许显示的行数
        private readonly int _maxRowCount = 3;
        // 第一行最后一个按钮（加号）点击事件
        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount < _maxRowCount)
            {
                _visibleRowCount++;
                UpdateRowsVisibility();
            }
        }
        // 第二行最后一个按钮（减号）点击事件
        private void BtnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount > 1)
            {
                _visibleRowCount--;
                UpdateRowsVisibility();
            }
        }
        // 核心逻辑：根据当前的行数，自动隐藏/显示控件
        private void UpdateRowsVisibility()
        {
            // 遍历 Grid 里的所有控件 (UniversialSplitButton 和 CircleImageButton)
            foreach (UIElement child in MainGrid.Children)
            {
                // 获取当前控件属于第几行 (0代表第一行, 1代表第二行...)
                int rowIndex = Grid.GetRow(child);

                // 如果控件所在行小于当前允许显示的行数，就显示，否则折叠隐藏
                if (rowIndex < _visibleRowCount)
                {
                    child.Visibility = Visibility.Visible;
                }
                else
                {
                    // Collapsed 会让控件完全消失，且不占位
                    child.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
    public class TestViewModel : ObserverableObject
    {
        public ClipboardSlotViewModel Slot1 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot2 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot3 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot4 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot5 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot6 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot7 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot8 { get; } = new ClipboardSlotViewModel();
        public ClipboardSlotViewModel Slot9 { get; } = new ClipboardSlotViewModel();
        public TestViewModel(UIApplication uiApp)
        {
        }
    }
    //子ViewModel，按钮内部通用逻辑
    public class ClipboardSlotViewModel : ObserverableObject
    {
        private string _mainContent = string.Empty;
        public string MainContent
        {
            get => _mainContent;
            set { _mainContent = value; OnPropertyChanged(); }
        }
        public ClipboardSlotViewModel()
        {
        }
        public ICommand MainActionCommand => new BaseBindingCommand(ExecuteMainAction);
        private void ExecuteMainAction(object parameter)
        {
            if (string.IsNullOrEmpty(MainContent))
            {
                if (Clipboard.ContainsText())
                    MainContent = Clipboard.GetText();
                else
                    MainContent = "非文本对象";
            }
            else
            {
                // 如果已经是“非文本对象”，点击时不应该往剪贴板存这个字符串
                if (MainContent != "非文本对象")
                    Clipboard.SetText(MainContent);
            }
        }
        public ICommand ClearActionCommand => new BaseBindingCommand(ExecuteClearAction);
        private void ExecuteClearAction(object parameter)
        {
            if (!string.IsNullOrEmpty(MainContent)) MainContent = string.Empty;
        }

    }
}
