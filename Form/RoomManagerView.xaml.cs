using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Obselete.RoomAttr;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
    public class RoomManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<RoomSingleEntity>
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public Autodesk.Revit.DB.View ActiveView { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        private readonly RoomWarningService _roomWarningService; // ViewModel 持有 RoomWarningService 实例
        //根据需要加载项目单独的ViewModel
        public DalianProjectViewModel DalianVM { get; set; }
        public RoomManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            _roomWarningService = new RoomWarningService(Document);
            // 实例化子 ViewModel
            DalianVM = new DalianProjectViewModel(uiApp);
            PrecacheRoomData(Document);
            QueryElement(string.Empty);
        }
        public void InitFunc() => QueryElement(null);
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string obj)
        {
            Collection.Clear();
            //  优化核心：先对轻量的 Room 对象进行过滤 
            var rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType().Cast<Room>().ToList();
            var filteredRooms = rooms.Where(r => r.IsValidObject && r.Location != null).Where(r => string.IsNullOrEmpty(obj) ||
            (r.Name != null && r.Name.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (r.Number != null && r.Number.Contains(obj))).ToList();

            //// 1. 进行一次全局的警告分析。这是最有效率的方式，避免循环查询Revit的警告列表。
            RoomWarningAnalysisResult analysisResult = _roomWarningService.AnalyzeRoomWarnings();
            // 只为过滤后的结果创建 RoomSingleEntity 对象
            foreach (var room in filteredRooms)
            {
                // 从缓存中获取这个房间的门和窗
                List<FamilyInstance> doorsForThisRoom = _doorsByRoomId.ContainsKey(room.Id) ? _doorsByRoomId[room.Id] : new List<FamilyInstance>();
                List<FamilyInstance> windowsForThisRoom = _windowsByRoomId.ContainsKey(room.Id) ? _windowsByRoomId[room.Id] : new List<FamilyInstance>();
                // 创建对象时传入缓存数据
                bool hasWarnings = _roomWarningService.HasWarningsForRoom(room.Id, analysisResult);
                //bool hasWarnings = _roomWarningService.HasWarningsForRoom(room.Id);
                var entity = new RoomSingleEntity(room, doorsForThisRoom, windowsForThisRoom, hasWarnings);
                Collection.Add(entity);
            }
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        //删除多选房间
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;
            var entities = selectedItems.Cast<RoomSingleEntity>().ToList();
            if (!entities.Any()) return;
            var idsToDelete = new HashSet<ElementId>();
            foreach (var item in entities)
            {
                idsToDelete.Add(item.Id);
            }
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "删除房间实例", () =>
                {
                    // Document.Delete 接受 ICollection<ElementId>
                    Document.Delete(idsToDelete.ToList());
                });
                // 刷新列表
                QueryElement(null);
                DalianVM.RefreshData();
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<RoomSingleEntity>(DeleteElement);
        //删除未放置房间
        public void DeleteElement(RoomSingleEntity obj)
        {
            ExternalHandler.Run(app =>
            {
                FilteredElementCollector collector = new FilteredElementCollector(Document);
                ICollection<Element> allRooms = collector.OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType().ToElements();
                // 2. 筛选出“未放置”的房间
                List<ElementId> unplacedRoomIds = new List<ElementId>();
                foreach (Element elem in allRooms)
                {
                    Room room = elem as Room;
                    if (room != null && room.Location == null)
                    {
                        unplacedRoomIds.Add(room.Id);
                    }
                }
                // 3. 开启事务，执行删除操作
                NewTransaction.Execute(Document, "彻底删除未放置房间", () =>
                {
                    if (unplacedRoomIds.Count > 0)
                    {
                        // 一次性删除所有收集到的未放置房间ID
                        Document.Delete(unplacedRoomIds);
                        TaskDialog.Show("完成", $"已彻底删除 {unplacedRoomIds.Count} 个未放置房间。");
                    }
                    else
                    {
                        TaskDialog.Show("提示", "没有找到未放置的房间。");
                    }
                });
                QueryElement(null);
                DalianVM.RefreshData();
            });
        }
        public ICommand PlaceRoomNameCommand => new RelayCommand<RoomSingleEntity>(PlaceRoomName);
        private void PlaceRoomName(RoomSingleEntity entity)
        {
            XYZ boundCenter = GetElementCenter(entity.Room);
            LocationPoint locPt = (LocationPoint)entity.Room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            // 1. 先查“三维文字”族是否存在
            Family targetFamily = new FilteredElementCollector(Document).OfClass(typeof(Family)).Cast<Family>()
                .FirstOrDefault(f => f.Name.Equals("三维文字", StringComparison.OrdinalIgnoreCase));
            if (targetFamily == null)
            {
                TaskDialog.Show("提示", "项目中未找到三维文字族"); return;
            }
            FamilySymbol selectSymbol = Document.GetElement(targetFamily.GetFamilySymbolIds().First()) as FamilySymbol;
            ExternalHandler.Run(app =>
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
        /// 一次性收集所有门窗，并按房间ID进行分组缓存。
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
        // 缓存门窗字典
        private Dictionary<ElementId, List<FamilyInstance>> _doorsByRoomId;
        private Dictionary<ElementId, List<FamilyInstance>> _windowsByRoomId;

        private ObservableCollection<RoomSingleEntity> allRooms = new ObservableCollection<RoomSingleEntity>();
        public ObservableCollection<RoomSingleEntity> Collection
        {
            get => allRooms;
            set => SetProperty(ref allRooms, value);
        }
    }
    public abstract class ProjectBaseViewModel : ObserverableObject
    {
        public Document Doc { get; protected set; }
        // --- 通用属性 ---
        private bool _isProjectConfigured;
        public bool IsProjectConfigured
        {
            get => _isProjectConfigured;
            set { _isProjectConfigured = value; OnPropertyChanged(); }
        }
        private string _configWarning;
        public string ConfigWarning
        {
            get => _configWarning;
            set { _configWarning = value; OnPropertyChanged(); }
        }
        // --- 抽象属性：强制子类提供自己特有的参数列表 ---
        protected abstract string[] RequiredParams { get; }
        // --- 通用辅助方法 ---
        protected void SetParameterValue(Element elem, string paramName, string value)
        {
            Parameter p = elem?.LookupParameter(paramName);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(value ?? "");
            }
        }
        // --- 通用验证逻辑 ---
        public virtual void ValidateRevitParameters()
        {
            Room sampleRoom = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType().Cast<Room>().FirstOrDefault();
            if (sampleRoom == null)
            {
                IsProjectConfigured = false;
                ConfigWarning = "模型中没有找到任何房间。";
                return;
            }
            List<string> missing = new List<string>();
            foreach (var pName in RequiredParams) // 使用子类提供的参数名
            {
                Parameter p = sampleRoom.LookupParameter(pName);
                if (p == null) missing.Add(pName + " (不存在)");
                else if (p.IsReadOnly) missing.Add(pName + " (只读)");
            }
            if (missing.Any())
            {
                IsProjectConfigured = false;
                ConfigWarning = "缺少必要参数：\n" + string.Join("\n", missing);
            }
            else
            {
                IsProjectConfigured = true;
                ConfigWarning = "参数配置正确。";
            }
        }
    }
    public class DalianProjectViewModel : ProjectBaseViewModel
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        // 提取硬编码，方便后续配置或由 UI 绑定
        private const string PROJECT_SINGLE_CODE = "HZQ02";
        // 只定义大连项目需要的参数
        protected override string[] RequiredParams => new[] { "空间名称", "空间面积", "所属单体", "空间类别编码", "空间设计编号", "空间编码", "所属楼层" };
        public DalianProjectViewModel(UIApplication uiApp)
        {
            Doc = uiApp.ActiveUIDocument.Document;
            ValidateRevitParameters();
            RefreshData();
        }
        public ICommand RoomCompareCommand => new BaseBindingCommand(RoomCompare);
        private void RoomCompare(object obj)
        {
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "写入空间编码信息", () =>
                {
                    if (!ShowList.Any()) return;
                    //// 优化：将 List 转为 Dictionary 哈希表进行 O(1) 查找，彻底告别双层嵌套循环导致的大量计算
                    var codeDict = ShowList.ToDictionary(k => k.Name, v => v.Code);
                    int index = 0;
                    foreach (var item in RoomModels)
                    {
                        if (codeDict.TryGetValue(item.roomName, out string matchedCode))
                        {
                            //item.roomCode = matchedCode;
                            index++;
                            foreach (var id in item.roomIds)
                            {
                                var room = Doc.GetElement(id) as Element;
                                SetParameterValue(room, "空间类别编码", matchedCode);
                            }
                        }
                    }
                    TaskDialog.Show("tt", $"发现{index}个匹配项并赋码");
                });
            });
        }
        public ICommand QueryELementCommand => new BaseBindingCommand(GetEntity);
        private void GetEntity(Object para)
        {
            // 优化：使用 LINQ 一次性完成过滤和分组，避免原来的双层 foreach 和 HashSet 造成的 O(N^2) 性能浪费
            var roomCollector = new FilteredElementCollector(Doc)
                   .OfCategory(BuiltInCategory.OST_Rooms)
                   .WhereElementIsNotElementType().Cast<Room>()
                   .Where(r => r != null && r.IsValidObject);
            var groupedRooms = roomCollector
                .GroupBy(r => r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "未命名").ToList();
            var newModels = new ObservableCollection<DLRoomEntity>();
            foreach (var group in groupedRooms)
            {
                var roomList = group.ToList();
                var entity = new DLRoomEntity(roomList, Doc);
                CheckCodedStatus(entity, roomList); // 优化：直接传入查好的 roomList 判定，不需全文档再查一遍
                newModels.Add(entity);
            }
            RoomModels = newModels;
        }
        public void RefreshData()
        {
            GetEntity(null);
        }

        // 3. 写入单个实体的参数
        public ICommand CodeEntityCommand => new RelayCommand<DLRoomEntity>(entity => ExecuteWriteRooms(new List<DLRoomEntity> { entity }), _ => IsProjectConfigured);
        // 4. 写入所有实体的参数
        public ICommand CodeAllCommand => new BaseBindingCommand(para => ExecuteWriteRooms(RoomModels.ToList()));
        // 核心写入方法：统一管理事务（单体或全部都可以复用）
        private void ExecuteWriteRooms(List<DLRoomEntity> entities)
        {
            if (entities == null || !entities.Any()) return;
            _externalHandler.Run(app =>
            {
                // 优化：无论是一个还是全部，都只开启一次事务。大大提高速度，且 Revit 撤销菜单只生成一条记录。
                NewTransaction.Execute(Doc, "写入空间编码信息", () =>
                {
                    foreach (var entity in entities)
                    {
                        for (int i = 0; i < entity.roomIds.Count; i++)
                        {
                            Room room = Doc.GetElement(entity.roomIds[i]) as Room;
                            if (room == null) continue;
                            int index = i + 1;
                            string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                            string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA)?.AsValueString() ?? "0";
                            // 安全获取楼层名称，防止参数为空导致崩溃
                            Parameter levelParam = room.LookupParameter("所属楼层");
                            string roomLevel = levelParam?.AsString() ?? (room.Level?.Name ?? "未知楼层");
                            string designCode = $"{entity.roomAbbreviation}{index}";
                            string fullCode = $"{PROJECT_SINGLE_CODE}-{entity.roomCode}-{roomLevel}-{designCode}";
                            // 封装的安全赋值方法
                            SetParameterValue(room, "空间名称", roomName);
                            SetParameterValue(room, "空间面积", roomArea);
                            SetParameterValue(room, "所属单体", PROJECT_SINGLE_CODE);
                            SetParameterValue(room, "空间类别编码", entity.roomCode);
                            SetParameterValue(room, "空间设计编号", designCode);
                            SetParameterValue(room, "空间编码", fullCode);
                        }
                        // 由于处于异步线程中，UI 更新需要调度或由外部处理。如果是简单绑定直接改状态即可。
                        entity.IsRoomCoded = true;
                    }
                    RefreshData();
                });
            });
        }
        // 检查实体是否已完成编码
        private void CheckCodedStatus(DLRoomEntity entity, List<Room> rooms)
        {
            // 只要该组里有一个房间的“空间编码”有值，就认为已编码
            entity.IsRoomCoded = rooms.Any(r =>
                !string.IsNullOrEmpty(r.LookupParameter("空间编码")?.AsString()));
        }
        public string RoomCount => RoomModels.Count.ToString();
        private ObservableCollection<DLRoomEntity> roomModels = new ObservableCollection<DLRoomEntity>();
        public ObservableCollection<DLRoomEntity> RoomModels
        {
            get => roomModels;
            set
            {
                roomModels = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoomCount));
            }
        }
        //编码列表的动态查询
        private ObservableCollection<RevitUnit> originalShowList = new ObservableCollection<RevitUnit>();
        // 动态过滤字典列表
        private void FilterShowList()
        {
            if (string.IsNullOrEmpty(_keyCodeName))
            {
                ShowList = new ObservableCollection<RevitUnit>(_originalShowList);
            }
            else
            {
                var filtered = _originalShowList.Where(unit => unit.Name.Contains(_keyCodeName)).ToList();
                ShowList = new ObservableCollection<RevitUnit>(filtered);
            }
        }
        private bool _hasLoadCsv;
        public bool HasLoadCsv
        {
            get { return _hasLoadCsv; }
            set
            {
                if (_hasLoadCsv != value)
                {
                    _hasLoadCsv = value;
                    OnPropertyChanged(nameof(HasLoadCsv)); // 通知 UI 更新
                }
            }
        }
        // [修正] 去掉了重复定义的 _originalShowList，统一放到这里
        private ObservableCollection<RevitUnit> _originalShowList = new ObservableCollection<RevitUnit>();
        private ObservableCollection<RevitUnit> _showList = new ObservableCollection<RevitUnit>();
        public ObservableCollection<RevitUnit> ShowList
        {
            get => _showList;
            set { _showList = value; OnPropertyChanged(); }
        }
        public ICommand SaveCsvCommand => new BaseBindingCommand(SaveCsv);
        private void SaveCsv(object obj)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "导出 CSV 文件",
                Filter = "CSV 文件 (*.csv)|*.csv",
                FileName = "空间编码对照表.csv"
            };
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // 1. 构造 CsvHelper 实例
                    var helper = new CsvHelper(saveDialog.FileName);
                    // 2. 将数据转换为 IEnumerable<IEnumerable<string>> 格式,处理 CSV 安全性：如果名称里有逗号，包裹双引号
                    var dataToSave = ShowList.Select(unit => new List<string> { unit.Code, unit.Name?.Contains(",") == true ? $"\"{unit.Name}\"" : unit.Name });
                    // 3. 一次性写入（内部已处理 UTF8-BOM）
                    helper.WriteAll(dataToSave);
                    MessageBox.Show("保存成功！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存 CSV 文件时发生错误: {ex.Message}");
                }
            }
        }
        public ICommand ReadCsvCommand => new BaseBindingCommand(ImportCsv);
        private void ImportCsv(object obj)
        {
            OpenFileDialog opDialog = new OpenFileDialog
            {
                Title = "导入 CSV 文件",
                Filter = "CSV 文件 (*.csv)|*.csv"
            };
            if (opDialog.ShowDialog() == true)
            {
                try
                {
                    // 1. 使用 Helper 静态方法解析（支持引号、复杂换行等）
                    // 默认传入 Encoding.Default (GB2312) 以兼容普通 Excel 导出的 CSV
                    var rows = CsvHelper.ParseCsv(opDialog.FileName, Encoding.Default);
                    _originalShowList.Clear();
                    // 2. 将解析出的 string[] 转换为 RevitUnit 实体
                    foreach (var fields in rows)
                    {
                        if (fields.Length >= 2)
                        {
                            _originalShowList.Add(new RevitUnit
                            {
                                Code = fields[0].Trim(),
                                Name = fields[1].Trim() // ParseCsv 内部已处理引号，此处无需再 Trim('"')
                            });
                        }
                    }
                    FilterShowList();
                    HasLoadCsv = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取 CSV 失败: {ex.Message}");
                }
            }
        }
        private string csvPath { get; set; }
        private string _keyCodeName;
        public string KeyCodeName
        {
            get => _keyCodeName;
            set
            {
                _keyCodeName = value;
                OnPropertyChanged();
                FilterShowList();
            }
        }
    }
    //public class DalianProjectViewModel : ObserverableObject
    //{ 



    //}
    public class RevitUnit
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
