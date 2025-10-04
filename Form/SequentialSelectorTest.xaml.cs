using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
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
    /// SequentialSelectorTest.xaml 的交互逻辑
    /// </summary>
    public partial class SequentialSelectorTest : Window
    {
        public SequentialSelectorTest(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new MainHostWindowViewModel(uiApp);
        }

        // 确保在主窗口关闭时，清理 UserControl 占用的资源
        private void Window_Closed(object sender, System.EventArgs e)
        {
            MyElementSelector.Dispose();
        }
    }
    public class MainHostWindowViewModel : ObserverableObject
    {
        // 持有 Revit Application 对象，以便通过绑定传递给 UserControl
        public UIApplication RevitApp { get; }

        // 定义一个命令，用于接收和处理 UserControl 返回的结果
        public ICommand ProcessSelectionResultCommand { get; }

        public MainHostWindowViewModel(UIApplication uiApp)
        {
            RevitApp = uiApp;
            ProcessSelectionResultCommand = new BaseBindingCommand(OnSelectionProcessFinished);
        }

        // 这个方法包含了之前在 MainHostWindow.xaml.cs 中的所有逻辑
        private void OnSelectionProcessFinished(object parameter)
        {
            // 参数就是 UserControl 传回来的字典
            if (parameter is Dictionary<int, ElementId> selectedElements)
            {
                if (selectedElements == null || selectedElements.Count == 0)
                {
                    TaskDialog.Show("结果", "没有选择任何构件。");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"选择完成！共计 {selectedElements.Count} 个构件。");
                sb.AppendLine("----------");
                foreach (var kvp in selectedElements)
                {
                    sb.AppendLine($"序号: {kvp.Key}, ElementId: {kvp.Value.IntegerValue}");
                }

                TaskDialog.Show("选择结果 (来自ViewModel)", sb.ToString());
                // 在这里继续执行您的其他业务逻辑...
            }
        }
    }
}
