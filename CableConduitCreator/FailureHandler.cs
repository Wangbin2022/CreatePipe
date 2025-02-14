using Autodesk.Revit.DB;

namespace CreatePipe.CableConduitCreator
{
    public class FailureHandler : IFailuresPreprocessor
    {
        public bool HasError { get; set; } = false;

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var Messages = failuresAccessor.GetFailureMessages();
            foreach (var accessor in Messages)
            {
                var severity = accessor.GetSeverity();
                if (severity == FailureSeverity.Error)
                {
                    HasError = true;
                    return FailureProcessingResult.ProceedWithRollBack;
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
