using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    //FailurePreProcessor 存在严重风险，不建议在生产环境中使用。
    ////0930 错误处理使用示例
    //using (Transaction trans = new Transaction(doc, "tt"))
    //{
    //    FailureHandlingOptions fho = trans.GetFailureHandlingOptions();
    //    fho.SetFailuresPreprocessor(new FailurePreProcessor());
    //    trans.SetFailureHandlingOptions(fho);
    //    trans.Start();
    //    trans.Commit();
    //}
    public class FailurePreProcessor : IFailuresPreprocessor
    {
        private string failureMessage;
        public string FailureMessage { get => failureMessage; set => failureMessage = value; }
        private bool _error;
        public bool HasError { get => _error; set => _error = value; }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor fa)
        {
            IList<FailureMessageAccessor> lstFma = fa.GetFailureMessages();
            if (lstFma.Count() == 0) return FailureProcessingResult.Continue;
            foreach (FailureMessageAccessor item in lstFma)
            {
                if (item.GetSeverity() == FailureSeverity.Warning)
                {
                    _error = false;
                    fa.DeleteWarning(item);
                }
                else if (item.GetSeverity() == FailureSeverity.Error)
                {
                    if (item.HasResolutions())
                    {
                        fa.ResolveFailure(item);
                        failureMessage = item.GetDescriptionText();
                        _error = true;
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
