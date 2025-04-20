using System;
using System.Windows.Input;


namespace CreatePipe.cmd
{
    public class BaseBindingCommand : ICommand
    {

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        private ICommand exportXml;

        public BaseBindingCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public BaseBindingCommand(ICommand exportXml)
        {
            this.exportXml = exportXml;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        public event EventHandler CanExecuteChangedInternal;
        public void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChangedInternal;
            if (handler != null)
            {
                //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
                handler.Invoke(this, EventArgs.Empty);
            }
        }
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
