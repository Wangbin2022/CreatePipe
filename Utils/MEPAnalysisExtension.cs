using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    public static class MEPAnalysisExtension
    {
        /// <summary>
        /// 判断两条直线是否平行（支持空间向量）
        /// </summary>
        public static bool IsParallelTo(this Line l1, Line l2)
        {
            return l1.Direction.IsAlmostEqualTo(l2.Direction) ||
                   l1.Direction.IsAlmostEqualTo(l2.Direction.Negate());
        }
        /// <summary>
        /// 判断两条直线是否共线（在三维空间中）
        /// </summary>
        public static bool IsCollinear(this Line l1, Line l2, double tolerance = 0.001)
        {
            // 首先检查方向是否平行
            if (!IsParallelTo(l1, l2))
                return false;

            // 检查直线1的起点到直线2的距离（三维空间中点到直线的距离）
            double distanceStart = DistanceFromPointToLine(l1.Origin, l2);
            if (distanceStart > tolerance)
                return false;

            // 检查直线1的终点到直线2的距离（确保整条直线都在同一直线上）
            double distanceEnd = DistanceFromPointToLine(l1.GetEndPoint(1), l2);
            if (distanceEnd > tolerance)
                return false;

            return true;
        }
        /// <summary>
        /// 计算点到直线（线段无限延伸）的三维距离
        /// </summary>
        public static double DistanceFromPointToLine(XYZ point, Line line)
        {
            XYZ lineDirection = line.Direction.Normalize();
            XYZ originToPoint = point - line.Origin;

            // 向量投影的长度
            double projectionLength = originToPoint.DotProduct(lineDirection);

            // 垂足点
            XYZ footPoint = line.Origin + projectionLength * lineDirection;

            // 点到垂足的距离
            return point.DistanceTo(footPoint);
        }
        //判断点是否在线段上
        public static bool IsPointOnCurveSegment(Curve curve, XYZ point, double tolerance = 1e-6)
        {
            if (point == null || curve == null) return false;
            // 保持原逻辑：只判断直线段
            Line line = curve as Line;
            if (line == null)
            {
                TaskDialog.Show("tt", "选择线段非直线");
                return false;
            }
            // 如果要求是线段，建议必须是有界 Line
            if (!line.IsBound) return false;
            // 1. 先判断到直线的距离
            if (DistanceFromPointToLine(point, line) > tolerance) return false;
            // 2. 再判断投影是否在线段范围内
            XYZ start = line.GetEndPoint(0);
            XYZ direction = line.Direction;
            double projectionLength = (point - start).DotProduct(direction);
            return projectionLength >= -tolerance &&
                   projectionLength <= line.Length + tolerance;
        }
        //判断点投影是否在线段上
        public static bool IsProjectedPointOnCurveSegment(Curve curve, XYZ point, double parameterTolerance = 1e-6)
        {
            if (curve == null || point == null) return false;
            if (!curve.IsBound) return false;
            try
            {
                IntersectionResult result = curve.Project(point);
                if (result == null) return false;
                double startParam = curve.GetEndParameter(0);
                double endParam = curve.GetEndParameter(1);
                double minParam = Math.Min(startParam, endParam);
                double maxParam = Math.Max(startParam, endParam);
                double projectedParam = result.Parameter;
                return projectedParam >= minParam - parameterTolerance &&
                       projectedParam <= maxParam + parameterTolerance;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 判断两条直线在指定轴上是否重叠（可选，用于更精细的控制）
        /// </summary>
        public static bool HasOverlapAlongDirection(this Line l1, Line l2, double tolerance = 0.001)
        {
            if (!IsCollinear(l1, l2, tolerance)) return false;
            // 获取投影参数 t
            XYZ direction = l1.Direction.Normalize();
            XYZ basePoint = l1.GetEndPoint(0);
            double t1_start = (l1.GetEndPoint(0) - basePoint).DotProduct(direction);
            double t1_end = (l1.GetEndPoint(1) - basePoint).DotProduct(direction);
            double t2_start = (l2.GetEndPoint(0) - basePoint).DotProduct(direction);
            double t2_end = (l2.GetEndPoint(1) - basePoint).DotProduct(direction);
            // 确保有序
            double t1_min = Math.Min(t1_start, t1_end);
            double t1_max = Math.Max(t1_start, t1_end);
            double t2_min = Math.Min(t2_start, t2_end);
            double t2_max = Math.Max(t2_start, t2_end);
            // 检查是否重叠
            return !(t1_max < t2_min - tolerance || t2_max < t1_min - tolerance);
        }
        //安全获取元素类别
        public static string GetCatNameSafe(Element owner)
        {
            if (owner == null) return "<null>";
            try
            {
                if (owner.Category != null && !string.IsNullOrWhiteSpace(owner.Category.Name))
                    return owner.Category.Name;
                return owner.GetType().Name;
            }
            catch
            {
                return "<unknown>";
            }
        }
        /// <summary>
        /// 判断管线是否水平
        /// </summary>
        public static bool IsHorizontal(this MEPCurve mep)
        {
            if (mep.Location is LocationCurve lc && lc.Curve is Line line)
            {
                return Math.Abs(line.Direction.Z) < 0.001;
            }
            return false;
        }
        /// <summary>
        /// 判断MEPCurve是否为垂直的
        /// </summary>
        public static bool IsVertical(MEPCurve mepCurve)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve)) return false;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);
            double tolerance = 0.001;
            return Math.Abs(start.X - end.X) < tolerance && Math.Abs(start.Y - end.Y) < tolerance;
        }
        /// <summary>
        /// 获取两条直线在XY平面上的投影交点
        /// </summary>
        public static XYZ GetIntersectionPoint2D(this Line line1, Line line2, double tolerance = 0.001)
        {
            XYZ p1 = line1.GetEndPoint(0);
            XYZ p2 = line1.GetEndPoint(1);
            XYZ q1 = line2.GetEndPoint(0);
            XYZ q2 = line2.GetEndPoint(1);
            XYZ d1 = p2 - p1;
            XYZ d2 = q2 - q1;
            double det = d1.X * d2.Y - d1.Y * d2.X;
            if (Math.Abs(det) < tolerance) return null; // 平行或共线，无唯一交点
            XYZ q1p1 = q1 - p1;
            double t = (q1p1.X * d2.Y - q1p1.Y * d2.X) / det;
            return p1 + t * d1;
        }
        /// <summary>
        /// 获取管线的主要尺寸（直径或宽度）
        /// </summary>
        public static double GetMainSize(this MEPCurve mep)
        {
            switch (mep)
            {
                case Pipe p:
                    return p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                case Duct d:
                    return d.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.AsDouble() ??
                           d.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                case CableTray ct:
                    return ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.AsDouble() ?? 0;
                default:
                    return 0;
            }
        }
        /// <summary>
        /// 获取元素的 ConnectorManager（MEPCurve / FamilyInstance 通用）
        /// </summary>
        public static ConnectorManager GetConnectorManager(this Element element)
        {
            if (element is MEPCurve curve) return curve.ConnectorManager;
            if (element is FamilyInstance fi) return fi.MEPModel?.ConnectorManager;
            return null;
        }
        /// <summary>
        /// 获取元素所有的连接器（支持管线和族实例）
        /// </summary>
        public static IEnumerable<Connector> GetConnectors(this Element element)
        {
            if (element == null) yield break;
            ConnectorManager cm = GetConnectorManager(element);
            if (cm == null) yield break;
            foreach (Connector conn in cm.Connectors)
            {
                yield return conn;
            }
        }
        /// <summary>
        /// 获取离指定点最近的连接器
        /// </summary>
        public static Connector GetClosestConnector(this Element element, XYZ point, double tolerance = 0.001)
        {
            var conns = element.GetConnectors();
            Connector closest = null;
            double minDistance = double.MaxValue;
            foreach (var conn in conns)
            {
                double dist = conn.Origin.DistanceTo(point);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = conn;
                }
            }
            return (closest != null && minDistance <= tolerance) ? closest : closest;
        }
        //获取Curve与Instance间最近连接器
        public static Connector GetClosestConnector(MEPCurve curve, FamilyInstance newSprinklerInstance)
        {
            // 获取水平管道的所有连接器
            var connectors = curve.ConnectorManager.Connectors.OfType<Connector>().ToList();
            // 获取新喷头的位置
            XYZ newSprinklerOrigin = ((LocationPoint)newSprinklerInstance.Location).Point;
            // 找到最近的连接器
            Connector closestConn = null;
            double minDistance = double.MaxValue;
            foreach (var conn in connectors)
            {
                double distance = conn.Origin.DistanceTo(newSprinklerOrigin);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestConn = conn;
                }
            }
            return closestConn;
        }
        /// <summary>
        /// 找最近的连接器并返回这对连接器。如果未找到返回 (null, null)
        /// </summary>
        public static (Connector c1, Connector c2) GetClosestConnectorsTuple(List<Connector> list1, List<Connector> list2)
        {
            if (list1 == null || list2 == null || list1.Count == 0 || list2.Count == 0)
                return (null, null);
            double minDistance = double.MaxValue;
            Connector closest1 = null;
            Connector closest2 = null;
            foreach (Connector c1 in list1)
            {
                if (c1 == null) continue;
                foreach (Connector c2 in list2)
                {
                    if (c2 == null) continue;

                    double currentDistance = c1.Origin.DistanceTo(c2.Origin);
                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        closest1 = c1;
                        closest2 = c2;
                    }
                }
            }
            return (closest1, closest2);
        }
        /// <summary>
        /// 获取第一个未使用的连接器
        /// </summary>
        public static Connector GetUnusedConnector(this Element element)
            => element.GetConnectors().FirstOrDefault(c => !c.IsConnected);
        /// <summary>
        /// 获取元素上所有未连接的 Connector
        /// </summary>
        public static IReadOnlyList<Connector> GetUnusedConnectors(this Element element)
        {
            ConnectorManager cm = element?.GetConnectorManager();
            if (cm == null)
                return Array.Empty<Connector>();
            var result = new List<Connector>();
            foreach (Connector conn in cm.Connectors)
            {
                if (conn.ConnectorType == ConnectorType.Logical)
                    continue;
                if (!conn.IsConnected)
                    result.Add(conn);
            }
            return result;
        }
        /// <summary>
        /// 获取与当前连接器物理相连的其他元素的连接器
        /// </summary>
        public static IEnumerable<Connector> GetConnectedRefs(this Connector connector)
        {
            if (!connector.IsConnected) yield break;
            foreach (Connector refConn in connector.AllRefs)
            {
                // 排除系统逻辑连接、自身及无效连接
                if (refConn.ConnectorType == ConnectorType.Logical) continue;
                if (refConn.Owner.Id == connector.Owner.Id) continue;
                yield return refConn;
            }
        }
        /// <summary>
        /// 获取管件（弯头/三通/四通）相连的所有邻居连接器
        /// </summary>
        public static List<Connector> GetNeighborConnectors(this FamilyInstance fitting)
        {
            return fitting.GetConnectors().Where(c => c.IsConnected).SelectMany(c => c.GetConnectedRefs()).ToList();
        }
        /// <summary>
        /// 找出三通的侧向接口（通过方向判断：反向的两个为主干，剩下为侧向）推荐！
        /// </summary>
        public static Connector GetTeeSideConn(FamilyInstance teeFitting)
        {
            var conns = MEPAnalysisExtension.GetConnectors(teeFitting).ToList();
            if (conns == null || conns.Count != 3) return null;
            Connector c0 = conns[0], c1 = conns[1], c2 = conns[2];
            // 获取三个连接器的朝向向量
            XYZ v0 = c0.CoordinateSystem.BasisZ;
            XYZ v1 = c1.CoordinateSystem.BasisZ;
            XYZ v2 = c2.CoordinateSystem.BasisZ;
            // 向量点积。当两个向量夹角为180度(反向)时，点积为 -1
            // 考虑到绘图可能存在的微小误差，使用 < -0.9 作为平行反向的容差判断
            if (v0.DotProduct(v1) < -0.9) return c2; // 0和1是主干，2是侧边
            if (v0.DotProduct(v2) < -0.9) return c1; // 0和2是主干，1是侧边
            if (v1.DotProduct(v2) < -0.9) return c0; // 1和2是主干，0是侧边
            // 如果没有找到呈180度的（比如特殊的三向交汇角），回退到距离判断
            return GetTeeSideConn_ByDistance(teeFitting);
        }
        /// <summary>
        /// 找出三通的侧向接口（通过距离判断：距离最远的两个为主干，剩下为侧向）
        /// </summary>
        public static Connector GetTeeSideConn_ByDistance(FamilyInstance teeFitting)
        {
            var conns = MEPAnalysisExtension.GetConnectors(teeFitting).ToList();

            // 1. 安全检查：如果不是3个接口，直接返回 null
            if (conns == null || conns.Count != 3) return null;

            Connector c0 = conns[0], c1 = conns[1], c2 = conns[2];

            // 2. 分别计算三条边的距离
            double d01 = c0.Origin.DistanceTo(c1.Origin);
            double d02 = c0.Origin.DistanceTo(c2.Origin);
            double d12 = c1.Origin.DistanceTo(c2.Origin);

            // 3. 找出距离最大的一对，剩下的那个就是侧边接口
            if (d01 >= d02 && d01 >= d12) return c2; // 0和1最远，2是侧边
            if (d02 >= d01 && d02 >= d12) return c1; // 0和2最远，1是侧边

            return c0; // 1和2最远，0是侧边
        }
        /// <summary>
        /// 检查两个连接器是否真的连在一起
        /// </summary>
        public static bool IsActuallyConnectedTo(this Connector c1, Connector c2)
        {
            if (c1 == null || c2 == null || !c1.IsConnected || !c2.IsConnected) return false;
            // 使用 ElementId 直接比较，不要使用 .IntegerValue
            return c1.AllRefs.Cast<Connector>().Any(r => r.Owner.Id == c2.Owner.Id && r.Id == c2.Id);
        }
        /// <summary>
        /// 断开连接并返回对方的连接器对象
        /// </summary>
        public static Connector SafeDisconnect(this Connector source)
        {
            Connector target = source.GetConnectedRefs().FirstOrDefault();
            if (target != null)
            {
                source.DisconnectFrom(target);
                return target;
            }
            return null;
        }
        public static void SafeDisconnect(Connector a, Connector b)
        {
            if (a == null || b == null) return;

            try
            {
                if (a.IsConnectedTo(b))
                {
                    a.DisconnectFrom(b);
                }
            }
            catch
            {
                try
                {
                    if (b.IsConnectedTo(a))
                    {
                        b.DisconnectFrom(a);
                    }
                }
                catch
                {
 
                }
            }
        }
        /// <summary>
        /// 获取指定位置的连接器
        /// </summary>
        public static Connector GetConnectorAtPoint(MEPCurve curve, XYZ point)
        {
            if (curve == null || point == null) return null;
            ConnectorManager cm = curve.ConnectorManager;
            foreach (Connector c in cm.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(point))
                {
                    return c;
                }
            }
            return null;
        }
        //0516 添加相关方法测试
        public static XYZ GetConnectorDirection(Connector c)
        {
            try
            {
                if (c == null) return XYZ.BasisX;
                //CoordinateSystem cs = c.CoordinateSystem;
                if (c.CoordinateSystem != null)
                {
                    // 常用 BasisZ 表示连接方向，但不同族不完全统一
                    XYZ dir = c.CoordinateSystem.BasisZ;
                    if (dir != null && dir.GetLength() > 1e-6)
                        return dir.Normalize();
                }
            }
            catch
            {
            }
            return XYZ.BasisX;
        }
        /// <summary>
        /// 合并两根等径共线管线
        /// </summary>
        public static void MergeTwoPipes(Document doc, MEPCurve m1, Connector c1, MEPCurve m2, Connector c2)
        {
            if (m1.Category.Id != m2.Category.Id)
            {
                TaskDialog.Show("提示", "请选择相同类别的管线"); return;
            }
            // 找到 m1 的另一端 (非点击端)
            Connector m1Far = MEPAnalysisExtension.GetConnectors(m1).FirstOrDefault(c => !c.Origin.IsAlmostEqualTo(c1.Origin));
            // 找到 m2 的另一端 (非点击端)
            Connector m2Far = MEPAnalysisExtension.GetConnectors(m2).FirstOrDefault(c => !c.Origin.IsAlmostEqualTo(c2.Origin));
            if (m1Far == null || m2Far == null) return;
            // 记录 m2Far 之前的连接状态，准备后续重连
            Connector m2OriginalNeighbor = MEPAnalysisExtension.GetConnectedRefs(m2Far).FirstOrDefault();
            // 获取两端点
            XYZ p1 = m1Far.Origin;
            XYZ p2 = m2Far.Origin;
            // ★关键修复★：保持管线原始的绘制方向，防止反向导致原有系统连接断裂
            XYZ originalDirection = ((m1.Location as LocationCurve).Curve as Line).Direction;
            XYZ newDirection = (p2 - p1).Normalize();
            // 如果新线方向与老线方向相反，则调换起点和终点
            if (originalDirection.DotProduct(newDirection) < 0)
            {
                XYZ temp = p1;
                p1 = p2;
                p2 = temp;
            }
            // 更新 m1 的长度
            LocationCurve lc1 = m1.Location as LocationCurve;
            lc1.Curve = Line.CreateBound(p1, p2);
            // 删除 m2
            doc.Delete(m2.Id);
            // 恢复 m2 原本的末端连接
            if (m2OriginalNeighbor != null)
            {
                // 在 m1 新生成的连接器中，找到位置最靠近原 m2Far 的那个端点
                Connector m1NewEnd = MEPAnalysisExtension.GetConnectors(m1)
                    .OrderBy(c => c.Origin.DistanceTo(m2Far.Origin))
                    .First();

                // 如果还没连上，就尝试连接
                if (!m1NewEnd.IsConnected)
                {
                    m1NewEnd.ConnectTo(m2OriginalNeighbor);
                }
            }
        }
        //管道管件检验
        public static bool IsPipeFitting(Element elem)
        {
            if (elem == null) return false;
            Category cat = elem.Category;
            if (cat == null) return false;
            return cat.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting;
        }
        //断开所有连接
        public static void DisconnectConnector(Connector connector)
        {
            if (connector == null) return;
            List<Connector> refs = new List<Connector>();
            foreach (Connector c in connector.AllRefs)
            {
                if (c != null && c.Owner != null && c.Owner.Id != connector.Owner.Id)
                    refs.Add(c);
            }
            foreach (var rc in refs)
            {
                try
                {
                    connector.DisconnectFrom(rc);
                }
                catch
                {
                    // 忽略
                }
            }
        }
        // 使用广度优先遍历(BFS)获取所有相连元素包括curve和族实例
        public static List<ElementId> GetAllConnectedElements(List<Connector> startConnectors)
        {
            if (startConnectors == null || startConnectors.Count == 0)
                return new List<ElementId>();

            var result = new List<ElementId>();
            var processedElements = new HashSet<ElementId>();
            var queuedElements = new HashSet<ElementId>();
            var queue = new Queue<Element>();

            var startElementIds = new HashSet<ElementId>(startConnectors
                .Where(c => c != null && c.Owner != null).Select(c => c.Owner.Id));
            foreach (var connector in startConnectors)
            {
                if (connector?.Owner != null && queuedElements.Add(connector.Owner.Id))
                {
                    queue.Enqueue(connector.Owner);
                }
            }
            while (queue.Count > 0)
            {
                Element currentElement = queue.Dequeue();
                //BFS 图遍历中的去重逻辑
                if (currentElement == null || processedElements.Contains(currentElement.Id))
                    continue;
                //// 标记当前元素已处理
                //processedElements.Add(currentElement.Id);
                // 如果不是初始的风口，添加到要删除的列表
                if (!startElementIds.Contains(currentElement.Id))
                {
                    result.Add(currentElement.Id);
                }
                // 获取当前连接器的所有连接
                ConnectorSet connectorSet = null;
                if (currentElement is FamilyInstance familyInstance && familyInstance.MEPModel != null)
                {
                    connectorSet = familyInstance.MEPModel.ConnectorManager.Connectors;
                }
                else if (currentElement is MEPCurve mepCurve)
                {
                    connectorSet = mepCurve.ConnectorManager.Connectors;
                }
                if (connectorSet == null) continue;
                foreach (Connector connector in connectorSet)
                {
                    // 获取连接器连接到的其他连接器
                    foreach (Connector connectedConnector in connector.AllRefs)
                    {
                        Element connectedElement = connectedConnector?.Owner;
                        if (connectedElement == null) continue;
                        if (connectedElement.Id == currentElement.Id) continue;

                        if (!processedElements.Contains(connectedElement.Id) &&
                            queuedElements.Add(connectedElement.Id))
                        {
                            //添加入列
                            queue.Enqueue(connectedElement);
                        }
                    }
                }
            }
            return result;
        }
        //查找一定范围内MepCurve
        public static IList<MEPCurve> GetMEPCurvesAtPoint(Document doc, XYZ point, double offsetHeight, double detectRange = 1.0)
        {
            var result = new List<MEPCurve>();
            try
            {
                if (doc == null || point == null) return result;
                View activeView = doc.ActiveView;
                Level currentLevel = activeView.GenLevel;
                if (currentLevel == null || activeView == null)
                {
                    TaskDialog.Show("错误", "当前视图没有关联标高，请在楼层平面视图中执行。");
                    return result;
                }
                // 注意：offsetHeight 必须是 Revit 内部单位，单位是英尺
                double targetElevation = currentLevel.Elevation + offsetHeight / 304.8;
                // 搜索高度范围
                // 原代码是上下各 1.0 英尺，约 304.8mm
                double searchHalfHeight = detectRange / 304.8;
                XYZ projectionStart = new XYZ(point.X, point.Y, targetElevation + searchHalfHeight);
                XYZ projectionEnd = new XYZ(point.X, point.Y, targetElevation - searchHalfHeight);
                Line verticalLine = Line.CreateBound(projectionStart, projectionEnd);
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(MEPCurve)).WhereElementIsNotElementType();
                foreach (MEPCurve mepCurve in collector)
                {
                    LocationCurve locationCurve = mepCurve.Location as LocationCurve;
                    if (locationCurve == null || mepCurve == null) continue;
                    Curve curve = locationCurve.Curve;
                    if (curve == null) continue;
                    IntersectionResultArray intersectionResults;
                    SetComparisonResult comparisonResult = curve.Intersect(verticalLine, out intersectionResults);
                    if (intersectionResults != null && intersectionResults.Size > 0)
                    {
                        result.Add(mepCurve);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("查找 MEP 曲线错误", ex.Message);
                return result;
            }
        }
        // 管道退后方法
        public static void RetreatMEPCurve(MEPCurve mepCurve, XYZ breakPoint, double retreatDistance)
        {
            try
            {
                LocationCurve locationCurve = mepCurve.Location as LocationCurve;
                if (locationCurve == null) return;
                Curve currentCurve = locationCurve.Curve;
                XYZ startPoint = currentCurve.GetEndPoint(0);
                XYZ endPoint = currentCurve.GetEndPoint(1);
                // 确定退后方向
                XYZ curveDirection = (endPoint - startPoint).Normalize();
                // 根据退后距离的正负决定退后方向
                XYZ retreatVector = curveDirection * retreatDistance;
                // 判断哪一端靠近打断点
                double distanceToStart = breakPoint.DistanceTo(startPoint);
                double distanceToEnd = breakPoint.DistanceTo(endPoint);
                Curve newCurve;
                if (distanceToStart < distanceToEnd)
                {
                    // 靠近起点端，移动起点
                    XYZ newStartPoint = startPoint + retreatVector;
                    // 确保新起点在曲线上且不超出范围
                    if (MEPAnalysisExtension.IsPointOnCurveSegment(currentCurve, newStartPoint))
                    {
                        newCurve = Line.CreateBound(newStartPoint, endPoint);
                    }
                    else
                    {
                        // 如果无法退后，保持原曲线
                        newCurve = currentCurve;
                    }
                }
                else
                {
                    // 靠近终点端，移动终点
                    XYZ newEndPoint = endPoint + retreatVector;
                    // 确保新终点在曲线上且不超出范围
                    if (MEPAnalysisExtension.IsPointOnCurveSegment(currentCurve, newEndPoint))
                    {
                        newCurve = Line.CreateBound(startPoint, newEndPoint);
                    }
                    else
                    {
                        // 如果无法退后，保持原曲线
                        newCurve = currentCurve;
                    }
                }
                locationCurve.Curve = newCurve;
            }
            catch (Exception ex)
            {
                // 退后失败时忽略错误，继续执行
                System.Diagnostics.Debug.WriteLine($"管道退后失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 从管件的连接器出发，获取所有“外部相邻”的连接器。
        /// 不返回管件自身连接器。
        /// 可用于弯头、直接头、变径、三通、四通等。
        /// </summary>
        public static List<Connector> GetExternalNeighborConnectors(FamilyInstance fitting)
        {
            var result = new List<Connector>();
            if (fitting?.MEPModel?.ConnectorManager == null) return result;
            ConnectorSet fitConns = fitting.MEPModel.ConnectorManager.Connectors;
            if (fitConns == null) return result;
            var seen = new HashSet<string>();
            foreach (Connector fitConn in fitConns)
            {
                if (fitConn == null || !fitConn.IsConnected) continue;
                foreach (Connector refConn in fitConn.AllRefs)
                {
                    if (refConn == null || refConn.Owner == null) continue;
                    // 排除自身
                    if (refConn.Owner.Id == fitting.Id) continue;
                    //这里不过度限制 Domain，避免附件/设备遗漏
                    //如果只处理风管系统可添加
                    //if (refConn.Domain != Domain.DomainHvac) continue;
                    string key = $"{refConn.Owner.Id.IntegerValue}:{refConn.Id}";
                    if (seen.Add(key))
                        result.Add(refConn);
                }
            }
            return result;
        }
        /// <summary>
        /// 只获取两端管件的外部相邻连接器。
        /// 如果外部连接器数量不是 2，则返回空集合。
        /// </summary>
        public static List<Connector> GetTwoEndNeighborConnectors(FamilyInstance fitting)
        {
            List<Connector> connectors = GetExternalNeighborConnectors(fitting);
            return connectors.Count == 2 ? connectors : new List<Connector>();
        }
        /// <summary>
        /// 尝试 cFrom.ConnectTo(cTo)，并通过 AllRefs 验证两者是否真正互相引用。
        /// 注意：调用方需要确保当前处于 Transaction 中。
        /// </summary>
        public static bool TryConnectAndVerify(Document doc, Connector cFrom, Connector cTo)
        {
            if (doc == null || cFrom == null || cTo == null) return false;
            if (cFrom.Owner?.Id == cTo.Owner?.Id) return false;
            // 已经互相连接，直接认为成功
            if (IsActuallyConnected(cFrom, cTo)) return true;
            try
            {
                cFrom.ConnectTo(cTo);
                doc.Regenerate();
                return IsActuallyConnected(cFrom, cTo);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 严格判断：c1 的 AllRefs 中包含 c2，并且 c2 的 AllRefs 中包含 c1。
        /// </summary>
        public static bool IsActuallyConnected(Connector c1, Connector c2)
        {
            if (c1 == null || c2 == null) return false;
            return ContainsConnectorRef(c1, c2) &&
                   ContainsConnectorRef(c2, c1);
        }
        //验证conn1 ref已连c2,单向
        public static bool ContainsConnectorRef(Connector source, Connector target)
        {
            if (source == null || target == null) return false;
            if (source.AllRefs == null) return false;
            int targetOwnerId = target.Owner?.Id.IntegerValue ?? -1;
            int targetConnectorId = target.Id;
            foreach (Connector r in source.AllRefs)
            {
                if (r?.Owner == null) continue;
                if (r.Owner.Id.IntegerValue == targetOwnerId &&
                    r.Id == targetConnectorId)
                {
                    return true;
                }
            }
            return false;
        }
        // 递归查找连接的 MEPCurve 是否水平
        public static MEPCurve GetHorizontalMEPCurveRecursive(Element currentElement, HashSet<ElementId> visited)
        {
            // 基础防护
            if (currentElement == null || visited.Contains(currentElement.Id)) return null;
            // 记录当前元素
            visited.Add(currentElement.Id);

            // 如果当前元素是 MEPCurve
            if (currentElement is MEPCurve curve)
            {
                // ✅ 修复1：移除永远为 false 的 curve.Id != currentElement.Id
                // visited 已经排除了 sourcePipe，此处直接判断水平即可
                if (MEPAnalysisExtension.IsHorizontal(curve))
                {
                    return curve;
                }
                // 如果是管道但不是水平的（例如是一段立管），继续往后探寻
                return SearchNext(curve, visited);
            }

            // 如果当前元素是管件或附件
            if (currentElement is FamilyInstance fi)
            {
                return SearchNext(fi, visited);
            }

            return null;
        }

        // 辅助方法：获取当前元素的所有连接器并继续递归
        private static MEPCurve SearchNext(Element element, HashSet<ElementId> visited)
        {
            // 获取连接管理器
            ConnectorManager cm = null;
            if (element is MEPCurve curve) cm = curve.ConnectorManager;
            else if (element is FamilyInstance fi) cm = fi.MEPModel?.ConnectorManager;
            if (cm == null) return null;

            foreach (Connector conn in cm.Connectors.OfType<Connector>())
            {
                foreach (Connector refConn in conn.AllRefs.OfType<Connector>())
                {
                    // ✅ 修复2：visited.Contains 的判断对象从 element.Id 改为 refConn.Owner.Id
                    if (refConn.Owner == null ||
                        refConn.Owner.Id == element.Id ||
                        visited.Contains(refConn.Owner.Id)) continue;

                    MEPCurve found = GetHorizontalMEPCurveRecursive(refConn.Owner, visited);
                    if (found != null) return found;
                }
            }
            return null;
        }
        ////强制管件实例Z轴重合 
        public static void ForceCoordFittingZ(FamilyInstance targetFitting, FamilyInstance sourceFitting)
        {
            if (targetFitting == null || sourceFitting == null) return;
            // 1. 获取绝对坐标点
            double targetZ = ((LocationPoint)targetFitting.Location).Point.Z;
            double currentZ = ((LocationPoint)sourceFitting.Location).Point.Z;
            // 2. 使用容差比较
            if (Math.Abs(targetZ - currentZ) > 0.0001)
            {
                // 获取目标高程（从 targetFitting）
                Parameter targetElevationParam = targetFitting.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                double targetElevation = targetElevationParam.AsDouble();
                Parameter teeElevationParam = sourceFitting.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                teeElevationParam.Set(targetElevation);
            }
        }
        //判断连接器方向是否水平
        public static bool IsHorizontalConnector(Connector conn)
        {
            if (conn == null) return false;
            XYZ dir = conn.CoordinateSystem.BasisZ.Normalize();
            // 水平连接器方向的 Z 分量应接近 0
            return Math.Abs(dir.Z) < 0.1;
        }
        //
        public static Parameter GetAssociatedParameter(Element element, Connector connector, BuiltInParameter bip)
        {
            var connectorInfo = connector.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;
            if (connectorInfo == null) return null;
            var associatedFamilyParameterId = connectorInfo.GetAssociateFamilyParameterId(new ElementId(bip));
            if (associatedFamilyParameterId == ElementId.InvalidElementId) return null;
            var document = element.Document;
            var parameterElement = document.GetElement(associatedFamilyParameterId) as ParameterElement;
            if (parameterElement == null) return null;
            var paramterDefinition = parameterElement.GetDefinition();
            return element.get_Parameter(paramterDefinition);
        }
    }
}
