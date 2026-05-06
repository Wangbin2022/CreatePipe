using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe.OfficalSamples
{
    internal class CreateColumnByWall
    {
        // 硬编码的柱族参数（可改为配置）
        private const string TargetFamilyName = "M_Wood Timber Column";
        private const string TargetSymbolName = "191 x 292mm";
        private const double DefaultColumnSpacing = 5.0;  // 单位：英尺
        private const string TransactionName = "沿墙布置结构柱";
        public CreateColumnByWall(ExternalCommandData commandData)
        {
            string message = string.Empty;
            // 验证并获取必要对象
            if (!TryGetRevitObjects(commandData, out var document, out var uiDocument, out string error))
            {
                message = error; return;
            }
            try
            {
                // 获取选中的墙元素
                var selectedWalls = GetSelectedWalls(uiDocument);
                // 验证选中结果
                if (!selectedWalls.Any())
                {
                    message = "请选中至少一个带有顶部和底部约束的墙"; return;
                }
                // 显示选中墙数量
                ShowSelectionInfo(selectedWalls.Count);
                // 查找目标柱族符号
                var columnSymbol = FindFamilySymbol(document, TargetFamilyName, TargetSymbolName);
                if (columnSymbol is null)
                {
                    message = $"请加载柱族: {TargetFamilyName} : {TargetSymbolName}"; return;
                }
                // 执行布置柱操作
                using (Transaction transaction = new Transaction(document, TransactionName))
                {
                    transaction.Start();

                    foreach (var wall in selectedWalls)
                    {
                        FrameWall(document, wall, DefaultColumnSpacing, columnSymbol);
                    }
                    transaction.Commit();
                }
                ShowCompletionMessage(selectedWalls.Count);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                message = $"操作错误: {ex.Message}"; return;
            }
            catch (Exception ex)
            {
                message = $"未预期的错误: {ex.Message}\n{ex.StackTrace}"; return;
            }
        }
        #region 辅助方法 - 初始化和验证
        /// <summary>
        /// 尝试获取Revit应用对象（使用out参数和元组）
        /// </summary>
        private bool TryGetRevitObjects(ExternalCommandData commandData,
            out Document document,
            out UIDocument uiDocument,
            out string error)
        {
            document = null;
            uiDocument = null;
            error = string.Empty;

            if (commandData?.Application?.ActiveUIDocument is null)
            {
                error = "无法获取有效的Revit文档";
                return false;
            }

            uiDocument = commandData.Application.ActiveUIDocument;
            document = uiDocument.Document;

            return document is object;
        }

        /// <summary>
        /// 获取选中的有效墙（表达式体方法）
        /// </summary>
        private List<Wall> GetSelectedWalls(UIDocument uiDocument) =>
            uiDocument.Selection.GetElementIds()
                .Select(id => uiDocument.Document.GetElement(id))
                .OfType<Wall>()
                .Where(wall => IsWallConstrained(wall))
                .ToList();

        /// <summary>
        /// 检查墙是否具有顶部和底部约束
        /// </summary>
        private bool IsWallConstrained(Wall wall)
        {
            var baseParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var topParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);

            return baseParam != null && topParam != null
                && baseParam.HasValue && topParam.HasValue;
        }

        /// <summary>
        /// 显示选中墙信息（使用字符串插值）
        /// </summary>
        private void ShowSelectionInfo(int wallCount)
        {
            TaskDialog.Show("Revit", $"选中了 {wallCount} 个具有约束的墙");
        }

        /// <summary>
        /// 显示完成信息
        /// </summary>
        private void ShowCompletionMessage(int wallCount)
        {
            TaskDialog.Show("Revit", $"成功在 {wallCount} 个墙上布置了结构柱");
        }
        #endregion

        #region 核心业务方法
        /// <summary>
        /// 查找族符号（使用LINQ简化代码）
        /// </summary>
        private FamilySymbol FindFamilySymbol(Document document, string familyName, string symbolName)
        {
            // 使用LINQ查询替代迭代器
            var targetFamily = new FilteredElementCollector(document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(family => family.Name == familyName);

            if (targetFamily is null) return null;

            // 使用LINQ查找匹配的族符号
            return targetFamily.GetFamilySymbolIds()
                .Select(id => document.GetElement(id) as FamilySymbol)
                .FirstOrDefault(symbol => symbol?.Name == symbolName);
        }

        /// <summary>
        /// 在墙上布置柱
        /// </summary>
        private void FrameWall(Document document, Wall wall, double spacing, FamilySymbol columnType)
        {
            // 获取墙的位置信息（使用元组）
            var (startPoint, endPoint, wallVector) = GetWallGeometry(wall);

            // 获取墙的约束层级
            var (baseLevelId, topLevelId) = GetWallLevels(wall);

            // 计算墙的长度和柱数量
            double wallLength = wallVector.GetLength();
            int columnCount = CalculateColumnCount(wallLength, spacing);

            // 显示调试信息
            ShowWallInfo(wallLength, spacing, columnCount);

            // 计算布置方向向量
            var normalizedVector = wallVector.Normalize();
            var direction = new UV(normalizedVector.X, normalizedVector.Y);

            // 计算旋转角度
            double angle = CalculateRotationAngle(direction, new UV(1, 0));

            // 放置柱
            PlaceColumnsAlongWall(document, startPoint, endPoint, direction,
                spacing, columnCount, angle, columnType, baseLevelId, topLevelId);
        }

        /// <summary>
        /// 获取墙几何信息（使用元组返回多个值）
        /// </summary>
        private (XYZ start, XYZ end, XYZ vector) GetWallGeometry(Wall wall)
        {
            var location = wall.Location as LocationCurve;
            if (location is null)
                throw new InvalidOperationException("无法获取墙的定位曲线");

            var start = location.Curve.GetEndPoint(0);
            var end = location.Curve.GetEndPoint(1);
            var vector = new XYZ(end.X - start.X, end.Y - start.Y, end.Z - start.Z);

            return (start, end, vector);
        }

        /// <summary>
        /// 获取墙的层级约束（使用元组）
        /// </summary>
        private (ElementId baseId, ElementId topId) GetWallLevels(Wall wall)
        {
            var baseParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var topParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);

            if (baseParam is null || topParam is null)
                throw new InvalidOperationException("墙缺少层级约束参数");

            return (baseParam.AsElementId(), topParam.AsElementId());
        }

        /// <summary>
        /// 计算需要布置的柱数量（起点不计入，终点单独处理）
        /// </summary>
        private int CalculateColumnCount(double wallLength, double spacing)
        {
            // 计算中间柱数量（不包括终点）
            return (int)(wallLength / spacing);
        }

        /// <summary>
        /// 显示墙信息（使用字符串插值）
        /// </summary>
        private void ShowWallInfo(double wallLength, double spacing, int columnCount)
        {
            TaskDialog.Show("Revit",
                $"墙长度 = {wallLength:F2} 英尺\n" +
                $"柱间距 = {spacing} 英尺\n" +
                $"中间柱数量 = {columnCount}");
        }

        /// <summary>
        /// 计算旋转角度
        /// </summary>
        private double CalculateRotationAngle(UV direction, UV axis)
        {
            return direction.AngleTo(axis);
        }

        /// <summary>
        /// 沿墙放置柱
        /// </summary>
        private void PlaceColumnsAlongWall(
            Document document,
            XYZ startPoint,
            XYZ endPoint,
            UV direction,
            double spacing,
            int columnCount,
            double angle,
            FamilySymbol columnType,
            ElementId baseLevelId,
            ElementId topLevelId)
        {
            var currentPoint = startPoint;
            var deltaX = direction.U * spacing;
            var deltaY = direction.V * spacing;

            // 放置中间柱（不包括终点）
            for (int i = 0; i < columnCount; i++)
            {
                PlaceColumn(document, currentPoint, angle, columnType, baseLevelId, topLevelId);
                currentPoint = new XYZ(currentPoint.X + deltaX, currentPoint.Y + deltaY, currentPoint.Z);
            }

            // 在终点放置柱
            PlaceColumn(document, endPoint, angle, columnType, baseLevelId, topLevelId);
        }
        #endregion

        #region 柱放置方法
        /// <summary>
        /// 在指定位置放置柱
        /// </summary>
        private void PlaceColumn(
            Document document,
            XYZ position,
            double angle,
            FamilySymbol columnType,
            ElementId baseLevelId,
            ElementId topLevelId)
        {
            // 激活族符号（如果未激活）
            if (!columnType.IsActive)
                columnType.Activate();

            // 获取基准层级
            var baseLevel = document.GetElement(baseLevelId) as Level;
            if (baseLevel is null)
                throw new InvalidOperationException("无效的基准层级");

            // 创建柱实例
            var column = document.Create.NewFamilyInstance(
                position,
                columnType,
                baseLevel,
                StructuralType.Column);

            if (column is null)
            {
                TaskDialog.Show("Revit", "创建柱实例失败");
                return;
            }

            // 旋转柱以对齐墙的方向
            RotateColumn(column, position, angle);

            // 设置柱的顶部和底部层级
            SetColumnLevels(column, baseLevelId, topLevelId);
        }

        /// <summary>
        /// 旋转柱（使用表达式体方法）
        /// </summary>
        private void RotateColumn(FamilyInstance column, XYZ position, double angle)
        {
            var rotationAxis = Line.CreateUnbound(position, XYZ.BasisZ);
            column.Location.Rotate(rotationAxis, angle);
        }

        /// <summary>
        /// 设置柱的层级参数
        /// </summary>
        private void SetColumnLevels(FamilyInstance column, ElementId baseLevelId, ElementId topLevelId)
        {
            var baseParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            var topParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

            if (baseParam != null && baseParam.HasValue)
                baseParam.Set(baseLevelId);

            if (topParam != null && topParam.HasValue)
                topParam.Set(topLevelId);
        }
        #endregion
    }
}
