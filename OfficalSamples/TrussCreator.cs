using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class TrussCreator
    {
        private Autodesk.Revit.DB.Document _document;
        private FamilyItemFactory _familyFactory;
        private Autodesk.Revit.Creation.Application _appCreator;
        public TrussCreator(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                // 验证并初始化
                if (!TryInitialize(commandData, ref message, out var document)) return;
                _document = document;
                _appCreator = _document.Application.Create;
                _familyFactory = _document.FamilyCreate;
                var transaction = new Transaction(_document, "创建桁架曲线");
                transaction.Start();
                CreateTruss();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return;
            }
        }
        /// <summary>腹杆角度（度）</summary>
        private const double WEB_ANGLE_DEGREES = 35.0;
        /// <summary>参考平面名称常量</summary>
        private static class ReferencePlaneNames
        {
            public const string Top = "Top";
            public const string Bottom = "Bottom";
            public const string Left = "Left";
            public const string Right = "Right";
            public const string Center = "Center";
        }
        /// <summary>视图名称常量</summary>
        private const string LEVEL1_VIEW_NAME = "Level 1";
        /// <summary>
        /// 验证并初始化命令
        /// 检查是否在桁架族文档中运行
        /// </summary>
        private bool TryInitialize(ExternalCommandData commandData, ref string message, out Autodesk.Revit.DB.Document document)
        {
            document = null;
            var uiDoc = commandData?.Application?.ActiveUIDocument;
            if (uiDoc is null)
            {
                message = "无法获取活动文档";
                return false;
            }
            document = uiDoc.Document;
            var family = document.OwnerFamily;
            // 验证是否为桁架族文档
            if (!document.IsFamilyDocument || family?.FamilyCategory?.Id.IntegerValue != (int)BuiltInCategory.OST_Truss)
            {
                message = "请在桁架族文档中运行此命令";
                return false;
            }
            return true;
        }
        /// <summary>
        /// 创建桁架的主方法
        /// 使用C# 7.3的元组和本地函数
        /// </summary>
        private void CreateTruss()
        {
            // 计算腹杆方向
            var angleRadians = (180 - WEB_ANGLE_DEGREES) * Math.PI / 180.0;
            var angleDirection = new XYZ(Math.Cos(angleRadians), Math.Sin(angleRadians), 0);
            // 查找参考平面和视图
            var (topPlane, bottomPlane, leftPlane, rightPlane, centerPlane, level1View) = FindReferencePlanesAndView();
            // 提取参考平面的几何线
            var (bottomLine, leftLine, rightLine, topLine, centerLine) = ExtractReferencePlaneLines(
                bottomPlane, leftPlane, rightPlane, topPlane, centerPlane);
            // 计算关键交点
            var (bottomLeft, bottomRight, topRight, bottomMidPoint) = CalculateIntersectionPoints(
                bottomLine, leftLine, rightLine, topLine, centerLine);
            var sketchPlane = level1View.SketchPlane;
            // 创建下弦杆
            var bottomChord = CreateBottomChord(bottomLeft, bottomRight, sketchPlane, bottomPlane, level1View);
            // 创建右侧腹杆
            var rightWeb = CreateRightWeb(bottomRight, topRight, sketchPlane, rightPlane, level1View);
            // 创建上弦杆
            var topChord = CreateTopChord(bottomLeft, topRight, sketchPlane,
                leftPlane, bottomPlane, topPlane, rightPlane, level1View);
            // 创建斜腹杆
            var angledWeb = CreateAngledWeb(bottomMidPoint, topChord, angleDirection, angleRadians, sketchPlane, bottomChord, level1View);
            // 创建角部斜腹杆
            var angledWeb2 = CreateCornerAngledWeb(bottomRight, topChord, angleDirection, angleRadians, sketchPlane, bottomChord, level1View);
            // 创建连接腹杆
            CreateBraceWeb(bottomMidPoint, angledWeb, sketchPlane);
        }
        /// <summary>
        /// 查找所需的参考平面和视图
        /// 使用C# 7.3的元组返回多个值
        /// </summary>
        private (ReferencePlane top, ReferencePlane bottom, ReferencePlane left,
                 ReferencePlane right, ReferencePlane center, View level1)
            FindReferencePlanesAndView()
        {
            var collector = new FilteredElementCollector(_document);

            // 使用LINQ获取所有ReferencePlane和View
            var refPlanes = collector.OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
            var views = collector.OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();

            ReferencePlane top = null, bottom = null, left = null, right = null, center = null;
            View level1 = null;

            // 按名称查找
            foreach (var plane in refPlanes)
            {
                switch (plane.Name)
                {
                    case ReferencePlaneNames.Top: top = plane; break;
                    case ReferencePlaneNames.Bottom: bottom = plane; break;
                    case ReferencePlaneNames.Left: left = plane; break;
                    case ReferencePlaneNames.Right: right = plane; break;
                    case ReferencePlaneNames.Center: center = plane; break;
                }
            }

            level1 = views.FirstOrDefault(v => v.Name == LEVEL1_VIEW_NAME);

            // 验证所有必需元素都存在
            if (top is null || bottom is null || left is null ||
                right is null || center is null || level1 is null)
            {
                throw new InvalidOperationException("未找到必需的参考平面或视图，请确保桁架族模板包含：Top/Bottom/Left/Right/Center参考平面和Level1视图");
            }

            return (top, bottom, left, right, center, level1);
        }

        /// <summary>
        /// 提取参考平面的几何线
        /// 使用C# 7.3的元组返回
        /// </summary>
        private (Line bottom, Line left, Line right, Line top, Line center)
            ExtractReferencePlaneLines(ReferencePlane bottomPlane, ReferencePlane leftPlane,
                                       ReferencePlane rightPlane, ReferencePlane topPlane, ReferencePlane centerPlane)
        {
            return (
                GetReferencePlaneLine(bottomPlane),
                GetReferencePlaneLine(leftPlane),
                GetReferencePlaneLine(rightPlane),
                GetReferencePlaneLine(topPlane),
                GetReferencePlaneLine(centerPlane)
            );
        }

        /// <summary>
        /// 计算关键交点
        /// 使用C# 7.3的元组返回
        /// </summary>
        private (XYZ bottomLeft, XYZ bottomRight, XYZ topRight, XYZ bottomMidPoint)
            CalculateIntersectionPoints(Line bottomLine, Line leftLine, Line rightLine, Line topLine, Line centerLine)
        {
            return (
                GetIntersection(bottomLine, leftLine),   // 下弦左端点
                GetIntersection(bottomLine, rightLine),  // 下弦右端点
                GetIntersection(topLine, rightLine),     // 上弦右端点
                GetIntersection(bottomLine, centerLine)  // 下弦中点
            );
        }
        /// <summary>
        /// 创建下弦杆
        /// </summary>
        private ModelCurve CreateBottomChord(XYZ bottomLeft, XYZ bottomRight, SketchPlane sketchPlane,
            ReferencePlane bottomPlane, View level1View)
        {
            var bottomChord = MakeTrussCurve(bottomLeft, bottomRight, sketchPlane, TrussCurveType.BottomChord);

            if (bottomChord != null)
            {
                // 将下弦杆锁定到底部参考平面
                _familyFactory.NewAlignment(level1View, bottomPlane.GetReference(), bottomChord.GeometryCurve.Reference);
            }

            return bottomChord;
        }

        /// <summary>
        /// 创建右侧腹杆
        /// </summary>
        private ModelCurve CreateRightWeb(XYZ bottomRight, XYZ topRight, SketchPlane sketchPlane,
            ReferencePlane rightPlane, View level1View)
        {
            var rightWeb = MakeTrussCurve(bottomRight, topRight, sketchPlane, TrussCurveType.Web);

            if (rightWeb != null)
            {
                // 将右侧腹杆锁定到右侧参考平面
                _familyFactory.NewAlignment(level1View, rightPlane.GetReference(), rightWeb.GeometryCurve.Reference);
            }

            return rightWeb;
        }

        /// <summary>
        /// 创建上弦杆
        /// </summary>
        private ModelCurve CreateTopChord(XYZ bottomLeft, XYZ topRight, SketchPlane sketchPlane,
            ReferencePlane leftPlane, ReferencePlane bottomPlane, ReferencePlane topPlane,
            ReferencePlane rightPlane, View level1View)
        {
            var topChord = MakeTrussCurve(bottomLeft, topRight, sketchPlane, TrussCurveType.TopChord);

            if (topChord != null)
            {
                var geometryCurve = topChord.GeometryCurve;

                // 锁定上弦杆起点到左参考平面和底参考平面的交点
                _familyFactory.NewAlignment(level1View, geometryCurve.GetEndPointReference(0), leftPlane.GetReference());
                _familyFactory.NewAlignment(level1View, geometryCurve.GetEndPointReference(0), bottomPlane.GetReference());

                // 锁定上弦杆终点到顶参考平面和右参考平面的交点
                _familyFactory.NewAlignment(level1View, geometryCurve.GetEndPointReference(1), topPlane.GetReference());
                _familyFactory.NewAlignment(level1View, geometryCurve.GetEndPointReference(1), rightPlane.GetReference());
            }

            return topChord;
        }

        /// <summary>
        /// 创建斜腹杆（从中点出发）
        /// </summary>
        private ModelCurve CreateAngledWeb(XYZ bottomMidPoint, ModelCurve topChord, XYZ angleDirection,
            double angleRadians, SketchPlane sketchPlane, ModelCurve bottomChord, View level1View)
        {
            // 创建腹杆方向线并计算与上弦杆的交点
            var webDirection = Line.CreateUnbound(bottomMidPoint, angleDirection);
            var endOfWeb = GetIntersection(topChord.GeometryCurve as Line, webDirection);

            var angledWeb = MakeTrussCurve(bottomMidPoint, endOfWeb, sketchPlane, TrussCurveType.Web);

            // 添加角度尺寸约束
            AddAngularDimension(bottomMidPoint, angledWeb, bottomChord, angleRadians, level1View);

            return angledWeb;
        }

        /// <summary>
        /// 创建角部斜腹杆（从右下角出发）
        /// </summary>
        private ModelCurve CreateCornerAngledWeb(XYZ bottomRight, ModelCurve topChord, XYZ angleDirection,
            double angleRadians, SketchPlane sketchPlane, ModelCurve bottomChord, View level1View)
        {
            var webDirection = Line.CreateUnbound(bottomRight, angleDirection);
            var endOfWeb = GetIntersection(topChord.GeometryCurve as Line, webDirection);

            var angledWeb = MakeTrussCurve(bottomRight, endOfWeb, sketchPlane, TrussCurveType.Web);

            // 添加角度尺寸约束
            AddAngularDimension(bottomRight, angledWeb, bottomChord, angleRadians, level1View);

            return angledWeb;
        }

        /// <summary>
        /// 创建连接腹杆
        /// </summary>
        private ModelCurve CreateBraceWeb(XYZ bottomMidPoint, ModelCurve angledWeb, SketchPlane sketchPlane)
        {
            var braceEndPoint = GetIntersection(angledWeb.GeometryCurve as Line,
                Line.CreateUnbound(bottomMidPoint, XYZ.BasisY));

            return MakeTrussCurve(bottomMidPoint, braceEndPoint, sketchPlane, TrussCurveType.Web);
        }

        /// <summary>
        /// 添加角度尺寸约束
        /// </summary>
        private void AddAngularDimension(XYZ centerPoint, ModelCurve angledWeb, ModelCurve bottomChord,
            double angleRadians, View level1View)
        {
            var arc = Arc.Create(centerPoint, angledWeb.GeometryCurve.Length / 2,
                angleRadians, Math.PI, XYZ.BasisX, XYZ.BasisY);

            var dimension = _familyFactory.NewAngularDimension(level1View, arc,
                angledWeb.GeometryCurve.Reference, bottomChord.GeometryCurve.Reference);

            if (dimension != null)
                dimension.IsLocked = true;
        }
        /// <summary>
        /// 创建桁架曲线
        /// </summary>
        private ModelCurve MakeTrussCurve(XYZ start, XYZ end, SketchPlane sketchPlane, TrussCurveType type)
        {
            var line = Line.CreateBound(start, end);
            var trussCurve = _familyFactory.NewModelCurve(line, sketchPlane);
            trussCurve.TrussCurveType = type;
            _document.Regenerate();
            return trussCurve;
        }

        /// <summary>
        /// 获取参考平面的几何线
        /// 将Z坐标重置为0以确保交点计算正确
        /// 使用C# 7.3的表达式体
        /// </summary>
        private Line GetReferencePlaneLine(ReferencePlane plane)
        {
            var origin = new XYZ(plane.BubbleEnd.X, plane.BubbleEnd.Y, 0.0);
            return Line.CreateUnbound(origin, plane.Direction);
        }

        /// <summary>
        /// 计算两条线的交点
        /// 使用C# 7.3的out变量模式匹配
        /// </summary>
        private XYZ GetIntersection(Line line1, Line line2)
        {
            // 使用out变量模式
            if (line1.Intersect(line2, out var results) != SetComparisonResult.Overlap)
            {
                throw new InvalidOperationException("两条线没有相交");
            }

            if (results is null || results.Size != 1)
            {
                throw new InvalidOperationException("无法获取交点");
            }

            return results.get_Item(0).XYZPoint;
        }
    }
    /// <summary>
    /// 几何扩展方法
    /// 使用C# 7.3的表达式体成员
    /// </summary>
    public static partial class GeometryExtensions
    {
        /// <summary>
        /// 安全获取线的端点引用
        /// </summary>
        public static Reference GetEndPointReference(this Curve curve, int endIndex) =>
            curve?.GetEndPointReference(endIndex);

        /// <summary>
        /// 检查两条线是否相交
        /// </summary>
        public static bool Intersects(this Line line1, Line line2, out XYZ intersectionPoint)
        {
            intersectionPoint = null;

            if (line1.Intersect(line2, out var results) == SetComparisonResult.Overlap
                && results?.Size == 1)
            {
                intersectionPoint = results.get_Item(0).XYZPoint;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取参考平面的安全引用
        /// </summary>
        public static Reference GetReferenceSafe(this ReferencePlane plane) =>
            plane?.GetReference();
    }
}
