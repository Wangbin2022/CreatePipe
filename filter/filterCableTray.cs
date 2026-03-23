using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    internal class filterCableTray : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is CableTray)
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
