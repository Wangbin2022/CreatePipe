using System;
using System.Globalization;
using System.Windows.Data;

namespace CreatePipe.Form.Converters
{
    public class ColorToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Autodesk.Revit.DB.Color color)
            {
                return color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
            }
            return "未指定";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
