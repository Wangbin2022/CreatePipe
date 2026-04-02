using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using RevitColor = Autodesk.Revit.DB.Color;

namespace CreatePipe.Utils
{
    public class ColorConvertHelper : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Autodesk.Revit.DB.Color color)
            {
                return ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue));
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class ColorExtension
    {
        public static string ConvertToHTML(this Autodesk.Revit.DB.Color color)
        {
            return ColorTranslator.ToHtml(Color.FromArgb(color.Red, color.Green, color.Blue));
        }

        public static Autodesk.Revit.DB.Color ConvertToRevitColor(this System.Drawing.Color color)
        {
            return new Autodesk.Revit.DB.Color(color.R, color.G, color.B);
        }
        // 修改接收器类型为 System.Windows.Media.Color
        public static RevitColor ConvertToRevitColor(this System.Windows.Media.Color color)
        {
            // Revit 的 Color 构造函数接受 byte R, byte G, byte B
            return new RevitColor(color.R, color.G, color.B);
        }
    }
}
