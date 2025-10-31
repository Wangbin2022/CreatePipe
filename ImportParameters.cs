using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    internal class ImportParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            try
            {
                // 选择CSV文件
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv",
                    Title = "选择要导入的CSV文件",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return Result.Failed;

                string filePath = openFileDialog.FileName;

                // 读取CSV文件
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    TaskDialog.Show("错误", "CSV文件格式不正确或为空"); return Result.Failed;
                }

                // 解析表头
                var headers = lines[0].Split(',').Select(h => h.Trim('\"').Trim()).ToList();
                if (!headers.Contains("ElementId"))
                {
                    TaskDialog.Show("错误", "CSV文件必须包含ElementId列"); return Result.Failed;
                }

                var elementIdIndex = headers.IndexOf("ElementId");
                var parameterNames = headers.Where(h => h != "ElementId").ToList();

                // 解析数据
                var updateData = new List<Dictionary<string, object>>();

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.Length <= elementIdIndex) continue;

                    if (!int.TryParse(values[elementIdIndex], out int elementId)) continue;

                    var rowData = new Dictionary<string, object>
                    {
                        ["ElementId"] = new ElementId(elementId)
                    };

                    for (int j = 0; j < headers.Count; j++)
                    {
                        if (j == elementIdIndex) continue;
                        if (j >= values.Length) break;

                        rowData[headers[j]] = values[j];
                    }

                    updateData.Add(rowData);
                }

                if (!updateData.Any())
                {
                    TaskDialog.Show("提示", "未找到有效数据");  return Result.Failed;
                }

                // 更新模型
                using (var transaction = new Transaction(doc, "批量更新构件参数"))
                {
                    transaction.Start();

                    int successCount = 0;
                    int totalUpdates = 0;
                    var results = new List<string>();

                    foreach (var data in updateData)
                    {
                        var elementId = (ElementId)data["ElementId"];
                        var element = doc.GetElement(elementId);

                        if (element == null)
                        {
                            results.Add($"ElementId {elementId.IntegerValue}: 构件不存在");
                            continue;
                        }

                        foreach (var paramName in parameterNames)
                        {
                            if (!data.ContainsKey(paramName)) continue;

                            var param = element.LookupParameter(paramName);
                            if (param == null)
                            {
                                results.Add($"ElementId {elementId.IntegerValue}: 参数 '{paramName}' 不存在");
                                continue;
                            }

                            if (param.IsReadOnly)
                            {
                                results.Add($"ElementId {elementId.IntegerValue}: 参数 '{paramName}' 为只读");
                                continue;
                            }

                            try
                            {
                                var value = data[paramName]?.ToString();
                                if (string.IsNullOrEmpty(value)) continue;

                                bool setSuccess = SetParameterValue(param, value);
                                if (setSuccess)
                                {
                                    totalUpdates++;
                                }
                                else
                                {
                                    results.Add($"ElementId {elementId.IntegerValue}: 参数 '{paramName}' 设置失败 - 值: {value}");
                                }
                            }
                            catch (Exception ex)
                            {
                                results.Add($"ElementId {elementId.IntegerValue}: 参数 '{paramName}' 错误 - {ex.Message}");
                            }
                        }
                        successCount++;
                    }

                    transaction.Commit();

                    // 显示结果
                    var resultMessage = new StringBuilder();
                    resultMessage.AppendLine($"导入完成:");
                    resultMessage.AppendLine($"成功处理: {successCount} 个构件");
                    resultMessage.AppendLine($"总更新: {totalUpdates} 个参数值");

                    if (results.Any())
                    {
                        resultMessage.AppendLine($"遇到 {results.Count} 个问题:");
                        foreach (var result in results.Take(10))
                        {
                            resultMessage.AppendLine($"  • {result}");
                        }
                        if (results.Count > 10)
                        {
                            resultMessage.AppendLine($"  ... 还有 {results.Count - 10} 个问题");
                        }
                    }

                    TaskDialog.Show("导入完成", resultMessage.ToString());
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("导入错误", $"导入过程中发生错误：\n{ex.Message}");
            }
            return Result.Succeeded;
        }
        // CSV行解析方法
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        // 参数值设置方法
        private bool SetParameterValue(Parameter param, string value)
        {
            try
            {
                switch (param.StorageType)
                {
                    case StorageType.Double:
                        if (double.TryParse(value, out double doubleValue))
                        {
                            // 如果是长度参数，从毫米转回英尺
                            if (param.Definition.ParameterType == ParameterType.Length)
                            {
                                doubleValue /= 304.8;
                            }
                            param.Set(doubleValue);
                            return true;
                        }
                        break;

                    case StorageType.Integer:
                        if (int.TryParse(value, out int intValue))
                        {
                            param.Set(intValue);
                            return true;
                        }
                        break;

                    case StorageType.String:
                        param.Set(value);
                        return true;

                    case StorageType.ElementId:
                        if (int.TryParse(value, out int elemIdValue))
                        {
                            param.Set(new ElementId(elemIdValue));
                            return true;
                        }
                        break;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
