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
            //rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>()
            //    .Where(room => room.Area > 0).ToList();
            //if (rooms.Count() == 0)
            //{
            //    TaskDialog.Show("tt", "当前模型中没有找到房间");
            //    return;
            //}
            rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType().Cast<Room>().ToList();
            PrecacheRoomData(Document);
            QueryELement(string.Empty);
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
            //List<RoomSingleEntity> vte = rooms.Select(v => new RoomSingleEntity(v))
            //    .Where(e => string.IsNullOrEmpty(obj) || e.roomName.Contains(obj) || e.roomName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || e.roomNumber.Contains(obj)).ToList();
            //foreach (var item in vte)
            //{
            //    AllRooms.Add(item);
            //}
            //  优化核心：先对轻量的 Room 对象进行过滤 
            var filteredRooms = rooms.Where(r => r.IsValidObject && r.Location != null).Where(r => string.IsNullOrEmpty(obj) ||
            (r.Name != null && r.Name.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (r.Number != null && r.Number.Contains(obj))).ToList();

            // 只为过滤后的结果创建 RoomSingleEntity 对象
            foreach (var room in filteredRooms)
            {
                // 从缓存中获取这个房间的门和窗
                List<FamilyInstance> doorsForThisRoom = _doorsByRoomId.ContainsKey(room.Id) ? _doorsByRoomId[room.Id] : new List<FamilyInstance>();
                List<FamilyInstance> windowsForThisRoom = _windowsByRoomId.ContainsKey(room.Id) ? _windowsByRoomId[room.Id] : new List<FamilyInstance>();
                // 创建对象时传入缓存数据
                var entity = new RoomSingleEntity(room, doorsForThisRoom, windowsForThisRoom);
                AllRooms.Add(entity);
            }
        }
        /// <summary>
        /// 一次性收集所有门窗，并按房间ID进行分组缓存。
        /// 这是性能优化的核心。
        /// </summary>
        private void PrecacheRoomData(Document doc)
        {
            _doorsByRoomId = new Dictionary<ElementId, List<FamilyInstance>>();
            _windowsByRoomId = new Dictionary<ElementId, List<FamilyInstance>>();
            // 一次性获取文档中所有的门
            var allDoors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            foreach (var door in allDoors)
            {
                // 处理 ToRoom
                if (door.ToRoom != null)
                {
                    if (!_doorsByRoomId.ContainsKey(door.ToRoom.Id))
                        _doorsByRoomId[door.ToRoom.Id] = new List<FamilyInstance>();
                    _doorsByRoomId[door.ToRoom.Id].Add(door);
                }
                // 处理 FromRoom
                if (door.FromRoom != null)
                {
                    if (!_doorsByRoomId.ContainsKey(door.FromRoom.Id))
                        _doorsByRoomId[door.FromRoom.Id] = new List<FamilyInstance>();
                    // 防止重复添加（门可能同时是两个房间的From/To）
                    if (!_doorsByRoomId[door.FromRoom.Id].Contains(door))
                        _doorsByRoomId[door.FromRoom.Id].Add(door);
                }
            }
            // 一次性获取文档中所有的窗
            var allWindows = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            foreach (var window in allWindows)
            {
                // 窗户通常只有一个 ToRoom 或 FromRoom
                var room = window.ToRoom ?? window.FromRoom;
                if (room != null)
                {
                    if (!_windowsByRoomId.ContainsKey(room.Id))
                        _windowsByRoomId[room.Id] = new List<FamilyInstance>();
                    _windowsByRoomId[room.Id].Add(window);
                }
            }
        }
        private Dictionary<ElementId, List<FamilyInstance>> _doorsByRoomId;
        private Dictionary<ElementId, List<FamilyInstance>> _windowsByRoomId;
        public List<Room> rooms = new List<Room>();
        private ObservableCollection<RoomSingleEntity> allRooms = new ObservableCollection<RoomSingleEntity>();
        public ObservableCollection<RoomSingleEntity> AllRooms
        {
            get => allRooms;
            set => SetProperty(ref allRooms, value);
        }
    }
}
