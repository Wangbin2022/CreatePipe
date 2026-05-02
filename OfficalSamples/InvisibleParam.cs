using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe.OfficalSamples
{
    internal class InvisibleParam
    {
        // 常量定义 - 使用const
        private const string PARAM_FILE_NAME = "RevitParameters.txt";
        private const string GROUP_NAME = "APIGroup";
        public InvisibleParam(ExternalCommandData commandData)
        {
            // 使用using语句简化事务管理
            using (var transaction = new Transaction(
                commandData.Application.ActiveUIDocument.Document,
                "创建共享参数"))
            {
                try
                {
                    transaction.Start();

                    // 获取参数文件路径（使用Path.Combine和表达式体）
                    string exePath = Assembly.GetExecutingAssembly().Location;
                    string paramFilePath = Path.Combine(Path.GetDirectoryName(exePath), PARAM_FILE_NAME);

                    // 创建或清空参数文件（使用using简化）
                    if (File.Exists(paramFilePath)) File.Delete(paramFilePath);
                    using (File.Create(paramFilePath)) { }  // 创建空文件并立即关闭

                    // 设置共享参数文件
                    Autodesk.Revit.ApplicationServices.Application revitApp = commandData.Application.Application;
                    revitApp.SharedParametersFilename = paramFilePath;

                    // 打开共享参数文件
                    DefinitionFile paramFile = revitApp.OpenSharedParameterFile()
                        ?? throw new InvalidOperationException("无法打开共享参数文件");

                    // 获取墙类别
                    Category wallCat = commandData.Application.ActiveUIDocument.Document
                        .Settings.Categories.get_Item(BuiltInCategory.OST_Walls);

                    // 创建类别集合并添加墙类别
                    CategorySet categories = revitApp.Create.NewCategorySet();
                    categories.Insert(wallCat);

                    // 创建实例绑定
                    InstanceBinding binding = revitApp.Create.NewInstanceBinding(categories);

                    // 获取或创建参数组（使用null合并运算符）
                    DefinitionGroup group = paramFile.Groups.get_Item(GROUP_NAME)
                        ?? paramFile.Groups.Create(GROUP_NAME);

                    // 创建可见参数（使用对象初始化器）
                    var visibleOptions = new ExternalDefinitionCreationOptions("VisibleParam", ParameterType.Text);
                    Definition visibleParamDef = group.Definitions.Create(visibleOptions);

                    // 创建不可见参数
                    var invisibleOptions = new ExternalDefinitionCreationOptions("InvisibleParam", ParameterType.Text);
                    Definition invisibleParamDef = group.Definitions.Create(invisibleOptions);

                    // 绑定参数
                    BindingMap bindingMap = commandData.Application.ActiveUIDocument.Document.ParameterBindings;
                    bindingMap.Insert(visibleParamDef, binding);
                    bindingMap.Insert(invisibleParamDef, binding);

                    transaction.Commit();
                }
                //catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                //{
                //    // 文件访问异常筛选
                //    message = $"文件操作失败: {ex.Message}";
                //    transaction.RollBack();
                //    return Result.Failed;
                //}
                catch (Exception ex)
                {
                    //message = ex.ToString();
                    transaction.RollBack();
                    return;
                }
            }
        }
    }
}
