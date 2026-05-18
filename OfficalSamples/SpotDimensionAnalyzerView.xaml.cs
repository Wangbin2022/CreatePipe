using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// SpotDimensionAnalyzerView.xaml 的交互逻辑
    /// </summary>
    public partial class SpotDimensionAnalyzerView : Window
    {
        private readonly SpotDimensionAnalyzerViewModel _viewModel;
        public SpotDimensionAnalyzerView(SpotDimensionAnalyzerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 窗口关闭时清理资源
            Closed += (s, e) => _viewModel.CloseCommand.Execute(null);
        }
    }
    /// <summary>
    /// 点标注分析器视图模型
    /// </summary>
    public class SpotDimensionAnalyzerViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly SpotDimensionService _service;
        private readonly ExternalCommandData _commandData;
        private ViewInfo _selectedView;
        private SpotDimensionInfo _selectedSpotDimension;
        private string _statusMessage;
        private bool _isLoading;
        #endregion

        #region 属性绑定
        /// <summary>
        /// 视图列表
        /// </summary>
        public ObservableCollection<ViewInfo> Views { get; set; }

        /// <summary>
        /// 当前选中的视图
        /// </summary>
        public ViewInfo SelectedView
        {
            get => _selectedView;
            set
            {
                _selectedView = value;
                OnPropertyChanged();
                LoadSpotDimensionsAsync();
            }
        }

        /// <summary>
        /// 点标注列表
        /// </summary>
        public ObservableCollection<SpotDimensionInfo> SpotDimensions { get; set; }

        /// <summary>
        /// 当前选中的点标注
        /// </summary>
        public SpotDimensionInfo SelectedSpotDimension
        {
            get => _selectedSpotDimension;
            set
            {
                _selectedSpotDimension = value;
                OnPropertyChanged();
                LoadParameterDetails();
                HighlightInRevit();
            }
        }

        /// <summary>
        /// 参数列表
        /// </summary>
        public ObservableCollection<ParameterItem> Parameters { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
        #endregion

        #region 命令
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        public SpotDimensionAnalyzerViewModel(ExternalCommandData commandData, SpotDimensionService service)
        {
            _commandData = commandData;
            _service = service;

            Views = new ObservableCollection<ViewInfo>();
            SpotDimensions = new ObservableCollection<SpotDimensionInfo>();
            Parameters = new ObservableCollection<ParameterItem>();

            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            CloseCommand = new BaseBindingCommand(ExecuteClose);

            LoadViewsAsync();
        }

        #region 命令执行方法
        /// <summary>
        /// 刷新数据
        /// </summary>
        private async void ExecuteRefresh(Object obj)
        {
            await LoadViewsAsync();
            StatusMessage = "数据已刷新";
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void ExecuteClose(Object obj)
        {
            // 清除高亮
            ClearHighlight();
        }
        #endregion

        #region 业务逻辑
        /// <summary>
        /// 异步加载视图列表
        /// </summary>
        private async System.Threading.Tasks.Task LoadViewsAsync()
        {
            IsLoading = true;
            StatusMessage = "正在加载视图...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var views = _service.GetAllViews();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Views.Clear();
                    foreach (var view in views)
                    {
                        Views.Add(view);
                    }
                    StatusMessage = $"已加载 {Views.Count} 个视图";
                });
            });

            IsLoading = false;
        }

        /// <summary>
        /// 异步加载点标注列表
        /// </summary>
        private async void LoadSpotDimensionsAsync()
        {
            if (SelectedView == null) return;

            IsLoading = true;
            StatusMessage = $"正在加载视图 \"{SelectedView.ViewName}\" 中的点标注...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var spotDimensions = _service.GetSpotDimensionsByView(SelectedView.ViewName);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SpotDimensions.Clear();
                    foreach (var sd in spotDimensions)
                    {
                        SpotDimensions.Add(sd);
                    }
                    StatusMessage = $"找到 {SpotDimensions.Count} 个点标注";
                });
            });

            IsLoading = false;
        }

        /// <summary>
        /// 加载参数详情
        /// </summary>
        private async void LoadParameterDetails()
        {
            if (SelectedSpotDimension?.SpotDimension == null) return;

            IsLoading = true;
            StatusMessage = $"正在分析点标注 (ID: {SelectedSpotDimension.Id.IntegerValue})...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var parameters = _service.GetParameterDetails(SelectedSpotDimension.SpotDimension);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Parameters.Clear();
                    foreach (var param in parameters)
                    {
                        Parameters.Add(param);
                    }
                    StatusMessage = $"已加载 {Parameters.Count} 个参数";
                });
            });

            IsLoading = false;
        }

        /// <summary>
        /// 在Revit中高亮选中的点标注
        /// </summary>
        private void HighlightInRevit()
        {
            if (SelectedSpotDimension?.SpotDimension == null) return;

            try
            {
                var uidoc = _commandData.Application.ActiveUIDocument;
                var selection = uidoc.Selection;

                // 清除之前的选中
                selection.SetElementIds(new System.Collections.Generic.List<ElementId>());

                // 选中当前元素
                selection.SetElementIds(new System.Collections.Generic.List<ElementId>
                    { SelectedSpotDimension.SpotDimension.Id });

                // 显示选中的元素
                uidoc.ShowElements(new System.Collections.Generic.List<ElementId>
                    { SelectedSpotDimension.SpotDimension.Id });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"高亮失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        private void ClearHighlight()
        {
            try
            {
                var uidoc = _commandData.Application.ActiveUIDocument;
                uidoc.Selection.SetElementIds(new System.Collections.Generic.List<ElementId>());
            }
            catch { }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }

    /// <summary>
    /// 点标注数据服务类 - 处理Revit API交互
    /// </summary>
    public class SpotDimensionService
    {
        #region 常量定义
        private const double ToFractionalInches = 0.08333333;  // 英尺转英寸转换系数
        private readonly string _numberFormatter = "#0.000";
        #endregion

        #region 静态数据字典
        // 高程原点选项
        private static readonly List<string> ElevationOrigin = new List<string>
            { "Project", "Shared", "Relative" };

        // 文本方向选项
        private static readonly List<string> TextOrientation = new List<string>
            { "Horizontal Above", "Horizontal Below" };

        // 指示器位置选项
        private static readonly List<string> Indicator = new List<string>
            { "Prefix", "Suffix" };

        // 上下值选项
        private static readonly List<string> TopBottomValue = new List<string>
            { "None", "North / South", "East / West" };

        // 文字背景选项
        private static readonly List<string> TextBackground = new List<string>
            { "Opaque", "Transparent" };
        #endregion

        private readonly UIApplication _uiApp;
        private readonly Document _document;

        public SpotDimensionService(ExternalCommandData commandData)
        {
            _uiApp = commandData?.Application;
            _document = _uiApp?.ActiveUIDocument?.Document;
        }

        /// <summary>
        /// 获取所有点标注数据（按视图分组）
        /// </summary>
        public ObservableCollection<ViewInfo> GetAllViews()
        {
            var views = new ObservableCollection<ViewInfo>();

            try
            {
                // 使用LINQ查询所有点标注
                var spotDimensions = new FilteredElementCollector(_document)
                    .OfClass(typeof(SpotDimension))
                    .Cast<SpotDimension>()
                    .ToList();

                // 获取包含点标注的视图（去重）
                var distinctViews = spotDimensions
                    .Where(sd => sd.View != null)
                    .Select(sd => new ViewInfo
                    {
                        ViewName = sd.View.Name,
                        ViewId = sd.View.Id
                    })
                    .DistinctBy(v => v.ViewId.IntegerValue)
                    .OrderBy(v => v.ViewName);

                foreach (var view in distinctViews)
                {
                    views.Add(view);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取视图失败: {ex.Message}");
            }

            return views;
        }

        /// <summary>
        /// 获取指定视图中的点标注
        /// </summary>
        public ObservableCollection<SpotDimensionInfo> GetSpotDimensionsByView(string viewName)
        {
            var spotDimensions = new ObservableCollection<SpotDimensionInfo>();

            try
            {
                var query = new FilteredElementCollector(_document)
                    .OfClass(typeof(SpotDimension))
                    .Cast<SpotDimension>()
                    .Where(sd => sd.View?.Name == viewName)
                    .OrderBy(sd => sd.Id.IntegerValue);  // 按ID排序

                foreach (var sd in query)
                {
                    spotDimensions.Add(new SpotDimensionInfo
                    {
                        SpotDimension = sd,
                        Id = sd.Id,
                        ViewName = sd.View?.Name,
                        Category = sd.Category?.Name,
                        DisplayName = GenerateDisplayName(sd)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取点标注失败: {ex.Message}");
            }

            return spotDimensions;
        }

        /// <summary>
        /// 生成点标注的显示名称
        /// </summary>
        private string GenerateDisplayName(SpotDimension sd)
        {
            try
            {
                // 尝试获取点标注的值
                var valueParam = sd.get_Parameter(BuiltInParameter.DIM_VALUE_LENGTH);
                if (valueParam != null && valueParam.HasValue)
                {
                    double value = valueParam.AsDouble();
                    return $"ID:{sd.Id.IntegerValue} - {value:F3}'";
                }
            }
            catch { }

            return $"ID:{sd.Id.IntegerValue} - {sd.Category?.Name ?? "Unknown"}";
        }

        /// <summary>
        /// 获取点标注的参数信息（返回参数项集合）
        /// </summary>
        public ObservableCollection<ParameterItem> GetParameterDetails(SpotDimension spotDimension)
        {
            var parameters = new ObservableCollection<ParameterItem>();

            if (spotDimension is null) return parameters;

            try
            {
                var dimensionType = spotDimension.DimensionType;
                if (dimensionType is null) return parameters;

                // 获取类别信息
                AddParameter(parameters, "Category", spotDimension.Category?.Name ?? "Unknown");

                // 引线箭头
                GetLeaderArrowheadParameter(parameters, dimensionType);

                // 引线线宽
                GetIntegerParameter(parameters, dimensionType, BuiltInParameter.SPOT_ELEV_LINE_PEN, "Leader Line Weight");

                // 引线箭头线宽
                GetIntegerParameter(parameters, dimensionType, BuiltInParameter.SPOT_ELEV_TICK_MARK_PEN, "Leader Arrowhead Line Weight");

                // 符号
                GetSymbolParameter(parameters, dimensionType, BuiltInParameter.SPOT_ELEV_SYMBOL, "Symbol");

                // 文字大小
                GetLengthParameter(parameters, dimensionType, BuiltInParameter.TEXT_SIZE, "Text Size");

                // 文字与引线偏移
                GetLengthParameter(parameters, dimensionType, BuiltInParameter.SPOT_TEXT_FROM_LEADER, "Text Offset from Leader");

                // 文字与符号偏移
                GetLengthParameter(parameters, dimensionType, BuiltInParameter.SPOT_ELEV_TEXT_HORIZ_OFFSET, "Text Offset from Symbol");

                // 根据类别添加特定参数
                if (spotDimension.Category?.Name == "Spot Coordinates")
                {
                    GetCoordinateSpecificParameters(parameters, dimensionType, spotDimension);
                }
                else
                {
                    GetElevationSpecificParameters(parameters, dimensionType, spotDimension);
                }

                // 通用参数
                GetCommonParameters(parameters, dimensionType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取参数失败: {ex.Message}");
                AddParameter(parameters, "Error", ex.Message);
            }

            return parameters;
        }

        #region 私有辅助方法 - 参数获取
        /// <summary>
        /// 添加参数项
        /// </summary>
        private void AddParameter(ObservableCollection<ParameterItem> list, string name, string value)
        {
            list.Add(new ParameterItem { ParameterName = name, ParameterValue = value });
        }

        /// <summary>
        /// 获取整数类型参数
        /// </summary>
        private void GetIntegerParameter(ObservableCollection<ParameterItem> list,
            DimensionType dimType, BuiltInParameter bip, string displayName)
        {
            var param = dimType.get_Parameter(bip);
            if (param != null && param.HasValue)
            {
                AddParameter(list, displayName, param.AsInteger().ToString());
            }
        }

        /// <summary>
        /// 获取长度类型参数（转换为英寸显示）
        /// </summary>
        private void GetLengthParameter(ObservableCollection<ParameterItem> list,
            DimensionType dimType, BuiltInParameter bip, string displayName)
        {
            var param = dimType.get_Parameter(bip);
            if (param != null && param.HasValue)
            {
                double valueInInches = param.AsDouble() / ToFractionalInches;
                AddParameter(list, displayName, $"{valueInInches.ToString(_numberFormatter)}\"");
            }
        }

        /// <summary>
        /// 获取引线箭头参数
        /// </summary>
        private void GetLeaderArrowheadParameter(ObservableCollection<ParameterItem> list,
            DimensionType dimType)
        {
            var param = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_LEADER_ARROWHEAD);
            if (param != null && param.HasValue)
            {
                var elementId = param.AsElementId();
                string value = (elementId.IntegerValue == -1) ? "None" :
                               _document.GetElement(elementId)?.Name ?? "None";
                AddParameter(list, "Leader Arrowhead", value);
            }
        }

        /// <summary>
        /// 获取符号参数
        /// </summary>
        private void GetSymbolParameter(ObservableCollection<ParameterItem> list,
            DimensionType dimType, BuiltInParameter bip, string displayName)
        {
            var param = dimType.get_Parameter(bip);
            if (param != null && param.HasValue)
            {
                var elementId = param.AsElementId();
                string value = (elementId.IntegerValue == -1) ? "None" :
                               _document.GetElement(elementId)?.Name ?? "None";
                AddParameter(list, displayName, value);
            }
        }

        /// <summary>
        /// 获取点坐标特有参数
        /// </summary>
        private void GetCoordinateSpecificParameters(ObservableCollection<ParameterItem> list,
            DimensionType dimType, SpotDimension spotDimension)
        {
            // 坐标原点
            GetIntegerParameter(list, dimType, BuiltInParameter.SPOT_COORDINATE_BASE, "Coordinate Origin");

            // 顶部值方向
            var topParam = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_TOP_VALUE);
            if (topParam != null && topParam.HasValue)
            {
                int index = topParam.AsInteger();
                string value = (index >= 0 && index < TopBottomValue.Count) ? TopBottomValue[index] : "None";
                AddParameter(list, "Top Value", value);
            }

            // 底部值方向
            var bottomParam = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_BOT_VALUE);
            if (bottomParam != null && bottomParam.HasValue)
            {
                int index = bottomParam.AsInteger();
                string value = (index >= 0 && index < TopBottomValue.Count) ? TopBottomValue[index] : "None";
                AddParameter(list, "Bottom Value", value);
            }

            // 南北指示器
            GetStringParameter(list, dimType, BuiltInParameter.SPOT_ELEV_IND_NS, "North / South Indicator");

            // 东西指示器
            GetStringParameter(list, dimType, BuiltInParameter.SPOT_ELEV_IND_EW, "East / West Indicator");
        }

        /// <summary>
        /// 获取点高程特有参数
        /// </summary>
        private void GetElevationSpecificParameters(ObservableCollection<ParameterItem> list,
            DimensionType dimType, SpotDimension spotDimension)
        {
            // 实例参数 - 数值
            var valueParam = spotDimension.get_Parameter(BuiltInParameter.DIM_VALUE_LENGTH);
            if (valueParam != null && valueParam.HasValue)
            {
                AddParameter(list, "Value", $"{valueParam.AsDouble().ToString(_numberFormatter)}'");
            }

            // 高程原点
            var originParam = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_BASE);
            if (originParam != null && originParam.HasValue)
            {
                int index = originParam.AsInteger();
                string value = (index >= 0 && index < ElevationOrigin.Count) ? ElevationOrigin[index] : "Unknown";
                AddParameter(list, "Elevation Origin", value);
            }

            // 高程指示器
            GetStringParameter(list, dimType, BuiltInParameter.SPOT_ELEV_IND_ELEVATION, "Elevation Indicator");
        }

        /// <summary>
        /// 获取字符串类型参数
        /// </summary>
        private void GetStringParameter(ObservableCollection<ParameterItem> list,
            DimensionType dimType, BuiltInParameter bip, string displayName)
        {
            var param = dimType.get_Parameter(bip);
            if (param != null && param.HasValue)
            {
                AddParameter(list, displayName, param.AsString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 获取通用参数
        /// </summary>
        private void GetCommonParameters(ObservableCollection<ParameterItem> list,
            DimensionType dimType)
        {
            // 文本方向
            var orientationParam = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_TEXT_ORIENTATION);
            if (orientationParam != null && orientationParam.HasValue)
            {
                int index = orientationParam.AsInteger();
                string value = (index >= 0 && index < TextOrientation.Count) ? TextOrientation[index] : "Unknown";
                AddParameter(list, "Text Orientation", value);
            }

            // 指示器位置
            var indicatorParam = dimType.get_Parameter(BuiltInParameter.SPOT_ELEV_IND_TYPE);
            if (indicatorParam != null && indicatorParam.HasValue)
            {
                int index = indicatorParam.AsInteger();
                string value = (index >= 0 && index < Indicator.Count) ? Indicator[index] : "Unknown";
                AddParameter(list, "Indicator Position", value);
            }

            // 文字字体
            GetStringParameter(list, dimType, BuiltInParameter.TEXT_FONT, "Text Font");

            // 文字背景
            var bgParam = dimType.get_Parameter(BuiltInParameter.DIM_TEXT_BACKGROUND);
            if (bgParam != null && bgParam.HasValue)
            {
                int index = bgParam.AsInteger();
                string value = (index >= 0 && index < TextBackground.Count) ? TextBackground[index] : "Unknown";
                AddParameter(list, "Text Background", value);
            }
        }
        #endregion
    }

    /// <summary>
    /// 点标注参数项模型
    /// </summary>
    public class ParameterItem : INotifyPropertyChanged
    {
        private string _parameterName;
        private string _parameterValue;

        public string ParameterName
        {
            get => _parameterName;
            set { _parameterName = value; OnPropertyChanged(); }
        }

        public string ParameterValue
        {
            get => _parameterValue;
            set { _parameterValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 点标注信息模型
    /// </summary>
    public class SpotDimensionInfo : ObserverableObject
    {
        private string _displayName;
        private SpotDimension _spotDimension;

        public SpotDimension SpotDimension
        {
            get => _spotDimension;
            set { _spotDimension = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string ViewName { get; set; }
        public ElementId Id { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// 视图信息模型
    /// </summary>
    public class ViewInfo
    {
        public string ViewName { get; set; }
        public ElementId ViewId { get; set; }

        public override string ToString() => ViewName;
    }

    /// <summary>
    /// LINQ扩展方法 - DistinctBy实现
    /// </summary>
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
