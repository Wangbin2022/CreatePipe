using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    public class TextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查值是否为0
            if (value is int intValue && intValue == 0)
            {
                return Colors.Black;// 返回黑色画刷
            }
            return Colors.Red; // 返回红色画刷
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported.");
        }
    }
}
