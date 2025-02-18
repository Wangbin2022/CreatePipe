using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace CreatePipe
{
    public class ViewModel: ObserverableObject
    {
        public Document Doc { get; set; }

        public ViewModel(UIApplication application)
        {
            Doc = application.ActiveUIDocument.Document;
            //TaskDialog.Show("tt", Doc.PathName);
        }
        public ICommand TestCommand => new BaseBindingCommand(Test);
        private void Test(object obj)
        {

        }

    }
    public class StartsWithConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && parameter is string prefix)
            {
                return text.StartsWith(prefix);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
