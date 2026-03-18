using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 编码检测辅助类
    /// </summary>
    public class EncodingHelper
    {
        /// <summary>
        /// 自动检测文件编码
        /// </summary>
        public static Encoding DetectEncoding(string filePath)
        {
            if (!File.Exists(filePath)) return Encoding.UTF8;
            byte[] bom = new byte[4];
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (file.Length >= 4)
                {
                    file.Read(bom, 0, 4);
                }
                else if (file.Length > 0)
                {
                    file.Read(bom, 0, (int)file.Length);
                }
            }
            // 检测 BOM
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;  // UTF-8 with BOM
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32;  // UTF-32 LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode;  // UTF-16 LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode;  // UTF-16 BE

            // 没有 BOM，尝试读取前 4KB 内容进行检测（避免读取整个大文件卡死内存）
            string content;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs, Encoding.Default))
            {
                char[] buffer = new char[4096];
                int readLen = reader.Read(buffer, 0, buffer.Length);
                content = new string(buffer, 0, readLen);
            }
            // 检测是否包含中文字符
            if (content.Any(c => c >= 0x4e00 && c <= 0x9fa5))
            {
                // 尝试 GB2312/GBK
                try
                {
                    // 在 .NET Framework (Revit 2024及更早) 中，直接获取即可，不需要 RegisterProvider
                    //在.NET 8 中，保留原代码，但你需要告诉 Visual Studio 去下载微软的扩展包：
                    //在 Visual Studio 中，右键你的项目->管理 NuGet 程序包(Manage NuGet Packages)。
                    //在“浏览”选项卡中搜索：System.Text.Encoding.CodePages....点击 安装。
                    //在代码文件顶部加上：using System.Text; 这样 CodePagesEncodingProvider.Instance 就能正常识别了。
                    //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    return Encoding.GetEncoding("GB2312");
                }
                catch
                {
                    return Encoding.UTF8;
                }
            }
            return Encoding.UTF8;  // 默认使用 UTF-8
        }
    }
}
