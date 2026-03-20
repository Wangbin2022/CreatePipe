using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converter
{
    //注意存在潜在问题，转换器中： return luma > 0.45 ? ... Black : ... White; 如果 value 传入的是 Transparent（透明），color.A 为 0，
    //但 R, G, B 通常也为 0。计算出的 luma 也会是 0。 0 不大于 0.45，所以它会返回 White。 如果你在 Revit 中依然看到的是黑色，那说明：
    //绑定确实失败了（TextBlock 使用了默认的系统前景色，即黑色）。
    //或者有其他的 Style 强制覆盖了 Foreground。
    public class BackgroundToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Brush brush)                // 确保传入的是Brush类型
            {
                var solidColorBrush = brush as SolidColorBrush;                // 获取颜色的SolidColorBrush
                if (solidColorBrush != null)
                {
                    System.Windows.Media.Color color = solidColorBrush.Color;
                    double red = color.R / 255.0;
                    double green = color.G / 255.0;
                    double blue = color.B / 255.0;
                    double luma = red * 0.2126 + green * 0.7152 + blue * 0.0722;
                    // 如果亮度大于0.5，则返回黑色，否则返回白色
                    return luma > 0.45 ? new SolidColorBrush(System.Windows.Media.Colors.Black) : new SolidColorBrush(System.Windows.Media.Colors.White);
                }
            }
            return new SolidColorBrush(System.Windows.Media.Colors.White); // 默认返回黑色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
