using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

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
    }
}
