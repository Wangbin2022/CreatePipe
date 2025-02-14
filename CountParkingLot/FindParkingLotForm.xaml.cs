using Autodesk.Revit.DB;
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

namespace CreatePipe.CountParkingLot
{
    /// <summary>
    /// FindParkingLotForm.xaml 的交互逻辑
    /// </summary>
    public partial class FindParkingLotForm : Window
    {
        private Document Document;
        private UIApplication Application;
        public FindParkingLotForm(UIApplication uiApp)
        {
            InitializeComponent();
            Document = uiApp.ActiveUIDocument.Document;
            Application = uiApp;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<Element> elems = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).ToList();
            List<FamilyInstance> parkingLots = new List<FamilyInstance>();
            List<ElementId> getIds = new List<ElementId>();
            foreach (Element elem in elems)
            {
                if (elem is FamilyInstance familyInstance)
                {
                    FamilySymbol familySymbol = familyInstance.Symbol;
                    if (familySymbol.Family.Name.Contains("车位"))
                    {
                        parkingLots.Add(familyInstance);
                    }
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                if (i < parkingLots.Count)
                {
                    var item = parkingLots[i];
                    Parameter parkingNumberParam = item.LookupParameter("车位编号");
                    if (parkingNumberParam.HasValue && parkingNumberParam.AsString().Contains(tb_keyword.Text))
                    {
                        stringBuilder.AppendLine($"车位编号“{parkingNumberParam.AsString()}”，ID是{item.Id}");
                        getIds.Add(item.Id);
                    }
                }
            }
            if (getIds.Count > 0)
            {
                TaskDialog.Show("tt", stringBuilder.ToString());
                Application.ActiveUIDocument.Selection.SetElementIds(getIds);
            }
            else
            {
                TaskDialog.Show("提示", "未找到符合条件的车位");
            }
            this.Close();
        }
    }
}
