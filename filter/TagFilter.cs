using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class TagFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is IndependentTag && elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModelTags)
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
