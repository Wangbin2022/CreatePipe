using Autodesk.Revit.DB;
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
    /// ParameterBindingBrowserWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterBindingBrowserWindow : Window
    {
        public ParameterBindingBrowserWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 参数绑定浏览器视图模型
    /// </summary>
    public class ParameterBindingBrowserViewModel : ObserverableObject
    {
        private readonly ParameterBindingService _bindingService;

        private ObservableCollection<ParameterBindingInfo> _bindings;
        private ParameterBindingInfo _selectedBinding;
        private bool _isLoading;
        private string _statusMessage;
        private int _totalParameters;
        private int _totalCategories;

        public ParameterBindingBrowserViewModel(ExternalCommandData commandData)
        {
            _bindingService = new ParameterBindingService(commandData);
            _bindings = new ObservableCollection<ParameterBindingInfo>();

            // 初始化命令
            RefreshCommand = new BaseBindingCommand(_ => RefreshBindings(), _ => !IsLoading);
            CloseCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 加载数据
            RefreshBindings();
        }

        /// <summary>
        /// 参数绑定列表
        /// </summary>
        public ObservableCollection<ParameterBindingInfo> Bindings
        {
            get => _bindings;
            set { _bindings = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的参数绑定
        /// </summary>
        public ParameterBindingInfo SelectedBinding
        {
            get => _selectedBinding;
            set { _selectedBinding = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRefresh)); }
        }

        /// <summary>
        /// 是否可以刷新
        /// </summary>
        public bool CanRefresh => !IsLoading;

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 参数总数
        /// </summary>
        public int TotalParameters
        {
            get => _totalParameters;
            set { _totalParameters = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 类别总数
        /// </summary>
        public int TotalCategories
        {
            get => _totalCategories;
            set { _totalCategories = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 统计摘要
        /// </summary>
        public string SummaryText => $"共 {TotalParameters} 个参数，绑定到 {TotalCategories} 个类别";

        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 刷新参数绑定列表
        /// </summary>
        private void RefreshBindings()
        {
            IsLoading = true;
            StatusMessage = "正在加载参数绑定信息...";

            try
            {
                var bindings = _bindingService.GetParameterBindings();

                Bindings.Clear();
                foreach (var binding in bindings)
                {
                    Bindings.Add(binding);
                }

                // 更新统计信息
                TotalParameters = Bindings.Count;
                TotalCategories = Bindings.Sum(b => b.CategoryCount);

                StatusMessage = $"加载完成，共 {TotalParameters} 个参数绑定";

                if (TotalParameters == 0)
                {
                    StatusMessage = "当前文档中没有找到任何参数绑定";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    /// <summary>
    /// 参数绑定服务类
    /// 负责获取文档中的参数绑定信息
    /// </summary>
    public class ParameterBindingService
    {
        private readonly Document _document;

        public ParameterBindingService(ExternalCommandData commandData)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取所有参数绑定信息
        /// </summary>
        /// <returns>参数绑定列表</returns>
        public List<ParameterBindingInfo> GetParameterBindings()
        {
            var bindingsMap = _document.ParameterBindings;
            var results = new List<ParameterBindingInfo>();

            var iterator = bindingsMap.ForwardIterator();

            while (iterator.MoveNext())
            {
                var elementBinding = iterator.Current as ElementBinding;
                var definition = iterator.Key as Definition;

                if (definition == null) continue;

                var bindingInfo = new ParameterBindingInfo
                {
                    ParameterName = definition.Name
                };

                // 获取绑定的类别
                if (elementBinding?.Categories != null)
                {
                    var categories = elementBinding.Categories.Cast<Category>();
                    foreach (var category in categories)
                    {
                        if (category != null)
                        {
                            bindingInfo.Categories.Add(category.Name);
                        }
                    }
                }

                // 按类别名称排序
                var sortedCategories = bindingInfo.Categories.OrderBy(c => c).ToList();
                bindingInfo.Categories.Clear();
                foreach (var cat in sortedCategories)
                {
                    bindingInfo.Categories.Add(cat);
                }

                results.Add(bindingInfo);
            }

            // 按参数名称排序
            return results.OrderBy(r => r.ParameterName).ToList();
        }

        /// <summary>
        /// 检查参数绑定是否已初始化
        /// </summary>
        public bool HasBindings()
        {
            return _document.ParameterBindings.IsEmpty == false;
        }
    }
    /// <summary>
    /// 参数绑定信息模型
    /// </summary>
    public class ParameterBindingInfo
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// 绑定的类别列表
        /// </summary>
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 绑定的类别数量
        /// </summary>
        public int CategoryCount => Categories.Count;

        /// <summary>
        /// 显示文本（用于TreeView）
        /// </summary>
        public string DisplayText => $"{ParameterName} ({CategoryCount}个类别)";
    }
}
