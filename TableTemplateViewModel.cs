using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.utils;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CreatePipe
{
    public class ColumnProperties
    {
        public string Title { get; set; }
        public double Width { get; set; } = 1.0; // 默认值为 1
        public string Alignment { get; set; } = "居中";
        //public int RowCount { get; set; } = 1; // 默认值为 1
    }
    public class TableTemplateViewModel : ObserverableObject
    {
        Document doc;
        Autodesk.Revit.DB.View activeView;
        UIDocument uiDoc;
        private ObservableCollection<ColumnProperties> columnPropertiesList = new ObservableCollection<ColumnProperties>();
        public ObservableCollection<ColumnProperties> ColumnPropertiesList
        {
            get => columnPropertiesList;
            set
            {
                columnPropertiesList = value;
                OnPropertyChanged(nameof(columnPropertiesList));
            }
        }
        public string filePath = @"D:\temp\test.xml";
        public TableTemplateViewModel(UIApplication application)
        {
            doc = application.ActiveUIDocument.Document;
            activeView = application.ActiveUIDocument.ActiveView;
            uiDoc = application.ActiveUIDocument;
            CategoryNameMap subcats = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).SubCategories;
            foreach (Category item in subcats)
            {
                LineStyles.Add(item);
            }
        }
        //public ICommand TestCommand => new BaseBindingCommand(Test);
        //private void Test(object obj)
        //{
        //    if (SelectedTableSingle != null)
        //    {
        //        //TaskDialog.Show("tt", SelectedTableSingle.tableEntities[1].entityWidth.ToString());
        //        //TaskDialog.Show("tt", ColumnPropertiesList.Count().ToString());
        //        TaskDialog.Show("tt", ColumnPropertiesList[0].Width.ToString());
        //    }
        //}
        public Category BorderStyle { get; set; }
        public List<Category> LineStyles { get; set; } = new List<Category>();
        public string[] csvContents { get; set; }
        //string[] lines = File.ReadAllLines(fDialog.FileName);
        public ICommand DrawTableCommand => new BaseBindingCommand(DrawTable);
        private void DrawTable(object obj)
        {
            if (ColumnPropertiesList.Count() > 0)
            {
                int rowCount = lines.Length;
                int columnCount = lines[0].Split(',').Length;
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("绘制表格");
                    //行>60,列超出12要提示退出
                    activeView.Scale = TableScale;
                    //double rowHeight = TextSize * (5 / 3) / 304.8 * activeView.Scale; // 行高（单位：mm）
                    double rowHeight = TextSize * 1.5 / 304.8 * activeView.Scale; // 行高（单位：mm）
                    double baseCellWidth = BaseWidth / 304.8 * activeView.Scale; // 基准列宽（单位：mm）
                    double[] colWidthFactors = new double[12];
                    if (columnCount <= 12)
                    {
                        colWidthFactors[0] = 3;
                        colWidthFactors[1] = 1.5;
                        colWidthFactors[2] = 2;
                        colWidthFactors[3] = 2.4;
                        colWidthFactors[4] = 3;
                        colWidthFactors[5] = 2;
                        colWidthFactors[6] = 1;
                        colWidthFactors[7] = 1;
                        colWidthFactors[8] = 1;
                        colWidthFactors[9] = 1;
                        colWidthFactors[10] = 1;
                        colWidthFactors[11] = 1;
                    }
                    else TaskDialog.Show("tt", "csv列数太多请清理后添加");
                    //要读取xml并替换WidthFactor内列宽度
                    //if (SelectedTableSingle != null && SelectedTableSingle.tableEntities != null)
                    //{
                    //    for (int i = 0; i < SelectedTableSingle.tableEntities.Count; i++)
                    //    {
                    //        colWidthFactors[i] = SelectedTableSingle.tableEntities[i].entityWidth;
                    //    }
                    //}
                    if (ColumnPropertiesList.Count() < 13)
                    {
                        for (int i = 0; i < ColumnPropertiesList.Count(); i++)
                        {
                            colWidthFactors[i] = ColumnPropertiesList[i].Width;
                        }
                    }
                    //确定外框线样式
                    //string wideLineStyleName = "H-CONA";
                    string wideLineStyleName;
                    if (BorderStyle != null)
                    {
                        wideLineStyleName = BorderStyle.Name;
                    }
                    else wideLineStyleName = "<Thin Lines>";
                    //确定字体，字号等
                    List<TextNoteType> refs = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
                    //string tableFont = "3_宋体_0.7";
                    string tableFont = $"{TextSize}_{Font}_0.7";
                    string[] fontAttr = tableFont.Split('_').ToArray();
                    double sizeFactor;
                    double.TryParse(fontAttr[0].ToString(), out sizeFactor);
                    //string tableFont2 = "3_仿宋_0.7";
                    //string tableFont3 = "3_宋体_0.7";
                    bool found = refs.Any(item => item.Name == tableFont);
                    //当无同名时新建
                    TextNoteType textType = found
                        ? refs.FirstOrDefault(item => item.Name == tableFont)
                        : refs.FirstOrDefault()?.Duplicate(tableFont) as TextNoteType;
                    if (textType != null)
                    {
                        textType.get_Parameter(BuiltInParameter.TEXT_FONT).Set(fontAttr[1]);
                        textType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(sizeFactor / 304.8);
                        textType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE).Set(0.7);
                    }
                    // 2. 初始化数据和对齐数组
                    string[,] tableData = new string[rowCount, columnCount];
                    TextAlignment[] columnAlignments = new TextAlignment[columnCount];
                    // 3. 解析每行数据
                    for (int i = 0; i < rowCount; i++)
                    {
                        string[] cells = lines[i].Split(',');
                        for (int j = 0; j < columnCount; j++)
                        {
                            string cellContent = cells[j].Trim();
                            TextAlignment alignment;
                            switch (ColumnPropertiesList[j].Alignment)
                            {
                                case "居中":
                                    alignment = TextAlignment.Center;
                                    break;
                                case "靠左":
                                    alignment = TextAlignment.Left;
                                    break;
                                default:
                                    alignment = TextAlignment.Center;
                                    break;
                            }
                            tableData[i, j] = cellContent;
                            // 第一行决定列对齐方式（可根据需要改为其他逻辑）
                            if (i == 0) columnAlignments[j] = alignment;
                        }
                    }
                    // 绘制表格
                    XYZ origin = uiDoc.Selection.PickPoint("请选择表格左上角点");
                    double totalWidth;
                    switch (InnerLineStyle)
                    {
                        case "通长绘制":
                            // 收集所有线条一次性创建
                            List<Curve> allCurves = new List<Curve>();
                            double t1 = 0;
                            for (int i = 0; i < columnCount; i++) t1 += baseCellWidth * colWidthFactors[i];
                            totalWidth = t1;
                            // 内部网格线
                            for (int i = 0; i < rowCount; i++)
                            {
                                double y = origin.Y - i * rowHeight;
                                allCurves.Add(Line.CreateBound(
                                    new XYZ(origin.X, y, origin.Z),
                                    new XYZ(origin.X + totalWidth, y, origin.Z)));
                            }
                            for (int j = 0; j < columnCount; j++)
                            {
                                double x = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
                                allCurves.Add(Line.CreateBound(
                                    new XYZ(x, origin.Y, origin.Z),
                                    new XYZ(x, origin.Y - rowCount * rowHeight, origin.Z)));
                            }
                            // 批量创建（效率提升关键）
                            foreach (var item in allCurves)
                            {
                                doc.Create.NewDetailCurve(activeView, item);
                            }
                            break;
                        default:
                            // 创建所有单元格边界(有重复)
                            for (int i = 0; i < rowCount; i++)
                            {
                                for (int j = 0; j < columnCount; j++)
                                {
                                    // 计算当前单元格的起始点
                                    double cellX = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
                                    double cellY = origin.Y - i * rowHeight;
                                    // 绘制当前单元格的四条边
                                    XYZ startTop = new XYZ(cellX, cellY, origin.Z);
                                    XYZ endTop = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY, origin.Z);
                                    Line lineTop = Line.CreateBound(startTop, endTop);
                                    doc.Create.NewDetailCurve(activeView, lineTop);
                                    XYZ startRight = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY, origin.Z);
                                    XYZ endRight = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY - rowHeight, origin.Z);
                                    Line lineRight = Line.CreateBound(startRight, endRight);
                                    doc.Create.NewDetailCurve(activeView, lineRight);
                                    XYZ startBottom = new XYZ(cellX, cellY - rowHeight, origin.Z);
                                    XYZ endBottom = new XYZ(cellX + baseCellWidth * colWidthFactors[j], cellY - rowHeight, origin.Z);
                                    Line lineBottom = Line.CreateBound(startBottom, endBottom);
                                    doc.Create.NewDetailCurve(activeView, lineBottom);
                                    XYZ startLeft = new XYZ(cellX, cellY, origin.Z);
                                    XYZ endLeft = new XYZ(cellX, cellY - rowHeight, origin.Z);
                                    Line lineLeft = Line.CreateBound(startLeft, endLeft);
                                    doc.Create.NewDetailCurve(activeView, lineLeft);
                                }
                            }
                            break;
                    }
                    //外框线绘制
                    double total = 0;
                    for (int i = 0; i < columnCount; i++) total += baseCellWidth * colWidthFactors[i];
                    totalWidth = total;
                    double totalHeight = rowCount * rowHeight;
                    Line topLine = Line.CreateBound(origin, new XYZ(origin.X + totalWidth, origin.Y, origin.Z));
                    Line rightLine = Line.CreateBound(new XYZ(origin.X + totalWidth, origin.Y, origin.Z), new XYZ(origin.X + totalWidth, origin.Y - totalHeight, origin.Z));
                    Line bottomLine = Line.CreateBound(new XYZ(origin.X, origin.Y - totalHeight, origin.Z), new XYZ(origin.X + totalWidth, origin.Y - totalHeight, origin.Z));
                    Line leftLine = Line.CreateBound(new XYZ(origin.X, origin.Y, origin.Z), new XYZ(origin.X, origin.Y - totalHeight, origin.Z));
                    DetailLine topBorder = doc.Create.NewDetailCurve(activeView, topLine) as DetailLine;
                    DetailLine rightBorder = doc.Create.NewDetailCurve(activeView, rightLine) as DetailLine;
                    DetailLine bottomBorder = doc.Create.NewDetailCurve(activeView, bottomLine) as DetailLine;
                    DetailLine leftBorder = doc.Create.NewDetailCurve(activeView, leftLine) as DetailLine;
                    //加粗外框线
                    CategoryNameMap subcats = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).SubCategories;
                    Category setstyle = subcats.Cast<Category>().FirstOrDefault(c => c.Name == wideLineStyleName);
                    topBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
                    rightBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
                    bottomBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
                    leftBorder.LineStyle = setstyle.GetGraphicsStyle(GraphicsStyleType.Projection);
                    //表内加字
                    for (int i = 0; i < rowCount; i++)
                    {
                        for (int j = 0; j < columnCount; j++)
                        {
                            double cellX = origin.X + GetColumnWidthSum(j, baseCellWidth, colWidthFactors);
                            double cellY = origin.Y - i * rowHeight;
                            XYZ cellOrigin = new XYZ(cellX, cellY - rowHeight / 2, origin.Z); // 垂直居中
                            AddTextToCell(doc, activeView, cellOrigin, tableData[i, j], baseCellWidth * colWidthFactors[j], columnAlignments[j], textType.Id);
                            //AddTextToCell(doc, activeView, cellOrigin, tableData[i, j], baseCellWidth * colWidthFactors[j], TextAlignment.Center, textType.Id);
                        }
                    }
                    tx.Commit();
                }
            }
            else TaskDialog.Show("tt", "请载入csv数据源表格并重试");
        }

        private double GetColumnWidthSum(int columnIndex, double baseCellWidth, double[] colWidthFactors)
        {
            double sum = 0;
            for (int i = 0; i < columnIndex; i++)
            {
                sum += baseCellWidth * colWidthFactors[i];
            }
            return sum;
        }
        public enum TextAlignment { Left, Center }
        private TextNote AddTextToCell(Document doc, Autodesk.Revit.DB.View view, XYZ insertionPoint, string text, double cellWidth, TextAlignment alignment, ElementId textTypeId)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "-"; // 默认显示内容
            }
            // 根据对齐方式调整插入点X坐标
            double offsetX = 0;
            switch (alignment)
            {
                case TextAlignment.Center:
                    offsetX = cellWidth / 3;
                    break;
                case TextAlignment.Left:
                default:
                    offsetX = 1.5 / 304.8; // 留0.1英尺左边距
                    break;
            }
            XYZ adjustedPoint = new XYZ(insertionPoint.X + offsetX, insertionPoint.Y, insertionPoint.Z);
            TextNoteOptions noteOptions = new TextNoteOptions();
            noteOptions.TypeId = textTypeId;
            //noteOptions.HorizontalAlignment = HorizontalTextAlignment.Right;
            noteOptions.VerticalAlignment = VerticalTextAlignment.Middle;
            TextNote textNote = TextNote.Create(doc, view.Id, adjustedPoint, text, noteOptions);
            // 设置对齐参数// 1=左, 2=中, 3=右
            textNote.get_Parameter(BuiltInParameter.TEXT_ALIGN_HORZ).Set((int)alignment);
            return textNote;
        }
        private int tableScale = 100;
        public int TableScale
        {
            get { return tableScale; }
            set { tableScale = value; }
        }
        //private string tableName = "默认表格名称，请务必修改";
        //public string TableName
        //{
        //    get { return tableName; }
        //    set { tableName = value; }
        //}
        private double baseWidth = 20.0;
        public double BaseWidth
        {
            get { return baseWidth; }
            set { baseWidth = value; }
        }
        private double textSize = 3.0;
        public double TextSize
        {
            get { return textSize; }
            set { textSize = value; }
        }
        public string InnerLineStyle { get; set; } = "通长绘制";
        public string Font { get; set; } = "宋体";
        public ICommand RemoveXmlCommand => new BaseBindingCommand(RemoveXml);
        private void RemoveXml(object obj)
        {
            if (SelectedTableSingle != null)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        int i = 0;
                        TableCollection tableCollection = XMLUtil.DeserializeFromXml<TableCollection>(filePath);
                        List<TableSingle> tss = tableCollection.tableSingles;
                        // 使用 RemoveAll 方法移除所有为 tableName 的元素
                        i = tss.RemoveAll(ts => ts.tableName == selectedTableSingle.tableName);
                        XMLUtil.SerializeToXml(filePath, tableCollection);
                        var itemsToRemove = TableSingles.Where(ts => ts.tableName == SelectedTableSingle.tableName).ToList();
                        foreach (var item in itemsToRemove)
                        {
                            TableSingles.Remove(item);
                        }
                        TaskDialog.Show("tt", $"已删除表格样式{i}个");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", ex.Message.ToString());
                }
            }
        }
        public ICommand ExportXmlCommand => new BaseBindingCommand(ExportXml);
        private void ExportXml(object obj)
        {
            if (tName == null)
            {
                TaskDialog.Show("tt", "请输入表格名称");
                return;
            }
            TableSingle tableSingle = new TableSingle() { tableName = tName };
            tableSingle.tableEntities = new List<TableEntity>();
            foreach (ColumnProperties item in ColumnPropertiesList)
            {
                TableEntity entity1 = new TableEntity()
                {
                    entityName = item.Title,
                    entityWidth = item.Width,
                    entityAligh = item.Alignment,
                    //entityRow = item.RowCount
                };
                tableSingle.tableEntities.Add(entity1);
            }
            // 2. 检查 XML 文件是否存在，并加载现有数据（如果存在）
            TableCollection tableCollection;
            if (File.Exists(filePath))
            {
                // 反序列化现有 XML
                tableCollection = XMLUtil.DeserializeFromXml<TableCollection>(filePath);
                // 确保 tableSingles 列表已初始化
                if (tableCollection.tableSingles == null)
                {
                    tableCollection.tableSingles = new List<TableSingle>();
                }
            }
            else
            {
                tableCollection = new TableCollection();
                tableCollection.tableSingles = new List<TableSingle>();
            }
            tableCollection.tableSingles.Add(tableSingle);
            XMLUtil.SerializeToXml(filePath, tableCollection);
            TaskDialog.Show("tt", "已生成表格新样式");
        }
        //public bool CanExportXML { get; set; } = false;
        private bool _canExportXML = false;
        public bool CanExportXML
        {
            get { return _canExportXML; }
            set
            {
                if (_canExportXML != value)
                {
                    _canExportXML = value;
                    OnPropertyChanged(nameof(CanExportXML));
                }
            }
        }
        public ICommand GetCsvCommand => new BaseBindingCommand(GetCsv);
        private string[] lines { get; set; }
        private void GetCsv(object obj)
        {
            ColumnPropertiesList.Clear();
            //XmlDoc.Instance.Task.Run(app =>
            //{       //});
            OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            fDialog.Filter = "csv 文件 (*.csv)|*.csv";
            if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string firstLine = File.ReadLines(fDialog.FileName).First();
                lines = File.ReadAllLines(fDialog.FileName);
                //csv异常监测
                int totalColumns = lines[0].Split(',').Length;
                if (totalColumns > 12)
                {
                    TaskDialog.Show("tt", "本工具暂不支持超过12列表格绘制");
                    return;
                }
                // 检查包含额外逗号的异常行数
                int rowsWithExtraCommas = 0;
                List<int> rowsWithExtraCommasPositions = new List<int>();
                for (int i = 0; i < lines.Length; i++)
                {
                    int columnCount = lines[i].Split(',').Length;
                    if (columnCount != totalColumns)
                    {
                        rowsWithExtraCommas++;
                        rowsWithExtraCommasPositions.Add(i + 1); // 行号从 1 开始
                    }
                }
                TaskDialog.Show("tt", $"待生成表格总行数: {lines.Length}+总列数: {totalColumns}");
                if (rowsWithExtraCommasPositions.Count != 0)
                {
                    TaskDialog.Show("tt", "异常行位置: " + string.Join(", ", rowsWithExtraCommasPositions));
                    return;
                }
                csvContents = lines;
                ////当对比xml内容有相同组合时，列出标题到combobox，内容到datagrid并绑定。更新ColumnPropertiesList
                try
                {
                    TableCollection test = XMLUtil.DeserializeFromXml<TableCollection>(filePath);
                    TableSingles = new ObservableCollection<TableSingle>();
                    foreach (TableSingle ts in test.tableSingles)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (TableEntity item in ts.tableEntities)
                        {
                            sb.Append(item.entityName + ",");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        if (sb.ToString() == firstLine)
                        {
                            TableSingles.Add(ts);
                        }
                    }
                    //// 在初始化时设置默认选中项
                    if (TableSingles != null && TableSingles.Count > 0)
                    {
                        SelectedTableSingle = TableSingles[0]; // 默认选中第一个
                    }
                    //当对比xml内容无相同组合时
                    if (tableSingles.Count() == 0)
                    {
                        //// 按逗号分割字段,统计字段数量
                        tableTitles = firstLine.Split(',').ToList();
                        fieldCount = tableTitles.Count;
                        foreach (var title in tableTitles)
                        {
                            ColumnPropertiesList.Add(new ColumnProperties { Title = title });
                        }
                    }
                    CanExportXML = true;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", ex.Message.ToString());
                }
            }
        }
        private void UpdateDataGridFromSelectedTable()
        {
            if (SelectedTableSingle == null || SelectedTableSingle.tableEntities == null)
            {
                ColumnPropertiesList.Clear();
                return;
            }
            ColumnPropertiesList.Clear();
            // 将选中的 TableSingle 的 tableEntities 映射到 ColumnPropertiesList
            foreach (TableEntity item in SelectedTableSingle.tableEntities)
            {
                ColumnPropertiesList.Add(new ColumnProperties
                {
                    Title = item.entityName,
                    Width = item.entityWidth,
                    Alignment = item.entityAligh,
                    //RowCount = item.entityRow
                });
            }
        }
        private ObservableCollection<TableSingle> tableSingles = new ObservableCollection<TableSingle>();
        public ObservableCollection<TableSingle> TableSingles
        {
            get => tableSingles;
            set
            {
                tableSingles = value;
                OnPropertyChanged(nameof(TableSingles));
            }
        }
        private TableSingle selectedTableSingle;
        public TableSingle SelectedTableSingle
        {
            get => selectedTableSingle;
            set
            {
                selectedTableSingle = value;
                OnPropertyChanged(nameof(SelectedTableSingle));
                UpdateDataGridFromSelectedTable();
            }
        }
        public List<string> tableTitles = new List<string>();
        public int fieldCount = 0;
        public string tName;
        public string TName
        {
            get => tName;
            set
            {
                tName = value;
                OnPropertyChanged(nameof(tName));
            }
        }
    }
    [XmlType(TypeName = "TableCollection")]
    public class TableCollection
    {
        [XmlArray("tableSingles")]
        public List<TableSingle> tableSingles { get; set; }
    }
    [XmlType(TypeName = "TableSingle")]
    public class TableSingle
    {
        [XmlAttribute]
        public string tableName { get; set; }
        [XmlArray("tableEntities")]
        public List<TableEntity> tableEntities { get; set; }
    }
    [XmlType(TypeName = "TableEntity")]
    public class TableEntity
    {
        [XmlAttribute]
        public string entityName { get; set; }
        [XmlAttribute]
        public double entityWidth { get; set; }
        [XmlAttribute]
        public string entityAligh { get; set; }
        //[XmlAttribute]
        //public int entityRow { get; set; }
    }
    //TableEntity entity1 = new TableEntity() { entityName = "楼号", entityWidth = "60", entityAligh = "居中", entityRow = "1" };
    //TableEntity entity2 = new TableEntity() { entityName = "层数", entityWidth = "160", entityAligh = "居中", entityRow = "1" };
    //TableEntity entity3 = new TableEntity() { entityName = "建筑功能", entityWidth = "260", entityAligh = "居中", entityRow = "1" };
    //TableSingle tableSingle = new TableSingle() { tableName = "平面功能表" };
    //tableSingle.tableEntities = new List<TableEntity>() { entity1, entity2, entity3 };
    //TableCollection tableCollection1 = new TableCollection();
    //tableCollection1.tableSingles = new List<TableSingle>() { tableSingle };
    //XMLUtil.SerializeToXml(@"D:\temp\test.xml", tableCollection1);
}
