using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
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
//using Point = System.Drawing.Point;
using Point = System.Windows.Point;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PathReinforcement.xaml 的交互逻辑
    /// </summary>
    public partial class PathReinforcementEdit : Window
    {
        private readonly NewPathReinforcementViewModel _viewModel;
        public PathReinforcementEdit(Profile profile)
        {
            InitializeComponent();
            _viewModel = new NewPathReinforcementViewModel(profile);
            _viewModel.CloseWindow = Close;

            DataContext = _viewModel;

            // 绑定绘图更新
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NewPathReinforcementViewModel.DrawingVisual))
                {
                    UpdateDrawing();
                }
            };
        }
        private void UpdateDrawing()
        {
            var visual = _viewModel.DrawingVisual;
            if (visual?.Drawing == null) return;

            var drawingImage = new DrawingImage(visual.Drawing);
            DrawingArea.Source = drawingImage;
        }
        private void DrawingArea_MouseDown(object sender, MouseButtonEventArgs e) =>
            _viewModel.MouseDownCommand.Execute(e);

        private void DrawingArea_MouseMove(object sender, MouseEventArgs e) =>
            _viewModel.MouseMoveCommand.Execute(e);

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) =>
            _viewModel.KeyPressCommand.Execute(e);
    }
    /// <summary>
    /// 路径钢筋编辑窗体ViewModel - 管理钢筋路径绘制和创建
    /// </summary>
    public class NewPathReinforcementViewModel : ObserverableObject
    {
        #region 字段
        private readonly Profile _profile;
        private readonly Matrix4 _to2DMatrix;
        private readonly Matrix4 _moveToCenterMatrix;
        private Matrix4 _scaleMatrix;
        private readonly LineTool _lineTool;
        private DrawingVisual _drawingVisual;
        private bool _isFlip;
        private bool _hasPoints;
        #endregion

        #region 属性
        public DrawingVisual DrawingVisual
        {
            get => _drawingVisual;
            set { _drawingVisual = value; OnPropertyChanged(); }
        }

        public bool IsFlip
        {
            get => _isFlip;
            set { _isFlip = value; OnPropertyChanged(); }
        }

        public bool HasPoints
        {
            get => _hasPoints;
            set { _hasPoints = value; OnPropertyChanged(); }
        }

        public string InstructionText => "左键点击添加控制点，右键完成绘制";
        #endregion

        #region 命令
        public ICommand CreateCommand;
        public ICommand CancelCommand;
        public ICommand PreviewCommand;
        public ICommand CleanCommand;
        public ICommand MouseDownCommand;
        public ICommand MouseMoveCommand;
        public ICommand KeyPressCommand;
        #endregion

        public NewPathReinforcementViewModel(Profile profile)
        {
            //_profile = profile ?? throw new ArgumentNullException(nameof(profile));
            //_to2DMatrix = profile.To2DMatrix;
            //_moveToCenterMatrix = profile.ToCenterMatrix();
            //_lineTool = new LineTool();

            //InitializeCommands();
            //ComputeScaleMatrix();

            //_drawingVisual = new DrawingVisual();
            //DrawProfile();

            //_lineTool.OnPointsChanged += OnPointsChanged;
        }

        private void InitializeCommands()
        {
            CreateCommand = new BaseBindingCommand(_ => CreatePathReinforcement(), _ => _hasPoints);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            PreviewCommand = new BaseBindingCommand(_ => PreviewPathReinforcement(), _ => _hasPoints);
            CleanCommand = new BaseBindingCommand(_ => CleanDrawing());
            MouseDownCommand = new RelayCommand<MouseEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(OnMouseMove);
            KeyPressCommand = new RelayCommand<KeyEventArgs>(OnKeyPress);
        }

        private void OnPointsChanged() => HasPoints = _lineTool.PointsNumber > 2;

        /// <summary>
        /// 计算缩放矩阵 - 使图形适配控件大小
        /// </summary>
        private void ComputeScaleMatrix(double controlWidth = 400, double controlHeight = 300)
        {
            //var bounds = _profile.GetFaceBounds();
            //float width = (float)controlWidth / (bounds[1].X - bounds[0].X);
            //float height = (float)controlHeight / (bounds[1].Y - bounds[0].Y);
            //float factor = Math.Min(width, height);
            //_scaleMatrix = new Matrix4(factor);
        }

        /// <summary>
        /// 绘制构件轮廓
        /// </summary>
        private void DrawProfile()
        {
            //using (var dc = _drawingVisual.RenderOpen())
            //{
            //    var transform = Compute3DTo2DMatrix();
            //    _profile.Draw2D(dc, new Pen(Brushes.Blue, 2), transform);
            //}
        }

        private Matrix4 Compute3DTo2DMatrix()
        {
            var result = Matrix4.Multiply(_to2DMatrix.Inverse(), _moveToCenterMatrix.Inverse());
            return Matrix4.Multiply(result, _scaleMatrix);
        }

        /// <summary>
        /// 刷新绘图区域
        /// </summary>
        public void RefreshDrawing()
        {
            //using (var dc = _drawingVisual.RenderOpen())
            //{
            //    var transform = Compute3DTo2DMatrix();
            //    _profile.Draw2D(dc, new Pen(Brushes.Blue, 2), transform);
            //    _lineTool.Draw(dc);
            //}
            //OnPropertyChanged(nameof(DrawingVisual));
        }

        #region 鼠标事件处理
        private void OnMouseDown(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _lineTool.OnMouseDown(e.GetPosition(null));
                RefreshDrawing();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _lineTool.OnRightClick();
                RefreshDrawing();
            }
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            if (_lineTool.IsDrawing)
            {
                _lineTool.OnMouseMove(e.GetPosition(null));
                RefreshDrawing();
            }
        }

        private void OnKeyPress(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CleanDrawing();
            }
        }
        #endregion

        /// <summary>
        /// 清理所有绘制的点
        /// </summary>
        private void CleanDrawing()
        {
            _lineTool.Clear();
            RefreshDrawing();
        }

        /// <summary>
        /// 预览路径钢筋（临时创建后删除）
        /// </summary>
        private void PreviewPathReinforcement()
        {
            //try
            //{
            //    var points2D = _lineTool.GetPoints();
            //    if (points2D.Count < 3) return;

            //    var points3D = TransformPointsTo3D(points2D);
            //    var previewReinforcement = _profile.CreatePathReinforcement(points3D, _isFlip);

            //    // 注意：实际预览需要临时创建后删除，或使用Transaction + Rollback
            //    // 简化实现：直接创建后删除，或调用OpenPreview方法

            //    TaskDialog.Show("预览", "已创建预览钢筋（需要在事务中实现回滚）");
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", $"预览失败：{ex.Message}");
            //}
        }

        /// <summary>
        /// 创建路径钢筋
        /// </summary>
        private void CreatePathReinforcement()
        {
            //try
            //{
            //    var points2D = _lineTool.GetPoints();
            //    if (points2D.Count < 3)
            //    {
            //        TaskDialog.Show("提示", "请至少绘制3个点以创建路径钢筋");
            //        return;
            //    }

            //    var points3D = TransformPointsTo3D(points2D);
            //    _profile.CreatePathReinforcement(points3D, _isFlip);

            //    TaskDialog.Show("成功", "路径钢筋创建成功");
            //    CloseWindow?.Invoke();
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", $"创建失败：{ex.Message}");
            //}
        }

        /// <summary>
        /// 2D屏幕坐标转3D世界坐标
        /// </summary>
        private List<Vector4> TransformPointsTo3D(List<Point> points)
        {
            //var transformMatrix = Matrix4.Multiply(_scaleMatrix.Inverse(), _moveToCenterMatrix);
            //transformMatrix = Matrix4.Multiply(transformMatrix, _to2DMatrix);

            //return points.Select(p => transformMatrix.Transform(new Vector4(p.X, p.Y, 0))).ToList();
            return null;
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 直线绘图工具 - 使用C# 7.3表达式体成员
    /// </summary>
    public class LineTool
    {
        private List<Point> _points = new List<Point>();
        private readonly Pen _foregroundPen = new Pen(Brushes.Red, 2);
        private readonly Pen _backgroundPen = new Pen(Brushes.White, 3);
        private Point _preMovePoint;
        private bool _isDrawing;

        public bool IsDrawing => _isDrawing;
        public int PointsNumber => _points.Count;
        public event Action OnPointsChanged;

        public List<Point> GetPoints() => _points.ToList();

        public void Clear()
        {
            _points.Clear();
            _isDrawing = false;
            OnPointsChanged?.Invoke();
        }

        public void OnMouseDown(Point position)
        {
            if (!_isDrawing)
            {
                _isDrawing = true;
                _points.Clear();
            }

            _preMovePoint = position;
            _points.Add(position);
            OnPointsChanged?.Invoke();
        }

        public void OnMouseMove(Point position)
        {
            if (!_isDrawing || _points.Count == 0) return;

            _preMovePoint = position;
        }

        public void OnRightClick()
        {
            if (_points.Count >= 3)
            {
                _isDrawing = false;
                OnPointsChanged?.Invoke();
            }
        }

        public void Draw(DrawingContext dc)
        {
            // 绘制已完成的线段
            for (int i = 0; i < _points.Count - 1; i++)
            {
                dc.DrawLine(_foregroundPen, _points[i], _points[i + 1]);
            }

            // 绘制预览线
            if (_isDrawing && _points.Count > 0)
            {
                dc.DrawLine(_backgroundPen, _points[_points.Count - 1], _preMovePoint);
                dc.DrawLine(_foregroundPen, _points[_points.Count - 1], _preMovePoint);
            }
        }
    }
}
