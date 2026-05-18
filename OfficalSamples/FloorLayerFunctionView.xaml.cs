using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FloorLayerFunctionView.xaml 的交互逻辑
    /// </summary>
    public partial class FloorLayerFunctionView : Window
    {
        private readonly FloorLayerFunctionViewModel _viewModel;
        public FloorLayerFunctionView(FloorLayerFunctionViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            // 窗口关闭时清理
            Closed += (s, e) => _viewModel.CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// 楼板结构层功能视图模型
    /// </summary>
    public class FloorLayerFunctionViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly FloorLayerService _service;
        private FloorInfoModel3 _selectedFloor;
        private ObservableCollection<FloorLayerFunctionModel> _layers;
        private string _statusMessage;
        private bool _isLoading;
        private string _floorSummary;
        #endregion

        #region 集合属性
        /// <summary>
        /// 结构层列表
        /// </summary>
        public ObservableCollection<FloorLayerFunctionModel> Layers
        {
            get => _layers;
            set { _layers = value; OnPropertyChanged(); }
        }
        #endregion

        #region 绑定属性
        /// <summary>
        /// 选中的楼板信息
        /// </summary>
        public FloorInfoModel3 SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                _selectedFloor = value;
                OnPropertyChanged();
                LoadLayerFunctions();
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
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 楼板摘要信息
        /// </summary>
        public string FloorSummary
        {
            get => _floorSummary;
            set { _floorSummary = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 总层数
        /// </summary>
        public int TotalLayerCount => Layers?.Count ?? 0;

        /// <summary>
        /// 结构层数量
        /// </summary>
        public int StructuralLayerCount => Layers?.Count(l =>
            l.FunctionType == MaterialFunctionAssignment.Structure) ?? 0;

        /// <summary>
        /// 面层数量
        /// </summary>
        public int FinishLayerCount => Layers?.Count(l =>
            l.FunctionType == MaterialFunctionAssignment.Finish1 ||
            l.FunctionType == MaterialFunctionAssignment.Finish2) ?? 0;

        /// <summary>
        /// 总厚度
        /// </summary>
        public double TotalThickness => Layers?.Sum(l => l.Thickness) ?? 0;

        /// <summary>
        /// 总厚度显示
        /// </summary>
        public string TotalThicknessDisplay => $"{TotalThickness:F4}' ({TotalThickness * 304.8:F1} mm)";
        #endregion

        #region 命令
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        public FloorLayerFunctionViewModel(FloorLayerService service)
        {
            _service = service;
            Layers = new ObservableCollection<FloorLayerFunctionModel>();

            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            CloseCommand = new BaseBindingCommand(ExecuteClose);

            LoadData();
        }

        #region 命令执行方法
        /// <summary>
        /// 刷新数据
        /// </summary>
        private async void ExecuteRefresh(Object obj)
        {
            await LoadDataAsync();
            StatusMessage = "数据已刷新";
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void ExecuteClose(Object obj)
        {
            // 清理资源
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 同步加载数据
        /// </summary>
        private void LoadData()
        {
            IsLoading = true;
            StatusMessage = "正在加载楼板信息...";

            try
            {
                var floor = _service.GetSelectedFloor();

                if (floor == null)
                {
                    StatusMessage = "请选择一个楼板";
                    SelectedFloor = null;
                    return;
                }

                if (!_service.IsValidFloor(floor, out string error))
                {
                    StatusMessage = error;
                    SelectedFloor = null;
                    return;
                }

                SelectedFloor = floor;
                UpdateFloorSummary();
                StatusMessage = $"已加载楼板: {floor.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 异步加载数据
        /// </summary>
        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "正在加载楼板信息...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var floor = _service.GetSelectedFloor();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (floor == null)
                    {
                        StatusMessage = "请选择一个楼板";
                        SelectedFloor = null;
                        return;
                    }

                    if (!_service.IsValidFloor(floor, out string error))
                    {
                        StatusMessage = error;
                        SelectedFloor = null;
                        return;
                    }

                    SelectedFloor = floor;
                    UpdateFloorSummary();
                    StatusMessage = $"已加载楼板: {floor.DisplayName}";
                });
            });

            IsLoading = false;
        }

        /// <summary>
        /// 加载结构层功能
        /// </summary>
        private void LoadLayerFunctions()
        {
            if (SelectedFloor?.Floor == null) return;

            IsLoading = true;
            StatusMessage = "正在分析结构层...";

            try
            {
                var layers = _service.GetLayerFunctions(SelectedFloor.Floor);

                Layers.Clear();
                foreach (var layer in layers)
                {
                    Layers.Add(layer);
                }

                UpdateFloorSummary();
                StatusMessage = $"已加载 {Layers.Count} 个结构层";

                OnPropertyChanged(nameof(TotalLayerCount));
                OnPropertyChanged(nameof(StructuralLayerCount));
                OnPropertyChanged(nameof(FinishLayerCount));
                OnPropertyChanged(nameof(TotalThicknessDisplay));
            }
            catch (Exception ex)
            {
                StatusMessage = $"分析失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新楼板摘要信息
        /// </summary>
        private void UpdateFloorSummary()
        {
            if (SelectedFloor == null)
            {
                FloorSummary = "未选择楼板";
                return;
            }

            FloorSummary = $"楼板类型: {SelectedFloor.FloorTypeName}\n" +
                          $"总层数: {TotalLayerCount}\n" +
                          $"结构层数: {StructuralLayerCount}\n" +
                          $"面层数: {FinishLayerCount}\n" +
                          $"总厚度: {TotalThicknessDisplay}";
        }
        #endregion
    }
    /// <summary>
    /// 楼板结构层服务类
    /// </summary>
    public class FloorLayerService
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;

        public FloorLayerService(ExternalCommandData commandData)
        {
            _uiDocument = commandData?.Application?.ActiveUIDocument;
            _document = _uiDocument?.Document;
        }

        /// <summary>
        /// 获取选中的楼板
        /// </summary>
        public FloorInfoModel3 GetSelectedFloor()
        {
            var selectedIds = _uiDocument?.Selection.GetElementIds();

            if (selectedIds?.Count != 1) return null;

            var element = _document?.GetElement(selectedIds.FirstOrDefault());
            if (element is Floor floor)
            {
                return new FloorInfoModel3 { Floor = floor };
            }

            return null;
        }

        /// <summary>
        /// 获取楼板的结构层功能列表（从外到内）
        /// </summary>
        public ObservableCollection<FloorLayerFunctionModel> GetLayerFunctions(Floor floor)
        {
            var layers = new ObservableCollection<FloorLayerFunctionModel>();

            if (floor?.FloorType == null) return layers;

            var cs = floor.FloorType.GetCompoundStructure();
            if (cs == null) return layers;

            // 获取层列表（按从外到内的顺序）
            var layerList = cs.GetLayers();

            for (int i = 0; i < layerList.Count; i++)
            {
                var layer = layerList[i];
                var layerModel = new FloorLayerFunctionModel
                {
                    LayerIndex = i + 1,
                    FunctionType = layer.Function,
                    Thickness = cs.GetLayerWidth(i)
                };

                // 尝试获取材料名称
                var materialId = cs.GetMaterialId(i);
                if (materialId != null && materialId != ElementId.InvalidElementId)
                {
                    var material = _document.GetElement(materialId) as Material;
                    layerModel.MaterialName = material?.Name ?? "未知材料";
                }
                else
                {
                    layerModel.MaterialName = "未指定材料";
                }

                layers.Add(layerModel);
            }

            return layers;
        }

        /// <summary>
        /// 验证选中的是否为有效楼板
        /// </summary>
        public bool IsValidFloor(FloorInfoModel3 floorInfo, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (floorInfo == null)
            {
                errorMessage = "请选择一个楼板";
                return false;
            }

            if (floorInfo.Floor?.FloorType == null)
            {
                errorMessage = "选中的楼板类型无效";
                return false;
            }

            var cs = floorInfo.Floor.FloorType.GetCompoundStructure();
            if (cs == null)
            {
                errorMessage = "选中的楼板没有复合结构";
                return false;
            }

            if (cs.LayerCount == 0)
            {
                errorMessage = "选中的楼板没有结构层";
                return false;
            }

            return true;
        }
    }
    /// <summary>
    /// 结构层功能模型
    /// </summary>
    public class FloorLayerFunctionModel : ObserverableObject
    {
        private int _layerIndex;
        private string _functionName;
        private MaterialFunctionAssignment _functionType;
        private double _thickness;
        private string _materialName;

        /// <summary>
        /// 层索引（从外部到内部）
        /// </summary>
        public int LayerIndex
        {
            get => _layerIndex;
            set { _layerIndex = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 功能名称
        /// </summary>
        public string FunctionName
        {
            get => _functionName;
            set { _functionName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 功能类型枚举
        /// </summary>
        public MaterialFunctionAssignment FunctionType
        {
            get => _functionType;
            set { _functionType = value; OnPropertyChanged(); UpdateFunctionName(); }
        }

        /// <summary>
        /// 层厚度（英尺）
        /// </summary>
        public double Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 材料名称
        /// </summary>
        public string MaterialName
        {
            get => _materialName;
            set { _materialName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 显示序号
        /// </summary>
        public string DisplayIndex => $"{LayerIndex}. ";

        /// <summary>
        /// 厚度显示（英尺和毫米）
        /// </summary>
        public string ThicknessDisplay => $"{Thickness:F4}' ({Thickness * 304.8:F1} mm)";

        /// <summary>
        /// 更新功能名称
        /// </summary>
        private void UpdateFunctionName()
        {
            FunctionName = GetFunctionDescription(FunctionType);
        }

        /// <summary>
        /// 获取功能描述
        /// </summary>
        public static string GetFunctionDescription(MaterialFunctionAssignment function)
        {
            switch (function)
            {
                case MaterialFunctionAssignment.Structure:
                    return "结构层 (Structure)";
                case MaterialFunctionAssignment.Insulation:
                    return "隔热/空气屏障层 (Thermal/Air Barrier)";
                case MaterialFunctionAssignment.Substrate:
                    return "基层 (Substrate)";
                case MaterialFunctionAssignment.Finish1:
                    return "面层1 (Finish 1)";
                case MaterialFunctionAssignment.Finish2:
                    return "面层2 (Finish 2)";
                case MaterialFunctionAssignment.Membrane:
                    return "防水层 (Membrane)";
                default:
                    return "无功能 (No Function)";
            }
        }

        /// <summary>
        /// 获取功能图标
        /// </summary>
        public string FunctionIcon => GetFunctionIcon(FunctionType);

        private string GetFunctionIcon(MaterialFunctionAssignment function)
        {
            switch (function)
            {
                case MaterialFunctionAssignment.Structure:
                    return "🏗️";
                case MaterialFunctionAssignment.Insulation:
                    return "🔥";
                case MaterialFunctionAssignment.Substrate:
                    return "📦";
                case MaterialFunctionAssignment.Finish1:
                    return "✨";
                case MaterialFunctionAssignment.Finish2:
                    return "✨";
                case MaterialFunctionAssignment.Membrane:
                    return "💧";
                default:
                    return "❓";
            }
        }
    }

    /// <summary>
    /// 楼板信息模型
    /// </summary>
    public class FloorInfoModel3 : ObserverableObject
    {
        private Floor _floor;
        private string _floorName;
        private string _floorTypeName;
        private int _layerCount;
        private double _totalThickness;

        public Floor Floor
        {
            get => _floor;
            set { _floor = value; OnPropertyChanged(); UpdateInfo(); }
        }

        public string FloorName
        {
            get => _floorName;
            set { _floorName = value; OnPropertyChanged(); }
        }

        public string FloorTypeName
        {
            get => _floorTypeName;
            set { _floorTypeName = value; OnPropertyChanged(); }
        }

        public int LayerCount
        {
            get => _layerCount;
            set { _layerCount = value; OnPropertyChanged(); }
        }

        public double TotalThickness
        {
            get => _totalThickness;
            set { _totalThickness = value; OnPropertyChanged(); }
        }

        public string DisplayName => $"{FloorName} - {FloorTypeName}";

        private void UpdateInfo()
        {
            if (_floor != null)
            {
                FloorName = $"楼板 ID:{_floor.Id.IntegerValue}";
                FloorTypeName = _floor.FloorType?.Name ?? "未知类型";

                var cs = _floor.FloorType?.GetCompoundStructure();
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

}
