using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using View = Autodesk.Revit.DB.View;
namespace CreatePipe
{
    //[XmlType(TypeName = "TestRule")]
    //public class testXml
    //{
    //    [XmlAttribute]
    //    public string Height { get; set; }
    //    [XmlAttribute]
    //    public string Angle { get; set; }
    //}
    //[Transaction(TransactionMode.Manual)]
    //public class Test07_0402 : IExternalCommand
    //{
    //    private static void JudgeConnection(Document doc, Element ele1st, Element ele2st)
    //    {
    //        try
    //        {
    //            //尝试连接几何
    //            JoinGeometryUtils.JoinGeometry(doc, ele1st, ele2st);
    //        }
    //        catch
    //        {
    //            Boolean ifJoined = JoinGeometryUtils.AreElementsJoined(doc, ele1st, ele2st);
    //            if (ifJoined)
    //            {
    //                //判断连接关系是否正确，若不正确切换连接关系
    //                Boolean if1stCut2st = JoinGeometryUtils.IsCuttingElementInJoin(doc, ele1st, ele2st);
    //                if (if1stCut2st != true)
    //                {
    //                    try
    //                    {
    //                        JoinGeometryUtils.SwitchJoinOrder(doc, ele2st, ele1st);
    //                    }
    //                    catch
    //                    {
    //                        //跳过----小帅帅呆了
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    private static List<Element> Get_Boundingbox_eles(Document doc, Element element, double offset)
    //    {
    //        //获取元素boundingbox相交的元素
    //        List<Element> eles_list = new List<Element>();
    //        XYZ element_max_boundingBox = element.get_BoundingBox(doc.ActiveView).Max;
    //        XYZ element_min_boundingBox = element.get_BoundingBox(doc.ActiveView).Min;
    //        Outline element_Outline = new Outline(element_min_boundingBox, element_max_boundingBox);
    //        //element_Outline.Scale(offset);
    //        FilteredElementCollector element_collector = new FilteredElementCollector(doc);
    //        ElementClassFilter elementClassFilter_beam = new ElementClassFilter(typeof(FamilyInstance));
    //        ElementClassFilter elementClassFilter_floor = new ElementClassFilter(typeof(Floor));
    //        LogicalOrFilter logicalOr = new LogicalOrFilter(elementClassFilter_beam, elementClassFilter_floor);
    //        element_collector.WherePasses(logicalOr).WherePasses(new BoundingBoxIntersectsFilter(element_Outline, offset));
    //        foreach (Element near_ele in element_collector)
    //        {
    //            eles_list.Add(near_ele as Element);
    //        }
    //        return eles_list;
    //    }
    //    static bool ViewHasTemplate(View v)
    //    {
    //        return !v.IsTemplate
    //          && (v.CanUseTemporaryVisibilityModes()
    //            || ((ViewType.Schedule == v.ViewType)
    //              && !((ViewSchedule)v).IsTitleblockRevisionSchedule));
    //    }
    //    public string filePath = @"D:\temp\test2.xml";
    //    private double GetColumnWidthSum(int columnIndex, double baseCellWidth, double[] colWidthFactors)
    //    {
    //        double sum = 0;
    //        for (int i = 0; i < columnIndex; i++)
    //        {
    //            sum += baseCellWidth * colWidthFactors[i];
    //        }
    //        return sum;
    //    }
    //    public enum TextAlignment { Left, Center }
    //    private TextNote AddTextToCell(Document doc, View view, XYZ insertionPoint, string text, double cellWidth, TextAlignment alignment, ElementId textTypeId)
    //    {
    //        if (string.IsNullOrWhiteSpace(text))
    //        {
    //            text = "-"; // 默认显示内容
    //        }
    //        // 根据对齐方式调整插入点X坐标
    //        double offsetX = 0;
    //        switch (alignment)
    //        {
    //            case TextAlignment.Center:
    //                offsetX = cellWidth / 3;
    //                break;
    //            case TextAlignment.Left:
    //            default:
    //                offsetX = 1.5 / 304.8; // 留0.1英尺左边距
    //                break;
    //        }
    //        XYZ adjustedPoint = new XYZ(insertionPoint.X + offsetX, insertionPoint.Y, insertionPoint.Z);
    //        TextNoteOptions noteOptions = new TextNoteOptions();
    //        noteOptions.TypeId = textTypeId;
    //        //noteOptions.HorizontalAlignment = HorizontalTextAlignment.Right;
    //        noteOptions.VerticalAlignment = VerticalTextAlignment.Middle;
    //        TextNote textNote = TextNote.Create(doc, view.Id, adjustedPoint, text, noteOptions);
    //        // 设置对齐参数// 1=左, 2=中, 3=右
    //        textNote.get_Parameter(BuiltInParameter.TEXT_ALIGN_HORZ).Set((int)alignment);
    //        return textNote;
    //    }
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;
    //        XmlDoc.Instance.UIDoc = uiDoc;
    //        XmlDoc.Instance.Task = new RevitTask();

