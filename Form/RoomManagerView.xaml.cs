using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// RoomManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class RoomManagerView : Window
    {
        public RoomManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new RoomManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class RoomManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public Autodesk.Revit.DB.View ActiveView { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public RoomManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>()
                .Where(room => room.Area > 0).ToList();
            if (rooms.Count() == 0)
            {
                TaskDialog.Show("tt", "当前模型中没有找到房间");
                return;
            }
            QueryELement(null);
        }



        public ICommand GotoRoomCommand => new RelayCommand<RoomSingleEntity>(GotoRoom);
        private void GotoRoom(RoomSingleEntity entity)
        {
            ElementId roomId = entity.Id;
            uIDoc.Selection.SetElementIds(new List<ElementId> { roomId });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            AllRooms.Clear();
            List<RoomSingleEntity> vte = rooms.Select(v => new RoomSingleEntity(v))
                .Where(e => string.IsNullOrEmpty(obj) || e.roomName.Contains(obj) || e.roomName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            foreach (var item in vte)
            {
                AllRooms.Add(item);
            }
        }
        public List<Room> rooms = new List<Room>();
        private ObservableCollection<RoomSingleEntity> allRooms = new ObservableCollection<RoomSingleEntity>();
        public ObservableCollection<RoomSingleEntity> AllRooms
        {
            get => allRooms;
            set => SetProperty(ref allRooms, value);
        }
    }
}
