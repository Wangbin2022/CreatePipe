using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    public class LastPipeSystemTextColor : IValueConverter
    {
        Document Document { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PipingSystemType pipingSystem = value as PipingSystemType;
            PipeSystemEntity pipeSystemEntity = new PipeSystemEntity(pipingSystem, null);
            Document = pipeSystemEntity.Document;

            bool lastSystem = IsLastSystemEntity(pipeSystemEntity);
            if (!lastSystem)
            {
                return new SolidColorBrush(Colors.Black);
            }
            return new SolidColorBrush(Colors.Red);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public bool IsLastSystemEntity(PipeSystemEntity systemType)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Document).OfClass(typeof(MEPSystemType));
            List<MEPSystemType> systemTypes = elems.OfType<MEPSystemType>().ToList();
            int systemCount = 0;
            foreach (MEPSystemType item in systemTypes)
            {
                if (systemType.MEPSystemClassOrigin == item.SystemClassification)
                {
                    systemCount++;
                }
            }
            if (systemCount > 1)
            {
                return false;
            }
            return true;
        }
    }
}
