using Autodesk.Revit.DB;
using CreatePipe.models;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// CableTraySystemTest.xaml 的交互逻辑
    /// </summary>
    public partial class CableTraySystemTest : Window
    {
        public CableTraySystemTest(Document document)
        {
            InitializeComponent();
            this.DataContext = new CableTraySystemViewModel(document);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
