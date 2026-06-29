using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    public static class MEPAnalysisExtension
    {
        //==========纯粹几何方法==========
        // 判断两条直线是否平行（支持空间向量）
        public static bool IsParallelTo(this Line l1, Line l2)
        {
            return l1.Direction.IsAlmostEqualTo(l2.Direction) ||
                   l1.Direction.IsAlmostEqualTo(l2.Direction.Negate());
        }
        // 判断两条直线是否共线（在三维空间中）
        public static bool IsCollinear(this Line l1, Line l2, double tolerance = 0.001)
        {
            // 首先检查方向是否平行
            if (!IsParallelTo(l1, l2))
                return false;

            // 检查直线1的起点到直线2的距离（三维空间中点到直线的距离）
            double distanceStart = GetDistanceFromPointToLine(l1.Origin, l2);
            if (distanceStart > tolerance)
                return false;

            // 检查直线1的终点到直线2的距离（确保整条直线都在同一直线上）
            double distanceEnd = GetDistanceFromPointToLine(l1.GetEndPoint(1), l2);
            if (distanceEnd > tolerance)
                return false;

            return true;
        }
        // 判断点是否在线段上
        public static bool IsPointOnLine(this Line line, XYZ point, double tolerance = 1e-6)
        {
            if (point == null || line == null) return false;
            // 保持原逻辑：只判断直线段
            if (line == null)
            {
                TaskDialog.Show("tt", "选择线段非直线");
                return false;
            }
            // 如果要求是线段，建议必须是有界 Line
            if (!line.IsBound) return false;
            // 1. 先判断到直线的距离
            if (GetDistanceFromPointToLine(point, line) > tolerance) return false;
            // 2. 再判断投影是否在线段范围内
            XYZ start = line.GetEndPoint(0);
            XYZ direction = line.Direction;
            double projectionLength = (point - start).DotProduct(direction);
            return projectionLength >= -tolerance &&
                   projectionLength <= line.Length + tolerance;
        }
        // 判断点投影是否在线段上
        public static bool IsProjectedPointOnCurveSegment(this Curve curve, XYZ point, double parameterTolerance = 1e-6)
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
        // 判断两条直线在指定轴上是否重叠（可选，用于更精细的控制）
        public static bool AreLinesOverlap(this Line l1, Line l2, double tolerance = 0.001)
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
        // 判断两条直线是否共面（使用混合积方法）
        public static bool AreLinesCoPlanar(this Line line1, Line line2, double tolerance = 0.001)
        {
            XYZ A = line1.GetEndPoint(0);
            XYZ B = line1.GetEndPoint(1);
            XYZ C = line2.GetEndPoint(0);
            XYZ D = line2.GetEndPoint(1);
            // 计算向量
            XYZ AB = B - A;
            XYZ AC = C - A;
            XYZ AD = D - A;
            // 计算混合积：AB · (AC × AD)
            XYZ crossProduct = AC.CrossProduct(AD);
            double mixedProduct = AB.DotProduct(crossProduct);
            // 判断混合积的绝对值是否接近零
            return Math.Abs(mixedProduct) < tolerance;
        }
        // 计算点到直线（线段无限延伸）的三维距离
        public static double GetDistanceFromPointToLine(this XYZ point, Line line)
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
        // 获取两条直线在XY平面上的投影交点
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
        // 计算三维空间中两条直线的（近似）交点，若两线严格相交，返回交点；若两线近似共面（异面），返回公垂线段的中点
        public static XYZ GetLinesIntersection(this Line line1, Line line2, out bool isParallel)
        {
            XYZ O1 = line1.GetEndPoint(0);
            XYZ D1 = line1.Direction;

            XYZ O2 = line2.GetEndPoint(0);
            XYZ D2 = line2.Direction;

            // 两条直线方向的叉积
            XYZ cross = D1.CrossProduct(D2);
            double crossLengthSq = cross.DotProduct(cross); // 叉积模长的平方

            // 如果叉积接近于0，说明两条线平行，无法求唯一交点
            if (crossLengthSq < 1e-9)
            {
                isParallel = true;
                return null;
            }

            isParallel = false;

            // 异面直线求参数 S 的核心代数公式： s = ((O2 - O1) × D2) · (D1 × D2) / |D1 × D2|²
            XYZ V = O2 - O1;
            double s = V.CrossProduct(D2).DotProduct(cross) / crossLengthSq;
            double t = V.CrossProduct(D1).DotProduct(cross) / crossLengthSq;

            // 计算这两条线相距最近的两个点
            XYZ closestPointOnLine1 = O1 + D1.Multiply(s);
            XYZ closestPointOnLine2 = O2 + D2.Multiply(t);

            // 取公垂线段的中点作为最终的“近似交点”
            return (closestPointOnLine1 + closestPointOnLine2) / 2.0;
        }
        /// 输入一组点（支持2到4个），计算所有两点组合之间的距离，并返回最大值和最小值。
        public static (double MinDistance, double MaxDistance) GetMinMaxDistances(params XYZ[] points)
        {
            // 1. 输入验证
            if (points == null || points.Length < 2)
            {
                throw new ArgumentException("至少需要输入 2 个点进行比较。");
            }
            if (points.Length > 4)
            {
                // 虽然算法支持任意数量，但根据需求，这里加上限制（如果不限制，去掉这个if即可）
                throw new ArgumentException("最多支持输入 4 个点。");
            }
            double minDistance = double.MaxValue; // 初始化为最大可能值
            double maxDistance = double.MinValue; // 初始化为最小可能值
            // 2. 使用双层循环计算所有唯一组合（不重复计算，不计算自身）
            // 假设输入 A, B, C：
            // i=0(A), j=1(B) -> 算 AB
            // i=0(A), j=2(C) -> 算 AC
            // i=1(B), j=2(C) -> 算 BC
            for (int i = 0; i < points.Length - 1; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    // 使用 Revit API 提供的 DistanceTo 方法计算两点距离
                    double distance = points[i].DistanceTo(points[j]);
                    // 3. 更新最大值和最小值
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }
            // 返回元组
            return (minDistance, maxDistance);
        }
        // 计算三维空间中两点水平面距离
        public static double GetHorizontalDistance(this XYZ xYZ1, XYZ xYZ2)
        {
            // 1. 计算两点在三维空间中的向量差
            XYZ vector = xYZ2 - xYZ1;
            // 2. 构造一个新向量，将Z轴分量置零，只保留水平分量
            XYZ horizontalVector = new XYZ(vector.X, vector.Y, 0);
            // 3. 返回水平向量的长度（即水平距离）
            return horizontalVector.GetLength();
        }

        //==========MEP判断方法==========
        // 判断管线是否水平
        public static bool IsHorizontal(this MEPCurve mep)
        {
            if (mep.Location is LocationCurve lc && lc.Curve is Line line)
            {
                return Math.Abs(line.Direction.Z) < 0.001;
            }
            return false;
        }
        // 判断MEPCurve是否为垂直的
        public static bool IsVertical(this MEPCurve mepCurve)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve)) return false;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);
            double tolerance = 0.001;
            return Math.Abs(start.X - end.X) < tolerance && Math.Abs(start.Y - end.Y) < tolerance;
        }
        //判断管线坡度是否大于指定值，2%=0.02
        public static bool IsSlopeGreaterThan(this MEPCurve mepCurve, double targetValue, double tol = 0.00001)
        {
            double actualSlope = GetMEPCurveSlope(mepCurve);
            return actualSlope > targetValue + tol;
        }
        // 检查两个连接器是否真的连在一起
        public static bool IsActuallyConnectedTo(this Connector c1, Connector c2)
        {
            if (c1 == null || c2 == null || !c1.IsConnected || !c2.IsConnected) return false;
            // 使用 ElementId 直接比较，不要使用 .IntegerValue
            return c1.AllRefs.Cast<Connector>().Any(r => r.Owner.Id == c2.Owner.Id && r.Id == c2.Id);
        }
        // 严格判断：c1 的 AllRefs 中包含 c2，并且 c2 的 AllRefs 中包含 c1。辅助方法ContainsConnectorRef
        public static bool IsActuallyConnected(this Connector c1, Connector c2)
        {
            if (c1 == null || c2 == null) return false;
            return ContainsConnectorRef(c1, c2) &&
                   ContainsConnectorRef(c2, c1);
        }
        // 辅助方法ContainsConnectorRef 验证conn1 ref已连c2, 单向
        private static bool ContainsConnectorRef(this Connector source, Connector target)
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
        // 管道管件检验
        public static bool IsPipeFitting(this Element elem)
        {
            if (elem == null) return false;
            Category cat = elem.Category;
            if (cat == null) return false;
            return cat.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting;
        }
        // 判断连接器方向是否水平
        public static bool IsHorizontalConnector(this Connector conn)
        {
            if (conn == null) return false;
            XYZ dir = conn.CoordinateSystem.BasisZ.Normalize();
            // 水平连接器方向的 Z 分量应接近 0
            return Math.Abs(dir.Z) < 0.1;
        }
        // 判断连接器方向是否垂直
        public static bool IsVerticalConnector(this Connector conn)
        {
            if (conn == null) return false;
            XYZ dir = conn.CoordinateSystem.BasisZ.Normalize();
            // 垂直连接器方向的 Z 分量绝对值应接近 1（向上或向下）
            return Math.Abs(Math.Abs(dir.Z) - 1.0) < 0.1;
        }
        // 判断三通是否为横向三通（判断依据：是否至少有两个连接器在水平方向上）
        public static bool IsHorizontalTee(this FamilyInstance fitting)
        {
            var connectors = GetConnectors(fitting);
            if (connectors == null || connectors.Count() != 3) return false;
            int horizontalConnectorCount = 0;
            foreach (Connector conn in connectors)
            {
                if (conn == null) continue;
                // 判断单个连接器是否水平
                if (IsHorizontalConnector(conn))
                {
                    horizontalConnectorCount++;
                }
            }
            // 三通共3个连接器，如果有2个或3个是水平的，则认定为横向三通
            return horizontalConnectorCount >= 2;
        }
        // 判断是否为直通管件（变径/束节：两个连接器且方向相反）
        public static bool IsStraightThroughFitting(this FamilyInstance fitting)
        {
            if (fitting.MEPModel?.ConnectorManager == null) return false;
            var connectors = fitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                .Where(c => c.ConnectorType != ConnectorType.Logical).ToList();
            if (connectors.Count != 2) return false;
            XYZ dir1 = connectors[0].CoordinateSystem.BasisZ;
            XYZ dir2 = connectors[1].CoordinateSystem.BasisZ;
            // 方向相反（相加约等于零向量）
            return dir1.IsAlmostEqualTo(-dir2);
        }
        // 判断两个连接器位置相同
        public static bool AreConnectorsEqual(this Connector conn1, Connector conn2)
        {
            if (conn1 == null || conn2 == null)
                return false;
            // 通过位置和方向判断是否是同一个连接器
            return conn1.Origin.IsAlmostEqualTo(conn2.Origin, 0.001) &&
                   Math.Abs(conn1.Angle - conn2.Angle) < 0.01;
        }

        /// <summary>判断两管是否共线（方向平行且端点在对方轴线上）</summary>
        public static bool AreMEPCurvesColinear(MEPCurve pA, MEPCurve pB)
        {
            XYZ dA = GetMEPCurveDirection(pA);
            XYZ dB = GetMEPCurveDirection(pB);

            // 方向必须平行
            if (!AreDirectionsParallel(dA, dB)) return false;

            // pA 的起点到 pB 的轴线无限延长线的距离 < 容差
            XYZ pA0 = (pA.Location as LocationCurve).Curve.GetEndPoint(0);
            XYZ pB0 = (pB.Location as LocationCurve).Curve.GetEndPoint(0);

            // 将 pA 端点到 pB 方向直线的距离：|(pA0 - pB0) × dB|
            XYZ diff = pA0 - pB0;
            // 只在XY平面判断（水平管）
            XYZ diff2D = new XYZ(diff.X, diff.Y, 0);
            XYZ dB2D = new XYZ(dB.X, dB.Y, 0).Normalize();
            XYZ cross = diff2D.CrossProduct(dB2D);
            double dist = cross.GetLength();

            return dist < 5.0 / 304.8; // 5mm 容差
        }
        /// <summary>获取管道水平方向单位向量</summary>
        public static XYZ GetMEPCurveDirection(MEPCurve p)
        {
            Curve c = (p.Location as LocationCurve).Curve;
            XYZ d = c.GetEndPoint(1) - c.GetEndPoint(0);
            return new XYZ(d.X, d.Y, 0).Normalize();
        }

        /// <summary>判断两个方向向量是否平行（同向或反向）</summary>
        public static bool AreDirectionsParallel(XYZ dA, XYZ dB)
        {
            double cross = dA.CrossProduct(dB).GetLength();
            return cross < 1e-4;
        }
        /// <summary>
        /// 求两管轴线在XY平面上的交点
        /// 使用参数方程：P = A0 + t*dA = B0 + s*dB，解出 t
        /// </summary>
        public static XYZ GetMEPCurveAxesIntersectionXY(MEPCurve pA, MEPCurve pB)
        {
            XYZ a0 = (pA.Location as LocationCurve).Curve.GetEndPoint(0);
            XYZ b0 = (pB.Location as LocationCurve).Curve.GetEndPoint(0);
            XYZ dA = GetMEPCurveDirection(pA);
            XYZ dB = GetMEPCurveDirection(pB);

            // 只在XY平面求解
            // a0 + t*dA = b0 + s*dB
            // => t*dA.X - s*dB.X = b0.X - a0.X
            //    t*dA.Y - s*dB.Y = b0.Y - a0.Y
            double dx = b0.X - a0.X;
            double dy = b0.Y - a0.Y;

            // 行列式
            double det = dA.X * (-dB.Y) - dA.Y * (-dB.X);
            if (Math.Abs(det) < 1e-9) return null; // 平行，无交点

            double t = (dx * (-dB.Y) - dy * (-dB.X)) / det;

            return new XYZ(
                a0.X + t * dA.X,
                a0.Y + t * dA.Y,
                a0.Z);
        }

        /// <summary>
        /// 判断交点落在哪根主管的线段范围内（含端点5mm容差）
        /// </summary>
        public static Pipe GetPipeContainingPoint(Pipe pA, Pipe pB, XYZ pt)
        {
            double tol = 5.0 / 304.8;
            if (IsPointOnPipeSegment(pA, pt, tol)) return pA;
            if (IsPointOnPipeSegment(pB, pt, tol)) return pB;
            return null;
        }

        /// <summary>判断点是否在管段上（含容差）</summary>
        public static bool IsPointOnPipeSegment(Pipe p, XYZ pt, double tol)
        {
            Curve c = (p.Location as LocationCurve).Curve;
            XYZ p0 = c.GetEndPoint(0);
            XYZ p1 = c.GetEndPoint(1);
            // 点到线段的投影参数
            XYZ d = p1 - p0;
            double len = d.GetLength();
            if (len < 1e-9) return false;
            double t = (pt - p0).DotProduct(d) / (len * len);
            // t 在 [-tol/len, 1+tol/len] 范围内 且 点到轴线距离 < tol
            double dist = c.Project(pt).Distance;
            return t >= -tol / len && t <= 1.0 + tol / len && dist < tol;
        }

        /// <summary>
        /// 将旁管端点延伸或裁剪到交点（修改旁管端点使其恰好到达交点）
        /// 返回修改后的管道（即 branch 本身，已修改 LocationCurve）
        /// </summary>
        public static MEPCurve ExtendOrTrimMEPCurveToPoint(Document doc, MEPCurve branch, XYZ intersection)
        {
            Curve c = (branch.Location as LocationCurve).Curve;
            XYZ p0 = c.GetEndPoint(0);
            XYZ p1 = c.GetEndPoint(1);

            // 判断哪个端点更靠近交点
            bool p0Closer = p0.DistanceTo(intersection) < p1.DistanceTo(intersection);

            XYZ newNear = intersection;
            XYZ newFar = p0Closer ? p1 : p0;

            // 修改 LocationCurve
            (branch.Location as LocationCurve).Curve =
                Line.CreateBound(p0Closer ? newNear : newFar,
                                 p0Closer ? newFar : newNear);
            return branch;
        }
        /// <summary>
        /// 判断两个方向向量是否平行
        /// </summary>
        private static bool IsParallelTo(XYZ dir1, XYZ dir2, double tolerance)
        {
            // 标准化向量
            dir1 = dir1.Normalize();
            dir2 = dir2.Normalize();

            // 计算叉积
            XYZ cross = dir1.CrossProduct(dir2);

            // 如果叉积接近零，说明平行（或反平行）
            double crossLength = cross.GetLength();
            if (crossLength < tolerance)
                return true;

            // 检查是否反平行（点积为 -1）
            double dot = dir1.DotProduct(dir2);
            if (Math.Abs(Math.Abs(dot) - 1) < tolerance)
                return true;

            return false;
        }
        /// <summary>
        /// 判断两个 XYZ 点是否在容差范围内相同
        /// </summary>
        public static bool IsSamePoint(XYZ p1, XYZ p2, double tolerance = 0.001)
        {
            return p1.DistanceTo(p2) <= tolerance;
        }
        /// <summary>
        /// 尝试从四根管道中找出两对共线的管道
        /// </summary>
        /// <param name="pipes">输入的四根管道列表</param>
        /// <param name="pair1">输出的第一对共线管道</param>
        /// <param name="pair2">输出的第二对共线管道</param>
        /// <returns>如果成功找到，返回true</returns>
        public static bool TryFindColinearPairs(List<MEPCurve> pipes, out List<MEPCurve> pair1, out List<MEPCurve> pair2)
        {
            pair1 = new List<MEPCurve>();
            pair2 = new List<MEPCurve>();

            // 总共有三种配对可能性: (0,1)+(2,3), (0,2)+(1,3), (0,3)+(1,2)

            // 可能性 1: (0,1)共线 且 (2,3)共线
            if (MEPAnalysisExtension.AreMEPCurvesColinear(pipes[0], pipes[1]) &&
                MEPAnalysisExtension.AreMEPCurvesColinear(pipes[2], pipes[3]))
            {
                pair1.Add(pipes[0]); pair1.Add(pipes[1]);
                pair2.Add(pipes[2]); pair2.Add(pipes[3]);
                return true;
            }

            // 可能性 2: (0,2)共线 且 (1,3)共线
            if (MEPAnalysisExtension.AreMEPCurvesColinear(pipes[0], pipes[2]) &&
                MEPAnalysisExtension.AreMEPCurvesColinear(pipes[1], pipes[3]))
            {
                pair1.Add(pipes[0]); pair1.Add(pipes[2]);
                pair2.Add(pipes[1]); pair2.Add(pipes[3]);
                return true;
            }

            // 可能性 3: (0,3)共线 且 (1,2)共线
            if (MEPAnalysisExtension.AreMEPCurvesColinear(pipes[0], pipes[3]) &&
                MEPAnalysisExtension.AreMEPCurvesColinear(pipes[1], pipes[2]))
            {
                pair1.Add(pipes[0]); pair1.Add(pipes[3]);
                pair2.Add(pipes[1]); pair2.Add(pipes[2]);
                return true;
            }

            return false; // 未找到符合条件的配对
        }
        public static bool IsFittingExistAtPoint(Document doc, XYZ point)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Where(fi => fi.Category != null && (
                    fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting ||
                    fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctFitting ||
                    fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_CableTrayFitting));

            foreach (FamilyInstance fi in collector)
            {
                if (fi.Location is LocationPoint locPoint && locPoint.Point.IsAlmostEqualTo(point, 0.001))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AreDirectionsPerpendicular(XYZ dir1, XYZ dir2, double toleranceDegrees = 1.0)
        {
            double angleRad = dir1.AngleTo(dir2);
            double deviation = Math.Abs(angleRad - Math.PI / 2.0); // 与90度(PI/2)的偏差
            return deviation <= (toleranceDegrees * Math.PI / 180.0);
        }
        public static double GetMEPCurveZ(MEPCurve curve) => ((curve.Location as LocationCurve).Curve as Line).Origin.Z;

        //==========MEP获取方法==========
        // 安全获取元素类别
        public static string GetCatNameSafe(this Element owner)
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
        // 获取connector所在元素的特定内置参数
        public static Parameter GetAssociatedParameter(this Connector connector, BuiltInParameter bip)
        {
            var element = connector?.Owner;
            if (element == null) return null;
            if (!(connector.GetMEPConnectorInfo() is MEPFamilyConnectorInfo info)) return null;
            var paramId = info.GetAssociateFamilyParameterId(new ElementId(bip));
            if (paramId == ElementId.InvalidElementId) return null;
            var definition = (element.Document.GetElement(paramId) as ParameterElement)?.GetDefinition();
            return definition == null ? null : element.get_Parameter(definition);
        }
        // 获取管线的主要尺寸（直径或高度）
        public static double GetMEPCurveMainSize(this MEPCurve mep)
        {
            switch (mep)
            {
                case Pipe p:
                    return p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                case Duct d:
                    return d.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.AsDouble() ??
                           d.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.AsDouble() ??
                           d.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)?.AsDouble() ?? 0;
                case CableTray ct:
                    double width = ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.AsDouble() ?? 0.0;
                    double height = ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.AsDouble() ?? 0.0;
                    return Math.Max(width, height);
                default:
                    return 25.4 / 304.8;
            }
        }
        // 获取管线的真实坡度
        public static double GetMEPCurveSlope(this MEPCurve mepCurve)
        {
            // 1. 获取图元的几何定位线
            LocationCurve locCurve = mepCurve.Location as LocationCurve;
            if (locCurve == null || locCurve.Curve == null) return 0;

            // 2. 获取起点和终点
            XYZ p1 = locCurve.Curve.GetEndPoint(0);
            XYZ p2 = locCurve.Curve.GetEndPoint(1);

            // 3. 计算 X Y Z 各自的增量
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dz = Math.Abs(p2.Z - p1.Z); // Z轴高差 (绝对值)

            // 4. 计算水平面投影长度 (Run)
            double run = Math.Sqrt(dx * dx + dy * dy);

            // 【核心修复点】如果水平长度无限趋近于 0，说明是 100% 的垂直立管
            // 将其坡度指定为 double.MaxValue，与你 ViewModel 中的 90 度解析值保持绝对一致！
            if (run < 0.0001)
            {
                return double.MaxValue;
            }
            // 5. 返回真实的 Tan 坡度值 (Rise / Run)
            return dz / run;
        }
        // 获取元素的 ConnectorManager（MEPCurve / FamilyInstance 通用）
        public static ConnectorManager GetConnectorManager(this Element element)
        {
            if (element is MEPCurve curve) return curve.ConnectorManager;
            if (element is FamilyInstance fi) return fi.MEPModel?.ConnectorManager;
            return null;
        }
        // 获取元素所有的连接器（支持管线和族实例）
        public static IEnumerable<Connector> GetConnectors(this Element element)
        {
            if (element == null) yield break;
            ConnectorManager cm = GetConnectorManager(element);
            if (cm == null) yield break;
            foreach (Connector conn in cm.Connectors)
            {
                if (conn.ConnectorType == ConnectorType.Logical) continue;
                yield return conn;
            }
        }
        // 获取离指定点最近的连接器
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
        // 获取Curve与Instance间最近连接器
        public static Connector GetClosestConnector(this MEPCurve curve, FamilyInstance newSprinklerInstance)
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
        // 找最近的连接器并返回这对连接器。如果未找到返回 (null, null)
        public static (Connector c1, Connector c2) GetClosestConnectorsTuple(this List<Connector> list1, List<Connector> list2)
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
        // 获取第一个未使用的连接器
        public static Connector GetUnusedConnector(this Element element)
            => element.GetConnectors().FirstOrDefault(c => !c.IsConnected);
        // 获取元素上所有未连接的 Connector
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
        // 获取与当前连接器物理相连的其他元素的连接器
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
        // 获取管件（弯头/三通/四通）相连的所有邻居连接器
        public static List<Connector> GetNeighborConnectors(this FamilyInstance fitting)
        {
            return fitting.GetConnectors().Where(c => c.IsConnected).SelectMany(c => c.GetConnectedRefs()).ToList();
        }
        // 获取管道对侧连接接口  (用于计算打断方向) 
        public static Connector GetOppositeConnector(this MEPCurve curve, Connector inputconnector)
        {
            foreach (Connector conn in curve.ConnectorManager.Connectors)
            {
                if (!conn.Origin.IsAlmostEqualTo(inputconnector.Origin, 0.005))
                {
                    // 过滤掉支管连接器等，只保留端点连接器
                    if (conn.ConnectorType == ConnectorType.End)
                    {
                        return conn;
                    }
                }
            }
            return null;
        }
        // 获取 FamilyInstance 上与指定连接器方向相反的对侧连接器。 可用GetBestFitConnector拓展
        public static Connector GetOppositeConnector(this FamilyInstance familyInstance, Connector inputConnector)
        {
            if (familyInstance == null || familyInstance.MEPModel == null || inputConnector == null)
                return null;

            ConnectorManager connectorManager = familyInstance.MEPModel.ConnectorManager;
            if (connectorManager == null)
                return null;

            // 获取输入连接器的向外法向量方向
            XYZ inputDirection = inputConnector.CoordinateSystem.BasisZ;

            foreach (Connector conn in connectorManager.Connectors)
            {
                // 排除自身连接器
                if (conn.Id == inputConnector.Id)
                    continue;

                // 获取当前连接器的方向
                XYZ currentDirection = conn.CoordinateSystem.BasisZ;

                // 判断两个向量是否相反：相反向量在 3D 空间中的夹角为 180 度 (Math.PI 弧度)
                // 使用 0.01 弧度（约 0.57度）作为 Revit 精度容差
                double angle = inputDirection.AngleTo(currentDirection);
                if (Math.Abs(angle - Math.PI) < 0.01)
                {
                    return conn;
                }
            }

            return null;
        }
        // 找出三通的侧向接口（通过方向判断：反向的两个为主干，剩下为侧向）推荐！
        public static Connector GetTeeSideConn(this FamilyInstance teeFitting)
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
        // 找出三通的侧向接口（通过距离判断：距离最远的两个为主干，剩下为侧向）
        public static Connector GetTeeSideConn_ByDistance(this FamilyInstance teeFitting)
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
        // 获取指定位置的连接器
        public static Connector GetConnectorAtPoint(this MEPCurve curve, XYZ point)
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
        // 获取连接器方向
        public static XYZ GetConnectorDirection(this Connector conn)
        {
            try
            {
                if (conn == null) return XYZ.BasisX;
                //CoordinateSystem cs = c.CoordinateSystem;
                if (conn.CoordinateSystem != null)
                {
                    // 常用 BasisZ 表示连接方向，但不同族不完全统一
                    XYZ dir = conn.CoordinateSystem.BasisZ;
                    if (dir != null && dir.GetLength() > 1e-6)
                        return dir.Normalize();
                }
            }
            catch
            {
            }
            return XYZ.BasisX;
        }
        // 使用广度优先遍历(BFS)获取所有相连元素包括curve和族实例
        public static List<ElementId> GetAllConnectedElements(this List<Connector> startConnectors)
        {
            if (startConnectors == null || startConnectors.Count == 0)
                return new List<ElementId>();

            var result = new List<ElementId>();
            var processedElements = new HashSet<ElementId>();
            var queue = new Queue<Element>();

            // 1. 将所有起点元素的主体ID加入黑名单，彻底排除自身，且防止后续转回来找到它
            foreach (var connector in startConnectors)
            {
                if (connector?.Owner != null)
                {
                    processedElements.Add(connector.Owner.Id);
                }
            }

            // 2. 严格【仅从入参连接器】向外探出第一步
            foreach (var startConnector in startConnectors)
            {
                if (startConnector == null) continue;

                foreach (Connector refConn in startConnector.AllRefs)
                {
                    // 屏蔽逻辑连接器（系统）
                    if (refConn.ConnectorType == ConnectorType.Logical) continue;

                    Element connectedElement = refConn.Owner;
                    if (connectedElement == null || connectedElement is Autodesk.Revit.DB.MEPSystem) continue;

                    // 如果对面的元素没被处理过，压入队列，并立即标记已处理
                    if (processedElements.Add(connectedElement.Id))
                    {
                        queue.Enqueue(connectedElement);
                    }
                }
            }

            // 3. 开始标准的广度优先遍历
            while (queue.Count > 0)
            {
                Element currentElement = queue.Dequeue();

                // 既然能进队列，必定不是起点元素，直接加入结果集
                result.Add(currentElement.Id);

                // 获取当前元素的所有连接器
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
                    if (connector.ConnectorType == ConnectorType.Logical) continue;

                    // 获取连接器连接到的其他连接器
                    foreach (Connector refConn in connector.AllRefs)
                    {
                        if (refConn.ConnectorType == ConnectorType.Logical) continue;

                        Element connectedElement = refConn.Owner;
                        if (connectedElement == null || connectedElement is Autodesk.Revit.DB.MEPSystem) continue;

                        // 如果未处理过，压入队列并立即标记
                        if (processedElements.Add(connectedElement.Id))
                        {
                            queue.Enqueue(connectedElement);
                        }
                    }
                }
            }
            return result;
        }
        // 查找相连元素，遇到水平管停止。 返回值：沿途经过的所有中间构件的 ElementId 集合（不含起点和水平管）。返回收集到的所有作为边界的水平管。辅助方法PushConnectedElements
        public static (List<ElementId>, List<MEPCurve>) GetAllConnectedElementsAndStopByHorizontalCurves(this Connector startConnector)
        {
            List<MEPCurve> boundaryHorizontalPipes = new List<MEPCurve>();
            var resultElements = new List<ElementId>();

            if (startConnector == null || startConnector.Owner == null)
                return (resultElements, boundaryHorizontalPipes);

            var processedElements = new HashSet<ElementId>();
            var stackedElements = new HashSet<ElementId>();
            var queue = new Queue<Element>();

            // 使用字典来收集水平管，确保绝对去重（防止成环管网导致两端查找到同一根管子）
            var uniquePipes = new Dictionary<ElementId, MEPCurve>();

            Element startElement = startConnector.Owner;
            ElementId startElementId = startElement.Id;

            queue.Enqueue(startElement);
            stackedElements.Add(startElementId);

            while (queue.Count > 0)
            {
                Element currentElement = queue.Dequeue();

                if (currentElement == null || processedElements.Contains(currentElement.Id))
                    continue;

                processedElements.Add(currentElement.Id);

                // 判断当前元素是否为横管（边界）
                bool isHorizontalPipe = currentElement is Pipe pipe && MEPAnalysisExtension.IsHorizontal(pipe);

                // 如果遇到了远端的水平管（不能是起点自身）
                if (isHorizontalPipe && currentElement.Id != startElementId)
                {
                    // 【核心收集】：把这根水平管加入集合
                    uniquePipes[currentElement.Id] = (Pipe)currentElement;

                    // 【核心打断】：遇到水平管就不再向外扩散，直接跳过连接器遍历
                    continue;
                }

                // 加入结果的条件：不是起点 且 不是横管
                if (currentElement.Id != startElementId && !isHorizontalPipe)
                {
                    resultElements.Add(currentElement.Id);
                }

                var connectorSet = GetConnectorManager(currentElement)?.Connectors;
                if (connectorSet == null) continue;

                // 向外扩散：遍历当前元素的所有连接器
                foreach (Connector connector in connectorSet)
                {
                    // 过滤逻辑连接器
                    if (connector == null || connector.ConnectorType == ConnectorType.Logical) continue;
                    // 【修改点】：只要当前元素是起点构件，强制过滤，只能顺着入参 startConnector 往外找
                    if (currentElement.Id == startElementId)
                    {
                        // 如果当前遍历的连接器不是我们传入的起始连接器，则跳过
                        if (connector.Id != startConnector.Id) continue;
                    }
                    //// 如果起点是横管，必须确保我们只顺着 startConnector 往外找，不往管子反方向找
                    //if (currentElement.Id == startElementId && isHorizontalPipe)
                    //{
                    //    if (connector.Id != startConnector.Id) continue;
                    //}

                    PushConnectedElements(connector, currentElement, processedElements, stackedElements, queue);
                }
            }
            // 赋值 out 参数
            boundaryHorizontalPipes = uniquePipes.Values.ToList();
            return (resultElements, boundaryHorizontalPipes);
        }
        // 辅助方法PushConnectedElements：获取连接器连接到的其他元素并压入队列
        private static void PushConnectedElements(Connector connector, Element currentElement,
            HashSet<ElementId> processedElements, HashSet<ElementId> stackedElements,
            Queue<Element> stack)
        {
            foreach (Connector connectedConnector in connector.AllRefs)
            {
                if (connectedConnector == null) continue;

                // 过滤逻辑连接器，避免顺着 MEPSystem 穿透整张管网
                if (connectedConnector.ConnectorType != ConnectorType.End &&
                    connectedConnector.ConnectorType != ConnectorType.Curve)
                    continue;

                Element connectedElement = connectedConnector.Owner;
                if (connectedElement == null) continue;

                // 排除 MEPSystem 元素和自身
                if (connectedElement is Autodesk.Revit.DB.MEPSystem) continue;
                if (connectedElement.Id == currentElement.Id) continue;

                if (!processedElements.Contains(connectedElement.Id) &&
                    stackedElements.Add(connectedElement.Id))
                {
                    stack.Enqueue(connectedElement);
                }
            }
        }
        // 深度优先搜索（DFS）顺着管件/管道寻找连接的垂直元素及其末端喷头，直到找到喷头（单连接器元素）为止，并收集这条路径上的所有元素以便后续删除    
        public static HashSet<ElementId> GetAllConnectedElementsAndStopByVerticalInstance(Element startElement, bool searchUpward)
        {
            var toRemove = new HashSet<ElementId>();
            var visited = new HashSet<ElementId>();
            // 内部局部函数，执行递归查找逻辑 (深度优先搜索 DFS)
            bool TraverseVerticalPath(Element currentElem)
            {
                // 1. 防死循环：如果已经访问过，或者是无效元素，跳过
                if (currentElem == null || !visited.Add(currentElem.Id)) return false;
                // 2. 获取当前元素所有连接器
                var connectors = MEPAnalysisExtension.GetConnectors(currentElem).ToList();
                if (connectors.Count == 0) return false;
                // 3. 将当前元素加入待删除列表
                toRemove.Add(currentElem.Id);
                // 4. 如果只有一个连接器（通常是喷头或管帽末端），说明找到终点，终止并确认成功
                if (connectors.Count == 1) return true;
                var verticalConnectors = connectors.Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) > 0.5);
                // 5. 根据方向排序连接器，优先遍历目标垂直方向
                // searchUpward = true  -> 优先 +Z (向上)
                // searchUpward = false -> 优先 -Z (向下，即 Z 值从小到大排序)
                var sortedConnectors = searchUpward
                        ? verticalConnectors.OrderByDescending(c => c.CoordinateSystem.BasisZ.Z) // 优先向上找
                        : verticalConnectors.OrderBy(c => c.CoordinateSystem.BasisZ.Z);          // 优先向下找
                                                                                                 // 6. 顺着排序后的连接器往下找
                foreach (var conn in sortedConnectors)
                {
                    var connectedRefs = conn.AllRefs.OfType<Connector>().Where(r => r.Owner?.Id != currentElem.Id);
                    foreach (var refConn in connectedRefs)
                    {
                        // refConn.Owner 直接就是 Element，无需调用 Document.GetElement
                        if (TraverseVerticalPath(refConn.Owner))
                        {
                            // 如果在子分支中成功找到了喷头，一路返回 true，保留路径
                            return true;
                        }
                    }
                }
                // 7. 【关键修复】如果遍历了所有接口都没找到喷头，说明这是一条死分支,必须把当前元素从删除列表中移除，否则会误删平行的主管道或其他无关管件！
                toRemove.Remove(currentElem.Id);
                return false;
            }
            // 启动递归搜索
            TraverseVerticalPath(startElement);
            return toRemove;
        }
        // 查找一定范围内MepCurve
        public static IList<MEPCurve> GetMEPCurvesAtPoint(Document doc, XYZ point, Level currentLevel, double offsetHeight, double detectRange = 1.0)
        {
            var result = new List<MEPCurve>();
            try
            {
                if (doc == null || point == null) return result;
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
        ////递归找水平管。辅助方法SearchNext。方法有问题暂停用
        //public static MEPCurve GetHorizontalMEPCurveRecursive(Element currentElement, HashSet<ElementId> visited)
        //{
        //    // 基础防护
        //    if (currentElement == null || visited.Contains(currentElement.Id)) return null;
        //    // 记录当前元素
        //    visited.Add(currentElement.Id);
        //    // 如果当前元素是 MEPCurve
        //    if (currentElement is MEPCurve curve)
        //    {
        //        // ✅ 修复1：移除永远为 false 的 curve.Id != currentElement.Id
        //        // visited 已经排除了 sourcePipe，此处直接判断水平即可
        //        if (MEPAnalysisExtension.IsHorizontal(curve))
        //        {
        //            return curve;
        //        }
        //        // 如果是管道但不是水平的（例如是一段立管），继续往后探寻
        //        return SearchNext(curve, visited);
        //    }
        //    // 如果当前元素是管件或附件
        //    if (currentElement is FamilyInstance fi)
        //    {
        //        return SearchNext(fi, visited);
        //    }
        //    return null;
        //}
        //private static MEPCurve SearchNext(Element element, HashSet<ElementId> visited)
        //{
        //    // 获取连接管理器
        //    ConnectorManager cm = GetConnectorManager(element);
        //    if (cm == null) return null;
        //    foreach (Connector conn in cm.Connectors.OfType<Connector>())
        //    {
        //        foreach (Connector refConn in conn.AllRefs.OfType<Connector>())
        //        {
        //            // ✅ 修复2：visited.Contains 的判断对象从 element.Id 改为 refConn.Owner.Id
        //            if (refConn.Owner == null ||
        //                refConn.Owner.Id == element.Id ||
        //                visited.Contains(refConn.Owner.Id)) continue;
        //            MEPCurve found = GetHorizontalMEPCurveRecursive(refConn.Owner, visited);
        //            if (found != null) return found;
        //        }
        //    }
        //    return null;
        //}
        // 从喷头端找垂直管，未递归循环
        public static MEPCurve GetVerticalMEPCurve(Element currentElement)
        {
            // 1. 获取喷头的连接器
            var startConn = MEPAnalysisExtension.GetConnectors(currentElement).FirstOrDefault();
            if (startConn == null) return null;
            // 2. 遍历喷头连接器所引用的目标
            foreach (Connector linkedConn in startConn.AllRefs.OfType<Connector>())
            {
                // 场景 A: 喷头直接连在管道上，linkedConn 此时就是管道那一侧的连接器
                if (linkedConn.Owner is MEPCurve curve)
                {
                    return curve;
                }
                // 场景 B: 通过管件（变径/束节等）中转
                if (linkedConn.Owner is FamilyInstance fitting)
                {
                    var fittingConns = fitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
                    if (fittingConns == null) continue;
                    foreach (Connector fConn in fittingConns)
                    {
                        // 遍历管件上每个接口连接的外部对象
                        foreach (Connector nextRef in fConn.AllRefs.OfType<Connector>())
                        {
                            // 找到了管道，且不是最初的喷头
                            if (nextRef.Owner is MEPCurve foundCurve && IsVertical(foundCurve))
                            {
                                return foundCurve;
                            }
                        }
                    }
                }
            }
            // 兜底返回
            return null;
        }
        // 从管件的连接器出发，获取所有“外部相邻”的连接器。不返回管件自身连接器。可用于弯头、直接头、变径、三通、四通等。
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
        // 只获取两端管件的外部相邻连接器。如果外部连接器数量不是 2，则返回空集合。
        public static List<Connector> GetTwoEndNeighborConnectors(FamilyInstance fitting)
        {
            List<Connector> connectors = GetExternalNeighborConnectors(fitting);
            return connectors.Count == 2 ? connectors : new List<Connector>();
        }
        // 对比管径，查找最大或最小管径。参数 isMax：传入 true 查找最大管径，传入 false 查找最小管径
        public static double GetMaxOrMinPipeDiameter(IEnumerable<Pipe> pipes, bool isMax)
        {
            // 1. 防御性编程：判断集合是否为空
            if (pipes == null || !pipes.Any()) return 0.0;
            // 2. 根据需求设置初始值：找最大值初始设极小，找最小值初始设极大
            double targetDiameter = isMax ? double.MinValue : double.MaxValue;
            bool hasValidPipe = false; // 用于记录集合中是否有非 null 的管道
            foreach (var pipe in pipes)
            {
                if (pipe == null) continue;
                hasValidPipe = true;
                double currentDiameter = pipe.Diameter;
                // 3. 比较并更新目标值
                if (isMax)
                {
                    // 找最大管径
                    if (currentDiameter > targetDiameter)
                    {
                        targetDiameter = currentDiameter;
                    }
                }
                else
                {
                    // 找最小管径
                    if (currentDiameter < targetDiameter)
                    {
                        targetDiameter = currentDiameter;
                    }
                }
            }
            // 4. 如果全都是 null，则返回 0，否则返回找到的目标值
            return hasValidPipe ? targetDiameter : 0.0;
        }
        // 在一个 Revit 图元（比如管件、设备）上，寻找与“目标位置”和“目标方向”最匹配的连接器（Connector）
        public static Connector GetBestFitConnector(this Element owner, XYZ targetOrigin, XYZ targetDirection)
        {
            var all = MEPAnalysisExtension.GetConnectors(owner);
            if (all.Count() == 0) return null;
            Connector best = null;
            double bestScore = double.MaxValue;
            foreach (var c in all)
            {
                //距离惩罚（越近越好）
                double dist = c.Origin.DistanceTo(targetOrigin);
                XYZ dir = MEPAnalysisExtension.GetConnectorDirection(c);
                //方向惩罚（越平行越好）
                double dirPenalty = 0;
                if (dir != null && targetDirection != null)
                {
                    double dot = Math.Abs(dir.Normalize().DotProduct(targetDirection.Normalize()));
                    dirPenalty = 1.0 - dot;
                }
                //综合打分（求最低分）
                double score = dist + dirPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }


        //==========MEP操作方法,注意：调用方需要确保当前处于 Transaction 中。==========
        // 断开连接并返回对方的连接器对象
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
        // 双向断开连接器
        public static void SafeDisconnect(this Connector a, Connector b)
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
        //断开所有连接
        public static void DisconnectConnector(this Connector connector)
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
        // 合并两根等径共线管线
        public static void MergeTwoPipes(this Document doc, MEPCurve m1, Connector c1, MEPCurve m2, Connector c2)
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
        // 管道退后方法
        public static void RetreatMEPCurve(this MEPCurve mepCurve, XYZ breakPoint, double retreatDistance)
        {
            try
            {
                LocationCurve locationCurve = mepCurve.Location as LocationCurve;
                if (locationCurve == null) return;
                Line currentCurve = locationCurve.Curve as Line;
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
                    if (MEPAnalysisExtension.IsPointOnLine(currentCurve, newStartPoint))
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
                    if (MEPAnalysisExtension.IsPointOnLine(currentCurve, newEndPoint))
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
        // 加长或缩短管道并返回端头点
        public static XYZ AdjustMEPCurveLength(this MEPCurve mepCurve, XYZ referencePoint, double distance)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve) || !(locationCurve.Curve is Line currentCurve))
            {
                return null; // 仅支持直线管道
            }
            try
            {
                XYZ startPoint = currentCurve.GetEndPoint(0);
                XYZ endPoint = currentCurve.GetEndPoint(1);
                double originalLength = currentCurve.Length;
                // 如果是缩短，检查距离是否会导致管道长度为负
                if (distance > 0 && distance >= originalLength - 1e-9)
                {
                    System.Diagnostics.Debug.WriteLine("管道调整失败：缩短距离过大。");
                    return null;
                }
                XYZ newStart, newEnd;
                bool isStartPointCloser = referencePoint.DistanceTo(startPoint) < referencePoint.DistanceTo(endPoint);
                if (isStartPointCloser)
                {
                    // 移动起点
                    XYZ direction = (endPoint - startPoint).Normalize();
                    newStart = startPoint + direction * distance;
                    newEnd = endPoint;
                }
                else
                {
                    // 移动终点
                    XYZ direction = (startPoint - endPoint).Normalize(); // 方向反转
                    newStart = startPoint;
                    newEnd = endPoint + direction * distance;
                }
                Line newCurve = Line.CreateBound(newStart, newEnd);
                locationCurve.Curve = newCurve;
                // 返回被改变的那个点
                return isStartPointCloser ? newStart : newEnd;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"管道长度调整失败: {ex.Message}");
                return null;
            }
        }
        // 尝试 cFrom.ConnectTo(cTo)，并通过 AllRefs 验证两者是否真正互相引用。
        public static bool TryConnectAndVerify(this Document doc, Connector cFrom, Connector cTo)
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
        // 强制管件实例Z轴重合 
        public static void ForceCoordFittingZ(this FamilyInstance targetFitting, FamilyInstance sourceFitting)
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
        //在两个管道之间直接创建弯头并连接 辅助方法 ExtendOrTrimMEPCurveToPoint
        public static FamilyInstance NewElbowBy2MEPCurve(this MEPCurve curve1, MEPCurve curve2)
        {
            Document doc = curve1.Document;
            FamilyInstance result;
            // 3. 计算中心线延长线的交点
            Line line1 = (curve1.Location as LocationCurve).Curve as Line;
            Line line2 = (curve2.Location as LocationCurve).Curve as Line;
            // 使用纯数学方法获取交点（彻底替代不稳定、对公差敏感的 Intersect 方法）
            XYZ intersectionPoint = GetLinesIntersection(line1, line2, out bool isParallel);
            if (isParallel || !AreLinesCoPlanar(line1, line2))
            {
                TaskDialog.Show("错误", "这两根管道在空间中不共面或平行，无法生成交点！");
                return result = null;
            }
            // 4. 将两根管道延伸或修剪到交点
            MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(curve1, intersectionPoint);
            MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(curve2, intersectionPoint);
            // 注意：修改管道几何后，需要重新生成文档以更新Connector的位置
            doc.Regenerate();
            // 5. 获取交点处的两个连接器 (Connector)
            Connector conn1 = MEPAnalysisExtension.GetClosestConnector(curve1, intersectionPoint);
            Connector conn2 = MEPAnalysisExtension.GetClosestConnector(curve2, intersectionPoint);
            if (conn1 == null || conn2 == null)
            {
                TaskDialog.Show("错误", "无法在交点处找到连接器！");
                return result = null;
            }
            // 6. 检查管径是否一致（变径处理逻辑）可自动处理无需手动
            if (Math.Abs(curve1.Diameter - curve2.Diameter) > 0.001)
            {

            }
            // 7. 生成弯头
            FamilyInstance elbow = doc.Create.NewElbowFitting(conn1, conn2);
            return elbow;
        }
        // 辅助方法 ExtendOrTrimMEPCurveToPoint：将管道端点移动到目标点 
        private static void ExtendOrTrimMEPCurveToPoint(this MEPCurve curve, XYZ targetPoint)
        {
            LocationCurve locCurve = curve.Location as LocationCurve;
            Line line = locCurve.Curve as Line;
            XYZ pt0 = line.GetEndPoint(0);
            XYZ pt1 = line.GetEndPoint(1);
            // 判断哪个端点离交点更近
            if (pt0.DistanceTo(targetPoint) < pt1.DistanceTo(targetPoint))
            {
                // 移动端点0
                locCurve.Curve = Line.CreateBound(targetPoint, pt1);
            }
            else
            {
                // 移动端点1
                locCurve.Curve = Line.CreateBound(pt0, targetPoint);
            }
        }
        // 连接管道与管件上的连接器方法，先如果mepcurve上与管件连接器较近的connector在一个位置优先连接，如果管件连接器在mepcurve延长线或线上调整管上较近连接器位置，如果连接器和mepcurve管径不同则增加变径
        public static void ConnectMEPCurve2FittingConn(this MEPCurve mEPCurve, Connector fitConnector)
        {
            if (mEPCurve == null || fitConnector == null) return;
            Document doc = mEPCurve.Document;
            // 1. 获取 MEPCurve 上距离给定的管件 Connector 最近的连接器
            Connector closestPipeConn = GetClosestConnector(mEPCurve, fitConnector.Origin);
            // 1. 设置一个合理的容差，比如 0.001 英尺 (约 0.3mm)
            bool isSameLocation = fitConnector.Origin.IsAlmostEqualTo(closestPipeConn.Origin, 0.001);
            try
            {
                if (!isSameLocation)
                {
                    if (Math.Abs(closestPipeConn.Radius - fitConnector.Radius) > 0.001) // 容差
                    {
                        FamilyInstance instance = NewTransitionSafely(doc, closestPipeConn, fitConnector);
                    }
                    else
                    {
                        // 管径相同，不需要变径，直接连接或生成直接(Union)
                        closestPipeConn.ConnectTo(fitConnector);
                    }
                }
                else
                {
                    bool hasConnected = TryConnectAndVerify(doc, closestPipeConn, fitConnector);
                    if (!hasConnected)
                    {
                        //NewTransitionSafely(doc, closestPipeConn, connector);
                        doc.Create.NewTransitionFitting(closestPipeConn, fitConnector);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        // 在两个连接器之间直接创建管道并连接
        public static Pipe NewPipeBetweenConnectors(this Document doc, Connector conn1, Connector conn2, ElementId pipeTypeId, ElementId levelId, ElementId systemTypeId, double pipeDiameter)
        {
            // 2. 根据两个连接器的坐标，创建新管道
            Pipe newPipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, conn1.Origin, conn2.Origin);
            newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipeDiameter);
            var conn = GetClosestConnector(newPipe, conn1.Origin);
            var spConn = GetOppositeConnector(newPipe, conn);
            TryConnectAndVerify(doc, conn, conn1);
            TryConnectAndVerify(doc, spConn, conn2);
            return newPipe;
        }
        // 在两个点之间创建一根新管道，其属性继承自参考管道。
        public static Pipe NewPipeBetweenPoints(this Pipe referencePipe, XYZ start, XYZ end)
        {
            Document doc = referencePipe.Document;
            Pipe newPipe = Pipe.Create(doc,
                referencePipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId(),
                referencePipe.PipeType.Id,
                referencePipe.ReferenceLevel.Id,
                start,
                end);
            newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(referencePipe.Diameter);
            return newPipe;
        }
        // 安全地生成变径（大小头），自动纠正连接器顺序
        public static FamilyInstance NewTransitionSafely(this Document doc, Connector conn1, Connector conn2)
        {
            if (conn1 == null || conn2 == null)
                throw new ArgumentNullException("连接器不能为空");

            Connector firstConn = conn1;
            Connector secondConn = conn2;

            // 检查第一个连接器的宿主是否为管道(MEPCurve)。如果不是，而第二个是，则交换顺序
            bool isConn1Pipe = conn1.Owner is MEPCurve;
            bool isConn2Pipe = conn2.Owner is MEPCurve;

            if (!isConn1Pipe && isConn2Pipe)
            {
                firstConn = conn2;
                secondConn = conn1;
            }
            else if (!isConn1Pipe && !isConn2Pipe)
            {
                // 如果两个都不是管道（两个都是管件），Revit 通常无法直接在两个管件之间生成变径
                throw new InvalidOperationException("无法在两个管件之间直接生成变径，至少需要一个是管道。");
            }

            // 调用 API，确保属于管道的 firstConn 永远在第一个
            return doc.Create.NewTransitionFitting(firstConn, secondConn);
        }
        ////在垂直面转管件OK,接口true向下,false向上
        public static void RotateTeeFittingVertical(this FamilyInstance fitting, MEPCurve pipe, bool upDown)
        {
            Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(fitting);
            if (sideConn == null) return;
            // 1. 确定目标方向
            XYZ targetDirection = upDown ? -XYZ.BasisZ : XYZ.BasisZ;
            XYZ currentDirection = sideConn.CoordinateSystem.BasisZ;
            // 2. 检查当前方向是否已经正确
            if (currentDirection.IsAlmostEqualTo(targetDirection, 0.001)) return;
            XYZ rotationAxis;
            double rotationAngle;
            // 3. 计算旋转轴和角度
            if (currentDirection.IsAlmostEqualTo(-targetDirection, 0.001))
            {
                // 【关键修复】方向相反（180度）时，必须绕水平主管道的轴线翻转！
                // 否则会导致主管两端的接口对调，破坏系统连接
                var pipeLine = (pipe?.Location as LocationCurve)?.Curve as Line;
                rotationAxis = pipeLine.Direction;
                // 180度
                rotationAngle = Math.PI;
            }
            else
            {
                // 90度等情况：叉积能直接得到完美符合右手定则的旋转轴（恰好也是主管道走向）
                rotationAxis = currentDirection.CrossProduct(targetDirection);
                rotationAngle = currentDirection.AngleTo(targetDirection);
            }
            // 4. 执行旋转
            XYZ fittingOrigin = ((LocationPoint)fitting.Location).Point;
            // 确保旋转轴是单位向量
            Line rotationAxisLine = Line.CreateBound(fittingOrigin, fittingOrigin + rotationAxis.Normalize());
            ElementTransformUtils.RotateElement(pipe.Document, fitting.Id, rotationAxisLine, rotationAngle);
        }
        // 通用方法打断管，返回新管
        public static MEPCurve BreakMEPCurveByOne(this MEPCurve mEPCurve, XYZ breakPoint)
        {
            Document doc = mEPCurve.Document;
            // 1. 投影点确保在中心线上
            Curve oriCurve = (mEPCurve.Location as LocationCurve).Curve;
            breakPoint = oriCurve.Project(breakPoint).XYZPoint;
            // 2. 识别原管两端的连接信息 (此处以End0和End1逻辑替代Hardcode的Id)
            XYZ startPoint = oriCurve.GetEndPoint(0);
            XYZ endPoint = oriCurve.GetEndPoint(1);
            // 找到原管靠近 End1 (终点) 的连接器并断开，记录它连接的对象
            Connector endConnector = MEPAnalysisExtension.GetClosestConnector(mEPCurve, endPoint);
            Connector remotePartner = MEPAnalysisExtension.SafeDisconnect(endConnector);
            // 3. 拷贝元素
            ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, XYZ.Zero);
            MEPCurve mEPCurveCopy = doc.GetElement(ids.First()) as MEPCurve;
            // 4. 更新几何
            (mEPCurve.Location as LocationCurve).Curve = Line.CreateBound(startPoint, breakPoint);
            (mEPCurveCopy.Location as LocationCurve).Curve = Line.CreateBound(breakPoint, endPoint);
            // 5. 恢复连接
            if (remotePartner != null)
            {
                // 在新管上找到对应的端点连接器，连接回原来的 remotePartner
                Connector copyEndConn = MEPAnalysisExtension.GetClosestConnector(mEPCurveCopy, endPoint);
                if (copyEndConn != null)
                {
                    copyEndConn.ConnectTo(remotePartner);
                }
            }
            return mEPCurveCopy;
        }
        //0622 管道L连接子方法
        //获取并验证管道，T连接或十字连接可能共用
        public static bool TryGetAndValidatePipe(this UIDocument uiDoc, string prompt, out Pipe pipe, out Connector connector, out Line line)
        {
            pipe = null;
            connector = null;
            line = null;

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), prompt);
            if (reference == null) return false;
            pipe = uiDoc.Document.GetElement(reference) as Pipe;
            XYZ pickPoint = reference.GlobalPoint;
            if (pipe == null) return false;
            if (pipe.IsSlopeGreaterThan(0.02))
            {
                TaskDialog.Show("限制", "暂不支持坡度过大的管道连接，请手工调整。");
                return false;
            }
            if (!(pipe.Location is LocationCurve lc) || !(lc.Curve is Line l))
            {
                TaskDialog.Show("限制", "仅支持直线管道。");
                return false;
            }
            line = l;
            connector = pipe.GetClosestConnector(pickPoint);
            return connector != null;
        }
        //获取并验证管道连接选项，T连接或十字连接可能共用
        public static bool TryGetElbowAndStrategy(this Pipe pipe, out string strategy)
        {
            strategy = null;
            Document doc = pipe.Document;
            var pipeType = doc.GetElement(pipe.GetTypeId()) as PipeType;
            var routePrefManager = pipeType.RoutingPreferenceManager;
            if (routePrefManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Elbows) == 0 ||
                routePrefManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Junctions) == 0)
            {
                TaskDialog.Show("错误", $"管道类型“{pipeType.Name}”的路由首选项中未定义任何弯头。");
                return false;
            }
            ////// 获取索引为 0 的规则，即“首选”规则
            //RoutingPreferenceRule rule = routePrefManager.GetRule(RoutingPreferenceRuleGroupType.Elbows, 0);
            //ElementId elbowFittingId = rule.MEPPartId;
            //if (elbowFittingId == ElementId.InvalidElementId) return false;
            //Family elbowFamily = (doc.GetElement(elbowFittingId) as FamilySymbol).Family;
            //string prompt = "默认弯头：" + elbowFamily.Name;
            //var dialog = new UniversalComboBoxSelection(new List<string> { "高概率", "中概率" }, prompt + "，请选择连接策略", _ => { });
            //if (dialog.ShowDialog() != true || !(dialog.DataContext is ComboboxStringViewModel vm) || string.IsNullOrWhiteSpace(vm.SelectName))
            //{
            //    return false;
            //}
            //strategy = vm.SelectName;
            strategy = "高概率";
            return true;
        }
        //获取并验证点间距，T连接或十字连接可能共用
        public static bool ValidatePipePair(this XYZ p1, XYZ p2)
        {
            var dist = MEPAnalysisExtension.GetMinMaxDistances(p1, p2);
            if (dist.MinDistance < 0.04 || dist.MaxDistance > 6) // 单位：英尺
            {
                TaskDialog.Show("限制", "检测到管道连接器位置过近或过远，请手工调整。");
                return false;
            }
            return true;
        }
        //////L连接私有方法
        public static bool ConnectParallelPipes(this Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
        {
            Document doc = p1.Document;
            // 分支 1: 共线管道
            if (l1.IsCollinear(l2))
            {
                // 共线：直接连接或变径
                if (Math.Abs(p1.Diameter - p2.Diameter) > 1e-6)
                {
                    doc.Create.NewTransitionFitting(c1, c2);
                }
                else
                {
                    doc.MergeTwoPipes(p1, c1, p2, c2);
                }
                return true;
            }
            // 分支 2: 平行、共面但不共线
            if (!l1.AreLinesCoPlanar(l2, 1e-6))
            {
                TaskDialog.Show("限制", "平行的两根管道不共面，无法自动连接。");
                return false;
            }
            // 核心逻辑: 在共面不共线的两线之间创建S弯连接
            return CreateS_BendConnection(p1, c1, p2, c2, strategy);
            //return true;
        }
        //// 为两根平行、共面但不共线的管道创建S型连接。
        public static bool CreateS_BendConnection(this Pipe p1, Connector c1, Pipe p2, Connector c2, string strategy)
        {
            double deltaZ = Math.Abs(c1.Origin.Z - c2.Origin.Z);
            double diameter = p1.Diameter;
            Pipe connectingPipe = null;
            //高差大于50小于200且2倍DN微差连接，后退连接器，根据连接器高度生成斜管再连接
            if (deltaZ < p1.Diameter * 2 || deltaZ < 200 / 304.8)
            {
                if (deltaZ < (60 / 304.8))
                {
                    TaskDialog.Show("tt", "检测到管道差过小，请手工调整");
                    return false;
                }
                ////管道连接器后退指定距离，需要考虑管长不能为0或负值
                double retreatDistance = p1.Diameter * 3;
                if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
                {
                    TaskDialog.Show("限制", "管道长度不足，无法后退创建连接。");
                    return false;
                }
                XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
                if (newConn1p == null)
                {
                    TaskDialog.Show("tt", "后退管道失败，无法创建连接。");
                    return false;
                }
                connectingPipe = p1.NewPipeBetweenPoints(newConn1p, c2.Origin);
            }
            //45度连接 默认高差4倍DN
            else if (deltaZ < p1.Diameter * 4)
            {
                double retreatDistance = Math.Abs(c1.Origin.Z - c2.Origin.Z);
                if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
                {
                    TaskDialog.Show("限制", "管道长度不足以创建45度连接。");
                    return false;
                }
                // (保留原始几何逻辑)
                double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
                XYZ tempPoint = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
                if (tempPoint == null)
                {
                    TaskDialog.Show("tt", "步骤1失败：调整管道长度以对齐失败。");
                    return false;
                }
                // 在新点上再次后退
                XYZ finalPoint = p1.AdjustMEPCurveLength(tempPoint, retreatDistance);
                if (finalPoint == null)
                {
                    TaskDialog.Show("tt", "步骤2失败：为连接管预留空间失败。");
                    return false;
                }
                // 创建最终的斜管
                connectingPipe = p1.NewPipeBetweenPoints(finalPoint, c2.Origin);
            }
            //90度连接
            else if (deltaZ >= p1.Diameter * 4)
            {
                double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
                XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
                if (newConn1p == null)
                {
                    TaskDialog.Show("tt", "调整管道长度以对齐失败，无法创建立管。");
                    return false;
                }
                XYZ intersection2D = new XYZ(c1.Origin.X, c1.Origin.Y, 0);
                connectingPipe = TryCreateVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
            }
            if (connectingPipe == null)
            {
                return false;
            }
            // 步骤3: 执行统一的连接操作
            p1.NewElbowBy2MEPCurve(connectingPipe);
            p2.NewElbowBy2MEPCurve(connectingPipe);
            return true;
        }
        //建立斜坡度管连接
        public static Pipe TryCreateSlopePipe(this Pipe p1, Connector c1, Connector c2, double retreatDistance)
        {
            XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
            if (newConn1p == null)
            {
                TaskDialog.Show("tt", "未成功建立连接，请手工调整");
                return null;
            }
            //在管2和退后的连接器之间画新管，新管以管1类型，尺寸为准
            Pipe newPipe = Pipe.Create(p1.Document, p1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId(), p1.PipeType.Id, p1.ReferenceLevel.Id, newConn1p, c2.Origin);
            newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
            return newPipe;
        }
        public static bool ConnectNonParallelPipes(this Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
        {
            Document doc = p1.Document;
            if (l1.AreLinesCoPlanar(l2))
            {
                // 相交且共面：直接创建弯头
                p1.NewElbowBy2MEPCurve(p2);
                return true;
            }
            // 异面：创建立管连接
            var intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(l1, l2);
            if (intersection2D == null || intersection2D.DistanceTo(c1.Origin) > 4 || intersection2D.DistanceTo(c2.Origin) > 4)
            {
                TaskDialog.Show("限制", "管道在平面上交点过远，请手工调整。");
                return false;
            }
            double z1 = c1.Origin.Z;
            double z2 = c2.Origin.Z;
            // 调整原管道至交点
            p1.AdjustMEPCurveLength(c1.Origin, -c1.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z1)));
            p2.AdjustMEPCurveLength(c2.Origin, -c2.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z2)));
            doc.Regenerate();
            // 创建立管
            Pipe verticalPipe = TryCreateVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
            if (verticalPipe == null) return false;
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
            doc.Regenerate();
            // 连接
            p1.NewElbowBy2MEPCurve(verticalPipe);
            p2.NewElbowBy2MEPCurve(verticalPipe);
            return true;
        }
        //基于参照管，两连接器高差，交点建立垂直立管，可复用
        public static Pipe TryCreateVerticalPipe(this Pipe p1, Connector c1, XYZ cp2, XYZ intersection2D, string strategy)
        {
            Document doc = p1.Document;
            double pipeDiameter = p1.Diameter;
            double heightDifference = Math.Abs(c1.Origin.Z - cp2.Z);
            double requiredMultiplier = 0;
            if (strategy == "高概率")
            {
                requiredMultiplier = 6;
            }
            else if (strategy == "中概率")
            {
                requiredMultiplier = 4;
            }
            double minRequiredHeight = pipeDiameter * requiredMultiplier;
            // 2. 检查实际高差是否满足要求
            if (heightDifference < minRequiredHeight)
            {
                // 高差不足，不满足创建条件。直接返回null，由调用者决定是否提示用户。
                TaskDialog.Show("tt", $"创建立管失败：实际高差 {heightDifference * 304.8:F3} < 所需最小高差 {minRequiredHeight * 304.8:F3} (策略: {strategy})");
                return null;
            }
            double z1 = c1.Origin.Z;
            double z2 = cp2.Z;
            if (Math.Abs(z1 - z2) < 0.01) // 0.01 feet
            {
                TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。"); return null;
            }
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);
            XYZ bottomPoint = new XYZ(intersection2D.X, intersection2D.Y, minZ);
            XYZ topPoint = new XYZ(intersection2D.X, intersection2D.Y, maxZ);
            Pipe verticalPipe = p1.NewPipeBetweenPoints(bottomPoint, topPoint);
            return verticalPipe;
        }

        //0623 管道T连接
        // T型连接的核心调度方法
        public static bool ConnectTeePipes(this Pipe mainPipe, Line mainLine, Pipe branchPipe, Connector branchConn, Line branchLine, string strategy)
        {
            Document doc = mainPipe.Document;
            // Rule 3: 判断管关系，平行管无论是否共线、共面均退出
            if (mainLine.IsParallelTo(branchLine))
            {
                TaskDialog.Show("限制", "T型连接不支持两根平行的管道。");
                return false;
            }
            // 计算两根无限长直线在XY平面上的交点
            XYZ intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(mainLine, branchLine);
            if (intersection2D == null)
            {
                // 理论上不会发生，因为已经排除了平行情况
                return false;
            }
            // 将2D交点提升到主管的高度，得到空间中的打断点
            XYZ breakPointOnMain = new XYZ(intersection2D.X, intersection2D.Y, mainLine.Origin.Z);
            // 判断1: 交点是否在主管的物理范围内?
            bool isBreakPointOnMainSegment = mainLine.IsPointOnLine(breakPointOnMain);
            // 判断2: 交点是否也在支管的物理范围内?
            // (将交点投影到支管高度来判断)
            XYZ breakPointOnBranch = new XYZ(intersection2D.X, intersection2D.Y, branchLine.Origin.Z);
            bool isBreakPointOnBranchSegment = branchLine.IsPointOnLine(breakPointOnBranch);
            // 如果交点不在主管上，则无法进行任何T型连接
            if (!isBreakPointOnMainSegment)
            {
                TaskDialog.Show("限制", "管道投影交点不在主管的物理范围内。");
                return false;
            }
            //判断管关系，共面管道直接生成三通
            if (mainLine.AreLinesCoPlanar(branchLine))
            {
                //调整支管长度，使其端点精确到达打断点
                double distToIntersection = branchConn.Origin.DistanceTo(breakPointOnMain);
                branchPipe.AdjustMEPCurveLength(branchConn.Origin, -distToIntersection);
                if (BreakPipeAndCreateTee(doc, mainPipe, breakPointOnMain, branchPipe))
                {
                    return true;
                }
                return false;
            }
            // Rule 5: 不共面管道，根据高差创建连接
            else
            {
                return ConnectSkewTee(mainPipe, breakPointOnMain, branchPipe, branchConn, strategy, isBreakPointOnBranchSegment);
            }
        }
        //处理不共面（异面）管道的T型连接
        public static bool ConnectSkewTee(this Pipe mainPipe, XYZ breakPoint, Pipe branchPipe, Connector branchConn, string strategy, bool useCrossConnectionLogic)
        {
            Document doc = mainPipe.Document;
            // 计算高差：支管端点与它在主管上投影点的高度差
            double deltaZ = Math.Abs(branchConn.Origin.Z - breakPoint.Z);
            double diameter = branchPipe.Diameter;

            // --- 场景1: 高差足够大，且几何关系为“交叉” ---
            // 这是我们新增的智能分支
            if (deltaZ > diameter * 6 && useCrossConnectionLogic)
            {
                //TaskDialog.Show("智能连接提示", "检测到交叉管道关系，将使用立管和双三通进行连接。");
                //// 直接调用我们封装好的交叉连接方法
                return ConnectCrossPipes(mainPipe, branchPipe);
            }
            // --- 场景2: 其他所有异面情况 (高差小 或 几何关系非“交叉”) ---
            // 走原有的“三通+弯头”或“三通+斜管”逻辑
            // 检查支管端头距离投影点是否过远
            if (breakPoint.DistanceTo(branchConn.Origin) > 2 * 3.28)
            {
                TaskDialog.Show("限制", "支管端头距离其在主管上的投影点过远(>2m)，无法自动连接。");
                return false;
            }
            // (以下是您原有的 ConnectSkewTee 逻辑)
            Pipe connectingPipe = null;
            if (deltaZ < diameter * 4) // 微差/斜管连接
            {
                double retreatDistance = (deltaZ < diameter * 2) ? diameter * 3 : deltaZ;
                if (branchPipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance) return false;
                XYZ newBranchEndPoint = branchPipe.AdjustMEPCurveLength(branchConn.Origin, retreatDistance);
                if (newBranchEndPoint == null) return false;
                connectingPipe = branchPipe.NewPipeBetweenPoints(newBranchEndPoint, breakPoint);
            }
            else // 立管连接 (Tee + Elbow)
            {
                XYZ tempC1 = breakPoint;
                connectingPipe = TryCreateVerticalPipe(branchPipe, branchConn, tempC1, breakPoint, strategy);
            }
            if (connectingPipe == null) return false;
            if (!BreakPipeAndCreateTee(mainPipe.Document, mainPipe, breakPoint, connectingPipe)) return false;
            branchPipe.NewElbowBy2MEPCurve(connectingPipe);
            return true;
        }
        // 在指定点打断主管，并与支管创建一个三通。
        public static bool BreakPipeAndCreateTee(this Document doc, Pipe mainPipe, XYZ breakPoint, MEPCurve branchElement)
        {
            // 1. 打断主管，返回新生成管道的ID
            ElementId newPipeId = PlumbingUtils.BreakCurve(doc, mainPipe.Id, breakPoint);
            doc.Regenerate();
            Pipe newPipePart = doc.GetElement(newPipeId) as Pipe;
            if (newPipePart == null) return false;
            // 2. 找到打断点附近的四个连接器
            Connector mainConn1 = mainPipe.GetClosestConnector(breakPoint);
            Connector mainConn2 = newPipePart.GetClosestConnector(breakPoint);
            Connector branchConn = branchElement.GetClosestConnector(breakPoint);
            if (mainConn1 == null || mainConn2 == null || branchConn == null) return false;
            // 3. 创建三通
            doc.Create.NewTeeFitting(mainConn1, mainConn2, branchConn);
            return true;
        }
        // 专用于处理两根异面交叉的水平管道，通过一根立管和两个三通进行连接。
        public static bool ConnectCrossPipes(this Pipe pipe1, Pipe pipe2)
        {
            Document doc = pipe1.Document;
            Line line1 = (pipe1.Location as LocationCurve).Curve as Line;
            Line line2 = (pipe2.Location as LocationCurve).Curve as Line;
            // 1. 计算XY平面上的投影交点
            XYZ intersectionPoint2D = MEPAnalysisExtension.GetIntersectionPoint2D(line1, line2);
            if (intersectionPoint2D == null)
            {
                TaskDialog.Show("错误", "两根管道在XY平面平行，无法生成垂直连接管。");
                return false;
            }
            // 2. 准备创建立管的坐标
            double z1 = line1.Origin.Z;
            double z2 = line2.Origin.Z;
            // 高度检查 (虽然调用前已检查，这里作为安全措施)
            if (Math.Abs(z1 - z2) < 0.2) // 约60mm
            {
                TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。");
                return false;
            }
            // 3. 创建立管
            XYZ bottomPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Min(z1, z2));
            XYZ topPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Max(z1, z2));
            ElementId systemTypeId = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
            ElementId pipeTypeId = pipe1.PipeType.Id;
            ElementId levelId = pipe1.ReferenceLevel.Id;
            Pipe riserPipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, bottomPoint, topPoint);
            riserPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe1.Diameter);
            // 4. 确定上下管道
            Pipe topPipe = z1 > z2 ? pipe1 : pipe2;
            Pipe bottomPipe = z1 > z2 ? pipe2 : pipe1;
            // 5. 连接顶部和底部
            if (!BreakPipeAndCreateTee(doc, topPipe, topPoint, riserPipe))
            {
                TaskDialog.Show("错误", "创建顶部三通连接失败。");
                return false;
            }
            if (!BreakPipeAndCreateTee(doc, bottomPipe, bottomPoint, riserPipe))
            {
                TaskDialog.Show("错误", "创建底部三通连接失败。");
                return false;
            }
            return true;
        }


    }
}

