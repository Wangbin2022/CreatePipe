using Autodesk.Revit.UI;
using CreatePipe.cmd;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// CategoryVisibilityView.xaml 的交互逻辑
    /// </summary>
    public partial class CategoryVisibilityView : Window
    {
        public CategoryVisibilityView(UIApplication uiApp)
        {
            InitializeComponent();
            this.Content=new CategoryVisibilityViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class CategoryVisibilityViewModel : ObserverableObject
    {
        public CategoryVisibilityViewModel(UIApplication uiApp)
        {
            
        }
    }
}
