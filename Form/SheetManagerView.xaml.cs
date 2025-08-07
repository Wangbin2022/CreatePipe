using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// SheetManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class SheetManagerView : Window
    {
        public SheetManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new SheetManagerViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SheetManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public Autodesk.Revit.DB.View ActiveView { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public SheetManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;

            List<ViewSheet> sheets = new FilteredElementCollector(Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            if (sheets.Count() == 0)
            {
                TaskDialog.Show("tt", "当前模型中没有找到图纸");
                return;
            }
            QueryELement(null);
        }

        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
        private void DeleteELements(IEnumerable<object> selectedElements)
        {
            _externalHandler.Run(app =>
                {
                    Document.NewTransaction(() =>
                    {
                        List<ElementId> toRemove = new List<ElementId>();
                        List<SheetEntity> selectedItems = selectedElements.Cast<SheetEntity>().ToList();
                        if (selectedItems == null) return;
                        foreach (var item in selectedItems)
                        {
                            if (item.Id == ActiveView.Id) return;
                            toRemove.Add(item.Id);
                            AllSheets.Remove(item);
                        }
                        Document.Delete(toRemove);
                    }, "删除多个视图");
                });
        } 
        public ICommand FindViewCommand => new RelayCommand<SheetEntity>(ActivateView);
        private void ActivateView(SheetEntity entity)
        {
            View view = Document.GetElement(entity.Id) as View;
            if (view != null) uIDoc.ActiveView = view;
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            AllSheets.Clear();
            List<ViewSheet> sheets = new FilteredElementCollector(Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            foreach (var item in sheets)
            {
                string sheetName = item.Name;
                if (string.IsNullOrEmpty(obj) || sheetName.Contains(obj) || sheetName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || item.SheetNumber.Contains(obj))
                {
                    SheetEntity sheetEntity = new SheetEntity(item);
                    AllSheets.Add(sheetEntity);
                }
            }
        }
        private ObservableCollection<SheetEntity> allSheets = new ObservableCollection<SheetEntity>();
        public ObservableCollection<SheetEntity> AllSheets
        {
            get => allSheets;
            set => SetProperty(ref allSheets, value);
        }
    }
}
