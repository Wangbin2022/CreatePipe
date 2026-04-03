using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class CadSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // 只允许选中导入或链接的 CAD 实例
            return elem is ImportInstance;
        }
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
