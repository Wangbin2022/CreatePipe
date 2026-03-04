using System;
using System.Windows;

namespace CreatePipe.RevitStylePopup
{
    /// <summary>
    /// ToastWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ToastWindow : Window
    {
        public ToastWindow(string title, string message)
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
}
