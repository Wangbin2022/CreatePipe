using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Autodesk.Revit.DB.Color color && color.IsValid)
            {
                //return ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue));
                // ColorTranslator.ToHtml 也是可以的，但手动拼接能确保格式始终为 #RRGGBB
                return string.Format("#{0:X2}{1:X2}{2:X2}", color.Red, color.Green, color.Blue);
            }
            return "#00000000";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
