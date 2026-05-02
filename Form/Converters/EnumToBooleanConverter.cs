using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    /// <summary>
    /// 枚举值到布尔值转换器（用于 RadioButton 绑定）
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(bool))]
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.Equals(Enum.Parse(value.GetType(), parameter.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(bool)value) return Binding.DoNothing;
            return parameter == null ? Binding.DoNothing : Enum.Parse(targetType, parameter.ToString());
        }
    }
}
