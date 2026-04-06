using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 碰撞检测结果封装类
    /// </summary>
    public class ClashDetectionResult
    {
        /// <summary> 检测是否成功执行（不代表是否有碰撞） </summary>
        public bool IsExecuted { get; private set; }
        /// <summary> 是否发现碰撞 </summary>
        public bool HasClash => IsExecuted && ClashElements.Count > 0;
        /// <summary> 碰撞数量 </summary>
        public int ClashCount => ClashElements.Count;
        /// <summary> 被检测的主体元素 </summary>
        public Element TargetElement { get; private set; }
        /// <summary> 与主体发生碰撞的所有元素 </summary>
        public List<Element> ClashElements { get; private set; } = new List<Element>();
        /// <summary> 所有碰撞元素的Id 列表 </summary>
        public List<ElementId> ClashElementIds => ClashElements.Select(e => e.Id).ToList();
        /// <summary> 失败或异常时的错误信息 </summary>
        public string ErrorMessage { get; private set; }
        private ClashDetectionResult() { }
        public static ClashDetectionResult Success(Element target, List<Element> clashElements)
        {
            return new ClashDetectionResult
            {
                IsExecuted = true,
                TargetElement = target,
                ClashElements = clashElements ?? new List<Element>()
            };
        }
        public static ClashDetectionResult Failed(string errorMessage)
        {
            return new ClashDetectionResult
            {
                IsExecuted = false,
                ErrorMessage = errorMessage
            };
        }
        /// <summary>
        /// 生成人类可读的结果摘要
        /// </summary>
        public string GetSummary()
        {
            if (!IsExecuted)
                return $"检测失败：{ErrorMessage}";
            if (!HasClash)
                return $"✅ 未发现碰撞（已检测元素：{TargetElement?.Name}）";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"⚠️ 发现碰撞 {ClashCount} 处（主体：{TargetElement?.Name}）");
            foreach (var elem in ClashElements)
            {
                sb.AppendLine($"  · [{elem.Category?.Name}] {elem.Name} (Id: {elem.Id.IntegerValue})");
            }
            return sb.ToString();
        }
    }
    /// <summary>
    /// 元素合法性验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }
        public static ValidationResult Valid()
            => new ValidationResult { IsValid = true };
        public static ValidationResult Invalid(string message)
            => new ValidationResult { IsValid = false, Message = message };
    }
    /// <summary>
    /// 碰撞检测服务类，负责所有碰撞检测的核心逻辑
    /// </summary>
    public class ClashDetectionService
    {
        private readonly Document _doc;
        public ClashDetectionService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }
        /// <summary>
        /// 检测单个元素与全图所有元素的碰撞
        /// </summary>
        /// <param name="targetElement">被检测的主体元素</param>
        /// <param name="excludeIds">额外需要排除的元素Id列表（主体自身会自动排除）</param>
        /// <returns>碰撞检测结果</returns>
        public ClashDetectionResult DetectClash(Element targetElement, List<ElementId> excludeIds = null)
        {
            if (targetElement == null)
                return ClashDetectionResult.Failed("目标元素为空");
            try
            {
                // 构建排除列表：主体自身 + 用户指定的排除项
                var finalExcludes = new List<ElementId> { targetElement.Id };
                if (excludeIds != null)
                    finalExcludes.AddRange(excludeIds);
                // 碰撞过滤器（false = 不反转，即找"确实相交"的）
                var intersectFilter = new ElementIntersectsElementFilter(targetElement, false);
                var clashElements = new FilteredElementCollector(_doc).WherePasses(intersectFilter)
                    .Excluding(finalExcludes).ToElements().ToList();
                return ClashDetectionResult.Success(targetElement, clashElements);
            }
            catch (Exception ex)
            {
                return ClashDetectionResult.Failed($"碰撞检测时发生异常：{ex.Message}");
            }
        }
        /// <summary>
        /// 检测单个元素与指定范围元素的碰撞（例如只检测结构柱、梁）
        /// </summary>
        /// <param name="targetElement">被检测的主体元素</param>
        /// <param name="candidateElements">参与碰撞检测的候选元素集合</param>
        public ClashDetectionResult DetectClashInScope(Element targetElement, IEnumerable<Element> candidateElements)
        {
            if (targetElement == null)
                return ClashDetectionResult.Failed("目标元素为空"); if (candidateElements == null)
                return ClashDetectionResult.Failed("候选元素集合为空");
            try
            {
                var intersectFilter = new ElementIntersectsElementFilter(targetElement, false);
                var candidateIds = candidateElements.Where(e => e.Id != targetElement.Id)
                    .Select(e => e.Id).ToList();
                if (candidateIds.Count == 0)
                    return ClashDetectionResult.Success(targetElement, new List<Element>());
                var clashElements = new FilteredElementCollector(_doc, candidateIds)
                    .WherePasses(intersectFilter).ToElements().ToList();
                return ClashDetectionResult.Success(targetElement, clashElements);
            }
            catch (Exception ex)
            {
                return ClashDetectionResult.Failed($"碰撞检测时发生异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量检测多个元素各自与全图的碰撞（返回每个元素各自的碰撞结果）
        /// </summary>
        public List<ClashDetectionResult> DetectClashBatch(IEnumerable<Element> targetElements)
        {
            var results = new List<ClashDetectionResult>();
            foreach (var element in targetElements)
            {
                results.Add(DetectClash(element));
            }
            return results;
        }
        // =========================================================
        // 元素合法性验证
        // =========================================================
        /// <summary>
        /// 验证元素是否支持碰撞检测
        /// 支持：MEP管线（排除保温层）、族实例、墙、板、结构柱梁
        /// </summary>
        public static ValidationResult ValidateElement(Element element)
        {
            if (element == null)
                return ValidationResult.Invalid("元素为空");
            // 明确不支持的类型
            if (element is InsulationLiningBase)
                return ValidationResult.Invalid("保温/衬里层不参与碰撞检测");
            // 明确支持的类型（可按需扩展）
            bool isSupported =
                element is MEPCurve ||// 管道、风管、桥架等
                element is FamilyInstance ||  // 所有族实例（管件、阀门、设备等）
                element is Wall ||  // 墙体
                element is Floor ||  // 楼板
                element is RoofBase ||  // 屋顶
                element is Ceiling ||  // 天花板
                (element.Category != null && IsSupportedCategory(element.Category.Id));
            if (!isSupported)
                return ValidationResult.Invalid($"不支持对[{element.Category?.Name ?? "未知类型"}] 进行碰撞检测");
            return ValidationResult.Valid();
        }
        private static bool IsSupportedCategory(ElementId categoryId)
        {
            // 扩展支持的内置类别
            var supportedCategories = new[]
            {
            BuiltInCategory.OST_StructuralColumns,   // 结构柱
            BuiltInCategory.OST_StructuralFraming,   // 结构框架（梁）
            BuiltInCategory.OST_PipeCurves,          // 管道
            BuiltInCategory.OST_DuctCurves,          // 风管
            BuiltInCategory.OST_CableTray,           // 桥架
            BuiltInCategory.OST_Conduit,             // 线管
        };
            return supportedCategories.Any(c => categoryId == new ElementId(c));
        }
    }
}
