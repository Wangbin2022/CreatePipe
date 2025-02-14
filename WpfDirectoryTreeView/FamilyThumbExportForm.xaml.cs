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

namespace CreatePipe.WpfDirectoryTreeView
{
    /// <summary>
    /// FamilyThumbExportForm.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyThumbExportForm : Window
    {
        UIApplication application;
        Dirs Dirs;
        public FamilyThumbExportForm(UIApplication uiapp, Dirs dirs)
        {
            InitializeComponent();
            this.DataContext = new FamilyThumbExportViewModel(uiapp, dirs);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void numberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text) || !int.TryParse(textBox.Text, out _))
            {
                textBox.Text = "600";
            }
        }
    }
}
