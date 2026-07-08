using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class ArrangeRectangle : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //GLM方案
            // 1. 定义输入的矩形数据：(宽, 高, 数量, 类型编码)
            var items = new List<(double w, double h, int count, int code)>
            {
                (15.0, 15.0, 4, 1),
                (8.3, 2.5, 8, 2),
                (3.5, 1.5, 6, 3),
                (11.5, 4.2, 2, 4),
                (7.2, 2.1, 4, 5)
            };

            var rects = new List<PackingRect>();
            int idx = 0;
            foreach (var item in items)
            {
                for (int i = 0; i < item.count; i++)
                {
                    rects.Add(new PackingRect(item.w, item.h, idx++, item.code));
                }
            }

            // 2. 运行排样算法
            double maxWidth = rects.Sum(r => r.W);
            double minWidth = rects.Max(r => r.W);

            var (bestW, bestH, bestLayout) = PackWithWidthSearch(rects, minWidth, maxWidth, 0.5);

            if (bestLayout == null)
            {
                TaskDialog.Show("错误", "未找到可行排样方案。");
                return Result.Failed;
            }

            // 3. 在 Revit 中使用模型线绘制结果
            using (Transaction tx = new Transaction(doc, "绘制排样方案"))
            {
                tx.Start();

                // 获取当前视图的草图平面，如果不存在则在 XY 平面创建
                SketchPlane sketchPlane = doc.ActiveView.SketchPlane;
                if (sketchPlane == null)
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                    sketchPlane = SketchPlane.Create(doc, plane);
                    // 注意：将草图平面设置为活动视图的草图平面需要视图类型支持，这里直接拿来画线即可
                }

                // 绘制外层容器边界
                DrawRectangle(doc, sketchPlane, 0, 0, bestW, bestH);

                // 绘制内部每个矩形
                foreach (var rect in bestLayout)
                {
                    DrawRectangle(doc, sketchPlane, rect.X, rect.Y, rect.W, rect.H);
                }

                tx.Commit();
            }

            // 4. 显示结果信息
            string info = $"最优容器尺寸: {bestW:F2} × {bestH:F2}\n" +
                          $"最小容器面积: {bestW * bestH:F2}\n\n" +
                          "各矩形放置坐标 (左下角 x, y)：\n" +
                          "编码\t宽\t高\tx\t\ty\n";
            foreach (var r in bestLayout)
            {
                info += $"{r.TypeCode}\t{r.W:F2}\t{r.H:F2}\t{r.X:F2}\t{r.Y:F2}\n";
            }

            TaskDialog.Show("排样完成", info);

            ////0707 Gemini车位排布测试
            //// --- 1. 定义输入的矩形数据 ---
            //// (宽, 高, 数量, 类型编码)
            //var items = new List<(double w, double h, int count, int typeCode)>
            //{
            //    (15.0, 15.0, 4, 1),
            //    (8.3, 2.5, 8, 2),
            //    (3.5, 1.5, 6, 3),
            //    (11.5, 4.2, 2, 4),
            //    (7.2, 2.1, 4, 5)
            //};

            //var rects = new List<PackingRectangle>();
            //int idx = 0;
            //foreach (var item in items)
            //{
            //    for (int i = 0; i < item.count; i++)
            //    {
            //        rects.Add(new PackingRectangle(item.w, item.h, idx++, item.typeCode));
            //    }
            //}

            //// --- 2. 运行排样算法 ---
            //double totalWidth = rects.Sum(r => r.W);
            //double minWidth = rects.Max(r => r.W);
            //var (bestW, bestH, bestLayout) = PackingAlgorithm.PackWithWidthSearch(rects, minWidth, totalWidth, 0.5);

            //if (bestLayout == null)
            //{
            //    TaskDialog.Show("错误", "未能找到可行的排样方案。");
            //    return Result.Failed;
            //}

            //// --- 3. 在 Revit 中绘制结果 ---
            //try
            //{
            //    using (Transaction tx = new Transaction(doc, "绘制排样结果"))
            //    {
            //        tx.Start();

            //        // 获取一个用于绘制的平面 (XY平面, Z=0)
            //        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //        SketchPlane sketchPlane = SketchPlane.Create(doc, plane);

            //        // 绘制排样结果
            //        DrawLayout(doc, sketchPlane, bestLayout, bestW, bestH);

            //        tx.Commit();
            //    }

            //    // --- 4. 显示结果信息 ---
            //    string resultMessage = $"成功找到最优方案！\n\n" +
            //                           $"最优容器尺寸: {bestW:F2} × {bestH:F2}\n" +
            //                           $"容器面积: {bestW * bestH:F2}\n\n" +
            //                           "结果已使用模型线在默认三维视图中绘制。";
            //    TaskDialog.Show("排样完成", resultMessage);

            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            return Result.Succeeded;
        }
        ////0707 几何布置测试
        ///// <summary>
        ///// 使用模型线在Revit中绘制布局
        ///// </summary>
        //private void DrawLayout(Document doc, SketchPlane sketchPlane, List<PackingRectangle> layout, double containerW, double containerH)
        //{
        //    // Revit 内部单位是英尺(feet)，而我们的输入是无单位的（假定为毫米）
        //    // 需要进行单位转换
        //    const double mmToFeet = 1.0 / 304.8;

        //    // 绘制容器边界
        //    DrawRevitRectangle(doc, sketchPlane, 0, 0, containerW, containerH, mmToFeet);

        //    // 绘制每一个小矩形
        //    foreach (var rect in layout)
        //    {
        //        DrawRevitRectangle(doc, sketchPlane, rect.X, rect.Y, rect.W, rect.H, mmToFeet);
        //    }
        //}

        ///// <summary>
        ///// 绘制单个矩形的辅助方法
        ///// </summary>
        //private void DrawRevitRectangle(Document doc, SketchPlane sketchPlane, double x, double y, double w, double h, double scale)
        //{
        //    // 将坐标和尺寸从毫米转换为英尺
        //    double x_ft = x * scale;
        //    double y_ft = y * scale;
        //    double w_ft = w * scale;
        //    double h_ft = h * scale;

        //    // 定义矩形的四个角点
        //    XYZ p1 = new XYZ(x_ft, y_ft, 0);
        //    XYZ p2 = new XYZ(x_ft + w_ft, y_ft, 0);
        //    XYZ p3 = new XYZ(x_ft + w_ft, y_ft + h_ft, 0);
        //    XYZ p4 = new XYZ(x_ft, y_ft + h_ft, 0);

        //    // 创建四条线并生成模型线
        //    doc.Create.NewModelCurve(Line.CreateBound(p1, p2), sketchPlane);
        //    doc.Create.NewModelCurve(Line.CreateBound(p2, p3), sketchPlane);
        //    doc.Create.NewModelCurve(Line.CreateBound(p3, p4), sketchPlane);
        //    doc.Create.NewModelCurve(Line.CreateBound(p4, p1), sketchPlane);
        //}
        //0707 GLM方案
        // 浮点数比较精度
        private const double Eps = 1e-6;
        /// <summary>
        /// 搜索最优容器宽度以获得最小面积
        /// </summary>
        private (double bestW, double bestH, List<PackingRect> bestLayout) PackWithWidthSearch(
            List<PackingRect> rects, double minWidth, double maxWidth, double step)
        {
            double bestArea = double.MaxValue;
            double bestW = 0, bestH = 0;
            List<PackingRect> bestLayout = null;

            // 重新实现 numpy.arange (粗略搜索)
            var widths = Arange(minWidth, maxWidth + step, step);

            foreach (double w in widths)
            {
                // 按面积降序 + 宽度降序排序
                var sortedRects = rects.OrderByDescending(r => r.W * r.H).ThenByDescending(r => r.W).ToList();
                var (ok, h, layout) = SkylinePack(sortedRects, w);

                if (ok)
                {
                    double area = w * h;
                    if (area < bestArea - 1e-6)
                    {
                        bestArea = area;
                        bestW = w;
                        bestH = h;
                        bestLayout = layout;
                    }
                }
            }

            // 精细搜索
            if (bestLayout != null)
            {
                double fineStart = Math.Max(minWidth, bestW - step);
                double fineEnd = Math.Min(maxWidth, bestW + step);
                double fineStep = step / 5.0;

                var fineWidths = Arange(fineStart, fineEnd + fineStep, fineStep);
                foreach (double w in fineWidths)
                {
                    var sortedRects = rects.OrderByDescending(r => r.W * r.H).ThenByDescending(r => r.W).ToList();
                    var (ok, h, layout) = SkylinePack(sortedRects, w);

                    if (ok)
                    {
                        double area = w * h;
                        if (area < bestArea - 1e-6)
                        {
                            bestArea = area;
                            bestW = w;
                            bestH = h;
                            bestLayout = layout;
                        }
                    }
                }
            }

            return (bestW, bestH, bestLayout);
        }

        /// <summary>
        /// 天际线排样核心算法
        /// </summary>
        private (bool success, double height, List<PackingRect> layout) SkylinePack(List<PackingRect> rects, double containerWidth)
        {
            // 天际线段: (x1, x2, y)
            var skyline = new List<(double x1, double x2, double y)> { (0.0, containerWidth, 0.0) };

            // 深拷贝副本，避免修改原始列表
            var rectsCopy = rects.Select(r => r.Clone()).ToList();

            foreach (var rect in rectsCopy)
            {
                double bestX = -1;
                double bestY = double.MaxValue;
                int bestSegIdx = -1;

                for (int i = 0; i < skyline.Count; i++)
                {
                    var seg = skyline[i];
                    if (seg.x2 - seg.x1 + Eps >= rect.W)
                    {
                        if (seg.y < bestY - Eps)
                        {
                            bestY = seg.y;
                            bestX = seg.x1;
                            bestSegIdx = i;
                        }
                        else if (Math.Abs(seg.y - bestY) < Eps && seg.x1 < bestX)
                        {
                            bestX = seg.x1;
                            bestSegIdx = i;
                        }
                    }
                }

                if (bestX < 0) return (false, 0, null);

                rect.X = bestX;
                rect.Y = bestY;
                double left = bestX;
                double right = bestX + rect.W;

                var newSegs = new List<(double x1, double x2, double y)>();
                foreach (var seg in skyline)
                {
                    if (seg.x2 <= left + Eps || seg.x1 >= right - Eps)
                    {
                        newSegs.Add(seg);
                    }
                    else
                    {
                        if (seg.x1 < left - Eps) newSegs.Add((seg.x1, left, seg.y));
                        if (seg.x2 > right + Eps) newSegs.Add((right, seg.x2, seg.y));
                        newSegs.Add((Math.Max(seg.x1, left), Math.Min(seg.x2, right), seg.y + rect.H));
                    }
                }

                // 按 x1 排序以确保合并逻辑正确
                newSegs = newSegs.OrderBy(s => s.x1).ToList();

                // 合并相邻同高线段
                var merged = new List<(double x1, double x2, double y)>();
                foreach (var seg in newSegs)
                {
                    if (merged.Count == 0)
                    {
                        merged.Add(seg);
                    }
                    else
                    {
                        var last = merged[merged.Count - 1];
                        if (Math.Abs(last.x2 - seg.x1) < Eps && Math.Abs(last.y - seg.y) < Eps)
                        {
                            merged[merged.Count - 1] = (last.x1, seg.x2, last.y);
                        }
                        else
                        {
                            merged.Add(seg);
                        }
                    }
                }
                skyline = merged;
            }

            double maxHeight = skyline.Count > 0 ? skyline.Max(s => s.y) : 0;
            return (true, maxHeight, rectsCopy);
        }

        // ==========================================
        // 辅助工具部分
        // ==========================================

        /// <summary>
        /// 重新实现 numpy.arange 逻辑，避免浮点数累加误差
        /// </summary>
        private List<double> Arange(double start, double stop, double step)
        {
            var result = new List<double>();
            int count = (int)Math.Ceiling((stop - start) / step);
            for (int i = 0; i <= count; i++)
            {
                double val = start + i * step;
                if (val <= stop + Eps)
                {
                    result.Add(Math.Round(val, 4)); // 保留4位小数截断微小误差
                }
            }
            return result;
        }

        /// <summary>
        /// 使用 Revit 模型线绘制矩形
        /// </summary>
        private void DrawRectangle(Document doc, SketchPlane sketchPlane, double x, double y, double w, double h)
        {
            // 假设输入数据的单位与 Revit 内部单位一致 (通常为英尺或毫米，取决于项目设置)
            // 如果你的输入是毫米且项目是毫米，这里直接使用即可。如果是米/英尺请在此处乘以转换系数。
            XYZ p1 = new XYZ(x, y, 0);
            XYZ p2 = new XYZ(x + w, y, 0);
            XYZ p3 = new XYZ(x + w, y + h, 0);
            XYZ p4 = new XYZ(x, y + h, 0);

            doc.Create.NewModelCurve(Line.CreateBound(p1, p2), sketchPlane);
            doc.Create.NewModelCurve(Line.CreateBound(p2, p3), sketchPlane);
            doc.Create.NewModelCurve(Line.CreateBound(p3, p4), sketchPlane);
            doc.Create.NewModelCurve(Line.CreateBound(p4, p1), sketchPlane);
        }
    }
    //GLM方案
    /// <summary>
    /// 矩形数据结构类
    /// </summary>
    public class PackingRect
    {
        public double W { get; set; }
        public double H { get; set; }
        public int Idx { get; set; }
        public int TypeCode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public PackingRect(double w, double h, int idx, int typeCode)
        {
            W = w; H = h; Idx = idx; TypeCode = typeCode;
            X = 0; Y = 0;
        }

        public PackingRect Clone()
        {
            return new PackingRect(W, H, Idx, TypeCode) { X = X, Y = Y };
        }
    }

    //0707
    // ==========================================================
    // 1. 数据结构定义
    // ==========================================================
    /// <summary>
    /// 矩形类，用于排样算法
    /// </summary>
    public class PackingRectangle
    {
        public double W { get; set; }        // 宽度
        public double H { get; set; }        // 高度
        public int Idx { get; set; }         // 唯一编号
        public int TypeCode { get; set; }    // 尺寸类型编码 1~5

        // 放置后的坐标 (左下角)
        public double X { get; set; }
        public double Y { get; set; }

        public PackingRectangle(double w, double h, int idx, int typeCode)
        {
            W = w;
            H = h;
            Idx = idx;
            TypeCode = typeCode;
            X = 0;
            Y = 0;
        }

        // 用于算法中复制对象的克隆方法
        public PackingRectangle Clone()
        {
            return new PackingRectangle(W, H, Idx, TypeCode);
        }
    }

    // ==========================================================
    // 2. 核心排样算法 (Python 翻译)
    // ==========================================================

    /// <summary>
    /// 包含天际线排样算法的静态类
    /// </summary>
    public static class PackingAlgorithm
    {
        private const double Epsilon = 1e-6; // 用于浮点数比较的精度

        /// <summary>
        /// 天际线排样核心算法
        /// </summary>
        /// <param name="rects">待排样的矩形列表</param>
        /// <param name="containerWidth">容器宽度</param>
        /// <returns>元组(是否成功, 容器高度, 放置后的矩形列表)</returns>
        public static (bool success, double height, List<PackingRectangle> layout) SkylinePack(IEnumerable<PackingRectangle> rects, double containerWidth)
        {
            // 天际线段定义: (x1, x2, y)
            var skyline = new List<(double x1, double x2, double y)> { (0.0, containerWidth, 0.0) };

            // 创建矩形的深拷贝副本，以防修改原始列表
            var rectsCopy = rects.Select(r => r.Clone()).ToList();

            foreach (var rect in rectsCopy)
            {
                double bestX = -1;
                double bestY = double.PositiveInfinity;
                int bestSegIdx = -1;

                // 寻找最佳放置点
                for (int i = 0; i < skyline.Count; i++)
                {
                    var (x1, x2, y) = skyline[i];
                    if (x2 - x1 + Epsilon >= rect.W)
                    {
                        if (y < bestY - Epsilon)
                        {
                            bestY = y;
                            bestX = x1;
                            bestSegIdx = i;
                        }
                        else if (Math.Abs(y - bestY) < Epsilon && x1 < bestX)
                        {
                            bestX = x1;
                            bestSegIdx = i;
                        }
                    }
                }

                if (bestX < 0)
                {
                    return (false, 0, null); // 找不到合适的位置
                }

                rect.X = bestX;
                rect.Y = bestY;
                double left = bestX;
                double right = bestX + rect.W;

                // 更新天际线
                var newSkyline = new List<(double x1, double x2, double y)>();
                foreach (var (x1, x2, y) in skyline)
                {
                    if (x2 <= left + Epsilon || x1 >= right - Epsilon)
                    {
                        newSkyline.Add((x1, x2, y));
                    }
                    else
                    {
                        if (x1 < left - Epsilon) newSkyline.Add((x1, left, y));
                        if (x2 > right + Epsilon) newSkyline.Add((right, x2, y));
                    }
                }
                newSkyline.Add((left, right, bestY + rect.H));
                newSkyline.Sort((a, b) => a.x1.CompareTo(b.x1));

                // 合并相邻且同高的天际线段
                var mergedSkyline = new List<(double x1, double x2, double y)>();
                if (newSkyline.Count > 0)
                {
                    mergedSkyline.Add(newSkyline[0]);
                    for (int i = 1; i < newSkyline.Count; i++)
                    {
                        var last = mergedSkyline.Last();
                        var current = newSkyline[i];
                        if (Math.Abs(last.x2 - current.x1) < Epsilon && Math.Abs(last.y - current.y) < Epsilon)
                        {
                            mergedSkyline[mergedSkyline.Count - 1] = (last.x1, current.x2, last.y);
                        }
                        else
                        {
                            mergedSkyline.Add(current);
                        }
                    }
                }
                skyline = mergedSkyline;
            }

            double maxHeight = skyline.Count > 0 ? skyline.Max(s => s.y) : 0;
            return (true, maxHeight, rectsCopy);
        }

        /// <summary>
        /// 搜索最优容器宽度以获得最小面积
        /// </summary>
        public static (double bestW, double bestH, List<PackingRectangle> bestLayout) PackWithWidthSearch(
            List<PackingRectangle> rects, double minWidth, double maxWidth, double step)
        {
            double bestArea = double.PositiveInfinity;
            double bestW = 0, bestH = 0;
            List<PackingRectangle> bestLayout = null;

            // 粗略搜索
            for (double w = minWidth; w <= maxWidth; w += step)
            {
                var sortedRects = rects.OrderByDescending(r => r.W * r.H).ThenByDescending(r => r.W).ToList();
                var (ok, h, layout) = SkylinePack(sortedRects, w);

                if (ok)
                {
                    double area = w * h;
                    if (area < bestArea - Epsilon)
                    {
                        bestArea = area;
                        bestW = w;
                        bestH = h;
                        bestLayout = layout;
                    }
                }
            }

            // 精细搜索
            if (bestLayout != null)
            {
                double fineStart = Math.Max(minWidth, bestW - step);
                double fineEnd = Math.Min(maxWidth, bestW + step);
                double fineStep = step / 5.0;

                for (double w = fineStart; w <= fineEnd; w += fineStep)
                {
                    var sortedRects = rects.OrderByDescending(r => r.W * r.H).ThenByDescending(r => r.W).ToList();
                    var (ok, h, layout) = SkylinePack(sortedRects, w);

                    if (ok)
                    {
                        double area = w * h;
                        if (area < bestArea - Epsilon)
                        {
                            bestArea = area;
                            bestW = w;
                            bestH = h;
                            bestLayout = layout;
                        }
                    }
                }
            }

            return (bestW, bestH, bestLayout);
        }
    }
}
