using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DeckPropertyView.xaml 的交互逻辑
    /// </summary>
    public partial class DeckPropertyView : Window
    {
        public DeckPropertyView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 主视图模型 - 管理楼板/压型钢板属性分析逻辑
    /// </summary>
    public class DeckPropertyViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private readonly UIApplication _app;
        private readonly UIDocument _uiDoc;

        private ObservableCollection<FloorInfoModel> _floors;
        private FloorInfoModel _selectedFloor;
        private bool _isLoading;
        private string _statusMessage;
        private AnalysisResultModel _analysisResult;
        #endregion

        #region 公开属性
        /// <summary>楼板列表</summary>
        public ObservableCollection<FloorInfoModel> Floors
        {
            get => _floors;
            set => SetProperty(ref _floors, value);
        }

        /// <summary>选中的楼板</summary>
        public FloorInfoModel SelectedFloor
        {
            get => _selectedFloor;
            set => SetProperty(ref _selectedFloor, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>分析结果</summary>
        public AnalysisResultModel AnalysisResult
        {
            get => _analysisResult;
            set => SetProperty(ref _analysisResult, value);
        }

        /// <summary>是否有选中的楼板</summary>
        public bool HasSelectedFloor => SelectedFloor != null;

        /// <summary>楼板数量</summary>
        public int FloorCount => Floors?.Count ?? 0;
        #endregion

        #region 命令
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        public DeckPropertyViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _app = commandData.Application;
            _uiDoc = _app.ActiveUIDocument;
            _document = _uiDoc.Document;

            Floors = new ObservableCollection<FloorInfoModel>();

            // 初始化命令
            RefreshCommand = new BaseBindingCommand(AnalyzeSelectedFloors);
            ExportCommand = new BaseBindingCommand(ExportAnalysisData, _ => FloorCount > 0);
            CancelCommand = new BaseBindingCommand(CloseWindow);

            // 自动开始分析
            AnalyzeSelectedFloors(null);
        }

        /// <summary>
        /// 分析选中的楼板
        /// </summary>
        private void AnalyzeSelectedFloors(Object obj)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在分析选中的楼板...";
                Floors.Clear();
                SelectedFloor = null;

                var selectionIds = _uiDoc.Selection.GetElementIds();
                if (selectionIds == null || selectionIds.Count == 0)
                {
                    StatusMessage = "请先在Revit中选中一个或多个楼板/压型钢板元素。";
                    AnalysisResult = new AnalysisResultModel
                    {
                        Success = false,
                        Message = "未选中任何元素",
                        FloorCount = 0,
                        DeckCount = 0
                    };
                    return;
                }

                var floorList = selectionIds
                    .Select(id => _document.GetElement(id))
                    .OfType<Floor>()
                    .ToList();

                if (!floorList.Any())
                {
                    StatusMessage = "选中的元素中没有有效的楼板/压型钢板。";
                    AnalysisResult = new AnalysisResultModel
                    {
                        Success = false,
                        Message = "未找到有效的楼板元素",
                        FloorCount = 0,
                        DeckCount = 0
                    };
                    return;
                }

                int totalDeckCount = 0;

                foreach (var floor in floorList)
                {
                    var floorInfo = AnalyzeFloor(floor);
                    Floors.Add(floorInfo);
                    totalDeckCount += floorInfo.Layers.Count(l => l is DeckLayerModel);
                }

                if (Floors.Any())
                    SelectedFloor = Floors.First();

                AnalysisResult = new AnalysisResultModel
                {
                    Success = true,
                    Message = $"分析完成！共处理 {Floors.Count} 个楼板，发现 {totalDeckCount} 个压型钢板层。",
                    FloorCount = Floors.Count,
                    DeckCount = totalDeckCount
                };

                StatusMessage = AnalysisResult.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"分析失败：{ex.Message}";
                AnalysisResult = new AnalysisResultModel
                {
                    Success = false,
                    Message = ex.Message,
                    FloorCount = 0,
                    DeckCount = 0
                };
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 分析单个楼板的复合结构
        /// </summary>
        private FloorInfoModel AnalyzeFloor(Floor floor)
        {
            var floorInfo = new FloorInfoModel
            {
                Id = floor.Id.IntegerValue.ToString(),
                TypeName = floor.FloorType?.Name ?? "未知类型"
            };

            var floorType = floor.FloorType;
            if (floorType == null)
            {
                floorInfo.Layers.Add(new RegularLayerModel
                {
                    LayerType = "未知",
                    MaterialName = "无类型信息",
                    Thickness = 0
                });
                return floorInfo;
            }

            var compoundStructure = floorType.GetCompoundStructure();
            if (compoundStructure == null)
            {
                floorInfo.Layers.Add(new RegularLayerModel
                {
                    LayerType = "无复合结构",
                    MaterialName = "此楼板类型未定义复合结构",
                    Thickness = 0
                });
                return floorInfo;
            }

            var layers = compoundStructure.GetLayers();
            int layerIndex = 0;

            foreach (var layer in layers)
            {
                var isDeckLayer = layer.Function == MaterialFunctionAssignment.StructuralDeck;

                if (isDeckLayer)
                {
                    var deckLayer = AnalyzeDeckLayer(layer);
                    floorInfo.Layers.Add(deckLayer);
                }
                else
                {
                    var regularLayer = AnalyzeRegularLayer(layer);
                    floorInfo.Layers.Add(regularLayer);
                }
                layerIndex++;
            }

            return floorInfo;
        }

        /// <summary>
        /// 分析压型钢板层
        /// </summary>
        private DeckLayerModel AnalyzeDeckLayer(CompoundStructureLayer layer)
        {
            var deckLayer = new DeckLayerModel
            {
                LayerType = "压型钢板层 (Structural Deck)",
                Thickness = layer.Width * 304.8, // 转换为毫米
            };

            // 获取材料信息
            if (layer.MaterialId != ElementId.InvalidElementId)
            {
                var material = _document.GetElement(layer.MaterialId) as Material;
                deckLayer.MaterialName = material?.Name ?? "未知材料";
            }

            // 获取压型钢板轮廓信息
            if (layer.DeckProfileId != ElementId.InvalidElementId)
            {
                var deckProfile = _document.GetElement(layer.DeckProfileId) as FamilySymbol;
                if (deckProfile != null)
                {
                    deckLayer.DeckProfileName = $"{deckProfile.Family?.Name ?? "未知"} : {deckProfile.Name}";
                    AnalyzeDeckParameters(deckProfile, deckLayer.Parameters);
                }
            }

            return deckLayer;
        }

        /// <summary>
        /// 分析压型钢板参数
        /// </summary>
        private void AnalyzeDeckParameters(Element element, ObservableCollection<ParameterInfoModel> parameters)
        {
            foreach (var param in element.Parameters)
            {
                //if (param == null || param.Definition == null) continue;
                if (param == null) continue;

                var paramInfo = new ParameterInfoModel
                {
                    //Name = param.Name,
                    Name = element.Document.GetElement((param as Parameter).Id).Name,
                    StorageType = param.GetType().Name.ToString()
                };

                try
                {
                    paramInfo.Value = GetParameterValue(param as Parameter);
                    parameters.Add(paramInfo);
                }
                catch
                {
                    paramInfo.Value = "无法读取";
                    parameters.Add(paramInfo);
                }
            }
        }

        /// <summary>
        /// 分析普通结构层
        /// </summary>
        private RegularLayerModel AnalyzeRegularLayer(CompoundStructureLayer layer)
        {
            var regularLayer = new RegularLayerModel
            {
                Thickness = layer.Width * 304.8 // 转换为毫米
            };

            // 获取层功能
            switch (layer.Function)
            {
                case MaterialFunctionAssignment.Structure:
                    regularLayer.LayerType = "结构层";
                    break;
                case MaterialFunctionAssignment.Substrate:
                    regularLayer.LayerType = "衬底层";
                    break;
                case MaterialFunctionAssignment.Insulation:
                    regularLayer.LayerType = "保温层";
                    break;
                case MaterialFunctionAssignment.Finish1:
                    regularLayer.LayerType = "面层(外)";
                    break;
                case MaterialFunctionAssignment.Finish2:
                    regularLayer.LayerType = "面层(内)";
                    break;
                case MaterialFunctionAssignment.Membrane:
                    regularLayer.LayerType = "防水层";
                    break;
                default:
                    regularLayer.LayerType = $"其他层 ({layer.Function})";
                    break;
            }

            // 获取材料信息
            if (layer.MaterialId != ElementId.InvalidElementId)
            {
                var material = _document.GetElement(layer.MaterialId) as Material;
                regularLayer.MaterialName = material?.Name ?? "未知材料";
            }
            return regularLayer;
        }

        /// <summary>
        /// 获取参数值（字符串形式）
        /// </summary>
        private string GetParameterValue(Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.Double:
                    return param.AsDouble().ToString("F3");
                case StorageType.ElementId:
                    return param.AsElementId().IntegerValue.ToString();
                case StorageType.String:
                    return param.AsString() ?? "";
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                default:
                    return "未知类型";
            }
        }

        /// <summary>
        /// 导出分析数据到文本
        /// </summary>
        private void ExportAnalysisData(Object obj)
        {
            try
            {
                var exportText = GenerateExportText(null);

                // 复制到剪贴板
                System.Windows.Clipboard.SetText(exportText);
                StatusMessage = "分析数据已复制到剪贴板！";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 生成导出文本
        /// </summary>
        private string GenerateExportText(Object obj)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("=".PadRight(60, '='));
            lines.AppendLine($"楼板/压型钢板分析报告");
            lines.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.AppendLine($"分析楼板数: {FloorCount}");
            lines.AppendLine($"压型钢板层数: {AnalysisResult?.DeckCount ?? 0}");
            lines.AppendLine("=".PadRight(60, '='));
            lines.AppendLine();

            foreach (var floor in Floors)
            {
                lines.AppendLine($"【{floor.DisplayHeader}】");
                lines.AppendLine($"  复合结构层数: {floor.Layers.Count}");
                lines.AppendLine();

                foreach (var layer in floor.Layers)
                {
                    lines.AppendLine($"    {layer.DisplayInfo}");

                    if (layer is DeckLayerModel deckLayer && deckLayer.Parameters.Any())
                    {
                        lines.AppendLine($"    压型钢板参数:");
                        foreach (var param in deckLayer.Parameters)
                        {
                            lines.AppendLine($"      - {param.Name}: {param.Value}");
                        }
                    }
                }
                lines.AppendLine();
            }

            return lines.ToString();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(Object obj)
        {
            if (System.Windows.Application.Current.Windows.Count > 0)
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
        }
    }
    /// <summary>
    /// 复合结构层基类
    /// </summary>
    public abstract class StructureLayerBase : ObserverableObject
    {
        private string _layerType;
        private double _thickness;
        private string _materialName;

        public string LayerType
        {
            get => _layerType;
            set => SetProperty(ref _layerType, value);
        }

        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }

        public string MaterialName
        {
            get => _materialName;
            set => SetProperty(ref _materialName, value);
        }

        public abstract string DisplayInfo { get; }
    }

    /// <summary>
    /// 普通结构层模型
    /// </summary>
    public class RegularLayerModel : StructureLayerBase
    {
        public override string DisplayInfo =>
            $"[{LayerType}] {MaterialName} - 厚度: {Thickness:F2} mm";
    }

    /// <summary>
    /// 压型钢板层模型
    /// </summary>
    public class DeckLayerModel : StructureLayerBase
    {
        private string _deckProfileName;
        private ObservableCollection<ParameterInfoModel> _parameters;

        public string DeckProfileName
        {
            get => _deckProfileName;
            set => SetProperty(ref _deckProfileName, value);
        }

        public ObservableCollection<ParameterInfoModel> Parameters
        {
            get => _parameters;
            set => SetProperty(ref _parameters, value);
        }

        public override string DisplayInfo =>
            $"[压型钢板] {DeckProfileName} - 材料: {MaterialName}";

        public DeckLayerModel()
        {
            Parameters = new ObservableCollection<ParameterInfoModel>();
        }
    }

    /// <summary>
    /// 参数信息模型
    /// </summary>
    public class ParameterInfoModel : ObserverableObject
    {
        private string _name;
        private string _value;
        private string _storageType;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string StorageType
        {
            get => _storageType;
            set => SetProperty(ref _storageType, value);
        }

        public string DisplayText => $"{Name} = {Value}";
    }

    /// <summary>
    /// 楼板信息模型
    /// </summary>
    public class FloorInfoModel : ObserverableObject
    {
        private string _id;
        private string _typeName;
        private ObservableCollection<StructureLayerBase> _layers;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        public ObservableCollection<StructureLayerBase> Layers
        {
            get => _layers;
            set => SetProperty(ref _layers, value);
        }

        public string DisplayHeader => $"楼板 ID: {Id} - {TypeName}";

        public FloorInfoModel()
        {
            Layers = new ObservableCollection<StructureLayerBase>();
        }
    }

    /// <summary>
    /// 分析结果模型
    /// </summary>
    public class AnalysisResultModel : ObserverableObject
    {
        private bool _success;
        private string _message;
        private int _floorCount;
        private int _deckCount;

        public bool Success
        {
            get => _success;
            set => SetProperty(ref _success, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public int FloorCount
        {
            get => _floorCount;
            set => SetProperty(ref _floorCount, value);
        }

        public int DeckCount
        {
            get => _deckCount;
            set => SetProperty(ref _deckCount, value);
        }
    }
    /// <summary>
    /// 层类型到画刷转换器
    /// </summary>
    public class LayerTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var layerType = value as string ?? "";

            if (layerType.Contains("压型钢板") || layerType.Contains("Deck"))
                return new SolidColorBrush(Color.FromRgb(232, 167, 53));  // 橙色

            if (layerType.Contains("结构"))
                return new SolidColorBrush(Color.FromRgb(52, 152, 219));   // 蓝色

            if (layerType.Contains("保温"))
                return new SolidColorBrush(Color.FromRgb(231, 76, 60));    // 红色

            if (layerType.Contains("面层"))
                return new SolidColorBrush(Color.FromRgb(46, 204, 113));   // 绿色

            return new SolidColorBrush(Color.FromRgb(149, 165, 166));       // 灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    /// <summary>
    /// 成功状态到图标转换器
    /// </summary>
    public class SuccessToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is bool success && success) ? "✓" : "✗";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
