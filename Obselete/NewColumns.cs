using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    //0718 初步完成
    [Transaction(TransactionMode.Manual)]
    public class NewColumns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            List<string> familyNames = new List<string>();
            ICollection<Element> families = new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements();
            // 2. 遍历所有族，检查其名称是否与目标名称匹配
            foreach (Family family in families)
            {
                familyNames.Add(family.Name);
            }
            if (!familyNames.Contains("CADC_结构_混凝土矩形柱") || !familyNames.Contains("CADC_结构_混凝土圆形柱"))
            {
                TaskDialog.Show("tt", "未找到名称为‘CADC_结构_混凝土矩形柱“及”CADC_结构_混凝土圆形柱“的对象，无法完成后续操作，请提前载入指定族");
                return Result.Cancelled;
            }
            if (doc.ActiveView.ViewType != ViewType.FloorPlan &&
    doc.ActiveView.ViewType != ViewType.EngineeringPlan)
            {
                TaskDialog.Show("错误", $"请在平面视图执行本操作。");
                return Result.Cancelled;
            }
            // 选择生成方式
            TaskDialog td = new TaskDialog("选择操作")
            {
                MainInstruction = "请选择柱翻模操作方式:",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                CommonButtons = TaskDialogCommonButtons.Cancel,
            };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "在正交轴线所有交点处生成柱子");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "参考CAD尺寸在相同位置生成柱子");
            //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "导出构件按类别详细数量统计csv");
            TaskDialogResult tdRes = td.Show();
            if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
            if (tdRes == TaskDialogResult.CommandLink1)
            {
                List<int> columnSizes = new List<int>() { 400, 500, 600, 800 };
                var columnDefinition = new Dictionary<string, List<int>>
                {
                    { "方柱", columnSizes},{ "圆柱", columnSizes}
                };
                var window = new UniversalDoubleComboboxWindow("设置柱参数", "1. 请选择柱类别:", "2. 请选择柱尺寸:", columnDefinition);
                if (window.ShowDialog() != true) return Result.Cancelled;
                // 获取第一个下拉框的选中项 (这是一个string)
                string selectedScope = window.SelectedItem1 as string;
                bool isRecColumn = (selectedScope == "方柱");
                try
                {
                    //// 收集所有轴线
                    //IList<Grid> grids = new FilteredElementCollector(doc)
                    //    .OfClass(typeof(Grid)).Cast<Grid>().ToList();
                    //拾取轴线
                    List<Grid> grids = uiDoc.Selection.PickObjects(ObjectType.Element, new GridFilter(), "请拾取要在交叉处生成柱子的所有正交轴线").Select(r => doc.GetElement(r) as Grid).ToList();
                    List<XYZ> intersectionPoints = FindGridIntersectionPoints(grids);
                    if (intersectionPoints.Count == 0)
                    {
                        TaskDialog.Show("提示", "未找到任何轴线交点。");
                        return Result.Succeeded;
                    }
                    // --- 3. 在交点处创建柱子 ---
                    using (Transaction ts = new Transaction(doc, "在轴线交点处创建柱子"))
                    {
                        ts.Start();
                        int createdCount = 0;
                        foreach (XYZ point in intersectionPoints)
                        {
                            FamilySymbol columnSymbol = null;
                            if (isRecColumn)
                            {
                                double widthInFeet = Convert.ToDouble(window.SelectedItem2) / 304.8;
                                double heightInFeet = Convert.ToDouble(window.SelectedItem2) / 304.8;
                                columnSymbol = CreateOrGetRectangularColumnSymbol(doc, "CADC_结构_混凝土矩形柱", widthInFeet, heightInFeet, ts);
                            }
                            else
                            {
                                double diameterInFeet = Convert.ToDouble(window.SelectedItem2) / 304.8;
                                columnSymbol = CreateOrGetRoundColumnSymbol(doc, "CADC_结构_混凝土圆形柱", diameterInFeet, ts);
                            }
                            if (columnSymbol != null)
                            {
                                CreateColumnInstance(doc, point, columnSymbol);
                                createdCount++;
                            }
                            else
                            {
                                // 如果无法获取或创建族类型，辅助方法内部已回滚事务并提示用户
                                return Result.Failed;
                            }
                        }
                        ts.Commit();
                        TaskDialog.Show("成功", $"操作完成！在 {intersectionPoints.Count} 个轴网交点处成功创建了 {createdCount} 个柱子。");
                    }
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return Result.Failed;
                }
            }
            if (tdRes == TaskDialogResult.CommandLink2)
            {
                try
                {
                    using (Transaction ts = new Transaction(doc, "从CAD生成柱子"))
                    {
                        ts.Start();
                        Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement, "请拾取一个代表柱子的CAD图元");
                        Element cadLink = doc.GetElement(r);
                        GeometryObject pickedGeoObj = cadLink.GetGeometryObjectFromReference(r);
                        if (pickedGeoObj == null || pickedGeoObj.GraphicsStyleId == ElementId.InvalidElementId)
                        {
                            TaskDialog.Show("错误", "无法获取有效的图形样式信息。");
                            return Result.Failed;
                        }
                        ElementId graphicsStyleId = pickedGeoObj.GraphicsStyleId;
                        GeometryElement geoElem = cadLink.get_Geometry(new Options());
                        int createdRectCount = 0;
                        int createdRoundCount = 0;
                        // 使用 SelectMany 遍历所有几何实例
                        foreach (var inst in geoElem.OfType<GeometryInstance>())
                        {
                            Transform transform = inst.Transform;
                            GeometryElement instGeo = inst.GetInstanceGeometry();
                            // --- 1. 处理闭合多段线（生成矩形柱） ---
                            var polyLinesOnLayer = instGeo.OfType<PolyLine>()
                                .Where(pl => pl.GraphicsStyleId == graphicsStyleId);
                            foreach (var polyLine in polyLinesOnLayer)
                            {
                                IList<XYZ> points = polyLine.GetCoordinates();
                                // 正确的闭合判断
                                bool isClosed = points.Count > 2 && points[0].IsAlmostEqualTo(points[points.Count - 1]);
                                if (!isClosed) continue;
                                Outline outline = polyLine.GetOutline();
                                double b = Math.Abs(outline.MaximumPoint.X - outline.MinimumPoint.X);
                                double h = Math.Abs(outline.MaximumPoint.Y - outline.MinimumPoint.Y);
                                XYZ centerInCad = (outline.MaximumPoint + outline.MinimumPoint) / 2.0;
                                XYZ centerInRevit = transform.OfPoint(centerInCad);
                                FamilySymbol columnSymbol = CreateOrGetRectangularColumnSymbol(doc, "CADC_结构_混凝土矩形柱", b, h, ts);
                                if (columnSymbol != null)
                                {
                                    CreateColumnInstance(doc, centerInRevit, columnSymbol);
                                    createdRectCount++;
                                }
                                else return Result.Failed;
                            }
                            // --- 2. 处理圆（生成圆柱） ---
                            var arcsOnLayer = instGeo.OfType<Arc>().Where(a => a.GraphicsStyleId == graphicsStyleId);
                            foreach (var arc in arcsOnLayer)
                            {
                                // 判断是否为一个完整的圆
                                // 一个完整的圆，其起点和终点在参数上相差2*PI，或者它是一个无边界的圆弧
                                bool isFullCircle = !arc.IsBound || arc.GetEndPoint(0).IsAlmostEqualTo(arc.GetEndPoint(1));
                                if (!isFullCircle) continue;
                                double diameter = arc.Radius * 2;
                                XYZ centerInCad = arc.Center;
                                XYZ centerInRevit = transform.OfPoint(centerInCad);
                                FamilySymbol columnSymbol = CreateOrGetRoundColumnSymbol(doc, "CADC_结构_混凝土圆形柱", diameter, ts);
                                if (columnSymbol != null)
                                {
                                    CreateColumnInstance(doc, centerInRevit, columnSymbol);
                                    createdRoundCount++;
                                }
                                else return Result.Failed;
                            }
                        }
                        if (createdRectCount == 0 && createdRoundCount == 0)
                        {
                            ts.RollBack(); // 没创建任何东西，回滚事务
                            TaskDialog.Show("提示", "在所选图层上未找到任何可识别的闭合多段线或圆。");
                            return Result.Cancelled;
                        }
                        ts.Commit();
                        TaskDialog.Show("成功", $"操作完成！\n创建了 {createdRectCount} 个矩形柱。\n创建了 {createdRoundCount} 个圆形柱。");
                        return Result.Succeeded;
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    // 确保在发生异常时回滚事务（如果它还未提交或回滚）
                    // using语句会自动处理，但明确一下逻辑
                    TaskDialog.Show("致命错误", $"执行过程中发生错误: {ex.Message}");
                    return Result.Failed;
                }
            }
            ////柱形态统计
            //try
            //{
            //    // 1. 收集模型中所有的结构柱实例 (过滤掉类型)
            //    FilteredElementCollector collector = new FilteredElementCollector(doc);
            //    collector.OfCategory(BuiltInCategory.OST_StructuralColumns);
            //    collector.WhereElementIsNotElementType();
            //    // 定义
            //    int totalColumns = collector.GetElementCount();
            //    int rectColumns = 0;
            //    int squareColumns = 0;
            //    // 定义容差，用于比较浮点数是否相等 (0.001英尺 约等于 0.3毫米)
            //    double tolerance = 0.001;
            //    // 2. 遍历所有结构柱实例
            //    foreach (Element elem in collector)
            //    {
            //        FamilyInstance column = elem as FamilyInstance;
            //        if (column == null) continue;
            //        FamilySymbol symbol = column.Symbol;
            //        if (symbol == null) continue;
            //        // 尝试获取宽度和高度参数
            //        // 兼容不同的族参数命名习惯：("b"/"h", "Width"/"Height", "宽度"/"高度")
            //        Parameter paramWidth = symbol.LookupParameter("b") ??
            //                               symbol.LookupParameter("Width") ??
            //                               symbol.LookupParameter("宽度");
            //        Parameter paramHeight = symbol.LookupParameter("h") ??
            //                                symbol.LookupParameter("Height") ??
            //                                symbol.LookupParameter("高度");
            //        // 3. 判断是否为矩形柱 (同时存在宽度和高度参数)
            //        if (paramWidth != null && paramHeight != null && paramWidth.HasValue && paramHeight.HasValue)
            //        {
            //            rectColumns++;
            //            double valWidth = paramWidth.AsDouble();
            //            double valHeight = paramHeight.AsDouble();
            //            // 4. 判断是否为方柱 (长宽相等，考虑浮点数精度)
            //            if (Math.Abs(valWidth - valHeight) < tolerance)
            //            {
            //                squareColumns++;
            //            }
            //        }
            //    }
            //    // 5. 弹出对话框显示统计结果
            //    string resultMsg = $"【结构柱统计结果】\n\n" +
            //                       $"模型中结构柱总数：{totalColumns} 个\n" +
            //                       $"其中 矩形柱 数量：{rectColumns} 个\n" +
            //                       $"其中 方柱(长宽相等) 数量：{squareColumns} 个";
            //    TaskDialog.Show("结构柱统计", resultMsg);
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    TaskDialog.Show("错误", $"统计过程中发生异常: {ex.Message}");
            //    return Result.Failed;
            //}
            return Result.Succeeded;
        }
        ///// <summary>
        ///// 辅助方法：为CSV字段添加引号（如果需要）
        ///// </summary>
        ///// <param name="field">要处理的文本</param>
        ///// <returns>符合CSV格式的字段</returns>
        //private string EscapeCsvField(string field)
        //{
        //    if (string.IsNullOrEmpty(field))
        //    {
        //        return "";
        //    }

        //    // 如果字段包含逗号、引号或换行符，则用双引号括起来
        //    if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        //    {
        //        // 将字段内的所有双引号替换为两个双引号
        //        return $"\"{field.Replace("\"", "\"\"")}\"";
        //    }
        //    return field;
        //} 

        //初生成矩形柱方法
        ///// <summary>
        ///// 根据给定的尺寸，查找或创建一个新的柱族类型
        ///// </summary>
        ///// <param name="doc">Revit文档</param>
        ///// <param name="targetFamilyName">目标族名称，如 "CADC_柱-混凝土-矩形"</param>
        ///// <param name="b">柱的宽度（英制单位）</param>
        ///// <param name="h">柱的高度（英制单位）</param>
        ///// <param name="transaction">当前活动的事务</param>
        ///// <returns>匹配或新建的FamilySymbol，失败则返回null</returns>
        //private FamilySymbol CreateOrGetColumnSymbol(Document doc, string targetFamilyName, double b, double h, Transaction transaction)
        //{
        //    // 使用LINQ更高效地查找目标族的所有类型
        //    FamilySymbol baseSymbol = new FilteredElementCollector(doc)
        //        .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
        //        .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
        //    if (baseSymbol == null)
        //    {
        //        TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
        //        return null;
        //    }
        //    // 定义一个比较容差，避免浮点数精度问题
        //    double tolerance = 0.001;
        //    // 查找具有相同尺寸的现有类型
        //    // 这是更稳健的方法：直接比较参数值，而不是比较类型名称字符串
        //    Family family = baseSymbol.Family;
        //    foreach (ElementId symbolId in family.GetFamilySymbolIds())
        //    {
        //        FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
        //        if (symbol == null) continue;
        //        Parameter paramB = symbol.LookupParameter("b");
        //        Parameter paramH = symbol.LookupParameter("h");
        //        if (paramB != null && paramH != null &&
        //            Math.Abs(paramB.AsDouble() - b) < tolerance &&
        //            Math.Abs(paramH.AsDouble() - h) < tolerance)
        //        {
        //            return symbol; // 找到完全匹配的类型
        //        }
        //    }
        //    // 如果没有找到，则创建新类型
        //    try
        //    {
        //        // 确保基础类型已激活
        //        if (!baseSymbol.IsActive) baseSymbol.Activate();
        //        // 将尺寸转换为毫米并四舍五入，用于命名
        //        string typeName = $"{Math.Round(b * 304.8)} x {Math.Round(h * 304.8)}mm";
        //        FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
        //        // 设置新类型的尺寸参数，注意：修改操作必须在事务中
        //        Parameter widthParam = newSymbol.LookupParameter("b");
        //        Parameter heightParam = newSymbol.LookupParameter("h");
        //        if (widthParam != null && heightParam != null)
        //        {
        //            widthParam.Set(b);
        //            heightParam.Set(h);
        //            return newSymbol;
        //        }
        //        else
        //        {
        //            TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到参数 'b' 或 'h'。");
        //            // 刚创建的类型是无效的，需要回滚事务
        //            transaction.RollBack();
        //            return null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("创建族类型失败", $"为尺寸 {b * 304.8:F2}x{h * 304.8:F2} 创建新类型时出错: {ex.Message}");
        //        transaction.RollBack();
        //        return null;
        //    }
        //}
        /// <summary>
        /// 根据给定的尺寸，查找或创建一个新的【矩形】柱族类型
        /// </summary>
        private FamilySymbol CreateOrGetRectangularColumnSymbol(Document doc, string targetFamilyName, double b, double h, Transaction transaction)
        {
            FamilySymbol baseSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
            if (baseSymbol == null)
            {
                TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
                return null;
            }
            double tolerance = 0.001; // 容差
            Family family = baseSymbol.Family;
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol == null) continue;
                Parameter paramB = symbol.LookupParameter("b");
                if (symbol.LookupParameter("b") == null)
                {
                    paramB = symbol.LookupParameter("宽度");
                }
                Parameter paramH = symbol.LookupParameter("h");
                if (symbol.LookupParameter("h") == null)
                {
                    paramH = symbol.LookupParameter("深度");
                }
                // 同时检查 b x h 和 h x b 两种情况，更鲁棒
                if (paramB != null && paramH != null &&
                    ((Math.Abs(paramB.AsDouble() - b) < tolerance && Math.Abs(paramH.AsDouble() - h) < tolerance) ||
                     (Math.Abs(paramB.AsDouble() - h) < tolerance && Math.Abs(paramH.AsDouble() - b) < tolerance)))
                {
                    return symbol;
                }
            }
            try
            {
                if (!baseSymbol.IsActive) baseSymbol.Activate();
                string typeName = $"{Math.Round(b * 304.8)} x {Math.Round(h * 304.8)}";
                FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
                Parameter widthParam = newSymbol.LookupParameter("b");
                if (newSymbol.LookupParameter("b") == null)
                {
                    widthParam = newSymbol.LookupParameter("宽度");
                }
                Parameter heightParam = newSymbol.LookupParameter("h");
                if (newSymbol.LookupParameter("h") == null)
                {
                    heightParam = newSymbol.LookupParameter("深度");
                }
                if (widthParam != null && heightParam != null)
                {
                    widthParam.Set(b);
                    heightParam.Set(h);
                    return newSymbol;
                }
                else
                {
                    TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到尺寸控制参数。");
                    transaction.RollBack();
                    return null;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("创建族类型失败", $"为尺寸 {b * 304.8:F2}x{h * 304.8:F2} 创建新类型时出错: {ex.Message}");
                transaction.RollBack();
                return null;
            }
        }
        /// <summary>
        /// 根据给定的直径，查找或创建一个新的【圆形】柱族类型
        /// </summary>
        private FamilySymbol CreateOrGetRoundColumnSymbol(Document doc, string targetFamilyName, double diameter, Transaction transaction)
        {
            FamilySymbol baseSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == targetFamilyName);
            if (baseSymbol == null)
            {
                TaskDialog.Show("错误", $"未在项目中找到名为 '{targetFamilyName}' 的族。");
                return null;
            }
            double tolerance = 0.001;
            Family family = baseSymbol.Family;
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol == null) continue;
                // 圆柱直径参数通常也叫 'b' 或 'd'，这里假设是 'b'
                Parameter paramB = symbol.LookupParameter("b");
                if (symbol.LookupParameter("b") == null)
                {
                    paramB = symbol.LookupParameter("直径");
                }
                if (paramB != null && Math.Abs(paramB.AsDouble() - diameter) < tolerance)
                {
                    return symbol; // 找到匹配的类型
                }
            }
            try
            {
                if (!baseSymbol.IsActive) baseSymbol.Activate();
                string typeName = $"D{Math.Round(diameter * 304.8)}"; // 命名为 D+直径(mm)
                FamilySymbol newSymbol = baseSymbol.Duplicate(typeName) as FamilySymbol;
                Parameter diameterParam = newSymbol.LookupParameter("b");
                if (newSymbol.LookupParameter("b") == null)
                {
                    diameterParam = newSymbol.LookupParameter("直径");
                }
                if (diameterParam != null)
                {
                    diameterParam.Set(diameter);
                    return newSymbol;
                }
                else
                {
                    TaskDialog.Show("错误", $"族 '{targetFamilyName}' 中找不到直径参数 'b'。");
                    transaction.RollBack();
                    return null;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("创建族类型失败", $"为直径 {diameter * 304.8:F2} 创建新类型时出错: {ex.Message}");
                transaction.RollBack();
                return null;
            }
        }
        /// <summary>
        /// 在指定点创建柱子实例
        /// </summary>
        private void CreateColumnInstance(Document doc, XYZ centerPoint, FamilySymbol symbol)
        {
            if (!symbol.IsActive) symbol.Activate();
            // 创建实例，默认标高为当前视图标高，偏移为0
            doc.Create.NewFamilyInstance(centerPoint, symbol, doc.ActiveView.GenLevel, StructuralType.Column);
        }
        /// <summary>
        /// 查找项目中所有直线轴线的交点
        /// </summary>
        private List<XYZ> FindGridIntersectionPoints(IEnumerable<Grid> grids)
        {
            if (grids.Count() < 2) return new List<XYZ>();
            // 提取并拍平直线轴线
            List<Line> infiniteFlatLines = new List<Line>();
            foreach (Grid grid in grids)
            {
                if (grid.Curve is Line line)
                {
                    XYZ originFlat = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, 0);
                    XYZ directionFlat = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                    Line unboundLine = Line.CreateUnbound(originFlat, directionFlat);
                    infiniteFlatLines.Add(unboundLine);
                }
            }
            // 计算交点并去重
            List<XYZ> intersectionPoints = new List<XYZ>();
            double tolerance = 0.001; // 去重容差（约0.3mm）
            for (int i = 0; i < infiniteFlatLines.Count; i++)
            {
                for (int j = i + 1; j < infiniteFlatLines.Count; j++)
                {
                    Line line1 = infiniteFlatLines[i];
                    Line line2 = infiniteFlatLines[j];

                    // 检查是否平行
                    if (line1.Direction.IsAlmostEqualTo(line2.Direction) || line1.Direction.IsAlmostEqualTo(-line2.Direction))
                    {
                        continue;
                    }

                    IntersectionResultArray intersections;
                    if (line1.Intersect(line2, out intersections) == SetComparisonResult.Overlap && intersections != null)
                    {
                        foreach (IntersectionResult iResult in intersections)
                        {
                            XYZ point = iResult.XYZPoint;
                            // 检查是否已存在非常接近的点
                            if (!intersectionPoints.Any(p => p.IsAlmostEqualTo(point, tolerance)))
                            {
                                intersectionPoints.Add(point);
                            }
                        }
                    }
                }
            }
            return intersectionPoints;
        }
    }
}
