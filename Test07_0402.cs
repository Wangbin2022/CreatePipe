using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test07_0402 : IExternalCommand
    {
        private static void JudgeConnection(Document doc, Element ele1st, Element ele2st)
        {
            try
            {
                //尝试连接几何
                JoinGeometryUtils.JoinGeometry(doc, ele1st, ele2st);
            }
            catch
            {
                Boolean ifJoined = JoinGeometryUtils.AreElementsJoined(doc, ele1st, ele2st);
                if (ifJoined)
                {
                    //判断连接关系是否正确，若不正确切换连接关系
                    Boolean if1stCut2st = JoinGeometryUtils.IsCuttingElementInJoin(doc, ele1st, ele2st);
                    if (if1stCut2st != true)
                    {
                        try
                        {
                            JoinGeometryUtils.SwitchJoinOrder(doc, ele2st, ele1st);
                        }
                        catch
                        {
                            //跳过----小帅帅呆了
                        }
                    }
                }
            }
        }
        private static List<Element> Get_Boundingbox_eles(Document doc, Element element, double offset)
        {
            //获取元素boundingbox相交的元素
            List<Element> eles_list = new List<Element>();
            XYZ element_max_boundingBox = element.get_BoundingBox(doc.ActiveView).Max;
            XYZ element_min_boundingBox = element.get_BoundingBox(doc.ActiveView).Min;
            Outline element_Outline = new Outline(element_min_boundingBox, element_max_boundingBox);
            //element_Outline.Scale(offset);
            FilteredElementCollector element_collector = new FilteredElementCollector(doc);
            ElementClassFilter elementClassFilter_beam = new ElementClassFilter(typeof(FamilyInstance));
            ElementClassFilter elementClassFilter_floor = new ElementClassFilter(typeof(Floor));
            LogicalOrFilter logicalOr = new LogicalOrFilter(elementClassFilter_beam, elementClassFilter_floor);
            element_collector.WherePasses(logicalOr).WherePasses(new BoundingBoxIntersectsFilter(element_Outline, offset));
            foreach (Element near_ele in element_collector)
            {
                eles_list.Add(near_ele as Element);
            }
            return eles_list;
        }
        static bool ViewHasTemplate(View v)
        {
            return !v.IsTemplate
              && (v.CanUseTemporaryVisibilityModes()
                || ((ViewType.Schedule == v.ViewType)
                  && !((ViewSchedule)v).IsTitleblockRevisionSchedule));
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();


            ////视图样板管理器
            //PropertiesForm propertiesForm = new PropertiesForm();
            //propertiesForm.ShowDialog();
            ////视图样式批量修改
            //ModifyViewDisplayForm modifyViewDisplayForm = new ModifyViewDisplayForm(uiApp);
            //modifyViewDisplayForm.Show();
            //0405 改当前视图细节属性.OK
            //ViewTemplateManagerView view = new ViewTemplateManagerView(uiApp);
            //view.ShowDialog();
            //doc.NewTransaction(() => activeView.DetailLevel = ViewDetailLevel.Fine, "修改名称");
            //activeView.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set("Coarse");
            //TaskDialog.Show("tt", activeView.Scale.ToString());
            //activeView.ViewType == ViewType.Schedule;
            //FilteredElementCollector list_viewTemplate = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.view).OfClass(typeof(FamilyInstance));
            //0404 视图样板相关
            //获取文档中的所有视图并计算
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //IEnumerable<View> views = collector.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>();
            //// 创建一个字典来存储模板的使用次数
            //Dictionary<ElementId, int> templateUsageCount = new Dictionary<ElementId, int>();
            //// 遍历所有视图
            //foreach (View item in views)
            //{
            //    if (item.ViewTemplateId.IntegerValue != -1)
            //    {
            //        ElementId templateId = item.ViewTemplateId;
            //        if (templateUsageCount.ContainsKey(templateId))
            //        {
            //            templateUsageCount[templateId]++;
            //        }
            //        else
            //        {
            //            templateUsageCount[templateId] = 1;
            //        }
            //    }
            //}
            //// 遍历字典，显示每个模板的使用情况
            //foreach (var keyValuePair in templateUsageCount)
            //{
            //    ElementId templateId = keyValuePair.Key;
            //    int usingCount = keyValuePair.Value;
            //    View targetView = doc.GetElement(templateId) as View;
            //    if (targetView != null)
            //    {
            //        string templateName = targetView.Name;
            //        string discipline = targetView.Discipline.ToString();
            //        TaskDialog.Show("模板使用情况", $"模板名称: {templateName}\n专业: {discipline}\n使用次数: {usingCount}次");
            //    }
            //}
            //例程结束
            ////0404 切换连接顺序抄网上代码，初步实现柱切板和梁，梁切板。
            //FilteredElementCollector list_column = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance));
            //FilteredElementCollector list_beam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance));
            ////TaskDialog.Show("tt", $"柱{list_column.Count().ToString()}个,梁{list_beam.Count().ToString()}个");
            //Transaction transaction = new Transaction(doc, "连接几何关系");
            //transaction.Start();
            //foreach (Element column in list_column)
            //{
            //    List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, 1.01);
            //    //TaskDialog.Show("柱子", column_box_eles.Count.ToString());
            //    foreach (Element ele in column_box_eles)
            //    {
            //        if (ele.Category.GetHashCode().ToString() == "-2001320" || ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, column, ele);
            //        }
            //    }
            //}
            //foreach (Element beam in list_beam)
            //{
            //    List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, 1.01);
            //    //TaskDialog.Show("梁", beam_box_eles.Count.ToString());
            //    foreach (Element ele in beam_box_eles)
            //    {
            //        //if (ele.Category.Name == "楼板")
            //        if (ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, beam, ele);
            //        }
            //    }
            //}
            //transaction.Commit();
            ////例程结束
            //0403 汇总昨天的，截断过长的csv
            //if (activeView.ViewType == ViewType.Schedule)
            //{
            //    ViewSchedule vsc = activeView as ViewSchedule;
            //    ViewScheduleExportOptions options = new ViewScheduleExportOptions();
            //    options.Title = false;
            //    options.ColumnHeaders = ExportColumnHeaders.MultipleRows;
            //    options.HeadersFootersBlanks = false;
            //    options.FieldDelimiter = ",";
            //    options.TextQualifier = ExportTextQualifier.None;
            //    vsc.Export("C:\\", "te.txt", options);
            //    string inputFilePath = "C:\\te.txt"; // 输入文件路径
            //    string outputFilePath = "D:\\outputFile.csv"; // 输出文件路径
            //    try
            //    {
            //        // 使用 UTF-16 编码读取文件内容
            //        string content = File.ReadAllText(inputFilePath, Encoding.Unicode);
            //        // 将内容以 UTF-8 编码写入到输出文件
            //        File.WriteAllText(outputFilePath, content, Encoding.UTF8);
            //        File.Delete(inputFilePath);
            //        string[] lines = File.ReadAllLines(outputFilePath);
            //        int maxLinesPerFile = 50;
            //        if (lines.Length > maxLinesPerFile)
            //        { 
            //            string header = lines[0];
            //            int totalColumns = lines[0].Split(',').Length;
            //            int fileCount = (int)Math.Ceiling((double)(lines.Length - 1) / maxLinesPerFile);
            //            for (int i = 0; i < fileCount; i++)
            //            {
            //                // 计算当前文件的起始行和结束行
            //                int startRow = 1 + i * maxLinesPerFile;
            //                int endRow = Math.Min(startRow + maxLinesPerFile - 1, lines.Length - 1);
            //                // 获取当前文件的内容
            //                string[] partLines = lines.Skip(startRow).Take(endRow - startRow + 1).ToArray();
            //                //// 检查包含额外逗号的异常行数
            //                int rowsWithExtraCommas = 0;
            //                List<int> rowsWithExtraCommasPositions = new List<int>();
            //                for (int j = 0; j < partLines.Length; j++)
            //                {
            //                    int columnCount = partLines[j].Split(',').Length;
            //                    if (columnCount != totalColumns)
            //                    {
            //                        rowsWithExtraCommas++;
            //                        rowsWithExtraCommasPositions.Add(j + 1); // 行号从 1 开始
            //                    }
            //                }
            //                // 将表头添加到当前文件的内容中
            //                partLines = new string[] { header }.Concat(partLines).ToArray();
            //                // 生成输出文件路径
            //                string outputFilePath2 = $"D:\\outputFile{i + 1}.csv";
            //                // 写入到输出文件
            //                File.WriteAllLines(outputFilePath2, partLines, Encoding.UTF8);
            //                if (rowsWithExtraCommasPositions.Count != 0)
            //                {
            //                    TaskDialog.Show("tt", $"{outputFilePath2}异常行位置: " + string.Join(", ", rowsWithExtraCommasPositions));
            //                }
            //            }
            //            File.Delete(outputFilePath);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}
            //else TaskDialog.Show("tt", "NO");
            //例程结束
            ////0402 读取csv并计算行列数，反馈问题行
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //fDialog.Filter = "csv 文件 (*.csv)|*.csv";
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    //FileInfo fileInfo = new FileInfo(fDialog.FileName);
            //    // 读取文件的所有行
            //    try
            //    {
            //        string[] lines = File.ReadAllLines(fDialog.FileName);
            //        // 计算总行数,列数（假设第一行是表头）
            //        int totalRows = lines.Length;
            //        int totalColumns = lines[0].Split(',').Length;
            //        // 检查包含额外逗号的异常行数
            //        int rowsWithExtraCommas = 0;
            //        List<int> rowsWithExtraCommasPositions = new List<int>();
            //        for (int i = 0; i < lines.Length; i++)
            //        {
            //            int columnCount = lines[i].Split(',').Length;
            //            if (columnCount != totalColumns)
            //            {
            //                rowsWithExtraCommas++;
            //                rowsWithExtraCommasPositions.Add(i + 1); // 行号从 1 开始
            //            }
            //        }
            //        TaskDialog.Show("tt", $"总行数: {totalRows}+总列数: {totalColumns}");
            //        if (rowsWithExtraCommasPositions.Count != 0)
            //        {
            //            TaskDialog.Show("tt", "异常行位置: " + string.Join(", ", rowsWithExtraCommasPositions));
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        TaskDialog.Show("tt", ex.Message.ToString());
            //    }
            //}
            ////例程结束
            //0402 找到明细表视图并导出为csv
            //    if (activeView.ViewType == ViewType.Schedule)
            //    {
            //        ViewSchedule vsc = activeView as ViewSchedule;
            //        ViewScheduleExportOptions options = new ViewScheduleExportOptions();
            //        options.Title = false;
            //        options.ColumnHeaders = ExportColumnHeaders.MultipleRows;
            //        options.HeadersFootersBlanks = false;
            //        options.FieldDelimiter = ",";
            //        options.TextQualifier = ExportTextQualifier.None;
            //        vsc.Export("C:\\", "te.txt", options);
            //        string inputFilePath = "C:\\te.txt"; // 输入文件路径
            //        string outputFilePath = "D:\\output_utf8.csv"; // 输出文件路径
            //        try
            //        {
            //            // 使用 UTF-16 编码读取文件内容
            //            string content = File.ReadAllText(inputFilePath, Encoding.Unicode);
            //            // 将内容以 UTF-8 编码写入到输出文件
            //            File.WriteAllText(outputFilePath, content, Encoding.UTF8);
            //            File.Delete(inputFilePath);
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine("发生错误: " + ex.Message);
            //        }
            //    }
            //    else TaskDialog.Show("tt", "NO");
            //    ////例程结束
            return Result.Succeeded;
        }
    }
}
