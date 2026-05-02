using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
    /// DesignOptionView.xaml 的交互逻辑
    /// </summary>
    public partial class DesignOptionView : Window
    {
        public DesignOptionView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主视图模型 - 管理设计选项查看逻辑
    /// </summary>
    public class DesignOptionViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;

        private DesignOptionCollectionModel _designOptions;
        private bool _isLoading;
        private string _statusMessage;
        private ExecutionResultModel _executionResult;
        #endregion

        #region 公开属性
        /// <summary>设计选项集合</summary>
        public DesignOptionCollectionModel DesignOptions
        {
            get => _designOptions;
            set => SetProperty(ref _designOptions, value);
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

        /// <summary>执行结果</summary>
        public ExecutionResultModel ExecutionResult
        {
            get => _executionResult;
            set => SetProperty(ref _executionResult, value);
        }

        /// <summary>是否有设计选项</summary>
        public bool HasDesignOptions => DesignOptions?.HasDesignOptions ?? false;

        /// <summary>设计选项数量</summary>
        public int DesignOptionCount => DesignOptions?.DesignOptions?.Count ?? 0;
        #endregion

        #region 命令
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand CopyToClipboardCommand { get; }
        #endregion

        public DesignOptionViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _document = commandData.Application.ActiveUIDocument.Document;

            DesignOptions = new DesignOptionCollectionModel();
            ExecutionResult = new ExecutionResultModel();

            // 初始化命令
            RefreshCommand = new BaseBindingCommand(LoadDesignOptions);
            CloseCommand = new BaseBindingCommand(CloseWindow);
            CopyToClipboardCommand = new BaseBindingCommand(CopyToClipboard, _=> HasDesignOptions);

            // 自动加载设计选项
            LoadDesignOptions(null);
        }

        /// <summary>
        /// 加载设计选项
        /// </summary>
        private void LoadDesignOptions(Object obj)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载设计选项...";
                DesignOptions.DesignOptions.Clear();

                // 使用FilteredElementCollector收集设计选项
                var collector = new FilteredElementCollector(_document);
                var designOptions = collector
                    .OfClass(typeof(DesignOption))
                    .Cast<DesignOption>()
                    .OrderBy(d => d.Name)
                    .ToList();

                foreach (var option in designOptions)
                {
                    // 获取设计选项中的成员数量
                    var memberCount = GetDesignOptionMemberCount(option);

                    DesignOptions.DesignOptions.Add(new DesignOptionModel
                    {
                        Name = option.Name,
                        Id = option.Id,
                        MemberCount = memberCount
                    });
                }

                DesignOptions.HasDesignOptions = DesignOptions.DesignOptions.Any();

                if (DesignOptions.HasDesignOptions)
                {
                    ExecutionResult = new ExecutionResultModel
                    {
                        Success = true,
                        Message = $"成功加载 {DesignOptions.DesignOptions.Count} 个设计选项",
                        DesignOptionCount = DesignOptions.DesignOptions.Count
                    };
                    StatusMessage = ExecutionResult.Message;
                }
                else
                {
                    ExecutionResult = new ExecutionResultModel
                    {
                        Success = false,
                        Message = "当前文档中没有设计选项",
                        DesignOptionCount = 0
                    };
                    StatusMessage = ExecutionResult.Message;
                }
            }
            catch (Exception ex)
            {
                ExecutionResult = new ExecutionResultModel
                {
                    Success = false,
                    Message = $"加载失败：{ex.Message}",
                    DesignOptionCount = 0
                };
                StatusMessage = ExecutionResult.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 获取设计选项中的成员数量
        /// </summary>
        private int GetDesignOptionMemberCount(DesignOption designOption)
        {
            try
            {
                // 使用FilteredElementCollector收集属于该设计选项的成员
                var collector = new FilteredElementCollector(_document);
                return collector
                    .WhereElementIsNotElementType()
                    .Cast<Element>()
                    .Count(e => e.DesignOption != null && e.DesignOption.Id == designOption.Id);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 复制设计选项列表到剪贴板
        /// </summary>
        private void CopyToClipboard(Object obj)
        {
            if (!HasDesignOptions) return;

            var clipboardText = GenerateReportText();
            System.Windows.Clipboard.SetText(clipboardText);
            StatusMessage = "设计选项列表已复制到剪贴板";
        }

        /// <summary>
        /// 生成报告文本（C#7.0本地函数）
        /// </summary>
        private string GenerateReportText()
        {
            string FormatHeader() => "=".PadRight(50, '=');
            string FormatFooter() => "=".PadRight(50, '=');

            var report = new System.Text.StringBuilder();
            report.AppendLine(FormatHeader());
            report.AppendLine($"设计选项列表 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"文档名称: {_document.Title}");
            report.AppendLine(FormatHeader());
            report.AppendLine();

            int index = 1;
            foreach (var option in DesignOptions.DesignOptions)
            {
                report.AppendLine($"{index,2}. {option.Name}");
                report.AppendLine($"     成员数量: {option.MemberCount}");
                report.AppendLine();
                index++;
            }

            report.AppendLine(FormatFooter());
            report.AppendLine($"总计: {DesignOptions.DesignOptions.Count} 个设计选项");

            return report.ToString();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(Object obj)
        {
            if (System.Windows.Application.Current.Windows.Count > 0)
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
        }
    }
    /// <summary>
    /// 设计选项模型
    /// </summary>
    public class DesignOptionModel : ObserverableObject
    {
        private string _name;
        private ElementId _id;
        private bool _isSelected;
        private int _memberCount;

        /// <summary>设计选项名称</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>设计选项ID</summary>
        public ElementId Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>包含的成员数量</summary>
        public int MemberCount
        {
            get => _memberCount;
            set => SetProperty(ref _memberCount, value);
        }

        /// <summary>显示名称（含成员数量）</summary>
        public string DisplayName => $"{Name} (成员数: {MemberCount})";
    }
    /// <summary>
    /// 设计选项集合模型
    /// </summary>
    public class DesignOptionCollectionModel : ObserverableObject
    {
        private ObservableCollection<DesignOptionModel> _designOptions;
        private DesignOptionModel _selectedOption;
        private bool _hasDesignOptions;

        public ObservableCollection<DesignOptionModel> DesignOptions
        {
            get => _designOptions;
            set => SetProperty(ref _designOptions, value);
        }

        public DesignOptionModel SelectedOption
        {
            get => _selectedOption;
            set => SetProperty(ref _selectedOption, value);
        }

        public bool HasDesignOptions
        {
            get => _hasDesignOptions;
            set => SetProperty(ref _hasDesignOptions, value);
        }

        public DesignOptionCollectionModel()
        {
            DesignOptions = new ObservableCollection<DesignOptionModel>();
        }
    }
    /// <summary>
    /// 执行结果模型
    /// </summary>
    public class ExecutionResultModel : ObserverableObject
    {
        private bool _success;
        private string _message;
        private int _designOptionCount;
        public bool Success
        {
            get => _success;
            set => SetProperty(ref _success, value);
        }
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }
        public int DesignOptionCount
        {
            get => _designOptionCount;
            set => SetProperty(ref _designOptionCount, value);
        }
    }
    /// <summary>
    /// 索引转换器（用于ItemsControl中显示序号）
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemsControl itemsControl)
            {
                int index = itemsControl.Items.IndexOf(itemsControl) + 1;
                return index.ToString();
            }
            return "•";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