    //        //PropertiesForm propertiesForm =new PropertiesForm(uiDoc);
    //        //propertiesForm.ShowDialog();
    //        //TableTemplateView tableTemplate = new TableTemplateView(uiApp);
    //        //tableTemplate.Show();
    //        //0410 csv画表测试
    //        //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
    //        //fDialog.Filter = "csv 文件 (*.csv)|*.csv";
    //        //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    //        //{
    //        //    string[] lines = File.ReadAllLines(fDialog.FileName);
    //        //    int rowCount = lines.Length;
    //        //    int columnCount = lines[0].Split(',').Length;
    //        //    using (Transaction tx = new Transaction(doc))
    //        //    {
    //        //        tx.Start("绘制表格");
    //        //        //行>60,列超出12要提示退出
    //        //        activeView.Scale = 50;
    //        //        double rowHeight = 5 / 304.8 * activeView.Scale; // 行高（单位：mm）
    //        //        double baseCellWidth = 10 / 304.8 * activeView.Scale; // 基准列宽（单位：mm）
    //        //        double[] colWidthFactors = new double[12];
    //        //        if (columnCount <= 12)
    //        //        {
    //        //            colWidthFactors[0] = 3;
    //        //            colWidthFactors[1] = 1.5;
    //        //            colWidthFactors[2] = 2;
    //        //            colWidthFactors[3] = 2.4;
    //        //            colWidthFactors[4] = 3;
    //        //            colWidthFactors[5] = 2;
    //        //            colWidthFactors[6] = 1;
    //        //            colWidthFactors[7] = 1;
    //        //            colWidthFactors[8] = 1;
    //        //            colWidthFactors[9] = 1;
    //        //            colWidthFactors[10] = 1;
    //        //            colWidthFactors[11] = 1;
    //        //        }
    //        //        else TaskDialog.Show("tt", "csv列数太多请清理后添加");
    //        //        //double[] colWidthFactors = Enumerable.Repeat(1.0, columnCount).ToArray();
    //        //        //double[] colWidthFactors = { 1.0, 1.5, 1, 1, 1 }; // 列宽系数 
    //        //        string wideLineStyleName = "H-CONA";
    //        //        //应该ComboBox选一种线样式
    //        //        //string wideLineStyleName = "<Sketch>";
    //        //        List<TextNoteType> refs = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
    //        //        string tableFont = "3_宋体_0.7";
    //        //        string[] fontAttr = tableFont.Split('_').ToArray();
    //        //        double sizeFactor;
    //        //        double.TryParse(fontAttr[0].ToString(), out sizeFactor);
    //        //        //string tableFont2 = "3_仿宋_0.7";
    //        //        //string tableFont3 = "3_宋体_0.7";
    //        //        bool found = refs.Any(item => item.Name == tableFont);
    //        //        //当无同名时新建
    //        //        TextNoteType textType = found
    //        //            ? refs.FirstOrDefault(item => item.Name == tableFont)
    //        //            : refs.FirstOrDefault()?.Duplicate(tableFont) as TextNoteType;
    //        //        if (textType != null)
    //        //        {
    //        //            textType.get_Parameter(BuiltInParameter.TEXT_FONT).Set(fontAttr[1]);
    //        //            textType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(sizeFactor / 304.8);
    //        //            textType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE).Set(0.7);
    //        //        }
    //        //        // 2. 初始化数据和对齐数组
    //        //        string[,] tableData = new string[rowCount, columnCount];
    //        //        TextAlignment[] columnAlignments = new TextAlignment[columnCount];
    //        //        // 3. 解析每行数据
    //        //        for (int i = 0; i < rowCount; i++)
    //        //        {
    //        //            string[] cells = lines[i].Split(',');
    //        //            for (int j = 0; j < columnCount; j++)
    //        //            {
    //        //                string cellContent = cells[j].Trim();
    //        //                TextAlignment alignment = TextAlignment.Center;
    //        //                if (!true)
    //        //                {
    //        //                    alignment = TextAlignment.Left;
    //        //                }
    //        //                tableData[i, j] = cellContent;
    //        //                // 第一行决定列对齐方式（可根据需要改为其他逻辑）
    //        //                if (i == 0) columnAlignments[j] = alignment;
    //        //            }
    //        //        }
    //        //        // 绘制表格
    //        //        XYZ origin = uiDoc.Selection.PickPoint("请选择表格左上角点");
    //        //        //// 收集所有线条一次性创建
    //        //        //List<Curve> allCurves = new List<Curve>();
    //        //        //double t1 = 0;
    //        //        //for (int i = 0; i < columnCount; i++) t1 += baseCellWidth * colWidthFactors[i];
    //        //        //double totalWidth = t1;
    //        //        //// 内部网格线
    //        //        //for (int i = 0; i <= rowCount; i++)
    //        //        //{
    //        //        //    double y = origin.Y - i * rowHeight;
    //        //        //    allCurves.Add(Line.CreateBound(
    //        //        //        new XYZ(origin.X, y, origin.Z),
    //        //        //        new XYZ(origin.X + totalWidth, y, origin.Z)));
    //        //        //}
    //        //        //for (int j = 0; j <= columnCount; j++)
    //        //        //{
    //        //        //    double x = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
    //        //        //    allCurves.Add(Line.CreateBound(
    //        //        //        new XYZ(x, origin.Y, origin.Z),
    //        //        //        new XYZ(x, origin.Y - rowCount * rowHeight, origin.Z)));
    //        //        //}
    //        //        //// 批量创建（效率提升关键）
    //        //        //foreach (var item in allCurves)
    //        //        //{
    //        //        //    doc.Create.NewDetailCurve(activeView, item);
    //        //        //}
    //        //        // 创建所有单元格边界(有重复)
    //        //        for (int i = 0; i < rowCount; i++)
    //        //        {
    //        //            for (int j = 0; j < columnCount; j++)
    //        //            {
    //        //                // 计算当前单元格的起始点
    //        //                double cellX = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
    //        //                double cellY = origin.Y - i * rowHeight;
    //        //                // 绘制当前单元格的四条边
    //        //                XYZ startTop = new XYZ(cellX, cellY, origin.Z);
    //        //                XYZ endTop = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY, origin.Z);
    //        //                Line lineTop = Line.CreateBound(startTop, endTop);
    //        //                doc.Create.NewDetailCurve(activeView, lineTop);
    //        //                XYZ startRight = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY, origin.Z);
    //        //                XYZ endRight = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY - rowHeight, origin.Z);
    //        //                Line lineRight = Line.CreateBound(startRight, endRight);
    //        //                doc.Create.NewDetailCurve(activeView, lineRight);
    //        //                XYZ startBottom = new XYZ(cellX, cellY - rowHeight, origin.Z);
    //        //                XYZ endBottom = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY - rowHeight, origin.Z);
    //        //                Line lineBottom = Line.CreateBound(startBottom, endBottom);
    //        //                doc.Create.NewDetailCurve(activeView, lineBottom);
    //        //                XYZ startLeft = new XYZ(cellX, cellY, origin.Z);
    //        //                XYZ endLeft = new XYZ(cellX, cellY - rowHeight, origin.Z);
    //        //                Line lineLeft = Line.CreateBound(startLeft, endLeft);
    //        //                doc.Create.NewDetailCurve(activeView, lineLeft);
    //        //            }
    //        //        }
    //        //        //外框线绘制
    //        //        double total = 0;
    //        //        for (int i = 0; i < columnCount; i++) total += baseCellWidth * colWidthFactors[i];
    //        //        double totalWidth = total;
    //        //        double totalHeight = rowCount * rowHeight;
    //        //        Line topLine = Line.CreateBound(origin, new XYZ(origin.X + totalWidth, origin.Y, origin.Z));
    //        //        Line rightLine = Line.CreateBound(new XYZ(origin.X + totalWidth, origin.Y, origin.Z), new XYZ(origin.X + totalWidth, origin.Y - totalHeight, origin.Z));
    //        //        Line bottomLine = Line.CreateBound(new XYZ(origin.X, origin.Y - totalHeight, origin.Z), new XYZ(origin.X + totalWidth, origin.Y - totalHeight, origin.Z));
    //        //        Line leftLine = Line.CreateBound(new XYZ(origin.X, origin.Y, origin.Z), new XYZ(origin.X, origin.Y - totalHeight, origin.Z));
    //        //        DetailLine topBorder = doc.Create.NewDetailCurve(activeView, topLine) as DetailLine;
    //        //        DetailLine rightBorder = doc.Create.NewDetailCurve(activeView, rightLine) as DetailLine;
    //        //        DetailLine bottomBorder = doc.Create.NewDetailCurve(activeView, bottomLine) as DetailLine;
    //        //        DetailLine leftBorder = doc.Create.NewDetailCurve(activeView, leftLine) as DetailLine;
    //        //        //加粗外框线
    //        //        CategoryNameMap subcats = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).SubCategories;
    //        //        Category setstyle = subcats.Cast<Category>().FirstOrDefault(c => c.Name == wideLineStyleName);
    //        //        topBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
    //        //        rightBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
    //        //        bottomBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
    //        //        leftBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
    //        //        //表内加字
    //        //        for (int i = 0; i < rowCount; i++)
    //        //        {
    //        //            for (int j = 0; j < columnCount; j++)
    //        //            {
    //        //                double cellX = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
    //        //                double cellY = origin.Y - i * rowHeight;
    //        //                XYZ cellOrigin = new XYZ(cellX, cellY - rowHeight / 2, origin.Z); // 垂直居中
    //        //                AddTextToCell(doc, activeView, cellOrigin, tableData[i, j], baseCellWidth * colWidthFactors[j], columnAlignments[j], textType.Id);
    //        //                //AddTextToCell(doc, activeView, cellOrigin, tableData[i, j], baseCellWidth * colWidthFactors[j], TextAlignment.Center, textType.Id);
    //        //            }
    //        //        }
    //        //        tx.Commit();
    //        //    }
    //        //}
    //        //List<DetailLine> horizontalLines = new List<DetailLine>();
    //        //double currentY = origin.Y;
    //        //for (int i = 0; i <= rowCount; i++)
    //        //{
    //        //    XYZ start = new XYZ(origin.X, currentY, origin.Z);
    //        //    XYZ end = new XYZ(origin.X + GetTotalWidth(columnCount, baseCellWidth, colWidthFactors), currentY, origin.Z);
    //        //    Line line = Line.CreateBound(start, end);
    //        //    DetailLine detailLine = doc.Create.NewDetailCurve(activeView, line) as DetailLine;
    //        //    horizontalLines.Add(detailLine);
    //        //    if (i < rowCount)
    //        //    {
    //        //        currentY -= rowHeight;
    //        //    }
    //        //}
    //        //// 创建通长垂直线
    //        //List<DetailLine> verticalLines = new List<DetailLine>();
    //        //double currentX = origin.X;
    //        //for (int j = 0; j <= columnCount; j++)
    //        //{
    //        //    XYZ start = new XYZ(currentX, origin.Y, origin.Z);
    //        //    XYZ end = new XYZ(currentX, origin.Y - GetTotalHeight(rowCount, rowHeight), origin.Z);
    //        //    Line line = Line.CreateBound(start, end);
    //        //    DetailLine detailLine = doc.Create.NewDetailCurve(activeView, line) as DetailLine;
    //        //    verticalLines.Add(detailLine);
    //        //    if (j < columnCount)
    //        //    {
    //        //        currentX += baseCellWidth * colWidthFactors[j];
    //        //    }
    //        //}
    //        //// 组合所有线为一个组，方便整体操作
    //        //Group tableGroup = doc.Create.NewGroup(horizontalLines.Select(l => l.Id)
    //        //    .Union(verticalLines.Select(l => l.Id)).ToList());
    //        //tableGroup.GroupType.Name = "表格_" + rowCount + "x" + columnCount;

