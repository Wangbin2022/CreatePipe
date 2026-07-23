using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreatePipe
{
    //0718 初步完成
    [Transaction(TransactionMode.Manual)]
    public class ElementCount : IExternalCommand
    {
        private Dictionary<string, ElementId> categoryDict = new Dictionary<string, ElementId>();
        private List<string> _rawCategoryNames = new List<string>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0717 改白名单收集控制类别，看原始已有类别查找代码
            FilteredElementCollector collector = null;
            ICollection<ElementId> idsToExclude = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WherePasses(new LogicalOrFilter(new ElementClassFilter(typeof(RevitLinkInstance)),
                new ElementClassFilter(typeof(ImportInstance)))).ToElementIds();
            // 1. 收集当前文档所有元素排除掉链接元素
            if (idsToExclude.Count() > 0)
            {
                ExclusionFilter exclusionFilter = new ExclusionFilter(idsToExclude);
                collector = new FilteredElementCollector(doc).WhereElementIsNotElementType().WherePasses(exclusionFilter);
            }
            else collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            categoryDict = new Dictionary<string, ElementId>();
            // 定义需要排除的类别名称（黑名单）
            HashSet<string> _excludedCategoryNames = new HashSet<string>
            {
                "<面积边界>","<房间分隔>","<空间分隔>",  "内部原点", "测量点", "项目基点", "主等高线","面积", "详图项目","中心线","HVAC 区","暖通空调分区","线","空间","房间","体量","体量楼层","组成部分","结构梁系统","结构区域钢筋",
                "结构钢筋","光栅图像","建筑地坪","家具系统","建筑红线","<隔热层线>","<行进路线>","支座","垂直循环","<基于区域的负荷边界>"
                // 如果有其他需要排除的，可以继续添加 
            };
            List<ElementId> countableIds = new List<ElementId>();
            foreach (Element elem in collector)
            {
                Category cat = elem.Category;
                // 过滤无效类别，并确保该类别在当前视图中是允许被隐藏的 + 模型类别过滤
                if (cat != null && activeView.CanCategoryBeHidden(cat.Id) && cat.CategoryType == CategoryType.Model)
                {
                    if (_excludedCategoryNames.Contains(cat.Name)) continue;
                    // 存入字典去重
                    if (!categoryDict.ContainsKey(cat.Name))
                    {
                        categoryDict.Add(cat.Name, cat.Id);
                        _rawCategoryNames.Add(cat.Name);
                    }
                    countableIds.Add(elem.Id);
                }
            }
            if (categoryDict.Count == 0)
            {
                TaskDialog.Show("提示", "当前模型中没有可统计类别构件！");
                return Result.Failed;
            }
            // 根据类别统计构件数量并选择操作，输出分析结果
            TaskDialog td = new TaskDialog("选择操作")
            {
                MainInstruction = "请选择输出查询结果类型:",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                CommonButtons = TaskDialogCommonButtons.Cancel
            };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "输出模型内构件类别组成分析结果");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "输出模型内构件专业分布分析结果");
            //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "导出构件按类别详细数量统计csv");
            TaskDialogResult tdRes = td.Show();
            if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
            //统计收集各类别总构件数，再次收集处理所有实例，是否可在上一步直接字典收集到ValueTuple
            //创建一个新字典，用于高效计数 键 (Key): Category 的 ElementId
            ////    值 (Value): 该 Category 下的实例数量 (int)
            Dictionary<ElementId, int> categoryCounts = new Dictionary<ElementId, int>();
            Dictionary<ElementId, int> symbolCounts = new Dictionary<ElementId, int>();
            foreach (Element elem in collector)
            {
                Category category = elem.Category;
                ElementId typeId = elem.GetTypeId();
                if (category != null && _rawCategoryNames.Contains(category.Name))
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
            }
            //只分析结果
            if (tdRes == TaskDialogResult.CommandLink1)
            {
                //TaskDialog.Show("tt", "Analysis");
                StringBuilder resultBuilder = new StringBuilder();
                resultBuilder.AppendLine($"模型中包含约 {countableIds.Count()} 个物理模型实例。");
                resultBuilder.AppendLine("====================================");
                resultBuilder.AppendLine();
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
                //// 最终显示整合后的对话框
                TaskDialog.Show("模型详细统计报告", resultBuilder.ToString());
            }
            //输出专业分析
            else if (tdRes == TaskDialogResult.CommandLink2)
            {
                var analyzer = new ModelProfessionAnalyzer(doc);
                string report = analyzer.GetDetailedReport();
                TaskDialog.Show("分析结果", report);
            }
            else
            {
                TaskDialog.Show("tt", "ExportCsv");
            }
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
            
            // 通用专业
            { BuiltInCategory.OST_SpecialityEquipment, "通用" },
            { BuiltInCategory.OST_GenericModel, "通用" },
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
            var allPhysicalElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                .Where(e =>
                {
                    // 健壮性检查：确保元素有类别
                    if (e.Category == null) return false;
                    if (e.Category.CategoryType != CategoryType.Model) return false;
                    // 核心条件2：元素必须有几何包围盒。
                    if (e.get_BoundingBox(null) == null) return false;
                    // 你的附加条件：元素必须与阶段相关
                    if (!e.HasPhases()) return false;
                    return true;
                }).ToList();
            int totalCount = 0;
            var categoryCountMap = new Dictionary<BuiltInCategory, int>();
            var professionCountMap = new Dictionary<string, int>();
            // 初始化专业计数字典
            foreach (var profession in new[] { "建筑", "结构", "给排水", "暖通", "电气", "通用", "其他" })
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
                .OrderByDescending(x => x.Value.Percentage).First().Key;
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
                .OrderByDescending(x => x.Value).Take(10);
            foreach (var kvp in topCategories)
            {
                //string categoryName = GetCategoryName(kvp.Key);
                string categoryName = string.Empty;
                //// 获取类别的显示名称
                try
                {
                    Category category = Category.GetCategory(_doc, kvp.Key);
                    categoryName = category?.Name ?? kvp.Key.ToString();
                }
                catch
                {
                    categoryName = kvp.Key.ToString();
                }
                report.AppendLine($"{categoryName}: {kvp.Value} 个");
            }
            return report.ToString();
        }
    }
}
