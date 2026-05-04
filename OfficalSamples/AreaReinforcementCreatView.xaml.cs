using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AreaReinforcementCreatView.xaml 的交互逻辑
    /// </summary>
    public partial class AreaReinforcementCreatView : Window
    {
        private readonly AreaReinforcementCreatViewModel _viewModel;
        public AreaReinforcementCreatView(ExternalCommandData commandData)
        {
            InitializeComponent();
            _viewModel = new AreaReinforcementCreatViewModel(commandData);
            DataContext = _viewModel;
            _viewModel.RequestClose += OnRequestClose;
        }
        private void OnRequestClose(object sender, bool result)
        {
            DialogResult = result;
            Close();
        }
    }
    /// <summary>
    /// 主窗口视图模型
    /// 使用C# 7.3的表达式体、模式匹配和命令绑定
    /// </summary>
    public class AreaReinforcementCreatViewModel : ObserverableObject
    {
        #region 成员变量

        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private AreaReinforcementData2 _currentParameters;
        private HostElementType _hostElementType;
        private string _statusMessage;
        private bool _isProcessing;
        private string _elementInfo;

        #endregion

        #region 构造函数

        public AreaReinforcementCreatViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;

            // 初始化命令
            CreateCommand = new BaseBindingCommand(ExecuteCreate);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);

            // 加载选中的元素
            LoadSelectedElement();
        }

        #endregion

        #region 属性

        /// <summary>当前钢筋参数</summary>
        public AreaReinforcementData2 CurrentParameters
        {
            get => _currentParameters;
            set => SetProperty(ref _currentParameters, value);
        }

        /// <summary>宿主元素类型</summary>
        public HostElementType HostElementType
        {
            get => _hostElementType;
            set => SetProperty(ref _hostElementType, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>是否正在处理</summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        /// <summary>元素信息</summary>
        public string ElementInfo
        {
            get => _elementInfo;
            set => SetProperty(ref _elementInfo, value);
        }

        /// <summary>窗口标题</summary>
        public string WindowTitle => HostElementType == HostElementType.Wall
            ? "创建墙体区域钢筋" : "创建楼板区域钢筋";

        /// <summary>是否可以创建</summary>
        public bool CanCreate => CurrentParameters != null && !IsProcessing;

        #endregion

        #region 命令

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region 命令执行方法

        private bool CanExecuteCreate() => CanCreate;

        private async void ExecuteCreate(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建区域钢筋...";

            var result = await System.Threading.Tasks.Task.Run(() => CreateAreaReinforcement());

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

        #region 核心业务方法

        /// <summary>
        /// 加载选中的元素
        /// 使用C# 7.3的模式匹配
        /// </summary>
        private void LoadSelectedElement()
        {
            var uiDoc = _commandData.Application.ActiveUIDocument;
            var selectedIds = uiDoc.Selection.GetElementIds();

            if (selectedIds.Count != 1)
            {
                StatusMessage = "请精确选择一个墙或楼板元素";
                OnRequestClose(false);
                return;
            }

            var element = _document.GetElement(selectedIds.First());
            ElementInfo = $"选中元素: {element.Name} (ID: {element.Id.IntegerValue})";

            // 使用模式匹配判断元素类型
            switch (element)
            {
                case Wall wall:
                    InitializeForWall(wall);
                    break;
                case Floor floor:
                    InitializeForFloor(floor);
                    break;
                default:
                    StatusMessage = "请选择墙体(Wall)或楼板(Floor)元素";
                    OnRequestClose(false);
                    break;
            }
        }

        /// <summary>
        /// 初始化墙体参数
        /// </summary>
        private void InitializeForWall(Wall wall)
        {
            // 检查墙体类型
            if (wall.WallType.Kind != WallKind.Basic)
            {
                StatusMessage = "请选择基本墙(Basic Wall)";
                OnRequestClose(false);
                return;
            }

            // 验证几何数据
            var geomService = new GeometryDataService(_document);
            var result = geomService.GetWallGeometry(wall);

            if (!result.Success)
            {
                StatusMessage = $"墙体验证失败: {result.ErrorMessage}";
                OnRequestClose(false);
                return;
            }

            HostElementType = HostElementType.Wall;
            CurrentParameters = new WallReinforcementData();
            StatusMessage = "请设置钢筋参数后点击创建";
        }

        /// <summary>
        /// 初始化楼板参数
        /// </summary>
        private void InitializeForFloor(Floor floor)
        {
            var geomService = new GeometryDataService(_document);
            var result = geomService.GetFloorGeometry(floor);

            if (!result.Success)
            {
                StatusMessage = $"楼板验证失败: {result.ErrorMessage}";
                OnRequestClose(false);
                return;
            }

            HostElementType = HostElementType.Floor;
            CurrentParameters = new FloorReinforcementData();
            StatusMessage = "请设置钢筋参数后点击创建";
        }

        /// <summary>
        /// 创建区域钢筋
        /// </summary>
        private CreationResult CreateAreaReinforcement()
        {
            var uiDoc = _commandData.Application.ActiveUIDocument;
            var selectedId = uiDoc.Selection.GetElementIds().First();
            var element = _document.GetElement(selectedId);

            var geomService = new GeometryDataService(_document);
            var reinService = new AreaReinforcementService2(_document);

            switch (element)
            {
                case Wall wall:
                    var wallResult = geomService.GetWallGeometry(wall);
                    if (!wallResult.Success)
                        return CreationResult.Failed(wallResult.ErrorMessage);

                    return reinService.CreateOnWall(wall,
                        (WallReinforcementData)CurrentParameters,
                        wallResult.ProfileCurves);

                case Floor floor:
                    var floorResult = geomService.GetFloorGeometry(floor);
                    if (!floorResult.Success)
                        return CreationResult.Failed(floorResult.ErrorMessage);

                    return reinService.CreateOnFloor(floor,
                        (FloorReinforcementData)CurrentParameters,
                        floorResult.ProfileCurves);

                default:
                    return CreationResult.Failed("无效的元素类型");
            }
        }

        #endregion
        #region 窗口关闭

        public event EventHandler<bool> RequestClose;
        protected virtual void OnRequestClose(bool result) =>
            RequestClose?.Invoke(this, result);

        #endregion
    }
    /// <summary>
    /// 区域钢筋创建服务
    /// 负责创建AreaReinforcement并应用参数
    /// 使用C# 7.3的using声明和模式匹配
    /// </summary>
    public class AreaReinforcementService2
    {
        private readonly Document _document;

        public AreaReinforcementService2(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// 在墙体上创建区域钢筋
        /// </summary>
        public CreationResult CreateOnWall(Wall wall, WallReinforcementData data, IList<Curve> curves)
        {
            var transaction = new Transaction(_document, "创建墙体区域钢筋");
            transaction.Start();

            try
            {
                // 计算主要方向（使用第一条曲线的方向）
                var majorDirection = CalculateMajorDirection(curves);

                // 创建默认类型（如果不存在）
                var areaReinTypeId = EnsureDefaultAreaReinforcementType();
                var barTypeId = EnsureDefaultRebarBarType();
                var hookTypeId = EnsureDefaultRebarHookType();

                // 创建区域钢筋
                var areaRein = AreaReinforcement.Create(
                    _document, wall, curves.ToList(),
                    majorDirection, areaReinTypeId, barTypeId, hookTypeId);

                // 应用参数
                data.ApplyToAreaReinforcement(areaRein);

                transaction.Commit();
                return CreationResult.Succeeded(areaRein.Id);
            }
            catch (System.Exception ex)
            {
                transaction.RollBack();
                return CreationResult.Failed($"创建墙体区域钢筋失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 在楼板上创建区域钢筋
        /// </summary>
        public CreationResult CreateOnFloor(Floor floor, FloorReinforcementData data, IList<Curve> curves)
        {
            var transaction = new Transaction(_document, "创建楼板区域钢筋");
            transaction.Start();

            try
            {
                // 计算主要方向（使用第一条曲线的方向）
                var majorDirection = CalculateMajorDirection(curves);

                // 创建默认类型
                var areaReinTypeId = EnsureDefaultAreaReinforcementType();
                var barTypeId = EnsureDefaultRebarBarType();
                var hookTypeId = EnsureDefaultRebarHookType();

                // 创建区域钢筋
                var areaRein = AreaReinforcement.Create(
                    _document, floor, curves.ToList(),
                    majorDirection, areaReinTypeId, barTypeId, hookTypeId);

                // 应用参数
                data.ApplyToAreaReinforcement(areaRein);

                transaction.Commit();
                return CreationResult.Succeeded(areaRein.Id);
            }
            catch (System.Exception ex)
            {
                transaction.RollBack();
                return CreationResult.Failed($"创建楼板区域钢筋失败: {ex.Message}");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 计算主要方向向量
        /// 使用第一条曲线确定方向
        /// </summary>
        private XYZ CalculateMajorDirection(IList<Curve> curves)
        {
            if (!curves.Any()) return XYZ.BasisX;

            var firstCurve = curves[0];
            var start = firstCurve.GetEndPoint(0);
            var end = firstCurve.GetEndPoint(1);

            return new XYZ(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
        }

        /// <summary>
        /// 确保存在默认的区域钢筋类型
        /// </summary>
        private ElementId EnsureDefaultAreaReinforcementType()
        {
            // 查找现有的AreaReinforcementType
            var collector = new FilteredElementCollector(_document);
            var existingType = collector.OfClass(typeof(AreaReinforcementType))
                .FirstOrDefault();

            if (existingType != null)
                return existingType.Id;

            // 创建默认类型
            return AreaReinforcementType.CreateDefaultAreaReinforcementType(_document);
        }

        /// <summary>
        /// 确保存在默认的钢筋类型
        /// </summary>
        private ElementId EnsureDefaultRebarBarType()
        {
            var collector = new FilteredElementCollector(_document);
            var existingType = collector.OfClass(typeof(RebarBarType))
                .FirstOrDefault();

            if (existingType != null)
                return existingType.Id;

            return RebarBarType.CreateDefaultRebarBarType(_document);
        }

        /// <summary>
        /// 确保存在默认的钢筋钩类型
        /// </summary>
        private ElementId EnsureDefaultRebarHookType()
        {
            var collector = new FilteredElementCollector(_document);
            var existingType = collector.OfClass(typeof(RebarHookType))
                .FirstOrDefault();

            if (existingType != null)
                return existingType.Id;

            return RebarHookType.CreateDefaultRebarHookType(_document);
        }

        #endregion
    }
    /// <summary>
    /// 几何数据提取服务
    /// 负责从墙体和楼板提取创建区域钢筋所需的数据
    /// 使用C# 7.3的元组返回和模式匹配
    /// </summary>
    public class GeometryDataService
    {
        private readonly Document _document;

        public GeometryDataService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// 墙体几何数据提取结果
        /// 使用元组返回多个值
        /// </summary>
        public class WallGeometryResult
        {
            public bool Success { get; set; }
            public Reference FaceReference { get; set; }
            public IList<Curve> ProfileCurves { get; set; }
            public Line LocationLine { get; set; }
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// 楼板几何数据提取结果
        /// </summary>
        public class FloorGeometryResult
        {
            public bool Success { get; set; }
            public Reference FaceReference { get; set; }
            public IList<Curve> ProfileCurves { get; set; }
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// 获取墙体的几何数据
        /// </summary>
        public WallGeometryResult GetWallGeometry(Wall wall)
        {
            var result = new WallGeometryResult { Success = false };

            // 获取墙体位置线
            if (!TryGetLocationLine(wall, out var locationLine))
            {
                result.ErrorMessage = "无法获取墙体位置线";
                return result;
            }
            result.LocationLine = locationLine;

            // 获取平行于墙体的面
            if (!TryGetParallelFace(wall, locationLine, out var faceReference))
            {
                result.ErrorMessage = "无法找到平行于墙体的垂直面";
                return result;
            }
            result.FaceReference = faceReference;

            // 获取分析模型轮廓
            if (!TryGetAnalyticalProfile(wall, out var curves))
            {
                result.ErrorMessage = "无法获取有效的分析模型轮廓";
                return result;
            }

            // 验证是否为矩形
            if (!GeometryService2.IsRectangular(curves))
            {
                result.ErrorMessage = "分析模型轮廓不是矩形";
                return result;
            }

            result.ProfileCurves = curves;
            result.Success = true;
            return result;
        }

        /// <summary>
        /// 获取楼板的几何数据
        /// </summary>
        public FloorGeometryResult GetFloorGeometry(Floor floor)
        {
            var result = new FloorGeometryResult { Success = false };

            // 获取水平面参考
            if (!TryGetHorizontalFace(floor, out var faceReference))
            {
                result.ErrorMessage = "无法找到水平面";
                return result;
            }
            result.FaceReference = faceReference;

            // 获取分析模型轮廓
            if (!TryGetAnalyticalProfile(floor, out var curves))
            {
                result.ErrorMessage = "无法获取有效的分析模型轮廓";
                return result;
            }

            // 验证是否为矩形
            if (!GeometryService2.IsRectangular(curves))
            {
                result.ErrorMessage = "分析模型轮廓不是矩形";
                return result;
            }

            result.ProfileCurves = curves;
            result.Success = true;
            return result;
        }

        #region 私有辅助方法

        private bool TryGetLocationLine(Wall wall, out Line locationLine)
        {
            locationLine = null;
            if (wall.Location is LocationCurve locationCurve)
            {
                locationLine = locationCurve.Curve as Line;
            }
            return locationLine != null;
        }

        private bool TryGetParallelFace(Element element, Line referenceLine, out Reference faceReference)
        {
            faceReference = null;

            foreach (var face in GeometryService2.GetFaces(element))
            {
                if (GeometryService2.IsFaceParallelToLine(face, referenceLine))
                {
                    faceReference = face.Reference;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetHorizontalFace(Floor floor, out Reference faceReference)
        {
            faceReference = null;

            foreach (var face in GeometryService2.GetFaces(floor))
            {
                if (GeometryService2.IsHorizontalFace(face))
                {
                    faceReference = face.Reference;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetAnalyticalProfile(Element element, out IList<Curve> curves)
        {
            curves = null;

            var analyticalModel = element.GetAnalyticalModel();
            if (analyticalModel is null) return false;

            curves = analyticalModel.GetCurves(AnalyticalCurveType.ActiveCurves);
            return curves != null && curves.Any();
        }

        #endregion
    }
    /// <summary>
    /// 几何计算服务
    /// 负责面分析、平行/垂直判断、矩形验证等几何计算
    /// 使用C# 7.3的表达式体、元组和模式匹配
    /// </summary>
    public static class GeometryService2
    {
        #region 常量

        private const double PRECISION = 0.00001;

        #endregion

        #region 基础几何方法

        /// <summary>
        /// 比较两个double值是否相等
        /// </summary>
        public static bool IsEqual(double d1, double d2) =>
            Math.Abs(d1 - d2) < PRECISION;

        /// <summary>
        /// 向量减法
        /// </summary>
        public static XYZ Subtract(XYZ p1, XYZ p2) =>
            new XYZ(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        /// <summary>
        /// 向量叉积
        /// </summary>
        public static XYZ CrossProduct(XYZ v1, XYZ v2) =>
            new XYZ(v1.Y * v2.Z - v1.Z * v2.Y,
                    v1.Z * v2.X - v1.X * v2.Z,
                    v1.X * v2.Y - v1.Y * v2.X);

        /// <summary>
        /// 向量点积
        /// </summary>
        public static double DotProduct(XYZ v1, XYZ v2) =>
            v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

        #endregion

        #region 面相关方法

        /// <summary>
        /// 获取元素的所有面
        /// 使用C# 7.3的模式匹配和yield return
        /// </summary>
        public static IEnumerable<Autodesk.Revit.DB.Face> GetFaces(Element element)
        {
            if (element is null) yield break;

            var geoOptions = new Options
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Fine
            };

            var geoElement = element.get_Geometry(geoOptions);
            if (geoElement is null) yield break;

            foreach (var geoObject in geoElement)
            {
                // 使用模式匹配获取Solid
                if (geoObject is Solid solid && solid.Faces.Size > 0)
                {
                    foreach (Autodesk.Revit.DB.Face face in solid.Faces)
                    {
                        yield return face;
                    }
                }
            }
        }

        /// <summary>
        /// 获取面的三角剖分顶点
        /// </summary>
        public static List<XYZ> GetFaceVertices(Autodesk.Revit.DB.Face face)
        {
            var mesh = face.Triangulate();
            return mesh?.Vertices?.ToList() ?? new List<XYZ>();
        }

        /// <summary>
        /// 判断面是否为水平面
        /// </summary>
        public static bool IsHorizontalFace(Autodesk.Revit.DB.Face face)
        {
            var vertices = GetFaceVertices(face);
            if (vertices.Count < 3) return false;

            var firstZ = vertices[0].Z;
            return vertices.All(v => IsEqual(v.Z, firstZ));
        }

        /// <summary>
        /// 判断面是否与直线平行
        /// </summary>
        public static bool IsFaceParallelToLine(Autodesk.Revit.DB.Face face, Line line)
        {
            var vertices = GetFaceVertices(face);
            if (vertices.Count < 3) return false;

            // 计算面的法向量
            var v1 = Subtract(vertices[0], vertices[1]);
            var v2 = Subtract(vertices[1], vertices[2]);
            var normal = CrossProduct(v1, v2);

            // 计算线的方向向量
            var lineDir = Subtract(line.GetEndPoint(1), line.GetEndPoint(0));

            // 如果法向量与线方向垂直，则线平行于面
            var dot = DotProduct(normal, lineDir);
            return Math.Abs(dot) < PRECISION;
        }

        #endregion

        #region 矩形验证方法

        /// <summary>
        /// 判断4条曲线是否构成矩形
        /// 使用C# 7.3的模式匹配和元组
        /// </summary>
        public static bool IsRectangular(IList<Curve> curves)
        {
            if (curves.Count != 4) return false;

            // 转换为Line数组，使用模式匹配
            var lines = curves.Select(c => c as Line).ToArray();
            if (lines.Any(l => l is null)) return false;

            return IsRectangular(lines);
        }

        /// <summary>
        /// 判断4条线段是否构成矩形
        /// </summary>
        private static bool IsRectangular(Autodesk.Revit.DB.Line[] lines)
        {
            var firstLine = lines[0];
            var verticalLines = new List<Line>(2);
            Line parallelLine = null;

            // 分类：垂直线和平行线
            foreach (var line in lines.Skip(1))
            {
                if (IsPerpendicular(firstLine, line))
                    verticalLines.Add(line);
                else
                    parallelLine = line;
            }

            // 必须有2条垂直线和1条平行线
            if (verticalLines.Count != 2 || parallelLine is null)
                return false;

            // 检查平行线是否与垂直线垂直
            return IsPerpendicular(parallelLine, verticalLines[0]);
        }
        /// <summary>
        /// 判断两条线段是否垂直
        /// </summary>
        private static bool IsPerpendicular(Autodesk.Revit.DB.Line line1, Line line2)
        {
            var dir1 = Subtract(line1.GetEndPoint(1), line1.GetEndPoint(0));
            var dir2 = Subtract(line2.GetEndPoint(1), line2.GetEndPoint(0));
            var dot = DotProduct(dir1, dir2);
            return Math.Abs(dot) < PRECISION;
        }
        #endregion
    }
    ///// <summary>
    ///// 区域钢筋布局规则枚举
    ///// </summary>
    //public enum LayoutRules
    //{
    //    FixedNumber = 2,
    //    MaximumSpacing = 3
    //}
    /// <summary>
    /// 元素类型枚举
    /// </summary>
    public enum HostElementType
    {
        Wall,
        Floor,
        Unknown
    }
    /// <summary>
    /// 钢筋参数基类
    /// 实现INotifyPropertyChanged支持MVVM
    /// 使用C# 7.3的表达式体和CallerMemberName
    /// </summary>
    public abstract class AreaReinforcementData2 : ObserverableObject
    {
        private LayoutRules _layoutRule = LayoutRules.MaximumSpacing;

        [Category("Construction")]
        [Description("钢筋布局规则：FixedNumber(固定数量) 或 MaximumSpacing(最大间距)")]
        public LayoutRules LayoutRule
        {
            get => _layoutRule;
            set => SetProperty(ref _layoutRule, value);
        }
        /// <summary>
        /// 应用到区域钢筋元素
        /// </summary>
        public abstract void ApplyToAreaReinforcement(AreaReinforcement areaRein);
    }
    /// <summary>
    /// 墙体钢筋参数数据
    /// </summary>
    public class WallReinforcementData : AreaReinforcementData2
    {
        private bool _exteriorMajorDirection = true;
        private bool _exteriorMinorDirection = true;
        private bool _interiorMajorDirection = true;
        private bool _interiorMinorDirection = true;

        [Category("Layers")]
        [Description("外墙主要方向钢筋")]
        public bool ExteriorMajorDirection
        {
            get => _exteriorMajorDirection;
            set => SetProperty(ref _exteriorMajorDirection, value);
        }

        [Category("Layers")]
        [Description("外墙次要方向钢筋")]
        public bool ExteriorMinorDirection
        {
            get => _exteriorMinorDirection;
            set => SetProperty(ref _exteriorMinorDirection, value);
        }

        [Category("Layers")]
        [Description("内墙主要方向钢筋")]
        public bool InteriorMajorDirection
        {
            get => _interiorMajorDirection;
            set => SetProperty(ref _interiorMajorDirection, value);
        }

        [Category("Layers")]
        [Description("内墙次要方向钢筋")]
        public bool InteriorMinorDirection
        {
            get => _interiorMinorDirection;
            set => SetProperty(ref _interiorMinorDirection, value);
        }

        public override void ApplyToAreaReinforcement(AreaReinforcement areaRein)
        {
            // 应用基类布局规则
            SetLayoutRule(areaRein, LayoutRule);

            // 应用墙体特定的层方向参数
            SetLayerParameter(areaRein, "Exterior Major Direction", ExteriorMajorDirection);
            SetLayerParameter(areaRein, "Interior Major Direction", InteriorMajorDirection);
            SetLayerParameter(areaRein, "Exterior Minor Direction", ExteriorMinorDirection);
            SetLayerParameter(areaRein, "Interior Minor Direction", InteriorMinorDirection);
        }

        private void SetLayoutRule(AreaReinforcement areaRein, LayoutRules rule)
        {
            var param = areaRein.get_Parameter(BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE);
            if (param != null)
                param.Set((int)rule);
        }

        private void SetLayerParameter(AreaReinforcement areaRein, string paramName, bool value)
        {
            foreach (Parameter param in areaRein.Parameters)
            {
                if (param.Definition?.Name == paramName)
                {
                    param.Set(Convert.ToInt32(value));
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 楼板钢筋参数数据
    /// </summary>
    public class FloorReinforcementData : AreaReinforcementData2
    {
        private bool _topMajorDirection = true;
        private bool _topMinorDirection = true;
        private bool _bottomMajorDirection = true;
        private bool _bottomMinorDirection = true;

        [Category("Layers")]
        [Description("顶部主要方向钢筋")]
        public bool TopMajorDirection
        {
            get => _topMajorDirection;
            set => SetProperty(ref _topMajorDirection, value);
        }

        [Category("Layers")]
        [Description("顶部次要方向钢筋")]
        public bool TopMinorDirection
        {
            get => _topMinorDirection;
            set => SetProperty(ref _topMinorDirection, value);
        }

        [Category("Layers")]
        [Description("底部主要方向钢筋")]
        public bool BottomMajorDirection
        {
            get => _bottomMajorDirection;
            set => SetProperty(ref _bottomMajorDirection, value);
        }

        [Category("Layers")]
        [Description("底部次要方向钢筋")]
        public bool BottomMinorDirection
        {
            get => _bottomMinorDirection;
            set => SetProperty(ref _bottomMinorDirection, value);
        }

        public override void ApplyToAreaReinforcement(AreaReinforcement areaRein)
        {
            // 应用基类布局规则
            SetLayoutRule(areaRein, LayoutRule);

            // 应用楼板特定的层方向参数（使用BuiltInParameter）
            SetBuiltInParameter(areaRein, BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1, BottomMajorDirection);
            SetBuiltInParameter(areaRein, BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2, BottomMinorDirection);
            SetBuiltInParameter(areaRein, BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_1, TopMajorDirection);
            SetBuiltInParameter(areaRein, BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2, TopMinorDirection);
        }

        private void SetLayoutRule(AreaReinforcement areaRein, LayoutRules rule)
        {
            var param = areaRein.get_Parameter(BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE);
            param?.Set((int)rule);
        }

        private void SetBuiltInParameter(AreaReinforcement areaRein, BuiltInParameter paramId, bool value)
        {
            var param = areaRein.get_Parameter(paramId);
            param?.Set(Convert.ToInt32(value));
        }
    }

    /// <summary>
    /// 创建结果
    /// 使用C# 7.3的只读结构体
    /// </summary>
    public readonly struct CreationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public ElementId CreatedElementId { get; }
        public CreationResult(bool success, string message, ElementId elementId = null)
        {
            Success = success;
            Message = message;
            CreatedElementId = elementId;
        }
        public static CreationResult Succeeded(ElementId id) =>
            new CreationResult(true, "区域钢筋创建成功", id);
        public static CreationResult Failed(string message) =>
            new CreationResult(false, message);
    }
}
