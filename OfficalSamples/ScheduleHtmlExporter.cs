//using System.Web.UI;

namespace CreatePipe.OfficalSamples
{
    ///// <summary>
    ///// 明细表HTML导出器 - 将明细表数据写入HTML文件
    ///// </summary>
    //internal class ScheduleHtmlExporter
    //{
    //    private readonly ViewSchedule _schedule;
    //    private HtmlTextWriter _writer;
    //    private TableSectionData _headerSection;
    //    private TableSectionData _bodySection;
    //    private readonly HashSet<(int row, int col)> _writtenCells = new HashSet<(int, int)>();

    //    // 预定义颜色
    //    private static readonly Color Black = new Color(0, 0, 0);
    //    private static readonly Color White = new Color(255, 255, 255);

    //    public ScheduleHtmlExporter(ViewSchedule schedule) => _schedule = schedule;

    //    /// <summary>
    //    /// 导出明细表为HTML文件
    //    /// </summary>
    //    public void ExportToHtml()
    //    {
    //        var htmlPath = GetHtmlFilePath();

    //        using (var stringWriter = new StreamWriter(htmlPath))
    //        using (_writer = new HtmlTextWriter(stringWriter))
    //        {
    //            _writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
    //            _writer.RenderBeginTag(HtmlTextWriterTag.Div);

    //            WriteHeader();
    //            WriteBody();

    //            _writer.RenderEndTag();
    //        }

    //        // 打开生成的HTML文件
    //        Process.Start(htmlPath);
    //    }

    //    /// <summary>
    //    /// 获取HTML文件路径 - 使用字符串插值
    //    /// </summary>
    //    private string GetHtmlFilePath()
    //    {
    //        var tempFolder = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
    //        var safeName = ReplaceIllegalCharacters(_schedule.Name);
    //        return Path.Combine(tempFolder, $"{safeName}.html");
    //    }

    //    /// <summary>
    //    /// 替换非法文件名字符
    //    /// </summary>
    //    private static string ReplaceIllegalCharacters(string input)
    //    {
    //        var illegalChars = Path.GetInvalidFileNameChars();
    //        return illegalChars.Aggregate(input, (current, ch) => current.Replace(ch, '_'));
    //    }

    //    /// <summary>
    //    /// 写入表头部分
    //    /// </summary>
    //    private void WriteHeader()
    //    {
    //        _writtenCells.Clear();

    //        _writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
    //        _writer.RenderBeginTag(HtmlTextWriterTag.Table);

    //        _headerSection = _schedule.GetTableData().GetSectionData(SectionType.Header);
    //        var lastRow = _headerSection.FirstRowNumber + _headerSection.NumberOfRows;

    //        for (int row = _headerSection.FirstRowNumber; row < lastRow; row++)
    //        {
    //            WriteSectionRow(SectionType.Header, _headerSection, row);
    //        }

    //        _writer.RenderEndTag();
    //    }

    //    /// <summary>
    //    /// 写入表体部分
    //    /// </summary>
    //    private void WriteBody()
    //    {
    //        _writtenCells.Clear();

    //        _writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
    //        _writer.RenderBeginTag(HtmlTextWriterTag.Table);

    //        _bodySection = _schedule.GetTableData().GetSectionData(SectionType.Body);
    //        var lastRow = _bodySection.FirstRowNumber + _bodySection.NumberOfRows;

    //        for (int row = _bodySection.FirstRowNumber; row < lastRow; row++)
    //        {
    //            WriteSectionRow(SectionType.Body, _bodySection, row);
    //        }

    //        _writer.RenderEndTag();
    //    }

    //    /// <summary>
    //    /// 写入分区的一行
    //    /// </summary>
    //    private void WriteSectionRow(SectionType sectionType, TableSectionData data, int row)
    //    {
    //        _writer.RenderBeginTag(HtmlTextWriterTag.Tr);

    //        var lastCol = data.FirstColumnNumber + data.NumberOfColumns;

    //        for (int col = data.FirstColumnNumber; col < lastCol; col++)
    //        {
    //            // 跳过已写入的合并单元格
    //            if (_writtenCells.Contains((row, col))) continue;

