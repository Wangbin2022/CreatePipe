using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 重复标记/类型标记警告的分析结果
    /// </summary>
    public class DuplicateMarkAnalysisResult
    {
        /// <summary>
        /// 具有重复标记值的图元ID集合
        /// </summary>
        public HashSet<ElementId> DuplicateElementIds { get; set; } = new HashSet<ElementId>();

        /// <summary>
        /// 是否存在重复标记的警告
        /// </summary>
        public bool HasAnyWarnings => DuplicateElementIds.Count > 0;
    }
    //服务来专门过滤和提取 DuplicateValue 类型的警告。
    /// <summary>
    /// 处理重复标记/类型标记警告的服务
    /// </summary>
    public class DuplicateMarkWarningService
    {
        private readonly Document _doc;
        public DuplicateMarkWarningService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }
        public DuplicateMarkAnalysisResult AnalyzeWarnings()
        {
            var result = new DuplicateMarkAnalysisResult();
            IList<FailureMessage> allWarnings = _doc.GetWarnings();
            // Revit 中“标记”或“类型标记”重复通常使用这个 FailureDefinitionId
            FailureDefinitionId duplicateValueId = BuiltInFailures.GeneralFailures.DuplicateValue;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == duplicateValueId))
            {
                // 获取涉及此警告的所有图元
                ICollection<ElementId> failingElements = warning.GetFailingElements();
                if (failingElements != null)
                {
                    result.DuplicateElementIds.UnionWith(failingElements);
                }
            }
            return result;
        }
    }
}
