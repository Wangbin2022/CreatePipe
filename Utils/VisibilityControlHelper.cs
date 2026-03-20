using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
     public class VisibilityHelper
    {
        /// <summary>
        /// 方法1：主干逻辑 - 根据用户的选择，隔离对应的类别
        /// </summary>
        public static void IsolateCategoryBySelection(UIDocument uiDoc, bool isMultipleSelect = true)
        {
            Document doc = uiDoc.Document;
            View activeView = doc.ActiveView;
            // 1. 获取用户选择的图元
            IList<Reference> refs = isMultipleSelect
                ? uiDoc.Selection.PickObjects(ObjectType.Element, "请框选要保留类别的图元")
                : new List<Reference> { uiDoc.Selection.PickObject(ObjectType.Element, "请点选要保留类别的图元") };
            // 2. 提取选中图元的唯一类别ID（使用 HashSet 防止重复）
            HashSet<ElementId> targetCategoryIds = new HashSet<ElementId>();
            foreach (Reference r in refs)
            {
                Category cat = doc.GetElement(r).Category;
                if (cat != null)
                {
                    targetCategoryIds.Add(cat.Id);
                }
            }
            // 3. 开启事务修改视图可见性
            using (Transaction t = new Transaction(doc, "按类别隔离图元"))
            {
                t.Start();
                // 4. 先隐藏当前视图中所有支持隐藏的类别
                HideAllCategories(doc, activeView);
                // 5. 将选中的类别重新设为可见
                SetCategoriesVisible(activeView, targetCategoryIds, true);
                t.Commit();
            }
        }
        /// <summary>
        /// 方法2：辅助逻辑 - 隐藏视图中的所有类别
        /// </summary>
        private static void HideAllCategories(Document doc, View view)
        {
            foreach (Category cat in doc.Settings.Categories)
            {
                // 检查该类别在当前视图中是否允许被隐藏
                if (view.CanCategoryBeHidden(cat.Id))
                {
                    view.SetCategoryHidden(cat.Id, true); // true 表示隐藏
                }
            }
        }
        /// <summary>
        /// 方法3：辅助逻辑 - 批量设置指定类别的可见性
        /// </summary>
        private static void SetCategoriesVisible(View view, IEnumerable<ElementId> categoryIds, bool visible)
        {
            foreach (ElementId catId in categoryIds)
            {
                if (view.CanCategoryBeHidden(catId))
                {
                    view.SetCategoryHidden(catId, !visible); // 第二个参数是 "hide" (是否隐藏)，所以取反
                }
            }
        }
    }
}
