using CreatePipe.cmd;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalNewString.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalNewString : Window
    {
        public NewStringViewModel ViewModel => (NewStringViewModel)DataContext;
        public UniversalNewString(string prompt, string optionalText = "")
        {
            InitializeComponent();
            DataContext = new NewStringViewModel(prompt, optionalText);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewName != null)
            {
                DialogResult = true;
                this.Close();
            }
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_OK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }
    }
    public class NewStringViewModel : ObserverableObject
    {
        private string _newName;
        private double _newNum = 0; // 新增的double类型后台字段

        /// <summary>
        /// 与TextBox直接绑定的字符串属性
        /// </summary>
        public string NewName
        {
            get => _newName;
            set
            {
                // 1. 更新字符串属性本身
                SetProperty(ref _newName, value);
                // 2. 尝试将新输入的字符串转换为double
                //    double.TryParse是安全的方法，转换失败不会抛出异常
                if (double.TryParse(value, out double numericValue))
                {
                    // 3. 如果转换成功，则更新NewNum属性
                    //    使用SetProperty来触发任何可能存在的对NewNum的绑定更新
                    NewNum = numericValue;
                }
                // 如果转换失败，NewNum将保持其上一次的有效值
            }
        }
        public double NewNum
        {
            get => _newNum;
            private set => SetProperty(ref _newNum, value);
        }
        public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
        public bool IsValid = true;
        public NewStringViewModel(string prompt, string optionalText = "")
        {
            NewName = optionalText;
            DisplayText = prompt;
        }
    }
}