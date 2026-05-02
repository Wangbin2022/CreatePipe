using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// JournalingView.xaml 的交互逻辑
    /// </summary>
    public partial class JournalingView : Window
    {
        public JournalingView(ExternalCommandData commandData)
        {
            InitializeComponent();
            this.DataContext=new JournalingViewModel(commandData);
        }
    }
    /// <summary>
    /// 主视图模型 - 管理墙体创建和日志数据
    /// </summary>
    public class JournalingViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly ExternalCommandData _commandData;
        private readonly WallCreationModel _model;
        private readonly JournalDataService _journalService;

        #region 属性

        private ObservableCollection<WallType> _wallTypes;
        public ObservableCollection<WallType> WallTypes
        {
            get => _wallTypes;
            set => SetProperty(ref _wallTypes, value);
        }

        private ObservableCollection<Level> _levels;
        public ObservableCollection<Level> Levels
        {
            get => _levels;
            set => SetProperty(ref _levels, value);
        }

        private WallType _selectedWallType;
        public WallType SelectedWallType
        {
            get => _selectedWallType;
            set => SetProperty(ref _selectedWallType, value);
        }

        private Level _selectedLevel;
        public Level SelectedLevel
        {
            get => _selectedLevel;
            set => SetProperty(ref _selectedLevel, value);
        }

        private double _startX;
        public double StartX
        {
            get => _startX;
            set => SetProperty(ref _startX, value);
        }

        private double _startY;
        public double StartY
        {
            get => _startY;
            set => SetProperty(ref _startY, value);
        }

        private double _endX;
        public double EndX
        {
            get => _endX;
            set => SetProperty(ref _endX, value);
        }

        private double _endY;
        public double EndY
        {
            get => _endY;
            set => SetProperty(ref _endY, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private bool _useJournalData;
        public bool UseJournalData
        {
            get => _useJournalData;
            set => SetProperty(ref _useJournalData, value);
        }

        #endregion

        #region 命令

        public ICommand CreateWallCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        public JournalingViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _uiApp = commandData.Application;
            _model = new WallCreationModel(_uiApp);
            _journalService = new JournalDataService(commandData);

            // 初始化命令
            CreateWallCommand = new BaseBindingCommand(_ => CreateWall(), _ => CanCreateWall());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow());

            // 加载数据
            LoadData();

            // 检查是否有日志数据
            if (_journalService.HasJournalData)
            {
                LoadFromJournal();
                UseJournalData = true;
            }
        }

        /// <summary>
        /// 加载壁类型和标高数据
        /// </summary>
        private void LoadData()
        {
            WallTypes = _model.GetWallTypes();
            Levels = _model.GetLevels();

            // 设置默认选择
            if (WallTypes.Count > 0) SelectedWallType = WallTypes[0];
            if (Levels.Count > 0) SelectedLevel = Levels[0];

            // 设置默认坐标
            StartX = 0;
            StartY = 0;
            EndX = 20;
            EndY = 0;
        }

        /// <summary>
        /// 从日志加载参数
        /// </summary>
        private void LoadFromJournal()
        {
            try
            {
                var (wallTypeName, levelId, startPoint, endPoint) = _journalService.ReadParameters();

                // 查找对应的壁类型
                SelectedWallType = _model.GetWallTypeByName(wallTypeName);

                // 查找对应的标高
                SelectedLevel = _model.GetLevelById(levelId);

                // 设置坐标
                StartX = startPoint.X;
                StartY = startPoint.Y;
                EndX = endPoint.X;
                EndY = endPoint.Y;

                StatusMessage = "已从日志加载参数，将自动创建墙体";
            }
            catch (Exception ex)
            {
                StatusMessage = $"读取日志失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 创建墙体
        /// </summary>
        private async void CreateWall()
        {
            IsBusy = true;
            StatusMessage = "正在创建墙体...";

            try
            {
                // 构建创建参数
                var parameters = new WallCreationParams
                {
                    StartPoint = new XYZ(StartX, StartY, 0),
                    EndPoint = new XYZ(EndX, EndY, 0),
                    LevelId = SelectedLevel?.Id ?? throw new InvalidOperationException("请选择标高"),
                    LevelElevation = SelectedLevel?.Elevation ?? 0,
                    WallTypeId = SelectedWallType?.Id ?? throw new InvalidOperationException("请选择墙体类型")
                };

                // 验证参数
                if (!parameters.IsValid)
                {
                    StatusMessage = "起点和终点不能相同";
                    return;
                }

                // 在事务中创建墙体
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (var transaction = new Transaction(_model.GetDocument(), "创建墙体"))
                    {
                        transaction.Start();
                        var wall = _model.CreateWall(parameters);
                        transaction.Commit();

                        // 写入日志数据（首次运行时）
                        if (!_journalService.HasJournalData)
                        {
                            _journalService.WriteParameters(
                                SelectedWallType.Name,
                                SelectedLevel.Id.IntegerValue,
                                parameters.StartPoint,
                                parameters.EndPoint);
                        }
                    }
                });

                StatusMessage = $"墙体创建成功！类型: {SelectedWallType?.Name}, 标高: {SelectedLevel?.Name}";
                MessageBox.Show("墙体创建成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
                MessageBox.Show($"墙体创建失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 检查是否可以创建墙体
        /// </summary>
        private bool CanCreateWall() =>
            !IsBusy && SelectedWallType != null && SelectedLevel != null;

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(bool result = false)
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        } 
    }
    /// <summary>
    /// 日志数据服务 - 负责读写Revit日志数据
    /// </summary>
    public class JournalDataService
    {
        // 日志键名常量
        private const string KEY_WALL_TYPE_NAME = "Wall Type Name";
        private const string KEY_LEVEL_ID = "Level Id";
        private const string KEY_START_POINT = "Start Point";
        private const string KEY_END_POINT = "End Point";

        private readonly ExternalCommandData _commandData;
        private readonly IDictionary<string, string> _journalData;

        public bool HasJournalData => _journalData?.Count > 0;

        public JournalDataService(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _journalData = commandData.JournalData;
        }

        /// <summary>
        /// 读取墙体创建参数（使用元组返回多个值）
        /// </summary>
        public (string wallTypeName, int levelId, XYZ startPoint, XYZ endPoint) ReadParameters()
        {
            // 使用本地函数简化键值获取
            string GetValue(string key) => GetSpecialData(_journalData, key);

            string wallTypeName = GetValue(KEY_WALL_TYPE_NAME);
            int levelId = int.Parse(GetValue(KEY_LEVEL_ID), CultureInfo.InvariantCulture);
            XYZ startPoint = StringToXYZ(GetValue(KEY_START_POINT));
            XYZ endPoint = StringToXYZ(GetValue(KEY_END_POINT));

            return (wallTypeName, levelId, startPoint, endPoint);
        }

        /// <summary>
        /// 写入墙体创建参数
        /// </summary>
        public void WriteParameters(string wallTypeName, int levelId, XYZ startPoint, XYZ endPoint)
        {
            _journalData.Clear();
            _journalData.Add(KEY_WALL_TYPE_NAME, wallTypeName);
            _journalData.Add(KEY_LEVEL_ID, levelId.ToString(CultureInfo.InvariantCulture));
            _journalData.Add(KEY_START_POINT, XYZToString(startPoint));
            _journalData.Add(KEY_END_POINT, XYZToString(endPoint));
        }

        /// <summary>
        /// XYZ坐标转字符串
        /// </summary>
        private static string XYZToString(XYZ point) =>
            $"({point.X.ToString(CultureInfo.InvariantCulture)},{point.Y.ToString(CultureInfo.InvariantCulture)},{point.Z.ToString(CultureInfo.InvariantCulture)})";

        /// <summary>
        /// 字符串转XYZ坐标
        /// </summary>
        private static XYZ StringToXYZ(string pointString)
        {
            // 使用字符串处理简化
            var trimmed = pointString.Trim('(', ')');
            var parts = trimmed.Split(',');

            if (parts.Length != 3)
                throw new InvalidOperationException("点的坐标格式不正确");

            double x = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double y = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double z = double.Parse(parts[2], CultureInfo.InvariantCulture);

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// 从字典获取数据（带验证）
        /// </summary>
        private static string GetSpecialData(IDictionary<string, string> dataMap, string key)
        {
            if (!dataMap.TryGetValue(key, out string value) || string.IsNullOrEmpty(value))
                throw new KeyNotFoundException($"日志中缺少键: {key}");

            return value;
        }
    }
    /// <summary>
    /// 墙体创建数据模型 - 封装Revit数据访问和墙体创建逻辑
    /// </summary>
    public class WallCreationModel
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;

        public WallCreationModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _document = uiApp.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取所有墙体类型（按名称排序）
        /// </summary>
        public ObservableCollection<WallType> GetWallTypes()
        {
            var collector = new FilteredElementCollector(_document);
            var wallTypes = collector.OfClass(typeof(WallType))
                .Cast<WallType>()
                .OrderBy(wt => wt.Name)
                .ToList();

            return new ObservableCollection<WallType>(wallTypes);
        }

        /// <summary>
        /// 获取所有标高
        /// </summary>
        public ObservableCollection<Level> GetLevels()
        {
            var collector = new FilteredElementCollector(_document);
            var levels = collector.OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            return new ObservableCollection<Level>(levels);
        }

        /// <summary>
        /// 创建墙体
        /// </summary>
        public Wall CreateWall(WallCreationParams parameters)
        {
            // 创建几何线
            var line = Autodesk.Revit.DB.Line.CreateBound(parameters.StartPoint, parameters.EndPoint);

            // 创建墙体
            var wall = Wall.Create(_document, line, parameters.WallTypeId,
                parameters.LevelId, 15, parameters.StartPoint.Z + parameters.LevelElevation,
                true, true);

            return wall;
        }

        /// <summary>
        /// 通过ID获取标高
        /// </summary>
        public Level GetLevelById(int id) =>
            _document.GetElement(new ElementId(id)) as Level;

        /// <summary>
        /// 通过名称获取墙体类型
        /// </summary>
        public WallType GetWallTypeByName(string name) =>
            GetWallTypes().FirstOrDefault(wt => wt.Name == name);
    }

    /// <summary>
    /// 墙体创建参数 - 使用元组风格的数据类
    /// </summary>
    public class WallCreationParams
    {
        public XYZ StartPoint { get; set; }
        public XYZ EndPoint { get; set; }
        public ElementId LevelId { get; set; }
        public double LevelElevation { get; set; }
        public ElementId WallTypeId { get; set; }

        public bool IsValid => StartPoint != null && EndPoint != null &&
                               !StartPoint.IsAlmostEqualTo(EndPoint);
    }
    /// <summary>
    /// 模型扩展方法
    /// </summary>
    public static class ModelExtensions
    {
        public static Document GetDocument(this WallCreationModel model)
        {
            // 通过反射获取私有字段（简化实现）
            var field = typeof(WallCreationModel).GetField("_document",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(model) as Document;
        }
    }
}
