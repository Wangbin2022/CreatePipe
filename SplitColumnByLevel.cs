using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class SplitColumnByLevel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            try
            {
                List<Level> allLevelsInModel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                if (!allLevelsInModel.Any())
                {
                    TaskDialog.Show("错误", "模型中未找到任何标高。");
                    return Result.Cancelled;
                }
                List<string> uniqueLevelNames = allLevelsInModel.Select(item => item.Name).Distinct().OrderBy(name => name).ToList();
                UniversalComboBoxMultiSelection boxMultiSelection = new UniversalComboBoxMultiSelection(uniqueLevelNames, "请选择要切分标高，实体不要成组");
                boxMultiSelection.Title = "标高选择";
                bool? dialogResult = boxMultiSelection.ShowDialog();

                if (dialogResult != true || !boxMultiSelection.SelectedResult.Any())
                {
                    TaskDialog.Show("操作取消", "用户已取消或未选择任何标高。");
                    return Result.Cancelled;
                }
                List<string> selectedLevelNames = boxMultiSelection.SelectedResult;
                List<Level> selectedLevels = allLevelsInModel.Where(level => selectedLevelNames.Contains(level.Name))
                    .GroupBy(l => l.Elevation).Select(g => g.First()).OrderBy(l => l.Elevation).ToList();

                var structuralColumnFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
                var architecturalColumnFilter = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
                var columnFilter = new LogicalOrFilter(structuralColumnFilter, architecturalColumnFilter);

                List<FamilyInstance> allColumns = new FilteredElementCollector(doc).WherePasses(columnFilter)
                    .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();

                // 筛选出垂直柱
                List<FamilyInstance> verticalColumns = allColumns.Where(c => IsVerticalColumn(c)).ToList();
                int processedColumnCount = 0;
                int newSegmentsCreated = 0;

                using (TransactionGroup transGroup = new TransactionGroup(doc, "批量切分柱子"))
                {
                    transGroup.Start();
                    foreach (var column in verticalColumns)
                    {
                        if (!column.IsValidObject) continue;
                        // 无法确定柱子范围，跳过
                        if (!TryGetColumnExtents(column, out double bottomZ, out double topZ)) continue;
                        // 筛选出穿过当前柱子的有效标高
                        List<Level> relevantLevels = selectedLevels
                            .Where(l => l.Elevation > bottomZ + 0.001 && l.Elevation < topZ - 0.001)
                            .ToList();
                        // 没有标高穿过此柱，无需处理
                        if (!relevantLevels.Any()) continue;
                        using (Transaction trans = new Transaction(doc, "切分单个柱"))
                        {
                            trans.Start();
                            try
                            {
                                LocationPoint columnLocation = column.Location as LocationPoint;
                                FamilySymbol columnSymbol = column.Symbol;

                                // 获取原始柱的完整约束信息
                                Level originalBaseLevel = doc.GetElement(column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId()) as Level;
                                double originalBaseOffset = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble();
                                Level originalTopLevel = doc.GetElement(column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()) as Level;
                                double originalTopOffset = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble();

                                // 初始化循环变量
                                Level currentBaseLevel = originalBaseLevel;
                                double currentBaseOffset = originalBaseOffset;

                                // 循环创建中间的柱段
                                foreach (Level splitLevel in relevantLevels)
                                {
                                    // 创建新柱段
                                    FamilyInstance newSegment = doc.Create.NewFamilyInstance(columnLocation.Point, columnSymbol, currentBaseLevel, StructuralType.Column);
                                    // 设置新柱段的底部约束
                                    newSegment.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Set(currentBaseLevel.Id);
                                    newSegment.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(currentBaseOffset);
                                    // 设置新柱段的顶部约束
                                    newSegment.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(splitLevel.Id);
                                    newSegment.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(0);
                                    newSegmentsCreated++;

                                    // 更新下一个柱段的基准
                                    currentBaseLevel = splitLevel;
                                    currentBaseOffset = 0; // 中间段的底部偏移总是0
                                }

                                // **关键：创建最后一个柱段 (从最后一个切分标高到原始柱顶)**
                                FamilyInstance finalSegment = doc.Create.NewFamilyInstance(columnLocation.Point, columnSymbol, currentBaseLevel, StructuralType.Column);
                                finalSegment.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Set(currentBaseLevel.Id);
                                finalSegment.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(currentBaseOffset);
                                finalSegment.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(originalTopLevel.Id);
                                finalSegment.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(originalTopOffset);
                                newSegmentsCreated++;

                                // 删除原始柱子
                                doc.Delete(column.Id);
                                processedColumnCount++;
                                trans.Commit();
                            }
                            catch (Exception)
                            {
                                trans.RollBack();
                            }
                        }
                    }
                    transGroup.Assimilate();
                }
                TaskDialog.Show("操作完成", $"成功处理了 {processedColumnCount} 根垂直柱。共创建了 {newSegmentsCreated}个新柱段。");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message.ToString());
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        /// <summary>
        /// 检查一个柱子是否是垂直的（排除斜柱）
        /// </summary>
        private bool IsVerticalColumn(FamilyInstance column)
        {
            // 建筑柱总是垂直的
            if (column.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Columns) return true;
            // 对于结构柱，检查其“柱样式”参数
            if (column.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns)
            {
                // 0 代表 "Vertical"
                Parameter param = column.get_Parameter(BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM);
                if (param != null && param.AsInteger() == 0) return true;
            }
            return false;
        }
        /// <summary>
        /// **优化改进**：获取柱子的实际底部和顶部Z坐标
        /// </summary>
        /// <returns>如果成功获取则返回true</returns>
        private bool TryGetColumnExtents(FamilyInstance column, out double bottomZ, out double topZ)
        {
            bottomZ = 0;
            topZ = 0;
            Parameter baseLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            Parameter topLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
            Parameter baseOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            Parameter topOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

            if (baseLevelParam == null || topLevelParam == null || baseOffsetParam == null || topOffsetParam == null)
                return false;
            ElementId baseLevelId = baseLevelParam.AsElementId();
            ElementId topLevelId = topLevelParam.AsElementId();

            if (baseLevelId == ElementId.InvalidElementId || topLevelId == ElementId.InvalidElementId) return false;
            Level baseLevel = column.Document.GetElement(baseLevelId) as Level;
            Level topLevel = column.Document.GetElement(topLevelId) as Level;

            if (baseLevel == null || topLevel == null) return false;
            double baseOffset = baseOffsetParam.AsDouble();
            double topOffset = topOffsetParam.AsDouble();
            bottomZ = baseLevel.Elevation + baseOffset;
            topZ = topLevel.Elevation + topOffset;
            return true;
        }
    }
}
