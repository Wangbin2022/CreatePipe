using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe
{
    /// <summary>
    /// Universal369Buttons.xaml 的交互逻辑
    /// </summary>
    public partial class Universal369Buttons : Window
    {
        public Universal369Buttons()
        {
            InitializeComponent();
            this.DataContext = new Universal369ButtonsViewModel();
        }
        // 窗口拖动逻辑
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        // 关闭窗口
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class Universal369ButtonsViewModel : ViewModelBase
    {
        private int _visibleCount = 3;

        // 当前置顶状态，默认为true
        public bool IsTopmost { get; set; } = true;

        public int VisibleCount
        {
            get => _visibleCount;
            set
            {
                if (_visibleCount != value)
                {
                    _visibleCount = value;
                    OnPropertyChanged(nameof(VisibleCount));
                    // 通知按钮的可见性发生改变
                    foreach (var btn in ButtonItems)
                    {
                        btn.OnVisibilityChanged();
                    }
                }
            }
        }

        public ObservableCollection<ButtonItem> ButtonItems { get; set; }

        public ICommand SwitchCountCommand { get; }

        public Universal369ButtonsViewModel()
        {
            // 初始化9个按钮
            ButtonItems = new ObservableCollection<ButtonItem>();
            for (int i = 1; i <= 9; i++)
            {
                ButtonItems.Add(new ButtonItem(i, this));
            }

            // 切换数量的命令
            SwitchCountCommand = new RelayCommand(param =>
            {
                if (VisibleCount == 3) VisibleCount = 6;
                else if (VisibleCount == 6) VisibleCount = 9;
                else VisibleCount = 3;
            });
        }
    }

    // 单个按钮的数据模型
    public class ButtonItem : ViewModelBase
    {
        private readonly Universal369ButtonsViewModel _parent;
        public int Index { get; }
        public string Name => $"功能 {Index}";

        public ButtonItem(int index, Universal369ButtonsViewModel parent)
        {
            Index = index;
            _parent = parent;
        }

        // 根据父级 VisibleCount 决定是否显示
        public System.Windows.Visibility Visibility
        {
            get
            {
                return Index <= _parent.VisibleCount
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }
        }

        public void OnVisibilityChanged()
        {
            OnPropertyChanged(nameof(Visibility));
        }
    }
    // ViewModel 基类，实现属性变更通知
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 简单的命令实现
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
