using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 一个可重用的进度条窗口，用于在 Revit 中显示长时间运行任务的进度。
    /// 实现 IDisposable 接口，以便通过 'using' 语句安全地管理其生命周期。
    /// </summary>
    //public class RevitProgressBar : IDisposable
    //{
    //    private ProgressForm _progressForm;
    //    private int _currentStep = 0;
    //    private string _title;

    //    /// <summary>
    //    /// 初始化进度条。
    //    /// </summary>
    //    /// <param name="uiApp">Revit UI 应用程序对象，用于获取主窗口句柄。</param>
    //    /// <param name="title">进度条窗口的标题。</param>
    //    /// <param name="maxSteps">总步数（例如，要处理的元素总数）。</param>
    //    public RevitProgressBar(UIApplication uiApp, string title, int maxSteps)
    //    {
    //        _title = title;
    //        _progressForm = new ProgressForm(title);
    //        _progressForm.ProgressBar.Minimum = 0;
    //        _progressForm.ProgressBar.Maximum = maxSteps;
    //        _progressForm.ProgressBar.Value = 0;

    //        // 将进度条窗口置于 Revit 主窗口的前面
    //        IntPtr revitHandle = uiApp.MainWindowHandle;
    //        var helper = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow);
    //        helper.Owner = revitHandle;

    //        _progressForm.Show();
    //    }

    //    /// <summary>
    //    /// 使进度条前进一个步长，并更新状态消息。
    //    /// </summary>
    //    /// <param name="statusMessage">显示在进度条下方的当前状态消息。</param>
    //    public void Increment(string statusMessage = "")
    //    {
    //        _currentStep++;
    //        _progressForm.ProgressBar.Value = _currentStep;
    //        _progressForm.StatusLabel.Text = statusMessage;

    //        // 更新标题以显示百分比
    //        int percentage = (_progressForm.ProgressBar.Maximum == 0) ? 100 : (_currentStep * 100 / _progressForm.ProgressBar.Maximum);
    //        _progressForm.Text = $"{_title} ({percentage}%)";

    //        // 必须调用此方法以允许 UI 线程处理事件并重绘窗口
    //        Application.DoEvents();
    //    }

    //    /// <summary>
    //    /// 实现 IDisposable 接口，用于在 'using' 块结束时自动关闭窗口。
    //    /// </summary>
    //    public void Dispose()
    //    {
    //        _progressForm?.Close();
    //        _progressForm?.Dispose();
    //    }
    //}

    /// <summary>
    /// 用于显示进度的简单 Windows 窗体。
    /// </summary>
    //internal class ProgressForm : Form
    //{
    //    public ProgressBar ProgressBar;
    //    public Label StatusLabel;

    //    public ProgressForm(string title)
    //    {
    //        this.Text = title;
    //        this.StartPosition = FormStartPosition.CenterScreen;
    //        this.ControlBox = false; // 隐藏关闭按钮
    //        this.FormBorderStyle = FormBorderStyle.FixedDialog;
    //        this.Size = new System.Drawing.Size(500, 150);

    //        ProgressBar = new ProgressBar();
    //        ProgressBar.Dock = DockStyle.Top;
    //        ProgressBar.Size = new System.Drawing.Size(460, 30);
    //        ProgressBar.Location = new System.Drawing.Point(15, 20);

    //        StatusLabel = new Label();
    //        StatusLabel.Dock = DockStyle.Fill;
    //        StatusLabel.Location = new System.Drawing.Point(15, 60);
    //        StatusLabel.Size = new System.Drawing.Size(460, 30);
    //        StatusLabel.Text = "准备开始...";

    //        this.Controls.Add(ProgressBar);
    //        this.Controls.Add(StatusLabel);
    //    }
    //}
}
