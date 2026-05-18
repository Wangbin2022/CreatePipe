using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;

namespace CreatePipe.OfficalSamples
{
    internal class CreateSharedParameter
    {
        /// <summary>共享参数文件名</summary>
        private const string SHARED_PARAM_FILE_NAME = "RevitParameters.txt";
        /// <summary>参数组名称</summary>
        private const string PARAMETER_GROUP_NAME = "RevitParameters";
        /// <summary>参数名称</summary>
        private const string PARAMETER_NAME = "APIParameter";
        /// <summary>目标类别名称</summary>
        private const string TARGET_CATEGORY_NAME = "Walls";
        /// <summary>参数类型</summary>
        private const ParameterType PARAMETER_TYPE = ParameterType.Text;
        public CreateSharedParameter(ExternalCommandData commandData)
        {
            string message = string.Empty;
            // 验证输入
            if (!TryValidateCommandData(commandData, ref message, out var document, out var app))
                return;
            // 设置共享参数文件路径
            if (!TrySetSharedParameterFilePath(app, out var paramFilePath, ref message)) return;
            // 打开共享参数文件
            if (!TryOpenSharedParameterFile(app, paramFilePath, out var definitionFile, ref message))
                return;
            // 执行事务：创建参数并绑定
            CreateAndBindSharedParameter(document, app, definitionFile, ref message);
        }
        /// <summary>
        /// 验证命令数据是否有效
        /// 使用C# 7.3的out变量和模式匹配
        /// </summary>
        private bool TryValidateCommandData(ExternalCommandData commandData, ref string message,
            out Document document, out Autodesk.Revit.ApplicationServices.Application app)
        {
            document = null;
            app = null;
            if (commandData?.Application?.ActiveUIDocument is null)
            {
                message = "无法获取活动文档";
                return false;
            }
            document = commandData.Application.ActiveUIDocument.Document;
            app = commandData.Application.Application;
            return true;
        }
        /// <summary>
        /// 设置共享参数文件路径
        /// 使用C# 7.3的Path.Combine和表达式体
        /// </summary>
        private bool TrySetSharedParameterFilePath(Autodesk.Revit.ApplicationServices.Application app,
            out string paramFilePath, ref string message)
        {
            paramFilePath = null;

            // 获取DLL所在目录
            var dllLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(dllLocation))
            {
                message = "无法获取插件所在目录";
                return false;
            }

            // 组合完整文件路径
            paramFilePath = Path.Combine(dllLocation, SHARED_PARAM_FILE_NAME);

            // 设置共享参数文件路径
            app.SharedParametersFilename = paramFilePath;

            return true;
        }

