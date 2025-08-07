using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class AdaptiveRouteEntity : ObserverableObject
    {
        Document Document { get; set; }
        public AdaptiveRouteEntity(FamilyInstance familyInstance)
        {
            Document = familyInstance.Document;
            AdaptiveInstance = familyInstance;
            entityName = familyInstance.Symbol.Family.Name;
            Id = familyInstance.Id;
            levelName = familyInstance.LookupParameter("楼层标高").AsString();
            totalLength = (familyInstance.LookupParameter("总长度").AsDouble() * 304.8).ToString("F2");
            entityMark = familyInstance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString();

            IntersectRooms = GetRoomsCrossFamilyInstance(familyInstance).Count();
            IntersectWalls = GetWallsCrossFamilyInstance(familyInstance).Count();
            NearDoors = GetDoorsNearFamilyInstance(familyInstance);
            if (NearDoors == null || NearDoors.Count == 0) return;
            ElementId minWidthDoorId = ElementId.InvalidElementId;
            double minWidth = double.MaxValue;
            foreach (ElementId doorId in NearDoors)
            {
                FamilyInstance door = Document.GetElement(doorId) as FamilyInstance;
                if (door == null) continue;
                // 方法1：从门类型参数获取宽度（推荐）
                if (door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) is Parameter widthParam && widthParam.HasValue)
                {
                    double width = widthParam.AsDouble();
                    if (width < minWidth)
                    {
                        minWidth = width;
                        minWidthDoorId = doorId;
                    }
                    continue;
                }
            }
            MinimalDoorWidth = minWidth * 304.8;
        }
        public FamilyInstance AdaptiveInstance { get; set; }
        // 对外暴露只读列表
        public IReadOnlyList<ElementId> Doors => NearDoors;
        private List<ElementId> NearDoors = new List<ElementId>();
        public List<ElementId> GetDoorsNearFamilyInstance(FamilyInstance familyInstance)
        {
            try
            {
                // 2. 获取选中元素的包围盒
                var bbox = familyInstance.get_BoundingBox(null);
                if (bbox == null) return null;
                // 3. 创建空间筛选器
                var bboxFilter = new BoundingBoxIntersectsFilter(new Outline(bbox.Min, bbox.Max));
                // 4. 收集所有与包围盒相交的门窗
                var collector = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).WherePasses(bboxFilter);
                // 5. 精确几何相交检测（可选）
                var results = new List<ElementId>();
                var selfSolid = GetElementSolid(familyInstance);
                if (selfSolid != null)
                {
                    foreach (FamilyInstance fi in collector)
                    {
                        var fiSolid = GetElementSolid(fi);
                        if (fiSolid == null) continue;
                        try
                        {
                            var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(selfSolid, fiSolid, BooleanOperationsType.Intersect);
                            if (intersection?.Volume > 1e-9) results.Add(fi.Id);
                        }
                        catch { /* 忽略几何错误 */ }
                    }
                }
                else
                {
                    // 如果没有有效几何体，则直接返回包围盒相交结果
                    return results = collector.ToElementIds().ToList();
                }
                return results;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }
        public List<ElementId> GetRoomsCrossFamilyInstance(FamilyInstance familyInstance)
        {
            List<ElementId> crossingRooms = new List<ElementId>();
            FilteredElementCollector rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Room room in rooms)
            {
                Solid roomSolid = GetElementSolid(room);
                if (roomSolid == null) continue;
                var filter = new ElementIntersectsSolidFilter(roomSolid);
                bool intersects = new FilteredElementCollector(Document).WhereElementIsNotElementType().OfClass(typeof(FamilyInstance))
                    .WherePasses(filter).Any(e => e.Id == familyInstance.Id);
                if (intersects) crossingRooms.Add(room.Id);
            }
            return crossingRooms;
        }
        public List<ElementId> GetWallsCrossFamilyInstance(FamilyInstance familyInstance)
        {
            // 直接使用ElementIntersectsElementFilter
            var filter = new ElementIntersectsElementFilter(familyInstance);
            return new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Walls).WherePasses(filter).Select(w => w.Id).ToList();
        }
        private Solid GetElementSolid(Element element)
        {
            return element.get_Geometry(new Options())?.OfType<Solid>().FirstOrDefault(s => s?.Volume > 0);
        }
        public string entityMark { get; set; }
        public double MinimalDoorWidth { get; set; } = 0;
        public int IntersectWalls { get; set; } = 0;
        public int IntersectRooms { get; set; } = 0;
        public string totalLength { get; set; }
        public string entityName { get; set; }
        public string levelName { get; set; }
        public ElementId Id { get; set; }
    }
}
