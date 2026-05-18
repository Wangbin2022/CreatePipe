using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.IO;

namespace CreatePipe.OfficalSamples
{
    internal class MaterialQuantities
    {
        public MaterialQuantities(Document doc)
        {
            const string filename = "CalculateMaterialQuantities.txt";
            var writer = new StreamWriter(filename);
            // 使用元组简化的类型调用
            ExecuteCalculations<WallMaterialQuantityCalculator>(doc, writer);
            ExecuteCalculations<FloorMaterialQuantityCalculator>(doc, writer);
            ExecuteCalculations<RoofMaterialQuantityCalculator>(doc, writer);
        }
        /// <summary>
        /// 执行指定类型的材质计算器
        /// </summary>
        private static void ExecuteCalculations<T>(Document doc, TextWriter writer)
            where T : MaterialQuantityCalculator, new()
        {
            var calculator = new T
            {
                Document = doc,
                OutputWriter = writer
            };
            calculator.CalculateMaterialQuantities();
            calculator.ReportResults();
        }
    }
    /// <summary>
    /// 墙体材质计算器
    /// </summary>
    class WallMaterialQuantityCalculator : MaterialQuantityCalculator
    {
        protected override void CollectElements() =>
            // 获取所有墙体实例（包含内建模型）
            m_elementsToProcess = new FilteredElementCollector(m_doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToElements();

        protected override string GetElementTypeName() => "Wall";
    }

    /// <summary>
    /// 楼板材质计算器
    /// </summary>
    class FloorMaterialQuantityCalculator : MaterialQuantityCalculator
    {
        protected override void CollectElements() =>
            m_elementsToProcess = new FilteredElementCollector(m_doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .ToElements();

        protected override string GetElementTypeName() => "Floor";
    }

    /// <summary>
    /// 屋顶材质计算器
    /// </summary>
    class RoofMaterialQuantityCalculator : MaterialQuantityCalculator
    {
        protected override void CollectElements() =>
            m_elementsToProcess = new FilteredElementCollector(m_doc)
                .OfCategory(BuiltInCategory.OST_Roofs)
                .WhereElementIsNotElementType()
                .ToElements();

        protected override string GetElementTypeName() => "Roof";
    }

    /// <summary>
    /// 材质工程量计算器基类
    /// </summary>
    abstract class MaterialQuantityCalculator
    {
        #region 属性字段
        protected IList<Element> m_elementsToProcess;
        protected Document m_doc;
        private TextWriter m_writer;

        // 使用out参数和元组简化数据传递
        private Dictionary<ElementId, Dictionary<ElementId, MaterialQuantitiesData>> m_quantitiesPerElement
            = new Dictionary<ElementId, Dictionary<ElementId, MaterialQuantitiesData>>();
        private Dictionary<ElementId, MaterialQuantitiesData> m_totalQuantities
            = new Dictionary<ElementId, MaterialQuantitiesData>();
        private bool m_calculatingGrossQuantities = false;
        private List<string> m_warnings = new List<string>();

        public Document Document
        {
            set => m_doc = value;
            get => m_doc;
        }

        public TextWriter OutputWriter
        {
            set => m_writer = value;
            get => m_writer;
        }
        #endregion

        #region 抽象方法
        protected abstract void CollectElements();
        protected abstract string GetElementTypeName();
        #endregion

        /// <summary>
        /// 执行完整计算流程
        /// </summary>
        public void CalculateMaterialQuantities()
        {
            CollectElements();
            CalculateNetMaterialQuantities();   // 计算净用量
            CalculateGrossMaterialQuantities(); // 计算毛用量
        }

        /// <summary>
        /// 计算所有构件的净用量
        /// </summary>
        private void CalculateNetMaterialQuantities()
        {
            foreach (var element in m_elementsToProcess)
            {
                ProcessElementMaterials(element);
            }
        }

        /// <summary>
        /// 计算毛用量（通过临时删除切割元素）
        /// </summary>
        private void CalculateGrossMaterialQuantities()
        {
            m_calculatingGrossQuantities = true;

            using (var trans = new Transaction(m_doc, "临时删除切割元素"))
            {
                trans.Start();
                DeleteAllCuttingElements(); // 删除所有洞口和门窗
                m_doc.Regenerate();

                foreach (var element in m_elementsToProcess)
                {
                    ProcessElementMaterials(element);
                }

                trans.RollBack(); // 回滚，恢复原始模型
            }
        }

        /// <summary>
        /// 删除所有切割目标构件的元素（洞口、门窗）
        /// </summary>
        private void DeleteAllCuttingElements()
        {
            // 构建过滤器：查找所有门窗实例和洞口
            var familyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));
            var windowFilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            var doorFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            var doorOrWindowFilter = new LogicalOrFilter(windowFilter, doorFilter);
            var doorWindowInstanceFilter = new LogicalAndFilter(doorOrWindowFilter, familyInstanceFilter);

