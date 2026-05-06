using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
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
    /// TransactionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TransactionWindow : Window
    {
        private readonly TransactionViewModel _viewModel;
        public TransactionWindow(TransactionViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // 可以在这里处理树节点选中事件
        }
    }
    /// <summary>
    /// 事务演示视图模型
    /// </summary>
    public class TransactionViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly TransactionService _service;
        private TransactionTreeNode _selectedNode;
        private WallInfo _selectedWall;
        private bool _isTransactionActive;
        private bool _isTransactionGroupActive;
        private string _transactionStatus;
        private string _transactionGroupStatus;
        private CreateWallParameters _createWallParams;
        private XYZ _moveTranslation = new XYZ(5, 0, 0);
        private bool _showCreateWallDialog;
        #endregion

        #region 集合属性
        /// <summary>
        /// 日志条目集合
        /// </summary>
        public ObservableCollection<TransactionLogEntry> LogEntries => _service.LogEntries;

        /// <summary>
        /// 事务树节点
        /// </summary>
        public TransactionTreeNode RootNode => _service.RootNode;

        /// <summary>
        /// 选中的墙列表
        /// </summary>
        public ObservableCollection<WallInfo> SelectedWalls { get; set; }

        /// <summary>
        /// 墙类型列表
        /// </summary>
        public ObservableCollection<Element> WallTypes { get; set; }

        /// <summary>
        /// 标高列表
        /// </summary>
        public ObservableCollection<Level> Levels { get; set; }
        #endregion

        #region 绑定属性
        /// <summary>
        /// 选中的树节点
        /// </summary>
        public TransactionTreeNode SelectedNode
        {
            get => _selectedNode;
            set { _selectedNode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的墙
        /// </summary>
        public WallInfo SelectedWall
        {
            get => _selectedWall;
            set { _selectedWall = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 事务是否活动
        /// </summary>
        public bool IsTransactionActive
        {
            get => _isTransactionActive;
            set { _isTransactionActive = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 事务组是否活动
        /// </summary>
        public bool IsTransactionGroupActive
        {
            get => _isTransactionGroupActive;
            set { _isTransactionGroupActive = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 事务状态文本
        /// </summary>
        public string TransactionStatus
        {
            get => _transactionStatus;
            set { _transactionStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 事务组状态文本
        /// </summary>
        public string TransactionGroupStatus
        {
            get => _transactionGroupStatus;
            set { _transactionGroupStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 创建墙参数
        /// </summary>
        public CreateWallParameters CreateWallParams
        {
            get => _createWallParams;
            set { _createWallParams = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 移动偏移量
        /// </summary>
        public XYZ MoveTranslation
        {
            get => _moveTranslation;
            set { _moveTranslation = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 移动X偏移
        /// </summary>
        public double MoveX
        {
            get => MoveTranslation.X;
            set { MoveTranslation = new XYZ(value, MoveTranslation.Y, MoveTranslation.Z); }
        }

        /// <summary>
        /// 移动Y偏移
        /// </summary>
        public double MoveY
        {
            get => MoveTranslation.Y;
            set { MoveTranslation = new XYZ(MoveTranslation.X, value, MoveTranslation.Z); }
        }

        /// <summary>
        /// 移动Z偏移
        /// </summary>
        public double MoveZ
        {
            get => MoveTranslation.Z;
            set { MoveTranslation = new XYZ(MoveTranslation.X, MoveTranslation.Y, value); }
        }

        /// <summary>
        /// 显示创建墙对话框
        /// </summary>
        public bool ShowCreateWallDialog
        {
            get => _showCreateWallDialog;
            set { _showCreateWallDialog = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 创建墙对话框标题
        /// </summary>
        public string CreateWallDialogTitle => "创建墙";
        #endregion

        #region 命令
        // 事务组命令
        public ICommand StartTransactionGroupCommand;
        public ICommand CommitTransactionGroupCommand;
        public ICommand RollbackTransactionGroupCommand;

        // 事务命令
        public ICommand StartTransactionCommand;
        public ICommand CommitTransactionCommand;
        public ICommand RollbackTransactionCommand;

        // 墙操作命令
        public ICommand CreateWallCommand;
        public ICommand MoveWallCommand;
        public ICommand DeleteWallCommand;

        // 其他命令
        public ICommand RefreshWallsCommand;
        public ICommand ClearLogCommand;
        #endregion

        public TransactionViewModel(TransactionService service)
        {
            _service = service;

            SelectedWalls = new ObservableCollection<WallInfo>();
            WallTypes = new ObservableCollection<Element>();
            Levels = new ObservableCollection<Level>();
            CreateWallParams = new CreateWallParameters();

            // 初始化命令
            StartTransactionGroupCommand = new BaseBindingCommand(_ => ExecuteStartTransactionGroup("演示事务组"));
            CommitTransactionGroupCommand = new BaseBindingCommand(ExecuteCommitTransactionGroup, _ => IsTransactionGroupActive);
            RollbackTransactionGroupCommand = new BaseBindingCommand(ExecuteRollbackTransactionGroup, _=> IsTransactionGroupActive);

            StartTransactionCommand = new BaseBindingCommand(_ => ExecuteStartTransaction("演示事务"));
            CommitTransactionCommand = new BaseBindingCommand(ExecuteCommitTransaction,_ => IsTransactionActive);
            RollbackTransactionCommand = new BaseBindingCommand(ExecuteRollbackTransaction, _ => IsTransactionActive);

            CreateWallCommand = new BaseBindingCommand(ExecuteCreateWall);
            MoveWallCommand = new BaseBindingCommand(ExecuteMoveWall, _ => SelectedWall != null);
            DeleteWallCommand = new BaseBindingCommand(ExecuteDeleteWall, _ => SelectedWall != null);

            RefreshWallsCommand = new BaseBindingCommand(ExecuteRefreshWalls);
            ClearLogCommand = new BaseBindingCommand(ExecuteClearLog);

            // 加载数据
            LoadData();
        }

        #region 命令执行方法
        /// <summary>
        /// 开始事务组
        /// </summary>
        private void ExecuteStartTransactionGroup(string name)
        {
            if (_service.StartTransactionGroup(name))
            {
                IsTransactionGroupActive = true;
                TransactionGroupStatus = "事务组活动中";
            }
        }

        /// <summary>
        /// 提交事务组
        /// </summary>
        private void ExecuteCommitTransactionGroup(Object obj)
        {
            if (_service.CommitTransactionGroup())
            {
                IsTransactionGroupActive = false;
                TransactionGroupStatus = "已提交";
            }
        }

        /// <summary>
        /// 回滚事务组
        /// </summary>
        private void ExecuteRollbackTransactionGroup(Object obj)
        {
            if (_service.RollbackTransactionGroup())
            {
                IsTransactionGroupActive = false;
                TransactionGroupStatus = "已回滚";
            }
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        private void ExecuteStartTransaction(string name)
        {
            if (_service.StartTransaction(name))
            {
                IsTransactionActive = true;
                TransactionStatus = "事务活动中";
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        private void ExecuteCommitTransaction(Object obj)
        {
            if (_service.CommitTransaction())
            {
                IsTransactionActive = false;
                TransactionStatus = "已提交";
                ExecuteRefreshWalls(null);
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        private void ExecuteRollbackTransaction(Object obj)
        {
            if (_service.RollbackTransaction())
            {
                IsTransactionActive = false;
                TransactionStatus = "已回滚";
                ExecuteRefreshWalls(null);
            }
        }

        /// <summary>
        /// 创建墙
        /// </summary>
        private void ExecuteCreateWall(Object obj)
        {
            ShowCreateWallDialog = true;

            // 创建墙
            if (_service.CreateWall(CreateWallParams))
            {
                ExecuteRefreshWalls(null);
            }

            ShowCreateWallDialog = false;
        }

        /// <summary>
        /// 移动墙
        /// </summary>
        private void ExecuteMoveWall(Object obj)
        {
            if (_service.MoveWall(SelectedWall?.Wall, MoveTranslation))
            {
                ExecuteRefreshWalls(null);
            }
        }

        /// <summary>
        /// 删除墙
        /// </summary>
        private void ExecuteDeleteWall(Object obj)
        {
            if (_service.DeleteWall(SelectedWall?.Wall))
            {
                ExecuteRefreshWalls(null);
            }
        }

        /// <summary>
        /// 刷新墙列表
        /// </summary>
        private void ExecuteRefreshWalls(Object obj)
        {
            var walls = _service.GetSelectedWalls();
            SelectedWalls.Clear();
            foreach (var wall in walls)
                SelectedWalls.Add(wall);
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        private void ExecuteClearLog(Object obj)
        {
            LogEntries.Clear();
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            var wallTypes = _service.GetWallTypes();
            WallTypes.Clear();
            foreach (var type in wallTypes)
                WallTypes.Add(type);

            var levels = _service.GetLevels();
            Levels.Clear();
            foreach (var level in levels)
                Levels.Add(level);

            ExecuteRefreshWalls(null);
        }
        #endregion
    }
    /// <summary>
    /// 事务管理服务类
    /// </summary>
    public class TransactionService
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;

        // 当前活动的事务和事务组
        private Transaction _currentTransaction;
        private TransactionGroup _currentTransactionGroup;

        // 日志和树节点
        public ObservableCollection<TransactionLogEntry> LogEntries { get; }
        public TransactionTreeNode RootNode { get; set; }

        public TransactionService(ExternalCommandData commandData)
        {
            _uiDocument = commandData?.Application?.ActiveUIDocument;
            _document = _uiDocument?.Document;

            LogEntries = new ObservableCollection<TransactionLogEntry>();
            RootNode = new TransactionTreeNode("事务结构");
        }

        #region 事务组操作
        /// <summary>
        /// 开始事务组
        /// </summary>
        public bool StartTransactionGroup(string name)
        {
            try
            {
                if (_currentTransactionGroup != null)
                {
                    AddLog("已有活动的事务组，请先提交或回滚", LogEntryType.Warning);
                    return false;
                }

                _currentTransactionGroup = new TransactionGroup(_document, name);
                _currentTransactionGroup.Start();

                var node = new TransactionTreeNode($"事务组: {name}");
                RootNode.Children.Add(node);
                RootNode = node; // 简化处理

                AddLog($"开始事务组: {name}", LogEntryType.Success);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"开始事务组失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 提交事务组
        /// </summary>
        public bool CommitTransactionGroup()
        {
            try
            {
                if (_currentTransactionGroup == null)
                {
                    AddLog("没有活动的事务组", LogEntryType.Warning);
                    return false;
                }

                _currentTransactionGroup.Commit();
                AddLog("提交事务组成功", LogEntryType.Success);
                _currentTransactionGroup = null;
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"提交事务组失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 回滚事务组
        /// </summary>
        public bool RollbackTransactionGroup()
        {
            try
            {
                if (_currentTransactionGroup == null)
                {
                    AddLog("没有活动的事务组", LogEntryType.Warning);
                    return false;
                }

                _currentTransactionGroup.RollBack();
                AddLog("回滚事务组成功", LogEntryType.Success);
                _currentTransactionGroup = null;
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"回滚事务组失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }
        #endregion

        #region 事务操作
        /// <summary>
        /// 开始事务
        /// </summary>
        public bool StartTransaction(string name)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    AddLog("已有活动的事务，请先提交或回滚", LogEntryType.Warning);
                    return false;
                }

                _currentTransaction = new Transaction(_document, name);
                _currentTransaction.Start();

                AddLog($"开始事务: {name}", LogEntryType.Success);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"开始事务失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public bool CommitTransaction()
        {
            try
            {
                if (_currentTransaction == null)
                {
                    AddLog("没有活动的事务", LogEntryType.Warning);
                    return false;
                }

                _currentTransaction.Commit();
                AddLog("提交事务成功", LogEntryType.Success);
                _currentTransaction = null;
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"提交事务失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public bool RollbackTransaction()
        {
            try
            {
                if (_currentTransaction == null)
                {
                    AddLog("没有活动的事务", LogEntryType.Warning);
                    return false;
                }

                _currentTransaction.RollBack();
                AddLog("回滚事务成功", LogEntryType.Success);
                _currentTransaction = null;
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"回滚事务失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }
        #endregion

        #region 墙操作
        /// <summary>
        /// 创建墙
        /// </summary>
        public bool CreateWall(CreateWallParameters parameters)
        {
            try
            {
                if (_currentTransaction == null)
                {
                    AddLog("请先开始事务", LogEntryType.Warning);
                    return false;
                }

                // 获取墙类型
                var wallType = _document.GetElement(parameters.WallTypeId) as WallType;
                if (wallType == null)
                {
                    AddLog("无效的墙类型", LogEntryType.Error);
                    return false;
                }

                // 获取标高
                var level = _document.GetElement(parameters.LevelId) as Level;
                if (level == null)
                {
                    AddLog("无效的标高", LogEntryType.Error);
                    return false;
                }

                // 创建墙
                var line = Autodesk.Revit.DB.Line.CreateBound(parameters.StartPoint, parameters.EndPoint);
                var wall = Wall.Create(_document, line, wallType.Id, level.Id,
                    parameters.StartZ, parameters.EndZ, false, false);

                AddLog($"创建墙成功: ID={wall.Id.IntegerValue}", LogEntryType.Success);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"创建墙失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 移动墙
        /// </summary>
        public bool MoveWall(Wall wall, XYZ translation)
        {
            try
            {
                if (_currentTransaction == null)
                {
                    AddLog("请先开始事务", LogEntryType.Warning);
                    return false;
                }

                if (wall == null)
                {
                    AddLog("请选择要移动的墙", LogEntryType.Warning);
                    return false;
                }

                var location = wall.Location as LocationCurve;
                if (location == null)
                {
                    AddLog("无法获取墙的位置信息", LogEntryType.Error);
                    return false;
                }

                location.Move(translation);
                AddLog($"移动墙成功: ID={wall.Id.IntegerValue}", LogEntryType.Success);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"移动墙失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 删除墙
        /// </summary>
        public bool DeleteWall(Wall wall)
        {
            try
            {
                if (_currentTransaction == null)
                {
                    AddLog("请先开始事务", LogEntryType.Warning);
                    return false;
                }

                if (wall == null)
                {
                    AddLog("请选择要删除的墙", LogEntryType.Warning);
                    return false;
                }

                _document.Delete(wall.Id);
                AddLog($"删除墙成功: ID={wall.Id.IntegerValue}", LogEntryType.Success);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"删除墙失败: {ex.Message}", LogEntryType.Error);
                return false;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取所有墙类型
        /// </summary>
        public ObservableCollection<Element> GetWallTypes()
        {
            var wallTypes = new ObservableCollection<Element>();
            var collector = new FilteredElementCollector(_document);

            var types = collector.OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();

            foreach (var type in types)
                wallTypes.Add(type);

            return wallTypes;
        }

        /// <summary>
        /// 获取所有标高
        /// </summary>
        public ObservableCollection<Level> GetLevels()
        {
            var levels = new ObservableCollection<Level>();
            var collector = new FilteredElementCollector(_document);

            var levelList = collector.OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            foreach (var level in levelList)
                levels.Add(level);

            return levels;
        }

        /// <summary>
        /// 获取选中的墙
        /// </summary>
        public ObservableCollection<WallInfo> GetSelectedWalls()
        {
            var walls = new ObservableCollection<WallInfo>();

            var selectedIds = _uiDocument.Selection.GetElementIds();
            foreach (var id in selectedIds)
            {
                if (_document.GetElement(id) is Wall wall)
                {
                    walls.Add(new WallInfo { Wall = wall });
                }
            }

            return walls;
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string message, LogEntryType type = LogEntryType.Info)
        {
            var entry = new TransactionLogEntry(message, type);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(entry);
                // 滚动到最新日志
                if (LogEntries.Count > 100)
                    LogEntries.RemoveAt(0);
            });
        }
        #endregion
    }
    /// <summary>
    /// 事务日志条目类型
    /// </summary>
    public enum LogEntryType
    {
        Info,
        Success,
        Error,
        Warning
    }

    /// <summary>
    /// 事务日志条目
    /// </summary>
    public class TransactionLogEntry : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        private string _message;
        private LogEntryType _entryType;

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public LogEntryType EntryType
        {
            get => _entryType;
            set { _entryType = value; OnPropertyChanged(); }
        }

        public string DisplayTime => Timestamp.ToString("HH:mm:ss");
        public string DisplayMessage => $"[{DisplayTime}] {Message}";

        public TransactionLogEntry(string message, LogEntryType type = LogEntryType.Info)
        {
            Timestamp = DateTime.Now;
            Message = message;
            EntryType = type;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 事务树节点模型
    /// </summary>
    public class TransactionTreeNode : INotifyPropertyChanged
    {
        private string _name;
        private string _status;
        private bool _isActive;
        private ObservableCollection<TransactionTreeNode> _children;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TransactionTreeNode> Children
        {
            get => _children;
            set { _children = value; OnPropertyChanged(); }
        }

        public TransactionTreeNode(string name)
        {
            Name = name;
            Status = "未开始";
            Children = new ObservableCollection<TransactionTreeNode>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 墙信息模型
    /// </summary>
    public class WallInfo : INotifyPropertyChanged
    {
        private Wall _wall;
        private string _displayName;
        private XYZ _startPoint;
        private XYZ _endPoint;

        public Wall Wall
        {
            get => _wall;
            set { _wall = value; OnPropertyChanged(); UpdateDisplayInfo(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public XYZ StartPoint
        {
            get => _startPoint;
            set { _startPoint = value; OnPropertyChanged(); }
        }

        public XYZ EndPoint
        {
            get => _endPoint;
            set { _endPoint = value; OnPropertyChanged(); }
        }

        public ElementId Id => _wall?.Id;

        private void UpdateDisplayInfo()
        {
            if (_wall != null && _wall.Location is LocationCurve loc)
            {
                StartPoint = loc.Curve.GetEndPoint(0);
                EndPoint = loc.Curve.GetEndPoint(1);
                DisplayName = $"墙 ID:{Id.IntegerValue} - {StartPoint.X:F2},{StartPoint.Y:F2} → {EndPoint.X:F2},{EndPoint.Y:F2}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 创建墙参数模型
    /// </summary>
    public class CreateWallParameters : INotifyPropertyChanged
    {
        private double _startX = 0;
        private double _startY = 0;
        private double _startZ = 0;
        private double _endX = 10;
        private double _endY = 0;
        private double _endZ = 0;
        private ElementId _levelId;
        private ElementId _wallTypeId;

        public double StartX
        {
            get => _startX;
            set { _startX = value; OnPropertyChanged(); }
        }

        public double StartY
        {
            get => _startY;
            set { _startY = value; OnPropertyChanged(); }
        }

        public double StartZ
        {
            get => _startZ;
            set { _startZ = value; OnPropertyChanged(); }
        }

        public double EndX
        {
            get => _endX;
            set { _endX = value; OnPropertyChanged(); }
        }

        public double EndY
        {
            get => _endY;
            set { _endY = value; OnPropertyChanged(); }
        }

        public double EndZ
        {
            get => _endZ;
            set { _endZ = value; OnPropertyChanged(); }
        }

        public ElementId LevelId
        {
            get => _levelId;
            set { _levelId = value; OnPropertyChanged(); }
        }

        public ElementId WallTypeId
        {
            get => _wallTypeId;
            set { _wallTypeId = value; OnPropertyChanged(); }
        }

        public XYZ StartPoint => new XYZ(StartX, StartY, StartZ);
        public XYZ EndPoint => new XYZ(EndX, EndY, EndZ);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
