using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class RoomSingleEntity : ObserverableObject
    {
        public RoomSingleEntity(Room singleRoom)
        {
            roomName = singleRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            Id = singleRoom.Id;
            levelName = singleRoom.Level.Name;
            roomHeight = Math.Round(singleRoom.UnboundedHeight * 304.8, 0);
            roomArea = Math.Round(singleRoom.Area * 304.8 * 304.8 / 1000 / 1000, 2);
            roomNumber = singleRoom.Number;

            // 获取房间边界上的门实例及边界数量
            var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            var boundarySegments = singleRoom.GetBoundarySegments(boundaryOptions);
            foreach (var boundaryLoop in boundarySegments)
            {
                foreach (var boundarySegment in boundaryLoop)
                {
                    edgeNum++;
                }
            }
            var bbox = singleRoom.get_BoundingBox(null);
            var outline = new Outline(bbox.Min, bbox.Max);
            // 先筛选可能在房间附近的门
            var nearbyDoors = new FilteredElementCollector(singleRoom.Document).OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
                .WherePasses(new BoundingBoxIntersectsFilter(outline)).Cast<FamilyInstance>();
            // 再进行精确匹配
            doorNum = nearbyDoors.Count(door => (door.ToRoom?.Id == singleRoom.Id) || (door.FromRoom?.Id == singleRoom.Id));
            var nearbyWindows = new FilteredElementCollector(singleRoom.Document).OfCategory(BuiltInCategory.OST_Windows).OfClass(typeof(FamilyInstance))
                .WherePasses(new BoundingBoxIntersectsFilter(outline)).Cast<FamilyInstance>();
            windowNum = nearbyWindows.Count(win => (win.ToRoom?.Id == singleRoom.Id) || (win.FromRoom?.Id == singleRoom.Id));
        }
        public int edgeNum { get; set; } = 0;
        public int doorNum { get; set; } = 0;
        public int windowNum { get; set; } = 0;
        public string roomNumber { get; set; }
        public bool IsFacingOutRoom { get; set; } = true;
        public double roomHeight { get; set; }
        public double roomArea { get; set; }
        public string roomName { get; set; }
        public string levelName { get; set; }
        public ElementId Id { get; set; }

    }
}
