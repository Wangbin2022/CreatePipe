using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Document = Autodesk.Revit.DB.Document;

//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : Decorator, IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        //0516 管道改平测试
        private bool ProcessPipe(Document doc, Pipe pipe)
        {
            try
            {
                LocationCurve lc = pipe.Location as LocationCurve;
                if (pipe.IsVertical())
                {
                    TaskDialog.Show("tt", "管道近似立管，压平后长度为0，跳过处理。");
                    return false;
                }
                if (lc == null) return false;
                Line oldLine = lc.Curve as Line;
                if (oldLine == null) return false;
                //找管道所在楼层
                ElementId levelId = pipe.ReferenceLevel.Id;
                Level level = doc.GetElement(levelId) as Level;

                //Level level = GetPipeReferenceLevel(doc, pipe);
                if (level == null)
                {
                    TaskDialog.Show("tt", "无法获取参考楼层。");
                    return false;
                }
                XYZ oldP0 = oldLine.GetEndPoint(0);
                XYZ oldP1 = oldLine.GetEndPoint(1);
                double targetZ = level.Elevation;
                XYZ newP0 = new XYZ(oldP0.X, oldP0.Y, targetZ);
                XYZ newP1 = new XYZ(oldP1.X, oldP1.Y, targetZ);
                //// 记录原端部连接
                //var oldConnectors = GetPipeEndConnectors(pipe);
                //var connectionInfos = new List<ConnectionInfo>();
                //foreach (var pc in oldConnectors)
                //{
                //    ConnectionInfo info = CaptureConnectionInfo(pipe, pc);
                //    connectionInfos.Add(info);
                //}
                //// 先断开原连接，并删除直接相连小管件（可选）
                //foreach (var info in connectionInfos)
                //{
                //    MEPAnalysisExtension.DisconnectAll(info.PipeConnector);
                //    // 如果对方是管件，可按策略删除，以便重新生成连接
                //    if (info.ConnectedOwnerId != ElementId.InvalidElementId)
                //    {
                //        Element owner = doc.GetElement(info.ConnectedOwnerId);
                //        if (owner != null && MEPAnalysisExtension.IsPipeFitting(owner))
                //        {
                //            TryDeleteElement(doc, owner.Id);
                //        }
                //    }
                //}
                // 再次确认管还存在
                pipe = doc.GetElement(pipe.Id) as Pipe;
                if (pipe == null)
                {
                    TaskDialog.Show("tt", "原管道在删除相邻管件后失效。");
                    return false;
                }
                // 强制改为水平线
                Line newLine = Line.CreateBound(newP0, newP1);
                lc.Curve = newLine;
                doc.Regenerate();
                //// 获取修改后的两端连接器
                //var newPipeConnectors = GetPipeEndConnectors(pipe);
                //// 将原连接信息按距离匹配到新的两端
                //var mapping = MatchOldInfosToNewConnectors(connectionInfos, newPipeConnectors);
                //List<string> reconnectLogs = new List<string>();
                //foreach (var pair in mapping)
                //{
                //    ConnectionInfo oldInfo = pair.Key;
                //    Connector newPipeConn = pair.Value;
                //    // 没有外部连接对象，忽略
                //    if (oldInfo.RemoteConnectorOwnerId == ElementId.InvalidElementId)
                //    {
                //        reconnectLogs.Add("端部原无外部连接。");
                //        continue;
                //    }
                //    Element remoteOwner = doc.GetElement(oldInfo.RemoteConnectorOwnerId);
                //    if (remoteOwner == null)
                //    {
                //        reconnectLogs.Add($"外部连接对象已不存在: {oldInfo.RemoteConnectorOwnerId.IntegerValue}");
                //        continue;
                //    }
                //    Connector remoteConn = FindBestConnector(remoteOwner, oldInfo.RemoteConnectorOrigin, oldInfo.RemoteConnectorDirection);
                //    if (remoteConn == null)
                //    {
                //        reconnectLogs.Add($"未找到外部连接器: {remoteOwner.Id.IntegerValue}");
                //        continue;
                //    }
                //    bool connected = TryReconnect(doc, newPipeConn, remoteConn, out string detail);
                //    reconnectLogs.Add(detail);
                //}
                //TaskDialog.Show("tt", "成功压平到标高 " + level.Name + " / Elev=" + targetZ.ToString("F3")
                //    + "；" + string.Join(" | ", reconnectLogs));
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // public static bool TryCreateTeeFitting(
        //Document doc,
        //Connector c1,
        //Connector c2,
        //Connector c3,
        //out string message)
        // {
        //     message = string.Empty;
        //     // ===== 1. 空值校验 =====
        //     if (c1 == null || c2 == null || c3 == null)
        //     {
        //         message = "Connector 不能为空";
        //         return false;
        //     }
        //     // ===== 2. Domain 校验 =====
        //     if (c1.Domain != c2.Domain || c2.Domain != c3.Domain)
        //     {
        //         message = "三个 Connector 的 Domain 不一致";
        //         return false;
        //     }
        //     if (c1.Domain != Domain.DomainPiping &&
        //         c1.Domain != Domain.DomainDuct)
        //     {
        //         message = "仅支持 Piping 或 Duct 三通";
        //         return false;
        //     }
        //     // ===== 3. Owner 校验 =====
        //     Element e1 = c1.Owner;
        //     Element e2 = c2.Owner;
        //     Element e3 = c3.Owner;
        //     if (e1.Id == e2.Id || e1.Id == e3.Id || e2.Id == e3.Id)
        //     {
        //         message = "三个 Connector 不能来自同一个 Element";
        //         return false;
        //     }
        //     // ===== 4. 几何位置校验 =====
        //     XYZ p1 = c1.Origin;
        //     XYZ p2 = c2.Origin;
        //     XYZ p3 = c3.Origin;
        //     if (!p1.IsAlmostEqualTo(p2) ||
        //         !p2.IsAlmostEqualTo(p3))
        //     {
        //         message = "三个 Connector 的 Origin 不重合";
        //         return false;
        //     }
        //     // ===== 5. 角度校验（支管 ≈ 90°）=====
        //     XYZ dirMain = (c1.CoordinateSystem.BasisZ).Normalize();
        //     XYZ dirBranch = (c3.CoordinateSystem.BasisZ).Normalize();
        //     double angle = dirMain.AngleTo(dirBranch);
        //     double angleDeg = angle * 180 / Math.PI;
        //     if (Math.Abs(angleDeg - 90.0) > 2.0)
        //     {
        //         message = $"支管角度 {angleDeg:F2}°，非 90°（容差 ±2°）";
        //         return false;
        //     }
        //     // ===== 6. Routing Preference 校验 =====
        //     try
        //     {
        //         using (Transaction tx = new Transaction(doc, "Create Tee Fitting"))
        //         {
        //             tx.Start();
        //             FamilyInstance tee =
        //                 doc.Create.NewTeeFitting(c1, c2, c3);
        //             if (tee == null)
        //             {
        //                 message = "NewTeeFitting 返回 null";
        //                 tx.RollBack();
        //                 return false;
        //             }
        //             tx.Commit();
        //         }
        //     }
        //     catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
        //     {
        //         message = "Routing Preference 中未配置 Tee Fitting 族：" + ex.Message;
        //         return false;
        //     }
        //     catch (Exception ex)
        //     {
        //         message = "创建三通失败：" + ex.Message;
        //         return false;
        //     }
        //     message = "三通创建成功";
        //     return true;
        // }
        //bool CanCreateCross(Connector c1, Connector c2, Connector c3, Connector c4)
        //{
        //    var cs = new[] { c1, c2, c3, c4 };    
        //    // 1. 同域（Domain）——均为 HVAC 或 Piping    var domain = cs[0].Domain;    if (cs.Any(c => c.Domain != domain)) return false;    
        //    // 2. 所有者是 Duct / Pipe / FlexDuct / FlexPipe，且不能来自同一 Element    var owners = cs.Select(c => c.Owner.Id).ToList();    if (owners.Distinct().Count() < 4) return false;    
        //    // 3. 四个 Connector 坐标原点相同（容差）    XYZ pt = cs[0].Origin;    if (cs.Any(c => c.Origin.DistanceTo(pt) > 1e-6)) return false;    
        //    // 4. 两根主管平行，两根侧管平行，且主⊥侧（近似90°）    //    取 owner 的 LocationCurve.Direction 判断    //    (此处略写，AngleTo < 1e-6 判平行，夹角≈90°)    
        //    // 5. 系统类型一致 cs[i].SystemType 相同    return true;}

        //    try { using (Transaction tr = new Transaction(doc, "Create Cross")) { tr.Start(); var fi = doc.Create.NewCrossFitting(connMain1, connMain2, connSide1, connSide2); tr.Commit(); } }
        //    catch (ArgumentException ex)
        //    {    // Connector 不满足要求（同元素、不同域等）}catch (InvalidOperationException ex){    // 无法插入四通——无四通族 / 方向不对 / 不在交点}
        //    }
        //}

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0629 水平三管道生成三通,四管道生成四通
            try
            {
                //// 水平四通生成
                var refs = uiDoc.Selection.PickObjects(ObjectType.Element, new filterMEPCurveClass(), "请选择四根水平管道（两两共线且互相垂直）");
                if (refs == null || refs.Count != 4)
                {
                    return Result.Cancelled;
                }
                List<MEPCurve> allPipes = refs.Select(r => doc.GetElement(r) as MEPCurve).ToList();
                // ── 1. 基础校验：全部水平且在同一Z平面 ────────────────────────
                foreach (var p in allPipes)
                {
                    if (!p.IsHorizontal())
                    {
                        TaskDialog.Show("校验失败", $"管道 [{p.Id}] 不是水平管道，请重新选择。");
                        return Result.Failed;
                    }
                }
                ////检查三管是否在同一平面，不符合则退出
                //// 取各管中心点Z值
                double[] zValues = allPipes
                    .Select(p => ((p.Location as LocationCurve).Curve.GetEndPoint(0).Z
                                + (p.Location as LocationCurve).Curve.GetEndPoint(1).Z) / 2.0)
                    .ToArray();
                if (zValues.Max() - zValues.Min() > 0.1 / 304.8)
                {
                    TaskDialog.Show("校验失败", "四根管道不在同一水平面，请选择同标高的管道。");
                    return Result.Failed;
                }

                // ── 2. 几何分析：找出两对共线的管道 ───────────────────────────
                List<MEPCurve> pair1, pair2;
                if (!MEPAnalysisExtension.TryFindColinearPairs(allPipes, out pair1, out pair2))
                {
                    TaskDialog.Show("校验失败", "未找到两对共线的管道。请确保选择的管道是两两共线的。");
                    return Result.Failed;
                }

                // ── 3. 垂直校验：检查两对管道是否互相垂直 ─────────────────────
                XYZ dir1 = MEPAnalysisExtension.GetMEPCurveDirection(pair1[0]);
                XYZ dir2 = MEPAnalysisExtension.GetMEPCurveDirection(pair2[0]);

                if (!MEPAnalysisExtension.AreDirectionsPerpendicular(dir1, dir2))
                {
                    TaskDialog.Show("校验失败", "找到的两对管道不互相垂直，无法生成四通。");
                    return Result.Failed;
                }

                // ── 3a. 【新增】根据管径大小，确定主管路和支管路 ─────────────
                List<MEPCurve> mainPipes;
                List<MEPCurve> branchPipes;

                // 获取每一对管道中的最大管径
                double sizePair1 = Math.Max(MEPAnalysisExtension.GetMEPCurveMainSize(pair1[0]), MEPAnalysisExtension.GetMEPCurveMainSize(pair1[1]));
                double sizePair2 = Math.Max(MEPAnalysisExtension.GetMEPCurveMainSize(pair2[0]), MEPAnalysisExtension.GetMEPCurveMainSize(pair2[1]));

                // 管径大的作为主管路
                if (sizePair1 >= sizePair2)
                {
                    mainPipes = pair1;
                    branchPipes = pair2;
                }
                else
                {
                    mainPipes = pair2;
                    branchPipes = pair1;
                }

                // ── 4. 计算交点并校验 ─────────────────────────────────────
                // 使用主管路和支管路的一根管来计算交点
                XYZ intersection = MEPAnalysisExtension.GetMEPCurveAxesIntersectionXY(mainPipes[0], branchPipes[0]);
                if (intersection == null)
                {
                    TaskDialog.Show("错误", "无法计算管道轴线交点。");
                    return Result.Failed;
                }
                intersection = new XYZ(intersection.X, intersection.Y, MEPAnalysisExtension.GetMEPCurveZ(mainPipes[0]));
                // ── 4. 计算交点并校验 ─────────────────────────────────────
                // 从每对中取一根管道计算交点
                //XYZ intersection = MEPAnalysisExtension.GetMEPCurveAxesIntersectionXY(pair1[0], pair2[0]);
                if (intersection == null)
                {
                    // 理论上垂直非平行的直线必有交点，此检查为保险
                    TaskDialog.Show("错误", "无法计算管道轴线交点。");
                    return Result.Failed;
                }
                // 统一Z坐标
                intersection = new XYZ(intersection.X, intersection.Y, MEPAnalysisExtension.GetMEPCurveZ(pair1[0]));

                // 检查交点处是否已存在管件
                if (MEPAnalysisExtension.IsFittingExistAtPoint(doc, intersection))
                {
                    TaskDialog.Show("校验失败", "交点处已存在管件，请检查。");
                    return Result.Failed;
                }

                // ── 5. 执行生成 ──────────────────────────────────────────
                using (var trans = new Transaction(doc, "四管生成四通"))
                {
                    trans.Start();
                    // 5a. 按 主管->支管 顺序，将四根管道的最近端点移动到交点
                    MEPCurve mainPipe1 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, mainPipes[0], intersection);
                    MEPCurve mainPipe2 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, mainPipes[1], intersection);
                    MEPCurve branchPipe1 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, branchPipes[0], intersection);
                    MEPCurve branchPipe2 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, branchPipes[1], intersection);

                    // 5b. 按 主管1, 主管2, 支管1, 支管2 的顺序获取连接器
                    Connector connMain1 = mainPipe1.GetClosestConnector(intersection);
                    Connector connMain2 = mainPipe2.GetClosestConnector(intersection);
                    Connector connBranch1 = branchPipe1.GetClosestConnector(intersection);
                    Connector connBranch2 = branchPipe2.GetClosestConnector(intersection);

                    if (connMain1 == null || connMain2 == null || connBranch1 == null || connBranch2 == null)
                    {
                        TaskDialog.Show("错误", "未能成功获取所有管道在交点处的连接器。");
                        trans.RollBack();
                        return Result.Failed;
                    }

                    // 5c. 按正确的优先级创建四通管件
                    try
                    {
                        // API 调用顺序：主管连接器1, 主管连接器2, 支管连接器1, 支管连接器2
                        FamilyInstance crossFitting = doc.Create.NewCrossFitting(connMain1, connMain2, connBranch1, connBranch2);
                    }
                    catch (Exception creationEx)
                    {
                        TaskDialog.Show("创建失败", "无法创建四通管件，请检查是否载入了合适的管件族，或者管径是否匹配。\n\n" + creationEx.Message);
                        trans.RollBack();
                        return Result.Failed;
                    }

                    //// 5a. 将四根管道的最近端点全部移动到交点
                    //MEPCurve pipe1 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, pair1[0], intersection);
                    //MEPCurve pipe2 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, pair1[1], intersection);
                    //MEPCurve pipe3 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, pair2[0], intersection);
                    //MEPCurve pipe4 = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, pair2[1], intersection);
                    //// 5b. 获取四根管道在交点处的连接器
                    //Connector conn1 = pipe1.GetClosestConnector(intersection);
                    //Connector conn2 = pipe2.GetClosestConnector(intersection);
                    //Connector conn3 = pipe3.GetClosestConnector(intersection);
                    //Connector conn4 = pipe4.GetClosestConnector(intersection);
                    //if (conn1 == null || conn2 == null || conn3 == null || conn4 == null)
                    //{
                    //    TaskDialog.Show("错误", "未能成功获取所有管道在交点处的连接器。");
                    //    trans.RollBack();
                    //    return Result.Failed;
                    //}
                    //// 5c. 创建四通管件
                    //// 注意：此方法能否成功，取决于项目中是否载入了能匹配这四个连接器尺寸和角度的四通族。
                    //try
                    //{
                    //    FamilyInstance crossFitting = doc.Create.NewCrossFitting(conn1, conn2, conn3, conn4);
                    //}
                    //catch (Exception creationEx)
                    //{
                    //    TaskDialog.Show("创建失败", "无法创建四通管件，请检查是否载入了合适的管件族，或者管径是否匹配。\n\n" + creationEx.Message);
                    //    trans.RollBack();
                    //    return Result.Failed;
                    //}
                    trans.Commit();
                }


                //// 水平三通生成
                //var ref1 = uiDoc.Selection.PickObjects(ObjectType.Element, new filterMEPCurveClass(), "请选择水平管道");
                //if (ref1 == null || ref1.Count != 3)
                //{
                //    return Result.Cancelled;
                //}
                //List<MEPCurve> pipes = ref1.Select(r => doc.GetElement(r) as MEPCurve).ToList();
                ////检查三管是否水平
                //foreach (var p in pipes)
                //{
                //    if (!p.IsHorizontal())
                //    {
                //        TaskDialog.Show("校验失败", $"管道 [{p.Id}] 不是水平管道，请重新选择。");
                //        return Result.Failed;
                //    }
                //}
                ////检查三管是否在同一平面，不符合则退出
                //// 取各管中心点Z值
                //double[] zValues = pipes
                //    .Select(p => ((p.Location as LocationCurve).Curve.GetEndPoint(0).Z
                //                + (p.Location as LocationCurve).Curve.GetEndPoint(1).Z) / 2.0)
                //    .ToArray();
                //if (zValues.Max() - zValues.Min() > 0.1 / 304.8)
                //{
                //    TaskDialog.Show("校验失败", "三根管道不在同一水平面，请选择同标高的管道。");
                //    return Result.Failed;
                //}
                ////检查三管中是否两管共线，不符合则退出，符合则以这两管为主管，另一根为旁管
                //// 共线判断：方向平行 + 任意一端点到另一管轴线的距离 < 容差
                //// ── 3. 找出两根共线的主管和一根旁管 ─────────────────────────────────
                //// 共线判断：方向平行 + 任意一端点到另一管轴线的距离 < 容差
                //MEPCurve mainA = null, mainB = null, branch = null;
                //bool foundColinear = false;
                //// 遍历三种两两组合
                //(int, int, int)[] combos = { (0, 1, 2), (0, 2, 1), (1, 2, 0) };
                //foreach (var (i, j, k) in combos)
                //{
                //    if (MEPAnalysisExtension.AreMEPCurvesColinear(pipes[i], pipes[j]))
                //    {
                //        mainA = pipes[i];
                //        mainB = pipes[j];
                //        branch = pipes[k];
                //        foundColinear = true;
                //        break;
                //    }
                //}
                //if (!foundColinear)
                //{
                //    TaskDialog.Show("校验失败", "未找到两根共线的主管，请确保两根主管在同一直线上。");
                //    return Result.Failed;
                //}
                //// ── 4. 检查三管是否全部平行（共线已排除，这里排除旁管与主管平行）──
                //XYZ mainDir = MEPAnalysisExtension.GetMEPCurveDirection(mainA);
                //XYZ branchDir = MEPAnalysisExtension.GetMEPCurveDirection(branch);
                //if (MEPAnalysisExtension.AreDirectionsParallel(mainDir, branchDir))
                //{
                //    TaskDialog.Show("校验失败", "旁管与主管平行，无法生成三通，请重新选择。");
                //    return Result.Failed;
                //}
                //// ── 5. 检查旁管与主管是否垂直（允许1度容差）────────────────────────
                //double angleDeg = Math.Abs(mainDir.AngleTo(branchDir) * 180.0 / Math.PI);
                //// AngleTo 返回 0~180，垂直时为90
                //double deviation = Math.Abs(angleDeg - 90.0);
                //if (deviation > 1.0)
                //{
                //    TaskDialog.Show("校验失败",
                //        $"旁管与主管夹角为 {angleDeg:F1}°，不满足垂直条件（允许误差±1°），请重新选择。");
                //    return Result.Failed;
                //}
                //// ── 6. 求旁管轴线与主管轴线在水平面上的交点 ──────────────────────
                //XYZ intersection = MEPAnalysisExtension.GetMEPCurveAxesIntersectionXY(mainA, branch);
                //if (intersection == null)
                //{
                //    TaskDialog.Show("校验失败", "主管轴线与旁管轴线无法求得交点，请检查管道位置。");
                //    return Result.Failed;
                //}
                //// 统一Z坐标（用主管Z）
                //double mainZ = (mainA.Location as LocationCurve).Curve.GetEndPoint(0).Z;
                //intersection = new XYZ(intersection.X, intersection.Y, mainZ);
                //////此处还应补充是否交点已存在管件
                //// 获取所有管道管件
                //List<FamilyInstance> existingFittings = new FilteredElementCollector(doc)
                //    .OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_PipeFitting).Cast<FamilyInstance>()
                //    .Where(fi =>
                //    {
                //        // 安全转换：管件的位置通常是 LocationPoint
                //        if (!(fi.Location is LocationPoint locPoint)) return false;
                //        XYZ fittingPoint = locPoint.Point;
                //        // 比较坐标（使用容差，避免浮点精度问题）
                //        return MEPAnalysisExtension.IsSamePoint(fittingPoint, intersection);
                //    }).ToList();
                //// 判断是否存在
                //if (existingFittings.Count > 0)
                //{
                //    TaskDialog.Show("校验失败", "可能交点已存在构件，请检查。");
                //    return Result.Failed;
                //}
                ////// ── 7. 校验交点在旁管的延伸范围内（不能打在旁管外太远）─────────────
                ////Curve branchCurve = (branch.Location as LocationCurve).Curve;
                ////XYZ branchMid = (branchCurve.GetEndPoint(0) + branchCurve.GetEndPoint(1)) / 2.0;
                ////double branchHalfLen = branchCurve.Length / 2.0;
                ////double distBranchToIntersect = branchCurve
                ////    .Project(new XYZ(intersection.X, intersection.Y, branchCurve.GetEndPoint(0).Z))
                ////    .Distance;
                ////if (distBranchToIntersect > 1.0 / 304.8) // 交点必须在旁管轴线上
                ////{
                ////    TaskDialog.Show("校验失败", "交点不在旁管轴线上，请检查管道是否真正垂直相交。");
                ////    return Result.Failed;
                ////}
                //// ── 8. 打断主管并生成三通 ─────────────────────────────────────────
                //using (var trans = new Transaction(doc, "三管生成三通"))
                //{
                //    trans.Start();
                //    //移动两主管
                //    MEPCurve mainMPECurveA = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, mainA, intersection);
                //    MEPCurve mainMPECurveB = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, mainB, intersection);
                //    //找主管两侧与交叉点接近连接器
                //    Connector connMain1 = mainMPECurveA.GetClosestConnector(intersection);
                //    Connector connMain2 = mainMPECurveB.GetClosestConnector(intersection);
                //    //// 8a. 在交点处打断主管A（较长的那根，或直接打断含有交点的那根）
                //    ////     先确定交点落在哪根主管的范围内
                //    //Pipe mainContaining = MEPAnalysisExtension.GetPipeContainingPoint(mainA, mainB, intersection);
                //    //if (mainContaining == null)
                //    //{
                //    //    TaskDialog.Show("校验失败", "交点不在任何一根主管的范围内（含端点延伸容差），请检查管段是否足够长。");
                //    //    trans.RollBack();
                //    //    return Result.Failed;
                //    //}
                //    //// 8b. 在交点处打断主管，得到第二段
                //    //MEPCurve splitResult = MEPAnalysisExtension.BreakMEPCurveByOne(mainContaining, intersection);
                //    //// splitResult 是打断后新生成的那一段
                //    //// mainContaining 是被缩短的那一段
                //    //// 取这两段在交点处的 Connector
                //    //Connector connMain1 = mainContaining.GetClosestConnector(intersection);
                //    //Connector connMain2 = splitResult.GetClosestConnector(intersection);
                //    // 8c. 旁管在交点处的 Connector
                //    //     旁管需要延伸或裁剪到交点
                //    //     先移动旁管端点到交点（打断旁管在交点处）
                //    MEPCurve branchAtIntersect = MEPAnalysisExtension.ExtendOrTrimMEPCurveToPoint(doc, branch, intersection);
                //    Connector connBranch = branchAtIntersect?.GetClosestConnector(intersection)
                //                           ?? branch.GetClosestConnector(intersection);
                //    // 8d. 生成三通
                //    //     NewTeeFitting 需要三个 Connector
                //    FamilyInstance tee = doc.Create.NewTeeFitting(connMain1, connMain2, connBranch);
                //    //doc.Create.NewCrossFitting();
                //    trans.Commit();
                //}
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户按了 ESC 键取消操作
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", ex.Message);
                return Result.Cancelled;
            }


            //////0222 批量改标高1500.OK 待深化
            //var selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds.Count > 1)
            //{
            //    return Result.Cancelled;
            //}
            //try
            //{
            //    UniversalNewString subView = new UniversalNewString("请输入要调整到距该层标高多高,默认为0");
            //    if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            //    {
            //        TaskDialog.Show("tt", "输入属性遇到错误，请重试");
            //        return Result.Cancelled;
            //    }
            //    double paraNum = vm.NewNum;
            //    TaskDialog.Show("tt", paraNum.ToString("F2"));
            //    //if (selectedIds.Count == 0)
            //    //{
            //    //    Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择一根水平管道");
            //    //    Pipe pipe1 = doc.GetElement(ref1) as Pipe;
            //    //    Parameter sysParam = pipe1.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            //    //    if (sysParam == null || !sysParam.HasValue)
            //    //    {
            //    //        TaskDialog.Show("提示", "未获取到系统参数");
            //    //        return Result.Cancelled;
            //    //    }
            //    //    using (Transaction tx = new Transaction(doc, "更改标高"))
            //    //    {
            //    //        tx.Start();
            //    //        sysParam.Set(1500 / 304.8);
            //    //        tx.Commit();
            //    //    }
            //    //}
            //    //else
            //    //{
            //    //    Pipe pipe = doc.GetElement(selectedIds.FirstOrDefault()) as Pipe;
            //    //    Parameter sysParam = pipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            //    //    if (sysParam == null || !sysParam.HasValue)
            //    //    {
            //    //        TaskDialog.Show("提示", "未获取到系统参数");
            //    //        return Result.Cancelled;
            //    //    }
            //    //    using (Transaction tx = new Transaction(doc, "更改标高"))
            //    //    {
            //    //        tx.Start();
            //    //        sysParam.Set(1500 / 304.8);
            //    //        tx.Commit();
            //    //    }
            //    //}
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户按了 ESC 键取消操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", ex.Message);
            //    return Result.Cancelled;
            //}
            //例程结束
            ////0516测试 管道改平改0 后续应节合instance尝试按指定高度放置，可能更实用 应把与立管之间弯头都删掉       
            //var selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds == null || selectedIds.Count == 0)
            //{
            //    TaskDialog.Show("提示", "请先选择管道。");
            //    return Result.Cancelled;
            //}
            //List<Pipe> pipes = selectedIds.Select(id => doc.GetElement(id)).OfType<Pipe>().ToList();
            //if (pipes.Count == 0)
            //{
            //    TaskDialog.Show("提示", "未选择任何管道。");
            //    return Result.Cancelled;
            //}
            //int success = 0;
            //int fail = 0;
            //List<string> logs = new List<string>();
            //using (TransactionGroup tg = new TransactionGroup(doc, "强制压平管道到楼层标高"))
            //{
            //    tg.Start();
            //    foreach (var pipe in pipes)
            //    {
            //        using (Transaction tx = new Transaction(doc, $"处理管道 {pipe.Id.IntegerValue}"))
            //        {
            //            tx.Start();
            //            try
            //            {
            //                string log;
            //                bool ok = ProcessPipe(doc, pipe);
            //                if (ok)
            //                {
            //                    tx.Commit();
            //                    success++;
            //                }
            //                else
            //                {
            //                    tx.RollBack();
            //                    fail++;
            //                }
            //                logs.Add($"管道 {pipe.Id.IntegerValue}");
            //            }
            //            catch (Exception ex)
            //            {
            //                tx.RollBack();
            //                fail++;
            //                logs.Add($"管道 {pipe.Id.IntegerValue}: 失败 - {ex.Message}");
            //            }
            //        }
            //    }
            //    tg.Assimilate();
            //}
            //string result = $"完成。\n成功：{success}\n失败：{fail}";
            //if (logs.Count > 0)
            //{
            //    result += "\n\n处理详情：\n" + string.Join("\n", logs.Take(50));
            //    if (logs.Count > 50)
            //        result += $"\n...其余 {logs.Count - 50} 条省略";
            //}
            //TaskDialog.Show("结果", result);
            //例程结束
            ////////1207 风口清理和连接
            //try
            //{
            //    // 1. 选择风口
            //    using (Transaction trans = new Transaction(doc, "修改风管系统"))
            //    {
            //        trans.Start();
            //        //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new AirTerminalSelectionFilter(), "请选择一个风口");
            //        //Element terminal = doc.GetElement(reference);
            //        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //        if (selectedIds == null || selectedIds.Count == 0)
            //        {
            //            TaskDialog.Show("错误", "未选择任意");
            //            return Result.Failed;
            //        }
            //        List<Element> ductTerminals = new List<Element>();
            //        foreach (var id in selectedIds)
            //        {
            //            Element element = doc.GetElement(id);
            //            if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal)
            //            {
            //                ductTerminals.Add(element);
            //            }
            //        }
            //        if (ductTerminals == null)
            //        {
            //            TaskDialog.Show("错误", "未选择风口");
            //            return Result.Failed;
            //        }
            //        foreach (var item in ductTerminals)
            //        {
            //            // 2. 获取风口的所有连接器
            //            List<Connector> connectors = GetConnectors(item);
            //            if (connectors.Count == 0)
            //            {
            //                TaskDialog.Show("提示", "该风口没有连接器");
            //                return Result.Failed;
            //            }
            //            // 3. 获取所有相连的管件和风管
            //            List<ElementId> connectedElements = GetAllConnectedElements(connectors, doc);
            //            // 4. 删除所有相连的管件和风管
            //            DeleteConnectedElements(doc, connectedElements);
            //            // 5. 设置风口高度
            //            SetTerminalHeight(item, 3000);
            //        }
            //        trans.Commit();
            //        //TaskDialog.Show("完成",$"已删除 {connectedElements.Count} 个相连元素，并将风口高度设置为4000mm");
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}      

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

            //////1029 管道属性批量填写,系统族批量可参考.OK
            //using (Transaction tx = new Transaction(doc, "管道属性批写入"))
            //{
            //    tx.Start();
            //    try
            //    {
            //        List<Pipe> allPipesInModel = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
            //        foreach (var pipe in allPipesInModel)
            //        {
            //            //TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
            //            double diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;
            //            double length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() * 304.8;
            //            // 参数配置字典
            //            var parameterConfigs = new Dictionary<string, string>
            //            {
            //                { "尺寸规格", $"DN{(int)diameter}" },
            //                { "直径", $"DN{(int)diameter}" },
            //                { "材质1", "钢管" },
            //                { "压力等级", "1.6MPa" },
            //                { "长度", $"{(int)length}mm" },
            //                { "系统类型", "喷淋" },
            //                { "坡度", "0" },
            //                { "保温材料", "柔性泡沫橡塑管壳" },
            //                { "保温厚度", "55mm" }
            //            };
            //            foreach (var config in parameterConfigs)
            //            {
            //                Parameter param = pipe.LookupParameter(config.Key);
            //                param?.Set(config.Value);
            //            }
            //            //简化前代码
            //            //Parameter parameter1 = item.LookupParameter("尺寸规格");
            //            //if (parameter1 != null)
            //            //{
            //            //    parameter1.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
            //            //}
            //            //Parameter parameter2 = item.LookupParameter("直径");
            //            //if (parameter2 != null)
            //            //{
            //            //    parameter2.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
            //            //}
            //        }
            //        //////属性测试
            //        ////Pipe item = doc.GetElement(uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterPipe()).ElementId) as Pipe;
            //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
            //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8).ToString("F0"));
            //        ////TaskDialog.Show("tt", ((int)((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8)).ToString());
            //        //TaskDialog.Show("tt", item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString());
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //    tx.Commit();
            //}
            ////例程结束

            ////0511 找轴线交叉点坐标测试
            //// 2. 收集所有轴线 (Grid) 图元
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //IList<Element> grids = collector.OfClass(typeof(Autodesk.Revit.DB.Grid))
            //    .WhereElementIsNotElementType().ToElements();
            //if (grids.Count < 2)
            //{
            //    TaskDialog.Show("提示", "当前文档中轴线数量不足，无法计算交点。");
            //    return Result.Failed;
            //}
            //// 提取所有轴线的二维无限长直线 (过滤掉弧形轴线)
            //List<Line> infiniteFlatLines = new List<Line>();
            //foreach (Autodesk.Revit.DB.Grid grid in grids)
            //{
            //    Curve curve = grid.Curve;
            //    // 目前仅处理直线型轴线，如果是弧形轴线(Arc)需要另外的数学逻辑
            //    if (curve is Line line)
            //    {
            //        // 提取起点和方向，强制 Z 坐标为 0，拍平到同一个二维平面
            //        XYZ originFlat = new XYZ(line.Origin.X, line.Origin.Y, 0);
            //        XYZ directionFlat = new XYZ(line.Direction.X, line.Direction.Y, 0).Normalize();
            //        // 创建无界(无限长)的直线
            //        Line unboundLine = Line.CreateUnbound(originFlat, directionFlat);
            //        infiniteFlatLines.Add(unboundLine);
            //    }
            //}
            //// 4. 存储所有交点 (使用 List 并在存入前去重，防止极近的点重复)
            //List<XYZ> intersectionPoints = new List<XYZ>();
            //// 5. 双重循环计算交点 (两两组合比对，避免自己和自己比，也避免重复比对)
            //for (int i = 0; i < infiniteFlatLines.Count; i++)
            //{
            //    for (int j = i + 1; j < infiniteFlatLines.Count; j++)
            //    {
            //        Line line1 = infiniteFlatLines[i];
            //        Line line2 = infiniteFlatLines[j];
            //        // 检查两条线是否平行 (方向向量的叉乘接近于0)
            //        XYZ crossProduct = line1.Direction.CrossProduct(line2.Direction);
            //        if (crossProduct.GetLength() < 1e-6)
            //        {
            //            continue; // 平行或重合，无唯一交点，跳过
            //        }
            //        IntersectionResultArray intersections;
            //        SetComparisonResult result = line1.Intersect(line2, out intersections);

            //        if (result == SetComparisonResult.Overlap && intersections != null)
            //        {
            //            foreach (IntersectionResult iResult in intersections)
            //            {
            //                XYZ point = iResult.XYZPoint;

            //                // 去重机制：检查是否已经存在非常接近的交点 (容差设为 0.001 英尺)
            //                if (!intersectionPoints.Any(p => p.DistanceTo(point) < 0.001))
            //                {
            //                    intersectionPoints.Add(point);
            //                }
            //            }
            //        }
            //    }
            //}
            //for (int i = 0; i < intersectionPoints.Count; i++)
            //{
            //    XYZ p = intersectionPoints[i];
            //    if (Math.Round(p.X * 304.8, 4)==0&& Math.Round(p.Y * 304.8, 4)==0)
            //    {
            //        TaskDialog.Show("tt", "轴网交点与项目基点有交叉");
            //    }
            //}
            ////// 6. 输出结果到文件
            ////string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            ////string outputPath = Path.Combine(desktopPath, "GridIntersections.txt");
            ////using (StreamWriter writer = new StreamWriter(outputPath))
            ////{
            ////    writer.WriteLine($"轴线交点坐标列表 (共 {intersectionPoints.Count} 个点):");
            ////    writer.WriteLine("=========================================");
            ////    writer.WriteLine("单位转换为mm");
            ////    for (int i = 0; i < intersectionPoints.Count; i++)
            ////    {
            ////        XYZ p = intersectionPoints[i];
            ////        writer.WriteLine($"点 {i + 1}: ({Math.Round(p.X * 304.8, 4)}, {Math.Round(p.Y * 304.8, 4)}, {Math.Round(p.Z * 304.8, 4)})");
            ////    }
            ////}
            ////TaskDialog.Show("完成", $"成功找到 {intersectionPoints.Count} 个轴线交点，已保存至：\n{outputPath}");


            ////0508 找未赋值深圳墙
            //var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().Cast<Wall>().ToList();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in walls)
            //{
            //    if (item.LookupParameter("深圳构件标识") == null)
            //    {
            //        ids.Add(item.Id);
            //    }
            //}
            //TaskDialog.Show("tt", ids.Count.ToString());

            //0426 文本框输入数字限定测试
            //TestWindow testWindow = new TestWindow(uiApp);
            //testWindow.ShowDialog();   

            ////0426 生成柱测试改，先基于通用combobox确定柱样式，需要在平面操作自动确定柱上下偏移。再确定圆柱或方柱（暂不考虑旋转角度）
            ////结构族SectionShape参数 symbol参数int值表示
            //var column = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new ColumnFilter(), "findA柱子")) as FamilyInstance;
            //TaskDialog.Show("tt", column.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_SHAPE).AsInteger().ToString());

            //////1102 结构柱翻模测试改造 https://zhuanlan.zhihu.com/p/108750783
            /////改为按标高打断管线,需要增加高度获取和。OK
            //////创建应用程序对象
            //try
            //{
            //    //开始事务
            //    using (Autodesk.Revit.DB.Transaction ts = new Autodesk.Revit.DB.Transaction(doc, "柱子翻模"))
            //    {
            //        ts.Start();
            //        Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement); //获取对象
            //        string ss = r.ConvertToStableRepresentation(doc); //转化为字符串
            //        Element elem = doc.GetElement(r);
            //        // 获取几何图元
            //        GeometryElement geoElem = elem.get_Geometry(new Options());
            //        GeometryObject geoObj = elem.GetGeometryObjectFromReference(r);
            //        //获取选中的cad图层
            //        Category targetCategory = null;
            //        ElementId graphicsStyleId = ElementId.InvalidElementId;
            //        //判断所选取的几何对象样式不为元素无效值
            //        if (geoObj != null && geoObj.GraphicsStyleId != ElementId.InvalidElementId)
            //        {
            //            graphicsStyleId = geoObj.GraphicsStyleId;
            //            GraphicsStyle gs = doc.GetElement(geoObj.GraphicsStyleId) as GraphicsStyle; //获得所选对象图形样式
            //            if (gs != null)
            //            {
            //                //图层及图层名字
            //                targetCategory = gs.GraphicsStyleCategory;
            //                string layerName = gs.GraphicsStyleCategory.Name;
            //            }
            //            double offsetHeight = 2000 / 304.8;
            //            ////隐藏选中的cad图层
            //            if (targetCategory != null)
            //            {
            //                //doc.ActiveView.SetCategoryHidden(targetCategory.Id, true);
            //            }
            //            CurveArray curveArray = new CurveArray();
            //            List<double> listdb = new List<double>();
            //            foreach (var gObj in geoElem)
            //            {
            //                GeometryInstance geomInstance = gObj as GeometryInstance;
            //                if (geomInstance != null)
            //                {
            //                    //坐标转换
            //                    Transform transform = geomInstance.Transform;
            //                    //TaskDialog.Show("tt", geomInstance.SymbolGeometry.Count().ToString());
            //                    //坐标空间
            //                    foreach (var insObj in geomInstance.SymbolGeometry)
            //                    {
            //                        if (insObj == null) continue;
            //                        // 检查图形样式ID是否匹配
            //                        if (insObj.GraphicsStyleId != graphicsStyleId)
            //                            continue;
            //                        //线类型 - 处理PolyLine
            //                        if (insObj is PolyLine polyLine)
            //                        {
            //                            //获取坐标点
            //                            IList<XYZ> points = polyLine.GetCoordinates();
            //                            XYZ pMax = polyLine.GetOutline().MaximumPoint;
            //                            XYZ pMin = polyLine.GetOutline().MinimumPoint;
            //                            //长和宽
            //                            double b = Math.Abs(pMin.X - pMax.X);
            //                            double h = Math.Abs(pMin.Y - pMax.Y);
            //                            //柱子的中点坐标+坐标转换
            //                            XYZ pp = pMax.Add(pMin) / 2;
            //                            pp = transform.OfPoint(pp);
            //                            ////////找到中点，向上找管道，打断并尝试两侧退后各100
            //                            ////MEPCurve mepCurveToBreak = FindMEPCurveAtPoint(uiDoc, offsetHeight, pp);
            //                            ////if (mepCurveToBreak != null)
            //                            ////{
            //                            ////    // 打断管道
            //                            ////    MEPCurve copiedMEPCurve = BreakMEPCurveByOne(doc, mepCurveToBreak, pp);
            //                            ////}
            //                            //CreatColu(doc, pp, b, h); //生成柱子
            //                        }
            //                        else if (insObj is Arc circle)
            //                        {
            //                            //XYZ pp = circle.Center;
            //                            //pp = transform.OfPoint(pp);
            //                            ////// 查找与投影点相交的MEP曲线
            //                            //MEPCurve mepCurveToBreak = FindMEPCurveAtPoint(uiDoc, offsetHeight, pp);
            //                            //if (mepCurveToBreak != null)
            //                            //{
            //                            //    // 打断管道
            //                            //    MEPCurve copiedMEPCurve = BreakMEPCurveByOne(doc, mepCurveToBreak, pp);
            //                            //}
            //                        }
            //                        else if (insObj is GeometryInstance instance)
            //                        {
            //                            //instance.Transform;
            //                            //
            //                        }
            //                        else
            //                        {
            //                            TaskDialog.Show("tt", "未检测到符合条件多段线");
            //                            return Result.Failed;
            //                        }
            //                    }
            //                }
            //            }
            //            ts.Commit();
            //        }
            //        else
            //        {
            //            ts.RollBack();
            //            TaskDialog.Show("错误", "无法获取有效的图形样式信息");
            //            return Result.Failed;
            //        }
            //        return Result.Succeeded;
            //    }
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户取消了选择操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    TaskDialog.Show("错误", $"执行过程中发生错误: {ex.Message}");
            //    return Result.Failed;
            //}
            ////例程结束

            ////////1003 检测A-B点之间可见
            ////try
            ////{
            ////    // --- 步骤 1: 获取用户输入 ---
            ////    // 1.1 获取观察点
            ////    XYZ observerPoint = uiDoc.Selection.PickPoint("请选择观察点 (眼睛的位置)");
            ////    // 1.2 获取目标面
            ////    Reference faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "请选择标记牌的正面");
            ////    Element targetElement = doc.GetElement(faceRef);
            ////    Face targetFace = targetElement.GetGeometryObjectFromReference(faceRef) as Face;
            ////    if (targetFace == null)
            ////    {
            ////        message = "未能获取有效的几何面。";
            ////        return Result.Failed;
            ////    }
            ////    // --- 步骤 2 & 3: 采样并进行可见性测试 ---
            ////    // 定义采样网格的密度 (例如 10x10)
            ////    int gridResolutionU = 15;
            ////    int gridResolutionV = 15;
            ////    List<XYZ> visiblePoints = new List<XYZ>();
            ////    List<XYZ> occludedPoints = new List<XYZ>();
            ////    BoundingBoxUV bbox = targetFace.GetBoundingBox();
            ////    UV min = bbox.Min;
            ////    UV max = bbox.Max;
            ////    // 准备 ReferenceIntersector
            ////    View3D view3D = FindBest3DView(doc);
            ////    if (view3D == null)
            ////    {
            ////        message = "需要一个3D视图来进行可见性分析。";
            ////        return Result.Failed;
            ////    }
            ////    ReferenceIntersector intersector = new ReferenceIntersector(targetElement.Id, FindReferenceTarget.Face, view3D);
            ////    intersector.FindReferencesInRevitLinks = true;
            ////    // 遍历采样网格
            ////    for (int i = 0; i <= gridResolutionU; i++)
            ////    {
            ////        for (int j = 0; j <= gridResolutionV; j++)
            ////        {
            ////            double u = min.U + (max.U - min.U) * i / gridResolutionU;
            ////            double v = min.V + (max.V - min.V) * j / gridResolutionV;
            ////            XYZ samplePointOnFace = targetFace.Evaluate(new UV(u, v));
            ////            XYZ direction = (samplePointOnFace - observerPoint).Normalize();
            ////            double distanceToFace = observerPoint.DistanceTo(samplePointOnFace);
            ////            // 执行射线检测
            ////            ReferenceWithContext refWithContext = intersector.FindNearest(observerPoint, direction);
            ////            bool isVisible = false;
            ////            double tolerance = 0.001; // 精度容差 (约0.3mm)
            ////            if (refWithContext == null)
            ////            {
            ////                // 射线未与任何物体相交，说明该点可见 (在开放空间中)
            ////                isVisible = true;
            ////            }
            ////            else
            ////            {
            ////                double hitDistance = refWithContext.Proximity;
            ////                // 如果碰撞点距离非常接近目标点，则认为是可见的
            ////                if (Math.Abs(hitDistance - distanceToFace) < tolerance)
            ////                {
            ////                    isVisible = true;
            ////                }
            ////            }
            ////            if (isVisible)
            ////            {
            ////                visiblePoints.Add(samplePointOnFace);
            ////            }
            ////            else
            ////            {
            ////                occludedPoints.Add(samplePointOnFace);
            ////            }
            ////        }
            ////    }
            ////    // --- 步骤 4: 可视化结果 ---
            ////    if (visiblePoints.Count == 0)
            ////    {
            ////        TaskDialog.Show("结果", "标记牌完全被遮挡，不可见。");
            ////        return Result.Succeeded;
            ////    }
            ////    using (Transaction tx = new Transaction(doc, "绘制可见性范围"))
            ////    {
            ////        tx.Start();
            ////        DrawVisibilityResults(doc, activeView, observerPoint, visiblePoints);
            ////        tx.Commit();
            ////    }
            ////    TaskDialog.Show("完成", $"可见性分析完成。\n可见采样点: {visiblePoints.Count}    已在视图中绘制可见范围。");
            ////    return Result.Succeeded;
            ////}
            ////catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            ////{
            ////    return Result.Cancelled;
            ////}
            ////catch (Exception ex)
            ////{
            ////    message = ex.Message;
            ////    return Result.Failed;
            ////}
            //////1003 升级二维射线检测方法
            //try
            //{
            //    // 1. 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择扫描中心点");
            //    // 2. 定义扫描高度 (例如 200mm)
            //    double deltaHeightMM = 200.0;
            //    double heightOffset = UnitUtils.ConvertToInternalUnits(deltaHeightMM, UnitTypeId.Millimeters);
            //    XYZ scanOrigin = origin + new XYZ(0, 0, heightOffset);
            //    // 3. 准备结果容器和字符串构建器
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 4. (性能优化) 在循环外创建 ReferenceIntersector
            //    View3D view3D = FindBest3DView(doc);
            //    if (view3D == null)
            //    {
            //        message = "项目中找不到可用于检测的3D视图。";
            //        return Result.Failed;
            //    }
            //    ReferenceIntersector intersector = new ReferenceIntersector(view3D);
            //    intersector.TargetType = FindReferenceTarget.Face;
            //    intersector.FindReferencesInRevitLinks = true;
            //    // 5. 在XY平面进行360度检测
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        double radians = angle * Math.PI / 180.0;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ReferenceWithContext refWithContext = intersector.FindNearest(scanOrigin, direction);
            //        if (refWithContext != null)
            //        {
            //            Reference reference = refWithContext.GetReference();
            //            if (reference != null && reference.ElementId != ElementId.InvalidElementId)
            //            {
            //                hitElementIds.Add(reference.ElementId);
            //            }
            //        }
            //    }
            //    // 6. 处理并显示结果
            //    if (hitElementIds.Count == 0)
            //    {
            //        TaskDialog.Show("扫描结果", "在指定高度和范围内没有检测到任何对象。");
            //    }
            //    else
            //    {
            //        foreach (var id in hitElementIds)
            //        {
            //            Element elem = doc.GetElement(id);
            //            stringBuilder.AppendLine($"ID: {id.IntegerValue}, 名称: {elem?.Name ?? "N/A"}");
            //        }
            //        TaskDialog.Show("扫描结果", $"共检测到 {hitElementIds.Count} 个独立对象:{stringBuilder}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds.ToList());
            //        uiDoc.RefreshActiveView();
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户按 ESC 取消，是正常操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    // 其他意外错误
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //////1002 拆分楼板，读取出所有轮廓并分别保存多个楼板。注意存在逻辑问题，未处理环嵌套的问题，无法维持板内部开洞
            //// 1. 提示用户选择一个楼板
            //Reference selectedRef;
            //try
            //{
            //    selectedRef = uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "请选择一个包含多个轮廓的楼板");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //Floor originalFloor = doc.GetElement(selectedRef) as Floor;
            //if (originalFloor == null)
            //{
            //    message = "选择的不是一个有效的楼板。";
            //    return Result.Failed;
            //}
            //// 2. 通过几何体获取楼板的轮廓
            //List<CurveArray> profileLoops = GetFloorLoopsFromGeometry(originalFloor);
            //if (profileLoops == null || profileLoops.Count == 0)
            //{
            //    message = "无法从楼板的几何体中提取轮廓。";
            //    return Result.Failed;
            //}
            //// 3. 检查轮廓数量
            //if (profileLoops.Count <= 1)
            //{
            //    TaskDialog.Show("提示", "所选楼板只包含一个轮廓，无需拆分。");
            //    return Result.Succeeded;
            //}
            //using (TransactionGroup tg = new TransactionGroup(doc, "拆分楼板"))
            //{
            //    tg.Start();
            //    try
            //    {
            //        ElementId floorTypeId = originalFloor.GetTypeId();
            //        ElementId levelId = originalFloor.LevelId;
            //        bool isStructural = originalFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() == 1;
            //        Level level = doc.GetElement(levelId) as Level;
            //        FloorType floorType = doc.GetElement(floorTypeId) as FloorType;
            //        foreach (CurveArray curveLoop in profileLoops)
            //        {
            //            using (Transaction tx = new Transaction(doc, "创建单个楼板"))
            //            {
            //                tx.Start();
            //                doc.Create.NewFloor(curveLoop, floorType, level, isStructural);
            //                tx.Commit();
            //            }
            //        }
            //        using (Transaction tx = new Transaction(doc, "删除原始楼板"))
            //        {
            //            tx.Start();
            //            doc.Delete(originalFloor.Id);
            //            tx.Commit();
            //        }
            //        tg.Assimilate();
            //        TaskDialog.Show("成功", $"已成功将原始楼板拆分为 {profileLoops.Count} 个独立的楼板。");
            //        return Result.Succeeded;
            //    }
            //    catch (System.Exception ex)
            //    {
            //        message = "在拆分楼板时发生错误: " + ex.Message;
            //        tg.RollBack();
            //        return Result.Failed;
            //    }
            //}
            //////0404 升级柱切板和梁，梁切板。使用 BuiltInCategory 枚举，而不是魔术数字
            //var structuralColumns = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralColumns)
            //    .WhereElementIsNotElementType().ToElements();
            //var structuralFraming = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralFraming)
            //    .WhereElementIsNotElementType().ToElements();
            //using (Transaction transaction = new Transaction(doc, "自动调整几何连接关系"))
            //{
            //    transaction.Start();
            //    // 1. 柱切割梁和楼板
            //    foreach (Element column in structuralColumns)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, column, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            // 使用类型安全的比较
            //            var categoryId = nearbyElem.Category.Id.IntegerValue;
            //            if (categoryId == (int)BuiltInCategory.OST_StructuralFraming || categoryId == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, column, nearbyElem);
            //            }
            //        }
            //    }
            //    // 2. 梁切割楼板
            //    foreach (Element beam in structuralFraming)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, beam, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            if (nearbyElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, beam, nearbyElem);
            //            }
            //        }
            //    }
            //    transaction.Commit();
            //}
            ////0909 取楼梯中心几何点
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            //BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            //if (bbox == null) return Result.Failed;
            //XYZ min = bbox.Min;
            //XYZ max = bbox.Max;
            //XYZ center = (min + max) * 0.5;
            //// 输出中心点（XY）
            //TaskDialog.Show("楼梯中心", $"楼梯 {stair.Id} 的中心点XY坐标: ({center.X}, {center.Y})");
            //例程结束
            //0906 楼梯应与空间结合，单独设置房间应付异型楼梯等非标情况
            //var instances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Stairs).ToElementIds();
            ////以上收集的包含symbol和实例
            //StringBuilder stringBuilder = new StringBuilder();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in instances)
            //{
            //    //只过滤实例,取得实体和symbol
            //    if (Stairs.IsByComponent(doc, item))
            //    {
            //        stringBuilder.AppendLine(item.IntegerValue.ToString());
            //        var component = doc.GetElement(item);
            //        stringBuilder.AppendLine(doc.GetElement(component.GetTypeId()).Name.ToString());
            //        ids.Add(component.Id);
            //    }
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString() + "+" + ids.Count().ToString());
            //////0906 楼梯entity属性梳理 
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            ////var instance = doc.GetElement(new ElementId(2187406)) as Element;
            ////if (instance is Stairs)
            ////{
            ////    var stair = (Stairs)instance;
            ////    //TaskDialog.Show("tt", stair.NumberOfStories.ToString());
            ////    //实际单步高度
            ////    //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
            ////    //实际单步深度,踏面数量
            ////    //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualTreadsNumber).ToString());
            ////    //绝对高度底和顶，要计入项目基点高差
            //var basePoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
            //double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
            //TaskDialog.Show("tt", (stair.BaseElevation * 304.8 - deltaHeight).ToString("F2"));
            //TaskDialog.Show("tt", (stair.TopElevation * 304.8 - deltaHeight).ToString("F2"));
            ////    //楼梯总高差
            ////    //TaskDialog.Show("tt", (stair.Height * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.GetStairsRuns().Count()).ToString());
            ////    //跑数和内部各跑宽度，高度等
            ////    //var runs = stair.GetStairsRuns();
            ////    //StringBuilder stringBuilder = new StringBuilder();
            ////    //foreach (var item in runs)
            ////    //{
            ////    //    StairsRun stairsRun = doc.GetElement(item) as StairsRun;
            ////    //    stringBuilder.AppendLine((stairsRun.ActualRunWidth * 304.8).ToString());
            ////    //}
            ////    //TaskDialog.Show("tt", runs.Count().ToString());
            ////}
            ////例程结束
            //////0906 房间楼梯关系梳理 ，判断楼梯是否有部分在房间内即可，没必要全匹配
            //var room = doc.GetElement(new ElementId(2006502)) as Room;
            //////var room = doc.GetElement(new ElementId(1295107)) as Room;
            //////var room = doc.GetElement(new ElementId(1295122)) as Room;
            ////var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            //////int edges = room.GetBoundarySegments(boundaryOptions).Sum(loop => loop.Count);
            ////IList<IList<BoundarySegment>> boundarySegments = room.GetBoundarySegments(boundaryOptions);
            ////BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            ////XYZ minPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            ////XYZ maxPoint = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            ////foreach (IList<BoundarySegment> boundaryLoop in boundarySegments)
            ////{
            ////    CurveLoop curveLoop = new CurveLoop();
            ////    foreach (BoundarySegment segment in boundaryLoop)
            ////    {
            ////        // 获取曲线的起点和终点
            ////        Curve curve = segment.GetCurve();
            ////        XYZ startPoint = curve.GetEndPoint(0);
            ////        XYZ endPoint = curve.GetEndPoint(1);
            ////        // 更新最小点
            ////        minPoint = new XYZ(
            ////            Math.Min(minPoint.X, Math.Min(startPoint.X, endPoint.X)),
            ////            Math.Min(minPoint.Y, Math.Min(startPoint.Y, endPoint.Y)),
            ////            Math.Min(minPoint.Z, Math.Min(startPoint.Z, endPoint.Z))
            ////        );
            ////        // 更新最大点
            ////        maxPoint = new XYZ(
            ////            Math.Max(maxPoint.X, Math.Max(startPoint.X, endPoint.X)),
            ////            Math.Max(maxPoint.Y, Math.Max(startPoint.Y, endPoint.Y)),
            ////            //Math.Max(maxPoint.Z, Math.Max(startPoint.Z, endPoint.Z))
            ////            double.MaxValue);
            ////    }
            ////}
            ////// 设置边界框的最小点和最大点
            ////boundingBox.Min = minPoint;
            ////boundingBox.Max = maxPoint;
            //////TaskDialog.Show("tt", $"{boundingBox.Max.X.ToString("F2")}+{boundingBox.Max.Y.ToString("F2")}+{boundingBox.Max.Z.ToString("F2")}");
            //////TaskDialog.Show("tt", $"{boundingBox.Min.X.ToString("F2")}+{boundingBox.Min.Y.ToString("F2")}+{boundingBox.Min.Z.ToString("F2")}");
            //例程结束
            ////检查楼梯中心点是否在房间内也可以
            ////var stair = doc.GetElement(new ElementId(1926218)) as Stairs;
            //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
            //if (isStairInRoom)
            //{  TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。"); }
            //else {   TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。"); }
            //例程结束
            //////0804 房间管理器.OK  
            //RoomManagerView roomManager = new RoomManagerView(uiApp);
            //roomManager.Show();

            //////0811 射线360扫射检测碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    double deltaHeight = 200;
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 5. 在XY平面进行360度检测（每1度一次）
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        // 计算当前角度方向向量（Z=0）
            //        double radians = angle * Math.PI / 180;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //        if (hitElementId != ElementId.InvalidElementId)
            //        {
            //            hitElementIds.Add(hitElementId);
            //        }
            //    }
            //    foreach (var item in hitElementIds)
            //    {
            //        stringBuilder.AppendLine(item.ToString());
            //    }
            //    if (hitElementIds == null) { TaskDialog.Show("结果", "没有检测到碰撞对象"); }
            //    else
            //    {
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElementIds.Count}\n" + $"ID: {stringBuilder.ToString()}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds);
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            ////0811 二维射线法手搓尝试，单点单方向碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    // 定义射线方向（这里使用X轴方向）
            //    XYZ direction = XYZ.BasisX;
            //    double deltaHeight = 200;
            //    // 执行射线检测
            //    ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //    if (hitElementId == ElementId.InvalidElementId)
            //    {
            //        TaskDialog.Show("结果", "没有检测到碰撞对象");
            //    }
            //    else
            //    {
            //        Element hitElement = doc.GetElement(hitElementId);
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElement.Name}\n" + $"ID: {hitElementId.IntegerValue}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(new List<ElementId> { hitElementId });
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            //例程结束



            //////1014 补充沟体替换
            //CircleGaugePlaceView circleGaugePlaceView = new CircleGaugePlaceView(uiApp);
            //circleGaugePlaceView.Show();

            ////0425 参照平面切割测试
            //// 检查当前视图是否为平面、立面或剖面
            //if (!(doc.ActiveView.ViewType is ViewType.FloorPlan || doc.ActiveView.ViewType is ViewType.Section || doc.ActiveView.ViewType is ViewType.Elevation))
            //{
            //    message = "请在平面、立面或剖面视图中运行此命令。";
            //    return Result.Failed;
            //}
            //// 1. 让用户选择一个参照平面
            //Reference refPlaneRef;
            //try
            //{
            //    refPlaneRef = uiDoc.Selection.PickObject(ObjectType.Element, new ReferencePlaneSelectionFilter(), "请选择一个用于打断的参照平面");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //// 获取选中的参照平面元素
            //ReferencePlane selectedRefPlane = doc.GetElement(refPlaneRef) as ReferencePlane;
            //if (selectedRefPlane == null)
            //{
            //    message = "未选择有效的参照平面。";
            //    return Result.Failed;
            //}
            //// 获取参照平面的几何信息
            //Plane refPlane = selectedRefPlane.GetPlane();
            //XYZ planeOrigin = refPlane.Origin;
            //XYZ planeNormal = refPlane.Normal;
            //// 收集所有目标元素
            //List<Element> targetElements = new List<Element>();
            //// 楼板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Floor)).WhereElementIsNotElementType().ToList());
            //// 天花板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Ceiling)).WhereElementIsNotElementType().ToList());
            //// 迹线屋面（通过参数筛选）
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(RoofBase)).WhereElementIsNotElementType().Cast<RoofBase>()
            //    .Where(r =>
            //    {
            //        return r is FootPrintRoof;
            //        //// 迹线屋面有 Footprint 草图，拉伸屋面没有
            //    }).Cast<Element>().ToList());
            //// 存储与参照平面相交且正交的楼板信息
            //List<KeyValuePair<ElementId, string>> intersectingFloors = new List<KeyValuePair<ElementId, string>>();
            //List<ElementId> intersectingFloorIds = new List<ElementId>();
            //foreach (Element floor in targetElements)
            //{
            //    // 获取楼板的边界框（快速筛选）
            //    BoundingBoxXYZ floorBbox = floor.get_BoundingBox(activeView);
            //    if (floorBbox == null) continue;
            //    // 快速检测：检查楼板的边界框是否与参照平面相交（可选，提高性能）
            //    bool bboxIntersects = CheckBoundingBoxIntersectsPlane(floorBbox, refPlane);
            //    if (!bboxIntersects) continue;
            //    // 获取楼板的几何信息进行精确检测
            //    Options geoOptions = new Options();
            //    geoOptions.ComputeReferences = true;
            //    geoOptions.DetailLevel = ViewDetailLevel.Fine;
            //    GeometryElement geoElement = floor.get_Geometry(geoOptions);
            //    if (geoElement == null) continue;
            //    bool isIntersectingAndOrthogonal = false;
            //    // 遍历楼板的几何实体进行精确相交和正交检测
            //    foreach (GeometryObject geoObj in geoElement)
            //    {
            //        Solid solid = geoObj as Solid;
            //        if (solid != null && solid.Faces.Size > 0)
            //        {
            //            // 检查实体是否与平面相交
            //            if (IsSolidIntersectPlane(solid, refPlane))
            //            {
            //                // 进一步检查是否有面的法向量与参照平面正交
            //                foreach (Face face in solid.Faces)
            //                {
            //                    XYZ faceNormal = face.ComputeNormal(UV.Zero);
            //                    if (faceNormal != null)
            //                    {
            //                        double dotProduct = Math.Abs(faceNormal.DotProduct(planeNormal));
            //                        if (dotProduct < 1e-6) // 正交检查
            //                        {
            //                            isIntersectingAndOrthogonal = true;
            //                            break;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
            //        {
            //            // 检查网格是否与平面相正交
            //            if (IsMeshIntersectPlane(mesh, refPlane))
            //            {
            //                for (int i = 0; i < mesh.NumTriangles; i++)
            //                {
            //                    MeshTriangle triangle = mesh.get_Triangle(i);
            //                    // 正确计算三角形法向量（叉积）
            //                    XYZ v0 = triangle.get_Vertex(0);
            //                    XYZ v1 = triangle.get_Vertex(1);
            //                    XYZ v2 = triangle.get_Vertex(2);
            //                    XYZ edge1 = v1 - v0;
            //                    XYZ edge2 = v2 - v0;
            //                    XYZ triangleNormal = edge1.CrossProduct(edge2).Normalize();
            //                    // 判断三角形是否与平面正交（三角形法向量平行于参考平面）
            //                    // 即三角形法向量与平面法向量垂直（点积接近0）
            //                    double dotProduct = Math.Abs(triangleNormal.DotProduct(planeNormal));
            //                    // dotProduct ≈ 0 表示三角形法向量 ⊥ 平面法向量
            //                    // 即三角形平面 ∥ 参考平面（三角形与参考平面正交/垂直）
            //                    if (dotProduct < 1e-3) // 使用稍大的容差
            //                    {
            //                        isIntersectingAndOrthogonal = true;
            //                        break; // 跳出三角形循环
            //                    }
            //                }
            //                // 关键：如果已找到正交三角形，跳出外层 mesh 循环
            //                if (isIntersectingAndOrthogonal)
            //                    break;
            //            }
            //            ////普通相交简化如下
            //            //if (IsMeshIntersectPlane(mesh, refPlane))
            //            //{
            //            //    isIntersectingAndOrthogonal = true;
            //            //    break; // 假设 mesh 相交即视为正交（根据业务需求）
            //            //}
            //        }
            //        if (isIntersectingAndOrthogonal) break;
            //    }
            //    if (isIntersectingAndOrthogonal)
            //    {
            //        intersectingFloorIds.Add(floor.Id);
            //        // 获取楼板信息用于显示
            //        Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            //        string levelName = levelParam != null ? levelParam.AsValueString() : "未知";
            //        string floorTypeName = floor.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString();
            //        string floorInfo = $"楼板 ID:{floor.Id.IntegerValue}, 类型:{floorTypeName}, 标高:{levelName}";
            //        intersectingFloors.Add(new KeyValuePair<ElementId, string>(floor.Id, floorInfo));
            //    }
            //}
            ////// 输出结果
            //int intersectingCount = intersectingFloors.Count;
            //message = $"共找到 {intersectingCount} 个与参照平面相交且正交的平面元素。";
            ////if (intersectingCount > 0)
            ////{
            ////    uiDoc.Selection.SetElementIds(intersectingFloorIds);
            ////}
            ////TaskDialog.Show("tt", message);
            ////// 在找到相交且正交的板之后，创建构造线
            //if (!(intersectingCount > 0 || intersectingFloorIds.Count > 0)) return Result.Cancelled;
            //// 开始一个事务来创建构造线
            //using (Transaction trans = new Transaction(doc, "创建相交构造线"))
            //{
            //    trans.Start();
            //    List<Curve> allIntersectionCurves = new List<Curve>();
            //    foreach (ElementId elementId in intersectingFloorIds)
            //    {
            //        Element element = doc.GetElement(elementId);
            //        if (element == null) continue;
            //        // 重新获取该元素的几何信息
            //        Options geoOptions = new Options();
            //        geoOptions.ComputeReferences = true;
            //        geoOptions.DetailLevel = ViewDetailLevel.Fine;
            //        GeometryElement geoElement = element.get_Geometry(geoOptions);
            //        if (geoElement == null) continue;
            //        // 获取该元素与参照平面的所有交线
            //        List<Curve> intersectionCurves = GetIntersectionCurvesWithPlane(geoElement, refPlane);
            //        allIntersectionCurves.AddRange(intersectionCurves);
            //        message += intersectionCurves.Count().ToString();
            //    }
            //    //// 创建构造线（使用模型线或详图线）
            //    if (allIntersectionCurves.Count > 0)
            //    {
            //        // 选择创建方式：在平面视图中使用详图线，在3D视图中使用模型线
            //        bool useDetailLines = (activeView.ViewType == ViewType.FloorPlan ||
            //                               activeView.ViewType == ViewType.CeilingPlan ||
            //                               activeView.ViewType == ViewType.Section ||
            //                               activeView.ViewType == ViewType.Elevation);
            //        //if (useDetailLines)
            //        //{
            //        //    // 在视图中创建详图线（仅在该视图中可见）
            //        //    foreach (Curve curve in allIntersectionCurves)
            //        //    {
            //        //        // 将曲线投影到视图平面（如果需要）
            //        //        Curve projectedCurve = ProjectCurveToViewPlane(curve, activeView);
            //        //        if (projectedCurve != null)
            //        //        {
            //        //            //TaskDialog.Show("tt", (projectedCurve.Length * 304.8).ToString());
            //        //            //// 创建详图线
            //        //            DetailLine detailLine = doc.Create.NewDetailCurve(activeView, projectedCurve) as DetailLine;
            //        //            if (detailLine != null)
            //        //            {
            //        //                // 设置线型样式（可选）
            //        //                // 注意：需要先获取或创建线型样式
            //        //                SetLineStyle(detailLine, "Dash");
            //        //            }
            //        //        }
            //        //    }
            //        //    message += $"\n已创建 {allIntersectionCurves.Count} 条详图线。";
            //        //}
            //        //else
            //        //{
            //        //// 创建模型线（在所有视图中可见）
            //        //// 需要选择一个工作平面
            //        SketchPlane sketchPlane = SketchPlane.Create(doc, refPlane);
            //        foreach (Curve curve in allIntersectionCurves)
            //        {
            //            ModelCurve modelCurve = doc.Create.NewModelCurve(curve, sketchPlane);
            //            if (modelCurve != null)
            //            {
            //                // 设置线型样式（可选）
            //                SetLineStyle(modelCurve, "Dash");
            //            }
            //        }
            //        message += $"\n已创建 {allIntersectionCurves.Count} 条模型线。";
            //        //}
            //    }
            //    else
            //    {
            //        message += "\n未找到有效的交线。";
            //    }
            //    trans.Commit();
            //}
            //TaskDialog.Show("执行结果", message);
            ////////1003 SplitElementsCommand 变形缝、后浇带打断板、梁 遗留考虑问题较多，板边界，连线方向等等

            ////0421 构件分析测试 要排除固定的MEP相关配置项和系统材质、视图等，只管理手动添加的元素
            //var analyzer = new ModelProfessionAnalyzer(doc);
            //string report = analyzer.GetDetailedReport();
            //TaskDialog.Show("分析结果", report);

            ////var fitting = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_DuctFitting).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().FirstOrDefault();
            ////Autodesk.Revit.ApplicationServices.Application app = uiApp.Application;
            //Document familyDoc;
            //if (uiApp.ActiveUIDocument?.Document.IsFamilyDocument != true) return Result.Cancelled;
            //familyDoc = doc;
            //var type =familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsValueString();
            //TaskDialog.Show("tt", type.ToString());        

            //////找出所有有几何instance并分类
            //List<Element> allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Cast<Element>().ToList();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in allInstances)
            //{
            //    if (item.HasPhases())
            //    {
            //        ids.Add(item.Id);
            //    }
            //}
            //uiDoc.Selection.SetElementIds(ids);
            return Result.Succeeded;
        }
        //private int _maximum;
        //public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
        //0323 进度条调用模板，无需单独声明ProgressBar
        //    TransactionWithProgressBarHelper.Execute(doc, "提取构件信息", (service) =>
        //    {
        //        service.UpdateMax(sortedIds.Count());
        //        int index = 0;
        //        foreach (var id in sortedIds)
        //        {
        //            service.Update(++index, id.Value.ToString());
        //        }
        //    });
        //    
        //0425 检查相交并画线
        /// <summary>
        /// 检查边界框是否与平面相交
        /// </summary>
        private bool CheckBoundingBoxIntersectsPlane(BoundingBoxXYZ bbox, Plane plane)
        {
            if (bbox == null || plane == null) return false;
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            // 获取边界框的8个角点
            List<XYZ> corners = new List<XYZ>    {
                new XYZ(min.X, min.Y, min.Z),        new XYZ(max.X, min.Y, min.Z),
                new XYZ(min.X, max.Y, min.Z),        new XYZ(max.X, max.Y, min.Z),
                new XYZ(min.X, min.Y, max.Z),        new XYZ(max.X, min.Y, max.Z),
                new XYZ(min.X, max.Y, max.Z),        new XYZ(max.X, max.Y, max.Z)    };
            // 计算平面方程: ax + by + cz + d = 0
            // 平面法向量 (a, b, c)
            XYZ normal = plane.Normal;
            // 平面上的点
            XYZ origin = plane.Origin;
            // 计算 d = -(a*x0 + b*y0 + c*z0)
            double d = -(normal.X * origin.X + normal.Y * origin.Y + normal.Z * origin.Z);
            // 检查角点是否在平面两侧
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (XYZ point in corners)
            {
                // 计算有符号距离: (a*x + b*y + c*z + d) / sqrt(a^2 + b^2 + c^2)
                // 或者简化为 (a*x + b*y + c*z + d)，因为只需要判断符号
                double signedDistance = normal.X * point.X + normal.Y * point.Y + normal.Z * point.Z + d;
                // 点在平面上（距离接近0）
                if (Math.Abs(signedDistance) < 1e-6) return true;
                if (signedDistance > 0) hasPositive = true;
                else hasNegative = true;
                // 平面穿过边界框（点在两侧）
                if (hasPositive && hasNegative) return true;
            }
            // 所有点在同一侧，不相交
            return false;
        }
        /// <summary>
        /// 检查实体是否与平面相交
        /// </summary>
        private bool IsSolidIntersectPlane(Solid solid, Plane plane)
        {
            if (solid == null || solid.Faces.Size == 0 || plane == null) return false;
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            // 收集所有顶点并检查有符号距离
            List<double> distances = new List<double>();
            // 从边获取顶点
            foreach (Edge edge in solid.Edges)
            {
                Curve curve = edge.AsCurve();
                distances.Add(SignedDistanceTo(curve.GetEndPoint(0), normal, origin));
                distances.Add(SignedDistanceTo(curve.GetEndPoint(1), normal, origin));
            }
            // 从三角化面获取顶点（更密集）
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                Mesh mesh = face.Triangulate();
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    MeshTriangle triangle = mesh.get_Triangle(i);
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(0), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(1), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(2), normal, origin));
                }
            }
            // 检查距离分布
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (double d in distances)
            {
                if (Math.Abs(d) < 1e-6) return true;
                if (d > 0) hasPositive = true;
                else hasNegative = true;
                if (hasPositive && hasNegative) return true;
            }
            return false;
        }
        /// <summary>
        /// 检查网格是否与平面相交
        /// </summary>
        private bool IsMeshIntersectPlane(Mesh mesh, Plane plane)
        {
            if (mesh == null || mesh.NumTriangles == 0 || plane == null) return false;
            // 预计算平面参数，避免重复计算
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用点积）
                double d0 = SignedDistanceTo(v0, normal, origin);
                double d1 = SignedDistanceTo(v1, normal, origin);
                double d2 = SignedDistanceTo(v2, normal, origin);
                // 检查是否相交：有正有负或等于0（点在平面上）
                bool hasZero = Math.Abs(d0) < 1e-6 || Math.Abs(d1) < 1e-6 || Math.Abs(d2) < 1e-6;
                if (hasZero) return true;
                bool hasPositive = d0 > 0 || d1 > 0 || d2 > 0;
                bool hasNegative = d0 < 0 || d1 < 0 || d2 < 0;
                // 平面穿过三角形（点在两侧）
                if (hasPositive && hasNegative)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算点到平面的有符号距离（内联辅助方法，避免重复代码）
        /// </summary>
        private double SignedDistanceTo(XYZ point, XYZ planeNormal, XYZ planeOrigin)
        {
            XYZ toPoint = point - planeOrigin;
            return toPoint.DotProduct(planeNormal);
        }
        /// <summary>
        /// 获取几何元素与平面的所有交线
        /// </summary>
        private List<Curve> GetIntersectionCurvesWithPlane(GeometryElement geoElement, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (geoElement == null) return intersectionCurves;
            foreach (GeometryObject geoObj in geoElement)
            {
                Solid solid = geoObj as Solid;
                if (solid != null && solid.Faces.Size > 0)
                {
                    //获取实体与平面的交线
                    List<Curve> solidIntersections = GetSolidIntersectionCurvesWithPlane(solid, plane);
                    intersectionCurves.AddRange(solidIntersections);

                }
                else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
                {
                    //获取网格与平面的交线
                    List<Curve> meshIntersections = GetMeshIntersectionCurvesWithPlane(mesh, plane);
                    intersectionCurves.AddRange(meshIntersections);
                }
            }
            // 合并连接的曲线
            return intersectionCurves;
            //return MergeConnectedCurves(intersectionCurves);
        }
        /// <summary>
        /// 获取实体与平面的交线（最简可靠版）byKIMI
        /// </summary>
        private List<Curve> GetSolidIntersectionCurvesWithPlane(Solid solid, Plane plane)
        {
            var result = new List<Curve>();
            if (solid == null) return result;
            XYZ n = plane.Normal;
            XYZ o = plane.Origin;
            // 收集所有有效交点
            var pointSet = new HashSet<string>(); // 用于去重
            var allPoints = new List<XYZ>();
            Action<XYZ> addPoint = (p) =>
            {
                string key = $"{Math.Round(p.X, 6)},{Math.Round(p.Y, 6)},{Math.Round(p.Z, 6)}";
                if (!pointSet.Contains(key))
                {
                    pointSet.Add(key);
                    allPoints.Add(p);
                }
            };
            foreach (Edge edge in solid.Edges)
            {
                var c = edge.AsCurve();
                XYZ p0 = c.GetEndPoint(0);
                XYZ p1 = c.GetEndPoint(1);
                double d0 = (p0 - o).DotProduct(n);
                double d1 = (p1 - o).DotProduct(n);
                if (Math.Abs(d0) < 1e-6) addPoint(p0);
                if (Math.Abs(d1) < 1e-6) addPoint(p1);
                if (d0 * d1 < -1e-12)
                {
                    double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
                    addPoint(p0 + (p1 - p0) * t);
                }
            }
            if (allPoints.Count < 2) return result;
            // 在平面内排序并连接
            XYZ u = plane.XVec.Normalize();
            XYZ v = plane.YVec.Normalize();
            allPoints = allPoints.OrderBy(p => (p - o).DotProduct(u))
                .ThenBy(p => (p - o).DotProduct(v)).ToList();
            // 连接相邻点形成交线
            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                double dist = allPoints[i].DistanceTo(allPoints[i + 1]);
                if (dist > 1e-6 && dist < solid.GetBoundingBox().Max.DistanceTo(solid.GetBoundingBox().Min))
                {
                    result.Add(Line.CreateBound(allPoints[i], allPoints[i + 1]));
                }
            }
            return result;
        }
        /// <summary>
        /// 在平面坐标系下排序点
        /// </summary>
        private List<XYZ> SortPointsOnPlane(List<XYZ> points, Plane plane)
        {
            XYZ xAxis = plane.XVec.Normalize();
            XYZ yAxis = plane.YVec.Normalize();
            return points
                .Select(p => new
                {
                    Point = p,
                    U = (p - plane.Origin).DotProduct(xAxis),
                    V = (p - plane.Origin).DotProduct(yAxis)
                }).OrderBy(item => item.U)
                .ThenBy(item => item.V).Select(item => item.Point).ToList();
        }
        /// <summary>
        /// 获取网格与平面的交线
        /// </summary>
        private List<Curve> GetMeshIntersectionCurvesWithPlane(Mesh mesh, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (mesh == null || mesh.NumTriangles == 0) return intersectionCurves;
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用自定义方法）
                double d0 = SignedDistanceTo(v0, planeNormal, planeOrigin);
                double d1 = SignedDistanceTo(v1, planeNormal, planeOrigin);
                double d2 = SignedDistanceTo(v2, planeNormal, planeOrigin);
                // 检查三角形是否与平面相交
                List<XYZ> intersectionPoints = new List<XYZ>();
                // 检查每条边与平面的交点
                AddIntersectionPoint(v0, v1, d0, d1, plane, intersectionPoints);
                AddIntersectionPoint(v1, v2, d1, d2, plane, intersectionPoints);
                AddIntersectionPoint(v2, v0, d2, d0, plane, intersectionPoints);
                // 如果有两个交点，创建线段
                if (intersectionPoints.Count == 2)
                {
                    Line line = Line.CreateBound(intersectionPoints[0], intersectionPoints[1]);
                    intersectionCurves.Add(line);
                }
            }
            return intersectionCurves;
        }
        /// <summary>
        /// 添加边的交点
        /// </summary>
        private void AddIntersectionPoint(XYZ p1, XYZ p2, double d1, double d2, Plane plane, List<XYZ> points)
        {
            if (Math.Abs(d1) < 1e-9)
            {
                points.Add(p1);
            }
            else if (Math.Abs(d2) < 1e-9)
            {
                points.Add(p2);
            }
            else if (d1 * d2 < 0) // 点在平面两侧
            {
                double t = -d1 / (d2 - d1); // 插值参数
                XYZ intersection = p1 + t * (p2 - p1);
                points.Add(intersection);
            }
        }
        /// <summary>
        /// 获取直线与平面的交线段（仅支持Line）
        /// </summary>
        private List<Curve> GetLinePlaneSegments(Line line, Plane plane)
        {
            List<Curve> segments = new List<Curve>();
            if (line == null || plane == null) return segments;
            XYZ start = line.GetEndPoint(0);
            XYZ end = line.GetEndPoint(1);
            double d0 = (start - plane.Origin).DotProduct(plane.Normal);
            double d1 = (end - plane.Origin).DotProduct(plane.Normal);
            const double eps = 1e-6;
            // 完全在平面内
            if (Math.Abs(d0) < eps && Math.Abs(d1) < eps)
            {
                segments.Add(line.Clone());
                return segments;
            }
            // 无交点（同侧且不接触）
            if (d0 > eps && d1 > eps) return segments;
            if (d0 < -eps && d1 < -eps) return segments;
            // 计算交点
            double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
            XYZ intersection = start + (end - start) * t;
            // 返回平面内的部分（根据有符号距离判断）
            if (d0 >= -eps && d1 >= -eps)
            {
                // 都在正侧或接触平面
                if (d0 < eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 < eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else if (d0 <= eps && d1 <= eps)
            {
                // 都在负侧或接触平面
                if (d0 > -eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 > -eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else
            {
                // 跨平面，返回两侧
                segments.Add(Line.CreateBound(start, intersection));
                segments.Add(Line.CreateBound(intersection, end));
            }
            return segments;
        }
        /// <summary>
        /// 合并连接的曲线
        /// </summary>
        private List<Curve> MergeConnectedCurves(List<Curve> curves)
        {
            if (curves.Count <= 1) return curves;
            List<Curve> mergedCurves = new List<Curve>();
            List<Curve> remaining = new List<Curve>(curves);
            while (remaining.Count > 0)
            {
                Curve current = remaining[0];
                remaining.RemoveAt(0);
                bool merged = true;
                while (merged && remaining.Count > 0)
                {
                    merged = false;
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        if (AreCurvesConnected(current, remaining[i]))
                        {
                            // 合并曲线
                            current = MergeTwoCurves(current, remaining[i]);
                            remaining.RemoveAt(i);
                            merged = true;
                            break;
                        }
                    }
                }
                mergedCurves.Add(current);
            }
            return mergedCurves;
        }
        /// <summary>
        /// 判断两条曲线是否连接
        /// </summary>
        private bool AreCurvesConnected(Curve curve1, Curve curve2, double tolerance = 1e-6)
        {
            XYZ end1 = curve1.GetEndPoint(1);
            XYZ start2 = curve2.GetEndPoint(0);
            return end1.DistanceTo(start2) < tolerance;
        }
        /// <summary>
        /// 合并两条曲线
        /// </summary>
        private Curve MergeTwoCurves(Curve curve1, Curve curve2)
        {
            // 简单实现：创建一条新的直线连接两个端点
            XYZ start = curve1.GetEndPoint(0);
            XYZ end = curve2.GetEndPoint(1);
            return Line.CreateBound(start, end);
        }
        /// <summary>
        /// 将曲线投影到当前视图平面,没使用此方法？？
        /// </summary>
        //private Curve ProjectCurveToViewPlane(Curve curve, View activeView)
        //{
        //    if (curve == null || activeView == null) return curve;
        //    // 获取视图平面（适用于平、立、剖面）
        //    Plane viewPlane = activeView.SketchPlane.GetPlane();
        //    if (viewPlane == null) return curve;
        //    List<XYZ> projectedPoints = new List<XYZ>();
        //    // 投影曲线的关键点
        //    IList<XYZ> points = curve.Tessellate();
        //    foreach (XYZ point in points)
        //    {
        //        XYZ projectedPoint = ProjectPointToPlane(point, viewPlane);
        //        projectedPoints.Add(projectedPoint);
        //    }
        //    if (projectedPoints.Count < 2) return curve;
        //    XYZ start = projectedPoints[0];
        //    XYZ end = projectedPoints[projectedPoints.Count - 1];
        //    // 检查投影后是否有有效长度
        //    double projectedLength = start.DistanceTo(end);
        //    if (projectedLength < 1e-6)
        //    {
        //        // 投影长度为0，曲线与视图平面垂直，返回空（防错）
        //        return null;
        //    }
        //    // 检查所有投影点是否基本重合（更严格的垂直判断）
        //    bool allSame = true;
        //    for (int i = 1; i < projectedPoints.Count; i++)
        //    {
        //        if (!projectedPoints[i].IsAlmostEqualTo(start))
        //        {
        //            allSame = false;
        //            break;
        //        }
        //    }
        //    if (allSame) return curve;
        //    // 根据原始曲线类型创建对应的投影曲线
        //    if (curve is Line) return Line.CreateBound(start, end);
        //    // 弧线投影后可能变为直线或保持弧线简化处理：返回直线段
        //    else if (curve is Arc arc) return Line.CreateBound(start, end);
        //    // 其他复杂曲线，返回首尾投影点连线
        //    else return Line.CreateBound(start, end);
        //}
        /// <summary>
        /// 将点投影到平面
        /// </summary>
        private XYZ ProjectPointToPlane(XYZ point, Plane plane)
        {
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            double distance = SignedDistanceTo(point, planeNormal, planeOrigin);
            return point - distance * plane.Normal;
        }
        /// <summary>
        /// 设置线条样式
        /// </summary>
        private void SetLineStyle(dynamic line, string styleName)
        {
            try
            {
                // 获取线型样式
                FilteredElementCollector collector = new FilteredElementCollector(line.Document);
                GraphicsStyle graphicsStyle = collector
                    .OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>()
                    .FirstOrDefault(g => g.Name == styleName);

                if (graphicsStyle != null)
                {
                    line.LineStyle = graphicsStyle;
                }
            }
            catch
            {
                // 如果设置失败，使用默认样式
            }
        }
        //找实例共同文字属性列表     
        public Dictionary<string, string> GetCommonStringParameterNames(Document doc)
        {
            // 1. 收集文档中所有的族实例
            var allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
                .Where(e => e.HasPhases()).Cast<FamilyInstance>().ToList();
            if (allInstances.Count == 0) return new Dictionary<string, string>();
            // 分别追踪实例参数和类型参数的交集
            HashSet<string> commonInstanceParams = null;
            HashSet<string> commonSymbolParams = null;
            // 2. 遍历所有实例，分别求交集
            foreach (FamilyInstance instance in allInstances)
            {
                // 收集当前实例的实例参数
                var currentInstanceParams = new HashSet<string>();
                foreach (Parameter param in instance.Parameters)
                {
                    if (param.StorageType == StorageType.String && !param.IsReadOnly)
                    {
                        currentInstanceParams.Add(param.Definition.Name);
                    }
                }
                // 收集当前实例的类型参数
                var currentSymbolParams = new HashSet<string>();
                FamilySymbol symbol = instance.Symbol;
                if (symbol != null)
                {
                    foreach (Parameter param in symbol.Parameters)
                    {
                        if (param.StorageType == StorageType.String && !param.IsReadOnly)
                        {
                            currentSymbolParams.Add(param.Definition.Name);
                        }
                    }
                }
                // 求交集
                if (commonInstanceParams == null)
                {
                    commonInstanceParams = new HashSet<string>(currentInstanceParams);
                }
                else
                {
                    commonInstanceParams.IntersectWith(currentInstanceParams);
                }
                if (commonSymbolParams == null)
                {
                    commonSymbolParams = new HashSet<string>(currentSymbolParams);
                }
                else
                {
                    commonSymbolParams.IntersectWith(currentSymbolParams);
                }
                // 提前退出
                if (commonInstanceParams.Count == 0 && commonSymbolParams.Count == 0)
                    break;
            }
            // 3. 组装字典结果，标记来源
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 标记仅实例共有的参数
            foreach (var name in commonInstanceParams ?? Enumerable.Empty<string>())
            {
                result[name] = "实例";
            }
            // 标记仅类型共有的参数，或两者共有
            foreach (var name in commonSymbolParams ?? Enumerable.Empty<string>())
            {
                if (result.ContainsKey(name))
                {
                    // 实例和类型都有同名参数
                    result[name] = "两者";
                }
                else
                {
                    result[name] = "类型";
                }
            }
            return result;
        }
        //0428 原TEst10方法
        //生成柱子
        // 截断小数位数的方法
        private double CutDecimalWithN(double value, int decimalPlaces)
        {
            double factor = Math.Pow(10, decimalPlaces);
            return Math.Truncate(value * factor) / factor;
        }
        private void CreatColu(Document doc, XYZ point, double b, double h)
        {
            FilteredElementCollector fil = new FilteredElementCollector(doc);
            fil.OfClass(typeof(FamilySymbol));
            string bh = CutDecimalWithN(b * 304.8, 4).ToString() + " " + "x" + " " + CutDecimalWithN(h * 304.8, 4);
            List<FamilySymbol> listFa = new List<FamilySymbol>();
            foreach (FamilySymbol fa in fil)
            {
                // 更安全的参数获取方式
                Parameter familyNameParam = fa.LookupParameter("族名称");
                if (familyNameParam != null && familyNameParam.AsString() == "CADC_柱-混凝土-矩形")
                {
                    listFa.Add(fa);
                }
            }
            if (listFa.Count == 0)
            {
                TaskDialog.Show("错误", "未找到名为'CADC_柱-混凝土-矩形'的族类型");
                return;
            }
            FamilySymbol targetSymbol = null;
            // 查找匹配的族类型
            foreach (FamilySymbol symbol in listFa)
            {
                if (bh == symbol.Name)
                {
                    targetSymbol = symbol;
                    break;
                }
            }
            if (targetSymbol != null)
            {
                // 确保族类型已激活
                if (!targetSymbol.IsActive) targetSymbol.Activate();
                doc.Create.NewFamilyInstance(point, targetSymbol, StructuralType.Column);
            }
            else
            {
                // 复制创建新的族类型
                FamilySymbol fam = listFa[0];
                // 确保族类型已激活
                if (!fam.IsActive) fam.Activate();
                try
                {
                    FamilySymbol newSymbol = fam.Duplicate(bh) as FamilySymbol;
                    // 设置参数 - 使用更安全的参数查找方式
                    Parameter widthParam = newSymbol.LookupParameter("b");
                    Parameter heightParam = newSymbol.LookupParameter("h");
                    if (widthParam != null && heightParam != null)
                    {
                        using (Transaction t = new Transaction(doc, "设置柱参数"))
                        {
                            t.Start();
                            widthParam.Set(b);
                            heightParam.Set(h);
                            t.Commit();
                        }
                        doc.Create.NewFamilyInstance(point, newSymbol, StructuralType.Column);
                    }
                    else
                    {
                        TaskDialog.Show("错误", "找不到截面宽度或截面高度参数");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"创建族类型失败: {ex.Message}");
                }
            }
        }
        //private void CreatColu(Document doc, XYZ point, double b, double h)
        //{
        //    FilteredElementCollector fil = new FilteredElementCollector(doc);
        //    fil.OfClass(typeof(FamilySymbol));
        //    string bh = CutDecimalWithN(b * 304.8, 4).ToString() + " " + "x" + " " + CutDecimalWithN(h * 304.8, 4);
        //    List<FamilySymbol> listFa = new List<FamilySymbol>();
        //    foreach (FamilySymbol fa in fil)
        //    {
        //        if (fa.GetParameters("族名称")[0].AsString() == "砼矩形柱")
        //        {
        //            listFa.Add(fa);
        //        }
        //    }
        //    int i = 0;
        //    bool bo = false;
        //    int j = 0;
        //    for (i = 0; i < listFa.Count; i++)
        //    {
        //        if (bh == listFa[i].Name)
        //        {
        //            bo = true;
        //            j = i;
        //        }
        //    }
        //    if (bo == true)
        //    {
        //        doc.Create.NewFamilyInstance(point, listFa[j], StructuralType.Column);
        //    }
        //    else
        //    {
        //        FamilySymbol fam = listFa[0];
        //        ElementType coluType = fam.Duplicate(bh);
        //        coluType.GetParameters("截面宽度")[0].Set(b);
        //        coluType.GetParameters("截面高度")[0].Set(h);
        //        FamilySymbol fs = coluType as FamilySymbol;
        //        doc.Create.NewFamilyInstance(point, fs, StructuralType.Column);
        //    }
        //}
        ///// <summary>
        ///// 在视图中绘制可见性分析的结果
        ///// </summary>
        private void DrawVisibilityResults(Document doc, View view, XYZ observerPoint, List<XYZ> visiblePoints)
        {
            // 创建新的图形样式以便区分
            GraphicsStyle gs = GetOrCreateGraphicsStyle(doc, "可见性分析线");
            if (visiblePoints.Count <= 1) return;
            // 找到可见区域的边界点（一个简化的方法是找到凸包）
            List<XYZ> boundaryPoints = FindConvexHull(visiblePoints);
            // 1. 绘制可见区域在标记牌上的轮廓线 (最大最小范围)
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                XYZ p1 = boundaryPoints[i];
                XYZ p2 = boundaryPoints[(i + 1) % boundaryPoints.Count]; // 连接到下一个点，最后一个点连回第一个
                Line line = Line.CreateBound(p1, p2);
                doc.Create.NewModelCurve(line, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
            }
            // 2. 绘制从观察点到可见区域边界的“视锥”
            foreach (XYZ boundaryPoint in boundaryPoints)
            {
                Line coneLine = Line.CreateBound(observerPoint, boundaryPoint);
                ModelCurve mc = doc.Create.NewModelCurve(coneLine, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
                mc.LineStyle = gs; // 应用自定义图形样式
            }
        }
        ///// <summary>
        ///// 找到一组点的2D凸包 (投影到XY平面)
        ///// 这是一个简化的凸包算法 (Gift wrapping algorithm)
        ///// </summary>
        public List<XYZ> FindConvexHull(List<XYZ> points)
        {
            if (points.Count <= 2) return points;
            List<XYZ> hull = new List<XYZ>();
            // 找到最左边的点作为起点
            XYZ startPoint = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            XYZ currentPoint = startPoint;
            do
            {
                hull.Add(currentPoint);
                XYZ nextPoint = points[0];
                foreach (XYZ p in points)
                {
                    if (nextPoint == currentPoint || IsLeft(currentPoint, nextPoint, p) > 0)
                    {
                        nextPoint = p;
                    }
                }
                currentPoint = nextPoint;
            } while (currentPoint != startPoint);
            return hull;
        }
        private double IsLeft(XYZ p1, XYZ p2, XYZ p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        }
        ///// <summary>
        ///// 获取或创建用于可视化的图形样式
        ///// </summary>
        private GraphicsStyle GetOrCreateGraphicsStyle(Document doc, string styleName)
        {
            var cat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            var subCat = cat.SubCategories.get_Item(styleName);
            if (subCat == null)
            {
                subCat = doc.Settings.Categories.NewSubcategory(cat, styleName);
                subCat.LineColor = new Color(255, 0, 0); // 红色
                subCat.SetLineWeight(5, GraphicsStyleType.Projection);
            }
            return subCat.GetGraphicsStyle(GraphicsStyleType.Projection);
        }
        ///// <summary>
        ///// 查找最适合进行射线检测的3D视图
        ///// </summary>
        //private View3D FindBest3DView(Document doc)
        //{
        //    var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
        //    View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
        //    return default3DView ?? collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        //}
        ///// <summary>
        ///// 从楼板的实体几何体中提取其底面的所有轮廓环路。
        ///// </summary>
        ///// <param name="floor">要分析的楼板</param>
        ///// <returns>包含所有轮廓的CurveArray列表</returns>
        private List<CurveArray> GetFloorLoopsFromGeometry(Floor floor)
        {
            var loops = new List<CurveArray>();
            Options geomOptions = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, View = floor.Document.ActiveView };
            GeometryElement geoElem = floor.get_Geometry(geomOptions);
            if (geoElem == null) return null;
            Solid solid = geoElem.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);
            if (solid == null) return null;
            PlanarFace bottomFace = null;
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                PlanarFace pFace = face as PlanarFace;
                if (pFace != null && pFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ.Negate()))
                {
                    bottomFace = pFace;
                    break;
                }
            }
            if (bottomFace == null) return null;
            // *** 这是修正的核心部分 ***
            // bottomFace.EdgeLoops 返回 IList<EdgeArray>
            var edgeLoopList = bottomFace.EdgeLoops;
            // 遍历每个 EdgeArray (每个代表一个闭合环路)
            foreach (EdgeArray edgeArray in edgeLoopList)
            {
                // 为每个环路创建一个新的 CurveArray 来存放曲线
                CurveArray curveArray = new CurveArray();
                // 遍历环路中的每一条边 (Edge)
                foreach (Edge edge in edgeArray)
                {
                    // 从边中提取几何曲线 (Curve) 并添加到 CurveArray 中
                    curveArray.Append(edge.AsCurve());
                }
                // 将转换好的 CurveArray 添加到结果列表中
                loops.Add(curveArray);
            }
            return loops;
        }
        ///// <summary>
        ///// 确保两个元素被连接，并且第一个元素切割第二个元素。
        ///// </summary>
        private void EnsureJoinOrder(Document doc, Element cutter, Element cuttee)
        {
            if (!JoinGeometryUtils.AreElementsJoined(doc, cutter, cuttee))
            {
                try
                {
                    JoinGeometryUtils.JoinGeometry(doc, cutter, cuttee);
                }
                catch (Exception ex)
                {
                    // 记录连接失败的日志，对于调试很重要
                    System.Diagnostics.Debug.WriteLine($"无法连接元素 {cutter.Id} 和 {cuttee.Id}: {ex.Message}");
                }
            }
            else
            {
                // 如果已经连接，检查顺序是否正确
                if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, cutter, cuttee))
                {
                    try
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, cutter, cuttee);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"无法切换元素 {cutter.Id} 和 {cuttee.Id} 的连接顺序: {ex.Message}");
                    }
                }
            }
        }
        ///// <summary>
        ///// 获取与给定元素包围盒相交的元素（梁和楼板）。
        ///// </summary>
        private List<Element> GetIntersectingElements(Document doc, Element element, double expansionAmount)
        {
            // 使用 get_BoundingBox(null) 获取模型空间的完整3D包围盒
            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox == null) return new List<Element>();
            // 扩大包围盒以确保捕捉到所有接触的元素
            Outline outline = new Outline(bbox.Min - new XYZ(expansionAmount, expansionAmount, expansionAmount),
                                          bbox.Max + new XYZ(expansionAmount, expansionAmount, expansionAmount));
            // 使用 BoundingBoxIntersectsFilter 时，公差应非常小
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline, 1e-9);
            // 定义要查找的元素类别
            var categoryFilters = new List<ElementFilter>
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                new ElementCategoryFilter(BuiltInCategory.OST_Floors)
            };
            var logicalOrFilter = new LogicalOrFilter(categoryFilters);
            var finalFilter = new LogicalAndFilter(bbFilter, logicalOrFilter);
            return new FilteredElementCollector(doc).WherePasses(finalFilter)
                .Where(e => e.Id != element.Id).ToList();
        }
        ////1003 
        ///// <summary>
        ///// 查找一个最适合进行射线检测的3D视图。
        ///// 优先选择默认的 {3D} 视图，因为它通常包含所有模型元素。
        ///// </summary>
        ///// 重复方法回头看一下是否去重？
        private static View3D FindBest3DView(Document doc)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            // 优先寻找默认的 {3D} 视图
            View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
            if (default3DView != null)
            {
                return default3DView;
            }
            // 如果找不到，再寻找任何一个非模板的3D视图作为备用
            return collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        }
        /////代码解析
        ////输入：通过 PickPoint 和 PickObject(ObjectType.Face)，我们精确地获取了观察点和用户想要分析的那个面。这是最可靠的方式。
        ////采样：
        ////targetFace.GetBoundingBox() 获取了面的 UV 参数范围。
        ////通过双重 for 循环，我们在 UV 空间中均匀地创建了一个网格。
        ////targetFace.Evaluate(new UV(u, v)) 将二维的 UV 参数转换为了三维世界坐标 XYZ。
        ////可见性检测：
        ////ReferenceIntersector 的构造函数传入了 targetElement.Id，这是一个优化，它会忽略目标本身，防止射线总是命中自己。但为了逻辑的严谨性，我们还是通过比较距离来判断，这样更通用。
        ////distanceToFace 是原始距离，hitDistance 是碰撞距离。Math.Abs(hitDistance - distanceToFace) < tolerance 是判断是否命中了目标本身的关键。
        ////可视化：
        ////"最大最小可见范围" 的最佳数学表达是可见区域的轮廓。
        ////我提供了一个简化的 凸包（Convex Hull） 算法 FindConvexHull 来找到包围所有可见点的最小多边形。这个多边形就是可见区域的边界。
        ////DrawVisibilityResults 方法做了两件事：
        ////用模型线在标记牌上绘制出这个凸包轮廓。
        ////从观察点向凸包的每个顶点发射连线，形成一个视锥（Viewing Frustum），直观地展示了可见范围。
        ////为了让绘制的线更醒目，我写了一个 GetOrCreateGraphicsStyle 方法来创建一个红色的、较粗的线样式。
        ///// <summary>
        ///// 执行射线检测并返回最近的碰撞图元ID
        ///// </summary>
        ///// <param name="doc">Revit文档</param>
        ///// <param name="origin">射线起点</param>
        ///// <param name="direction">射线方向</param>
        ///// <param name="view">用于检测的视图（可选）</param>
        ///// <returns>碰撞到的第一个图元的ElementId，如果没有碰撞则返回ElementId.InvalidElementId</returns>
        //public static ElementId RaycastNearest(Document doc, XYZ origin, XYZ direction, double deltaHeight, Autodesk.Revit.DB.View view = null)
        //{
        //    // 规范化方向向量
        //    direction = direction.Normalize();
        //    // 创建ReferenceIntersector
        //    ReferenceIntersector intersector;
        //    if (view != null)
        //    {
        //        intersector = new ReferenceIntersector((View3D)view);
        //    }
        //    else
        //    {
        //        // 使用3D视图设置进行检测
        //        intersector = new ReferenceIntersector(Find3DView(doc) ?? throw new System.Exception("找不到可用的3D视图"));
        //    }
        //    // 设置查找最近的交点
        //    intersector.TargetType = FindReferenceTarget.Face;
        //    intersector.FindReferencesInRevitLinks = true;
        //    XYZ originptWithHeight = new XYZ(origin.X, origin.Y, deltaHeight / 304.8);
        //    // 执行射线检测
        //    ReferenceWithContext referenceWithContext = intersector.FindNearest(originptWithHeight, direction);
        //    //ReferenceWithContext referenceWithContext = intersector.FindNearest(origin, direction);
        //    if (referenceWithContext == null) return ElementId.InvalidElementId;
        //    // 获取碰撞图元的ElementId
        //    Reference reference = referenceWithContext.GetReference();
        //    return reference?.ElementId ?? ElementId.InvalidElementId;
        //}
        //private static View3D Find3DView(Document doc)
        //{
        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    collector.OfClass(typeof(View3D));
        //    foreach (View3D view in collector)
        //    {
        //        if (!view.IsTemplate && view.Name != "{3D}") return view;
        //    }
        //    return null;
        //}
        //Action<string> onSelected = selectedName =>
        //{
        //    Autodesk.Revit.UI.TaskDialog.Show("tt", selectedName);
        //};
        //public bool IsBoundingBoxContained(BoundingBoxXYZ container, BoundingBoxXYZ contained)
        //{
        //    // 检查 contained 的最小点是否在 container 内
        //    bool minContained = container.Min.X <= contained.Min.X &&
        //                        container.Min.Y <= contained.Min.Y &&
        //                        container.Min.Z <= contained.Min.Z;

        //    // 检查 contained 的最大点是否在 container 内
        //    bool maxContained = container.Max.X >= contained.Max.X &&
        //                        container.Max.Y >= contained.Max.Y &&
        //                        container.Max.Z >= contained.Max.Z;

        //    return minContained && maxContained;
        //}
        ///// <returns>如果在房间内则返回true，否则返回false</returns>
        //public bool IsAnyPartOfStairInRoom(Stairs stair, Room room, Document doc)
        //{
        //    // 1. 检查所有梯段 (StairsRun)
        //    foreach (ElementId runId in stair.GetStairsRuns())
        //    {
        //        Element runElem = doc.GetElement(runId);
        //        if (IsElementCenterInRoom(runElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"梯段 {runId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个梯段在，就返回true
        //        }
        //    }
        //    // 2. 检查所有平台 (StairsLanding)
        //    foreach (ElementId landingId in stair.GetStairsLandings())
        //    {
        //        Element landingElem = doc.GetElement(landingId);
        //        if (IsElementCenterInRoom(landingElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"平台 {landingId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个平台在，就返回true
        //        }
        //    }
        //    // 如果所有子构件都不在房间内，则认为整个楼梯不在
        //    return false;
        //}
        ///// <summary>
        ///// 辅助方法：检查一个元素的包围盒中心点是否在房间内。物体与房间关系
        ///// </summary>
        //private bool IsElementCenterInRoom(Element elem, Room room)
        //{
        //    if (elem == null || room == null) return false;
        //    BoundingBoxXYZ bbox = elem.get_BoundingBox(null); // 使用全局坐标，不依赖视图
        //    if (bbox == null || !bbox.Enabled) return false;
        //    XYZ centerPoint = (bbox.Min + bbox.Max) / 2.0;
        //    return room.IsPointInRoom(centerPoint);
        //}


        // 设置风口高度
        private void SetTerminalHeight(Element terminal, double heightMm)
        {
            // 将毫米转换为Revit内部单位（英尺）
            double height = heightMm / 304.8;
            // 获取高度参数
            Parameter elevationParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevationParam != null && elevationParam.IsReadOnly == false)
            {
                elevationParam.Set(height);
            }
            else
            {
                // 尝试其他可能的高度参数
                Parameter levelOffsetParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (levelOffsetParam != null && levelOffsetParam.IsReadOnly == false)
                {
                    levelOffsetParam.Set(height);
                }
                else
                {
                    // 尝试通过实例属性设置
                    Parameter offsetParam = terminal.LookupParameter("Offset");
                    if (offsetParam != null && offsetParam.IsReadOnly == false)
                    {
                        offsetParam.Set(height);
                    }
                }
            }
        }
    }
    /// <summary>
    /// 专业统计结果类
    /// </summary>
    public class ProfessionStatistic
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    /// <summary>
    /// 分析结果类
    /// </summary>
    public class ProfessionAnalysisResult
    {
        public int TotalElementCount { get; set; }
        public string PrimaryProfession { get; set; }
        public bool IsMultiDiscipline { get; set; }
        public Dictionary<BuiltInCategory, int> CategoryStatistics { get; set; }
        public Dictionary<string, ProfessionStatistic> ProfessionStatistics { get; set; }

        public ProfessionAnalysisResult()
        {
            CategoryStatistics = new Dictionary<BuiltInCategory, int>();
            ProfessionStatistics = new Dictionary<string, ProfessionStatistic>();
        }
    }
    /// <summary>
    /// 统计模型中各类别构件的数量，并分析模型的专业归属
    /// </summary>
    public class ModelProfessionAnalyzer
    {
        private readonly Document _doc;

        // 专业类别映射字典
        private readonly Dictionary<BuiltInCategory, string> _categoryToProfession;

        public ModelProfessionAnalyzer(Document doc)
        {
            _doc = doc;
            _categoryToProfession = InitializeCategoryMapping();
        }

        /// <summary>
        /// 初始化 BuiltInCategory 到专业的映射关系
        /// </summary>
        private Dictionary<BuiltInCategory, string> InitializeCategoryMapping()
        {
            return new Dictionary<BuiltInCategory, string>
        {
            // 建筑专业
            { BuiltInCategory.OST_Walls, "建筑" },
            { BuiltInCategory.OST_Doors, "建筑" },
            { BuiltInCategory.OST_Windows, "建筑" },
            { BuiltInCategory.OST_Rooms, "建筑" },
            { BuiltInCategory.OST_Floors, "建筑" },
            { BuiltInCategory.OST_Ceilings, "建筑" },
            { BuiltInCategory.OST_Stairs, "建筑" },
            { BuiltInCategory.OST_Ramps, "建筑" },
            { BuiltInCategory.OST_Railings, "建筑" },
            { BuiltInCategory.OST_CurtainWallMullions, "建筑" },
            { BuiltInCategory.OST_CurtainWallPanels, "建筑" },
            
            // 结构专业
            { BuiltInCategory.OST_StructuralColumns, "结构" },
            { BuiltInCategory.OST_StructuralFraming, "结构" },
            { BuiltInCategory.OST_StructuralFoundation, "结构" },
            { BuiltInCategory.OST_Rebar, "结构" },
            { BuiltInCategory.OST_Truss, "结构" },
            { BuiltInCategory.OST_StructuralBracePlanReps, "结构" },
            
            // 给排水专业
            { BuiltInCategory.OST_PipeCurves, "给排水" },
            { BuiltInCategory.OST_PipeFitting, "给排水" },
            { BuiltInCategory.OST_PipeAccessory, "给排水" },
            { BuiltInCategory.OST_PlumbingFixtures, "给排水" },
            { BuiltInCategory.OST_Sprinklers, "给排水" },
            
            // 暖通专业
            { BuiltInCategory.OST_DuctCurves, "暖通" },
            { BuiltInCategory.OST_DuctFitting, "暖通" },
            { BuiltInCategory.OST_DuctAccessory, "暖通" },
            { BuiltInCategory.OST_MechanicalEquipment, "暖通" },
            { BuiltInCategory.OST_DuctTerminal, "暖通" },
            { BuiltInCategory.OST_FlexDuctCurves, "暖通" },
            
            // 电气专业
            { BuiltInCategory.OST_Conduit, "电气" },
            { BuiltInCategory.OST_ConduitFitting, "电气" },
            { BuiltInCategory.OST_CableTray, "电气" },
            { BuiltInCategory.OST_CableTrayFitting, "电气" },
            { BuiltInCategory.OST_LightingFixtures, "电气" },
            { BuiltInCategory.OST_ElectricalEquipment, "电气" },
            { BuiltInCategory.OST_ElectricalFixtures, "电气" },
            { BuiltInCategory.OST_DataDevices, "电气" },
            { BuiltInCategory.OST_FireAlarmDevices, "电气" },
            { BuiltInCategory.OST_SecurityDevices, "电气" },
            { BuiltInCategory.OST_TelephoneDevices, "电气" },
            { BuiltInCategory.OST_Wire, "电气" },
            
            // 工艺专业
            { BuiltInCategory.OST_SpecialityEquipment, "工艺" },
            { BuiltInCategory.OST_GenericModel, "工艺" },
            { BuiltInCategory.OST_Entourage, "工艺" },
            
            // 其他通用类别（归入"其他"）
            { BuiltInCategory.OST_Levels, "其他" },
            { BuiltInCategory.OST_Grids, "其他" },
            { BuiltInCategory.OST_Views, "其他" },
            { BuiltInCategory.OST_Sheets, "其他" },
            { BuiltInCategory.OST_Materials, "其他" },
            { BuiltInCategory.OST_ElectricalLoadClassifications, "其他" },
            { BuiltInCategory.OST_ParamElemElectricalLoadClassification, "其他" },
            { BuiltInCategory.OST_HVAC_Load_Space_Types, "其他" },
            { BuiltInCategory.OST_PreviewLegendComponents, "其他" }
        };
        }

        /// <summary>
        /// 执行分析，返回各专业构件数量及占比
        /// </summary>
        public ProfessionAnalysisResult Analyze()
        {
            var result = new ProfessionAnalysisResult();

            // 获取所有实体元素（排除视图、图纸等非实体类别）
            var allElements = new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()  // 排除类型元素，只取实例
                .WhereElementIsViewIndependent() // 排除视图相关元素
                .ToElements();

            int totalCount = 0;
            var categoryCountMap = new Dictionary<BuiltInCategory, int>();
            var professionCountMap = new Dictionary<string, int>();

            // 初始化专业计数字典
            foreach (var profession in new[] { "建筑", "结构", "给排水", "暖通", "电气", "工艺", "其他" })
            {
                professionCountMap[profession] = 0;
            }

            foreach (var element in allElements)
            {
                // 获取元素的类别
                Category category = element.Category;
                if (category == null) continue;

                // 获取 BuiltInCategory 值
                BuiltInCategory bic = (BuiltInCategory)category.Id.IntegerValue;

                // 统计类别计数
                if (!categoryCountMap.ContainsKey(bic))
                    categoryCountMap[bic] = 0;
                categoryCountMap[bic]++;

                // 统计专业计数
                if (_categoryToProfession.TryGetValue(bic, out string profession))
                {
                    professionCountMap[profession]++;
                }
                else
                {
                    // 未映射的类别归入"其他"
                    professionCountMap["其他"]++;
                }

                totalCount++;
            }

            result.TotalElementCount = totalCount;
            result.CategoryStatistics = categoryCountMap;

            // 计算各专业占比
            foreach (var kvp in professionCountMap)
            {
                double percentage = totalCount > 0 ? (kvp.Value * 100.0 / totalCount) : 0;
                result.ProfessionStatistics.Add(kvp.Key, new ProfessionStatistic
                {
                    Count = kvp.Value,
                    Percentage = percentage
                });
            }

            // 确定模型的主要专业（占比最高的专业）
            result.PrimaryProfession = result.ProfessionStatistics
                .OrderByDescending(x => x.Value.Percentage)
                .First().Key;

            // 判断是否为综合模型（非主导专业占比超过15%）
            double topPercentage = result.ProfessionStatistics.Max(x => x.Value.Percentage);
            result.IsMultiDiscipline = topPercentage < 60;

            return result;
        }

        /// <summary>
        /// 获取详细的类别统计信息
        /// </summary>
        public string GetDetailedReport()
        {
            var result = Analyze();
            var report = new System.Text.StringBuilder();

            report.AppendLine("========== Revit 模型专业分析报告 ==========");
            report.AppendLine($"模型总构件数: {result.TotalElementCount}");
            report.AppendLine($"主要专业: {result.PrimaryProfession}");
            report.AppendLine($"是否综合模型: {(result.IsMultiDiscipline ? "是" : "否")}");
            report.AppendLine();
            report.AppendLine("各专业统计:");
            report.AppendLine("----------------------------------------");

            foreach (var stat in result.ProfessionStatistics.OrderByDescending(x => x.Value.Percentage))
            {
                report.AppendLine($"{stat.Key}: {stat.Value.Count} 个构件 ({stat.Value.Percentage:F2}%)");
            }

            report.AppendLine();
            report.AppendLine("主要类别明细 (Top 10):");
            report.AppendLine("----------------------------------------");

            var topCategories = result.CategoryStatistics
                .OrderByDescending(x => x.Value)
                .Take(10);

            foreach (var kvp in topCategories)
            {
                string categoryName = GetCategoryName(kvp.Key);
                report.AppendLine($"{categoryName}: {kvp.Value} 个");
            }

            return report.ToString();
        }

        /// <summary>
        /// 获取类别的显示名称
        /// </summary>
        private string GetCategoryName(BuiltInCategory bic)
        {
            try
            {
                Category category = Category.GetCategory(_doc, bic);
                return category?.Name ?? bic.ToString();
            }
            catch
            {
                return bic.ToString();
            }
        }
    }
}
