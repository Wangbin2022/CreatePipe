using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe
{
    /// <summary>
    /// PropertiesForm.xaml 的交互逻辑
    /// </summary>
    public partial class PropertiesForm : Window
    {
        public double NumericValue { get; set; }
        public PropertiesForm()
        {
            InitializeComponent();
            DataContext = this;
            //Bitmap bitmap = new Bitmap(); 
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    //文本框数值
    public class NumberValidationRule : ValidationRule
    {
        public double Minimum { get; set; } = double.MinValue;
        public double Maximum { get; set; } = double.MaxValue;
        public override ValidationResult Validate(object value, CultureInfo culture)
        {
            if (double.TryParse(value.ToString(), out double num))
            {
                if (num >= Minimum && num <= Maximum)
                    return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, $"请输入 {Minimum} 到 {Maximum} 之间的数字");
        }
    }

}
