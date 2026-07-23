using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalSliderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalSliderWindow : Window
    {
        public UniversalSliderViewModel ViewModel => (UniversalSliderViewModel)DataContext;
        public UniversalSliderWindow(string prompt, double maxValue, string optionalText = "")
        {
            InitializeComponent();
            DataContext = new UniversalSliderViewModel(prompt, maxValue, optionalText);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsValid)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("请输入有效的数字！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //if (ViewModel.NewName != null)
            //{
            //    DialogResult = true;
            //    this.Close();
            //}
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_OK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }
    }
    public class UniversalSliderViewModel : ObserverableObject
    {
        private double _newNum;
        private bool _isValid = true;
        private string _errorMessage;
        /// <summary>
        /// 绑定的数值（双向同步）
        /// </summary>
        public double NewNum
        {
            get => _newNum;
            set
            {
                // 使用三元运算符实现 Clamp
                double clamped = value < MinValue ? MinValue : (value > MaxValue ? MaxValue : value);
                double rounded = Math.Round(clamped, 2);

                if (SetProperty(ref _newNum, rounded))
                {
                    IsValid = true;
                    ErrorMessage = "";
                    OnPropertyChanged(nameof(DisplayValue));
                }
            }
        }
        public double MinValue { get; } = 0;
        public double MaxValue { get; }
        public int SliderTickFrequency => 1;
        public string DisplayText { get; }
        public string DisplayValue;
        public bool IsValid
        {
            get => _isValid;
            private set => SetProperty(ref _isValid, value);
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }
        public string Error => ErrorMessage;
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(NewNum))
                {
                    if (NewNum < MinValue || NewNum > MaxValue)
                    {
                        IsValid = false;
                        ErrorMessage = $"数值必须在 {MinValue} 到 {MaxValue} 之间";
                        return ErrorMessage;
                    }
                }
                IsValid = true;
                ErrorMessage = "";
                return null;
            }
        }
        public UniversalSliderViewModel(string prompt, double maxValue, string optionalText = "")
        {
            DisplayText = prompt;
            MaxValue = maxValue;

            // 初始化默认值
            if (double.TryParse(optionalText, out double defaultVal))
            {
                // 使用 Math.Min/Max 限制范围
                NewNum = Math.Min(Math.Max(defaultVal, MinValue), MaxValue);
            }
            else
            {
                NewNum = 0;
            }
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
