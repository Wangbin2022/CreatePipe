using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace CreatePipe.OfficalSamples
{
    internal class AirHandlerCreator
    {
        private Autodesk.Revit.DB.Document _document;
        private FamilyItemFactory _familyFactory;
        private Extrusion[] _extrusions;
        private CombinableElementArray _combineElements;
        private Transaction _transaction;

        public AirHandlerCreator(ExternalCommandData commandData)
        {
            string message = string.Empty;
            // 使用本地函数进行初始化验证
            if (!TryInitialize(commandData, ref message, out var document)) return;

            _document = document;
            _familyFactory = _document.FamilyCreate;
            _extrusions = new Extrusion[5];
            _combineElements = new CombinableElementArray();

            // 验证当前族类别是否为机械设备
            if (!IsMechanicalEquipmentFamily())
            {
                message = "请确保在机械设备族模板中打开此命令";
                return;
            }

            var transaction = new Transaction(_document, "创建空气处理单元");
            _transaction = transaction;
            _transaction.Start();

            try
            {
                // 创建所有拉伸体
                CreateAllExtrusions();
                _document.Regenerate();

                // 创建所有连接器
                CreateAllConnectors();
                _document.Regenerate();

                // 合并几何体
                _document.CombineElements(_combineElements);
                _document.Regenerate();

                _transaction.Commit();
            }
            catch (Exception ex)
            {
                _transaction.RollBack();
                message = $"创建失败: {ex.Message}";
                return;
            }
        }
        /// <summary>
        /// 初始化命令
        /// 使用C# 7.3的out变量和模式匹配
        /// </summary>
        private bool TryInitialize(ExternalCommandData commandData, ref string message, out Autodesk.Revit.DB.Document document)
        {
            document = null;
            if (commandData?.Application?.ActiveUIDocument is null)
            {
                message = "无法获取活动文档";
                return false;
            }
            var uiDoc = commandData.Application.ActiveUIDocument;
            document = uiDoc.Document;

            if (document is null || !document.IsFamilyDocument)
            {
                message = "请在族编辑器中运行此命令";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证当前族是否为机械设备类别
        /// 使用C# 7.3的模式匹配和switch表达式
        /// </summary>
        private bool IsMechanicalEquipmentFamily()
        {
            var family = _document.OwnerFamily;
            if (family is null) return false;

            var categoryName = family.FamilyCategory?.Name;
            var mechanicalCategoryName = _document.Settings.Categories
                .get_Item(BuiltInCategory.OST_MechanicalEquipment)?.Name;

            return string.Equals(categoryName, mechanicalCategoryName, StringComparison.OrdinalIgnoreCase);
        }

        #region 拉伸体创建方法

        /// <summary>
        /// 创建所有拉伸体
        /// 使用C# 7.3的元组和解构
        /// </summary>
        private void CreateAllExtrusions()
        {
            var appCreation = _document.Application.Create;

            // 创建矩形拉伸体 (索引0-2)
            for (int i = 0; i <= 2; i++)
            {
                var (curves, profile) = CreateRectangularProfile(appCreation, PROFILE_RECTANGULAR_POINTS[i]);
                var sketchPlane = CreateSketchPlane(SKETCH_PLANE_DATA[i].normal, SKETCH_PLANE_DATA[i].origin);
                var (startOffset, endOffset) = EXTRUSION_OFFSETS[i];

                _extrusions[i] = CreateExtrusion(IS_SOLID[i], profile, sketchPlane, endOffset, startOffset);
                _combineElements.Append(_extrusions[i]);
            }
            // 创建圆形拉伸体 (索引3-4)
            for (int i = 3; i <= 4; i++)
            {
                var (normal, origin, radius) = CIRCULAR_PROFILE_DATA[i - 3];
                var (startOffset, endOffset) = EXTRUSION_OFFSETS[i];

                var profile = CreateCircularProfile(appCreation, normal, origin, radius);
                var sketchPlane = CreateSketchPlane(SKETCH_PLANE_DATA[i].normal, SKETCH_PLANE_DATA[i].origin);

                _extrusions[i] = CreateExtrusion(IS_SOLID[i], profile, sketchPlane, endOffset, startOffset);
                _combineElements.Append(_extrusions[i]);
            }
        }

        /// <summary>
        /// 创建矩形轮廓
        /// 使用C# 7.3的元组返回类型
        /// </summary>
        private (CurveArray curves, CurveArrArray profile) CreateRectangularProfile(
            Autodesk.Revit.Creation.Application appCreation, XYZ[] points)
        {
            var curves = appCreation.NewCurveArray();
            curves.Append(Line.CreateBound(points[0], points[1]));
            curves.Append(Line.CreateBound(points[1], points[2]));
            curves.Append(Line.CreateBound(points[2], points[3]));
            curves.Append(Line.CreateBound(points[3], points[0]));

            var profile = appCreation.NewCurveArrArray();
            profile.Append(curves);

            return (curves, profile);
        }

        /// <summary>
        /// 创建圆形轮廓
        /// </summary>
        private CurveArrArray CreateCircularProfile(Autodesk.Revit.Creation.Application appCreation,
            XYZ normal, XYZ origin, double radius)
        {
            var plane = Plane.CreateByNormalAndOrigin(normal, origin);
            var arc = Arc.Create(plane, radius, 0, Math.PI * 2);

            var curves = appCreation.NewCurveArray();
            curves.Append(arc);

            var profile = appCreation.NewCurveArrArray();
            profile.Append(curves);

            return profile;
        }

        /// <summary>
        /// 创建草图平面
        /// </summary>
        private SketchPlane CreateSketchPlane(XYZ normal, XYZ origin)
        {
            var plane = Plane.CreateByNormalAndOrigin(normal, origin);
            return SketchPlane.Create(_document, plane);
        }

        /// <summary>
        /// 创建拉伸体
        /// </summary>
        private Extrusion CreateExtrusion(bool isSolid, CurveArrArray profile,
            SketchPlane sketchPlane, double endOffset, double startOffset)
        {
            var extrusion = _familyFactory.NewExtrusion(isSolid, profile, sketchPlane, endOffset);
            extrusion.StartOffset = startOffset;
            return extrusion;
        }

        #endregion

        #region 连接器创建方法

        /// <summary>
        /// 创建所有连接器
        /// 使用C# 7.3的本地函数和模式匹配
        /// </summary>
        private void CreateAllConnectors()
        {
            // 定义连接器配置列表
            var connectorConfigs = new[]
            {
                CreateDuctConnectorConfig(1, ConnectorType.DuctSupplyAir,
                    CONNECTOR_DIMENSIONS[0].height, CONNECTOR_DIMENSIONS[0].width,
                    DUCT_CONNECTOR_FLOWS[0].flow, DUCT_CONNECTOR_FLOWS[0].direction,
                    DUCT_CONNECTOR_FLOWS[0].config),

                CreateDuctConnectorConfig(2, ConnectorType.DuctReturnAir,
                    CONNECTOR_DIMENSIONS[1].height, CONNECTOR_DIMENSIONS[1].width,
                    DUCT_CONNECTOR_FLOWS[1].flow, DUCT_CONNECTOR_FLOWS[1].direction,
                    DUCT_CONNECTOR_FLOWS[1].config),

                CreatePipeConnectorConfig(3, ConnectorType.PipeSupplyHydronic,
                    CIRCLE_RADIUS, PIPE_CONNECTOR_FLOWS[0].flow,
                    PIPE_CONNECTOR_FLOWS[0].direction),

                CreatePipeConnectorConfig(4, ConnectorType.PipeReturnHydronic,
                    CIRCLE_RADIUS, PIPE_CONNECTOR_FLOWS[1].flow,
                    PIPE_CONNECTOR_FLOWS[1].direction)
            };
            // 创建每个连接器
            foreach (var config in connectorConfigs)
            {
                CreateConnector(config);
            }
        }

        /// <summary>
        /// 创建风管连接器配置
        /// </summary>
        private ConnectorParameters CreateDuctConnectorConfig(int extrusionIndex, ConnectorType type,
            double height, double width, double flow, int direction, int config) =>
            new ConnectorParameters(extrusionIndex, type, height, width, flow, direction, config);
        /// <summary>
        /// 创建水管连接器配置
        /// </summary>
        private ConnectorParameters CreatePipeConnectorConfig(int extrusionIndex, ConnectorType type,
            double radius, double flow, int direction) =>
            new ConnectorParameters(extrusionIndex, type, radius, flow, direction);

        /// <summary>
        /// 创建单个连接器
        /// 使用C# 7.3的switch表达式处理不同类型
        /// </summary>
        private void CreateConnector(ConnectorParameters config)
        {
            //// 获取目标拉伸体的面
            //var planarFaces = GetPlanarFaces(_extrusions[config.ExtrusionIndex]);
            //if (!planarFaces.Any()) return;
            //var targetFace = planarFaces.First();
            //var faceReference = targetFace.Reference;
            //// 根据连接器类型创建对应的连接器
            //switch (config.Type)
            //{
            //    case ConnectorType.DuctSupplyAir:
            //        CreateDuctConnector(faceReference, DuctSystemType.SupplyAir,
            //            config.Height, config.Width, config.Flow, config.FlowDirection, config.FlowConfiguration);
            //        break;
            //    case ConnectorType.DuctReturnAir:
            //        CreateDuctConnector(faceReference, DuctSystemType.ReturnAir,
            //            config.Height, config.Width, config.Flow, config.FlowDirection, config.FlowConfiguration);
            //        break;
            //    case ConnectorType.PipeSupplyHydronic:
            //        CreatePipeConnector(faceReference, PipeSystemType.SupplyHydronic,
            //            config.Radius, config.Flow, config.FlowDirection);
            //        break;
            //    case ConnectorType.PipeReturnHydronic:
            //        CreatePipeConnector(faceReference, PipeSystemType.ReturnHydronic,
            //            config.Radius, config.Flow, config.FlowDirection);
            //        break;
            //}
        }

        /// <summary>
        /// 创建风管连接器
        /// 使用C# 7.3的本地函数简化参数设置
        /// </summary>
        private void CreateDuctConnector(Reference faceReference, DuctSystemType systemType,
            double height, double width, double flow, int flowDirection, int flowConfig)
        {
            var connector = ConnectorElement.CreateDuctConnector(_document, systemType,
                ConnectorProfileType.Rectangular, faceReference);

            // 使用本地函数设置参数
            void SetParameter(BuiltInParameter param, double value) =>
                connector.get_Parameter(param)?.Set(value);

            void SetIntegerParameter(BuiltInParameter param, int value) =>
                connector.get_Parameter(param)?.Set(value);

            SetParameter(BuiltInParameter.CONNECTOR_HEIGHT, height);
            SetParameter(BuiltInParameter.CONNECTOR_WIDTH, width);
            SetIntegerParameter(BuiltInParameter.RBS_DUCT_FLOW_DIRECTION_PARAM, flowDirection);
            SetIntegerParameter(BuiltInParameter.RBS_DUCT_FLOW_CONFIGURATION_PARAM, flowConfig);
            SetParameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM, flow);
        }

        /// <summary>
        /// 创建水管连接器
        /// </summary>
        private void CreatePipeConnector(Reference faceReference, PipeSystemType systemType,
            double radius, double flow, int flowDirection)
        {
            var connector = ConnectorElement.CreatePipeConnector(_document, systemType, faceReference);

            // 使用本地函数设置参数
            void SetParameter(BuiltInParameter param, double value) =>
                connector.get_Parameter(param)?.Set(value);

            void SetIntegerParameter(BuiltInParameter param, int value) =>
                connector.get_Parameter(param)?.Set(value);

            SetParameter(BuiltInParameter.CONNECTOR_RADIUS, radius);
            SetIntegerParameter(BuiltInParameter.RBS_PIPE_FLOW_DIRECTION_PARAM, flowDirection);

            if (flow > 0)
                SetParameter(BuiltInParameter.RBS_PIPE_FLOW_PARAM, flow);
        }

        #endregion

        #region 几何体面提取方法

        /// <summary>
        /// 获取拉伸体的所有平面
        /// 使用C# 7.3的yield return和模式匹配优化
        /// </summary>
        private List<PlanarFace> GetPlanarFaces(Extrusion extrusion)
        {
            var planarFaces = new List<PlanarFace>();

            if (extrusion is null)
                return planarFaces;

            var geoOptions = _document.Application.Create.NewGeometryOptions();
            geoOptions.View = _document.ActiveView;
            geoOptions.ComputeReferences = true;

            var geoElement = extrusion.get_Geometry(geoOptions);
            if (geoElement is null)
                return planarFaces;

            // 使用C# 7.3的模式匹配和迭代器
            foreach (GeometryObject geoObject in geoElement)
            {
                // 模式匹配：如果是Solid类型
                if (geoObject is Solid solid)
                {
                    foreach (Autodesk.Revit.DB.Face face in solid.Faces)
                    {
                        // 模式匹配：如果是PlanarFace
                        if (face is PlanarFace planarFace)
                        {
                            planarFaces.Add(planarFace);
                        }
                    }
                }
            }

            return planarFaces;
        }

        #endregion


        #region 几何数据常量 - 使用元组和只读数组

        // 矩形拉伸体轮廓点 (索引0-2)
        private static readonly XYZ[][] PROFILE_RECTANGULAR_POINTS = new XYZ[][]
        {
            new XYZ[] { new XYZ(-17.28, -0.53, 0.9), new XYZ(-17.28, 11, 0.9),
                        new XYZ(-0.57, 11, 0.9), new XYZ(-0.57, -0.53, 0.9) },  // 主体底部
            new XYZ[] { new XYZ(-0.57, 7, 6.58), new XYZ(-0.57, 7, 3),
                        new XYZ(-0.57, 3.6, 3), new XYZ(-0.57, 3.6, 6.58) },      // 送风侧
            new XYZ[] { new XYZ(-17.28, -0.073, 7.17), new XYZ(-17.28, 10.76, 7.17),
                        new XYZ(-17.28, 10.76, 3.58), new XYZ(-17.28, -0.073, 3.58) }  // 回风侧
        };

        // 圆形拉伸体数据 (索引3-4)
        private static readonly (XYZ normal, XYZ origin, double radius)[] CIRCULAR_PROFILE_DATA = new (XYZ, XYZ, double)[]
        {
            (new XYZ(0, -1, 0), new XYZ(-9, 0.53, 7.17), 0.17),   // 供水接口
            (new XYZ(0, -1, 0), new XYZ(-8.24, 0.53, 0.67), 0.17)  // 回水接口
        };

        // 草图平面数据 (法线和原点)
        private static readonly (XYZ normal, XYZ origin)[] SKETCH_PLANE_DATA = new (XYZ, XYZ)[]
        {
            (new XYZ(0, 0, 1), new XYZ(0, 0, 0.9)),      // 主体底部平面
            (new XYZ(1, 0, 0), new XYZ(-0.57, 0, 0)),    // 送风侧平面
            (new XYZ(-1, 0, 0), new XYZ(-17.28, 0, 0)),  // 回风侧平面
            (new XYZ(0, -1, 0), new XYZ(0, 0.53, 0)),    // 供水接口平面
            (new XYZ(0, -1, 0), new XYZ(0, 0.53, 0))     // 回水接口平面
        };

        // 拉伸偏移量 (起始偏移, 结束偏移)
        private static readonly (double start, double end)[] EXTRUSION_OFFSETS = new (double, double)[]
        {
            (-0.9, 6.77),   // 主体底部拉伸
            (0, -0.18),     // 送风侧拉伸
            (0, -0.08),     // 回风侧拉伸
            (1, 1.15),      // 供水接口拉伸
            (1, 1.15)       // 回水接口拉伸
        };

        // 是否为实体
        private static readonly bool[] IS_SOLID = { true, false, false, true, true };

        // 连接器尺寸 [高, 宽]
        private static readonly (double height, double width)[] CONNECTOR_DIMENSIONS = new (double, double)[]
        {
            (3.58, 3.4),    // 送风连接器
            (3.59, 10.833)  // 回风连接器
        };

        // 连接器流量配置
        private static readonly (double flow, int direction, int config)[] DUCT_CONNECTOR_FLOWS = new (double, int, int)[]
        {
            (547, 2, 1),    // 送风: 流量547, 流出, 固定流量
            (547, 1, 1)     // 回风: 流量547, 流入, 固定流量
        };

        // 水管连接器流量配置
        private static readonly (double flow, int direction)[] PIPE_CONNECTOR_FLOWS = new (double, int)[]
        {
            (0, 2),    // 供水: 流出
            (0, 1)     // 回水: 流入
        };

        private const double CIRCLE_RADIUS = 0.17;
        private const double CONNECTOR_RADIUS = 0.17;
        private const double DEFAULT_FLOW = 547;

        #endregion
        /// <summary>
        /// 拉伸体创建参数（使用只读结构体，C# 7.3特性）
        /// </summary>
        public readonly struct ExtrusionParameters
        {
            public XYZ[] ProfilePoints { get; }
            public XYZ PlaneNormal { get; }
            public XYZ PlaneOrigin { get; }
            public double StartOffset { get; }
            public double EndOffset { get; }
            public bool IsSolid { get; }
            public bool IsCircular { get; }
            public double CircleRadius { get; }

            public ExtrusionParameters(XYZ[] profilePoints, XYZ planeNormal, XYZ planeOrigin,
                double startOffset, double endOffset, bool isSolid, bool isCircular = false, double radius = 0)
            {
                ProfilePoints = profilePoints;
                PlaneNormal = planeNormal;
                PlaneOrigin = planeOrigin;
                StartOffset = startOffset;
                EndOffset = endOffset;
                IsSolid = isSolid;
                IsCircular = isCircular;
                CircleRadius = radius;
            }
        }

        /// <summary>
        /// 连接器创建参数
        /// </summary>
        public readonly struct ConnectorParameters
        {
            public int ExtrusionIndex { get; }      // 关联的拉伸体索引
            public ConnectorType Type { get; }      // 连接器类型
            public double Height { get; }           // 高度（矩形）
            public double Width { get; }            // 宽度（矩形）
            public double Radius { get; }           // 半径（圆形）
            public double Flow { get; }             // 流量
            public int FlowDirection { get; }       // 流向 (1=进, 2=出)
            public int FlowConfiguration { get; }   // 流量配置

            public ConnectorParameters(int extrusionIndex, ConnectorType type, double height,
                double width, double flow, int flowDirection, int flowConfig = 1)
            {
                ExtrusionIndex = extrusionIndex;
                Type = type;
                Height = height;
                Width = width;
                Radius = 0;
                Flow = flow;
                FlowDirection = flowDirection;
                FlowConfiguration = flowConfig;
            }

            public ConnectorParameters(int extrusionIndex, ConnectorType type, double radius,
                double flow, int flowDirection, int flowConfig = 1)
            {
                ExtrusionIndex = extrusionIndex;
                Type = type;
                Height = 0;
                Width = 0;
                Radius = radius;
                Flow = flow;
                FlowDirection = flowDirection;
                FlowConfiguration = flowConfig;
            }
        }

        /// <summary>
        /// 连接器类型枚举
        /// </summary>
        public enum ConnectorType
        {
            DuctSupplyAir,
            DuctReturnAir,
            PipeSupplyHydronic,
            PipeReturnHydronic
        }
    }
    #region 扩展方法

    /// <summary>
    /// 几何对象扩展方法
    /// 使用C# 7.3的表达式体成员
    /// </summary>
    public static partial class GeometryExtensions
    {
        /// <summary>
        /// 安全获取几何元素
        /// </summary>
        public static GeometryElement GetGeometrySafe(this Element element, Options options) =>
            element?.get_Geometry(options);

        /// <summary>
        /// 获取所有平面类型的面
        /// </summary>
        public static IEnumerable<PlanarFace> GetPlanarFaces(this Solid solid)
        {
            if (solid is null) yield break;

            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                if (face is PlanarFace planarFace)
                    yield return planarFace;
            }
        }
    }

    #endregion
}
