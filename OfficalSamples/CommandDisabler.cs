using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// Revit命令禁用应用程序级插件
    /// 功能：在Revit启动时禁用指定的内置命令，防止用户执行
    /// </summary>
    public class Application : IExternalApplication
    {
        #region 常量定义
        /// <summary>
        /// 需要禁用的命令ID字符串
        /// 查找方法：执行Revit命令后查看Journal文件中的"Jrn.Command"条目
        /// </summary>
        private const string COMMAND_TO_DISABLE = "ID_EDIT_DESIGNOPTIONS";

        private const string ERROR_CANNOT_OVERRIDE = "无法覆盖目标命令: {0}";
        private const string ERROR_ALREADY_BOUND = "命令 {0} 已被其他插件绑定，无法禁用";
        private const string DISABLED_MESSAGE = "此命令已被禁用。";
        private const string DISABLED_TITLE = "命令已禁用";
        private const string ERROR_TITLE = "错误";
        #endregion

        #region 私有字段
        private static RevitCommandId _commandId;
        private static AddInCommandBinding _commandBinding;
        #endregion

        #region IExternalApplication 实现

        /// <summary>
        /// Revit启动时调用 - 注册命令拦截
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 查找目标命令
                if (!TryFindCommand())
                {
                    return Result.Failed;
                }

                // 验证命令是否可被覆盖
                if (!_commandId.CanHaveBinding)
                {
                    ShowErrorDialog(string.Format(ERROR_CANNOT_OVERRIDE, COMMAND_TO_DISABLE));
                    return Result.Failed;
                }

                // 创建命令绑定并注册事件
                RegisterCommandBinding(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"启动失败: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Revit关闭时调用 - 清理命令绑定
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                if (_commandId?.HasBinding == true)
                {
                    application.RemoveAddInCommandBinding(_commandId);
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"关闭清理失败: {ex.Message}");
                return Result.Failed;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 查找目标命令ID
        /// </summary>
        private static bool TryFindCommand()
        {
            _commandId = RevitCommandId.LookupCommandId(COMMAND_TO_DISABLE);

            if (_commandId == null)
            {
                ShowErrorDialog($"未找到命令: {COMMAND_TO_DISABLE}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 注册命令绑定
        /// </summary>
        private static void RegisterCommandBinding(UIControlledApplication application)
        {
            try
            {
                _commandBinding = application.CreateAddInCommandBinding(_commandId);
                _commandBinding.Executed += OnCommandExecuted;

                // 可选：同时禁用命令的可用性（使按钮变灰）
                // _commandBinding.CanExecute += OnCanExecute;
            }
            catch (Exception)
            {
                ShowErrorDialog(string.Format(ERROR_ALREADY_BOUND, COMMAND_TO_DISABLE));
                throw;
            }
        }

        /// <summary>
        /// 命令执行时的拦截事件
        /// </summary>
        private static void OnCommandExecuted(object sender, ExecutedEventArgs args)
        {
            ShowDisabledDialog();
        }

        /// <summary>
        /// 控制命令是否可用（可选功能）
        /// 如果启用此方法，命令按钮将显示为灰色
        /// </summary>
        private static void OnCanExecute(object sender, CanExecuteEventArgs args)
        {
            // 设置命令不可用
            args.CanExecute = false;
        }

        /// <summary>
        /// 显示禁用提示对话框
        /// </summary>
        private static void ShowDisabledDialog()
        {
            ShowDialog(DISABLED_TITLE, DISABLED_MESSAGE);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private static void ShowErrorDialog(string message)
        {
            ShowDialog(ERROR_TITLE, message);
        }

        /// <summary>
        /// 显示任务对话框（C#7.0表达式体方法）
        /// </summary>
        private static void ShowDialog(string title, string message) =>
            new TaskDialog(title)
            {
                MainInstruction = message,
                TitleAutoPrefix = false
            }.Show();

        #endregion
    }

    /// <summary>
    /// 增强版命令禁用器 - 支持动态配置和多个命令禁用
    /// </summary>
    public class EnhancedCommandDisabler : IExternalApplication
    {
        #region 配置
        /// <summary>
        /// 需要禁用的命令列表
        /// </summary>
        private static readonly string[] CommandsToDisable = new[]
        {
            "ID_EDIT_DESIGNOPTIONS",      // 设计选项
            "ID_EDIT_PIN",                 // 固定图元
            "ID_EDIT_UNPIN",               // 解锁图元
            // 可继续添加更多命令
        };

        /// <summary>
        /// 是否显示禁用提示
        /// </summary>
        private static bool _showDisabledMessage = true;

        /// <summary>
        /// 是否同时禁用命令可用性（使按钮变灰）
        /// </summary>
        private static bool _disableCanExecute = false;
        #endregion

        #region 私有字段
        private readonly List<RevitCommandId> _boundCommandIds = new List<RevitCommandId>();
        #endregion

        public Result OnStartup(UIControlledApplication application)
        {
            var successCount = 0;
            var failedCommands = new List<string>();

            foreach (var commandIdString in CommandsToDisable)
            {
                if (TryDisableCommand(application, commandIdString))
                    successCount++;
                else
                    failedCommands.Add(commandIdString);
            }

            // 显示启动结果
            ShowStartupResult(successCount, failedCommands);

            return failedCommands.Count == CommandsToDisable.Length ? Result.Failed : Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            foreach (var commandId in _boundCommandIds)
            {
                if (commandId.HasBinding)
                {
                    application.RemoveAddInCommandBinding(commandId);
                }
            }

            _boundCommandIds.Clear();
            return Result.Succeeded;
        }

        /// <summary>
        /// 尝试禁用单个命令
        /// </summary>
        private bool TryDisableCommand(UIControlledApplication application, string commandIdString)
        {
            try
            {
                var commandId = RevitCommandId.LookupCommandId(commandIdString);

                if (commandId == null)
                {
                    LogError($"未找到命令: {commandIdString}");
                    return false;
                }

                if (!commandId.CanHaveBinding)
                {
                    LogError($"命令 {commandIdString} 无法被覆盖");
                    return false;
                }

                var binding = application.CreateAddInCommandBinding(commandId);
                binding.Executed += (s, e) => OnCommandExecuted(commandIdString);

                if (_disableCanExecute)
                {
                    binding.CanExecute += (s, e) => e.CanExecute = false;
                }

                _boundCommandIds.Add(commandId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"禁用命令 {commandIdString} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 命令执行时的处理
        /// </summary>
        private void OnCommandExecuted(string commandName)
        {
            if (_showDisabledMessage)
            {
                TaskDialog.Show("命令已禁用",
                    $"命令 \"{commandName}\" 已被管理员禁用。\n如需使用请联系系统管理员。");
            }
        }

        /// <summary>
        /// 显示启动结果
        /// </summary>
        private static void ShowStartupResult(int successCount, List<string> failedCommands)
        {
            var message = $"命令禁用器已启动\n" +
                         $"├─ 成功禁用: {successCount} 个命令\n" +
                         $"└─ 失败: {failedCommands.Count} 个命令";

            if (failedCommands.Any())
            {
                message += $"\n\n失败命令:\n  {string.Join("\n  ", failedCommands)}";
                TaskDialog.Show("启动完成（部分失败）", message);
            }
            else
            {
                TaskDialog.Show("启动完成", message);
            }
        }

        private static void LogError(string message)
        {
            // 可在此处添加日志记录
            System.Diagnostics.Debug.WriteLine($"[CommandDisabler] {message}");
        }
    }

    /// <summary>
    /// 条件命令禁用器 - 基于条件判断是否禁用命令
    /// </summary>
    public class ConditionalCommandDisabler : IExternalApplication
    {
        private const string TARGET_COMMAND = "ID_EDIT_PIN";
        private RevitCommandId _commandId;
        private AddInCommandBinding _binding;

        public Result OnStartup(UIControlledApplication application)
        {
            _commandId = RevitCommandId.LookupCommandId(TARGET_COMMAND);

            if (_commandId?.CanHaveBinding != true)
                return Result.Failed;

            _binding = application.CreateAddInCommandBinding(_commandId);

            // 使用CanExecute动态控制命令可用性
            _binding.CanExecute += OnCanExecute;
            _binding.Executed += OnExecuted;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            if (_commandId?.HasBinding == true)
                application.RemoveAddInCommandBinding(_commandId);

            return Result.Succeeded;
        }

        /// <summary>
        /// 动态判断命令是否可用
        /// </summary>
        private void OnCanExecute(object sender, CanExecuteEventArgs args)
        {
            // 示例：根据当前文档类型判断
            // 这里可以根据实际需求添加判断逻辑
            args.CanExecute = ShouldEnableCommand();
        }

        /// <summary>
        /// 判断是否应该启用命令
        /// </summary>
        private static bool ShouldEnableCommand()
        {
            // 示例条件：返回false表示始终禁用
            // 可根据需求实现：文档类型、用户权限、时间等条件
            return false;
        }

        /// <summary>
        /// 命令执行时的处理（仅在CanExecute返回true时才会触发）
        /// </summary>
        private void OnExecuted(object sender, ExecutedEventArgs args)
        {
            // 即使命令可用，也可以在执行时添加额外逻辑
            TaskDialog.Show("提示", "此命令已被修改行为");
        }
    }

    /// <summary>
    /// 命令禁用器配置类
    /// </summary>
    public static class CommandDisablerConfig
    {
        /// <summary>是否启用日志记录</summary>
        public static bool EnableLogging { get; set; } = true;

        /// <summary>是否显示禁用提示</summary>
        public static bool ShowDisabledMessage { get; set; } = true;

        /// <summary>自定义提示消息</summary>
        public static string CustomDisabledMessage { get; set; } = "此命令已被禁用。";

        /// <summary>自定义提示标题</summary>
        public static string CustomDisabledTitle { get; set; } = "命令不可用";

        /// <summary>禁用命令列表（从外部配置文件加载）</summary>
        public static string[] GetCommandList()
        {
            // 可从配置文件读取
            return new[]
            {
                "ID_EDIT_DESIGNOPTIONS"
            };
        }
    }
    internal class CommandDisabler
    {
    }
}
