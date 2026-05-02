using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 文本注释格式批量转换命令
    /// 功能：将文档中所有未应用全大写格式的文本注释转换为全大写
    /// 使用C# 7.3语法特性：表达式体、模式匹配、out变量、本地函数
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FormatStatusExtensions : IExternalCommand
    {
        /// <summary>
        /// 外部命令执行入口
        /// 使用C# 7.3的表达式体和模式匹配简化代码
        /// </summary>
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // 获取当前文档 - 使用C# 7.3的嵌套属性访问
                var document = commandData?.Application?.ActiveUIDocument?.Document;

                // 使用null条件运算符进行安全验证
                if (document is null)
                {
                    message = "无法获取当前文档";
                    return Result.Failed;
                }

                // 使用本地函数处理核心逻辑（C# 7.3特性）
                return ProcessTextNotes(document, ref message, elements);
            }
            catch (Exception ex)
            {
                // 使用nameof获取参数名，避免硬编码字符串
                message = $"{nameof(Execute)}执行异常: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// 处理文本注释的核心逻辑
        /// 使用C# 7.3的本地函数和模式匹配
        /// </summary>
        private Result ProcessTextNotes(Document document, ref string message, ElementSet elements)
        {
            // 使用本地函数获取需要更新的文本注释（C# 7.3）
            var textNotesToUpdate = FindTextNotesNeedingUpdate(document).ToList();

            // 模式匹配 + 元组解构判断结果
            if (textNotesToUpdate.Count == 0)
            {
                message = textNotesToUpdate.Count == 0
                    ? "文档中没有需要更新的文本注释（要么没有TextNote，要么都已是大写格式）"
                    : "文档中未找到TextNote元素";
                return Result.Failed;
            }

            // 使用本地函数执行批量更新（C# 7.3）
            return ApplyAllCapsFormatting(document, textNotesToUpdate, ref message);
        }

        /// <summary>
        /// 查找需要更新格式的文本注释
        /// 使用C# 7.3的yield return和LINQ简化
        /// </summary>
        private System.Collections.Generic.IEnumerable<TextNote> FindTextNotesNeedingUpdate(Document document)
        {
            // 使用C# 7.3的out变量内联声明
            var collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(TextNote));

            // 使用模式匹配和LINQ简化遍历
            foreach (var element in collector)
            {
                // 使用C# 7.3的模式匹配进行类型转换
                if (!(element is TextNote textNote))
                    continue;

                // 提取格式化文本
                var formattedText = textNote.GetFormattedText();

                // 使用C# 7.3的模式匹配和条件表达式
                // 只有当不是全大写格式时才需要更新
                if (formattedText.GetAllCapsStatus() != FormatStatus.All)
                {
                    yield return textNote;
                }
            }
        }

        /// <summary>
        /// 应用全大写格式到指定的文本注释列表
        /// 使用C# 7.3的本地函数和using声明简化代码
        /// </summary>
        private Result ApplyAllCapsFormatting(Document document,
            System.Collections.Generic.List<TextNote> textNotesToUpdate,
            ref string message)
        {
            Transaction transaction = new Transaction(document, "批量转换文本注释为大写");
            try
            {
                transaction.Start();
                // 使用C# 7.3的ForEach和Lambda表达式简化
                int updatedCount = 0;
                foreach (var textNote in textNotesToUpdate)
                {
                    if (ApplyAllCapsToTextNote(textNote))
                    {
                        updatedCount++;
                    }
                }
                transaction.Commit();
                // 使用字符串插值（C# 7.3支持）
                message = $"成功更新 {updatedCount} 个文本注释为大写格式";
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                message = $"更新失败: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// 为单个文本注释应用全大写格式
        /// 使用C# 7.3的表达式体和模式匹配
        /// </summary>
        private bool ApplyAllCapsToTextNote(TextNote textNote)
        {
            // 防御性检查
            if (textNote is null) return false;

            // 获取格式化文本
            var formattedText = textNote.GetFormattedText();
            if (formattedText is null) return false;

            // 获取当前大写格式状态
            var currentStatus = formattedText.GetAllCapsStatus();

            bool needsUpdate;
            switch (currentStatus)
            {
                case FormatStatus.None:   // 完全没有大写格式
                    needsUpdate = true;
                    break;
                case FormatStatus.Mixed:  // 部分字符有大写格式
                    needsUpdate = true;
                    break;
                case FormatStatus.All:    // 已经是全大写
                    needsUpdate = false;
                    break;
                default:                   // 未知状态
                    needsUpdate = false;
                    break;
            }
            //更简洁的 if-else 版本：
            //bool needsUpdate = false;
            //if (currentStatus == FormatStatus.None || currentStatus == FormatStatus.Mixed)
            //{
            //    needsUpdate = true;   // 完全没有大写格式 或 部分字符有大写格式
            //}
            //else if (currentStatus == FormatStatus.All)
            //{
            //    needsUpdate = false;  // 已经是全大写
            //}

            if (!needsUpdate) return false;

            // 应用全大写格式（使用表达式体方法简化的调用）
            formattedText.SetAllCapsStatus(true);

            // 应用格式回文本注释
            textNote.SetFormattedText(formattedText);

            return true;
        }
    }
}
