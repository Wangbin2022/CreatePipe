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

namespace CreatePipe
{
    /// <summary>
    /// BasePointMover.xaml 的交互逻辑
    /// </summary>
    public partial class BasePointMover : Window
    {
        public BasePointMover()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
