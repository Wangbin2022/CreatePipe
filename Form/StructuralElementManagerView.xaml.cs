using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace CreatePipe.Form
{
    /// <summary>
    /// StructuralElementManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class StructuralElementManagerView : Window
    {
        public StructuralElementManagerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new StructuralElementManagerViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class StructuralElementManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<StructuralEntity>
    {
        public Document Doc { get; set; }
        public UIDocument UIDocument { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        private List<StructuralEntity> _cachedPipeSystems = new List<StructuralEntity>();
        public ObservableCollection<StructuralEntity> Collection { get; set; } = new ObservableCollection<StructuralEntity>();
        public StructuralElementManagerViewModel(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            UIDocument = uIApplication.ActiveUIDocument as UIDocument;
            InitFunc();
        }
        public void InitFunc()
        {
            QueryElement(null);

        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            Collection.Clear();
            var allInstances = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
    .Cast<FamilyInstance>().GroupBy(i => i.Symbol.Id).ToDictionary(g => g.Key, g => g.ToList());
            var symbols = new FilteredElementCollector(Doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
            .Where(s => s.Category != null &&
                       (s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns ||
                        s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming ||
                         s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation || s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralTruss))
            .ToList();
            var result = new List<StructuralEntity>();
            foreach (var sym in symbols)
            {
                string familyName = sym.FamilyName;
                if (string.IsNullOrWhiteSpace(text) ||
                    sym.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    familyName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var openInstances = allInstances.ContainsKey(sym.Id) ? allInstances[sym.Id] : new List<FamilyInstance>();
                    result.Add(new StructuralEntity(sym, ExternalHandler, openInstances));
                }
            }
            foreach (var item in result) Collection.Add(item);
        }
        public ICommand PickItemCommand => new RelayCommand<StructuralEntity>(PickItem);
        private void PickItem(StructuralEntity entity)
        {
            if (entity != null)
                UIDocument.Selection.SetElementIds(entity.instanceIds);
        }
        public ICommand SubViewCommand => new RelayCommand<StructuralEntity>(SubView);
        private static void SubView(StructuralEntity entity)
        {
            if (entity == null) return;
            Dictionary<string, string> instanceCount = entity.StructuralInstanceCount;
            UniversalDictionaryListView universalDictionaryList = new UniversalDictionaryListView(instanceCount, "分层统计");
            universalDictionaryList.ShowDialog();
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            if (selectedElements == null) return;
            List<StructuralEntity> selectedItems = selectedElements.Cast<StructuralEntity>().ToList();
            // 分别收集可删除和不可删除的类型
            var deletableItems = selectedItems.Where(item => item.InstanceCount == 0).ToList();
            var nonDeletableItems = selectedItems.Where(item => item.InstanceCount > 0).ToList();
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(Doc, "删除多个系统", (service) =>
                {
                    service.UpdateMax(deletableItems.Count);
                    int index = 0;
                    foreach (var item in deletableItems)
                    {
                        Doc.Delete(item.SymbolId);
                        service.Update(++index, item.SymbolName);
                    }
                    InitFunc();
                    TaskDialog.Show("tt", $"已删除选定的空类型{index}个，{nonDeletableItems.Count.ToString()}个使用中类型未删除");
                });
            });
        }
        ///// <summary>
        ///// 辅助方法：截断字符串以适应表格宽度
        ///// </summary>
        //private string TruncateString(string input, int maxLength)
        //{
        //    if (string.IsNullOrEmpty(input)) return string.Empty;
        //    if (input.Length <= maxLength) return input;
        //    return input.Substring(0, maxLength - 3) + "...";
        //}
        public ICommand TotalSystemCommand => new RelayCommand<IEnumerable<object>>(TotalSystemCount);
        private void TotalSystemCount(IEnumerable<object> selectedElements)
        {
            //List<StructuralEntity> selectedItems = selectedElements.Cast<StructuralEntity>().ToList();
            //if (selectedElements == null) return;
            // 1. 获取要统计的 StructuralEntity 列表
            List<StructuralEntity> selectedItems;
            if (selectedElements == null || !selectedElements.Any())
            {
                // 如果没有选择，从现有的所有 StructuralEntity 获取（需要从外部传入或获取）
                // 假设有一个全局的 StructuralEntities 集合
                selectedItems = selectedElements.Cast<StructuralEntity>().ToList();
                if (selectedItems == null || !selectedItems.Any())
                {
                    TaskDialog.Show("统计结果", "没有选择任何结构类型！");
                    return;
                }
            }
            else
            {
                selectedItems = selectedElements.Cast<StructuralEntity>().ToList();
            }
            // 构建统计报告
            var report = new StringBuilder();
            report.AppendLine("========== 结构构件统计报告 ==========");
            report.AppendLine($"统计时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"统计范围: {(selectedElements == null || !selectedElements.Any() ? "全部模型" : $"已选择 {selectedItems.Count} 个族类型")}");
            // ========== 第一部分：按类型统计 ==========
            report.AppendLine("【第一部分：按族Family统计】");
            //report.AppendLine(" 族名称       类型名称      │ 构件数量    │ 总长度(m)      │ 总体积(m³)   │");
            double grandTotalLength = 0;
            double grandTotalVolume = 0;
            int grandTotalInstanceCount = 0;
            int grandTotalSymbolCount = 0;
            // 按FamilyId分组
            var familiesGroup = selectedItems.GroupBy(item => item.FamilyId)
                .Select(group => new
                {
                    FamilyId = group.Key,
                    FamilyName = group.First().FamilyName,
                    SymbolCount = group.Select(item => item.SymbolId).Distinct().Count(),
                    TotalLength = group.Sum(item => item.TotalLength),
                    TotalVolume = group.Sum(item => item.TotalVolume),
                    TotalInstanceCount = group.Sum(item => item.InstanceCount)
                }).ToList();
            report.AppendLine($"{"族名称"} // {"族类型数"} // {"实例数"}//{"总长度"} // {"总体积"}");
            // 输出每个族的汇总数据
            foreach (var family in familiesGroup)
            {
                report.AppendLine($"│ {family.FamilyName}//{family.SymbolCount}//{family.TotalInstanceCount}//{family.TotalLength:F2}//{family.TotalVolume:F3} │");
                grandTotalLength += family.TotalLength;
                grandTotalVolume += family.TotalVolume;
                grandTotalInstanceCount += family.TotalInstanceCount;
                grandTotalSymbolCount += family.SymbolCount;
            }

            // 输出合计行
            report.AppendLine($"{"合计"}//{grandTotalSymbolCount}//{grandTotalInstanceCount}// {grandTotalLength:F2}//{grandTotalVolume:F3}");
            report.AppendLine();
            // ========== 第二部分：按类别统计 ==========
            report.AppendLine("【第二部分：按构件类别统计】");
            var categoryStats = selectedItems.GroupBy(e => e.StructuralCategory)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalLength = g.Sum(e => e.TotalLength),
                    TotalVolume = g.Sum(e => e.TotalVolume),
                    InstanceCount = g.Sum(e => e.InstanceCount),
                    FamilyCount = g.Select(e => e.FamilyName).Distinct().Count(),
                    SymbolCount = g.Count()
                }).OrderBy(g => g.Category).ToList();
            report.AppendLine("类别//族数量//类型数量// 总长度(m)//总体积(m³) ");
            foreach (var stat in categoryStats)
            {
                report.AppendLine($"│ {stat.Category}//{stat.FamilyCount}//{stat.SymbolCount}//{stat.TotalLength:F2}//{stat.TotalVolume:F3} │");
            }
            report.AppendLine($"{"合计"}//{categoryStats.Sum(s => s.FamilyCount)}//{categoryStats.Sum(s => s.SymbolCount)}//{grandTotalLength:F2}//{grandTotalVolume:F3} │");
            // 梁柱细分统计（如果存在）
            var beamColumnStats = selectedItems
                .Where(e => e.StructuralCategory == "结构梁" || e.StructuralCategory == "结构柱")
                .GroupBy(e => e.StructuralCategory)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalLength = g.Sum(e => e.TotalLength),
                    TotalVolume = g.Sum(e => e.TotalVolume)
                })
                .ToDictionary(g => g.Category, g => new { g.TotalLength, g.TotalVolume });
            if (beamColumnStats.Any())
            {
                report.AppendLine("【梁柱细分统计】");
                foreach (var stat in beamColumnStats)
                {
                    report.AppendLine($"•{stat.Key}: 总长度 = {stat.Value.TotalLength:F2}m, 总体积 = {stat.Value.TotalVolume:F3}m³");
                }
                report.AppendLine();
            }
            // ========== 第三部分：按材质统计 ==========
            report.AppendLine("【第三部分：按材质统计】");
            var materialStats = selectedItems
                .Where(e => !string.IsNullOrEmpty(e.StructuralMaterialName))
                .GroupBy(e => e.StructuralMaterialName)
                .Select(g => new
                {
                    Material = g.Key,
                    TotalVolume = g.Sum(e => e.TotalVolume),
                    TotalLength = g.Sum(e => e.TotalLength),
                    InstanceCount = g.Sum(e => e.InstanceCount),
                    // 统计来源（哪些族类型使用此材质）
                    Sources = g.Select(e => $"{e.FamilyName}/{e.SymbolName}").Distinct().ToList()
                }).OrderByDescending(g => g.TotalVolume).ToList();
            if (materialStats.Any())
            {
                report.AppendLine("材质名称//总体积(m³)//总长度(m)//来源（族/类型）│");
                foreach (var stat in materialStats)
                {
                    string sources = string.Join(", ", stat.Sources.Take(3));
                    if (stat.Sources.Count > 3) sources += $" 等{stat.Sources.Count}处";

                    report.AppendLine($"│ {stat.Material}//{stat.TotalVolume,12:F3}//{stat.TotalLength,12:F2}//{sources}");
                }
            }
            else
            {
                report.AppendLine("  未找到材质信息或所有构件均未定义材质");
            }
            report.AppendLine();
            // 添加材质体积占比分析
            if (materialStats.Any() && grandTotalVolume > 0)
            {
                report.AppendLine("【材质体积占比】");
                foreach (var stat in materialStats.Take(5))
                {
                    double percentage = (stat.TotalVolume / grandTotalVolume) * 100;
                    report.AppendLine($"•{stat.Material}: {percentage:F1}% ({stat.TotalVolume:F3}m³)");
                }
                report.AppendLine();
            }
            // 添加汇总信息
            report.AppendLine("========== 统计摘要 ==========");
            report.AppendLine($"✓ 统计族类型数量: {selectedItems.Count}");
            report.AppendLine($"✓ 涉及不同族数量: {selectedItems.Select(e => e.FamilyName).Distinct().Count()}");
            report.AppendLine($"✓ 构件实例总数: {grandTotalInstanceCount}");
            report.AppendLine($"✓ 所有构件总长度: {grandTotalLength:F2} 米");
            report.AppendLine($"✓ 所有构件总体积: {grandTotalVolume:F3} 立方米");
            report.AppendLine($"✓ 涉及材质种类: {materialStats.Count}");
            if (selectedItems.Any(e => string.IsNullOrEmpty(e.StructuralMaterialName)))
            {
                int noMaterialCount = selectedItems.Count(e => string.IsNullOrEmpty(e.StructuralMaterialName));
                report.AppendLine($"⚠ 未定义材质的类型数量: {noMaterialCount}");
            }
            // 显示报告
            TaskDialog.Show("结构构件统计报告", report.ToString());
        }
        public ICommand DeleteElementCommand => new RelayCommand<StructuralEntity>(DeleteElement);
        public void DeleteElement(StructuralEntity entity)
        {
            throw new NotImplementedException();
        }
    }
    public class StructuralEntity : ObserverableObject
    {
        Document Document;
        private readonly BaseExternalHandler _handler;
        public List<ElementId> instanceIds = new List<ElementId>();
        public StructuralEntity(FamilySymbol symbol, BaseExternalHandler handler, List<FamilyInstance> instances)
        {
            Document = symbol.Document;
            _handler = handler;
            FamilyId = symbol.Family.Id;
            SymbolId = symbol.Id;
            SymbolName = symbol.Name;
            FamilyName = symbol.FamilyName;
            InstanceCount = instances.Count;
            foreach (var item in instances)
            {
                instanceIds.Add(item.Id);
            }
            switch (symbol.Category.Id.IntegerValue)
            {
                case (int)BuiltInCategory.OST_StructuralColumns:
                    StructuralCategory = "结构柱";
                    break;
                case (int)BuiltInCategory.OST_StructuralFraming:
                    StructuralCategory = "结构梁";
                    break;
                case (int)BuiltInCategory.OST_StructuralFoundation:
                    StructuralCategory = "基础";
                    break;
                default:
                    StructuralCategory = "桁架";
                    break;
            }
            if (symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns ||
                symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
            {
                foreach (var item in instances)
                {
                    TotalLength += item.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble() * 304.8 / 1000;
                }
            }
            foreach (var item in instances)
            {
                TotalVolume += item.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble() * 304.8 * 304.8 * 304.8 / (1000 * 1000 * 1000);
            }
            // 2. 楼层统计
            StructuralInstanceCount = instances.GroupBy(fi => fi.Document.GetElement(fi.LevelId)?.Name ?? "未定义楼层")
                .ToDictionary(g => g.Key, g => g.Count().ToString());
            try
            {
                if (instances.Count != 0)
                {
                    // 1. 首先尝试从实例层级获取材质
                    var instanceMaterialParam = instances.FirstOrDefault().get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                    var materialId = instanceMaterialParam?.AsElementId();

                    // 2. 如果实例层级的材质为无效或 -1，则从类型层级获取
                    if (materialId == null || materialId == ElementId.InvalidElementId)
                    {
                        var symbol2 = instances.FirstOrDefault().Symbol;
                        if (symbol2 != null)
                        {
                            var symbolMaterialParam = symbol2.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                            materialId = symbolMaterialParam?.AsElementId();
                        }
                    }
                    // 3. 获取材质名称
                    if (materialId != null && materialId != ElementId.InvalidElementId)
                    {
                        var material = Document.GetElement(materialId) as Material;
                        StructuralMaterialName = material?.Name;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public Dictionary<string, string> StructuralInstanceCount { get; set; } = new Dictionary<string, string>();
        public string StructuralMaterialName { get; set; } = string.Empty;
        public double TotalLength { get; set; } = 0;
        public double TotalVolume { get; set; } = 0;
        public string StructuralCategory { get; set; }
        public string FamilyName { get; private set; }
        public string SymbolName { get; set; }
        public int InstanceCount { get; set; } = 0;
        public ElementId FamilyId { get; private set; }
        public ElementId SymbolId { get; private set; }
    }
}
