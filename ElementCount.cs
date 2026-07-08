using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreatePipe
{
    //0323 代码似乎有问题，检查简单模型但仍有7k+实例明显违背逻辑，待查
    [Transaction(TransactionMode.Manual)]
    public class ElementCount : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0421 构件分析测试 要排除固定的MEP相关配置项和系统材质、视图等，只管理手动添加的元素
            var analyzer = new ModelProfessionAnalyzer(doc);
            string report = analyzer.GetDetailedReport();
            TaskDialog.Show("分析结果", report);

            //////找出所有有几何instance并分类
            //List<Element> allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Cast<Element>().ToList();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in allInstances)
            //{
            //    if (item.HasPhases())
            //    {
            //        ids.Add(item.Id);
            //    }
            //}
            //uiDoc.Selection.SetElementIds(ids);

            ////只统计可见构件数量
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //IList<Element> physicalElements = collector.WhereElementIsNotElementType().Where(e => e.get_Geometry(new Options()) != null &&
            //                 e.Category != null && e.Category.HasMaterialQuantities).ToList();
            //int physicalElementCount = collector.GetElementCount();
            ////TaskDialog.Show("统计结果", $"模型中包含约 {physicalElementCount} 个物理模型实例。");
            //Dictionary<ElementId, int> symbolCounts = new Dictionary<ElementId, int>();
            //// 2. 遍历所有物理实例
            //foreach (Element elem in physicalElements)
            //{
            //    // 获取当前实例的类型ID
            //    ElementId typeId = elem.GetTypeId();

            //    // 检查类型ID是否有效
            //    if (typeId != null && typeId != ElementId.InvalidElementId)
            //    {
            //        // 如果字典中已经有这个类型ID，则数量加1
            //        if (symbolCounts.ContainsKey(typeId))
            //        {
            //            symbolCounts[typeId]++;
            //        }
            //        // 否则，将这个新的类型ID添加到字典中，数量设为1
            //        else
            //        {
            //            symbolCounts.Add(typeId, 1);
            //        }
            //    }
            //}
            //// --- 第三部分：【新增】统计实例所属的Category数量和分布 ---

            //// 1. 创建一个新字典，用于高效计数
            ////    键 (Key): Category 的 ElementId
            ////    值 (Value): 该 Category 下的实例数量 (int)
            //Dictionary<ElementId, int> categoryCounts = new Dictionary<ElementId, int>();

            //// 2. 遍历所有物理实例
            //foreach (Element elem in physicalElements)
            //{
            //    // 获取当前实例的Category
            //    Category category = elem.Category;

            //    // 检查Category是否有效
            //    if (category != null)
            //    {
            //        ElementId categoryId = category.Id;

            //        // 使用与统计Symbol时完全相同的字典逻辑
            //        if (categoryCounts.ContainsKey(categoryId))
            //        {
            //            categoryCounts[categoryId]++;
            //        }
            //        else
            //        {
            //            categoryCounts.Add(categoryId, 1);
            //        }
            //    }
            //}
            //// --- 第四部分：整合所有结果并格式化输出 ---
            //StringBuilder resultBuilder = new StringBuilder();
            //resultBuilder.AppendLine($"模型中包含约 {physicalElementCount} 个物理模型实例。");
            //resultBuilder.AppendLine("====================================");
            //resultBuilder.AppendLine(); // 添加空行增加可读性

            //// 添加Symbol统计结果
            //resultBuilder.AppendLine($"▶ 这些实例分属于 {symbolCounts.Keys.Count} 个不同的构件类型/族。");
            //resultBuilder.AppendLine("------------------------------------");
            //resultBuilder.AppendLine("【类型】数量统计 (按数量降序):");
            //var sortedSymbols = symbolCounts.OrderByDescending(kvp => kvp.Value);
            //int countToShow = 10; // 最多显示前10条
            //int currentCount = 0;
            //foreach (KeyValuePair<ElementId, int> pair in sortedSymbols)
            //{
            //    if (currentCount >= countToShow)
            //    {
            //        resultBuilder.AppendLine($"... 以及另外 {sortedSymbols.Count() - countToShow} 种类型。");
            //        break;
            //    }
            //    ElementType typeElement = doc.GetElement(pair.Key) as ElementType;
            //    if (typeElement != null)
            //    {
            //        string typeName = (typeElement is FamilySymbol symbol) ? $"{symbol.Family.Name} : {typeElement.Name}" : typeElement.Name;
            //        resultBuilder.AppendLine($" - {typeName}: {pair.Value} 个");
            //        currentCount++;
            //    }
            //}
            //resultBuilder.AppendLine();
            //resultBuilder.AppendLine();

            //// 【新增】添加Category统计结果
            //resultBuilder.AppendLine($"▶ 这些实例分布在 {categoryCounts.Keys.Count} 个不同的类别中。");
            //resultBuilder.AppendLine("------------------------------------");
            //resultBuilder.AppendLine("【类别】数量统计 (按数量降序):");
            //var sortedCategories = categoryCounts.OrderByDescending(kvp => kvp.Value);
            //foreach (KeyValuePair<ElementId, int> pair in sortedCategories)
            //{
            //    // Revit中Category没有直接的Element对象，但可以通过Id获取其信息
            //    // 注意：Category.GetCategory(doc, pair.Key) 是获取Category对象的标准方法
            //    Category categoryInfo = Category.GetCategory(doc, pair.Key);
            //    if (categoryInfo != null)
            //    {
            //        resultBuilder.AppendLine($" - {categoryInfo.Name}: {pair.Value} 个");
            //    }
            //}

            //// 最终显示整合后的对话框
            //TaskDialog.Show("模型详细统计报告", resultBuilder.ToString());
            return Result.Succeeded;
        }
    }
    /// <summary>
    /// 专业统计结果类
    /// </summary>
    public class ProfessionStatistic
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    /// <summary>
    /// 分析结果类
    /// </summary>
    public class ProfessionAnalysisResult
    {
        public int TotalElementCount { get; set; }
        public string PrimaryProfession { get; set; }
        public bool IsMultiDiscipline { get; set; }
        public Dictionary<BuiltInCategory, int> CategoryStatistics { get; set; }
        public Dictionary<string, ProfessionStatistic> ProfessionStatistics { get; set; }

        public ProfessionAnalysisResult()
        {
            CategoryStatistics = new Dictionary<BuiltInCategory, int>();
            ProfessionStatistics = new Dictionary<string, ProfessionStatistic>();
        }
    }
    /// <summary>
    /// 统计模型中各类别构件的数量，并分析模型的专业归属
    /// </summary>
    public class ModelProfessionAnalyzer
    {
        private readonly Document _doc;

        // 专业类别映射字典
        private readonly Dictionary<BuiltInCategory, string> _categoryToProfession;

        public ModelProfessionAnalyzer(Document doc)
        {
            _doc = doc;
            _categoryToProfession = InitializeCategoryMapping();
        }

        /// <summary>
        /// 初始化 BuiltInCategory 到专业的映射关系
        /// </summary>
        private Dictionary<BuiltInCategory, string> InitializeCategoryMapping()
        {
            return new Dictionary<BuiltInCategory, string>
        {
            // 建筑专业
            { BuiltInCategory.OST_Walls, "建筑" },
            { BuiltInCategory.OST_Doors, "建筑" },
            { BuiltInCategory.OST_Windows, "建筑" },
            { BuiltInCategory.OST_Rooms, "建筑" },
            { BuiltInCategory.OST_Floors, "建筑" },
            { BuiltInCategory.OST_Ceilings, "建筑" },
            { BuiltInCategory.OST_Stairs, "建筑" },
            { BuiltInCategory.OST_Ramps, "建筑" },
            { BuiltInCategory.OST_Railings, "建筑" },
            { BuiltInCategory.OST_CurtainWallMullions, "建筑" },
            { BuiltInCategory.OST_CurtainWallPanels, "建筑" },
            
            // 结构专业
            { BuiltInCategory.OST_StructuralColumns, "结构" },
            { BuiltInCategory.OST_StructuralFraming, "结构" },
            { BuiltInCategory.OST_StructuralFoundation, "结构" },
            { BuiltInCategory.OST_Rebar, "结构" },
            { BuiltInCategory.OST_Truss, "结构" },
            { BuiltInCategory.OST_StructuralBracePlanReps, "结构" },
            
            // 给排水专业
            { BuiltInCategory.OST_PipeCurves, "给排水" },
            { BuiltInCategory.OST_PipeFitting, "给排水" },
            { BuiltInCategory.OST_PipeAccessory, "给排水" },
            { BuiltInCategory.OST_PlumbingFixtures, "给排水" },
            { BuiltInCategory.OST_Sprinklers, "给排水" },
            
            // 暖通专业
            { BuiltInCategory.OST_DuctCurves, "暖通" },
            { BuiltInCategory.OST_DuctFitting, "暖通" },
            { BuiltInCategory.OST_DuctAccessory, "暖通" },
            { BuiltInCategory.OST_MechanicalEquipment, "暖通" },
            { BuiltInCategory.OST_DuctTerminal, "暖通" },
            { BuiltInCategory.OST_FlexDuctCurves, "暖通" },
            
            // 电气专业
            { BuiltInCategory.OST_Conduit, "电气" },
            { BuiltInCategory.OST_ConduitFitting, "电气" },
            { BuiltInCategory.OST_CableTray, "电气" },
            { BuiltInCategory.OST_CableTrayFitting, "电气" },
            { BuiltInCategory.OST_LightingFixtures, "电气" },
            { BuiltInCategory.OST_ElectricalEquipment, "电气" },
            { BuiltInCategory.OST_ElectricalFixtures, "电气" },
            { BuiltInCategory.OST_DataDevices, "电气" },
            { BuiltInCategory.OST_FireAlarmDevices, "电气" },
            { BuiltInCategory.OST_SecurityDevices, "电气" },
            { BuiltInCategory.OST_TelephoneDevices, "电气" },
            { BuiltInCategory.OST_Wire, "电气" },
            
            // 工艺专业
            { BuiltInCategory.OST_SpecialityEquipment, "工艺" },
            { BuiltInCategory.OST_GenericModel, "工艺" },
            //{ BuiltInCategory.OST_Entourage, "工艺" },
            
             //其他通用类别（归入"其他"）
            { BuiltInCategory.OST_Levels, "其他" },
            { BuiltInCategory.OST_Grids, "其他" },
            //{ BuiltInCategory.OST_Views, "其他" },
            //{ BuiltInCategory.OST_Sheets, "其他" },
            //{ BuiltInCategory.OST_Materials, "其他" },
            //{ BuiltInCategory.OST_ElectricalLoadClassifications, "其他" },
            //{ BuiltInCategory.OST_ParamElemElectricalLoadClassification, "其他" },
            //{ BuiltInCategory.OST_HVAC_Load_Space_Types, "其他" },
            //{ BuiltInCategory.OST_PreviewLegendComponents, "其他" }
        };
        }

        /// <summary>
        /// 执行分析，返回各专业构件数量及占比
        /// </summary>
        public ProfessionAnalysisResult Analyze()
        {
            var result = new ProfessionAnalysisResult();

            //var categoriesToExclude = new List<BuiltInCategory>
            //        {
            //            BuiltInCategory.OST_CenterLines,    BuiltInCategory.OST_CableTrayCenterLine,
            //            BuiltInCategory.OST_CableTrayFittingCenterLine,    BuiltInCategory.OST_PipeCurvesCenterLine,
            //            BuiltInCategory.OST_ConduitCenterLine,     BuiltInCategory.OST_ConduitFittingCenterLine,
            //            BuiltInCategory.OST_DuctCurvesCenterLine,     BuiltInCategory.OST_DuctFittingCenterLine,
            //            BuiltInCategory.OST_FlexDuctCurvesCenterLine,     BuiltInCategory.OST_FlexPipeCurvesCenterLine,
            //            BuiltInCategory.OST_PipeFittingCenterLine,     BuiltInCategory.OST_StairsSketchLandingCenterLines,
            //        };
            //var categoryIdsToExclude = categoriesToExclude.Select(c => new ElementId((int)c)).ToList();
            //// 3. 【关键步骤1】创建一个过滤器，用于【匹配】所有我们不想要的类别
            //ElementFilter filterForUnwantedCategories = new ElementMulticategoryFilter(categoryIdsToExclude);
            //// 4. 【关键步骤2】创建一个排除过滤器，它会【反转】上一步过滤器的结果
            //ElementFilter exclusionFilter = new ExclusionFilter(filterForUnwantedCategories);
            //// 5. 应用过滤器
            //var allPhysicalElements = new FilteredElementCollector(_doc)
            //    .WhereElementIsNotElementType() // 首先排除类型
            //    .WherePasses(exclusionFilter)   // 然后应用我们的排除过滤器
            //    .ToElements();

            var allPhysicalElements = new FilteredElementCollector(_doc)
    .WhereElementIsNotElementType() // 排除类型
                                    // 使用 LINQ 进行精确的二次过滤
    .Where(e =>
    {
        // 健壮性检查：确保元素有类别
        if (e.Category == null) return false;

        // 核心条件1：类别必须是“模型”类别。
        // 这一步就能完美排除掉 CenterLine, Grid, Level, ReferencePlane 等。
        if (e.Category.CategoryType != CategoryType.Model) return false;

        // 核心条件2：元素必须有几何包围盒。
        // 这可以排除掉一些空的、没有几何形状的“模型”元素。
        if (e.get_BoundingBox(null) == null) return false;

        // 你的附加条件：元素必须与阶段相关
        if (!e.HasPhases()) return false;

        return true;
    }).ToList();
    //.ToElements();

            //        var allPhysicalElements = new FilteredElementCollector(_doc)
            //.WhereElementIsNotElementType()          // 1. 排除类型元素，只取实例
            //.WhereElementIsViewIndependent()         // 2. 排除视图相关元素（如标注、尺寸标注等）
            //.Where(e =>
            //    // 3. 核心过滤 1：必须具有类别，且类别类型必须是“模型”
            //    // 这一步可以直接排掉 CenterLine(中心线)、Grid(轴网)、Level(标高)、ReferencePlane(参照平面) 等基准/注释元素
            //    e.Category != null &&
            //    e.Category.CategoryType == CategoryType.Model &&
            //    // 4. 核心过滤 2：必须具有真实的 3D 包围盒
            //    // 某些模型类别的空族实例可能没有几何体，通过包围盒检查可以进一步确保它是“实体”
            //    e.get_BoundingBox(null) != null &&
            //    // 5. 你的业务逻辑：必须包含阶段信息
            //    e.HasPhases());



            ////        //// 获取所有实体元素（排除视图、图纸等非实体类别）
            ////        //var allPhysicalElements = new FilteredElementCollector(_doc)
            ////        //    .WhereElementIsNotElementType()  // 排除类型元素，只取实例
            ////        //    .WhereElementIsViewIndependent() // 排除视图相关元素               
            ////        //    .Where(e => e.HasPhases());
            ////        ////.ToElements();
            ////        var categoriesToExclude = new List<BuiltInCategory>
            ////        {
            ////            BuiltInCategory.OST_Grids,    BuiltInCategory.OST_Levels,
            ////            BuiltInCategory.OST_ReferenceLines,    BuiltInCategory.OST_CenterLines,
            ////            BuiltInCategory.OST_Views,     BuiltInCategory.OST_SectionBox,
            ////        };
            ////        // 2. 将 BuiltInCategory 转换为 ElementId
            ////        var categoryIdsToExclude = categoriesToExclude.Select(c => new ElementId((int)c)).ToList();
            ////        // 3. 创建过滤器
            ////        var multiCategoryFilter = new ElementMulticategoryFilter(categoryIdsToExclude);
            ////        // 1. 必须有有效的包围盒（这是排除非实体元素的最快方法）2. 必须有类别3. 类别类型必须是模型（排除注释、分析等类别）
            ////        var allPhysicalElements = new FilteredElementCollector(_doc)
            ////            .WhereElementIsNotElementType()
            ////// 关键：排除掉我们不想要的类别
            ////.WherePasses(new LogicalAndFilter(new ElementIsElementTypeFilter(true), multiCategoryFilter))
            ////            .Where(e => e.get_BoundingBox(null) != null && e.Category != null &&
            ////            e.Category.CategoryType == CategoryType.Model)
            ////            ;
            //Options opt = new Options();
            //var allPhysicalElements = new FilteredElementCollector(_doc)
            //    .WhereElementIsNotElementType() 
            //    .Where(e => e.get_Geometry(opt) != null);

            int totalCount = 0;
            var categoryCountMap = new Dictionary<BuiltInCategory, int>();
            var professionCountMap = new Dictionary<string, int>();

            // 初始化专业计数字典
            foreach (var profession in new[] { "建筑", "结构", "给排水", "暖通", "电气", "工艺", "其他" })
            {
                professionCountMap[profession] = 0;
            }

            foreach (var element in allPhysicalElements)
            {
                // 获取元素的类别
                Category category = element.Category;
                if (category == null) continue;

                // 获取 BuiltInCategory 值
                BuiltInCategory bic = (BuiltInCategory)category.Id.IntegerValue;

                // 统计类别计数
                if (!categoryCountMap.ContainsKey(bic))
                    categoryCountMap[bic] = 0;
                categoryCountMap[bic]++;

                // 统计专业计数
                if (_categoryToProfession.TryGetValue(bic, out string profession))
                {
                    professionCountMap[profession]++;
                }
                else
                {
                    // 未映射的类别归入"其他"
                    professionCountMap["其他"]++;
                }

                totalCount++;
            }

            result.TotalElementCount = totalCount;
            result.CategoryStatistics = categoryCountMap;

            // 计算各专业占比
            foreach (var kvp in professionCountMap)
            {
                double percentage = totalCount > 0 ? (kvp.Value * 100.0 / totalCount) : 0;
                result.ProfessionStatistics.Add(kvp.Key, new ProfessionStatistic
                {
                    Count = kvp.Value,
                    Percentage = percentage
                });
            }

            // 确定模型的主要专业（占比最高的专业）
            result.PrimaryProfession = result.ProfessionStatistics
                .OrderByDescending(x => x.Value.Percentage)
                .First().Key;

            // 判断是否为综合模型（非主导专业占比超过15%）
            double topPercentage = result.ProfessionStatistics.Max(x => x.Value.Percentage);
            result.IsMultiDiscipline = topPercentage < 60;

            return result;
        }

        /// <summary>
        /// 获取详细的类别统计信息
        /// </summary>
        public string GetDetailedReport()
        {
            var result = Analyze();
            var report = new System.Text.StringBuilder();

            report.AppendLine("========== Revit 模型专业分析报告 ==========");
            report.AppendLine($"模型总构件数: {result.TotalElementCount}");
            report.AppendLine($"主要专业: {result.PrimaryProfession}");
            report.AppendLine($"是否综合模型: {(result.IsMultiDiscipline ? "是" : "否")}");
            report.AppendLine();
            report.AppendLine("各专业统计:");
            report.AppendLine("----------------------------------------");

            foreach (var stat in result.ProfessionStatistics.OrderByDescending(x => x.Value.Percentage))
            {
                report.AppendLine($"{stat.Key}: {stat.Value.Count} 个构件 ({stat.Value.Percentage:F2}%)");
            }

            report.AppendLine();
            report.AppendLine("主要类别明细 (Top 10):");
            report.AppendLine("----------------------------------------");

            var topCategories = result.CategoryStatistics
                .OrderByDescending(x => x.Value)
                .Take(10);

            foreach (var kvp in topCategories)
            {
                string categoryName = GetCategoryName(kvp.Key);
                report.AppendLine($"{categoryName}: {kvp.Value} 个");
            }

            return report.ToString();
        }

        /// <summary>
        /// 获取类别的显示名称
        /// </summary>
        private string GetCategoryName(BuiltInCategory bic)
        {
            try
            {
                Category category = Category.GetCategory(_doc, bic);
                return category?.Name ?? bic.ToString();
            }
            catch
            {
                return bic.ToString();
            }
        }
    }
}
