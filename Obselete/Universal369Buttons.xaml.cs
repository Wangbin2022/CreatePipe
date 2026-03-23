using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CreatePipe.Obselete
{
    /// <summary>
    /// Universal369Buttons.xaml 的交互逻辑
    /// </summary>
    public partial class Universal369Buttons : Window
    {
        public Universal369Buttons()
        {
            this.DataContext = new MainViewModel();
            InitializeComponent();
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class ClipboardItem : ObservableObject
    {
        private string _text;
        private DateTime _timestamp;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(HasContent));
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText => string.IsNullOrEmpty(_text) ? "空" :
            (_text.Length > 8 ? _text.Substring(0, 6) + "..." : _text);

        public bool HasContent => !string.IsNullOrEmpty(_text);
    }

    public class ObservableObject : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }
    public class MainViewModel : ObservableObject
    {
        private ObservableCollection<ClipboardItem> _clipboardItems;
        private int _visibleButtonCount = 3;
        private string _statusMessage;
        private bool _isTopmost = true;

        public MainViewModel()
        {
            InitializeClipboardItems();
            SaveToButtonCommand = new RelayCommand(ExecuteSaveToButton, CanExecuteSaveToButton);
            CopyFromButtonCommand = new RelayCommand(ExecuteCopyFromButton, CanExecuteCopyFromButton);
            ClearButtonCommand = new RelayCommand(ExecuteClearButton, CanExecuteClearButton);
            ToggleButtonCountCommand = new RelayCommand(ExecuteToggleButtonCount);
            ToggleTopmostCommand = new RelayCommand(ExecuteToggleTopmost);
        }

        public ObservableCollection<ClipboardItem> ClipboardItems
        {
            get => _clipboardItems;
            set
            {
                _clipboardItems = value;
                OnPropertyChanged();
            }
        }

        public int VisibleButtonCount
        {
            get => _visibleButtonCount;
            set
            {
                _visibleButtonCount = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(VisibleButtons));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsTopmost
        {
            get => _isTopmost;
            set
            {
                _isTopmost = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveToButtonCommand { get; }
        public ICommand CopyFromButtonCommand { get; }
        public ICommand ClearButtonCommand { get; }
        public ICommand ToggleButtonCountCommand { get; }
        public ICommand ToggleTopmostCommand { get; }

        private void InitializeClipboardItems()
        {
            ClipboardItems = new ObservableCollection<ClipboardItem>();
            for (int i = 0; i < 9; i++)
            {
                ClipboardItems.Add(new ClipboardItem { Timestamp = DateTime.Now });
            }
        }

        private bool CanExecuteSaveToButton(object parameter)
        {
            return parameter is int index && index >= 0 && index < 9;
        }

        private void ExecuteSaveToButton(object parameter)
        {
            try
            {
                int index = (int)parameter;

                // 检查剪贴板是否包含文本
                if (!System.Windows.Clipboard.ContainsText())
                {
                    StatusMessage = "剪贴板中不包含文本内容，操作取消";
                    return;
                }

                // 获取文本内容
                string text = System.Windows.Clipboard.GetText();

                // 保存到按钮
                var item = ClipboardItems[index];
                item.Text = text;
                item.Timestamp = DateTime.Now;

                StatusMessage = $"已保存文本到按钮 {index + 1}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
        }

        private bool CanExecuteCopyFromButton(object parameter)
        {
            return parameter is int index &&
                   index >= 0 &&
                   index < 9 &&
                   ClipboardItems[index].HasContent;
        }

        private void ExecuteCopyFromButton(object parameter)
        {
            try
            {
                int index = (int)parameter;
                var item = ClipboardItems[index];

                // 将文本复制回剪贴板
                System.Windows.Clipboard.SetText(item.Text);
                StatusMessage = $"已从按钮 {index + 1} 复制文本到剪贴板";
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
        }

        private bool CanExecuteClearButton(object parameter)
        {
            return parameter is int index &&
                   index >= 0 &&
                   index < 9 &&
                   ClipboardItems[index].HasContent;
        }

        private void ExecuteClearButton(object parameter)
        {
            int index = (int)parameter;
            ClipboardItems[index].Text = null;
            StatusMessage = $"已清空按钮 {index + 1}";
        }

        private void ExecuteToggleButtonCount(object parameter)
        {
            switch (VisibleButtonCount)
            {
                case 3:
                    VisibleButtonCount = 6;
                    break;
                case 6:
                    VisibleButtonCount = 9;
                    break;
                case 9:
                    VisibleButtonCount = 3;
                    break;
                default:
                    break;
            }
            //VisibleButtonCount = VisibleButtonCount switch
            //{
            //    3 => 6,
            //    6 => 9,
            //    9 => 3,
            //    _ => 3
            //};
        }

        private void ExecuteToggleTopmost(object parameter)
        {
            IsTopmost = !IsTopmost;
            StatusMessage = IsTopmost ? "窗口已置顶" : "窗口已取消置顶";
        }

    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);
    }
    // 按钮数量转可见性转换器
    public class ButtonCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Border border && parameter is string countStr)
            {
                int count = int.Parse(countStr);
                var itemsControl = border.TemplatedParent as ItemsControl;
                if (itemsControl != null)
                {
                    int index = itemsControl.Items.IndexOf(border.DataContext);

                    switch (count)
                    {
                        case 3:
                            return index < 3 ? Visibility.Visible : Visibility.Collapsed;
                        case 6:
                            return index < 6 ? Visibility.Visible : Visibility.Collapsed;
                        default:
                            return Visibility.Visible;
                    }
                }
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 布尔值转字符转换器（用于图钉图标）
    public class BooleanToCharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isTopmost)
            {
                return isTopmost ? "📌" : "📍";
            }
            return "📌";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 布尔值转ToolTip转换器
    public class BooleanToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isTopmost)
            {
                return isTopmost ? "取消置顶" : "置顶窗口";
            }
            return "置顶切换";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
