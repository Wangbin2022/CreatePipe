using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 集合数量转布尔值转换器
    /// </summary>
    public class CountToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is int count && count > 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
