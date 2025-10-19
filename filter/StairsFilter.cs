using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI.Selection;
using System;

namespace CreatePipe.filter
{
    internal class StairsFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Stairs)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
