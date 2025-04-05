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
    /// ViewTemplateManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewTemplateManagerView : Window
    {
        public ViewTemplateManagerView(UIApplication application)
        {
            InitializeComponent();
            this.DataContext = new ViewTemplateManagerViewModel(application);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                foreach (ViewTemplateManagerViewModel item in e.AddedItems)
                {
                    item.IsSelected = true;
                }
                foreach (ViewTemplateManagerViewModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                }
            }
        }
    }
}
