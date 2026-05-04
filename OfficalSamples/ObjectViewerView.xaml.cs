using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ObjectViewerView.xaml 的交互逻辑
    /// </summary>
    public partial class ObjectViewerView : Window
    {
        public ObjectViewerView(Element selectedElement, UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ObjectViewerViewModel(selectedElement, uiApp);
        }
    }
    /// <summary>
    /// 显示模型类型枚举
    /// </summary>
    public enum DisplayKind
    {
        GeometryModel,   // 物理几何模型
        AnalyticalModel  // 分析模型
    }

    /// <summary>
    /// 视角方向枚举
    /// </summary>
    public enum ViewDirection
    {
        Top, Front, Left, Right, Bottom, Back, IsoMetric
    }

    /// <summary>
    /// 对象查看器ViewModel - 管理3D预览和参数显示
    /// </summary>
    public class ObjectViewerViewModel : ObserverableObject
    {
        #region 字段
        private readonly Element _selectedElement;
        private readonly Document _document;
        private DisplayKind _displayKind = DisplayKind.GeometryModel;
        private DetailLevel _detailLevel = DetailLevel.Fine;
        private View _selectedView;
        private ViewDirection _currentViewDirection = ViewDirection.IsoMetric;
        private DrawingVisual _previewDrawing;
        private bool _useViewSelection = true;
        #endregion

        #region 属性
        public ObservableCollection<View> AvailableViews { get; } = new ObservableCollection<View>();
        public ObservableCollection<ParameterInfo> Parameters { get; } = new ObservableCollection<ParameterInfo>();

        public ObservableCollection<DetailLevel> DetailLevels { get; } = new ObservableCollection<DetailLevel>
        {
            DetailLevel.Undefined, DetailLevel.Coarse, DetailLevel.Medium, DetailLevel.Fine
        };

        public ObservableCollection<ViewDirection> ViewDirections { get; } =
            new ObservableCollection<ViewDirection>(Enum.GetValues(typeof(ViewDirection)).Cast<ViewDirection>());

        public DisplayKind DisplayKind
        {
            get => _displayKind;
            set { _displayKind = value; OnPropertyChanged(); UpdatePreview(); }
        }

        public DetailLevel DetailLevel
        {
            get => _detailLevel;
            set
            {
                _detailLevel = value;
                _useViewSelection = false;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        public View SelectedView
        {
            get => _selectedView;
            set
            {
                _selectedView = value;
                _useViewSelection = true;
                OnPropertyChanged();
                UpdatePreview();
            }
        }

        public ViewDirection CurrentViewDirection
        {
            get => _currentViewDirection;
            set { _currentViewDirection = value; OnPropertyChanged(); UpdatePreview(); }
        }

        public DrawingVisual PreviewDrawing
        {
            get => _previewDrawing;
            set { _previewDrawing = value; OnPropertyChanged(); }
        }

        public string ElementInfo => $"{_selectedElement.Name} (ID: {_selectedElement.Id.IntegerValue})";
        #endregion

        #region 命令
        public ICommand OkCommand;
        public ICommand CancelCommand;
        #endregion

        public ObjectViewerViewModel(Element selectedElement, UIApplication uiApp)
        {
            _selectedElement = selectedElement ?? throw new ArgumentNullException(nameof(selectedElement));
            _document = uiApp.ActiveUIDocument.Document;

            InitializeViews();
            LoadParameters();
            InitializeCommands();

            _selectedView = _document.ActiveView;
            UpdatePreview();
        }

        private void InitializeCommands()
        {
            OkCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        /// <summary>
        /// 初始化可用视图列表 - 使用LINQ过滤视图模板
        /// </summary>
        private void InitializeViews()
        {
            var allViews = new FilteredElementCollector(_document)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v != null && !v.IsTemplate);

            AvailableViews.Clear();
            foreach (var view in allViews)
                AvailableViews.Add(view);
        }

        /// <summary>
        /// 加载元素参数 - 使用解构和LINQ简化
        /// </summary>
        private void LoadParameters()
        {
            Parameters.Clear();

            foreach (Parameter param in _selectedElement.Parameters)
            {
                if (param == null) continue;

                var value = GetParameterValue(param);
                if (!string.IsNullOrEmpty(value))
                {
                    Parameters.Add(new ParameterInfo
                    {
                        Name = param.Definition?.Name ?? "未知",
                        Value = value
                    });
                }
            }
        }

        /// <summary>
        /// 获取参数值 - 使用switch表达式(C# 8.0)
        /// </summary>
        /// <summary>
        /// 获取参数值 - 使用传统方式保持兼容
        /// </summary>
        private string GetParameterValue(Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    return param.AsString();
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                case StorageType.Double:
                    return param.AsValueString() ?? param.AsDouble().ToString();
                case StorageType.ElementId:
                    return GetElementName(param.AsElementId());
                default:
                    return "未知";
            }
        }

        private string GetElementName(ElementId id) =>
            id != ElementId.InvalidElementId
                ? _document.GetElement(id)?.Name ?? id.IntegerValue.ToString()
                : "无";

        /// <summary>
        /// 更新3D预览 - 根据当前设置重新生成图形
        /// </summary>
        private void UpdatePreview()
        {
            using (var dc = new DrawingVisual().RenderOpen())
            {
                var transform = GetViewTransform();

                if (_displayKind == DisplayKind.GeometryModel)
                {
                    DrawGeometryModel(dc, transform);
                }
                else
                {
                    DrawAnalyticalModel(dc, transform);
                }

                //PreviewDrawing = dc.DrawingVisual;
            }
        }

        /// <summary>
        /// 获取视图变换矩阵 - 根据视角方向计算
        /// </summary>
        private Matrix3D GetViewTransform()
        {
            // 根据ViewDirection返回相应的观察矩阵
            switch (_currentViewDirection)
            {
                case ViewDirection.Top:
                    return CreateTopViewMatrix();
                case ViewDirection.Front:
                    return CreateFrontViewMatrix();
                case ViewDirection.Left:
                    return CreateLeftViewMatrix();
                case ViewDirection.Right:
                    return CreateRightViewMatrix();
                case ViewDirection.Bottom:
                    return CreateBottomViewMatrix();
                case ViewDirection.Back:
                    return CreateBackViewMatrix();
                default:
                    return CreateIsoMetricViewMatrix();
            }
        }

        private Matrix3D CreateTopViewMatrix() => new Matrix3D(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);
        private Matrix3D CreateFrontViewMatrix() => new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
        private Matrix3D CreateLeftViewMatrix() => new Matrix3D(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
        private Matrix3D CreateRightViewMatrix() => new Matrix3D(0, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
        private Matrix3D CreateBottomViewMatrix() => new Matrix3D(1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1);
        private Matrix3D CreateBackViewMatrix() => new Matrix3D(1, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);

        private Matrix3D CreateIsoMetricViewMatrix() => new Matrix3D(
            0.707, -0.408, 0.577, 0,
            0, 0.816, 0.577, 0,
            -0.707, -0.408, 0.577, 0,
            0, 0, 0, 1);

        /// <summary>
        /// 绘制几何模型
        /// </summary>
        private void DrawGeometryModel(DrawingContext dc, Matrix3D transform)
        {
            // 获取几何数据
            var options = new Options
            {
                //DetailLevel = _detailLevel,
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true
            };

            var geometry = _selectedElement.get_Geometry(options);
            if (geometry == null) return;

            // 使用元组和迭代器简化绘制逻辑
            var pen = new Pen(Brushes.Blue, 1);

            foreach (var geomObj in geometry)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    DrawSolidEdges(dc, solid, pen, transform);
                }
                else if (geomObj is GeometryInstance instance)
                {
                    DrawInstanceEdges(dc, instance, pen, transform);
                }
            }
        }

        private void DrawSolidEdges(DrawingContext dc, Solid solid, Pen pen, Matrix3D transform)
        {
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                foreach (EdgeArray edgeLoop in face.EdgeLoops)
                {
                    foreach (Edge edge in edgeLoop)
                    {
                        DrawEdge(dc, edge, pen, transform);
                    }
                }
            }
        }

        private void DrawInstanceEdges(DrawingContext dc, GeometryInstance instance, Pen pen, Matrix3D transform)
        {
            var geometry = instance.GetInstanceGeometry();
            foreach (var geomObj in geometry)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                    DrawSolidEdges(dc, solid, pen, transform);
            }
        }

        private void DrawEdge(DrawingContext dc, Edge edge, Pen pen, Matrix3D transform)
        {
            var points = edge.Tessellate().ToList();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = TransformPoint(points[i], transform);
                var p2 = TransformPoint(points[i + 1], transform);
                dc.DrawLine(pen, p1, p2);
            }
        }

        /// <summary>
        /// 绘制分析模型
        /// </summary>
        private void DrawAnalyticalModel(DrawingContext dc, Matrix3D transform)
        {
            var analyticalModel = _selectedElement.GetAnalyticalModel();
            if (analyticalModel == null) return;

            var pen = new Pen(Brushes.Red, 2);

            // 绘制分析模型的曲线
            if (analyticalModel.GetCurve() is Curve curve)
            {
                DrawCurve(dc, curve, pen, transform);
            }

            //// 绘制分析模型的节点
            //foreach (var node in analyticalModel.GetNodes())
            //{
            //    var point = TransformPoint(node.Point, transform);
            //    dc.DrawEllipse(Brushes.Red, null, point, 3, 3);
            //}
        }

        private void DrawCurve(DrawingContext dc, Curve curve, Pen pen, Matrix3D transform)
        {
            var points = curve.Tessellate().ToList();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = TransformPoint(points[i], transform);
                var p2 = TransformPoint(points[i + 1], transform);
                dc.DrawLine(pen, p1, p2);
            }
        }

        private System.Windows.Point TransformPoint(XYZ point, Matrix3D matrix) =>
            new System.Windows.Point(
                point.X * matrix.M11 + point.Y * matrix.M12 + point.Z * matrix.M13 + matrix.OffsetX,
                point.X * matrix.M21 + point.Y * matrix.M22 + point.Z * matrix.M23 + matrix.OffsetY);

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 参数信息类
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 详图级别枚举扩展
    /// </summary>
    public enum DetailLevel
    {
        Undefined = -1,
        Coarse = 0,
        Medium = 1,
        Fine = 2
    }
    /// <summary>
    /// 枚举到布尔值的转换器 - 用于RadioButton绑定
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.Equals(Enum.Parse(value.GetType(), parameter.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (!(value is bool boolValue) || !boolValue) return Binding.DoNothing;
            //return Enum.Parse(targetType, parameter.ToString());
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// 详图级别显示名称转换器
    /// </summary>
    public class DetailLevelToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ViewDetailLevel detailLevel)
            {
                switch (detailLevel)
                {
                    case ViewDetailLevel.Undefined:
                        return "未定义";
                    case ViewDetailLevel.Coarse:
                        return "粗略";
                    case ViewDetailLevel.Medium:
                        return "中等";
                    case ViewDetailLevel.Fine:
                        return "精细";
                    default:
                        return value?.ToString();
                }
            }

            return value?.ToString();
        }

        //public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>      Binding.DoNothing;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
