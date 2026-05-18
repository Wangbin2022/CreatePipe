using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class DimensionCleaner
    {
        public DimensionCleaner(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                // 获取当前文档和选择集
                var uiDoc = commandData.Application.ActiveUIDocument;
                var doc = uiDoc.Document;

                // 使用C#7.0的模式匹配和属性表达式获取选中元素
                var selectedElementIds = uiDoc.Selection.GetElementIds();

                // 验证是否有选中的元素
                if (selectedElementIds == null || selectedElementIds.Count == 0)
                {
                    message = NO_SELECTION_MESSAGE;
                    return;
                }

                // 使用LINQ筛选未固定的尺寸标注
                var unpinnedDimensions = selectedElementIds
                    .Select(id => doc.GetElement(id))
                    .OfType<Dimension>()          // 筛选尺寸标注类型
                    .Where(dim => !dim.Pinned)    // 筛选未固定的
                    .ToList();

                // 验证是否有符合条件的尺寸标注
                if (!unpinnedDimensions.Any())
                {
                    message = NO_UNPINNED_DIMENSIONS_MESSAGE;
                    return;
                }

                // 执行删除操作
                var deleteCount = DeleteDimensions(doc, unpinnedDimensions);

                // 设置成功消息
                message = string.Format(SUCCESS_MESSAGE, deleteCount);
            }
            catch (Exception ex)
            {
                message = $"执行失败：{ex.Message}";
                return;
            }
        }
        private const string SUCCESS_MESSAGE = "成功删除 {0} 个未固定的尺寸标注";
        private const string NO_SELECTION_MESSAGE = "请先选中需要删除的尺寸标注";
        private const string NO_UNPINNED_DIMENSIONS_MESSAGE = "选中的元素中没有未固定的尺寸标注";
        private const string TRANSACTION_NAME = "批量删除未固定尺寸标注";
        /// <summary>
        /// 批量删除尺寸标注
        /// </summary>
        /// <param name="document">Revit文档</param>
        /// <param name="dimensions">待删除的尺寸标注列表</param>
        /// <returns>成功删除的数量</returns>
        private int DeleteDimensions(Document document, List<Dimension> dimensions)
        {
            using (var transaction = new Transaction(document, TRANSACTION_NAME))
            {
                transaction.Start();

                // 批量删除元素
                var elementIds = dimensions.Select(d => d.Id).ToList();
                document.Delete(elementIds);

                transaction.Commit();

                return dimensions.Count;
            }
        }

        /// <summary>
        /// 获取选中元素的描述信息（用于日志/调试）
        /// </summary>
        private string GetSelectionSummary(IEnumerable<Dimension> dimensions)
        {
            var count = dimensions.Count();
            var pinnedCount = dimensions.Count(d => d.Pinned);
            var unpinnedCount = count - pinnedCount;

            return $"共选中 {count} 个尺寸标注，其中已固定 {pinnedCount} 个，未固定 {unpinnedCount} 个";
        }
    }
    /// <summary>
    /// 扩展方法类 - 提供LINQ风格的操作
    /// </summary>
    public static partial class DocumentExtensions
    {
        /// <summary>
        /// 批量删除元素（C#7.0扩展方法）
        /// </summary>
        public static void DeleteElements(this Document doc, IEnumerable<Element> elements)
        {
            if (elements == null || !elements.Any()) return;

            var ids = elements.Select(e => e.Id).ToList();
            doc.Delete(ids);
        }
    }

    /// <summary>
    /// 命令配置类 - 存储命令相关配置
    /// </summary>
    public static class CommandConfig
    {
        /// <summary>是否显示详细日志</summary>
        public static bool ShowDetailedLog { get; set; } = false;

        /// <summary>是否在删除前确认</summary>
        public static bool RequireConfirmation { get; set; } = false;
    }
}
