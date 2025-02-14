﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class filterRoomClass : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Room)
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
