using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    class StructuralConnectionSelectionFilter : ISelectionFilter
    {
        LogicalOrFilter _filter;
        /// <summary>
        /// Initialize the filter with the accepted element types.
        /// </summary>
        /// <param name="elemTypesAllowed">Logical filter containing accepted element types.</param>
        /// <returns></returns>
        public StructuralConnectionSelectionFilter(LogicalOrFilter elemTypesAllowed)
        {
            _filter = elemTypesAllowed;
        }

        /// <summary>
        /// Allows an element to be selected
        /// </summary>
        /// <param name="element">A candidate element in the selection operation.</param>
        /// <returns>Return true to allow the user to select this candidate element.</returns>
        public bool AllowElement(Element element)
        {
            return _filter.PassesFilter(element);
        }
        /// <summary>
        /// Allows a reference to be selected.
        /// </summary>
        /// <param name="refer"> A candidate reference in the selection operation.</param>
        /// <param name="point">The 3D position of the mouse on the candidate reference.</param>
        /// <returns>Return true to allow the user to select this candidate reference.</returns>
        public bool AllowReference(Reference refer, XYZ point)
        {
            return true;
        }
    }
}
