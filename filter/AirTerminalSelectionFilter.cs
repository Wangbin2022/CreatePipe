using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    public class AirTerminalSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category != null && elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
