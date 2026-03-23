using System;
using System.Windows;

namespace CreatePipe.Form.RevitStylePopup
{
    /// <summary>
    /// ToastWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RevitStylePopupView : Window
    {
        public RevitStylePopupView(string title, string message)
        {
            InitializeComponent();
            MessageTitle.Text = title;
            MessageTextBlock.Text = message;
        }
        private void CloseWindow(object sender, EventArgs e)
        {
            this.Close();
        }
    }
    // 1. 创建一个辅助类，例如 ToastManager.cs
    public static class RevitStylePopup
    {
        public static void Show(string title, string message)
        {
            // 可以在这里添加一些线程安全检查，确保在UI线程上创建和显示窗口
            if (Application.Current != null && Application.Current.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var toast = new RevitStylePopupView(title, message);
                    toast.Show();
                });
            }
            else
            {
                var toast = new RevitStylePopupView(title, message);
                toast.Show();
            }
        }

        //// 可以添加更多重载，例如只显示消息，使用默认标题
        //public static void ShowToast(string message)
        //{
        //    ShowToast("提示", message); // 默认标题
        //}
        //// 如果ToastWindow支持，可以添加显示特定类型图标的重载
        //// public static void ShowToast(string title, string message, ToastType type) { ... }
    }

}
