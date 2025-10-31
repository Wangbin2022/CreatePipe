using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    internal class ExportParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            try
            {
                var selectItem = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass()).ElementId) as FamilyInstance;
                var instances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                    .Where(fi => fi.Symbol.Id == selectItem.Symbol.Id).ToList();

                // 选择多个参数
                var parameterNames = new List<string>();
                UniversalMultiParameterSelect paramSelector = new UniversalMultiParameterSelect(selectItem, "选择要导出的参数");
                if (paramSelector.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }
                parameterNames = paramSelector.SelectedParameters;

                if (!parameterNames.Any())
                {
                    TaskDialog.Show("提示", "未选择任何参数");
                    return Result.Cancelled;
                }

                // 收集数据
                var parameterData = new List<Dictionary<string, object>>();

                foreach (var instance in instances)
                {
                    var rowData = new Dictionary<string, object>
                    {
                        ["ElementId"] = instance.Id.IntegerValue
                    };

                    foreach (var paramName in parameterNames)
                    {
                        var param = instance.LookupParameter(paramName);
                        if (param?.HasValue == true)
                        {
                            object value = null;
                            switch (param.StorageType)
                            {
                                case StorageType.Double:
                                    double doubleValue = param.AsDouble();
                                    // 如果是长度类型参数，转换为毫米
                                    if (param.Definition.ParameterType == ParameterType.Length)
                                    {
                                        value = doubleValue * 304.8;
                                    }
                                    else
                                    {
                                        value = doubleValue;
                                    }
                                    break;
                                case StorageType.Integer:
                                    value = param.AsInteger();
                                    break;
                                case StorageType.String:
                                    value = param.AsString();
                                    break;
                                case StorageType.ElementId:
                                    var elemId = param.AsElementId();
                                    value = elemId.IntegerValue;
                                    break;
                            }
                            rowData[paramName] = value;
                        }
                        else
                        {
                            rowData[paramName] = "";
                        }
                    }
                    parameterData.Add(rowData);
                }

                // 生成CSV文件
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"模型属性传递_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = Path.Combine(desktopPath, fileName);

                var csvContent = new StringBuilder();

                // 写入表头
                var headers = new List<string> { "ElementId" };
                headers.AddRange(parameterNames);
                csvContent.AppendLine(string.Join(",", headers));

                // 写入数据
                foreach (var row in parameterData)
                {
                    var values = new List<string>();
                    foreach (var header in headers)
                    {
                        var value = row.ContainsKey(header) ? row[header]?.ToString() ?? "" : "";
                        // 处理包含逗号的情况
                        if (value.Contains(","))
                            value = $"\"{value}\"";
                        values.Add(value);
                    }
                    csvContent.AppendLine(string.Join(",", values));
                }

                File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
                TaskDialog.Show("导出成功",
                    $"共导出 {parameterData.Count} 个构件的 {parameterNames.Count} 个参数\n文件路径：{filePath}");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户取消选择，正常退出
            }
            catch (Exception ex)
            {
                TaskDialog.Show("导出错误", $"导出过程中发生错误：\n{ex.Message}");
            }

            return Result.Succeeded;
        }
    }
}
