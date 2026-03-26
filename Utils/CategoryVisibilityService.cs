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
    //0320注意自带事务处理，功能无需开启事务
    public static class CategoryVisibilityService
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

        #region 业务复合方法（根据之前逻辑集成）
        /// <summary>
        /// 隔离选中图元的类别
        /// </summary>
        public static void IsolateCategoryBySelection(UIDocument uiDoc, bool isMultipleSelect = true)
        {
            Document doc = uiDoc.Document;
            View activeView = doc.ActiveView;
            IList<Reference> refs = isMultipleSelect
                ? uiDoc.Selection.PickObjects(ObjectType.Element, "请框选要保留类别的图元")
                : new List<Reference> { uiDoc.Selection.PickObject(ObjectType.Element, "请点选要保留类别的图元") };
            HashSet<ElementId> targetCategoryIds = new HashSet<ElementId>();
            foreach (Reference r in refs)
            {
                Category cat = doc.GetElement(r).Category;
                if (cat != null) targetCategoryIds.Add(cat.Id);
            }
            using (Transaction t = new Transaction(doc, "隔离类别"))
            {
                t.Start();
                SetAllCategoriesVisibility(doc, activeView, false);
                SetTargetCategoriesVisibility(activeView, targetCategoryIds, true);
                t.Commit();
            }
        }
        /// <summary>
        /// 一键显示所有类别
        /// </summary>
        public static void ShowAllCategories(Document doc)
        {
            using (Transaction t = new Transaction(doc, "显示所有类别"))
            {
                t.Start();
                SetAllCategoriesVisibility(doc, doc.ActiveView, true);
                t.Commit();
            }
        }
        #endregion

        #region 私有辅助方法
        private static void SetAllCategoriesVisibility(Document doc, View view, bool visible)
        {
            foreach (Category cat in doc.Settings.Categories)
            {
                if (view.CanCategoryBeHidden(cat.Id))
                {
                    view.SetCategoryHidden(cat.Id, !visible);
                }
            }
        }
        private static void SetTargetCategoriesVisibility(View view, IEnumerable<ElementId> categoryIds, bool visible)
        {
            foreach (ElementId catId in categoryIds)
            {
                if (view.CanCategoryBeHidden(catId))
                {
                    view.SetCategoryHidden(catId, !visible);
                }
            }
        }
        #endregion
    }
}
