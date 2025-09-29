using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class SplitWallByLevel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            List<Wall> verticalWalls = new List<Wall>();
            var wallRef = uiDoc.Selection.PickObjects(ObjectType.Element, new filterWallClass(), "选择结构柱");
            foreach (var item in wallRef)
            {
                Wall wall = doc.GetElement(item) as Wall;
                verticalWalls.Add(wall);
            }
            try
            {
                // --- (步骤 1 & 2: 获取标高和筛选垂直墙的代码保持不变) ---
                List<Level> allLevelsInModel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                if (!allLevelsInModel.Any()) { /* ... */ return Result.Cancelled; }
                List<string> uniqueLevelNames = allLevelsInModel.Select(item => item.Name).Distinct().OrderBy(name => name).ToList();
                UniversalComboBoxMultiSelection boxMultiSelection = new UniversalComboBoxMultiSelection(uniqueLevelNames, "请选择一个或多个用于切分的标高：");
                boxMultiSelection.Title = "标高选择";
                bool? dialogResult = boxMultiSelection.ShowDialog();
                if (dialogResult != true || !boxMultiSelection.SelectedResult.Any()) { return Result.Cancelled; }
                List<string> selectedLevelNames = boxMultiSelection.SelectedResult;
                List<Level> selectedLevels = allLevelsInModel
                    .Where(level => selectedLevelNames.Contains(level.Name))
                    .GroupBy(l => l.Elevation).Select(g => g.First())
                    .OrderBy(l => l.Elevation).ToList();

                int processedWallCount = 0;
                int newSegmentsCreated = 0;
                int insertsRecreatedCount = 0;

                // --- 步骤 3: 核心切分逻辑 (已重构) ---
                using (TransactionGroup transGroup = new TransactionGroup(doc, "批量切分墙体并重建洞口"))
                {
                    transGroup.Start();

                    foreach (var wall in verticalWalls)
                    {
                        if (!wall.IsValidObject || wall.IsStackedWall) continue;

                        if (!TryGetWallExtents(wall, out double bottomZ, out double topZ)) continue;

                        List<Level> relevantLevels = selectedLevels
                            .Where(l => l.Elevation > bottomZ + 0.001 && l.Elevation < topZ - 0.001)
                            .ToList();

                        if (!relevantLevels.Any()) continue;

                        using (Transaction trans = new Transaction(doc, "切分墙体并重建洞口"))
                        {
                            trans.Start();
                            try
                            {
                                // **第1步: 数据采集 - 存储所有洞口信息**
                                List<InsertData> insertDataList = GatherInsertData(wall);

                                // **第2步: 墙体重建**
                                // 保存原始墙属性
                                LocationCurve wallLocation = wall.Location as LocationCurve;
                                Curve originalCurve = wallLocation.Curve;
                                ElementId originalWallTypeId = wall.WallType.Id;
                                bool isFlipped = wall.Flipped;
                                Level baseLevel = doc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId()) as Level;
                                double baseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();

                                // 删除原始墙
                                doc.Delete(wall.Id);

                                // 创建所有新的墙段
                                List<Wall> newWallSegments = new List<Wall>();
                                Level currentBaseLevel = baseLevel;
                                double currentBaseOffset = baseOffset;

                                // 创建直到最后一个切分标高的所有墙段
                                foreach (var splitLevel in relevantLevels)
                                {
                                    Wall newSegment = Wall.Create(doc, originalCurve, originalWallTypeId, currentBaseLevel.Id, 10, 0, isFlipped, false);
                                    newSegment.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(currentBaseOffset);
                                    newSegment.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(splitLevel.Id);
                                    newSegmentsCreated++;
                                    newWallSegments.Add(newSegment);

                                    // 更新下一个墙段的基准
                                    currentBaseLevel = splitLevel;
                                    currentBaseOffset = 0; // 中间段的底部偏移总是0
                                }

                                // 创建最后一个墙段（从最后一个切分标高到原始墙顶）
                                Wall finalSegment = Wall.Create(doc, originalCurve, originalWallTypeId, currentBaseLevel.Id, topZ - currentBaseLevel.Elevation, 0, isFlipped, false);
                                newSegmentsCreated++;
                                newWallSegments.Add(finalSegment);

                                // **第3步: 洞口重建**
                                insertsRecreatedCount += RecreateInserts(doc, insertDataList, newWallSegments);

                                processedWallCount++;
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
                // --- 步骤 4: 结果报告 (更新) ---
                string summaryMessage = $"成功处理了 {processedWallCount} 面垂直墙。" + $"共创建了 {newSegmentsCreated} 个新墙段。" +
                                  $"并成功重新创建了 {insertsRecreatedCount} 个门窗洞口。";
                TaskDialog.Show("操作完成", summaryMessage);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        /// <summary>
        /// 一个简单的数据结构，用于在重建墙体期间临时存储门/窗/洞口的信息。
        /// </summary>
        private class InsertData
        {
            public XYZ Location { get; set; }
            public FamilySymbol Symbol { get; set; }
            public Level Level { get; set; }
            public bool IsHandFlipped { get; set; }
            public bool IsFacingFlipped { get; set; }
        }
        /// <summary>
        /// **新增**: 采集墙上所有洞口（门、窗、常规开洞）的关键信息。
        /// </summary>
        private List<InsertData> GatherInsertData(Wall wall)
        {
            var dataList = new List<InsertData>();
            // FindInserts 获取门、窗、常规开洞和幕墙嵌入物
            IList<ElementId> insertIds = wall.FindInserts(true, true, true, true);
            foreach (ElementId id in insertIds)
            {
                FamilyInstance instance = wall.Document.GetElement(id) as FamilyInstance;
                if (instance == null) continue;
                LocationPoint locPoint = instance.Location as LocationPoint;
                if (locPoint == null) continue;
                dataList.Add(new InsertData
                {
                    Location = locPoint.Point,
                    Symbol = instance.Symbol,
                    Level = wall.Document.GetElement(instance.LevelId) as Level,
                    IsHandFlipped = instance.HandFlipped,
                    IsFacingFlipped = instance.FacingFlipped
                });
            }
            return dataList;
        }
        /// <summary>
        /// **新增**: 在新的墙段上重新创建洞口。
        /// </summary>
        /// <returns>成功创建的洞口数量</returns>
        /// <summary>
        /// **新增**: 在新的墙段上重新创建洞口。
        /// </summary>
        /// <returns>成功创建的洞口数量</returns>
        private int RecreateInserts(Document doc, List<InsertData> insertsData, List<Wall> wallSegments)
        {
            const double Epsilon = 1e-9; // 定义一个极小的容差值来处理浮点数精度问题
            int createdCount = 0;
            foreach (var data in insertsData)
            {
                // 使用 for 循环以便我们能判断是否为最后一个墙段
                for (int i = 0; i < wallSegments.Count; i++)
                {
                    var segment = wallSegments[i];
                    bool isLastSegment = (i == wallSegments.Count - 1);
                    if (TryGetWallExtents(segment, out double bottomZ, out double topZ))
                    {
                        bool isInRange;
                        // 如果是最后一个墙段，则包含其顶部边界
                        if (isLastSegment)
                        {
                            isInRange = (data.Location.Z >= bottomZ - Epsilon && data.Location.Z <= topZ + Epsilon);
                        }
                        // 否则，不包含其顶部边界，以确保洞口被放置在其上方的墙段中
                        else
                        {
                            isInRange = (data.Location.Z >= bottomZ - Epsilon && data.Location.Z < topZ - Epsilon);
                        }
                        if (isInRange)
                        {
                            FamilyInstance newInsert = doc.Create.NewFamilyInstance(data.Location, data.Symbol, segment, data.Level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            if (data.IsHandFlipped)
                            {
                                newInsert.flipHand();
                            }
                            if (data.IsFacingFlipped)
                            {
                                newInsert.flipFacing();
                            }
                            createdCount++;
                            break; // 找到宿主后，立即跳出内层循环，避免重复创建
                        }
                    }
                }
            }
            return createdCount;
        }
        /// <summary>
        /// 检查墙体是否垂直（排除斜墙）
        /// </summary>
        private bool IsVerticalWall(Wall wall)
        {
            // 垂直墙的法线向量的Z分量应该为0
            if (wall == null || wall.WallType == null) return false;
            return !wall.IsStackedWall
                && wall.WallType.Kind != WallKind.Curtain // **新增：排除幕墙**
                && Math.Abs(wall.Orientation.Z) < 0.001;
        }
        /// <summary>
        /// 获取墙体的实际底部和顶部Z坐标
        /// </summary>
        private bool TryGetWallExtents(Wall wall, out double bottomZ, out double topZ)
        {
            bottomZ = 0;
            topZ = 0;
            Parameter baseLevelParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
            Parameter topConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            Parameter topOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
            Parameter unconnectedHeightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

            if (baseLevelParam == null || baseOffsetParam == null) return false;
            Level baseLevel = wall.Document.GetElement(baseLevelParam.AsElementId()) as Level;
            if (baseLevel == null) return false;
            double baseOffset = baseOffsetParam.AsDouble();
            bottomZ = baseLevel.Elevation + baseOffset;

            // 墙体高度可以是约束到标高，也可以是未连接高度
            if (topConstraintParam != null && topConstraintParam.AsElementId() != ElementId.InvalidElementId)
            {
                Level topLevel = wall.Document.GetElement(topConstraintParam.AsElementId()) as Level;
                if (topLevel != null && topOffsetParam != null)
                {
                    topZ = topLevel.Elevation + topOffsetParam.AsDouble();
                    return true;
                }
            }
            if (unconnectedHeightParam != null)
            {
                topZ = bottomZ + unconnectedHeightParam.AsDouble();
                return true;
            }
            return false;
        }
    }
}
