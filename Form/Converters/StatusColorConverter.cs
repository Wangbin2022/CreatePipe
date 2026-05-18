using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    ///// <summary>
    ///// 状态颜色转换器
    ///// </summary>
    //public class StatusColorConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (value is bool b && b) ? Brushes.Orange : Brushes.Green;
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            if (status?.Contains("成功") == true)
                return Brushes.Green;
            if (status?.Contains("失败") == true || status?.Contains("错误") == true)
                return Brushes.Red;
            if (status?.Contains("验证失败") == true)
                return Brushes.Orange;
            return Brushes.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
