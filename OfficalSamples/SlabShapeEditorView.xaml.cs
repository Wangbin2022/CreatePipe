using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// SlabShapeEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class SlabShapeEditorView : Window
    {
        private SlabShapeEditorViewModel _viewModel;
        public SlabShapeEditorView(SlabShapeEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            // 绑定画布的绘图源
            //Loaded += SlabShapeEditorView_Loaded;
        }
        //private void SlabShapeEditorView_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // 将ViewModel的绘图绑定到Image控件
        //    var drawingGroup = _viewModel.SlabDrawing;
        //    if (drawingGroup != null)
        //    {
        //        SlabImage.Source = new DrawingImage(drawingGroup);
        //    }
        //}
        ///// <summary>
        ///// 画布鼠标点击事件
        ///// </summary>
        //private void Canvas_MouseClick(object sender, MouseButtonEventArgs e)
        //{
        //    var position = e.GetPosition(DrawingCanvas);

        //    // 处理右键旋转
        //    if (e.RightButton == MouseButtonState.Pressed)
        //    {
        //        // 右键旋转逻辑通过ViewModel的鼠标移动处理
        //        return;
        //    }

        //    // 传递点击位置到ViewModel
        //    if (_viewModel.CanvasClickCommand?.CanExecute(position) == true)
        //    {
        //        _viewModel.CanvasClickCommand.Execute(position);
        //        RefreshDisplay();
        //    }
        //}
        ///// <summary>
        ///// 画布鼠标移动事件
        ///// </summary>
        //private void Canvas_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (_viewModel.CanvasMouseMoveCommand?.CanExecute(e) == true)
        //    {
        //        _viewModel.CanvasMouseMoveCommand.Execute(e);
        //        RefreshDisplay();
        //    }
        //}
        ///// <summary>
        ///// 刷新显示
        ///// </summary>
        //private void RefreshDisplay()
        //{
        //    var drawingGroup = _viewModel.SlabDrawing;
        //    if (drawingGroup != null)
        //    {
        //        SlabImage.Source = new  DrawingImage(drawingGroup);
        //    }
        //}
        /// <summary>
        /// 确定按钮点击
        /// </summary>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// 楼板形状编辑器视图模型 - 实现MVVM模式
    /// </summary>
    public class SlabShapeEditorViewModel : ObserverableObject
    {
        private readonly SlabProfileService _profileService;
        private SlabGeometryModel _currentGeometry;

        // 编辑模式枚举
        public enum EditModeType
        {
            None,
            AddVertex,
            AddCrease,
            Move
        }

        private EditModeType _currentMode = EditModeType.None;
        private Point? _tempStartPoint; // 用于折线绘制的临时起点

        public SlabShapeEditorViewModel(SlabProfileService profileService)
        {
            _profileService = profileService;
            _currentGeometry = profileService.GetSlabGeometry();

            // 初始化命令
            AddVertexCommand = new BaseBindingCommand(ExecuteAddVertexMode);
            AddCreaseCommand = new BaseBindingCommand(ExecuteAddCreaseMode);
            MoveCommand = new BaseBindingCommand(ExecuteMoveMode);
            ResetCommand = new BaseBindingCommand(ExecuteReset);
            UpdateCommand = new BaseBindingCommand(ExecuteUpdate);
            CanvasClickCommand = new RelayCommand<Point>(ExecuteCanvasClick);
            CanvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(ExecuteCanvasMouseMove);
        }

        // 属性绑定
        private DrawingGroup _slabDrawing;
        public DrawingGroup SlabDrawing
        {
            get => _slabDrawing;
            set
            {
                _slabDrawing = value;
                OnPropertyChanged();
            }
        }

        private double _distanceValue = 1.0;
        public double DistanceValue
        {
            get => _distanceValue;
            set
            {
                _distanceValue = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // 命令定义
        public ICommand AddVertexCommand;
        public ICommand AddCreaseCommand;
        public ICommand MoveCommand;
        public ICommand ResetCommand;
        public ICommand UpdateCommand;
        public ICommand CanvasClickCommand;
        public ICommand CanvasMouseMoveCommand;

        /// <summary>
        /// 执行添加顶点模式
        /// </summary>
        private void ExecuteAddVertexMode(Object obj)
        {
            _currentMode = EditModeType.AddVertex;
            StatusMessage = "点击画布添加顶点";
        }

        /// <summary>
        /// 执行添加折线模式
        /// </summary>
        private void ExecuteAddCreaseMode(Object obj)
        {
            _currentMode = EditModeType.AddCrease;
            _tempStartPoint = null;
            StatusMessage = "点击画布选择折线起点";
        }

        /// <summary>
        /// 执行移动模式
        /// </summary>
        private void ExecuteMoveMode(Object obj)
        {
            _currentMode = EditModeType.Move;
            StatusMessage = "拖动鼠标旋转视图";
        }

        /// <summary>
        /// 执行重置操作
        /// </summary>
        private async void ExecuteReset(Object obj)
        {
            try
            {
                _profileService.ResetSlabShape();
                await RefreshDisplay();
                StatusMessage = "形状已重置";
            }
            catch (Exception ex)
            {
                StatusMessage = $"重置失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 执行更新操作
        /// </summary>
        private async void ExecuteUpdate(Object obj)
        {
            try
            {
                _profileService.UpdateSlabShape();
                await RefreshDisplay();
                StatusMessage = "已更新";
            }
            catch (Exception ex)
            {
                StatusMessage = $"更新失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 画布点击处理
        /// </summary>
        private async void ExecuteCanvasClick(Point clickPoint)
        {
            switch (_currentMode)
            {
                case EditModeType.AddVertex:
                    await AddVertexAtPoint(clickPoint);
                    break;

                case EditModeType.AddCrease:
                    await HandleCreaseDrawing(clickPoint);
                    break;

                case EditModeType.Move:
                    // 移动模式下点击不做操作
                    break;
            }
        }

        /// <summary>
        /// 处理折线绘制
        /// </summary>
        private async System.Threading.Tasks.Task HandleCreaseDrawing(Point clickPoint)
        {
            if (_tempStartPoint == null)
            {
                // 第一个点：检查是否可以创建顶点
                if (_profileService.CanCreateVertex(clickPoint))
                {
                    _tempStartPoint = clickPoint;
                    StatusMessage = "选择折线终点";
                }
                else
                {
                    StatusMessage = "无效的起点位置";
                }
            }
            else
            {
                // 第二个点：创建折线
                if (_profileService.AddCrease(_tempStartPoint.Value, clickPoint))
                {
                    await RefreshDisplay();
                    StatusMessage = "折线已添加";
                }
                else
                {
                    StatusMessage = "添加折线失败";
                }
                _tempStartPoint = null;
                _currentMode = EditModeType.None;
            }
        }

        /// <summary>
        /// 在指定点添加顶点
        /// </summary>
        private async System.Threading.Tasks.Task AddVertexAtPoint(Point point)
        {
            if (_profileService.CanCreateVertex(point))
            {
                if (_profileService.AddVertex(point))
                {
                    await RefreshDisplay();
                    StatusMessage = "顶点已添加";
                }
                else
                {
                    StatusMessage = "添加顶点失败";
                }
            }
            else
            {
                StatusMessage = "不能在当前位置添加顶点";
            }
        }

        /// <summary>
        /// 画布鼠标移动处理（用于旋转）
        /// </summary>
        private Point _lastMousePosition;

        private async void ExecuteCanvasMouseMove(MouseEventArgs e)
        {
            if (_currentMode == EditModeType.Move && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                if (_lastMousePosition != default)
                {
                    var deltaX = (currentPosition.X - _lastMousePosition.X) * 0.01;
                    var deltaY = (currentPosition.Y - _lastMousePosition.Y) * 0.01;

                    _profileService.Rotate(deltaX, deltaY);
                    await RefreshDisplay();
                }
                _lastMousePosition = currentPosition;
            }
            else
            {
                _lastMousePosition = default;
            }
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        private async System.Threading.Tasks.Task RefreshDisplay()
        {
            _currentGeometry = _profileService.GetSlabGeometry();
            RenderSlabGeometry();
        }

        /// <summary>
        /// 渲染楼板几何图形
        /// </summary>
        private void RenderSlabGeometry()
        {
            var drawingGroup = new DrawingGroup();

            using (var context = drawingGroup.Open())
            {
                // 绘制背景
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 354, 280));

                // 绘制楼板轮廓
                var pen = new Pen(Brushes.Black, 2);
                for (int i = 0; i < _currentGeometry.BoundaryPoints.Count - 1; i++)
                {
                    var p1 = _currentGeometry.BoundaryPoints[i];
                    var p2 = _currentGeometry.BoundaryPoints[i + 1];
                    context.DrawLine(pen, p1, p2);
                }

                // 绘制顶点
                foreach (var vertex in _currentGeometry.Vertices)
                {
                    context.DrawEllipse(Brushes.Red, null, vertex.ScreenPosition, 4, 4);
                }

                // 绘制折线
                var creasePen = new Pen(Brushes.Blue, 1.5);
                foreach (var crease in _currentGeometry.Creases)
                {
                    context.DrawLine(creasePen, crease.ScreenStart, crease.ScreenEnd);
                }
            }

            SlabDrawing = drawingGroup;
        }
    }

    /// <summary>
    /// 楼板轮廓服务类 - 处理Revit API交互
    /// </summary>
    public class SlabProfileService
    {
        private readonly ExternalCommandData _commandData;
        private Floor _floor;
        private SlabShapeEditor _slabShapeEditor;

        // 坐标变换矩阵
        private Matrix4 _to2DMatrix;
        private Matrix4 _restoreMatrix;
        private Matrix4 _transformMatrix;
        private Matrix4 _rotateMatrix;

        private double _rotateAngleX;
        private double _rotateAngleY;
        private const int CanvasWidth = 354;
        private const int CanvasHeight = 280;

        public SlabProfileService(ExternalCommandData commandData, Floor floor)
        {
            _commandData = commandData;
            _floor = floor;
            _slabShapeEditor = floor.SlabShapeEditor;
            InitializeMatrices();
        }

        /// <summary>
        /// 初始化坐标变换矩阵
        /// </summary>
        private void InitializeMatrices()
        {
            // 创建2D变换矩阵（Y轴取反以适配屏幕坐标）
            _to2DMatrix = new Matrix4(new Vector4(1, 0, 0), new Vector4(0, -1, 0), new Vector4(0, 0, 1));
        }

        /// <summary>
        /// 获取楼板几何信息
        /// </summary>
        public SlabGeometryModel GetSlabGeometry()
        {
            var model = new SlabGeometryModel();

            // 获取楼板边界边
            var edges = GetFloorEdges();

            // 提取所有边界点
            var allPoints = new List<XYZ>();
            var boundaryPoints = new List<Point>();

            foreach (Edge edge in edges)
            {
                var tessellatePoints = edge.Tessellate() as List<XYZ>;
                if (tessellatePoints == null) continue;

                for (int i = 0; i < tessellatePoints.Count - 1; i++)
                {
                    var point = tessellatePoints[i];
                    allPoints.Add(point);

                    // 变换到屏幕坐标
                    var screenPoint = TransformToScreen(point);
                    boundaryPoints.Add(screenPoint);
                }
            }

            // 计算边界范围
            if (allPoints.Any())
            {
                var minX = allPoints.Min(p => p.X);
                var maxX = allPoints.Max(p => p.X);
                var minY = allPoints.Min(p => p.Y);
                var maxY = allPoints.Max(p => p.Y);

                model.MinBound = new Point(minX, minY);
                model.MaxBound = new Point(maxX, maxY);
            }

            model.BoundaryPoints = boundaryPoints;
            model.RotateAngleX = _rotateAngleX;
            model.RotateAngleY = _rotateAngleY;

            return model;
        }

        /// <summary>
        /// 获取楼板所有边
        /// </summary>
        private EdgeArray GetFloorEdges()
        {
            var edges = new EdgeArray();
            var options = _commandData.Application.Application.Create.NewGeometryOptions();
            options.DetailLevel = ViewDetailLevel.Medium;
            options.ComputeReferences = true;

            var geometry = _floor.get_Geometry(options);

            foreach (GeometryObject geo in geometry)
            {
                if (geo is Solid solid)
                {
                    foreach (Autodesk.Revit.DB.Face face in solid.Faces)
                    {
                        foreach (EdgeArray edgeLoop in face.EdgeLoops)
                        {
                            foreach (Edge edge in edgeLoop)
                            {
                                edges.Append(edge);
                            }
                        }
                    }
                }
            }

            return edges;
        }

        /// <summary>
        /// 将Revit 3D坐标变换为屏幕2D坐标
        /// </summary>
        private Point TransformToScreen(XYZ point)
        {
            // 获取边界范围用于缩放计算
            var bounds = GetBounds();
            var scaleX = CanvasWidth / (bounds.MaxX - bounds.MinX);
            var scaleY = CanvasHeight / (bounds.MaxY - bounds.MinY);
            var scale = Math.Min(scaleX, scaleY) * 0.85;

            // 计算中心偏移
            var centerX = (bounds.MinX + bounds.MaxX) / 2;
            var centerY = (bounds.MinY + bounds.MaxY) / 2;

            // 应用变换：缩放、移动到中心、Y轴翻转
            var screenX = (point.X - centerX) * scale + CanvasWidth / 2;
            var screenY = CanvasHeight / 2 - (point.Y - centerY) * scale;

            // 应用旋转变换
            if (_rotateAngleX != 0 || _rotateAngleY != 0)
            {
                // 简化旋转变换
                var rotatedX = screenX * Math.Cos(_rotateAngleX) - screenY * Math.Sin(_rotateAngleY);
                var rotatedY = screenX * Math.Sin(_rotateAngleX) + screenY * Math.Cos(_rotateAngleY);
                screenX = rotatedX;
                screenY = rotatedY;
            }

            return new Point(screenX, screenY);
        }

        /// <summary>
        /// 将屏幕坐标变换为Revit世界坐标
        /// </summary>
        private XYZ TransformToWorld(Point screenPoint)
        {
            var bounds = GetBounds();
            var scaleX = CanvasWidth / (bounds.MaxX - bounds.MinX);
            var scaleY = CanvasHeight / (bounds.MaxY - bounds.MinY);
            var scale = Math.Min(scaleX, scaleY) * 0.85;

            var centerX = (bounds.MinX + bounds.MaxX) / 2;
            var centerY = (bounds.MinY + bounds.MaxY) / 2;

            // 逆变换
            var worldX = (screenPoint.X - CanvasWidth / 2) / scale + centerX;
            var worldY = (centerY - screenPoint.Y / scale); // Y轴翻转

            return new XYZ(worldX, worldY, 0);
        }

        /// <summary>
        /// 获取楼板边界范围
        /// </summary>
        private (double MinX, double MaxX, double MinY, double MaxY) GetBounds()
        {
            var edges = GetFloorEdges();
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (Edge edge in edges)
            {
                var points = edge.Tessellate() as List<XYZ>;
                if (points == null) continue;

                foreach (var point in points)
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                }
            }

            return (minX, maxX, minY, maxY);
        }

        /// <summary>
        /// 添加顶点
        /// </summary>
        public bool AddVertex(Point screenPoint)
        {
            var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "添加顶点");
            transaction.Start();

            try
            {
                var worldPoint = TransformToWorld(screenPoint);
                var vertex = _slabShapeEditor.DrawPoint(worldPoint);
                transaction.Commit();
                return vertex != null;
            }
            catch
            {
                transaction.RollBack();
                return false;
            }
        }

        /// <summary>
        /// 添加折线
        /// </summary>
        public bool AddCrease(Point startScreen, Point endScreen)
        {
            var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "添加折线");
            transaction.Start();

            try
            {
                var startWorld = TransformToWorld(startScreen);
                var endWorld = TransformToWorld(endScreen);

                var vertex1 = _slabShapeEditor.DrawPoint(startWorld);
                var vertex2 = _slabShapeEditor.DrawPoint(endWorld);
                var creases = _slabShapeEditor.DrawSplitLine(vertex1, vertex2);

                transaction.Commit();
                return creases != null && creases.Size > 0;
            }
            catch
            {
                transaction.RollBack();
                return false;
            }
        }

        /// <summary>
        /// 重置楼板形状
        /// </summary>
        public void ResetSlabShape()
        {
            var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "重置形状");
            transaction.Start();
            _slabShapeEditor.ResetSlabShape();
            transaction.Commit();

            // 重置旋转角度
            _rotateAngleX = 0;
            _rotateAngleY = 0;
            _rotateMatrix = null;
        }

        /// <summary>
        /// 旋转视图
        /// </summary>
        public void Rotate(double deltaX, double deltaY)
        {
            _rotateAngleX += deltaX;
            _rotateAngleY += deltaY;
        }

        /// <summary>
        /// 检查是否可以创建顶点
        /// </summary>
        public bool CanCreateVertex(Point screenPoint)
        {
            var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "检查顶点");
            transaction.Start();

            try
            {
                var worldPoint = TransformToWorld(screenPoint);
                var vertex = _slabShapeEditor.DrawPoint(worldPoint);
                transaction.RollBack();
                return vertex != null;
            }
            catch
            {
                transaction.RollBack();
                return false;
            }
        }

        /// <summary>
        /// 更新楼板形状（刷新显示）
        /// </summary>
        public void UpdateSlabShape()
        {
            GetSlabGeometry(); // 重新获取几何信息
        }
    }

    /// <summary>
    /// 楼板几何数据模型
    /// </summary>
    public class SlabGeometryModel
    {
        /// <summary>
        /// 楼板轮廓点集合（2D坐标）
        /// </summary>
        public List<Point> BoundaryPoints { get; set; } = new List<Point>();

        /// <summary>
        /// 顶点集合
        /// </summary>
        public List<SlabShapeVertexInfo> Vertices { get; set; } = new List<SlabShapeVertexInfo>();

        /// <summary>
        /// 折线集合
        /// </summary>
        public List<SlabShapeCreaseInfo> Creases { get; set; } = new List<SlabShapeCreaseInfo>();

        /// <summary>
        /// 边界最小点
        /// </summary>
        public Point MinBound { get; set; }

        /// <summary>
        /// 边界最大点
        /// </summary>
        public Point MaxBound { get; set; }

        /// <summary>
        /// 当前旋转角度
        /// </summary>
        public double RotateAngleX { get; set; }

        public double RotateAngleY { get; set; }
    }

    /// <summary>
    /// 楼板形状顶点信息
    /// </summary>
    public class SlabShapeVertexInfo
    {
        public XYZ WorldPosition { get; set; }
        public Point ScreenPosition { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// 楼板形状折线信息
    /// </summary>
    public class SlabShapeCreaseInfo
    {
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public Point ScreenStart { get; set; }
        public Point ScreenEnd { get; set; }
    }

    ///// <summary>
    ///// 4x4矩阵变换辅助类
    ///// </summary>
    //public class Matrix4b
    //{
    //    public double[,] M { get; set; } = new double[4, 4];

    //    public Matrix4b()
    //    {
    //        // 初始化为单位矩阵
    //        for (int i = 0; i < 4; i++)
    //            M[i, i] = 1;
    //    }

    //    public Matrix4b(Vector4 xAxis, Vector4 yAxis, Vector4 zAxis)
    //    {
    //        M[0, 0] = xAxis.X; M[0, 1] = xAxis.Y; M[0, 2] = xAxis.Z; M[0, 3] = xAxis.W;
    //        M[1, 0] = yAxis.X; M[1, 1] = yAxis.Y; M[1, 2] = yAxis.Z; M[1, 3] = yAxis.W;
    //        M[2, 0] = zAxis.X; M[2, 1] = zAxis.Y; M[2, 2] = zAxis.Z; M[2, 3] = zAxis.W;
    //        M[3, 3] = 1;
    //    }

    //    public Matrix4b(Vector4 translation)
    //    {
    //        M[0, 0] = 1; M[0, 1] = 0; M[0, 2] = 0; M[0, 3] = translation.X;
    //        M[1, 0] = 0; M[1, 1] = 1; M[1, 2] = 0; M[1, 3] = translation.Y;
    //        M[2, 0] = 0; M[2, 1] = 0; M[2, 2] = 1; M[2, 3] = translation.Z;
    //        M[3, 3] = 1;
    //    }

    //    public Matrix4b(double scale)
    //    {
    //        M[0, 0] = scale; M[1, 1] = scale; M[2, 2] = scale; M[3, 3] = 1;
    //    }

    //    public static Matrix4b Multiply(Matrix4b a, Matrix4b b)
    //    {
    //        var result = new Matrix4b();
    //        for (int i = 0; i < 4; i++)
    //        {
    //            for (int j = 0; j < 4; j++)
    //            {
    //                result.M[i, j] = 0;
    //                for (int k = 0; k < 4; k++)
    //                {
    //                    result.M[i, j] += a.M[i, k] * b.M[k, j];
    //                }
    //            }
    //        }
    //        return result;
    //    }

    //    public Vector4b Transform(Vector4b v)
    //    {
    //        return new Vector4b(
    //            M[0, 0] * v.X + M[0, 1] * v.Y + M[0, 2] * v.Z + M[0, 3] * v.W,
    //            M[1, 0] * v.X + M[1, 1] * v.Y + M[1, 2] * v.Z + M[1, 3] * v.W,
    //            M[2, 0] * v.X + M[2, 1] * v.Y + M[2, 2] * v.Z + M[2, 3] * v.W,
    //            M[3, 0] * v.X + M[3, 1] * v.Y + M[3, 2] * v.Z + M[3, 3] * v.W
    //        );
    //    }

    //    public Matrix4b Inverse()
    //    {
    //        // 简化的逆矩阵计算（实际应用中需要完整实现）
    //        var inv = new Matrix4b();
    //        // TODO: 实现完整的4x4矩阵求逆
    //        return inv;
    //    }

    //    public static Matrix4b RotateX(double angle)
    //    {
    //        var m = new Matrix4b();
    //        double cos = Math.Cos(angle);
    //        double sin = Math.Sin(angle);
    //        m.M[1, 1] = cos; m.M[1, 2] = -sin;
    //        m.M[2, 1] = sin; m.M[2, 2] = cos;
    //        return m;
    //    }

    //    public static Matrix4b RotateY(double angle)
    //    {
    //        var m = new Matrix4b();
    //        double cos = Math.Cos(angle);
    //        double sin = Math.Sin(angle);
    //        m.M[0, 0] = cos; m.M[0, 2] = sin;
    //        m.M[2, 0] = -sin; m.M[2, 2] = cos;
    //        return m;
    //    }
    //}

    ///// <summary>
    ///// 四维向量辅助类
    ///// </summary>
    //public class Vector4b
    //{
    //    public double X { get; set; }
    //    public double Y { get; set; }
    //    public double Z { get; set; }
    //    public double W { get; set; } = 1;

    //    public Vector4b(double x = 0, double y = 0, double z = 0, double w = 1)
    //    {
    //        X = x; Y = y; Z = z; W = w;
    //    }

    //    public Vector4b(XYZ xyz)
    //    {
    //        X = xyz.X; Y = xyz.Y; Z = xyz.Z; W = 1;
    //    }
    //}
}
