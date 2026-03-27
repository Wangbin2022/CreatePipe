using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace CreatePipe.Utils
{
    //260326 gemini修改优化后
    /// <summary>
    /// 基于队列的 Revit 外部事件包装器
    /// </summary>
    public class BaseExternalHandler : IDisposable
    {
        private ExternalEventHandler _handler;
        private ExternalEvent _externalEvent;
        private bool _disposed = false;
        public BaseExternalHandler()
        {
            _handler = new ExternalEventHandler();
            _externalEvent = ExternalEvent.Create(_handler);
        }
        /// <summary>
        /// 将任务加入队列并触发 Revit 外部事件
        /// </summary>
        /// <param name="action">要在 Revit API 上下文中执行的代码</param>
        public void Run(Action<UIApplication> action)
        {
            if (_disposed || _handler == null) return;
            // 1. 将任务送入内部处理器的队列
            _handler.EnqueueAction(action);
            // 2. 激活 Revit 外部事件
            _externalEvent.Raise();
        }
        /// <summary>
        /// 实现 IDisposable 接口，释放非托管资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放 ExternalEvent 资源 (Revit API 内部会清理相关句柄)
                    if (_externalEvent != null)
                    {
                        _externalEvent.Dispose();
                        _externalEvent = null;
                    }
                    _handler = null;
                }
                _disposed = true;
            }
        }
        /// <summary>
        /// 内部处理器类：实现队列逻辑
        /// </summary>
        private class ExternalEventHandler : IExternalEventHandler
        {
            // 任务队列
            private readonly Queue<Action<UIApplication>> _actionQueue = new Queue<Action<UIApplication>>();
            // 线程锁，确保在多线程环境下入队和出队安全
            private readonly object _lock = new object();
            /// <summary>
            /// 入队任务
            /// </summary>
            public void EnqueueAction(Action<UIApplication> action)
            {
                lock (_lock)
                {
                    _actionQueue.Enqueue(action);
                }
            }
            /// <summary>
            /// Revit 框架回调的核心方法
            /// </summary>
            public void Execute(UIApplication app)
            {
                // 1. 提取当前队列中所有的任务（为了缩短锁的持有时间）
                List<Action<UIApplication>> tasksToExecute = new List<Action<UIApplication>>();

                lock (_lock)
                {
                    while (_actionQueue.Count > 0)
                    {
                        tasksToExecute.Add(_actionQueue.Dequeue());
                    }
                }
                // 2. 依次执行提取出来的任务
                foreach (var action in tasksToExecute)
                {
                    try
                    {
                        action?.Invoke(app);
                    }
                    catch (Exception ex)
                    {
                        // 记录日志或弹窗提示，不影响后续任务
                        TaskDialog.Show("外部事件错误", $"执行任务时发生异常: {ex.Message}");
                    }
                }
            }
            public string GetName()
            {
                return "Revit Queue-Based External Handler";
            }
        }
    }
    //public class BaseExternalHandler
    //{
    //    private ExternalEventHandler Handler { get; set; }
    //    private ExternalEvent ExternalEvent { get; set; }
    //    public BaseExternalHandler()
    //    {
    //        Handler = new ExternalEventHandler();
    //        ExternalEvent = ExternalEvent.Create(Handler);
    //    }
    //    public void Run(Action<UIApplication> action)
    //    {
    //        Handler.Action = action;
    //        ExternalEvent.Raise();
    //    }
    //    // 实现了IDisposable，以便在窗口关闭时可以安全地释放ExternalEvent资源
    //    public void Dispose()
    //    {
    //        ExternalEvent?.Dispose();
    //        ExternalEvent = null;
    //        Handler = null;
    //    }
    //    public class ExternalEventHandler : IExternalEventHandler
    //    {
    //        public Action<UIApplication> Action { get; set; }
    //        public void Execute(UIApplication app)
    //        {
    //            try
    //            {
    //                Action?.Invoke(app);
    //            }
    //            catch (Exception ex)
    //            {
    //                TaskDialog.Show("tt", "Revit外部事件发生错误：" + ex.Message + "\n堆栈信息：" + ex.StackTrace);
    //            }
    //        }
    //        public string GetName()
    //        {
    //            return "Revit外部事件";
    //        }
    //    }
    //}
}
