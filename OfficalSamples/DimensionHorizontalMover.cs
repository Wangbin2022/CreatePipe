using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class DimensionHorizontalMover
    {
        public DimensionHorizontalMover(ExternalCommandData commandData)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            string message = string.Empty;
            try
            {
                // 可选：让用户输入移动距离
                if (UseInteractiveInput)
                {
                    var inputResult = GetUserInput();
                    if (!inputResult.Success)
                    {
                        message = inputResult.Message; return;
                    }
                    Delta = inputResult.DeltaValue;
                }
                // 获取选中的尺寸标注
                var dimensions = GetSelectedDimensions(uidoc);
                if (!dimensions.Any())
                {
                    message = NO_VALID_DIMENSION;
                    TaskDialog.Show("Revit", NO_SELECTION_MESSAGE);
                    return;
                }
                // 执行移动操作
                var result = MoveDimensionLeaders(doc, dimensions);
                message = result.Message;
            }
            catch (Exception ex)
            {
                message = $"执行失败：{ex.Message}"; return;
            }
        }
        private const string TRANSACTION_NAME = "水平移动尺寸标注引线";
        private const string NO_SELECTION_MESSAGE = "请至少选中一个尺寸标注元素";
        private const string NO_VALID_DIMENSION = "选中的元素中没有有效的尺寸标注";
        private const string OPERATION_FAILED = "移动引线失败: {0}";
        /// <summary>移动偏移量（单位：英尺）</summary>
        private double Delta { get; set; } = -10.0;

        /// <summary>是否使用交互式输入</summary>
        private bool UseInteractiveInput { get; set; } = false;
        /// <summary>
        /// 获取用户输入的偏移量
        /// </summary>
        private (bool Success, double DeltaValue, string Message) GetUserInput()
        {
            // 使用TaskDialog显示输入框
            var options = new TaskDialog("输入偏移量");
            options.Title = "移动距离设置";
            options.MainInstruction = "请输入引线移动距离（单位：英尺）";
            options.MainContent = $"当前偏移量: {Delta}";
            options.CommonButtons = TaskDialogCommonButtons.Ok;

            // 注意：TaskDialog不支持直接输入，此处简化为确认使用默认值
            // 实际项目中可使用WPF对话框获取输入
            var result = options.Show();

            if (result == TaskDialogResult.Cancel)
                return (false, 0, "用户取消了操作");

            return (true, Delta, "使用默认偏移量");
        }

        /// <summary>
        /// 获取选中的尺寸标注
        /// </summary>
        private List<Dimension> GetSelectedDimensions(UIDocument uidoc)
        {
            return uidoc.Selection.GetElementIds()
                .Select(id => uidoc.Document.GetElement(id))
                .OfType<Dimension>()
                .ToList();
        }

        /// <summary>
        /// 移动尺寸标注的引线端点
        /// </summary>
        private (Result Status, string Message) MoveDimensionLeaders(Document doc, List<Dimension> dimensions)
        {
            var successCount = 0;
            var failedDimensions = new List<long>();

            using (var transaction = new Transaction(doc, TRANSACTION_NAME))
            {
                transaction.Start();

                foreach (var dimension in dimensions)
                {
                    try
                    {
                        MoveSingleDimension(dimension);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedDimensions.Add(dimension.Id.IntegerValue);
                        TaskDialog.Show("警告", $"尺寸标注 {dimension.Id.IntegerValue} 处理失败: {ex.Message}");
                    }
                }

                if (failedDimensions.Any())
                {
                    transaction.RollBack();
                    var failedMessage = $"以下尺寸标注处理失败: {string.Join(", ", failedDimensions)}";
                    return (Result.Failed, failedMessage);
                }

                transaction.Commit();
            }

            var resultMessage = $"成功移动 {successCount} 个尺寸标注的引线";
            TaskDialog.Show("完成", resultMessage);
            return (Result.Succeeded, resultMessage);
        }

        /// <summary>
        /// 移动单个尺寸标注的引线
        /// </summary>
        private void MoveSingleDimension(Dimension dimension)
        {
            // 获取尺寸线的方向
            var line = dimension.Curve as Line;
            if (line == null)
            {
                throw new InvalidOperationException("无法获取尺寸线的方向信息");
            }

            var direction = line.Direction;

            // 根据尺寸类型选择处理方式
            if (dimension.Segments.IsEmpty)
            {
                // 简单尺寸标注
                var newPosition = ComputeLeaderPosition(direction, dimension.Origin);
                dimension.LeaderEndPosition = newPosition;
            }
            else
            {
                // 多线段尺寸标注
                MoveMultiSegmentDimension(dimension, direction);
            }
        }

        /// <summary>
        /// 移动多线段尺寸标注的引线
        /// </summary>
        private void MoveMultiSegmentDimension(Dimension dimension, XYZ direction)
        {
            var segments = dimension.Segments;

            for (int i = 0; i < segments.Size; i++)
            {
                var segment = segments.get_Item(i);
                var newPosition = ComputeLeaderPosition(direction, segment.Origin);
                segment.LeaderEndPosition = newPosition;
            }
        }

        /// <summary>
        /// 计算引线的新位置
        /// </summary>
        /// <param name="direction">尺寸线方向向量</param>
        /// <param name="origin">原始点位置</param>
        /// <returns>计算后的新位置</returns>
        private XYZ ComputeLeaderPosition(XYZ direction, XYZ origin)
        {
            // 沿方向向量移动指定距离
            var offset = direction.Multiply(Delta);
            return origin.Add(offset);
        }
    }
}
