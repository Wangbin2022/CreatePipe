using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    //要提高效率，逻辑需要优化，反正不会变的属性应该从viewmodel传进来
    public class RoomSingleEntity : ObserverableObject
    {
        Document Document { get => Room.Document; }
        public RoomSingleEntity(Room singleRoom, List<FamilyInstance> roomDoors, List<FamilyInstance> roomWindows)
        {
            Room = singleRoom;
            roomName = singleRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            Id = singleRoom.Id;
            levelName = singleRoom.Level?.Name ?? "未指定标高";
            roomHeight = Math.Round(singleRoom.UnboundedHeight * 304.8, 0);
            roomArea = Math.Round(singleRoom.Area * 304.8 * 304.8 / 1000 / 1000, 2);
            roomNumber = singleRoom.Number;
            doorNum = roomDoors?.Count ?? 0;
            windowNum = roomWindows?.Count ?? 0;
            // 获取房间边界上的门实例及边界数量
            var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            // 改为只计算边界数量而不存储边界数据
            edgeNum = singleRoom.GetBoundarySegments(boundaryOptions).Sum(loop => loop.Count);
            // --- 门相关计算 ---
            if (doorNum > 0)
            {
                double minWidth = double.MaxValue;
                foreach (var door in roomDoors)
                {
                    // 注意：DOOR_WIDTH通常是类型参数
                    if (door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) is Parameter widthParam && widthParam.HasValue)
                    {
                        double width = widthParam.AsDouble();
                        if (width < minWidth)
                        {
                            minWidth = width;
                        }
                    }
                }
                doorWidthMin = minWidth == double.MaxValue ? 0 : Math.Round(minWidth * 304.8, 0);
            }
            else
            {
                doorWidthMin = 0;
            }
            // --- 房间类型判断 ---
            // 注意：这里的逻辑可能也需要优化，GetMaxLevelHeight()每次都算一遍也很慢
            // 最好将maxLevelHeight也作为参数传入
            if (roomDoors != null)
            {
                // 统计不重复的LevelId
                var levelIds = new HashSet<ElementId>(roomDoors.Select(d => d.LevelId).Where(id => id != ElementId.InvalidElementId));
                if (levelIds.Count > 1)
                {
                    RoomType = "垂直交通空间";
                }
                else if (doorNum > 4)
                {
                    RoomType = "水平交通空间";
                }
            }

            if (doorNum == 0)
            {
                RoomType = "密闭空间";
            }


            //var bbox = singleRoom.get_BoundingBox(null);
            //var outline = new Outline(bbox.Min, bbox.Max);
            ////// 先筛选可能在房间附近的门
            //var nearbyDoors = new FilteredElementCollector(singleRoom.Document).OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
            //    .WherePasses(new BoundingBoxIntersectsFilter(outline)).Cast<FamilyInstance>();
            //// 再进行精确匹配
            //////doorNum = nearbyDoors.Count(door => (door.ToRoom?.Id == singleRoom.Id) || (door.FromRoom?.Id == singleRoom.Id));
            //List<FamilyInstance> doors = new List<FamilyInstance>();
            //foreach (var door in nearbyDoors)
            //{
            //    if ((door.ToRoom?.Id == singleRoom.Id) || (door.FromRoom?.Id == singleRoom.Id))
            //    {
            //        doorNum++;
            //        doors.Add(door);
            //    }
            //}
            //double minWidth = double.MaxValue;
            //foreach (var door in doors)
            //{
            //    if (door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) is Parameter widthParam && widthParam.HasValue)
            //    {
            //        double width = widthParam.AsDouble();
            //        if (width < minWidth)
            //        {
            //            minWidth = width;
            //        }
            //        continue;
            //    }
            //}
            //doorWidthMin = minWidth * 304.8;

            ////var doors = nearbyDoors.Where(d => d.ToRoom?.Id == singleRoom.Id || d.FromRoom?.Id == singleRoom.Id).ToList();
            ////doorWidthMin = doors.Select(d => d.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble() ?? double.MaxValue)
            ////                    .DefaultIfEmpty(double.MaxValue).Min() * 304.8;
            //var nearbyWindows = new FilteredElementCollector(singleRoom.Document).OfCategory(BuiltInCategory.OST_Windows).OfClass(typeof(FamilyInstance))
            //    .WherePasses(new BoundingBoxIntersectsFilter(outline)).Cast<FamilyInstance>();
            //windowNum = nearbyWindows.Count(win => (win.ToRoom?.Id == singleRoom.Id) || (win.FromRoom?.Id == singleRoom.Id));

            //double maxSpacing = GetMaxLevelHeight();
            //// 统计 nearbyDoors 中所有不重复的 LevelId
            //HashSet<ElementId> levelIds = new HashSet<ElementId>();
            //foreach (FamilyInstance door in nearbyDoors)
            //{
            //    if (door.LevelId != ElementId.InvalidElementId)
            //    {
            //        levelIds.Add(door.LevelId);
            //    }
            //}
            //if (levelIds.Count > 1)
            //{
            //    RoomType = "垂直交通空间";
            //}
            //if (nearbyDoors.Count() > 4)
            //{
            //    RoomType = "水平交通空间";
            //}
            //if (nearbyDoors.Count() == 0)
            //{
            //    RoomType = "密闭空间";
            //}
        }
        private double GetMaxLevelHeight()
        {
            var levels = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
            if (levels.Count < 2) return 0;
            // 计算相邻标高间距并找出最大值
            double maxSpacing = 0;
            Level lowerLevel = null;
            Level upperLevel = null;
            for (int i = 0; i < levels.Count - 1; i++)
            {
                double spacing = levels[i + 1].Elevation - levels[i].Elevation;
                if (spacing > maxSpacing)
                {
                    maxSpacing = spacing;
                    lowerLevel = levels[i];
                    upperLevel = levels[i + 1];
                }
            }
            return maxSpacing;
        }
        public double doorWidthMin { get; set; } = 0;
        public string RoomType { get; set; }
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
        public Room Room { get; set; }
    }
}
