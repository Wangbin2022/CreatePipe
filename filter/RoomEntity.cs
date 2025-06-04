using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreatePipe.cmd;

namespace CreatePipe.filter
{
    public class RoomEntity : ObserverableObject
    {
        public Room room { get; set; }
        public Document Doc;
        public RoomEntity(List<Room> RoomCollection, Document document)
        {
            room = RoomCollection.FirstOrDefault();
            Doc = document;
            roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            //roomAbbreviation = ChineseToSpell.GetChineseSpell(roomName);
            roomNumber = RoomCollection.Count().ToString();
            foreach (var item in RoomCollection)
            {
                roomIds.Add(item.Id);
            }

            //double类型面积
            double area = Math.Round((room.Area * 304.8 * 304.8 / (1000 * 1000)), 2);
            //楼层统计，异常高度如何确定，
            ElementId roomLevel = room.LevelId;

            //房间边界
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
            options.StoreFreeBoundaryFaces = true;
            using (Transaction ts = new Transaction(Doc))
            {
                ts.Start("房间边界");
                double z = room.Level.Elevation;
                Plane levelPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, z));
                SketchPlane sketchPlane = SketchPlane.Create(Doc, levelPlane);
                IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(options);
                foreach (IList<BoundarySegment> loop in loops)
                {
                    foreach (BoundarySegment seg in loop)
                    {
                        Doc.Create.NewModelCurve(seg.GetCurve(), sketchPlane);
                    }
                }
                ts.Commit();
            }
            //房间相关的门
            //var elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms).OfClass(typeof(Room)).Cast<Room>().ToList();
            //var elems = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            //List<FamilyInstance> doors = new List<FamilyInstance>();
            var doors = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Doors)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(elem =>
                {
                    // 安全检查FromRoom和ToRoom
                    Room fromRoom = elem.FromRoom;
                    Room toRoom = elem.ToRoom;

                    return (fromRoom != null && fromRoom.Id == room.Id) ||
                           (toRoom != null && toRoom.Id == room.Id);
                })
                .ToList();


        }
        public string roomName { get; set; }
        public string roomCode { get; set; }
        public string roomAbbreviation { get; set; }
        public string roomNumber { get; set; }
        public bool IsRoomCoded { get; set; } = false;
        public List<ElementId> roomIds { get; set; } = new List<ElementId>();
    }
}
