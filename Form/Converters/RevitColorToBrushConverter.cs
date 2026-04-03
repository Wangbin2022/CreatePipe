using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    public class RevitColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 确保目标类型是 Brush，因为 Rectangle.Fill 需要 Brush
            if (targetType != typeof(Brush) && targetType != typeof(Color))
            {
                return null; // 或者 throw new InvalidOperationException("Target type must be Brush or Color.");
            }

            if (value is Autodesk.Revit.DB.Color revitColor)
            {
                // Revit.DB.Color 没有 Alpha，通常默认为不透明 (255)
                System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(
                    255, // Alpha
                    revitColor.Red,
                    revitColor.Green,
                    revitColor.Blue
                );
                return new SolidColorBrush(wpfColor); // 返回 SolidColorBrush
            }

            // 如果没有有效的 Revit.DB.Color，返回一个默认的 Brush (例如透明或灰色)
            return new SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
