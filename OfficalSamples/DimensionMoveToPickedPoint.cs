using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class DimensionMoveToPickedPoint
    {
        public DimensionMoveToPickedPoint(ExternalCommandData commandData)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            string message = string.Empty;
            try
            {
                // 获取当前选中的元素ID列表
                var selectedIds = uidoc.Selection.GetElementIds();
                // 验证是否有选中的元素
                if (!selectedIds.Any())
                {
                    message = NO_SELECTION_MESSAGE;
                    TaskDialog.Show("Revit", message);
                    return;
                }
                // 筛选出尺寸标注元素
                var dimensions = selectedIds.Select(id => doc.GetElement(id))
                    .OfType<Dimension>().ToList();
                // 验证是否有有效的尺寸标注
                if (!dimensions.Any())
                {
                    message = NO_DIMENSION_SELECTED;
                    TaskDialog.Show("Revit", message);
                    return;
                }
                // 拾取目标点
                var targetPoint = PickPoint(uidoc);
                if (targetPoint == null)
                {
                    message = "未拾取到有效的点位置"; return;
                }
                // 批量处理所有选中的尺寸标注
                var result = ProcessDimensions(doc, dimensions, targetPoint);
                message = result.Message;
                //return result.Status;
            }
            catch (Exception ex)
            {
                message = $"执行失败：{ex.Message}";
                return;
            }
        }
        private const string TRANSACTION_NAME = "移动尺寸标注引线端点";
        private const string NO_SELECTION_MESSAGE = "请至少选中一个尺寸标注元素";
        private const string NO_DIMENSION_SELECTED = "选中的元素中没有有效的尺寸标注";
        private const string PICK_POINT_PROMPT = "请拾取引线端点的新位置";
        private const string OPERATION_FAILED = "设置引线端点失败: {0}";
        /// <summary>
        /// 拾取点位置
        /// </summary>
        private XYZ PickPoint(UIDocument uidoc)
        {
            try
            {
                // 允许捕捉多种对象类型
                var snapTypes = ObjectSnapTypes.Endpoints |
                                ObjectSnapTypes.Intersections |
                                ObjectSnapTypes.Centers |
                                ObjectSnapTypes.Nearest |
                                ObjectSnapTypes.Perpendicular |
                                ObjectSnapTypes.Midpoints;

                return uidoc.Selection.PickPoint(snapTypes, PICK_POINT_PROMPT);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户取消了拾取操作
                return null;
            }
        }

        /// <summary>
        /// 处理尺寸标注列表
        /// </summary>
        private (Result Status, string Message) ProcessDimensions(Document doc, List<Dimension> dimensions, XYZ targetPoint)
        {
            var successCount = 0;
            var failedList = new List<string>();

            using (var transaction = new Transaction(doc, TRANSACTION_NAME))
            {
                transaction.Start();

                foreach (var dimension in dimensions)
                {
                    try
                    {
                        SetLeaderEndPosition(dimension, targetPoint);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedList.Add($"尺寸标注 ID {dimension.Id.IntegerValue}: {ex.Message}");
                    }
                }

                if (failedList.Any())
                {
                    transaction.RollBack();
                    var errorMessage = string.Join("\n", failedList);
                    TaskDialog.Show("错误", $"部分尺寸标注处理失败：\n{errorMessage}");
                    return (Result.Failed, errorMessage);
                }

                transaction.Commit();
            }

            var resultMessage = $"成功移动 {successCount} 个尺寸标注的引线端点";
            TaskDialog.Show("完成", resultMessage);
            return (Result.Succeeded, resultMessage);
        }

        /// <summary>
        /// 设置尺寸标注的引线端点位置
        /// </summary>
        private void SetLeaderEndPosition(Dimension dimension, XYZ targetPoint)
        {
            // 检查尺寸标注是否有引线
            if (!dimension.HasLeader)
            {
                throw new InvalidOperationException("该尺寸标注没有引线");
            }

            // 根据尺寸标注的线段数量采用不同的处理方式
            if (dimension.Segments.IsEmpty)
            {
                // 简单尺寸标注：直接设置引线端点
                dimension.LeaderEndPosition = targetPoint;
            }
            else
            {
                // 多线段尺寸标注：依次设置各线段的引线端点
                SetMultiSegmentLeaderPositions(dimension, targetPoint);
            }
        }

        /// <summary>
        /// 设置多线段尺寸标注的引线端点位置
        /// </summary>
        private void SetMultiSegmentLeaderPositions(Dimension dimension, XYZ targetPoint)
        {
            var segments = dimension.Segments;
            var segmentCount = segments.Size;

            if (segmentCount < 2)
            {
                // 只有一个线段时，与简单尺寸标注处理方式相同
                dimension.LeaderEndPosition = targetPoint;
                return;
            }

            // 计算相邻线段原点之间的偏移向量
            var firstOrigin = segments.get_Item(0).Origin;
            var secondOrigin = segments.get_Item(1).Origin;
            var offsetVector = secondOrigin.Subtract(firstOrigin);

            // 为每个线段设置引线端点（依次偏移）
            var currentOffset = XYZ.Zero;

            for (int i = 0; i < segmentCount; i++)
            {
                var segment = segments.get_Item(i);
                segment.LeaderEndPosition = targetPoint.Add(currentOffset);
                currentOffset = currentOffset.Add(offsetVector);
            }
        }
    }

    /// <summary>
    /// 扩展方法类 - 提供尺寸标注相关的扩展功能
    /// </summary>
    public static class DimensionExtensions
    {
        /// <summary>
        /// 判断尺寸标注是否有引线
        /// </summary>
        public static bool HasLeader(this Dimension dimension)
        {
            if (dimension == null) return false;

            // 检查是否有引线端点位置（非零表示有引线）
            return dimension.LeaderEndPosition != null &&
                   !dimension.LeaderEndPosition.IsAlmostEqualTo(XYZ.Zero);
        }
        /// <summary>
        /// 获取尺寸标注的所有线段原点
        /// </summary>
        public static IList<XYZ> GetSegmentOrigins(this Dimension dimension)
        {
            var origins = new List<XYZ>();

            if (dimension.Segments.IsEmpty) return origins;

            for (int i = 0; i < dimension.Segments.Size; i++)
            {
                origins.Add(dimension.Segments.get_Item(i).Origin);
            }

            return origins;
        }
        /// <summary>
        /// 获取尺寸标注的信息摘要（用于调试）
        /// </summary>
        public static string GetInfoSummary(this Dimension dimension)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"尺寸标注 ID: {dimension.Id.IntegerValue}");
            info.AppendLine($"类型: {dimension.GetType().Name}");
            info.AppendLine($"是否有引线: {dimension.HasLeader()}");
            info.AppendLine($"线段数量: {dimension.Segments.Size}");
            if (!dimension.Segments.IsEmpty)
            {
                info.AppendLine("线段原点:");
                var origins = dimension.GetSegmentOrigins();
                for (int i = 0; i < origins.Count; i++)
                {
                    info.AppendLine($"  [{i}]: ({origins[i].X:F2}, {origins[i].Y:F2}, {origins[i].Z:F2})");
                }
            }
            if (dimension.HasLeader)
            {
                info.AppendLine($"当前引线端点: ({dimension.LeaderEndPosition.X:F2}, " +
                               $"{dimension.LeaderEndPosition.Y:F2}, {dimension.LeaderEndPosition.Z:F2})");
            }
            return info.ToString();
        }
    }
}
