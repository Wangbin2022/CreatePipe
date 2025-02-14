using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// ProgressBarTest1.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarTest1 : Window
    {
        public ProgressBarTest1()
        {
            InitializeComponent();
        }

        private async void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var target = sender as ProgressBar;
            target.Value++;
            TaskbarItemInfo.ProgressValue = target.Value / target.Maximum;
            //<TaskbarItemInfo ProgressState="Normal"/>
            //上两句实现进度条显示在任务栏上，但Win11似乎无法实现一直滚动的效果？

            await Task.Delay(100);
            if (target.Value < target.Maximum)
            {
                ProgressBar_MouseLeftButtonDown(target, null);
            }
        }
    }
}
