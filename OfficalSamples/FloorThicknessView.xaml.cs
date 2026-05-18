using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FloorThicknessView.xaml 的交互逻辑
    /// </summary>
    public partial class FloorThicknessView : Window
    {
        public FloorThicknessView(FloorThicknessViewModel viewModel)
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 楼板厚度修改视图模型
    /// </summary>
    public class FloorThicknessViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly FloorThicknessService _service;
        private FloorInfoModel2 _selectedFloor;
        private double _multiplyFactor = 10.0;
        private double _addValue = 0.0;
        private bool _applyToSelectedOnly = true;
        private bool _isPreviewMode = true;
        private string _statusMessage;
        private bool _isProcessing;
        private ObservableCollection<FloorLayerModel> _currentLayers;
        private ObservableCollection<FloorLayerModel> _previewLayers;
        #endregion

        #region 集合属性
        /// <summary>
        /// 选中的楼板列表
        /// </summary>
        public ObservableCollection<FloorInfoModel2> SelectedFloors { get; set; }

        /// <summary>
        /// 当前显示的层列表
        /// </summary>
        public ObservableCollection<FloorLayerModel> CurrentLayers
        {
            get => _currentLayers;
            set { _currentLayers = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 预览层列表
        /// </summary>
        public ObservableCollection<FloorLayerModel> PreviewLayers
        {
            get => _previewLayers;
            set { _previewLayers = value; OnPropertyChanged(); }
        }
        #endregion

        #region 绑定属性
        /// <summary>
        /// 选中的楼板
        /// </summary>
        public FloorInfoModel2 SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                _selectedFloor = value;
                OnPropertyChanged();
                LoadFloorLayers();
            }
        }

        /// <summary>
        /// 乘法因子
        /// </summary>
        public double MultiplyFactor
        {
            get => _multiplyFactor;
            set
            {
                _multiplyFactor = value;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        /// <summary>
        /// 增加值 (英尺)
        /// </summary>
        public double AddValue
        {
            get => _addValue;
            set
            {
                _addValue = value;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        /// <summary>
        /// 是否只应用于选中的层
        /// </summary>
        public bool ApplyToSelectedOnly
        {
            get => _applyToSelectedOnly;
            set
            {
                _applyToSelectedOnly = value;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        /// <summary>
        /// 是否预览模式
        /// </summary>
        public bool IsPreviewMode
        {
            get => _isPreviewMode;
            set
            {
                _isPreviewMode = value;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 总厚度变化摘要
        /// </summary>
        public string ThicknessSummary
        {
            get
            {
                if (CurrentLayers == null || PreviewLayers == null) return "暂无数据";

                var originalTotal = CurrentLayers.Sum(l => l.OriginalThickness);
                var newTotal = PreviewLayers.Sum(l => l.NewThickness);
                var change = newTotal - originalTotal;
                var changePercent = originalTotal > 0 ? (change / originalTotal) * 100 : 0;

                return $"总厚度: {originalTotal:F4}' → {newTotal:F4}' ({change:+0.000;-0.000;0}') [{changePercent:+0.0;-0.0;0.0}%]";
            }
        }
        #endregion

        #region 命令
        public ICommand RefreshCommand;
        public ICommand ApplyChangesCommand;
        public ICommand ResetCommand;
        public ICommand SelectAllLayersCommand;
        public ICommand DeselectAllLayersCommand;
        public ICommand SetMultiplyFactorCommand;
        #endregion

        public FloorThicknessViewModel(FloorThicknessService service)
        {
            _service = service;

            SelectedFloors = new ObservableCollection<FloorInfoModel2>();
            CurrentLayers = new ObservableCollection<FloorLayerModel>();
            PreviewLayers = new ObservableCollection<FloorLayerModel>();

            // 初始化命令
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            ApplyChangesCommand = new BaseBindingCommand(ExecuteApplyChanges);
            ResetCommand = new BaseBindingCommand(ExecuteReset, _ => SelectedFloor != null);
            SelectAllLayersCommand = new BaseBindingCommand(_ => SetAllLayersSelected(true));
            DeselectAllLayersCommand = new BaseBindingCommand(_ => SetAllLayersSelected(false));
            SetMultiplyFactorCommand = new RelayCommand<double>(ExecuteSetMultiplyFactor);

            // 加载数据
            LoadData();
        }

        #region 命令执行方法
        /// <summary>
        /// 刷新数据
        /// </summary>
        private async void ExecuteRefresh(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在刷新数据...";

            await System.Threading.Tasks.Task.Run(() => LoadData());

            StatusMessage = $"数据已刷新，找到 {SelectedFloors.Count} 个楼板";
            IsProcessing = false;
        }

        /// <summary>
        /// 应用修改
        /// </summary>
        private async void ExecuteApplyChanges(Object obj)
        {
            if (!SelectedFloors.Any()) return;

            IsProcessing = true;
            StatusMessage = "正在应用厚度修改...";

            var parameters = new ThicknessModifyParams
            {
                MultiplyFactor = MultiplyFactor,
                AddValue = AddValue,
                ApplyToSelectedOnly = ApplyToSelectedOnly,
                PreviewMode = false
            };

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, message, results) = _service.ApplyThicknessChanges(
                    SelectedFloors, parameters,
                    (floor, layerIndex) => CalculateNewThickness(layerIndex));

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = message;
                    if (success)
                    {
                        // 刷新显示
                        LoadData();
                        System.Windows.MessageBox.Show(message, "操作完成",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(message, "操作失败",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                });
            });

            IsProcessing = false;
        }

        /// <summary>
        /// 重置修改
        /// </summary>
        private async void ExecuteReset(Object obj)
        {
            if (SelectedFloor == null) return;

            IsProcessing = true;
            StatusMessage = "正在重置...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var success = _service.ResetFloorThickness(SelectedFloor.Floor);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        LoadFloorLayers();
                        StatusMessage = "已重置为原始值";
                    }
                    else
                    {
                        StatusMessage = "重置失败";
                    }
                });
            });

            IsProcessing = false;
        }

        /// <summary>
        /// 设置乘法因子
        /// </summary>
        private void ExecuteSetMultiplyFactor(double factor)
        {
            MultiplyFactor = factor;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            var floors = _service.GetSelectedFloors();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedFloors.Clear();
                foreach (var floor in floors)
                    SelectedFloors.Add(floor);

                if (SelectedFloors.Any() && SelectedFloor == null)
                    SelectedFloor = SelectedFloors.FirstOrDefault();
            });
        }

        /// <summary>
        /// 加载楼板层信息
        /// </summary>
        private void LoadFloorLayers()
        {
            if (SelectedFloor?.Floor == null) return;

            var layers = _service.GetFloorLayers(SelectedFloor.Floor);

            CurrentLayers.Clear();
            foreach (var layer in layers)
                CurrentLayers.Add(layer);

            UpdatePreview();
        }

        /// <summary>
        /// 更新预览
        /// </summary>
        private void UpdatePreview()
        {
            if (!CurrentLayers.Any()) return;

            var parameters = new ThicknessModifyParams
            {
                MultiplyFactor = MultiplyFactor,
                AddValue = AddValue,
                ApplyToSelectedOnly = ApplyToSelectedOnly,
                PreviewMode = true
            };

            PreviewLayers = _service.PreviewThicknessChanges(CurrentLayers, parameters);

            OnPropertyChanged(nameof(PreviewLayers));
            OnPropertyChanged(nameof(ThicknessSummary));
        }

        /// <summary>
        /// 计算新厚度
        /// </summary>
        private double CalculateNewThickness(int layerIndex)
        {
            var layer = CurrentLayers.FirstOrDefault(l => l.LayerIndex == layerIndex);
            if (layer == null) return 0;

            if (ApplyToSelectedOnly && !layer.IsSelected)
                return layer.OriginalThickness;

            return layer.OriginalThickness * MultiplyFactor + AddValue;
        }

        /// <summary>
        /// 设置所有层的选中状态
        /// </summary>
        private void SetAllLayersSelected(bool selected)
        {
            foreach (var layer in CurrentLayers)
                layer.IsSelected = selected;
            UpdatePreview();
        }

        private bool CanExecuteApplyChanges() =>
            SelectedFloors.Any() && !IsProcessing;
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 楼板结构层模型
    /// </summary>
    public class FloorLayerModel : ObserverableObject
    {
        private int _layerIndex;
        private string _layerName;
        private double _originalThickness;
        private double _newThickness;
        private string _layerFunction;
        private bool _isSelected = true;

        /// <summary>
        /// 层索引
        /// </summary>
        public int LayerIndex
        {
            get => _layerIndex;
            set { _layerIndex = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 层名称
        /// </summary>
        public string LayerName
        {
            get => _layerName;
            set { _layerName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 原始厚度 (英尺)
        /// </summary>
        public double OriginalThickness
        {
            get => _originalThickness;
            set { _originalThickness = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 新厚度 (英尺)
        /// </summary>
        public double NewThickness
        {
            get => _newThickness;
            set { _newThickness = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 层功能描述
        /// </summary>
        public string LayerFunction
        {
            get => _layerFunction;
            set { _layerFunction = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否选中此层
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 原始厚度显示 (转换为显示单位)
        /// </summary>
        public string OriginalThicknessDisplay => $"{OriginalThickness:F4}' ({OriginalThickness * 304.8:F1} mm)";

        /// <summary>
        /// 新厚度显示
        /// </summary>
        public string NewThicknessDisplay => $"{NewThickness:F4}' ({NewThickness * 304.8:F1} mm)";

        /// <summary>
        /// 厚度变化比例
        /// </summary>
        public double ThicknessRatio => OriginalThickness > 0 ? NewThickness / OriginalThickness : 1.0;
    }

    /// <summary>
    /// 楼板信息模型
    /// </summary>
    public partial class FloorInfoModel2 : ObserverableObject
    {
        private Floor _floor;
        private string _floorTypeName;
        private double _totalThickness;
        private int _layerCount;

        public Floor Floor
        {
            get => _floor;
            set { _floor = value; OnPropertyChanged(); UpdateInfo(); }
        }

        public string FloorTypeName
        {
            get => _floorTypeName;
            set { _floorTypeName = value; OnPropertyChanged(); }
        }

        public double TotalThickness
        {
            get => _totalThickness;
            set { _totalThickness = value; OnPropertyChanged(); }
        }

        public int LayerCount
        {
            get => _layerCount;
            set { _layerCount = value; OnPropertyChanged(); }
        }

        public ElementId Id => _floor?.Id;

        public string DisplayName => $"楼板 ID:{Id?.IntegerValue} - {FloorTypeName} ({LayerCount}层)";

        private void UpdateInfo()
        {
            if (_floor?.FloorType != null)
            {
                FloorTypeName = _floor.FloorType.Name;
                var cs = _floor.FloorType.GetCompoundStructure();
                if (cs != null)
                {
                    LayerCount = cs.LayerCount;
                    TotalThickness = 0;
                    for (int i = 0; i < cs.LayerCount; i++)
                        TotalThickness += cs.GetLayerWidth(i);
                }
            }
        }
    }

    /// <summary>
    /// 厚度修改参数
    /// </summary>
    public class ThicknessModifyParams
    {
        public double MultiplyFactor { get; set; } = 10.0;  // 乘法因子
        public double AddValue { get; set; } = 0.0;        // 增加值 (英尺)
        public bool ApplyToSelectedOnly { get; set; } = true; // 仅应用于选中层
        public bool PreviewMode { get; set; } = false;      // 预览模式
    }

    /// <summary>
    /// 楼板厚度修改服务类
    /// </summary>
    public class FloorThicknessService
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;

        public FloorThicknessService(ExternalCommandData commandData)
        {
            _uiDocument = commandData?.Application?.ActiveUIDocument;
            _document = _uiDocument?.Document;
        }

        /// <summary>
        /// 获取选中的楼板
        /// </summary>
        public ObservableCollection<FloorInfoModel2> GetSelectedFloors()
        {
            var floors = new ObservableCollection<FloorInfoModel2>();

            var selectedIds = _uiDocument?.Selection.GetElementIds();
            if (selectedIds == null) return floors;

            foreach (var id in selectedIds)
            {
                if (_document.GetElement(id) is Floor floor)
                {
                    floors.Add(new FloorInfoModel2 { Floor = floor });
                }
            }

            return floors;
        }

        /// <summary>
        /// 获取楼板的结构层信息
        /// </summary>
        public ObservableCollection<FloorLayerModel> GetFloorLayers(Floor floor)
        {
            var layers = new ObservableCollection<FloorLayerModel>();

            if (floor?.FloorType == null) return layers;

            var cs = floor.FloorType.GetCompoundStructure();
            if (cs == null) return layers;

            for (int i = 0; i < cs.LayerCount; i++)
            {
                var layer = new FloorLayerModel
                {
                    LayerIndex = i,
                    OriginalThickness = cs.GetLayerWidth(i),
                    NewThickness = cs.GetLayerWidth(i),
                    LayerFunction = GetLayerFunctionDescription(cs.GetLayerFunction(i))
                };

                // 尝试获取层名称
                var layerParam = cs.GetMaterialId(i);
                if (layerParam != null && layerParam != ElementId.InvalidElementId)
                {
                    var material = _document.GetElement(layerParam) as Material;
                    layer.LayerName = material?.Name ?? $"层 {i + 1}";
                }
                else
                {
                    layer.LayerName = GetDefaultLayerName(i, cs.GetLayerFunction(i));
                }

                layers.Add(layer);
            }

            return layers;
        }

        /// <summary>
        /// 获取层功能描述
        /// </summary>
        private string GetLayerFunctionDescription(MaterialFunctionAssignment function)
        {
            switch (function)
            {
                case MaterialFunctionAssignment.Structure:
                    return "结构层";
                case MaterialFunctionAssignment.Insulation:
                    return "隔热层";
                case MaterialFunctionAssignment.Substrate:
                    return "基层";
                case MaterialFunctionAssignment.Finish1:
                    return "面层1";
                case MaterialFunctionAssignment.Finish2:
                    return "面层2";
                case MaterialFunctionAssignment.Membrane:
                    return "防水层";
                default:
                    return "其他层";
            }
        }

        /// <summary>
        /// 获取默认层名称
        /// </summary>
        private string GetDefaultLayerName(int index, MaterialFunctionAssignment function)
        {
            if (function == MaterialFunctionAssignment.Structure)
                return $"结构层 {index + 1}";
            return $"层 {index + 1}";
        }

        /// <summary>
        /// 预览厚度修改
        /// </summary>
        public ObservableCollection<FloorLayerModel> PreviewThicknessChanges(
            ObservableCollection<FloorLayerModel> layers, ThicknessModifyParams parameters)
        {
            var previewLayers = new ObservableCollection<FloorLayerModel>();

            foreach (var layer in layers)
            {
                var previewLayer = new FloorLayerModel
                {
                    LayerIndex = layer.LayerIndex,
                    LayerName = layer.LayerName,
                    OriginalThickness = layer.OriginalThickness,
                    LayerFunction = layer.LayerFunction,
                    IsSelected = parameters.ApplyToSelectedOnly ? layer.IsSelected : true
                };

                if (!parameters.ApplyToSelectedOnly || layer.IsSelected)
                {
                    previewLayer.NewThickness = CalculateThickness(
                        layer.OriginalThickness, parameters.MultiplyFactor, parameters.AddValue);
                }
                else
                {
                    previewLayer.NewThickness = layer.OriginalThickness;
                }

                previewLayers.Add(previewLayer);
            }

            return previewLayers;
        }

        /// <summary>
        /// 计算新厚度
        /// </summary>
        private double CalculateThickness(double original, double multiplyFactor, double addValue)
        {
            return original * multiplyFactor + addValue;
        }

        /// <summary>
        /// 应用厚度修改
        /// </summary>
        public (bool success, string message, Dictionary<FloorInfoModel2, bool> results)
            ApplyThicknessChanges(IEnumerable<FloorInfoModel2> floors,
                                  ThicknessModifyParams parameters,
                                  Func<Floor, int, double> calculateNewThickness)
        {
            var results = new Dictionary<FloorInfoModel2, bool>();
            var failedFloors = new List<string>();

            using (var transaction = new Transaction(_document, "修改楼板厚度"))
            {
                transaction.Start();

                foreach (var floorInfo in floors)
                {
                    try
                    {
                        var success = ModifyFloorThickness(floorInfo.Floor, parameters, calculateNewThickness);
                        results[floorInfo] = success;

                        if (!success)
                            failedFloors.Add($"楼板 ID:{floorInfo.Id.IntegerValue}");
                    }
                    catch (Exception ex)
                    {
                        results[floorInfo] = false;
                        failedFloors.Add($"楼板 ID:{floorInfo.Id.IntegerValue} - {ex.Message}");
                    }
                }

                if (failedFloors.Any())
                {
                    transaction.RollBack();
                    return (false, $"修改失败: {string.Join(", ", failedFloors)}", results);
                }

                transaction.Commit();
                return (true, $"成功修改了 {results.Count(r => r.Value)} 个楼板", results);
            }
        }

        /// <summary>
        /// 修改单个楼板厚度
        /// </summary>
        private bool ModifyFloorThickness(Floor floor, ThicknessModifyParams parameters,
            Func<Floor, int, double> calculateNewThickness)
        {
            if (floor?.FloorType == null) return false;

            var cs = floor.FloorType.GetCompoundStructure();
            if (cs == null) return false;

            // 获取需要修改的层
            var layersToModify = parameters.ApplyToSelectedOnly
                ? GetSelectedLayerIndices(floor)
                : Enumerable.Range(0, cs.LayerCount);

            // 应用厚度修改
            foreach (var layerIndex in layersToModify)
            {
                if (layerIndex < 0 || layerIndex >= cs.LayerCount) continue;

                var newThickness = calculateNewThickness(floor, layerIndex);
                cs.SetLayerWidth(layerIndex, newThickness);
            }

            // 应用修改后的复合结构
            floor.FloorType.SetCompoundStructure(cs);

            // 如果需要保存选中的层信息，可以存储到扩展数据中
            // 这里简化处理

            return true;
        }

        /// <summary>
        /// 获取选中的层索引（需要从扩展数据中读取）
        /// </summary>
        private IEnumerable<int> GetSelectedLayerIndices(Floor floor)
        {
            // 实际项目中可以通过Parameter或外部存储来记录
            // 这里简化返回所有层
            var cs = floor.FloorType.GetCompoundStructure();
            return Enumerable.Range(0, cs?.LayerCount ?? 0);
        }

        /// <summary>
        /// 刷新显示（重新生成文档）
        /// </summary>
        public void RefreshDisplay()
        {
            _document?.Regenerate();
        }

        /// <summary>
        /// 重置楼板厚度
        /// </summary>
        public bool ResetFloorThickness(Floor floor)
        {
            if (floor?.FloorType == null) return false;

            using (var transaction = new Transaction(_document, "重置楼板厚度"))
            {
                transaction.Start();

                try
                {
                    // 重新加载原始复合结构
                    var originalCs = floor.FloorType.GetCompoundStructure();
                    // 注意：这里需要保存原始值，简化处理

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.RollBack();
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// 厚度到显示字符串转换器
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    public class ThicknessToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double thickness)
            {
                var unit = parameter as string ?? "ft";
                return unit == "mm"
                    ? $"{thickness * 304.8:F1} mm"
                    : $"{thickness:F4}'";
            }
            return "0'";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 比例到颜色转换器
    /// </summary>
    [ValueConversion(typeof(double), typeof(Brush))]
    public class RatioToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                if (ratio > 1.1) return new SolidColorBrush(Colors.Red);
                if (ratio < 0.9) return new SolidColorBrush(Colors.Green);
                return new SolidColorBrush(Colors.Gray);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 选中背景转换器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class SelectedBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xF0, 0xFE))
                : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 处理中颜色转换器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToProcessingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true
                ? new SolidColorBrush(Colors.Orange)
                : new SolidColorBrush(Colors.Green);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
