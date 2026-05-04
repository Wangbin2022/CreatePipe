using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 洞口信息类 - 封装Revit洞口的几何数据和属性
    /// 使用C# 7.3语法：表达式体成员、元组、out变量、模式匹配等
    /// </summary>
    public class OpeningInfo
    {
        #region 私有字段
        private UIApplication _revit;
        private readonly List<Line3D> _lines = new List<Line3D>();
        private readonly Opening _opening;
        private OpeningProperty _property;
        private WireFrame _sketch;
        private BoundingBox _boundingBox;
        #endregion

        #region 公共属性 - 使用表达式体成员简化
        /// <summary>Revit应用程序引用</summary>
        public UIApplication Revit
        {
            get => _revit;
            set => _revit = value;
        }

        /// <summary>原始Opening元素</summary>
        public Opening Opening => _opening;

        /// <summary>名称和ID的组合字符串</summary>
        public string NameAndId => $"{_opening.Name} ({_opening.Id.IntegerValue})";

        /// <summary>判断是否为竖井洞口 - 使用模式匹配和条件运算符</summary>
        public bool IsShaft => _opening.Category?.Name == "Shaft Openings";

        /// <summary>用于PropertyGrid的属性包装</summary>
        public OpeningProperty Property => _property;

        /// <summary>轮廓线框模型</summary>
        public WireFrame Sketch => _sketch;

        /// <summary>边界框</summary>
        public BoundingBox BoundingBox => _boundingBox;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数 - 使用元组初始化
        /// </summary>
        /// <param name="opening">Revit洞口元素</param>
        /// <param name="app">Revit应用程序</param>
        public OpeningInfo(Opening opening, UIApplication app)
        {
            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
            _revit = app ?? throw new ArgumentNullException(nameof(app));

            // 初始化属性包装
            _property = new OpeningProperty(_opening);

            // 获取边界框 - 使用条件运算符处理null情况
            var activeView = _revit.ActiveUIDocument.Document.ActiveView;
            var boxXyz = _opening.get_BoundingBox(activeView);
            _boundingBox = boxXyz != null ? new BoundingBox(boxXyz) : null;

            // 获取轮廓线
            LoadProfile();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 加载洞口轮廓 - 使用switch表达式(C# 8.0)或条件判断
        /// </summary>
        private void LoadProfile()
        {
            _lines.Clear();

            // 优先获取边界曲线
            var curveArray = _opening.BoundaryCurves;
            if (curveArray != null && curveArray.Size > 0)
            {
                ProcessCurveArray(curveArray);
            }
            // 其次处理矩形边界
            else if (_opening.IsRectBoundary)
            {
                ProcessRectBoundary();
            }
            else
            {
                _sketch = null;
                return;
            }

            // 创建线框模型
            _sketch = new WireFrame(new ReadOnlyCollection<Line3D>(_lines));
        }

        /// <summary>
        /// 处理曲线数组 - 使用foreach和LINQ简化
        /// </summary>
        private void ProcessCurveArray(CurveArray curveArray)
        {
            foreach (Curve curve in curveArray)
            {
                var points = curve.Tessellate()?.ToList();
                if (points?.Any() == true)
                {
                    AddLinesFromPoints(points);
                }
            }
        }

        /// <summary>
        /// 处理矩形边界 - 使用元组解构
        /// </summary>
        private void ProcessRectBoundary()
        {
            var boundRect = _opening.BoundaryRect as List<XYZ>;
            if (boundRect?.Count < 2) return;

            var rectPoints = GetRectangleCorners(boundRect[0], boundRect[1]);
            AddLinesFromPoints(rectPoints);
        }

        /// <summary>
        /// 获取矩形的四个角点（闭合，返回5个点）
        /// </summary>
        private List<XYZ> GetRectangleCorners(XYZ min, XYZ max)
        {
            // 使用元组简化点的创建
            return new List<XYZ>
            {
                new XYZ(min.X, min.Y, min.Z),  // p1
                new XYZ(min.X, min.Y, max.Z),  // p2
                new XYZ(max.X, max.Y, max.Z),  // p3
                new XYZ(max.X, max.Y, min.Z),  // p4
                new XYZ(min.X, min.Y, min.Z)   // 闭合回到p1
            };
        }

        /// <summary>
        /// 从点列表创建线段 - 使用索引器和C# 7.0的out变量
        /// </summary>
        private void AddLinesFromPoints(List<XYZ> points)
        {
            if (points?.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var line = CreateLine3DFromPoints(points[i], points[i + 1]);
                if (line != null)
                {
                    _lines.Add(line);
                }
            }
        }

        /// <summary>
        /// 从两个XYZ点创建Line3D - 使用Vector索引器
        /// </summary>
        private Line3D CreateLine3DFromPoints(XYZ start, XYZ end)
        {
            var line = new Line3D();

            // 使用元组和Vector索引器初始化
            var startVec = new Vector(start.X, start.Y, start.Z);
            var endVec = new Vector(end.X, end.Y, end.Z);

            line.StartPoint = startVec;
            line.EndPoint = endVec;

            return line;
        }
        #endregion
    }
    /// <summary>
    /// 3D线段类 - 使用C# 7.3语法：表达式体成员、条件表达式、nameof
    /// </summary>
    public class Line3D
    {
        private Vector _startPoint;
        private Vector _endPoint;
        private Vector _normal;
        private double _length;

        /// <summary>线段长度</summary>
        public double Length
        {
            get => _length;
            set
            {
                if (Math.Abs(_length - value) < 1e-9) return;
                _length = value;
                CalculateEndPoint();
            }
        }

        /// <summary>线段起点</summary>
        public Vector StartPoint
        {
            get => _startPoint;
            set
            {
                if (_startPoint == value) return;
                _startPoint = value;
                CalculateDirection();
            }
        }

        /// <summary>线段终点</summary>
        public Vector EndPoint
        {
            get => _endPoint;
            set
            {
                if (_endPoint == value) return;
                _endPoint = value;
                CalculateDirection();
            }
        }

        /// <summary>单位方向向量</summary>
        public Vector Normal
        {
            get => _normal;
            set
            {
                if (_normal == value) return;
                _normal = value;
                CalculateEndPoint();
            }
        }

        /// <summary>
        /// 默认构造函数 - 使用表达式体成员初始化
        /// </summary>
        public Line3D()
        {
            _startPoint = new Vector(0, 0, 0);
            _endPoint = new Vector(1, 0, 0);
            _length = 1.0;
            _normal = new Vector(1, 0, 0);
        }

        /// <summary>
        /// 通过起点和终点构造 - 使用元组简化参数
        /// </summary>
        public Line3D(Vector startPoint, Vector endPoint) =>
            (_startPoint, _endPoint) = (startPoint, endPoint);

        /// <summary>
        /// 计算长度（通过起点和终点）
        /// </summary>
        private void CalculateLength() => _length = (_startPoint - _endPoint).GetLength();

        /// <summary>
        /// 计算方向向量和长度（通过起点和终点）
        /// </summary>
        private void CalculateDirection()
        {
            CalculateLength();
            _normal = _length > 1e-9 ? (_endPoint - _startPoint) / _length : new Vector(1, 0, 0);
        }

        /// <summary>
        /// 计算终点（通过起点、长度和方向）
        /// </summary>
        private void CalculateEndPoint() => _endPoint = _startPoint + _normal * _length;
    }
    /// <summary>
    /// 线框模型类 - 处理3D到2D转换和自适应显示
    /// 使用C# 7.3语法：表达式体成员、元组、常量、模式匹配
    /// </summary>
    public class WireFrame : ObjectSketch
    {
        private const float MarginRatio = 0.1f;  // 边距比例

        /// <summary>
        /// 构造函数 - 从3D线段列表构建2D线框
        /// </summary>
        public WireFrame(ReadOnlyCollection<Line3D> line3Ds) => Frame3DTo2D(line3Ds);

        /// <summary>
        /// 绘制2D线框到指定Graphics
        /// </summary>
        public void Draw2D(float previewWidth, float previewHeight, Graphics graphics)
        {
            graphics.Clear(System.Drawing.Color.Black);
            CalculateTransform(previewWidth, previewHeight);

            foreach (var sketch in m_objects)
            {
                sketch.Draw(graphics, m_transform);
            }
        }

        /// <summary>
        /// 基类Draw方法覆盖 - 空实现
        /// </summary>
        public override void Draw(Graphics g, Matrix translate) { }

        /// <summary>
        /// 计算变换矩阵 - 使用条件表达式
        /// </summary>
        private void CalculateTransform(float previewWidth, float previewHeight) =>
            m_transform = new Matrix(BoundingBox, CalculateCanvasRegion(previewWidth, previewHeight));

        /// <summary>
        /// 计算画布显示区域 - 保持宽高比居中
        /// </summary>
        private PointF[] CalculateCanvasRegion(float previewWidth, float previewHeight)
        {
            // 计算去除边距后的实际可用区域
            var (realWidth, realHeight) = (
                previewWidth * (1 - 2 * MarginRatio),
                previewHeight * (1 - 2 * MarginRatio)
            );

            var (minX, minY) = (previewWidth * MarginRatio, previewHeight * MarginRatio);

            var originRate = m_boundingBox.Width / m_boundingBox.Height;
            var displayRate = realWidth / realHeight;

            // 根据宽高比调整显示区域
            var (finalWidth, finalHeight, finalMinX, finalMinY) =
                originRate > displayRate
                    ? AdjustByWidth(realWidth, realHeight, originRate, minX, minY)
                    : AdjustByHeight(realWidth, realHeight, originRate, minX, minY);

            // 返回变换矩形的三个角点（左上、右上、左下）
            return new[]
            {
                new PointF(finalMinX, finalHeight + finalMinY),           // 左上
                new PointF(finalWidth + finalMinX, finalHeight + finalMinY), // 右上
                new PointF(finalMinX, finalMinY)                          // 左下
            };
        }

        /// <summary>
        /// 按宽度调整显示区域 - 使用元组返回
        /// </summary>
        private (float width, float height, float minX, float minY) AdjustByWidth(
            float realWidth, float realHeight, float originRate, float minX, float minY)
        {
            var goalHeight = realWidth / originRate;
            return (realWidth, goalHeight, minX, minY + (realHeight - goalHeight) / 2);
        }

        /// <summary>
        /// 按高度调整显示区域 - 使用元组返回
        /// </summary>
        private (float width, float height, float minX, float minY) AdjustByHeight(
            float realWidth, float realHeight, float originRate, float minX, float minY)
        {
            var goalWidth = realHeight * originRate;
            return (goalWidth, realHeight, minX + (realWidth - goalWidth) / 2, minY);
        }

        /// <summary>
        /// 将3D线段集合转换为2D - 核心投影算法
        /// </summary>
        private void Frame3DTo2D(ReadOnlyCollection<Line3D> line3Ds)
        {
            const double lengthEpsilon = 0.01;
            const double angleEpsilon = 0.1;

            if (line3Ds.Count == 0) return;

            // 查找两个不共线的向量以构建局部坐标系
            var (vector0, vector1) = FindTwoNonCollinearVectors(line3Ds, lengthEpsilon, angleEpsilon);

            if (vector0 == null || vector1 == null) return;

            // 构建局部坐标系（使轮廓平面水平）
            var zAxis = (vector0 & vector1).GetNormal();
            var xAxis = zAxis & new Vector(0, 1, 0);
            var yAxis = zAxis & xAxis;
            var ucs = new UCS(new Vector(0, 0, 0), xAxis, yAxis);

            // 转换所有线段到2D
            TransformLinesTo2D(line3Ds, ucs);
        }

        /// <summary>
        /// 查找两个不共线的向量 - 使用元组和out变量简化
        /// </summary>
        private (Vector vector0, Vector vector1) FindTwoNonCollinearVectors(
            ReadOnlyCollection<Line3D> line3Ds, double lengthEpsilon, double angleEpsilon)
        {
            var firstLine = line3Ds[0];
            Vector vector0 = new Vector();
            Vector vector1 = new Vector();
            int foundIndex = 0;

            // 找第一个有效向量
            for (int i = 1; i < line3Ds.Count; i++)
            {
                var tempVec = line3Ds[i].StartPoint - firstLine.StartPoint;
                if (tempVec.GetLength() > lengthEpsilon)
                {
                    vector0 = tempVec;
                    foundIndex = i;
                    break;
                }
            }

            //if (vector0 == null) return (null, null);

            // 找第二个不共线的向量
            for (int j = foundIndex + 1; j < line3Ds.Count; j++)
            {
                var tempVec = line3Ds[j].StartPoint - line3Ds[foundIndex].StartPoint;
                var angle = Vector.GetAngleOf2Vectors(vector0, tempVec, true);
                if (tempVec.GetLength() > lengthEpsilon && angle > angleEpsilon)
                {
                    vector1 = tempVec;
                    break;
                }
            }

            return (vector0, vector1);
        }

        /// <summary>
        /// 将所有线段转换到2D坐标系
        /// </summary>
        private void TransformLinesTo2D(ReadOnlyCollection<Line3D> line3Ds, UCS ucs)
        {
            bool isFirst = true;

            foreach (var line in line3Ds)
            {
                var transformedLine = ucs.GC2LC(line);
                var start2D = new PointF((float)transformedLine.StartPoint.X, (float)transformedLine.StartPoint.Y);
                var end2D = new PointF((float)transformedLine.EndPoint.X, (float)transformedLine.EndPoint.Y);

                var line2D = new Line2D(start2D, end2D);
                var lineSketch = new LineSketch(line2D);

                // 合并边界框
                m_boundingBox = isFirst
                    ? lineSketch.BoundingBox
                    : RectangleF.Union(m_boundingBox, lineSketch.BoundingBox);

                m_objects.Add(lineSketch);
                isFirst = false;
            }
        }
    }
    /// <summary>
    /// 2D线段类 - 使用C# 7.3语法：表达式体成员、nameof、浮点容差
    /// </summary>
    public class Line2D
    {
        private PointF _startPnt;
        private PointF _endPnt;
        private float _length;
        private PointF _normal;
        private RectangleF _boundingBox;

        /// <summary>边界框（只读）</summary>
        public RectangleF BoundingBox => _boundingBox;

        /// <summary>线段起点</summary>
        public PointF StartPnt
        {
            get => _startPnt;
            set
            {
                if (_startPnt == value) return;
                _startPnt = value;
                CalculateDirection();
                CalculateBoundingBox();
            }
        }

        /// <summary>线段终点</summary>
        public PointF EndPnt
        {
            get => _endPnt;
            set
            {
                if (_endPnt == value) return;
                _endPnt = value;
                CalculateDirection();
                CalculateBoundingBox();
            }
        }

        /// <summary>线段长度</summary>
        public float Length
        {
            get => _length;
            set
            {
                if (Math.Abs(_length - value) < 1e-6f) return;
                _length = value;
                CalculateEndPoint();
                CalculateBoundingBox();
            }
        }

        /// <summary>单位方向向量</summary>
        public PointF Normal
        {
            get => _normal;
            set
            {
                if (_normal == value) return;
                _normal = value;
                CalculateEndPoint();
                CalculateBoundingBox();
            }
        }

        /// <summary>默认构造函数 - 起点(0,0)到终点(1,0)</summary>
        public Line2D()
        {
            _startPnt = new PointF(0, 0);
            _endPnt = new PointF(1, 0);
            CalculateDirection();
            CalculateBoundingBox();
        }

        /// <summary>通过起点和终点构造</summary>
        public Line2D(PointF startPnt, PointF endPnt)
        {
            _startPnt = startPnt;
            _endPnt = endPnt;
            CalculateDirection();
            CalculateBoundingBox();
        }

        /// <summary>计算边界框</summary>
        private void CalculateBoundingBox()
        {
            var minX = Math.Min(_startPnt.X, _endPnt.X);
            var minY = Math.Min(_startPnt.Y, _endPnt.Y);
            var width = Math.Abs(_endPnt.X - _startPnt.X);
            var height = Math.Abs(_endPnt.Y - _startPnt.Y);
            _boundingBox = new RectangleF(minX, minY, width, height);
        }

        /// <summary>计算长度</summary>
        private void CalculateLength()
        {
            var dx = _startPnt.X - _endPnt.X;
            var dy = _startPnt.Y - _endPnt.Y;
            _length = (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>计算方向和长度</summary>
        private void CalculateDirection()
        {
            CalculateLength();
            if (_length < 1e-6f)
            {
                _normal = new PointF(1, 0);
            }
            else
            {
                _normal.X = (_endPnt.X - _startPnt.X) / _length;
                _normal.Y = (_endPnt.Y - _startPnt.Y) / _length;
            }
        }

        /// <summary>通过起点、长度、方向计算终点</summary>
        private void CalculateEndPoint()
        {
            _endPnt.X = _startPnt.X + _length * _normal.X;
            _endPnt.Y = _startPnt.Y + _length * _normal.Y;
        }
    }
    /// <summary>
    /// 2D线段绘制类 - 使用C# 7.3语法：只读字段、表达式体
    /// </summary>
    public class LineSketch : ObjectSketch
    {
        private readonly Line2D _line;

        /// <summary>
        /// 构造函数 - 使用表达式体初始化
        /// </summary>
        public LineSketch(Line2D line)
        {
            _line = line;
            m_boundingBox = line.BoundingBox;
            _pen.Color = System.Drawing.Color.Yellow;
            _pen.Width = 1f;
        }

        /// <summary>
        /// 绘制线段
        /// </summary>
        public override void Draw(Graphics graphics, Matrix transform)
        {
            m_transform = transform;

            using (var path = new GraphicsPath())
            {
                path.AddLine(_line.StartPnt, _line.EndPnt);
                path.Transform(transform);
                graphics.DrawPath(_pen, path);
            }
        }
    }
    /// <summary>
    /// 2D几何图形绘制基类 - 使用C# 7.3语法：表达式体成员、只读自动属性
    /// </summary>
    public abstract class ObjectSketch
    {
        /// <summary>边界框</summary>
        protected RectangleF m_boundingBox = new RectangleF();

        /// <summary>绘制画笔</summary>
        protected Pen _pen = new Pen(System.Drawing.Color.DarkGreen);

        /// <summary>局部几何变换矩阵</summary>
        protected Matrix m_transform;

        /// <summary>子图形对象集合</summary>
        protected List<ObjectSketch> m_objects = new List<ObjectSketch>();

        /// <summary>边界框（只读）</summary>
        public RectangleF BoundingBox => m_boundingBox;

        /// <summary>
        /// 几何对象自绘制方法
        /// </summary>
        /// <param name="graphics">Graphics绘图对象</param>
        /// <param name="transform">变换矩阵</param>
        public abstract void Draw(Graphics graphics, Matrix transform);
    }
    /// <summary>
    /// 洞口属性包装类 - 用于PropertyGrid控件显示
    /// 使用C# 7.3语法：表达式体成员、nameof、条件运算符
    /// </summary>
    public class OpeningProperty
    {
        private readonly string _name;
        private readonly string _elementId;
        private readonly string _hostElementId;
        private readonly string _hostName;
        private readonly bool _isShaft;

        /// <summary>
        /// 构造函数 - 使用条件运算符和表达式体简化
        /// </summary>
        /// <param name="opening">Revit洞口元素</param>
        public OpeningProperty(Opening opening)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));

            _name = opening.Name ?? "Opening";
            _elementId = opening.Id.IntegerValue.ToString();

            // 获取宿主信息 - 使用条件运算符
            _hostName = opening.Host?.Category?.Name ?? "Null";
            _hostElementId = opening.Host?.Id.IntegerValue.ToString() ?? "";

            // 判断是否为竖井洞口
            _isShaft = opening.Category?.Name == "Shaft Openings";
        }

        /// <summary>洞口名称</summary>
        [Description("Name of current displayed Opening")]
        [Category("Opening Name")]
        public string Name => _name;

        /// <summary>洞口元素ID</summary>
        [Description("ElementId of current displayed Opening")]
        [Category("Opening Property")]
        public string ElementID => _elementId;

        /// <summary>宿主名称</summary>
        [Description("Name of the Host which contains Current displayed Opening")]
        [Category("Opening Property")]
        public string HostName => _hostName;

        /// <summary>宿主元素ID</summary>
        [Description("ElementId of Host")]
        [Category("Opening Property")]
        public string HostElementID => _hostElementId;

        /// <summary>是否为竖井洞口</summary>
        [Description("whether displayed opening is Shaft Opening")]
        [Category("Opening Property")]
        public bool ShaftOpening => _isShaft;
    }
    /// <summary>
    /// 边界框扩展类 - 提供8个角点计算和线框创建
    /// 使用C# 7.3语法：表达式体成员、元组、索引器、nameof
    /// </summary>
    public class BoundingBox : BoundingBoxXYZ
    {
        private bool _isCreated;
        private readonly List<XYZ> _points = new List<XYZ>();

        /// <summary>8个角点列表（只读）</summary>
        public IReadOnlyList<XYZ> Points => _points;

        /// <summary>宽度（短边）</summary>
        public double Width => Math.Min(XDistance, YDistance);

        /// <summary>长度（长边）</summary>
        public double Length => Math.Max(XDistance, YDistance);

        private double XDistance => _points[5].X - _points[2].X;
        private double YDistance => _points[2].Y - _points[1].Y;

        /// <summary>
        /// 构造函数 - 从现有BoundingBoxXYZ创建
        /// </summary>
        public BoundingBox(BoundingBoxXYZ source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Min = source.Min;
            Max = source.Max;
            ComputeCorners();
        }

        /// <summary>
        /// 创建12条边线构成立方体线框
        /// </summary>
        public void CreateLines(UIApplication app)
        {
            if (_isCreated) return;

            var doc = app.ActiveUIDocument.Document;

            // 使用元组数组定义12条边的连接关系
            var edges = new (int start, int end)[]
            {
                // 顶面4条边 (0-3)
                (0, 1), (1, 2), (2, 3), (3, 0),
                // 底面4条边 (4-7)
                (4, 5), (5, 6), (6, 7), (7, 4),
                // 垂直4条边
                (0, 4), (1, 5), (2, 6), (3, 7)
            };

            foreach (var (start, end) in edges)
            {
                CreateModelLine(app, doc, start, end);
            }

            _isCreated = true;
        }

        /// <summary>
        /// 计算8个角点坐标 - 使用集合初始化器
        /// </summary>
        private void ComputeCorners()
        {
            var min = Min;
            var max = Max;

            _points.Clear();
            _points.AddRange(new[]
            {
                new XYZ(min.X, min.Y, max.Z), // 0
                new XYZ(min.X, max.Y, max.Z), // 1
                new XYZ(max.X, max.Y, max.Z), // 2
                new XYZ(max.X, min.Y, max.Z), // 3
                new XYZ(min.X, min.Y, min.Z), // 4
                new XYZ(min.X, max.Y, min.Z), // 5
                new XYZ(max.X, max.Y, min.Z), // 6
                new XYZ(max.X, min.Y, min.Z)  // 7
            });
        }

        /// <summary>
        /// 创建草图平面 - 使用switch表达式(C# 8.0)
        /// </summary>
        private SketchPlane CreateSketchPlaneForLine(Autodesk.Revit.DB.Line line, UIApplication app)
        {
            var p1 = line.GetEndPoint(0);
            var p2 = line.GetEndPoint(1);

            // 根据线段方向确定法线
            var normal = Math.Abs(p1.X - p2.X) < 1e-9 ? new XYZ(1, 0, 0) :
                         Math.Abs(p1.Y - p2.Y) < 1e-9 ? new XYZ(0, 1, 0) :
                         new XYZ(0, 0, 1);

            var plane = Plane.CreateByNormalAndOrigin(normal, p1);
            return SketchPlane.Create(app.ActiveUIDocument.Document, plane);
        }

        /// <summary>
        /// 创建单条模型线
        /// </summary>
        private void CreateModelLine(UIApplication app, Document doc, int startIdx, int endIdx)
        {
            try
            {
                var start = _points[startIdx];
                var end = _points[endIdx];
                var line = Autodesk.Revit.DB.Line.CreateBound(start, end);
                var sketchPlane = CreateSketchPlaneForLine(line, app);
                doc.Create.NewModelCurve(line, sketchPlane);
            }
            catch
            {
                // 忽略创建失败的线（如零长度线段）
            }
        }
    }
    /// <summary>
    /// 三维向量结构体 - 使用C# 7.3语法：表达式体成员、元组、nameof
    /// </summary>
    public struct Vector
    {
        private double _x;
        private double _y;
        private double _z;

        public double X { get => _x; set => _x = value; }
        public double Y { get => _y; set => _y = value; }
        public double Z { get => _z; set => _z = value; }

        /// <summary>索引器 - 使用传统方式保持兼容</summary>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return _x;
                    case 1:
                        return _y;
                    case 2:
                        return _z;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        _x = value;
                        break;
                    case 1:
                        _y = value;
                        break;
                    case 2:
                        _z = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        /// <summary>复制构造函数 - 使用表达式体</summary>
        public Vector(Vector rhs) => (_x, _y, _z) = (rhs._x, rhs._y, rhs._z);

        /// <summary>构造函数 - 使用元组赋值</summary>
        public Vector(double x, double y, double z) => (_x, _y, _z) = (x, y, z);

        /// <summary>获取单位向量</summary>
        public Vector GetNormal()
        {
            double len = GetLength();
            return len < 1e-9 ? new Vector(0, 0, 0) : new Vector(_x / len, _y / len, _z / len);
        }

        /// <summary>获取向量长度</summary>
        public double GetLength() => Math.Sqrt(_x * _x + _y * _y + _z * _z);

        // 运算符重载 - 使用表达式体
        public static Vector operator +(Vector lhs, Vector rhs) =>
            new Vector(lhs._x + rhs._x, lhs._y + rhs._y, lhs._z + rhs._z);

        public static Vector operator -(Vector lhs, Vector rhs) =>
            new Vector(lhs._x - rhs._x, lhs._y - rhs._y, lhs._z - rhs._z);

        public static Vector operator -(Vector lhs) =>
            new Vector(-lhs._x, -lhs._y, -lhs._z);

        /// <summary>叉积运算符 &</summary>
        public static Vector operator &(Vector lhs, Vector rhs) =>
            new Vector(
                lhs._y * rhs._z - lhs._z * rhs._y,
                lhs._z * rhs._x - lhs._x * rhs._z,
                lhs._x * rhs._y - lhs._y * rhs._x);

        /// <summary>点积运算符 *</summary>
        public static double operator *(Vector lhs, Vector rhs) =>
            lhs._x * rhs._x + lhs._y * rhs._y + lhs._z * rhs._z;

        /// <summary>标量乘法</summary>
        public static Vector operator *(Vector lhs, double rhs) =>
            new Vector(lhs._x * rhs, lhs._y * rhs, lhs._z * rhs);

        /// <summary>标量除法</summary>
        public static Vector operator /(Vector lhs, double rhs) =>
            new Vector(lhs._x / rhs, lhs._y / rhs, lhs._z / rhs);

        /// <summary>长度运算符 ~</summary>
        public static double operator ~(Vector lhs) => lhs.GetLength();

        public static bool operator ==(Vector lhs, Vector rhs) => IsEqual(lhs, rhs);
        public static bool operator !=(Vector lhs, Vector rhs) => !IsEqual(lhs, rhs);

        /// <summary>计算两向量夹角</summary>
        public static double GetAngleOf2Vectors(Vector lhs, Vector rhs, bool acuteAngleDesired)
        {
            double angle = Math.Acos(lhs.GetNormal() * rhs.GetNormal());
            return acuteAngleDesired && angle > Math.PI / 2 ? Math.PI - angle : angle;
        }

        public override bool Equals(object obj) =>
            obj is Vector other && IsEqual(this, other);

        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode() ^ _z.GetHashCode();
        }

        /// <summary>判断两向量是否相等 - 使用浮点容差</summary>
        private static bool IsEqual(Vector lhs, Vector rhs) =>
            Math.Abs(lhs._x - rhs._x) < 1e-9 &&
            Math.Abs(lhs._y - rhs._y) < 1e-9 &&
            Math.Abs(lhs._z - rhs._z) < 1e-9;
    }
    /// <summary>
    /// 用户坐标系类 - 提供坐标变换功能
    /// 使用C# 7.3语法：表达式体成员、nameof、元组
    /// </summary>
    public class UCS
    {
        private readonly Vector _origin;
        private readonly Vector _xAxis;
        private readonly Vector _yAxis;
        private readonly Vector _zAxis;

        public Vector Origin => _origin;
        public Vector XAxis => _xAxis;
        public Vector YAxis => _yAxis;
        public Vector ZAxis => _zAxis;

        /// <summary>
        /// 构造函数 - 默认右手系
        /// </summary>
        public UCS(Vector origin, Vector xAxis, Vector yAxis)
            : this(origin, xAxis, yAxis, true)
        {
        }

        /// <summary>
        /// 构造函数 - 可指定左手/右手系
        /// </summary>
        public UCS(Vector origin, Vector xAxis, Vector yAxis, bool isRightHanded)
        {
            var xNorm = xAxis / ~xAxis;      // 归一化X轴
            var yNorm = yAxis / ~yAxis;      // 归一化Y轴
            var zNorm = xNorm & yNorm;       // 叉积计算Z轴

            if (~zNorm < double.Epsilon)
                throw new InvalidOperationException("Cannot create UCS: axes are collinear");

            // 左手系时反转Z轴
            if (!isRightHanded) zNorm = -zNorm;

            (_origin, _xAxis, _yAxis, _zAxis) = (origin, xNorm, yNorm, zNorm);
        }

        /// <summary>
        /// 局部坐标转全局坐标
        /// </summary>
        public Vector LC2GC(Vector local)
        {
            return new Vector(
                local.X * _xAxis.X + local.Y * _yAxis.X + local.Z * _zAxis.X + _origin.X,
                local.X * _xAxis.Y + local.Y * _yAxis.Y + local.Z * _zAxis.Y + _origin.Y,
                local.X * _xAxis.Z + local.Y * _yAxis.Z + local.Z * _zAxis.Z + _origin.Z
            );
        }

        /// <summary>
        /// 全局坐标转局部坐标
        /// </summary>
        public Vector GC2LC(Vector global)
        {
            var offset = global - _origin;
            return new Vector(
                _xAxis * offset,
                _yAxis * offset,
                _zAxis * offset
            );
        }

        /// <summary>
        /// 将3D线段转换到局部坐标系
        /// </summary>
        public Line3D GC2LC(Line3D line) =>
            new Line3D(GC2LC(line.StartPoint), GC2LC(line.EndPoint));
    }
}
