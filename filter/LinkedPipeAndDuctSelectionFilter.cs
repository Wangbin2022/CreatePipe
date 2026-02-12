namespace CreatePipe.filter
{
    ///// <summary>
    ///// 一个选择过滤器，用于只允许用户选择链接 Revit 模型中的管道（Pipe）或风管（Duct）。
    ///// </summary>
    //public class LinkedPipeAndDuctSelectionFilter : ISelectionFilter
    //{
    //    /// <summary>
    //    /// 此方法决定哪些元素可以被“预选”（鼠标悬停时高亮）。
    //    /// 我们需要允许选择 Revit 链接实例本身。
    //    /// </summary>
    //    public bool AllowElement(Element elem)
    //    {
    //        // 只允许选择 RevitLinkInstance 类型的元素。
    //        // 如果不过滤这个，鼠标将无法与链接模型进行交互。
    //        return elem is RevitLinkInstance;
    //    }
    //    /// <summary>
    //    /// 此方法在用户实际点击后进行最终的筛选。
    //    /// 这是进行精确类别判断的地方。
    //    /// </summary>
    //    public bool AllowReference(Reference reference, XYZ position)
    //    {
    //        // 检查引用是否有效，并且确实指向一个链接内的元素
    //        if (reference.LinkedElementId == ElementId.InvalidElementId)
    //        {
    //            return false;
    //        }
    //        // 获取主文档中的 RevitLinkInstance
    //        Document doc = reference.Document;
    //        var linkInstance = doc.GetElement(reference.ElementId) as RevitLinkInstance;
    //        if (linkInstance == null)
    //        {
    //            return false;
    //        }
    //        // 获取链接文档
    //        Document linkDoc = linkInstance.GetLinkDocument();
    //        if (linkDoc == null)
    //        {
    //            // 链接可能未加载
    //            return false;
    //        }
    //        // 从链接文档中获取被选中的实际元素
    //        Element linkedElement = linkDoc.GetElement(reference.LinkedElementId);
    //        if (linkedElement == null)
    //        {
    //            return false;
    //        }
    //        // 判断元素的类别是否为管道或风管
    //        // BuiltInCategory.OST_PipeCurves for Pipes
    //        // BuiltInCategory.OST_DuctCurves for Ducts
    //        if (linkedElement.Category != null)
    //        {
    //            if (linkedElement.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves ||
    //                linkedElement.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctCurves)
    //            {
    //                return true; // 是管道或风管，允许选择
    //            }
    //        }
    //        return false; // 其他所有情况都禁止选择
    //    }
    //}
}
