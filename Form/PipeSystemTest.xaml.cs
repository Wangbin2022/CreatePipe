using Autodesk.Revit.DB;
using CreatePipe.models;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// PipeSystemTest.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSystemTest : Window
    {
        public PipeSystemTest(Document document)
        {
            InitializeComponent();
            this.DataContext = new PipeSystemViewModel(document);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
