using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Size = System.Drawing.Size;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ShaftHolePuncherView.xaml 的交互逻辑
    /// </summary>
    public partial class ShaftHolePuncherView : Window
    {
        public ShaftHolePuncherView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 竖井洞口创建ViewModel - 管理曲线绘制和洞口创建
    /// </summary>
    public class ShaftHolePuncherViewModel : ObserverableObject
    {
        private readonly Profile3 _profile;
        private readonly ExternalCommandData _commandData;

        // 绘制相关
        private ObservableCollection<System.Windows.Point> _currentPoints;
        private ObservableCollection<ObservableCollection<System.Windows.Point>> _curves;
        private System.Windows.Point _previewPoint;
        private bool _isDrawing;

        // UI状态
        private double _scale = 1.0;
        private string _direction = "Z-axis";
        private bool _isProcessing;
        private DrawingVisual _previewVisual;

        public ObservableCollection<ObservableCollection<System.Windows.Point>> Curves
        {
            get => _curves;
            set { _curves = value; OnPropertyChanged(); }
        }

        public double Scale
        {
            get => _scale;
            set { _scale = value; OnPropertyChanged(); UpdatePreview(); }
        }

        public string Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(); UpdatePreview(); }
        }

        public ObservableCollection<string> ScaleOptions { get; } = new ObservableCollection<string>
            { "0.1", "0.3", "0.5", "0.8", "1", "2", "3" };

        public ObservableCollection<string> DirectionOptions { get; } = new ObservableCollection<string>
            { "Z-axis", "Y-axis" };

        public DrawingVisual PreviewVisual
        {
            get => _previewVisual;
            set { _previewVisual = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand CreateCommand;
        public ICommand CleanCommand;
        public ICommand CancelCommand;
        public ICommand MouseDownCommand;
        public ICommand MouseMoveCommand;

        public ShaftHolePuncherViewModel(Profile3 profile, ExternalCommandData commandData)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _commandData = commandData;

            Curves = new ObservableCollection<ObservableCollection<System.Windows.Point>>();
            _currentPoints = new ObservableCollection<System.Windows.Point>();

            InitializeCommands();
            UpdatePreview();
        }

        private void InitializeCommands()
        {
            CreateCommand = new BaseBindingCommand(_ => ExecuteCreate(), _ => CanExecute);
            CleanCommand = new BaseBindingCommand(_ => ExecuteClean(), _ => CanExecute);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            MouseDownCommand = new RelayCommand<MouseEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(OnMouseMove);
        }

        private void OnMouseDown(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(null);
                _currentPoints.Add(position);
                _isDrawing = true;
                UpdatePreview();
            }
            else if (e.RightButton == MouseButtonState.Pressed && _currentPoints.Any())
            {
                // 闭合曲线
                if (_currentPoints.Count >= 3)
                {
                    Curves.Add(new ObservableCollection<System.Windows.Point>(_currentPoints));
                }
                _currentPoints.Clear();
                _isDrawing = false;
                UpdatePreview();
            }
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            if (!_isDrawing || !_currentPoints.Any()) return;

            _previewPoint = e.GetPosition(null);
            UpdatePreview();
        }

        private void ExecuteClean()
        {
            Curves.Clear();
            _currentPoints.Clear();
            _isDrawing = false;
            UpdatePreview();
        }

        private void ExecuteCreate()
        {
            IsProcessing = true;
            try
            {
                var pointsList = Curves.SelectMany(curve =>
                    _profile.Transform2DTo3D(curve.ToArray()))
                    .ToList();

                if (pointsList.Any())
                {
                    using (var transaction = new Transaction(_profile.Document, "创建竖井洞口"))
                    {
                        transaction.Start();
                        var opening = _profile.CreateOpening(pointsList);
                        transaction.Commit();
                    }
                    CloseWindow?.Invoke();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"创建洞口失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void UpdatePreview()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // 绘制宿主轮廓
                _profile.Draw2D(dc, new System.Windows.Media.Pen(Brushes.Blue, 2));

                // 绘制已完成的曲线
                foreach (var curve in Curves)
                {
                    DrawCurve(dc, curve, Brushes.Red);
                }

                // 绘制当前正在绘制的曲线
                if (_currentPoints.Any())
                {
                    var points = _currentPoints.ToList();
                    if (_previewPoint != null)
                        points.Add(_previewPoint);
                    DrawCurve(dc, points, Brushes.Red, true);
                }
            }
            PreviewVisual = visual;
        }

        private static void DrawCurve(DrawingContext dc, System.Collections.Generic.IList<System.Windows.Point> points, Brush color, bool isPreview = false)
        {
            if (points.Count < 2) return;

            var pen = new System.Windows.Media.Pen(color, isPreview ? 1 : 2);
            for (int i = 0; i < points.Count - 1; i++)
            {
                dc.DrawLine(pen, points[i], points[i + 1]);
            }
        }

        public Action CloseWindow { get; set; }
    }

    /// <summary>
    /// 轮廓基类 - 处理几何转换和洞口创建
    /// 使用C# 7.3语法：表达式体成员、元组、LINQ
    /// </summary>
    public abstract class Profile3
    {
        protected readonly ExternalCommandData _commandData;
        protected readonly Document _document;
        protected readonly Autodesk.Revit.Creation.Application _appCreator;

        protected List<List<XYZ>> _points;
        protected Matrix4 _to2DMatrix;
        protected Matrix4 _moveToCenterMatrix;
        protected Matrix4 _scaleMatrix;
        protected Size _pictureBoxSize;

        public Document Document => _document;

        protected Profile3(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            _appCreator = commandData.Application.Application.Create;
        }

        /// <summary>
        /// 创建洞口 - 抽象方法
        /// </summary>
        public abstract Opening CreateOpening(List<Vector4> points);

        /// <summary>
        /// 获取面的边界框
        /// </summary>
        public virtual (float minX, float minY, float maxX, float maxY) GetFaceBounds()
        {
            var matrix = _to2DMatrix.Inverse();
            var bounds = (minX: float.MaxValue, minY: float.MaxValue,
                          maxX: float.MinValue, maxY: float.MinValue);
            bool first = true;

            //foreach (var pointList in _points)
            //{
            //    foreach (var point in pointList)
            //    {
            //        var v = matrix.Transform(new Vector4(point));
            //        if (first)
            //        {
            //            bounds = ((float)v.X, (float)v.Y, (float)v.X, (float)v.Y);
            //            first = false;
            //        }
            //        else
            //        {
            //            bounds.minX = Math.Min(bounds.minX, (float)v.X);
            //            bounds.maxX = Math.Max(bounds.maxX, (float)v.X);
            //            bounds.minY = Math.Min(bounds.minY, (float)v.Y);
            //            bounds.maxY = Math.Max(bounds.maxY, (float)v.Y);
            //        }
            //    }
            //}
            return bounds;
        }

        /// <summary>
        /// 计算缩放矩阵
        /// </summary>
        public Matrix4 ComputeScaleMatrix(Size pictureBoxSize)
        {
            _pictureBoxSize = pictureBoxSize;
            var (minX, minY, maxX, maxY) = GetFaceBounds();

            float width = (float)pictureBoxSize.Width / (maxX - minX);
            float height = (float)pictureBoxSize.Height / (maxY - minY);
            float factor = Math.Min(width, height) * 0.85f;

            _scaleMatrix = new Matrix4(factor);
            return _scaleMatrix;
        }

        /// <summary>
        /// 计算3D到2D的变换矩阵
        /// </summary>
        public Matrix4 Compute3DTo2DMatrix()
        {
            var result = Matrix4.Multiply(_to2DMatrix.Inverse(), _moveToCenterMatrix.Inverse());
            return Matrix4.Multiply(result, _scaleMatrix);
        }

        /// <summary>
        /// 将2D屏幕坐标转换为3D世界坐标
        /// </summary>
        public List<Vector4> Transform2DTo3D(System.Windows.Point[] screenPoints)
        {
            var transformMatrix = Matrix4.Multiply(_scaleMatrix.Inverse(), _moveToCenterMatrix);
            transformMatrix = Matrix4.Multiply(transformMatrix, _to2DMatrix);

            // 转换屏幕点到画布中心坐标系
            var centerMatrix = new Matrix();
            centerMatrix.Translate(_pictureBoxSize.Width / 2, _pictureBoxSize.Height / 2);
            centerMatrix.Invert();

            var result = new List<Vector4>();
            //foreach (var pt in screenPoints)
            //{
            //    var points = new[] { new System.Drawing.PointF((float)pt.X, (float)pt.Y) };
            //    centerMatrix.TransformPoints(points);
            //    var v = new Vector4(points[0].X, points[0].Y, 0);
            //    v = transformMatrix.Transform(v);
            //    result.Add(v);
            //}
            return result;
        }

        /// <summary>
        /// 绘制2D轮廓
        /// </summary>
        public virtual void Draw2D(DrawingContext dc, Pen pen)
        {
            //var transform = Compute3DTo2DMatrix();

            //foreach (var pointList in _points)
            //{
            //    for (int i = 0; i < pointList.Count - 1; i++)
            //    {
            //        var p1 = transform.Transform(new Vector4(pointList[i]));
            //        var p2 = transform.Transform(new Vector4(pointList[i + 1]));
            //        dc.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
            //    }
            //}
        }

        /// <summary>
        /// 获取元素的所有面
        /// </summary>
        protected List<List<Edge>> GetFaces(Element element)
        {
            var options = new Options
            {
                DetailLevel = ViewDetailLevel.Medium,
                ComputeReferences = true
            };

            var geometry = element.get_Geometry(options);
            var faces = new List<List<Edge>>();

            foreach (var geomObj in geometry)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Autodesk.Revit.DB.Face face in solid.Faces)
                    {
                        foreach (EdgeArray edgeLoop in face.EdgeLoops)
                        {
                            faces.Add(edgeLoop.Cast<Edge>().ToList());
                        }
                    }
                }
            }
            return faces;
        }
    }

    /// <summary>
    /// 无宿主轮廓类 - 用于创建自由空间竖井洞口
    /// 使用C# 7.3语法：表达式体成员、LINQ、字符串插值、nameof
    /// </summary>
    public class ProfileNull : Profile3
    {
        private Level _bottomLevel;   // 底部标高 (Level 1)
        private Level _topLevel;      // 顶部标高 (Level 2)
        private float _scale = 1.0f;  // 缩放比例

        // 坐标系参考点（原点在UI中的位置）
        private const int OriginX = 20;
        private const int OriginY = 280;

        // 刻度线间距
        private const int TickSpacing = 100;
        private const int AxisMaxX = 400;
        private const int AxisMaxY = 50;

        public float Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public ProfileNull(ExternalCommandData commandData) : base(commandData)
        {
            LoadLevels();
            _to2DMatrix = new Matrix4();
            _moveToCenterMatrix = new Matrix4();
        }

        /// <summary>
        /// 加载Level 1和Level 2标高
        /// </summary>
        private void LoadLevels()
        {
            _bottomLevel = FindLevelByName("Level 1");
            _topLevel = FindLevelByName("Level 2");
        }

        /// <summary>
        /// 根据名称查找标高 - 使用LINQ
        /// </summary>
        private Level FindLevelByName(string levelName) =>
            new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == levelName);

        ///// <summary>
        ///// 计算缩放矩阵 - 使用用户指定的缩放比例
        ///// </summary>
        //public override Matrix4 ComputeScaleMatrix(Size pictureBoxSize)
        //{
        //    _scaleMatrix = new Matrix4(Scale);
        //    return _scaleMatrix;
        //}

        ///// <summary>
        ///// 计算3D到2D变换矩阵 - 不需要变换
        ///// </summary>
        //public override Matrix4 Compute3DTo2DMatrix()
        //{
        //    _transformMatrix = new Matrix4();
        //    return _transformMatrix;
        //}

        ///// <summary>
        ///// 绘制坐标系 - 使用辅助方法简化
        ///// </summary>
        //public override void Draw2D(Graphics graphics, Pen pen, Matrix4 matrix)
        //{
        //    // 重置变换
        //    graphics.Transform = new Matrix(1, 0, 0, 1, 0, 0);

        //    DrawXAxis(graphics, pen);
        //    DrawYAxis(graphics, pen);
        //    DrawTickMarks(graphics, pen);
        //    DrawDimensionLabels(graphics);
        //}

        ///// <summary>
        ///// 绘制X轴及箭头
        ///// </summary>
        //private static void DrawXAxis(Graphics graphics, Pen pen)
        //{
        //    graphics.DrawLine(pen, OriginX, OriginY, AxisMaxX, OriginY);
        //    graphics.DrawPie(pen, AxisMaxX, OriginY - 15, 30, 30, 165, 30);
        //}

        ///// <summary>
        ///// 绘制Y轴及箭头
        ///// </summary>
        //private static void DrawYAxis(Graphics graphics, Pen pen)
        //{
        //    graphics.DrawLine(pen, OriginX, OriginY, OriginX, AxisMaxY);
        //    graphics.DrawPie(pen, OriginX - 15, AxisMaxY - 15, 30, 30, 75, 30);
        //}

        ///// <summary>
        ///// 绘制刻度线 - 使用循环简化
        ///// </summary>
        //private static void DrawTickMarks(Graphics graphics, Pen pen)
        //{
        //    // X轴刻度 (100, 200, 300)
        //    for (int x = 100; x <= 300; x += 100)
        //    {
        //        int pos = OriginX + x;
        //        graphics.DrawLine(pen, pos, OriginY - 5, pos, OriginY + 5);
        //    }

        //    // Y轴刻度 (100, 200)
        //    for (int y = 100; y <= 200; y += 100)
        //    {
        //        int pos = OriginY - y;
        //        graphics.DrawLine(pen, OriginX - 5, pos, OriginX + 5, pos);
        //    }
        //}

        ///// <summary>
        ///// 绘制尺寸标签
        ///// </summary>
        //private static void DrawDimensionLabels(Graphics graphics)
        //{
        //    using var font = new Font("Verdana", 10, FontStyle.Regular);
        //    var brush = Brushes.Blue;

        //    // X轴标签
        //    graphics.DrawString("100'", font, brush, OriginX + 102, OriginY - 14);
        //    graphics.DrawString("200'", font, brush, OriginX + 202, OriginY - 14);
        //    graphics.DrawString("300'", font, brush, OriginX + 302, OriginY - 14);

        //    // Y轴标签
        //    graphics.DrawString("100'", font, brush, OriginX - 18, OriginY - 99);
        //    graphics.DrawString("200'", font, brush, OriginX - 18, OriginY - 199);

        //    // 原点标签
        //    graphics.DrawString("(0,0)", font, brush, OriginX + 2, OriginY + 4);
        //}

        ///// <summary>
        ///// 将2D屏幕坐标转换为3D坐标 - UI坐标系到Revit坐标系
        ///// </summary>
        //public override List<Vector4> Transform2DTo3D(Point[] screenPoints)
        //{
        //    var result = new List<Vector4>();

        //    foreach (var point in screenPoints)
        //    {
        //        // UI坐标转Revit坐标：
        //        // X: 减去原点偏移量
        //        // Y: 反转方向 (UI中Y向下为正，Revit中Y向上为正)
        //        var revitX = point.X - OriginX;
        //        var revitY = -(point.Y - OriginY);

        //        var v = new Vector4(revitX, revitY, 0);
        //        v = _scaleMatrix.Transform(v);
        //        result.Add(v);
        //    }

        //    return result;
        //}

        /// <summary>
        /// 创建竖井洞口 - 在Level 1和Level 2之间
        /// </summary>
        public override Opening CreateOpening(List<Vector4> points)
        {
            //    if (_bottomLevel == null || _topLevel == null)
            //    {
            //        throw new Exception("未找到Level 1或Level 2标高，无法创建竖井洞口。");
            //    }
            //    var curveArray = _appCreator.NewCurveArray();
            //    // 创建线段
            //    for (int i = 0; i < points.Count - 1; i++)
            //    {
            //        var curve = CreateLineFromPoints(points[i], points[i + 1]);
            //        curveArray.Append(curve);
            //    }
            //    // 闭合曲线
            //    var closeCurve = CreateLineFromPoints(points[^1], points[0]);
            //    curveArray.Append(closeCurve);
            //    return _docCreator.NewOpening(_bottomLevel, _topLevel, curveArray);
            return null;
        }

        ///// <summary>
        ///// 从两个Vector4点创建Line - 使用元组解构
        ///// </summary>
        //private static Line CreateLineFromPoints(Vector4 start, Vector4 end)
        //{
        //    var p1 = new XYZ(start.X, start.Y, start.Z);
        //    var p2 = new XYZ(end.X, end.Y, end.Z);
        //    return Line.CreateBound(p1, p2);
        //}
    }
}
