using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// StructuralFrameBuilderView.xaml 的交互逻辑
    /// </summary>
    public partial class StructuralFrameBuilderView : Window
    {
        public StructuralFrameBuilderView(UIApplication uiApp)
        {
            InitializeComponent();
            DataContext = new StructuralFrameBuilderViewModel(uiApp);
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
    public class StructuralFrameBuilderViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly Document _doc;
        private readonly FrameTypeService _typeService;
        private readonly LevelService _levelService;
        private readonly FrameConfiguration _config;

        private bool _isProcessing;
        private string _statusMessage;

        public StructuralFrameBuilderViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _doc = uiApp.ActiveUIDocument.Document;
            _typeService = new FrameTypeService(_doc);
            _levelService = new LevelService(_doc);
            _config = new FrameConfiguration();

            // 命令
            BuildCommand = new BaseBindingCommand(ExecuteBuild);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            DuplicateColumnCommand = new BaseBindingCommand(_ => DuplicateType("Column"));
            DuplicateBeamCommand = new BaseBindingCommand(_ => DuplicateType("Beam"));
            DuplicateBraceCommand = new BaseBindingCommand(_ => DuplicateType("Brace"));

            ExecuteRefresh(null);
        }

        #region 属性

        public FrameConfiguration Config => _config;

        public ObservableCollection<FamilySymbolItem> ColumnTypes => _typeService.ColumnTypes;
        public ObservableCollection<FamilySymbolItem> BeamTypes => _typeService.BeamTypes;
        public ObservableCollection<FamilySymbolItem> BraceTypes => _typeService.BraceTypes;

        public FamilySymbolItem SelectedColumn
        {
            get => _typeService.SelectedColumn;
            set { _typeService.SelectedColumn = value; OnPropertyChanged(); }
        }

        public FamilySymbolItem SelectedBeam
        {
            get => _typeService.SelectedBeam;
            set { _typeService.SelectedBeam = value; OnPropertyChanged(); }
        }

        public FamilySymbolItem SelectedBrace
        {
            get => _typeService.SelectedBrace;
            set { _typeService.SelectedBrace = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Level> Levels { get; set; } = new ObservableCollection<Level>();

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        #endregion

        #region 命令

        public ICommand BuildCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DuplicateColumnCommand { get; }
        public ICommand DuplicateBeamCommand { get; }
        public ICommand DuplicateBraceCommand { get; }

        #endregion

        private bool CanExecuteBuild() => !IsProcessing && Config.IsValid &&
            SelectedColumn != null && SelectedBeam != null && SelectedBrace != null;

        private void ExecuteRefresh(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在加载数据...";

            try
            {
                _typeService.LoadTypes();

                Levels.Clear();
                foreach (var level in _levelService.GetLevels())
                    Levels.Add(level);

                if (Levels.Count >= 2)
                {
                    var lastIndex = Levels.Count - 1;
                    Config.LevelHeight = Levels[lastIndex].Elevation - Levels[lastIndex - 1].Elevation;
                }

                StatusMessage = $"加载完成：{ColumnTypes.Count} 种柱类型，{BeamTypes.Count} 种梁类型";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void ExecuteBuild(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建框架...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // 确保有足够的楼层
                    var levels = _levelService.EnsureLevels(Config.FloorNumber, Config.LevelHeight);

                    // 创建框架
                    var builder = new FrameBuilderService(_doc, Config);
                    builder.Build(levels.ToList(),
                        SelectedColumn.Symbol,
                        SelectedBeam.Symbol,
                        SelectedBrace.Symbol);

                    DispatcherHelper.Invoke(() =>
                    {
                        StatusMessage = "框架创建成功！";
                        TaskDialog.Show("完成", $"已创建 {Config.TotalColumns} 根柱子");
                    });
                }
                catch (Exception ex)
                {
                    DispatcherHelper.Invoke(() => StatusMessage = $"创建失败：{ex.Message}");
                }
                finally
                {
                    DispatcherHelper.Invoke(() => IsProcessing = false);
                }
            });
        }

        private void DuplicateType(string typeName)
        {
            //var dialog = new Views.InputDialog("输入新类型名称", $"请输入新的{typeName}类型名称");
            //if (dialog.ShowDialog() != true) return;
            //var source = typeName switch
            //{
            //    "Column" => SelectedColumn?.Symbol,
            //    "Beam" => SelectedBeam?.Symbol,
            //    "Brace" => SelectedBrace?.Symbol,
            //    _ => null
            //};
            //if (source == null) return;
            //var newType = _typeService.DuplicateType(source, dialog.InputValue);
            //if (newType != null)
            //{
            //    StatusMessage = $"成功创建新类型：{dialog.InputValue}";
            //    ExecuteRefresh(null);
            //}
        }
    }
    /// <summary>
    /// 结构构件类型管理服务
    /// </summary>
    public class FrameTypeService
    {
        private readonly Document _doc;

        public FrameTypeService(Document doc)
        {
            _doc = doc;
        }

        public ObservableCollection<FamilySymbolItem> ColumnTypes { get; } = new ObservableCollection<FamilySymbolItem>();
        public ObservableCollection<FamilySymbolItem> BeamTypes { get; } = new ObservableCollection<FamilySymbolItem>();
        public ObservableCollection<FamilySymbolItem> BraceTypes { get; } = new ObservableCollection<FamilySymbolItem>();

        public FamilySymbolItem SelectedColumn { get; set; }
        public FamilySymbolItem SelectedBeam { get; set; }
        public FamilySymbolItem SelectedBrace { get; set; }

        /// <summary>
        /// 加载所有结构构件类型
        /// </summary>
        public void LoadTypes()
        {
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>();

            foreach (var symbol in collector)
            {
                var category = symbol.Category;
                if (category == null) continue;

                var item = new FamilySymbolItem { Symbol = symbol };

                if (category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns)
                    ColumnTypes.Add(item);
                else if (category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
                {
                    BeamTypes.Add(item);
                    BraceTypes.Add(item);
                }
            }

            SelectedColumn = ColumnTypes.FirstOrDefault();
            SelectedBeam = BeamTypes.FirstOrDefault();
            SelectedBrace = BraceTypes.FirstOrDefault();
        }

        /// <summary>
        /// 复制类型
        /// </summary>
        public FamilySymbol DuplicateType(FamilySymbol source, string newName)
        {
            return source.Duplicate(newName) as FamilySymbol;
        }
    }

    /// <summary>
    /// 标高管理服务
    /// </summary>
    public class LevelService
    {
        private readonly Document _doc;

        public LevelService(Document doc)
        {
            _doc = doc;
        }

        /// <summary>
        /// 获取所有标高（按标高排序）
        /// </summary>
        public List<Level> GetLevels()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }

        /// <summary>
        /// 确保有足够数量的楼层标高
        /// </summary>
        public List<Level> EnsureLevels(int requiredFloorCount, double levelHeight)
        {
            var existingLevels = GetLevels();
            var currentFloorCount = existingLevels.Count - 1;

            if (currentFloorCount >= requiredFloorCount)
                return existingLevels.Take(requiredFloorCount + 1).ToList();

            // 获取楼层平面视图类型ID
            var floorPlanTypeId = GetFloorPlanTypeId();

            var levels = new List<Level>(existingLevels);
            var baseElevation = levels.Last().Elevation;

            for (int i = 0; i < requiredFloorCount - currentFloorCount; i++)
            {
                var elevation = baseElevation + levelHeight * (i + 1);
                var newLevel = Level.Create(_doc, elevation);

                // 创建对应的楼层平面视图
                if (floorPlanTypeId != ElementId.InvalidElementId)
                {
                    var viewPlan = ViewPlan.Create(_doc, floorPlanTypeId, newLevel.Id);
                    viewPlan.Name = newLevel.Name;
                }

                levels.Add(newLevel);
            }

            return levels;
        }

        private ElementId GetFloorPlanTypeId()
        {
            var viewFamilyTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(v => v.ViewFamily == ViewFamily.FloorPlan);

            return viewFamilyTypes.FirstOrDefault()?.Id ?? ElementId.InvalidElementId;
        }
    }

    /// <summary>
    /// 框架构建服务 - 核心生成逻辑
    /// </summary>
    public class FrameBuilderService
    {
        private readonly Document _doc;
        private readonly FrameConfiguration _config;
        private readonly Autodesk.Revit.Creation.Document _creator;

        public FrameBuilderService(Document doc, FrameConfiguration config)
        {
            _doc = doc;
            _config = config;
            _creator = _doc.Create;
        }

        /// <summary>
        /// 创建完整框架
        /// </summary>
        public void Build(List<Level> levels, FamilySymbol columnType, FamilySymbol beamType, FamilySymbol braceType)
        {
            var transaction = new Transaction(_doc, "创建结构框架");
            transaction.Start();

            // 创建坐标矩阵
            var matrix = CreateMatrix();

            // 为每个楼层创建框架
            for (int floor = 0; floor < _config.FloorNumber; floor++)
            {
                var baseLevel = levels[floor];
                var topLevel = levels[floor + 1];

                // 1. 创建柱子
                CreateColumns(matrix, columnType, baseLevel, topLevel);

                // 2. 创建梁
                CreateBeams(matrix, beamType, topLevel);

                // 3. 创建支撑
                CreateBraces(matrix, braceType, baseLevel, topLevel);
            }

            transaction.Commit();
        }

        /// <summary>
        /// 创建坐标矩阵 (UV 网格)
        /// </summary>
        private UV[,] CreateMatrix()
        {
            var matrix = new UV[_config.XNumber, _config.YNumber];
            for (int i = 0; i < _config.XNumber; i++)
                for (int j = 0; j < _config.YNumber; j++)
                    matrix[i, j] = new UV(i * _config.Distance, j * _config.Distance);
            return matrix;
        }

        /// <summary>
        /// 创建所有柱子
        /// </summary>
        private void CreateColumns(UV[,] matrix, FamilySymbol type, Level baseLevel, Level topLevel)
        {
            if (!type.IsActive) type.Activate();

            foreach (UV point in matrix)
            {
                var location = new XYZ(point.U, point.V, 0);
                var column = _creator.NewFamilyInstance(location, type, baseLevel, StructuralType.Column);

                // 设置柱子的顶部和底部标高
                SetParameter(column, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topLevel.Id);
                SetParameter(column, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, baseLevel.Id);
                SetParameter(column, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM, 0.0);
                SetParameter(column, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM, 0.0);
            }
        }

        /// <summary>
        /// 创建梁
        /// </summary>
        private void CreateBeams(UV[,] matrix, FamilySymbol type, Level level)
        {
            if (!type.IsActive) type.Activate();
            var height = level.Elevation;

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    // X 方向梁
                    if (i < matrix.GetLength(0) - 1)
                        CreateBeam(matrix[i, j], matrix[i + 1, j], type, level, height);

                    // Y 方向梁
                    if (j < matrix.GetLength(1) - 1)
                        CreateBeam(matrix[i, j], matrix[i, j + 1], type, level, height);
                }
            }
        }

        /// <summary>
        /// 创建单根梁
        /// </summary>
        private void CreateBeam(UV p1, UV p2, FamilySymbol type, Level level, double height)
        {
            var start = new XYZ(p1.U, p1.V, height);
            var end = new XYZ(p2.U, p2.V, height);
            var line = Line.CreateBound(start, end);
            _creator.NewFamilyInstance(line, type, level, StructuralType.Beam);
        }

        /// <summary>
        /// 创建 X 形支撑（每对柱子之间创建两根对角支撑）
        /// </summary>
        private void CreateBraces(UV[,] matrix, FamilySymbol type, Level baseLevel, Level topLevel)
        {
            if (!type.IsActive) type.Activate();

            var topHeight = topLevel.Elevation;
            var baseHeight = baseLevel.Elevation;
            var midHeight = (topHeight + baseHeight) / 2;  // 柱子中点高度

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    // X 方向支撑
                    if (i < matrix.GetLength(0) - 1)
                        CreateBracePair(matrix[i, j], matrix[i + 1, j], type, topLevel, midHeight, topHeight);

                    // Y 方向支撑
                    if (j < matrix.GetLength(1) - 1)
                        CreateBracePair(matrix[i, j], matrix[i, j + 1], type, topLevel, midHeight, topHeight);
                }
            }
        }

        /// <summary>
        /// 创建一对 X 形支撑
        /// </summary>
        /// <param name="p1">第一个柱子的平面坐标</param>
        /// <param name="p2">第二个柱子的平面坐标</param>
        /// <param name="type">支撑类型</param>
        /// <param name="level">参考标高</param>
        /// <param name="midHeight">柱子中点高度（支撑起点/终点）</param>
        /// <param name="topHeight">顶部高度（交叉点高度）</param>
        private void CreateBracePair(UV p1, UV p2, FamilySymbol type, Level level, double midHeight, double topHeight)
        {
            // 计算三个关键点
            var startPoint = new XYZ(p1.U, p1.V, midHeight);      // 左柱中点
            var endPoint = new XYZ(p2.U, p2.V, midHeight);        // 右柱中点
            var midPoint = new XYZ((p1.U + p2.U) / 2, (p1.V + p2.V) / 2, topHeight);  // 顶部中心点

            // 支撑1: 从左柱中点到顶部中心
            var line1 = Line.CreateBound(startPoint, midPoint);
            _creator.NewFamilyInstance(line1, type, level, StructuralType.Brace);

            // 支撑2: 从右柱中点到顶部中心
            var line2 = Line.CreateBound(endPoint, midPoint);
            _creator.NewFamilyInstance(line2, type, level, StructuralType.Brace);
        }

        /// <summary>
        /// 设置 ElementId 类型参数
        /// </summary>
        private static void SetParameter(Element elem, BuiltInParameter param, ElementId value)
        {
            var para = elem.get_Parameter(param);
            if (para != null && para.StorageType == StorageType.ElementId && !para.IsReadOnly)
                para.Set(value);
        }

        /// <summary>
        /// 设置 double 类型参数
        /// </summary>
        private static void SetParameter(Element elem, BuiltInParameter param, double value)
        {
            var para = elem.get_Parameter(param);
            if (para != null && para.StorageType == StorageType.Double && !para.IsReadOnly)
                para.Set(value);
        }
    }
    /// <summary>
    /// 框架配置数据模型
    /// </summary>
    public class FrameConfiguration : ObserverableObject
    {
        // 常量定义
        public const int XNumberMin = 2, XNumberMax = 50;
        public const int YNumberMin = 2, YNumberMax = 50;
        public const int FloorNumberMin = 1, FloorNumberMax = 200;
        public const double DistanceMin = 1, DistanceMax = 3000;
        public const double LevelHeightMin = 1, LevelHeightMax = 100;
        public const int TotalMaxColumns = 200;

        private int _xNumber = 3;
        private int _yNumber = 3;
        private double _distance = 5;
        private int _floorNumber = 1;
        private double _levelHeight = 10;
        private double _originX;
        private double _originY;
        private double _rotateAngle;

        /// <summary>X方向柱数量 (2-50)</summary>
        public int XNumber
        {
            get => _xNumber;
            set { _xNumber = value; OnPropertyChanged(); ValidateTotal(); }
        }

        /// <summary>Y方向柱数量 (2-50)</summary>
        public int YNumber
        {
            get => _yNumber;
            set { _yNumber = value; OnPropertyChanged(); ValidateTotal(); }
        }

        /// <summary>柱间距 (英尺)</summary>
        public double Distance
        {
            get => _distance;
            set { _distance = value; OnPropertyChanged(); }
        }

        /// <summary>楼层数量</summary>
        public int FloorNumber
        {
            get => _floorNumber;
            set { _floorNumber = value; OnPropertyChanged(); ValidateTotal(); }
        }

        /// <summary>自动生成楼层的层高 (英尺)</summary>
        public double LevelHeight
        {
            get => _levelHeight;
            set { _levelHeight = value; OnPropertyChanged(); }
        }

        /// <summary>框架原点 X 坐标</summary>
        public double OriginX
        {
            get => _originX;
            set { _originX = value; OnPropertyChanged(); }
        }

        /// <summary>框架原点 Y 坐标</summary>
        public double OriginY
        {
            get => _originY;
            set { _originY = value; OnPropertyChanged(); }
        }

        /// <summary>整体旋转角度 (弧度)</summary>
        public double RotateAngle
        {
            get => _rotateAngle;
            set { _rotateAngle = value; OnPropertyChanged(); }
        }

        /// <summary>总柱子数量计算</summary>
        public int TotalColumns => (XNumber * YNumber) * (FloorNumber - 1);

        /// <summary>配置是否有效</summary>
        public bool IsValid => TotalColumns <= TotalMaxColumns;

        private void ValidateTotal()
        {
            if (TotalColumns > TotalMaxColumns)
                throw new System.Exception($"总柱子数量不能超过 {TotalMaxColumns}");
            OnPropertyChanged(nameof(TotalColumns));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    /// <summary>
    /// 构件类型项
    /// </summary>
    public class FamilySymbolItem
    {
        public FamilySymbol Symbol { get; set; }
        public string Name => Symbol?.Name ?? "未选择";
        public override string ToString() => Name;
    }

}
