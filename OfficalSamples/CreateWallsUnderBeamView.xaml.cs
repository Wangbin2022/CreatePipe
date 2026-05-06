using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
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
    /// CreateWallsUnderBeamView.xaml 的交互逻辑
    /// </summary>
    public partial class CreateWallsUnderBeamView : Window
    {
        private readonly CreateWallsUnderBeamViewModel _viewModel;
        public CreateWallsUnderBeamView(ExternalCommandData commandData)
        {
            InitializeComponent();
            _viewModel = new CreateWallsUnderBeamViewModel(commandData);
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
    public class CreateWallsUnderBeamViewModel : ObserverableObject
    {
        #region 成员变量

        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private WallUnderBeamSettings _settings;
        private ObservableCollection<BeamInfo> _selectedBeams;
        private string _statusMessage;
        private bool _isProcessing;
        private BeamInfo _selectedBeam;

        #endregion

        #region 构造函数

        public CreateWallsUnderBeamViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;
            _settings = new WallUnderBeamSettings();
            _selectedBeams = new ObservableCollection<BeamInfo>();

            // 初始化命令
            CreateWallsCommand = new BaseBindingCommand(ExecuteCreateWalls);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);

            // 加载数据
            LoadData();
        }

        #endregion

        #region 属性

        public WallUnderBeamSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public ObservableCollection<BeamInfo> SelectedBeams
        {
            get => _selectedBeams;
            set => SetProperty(ref _selectedBeams, value);
        }

        public BeamInfo SelectedBeam
        {
            get => _selectedBeam;
            set => SetProperty(ref _selectedBeam, value);
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

        public int ValidBeamCount => SelectedBeams.Count(b => b.Status?.Contains("✓") == true);
        public int InvalidBeamCount => SelectedBeams.Count - ValidBeamCount;
        public bool HasValidBeams => ValidBeamCount > 0;

        public string SummaryInfo => $"有效梁: {ValidBeamCount}, 无效梁: {InvalidBeamCount}";

        public bool CanCreate => HasValidBeams && Settings.CanCreate && !IsProcessing;

        #endregion

        #region 命令

        public ICommand CreateWallsCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region 命令执行方法

        private bool CanExecuteCreateWalls() => CanCreate;

        private async void ExecuteCreateWalls(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建墙体...";

            var result = await System.Threading.Tasks.Task.Run(() => CreateWalls());

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

        #region 数据加载和创建方法

        /// <summary>
        /// 加载选中的梁并验证
        /// </summary>
        private void LoadData()
        {
            IsProcessing = true;

            // 加载墙体类型
            var wallTypes = LoadWallTypes();
            foreach (var wallType in wallTypes)
            {
                Settings.AvailableWallTypes.Add(wallType);
            }

            // 提取并验证选中的梁
            var beams = ExtractSelectedBeams();
            var (allValid, validBeams, errors) = BeamValidationService.ValidateBeams(beams);

            foreach (var beamInfo in validBeams)
            {
                SelectedBeams.Add(beamInfo);
            }

            if (errors.Any())
            {
                StatusMessage = $"发现 {errors.Count} 个无效梁: {string.Join("; ", errors.Take(3))}";
                if (errors.Count > 3) StatusMessage += "...";
            }
            else if (SelectedBeams.Any())
            {
                StatusMessage = $"成功加载 {SelectedBeams.Count} 根水平梁";
            }
            else
            {
                StatusMessage = "未找到有效的水平梁，请选择水平梁后重试";
            }

            OnPropertyChanged(nameof(ValidBeamCount));
            OnPropertyChanged(nameof(InvalidBeamCount));
            OnPropertyChanged(nameof(SummaryInfo));
            OnPropertyChanged(nameof(HasValidBeams));

            IsProcessing = false;
        }

        /// <summary>
        /// 加载所有墙体类型
        /// </summary>
        private List<WallType> LoadWallTypes()
        {
            var collector = new FilteredElementCollector(_document);
            return collector.OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();
        }

        /// <summary>
        /// 提取选中的梁
        /// 使用C# 7.3的模式匹配
        /// </summary>
        private List<FamilyInstance> ExtractSelectedBeams()
        {
            var uiDoc = _commandData.Application.ActiveUIDocument;
            var selectedIds = uiDoc.Selection.GetElementIds();
            var beams = new List<FamilyInstance>();

            foreach (var id in selectedIds)
            {
                var element = _document.GetElement(id);

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
        /// 创建墙体
        /// </summary>
        private WallCreationResult2 CreateWalls()
        {
            var validBeams = SelectedBeams
                .Where(b => b.Status?.Contains("✓") == true)
                .Select(b => b.Beam)
                .ToList();

            var wallService = new WallUnderBeamService(_document);
            return wallService.CreateWallsForBeams(validBeams, Settings);
        }
        #endregion
        #region 窗口关闭

        public event EventHandler<bool> RequestClose;
        protected virtual void OnRequestClose(bool result) =>
            RequestClose?.Invoke(this, result);

        #endregion
    }
    /// <summary>
    /// 墙体创建服务
    /// 负责在梁下方创建墙体
    /// 使用C# 7.3的using声明和模式匹配
    /// </summary>
    public class WallUnderBeamService
    {
        private readonly Document _document;

        // 单位转换常量（英尺转内部单位）
        private const double FEET_TO_INTERNAL = 1.0;  // Revit内部单位即为英尺
        private const double INCH_TO_FEET = 1.0 / 12.0;

        public WallUnderBeamService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// 为单根梁创建墙体
        /// </summary>
        public (bool success, ElementId wallId, string errorMessage) CreateWallForBeam(
            FamilyInstance beam, WallUnderBeamSettings settings)
        {
            // 验证梁
            var (isValid, errorMsg, curve, level) = BeamValidationService.ValidateBeam(beam);
            if (!isValid)
                return (false, null, errorMsg);

            if (level is null)
                return (false, null, "无法获取梁的参考标高");

            var transaction = new Transaction(_document, "创建梁下墙体");
            transaction.Start();

            try
            {
                // 计算墙体底部偏移（梁底部到标高的距离）
                var beamZ = curve.GetEndPoint(0).Z;
                var baseOffset = beamZ - level.Elevation - settings.OffsetFromBeam;

                // 创建墙体
                var wall = Wall.Create(_document, curve, settings.SelectedWallType.Id,
                    level.Id, settings.WallHeight, baseOffset, true, settings.IsStructural);

                if (wall is null)
                {
                    transaction.RollBack();
                    return (false, null, "无法创建墙体");
                }

                // 设置墙体参数
                SetWallParameters(wall, level.Id, baseOffset);

                transaction.Commit();
                return (true, wall.Id, null);
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                return (false, null, $"创建墙体失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为多根梁批量创建墙体
        /// 返回汇总结果
        /// </summary>
        public WallCreationResult2 CreateWallsForBeams(
            IEnumerable<FamilyInstance> beams, WallUnderBeamSettings settings)
        {
            var successCount = 0;
            var failCount = 0;
            var errors = new List<string>();

            foreach (var beam in beams)
            {
                var (success, wallId, error) = CreateWallForBeam(beam, settings);
                if (success)
                    successCount++;
                else
                {
                    failCount++;
                    errors.Add(error);
                }
            }

            if (successCount == 0)
                return WallCreationResult2.Failed($"所有墙体创建失败: {string.Join("; ", errors)}");

            if (failCount > 0)
                return WallCreationResult2.Partial(successCount, failCount, errors);

            return WallCreationResult2.Succeeded(successCount);
        }

        /// <summary>
        /// 设置墙体参数
        /// </summary>
        private void SetWallParameters(Wall wall, ElementId levelId, double baseOffset)
        {
            var baseConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            var baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);

            baseConstraintParam?.Set(levelId);
            baseOffsetParam?.Set(baseOffset);
        }
    }
    /// <summary>
    /// 梁验证服务
    /// 负责验证梁是否为水平、提取分析模型数据
    /// 使用C# 7.3的LINQ、元组和模式匹配
    /// </summary>
    public static class BeamValidationService
    {
        private const double PRECISION = 1e-10;

        /// <summary>
        /// 验证梁是否为水平
        /// 使用元组返回结果
        /// </summary>
        public static (bool isValid, string errorMessage, Curve analyticalCurve, Level referenceLevel)
            ValidateBeam(FamilyInstance beam)
        {
            if (beam is null)
                return (false, "梁对象为空", null, null);

            // 获取分析模型
            var analyticalModel = beam.GetAnalyticalModel();
            if (analyticalModel is null)
                return (false, $"梁 {beam.Name} 没有分析模型", null, null);

            // 获取分析曲线
            var curve = analyticalModel.GetCurve();
            if (curve is null)
                return (false, $"梁 {beam.Name} 的分析模型线无效", null, null);

            // 检查是否为水平线（Z坐标相同）
            var startZ = curve.GetEndPoint(0).Z;
            var endZ = curve.GetEndPoint(1).Z;
            if (Math.Abs(startZ - endZ) > PRECISION)
                return (false, $"梁 {beam.Name} 不是水平梁，请选择水平梁", null, null);

            // 获取参考标高
            var levelId = beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId();
            var level = beam.Document.GetElement(levelId) as Level;

            return (true, null, curve, level);
        }

        /// <summary>
        /// 批量验证梁
        /// 使用LINQ和元组
        /// </summary>
        public static (bool allValid, List<BeamInfo> validBeams, List<string> errors)
            ValidateBeams(IEnumerable<FamilyInstance> beams)
        {
            var validBeams = new List<BeamInfo>();
            var errors = new List<string>();

            foreach (var beam in beams)
            {
                var (isValid, error, curve, level) = ValidateBeam(beam);
                var beamInfo = new BeamInfo { Beam = beam, Status = isValid ? "✓ 有效" : $"✗ {error}" };

                if (isValid)
                {
                    beamInfo.Length = curve?.Length ?? 0;
                    validBeams.Add(beamInfo);
                }
                else if (error != null)
                {
                    errors.Add(error);
                }
            }

            return (validBeams.Count == beams.Count(), validBeams, errors);
        }

        /// <summary>
        /// 验证梁是否为水平（简化版，用于快速检查）
        /// </summary>
        public static bool IsBeamHorizontal(FamilyInstance beam)
        {
            var (isValid, _, _, _) = ValidateBeam(beam);
            return isValid;
        }
    }
    /// <summary>
    /// 墙体创建设置模型
    /// 使用C# 7.3的表达式体和自动属性
    /// </summary>
    public class WallUnderBeamSettings : INotifyPropertyChanged
    {
        private WallType _selectedWallType;
        private bool _isStructural = true;
        private double _wallHeight = 10.0;  // 默认高度10英尺
        private double _offsetFromBeam = 0.0; // 从梁底部的偏移量

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

        /// <summary>墙体高度（英尺）</summary>
        public double WallHeight
        {
            get => _wallHeight;
            set => SetField(ref _wallHeight, value);
        }

        /// <summary>从梁底部的偏移量（英尺）</summary>
        public double OffsetFromBeam
        {
            get => _offsetFromBeam;
            set => SetField(ref _offsetFromBeam, value);
        }

        /// <summary>是否有有效的墙体类型</summary>
        public bool HasValidWallType => SelectedWallType != null;

        /// <summary>是否可以创建墙体</summary>
        public bool CanCreate => HasValidWallType;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(SelectedWallType))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCreate)));
            }
        }
    }

    ///// <summary>
    ///// 梁信息模型
    ///// </summary>
    //public class BeamInfo : ObserverableObject
    //{
    //    private FamilyInstance _beam;
    //    private double _length;
    //    private string _status;

    //    public FamilyInstance Beam
    //    {
    //        get => _beam;
    //        set => SetProperty(ref _beam, value);
    //    }

    //    public double Length
    //    {
    //        get => _length;
    //        set => SetProperty(ref _length, value);
    //    }

    //    public string Status
    //    {
    //        get => _status;
    //        set => SetProperty(ref _status, value);
    //    }

    //    public string Name => Beam?.Name ?? "未知";
    //    public string Id => Beam?.Id.IntegerValue.ToString() ?? "N/A";
    //}

    /// <summary>
    /// 墙体创建结果
    /// </summary>
    public readonly struct WallCreationResult2
    {
        public bool Success { get; }
        public string Message { get; }
        public int SuccessCount { get; }
        public int FailCount { get; }
        public List<string> Errors { get; }

        public WallCreationResult2(bool success, string message, int successCount = 0, int failCount = 0, List<string> errors = null)
        {
            Success = success;
            Message = message;
            SuccessCount = successCount;
            FailCount = failCount;
            Errors = errors ?? new List<string>();
        }

        public static WallCreationResult2 Succeeded(int count) =>
            new WallCreationResult2(true, $"成功创建 {count} 面墙体", count, 0);

        public static WallCreationResult2 Partial(int success, int fail, List<string> errors) =>
            new WallCreationResult2(false, $"成功: {success}, 失败: {fail}", success, fail, errors);

        public static WallCreationResult2 Failed(string message) =>
            new WallCreationResult2(false, message);
    }

    /// <summary>
    /// 状态文本转颜色转换器
    /// </summary>
    public class CreateWallStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            if (status?.Contains("✓") == true)
                return System.Windows.Media.Brushes.Green;
            if (status?.Contains("✗") == true)
                return System.Windows.Media.Brushes.Red;
            return System.Windows.Media.Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
