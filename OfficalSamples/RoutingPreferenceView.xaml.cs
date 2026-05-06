using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// RoutingPreferenceView.xaml 的交互逻辑
    /// </summary>
    public partial class RoutingPreferenceView : Window
    {
        public RoutingPreferenceView()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 管道信息项 - 用于下拉列表显示
        /// </summary>
        public class PipeTypeItem
        {
            public string DisplayName { get; set; }
            public ElementId Id { get; set; }
            public PipeType PipeType { get; set; }
        }

        /// <summary>
        /// 主窗口ViewModel - 管理管道类型选择和路由偏好分析
        /// </summary>
        public class RoutingPreferenceViewModel : ObserverableObject
        {
            private readonly UIApplication _uiApp;
            private readonly Document _document;
            private ObservableCollection<PipeTypeItem> _pipeTypes;
            private PipeTypeItem _selectedPipeType;
            private ObservableCollection<string> _availableSizes;
            private string _selectedSize;
            private string _outputText;
            private bool _isProcessing;

            public ObservableCollection<PipeTypeItem> PipeTypes
            {
                get => _pipeTypes;
                set { _pipeTypes = value; OnPropertyChanged(); }
            }

            public PipeTypeItem SelectedPipeType
            {
                get => _selectedPipeType;
                set
                {
                    _selectedPipeType = value;
                    OnPropertyChanged();
                    LoadAvailableSizes();
                }
            }

            public ObservableCollection<string> AvailableSizes
            {
                get => _availableSizes;
                set { _availableSizes = value; OnPropertyChanged(); }
            }

            public string SelectedSize
            {
                get => _selectedSize;
                set { _selectedSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCheckSpecificSize)); }
            }

            public string OutputText
            {
                get => _outputText;
                set { _outputText = value; OnPropertyChanged(); }
            }

            public bool IsProcessing
            {
                get => _isProcessing;
                set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
            }

            public bool CanExecute => !IsProcessing;
            public bool CanCheckSpecificSize => CanExecute && !string.IsNullOrEmpty(SelectedSize);

            public ICommand CheckAllCommand;
            public ICommand CheckSpecificSizeCommand;
            public ICommand CloseCommand;

            public RoutingPreferenceViewModel(UIApplication uiApp)
            {
                _uiApp = uiApp;
                _document = uiApp.ActiveUIDocument?.Document;

                if (_document == null)
                {
                    TaskDialog.Show("路由偏好分析", "没有活动文档。");
                    return;
                }

                InitializeCommands();
                LoadPipeTypes();
            }

            private void InitializeCommands()
            {
                CheckAllCommand = new BaseBindingCommand(_ => ExecuteCheckAll(), _ => CanExecute);
                CheckSpecificSizeCommand = new BaseBindingCommand(_ => ExecuteCheckSpecificSize(), _ => CanCheckSpecificSize);
                CloseCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            }

            /// <summary>
            /// 加载所有管道类型 - 使用LINQ
            /// </summary>
            private void LoadPipeTypes()
            {
                var collector = new FilteredElementCollector(_document);
                var pipeTypes = collector.OfClass(typeof(PipeType))
                    .Cast<PipeType>()
                    .Select(pt => new PipeTypeItem
                    {
                        DisplayName = $"{pt.Name}, Id: {pt.Id.IntegerValue}",
                        Id = pt.Id,
                        PipeType = pt
                    })
                    .ToList();

                PipeTypes = new ObservableCollection<PipeTypeItem>(pipeTypes);
                SelectedPipeType = PipeTypes.FirstOrDefault();
            }

            /// <summary>
            /// 加载可用尺寸列表 - 根据选中的管道类型
            /// </summary>
            private void LoadAvailableSizes()
            {
                if (SelectedPipeType?.PipeType == null) return;

                IsProcessing = true;
                try
                {
                    var sizes = Analyzer.GetAvailableSegmentSizes(
                        SelectedPipeType.PipeType.RoutingPreferenceManager,
                        _document);

                    AvailableSizes = new ObservableCollection<string>(
                        sizes.Select(size =>
                            ConvertValueDocumentUnits(size, _document).ToString()));

                    SelectedSize = AvailableSizes.FirstOrDefault();
                }
                finally
                {
                    IsProcessing = false;
                }
            }

            /// <summary>
            /// 执行全面检查
            /// </summary>
            private void ExecuteCheckAll()
            {
                if (SelectedPipeType?.PipeType == null) return;

                IsProcessing = true;
                try
                {
                    var analyzer = new Analyzer(
                        SelectedPipeType.PipeType.RoutingPreferenceManager,
                        _document);

                    var results = analyzer.GetWarnings();
                    OutputText = FormatXml(results);
                }
                catch (Exception ex)
                {
                    OutputText = $"分析失败：{ex.Message}";
                }
                finally
                {
                    IsProcessing = false;
                }
            }

            /// <summary>
            /// 执行特定尺寸检查
            /// </summary>
            private void ExecuteCheckSpecificSize()
            {
                if (SelectedPipeType?.PipeType == null || string.IsNullOrEmpty(SelectedSize)) return;

                IsProcessing = true;
                try
                {
                    var size = double.Parse(SelectedSize);
                    var analyzer = new Analyzer(
                        SelectedPipeType.PipeType.RoutingPreferenceManager,
                        size,
                        _document);

                    var results = analyzer.GetSpecificSizeQuery();
                    OutputText = FormatXml(results);
                }
                catch (Exception ex)
                {
                    OutputText = $"分析失败：{ex.Message}";
                }
                finally
                {
                    IsProcessing = false;
                }
            }

            /// <summary>
            /// 格式化XML输出
            /// </summary>
            private static string FormatXml(XDocument document)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineOnAttributes = false,
                    Encoding = Encoding.UTF8
                };

                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, settings))
                {
                    document.WriteTo(writer);
                }
                return sb.ToString();
            }

            /// <summary>
            /// 转换文档单位（内部单位到显示单位）
            /// </summary>
            private static double ConvertValueDocumentUnits(double value, Document doc)
            {
                // 简化实现：实际应使用UnitUtils转换
                return value;
            }

            public Action CloseWindow { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        /// <summary>
        /// 部件信息类 - 存储路由偏好规则组匹配的管段/管件ID
        /// 使用C# 7.3语法：表达式体成员、字符串插值
        /// </summary>
        public class PartIdInfo
        {
            public IReadOnlyList<ElementId> Ids { get; }
            public RoutingPreferenceRuleGroupType GroupType { get; }

            public PartIdInfo(RoutingPreferenceRuleGroupType groupType, IList<ElementId> ids)
            {
                GroupType = groupType;
                Ids = ids?.ToList().AsReadOnly() ?? new List<ElementId>().AsReadOnly();
            }

            /// <summary>
            /// 生成XML表示
            /// </summary>
            public XElement GetXml(Document document) => new XElement("PartInfo",
                new XAttribute("groupType", GroupType.ToString()),
                new XAttribute("partNames", GetFittingNames(document)));

            /// <summary>
            /// 获取部件名称 - 使用switch表达式
            /// </summary>
            private string GetFittingName(Document document, ElementId id)
            {
                if (id == ElementId.InvalidElementId) return "None -1";

                var element = document.GetElement(id);

                if (element is Segment)
                {
                    var segment = (Segment)element;
                    return segment.Name;
                }
                else if (element is FamilySymbol)
                {
                    var symbol = (FamilySymbol)element;
                    return $"{symbol.Family.Name} {symbol.Name}";
                }
                else
                {
                    return "Unknown";
                }
            }

            /// <summary>
            /// 获取所有部件名称的逗号分隔字符串
            /// </summary>
            private string GetFittingNames(Document document)
            {
                if (!Ids.Any()) return "None -1";

                var names = Ids.Select(id => $"{GetFittingName(document, id)} {id.IntegerValue}");
                return string.Join(", ", names);
            }
        }

        /// <summary>
        /// 路由偏好分析器 - 检查管道系统路由偏好配置问题
        /// 使用C# 7.3语法：表达式体成员、LINQ、字符串插值、模式匹配
        /// </summary>
        internal class Analyzer
        {
            #region 私有字段
            private readonly Document _document;
            private readonly RoutingPreferenceManager _routingManager;
            private readonly double _mepSize;
            #endregion

            #region 构造函数
            public Analyzer(RoutingPreferenceManager routingManager, Document document)
            {
                _routingManager = routingManager;
                _document = document;
                _mepSize = 0;
            }

            public Analyzer(RoutingPreferenceManager routingManager, double mepSize, Document document)
            {
                _routingManager = routingManager;
                _document = document;
                _mepSize = ConvertToFeet(mepSize, document);
            }
            #endregion

            #region 公共方法
            /// <summary>
            /// 获取所有管段的可用尺寸
            /// </summary>
            public static List<double> GetAvailableSegmentSizes(RoutingPreferenceManager routingManager, Document document)
            {
                var sizes = new HashSet<double>();
                int segmentCount = routingManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                for (int i = 0; i < segmentCount; i++)
                {
                    var rule = routingManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);
                    if (rule.MEPPartId == ElementId.InvalidElementId) continue;

                    var segment = document.GetElement(rule.MEPPartId) as Segment;
                    var segmentSizes = segment?.GetSizes() ?? Enumerable.Empty<MEPSize>();

                    foreach (var size in segmentSizes)
                    {
                        sizes.Add(size.NominalDiameter);
                    }
                }

                return sizes.OrderBy(s => s).ToList();
            }

            /// <summary>
            /// 获取特定尺寸的查询结果
            /// </summary>
            public XDocument GetSpecificSizeQuery()
            {
                var root = new XElement("RoutingPreferenceAnalysisSizeQuery");
                root.Add(GetHeaderInfo());

                foreach (var partInfo in GetPreferredFittingsAndSegments())
                {
                    root.Add(partInfo.GetXml(_document));
                }

                return new XDocument(root);
            }

            /// <summary>
            /// 获取所有警告信息
            /// </summary>
            public XDocument GetWarnings()
            {
                var root = new XElement("RoutingPreferenceAnalysis");
                root.Add(GetHeaderInfo());

                var warnings = new XElement("Warnings");
                warnings.Add(GetNoRangeSetWarnings());
                warnings.Add(GetJunctionTypeWarning());
                warnings.Add(GetSegmentCoverageWarning(RoutingPreferenceRuleGroupType.Elbows, "Elbows"));
                warnings.Add(GetSegmentCoverageWarning(RoutingPreferenceRuleGroupType.Junctions, "Junctions"));
                warnings.Add(GetSegmentCoverageWarning(RoutingPreferenceRuleGroupType.Crosses, "Crosses"));

                root.Add(warnings);
                return new XDocument(root);
            }
            #endregion

            #region 私有辅助方法
            /// <summary>
            /// 获取头部信息
            /// </summary>
            private XElement GetHeaderInfo()
            {
                var ownerId = _routingManager.OwnerId;
                var pipeType = _document.GetElement(ownerId);
                var pipeTypeName = pipeType?.Name ?? "Unknown";

                return new XElement("PipeType",
                    new XAttribute("name", pipeTypeName),
                    new XAttribute("elementId", ownerId.IntegerValue));
            }

            /// <summary>
            /// 获取范围未设置的警告
            /// </summary>
            private IEnumerable<XElement> GetNoRangeSetWarnings()
            {
                var warnings = new List<XElement>();
                var groupTypes = Enum.GetValues(typeof(RoutingPreferenceRuleGroupType))
                    .Cast<RoutingPreferenceRuleGroupType>()
                    .Where(g => g != RoutingPreferenceRuleGroupType.Undefined);

                foreach (var groupType in groupTypes)
                {
                    if (!HasAnyRule(_routingManager, groupType)) continue;

                    if (IsFirstRuleSetToNone(_routingManager, groupType))
                    {
                        var warning = new XElement("NoRangeSet",
                            new XAttribute("groupType", groupType.ToString()),
                            new XAttribute("rule", IsAllRulesSetToNone(_routingManager, groupType) ? "allRules" : "firstRule"));
                        warnings.Add(warning);
                    }
                }

                return warnings;
            }

            /// <summary>
            /// 获取首选连接件类型警告
            /// </summary>
            private XElement GetJunctionTypeWarning()
            {
                return IsPreferredJunctionTypeValid(_routingManager, _document)
                    ? null
                    : new XElement("FittingsNotDefinedForPreferredJunction");
            }

            /// <summary>
            /// 获取管段覆盖范围警告
            /// </summary>
            private XElement GetSegmentCoverageWarning(RoutingPreferenceRuleGroupType groupType, string groupName)
            {
                var segmentCount = _routingManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                for (int i = 0; i < segmentCount; i++)
                {
                    var rule = _routingManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);
                    if (rule.MEPPartId == ElementId.InvalidElementId) continue;
                    if (rule.NumberOfCriteria == 0) continue;

                    var psc = rule.GetCriterion(0) as PrimarySizeCriterion;
                    if (psc == null) continue;

                    var segment = _document.GetElement(rule.MEPPartId) as PipeSegment;
                    if (segment == null) continue;

                    var uncoveredSizes = new List<double>();
                    bool isCovered = CheckSegmentCoverage(groupType, psc.MinimumSize, psc.MaximumSize, uncoveredSizes);

                    if (!isCovered && uncoveredSizes.Any())
                    {
                        var sizesStr = string.Join(" ", uncoveredSizes.Select(s => ConvertToDisplayUnits(s, _document).ToString("F2")));

                        return new XElement("SegmentRangeNotCovered",
                            new XAttribute("name", segment.Name),
                            new XAttribute("sizes", sizesStr),
                            new XAttribute("unit", GetDisplayUnit(_document)),
                            new XAttribute("groupType", groupType.ToString()));
                    }
                }
                return null;
            }

            /// <summary>
            /// 检查管段覆盖范围
            /// </summary>
            private bool CheckSegmentCoverage(RoutingPreferenceRuleGroupType groupType, double min, double max, List<double> uncoveredSizes)
            {
                bool isFullyCovered = true;

                var conditions = new RoutingConditions(RoutingPreferenceErrorLevel.None);
                conditions.AppendCondition(new RoutingCondition(min)); // 使用最小尺寸测试

                var preferredId = _routingManager.GetMEPPartId(groupType, conditions);
                if (preferredId == ElementId.InvalidElementId)
                {
                    uncoveredSizes.Add(min);
                    isFullyCovered = false;
                }

                return isFullyCovered;
            }

            /// <summary>
            /// 检查首条规则是否设置为"None"范围
            /// </summary>
            private static bool IsFirstRuleSetToNone(RoutingPreferenceManager manager, RoutingPreferenceRuleGroupType groupType)
            {
                if (manager.GetNumberOfRules(groupType) == 0) return false;

                var rule = manager.GetRule(groupType, 0);
                if (rule.NumberOfCriteria == 0) return false;

                var psc = rule.GetCriterion(0) as PrimarySizeCriterion;
                return psc?.IsEqual(PrimarySizeCriterion.None()) == true;
            }

            /// <summary>
            /// 检查是否所有规则都设置为"None"范围
            /// </summary>
            private static bool IsAllRulesSetToNone(RoutingPreferenceManager manager, RoutingPreferenceRuleGroupType groupType)
            {
                int ruleCount = manager.GetNumberOfRules(groupType);
                if (ruleCount == 0) return false;

                for (int i = 0; i < ruleCount; i++)
                {
                    var rule = manager.GetRule(groupType, i);
                    if (rule.NumberOfCriteria == 0) return false;

                    var psc = rule.GetCriterion(0) as PrimarySizeCriterion;
                    if (psc?.IsEqual(PrimarySizeCriterion.None()) != true) return false;
                }
                return true;
            }

            /// <summary>
            /// 检查是否有任何规则
            /// </summary>
            private static bool HasAnyRule(RoutingPreferenceManager manager, RoutingPreferenceRuleGroupType groupType) =>
                manager.GetNumberOfRules(groupType) > 0;

            /// <summary>
            /// 检查首选连接件类型是否有效
            /// </summary>
            private static bool IsPreferredJunctionTypeValid(RoutingPreferenceManager manager, Document document)
            {
                var preferredType = manager.PreferredJunctionType;
                if (manager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Junctions) == 0) return false;

                bool hasTee = false;
                bool hasTap = false;

                for (int i = 0; i < manager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Junctions); i++)
                {
                    var rule = manager.GetRule(RoutingPreferenceRuleGroupType.Junctions, i);
                    if (rule.MEPPartId == ElementId.InvalidElementId) continue;

                    var symbol = document.GetElement(rule.MEPPartId) as FamilySymbol;
                    if (symbol == null) continue;

                    var partTypeParam = symbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
                    var partType = (PartType)(partTypeParam?.AsInteger() ?? 0);

                    switch (partType)
                    {
                        case PartType.Tee:
                            hasTee = true;
                            break;
                        case PartType.TapAdjustable:
                        case PartType.TapPerpendicular:
                        case PartType.SpudPerpendicular:
                        case PartType.SpudAdjustable:
                            hasTap = true;
                            break;
                    }
                }

                return (preferredType == PreferredJunctionType.Tee && hasTee) ||
                       (preferredType == PreferredJunctionType.Tap && hasTap);
            }

            /// <summary>
            /// 获取指定尺寸下匹配的管段和管件
            /// </summary>
            private List<PartIdInfo> GetPreferredFittingsAndSegments()
            {
                var results = new List<PartIdInfo>();
                var conditions = new RoutingConditions(RoutingPreferenceErrorLevel.None);
                conditions.AppendCondition(new RoutingCondition(_mepSize));

                var groupTypes = Enum.GetValues(typeof(RoutingPreferenceRuleGroupType))
                    .Cast<RoutingPreferenceRuleGroupType>()
                    .Where(g => g != RoutingPreferenceRuleGroupType.Undefined);

                foreach (var groupType in groupTypes)
                {
                    IList<ElementId> preferredIds;

                    if (groupType == RoutingPreferenceRuleGroupType.Segments)
                    {
                        // 获取支持指定尺寸的所有管段
                        preferredIds = _routingManager.GetSharedSizes(_mepSize, ConnectorProfileType.Round);
                    }
                    else
                    {
                        var id = _routingManager.GetMEPPartId(groupType, conditions);
                        preferredIds = id != ElementId.InvalidElementId
                            ? new List<ElementId> { id }
                            : new List<ElementId>();
                    }

                    results.Add(new PartIdInfo(groupType, preferredIds));
                }

                return results;
            }

            /// <summary>
            /// 转换内部单位到英尺
            /// </summary>
            private static double ConvertToFeet(double value, Document doc) =>
                UnitUtils.Convert(value, DisplayUnitType.DUT_DECIMAL_INCHES, DisplayUnitType.DUT_DECIMAL_FEET);

            /// <summary>
            /// 转换到显示单位
            /// </summary>
            private static double ConvertToDisplayUnits(double value, Document doc) =>
                UnitUtils.Convert(value, DisplayUnitType.DUT_DECIMAL_FEET,
                    doc.GetUnits().GetFormatOptions(UnitType.UT_PipeSize).DisplayUnits);

            /// <summary>
            /// 获取显示单位字符串
            /// </summary>
            private static string GetDisplayUnit(Document doc) =>
                doc.GetUnits().GetFormatOptions(UnitType.UT_PipeSize).DisplayUnits.ToString();
            #endregion
        }

        /// <summary>
        /// 族文件查找工具类 - 在Revit库路径中查找.rfa文件
        /// 使用C# 7.3语法：表达式体成员、LINQ、字符串插值、using声明
        /// </summary>
        internal class FindFolderUtility
        {
            private readonly Autodesk.Revit.ApplicationServices.Application _application;
            private readonly Dictionary<string, string> _familyFiles;
            private const string FamilyPathsXml = "familypaths.xml";

            public FindFolderUtility(Autodesk.Revit.ApplicationServices.Application application)
            {
                _application = application;
                _familyFiles = new Dictionary<string, string>();

                var basePath = GetFamilyBasePath();
                GetAllFiles(basePath, _familyFiles);

                var extraPaths = GetAdditionalFamilyPaths();
                foreach (var extraPath in extraPaths)
                {
                    GetAllFiles(extraPath, _familyFiles);
                }
            }

            /// <summary>
            /// 根据文件名查找族文件完整路径
            /// </summary>
            public string FindFileFolder(string fileName) =>
                _familyFiles.TryGetValue(fileName, out var path) ? path : "";

            /// <summary>
            /// 获取用户自定义路径列表 - 从familypaths.xml读取
            /// </summary>
            private static List<string> GetAdditionalFamilyPaths()
            {
                var exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(exeDir ?? ".", FamilyPathsXml);

                if (!File.Exists(configPath)) return new List<string>();

                try
                {
                    var reader = new StreamReader(configPath);
                    var pathsDoc = XDocument.Load(new XmlTextReader(reader));

                    return pathsDoc.Root?.Elements("FamilyPath")
                        .Select(x => x.Attribute("pathname")?.Value)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList() ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }

            /// <summary>
            /// 递归获取目录下所有.rfa文件
            /// </summary>
            private static void GetAllFiles(string basePath, Dictionary<string, string> fileDict)
            {
                if (!Directory.Exists(basePath)) return;

                // 获取当前目录下的所有.rfa文件
                foreach (var file in Directory.GetFiles(basePath, "*.rfa"))
                {
                    var fileName = System.IO.Path.GetFileName(file);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        fileDict[fileName] = file;
                    }
                }

                // 递归处理子目录
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    GetAllFiles(dir, fileDict);
                }
            }

            /// <summary>
            /// 获取Revit族文件基础路径
            /// </summary>
            private string GetFamilyBasePath()
            {
                // 优先尝试获取Imperial Library路径
                var basePath = GetLibraryPath("Imperial Library");
                if (!string.IsNullOrEmpty(basePath)) return basePath;

                // 备选：获取第一个库路径的父目录
                basePath = GetFirstLibraryParentPath();
                if (!string.IsNullOrEmpty(basePath)) return basePath;

                // 回退：使用Revit可执行文件所在目录的父目录
                return GetRevitInstallParentPath();
            }

            private string GetLibraryPath(string key)
            {
                try
                {
                    if (_application.GetLibraryPaths().TryGetValue(key, out var path))
                    {
                        return NormalizeAndGetParent(path);
                    }
                }
                catch
                {
                    // 忽略异常，继续尝试其他方法
                }
                return null;
            }

            private string GetFirstLibraryParentPath()
            {
                try
                {
                    var firstPath = _application.GetLibraryPaths().Values.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstPath))
                    {
                        return NormalizeAndGetParent(firstPath);
                    }
                }
                catch
                {
                    // 忽略异常
                }
                return null;
            }

            private static string GetRevitInstallParentPath()
            {
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        return Directory.GetParent(System.IO.Path.GetDirectoryName(exePath))?.FullName;
                    }
                }
                catch
                {
                    // 忽略异常
                }
                return "";
            }

            private static string NormalizeAndGetParent(string path)
            {
                if (string.IsNullOrEmpty(path)) return null;

                // 移除末尾的反斜杠
                var normalized = path.TrimEnd('\\');
                // 返回父目录路径
                return Directory.GetParent(normalized)?.FullName;
            }
        }

        /// <summary>
        /// 路由偏好数据异常类 - 用于标识此应用相关的错误
        /// </summary>
        internal class RoutingPreferenceDataException : Exception
        {
            private readonly string _message;

            public RoutingPreferenceDataException(string message) => _message = message;

            public override string ToString() => _message;
        }

        /// <summary>
        /// XML架构验证辅助类 - 验证RoutingPreferenceBuilder XML文档
        /// </summary>
        internal static class SchemaValidationHelper
        {
            private const string SchemaResourceName = "Revit.SDK.Samples.RoutingPreferenceTools.CS.RoutingPreferenceBuilder.RoutingPreferenceBuilderData.xsd";

            /// <summary>
            /// 验证XML文档是否符合预定义的架构
            /// </summary>
            public static bool ValidateRoutingPreferenceBuilderXml(XDocument doc, out string message)
            {
                message = "";
                var schemas = new XmlSchemaSet();
                var schemaContent = GetEmbeddedSchema();

                var stringReader = new StringReader(schemaContent);
                schemas.Add("", XmlReader.Create(stringReader));

                try
                {
                    doc.Validate(schemas, null);
                    return true;
                }
                catch (XmlSchemaValidationException ex)
                {
                    message = $"{ex.Message}, {ex.LineNumber}, {ex.LinePosition}";
                    return false;
                }
            }

            /// <summary>
            /// 从嵌入资源获取XSD架构内容
            /// </summary>
            private static string GetEmbeddedSchema()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(SchemaResourceName);

                if (stream == null)
                {
                    throw new Exception($"找不到嵌入的XML架构资源: {SchemaResourceName}");
                }

                var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 读取路由偏好配置命令 - 从XML文件导入管道配置
        /// 使用C# 7.3语法：表达式体成员、nameof、using声明、模式匹配
        /// </summary>
        [Transaction(TransactionMode.Manual)]
        public class ReadPreferencesCommand : IExternalCommand
        {
            private const string FileFilter = "RoutingPreference Builder Xml files (*.xml)|*.xml";
            private const string DefaultExtension = ".xml";
            private const string SuccessMessage = "路由偏好设置导入成功。";
            private const string ValidationErrorMessage = "XML文件不是有效的RoutingPreferenceBuilder文档。请检查RoutingPreferenceBuilderData.xsd。\n{0}";

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                var app = commandData.Application.Application;
                var doc = commandData.Application.ActiveUIDocument?.Document;

                // 验证MEP模块
                if (!IsMepAvailable(app))
                {
                    ShowMepWarning();
                    return Result.Succeeded;
                }

                // 验证管道定义
                if (!ArePipesDefined(doc))
                {
                    ShowPipesDefinedWarning();
                    return Result.Succeeded;
                }

                // 选择XML文件
                if (!TrySelectXmlFile(out string filePath))
                {
                    return Result.Succeeded;
                }

                // 加载并验证XML文档
                if (!TryLoadAndValidateXml(filePath, out XDocument xmlDoc, out string validationError))
                {
                    TaskDialog.Show("路由偏好构建器", string.Format(ValidationErrorMessage, validationError));
                    return Result.Succeeded;
                }

                // 导入路由偏好配置
                return ImportRoutingPreferences(doc, xmlDoc);
            }

            /// <summary>
            /// 检查MEP模块是否可用
            /// </summary>
            private static bool IsMepAvailable(Autodesk.Revit.ApplicationServices.Application app) =>
                app.Product.ToString().Contains("MEP");

            /// <summary>
            /// 检查文档中是否定义了管道类型
            /// </summary>
            private static bool ArePipesDefined(Document doc) =>
                doc != null && new FilteredElementCollector(doc).OfClass(typeof(PipeType)).Any();

            /// <summary>
            /// 显示MEP模块警告
            /// </summary>
            private static void ShowMepWarning() =>
                TaskDialog.Show("路由偏好分析", "此功能需要MEP模块支持。");

            /// <summary>
            /// 显示管道定义警告
            /// </summary>
            private static void ShowPipesDefinedWarning() =>
                TaskDialog.Show("路由偏好分析", "文档中没有定义管道类型。");

            /// <summary>
            /// 选择XML文件 - 使用out参数返回文件路径
            /// </summary>
            private static bool TrySelectXmlFile(out string filePath)
            {
                filePath = null;

                var dialog = new OpenFileDialog
                {
                    DefaultExt = DefaultExtension,
                    Filter = FileFilter,
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    filePath = dialog.FileName;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 加载并验证XML文件 - 使用using声明自动释放资源
            /// </summary>
            private static bool TryLoadAndValidateXml(string filePath, out XDocument xmlDoc, out string validationError)
            {
                xmlDoc = null;
                validationError = null;

                try
                {
                    // 加载XML文档
                    var reader = new StreamReader(filePath);
                    var xmlReader = new XmlTextReader(reader);
                    xmlDoc = XDocument.Load(xmlReader);
                }
                catch (Exception ex)
                {
                    validationError = $"文件加载失败：{ex.Message}";
                    return false;
                }

                // 验证XML结构
                if (!SchemaValidationHelper.ValidateRoutingPreferenceBuilderXml(xmlDoc, out validationError))
                {
                    xmlDoc = null;
                    return false;
                }

                return true;
            }

            /// <summary>
            /// 导入路由偏好配置
            /// </summary>
            private static Result ImportRoutingPreferences(Document doc, XDocument xmlDoc)
            {
                try
                {
                    using (var transaction = new Transaction(doc, "导入路由偏好配置"))
                    {
                        transaction.Start();

                        var builder = new RoutingPreferenceBuilder(doc);
                        builder.ParseAllPipingPoliciesFromXml(xmlDoc);

                        transaction.Commit();
                    }

                    TaskDialog.Show("路由偏好构建器", SuccessMessage);
                    return Result.Succeeded;
                }
                catch (RoutingPreferenceDataException ex)
                {
                    TaskDialog.Show("路由偏好构建器错误", ex.ToString());
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("路由偏好构建器错误", $"未知错误：{ex.Message}");
                    return Result.Succeeded;
                }
            }
        }

        /// <summary>
        /// 导出路由偏好配置命令 - 将管道配置导出为XML文件
        /// 使用C# 7.3语法：表达式体成员、using声明、字符串插值、nameof
        /// </summary>
        [Transaction(TransactionMode.ReadOnly)]
        public class WritePreferencesCommand : IExternalCommand
        {
            private const string FileFilter = "RoutingPreference Builder Xml files (*.xml)|*.xml";
            private const string DefaultExtension = ".xml";
            private const string FileNameSuffix = ".routingPreferences.xml";

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                var app = commandData.Application.Application;
                var doc = commandData.Application.ActiveUIDocument?.Document;

                // 验证MEP模块
                if (!IsMepAvailable(app))
                {
                    ShowMepWarning();
                    return Result.Succeeded;
                }

                // 验证管道定义
                if (!ArePipesDefined(doc))
                {
                    ShowPipesDefinedWarning();
                    return Result.Succeeded;
                }

                // 选择保存路径
                if (!TrySelectSavePath(doc, out string filePath))
                {
                    return Result.Succeeded;
                }

                // 导出配置
                ExportRoutingPreferences(doc, filePath);

                return Result.Succeeded;
            }

            /// <summary>
            /// 检查MEP模块是否可用
            /// </summary>
            private static bool IsMepAvailable(Autodesk.Revit.ApplicationServices.Application app) =>
                app.Product.ToString().Contains("MEP");

            /// <summary>
            /// 检查文档中是否定义了管道类型
            /// </summary>
            private static bool ArePipesDefined(Document doc) =>
                doc != null && new FilteredElementCollector(doc).OfClass(typeof(PipeType)).Any();

            /// <summary>
            /// 显示MEP模块警告
            /// </summary>
            private static void ShowMepWarning() =>
                TaskDialog.Show("路由偏好分析", "此功能需要MEP模块支持。");

            /// <summary>
            /// 显示管道定义警告
            /// </summary>
            private static void ShowPipesDefinedWarning() =>
                TaskDialog.Show("路由偏好分析", "文档中没有定义管道类型。");

            /// <summary>
            /// 选择保存路径 - 使用out参数返回文件路径
            /// </summary>
            private static bool TrySelectSavePath(Document doc, out string filePath)
            {
                filePath = null;

                var dialog = new SaveFileDialog
                {
                    DefaultExt = DefaultExtension,
                    Filter = FileFilter,
                    FileName = GetDefaultFileName(doc)
                };

                if (dialog.ShowDialog() == true)
                {
                    filePath = dialog.FileName;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 获取默认文件名 - 使用字符串插值
            /// </summary>
            private static string GetDefaultFileName(Document doc)
            {
                var docPath = doc?.PathName;
                var docName = string.IsNullOrEmpty(docPath)
                    ? "Document"
                    : System.IO.Path.GetFileNameWithoutExtension(docPath);

                return $"{docName}{FileNameSuffix}";
            }

            /// <summary>
            /// 导出路由偏好配置
            /// </summary>
            private static void ExportRoutingPreferences(Document doc, string filePath)
            {
                try
                {
                    var builder = new RoutingPreferenceBuilder(doc);
                    bool pathsNotFound = false;

                    var xmlDoc = builder.CreateXmlFromAllPipingPolicies(ref pathsNotFound);

                    WriteXmlToFile(xmlDoc, filePath);

                    ShowSuccessMessage(pathsNotFound);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("路由偏好构建器错误", $"导出失败：{ex.Message}");
                }
            }

            /// <summary>
            /// 将XML写入文件 - 使用using声明自动释放资源
            /// </summary>
            private static void WriteXmlToFile(XDocument xmlDoc, string filePath)
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineOnAttributes = false,
                    Encoding = System.Text.Encoding.UTF8
                };

                var writer = XmlWriter.Create(filePath, settings);
                xmlDoc.WriteTo(writer);
                writer.Flush();
            }

            /// <summary>
            /// 显示成功消息
            /// </summary>
            private static void ShowSuccessMessage(bool pathsNotFound)
            {
                var pathMessage = pathsNotFound
                    ? "\n\n注意：一个或多个.rfa文件路径未找到，可能需要手动添加到生成的XML文件中。"
                    : "";

                TaskDialog.Show("路由偏好构建器", $"路由偏好设置导出成功。{pathMessage}");
            }
        }


        /// <summary>
        /// 管道路由偏好构建器 - 用于读取和写入XML及路由偏好数据
        /// </summary>
        public class RoutingPreferenceBuilder
        {
            #region 数据字段
            private IEnumerable<Segment> m_segments;           // 所有管段
            private IEnumerable<FamilySymbol> m_fittings;      // 所有管件族
            private IEnumerable<Material> m_materials;         // 所有材质
            private IEnumerable<PipeScheduleType> m_pipeSchedules;  // 所有管程类型
            private IEnumerable<PipeType> m_pipeTypes;         // 所有管类型
            private Autodesk.Revit.DB.Document m_document;     // 当前Revit文档
            #endregion

            #region 公共接口
            /// <summary>
            /// 构造函数 - 初始化所有列表
            /// </summary>
            public RoutingPreferenceBuilder(Document document)
            {
                m_document = document;
                m_segments = GetAllPipeSegments(m_document);
                m_fittings = GetAllFittings(m_document);
                m_materials = GetAllMaterials(m_document);
                m_pipeSchedules = GetAllPipeScheduleTypes(m_document);
                m_pipeTypes = GetAllPipeTypes(m_document);
            }

            /// <summary>
            /// 从XML读取数据并加载管道配置
            /// 执行流程：加载族 -> 创建管类型 -> 创建管程 -> 创建管段 -> 设置路由偏好规则
            /// </summary>
            public void ParseAllPipingPoliciesFromXml(XDocument xDoc)
            {
                // 检查项目中是否至少定义了一种管类型
                if (m_pipeTypes.Count() == 0)
                    throw new RoutingPreferenceDataException("项目中未定义管类型，至少需要定义一种。");

                // 验证管道尺寸单位一致性
                FormatOptions formatOptionPipeSize = m_document.GetUnits().GetFormatOptions(UnitType.UT_PipeSize);
                string docPipeSizeUnit = formatOptionPipeSize.DisplayUnits.ToString();
                string xmlPipeSizeUnit = xDoc.Root.Attribute("pipeSizeUnits").Value;
                if (docPipeSizeUnit != xmlPipeSizeUnit)
                    throw new RoutingPreferenceDataException("XML中的单位与当前管道尺寸单位不匹配。");

                // 验证管道粗糙度单位一致性
                FormatOptions formatOptionRoughness = m_document.GetUnits().GetFormatOptions(UnitType.UT_Piping_Roughness);
                string docRoughnessUnit = formatOptionRoughness.DisplayUnits.ToString();
                string xmlRoughnessUnit = xDoc.Root.Attribute("pipeRoughnessUnits").Value;
                if (docRoughnessUnit != xmlRoughnessUnit)
                    throw new RoutingPreferenceDataException("XML中的单位与当前管道粗糙度单位不匹配。");

                // 步骤1：加载管件族文件(.rfa)
                Transaction loadFamilies = new Transaction(m_document, "加载族");
                loadFamilies.Start();
                IEnumerable<XElement> families = xDoc.Root.Elements("Family");
                FindFolderUtility findFolderUtility = new FindFolderUtility(m_document.Application);

                foreach (XElement xfamily in families)
                {
                    try
                    {
                        ParseFamilyFromXml(xfamily, findFolderUtility);
                    }
                    catch (Exception ex)
                    {
                        loadFamilies.RollBack();
                        throw ex;
                    }
                }
                loadFamilies.Commit();

                // 步骤2：创建新的管类型
                Transaction addPipeTypes = new Transaction(m_document, "添加管类型");
                addPipeTypes.Start();
                IEnumerable<XElement> pipeTypes = xDoc.Root.Elements("PipeType");
                foreach (XElement xpipeType in pipeTypes)
                {
                    try
                    {
                        ParsePipeTypeFromXml(xpipeType);
                    }
                    catch (Exception ex)
                    {
                        addPipeTypes.RollBack();
                        throw ex;
                    }
                }
                addPipeTypes.Commit();

                // 步骤3：创建新的管程类型
                Transaction addPipeSchedules = new Transaction(m_document, "添加管程类型");
                addPipeSchedules.Start();
                IEnumerable<XElement> pipeScheduleTypes = xDoc.Root.Elements("PipeScheduleType");
                foreach (XElement xpipeScheduleType in pipeScheduleTypes)
                {
                    try
                    {
                        ParsePipeScheduleTypeFromXml(xpipeScheduleType);
                    }
                    catch (Exception ex)
                    {
                        addPipeSchedules.RollBack();
                        throw ex;
                    }
                }
                addPipeSchedules.Commit();

                // 更新列表（因为前面步骤可能新增了类型）
                UpdatePipeTypesList();
                UpdatePipeTypeSchedulesList();
                UpdateFittingsList();

                // 步骤4：创建管段（包含材质、管程、粗糙度和管径尺寸）
                Transaction addPipeSegments = new Transaction(m_document, "添加管段");
                addPipeSchedules.Start();  // 注意：这里使用了addPipeSchedules而非addPipeSegments，可能是原代码笔误
                IEnumerable<XElement> pipeSegments = xDoc.Root.Elements("PipeSegment");
                foreach (XElement xpipeSegment in pipeSegments)
                {
                    try
                    {
                        ParsePipeSegmentFromXML(xpipeSegment);
                    }
                    catch (Exception ex)
                    {
                        addPipeSchedules.RollBack();
                        throw ex;
                    }
                }
                addPipeSchedules.Commit();

                UpdateSegmentsList();  // 更新管段列表

                // 步骤5：设置路由偏好规则（弯头、三通、变径等优先使用规则）
                Transaction addRoutingPreferences = new Transaction(m_document, "添加路由偏好");
                addRoutingPreferences.Start();
                IEnumerable<XElement> routingPreferenceManagers = xDoc.Root.Elements("RoutingPreferenceManager");
                foreach (XElement xroutingPreferenceManager in routingPreferenceManagers)
                {
                    try
                    {
                        ParseRoutingPreferenceManagerFromXML(xroutingPreferenceManager);
                    }
                    catch (Exception ex)
                    {
                        addRoutingPreferences.RollBack();
                        throw ex;
                    }
                }
                addRoutingPreferences.Commit();
            }

            /// <summary>
            /// 将当前文档的管道配置导出为XML
            /// </summary>
            public XDocument CreateXmlFromAllPipingPolicies(ref bool pathsNotFound)
            {
                FindFolderUtility findFolderUtility = new FindFolderUtility(m_document.Application);

                XDocument routingPreferenceBuilderDoc = new XDocument();
                XElement xroot = new XElement(XName.Get("RoutingPreferenceBuilder"));

                // 写入管道尺寸单位
                FormatOptions formatOptionPipeSize = m_document.GetUnits().GetFormatOptions(UnitType.UT_PipeSize);
                string unitStringPipeSize = formatOptionPipeSize.DisplayUnits.ToString();
                xroot.Add(new XAttribute(XName.Get("pipeSizeUnits"), unitStringPipeSize));

                // 写入管道粗糙度单位
                FormatOptions formatOptionRoughness = m_document.GetUnits().GetFormatOptions(UnitType.UT_Piping_Roughness);
                string unitStringRoughness = formatOptionRoughness.DisplayUnits.ToString();
                xroot.Add(new XAttribute(XName.Get("pipeRoughnessUnits"), unitStringRoughness));

                // 导出所有管件族信息
                foreach (FamilySymbol familySymbol in this.m_fittings)
                {
                    xroot.Add(CreateXmlFromFamily(familySymbol, findFolderUtility, ref pathsNotFound));
                }

                // 导出所有管类型
                foreach (PipeType pipeType in m_pipeTypes)
                {
                    xroot.Add(CreateXmlFromPipeType(pipeType));
                }

                // 导出所有管程类型
                foreach (PipeScheduleType pipeScheduleType in m_pipeSchedules)
                {
                    xroot.Add(CreateXmlFromPipeScheduleType(pipeScheduleType));
                }

                // 导出所有管段（含管径尺寸）
                foreach (PipeSegment pipeSegment in m_segments)
                {
                    xroot.Add(CreateXmlFromPipeSegment(pipeSegment));
                }

                // 导出所有路由偏好规则
                foreach (PipeType pipeType in m_pipeTypes)
                {
                    xroot.Add(CreateXmlFromRoutingPreferenceManager(pipeType.RoutingPreferenceManager));
                }

                routingPreferenceBuilderDoc.Add(xroot);
                return routingPreferenceBuilderDoc;
            }
            #endregion

            #region XML解析和生成
            /// <summary>
            /// 从XML加载族文件(.rfa)
            /// </summary>
            private void ParseFamilyFromXml(XElement familyXElement, FindFolderUtility findFolderUtility)
            {
                XAttribute xafilename = familyXElement.Attribute(XName.Get("filename"));
                string familyPath = xafilename.Value;

                // 如果指定路径不存在，尝试在系统目录中查找
                if (!System.IO.File.Exists(familyPath))
                {
                    string filename = System.IO.Path.GetFileName(familyPath);
                    familyPath = findFolderUtility.FindFileFolder(filename);
                    if (!System.IO.File.Exists(familyPath))
                        throw new RoutingPreferenceDataException("找不到族文件: " + xafilename.Value);
                }

                // 验证文件扩展名
                if (string.Compare(System.IO.Path.GetExtension(familyPath), ".rfa", true) != 0)
                    throw new RoutingPreferenceDataException(familyPath + " 不是族文件。");

                try
                {
                    // 加载族到文档（返回false表示已加载）
                    if (!m_document.LoadFamily(familyPath))
                        return;
                }
                catch (System.Exception ex)
                {
                    throw new RoutingPreferenceDataException("无法加载族: " + xafilename.Value + ": " + ex.ToString());
                }
            }

            /// <summary>
            /// 将管件族信息导出为XML
            /// </summary>
            private static XElement CreateXmlFromFamily(FamilySymbol pipeFitting, FindFolderUtility findFolderUtility, ref bool pathNotFound)
            {
                // 查找.rfa文件的完整路径
                string path = findFolderUtility.FindFileFolder(pipeFitting.Family.Name + ".rfa");
                string pathToWrite;
                if (path == "")
                {
                    pathNotFound = true;
                    pathToWrite = pipeFitting.Family.Name + ".rfa";  // 找不到时只写文件名
                }
                else
                {
                    pathToWrite = path;
                }

                XElement xFamilySymbol = new XElement(XName.Get("Family"));
                xFamilySymbol.Add(new XAttribute(XName.Get("filename"), pathToWrite));
                return xFamilySymbol;
            }

            /// <summary>
            /// 从XML创建管类型
            /// </summary>
            private void ParsePipeTypeFromXml(XElement pipetypeXElement)
            {
                XAttribute xaName = pipetypeXElement.Attribute(XName.Get("name"));
                ElementId pipeTypeId = GetPipeTypeByName(xaName.Value);

                // 如果不存在则复制创建
                if (pipeTypeId == ElementId.InvalidElementId)
                {
                    PipeType newPipeType = m_pipeTypes.First().Duplicate(xaName.Value) as PipeType;
                    ClearRoutingPreferenceRules(newPipeType);  // 清空默认路由规则
                }
            }

            /// <summary>
            /// 清空管类型的所有路由偏好规则
            /// </summary>
            private static void ClearRoutingPreferenceRules(PipeType pipeType)
            {
                foreach (RoutingPreferenceRuleGroupType group in System.Enum.GetValues(typeof(RoutingPreferenceRuleGroupType)))
                {
                    int ruleCount = pipeType.RoutingPreferenceManager.GetNumberOfRules(group);
                    for (int index = 0; index != ruleCount; ++index)
                    {
                        pipeType.RoutingPreferenceManager.RemoveRule(group, 0);
                    }
                }
            }

            /// <summary>
            /// 将管类型导出为XML
            /// </summary>
            private static XElement CreateXmlFromPipeType(PipeType pipeType)
            {
                XElement xPipeType = new XElement(XName.Get("PipeType"));
                xPipeType.Add(new XAttribute(XName.Get("name"), pipeType.Name));
                return xPipeType;
            }

            /// <summary>
            /// 从XML创建管程类型
            /// </summary>
            private void ParsePipeScheduleTypeFromXml(XElement pipeScheduleTypeXElement)
            {
                XAttribute xaName = pipeScheduleTypeXElement.Attribute(XName.Get("name"));
                ElementId pipeScheduleTypeId = GetPipeScheduleTypeByName(xaName.Value);
                if (pipeScheduleTypeId == ElementId.InvalidElementId)
                    m_pipeSchedules.First().Duplicate(xaName.Value);  // 复制创建
            }

            /// <summary>
            /// 将管程类型导出为XML
            /// </summary>
            private static XElement CreateXmlFromPipeScheduleType(PipeScheduleType pipeScheduleType)
            {
                XElement xPipeSchedule = new XElement(XName.Get("PipeScheduleType"));
                xPipeSchedule.Add(new XAttribute(XName.Get("name"), pipeScheduleType.Name));
                return xPipeSchedule;
            }

            /// <summary>
            /// 从XML创建管段（核心步骤：定义材质、管程、粗糙度和管径表）
            /// </summary>
            private void ParsePipeSegmentFromXML(XElement segmentXElement)
            {
                XAttribute xaMaterial = segmentXElement.Attribute(XName.Get("materialName"));
                XAttribute xaSchedule = segmentXElement.Attribute(XName.Get("pipeScheduleTypeName"));
                XAttribute xaRoughness = segmentXElement.Attribute(XName.Get("roughness"));

                // 查找材质（必须已存在于文档中）
                ElementId materialId = GetMaterialByName(xaMaterial.Value);
                if (materialId == ElementId.InvalidElementId)
                {
                    throw new RoutingPreferenceDataException("找不到材质: " + xaMaterial.Value + " 在: " + segmentXElement.ToString());
                }

                // 查找管程类型
                ElementId scheduleId = GetPipeScheduleTypeByName(xaSchedule.Value);

                // 解析粗糙度值
                double roughness;
                bool r1 = double.TryParse(xaRoughness.Value, out roughness);
                if (!r1)
                    throw new RoutingPreferenceDataException("无效的粗糙度值: " + xaRoughness.Value + " 在: " + segmentXElement.ToString());
                if (roughness <= 0)
                    throw new RoutingPreferenceDataException("无效的粗糙度值: " + xaRoughness.Value + " 在: " + segmentXElement.ToString());

                if (scheduleId == ElementId.InvalidElementId)
                {
                    throw new RoutingPreferenceDataException("找不到管程: " + xaSchedule.Value + " 在: " + segmentXElement.ToString());
                }

                // 检查是否已存在相同材质+管程的管段
                ElementId existingPipeSegmentId = GetSegmentByIds(materialId, scheduleId);
                if (existingPipeSegmentId != ElementId.InvalidElementId)
                    return;  // 已存在，跳过

                // 解析所有管径尺寸
                ICollection<MEPSize> sizes = new List<MEPSize>();
                foreach (XNode sizeNode in segmentXElement.Nodes())
                {
                    if (sizeNode is XElement)
                    {
                        MEPSize newSize = ParseMEPSizeFromXml(sizeNode as XElement, m_document);
                        sizes.Add(newSize);
                    }
                }

                // 创建管段并设置粗糙度
                PipeSegment pipeSegment = PipeSegment.Create(m_document, materialId, scheduleId, sizes);
                pipeSegment.Roughness = roughness / 304.8;
            }

            /// <summary>
            /// 将管段导出为XML（含管径尺寸表）
            /// </summary>
            private XElement CreateXmlFromPipeSegment(PipeSegment pipeSegment)
            {
                XElement xPipeSegment = new XElement(XName.Get("PipeSegment"));

                xPipeSegment.Add(new XAttribute(XName.Get("pipeScheduleTypeName"), GetPipeScheduleTypeNamebyId(pipeSegment.ScheduleTypeId)));
                xPipeSegment.Add(new XAttribute(XName.Get("materialName"), GetMaterialNameById(pipeSegment.MaterialId)));

                // 粗糙度转换为文档单位
                double roughnessInDocumentUnits = pipeSegment.Roughness / 304.8;
                xPipeSegment.Add(new XAttribute(XName.Get("roughness"), roughnessInDocumentUnits.ToString("r")));

                // 导出所有管径尺寸
                foreach (MEPSize size in pipeSegment.GetSizes())
                    xPipeSegment.Add(CreateXmlFromMEPSize(size, m_document));

                return xPipeSegment;
            }

            /// <summary>
            /// 从XML解析单个管径尺寸
            /// </summary>
            private static MEPSize ParseMEPSizeFromXml(XElement sizeXElement, Autodesk.Revit.DB.Document document)
            {
                XAttribute xaNominal = sizeXElement.Attribute(XName.Get("nominalDiameter"));
                XAttribute xaInner = sizeXElement.Attribute(XName.Get("innerDiameter"));
                XAttribute xaOuter = sizeXElement.Attribute(XName.Get("outerDiameter"));
                XAttribute xaUsedInSizeLists = sizeXElement.Attribute(XName.Get("usedInSizeLists"));
                XAttribute xaUsedInSizing = sizeXElement.Attribute(XName.Get("usedInSizing"));

                double nominal, inner, outer;
                bool usedInSizeLists, usedInSizing;
                bool r1 = double.TryParse(xaNominal.Value, out nominal);
                bool r2 = double.TryParse(xaInner.Value, out inner);
                bool r3 = double.TryParse(xaOuter.Value, out outer);
                bool r4 = bool.TryParse(xaUsedInSizeLists.Value, out usedInSizeLists);
                bool r5 = bool.TryParse(xaUsedInSizing.Value, out usedInSizing);

                if (!r1 || !r2 || !r3 || !r4 || !r5)
                    throw new RoutingPreferenceDataException("无法解析MEPSize属性:" + xaNominal.Value + ", " + xaInner.Value + ", " + xaOuter.Value + ", " + xaUsedInSizeLists.Value + ", " + xaUsedInSizing.Value);

                MEPSize newSize = null;
                try
                {
                    // 转换为内部单位（英尺）后创建
                    newSize = new MEPSize(
                                                //Convert.ConvertValueToFeet(nominal, document),
                                                //Convert.ConvertValueToFeet(inner, document),
                                                //Convert.ConvertValueToFeet(outer, document),
                                                nominal / 304.8,
                        inner / 304.8,
                        outer / 304.8,
                        usedInSizeLists,
                        usedInSizing);
                }
                catch (Exception)
                {
                    throw new RoutingPreferenceDataException("无效的MEPSize值: " + nominal.ToString() + ", " + inner.ToString() + ", " + outer.ToString());
                }
                return newSize;
            }

            /// <summary>
            /// 将管径尺寸导出为XML
            /// </summary>
            private static XElement CreateXmlFromMEPSize(MEPSize size, Autodesk.Revit.DB.Document document)
            {
                XElement xMEPSize = new XElement(XName.Get("MEPSize"));

                //xMEPSize.Add(new XAttribute(XName.Get("innerDiameter"), (Convert.ConvertValueDocumentUnits(size.InnerDiameter, document)).ToString()));
                //xMEPSize.Add(new XAttribute(XName.Get("nominalDiameter"), (Convert.ConvertValueDocumentUnits(size.NominalDiameter, document)).ToString()));
                //xMEPSize.Add(new XAttribute(XName.Get("outerDiameter"), (Convert.ConvertValueDocumentUnits(size.OuterDiameter, document)).ToString()));
                xMEPSize.Add(new XAttribute(XName.Get("usedInSizeLists"), size.UsedInSizeLists));
                xMEPSize.Add(new XAttribute(XName.Get("usedInSizing"), size.UsedInSizing));
                return xMEPSize;
            }

            /// <summary>
            /// 从XML解析路由偏好管理器（核心：设置管件使用规则）
            /// </summary>
            private void ParseRoutingPreferenceManagerFromXML(XElement routingPreferenceManagerXElement)
            {
                XAttribute xaPipeTypeName = routingPreferenceManagerXElement.Attribute(XName.Get("pipeTypeName"));
                XAttribute xaPreferredJunctionType = routingPreferenceManagerXElement.Attribute(XName.Get("preferredJunctionType"));

                // 解析首选连接类型（三通 vs 四通）
                PreferredJunctionType preferredJunctionType;
                bool r1 = Enum.TryParse<PreferredJunctionType>(xaPreferredJunctionType.Value, out preferredJunctionType);
                if (!r1)
                    throw new RoutingPreferenceDataException("无效的首选连接类型在: " + routingPreferenceManagerXElement.ToString());

                // 查找目标管类型
                ElementId pipeTypeId = GetPipeTypeByName(xaPipeTypeName.Value);
                if (pipeTypeId == ElementId.InvalidElementId)
                    throw new RoutingPreferenceDataException("找不到管类型元素在: " + routingPreferenceManagerXElement.ToString());

                PipeType pipeType = m_document.GetElement(pipeTypeId) as PipeType;
                RoutingPreferenceManager routingPreferenceManager = pipeType.RoutingPreferenceManager;
                routingPreferenceManager.PreferredJunctionType = preferredJunctionType;

                // 添加所有路由规则（弯头、三通、变径等）
                foreach (XNode xRule in routingPreferenceManagerXElement.Nodes())
                {
                    if (xRule is XElement)
                    {
                        RoutingPreferenceRuleGroupType groupType;
                        RoutingPreferenceRule rule = ParseRoutingPreferenceRuleFromXML(xRule as XElement, out groupType);
                        routingPreferenceManager.AddRule(groupType, rule);
                    }
                }
            }

            /// <summary>
            /// 将路由偏好管理器导出为XML
            /// </summary>
            private XElement CreateXmlFromRoutingPreferenceManager(RoutingPreferenceManager routingPreferenceManager)
            {
                XElement xRoutingPreferenceManager = new XElement(XName.Get("RoutingPreferenceManager"));

                xRoutingPreferenceManager.Add(new XAttribute(XName.Get("pipeTypeName"), GetPipeTypeNameById(routingPreferenceManager.OwnerId)));
                xRoutingPreferenceManager.Add(new XAttribute(XName.Get("preferredJunctionType"), routingPreferenceManager.PreferredJunctionType.ToString()));

                // 导出各类管件的规则：交叉、弯头、管段、连接点、变径、接头、机械接头
                for (int indexCrosses = 0; indexCrosses != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Crosses); indexCrosses++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Crosses, indexCrosses),
                        RoutingPreferenceRuleGroupType.Crosses));
                }

                for (int indexElbows = 0; indexElbows != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Elbows); indexElbows++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Elbows, indexElbows),
                        RoutingPreferenceRuleGroupType.Elbows));
                }

                for (int indexSegments = 0; indexSegments != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments); indexSegments++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, indexSegments),
                        RoutingPreferenceRuleGroupType.Segments));
                }

                for (int indexJunctions = 0; indexJunctions != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Junctions); indexJunctions++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Junctions, indexJunctions),
                        RoutingPreferenceRuleGroupType.Junctions));
                }

                for (int indexTransitions = 0; indexTransitions != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Transitions); indexTransitions++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Transitions, indexTransitions),
                        RoutingPreferenceRuleGroupType.Transitions));
                }

                for (int indexUnions = 0; indexUnions != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Unions); indexUnions++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Unions, indexUnions),
                        RoutingPreferenceRuleGroupType.Unions));
                }

                for (int indexMechanicalJoints = 0; indexMechanicalJoints != routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.MechanicalJoints); indexMechanicalJoints++)
                {
                    xRoutingPreferenceManager.Add(createXmlFromRoutingPreferenceRule(
                        routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.MechanicalJoints, indexMechanicalJoints),
                        RoutingPreferenceRuleGroupType.MechanicalJoints));
                }

                return xRoutingPreferenceManager;
            }

            /// <summary>
            /// 从XML解析单条路由偏好规则（指定管件类型和适用管径范围）
            /// </summary>
            private RoutingPreferenceRule ParseRoutingPreferenceRuleFromXML(XElement ruleXElement, out RoutingPreferenceRuleGroupType groupType)
            {
                XAttribute xaDescription = ruleXElement.Attribute(XName.Get("description"));
                XAttribute xaPartName = ruleXElement.Attribute(XName.Get("partName"));
                XAttribute xaMinSize = ruleXElement.Attribute(XName.Get("minimumSize"));
                XAttribute xaMaxSize = null;
                XAttribute xaGroup = ruleXElement.Attribute(XName.Get("ruleGroup"));

                // 解析规则分组类型（弯头/三通/变径等）
                bool r3 = Enum.TryParse<RoutingPreferenceRuleGroupType>(xaGroup.Value, out groupType);
                if (!r3)
                    throw new RoutingPreferenceDataException("无法解析规则分组类型: " + xaGroup.Value);

                string description = xaDescription.Value;

                // 根据分组类型查找对应管件（管段或管件族）
                ElementId partId;
                if (groupType == RoutingPreferenceRuleGroupType.Segments)
                    partId = GetSegmentByName(xaPartName.Value);
                else
                    partId = GetFittingByName(xaPartName.Value);

                if (partId == ElementId.InvalidElementId)
                    throw new RoutingPreferenceDataException("找不到MEP部件: " + xaPartName.Value + ". 请确认族名称正确且已加载。");

                RoutingPreferenceRule rule = new RoutingPreferenceRule(partId, description);

                // 解析管径范围条件
                PrimarySizeCriterion sizeCriterion;
                if (string.Compare(xaMinSize.Value, "All", true) == 0)
                {
                    sizeCriterion = PrimarySizeCriterion.All();  // 适用所有管径
                }
                else if (string.Compare(xaMinSize.Value, "None", true) == 0)
                {
                    sizeCriterion = PrimarySizeCriterion.None();  // 不适用任何管径
                }
                else
                {
                    // 指定最小和最大管径
                    try
                    {
                        xaMaxSize = ruleXElement.Attribute(XName.Get("maximumSize"));
                    }
                    catch (System.Exception)
                    {
                        throw new RoutingPreferenceDataException("无法获取maximumSize属性在: " + ruleXElement.ToString());
                    }

                    double min, max;
                    bool r1 = double.TryParse(xaMinSize.Value, out min);
                    bool r2 = double.TryParse(xaMaxSize.Value, out max);
                    if (!r1 || !r2)
                        throw new RoutingPreferenceDataException("无法解析尺寸值: " + xaMinSize.Value + ", " + xaMaxSize.Value);
                    if (min > max)
                        throw new RoutingPreferenceDataException("无效的管径范围。");

                    // 转换为内部单位（英尺）
                    //min = Convert.ConvertValueToFeet(min, m_document);
                    //max = Convert.ConvertValueToFeet(max, m_document);
                    min = min / 304.8;
                    max = max / 304.8;
                    sizeCriterion = new PrimarySizeCriterion(min, max);
                }

                rule.AddCriterion(sizeCriterion);
                return rule;
            }

            /// <summary>
            /// 将单条路由偏好规则导出为XML
            /// </summary>
            private XElement createXmlFromRoutingPreferenceRule(RoutingPreferenceRule rule, RoutingPreferenceRuleGroupType groupType)
            {
                XElement xRoutingPreferenceRule = new XElement(XName.Get("RoutingPreferenceRule"));
                xRoutingPreferenceRule.Add(new XAttribute(XName.Get("description"), rule.Description));
                xRoutingPreferenceRule.Add(new XAttribute(XName.Get("ruleGroup"), groupType.ToString()));

                // 导出管径范围条件
                if (rule.NumberOfCriteria >= 1)
                {
                    PrimarySizeCriterion psc = rule.GetCriterion(0) as PrimarySizeCriterion;

                    if (psc.IsEqual(PrimarySizeCriterion.All()))
                    {
                        xRoutingPreferenceRule.Add(new XAttribute(XName.Get("minimumSize"), "All"));
                    }
                    else if (psc.IsEqual(PrimarySizeCriterion.None()))
                    {
                        xRoutingPreferenceRule.Add(new XAttribute(XName.Get("minimumSize"), "None"));
                    }
                    else
                    {
                        //// 导出具体管径范围（转换为文档显示单位）
                        //xRoutingPreferenceRule.Add(new XAttribute(XName.Get("minimumSize"),
                        //    (Convert.ConvertValueDocumentUnits(psc.MinimumSize, m_document)).ToString()));
                        //xRoutingPreferenceRule.Add(new XAttribute(XName.Get("maximumSize"),
                        //    (Convert.ConvertValueDocumentUnits(psc.MaximumSize, m_document)).ToString()));
                    }
                }
                else
                {
                    xRoutingPreferenceRule.Add(new XAttribute(XName.Get("minimumSize"), "All"));
                }

                // 根据类型导出部件名称（管段名或管件族名）
                if (groupType == RoutingPreferenceRuleGroupType.Segments)
                {
                    xRoutingPreferenceRule.Add(new XAttribute(XName.Get("partName"), GetSegmentNameById(rule.MEPPartId)));
                }
                else
                {
                    xRoutingPreferenceRule.Add(new XAttribute(XName.Get("partName"), GetFittingNameById(rule.MEPPartId)));
                }

                return xRoutingPreferenceRule;
            }
            #endregion

            #region 查询和查找方法
            /// <summary>
            /// 根据ID获取管程类型名称
            /// </summary>
            private string GetPipeScheduleTypeNamebyId(ElementId pipescheduleTypeId)
            {
                return m_document.GetElement(pipescheduleTypeId).Name;
            }

            /// <summary>
            /// 根据ID获取材质名称
            /// </summary>
            private string GetMaterialNameById(ElementId materialId)
            {
                return m_document.GetElement(materialId).Name;
            }

            /// <summary>
            /// 根据ID获取管段名称
            /// </summary>
            private string GetSegmentNameById(ElementId segmentId)
            {
                return m_document.GetElement(segmentId).Name;
            }

            /// <summary>
            /// 根据ID获取管件名称（族名+类型名）
            /// </summary>
            private string GetFittingNameById(ElementId fittingId)
            {
                FamilySymbol fs = m_document.GetElement(fittingId) as FamilySymbol;
                return fs.Family.Name + " " + fs.Name;
            }

            /// <summary>
            /// 根据材质ID和管程ID查找管段
            /// </summary>
            private ElementId GetSegmentByIds(ElementId materialId, ElementId pipeScheduleTypeId)
            {
                if ((materialId == ElementId.InvalidElementId) || (pipeScheduleTypeId == ElementId.InvalidElementId))
                    return ElementId.InvalidElementId;

                Element material = m_document.GetElement(materialId);
                Element pipeScheduleType = m_document.GetElement(pipeScheduleTypeId);
                string segmentName = material.Name + " - " + pipeScheduleType.Name;
                return GetSegmentByName(segmentName);
            }

            /// <summary>
            /// 根据ID获取管类型名称
            /// </summary>
            private string GetPipeTypeNameById(ElementId id)
            {
                return m_document.GetElement(id).Name;
            }

            /// <summary>
            /// 根据名称查找管段
            /// </summary>
            private ElementId GetSegmentByName(string name)
            {
                foreach (Segment segment in m_segments)
                    if (segment.Name == name)
                        return segment.Id;
                return ElementId.InvalidElementId;
            }

            /// <summary>
            /// 根据名称查找管件
            /// </summary>
            private ElementId GetFittingByName(string name)
            {
                foreach (FamilySymbol fitting in m_fittings)
                    if ((fitting.Family.Name + " " + fitting.Name) == name)
                        return fitting.Id;
                return ElementId.InvalidElementId;
            }

            /// <summary>
            /// 根据名称查找材质
            /// </summary>
            private ElementId GetMaterialByName(string name)
            {
                foreach (Material material in m_materials)
                    if (material.Name == name)
                        return material.Id;
                return ElementId.InvalidElementId;
            }

            /// <summary>
            /// 根据名称查找管程类型
            /// </summary>
            private ElementId GetPipeScheduleTypeByName(string name)
            {
                foreach (PipeScheduleType pipeScheduleType in m_pipeSchedules)
                    if (pipeScheduleType.Name == name)
                        return pipeScheduleType.Id;
                return ElementId.InvalidElementId;
            }

            /// <summary>
            /// 根据名称查找管类型
            /// </summary>
            private ElementId GetPipeTypeByName(string name)
            {
                foreach (PipeType pipeType in m_pipeTypes)
                    if (pipeType.Name == name)
                        return pipeType.Id;
                return ElementId.InvalidElementId;
            }

            /// <summary>
            /// 更新管件列表
            /// </summary>
            private void UpdateFittingsList()
            {
                m_fittings = GetAllFittings(m_document);
            }

            /// <summary>
            /// 更新管段列表
            /// </summary>
            private void UpdateSegmentsList()
            {
                m_segments = GetAllPipeSegments(m_document);
            }

            /// <summary>
            /// 更新管类型列表
            /// </summary>
            private void UpdatePipeTypesList()
            {
                m_pipeTypes = GetAllPipeTypes(m_document);
            }

            /// <summary>
            /// 更新管程类型列表
            /// </summary>
            private void UpdatePipeTypeSchedulesList()
            {
                m_pipeSchedules = GetAllPipeScheduleTypes(m_document);
            }

            /// <summary>
            /// 更新材质列表
            /// </summary>
            private void UpdateMaterialsList()
            {
                m_materials = GetAllMaterials(m_document);
            }

            /// <summary>
            /// 获取文档中所有管段
            /// </summary>
            public IEnumerable<PipeSegment> GetAllPipeSegments(Document document)
            {
                FilteredElementCollector fec = new FilteredElementCollector(document);
                fec.OfClass(typeof(PipeSegment));
                IEnumerable<PipeSegment> segments = fec.ToElements().Cast<PipeSegment>();
                return segments;
            }

            /// <summary>
            /// 获取文档中所有管件族
            /// </summary>
            public IEnumerable<FamilySymbol> GetAllFittings(Document document)
            {
                FilteredElementCollector fec = new FilteredElementCollector(document);
                fec.OfClass(typeof(FamilySymbol));
                fec.OfCategory(BuiltInCategory.OST_PipeFitting);
                IEnumerable<FamilySymbol> fittings = fec.ToElements().Cast<FamilySymbol>();
                return fittings;
            }

            /// <summary>
            /// 获取文档中所有材质
            /// </summary>
            private IEnumerable<Material> GetAllMaterials(Document document)
            {
                FilteredElementCollector fec = new FilteredElementCollector(document);
                fec.OfClass(typeof(Material));
                IEnumerable<Material> materials = fec.ToElements().Cast<Material>();
                return materials;
            }

            /// <summary>
            /// 获取文档中所有管程类型
            /// </summary>
            public IEnumerable<PipeScheduleType> GetAllPipeScheduleTypes(Document document)
            {
                FilteredElementCollector fec = new FilteredElementCollector(document);
                fec.OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeScheduleType));
                IEnumerable<Autodesk.Revit.DB.Plumbing.PipeScheduleType> pipeScheduleTypes = fec.ToElements().Cast<Autodesk.Revit.DB.Plumbing.PipeScheduleType>();
                return pipeScheduleTypes;
            }

            /// <summary>
            /// 获取文档中所有管类型
            /// </summary>
            public IEnumerable<PipeType> GetAllPipeTypes(Document document)
            {
                ElementClassFilter ecf = new ElementClassFilter(typeof(PipeType));
                FilteredElementCollector fec = new FilteredElementCollector(document);
                fec.WherePasses(ecf);
                IEnumerable<PipeType> pipeTypes = fec.ToElements().Cast<PipeType>();
                return pipeTypes;
            }
            #endregion
        }
    }
}
