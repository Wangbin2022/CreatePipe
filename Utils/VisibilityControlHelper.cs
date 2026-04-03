using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    public static class VisibilityControlHelper
    {
        #region 基础通用方法 
        /// <summary>
        /// 检查当前视图是否允许修改可见性。
        /// 如果被视图样板锁定，会自动弹出警告并返回 false；否则返回 true。
        /// </summary>
        public static bool CanModifyViewVisibility(View view)
        {
            if (view.ViewTemplateId != ElementId.InvalidElementId)
            {
                TaskDialog.Show("拦截提示", "当前视图被视图样板锁定，无法通过插件修改可见性。\n\n请先在属性面板中将“视图样板”设置为“<无>”！");
                return false; // 不允许修改
            }
            return true; // 允许修改
        }
        /// <summary>
        /// 批量控制指定内置类别(BuiltInCategory)的显示/隐藏状态
        /// </summary>
        public static void SetCategoriesVisibility(Document doc, View view, IEnumerable<BuiltInCategory> builtInCats, bool visible)
        {
            foreach (var bCat in builtInCats)
            {
                Category cat = Category.GetCategory(doc, bCat);
                if (cat != null && view.CanCategoryBeHidden(cat.Id))
                {
                    view.SetCategoryHidden(cat.Id, !visible); // true 为隐藏，false 为显示
                }
            }
        }
        /// <summary>
        /// 一键切换（Toggle）指定内置类别的可见性状态 (关变开，开变关)
        /// </summary>
        public static void ToggleCategoriesVisibility(Document doc, View view, IEnumerable<BuiltInCategory> builtInCats)
        {
            if (!builtInCats.Any()) return;
            // 以传入的第一个类别当前状态为基准
            Category firstCat = Category.GetCategory(doc, builtInCats.First());
            if (firstCat != null && view.CanCategoryBeHidden(firstCat.Id))
            {
                bool isCurrentlyHidden = view.GetCategoryHidden(firstCat.Id);
                // 批量设置为相反状态
                SetCategoriesVisibility(doc, view, builtInCats, isCurrentlyHidden);
            }
        }
        #endregion
        public static void SetAllCategoriesVisibility(Document doc, View view, bool visible)
        {
            foreach (Category cat in doc.Settings.Categories)
            {
                if (view.CanCategoryBeHidden(cat.Id))
                {
                    view.SetCategoryHidden(cat.Id, !visible);
                }
            }
        }
        public static void SetTargetCategoriesVisibility(View view, IEnumerable<ElementId> categoryIds, bool visible)
        {
            foreach (ElementId catId in categoryIds)
            {
                if (view.CanCategoryBeHidden(catId))
                {
                    view.SetCategoryHidden(catId, !visible);
                }
            }
        }
    }
    // public class VisibilityHelper
    //{
    //    /// <summary>
    //    /// 方法1：主干逻辑 - 根据用户的选择，隔离对应的类别
    //    /// </summary>
    //    public static void IsolateCategoryBySelection(UIDocument uiDoc, bool isMultipleSelect = true)
    //    {
    //        Document doc = uiDoc.Document;
    //        View activeView = doc.ActiveView;
    //        // 1. 获取用户选择的图元
    //        IList<Reference> refs = isMultipleSelect
    //            ? uiDoc.Selection.PickObjects(ObjectType.Element, "请框选要保留类别的图元")
    //            : new List<Reference> { uiDoc.Selection.PickObject(ObjectType.Element, "请点选要保留类别的图元") };
    //        // 2. 提取选中图元的唯一类别ID（使用 HashSet 防止重复）
    //        HashSet<ElementId> targetCategoryIds = new HashSet<ElementId>();
    //        foreach (Reference r in refs)
    //        {
    //            Category cat = doc.GetElement(r).Category;
    //            if (cat != null)
    //            {
    //                targetCategoryIds.Add(cat.Id);
    //            }
    //        }
    //        // 3. 开启事务修改视图可见性
    //        using (Transaction t = new Transaction(doc, "按类别隔离图元"))
    //        {
    //            t.Start();
    //            // 4. 先隐藏当前视图中所有支持隐藏的类别
    //            HideAllCategories(doc, activeView);
    //            // 5. 将选中的类别重新设为可见
    //            SetCategoriesVisible(activeView, targetCategoryIds, true);
    //            t.Commit();
    //        }
    //    }
    //    /// <summary>
    //    /// 方法2：辅助逻辑 - 隐藏视图中的所有类别
    //    /// </summary>
    //    private static void HideAllCategories(Document doc, View view)
    //    {
    //        foreach (Category cat in doc.Settings.Categories)
    //        {
    //            // 检查该类别在当前视图中是否允许被隐藏
    //            if (view.CanCategoryBeHidden(cat.Id))
    //            {
    //                view.SetCategoryHidden(cat.Id, true); // true 表示隐藏
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 方法3：辅助逻辑 - 批量设置指定类别的可见性
    //    /// </summary>
    //    private static void SetCategoriesVisible(View view, IEnumerable<ElementId> categoryIds, bool visible)
    //    {
    //        foreach (ElementId catId in categoryIds)
    //        {
    //            if (view.CanCategoryBeHidden(catId))
    //            {
    //                view.SetCategoryHidden(catId, !visible); // 第二个参数是 "hide" (是否隐藏)，所以取反
    //            }
    //        }
    //    }
    //}
}
