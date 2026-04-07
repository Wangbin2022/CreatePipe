using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    /// <summary>
    /// 屋面选择过滤器，兼容所有派生自 RoofBase 的屋顶类型
    /// </summary>
    public class RoofFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // 允许迹线屋顶 (FootPrintRoof) 和 拉伸屋顶 (ExtrusionRoof) 等
            return elem is RoofBase;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
