using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;

namespace CreatePipe.filter
{
    public class ParkingLotFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance familyInstance)
            {
                //获取 FamilySymbol
                FamilySymbol familySymbol = familyInstance.Symbol;
                if (familySymbol.Family.Name.Contains("车位"))
                {
                    return true;
                }
            }
            return false;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
