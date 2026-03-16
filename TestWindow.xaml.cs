using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        // 记录当前显示了几行（默认显示1行）
        private int _visibleRowCount = 1;
        // 界面最大允许显示的行数
        private readonly int _maxRowCount = 3;
        public TestWindow()
        {
            InitializeComponent();
            this.DataContext = new TestViewModel();
            // 窗体初始化时，刷新一次显示状态，隐藏多余的行
            UpdateRowsVisibility();
        }
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
        private void CircleImageButton_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show("tt","这是一个提示信息！");
            this.Close();
        }

        public class TestViewModel
        {
            public TestViewModel()
            {

            }
            public ICommand SaveCommand => new BaseBindingCommand(ExecuteSave);
            private void ExecuteSave(object parameter)
            {
                System.Windows.MessageBox.Show("保存按钮被点击", "提示");
            }
            public ICommand SaveOptionsCommand => new BaseBindingCommand(ExecuteSaveOptions);
            private void ExecuteSaveOptions(object parameter)
            {
                System.Windows.MessageBox.Show("保存选项被点击", "提示");
            }
            public ICommand OpenCommand => new BaseBindingCommand(ExecuteOpen);
            private void ExecuteOpen(object parameter)
            {
                System.Windows.MessageBox.Show("打开按钮被点击", "提示");
            }
            public ICommand OpenOptionsCommand => new BaseBindingCommand(ExecuteOpenOptions);
            private void ExecuteOpenOptions(object parameter)
            {
                System.Windows.MessageBox.Show("打开选项被点击", "提示");
            }
            public ICommand RefreshCommand => new BaseBindingCommand(ExecuteRefresh);
            private void ExecuteRefresh(object parameter)
            {
                //System.Windows.MessageBox.Show("刷新按钮被点击", "提示");
                TaskDialog.Show("tt", "刷新按钮被点击");
            }
            public ICommand RefreshOptionsCommand => new BaseBindingCommand(ExecuteRefreshOptions);
            private void ExecuteRefreshOptions(object parameter)
            {
                System.Windows.MessageBox.Show("刷新选项被点击", "提示");
            }
            public ICommand ConfirmCommand => new BaseBindingCommand(ExecuteConfirm);
            private void ExecuteConfirm(object parameter)
            {
                System.Windows.MessageBox.Show("确认按钮被点击", "提示");
            }
        }
    }
}
