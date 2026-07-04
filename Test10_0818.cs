//ObserverableObject
//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);
// string message = string.Empty;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]//翻接机电管线 20260628
    public class MEPCurveTurnOver : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0628 选择翻弯类型
            TaskDialog td = new TaskDialog("选择翻弯类型")
            {
                MainInstruction = "请选择单侧翻弯（ 第二点变高）还是双侧翻弯（第二点继续原高度）:",
                CommonButtons = TaskDialogCommonButtons.Cancel
            };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "单侧翻弯（变高），仅单次");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "双侧连续翻弯（继续原高度）");
            TaskDialogResult tdRes = td.Show();
            if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
            bool useSingle = (tdRes == TaskDialogResult.CommandLink1);
            //// 翻弯方法
            var selectionList = new Dictionary<string, List<string>>
            {
                {  "向上翻", new List<string>{ "300","400","500","600","800","100","150","200","250"}},
                {  "向下翻", new List<string>{ "400","300","500","600","800","100","150","200","250"}}
            };
            UniversalDoubleComboboxWindow dialog = new UniversalDoubleComboboxWindow("设置翻弯参数", "1. 请选择翻弯方向:", "2. 请选择翻弯高度:", selectionList);
            if (dialog.ShowDialog() != true) return Result.Cancelled;
            bool upWard = dialog.SelectedItem1?.ToString() == "向上翻";
            double distanceMM = double.Parse(dialog.SelectedItem2?.ToString() ?? "0");
            // 翻弯高度转内部单位
            double height = distanceMM / 304.8;
            if (useSingle)
            {
                //// 以下执行单侧翻弯，不循环
                _externalHandler.Run(app =>
                {
                    uiDoc = app.ActiveUIDocument;
                    doc = uiDoc.Document;
                    //while (true) // 1. 创建无限循环
                    //{
                    try
                    {
                        Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass(), "请选择第一个打断点 (按ESC退出)");
                        Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass(), "请选择第一个打断点 (按ESC退出)");
                        // 如果点的不是同一根管或空值则不翻弯
                        if (ref1 == null || ref2 == null) return;
                        if (ref1.ElementId != ref2.ElementId)
                        {
                            TaskDialog.Show("提示", "两个拾取点必须在同一根管道/桥架上，请重新拾取。");
                            return;
                        }
                        // 取管1及打断点 
                        MEPCurve mEPCurve = doc.GetElement(ref1.ElementId) as MEPCurve;
                        if (mEPCurve == null || !mEPCurve.IsHorizontal()) return;
                        // 投影到中心线 
                        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
                        XYZ xyz1 = curve.Project(ref1.GlobalPoint).XYZPoint;
                        XYZ xyz2 = curve.Project(ref2.GlobalPoint).XYZPoint;
                        //// 确保 xyz1 在 xyz2 之前（沿管方向） 
                        //// 用参数排序，保证 xyz1 是更靠近管起点的那个
                        double param1 = curve.Project(xyz1).Parameter;
                        double param2 = curve.Project(xyz2).Parameter;
                        if (param1 > param2)
                        {
                            XYZ tmp = xyz1; xyz1 = xyz2; xyz2 = tmp;
                        }
                        // 计算两点间水平距离（用于校验和判断翻弯角度） 
                        double span = xyz1.DistanceTo(xyz2);
                        // 取主要尺寸（直径或宽度）用于最小距离校验 
                        double mainSizeFt = mEPCurve.GetMEPCurveMainSize();
                        // 最小翻弯净距：翻弯高度至少要大于管径，且两点间距至少要能放下两段立管+弯头
                        // 视翻弯难度可改为2 / 1
                        // 经验值：span >= 3 * mainSizeFt，height >= 1.5 x mainSizeFt
                        double minSpan = 3.0 * mainSizeFt;
                        double minHeight = 1.5 * mainSizeFt;
                        if (span < minSpan)
                        {
                            TaskDialog.Show("校验失败",
                                $"两拾取点间距 {span * 304.8:F0}mm 过小（最小 {minSpan * 304.8:F0}mm），无法翻弯。");
                            return;
                        }
                        if (height < minHeight)
                        {
                            TaskDialog.Show("校验失败",
                                $"翻弯高度 {distanceMM:F0}mm 小于管道主尺寸 {mainSizeFt * 304.8:F0}mm，无法翻弯。");
                            return;
                        }
                        // ---- 判断翻弯角度 ----
                        // 45度条件：height <= span/2（几何上能构成45度斜边）
                        // 90度条件：height > span/2，直接垂直上去再水平过去
                        bool use90 = (height > span / 2.0);
                        // ---- 方向向量 ----
                        XYZ vertDir = upWard ? XYZ.BasisZ : XYZ.BasisZ.Negate();
                        XYZ horzDir = (xyz2 - xyz1).Normalize(); // 水平方向（管轴方向）
                        XYZ vEnd1;
                        if (use90)
                        {
                            // 90度：立管垂直上升 height，顶部水平连接
                            vEnd1 = xyz1 + vertDir * height;
                        }
                        else
                        {
                            // 此处还应该复核xyz1,xyz2各自到mepcurve两端点距离要大于1.5 x mainSizeFt
                            double minEdgeDistance = 1.5 * mainSizeFt;
                            // 获取曲线端点
                            XYZ startPoint = curve.GetEndPoint(0);
                            XYZ endPoint = curve.GetEndPoint(1);
                            double dist1ToStart = xyz1.DistanceTo(startPoint);
                            double dist1ToEnd = xyz1.DistanceTo(endPoint);
                            double minDist1 = Math.Min(dist1ToStart, dist1ToEnd);
                            if (minDist1 < minEdgeDistance)
                            {
                                TaskDialog.Show("校验失败", $"第一个打断点距离管端太近（{minDist1.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
                                return;
                            }
                            // 45度：斜管以45度角上升，水平偏移 = 竖直高度
                            // 斜管水平分量 = height（45度时水平=垂直）
                            // 为了保证几何可行，水平分量不能超过 span/2
                            double hOffset = height; // 45度时水平分量等于竖直分量
                            vEnd1 = xyz1 + vertDir * height + horzDir * hOffset;
                            // 注意：vEnd2 是从 xyz2 向反方向退 hOffset
                        }
                        // ==================== 计算2个关键点 ====================
                        //
                        //  90度翻弯示意（向上）:
                        //
                        //          vEnd1 ----mepNeo 
                        //            |                  
                        //          vPipe1             
                        //            |                  
                        //  ---mep---xyz1              
                        //
                        //  45度翻弯示意（向上）:
                        //
                        //               vMid1 ----mepNeo---- 
                        //              /                         
                        //         45Pipe1                       
                        //            /                             
                        //  ---mep--xyz1                           
                        //
                        // ======================================================
                        using (var trans = new Transaction(doc, "两点翻弯"))
                        {
                            try
                            {
                                trans.Start();
                                // 1. 在 xyz1 处打断原管
                                MEPCurve mepSeg1 = mEPCurve.BreakMEPCurveByOne(xyz1);
                                // 单翻时，修改逻辑
                                // 移动mepSeg1到指定位置高度
                                ElementTransformUtils.MoveElement(doc, mepSeg1.Id, vertDir * height);
                                //// 4. 根据管类型创建翻弯管段
                                if (mEPCurve is Pipe pipe)
                                {
                                    var pipe2 = mepSeg1 as Pipe;
                                    CreatePipeBendConnector(doc, pipe, pipe2, xyz1, vEnd1);
                                }
                                else if (mEPCurve is Duct duct)
                                {
                                    var duct2 = mepSeg1 as Duct;
                                    CreateDuctBendConnector(doc, duct, duct2, xyz1, vEnd1);
                                }
                                else if (mEPCurve is CableTray cableTray)
                                {
                                    var cableTray2 = mepSeg1 as CableTray;
                                    CreateCableTrayBendConnector(doc, cableTray, cableTray2, xyz1, vEnd1);
                                }
                                else return;
                                trans.Commit();
                            }
                            catch (Exception exInner)
                            {
                                trans.RollBack();
                                TaskDialog.Show("翻弯事务错误", $"操作失败：{exInner.Message}\n{exInner.StackTrace}");
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // 用户按了 ESC 键取消操作
                        return;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("翻弯内部事务发生错误", $"操作失败：{ex.Message}");
                        return;
                    }
                    //}
                });
            }
            else
            {
                // 开始以下循环执行双侧翻弯
                _externalHandler.Run(app =>
                {
                    uiDoc = app.ActiveUIDocument;
                    doc = uiDoc.Document;
                    //while (true) // 1. 创建无限循环
                    //{
                    try
                    {
                        Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(), "请选择第一个打断点 (按ESC退出)");
                        Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(), "请选择第一个打断点 (按ESC退出)");
                        // 如果点的不是同一根管或空值则不翻弯
                        if (ref1 == null || ref2 == null) return;
                        if (ref1.ElementId != ref2.ElementId)
                        {
                            TaskDialog.Show("提示", "两个拾取点必须在同一根管道/桥架上，请重新拾取。");
                            return;
                            //continue;
                        }
                        // 取管1及打断点 
                        MEPCurve mEPCurve = doc.GetElement(ref1.ElementId) as MEPCurve;
                        if (mEPCurve == null || !mEPCurve.IsHorizontal()) return;
                        // 投影到中心线 
                        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
                        XYZ xyz1 = curve.Project(ref1.GlobalPoint).XYZPoint;
                        XYZ xyz2 = curve.Project(ref2.GlobalPoint).XYZPoint;
                        //// 确保 xyz1 在 xyz2 之前（沿管方向） 
                        //// 用参数排序，保证 xyz1 是更靠近管起点的那个
                        double param1 = curve.Project(xyz1).Parameter;
                        double param2 = curve.Project(xyz2).Parameter;
                        if (param1 > param2)
                        {
                            XYZ tmp = xyz1; xyz1 = xyz2; xyz2 = tmp;
                        }
                        // 计算两点间水平距离（用于校验和判断翻弯角度） 
                        double span = xyz1.DistanceTo(xyz2);
                        // 取主要尺寸（直径或宽度）用于最小距离校验 
                        double mainSizeFt = mEPCurve.GetMEPCurveMainSize();
                        // 最小翻弯净距：翻弯高度至少要大于管径，且两点间距至少要能放下两段立管+弯头
                        // 视翻弯难度可改为2 / 1
                        // 经验值：span >= 3 * mainSizeFt，height >= 1.5 x mainSizeFt
                        double minSpan = 3.0 * mainSizeFt;
                        double minHeight = 1.5 * mainSizeFt;
                        if (span < minSpan)
                        {
                            TaskDialog.Show("校验失败",
                                $"两拾取点间距 {span * 304.8:F0}mm 过小（最小 {minSpan * 304.8:F0}mm），无法翻弯。");
                            return;
                            //continue;
                        }
                        if (height < minHeight)
                        {
                            TaskDialog.Show("校验失败",
                                $"翻弯高度 {distanceMM:F0}mm 小于管道主尺寸 {mainSizeFt * 304.8:F0}mm，无法翻弯。");
                            return;
                            //continue;
                        }
                        // ---- 判断翻弯角度 ----
                        // 45度条件：height <= span/2（几何上能构成45度斜边）
                        // 90度条件：height > span/2，直接垂直上去再水平过去
                        bool use90 = (height > span / 2.0);
                        // ---- 方向向量 ----
                        XYZ vertDir = upWard ? XYZ.BasisZ : XYZ.BasisZ.Negate();
                        XYZ horzDir = (xyz2 - xyz1).Normalize(); // 水平方向（管轴方向）
                        XYZ vEnd1, vEnd2;
                        if (use90)
                        {
                            // 90度：立管垂直上升 height，顶部水平连接
                            vEnd1 = xyz1 + vertDir * height;
                            vEnd2 = xyz2 + vertDir * height;
                        }
                        else
                        {
                            // 此处还应该复核xyz1,xyz2各自到mepcurve两端点距离要大于1.5 x mainSizeFt
                            double minEdgeDistance = 1.5 * mainSizeFt;
                            // 获取曲线端点
                            XYZ startPoint = curve.GetEndPoint(0);
                            XYZ endPoint = curve.GetEndPoint(1);
                            double dist1ToStart = xyz1.DistanceTo(startPoint);
                            double dist1ToEnd = xyz1.DistanceTo(endPoint);
                            double minDist1 = Math.Min(dist1ToStart, dist1ToEnd);
                            if (minDist1 < minEdgeDistance)
                            {
                                TaskDialog.Show("校验失败", $"第一个打断点距离管端太近（{minDist1.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
                                return;
                                //continue;
                            }
                            // === 新增：检查 xyz2 到两端点的距离 ===
                            double dist2ToStart = xyz2.DistanceTo(startPoint);
                            double dist2ToEnd = xyz2.DistanceTo(endPoint);
                            double minDist2 = Math.Min(dist2ToStart, dist2ToEnd);
                            if (minDist2 < minEdgeDistance)
                            {
                                TaskDialog.Show("校验失败", $"第二个打断点距离管端太近（{minDist2.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
                                return;
                                //continue;
                            }
                            // 45度：斜管以45度角上升，水平偏移 = 竖直高度
                            // 斜管水平分量 = height（45度时水平=垂直）
                            // 为了保证几何可行，水平分量不能超过 span/2
                            double hOffset = height; // 45度时水平分量等于竖直分量
                            vEnd1 = xyz1 + vertDir * height + horzDir * hOffset;
                            vEnd2 = xyz2 + vertDir * height - horzDir * hOffset;
                            // 注意：vEnd2 是从 xyz2 向反方向退 hOffset
                        }
                        // ==================== 计算4个关键点 ====================
                        //
                        //  90度翻弯示意（向上）:
                        //
                        //          vEnd1 ----hPipe---- vEnd2
                        //            |                  |
                        //          vPipe1             vPipe2
                        //            |                  |
                        //  ---mep---xyz1              xyz2---mepNeo---
                        //
                        //  45度翻弯示意（向上）:
                        //
                        //         vMid1 ----hPipe---- vMid2
                        //        /                        \
                        //   45Pipe1                      45Pipe2
                        //      /                              \
                        //  ---xyz1                           xyz2---
                        //
                        // ======================================================
                        using (var trans = new Transaction(doc, "两点翻弯"))
                        {
                            try
                            {
                                trans.Start();
                                // 1. 在 xyz1 处打断原管
                                MEPCurve mepSeg1 = mEPCurve.BreakMEPCurveByOne(xyz1);
                                // 打断后：mEPCurve = [原起点 → xyz1]，mepSeg1 = [xyz1 → 原终点]
                                // 2. 在 xyz2 处打断 mepSeg1
                                MEPCurve mepSeg2 = mepSeg1.BreakMEPCurveByOne(xyz2);
                                // 打断后：mepSeg1 = [xyz1 → xyz2]（中间段，将被翻弯替代）
                                //         mepSeg2 = [xyz2 → 原终点]
                                // 3. 删除中间段
                                doc.Delete(mepSeg1.Id);
                                //以上为打断并删除中段代码
                                //// 4. 根据管类型创建翻弯管段
                                if (mEPCurve is Pipe pipe)
                                {
                                    var pipe2 = mepSeg2 as Pipe;
                                    CreatePipeOffset(doc, pipe, pipe2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
                                }
                                else if (mEPCurve is Duct duct)
                                {
                                    var duct2 = mepSeg2 as Duct;
                                    CreateDuctOffset(doc, duct, duct2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
                                }
                                else if (mEPCurve is CableTray cableTray)
                                {
                                    var cableTray2 = mepSeg2 as CableTray;
                                    CreateCableTrayOffset(doc, cableTray, cableTray2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
                                }
                                else return;
                                trans.Commit();
                            }
                            catch (Exception exInner)
                            {
                                trans.RollBack();
                                TaskDialog.Show("翻弯事务错误", $"操作失败：{exInner.Message}\n{exInner.StackTrace}");
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // 用户按了 ESC 键取消操作
                        return;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("翻弯内部事务发生错误", $"操作失败：{ex.Message}");
                        return;
                    }
                    //}
                });
            }
            return Result.Succeeded;
        }
        // ==================== 管道双向翻弯 （4个弯头） ====================
        private void CreatePipeOffset(Document doc, Pipe refPipe, Pipe refPipe2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
        {
            ElementId systemTypeId = refPipe.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
            ElementId pipeTypeId = refPipe.GetTypeId();
            ElementId levelId = refPipe.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
            double diameter = refPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            // 创建立管/斜管1
            Pipe leg1 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, xyz1, vEnd1);
            leg1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            // 创建顶部横管
            Pipe top = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, vEnd1, vEnd2);
            top.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            // 创建立管/斜管2
            Pipe leg2 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, vEnd2, xyz2);
            leg2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            // 创建弯头连接
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
                doc.Create.NewElbowFitting(refPipe.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
                doc.Create.NewElbowFitting(refPipe2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
            }
            catch { }
            // mepSeg2 已在调用方处理，此处连接 leg2 尾端到 mepSeg2 起端
            // （Revit 在创建管段时会自动对齐，可不手动连接；如需手动则在调用方补充）
        }
        // ==================== 风管双向翻弯 （4个弯头） ====================
        private void CreateDuctOffset(Document doc, Duct refDuct, Duct refDuct2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
        {
            ElementId systemTypeId = refDuct.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
            ElementId ductTypeId = refDuct.GetTypeId();
            ElementId levelId = refDuct.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
            double width = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
            double dHeight = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
            Duct leg1 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, xyz1, vEnd1);
            Duct top = Duct.Create(doc, systemTypeId, ductTypeId, levelId, vEnd1, vEnd2);
            Duct leg2 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, vEnd2, xyz2);
            // 设置截面尺寸
            foreach (Duct d in new[] { leg1, top, leg2 })
            {
                d.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.Set(width);
                d.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.Set(dHeight);
            }
            // 创建弯头
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
                doc.Create.NewElbowFitting(refDuct.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
                doc.Create.NewElbowFitting(refDuct2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
            }
            catch { }
        }
        // ==================== 桥架双向翻弯 （4个弯头） ====================
        private void CreateCableTrayOffset(Document doc, CableTray refTray, CableTray refTray2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
        {
            ElementId trayTypeId = refTray.GetTypeId();
            ElementId levelId = refTray.ReferenceLevel?.Id ?? ElementId.InvalidElementId;

            double width = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
            double tHeight = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();

            CableTray leg1 = CableTray.Create(doc, trayTypeId, xyz1, vEnd1, levelId);
            CableTray top = CableTray.Create(doc, trayTypeId, vEnd1, vEnd2, levelId);
            CableTray leg2 = CableTray.Create(doc, trayTypeId, vEnd2, xyz2, levelId);

            foreach (CableTray ct in new[] { leg1, top, leg2 })
            {
                ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.Set(width);
                ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.Set(tHeight);
            }

            // 桥架弯头：通过连接器连接，Revit自动插入弯头配件
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
                doc.Create.NewElbowFitting(refTray.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
                doc.Create.NewElbowFitting(refTray2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
            }
            catch { }
        }
        // ==================== 单翻管道连接件（2个弯头）====================
        private void CreatePipeBendConnector(Document doc, Pipe refPipe, Pipe refPipe2, XYZ xyz1, XYZ vEnd1)
        {
            ElementId systemTypeId = refPipe.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
            ElementId pipeTypeId = refPipe.GetTypeId();
            ElementId levelId = refPipe.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
            double diameter = refPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            // 创建立管/斜管1
            Pipe leg1 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, xyz1, vEnd1);
            leg1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            Pipe top = refPipe2;
            // 创建弯头连接
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(refPipe.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
            }
            catch { }
        }
        // ==================== 单翻风管连接件（2个弯头）====================
        private void CreateDuctBendConnector(Document doc, Duct refDuct, Duct refDuct2, XYZ xyz1, XYZ vEnd1)
        {
            ElementId systemTypeId = refDuct.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
            ElementId ductTypeId = refDuct.GetTypeId();
            ElementId levelId = refDuct.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
            double width = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
            double dHeight = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
            Duct leg1 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, xyz1, vEnd1);
            Duct top = refDuct2;
            // 设置截面尺寸
            leg1.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.Set(width);
            leg1.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.Set(dHeight);
            // 创建弯头
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(refDuct.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
            }
            catch { }
        }
        // ==================== 单翻桥架连接件（2个弯头）====================
        private void CreateCableTrayBendConnector(Document doc, CableTray refTray, CableTray refTray2, XYZ xyz1, XYZ vEnd1)
        {
            ElementId trayTypeId = refTray.GetTypeId();
            ElementId levelId = refTray.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
            double width = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
            double tHeight = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
            CableTray leg1 = CableTray.Create(doc, trayTypeId, xyz1, vEnd1, levelId);
            CableTray top = refTray2;
            leg1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.Set(width);
            leg1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.Set(tHeight);
            // 桥架弯头：通过连接器连接，Revit自动插入弯头配件
            try
            {
                doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
                doc.Create.NewElbowFitting(refTray.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
            }
            catch { }
        }
    }
    [Transaction(TransactionMode.Manual)]//L连接水管道 20260628
    public class PipeBendConnect : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            BentConnect(uiDoc);
            return Result.Succeeded;
        }
        public void BentConnect(UIDocument uiDoc)
        {
            Document doc = uiDoc.Document;
            //////要先选管，默认已选的不作数，不考虑重复执行命令
            try
            {
                // 1. 获取并预检第一根管道
                if (!TryGetAndValidatePipe(uiDoc, "请选择第一根管道", out Pipe pipe1, out Connector conn1, out Line line1))
                    //return Result.Cancelled;
                    return;
                // 2. 获取弯头信息和用户选择的连接策略
                if (!pipe1.TryGetFittingAndStrategy(out string strategy))
                    //return Result.Cancelled;
                    return;
                // 3. 获取并预检第二根管道
                if (!TryGetAndValidatePipe(uiDoc, "请选择第二根管道", out Pipe pipe2, out Connector conn2, out Line line2))
                    //return Result.Cancelled;
                    return;
                // 4. 最终校验两根管道的相对关系，单位：英尺
                if (!conn1.Origin.ValidatePointsDistance(conn2.Origin, 0.04, 6))
                    //return Result.Cancelled;
                    return;
                // 5. 根据几何关系执行连接操作
                using (var trans = new Transaction(doc, "管道L型连接"))
                {
                    trans.Start();
                    bool success = false;
                    if (line1.IsParallelTo(line2))
                    {
                        success = ConnectParallelPipes(pipe1, conn1, line1, pipe2, conn2, line2, strategy);
                    }
                    else
                    {
                        success = ConnectNonParallelPipes(pipe1, conn1, line1, pipe2, conn2, line2, strategy);
                    }
                    if (success)
                    {
                        trans.Commit();
                        //return Result.Succeeded;
                    }
                    else
                    {
                        trans.RollBack();
                        //return Result.Cancelled;
                        return;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户按了 ESC 键取消操作
                //return Result.Cancelled;
                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", ex.Message);
                //return Result.Cancelled;
                return;
            }
        }
        public bool TryGetAndValidatePipe(UIDocument uiDoc, string prompt, out Pipe pipe, out Connector connector, out Line line)
        {
            pipe = null;
            connector = null;
            line = null;

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), prompt);
            if (reference == null) return false;
            pipe = uiDoc.Document.GetElement(reference) as Pipe;
            XYZ pickPoint = reference.GlobalPoint;
            if (pipe == null) return false;
            if (pipe.IsSlopeGreaterThan(0.02))
            {
                TaskDialog.Show("限制", "暂不支持坡度过大的管道连接，请手工调整。");
                return false;
            }
            if (!(pipe.Location is LocationCurve lc) || !(lc.Curve is Line l))
            {
                TaskDialog.Show("限制", "仅支持直线管道。");
                return false;
            }
            line = l;
            connector = pipe.GetClosestConnector(pickPoint);
            return connector != null;
        }
        public bool ConnectParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
        {
            Document doc = p1.Document;
            // 分支 1: 共线管道
            if (l1.IsCollinear(l2))
            {
                // 共线：直接连接或变径
                if (Math.Abs(p1.Diameter - p2.Diameter) > 1e-6)
                {
                    doc.Create.NewTransitionFitting(c1, c2);
                }
                else
                {
                    doc.MergeTwoPipes(p1, c1, p2, c2);
                }
                return true;
            }
            // 分支 2: 平行、共面但不共线
            if (!l1.AreLinesCoPlanar(l2, 1e-6))
            {
                TaskDialog.Show("限制", "平行的两根管道不共面，无法自动连接。");
                return false;
            }
            // 核心逻辑: 在共面不共线的两线之间创建S弯连接
            return CreateS_BendConnection(p1, c1, p2, c2, strategy);
            //return true;
        }
        public bool ConnectNonParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
        {
            Document doc = p1.Document;
            if (l1.AreLinesCoPlanar(l2))
            {
                // 相交且共面：直接创建弯头
                p1.NewElbowBy2MEPCurve(p2);
                return true;
            }
            // 异面：创建立管连接
            var intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(l1, l2);
            if (intersection2D == null || intersection2D.DistanceTo(c1.Origin) > 4 || intersection2D.DistanceTo(c2.Origin) > 4)
            {
                TaskDialog.Show("限制", "管道在平面上交点过远，请手工调整。");
                return false;
            }
            double z1 = c1.Origin.Z;
            double z2 = c2.Origin.Z;
            // 调整原管道至交点
            p1.AdjustMEPCurveLength(c1.Origin, -c1.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z1)));
            p2.AdjustMEPCurveLength(c2.Origin, -c2.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z2)));
            doc.Regenerate();
            // 创建立管
            Pipe verticalPipe = TryCreateVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
            if (verticalPipe == null) return false;
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
            doc.Regenerate();
            // 连接
            p1.NewElbowBy2MEPCurve(verticalPipe);
            p2.NewElbowBy2MEPCurve(verticalPipe);
            return true;
        }
        //基于参照管，两连接器高差，交点建立垂直立管，可复用
        public Pipe TryCreateVerticalPipe(Pipe p1, Connector c1, XYZ cp2, XYZ intersection2D, string strategy)
        {
            Document doc = p1.Document;
            double pipeDiameter = p1.Diameter;
            double heightDifference = Math.Abs(c1.Origin.Z - cp2.Z);
            double requiredMultiplier = 0;
            if (strategy == "高概率")
            {
                requiredMultiplier = 6;
            }
            else if (strategy == "中概率")
            {
                requiredMultiplier = 4;
            }
            double minRequiredHeight = pipeDiameter * requiredMultiplier;
            // 2. 检查实际高差是否满足要求
            if (heightDifference < minRequiredHeight)
            {
                // 高差不足，不满足创建条件。直接返回null，由调用者决定是否提示用户。
                TaskDialog.Show("tt", $"创建立管失败：实际高差 {heightDifference * 304.8:F3} < 所需最小高差 {minRequiredHeight * 304.8:F3} (策略: {strategy})");
                return null;
            }
            double z1 = c1.Origin.Z;
            double z2 = cp2.Z;
            if (Math.Abs(z1 - z2) < 0.01) // 0.01 feet
            {
                TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。"); return null;
            }
            double minZ = Math.Min(z1, z2);
            double maxZ = Math.Max(z1, z2);
            XYZ bottomPoint = new XYZ(intersection2D.X, intersection2D.Y, minZ);
            XYZ topPoint = new XYZ(intersection2D.X, intersection2D.Y, maxZ);
            Pipe verticalPipe = p1.NewPipeBetweenPoints(bottomPoint, topPoint);
            return verticalPipe;
        }
        //// 为两根平行、共面但不共线的管道创建S型连接。
        public bool CreateS_BendConnection(Pipe p1, Connector c1, Pipe p2, Connector c2, string strategy)
        {
            double deltaZ = Math.Abs(c1.Origin.Z - c2.Origin.Z);
            double diameter = p1.Diameter;
            Pipe connectingPipe = null;
            //高差大于50小于200且2倍DN微差连接，后退连接器，根据连接器高度生成斜管再连接
            if (deltaZ < p1.Diameter * 2 || deltaZ < 200 / 304.8)
            {
                if (deltaZ < (60 / 304.8))
                {
                    TaskDialog.Show("tt", "检测到管道差过小，请手工调整");
                    return false;
                }
                ////管道连接器后退指定距离，需要考虑管长不能为0或负值
                double retreatDistance = p1.Diameter * 3;
                if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
                {
                    TaskDialog.Show("限制", "管道长度不足，无法后退创建连接。");
                    return false;
                }
                XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
                if (newConn1p == null)
                {
                    TaskDialog.Show("tt", "后退管道失败，无法创建连接。");
                    return false;
                }
                connectingPipe = p1.NewPipeBetweenPoints(newConn1p, c2.Origin);
            }
            //45度连接 默认高差4倍DN
            else if (deltaZ < p1.Diameter * 4)
            {
                double retreatDistance = Math.Abs(c1.Origin.Z - c2.Origin.Z);
                if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
                {
                    TaskDialog.Show("限制", "管道长度不足以创建45度连接。");
                    return false;
                }
                // (保留原始几何逻辑)
                double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
                XYZ tempPoint = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
                if (tempPoint == null)
                {
                    TaskDialog.Show("tt", "步骤1失败：调整管道长度以对齐失败。");
                    return false;
                }
                // 在新点上再次后退
                XYZ finalPoint = p1.AdjustMEPCurveLength(tempPoint, retreatDistance);
                if (finalPoint == null)
                {
                    TaskDialog.Show("tt", "步骤2失败：为连接管预留空间失败。");
                    return false;
                }
                // 创建最终的斜管
                connectingPipe = p1.NewPipeBetweenPoints(finalPoint, c2.Origin);
            }
            //90度连接
            else if (deltaZ >= p1.Diameter * 4)
            {
                double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
                XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
                if (newConn1p == null)
                {
                    TaskDialog.Show("tt", "调整管道长度以对齐失败，无法创建立管。");
                    return false;
                }
                XYZ intersection2D = new XYZ(c1.Origin.X, c1.Origin.Y, 0);
                connectingPipe = MEPAnalysisExtension.NewVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
            }
            if (connectingPipe == null)
            {
                return false;
            }
            // 步骤3: 执行统一的连接操作
            p1.NewElbowBy2MEPCurve(connectingPipe);
            p2.NewElbowBy2MEPCurve(connectingPipe);
            return true;
        }
        //建立斜坡度管连接
        public Pipe TryCreateSlopePipe(Pipe p1, Connector c1, Connector c2, double retreatDistance)
        {
            XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
            if (newConn1p == null)
            {
                TaskDialog.Show("tt", "未成功建立连接，请手工调整");
                return null;
            }
            //在管2和退后的连接器之间画新管，新管以管1类型，尺寸为准
            Pipe newPipe = Pipe.Create(p1.Document, p1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId(), p1.PipeType.Id, p1.ReferenceLevel.Id, newConn1p, c2.Origin);
            newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
            return newPipe;
        }

    }
    [Transaction(TransactionMode.Manual)]//T连接水管道 20260628
    public class PipeTripleConnect : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            TripleConnect(uiDoc);
            return Result.Succeeded;
        }
        public void TripleConnect(UIDocument uiDoc)
        {
            Document doc = uiDoc.Document;
            try
            {
                // 1. 获取并预检第一根管道（主管）
                if (!TryGetAndValidatePipe(uiDoc, "请选择主管（将被打断）", out Pipe mainPipe, out Connector mainConn, out Line mainLine))
                    //return Result.Cancelled;
                    return;
                // 2. 获取连接策略（主要用于立管连接）
                if (!mainPipe.TryGetFittingAndStrategy(out string strategy))
                    //return Result.Cancelled;
                    return;
                // 3. 获取并预检第二根管道（支管）
                if (!TryGetAndValidatePipe(uiDoc, "请选择支管（将连接到主管）", out Pipe branchPipe, out Connector branchConn, out Line branchLine))
                    //return Result.Cancelled;
                    return;
                // 4. 校验两根管道的相对关系，单位：英尺
                if (!mainConn.Origin.ValidatePointsDistance(branchConn.Origin, 0.04, 6))
                    //return Result.Cancelled;
                    return;
                // 5. 检查管径，通常支管不应大于主管
                if (branchPipe.Diameter > mainPipe.Diameter)
                {
                    TaskDialog.Show("警告", "支管管径大于主管，可能无法创建标准三通。程序将继续尝试。");
                }
                // 6. 根据几何关系执行T型连接操作
                using (var trans = new Transaction(doc, "管道T型连接"))
                {
                    trans.Start();
                    bool success = ConnectTeePipes(mainPipe, mainLine, branchPipe, branchConn, branchLine, strategy);
                    if (success)
                    {
                        trans.Commit();
                        //return Result.Succeeded;
                    }
                    else
                    {
                        // 如果失败，相应的子方法已经给出了提示
                        trans.RollBack();
                        //return Result.Cancelled;
                        return;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户按了 ESC 键取消操作
                //return Result.Cancelled;
                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", ex.Message);
                //return Result.Cancelled;
                return;
            }
        }
        // 获取并验证管道，T连接或十字连接可能共用
        public bool TryGetAndValidatePipe(UIDocument uiDoc, string prompt, out Pipe pipe, out Connector connector, out Line line)
        {
            pipe = null;
            connector = null;
            line = null;

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), prompt);
            if (reference == null) return false;
            pipe = uiDoc.Document.GetElement(reference) as Pipe;
            XYZ pickPoint = reference.GlobalPoint;
            if (pipe == null) return false;
            if (pipe.IsSlopeGreaterThan(0.02))
            {
                TaskDialog.Show("限制", "暂不支持坡度过大的管道连接，请手工调整。");
                return false;
            }
            if (!(pipe.Location is LocationCurve lc) || !(lc.Curve is Line l))
            {
                TaskDialog.Show("限制", "仅支持直线管道。");
                return false;
            }
            line = l;
            connector = pipe.GetClosestConnector(pickPoint);
            return connector != null;
        }
        // T型连接的核心调度方法
        public bool ConnectTeePipes(Pipe mainPipe, Line mainLine, Pipe branchPipe, Connector branchConn, Line branchLine, string strategy)
        {
            Document doc = mainPipe.Document;
            // Rule 3: 判断管关系，平行管无论是否共线、共面均退出
            if (mainLine.IsParallelTo(branchLine))
            {
                TaskDialog.Show("限制", "T型连接不支持两根平行的管道。");
                return false;
            }
            // 计算两根无限长直线在XY平面上的交点
            XYZ intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(mainLine, branchLine);
            if (intersection2D == null)
            {
                // 理论上不会发生，因为已经排除了平行情况
                return false;
            }
            // 将2D交点提升到主管的高度，得到空间中的打断点
            XYZ breakPointOnMain = new XYZ(intersection2D.X, intersection2D.Y, mainLine.Origin.Z);
            // 判断1: 交点是否在主管的物理范围内?
            bool isBreakPointOnMainSegment = mainLine.IsPointOnLine(breakPointOnMain);
            // 判断2: 交点是否也在支管的物理范围内?
            // (将交点投影到支管高度来判断)
            XYZ breakPointOnBranch = new XYZ(intersection2D.X, intersection2D.Y, branchLine.Origin.Z);
            bool isBreakPointOnBranchSegment = branchLine.IsPointOnLine(breakPointOnBranch);
            // 如果交点不在主管上，则无法进行任何T型连接
            if (!isBreakPointOnMainSegment)
            {
                TaskDialog.Show("限制", "管道投影交点不在主管的物理范围内。");
                return false;
            }
            //判断管关系，共面管道直接生成三通
            if (mainLine.AreLinesCoPlanar(branchLine))
            {
                //调整支管长度，使其端点精确到达打断点
                double distToIntersection = branchConn.Origin.DistanceTo(breakPointOnMain);
                branchPipe.AdjustMEPCurveLength(branchConn.Origin, -distToIntersection);
                if (BreakPipeAndCreateTee(doc, mainPipe, breakPointOnMain, branchPipe))
                {
                    return true;
                }
                return false;
            }
            // Rule 5: 不共面管道，根据高差创建连接
            else
            {
                return ConnectSkewTee(mainPipe, breakPointOnMain, branchPipe, branchConn, strategy, isBreakPointOnBranchSegment);
            }
        }
        //处理不共面（异面）管道的T型连接
        public bool ConnectSkewTee(Pipe mainPipe, XYZ breakPoint, Pipe branchPipe, Connector branchConn, string strategy, bool useCrossConnectionLogic)
        {
            Document doc = mainPipe.Document;
            // 计算高差：支管端点与它在主管上投影点的高度差
            double deltaZ = Math.Abs(branchConn.Origin.Z - breakPoint.Z);
            double diameter = branchPipe.Diameter;

            // --- 场景1: 高差足够大，且几何关系为“交叉” ---
            // 这是我们新增的智能分支
            if (deltaZ > diameter * 6 && useCrossConnectionLogic)
            {
                //TaskDialog.Show("智能连接提示", "检测到交叉管道关系，将使用立管和双三通进行连接。");
                //// 直接调用我们封装好的交叉连接方法
                return ConnectCrossPipes(mainPipe, branchPipe);
            }
            // --- 场景2: 其他所有异面情况 (高差小 或 几何关系非“交叉”) ---
            // 走原有的“三通+弯头”或“三通+斜管”逻辑
            // 检查支管端头距离投影点是否过远
            if (breakPoint.DistanceTo(branchConn.Origin) > 2 * 3.28)
            {
                TaskDialog.Show("限制", "支管端头距离其在主管上的投影点过远(>2m)，无法自动连接。");
                return false;
            }
            // (以下是您原有的 ConnectSkewTee 逻辑)
            Pipe connectingPipe = null;
            if (deltaZ < diameter * 4) // 微差/斜管连接
            {
                double retreatDistance = (deltaZ < diameter * 2) ? diameter * 3 : deltaZ;
                if (branchPipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance) return false;
                XYZ newBranchEndPoint = branchPipe.AdjustMEPCurveLength(branchConn.Origin, retreatDistance);
                if (newBranchEndPoint == null) return false;
                connectingPipe = branchPipe.NewPipeBetweenPoints(newBranchEndPoint, breakPoint);
            }
            else // 立管连接 (Tee + Elbow)
            {
                XYZ tempC1 = breakPoint;
                connectingPipe = MEPAnalysisExtension.NewVerticalPipe(branchPipe, branchConn, tempC1, breakPoint, strategy);
            }
            if (connectingPipe == null) return false;
            if (!BreakPipeAndCreateTee(mainPipe.Document, mainPipe, breakPoint, connectingPipe)) return false;
            branchPipe.NewElbowBy2MEPCurve(connectingPipe);
            return true;
        }
        // 在指定点打断主管，并与支管创建一个三通。
        public bool BreakPipeAndCreateTee(Document doc, Pipe mainPipe, XYZ breakPoint, MEPCurve branchElement)
        {
            // 1. 打断主管，返回新生成管道的ID
            ElementId newPipeId = PlumbingUtils.BreakCurve(doc, mainPipe.Id, breakPoint);
            doc.Regenerate();
            Pipe newPipePart = doc.GetElement(newPipeId) as Pipe;
            if (newPipePart == null) return false;
            // 2. 找到打断点附近的四个连接器
            Connector mainConn1 = mainPipe.GetClosestConnector(breakPoint);
            Connector mainConn2 = newPipePart.GetClosestConnector(breakPoint);
            Connector branchConn = branchElement.GetClosestConnector(breakPoint);
            if (mainConn1 == null || mainConn2 == null || branchConn == null) return false;
            // 3. 创建三通
            doc.Create.NewTeeFitting(mainConn1, mainConn2, branchConn);
            return true;
        }
        // 专用于处理两根异面交叉的水平管道，通过一根立管和两个三通进行连接。
        public bool ConnectCrossPipes(Pipe pipe1, Pipe pipe2)
        {
            Document doc = pipe1.Document;
            Line line1 = (pipe1.Location as LocationCurve).Curve as Line;
            Line line2 = (pipe2.Location as LocationCurve).Curve as Line;
            // 1. 计算XY平面上的投影交点
            XYZ intersectionPoint2D = MEPAnalysisExtension.GetIntersectionPoint2D(line1, line2);
            if (intersectionPoint2D == null)
            {
                TaskDialog.Show("错误", "两根管道在XY平面平行，无法生成垂直连接管。");
                return false;
            }
            // 2. 准备创建立管的坐标
            double z1 = line1.Origin.Z;
            double z2 = line2.Origin.Z;
            // 高度检查 (虽然调用前已检查，这里作为安全措施)
            if (Math.Abs(z1 - z2) < 0.2) // 约60mm
            {
                TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。");
                return false;
            }
            // 3. 创建立管
            XYZ bottomPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Min(z1, z2));
            XYZ topPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Max(z1, z2));
            ElementId systemTypeId = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
            ElementId pipeTypeId = pipe1.PipeType.Id;
            ElementId levelId = pipe1.ReferenceLevel.Id;
            Pipe riserPipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, bottomPoint, topPoint);
            riserPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe1.Diameter);
            // 4. 确定上下管道
            Pipe topPipe = z1 > z2 ? pipe1 : pipe2;
            Pipe bottomPipe = z1 > z2 ? pipe2 : pipe1;
            // 5. 连接顶部和底部
            if (!BreakPipeAndCreateTee(doc, topPipe, topPoint, riserPipe))
            {
                TaskDialog.Show("错误", "创建顶部三通连接失败。");
                return false;
            }
            if (!BreakPipeAndCreateTee(doc, bottomPipe, bottomPoint, riserPipe))
            {
                TaskDialog.Show("错误", "创建底部三通连接失败。");
                return false;
            }
            return true;
        }
        public bool ConnectNonParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
        {
            Document doc = p1.Document;
            if (l1.AreLinesCoPlanar(l2))
            {
                // 相交且共面：直接创建弯头
                p1.NewElbowBy2MEPCurve(p2);
                return true;
            }
            // 异面：创建立管连接
            var intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(l1, l2);
            if (intersection2D == null || intersection2D.DistanceTo(c1.Origin) > 4 || intersection2D.DistanceTo(c2.Origin) > 4)
            {
                TaskDialog.Show("限制", "管道在平面上交点过远，请手工调整。");
                return false;
            }
            double z1 = c1.Origin.Z;
            double z2 = c2.Origin.Z;
            // 调整原管道至交点
            p1.AdjustMEPCurveLength(c1.Origin, -c1.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z1)));
            p2.AdjustMEPCurveLength(c2.Origin, -c2.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z2)));
            doc.Regenerate();
            // 创建立管
            Pipe verticalPipe = MEPAnalysisExtension.NewVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
            if (verticalPipe == null) return false;
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
            doc.Regenerate();
            // 连接
            p1.NewElbowBy2MEPCurve(verticalPipe);
            p2.NewElbowBy2MEPCurve(verticalPipe);
            return true;
        }
    }


    //[Transaction(TransactionMode.Manual)]
    //public class Test10_0818 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Autodesk.Revit.DB.Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        //////0507 官方代码测试
    //        //UIAPI详细说明，新增选项等
    //        //https://www.mxbim.com/article/detail/UIAPI.html
    //        //OptionsDialog 4个userControl待测试用途能否扩展

    //        //Truss 创建桁架并修改其桁架成员和剖面。将近1500行待深化  带界面
    //        //Site 比较旧的做法，现在是否还有看的需要？
    //        //包括TopographySurface和SiteSubRegion编辑工具，其中包括添加预定义点区域、更改点高程、移动点和删除点。
    //        //· Application.cs - 实现Revit插件接口IExternalApplication
    //        //· SiteAddRetainingPondCommand.cs - 添加一个新的圆形蓄水池到用户选择的TopographySurface的命令。
    //        //· SiteDeleteRegionAndPointsCommand.cs - 删除一个子区域和其中包含的所有地形表面点的命令。
    //        //· SiteLowerTerrainInRegionCommand.cs - 将一个子区域中包含的所有点下降3英尺的命令。
    //        //· SiteMoveRegionAndPointsCommand.cs - 将子区域及其包含的点移动到宿主表面上新的位置的命令。
    //        //· SiteNormalizeTerrainInRegionCommand.cs - 将区域中的所有点正常化到平均海拔高度的命令。
    //        //· SiteRaiseTerrainInRegionCommand.cs - 提起子区域中包含的所有点3英尺的命令。
    //        //· SiteEditingUtils.cs - 用于现场编辑命令的数据库级别工具。
    //        //· SiteUIUtils.cs - 用于现场编辑命令的用户界面工具。
    //        //StairsAutomation 基于预定义的规则和参数，创建一系列楼梯、楼梯跑和楼梯平台配置。 类目太多将近1500行待深化 无界面是否增加
    //        //· Command.cs - 包含继承自IExternalCommand接口并实现Execute方法的Command类。
    //        //· StairsAutomationUtility.cs - 掌管自动楼梯元素创建的主要类。
    //        //· IStairsConfiguration.cs - 此接口表示要创建的楼梯跑和平台的配置。
    //        //· StairsConfiguration.cs - IStairsConfiguration的特定实现，包含一些默认存储。
    //        //· StairsSingleStraightRun.cs - 表示一个单直线跑的楼梯配置。
    //        //· StairsSingleCurvedRun.cs - 由一条弯曲的楼梯跑组成的楼梯构件。
    //        //· StairsSingleSketchedStraightRun.cs - 由一个单线段直线跑组成的楼梯跑。
    //        //· StairsSingleSketchedCurvedRun.cs - 代表一个弧形跑（在Revit中将被作为绘制的跑形成）的楼梯配置。
    //        //· StairsStandardConfiguration.cs - 表示由直线跑和矩形平台组成的楼梯配置。根据输入参数切换跑。平台宽度可以独立调整。
    //        //· GeometryUtils.cs - 此示例中使用的几何实用程序。
    //        //· RunComponents / IStairsRunComponent.cs - 单个楼梯跑的基本接口。
    //        //· RunComponents / TransformedStairsComponent.cs - 一个可通过平移和旋转移动的楼梯组件的抽象基类。
    //        //· RunComponents / StraightStairsRunComponent.cs - 由线性直线跑组成的楼梯跑。
    //        //· RunComponents / CurvedStairsRunComponent.cs - 由一条弯曲的楼梯跑构成的楼梯组件。
    //        //· RunComponents / SketchedStraightStairsRunComponent.cs - 由单线段直线跑组成的楼梯跑。
    //        //· RunComponents / SketchedCurvedStairsRunComponent.cs - 由一个弧形跑组成的楼梯跑。
    //        //· LandingComponents / IStairsLandingComponent.cs - 表示平台的接口。
    //        //· LandingComponents / StairsRectangleLandingConfiguration.cs - 用于创建具有固定横截面的平台的配置。
    //        //· LandingComponents / LandingComponentUtils.cs - 处理平台时使用的实用程序。

    //        //WinderStairs 转角楼梯设计 类目太多将近1500行待深化 带界面

    //        //////0506 官方代码测试
    //        //Viewers 基于VB开发的四个工具似乎无特殊性，暂跳过

    //        ////TypeSelector 元素类型切换工具，主要功能包括：
    //        ////选择元素：用户选择一个墙体或构件（FamilyInstance）
    //        ////获取可用类型：根据选中元素显示所有可用的类型列表
    //        ////切换类型：将选中元素的类型更改为用户选择的类型
    //        ////支持类型：墙体（Wall）和构件（FamilyInstance）
    //        //try
    //        //{
    //        //    var uiDocument = commandData.Application.ActiveUIDocument;
    //        //    // 创建视图模型
    //        //    var viewModel = new TypeSelectorViewModel(uiDocument);
    //        //    // 创建并显示WPF窗口
    //        //    var window = new TypeSelectorView(viewModel);
    //        //    ////// 设置Revit为所有者窗口
    //        //    ////var revitWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    ////var helper = new System.Windows.Interop.WindowInteropHelper(window)
    //        //    ////{
    //        //    ////    Owner = revitWindow
    //        //    ////};
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //Options 控件UserControl插件选项配置控件，主要功能包括：
    //        //按钮可用性设置：通过ComboBox控制Revit工具栏按钮在不同专业环境下的可用性
    //        //选项持久化：通过ApplicationOptions.Get()获取和应用配置
    //        //恢复默认设置：提供恢复默认配置的功能

    //        //UIAPI DragAndDrop改WPF MVVM转义测试500行不到转了近千行
    //        //家具族管理工具，主要功能包括：
    //        //显示已加载的家具族：在ListView中显示当前文档中已加载的家具族
    //        //显示可加载的族文件：在ListBox中显示可用的家具族文件
    //        //拖放功能：支持在两个列表之间拖放，实现族的加载和卸载
    //        //实时更新：拖放后自动更新Revit文档内容
    //        //try
    //        //{
    //        //    // 创建视图模型
    //        //    var viewModel = new DragAndDropViewModel(commandData.Application.ActiveUIDocument);
    //        //    // 创建并显示WPF窗口
    //        //    var window = new DragAndDropWindow(viewModel);
    //        //    //// 设置Revit为所有者窗口
    //        //    //var revitWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    //var helper = new System.Windows.Interop.WindowInteropHelper(window)
    //        //    //{
    //        //    //    Owner = revitWindow
    //        //    //};
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //Units 单位换算部分跳过

    //        ////VersionChecking
    //        //try
    //        //{
    //        //    // 创建视图模型
    //        //    var viewModel = new VersionCheckViewModel(commandData);
    //        //    // 创建并显示WPF窗口
    //        //    var window = new VersionCheckView(viewModel);
    //        //    // 显示模态对话框
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"发生错误：{ex.Message}";
    //        //    return Result.Failed;
    //        //}

    //        ////ViewPrinter ViewSheetSetForm 初始化出错待查 其他PrintManager工具暂跳过
    //        ////Revit打印管理工具，主要功能包括：
    //        ////视图 / 图纸集管理：创建、编辑、保存、重命名、删除视图 / 图纸集
    //        ////打印范围控制：选择要打印的视图和图纸
    //        ////显示过滤：根据视图类型（仅视图 / 仅图纸 / 两者）过滤显示
    //        ////批量选择：全选 / 全不选功能
    //        //try
    //        //{
    //        //    var uiDocument = commandData?.Application?.ActiveUIDocument;
    //        //    // 创建视图模型
    //        //    var viewModel = new ViewSheetViewModel(uiDocument);
    //        //    // 创建并显示WPF窗口
    //        //    var window = new ViewSheetWindow
    //        //    {
    //        //        DataContext = viewModel
    //        //    };
    //        //    //// 设置Revit作为所有者窗口
    //        //    //var revitWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    //var helper = new System.Windows.Interop.WindowInteropHelper(window)
    //        //    //{
    //        //    //    Owner = revitWindow
    //        //    //};
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //VisibilityControl 控制视图中的类别可见性， 已实现可跳过
    //        //显示所有类别：列出当前视图中所有可控制可见性的类别
    //        //批量控制可见性：通过复选框批量显示 / 隐藏类别
    //        //隔离元素：通过点选或框选元素，自动隔离所选元素所属的类别

    //        ////SlabProperties Revit楼板详细属性查看工具，主要功能： Slab专指基础板
    //        ////基础属性：显示楼板的标高、类型名称、跨度方向
    //        ////结构层分析：显示每个结构层的详细信息
    //        //try
    //        //{
    //        //    var service = new SlabPropertyService(commandData);
    //        //    // 检查选中的楼板
    //        //    var floor = service.GetSelectedFloor(out string error);
    //        //    if (floor == null)
    //        //    {
    //        //        message = error;
    //        //        return Result.Failed;
    //        //    }
    //        //    if (!service.IsValidFloor(floor, out error))
    //        //    {
    //        //        message = error;
    //        //        return Result.Failed;
    //        //    }
    //        //    var viewModel = new SlabPropertiesViewModel(service);
    //        //    var window = new SlabPropertiesWindow(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"发生错误: {ex.Message}";
    //        //    return Result.Failed;
    //        //}

    //        ////StructuralLayerFunction Revit楼板结构层功能查看工具，主要功能：
    //        ////选择楼板：用户选择一个楼板（Floor）
    //        ////获取复合结构：读取楼板类型的复合结构层
    //        ////提取层功能：获取每个结构层的功能（Function）
    //        ////显示结果：在对话框中按从外到内的顺序显示各层功能
    //        //try
    //        //{
    //        //    // 创建服务层
    //        //    var service = new FloorLayerService(commandData);
    //        //    // 检查是否有选中的楼板
    //        //    var floor = service.GetSelectedFloor();
    //        //    if (floor == null)
    //        //    {
    //        //        message = "请选中一个楼板";
    //        //        return Result.Failed;
    //        //    }
    //        //    // 验证楼板有效性
    //        //    if (!service.IsValidFloor(floor, out string error))
    //        //    {
    //        //        message = error;
    //        //        return Result.Failed;
    //        //    }
    //        //    // 创建视图模型并显示窗口
    //        //    var viewModel = new FloorLayerFunctionViewModel(service);
    //        //    var window = new FloorLayerFunctionView(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"发生错误: {ex.Message}";
    //        //    return Result.Failed;
    //        //}

    //        //MEPSystemTraversal 待后续增补界面和细化
    //        ////TraverseSystem  Revit MEP系统遍历分析工具，支持水暖系统分析，功能：
    //        ////系统识别：从选中的元素（机械系统、管道系统、设备、管件）中提取MEP系统
    //        ////系统遍历：沿流向遍历整个系统，构建树形结构
    //        ////数据导出：将遍历结果导出为XML文件
    //        //new MEPSystemTraversal(commandData);

    //        ////TransactionControl  Revit事务管理演示工具，主要功能：
    //        ////事务组管理：演示Transaction Group的使用
    //        ////事务管理：演示Start / Commit / Rollback操作
    //        ////墙操作：创建、移动、删除墙
    //        ////事务树显示：显示所有事务的状态 (空)
    //        //try
    //        //{
    //        //    if (commandData?.Application?.ActiveUIDocument?.Document == null)
    //        //    {
    //        //        message = "无法获取有效的Revit文档";
    //        //        return Result.Failed;
    //        //    }
    //        //    var service = new TransactionService(commandData);
    //        //    var viewModel = new TransactionViewModel(service);
    //        //    var window = new TransactionWindow(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////TestWallThickness Revit 墙厚度批量修改工具，主要功能
    //        ////选择墙元素 获取用户当前选择的元素
    //        ////过滤墙类型 只处理 Wall 类型的元素
    //        ////修改墙厚度 将墙的每一层厚度乘以 10 倍
    //        ////事务管理    使用 Transaction 确保修改可撤销
    //        //// 获取Revit应用程序和文档对象
    //        //UIApplication application = commandData.Application;
    //        //UIDocument document = application.ActiveUIDocument;
    //        //// 标记是否选中了墙元素
    //        //bool selectWalls = false;
    //        //// 启动事务 - 所有修改将在一个事务中完成，支持撤销
    //        //Transaction tran = new Transaction(document.Document, "测试墙厚度修改");
    //        //tran.Start();
    //        //// 遍历用户当前选中的所有元素
    //        //foreach (ElementId elementId in document.Selection.GetElementIds())
    //        //{
    //        //    // 根据ID获取元素实例
    //        //    Element element = document.Document.GetElement(elementId);
    //        //    // 检查元素是否为墙类型
    //        //    if (element is Wall)
    //        //    {
    //        //        Wall wall = (Wall)element;
    //        //        selectWalls = true;  // 标记已选中墙
    //        //        try
    //        //        {
    //        //            // 获取墙类型的复合结构（包含各层材料、厚度、功能等信息）
    //        //            CompoundStructure cs = wall.WallType.GetCompoundStructure();
    //        //            // 遍历复合结构的每一层
    //        //            for (int ii = 0; ii < cs.LayerCount; ii++)
    //        //            {
    //        //                // 获取当前层厚度并乘以10
    //        //                double currentWidth = cs.GetLayerWidth(ii);
    //        //                double newWidth = currentWidth * 10;

    //        //                // 设置新的层厚度
    //        //                cs.SetLayerWidth(ii, newWidth);
    //        //            }
    //        //            // 将修改后的复合结构应用回墙类型
    //        //            // 注意：这会修改墙类型，影响所有使用该类型的墙实例
    //        //            wall.WallType.SetCompoundStructure(cs);
    //        //            // 强制重新生成文档以更新显示
    //        //            document.Document.Regenerate();
    //        //        }
    //        //        catch (Exception ex)
    //        //        {
    //        //            // 遇到错误跳过当前墙，继续处理下一个
    //        //            // 不抛出异常，避免中断整个批处理流程
    //        //            continue;
    //        //        }
    //        //    }
    //        //}
    //        //// 根据是否选中墙决定事务提交或回滚
    //        //if (selectWalls)
    //        //{
    //        //    // 提交事务 - 保存所有修改
    //        //    tran.Commit();
    //        //    return Result.Succeeded;
    //        //}
    //        //else
    //        //{
    //        //    // 未选中任何墙，设置提示消息
    //        //    message = "请至少选择一个墙。";
    //        //    // 回滚事务 - 撤销所有修改
    //        //    tran.RollBack();
    //        //    return Result.Cancelled;
    //        //}

    //        ////TestFloorThickness Revit楼板厚度批量修改工具，主要功能：
    //        ////选择楼板：用户选择一个或多个楼板
    //        ////获取复合结构：读取楼板类型的复合结构（CompoundStructure）
    //        ////修改层厚度：将每个结构层的厚度乘以10
    //        ////应用修改：将修改后的复合结构应用回楼板类型
    //        //try
    //        //{
    //        //    // 验证命令数据
    //        //    if (commandData?.Application?.ActiveUIDocument?.Document == null)
    //        //    {
    //        //        message = "无法获取有效的Revit文档";
    //        //        return Result.Failed;
    //        //    }
    //        //    // 检查是否有选中的楼板
    //        //    var service = new FloorThicknessService(commandData);
    //        //    var floors = service.GetSelectedFloors();
    //        //    if (floors.Count == 0)
    //        //    {
    //        //        TaskDialog.Show("提示", "请至少选中一个楼板");
    //        //        return Result.Cancelled;
    //        //    }
    //        //    // 创建视图模型并显示窗口
    //        //    var viewModel = new FloorThicknessViewModel(service);
    //        //    var window = new FloorThicknessView(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"发生错误: {ex.Message}";
    //        //    return Result.Failed;
    //        //}

    //        //TagBeam Revit结构构件标注工具集，包含三个主要功能： 标注梁可用，钢筋有增补代码未测试
    //        //1.TagBeam - 梁端部标注
    //        //选中梁后，在梁的两端自动添加标记
    //        //支持多种标记类型：结构框架标记、材质标记、多类别标记
    //        //可设置标记方向、是否带引线
    //        //2.TagRebar - 钢筋标记
    //        //为选中的钢筋添加标记
    //        //在钢筋的第一个端点放置标记
    //        //3.CreateText - 创建文字注释
    //        //为选中的钢筋创建文字注释
    //        //显示钢筋的类别和名称
    //        ///// <summary>
    //        ///// 钢筋标记命令
    //        ///// </summary>
    //        //try
    //        //{
    //        //    var service = new TaggingService(commandData);
    //        //    // 检查是否有选中的钢筋
    //        //    var rebars = service.GetSelectedRebars();
    //        //    if (rebars.Count == 0)
    //        //    {
    //        //        message = "请至少选中一个钢筋";
    //        //        return Result.Failed;
    //        //    }
    //        //    var viewModel = new TagRebarViewModel(service);
    //        //    var window = new TagRebarView(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}
    //        ///// <summary>
    //        ///// 梁端标记命令
    //        ///// </summary>
    //        //try
    //        //{
    //        //    var service = new TaggingService(commandData);
    //        //    // 检查是否有选中的梁
    //        //    var beams = service.GetSelectedBeams();
    //        //    if (beams.Count == 0)
    //        //    {
    //        //        message = "请至少选中一个梁";
    //        //        return Result.Failed;
    //        //    }
    //        //    var viewModel = new TagBeamViewModel(service);
    //        //    var window = new TagBeamView(viewModel);
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}
    //        ///// <summary>
    //        ///// 钢筋文字注释命令（简化版，直接执行）
    //        ///// </summary>
    //        //try
    //        //{
    //        //    var service = new TaggingService(commandData);
    //        //    var rebars = service.GetSelectedRebars();
    //        //    if (rebars.Count == 0)
    //        //    {
    //        //        message = "请至少选中一个钢筋";
    //        //        return Result.Failed;
    //        //    }
    //        //    int successCount = 0;
    //        //    foreach (var rebarInfo in rebars)
    //        //    {
    //        //        if (service.CreateRebarTextNote(rebarInfo.Rebar))
    //        //            successCount++;
    //        //    }
    //        //    TaskDialog.Show("完成", $"成功创建了 {successCount} 个文字注释");
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////StructSample Revit沿墙布置结构柱工具。逻辑似无问题，柱墙均应编码，后续待研究组合 主要功能：
    //        ////选择墙：用户选择需要布置柱的墙
    //        ////过滤墙：过滤出有顶部和底部约束的有效墙
    //        ////加载柱族：查找指定的柱族类型（木柱 191x292mm）
    //        ////计算位置：根据墙的长度和间距（5英尺）计算柱的位置
    //        ////放置柱：沿墙方向等间距放置柱，包括起点和终点
    //        //new CreateColumnByWall(commandData);

    //        ////Revit点标注(SpotDimension)信息查看工具，主要功能：
    //        ////视图筛选：按视图筛选显示点标注
    //        ////点标注列表：显示选中视图中所有点标注
    //        ////参数信息展示：显示选中点标注的详细参数（包括类型参数和实例参数）
    //        ////高亮显示：选中点标注后在Revit中高亮显示
    //        //try
    //        //{
    //        //    // 创建服务层
    //        //    var service = new SpotDimensionService(commandData);
    //        //    // 创建视图模型
    //        //    var viewModel = new SpotDimensionAnalyzerViewModel(commandData, service);
    //        //    // 创建并显示窗口
    //        //    var window = new SpotDimensionAnalyzerView(viewModel);
    //        //    // 显示对话框
    //        //    bool? result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"发生错误: {ex.Message}\n{ex.StackTrace}";
    //        //    return Result.Failed;
    //        //}

    //        ////SpanDirection Revit楼板跨度方向分析工具，基本可用。。主要功能：
    //        ////用户选择一个楼板(Slab / Floor)
    //        ////获取楼板的跨度方向角度(SpanDirectionAngle)
    //        ////获取楼板的所有跨度方向符号(SpanDirectionSymbols)
    //        ////在对话框中显示这些信息
    //        //// 初始化文档引用
    //        //try
    //        //{
    //        //    // 获取用户选中的元素ID集合
    //        //    Floor selectedFloor = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "sth")) as Floor;
    //        //    // 验证是否选中了元素
    //        //    if (selectedFloor == null)
    //        //    {
    //        //        TaskDialog.Show("tt", "请先选择一个楼板");
    //        //        return Result.Cancelled;
    //        //    }
    //        //    // 处理选中的楼板元素
    //        //    new FloorSpanDirectionProcess(selectedFloor);
    //        //}
    //        //catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
    //        //{
    //        //    // 使用异常过滤器，仅捕获特定的预期异常
    //        //    message = $"操作错误: {ex.Message}";
    //        //    return Result.Failed;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"未预期的错误: {ex}\n{ex.StackTrace}";
    //        //    return Result.Failed;
    //        //}

    //        //SolidSolidCut 待后续研究
    //        //Cut命令：使用一个实体（球体）切割另一个实体（立方体），演示SolidSolidCutUtils.AddCutBetweenSolids API
    //        //Uncut命令：移除两个实体之间的切割关系，恢复原始形状，演示SolidSolidCutUtils.RemoveCutBetweenSolids API

    //        ////SlabShapeEditing 转义基本完整但有重复MathTool和View比较臃肿问题
    //        ////选中楼板后在WinForm窗口中显示2D轮廓
    //        ////支持添加顶点（Vertex）和折线（Crease）
    //        ////支持移动和旋转视角
    //        ////支持重置形状和更新操作
    //        //Floor selectedFloor = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "sth")) as Floor;
    //        //try
    //        //{
    //        //    // 创建服务层
    //        //    var profileService = new SlabProfileService(commandData, selectedFloor);
    //        //    // 创建ViewModel
    //        //    var viewModel = new SlabShapeEditorViewModel(profileService);
    //        //    // 创建并显示WPF窗口
    //        //    var window = new SlabShapeEditorView(viewModel);
    //        //    //window.Owner = System.Windows.Application.Current.MainWindow;
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = $"错误: {ex.Message}";
    //        //    return Result.Failed;
    //        //}

    //        //0505 官方代码测试
    //        ////SinePlotter 沿正弦曲线批量放置族实例，生成由族实例构成的波形图案。
    //        ////查找指定名称的族符号（FamilySymbol）
    //        ////根据分区数计算角度增量 θ = 2π / partitions
    //        ////循环计算每个点的位置(x, y)
    //        ////在计算出的位置上放置族实例
    //        //    try
    //        //    {
    //        //        //Application 为自定义，部分逻辑在那里
    //        //        var fsName = Application2.GetFamilySymbolName();
    //        //        // 查找指定名称的族符号
    //        //        var familySymbol = FindFamilySymbol(doc, fsName);
    //        //        if (familySymbol == null)
    //        //        {
    //        //            TaskDialog.Show("族符号加载错误", "项目中未加载指定的族符号。");
    //        //            return Result.Failed;
    //        //        }
    //        //        // 激活族符号
    //        //        if (!familySymbol.IsActive)
    //        //        {
    //        //            familySymbol.Activate();
    //        //        }
    //        //        // 获取绘制参数
    //        //        var plotter = new FamilyInstancePlotter(familySymbol, doc);
    //        //        var parameters = Application2.GetPlottingParameters();
    //        //        // 沿曲线放置实例
    //        //        plotter.PlaceInstancesOnCurve(
    //        //            parameters.partitions,
    //        //            parameters.period,
    //        //            parameters.amplitude,
    //        //            parameters.numOfCircles);
    //        //        return Result.Succeeded;
    //        //    }
    //        //    catch (Exception ex)
    //        //    {
    //        //        message = ex.Message;
    //        //        return Result.Failed;
    //        //    }
    //        ///// <summary>
    //        ///// 查找族符号 - 使用LINQ和模式匹配
    //        ///// </summary>
    //        //private static FamilySymbol FindFamilySymbol(Document doc, string symbolName)
    //        //{
    //        //    var collector = new FilteredElementCollector(doc);
    //        //    return collector.OfClass(typeof(FamilySymbol))
    //        //        .Cast<FamilySymbol>()
    //        //        .FirstOrDefault(s => s.Name == symbolName);
    //        //}

    //        ////SharedCoordinateSystem Revit 共享坐标系统示例程序的功能 跟预想差异较大 没啥深究必要 界面可看一下
    //        ////CoordinateSystemData 数据层 -管理项目位置、偏移量、经纬度等核心数据
    //        ////CoordinateSystemDataForm    表现层(WinForms) - 位置列表、偏移量编辑、城市选择、时区设置
    //        ////DuplicateForm   复制位置对话框（未提供代码）
    //        //try
    //        //{
    //        //    CoordinateSystemView window = new CoordinateSystemView(commandData);
    //        //    // 使用ShowDialog模态显示，确保Revit事务正确管理
    //        //    bool? result = window.ShowDialog();
    //        //    if (result == true)
    //        //    {
    //        //        return Result.Succeeded;
    //        //    }
    //        //    else
    //        //    {
    //        //        return Result.Cancelled;
    //        //    }
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //////ShaftHolePuncher 在墙、楼板、梁或自由空间中创建竖井洞口（Shaft Opening），用户可以在2D预览区域绘制任意形状的曲线。
    //        //////几何部分命令与已有重复ITool，RectangleTool、LineTool、MathTools等，代码量太长只有核心逻辑缺失太多
    //        //////Revit 3D世界坐标
    //        //////↓ To2DMatrix（投影到平面）
    //        //////↓ MoveToCenterMatrix（居中）
    //        //////↓ ScaleMatrix（缩放适配PictureBox）
    //        //////↓ TransformPoints（屏幕坐标偏移）
    //        //////→ 2D屏幕坐标
    //        ////var selectedIds = uiDoc.Selection.GetElementIds();
    //        ////Profile3 profile;
    //        ////if (selectedIds.Count == 0)
    //        ////{
    //        ////    profile = new ProfileNull(commandData);
    //        ////}
    //        ////else if (selectedIds.Count == 1)
    //        ////{
    //        ////    var element = uiDoc.Document.GetElement(selectedIds.First());
    //        ////    if (element is Wall)
    //        ////    {
    //        ////        var wall = (Wall)element;
    //        ////        profile = new ProfileWall(wall, commandData);
    //        ////    }
    //        ////    else if (element is Floor)
    //        ////    {
    //        ////        var floor = (Floor)element;
    //        ////        profile = new ProfileFloor(floor, commandData);
    //        ////    }
    //        ////    else if (element is FamilyInstance)
    //        ////    {
    //        ////        var fi = (FamilyInstance)element;
    //        ////        if (fi.StructuralType == Structure.StructuralType.Beam)
    //        ////        {
    //        ////            profile = new ProfileBeam(fi, commandData);
    //        ////        }
    //        ////        else
    //        ////        {
    //        ////            throw new Exception("请选择一个墙、楼板或梁来创建洞口，或取消选择以创建自由竖井洞口。");
    //        ////        }
    //        ////    }
    //        ////    else
    //        ////    {
    //        ////        throw new Exception("请选择一个墙、楼板或梁来创建洞口，或取消选择以创建自由竖井洞口。");
    //        ////    }
    //        ////}
    //        ////else
    //        ////{
    //        ////    message = "请仅选择一个元素，或取消选择以创建自由竖井洞口。";
    //        ////    return Result.Failed;
    //        ////}
    //        ////var viewModel = new ShaftHolePuncherViewModel(profile, commandData);
    //        //var viewModel = new ShaftHolePuncherViewModel(new ProfileNull(commandData), commandData);
    //        //var window = new ShaftHolePuncherView { DataContext = viewModel };
    //        //viewModel.CloseWindow = window.Close;
    //        //window.ShowDialog();


    //        ////Selections 似乎也有VB版 多种图形交互选择功能，包括元素选择、面选择、点选择、移动元素、放置窗体和创建圆形等。
    //        ////整体逻辑有问题，几个执行命令是独立的没有放到VM中。
    //        ////PickforDeletion 多选元素并删除
    //        ////PlaceAtPointOnWallFace 在墙面点上放置固定尺寸窗户
    //        ////PlaceAtPickedFaceWorkplane 选择平面→设工作平面→拾取圆心→画圆
    //        ////SelectionDialog 拾取元素→拾取点→移动元素到点
    //        //try
    //        //{
    //        //    var viewModel = new SelectionDialogViewModel(commandData);
    //        //    var window = new SelectionDialogView { DataContext = viewModel };
    //        //    viewModel.CloseWindow = window.Close;
    //        //    window.ShowDialog();

    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////ScheduleToHTML 将当前活动的明细表（Schedule）导出为HTML文件，支持单元格合并、背景色、字体样式等格式保留。
    //        ////TableSectionData 获取明细表的分区数据（表头 / 主体）
    //        ////HtmlTextWriter.NET HTML写入器，简化标签生成
    //        ////合并单元格处理 通过TableMergedCell获取合并范围，设置colspan / rowspan
    //        ////写html部分需要引用using System.Web.UI; 可能有nuget引入库问题
    //        //if (!(activeView is ViewSchedule schedule))
    //        //{
    //        //    TaskDialog.Show("无法继续", "活动视图必须是明细表。");
    //        //    return Result.Cancelled;
    //        //}
    //        //var exporter = new ScheduleHtmlExporter(schedule);
    //        //exporter.ExportToHtml();

    //        ////ScheduleCreation 自动创建墙体明细表并添加到图纸，同时对明细表进行格式化和过滤设置。
    //        ////ScheduleCreationUtility	明细表创建工具类，封装创建、格式化、添加到图纸的逻辑
    //        ////new ScheduleCreatorCommand(commandData);
    //        //try
    //        //{
    //        //    var utility = new ScheduleCreatorCommand(commandData);
    //        //    utility.CreateAndAddSchedules(uiDoc);
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////ScheduleAutomaticFormatter 自动格式化明细表（Schedule）的列背景色，并在明细表变化时自动更新格式。
    //        ////ScheduleFormatterCommand 外部命令，对选中的明细表应用格式并注册更新器
    //        ////ScheduleFormatter   格式化器，实现IUpdater接口，监听明细表变化
    //        ////用户选择明细表 → 执行命令..应用格式：为明细表列设置交替背景色..添加标记：使用ExtensibleStorage在明细表上添加"Formatted"标记..注册更新器：监听所有带标记的明细表，变化时自动重新格式化
    //        //new ScheduleFormatterCommand(commandData);

    //        //RvtSamples 示例项数据模型类，用于存储Revit插件菜单中每个示例项的配置信息。作为数据容器，存储一个菜单项的完整配置信息，供Ribbon菜单生成时使用。

    //        ////RoutingPreferenceAnalysis 分析管道的路由偏好设置，检查管段（Segments）和管件（Fittings）的配置问题，并以XML格式输出结果。 Builder近千行，后面几个命令相对独立没有从窗体调用
    //        ////WPF窗口，提供管道类型选择、尺寸选择、分析功能
    //        ////Analyzer    分析器（未完整显示），检查路由偏好规则
    //        ////PartIdInfo  存储管段 / 管件的ID和分组类型信息
    //        ////Validation 验证辅助类（未完整显示），检查MEP模块和管道定义
    //        ////FindFolderUtility	查找Revit安装目录中的族文件，支持用户自定义路径
    //        ////RoutingPreferenceDataException 自定义异常类
    //        ////SchemaValidationHelper 验证XML文档是否符合预定义的XSD架构
    //        ////CommandReadPreferences 从XML文件读取路由偏好配置数据，并在文档中创建管道类型、管段、尺寸和路由偏好规则。
    //        ////CommandWritePreferences  将当前文档中的路由偏好配置导出为XML文件，供CommandReadPreferences命令后续导入使用。
    //        ////RoutingPreferenceBuilder 从XML读取路由偏好配置并在Revit中创建完整的管道系统，以及将现有管道配置导出为XML。主要功能： 从 XML 导入管道系统配置 - 读取 XML 文件，创建 / 加载管道族、管段、管径、管件、路由偏好规则..导出管道系统配置到 XML - 将当前文档中的管道配置序列化为 XML..管理管道系统的各种类型 - 管段(Segment)、管件(Fitting)、材质(Material)、管程类型(Schedule)、管类型(PipeType)..路由偏好管理 - 控制管道自动布线时优先使用的管件类型（弯头、三通、变径等）
    //        //// 验证管道定义
    //        //if (!new FilteredElementCollector(doc).OfClass(typeof(PipeType)).Any())
    //        //{
    //        //    TaskDialog.Show("路由偏好分析", "文档中没有定义管道类型。");
    //        //    return Result.Cancelled;
    //        //}
    //        //var viewModel = new RoutingPreferenceViewModel(commandData.Application);
    //        //var window = new RoutingPreferenceView { DataContext = viewModel };
    //        //viewModel.CloseWindow = window.Close;
    //        //window.ShowDialog();

    //        ////RotateFramingObjects 旋转选中的结构构件（梁、支撑、柱），支持绝对旋转和相对旋转两种模式。
    //        //// 验证选中元素
    //        //var selectedIds = uiApp.ActiveUIDocument.Selection.GetElementIds();
    //        //if (selectedIds.Count == 0)
    //        //{
    //        //    message = "请选中需要旋转的梁、支撑或柱。";
    //        //    return Result.Failed;
    //        //}
    //        ////// 验证选中元素是否为有效的结构构件
    //        ////var invalidElements = selectedIds
    //        ////    .Select(id => doc.GetElement(id))
    //        ////    .Where(e => !IsValidStructuralElement(e))
    //        ////    .ToList();
    //        ////if (invalidElements.Any())
    //        ////{
    //        ////    message = "选中的元素中包含非梁/支撑/柱的构件。";
    //        ////    foreach (var elem in invalidElements)
    //        ////        elements.Insert(elem);
    //        ////    return Result.Failed;
    //        ////}
    //        ////    /// <summary>
    //        ////    /// 判断是否为有效的结构构件
    //        ////    /// </summary>
    //        ////private static bool IsValidStructuralElement(Element element)
    //        ////{
    //        ////    return element is FamilyInstance instance &&
    //        ////           (instance.StructuralType == StructuralType.Beam ||
    //        ////            instance.StructuralType == StructuralType.Brace ||
    //        ////            instance.StructuralType == StructuralType.Column);
    //        ////}
    //        //// 显示旋转对话框
    //        //var viewModel = new RotateFramingViewModel(uiApp);
    //        //var window = new RotateFramingView { DataContext = viewModel };
    //        //viewModel.CloseWindow = window.Close;
    //        ////// 如果是单个梁/支撑，尝试获取当前旋转值并填入
    //        ////if (selectedIds.Count == 1)
    //        ////{
    //        ////    var element = doc.GetElement(selectedIds.First());
    //        ////    if (element is FamilyInstance instance)
    //        ////    {
    //        ////        var currentAngle = GetCurrentRotationAngle(instance);
    //        ////        if (currentAngle.HasValue)
    //        ////            viewModel.RotationAngle = currentAngle.Value;
    //        ////    }
    //        ////}
    //        ////    /// <summary>
    //        ////    /// 获取构件当前的旋转角度（度数）
    //        ////    /// </summary>
    //        ////private static double? GetCurrentRotationAngle(FamilyInstance instance)
    //        ////{
    //        ////    const string paramName = "Cross-Section Rotation";
    //        ////    if (instance.StructuralType == StructuralType.Beam ||
    //        ////        instance.StructuralType == StructuralType.Brace)
    //        ////    {
    //        ////        var param = instance.LookupParameter(paramName);
    //        ////        if (param != null && param.StorageType == StorageType.Double)
    //        ////        {
    //        ////            return Math.Round(param.AsDouble() * 180 / Math.PI, 3);
    //        ////        }
    //        ////    }
    //        ////    else if (instance.StructuralType == StructuralType.Column)
    //        ////    {
    //        ////        var location = instance.Location as LocationPoint;
    //        ////        if (location != null)
    //        ////        {
    //        ////            return Math.Round(location.Rotation * 180 / Math.PI, 3);
    //        ////        }
    //        ////    }
    //        ////    return null;
    //        ////}
    //        //window.ShowDialog();

    //        ////RoomSchedule 从Excel文件导入房间数据并在Revit中创建房间，支持房间数据的双向同步。
    //        ////RoomScheduleForm WinForm窗体，显示Excel数据源和Revit房间列表
    //        ////RoomsData 改分布类  Revit房间数据管理类，获取房间参数、判断共享参数
    //        ////XlsDBConnector  Excel数据源连接器，读取 / 更新.xls文件
    //        //try
    //        //{
    //        //    using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "房间调度"))
    //        //    {
    //        //        transaction.Start();

    //        //        var viewModel = new RoomScheduleViewModel(commandData);
    //        //        var window = new RoomScheduleView { DataContext = viewModel };
    //        //        viewModel.CloseWindow = window.Close;
    //        //        window.ShowDialog();

    //        //        transaction.Commit();
    //        //    }
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //////Rooms 房间管理实现，包括房间标记、编号排序、部门面积统计和导出功能。基本可用
    //        //////RoomsData 核心数据类，管理房间数据、标记、部门统计
    //        //////roomsInformationForm    WinForm窗体，显示房间列表和部门信息
    //        //try
    //        //{
    //        //    using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "房间管理"))
    //        //    {
    //        //        transaction.Start();
    //        //        var viewModel = new RoomManagerOffViewModel(commandData);
    //        //        var window = new RoomManagerOffView { DataContext = viewModel };
    //        //        viewModel.CloseWindow = window.Close;
    //        //        window.ShowDialog();
    //        //        transaction.Commit();
    //        //    }
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////RoofsRooms RoomRoofBoundaryChecker检查房间与屋顶的几何关系，判断哪些房间有边界屋顶，哪些房间缺少边界屋顶。 逻辑似乎有问题 待修复
    //        ////获取所有房间 / 空间   从文档中获取所有Room和Space元素
    //        ////计算房间几何  使用SpatialElementGeometryCalculator计算房间的几何体
    //        ////分析边界子面  检查房间每个面的边界子面，判断是否与屋顶相交
    //        ////分类记录    将有屋顶和无屋顶的房间分别记录
    //        ////输出结果    将结果写入日志文件并显示在TaskDialog中
    //        //new RoomRoofBoundaryChecker(commandData, elements);

    //        //Ribbon创建自定义Ribbon选项卡和面板，提供创建墙体的各种UI控件。 可跳过

    //        //LibraryPath 管理Revit的库路径（Library Paths）设置。应该是API已过期但不报错
    //        //LoadFamily 加载族文件（Family）和族类型（Family Symbol）。获取Revit的库路径配置，递归搜索族文件
    //        //RvtUtils 静态方法用于提取和显示元素的信息，包括参数、几何体和位置。报告属性参数和几何体信息，MsgBox对话框
    //        //Selection 获取当前选中的元素并显示其基本信息。基本信息较简单 跳过
    //        ////ShowElementData VB转C# 显示选中元素的详细信息，包括类型、位置、几何体和参数。
    //        //// 获取选中的元素
    //        //var selectedElements = GetSelectedElements(doc, uiDoc.Selection.GetElementIds());
    //        //// 显示选中数量
    //        //ShowMessage($"选中元素数量: {selectedElements.Count}");
    //        //// 遍历每个选中元素
    //        //foreach (var element in selectedElements)
    //        //{
    //        //    ShowElementType(element);
    //        //    ShowElementLocation(element);
    //        //    ShowElementGeometry(uiApp.Application, element);
    //        //    ShowElementParameters(element);
    //        //    ShowSeparator();
    //        //}

    //        //Reinforcement 快速为混凝土结构梁/柱添加配筋 跳过 有的几何方法需要和已转移的Reinforcement相关类对比

    //        ////ReferencePlane 参照平面管理 难得基本可用
    //        ////在选中的墙或楼板上创建参考平面（Reference Plane），并提供现有参考平面的列表查看功能。
    //        ////参考平面列表显示：在DataGridView中显示文档中所有参考平面的ID、端点、法线信息
    //        //var viewModel = new ReferencePlaneMgrViewModel(uiDoc);
    //        //var window = new ReferencePlaneMgrView { DataContext = viewModel };
    //        //viewModel.CloseWindow = window.Close;
    //        //window.ShowDialog();

    //        //RebarContainerAnyShapeType 为选中的混凝土梁或柱自动创建钢筋（Rebar）。跳过
    //        //FrameReinMakerFactory 工厂类，根据选中元素类型创建对应的钢筋生成器
    //        //FrameReinMaker  钢筋生成器基类 / 接口，具体实现梁 / 柱的钢筋创建逻辑

    //        ////ReadonlySharedParameters 只读共享参数 管理只读共享参数，支持批量设置参数值并将参数绑定到文档。
    //        ////有多个入口方法类，尝试增加WPF窗口统一进入
    //        ////SetReadonlyCost1 / 2  设置"ReadonlyCost"参数（基于ID / 增量计算）
    //        ////SetReadonlyId1 / 2    设置"ReadonlyId"参数（基于UniqueId / 类型名 + ID）
    //        ////BindNewReadonlySharedParametersToDocument 创建并绑定两个只读共享参数到文档
    //        ////ReadonlyCostSetter 成本参数设置逻辑
    //        ////ReadonlyIdSetter ID参数设置逻辑
    //        ////SharedParameterBindingManager 共享参数绑定管理器（定义、类别、绑定）
    //        //var viewModel = new ReadonlySharedParametersViewModel(doc);
    //        //var window = new ReadonlySharedParametersView { DataContext = viewModel };
    //        //viewModel.CloseWindow = window.Close;
    //        //window.ShowDialog();

    //        ////ProjectInfo 查看和编辑项目信息（Project Information），并提供了丰富的gbXML导出相关配置数据。
    //        ////ProjectInfoWrapper	项目信息的PropertyGrid包装类 还有一堆专用的converter
    //        ////原先的一些项目属性被移除了，wpf莫名增加了一种propGrid控件，没啥必要深究这部分
    //        //// 初始化全局信息
    //        ////RevitStartInfo.RevitApp = commandData.Application.Application;
    //        //RevitStartInfo.RevitDoc = doc;
    //        //RevitStartInfo.RevitProduct = commandData.Application.Application.Product;
    //        //var viewModel = new ProjectInfoViewModel(doc);
    //        //var window = new ProjectInfoView{ DataContext = viewModel };
    //        //bool? shouldSave = null;
    //        ////viewModel.CloseRequested = save =>
    //        ////{
    //        ////    shouldSave = save;
    //        ////    window.Close();
    //        ////};
    //        //window.ShowDialog();
    //        //if (shouldSave == true)
    //        //{
    //        //    using (var transaction = new Transaction(doc, "更新项目信息"))
    //        //    {
    //        //        transaction.Start();
    //        //        transaction.Commit();
    //        //    }
    //        //    return Result.Succeeded;
    //        //}

    //        //PowerCircuit 电气电路插件的核心数据类，负责：
    //        //收集连接器信息：验证选中设备是否有可用的电气连接器
    //        //收集电路信息：找出所有选中设备共用的电路
    //        //执行操作：创建电路、编辑电路、选择 / 断开配电盘
    //        //结构有点复杂，但ElectricalSystemSet在2021版已被废掉，还有无必要深入
    //        //try
    //        //{
    //        //    // 验证是否有选中的元素
    //        //    var selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
    //        //    if (selectedIds.Count == 0)
    //        //    {
    //        //        message = "请选中需要操作的电气设备（如灯具）。";
    //        //        return Result.Failed;
    //        //    }
    //        //    // 创建操作数据对象
    //        //    var operationData = new CircuitOperationData(commandData);
    //        //    // 显示主操作窗口
    //        //    var mainVm = new CircuitOperationViewModel(operationData);
    //        //    OfficalSamples.OperationType? selectedOperation = null;
    //        //    mainVm.OperationSelected += op => selectedOperation = op;
    //        //    var mainWindow = new CircuitOperationView { DataContext = mainVm };
    //        //    mainVm.CloseWindow = () => mainWindow.Close();
    //        //    mainWindow.ShowDialog();
    //        //    if (!selectedOperation.HasValue) return Result.Cancelled;
    //        //    // 处理编辑电路子操作
    //        //    if (selectedOperation == OfficalSamples.OperationType.EditCircuit)
    //        //    {
    //        //        // 如果有多个电路，已在主VM中处理选择
    //        //        var editVm = new EditCircuitViewModel();
    //        //        EditOptionType? editOption = null;
    //        //        editVm.EditOptionConfirmed += opt => editOption = opt;
    //        //        var editWindow = new CircuitEditWindow { DataContext = editVm };
    //        //        editVm.CloseWindow = () => editWindow.Close();
    //        //        editWindow.ShowDialog();
    //        //        if (editOption.HasValue)
    //        //        {
    //        //            operationData.EditOption = editOption.Value;
    //        //        }
    //        //        else
    //        //        {
    //        //            return Result.Cancelled;
    //        //        }
    //        //    }
    //        //    // 执行操作
    //        //    operationData.Operation = selectedOperation.Value;
    //        //    operationData.Operate();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.ToString();
    //        //    return Result.Failed;
    //        //}

    //        //////0505 官方代码测试
    //        //PostCommandWorkflow监控文档保存操作，在保存前检查是否已添加修订（Revision），并引导用户完成修订流程。
    //        //目前执行缺乏反馈，待重修
    //        //PostCommandRevisionMonitor 核心监控类，订阅DocumentSaving事件，处理修订检查逻辑
    //        //PostCommandRevisionMonitorEvent 外部事件处理器，在修订命令完成后执行清理和重新保存
    //        //new PostCommandRevisionMonitor(doc);

    //        //PointCloudEngine 点云相关跳过

    //        ////PlacementOptions 帮助用户放置族实例，支持两种放置方式：  
    //        ///////原逻辑 两重界面就没有必要 待重优化
    //        ////FaceBased(基于面) 将族实例放置在现有面上 插座、开关、设备
    //        ////SketchBased(基于草图)  通过绘制草图线放置族实例 梁、支撑、幕墙网格
    //        //try
    //        //{
    //        //    if (commandData.Application.ActiveUIDocument?.Document == null)
    //        //    {
    //        //        message = "请打开一个活动文档。";
    //        //        return Result.Failed;
    //        //    }
    //        //    var window = new FamilyPlacementView(commandData);
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////PhysicalProp
    //        ////读取并显示结构构件（梁、柱、支撑）的材料物理属性。使用TaskDialog显示所有属性值.
    //        //new DumpMaterialPhysicalParameters(commandData);

    //        ////PlaceFamilyInstanceByFace 在选中的面上创建基于点(Point-Based)或基于线(Line-Based)的族实例
    //        ////原逻辑 两重界面就没有必要 待重优化
    //        ////BasedTypeForm 选择族类型（Point - Based / Line - Based）
    //        ////PlaceFamilyInstanceForm 主窗体，选择面、族类型、位置和方向
    //        ////FamilyInstanceCreator   核心数据类，管理面列表、族符号列表，执行创建
    //        ////BasedType   枚举类型（Point / Line）
    //        //try
    //        //{
    //        //    var creator = new FamilyInstanceCreator(commandData.Application);
    //        //    var baseTypeWindow = new PlaceFamilyBasedTypeView();
    //        //    var baseTypeVm = new BasedTypeViewModel();
    //        //    baseTypeWindow.DataContext = baseTypeVm;
    //        //    BasedType? selectedType = null;
    //        //    baseTypeVm.TypeSelected += type => selectedType = type;
    //        //    baseTypeVm.CloseWindow = () => baseTypeWindow.Close();
    //        //    baseTypeWindow.ShowDialog();
    //        //    if (selectedType.HasValue)
    //        //    {
    //        //        var placeWindow = new PlaceFamilyInstanceView(creator, selectedType.Value);
    //        //        placeWindow.ShowDialog();
    //        //    }
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////PhaseSample PickPhaseCmd根据构件的创建阶段或拆除阶段来筛选和选择构件。
    //        //var document = commandData.Application.ActiveUIDocument.Document;
    //        //// 检查是否有阶段定义
    //        //if (document.Phases.Size == 0)
    //        //{
    //        //    message = "当前文档中没有定义任何阶段。";
    //        //    return Result.Failed;
    //        //}
    //        //var window = new PhaseSelectorView(document);
    //        //window.ShowDialog();
    //        //// 获取选中的元素并添加到输出集合
    //        //var selectedElements = window.GetSelectedElements();
    //        //if (selectedElements.Size > 0)
    //        //{
    //        //    foreach (Element element in selectedElements)
    //        //    {
    //        //        elements.Insert(element);
    //        //    }
    //        //    message = $"找到 {selectedElements.Size} 个符合条件的构件";
    //        //}

    //        ////PerformanceAdviserControl 通过PerformanceAdviser API执行性能规则检查，特别是检查门的面翻转(FacingFlipped)状态。
    //        ////主要是IPerformanceAdviserRule接口如何调用
    //        ////RuleInfo 规则信息数据类
    //        ////FlippedDoorCheck 实现IPerformanceAdviserRule的自定义规则，检查门是否面翻转
    //        ////TestDisplayDialog   WinForm窗体，显示规则列表供用户选择执行
    //        ////主要功能流程
    //        ////获取规则列表：从PerformanceAdviser获取所有已注册规则
    //        ////识别自定义规则：通过RuleId识别FlippedDoorCheck规则
    //        ////用户选择：在DataGridView中显示规则名称、描述、是否自定义规则
    //        ////执行检查：用户选择要运行的规则，调用PerformanceAdviser执行
    //        ////结果报告：FlippedDoorCheck将检查结果以FailureMessage形式报告到Revit
    //        //var performanceAdviser = PerformanceAdviser.GetPerformanceAdviser();
    //        //var document = commandData.Application.ActiveUIDocument.Document;
    //        //var window = new PerformanceCheckerView(performanceAdviser, document);
    //        //window.ShowDialog();
    //        //地阿妈存在较大问题，直接执行会崩溃。

    //        ////PathReinforcement用于查看和编辑路径钢筋(PathReinforcement)的属性，并提供几何预览。
    //        ////预览绘制	PictureBox + GDI+转为	DrawingVisual + Image是否有必要？
    //        //PathReinforcementView pathReinforcementView = new PathReinforcementView();
    //        //pathReinforcementView.Show();

    //        //PanelSchedule 用于显示获取选中元素的所有参数并格式化，类似Revit自带的"属性"面板。 已转移

    //        ////Openings OpeningInfoView在洞口处画X线，也有Vector类 比较复杂
    //        ////所有辅助类都在OpeningInfo 中。大概10个中等长度辅助族
    //        //OpeningInfoView opening = new OpeningInfoView(new OpeningViewModel(uiApp,new OpeningInfo(new Opening(),uiApp)));
    //        //opening.ShowDialog();

    //        ////ObjectViewer 查看选中元素的三维几何模型/分析模型及其参数信息。
    //        //只转义了主界面逻辑，MathUtil，Sketch，UCS，Vector，Para和ParaFactory都尚缺。GeometryData和Graphics2DData，Graphics3DData负责将构件转为GDI显示，也没有实现。
    //        //Element element = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, "Sth")) as Element;
    //        //ObjectViewerView objectViewerView = new ObjectViewerView(element,uiApp);
    //        //objectViewerView.ShowDialog();

    //        ////NewRoof 包括RoofManager和两种RoofWrapper 代码量太大只有核心翻译，form的主界面有500行？conv也不全
    //        ////创建两种类型的屋顶：迹线屋顶（FootPrintRoof）和拉伸屋顶（ExtrusionRoof）。
    //        ////RoofsManager 数据管理，收集Revit中的屋顶、标高、类型等信息
    //        ////RoofForm    主窗体，显示屋顶列表，创建 / 编辑屋顶
    //        ////RoofEditorForm 编辑窗体，使用PropertyGrid编辑屋顶属性
    //        ////FootPrintRoofWrapper    迹线屋顶的PropertyGrid包装器
    //        ////ExtrusionRoofWrapper    拉伸屋顶的PropertyGrid包装器
    //        ////FootPrintRoofLine   迹线屋顶单条边线的属性包装
    //        ////LevelConverter  标高类型转换器（用于PropertyGrid下拉选择）
    //        ////RoofItem ListView项，显示屋顶基本信息
    //        //RoofsManager roofsManager = new RoofsManager(commandData);
    //        //NewRoofView newRoofView = new NewRoofView(commandData, roofsManager);
    //        //newRoofView.ShowDialog();

    //        ////NewPathReinforcement Revit插件，用于在墙或楼板上创建路径钢筋（PathReinforcement）。又出现MathTool,PRofile交叉识别错误，需后续补充
    //        ////LineTool 直线/ 折线绘图工具，左键添加点，右键完成
    //        ////Profile / ProfileWall / ProfileFloor    处理几何转换和路径钢筋创建
    //        ////Matrix4 3D / 2D坐标转换矩阵（从引用的MathTools）
    //        //PathReinforcementEdit pathReinforcement =new PathReinforcementEdit(null);
    //        //pathReinforcement.ShowDialog();

    //        ////NewOpening 通用开洞工具 再次涉及大量几何元素生成和MathTool编辑方法 代码过长只有核心部分待后续补充
    //        ////用于在墙或楼板上创建各种形状的洞口（矩形、圆形、弧形、多边形）。
    //        ////ITool派生类 绘图工具（直线、矩形、圆形、弧形）
    //        ////Profile / ProfileWall / ProfileFloor    处理几何转换和洞口创建
    //        //NewOpeningView newOpeningView = new NewOpeningView(null);
    //        //newOpeningView.ShowDialog();

    //        //MultiThreading=WorkThread 对墙面进行热力图/应力分析的可视化展示，核心特点是将计算任务放在后台线程执行，通过Idling事件异步更新UI。
    //        //是否应改为Async await方式简化调用？？
    //        //new MultiThreadingFaceAnalysis(commandData);

    //        //MultistoryStairs 问题有点多，待测试 创建和管理多层楼梯（Multistory Stairs），包含三个独立命令： 命令类 功能
    //        //CreateMultistoryStairsCommand 从选中的单层楼梯创建多层楼梯
    //        //AddStairsCommand 向现有多层楼梯中添加层（通过选中标高）
    //        //RemoveStairsCommand 从多层楼梯中移除指定层

    //        ////MaterialProperties 砖和混凝土材质返回有问题，得看API是否改了
    //        ////Revit插件，用于修改结构构件（梁、柱、支撑）的材料属性：
    //        ////获取选中的结构构件
    //        ////显示当前材料属性
    //        ////允许用户切换材料类型（钢 / 混凝土等）
    //        ////修改材料的单位重量
    //        ////应用材料变更到Revit模型
    //        //// 检查选中元素数量
    //        //var selectedIds = uiDoc.Selection.GetElementIds();
    //        //if (selectedIds.Count != 1)
    //        //{
    //        //    TaskDialog.Show("Revit", "请选择一个结构构件（梁、柱或支撑）");
    //        //    return Result.Failed;
    //        //}
    //        //var selectedElement = doc.GetElement(selectedIds.First());
    //        //// 验证选中元素是否为有效的结构构件
    //        //if (!IsValidStructuralElement(selectedElement))
    //        //{
    //        //    TaskDialog.Show("Revit", "请选择一个结构构件（梁、柱或支撑）");
    //        //    return Result.Failed;
    //        //}
    //        //try
    //        //{
    //        //    var dataService = new MaterialPropertiesData(uiApp, selectedElement.Id);
    //        //    var window = new MaterialPropertiesView(dataService);
    //        //    using (var transaction = new Transaction(doc, "修改材料属性"))
    //        //    {
    //        //        transaction.Start();
    //        //        window.ShowDialog();
    //        //        transaction.Commit();
    //        //    }
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("错误", $"操作失败：{ex.Message}");
    //        //    return Result.Failed;
    //        //}

    //        //MASS 中的8个单独工具类 如果要开发造型相关得深入研究
    //        //DistanceToPanels 用于计算分割表面（Divided Surface）上的每个面板到用户指定参考点的距离，并将该距离值写入面板族实例的"Distance"参数中。需要预先选中一个族实例（FamilyInstance）作为距离测量的参考点.遍历当前Revit文档中的所有DividedSurface元素.对每个分割表面的U方向和V方向网格线进行双重循环遍历,通过IsSeedNode判断节点是否为有效种子节点（即存在面板的位置）..获取每个面板的族实例（GetTileFamilyInstance）,找到该族实例的"Distance"参数,计算面板中心点到参考点的三维欧氏距离,将距离值写入参数.
    //        //DividedSurfaceByIntersects 用于演示分割表面（Divided Surface）的交集元素（Intersection Element）的动态添加与移除操作。主要功能流程:获取分割表面,通过硬编码的元素ID（31519）获取目标分割表面,如果未找到，程序会失败并提示用户打开示例族文件。准备交集元素：参考平面和标高（GetPlanes）：3个元素（ID: 1027, 1071, 1072），模型线（GetLines）：6个模型线（ID: 31170~31395）。三步操作演示：步骤1：将参考平面和标高添加为分割表面的交集元素，用于分割表面；步骤2：移除所有已添加的交集元素（清空效果）；步骤3：将模型线添加为交集元素，用不同的元素类型分割表面。 典型用途包括：根据设计条件动态调整曲面分割方案，在不同分割方式之间切换（参考元素 vs 模型线），演示API对分割表面交互式控制的支持
    //        //ManipulateForm 通过API编程方式动态创建和操控放样形体（Loft Form），展示了Forms API中各种子元素操作方法。主要功能：创建放样形体，添加中间轮廓，操作轮廓上的边，分别向相反方向移动（±10单位），移动整个轮廓，演示MoveProfile方法，找到底部轮廓左下角的两个控制点，演示控制点级编辑，在顶部边和底部边之间添加一条连接边，移动新添加的边，演示子元素移动，找到中间轮廓上X坐标接近0的顶点。应用场景：参数化族创建：通过代码生成复杂几何形体，形体优化：验证F形式的API操作可行性，开发参考：演示完整的Form子元素操作API用法
    //        //MeasurePanelArea 批量测量分割表面（Divided Surface）的面板面积，并根据面积范围自动更换面板类型。设置面积范围（最小值/最大值），为三个区间选择对应的面板类型（小于最小值 / 范围内 / 大于最大值）自动将面板更换为用户指定的族类型，生成文本文件记录每个面板的ID和面积，统计三个区间的面板数量
    //        //NewForm 包含了所有种类体量造型方法，拉伸MakeExtrusionForm、融合MakeCapForm、旋转MakeRevolveForm、放样MakeSweptBlendForm、放样融合MakeLoftForm，以及通用方法FormUtils
    //        //PanelEdgeLengthAngle 计算幕墙面板（Curtain Panel）的各边长度和相邻边夹角，并将这些几何数据写入面板族实例的参数中。面板族必须包含以下8个实例参数（否则报错）Length1~4，Angle1~4。夹角计算原理AngleBetweenEdges方法：找到两条边的公共顶点（比较端点是否重合），获取公共顶点处的切线方向(BasisX)，调整方向使夹角指向内侧，通过点积公式 acos(v1·v2) 计算角度
    //        //ParameterValuesFromImage 根据图像数据设置分割表面面板的参数值，实现将灰度图像映射到建筑表皮面板上。设计意图：参数化表皮，根据图像亮度控制面板透明度、颜色或材质；图案生成 用黑白图像在建筑表皮上"绘制"；空设计    白色区域（灰度 = 0）删除面板，形成镂空效果；渐变控制    灰度值映射到0~1范围，控制参数化行为
    //        //PointCurveCreation 包含了七个工具类 PointsParabola在族编辑器中创建沿抛物线弧线分布的参考点（Reference Point）在Z-X平面上生成一系列参考点，这些点遵循幂函数曲线 z = x^p 的分布规律。PointsOnCurve 在族编辑器中创建参考点，并将这些参考点约束到一条模型曲线上，参考点跟随曲线移动。PointsFromExcel 读取指定Excel文件中的X、Y、Z坐标数据，为每一行数据创建一个参考点 。PointsFromTextFile 用于从CSV/文本文件中读取逗号分隔的三维坐标数据，并在族编辑器中批量创建参考点。SineCurve 创建一条通过点集的曲线，这些点遵循余弦函数 y = 10·cos(x) 的分布规律 在族编辑器中生成一系列按余弦函数分布的点，然后通过这些点创建一条平滑曲线。CatenaryCurve 创建基于悬链线方程分布的曲线（Catenary Curve），并生成多条不同缩放因子的曲线。在族编辑器中生成一系列按双曲余弦函数（悬链线）分布的点，通过这些点创建平滑的悬链线曲线，并绘制多组不同参数的曲线。CyclicSurface 创建基于双余弦函数曲面的放样形体（Loft Form），生成周期性波动的三维曲面。在族编辑器中通过沿X方向的一系列曲线创建放样曲面，每条曲线的点遵循 z = 50·(cos(x°) + cos(y°)) 的规律分布。

    //        //////0503 官方代码测试
    //        //NewHostedSweep 主体放样构件创建工具，是用于创建屋顶檐沟、封檐板、楼板边缘等建筑装饰构件的框架代码。
    //        //从Revit元素中提取几何信息（Solid实体）
    //        //识别可用于放置放样构件的边缘(Edge)
    //        //TrackBall用于实现3D视图的交互式旋转和缩放控制，常用于CAD/3D建模软件中的视图操控。
    //        //ElementGeometry Revit元素几何线框显示类，用于将Revit元素的3D实体(Solid)转换为2D线框并在GDI+图形上下文中绘制。
    //        //EdgeBinding Revit边绑定类，用于将Revit模型的边缘(Edge)转换为GDI+可绘制的2D图形，并提供交互式高亮和选择功能。

    //        //NewRebar 钢筋相关 跳过
    //        //MultiplanarRebar 钢筋相关 跳过

    //        ////MoveLinear
    //        ////Revit线性构件移动工具，主要功能：
    //        ////选中验证：
    //        ////检查用户是否选中了元素
    //        ////确保只选中一个元素
    //        ////验证选中的元素是基于线条的构件（LocationCurve）
    //        ////移动操作：
    //        ////获取线段的起点和终点
    //        ////将起点X坐标增加100单位
    //        ////将终点Y坐标增加100单位
    //        ////更新线段位置实现构件移动
    //        ////BUG分析：
    //        ////原代码移动逻辑有问题：起点和终点被移动到了不同方向，会导致线段倾斜而不是平移
    //        ////应该是整体平移而不是分别修改端点
    //        //new MoveLinear(commandData);

    //        ////ModelLines
    //        ////Revit插件程序的功能，并使用C# 7.3语法和WPF MVVM模式进行改写。程序功能分析
    //        ////这是一个Revit模型线创建与管理工具，主要功能：
    //        ////统计功能：统计当前文档中各类模型线的数量
    //        ////ModelLine（直线）、ModelArc（圆弧）、ModelEllipse（椭圆）、ModelHermiteSpline（埃尔米特样条曲线）、ModelNurbSpline（NURBS样条曲线）
    //        ////创建功能：创建新的模型线
    //        ////创建直线（通过起点、终点）、创建圆弧（通过起点、终点、弧上点）、创建椭圆 / 样条曲线（复制现有曲线并偏移）
    //        ////辅助功能：
    //        ////选择 / 创建草图平面、从现有曲线复制创建新曲线
    //        //try
    //        //{
    //        //    var _window = new ModelLineView(uiApp);
    //        //    _window.Show();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////ModelessForm_IdlingEvent 测试方法转为DoorOperator DS找错了重点，应该指执行条件不同 以后再说吧
    //        ////Revit门构件交互操作工具，主要功能：门的方向调整：
    //        ////Left / Right：调整门的左右开向（把手位置）
    //        ////In / Out：调整门的内外开启方向
    //        ////Rotate：同时翻转两个方向（相当于旋转180度）
    //        ////门删除功能：删除选中的门
    //        ////工作机制：
    //        ////使用无模式窗体实现非阻塞UI
    //        ////通过Request机制存储用户操作请求
    //        ////操作基于当前选中的门执行（而非列表选择）
    //        //DoorOperatorByIdleEvent doorOperatorByIdleEvent = new DoorOperatorByIdleEvent();
    //        //doorOperatorByIdleEvent.Show();

    //        ////ModelessForm_ExternalEvent 测试方法转为DoorOperator
    //        ////Revit门构件交互操作工具，主要功能：门的方向调整：
    //        ////Left / Right：调整门的左右开向（把手位置）
    //        ////In / Out：调整门的内外开启方向
    //        ////Rotate：同时翻转两个方向
    //        ////门删除功能：删除选中的门
    //        ////工作机制：
    //        ////使用无模式窗体(Modeless Form)实现非阻塞UI
    //        ////通过外部事件(External Event)机制确保线程安全
    //        //try
    //        //{
    //        //    var _window = new DoorOperatorView(uiApp); 
    //        //    _window.Show();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////MaterialQuantities 无界面 CalculateMaterialQuantities.txt生成在桌面
    //        ////Revit材质工程量计算工具，主要功能：计算三类构件的材质工程量：墙体(Wall)、楼板(Floor)、屋顶(Roof)
    //        ////两种计算模式：
    //        ////净用量(Net)：考虑门窗洞口、开洞等切割元素的实际用量
    //        ////毛用量(Gross)：删除所有切割元素后的完整用量
    //        ////输出内容：
    //        ////各材质的总面积(平方英尺)和体积(立方英尺); 按构件类型统计;按单个构件统计
    //        ////实现原理：
    //        ////通过临时删除洞口、门、窗等切割元素计算毛用量; 使用事务回滚确保不修改原模型
    //        //new MaterialQuantities(doc);

    //        //////0502 官方代码测试
    //        //Loads 官方结构荷载计算管理相关 跳过

    //        ////LevelsProperty DisplayUnitType有问题
    //        ////Revit标高管理工具，用于查看、修改、添加和删除Revit项目中的标高。核心功能
    //        ////显示所有标高 - 在DataGridView中显示标高的名称和高度
    //        ////编辑标高 - 支持直接修改标高名称和高度
    //        ////添加标高 - 新增自定义标高
    //        ////删除标高 - 删除选中的标高
    //        ////单位转换 - 支持Revit内部单位与显示单位的转换
    //        //try
    //        //{
    //        //    var window = new LevelsPropertyView(uiApp);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //} 

    //        ////Journaling 官方日志功能实现
    //        ////Revit日志记录与重放工具，演示如何使用Revit的JournalData功能保存和恢复用户操作。核心功能
    //        ////首次运行 - 显示对话框收集墙体创建参数（类型、标高、起点、终点）
    //        ////创建墙体 - 根据用户输入创建墙体
    //        ////保存到日志 - 将参数保存到Revit日志中（墙体类型名称、标高ID、起点 / 终点坐标）
    //        ////重放操作 - 再次运行时从日志读取参数，自动创建相同墙体
    //        ////无需用户交互 - 有日志数据时直接创建，实现操作自动化
    //        //try
    //        //{
    //        //    if (commandData.Application.ActiveUIDocument?.Document == null)
    //        //    {
    //        //        message = "请先打开一个Revit项目文档";
    //        //        return Result.Failed;
    //        //    }

    //        //    var window = new JournalingView(commandData);
    //        //    var result = window.ShowDialog();

    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////InvisibleParam 
    //        ////Revit共享参数创建工具，演示如何通过参数文件创建可见和不可见的共享参数。
    //        //new InvisibleParam(commandData);

    //        ////InPlaceFamilyAnalyzer 附加多余内容太多，DataGrid绑定有问题 待深化
    //        ////Revit内建族（In - Place Family）分析工具，用于查看内建族实例的属性并可视化其分析模型。核心功能
    //        ////选择内建族实例 - 用户选择一个具有分析模型的内建族
    //        ////显示属性 - 在PropertyGrid中显示实例的属性（ID、名称、族类型、结构类型等）
    //        ////3D模型可视化 - 在PictureBox中显示分析模型的3D轮廓，支持旋转交互
    //        ////矩阵变换 - 实现3D到2D的投影和旋转变换
    //        ////技术架构
    //        ////使用AnalyticalModel.GetCurves()获取分析模型的曲线
    //        ////实现3D矩阵变换（绕X / Y / Z轴旋转）
    //        ////自定义PictureBox3D控件处理鼠标交互
    //        //try
    //        //{
    //        //    var window = new InPlaceFamilyAnalyzerView(uiApp);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //ImportExport 已基本实现了，需要的时候再看吧
    //        //视图相关格式：DWG、DXF、SAT、DWF、DGN、Image 使用 ExportWithViewsForm（需要选择视图）
    //        //Civil3D格式：使用专用 ExportCivil3DForm 并验证数据有效性
    //        //简单格式：GBXML、FBX 直接使用 SaveFileDialog 选择路径

    //        //CreateOrthogonalGrid 仅转正交轴网生成逻辑
    //        ////GridCreation有三种方法 提取公共Validation验证类还是用了WinForm待替换
    //        ////建立正交轴网
    //        ////建立弧线放射轴网
    //        ////基于选择线建立轴网
    //        ////正交轴网生成代码
    //        //try
    //        //{
    //        //    // 显示WPF窗口
    //        //    var window = new CreateOrthogonalGridView(uiApp);
    //        //    var result = window.ShowDialog();
    //        //    //var result = window.ShowModal();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //RevitDefaultFamilyTypes  Revit默认族类型管理器，用于查看和修改Revit中各种族类别的默认类型设置
    //        //列出所有族类别（门窗、家具、照明设备等）
    //        //显示每个类别的有效族类型
    //        //查看和修改默认族类型
    //        //支持特殊类别：MatchModel和MatchDetail（组件类型）

    //        //RevitDefaultElementTypes 定义并非一般Window而是page，暂不深究实现
    //        //Revit默认元素类型管理器，用于查看和修改Revit中各种元素类型的默认设置。核心功能
    //        //显示所有元素类型组（如墙类型、楼板类型、尺寸标注类型等）
    //        //列出每个组的有效候选类型
    //        //查看当前的默认类型
    //        //修改默认类型（通过下拉选择）
    //        //try
    //        //{
    //        //    if (uiDoc?.Document == null)
    //        //    {
    //        //        message = "没有活动的文档";
    //        //        return Result.Failed;
    //        //    }
    //        //    // 创建视图模型
    //        //    var viewModel = new DefaultElementTypesViewModel(uiApp);
    //        //    viewModel.SetDocument(uiDoc.Document);
    //        //    // 创建视图
    //        //    var view = new DefaultElementTypesView();
    //        //    view.SetViewModel(viewModel);
    //        //    // 注册并显示可停靠面板
    //        //    var paneId = DefaultElementTypesView.PaneId;
    //        //    var pane = uiApp.GetDockablePane(paneId);
    //        //    if (pane == null)
    //        //    {
    //        //        uiApp.RegisterDockablePane(paneId, "默认元素类型管理器", view);
    //        //        pane = uiApp.GetDockablePane(paneId);
    //        //    }
    //        //    else
    //        //    {
    //        //        // 更新现有面板内容
    //        //        //pane.Close();
    //        //        uiApp.RegisterDockablePane(paneId, "默认元素类型管理器", view);
    //        //        pane = uiApp.GetDockablePane(paneId);
    //        //    }
    //        //    // 显示面板
    //        //    pane.Show();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////GeometryCreation_BooleanOperation 几何布尔运算？？RevitCSGGenerator
    //        ////Revit CSG（Constructive Solid Geometry，构造实体几何）工具，用于创建和显示布尔运算后的3D实体。核心功能
    //        ////创建5个基本几何体：立方体、球体、沿X / Y / Z轴的圆柱体
    //        ////执行布尔运算：并集、交集、差集
    //        ////构建CSG树：按照特定顺序组合几何体
    //        ////可视化显示：在Revit中创建3D视图并着色显示结果
    //        //new RevitCSGGenerator(commandData);

    //        ////GenericStructuralConnection
    //        ////Revit结构连接管理插件，提供以下功能：通用结构连接操作：
    //        ////创建：选择结构构件创建通用连接
    //        ////删除：删除选中的连接
    //        ////读取：显示连接信息（ID、类型、连接的构件ID）
    //        ////更新：向现有连接添加更多构件
    //        ////详细结构连接操作：
    //        ////创建：创建特定类型的详细连接（如US夹持角钢）
    //        ////更改：更改连接类型（如改为剪切板）
    //        ////复制：复制连接（偏移20单位）
    //        ////匹配属性：在两个连接间复制属性
    //        ////重置：将详细连接恢复为通用类型
    //        //try
    //        //{
    //        //    // 创建并显示WPF窗口
    //        //    var window = new GenericStructuralConnectionView(uiDoc);
    //        //    //// 设置Revit作为父窗口，确保窗口模态正确
    //        //    //var revitWindow = new WindowInteropHelper(uiDoc.Application.MainWindowHandle).Handle;
    //        //    //new WindowInteropHelper(window).Owner = revitWindow;
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////GenerateFloor 
    //        ////Revit 基于选中墙体自动生成楼板工具，用户选中一组构成封闭轮廓的墙体，程序自动分析墙体轮廓并生成对应的结构楼板。
    //        //try
    //        //{
    //        //    var window = new GenerateFloorView(commandData.Application);
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //////0501 官方代码测试
    //        ////界面有点问题，难得调用Document来源于creation的而不是db
    //        ////FrameBuilder 梁相关工具 主要是建模，也有改类型名和赋值类型的界面，暂未细分
    //        ////Revit 结构框架自动生成工具，用于批量创建柱、梁、支撑组成的建筑结构框架。核心功能
    //        ////批量生成结构柱：按矩阵排列创建结构柱
    //        ////自动生成梁：在柱顶之间创建水平梁
    //        ////自动生成支撑：在柱间创建 X 形斜撑
    //        ////多楼层支持：可生成多楼层结构
    //        ////类型管理：支持复制、编辑结构构件类型参数
    //        //try
    //        //{
    //        //    var window = new StructuralFrameBuilderView(commandData.Application);
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //FreeFormElement Revit 负型块（Negative Block）创建工具，用于基于现有几何体创建与其互补的负空间族构件。
    //        //用户选择一个目标元素（如家具、设备）和一组边界曲线，自动创建一个与其外部形状互补的负型块家族实例。
    //        //TargetElementSelectionFilter	过滤目标元素	元素必须包含有效实体（Solid）
    //        //BoundarySelectionFilter 过滤边界曲线  必须是曲线元素，位于 XY 平面，支持 Line/ Arc
    //        //空间预留和模具设计工具，通过实体布尔运算自动生成几何体的互补形状，适用于需要精确切割或预留空间的设计场景。

    //        ////FoundationSlab 初始化有空值bug，GeometryDrawingService方法用于在界面重绘概要，是不是比GDI32更新？
    //        ////Revit 基础筏板自动创建工具，用于分析建筑模型中的底层楼板，并自动生成相应的基础筏板。核心功能
    //        ////识别底层楼板：找到最低标高处所有非结构楼板
    //        ////图形化预览：显示楼板轮廓和选中的八角形区域
    //        ////交互选择：用户可选择需要生成基础筏板的楼板
    //        ////批量生成：自动将选中的楼板替换为基础筏板
    //        //try
    //        //{
    //        //    var window = new FoundationSlabView(commandData.Application);
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////FireRating VB转C#  缺door的Parameter扩展方法，和Excel库 需求不大
    //        //Revit 防火等级参数管理工具集，包含三个命令：
    //        //1.ApplyParameter - 应用参数
    //        //创建共享参数文件（FireRating.txt）
    //        //在 Revit 中创建 / 获取 "Fire Rating" 共享参数
    //        //将参数绑定到"门"类别
    //        //2.ExportFireRating - 导出数据
    //        //获取所有门的 Fire Rating 参数值
    //        //导出到 Excel 文件（FireRating.xls）
    //        //3.ImportFireRating - 导入数据
    //        //从 Excel 文件读取防火等级数据
    //        //批量更新门的 Fire Rating 参数值
    //        //try
    //        //{
    //        //    var mainWindow = new FireRatingManagerView();
    //        //    var viewModel = new FireRatingManagerViewModel(commandData.Application);
    //        //    mainWindow.DataContext = viewModel;
    //        //    mainWindow.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////SortFamilyParametersView  
    //        ////Revit 族文件参数排序工具，包含两个功能：
    //        ////1.SortFamilyFilesParamsForm
    //        ////功能：批量处理指定目录下的族文件（.rfa），对其中的参数进行排序
    //        ////操作：选择文件夹 → 选择排序方式（A→Z 或 Z→A）→ 批量修改族参数顺序
    //        ////2.SortLoadedFamiliesParamsForm
    //        ////功能：对当前 Revit 文档中已加载的族的参数进行排序
    //        ////操作：选择排序方式（A→Z 或 Z→A）→ 立即执行
    //        //try
    //        //{
    //        //    var mainWindow = new SortFamilyParametersView(commandData.Application);
    //        //    mainWindow.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //FabricationPartLayout  Revit 预制加工（Fabrication）工具集 暂无需求
    //        //Ancillaries	查询预制构件的附件信息	FabricationPart.GetPartAncillaryUsage()
    //        //ButtonGroupExclusions 设置服务按钮和组的排除项    OverrideServiceButtonExclusion(), SetServiceGroupExclusions()
    //        //ConvertToFabrication 将设计元素转换为预制构件    DesignToFabricationConverter.Convert()
    //        //CustomData 预制构件自定义数据管理工具，提供了查询和设置预制构件自定义数据的功能。
    //        //ExportToPCF Revit 预制构件 PCF 文件导出工具，用于将选中的预制构件导出为 PCF（Pipe Component File，管道组件文件）格式。
    //        //FabricationPartLayout Revit 预制构件布局自动创建工具，通过代码自动化生成复杂的 HVAC（暖通空调）和管道系统的预制构件布局。展示了 Revit Fabrication API 的几乎所有核心功能，适合作为预制加工二次开发的参考模板。
    //        //HangorRods Revit 预制构件吊架（Hanger）吊杆管理工具集，包含5个独立命令，用于控制吊架吊杆的挂接状态、长度和结构延伸长度。
    //        //OptimizeStraights Revit 预制构件直管长度优化工具，用于自动优化选中预制构件中直管段（Straight Parts）的长度。
    //        //PartInfo Revit 预制构件信息查询工具，用于显示选中预制构件的详细属性和参数。
    //        //PartRenumber Revit 预制构件自动重编号工具，用于为选中的预制构件自动生成并分配统一的编号（Item Number）。
    //        //SplitStraight Revit 预制直管段分割工具，用于将选中的预制直管构件从中点位置分割成两个独立的构件
    //        //StretchAndFit Revit 预制构件拉伸适配工具，用于将一个预制构件拉伸并连接到另一个目标构件上，自动生成中间的连接构件。。将起点构件（非直管 / 非三通 / 非吊架）的空闲连接器拉伸，使其连接到目标构件的空闲连接器上，并自动生成适配的连接段。

    //        //ExternalResourceUIServer 暂无需求
    //        //Revit 外部资源 UI 服务器（External Resource UI Server）示例，作为上一个问题的配套 UI 层组件，负责处理外部资源加载过程中的用户交互和结果反馈。
    //        //ExternalResourceDBServerRevit 外部资源服务器（External Resource Server）示例程序，演示了如何为 Revit 创建自定义的资源提供程序，用于动态提供图集数据（Keynotes）和 Revit 链接文件。核心功能
    //        //1.图集数据（Keynotes）服务
    //        //为德国和法国用户从虚拟数据库提供图集数据
    //        //为其他语言用户从本地文件提供图集数据
    //        //支持不同版本的图集数据管理
    //        //2.Revit 链接服务
    //        //从服务器提供 Revit 链接文件
    //        //自动缓存到本地
    //        //支持共享坐标更新回传
    //        //两者配合实现完整的 Revit 外部资源服务：DB Server 负责数据和逻辑，UI Server 负责交互和反馈。

    //        //ExternalCommandRegistration Revit 外部命令注册示例程序，演示了 Revit 插件开发的几个核心概念：外部命令、命令可用性控制和应用生命周期管理。

    //        //ExtensibleStorage 这两个相关方法DS转义非常糟糕 Gemini得全部重做
    //        //Revit 可扩展存储管理工具集，提供了查询和删除文档中所有扩展存储数据的功能。核心功能
    //        //存储查询（QueryStorage）：列出当前文档中所有包含扩展存储的 Schema 和元素信息
    //        //存储删除（DeleteStorage）：删除当前文档中所有扩展存储数据
    //        //工具类（StorageUtility）：提供通用的存储查询辅助方法
    //        //new ExtensibleStorageStatistics(commandData);
    //        //new ExtensibleStorageDeletion(commandData);

    //        //ExtensibleStorageManager
    //        ////Revit 可扩展存储（Extensible Storage）管理工具，用于创建、管理和操作 Revit 的扩展存储数据。核心功能
    //        ////创建 Schema：在 Revit 文档中创建自定义数据架构
    //        ////存储数据：将数据存储到 ProjectInformation 元素中
    //        ////导入 / 导出 XML：支持 Schema 的序列化和反序列化
    //        ////查询编辑：查询和编辑已存储的 Entity 数据
    //        ////支持复杂数据类型：基本类型、数组、字典、子实体等
    //        //try
    //        //{
    //        //    var document = commandData.Application.ActiveUIDocument.Document;
    //        //    var addInId = commandData.Application.ActiveAddInId.GetGUID().ToString();
    //        //    var mainWindow = new ExtensibleStorageManagerView();
    //        //    var viewModel = new ExtensibleStorageManagerViewModel(document, addInId);
    //        //    mainWindow.DataContext = viewModel;
    //        //    mainWindow.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}


    //        //ErrorHandling Gemini分析过，待想办法批量处理

    //        //EnergyAnalysisModel 跳过，没啥需求

    //        //ElementsBatchCreator 增补DocumentExtensions中方法待测试
    //        ////Revit 批量创建元素工具，演示如何使用 Revit API 的批量创建方法一次性生成多种类型的建筑元素。
    //        ////核心功能
    //        ////批量创建区域（Area）：在指定楼层创建多个区域
    //        ////批量创建结构柱（Column）：在指定位置创建多个结构柱
    //        ////批量创建房间（Room）：在封闭区域内创建多个房间
    //        ////批量创建文字注释（TextNote）：在视图中创建多个文字注释
    //        ////批量创建墙体（Wall）：创建多种形状的墙体（直墙、弧形墙）
    //        //try
    //        //{
    //        //    var batchCreator = new ElementsBatchCreator(commandData);
    //        //    var result = batchCreator.CreateAllElements();

    //        //    if (!result)
    //        //    {
    //        //        message = "部分元素创建失败，请查看详细信息。";
    //        //        return Result.Failed;
    //        //    }

    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = $"批量创建失败：{ex.Message}";
    //        //    return Result.Failed;
    //        //}




    //        ////DynamicModelUpdate =>  DMUAssociativeSectionUpdate
    //        ////计算旋转角度 应提取作为公共方法
    //        ////Revit 关联剖面更新器，实现剖面视图与窗户元素的动态关联，当窗户移动或修改时，剖面视图自动跟随调整位置和方向。核心功能
    //        ////关联剖面与窗户：用户选择一个剖面视图和一个窗户，建立二者之间的关联关系
    //        ////自动跟随更新：当窗户位置或方向改变时，剖面视图自动调整位置和角度
    //        ////智能方向计算：根据窗户的朝向自动计算剖面的旋转角度
    //        ////动态注册 / 注销：使用 Revit Updater 机制监听元素变化
    //        //new DMUAssociativeSectionUpdate(commandData);

    //        //DWGFamilyCreation 项目如果需要表达是否有必要重建
    //        // 族文档 DWG 导入工具，主要功能是在族编辑环境中将 DWG 文件导入并创建参数化族。
    //        //核心功能
    //        //验证文档类型：确保当前文档是族文档（.rfa）
    //        //查找目标视图：在族文档中找到名为 "Ref. Level" 的非模板楼层平面视图
    //        //导入 DWG 文件：将指定的 Desk.dwg 文件导入到族文档中
    //        //添加类型参数：自动添加两个族类型参数记录导入信息

    //        ////Revit 视图复制工具，主要功能是将一个文档中的明细表视图和草图视图复制到另一个打开的文档中。核心功能
    //        ////复制明细表（ViewSchedule）：将源文档中的明细表复制到目标文档
    //        ////复制草图视图（ViewDrafting）：复制草图视图及其内部的所有详图元素
    //        ////智能处理依赖关系：使用 CopyElements API 确保依赖元素只被复制一次
    //        ////自动处理冲突：通过自定义处理器处理重复类型名称和警告消息
    //        ////技术要点
    //        ////要求同时打开两个 Revit 文档
    //        ////使用 ElementTransformUtils.CopyElements 跨文档复制
    //        ////使用 IDuplicateTypeNamesHandler 自动处理类型名称冲突
    //        ////使用 IFailuresPreprocessor 过滤警告消息
    //        //DuplicateView duplicateView = new DuplicateView();
    //        //var app = commandData.Application.Application;
    //        //var currentDoc = commandData.Application.ActiveUIDocument.Document;
    //        //// 查找目标文档：必须恰好有两个打开的文档
    //        //var openDocs = app.Documents.Cast<Document>().ToList();
    //        //if (openDocs.Count != 2)
    //        //{
    //        //    TaskDialog.Show("无目标文档",
    //        //        "此工具需要同时打开两个文档（一个源文档和一个目标文档）");
    //        //    return Result.Cancelled;
    //        //}
    //        //// 确定目标文档（不是当前文档的那个）
    //        //var targetDoc = openDocs.FirstOrDefault(d => d.Title != currentDoc.Title);
    //        //if (targetDoc == null)
    //        //{
    //        //    message = "无法确定目标文档";
    //        //    return Result.Failed;
    //        //}
    //        //// 收集当前文档中的所有明细表和草图视图
    //        //var collector = new FilteredElementCollector(currentDoc);
    //        //// 筛选明细表和草图视图类型
    //        //var viewTypes = new List<Type> { typeof(ViewSchedule), typeof(ViewDrafting) };
    //        //var multiFilter = new ElementMulticlassFilter(viewTypes);
    //        //collector.WherePasses(multiFilter);
    //        //// 跳过视图特定明细表（如修订明细表），这些不能独立复制
    //        //collector.WhereElementIsViewIndependent();
    //        //// 复制明细表
    //        //var schedules = collector.OfType<ViewSchedule>().ToList();
    //        //if (schedules.Any())
    //        //{
    //        //    DuplicateViewUtils.DuplicateSchedules(currentDoc, schedules, targetDoc);
    //        //}
    //        //// 复制草图视图及其内容
    //        //var draftingViews = collector.OfType<ViewDrafting>().ToList();
    //        //var newDetailCount = 0;
    //        //if (draftingViews.Any())
    //        //{
    //        //    newDetailCount = DuplicateViewUtils.DuplicateDraftingViews(
    //        //        currentDoc, draftingViews, targetDoc);
    //        //}
    //        //// 显示统计结果
    //        //TaskDialog.Show("复制统计", $"复制完成：\n" + $"\t{schedules.Count} 个明细表。\n" +
    //        //    $"\t{draftingViews.Count} 个草图视图。\n" + $"\t{newDetailCount} 个新详图元素。");


    //        //高级功能先有个印象以后再考虑是否深挖
    //        //DuplicateGraphics管理通过 DirectContext3D 绘制的自定义图形。主要包含两个外部命令：
    //        //1.CommandDuplicateGraphics - 复制图形命令
    //        //2.CommandClearExternalGraphics - 清除图形命令
    //        //DirectContext3D 是 Revit API 提供的底层图形绘制接口，允许开发者：
    //        //直接绘制：绕过 Revit 元素系统，直接在视图上绘制几何图形
    //        //高性能渲染：适合大量动态图形的实时显示
    //        //临时视觉反馈：用于选择高亮、分析结果展示、测量标注等

    //        //DoorSwing 门摆向修改，如果跟族关联，那似乎没啥需求
    //        //Revit插件，用于管理门的开向（左开/右开）和相关参数。我来分析程序功能并使用C# 7.3语法和WPF MVVM模式重构。
    //        //初始化门开向：根据门的几何形状和国家标准，设置门的开向参数（左开 / 右开 / 双开等）
    //        //更新门参数：更新门实例的开向、内外门标志、从房间 / 到房间信息
    //        //更新门几何：根据从房间 / 到房间信息调整门的几何方向
    //        //自动保存更新：在文档保存时自动更新门信息

    //        ////0430 官方代码测试
    //        //DockableDialogs 可停靠窗口实现，等需要时再测暂跳过

    //        //DisplacementElementAnimation
    //        //Revit 位移结构模型动画工具，主要功能包括：
    //        //位移元素动画：对 Revit 中设置了位移（Displacement）的结构模型元素进行动画演示
    //        //两种动画模式：
    //        //自动播放模式：连续播放完整动画
    //        //步进模式：手动控制动画步骤，逐帧推进

    //        //CommandDisabler 不好测试，没啥场合应用
    //        //Revit命令禁用工具，主要功能包括：
    //        //启动时拦截指定命令：在Revit启动时查找并绑定目标命令
    //        //禁用命令执行：当用户尝试执行该命令时，拦截并显示提示信息
    //        //关闭时清理绑定：在Revit关闭时移除命令绑定

    //        ////FindSouthFacingWalls 找南向墙、南向窗的方法比较乱 参考价值一般
    //        ////Revit朝南外墙查找工具，主要功能包括：
    //        ////收集外墙：筛选出所有外墙类型（Function参数为Exterior）
    //        ////计算朝向：通过墙的方向向量计算外法线方向
    //        ////判断朝南：检查法线方向是否在朝南范围内（±45度）
    //        ////两种模式：支持使用项目北向或默认坐标系
    //        ////选中结果：将朝南的外墙添加到当前选择集中
    //        //FindSouthFacingWalls findSouthFacingWalls = new FindSouthFacingWalls(commandData);

    //        ////DimensionHorizontalMover 待测试
    //        ////Revit尺寸标注水平移动工具，主要功能包括：
    //        ////沿尺寸线方向移动引线：将尺寸标注的引线端点沿尺寸线方向水平移动固定距离（-10单位）
    //        ////支持多线段尺寸：处理包含多个线段的尺寸标注
    //        ////批量处理：支持同时处理多个选中的尺寸标注
    //        //DimensionHorizontalMover dimensionHorizontalMover = new DimensionHorizontalMover(commandData);

    //        ////DimensionMoveToPickedPoint 待测试
    //        ////Revit尺寸标注引线端点移动工具，主要功能包括：
    //        ////选择尺寸标注：获取当前选中的尺寸标注元素
    //        ////拾取目标点：让用户在视图中拾取一个点作为引线的新端点
    //        ////移动引线端点：将尺寸标注的引线端点移动到拾取的位置
    //        ////处理多线段尺寸：支持包含多个线段的尺寸标注，按顺序偏移设置各线段端点
    //        //DimensionMoveToPickedPoint moveToPickedPoint = new DimensionMoveToPickedPoint(commandData);

    //        //DesignOptionView 没啥实际作用
    //        ////Revit设计选项查看工具，主要功能包括：
    //        ////收集设计选项：从当前Revit文档中收集所有设计选项
    //        ////显示列表：在对话框中以列表形式显示所有设计选项的名称
    //        ////查看信息：用户可以查看当前文档中存在哪些设计选项
    //        //try
    //        //{
    //        //    var window = new DesignOptionView();
    //        //    var viewModel = new DesignOptionViewModel(commandData);
    //        //    window.DataContext = viewModel;

    //        //    // 获取Revit主窗口并设置为所有者
    //        //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    if (revitHandle != IntPtr.Zero)
    //        //    {
    //        //        var helper = new WindowInteropHelper(window);
    //        //        helper.Owner = revitHandle;
    //        //    }

    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //DimensionCleaner 功能可用，需要先选择有点操作反直觉 增补DocumentExtensions中方法待测试
    //        //Revit尺寸标注批量删除工具，主要功能包括：
    //        //获取Revit选中的元素：从当前选择集中获取所有选中的元素
    //        //筛选未固定的尺寸标注：过滤出类型为Dimension且未被固定(Pinned = false)的尺寸标注
    //        //批量删除：在事务中批量删除所有符合条件的尺寸标注
    //        //DimensionCleaner dimensionCleaner = new DimensionCleaner(commandData);

    //        ////DeckPropertyView 对理解楼板有点用，实际作用不大
    //        ////Revit楼板 / 压型钢板属性查看工具，主要功能包括：
    //        ////选择楼板 / 压型钢板：从Revit中选中一个或多个楼板元素
    //        ////解析复合结构：读取楼板类型中的复合结构层
    //        ////识别压型钢板层：检测并专门处理压型钢板（Deck）层
    //        ////显示属性信息：展示材料、厚度、压型钢板轮廓参数等详细信息
    //        //try
    //        //{
    //        //    var window = new DeckPropertyView();
    //        //    var viewModel = new DeckPropertyViewModel(commandData);
    //        //    window.DataContext = viewModel;
    //        //    // 获取Revit主窗口并设置为所有者
    //        //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    if (revitHandle != IntPtr.Zero)
    //        //    {
    //        //        var helper = new WindowInteropHelper(window);
    //        //        helper.Owner = revitHandle;
    //        //    }
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////DatumPropagationView 3个工具的界面增加了不少元素，跟原始意图相差很大
    //        ////Revit基准线（轴线 / 标高 / 参照平面）范围传播工具，主要功能包括：
    //        ////选择基准线：从当前选中的基准线中获取一个作为源
    //        ////获取传播视图列表：获取该基准线可以传播到的所有视图
    //        ////选择目标视图：用户勾选需要应用相同范围设置的视图
    //        ////传播范围：将当前视图中的基准线范围设置传播到选中的目标视图
    //        //try
    //        //{
    //        //    var window = new DatumPropagationView(commandData);
    //        //    var viewModel = new DatumPropagationViewModel(commandData);
    //        //    window.DataContext = viewModel;
    //        //    // 获取Revit主窗口并设置为所有者
    //        //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    if (revitHandle != IntPtr.Zero)
    //        //    {
    //        //        var helper = new WindowInteropHelper(window);
    //        //        helper.Owner = revitHandle;
    //        //    }
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////DatumAlignment.可视化修改？没啥意义吧
    //        ////Revit基准线（轴线/标高/参照平面）对齐工具，主要功能包括：
    //        ////选择参考基准线：从选中的基准线中选择一条作为对齐参考
    //        ////对齐其他基准线：将所有选中的基准线按照参考基准线的方向对齐（X / Y / Z方向）
    //        //try
    //        //{
    //        //    var window = new DatumAlignmentView(commandData);
    //        //    var viewModel = new DatumAlignmentViewModel(commandData);
    //        //    window.DataContext = viewModel;
    //        //    // 获取Revit主窗口并设置为所有者
    //        //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
    //        //    if (revitHandle != IntPtr.Zero)
    //        //    {
    //        //        var helper = new WindowInteropHelper(window);
    //        //        helper.Owner = revitHandle;
    //        //    }
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////DatumStyleModification 可视化修改？没啥意义吧
    //        ////Revit基准线（标高 / 轴线 / 参照平面）样式修改工具，主要功能包括：
    //        ////控制基准线两端的气泡显示 / 隐藏
    //        ////添加 / 删除基准线两端的弯头（肘部）
    //        ////切换基准线端点的2D / 3D范围模式
    //        //try
    //        //{
    //        //    var window = new DatumStyleModificationView(commandData);
    //        //    // 获取Revit主窗口句柄并设置为所有者
    //        //    var revitWindow = System.Diagnostics.Process
    //        //        .GetCurrentProcess()
    //        //        .MainWindowHandle;
    //        //    if (revitWindow != IntPtr.Zero)
    //        //    {
    //        //        var helper = new System.Windows.Interop.WindowInteropHelper(window);
    //        //        helper.Owner = revitWindow;
    //        //    }
    //        //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //CreateCurvedBeam 问题是
    //        ////Revit弧形梁创建工具，主要功能包括：
    //        ////选择梁类型：从项目中加载所有结构框架族类型
    //        ////选择标高：选择梁的放置标高
    //        ////创建三种曲线梁：圆弧梁、椭圆弧梁、样条曲线梁
    //        //// 创建WPF窗口并设置为Revit主窗口的所有者
    //        //var mainWindow = new CreateCurvedBeamView(commandData);
    //        //mainWindow.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero
    //        //    ? System.Windows.Interop.HwndSource.FromHwnd(
    //        //        System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle)?.RootVisual as CreateCurvedBeamView
    //        //    : null;
    //        ////// 设置数据上下文，传入commandData
    //        ////var viewModel = new CreateCurvedBeamViewModel(commandData);
    //        ////mainWindow.DataContext = viewModel;
    //        //// 显示模态窗口
    //        ////mainWindow.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
    //        //mainWindow.ShowDialog();

    //        ////CurtainWallGrid 也包括了MathTools的Vector部分还有Matrix 超长方法代码超过5k行，仅生成主干框架部分后续待补
    //        ////Revit幕墙网格编辑工具，主要功能包括：
    //        ////创建幕墙：在指定视图中绘制基线，选择幕墙类型创建幕墙
    //        ////编辑网格：添加 / 删除U / V方向网格线、添加 / 恢复网格线段、锁定 / 解锁网格线、移动网格线
    //        ////添加 / 删除竖梃
    //        ////查看网格属性
    //        //var window = new CreateCurtainWallView(commandData);
    //        //var result = window.ShowDialog();

    //        //CurtainSystem 体系比较庞大，先绕过Command 人口方法，Vector方法提出来先看看四维齐次坐标向量类
    //        //四维向量类，主要用于三维空间计算（第四分量W默认为1.0，常用于齐次坐标）。主要功能包括：
    //        //存储X、Y、Z、W四个分量
    //        //支持从Revit的XYZ类型转换
    //        //计算两个向量的叉积（得到法向量）
    //        //原始代码存在一个小问题：CrossProduct方法没有设置结果向量的W分量，会使用默认值1.0。在齐次坐标系中，叉积结果应是方向向量（垂直于平面的法向量），W应为0而非1。改写版本已修正。

    //        //CreateWallsUnderBeam 大致成功，待完善
    //        ////Revit 在梁下方创建墙体插件，主要功能：
    //        ////选择梁 - 用户选择一根或多根水平梁
    //        ////验证梁为水平 - 检查每根梁的分析模型线是否为水平
    //        ////选择墙体类型 - 通过对话框选择墙体类型和是否结构墙
    //        ////创建墙体 - 沿每根梁的分析模型线创建墙体，位于梁下方
    //        //try
    //        //{
    //        //    var window = new CreateWallsUnderBeamView(commandData);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////0429 官方代码测试
    //        ////WallFromBeamsCreatView，有GeometryHelper参考，DataLoadingService类重叠
    //        ////Revit 基于梁轮廓创建墙体插件，主要功能：
    //        ////选择梁 - 用户选择多根梁（首尾相连形成闭合轮廓）
    //        ////验证垂直平面 - 检查所有梁是否在同一垂直平面内
    //        ////验证闭合轮廓 - 检查梁是否能形成闭合轮廓
    //        ////选择墙体类型 - 通过对话框选择墙体类型和是否结构墙
    //        ////创建墙体 - 沿着梁轮廓创建墙体，并设置标高和偏移
    //        //try
    //        //{
    //        //    var window = new WallFromBeamsCreatView(commandData);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        ////SectionViewCreator 有XYZMath方法可调用 功能基本可直接使用
    //        ////Revit 剖面视图自动创建插件，主要功能：
    //        ////选择线性元素 - 支持墙体(Wall)、梁(Beam)、楼板(Floor)
    //        ////计算截面位置 - 自动计算元素的中点作为剖面位置
    //        ////计算视图方向 - 根据元素类型确定剖面的方向和范围
    //        ////创建剖面视图 - 在元素中点处生成垂直剖面图
    //        ////创建详图视图 - 额外功能，创建空白详图视图
    //        ////try
    //        ////{
    //        ////    var document = commandData.Application.ActiveUIDocument.Document;
    //        ////    var transaction = new Transaction(document, "创建详图视图");
    //        ////    transaction.Start();
    //        ////    var collector = new FilteredElementCollector(document);
    //        ////    var viewFamilyType = collector
    //        ////        .OfClass(typeof(ViewFamilyType))
    //        ////        .Cast<ViewFamilyType>()
    //        ////        .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);
    //        ////    if (viewFamilyType is null)
    //        ////    {
    //        ////        return Result.Failed;
    //        ////    }
    //        ////    else
    //        ////    {  
    //        ////        ViewDrafting draftingView = ViewDrafting.Create(document, viewFamilyType.Id);
    //        ////        if (draftingView is null)
    //        ////        {
    //        ////            message = "无法创建详图视图";
    //        ////            transaction.RollBack();
    //        ////            return Result.Failed;
    //        ////        }
    //        ////        transaction.Commit();
    //        ////        TaskDialog.Show("Revit", $"详图视图创建成功！视图名称: {draftingView.Name}");
    //        ////    }
    //        ////    return Result.Succeeded;
    //        ////}
    //        ////catch (Exception ex)
    //        ////{
    //        ////    message = ex.Message;
    //        ////    return Result.Failed;
    //        ////}
    //        ////下一句与上面是两种方式生成剖面
    //        //SectionViewCreator sectionViewCreator = new SectionViewCreator(commandData);

    //        ////TrussCreator GeometryExtensions与AirHandlerCreator 共享partial class 
    //        ////Revit 桁架族创建插件，主要功能：
    //        ////在桁架族文档中创建单榀桁架 - 使用参考平面构建桁架几何
    //        ////创建桁架构件 - 生成上弦杆、下弦杆、腹杆等
    //        ////添加对齐约束 - 将桁架线锁定到参考平面
    //        ////添加角度尺寸约束 - 确保腹杆角度可调且稳定
    //        //TrussCreator trussCreator =new TrussCreator(commandData);

    //        ////主要功能用不上，有一些几何基本方法GeometryDataService GeometryService(RevitBeamSystemCreatorView也有)可参考
    //        ////Revit 区域钢筋创建插件，主要功能：
    //        ////选择结构元素 - 支持墙体(Wall)和楼板(Floor)
    //        ////验证几何条件 - 检查选中的元素是否为矩形、垂直 / 水平面
    //        ////设置钢筋参数 - 通过属性网格配置布局规则、钢筋层方向等
    //        ////创建区域钢筋 - 根据配置参数自动生成AreaReinforcement
    //        //try
    //        //{
    //        //    var window = new AreaReinforcementCreatView(commandData);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //////CreateSharedParameter 共享参数方法 没成功 ，待深化测试
    //        //////创建并绑定共享参数,不存在则创建,绑定到墙体类别,不设置值，目标类别级别的绑定
    //        //CreateSharedParameter createSharedParameter = new CreateSharedParameter(commandData);
    //        //////SetSharedParameterCommand
    //        //////Revit 共享参数批量设置插件（VB.NET原始代码），主要功能：
    //        //////读取共享参数文件 - 从共享参数文件中获取名为"APIParameter"的参数定义
    //        //////筛选元素 - 获取所有非类型元素（实例元素）
    //        //////过滤墙体 - 只处理类别为"Walls"的元素
    //        //////批量设置参数 - 为所有墙元素设置共享参数值为"Hello Revit"
    //        //SetSharedParameterCommand setSharedParameterCommand = new SetSharedParameterCommand(commandData);

    //        ////PatternManagerView 缺导入窗体，方法执行细节有错误待细化
    //        //////Revit 图案样式应用插件，主要功能：
    //        //////填充图案管理 - 显示并应用填充图案(FillPattern)到表面或切割面
    //        //////线型图案管理 - 显示并应用线型图案(LinePattern)到网格线
    //        //////图案创建 - 支持创建简单填充图案、复杂填充图案和线型图案
    //        //try
    //        //{
    //        //    var window = new PatternManagerView(commandData);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //WallDimensionDemo给选择墙体加标注，逻辑基本无问题但没成功
    //        //Revit 墙体尺寸标注插件，主要功能：
    //        //选择墙体 - 用户选择一面或多面基础墙(Basic Wall)
    //        //分析分析模型 - 获取墙体的分析模型中的非垂直线段
    //        //创建尺寸标注 - 从墙体起点到终点创建尺寸标注
    //        //只支持2D视图 - 在3D视图或图纸视图中会提示错误
    //        //WallDimensionDemo wallDimensionDemo = new WallDimensionDemo(commandData);

    //        ////CreateBeamSystem有界面，基本完整，似乎不实用，有的底层GeometryUtil	几何工具（线条排序、共面检查）可参考，预览窗口实现逻辑
    //        ////Revit 梁系统创建插件，主要功能：
    //        ////从选中的梁构建闭合轮廓 - 用户选择首尾相连的梁，自动排序形成闭合多边形轮廓
    //        ////可视化预览 - 在窗口中显示轮廓的2D示意图，支持方向切换
    //        ////设置梁系统参数 - 通过属性网格配置布局规则、梁类型等
    //        ////创建梁系统 - 根据设置的参数自动生成Revit梁系统
    //        //try
    //        //{
    //        //    var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "创建梁系统");
    //        //    var window = new RevitBeamSystemCreatorView(commandData);
    //        //    var result = window.ShowDialog();
    //        //    return result == true ? Result.Succeeded : Result.Cancelled;
    //        //}
    //        //catch (System.Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}            

    //        //AirHandlerCreator无界面，待重构看是否有需求。有几何和实体族建立逻辑流程可参考
    //        //Revit 空气处理单元(AHU)族创建插件，主要功能：
    //        //创建自定义机械族 - 在族编辑器中创建空气处理单元
    //        //构建复杂几何体 - 通过5个拉伸体组合形成设备外形（3个矩形拉伸 + 2个圆形拉伸）
    //        //添加连接件 - 创建4个系统连接器：
    //        //送风管连接器(Supply Air)
    //        //回风管连接器(Return Air)
    //        //供水管连接器(Supply Hydronic)
    //        //回水管连接器(Return Hydronic)
    //        //参数设置 - 设置连接器尺寸、流量、流向等参数
    //        //合并几何体 - 将所有拉伸体合并为单个实体
    //        //AirHandlerCreator airHandlerCreator = new AirHandlerCreator(commandData); 

    //        ////CompoundStructure无界面，待重构看是否有需求
    //        ////Revit 墙体复合结构创建插件，主要功能：
    //        ////为选中的墙体应用复合结构 - 创建多层墙体构造（饰面层、基层、结构层、膜层等）
    //        ////创建自定义材质 - 砖和混凝土材质，包含结构和热工属性
    //        ////分割墙体区域 - 在墙体中创建新的区域分区
    //        ////添加墙饰条和墙嵌条 - 在指定位置添加扫掠和凹槽
    //        ////设置复合结构参数 - 结构层索引、包络层、包裹参与等
    //        //WallCompoundStructureCommand wallCompound = new WallCompoundStructureCommand(commandData);

    //        //ChangesMonitor变更追踪器 - Revit元素变更监控 没啥使用可能，跳过

    //        //FormatStatusExtensions 待测试
    //        //Revit 文本注释格式批量转换插件，主要功能：
    //        //查找所有TextNote元素 - 遍历当前文档中的所有文本注释
    //        //检测大写格式状态 - 使用GetAllCapsStatus()判断是否已全大写
    //        //批量应用全大写格式 - 对未全大写的文本注释应用AllCaps格式
    //        //支持部分格式检测 - 可检测单个字符或字符范围的格式状态

    //        //////0428 官方代码测试
    //        //CancelSave逻辑已基本实现，但内部有个LogManager可以看一下思路与自己完成的是否一致。ds改写后变动很大。

    //        //Revit 结构边界条件编辑器插件，没啥使用可能，跳过
    //        //选择结构元素（柱、梁、墙、楼板等）
    //        //查看 / 编辑边界条件属性（固定、铰支、滚动支座、自定义）
    //        //支持三种边界条件类型：点、线、面
    //        //设置弹簧刚度（用户自定义状态）

    //        //BRepBuilder没啥使用可能，跳过

    //        //ParameterBindingBrowserWindow基本可用，难得
    //        ////Revit插件，用于浏览和查看文档中的参数绑定关系：
    //        ////获取参数绑定 - 从文档的ParameterBindings中获取所有参数绑定
    //        ////树形展示 - 以树形结构显示参数名称及其绑定的类别
    //        ////参数分类 - 展示每个参数绑定到哪些构件类别（如梁、楼板等）
    //        //try
    //        //{
    //        //    // 创建视图模型
    //        //    var viewModel = new ParameterBindingBrowserViewModel(commandData);
    //        //    // 创建并显示窗口
    //        //    var window = new ParameterBindingBrowserWindow
    //        //    {
    //        //        DataContext = viewModel
    //        //    };
    //        //    // 设置关闭回调
    //        //    viewModel.CloseAction = window.Close;
    //        //    // 显示窗口（非模态）
    //        //    window.Show();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //UniqueIdManagerWindow插件是否必要？
    //        ////翻改逻辑有问题，builtInParameter需单独处理
    //        ////Revit插件，用于为梁和楼板添加共享参数并管理唯一标识：
    //        ////添加共享参数 - 创建"Unique ID"共享参数，绑定到梁和楼板类别
    //        ////自动赋值 - 为每个构件生成GUID并写入参数
    //        ////显示参数值 - 在列表中显示选中构件的唯一ID
    //        ////查找定位 - 根据唯一ID在模型中定位构件
    //        //using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "唯一ID管理"))
    //        //{
    //        //    transaction.Start();

    //        //    var viewModel = new UniqueIdManagerViewModel(commandData);
    //        //    var window = new UniqueIdManagerWindow { DataContext = viewModel };
    //        //    viewModel.CloseAction = window.Close;
    //        //    window.ShowDialog();
    //        //    transaction.Commit();
    //        //    return Result.Succeeded;
    //        //}

    //        //PipeCollisionResolver默认为自动执行，需要调整
    //        ////需要看一下早期叶的几何计算处理相关视频,需要学习实现使用的方法
    //        ////Revit插件，用于自动解决管道与结构构件（梁、风管等）的碰撞问题：
    //        ////碰撞检测 - 使用ReferenceIntersector检测管道与周围构件的交叉
    //        ////过滤碰撞对象 - 只关注管道、风管、结构框架
    //        ////分段处理 - 将碰撞区域分段，生成U形绕行路径
    //        ////自动绕行 - 创建偏移管道和弯头绕过障碍物
    //        //using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "解决管道碰撞"))
    //        //{
    //        //    try
    //        //    {
    //        //        transaction.Start();
    //        //        var resolver = new PipeCollisionResolver(commandData);
    //        //        var resolvedCount = resolver.ResolveAllPipes();
    //        //        transaction.Commit();
    //        //        TaskDialog.Show("完成", $"共处理了 {resolvedCount} 条管道的碰撞问题");
    //        //        return Result.Succeeded;
    //        //    }
    //        //    catch (Exception ex)
    //        //    {
    //        //        transaction.RollBack();
    //        //        message = ex.Message;
    //        //        return Result.Failed;
    //        //    }
    //        //}

    //        //AutoUpdate功能已实现在记录工作时间
    //        //Revit外部应用程序，用于监控文档打开事件并自动修改项目信息：
    //        //事件注册 - 注册DocumentOpened事件
    //        //日志记录 - 记录事件参数和操作结果
    //        //自动修改 - 当文档打开时自动修改项目地址信息
    //        //异常处理 - 跳过族文档，处理修改失败的情况

    //        //AutoTagRoomsWindow可以借鉴RoomEntity的思路，加标记相比系统级别自动没想出太多优点。
    //        ////Revit插件，用于自动为房间添加标记：
    //        ////选择楼层 - 选择需要添加房间标记的楼层
    //        ////选择标记类型 - 选择使用的房间标记类型
    //        ////显示房间列表 - 展示该楼层所有房间及已有标记数量
    //        ////自动标记 - 为所有未标记房间自动添加标记
    //        //var viewModel = new AutoTagRoomsViewModel(commandData);
    //        //var window = new AutoTagRoomsWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();

    //        //PrintMonitorWindow 比较复杂，没啥实用性，只实现了界面
    //        ////Revit外部应用程序，用于监控视图打印事件并在打印时添加水印：
    //        ////事件注册 - 注册ViewPrinting和ViewPrinted事件
    //        ////打印前处理 - 在视图上创建带打印信息的文字注释
    //        ////打印后清理 - 删除添加的文字注释
    //        ////日志记录 - 记录事件参数和打印信息到日志文件
    //        //// 创建监控窗口（可选，显示在Revit中）
    //        //var viewModel = new PrintMonitorViewModel();
    //        //var window = new PrintMonitorWindow { DataContext = viewModel };
    //        //window.ShowDialog();
    //        //return Result.Succeeded;

    //        //DuctSystemWindow有点意思的功能，可以考虑细化按区域布置，风管尺寸变径、分叉如何考虑，要改为非模态调用
    //        ////Revit插件，用于自动创建空调风管系统：
    //        ////连接设备 - 将1个送风设备和2个末端设备用风管连接
    //        ////智能路径规划 - 自动计算最优的风管布置路径
    //        ////创建管件 - 自动添加弯头、三通等管件
    //        ////系统日志 - 记录创建的风管系统详细信息
    //        //var viewModel = new DuctSystemViewModel(commandData);
    //        //var window = new DuctSystemWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();

    //        //AddParameterWindow 待测试
    //        //Revit插件，用于批量向族文件添加参数：
    //        ////单文件模式 - 向当前打开的族文档添加参数
    //        ////批量模式 - 遍历文件夹中的所有族文件，批量添加参数
    //        ////参数来源 - 从文本文件读取参数定义（支持族参数和共享参数）
    //        ////参数格式 - 名称、分组、类型、实例 / 类型参数
    //        //var viewModel = new AddParameterViewModel(commandData);
    //        //var window = new AddParameterWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();
    //        ////// 批量模式入口（可复用同一个ViewModel，通过参数区分）
    //        ////[Transaction(TransactionMode.Manual)]
    //        ////    public class AddParameterToFamilies : IExternalCommand
    //        ////{
    //        ////    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //        ////    {
    //        ////        var viewModel = new AddParameterViewModel(commandData);
    //        ////        viewModel.IsBatchMode = true;
    //        ////        var window = new AddParameterWindow { DataContext = viewModel };
    //        ////        viewModel.CloseAction = window.Close;
    //        ////        window.ShowDialog();
    //        ////        return Result.Succeeded;
    //        ////    }
    //        ////}

    //        //AutoJoinWindow界面和代码完整，但似乎没有实际执行，选择后未识别到符合条件的构件
    //        ////这是一个Revit插件，用于自动连接重叠的几何形体（如体量、通用模型）：
    //        ////检测重叠 - 通过几何相交测试判断两个实体是否重叠
    //        ////查找重叠组 - 递归查找所有相互重叠的构件集合
    //        ////合并几何 - 使用CombineElements将重叠的几何合并为一个整体
    //        ////两种模式 - 支持选中构件合并或全文档自动合并
    //        //var viewModel = new AutoJoinViewModel(commandData);
    //        //var window = new AutoJoinWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();

    //        //AreaReinforcementParameterWindow WPF xaml语法存在问题
    //        ///Revit插件，用于编辑面积配筋(AreaReinforcement)的参数：
    //        //支持两种构件类型 - 墙体面积配筋(Wall)和楼板面积配筋(Floor)
    //        //参数分类管理 - 按图层分类（外部 / 内部、顶部 / 底部、主向 / 次向）
    //        //动态数据源 - 从当前项目获取钢筋类型和弯钩类型列表
    //        //属性网格编辑 - 使用PropertyGrid控件进行参数编辑
    //        //var selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
    //        //// 验证是否选中构件
    //        //if (selectedIds.Count != 1)
    //        //{
    //        //    message = "请只选择一个面积配筋构件";
    //        //    return Result.Failed;
    //        //}
    //        //var element = doc.GetElement(selectedIds.First());
    //        //var areaRein = element as AreaReinforcement;
    //        //if (areaRein == null)
    //        //{
    //        //    message = "请选择一个面积配筋构件";
    //        //    return Result.Failed;
    //        //}
    //        //var service = new AreaReinforcementParameterService(commandData);
    //        //// 验证项目中是否有钢筋类型和弯钩类型
    //        //if (!service.HasRequiredTypes)
    //        //{
    //        //    message = "当前项目中缺少钢筋类型或弯钩类型定义";
    //        //    return Result.Failed;
    //        //}
    //        //var viewModel = new AreaReinforcementParameterViewModel(commandData, areaRein);
    //        //var window = new AreaReinforcementParameterWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();

    //        //AreaReinforcementWindow
    //        ////Revit插件，用于处理面积配筋(AreaReinforcement)的显示和弯钩设置：
    //        ////验证选择 - 检查用户是否只选择了一个矩形面积配筋
    //        ////关闭图层 - 关闭除主方向层以外的所有钢筋层
    //        ////移除弯钩 - 移除主要方向边界曲线上的弯钩
    //        ////显示结果 - 通过对话框告知用户操作结果
    //        //var selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds().ToList();
    //        //// 验证是否选中了构件
    //        //if (!selectedIds.Any())
    //        //{
    //        //    TaskDialog.Show("提示", "请至少选择一个面积配筋");
    //        //    return Result.Cancelled;
    //        //}
    //        //// 创建视图模型
    //        //var viewModel = new AreaReinforcementViewModel(commandData, selectedIds);
    //        //var window = new AreaReinforcementWindow { DataContext = viewModel };
    //        //viewModel.CloseAction = window.Close;
    //        //window.ShowDialog();
    //        //// 显示操作结果
    //        //if (viewModel.OperationSucceeded)
    //        //{
    //        //    TaskDialog.Show("成功", viewModel.ResultMessage);
    //        //    return Result.Succeeded;
    //        //}
    //        //message = viewModel.ErrorMessage;

    //        //ExportToExcelWindow 载入构件存在崩溃问题，未涉及到导出环境
    //        ////VB转Revit插件，用于将项目中所有构件按类别分组导出到Excel：
    //        ////收集所有非类型构件 - 过滤掉ElementType，只保留实例构件
    //        ////按Category分组 - 将相同类别的构件归类
    //        ////提取公共属性 - 找出同类构件共有的参数
    //        ////导出到Excel - 每个类别创建一个工作表，列出构件ID和所有公共参数值
    //        //try
    //        //{
    //        //    // 创建视图模型并传入当前文档
    //        //    var viewModel = new ExportToExcelViewModel(commandData, doc);
    //        //    var window = new ExportToExcelWindow { DataContext = viewModel };
    //        //    // 设置关闭回调
    //        //    viewModel.CloseAction = window.Close;
    //        //    window.ShowDialog();
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}

    //        //AnalyticalSupportWindow
    //        ////Revit插件，用于显示选中构件的分析模型支撑信息：
    //        ////获取选中构件 - 从Revit当前选择集中获取所有构件
    //        ////提取分析模型 - 获取每个构件的AnalyticalModel
    //        ////判断支撑状态 - 通过IsElementFullySupported()判断是否完全支撑
    //        ////获取支撑类型 - 调用GetAnalyticalModelSupports()获取支撑详细信息
    //        ////分类显示 - 在表格中显示构件ID、类型名称、支撑类型、备注说明
    //        //// 获取当前选中的构件ID集合
    //        //List<ElementId> selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds().ToList();
    //        //// 如果没有选中任何构件，提示用户
    //        //if (!selectedIds.Any())
    //        //{
    //        //    TaskDialog.Show("提示", "请至少选择一个构件");
    //        //    return Result.Cancelled;
    //        //}
    //        //// 创建视图模型并传入选中的构件数据
    //        //var viewModel = new AnalyticalSupportViewModel(commandData, selectedIds);
    //        //var window = new AnalyticalSupportWindow { DataContext = viewModel };
    //        //window.ShowDialog();

    //        ////0427 官方代码测试
    //        //ZoneEditorMainWindow 待测试
    //        ////Revit插件，用于管理空间(Space)和区域(Zone)，主要功能包括：
    //        ////切换楼层(Level) - 不同楼层显示不同的空间和区域
    //        ////创建空间(Create Spaces) - 在当前楼层自动创建所有封闭区域的空间
    //        ////创建区域(Create Zone) - 在当前楼层创建新区域
    //        ////编辑区域(Edit Zone) - 将空间添加到区域或从区域移除
    //        //using (var trans = new Transaction(doc, "AddSpaceAndZone"))
    //        //{
    //        //    trans.Start();
    //        //    var viewModel = new ZoneEditorMainViewModel(commandData);
    //        //    var window = new ZoneEditorMainWindow { DataContext = viewModel };
    //        //    var result = window.ShowDialog() == true;
    //        //    if (result) trans.Commit();
    //        //    else trans.RollBack();
    //        //    return result ? Result.Succeeded : Result.Cancelled;
    //        //}

    //        //AllViewsWindow 待测试
    //        ////读取视图放到图纸上
    //        //var viewModel = new MainViewModel(doc);
    //        //var window = new AllViewsWindow { DataContext = viewModel };
    //        //return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;

    //        return Result.Succeeded;
    //    }

    //}
}
