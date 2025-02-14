using Autodesk.Revit.DB;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// test.xaml 的交互逻辑
    /// </summary>
    public partial class FilterTest : Window
    {
        public FilterTest(Document doc)
        {
            InitializeComponent();
            //this.DataContext = new ViewModel1127(doc);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}
    }
}
