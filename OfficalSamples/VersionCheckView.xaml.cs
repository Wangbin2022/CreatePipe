using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// VersionCheckView.xaml 的交互逻辑
    /// </summary>
    public partial class VersionCheckView : Window
    {
        public VersionCheckView(VersionCheckViewModel viewModel)
        {
            InitializeComponent();
            // 窗口关闭时释放资源
            Closed += (s, e) => viewModel.Dispose();
        }
    }
    /// <summary>
    /// 主视图模型类
    /// 处理版本信息显示窗口的所有业务逻辑
    /// 实现INotifyPropertyChanged以支持WPF数据绑定
    /// </summary>
    public class VersionCheckViewModel : ObserverableObject, IDisposable
    {
        private readonly RevitVersionService _versionService;
        private RevitVersionInfo _versionInfo;
        private bool _isLoading;
        private bool _isDisposed;

        /// <summary>
        /// Revit版本信息（绑定到UI）
        /// </summary>
        public RevitVersionInfo VersionInfo
        {
            get => _versionInfo;
            set
            {
                _versionInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // 命令定义
        public ICommand CloseCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="commandData">Revit外部命令数据</param>
        public VersionCheckViewModel(ExternalCommandData commandData)
        {
            if (commandData == null)
                throw new ArgumentNullException(nameof(commandData));

            // 初始化服务
            _versionService = new RevitVersionService(commandData);

            // 初始化命令
            CloseCommand = new BaseBindingCommand(ExecuteClose);
            CopyToClipboardCommand = new BaseBindingCommand(ExecuteCopyToClipboard);

            // 加载版本信息
            LoadVersionInfo();
        }

        /// <summary>
        /// 加载Revit版本信息
        /// 使用异步模式避免UI阻塞
        /// </summary>
        private async void LoadVersionInfo()
        {
            try
            {
                IsLoading = true;

                // 模拟异步加载（实际获取版本信息很快，这里展示异步模式）
                VersionInfo = await System.Threading.Tasks.Task.Run(() => _versionService.GetVersionInfo());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载版本信息失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                //// 创建错误信息对象
                //VersionInfo = new RevitVersionInfo
                //{
                //    FormattedInfo = $"无法获取Revit版本信息：{ex.Message}"
                //};
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行关闭命令
        /// </summary>
        private void ExecuteClose(Object obj)
        {
            CloseWindow();
        }

        /// <summary>
        /// 检查是否可以执行关闭命令
        /// </summary>
        private bool CanExecuteClose()
        {
            return !IsLoading;
        }

        /// <summary>
        /// 执行复制到剪贴板命令
        /// </summary>
        private void ExecuteCopyToClipboard(Object obj)
        {
            if (VersionInfo?.FormattedInfo != null)
            {
                try
                {
                    Clipboard.SetText(VersionInfo.FormattedInfo);
                    MessageBox.Show("版本信息已复制到剪贴板", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 检查是否可以执行复制命令
        /// </summary>
        private bool CanExecuteCopy()
        {
            return VersionInfo != null && !IsLoading;
        }

        /// <summary>
        /// 关闭当前窗口
        /// </summary>
        private void CloseWindow()
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            window?.Close();
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                VersionInfo = null;
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Revit版本信息服务类
    /// 负责从Revit应用程序获取版本信息
    /// </summary>
    public class RevitVersionService
    {
        private readonly Autodesk.Revit.ApplicationServices.Application _revitApplication;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="commandData">Revit外部命令数据</param>
        /// <exception cref="ArgumentNullException">当commandData为null时抛出</exception>
        public RevitVersionService(ExternalCommandData commandData)
        {
            if (commandData == null)
                throw new ArgumentNullException(nameof(commandData));

            if (commandData.Application?.Application == null)
                throw new InvalidOperationException("无法获取Revit应用程序实例");

            _revitApplication = commandData.Application.Application;
        }

        /// <summary>
        /// 获取Revit版本信息
        /// </summary>
        /// <returns>RevitVersionInfo对象</returns>
        public RevitVersionInfo GetVersionInfo()
        {
            return new RevitVersionInfo
            {
                ProductName = _revitApplication.VersionName,
                VersionNumber = _revitApplication.VersionNumber,
                BuildNumber = _revitApplication.VersionBuild,
                VersionName = GetVersionName() // 获取友好的版本名称
            };
        }

        /// <summary>
        /// 获取友好的版本名称
        /// 使用C# 7.3的switch表达式简化条件判断
        /// </summary>
        private string GetVersionName()
        {
            var versionNum = _revitApplication.VersionNumber;

            switch (versionNum)
            {
                case "2025":
                    return "Revit 2025";
                case "2024":
                    return "Revit 2024";
                case "2023":
                    return "Revit 2023";
                case "2022":
                    return "Revit 2022";
                case "2021":
                    return "Revit 2021";
                case "2020":
                    return "Revit 2020";
                case "2019":
                    return "Revit 2019";
                case "2018":
                    return "Revit 2018";
                case "2017":
                    return "Revit 2017";
                default:
                    return "Revit " + versionNum;
            }
        }

        /// <summary>
        /// 获取完整的版本描述字符串
        /// </summary>
        public string GetFullVersionDescription()
        {
            var info = GetVersionInfo();
            return info.FormattedInfo;
        }
    }
    /// <summary>
    /// Revit版本信息数据模型
    /// 封装Revit应用程序的版本相关信息
    /// </summary>
    public class RevitVersionInfo : ObserverableObject
    {
        private string _productName;
        private string _versionNumber;
        private string _buildNumber;
        private string _versionName;
        private string _formattedInfo;

        /// <summary>
        /// 产品名称（如：Revit 2023）
        /// </summary>
        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged();
                UpdateFormattedInfo();
            }
        }

        /// <summary>
        /// 版本号（如：2023）
        /// </summary>
        public string VersionNumber
        {
            get => _versionNumber;
            set
            {
                _versionNumber = value;
                OnPropertyChanged();
                UpdateFormattedInfo();
            }
        }

        /// <summary>
        /// 编译版本号（如：23.0.1234.5678）
        /// </summary>
        public string BuildNumber
        {
            get => _buildNumber;
            set
            {
                _buildNumber = value;
                OnPropertyChanged();
                UpdateFormattedInfo();
            }
        }

        /// <summary>
        /// 版本名称（完整版本标识）
        /// </summary>
        public string VersionName
        {
            get => _versionName;
            set
            {
                _versionName = value;
                OnPropertyChanged();
                UpdateFormattedInfo();
            }
        }

        /// <summary>
        /// 格式化的完整版本信息，用于显示
        /// </summary>
        public string FormattedInfo
        {
            get => _formattedInfo;
            private set
            {
                _formattedInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新格式化的显示信息
        /// 使用C# 7.3的字符串插值特性简化字符串格式化
        /// </summary>
        private void UpdateFormattedInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            info.AppendLine($"        Revit 版本信息");
            info.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            if (!string.IsNullOrWhiteSpace(ProductName))
                info.AppendLine($"产品名称: {ProductName}");

            if (!string.IsNullOrWhiteSpace(VersionName))
                info.AppendLine($"版本标识: {VersionName}");

            if (!string.IsNullOrWhiteSpace(VersionNumber))
                info.AppendLine($"版本号:   {VersionNumber}");

            if (!string.IsNullOrWhiteSpace(BuildNumber))
                info.AppendLine($"编译号:   {BuildNumber}");

            info.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            info.AppendLine($"©  Autodesk, Inc. 保留所有权利");

            FormattedInfo = info.ToString();
        }
    }
}
