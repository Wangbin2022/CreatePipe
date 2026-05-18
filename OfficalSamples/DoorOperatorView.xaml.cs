using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DoorOperatorView.xaml 的交互逻辑
    /// </summary>
    public partial class DoorOperatorView : Window
    {
        private readonly DoorOperationHandler _operationHandler;
        private readonly DoorOperatorViewModel _viewModel;
        public DoorOperatorView(UIApplication uIApplication)
        {
            InitializeComponent();
            // 创建外部事件处理器和视图模型
            _operationHandler = new DoorOperationHandler();
            _viewModel = new DoorOperatorViewModel(uIApplication, _operationHandler);

            // 绑定数据上下文
            DataContext = _viewModel;

            // 窗口关闭时清理资源
            Closed += (s, e) =>
            {
                // 外部事件会在Revit关闭时自动清理
                _viewModel?.LoadDoorsCommand?.Execute(null);
            };

            // 自动加载门列表
            Loaded += (s, e) => _viewModel.LoadDoorsCommand.Execute(null);
        }
    }
    /// <summary>
    /// 主视图模型 - 遵循MVVM模式
    /// </summary>
    public class DoorOperatorViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly DoorOperationHandler _operationHandler;
        private ObservableCollection<DoorModel> _doors;
        private DoorModel _selectedDoor;
        private string _statusMessage;
        private bool _isProcessing;

        public DoorOperatorViewModel(UIApplication uiApp, DoorOperationHandler handler)
        {
            _uiApp = uiApp;
            _operationHandler = handler;
            Doors = new ObservableCollection<DoorModel>();

            // 初始化命令
            LoadDoorsCommand = new BaseBindingCommand(_ => LoadDoors(), _ => !IsProcessing);
            DeleteCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.Delete), CanExecuteOperation);
            FlipHandCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.FlipHand), CanExecuteOperation);
            FlipFacingCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.FlipFacing), CanExecuteOperation);
            MakeLeftCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.MakeLeft), CanExecuteOperation);
            MakeRightCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.MakeRight), CanExecuteOperation);
            TurnInCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.TurnIn), CanExecuteOperation);
            TurnOutCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.TurnOut), CanExecuteOperation);
            RotateCommand = new BaseBindingCommand(_ => ExecuteOperation(DoorOperationRequest.Rotate), CanExecuteOperation);
            RefreshCommand = new BaseBindingCommand(_ => LoadDoors(), _ => !IsProcessing);
        }

        public ObservableCollection<DoorModel> Doors
        {
            get => _doors;
            set { _doors = value; OnPropertyChanged(); }
        }

        public DoorModel SelectedDoor
        {
            get => _selectedDoor;
            set { _selectedDoor = value; OnPropertyChanged(); }
        }

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
                // 刷新命令可用状态
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // 命令定义
        public ICommand LoadDoorsCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand FlipHandCommand { get; }
        public ICommand FlipFacingCommand { get; }
        public ICommand MakeLeftCommand { get; }
        public ICommand MakeRightCommand { get; }
        public ICommand TurnInCommand { get; }
        public ICommand TurnOutCommand { get; }
        public ICommand RotateCommand { get; }
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 加载当前文档中的所有门构件
        /// </summary>
        private void LoadDoors()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "正在加载门构件...";

                var doc = _uiApp.ActiveUIDocument.Document;
                var doorCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .Cast<FamilyInstance>();

                Doors.Clear();
                foreach (var door in doorCollector)
                {
                    Doors.Add(new DoorModel
                    {
                        Id = door.Id,
                        Name = door.Name,
                        FamilyName = door.Symbol.Family.Name,
                        TypeName = door.Symbol.Name,
                        IsSelected = false
                    });
                }

                StatusMessage = $"加载完成，共找到 {Doors.Count} 个门构件";
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

        /// <summary>
        /// 检查是否可以执行操作（有选中的门且未在处理中）
        /// </summary>
        private bool CanExecuteOperation(object parameter) =>
            SelectedDoor != null && !IsProcessing;

        /// <summary>
        /// 执行门操作（通过外部事件机制）
        /// </summary>
        private void ExecuteOperation(DoorOperationRequest request)
        {
            if (SelectedDoor == null) return;

            IsProcessing = true;
            StatusMessage = $"正在执行操作: {GetOperationName(request)}...";

            // 设置操作参数并触发外部事件
            _operationHandler.SetOperation(SelectedDoor.Id, request);
            _operationHandler.Raise();

            // 操作完成后刷新状态
            _operationHandler.OperationCompleted += OnOperationCompleted;
        }

        private void OnOperationCompleted(bool success, string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = success ? message : $"操作失败: {message}";
                IsProcessing = false;

                // 刷新门列表以显示最新状态
                LoadDoors();

                _operationHandler.OperationCompleted -= OnOperationCompleted;
            });
        }

        private string GetOperationName(DoorOperationRequest request)
        {
            switch (request)
            {
                case DoorOperationRequest.Delete:
                    return "删除";
                case DoorOperationRequest.FlipHand:
                    return "翻转把手";
                case DoorOperationRequest.FlipFacing:
                    return "翻转朝向";
                case DoorOperationRequest.MakeLeft:
                    return "设置为左开";
                case DoorOperationRequest.MakeRight:
                    return "设置为右开";
                case DoorOperationRequest.TurnIn:
                    return "设置为内开";
                case DoorOperationRequest.TurnOut:
                    return "设置为外开";
                case DoorOperationRequest.Rotate:
                    return "旋转";
                default:
                    return "未知操作";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 门构件数据模型
    /// </summary>
    public class DoorModel : ObserverableObject
    {
        private ElementId _id;
        private string _name;
        private string _familyName;
        private string _typeName;
        private bool _isSelected;

        public ElementId Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string FamilyName
        {
            get => _familyName;
            set { _familyName = value; OnPropertyChanged(); }
        }

        public string TypeName
        {
            get => _typeName;
            set { _typeName = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        public string DisplayName => $"{FamilyName} : {TypeName} ({Name})";
    }

    /// <summary>
    /// 门操作请求枚举
    /// </summary>
    public enum DoorOperationRequest
    {
        None,
        Delete,
        FlipHand,       // 翻转把手方向 Left/Right
        FlipFacing,     // 翻转朝向 In/Out
        MakeLeft,       // 设置为左开
        MakeRight,      // 设置为右开
        TurnIn,         // 设置为向内开
        TurnOut,        // 设置为向外开
        Rotate          // 旋转180度
    }

    /// <summary>
    /// 门操作外部事件处理器 - 负责在Revit API上下文中执行实际操作
    /// </summary>
    public class DoorOperationHandler : IExternalEventHandler
    {
        private readonly ExternalEvent _externalEvent;
        private ElementId _targetDoorId;
        private DoorOperationRequest _pendingRequest;
        private bool _hasPendingRequest;

        /// <summary>
        /// 操作完成事件
        /// </summary>
        public event Action<bool, string> OperationCompleted;

        public DoorOperationHandler()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        /// <summary>
        /// 设置待执行的操作
        /// </summary>
        public void SetOperation(ElementId doorId, DoorOperationRequest request)
        {
            _targetDoorId = doorId;
            _pendingRequest = request;
            _hasPendingRequest = true;
        }

        /// <summary>
        /// 触发外部事件
        /// </summary>
        public void Raise() => _externalEvent.Raise();

        /// <summary>
        /// 外部事件执行入口 - 在Revit API的有效上下文中运行
        /// </summary>
        public void Execute(UIApplication app)
        {
            bool success = false;
            string message = string.Empty;

            try
            {
                if (!_hasPendingRequest)
                {
                    message = "无待处理的操作";
                    return;
                }

                var doc = app.ActiveUIDocument.Document;
                var door = doc.GetElement(_targetDoorId) as FamilyInstance;

                if (door == null)
                {
                    message = "未找到目标门构件，可能已被删除";
                    return;
                }

                // 在事务中执行操作
                using (var trans = new Transaction(doc, $"门操作 - {GetOperationName(_pendingRequest)}"))
                {
                    if (trans.Start() == TransactionStatus.Started)
                    {
                        success = ExecuteDoorOperation(door, _pendingRequest);
                        if (success)
                        {
                            trans.Commit();
                            message = $"操作成功完成: {GetOperationName(_pendingRequest)}";
                        }
                        else
                        {
                            trans.RollBack();
                            message = "操作执行失败";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            finally
            {
                _hasPendingRequest = false;
                OperationCompleted?.Invoke(success, message);
            }
        }

        /// <summary>
        /// 执行具体的门操作
        /// </summary>
        private bool ExecuteDoorOperation(FamilyInstance door, DoorOperationRequest request)
        {
            try
            {
                switch (request)
                {
                    case DoorOperationRequest.Delete:
                        door.Document.Delete(door.Id);
                        return true;

                    case DoorOperationRequest.FlipHand:
                        door.flipHand();
                        return true;

                    case DoorOperationRequest.FlipFacing:
                        door.flipFacing();
                        return true;

                    case DoorOperationRequest.MakeLeft:
                        // 左开门：HandFlipped XOR FacingFlipped 应为 true
                        if (door.HandFlipped ^ door.FacingFlipped == false)
                            door.flipHand();
                        return true;

                    case DoorOperationRequest.MakeRight:
                        // 右开门：HandFlipped XOR FacingFlipped 应为 false
                        if (door.HandFlipped ^ door.FacingFlipped)
                            door.flipHand();
                        return true;

                    case DoorOperationRequest.TurnIn:
                        // 内开：FacingFlipped 应为 false
                        if (door.FacingFlipped)
                            door.flipFacing();
                        return true;

                    case DoorOperationRequest.TurnOut:
                        // 外开：FacingFlipped 应为 true
                        if (!door.FacingFlipped)
                            door.flipFacing();
                        return true;

                    case DoorOperationRequest.Rotate:
                        // 旋转180度：同时翻转两个方向
                        door.flipHand();
                        door.flipFacing();
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        private string GetOperationName(DoorOperationRequest request)
        {
            switch (request)
            {
                case DoorOperationRequest.Delete:
                    return "删除";
                case DoorOperationRequest.FlipHand:
                    return "翻转把手";
                case DoorOperationRequest.FlipFacing:
                    return "翻转朝向";
                case DoorOperationRequest.MakeLeft:
                    return "设置为左开";
                case DoorOperationRequest.MakeRight:
                    return "设置为右开";
                case DoorOperationRequest.TurnIn:
                    return "设置为内开";
                case DoorOperationRequest.TurnOut:
                    return "设置为外开";
                case DoorOperationRequest.Rotate:
                    return "旋转";
                default:
                    return "未知操作";
            }
        }

        public string GetName() => "门操作外部事件处理器";
    }
}
