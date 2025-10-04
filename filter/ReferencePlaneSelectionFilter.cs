using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    public class ReferencePlaneSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is ReferencePlane;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
