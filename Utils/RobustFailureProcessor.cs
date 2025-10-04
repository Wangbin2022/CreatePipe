using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    public class RobustFailureProcessor : IFailuresPreprocessor
    {
        private List<string> _errorMessages;
        private List<string> _resolvedMessages;
        private bool _hasUnresolvableError;
        public bool HasError => _hasUnresolvableError;
        public bool HasAnyFailures => _errorMessages.Count > 0 || _resolvedMessages.Count > 0;
        public RobustFailureProcessor()
        {
            _errorMessages = new List<string>();
            _resolvedMessages = new List<string>();
            _hasUnresolvableError = false;
        }
        public string GetAggregatedErrorMessages()
        {
            var messages = new List<string>();
            if (_errorMessages.Count > 0)
            {
                messages.Add("未解决的错误:");
                messages.AddRange(_errorMessages.Select(msg => "  • " + msg));
            }
            if (_resolvedMessages.Count > 0)
            {
                messages.Add("已自动解决的问题:");
                messages.AddRange(_resolvedMessages.Select(msg => "  • " + msg));
            }
            return string.Join(Environment.NewLine, messages);
        }
        public FailureProcessingResult PreprocessFailures(FailuresAccessor fa)
        {
            IList<FailureMessageAccessor> failures = fa.GetFailureMessages();
            if (failures.Count == 0)
            {
                return FailureProcessingResult.Continue;
            }
            bool shouldRollback = false;
            foreach (FailureMessageAccessor failure in failures)
            {
                FailureSeverity severity = failure.GetSeverity();
                string failureDescription = failure.GetDescriptionText();

                // --- 处理警告 ---
                if (severity == FailureSeverity.Warning)
                {
                    fa.DeleteWarning(failure);
                }
                // --- 处理错误 ---
                else if (severity == FailureSeverity.Error)
                {
                    // 检查是否是可以安全自动解决的特定错误
                    if (IsResolvableError(failure.GetFailureDefinitionId()))
                    {
                        try
                        {
                            fa.ResolveFailure(failure);
                            _resolvedMessages.Add($"已解决: {failureDescription}");
                            // 注意：这里不设置 shouldRollback = true，因为错误已经被解决
                        }
                        catch (Exception ex)
                        {
                            // 如果解决失败，将其视为不可解决的错误
                            _errorMessages.Add($"解决失败: {failureDescription} (解决时出错: {ex.Message})");
                            _hasUnresolvableError = true;
                            shouldRollback = true;
                        }
                    }
                    else
                    {
                        // 不可解决的错误
                        _errorMessages.Add(failureDescription);
                        _hasUnresolvableError = true;
                        shouldRollback = true;
                    }
                }
            }
            // 根据是否有不可解决的错误来决定最终操作
            if (shouldRollback)
            {
                return FailureProcessingResult.ProceedWithRollBack;
            }
            else
            {
                return FailureProcessingResult.Continue;
            }
        }
        /// <summary>
        /// 判断指定的错误是否可以安全自动解决
        /// </summary>
        private bool IsResolvableError(FailureDefinitionId failureId)
        {
            // 管线断开和反向连接管件错误
            return failureId == BuiltInFailures.PipingFailures.DuctPipeModified;

            // 如果将来需要处理更多类型的错误，可以在这里添加
            // || failureId == BuiltInFailures.SomeOtherFailures.SomeSpecificError;
        }
    }
}