    //        ////0409 字体设置OK
    //        //List<TextNoteType> refs = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
    //        //doc.NewTransaction(() =>
    //        //{
    //        //    string tableFont = "3_仿宋_0.7";
    //        //    string[] fontAttr= tableFont.Split('_').ToArray(); 
    //        //    double sizeFactor;
    //        //    double.TryParse(fontAttr[0].ToString(), out sizeFactor);
    //        //    //string tableFont2 = "3_仿宋_0.7";
    //        //    //string tableFont3 = "3_宋体_0.7";
    //        //    bool found = refs.Any(item => item.Name == tableFont);
    //        //    //当无同名时新建
    //        //    TextNoteType targetType = found
    //        //        ? refs.FirstOrDefault(item => item.Name == tableFont)
    //        //        : refs.FirstOrDefault()?.Duplicate(tableFont) as TextNoteType;
    //        //    if (targetType != null)
    //        //    {
    //        //        targetType.get_Parameter(BuiltInParameter.TEXT_FONT).Set(fontAttr[1]);
    //        //        targetType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(sizeFactor / 304.8);
    //        //        targetType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE).Set(0.7);
    //        //    }
    //        //}, "新建文字样式");
    //        ////例程结束
    //        ////取默认ElementId方法不错
    //        // doc.NewTransaction(() =>
    //        //{
    //        //    ElementId TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
    //        //    XYZ pt1 = uiDoc.Selection.PickPoint("请选择表格左上角点");
    //        //    TextNote textNote = TextNote.Create(doc, activeView.Id, pt1, "R1+C1", // 示例文字：R1C1表示第1行第1列
    //        //        new TextNoteOptions
    //        //        {
    //        //            HorizontalAlignment = HorizontalTextAlignment.Center,
    //        //            VerticalAlignment = VerticalTextAlignment.Middle,
    //        //            TypeId = TypeId // 获取默认文字类型
    //        //        });
    //        //}, "新建文字");
    //        ////例程结束
    //        ////0409 改详图视图名称和比例
    //        //if (activeView.ViewType == ViewType.Legend || activeView.ViewType == ViewType.DraftingView)
    //        //{
    //        //    //doc.NewTransaction(() => activeView.DetailLevel = ViewDetailLevel.Fine, "修改名称");
    //        //    doc.NewTransaction(() =>
    //        //    {
    //        //        activeView.Scale = 20;
    //        //        activeView.Name = "新详图11";
    //        //    }, "改视图属性");
    //        //}
    //        //例程结束
    //        ////0409 正确XML修改方法
    //        //int i = 0;
    //        //TableCollection tableCollection = XMLUtil.DeserializeFromXml<TableCollection>(filePath);
    //        //List<TableSingle> tss = tableCollection.tableSingles;
    //        //// 使用 RemoveAll 方法移除所有 tableName 为 "11" 的元素
    //        //i = tss.RemoveAll(ts => ts.tableName == "11");
    //        //XMLUtil.SerializeToXml(filePath, tableCollection);
    //        //TaskDialog.Show("tt", $"已删除表格样式{i}个");
    //        ////下面方法不可用，直接在遍历 List<TableSingle> 的过程中调用 Remove 方法会导致集合在迭代过程中被修改，这可能会引发异常。
    //        ////foreach (TableSingle ts in tss)
    //        ////{
    //        ////    if (ts.tableName == "11")
    //        ////    {
    //        ////        tss.Remove(ts);
    //        ////        i++;
    //        ////    }
    //        ////}
    //        ////例程结束
    //        //0407 Xml试验
    //        //TaskDialog.Show("tt", PathUtil.GetCurrentDllPathDirectory());
    //        //string directoryPath = @"D:\temp";
    //        //string filePath = Path.Combine(directoryPath, "test.xml");
    //        //try
    //        //{
    //        //    // 检查目录是否存在，如果不存在则创建
    //        //    if (!Directory.Exists(directoryPath))
    //        //    {
    //        //        Directory.CreateDirectory(directoryPath);
    //        //        TaskDialog.Show("tt", $"目录 {directoryPath} 已创建。");
    //        //    }
    //        //    ////保存Xml
    //        //    //testXml test = new testXml();
    //        //    //test.Height = "1000";
    //        //    //test.Angle = "45";
    //        //    ////调用工具类输出xml
    //        //    //XMLUtil.SerializeToXml(@"D:\temp\test.xml", test);
    //        //    //读取Xml
    //        //    testXml test = XMLUtil.DeserializeFromXml<testXml>(@"D:\temp\test.xml");
    //        //    TaskDialog.Show("tt", $"{test.Angle}PASS{test.Height}");
    //        //    //// 创建 XML 文件
    //        //    //XmlDocument xmlDoc = new XmlDocument();
    //        //    //XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
    //        //    //xmlDoc.AppendChild(xmlDeclaration);
    //        //    //// 创建根节点
    //        //    //XmlElement root = xmlDoc.CreateElement("Root");
    //        //    //xmlDoc.AppendChild(root);
    //        //    //// 添加子节点
    //        //    //XmlElement child = xmlDoc.CreateElement("Child");
    //        //    //child.InnerText = "This is a test.";
    //        //    //root.AppendChild(child);
    //        //    //// 保存 XML 文件
    //        //    //xmlDoc.Save(filePath);
    //        //    //TaskDialog.Show("tt", $"文件 {filePath} 已创建。");
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("tt", $"发生错误: {ex.Message}");
    //        //}
    //        //0403 汇总昨天的，截断过长的csv
    //        //if (activeView.ViewType == ViewType.Schedule)
    //        //{
    //        //    ViewSchedule vsc = activeView as ViewSchedule;
    //        //    ViewScheduleExportOptions options = new ViewScheduleExportOptions();
    //        //    options.Title = false;
    //        //    options.ColumnHeaders = ExportColumnHeaders.MultipleRows;
    //        //    options.HeadersFootersBlanks = false;
    //        //    options.FieldDelimiter = ",";
    //        //    options.TextQualifier = ExportTextQualifier.None;
    //        //    vsc.Export("C:\\", "te.txt", options);
    //        //    string inputFilePath = "C:\\te.txt"; // 输入文件路径
    //        //    string outputFilePath = "D:\\outputFile.csv"; // 输出文件路径
    //        //    try
    //        //    {
    //        //        // 使用 UTF-16 编码读取文件内容
    //        //        string content = File.ReadAllText(inputFilePath, Encoding.Unicode);
    //        //        // 将内容以 UTF-8 编码写入到输出文件
    //        //        File.WriteAllText(outputFilePath, content, Encoding.UTF8);
    //        //        File.Delete(inputFilePath);
    //        //        string[] lines = File.ReadAllLines(outputFilePath);
    //        //        int maxLinesPerFile = 50;
    //        //        if (lines.Length > maxLinesPerFile)
    //        //        { 
    //        //            string header = lines[0];
    //        //            int totalColumns = lines[0].Split(',').Length;
    //        //            int fileCount = (int)Math.Ceiling((double)(lines.Length - 1) / maxLinesPerFile);
    //        //            for (int i = 0; i < fileCount; i++)
    //        //            {
    //        //                // 计算当前文件的起始行和结束行
    //        //                int startRow = 1 + i * maxLinesPerFile;
    //        //                int endRow = Math.Min(startRow + maxLinesPerFile - 1, lines.Length - 1);
    //        //                // 获取当前文件的内容
    //        //                string[] partLines = lines.Skip(startRow).Take(endRow - startRow + 1).ToArray();
    //        //                //// 检查包含额外逗号的异常行数
    //        //                int rowsWithExtraCommas = 0;
    //        //                List<int> rowsWithExtraCommasPositions = new List<int>();
    //        //                for (int j = 0; j < partLines.Length; j++)
    //        //                {
    //        //                    int columnCount = partLines[j].Split(',').Length;
    //        //                    if (columnCount != totalColumns)
    //        //                    {
    //        //                        rowsWithExtraCommas++;
    //        //                        rowsWithExtraCommasPositions.Add(j + 1); // 行号从 1 开始
    //        //                    }
    //        //                }
    //        //                // 将表头添加到当前文件的内容中
    //        //                partLines = new string[] { header }.Concat(partLines).ToArray();
    //        //                // 生成输出文件路径
    //        //                string outputFilePath2 = $"D:\\outputFile{i + 1}.csv";
    //        //                // 写入到输出文件
    //        //                File.WriteAllLines(outputFilePath2, partLines, Encoding.UTF8);
    //        //                if (rowsWithExtraCommasPositions.Count != 0)
    //        //                {
    //        //                    TaskDialog.Show("tt", $"{outputFilePath2}异常行位置: " + string.Join(", ", rowsWithExtraCommasPositions));
    //        //                }
    //        //            }
    //        //            File.Delete(outputFilePath);
    //        //        }
    //        //    }
    //        //    catch (Exception ex)
    //        //    {

