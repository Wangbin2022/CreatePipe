using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 空值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
