using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class filterPipeFitting : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance fi)
            {
                // 检查是否有MEPModel和连接件
                return fi.MEPModel?.ConnectorManager != null &&
                       fi.Symbol.Family.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting;
            }
            return false;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
