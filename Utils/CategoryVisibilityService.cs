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

        #region 私有辅助方法
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
        #endregion
    }
}
