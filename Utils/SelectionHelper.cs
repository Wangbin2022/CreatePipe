using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    /// <summary>
    /// Revit 选择操作公共助手类
    /// 覆盖：单选、多选、过滤选、拾取面、拾取点、拾取线等所有高频场景
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// 智能获取单个目标元素
        /// 优先使用预选集中的单个元素，预选为空则提示拾取，预选多个则报错返回null
        /// </summary>
        /// <param name="uiDoc">UIDocument</param>
        /// <param name="pickPrompt">拾取时的提示语</param>
        /// <param name="filter">拾取时的过滤器（可选）</param>
        public static Element GetSingleElement(UIDocument uiDoc,
            string pickPrompt = "请选择一个构件", ISelectionFilter filter = null)
        {
            try
            {
                var selectedIds = uiDoc.Selection.GetElementIds().ToList();
                // 情况1：没有预选，提示用户拾取
                if (selectedIds.Count == 0)
                {
                    Reference reference = filter != null
                        ? uiDoc.Selection.PickObject(ObjectType.Element, filter, pickPrompt)
                        : uiDoc.Selection.PickObject(ObjectType.Element, pickPrompt);

                    return uiDoc.Document.GetElement(reference);
                }
                // 情况2：预选了多个，提示错误
                if (selectedIds.Count > 1)
                {
                    TaskDialog.Show("选择提示",
                        $"请只选择一个构件。\n当前已选中{selectedIds.Count} 个构件，请重新选择。");
                    return null;
                }
                // 情况3：预选了恰好一个
                return uiDoc.Document.GetElement(selectedIds[0]);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null; // 用户按ESC，静默返回
            }
        }
        /// <summary>
        /// 智能获取单个目标元素的Id
        /// </summary>
        public static ElementId GetSingleElementId(UIDocument uiDoc,
            string pickPrompt = "请选择一个构件", ISelectionFilter filter = null)
        {
            return GetSingleElement(uiDoc, pickPrompt, filter)?.Id;
        }
        /// <summary>
        /// 智能获取单个目标元素，并强制转换为指定类型
        /// 类型不匹配时自动提示并返回 null
        /// </summary>
        /// <typeparam name="T">期望的 Revit 元素类型</typeparam>
        public static T GetSingleElement<T>(UIDocument uiDoc,
            string pickPrompt = null, ISelectionFilter filter = null) where T : Element
        {
            string prompt = pickPrompt ?? $"请选择一个{typeof(T).Name}";
            var element = GetSingleElement(uiDoc, prompt, filter);
            if (element == null) return null;
            if (element is T typedElement)
                return typedElement;
            TaskDialog.Show("类型不匹配",
                $"选中的构件类型为 [{element.Category?.Name ?? element.GetType().Name}]，\n" +
                $"期望类型为 [{typeof(T).Name}]，请重新选择。");
            return null;
        }

        // =========================================================
        // 多元素获取
        // =========================================================

        /// <summary>
        /// 智能获取多个目标元素
        /// 优先使用预选集，预选为空则提示框选/多选
        /// </summary>
        /// <param name="uiDoc">UIDocument</param>
        /// <param name="pickPrompt">拾取提示语</param>
        /// <param name="filter">过滤器（可选）</param>
        /// <param name="minCount">最少需要选择的数量，默认1</param>
        public static List<Element> GetMultipleElements(UIDocument uiDoc,
            string pickPrompt = "请选择一个或多个构件（可框选）", ISelectionFilter filter = null, int minCount = 1)
        {
            try
            {
                var selectedIds = uiDoc.Selection.GetElementIds().ToList();
                List<Element> result;

                // 有预选集直接使用
                if (selectedIds.Count > 0)
                {
                    result = selectedIds.Select(id => uiDoc.Document.GetElement(id))
                        .Where(e => e != null).ToList();
                }
                else
                {
                    // 无预选，提示框选
                    IList<Reference> references = filter != null
                        ? uiDoc.Selection.PickObjects(ObjectType.Element, filter, pickPrompt)
                        : uiDoc.Selection.PickObjects(ObjectType.Element, pickPrompt);

                    result = references.Select(r => uiDoc.Document.GetElement(r))
                        .Where(e => e != null).ToList();
                }

                // 验证最少数量
                if (result.Count < minCount)
                {
                    TaskDialog.Show("选择提示", $"至少需要选择 {minCount} 个构件，当前选择了{result.Count} 个。");
                    return null;
                }

                return result;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取多个元素并过滤为指定类型
        /// </summary>
        public static List<T> GetMultipleElements<T>(UIDocument uiDoc,
            string pickPrompt = null, ISelectionFilter filter = null) where T : Element
        {
            string prompt = pickPrompt ?? $"请选择一个或多个 {typeof(T).Name}";
            var elements = GetMultipleElements(uiDoc, prompt, filter);
            return elements?.OfType<T>().ToList();
        }

        // =========================================================
        // 拾取面
        // =========================================================

        ///// <summary>
        ///// 拾取元素的某个面，返回面的 Reference和几何 Face对象
        ///// </summary>
        ///// <param name="uiDoc">UIDocument</param>
        ///// <param name="pickPrompt">提示语</param>
        ///// <param name="filter">元素过滤器（可选）</param>
        //public static FacePickResult PickFace(UIDocument uiDoc,
        //    string pickPrompt = "请拾取一个面", ISelectionFilter filter = null)
        //{
        //    try
        //    {
        //        Reference reference = filter != null
        //            ? uiDoc.Selection.PickObject(ObjectType.Face, filter, pickPrompt)
        //            : uiDoc.Selection.PickObject(ObjectType.Face, pickPrompt);

        //        Element hostElement = uiDoc.Document.GetElement(reference);
        //        Face face = hostElement.GetGeometryObjectFromReference(reference) as Face;

        //        return new FacePickResult
        //        {
        //            IsSuccess = true,
        //            Reference = reference,
        //            Face = face,
        //            HostElement = hostElement,
        //            IsPlanar = face is PlanarFace,
        //            PlanarFace = face as PlanarFace
        //        };
        //    }
        //    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        //    {
        //        return FacePickResult.Cancelled;
        //    }
        //    catch (Exception ex)
        //    {
        //        return FacePickResult.Failed(ex.Message);
        //    }
        //}

        // =========================================================
        // 拾取点
        // =========================================================

        /// <summary>
        /// 拾取空间中的一个点
        /// </summary>
        public static XYZ PickPoint(UIDocument uiDoc,
            string pickPrompt = "请在视图中拾取一个点", ObjectSnapTypes snapTypes = ObjectSnapTypes.None)
        {
            try
            {
                return snapTypes == ObjectSnapTypes.None
                    ? uiDoc.Selection.PickPoint(pickPrompt)
                    : uiDoc.Selection.PickPoint(snapTypes, pickPrompt);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// 连续拾取两个点，返回由这两点构成的线
        /// </summary>
        public static Line PickLine(UIDocument uiDoc,
            string firstPointPrompt = "请拾取起点", string secondPointPrompt = "请拾取终点")
        {
            try
            {
                XYZ startPoint = uiDoc.Selection.PickPoint(firstPointPrompt);
                if (startPoint == null) return null;

                XYZ endPoint = uiDoc.Selection.PickPoint(secondPointPrompt);
                if (endPoint == null) return null;

                if (startPoint.IsAlmostEqualTo(endPoint))
                {
                    TaskDialog.Show("提示", "起点和终点不能相同！");
                    return null;
                }

                return Line.CreateBound(startPoint, endPoint);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }

        // =========================================================
        // 高亮显示
        // =========================================================

        /// <summary>
        /// 在视图中高亮显示指定元素
        /// </summary>
        public static void HighlightElements(UIDocument uiDoc, IEnumerable<ElementId> elementIds)
        {
            var idList = elementIds?.Where(id => id != null).ToList();
            if (idList == null || idList.Count == 0) return;
            uiDoc.Selection.SetElementIds(idList);
        }

        /// <summary>
        /// 在视图中高亮显示指定元素
        /// </summary>
        public static void HighlightElements(UIDocument uiDoc, IEnumerable<Element> elements)
        {
            HighlightElements(uiDoc, elements?.Select(e => e?.Id));
        }

        /// <summary>
        /// 清除当前所有高亮选中状态
        /// </summary>
        public static void ClearSelection(UIDocument uiDoc)
        {
            uiDoc.Selection.SetElementIds(new List<ElementId>());
        }

        // =========================================================
        // 预选集验证工具
        // =========================================================

        ///// <summary>
        ///// 验证当前预选集的状态
        ///// </summary>
        //public static SelectionState CheckSelectionState(UIDocument uiDoc)
        //{
        //    var count = uiDoc.Selection.GetElementIds().Count;
        //    if (count == 0) return SelectionState.Empty;
        //    if (count == 1) return SelectionState.Single;
        //    return SelectionState.Multiple;
        //}
    }

}
