using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Management.Instrumentation;

namespace CreatePipe.filter
{
    public class AdaptiveFamilyFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance instance && AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(instance))
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