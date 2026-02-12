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
