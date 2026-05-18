using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 结果前景色转换器
    /// </summary>
    public class ResultForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? new SolidColorBrush(Color.FromRgb(0, 100, 0))
                                          : new SolidColorBrush(Color.FromRgb(180, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
