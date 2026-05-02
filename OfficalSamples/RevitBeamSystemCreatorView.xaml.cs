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


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// RevitBeamSystemCreatorView.xaml 的交互逻辑
    /// </summary>
    public partial class RevitBeamSystemCreatorView : Window
    {
        private readonly RevitBeamSystemCreatorViewModel _viewModel;
        public RevitBeamSystemCreatorView(ExternalCommandData commandData)
        {
            InitializeComponent();
            _viewModel = new RevitBeamSystemCreatorViewModel(commandData);
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
    public class RevitBeamSystemCreatorViewModel : ObserverableObject
    {
        #region 成员变量

        private readonly BeamSystemDataModel _dataModel;
        private GeometryDrawing _profileDrawing;
        private double _canvasWidth = 400;
        private double _canvasHeight = 400;
        private bool _isLoading = true;
        private string _statusMessage;

        #endregion

        #region 构造函数

        public RevitBeamSystemCreatorViewModel(ExternalCommandData commandData)
        {
            _dataModel = new BeamSystemDataModel();
            _dataModel.PropertyChanged += OnDataModelPropertyChanged;

            // 初始化命令
            OkCommand = new BaseBindingCommand(ExecuteOk);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);
            RotateDirectionCommand = new BaseBindingCommand(ExecuteRotateDirection, _ => _dataModel.HasValidProfile);

            // 加载数据
            LoadData(commandData);
        }

        #endregion

        #region 属性

        /// <summary>数据模型</summary>
        public BeamSystemDataModel DataModel => _dataModel;

        /// <summary>轮廓绘图</summary>
        public GeometryDrawing ProfileDrawing
        {
            get => _profileDrawing;
            set => SetProperty(ref _profileDrawing, value);
        }

        /// <summary>画布宽度</summary>
        public double CanvasWidth
        {
            get => _canvasWidth;
            set => SetProperty(ref _canvasWidth, value);
        }

        /// <summary>画布高度</summary>
        public double CanvasHeight
        {
            get => _canvasHeight;
            set => SetProperty(ref _canvasHeight, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 命令

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RotateDirectionCommand { get; }

        #endregion

        #region 命令执行方法

        private bool CanExecuteOk() => _dataModel.HasValidProfile && _dataModel.SelectedBeamType != null;

        private void ExecuteOk(Object obj)
        {
            try
            {
                var doc = _dataModel.CommandData.Application.ActiveUIDocument.Document;

                var transaction = new Transaction(doc, "创建梁系统");
                transaction.Start();

                var beamSystem = BeamSystemBuilderService.CreateBeamSystem(
                    doc,
                    _dataModel.ProfileLines,
                    _dataModel);

                transaction.Commit();

                StatusMessage = $"梁系统创建成功，ID: {beamSystem.Id.IntegerValue}";
                OnRequestClose(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
                OnRequestClose(false);
            }
        }

        private void ExecuteCancel(Object obj)
        {
            StatusMessage = "操作已取消";
            OnRequestClose(false);
        }

        private void ExecuteRotateDirection(Object obj)
        {
            _dataModel.RotateProfileDirection();
            UpdateProfileDrawing();
        }

        #endregion

        #region 辅助方法

        private void LoadData(ExternalCommandData commandData)
        {
            IsLoading = true;

            try
            {
                DataLoadingService.InitializeDataModel(_dataModel, commandData);
                UpdateProfileDrawing();
                StatusMessage = $"已加载 {_dataModel.ProfileLines.Count} 条轮廓线";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                OnRequestClose(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateProfileDrawing()
        {
            if (!_dataModel.HasValidProfile) return;

            // 计算轮廓边界
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var line in _dataModel.ProfileLines)
            {
                var p1 = line.GetEndPoint(0);
                var p2 = line.GetEndPoint(1);

                minX = Math.Min(minX, Math.Min(p1.X, p2.X));
                minY = Math.Min(minY, Math.Min(p1.Y, p2.Y));
                maxX = Math.Max(maxX, Math.Max(p1.X, p2.X));
                maxY = Math.Max(maxY, Math.Max(p1.Y, p2.Y));
            }

            var width = maxX - minX;
            var height = maxY - minY;
            var scale = Math.Min(CanvasWidth / width, CanvasHeight / height) * 0.8;

            // 计算偏移使图形居中
            var offsetX = (CanvasWidth - width * scale) / 2 - minX * scale;
            var offsetY = (CanvasHeight - height * scale) / 2 - minY * scale;

            // 创建几何图形
            var geometryGroup = new GeometryGroup();

            foreach (var line in _dataModel.ProfileLines)
            {
                var p1 = line.GetEndPoint(0);
                var p2 = line.GetEndPoint(1);

                var start = new System.Windows.Point(p1.X * scale + offsetX, p1.Y * scale + offsetY);
                var end = new System.Windows.Point(p2.X * scale + offsetX, p2.Y * scale + offsetY);

                geometryGroup.Children.Add(new LineGeometry(start, end));
            }

            // 创建绘图
            ProfileDrawing = new GeometryDrawing
            {
                Geometry = geometryGroup,
                Pen = new Pen(Brushes.DarkGreen, 2),
                Brush = Brushes.Transparent
            };
        }

        private void OnDataModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BeamSystemDataModel.ProfileLines) ||
                e.PropertyName == nameof(BeamSystemDataModel.CurrentProfileStartIndex))
            {
                UpdateProfileDrawing();
            }

            if (e.PropertyName == nameof(BeamSystemDataModel.HasValidProfile))
            {
                (RotateDirectionCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
                (OkCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
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
    /// 数据加载服务
    /// 负责从Revit文档中加载梁类型和选中的梁
    /// 使用C# 7.3的LINQ和模式匹配
    /// </summary>
    public static class DataLoadingService
    {
        /// <summary>
        /// 从选中的元素中提取梁实例
        /// </summary>
        public static List<FamilyInstance> ExtractSelectedBeams(UIDocument uiDoc)
        {
            var selectedIds = uiDoc.Selection.GetElementIds();
            var beams = new List<FamilyInstance>();

            foreach (var id in selectedIds)
            {
                var element = uiDoc.Document.GetElement(id);

                // 使用C# 7.3的模式匹配
                if (element is FamilyInstance beam &&
                    beam.Category?.Name == "Structural Framing" &&
                    beam.StructuralType == StructuralType.Beam)
                {
                    beams.Add(beam);
                }
            }

            return beams;
        }

        /// <summary>
        /// 加载所有可用的梁类型
        /// </summary>
        public static List<FamilySymbol> LoadBeamTypes(Document document)
        {
            var beamTypes = new List<FamilySymbol>();

            var families = new FilteredElementCollector(document)
                .OfClass(typeof(Family))
                .Cast<Family>();

            foreach (var family in families)
            {
                var symbolIds = family.GetFamilySymbolIds();
                foreach (var symbolId in symbolIds)
                {
                    var symbol = document.GetElement(symbolId) as FamilySymbol;
                    if (symbol?.Category?.Name == "Structural Framing")
                    {
                        beamTypes.Add(symbol);
                    }
                }
            }

            return beamTypes;
        }

        /// <summary>
        /// 从选中的梁构建轮廓线
        /// </summary>
        public static (bool success, List<Line> profileLines, string errorMessage)
            BuildProfileFromBeams(List<FamilyInstance> beams)
        {
            // 提取直线
            var lines = new List<Line>();
            foreach (var beam in beams)
            {
                var line = GeometryService1.GetBeamCurve(beam);
                if (line is null)
                {
                    return (false, null, "请勿选择弧形梁");
                }
                lines.Add(line);
            }

            // 检查共面
            if (!GeometryService1.AreLinesInSameHorizontalPlane(lines))
            {
                return (false, null, "选中的梁不在同一水平面上");
            }

            // 排序为闭合轮廓
            var sortedLines = GeometryService1.SortLinesToClosedProfile(lines);
            if (sortedLines is null)
            {
                return (false, null, "选中的梁无法形成闭合轮廓");
            }

            return (true, sortedLines, null);
        }

        /// <summary>
        /// 初始化数据模型
        /// </summary>
        public static bool InitializeDataModel(BeamSystemDataModel dataModel, ExternalCommandData commandData)
        {
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var document = uiDoc.Document;

                // 存储命令数据
                dataModel.CommandData = commandData;

                // 提取选中的梁
                var selectedBeams = ExtractSelectedBeams(uiDoc);
                if (!selectedBeams.Any())
                    throw new Exception("请先选择梁");

                // 构建轮廓
                var (success, profileLines, errorMessage) = BuildProfileFromBeams(selectedBeams);
                if (!success)
                    throw new Exception(errorMessage);

                dataModel.ProfileLines = new System.Collections.ObjectModel.ObservableCollection<Line>(profileLines);

                // 加载梁类型
                var beamTypes = LoadBeamTypes(document);
                if (!beamTypes.Any())
                    throw new Exception("当前项目中没有加载任何梁族");

                foreach (var beamType in beamTypes)
                    dataModel.AvailableBeamTypes.Add(beamType);

                dataModel.SelectedBeamType = beamTypes.First();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据初始化失败: {ex.Message}", ex);
            }
        }
    }
    /// <summary>
    /// 梁系统创建服务
    /// 使用C# 7.3的表达式体和本地函数
    /// </summary>
    public static class BeamSystemBuilderService
    {
        /// <summary>
        /// 创建梁系统
        /// </summary>
        /// <param name="document">当前文档</param>
        /// <param name="profileLines">轮廓线集合</param>
        /// <param name="dataModel">参数数据模型</param>
        /// <returns>创建的梁系统</returns>
        public static BeamSystem CreateBeamSystem(Document document,
            IList<Line> profileLines, BeamSystemDataModel dataModel)
        {
            if (document is null) throw new System.ArgumentNullException(nameof(document));
            if (profileLines is null || profileLines.Count < 3)
                throw new System.InvalidOperationException("需要至少3条轮廓线");
            if (dataModel is null) throw new System.ArgumentNullException(nameof(dataModel));

            // 创建曲线集合
            var curves = new List<Curve>();
            foreach (var line in profileLines)
                curves.Add(line);

            // 创建梁系统
            var sketchPlane = document.ActiveView.SketchPlane;
            var beamSystem = BeamSystem.Create(document, curves, sketchPlane, 0);

            // 设置参数
            beamSystem.LayoutRule = dataModel.CreateLayoutRule();
            beamSystem.BeamType = dataModel.SelectedBeamType;

            return beamSystem;
        }

        /// <summary>
        /// 验证数据是否有效
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateData(BeamSystemDataModel dataModel)
        {
            if (!dataModel.HasValidProfile)
                return (false, "没有有效的轮廓线，请先选择梁");

            if (dataModel.SelectedBeamType is null)
                return (false, "请选择梁类型");

            if (dataModel.ShowSpacing && dataModel.Spacing <= 0)
                return (false, "间距必须大于0");

            if (dataModel.ShowNumberOfLines && dataModel.NumberOfLines < 1)
                return (false, "梁数量必须至少为1");

            return (true, null);
        }
    }
    /// <summary>
    /// 几何计算服务
    /// 使用C# 7.3的元组、本地函数和表达式体
    /// </summary>
    public static class GeometryService1
    {
        #region 常量

        private const double DOUBLE_EPSILON = 0.00001;

        #endregion

        #region 核心方法

        /// <summary>
        /// 比较两个XYZ是否相等
        /// 使用C# 7.3的元组解构
        /// </summary>
        public static bool AreXYZEqual(XYZ p1, XYZ p2)
        {
            if (p1 is null || p2 is null) return false;

            return AreDoublesEqual(p1.X, p2.X) &&
                   AreDoublesEqual(p1.Y, p2.Y) &&
                   AreDoublesEqual(p1.Z, p2.Z);
        }

        /// <summary>
        /// 比较两个double值
        /// </summary>
        public static bool AreDoublesEqual(double d1, double d2) =>
            Math.Abs(d1 - d2) < DOUBLE_EPSILON;

        /// <summary>
        /// 检查线条是否在同一水平面上
        /// </summary>
        public static bool AreLinesInSameHorizontalPlane(IEnumerable<Line> lines)
        {
            var lineList = lines.ToList();
            if (!lineList.Any()) return false;

            var referenceZ = lineList[0].GetEndPoint(0).Z;

            return lineList.All(line =>
                AreDoublesEqual(line.GetEndPoint(0).Z, referenceZ) &&
                AreDoublesEqual(line.GetEndPoint(1).Z, referenceZ));
        }

        /// <summary>
        /// 将线条排序为闭合轮廓
        /// 返回排序后的线条列表，如果不能形成闭合轮廓则返回null
        /// 使用C# 7.3的本地函数和模式匹配
        /// </summary>
        public static List<Line> SortLinesToClosedProfile(List<Line> originLines)
        {
            if (originLines.Count < 3) return null;

            var lines = new List<Line>(originLines);
            var result = new List<Line>();

            // 本地函数：查找下一条连接线
            Line FindNextLine(XYZ currentEnd, ref int foundIndex)
            {
                for (int i = 1; i < lines.Count; i++)
                {
                    if (lines[i] is null) continue;

                    if (AreXYZEqual(lines[i].GetEndPoint(0), currentEnd))
                    {
                        foundIndex = i;
                        return lines[i];
                    }

                    if (AreXYZEqual(lines[i].GetEndPoint(1), currentEnd))
                    {
                        // 需要反转方向
                        var reversedLine = Line.CreateBound(lines[i].GetEndPoint(1), lines[i].GetEndPoint(0));
                        foundIndex = i;
                        return reversedLine;
                    }
                }
                return null;
            }

            // 开始排序
            result.Add(lines[0]);
            var intersectPoint = lines[0].GetEndPoint(1);
            lines[0] = null;

            for (int i = 0; i < lines.Count; i++)
            {
                int foundIndex = -1;
                var nextLine = FindNextLine(intersectPoint, ref foundIndex);

                if (nextLine is null) break;

                result.Add(nextLine);
                intersectPoint = nextLine.GetEndPoint(1);
                lines[foundIndex] = null;
            }

            // 验证是否所有线条都被使用
            if (result.Count != lines.Count) return null;

            // 验证是否闭合
            if (!AreXYZEqual(intersectPoint, result[0].GetEndPoint(0))) return null;

            // 验证无自交（简化版 - 检查非相邻线是否相交）
            if (HasSelfIntersection(result)) return null;

            return result;
        }

        /// <summary>
        /// 检查轮廓是否有自交
        /// 使用C# 7.3的本地函数
        /// </summary>
        private static bool HasSelfIntersection(List<Line> lines)
        {
            for (int i = 0; i < lines.Count - 2; i++)
            {
                for (int j = i + 2; j < lines.Count; j++)
                {
                    // 跳过首尾相接的线
                    if (i == 0 && j == lines.Count - 1) continue;

                    if (DoLinesIntersect2D(lines[i], lines[j]))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查2D线条是否相交（忽略Z坐标）
        /// 使用C# 7.3的元组解构
        /// </summary>
        private static bool DoLinesIntersect2D(Line line1, Line line2)
        {
            var (p1, p2) = (GetPoint2D(line1.GetEndPoint(0)), GetPoint2D(line1.GetEndPoint(1)));
            var (p3, p4) = (GetPoint2D(line2.GetEndPoint(0)), GetPoint2D(line2.GetEndPoint(1)));

            return DoSegmentsIntersect(p1, p2, p3, p4);
        }

        /// <summary>
        /// 获取2D点（忽略Z）
        /// </summary>
        private static (double x, double y) GetPoint2D(XYZ p) => (p.X, p.Y);

        /// <summary>
        /// 检查2D线段是否相交
        /// 使用跨立实验法
        /// </summary>
        private static bool DoSegmentsIntersect((double x, double y) p1, (double x, double y) p2,
                                                  (double x, double y) p3, (double x, double y) p4)
        {
            double Cross((double x, double y) a, (double x, double y) b) =>
                a.x * b.y - a.y * b.x;

            var v1 = (p2.x - p1.x, p2.y - p1.y);
            var v2 = (p3.x - p1.x, p3.y - p1.y);
            var v3 = (p4.x - p1.x, p4.y - p1.y);
            var v4 = (p4.x - p3.x, p4.y - p3.y);
            var v5 = (p1.x - p3.x, p1.y - p3.y);
            var v6 = (p2.x - p3.x, p2.y - p3.y);

            var cross1 = Cross(v1, v2);
            var cross2 = Cross(v1, v3);
            var cross3 = Cross(v4, v5);
            var cross4 = Cross(v4, v6);

            return (cross1 * cross2 < 0) && (cross3 * cross4 < 0);
        }

        /// <summary>
        /// 从梁实例获取曲线
        /// </summary>
        public static Line GetBeamCurve(FamilyInstance beam)
        {
            if (beam?.Location is LocationCurve locationCurve)
                return locationCurve.Curve as Line;
            return null;
        }

        /// <summary>
        /// 检查梁是否为直线（非弧线）
        /// </summary>
        public static bool IsStraightBeam(FamilyInstance beam) =>
            GetBeamCurve(beam) != null;

        #endregion
    }
    /// <summary>
    /// 梁系统布局方法枚举
    /// </summary>
    public enum LayoutMethod
    {
        ClearSpacing,      // 净间距
        MaximumSpacing,    // 最大间距
        FixedNumber,       // 固定数量
        FixedDistance      // 固定距离
    }

    /// <summary>
    /// 梁系统数据模型
    /// 实现INotifyPropertyChanged支持MVVM绑定
    /// 使用C# 7.3的表达式体和CallerMemberName
    /// </summary>
    public class BeamSystemDataModel : INotifyPropertyChanged
    {
        #region 成员变量

        private LayoutMethod _selectedLayoutMethod = LayoutMethod.ClearSpacing;
        private double _spacing = 2000.0;           // 间距 (mm)
        private int _numberOfLines = 6;              // 梁数量
        private BeamSystemJustifyType _justifyType = BeamSystemJustifyType.Center;
        private FamilySymbol _selectedBeamType;
        private ObservableCollection<Line> _profileLines;
        private int _currentProfileStartIndex;

        #endregion

        #region 属性

        /// <summary>选中的布局方法</summary>
        public LayoutMethod SelectedLayoutMethod
        {
            get => _selectedLayoutMethod;
            set => SetProperty(ref _selectedLayoutMethod, value);
        }

        /// <summary>间距值（用于ClearSpacing/MaximumSpacing/FixedDistance）</summary>
        public double Spacing
        {
            get => _spacing;
            set => SetProperty(ref _spacing, value);
        }

        /// <summary>梁数量（用于FixedNumber）</summary>
        public int NumberOfLines
        {
            get => _numberOfLines;
            set => SetProperty(ref _numberOfLines, Math.Max(1, value));
        }

        /// <summary>对齐方式</summary>
        public BeamSystemJustifyType JustifyType
        {
            get => _justifyType;
            set => SetProperty(ref _justifyType, value);
        }

        /// <summary>选中的梁类型</summary>
        public FamilySymbol SelectedBeamType
        {
            get => _selectedBeamType;
            set => SetProperty(ref _selectedBeamType, value);
        }

        /// <summary>可用的梁类型列表</summary>
        public ObservableCollection<FamilySymbol> AvailableBeamTypes { get; } = new ObservableCollection<FamilySymbol>();

        /// <summary>轮廓线集合</summary>
        public ObservableCollection<Line> ProfileLines
        {
            get => _profileLines;
            set => SetProperty(ref _profileLines, value);
        }

        /// <summary>当前轮廓起始索引（用于方向切换）</summary>
        public int CurrentProfileStartIndex
        {
            get => _currentProfileStartIndex;
            set => SetProperty(ref _currentProfileStartIndex, value);
        }

        /// <summary>是否有有效的轮廓</summary>
        public bool HasValidProfile => ProfileLines?.Count >= 3;

        /// <summary>外部命令数据</summary>
        public ExternalCommandData CommandData { get; set; }

        #endregion

        #region UI显示属性（使用表达式体）

        /// <summary>是否显示间距输入</summary>
        public bool ShowSpacing => SelectedLayoutMethod != LayoutMethod.FixedNumber;

        /// <summary>是否显示数量输入</summary>
        public bool ShowNumberOfLines => SelectedLayoutMethod == LayoutMethod.FixedNumber;

        /// <summary>是否显示对齐方式</summary>
        public bool ShowJustifyType => SelectedLayoutMethod != LayoutMethod.FixedNumber;

        /// <summary>间距标签文本</summary>
        public string SpacingLabel
        {
            get
            {
                switch (SelectedLayoutMethod)
                {
                    case LayoutMethod.ClearSpacing:
                        return "净间距 (mm)";
                    case LayoutMethod.MaximumSpacing:
                        return "最大间距 (mm)";
                    case LayoutMethod.FixedDistance:
                        return "固定距离 (mm)";
                    default:
                        return "间距 (mm)";
                }
            }
        }

        /// <summary>摘要信息</summary>
        public string SummaryInfo => $"轮廓线数: {ProfileLines?.Count ?? 0}, 布局方式: {SelectedLayoutMethod}";

        #endregion

        #region 命令方法

        /// <summary>
        /// 切换轮廓方向（旋转起始点）
        /// </summary>
        public void RotateProfileDirection()
        {
            if (ProfileLines == null || ProfileLines.Count < 2) return;

            var lines = ProfileLines.ToList();
            var firstLine = lines[0];
            lines.RemoveAt(0);
            lines.Add(firstLine);

            ProfileLines.Clear();
            foreach (var line in lines)
                ProfileLines.Add(line);

            CurrentProfileStartIndex = (CurrentProfileStartIndex + 1) % ProfileLines.Count;
        }

        /// <summary>
        /// 创建LayoutRule对象（根据当前参数）
        /// 使用C# 7.3的switch表达式
        /// </summary>
        public LayoutRule CreateLayoutRule()
        {
            switch (SelectedLayoutMethod)
            {
                case LayoutMethod.ClearSpacing:
                    return new LayoutRuleClearSpacing(Spacing, JustifyType);
                case LayoutMethod.MaximumSpacing:
                    return new LayoutRuleMaximumSpacing(Spacing);
                case LayoutMethod.FixedNumber:
                    return new LayoutRuleFixedNumber(NumberOfLines);
                case LayoutMethod.FixedDistance:
                    return new LayoutRuleFixedDistance(Spacing, JustifyType);
                default:
                    throw new InvalidOperationException("未知的布局方法");
            }
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            // 触发相关属性的变更通知
            if (propertyName == nameof(SelectedLayoutMethod))
            {
                OnPropertyChanged(nameof(ShowSpacing));
                OnPropertyChanged(nameof(ShowNumberOfLines));
                OnPropertyChanged(nameof(ShowJustifyType));
                OnPropertyChanged(nameof(SpacingLabel));
            }
            OnPropertyChanged(nameof(SummaryInfo));
            return true;
        }
        #endregion
    }
}
