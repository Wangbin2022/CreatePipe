using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AddParameterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddParameterWindow : Window
    {
        public AddParameterWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 添加参数视图模型
    /// </summary>
    public class AddParameterViewModel : ObserverableObject
    {
        private readonly UIDocument _uiDoc;
        private readonly ParameterAdditionService _service;

        private bool _isBatchMode;
        private string _folderPath;
        private bool _isProcessing;
        private string _statusMessage;
        private ProcessResult _lastResult;
        private ObservableCollection<string> _logMessages;

        public AddParameterViewModel(ExternalCommandData commandData)
        {
            _uiDoc = commandData.Application.ActiveUIDocument;
            _service = new ParameterAdditionService(commandData.Application.Application);
            _logMessages = new ObservableCollection<string>();

            // 初始化命令
            ExecuteCommand = new BaseBindingCommand(_ => Execute(), _ => !IsProcessing);
            BrowseFolderCommand = new BaseBindingCommand(_ => BrowseFolder());
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 设置默认文件夹路径
            var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FolderPath = System.IO.Path.Combine(docsPath, "AutoParameter_Families");
        }

        /// <summary>
        /// 是否为批量模式
        /// </summary>
        public bool IsBatchMode
        {
            get => _isBatchMode;
            set { _isBatchMode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 文件夹路径（批量模式使用）
        /// </summary>
        public string FolderPath
        {
            get => _folderPath;
            set { _folderPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 最后执行结果
        /// </summary>
        public ProcessResult LastResult
        {
            get => _lastResult;
            set { _lastResult = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 日志消息列表
        /// </summary>
        public ObservableCollection<string> LogMessages
        {
            get => _logMessages;
            set { _logMessages = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否可以执行
        /// </summary>
        public bool CanExecute => !IsProcessing && (!IsBatchMode || (!string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath)));

        public ICommand ExecuteCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 执行添加参数操作
        /// </summary>
        private void Execute()
        {
            IsProcessing = true;
            LogMessages.Clear();

            try
            {
                if (IsBatchMode)
                {
                    StatusMessage = "正在批量处理族文件...";
                    LastResult = _service.AddParametersToFamiliesInFolder(FolderPath);
                }
                else
                {
                    StatusMessage = "正在向当前族添加参数...";
                    var doc = _uiDoc.Document;
                    var (success, messages) = _service.AddParametersToCurrentDocument(doc);

                    LastResult = new ProcessResult
                    {
                        TotalFamilies = 1,
                        SuccessCount = success ? 1 : 0,
                        FailedCount = success ? 0 : 1
                    };

                    foreach (var msg in messages)
                    {
                        if (msg.Contains("失败") || msg.Contains("错误"))
                            LastResult.Errors.Add(msg);
                        else
                            LastResult.Warnings.Add(msg);
                        LogMessages.Add(msg);
                    }
                }

                StatusMessage = LastResult.IsSuccess ? "处理完成" : "处理完成，存在错误";

                // 显示结果摘要
                LogMessages.Add($"--- 结果摘要 ---");
                LogMessages.Add(LastResult.Summary);
                foreach (var err in LastResult.Errors)
                    LogMessages.Add($"错误: {err}");
                foreach (var warn in LastResult.Warnings)
                    LogMessages.Add($"警告: {warn}");
            }
            catch (Exception ex)
            {
                StatusMessage = "处理失败";
                LogMessages.Add($"异常: {ex.Message}");
                LastResult = new ProcessResult { FailedCount = 1 };
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 浏览文件夹
        /// </summary>
        private void BrowseFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择包含族文件的文件夹";
                dialog.SelectedPath = FolderPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FolderPath = dialog.SelectedPath;
                }
            }
        }
    }
    /// <summary>
    /// 参数添加服务
    /// </summary>
    public class ParameterAdditionService
    {
        private readonly Autodesk.Revit.ApplicationServices.Application _app;
        private readonly ParameterFileParser _parser;

        public ParameterAdditionService(Autodesk.Revit.ApplicationServices.Application app)
        {
            _app = app;
            _parser = new ParameterFileParser();
        }

        /// <summary>
        /// 向当前文档添加参数
        /// </summary>
        public (bool success, List<string> messages) AddParametersToCurrentDocument(Document doc)
        {
            var messages = new List<string>();

            // 验证是否为族文档
            if (!doc.IsFamilyDocument)
            {
                messages.Add("当前文档不是族文档");
                return (false, messages);
            }

            // 解析参数文件
            var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var familyParamFile = Path.Combine(assemblyPath, "FamilyParameter.txt");
            var sharedParamFile = Path.Combine(assemblyPath, "SharedParameter.txt");

            var (famSuccess, familyParams, famErrors) = _parser.LoadFamilyParameters(familyParamFile);
            var (sharedSuccess, sharedFile, sharedError) = _parser.LoadSharedParameterFile(sharedParamFile, _app);

            messages.AddRange(famErrors);
            if (!string.IsNullOrEmpty(sharedError)) messages.Add(sharedError);

            if (!famSuccess || !sharedSuccess)
                return (false, messages);

            // 添加参数
            using (var trans = new Transaction(doc, "添加族参数"))
            {
                trans.Start();

                var manager = doc.FamilyManager;
                var (addSuccess, addMessages) = AddFamilyParameters(manager, familyParams);
                messages.AddRange(addMessages);

                if (addSuccess && sharedFile != null)
                {
                    var (sharedAddSuccess, sharedMessages) = AddSharedParameters(manager, sharedFile);
                    messages.AddRange(sharedMessages);
                    addSuccess = sharedAddSuccess;
                }

                if (addSuccess)
                {
                    trans.Commit();
                    return (true, messages);
                }
                else
                {
                    trans.RollBack();
                    return (false, messages);
                }
            }
        }

        /// <summary>
        /// 批量向文件夹中的族文件添加参数
        /// </summary>
        public ProcessResult AddParametersToFamiliesInFolder(string folderPath)
        {
            var result = new ProcessResult();

            if (!Directory.Exists(folderPath))
            {
                result.Errors.Add($"文件夹不存在: {folderPath}");
                return result;
            }

            var familyFiles = Directory.GetFiles(folderPath, "*.rfa");
            result.TotalFamilies = familyFiles.Length;

            if (familyFiles.Length == 0)
            {
                result.Warnings.Add("未找到任何族文件(.rfa)");
                return result;
            }

            // 预加载参数定义（所有族使用相同的参数定义）
            var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var familyParamFile = Path.Combine(assemblyPath, "FamilyParameter.txt");
            var sharedParamFile = Path.Combine(assemblyPath, "SharedParameter.txt");

            var (famSuccess, familyParams, famErrors) = _parser.LoadFamilyParameters(familyParamFile);
            var (sharedSuccess, sharedFile, sharedError) = _parser.LoadSharedParameterFile(sharedParamFile, _app);

            foreach (var error in famErrors) result.Errors.Add(error);
            if (!string.IsNullOrEmpty(sharedError)) result.Errors.Add(sharedError);

            if (!famSuccess || !sharedSuccess)
                return result;

            // 逐个处理族文件
            foreach (var filePath in familyFiles)
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    result.Warnings.Add($"跳过只读文件: {fileInfo.Name}");
                    result.FailedCount++;
                    continue;
                }

                try
                {
                    var doc = _app.OpenDocumentFile(filePath);

                    if (!doc.IsFamilyDocument)
                    {
                        result.Errors.Add($"不是族文档: {fileInfo.Name}");
                        result.FailedCount++;
                        doc.Close(false);
                        continue;
                    }

                    using (var trans = new Transaction(doc, "添加族参数"))
                    {
                        trans.Start();

                        var manager = doc.FamilyManager;
                        var (addSuccess, addMessages) = AddFamilyParameters(manager, familyParams);

                        if (addSuccess && sharedFile != null)
                        {
                            var (sharedAddSuccess, sharedMessages) = AddSharedParameters(manager, sharedFile);
                            addSuccess = sharedAddSuccess;
                        }

                        if (addSuccess)
                        {
                            trans.Commit();
                            doc.Save();
                            result.SuccessCount++;
                        }
                        else
                        {
                            trans.RollBack();
                            result.FailedCount++;
                        }
                    }

                    doc.Close(true);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"处理失败 {Path.GetFileName(filePath)}: {ex.Message}");
                    result.FailedCount++;
                }
            }

            return result;
        }

        #region 私有辅助方法

        /// <summary>
        /// 添加族参数
        /// </summary>
        private (bool success, List<string> messages) AddFamilyParameters(
            FamilyManager manager, List<ParameterDefinition> parameters)
        {
            var messages = new List<string>();

            if (parameters == null || parameters.Count == 0)
            {
                messages.Add("没有需要添加的族参数");
                return (true, messages);
            }

            // 检查重复参数
            var existingParams = new HashSet<string>();
            foreach (FamilyParameter param in manager.Parameters)
            {
                existingParams.Add(param.Definition.Name);
            }

            var duplicateParams = parameters.Where(p => existingParams.Contains(p.Name)).ToList();
            foreach (var dup in duplicateParams)
            {
                messages.Add($"参数已存在: {dup.Name} (第{dup.LineNumber}行)");
            }

            if (duplicateParams.Any())
                return (false, messages);

            // 添加参数
            foreach (var param in parameters)
            {
                try
                {
                    manager.AddParameter(param.Name, param.Group, param.Type, param.IsInstance);
                    messages.Add($"成功添加: {param.Name}");
                }
                catch (Exception ex)
                {
                    messages.Add($"添加失败 {param.Name}: {ex.Message}");
                    return (false, messages);
                }
            }

            return (true, messages);
        }

        /// <summary>
        /// 添加共享参数
        /// </summary>
        private (bool success, List<string> messages) AddSharedParameters(
            FamilyManager manager, DefinitionFile sharedFile)
        {
            var messages = new List<string>();

            if (sharedFile == null)
                return (true, messages);

            foreach (DefinitionGroup group in sharedFile.Groups)
            {
                foreach (ExternalDefinition def in group.Definitions)
                {
                    // 检查是否已存在
                    var existing = manager.get_Parameter(def.Name);
                    if (existing != null)
                    {
                        messages.Add($"共享参数已存在，跳过: {def.Name}");
                        continue;
                    }

                    try
                    {
                        manager.AddParameter(def, def.ParameterGroup, true);
                        messages.Add($"成功添加共享参数: {def.Name}");
                    }
                    catch (Exception ex)
                    {
                        messages.Add($"添加共享参数失败 {def.Name}: {ex.Message}");
                        return (false, messages);
                    }
                }
            }

            return (true, messages);
        }

        #endregion
    }
    /// <summary>
    /// 参数文件解析服务
    /// </summary>
    public class ParameterFileParser
    {
        private readonly string _assemblyPath;

        public ParameterFileParser()
        {
            _assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        /// <summary>
        /// 从文本文件加载族参数定义
        /// </summary>
        public (bool success, List<ParameterDefinition> parameters, List<string> errors)
            LoadFamilyParameters(string filePath)
        {
            var parameters = new List<ParameterDefinition>();
            var errors = new List<string>();

            if (!File.Exists(filePath))
                return (true, parameters, errors); // 文件不存在不算错误

            var lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                int lineNumber = i + 1;

                // 跳过空行和注释行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("*"))
                    continue;

                // 按空白字符分割
                var parts = Regex.Split(line, @"\s+").Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

                if (parts.Count != 4)
                {
                    errors.Add($"第{lineNumber}行格式错误: 需要4个字段，实际{parts.Count}个");
                    continue;
                }

                try
                {
                    var param = new ParameterDefinition
                    {
                        Name = parts[0],
                        Group = ParseEnum<BuiltInParameterGroup>(parts[1], "BuiltInParameterGroup"),
                        Type = ParseEnum<ParameterType>(parts[2], "ParameterType"),
                        IsInstance = bool.Parse(parts[3]),
                        LineNumber = lineNumber,
                        Source = ParameterSource.FamilyParameter
                    };
                    parameters.Add(param);
                }
                catch (Exception ex)
                {
                    errors.Add($"第{lineNumber}行解析失败: {ex.Message}");
                }
            }

            return (errors.Count == 0, parameters, errors);
        }

        /// <summary>
        /// 设置共享参数文件路径并打开
        /// </summary>
        public (bool success, DefinitionFile definitionFile, string error)
            LoadSharedParameterFile(string filePath, Autodesk.Revit.ApplicationServices.Application app)
        {
            if (!File.Exists(filePath))
                return (true, null, null); // 文件不存在不算错误

            try
            {
                app.SharedParametersFilename = filePath;
                var defFile = app.OpenSharedParameterFile();

                if (defFile == null)
                    return (false, null, "共享参数文件格式无效");

                return (true, defFile, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"打开共享参数文件失败: {ex.Message}");
            }
        }

        ///// <summary>
        ///// 解析枚举值（兼容带命名空间前缀的格式）
        ///// </summary>
        //private T ParseEnum<T>(string value, string prefix) where T : struct
        //{
        //    var lowerValue = value.ToLower();
        //    var prefixLower = prefix.ToLower();
        //    if (lowerValue.Contains(prefixLower))
        //    {
        //        var startIndex = lowerValue.IndexOf(prefixLower) + prefixLower.Length;
        //        if (value[startIndex] == '.') startIndex++;
        //        value = value.Substring(startIndex);
        //    }
        //    return Enum.Parse<T>(value, true);
        //}
        /// <summary>
        /// 解析枚举值（兼容带命名空间前缀的格式）
        /// </summary>
        private T ParseEnum<T>(string value, string prefix) where T : struct
        {
            var lowerValue = value.ToLower();
            var prefixLower = prefix.ToLower();
            if (lowerValue.Contains(prefixLower))
            {
                var startIndex = lowerValue.IndexOf(prefixLower) + prefixLower.Length;
                if (startIndex < value.Length && value[startIndex] == '.') startIndex++;
                value = value.Substring(startIndex);
            }
            // .NET Framework 兼容：使用 Enum.TryParse 的非泛型方式
            if (Enum.TryParse(value, true, out T result))
            {
                return result;
            }
            // 或者使用转换方式
            object parsed = Enum.Parse(typeof(T), value, true);
            return (T)parsed;
        }
    }
    /// <summary>
    /// 参数定义模型
    /// </summary>
    public class ParameterDefinition
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 参数分组
        /// </summary>
        public BuiltInParameterGroup Group { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public ParameterType Type { get; set; }

        /// <summary>
        /// 是否为实例参数（true:实例, false:类型）
        /// </summary>
        public bool IsInstance { get; set; }

        /// <summary>
        /// 在文件中的行号（用于错误定位）
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 参数来源类型
        /// </summary>
        public ParameterSource Source { get; set; }

        /// <summary>
        /// 显示名称（用于UI）
        /// </summary>
        public string DisplayName => $"{Name} ({(IsInstance ? "实例" : "类型")})";
    }

    /// <summary>
    /// 参数来源类型
    /// </summary>
    public enum ParameterSource
    {
        FamilyParameter,  // 族参数
        SharedParameter   // 共享参数
    }

    /// <summary>
    /// 处理结果统计
    /// </summary>
    public class ProcessResult
    {
        public int TotalFamilies { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool IsSuccess => FailedCount == 0;
        public string Summary => $"成功: {SuccessCount}, 失败: {FailedCount}, 总计: {TotalFamilies}";
    }
}
