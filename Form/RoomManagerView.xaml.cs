using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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

        public ICommand PlaceRoomNameCommand => new RelayCommand<RoomSingleEntity>(PlaceRoomName);
        private void PlaceRoomName(RoomSingleEntity entity)
        {
            XYZ boundCenter = GetElementCenter(entity.Room);
            LocationPoint locPt = (LocationPoint)entity.Room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            //TaskDialog.Show("tt", roomCenter.X.ToString()+"\n"+roomCenter.Y.ToString());
            // 1. 先查“三维文字”族是否存在
            Family targetFamily = new FilteredElementCollector(Document).OfClass(typeof(Family)).Cast<Family>()
                .FirstOrDefault(f => f.Name.Equals("三维文字", StringComparison.OrdinalIgnoreCase));
            if (targetFamily == null)
            {
                TaskDialog.Show("提示", "项目中未找到三维文字族"); return;
            }
            FamilySymbol selectSymbol = Document.GetElement(targetFamily.GetFamilySymbolIds().First()) as FamilySymbol;
            _externalHandler.Run(app =>
            {
                Document.NewTransaction(() =>
            {
                if (!selectSymbol.IsActive)
                {
                    selectSymbol.Activate();
                    Document.Regenerate();
                }
                try
                {
                    FamilyInstance instance = Document.Create.NewFamilyInstance(roomCenter, selectSymbol, StructuralType.NonStructural);
                    instance.LookupParameter("文字内容").Set(entity.roomName);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", ex.Message);
                }
            }, "创建三维文字");
            });
        }
        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
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
