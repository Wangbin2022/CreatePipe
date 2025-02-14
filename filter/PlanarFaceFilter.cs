using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class PlanarFaceFilter : ISelectionFilter
    {

        Document doc = null;
        public PlanarFaceFilter(Document document)
        {
            doc = document;
        }
        public bool AllowElement(Element elem)
        {
            return true;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            if (doc.GetElement(reference).GetGeometryObjectFromReference(reference) is PlanarFace)
            {
                return true;
            }
            return false;
        }
    }
}
