using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class PlanarFaceFilter : ISelectionFilter
    {
        private readonly Document _doc;
        public PlanarFaceFilter(Document doc)
        {
            _doc = doc;
        }
        public bool AllowElement(Element elem)
        {
            // 允许所有图元被初步鼠标悬停（具体能否选中由 AllowReference 决定）
            return true;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            try
            {
                Element elem = _doc.GetElement(reference);
                if (elem != null)
                {
                    // 获取鼠标当前悬停的几何面
                    GeometryObject geoObj = elem.GetGeometryObjectFromReference(reference);
                    // 只有当这个面是平面时，才允许鼠标点击选中
                    return geoObj is PlanarFace;
                }
            }
            catch
            {
                // 忽略异常，默认不可选
            }
            return false;
        }
        //Document doc = null;
        //public PlanarFaceFilter(Document document)
        //{
        //    doc = document;
        //}
        //public bool AllowElement(Element elem)
        //{
        //    return true;
        //}

        //public bool AllowReference(Reference reference, XYZ position)
        //{
        //    if (doc.GetElement(reference).GetGeometryObjectFromReference(reference) is PlanarFace)
        //    {
        //        return true;
        //    }
        //    return false;
        //}
    }
}
