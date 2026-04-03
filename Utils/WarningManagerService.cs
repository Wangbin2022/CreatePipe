using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 重叠警告分析结果 DTO
    /// </summary>
    public class OverlapAnalysisResult
    {
        public int TotalWarningsAnalyzed { get; set; }
        public HashSet<ElementId> ElementsToKeep { get; set; } = new HashSet<ElementId>();
        public HashSet<ElementId> ElementsToDelete { get; set; } = new HashSet<ElementId>();
        public bool HasOverlaps => ElementsToDelete.Count > 0;
    }
    /// <summary>
    /// 警告管理与处理服务
    /// </summary>
    public class WarningManagerService
    {
        private readonly Document _doc;
        public WarningManagerService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }
        /// <summary>
        /// 获取文档中指定类型的未解决警告
        /// </summary>
        public IList<FailureMessage> GetWarningsByType(params FailureDefinitionId[] targetFailureIds)
        {
            var targetIds = new HashSet<FailureDefinitionId>(targetFailureIds);
            return _doc.GetWarnings().Where(w => targetIds.Contains(w.GetFailureDefinitionId())).ToList();
        }
        /// <summary>
        /// 分析重叠警告，计算出哪些该保留，哪些该删除（不执行实际删除）
        /// </summary>
        public OverlapAnalysisResult AnalyzeOverlaps(IList<FailureMessage> overlapWarnings)
        {
            var result = new OverlapAnalysisResult { TotalWarningsAnalyzed = overlapWarnings.Count };
            foreach (var warning in overlapWarnings)
            {
                var failingElements = warning.GetFailingElements();
                if (failingElements.Count < 2) continue; // 小于2个构件构不成重叠
                // 核心逻辑：处理连锁重叠 (A重叠B, B重叠C)
                // 检查当前这个警告组里，是不是已经有一个构件被我们标记为“保留”了？
                var alreadyKeptElement = failingElements.FirstOrDefault(id => result.ElementsToKeep.Contains(id));
                if (alreadyKeptElement == null)
                {
                    // 这个重叠组是全新的，保留第一个，剩下的全删
                    var keeper = failingElements.First();
                    result.ElementsToKeep.Add(keeper);

                    foreach (var id in failingElements.Skip(1))
                    {
                        result.ElementsToDelete.Add(id);
                    }
                }
                else
                {
                    // 这个组里已经有一个被保留了，那么当前组里除了保留的那个，其他全删
                    foreach (var id in failingElements.Where(e => e != alreadyKeptElement))
                    {
                        result.ElementsToDelete.Add(id);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 执行清理操作：解锁并删除指定的元素
        /// </summary>
        /// <returns>成功删除的数量</returns>
        public int ExecuteCleanup(HashSet<ElementId> elementsToDelete)
        {
            if (elementsToDelete == null || elementsToDelete.Count == 0) return 0;
            using (Transaction t = new Transaction(_doc, "清理重叠构件"))
            {
                t.Start();
                // 1. 解除锁定 (Pinned) 状态，否则删除会报错回滚
                foreach (var id in elementsToDelete)
                {
                    var elem = _doc.GetElement(id);
                    if (elem != null && elem.Pinned)
                    {
                        elem.Pinned = false;
                    }
                }
                // 2. 批量删除
                var deletedIds = _doc.Delete(elementsToDelete.ToList());
                t.Commit();
                return deletedIds.Count;
            }
        }
    }
}
