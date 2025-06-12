using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// TableTemplateViewHorizon.xaml 的交互逻辑
    /// </summary>
    public partial class TableTemplateViewHorizon : Window
    {
        public TableTemplateViewHorizon(UIApplication application)
        {
            InitializeComponent();
            this.DataContext = new TableTemplateViewModel(application);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
