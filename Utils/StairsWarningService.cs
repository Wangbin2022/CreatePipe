using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    public class StairsWarningAnalysisResult
    {
        //实际楼梯踏板深度小于在楼梯类型中指定的最小踏板深度
        public HashSet<ElementId> minDepthOverpassIds { get; set; } = new HashSet<ElementId>();
        //楼梯子图元未连接。这可能会导致表示和注释不正确。
        public HashSet<ElementId> unConnectedUnitsIds { get; set; } = new HashSet<ElementId>();
        //楼梯顶端超过或无法达到楼梯的顶部高程。请使用控件在顶端添加/删除踢面，或在“属性”选项板上修改楼梯梯段的“相对顶部高度”参数。
        public HashSet<ElementId> stairsTopEndExceedsOrNotReachIds { get; set; } = new HashSet<ElementId>();
        //所有有问题的ID集合（去重）
        public HashSet<ElementId> AllProblemStairsIds
        {
            get
            {
                var allIds = new HashSet<ElementId>(minDepthOverpassIds);
                allIds.UnionWith(unConnectedUnitsIds);
                allIds.UnionWith(stairsTopEndExceedsOrNotReachIds);
                return allIds;
            }
        }
        public bool HasAnyWarnings => AllProblemStairsIds.Count > 0;
    }
    public class StairsWarningService
    {
        private readonly Document _doc;
        public StairsWarningService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }
        public StairsWarningAnalysisResult AnalyzeStairsWarnings()
        {
            var result = new StairsWarningAnalysisResult();
            // 1. 获取所有未解决的警告
            IList<FailureMessage> allWarnings = _doc.GetWarnings();
            //实际楼梯踏板深度小于在楼梯类型中指定的最小踏板深度。
            FailureDefinitionId minDepthOverpassId = BuiltInFailures.StairRampFailures.StairsActualTreadDepthLessThanMinimumFailure;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == minDepthOverpassId))
            {
                foreach (ElementId failingElementId in warning.GetFailingElements())
                {
                    if (_doc.GetElement(failingElementId) is Stairs stair)
                    {
                        result.minDepthOverpassIds.Add(failingElementId);
                    }
                }
            }
            //楼梯子图元未连接。这可能会导致表示和注释不正确。
            FailureDefinitionId unConnectedUnitsId = BuiltInFailures.StairRampFailures.StairsSubElementsNotConnectedWarning;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == unConnectedUnitsId))
            {
                foreach (ElementId failingElementId in warning.GetFailingElements())
                {
                    if (_doc.GetElement(failingElementId) is Stairs stair)
                    {
                        result.unConnectedUnitsIds.Add(failingElementId);
                    }
                }
            }
            //楼梯顶端超过或无法达到楼梯的顶部高程。请使用控件在顶端添加/删除踢面，或在“属性”选项板上修改楼梯梯段的“相对顶部高度”参数。
            FailureDefinitionId stairsTopEndExceedsOrNotReachId = BuiltInFailures.StairRampFailures.StairsTopEndExceedsOrNotReachWarning;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == stairsTopEndExceedsOrNotReachId))
            {
                foreach (ElementId failingElementId in warning.GetFailingElements())
                {
                    if (_doc.GetElement(failingElementId) is Stairs stair)
                    {
                        result.stairsTopEndExceedsOrNotReachIds.Add(failingElementId);
                    }
                }
            }
            return result;
        }
        public bool HasWarningsForStairs(ElementId stairId, StairsWarningAnalysisResult allWarningsResult = null)
        {
            if (stairId == null || stairId == ElementId.InvalidElementId) return false;
            if (allWarningsResult == null)
            {
                // 如果没有提供预计算结果，则重新分析整个文档 (效率较低，不推荐循环调用)
                allWarningsResult = AnalyzeStairsWarnings();
            }
            return allWarningsResult.AllProblemStairsIds.Contains(stairId);
        }
    }
}
