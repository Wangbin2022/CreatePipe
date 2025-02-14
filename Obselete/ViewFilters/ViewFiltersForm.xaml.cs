using Autodesk.Revit.UI;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.ViewFilters
{
    /// <summary>
    /// ViewFiltersForm.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFiltersForm : Window
    {
        public ViewFiltersForm(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ViewFilterViewModel(uiApp);


            //Document doc =uiApp.ActiveUIDocument.Document;
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //List<ParameterFilterElement> pfe = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
            //List<string> filters = new List<string>();
            //foreach (var item in pfe)
            //{
            //    filters.Add(item.Name);
            //}
            //lsBox_Filters.ItemsSource = filters;  

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var viewModel = DataContext as ViewFilterViewModel;
            //if (viewModel != null)
            //{
            //    viewModel.OnSelectionChanged(sender, e);
            //}
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                foreach (ViewFilterModel item in e.AddedItems)
                {
                    item.IsSelected = true;
                }
                foreach (ViewFilterModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                }
            }
        }
    }
}
