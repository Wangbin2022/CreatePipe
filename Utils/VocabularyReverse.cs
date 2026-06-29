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
                var lines = File.ReadAllLines(inputPath, encoding).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
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
                }
                // 3. 解析数据行（只解析一次！）
                var dataRows = lines.Skip(startIndex)
                    .Select(line =>
                    {
                        var fields = ParseCsvLine(line);  // 只解析一次
                        return new
                        {
                            OriginalLine = line,
                            Fields = fields,
                            FirstColumn = fields.Length > 0 ? fields[0] : "",
                            ReversedFirstColumn = fields.Length > 0 ? ReverseString(fields[0]) : ""
                        };
                    })
                    .Where(row => !string.IsNullOrEmpty(row.FirstColumn))
                    .ToList();
                // 4. 排序
                var sortedRows = dataRows.OrderByDescending(row => row.ReversedFirstColumn, StringComparer.OrdinalIgnoreCase)
                    .Select(row => row.OriginalLine);
                // 5. 写入文件（使用流，避免构建完整列表）
                using (var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true)))
                {
                    if (hasHeader && headerLine != null)
                    {
                        writer.WriteLine(headerLine);
                    }
                    foreach (var row in sortedRows)
                    {
                        writer.WriteLine(row);
                    }
                }
                return lines.Count;
                //    // 1. 读取所有行
                //    var lines = File.ReadAllLines(inputPath, encoding).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                //    if (lines.Count == 0)
                //    {
                //        Console.WriteLine("文件为空");
                //        return 0;
                //    }
                //    string headerLine = null;
                //    int startIndex = 0;
                //    // 2. 处理标题行
                //    if (hasHeader && lines.Count > 0)
                //    {
                //        headerLine = lines[0];
                //        startIndex = 1;
                //        //Console.WriteLine($"标题行: {headerLine}");
                //    }
                //    // 3. 解析数据行
                //    var dataRows = lines.Skip(startIndex).Select(line => new
                //    {
                //        OriginalLine = line,
                //        Fields = ParseCsvLine(line),
                //        FirstColumn = ParseCsvLine(line).Length > 0 ? ParseCsvLine(line)[0] : ""
                //    }).Where(row => !string.IsNullOrEmpty(row.FirstColumn))
                //        .Select(row => new
                //        {
                //            row.OriginalLine,
                //            row.Fields,
                //            row.FirstColumn,
                //            ReversedFirstColumn = ReverseString(row.FirstColumn)
                //        }).ToList();
                //    //Console.WriteLine($"读取到 {dataRows.Count} 行数据");
                //    // 4. 按第一列的反转字符串倒序排序（Z-A）
                //    var sortedRows = dataRows.OrderByDescending(row => row.ReversedFirstColumn, StringComparer.OrdinalIgnoreCase)
                //        .Select(row => row.OriginalLine).ToList();
                //    // 5. 写入文件
                //    var outputLines = new List<string>();
                //    if (hasHeader && headerLine != null)
                //    {
                //        outputLines.Add(headerLine);
                //    }
                //    outputLines.AddRange(sortedRows);
                //    File.WriteAllLines(outputPath, outputLines, new UTF8Encoding(true));
                //    return lines.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                throw;
            }
        }
        //随机排列最后一列字符串
        private static string FormatCsvLine(string[] fields)
        {
            return string.Join(",", fields);
        }
        public static int RandomLastColumn(string inputPath, string outputPath, Encoding encoding, bool hasHeader = false)
        {
            try
            {
                // 1. 读取所有行
                var lines = File.ReadAllLines(inputPath, encoding).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
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
                }
                // 3. 解析数据行（只解析一次，缓存结果）
                var parsedRows = lines.Skip(startIndex).Select(line => ParseCsvLine(line))
                    .Where(fields => fields.Length > 0 && !string.IsNullOrEmpty(fields[0]))
                    .Select(fields => new
                    {
                        Fields = fields,
                        LastColumn = fields.Last()
                    }).ToList();
                if (parsedRows.Count == 0)
                {
                    Console.WriteLine("没有有效数据行");
                    return 0;
                }
                // 4. 提取最后一列并使用 Fisher-Yates 算法随机重排
                var lastColumnValues = parsedRows.Select(r => r.LastColumn).ToList();
                var random = new Random();
                for (int i = lastColumnValues.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (lastColumnValues[i], lastColumnValues[j]) = (lastColumnValues[j], lastColumnValues[i]);
                }
                // 5. 重新组合数据行
                var recombinedRows = new List<string>(parsedRows.Count);
                for (int i = 0; i < parsedRows.Count; i++)
                {
                    var fields = parsedRows[i].Fields;
                    // 提取除最后一列外的所有列
                    var otherFields = fields.Length > 1
                        ? fields.Take(fields.Length - 1).ToArray()
                        : Array.Empty<string>();
                    // 组合：其他字段 + 随机重排后的最后一列
                    var allFields = otherFields.Concat(new[] { lastColumnValues[i] }).ToArray();
                    // 格式化为CSV行
                    var csvLine = FormatCsvLine(allFields);
                    recombinedRows.Add(csvLine);
                }
                // 6. 构建输出
                var outputLines = new List<string>(parsedRows.Count + (hasHeader ? 1 : 0));
                if (hasHeader && headerLine != null)
                {
                    outputLines.Add(headerLine);
                }
                outputLines.AddRange(recombinedRows);
                // 7. 写入文件（使用UTF8 with BOM）
                File.WriteAllLines(outputPath, outputLines, new UTF8Encoding(true));
                return lines.Count;
                //// 1. 读取所有行
                //var lines = File.ReadAllLines(inputPath, encoding).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                //if (lines.Count == 0)
                //{
                //    Console.WriteLine("文件为空");
                //    return 0;
                //}
                //string headerLine = null;
                //int startIndex = 0;
                //// 2. 处理标题行
                //if (hasHeader && lines.Count > 0)
                //{
                //    headerLine = lines[0];
                //    startIndex = 1;
                //}
                //// 3. 解析数据行（一次性解析，避免重复调用ParseCsvLine）
                //var parsedRows = lines.Skip(startIndex).Select(line => ParseCsvLine(line))
                //    .Where(fields => fields.Length > 0 && !string.IsNullOrEmpty(fields[0])).ToList();
                //if (parsedRows.Count == 0)
                //{
                //    Console.WriteLine("没有有效数据行");
                //    return 0;
                //}
                //// 4. 提取最后一列并随机重排
                //var random = new Random(Guid.NewGuid().GetHashCode()); // 使用更随机的种子
                //var lastColumnValues = parsedRows.Select(fields => fields.Last()).OrderBy(v => random.Next()).ToList();
                //// 5. 重新组合数据行
                //var recombinedRows = new List<string>();
                //for (int i = 0; i < parsedRows.Count; i++)
                //{
                //    var fields = parsedRows[i];
                //    // 提取除最后一列外的所有列
                //    var otherFields = fields.Length > 1 ? fields.Take(fields.Length - 1).ToArray() : new string[0];
                //    // 组合：其他字段 + 随机重排后的最后一列
                //    var allFields = otherFields.Concat(new[] { lastColumnValues[i] }).ToArray();
                //    // 格式化为CSV行（处理特殊字符）
                //    var csvLine = FormatCsvLine(allFields);
                //    recombinedRows.Add(csvLine);
                //}
                //// 6. 构建输出
                //var outputLines = new List<string>();
                //if (hasHeader && headerLine != null)
                //{
                //    outputLines.Add(headerLine);
                //}
                //outputLines.AddRange(recombinedRows);
                //// 7. 写入文件（使用UTF8 with BOM）
                //File.WriteAllLines(outputPath, outputLines, new UTF8Encoding(true));
                //return lines.Count;
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
            TaskDialog td = new TaskDialog("选择操作")
            {
                MainInstruction = "请选择要执行的操作:",
                CommonButtons = TaskDialogCommonButtons.Cancel
            };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "将csv最后一列顺序随机重排");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "根据csv第一列字母逆序重排");
            TaskDialogResult tdRes = td.Show();
            if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
            bool randomWords = (tdRes == TaskDialogResult.CommandLink1);
            if (randomWords)
            {
                // 保存文件到下载目录
                string fileName = "words_random_asc.csv";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Console.WriteLine($"文档: {documentsPath}");
                string filePath = Path.Combine(documentsPath, fileName);
                int lines = RandomLastColumn(openFileDialog.FileName, filePath, encoding);
                TaskDialog.Show("tt", $"任务已完成，处理{lines}行数据，words_random_asc.csv保存到'我的文档'");
            }
            else
            {
                // 保存文件到下载目录
                string fileName = "words_reverse_asc.csv";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Console.WriteLine($"文档: {documentsPath}");
                string filePath = Path.Combine(documentsPath, fileName);
                //TaskDialog.Show("tt", $"检测到文件编码: {encoding.EncodingName}");
                //单列csv逆序重排
                //SortByReversedWords(openFileDialog.FileName, "words_reverse_asc.csv", encoding);
                //多列csv逆序重排
                int lines = SortByReversedWords(openFileDialog.FileName, filePath, encoding);
                TaskDialog.Show("tt", $"任务已完成，处理{lines}行数据，words_reverse_asc.csv保存到'我的文档'");
            }
            return Result.Succeeded;
        }
    }
}