        /// <summary>
        /// 打开共享参数文件
        /// 使用C# 7.3的模式匹配和using声明
        /// </summary>
        private bool TryOpenSharedParameterFile(Autodesk.Revit.ApplicationServices.Application app,
            string filePath, out DefinitionFile definitionFile, ref string message)
        {
            definitionFile = null;

            if (!File.Exists(filePath))
            {
                message = $"共享参数文件不存在: {filePath}";
                return false;
            }

            try
            {
                definitionFile = app.OpenSharedParameterFile();
                if (definitionFile is null)
                {
                    message = $"无法打开共享参数文件: {filePath}";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = $"打开共享参数文件失败: {ex.Message}";
                return false;
            }
        }
        /// <summary>
        /// 创建并绑定共享参数
        /// 使用C# 7.3的using声明和本地函数
        /// </summary>
        private Result CreateAndBindSharedParameter(Document document,
            Autodesk.Revit.ApplicationServices.Application app,
            DefinitionFile definitionFile, ref string message)
        {
            var transaction = new Transaction(document, "创建共享参数");
            transaction.Start();

            // 获取或创建参数组
            var parameterGroup = GetOrCreateParameterGroup(definitionFile);
            if (parameterGroup is null)
            {
                message = "无法创建或获取参数组";
                transaction.RollBack();
                return Result.Failed;
            }

            // 获取或创建参数定义
            var parameterDefinition = GetOrCreateParameterDefinition(parameterGroup);
            if (parameterDefinition is null)
            {
                message = "无法创建或获取参数定义";
                transaction.RollBack();
                return Result.Failed;
            }

            // 创建类别集并绑定
            if (!TryCreateBinding(document, app, parameterDefinition, ref message))
            {
                transaction.RollBack();
                return Result.Failed;
            }

            transaction.Commit();
            message = $"成功创建共享参数: {PARAMETER_GROUP_NAME}/{PARAMETER_NAME}";
            return Result.Succeeded;
        }

        /// <summary>
        /// 获取或创建参数组
        /// 使用C# 7.3的null条件运算符
        /// </summary>
        private DefinitionGroup GetOrCreateParameterGroup(DefinitionFile definitionFile)
        {
            // 尝试获取已存在的组
            //var group = definitionFile.Groups?.Item[PARAMETER_GROUP_NAME];
            var group = definitionFile.Groups.get_Item(PARAMETER_GROUP_NAME);

            // 不存在则创建
            if (group is null)
            {
                group = definitionFile.Groups?.Create(PARAMETER_GROUP_NAME);
            }

            return group;
        }

        /// <summary>
        /// 获取或创建参数定义
        /// 使用C# 7.3的表达式体和对象初始化器
        /// </summary>
        private Definition GetOrCreateParameterDefinition(DefinitionGroup parameterGroup)
        {
            // 尝试获取已存在的定义
            //var definition = parameterGroup.Definitions?.Item[PARAMETER_NAME];
            var definition = parameterGroup.Definitions.get_Item(PARAMETER_NAME);

            // 不存在则创建
            if (definition is null)
            {
                var creationOptions = new ExternalDefinitionCreationOptions(PARAMETER_NAME, PARAMETER_TYPE)
                {
                    // 可选的附加属性设置
                    Visible = true,
                    UserModifiable = true
                };
                definition = parameterGroup.Definitions.Create(creationOptions);
            }

            return definition;
        }

        /// <summary>
        /// 创建参数绑定
        /// 使用C# 7.3的模式匹配和集合初始化器
        /// </summary>
        private bool TryCreateBinding(Document document,
            Autodesk.Revit.ApplicationServices.Application app,
            Definition parameterDefinition, ref string message)
        {
            // 获取目标类别
            var wallCategory = document.Settings.Categories.get_Item(TARGET_CATEGORY_NAME);
            if (wallCategory is null)
            {
                message = $"未找到目标类别: {TARGET_CATEGORY_NAME}";
                return false;
            }

            // 创建类别集
            var categories = app.Create.NewCategorySet();
            categories.Insert(wallCategory);

            // 创建实例绑定（实例参数，非类型参数）
            var instanceBinding = app.Create.NewInstanceBinding(categories);

            // 绑定参数
            document.ParameterBindings.Insert(parameterDefinition, instanceBinding);

            return true;
        }
    }
    /// <summary>
    /// 共享参数扩展方法
    /// 使用C# 7.3的表达式体成员
    /// </summary>
    public static class SharedParameterExtensions
    {
        /// <summary>
        /// 检查共享参数文件是否已配置
        /// </summary>
        public static bool IsSharedParameterFileConfigured(this Autodesk.Revit.ApplicationServices.Application app)
        {
            var filePath = app.SharedParametersFilename;
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }

        /// <summary>
        /// 获取共享参数文件信息
        /// 使用C# 7.3的元组返回
        /// </summary>
        public static (bool exists, string filePath, DateTime lastWriteTime) GetSharedParameterFileInfo(
            this Autodesk.Revit.ApplicationServices.Application app)
        {
            var filePath = app.SharedParametersFilename;
            var exists = !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
            var lastWriteTime = exists ? File.GetLastWriteTime(filePath) : DateTime.MinValue;

            return (exists, filePath, lastWriteTime);
        }
    }
}
