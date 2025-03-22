using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Windows;

namespace CreatePipe.CountParkingLot
{
    /// <summary>
    /// CountParkingLotForm.xaml 的交互逻辑
    /// </summary>
    public partial class CountParkingLotForm : Window
    {
        public CountParkingLotForm(UIApplication uiApp, IList<ElementId> references)
        {
            InitializeComponent();
            this.DataContext = new CountParkingLotViewModel(uiApp, references);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
