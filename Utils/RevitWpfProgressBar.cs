using Autodesk.Revit.UI;
using CreatePipe.Utils.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CreatePipe.Utils
{
    public class RevitWpfProgressBar : IDisposable
    {
        private ProgressWindow _progressWindow;
        private int _currentStep = 0;
        private string _title;

        public RevitWpfProgressBar(UIApplication uiApp, string title, int maxSteps)
        {
            _title = title;
            _progressWindow = new ProgressWindow();
            _progressWindow.Title = title;
            _progressWindow.MainProgressBar.Maximum = maxSteps;
            _progressWindow.MainProgressBar.Value = 0;

            // 关键：将 WPF 窗口的所有者设置为 Revit 主窗口
            // 这样它就会显示在 Revit 的最前面
            new WindowInteropHelper(_progressWindow).Owner = uiApp.MainWindowHandle;

            _progressWindow.Show();
        }

        public void Increment(string statusMessage = "")
        {
            _currentStep++;

            // 使用 Dispatcher 在 UI 线程上安全地更新 WPF 控件
            _progressWindow.Dispatcher.Invoke(() =>
            {
                _progressWindow.MainProgressBar.Value = _currentStep;
                _progressWindow.StatusLabel.Text = statusMessage;

                int percentage = (_progressWindow.MainProgressBar.Maximum == 0) ? 100 : (int)((_currentStep / _progressWindow.MainProgressBar.Maximum) * 100);
                _progressWindow.Title = $"{_title} ({percentage}%)";

            }, DispatcherPriority.Background); // 使用后台优先级，允许 Revit 响应
        }

        public void Dispose()
        {
            // 确保窗口在操作结束或异常时关闭
            _progressWindow?.Dispatcher.Invoke(() => _progressWindow.Close());
        }
    }
}
