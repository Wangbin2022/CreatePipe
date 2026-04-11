using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long fileSize)
            {
                // 自动选择合适的单位
                if (fileSize >= 1024 * 1024) // 大于等于 1 MB
                {
                    return $"{fileSize / (1024 * 1024):N2} MB";
                }
                else if (fileSize >= 1024) // 大于等于 1 KB
                {
                    return $"{fileSize / 1024:N2} KB";
                }
                else // 小于 1 KB
                {
                    return $"{fileSize} Bytes";
                }
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
