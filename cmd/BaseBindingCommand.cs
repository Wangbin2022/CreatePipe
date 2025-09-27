using System;
using System.Windows.Input;


namespace CreatePipe.cmd
{
    public class BaseBindingCommand : ICommand
    {
        // --- 核心改动在这里 ---
        // 1. 移除对 CommandManager 的依赖，改为一个标准的 C# 事件。WPF 控件会自动订阅这个事件。
        public event EventHandler CanExecuteChanged;
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        // --- 核心改动结束 ---
        public BaseBindingCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            // 如果 _canExecute 为 null，说明该命令总能执行
            // 否则，调用委托判断
            return _canExecute == null || _canExecute(parameter);
        }
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        // 2. 实现您需要的 RaiseCanExecuteChanged 方法。
        //    您可以直接重命名您现有的 OnCanExecuteChanged 方法，或者新建一个。
        //    它的作用是手动触发上面的 CanExecuteChanged 事件。
        public void RaiseCanExecuteChanged()
        {
            // 安全地调用事件委托
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        // 3. 您原有的 CanExecuteChangedInternal 和 OnCanExecuteChanged() 现在是多余的了，
        //    因为 RaiseCanExecuteChanged() 已经实现了同样的功能，并且是直接作用于
        //    WPF 绑定的标准事件。建议删除它们以避免混淆。
        //0925 废弃原有
        //public event EventHandler CanExecuteChanged
        //{
        //    add => CommandManager.RequerySuggested += value;
        //    remove => CommandManager.RequerySuggested -= value;
        //}
        //private readonly Action<object> _execute;
        //private readonly Predicate<object> _canExecute;
        //private ICommand exportXml;
        //public BaseBindingCommand(Action<object> execute, Predicate<object> canExecute = null)
        //{
        //    _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        //    _canExecute = canExecute;
        //}
        //public BaseBindingCommand(ICommand exportXml)
        //{
        //    this.exportXml = exportXml;
        //}
        //public bool CanExecute(object parameter)
        //{
        //    return _canExecute == null || _canExecute(parameter);
        //}
        //public void Execute(object parameter)
        //{
        //    _execute(parameter);
        //}
        //public event EventHandler CanExecuteChangedInternal;
        //public void OnCanExecuteChanged()
        //{
        //    EventHandler handler = this.CanExecuteChangedInternal;
        //    if (handler != null)
        //    {
        //        //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
        //        handler.Invoke(this, EventArgs.Empty);
        //    }
        //}
        //1220 替代测试
        //public BaseBindingCommand(Action<object> execute) : this(execute, DefaultCanExecute)
        //{
        //}
        //public BaseBindingCommand(Action<object, object> execute)  
        //{
        //    DelegateExecute2 = execute;
        //}
        //public BaseBindingCommand(Action<object> execute, Func<object, bool> canExecute = null)
        //{
        //    if (execute == null)
        //    {
        //        throw new ArgumentNullException("execute");
        //    }
        //    if (canExecute == null)
        //    {
        //        throw new ArgumentNullException("canExecute");
        //    }
        //    this.DelegateExecute = execute;
        //    this.DelegateCanExecute = canExecute;
        //}
        //public event EventHandler CanExecuteChanged
        //{
        //    add
        //    {
        //        CommandManager.RequerySuggested += value;
        //        this.CanExecuteChangedInternal += value;
        //    }
        //    remove
        //    {
        //        CommandManager.RequerySuggested -= value;
        //        this.CanExecuteChangedInternal -= value;
        //    }
        //}
        //protected Action<object> DelegateExecute { get; set; }
        ////protected Action<object,object> DelegateExecute2 { get; set; }
        //protected Func<object, bool> DelegateCanExecute { get; set; }

        //1220 替代测试
        //public virtual bool CanExecute(object parameter)
        //{
        //    bool result;
        //    try
        //    {
        //        var delegateCanExecute = DelegateCanExecute;
        //        result = delegateCanExecute == null || delegateCanExecute(parameter);
        //    }
        //    catch (Exception)
        //    {
        //        result = false;
        //    }
        //    return result;
        //}
        //public virtual void Execute(object parameter)
        //{
        //    try
        //    {
        //        var delegateExecute = DelegateExecute;
        //        delegateExecute?.Invoke(parameter);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}
        //public void Destroy()
        //{
        //    this.DelegateCanExecute = _ => false;
        //    this.DelegateExecute = _ => { return; };
        //}
        //private static bool DefaultCanExecute(object parameter)
        //{
        //    return true;
        //}
    }
    //以下由Kimi生成/跟GPT生成的一模一样
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
