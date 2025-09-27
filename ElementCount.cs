using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class ElementCount : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //只统计可见构件数量
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> physicalElements = collector.WhereElementIsNotElementType().Where(e => e.get_Geometry(new Options()) != null &&
                             e.Category != null && e.Category.HasMaterialQuantities).ToList();
            int physicalElementCount = collector.GetElementCount();
            //TaskDialog.Show("统计结果", $"模型中包含约 {physicalElementCount} 个物理模型实例。");
            Dictionary<ElementId, int> symbolCounts = new Dictionary<ElementId, int>();
            // 2. 遍历所有物理实例
            foreach (Element elem in physicalElements)
            {
                // 获取当前实例的类型ID
                ElementId typeId = elem.GetTypeId();

                // 检查类型ID是否有效
                if (typeId != null && typeId != ElementId.InvalidElementId)
                {
                    // 如果字典中已经有这个类型ID，则数量加1
                    if (symbolCounts.ContainsKey(typeId))
                    {
                        symbolCounts[typeId]++;
                    }
                    // 否则，将这个新的类型ID添加到字典中，数量设为1
                    else
                    {
                        symbolCounts.Add(typeId, 1);
                    }
                }
            }
            // --- 第三部分：【新增】统计实例所属的Category数量和分布 ---

            // 1. 创建一个新字典，用于高效计数
            //    键 (Key): Category 的 ElementId
            //    值 (Value): 该 Category 下的实例数量 (int)
            Dictionary<ElementId, int> categoryCounts = new Dictionary<ElementId, int>();

            // 2. 遍历所有物理实例
            foreach (Element elem in physicalElements)
            {
                // 获取当前实例的Category
                Category category = elem.Category;

                // 检查Category是否有效
                if (category != null)
                {
                    ElementId categoryId = category.Id;

                    // 使用与统计Symbol时完全相同的字典逻辑
                    if (categoryCounts.ContainsKey(categoryId))
                    {
                        categoryCounts[categoryId]++;
                    }
                    else
                    {
                        categoryCounts.Add(categoryId, 1);
                    }
                }
            }
            // --- 第四部分：整合所有结果并格式化输出 ---
            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.AppendLine($"模型中包含约 {physicalElementCount} 个物理模型实例。");
            resultBuilder.AppendLine("====================================");
            resultBuilder.AppendLine(); // 添加空行增加可读性

            // 添加Symbol统计结果
            resultBuilder.AppendLine($"▶ 这些实例分属于 {symbolCounts.Keys.Count} 个不同的构件类型/族。");
            resultBuilder.AppendLine("------------------------------------");
            resultBuilder.AppendLine("【类型】数量统计 (按数量降序):");
            var sortedSymbols = symbolCounts.OrderByDescending(kvp => kvp.Value);
            int countToShow = 10; // 最多显示前10条
            int currentCount = 0;
            foreach (KeyValuePair<ElementId, int> pair in sortedSymbols)
            {
                if (currentCount >= countToShow)
                {
                    resultBuilder.AppendLine($"... 以及另外 {sortedSymbols.Count() - countToShow} 种类型。");
                    break;
                }
                ElementType typeElement = doc.GetElement(pair.Key) as ElementType;
                if (typeElement != null)
                {
                    string typeName = (typeElement is FamilySymbol symbol) ? $"{symbol.Family.Name} : {typeElement.Name}" : typeElement.Name;
                    resultBuilder.AppendLine($" - {typeName}: {pair.Value} 个");
                    currentCount++;
                }
            }
            resultBuilder.AppendLine();
            resultBuilder.AppendLine();

            // 【新增】添加Category统计结果
            resultBuilder.AppendLine($"▶ 这些实例分布在 {categoryCounts.Keys.Count} 个不同的类别中。");
            resultBuilder.AppendLine("------------------------------------");
            resultBuilder.AppendLine("【类别】数量统计 (按数量降序):");
            var sortedCategories = categoryCounts.OrderByDescending(kvp => kvp.Value);
            foreach (KeyValuePair<ElementId, int> pair in sortedCategories)
            {
                // Revit中Category没有直接的Element对象，但可以通过Id获取其信息
                // 注意：Category.GetCategory(doc, pair.Key) 是获取Category对象的标准方法
                Category categoryInfo = Category.GetCategory(doc, pair.Key);
                if (categoryInfo != null)
                {
                    resultBuilder.AppendLine($" - {categoryInfo.Name}: {pair.Value} 个");
                }
            }

            // 最终显示整合后的对话框
            TaskDialog.Show("模型详细统计报告", resultBuilder.ToString());
            return Result.Succeeded;
        }
    }
}
