using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// TableTemplateView.xaml 的交互逻辑
    /// </summary>
    public partial class TableTemplateView : Window
    {
        public TableTemplateView(UIApplication application)
        {
            InitializeComponent();
            this.DataContext = new TableTemplateViewModel(application);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
