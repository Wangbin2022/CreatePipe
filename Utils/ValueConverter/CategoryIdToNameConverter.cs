using Autodesk.Revit.DB;
using OfficeOpenXml.FormulaParsing.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CreatePipe.Utils.ValueConverter
{
    public class CategoryIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ElementId elementId)
            {
                Document doc = (Document)parameter;
                Category category = Category.GetCategory(doc, (ElementId)value);
                return category?.Name;  
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}