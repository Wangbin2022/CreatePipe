using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// InPlaceFamilyAnalyzerView.xaml 的交互逻辑
    /// </summary>
    public partial class InPlaceFamilyAnalyzerView : Window
    {
        public InPlaceFamilyAnalyzerView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new InPlaceFamilyAnalyzerViewModel(uIApplication);
        }
    }
    /// <summary>
    /// 主视图模型 - 管理界面交互和业务逻辑
    /// </summary>
    public class InPlaceFamilyAnalyzerViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly AnalyticalModelData _model;

        #region 属性
        private InPlaceMemberProperties _selectedMemberProperties;
        public InPlaceMemberProperties SelectedMemberProperties
        {
            get => _selectedMemberProperties;
            set => SetField(ref _selectedMemberProperties, value);
        }

        private AnalyticalModelViewModel _analyticalModelViewModel;
        public AnalyticalModelViewModel AnalyticalModelViewModel
        {
            get => _analyticalModelViewModel;
            set => SetField(ref _analyticalModelViewModel, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }
        #endregion

        #region 命令
        public ICommand CloseCommand { get; }
        public ICommand RotateXCommand { get; }
        public ICommand RotateYCommand { get; }
        public ICommand RotateZCommand { get; }
        public ICommand ResetViewCommand { get; }
        #endregion

        public InPlaceFamilyAnalyzerViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _model = new AnalyticalModelData(uiApp);
            AnalyticalModelViewModel = new AnalyticalModelViewModel();

            // 初始化命令
            CloseCommand = new BaseBindingCommand(_ => CloseWindow());
            RotateXCommand = new RelayCommand<bool>(positive => AnalyticalModelViewModel?.RotateX(positive));
            RotateYCommand = new RelayCommand<bool>(positive => AnalyticalModelViewModel?.RotateY(positive));
            RotateZCommand = new RelayCommand<bool>(positive => AnalyticalModelViewModel?.RotateZ(positive));
            ResetViewCommand = new BaseBindingCommand(_ => AnalyticalModelViewModel?.ResetView());

            // 加载数据
            LoadData();
        }

        /// <summary>
        /// 加载内建族数据
        /// </summary>
        private async void LoadData()
        {
            IsLoading = true;
            StatusMessage = "正在加载数据...";

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // 获取选中的内建族实例
                    var (instance, model) = _model.GetSelectedInPlaceMember();

                    if (instance == null || model == null)
                    {
                        StatusMessage = "请选择一个具有分析模型的内建族实例";
                        return;
                    }

                    // 获取属性
                    SelectedMemberProperties = InPlaceMemberProperties.FromFamilyInstance(instance);

                    // 获取曲线数据
                    var curves = _model.GetCurvePoints(model);

                    // 更新3D视图
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        AnalyticalModelViewModel.LoadCurves(curves);
                        StatusMessage = $"已加载: {instance.Name}";
                    });
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                MessageBox.Show($"加载数据时出错:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// 分析模型可视化控件 - 使用WPF绘图引擎
    /// </summary>
    public class AnalyticalModelViewer : FrameworkElement
    {
        private readonly DrawingVisual _drawingVisual;

        public static readonly DependencyProperty CurvesProperty =
            DependencyProperty.Register(nameof(Curves), typeof(List<List<Point>>),
                typeof(AnalyticalModelViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ViewportBoundsProperty =
            DependencyProperty.Register(nameof(ViewportBounds), typeof(Rect),
                typeof(AnalyticalModelViewer),
                new FrameworkPropertyMetadata(new Rect(-100, -100, 200, 200),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public List<List<Point>> Curves
        {
            get => (List<List<Point>>)GetValue(CurvesProperty);
            set => SetValue(CurvesProperty, value);
        }

        public Rect ViewportBounds
        {
            get => (Rect)GetValue(ViewportBoundsProperty);
            set => SetValue(ViewportBoundsProperty, value);
        }

        public AnalyticalModelViewer()
        {
            _drawingVisual = new DrawingVisual();
            AddVisualChild(_drawingVisual);

            // 支持鼠标交互
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
        }

        #region 鼠标交互
        private Point _lastMousePosition;
        private bool _isPanning;
        private TransformGroup _transform = new TransformGroup();
        private TranslateTransform _panTransform = new TranslateTransform();
        private ScaleTransform _zoomTransform = new ScaleTransform();

        private void InitializeTransforms()
        {
            _transform.Children.Clear();
            _transform.Children.Add(_zoomTransform);
            _transform.Children.Add(_panTransform);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this);
                Cursor = Cursors.Hand;
                CaptureMouse();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var current = e.GetPosition(this);
                var delta = current - _lastMousePosition;
                _panTransform.X += delta.X;
                _panTransform.Y += delta.Y;
                _lastMousePosition = current;
                InvalidateVisual();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            Cursor = null;
            ReleaseMouseCapture();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scaleFactor = e.Delta > 0 ? 1.1 : 0.9;
            _zoomTransform.ScaleX *= scaleFactor;
            _zoomTransform.ScaleY *= scaleFactor;
            InvalidateVisual();
        }
        #endregion

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _drawingVisual;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            using (var context = _drawingVisual.RenderOpen())
            {
                // 绘制背景
                context.DrawRectangle(Brushes.White, null, new Rect(RenderSize));

                if (Curves == null || Curves.Count == 0)
                {
                    DrawEmptyMessage(context);
                    return;
                }

                // 保存当前变换状态
                context.PushTransform(_transform);

                // 应用视图变换
                ApplyViewTransform(context);

                // 绘制坐标轴
                DrawAxes(context);

                // 绘制曲线
                DrawCurves(context);

                context.Pop();
            }
        }

        /// <summary>
        /// 应用视图变换（将模型坐标映射到屏幕坐标）
        /// </summary>
        private void ApplyViewTransform(DrawingContext context)
        {
            if (ViewportBounds.Width <= 0 || ViewportBounds.Height <= 0)
                return;

            // 计算缩放比例
            var scaleX = RenderSize.Width / ViewportBounds.Width;
            var scaleY = RenderSize.Height / ViewportBounds.Height;
            var scale = Math.Min(scaleX, scaleY) * 0.9;

            // 平移到中心
            var translateX = RenderSize.Width / 2 - (ViewportBounds.X + ViewportBounds.Width / 2) * scale;
            var translateY = RenderSize.Height / 2 - (ViewportBounds.Y + ViewportBounds.Height / 2) * scale;

            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(scale, scale));
            transform.Children.Add(new TranslateTransform(translateX, translateY));
            transform.Children.Add(_zoomTransform);
            transform.Children.Add(_panTransform);

            context.PushTransform(transform);
        }

        /// <summary>
        /// 绘制坐标轴
        /// </summary>
        private void DrawAxes(DrawingContext context)
        {
            var axisPen = new Pen(Brushes.LightGray, 0.5);
            var center = new Point(0, 0);
            var axisLength = 100;

            // X轴（红色）
            context.DrawLine(new Pen(Brushes.Red, 1), new Point(-axisLength, 0), new Point(axisLength, 0));
            // Y轴（绿色）
            context.DrawLine(new Pen(Brushes.Green, 1), new Point(0, -axisLength), new Point(0, axisLength));
        }

        /// <summary>
        /// 绘制所有曲线
        /// </summary>
        private void DrawCurves(DrawingContext context)
        {
            var pen = new Pen(Brushes.Blue, 2);
            pen.Freeze();

            var highlightPen = new Pen(Brushes.Orange, 3);
            highlightPen.Freeze();

            for (int i = 0; i < Curves.Count; i++)
            {
                var points = Curves[i];
                if (points.Count < 2) continue;

                // 创建几何图形
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = points[0], IsClosed = true };

                for (int j = 1; j < points.Count; j++)
                {
                    figure.Segments.Add(new System.Windows.Media.LineSegment(points[j], true));
                }

                geometry.Figures.Add(figure);

                // 如果是闭合曲线，填充半透明颜色
                if (IsClosedShape(points))
                {
                    context.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 255)), pen, geometry);
                }
                else
                {
                    context.DrawGeometry(null, pen, geometry);
                }
            }
        }

        /// <summary>
        /// 判断是否为闭合形状
        /// </summary>
        private static bool IsClosedShape(List<Point> points)
        {
            return points.Count >= 3 &&
                   Math.Abs(points[0].X - points[points.Count - 1].X) < 0.01 &&
                   Math.Abs(points[0].Y - points[points.Count - 1].Y) < 0.01;
        }

        /// <summary>
        /// 绘制空状态提示
        /// </summary>
        private void DrawEmptyMessage(DrawingContext context)
        {
            var formattedText = new System.Windows.Media.FormattedText(
                "请选择一个具有分析模型的内建族实例",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                14,
                Brushes.Gray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var x = (RenderSize.Width - formattedText.Width) / 2;
            var y = (RenderSize.Height - formattedText.Height) / 2;

            context.DrawText(formattedText, new Point(x, y));
        }
    }

    /// <summary>
    /// 分析模型3D视图模型 - 处理3D变换和可视化
    /// </summary>
    public class AnalyticalModelViewModel : ObserverableObject
    {
        #region 常量定义
        private const double ROTATE_ANGLE = Math.PI / 90;
        private const float MIN_EDGE_LENGTH = 1.0f;
        private const double INITIAL_ANGLE = Math.PI / 4;
        #endregion

        #region 字段
        private Matrix3D _rotationMatrix = Matrix3D.Identity;
        private Point3D _originMin = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
        private Point3D _originMax = new Point3D(double.MinValue, double.MinValue, double.MinValue);
        private Point3D _transformedMin;
        private Point3D _transformedMax;
        private List<List<Point>> _projectedCurves; // 投影后的2D点
        private List<List<Point3D>> _originalCurves;
        #endregion

        #region 属性
        public Rect ViewportBounds { get; private set; }
        public List<List<Point>> Curves2D => _projectedCurves;

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }
        #endregion

        public AnalyticalModelViewModel()
        {
            _rotationMatrix = CreateInitialRotationMatrix();
            _originalCurves = new List<List<Point3D>>();
            _projectedCurves = new List<List<Point>>();
        }

        #region 数据加载
        /// <summary>
        /// 加载分析模型曲线数据
        /// </summary>
        public void LoadCurves(List<List<Point3D>> curves)
        {
            _originalCurves = curves;
            UpdateBounds(curves);
            UpdateProjection();
        }

        /// <summary>
        /// 更新边界范围
        /// </summary>
        private void UpdateBounds(List<List<Point3D>> curves)
        {
            _originMin = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            _originMax = new Point3D(double.MinValue, double.MinValue, double.MinValue);

            foreach (var curve in curves)
            {
                foreach (var point in curve)
                {
                    _originMin = new Point3D(
                        Math.Min(_originMin.X, point.X),
                        Math.Min(_originMin.Y, point.Y),
                        Math.Min(_originMin.Z, point.Z));

                    _originMax = new Point3D(
                        Math.Max(_originMax.X, point.X),
                        Math.Max(_originMax.Y, point.Y),
                        Math.Max(_originMax.Z, point.Z));
                }
            }
        }

        /// <summary>
        /// 创建初始旋转矩阵（45度视角）
        /// </summary>
        private static Matrix3D CreateInitialRotationMatrix()
        {
            var matrix = Matrix3D.Identity;
            matrix.Rotate(new Quaternion(new Vector3D(1, 0, 0), INITIAL_ANGLE * 180 / Math.PI));
            matrix.Rotate(new Quaternion(new Vector3D(0, 1, 0), INITIAL_ANGLE * 180 / Math.PI));
            return matrix;
        }
        #endregion

        #region 3D变换操作
        /// <summary>
        /// 绕X轴旋转
        /// </summary>
        public void RotateX(bool positive)
        {
            var angle = positive ? ROTATE_ANGLE : -ROTATE_ANGLE;
            var rotation = new Quaternion(new Vector3D(1, 0, 0), angle * 180 / Math.PI);
            _rotationMatrix.Rotate(rotation);
            UpdateProjection();
        }

        /// <summary>
        /// 绕Y轴旋转
        /// </summary>
        public void RotateY(bool positive)
        {
            var angle = positive ? ROTATE_ANGLE : -ROTATE_ANGLE;
            var rotation = new Quaternion(new Vector3D(0, 1, 0), angle * 180 / Math.PI);
            _rotationMatrix.Rotate(rotation);
            UpdateProjection();
        }

        /// <summary>
        /// 绕Z轴旋转
        /// </summary>
        public void RotateZ(bool positive)
        {
            var angle = positive ? ROTATE_ANGLE : -ROTATE_ANGLE;
            var rotation = new Quaternion(new Vector3D(0, 0, 1), angle * 180 / Math.PI);
            _rotationMatrix.Rotate(rotation);
            UpdateProjection();
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        public void ResetView()
        {
            _rotationMatrix = CreateInitialRotationMatrix();
            UpdateProjection();
        }
        #endregion

        #region 投影计算
        /// <summary>
        /// 更新投影到2D平面
        /// </summary>
        private void UpdateProjection()
        {
            if (!_originalCurves.Any())
                return;

            // 变换所有点
            var transformedPoints = new List<List<Point3D>>();

            foreach (var curve in _originalCurves)
            {
                var transformedCurve = new List<Point3D>();
                foreach (var point in curve)
                {
                    // 应用旋转
                    var rotated = _rotationMatrix.Transform(point);
                    transformedCurve.Add(rotated);
                }
                transformedPoints.Add(transformedCurve);
            }

            // 计算变换后的边界
            _transformedMin = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            _transformedMax = new Point3D(double.MinValue, double.MinValue, double.MinValue);

            foreach (var curve in transformedPoints)
            {
                foreach (var point in curve)
                {
                    _transformedMin = new Point3D(
                        Math.Min(_transformedMin.X, point.X),
                        Math.Min(_transformedMin.Y, point.Y),
                        Math.Min(_transformedMin.Z, point.Z));

                    _transformedMax = new Point3D(
                        Math.Max(_transformedMax.X, point.X),
                        Math.Max(_transformedMax.Y, point.Y),
                        Math.Max(_transformedMax.Z, point.Z));
                }
            }

            // 计算中心点
            var center = new Point3D(
                (_transformedMax.X + _transformedMin.X) / 2,
                (_transformedMax.Y + _transformedMin.Y) / 2,
                (_transformedMax.Z + _transformedMin.Z) / 2);

            // 投影到XY平面并居中
            _projectedCurves = new List<List<Point>>();

            foreach (var curve in transformedPoints)
            {
                var projectedCurve = new List<Point>();
                foreach (var point in curve)
                {
                    // 投影到XY平面（忽略Z），并居中到原点
                    var x = point.X - center.X;
                    var y = point.Y - center.Y;
                    projectedCurve.Add(new Point(x, y));
                }
                _projectedCurves.Add(projectedCurve);
            }

            // 计算视口边界
            UpdateViewportBounds();

            HasData = true;
            OnPropertyChanged(nameof(Curves2D));
        }

        /// <summary>
        /// 更新视口边界
        /// </summary>
        private void UpdateViewportBounds()
        {
            if (!_projectedCurves.Any())
            {
                ViewportBounds = new Rect(-100, -100, 200, 200);
                return;
            }

            var allPoints = _projectedCurves.SelectMany(p => p);
            var minX = allPoints.Min(p => p.X);
            var minY = allPoints.Min(p => p.Y);
            var maxX = allPoints.Max(p => p.X);
            var maxY = allPoints.Max(p => p.Y);

            var width = Math.Max(maxX - minX, MIN_EDGE_LENGTH);
            var height = Math.Max(maxY - minY, MIN_EDGE_LENGTH);

            // 添加边距
            var margin = Math.Max(width, height) * 0.1;
            ViewportBounds = new Rect(minX - margin, minY - margin, width + 2 * margin, height + 2 * margin);
        }
        #endregion
    }
    /// <summary>
    /// 内建族实例数据模型 - 封装Revit数据访问
    /// </summary>
    public class AnalyticalModelData
    {
        private readonly Document _document;

        public AnalyticalModelData(UIApplication uiApp)
        {
            _document = uiApp.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取选中的内建族实例及其分析模型
        /// </summary>
        public (FamilyInstance Instance, AnalyticalModel Model) GetSelectedInPlaceMember()
        {
            //var selection = _document.GetElementIds();
            var selection = new FilteredElementCollector(_document).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().ToList();
            if (selection.Count != 1)
                return (null, null);

            var instance = _document.GetElement(selection.First().Id) as FamilyInstance;
            if (instance == null)
                return (null, null);

            var model = instance.GetAnalyticalModel();
            return model != null ? (instance, model) : (null, null);
        }

        /// <summary>
        /// 从分析模型获取曲线点集
        /// </summary>
        public List<List<Point3D>> GetCurvePoints(AnalyticalModel model)
        {
            var curves = model.GetCurves(AnalyticalCurveType.ActiveCurves);
            var result = new List<List<Point3D>>();

            foreach (Curve curve in curves)
            {
                try
                {
                    var points = curve.Tessellate()
                        .Cast<XYZ>()
                        .Select(p => new Point3D(p.X, p.Y, p.Z))
                        .ToList();

                    if (points.Any())
                        result.Add(points);
                }
                catch
                {
                    // 忽略无法细分的曲线
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 内建族实例属性 - 用于显示
    /// </summary>
    public class InPlaceMemberProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string Type { get; set; }
        public string StructuralType { get; set; }
        public string StructuralUsage { get; set; }
        public string MaterialType { get; set; }

        public static InPlaceMemberProperties FromFamilyInstance(FamilyInstance instance)
        {
            string structuralUsage = null;
            try
            {
                structuralUsage = instance.StructuralUsage.ToString();
            }
            catch { }

            return new InPlaceMemberProperties
            {
                Id = instance.Id.IntegerValue,
                Name = instance.Name,
                Family = instance.Symbol.Family.Name,
                Type = instance.Symbol.Name,
                StructuralType = instance.StructuralType.ToString(),
                StructuralUsage = structuralUsage,
                MaterialType = instance.StructuralMaterialType.ToString()
            };
        }
    }
}
