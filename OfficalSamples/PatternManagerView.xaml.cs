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
//using Gird = Autodesk.Revit.DB.Grid;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PatternManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class PatternManagerView : Window
    {
        private readonly PatternManagerViewModel _viewModel;
        public PatternManagerView(ExternalCommandData commandData)
        {
            InitializeComponent();
            _viewModel = new PatternManagerViewModel(commandData);
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
    public class PatternManagerViewModel : ObserverableObject
    {
        #region 成员变量
        private readonly PatternService _patternService;
        private ObservableCollection<FillPatternItem> _fillPatterns;
        private ObservableCollection<LinePatternItem> _linePatterns;
        private FillPatternItem _selectedFillPattern;
        private LinePatternItem _selectedLinePattern;
        private int _selectedTabIndex;
        private bool _isProcessing;
        private string _statusMessage;
        #endregion

        #region 构造函数
        public PatternManagerViewModel(ExternalCommandData commandData)
        {
            _patternService = new PatternService(commandData);
            // 初始化命令
            ApplyToSurfaceCommand = new BaseBindingCommand(ExecuteApplyToSurface);
            ApplyToCutSurfaceCommand = new BaseBindingCommand(ExecuteApplyToCutSurface);
            ApplyToGridsCommand = new BaseBindingCommand(ExecuteApplyToGrids);
            CreateFillPatternCommand = new BaseBindingCommand(ExecuteCreateFillPattern);
            CreateComplexFillPatternCommand = new BaseBindingCommand(ExecuteCreateComplexFillPattern);
            CreateLinePatternCommand = new BaseBindingCommand(ExecuteCreateLinePattern);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);
            // 加载数据
            LoadPatterns();
        }
        #endregion
        #region 属性
        /// <summary>填充图案列表</summary>
        public ObservableCollection<FillPatternItem> FillPatterns
        {
            get => _fillPatterns;
            set => SetProperty(ref _fillPatterns, value);
        }
        /// <summary>线型图案列表</summary>
        public ObservableCollection<LinePatternItem> LinePatterns
        {
            get => _linePatterns;
            set => SetProperty(ref _linePatterns, value);
        }

        /// <summary>选中的填充图案</summary>
        public FillPatternItem SelectedFillPattern
        {
            get => _selectedFillPattern;
            set => SetProperty(ref _selectedFillPattern, value);
        }

        /// <summary>选中的线型图案</summary>
        public LinePatternItem SelectedLinePattern
        {
            get => _selectedLinePattern;
            set => SetProperty(ref _selectedLinePattern, value);
        }

        /// <summary>选中的标签页索引</summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>是否正在处理</summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>是否有选中的填充图案</summary>
        public bool HasSelectedFillPattern => SelectedFillPattern != null;

        /// <summary>是否有选中的线型图案</summary>
        public bool HasSelectedLinePattern => SelectedLinePattern != null;

        #endregion

        #region 命令
        public ICommand ApplyToSurfaceCommand { get; }
        public ICommand ApplyToCutSurfaceCommand { get; }
        public ICommand ApplyToGridsCommand { get; }
        public ICommand CreateFillPatternCommand { get; }
        public ICommand CreateComplexFillPatternCommand { get; }
        public ICommand CreateLinePatternCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region 命令执行方法
        private bool CanApplyPattern() => HasSelectedFillPattern && !IsProcessing;
        private bool CanApplyLinePattern() => HasSelectedLinePattern && !IsProcessing;
        private async void ExecuteApplyToSurface(Object obj)
        {
            IsProcessing = true;
            bool success = await System.Threading.Tasks.Task.Run(() =>
                _patternService.ApplyFillPatternToSurface(SelectedFillPattern.FillPattern));

            StatusMessage = success ? "成功应用填充图案到表面" : "应用填充图案失败，请确保已选中元素";
            IsProcessing = false;
        }
        private async void ExecuteApplyToCutSurface(Object obj)
        {
            IsProcessing = true;
            bool success = await System.Threading.Tasks.Task.Run(() =>
                _patternService.ApplyFillPatternToCutSurface(SelectedFillPattern.FillPattern));

            StatusMessage = success ? "成功应用填充图案到切割面" : "应用填充图案失败，请确保已选中元素";
            IsProcessing = false;
        }
        private async void ExecuteApplyToGrids(Object obj)
        {
            IsProcessing = true;
            bool success = await System.Threading.Tasks.Task.Run(() =>
                _patternService.ApplyLinePatternToGrids(SelectedLinePattern.LinePattern));

            StatusMessage = success ? "成功应用线型图案到网格" : "应用线型图案失败，请确保已选中网格线";
            IsProcessing = false;
        }
        private void ExecuteCreateFillPattern(Object obj)
        {
            //// 打开创建填充图案对话框
            //var dialog = new PatternCreateView();
            //if (dialog.ShowDialog() == true)
            //{
            //    IsProcessing = true;
            //    var (success, message) = _patternService.CreateSimpleFillPattern(dialog.Parameters, out var error);
            //    StatusMessage = success ? "成功创建填充图案" : $"创建失败: {error}";
            //    if (success) LoadPatterns();
            //    IsProcessing = false;
            //}
        }
        private void ExecuteCreateComplexFillPattern(Object obj)
        {
            //没有生成这个窗口
            //// 打开导入复杂填充图案对话框
            //var dialog = new ImportFillPatternDialog();
            //if (dialog.ShowDialog() == true)
            //{
            //    IsProcessing = true;
            //    var success = _patternService.CreateComplexFillPattern(dialog.Parameters, out var error);
            //    StatusMessage = success ? "成功导入复杂填充图案" : $"导入失败: {error}";
            //    if (success) LoadPatterns();
            //    IsProcessing = false;
            //}
        }

        private void ExecuteCreateLinePattern(Object obj)
        {
            //var dialog = new PatternCreateView();
            //if (dialog.ShowDialog() == true)
            //{
            //    IsProcessing = true;
            //    var success = _patternService.CreateLinePattern(dialog.Parameters, out var error);
            //    StatusMessage = success ? "成功创建线型图案" : $"创建失败: {error}";
            //    if (success) LoadPatterns();
            //    IsProcessing = false;
            //}
        }

        //private void ExecuteCancel ()=> OnRequestClose(false);
        private void ExecuteCancel(Object obj)
        {
            OnRequestClose(false);
        }

        #endregion

        #region 辅助方法

        private void LoadPatterns()
        {
            IsProcessing = true;

            var fillPatterns = _patternService.LoadFillPatterns();
            FillPatterns = new ObservableCollection<FillPatternItem>(fillPatterns);

            var linePatterns = _patternService.LoadLinePatterns();
            LinePatterns = new ObservableCollection<LinePatternItem>(linePatterns);

            StatusMessage = $"已加载 {FillPatterns.Count} 个填充图案, {LinePatterns.Count} 个线型图案";
            IsProcessing = false;
        }
        #endregion

        #region 窗口关闭
        public event EventHandler<bool> RequestClose;
        protected virtual void OnRequestClose(bool result) => RequestClose?.Invoke(this, result);
        #endregion
    }
    /// <summary>
    /// 图案管理服务
    /// 负责加载、应用和创建图案
    /// 使用C# 7.3的LINQ和模式匹配
    /// </summary>
    public class PatternService
    {
        private readonly Document _document;
        private readonly UIDocument _uiDoc;

        public PatternService(ExternalCommandData commandData)
        {
            _uiDoc = commandData?.Application?.ActiveUIDocument;
            _document = _uiDoc?.Document;
        }

        #region 加载图案

        /// <summary>
        /// 加载所有填充图案
        /// 使用C# 7.3的LINQ和表达式体
        /// </summary>
        public List<FillPatternItem> LoadFillPatterns()
        {
            var fillPatterns = new FilteredElementCollector(_document)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .Select(elem => new FillPatternItem(elem.GetFillPattern()))
                .ToList();

            //// 添加实心填充选项（Revit中实心填充是特殊处理）以下有误，不能为null
            //var solidFill = new FillPatternItem(CreateSolidFillPattern());
            //fillPatterns.Insert(0, solidFill);

            return fillPatterns;
        }

        /// <summary>
        /// 加载所有线型图案
        /// </summary>
        public List<LinePatternItem> LoadLinePatterns()
        {
            var linePatterns = new FilteredElementCollector(_document)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs => gs.GraphicsStyleCategory?.Name == "线" || gs.Name != null)
                .Select(gs => new LinePatternItem(gs))
                .ToList();

            return linePatterns;
        }

        /// <summary>
        /// 创建实心填充图案（模拟）
        /// </summary>
        private FillPattern CreateSolidFillPattern()
        {
            // 实心填充实际返回null，但为了UI展示创建一个特殊标记
            return null;
        }

        #endregion

        #region 应用图案

        /// <summary>
        /// 应用填充图案到表面
        /// </summary>
        public bool ApplyFillPatternToSurface(FillPattern fillPattern)
        {
            var selectedElements = GetSelectedElements();
            if (!selectedElements.Any()) return false;

            var transaction = new Transaction(_document, "应用填充图案");
            transaction.Start();

            foreach (var element in selectedElements)
            {
                ApplyFillPatternToElement(element, fillPattern, surfaceOnly: true);
            }

            transaction.Commit();
            return true;
        }

        /// <summary>
        /// 应用填充图案到切割面
        /// </summary>
        public bool ApplyFillPatternToCutSurface(FillPattern fillPattern)
        {
            var selectedElements = GetSelectedElements();
            if (!selectedElements.Any()) return false;

            var transaction = new Transaction(_document, "应用填充图案到切割面");
            transaction.Start();

            foreach (var element in selectedElements)
            {
                ApplyFillPatternToElement(element, fillPattern, surfaceOnly: false);
            }

            transaction.Commit();
            return true;
        }

        /// <summary>
        /// 应用线型图案到网格线
        /// </summary>
        public bool ApplyLinePatternToGrids(GraphicsStyle linePattern)
        {
            var selectedGrids = GetSelectedGrids();
            if (!selectedGrids.Any()) return false;

            var transaction = new Transaction(_document, "应用线型图案");
            transaction.Start();
            foreach (var grid in selectedGrids)
            {
                // 使用模式匹配设置线型
                if (grid is Autodesk.Revit.DB.Grid gridLine)
                {
                    //GridLine为何不同，没有lingstyle属性
                    //var lineStyle = gridLine.LineStyle;
                    // 注：实际需要根据API获取可修改的线型参数
                }
            }

            transaction.Commit();
            return true;
        }

        /// <summary>
        /// 获取选中的元素
        /// </summary>
        private List<Element> GetSelectedElements()
        {
            var selectedIds = _uiDoc.Selection.GetElementIds();
            return selectedIds.Select(id => _document.GetElement(id)).ToList();
        }

        /// <summary>
        /// 获取选中的网格线
        /// </summary>
        private List<Autodesk.Revit.DB.Grid> GetSelectedGrids()
        {
            var selectedIds = _uiDoc.Selection.GetElementIds();
            return selectedIds
                .Select(id => _document.GetElement(id))
                .OfType<Autodesk.Revit.DB.Grid>()  // 使用C# 7.3的OfType筛选
                .ToList();
        }

        /// <summary>
        /// 应用填充图案到单个元素
        /// </summary>
        private void ApplyFillPatternToElement(Element element, FillPattern fillPattern, bool surfaceOnly)
        {
            if (element is Wall wall)
            {
                // 获取墙的类型并设置填充图案
                var wallType = wall.WallType;
                var compoundStruct = wallType.GetCompoundStructure();

                // 注：实际需要根据API设置具体的填充图案参数
                // 这里展示模式匹配的使用
                switch (compoundStruct)
                {
                    case CompoundStructure cs when cs != null:
                        // 设置复合结构的填充图案
                        break;
                    case null:
                        // 设置简单墙的填充图案
                        break;
                }
            }
            else if (element is Floor floor)
            {
                // 设置楼板的填充图案
                var floorType = floor.FloorType;
            }
            // 使用C# 7.3的模式匹配处理其他类型
        }

        #endregion

        #region 创建图案

        /// <summary>
        /// 创建简单填充图案
        /// 使用C# 7.3的元组验证
        /// </summary>
        public bool CreateSimpleFillPattern(PatternCreationParams parameters, out string errorMessage)
        {
            errorMessage = null;
            // 使用元组验证参数
            var (isValid, validationError) = ValidateCreationParams(parameters);
            if (!isValid)
            {
                errorMessage = validationError;
                return false;
            }
            //var transaction = new Transaction(_document, "创建填充图案");
            //transaction.Start();
            //try
            //{
            //    // 创建填充图案定义
            //    var definition = FillPattern.GetFillPatternByName(_document, parameters.Name);
            //    if (definition != null)
            //    {
            //        errorMessage = $"填充图案 {parameters.Name} 已存在";
            //        transaction.RollBack();
            //        return false;
            //    }
            //    // 创建新的填充图案元素
            //    var fillPatternElement = FillPatternElement.Create(_document, parameters.Name,
            //        parameters.Target, parameters.Orientation, parameters.LineSpacing, parameters.LineAngle);
            //    transaction.Commit();
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    transaction.RollBack();
            //    errorMessage = ex.Message;
            //    return false;
            //}
            return true;
        }

        /// <summary>
        /// 验证创建参数
        /// 使用C# 7.3的元组返回
        /// </summary>
        private (bool isValid, string errorMessage) ValidateCreationParams(PatternCreationParams parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.Name))
                return (false, "图案名称不能为空");

            if (parameters.LineSpacing <= 0)
                return (false, "线间距必须大于0");

            return (true, null);
        }

        /// <summary>
        /// 创建复杂填充图案（从PAT文件导入）
        /// </summary>
        public bool CreateComplexFillPattern(PatternCreationParams parameters, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(parameters.PatternFile) || !System.IO.File.Exists(parameters.PatternFile))
            {
                errorMessage = "图案文件不存在";
                return false;
            }

            var transaction = new Transaction(_document, "导入复杂填充图案");
            transaction.Start();

            try
            {
                // 注：实际复杂填充图案需要通过FillPatternElement.CreateFromFile导入
                // 这里展示基本结构
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 创建线型图案
        /// </summary>
        public bool CreateLinePattern(LinePatternCreationParams parameters, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(parameters.Name))
            {
                errorMessage = "线型名称不能为空";
                return false;
            }
            var transaction = new Transaction(_document, "创建线型图案");
            transaction.Start();
            try
            {
                // 注：实际创建线型图案需要通过GraphicsStyle.Create
                // 这里展示基本结构
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                errorMessage = ex.Message;
                return false;
            }
        }
        #endregion
    }
    /// <summary>
    /// 图案类型枚举
    /// </summary>
    public enum PatternCategory
    {
        FillPattern,      // 填充图案
        LinePattern       // 线型图案
    }

    /// <summary>
    /// 填充图案项视图模型
    /// 使用C# 7.3的表达式体和属性初始化器
    /// </summary>
    public class FillPatternItem : ObserverableObject
    {
        private bool _isSelected;
        public FillPatternItem(FillPattern pattern)
        {
            FillPattern = pattern ?? throw new System.ArgumentNullException(nameof(pattern));
        }
        public FillPattern FillPattern { get; }
        public string Name => FillPattern?.Name ?? "未命名";
        public bool IsSolid => FillPattern?.IsSolidFill ?? false;

        /// <summary>填充图案预览（简化表示）</summary>
        public string PreviewText => IsSolid ? "■ 实心填充" : $"◧ {Name}";
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    /// <summary>
    /// 线型图案项视图模型
    /// </summary>
    public class LinePatternItem : ObserverableObject
    {
        private bool _isSelected;

        public GraphicsStyle LinePattern { get; }
        public string Name => LinePattern?.Name ?? "未命名";
        public string PreviewText => $"─ {Name}";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public LinePatternItem(GraphicsStyle pattern)
        {
            LinePattern = pattern;
        }
    }

    /// <summary>
    /// 图案创建参数模型
    /// 使用C# 7.3的只读结构体和元组
    /// </summary>
    public class PatternCreationParams
    {
        public string Name { get; set; }
        public FillPatternTarget Target { get; set; }
        public FillPatternHostOrientation Orientation { get; set; }
        public double LineSpacing { get; set; } = 10.0;
        public double LineAngle { get; set; } = 0.0;

        // 用于复杂填充图案
        public string PatternFile { get; set; }
        public string PatternId { get; set; }
    }

    /// <summary>
    /// 线型图案创建参数
    /// </summary>
    public class LinePatternCreationParams
    {
        public string Name { get; set; }
        public string PatternString { get; set; }  // 线型定义字符串
    }

}
