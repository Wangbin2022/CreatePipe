using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class SectionViewCreator
    {
        /// <summary>
        /// 选中的元素类型枚举
        /// </summary>
        private enum SelectType
        {
            Wall = 0,
            Beam = 1,
            Floor = 2,
            Invalid = -1
        }
        private UIDocument _uiDoc;
        private Document _document;
        private Element _selectedElement;
        private SelectType _elementType = SelectType.Invalid;
        private string _errorMessage;
        public SectionViewCreator(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                if (!TryInitialize(commandData, ref message)) return;
                if (!TrySelectAndValidateElement())
                {
                    message = _errorMessage; return;
                }
                var boundingBox = CreateBoundingBox();
                if (boundingBox is null)
                {
                    message = _errorMessage; return;
                }
                CreateSectionView(boundingBox, ref message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return;
            }
        }
        private const double PRECISION = 1e-10;
        private const double BOX_HALF_LENGTH = 10.0;   // 包围盒半长
        private const double BOX_HALF_WIDTH = 10.0;    // 包围盒半宽
        private const double BOX_HEIGHT = 5.0;         // 包围盒高度
        private const int DETAIL_LEVEL_FINE = 2;       // 详细程度：精细
        /// <summary>
        /// 初始化命令数据
        /// 使用C# 7.3的模式匹配验证
        /// </summary>
        private bool TryInitialize(ExternalCommandData commandData, ref string message)
        {
            _uiDoc = commandData?.Application?.ActiveUIDocument;
            if (_uiDoc is null)
            {
                message = "无法获取活动文档";
                return false;
            }
            _document = _uiDoc.Document;
            return true;
        }
        /// <summary>
        /// 选择并验证元素
        /// 使用C# 7.3的模式匹配和LINQ
        /// </summary>
        private bool TrySelectAndValidateElement()
        {
            var selectedIds = _uiDoc.Selection.GetElementIds();
            if (selectedIds.Count != 1)
            {
                _errorMessage = "请仅选择一个元素（墙、梁或楼板）";
                return false;
            }
            _selectedElement = _document.GetElement(selectedIds.First());
            // 使用模式匹配判断元素类型
            switch (_selectedElement)
            {
                case Wall wall:
                    return ValidateWall(wall);
                case FamilyInstance beam when beam.StructuralType == StructuralType.Beam:
                    return ValidateBeam(beam);
                case Floor floor:
                    return ValidateFloor(floor);
                default:
                    _errorMessage = "请选择墙、梁或楼板元素";
                    return false;
            }
        }
        /// <summary>
        /// 验证墙体是否为直线墙
        /// 使用C# 7.3的模式匹配
        /// </summary>
        private bool ValidateWall(Wall wall)
        {
            var location = wall.Location as LocationCurve;
            if (location?.Curve is Line)
            {
                _elementType = SelectType.Wall;
                return true;
            }
            _errorMessage = "请选择直线墙";
            return false;
        }
        /// <summary>
        /// 验证梁是否为水平梁
        /// 使用元组和参数验证
        /// </summary>
        private bool ValidateBeam(FamilyInstance beam)
        {
            // 检查梁是否水平
            var startOffset = beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION)?.AsDouble() ?? 0;
            var endOffset = beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION)?.AsDouble() ?? 0;
            if (Math.Abs(startOffset - endOffset) > PRECISION)
            {
                _errorMessage = "请选择水平梁";
                return false;
            }
            // 检查分析模型是否存在
            if (beam.GetAnalyticalModel()?.GetCurve() is null)
            {
                _errorMessage = "选中的梁没有分析模型线";
                return false;
            }
            _elementType = SelectType.Beam;
            return true;
        }
        /// <summary>
        /// 验证楼板是否为结构楼板
        /// </summary>
        private bool ValidateFloor(Floor floor)
        {
            var model = floor.GetAnalyticalModel();
            if (model is null)
            {
                _errorMessage = "请选择结构楼板";
                return false;
            }
            var curves = model.GetCurves(AnalyticalCurveType.ActiveCurves);
            if (curves is null || !curves.Any())
            {
                _errorMessage = "楼板分析模型无效";
                return false;
            }
            _elementType = SelectType.Floor;
            return true;
        }
        /// <summary>
        /// 创建BoundingBoxXYZ
        /// 使用using声明简化事务
        /// </summary>
        private BoundingBoxXYZ CreateBoundingBox()
        {
            var transaction = new Transaction(_document, "创建包围盒");
            transaction.Start();
            var box = new BoundingBoxXYZ
            {
                Enabled = true,
                Max = new XYZ(BOX_HALF_LENGTH, BOX_HALF_WIDTH, 0),
                Min = new XYZ(-BOX_HALF_LENGTH, -BOX_HALF_WIDTH, -BOX_HEIGHT)
            };
            var transform = CreateTransform();
            if (transform is null)
            {
                transaction.RollBack();
                return null;
            }
            box.Transform = transform;
            transaction.Commit();
            return box;
        }
        /// <summary>
        /// 创建变换矩阵
        /// 使用C# 7.3的switch表达式
        /// </summary>
        private Transform CreateTransform()
        {
            switch (_elementType)
            {
                case SelectType.Wall:
                    return CreateWallTransform();
                case SelectType.Beam:
                    return CreateBeamTransform();
                case SelectType.Floor:
                    return CreateFloorTransform();
                default:
                    return null;
            }
        }
        /// <summary>
        /// 创建墙体的变换矩阵
        /// </summary>
        private Transform CreateWallTransform()
        {
            var wall = _selectedElement as Wall;
            if (wall is null) return null;

            var location = wall.Location as LocationCurve;
            var locationLine = location?.Curve as Line;
            if (locationLine is null) return null;

            var transform = Transform.Identity;
            var (start, end) = (locationLine.GetEndPoint(0), locationLine.GetEndPoint(1));
            var midPoint = XYZMath.FindMidPoint(start, end);

            // 计算墙体中点高程偏移
            var baseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET)?.AsDouble() ?? 0;
            var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0;
            var zOffset = baseOffset + height / 2;

            transform.Origin = new XYZ(midPoint.X, midPoint.Y, midPoint.Z + zOffset);

            var basisZ = XYZMath.FindDirection(start, end);
            var (basisX, basisY) = (XYZMath.FindRightDirection(basisZ), XYZMath.FindUpDirection(basisZ));

            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            return transform;
        }

        /// <summary>
        /// 创建梁的变换矩阵
        /// </summary>
        private Transform CreateBeamTransform()
        {
            var beam = _selectedElement as FamilyInstance;
            if (beam is null) return null;

            var analyticalModel = beam.GetAnalyticalModel();
            var curve = analyticalModel?.GetCurve();
            if (curve is null) return null;

            var transform = Transform.Identity;
            var (start, end) = (curve.GetEndPoint(0), curve.GetEndPoint(1));
            var midPoint = XYZMath.FindMidPoint(start, end);
            transform.Origin = midPoint;

            var basisZ = XYZMath.FindDirection(start, end);
            var (basisX, basisY) = (XYZMath.FindRightDirection(basisZ), XYZMath.FindUpDirection(basisZ));

            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            return transform;
        }

        /// <summary>
        /// 创建楼板的变换矩阵
        /// </summary>
        private Transform CreateFloorTransform()
        {
            var floor = _selectedElement as Floor;
            if (floor is null) return null;

            var analyticalModel = floor.GetAnalyticalModel();
            var curves = analyticalModel?.GetCurves(AnalyticalCurveType.ActiveCurves);
            if (curves is null || !curves.Any()) return null;

            var transform = Transform.Identity;
            var midPoint = XYZMath.FindMiddlePoint(curves);
            transform.Origin = midPoint;

            var firstCurve = curves.First();
            var (start, end) = (firstCurve.GetEndPoint(0), firstCurve.GetEndPoint(1));
            var basisZ = XYZMath.FindDirection(start, end);
            var (basisX, basisY) = (XYZMath.FindRightDirection(basisZ), XYZMath.FindUpDirection(basisZ));

            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            return transform;
        }
        /// <summary>
        /// 创建剖面视图
        /// 使用C# 7.3的LINQ查找视图族类型
        /// </summary>
        private Result CreateSectionView(BoundingBoxXYZ boundingBox, ref string message)
        {
            var transaction = new Transaction(_document, "创建剖面视图");
            transaction.Start();

            // 查找详图视图族类型
            var detailViewTypeId = FindDetailViewFamilyTypeId();
            if (detailViewTypeId == ElementId.InvalidElementId)
            {
                message = "未找到详图视图族类型";
                transaction.RollBack();
                return Result.Failed;
            }

            var section = ViewSection.CreateDetail(_document, detailViewTypeId, boundingBox);
            if (section is null)
            {
                message = "无法创建剖面视图";
                transaction.RollBack();
                return Result.Failed;
            }

            // 设置详细程度为精细
            section.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL)?.Set(DETAIL_LEVEL_FINE);

            transaction.Commit();

            TaskDialog.Show("Revit", $"剖面视图创建成功！视图名称: {section.Name}");
            return Result.Succeeded;
        }

        /// <summary>
        /// 查找详图视图族类型ID
        /// 使用C# 7.3的LINQ和模式匹配
        /// </summary>
        private ElementId FindDetailViewFamilyTypeId()
        {
            var collector = new FilteredElementCollector(_document);

            return collector
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.Detail)
                ?.Id ?? ElementId.InvalidElementId;
        }
    }
    /// <summary>
    /// XYZ数学工具类
    /// 提供点和向量的常用计算
    /// 使用C# 7.3的表达式体和元组
    /// </summary>
    public static class XYZMath
    {
        private const double PRECISION = 1e-10;

        /// <summary>
        /// 计算线段中点
        /// 使用C# 7.3的元组解构
        /// </summary>
        public static XYZ FindMidPoint(XYZ first, XYZ second)
        {
            return new XYZ(
                (first.X + second.X) / 2,
                (first.Y + second.Y) / 2,
                (first.Z + second.Z) / 2
            );
        }

        /// <summary>
        /// 计算两点间距离
        /// 使用C# 7.3的表达式体
        /// </summary>
        public static double FindDistance(XYZ first, XYZ second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            var dz = first.Z - second.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 计算从first指向second的单位方向向量
        /// </summary>
        public static XYZ FindDirection(XYZ first, XYZ second)
        {
            var dx = second.X - first.X;
            var dy = second.Y - first.Y;
            var dz = second.Z - first.Z;
            var distance = FindDistance(first, second);

            return new XYZ(dx / distance, dy / distance, dz / distance);
        }

        /// <summary>
        /// 计算右方向向量（垂直于视图方向）
        /// 绕Z轴旋转90度
        /// 使用C# 7.3的表达式体
        /// </summary>
        public static XYZ FindRightDirection(XYZ viewDirection) =>
            new XYZ(-viewDirection.Y, viewDirection.X, viewDirection.Z);

        /// <summary>
        /// 计算上方向向量
        /// 对于水平和垂直视图，上方向为Z轴正方向
        /// </summary>
        public static XYZ FindUpDirection(XYZ viewDirection) =>
            new XYZ(0, 0, 1);

        /// <summary>
        /// 查找轮廓的中点
        /// 使用C# 7.3的LINQ聚合
        /// </summary>
        public static XYZ FindMiddlePoint(IEnumerable<Curve> curves)
        {
            // 收集所有端点
            var points = new List<XYZ>();
            foreach (var curve in curves)
            {
                points.Add(curve.GetEndPoint(0));
                points.Add(curve.GetEndPoint(1));
            }

            if (!points.Any()) return XYZ.Zero;

            // 计算边界
            var maxX = points.Max(p => p.X);
            var minX = points.Min(p => p.X);
            var maxY = points.Max(p => p.Y);
            var minY = points.Min(p => p.Y);
            var maxZ = points.Max(p => p.Z);
            var minZ = points.Min(p => p.Z);

            return new XYZ(
                (maxX + minX) / 2,
                (maxY + minY) / 2,
                (maxZ + minZ) / 2
            );
        }

        /// <summary>
        /// 查找墙体剖面视图方向
        /// 找到第一条非垂直/水平的曲线
        /// </summary>
        public static XYZ FindWallViewDirection(IEnumerable<Curve> curves)
        {
            foreach (var curve in curves)
            {
                var start = curve.GetEndPoint(0);
                var end = curve.GetEndPoint(1);
                var dx = Math.Abs(start.X - end.X);
                var dy = Math.Abs(start.Y - end.Y);

                // 检查曲线是否为水平或垂直方向（有实际长度）
                if (dx > PRECISION || dy > PRECISION)
                {
                    var start2D = new XYZ(start.X, start.Y, 0);
                    var end2D = new XYZ(end.X, end.Y, 0);
                    return FindDirection(start2D, end2D);
                }
            }

            return XYZ.BasisX;
        }

        /// <summary>
        /// 查找楼板剖面视图方向
        /// 使用第一条曲线的方向
        /// </summary>
        public static XYZ FindFloorViewDirection(IEnumerable<Curve> curves)
        {
            var firstCurve = curves.FirstOrDefault();
            if (firstCurve is null) return XYZ.BasisX;

            var start = firstCurve.GetEndPoint(0);
            var end = firstCurve.GetEndPoint(1);
            return FindDirection(start, end);
        }
    }
}
