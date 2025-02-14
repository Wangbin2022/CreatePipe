using Autodesk.Revit.UI;
using CreatePipe.models;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form
{
    /// <summary>
    /// BatchFamilyExport_WPF.xaml 的交互逻辑
    /// </summary>
    public partial class BatchFamilyExport_WPF : Window
    {
        public BatchFamilyExport_WPF(UIApplication uiapp)
        {
            InitializeComponent();
            this.DataContext = new BatchFamilyExport_ViewModel(uiapp);
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
