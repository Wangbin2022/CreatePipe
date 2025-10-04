using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // Allow selection if the element is a Floor.
            return elem is Floor;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            // This method is not used for element selection, but must be implemented.
            return false;
        }
    }
}
