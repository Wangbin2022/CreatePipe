using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class OpenningFilter : ISelectionFilter
    {
        //0627只过滤门窗
        public bool AllowElement(Element elem)
        {
            //if (elem is FamilyInstance) //此句指定过滤类型，只选取指定类型构件
            if (elem is FamilyInstance &&
                (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors || elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows))
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
