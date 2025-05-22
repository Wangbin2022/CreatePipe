using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class FamilyInstanceFilterClass : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance)
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
