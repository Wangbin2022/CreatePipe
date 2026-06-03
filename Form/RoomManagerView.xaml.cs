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
        // 大连项目的 ViewModel (即你之前写的 RoomAttrViewModel 的变体)
        public DalianProjectViewModel DalianVM { get; set; }
        public RoomManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            DalianVM = new DalianProjectViewModel(uiApp);

            _roomWarningService = new RoomWarningService(Document);
            rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType().Cast<Room>().ToList();
            PrecacheRoomData(Document);
            QueryElement(string.Empty);
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
        public void InitFunc() => QueryElement(null);
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string obj)
        {
            Collection.Clear();
            //  优化核心：先对轻量的 Room 对象进行过滤 
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
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
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
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<RoomSingleEntity>(DeleteElement);
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
                using (Transaction trans = new Transaction(Document, "彻底删除未放置房间"))
                {
                    trans.Start();
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
                    trans.Commit();
                }
                QueryElement(null);
            });
        }
        private Dictionary<ElementId, List<FamilyInstance>> _doorsByRoomId;
        private Dictionary<ElementId, List<FamilyInstance>> _windowsByRoomId;
        public List<Room> rooms = new List<Room>();
        private ObservableCollection<RoomSingleEntity> allRooms = new ObservableCollection<RoomSingleEntity>();
        public ObservableCollection<RoomSingleEntity> Collection
        {
            get => allRooms;
            set => SetProperty(ref allRooms, value);
        }
    }
    public class DalianProjectViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        // 提取硬编码，方便后续配置或由 UI 绑定
        private const string PROJECT_SINGLE_CODE = "HZQ02";
        public DalianProjectViewModel(UIApplication uiApp)
        {
            Doc = uiApp.ActiveUIDocument.Document;
            GetEntity(null);
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
        // 3. 写入单个实体的参数
        public ICommand CodeEntityCommand => new RelayCommand<RoomEntity>(entity => ExecuteWriteRooms(new List<RoomEntity> { entity }));
        // 4. 写入所有实体的参数
        public ICommand CodeAllCommand => new BaseBindingCommand(para => ExecuteWriteRooms(RoomModels.ToList()));
        // 核心写入方法：统一管理事务（单体或全部都可以复用）
        private void ExecuteWriteRooms(List<RoomEntity> entities)
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
                });
            });
        }
        // 安全赋值辅助方法（防止共享参数缺失导致报错跳出）
        private void SetParameterValue(Element elem, string paramName, string value)
        {
            Parameter p = elem.LookupParameter(paramName);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(value ?? "");
            }
        }
        //写入单个Entity修改信息（类别、空间设计编号（缩写加序号）、空间编码）
        //public ICommand CodeEntityCommand => new RelayCommand<RoomEntity>(RewriteEntityRoom);
        //private void RewriteEntityRoom(RoomEntity entity)
        //{
        //    _externalHandler.Run(app =>
        //    {
        //        NewTransaction.Execute(Doc, "写入单元信息", () =>
        //        {
        //            foreach (ElementId item in entity.roomIds)
        //            {
        //                Room room = Doc.GetElement(item) as Room;
        //                int index = entity.roomIds.IndexOf(room.Id) + 1;
        //                string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
        //                string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA).AsValueString();
        //                room.LookupParameter("空间名称").Set(roomName);
        //                room.LookupParameter("空间面积").Set(roomArea);
        //                room.LookupParameter("所属单体").Set("HZQ02");
        //                room.LookupParameter("空间类别编码").Set(entity.roomCode);
        //                room.LookupParameter("空间设计编号").Set(entity.roomAbbreviation + index.ToString());
        //                string roomLevel = room.LookupParameter("所属楼层").AsString();
        //                room.LookupParameter("空间编码").Set("HZQ02-" + entity.roomCode + "-" + roomLevel + "-" + entity.roomAbbreviation + index.ToString());
        //            }
        //        });
        //    });
        //    entity.IsRoomCoded = true;
        //}
        ////写全部的重复信息（面积，项目代码，空间名称）
        //public ICommand CodeAllCommand => new BaseBindingCommand(RewriteAllRoom);
        //private void RewriteAllRoom(Object para)
        //{
        //    foreach (RoomEntity room in RoomModels)
        //    {
        //        RewriteEntityRoom(room);
        //        CheckAllCoded(room);
        //    }
        //}
        private void CheckAllCoded(RoomEntity roomEntity)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            List<Room> roomQuery = elems.OfType<Room>().ToList();
            foreach (Room room in roomQuery)
            {
                // 方案3：使用模式匹配（C# 7.0+）
                if (room.get_Parameter(BuiltInParameter.ROOM_NAME) is Parameter roomNameParam &&
                    roomNameParam.AsString() == roomEntity.roomName &&
                    room.LookupParameter("空间编码") is Parameter spaceCodeParam &&
                    !string.IsNullOrEmpty(spaceCodeParam.AsString()))
                {
                    roomEntity.IsRoomCoded = true;
                }
            }
        }
        private void RewriteRoom()
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            List<Room> roomQuery = elems.OfType<Room>().ToList();
            NewTransaction.Execute(Doc, "写重复信息", () =>
            {
                foreach (Room room in roomQuery)
                {
                    try
                    {
                        string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
                        string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA).AsValueString();
                        room.LookupParameter("空间名称").Set(roomName);
                        room.LookupParameter("空间面积").Set(roomArea);
                        room.LookupParameter("所属单体").Set("HZQ02");
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            });
        }
        public ICommand RoomCompareCommand => new BaseBindingCommand(RoomCompare);
        private void RoomCompare(object obj)
        {
            if (!ShowList.Any()) return;

            // 优化：将 List 转为 Dictionary 哈希表进行 O(1) 查找，彻底告别双层嵌套循环导致的大量计算
            var codeDict = ShowList.ToDictionary(k => k.Name, v => v.Code);

            foreach (var item in RoomModels)
            {
                if (codeDict.TryGetValue(item.roomName, out string matchedCode))
                {
                    item.roomCode = matchedCode;
                }
            }
        }
        //赋码
        //private void RoomCompare(object obj)
        //{
        //    foreach (RoomEntity item in RoomModels)
        //    {
        //        foreach (var unit in showList)
        //        {
        //            if (item.roomName == unit.Name)
        //            {
        //                item.roomCode = unit.Code;
        //            }
        //        }
        //        //item.IsRoomCoded= true;
        //    }
        //    //GetEntity(null);
        //}
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
            var newModels = new ObservableCollection<RoomEntity>();
            foreach (var group in groupedRooms)
            {
                var roomList = group.ToList();
                var entity = new RoomEntity(roomList, Doc);
                CheckCodedStatus(entity, roomList); // 优化：直接传入查好的 roomList 判定，不需全文档再查一遍
                newModels.Add(entity);
            }
            RoomModels = newModels;
            //RoomModels.Clear();
            //ObservableCollection<RoomEntity> roomEntities = new ObservableCollection<RoomEntity>();
            //RoomEntity entity = null;
            //FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            //IEnumerable<Room> roomQuery = elems.OfType<Room>();
            ////// 如果 Keyword 为空，则获取所有房间；否则，只获取包含 Keyword 的房间
            ////IEnumerable<Room> filteredRooms = string.IsNullOrEmpty(Keyword)
            ////    ? roomQuery
            ////    : roomQuery.Where(item => item.get_Parameter(BuiltInParameter.ROOM_NAME).AsString().Contains(Keyword));
            ////List<Room> rooms = elems.OfType<Room>().ToList();
            ////List<Room> rooms = filteredRooms.ToList();
            //List<Room> rooms = roomQuery.ToList();
            //HashSet<string> sinRooms = new HashSet<string>();
            //foreach (Room item in rooms)
            //{
            //    sinRooms.Add(item.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
            //}
            //foreach (string name in sinRooms)
            //{
            //    List<Room> roomList = new List<Room>();
            //    foreach (var room in rooms)
            //    {
            //        if (room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() == name)
            //        {
            //            roomList.Add(room);
            //        }
            //    }
            //    entity = new RoomEntity(roomList, Doc);
            //    CheckAllCoded(entity);
            //    roomEntities.Add(entity);
            //}
            //RoomModels = roomEntities;
        }
        // 检查实体是否已完成编码
        private void CheckCodedStatus(RoomEntity entity, List<Room> rooms)
        {
            // 只要该组里有一个房间的“空间编码”有值，就认为已编码
            entity.IsRoomCoded = rooms.Any(r =>
                !string.IsNullOrEmpty(r.LookupParameter("空间编码")?.AsString()));
        }
        public string RoomCount => RoomModels.Count.ToString();
        private ObservableCollection<RoomEntity> roomModels = new ObservableCollection<RoomEntity>();
        public ObservableCollection<RoomEntity> RoomModels
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
                    // 优化：使用 using，自动释放句柄。加入 UTF8-BOM 以兼容 Excel 打开不乱码
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, new UTF8Encoding(true)))
                    {
                        foreach (var unit in ShowList)
                        {
                            // 简单的防止名称里带逗号破坏 CSV 结构的保护
                            string safeName = unit.Name?.Contains(",") == true ? $"\"{unit.Name}\"" : unit.Name;
                            writer.WriteLine($"{unit.Code},{safeName}");
                        }
                    }
                    MessageBox.Show("保存成功！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存 CSV 文件时发生错误: {ex.Message}");
                }
            }
        }
        private string csvPath { get; set; }
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
                    _originalShowList.Clear();

                    // 优化：废弃了全局的 StreamReader 属性。直接使用 File.ReadLines 高效读取，无文件占用死锁风险
                    // 兼容 Default (GB2312) 编码处理中文字符
                    var lines = File.ReadLines(opDialog.FileName, Encoding.Default);

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] values = line.Split(',');
                        if (values.Length >= 2)
                        {
                            _originalShowList.Add(new RevitUnit
                            {
                                Code = values[0].Trim(),
                                Name = values[1].Trim().Trim('"') // 移除可能存在的安全引号
                            });
                        }
                    }

                    FilterShowList();
                    HasLoadCsv = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取 CSV 失败，文件可能被占用。\n详情：{ex.Message}");
                }
            }
        }
        private ObservableCollection<RevitUnit> _originalShowList = new ObservableCollection<RevitUnit>();
        private ObservableCollection<RevitUnit> showList = new ObservableCollection<RevitUnit>();
        public ObservableCollection<RevitUnit> ShowList
        {
            get => showList;
            set
            {
                showList = value;
                OnPropertyChanged(nameof(showList));
            }
        }
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
    public class RevitUnit
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
