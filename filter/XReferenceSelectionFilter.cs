using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class XReferenceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // 1. 允许 Revit 链接
            if (elem is RevitLinkInstance) return true;
            // 2. 允许 CAD (ImportInstance 包含了 链接的DWG 和 导入的DWG 两种)
            if (elem is ImportInstance) return true;
            return false;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            return false; // 我们只选整个图元，不选面或边
        }
    }
}
