using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        // 从 源(ViewModel/Entity) 转换到 目标(UI)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                // 如果数量大于0，返回 true (按钮启用)；否则返回 false (按钮禁用)
                return intValue > 0;
            }
            return false; // 如果绑定的不是int，默认禁用
        }
        // 从 目标(UI) 转换回 源(ViewModel) - 这里不需要双向绑定，所以抛出异常即可
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
