using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    /// <summary>
    /// 墙体面过滤器 - 只允许选择墙体上的点
    /// </summary>
    public class WallFaceFilter : ISelectionFilter
    {
        private readonly Document _doc;

        public WallFaceFilter(Document doc) => _doc = doc;

        public bool AllowElement(Element elem) => true;

        public bool AllowReference(Reference refer, XYZ position)
        {
            var elem = _doc.GetElement(refer);
            return elem is Wall;
        }
    }
}
