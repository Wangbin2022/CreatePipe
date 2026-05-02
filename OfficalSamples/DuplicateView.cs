using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    internal class DuplicateView
    {
        /// <summary>
        /// 视图复制工具类 - 提供跨文档复制明细表和草图视图的功能
        /// </summary>
        public static class DuplicateViewUtils
        {
            /// <summary>
            /// 将明细表从一个文档复制到另一个文档
            /// </summary>
            /// <param name="sourceDoc">源文档</param>
            /// <param name="schedules">要复制的明细表集合</param>
            /// <param name="targetDoc">目标文档</param>
            public static void DuplicateSchedules(Document sourceDoc,
                IEnumerable<ViewSchedule> schedules, Document targetDoc)
            {
                // 使用 LINQ 将视图集合转换为 ElementId 列表
                var viewIds = schedules.Cast<View>().Select(v => v.Id).ToList();

                // 执行跨文档复制，不需要返回源元素到目标元素的映射
                DuplicateElementsAcrossDocuments(sourceDoc, viewIds, targetDoc, false);
            }

            /// <summary>
            /// 将草图视图及其内容从一个文档复制到另一个文档
            /// </summary>
            /// <param name="sourceDoc">源文档</param>
            /// <param name="draftingViews">要复制的草图视图集合</param>
            /// <param name="targetDoc">目标文档</param>
            /// <returns>新创建的详图元素数量</returns>
            public static int DuplicateDraftingViews(Document sourceDoc,
                IEnumerable<ViewDrafting> draftingViews, Document targetDoc)
            {
                var detailElementCount = 0;

                // 使用事务组确保所有操作的原子性
                using (var tg = new TransactionGroup(targetDoc, "跨文档复制草图视图"))
                {
                    tg.Start();

                    // 转换视图集合为 ElementId 列表
                    var viewIds = draftingViews.Cast<View>().Select(v => v.Id).ToList();

                    // 复制视图，获取源视图到目标视图的映射关系
                    var viewMap = DuplicateElementsAcrossDocuments(sourceDoc, viewIds, targetDoc, true);

                    // 遍历每个复制的视图，复制其内部详图内容
                    foreach (var kvp in viewMap)
                    {
                        var sourceView = sourceDoc.GetElement(kvp.Key) as View;
                        var targetView = targetDoc.GetElement(kvp.Value) as View;

                        if (sourceView != null && targetView != null)
                        {
                            detailElementCount += DuplicateDetailingAcrossViews(sourceView, targetView);
                        }
                    }

                    tg.Assimilate();
                }

                return detailElementCount;
            }

            /// <summary>
            /// 跨文档复制元素的核心方法
            /// </summary>
            /// <param name="sourceDoc">源文档</param>
            /// <param name="elementIds">要复制的元素ID集合</param>
            /// <param name="targetDoc">目标文档</param>
            /// <param name="needMapping">是否需要返回源元素到目标元素的映射</param>
            /// <returns>元素映射字典（仅当 needMapping 为 true 时有效）</returns>
            private static Dictionary<ElementId, ElementId> DuplicateElementsAcrossDocuments(
                Document sourceDoc, ICollection<ElementId> elementIds,
                Document targetDoc, bool needMapping)
            {
                var elementMap = new Dictionary<ElementId, ElementId>();

                // 配置复制粘贴选项，自动处理重复类型名称
                var copyOptions = new CopyPasteOptions();
                copyOptions.SetDuplicateTypeNamesHandler(new HideAndAcceptDuplicateTypeNamesHandler());

                ICollection<ElementId> copiedIds;

                // 执行复制操作的事务
                using (var transaction = new Transaction(targetDoc, "复制元素"))
                {
                    transaction.Start();

                    // 执行跨文档复制，使用单位变换（位置不变）
                    copiedIds = ElementTransformUtils.CopyElements(
                        sourceDoc, elementIds, targetDoc, Transform.Identity, copyOptions);

                    // 配置失败处理选项，过滤重复类型的警告
                    var failureOptions = transaction.GetFailureHandlingOptions();
                    failureOptions.SetFailuresPreprocessor(new HidePasteDuplicateTypesPreprocessor());
                    transaction.Commit(failureOptions);
                }

                // 如果需要返回元素映射，通过元素名称建立关联
                if (needMapping && copiedIds != null)
                {
                    // 创建源文档元素名称到ID的映射（过滤空名称）
                    var sourceNameMap = elementIds
                        .Select(id => sourceDoc.GetElement(id))
                        .Where(e => !string.IsNullOrEmpty(e?.Name))
                        .ToDictionary(e => e.Name, e => e.Id);

                    // 创建目标文档元素名称到ID的映射
                    var targetNameMap = copiedIds
                        .Select(id => targetDoc.GetElement(id))
                        .Where(e => !string.IsNullOrEmpty(e?.Name))
                        .ToDictionary(e => e.Name, e => e.Id);

                    // 通过名称匹配建立源到目标的映射
                    foreach (var name in sourceNameMap.Keys.Intersect(targetNameMap.Keys))
                    {
                        elementMap[sourceNameMap[name]] = targetNameMap[name];
                    }
                }

                return elementMap;
            }

            /// <summary>
            /// 复制视图中的所有视图特定元素（详图、标注等）
            /// </summary>
            /// <param name="sourceView">源视图</param>
            /// <param name="targetView">目标视图</param>
            /// <returns>新创建的元素数量</returns>
            private static int DuplicateDetailingAcrossViews(View sourceView, View targetView)
            {
                // 收集源视图中的视图特定元素
                var collector = new FilteredElementCollector(sourceView.Document, sourceView.Id);

                // 过滤掉没有类别的元素（如修订表、范围框等不应复制的内容）
                collector.WherePasses(new ElementCategoryFilter(ElementId.InvalidElementId, true));

                var elementsToCopy = collector.ToElementIds();

                if (elementsToCopy.Count == 0) return 0;

                var copyOptions = new CopyPasteOptions();
                copyOptions.SetDuplicateTypeNamesHandler(new HideAndAcceptDuplicateTypeNamesHandler());

                using (var transaction = new Transaction(targetView.Document, "复制视图详图"))
                {
                    transaction.Start();

                    // 从源视图复制元素到目标视图
                    var copiedElements = ElementTransformUtils.CopyElements(
                        sourceView, elementsToCopy, targetView, Transform.Identity, copyOptions);

                    // 配置失败处理，过滤重复类型的警告
                    var failureOptions = transaction.GetFailureHandlingOptions();
                    failureOptions.SetFailuresPreprocessor(new HidePasteDuplicateTypesPreprocessor());
                    transaction.Commit(failureOptions);

                    return copiedElements.Count;
                }
            }
        }

        /// <summary>
        /// 重复类型名称处理器 - 自动使用目标文档中的现有类型
        /// </summary>
        public class HideAndAcceptDuplicateTypeNamesHandler : IDuplicateTypeNamesHandler
        {
            public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args)
            {
                // 自动使用目标文档中的类型，不弹出对话框
                return DuplicateTypeAction.UseDestinationTypes;
            }
        }

        /// <summary>
        /// 失败预处理器 - 过滤掉重复类型粘贴的警告消息
        /// </summary>
        public class HidePasteDuplicateTypesPreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // 遍历所有失败消息
                foreach (var failure in failuresAccessor.GetFailureMessages())
                {
                    // 删除"无法粘贴重复类型"的警告
                    if (failure.GetFailureDefinitionId() == BuiltInFailures.CopyPasteFailures.CannotCopyDuplicates)
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }

                // 继续处理其他类型的错误（弹出对话框）
                return FailureProcessingResult.Continue;
            }
        }
    }
}
