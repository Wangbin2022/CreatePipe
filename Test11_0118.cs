using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.RevitStylePopup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : IExternalCommand
    {
        //0118 新开规则，
        private bool IsHorizontal(Pipe pipe)
        {
            Line line = (pipe.Location as LocationCurve).Curve as Line;
            return Math.Abs(line.Direction.Z) < 0.001; // 允许微小误差
        }
        /// <summary>
        /// 获取指定位置的连接器
        /// </summary>
        private Connector GetConnectorAtPoint(Pipe pipe, XYZ point)
        {
            ConnectorManager cm = pipe.ConnectorManager;
            foreach (Connector c in cm.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(point))
                {
                    return c;
                }
            }
            return null;
        }
        /// <summary>
        /// 计算两条直线在XY平面上的投影交点
        /// </summary>
        private XYZ GetIntersectionPoint2D(Line line1, Line line2)
        {
            // 提取XY平面的坐标方程: P = Origin + t * Direction
            double x1 = line1.Origin.X;
            double y1 = line1.Origin.Y;
            double dx1 = line1.Direction.X;
            double dy1 = line1.Direction.Y;

            double x2 = line2.Origin.X;
            double y2 = line2.Origin.Y;
            double dx2 = line2.Direction.X;
            double dy2 = line2.Direction.Y;

            // 解线性方程组求解交点
            // x = x1 + t1*dx1
            // y = y1 + t1*dy1
            // x = x2 + t2*dx2
            // y = y2 + t2*dy2

            double det = dx1 * dy2 - dy1 * dx2;

            // 如果行列式为0，说明平行
            if (Math.Abs(det) < 0.00001) return null;

            double t1 = ((x2 - x1) * dy2 - (y2 - y1) * dx2) / det;

            // 计算交点坐标 (Z值这里先暂定为0，后续业务逻辑会覆盖)
            return new XYZ(x1 + t1 * dx1, y1 + t1 * dy1, 0);
        }

        //0206 应与PipeJoinHorizon合并考虑
        ///// <summary>
        ///// 从风管管件的连接器出发，获取其“外部相邻”的两个端点连接器（不返回管件自身连接器）。
        ///// 只返回两端连接场景（例如：弯头/直接头/变径等）。三通等会返回 !=2 从而被跳过。
        ///// </summary>
        //private List<Connector> GetTwoEndNeighborConnectors(FamilyInstance fitting)
        //{
        //    var result = new List<Connector>();

        //    if (fitting?.MEPModel?.ConnectorManager == null) return result;

        //    ConnectorSet fitConns = fitting.MEPModel.ConnectorManager.Connectors;
        //    if (fitConns == null) return result;

        //    // 用于去重：同一 owner + connector id
        //    var seen = new HashSet<string>();

        //    foreach (Connector fitConn in fitConns)
        //    {
        //        if (fitConn == null || !fitConn.IsConnected) continue;

        //        foreach (Connector refConn in fitConn.AllRefs)
        //        {
        //            if (refConn == null) continue;

        //            // 排除引用到自己（防御）
        //            if (refConn.Owner != null && refConn.Owner.Id == fitting.Id) continue;

        //            // 只接受 MEP 连接器（End/Curve 等；这里不过度限制 Domain，避免附件/设备遗漏）
        //            if (refConn.Owner == null || refConn.Owner.Category == null) continue;

        //            string key = $"{refConn.Owner.Id.IntegerValue}:{refConn.Id}";
        //            if (seen.Add(key))
        //                result.Add(refConn);
        //        }
        //    }
        //    // 仅两端
        //    return result;
        //}
        ///// <summary>
        ///// 尝试 cFrom.ConnectTo(cTo)，并通过 AllRefs/IsConnected 验证两者是否真正互相引用。
        ///// </summary>
        //private bool TryConnectAndVerify(Connector cFrom, Connector cTo, Document doc)
        //{
        //    if (cFrom == null || cTo == null) return false;

        //    // 先短路：如果已经互相连接，则认为成功
        //    if (IsActuallyConnected(cFrom, cTo)) return true;

        //    // ConnectTo 可能抛异常（距离太远/系统不兼容/几何不满足等）
        //    cFrom.ConnectTo(cTo);
        //    doc.Regenerate();

        //    return IsActuallyConnected(cFrom, cTo);
        //}
        ///// <summary>
        ///// 严格判断：c1 的 AllRefs 中是否包含 c2（ownerId + connectorId 匹配）
        ///// </summary>
        //private bool IsActuallyConnected(Connector c1, Connector c2)
        //{
        //    if (c1 == null || c2 == null) return false;
        //    if (!c1.IsConnected || !c2.IsConnected) return false;

        //    int owner2 = c2.Owner?.Id.IntegerValue ?? -1;
        //    int cid2 = c2.Id;

        //    foreach (Connector r in c1.AllRefs)
        //    {
        //        if (r?.Owner == null) continue;
        //        if (r.Owner.Id.IntegerValue == owner2 && r.Id == cid2)
        //            return true;
        //    }
        //    return false;
        //}
        //private bool IsPhysicallyConnected(Connector c1, Connector c2)
        //{
        //    if (!c1.IsConnected || !c2.IsConnected) return false;

        //    foreach (Connector r in c1.AllRefs)
        //    {
        //        if (r.Owner?.Category?.Id.IntegerValue ==
        //            (int)BuiltInCategory.OST_DuctFitting)
        //            return true;
        //    }
        //    return false;
        //}
        //private bool TryCreateFitting(Connector c1, Connector c2, Document doc)
        //{
        //    if (c1 == null || c2 == null) return false;

        //    try
        //    {
        //        // 如果已经是物理连接，直接返回成功
        //        if (IsPhysicallyConnected(c1, c2))
        //            return true;

        //        // ✅ 强制创建风管管件（弯头/变径/直接头）
        //        FamilyInstance newFitting =
        //            MechanicalUtils.CreateDuctFitting(doc, c1, c2);

        //        doc.Regenerate();

        //        return newFitting != null;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private string SafeCatName(Element owner)
        //{
        //    try { return owner?.Category?.Name ?? owner?.GetType().Name ?? "<null>"; }
        //    catch { return "<unknown>"; }
        //}
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0206 重新连接天圆地方 还是没成功，只能手工替换天圆地方
            //var selIds = uiDoc.Selection.GetElementIds();
            //if (selIds == null || selIds.Count == 0)
            //{
            //    message = "请先在选集中选择风管管件（Duct Fitting）。";
            //    return Result.Failed;
            //}
            //var logs = new List<string>();
            //int ok = 0, skipped = 0, failed = 0;
            //using (var tx = new Transaction(doc, "删除风管管件并重连两端连接器"))
            //{
            //    tx.Start();
            //    foreach (var id in selIds)
            //    {
            //        Element e = doc.GetElement(id);
            //        if (e == null) continue;
            //        // 仅处理风管管件
            //        if (!(e is FamilyInstance fi) ||
            //            fi.Category == null ||
            //            fi.Category.Id.IntegerValue != (int)BuiltInCategory.OST_DuctFitting)
            //        {
            //            continue;
            //        }
            //        using (var st = new SubTransaction(doc))
            //        {
            //            st.Start();
            //            try
            //            {
            //                // 1) 获取该管件两端连接到的“外部连接器”（风管/附件/设备等的连接器）
            //                //    必须在删除管件前取到，因为删除后引用关系会改变
            //                var endConnectors = GetTwoEndNeighborConnectors(fi);
            //                if (endConnectors == null || endConnectors.Count != 2)
            //                {
            //                    skipped++;
            //                    st.RollBack(); // 本次子事务不做任何改变，回滚/不提交都可以；这里统一回滚
            //                    continue;
            //                }
            //                Connector c1 = endConnectors[0];
            //                Connector c2 = endConnectors[1];
            //                // 记录“连接器id”：Revit 的 Connector.Id 是 int（同一 Owner 内唯一）
            //                // 为便于唯一定位，记录 OwnerId + ConnectorId
            //                string c1Key = $"{c1.Owner.Id.IntegerValue}:{c1.Id}";
            //                string c2Key = $"{c2.Owner.Id.IntegerValue}:{c2.Id}";
            //                logs.Add($"Fitting {fi.Id.IntegerValue} -> C1 {c1Key} ({SafeCatName(c1.Owner)}) , C2 {c2Key} ({SafeCatName(c2.Owner)})");
            //                // 2) 删除管件
            //                doc.Delete(fi.Id);
            //                doc.Regenerate();
            //                // 尝试强制生成新管件
            //                bool created = TryCreateFitting(c1, c2, doc);
            //                // 若失败，尝试反向
            //                if (!created)
            //                    created = TryCreateFitting(c2, c1, doc);
            //                if (!created)
            //                    throw new InvalidOperationException("无法生成新的风管管件");
            //                st.Commit();
            //                ok++;
            //            }
            //            catch (Exception ex)
            //            {
            //                failed++;
            //                logs.Add($"Fitting {fi.Id.IntegerValue} FAILED: {ex.GetType().Name} - {ex.Message}");
            //                st.RollBack();
            //            }
            //        }
            //    }
            //    tx.Commit();
            //}
            //TaskDialog.Show("结果",$"成功: {ok}\n跳过(非两端或无法解析): {skipped}\n失败并回滚: {failed}\n" + string.Join("\n", logs.Take(20)) + (logs.Count > 20 ? "\n..." : ""));

            ////0205 查找特定属性风口构建
            //List<FamilyInstance> allInstance = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            //List<FamilyInstance> terminalNames = new List<FamilyInstance>();
            //foreach (var item in allInstance)
            //{
            //    if ((item.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal) && (item.Name == "风道末端_单层百叶风口"))
            //    {
            //        terminalNames.Add(item);
            //    }
            //}
            //List<ElementId> selectedElementIds = new List<ElementId>();
            ////foreach (var item in terminalNames)
            ////{
            ////    try
            ////    {
            ////        Parameter widthParameter = item.LookupParameter("风口宽度");
            ////        Parameter heightParameter = item.LookupParameter("风口高度");
            ////        //if (widthParameter != null && widthParameter.AsDouble() == 600 / 304.8 && heightParameter != null && heightParameter.AsDouble() == 500 / 304.8)
            ////        if (widthParameter != null && widthParameter.AsDouble() == 1000 / 304.8)
            ////        //if (heightParameter != null && heightParameter.AsDouble() == 600 / 304.8)
            ////        {
            ////            selectedElementIds.Add(item.Id);
            ////        }
            ////    }
            ////    catch (Exception)
            ////    {
            ////        throw;
            ////    }
            ////}
            ////TaskDialog.Show("tt", selectedElementIds.Count().ToString());
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in terminalNames)
            //{
            //    selectedElementIds.Add(item.Id);
            //    stringBuilder.Append(item.Id.ToString() + ",");
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString());
            ////uiDoc.Selection.SetElementIds(selectedElementIds);


            ////0204 复制文字属性？直接用导出导入属性试试
            ////需要简单界面，选择要复制到的类型
            ////查找当前族实例所有实例文字属性，复制到已选择的对象
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "请选择第一根水平管道");
            //FamilyInstance instance = doc.GetElement(ref1) as FamilyInstance;
            //TaskDialog.Show("tt", instance.Name);

            //////0131 测试临时窗口。OK
            //string tt = "测试临时窗口";
            //string myMessage = "使用DataTrigger和BooleanAnimation";
            //var toast = new ToastWindow(tt,myMessage);
            //toast.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //toast.Show();
            //var tw = new TestWindow();
            //tw.Show();
            ////////1122 生成交叉中间立管OK
            //// 1. 拾取第一根管道
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第一根水平管道");
            //Pipe pipe1 = doc.GetElement(ref1) as Pipe;
            //// 2. 拾取第二根管道
            //Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第二根水平管道");
            //Pipe pipe2 = doc.GetElement(ref2) as Pipe;
            //// 校验：确保是水平管道 (Z轴方向分量接近0)
            //if (!IsHorizontal(pipe1) || !IsHorizontal(pipe2))
            //{
            //    TaskDialog.Show("错误", "请选择水平管道。");
            //    return Result.Failed;
            //}
            //using (Transaction trans = new Transaction(doc, "生成垂直立管"))
            //{
            //    trans.Start();
            //    // 3. 获取管道的几何中心线
            //    Line line1 = (pipe1.Location as LocationCurve).Curve as Line;
            //    Line line2 = (pipe2.Location as LocationCurve).Curve as Line;
            //    // 4. 计算XY平面上的投影交点 (无限延伸)
            //    XYZ intersectionPoint2D = GetIntersectionPoint2D(line1, line2);
            //    if (intersectionPoint2D == null)
            //    {
            //        TaskDialog.Show("错误", "两根管道在XY平面平行，无法生成垂直连接管。");
            //        return Result.Failed;
            //    }
            //    // 5. 准备创建立管的坐标,获取两根管各自在交点处的Z高度
            //    double z1 = line1.Origin.Z;
            //    double z2 = line2.Origin.Z;
            //    // 容差处理，如果高度极度接近则不需要立管
            //    if (Math.Abs(z1 - z2) < 0.01) // 0.01 feet
            //    {
            //        TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。");
            //        return Result.Cancelled;
            //    }
            //    // 确定立管的底点和顶点
            //    XYZ bottomPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Min(z1, z2));
            //    XYZ topPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Max(z1, z2));
            //    // 6. 创建垂直立管
            //    // 使用第一根管的系统类型和管材类型，以及标高
            //    ElementId systemTypeId = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
            //    ElementId pipeTypeId = pipe1.PipeType.Id;
            //    ElementId levelId = pipe1.ReferenceLevel.Id;
            //    Pipe riserInfo = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, bottomPoint, topPoint);
            //    // 设置立管直径（这里取较小管径或第一根管径，可视需求调整）
            //    // 注意：Diameter是只读属性，需通过参数设置
            //    double diameter = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            //    riserInfo.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            //    // 7. 连接管件 (生成三通/机械三通)
            //    // 需要找到立管的上下连接器
            //    Connector topConnector = GetConnectorAtPoint(riserInfo, topPoint);
            //    Connector bottomConnector = GetConnectorAtPoint(riserInfo, bottomPoint);
            //    // 判断哪个现有管道在上方，哪个在下方
            //    Pipe topPipe = z1 > z2 ? pipe1 : pipe2;
            //    Pipe bottomPipe = z1 > z2 ? pipe2 : pipe1;
            //    // 核心API: NewTakeoffFitting
            //    // 这个方法会在现有管道(pipe)上打断并插入三通，或者插入接头，并连接到指定的connector
            //    //try
            //    //{
            //    //    doc.Create.NewTakeoffFitting(topConnector, topPipe);
            //    //    doc.Create.NewTakeoffFitting(bottomConnector, bottomPipe);
            //    //}
            //    //catch (Exception ex)
            //    //{
            //    //    //TaskDialog.Show("警告", "生成管件失败，可能是没有配置路由首选项或空间不足。" + ex.Message);
            //    //    // 即便管件失败，立管可能已生成，视情况决定是否回滚
            //    //}

            //    trans.Commit();
            //}
            return Result.Succeeded;
        }
    }
}