            var openingFilter = new ElementClassFilter(typeof(Opening));
            var cuttingElementFilter = new LogicalOrFilter(openingFilter, doorWindowInstanceFilter);

            var cuttingElements = new FilteredElementCollector(m_doc)
                .WherePasses(cuttingElementFilter)
                .ToElements();

            foreach (var element in cuttingElements)
            {
                // 跳过幕墙系统中的门（无法删除且不影响计算结果）
                if (element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
                {
                    var door = element as FamilyInstance;
                    var hostWall = door?.Host as Wall;
                    if (hostWall?.CurtainGrid != null)
                        continue;
                }

                // 尝试删除，记录失败情况
                var deletedIds = m_doc.Delete(element.Id);
                if (deletedIds == null || deletedIds.Count == 0)
                {
                    m_warnings.Add($"无法删除 {element.GetType().Name} (ID:{element.Id.IntegerValue}, 名称:{element.Name})");
                }
            }
        }

        /// <summary>
        /// 处理单个构件的材质数据
        /// </summary>
        private void ProcessElementMaterials(Element element)
        {
            var elementId = element.Id;
            var materialIds = element.GetMaterialIds(false); // 获取材质ID列表

            foreach (var materialId in materialIds)
            {
                var volume = element.GetMaterialVolume(materialId);   // 体积(立方英尺)
                var area = element.GetMaterialArea(materialId, false); // 面积(平方英尺)

                if (volume <= 0.0 && area <= 0.0) continue;

                // 存储到总计字典
                StoreQuantities(materialId, volume, area, m_totalQuantities);

                // 存储到各构件字典
                if (!m_quantitiesPerElement.TryGetValue(elementId, out var perElementDict))
                {
                    perElementDict = new Dictionary<ElementId, MaterialQuantitiesData>();
                    m_quantitiesPerElement[elementId] = perElementDict;
                }
                StoreQuantities(materialId, volume, area, perElementDict);
            }
        }

        /// <summary>
        /// 存储材质工程量到指定字典
        /// </summary>
        private void StoreQuantities(ElementId materialId, double volume, double area,
                                     Dictionary<ElementId, MaterialQuantitiesData> dict)
        {
            // 使用TryGetValue和out变量简化代码
            if (!dict.TryGetValue(materialId, out var quantities))
            {
                quantities = new MaterialQuantitiesData();
                dict[materialId] = quantities;
            }

            // 根据当前模式更新对应数据
            if (m_calculatingGrossQuantities)
            {
                quantities.GrossVolume += volume;
                quantities.GrossArea += area;
            }
            else
            {
                quantities.NetVolume += volume;
                quantities.NetArea += area;
            }
        }

        /// <summary>
        /// 输出计算结果到文件
        /// </summary>
        public void ReportResults()
        {
            if (m_totalQuantities.Count == 0) return;

            const string legend = "Gross volume(cubic ft),Net volume(cubic ft),Gross area(sq ft),Net area(sq ft)";

            m_writer.WriteLine();
            m_writer.WriteLine($"{GetElementTypeName()} elements totals,{legend}");

            // 输出警告信息
            if (m_warnings.Count > 0)
            {
                m_writer.WriteLine("警告：毛用量计算可能不准确，原因如下：");
                foreach (var warning in m_warnings)
                    m_writer.WriteLine(warning);
                m_writer.WriteLine();
            }

            // 输出总计数据
            ReportMaterialQuantities(m_totalQuantities);

            // 输出各构件明细数据
            foreach (var kvp in m_quantitiesPerElement)
            {
                var element = m_doc.GetElement(kvp.Key);
                var safeName = element.Name.Replace(',', ':'); // CSV格式兼容

                m_writer.WriteLine();
                m_writer.WriteLine($"{GetElementTypeName()} element: {safeName} (ID:{kvp.Key.IntegerValue}),{legend}");
                ReportMaterialQuantities(kvp.Value);
            }
        }

        /// <summary>
        /// 输出材质工程量明细
        /// </summary>
        private void ReportMaterialQuantities(Dictionary<ElementId, MaterialQuantitiesData> quantities)
        {
            foreach (var kvp in quantities)
            {
                var material = m_doc.GetElement(kvp.Key) as Material;
                var qty = kvp.Value;
                var safeName = material?.Name.Replace(',', ':') ?? "Unknown";

                // 使用内插字符串简化输出
                m_writer.WriteLine($"{safeName},{qty.GrossVolume:F2},{qty.NetVolume:F2},{qty.GrossArea:F2},{qty.NetArea:F2}");
            }
        }
    }

    /// <summary>
    /// 材质工程量数据容器
    /// </summary>
    class MaterialQuantitiesData
    {
        public double GrossVolume { get; set; } // 毛用量体积
        public double GrossArea { get; set; }   // 毛用量面积
        public double NetVolume { get; set; }   // 净用量体积
        public double NetArea { get; set; }     // 净用量面积
    }
}
