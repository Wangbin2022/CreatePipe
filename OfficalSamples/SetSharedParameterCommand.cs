using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class SetSharedParameterCommand
    {
        /// <summary>共享参数组名称</summary>
        private const string PARAMETER_GROUP_NAME = "RevitParameters";
        /// <summary>共享参数名称</summary>
        private const string PARAMETER_NAME = "APIParameter";
        /// <summary>要设置的参数值</summary>
        private const string PARAMETER_VALUE = "Hello Revit";
        /// <summary>目标元素类别名称</summary>
        private const string TARGET_CATEGORY_NAME = "Walls";
        public SetSharedParameterCommand(ExternalCommandData commandData)
        {
            string message = string.Empty;
            // 验证输入
            if (!TryValidateCommandData(commandData, ref message, out var document))
                return;
            // 获取共享参数定义
            if (!TryGetSharedParameterDefinition(commandData, out var parameterDefinition, ref message))
                return;
            // 获取需要更新的墙元素
            var walls = GetWallInstances(document);
            if (!walls.Any())
            {
                TaskDialog.Show("tt", "未找到任何墙实例元素");
                return;
            }
            // 批量设置参数
            SetParameterForWalls(document, walls, parameterDefinition, ref message);
        }
        /// <summary>
        /// 验证命令数据是否有效
        /// 使用C# 7.3的out变量和模式匹配
        /// </summary>
        private bool TryValidateCommandData(ExternalCommandData commandData, ref string message, out Document document)
        {
            document = null;

            if (commandData?.Application?.ActiveUIDocument is null)
            {
                message = "无法获取活动文档";
                return false;
            }
            document = commandData.Application.ActiveUIDocument.Document;
            return true;
        }
        /// <summary>
        /// 获取共享参数定义
        /// 使用C# 7.3的null条件运算符和模式匹配
        /// </summary>
        private bool TryGetSharedParameterDefinition(ExternalCommandData commandData,
            out Definition parameterDefinition, ref string message)
        {
            parameterDefinition = null;
            // 获取共享参数文件
            var sharedParameterFile = commandData.Application.Application.OpenSharedParameterFile();
            if (sharedParameterFile is null)
            {
                message = "未找到共享参数文件，请确保已正确设置共享参数文件路径";
                return false;
            }
            // 使用null条件运算符安全访问
            //var parameterGroup = sharedParameterFile.Groups?.Item[PARAMETER_GROUP_NAME];
            var parameterGroup = sharedParameterFile.Groups.get_Item(PARAMETER_GROUP_NAME);
            if (parameterGroup is null)
            {
                message = $"未找到共享参数组: {PARAMETER_GROUP_NAME}";
                return false;
            }
            // 获取参数定义
            //parameterDefinition = parameterGroup.Definitions?.Item[PARAMETER_NAME];
            parameterDefinition = parameterGroup.Definitions.get_Item(PARAMETER_NAME);
            if (parameterDefinition is null)
            {
                message = $"未找到共享参数: {PARAMETER_NAME}";
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取所有墙实例（非类型元素）
        /// 使用C# 7.3的LINQ和模式匹配
        /// </summary>
        private List<Element> GetWallInstances(Document document)
        {
            // 使用元素类型过滤器（反向：获取非类型元素）
            var typeFilter = new ElementIsElementTypeFilter(true); // true = 反转，获取实例元素
            var collector = new FilteredElementCollector(document);
            // 使用LINQ链式调用筛选
            return collector
                .WherePasses(typeFilter)
                .Cast<Element>()
                .Where(element => !(element is ElementType))  // 确保不是类型元素
                .Where(element => element.Category != null)
                .Where(element => element.Category.Name == TARGET_CATEGORY_NAME)
                .ToList();
        }
        /// <summary>
        /// 为墙元素批量设置参数
        /// 使用C# 7.3的using声明和本地函数
        /// </summary>
        private Result SetParameterForWalls(Document document, List<Element> walls,
            Definition parameterDefinition, ref string message)
        {
            var transaction = new Transaction(document, "设置共享参数");
            transaction.Start();

            var successCount = 0;
            var failCount = 0;
            var parameterName = parameterDefinition.Name;

            foreach (var wall in walls)
            {
                // 使用本地函数尝试设置参数
                if (TrySetParameterValue(wall, parameterName, PARAMETER_VALUE))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            transaction.Commit();
            // 构建结果消息
            message = $"参数设置完成: 成功 {successCount} 个, 失败 {failCount} 个";
            return successCount > 0 ? Result.Succeeded : Result.Failed;
        }
        /// <summary>
        /// 尝试为单个元素设置参数值
        /// 使用C# 7.3的LINQ和模式匹配优化参数查找
        /// </summary>
        private bool TrySetParameterValue(Element element, string parameterName, string value)
        {
            if (element is null) return false;
            var parameter = element.Parameters
                                   .Cast<Parameter>()
                                   .FirstOrDefault(p => p.Definition?.Name == parameterName);
            if (parameter is null) return false;
            try
            {
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        return SetStringParameter(parameter, value);
                    case StorageType.Integer:
                        return SetIntegerParameter(parameter, value);
                    case StorageType.Double:
                        return SetDoubleParameter(parameter, value);
                    case StorageType.ElementId:
                        return SetElementIdParameter(parameter, value);
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置字符串类型参数
        /// 使用表达式体方法
        /// </summary>
        private bool SetStringParameter(Parameter parameter, string value)
        {
            parameter.Set(value);
            return true;
        }
        /// <summary>
        /// 设置整数类型参数
        /// </summary>
        private bool SetIntegerParameter(Parameter parameter, string value)
        {
            if (int.TryParse(value, out var intValue))
            {
                parameter.Set(intValue);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 设置浮点数类型参数
        /// </summary>
        private bool SetDoubleParameter(Parameter parameter, string value)
        {
            if (double.TryParse(value, out var doubleValue))
            {
                parameter.Set(doubleValue);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 设置ElementId类型参数
        /// </summary>
        private bool SetElementIdParameter(Parameter parameter, string value)
        {
            // 字符串转ElementId需要特殊处理，这里简单返回false
            return false;
        }

    }
}
