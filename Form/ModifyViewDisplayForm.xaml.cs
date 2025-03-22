using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// ModifyViewDisplayForm.xaml 的交互逻辑
    /// </summary>
    public partial class ModifyViewDisplayForm : Window
    {
        public ModifyViewDisplayForm(UIApplication app)
        {
            InitializeComponent();
            this.DataContext = new ModifyViewDisplayViewModel(app);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
