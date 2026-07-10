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
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
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

        ///// <summary>
        ///// 辅助方法：为CSV字段添加引号（如果需要）
        ///// </summary>
        ///// <param name="field">要处理的文本</param>
        ///// <returns>符合CSV格式的字段</returns>
        //private string EscapeCsvField(string field)
        //{
        //    if (string.IsNullOrEmpty(field))
        //    {
        //        return "";
        //    }

        //    // 如果字段包含逗号、引号或换行符，则用双引号括起来
        //    if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        //    {
        //        // 将字段内的所有双引号替换为两个双引号
        //        return $"\"{field.Replace("\"", "\"\"")}\"";
        //    }
        //    return field;
        //} 

        //初生成矩形柱方法
        ///// <summary>
        ///// 根据给定的尺寸，查找或创建一个新的柱族类型
        ///// </summary>
        ///// <param name="doc">Revit文档</param>
        ///// <param name="targetFamilyName">目标族名称，如 "CADC_柱-混凝土-矩形"</param>
        ///// <param name="b">柱的宽度（英制单位）</param>
        ///// <param name="h">柱的高度（英制单位）</param>
        ///// <param name="transaction">当前活动的事务</param>
        ///// <returns>匹配或新建的FamilySymbol，失败则返回null</returns>
        //private FamilySymbol CreateOrGetColumnSymbol(Document doc, string targetFamilyName, double b, double h, Transaction transaction)
        //{
        //    // 使用LINQ更高效地查找目标族的所有类型
        //    FamilySymbol baseSymbol = new FilteredElementCollector(doc)
        //        .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
        //        .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
        //    if (baseSymbol == null)
        //    {
        //        TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
        //        return null;
        //    }
        //    // 定义一个比较容差，避免浮点数精度问题
        //    double tolerance = 0.001;
        //    // 查找具有相同尺寸的现有类型
        //    // 这是更稳健的方法：直接比较参数值，而不是比较类型名称字符串
        //    Family family = baseSymbol.Family;
        //    foreach (ElementId symbolId in family.GetFamilySymbolIds())
        //    {
        //        FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
        //        if (symbol == null) continue;
        //        Parameter paramB = symbol.LookupParameter("b");
        //        Parameter paramH = symbol.LookupParameter("h");
        //        if (paramB != null && paramH != null &&
        //            Math.Abs(paramB.AsDouble() - b) < tolerance &&
        //            Math.Abs(paramH.AsDouble() - h) < tolerance)
        //        {
        //            return symbol; // 找到完全匹配的类型
        //        }
        //    }
        //    // 如果没有找到，则创建新类型
        //    try
        //    {
        //        // 确保基础类型已激活
        //        if (!baseSymbol.IsActive) baseSymbol.Activate();
        //        // 将尺寸转换为毫米并四舍五入，用于命名
        //        string typeName = $"{Math.Round(b * 304.8)} x {Math.Round(h * 304.8)}mm";
        //        FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
        //        // 设置新类型的尺寸参数，注意：修改操作必须在事务中
        //        Parameter widthParam = newSymbol.LookupParameter("b");
        //        Parameter heightParam = newSymbol.LookupParameter("h");
        //        if (widthParam != null && heightParam != null)
        //        {
        //            widthParam.Set(b);
        //            heightParam.Set(h);
        //            return newSymbol;
        //        }
        //        else
        //        {
        //            TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到参数 'b' 或 'h'。");
        //            // 刚创建的类型是无效的，需要回滚事务
        //            transaction.RollBack();
        //            return null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("创建族类型失败", $"为尺寸 {b * 304.8:F2}x{h * 304.8:F2} 创建新类型时出错: {ex.Message}");
        //        transaction.RollBack();
        //        return null;
        //    }
        //}
        /// <summary>
        /// 根据给定的尺寸，查找或创建一个新的【矩形】柱族类型
        /// </summary>
        private FamilySymbol CreateOrGetRectangularColumnSymbol(Document doc, string targetFamilyName, double b, double h, Transaction transaction)
        {
            FamilySymbol baseSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
            if (baseSymbol == null)
            {
                TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
                return null;
            }
            double tolerance = 0.001; // 容差
            Family family = baseSymbol.Family;
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol == null) continue;
                Parameter paramB = symbol.LookupParameter("b");
                Parameter paramH = symbol.LookupParameter("h");
                // 同时检查 b x h 和 h x b 两种情况，更鲁棒
                if (paramB != null && paramH != null &&
                    ((Math.Abs(paramB.AsDouble() - b) < tolerance && Math.Abs(paramH.AsDouble() - h) < tolerance) ||
                     (Math.Abs(paramB.AsDouble() - h) < tolerance && Math.Abs(paramH.AsDouble() - b) < tolerance)))
                {
                    return symbol;
                }
            }
            try
            {
                if (!baseSymbol.IsActive) baseSymbol.Activate();
                string typeName = $"{Math.Round(b * 304.8)} x {Math.Round(h * 304.8)}";
                FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
                Parameter widthParam = newSymbol.LookupParameter("b");
                Parameter heightParam = newSymbol.LookupParameter("h");
                if (widthParam != null && heightParam != null)
                {
                    widthParam.Set(b);
                    heightParam.Set(h);
                    return newSymbol;
                }
                else
                {
                    TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到参数 'b' 或 'h'。");
                    transaction.RollBack();
                    return null;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("创建族类型失败", $"为尺寸 {b * 304.8:F2}x{h * 304.8:F2} 创建新类型时出错: {ex.Message}");
                transaction.RollBack();
                return null;
            }
        }
        /// <summary>
        /// 根据给定的直径，查找或创建一个新的【圆形】柱族类型
        /// </summary>
        private FamilySymbol CreateOrGetRoundColumnSymbol(Document doc, string targetFamilyName, double diameter, Transaction transaction)
        {
            FamilySymbol baseSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
            if (baseSymbol == null)
            {
                TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
                return null;
            }
            double tolerance = 0.001;
            Family family = baseSymbol.Family;
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol == null) continue;
                // 圆柱直径参数通常也叫 'b' 或 'd'，这里假设是 'b'
                Parameter paramB = symbol.LookupParameter("b");
                if (paramB != null && Math.Abs(paramB.AsDouble() - diameter) < tolerance)
                {
                    return symbol; // 找到匹配的类型
                }
            }
            try
            {
                if (!baseSymbol.IsActive) baseSymbol.Activate();
                string typeName = $"D{Math.Round(diameter * 304.8)}"; // 命名为 D+直径(mm)
                FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
                Parameter diameterParam = newSymbol.LookupParameter("b");
                if (diameterParam != null)
                {
                    diameterParam.Set(diameter);
                    return newSymbol;
                }
                else
                {
                    TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到直径参数 'b'。");
                    transaction.RollBack();
                    return null;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("创建族类型失败", $"为直径 {diameter * 304.8:F2} 创建新类型时出错: {ex.Message}");
                transaction.RollBack();
                return null;
            }
        }
        /// <summary>
        /// 在指定点创建柱子实例
        /// </summary>
        private void CreateColumnInstance(Document doc, XYZ centerPoint, FamilySymbol symbol)
        {
            if (!symbol.IsActive) symbol.Activate();
            // 创建实例，默认标高为当前视图标高，偏移为0
            doc.Create.NewFamilyInstance(centerPoint, symbol, doc.ActiveView.GenLevel, StructuralType.Column);
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            try
            {
                if (doc.ActiveView.ViewType != ViewType.FloorPlan)
                {
                    TaskDialog.Show("错误", $"请在平面视图执行本操作。");
                    return Result.Cancelled;
                }
                using (Transaction ts = new Transaction(doc, "从CAD生成柱子"))
                {
                    ts.Start();
                    Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement, "请拾取一个代表柱子的CAD图元");
                    Element cadLink = doc.GetElement(r);
                    GeometryObject pickedGeoObj = cadLink.GetGeometryObjectFromReference(r);
                    if (pickedGeoObj == null || pickedGeoObj.GraphicsStyleId == ElementId.InvalidElementId)
                    {
                        TaskDialog.Show("错误", "无法获取有效的图形样式信息。");
                        return Result.Failed;
                    }
                    ElementId graphicsStyleId = pickedGeoObj.GraphicsStyleId;
                    GeometryElement geoElem = cadLink.get_Geometry(new Options());
                    int createdRectCount = 0;
                    int createdRoundCount = 0;
                    // 使用 SelectMany 遍历所有几何实例
                    foreach (var inst in geoElem.OfType<GeometryInstance>())
                    {
                        Transform transform = inst.Transform;
                        GeometryElement instGeo = inst.GetInstanceGeometry();
                        // --- 1. 处理闭合多段线（生成矩形柱） ---
                        var polyLinesOnLayer = instGeo.OfType<PolyLine>()
                            .Where(pl => pl.GraphicsStyleId == graphicsStyleId);
                        foreach (var polyLine in polyLinesOnLayer)
                        {
                            IList<XYZ> points = polyLine.GetCoordinates();
                            // 正确的闭合判断
                            bool isClosed = points.Count > 2 && points[0].IsAlmostEqualTo(points[points.Count - 1]);
                            if (!isClosed) continue;
                            Outline outline = polyLine.GetOutline();
                            double b = Math.Abs(outline.MaximumPoint.X - outline.MinimumPoint.X);
                            double h = Math.Abs(outline.MaximumPoint.Y - outline.MinimumPoint.Y);
                            XYZ centerInCad = (outline.MaximumPoint + outline.MinimumPoint) / 2.0;
                            XYZ centerInRevit = transform.OfPoint(centerInCad);
                            FamilySymbol columnSymbol = CreateOrGetRectangularColumnSymbol(doc, "CADC_结构_混凝土矩形柱", b, h, ts);
                            if (columnSymbol != null)
                            {
                                CreateColumnInstance(doc, centerInRevit, columnSymbol);
                                createdRectCount++;
                            }
                            else return Result.Failed;
                        }
                        // --- 2. 处理圆（生成圆柱） ---
                        var arcsOnLayer = instGeo.OfType<Arc>().Where(a => a.GraphicsStyleId == graphicsStyleId);
                        foreach (var arc in arcsOnLayer)
                        {
                            // 判断是否为一个完整的圆
                            // 一个完整的圆，其起点和终点在参数上相差2*PI，或者它是一个无边界的圆弧
                            bool isFullCircle = !arc.IsBound || arc.GetEndPoint(0).IsAlmostEqualTo(arc.GetEndPoint(1));
                            if (!isFullCircle) continue;
                            double diameter = arc.Radius * 2;
                            XYZ centerInCad = arc.Center;
                            XYZ centerInRevit = transform.OfPoint(centerInCad);
                            FamilySymbol columnSymbol = CreateOrGetRoundColumnSymbol(doc, "CADC_结构_混凝土圆形柱", diameter, ts);
                            if (columnSymbol != null)
                            {
                                CreateColumnInstance(doc, centerInRevit, columnSymbol);
                                createdRoundCount++;
                            }
                            else return Result.Failed;
                        }
                    }
                    if (createdRectCount == 0 && createdRoundCount == 0)
                    {
                        ts.RollBack(); // 没创建任何东西，回滚事务
                        TaskDialog.Show("提示", "在所选图层上未找到任何可识别的闭合多段线或圆。");
                        return Result.Cancelled;
                    }
                    ts.Commit();
                    TaskDialog.Show("成功", $"操作完成！\n创建了 {createdRectCount} 个矩形柱。\n创建了 {createdRoundCount} 个圆形柱。");
                    return Result.Succeeded;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                // 确保在发生异常时回滚事务（如果它还未提交或回滚）
                // using语句会自动处理，但明确一下逻辑
                TaskDialog.Show("致命错误", $"执行过程中发生错误: {ex.Message}");
                return Result.Failed;
            }
            ////CADC_结构_混凝土矩形柱
            ////CADC_结构_混凝土圆形柱
            //try
            //{
            //    // 使用using语句确保事务被正确处理
            //    using (Transaction ts = new Transaction(doc, "从CAD生成柱子"))
            //    {
            //        ts.Start();
            //        // 1. 拾取CAD链接中的一个对象
            //        Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement, "请拾取一个代表柱子的CAD图元");
            //        Element cadLink = doc.GetElement(r);
            //        GeometryObject pickedGeoObj = cadLink.GetGeometryObjectFromReference(r);
            //        if (pickedGeoObj == null || pickedGeoObj.GraphicsStyleId == ElementId.InvalidElementId)
            //        {
            //            TaskDialog.Show("错误", "无法获取有效的图形样式信息。请确保拾取的是CAD图元。");
            //            return Result.Failed;
            //        }
            //        // 2. 获取图层信息 (通过GraphicsStyleId)
            //        ElementId graphicsStyleId = pickedGeoObj.GraphicsStyleId;
            //        // 3. 遍历CAD几何体，找到所有在同一图层上的闭合多段线
            //        GeometryElement geoElem = cadLink.get_Geometry(new Options());
            //        // 使用LINQ简化几何遍历
            //        var polyLinesOnLayer = geoElem
            //            .OfType<GeometryInstance>() // 进入CAD链接的实例几何
            //            .SelectMany(inst => inst.GetInstanceGeometry().OfType<PolyLine>() // 获取实例内的多段线
            //                        .Select(pl => new { PolyLine = pl, Transform = inst.Transform })) // 同时获取变换矩阵
            //            .Where(item => item.PolyLine.GraphicsStyleId == graphicsStyleId); 
            //        if (!polyLinesOnLayer.Any())
            //        {
            //            TaskDialog.Show("提示", "在所选图层上未找到任何闭合的多段线。");
            //            return Result.Cancelled;
            //        }
            //        // 4. 为每个多段线创建柱子
            //        foreach (var item in polyLinesOnLayer)
            //        {
            //            PolyLine polyLine = item.PolyLine;
            //            Transform transform = item.Transform;
            //            // 计算包围盒和尺寸
            //            Outline outline = polyLine.GetOutline();
            //            double b = Math.Abs(outline.MaximumPoint.X - outline.MinimumPoint.X);
            //            double h = Math.Abs(outline.MaximumPoint.Y - outline.MinimumPoint.Y);
            //            // 计算中心点并应用变换
            //            XYZ centerInCad = (outline.MaximumPoint + outline.MinimumPoint) / 2.0;
            //            XYZ centerInRevit = transform.OfPoint(centerInCad);
            //            // 获取或创建族类型 (传递主事务)
            //            FamilySymbol columnSymbol = CreateOrGetColumnSymbol(doc, "CADC_结构_混凝土矩形柱", b, h, ts);
            //            // 如果成功获取了族类型，则创建实例
            //            if (columnSymbol != null)
            //            {
            //                CreateColumnInstance(doc, centerInRevit, columnSymbol);
            //            }
            //            else
            //            {
            //                // 如果CreateOrGetColumnSymbol内部失败并回滚，这里应该停止
            //                // 由于它内部已经回滚了，我们直接返回失败
            //                return Result.Failed;
            //            }
            //        }
            //        ts.Commit();
            //        return Result.Succeeded;
            //    }
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    TaskDialog.Show("致命错误", $"执行过程中发生错误: {ex.Message}");
            //    return Result.Failed;
            //}
            //////例程结束

            ////0708 统计文档特定族和族类型清单并输出csv
            //// 1. 定义需要收集的MEP类别
            //var categoriesToCollect = new List<BuiltInCategory>
            //{
            //    BuiltInCategory.OST_MechanicalEquipment, // 机械设备
            //    BuiltInCategory.OST_PipeAccessory,       // 管道附件
            //    BuiltInCategory.OST_PipeFitting,         // 管件
            //    BuiltInCategory.OST_DuctTerminal,        // 风口
            //    BuiltInCategory.OST_DuctFitting,         // 风管管件
            //    BuiltInCategory.OST_DuctAccessory        // 风管附件
            //};
            //// 2. 创建多类别过滤器并收集所有【实例】
            //var multiCategoryFilter = new ElementMulticategoryFilter(categoriesToCollect);
            //var collector = new FilteredElementCollector(doc)
            //    .WherePasses(multiCategoryFilter)
            //    .WhereElementIsNotElementType(); // 关键：只收集实例，从而得知哪些类型被使用了
            //// 3. 提取唯一的族和类型信息
            //// 使用字典来存储唯一的族类型，键为 FamilySymbol 的 ElementId
            //var usedTypes = new Dictionary<ElementId, (string Category, string FamilyName, string TypeName)>();
            //foreach (Element instance in collector)
            //{
            //    ElementId typeId = instance.GetTypeId();
            //    // 如果该类型ID已处理过，则跳过，实现去重
            //    if (typeId == ElementId.InvalidElementId || usedTypes.ContainsKey(typeId))
            //    {
            //        continue;
            //    }
            //    // 获取类型(FamilySymbol)和族(Family)
            //    if (doc.GetElement(typeId) is FamilySymbol symbol)
            //    {
            //        string familyName = symbol.Family.Name;
            //        string typeName = symbol.Name;
            //        // 获取本地化的类别名称
            //        string categoryName = instance.Category?.Name ?? "未知类别";
            //        usedTypes.Add(typeId, (categoryName, familyName, typeName));
            //    }
            //}
            //if (usedTypes.Count == 0)
            //{
            //    TaskDialog.Show("信息", "在当前文件中未找到指定类别的任何已使用族实例。");
            //    return Result.Succeeded;
            //}
            //// 4. 生成CSV文件内容
            //StringBuilder csvContent = new StringBuilder();
            //// 添加表头
            //csvContent.AppendLine("类别,族名称,类型名称");
            //// 排序后输出，结果更清晰
            //var sortedList = usedTypes.Values.OrderBy(t => t.Category).ThenBy(t => t.FamilyName).ThenBy(t => t.TypeName);
            //foreach (var (category, familyName, typeName) in sortedList)
            //{
            //    // 处理名称中可能包含逗号的情况
            //    string line = $"{EscapeCsvField(category)},{EscapeCsvField(familyName)},{EscapeCsvField(typeName)}";
            //    csvContent.AppendLine(line);
            //}
            //// 5. 弹出对话框让用户选择保存路径
            //using (System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog())
            //{
            //    saveDialog.Filter = "CSV 文件 (*.csv)|*.csv";
            //    saveDialog.Title = "保存已使用的MEP族清单";
            //    saveDialog.FileName = $"{doc.Title}_MEP_Families_List.csv";
            //    if (saveDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        try
            //        {
            //            // 使用 UTF-8 with BOM 编码写入文件，确保Excel能正确显示中文
            //            File.WriteAllText(saveDialog.FileName, csvContent.ToString(), new UTF8Encoding(true));
            //            TaskDialog.Show("成功", $"已成功导出 {usedTypes.Count} 个族类型到:\n{saveDialog.FileName}");
            //        }
            //        catch (Exception ex)
            //        {
            //            message = $"导出文件失败: {ex.Message}";
            //            return Result.Failed;
            //        }
            //    }
            //}

            ////0511 找轴线交叉点坐标测试
            //// 2. 收集所有轴线 (Grid) 图元
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //IList<Element> grids = collector.OfClass(typeof(Autodesk.Revit.DB.Grid))
            //    .WhereElementIsNotElementType().ToElements();
            //if (grids.Count < 2)
            //{
            //    TaskDialog.Show("提示", "当前文档中轴线数量不足，无法计算交点。");
            //    return Result.Failed;
            //}
            //// 提取所有轴线的二维无限长直线 (过滤掉弧形轴线)
            //List<Line> infiniteFlatLines = new List<Line>();
            //foreach (Autodesk.Revit.DB.Grid grid in grids)
            //{
            //    Curve curve = grid.Curve;
            //    // 目前仅处理直线型轴线，如果是弧形轴线(Arc)需要另外的数学逻辑
            //    if (curve is Line line)
            //    {
            //        // 提取起点和方向，强制 Z 坐标为 0，拍平到同一个二维平面
            //        XYZ originFlat = new XYZ(line.Origin.X, line.Origin.Y, 0);
            //        XYZ directionFlat = new XYZ(line.Direction.X, line.Direction.Y, 0).Normalize();
            //        // 创建无界(无限长)的直线
            //        Line unboundLine = Line.CreateUnbound(originFlat, directionFlat);
            //        infiniteFlatLines.Add(unboundLine);
            //    }
            //}
            //// 4. 存储所有交点 (使用 List 并在存入前去重，防止极近的点重复)
            //List<XYZ> intersectionPoints = new List<XYZ>();
            //// 5. 双重循环计算交点 (两两组合比对，避免自己和自己比，也避免重复比对)
            //for (int i = 0; i < infiniteFlatLines.Count; i++)
            //{
            //    for (int j = i + 1; j < infiniteFlatLines.Count; j++)
            //    {
            //        Line line1 = infiniteFlatLines[i];
            //        Line line2 = infiniteFlatLines[j];
            //        // 检查两条线是否平行 (方向向量的叉乘接近于0)
            //        XYZ crossProduct = line1.Direction.CrossProduct(line2.Direction);
            //        if (crossProduct.GetLength() < 1e-6)
            //        {
            //            continue; // 平行或重合，无唯一交点，跳过
            //        }
            //        IntersectionResultArray intersections;
            //        SetComparisonResult result = line1.Intersect(line2, out intersections);
            //        if (result == SetComparisonResult.Overlap && intersections != null)
            //        {
            //            foreach (IntersectionResult iResult in intersections)
            //            {
            //                XYZ point = iResult.XYZPoint;
            //                // 去重机制：检查是否已经存在非常接近的交点 (容差设为 0.001 英尺)
            //                if (!intersectionPoints.Any(p => p.DistanceTo(point) < 0.001))
            //                {
            //                    intersectionPoints.Add(point);
            //                }
            //            }
            //        }
            //    }
            //}
            ////for (int i = 0; i < intersectionPoints.Count; i++)
            ////{
            ////    XYZ p = intersectionPoints[i];
            ////    if (Math.Round(p.X * 304.8, 4) == 0 && Math.Round(p.Y * 304.8, 4) == 0)
            ////    {
            ////        TaskDialog.Show("tt", "轴网交点与项目基点有交叉");
            ////    }
            ////}
            //////// 6. 输出结果到文件
            //////string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //////string outputPath = Path.Combine(desktopPath, "GridIntersections.txt");
            //////using (StreamWriter writer = new StreamWriter(outputPath))
            //////{
            //////    writer.WriteLine($"轴线交点坐标列表 (共 {intersectionPoints.Count} 个点):");
            //////    writer.WriteLine("=========================================");
            //////    writer.WriteLine("单位转换为mm");
            //////    for (int i = 0; i < intersectionPoints.Count; i++)
            //////    {
            //////        XYZ p = intersectionPoints[i];
            //////        writer.WriteLine($"点 {i + 1}: ({Math.Round(p.X * 304.8, 4)}, {Math.Round(p.Y * 304.8, 4)}, {Math.Round(p.Z * 304.8, 4)})");
            //////    }
            //////}
            //////TaskDialog.Show("完成", $"成功找到 {intersectionPoints.Count} 个轴线交点，已保存至：\n{outputPath}");

            //////0426 生成柱测试改，先基于通用combobox确定柱样式，需要在平面操作自动确定柱上下偏移。再确定圆柱或方柱（暂不考虑旋转角度）
            //////结构族SectionShape参数 symbol参数int值表示




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
            ////0909 取楼梯中心几何点
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            //BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            //if (bbox == null) return Result.Failed;
            //XYZ min = bbox.Min;
            //XYZ max = bbox.Max;
            //XYZ center = (min + max) * 0.5;
            //// 输出中心点（XY）
            //TaskDialog.Show("楼梯中心", $"楼梯 {stair.Id} 的中心点XY坐标: ({center.X}, {center.Y})");
            //例程结束
            //0906 楼梯应与空间结合，单独设置房间应付异型楼梯等非标情况
            //var instances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Stairs).ToElementIds();
            ////以上收集的包含symbol和实例
            //StringBuilder stringBuilder = new StringBuilder();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in instances)
            //{
            //    //只过滤实例,取得实体和symbol
            //    if (Stairs.IsByComponent(doc, item))
            //    {
            //        stringBuilder.AppendLine(item.IntegerValue.ToString());
            //        var component = doc.GetElement(item);
            //        stringBuilder.AppendLine(doc.GetElement(component.GetTypeId()).Name.ToString());
            //        ids.Add(component.Id);
            //    }
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString() + "+" + ids.Count().ToString());
            //////0906 楼梯entity属性梳理 
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            ////var instance = doc.GetElement(new ElementId(2187406)) as Element;
            ////if (instance is Stairs)
            ////{
            ////    var stair = (Stairs)instance;
            ////    //TaskDialog.Show("tt", stair.NumberOfStories.ToString());
            ////    //实际单步高度
            ////    //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
            ////    //实际单步深度,踏面数量
            ////    //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualTreadsNumber).ToString());
            ////    //绝对高度底和顶，要计入项目基点高差
            //var basePoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
            //double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
            //TaskDialog.Show("tt", (stair.BaseElevation * 304.8 - deltaHeight).ToString("F2"));
            //TaskDialog.Show("tt", (stair.TopElevation * 304.8 - deltaHeight).ToString("F2"));
            ////    //楼梯总高差
            ////    //TaskDialog.Show("tt", (stair.Height * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.GetStairsRuns().Count()).ToString());
            ////    //跑数和内部各跑宽度，高度等
            ////    //var runs = stair.GetStairsRuns();
            ////    //StringBuilder stringBuilder = new StringBuilder();
            ////    //foreach (var item in runs)
            ////    //{
            ////    //    StairsRun stairsRun = doc.GetElement(item) as StairsRun;
            ////    //    stringBuilder.AppendLine((stairsRun.ActualRunWidth * 304.8).ToString());
            ////    //}
            ////    //TaskDialog.Show("tt", runs.Count().ToString());
            ////}
            ////例程结束
            //////0906 房间楼梯关系梳理 ，判断楼梯是否有部分在房间内即可，没必要全匹配
            //var room = doc.GetElement(new ElementId(2006502)) as Room;
            //////var room = doc.GetElement(new ElementId(1295107)) as Room;
            //////var room = doc.GetElement(new ElementId(1295122)) as Room;
            ////var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            //////int edges = room.GetBoundarySegments(boundaryOptions).Sum(loop => loop.Count);
            ////IList<IList<BoundarySegment>> boundarySegments = room.GetBoundarySegments(boundaryOptions);
            ////BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            ////XYZ minPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            ////XYZ maxPoint = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            ////foreach (IList<BoundarySegment> boundaryLoop in boundarySegments)
            ////{
            ////    CurveLoop curveLoop = new CurveLoop();
            ////    foreach (BoundarySegment segment in boundaryLoop)
            ////    {
            ////        // 获取曲线的起点和终点
            ////        Curve curve = segment.GetCurve();
            ////        XYZ startPoint = curve.GetEndPoint(0);
            ////        XYZ endPoint = curve.GetEndPoint(1);
            ////        // 更新最小点
            ////        minPoint = new XYZ(
            ////            Math.Min(minPoint.X, Math.Min(startPoint.X, endPoint.X)),
            ////            Math.Min(minPoint.Y, Math.Min(startPoint.Y, endPoint.Y)),
            ////            Math.Min(minPoint.Z, Math.Min(startPoint.Z, endPoint.Z))
            ////        );
            ////        // 更新最大点
            ////        maxPoint = new XYZ(
            ////            Math.Max(maxPoint.X, Math.Max(startPoint.X, endPoint.X)),
            ////            Math.Max(maxPoint.Y, Math.Max(startPoint.Y, endPoint.Y)),
            ////            //Math.Max(maxPoint.Z, Math.Max(startPoint.Z, endPoint.Z))
            ////            double.MaxValue);
            ////    }
            ////}
            ////// 设置边界框的最小点和最大点
            ////boundingBox.Min = minPoint;
            ////boundingBox.Max = maxPoint;
            //////TaskDialog.Show("tt", $"{boundingBox.Max.X.ToString("F2")}+{boundingBox.Max.Y.ToString("F2")}+{boundingBox.Max.Z.ToString("F2")}");
            //////TaskDialog.Show("tt", $"{boundingBox.Min.X.ToString("F2")}+{boundingBox.Min.Y.ToString("F2")}+{boundingBox.Min.Z.ToString("F2")}");
            //例程结束
            ////检查楼梯中心点是否在房间内也可以
            ////var stair = doc.GetElement(new ElementId(1926218)) as Stairs;
            //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
            //if (isStairInRoom)
            //{  TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。"); }
            //else {   TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。"); }
            //例程结束
            //////0804 房间管理器.OK  
            //RoomManagerView roomManager = new RoomManagerView(uiApp);
            //roomManager.Show();

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


            return Result.Succeeded;
        }
        //private int _maximum;
        //public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
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
        //0425 检查相交并画线

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
        ///// <summary>
        ///// 合并连接的曲线
        ///// </summary>
        //private List<Curve> MergeConnectedCurves(List<Curve> curves)
        //{
        //    if (curves.Count <= 1) return curves;
        //    List<Curve> mergedCurves = new List<Curve>();
        //    List<Curve> remaining = new List<Curve>(curves);
        //    while (remaining.Count > 0)
        //    {
        //        Curve current = remaining[0];
        //        remaining.RemoveAt(0);
        //        bool merged = true;
        //        while (merged && remaining.Count > 0)
        //        {
        //            merged = false;
        //            for (int i = 0; i < remaining.Count; i++)
        //            {
        //                if (AreCurvesConnected(current, remaining[i]))
        //                {
        //                    // 合并曲线
        //                    current = MergeTwoCurves(current, remaining[i]);
        //                    remaining.RemoveAt(i);
        //                    merged = true;
        //                    break;
        //                }
        //            }
        //        }
        //        mergedCurves.Add(current);
        //    }
        //    return mergedCurves;
        //}
        ///// <summary>
        ///// 判断两条曲线是否连接
        ///// </summary>
        //private bool AreCurvesConnected(Curve curve1, Curve curve2, double tolerance = 1e-6)
        //{
        //    XYZ end1 = curve1.GetEndPoint(1);
        //    XYZ start2 = curve2.GetEndPoint(0);
        //    return end1.DistanceTo(start2) < tolerance;
        //}
        ///// <summary>
        ///// 合并两条曲线
        ///// </summary>
        //private Curve MergeTwoCurves(Curve curve1, Curve curve2)
        //{
        //    // 简单实现：创建一条新的直线连接两个端点
        //    XYZ start = curve1.GetEndPoint(0);
        //    XYZ end = curve2.GetEndPoint(1);
        //    return Line.CreateBound(start, end);
        //}
        /// <summary>
        /// 将曲线投影到当前视图平面,没使用此方法？？
        /// </summary>
        //private Curve ProjectCurveToViewPlane(Curve curve, View activeView)
        //{
        //    if (curve == null || activeView == null) return curve;
        //    // 获取视图平面（适用于平、立、剖面）
        //    Plane viewPlane = activeView.SketchPlane.GetPlane();
        //    if (viewPlane == null) return curve;
        //    List<XYZ> projectedPoints = new List<XYZ>();
        //    // 投影曲线的关键点
        //    IList<XYZ> points = curve.Tessellate();
        //    foreach (XYZ point in points)
        //    {
        //        XYZ projectedPoint = ProjectPointToPlane(point, viewPlane);
        //        projectedPoints.Add(projectedPoint);
        //    }
        //    if (projectedPoints.Count < 2) return curve;
        //    XYZ start = projectedPoints[0];
        //    XYZ end = projectedPoints[projectedPoints.Count - 1];
        //    // 检查投影后是否有有效长度
        //    double projectedLength = start.DistanceTo(end);
        //    if (projectedLength < 1e-6)
        //    {
        //        // 投影长度为0，曲线与视图平面垂直，返回空（防错）
        //        return null;
        //    }
        //    // 检查所有投影点是否基本重合（更严格的垂直判断）
        //    bool allSame = true;
        //    for (int i = 1; i < projectedPoints.Count; i++)
        //    {
        //        if (!projectedPoints[i].IsAlmostEqualTo(start))
        //        {
        //            allSame = false;
        //            break;
        //        }
        //    }
        //    if (allSame) return curve;
        //    // 根据原始曲线类型创建对应的投影曲线
        //    if (curve is Line) return Line.CreateBound(start, end);
        //    // 弧线投影后可能变为直线或保持弧线简化处理：返回直线段
        //    else if (curve is Arc arc) return Line.CreateBound(start, end);
        //    // 其他复杂曲线，返回首尾投影点连线
        //    else return Line.CreateBound(start, end);
        //}
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

        //找实例共同文字属性列表     
        public Dictionary<string, string> GetCommonStringParameterNames(Document doc)
        {
            // 1. 收集文档中所有的族实例
            var allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
                .Where(e => e.HasPhases()).Cast<FamilyInstance>().ToList();
            if (allInstances.Count == 0) return new Dictionary<string, string>();
            // 分别追踪实例参数和类型参数的交集
            HashSet<string> commonInstanceParams = null;
            HashSet<string> commonSymbolParams = null;
            // 2. 遍历所有实例，分别求交集
            foreach (FamilyInstance instance in allInstances)
            {
                // 收集当前实例的实例参数
                var currentInstanceParams = new HashSet<string>();
                foreach (Parameter param in instance.Parameters)
                {
                    if (param.StorageType == StorageType.String && !param.IsReadOnly)
                    {
                        currentInstanceParams.Add(param.Definition.Name);
                    }
                }
                // 收集当前实例的类型参数
                var currentSymbolParams = new HashSet<string>();
                FamilySymbol symbol = instance.Symbol;
                if (symbol != null)
                {
                    foreach (Parameter param in symbol.Parameters)
                    {
                        if (param.StorageType == StorageType.String && !param.IsReadOnly)
                        {
                            currentSymbolParams.Add(param.Definition.Name);
                        }
                    }
                }
                // 求交集
                if (commonInstanceParams == null)
                {
                    commonInstanceParams = new HashSet<string>(currentInstanceParams);
                }
                else
                {
                    commonInstanceParams.IntersectWith(currentInstanceParams);
                }
                if (commonSymbolParams == null)
                {
                    commonSymbolParams = new HashSet<string>(currentSymbolParams);
                }
                else
                {
                    commonSymbolParams.IntersectWith(currentSymbolParams);
                }
                // 提前退出
                if (commonInstanceParams.Count == 0 && commonSymbolParams.Count == 0)
                    break;
            }
            // 3. 组装字典结果，标记来源
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 标记仅实例共有的参数
            foreach (var name in commonInstanceParams ?? Enumerable.Empty<string>())
            {
                result[name] = "实例";
            }
            // 标记仅类型共有的参数，或两者共有
            foreach (var name in commonSymbolParams ?? Enumerable.Empty<string>())
            {
                if (result.ContainsKey(name))
                {
                    // 实例和类型都有同名参数
                    result[name] = "两者";
                }
                else
                {
                    result[name] = "类型";
                }
            }
            return result;
        }
        ////0428 原TEst10方法

        //应该是可视性分析用的
        ///// <summary>
        ///// 在视图中绘制可见性分析的结果
        ///// </summary>
        //private void DrawVisibilityResults(Document doc, View view, XYZ observerPoint, List<XYZ> visiblePoints)
        //{
        //    // 创建新的图形样式以便区分
        //    GraphicsStyle gs = GetOrCreateGraphicsStyle(doc, "可见性分析线");
        //    if (visiblePoints.Count <= 1) return;
        //    // 找到可见区域的边界点（一个简化的方法是找到凸包）
        //    List<XYZ> boundaryPoints = FindConvexHull(visiblePoints);
        //    // 1. 绘制可见区域在标记牌上的轮廓线 (最大最小范围)
        //    for (int i = 0; i < boundaryPoints.Count; i++)
        //    {
        //        XYZ p1 = boundaryPoints[i];
        //        XYZ p2 = boundaryPoints[(i + 1) % boundaryPoints.Count]; // 连接到下一个点，最后一个点连回第一个
        //        Line line = Line.CreateBound(p1, p2);
        //        doc.Create.NewModelCurve(line, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
        //    }
        //    // 2. 绘制从观察点到可见区域边界的“视锥”
        //    foreach (XYZ boundaryPoint in boundaryPoints)
        //    {
        //        Line coneLine = Line.CreateBound(observerPoint, boundaryPoint);
        //        ModelCurve mc = doc.Create.NewModelCurve(coneLine, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
        //        mc.LineStyle = gs; // 应用自定义图形样式
        //    }
        //}
        ///// <summary>
        ///// 找到一组点的2D凸包 (投影到XY平面)
        ///// 这是一个简化的凸包算法 (Gift wrapping algorithm)
        ///// </summary>
        //public List<XYZ> FindConvexHull(List<XYZ> points)
        //{
        //    if (points.Count <= 2) return points;
        //    List<XYZ> hull = new List<XYZ>();
        //    // 找到最左边的点作为起点
        //    XYZ startPoint = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();
        //    XYZ currentPoint = startPoint;
        //    do
        //    {
        //        hull.Add(currentPoint);
        //        XYZ nextPoint = points[0];
        //        foreach (XYZ p in points)
        //        {
        //            if (nextPoint == currentPoint || IsLeft(currentPoint, nextPoint, p) > 0)
        //            {
        //                nextPoint = p;
        //            }
        //        }
        //        currentPoint = nextPoint;
        //    } while (currentPoint != startPoint);
        //    return hull;
        //}
        //private double IsLeft(XYZ p1, XYZ p2, XYZ p3)
        //{
        //    return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        //}
        ///// <summary>
        ///// 获取或创建用于可视化的图形样式
        ///// </summary>
        //private GraphicsStyle GetOrCreateGraphicsStyle(Document doc, string styleName)
        //{
        //    var cat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
        //    var subCat = cat.SubCategories.get_Item(styleName);
        //    if (subCat == null)
        //    {
        //        subCat = doc.Settings.Categories.NewSubcategory(cat, styleName);
        //        subCat.LineColor = new Color(255, 0, 0); // 红色
        //        subCat.SetLineWeight(5, GraphicsStyleType.Projection);
        //    }
        //    return subCat.GetGraphicsStyle(GraphicsStyleType.Projection);
        //}
        ///// <summary>
        ///// 查找最适合进行射线检测的3D视图
        ///// </summary>
        //private View3D FindBest3DView(Document doc)
        //{
        //    var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
        //    View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
        //    return default3DView ?? collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        //}
        ///// <summary>
        ///// 从楼板的实体几何体中提取其底面的所有轮廓环路。
        ///// </summary>
        ///// <param name="floor">要分析的楼板</param>
        ///// <returns>包含所有轮廓的CurveArray列表</returns>
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
        private static View3D FindBest3DView(Document doc)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            // 优先寻找默认的 {3D} 视图
            View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
            if (default3DView != null)
            {
                return default3DView;
            }
            // 如果找不到，再寻找任何一个非模板的3D视图作为备用
            return collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        }
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
        ///// <summary>
        ///// 执行射线检测并返回最近的碰撞图元ID
        ///// </summary>
        ///// <param name="doc">Revit文档</param>
        ///// <param name="origin">射线起点</param>
        ///// <param name="direction">射线方向</param>
        ///// <param name="view">用于检测的视图（可选）</param>
        ///// <returns>碰撞到的第一个图元的ElementId，如果没有碰撞则返回ElementId.InvalidElementId</returns>
        //public static ElementId RaycastNearest(Document doc, XYZ origin, XYZ direction, double deltaHeight, Autodesk.Revit.DB.View view = null)
        //{
        //    // 规范化方向向量
        //    direction = direction.Normalize();
        //    // 创建ReferenceIntersector
        //    ReferenceIntersector intersector;
        //    if (view != null)
        //    {
        //        intersector = new ReferenceIntersector((View3D)view);
        //    }
        //    else
        //    {
        //        // 使用3D视图设置进行检测
        //        intersector = new ReferenceIntersector(Find3DView(doc) ?? throw new System.Exception("找不到可用的3D视图"));
        //    }
        //    // 设置查找最近的交点
        //    intersector.TargetType = FindReferenceTarget.Face;
        //    intersector.FindReferencesInRevitLinks = true;
        //    XYZ originptWithHeight = new XYZ(origin.X, origin.Y, deltaHeight / 304.8);
        //    // 执行射线检测
        //    ReferenceWithContext referenceWithContext = intersector.FindNearest(originptWithHeight, direction);
        //    //ReferenceWithContext referenceWithContext = intersector.FindNearest(origin, direction);
        //    if (referenceWithContext == null) return ElementId.InvalidElementId;
        //    // 获取碰撞图元的ElementId
        //    Reference reference = referenceWithContext.GetReference();
        //    return reference?.ElementId ?? ElementId.InvalidElementId;
        //}
        //private static View3D Find3DView(Document doc)
        //{
        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    collector.OfClass(typeof(View3D));
        //    foreach (View3D view in collector)
        //    {
        //        if (!view.IsTemplate && view.Name != "{3D}") return view;
        //    }
        //    return null;
        //}
        //Action<string> onSelected = selectedName =>
        //{
        //    Autodesk.Revit.UI.TaskDialog.Show("tt", selectedName);
        //};
        //public bool IsBoundingBoxContained(BoundingBoxXYZ container, BoundingBoxXYZ contained)
        //{
        //    // 检查 contained 的最小点是否在 container 内
        //    bool minContained = container.Min.X <= contained.Min.X &&
        //                        container.Min.Y <= contained.Min.Y &&
        //                        container.Min.Z <= contained.Min.Z;

        //    // 检查 contained 的最大点是否在 container 内
        //    bool maxContained = container.Max.X >= contained.Max.X &&
        //                        container.Max.Y >= contained.Max.Y &&
        //                        container.Max.Z >= contained.Max.Z;

        //    return minContained && maxContained;
        //}
        ///// <returns>如果在房间内则返回true，否则返回false</returns>
        //public bool IsAnyPartOfStairInRoom(Stairs stair, Room room, Document doc)
        //{
        //    // 1. 检查所有梯段 (StairsRun)
        //    foreach (ElementId runId in stair.GetStairsRuns())
        //    {
        //        Element runElem = doc.GetElement(runId);
        //        if (IsElementCenterInRoom(runElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"梯段 {runId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个梯段在，就返回true
        //        }
        //    }
        //    // 2. 检查所有平台 (StairsLanding)
        //    foreach (ElementId landingId in stair.GetStairsLandings())
        //    {
        //        Element landingElem = doc.GetElement(landingId);
        //        if (IsElementCenterInRoom(landingElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"平台 {landingId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个平台在，就返回true
        //        }
        //    }
        //    // 如果所有子构件都不在房间内，则认为整个楼梯不在
        //    return false;
        //}
        ///// <summary>
        ///// 辅助方法：检查一个元素的包围盒中心点是否在房间内。物体与房间关系
        ///// </summary>
        //private bool IsElementCenterInRoom(Element elem, Room room)
        //{
        //    if (elem == null || room == null) return false;
        //    BoundingBoxXYZ bbox = elem.get_BoundingBox(null); // 使用全局坐标，不依赖视图
        //    if (bbox == null || !bbox.Enabled) return false;
        //    XYZ centerPoint = (bbox.Min + bbox.Max) / 2.0;
        //    return room.IsPointInRoom(centerPoint);
        //}


        // 设置风口高度
        private void SetTerminalHeight(Element terminal, double heightMm)
        {
            // 将毫米转换为Revit内部单位（英尺）
            double height = heightMm / 304.8;
            // 获取高度参数
            Parameter elevationParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevationParam != null && elevationParam.IsReadOnly == false)
            {
                elevationParam.Set(height);
            }
            else
            {
                // 尝试其他可能的高度参数
                Parameter levelOffsetParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (levelOffsetParam != null && levelOffsetParam.IsReadOnly == false)
                {
                    levelOffsetParam.Set(height);
                }
                else
                {
                    // 尝试通过实例属性设置
                    Parameter offsetParam = terminal.LookupParameter("Offset");
                    if (offsetParam != null && offsetParam.IsReadOnly == false)
                    {
                        offsetParam.Set(height);
                    }
                }
            }
        }
    }

}
