using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class filterDuct : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Duct)
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
