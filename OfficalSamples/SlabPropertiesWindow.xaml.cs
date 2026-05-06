using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// SlabPropertiesWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SlabPropertiesWindow : Window
    {
        private readonly SlabPropertiesViewModel _viewModel;
        public SlabPropertiesWindow(SlabPropertiesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Closed += (s, e) => _viewModel.CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// 楼板属性视图模型
    /// </summary>
    public class SlabPropertiesViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly SlabPropertyService _service;
        private SlabPropertiesModel _slabProperties;
        private LayerPropertyModel _selectedLayer;
        private string _statusMessage;
        private bool _isLoading;
        #endregion

        #region 绑定属性
        /// <summary>
        /// 楼板属性数据
        /// </summary>
        public SlabPropertiesModel SlabProperties
        {
            get => _slabProperties;
            set { _slabProperties = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的结构层
        /// </summary>
        public LayerPropertyModel SelectedLayer
        {
            get => _selectedLayer;
            set { _selectedLayer = value; OnPropertyChanged(); }
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
        /// 基本信息摘要
        /// </summary>
        public string BasicInfoSummary => SlabProperties == null ? "无数据" :
            $"标高: {SlabProperties.LevelName} | 类型: {SlabProperties.TypeName} | 跨度: {SlabProperties.SpanDirectionDisplay}";

        /// <summary>
        /// 统计信息
        /// </summary>
        public string StatisticsInfo => SlabProperties == null ? "无数据" :
            $"总层数: {SlabProperties.LayerCount} | 总厚度: {SlabProperties.TotalThicknessDisplay}";
        #endregion

        #region 命令
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ExportCommand { get; }
        #endregion

        public SlabPropertiesViewModel(SlabPropertyService service)
        {
            _service = service;

            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            CloseCommand = new BaseBindingCommand(ExecuteClose);
            ExportCommand = new BaseBindingCommand(ExecuteExport, _ => SlabProperties != null);

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

        /// <summary>
        /// 导出数据
        /// </summary>
        private void ExecuteExport(Object obj)
        {
            if (SlabProperties == null) return;

            // 可在此实现导出到CSV或文本文件
            StatusMessage = "导出功能开发中...";
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
                var floor = _service.GetSelectedFloor(out string error);

                if (floor == null)
                {
                    StatusMessage = error;
                    SlabProperties = null;
                    return;
                }

                if (!_service.IsValidFloor(floor, out error))
                {
                    StatusMessage = error;
                    SlabProperties = null;
                    return;
                }

                SlabProperties = _service.GetSlabProperties(floor);
                StatusMessage = $"已加载楼板: {SlabProperties.TypeName}";

                OnPropertyChanged(nameof(BasicInfoSummary));
                OnPropertyChanged(nameof(StatisticsInfo));
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                SlabProperties = null;
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
                var floor = _service.GetSelectedFloor(out string error);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (floor == null)
                    {
                        StatusMessage = error;
                        SlabProperties = null;
                        return;
                    }

                    if (!_service.IsValidFloor(floor, out error))
                    {
                        StatusMessage = error;
                        SlabProperties = null;
                        return;
                    }

                    SlabProperties = _service.GetSlabProperties(floor);
                    StatusMessage = $"已加载楼板: {SlabProperties.TypeName}";

                    OnPropertyChanged(nameof(BasicInfoSummary));
                    OnPropertyChanged(nameof(StatisticsInfo));
                });
            });

            IsLoading = false;
        }
        #endregion
    }
    /// <summary>
    /// 楼板属性服务类
    /// </summary>
    public class SlabPropertyService
    {
        #region 常量定义
        private const double FeetToMeter = 0.3048;
        private const double MeterToMM = 1000;
        private const double FeetToMM = 1 / 0.3048;
        private const double YoungModulusConversion = 304800.0; // 转换为 MPa
        private const double AngleTolerance = 1E-12;
        #endregion

        private readonly UIDocument _uiDocument;
        private readonly Document _document;

        public SlabPropertyService(ExternalCommandData commandData)
        {
            _uiDocument = commandData?.Application?.ActiveUIDocument;
            _document = _uiDocument?.Document;
        }

        /// <summary>
        /// 获取选中的楼板
        /// </summary>
        public Floor GetSelectedFloor(out string errorMessage)
        {
            errorMessage = string.Empty;

            var selectedIds = _uiDocument?.Selection.GetElementIds();

            if (selectedIds == null || selectedIds.Count == 0)
            {
                errorMessage = "请选择一个楼板";
                return null;
            }

            if (selectedIds.Count != 1)
            {
                errorMessage = "请只选择一个楼板";
                return null;
            }

            var element = _document?.GetElement(selectedIds.FirstOrDefault());
            if (!(element is Floor floor))
            {
                errorMessage = "选中的元素不是楼板";
                return null;
            }

            return floor;
        }

        /// <summary>
        /// 获取楼板完整属性
        /// </summary>
        public SlabPropertiesModel GetSlabProperties(Floor floor)
        {
            if (floor == null) return null;

            var model = new SlabPropertiesModel
            {
                Layers = new ObservableCollection<LayerPropertyModel>()
            };

            // 获取标高
            model.LevelName = GetLevelName(floor);

            // 获取类型名称
            model.TypeName = floor.FloorType?.Name ?? "未知类型";

            // 获取跨度方向
            model.SpanDirectionRadians = GetSpanDirection(floor);

            // 获取结构层信息
            var layers = GetLayerProperties(floor);

            double totalThickness = 0;
            foreach (var layer in layers)
            {
                model.Layers.Add(layer);
                totalThickness += layer.ThicknessMM;
            }

            model.LayerCount = model.Layers.Count;
            model.TotalThicknessMM = totalThickness;

            return model;
        }

        /// <summary>
        /// 获取标高名称
        /// </summary>
        private string GetLevelName(Floor floor)
        {
            if (floor.LevelId == null) return "未指定";

            var level = _document.GetElement(floor.LevelId) as Level;
            return level?.Name ?? "未知标高";
        }

        /// <summary>
        /// 获取跨度方向（弧度）
        /// </summary>
        private double GetSpanDirection(Floor floor)
        {
            var param = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_SPAN_DIRECTION);
            if (param == null || !param.HasValue) return 0;

            double radians = param.AsDouble();

            // 处理极小值
            return Math.Abs(radians) < AngleTolerance ? 0 : radians;
        }

        /// <summary>
        /// 获取结构层属性列表
        /// </summary>
        private ObservableCollection<LayerPropertyModel> GetLayerProperties(Floor floor)
        {
            var layers = new ObservableCollection<LayerPropertyModel>();

            var cs = floor.FloorType?.GetCompoundStructure();
            if (cs == null) return layers;

            var layerList = cs.GetLayers();

            for (int i = 0; i < layerList.Count; i++)
            {
                var layer = layerList[i];
                var layerModel = new LayerPropertyModel
                {
                    LayerIndex = i + 1,
                    ThicknessFeet = cs.GetLayerWidth(i)
                };

                // 转换厚度为毫米
                layerModel.ThicknessMM = layerModel.ThicknessFeet * FeetToMM;

                // 获取材料信息
                var materialId = cs.GetMaterialId(i);
                if (materialId != null && materialId != ElementId.InvalidElementId)
                {
                    var material = _document.GetElement(materialId) as Material;
                    layerModel.MaterialName = material?.Name ?? "未指定材料";

                    // 获取弹性模量
                    GetYoungModulus(material, layerModel);
                }
                else
                {
                    layerModel.MaterialName = "无材料";
                }

                layers.Add(layerModel);
            }

            return layers;
        }

        /// <summary>
        /// 获取材料的弹性模量
        /// </summary>
        private void GetYoungModulus(Material material, LayerPropertyModel layerModel)
        {
            if (material == null) return;

            // 获取 X 方向弹性模量
            var paramX = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD1);
            if (paramX != null && paramX.HasValue)
            {
                layerModel.YoungModulusX = paramX.AsDouble() / YoungModulusConversion;
            }

            // 获取 Y 方向弹性模量
            var paramY = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD2);
            if (paramY != null && paramY.HasValue)
            {
                layerModel.YoungModulusY = paramY.AsDouble() / YoungModulusConversion;
            }

            // 获取 Z 方向弹性模量
            var paramZ = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD3);
            if (paramZ != null && paramZ.HasValue)
            {
                layerModel.YoungModulusZ = paramZ.AsDouble() / YoungModulusConversion;
            }
        }

        /// <summary>
        /// 验证楼板是否有效
        /// </summary>
        public bool IsValidFloor(Floor floor, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (floor == null)
            {
                errorMessage = "楼板对象为空";
                return false;
            }

            if (floor.FloorType == null)
            {
                errorMessage = "楼板类型无效";
                return false;
            }

            var cs = floor.FloorType.GetCompoundStructure();
            if (cs == null)
            {
                errorMessage = "楼板没有复合结构";
                return false;
            }

            return true;
        }
    }
    /// <summary>
    /// 结构层属性模型
    /// </summary>
    public class LayerPropertyModel : ObserverableObject
    {
        private int _layerIndex;
        private string _materialName;
        private double _thicknessFeet;
        private double _thicknessMM;
        private double _youngModulusX;
        private double _youngModulusY;
        private double _youngModulusZ;
        private string _youngModulusXDisplay;
        private string _youngModulusYDisplay;
        private string _youngModulusZDisplay;

        public int LayerIndex
        {
            get => _layerIndex;
            set { _layerIndex = value; OnPropertyChanged(); }
        }

        public string MaterialName
        {
            get => _materialName;
            set { _materialName = value; OnPropertyChanged(); }
        }

        public double ThicknessFeet
        {
            get => _thicknessFeet;
            set { _thicknessFeet = value; OnPropertyChanged(); UpdateThicknessDisplay(); }
        }

        public double ThicknessMM
        {
            get => _thicknessMM;
            set { _thicknessMM = value; OnPropertyChanged(); UpdateThicknessDisplay(); }
        }

        public double YoungModulusX
        {
            get => _youngModulusX;
            set { _youngModulusX = value; OnPropertyChanged(); UpdateYoungModulusDisplay(); }
        }

        public double YoungModulusY
        {
            get => _youngModulusY;
            set { _youngModulusY = value; OnPropertyChanged(); UpdateYoungModulusDisplay(); }
        }

        public double YoungModulusZ
        {
            get => _youngModulusZ;
            set { _youngModulusZ = value; OnPropertyChanged(); UpdateYoungModulusDisplay(); }
        }

        public string YoungModulusXDisplay
        {
            get => _youngModulusXDisplay;
            set { _youngModulusXDisplay = value; OnPropertyChanged(); }
        }

        public string YoungModulusYDisplay
        {
            get => _youngModulusYDisplay;
            set { _youngModulusYDisplay = value; OnPropertyChanged(); }
        }

        public string YoungModulusZDisplay
        {
            get => _youngModulusZDisplay;
            set { _youngModulusZDisplay = value; OnPropertyChanged(); }
        }

        public string ThicknessDisplay => $"{ThicknessMM:F1} mm ({ThicknessFeet:F4}')";
        public string LayerTitle => $"第 {LayerIndex} 层";

        private void UpdateThicknessDisplay()
        {
            OnPropertyChanged(nameof(ThicknessDisplay));
        }

        private void UpdateYoungModulusDisplay()
        {
            YoungModulusXDisplay = YoungModulusX > 0 ? $"{YoungModulusX:F2} MPa" : "未定义";
            YoungModulusYDisplay = YoungModulusY > 0 ? $"{YoungModulusY:F2} MPa" : "未定义";
            YoungModulusZDisplay = YoungModulusZ > 0 ? $"{YoungModulusZ:F2} MPa" : "未定义";
        }
    }

    /// <summary>
    /// 楼板完整属性模型
    /// </summary>
    public class SlabPropertiesModel : ObserverableObject
    {
        private string _levelName;
        private string _typeName;
        private double _spanDirectionRadians;
        private double _spanDirectionDegrees;
        private int _layerCount;
        private double _totalThicknessMM;
        private ObservableCollection<LayerPropertyModel> _layers;

        public string LevelName
        {
            get => _levelName;
            set { _levelName = value; OnPropertyChanged(); }
        }

        public string TypeName
        {
            get => _typeName;
            set { _typeName = value; OnPropertyChanged(); }
        }

        public double SpanDirectionRadians
        {
            get => _spanDirectionRadians;
            set { _spanDirectionRadians = value; OnPropertyChanged(); UpdateSpanDirection(); }
        }

        public double SpanDirectionDegrees
        {
            get => _spanDirectionDegrees;
            set { _spanDirectionDegrees = value; OnPropertyChanged(); }
        }

        public string SpanDirectionDisplay => $"{SpanDirectionDegrees:F2}° ({SpanDirectionRadians:F4} rad)";

        public int LayerCount
        {
            get => _layerCount;
            set { _layerCount = value; OnPropertyChanged(); }
        }

        public double TotalThicknessMM
        {
            get => _totalThicknessMM;
            set { _totalThicknessMM = value; OnPropertyChanged(); }
        }

        public string TotalThicknessDisplay => $"{TotalThicknessMM:F1} mm";

        public ObservableCollection<LayerPropertyModel> Layers
        {
            get => _layers;
            set { _layers = value; OnPropertyChanged(); }
        }

        private void UpdateSpanDirection()
        {
            SpanDirectionDegrees = SpanDirectionRadians * (180.0 / Math.PI);
            OnPropertyChanged(nameof(SpanDirectionDisplay));
        }
    }

}
