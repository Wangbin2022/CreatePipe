using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    internal class GroupFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Group)
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
