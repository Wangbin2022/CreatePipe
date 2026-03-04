using System.Windows;

namespace CreatePipe
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }
        //private void ShowToast(string message)
        //{
        //    var toast = new ToastWindow(message)
        //    {
        //        Owner = this,
        //        WindowStartupLocation = WindowStartupLocation.CenterScreen
        //        //WindowStartupLocation = WindowStartupLocation.Manual
        //    };

        //    //// 设置窗口位置
        //    //toast.Left = this.Left + (this.Width - toast.Width) / 2;
        //    //toast.Top = this.Top + this.Height - toast.Height - 50;

        //    toast.Show();
        //}
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    ShowToast("这是一个提示信息！");
        //}
    }
}