    //        //    }
    //        //}
    //        //else TaskDialog.Show("tt", "NO");
    //        //例程结束
    //        ////视图样式批量修改
    //        //ModifyViewDisplayForm modifyViewDisplayForm = new ModifyViewDisplayForm(uiApp);
    //        //modifyViewDisplayForm.Show();
    //        //0405 改当前视图细节属性.OK
    //        //ViewTemplateManagerView view = new ViewTemplateManagerView(uiApp);
    //        //view.ShowDialog();
    //        //doc.NewTransaction(() => activeView.DetailLevel = ViewDetailLevel.Fine, "修改名称");
    //        //activeView.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set("Coarse");
    //        //TaskDialog.Show("tt", activeView.Scale.ToString());
    //        //activeView.ViewType == ViewType.Schedule;
    //        //FilteredElementCollector list_viewTemplate = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.view).OfClass(typeof(FamilyInstance));
    //        //0404 视图样板相关
    //        //获取文档中的所有视图并计算
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //IEnumerable<View> views = collector.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>();
    //        //// 创建一个字典来存储模板的使用次数
    //        //Dictionary<ElementId, int> templateUsageCount = new Dictionary<ElementId, int>();
    //        //// 遍历所有视图
    //        //foreach (View item in views)
    //        //{
    //        //    if (item.ViewTemplateId.IntegerValue != -1)
    //        //    {
    //        //        ElementId templateId = item.ViewTemplateId;
    //        //        if (templateUsageCount.ContainsKey(templateId))
    //        //        {
    //        //            templateUsageCount[templateId]++;
    //        //        }
    //        //        else
    //        //        {
    //        //            templateUsageCount[templateId] = 1;
    //        //        }
    //        //    }
    //        //}
    //        //// 遍历字典，显示每个模板的使用情况
    //        //foreach (var keyValuePair in templateUsageCount)
    //        //{
    //        //    ElementId templateId = keyValuePair.Key;
    //        //    int usingCount = keyValuePair.Value;
    //        //    View targetView = doc.GetElement(templateId) as View;
    //        //    if (targetView != null)
    //        //    {
    //        //        string templateName = targetView.Name;
    //        //        string discipline = targetView.Discipline.ToString();
    //        //        TaskDialog.Show("模板使用情况", $"模板名称: {templateName}\n专业: {discipline}\n使用次数: {usingCount}次");
    //        //    }
    //        //}
    //        //例程结束

