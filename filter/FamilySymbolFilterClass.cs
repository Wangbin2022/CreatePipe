using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class FamilySymbolFilterClass : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilySymbol)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
