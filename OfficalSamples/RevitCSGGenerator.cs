using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using View = Autodesk.Revit.DB.View;


namespace CreatePipe.OfficalSamples
{
    internal class RevitCSGGenerator
    {
        // 常量定义 - 使用readonly提升安全性
        private static readonly string CSG_VIEW_NAME = "CSGTree";
        public RevitCSGGenerator(ExternalCommandData commandData)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;

                // 使用using语句确保事务正确释放（C# 7.3支持）
                using (var tran = new Transaction(doc, "CSG树几何体创建"))
                {
                    tran.Start();

                    // 获取单例实例 - 使用属性初始化器简化
                    var geometryCreation = GeometryCreation.Instance(commandData.Application.Application);
                    var avf = AnalysisVisualizationFramework.Instance(doc);

                    // 构建CSG树并获取最终实体
                    var finalSolid = BuildCSGTree(geometryCreation);

                    // 在视图中显示结果
                    avf.PaintSolid(finalSolid, CSG_VIEW_NAME);

                    tran.Commit();
                }

                // 切换到显示结果的视图（使用null条件运算符）
                var csgView = new FilteredElementCollector(commandData.Application.ActiveUIDocument.Document)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .FirstOrDefault(v => v.Name == CSG_VIEW_NAME);

                if (csgView != null)
                {
                    commandData.Application.ActiveUIDocument.ActiveView = csgView;
                }
            }
            //catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            //{
            //    // 使用when过滤器进行异常分类
            //    //message = $"几何操作异常：{ex.Message}";
            //    return  ;
            //}
            catch (Exception ex)
            {
                //message = $"未知错误：{ex.Message}";
                return;
            }
        }

        /// <summary>
        /// 构建CSG树 - 使用元组和表达式体方法简化
        /// </summary>
        private Solid BuildCSGTree(GeometryCreation geometryCreation)
        {
            // 使用元组解构一次性获取所有几何体
            var (box, sphere, cylinderX, cylinderY, cylinderZ) = PrepareSolids(geometryCreation);

            // 步骤1: 立方体与球体求交
            var intersectResult = BooleanOperations.Intersect(box, sphere);

            // 步骤2: X轴和Y轴圆柱求并集（使用表达式体方法）
            var unionResult = BooleanOperations.Union(cylinderX, cylinderY);

            // 步骤3: 与Z轴圆柱求并集（就地修改）
            unionResult = BooleanOperations.Union(unionResult, cylinderZ);

            // 步骤4: 差集运算得到最终结果
            return BooleanOperations.Difference(intersectResult, unionResult);
        }

        /// <summary>
        /// 准备5个基本几何体 - 使用元组作为返回值
        /// </summary>
        private (Solid box, Solid sphere, Solid cylinderX, Solid cylinderY, Solid cylinderZ)
            PrepareSolids(GeometryCreation geometryCreation)
        {
            var origin = XYZ.Zero;

            // 使用C# 7.0的元组语法一次性创建多个几何体
            return (
                box: geometryCreation.CreateCenterBasedBox(origin, 25),
                sphere: geometryCreation.CreateCenterBasedSphere(origin, 20),
                cylinderX: geometryCreation.CreateCenterBasedCylinder(origin, 5, 40,
                    GeometryCreation.CylinderDirection.BasisX),
                cylinderY: geometryCreation.CreateCenterBasedCylinder(origin, 5, 40,
                    GeometryCreation.CylinderDirection.BasisY),
                cylinderZ: geometryCreation.CreateCenterBasedCylinder(origin, 5, 40,
                    GeometryCreation.CylinderDirection.BasisZ)
            );
        }
    }
    /// <summary>
    /// 布尔操作工具类 - 封装Revit的布尔运算API
    /// </summary>
    public static class BooleanOperations
    {
        /// <summary>
        /// 交集运算 - 返回新实体
        /// </summary>
        public static Solid Intersect(Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);

        /// <summary>
        /// 并集运算 - 返回新实体
        /// </summary>
        public static Solid Union(Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Union);

        /// <summary>
        /// 差集运算 - 返回新实体
        /// </summary>
        public static Solid Difference(Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Difference);

        /// <summary>
        /// 交集运算 - 修改原始实体（就地操作）
        /// </summary>
        public static void IntersectModifying(ref Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid1, solid2, BooleanOperationsType.Intersect);

        /// <summary>
        /// 并集运算 - 修改原始实体（就地操作）
        /// </summary>
        public static void UnionModifying(ref Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid1, solid2, BooleanOperationsType.Union);

        /// <summary>
        /// 差集运算 - 修改原始实体（就地操作）
        /// </summary>
        public static void DifferenceModifying(ref Solid solid1, Solid solid2) =>
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(solid1, solid2, BooleanOperationsType.Difference);

        /// <summary>
        /// 安全的布尔运算（带null检查）
        /// </summary>
        public static Solid SafeIntersect(Solid solid1, Solid solid2) =>
            solid1 == null || solid2 == null
                ? throw new ArgumentNullException("实体不能为null")
                : Intersect(solid1, solid2);

        /// <summary>
        /// 批量并集运算 - 使用params数组参数
        /// </summary>
        public static Solid UnionAll(params Solid[] solids)
        {
            if (solids == null || solids.Length == 0)
                throw new ArgumentException("至少需要一个实体");

            var result = solids[0];
            for (int i = 1; i < solids.Length; i++)
            {
                result = Union(result, solids[i]);
            }
            return result;
        }
    }
    /// <summary>
    /// 几何体创建工厂 - 单例模式，负责创建各种基本几何体
    /// </summary>
    public class GeometryCreation
    {
        // 使用只读自动属性（C# 7.3支持）
        private static GeometryCreation _instance;
        private readonly Autodesk.Revit.ApplicationServices.Application _app;

        /// <summary>
        /// 圆柱方向枚举
        /// </summary>
        public enum CylinderDirection
        {
            BasisX,
            BasisY,
            BasisZ
        }

        // 私有构造函数 - 防止外部实例化
        private GeometryCreation(Autodesk.Revit.ApplicationServices.Application app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        /// <summary>
        /// 获取单例实例（使用属性替代方法）
        /// </summary>
        public static GeometryCreation Instance(Autodesk.Revit.ApplicationServices.Application app) =>
            _instance = new GeometryCreation(app);

        /// <summary>
        /// 创建立方体（中心定位）
        /// </summary>
        /// <param name="center">立方体中心点</param>
        /// <param name="edgeLength">边长</param>
        /// <returns>立方体实体</returns>
        public Solid CreateCenterBasedBox(XYZ center, double edgeLength)
        {
            double halfLength = edgeLength / 2.0;

            // 创建底部矩形轮廓（使用对象初始化器）
            var profileLoop = new CurveLoop();
            var points = new[]
            {
                new XYZ(center.X - halfLength, center.Y - halfLength, center.Z - halfLength),
                new XYZ(center.X - halfLength, center.Y + halfLength, center.Z - halfLength),
                new XYZ(center.X + halfLength, center.Y + halfLength, center.Z - halfLength),
                new XYZ(center.X + halfLength, center.Y - halfLength, center.Z - halfLength),
                new XYZ(center.X - halfLength, center.Y - halfLength, center.Z - halfLength) // 闭合
            };

            // 使用LINQ创建边界线段（C# 7.3特性）
            for (int i = 0; i < points.Length - 1; i++)
            {
                profileLoop.Append(Line.CreateBound(points[i], points[i + 1]));
            }

            var profileLoops = new List<CurveLoop> { profileLoop };
            var extrusionDir = XYZ.BasisZ;

            return GeometryCreationUtilities.CreateExtrusionGeometry(profileLoops, extrusionDir, edgeLength);
        }

        /// <summary>
        /// 创建球体（中心定位）
        /// </summary>
        public Solid CreateCenterBasedSphere(XYZ center, double radius)
        {
            // 创建旋转坐标系
            var frame = new Frame(center, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            // 创建半椭圆轮廓作为旋转剖面
            var profileLoop = new CurveLoop();
            var semiEllipse = Ellipse.CreateCurve(
                center, radius, radius,
                XYZ.BasisX, XYZ.BasisZ,
                -Math.PI / 2.0, Math.PI / 2.0);

            profileLoop.Append(semiEllipse);
            profileLoop.Append(Line.CreateBound(
                new XYZ(center.X, center.Y, center.Z + radius),
                new XYZ(center.X, center.Y, center.Z - radius)));

            var profileLoops = new List<CurveLoop> { profileLoop };

            // 绕Y轴旋转360度生成球体
            return GeometryCreationUtilities.CreateRevolvedGeometry(frame, profileLoops, -Math.PI, Math.PI);
        }

        /// <summary>
        /// 创建圆柱体（中心定位，轴向可选）
        /// </summary>
        public Solid CreateCenterBasedCylinder(XYZ center, double radius, double height, CylinderDirection direction)
        {
            double halfHeight = height / 2.0;

            XYZ bottomCenter;
            switch (direction)
            {
                case CylinderDirection.BasisX:
                    bottomCenter = new XYZ(center.X - halfHeight, center.Y, center.Z);
                    break;
                case CylinderDirection.BasisY:
                    bottomCenter = new XYZ(center.X, center.Y - halfHeight, center.Z);
                    break;
                case CylinderDirection.BasisZ:
                    bottomCenter = new XYZ(center.X, center.Y, center.Z - halfHeight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }

            XYZ topCenter;
            switch (direction)
            {
                case CylinderDirection.BasisX:
                    topCenter = new XYZ(center.X + halfHeight, center.Y, center.Z);
                    break;
                case CylinderDirection.BasisY:
                    topCenter = new XYZ(center.X, center.Y + halfHeight, center.Z);
                    break;
                case CylinderDirection.BasisZ:
                    topCenter = new XYZ(center.X, center.Y, center.Z + halfHeight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }

            // 创建扫掠路径
            var sweepPath = new CurveLoop();
            sweepPath.Append(Line.CreateBound(bottomCenter, topCenter));

            // 确定圆的基向量
            var (majorAxis, minorAxis) = GetCircleAxes(direction);

            // 创建圆形轮廓（由两个半椭圆组成）
            var profileLoop = new CurveLoop();
            var semiEllipse1 = Ellipse.CreateCurve(bottomCenter, radius, radius, majorAxis, minorAxis, -Math.PI, 0);
            var semiEllipse2 = Ellipse.CreateCurve(bottomCenter, radius, radius, majorAxis, minorAxis, 0, Math.PI);

            profileLoop.Append(semiEllipse1);
            profileLoop.Append(semiEllipse2);

            var profileLoops = new List<CurveLoop> { profileLoop };

            return GeometryCreationUtilities.CreateSweptGeometry(sweepPath, 0, 0, profileLoops);
        }

        /// <summary>
        /// 根据圆柱方向获取圆的基向量 - 使用元组作为返回值
        /// </summary>
        private (XYZ majorAxis, XYZ minorAxis) GetCircleAxes(CylinderDirection direction)
        {
            XYZ majorAxis;
            XYZ minorAxis;

            switch (direction)
            {
                case CylinderDirection.BasisX:
                    majorAxis = XYZ.BasisY;
                    minorAxis = XYZ.BasisZ;
                    break;
                case CylinderDirection.BasisY:
                    majorAxis = XYZ.BasisX;
                    minorAxis = XYZ.BasisZ;
                    break;
                case CylinderDirection.BasisZ:
                    majorAxis = XYZ.BasisX;
                    minorAxis = XYZ.BasisY;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }

            return (majorAxis, minorAxis);
        }
    }
    /// <summary>
    /// 分析可视化框架 - 用于在Revit中显示几何体
    /// </summary>
    public class AnalysisVisualizationFramework
    {
        private static AnalysisVisualizationFramework _instance;
        private readonly Document _doc;
        private readonly HashSet<string> _viewNameSet; // 使用HashSet提升查找性能
        private int _schemaId = -1;

        // 使用表达式体属性
        private static string DisplayStyleName(string viewName) => $"Real_Color_Surface_{viewName}";
        private static string ResultSchemaName(string viewName) => $"PaintedSolid_{viewName}";

        private AnalysisVisualizationFramework(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _viewNameSet = new HashSet<string>();
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static AnalysisVisualizationFramework Instance(Document doc) =>
            _instance = new AnalysisVisualizationFramework(doc);

        /// <summary>
        /// 在指定视图中着色显示实体
        /// </summary>
        public void PaintSolid(Solid solid, string viewName)
        {
            var view = GetOrCreateView(viewName);
            var sfm = GetOrCreateSpatialFieldManager(view);

            // 注册分析结果模式（如未注册）
            if (_schemaId == -1 || !IsSchemaRegistered(sfm))
            {
                _schemaId = RegisterAnalysisResultSchema(sfm, viewName);
            }

            // 遍历所有面并添加到可视化场
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                AddFaceToSpatialField(sfm, face);
            }
        }

        /// <summary>
        /// 获取或创建3D视图（使用null条件运算符）
        /// </summary>
        private View GetOrCreateView(string viewName)
        {
            // 查找已存在的视图
            var existingView = new FilteredElementCollector(_doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => v.Name == viewName && !v.IsTemplate);

            if (existingView != null)
                return existingView;

            // 创建新视图
            return CreateNewView(viewName);
        }

        /// <summary>
        /// 创建新的3D视图
        /// </summary>
        private View CreateNewView(string viewName)
        {
            // 获取3D视图族类型
            var viewFamilyType = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);

            if (viewFamilyType == null)
                throw new InvalidOperationException("未找到3D视图族类型");

            // 创建等轴测视图
            var view = View3D.CreateIsometric(_doc, viewFamilyType.Id);

            // 设置相机方向
            var eyePosition = new XYZ(1, -1, -1);
            var upDirection = new XYZ(1, 1, 1);
            var forwardDirection = new XYZ(1, 1, -2);

            var orientation = new ViewOrientation3D(eyePosition, upDirection, forwardDirection);
            view.SetOrientation(orientation);
            view.SaveOrientation();
            view.Name = viewName;

            _viewNameSet.Add(viewName);
            return view;
        }

        /// <summary>
        /// 获取或创建空间场管理器
        /// </summary>
        private static SpatialFieldManager GetOrCreateSpatialFieldManager(View view)
        {
            var sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            return sfm ?? SpatialFieldManager.CreateSpatialFieldManager(view, 1);
        }

        /// <summary>
        /// 检查Schema是否已注册
        /// </summary>
        private bool IsSchemaRegistered(SpatialFieldManager sfm) =>
            sfm.GetRegisteredResults().Contains(_schemaId);

        /// <summary>
        /// 注册分析结果模式
        /// </summary>
        private int RegisterAnalysisResultSchema(SpatialFieldManager sfm, string viewName)
        {
            // 创建设置对象（使用对象初始化器）
            var coloredSurfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
            var colorSettings = new AnalysisDisplayColorSettings();
            var legendSettings = new AnalysisDisplayLegendSettings();

            var displayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                _doc,
                DisplayStyleName(viewName),
                coloredSurfaceSettings,
                colorSettings,
                legendSettings);

            var resultSchema = new AnalysisResultSchema(ResultSchemaName(viewName), "CSG树实体显示")
            {
                AnalysisDisplayStyleId = displayStyle.Id
            };

            return sfm.RegisterResult(resultSchema);
        }

        /// <summary>
        /// 将面添加到空间场
        /// </summary>
        private void AddFaceToSpatialField(SpatialFieldManager sfm, Autodesk.Revit.DB.Face face)
        {
            // 添加面作为空间场基元
            var primitiveIndex = sfm.AddSpatialFieldPrimitive(face, Transform.Identity);

            // 计算面上的点值
            ComputeFaceValues(face, out var uvPoints, out var valueList);

            // 更新空间场数据
            var domainPoints = new FieldDomainPointsByUV(uvPoints);
            var fieldValues = new FieldValues(valueList);

            sfm.UpdateSpatialFieldPrimitive(primitiveIndex, domainPoints, fieldValues, _schemaId);
        }

        /// <summary>
        /// 计算面上的采样点值 - 使用out参数简化
        /// </summary>
        private static void ComputeFaceValues(Autodesk.Revit.DB.Face face, out IList<UV> uvPoints, out IList<ValueAtPoint> valueList)
        {
            uvPoints = new List<UV>();
            valueList = new List<ValueAtPoint>();
            var boundingBox = face.GetBoundingBox();

            // 计算采样步长
            var uStep = (boundingBox.Max.U - boundingBox.Min.U) / 10;
            var vStep = (boundingBox.Max.V - boundingBox.Min.V) / 10;

            for (double u = boundingBox.Min.U; u <= boundingBox.Max.U + 0.0001; u += uStep)
            {
                for (double v = boundingBox.Min.V; v <= boundingBox.Max.V + 0.0001; v += vStep)
                {
                    var uvPoint = new UV(u, v);
                    uvPoints.Add(uvPoint);

                    // 计算该点的空间坐标
                    var worldPoint = face.Evaluate(uvPoint);

                    // 计算值（距离原点的距离）
                    var distance = worldPoint.DistanceTo(XYZ.Zero);
                    var values = new List<double> { distance, distance * 2, distance * 3 };

                    valueList.Add(new ValueAtPoint(values));
                }
            }
        }
    }

}
