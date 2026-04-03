using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 通用警告分析结果
    /// </summary>
    public class GenericWarningAnalysisResult
    {
        /// <summary>
        /// 按警告描述文本分组的警告信息。
        /// Key: 警告的描述文本 (e.g., "高亮显示的墙重叠")
        /// Value: 与该警告相关的所有元素ID
        /// </summary>
        public Dictionary<string, HashSet<ElementId>> WarningsByDescription { get; } = new Dictionary<string, HashSet<ElementId>>();

        /// <summary>
        /// 所有有问题的元素ID集合（去重）
        /// </summary>
        public HashSet<ElementId> AllProblemElementIds
        {
            get
            {
                var allIds = new HashSet<ElementId>();
                foreach (var idSet in WarningsByDescription.Values)
                {
                    allIds.UnionWith(idSet);
                }
                return allIds;
            }
        }

        public bool HasAnyWarnings => WarningsByDescription.Any();
    }
    //这个服务将扫描所有警告，并排除那些已经被其他特定服务处理过的警告。
    public class GenericWarningService
    {
        private readonly Document _doc;

        public GenericWarningService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        /// <summary>
        /// 分析所有通用警告。
        /// </summary>
        /// <param name="idsToExclude">需要排除的警告类型ID，这些通常由专门的服务处理。</param>
        /// <returns>通用警告的分析结果。</returns>
        public GenericWarningAnalysisResult AnalyzeGenericWarnings(IEnumerable<FailureDefinitionId> idsToExclude = null)
        {
            var result = new GenericWarningAnalysisResult();
            var exclusionSet = new HashSet<FailureDefinitionId>(idsToExclude ?? Enumerable.Empty<FailureDefinitionId>());

            IList<FailureMessage> allWarnings = _doc.GetWarnings();

            foreach (FailureMessage warning in allWarnings)
            {
                // 如果这个警告类型在排除列表中，则跳过
                if (exclusionSet.Contains(warning.GetFailureDefinitionId()))
                {
                    continue;
                }

                string description = warning.GetDescriptionText();
                if (string.IsNullOrEmpty(description))
                {
                    description = "未分类的警告";
                }

                if (!result.WarningsByDescription.ContainsKey(description))
                {
                    result.WarningsByDescription[description] = new HashSet<ElementId>();
                }

                // 添加与此警告相关的所有元素ID
                ICollection<ElementId> failingElements = warning.GetFailingElements();
                if (failingElements != null && failingElements.Any())
                {
                    result.WarningsByDescription[description].UnionWith(failingElements);
                }
            }

            return result;
        }
    }
}
