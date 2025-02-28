﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.Filter
{
    /// <summary>
    /// 过滤房间
    /// </summary>
    public class RoomFilterClass : ISelectionFilter
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
