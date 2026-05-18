using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.Utils
{
    public static class RevitNameHelper
    {
        // Revit 名称中不允许的非法字符
        private static readonly char[] InvalidChars = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };

        /// <summary>
        /// 检查输入的 Revit 名称是否合法（不为空、无非法字符、不重名）
        /// </summary>
        /// <param name="existingNames">已存在的名称集合</param>
        /// <param name="newName">待验证的新名称</param>
        /// <param name="errorMessage">如果不合法，返回具体的错误提示信息</param>
        /// <returns>合法返回 true，否则返回 false</returns>
        public static bool IsValidName(IEnumerable<string> existingNames, string newName, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. 检查非空
            if (string.IsNullOrWhiteSpace(newName))
            {
                errorMessage = "名称不能为空或仅包含空格。";
                return false;
            }

            // 2. 检查是否包含非法字符 (使用 IndexOfAny 性能远高于 foreach)
            if (newName.IndexOfAny(InvalidChars) >= 0)
            {
                errorMessage = $"名称中不能包含以下非法字符：\n{string.Join("  ", InvalidChars)}";
                return false;
            }

            // 3. 检查是否重名 (忽略大小写)
            if (existingNames != null && existingNames.Contains(newName, StringComparer.OrdinalIgnoreCase))
            {
                errorMessage = $"名称 “{newName}” 已存在，请使用其他名称。";
                return false;
            }

            return true;
        }
    }

}
