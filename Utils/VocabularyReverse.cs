using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    internal class VocabularyReverse : IExternalCommand
    {
        /// <summary>
        /// 反转字符串
        /// </summary>
        private static string ReverseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        ///// <summary>
        ///// 按单词逆序排序（从词尾到词首）,只支持一列
        ///// </summary>
        //public static void SortByReversedWords(string inputPath, string outputAscPath, Encoding encoding)
        //{
        //    try
        //    {
        //        // 1. 读取所有单词
        //        var words = File.ReadAllLines(inputPath, encoding)
        //            .Where(line => !string.IsNullOrWhiteSpace(line))
        //            .Select(line => line.Trim())
        //            .ToList();
        //        //Console.WriteLine($"读取到 {words.Count} 个单词");
        //        // 2. 创建 (原单词, 反转单词) 对
        //        var wordPairs = words.Select(word => new
        //        {
        //            Original = word,
        //            Reversed = ReverseString(word)
        //        }).ToList();
        //        //// 3. 按反转后的单词正序排序（A-Z）
        //        //var sortedAsc = wordPairs
        //        //    .OrderBy(pair => pair.Reversed, StringComparer.OrdinalIgnoreCase)
        //        //    .Select(pair => pair.Original)
        //        //    .ToList();
        //        //File.WriteAllLines(outputAscPath, sortedAsc, new UTF8Encoding(true));
        //        // 4. 按反转后的单词倒序排序（Z-A）
        //        var sortedDesc = wordPairs
        //            .OrderByDescending(pair => pair.Reversed, StringComparer.OrdinalIgnoreCase)
        //            .Select(pair => pair.Original)
        //            .ToList();
        //        File.WriteAllLines(outputAscPath, sortedDesc, new UTF8Encoding(true)); 
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"错误: {ex.Message}");
        //    }
        //}       
        /// <summary>
        /// 解析 CSV 行（简单版本，不处理引号内的逗号）
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            return line.Split(',').Select(field => field.Trim()).ToArray();
        }
        /// <summary>
        /// 多列 CSV 按第一列逆序排序
        /// </summary>
        public static int SortByReversedWords(string inputPath, string outputPath, Encoding encoding, bool hasHeader = false)
        {
            try
            {
                // 1. 读取所有行
                var lines = File.ReadAllLines(inputPath, encoding)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                if (lines.Count == 0)
                {
                    Console.WriteLine("文件为空");
                    return 0;
                }
                string headerLine = null;
                int startIndex = 0;
                // 2. 处理标题行
                if (hasHeader && lines.Count > 0)
                {
                    headerLine = lines[0];
                    startIndex = 1;
                    //Console.WriteLine($"标题行: {headerLine}");
                }
                // 3. 解析数据行
                var dataRows = lines
                    .Skip(startIndex)
                    .Select(line => new
                    {
                        OriginalLine = line,
                        Fields = ParseCsvLine(line),
                        FirstColumn = ParseCsvLine(line).Length > 0 ? ParseCsvLine(line)[0] : ""
                    })
                    .Where(row => !string.IsNullOrEmpty(row.FirstColumn))
                    .Select(row => new
                    {
                        row.OriginalLine,
                        row.Fields,
                        row.FirstColumn,
                        ReversedFirstColumn = ReverseString(row.FirstColumn)
                    })
                    .ToList();
                //Console.WriteLine($"读取到 {dataRows.Count} 行数据");
                // 4. 按第一列的反转字符串倒序排序（Z-A）
                var sortedRows = dataRows
                    .OrderByDescending(row => row.ReversedFirstColumn, StringComparer.OrdinalIgnoreCase)
                    .Select(row => row.OriginalLine)
                    .ToList();
                // 5. 写入文件
                var outputLines = new List<string>();
                if (hasHeader && headerLine != null)
                {
                    outputLines.Add(headerLine);
                }
                outputLines.AddRange(sortedRows);
                File.WriteAllLines(outputPath, outputLines, new UTF8Encoding(true));
                return lines.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                throw;
            }
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //0302 逆序单词
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "(*.csv)|*.csv";
            openFileDialog.Title = "请选择 CSV 文件";
            if (openFileDialog.ShowDialog() != true) return Result.Cancelled;
            //检测csv文件编码
            Encoding encoding = EncodingHelper.DetectEncoding(openFileDialog.FileName);
            //TaskDialog.Show("tt", $"检测到文件编码: {encoding.EncodingName}");
            //单列csv逆序重排
            //SortByReversedWords(openFileDialog.FileName, "words_reverse_asc.csv", encoding);
            //多列csv逆序重排
            int lines = SortByReversedWords(openFileDialog.FileName, "words_reverse_asc.csv", encoding);
            TaskDialog.Show("tt", $"任务已完成，处理{lines}行数据，words_reverse_asc.csv保存到桌面");

            return Result.Succeeded;
        }
    }
}
