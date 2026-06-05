using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Obselete.RoomAttr
{
    public class DLRoomEntity : ObserverableObject
    {
        public Room room { get; set; }
        public Document Doc;
        public DLRoomEntity(List<Room> singleRoom, Document document)
        {
            room = singleRoom.FirstOrDefault();
            Doc = document;
            roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            roomAbbreviation = ChineseToSpell.GetChineseSpell(roomName);
            roomNumber = singleRoom.Count().ToString();
            int hadCodeIndex = 0;
            foreach (var item in singleRoom)
            {
                roomIds.Add(item.Id);
                // 检查特定参数是否有值
                var p = item.LookupParameter("空间编码");
                if (p != null && p.HasValue && !string.IsNullOrEmpty(p.AsString()))
                {
                    hadCodeIndex++;
                }
            }
            hasCodeNum = hadCodeIndex.ToString();
        }
        public string roomName { get; set; }
        public string roomCode { get; set; }
        public string roomAbbreviation { get; set; }
        public string roomNumber { get; set; }
        public string hasCodeNum { get; set; }
        public bool IsRoomCoded { get; set; } = false;
        public List<ElementId> roomIds { get; set; } = new List<ElementId>();
    }

}
