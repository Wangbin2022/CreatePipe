using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.RoomAttr
{
    public class RoomEntity : ObserverableObject
    {
        public Room room { get; set; }
        public Document Doc;
        public RoomEntity(List<Room> singleRoom, Document document)
        {
            room = singleRoom.FirstOrDefault();
            Doc = document;
            roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            roomAbbreviation = ChineseToSpell.GetChineseSpell(roomName);
            roomNumber = singleRoom.Count().ToString();
            foreach (var item in singleRoom)
            {
                roomIds.Add(item.Id);
            }
        }
        public string roomName { get; set; }
        public string roomCode { get; set; }
        public string roomAbbreviation { get; set; }
        public string roomNumber { get; set; }
        public bool IsRoomCoded { get; set; } = false;
        public List<ElementId> roomIds { get; set; } = new List<ElementId>();
    }

}
