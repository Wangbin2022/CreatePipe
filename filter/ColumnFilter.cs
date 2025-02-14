using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class ColumnFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {

            if (elem is FamilyInstance && (elem.Category.Id.IntegerValue == -2001330 || elem.Category.Id.IntegerValue == -2001300 || elem.Category.Id.IntegerValue == -2001320))
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
