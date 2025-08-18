using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text;

namespace CreatePipe.Form
{
    /// <summary>
    /// GuidanaceSignPlaceView.xaml 的交互逻辑
    /// </summary>
    public partial class GuidanceSignPlaceView : Window
    {
        public GuidanceSignPlaceView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new GuidanceSignPlaceViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ContentTb_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            // 立刻刷新一次绑定，触发 ValidationRule
            tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        }
    }
    public class GuidanceSignPlaceViewModel : ObserverableObject
    {
        public GuidanceSignPlaceViewModel(UIApplication uiApp)
        {

        }

        public ICommand SplitContentCommand => new RelayCommand<string>(SplitContent);
        private void SplitContent(string obj)
        {
            string frontContent = null;
            string backContent = null;
            if (obj.Contains("|"))
            {
                // 分割字符串，最多分成2部分
                string[] parts = obj.Split(new[] { '|' }, 2);
                frontContent = parts[0].Trim();
                backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else frontContent = obj;
            //TaskDialog.Show("tt", frontContent);
            //TaskDialog.Show("tt", backContent);
            // 分割正面内容
            string[] frontParts = frontContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
            int frontCount = Math.Min(frontParts.Length, 3); // 最多取3个
            if (frontCount > 0) FrontSignFirst = RemovePrefix(frontParts[0].Trim());
            if (frontCount > 1) FrontSignSecond = frontParts[1].Trim();
            if (frontCount > 2) FrontSignThird = frontParts[2].Trim();
            // 分割背面内容（如果有）
            int backCount = 0;
            if (!string.IsNullOrEmpty(backContent))
            {
                string[] backParts = backContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
                backCount = Math.Min(backParts.Length, 3); // 最多取3个
                if (backCount > 0) BackSignFirst = RemovePrefix(backParts[0].Trim());
                if (backCount > 1) BackSignSecond = backParts[1].Trim();
                if (backCount > 2) BackSignThird = backParts[2].Trim();
            }
            else
            {
                BackSignFirst = "-";
                BackSignSecond = "-";
                BackSignThird = "-";
            }
                // 确定行数（取正反面中较大的数量）
                SignRows = Math.Max(frontCount, backCount);
            //TaskDialog.Show("tt", SignRows.ToString());
        }
        private string RemovePrefix(string input)
        {
            if (input.StartsWith("正面：") || input.StartsWith("正面:") || input.StartsWith("背面：") || input.StartsWith("背面:"))
                return input.Substring(3);
            else return input;
        }
        public int SignRows { get; set; } = 1;
        private string frontSignFirst = "-";
        private string frontSignSecond = "-";
        private string frontSignThird = "-";
        private string backSignFirst = "-";
        private string backSignSecond = "-";
        private string backSignThird = "-";
        public string FrontSignFirst { get => frontSignFirst; set => SetProperty(ref frontSignFirst, value); }
        public string FrontSignSecond { get => frontSignSecond; set => SetProperty(ref frontSignSecond, value); }
        public string FrontSignThird { get => frontSignThird; set => SetProperty(ref frontSignThird, value); }
        public string BackSignFirst { get => backSignFirst; set => SetProperty(ref backSignFirst, value); }
        public string BackSignSecond { get => backSignSecond; set => SetProperty(ref backSignSecond, value); }
        public string BackSignThird { get => backSignThird; set => SetProperty(ref backSignThird, value); }
        public ICommand InputRuleCommand => new BaseBindingCommand(InputRule);
        private void InputRule(object obj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("输入内容规则说明：");
            stringBuilder.AppendLine("1.正反牌面内容以竖线 | 分割，输入|不超过1个");
            stringBuilder.AppendLine("2.正反牌面内容分别以“正面：”“背面：”开头");
            stringBuilder.AppendLine("3.多行牌面内容以分号区分，每段最多2个 ;");
            stringBuilder.AppendLine("4.输入字符串中不得包含半角逗号 ,");
            TaskDialog.Show("tt", stringBuilder.ToString());
        }
        public ICommand TestCommand => new RelayCommand<int>(test);
        private void test(int obj)
        {
            TaskDialog.Show("tt", obj.ToString());
        }
        private bool _hasError = true;   // 初始置 true，空内容即报错
        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(nameof(isContentValidate)); }
        }
        // 供按钮 IsEnabled 绑定
        public bool isContentValidate => !HasError;
        public int AlignSignHeight { get; set; } = 2800;
        public int HangSignHeight { get; set; } = 3400;
        public int SignLength { get; set; } = 3000;
        public int SignWidth { get; set; } = 350;
        public int SignAngle { get; set; } = 0;
        public string ContentText
        {
            get => contentText;
            set => SetProperty(ref contentText, value);
        }
        private string contentText;
    }
    public static class ValidationBehaviors
    {
        #region HasError 附加属性
        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.RegisterAttached(
                "HasError", typeof(bool), typeof(ValidationBehaviors),
                new PropertyMetadata(false, OnHasErrorChanged));
        public static bool GetHasError(DependencyObject obj) =>
            (bool)obj.GetValue(HasErrorProperty);
        public static void SetHasError(DependencyObject obj, bool value) =>
            obj.SetValue(HasErrorProperty, value);
        private static void OnHasErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox tb && tb.DataContext is GuidanceSignPlaceViewModel vm)
                vm.HasError = (bool)e.NewValue;
        }
        #endregion
    }

}
