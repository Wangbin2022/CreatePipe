using Autodesk.Revit.DB;
using System;

namespace CreatePipe.Utils
{
    public class FailureHelper : IFailuresProcessor
    {
        public bool HasError { get; private set; } = false;
        public void Dismiss(Document document)
        {
            throw new NotImplementedException();
        }

        public FailureProcessingResult ProcessFailures(FailuresAccessor failuresAccessor)
        {
            var messages = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor accessor in messages)
            {
                FailureSeverity severity = accessor.GetSeverity();
                if (severity == FailureSeverity.Error)
                {
                    HasError = true;
                    return FailureProcessingResult.ProceedWithRollBack;
                }
            }
            return FailureProcessingResult.Continue;//忽略警告的错误捕捉
        }
    }
}
