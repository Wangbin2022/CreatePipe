using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// GuidanaceSignManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class GuidanaceSignManagerView : Window
    {
        public GuidanaceSignManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new GuidanaceSignManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class GuidanaceSignManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public View ActiveView { get; set; }
        public UIApplication uIApp { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public GuidanaceSignManagerViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            uIDoc = application.ActiveUIDocument;
            ActiveView = application.ActiveUIDocument.ActiveView;
            uIApp = application;
            QueryElement(null);
        }


        public ICommand PickElementCommand => new RelayCommand<GuidanceSignEntity>(PickElement);
        private void PickElement(GuidanceSignEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                var currentLevelInstances = new List<ElementId>();
                currentLevelInstances.Add(entity.Id);
                currentLevelInstances.Add(entity.entityId);
                select.SetElementIds(currentLevelInstances);
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        private void QueryElement(string obj)
        {
            _externalHandler.Run(app =>
            {
                AllSigns.Clear();
                var guidanceSigns = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name == "标记_标识").ToList();
                foreach (var sign in guidanceSigns)
                {
                    if (string.IsNullOrEmpty(obj) || sign.TagText.Contains(obj) || sign.TagText.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        allSigns.Add(new GuidanceSignEntity(sign));
                    }
                }
            });
        }
        private ObservableCollection<GuidanceSignEntity> allSigns = new ObservableCollection<GuidanceSignEntity>();
        public ObservableCollection<GuidanceSignEntity> AllSigns
        {
            get => allSigns;
            set => SetProperty(ref allSigns, value);
        }
    }
}
