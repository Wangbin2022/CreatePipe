using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    public static class TaskDialogHelper
    {
        /// <summary>
        /// 显示自定义选择对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="iconType">图标类型: 0-无, 1-信息(蓝), 2-警告(黄), 3-错误(红)</param>
        /// <param name="instruction">大标题</param>
        /// <param name="content">正文内容</param>
        /// <param name="options">选项列表(最多4个)</param>
        /// <returns>返回选项索引(0-3)，关闭则返回-1</returns>
        public static int ShowCommandLinks(string title, int iconType, string instruction, string content, List<string> options)
        {
            TaskDialog td = new TaskDialog(title)
            {
                MainInstruction = instruction,
                MainContent = content,
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close
            };

            // 1. 设置图标映射
            switch (iconType)
            {
                case 1: td.MainIcon = TaskDialogIcon.TaskDialogIconInformation; break;
                case 2: td.MainIcon = TaskDialogIcon.TaskDialogIconWarning; break;
                case 3: td.MainIcon = TaskDialogIcon.TaskDialogIconError; break;
                default: td.MainIcon = TaskDialogIcon.TaskDialogIconNone; break;
            }
            // 根据传入的列表动态添加 CommandLink
            // 注意：TaskDialog 最多支持 4 个 CommandLink
            int count = options.Count > 4 ? 4 : options.Count;
            for (int i = 0; i < count; i++)
            {
                // 利用反射或简单的 Switch 映射 ID
                TaskDialogCommandLinkId id = (TaskDialogCommandLinkId)System.Enum.Parse(
                    typeof(TaskDialogCommandLinkId), $"CommandLink{i + 1}");
                td.AddCommandLink(id, options[i]);
            }
            TaskDialogResult result = td.Show();
            // 将 Result 映射回索引
            switch (result)
            {
                case TaskDialogResult.CommandLink1: return 0;
                case TaskDialogResult.CommandLink2: return 1;
                case TaskDialogResult.CommandLink3: return 2;
                case TaskDialogResult.CommandLink4: return 3;
                default: return -1; // 点击了关闭或其他
            }
        }
    }
}
