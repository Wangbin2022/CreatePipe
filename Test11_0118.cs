using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using static CreatePipe.Utils.XYZComparer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Document = Autodesk.Revit.DB.Document;

//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]

    public class Test11_0118 : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        /// <summary>
        /// 检查边界框是否与平面相交
        /// </summary>
        private bool CheckBoundingBoxIntersectsPlane(BoundingBoxXYZ bbox, Plane plane)
        {
            if (bbox == null || plane == null) return false;
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            // 获取边界框的8个角点
            List<XYZ> corners = new List<XYZ>    {
                new XYZ(min.X, min.Y, min.Z),        new XYZ(max.X, min.Y, min.Z),
                new XYZ(min.X, max.Y, min.Z),        new XYZ(max.X, max.Y, min.Z),
                new XYZ(min.X, min.Y, max.Z),        new XYZ(max.X, min.Y, max.Z),
                new XYZ(min.X, max.Y, max.Z),        new XYZ(max.X, max.Y, max.Z)    };
            // 计算平面方程: ax + by + cz + d = 0
            // 平面法向量 (a, b, c)
            XYZ normal = plane.Normal;
            // 平面上的点
            XYZ origin = plane.Origin;
            // 计算 d = -(a*x0 + b*y0 + c*z0)
            double d = -(normal.X * origin.X + normal.Y * origin.Y + normal.Z * origin.Z);
            // 检查角点是否在平面两侧
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (XYZ point in corners)
            {
                // 计算有符号距离: (a*x + b*y + c*z + d) / sqrt(a^2 + b^2 + c^2)
                // 或者简化为 (a*x + b*y + c*z + d)，因为只需要判断符号
                double signedDistance = normal.X * point.X + normal.Y * point.Y + normal.Z * point.Z + d;
                // 点在平面上（距离接近0）
                if (Math.Abs(signedDistance) < 1e-6) return true;
                if (signedDistance > 0) hasPositive = true;
                else hasNegative = true;
                // 平面穿过边界框（点在两侧）
                if (hasPositive && hasNegative) return true;
            }
            // 所有点在同一侧，不相交
            return false;
        }
        /// <summary>
        /// 检查实体是否与平面相交
        /// </summary>
        private bool IsSolidIntersectPlane(Solid solid, Plane plane)
        {
            if (solid == null || solid.Faces.Size == 0 || plane == null) return false;
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            // 收集所有顶点并检查有符号距离
            List<double> distances = new List<double>();
            // 从边获取顶点
            foreach (Edge edge in solid.Edges)
            {
                Curve curve = edge.AsCurve();
                distances.Add(SignedDistanceTo(curve.GetEndPoint(0), normal, origin));
                distances.Add(SignedDistanceTo(curve.GetEndPoint(1), normal, origin));
            }
            // 从三角化面获取顶点（更密集）
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                Mesh mesh = face.Triangulate();
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    MeshTriangle triangle = mesh.get_Triangle(i);
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(0), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(1), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(2), normal, origin));
                }
            }
            // 检查距离分布
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (double d in distances)
            {
                if (Math.Abs(d) < 1e-6) return true;
                if (d > 0) hasPositive = true;
                else hasNegative = true;
                if (hasPositive && hasNegative) return true;
            }
            return false;
        }
        /// <summary>
        /// 检查网格是否与平面相交
        /// </summary>
        private bool IsMeshIntersectPlane(Mesh mesh, Plane plane)
        {
            if (mesh == null || mesh.NumTriangles == 0 || plane == null) return false;
            // 预计算平面参数，避免重复计算
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用点积）
                double d0 = SignedDistanceTo(v0, normal, origin);
                double d1 = SignedDistanceTo(v1, normal, origin);
                double d2 = SignedDistanceTo(v2, normal, origin);
                // 检查是否相交：有正有负或等于0（点在平面上）
                bool hasZero = Math.Abs(d0) < 1e-6 || Math.Abs(d1) < 1e-6 || Math.Abs(d2) < 1e-6;
                if (hasZero) return true;
                bool hasPositive = d0 > 0 || d1 > 0 || d2 > 0;
                bool hasNegative = d0 < 0 || d1 < 0 || d2 < 0;
                // 平面穿过三角形（点在两侧）
                if (hasPositive && hasNegative)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算点到平面的有符号距离（内联辅助方法，避免重复代码）
        /// </summary>
        private double SignedDistanceTo(XYZ point, XYZ planeNormal, XYZ planeOrigin)
        {
            XYZ toPoint = point - planeOrigin;
            return toPoint.DotProduct(planeNormal);
        }
        /// <summary>
        /// 获取几何元素与平面的所有交线
        /// </summary>
        private List<Curve> GetIntersectionCurvesWithPlane(GeometryElement geoElement, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (geoElement == null) return intersectionCurves;
            foreach (GeometryObject geoObj in geoElement)
            {
                Solid solid = geoObj as Solid;
                if (solid != null && solid.Faces.Size > 0)
                {
                    //获取实体与平面的交线
                    List<Curve> solidIntersections = GetSolidIntersectionCurvesWithPlane(solid, plane);
                    intersectionCurves.AddRange(solidIntersections);

                }
                else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
                {
                    //获取网格与平面的交线
                    List<Curve> meshIntersections = GetMeshIntersectionCurvesWithPlane(mesh, plane);
                    intersectionCurves.AddRange(meshIntersections);
                }
            }
            // 合并连接的曲线
            return intersectionCurves;
            //return MergeConnectedCurves(intersectionCurves);
        }
        /// <summary>
        /// 获取实体与平面的交线（最简可靠版）byKIMI
        /// </summary>
        private List<Curve> GetSolidIntersectionCurvesWithPlane(Solid solid, Plane plane)
        {
            var result = new List<Curve>();
            if (solid == null) return result;
            XYZ n = plane.Normal;
            XYZ o = plane.Origin;
            // 收集所有有效交点
            var pointSet = new HashSet<string>(); // 用于去重
            var allPoints = new List<XYZ>();
            Action<XYZ> addPoint = (p) =>
            {
                string key = $"{Math.Round(p.X, 6)},{Math.Round(p.Y, 6)},{Math.Round(p.Z, 6)}";
                if (!pointSet.Contains(key))
                {
                    pointSet.Add(key);
                    allPoints.Add(p);
                }
            };
            foreach (Edge edge in solid.Edges)
            {
                var c = edge.AsCurve();
                XYZ p0 = c.GetEndPoint(0);
                XYZ p1 = c.GetEndPoint(1);
                double d0 = (p0 - o).DotProduct(n);
                double d1 = (p1 - o).DotProduct(n);
                if (Math.Abs(d0) < 1e-6) addPoint(p0);
                if (Math.Abs(d1) < 1e-6) addPoint(p1);
                if (d0 * d1 < -1e-12)
                {
                    double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
                    addPoint(p0 + (p1 - p0) * t);
                }
            }
            if (allPoints.Count < 2) return result;
            // 在平面内排序并连接
            XYZ u = plane.XVec.Normalize();
            XYZ v = plane.YVec.Normalize();
            allPoints = allPoints.OrderBy(p => (p - o).DotProduct(u))
                .ThenBy(p => (p - o).DotProduct(v)).ToList();
            // 连接相邻点形成交线
            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                double dist = allPoints[i].DistanceTo(allPoints[i + 1]);
                if (dist > 1e-6 && dist < solid.GetBoundingBox().Max.DistanceTo(solid.GetBoundingBox().Min))
                {
                    result.Add(Line.CreateBound(allPoints[i], allPoints[i + 1]));
                }
            }
            return result;
        }
        /// <summary>
        /// 设置线条样式
        /// </summary>
        private void SetLineStyle(dynamic line, string styleName)
        {
            try
            {
                // 获取线型样式
                FilteredElementCollector collector = new FilteredElementCollector(line.Document);
                GraphicsStyle graphicsStyle = collector
                    .OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>()
                    .FirstOrDefault(g => g.Name == styleName);

                if (graphicsStyle != null)
                {
                    line.LineStyle = graphicsStyle;
                }
            }
            catch
            {
                // 如果设置失败，使用默认样式
            }
        }
        /// <summary>
        /// 获取网格与平面的交线
        /// </summary>
        private List<Curve> GetMeshIntersectionCurvesWithPlane(Mesh mesh, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (mesh == null || mesh.NumTriangles == 0) return intersectionCurves;
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用自定义方法）
                double d0 = SignedDistanceTo(v0, planeNormal, planeOrigin);
                double d1 = SignedDistanceTo(v1, planeNormal, planeOrigin);
                double d2 = SignedDistanceTo(v2, planeNormal, planeOrigin);
                // 检查三角形是否与平面相交
                List<XYZ> intersectionPoints = new List<XYZ>();
                // 检查每条边与平面的交点
                AddIntersectionPoint(v0, v1, d0, d1, plane, intersectionPoints);
                AddIntersectionPoint(v1, v2, d1, d2, plane, intersectionPoints);
                AddIntersectionPoint(v2, v0, d2, d0, plane, intersectionPoints);
                // 如果有两个交点，创建线段
                if (intersectionPoints.Count == 2)
                {
                    Line line = Line.CreateBound(intersectionPoints[0], intersectionPoints[1]);
                    intersectionCurves.Add(line);
                }
            }
            return intersectionCurves;
        }
        /// <summary>
        /// 添加边的交点
        /// </summary>
        private void AddIntersectionPoint(XYZ p1, XYZ p2, double d1, double d2, Plane plane, List<XYZ> points)
        {
            if (Math.Abs(d1) < 1e-9)
            {
                points.Add(p1);
            }
            else if (Math.Abs(d2) < 1e-9)
            {
                points.Add(p2);
            }
            else if (d1 * d2 < 0) // 点在平面两侧
            {
                double t = -d1 / (d2 - d1); // 插值参数
                XYZ intersection = p1 + t * (p2 - p1);
                points.Add(intersection);
            }
        }
        /// <summary>
        /// 在平面坐标系下排序点
        /// </summary>
        private List<XYZ> SortPointsOnPlane(List<XYZ> points, Plane plane)
        {
            XYZ xAxis = plane.XVec.Normalize();
            XYZ yAxis = plane.YVec.Normalize();
            return points
                .Select(p => new
                {
                    Point = p,
                    U = (p - plane.Origin).DotProduct(xAxis),
                    V = (p - plane.Origin).DotProduct(yAxis)
                }).OrderBy(item => item.U)
                .ThenBy(item => item.V).Select(item => item.Point).ToList();
        }
        /// <summary>
        /// 获取直线与平面的交线段（仅支持Line）
        /// </summary>
        private List<Curve> GetLinePlaneSegments(Line line, Plane plane)
        {
            List<Curve> segments = new List<Curve>();
            if (line == null || plane == null) return segments;
            XYZ start = line.GetEndPoint(0);
            XYZ end = line.GetEndPoint(1);
            double d0 = (start - plane.Origin).DotProduct(plane.Normal);
            double d1 = (end - plane.Origin).DotProduct(plane.Normal);
            const double eps = 1e-6;
            // 完全在平面内
            if (Math.Abs(d0) < eps && Math.Abs(d1) < eps)
            {
                segments.Add(line.Clone());
                return segments;
            }
            // 无交点（同侧且不接触）
            if (d0 > eps && d1 > eps) return segments;
            if (d0 < -eps && d1 < -eps) return segments;
            // 计算交点
            double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
            XYZ intersection = start + (end - start) * t;
            // 返回平面内的部分（根据有符号距离判断）
            if (d0 >= -eps && d1 >= -eps)
            {
                // 都在正侧或接触平面
                if (d0 < eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 < eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else if (d0 <= eps && d1 <= eps)
            {
                // 都在负侧或接触平面
                if (d0 > -eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 > -eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else
            {
                // 跨平面，返回两侧
                segments.Add(Line.CreateBound(start, intersection));
                segments.Add(Line.CreateBound(intersection, end));
            }
            return segments;
        }
        /// <summary>
        /// 合并连接的曲线
        /// </summary>
        private List<Curve> MergeConnectedCurves(List<Curve> curves)
        {
            if (curves.Count <= 1) return curves;
            List<Curve> mergedCurves = new List<Curve>();
            List<Curve> remaining = new List<Curve>(curves);
            while (remaining.Count > 0)
            {
                Curve current = remaining[0];
                remaining.RemoveAt(0);
                bool merged = true;
                while (merged && remaining.Count > 0)
                {
                    merged = false;
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        if (AreCurvesConnected(current, remaining[i]))
                        {
                            // 合并曲线
                            current = MergeTwoCurves(current, remaining[i]);
                            remaining.RemoveAt(i);
                            merged = true;
                            break;
                        }
                    }
                }
                mergedCurves.Add(current);
            }
            return mergedCurves;
        }
        /// <summary>
        /// 合并两条曲线
        /// </summary>
        private Curve MergeTwoCurves(Curve curve1, Curve curve2)
        {
            // 简单实现：创建一条新的直线连接两个端点
            XYZ start = curve1.GetEndPoint(0);
            XYZ end = curve2.GetEndPoint(1);
            return Line.CreateBound(start, end);
        }
        /// <summary>
        /// 判断两条曲线是否连接
        /// </summary>
        private bool AreCurvesConnected(Curve curve1, Curve curve2, double tolerance = 1e-6)
        {
            XYZ end1 = curve1.GetEndPoint(1);
            XYZ start2 = curve2.GetEndPoint(0);
            return end1.DistanceTo(start2) < tolerance;
        }

        /// <summary>
        /// 将曲线投影到当前视图平面,没使用此方法？？
        /// </summary>
        private Curve ProjectCurveToViewPlane(Curve curve, View activeView)
        {
            if (curve == null || activeView == null) return curve;
            // 获取视图平面（适用于平、立、剖面）
            Plane viewPlane = activeView.SketchPlane.GetPlane();
            if (viewPlane == null) return curve;
            List<XYZ> projectedPoints = new List<XYZ>();
            // 投影曲线的关键点
            IList<XYZ> points = curve.Tessellate();
            foreach (XYZ point in points)
            {
                XYZ projectedPoint = ProjectPointToPlane(point, viewPlane);
                projectedPoints.Add(projectedPoint);
            }
            if (projectedPoints.Count < 2) return curve;
            XYZ start = projectedPoints[0];
            XYZ end = projectedPoints[projectedPoints.Count - 1];
            // 检查投影后是否有有效长度
            double projectedLength = start.DistanceTo(end);
            if (projectedLength < 1e-6)
            {
                // 投影长度为0，曲线与视图平面垂直，返回空（防错）
                return null;
            }
            // 检查所有投影点是否基本重合（更严格的垂直判断）
            bool allSame = true;
            for (int i = 1; i < projectedPoints.Count; i++)
            {
                if (!projectedPoints[i].IsAlmostEqualTo(start))
                {
                    allSame = false;
                    break;
                }
            }
            if (allSame) return curve;
            // 根据原始曲线类型创建对应的投影曲线
            if (curve is Line) return Line.CreateBound(start, end);
            // 弧线投影后可能变为直线或保持弧线简化处理：返回直线段
            else if (curve is Arc arc) return Line.CreateBound(start, end);
            // 其他复杂曲线，返回首尾投影点连线
            else return Line.CreateBound(start, end);
        }
        /// <summary>
        /// 将点投影到平面
        /// </summary>
        private XYZ ProjectPointToPlane(XYZ point, Plane plane)
        {
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            double distance = SignedDistanceTo(point, planeNormal, planeOrigin);
            return point - distance * plane.Normal;
        }

        /// <summary>
        /// 在视图中绘制可见性分析的结果
        /// </summary>
        private void DrawVisibilityResults(Document doc, View view, XYZ observerPoint, List<XYZ> visiblePoints)
        {
            // 创建新的图形样式以便区分
            GraphicsStyle gs = GetOrCreateGraphicsStyle(doc, "可见性分析线");
            if (visiblePoints.Count <= 1) return;
            // 找到可见区域的边界点（一个简化的方法是找到凸包）
            List<XYZ> boundaryPoints = FindConvexHull(visiblePoints);
            // 1. 绘制可见区域在标记牌上的轮廓线 (最大最小范围)
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                XYZ p1 = boundaryPoints[i];
                XYZ p2 = boundaryPoints[(i + 1) % boundaryPoints.Count]; // 连接到下一个点，最后一个点连回第一个
                Line line = Line.CreateBound(p1, p2);
                doc.Create.NewModelCurve(line, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
            }
            // 2. 绘制从观察点到可见区域边界的“视锥”
            foreach (XYZ boundaryPoint in boundaryPoints)
            {
                Line coneLine = Line.CreateBound(observerPoint, boundaryPoint);
                ModelCurve mc = doc.Create.NewModelCurve(coneLine, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
                mc.LineStyle = gs; // 应用自定义图形样式
            }
        }
        /// <summary>
        /// 找到一组点的2D凸包 (投影到XY平面)
        /// 这是一个简化的凸包算法 (Gift wrapping algorithm)
        /// </summary>
        public List<XYZ> FindConvexHull(List<XYZ> points)
        {
            if (points.Count <= 2) return points;
            List<XYZ> hull = new List<XYZ>();
            // 找到最左边的点作为起点
            XYZ startPoint = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            XYZ currentPoint = startPoint;
            do
            {
                hull.Add(currentPoint);
                XYZ nextPoint = points[0];
                foreach (XYZ p in points)
                {
                    if (nextPoint == currentPoint || IsLeft(currentPoint, nextPoint, p) > 0)
                    {
                        nextPoint = p;
                    }
                }
                currentPoint = nextPoint;
            } while (currentPoint != startPoint);
            return hull;
        }
        private double IsLeft(XYZ p1, XYZ p2, XYZ p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        }
        /// <summary>
        /// 获取或创建用于可视化的图形样式
        /// </summary>
        private GraphicsStyle GetOrCreateGraphicsStyle(Document doc, string styleName)
        {
            var cat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            var subCat = cat.SubCategories.get_Item(styleName);
            if (subCat == null)
            {
                subCat = doc.Settings.Categories.NewSubcategory(cat, styleName);
                subCat.LineColor = new Color(255, 0, 0); // 红色
                subCat.SetLineWeight(5, GraphicsStyleType.Projection);
            }
            return subCat.GetGraphicsStyle(GraphicsStyleType.Projection);
        }
        /// <summary>
        /// 查找最适合进行射线检测的3D视图
        /// </summary>
        private View3D FindBest3DView(Document doc)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
            return default3DView ?? collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        }
        /// <summary>
        /// 从楼板的实体几何体中提取其底面的所有轮廓环路。
        /// </summary>
        /// <param name="floor">要分析的楼板</param>
        /// <returns>包含所有轮廓的CurveArray列表</returns>
        private List<CurveArray> GetFloorLoopsFromGeometry(Floor floor)
        {
            var loops = new List<CurveArray>();
            Options geomOptions = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, View = floor.Document.ActiveView };
            GeometryElement geoElem = floor.get_Geometry(geomOptions);
            if (geoElem == null) return null;
            Solid solid = geoElem.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);
            if (solid == null) return null;
            PlanarFace bottomFace = null;
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                PlanarFace pFace = face as PlanarFace;
                if (pFace != null && pFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ.Negate()))
                {
                    bottomFace = pFace;
                    break;
                }
            }
            if (bottomFace == null) return null;
            // *** 这是修正的核心部分 ***
            // bottomFace.EdgeLoops 返回 IList<EdgeArray>
            var edgeLoopList = bottomFace.EdgeLoops;
            // 遍历每个 EdgeArray (每个代表一个闭合环路)
            foreach (EdgeArray edgeArray in edgeLoopList)
            {
                // 为每个环路创建一个新的 CurveArray 来存放曲线
                CurveArray curveArray = new CurveArray();
                // 遍历环路中的每一条边 (Edge)
                foreach (Edge edge in edgeArray)
                {
                    // 从边中提取几何曲线 (Curve) 并添加到 CurveArray 中
                    curveArray.Append(edge.AsCurve());
                }
                // 将转换好的 CurveArray 添加到结果列表中
                loops.Add(curveArray);
            }
            return loops;
        }
        ///// <summary>
        ///// 确保两个元素被连接，并且第一个元素切割第二个元素。
        ///// </summary>
        private void EnsureJoinOrder(Document doc, Element cutter, Element cuttee)
        {
            if (!JoinGeometryUtils.AreElementsJoined(doc, cutter, cuttee))
            {
                try
                {
                    JoinGeometryUtils.JoinGeometry(doc, cutter, cuttee);
                }
                catch (Exception ex)
                {
                    // 记录连接失败的日志，对于调试很重要
                    System.Diagnostics.Debug.WriteLine($"无法连接元素 {cutter.Id} 和 {cuttee.Id}: {ex.Message}");
                }
            }
            else
            {
                // 如果已经连接，检查顺序是否正确
                if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, cutter, cuttee))
                {
                    try
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, cutter, cuttee);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"无法切换元素 {cutter.Id} 和 {cuttee.Id} 的连接顺序: {ex.Message}");
                    }
                }
            }
        }
        ///// <summary>
        ///// 获取与给定元素包围盒相交的元素（梁和楼板）。
        ///// </summary>
        private List<Element> GetIntersectingElements(Document doc, Element element, double expansionAmount)
        {
            // 使用 get_BoundingBox(null) 获取模型空间的完整3D包围盒
            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox == null) return new List<Element>();
            // 扩大包围盒以确保捕捉到所有接触的元素
            Outline outline = new Outline(bbox.Min - new XYZ(expansionAmount, expansionAmount, expansionAmount),
                                          bbox.Max + new XYZ(expansionAmount, expansionAmount, expansionAmount));
            // 使用 BoundingBoxIntersectsFilter 时，公差应非常小
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline, 1e-9);
            // 定义要查找的元素类别
            var categoryFilters = new List<ElementFilter>
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                new ElementCategoryFilter(BuiltInCategory.OST_Floors)
            };
            var logicalOrFilter = new LogicalOrFilter(categoryFilters);
            var finalFilter = new LogicalAndFilter(bbFilter, logicalOrFilter);
            return new FilteredElementCollector(doc).WherePasses(finalFilter)
                .Where(e => e.Id != element.Id).ToList();
        }
        ////1003 
        ///// <summary>
        ///// 查找一个最适合进行射线检测的3D视图。
        ///// 优先选择默认的 {3D} 视图，因为它通常包含所有模型元素。
        ///// </summary>
        ///// 重复方法回头看一下是否去重？
        //private static View3D FindBest3DView(Document doc)
        //{
        //    var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
        //    // 优先寻找默认的 {3D} 视图
        //    View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
        //    if (default3DView != null)
        //    {
        //        return default3DView;
        //    }
        //    // 如果找不到，再寻找任何一个非模板的3D视图作为备用
        //    return collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        //}
        /////代码解析
        ////输入：通过 PickPoint 和 PickObject(ObjectType.Face)，我们精确地获取了观察点和用户想要分析的那个面。这是最可靠的方式。
        ////采样：
        ////targetFace.GetBoundingBox() 获取了面的 UV 参数范围。
        ////通过双重 for 循环，我们在 UV 空间中均匀地创建了一个网格。
        ////targetFace.Evaluate(new UV(u, v)) 将二维的 UV 参数转换为了三维世界坐标 XYZ。
        ////可见性检测：
        ////ReferenceIntersector 的构造函数传入了 targetElement.Id，这是一个优化，它会忽略目标本身，防止射线总是命中自己。但为了逻辑的严谨性，我们还是通过比较距离来判断，这样更通用。
        ////distanceToFace 是原始距离，hitDistance 是碰撞距离。Math.Abs(hitDistance - distanceToFace) < tolerance 是判断是否命中了目标本身的关键。
        ////可视化：
        ////"最大最小可见范围" 的最佳数学表达是可见区域的轮廓。
        ////我提供了一个简化的 凸包（Convex Hull） 算法 FindConvexHull 来找到包围所有可见点的最小多边形。这个多边形就是可见区域的边界。
        ////DrawVisibilityResults 方法做了两件事：
        ////用模型线在标记牌上绘制出这个凸包轮廓。
        ////从观察点向凸包的每个顶点发射连线，形成一个视锥（Viewing Frustum），直观地展示了可见范围。
        ////为了让绘制的线更醒目，我写了一个 GetOrCreateGraphicsStyle 方法来创建一个红色的、较粗的线样式。
        /// <summary>
        /// 执行射线检测并返回最近的碰撞图元ID
        /// </summary>
        /// <param name="doc">Revit文档</param>
        /// <param name="origin">射线起点</param>
        /// <param name="direction">射线方向</param>
        /// <param name="view">用于检测的视图（可选）</param>
        /// <returns>碰撞到的第一个图元的ElementId，如果没有碰撞则返回ElementId.InvalidElementId</returns>
        public static ElementId RaycastNearest(Document doc, XYZ origin, XYZ direction, double deltaHeight, Autodesk.Revit.DB.View view = null)
        {
            // 规范化方向向量
            direction = direction.Normalize();
            // 创建ReferenceIntersector
            ReferenceIntersector intersector;
            if (view != null)
            {
                intersector = new ReferenceIntersector((View3D)view);
            }
            else
            {
                // 使用3D视图设置进行检测
                intersector = new ReferenceIntersector(Find3DView(doc) ?? throw new System.Exception("找不到可用的3D视图"));
            }
            // 设置查找最近的交点
            intersector.TargetType = FindReferenceTarget.Face;
            intersector.FindReferencesInRevitLinks = true;
            XYZ originptWithHeight = new XYZ(origin.X, origin.Y, deltaHeight / 304.8);
            // 执行射线检测
            ReferenceWithContext referenceWithContext = intersector.FindNearest(originptWithHeight, direction);
            //ReferenceWithContext referenceWithContext = intersector.FindNearest(origin, direction);
            if (referenceWithContext == null) return ElementId.InvalidElementId;
            // 获取碰撞图元的ElementId
            Reference reference = referenceWithContext.GetReference();
            return reference?.ElementId ?? ElementId.InvalidElementId;
        }
        private static View3D Find3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(View3D));
            foreach (View3D view in collector)
            {
                if (!view.IsTemplate && view.Name != "{3D}") return view;
            }
            return null;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //0721 楼梯收集
            StairsManagerView stairsManagerView = new StairsManagerView(uiApp);
            stairsManagerView.Show();


            ////////1003 检测A-B点之间可见
            ////try
            ////{
            ////    // --- 步骤 1: 获取用户输入 ---
            ////    // 1.1 获取观察点
            ////    XYZ observerPoint = uiDoc.Selection.PickPoint("请选择观察点 (眼睛的位置)");
            ////    // 1.2 获取目标面
            ////    Reference faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "请选择标记牌的正面");
            ////    Element targetElement = doc.GetElement(faceRef);
            ////    Face targetFace = targetElement.GetGeometryObjectFromReference(faceRef) as Face;
            ////    if (targetFace == null)
            ////    {
            ////        message = "未能获取有效的几何面。";
            ////        return Result.Failed;
            ////    }
            ////    // --- 步骤 2 & 3: 采样并进行可见性测试 ---
            ////    // 定义采样网格的密度 (例如 10x10)
            ////    int gridResolutionU = 15;
            ////    int gridResolutionV = 15;
            ////    List<XYZ> visiblePoints = new List<XYZ>();
            ////    List<XYZ> occludedPoints = new List<XYZ>();
            ////    BoundingBoxUV bbox = targetFace.GetBoundingBox();
            ////    UV min = bbox.Min;
            ////    UV max = bbox.Max;
            ////    // 准备 ReferenceIntersector
            ////    View3D view3D = FindBest3DView(doc);
            ////    if (view3D == null)
            ////    {
            ////        message = "需要一个3D视图来进行可见性分析。";
            ////        return Result.Failed;
            ////    }
            ////    ReferenceIntersector intersector = new ReferenceIntersector(targetElement.Id, FindReferenceTarget.Face, view3D);
            ////    intersector.FindReferencesInRevitLinks = true;
            ////    // 遍历采样网格
            ////    for (int i = 0; i <= gridResolutionU; i++)
            ////    {
            ////        for (int j = 0; j <= gridResolutionV; j++)
            ////        {
            ////            double u = min.U + (max.U - min.U) * i / gridResolutionU;
            ////            double v = min.V + (max.V - min.V) * j / gridResolutionV;
            ////            XYZ samplePointOnFace = targetFace.Evaluate(new UV(u, v));
            ////            XYZ direction = (samplePointOnFace - observerPoint).Normalize();
            ////            double distanceToFace = observerPoint.DistanceTo(samplePointOnFace);
            ////            // 执行射线检测
            ////            ReferenceWithContext refWithContext = intersector.FindNearest(observerPoint, direction);
            ////            bool isVisible = false;
            ////            double tolerance = 0.001; // 精度容差 (约0.3mm)
            ////            if (refWithContext == null)
            ////            {
            ////                // 射线未与任何物体相交，说明该点可见 (在开放空间中)
            ////                isVisible = true;
            ////            }
            ////            else
            ////            {
            ////                double hitDistance = refWithContext.Proximity;
            ////                // 如果碰撞点距离非常接近目标点，则认为是可见的
            ////                if (Math.Abs(hitDistance - distanceToFace) < tolerance)
            ////                {
            ////                    isVisible = true;
            ////                }
            ////            }
            ////            if (isVisible)
            ////            {
            ////                visiblePoints.Add(samplePointOnFace);
            ////            }
            ////            else
            ////            {
            ////                occludedPoints.Add(samplePointOnFace);
            ////            }
            ////        }
            ////    }
            ////    // --- 步骤 4: 可视化结果 ---
            ////    if (visiblePoints.Count == 0)
            ////    {
            ////        TaskDialog.Show("结果", "标记牌完全被遮挡，不可见。");
            ////        return Result.Succeeded;
            ////    }
            ////    using (Transaction tx = new Transaction(doc, "绘制可见性范围"))
            ////    {
            ////        tx.Start();
            ////        DrawVisibilityResults(doc, activeView, observerPoint, visiblePoints);
            ////        tx.Commit();
            ////    }
            ////    TaskDialog.Show("完成", $"可见性分析完成。\n可见采样点: {visiblePoints.Count}    已在视图中绘制可见范围。");
            ////    return Result.Succeeded;
            ////}
            ////catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            ////{
            ////    return Result.Cancelled;
            ////}
            ////catch (Exception ex)
            ////{
            ////    message = ex.Message;
            ////    return Result.Failed;
            ////}
            //////1003 升级二维射线检测方法
            //try
            //{
            //    // 1. 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择扫描中心点");
            //    // 2. 定义扫描高度 (例如 200mm)
            //    double deltaHeightMM = 200.0;
            //    double heightOffset = UnitUtils.ConvertToInternalUnits(deltaHeightMM, UnitTypeId.Millimeters);
            //    XYZ scanOrigin = origin + new XYZ(0, 0, heightOffset);
            //    // 3. 准备结果容器和字符串构建器
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 4. (性能优化) 在循环外创建 ReferenceIntersector
            //    View3D view3D = FindBest3DView(doc);
            //    if (view3D == null)
            //    {
            //        message = "项目中找不到可用于检测的3D视图。";
            //        return Result.Failed;
            //    }
            //    ReferenceIntersector intersector = new ReferenceIntersector(view3D);
            //    intersector.TargetType = FindReferenceTarget.Face;
            //    intersector.FindReferencesInRevitLinks = true;
            //    // 5. 在XY平面进行360度检测
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        double radians = angle * Math.PI / 180.0;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ReferenceWithContext refWithContext = intersector.FindNearest(scanOrigin, direction);
            //        if (refWithContext != null)
            //        {
            //            Reference reference = refWithContext.GetReference();
            //            if (reference != null && reference.ElementId != ElementId.InvalidElementId)
            //            {
            //                hitElementIds.Add(reference.ElementId);
            //            }
            //        }
            //    }
            //    // 6. 处理并显示结果
            //    if (hitElementIds.Count == 0)
            //    {
            //        TaskDialog.Show("扫描结果", "在指定高度和范围内没有检测到任何对象。");
            //    }
            //    else
            //    {
            //        foreach (var id in hitElementIds)
            //        {
            //            Element elem = doc.GetElement(id);
            //            stringBuilder.AppendLine($"ID: {id.IntegerValue}, 名称: {elem?.Name ?? "N/A"}");
            //        }
            //        TaskDialog.Show("扫描结果", $"共检测到 {hitElementIds.Count} 个独立对象:{stringBuilder}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds.ToList());
            //        uiDoc.RefreshActiveView();
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户按 ESC 取消，是正常操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    // 其他意外错误
            //    message = ex.Message;
            //    return Result.Failed;
            //}
            //////0811 射线360扫射检测碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    double deltaHeight = 200;
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 5. 在XY平面进行360度检测（每1度一次）
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        // 计算当前角度方向向量（Z=0）
            //        double radians = angle * Math.PI / 180;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //        if (hitElementId != ElementId.InvalidElementId)
            //        {
            //            hitElementIds.Add(hitElementId);
            //        }
            //    }
            //    foreach (var item in hitElementIds)
            //    {
            //        stringBuilder.AppendLine(item.ToString());
            //    }
            //    if (hitElementIds == null) { TaskDialog.Show("结果", "没有检测到碰撞对象"); }
            //    else
            //    {
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElementIds.Count}\n" + $"ID: {stringBuilder.ToString()}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds);
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            ////0811 二维射线法手搓尝试，单点单方向碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    // 定义射线方向（这里使用X轴方向）
            //    XYZ direction = XYZ.BasisX;
            //    double deltaHeight = 200;
            //    // 执行射线检测
            //    ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //    if (hitElementId == ElementId.InvalidElementId)
            //    {
            //        TaskDialog.Show("结果", "没有检测到碰撞对象");
            //    }
            //    else
            //    {
            //        Element hitElement = doc.GetElement(hitElementId);
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElement.Name}\n" + $"ID: {hitElementId.IntegerValue}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(new List<ElementId> { hitElementId });
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            //例程结束

            //////1014 补充沟体替换
            //CircleGaugePlaceView circleGaugePlaceView = new CircleGaugePlaceView(uiApp);
            //circleGaugePlaceView.Show();

            ////////1003 SplitElementsCommand 变形缝、后浇带打断板、梁 遗留考虑问题较多，板边界，连线方向等等
            ////0704 切割楼板应参考官方示例 暂缓
            ////0425 参照平面切割测试
            //// 检查当前视图是否为平面、立面或剖面
            //if (!(doc.ActiveView.ViewType is ViewType.FloorPlan || doc.ActiveView.ViewType is ViewType.Section || doc.ActiveView.ViewType is ViewType.Elevation))
            //{
            //    message = "请在平面、立面或剖面视图中运行此命令。";
            //    return Result.Failed;
            //}
            //// 1. 让用户选择一个参照平面
            //Reference refPlaneRef;
            //try
            //{
            //    refPlaneRef = uiDoc.Selection.PickObject(ObjectType.Element, new ReferencePlaneSelectionFilter(), "请选择一个用于打断的参照平面");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //// 获取选中的参照平面元素
            //ReferencePlane selectedRefPlane = doc.GetElement(refPlaneRef) as ReferencePlane;
            //if (selectedRefPlane == null)
            //{
            //    message = "未选择有效的参照平面。";
            //    return Result.Failed;
            //}
            //// 获取参照平面的几何信息
            //Plane refPlane = selectedRefPlane.GetPlane();
            //XYZ planeOrigin = refPlane.Origin;
            //XYZ planeNormal = refPlane.Normal;
            //// 收集所有目标元素
            //List<Element> targetElements = new List<Element>();
            //// 楼板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Floor)).WhereElementIsNotElementType().ToList());
            //// 天花板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Ceiling)).WhereElementIsNotElementType().ToList());
            //// 迹线屋面（通过参数筛选）
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(RoofBase)).WhereElementIsNotElementType().Cast<RoofBase>()
            //    .Where(r =>
            //    {
            //        return r is FootPrintRoof;
            //        //// 迹线屋面有 Footprint 草图，拉伸屋面没有
            //    }).Cast<Element>().ToList());
            //// 存储与参照平面相交且正交的楼板信息
            //List<KeyValuePair<ElementId, string>> intersectingFloors = new List<KeyValuePair<ElementId, string>>();
            //List<ElementId> intersectingFloorIds = new List<ElementId>();
            //foreach (Element floor in targetElements)
            //{
            //    // 获取楼板的边界框（快速筛选）
            //    BoundingBoxXYZ floorBbox = floor.get_BoundingBox(activeView);
            //    if (floorBbox == null) continue;
            //    // 快速检测：检查楼板的边界框是否与参照平面相交（可选，提高性能）
            //    bool bboxIntersects = CheckBoundingBoxIntersectsPlane(floorBbox, refPlane);
            //    if (!bboxIntersects) continue;
            //    // 获取楼板的几何信息进行精确检测
            //    Options geoOptions = new Options();
            //    geoOptions.ComputeReferences = true;
            //    geoOptions.DetailLevel = ViewDetailLevel.Fine;
            //    GeometryElement geoElement = floor.get_Geometry(geoOptions);
            //    if (geoElement == null) continue;
            //    bool isIntersectingAndOrthogonal = false;
            //    // 遍历楼板的几何实体进行精确相交和正交检测
            //    foreach (GeometryObject geoObj in geoElement)
            //    {
            //        Solid solid = geoObj as Solid;
            //        if (solid != null && solid.Faces.Size > 0)
            //        {
            //            // 检查实体是否与平面相交
            //            if (IsSolidIntersectPlane(solid, refPlane))
            //            {
            //                // 进一步检查是否有面的法向量与参照平面正交
            //                foreach (Face face in solid.Faces)
            //                {
            //                    XYZ faceNormal = face.ComputeNormal(UV.Zero);
            //                    if (faceNormal != null)
            //                    {
            //                        double dotProduct = Math.Abs(faceNormal.DotProduct(planeNormal));
            //                        if (dotProduct < 1e-6) // 正交检查
            //                        {
            //                            isIntersectingAndOrthogonal = true;
            //                            break;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
            //        {
            //            // 检查网格是否与平面相正交
            //            if (IsMeshIntersectPlane(mesh, refPlane))
            //            {
            //                for (int i = 0; i < mesh.NumTriangles; i++)
            //                {
            //                    MeshTriangle triangle = mesh.get_Triangle(i);
            //                    // 正确计算三角形法向量（叉积）
            //                    XYZ v0 = triangle.get_Vertex(0);
            //                    XYZ v1 = triangle.get_Vertex(1);
            //                    XYZ v2 = triangle.get_Vertex(2);
            //                    XYZ edge1 = v1 - v0;
            //                    XYZ edge2 = v2 - v0;
            //                    XYZ triangleNormal = edge1.CrossProduct(edge2).Normalize();
            //                    // 判断三角形是否与平面正交（三角形法向量平行于参考平面）
            //                    // 即三角形法向量与平面法向量垂直（点积接近0）
            //                    double dotProduct = Math.Abs(triangleNormal.DotProduct(planeNormal));
            //                    // dotProduct ≈ 0 表示三角形法向量 ⊥ 平面法向量
            //                    // 即三角形平面 ∥ 参考平面（三角形与参考平面正交/垂直）
            //                    if (dotProduct < 1e-3) // 使用稍大的容差
            //                    {
            //                        isIntersectingAndOrthogonal = true;
            //                        break; // 跳出三角形循环
            //                    }
            //                }
            //                // 关键：如果已找到正交三角形，跳出外层 mesh 循环
            //                if (isIntersectingAndOrthogonal)
            //                    break;
            //            }
            //            ////普通相交简化如下
            //            //if (IsMeshIntersectPlane(mesh, refPlane))
            //            //{
            //            //    isIntersectingAndOrthogonal = true;
            //            //    break; // 假设 mesh 相交即视为正交（根据业务需求）
            //            //}
            //        }
            //        if (isIntersectingAndOrthogonal) break;
            //    }
            //    if (isIntersectingAndOrthogonal)
            //    {
            //        intersectingFloorIds.Add(floor.Id);
            //        // 获取楼板信息用于显示
            //        Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            //        string levelName = levelParam != null ? levelParam.AsValueString() : "未知";
            //        string floorTypeName = floor.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString();
            //        string floorInfo = $"楼板 ID:{floor.Id.IntegerValue}, 类型:{floorTypeName}, 标高:{levelName}";
            //        intersectingFloors.Add(new KeyValuePair<ElementId, string>(floor.Id, floorInfo));
            //    }
            //}
            ////// 输出结果
            //int intersectingCount = intersectingFloors.Count;
            //message = $"共找到 {intersectingCount} 个与参照平面相交且正交的平面元素。";
            ////if (intersectingCount > 0)
            ////{
            ////    uiDoc.Selection.SetElementIds(intersectingFloorIds);
            ////}
            //TaskDialog.Show("tt", message);
            //////// 在找到相交且正交的板之后，创建构造线
            ////if (!(intersectingCount > 0 || intersectingFloorIds.Count > 0)) return Result.Cancelled;
            ////// 开始一个事务来创建构造线
            ////using (Transaction trans = new Transaction(doc, "创建相交构造线"))
            ////{
            ////    trans.Start();
            ////    List<Curve> allIntersectionCurves = new List<Curve>();
            ////    foreach (ElementId elementId in intersectingFloorIds)
            ////    {
            ////        Element element = doc.GetElement(elementId);
            ////        if (element == null) continue;
            ////        // 重新获取该元素的几何信息
            ////        Options geoOptions = new Options();
            ////        geoOptions.ComputeReferences = true;
            ////        geoOptions.DetailLevel = ViewDetailLevel.Fine;
            ////        GeometryElement geoElement = element.get_Geometry(geoOptions);
            ////        if (geoElement == null) continue;
            ////        // 获取该元素与参照平面的所有交线
            ////        List<Curve> intersectionCurves = GetIntersectionCurvesWithPlane(geoElement, refPlane);
            ////        allIntersectionCurves.AddRange(intersectionCurves);
            ////        message += intersectionCurves.Count().ToString();
            ////    }
            ////    //// 创建构造线（使用模型线或详图线）
            ////    if (allIntersectionCurves.Count > 0)
            ////    {
            ////        // 选择创建方式：在平面视图中使用详图线，在3D视图中使用模型线
            ////        bool useDetailLines = (activeView.ViewType == ViewType.FloorPlan ||
            ////                               activeView.ViewType == ViewType.CeilingPlan ||
            ////                               activeView.ViewType == ViewType.Section ||
            ////                               activeView.ViewType == ViewType.Elevation);
            ////        //if (useDetailLines)
            ////        //{
            ////        //    // 在视图中创建详图线（仅在该视图中可见）
            ////        //    foreach (Curve curve in allIntersectionCurves)
            ////        //    {
            ////        //        // 将曲线投影到视图平面（如果需要）
            ////        //        Curve projectedCurve = ProjectCurveToViewPlane(curve, activeView);
            ////        //        if (projectedCurve != null)
            ////        //        {
            ////        //            //TaskDialog.Show("tt", (projectedCurve.Length * 304.8).ToString());
            ////        //            //// 创建详图线
            ////        //            DetailLine detailLine = doc.Create.NewDetailCurve(activeView, projectedCurve) as DetailLine;
            ////        //            if (detailLine != null)
            ////        //            {
            ////        //                // 设置线型样式（可选）
            ////        //                // 注意：需要先获取或创建线型样式
            ////        //                SetLineStyle(detailLine, "Dash");
            ////        //            }
            ////        //        }
            ////        //    }
            ////        //    message += $"\n已创建 {allIntersectionCurves.Count} 条详图线。";
            ////        //}
            ////        //else
            ////        //{
            ////        //// 创建模型线（在所有视图中可见）
            ////        //// 需要选择一个工作平面
            ////        SketchPlane sketchPlane = SketchPlane.Create(doc, refPlane);
            ////        foreach (Curve curve in allIntersectionCurves)
            ////        {
            ////            ModelCurve modelCurve = doc.Create.NewModelCurve(curve, sketchPlane);
            ////            if (modelCurve != null)
            ////            {
            ////                // 设置线型样式（可选）
            ////                SetLineStyle(modelCurve, "Dash");
            ////            }
            ////        }
            ////        message += $"\n已创建 {allIntersectionCurves.Count} 条模型线。";
            ////        //}
            ////    }
            ////    else
            ////    {
            ////        message += "\n未找到有效的交线。";
            ////    }
            ////    trans.Commit();
            ////}
            ////TaskDialog.Show("执行结果", message);

            //////1002 拆分楼板，读取出所有轮廓并分别保存多个楼板。注意存在逻辑问题，未处理环嵌套的问题，无法维持板内部开洞
            //// 1. 提示用户选择一个楼板
            //Reference selectedRef;
            //try
            //{
            //    selectedRef = uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "请选择一个包含多个轮廓的楼板");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //Floor originalFloor = doc.GetElement(selectedRef) as Floor;
            //if (originalFloor == null)
            //{
            //    message = "选择的不是一个有效的楼板。";
            //    return Result.Failed;
            //}
            //// 2. 通过几何体获取楼板的轮廓
            //List<CurveArray> profileLoops = GetFloorLoopsFromGeometry(originalFloor);
            //if (profileLoops == null || profileLoops.Count == 0)
            //{
            //    message = "无法从楼板的几何体中提取轮廓。";
            //    return Result.Failed;
            //}
            //// 3. 检查轮廓数量
            //if (profileLoops.Count <= 1)
            //{
            //    TaskDialog.Show("提示", "所选楼板只包含一个轮廓，无需拆分。");
            //    return Result.Succeeded;
            //}
            //using (TransactionGroup tg = new TransactionGroup(doc, "拆分楼板"))
            //{
            //    tg.Start();
            //    try
            //    {
            //        ElementId floorTypeId = originalFloor.GetTypeId();
            //        ElementId levelId = originalFloor.LevelId;
            //        bool isStructural = originalFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() == 1;
            //        Level level = doc.GetElement(levelId) as Level;
            //        FloorType floorType = doc.GetElement(floorTypeId) as FloorType;
            //        foreach (CurveArray curveLoop in profileLoops)
            //        {
            //            using (Transaction tx = new Transaction(doc, "创建单个楼板"))
            //            {
            //                tx.Start();
            //                doc.Create.NewFloor(curveLoop, floorType, level, isStructural);
            //                tx.Commit();
            //            }
            //        }
            //        using (Transaction tx = new Transaction(doc, "删除原始楼板"))
            //        {
            //            tx.Start();
            //            doc.Delete(originalFloor.Id);
            //            tx.Commit();
            //        }
            //        tg.Assimilate();
            //        TaskDialog.Show("成功", $"已成功将原始楼板拆分为 {profileLoops.Count} 个独立的楼板。");
            //        return Result.Succeeded;
            //    }
            //    catch (System.Exception ex)
            //    {
            //        message = "在拆分楼板时发生错误: " + ex.Message;
            //        tg.RollBack();
            //        return Result.Failed;
            //    }
            //}
            //////0404 升级柱切板和梁，梁切板。使用 BuiltInCategory 枚举，而不是魔术数字
            //var structuralColumns = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralColumns)
            //    .WhereElementIsNotElementType().ToElements();
            //var structuralFraming = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralFraming)
            //    .WhereElementIsNotElementType().ToElements();
            //using (Transaction transaction = new Transaction(doc, "自动调整几何连接关系"))
            //{
            //    transaction.Start();
            //    // 1. 柱切割梁和楼板
            //    foreach (Element column in structuralColumns)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, column, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            // 使用类型安全的比较
            //            var categoryId = nearbyElem.Category.Id.IntegerValue;
            //            if (categoryId == (int)BuiltInCategory.OST_StructuralFraming || categoryId == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, column, nearbyElem);
            //            }
            //        }
            //    }
            //    // 2. 梁切割楼板
            //    foreach (Element beam in structuralFraming)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, beam, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            if (nearbyElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, beam, nearbyElem);
            //            }
            //        }
            //    }
            //    transaction.Commit();
            //}

            //////0718 批量旋转、左右镜像 基本完成
            //try
            //{
            //    // 1. 获取当前选中的构件
            //    ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //    // 2. 如果没有选中任何构件，提示用户选择
            //    if (selectedIds == null || selectedIds.Count == 0)
            //    {
            //        selectedIds = uiDoc.Selection.PickObjects(ObjectType.Element, new FamilyInstanceFilterClass(), "选择要处理的构件").Select(f => f.ElementId).ToList();
            //    }
            //    // 3. 过滤出 FamilyInstance 类型的元素
            //    List<FamilyInstance> instancesToMirror = new List<FamilyInstance>();
            //    foreach (ElementId id in selectedIds)
            //    {
            //        Element elem = doc.GetElement(id);
            //        if (elem is FamilyInstance instance && instance.Host == null)
            //        {
            //            // 只处理独立构件，跳过嵌入主体墙的门窗等   
            //            instancesToMirror.Add(instance);
            //        }
            //    }
            //    if (instancesToMirror.Count == 0)
            //    {
            //        TaskDialog.Show("提示", "选中的元素中没有可镜像的族实例。");
            //        return Result.Cancelled;
            //    }
            //    // 选择生成方式
            //    TaskDialog td = new TaskDialog("选择批量操作")
            //    {
            //        MainInstruction = "请选择要对构件进行哪种操作:",
            //        MainIcon = TaskDialogIcon.TaskDialogIconInformation,
            //        CommonButtons = TaskDialogCommonButtons.Cancel,
            //    };
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "绕构件中心顺时针旋转");
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "沿构件X轴方向镜像");
            //    //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "沿构件Y轴方向镜像");
            //    TaskDialogResult tdRes = td.Show();
            //    if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
            //    if (tdRes == TaskDialogResult.CommandLink1)
            //    {
            //        // 1. 弹出滑动窗口获取旋转角度
            //        UniversalSliderWindow sliderWindow = new UniversalSliderWindow("请滑动或输入旋转角度值", 360, "0");
            //        var result = sliderWindow.ShowDialog();
            //        if (result == false) return Result.Cancelled;
            //        double rotateDegree = sliderWindow.ViewModel.NewNum;
            //        // 2. 将角度转换为弧度（Revit 内部使用弧度）
            //        double rotateRadian = rotateDegree * (Math.PI / 180.0);
            //        // 3. 过滤出可旋转的构件（排除嵌入墙体的门窗等）
            //        List<FamilyInstance> instancesToRotate = new List<FamilyInstance>();
            //        foreach (ElementId id in selectedIds)
            //        {
            //            Element elem = doc.GetElement(id);
            //            if (elem is FamilyInstance instance)
            //            {
            //                // 跳过有主体的嵌入构件（如门窗）
            //                if (instance.Host == null)
            //                {
            //                    instancesToRotate.Add(instance);
            //                }
            //            }
            //        }
            //        if (instancesToRotate.Count == 0)
            //        {
            //            TaskDialog.Show("提示", "选中的元素中没有可旋转的独立构件。");
            //            return Result.Failed;
            //        }
            //        // 4. 开始批量处理
            //        using (Transaction trans = new Transaction(doc, "批量旋转构件"))
            //        {
            //            trans.Start();
            //            int successCount = 0;
            //            foreach (FamilyInstance instance in instancesToRotate)
            //            {
            //                try
            //                {
            //                    // 获取构件的中心点（旋转中心）
            //                    LocationPoint locationPoint = instance.Location as LocationPoint;
            //                    if (locationPoint == null) continue;
            //                    XYZ centerPoint = locationPoint.Point;
            //                    // 获取构件的局部坐标系
            //                    Transform transform = instance.GetTotalTransform();
            //                    // 获取局部 Z 轴方向（垂直轴，即旋转轴）
            //                    XYZ zAxis = transform.BasisZ;
            //                    // 创建旋转轴（通过中心点沿 Z 轴方向）
            //                    Line rotationAxis = Line.CreateBound(
            //                        centerPoint - zAxis * 10,  // 起点（沿 Z 轴负方向延伸）
            //                        centerPoint + zAxis * 10   // 终点（沿 Z 轴正方向延伸）
            //                    );
            //                    // 执行旋转
            //                    ElementTransformUtils.RotateElement(doc, instance.Id, rotationAxis, rotateRadian);
            //                    successCount++;
            //                }
            //                catch (Exception ex)
            //                {
            //                    // 单个构件失败不影响整体
            //                    TaskDialog.Show("警告", $"构件 {instance.Name} 旋转失败：{ex.Message}\n已跳过。");
            //                    continue;
            //                }
            //            }
            //            trans.Commit();
            //        }
            //        // 5. 提示结果
            //        TaskDialog.Show("成功", $"已成功旋转 {instancesToRotate.Count} 个构件。");
            //        int skippedCount = selectedIds.Count - instancesToRotate.Count;
            //        if (skippedCount > 0)
            //        {
            //            TaskDialog.Show("提示", $"已跳过 {skippedCount} 个存在依赖主体的构件（如门窗）。");
            //        }

            //    }
            //    else if (tdRes == TaskDialogResult.CommandLink2)
            //    {
            //        // 开始批量处理（外层统一事务）
            //        using (Transaction trans = new Transaction(doc, "批量镜像构件X向"))
            //        {
            //            trans.Start();
            //            // 批量镜像构件
            //            List<ElementId> mirrorIds = new List<ElementId>();
            //            foreach (FamilyInstance instance in instancesToMirror)
            //            {
            //                // 获取构件的中心点
            //                LocationPoint locationPoint = instance.Location as LocationPoint;
            //                if (locationPoint == null) continue;
            //                XYZ centerPoint = locationPoint.Point;
            //                // 获取构件的局部坐标系
            //                Transform transform = instance.GetTotalTransform();
            //                // 获取局部 X 轴方向（左右方向）
            //                XYZ xAxis = transform.BasisX;
            //                // 创建镜像平面（通过中心点和局部 X 轴方向）
            //                Plane mirrorPlane = Plane.CreateByNormalAndOrigin(xAxis, centerPoint);
            //                // 执行镜像// false = 复制并镜像，true = 仅移动（不复制）
            //                ElementTransformUtils.MirrorElements(doc, new List<ElementId> { instance.Id }, mirrorPlane, false);
            //            }
            //            trans.Commit();
            //        }
            //        TaskDialog.Show("成功", $"已成功镜像 {instancesToMirror.Count} 个构件。");
            //        int skippedCount = selectedIds.Count - instancesToMirror.Count;
            //        if (skippedCount > 0)
            //        {
            //            TaskDialog.Show("提示", $"已跳过 {skippedCount} 个存在依赖主体的构件（如门窗）。");
            //        }
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户取消操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", $"操作失败：{ex.Message}");
            //    return Result.Failed;
            //}
            //////例程结束
            return Result.Succeeded;
        }
        //private int _maximum;
        //public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
        //Action<string> onSelected = selectedName => { TaskDialog.Show("tt", selectedName); }; 是一个没有闭包的回调。如果在这个 Lambda 中引用了外部变量（如 doc、selectedIds、计数器等），就产生了带闭包的回调。
        //0323 进度条调用模板，无需单独声明ProgressBar
        //    TransactionWithProgressBarHelper.Execute(doc, "提取构件信息", (service) =>
        //    {
        //        service.UpdateMax(sortedIds.Count());
        //        int index = 0;
        //        foreach (var id in sortedIds)
        //        {
        //            service.Update(++index, id.Value.ToString());
        //        }
        //    });
        //    
    }

}
