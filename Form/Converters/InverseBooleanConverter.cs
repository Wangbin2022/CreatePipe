using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果传入的值是 bool，则取反返回
            if (value is bool isUsed)
            {
                return !isUsed;
            }
            return true; // 默认防错机制
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return !isEnabled;
            }
            return false;
        }
    }
}
