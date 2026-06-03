using Autodesk.Revit.UI;
using System.Windows;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show("tt", "PASS");
            this.Close();
        }
    }
}
