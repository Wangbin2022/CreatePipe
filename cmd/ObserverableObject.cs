using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CreatePipe.cmd
{
    public class ObserverableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //SetProperty<T> 方法的目的是提供一个通用的属性设置机制，用于在对象的属性值发生变化时，自动触发 INotifyPropertyChanged 接口的 PropertyChanged 事件。
        //public string Name{get { return _name; } set { SetProperty(ref _name, value); }}
        //当 Name 属性的值发生变化时，SetProperty 方法会被调用，并且如果值确实发生了变化，它会触发 PropertyChanged 事件。
        protected virtual void SetProperty<T>(ref T store, T v, [CallerMemberName] string propertyName = null)
        {
            store = v;
            this.OnPropertyChanged(propertyName);
        }
    }
}
