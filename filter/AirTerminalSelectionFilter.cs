using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

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
