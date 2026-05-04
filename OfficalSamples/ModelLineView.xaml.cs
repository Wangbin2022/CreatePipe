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
    /// ModelLineView.xaml 的交互逻辑
    /// </summary>
    public partial class ModelLineView : Window
    {
        private readonly ModelLineViewModel _viewModel;
        public ModelLineView(UIApplication uiApp)
        {
            InitializeComponent();
            // 添加类型枚举作为静态资源
            //Resources.Add("LineTypes", Enum.GetValues(typeof(ModelLine)));

            _viewModel = new ModelLineViewModel(uiApp);
            DataContext = _viewModel;

            Loaded += (s, e) => _viewModel.LoadDataCommand.Execute(null);
        }
    }
    /// <summary>
    /// 主视图模型
    /// </summary>
    public class ModelLineViewModel : ObserverableObject
    {
        private readonly ModelLineService _service;
        private ObservableCollection<ModelLineStatistic> _statistics;
        private ObservableCollection<SketchPlaneInfo> _sketchPlanes;
        private ObservableCollection<CurveElementInfo> _availableCurves;

        private ModelLineType _selectedLineType;
        private SketchPlaneInfo _selectedSketchPlane;
        private CurveElementInfo _selectedCurve;

        private XYZPoint _lineStart;
        private XYZPoint _lineEnd;
        private XYZPoint _arcStart;
        private XYZPoint _arcEnd;
        private XYZPoint _arcPoint;
        private XYZPoint _offsetVector;

        private string _statusMessage;
        private bool _isProcessing;

        public ModelLineViewModel(UIApplication uiApp)
        {
            _service = new ModelLineService(uiApp);

            // 初始化默认值
            _lineStart = new XYZPoint();
            _lineEnd = new XYZPoint { X = 10, Y = 0, Z = 0 };
            _arcStart = new XYZPoint();
            _arcEnd = new XYZPoint { X = 10, Y = 0, Z = 0 };
            _arcPoint = new XYZPoint { X = 5, Y = 5, Z = 0 };
            _offsetVector = new XYZPoint { X = 5, Y = 5, Z = 0 };

            _selectedLineType = ModelLineType.Line;

            // 初始化命令
            LoadDataCommand = new BaseBindingCommand(_ => LoadData());
            CreateSketchPlaneCommand = new BaseBindingCommand(_ => CreateSketchPlane());
            CreateModelLineCommand = new BaseBindingCommand(_ => CreateModelLine(), _ => CanCreate());
            RefreshCommand = new BaseBindingCommand(_ => LoadData());

            // 加载初始数据
            LoadData();
        }

        public ObservableCollection<ModelLineStatistic> Statistics
        {
            get => _statistics;
            set { _statistics = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SketchPlaneInfo> SketchPlanes
        {
            get => _sketchPlanes;
            set { _sketchPlanes = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CurveElementInfo> AvailableCurves
        {
            get => _availableCurves;
            set { _availableCurves = value; OnPropertyChanged(); }
        }

        public ModelLineType SelectedLineType
        {
            get => _selectedLineType;
            set
            {
                _selectedLineType = value;
                OnPropertyChanged();
                // 切换类型时刷新可用曲线列表
                LoadCurvesForCurrentType();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public SketchPlaneInfo SelectedSketchPlane
        {
            get => _selectedSketchPlane;
            set { _selectedSketchPlane = value; OnPropertyChanged(); }
        }

        public CurveElementInfo SelectedCurve
        {
            get => _selectedCurve;
            set { _selectedCurve = value; OnPropertyChanged(); }
        }

        // 直线参数
        public XYZPoint LineStart { get => _lineStart; set { _lineStart = value; OnPropertyChanged(); } }
        public XYZPoint LineEnd { get => _lineEnd; set { _lineEnd = value; OnPropertyChanged(); } }

        // 圆弧参数
        public XYZPoint ArcStart { get => _arcStart; set { _arcStart = value; OnPropertyChanged(); } }
        public XYZPoint ArcEnd { get => _arcEnd; set { _arcEnd = value; OnPropertyChanged(); } }
        public XYZPoint ArcPoint { get => _arcPoint; set { _arcPoint = value; OnPropertyChanged(); } }

        // 偏移向量
        public XYZPoint OffsetVector { get => _offsetVector; set { _offsetVector = value; OnPropertyChanged(); } }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // 界面显示控制属性
        public bool IsLineOrArcSelected =>
            SelectedLineType == ModelLineType.Line || SelectedLineType == ModelLineType.Arc;

        public bool IsComplexCurveSelected =>
            SelectedLineType == ModelLineType.Ellipse ||
            SelectedLineType == ModelLineType.HermiteSpline ||
            SelectedLineType == ModelLineType.NurbSpline;

        public string CreationTitle
        {
            get
            {
                switch (SelectedLineType)
                {
                    case ModelLineType.Line:
                        return "创建直线";
                    case ModelLineType.Arc:
                        return "创建圆弧";
                    case ModelLineType.Ellipse:
                        return "创建椭圆";
                    case ModelLineType.HermiteSpline:
                        return "创建埃尔米特样条曲线";
                    case ModelLineType.NurbSpline:
                        return "创建NURBS样条曲线";
                    default:
                        return "创建模型线";
                }
            }
        }

        public ICommand LoadDataCommand { get; }
        public ICommand CreateSketchPlaneCommand { get; }
        public ICommand CreateModelLineCommand { get; }
        public ICommand RefreshCommand { get; }

        private void LoadData()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "正在加载数据...";

                // 加载统计数据
                var stats = _service.GetModelLineStatistics();
                Statistics = new ObservableCollection<ModelLineStatistic>(stats);

                // 加载草图平面
                var planes = _service.GetSketchPlanes();
                SketchPlanes = new ObservableCollection<SketchPlaneInfo>(planes);

                if (SketchPlanes.Any())
                    SelectedSketchPlane = SketchPlanes.First();

                // 加载当前类型的曲线
                LoadCurvesForCurrentType();

                StatusMessage = $"加载完成 - 共找到 {stats.Sum(s => s.Count)} 条模型线";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void LoadCurvesForCurrentType()
        {
            string curveType;
            switch (SelectedLineType)
            {
                case ModelLineType.Ellipse:
                    curveType = "ModelEllipse";
                    break;
                case ModelLineType.HermiteSpline:
                    curveType = "ModelHermiteSpline";
                    break;
                case ModelLineType.NurbSpline:
                    curveType = "ModelNurbSpline";
                    break;
                default:
                    curveType = null;
                    break;
            }
            if (curveType != null)
            {
                var curves = _service.GetCurveElementsByType(curveType);
                AvailableCurves = new ObservableCollection<CurveElementInfo>(curves);
                SelectedCurve = AvailableCurves.FirstOrDefault();
            }
            else
            {
                AvailableCurves = new ObservableCollection<CurveElementInfo>();
                SelectedCurve = null;
            }
        }

        private void CreateSketchPlane()
        {
            var dialog = new SketchPlaneDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsProcessing = true;
                    var plane = _service.CreateSketchPlane(
                        dialog.CreationParams.Normal.ToXYZ(),
                        dialog.CreationParams.Origin.ToXYZ());

                    // 刷新草图平面列表
                    LoadData();
                    StatusMessage = $"成功创建草图平面，ID: {plane.Id.IntegerValue}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"创建草图平面失败: {ex.Message}";
                    MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        private bool CanCreate() => SelectedSketchPlane != null && !IsProcessing;

        private void CreateModelLine()
        {
            try
            {
                IsProcessing = true;

                switch (SelectedLineType)
                {
                    case ModelLineType.Line:
                        var line = _service.CreateLine(
                            SelectedSketchPlane.Id,
                            LineStart.ToXYZ(),
                            LineEnd.ToXYZ());
                        StatusMessage = $"成功创建直线，ID: {line.Id.IntegerValue}";
                        break;

                    case ModelLineType.Arc:
                        var arc = _service.CreateArc(
                            SelectedSketchPlane.Id,
                            ArcStart.ToXYZ(),
                            ArcEnd.ToXYZ(),
                            ArcPoint.ToXYZ());
                        StatusMessage = $"成功创建圆弧，ID: {arc.Id.IntegerValue}";
                        break;

                    case ModelLineType.Ellipse:
                    case ModelLineType.HermiteSpline:
                    case ModelLineType.NurbSpline:
                        if (SelectedCurve == null)
                        {
                            StatusMessage = "请选择要复制的源曲线";
                            return;
                        }
                        var curve = _service.CreateFromExisting(
                            SelectedSketchPlane.Id,
                            SelectedCurve.Id,
                            OffsetVector.ToXYZ());
                        StatusMessage = $"成功创建 {SelectedLineType}，ID: {curve.Id.IntegerValue}";
                        break;
                }

                // 刷新统计数据
                LoadData();
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }

    /// <summary>
    /// Revit模型线服务 - 负责所有Revit API交互
    /// </summary>
    public class ModelLineService
    {
        private readonly UIApplication _uiApp;
        private readonly Document _doc;
        private readonly Autodesk.Revit.Creation.Document _createDoc;

        public ModelLineService(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _doc = uiApp.ActiveUIDocument.Document;
            _createDoc = _doc.Create;
        }

        /// <summary>
        /// 获取所有模型线的统计信息
        /// </summary>
        public List<ModelLineStatistic> GetModelLineStatistics()
        {
            var stats = new Dictionary<string, int>
            {
                ["ModelLine"] = 0,
                ["ModelArc"] = 0,
                ["ModelEllipse"] = 0,
                ["ModelHermiteSpline"] = 0,
                ["ModelNurbSpline"] = 0
            };

            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(CurveElement))
                .Cast<CurveElement>();

            foreach (var curveElement in collector)
            {
                var typeName = curveElement.GetType().Name;
                if (stats.ContainsKey(typeName))
                    stats[typeName]++;
            }

            return stats.Select(s => new ModelLineStatistic
            {
                TypeName = s.Key,
                Count = s.Value
            }).ToList();
        }

        /// <summary>
        /// 获取所有草图平面
        /// </summary>
        public List<SketchPlaneInfo> GetSketchPlanes()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(SketchPlane))
                .Cast<SketchPlane>()
                .Select(sp => new SketchPlaneInfo
                {
                    Id = sp.Id.IntegerValue,
                    DisplayName = $"SketchPlane : {sp.Id.IntegerValue}"
                })
                .ToList();
        }

        /// <summary>
        /// 获取指定类型的曲线元素列表
        /// </summary>
        public List<CurveElementInfo> GetCurveElementsByType(string curveType)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(CurveElement))
                .Cast<CurveElement>()
                .Where(e => e.GetType().Name == curveType)
                .Select(e => new CurveElementInfo
                {
                    Id = e.Id.IntegerValue,
                    CurveType = curveType,
                    DisplayName = $"{curveType} : {e.Id.IntegerValue}"
                })
                .ToList();
        }

        /// <summary>
        /// 创建草图平面
        /// </summary>
        public SketchPlane CreateSketchPlane(XYZ normal, XYZ origin)
        {
            var plane = Plane.CreateByNormalAndOrigin(normal, origin);
            return SketchPlane.Create(_doc, plane);
        }

        /// <summary>
        /// 创建直线模型线
        /// </summary>
        public ModelLine CreateLine(int sketchPlaneId, XYZ startPoint, XYZ endPoint)
        {
            var sketchPlane = GetSketchPlaneById(sketchPlaneId);
            var line = Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint);
            return _createDoc.NewModelCurve(line, sketchPlane) as ModelLine;
        }

        /// <summary>
        /// 创建圆弧模型线
        /// </summary>
        public ModelArc CreateArc(int sketchPlaneId, XYZ startPoint, XYZ endPoint, XYZ pointOnArc)
        {
            var sketchPlane = GetSketchPlaneById(sketchPlaneId);
            var arc = Arc.Create(startPoint, endPoint, pointOnArc);
            return _createDoc.NewModelCurve(arc, sketchPlane) as ModelArc;
        }

        /// <summary>
        /// 从现有曲线复制创建新曲线（椭圆、样条曲线）
        /// </summary>
        public ModelCurve CreateFromExisting(int sketchPlaneId, int sourceElementId, XYZ offset)
        {
            var sketchPlane = GetSketchPlaneById(sketchPlaneId);
            var sourceElement = _doc.GetElement(new ElementId(sourceElementId)) as ModelCurve;

            if (sourceElement == null)
                throw new Exception("未找到源曲线元素");

            var curves = new CurveArray();
            curves.Append(sourceElement.GeometryCurve);

            var modelCurves = _createDoc.NewModelCurveArray(curves, sketchPlane);

            if (modelCurves == null || modelCurves.Size == 0)
                throw new Exception("创建曲线失败");

            var result = modelCurves.get_Item(0);
            ElementTransformUtils.MoveElement(_doc, result.Id, offset);

            return result;
        }

        private SketchPlane GetSketchPlaneById(int id)
        {
            var element = _doc.GetElement(new ElementId(id));
            return element as SketchPlane ?? throw new Exception("无效的草图平面");
        }
    }

    /// <summary>
    /// 模型线类型统计数据模型
    /// </summary>
    public class ModelLineStatistic : INotifyPropertyChanged
    {
        private string _typeName;
        private int _count;

        public string TypeName
        {
            get => _typeName;
            set { _typeName = value; OnPropertyChanged(); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 三维点坐标模型
    /// </summary>
    public class XYZPoint : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _z;

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        public double Z
        {
            get => _z;
            set { _z = value; OnPropertyChanged(); }
        }

        public XYZ ToXYZ() => new XYZ(X, Y, Z);

        public static XYZPoint FromXYZ(XYZ xyz) =>
            new XYZPoint { X = xyz.X, Y = xyz.Y, Z = xyz.Z };

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 草图平面信息模型
    /// </summary>
    public class SketchPlaneInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _displayName;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 曲线元素信息模型（用于复制创建）
    /// </summary>
    public class CurveElementInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _curveType;
        private string _displayName;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string CurveType
        {
            get => _curveType;
            set { _curveType = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 模型线创建类型枚举
    /// </summary>
    public enum ModelLineType
    {
        Line,
        Arc,
        Ellipse,
        HermiteSpline,
        NurbSpline
    }

    /// <summary>
    /// 草图平面创建参数
    /// </summary>
    public class SketchPlaneCreationParams
    {
        public XYZPoint Normal { get; set; } = new XYZPoint { X = 0, Y = 0, Z = 1 };
        public XYZPoint Origin { get; set; } = new XYZPoint();
    }
}
