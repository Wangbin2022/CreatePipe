using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Grid = Autodesk.Revit.DB.Grid;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CreateOrthogonalGridView.xaml 的交互逻辑
    /// </summary>
    public partial class CreateOrthogonalGridView : Window
    {
        public CreateOrthogonalGridView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new CreateOrthogonalGridViewModel(uIApplication);
        }
        ///// <summary>
        ///// 显示窗口并返回对话框结果
        ///// </summary>
        //public bool? ShowModal()
        //{
        //    Owner = System.Windows.Application.Current.MainWindow;
        //    return ShowDialog();
        //}
    }
    /// <summary>
    /// 正交轴线创建视图模型
    /// </summary>
    public class CreateOrthogonalGridViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly OrthogonalGridModel _model;

        // 字段定义
        private double _xOrigin;
        private double _yOrigin;
        private uint _xCount;
        private uint _yCount;
        private double _xSpacing;
        private double _ySpacing;
        private string _xFirstLabel;
        private string _yFirstLabel;
        private BubbleLocation _xBubbleLoc;
        private BubbleLocation _yBubbleLoc;
        private string _statusMessage;
        private bool _isBusy;

        // 气泡位置选项
        public ObservableCollection<BubbleLocationItem> BubbleLocationOptions { get; }

        // 属性绑定
        public double XOrigin
        {
            get => _xOrigin;
            set { _xOrigin = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public double YOrigin
        {
            get => _yOrigin;
            set { _yOrigin = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public uint XCount
        {
            get => _xCount;
            set { _xCount = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public uint YCount
        {
            get => _yCount;
            set { _yCount = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public double XSpacing
        {
            get => _xSpacing;
            set { _xSpacing = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public double YSpacing
        {
            get => _ySpacing;
            set { _ySpacing = value; OnPropertyChanged(); ValidateInputs(); }
        }

        public string XFirstLabel
        {
            get => _xFirstLabel;
            set { _xFirstLabel = value; OnPropertyChanged(); ValidateLabel(value); }
        }

        public string YFirstLabel
        {
            get => _yFirstLabel;
            set { _yFirstLabel = value; OnPropertyChanged(); ValidateLabel(value); }
        }

        public BubbleLocation XBubbleLoc
        {
            get => _xBubbleLoc;
            set { _xBubbleLoc = value; OnPropertyChanged(); }
        }

        public BubbleLocation YBubbleLoc
        {
            get => _yBubbleLoc;
            set { _yBubbleLoc = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool CanCreate => !IsBusy && IsValid;

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set { _isValid = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCreate)); }
        }

        private bool _isXLabelValid = true;
        private bool _isYLabelValid = true;

        // 命令
        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public CreateOrthogonalGridViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _model = new OrthogonalGridModel(uiApp);

            // 初始化气泡位置选项
            BubbleLocationOptions = new ObservableCollection<BubbleLocationItem>
            {
                new BubbleLocationItem { Value = BubbleLocation.StartPoint, DisplayName = "起点" },
                new BubbleLocationItem { Value = BubbleLocation.EndPoint, DisplayName = "终点" }
            };

            // 从模型加载默认值
            LoadDefaultValues();

            // 初始化命令
            CreateCommand = new BaseBindingCommand(_ => CreateGrids(), _ => CanCreate);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow());

            // 初始验证
            ValidateInputs();
        }

        /// <summary>
        /// 加载默认值
        /// </summary>
        private void LoadDefaultValues()
        {
            XCount = _model.XCount;
            YCount = _model.YCount;
            XSpacing = _model.XSpacing;
            YSpacing = _model.YSpacing;
            XFirstLabel = _model.XFirstLabel;
            YFirstLabel = _model.YFirstLabel;
            XBubbleLoc = _model.XBubbleLoc;
            YBubbleLoc = _model.YBubbleLoc;
        }

        /// <summary>
        /// 验证输入参数
        /// </summary>
        private void ValidateInputs()
        {
            var result = _model.ValidateInputs();
            IsValid = result.IsValid;

            if (!IsValid)
                StatusMessage = result.Message;
            else
                StatusMessage = "就绪";
        }

        /// <summary>
        /// 验证标签唯一性
        /// </summary>
        private async void ValidateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return;

            var existingLabels = await System.Threading.Tasks.Task.Run(
                () => _model.GetExistingGridLabels());

            // 标签验证（简单规则）
            bool isValid = !existingLabels.Contains(label);

            // 更新对应的验证状态
            if (label == XFirstLabel)
                _isXLabelValid = isValid;
            if (label == YFirstLabel)
                _isYLabelValid = isValid;
        }

        /// <summary>
        /// 创建轴线
        /// </summary>
        private async void CreateGrids()
        {
            if (!IsValid) return;

            IsBusy = true;
            StatusMessage = "正在创建轴线...";

            try
            {
                // 将UI参数同步到模型
                SyncModel();

                // 异步执行创建操作
                var result = await System.Threading.Tasks.Task.Run(() => _model.CreateGrids());

                if (result.Success)
                {
                    StatusMessage = "轴线创建成功！";
                    MessageBox.Show($"成功创建 {XCount + YCount} 条轴线", "完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow(true);
                }
                else
                {
                    StatusMessage = $"创建失败: {result.Errors.FirstOrDefault()}";
                    MessageBox.Show($"轴线创建失败:\n{string.Join("\n", result.Errors)}",
                        "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"异常: {ex.Message}";
                MessageBox.Show($"创建轴线时发生异常:\n{ex.Message}", "异常",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 同步视图模型数据到数据模型
        /// </summary>
        private void SyncModel()
        {
            _model.XOrigin = XOrigin;
            _model.YOrigin = YOrigin;
            _model.XCount = XCount;
            _model.YCount = YCount;
            _model.XSpacing = XSpacing;
            _model.YSpacing = YSpacing;
            _model.XFirstLabel = XFirstLabel;
            _model.YFirstLabel = YFirstLabel;
            _model.XBubbleLoc = XBubbleLoc;
            _model.YBubbleLoc = YBubbleLoc;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(bool result = false)
        {
            var window = System.Windows.Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }

    /// <summary>
    /// 气泡位置选项包装类
    /// </summary>
    public class BubbleLocationItem
    {
        public BubbleLocation Value { get; set; }
        public string DisplayName { get; set; }
    }
    /// <summary>
    /// 气泡位置枚举
    /// </summary>
    public enum BubbleLocation
    {
        StartPoint,  // 气泡在起点
        EndPoint     // 气泡在终点
    }

    /// <summary>
    /// 正交轴线数据模型 - 封装Revit轴线创建逻辑
    /// </summary>
    public class OrthogonalGridModel
    {
        private readonly Document _document;
        private readonly UIApplication _uiApp;

        // 轴线创建参数
        public double XOrigin { get; set; }      // X方向原点
        public double YOrigin { get; set; }      // Y方向原点
        public uint XCount { get; set; }         // X方向轴线数量
        public uint YCount { get; set; }         // Y方向轴线数量
        public double XSpacing { get; set; }     // X方向间距
        public double YSpacing { get; set; }     // Y方向间距
        public string XFirstLabel { get; set; }  // X方向第一条轴线标签
        public string YFirstLabel { get; set; }  // Y方向第一条轴线标签
        public BubbleLocation XBubbleLoc { get; set; }  // X方向气泡位置
        public BubbleLocation YBubbleLoc { get; set; }  // Y方向气泡位置

        public OrthogonalGridModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _document = uiApp.ActiveUIDocument.Document;

            // 默认值
            XCount = 3;
            YCount = 3;
            XSpacing = 10.0;
            YSpacing = 10.0;
            XFirstLabel = "1";
            YFirstLabel = "A";
        }

        /// <summary>
        /// 创建正交轴线系统
        /// </summary>
        /// <returns>创建结果，包含成功/失败信息和错误列表</returns>
        public (bool Success, List<string> Errors) CreateGrids()
        {
            var errors = new List<string>();
            var curves = new List<Curve>();

            // 创建X方向轴线
            var xErrors = CreateXDirectionGrids(curves);
            errors.AddRange(xErrors);

            // 创建Y方向轴线
            var yErrors = CreateYDirectionGrids(curves);
            errors.AddRange(yErrors);

            // 批量创建轴线
            if (curves.Any())
            {
                using (var tran = new Transaction(_document, "创建正交轴线"))
                {
                    tran.Start();

                    foreach (var curve in curves)
                    {
                        Line line = Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(1));
                        try
                        {
                            Grid.Create(_document, line);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"创建轴线失败: {ex.Message}");
                        }
                    }

                    tran.Commit();
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 创建X方向（水平）轴线
        /// </summary>
        private List<string> CreateXDirectionGrids(List<Curve> curves)
        {
            var errors = new List<string>();

            for (int i = 0; i < XCount; i++)
            {
                try
                {
                    // 计算轴线端点
                    double yPos = YOrigin + i * XSpacing;
                    double xStart, xEnd;

                    if (YCount > 0)
                    {
                        // 有Y方向轴线时，扩展超出范围
                        xStart = XOrigin - YSpacing / 2;
                        xEnd = XOrigin + (YCount - 1) * YSpacing + YSpacing / 2;
                    }
                    else
                    {
                        xStart = XOrigin;
                        xEnd = XOrigin + XSpacing / 2;
                    }

                    var startPoint = new XYZ(xStart, yPos, 0);
                    var endPoint = new XYZ(xEnd, yPos, 0);

                    // 根据气泡位置决定线段方向
                    var line = XBubbleLoc == BubbleLocation.StartPoint
                        ? Line.CreateBound(startPoint, endPoint)
                        : Line.CreateBound(endPoint, startPoint);

                    curves.Add(line);
                }
                catch (Exception ex)
                {
                    errors.Add($"X方向第{i + 1}条轴线创建失败: {ex.Message}");
                }
            }

            return errors;
        }

        /// <summary>
        /// 创建Y方向（垂直）轴线
        /// </summary>
        private List<string> CreateYDirectionGrids(List<Curve> curves)
        {
            var errors = new List<string>();

            for (int i = 0; i < YCount; i++)
            {
                try
                {
                    // 计算轴线端点
                    double xPos = XOrigin + i * YSpacing;
                    double yStart, yEnd;

                    if (XCount > 0)
                    {
                        yStart = YOrigin - XSpacing / 2;
                        yEnd = YOrigin + (XCount - 1) * XSpacing + XSpacing / 2;
                    }
                    else
                    {
                        yStart = YOrigin;
                        yEnd = YOrigin + YSpacing / 2;
                    }

                    var startPoint = new XYZ(xPos, yStart, 0);
                    var endPoint = new XYZ(xPos, yEnd, 0);

                    var line = YBubbleLoc == BubbleLocation.StartPoint
                        ? Line.CreateBound(startPoint, endPoint)
                        : Line.CreateBound(endPoint, startPoint);

                    curves.Add(line);
                }
                catch (Exception ex)
                {
                    errors.Add($"Y方向第{i + 1}条轴线创建失败: {ex.Message}");
                }
            }

            return errors;
        }

        /// <summary>
        /// 获取文档中现有的轴线标签列表
        /// </summary>
        public List<string> GetExistingGridLabels()
        {
            var collector = new FilteredElementCollector(_document);
            var grids = collector.OfClass(typeof(Grid)).Cast<Grid>();
            return grids.Select(g => g.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
        }

        /// <summary>
        /// 验证标签是否唯一
        /// </summary>
        public bool IsLabelUnique(string label, List<string> existingLabels) =>
            !existingLabels.Contains(label);

        /// <summary>
        /// 验证创建参数
        /// </summary>
        public (bool IsValid, string Message) ValidateInputs()
        {
            if (XCount == 0 && YCount == 0)
                return (false, "X和Y方向的轴线数量不能同时为0");

            if (XCount > 0 && XSpacing <= 0)
                return (false, "X方向间距必须大于0");

            if (YCount > 0 && YSpacing <= 0)
                return (false, "Y方向间距必须大于0");

            return (true, null);
        }
    }
}