    //            var mergedCell = data.GetMergedCell(row, col);
    //            var style = data.GetTableCellStyle(row, col);

    //            WriteCell(sectionType, row, col, mergedCell, style);
    //        }

    //        _writer.RenderEndTag();
    //    }

    //    /// <summary>
    //    /// 写入单个单元格
    //    /// </summary>
    //    private void WriteCell(SectionType sectionType, int row, int col,
    //        TableMergedCell mergedCell, TableCellStyle style)
    //    {
    //        // 记录合并范围内的所有单元格
    //        var (colspan, rowspan) = (mergedCell.Right - mergedCell.Left + 1,
    //                                   mergedCell.Bottom - mergedCell.Top + 1);

    //        for (int r = mergedCell.Top; r <= mergedCell.Bottom; r++)
    //            for (int c = mergedCell.Left; c <= mergedCell.Right; c++)
    //            {
    //                _writtenCells.Add((r, c));
    //            }

    //        // 添加colspan属性
    //        if (colspan > 1)
    //            _writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());

    //        // 添加rowspan属性
    //        if (rowspan > 1)
    //            _writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, rowspan.ToString());

    //        // 添加背景色
    //        if (!ColorsEqual(style.BackgroundColor, White))
    //            _writer.AddAttribute(HtmlTextWriterAttribute.Bgcolor, GetColorHtmlString(style.BackgroundColor));

    //        // 添加水平对齐
    //        _writer.AddAttribute(HtmlTextWriterAttribute.Align, GetAlignString(style.FontHorizontalAlignment));

    //        _writer.RenderBeginTag(HtmlTextWriterTag.Td);

    //        // 应用字体样式
    //        using var styleScope = new StyleScope(_writer);

    //        if (style.IsFontUnderline) styleScope.AddStyle(HtmlTextWriterTag.U);
    //        if (style.IsFontItalic) styleScope.AddStyle(HtmlTextWriterTag.I);
    //        if (style.IsFontBold) styleScope.AddStyle(HtmlTextWriterTag.B);

    //        // 写入单元格内容
    //        var cellText = _schedule.GetCellText(sectionType, row, col);
    //        _writer.Write(string.IsNullOrEmpty(cellText) ? "&nbsp;" : cellText);

    //        _writer.RenderEndTag(); // 结束</td>
    //    }

    //    #region 辅助方法
    //    /// <summary>
    //    /// 获取颜色的HTML字符串 (#RRGGBB)
    //    /// </summary>
    //    private static string GetColorHtmlString(Color color) =>
    //        $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    //    /// <summary>
    //    /// 比较两个颜色是否相等
    //    /// </summary>
    //    private static bool ColorsEqual(Color c1, Color c2) =>
    //        c1.Red == c2.Red && c1.Green == c2.Green && c1.Blue == c2.Blue;

    //    /// <summary>
    //    /// 获取水平对齐的HTML字符串
    //    /// </summary>
    //    private static string GetAlignString(HorizontalAlignmentStyle style) => style switch
    //    {
    //        HorizontalAlignmentStyle.Left => "left",
    //        HorizontalAlignmentStyle.Center => "center",
    //        HorizontalAlignmentStyle.Right => "right",
    //        _ => ""
    //    };
    //    #endregion

    //    /// <summary>
    //    /// 样式作用域辅助类 - 自动管理嵌套标签的关闭
    //    /// </summary>
    //    private class StyleScope : IDisposable
    //    {
    //        private readonly HtmlTextWriter _writer;
    //        private readonly List<HtmlTextWriterTag> _openedTags = new List<HtmlTextWriterTag>();

    //        public StyleScope(HtmlTextWriter writer) => _writer = writer;

    //        public void AddStyle(HtmlTextWriterTag tag)
    //        {
    //            _writer.RenderBeginTag(tag);
    //            _openedTags.Add(tag);
    //        }

    //        public void Dispose()
    //        {
    //            for (int i = _openedTags.Count - 1; i >= 0; i--)
    //            {
    //                _writer.RenderEndTag();
    //            }
    //        }
    //    }
}
