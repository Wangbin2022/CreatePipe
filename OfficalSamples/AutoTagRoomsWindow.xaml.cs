using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AutoTagRoomsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AutoTagRoomsWindow : Window
    {
        public AutoTagRoomsWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 自动房间标记视图模型
    /// </summary>
    public class AutoTagRoomsViewModel : ObserverableObject
    {
        private readonly RoomTaggingService _service;

        private LevelInfo _selectedLevel;
        private RoomTagType _selectedTagType;
        private ObservableCollection<LevelInfo> _levels;
        private ObservableCollection<RoomTagType> _tagTypes;
        private ObservableCollection<RoomInfo> _rooms;
        private RoomInfo _selectedRoom;
        private bool _isProcessing;
        private string _statusMessage;
        private string _taggingResult;

        public AutoTagRoomsViewModel(ExternalCommandData commandData)
        {
            _service = new RoomTaggingService(commandData);

            // 初始化命令
            LoadDataCommand = new BaseBindingCommand(_ => LoadData());
            AutoTagCommand = new BaseBindingCommand(_ => AutoTagRooms(), _ => CanAutoTag);
            SelectAllCommand = new BaseBindingCommand(_ => SelectAllRooms());
            DeselectAllCommand = new BaseBindingCommand(_ => DeselectAllRooms());
            RefreshCommand = new BaseBindingCommand(_ => RefreshRooms());
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 加载数据
            LoadData();
        }

        /// <summary>
        /// 选中的楼层
        /// </summary>
        public LevelInfo SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                OnPropertyChanged();
                if (value != null)
                    LoadRoomsForLevel(value.Level);
                OnPropertyChanged(nameof(CanAutoTag));
            }
        }

        /// <summary>
        /// 选中的标记类型
        /// </summary>
        public RoomTagType SelectedTagType
        {
            get => _selectedTagType;
            set
            {
                _selectedTagType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAutoTag));
            }
        }

        /// <summary>
        /// 楼层列表
        /// </summary>
        public ObservableCollection<LevelInfo> Levels
        {
            get => _levels;
            set { _levels = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 标记类型列表
        /// </summary>
        public ObservableCollection<RoomTagType> TagTypes
        {
            get => _tagTypes;
            set { _tagTypes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 房间列表
        /// </summary>
        public ObservableCollection<RoomInfo> Rooms
        {
            get => _rooms;
            set { _rooms = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的房间
        /// </summary>
        public RoomInfo SelectedRoom
        {
            get => _selectedRoom;
            set { _selectedRoom = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAutoTag)); }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 标记结果
        /// </summary>
        public string TaggingResult
        {
            get => _taggingResult;
            set { _taggingResult = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否可以自动标记
        /// </summary>
        public bool CanAutoTag => !IsProcessing && SelectedLevel != null && SelectedTagType != null;

        public ICommand LoadDataCommand { get; }
        public ICommand AutoTagCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            IsProcessing = true;
            StatusMessage = "正在加载数据...";

            try
            {
                // 加载楼层列表
                Levels = new ObservableCollection<LevelInfo>(_service.GetLevelsWithRooms());

                // 加载标记类型
                TagTypes = new ObservableCollection<RoomTagType>(_service.GetRoomTagTypes());

                if (TagTypes.Any())
                    SelectedTagType = TagTypes.First();

                StatusMessage = $"加载完成，共 {Levels.Count} 个楼层，{TagTypes.Count} 种标记类型";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 加载指定楼层的房间
        /// </summary>
        private void LoadRoomsForLevel(Level level)
        {
            try
            {
                var rooms = _service.GetRoomsByLevel(level);
                Rooms = new ObservableCollection<RoomInfo>(rooms);

                // 默认全选未标记的房间
                foreach (var room in Rooms)
                {
                    room.IsChecked = !room.HasTag;
                }

                StatusMessage = $"楼层 {level.Name} 共有 {rooms.Count} 个房间，" +
                               $"其中 {rooms.Count(r => r.HasTag)} 个已标记";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载房间失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 刷新房间列表
        /// </summary>
        private void RefreshRooms()
        {
            if (SelectedLevel != null)
                LoadRoomsForLevel(SelectedLevel.Level);
        }

        /// <summary>
        /// 全选房间
        /// </summary>
        private void SelectAllRooms()
        {
            if (Rooms != null)
            {
                foreach (var room in Rooms)
                    room.IsChecked = true;
            }
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        private void DeselectAllRooms()
        {
            if (Rooms != null)
            {
                foreach (var room in Rooms)
                    room.IsChecked = false;
            }
        }

        /// <summary>
        /// 自动标记房间
        /// </summary>
        private void AutoTagRooms()
        {
            IsProcessing = true;
            StatusMessage = "正在添加房间标记...";

            try
            {
                var roomsToTag = Rooms.Where(r => r.IsChecked).ToList();

                if (!roomsToTag.Any())
                {
                    StatusMessage = "没有选中需要标记的房间";
                    return;
                }

                var result = _service.AutoTagRooms(SelectedLevel.Level, SelectedTagType, roomsToTag);

                TaggingResult = result.Summary;
                StatusMessage = result.IsSuccess ? "标记添加成功" : "标记添加完成，存在错误";

                // 刷新房间列表
                RefreshRooms();
            }
            catch (Exception ex)
            {
                StatusMessage = $"标记失败: {ex.Message}";
                TaggingResult = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
    public partial class RoomInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _number;
        private double _area;
        private int _tagCount;
        private Room _room;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); }
        }

        public double Area
        {
            get => _area;
            set { _area = value; OnPropertyChanged(); }
        }

        public int TagCount
        {
            get => _tagCount;
            set { _tagCount = value; OnPropertyChanged(); }
        }

        public Room Room
        {
            get => _room;
            set { _room = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否有标记
        /// </summary>
        public bool HasTag => TagCount > 0;

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{Number} - {Name} (面积: {Area:F2}m², 标记数: {TagCount})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    // 扩展RoomInfo以支持选中状态
    public partial class RoomInfo:ObserverableObject
    {
        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }
    }
    /// <summary>
    /// 房间标记服务类
    /// </summary>
    public class RoomTaggingService
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;

        public RoomTaggingService(ExternalCommandData commandData)
        {
            _uiDoc = commandData.Application.ActiveUIDocument;
            _doc = _uiDoc.Document;
        }

        /// <summary>
        /// 获取所有有房间的楼层
        /// </summary>
        public List<LevelInfo> GetLevelsWithRooms()
        {
            var levelInfos = new List<LevelInfo>();
            var roomDict = new Dictionary<Level, List<Room>>();

            // 通过PlanTopology获取房间信息
            foreach (PlanTopology planTopology in _doc.PlanTopologies)
            {
                if (planTopology.Level == null) continue;

                var roomIds = planTopology.GetRoomIds();
                if (roomIds.Count == 0) continue;

                var rooms = new List<Room>();
                var taggedCount = 0;

                foreach (ElementId eid in roomIds)
                {
                    var room = _doc.GetElement(eid) as Room;
                    if (room == null) continue;

                    rooms.Add(room);

                    // 检查是否有房间标记
                    if (GetRoomTags(room).Any())
                        taggedCount++;
                }

                levelInfos.Add(new LevelInfo
                {
                    Level = planTopology.Level,
                    RoomCount = rooms.Count,
                    TaggedRoomCount = taggedCount
                });
            }

            return levelInfos.OrderBy(l => l.Level?.Elevation).ToList();
        }

        /// <summary>
        /// 获取指定楼层的房间列表
        /// </summary>
        public List<RoomInfo> GetRoomsByLevel(Level level)
        {
            var rooms = new List<RoomInfo>();
            var planTopology = _doc.get_PlanTopology(level);

            if (planTopology == null) return rooms;

            foreach (ElementId eid in planTopology.GetRoomIds())
            {
                var room = _doc.GetElement(eid) as Room;
                if (room == null) continue;

                var tags = GetRoomTags(room);
                var roomInfo = new RoomInfo
                {
                    Id = room.Id.IntegerValue,
                    Name = room.Name ?? "未命名",
                    Number = room.Number ?? "",
                    Area = room.Area,
                    TagCount = tags.Count,
                    Room = room
                };

                rooms.Add(roomInfo);
            }

            return rooms.OrderBy(r => r.Number).ToList();
        }

        /// <summary>
        /// 获取房间的标记
        /// </summary>
        public List<RoomTag> GetRoomTags(Room room)
        {
            // 查找该房间的所有标记
            var collector = new FilteredElementCollector(_doc);
            var roomTags = collector.OfClass(typeof(RoomTag))
                .Cast<RoomTag>()
                .Where(tag => tag.Room != null && tag.Room.Id == room.Id)
                .ToList();

            return roomTags;
        }

        /// <summary>
        /// 获取所有房间标记类型
        /// </summary>
        public List<RoomTagType> GetRoomTagTypes()
        {
            var collector = new FilteredElementCollector(_doc);
            return collector.OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .Cast<RoomTagType>()
                .ToList();
        }

        /// <summary>
        /// 自动标记房间
        /// </summary>
        public RoomTaggingResult AutoTagRooms(Level level, RoomTagType tagType, List<RoomInfo> roomsToTag)
        {
            var result = new RoomTaggingResult();
            var roomsToProcess = roomsToTag.Where(r => !r.HasTag).ToList();

            result.TotalRooms = roomsToTag.Count;
            result.AlreadyTagged = roomsToTag.Count - roomsToProcess.Count;

            using (var subTrans = new SubTransaction(_doc))
            {
                subTrans.Start();

                foreach (var roomInfo in roomsToProcess)
                {
                    try
                    {
                        var room = roomInfo.Room;
                        var locationPoint = room.Location as LocationPoint;

                        if (locationPoint == null) continue;

                        var point = new UV(locationPoint.Point.X, locationPoint.Point.Y);

                        var newTag = _doc.Create.NewRoomTag(
                            new LinkElementId(room.Id),
                            point,
                            null);

                        newTag.RoomTagType = tagType;

                        result.SuccessCount++;
                        roomInfo.TagCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"房间 {roomInfo.Number} 标记失败: {ex.Message}");
                    }
                }

                subTrans.Commit();
            }

            result.IsSuccess = result.FailedCount == 0;
            return result;
        }
    }

    /// <summary>
    /// 房间标记结果
    /// </summary>
    public class RoomTaggingResult
    {
        public bool IsSuccess { get; set; }
        public int TotalRooms { get; set; }
        public int AlreadyTagged { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string Summary => $"总计: {TotalRooms}, 已有标记: {AlreadyTagged}, " +
                                 $"成功添加: {SuccessCount}, 失败: {FailedCount}";
    }
    /// <summary>
    /// 楼层信息模型
    /// </summary>
    public class LevelInfo
    {
        public Level Level { get; set; }
        public string Name => Level?.Name ?? "未知";
        public int RoomCount { get; set; }
        public int TaggedRoomCount { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{Name} (房间: {RoomCount}, 已标记: {TaggedRoomCount})";
    }
    /// <summary>
    /// 房间信息模型
    /// </summary>


}
