using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    /// RoomManager.xaml 的交互逻辑
    /// </summary>
    public partial class RoomManagerOffView : Window
    {
        public RoomManagerOffView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 主窗口ViewModel - 管理房间数据和部门统计
    /// </summary>
    public class RoomManagerOffViewModel : ObserverableObject
    {
        private readonly RoomsData _roomsData;
        private ObservableCollection<RoomItemViewModel> _rooms;
        private ObservableCollection<DepartmentItemViewModel> _departments;
        private bool _isProcessing;

        public ObservableCollection<RoomItemViewModel> Rooms
        {
            get => _rooms;
            set { _rooms = value; OnPropertyChanged(); }
        }

        public ObservableCollection<DepartmentItemViewModel> Departments
        {
            get => _departments;
            set { _departments = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand AddTagsCommand { get; }
        public ICommand ReorderCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CloseCommand { get; }

        public RoomManagerOffViewModel(ExternalCommandData commandData)
        {
            _roomsData = new RoomsData(commandData);
            LoadData();

            AddTagsCommand = new BaseBindingCommand(_ => ExecuteAddTags(), _ => CanExecute);
            ReorderCommand = new BaseBindingCommand(_ => ExecuteReorder(), _ => CanExecute);
            ExportCommand = new BaseBindingCommand(_ => ExecuteExport(), _ => CanExecute);
            CloseCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void LoadData()
        {
            // 加载房间列表
            var roomsWithTag = _roomsData.RoomsWithTag.Select(r => r.Id.IntegerValue).ToHashSet();

            Rooms = new ObservableCollection<RoomItemViewModel>(
                _roomsData.Rooms.Select(r => new RoomItemViewModel(r, roomsWithTag.Contains(r.Id.IntegerValue)))
            );

            // 加载部门统计
            Departments = new ObservableCollection<DepartmentItemViewModel>(
                _roomsData.DepartmentInfos.Select(d => new DepartmentItemViewModel(
                    d.DepartmentName, d.RoomsAmount, d.DepartmentAreaValue))
            );
        }

        private void ExecuteAddTags()
        {
            IsProcessing = true;
            try
            {
                _roomsData.CreateTags();
                RefreshData();
                TaskDialog.Show("成功", "房间标记添加完成！");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteReorder()
        {
            IsProcessing = true;
            try
            {
                _roomsData.ReorderRooms();
                RefreshData();
                TaskDialog.Show("成功", "房间编号重新排序完成！");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteExport()
        {
            IsProcessing = true;
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = ".csv",
                    FileName = $"{_roomsData.ProjectTitle}_房间统计.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _roomsData.ExportFile(saveDialog.FileName);
                    TaskDialog.Show("成功", $"数据已导出到：{saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"导出失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void RefreshData()
        {
            _roomsData.RefreshData();
            LoadData();
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 房间数据管理类 - 收集房间信息、管理标记、统计部门
    /// </summary>
    public partial class RoomsData
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;
        private List<Room> _rooms = new List<Room>();
        private List<RoomTag> _roomTags = new List<RoomTag>();
        private List<DepartmentInfo> _departmentInfos = new List<DepartmentInfo>();

        public IReadOnlyList<Room> Rooms => _rooms.AsReadOnly();
        public IReadOnlyList<RoomTag> RoomTags => _roomTags.AsReadOnly();
        public IReadOnlyList<Room> RoomsWithTag => GetRoomsWithTag();
        public IReadOnlyList<Room> RoomsWithoutTag => GetRoomsWithoutTag();
        public IReadOnlyList<DepartmentInfo> DepartmentInfos => _departmentInfos.AsReadOnly();
        public string ProjectTitle => _document.Title;

        public RoomsData(ExternalCommandData commandData)
        {
            _uiApp = commandData.Application;
            _document = _uiApp.ActiveUIDocument.Document;
            LoadRoomsAndTags();
            ClassifyRooms();
        }

        private void LoadRoomsAndTags()
        {
            var roomFilter = new RoomFilter();
            var tagFilter = new RoomTagFilter();
            var orFilter = new LogicalOrFilter(roomFilter, tagFilter);

            var elements = new FilteredElementCollector(_document)
                .WherePasses(orFilter)
                .ToElements();

            foreach (var elem in elements)
            {
                switch (elem)
                {
                    case Room room when _document.GetElement(room.LevelId) != null:
                        _rooms.Add(room);
                        break;
                    case RoomTag tag:
                        _roomTags.Add(tag);
                        break;
                }
            }
        }

        private void ClassifyRooms()
        {
            var roomsWithTag = _roomTags.Select(t => t.Room).ToHashSet();
            var roomsWithoutTag = _rooms.Except(roomsWithTag).ToList();
        }

        private IReadOnlyList<Room> GetRoomsWithTag() =>
            _roomTags.Select(t => t.Room).Distinct().ToList().AsReadOnly();

        private IReadOnlyList<Room> GetRoomsWithoutTag() =>
            _rooms.Except(GetRoomsWithTag()).ToList().AsReadOnly();

        public void RefreshData()
        {
            _rooms.Clear();
            _roomTags.Clear();
            _departmentInfos.Clear();
            LoadRoomsAndTags();
            ClassifyRooms();
        }

        public void CreateTags()
        {
            var roomsWithoutTag = GetRoomsWithoutTag();
            if (!roomsWithoutTag.Any()) return;

            foreach (var room in roomsWithoutTag)
            {
                var location = room.Location as LocationPoint;
                if (location == null)
                    throw new Exception($"房间 {room.Id.IntegerValue} 无法获取位置点");

                var uv = new UV(location.Point.X, location.Point.Y);
                var tag = _document.Create.NewRoomTag(new LinkElementId(room.Id), uv, null);
                if (tag != null) _roomTags.Add(tag);
            }

            RefreshData();
        }

        public void ReorderRooms()
        {
            if (!SortRoomsByLocation())
                throw new Exception("无法按位置排序房间");

            // 临时重命名避免冲突
            foreach (var room in _rooms)
                room.Number += "XXX";

            for (int i = 0; i < _rooms.Count; i++)
                _rooms[i].Number = (i + 1).ToString();
        }

        private bool SortRoomsByLocation()
        {
            for (int i = 0; i < _rooms.Count - 1; i++)
            {
                for (int j = i + 1; j < _rooms.Count; j++)
                {
                    var pointI = (_rooms[i].Location as LocationPoint)?.Point;
                    var pointJ = (_rooms[j].Location as LocationPoint)?.Point;

                    if (pointI == null || pointJ == null) return false;

                    if (ShouldSwap(pointI, pointJ))
                    {
                        (_rooms[i], _rooms[j]) = (_rooms[j], _rooms[i]);
                    }
                }
            }
            return true;
        }

        private static bool ShouldSwap(XYZ a, XYZ b) =>
            a.Z > b.Z || (Math.Abs(a.Z - b.Z) < 1e-9 && (a.X > b.X || (Math.Abs(a.X - b.X) < 1e-9 && a.Y > b.Y)));

        public void CalculateDepartmentArea(string department, double area)
        {
            var existing = _departmentInfos.FirstOrDefault(d => d.DepartmentName == department);
            if (existing.DepartmentName != null)
            {
                var index = _departmentInfos.IndexOf(existing);
                _departmentInfos[index] = new DepartmentInfo(
                    department,
                    existing.RoomsAmount + 1,
                    existing.DepartmentAreaValue + area);
            }
            else
            {
                _departmentInfos.Add(new DepartmentInfo(department, 1, area));
            }
        }

        public void ExportFile(string filePath)
        {
            var writer = new StreamWriter(filePath);
            writer.WriteLine($"项目房间统计 - {ProjectTitle}");
            writer.WriteLine("部门,房间数量,总面积(SF)");

            foreach (var dept in _departmentInfos)
            {
                writer.WriteLine($"{dept.DepartmentName},{dept.RoomsAmount},{dept.DepartmentAreaValue:F2}");
            }
        }

        public string GetRoomProperty(Room room, BuiltInParameter param) =>
            room.get_Parameter(param)?.AsString() ?? "";
    }

    /// <summary>
    /// 部门信息结构体
    /// </summary>
    public readonly struct DepartmentInfo
    {
        public string DepartmentName { get; }
        public int RoomsAmount { get; }
        public double DepartmentAreaValue { get; }

        public DepartmentInfo(string name, int amount, double area) =>
            (DepartmentName, RoomsAmount, DepartmentAreaValue) = (name, amount, area);
    }

    /// <summary>
    /// 房间项ViewModel - 用于ListView显示
    /// </summary>
    public class RoomItemViewModel : INotifyPropertyChanged
    {
        private readonly Room _room;
        private string _number;
        private bool _hasTag;

        public RoomItemViewModel(Room room, bool hasTag)
        {
            _room = room;
            _hasTag = hasTag;
            _number = room.Number;
        }

        public int Id => _room.Id.IntegerValue;
        public string Name => _room.Name;
        public string Number { get => _number; set { _number = value; OnPropertyChanged(); } }
        public string LevelName => _room.Level?.Name ?? "未知";
        public string Department => GetParameterValue(BuiltInParameter.ROOM_DEPARTMENT);
        public double Area => Math.Round(_room.Area, 2);
        public bool HasTag { get => _hasTag; set { _hasTag = value; OnPropertyChanged(); } }

        public Room Room => _room;

        private string GetParameterValue(BuiltInParameter param) =>
            _room.get_Parameter(param)?.AsString() ?? "";

        public void UpdateNumber() => Number = _room.Number;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 部门信息项ViewModel
    /// </summary>
    public class DepartmentItemViewModel : INotifyPropertyChanged
    {
        public string DepartmentName { get; }
        public int RoomsAmount { get; set; }
        public double TotalArea { get; set; }

        public DepartmentItemViewModel(string name, int amount, double area)
        {
            DepartmentName = name;
            RoomsAmount = amount;
            TotalArea = area;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
