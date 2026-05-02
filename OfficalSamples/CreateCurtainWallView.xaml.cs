using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Point = Autodesk.Revit.DB.Point;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CreateCurtainWallView.xaml 的交互逻辑
    /// </summary>
    public partial class CreateCurtainWallView : Window
    {
        public CreateCurtainWallView(ExternalCommandData commandData)
        {
            InitializeComponent();
            this.DataContext = new CreateCurtainWallViewModel(commandData);
        }
    }
    /// <summary>
    /// 主视图模型 - 管理整个应用程序的状态和操作
    /// </summary>
    public class CreateCurtainWallViewModel : ObserverableObject
    {
        #region 私有字段
        private ExternalCommandData _commandData;
        private Document _document;
        private UIDocument _uiDocument;
        private Wall _curtainWall;
        private GridGeometry _gridGeometry;
        private bool _wallCreated;
        private int _selectedUIndex = -1;
        private int _selectedVIndex = -1;
        private int _selectedUSegmentIndex = -1;
        private int _selectedVSegmentIndex = -1;
        private string _operationStatus = "就绪";
        #endregion

        #region 公开属性
        public ObservableCollection<WallType> WallTypes { get; } = new ObservableCollection<WallType>();
        public ObservableCollection<ViewPlan> Views { get; } = new ObservableCollection<ViewPlan>();
        public ObservableCollection<string> LineOperations { get; } = new ObservableCollection<string>();

        private WallType _selectedWallType;
        public WallType SelectedWallType { get => _selectedWallType; set => SetProperty(ref _selectedWallType, value); }

        private ViewPlan _selectedView;
        public ViewPlan SelectedView { get => _selectedView; set => SetProperty(ref _selectedView, value); }

        private string _selectedLineOperation;
        public string SelectedLineOperation { get => _selectedLineOperation; set => SetProperty(ref _selectedLineOperation, value); }

        public GridPropertiesModel GridProperties { get; } = new GridPropertiesModel();

        private DrawingVisual _curtainWallDrawing;
        public DrawingVisual CurtainWallDrawing { get => _curtainWallDrawing; set => SetProperty(ref _curtainWallDrawing, value); }

        private DrawingVisual _curtainGridDrawing;
        public DrawingVisual CurtainGridDrawing { get => _curtainGridDrawing; set => SetProperty(ref _curtainGridDrawing, value); }

        public string OperationStatus { get => _operationStatus; set => SetProperty(ref _operationStatus, value); }
        public bool WallCreated { get => _wallCreated; set => SetProperty(ref _wallCreated, value); }
        #endregion

        #region 命令
        public ICommand CreateWallCommand { get; }
        public ICommand ClearBaselineCommand { get; }
        public ICommand AddUGridLineCommand { get; }
        public ICommand AddVGridLineCommand { get; }
        public ICommand RemoveSegmentCommand { get; }
        public ICommand AddSegmentCommand { get; }
        public ICommand AddAllSegmentsCommand { get; }
        public ICommand LockUnlockGridLineCommand { get; }
        public ICommand AddAllMullionsCommand { get; }
        public ICommand DeleteAllMullionsCommand { get; }
        public ICommand ReloadDataCommand { get; }
        public ICommand ExitCommand { get; }
        #endregion

        public CreateCurtainWallViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _uiDocument = commandData.Application.ActiveUIDocument;
            _document = _uiDocument.Document;
            _gridGeometry = new GridGeometry();

            // 初始化命令
            CreateWallCommand = new BaseBindingCommand(CreateCurtainWall, _ => SelectedWallType != null && SelectedView != null);
            ClearBaselineCommand = new BaseBindingCommand(ClearBaseline);
            AddUGridLineCommand = new BaseBindingCommand(AddUGridLine);
            AddVGridLineCommand = new BaseBindingCommand(AddVGridLine);
            RemoveSegmentCommand = new BaseBindingCommand(RemoveSegment);
            AddSegmentCommand = new BaseBindingCommand(AddSegment);
            AddAllSegmentsCommand = new BaseBindingCommand(AddAllSegments);
            LockUnlockGridLineCommand = new BaseBindingCommand(LockOrUnlockGridLine);
            AddAllMullionsCommand = new BaseBindingCommand(AddAllMullions);
            DeleteAllMullionsCommand = new BaseBindingCommand(DeleteAllMullions);
            ReloadDataCommand = new BaseBindingCommand(ReloadGeometryData);
            //ExitCommand = new BaseBindingCommand(_ => Application.Current.Windows[0]?.Close());

            // 初始化操作类型列表
            //LineOperations.AddRange(new List<string> { "等待操作", "添加水平网格线", "添加垂直网格线", "删除线段", "添加线段", "恢复所有线段", "锁定/解锁网格线" });
            LineOperations.Add("等待操作");
            LineOperations.Add("添加水平网格线");
            LineOperations.Add("添加垂直网格线");
            LineOperations.Add("删除线段");
            LineOperations.Add("添加线段");
            LineOperations.Add("恢复所有线段");
            LineOperations.Add("锁定/解锁网格线");

            LoadInitialData();
        }

        /// <summary>
        /// 加载初始数据：幕墙类型、视图平面
        /// </summary>
        private void LoadInitialData()
        {
            // 获取所有幕墙类型
            var collector = new FilteredElementCollector(_document);
            var wallTypes = collector.OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => wt.Kind == WallKind.Curtain)
                .OrderBy(wt => wt.Name);
            foreach (var wt in wallTypes) WallTypes.Add(wt);

            // 获取所有非模板视图平面
            var views = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate)
                .OrderBy(v => v.Name);
            foreach (var v in views) Views.Add(v);
        }

        /// <summary>
        /// 创建幕墙
        /// </summary>
        private void CreateCurtainWall(Object obj)
        {
            try
            {
                using (var trans = new Transaction(_document, "创建幕墙"))
                {
                    trans.Start();
                    // 创建基线
                    var startPoint = new XYZ(0, 0, 0);
                    var endPoint = new XYZ(20, 0, 0);
                    var line = Line.CreateBound(startPoint, endPoint);

                    _curtainWall = Wall.Create(_document, line, SelectedWallType.Id,
                        SelectedView.GenLevel.Id, 20, 0, false, false);
                    trans.Commit();
                }
                WallCreated = true;
                OperationStatus = "幕墙创建成功";
                ReloadGeometryData(null);
            }
            catch (Exception ex)
            {
                OperationStatus = $"创建失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 重新加载网格几何数据
        /// </summary>
        private void ReloadGeometryData(Object obj)
        {
            if (_curtainWall?.CurtainGrid == null) return;

            _gridGeometry.LoadFromCurtainGrid(_curtainWall.CurtainGrid, _curtainWall.Document);
            LoadGridProperties();
            RenderCurtainGrid();
        }

        /// <summary>
        /// 加载网格属性
        /// </summary>
        private void LoadGridProperties()
        {
            var grid = _curtainWall.CurtainGrid;
            GridProperties.HorizontalJustification = grid.Grid2Justification.ToString();
            GridProperties.HorizontalAngle = grid.Grid2Angle * 180 / Math.PI;
            GridProperties.HorizontalOffset = grid.Grid2Offset;
            GridProperties.HorizontalLinesCount = grid.NumULines;
            GridProperties.VerticalJustification = grid.Grid1Justification.ToString();
            GridProperties.VerticalAngle = grid.Grid1Angle * 180 / Math.PI;
            GridProperties.VerticalOffset = grid.Grid1Offset;
            GridProperties.VerticalLinesCount = grid.NumVLines;
            GridProperties.PanelCount = grid.NumPanels;
            GridProperties.CellCount = grid.GetCurtainCells().Count;
        }

        /// <summary>
        /// 渲染幕墙网格（WPF绘制逻辑）
        /// </summary>
        private void RenderCurtainGrid()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var pen = new System.Windows.Media.Pen(Brushes.Blue, 1);
                // 绘制U方向网格线
                foreach (var line in _gridGeometry.UGridLines2D)
                {
                    var start = new System.Windows.Point(line.StartPoint.Coord.X, line.StartPoint.Coord.Y);
                    var end = new System.Windows.Point(line.EndPoint.Coord.X, line.EndPoint.Coord.Y);
                    //var start = new System.Windows.Point(line.StartPoint..X, line.StartPoint.Y);
                    //var end = new System.Windows.Point(line.EndPoint.X, line.EndPoint.Y);
                    dc.DrawLine(pen, start, end);
                }
                // 绘制V方向网格线
                foreach (var line in _gridGeometry.VGridLines2D)
                {
                    var start = new System.Windows.Point(line.StartPoint.Coord.X, line.StartPoint.Coord.Y);
                    var end = new System.Windows.Point(line.EndPoint.Coord.X, line.EndPoint.Coord.Y); 
                    //var start = new System.Windows.Point(line.StartPoint.X, line.StartPoint.Y);
                    //var end = new System.Windows.Point(line.EndPoint.X, line.EndPoint.Y);
                    dc.DrawLine(pen, start, end);
                }
            }
            CurtainGridDrawing = visual;
        }

        private void ClearBaseline(Object obj) => OperationStatus = "基线已清除";
        private void AddUGridLine(Object obj) => OperationStatus = "添加水平网格线";
        private void AddVGridLine(Object obj) => OperationStatus = "添加垂直网格线";
        private void RemoveSegment(Object obj) => OperationStatus = "删除线段";
        private void AddSegment(Object obj) => OperationStatus = "添加线段";
        private void AddAllSegments(Object obj) => OperationStatus = "恢复所有线段";
        private void LockOrUnlockGridLine(Object obj) => OperationStatus = "切换锁定状态";
        private void AddAllMullions(Object obj) => OperationStatus = "添加所有竖梃";
        private void DeleteAllMullions(Object obj) => OperationStatus = "删除所有竖梃";
    }

    /// <summary>
    /// 网格几何数据管理类 - 存储2D投影数据
    /// </summary>
    public class GridGeometry
    {
        public List<GridLine2DModel> UGridLines2D { get; } = new List<GridLine2DModel>();
        public List<GridLine2DModel> VGridLines2D { get; } = new List<GridLine2DModel>();
        public List<Line2DModel> BoundLines2D { get; } = new List<Line2DModel>();

        public void LoadFromCurtainGrid(CurtainGrid grid, Document document)
        {
            UGridLines2D.Clear();
            VGridLines2D.Clear();
            BoundLines2D.Clear();

            // 加载U方向网格线
            foreach (var lineId in grid.GetUGridLineIds())
            {
                var line = document.GetElement(lineId) as CurtainGridLine;
                if (line != null) UGridLines2D.Add(ConvertTo2D(line));
            }

            // 加载V方向网格线
            foreach (var lineId in grid.GetVGridLineIds())
            {
                var line = document.GetElement(lineId) as CurtainGridLine;
                if (line != null) VGridLines2D.Add(ConvertTo2D(line));
            }
        }

        /// <summary>
        /// 将Revit曲线转换为2D线段模型
        /// </summary>
        private GridLine2DModel ConvertTo2D(CurtainGridLine line)
        {
            var curve = line.FullCurve;
            var start = curve.GetEndPoint(0);
            var end = curve.GetEndPoint(1);

            return new GridLine2DModel
            {
                //StartPoint = new Point((int)start.X, (int)start.Y),
                //EndPoint = new Point((int)end.X, (int)end.Y),
                StartPoint = Point.Create(new XYZ((int)start.X, (int)start.Y, 0)),
                EndPoint = Point.Create(new XYZ((int)end.X, (int)end.Y, 0)),
                IsLocked = line.Lock,
                IsUGridLine = line.IsUGridLine
            };
        }
    }

    /// <summary>
    /// 二维线段数据模型
    /// </summary>
    public class Line2DModel : ObserverableObject
    {
        private Point _startPoint;
        private Point _endPoint;

        public Point StartPoint { get => _startPoint; set => SetProperty(ref _startPoint, value); }
        public Point EndPoint { get => _endPoint; set => SetProperty(ref _endPoint, value); }

        public Line2DModel() { }
        public Line2DModel(Point start, Point end) => (StartPoint, EndPoint) = (start, end);
    }

    /// <summary>
    /// 网格线段模型
    /// </summary>
    public class SegmentLine2DModel : Line2DModel
    {
        private bool _isRemoved;      // 是否已删除
        private bool _isIsolated;     // 是否孤立
        private int _segmentIndex;    // 线段索引
        private int _gridLineIndex;   // 所属网格线索引
        private bool _isUSegment;     // 是否为U方向线段

        public bool IsRemoved { get => _isRemoved; set => SetProperty(ref _isRemoved, value); }
        public bool IsIsolated { get => _isIsolated; set => SetProperty(ref _isIsolated, value); }
        public int SegmentIndex { get => _segmentIndex; set => SetProperty(ref _segmentIndex, value); }
        public int GridLineIndex { get => _gridLineIndex; set => SetProperty(ref _gridLineIndex, value); }
        public bool IsUSegment { get => _isUSegment; set => SetProperty(ref _isUSegment, value); }
    }

    /// <summary>
    /// 网格线模型
    /// </summary>
    public class GridLine2DModel : Line2DModel
    {
        private bool _isLocked;           // 是否锁定
        private int _removedCount;        // 已删除线段数量
        private bool _isUGridLine;        // 是否为U方向网格线
        private List<SegmentLine2DModel> _segments;

        public bool IsLocked { get => _isLocked; set => SetProperty(ref _isLocked, value); }
        public int RemovedCount { get => _removedCount; set => SetProperty(ref _removedCount, value); }
        public bool IsUGridLine { get => _isUGridLine; set => SetProperty(ref _isUGridLine, value); }
        public List<SegmentLine2DModel> Segments { get => _segments; set => SetProperty(ref _segments, value); }

        public GridLine2DModel() => _segments = new List<SegmentLine2DModel>();
    }

    /// <summary>
    /// 网格属性模型
    /// </summary>
    public class GridPropertiesModel : ObserverableObject
    {
        private string _horizontalJustification;
        private double _horizontalAngle;
        private double _horizontalOffset;
        private int _horizontalLinesCount;
        private string _verticalJustification;
        private double _verticalAngle;
        private double _verticalOffset;
        private int _verticalLinesCount;
        private int _panelCount;
        private int _cellCount;

        public string HorizontalJustification { get => _horizontalJustification; set => SetProperty(ref _horizontalJustification, value); }
        public double HorizontalAngle { get => _horizontalAngle; set => SetProperty(ref _horizontalAngle, value); }
        public double HorizontalOffset { get => _horizontalOffset; set => SetProperty(ref _horizontalOffset, value); }
        public int HorizontalLinesCount { get => _horizontalLinesCount; set => SetProperty(ref _horizontalLinesCount, value); }
        public string VerticalJustification { get => _verticalJustification; set => SetProperty(ref _verticalJustification, value); }
        public double VerticalAngle { get => _verticalAngle; set => SetProperty(ref _verticalAngle, value); }
        public double VerticalOffset { get => _verticalOffset; set => SetProperty(ref _verticalOffset, value); }
        public int VerticalLinesCount { get => _verticalLinesCount; set => SetProperty(ref _verticalLinesCount, value); }
        public int PanelCount { get => _panelCount; set => SetProperty(ref _panelCount, value); }
        public int CellCount { get => _cellCount; set => SetProperty(ref _cellCount, value); }
    }


}
