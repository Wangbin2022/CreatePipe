using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    public class RevitOperationLogger : IDisposable
    {
        private static RevitOperationLogger _instance;
        private static readonly object _lock = new object();
        private readonly string _logFilePath;
        private readonly object _fileLock = new object();
        private bool _disposed = false;
        private UIApplication _uiApp;

        // 操作类型枚举
        public enum OperationType
        {
            Transaction,    // 事务操作
            NullCheck,      // 空值检查
            Parameter,      // 参数操作
            Selection,      // 选择操作
            Validation,     // 验证操作
            Exception,      // 异常处理
            General         // 通用操作
        }
        private RevitOperationLogger(UIApplication uiApp = null, string logDirectory = null)
        {
            _uiApp = uiApp;

            if (string.IsNullOrEmpty(logDirectory))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                logDirectory = Path.Combine(desktopPath, "RevitLogs");
            }

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string fileName = $"revit_operation_log_{DateTime.Now:yyyyMMdd}.txt";
            _logFilePath = Path.Combine(logDirectory, fileName);

            LogSystem("日志系统初始化");
        }
        public static RevitOperationLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("日志器未初始化，请先调用 Initialize 方法");
                }
                return _instance;
            }
        }
        public static void Initialize(UIApplication uiApp, string logDirectory = null)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new RevitOperationLogger(uiApp, logDirectory);
                    }
                }
            }
        }
        // 核心日志方法 - 按操作类型分类
        private void WriteLog(OperationType type, string message, bool isSuccess = true, bool showDialog = false)
        {
            if (_disposed) return;

            string status = isSuccess ? "✓ 成功" : "✗ 失败";
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] [{GetOperationTypeName(type)}] {status} - {message}";

            // 控制台输出
            ConsoleColor originalColor = Console.ForegroundColor;
            if (!isSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (type == OperationType.Exception)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Console.WriteLine(logMessage);
            Console.ForegroundColor = originalColor;

            // 写入文件
            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[系统错误] 写入日志文件失败: {ex.Message}");
                    Console.ForegroundColor = originalColor;
                }
            }

            // 如果需要显示对话框
            if (showDialog && _uiApp != null && !isSuccess)
            {
                TaskDialog.Show(GetOperationTypeName(type), message);
            }
        }
        private string GetOperationTypeName(OperationType type)
        {
            switch (type)
            {   
                case OperationType.Transaction:
                    return "事务操作";
                case OperationType.NullCheck:
                    return "空值检查";
                case OperationType.Parameter:
                    return "参数操作";
                case OperationType.Selection:
                    return "选择操作";
                case OperationType.Validation:
                    return "验证操作";
                case OperationType.Exception:
                    return "异常处理";
                case OperationType.General:
                    return "通用操作";
                default:
                    return "其他操作";
            }
            //return type switch
            //{
            //    OperationType.Transaction => "事务操作",
            //    OperationType.NullCheck => "空值检查",
            //    OperationType.Parameter => "参数操作",
            //    OperationType.Selection => "选择操作",
            //    OperationType.Validation => "验证操作",
            //    OperationType.Exception => "异常处理",
            //    OperationType.General => "通用操作",
            //    _ => "其他操作"
            //};
        }
        //private string GetOperationTypeName(OperationType type)
        //{
        //    return type switch
        //    {
        //        OperationType.Transaction => "事务操作",
        //        OperationType.NullCheck => "空值检查",
        //        OperationType.Parameter => "参数操作",
        //        OperationType.Selection => "选择操作",
        //        OperationType.Validation => "验证操作",
        //        OperationType.Exception => "异常处理",
        //        OperationType.General => "通用操作",
        //        _ => "其他操作"
        //    };
        //}

        // 事务操作日志
        public void LogTransaction(string operationName, bool isSuccess, string details = null)
        {
            string message = $"事务 '{operationName}'";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            WriteLog(OperationType.Transaction, message, isSuccess);
        }
        // 空值检查日志
        public void LogNullCheck(string objectName, bool isNull, bool showDialog = false)
        {
            string message = $"{objectName} 为空值检查";
            WriteLog(OperationType.NullCheck, message, !isNull, showDialog);
        }
        // 参数操作日志
        public void LogParameterOperation(string paramName, string elementInfo, string oldValue, string newValue, bool isSuccess)
        {
            string message = $"参数 '{paramName}' 在 {elementInfo} 中从 '{oldValue}' 修改为 '{newValue}'";
            WriteLog(OperationType.Parameter, message, isSuccess);
        }
        // 选择操作日志
        public void LogSelection(string action, int selectedCount, bool isSuccess)
        {
            string message = $"{action} - 选择了 {selectedCount} 个元素";
            WriteLog(OperationType.Selection, message, isSuccess);
        }
        // 验证操作日志
        public void LogValidation(string checkName, bool isValid, string details = null, bool showDialog = false)
        {
            string message = $"验证 '{checkName}': {(isValid ? "通过" : "不通过")}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            WriteLog(OperationType.Validation, message, isValid, showDialog && !isValid);
        }
        // 异常处理日志
        public void LogException(Exception ex, string context = null, bool showDialog = true)
        {
            string message = $"异常: {ex.GetType().Name} - {ex.Message}";
            if (!string.IsNullOrEmpty(context))
            {
                message = $"[{context}] {message}";
            }
            WriteLog(OperationType.Exception, message, false, showDialog);
        }
        // 通用操作日志
        public void LogGeneral(string operation, bool isSuccess, string details = null)
        {
            string message = operation;
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            WriteLog(OperationType.General, message, isSuccess);
        }
        // 命令开始/结束日志
        public void LogCommandStart(string commandName)
        {
            WriteLog(OperationType.General, $"========== {commandName} 开始 ==========", true);
        }
        public void LogCommandEnd(string commandName, bool isSuccess)
        {
            string status = isSuccess ? "成功完成" : "失败";
            WriteLog(OperationType.General, $"========== {commandName} {status} ==========", isSuccess);
        }
        private void LogSystem(string message)
        {
            WriteLog(OperationType.General, $"[系统] {message}", true);
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                LogSystem("日志系统关闭");
                _disposed = true;
            }
        }
    }
}
