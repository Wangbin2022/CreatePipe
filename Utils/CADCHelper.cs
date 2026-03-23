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
    /// <summary>
    /// 统一的主事务封装类
    /// </summary>
    public static class NewTransaction
    {
        /// <summary>
        /// 基础事务调用 (Action 无参数)
        /// </summary>
        public static void Execute(Document doc, string transactionName, Action action)
        {
            using (Transaction t = new Transaction(doc, transactionName))
            {
                t.Start();
                try
                {
                    action?.Invoke();
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    throw new Exception($"事务【{transactionName}】执行失败: {ex.Message}", ex);
                }
            }
        }
    }
    /// <summary>
    /// 统一的子事务封装类 (子事务不需要命名)
    /// </summary>
    public static class NewSubTransaction
    {
        public static void Execute(Document doc, Action action)
        {
            using (SubTransaction st = new SubTransaction(doc))
            {
                st.Start();
                try
                {
                    action?.Invoke();
                    st.Commit();
                }
                catch (Exception ex)
                {
                    st.RollBack();
                    throw new Exception($"子事务执行失败: {ex.Message}", ex);
                }
            }
        }
    }
    public static class TransactionWithProgressBarHelper
    {
        /// <summary>
        /// 封装事务 + 进度条生命周期，业务逻辑通过 Action 传入
        /// </summary>
        /// <param name="doc">Revit 文档</param>
        /// <param name="transactionName">事务名称</param>
        /// <param name="totalCount">进度条总数</param>
        /// <param name="initialTitle">进度条初始标题</param>
        /// <param name="action">业务逻辑，参数为已启动的 ProgressBarService 实例</param>
        /// <param name="message">失败时的错误信息输出</param>
        public static Result Execute(Document doc, string transactionName, Action<ProgressBarService> action)
        //public static Result Execute(Document doc, string transactionName, int totalCount, string initialTitle, Action<ProgressBarService> action)
        {
            string message = string.Empty;
            ProgressBarService progressService = new ProgressBarService();
            progressService.Start(1, "准备中...");

            try
            {
                using (Transaction trans = new Transaction(doc, transactionName))
                {
                    trans.Start();
                    action(progressService);
                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message.ToString());
                return Result.Failed;
            }
            finally
            {
                progressService.Stop();
            }
        }
    }
    public static class TransactionHelper
    {
        public static void NewTransaction(this Document doc, Action action, string name = "Default Name", bool rollback = false)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            using (Transaction transaction = new Transaction(doc, name))
            {
                if (transaction.Start() == TransactionStatus.Started)
                {
                    try
                    {
                        action.Invoke();
                        if (rollback)
                        {
                            transaction.RollBack();
                        }
                        else
                        {
                            if (transaction.Commit() != TransactionStatus.Committed)
                            {
                                TaskDialog.Show("Message", "事务提交失败");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 【核心改进】发生异常时强制回滚，防止产生脏数据
                        if (transaction.GetStatus() != TransactionStatus.RolledBack)
                        {
                            transaction.RollBack();
                        }
                        throw; // 继续向上抛出异常，让外层（如你的Helper）能捕获并显示
                    }
                }
            }
        }
        public static void NewSubTransaction(this Document doc, Action action, bool rollback = false)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            using (SubTransaction sts = new SubTransaction(doc))
            {
                if (sts.Start() == TransactionStatus.Started)
                {
                    try
                    {
                        action.Invoke(); // 原名为 predicate，改为 action 更加见名知意
                        if (rollback)
                        {
                            sts.RollBack();
                        }
                        else
                        {
                            if (sts.Commit() != TransactionStatus.Committed)
                            {
                                TaskDialog.Show("Message", "子事务提交失败");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 【核心改进】发生异常时强制回滚
                        if (sts.GetStatus() != TransactionStatus.RolledBack)
                        {
                            sts.RollBack();
                        }
                        throw;
                    }
                }
            }
        }
    }
    //public static class TransactionExtension
    //{
    //    public static void IgnoreFailure(this Transaction trans)
    //    {
    //        var options = trans.GetFailureHandlingOptions();
    //        options.SetFailuresPreprocessor(new failure_ignore());
    //    }
    //}
    //public class failure_ignore : IFailuresPreprocessor
    //{
    //    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    //    {
    //        failuresAccessor.DeleteAllWarnings();
    //        return FailureProcessingResult.Continue;
    //    }
    //}
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
