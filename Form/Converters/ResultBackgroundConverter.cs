using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 结果背景色转换器
    /// </summary>
    public class ResultBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? new SolidColorBrush(Color.FromRgb(220, 235, 220))
                                          : new SolidColorBrush(Color.FromRgb(255, 220, 220));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
