using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class filterPipe : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Pipe)
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
