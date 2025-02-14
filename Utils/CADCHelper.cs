using Autodesk.Revit.DB;
//using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using System;
using System.IO;

namespace CreatePipe.Utils
{
    public static class CADCHelper
    {


    }
    public static class TransactionHelper
    {
        public static void NewTransaction(this Document doc, Action action, string name = "Default Name", bool rollback = false)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }
            using (Transaction transaction = new Transaction(doc, name))
            {
                if (transaction.Start() == TransactionStatus.Started)
                {
                    action.Invoke();
                    if (!rollback)
                    {
                        if (transaction.Commit() != TransactionStatus.Committed)
                        {
                            TaskDialog.Show("Message", "事务出错误");
                        }
                        return;
                    }
                    transaction.RollBack();
                }
            }
        }
        public static void NewSubTransaction(this Document doc, Action predicate, bool rollback = false)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }
            using (SubTransaction sts = new SubTransaction(doc))
            {
                if (sts.Start() == TransactionStatus.Started)
                {
                    predicate.Invoke();
                    if (!rollback)
                    {
                        if (sts.Commit() != TransactionStatus.Committed)
                        {
                            TaskDialog.Show("Message", "事务出错误");
                        }
                        return;
                    }
                    sts.RollBack();
                }
            }
        }
    }
    public static class TransactionExtension
    {
        public static void IgnoreFailure(this Transaction trans)
        {
            var options = trans.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(new failure_ignore());
        }
    }
    public class failure_ignore : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
    public static class LogHelper
    {
        public static void LogException(Action action, string path)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogWrite(e.ToString(), path);
            }
        }
        public static void LogWrite(string msg, string path, bool append = false)
        {
            StreamWriter sw = new StreamWriter(path, append);
            sw.WriteLine(msg);
            sw.Close();
        }
    }


}
