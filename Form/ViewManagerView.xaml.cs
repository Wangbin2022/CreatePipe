using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// ViewManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewManagerView : Window
    {
        public ViewManagerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new ViewManagerViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
