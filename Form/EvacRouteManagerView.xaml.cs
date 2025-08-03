using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
    /// EvacRouteView.xaml 的交互逻辑
    /// </summary>
    public partial class EvacRouteManagerView : Window
    {
        public EvacRouteManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new EvacRouteManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class EvacRouteManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public Autodesk.Revit.DB.View ActiveView { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public EvacRouteManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
            if (routeSymbols.Count() != 4) TaskDialog.Show("错误", "未找到指定的自适应族");
            QueryELement(null);
        }

        public ICommand PickRoutesCommand => new RelayCommand<AdaptiveRouteEntity>(PickViewRoutes);
        private void PickViewRoutes(AdaptiveRouteEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                if (ActiveView.GetType().Name != "ViewPlan")
                {
                    TaskDialog.Show("tt", "请在平面操作本功能");
                    return;
                }
                Level currentLevel = ActiveView.GenLevel;
                if (currentLevel == null) return;
                string currentLevelName = currentLevel.Name;
                var currentLevelInstances = new List<ElementId>();
                foreach (var item in AllRoutes)
                {
                    if (item.levelName==currentLevelName)
                    {
                        currentLevelInstances.Add(item.Id);
                    }
                } 
                select.SetElementIds(currentLevelInstances);
                TaskDialog.Show("tt", $"选中{currentLevelInstances.Count().ToString()}个对象");
            });
        }
        public ICommand PickRouteCommand => new RelayCommand<AdaptiveRouteEntity>(PickRoute);
        private void PickRoute(AdaptiveRouteEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                var currentLevelInstances = new List<ElementId>();
                currentLevelInstances.Add(entity.Id);
                select.SetElementIds(currentLevelInstances);
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            AllRoutes.Clear();
            var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
            var familyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(e => routeSymbols.Any(s => s.Id == e.Symbol.Id)).ToList();
            foreach (var item in familyInstances)
            {
                string levelName = item.LookupParameter("楼层标高").AsString();
                if (string.IsNullOrEmpty(obj) || levelName.Contains(obj) || levelName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || item.Symbol.Family.Name.Contains(obj))
                {
                    AdaptiveRouteEntity routeEntity = new AdaptiveRouteEntity(item);
                    AllRoutes.Add(routeEntity);
                }
            }
        }
        private ObservableCollection<AdaptiveRouteEntity> allRoutes = new ObservableCollection<AdaptiveRouteEntity>();
        public ObservableCollection<AdaptiveRouteEntity> AllRoutes
        {
            get => allRoutes;
            set => SetProperty(ref allRoutes, value);
        }
    }
}
