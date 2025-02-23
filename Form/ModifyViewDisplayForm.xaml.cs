using Autodesk.Revit.UI;
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
