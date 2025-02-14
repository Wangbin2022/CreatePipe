using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection;
using System;

namespace CreatePipe.CableConduitCreator
{
    public class OnlyMepElemFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is CableTray || elem is Conduit)
            {
                return true;
            }
            else if (elem is FamilyInstance)
            {
                FamilyInstance temp = elem as FamilyInstance;
                if (temp != null)
                {
                    return true;
                }
                else return false;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}

