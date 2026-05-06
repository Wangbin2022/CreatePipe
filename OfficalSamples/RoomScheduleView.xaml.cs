using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
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
    /// RoomScheduleView.xaml 的交互逻辑
    /// </summary>
    public partial class RoomScheduleView : Window
    {
        public RoomScheduleView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 主窗口ViewModel - 管理Excel数据和Revit房间的同步
    /// </summary>
    public class RoomScheduleViewModel : ObserverableObject
    {
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private RoomsData _roomsData;
        private XlsDBConnector _dbConnector;

        private ObservableCollection<string> _worksheets;
        private string _selectedWorksheet;
        private ObservableCollection<RoomItemModel> _excelRooms;
        private ObservableCollection<RoomItemModel> _revitRooms;
        private ObservableCollection<Level> _levels;
        private Level _selectedLevel;
        private ObservableCollection<Phase> _phases;
        private Phase _selectedPhase;
        private string _excelFilePath;
        private bool _showAllRooms;
        private bool _isProcessing;

        public ObservableCollection<string> Worksheets
        {
            get => _worksheets;
            set { _worksheets = value; OnPropertyChanged(); }
        }

        public string SelectedWorksheet
        {
            get => _selectedWorksheet;
            set { _selectedWorksheet = value; OnPropertyChanged(); LoadExcelData(); }
        }

        public ObservableCollection<RoomItemModel> ExcelRooms
        {
            get => _excelRooms;
            set { _excelRooms = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RoomItemModel> RevitRooms
        {
            get => _revitRooms;
            set { _revitRooms = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Level> Levels
        {
            get => _levels;
            set { _levels = value; OnPropertyChanged(); }
        }

        public Level SelectedLevel
        {
            get => _selectedLevel;
            set { _selectedLevel = value; OnPropertyChanged(); LoadRevitRooms(); }
        }

        public ObservableCollection<Phase> Phases
        {
            get => _phases;
            set { _phases = value; OnPropertyChanged(); }
        }

        public Phase SelectedPhase
        {
            get => _selectedPhase;
            set { _selectedPhase = value; OnPropertyChanged(); }
        }

        public string ExcelFilePath
        {
            get => _excelFilePath;
            set { _excelFilePath = value; OnPropertyChanged(); }
        }

        public bool ShowAllRooms
        {
            get => _showAllRooms;
            set { _showAllRooms = value; OnPropertyChanged(); LoadRevitRooms(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand ImportExcelCommand;
        public ICommand CreateRoomsCommand;
        public ICommand ClearExternalIdCommand;
        public ICommand CloseCommand;

        public RoomScheduleViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            _roomsData = new RoomsData(_document);

            InitializeCommands();
            LoadLevels();
            LoadPhases();
            LoadRevitRooms();
        }

        private void InitializeCommands()
        {
            ImportExcelCommand = new BaseBindingCommand(_ => ExecuteImportExcel(), _ => CanExecute);
            CreateRoomsCommand = new BaseBindingCommand(_ => ExecuteCreateRooms(), _ => CanExecute);
            ClearExternalIdCommand = new BaseBindingCommand(_ => ExecuteClearExternalId(), _ => CanExecute);
            CloseCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void LoadLevels()
        {
            var levels = new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation);

            Levels = new ObservableCollection<Level>(levels);
            SelectedLevel = Levels.FirstOrDefault();
        }

        private void LoadPhases()
        {
            var phases = new FilteredElementCollector(_document)
                .OfClass(typeof(Phase))
                .Cast<Phase>();

            Phases = new ObservableCollection<Phase>(phases);
            SelectedPhase = Phases.FirstOrDefault();
        }

        private void LoadRevitRooms()
        {
            var rooms = _roomsData.Rooms;
            var filteredRooms = ShowAllRooms
                ? rooms
                : rooms.Where(r => _document.GetElement(r.LevelId) == SelectedLevel);

            RevitRooms = new ObservableCollection<RoomItemModel>(
                filteredRooms.Select(room => new RoomItemModel
                {
                    ExternalId = GetExternalRoomId(room),
                    Name = RoomsData.GetProperty(_document, room, BuiltInParameter.ROOM_NAME, true),
                    Number = room.Number,
                    Area = RoomsData.GetProperty(_document, room, BuiltInParameter.ROOM_AREA, true),
                    Comments = RoomsData.GetProperty(_document, room, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, true),
                    Level = _document.GetElement(room.LevelId)?.Name,
                    Phase = RoomsData.GetProperty(_document, room, BuiltInParameter.ROOM_PHASE, true),
                    Room = room,
                    IsFromExcel = false
                }));
        }

        private string GetExternalRoomId(Room room)
        {
            var param = room.LookupParameter(RoomsData.SharedParam);
            return param?.AsString() ?? "";
        }

        private void ExecuteImportExcel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件|*.xls;*.xlsx|所有文件|*.*",
                Title = "选择房间数据Excel文件"
            };

            if (dialog.ShowDialog() == true)
            {
                IsProcessing = true;
                try
                {
                    ExcelFilePath = dialog.FileName;
                    _dbConnector?.Dispose();
                    _dbConnector = new XlsDBConnector(ExcelFilePath);

                    Worksheets = new ObservableCollection<string>(_dbConnector.RetrieveAllTables());
                    if (Worksheets.Any())
                        SelectedWorksheet = Worksheets.First();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"导入Excel失败：{ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        private void LoadExcelData()
        {
            if (_dbConnector == null || string.IsNullOrEmpty(SelectedWorksheet)) return;

            IsProcessing = true;
            try
            {
                var dataTable = _dbConnector.GetDataTable(SelectedWorksheet);
                ExcelRooms = new ObservableCollection<RoomItemModel>(
                    dataTable.Rows.Cast<DataRow>().Select(row => new RoomItemModel
                    {
                        ExternalId = row[RoomsData.RoomID]?.ToString(),
                        Name = row[RoomsData.RoomName]?.ToString(),
                        Number = row[RoomsData.RoomNumber]?.ToString(),
                        Area = row[RoomsData.RoomArea]?.ToString(),
                        Comments = row[RoomsData.RoomComments]?.ToString(),
                        IsFromExcel = true
                    }));
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"加载Excel数据失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteCreateRooms()
        {
            if (_dbConnector == null || ExcelRooms == null) return;

            IsProcessing = true;
            using (var transaction = new Transaction(_document, "创建房间"))
            {
                transaction.Start();
                try
                {
                    var existingIds = RevitRooms.Select(r => r.ExternalId).ToHashSet();
                    var newRooms = ExcelRooms.Where(r => !existingIds.Contains(r.ExternalId)).ToList();

                    foreach (var newRoom in newRooms)
                    {
                        CreateRoomFromExcelData(newRoom);
                    }

                    transaction.Commit();
                    _roomsData.UpdateRoomsData();
                    LoadRevitRooms();

                    TaskDialog.Show("成功", $"成功创建 {newRooms.Count} 个房间。");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    TaskDialog.Show("错误", $"创建房间失败：{ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        private void CreateRoomFromExcelData(RoomItemModel roomData)
        {
            var level = Levels.FirstOrDefault(l => l.Name == roomData.Level);
            if (level == null) return;

            // 创建未放置房间
            //var room = _document.Create.NewRoom(level, SelectedPhase);
            var room = _document.Create.NewRoom(SelectedPhase);
            room.Name = roomData.Name;
            room.Number = roomData.Number;

            // 设置外部ID共享参数
            var param = room.LookupParameter(RoomsData.SharedParam);
            param?.Set(roomData.ExternalId);
        }

        private void ExecuteClearExternalId()
        {
            var selectedRooms = RevitRooms.Where(r => r.IsSelected).ToList();
            if (!selectedRooms.Any()) return;

            IsProcessing = true;
            using (var transaction = new Transaction(_document, "清除外部房间ID"))
            {
                transaction.Start();
                try
                {
                    foreach (var roomVm in selectedRooms)
                    {
                        var room = roomVm.Room;
                        if (room != null)
                        {
                            var param = room.LookupParameter(RoomsData.SharedParam);
                            param?.Set("");
                        }
                    }
                    transaction.Commit();
                    LoadRevitRooms();
                    TaskDialog.Show("成功", $"已清除 {selectedRooms.Count} 个房间的外部ID。");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    TaskDialog.Show("错误", $"清除失败：{ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        public Action CloseWindow { get; set; }
    }
    /// <summary>
    /// 房间项ViewModel - 用于Excel和Revit列表显示
    /// </summary>
    public class RoomItemModel : ObserverableObject
    {
        private string _externalId;
        private string _name;
        private string _number;
        private string _area;
        private string _comments;
        private string _level;
        private string _phase;
        private bool _isSelected;
        private Room _room;

        public string ExternalId { get => _externalId; set { _externalId = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Number { get => _number; set { _number = value; OnPropertyChanged(); } }
        public string Area { get => _area; set { _area = value; OnPropertyChanged(); } }
        public string Comments { get => _comments; set { _comments = value; OnPropertyChanged(); } }
        public string Level { get => _level; set { _level = value; OnPropertyChanged(); } }
        public string Phase { get => _phase; set { _phase = value; OnPropertyChanged(); } }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public Room Room
        {
            get => _room;
            set { _room = value; OnPropertyChanged(); }
        }

        // 标记此房间是否来自Excel源
        public bool IsFromExcel { get; set; }
    }

    /// <summary>
    /// Excel数据源连接器 - 支持读取和更新.xls文件
    /// 实现IDisposable接口确保连接释放
    /// </summary>
    public class XlsDBConnector : IDisposable
    {
        private OleDbConnection _connection;
        private OleDbCommand _command;
        private bool _disposed;

        // Excel必需列名常量
        private static readonly string[] RequiredColumns =
        {
            RoomsData.RoomID, RoomsData.RoomName,
            RoomsData.RoomNumber, RoomsData.RoomArea,
            RoomsData.RoomComments
        };

        public XlsDBConnector(string excelFilePath)
        {
            if (!ValidateFile(excelFilePath))
                throw new ArgumentException($"文件不存在或只读：{excelFilePath}", nameof(excelFilePath));

            var connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=\"{excelFilePath}\";Extended Properties=\"Excel 8.0;HDR=YES;\"";
            _connection = new OleDbConnection(connectionString);
            _connection.Open();
        }

        /// <summary>
        /// 获取所有工作表的名称
        /// </summary>
        public IList<string> RetrieveAllTables()
        {
            var schemaTable = _connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            return schemaTable.Rows.Cast<DataRow>()
                .Select(row => row[2].ToString().TrimEnd('$'))
                .ToList();
        }

        /// <summary>
        /// 根据工作表名称生成DataTable，并验证必需列
        /// </summary>
        public DataTable GetDataTable(string worksheetName)
        {
            var commandText = $"Select * From [{worksheetName}$]";
            var adapter = new OleDbDataAdapter(commandText, _connection);
            var dataSet = new DataSet();
            adapter.Fill(dataSet, $"[{worksheetName}$]");
            var dataTable = dataSet.Tables[0];

            ValidateColumns(dataTable);
            return dataTable;
        }

        /// <summary>
        /// 验证Excel列是否包含所有必需列，且无重复
        /// </summary>
        private void ValidateColumns(DataTable dataTable)
        {
            var columnNames = dataTable.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToList();

            // 检查重复列
            var duplicates = columnNames.GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                throw new Exception($"Excel中存在重复列：{string.Join(", ", duplicates)}");
            }

            // 检查缺失列
            var missingColumns = RequiredColumns.Except(columnNames).ToList();
            if (missingColumns.Any())
            {
                throw new Exception($"Excel中缺少必需列：{string.Join(", ", missingColumns)}");
            }
        }

        /// <summary>
        /// 执行SQL命令（更新/插入）
        /// </summary>
        public int ExecuteCommand(string commandText)
        {
            _command = _connection.CreateCommand();
            _command.CommandText = commandText;
            return _command.ExecuteNonQuery();
        }

        private static bool ValidateFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            File.SetAttributes(filePath, FileAttributes.Normal);
            return File.GetAttributes(filePath) == FileAttributes.Normal;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _connection?.Close();
            _connection?.Dispose();
            _command?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~XlsDBConnector() => Dispose();
    }
    /// <summary>
    /// 房间数据管理类 - 收集房间信息、管理共享参数
    /// 使用C# 7.3语法：LINQ、表达式体、字符串插值
    /// </summary>
    public partial class RoomsData
    {
        #region 常量定义
        public const string RoomID = "ID";
        public const string RoomArea = "Room Area";
        public const string RoomName = "Room Name";
        public const string RoomNumber = "Room Number";
        public const string RoomComments = "Room Comments";
        public const string SharedParam = "External Room ID";
        #endregion

        //private readonly Document _document;
        //private List<Room> _rooms;
        //public IReadOnlyList<Room> Rooms => _rooms.AsReadOnly();

        public RoomsData(Document document)
        {
            _document = document;
            LoadAllRooms();
        }

        /// <summary>
        /// 更新房间数据 - 重新加载
        /// </summary>
        public void UpdateRoomsData()
        {
            LoadAllRooms();
        }

        /// <summary>
        /// 获取所有房间（按编号排序）
        /// </summary>
        private void LoadAllRooms()
        {
            var filter = new RoomFilter();
            var collector = new FilteredElementCollector(_document);
            _rooms = collector.WherePasses(filter)
                .Cast<Room>()
                .OrderBy(r => r.Number)
                .ToList();
        }

        /// <summary>
        /// 获取房间属性值 - 使用switch表达式
        /// </summary>
        public static string GetProperty(Document doc, Room room, BuiltInParameter paramEnum, bool useUnit)
        {
            try
            {
                var param = room.get_Parameter(paramEnum);
                if (param == null) return "";

                switch (param.StorageType)
                {
                    case StorageType.Integer:
                        return param.AsInteger().ToString();
                    case StorageType.String:
                        return param.AsString() ?? "";
                    case StorageType.Double:
                        return useUnit ? param.AsValueString() : param.AsDouble().ToString();
                    case StorageType.ElementId:
                        return GetElementName(doc, param.AsElementId());
                    default:
                        return param.AsString() ?? "";
                }
            }
            catch
            {
                //// 未放置的房间可能无法获取某些属性
                //return room.Location == null ? "未放置" : throw;
                return "未放置";
            }
        }

        private static string GetElementName(Document doc, ElementId id) =>
            doc.GetElement(id)?.Name ?? id.IntegerValue.ToString();

        /// <summary>
        /// 检查共享参数是否存在
        /// </summary>
        public static bool HasSharedParameter(Room room, out Parameter sharedParam)
        {
            sharedParam = room.LookupParameter(SharedParam);
            return sharedParam != null;
        }

        /// <summary>
        /// 获取共享参数值
        /// </summary>
        public static string GetSharedParameterValue(Room room) =>
            room.LookupParameter(SharedParam)?.AsString() ?? "";
    }
}
