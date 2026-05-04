using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
using Point = System.Drawing.Point;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// NewOpeningView.xaml 的交互逻辑
    /// </summary>
    public partial class NewOpeningView : Window
    {
        private NewOpeningsViewModel _viewModel;
        public NewOpeningView(Profile profile)
        {
            InitializeComponent();
            _viewModel = new NewOpeningsViewModel(profile);
            _viewModel.CloseWindow = Close;

            DataContext = _viewModel;

            // 绑定绘图更新
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NewOpeningsViewModel.DrawingVisual))
                {
                    UpdateDrawing();
                }
            };
        }

        private void UpdateDrawing()
        {
            var visual = _viewModel.DrawingVisual;
            if (visual == null) return;

            var drawingImage = new DrawingImage(visual.Drawing);
            DrawingArea.Source = drawingImage;
        }

        private void DrawingArea_MouseDown(object sender, MouseButtonEventArgs e) =>
            _viewModel.MouseDownCommand.Execute(e);

        private void DrawingArea_MouseMove(object sender, MouseEventArgs e) =>
            _viewModel.MouseMoveCommand.Execute(e);

        private void DrawingArea_MouseUp(object sender, MouseButtonEventArgs e) =>
            _viewModel.MouseUpCommand.Execute(e);
    }
    /// <summary>
    /// 洞口编辑窗体ViewModel - 管理工具切换、图形绘制和坐标转换
    /// </summary>
    public class NewOpeningsViewModel : ObserverableObject
    {
        #region 字段
        private readonly Profile _profile;
        private readonly Matrix4 _to2DMatrix;
        private readonly Matrix4 _moveToCenterMatrix;
        private Matrix4 _scaleMatrix;
        private ITool _currentTool;
        private readonly Queue<ITool> _tools = new Queue<ITool>();
        private DrawingVisual _drawingVisual;
        private string _currentToolTip;
        #endregion

        #region 属性
        public ObservableCollection<ITool> Tools { get; } = new ObservableCollection<ITool>();

        public ITool CurrentTool
        {
            get => _currentTool;
            set
            {
                _currentTool = value;
                CurrentToolTip = value?.ToolType.ToString() ?? "无";
                OnPropertyChanged();
                RefreshDrawing();
            }
        }

        public string CurrentToolTip
        {
            get => _currentToolTip;
            set { _currentToolTip = value; OnPropertyChanged(); }
        }

        public DrawingVisual DrawingVisual
        {
            get => _drawingVisual;
            set { _drawingVisual = value; OnPropertyChanged(); }
        }
        #endregion

        #region 命令
        public ICommand OkCommand;
        public ICommand CancelCommand;
        public ICommand SwitchToolCommand;
        public ICommand MouseDownCommand;
        public ICommand MouseMoveCommand;
        public ICommand MouseUpCommand;
        #endregion

        public NewOpeningsViewModel(Profile profile)
        {
            //_profile = profile ?? throw new ArgumentNullException(nameof(profile));
            //_to2DMatrix = profile.To2DMatrix();
            //_moveToCenterMatrix = profile.ToCenterMatrix();
            //InitializeTools();
            //InitializeCommands();
            //ComputeScaleMatrix();
            //_drawingVisual = new DrawingVisual();
            //DrawProfile();
        }

        private void InitializeCommands()
        {
            OkCommand = new BaseBindingCommand(_ => OkButtonClick());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow());
            SwitchToolCommand = new BaseBindingCommand(_ => SwitchTool());
            MouseDownCommand = new RelayCommand<MouseEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(OnMouseMove);
            MouseUpCommand = new RelayCommand<MouseEventArgs>(OnMouseUp);
        }

        ///// <summary>
        ///// 初始化绘图工具 - 墙和楼板支持不同工具集
        ///// </summary>
        //private void InitializeTools()
        //{
        //    if (_profile is ProfileWall)
        //    {
        //        AddTool(new RectTool());
        //        AddTool(new EmptyTool());
        //    }
        //    else
        //    {
        //        AddTool(new LineTool());
        //        AddTool(new RectTool());
        //        AddTool(new CircleTool());
        //        AddTool(new ArcTool());
        //        AddTool(new EmptyTool());
        //    }
        //    CurrentTool = Tools.FirstOrDefault();
        //}

        private void AddTool(ITool tool)
        {
            Tools.Add(tool);
            _tools.Enqueue(tool);
        }

        /// <summary>
        /// 切换工具 - 中键点击时调用
        /// </summary>
        private void SwitchTool()
        {
            var nextTool = _tools.Dequeue();
            _tools.Enqueue(nextTool);
            CurrentTool = _tools.Peek();
        }

        /// <summary>
        /// 计算缩放矩阵 - 使图形适配PictureBox大小
        /// </summary>
        private void ComputeScaleMatrix(double controlWidth = 400, double controlHeight = 400)
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
            //    var trans = Compute3DTo2DMatrix();
            //    _profile.Draw2D(dc, new System.Drawing.Pen(System.Drawing.Brushes.Blue, 2), trans);
            //}
        }

        private Matrix4 Compute3DTo2DMatrix()
        {
            var result = Matrix4.Multiply(_to2DMatrix.Inverse(), _moveToCenterMatrix.Inverse());
            result = Matrix4.Multiply(result, _scaleMatrix);
            return result;
        }

        /// <summary>
        /// 刷新绘图区域
        /// </summary>
        public void RefreshDrawing()
        {
            //DrawProfile();
            //if (CurrentTool?.Lines != null)
            //{
            //    using (var dc = _drawingVisual.RenderOpen())
            //    {
            //        CurrentTool.Draw(dc);
            //    }
            //}
            //OnPropertyChanged(nameof(DrawingVisual));
        }

        #region 鼠标事件处理
        private void OnMouseDown(MouseEventArgs e)
        {
            //if (e.LeftButton == MouseButtonState.Pressed)
            //{
            //    CurrentTool?.OnMouseDown(CreateGraphics(), e.GetPosition(null));
            //}
            //else if (e.MiddleButton == MouseButtonState.Pressed)
            //{
            //    SwitchTool();
            //}
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            //CurrentTool?.OnMouseMove(CreateGraphics(), e.GetPosition(null));
            //RefreshDrawing();
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            //CurrentTool?.OnMouseUp(CreateGraphics(), e.GetPosition(null));
            //RefreshDrawing();
        }
        #endregion

        /// <summary>
        /// 创建临时Graphics用于预览绘制
        /// </summary>
        private DrawingContext CreateGraphics()
        {
            var visual = new DrawingVisual();
            return visual.RenderOpen();
        }

        /// <summary>
        /// 确定按钮 - 生成洞口
        /// </summary>
        private void OkButtonClick()
        {
            //foreach (var tool in Tools)
            //{
            //    foreach (var curve in tool.GetLines())
            //    {
            //        List<Vector4> points3D;

            //        if (tool.ToolType == ToolType.Circle)
            //        {
            //            points3D = GenerateCirclePoints(curve);
            //        }
            //        else if (tool.ToolType == ToolType.Rectangle)
            //        {
            //            var points = new Point[4];
            //            points[0] = curve[0];
            //            points[1] = new Point(curve[0].X, curve[1].Y);
            //            points[2] = curve[1];
            //            points[3] = new Point(curve[1].X, curve[0].Y);
            //            points3D = Transform2DTo3D(points);
            //        }
            //        else
            //        {
            //            points3D = Transform2DTo3D(curve.ToArray());
            //        }
            //        _profile.DrawOpening(points3D, tool.ToolType);
            //    }
            //}
            CloseWindow?.Invoke();
        }

        /// <summary>
        /// 生成圆形洞口的四个点
        /// </summary>
        private List<Vector4> GenerateCirclePoints(List<Point> points)
        {
            //var center = points[0];
            //var bound = points[1];

            //var rotation = new Matrix();
            //rotation.RotateAt(90, (PointF)center);

            //var circlePoints = new Point[4];
            //circlePoints[0] = points[1];

            //for (int i = 1; i < 4; i++)
            //{
            //    var ps = new[] { bound };
            //    rotation.TransformPoints(ps);
            //    circlePoints[i] = ps[0];
            //    bound = ps[0];
            //}
            //return Transform2DTo3D(circlePoints);
            return null;
        }

        /// <summary>
        /// 2D屏幕坐标转3D世界坐标
        /// </summary>
        private List<Vector4> Transform2DTo3D(Point[] points)
        {
            //var transformMatrix = Matrix4.Multiply(_scaleMatrix.Inverse(), _moveToCenterMatrix);
            //transformMatrix = Matrix4.Multiply(transformMatrix, _to2DMatrix);

            //return points.Select(p => transformMatrix.TransForm(new Vector4(p.X, p.Y, 0))).ToList();
            return null;
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }




    /// <summary>
    /// 绘图工具基类 - 使用C# 7.3表达式体成员
    /// </summary>
    public abstract class ITool
    {
        protected ToolType _type;
        protected List<Point> _points = new List<Point>();
        protected List<List<Point>> _lines = new List<List<Point>>();
        protected System.Drawing.Pen _backgroundPen = new System.Drawing.Pen(System.Drawing.Brushes.White, 1);
        protected System.Drawing.Pen _foregroundPen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 1);
        protected Point _preMovePoint;

        public virtual ToolType ToolType => _type;
        public List<List<Point>> GetLines() => _lines;

        public virtual void Draw(DrawingContext dc)
        {
            foreach (var line in _lines)
            {
                if (line.Count >= 2)
                    DrawGeometry(dc, line);
            }
        }

        protected abstract void DrawGeometry(DrawingContext dc, List<Point> points);

        public virtual void OnMouseDown(DrawingContext dc, Point position)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _preMovePoint = position;
                _points.Add(position);
            }
        }

        public virtual void OnMouseMove(DrawingContext dc, Point position) { }
        public virtual void OnMouseUp(DrawingContext dc, Point position) { }
        public virtual void OnRightClick(DrawingContext dc, Point position) { }
        public virtual void OnMidClick() => _points.Clear();
    }

    ///// <summary>
    ///// 直线/多边形工具
    ///// </summary>
    //public class LineTool : ITool
    //{
    //    public LineTool() => _type = ToolType.Line;

    //    protected override void DrawGeometry(DrawingContext dc, List<Point> points)
    //    {
    //        for (int i = 0; i < points.Count - 1; i++)
    //            dc.DrawLine(_foregroundPen, points[i], points[i + 1]);
    //        // 闭合多边形
    //        if (points.Count > 2)
    //            dc.DrawLine(_foregroundPen, points[points.Count - 1], points[0]);
    //    }

    //    public override void OnMouseMove(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count > 0)
    //        {
    //            dc.DrawLine(_backgroundPen, _points[_points.Count - 1], _preMovePoint);
    //            _preMovePoint = position;
    //            dc.DrawLine(_foregroundPen, _points[_points.Count - 1], position);
    //        }
    //    }

    //    public override void OnRightClick(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count > 2)
    //        {
    //            _lines.Add(new List<Point>(_points));
    //            _points.Clear();
    //        }
    //    }
    //}

    ///// <summary>
    ///// 矩形工具
    ///// </summary>
    //public class RectTool : ITool
    //{
    //    public RectTool() => _type = ToolType.Rectangle;

    //    protected override void DrawGeometry(DrawingContext dc, List<Point> points)
    //    {
    //        if (points.Count == 2)
    //        {
    //            var rect = new Rect(points[0], points[1]);
    //            dc.DrawRectangle(null, _foregroundPen, rect);
    //        }
    //    }

    //    public override void OnMouseMove(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count == 1)
    //        {
    //            var oldRect = new Rect(_points[0], _preMovePoint);
    //            var newRect = new Rect(_points[0], position);
    //            dc.DrawRectangle(null, _backgroundPen, oldRect);
    //            _preMovePoint = position;
    //            dc.DrawRectangle(null, _foregroundPen, newRect);
    //        }
    //    }

    //    public override void OnMouseUp(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count == 1)
    //        {
    //            _points.Add(position);
    //            _lines.Add(new List<Point>(_points));
    //            _points.Clear();
    //        }
    //    }
    //}

    ///// <summary>
    ///// 圆形工具
    ///// </summary>
    //public class CircleTool : ITool
    //{
    //    public CircleTool() => _type = ToolType.Circle;

    //    protected override void DrawGeometry(DrawingContext dc, List<Point> points)
    //    {
    //        if (points.Count == 2)
    //            DrawCircle(dc, _foregroundPen, points[0], points[1]);
    //    }

    //    private void DrawCircle(DrawingContext dc, Pen pen, Point center, Point bound)
    //    {
    //        double radius = Math.Sqrt(Math.Pow(bound.X - center.X, 2) + Math.Pow(bound.Y - center.Y, 2));
    //        var rect = new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
    //        dc.DrawEllipse(null, pen, center, radius, radius);
    //    }

    //    public override void OnMouseMove(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count == 1)
    //        {
    //            DrawCircle(dc, _backgroundPen, _points[0], _preMovePoint);
    //            _preMovePoint = position;
    //            DrawCircle(dc, _foregroundPen, _points[0], position);
    //        }
    //    }

    //    public override void OnMouseUp(DrawingContext dc, Point position)
    //    {
    //        if (_points.Count == 1)
    //        {
    //            _points.Add(position);
    //            _lines.Add(new List<Point>(_points));
    //            _points.Clear();
    //        }
    //    }
    //}

    public enum ToolType { None, Line, Rectangle, Circle, Arc }
    //原代码
    /// <summary>
    /// Base class of ProfileFloor and ProfileWall
    /// contain the profile information and can make matrix to transform point to 2D plane
    /// </summary>
    //public abstract class Profile
    //{
    //    #region members
    //    /// <summary>
    //    ///Wall or Floor element 
    //    /// </summary>
    //    protected Autodesk.Revit.DB.Element m_dataProfile;

    //    /// <summary>
    //    /// geometry object [face]
    //    /// </summary>
    //    protected List<Edge> m_face;

    //    /// <summary>
    //    ///  command data
    //    /// </summary>
    //    protected Autodesk.Revit.UI.ExternalCommandData m_commandData;

    //    /// <summary>
    //    /// Application creator
    //    /// </summary>
    //    protected Autodesk.Revit.Creation.Application m_appCreator;

    //    /// <summary>
    //    /// Document creator
    //    /// </summary>
    //    protected Autodesk.Revit.Creation.Document m_docCreator;
    //    #endregion

    //    /// <summary>
    //    /// Abstract method to create Opening
    //    /// </summary>
    //    public abstract void DrawOpening(List<Vector4> points, ToolType type);

    //    /// <summary>
    //    /// Draw profile of wall or floor in 2D
    //    /// </summary>
    //    /// <param name="graphics">form graphic</param>
    //    /// <param name="pen">pen use to draw line in pictureBox</param>
    //    /// <param name="matrix4">matrix used to transform points between 3d and 2d.</param>>
    //    public void Draw2D(Graphics graphics, System.Drawing.Pen pen, Matrix4 matrix4)
    //    {
    //        foreach (Edge edge in m_face)
    //        {
    //            List<XYZ> points = edge.Tessellate() as List<XYZ>;
    //            for (int i = 0; i < points.Count - 1; i++)
    //            {
    //                Autodesk.Revit.DB.XYZ point1 = points[i];
    //                Autodesk.Revit.DB.XYZ point2 = points[i + 1];

    //                Vector4 v1 = new Vector4(point1);
    //                Vector4 v2 = new Vector4(point2);

    //                v1 = matrix4.TransForm(v1);
    //                v2 = matrix4.TransForm(v2);
    //                graphics.DrawLine(pen, new Point((int)v1.X, (int)v1.Y),
    //                    new Point((int)v2.X, (int)v2.Y));
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Constructor
    //    /// </summary>
    //    /// <param name="elem">Selected element</param>
    //    /// <param name="commandData">ExternalCommandData</param>
    //    public Profile(Autodesk.Revit.DB.Element elem, ExternalCommandData commandData)
    //    {
    //        m_dataProfile = elem;
    //        m_commandData = commandData;
    //        m_appCreator = m_commandData.Application.Application.Create;
    //        m_docCreator = m_commandData.Application.ActiveUIDocument.Document.Create;

    //        List<List<Edge>> faces = GetFaces(m_dataProfile);
    //        m_face = GetNeedFace(faces);
    //    }

    //    /// <summary>
    //    /// Get edges of element's profile
    //    /// </summary>
    //    /// <param name="elem">Selected element</param>
    //    public List<List<Edge>> GetFaces(Autodesk.Revit.DB.Element elem)
    //    {
    //        List<List<Edge>> faceEdges = new List<List<Edge>>();
    //        Options options = m_appCreator.NewGeometryOptions();
    //        options.DetailLevel = ViewDetailLevel.Medium;
    //        options.ComputeReferences = true;
    //        Autodesk.Revit.DB.GeometryElement geoElem = elem.get_Geometry(options);

    //        //GeometryObjectArray gObjects = geoElem.Objects;
    //        IEnumerator<GeometryObject> Objects = geoElem.GetEnumerator();
    //        //foreach (GeometryObject geo in gObjects)
    //        while (Objects.MoveNext())
    //        {
    //            GeometryObject geo = Objects.Current;

    //            Solid solid = geo as Solid;
    //            if (solid != null)
    //            {
    //                EdgeArray edges = solid.Edges;
    //                FaceArray faces = solid.Faces;
    //                foreach (Face face in faces)
    //                {
    //                    EdgeArrayArray edgeArrarr = face.EdgeLoops;
    //                    foreach (EdgeArray edgeArr in edgeArrarr)
    //                    {
    //                        List<Edge> edgesList = new List<Edge>();
    //                        foreach (Edge edge in edgeArr)
    //                        {
    //                            edgesList.Add(edge);
    //                        }
    //                        faceEdges.Add(edgesList);
    //                    }
    //                }
    //            }
    //        }
    //        return faceEdges;
    //    }

    //    /// <summary>
    //    /// Get Face Normal
    //    /// </summary>
    //    /// <param name="face">Edges in a face</param>
    //    private Vector4 GetFaceNormal(List<Edge> face)
    //    {
    //        Edge eg0 = face[0];
    //        Edge eg1 = face[1];

    //        List<XYZ> points = eg0.Tessellate() as List<XYZ>;
    //        Autodesk.Revit.DB.XYZ start = points[0];
    //        Autodesk.Revit.DB.XYZ end = points[1];

    //        Vector4 vStart = new Vector4((float)start.X, (float)start.Y, (float)start.Z);
    //        Vector4 vEnd = new Vector4((float)end.X, (float)end.Y, (float)end.Z);
    //        Vector4 vSub = vEnd - vStart;

    //        points = eg1.Tessellate() as List<XYZ>;
    //        start = points[0];
    //        end = points[1];

    //        vStart = new Vector4((float)start.X, (float)start.Y, (float)start.Z);
    //        vEnd = new Vector4((float)end.X, (float)end.Y, (float)end.Z);
    //        Vector4 vSub2 = vEnd - vStart;

    //        Vector4 result = vSub.CrossProduct(vSub2);
    //        result.Normalize();
    //        return result;
    //    }

    //    /// <summary>
    //    /// Get First Face
    //    /// </summary>
    //    /// <param name="faces">edges in all faces</param>
    //    private List<Edge> GetNeedFace(List<List<Edge>> faces)
    //    {
    //        if (m_dataProfile is Wall)
    //        {
    //            return GetWallFace(faces);
    //        }
    //        return faces[0];
    //    }

    //    /// <summary>
    //    /// Get a matrix which can transform points to 2D
    //    /// </summary>
    //    public Matrix4 To2DMatrix()
    //    {
    //        if (m_dataProfile is Wall)
    //        {
    //            return WallMatrix();
    //        }
    //        List<XYZ> eg0 = m_face[0].Tessellate() as List<XYZ>;
    //        List<XYZ> eg1 = m_face[1].Tessellate() as List<XYZ>;

    //        Vector4 v1 = new Vector4((float)eg0[0].X,
    //            (float)eg0[0].Y, (float)eg0[0].Z);

    //        Vector4 v2 = new Vector4((float)eg0[1].X,
    //            (float)eg0[1].Y, (float)eg0[1].Z);
    //        Vector4 v21 = v1 - v2;
    //        v21.Normalize();

    //        Vector4 v3 = new Vector4((float)eg1[0].X,
    //            (float)eg1[0].Y, (float)eg1[0].Z);

    //        Vector4 v4 = new Vector4((float)eg1[1].X,
    //            (float)eg1[1].Y, (float)eg1[1].Z);
    //        Vector4 v43 = v4 - v3;
    //        v43.Normalize();

    //        Vector4 vZAxis = Vector4.CrossProduct(v43, v21);
    //        Vector4 vYAxis = Vector4.CrossProduct(vZAxis, v43);
    //        vYAxis.Normalize();
    //        vZAxis.Normalize();
    //        Vector4 vOrigin = (v4 + v1) / 2;

    //        Matrix4 result = new Matrix4(v43, vYAxis, vZAxis, vOrigin);
    //        return result;
    //    }

    //    /// <summary>
    //    /// Wall matrix
    //    /// </summary>
    //    /// <returns></returns>
    //    public Matrix4 WallMatrix()
    //    {
    //        //get the location curve
    //        LocationCurve location = m_dataProfile.Location as LocationCurve;
    //        Vector4 xAxis = new Vector4(1, 0, 0);
    //        Vector4 yAxis = new Vector4(0, 1, 0);
    //        Vector4 zAxis = new Vector4(0, 0, 1);
    //        Vector4 origin = new Vector4(0, 0, 0);
    //        if (location != null)
    //        {
    //            Curve curve = location.Curve;
    //            Autodesk.Revit.DB.XYZ start = curve.GetEndPoint(0);
    //            Autodesk.Revit.DB.XYZ end = curve.GetEndPoint(1);

    //            xAxis = new Vector4((float)(end.X - start.X),
    //                (float)(end.Y - start.Y), (float)(end.Z - start.Z));
    //            xAxis.Normalize();

    //            yAxis = new Vector4(0, 0, 1);

    //            zAxis = Vector4.CrossProduct(xAxis, yAxis);
    //            zAxis.Normalize();

    //            origin = new Vector4((float)(end.X + start.X) / 2,
    //                (float)(end.Y + start.Y) / 2, (float)(end.Z + start.Z) / 2);
    //        }
    //        return new Matrix4(xAxis, yAxis, zAxis, origin);
    //    }

    //    /// <summary>
    //    /// Get wall face
    //    /// </summary>
    //    /// <param name="faces"></param>
    //    /// <returns></returns>
    //    private List<Edge> GetWallFace(List<List<Edge>> faces)
    //    {
    //        LocationCurve location = m_dataProfile.Location as LocationCurve;
    //        Curve curve = location.Curve;
    //        List<XYZ> xyzs = curve.Tessellate() as List<XYZ>;
    //        Vector4 zAxis = new Vector4(0, 0, 1);

    //        if (xyzs.Count == 2)
    //        {
    //            return faces[0];
    //        }

    //        foreach (List<Edge> face in faces)
    //        {
    //            foreach (Edge edge in face)
    //            {
    //                List<XYZ> edgexyzs = edge.Tessellate() as List<XYZ>;
    //                if (xyzs.Count == edgexyzs.Count)
    //                {
    //                    Vector4 normal = GetFaceNormal(face);
    //                    Vector4 cross = Vector4.CrossProduct(zAxis, normal);
    //                    cross.Normalize();
    //                    if (cross.Length() == 1)
    //                    {
    //                        return face;
    //                    }
    //                }
    //            }
    //        }
    //        return faces[0];
    //    }

    //    /// <summary>
    //    /// Get a matrix which can move points to origin
    //    /// </summary>
    //    public Matrix4 ToCenterMatrix()
    //    {
    //        //translate the origin to bound center
    //        PointF[] bounds = GetFaceBounds();
    //        PointF min = bounds[0];
    //        PointF max = bounds[1];
    //        PointF center = new PointF((min.X + max.X) / 2, (min.Y + max.Y) / 2);
    //        return new Matrix4(new Vector4(center.X, center.Y, 0));
    //    }

    //    /// <summary>
    //    /// Get Face Bounds
    //    /// </summary>
    //    public PointF[] GetFaceBounds()
    //    {
    //        Matrix4 matrix = To2DMatrix();
    //        Matrix4 inverseMatrix = matrix.Inverse();
    //        float minX = 0, maxX = 0, minY = 0, maxY = 0;
    //        bool bFirstPoint = true;
    //        foreach (Edge edge in m_face)
    //        {
    //            List<XYZ> points = edge.Tessellate() as List<XYZ>;

    //            foreach (Autodesk.Revit.DB.XYZ point in points)
    //            {
    //                Vector4 v = new Vector4(point);
    //                Vector4 v1 = inverseMatrix.TransForm(v);

    //                if (bFirstPoint)
    //                {
    //                    minX = maxX = v1.X;
    //                    minY = maxY = v1.Y;
    //                    bFirstPoint = false;
    //                }
    //                else
    //                {
    //                    if (v1.X < minX)
    //                    {
    //                        minX = v1.X;
    //                    }
    //                    else if (v1.X > maxX)
    //                    {
    //                        maxX = v1.X;
    //                    }

    //                    if (v1.Y < minY)
    //                    {
    //                        minY = v1.Y;
    //                    }
    //                    else if (v1.Y > maxY)
    //                    {
    //                        maxY = v1.Y;
    //                    }
    //                }
    //            }
    //        }
    //        PointF[] resultPoints = new PointF[2] {
    //            new PointF(minX, minY), new PointF(maxX, maxY) };
    //        return resultPoints;
    //    }
    //}
}
