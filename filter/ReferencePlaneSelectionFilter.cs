using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class ReferencePlaneSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is ReferencePlane;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
