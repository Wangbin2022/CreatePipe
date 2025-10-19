using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;

namespace CreatePipe.filter
{
    internal class FamilyFilterClass : ISelectionFilter
    {
        private readonly Family _targetFamily;

        public FamilyFilterClass(Family targetFamily)
        {
            _targetFamily = targetFamily;
        }

        public bool AllowElement(Element elem)
        {
            // 检查元素是否为FamilyInstance并且属于目标Family
            if (elem is FamilyInstance familyInstance)
            {
                return familyInstance.Symbol.Family.Id == _targetFamily.Id;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
