using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class DWGReferenceFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // 只允许选择 ImportInstance 类型的元素（即参照 DWG 文件）
            return elem is ImportInstance;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            // 允许选择参照 DWG 文件中的对象
            return true;
        }
    }
}
