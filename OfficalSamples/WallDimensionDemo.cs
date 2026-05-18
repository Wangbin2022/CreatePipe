using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CreatePipe.OfficalSamples
{
    internal class WallDimensionDemo
    {
        public WallDimensionDemo(ExternalCommandData commandData)
        {
            string message = string.Empty;
            // 验证输入
            if (!TryValidateCommandData(commandData, ref message, out var document, out var activeView))
                return;
            // 验证视图类型（只支持2D视图）
            if (!IsValidDimensionView(activeView, ref message))
                return;
            // 获取选中的墙体
            var selectedWalls = GetSelectedWalls(commandData);
            if (!selectedWalls.Any())
            {
                TaskDialog.Show("tt", "请至少选择一面基础墙(Basic Wall)");
                return;
            }
            // 创建尺寸标注
            CreateDimensionsForWalls(document, activeView, selectedWalls, ref message);
        }
        /// <summary>双精度比较精度</summary>
        private const double DOUBLE_PRECISION = 1e-7;
        /// <summary>尺寸标注偏移量</summary>
        private const double DIMENSION_OFFSET = 5.0;
        /// <summary>支持的墙体类型名称</summary>
        private const string SUPPORTED_WALL_KIND = "Basic";
        /// <summary>
        /// 验证命令数据是否有效
        /// 使用C# 7.3的out变量和模式匹配
        /// </summary>
        private bool TryValidateCommandData(ExternalCommandData commandData, ref string message,
            out Document document, out View activeView)
        {
            document = null;
            activeView = null;

            if (commandData?.Application?.ActiveUIDocument is null)
            {
                message = "无法获取活动文档";
                return false;
            }

            var uiDoc = commandData.Application.ActiveUIDocument;
            document = uiDoc.Document;
            activeView = uiDoc.ActiveView;

            return true;
        }

        /// <summary>
        /// 验证视图是否支持尺寸标注（非3D视图、非图纸视图）
        /// 使用C# 7.3的模式匹配
        /// </summary>
        private bool IsValidDimensionView(View view, ref string message)
        {
            if (view is null)
            {
                message = "无法获取活动视图";
                return false;
            }

            // 使用模式匹配检查视图类型
            switch (view)
            {
                case View3D _:
                    message = "不能在3D视图中创建尺寸标注";
                    return false;
                case ViewSheet _:
                    message = "不能在图纸视图中创建尺寸标注";
                    return false;
                default:
                    return true;
            }
        }
        /// <summary>
        /// 获取选中的基础墙
        /// 使用C# 7.3的LINQ和模式匹配
        /// </summary>
        private List<Wall> GetSelectedWalls(ExternalCommandData commandData)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var selectedIds = uiDoc.Selection.GetElementIds();

            // 使用LINQ筛选基础墙
            return selectedIds
                .Select(id => uiDoc.Document.GetElement(id))
                .OfType<Wall>()                          // 模式匹配：筛选Wall类型
                .Where(wall => IsBasicWall(wall))
                .ToList();
        }

        /// <summary>
        /// 判断是否为基础墙
        /// 使用C# 7.3的表达式体
        /// </summary>
        private bool IsBasicWall(Wall wall) =>
            wall?.WallType?.Kind.ToString() == SUPPORTED_WALL_KIND;
        /// <summary>
        /// 为选中的墙体创建尺寸标注
        /// 使用C# 7.3的本地函数和using声明
        /// </summary>
        private Result CreateDimensionsForWalls(Document document, View activeView,
            List<Wall> walls, ref string message)
        {
            var transaction = new Transaction(document, "添加墙体尺寸标注");
            transaction.Start();

            var successCount = 0;
            var errorMessages = new List<string>();

            foreach (var wall in walls)
            {
                // 尝试创建尺寸标注，使用本地函数处理错误
                if (TryCreateDimensionForWall(document, activeView, wall, out var errorMessage))
                {
                    successCount++;
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorMessages.Add(errorMessage);
                }
            }

            if (successCount == 0)
            {
                transaction.RollBack();
                message = errorMessages.Any()
                    ? string.Join("; ", errorMessages)
                    : "未能为任何选中的墙体创建尺寸标注";
                return Result.Failed;
            }

            transaction.Commit();
            message = $"成功为 {successCount} 面墙体创建尺寸标注";

            if (errorMessages.Any())
                message += $"，警告: {string.Join("; ", errorMessages)}";

            return Result.Succeeded;
        }

        /// <summary>
        /// 为单面墙体创建尺寸标注
        /// 使用C# 7.3的元组和本地函数
        /// </summary>
        private bool TryCreateDimensionForWall(Document document, View activeView,
            Wall wall, out string errorMessage)
        {
            errorMessage = null;

            // 使用元组解构获取墙体线段和分析参考
            var (lineSegment, references) = ExtractWallDimensionData(wall);

            if (lineSegment is null)
            {
                errorMessage = $"墙体 ID:{wall.Id.IntegerValue} 无法获取有效的线段";
                return false;
            }

            if (references is null || references.Size != 2)
            {
                errorMessage = $"墙体 ID:{wall.Id.IntegerValue} 无法获取足够的分析参考点 (需要2个)";
                return false;
            }

            // 创建偏移后的尺寸标注线
            var dimensionLine = CreateOffsetDimensionLine(lineSegment, DIMENSION_OFFSET);
            if (dimensionLine is null)
            {
                errorMessage = $"墙体 ID:{wall.Id.IntegerValue} 无法创建尺寸标注线";
                return false;
            }

            // 创建尺寸标注
            try
            {
                var dimension = document.Create.NewDimension(activeView, dimensionLine, references);
                return dimension != null;
            }
            catch (Exception ex)
            {
                errorMessage = $"墙体 ID:{wall.Id.IntegerValue} 创建尺寸标注失败: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 提取墙体的尺寸标注数据
        /// 返回：(线段, 参考点数组)
        /// 使用C# 7.3的元组返回多个值
        /// </summary>
        private (Line lineSegment, ReferenceArray references) ExtractWallDimensionData(Wall wall)
        {
            // 获取墙体位置曲线
            if (!TryGetLocationCurve(wall, out var locationCurve))
                return (null, null);

            // 获取分析模型
            var analyticalModel = wall.GetAnalyticalModel();
            if (analyticalModel is null)
                return (null, null);

            // 获取活动曲线
            var activeCurves = analyticalModel.GetCurves(AnalyticalCurveType.ActiveCurves);
            if (activeCurves is null || !activeCurves.Any())
                return (null, null);

            Line horizontalLine = null;
            var references = new ReferenceArray();

            // 遍历曲线，找到水平线段和分析参考点
            foreach (var curve in activeCurves)
            {
                // 使用模式匹配和元组判断曲线类型
                if (TryClassifyCurve(curve, out var isHorizontal, out var isVertical, out var line))
                {
                    if (isHorizontal && horizontalLine is null)
                    {
                        horizontalLine = line;
                    }

                    if (isVertical && references.Size < 2)
                    {
                        AddReferenceFromCurve(analyticalModel, curve, references);
                    }
                }

                // 提前退出条件
                if (horizontalLine != null && references.Size >= 2)
                    break;
            }

            return (horizontalLine, references);
        }

        /// <summary>
        /// 尝试获取墙体位置曲线
        /// </summary>
        private bool TryGetLocationCurve(Wall wall, out LocationCurve locationCurve)
        {
            locationCurve = wall?.Location as LocationCurve;
            return locationCurve != null;
        }

        /// <summary>
        /// 分类曲线类型
        /// 使用C# 7.3的元组和模式匹配
        /// </summary>
        private bool TryClassifyCurve(Curve curve, out bool isHorizontal, out bool isVertical, out Line line)
        {
            isHorizontal = false;
            isVertical = false;
            line = curve as Line;

            if (line is null) return false;

            var startZ = line.GetEndPoint(0).Z;
            var endZ = line.GetEndPoint(1).Z;
            var isHorizontalCurve = Math.Abs(startZ - endZ) < DOUBLE_PRECISION;
            var isVerticalCurve = !isHorizontalCurve;

            isHorizontal = isHorizontalCurve;
            isVertical = isVerticalCurve;

            return true;
        }

        /// <summary>
        /// 从曲线添加分析参考点
        /// </summary>
        private void AddReferenceFromCurve(AnalyticalModel analyticalModel, Curve curve, ReferenceArray references)
        {
            var selector = new AnalyticalModelSelector(curve)
            {
                CurveSelector = AnalyticalCurveSelector.StartPoint
            };
            var reference = analyticalModel.GetReference(selector);
            if (reference != null)
            {
                references.Append(reference);
            }
        }

        /// <summary>
        /// 创建偏移后的尺寸标注线
        /// 使用C# 7.3的元组解构
        /// </summary>
        private Line CreateOffsetDimensionLine(Line originalLine, double offset)
        {
            if (originalLine is null) return null;

            var startPoint = originalLine.GetEndPoint(0);
            var endPoint = originalLine.GetEndPoint(1);

            // 计算偏移方向（垂直于线段方向）
            var direction = endPoint - startPoint;
            var perpendicular = new XYZ(-direction.Y, direction.X, 0).Normalize();

            // 应用偏移
            var offsetVector = perpendicular * offset;
            var newStart = startPoint + offsetVector;
            var newEnd = endPoint + offsetVector;

            return Line.CreateBound(newStart, newEnd);
        }

    }
    ///// <summary>
    ///// 几何扩展方法
    ///// 使用C# 7.3的表达式体成员
    ///// </summary>
    //public static class GeometryExtensions
    //{
    //    /// <summary>
    //    /// 判断曲线是否为水平线（Z坐标不变）
    //    /// </summary>
    //    public static bool IsHorizontal(this Curve curve, double precision = 1e-7)
    //    {
    //        if (curve is null) return false;
    //        var diff = Math.Abs(curve.GetEndPoint(0).Z - curve.GetEndPoint(1).Z);
    //        return diff < precision;
    //    }

    //    /// <summary>
    //    /// 判断曲线是否为垂直线（Z坐标变化）
    //    /// </summary>
    //    public static bool IsVertical(this Curve curve, double precision = 1e-7) =>
    //        !curve.IsHorizontal(precision);

    //    /// <summary>
    //    /// 获取曲线的方向向量
    //    /// </summary>
    //    public static XYZ GetDirection(this Curve curve)
    //    {
    //        if (curve is null) return XYZ.Zero;
    //        return (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
    //    }

    //    /// <summary>
    //    /// 获取曲线的垂直向量（在XY平面内）
    //    /// </summary>
    //    public static XYZ GetPerpendicular(this Curve curve)
    //    {
    //        var direction = curve.GetDirection();
    //        return new XYZ(-direction.Y, direction.X, 0).Normalize();
    //    }
    //}
}
