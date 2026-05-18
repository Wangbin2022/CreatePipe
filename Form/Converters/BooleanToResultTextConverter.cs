using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 布尔值转结果文本转换器
    /// </summary>
    public class BooleanToResultTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "成功" : "失败";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
