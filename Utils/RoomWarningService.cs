using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 房间警告分析结果DTO
    /// </summary>
    public class RoomWarningAnalysisResult
    {
        /// <summary>
        /// 存在“房间不在完全闭合的区域”警告的房间ID集合
        /// </summary>
        public HashSet<ElementId> UnenclosedRoomIds { get; set; } = new HashSet<ElementId>();
        /// <summary>
        /// 存在“多个房间位于同一闭合区域中”警告的房间ID集合
        /// </summary>
        public HashSet<ElementId> RoomsInSameRegionIds { get; set; } = new HashSet<ElementId>();
        /// <summary>
        /// 所有有问题的房间ID集合（去重）
        /// </summary>
        public HashSet<ElementId> AllProblemRoomIds
        {
            get
            {
                var allIds = new HashSet<ElementId>(UnenclosedRoomIds);
                allIds.UnionWith(RoomsInSameRegionIds);
                return allIds;
            }
        }
        /// <summary>
        /// 是否存在任何房间相关的警告
        /// </summary>
        public bool HasAnyWarnings => UnenclosedRoomIds.Count > 0 || RoomsInSameRegionIds.Count > 0;
    }
    /// <summary>
    /// 房间警告处理服务
    /// </summary>
    public class RoomWarningService
    {
        private readonly Document _doc;
        public RoomWarningService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }
        /// <summary>
        /// 分析文档中所有房间相关的警告。
        /// </summary>
        /// <returns>包含不同类型警告中涉及的房间ID的分析结果。</returns>
        public RoomWarningAnalysisResult AnalyzeRoomWarnings()
        {
            var result = new RoomWarningAnalysisResult();
            // 1. 获取所有未解决的警告
            IList<FailureMessage> allWarnings = _doc.GetWarnings();
            // 2. 过滤并处理“房间不在完全闭合的区域”警告
            FailureDefinitionId roomNotEnclosedId = BuiltInFailures.RoomFailures.RoomNotEnclosed;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == roomNotEnclosedId))
            {
                foreach (ElementId failingElementId in warning.GetFailingElements())
                {
                    // 验证是否确实是房间
                    if (_doc.GetElement(failingElementId) is Room room)
                    {
                        result.UnenclosedRoomIds.Add(failingElementId);
                    }
                }
            }
            // 3. 过滤并处理“多个房间位于同一闭合区域中”警告
            FailureDefinitionId roomsInSameRegionId = BuiltInFailures.RoomFailures.RoomsInSameRegionRooms;
            foreach (FailureMessage warning in allWarnings.Where(w => w.GetFailureDefinitionId() == roomsInSameRegionId))
            {
                foreach (ElementId failingElementId in warning.GetFailingElements())
                {
                    // 验证是否确实是房间
                    if (_doc.GetElement(failingElementId) is Room room)
                    {
                        result.RoomsInSameRegionIds.Add(failingElementId);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 针对指定的房间 ElementId 检查是否存在警告。
        /// 注意：此方法效率不如 AnalyzeRoomWarnings() 检查所有房间。
        /// 如果你需要检查一个特定的房间，建议先调用 AnalyzeRoomWarnings() 得到所有问题房间的列表，
        /// 然后再判断你的房间是否在列表中。
        /// </summary>
        /// <param name="roomId">要检查的房间的ElementId。</param>
        /// <param name="allWarningsResult">可选：预先计算好的所有房间警告分析结果，可提高效率。</param>
        /// <returns>如果指定房间存在任一上述警告，则返回true。</returns>
        public bool HasWarningsForRoom(ElementId roomId, RoomWarningAnalysisResult allWarningsResult = null)
        {
            if (roomId == null || roomId == ElementId.InvalidElementId) return false;
            if (allWarningsResult == null)
            {
                // 如果没有提供预计算结果，则重新分析整个文档 (效率较低，不推荐循环调用)
                allWarningsResult = AnalyzeRoomWarnings();
            }
            return allWarningsResult.AllProblemRoomIds.Contains(roomId);
        }
    }
}
