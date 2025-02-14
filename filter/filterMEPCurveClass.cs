using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class filterMEPCurveClass : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is MEPCurve && !(elem is InsulationLiningBase))
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
