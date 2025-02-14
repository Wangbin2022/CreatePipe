using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class DoorFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) //0326加上类别过滤器
        {
            //if (elem is FamilyInstance) //此句指定过滤类型，只选取指定类型构件
            if (elem is FamilyInstance && elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
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
