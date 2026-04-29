using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 日志服务 - 单例模式
    /// 负责记录事件跟踪日志
    /// 使用C# 7.3的只读结构体和表达式体
    /// </summary>
    public class LogService : IDisposable
    {
        private readonly TraceListener _traceListener;
        private readonly string _logFilePath;
        private bool _isDisposed;

        // 单例实例
        private static readonly Lazy<LogService> _instance =
            new Lazy<LogService>(() => new LogService());

        public static LogService Instance => _instance.Value;

        // 是否处于回归测试模式（通过检测ExpectedOutPut.log文件）
        public bool IsRegressionTestMode { get; }

        private LogService()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logFilePath = Path.Combine(assemblyLocation, "CancelSave.log");
            IsRegressionTestMode = CheckRegressionTestMode(assemblyLocation);

            // 删除旧的日志文件
            if (File.Exists(_logFilePath))
                File.Delete(_logFilePath);

            _traceListener = new TextWriterTraceListener(_logFilePath);
            Trace.Listeners.Add(_traceListener);
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// 检查是否为回归测试模式
        /// 使用C# 7.3的模式匹配
        /// </summary>
        private bool CheckRegressionTestMode(string assemblyPath) =>
            File.Exists(Path.Combine(assemblyPath, "ExpectedOutPut.log"));

        /// <summary>
        /// 记录事件日志
        /// 使用C# 7.3的字符串插值和表达式体
        /// </summary>
        public void LogEvent(EventArgs args, Document doc, string additionalInfo = null)
        {
            var eventName = GetEventName(args.GetType());
            var docTitle = GetTitleWithoutExtension(doc?.Title);

            WriteLine(string.Empty);
            WriteLine($"[Event] {eventName}: {docTitle}");

            if (!string.IsNullOrEmpty(additionalInfo))
                WriteLine($"   {additionalInfo}");
        }

        /// <summary>
        /// 记录状态验证日志
        /// </summary>
        public void LogValidation(Document doc, ValidationResult result)
        {
            if (result.IsValid)
            {
                WriteLine($"   状态已更新: {result.CurrentStatus}");
                return;
            }

            WriteLine($"   验证失败: {result.Message}");
            WriteLine($"   原始状态: {result.OriginalStatus ?? "空"}");
            WriteLine($"   当前状态: {result.CurrentStatus ?? "空"}");
        }

        /// <summary>
        /// 写入单行日志
        /// </summary>
        public void WriteLine(string message) => Trace.WriteLine(message);

        /// <summary>
        /// 刷新并关闭日志
        /// </summary>
        public void FlushAndClose()
        {
            if (_isDisposed) return;

            Trace.Flush();
            _traceListener?.Close();
            Trace.Close();
            Trace.Listeners.Remove(_traceListener);
        }

        /// <summary>
        /// 获取事件名称（去除命名空间和EventArgs后缀）
        /// 使用C# 7.3的模式匹配和Range操作符
        /// </summary>
        private string GetEventName(Type type)
        {
            var fullName = type.ToString();
            const string prefix = "Autodesk.Revit.DB.Events.";
            const string suffix = "EventArgs";

            if (!fullName.StartsWith(prefix) || !fullName.EndsWith(suffix))
                return fullName;

            var startIndex = prefix.Length;
            var length = fullName.Length - prefix.Length - suffix.Length;
            return fullName.Substring(startIndex, length);
        }

        /// <summary>
        /// 获取标题（不含扩展名）
        /// </summary>
        private string GetTitleWithoutExtension(string title)
        {
            if (string.IsNullOrEmpty(title)) return string.Empty;

            var lastDotIndex = title.LastIndexOf('.');
            return lastDotIndex > 0 ? title.Remove(lastDotIndex) : title;
        }
        public void Dispose()
        {
            if (_isDisposed) return;
            FlushAndClose();
            _isDisposed = true;
        }
    }
    /// <summary>
    /// 保存验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string OriginalStatus { get; set; }
        public string CurrentStatus { get; set; }
        // 使用C# 7.3表达式体构造函数
        public static ValidationResult Success() =>
            new ValidationResult { IsValid = true };
        public static ValidationResult Failure(string message, string original, string current) =>
            new ValidationResult
            {
                IsValid = false,
                Message = message,
                OriginalStatus = original,
                CurrentStatus = current
            };
    }
}
