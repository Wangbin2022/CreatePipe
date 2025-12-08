using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.filter
{
    ////未跑通选择
    //public class LinkedPipeDuctSelectionFilter : ISelectionFilter
    //{
    //    public bool AllowElement(Element elem)
    //    {
    //        // 只能是 RevitLinkInstance
    //        if (!(elem is RevitLinkInstance linkInstance))
    //            return false;
    //        // 取链接文档
    //        Document linkDoc = linkInstance.GetLinkDocument();
    //        if (linkDoc == null) return false;
    //        // 注意：这里直接点击链接里的构件时，elem 是链接本身
    //        // 所以如果需要按LinkedElementId来判断，需要在Pick时获取Reference
    //        return true; // 此处先允许 RevitLinkInstance，通过Reference再精筛
    //    }
    //    public bool AllowReference(Reference reference, XYZ position)
    //    {
    //        // 取引用的真正元素
    //        Document doc = reference.ElementReferenceType == ElementReferenceType.Linked ?
    //                       (reference.LinkedElementId != ElementId.InvalidElementId
    //                            ? ((RevitLinkInstance)reference.Element).GetLinkDocument()
    //                            : null)
    //                       : reference.Document;
    //        if (doc == null) return false;
    //        ElementId linkedElemId = reference.LinkedElementId;
    //        if (linkedElemId == ElementId.InvalidElementId) return false;
    //        Element linkedElem = doc.GetElement(linkedElemId);
    //        if (linkedElem == null) return false;
    //        // 过滤类别：管道或风管
    //        return linkedElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves ||
    //               linkedElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctCurves;
    //    }
    //}
}