    //        ////0404 切换连接顺序抄网上代码，初步实现柱切板和梁，梁切板。
    //        //FilteredElementCollector list_column = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance));
    //        //FilteredElementCollector list_beam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance));
    //        ////TaskDialog.Show("tt", $"柱{list_column.Count().ToString()}个,梁{list_beam.Count().ToString()}个");
    //        //Transaction transaction = new Transaction(doc, "连接几何关系");
    //        //transaction.Start();
    //        //foreach (Element column in list_column)
    //        //{
    //        //    List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, 1.01);
    //        //    //TaskDialog.Show("柱子", column_box_eles.Count.ToString());
    //        //    foreach (Element ele in column_box_eles)
    //        //    {
    //        //        if (ele.Category.GetHashCode().ToString() == "-2001320" || ele.Category.GetHashCode().ToString() == "-2000032")
    //        //        {
    //        //            JudgeConnection(doc, column, ele);
    //        //        }
    //        //    }
    //        //}
    //        //foreach (Element beam in list_beam)
    //        //{
    //        //    List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, 1.01);
    //        //    //TaskDialog.Show("梁", beam_box_eles.Count.ToString());
    //        //    foreach (Element ele in beam_box_eles)
    //        //    {
    //        //        //if (ele.Category.Name == "楼板")
    //        //        if (ele.Category.GetHashCode().ToString() == "-2000032")
    //        //        {
    //        //            JudgeConnection(doc, beam, ele);
    //        //        }
    //        //    }
    //        //}
    //        //transaction.Commit();
    //        ////例程结束
    //        ////0402 读取csv并计算行列数，反馈问题行
    //        //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
    //        //fDialog.Filter = "csv 文件 (*.csv)|*.csv";
    //        //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    //        //{
    //        //    //FileInfo fileInfo = new FileInfo(fDialog.FileName);
    //        //    // 读取文件的所有行
    //        //    try
    //        //    {
    //        //        string[] lines = File.ReadAllLines(fDialog.FileName);
    //        //        // 计算总行数,列数（假设第一行是表头）
    //        //        int totalRows = lines.Length;
    //        //        int totalColumns = lines[0].Split(',').Length;
    //        //        // 检查包含额外逗号的异常行数
    //        //        int rowsWithExtraCommas = 0;
    //        //        List<int> rowsWithExtraCommasPositions = new List<int>();
    //        //        for (int i = 0; i < lines.Length; i++)
    //        //        {
    //        //            int columnCount = lines[i].Split(',').Length;
    //        //            if (columnCount != totalColumns)
    //        //            {
    //        //                rowsWithExtraCommas++;
    //        //                rowsWithExtraCommasPositions.Add(i + 1); // 行号从 1 开始
    //        //            }
    //        //        }
    //        //        TaskDialog.Show("tt", $"总行数: {totalRows}+总列数: {totalColumns}");
    //        //        if (rowsWithExtraCommasPositions.Count != 0)
    //        //        {
    //        //            TaskDialog.Show("tt", "异常行位置: " + string.Join(", ", rowsWithExtraCommasPositions));
    //        //        }
    //        //    }
    //        //    catch (Exception ex)
    //        //    {
    //        //        TaskDialog.Show("tt", ex.Message.ToString());
    //        //    }
    //        //}
    //        ////例程结束
    //        //0402 找到明细表视图并导出为csv
    //        //    if (activeView.ViewType == ViewType.Schedule)
    //        //    {
    //        //        ViewSchedule vsc = activeView as ViewSchedule;
    //        //        ViewScheduleExportOptions options = new ViewScheduleExportOptions();
    //        //        options.Title = false;
    //        //        options.ColumnHeaders = ExportColumnHeaders.MultipleRows;
    //        //        options.HeadersFootersBlanks = false;
    //        //        options.FieldDelimiter = ",";
    //        //        options.TextQualifier = ExportTextQualifier.None;
    //        //        vsc.Export("C:\\", "te.txt", options);
    //        //        string inputFilePath = "C:\\te.txt"; // 输入文件路径
    //        //        string outputFilePath = "D:\\output_utf8.csv"; // 输出文件路径
    //        //        try
    //        //        {
    //        //            // 使用 UTF-16 编码读取文件内容
    //        //            string content = File.ReadAllText(inputFilePath, Encoding.Unicode);
    //        //            // 将内容以 UTF-8 编码写入到输出文件
    //        //            File.WriteAllText(outputFilePath, content, Encoding.UTF8);
    //        //            File.Delete(inputFilePath);
    //        //        }
    //        //        catch (Exception ex)
    //        //        {
    //        //            Console.WriteLine("发生错误: " + ex.Message);
    //        //        }
    //        //    }
    //        //    else TaskDialog.Show("tt", "NO");
    //        //    ////例程结束
    //        return Result.Succeeded;
    //    }
    //}
}
