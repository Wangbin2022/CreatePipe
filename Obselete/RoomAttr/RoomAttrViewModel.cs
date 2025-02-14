using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;


namespace CreatePipe.RoomAttr
{
    public class RoomAttrViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public RoomAttrViewModel(UIApplication uiApp)
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
        //写入单个Entity修改信息（类别、空间设计编号（缩写加序号）、空间编码）
        public ICommand CodeEntityCommand => new RelayCommand<RoomEntity>(RewriteEntityRoom);
        private void RewriteEntityRoom(RoomEntity entity)
        {
            XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    foreach (ElementId item in entity.roomIds)
                    {
                        Room room = Doc.GetElement(item) as Room;
                        int index = entity.roomIds.IndexOf(room.Id) + 1;
                        string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
                        string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA).AsValueString();
                        room.LookupParameter("空间名称").Set(roomName);
                        room.LookupParameter("空间面积").Set(roomArea);
                        room.LookupParameter("所属单体").Set("HZQ02");
                        room.LookupParameter("空间类别编码").Set(entity.roomCode);
                        room.LookupParameter("空间设计编号").Set(entity.roomAbbreviation + index.ToString());
                        string roomLevel = room.LookupParameter("所属楼层").AsString();
                        room.LookupParameter("空间编码").Set("HZQ02-" + entity.roomCode + "-" + roomLevel + "-" + entity.roomAbbreviation + index.ToString());
                    }
                }, "写入单元信息");
            });
            entity.IsRoomCoded = true;
        }

        //写全部的重复信息（面积，项目代码，空间名称）
        public ICommand CodeAllCommand => new BaseBindingCommand(RewriteAllRoom);
        private void RewriteAllRoom(Object para)
        {
            //RewriteRoom();
            foreach (RoomEntity room in RoomModels)
            {
                RewriteEntityRoom(room);
                CheckAllCoded(room);
            }
        }
        private void CheckAllCoded(RoomEntity roomEntity)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            List<Room> roomQuery = elems.OfType<Room>().ToList();
            foreach (Room room in roomQuery)
            {
                if (room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() == roomEntity.roomName &&
                    !string.IsNullOrEmpty(room.LookupParameter("空间编码").AsString()))
                {
                    roomEntity.IsRoomCoded = true;
                }
            }
        }
        private void RewriteRoom()
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            List<Room> roomQuery = elems.OfType<Room>().ToList();
            Doc.NewTransaction(() =>
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
            }, "写重复信息");
        }
        public ICommand RoomCompareCommand => new BaseBindingCommand(RoomCompare);
        //赋码
        private void RoomCompare(object obj)
        {
            foreach (RoomEntity item in RoomModels)
            {
                foreach (var unit in showList)
                {
                    if (item.roomName == unit.Name)
                    {
                        item.roomCode = unit.Code;
                    }
                }
                //item.IsRoomCoded= true;
            }
            //GetEntity(null);
        }
        public ICommand QueryELementCommand => new BaseBindingCommand(GetEntity);
        private void GetEntity(Object para)
        {
            RoomModels.Clear();
            ObservableCollection<RoomEntity> roomEntities = new ObservableCollection<RoomEntity>();
            RoomEntity entity = null;
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms);
            IEnumerable<Room> roomQuery = elems.OfType<Room>();
            // 如果 Keyword 为空，则获取所有房间；否则，只获取包含 Keyword 的房间
            IEnumerable<Room> filteredRooms = string.IsNullOrEmpty(Keyword)
                ? roomQuery
                : roomQuery.Where(item => item.get_Parameter(BuiltInParameter.ROOM_NAME).AsString().Contains(Keyword));
            //List<Room> rooms = elems.OfType<Room>().ToList();
            List<Room> rooms = filteredRooms.ToList();
            HashSet<string> sinRooms = new HashSet<string>();
            foreach (Room item in rooms)
            {
                sinRooms.Add(item.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
            }
            foreach (string name in sinRooms)
            {
                List<Room> roomList = new List<Room>();
                foreach (var room in rooms)
                {
                    if (room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() == name)
                    {
                        roomList.Add(room);
                    }
                }
                entity = new RoomEntity(roomList, Doc);
                CheckAllCoded(entity);
                roomEntities.Add(entity);
            }
            RoomModels = roomEntities;

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
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set
            {
                _keyword = value;
                OnPropertyChanged(nameof(Keyword));
            }
        }

        //编码列表的动态查询
        private ObservableCollection<RevitUnit> originalShowList = new ObservableCollection<RevitUnit>();
        private void FilterShowList()
        {
            if (string.IsNullOrEmpty(_keyCodeName))
            {
                // 如果关键字为空，则显示所有条目
                ShowList = new ObservableCollection<RevitUnit>(originalShowList);
            }
            else
            {
                // 过滤包含关键字的条目
                var filteredList = originalShowList.Where(unit => unit.Name.Contains(_keyCodeName)).ToList();
                ShowList = new ObservableCollection<RevitUnit>(filteredList);
            }
        }
        public ICommand SaveCsvCommand => new BaseBindingCommand(SaveCsv);
        private void SaveCsv(object obj)
        {
            if (string.IsNullOrEmpty(csvPath)) return;
            try
            {
                // 使用写入模式打开文件，如果文件存在则覆盖
                using (StreamWriter writer = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    foreach (var revitUnit in ShowList)
                    {
                        string line = $"{revitUnit.Code},{revitUnit.Name}";
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存CSV文件时发生错误: {ex.Message}");
            }
        }
        public ICommand ReadCsvCommand => new BaseBindingCommand(ImportCsv);
        private StreamReader streamReader;
        public StreamReader StreamReader
        {
            get => streamReader;
            set
            {
                streamReader = value;
                OnPropertyChanged();
            }
        }
        private string csvPath { get; set; }
        private void ImportCsv(object obj)
        {
            OpenFileDialog opDialog = new OpenFileDialog();
            opDialog.Title = "导入csv文件";
            opDialog.Filter = "csv文件（*。csv）|*.csv";
            opDialog.ShowDialog();
            csvPath = opDialog.FileName;
            FileStream fileStream = new FileStream(csvPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            try
            {
                // 打开文件并创建 StreamReader 对象
                streamReader = new StreamReader(fileStream, System.Text.Encoding.Default);
                ReadCsv(null);
                fileStream.Close();
                HasLoadCsv = true;
            }
            catch (Exception)
            {
            }
        }
        private void ReadCsv(object obj)
        {
            originalShowList.Clear();
            if (streamReader != null && !streamReader.EndOfStream)
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    string[] values = line.Split(',');
                    RevitUnit revitUnit = new RevitUnit
                    {
                        Code = values[0],
                        Name = values[1]
                    };
                    originalShowList.Add(revitUnit);
                }
            }
            FilterShowList(); // 调用过滤方法以初始化显示列表
        }
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
                OnPropertyChanged(nameof(KeyCodeName));
                FilterShowList();
            }
        }


    }
}
