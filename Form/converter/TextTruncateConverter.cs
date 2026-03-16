using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CreatePipe.Form.converter
{
    /// <summary>
    /// 文本截断转换器 - 当文本超过指定长度时显示为...
    /// </summary>
    public class TextTruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string text = value.ToString();

            // 获取最大长度参数，默认为8
            int maxLength = 8;
            if (parameter != null)
            {
                int.TryParse(parameter.ToString(), out maxLength);
            }
            // 如果文本长度小于等于最大长度，返回原文本
            if (text.Length <= maxLength)
                return text;
            // 否则截断并添加...
            return text.Substring(0, maxLength) + "...";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
