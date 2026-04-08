using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    //20260408 未来碰撞逻辑尽量汇总到 ClashDetectionService中简化调用
    public class AdaptiveRouteEntity : ObserverableObject
    {
        public Document Document { get; }
        public FamilyInstance AdaptiveInstance { get; }
        public ElementId Id { get; }
        // 规范属性命名 (PascalCase)
        public string EntityName { get; }
        public string LevelName { get; }
        public string TotalLength { get; }
        public string EntityMark { get; }
        public ElementId LevelId { get; }
        public int IntersectWalls { get; private set; }
        public int IntersectRooms { get; private set; }
        public double MinimalDoorWidth { get; private set; } = 0;
        public IReadOnlyList<ElementId> NearDoors => _nearDoors;
        private List<ElementId> _nearDoors = new List<ElementId>();
        public AdaptiveRouteEntity(FamilyInstance familyInstance, BaseExternalHandler handler)
        {
            Document = familyInstance.Document;
            AdaptiveInstance = familyInstance;
            Id = familyInstance.Id;
            LevelId = familyInstance.LevelId;
            EntityName = familyInstance.Symbol.Family.Name;
            LevelName = familyInstance.LookupParameter("楼层标高")?.AsString() ?? "未知";
            double lengthRaw = familyInstance.LookupParameter("总长度")?.AsDouble() ?? 0;
            TotalLength = (lengthRaw * 304.8).ToString("F2");
            EntityMark = familyInstance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
            //// 仅在初始化时做轻量级计算。如果模型极大，建议将以下三行移出构造函数，改为按需点击计算
            CalculateIntersections();
        }
        public void CalculateIntersections()
        {
            // 获取自适应族的核心路径点（骨架线）
            List<XYZ> pathPoints = GetAdaptivePointPath(AdaptiveInstance);
            if (pathPoints == null || pathPoints.Count < 2) return;

            // ==========================================
            // 1. 墙体碰撞检测：直接使用原生的元素交叉过滤器 (底层支持自适应3D实体)
            // ==========================================
            var wallFilter = new ElementIntersectsElementFilter(AdaptiveInstance);
            IntersectWalls = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .WherePasses(wallFilter)
                .GetElementCount();

            // ==========================================
            // 2. 房间碰撞检测：使用房间的 Solid 过滤自适应路线
            // ==========================================
            IntersectRooms = 0;
            var rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>();
            foreach (var room in rooms)
            {
                Solid roomSolid = GetElementSolid(room); // 获取房间闭合实体
                if (roomSolid == null || roomSolid.Volume <= 0) continue;

                var roomSolidFilter = new ElementIntersectsSolidFilter(roomSolid);
                bool isIntersecting = new FilteredElementCollector(Document, new[] { AdaptiveInstance.Id })
                    .WherePasses(roomSolidFilter)
                    .Any();

                if (isIntersecting) IntersectRooms++;
            }

            // ==========================================
            // 3. 门碰撞与距离检测：包围盒初筛 + 纯数学精确相交 (得出位置和距离)
            // ==========================================
            BoundingBoxXYZ bbox = AdaptiveInstance.get_BoundingBox(null);
            if (bbox != null)
            {
                var bboxFilter = new BoundingBoxIntersectsFilter(new Outline(bbox.Min, bbox.Max));
                var candidateDoors = new FilteredElementCollector(Document)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfClass(typeof(FamilyInstance))
                    .WherePasses(bboxFilter)
                    .Cast<FamilyInstance>();

                double minWidth = double.MaxValue;

                foreach (var door in candidateDoors)
                {
                    // 获取门的实际参数宽度 (默认给3英尺防错)
                    double width = door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble() ?? 3.0;

                    // 获取门中心轴线
                    var (doorPt1, doorPt2) = GetDoorMemoryLine(door, width);

                    // 【核心】：判断门的线段与自适应路线的骨架线段是否发生交叉
                    var (segIdx, dist, sum) = CheckIntersectionPureMath(doorPt1, doorPt2, pathPoints);

                    if (segIdx >= 0) // segIdx >= 0 说明确实发生了碰撞
                    {
                        _nearDoors.Add(door.Id);

                        // 收集最小门宽
                        minWidth = Math.Min(minWidth, width);
                    }
                }
                MinimalDoorWidth = minWidth == double.MaxValue ? 0 : minWidth * 304.8;
            }
        }
        // ==== 修复后的万能获取 Solid 方法 (支持解析自适应族的 GeometryInstance) ====
        private Solid GetElementSolid(Element element)
        {
            GeometryElement geomElem = element.get_Geometry(new Options { ComputeReferences = false });
            if (geomElem == null) return null;

            foreach (GeometryObject geomObj in geomElem)
            {
                // 直接就是实体 (如房间 ClosedShell)
                if (geomObj is Solid solid && solid.Volume > 0)
                    return solid;

                // 嵌套在族实例中 (如自适应拉伸体)
                if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    foreach (GeometryObject instObj in instGeom)
                    {
                        if (instObj is Solid instSolid && instSolid.Volume > 0)
                            return instSolid;
                    }
                }
            }
            return null;
        }
        private List<XYZ> GetAdaptivePointPath(FamilyInstance adaptiveInstance)
        {
            return AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(adaptiveInstance)
                .Select(id => Document.GetElement(id) as ReferencePoint)
                .Where(rp => rp != null)
                .Select(rp => rp.Position)
                .ToList();
        }

        private (XYZ, XYZ) GetDoorMemoryLine(FamilyInstance door, double width)
        {
            if (door.Location is LocationCurve locCurve)
                return (locCurve.Curve.GetEndPoint(0), locCurve.Curve.GetEndPoint(1));

            if (door.Location is LocationPoint locPoint)
            {
                XYZ origin = locPoint.Point;
                XYZ dir = door.FacingOrientation?.Normalize() ?? XYZ.BasisX;
                XYZ perpDir = new XYZ(-dir.Y, dir.X, 0).Normalize();
                return (origin - perpDir * width / 2, origin + perpDir * width / 2);
            }
            return (XYZ.Zero, XYZ.Zero);
        }

        // 返回：(相交段落索引, 距离相交段起点的距离, 累计总距离)
        private (int segIndex, double distToStart, double sumToStart) CheckIntersectionPureMath(XYZ doorPt1, XYZ doorPt2, List<XYZ> routePts)
        {
            double totalLength = 0.0;
            for (int i = 0; i < routePts.Count - 1; i++)
            {
                XYZ p1 = routePts[i];
                XYZ p2 = routePts[i + 1];

                XYZ intersect = Get2DSegmentIntersection(doorPt1, doorPt2, p1, p2);
                if (intersect != null)
                {
                    // 拍平到XY平面计算距离 (排除Z轴拉伸高度影响)
                    double dist = new XYZ(intersect.X, intersect.Y, 0).DistanceTo(new XYZ(p1.X, p1.Y, 0));
                    return (i, dist, totalLength + dist);
                }
                totalLength += new XYZ(p1.X, p1.Y, 0).DistanceTo(new XYZ(p2.X, p2.Y, 0));
            }
            return (-1, 0, 0);
        }

        private XYZ Get2DSegmentIntersection(XYZ p1, XYZ p2, XYZ q1, XYZ q2)
        {
            double x1 = p1.X, y1 = p1.Y, x2 = p2.X, y2 = p2.Y;
            double x3 = q1.X, y3 = q1.Y, x4 = q2.X, y4 = q2.Y;
            double denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (Math.Abs(denominator) < 1e-9) return null; // 平行或共线

            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denominator;

            // 交点在两条线段内部
            if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
                return new XYZ(x1 + ua * (x2 - x1), y1 + ua * (y2 - y1), 0);

            return null;
        }
        //private void CalculateIntersections()
        //{
        //    // 1. 只获取一次自身的 Solid，极大地节省性能
        //    Solid selfSolid = GetElementSolid(AdaptiveInstance);
        //    if (selfSolid == null) return;
        //    // 2. 墙和房间交集计算 (复用一个 Solid 过滤器)
        //    var solidFilter = new ElementIntersectsSolidFilter(selfSolid);
        //    IntersectRooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms)
        //        .WherePasses(solidFilter).GetElementCount(); // 直接获取数量，不遍历
        //    IntersectWalls = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Walls)
        //        .WherePasses(solidFilter).GetElementCount();
        //    // 3. 门的相交与最小宽度计算
        //    var doors = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
        //        .WherePasses(solidFilter).Cast<FamilyInstance>().ToList();
        //    double minWidth = double.MaxValue;
        //    foreach (var door in doors)
        //    {
        //        _nearDoors.Add(door.Id);
        //        if (door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) is Parameter widthParam && widthParam.HasValue)
        //        {
        //            minWidth = Math.Min(minWidth, widthParam.AsDouble());
        //        }
        //    }
        //    MinimalDoorWidth = minWidth == double.MaxValue ? 0 : minWidth * 304.8;
        //}
        ////private Solid GetElementSolid(Element element)
        ////{
        ////    return element.get_Geometry(new Options { ComputeReferences = false })?
        ////                  .OfType<Solid>()
        ////                  .FirstOrDefault(s => s?.Volume > 0);
        ////}
    }

    //public class AdaptiveRouteEntity : ObserverableObject
    //{
    //    Document Document { get; set; }
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public AdaptiveRouteEntity(FamilyInstance familyInstance)
    //    {
    //        Document = familyInstance.Document;
    //        AdaptiveInstance = familyInstance;
    //        entityName = familyInstance.Symbol.Family.Name;
    //        Id = familyInstance.Id;
    //        levelName = familyInstance.LookupParameter("楼层标高").AsString();
    //        totalLength = (familyInstance.LookupParameter("总长度").AsDouble() * 304.8).ToString("F2");
    //        //mark单向只读，后续考虑修改
    //        entityMark = familyInstance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString();

    //        IntersectRooms = GetRoomsCrossFamilyInstance(familyInstance).Count();
    //        IntersectWalls = GetWallsCrossFamilyInstance(familyInstance).Count();
    //        NearDoors = GetDoorsNearFamilyInstance(familyInstance);
    //        if (NearDoors == null || NearDoors.Count == 0) return;
    //        ElementId minWidthDoorId = ElementId.InvalidElementId;
    //        double minWidth = double.MaxValue;
    //        foreach (ElementId doorId in NearDoors)
    //        {
    //            FamilyInstance door = Document.GetElement(doorId) as FamilyInstance;
    //            if (door == null) continue;
    //            // 方法1：从门类型参数获取宽度（推荐）
    //            if (door.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH) is Parameter widthParam && widthParam.HasValue)
    //            {
    //                double width = widthParam.AsDouble();
    //                if (width < minWidth)
    //                {
    //                    minWidth = width;
    //                    minWidthDoorId = doorId;
    //                }
    //                continue;
    //            }
    //        }
    //        MinimalDoorWidth = minWidth * 304.8;
    //    }
    //    public FamilyInstance AdaptiveInstance { get; set; }
    //    // 对外暴露只读列表
    //    public IReadOnlyList<ElementId> Doors => NearDoors;
    //    private List<ElementId> NearDoors = new List<ElementId>();
    //    public List<ElementId> GetDoorsNearFamilyInstance(FamilyInstance familyInstance)
    //    {
    //        try
    //        {
    //            // 2. 获取选中元素的包围盒
    //            var bbox = familyInstance.get_BoundingBox(null);
    //            if (bbox == null) return null;
    //            // 3. 创建空间筛选器
    //            var bboxFilter = new BoundingBoxIntersectsFilter(new Outline(bbox.Min, bbox.Max));
    //            // 4. 收集所有与包围盒相交的门窗
    //            var collector = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).WherePasses(bboxFilter);
    //            // 5. 精确几何相交检测（可选）
    //            var results = new List<ElementId>();
    //            var selfSolid = GetElementSolid(familyInstance);
    //            if (selfSolid != null)
    //            {
    //                foreach (FamilyInstance fi in collector)
    //                {
    //                    var fiSolid = GetElementSolid(fi);
    //                    if (fiSolid == null) continue;
    //                    try
    //                    {
    //                        var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(selfSolid, fiSolid, BooleanOperationsType.Intersect);
    //                        if (intersection?.Volume > 1e-9) results.Add(fi.Id);
    //                    }
    //                    catch { /* 忽略几何错误 */ }
    //                }
    //            }
    //            else
    //            {
    //                // 如果没有有效几何体，则直接返回包围盒相交结果
    //                return results = collector.ToElementIds().ToList();
    //            }
    //            return results;
    //        }
    //        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //        {
    //            return null;
    //        }
    //    }
    //    public List<ElementId> GetRoomsCrossFamilyInstance(FamilyInstance familyInstance)
    //    {
    //        List<ElementId> crossingRooms = new List<ElementId>();
    //        FilteredElementCollector rooms = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Rooms);
    //        foreach (Room room in rooms)
    //        {
    //            Solid roomSolid = GetElementSolid(room);
    //            if (roomSolid == null) continue;
    //            var filter = new ElementIntersectsSolidFilter(roomSolid);
    //            bool intersects = new FilteredElementCollector(Document).WhereElementIsNotElementType().OfClass(typeof(FamilyInstance))
    //                .WherePasses(filter).Any(e => e.Id == familyInstance.Id);
    //            if (intersects) crossingRooms.Add(room.Id);
    //        }
    //        return crossingRooms;
    //    }
    //    public List<ElementId> GetWallsCrossFamilyInstance(FamilyInstance familyInstance)
    //    {
    //        // 直接使用ElementIntersectsElementFilter
    //        var filter = new ElementIntersectsElementFilter(familyInstance);
    //        return new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Walls).WherePasses(filter).Select(w => w.Id).ToList();
    //    }
    //    private Solid GetElementSolid(Element element)
    //    {
    //        return element.get_Geometry(new Options())?.OfType<Solid>().FirstOrDefault(s => s?.Volume > 0);
    //    }
    //    private string entityMark { get; set; }
    //    public double MinimalDoorWidth { get; set; } = 0;
    //    public int IntersectWalls { get; set; } = 0;
    //    public int IntersectRooms { get; set; } = 0;
    //    public string totalLength { get; set; }
    //    public string entityName { get; set; }
    //    public string levelName { get; set; }
    //    public ElementId Id { get; set; }
    //}
}
