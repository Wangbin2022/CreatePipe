using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
    /// WallFromBeamsCreatView.xaml 的交互逻辑
    /// </summary>
    public partial class WallFromBeamsCreatView : Window
    {
        public WallFromBeamsCreatView(ExternalCommandData commandData)
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主窗口视图模型
    /// 使用C# 7.3的表达式体、模式匹配和命令绑定
    /// </summary>
    public class WallFromBeamsCreatViewModel : ObserverableObject
    {
        #region 成员变量

        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private WallCreationSettings _settings;
        private string _statusMessage;
        private bool _isProcessing;
        private int _selectedBeamCount;
        private bool _isProfileValid;

        #endregion

        #region 构造函数

        public WallFromBeamsCreatViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            _settings = new WallCreationSettings();

            // 初始化命令
            CreateWallCommand = new BaseBindingCommand(ExecuteCreateWall);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);

            // 加载数据
            LoadData();
        }

        #endregion

        #region 属性

        public WallCreationSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public int SelectedBeamCount
        {
            get => _selectedBeamCount;
            set => SetProperty(ref _selectedBeamCount, value);
        }

        public bool IsProfileValid
        {
            get => _isProfileValid;
            set => SetProperty(ref _isProfileValid, value);
        }

        public string ProfileValidationMessage { get; private set; }

        public bool CanCreate => IsProfileValid && Settings.CanCreate && !IsProcessing;

        #endregion

        #region 命令

        public ICommand CreateWallCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region 命令执行方法

        private bool CanExecuteCreateWall() => CanCreate;

        private async void ExecuteCreateWall(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建墙体...";

            var result = await System.Threading.Tasks.Task.Run(() => CreateWall());

            StatusMessage = result.Message;
            IsProcessing = false;

            if (result.Success)
            {
                OnRequestClose(true);
            }
        }

        private void ExecuteCancel(Object obj)
        {
            StatusMessage = "操作已取消";
            OnRequestClose(false);
        }

        #endregion

        #region 数据加载和验证方法

        /// <summary>
        /// 加载数据并验证梁轮廓
        /// </summary>
        private void LoadData()
        {
            IsProcessing = true;

            // 加载墙体类型
            var wallTypes = DataLoadingService2.LoadWallTypes(_document);
            foreach (var wallType in wallTypes)
            {
                Settings.AvailableWallTypes.Add(wallType);
            }

            // 加载标高
            Settings.TargetLevel = DataLoadingService2.LoadFirstLevel(_document);

            // 提取选中的梁
            var uiDoc = _commandData.Application.ActiveUIDocument;
            var beams = DataLoadingService2.ExtractBeams(uiDoc);
            SelectedBeamCount = beams.Count;

            if (beams.Count == 0)
            {
                StatusMessage = "请至少选择一根梁";
                IsProfileValid = false;
                IsProcessing = false;
                return;
            }

            // 提取分析模型曲线
            var (success, curves, error) = DataLoadingService2.ExtractAnalyticalCurves(beams);
            if (!success)
            {
                StatusMessage = error;
                IsProfileValid = false;
                IsProcessing = false;
                return;
            }

            // 验证轮廓
            var validationResult = DataLoadingService2.ValidateBeamProfile(curves);
            IsProfileValid = validationResult.IsValid;
            ProfileValidationMessage = validationResult.IsValid
                ? $"✓ 有效轮廓 (高程范围: {validationResult.MinElevation:F2} ~ {validationResult.MaxElevation:F2})"
                : $"✗ {validationResult.ErrorMessage}";

            StatusMessage = IsProfileValid
                ? "梁轮廓验证通过，请选择墙体类型后创建"
                : "梁轮廓验证失败，请选择不同的梁";

            IsProcessing = false;
        }

        /// <summary>
        /// 创建墙体
        /// </summary>
        private WallCreationResult CreateWall()
        {
            var uiDoc = _commandData.Application.ActiveUIDocument;
            var beams = DataLoadingService2.ExtractBeams(uiDoc);
            var (_, curves, _) = DataLoadingService2.ExtractAnalyticalCurves(beams);

            var wallService = new WallCreationService(_document);
            return wallService.CreateWallFromBeamProfile(curves, Settings);
        }
        #endregion

        #region 窗口关闭

        public event EventHandler<bool> RequestClose;
        protected virtual void OnRequestClose(bool result) =>
            RequestClose?.Invoke(this, result);

        #endregion
    }
    /// <summary>
    /// 数据加载服务
    /// 负责从Revit文档中加载梁、墙体类型、标高等数据
    /// 使用C# 7.3的LINQ和模式匹配
    /// </summary>
    public static class DataLoadingService2
    {
        /// <summary>
        /// 从选中的元素中提取梁
        /// 使用C# 7.3的模式匹配
        /// </summary>
        public static List<FamilyInstance> ExtractBeams(UIDocument uiDoc)
        {
            var selectedIds = uiDoc.Selection.GetElementIds();
            var beams = new List<FamilyInstance>();
            foreach (var id in selectedIds)
            {
                var element = uiDoc.Document.GetElement(id);
                // 使用模式匹配判断是否为梁
                if (element is FamilyInstance instance &&
                    instance.StructuralType == StructuralType.Beam)
                {
                    beams.Add(instance);
                }
            }
            return beams;
        }
        /// <summary>
        /// 从梁提取分析模型曲线
        /// </summary>
        public static (bool success, List<Curve> curves, string errorMessage)
            ExtractAnalyticalCurves(List<FamilyInstance> beams)
        {
            var curves = new List<Curve>();
            foreach (var beam in beams)
            {
                var analyticalModel = beam.GetAnalyticalModel();
                if (analyticalModel is null)
                {
                    return (false, null, $"梁 {beam.Name} 没有分析模型");
                }
                var curve = analyticalModel.GetCurve();
                if (curve is null)
                {
                    return (false, null, $"梁 {beam.Name} 的分析模型无效");
                }
                curves.Add(curve);
            }
            return (true, curves, null);
        }
        /// <summary>
        /// 加载所有墙体类型
        /// </summary>
        public static List<WallType> LoadWallTypes(Document document)
        {
            var collector = new FilteredElementCollector(document);
            return collector.OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();
        }
        /// <summary>
        /// 加载第一个可用标高
        /// </summary>
        public static Level LoadFirstLevel(Document document)
        {
            var collector = new FilteredElementCollector(document);
            return collector.OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault();
        }
        /// <summary>
        /// 验证梁轮廓是否有效
        /// 使用元组和本地函数
        /// </summary>
        public static BeamProfileValidationResult ValidateBeamProfile(List<Curve> curves)
        {
            // 检查垂直平面
            if (!GeometryHelper.AreInSameVerticalPlane(curves, out var planeError))
            {
                return BeamProfileValidationResult.Failure(planeError);
            }
            // 检查是否能形成闭合轮廓
            if (!GeometryHelper.CanFormClosedProfile(curves))
            {
                return BeamProfileValidationResult.Failure("选中的梁无法形成闭合轮廓");
            }
            // 获取高程边界
            var (minZ, maxZ) = GeometryHelper.GetElevationBounds(curves);
            return BeamProfileValidationResult.Success(curves, minZ, maxZ);
        }
    }
    /// <summary>
    /// 墙体创建服务
    /// 负责排序曲线、创建墙体、设置参数
    /// 使用C# 7.3的using声明和模式匹配
    /// </summary>
    public class WallCreationService
    {
        private readonly Document _document;

        public WallCreationService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// 从梁轮廓创建墙体
        /// </summary>
        public WallCreationResult CreateWallFromBeamProfile(
            IList<Curve> unsortedCurves,
            WallCreationSettings settings)
        {
            // 排序曲线形成闭合轮廓
            var sortedCurves = GeometryHelper.SortCurvesToClosedProfile(unsortedCurves);
            if (sortedCurves is null || !sortedCurves.Any())
            {
                return WallCreationResult.Failed("无法将梁曲线排序为闭合轮廓");
            }

            // 计算高程偏移
            var (minZ, maxZ) = GeometryHelper.GetElevationBounds(sortedCurves);
            var baseOffset = minZ - settings.TargetLevel.Elevation;
            var topOffset = maxZ - settings.TargetLevel.Elevation;

            var transaction = new Transaction(_document, "从梁轮廓创建墙体");
            transaction.Start();

            // 创建墙体
            var wall = Wall.Create(_document, sortedCurves,
                settings.SelectedWallType.Id,
                settings.TargetLevel.Id,
                settings.IsStructural);

            if (wall is null)
            {
                transaction.RollBack();
                return WallCreationResult.Failed("无法创建墙体");
            }

            // 设置墙体参数
            SetWallParameters(wall, settings.TargetLevel.Id, baseOffset, topOffset);

            transaction.Commit();
            return WallCreationResult.Succeeded(wall.Id);
        }

        /// <summary>
        /// 设置墙体参数
        /// </summary>
        private void SetWallParameters(Wall wall, ElementId levelId, double baseOffset, double topOffset)
        {
            var baseConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
            var topConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            var topOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);

            baseConstraintParam?.Set(levelId);
            baseOffsetParam?.Set(baseOffset);
            topConstraintParam?.Set(levelId);
            topOffsetParam?.Set(topOffset);
        }
    }
    /// <summary>
    /// 几何计算辅助类
    /// 使用C# 7.3的表达式体和元组
    /// </summary>
    public static class GeometryHelper
    {
        private const double PRECISION = 1e-10;

        /// <summary>
        /// 判断两个点是否相等
        /// </summary>
        public static bool ArePointsEqual(XYZ p1, XYZ p2) =>
            Math.Abs(p1.X - p2.X) < PRECISION &&
            Math.Abs(p1.Y - p2.Y) < PRECISION &&
            Math.Abs(p1.Z - p2.Z) < PRECISION;

        /// <summary>
        /// 判断两个double值是否相等
        /// </summary>
        public static bool AreEqual(double d1, double d2) =>
            Math.Abs(d1 - d2) < PRECISION;

        /// <summary>
        /// 检查所有梁是否在同一垂直平面内
        /// 使用C# 7.3的switch表达式和元组
        /// </summary>
        public static bool AreInSameVerticalPlane(IList<Curve> curves, out string errorMessage)
        {
            errorMessage = null;
            if (!curves.Any())
            {
                errorMessage = "没有可用的曲线";
                return false;
            }

            var firstCurve = curves[0];
            var (start, end) = (firstCurve.GetEndPoint(0), firstCurve.GetEndPoint(1));

            // 确定平面类型
            var planeType = DeterminePlaneType(start, end);

            // 验证所有曲线
            for (int i = 1; i < curves.Count; i++)
            {
                var curve = curves[i];
                var (s, e) = (curve.GetEndPoint(0), curve.GetEndPoint(1));

                if (!IsCurveInPlane(s, e, planeType))
                {
                    errorMessage = "所有梁必须在同一垂直平面内";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 确定平面类型
        /// </summary>
        private static int DeterminePlaneType(XYZ start, XYZ end)
        {
            if (AreEqual(start.X, end.X)) return 1;  // Y-Z平面
            if (AreEqual(start.Y, end.Y)) return 2;  // X-Z平面
            return 0;  // 斜平面
        }

        /// <summary>
        /// 检查曲线是否在指定平面内
        /// </summary>
        private static bool IsCurveInPlane(XYZ start, XYZ end, int planeType)
        {
            switch (planeType)
            {
                case 0:
                    return AreEqual(CalculateSlope(start, end), CalculateSlope(start, end)); // 斜率相同
                case 1:
                    return AreEqual(start.X, end.X);  // X坐标相同
                case 2:
                    return AreEqual(start.Y, end.Y);  // Y坐标相同
                default:
                    return false;
            }
        }

        /// <summary>
        /// 计算XY平面上的斜率
        /// </summary>
        private static double CalculateSlope(XYZ p1, XYZ p2) =>
            (p1.Y - p2.Y) / (p1.X - p2.X);

        /// <summary>
        /// 排序曲线形成闭合轮廓
        /// 使用C# 7.3的本地函数和元组
        /// </summary>
        public static List<Curve> SortCurvesToClosedProfile(IList<Curve> unsortedCurves)
        {
            if (!unsortedCurves.Any()) return new List<Curve>();

            var sortedCurves = new List<Curve>();
            var remainingCurves = new List<Curve>(unsortedCurves);

            // 从第一条曲线开始
            var currentCurve = remainingCurves[0];
            sortedCurves.Add(currentCurve);
            remainingCurves.RemoveAt(0);

            var currentEndPoint = currentCurve.GetEndPoint(1);

            // 循环查找下一条连接曲线
            while (remainingCurves.Any())
            {
                // 使用 bool 标志判断是否找到
                bool found = FindNextConnectedCurve(remainingCurves, currentEndPoint,
                    out Curve nextCurve, out XYZ newEndPoint, out int index);

                if (!found) break;

                sortedCurves.Add(nextCurve);
                currentEndPoint = newEndPoint;
                remainingCurves.RemoveAt(index);
            }

            // 验证是否使用了所有曲线并闭合
            if (remainingCurves.Any() || !ArePointsEqual(currentEndPoint, sortedCurves[0].GetEndPoint(0)))
                return null;

            return sortedCurves;
        }

        /// <summary>
        /// 查找下一条连接的曲线（使用 out 参数替代元组返回）
        /// </summary>
        private static bool FindNextConnectedCurve(
            List<Curve> curves,
            XYZ targetPoint,
            out Curve foundCurve,
            out XYZ newEndPoint,
            out int foundIndex)
        {
            foundCurve = null;
            newEndPoint = null;
            foundIndex = -1;

            for (int i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];
                var startPoint = curve.GetEndPoint(0);
                var endPoint = curve.GetEndPoint(1);

                // 检查起点是否匹配
                if (ArePointsEqual(startPoint, targetPoint))
                {
                    foundCurve = curve;
                    newEndPoint = endPoint;
                    foundIndex = i;
                    return true;
                }

                // 检查终点是否匹配（需要翻转曲线）
                if (ArePointsEqual(endPoint, targetPoint))
                {
                    foundCurve = curve;
                    newEndPoint = startPoint;
                    foundIndex = i;
                    return true;
                }
            }

            return false;
        }
        //public static List<Curve> SortCurvesToClosedProfile(IList<Curve> unsortedCurves)
        //{
        //    if (!unsortedCurves.Any()) return new List<Curve>();
        //    var sortedCurves = new List<Curve>();
        //    var remainingCurves = new List<Curve>(unsortedCurves);
        //    // 从第一条曲线开始
        //    var currentCurve = remainingCurves[0];
        //    sortedCurves.Add(currentCurve);
        //    remainingCurves.RemoveAt(0);
        //    var currentEndPoint = currentCurve.GetEndPoint(1);
        //    // 循环查找下一条连接曲线
        //    while (remainingCurves.Any())
        //    {
        //        var nextCurve = FindNextConnectedCurve(remainingCurves, currentEndPoint);
        //        if (nextCurve is null) break;
        //        sortedCurves.Add(nextCurve.curve);
        //        currentEndPoint = nextCurve.newEndPoint;
        //        remainingCurves.RemoveAt(nextCurve.index);
        //    }
        //    // 验证是否使用了所有曲线并闭合
        //    if (remainingCurves.Any() || !ArePointsEqual(currentEndPoint, sortedCurves[0].GetEndPoint(0)))
        //        return null;
        //    return sortedCurves;
        //}

        /// <summary>
        /// 查找下一条连接的曲线
        /// 使用元组返回多个值
        /// </summary>
        private static (Curve curve, XYZ newEndPoint, int index) FindNextConnectedCurve(
            List<Curve> curves, XYZ targetPoint)
        {
            for (int i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];
                var (start, end) = (curve.GetEndPoint(0), curve.GetEndPoint(1));

                if (ArePointsEqual(start, targetPoint))
                    return (curve, end, i);

                if (ArePointsEqual(end, targetPoint))
                    return (curve, start, i);
            }

            return (null, null, -1);
        }

        /// <summary>
        /// 获取轮廓的边界高程
        /// 使用元组返回
        /// </summary>
        public static (double minZ, double maxZ) GetElevationBounds(IList<Curve> curves)
        {
            var elevations = curves.SelectMany(c => new[] { c.GetEndPoint(0).Z, c.GetEndPoint(1).Z }).ToList();
            return (elevations.Min(), elevations.Max());
        }

        /// <summary>
        /// 检查曲线是否形成闭合轮廓
        /// </summary>
        public static bool CanFormClosedProfile(IList<Curve> curves)
        {
            var points = new HashSet<XYZ>(new XYZEqualityComparer());

            foreach (var curve in curves)
            {
                points.Add(curve.GetEndPoint(0));
                points.Add(curve.GetEndPoint(1));
            }

            // 闭合轮廓中曲线数量应等于顶点数量
            return points.Count == curves.Count;
        }

        /// <summary>
        /// XYZ相等比较器
        /// </summary>
        private class XYZEqualityComparer : IEqualityComparer<XYZ>
        {
            public bool Equals(XYZ x, XYZ y) => ArePointsEqual(x, y);
            public int GetHashCode(XYZ obj) => obj.GetHashCode();
        }
    }
    /// <summary>
    /// 墙体创建设置模型
    /// 使用C# 7.3的表达式体和自动属性
    /// </summary>
    public class WallCreationSettings : INotifyPropertyChanged
    {
        private WallType _selectedWallType;
        private bool _isStructural = true;
        private Level _targetLevel;

        /// <summary>可用的墙体类型列表</summary>
        public ObservableCollection<WallType> AvailableWallTypes { get; } = new ObservableCollection<WallType>();

        /// <summary>选中的墙体类型</summary>
        public WallType SelectedWallType
        {
            get => _selectedWallType;
            set => SetField(ref _selectedWallType, value);
        }

        /// <summary>是否为结构墙</summary>
        public bool IsStructural
        {
            get => _isStructural;
            set => SetField(ref _isStructural, value);
        }

        /// <summary>目标标高</summary>
        public Level TargetLevel
        {
            get => _targetLevel;
            set => SetField(ref _targetLevel, value);
        }

        /// <summary>是否有有效的墙体类型</summary>
        public bool HasValidWallType => SelectedWallType != null;

        /// <summary>是否有有效的标高</summary>
        public bool HasValidLevel => TargetLevel != null;

        /// <summary>是否可以创建墙体</summary>
        public bool CanCreate => HasValidWallType && HasValidLevel;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(SelectedWallType) || propertyName == nameof(TargetLevel))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCreate)));
            }
        }
    }

    /// <summary>
    /// 梁轮廓验证结果
    /// 使用C# 7.3的只读结构体
    /// </summary>
    public readonly struct BeamProfileValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }
        public List<Curve> SortedCurves { get; }
        public double MinElevation { get; }
        public double MaxElevation { get; }

        public BeamProfileValidationResult(bool isValid, string errorMessage = null,
            List<Curve> sortedCurves = null, double minElevation = 0, double maxElevation = 0)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            SortedCurves = sortedCurves ?? new List<Curve>();
            MinElevation = minElevation;
            MaxElevation = maxElevation;
        }

        public static BeamProfileValidationResult Success(List<Curve> curves, double minZ, double maxZ) =>
            new BeamProfileValidationResult(true, null, curves, minZ, maxZ);

        public static BeamProfileValidationResult Failure(string errorMessage) =>
            new BeamProfileValidationResult(false, errorMessage);
    }

    /// <summary>
    /// 墙体创建结果
    /// </summary>
    public readonly struct WallCreationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public ElementId CreatedWallId { get; }

        public WallCreationResult(bool success, string message, ElementId wallId = null)
        {
            Success = success;
            Message = message;
            CreatedWallId = wallId;
        }

        public static WallCreationResult Succeeded(ElementId wallId) =>
            new WallCreationResult(true, "墙体创建成功", wallId);

        public static WallCreationResult Failed(string message) =>
            new WallCreationResult(false, message);
    }
}
