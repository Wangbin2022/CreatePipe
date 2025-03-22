using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// TabTest.xaml 的交互逻辑
    /// </summary>
    public partial class TabTest : Window
    {
        public TabTest(UIApplication application)
        {
            InitializeComponent();
            this.DataContext = new RPManagerViewModel(application);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
