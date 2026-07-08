using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //public class Test12_0704 : IExternalCommand
    //{
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        ////////1207 风口清理和连接
    //        //try
    //        //{
    //        //    // 1. 选择风口
    //        //    using (Transaction trans = new Transaction(doc, "修改风管系统"))
    //        //    {
    //        //        trans.Start();
    //        //        //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new AirTerminalSelectionFilter(), "请选择一个风口");
    //        //        //Element terminal = doc.GetElement(reference);
    //        //        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
    //        //        if (selectedIds == null || selectedIds.Count == 0)
    //        //        {
    //        //            TaskDialog.Show("错误", "未选择任意");
    //        //            return Result.Failed;
    //        //        }
    //        //        List<Element> ductTerminals = new List<Element>();
    //        //        foreach (var id in selectedIds)
    //        //        {
    //        //            Element element = doc.GetElement(id);
    //        //            if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal)
    //        //            {
    //        //                ductTerminals.Add(element);
    //        //            }
    //        //        }
    //        //        if (ductTerminals == null)
    //        //        {
    //        //            TaskDialog.Show("错误", "未选择风口");
    //        //            return Result.Failed;
    //        //        }
    //        //        foreach (var item in ductTerminals)
    //        //        {
    //        //            // 2. 获取风口的所有连接器
    //        //            List<Connector> connectors = GetConnectors(item);
    //        //            if (connectors.Count == 0)
    //        //            {
    //        //                TaskDialog.Show("提示", "该风口没有连接器");
    //        //                return Result.Failed;
    //        //            }
    //        //            // 3. 获取所有相连的管件和风管
    //        //            List<ElementId> connectedElements = GetAllConnectedElements(connectors, doc);
    //        //            // 4. 删除所有相连的管件和风管
    //        //            DeleteConnectedElements(doc, connectedElements);
    //        //            // 5. 设置风口高度
    //        //            SetTerminalHeight(item, 3000);
    //        //        }
    //        //        trans.Commit();
    //        //        //TaskDialog.Show("完成",$"已删除 {connectedElements.Count} 个相连元素，并将风口高度设置为4000mm");
    //        //    }
    //        //    return Result.Succeeded;
    //        //}
    //        //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //        //{
    //        //    return Result.Cancelled;
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    message = ex.Message;
    //        //    return Result.Failed;
    //        //}      

    //        ////0205 查找特定属性风口构建
    //        //List<FamilyInstance> allInstance = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
    //        //List<FamilyInstance> terminalNames = new List<FamilyInstance>();
    //        //foreach (var item in allInstance)
    //        //{
    //        //    if ((item.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal) && (item.Name == "风道末端_单层百叶风口"))
    //        //    {
    //        //        terminalNames.Add(item);
    //        //    }
    //        //}
    //        //List<ElementId> selectedElementIds = new List<ElementId>();
    //        ////foreach (var item in terminalNames)
    //        ////{
    //        ////    try
    //        ////    {
    //        ////        Parameter widthParameter = item.LookupParameter("风口宽度");
    //        ////        Parameter heightParameter = item.LookupParameter("风口高度");
    //        ////        //if (widthParameter != null && widthParameter.AsDouble() == 600 / 304.8 && heightParameter != null && heightParameter.AsDouble() == 500 / 304.8)
    //        ////        if (widthParameter != null && widthParameter.AsDouble() == 1000 / 304.8)
    //        ////        //if (heightParameter != null && heightParameter.AsDouble() == 600 / 304.8)
    //        ////        {
    //        ////            selectedElementIds.Add(item.Id);
    //        ////        }
    //        ////    }
    //        ////    catch (Exception)
    //        ////    {
    //        ////        throw;
    //        ////    }
    //        ////}
    //        ////TaskDialog.Show("tt", selectedElementIds.Count().ToString());
    //        //StringBuilder stringBuilder = new StringBuilder();
    //        //foreach (var item in terminalNames)
    //        //{
    //        //    selectedElementIds.Add(item.Id);
    //        //    stringBuilder.Append(item.Id.ToString() + ",");
    //        //}
    //        //TaskDialog.Show("tt", stringBuilder.ToString());
    //        ////uiDoc.Selection.SetElementIds(selectedElementIds);

    //        //////1029 管道属性批量填写,系统族批量可参考.OK
    //        //using (Transaction tx = new Transaction(doc, "管道属性批写入"))
    //        //{
    //        //    tx.Start();
    //        //    try
    //        //    {
    //        //        List<Pipe> allPipesInModel = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
    //        //        foreach (var pipe in allPipesInModel)
    //        //        {
    //        //            //TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
    //        //            double diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;
    //        //            double length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() * 304.8;
    //        //            // 参数配置字典
    //        //            var parameterConfigs = new Dictionary<string, string>
    //        //            {
    //        //                { "尺寸规格", $"DN{(int)diameter}" },
    //        //                { "直径", $"DN{(int)diameter}" },
    //        //                { "材质1", "钢管" },
    //        //                { "压力等级", "1.6MPa" },
    //        //                { "长度", $"{(int)length}mm" },
    //        //                { "系统类型", "喷淋" },
    //        //                { "坡度", "0" },
    //        //                { "保温材料", "柔性泡沫橡塑管壳" },
    //        //                { "保温厚度", "55mm" }
    //        //            };
    //        //            foreach (var config in parameterConfigs)
    //        //            {
    //        //                Parameter param = pipe.LookupParameter(config.Key);
    //        //                param?.Set(config.Value);
    //        //            }
    //        //            //简化前代码
    //        //            //Parameter parameter1 = item.LookupParameter("尺寸规格");
    //        //            //if (parameter1 != null)
    //        //            //{
    //        //            //    parameter1.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
    //        //            //}
    //        //            //Parameter parameter2 = item.LookupParameter("直径");
    //        //            //if (parameter2 != null)
    //        //            //{
    //        //            //    parameter2.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
    //        //            //}
    //        //        }
    //        //        //////属性测试
    //        //        ////Pipe item = doc.GetElement(uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterPipe()).ElementId) as Pipe;
    //        //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
    //        //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8).ToString("F0"));
    //        //        ////TaskDialog.Show("tt", ((int)((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8)).ToString());
    //        //        //TaskDialog.Show("tt", item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString());
    //        //    }
    //        //    catch (Exception)
    //        //    {
    //        //        throw;
    //        //    }
    //        //    tx.Commit();
    //        //}
    //        ////例程结束
    //        return Result.Succeeded;
    //    }
    //}

    //[Transaction(TransactionMode.Manual)]//翻接机电管线 20260628
    //public class MEPCurveTurnOver : IExternalCommand
    //{
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        ////0628 选择翻弯类型
    //        TaskDialog td = new TaskDialog("选择翻弯类型")
    //        {
    //            MainInstruction = "请选择单侧翻弯（ 第二点变高）还是双侧翻弯（第二点继续原高度）:",
    //            CommonButtons = TaskDialogCommonButtons.Cancel
    //        };
    //        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "单侧翻弯（变高），仅单次");
    //        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "双侧连续翻弯（继续原高度）");
    //        TaskDialogResult tdRes = td.Show();
    //        if (tdRes == TaskDialogResult.Cancel) return Result.Cancelled;
    //        bool useSingle = (tdRes == TaskDialogResult.CommandLink1);
    //        //// 翻弯方法
    //        var selectionList = new Dictionary<string, List<string>>
    //        {
    //            {  "向上翻", new List<string>{ "300","400","500","600","800","100","150","200","250"}},
    //            {  "向下翻", new List<string>{ "400","300","500","600","800","100","150","200","250"}}
    //        };
    //        UniversalDoubleComboboxWindow dialog = new UniversalDoubleComboboxWindow("设置翻弯参数", "1. 请选择翻弯方向:", "2. 请选择翻弯高度:", selectionList);
    //        if (dialog.ShowDialog() != true) return Result.Cancelled;
    //        bool upWard = dialog.SelectedItem1?.ToString() == "向上翻";
    //        double distanceMM = double.Parse(dialog.SelectedItem2?.ToString() ?? "0");
    //        // 翻弯高度转内部单位
    //        double height = distanceMM / 304.8;
    //        if (useSingle)
    //        {
    //            //// 以下执行单侧翻弯，不循环
    //            _externalHandler.Run(app =>
    //            {
    //                uiDoc = app.ActiveUIDocument;
    //                doc = uiDoc.Document;
    //                //while (true) // 1. 创建无限循环
    //                //{
    //                try
    //                {
    //                    Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass(), "请选择第一个打断点 (按ESC退出)");
    //                    Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass(), "请选择第一个打断点 (按ESC退出)");
    //                    // 如果点的不是同一根管或空值则不翻弯
    //                    if (ref1 == null || ref2 == null) return;
    //                    if (ref1.ElementId != ref2.ElementId)
    //                    {
    //                        TaskDialog.Show("提示", "两个拾取点必须在同一根管道/桥架上，请重新拾取。");
    //                        return;
    //                    }
    //                    // 取管1及打断点 
    //                    MEPCurve mEPCurve = doc.GetElement(ref1.ElementId) as MEPCurve;
    //                    if (mEPCurve == null || !mEPCurve.IsHorizontal()) return;
    //                    // 投影到中心线 
    //                    Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //                    XYZ xyz1 = curve.Project(ref1.GlobalPoint).XYZPoint;
    //                    XYZ xyz2 = curve.Project(ref2.GlobalPoint).XYZPoint;
    //                    //// 确保 xyz1 在 xyz2 之前（沿管方向） 
    //                    //// 用参数排序，保证 xyz1 是更靠近管起点的那个
    //                    double param1 = curve.Project(xyz1).Parameter;
    //                    double param2 = curve.Project(xyz2).Parameter;
    //                    if (param1 > param2)
    //                    {
    //                        XYZ tmp = xyz1; xyz1 = xyz2; xyz2 = tmp;
    //                    }
    //                    // 计算两点间水平距离（用于校验和判断翻弯角度） 
    //                    double span = xyz1.DistanceTo(xyz2);
    //                    // 取主要尺寸（直径或宽度）用于最小距离校验 
    //                    double mainSizeFt = mEPCurve.GetMEPCurveMainSize();
    //                    // 最小翻弯净距：翻弯高度至少要大于管径，且两点间距至少要能放下两段立管+弯头
    //                    // 视翻弯难度可改为2 / 1
    //                    // 经验值：span >= 3 * mainSizeFt，height >= 1.5 x mainSizeFt
    //                    double minSpan = 3.0 * mainSizeFt;
    //                    double minHeight = 1.5 * mainSizeFt;
    //                    if (span < minSpan)
    //                    {
    //                        TaskDialog.Show("校验失败",
    //                            $"两拾取点间距 {span * 304.8:F0}mm 过小（最小 {minSpan * 304.8:F0}mm），无法翻弯。");
    //                        return;
    //                    }
    //                    if (height < minHeight)
    //                    {
    //                        TaskDialog.Show("校验失败",
    //                            $"翻弯高度 {distanceMM:F0}mm 小于管道主尺寸 {mainSizeFt * 304.8:F0}mm，无法翻弯。");
    //                        return;
    //                    }
    //                    // ---- 判断翻弯角度 ----
    //                    // 45度条件：height <= span/2（几何上能构成45度斜边）
    //                    // 90度条件：height > span/2，直接垂直上去再水平过去
    //                    bool use90 = (height > span / 2.0);
    //                    // ---- 方向向量 ----
    //                    XYZ vertDir = upWard ? XYZ.BasisZ : XYZ.BasisZ.Negate();
    //                    XYZ horzDir = (xyz2 - xyz1).Normalize(); // 水平方向（管轴方向）
    //                    XYZ vEnd1;
    //                    if (use90)
    //                    {
    //                        // 90度：立管垂直上升 height，顶部水平连接
    //                        vEnd1 = xyz1 + vertDir * height;
    //                    }
    //                    else
    //                    {
    //                        // 此处还应该复核xyz1,xyz2各自到mepcurve两端点距离要大于1.5 x mainSizeFt
    //                        double minEdgeDistance = 1.5 * mainSizeFt;
    //                        // 获取曲线端点
    //                        XYZ startPoint = curve.GetEndPoint(0);
    //                        XYZ endPoint = curve.GetEndPoint(1);
    //                        double dist1ToStart = xyz1.DistanceTo(startPoint);
    //                        double dist1ToEnd = xyz1.DistanceTo(endPoint);
    //                        double minDist1 = Math.Min(dist1ToStart, dist1ToEnd);
    //                        if (minDist1 < minEdgeDistance)
    //                        {
    //                            TaskDialog.Show("校验失败", $"第一个打断点距离管端太近（{minDist1.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
    //                            return;
    //                        }
    //                        // 45度：斜管以45度角上升，水平偏移 = 竖直高度
    //                        // 斜管水平分量 = height（45度时水平=垂直）
    //                        // 为了保证几何可行，水平分量不能超过 span/2
    //                        double hOffset = height; // 45度时水平分量等于竖直分量
    //                        vEnd1 = xyz1 + vertDir * height + horzDir * hOffset;
    //                        // 注意：vEnd2 是从 xyz2 向反方向退 hOffset
    //                    }
    //                    // ==================== 计算2个关键点 ====================
    //                    //
    //                    //  90度翻弯示意（向上）:
    //                    //
    //                    //          vEnd1 ----mepNeo 
    //                    //            |                  
    //                    //          vPipe1             
    //                    //            |                  
    //                    //  ---mep---xyz1              
    //                    //
    //                    //  45度翻弯示意（向上）:
    //                    //
    //                    //               vMid1 ----mepNeo---- 
    //                    //              /                         
    //                    //         45Pipe1                       
    //                    //            /                             
    //                    //  ---mep--xyz1                           
    //                    //
    //                    // ======================================================
    //                    using (var trans = new Transaction(doc, "两点翻弯"))
    //                    {
    //                        try
    //                        {
    //                            trans.Start();
    //                            // 1. 在 xyz1 处打断原管
    //                            MEPCurve mepSeg1 = mEPCurve.BreakMEPCurveByOne(xyz1);
    //                            // 单翻时，修改逻辑
    //                            // 移动mepSeg1到指定位置高度
    //                            ElementTransformUtils.MoveElement(doc, mepSeg1.Id, vertDir * height);
    //                            //// 4. 根据管类型创建翻弯管段
    //                            if (mEPCurve is Pipe pipe)
    //                            {
    //                                var pipe2 = mepSeg1 as Pipe;
    //                                CreatePipeBendConnector(doc, pipe, pipe2, xyz1, vEnd1);
    //                            }
    //                            else if (mEPCurve is Duct duct)
    //                            {
    //                                var duct2 = mepSeg1 as Duct;
    //                                CreateDuctBendConnector(doc, duct, duct2, xyz1, vEnd1);
    //                            }
    //                            else if (mEPCurve is CableTray cableTray)
    //                            {
    //                                var cableTray2 = mepSeg1 as CableTray;
    //                                CreateCableTrayBendConnector(doc, cableTray, cableTray2, xyz1, vEnd1);
    //                            }
    //                            else return;
    //                            trans.Commit();
    //                        }
    //                        catch (Exception exInner)
    //                        {
    //                            trans.RollBack();
    //                            TaskDialog.Show("翻弯事务错误", $"操作失败：{exInner.Message}\n{exInner.StackTrace}");
    //                        }
    //                    }
    //                }
    //                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //                {
    //                    // 用户按了 ESC 键取消操作
    //                    return;
    //                }
    //                catch (Exception ex)
    //                {
    //                    TaskDialog.Show("翻弯内部事务发生错误", $"操作失败：{ex.Message}");
    //                    return;
    //                }
    //                //}
    //            });
    //        }
    //        else
    //        {
    //            // 开始以下循环执行双侧翻弯
    //            _externalHandler.Run(app =>
    //            {
    //                uiDoc = app.ActiveUIDocument;
    //                doc = uiDoc.Document;
    //                //while (true) // 1. 创建无限循环
    //                //{
    //                try
    //                {
    //                    Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(), "请选择第一个打断点 (按ESC退出)");
    //                    Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new MEPCurveFilter(), "请选择第一个打断点 (按ESC退出)");
    //                    // 如果点的不是同一根管或空值则不翻弯
    //                    if (ref1 == null || ref2 == null) return;
    //                    if (ref1.ElementId != ref2.ElementId)
    //                    {
    //                        TaskDialog.Show("提示", "两个拾取点必须在同一根管道/桥架上，请重新拾取。");
    //                        return;
    //                        //continue;
    //                    }
    //                    // 取管1及打断点 
    //                    MEPCurve mEPCurve = doc.GetElement(ref1.ElementId) as MEPCurve;
    //                    if (mEPCurve == null || !mEPCurve.IsHorizontal()) return;
    //                    // 投影到中心线 
    //                    Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //                    XYZ xyz1 = curve.Project(ref1.GlobalPoint).XYZPoint;
    //                    XYZ xyz2 = curve.Project(ref2.GlobalPoint).XYZPoint;
    //                    //// 确保 xyz1 在 xyz2 之前（沿管方向） 
    //                    //// 用参数排序，保证 xyz1 是更靠近管起点的那个
    //                    double param1 = curve.Project(xyz1).Parameter;
    //                    double param2 = curve.Project(xyz2).Parameter;
    //                    if (param1 > param2)
    //                    {
    //                        XYZ tmp = xyz1; xyz1 = xyz2; xyz2 = tmp;
    //                    }
    //                    // 计算两点间水平距离（用于校验和判断翻弯角度） 
    //                    double span = xyz1.DistanceTo(xyz2);
    //                    // 取主要尺寸（直径或宽度）用于最小距离校验 
    //                    double mainSizeFt = mEPCurve.GetMEPCurveMainSize();
    //                    // 最小翻弯净距：翻弯高度至少要大于管径，且两点间距至少要能放下两段立管+弯头
    //                    // 视翻弯难度可改为2 / 1
    //                    // 经验值：span >= 3 * mainSizeFt，height >= 1.5 x mainSizeFt
    //                    double minSpan = 3.0 * mainSizeFt;
    //                    double minHeight = 1.5 * mainSizeFt;
    //                    if (span < minSpan)
    //                    {
    //                        TaskDialog.Show("校验失败",
    //                            $"两拾取点间距 {span * 304.8:F0}mm 过小（最小 {minSpan * 304.8:F0}mm），无法翻弯。");
    //                        return;
    //                        //continue;
    //                    }
    //                    if (height < minHeight)
    //                    {
    //                        TaskDialog.Show("校验失败",
    //                            $"翻弯高度 {distanceMM:F0}mm 小于管道主尺寸 {mainSizeFt * 304.8:F0}mm，无法翻弯。");
    //                        return;
    //                        //continue;
    //                    }
    //                    // ---- 判断翻弯角度 ----
    //                    // 45度条件：height <= span/2（几何上能构成45度斜边）
    //                    // 90度条件：height > span/2，直接垂直上去再水平过去
    //                    bool use90 = (height > span / 2.0);
    //                    // ---- 方向向量 ----
    //                    XYZ vertDir = upWard ? XYZ.BasisZ : XYZ.BasisZ.Negate();
    //                    XYZ horzDir = (xyz2 - xyz1).Normalize(); // 水平方向（管轴方向）
    //                    XYZ vEnd1, vEnd2;
    //                    if (use90)
    //                    {
    //                        // 90度：立管垂直上升 height，顶部水平连接
    //                        vEnd1 = xyz1 + vertDir * height;
    //                        vEnd2 = xyz2 + vertDir * height;
    //                    }
    //                    else
    //                    {
    //                        // 此处还应该复核xyz1,xyz2各自到mepcurve两端点距离要大于1.5 x mainSizeFt
    //                        double minEdgeDistance = 1.5 * mainSizeFt;
    //                        // 获取曲线端点
    //                        XYZ startPoint = curve.GetEndPoint(0);
    //                        XYZ endPoint = curve.GetEndPoint(1);
    //                        double dist1ToStart = xyz1.DistanceTo(startPoint);
    //                        double dist1ToEnd = xyz1.DistanceTo(endPoint);
    //                        double minDist1 = Math.Min(dist1ToStart, dist1ToEnd);
    //                        if (minDist1 < minEdgeDistance)
    //                        {
    //                            TaskDialog.Show("校验失败", $"第一个打断点距离管端太近（{minDist1.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
    //                            return;
    //                            //continue;
    //                        }
    //                        // === 新增：检查 xyz2 到两端点的距离 ===
    //                        double dist2ToStart = xyz2.DistanceTo(startPoint);
    //                        double dist2ToEnd = xyz2.DistanceTo(endPoint);
    //                        double minDist2 = Math.Min(dist2ToStart, dist2ToEnd);
    //                        if (minDist2 < minEdgeDistance)
    //                        {
    //                            TaskDialog.Show("校验失败", $"第二个打断点距离管端太近（{minDist2.ToString("F3")} < {minEdgeDistance.ToString("F3")}），无法生成翻弯。");
    //                            return;
    //                            //continue;
    //                        }
    //                        // 45度：斜管以45度角上升，水平偏移 = 竖直高度
    //                        // 斜管水平分量 = height（45度时水平=垂直）
    //                        // 为了保证几何可行，水平分量不能超过 span/2
    //                        double hOffset = height; // 45度时水平分量等于竖直分量
    //                        vEnd1 = xyz1 + vertDir * height + horzDir * hOffset;
    //                        vEnd2 = xyz2 + vertDir * height - horzDir * hOffset;
    //                        // 注意：vEnd2 是从 xyz2 向反方向退 hOffset
    //                    }
    //                    // ==================== 计算4个关键点 ====================
    //                    //
    //                    //  90度翻弯示意（向上）:
    //                    //
    //                    //          vEnd1 ----hPipe---- vEnd2
    //                    //            |                  |
    //                    //          vPipe1             vPipe2
    //                    //            |                  |
    //                    //  ---mep---xyz1              xyz2---mepNeo---
    //                    //
    //                    //  45度翻弯示意（向上）:
    //                    //
    //                    //         vMid1 ----hPipe---- vMid2
    //                    //        /                        \
    //                    //   45Pipe1                      45Pipe2
    //                    //      /                              \
    //                    //  ---xyz1                           xyz2---
    //                    //
    //                    // ======================================================
    //                    using (var trans = new Transaction(doc, "两点翻弯"))
    //                    {
    //                        try
    //                        {
    //                            trans.Start();
    //                            // 1. 在 xyz1 处打断原管
    //                            MEPCurve mepSeg1 = mEPCurve.BreakMEPCurveByOne(xyz1);
    //                            // 打断后：mEPCurve = [原起点 → xyz1]，mepSeg1 = [xyz1 → 原终点]
    //                            // 2. 在 xyz2 处打断 mepSeg1
    //                            MEPCurve mepSeg2 = mepSeg1.BreakMEPCurveByOne(xyz2);
    //                            // 打断后：mepSeg1 = [xyz1 → xyz2]（中间段，将被翻弯替代）
    //                            //         mepSeg2 = [xyz2 → 原终点]
    //                            // 3. 删除中间段
    //                            doc.Delete(mepSeg1.Id);
    //                            //以上为打断并删除中段代码
    //                            //// 4. 根据管类型创建翻弯管段
    //                            if (mEPCurve is Pipe pipe)
    //                            {
    //                                var pipe2 = mepSeg2 as Pipe;
    //                                CreatePipeOffset(doc, pipe, pipe2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
    //                            }
    //                            else if (mEPCurve is Duct duct)
    //                            {
    //                                var duct2 = mepSeg2 as Duct;
    //                                CreateDuctOffset(doc, duct, duct2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
    //                            }
    //                            else if (mEPCurve is CableTray cableTray)
    //                            {
    //                                var cableTray2 = mepSeg2 as CableTray;
    //                                CreateCableTrayOffset(doc, cableTray, cableTray2, xyz1, xyz2, vEnd1, vEnd2, use90, vertDir, horzDir);
    //                            }
    //                            else return;
    //                            trans.Commit();
    //                        }
    //                        catch (Exception exInner)
    //                        {
    //                            trans.RollBack();
    //                            TaskDialog.Show("翻弯事务错误", $"操作失败：{exInner.Message}\n{exInner.StackTrace}");
    //                        }
    //                    }
    //                }
    //                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //                {
    //                    // 用户按了 ESC 键取消操作
    //                    return;
    //                }
    //                catch (Exception ex)
    //                {
    //                    TaskDialog.Show("翻弯内部事务发生错误", $"操作失败：{ex.Message}");
    //                    return;
    //                }
    //                //}
    //            });
    //        }
    //        return Result.Succeeded;
    //    }
    //    // ==================== 管道双向翻弯 （4个弯头） ====================
    //    private void CreatePipeOffset(Document doc, Pipe refPipe, Pipe refPipe2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
    //    {
    //        ElementId systemTypeId = refPipe.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
    //        ElementId pipeTypeId = refPipe.GetTypeId();
    //        ElementId levelId = refPipe.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
    //        double diameter = refPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //        // 创建立管/斜管1
    //        Pipe leg1 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, xyz1, vEnd1);
    //        leg1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //        // 创建顶部横管
    //        Pipe top = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, vEnd1, vEnd2);
    //        top.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //        // 创建立管/斜管2
    //        Pipe leg2 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, vEnd2, xyz2);
    //        leg2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //        // 创建弯头连接
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
    //            doc.Create.NewElbowFitting(refPipe.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //            doc.Create.NewElbowFitting(refPipe2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
    //        }
    //        catch { }
    //        // mepSeg2 已在调用方处理，此处连接 leg2 尾端到 mepSeg2 起端
    //        // （Revit 在创建管段时会自动对齐，可不手动连接；如需手动则在调用方补充）
    //    }
    //    // ==================== 风管双向翻弯 （4个弯头） ====================
    //    private void CreateDuctOffset(Document doc, Duct refDuct, Duct refDuct2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
    //    {
    //        ElementId systemTypeId = refDuct.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
    //        ElementId ductTypeId = refDuct.GetTypeId();
    //        ElementId levelId = refDuct.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
    //        double width = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
    //        double dHeight = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
    //        Duct leg1 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, xyz1, vEnd1);
    //        Duct top = Duct.Create(doc, systemTypeId, ductTypeId, levelId, vEnd1, vEnd2);
    //        Duct leg2 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, vEnd2, xyz2);
    //        // 设置截面尺寸
    //        foreach (Duct d in new[] { leg1, top, leg2 })
    //        {
    //            d.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.Set(width);
    //            d.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.Set(dHeight);
    //        }
    //        // 创建弯头
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
    //            doc.Create.NewElbowFitting(refDuct.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //            doc.Create.NewElbowFitting(refDuct2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
    //        }
    //        catch { }
    //    }
    //    // ==================== 桥架双向翻弯 （4个弯头） ====================
    //    private void CreateCableTrayOffset(Document doc, CableTray refTray, CableTray refTray2, XYZ xyz1, XYZ xyz2, XYZ vEnd1, XYZ vEnd2, bool use90, XYZ vertDir, XYZ horzDir)
    //    {
    //        ElementId trayTypeId = refTray.GetTypeId();
    //        ElementId levelId = refTray.ReferenceLevel?.Id ?? ElementId.InvalidElementId;

    //        double width = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
    //        double tHeight = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();

    //        CableTray leg1 = CableTray.Create(doc, trayTypeId, xyz1, vEnd1, levelId);
    //        CableTray top = CableTray.Create(doc, trayTypeId, vEnd1, vEnd2, levelId);
    //        CableTray leg2 = CableTray.Create(doc, trayTypeId, vEnd2, xyz2, levelId);

    //        foreach (CableTray ct in new[] { leg1, top, leg2 })
    //        {
    //            ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.Set(width);
    //            ct.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.Set(tHeight);
    //        }

    //        // 桥架弯头：通过连接器连接，Revit自动插入弯头配件
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(top.GetClosestConnector(vEnd2), leg2.GetClosestConnector(vEnd2));
    //            doc.Create.NewElbowFitting(refTray.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //            doc.Create.NewElbowFitting(refTray2.GetClosestConnector(xyz2), leg2.GetClosestConnector(xyz2));
    //        }
    //        catch { }
    //    }
    //    // ==================== 单翻管道连接件（2个弯头）====================
    //    private void CreatePipeBendConnector(Document doc, Pipe refPipe, Pipe refPipe2, XYZ xyz1, XYZ vEnd1)
    //    {
    //        ElementId systemTypeId = refPipe.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
    //        ElementId pipeTypeId = refPipe.GetTypeId();
    //        ElementId levelId = refPipe.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
    //        double diameter = refPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //        // 创建立管/斜管1
    //        Pipe leg1 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, xyz1, vEnd1);
    //        leg1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //        Pipe top = refPipe2;
    //        // 创建弯头连接
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(refPipe.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //        }
    //        catch { }
    //    }
    //    // ==================== 单翻风管连接件（2个弯头）====================
    //    private void CreateDuctBendConnector(Document doc, Duct refDuct, Duct refDuct2, XYZ xyz1, XYZ vEnd1)
    //    {
    //        ElementId systemTypeId = refDuct.MEPSystem?.GetTypeId() ?? ElementId.InvalidElementId;
    //        ElementId ductTypeId = refDuct.GetTypeId();
    //        ElementId levelId = refDuct.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
    //        double width = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
    //        double dHeight = refDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
    //        Duct leg1 = Duct.Create(doc, systemTypeId, ductTypeId, levelId, xyz1, vEnd1);
    //        Duct top = refDuct2;
    //        // 设置截面尺寸
    //        leg1.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.Set(width);
    //        leg1.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.Set(dHeight);
    //        // 创建弯头
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(refDuct.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //        }
    //        catch { }
    //    }
    //    // ==================== 单翻桥架连接件（2个弯头）====================
    //    private void CreateCableTrayBendConnector(Document doc, CableTray refTray, CableTray refTray2, XYZ xyz1, XYZ vEnd1)
    //    {
    //        ElementId trayTypeId = refTray.GetTypeId();
    //        ElementId levelId = refTray.ReferenceLevel?.Id ?? ElementId.InvalidElementId;
    //        double width = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
    //        double tHeight = refTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
    //        CableTray leg1 = CableTray.Create(doc, trayTypeId, xyz1, vEnd1, levelId);
    //        CableTray top = refTray2;
    //        leg1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.Set(width);
    //        leg1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.Set(tHeight);
    //        // 桥架弯头：通过连接器连接，Revit自动插入弯头配件
    //        try
    //        {
    //            doc.Create.NewElbowFitting(leg1.GetClosestConnector(vEnd1), top.GetClosestConnector(vEnd1));
    //            doc.Create.NewElbowFitting(refTray.GetClosestConnector(xyz1), leg1.GetClosestConnector(xyz1));
    //        }
    //        catch { }
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]//L连接水管道 20260628
    //public class PipeBendConnect : IExternalCommand
    //{
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        BentConnect(uiDoc);
    //        return Result.Succeeded;
    //    }
    //    public void BentConnect(UIDocument uiDoc)
    //    {
    //        Document doc = uiDoc.Document;
    //        //////要先选管，默认已选的不作数，不考虑重复执行命令
    //        try
    //        {
    //            // 1. 获取并预检第一根管道
    //            if (!TryGetAndValidatePipe(uiDoc, "请选择第一根管道", out Pipe pipe1, out Connector conn1, out Line line1))
    //                //return Result.Cancelled;
    //                return;
    //            // 2. 获取弯头信息和用户选择的连接策略
    //            if (!pipe1.TryGetFittingAndStrategy(out string strategy))
    //                //return Result.Cancelled;
    //                return;
    //            // 3. 获取并预检第二根管道
    //            if (!TryGetAndValidatePipe(uiDoc, "请选择第二根管道", out Pipe pipe2, out Connector conn2, out Line line2))
    //                //return Result.Cancelled;
    //                return;
    //            // 4. 最终校验两根管道的相对关系，单位：英尺
    //            if (!conn1.Origin.ValidatePointsDistance(conn2.Origin, 0.04, 6))
    //                //return Result.Cancelled;
    //                return;
    //            // 5. 根据几何关系执行连接操作
    //            using (var trans = new Transaction(doc, "管道L型连接"))
    //            {
    //                trans.Start();
    //                bool success = false;
    //                if (line1.IsParallelTo(line2))
    //                {
    //                    success = ConnectParallelPipes(pipe1, conn1, line1, pipe2, conn2, line2, strategy);
    //                }
    //                else
    //                {
    //                    success = ConnectNonParallelPipes(pipe1, conn1, line1, pipe2, conn2, line2, strategy);
    //                }
    //                if (success)
    //                {
    //                    trans.Commit();
    //                    //return Result.Succeeded;
    //                }
    //                else
    //                {
    //                    trans.RollBack();
    //                    //return Result.Cancelled;
    //                    return;
    //                }
    //            }
    //        }
    //        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //        {
    //            // 用户按了 ESC 键取消操作
    //            //return Result.Cancelled;
    //            return;
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog.Show("错误", ex.Message);
    //            //return Result.Cancelled;
    //            return;
    //        }
    //    }
    //    public bool TryGetAndValidatePipe(UIDocument uiDoc, string prompt, out Pipe pipe, out Connector connector, out Line line)
    //    {
    //        pipe = null;
    //        connector = null;
    //        line = null;

    //        Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), prompt);
    //        if (reference == null) return false;
    //        pipe = uiDoc.Document.GetElement(reference) as Pipe;
    //        XYZ pickPoint = reference.GlobalPoint;
    //        if (pipe == null) return false;
    //        if (pipe.IsSlopeGreaterThan(0.02))
    //        {
    //            TaskDialog.Show("限制", "暂不支持坡度过大的管道连接，请手工调整。");
    //            return false;
    //        }
    //        if (!(pipe.Location is LocationCurve lc) || !(lc.Curve is Line l))
    //        {
    //            TaskDialog.Show("限制", "仅支持直线管道。");
    //            return false;
    //        }
    //        line = l;
    //        connector = pipe.GetClosestConnector(pickPoint);
    //        return connector != null;
    //    }
    //    public bool ConnectParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
    //    {
    //        Document doc = p1.Document;
    //        // 分支 1: 共线管道
    //        if (l1.IsCollinear(l2))
    //        {
    //            // 共线：直接连接或变径
    //            if (Math.Abs(p1.Diameter - p2.Diameter) > 1e-6)
    //            {
    //                doc.Create.NewTransitionFitting(c1, c2);
    //            }
    //            else
    //            {
    //                doc.MergeTwoPipes(p1, c1, p2, c2);
    //            }
    //            return true;
    //        }
    //        // 分支 2: 平行、共面但不共线
    //        if (!l1.AreLinesCoPlanar(l2, 1e-6))
    //        {
    //            TaskDialog.Show("限制", "平行的两根管道不共面，无法自动连接。");
    //            return false;
    //        }
    //        // 核心逻辑: 在共面不共线的两线之间创建S弯连接
    //        return CreateS_BendConnection(p1, c1, p2, c2, strategy);
    //        //return true;
    //    }
    //    public bool ConnectNonParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
    //    {
    //        Document doc = p1.Document;
    //        if (l1.AreLinesCoPlanar(l2))
    //        {
    //            // 相交且共面：直接创建弯头
    //            p1.NewElbowBy2MEPCurve(p2);
    //            return true;
    //        }
    //        // 异面：创建立管连接
    //        var intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(l1, l2);
    //        if (intersection2D == null || intersection2D.DistanceTo(c1.Origin) > 4 || intersection2D.DistanceTo(c2.Origin) > 4)
    //        {
    //            TaskDialog.Show("限制", "管道在平面上交点过远，请手工调整。");
    //            return false;
    //        }
    //        double z1 = c1.Origin.Z;
    //        double z2 = c2.Origin.Z;
    //        // 调整原管道至交点
    //        p1.AdjustMEPCurveLength(c1.Origin, -c1.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z1)));
    //        p2.AdjustMEPCurveLength(c2.Origin, -c2.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z2)));
    //        doc.Regenerate();
    //        // 创建立管
    //        Pipe verticalPipe = TryCreateVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
    //        if (verticalPipe == null) return false;
    //        verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
    //        doc.Regenerate();
    //        // 连接
    //        p1.NewElbowBy2MEPCurve(verticalPipe);
    //        p2.NewElbowBy2MEPCurve(verticalPipe);
    //        return true;
    //    }
    //    //基于参照管，两连接器高差，交点建立垂直立管，可复用
    //    public Pipe TryCreateVerticalPipe(Pipe p1, Connector c1, XYZ cp2, XYZ intersection2D, string strategy)
    //    {
    //        Document doc = p1.Document;
    //        double pipeDiameter = p1.Diameter;
    //        double heightDifference = Math.Abs(c1.Origin.Z - cp2.Z);
    //        double requiredMultiplier = 0;
    //        if (strategy == "高概率")
    //        {
    //            requiredMultiplier = 6;
    //        }
    //        else if (strategy == "中概率")
    //        {
    //            requiredMultiplier = 4;
    //        }
    //        double minRequiredHeight = pipeDiameter * requiredMultiplier;
    //        // 2. 检查实际高差是否满足要求
    //        if (heightDifference < minRequiredHeight)
    //        {
    //            // 高差不足，不满足创建条件。直接返回null，由调用者决定是否提示用户。
    //            TaskDialog.Show("tt", $"创建立管失败：实际高差 {heightDifference * 304.8:F3} < 所需最小高差 {minRequiredHeight * 304.8:F3} (策略: {strategy})");
    //            return null;
    //        }
    //        double z1 = c1.Origin.Z;
    //        double z2 = cp2.Z;
    //        if (Math.Abs(z1 - z2) < 0.01) // 0.01 feet
    //        {
    //            TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。"); return null;
    //        }
    //        double minZ = Math.Min(z1, z2);
    //        double maxZ = Math.Max(z1, z2);
    //        XYZ bottomPoint = new XYZ(intersection2D.X, intersection2D.Y, minZ);
    //        XYZ topPoint = new XYZ(intersection2D.X, intersection2D.Y, maxZ);
    //        Pipe verticalPipe = p1.NewPipeBetweenPoints(bottomPoint, topPoint);
    //        return verticalPipe;
    //    }
    //    //// 为两根平行、共面但不共线的管道创建S型连接。
    //    public bool CreateS_BendConnection(Pipe p1, Connector c1, Pipe p2, Connector c2, string strategy)
    //    {
    //        double deltaZ = Math.Abs(c1.Origin.Z - c2.Origin.Z);
    //        double diameter = p1.Diameter;
    //        Pipe connectingPipe = null;
    //        //高差大于50小于200且2倍DN微差连接，后退连接器，根据连接器高度生成斜管再连接
    //        if (deltaZ < p1.Diameter * 2 || deltaZ < 200 / 304.8)
    //        {
    //            if (deltaZ < (60 / 304.8))
    //            {
    //                TaskDialog.Show("tt", "检测到管道差过小，请手工调整");
    //                return false;
    //            }
    //            ////管道连接器后退指定距离，需要考虑管长不能为0或负值
    //            double retreatDistance = p1.Diameter * 3;
    //            if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
    //            {
    //                TaskDialog.Show("限制", "管道长度不足，无法后退创建连接。");
    //                return false;
    //            }
    //            XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
    //            if (newConn1p == null)
    //            {
    //                TaskDialog.Show("tt", "后退管道失败，无法创建连接。");
    //                return false;
    //            }
    //            connectingPipe = p1.NewPipeBetweenPoints(newConn1p, c2.Origin);
    //        }
    //        //45度连接 默认高差4倍DN
    //        else if (deltaZ < p1.Diameter * 4)
    //        {
    //            double retreatDistance = Math.Abs(c1.Origin.Z - c2.Origin.Z);
    //            if (p1.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance)
    //            {
    //                TaskDialog.Show("限制", "管道长度不足以创建45度连接。");
    //                return false;
    //            }
    //            // (保留原始几何逻辑)
    //            double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
    //            XYZ tempPoint = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
    //            if (tempPoint == null)
    //            {
    //                TaskDialog.Show("tt", "步骤1失败：调整管道长度以对齐失败。");
    //                return false;
    //            }
    //            // 在新点上再次后退
    //            XYZ finalPoint = p1.AdjustMEPCurveLength(tempPoint, retreatDistance);
    //            if (finalPoint == null)
    //            {
    //                TaskDialog.Show("tt", "步骤2失败：为连接管预留空间失败。");
    //                return false;
    //            }
    //            // 创建最终的斜管
    //            connectingPipe = p1.NewPipeBetweenPoints(finalPoint, c2.Origin);
    //        }
    //        //90度连接
    //        else if (deltaZ >= p1.Diameter * 4)
    //        {
    //            double coDistance = c1.Origin.GetHorizontalDistance(c2.Origin);
    //            XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, -coDistance);
    //            if (newConn1p == null)
    //            {
    //                TaskDialog.Show("tt", "调整管道长度以对齐失败，无法创建立管。");
    //                return false;
    //            }
    //            XYZ intersection2D = new XYZ(c1.Origin.X, c1.Origin.Y, 0);
    //            connectingPipe = MEPAnalysisExtension.NewVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
    //        }
    //        if (connectingPipe == null)
    //        {
    //            return false;
    //        }
    //        // 步骤3: 执行统一的连接操作
    //        p1.NewElbowBy2MEPCurve(connectingPipe);
    //        p2.NewElbowBy2MEPCurve(connectingPipe);
    //        return true;
    //    }
    //    //建立斜坡度管连接
    //    public Pipe TryCreateSlopePipe(Pipe p1, Connector c1, Connector c2, double retreatDistance)
    //    {
    //        XYZ newConn1p = p1.AdjustMEPCurveLength(c1.Origin, retreatDistance);
    //        if (newConn1p == null)
    //        {
    //            TaskDialog.Show("tt", "未成功建立连接，请手工调整");
    //            return null;
    //        }
    //        //在管2和退后的连接器之间画新管，新管以管1类型，尺寸为准
    //        Pipe newPipe = Pipe.Create(p1.Document, p1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId(), p1.PipeType.Id, p1.ReferenceLevel.Id, newConn1p, c2.Origin);
    //        newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
    //        return newPipe;
    //    }

    //}
    //[Transaction(TransactionMode.Manual)]//T连接水管道 20260628
    //public class PipeTripleConnect : IExternalCommand
    //{
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        TripleConnect(uiDoc);
    //        return Result.Succeeded;
    //    }
    //    public void TripleConnect(UIDocument uiDoc)
    //    {
    //        Document doc = uiDoc.Document;
    //        try
    //        {
    //            // 1. 获取并预检第一根管道（主管）
    //            if (!TryGetAndValidatePipe(uiDoc, "请选择主管（将被打断）", out Pipe mainPipe, out Connector mainConn, out Line mainLine))
    //                //return Result.Cancelled;
    //                return;
    //            // 2. 获取连接策略（主要用于立管连接）
    //            if (!mainPipe.TryGetFittingAndStrategy(out string strategy))
    //                //return Result.Cancelled;
    //                return;
    //            // 3. 获取并预检第二根管道（支管）
    //            if (!TryGetAndValidatePipe(uiDoc, "请选择支管（将连接到主管）", out Pipe branchPipe, out Connector branchConn, out Line branchLine))
    //                //return Result.Cancelled;
    //                return;
    //            // 4. 校验两根管道的相对关系，单位：英尺
    //            if (!mainConn.Origin.ValidatePointsDistance(branchConn.Origin, 0.04, 6))
    //                //return Result.Cancelled;
    //                return;
    //            // 5. 检查管径，通常支管不应大于主管
    //            if (branchPipe.Diameter > mainPipe.Diameter)
    //            {
    //                TaskDialog.Show("警告", "支管管径大于主管，可能无法创建标准三通。程序将继续尝试。");
    //            }
    //            // 6. 根据几何关系执行T型连接操作
    //            using (var trans = new Transaction(doc, "管道T型连接"))
    //            {
    //                trans.Start();
    //                bool success = ConnectTeePipes(mainPipe, mainLine, branchPipe, branchConn, branchLine, strategy);
    //                if (success)
    //                {
    //                    trans.Commit();
    //                    //return Result.Succeeded;
    //                }
    //                else
    //                {
    //                    // 如果失败，相应的子方法已经给出了提示
    //                    trans.RollBack();
    //                    //return Result.Cancelled;
    //                    return;
    //                }
    //            }
    //        }
    //        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //        {
    //            // 用户按了 ESC 键取消操作
    //            //return Result.Cancelled;
    //            return;
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog.Show("错误", ex.Message);
    //            //return Result.Cancelled;
    //            return;
    //        }
    //    }
    //    // 获取并验证管道，T连接或十字连接可能共用
    //    public bool TryGetAndValidatePipe(UIDocument uiDoc, string prompt, out Pipe pipe, out Connector connector, out Line line)
    //    {
    //        pipe = null;
    //        connector = null;
    //        line = null;

    //        Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), prompt);
    //        if (reference == null) return false;
    //        pipe = uiDoc.Document.GetElement(reference) as Pipe;
    //        XYZ pickPoint = reference.GlobalPoint;
    //        if (pipe == null) return false;
    //        if (pipe.IsSlopeGreaterThan(0.02))
    //        {
    //            TaskDialog.Show("限制", "暂不支持坡度过大的管道连接，请手工调整。");
    //            return false;
    //        }
    //        if (!(pipe.Location is LocationCurve lc) || !(lc.Curve is Line l))
    //        {
    //            TaskDialog.Show("限制", "仅支持直线管道。");
    //            return false;
    //        }
    //        line = l;
    //        connector = pipe.GetClosestConnector(pickPoint);
    //        return connector != null;
    //    }
    //    // T型连接的核心调度方法
    //    public bool ConnectTeePipes(Pipe mainPipe, Line mainLine, Pipe branchPipe, Connector branchConn, Line branchLine, string strategy)
    //    {
    //        Document doc = mainPipe.Document;
    //        // Rule 3: 判断管关系，平行管无论是否共线、共面均退出
    //        if (mainLine.IsParallelTo(branchLine))
    //        {
    //            TaskDialog.Show("限制", "T型连接不支持两根平行的管道。");
    //            return false;
    //        }
    //        // 计算两根无限长直线在XY平面上的交点
    //        XYZ intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(mainLine, branchLine);
    //        if (intersection2D == null)
    //        {
    //            // 理论上不会发生，因为已经排除了平行情况
    //            return false;
    //        }
    //        // 将2D交点提升到主管的高度，得到空间中的打断点
    //        XYZ breakPointOnMain = new XYZ(intersection2D.X, intersection2D.Y, mainLine.Origin.Z);
    //        // 判断1: 交点是否在主管的物理范围内?
    //        bool isBreakPointOnMainSegment = mainLine.IsPointOnLine(breakPointOnMain);
    //        // 判断2: 交点是否也在支管的物理范围内?
    //        // (将交点投影到支管高度来判断)
    //        XYZ breakPointOnBranch = new XYZ(intersection2D.X, intersection2D.Y, branchLine.Origin.Z);
    //        bool isBreakPointOnBranchSegment = branchLine.IsPointOnLine(breakPointOnBranch);
    //        // 如果交点不在主管上，则无法进行任何T型连接
    //        if (!isBreakPointOnMainSegment)
    //        {
    //            TaskDialog.Show("限制", "管道投影交点不在主管的物理范围内。");
    //            return false;
    //        }
    //        //判断管关系，共面管道直接生成三通
    //        if (mainLine.AreLinesCoPlanar(branchLine))
    //        {
    //            //调整支管长度，使其端点精确到达打断点
    //            double distToIntersection = branchConn.Origin.DistanceTo(breakPointOnMain);
    //            branchPipe.AdjustMEPCurveLength(branchConn.Origin, -distToIntersection);
    //            if (BreakPipeAndCreateTee(doc, mainPipe, breakPointOnMain, branchPipe))
    //            {
    //                return true;
    //            }
    //            return false;
    //        }
    //        // Rule 5: 不共面管道，根据高差创建连接
    //        else
    //        {
    //            return ConnectSkewTee(mainPipe, breakPointOnMain, branchPipe, branchConn, strategy, isBreakPointOnBranchSegment);
    //        }
    //    }
    //    //处理不共面（异面）管道的T型连接
    //    public bool ConnectSkewTee(Pipe mainPipe, XYZ breakPoint, Pipe branchPipe, Connector branchConn, string strategy, bool useCrossConnectionLogic)
    //    {
    //        Document doc = mainPipe.Document;
    //        // 计算高差：支管端点与它在主管上投影点的高度差
    //        double deltaZ = Math.Abs(branchConn.Origin.Z - breakPoint.Z);
    //        double diameter = branchPipe.Diameter;

    //        // --- 场景1: 高差足够大，且几何关系为“交叉” ---
    //        // 这是我们新增的智能分支
    //        if (deltaZ > diameter * 6 && useCrossConnectionLogic)
    //        {
    //            //TaskDialog.Show("智能连接提示", "检测到交叉管道关系，将使用立管和双三通进行连接。");
    //            //// 直接调用我们封装好的交叉连接方法
    //            return ConnectCrossPipes(mainPipe, branchPipe);
    //        }
    //        // --- 场景2: 其他所有异面情况 (高差小 或 几何关系非“交叉”) ---
    //        // 走原有的“三通+弯头”或“三通+斜管”逻辑
    //        // 检查支管端头距离投影点是否过远
    //        if (breakPoint.DistanceTo(branchConn.Origin) > 2 * 3.28)
    //        {
    //            TaskDialog.Show("限制", "支管端头距离其在主管上的投影点过远(>2m)，无法自动连接。");
    //            return false;
    //        }
    //        // (以下是您原有的 ConnectSkewTee 逻辑)
    //        Pipe connectingPipe = null;
    //        if (deltaZ < diameter * 4) // 微差/斜管连接
    //        {
    //            double retreatDistance = (deltaZ < diameter * 2) ? diameter * 3 : deltaZ;
    //            if (branchPipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() < retreatDistance) return false;
    //            XYZ newBranchEndPoint = branchPipe.AdjustMEPCurveLength(branchConn.Origin, retreatDistance);
    //            if (newBranchEndPoint == null) return false;
    //            connectingPipe = branchPipe.NewPipeBetweenPoints(newBranchEndPoint, breakPoint);
    //        }
    //        else // 立管连接 (Tee + Elbow)
    //        {
    //            XYZ tempC1 = breakPoint;
    //            connectingPipe = MEPAnalysisExtension.NewVerticalPipe(branchPipe, branchConn, tempC1, breakPoint, strategy);
    //        }
    //        if (connectingPipe == null) return false;
    //        if (!BreakPipeAndCreateTee(mainPipe.Document, mainPipe, breakPoint, connectingPipe)) return false;
    //        branchPipe.NewElbowBy2MEPCurve(connectingPipe);
    //        return true;
    //    }
    //    // 在指定点打断主管，并与支管创建一个三通。
    //    public bool BreakPipeAndCreateTee(Document doc, Pipe mainPipe, XYZ breakPoint, MEPCurve branchElement)
    //    {
    //        // 1. 打断主管，返回新生成管道的ID
    //        ElementId newPipeId = PlumbingUtils.BreakCurve(doc, mainPipe.Id, breakPoint);
    //        doc.Regenerate();
    //        Pipe newPipePart = doc.GetElement(newPipeId) as Pipe;
    //        if (newPipePart == null) return false;
    //        // 2. 找到打断点附近的四个连接器
    //        Connector mainConn1 = mainPipe.GetClosestConnector(breakPoint);
    //        Connector mainConn2 = newPipePart.GetClosestConnector(breakPoint);
    //        Connector branchConn = branchElement.GetClosestConnector(breakPoint);
    //        if (mainConn1 == null || mainConn2 == null || branchConn == null) return false;
    //        // 3. 创建三通
    //        doc.Create.NewTeeFitting(mainConn1, mainConn2, branchConn);
    //        return true;
    //    }
    //    // 专用于处理两根异面交叉的水平管道，通过一根立管和两个三通进行连接。
    //    public bool ConnectCrossPipes(Pipe pipe1, Pipe pipe2)
    //    {
    //        Document doc = pipe1.Document;
    //        Line line1 = (pipe1.Location as LocationCurve).Curve as Line;
    //        Line line2 = (pipe2.Location as LocationCurve).Curve as Line;
    //        // 1. 计算XY平面上的投影交点
    //        XYZ intersectionPoint2D = MEPAnalysisExtension.GetIntersectionPoint2D(line1, line2);
    //        if (intersectionPoint2D == null)
    //        {
    //            TaskDialog.Show("错误", "两根管道在XY平面平行，无法生成垂直连接管。");
    //            return false;
    //        }
    //        // 2. 准备创建立管的坐标
    //        double z1 = line1.Origin.Z;
    //        double z2 = line2.Origin.Z;
    //        // 高度检查 (虽然调用前已检查，这里作为安全措施)
    //        if (Math.Abs(z1 - z2) < 0.2) // 约60mm
    //        {
    //            TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。");
    //            return false;
    //        }
    //        // 3. 创建立管
    //        XYZ bottomPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Min(z1, z2));
    //        XYZ topPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Max(z1, z2));
    //        ElementId systemTypeId = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
    //        ElementId pipeTypeId = pipe1.PipeType.Id;
    //        ElementId levelId = pipe1.ReferenceLevel.Id;
    //        Pipe riserPipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, bottomPoint, topPoint);
    //        riserPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe1.Diameter);
    //        // 4. 确定上下管道
    //        Pipe topPipe = z1 > z2 ? pipe1 : pipe2;
    //        Pipe bottomPipe = z1 > z2 ? pipe2 : pipe1;
    //        // 5. 连接顶部和底部
    //        if (!BreakPipeAndCreateTee(doc, topPipe, topPoint, riserPipe))
    //        {
    //            TaskDialog.Show("错误", "创建顶部三通连接失败。");
    //            return false;
    //        }
    //        if (!BreakPipeAndCreateTee(doc, bottomPipe, bottomPoint, riserPipe))
    //        {
    //            TaskDialog.Show("错误", "创建底部三通连接失败。");
    //            return false;
    //        }
    //        return true;
    //    }
    //    public bool ConnectNonParallelPipes(Pipe p1, Connector c1, Line l1, Pipe p2, Connector c2, Line l2, string strategy)
    //    {
    //        Document doc = p1.Document;
    //        if (l1.AreLinesCoPlanar(l2))
    //        {
    //            // 相交且共面：直接创建弯头
    //            p1.NewElbowBy2MEPCurve(p2);
    //            return true;
    //        }
    //        // 异面：创建立管连接
    //        var intersection2D = MEPAnalysisExtension.GetIntersectionPoint2D(l1, l2);
    //        if (intersection2D == null || intersection2D.DistanceTo(c1.Origin) > 4 || intersection2D.DistanceTo(c2.Origin) > 4)
    //        {
    //            TaskDialog.Show("限制", "管道在平面上交点过远，请手工调整。");
    //            return false;
    //        }
    //        double z1 = c1.Origin.Z;
    //        double z2 = c2.Origin.Z;
    //        // 调整原管道至交点
    //        p1.AdjustMEPCurveLength(c1.Origin, -c1.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z1)));
    //        p2.AdjustMEPCurveLength(c2.Origin, -c2.Origin.DistanceTo(new XYZ(intersection2D.X, intersection2D.Y, z2)));
    //        doc.Regenerate();
    //        // 创建立管
    //        Pipe verticalPipe = MEPAnalysisExtension.NewVerticalPipe(p1, c1, c2.Origin, intersection2D, strategy);
    //        if (verticalPipe == null) return false;
    //        verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p1.Diameter);
    //        doc.Regenerate();
    //        // 连接
    //        p1.NewElbowBy2MEPCurve(verticalPipe);
    //        p2.NewElbowBy2MEPCurve(verticalPipe);
    //        return true;
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]//展平管网 20260704
    //public class MEPCurveFaltten : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        // 0. 获取用户选择
    //        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
    //        if (selectedIds.Count == 0)
    //        {
    //            TaskDialog.Show("提示", "请先选择至少一根管道。");
    //            try
    //            {
    //                Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter(), "选择一根管道");
    //                selectedIds.Add(reference.ElementId);
    //            }
    //            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //            {
    //                // 用户按了 ESC 键取消操作
    //                return Result.Cancelled;
    //            }
    //            catch (Exception ex)
    //            {
    //                TaskDialog.Show("错误", ex.Message);
    //                return Result.Cancelled;
    //            }
    //            //return Result.Cancelled;
    //        }
    //        Element startElement = doc.GetElement(selectedIds.First());
    //        // 1. 【新】一次性遍历，获取所有需要的信息
    //        FindAndCategorizePipeNetwork_Optimized(startElement,
    //            out HashSet<MEPCurve> horizontalPipesToProcess,
    //            out HashSet<MEPCurve> verticalPipes,
    //            out HashSet<FamilyInstance> allFittings,
    //            out HashSet<ElementId> connectingFittingIds);
    //        if (horizontalPipesToProcess.Count == 0)
    //        {
    //            TaskDialog.Show("提示", "在所选管网中未找到任何水平管道。");
    //            return Result.Cancelled;
    //        }
    //        // 删除立管连接构件防止移动出错,提醒保存立管id备重新连接，可能立管改变后连接关系会变不考虑平管与立管重连
    //        if (verticalPipes.Any())
    //        {
    //            var verticalPipeIds = verticalPipes.Select(p => p.Id).ToHashSet();
    //            var options = new List<string> { "导出为CSV文件", "导出为JSON文件" };
    //            int choice = TaskDialogHelper.ShowCommandLinks("重要提示", 1, "调整管网高程操作将断开所有立管连接，请注意根据处理管道ID检查并重新连接。", $"发现 {verticalPipeIds.Count} 个立管构件，请选择导出格式。", options);
    //            if (choice != -1)
    //            {
    //                // 1. 统一设置保存对话框
    //                string filter = (choice == 0) ? "CSV 配置文件 (*.csv)|*.csv" : "JSON 文件 (*.json)|*.json";
    //                string ext = (choice == 0) ? "csv" : "json";
    //                SaveFileDialog saveFileDialog = new SaveFileDialog
    //                {
    //                    Filter = filter,
    //                    Title = "保存所选构件ID",
    //                    FileName = $"所选ID_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}"
    //                };
    //                if (saveFileDialog.ShowDialog() != true) return Result.Cancelled;
    //                string filePath = saveFileDialog.FileName;
    //                // 2. 统一执行数据提取 (带进度条)
    //                // 创建一个中间列表来存储提取后的信息
    //                //var extractedData = new List<dynamic>();
    //                var extractedData = new List<ExportData>(verticalPipeIds.Count);
    //                int count = 0;
    //                foreach (var id in verticalPipeIds)
    //                {
    //                    Element elem = doc.GetElement(id);
    //                    count++;
    //                    // 3. 提取数据
    //                    extractedData.Add(new ExportData
    //                    {
    //                        Name = elem.Name, // 或者使用内置参数优化
    //                        ID = elem.Id.IntegerValue.ToString()
    //                    });
    //                }
    //                // 3. 根据选择执行不同的写入操作
    //                try
    //                {
    //                    if (choice == 0)
    //                    {
    //                        string[] headers = { "构件名称", "ElementID" };
    //                        var rows = extractedData.Select(d => new string[] { d.Name, d.ID.ToString() }).ToList();
    //                        new CsvHelper(filePath).WriteAllWithHeaders(headers, rows);
    //                    }
    //                    else if (choice == 1)
    //                    {
    //                        JsonHelper.SaveToFile(filePath, extractedData);
    //                    }
    //                    TaskDialog.Show("成功", $"成功导出 {extractedData.Count} 个构件信息！");
    //                }
    //                catch (Exception ex)
    //                {
    //                    TaskDialog.Show("错误", "操作失败原因参考: " + ex.Message);
    //                }
    //            }
    //        }
    //        // 2. 【新】在删除任何东西之前，先分析并保存水平管网的连接拓扑
    //        Dictionary<int, List<List<MEPCurve>>> junctionGroups = GroupPipesByJunctions_ClusterTraversal(horizontalPipesToProcess, allFittings);
    //        // ★★ 立刻转成 Id，切断对活对象的依赖 ★★
    //        List<List<ElementId>> conn2groupIds =
    //            (junctionGroups.ContainsKey(2) ? junctionGroups[2] : new List<List<MEPCurve>>())
    //            .Select(g => g.Where(p => p != null).Select(p => p.Id).ToList()).ToList();
    //        List<List<ElementId>> conn3groupIds =
    //            (junctionGroups.ContainsKey(3) ? junctionGroups[3] : new List<List<MEPCurve>>())
    //            .Select(g => g.Where(p => p != null).Select(p => p.Id).ToList()).ToList();
    //        List<List<ElementId>> conn4groupIds =
    //            (junctionGroups.ContainsKey(4) ? junctionGroups[4] : new List<List<MEPCurve>>())
    //            .Select(g => g.Where(p => p != null).Select(p => p.Id).ToList()).ToList();
    //        // 计算统一的目标Z值 (使用第一个管道的参考标高)
    //        var firstPipe = horizontalPipesToProcess.First() as MEPCurve;
    //        Level level = doc.GetElement(firstPipe.ReferenceLevel.Id) as Level;
    //        if (level == null)
    //        {
    //            TaskDialog.Show("错误", "无法获取参考标高。");
    //            return Result.Cancelled;
    //        }
    //        UniversalNewString subView = new UniversalNewString("请输入要调整到距该层标高多高,默认为0");
    //        if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
    //        {
    //            TaskDialog.Show("tt", "输入属性遇到错误，请重试");
    //            return Result.Cancelled;
    //        }
    //        double targetHeight = vm.NewNum;
    //        using (Transaction t = new Transaction(doc, "强制平整管道"))
    //        {
    //            t.Start();
    //            // 3. 打破约束：删除所有相关的管件
    //            if (allFittings.Any())
    //            {
    //                doc.Delete(allFittings.Select(f => f.Id).ToList());
    //            }
    //            // 4. 批量修改：移动所有独立的水平管道
    //            foreach (MEPCurve pipe in horizontalPipesToProcess)
    //            {
    //                ProcessPipe(doc, pipe, targetHeight); // 调用你已有的方法
    //            }
    //            //// 强制刷新模型以确保几何位置更新
    //            doc.Regenerate();
    //            // 6. 【新】重新连接：根据之前保存的拓扑图，带重映射地调用连接方法
    //            var ctx = new MergeConnectContext();
    //            foreach (var groupIds in conn2groupIds)
    //            {
    //                // 3.1 逐个 Id 解析到“当前存活的接班管”
    //                var aliveIds = groupIds
    //                    .Select(id => ctx.ResolveAlive(doc, id))
    //                    .Where(id => id != null).Distinct().ToList();
    //                // 3.2 惰性取元素（此刻取的一定是活的）
    //                var validGroup = aliveIds
    //                    .Select(id => doc.GetElement(id) as MEPCurve)
    //                    .Where(m => m != null).ToList();
    //                // 3.3 判定
    //                if (validGroup.Count < 2)
    //                {
    //                    var lost = groupIds
    //                        .Where(id => ctx.ResolveAlive(doc, id) == null)
    //                        .Select(id => id.ToString());
    //                    if (lost.Any())
    //                        ctx.Errors.Add($"管对 [{string.Join(",", groupIds)}] 中 [{string.Join(",", lost)}] " + "已被前序合并删除且无接班管，跳过。");
    //                    // 若是两者归并成同一根 => 本就已连好，静默跳过
    //                    continue;
    //                }
    //                if (validGroup.Count > 2)
    //                {
    //                    // 理论上 conn2 不该出现，防御性处理：只取前两根
    //                    validGroup = validGroup.Take(2).ToList();
    //                }
    //                connect2MEPCurves(validGroup, ctx);
    //            }
    //            if (ctx.Errors.Any())
    //            {
    //                TaskDialog.Show("连接报告",
    //                    $"共 {ctx.Errors.Count} 处提示：\n" + string.Join("\n", ctx.Errors));
    //            }
    //            foreach (var group in conn3groupIds)
    //            {
    //                var validGroup = group.Select(p => doc.GetElement(p) as MEPCurve).Where(p => p != null).ToList();
    //                validGroup.connect3MEPCurves();
    //            }

    //            foreach (var group in conn4groupIds)
    //            {
    //                var validGroup = group.Select(p => doc.GetElement(p) as MEPCurve).Where(p => p != null).ToList();
    //                validGroup.connect4MEPCurves();
    //            }
    //            t.Commit();
    //        }
    //        TaskDialog.Show("完成", $"已处理 {horizontalPipesToProcess.Count} 根水平管道，并重新生成了连接。");
    //        return Result.Succeeded;
    //    }
    //    /// <summary>
    //    /// 【优化版】从起点开始，遍历并分类整个水平连接的管网。
    //    /// 当遇到立管时，会记录立管和其连接管件，并停止对该垂直分支的进一步遍历。
    //    /// </summary>
    //    /// <param name="startElement">遍历起点。</param>
    //    /// <param name="horizontalPipes">输出：所有水平管道（或被视为水平处理的斜管）。</param>
    //    /// <param name="verticalPipes">输出：所有直接与水平管网相连的立管。</param>
    //    /// <param name="allFittings">输出：水平管网中的所有管件（不包括仅在立管系统中的管件）。</param>
    //    /// <param name="connectingFittingIds">输出：连接到立管的管件的ID集合，用于后续删除。</param>
    //    public void FindAndCategorizePipeNetwork_Optimized(Element startElement,
    //        out HashSet<MEPCurve> horizontalPipes, out HashSet<MEPCurve> verticalPipes,
    //        out HashSet<FamilyInstance> allFittings, out HashSet<ElementId> connectingFittingIds)
    //    {
    //        // 1. 初始化所有输出集合
    //        horizontalPipes = new HashSet<MEPCurve>(new ElementIdComparer());
    //        verticalPipes = new HashSet<MEPCurve>(new ElementIdComparer());
    //        allFittings = new HashSet<FamilyInstance>(new ElementIdComparer());
    //        connectingFittingIds = new HashSet<ElementId>();

    //        if (startElement == null) return;

    //        var queue = new Queue<Element>();
    //        var visited = new HashSet<ElementId>();

    //        // 2. 启动遍历
    //        queue.Enqueue(startElement);
    //        visited.Add(startElement.Id);

    //        while (queue.Count > 0)
    //        {
    //            Element currentElem = queue.Dequeue();

    //            // 3. 遍历当前元素的邻居来决定行为（这比先分类当前元素更符合逻辑）
    //            foreach (var conn in MEPAnalysisExtension.GetConnectors(currentElem))
    //            {
    //                if (!conn.IsConnected) continue;

    //                foreach (var refConn in conn.AllRefs.OfType<Connector>())
    //                {
    //                    Element neighbor = refConn.Owner;

    //                    // 跳过无效或已访问的邻居
    //                    if (neighbor == null || !visited.Add(neighbor.Id))
    //                    {
    //                        continue;
    //                    }

    //                    // 4. 【核心逻辑】根据邻居的类型进行判断和分类
    //                    if (neighbor is MEPCurve neighborPipe)
    //                    {
    //                        if (neighborPipe.IsVertical())
    //                        {
    //                            // Case A: 邻居是立管
    //                            // 记录下这个立管
    //                            verticalPipes.Add(neighborPipe);

    //                            // 当前元素（currentElem）就是连接管件
    //                            if (currentElem is FamilyInstance fitting)
    //                            {
    //                                allFittings.Add(fitting); // 确保连接管件也被加入列表
    //                                connectingFittingIds.Add(fitting.Id);
    //                            }

    //                            // **关键点**：不将立管 neighbor 加入队列，停止该分支的遍历。
    //                        }
    //                        else
    //                        {
    //                            // Case B: 邻居是水平管（或斜管）
    //                            horizontalPipes.Add(neighborPipe);
    //                            queue.Enqueue(neighbor); // 继续遍历
    //                        }
    //                    }
    //                    else if (neighbor is FamilyInstance neighborFitting && neighborFitting.IsMEPFitting())
    //                    {
    //                        // Case C: 邻居是管件
    //                        allFittings.Add(neighborFitting);
    //                        queue.Enqueue(neighbor); // 继续遍历
    //                    }
    //                    // Case D: 邻居是其他类型，忽略并停止该分支遍历
    //                }
    //            }

    //            // 5. 在循环的最后，确保当前元素本身也被正确分类（处理起点的情况）
    //            if (currentElem is MEPCurve currentPipe)
    //            {
    //                if (currentPipe.IsVertical())
    //                {
    //                    // 如果起点是立管，它应该已经被邻居逻辑处理过了，但以防万一
    //                    verticalPipes.Add(currentPipe);
    //                }
    //                else
    //                {
    //                    horizontalPipes.Add(currentPipe);
    //                }
    //            }
    //            else if (currentElem is FamilyInstance currentFitting && currentFitting.IsMEPFitting())
    //            {
    //                allFittings.Add(currentFitting);
    //            }
    //        }
    //    }
    //    //0701 按连接方式分类管道组合 
    //    /// 根据管件的位置和连接关系，分析并分组水平管道。
    //    /// 最终优化版：通过遍历“管件簇”来对水平管道进行分组。
    //    /// 此方法可以“看穿”变径等中间管件。
    //    /// <param name="horizontalPipes">需要分析的水平管道集合。</param>
    //    /// <param name="allFittings">网络中所有的管件。</param>
    //    /// <returns>一个字典，Key是连接点数量(2,3,4等)，Value是这些连接组的列表。</returns>
    //    public Dictionary<int, List<List<MEPCurve>>> GroupPipesByJunctions_ClusterTraversal(
    //        HashSet<MEPCurve> horizontalPipes,
    //        HashSet<FamilyInstance> allFittings)
    //    {
    //        var result = new Dictionary<int, List<List<MEPCurve>>>();
    //        var horizontalPipeIds = new HashSet<ElementId>(horizontalPipes.Select(p => p.Id));

    //        // 用于确保每个管件只参与一次成组过程，避免重复计算
    //        var processedFittingIds = new HashSet<ElementId>();

    //        foreach (var startFitting in allFittings)
    //        {
    //            // 如果这个管件已经被其他簇的搜索处理过，则跳过
    //            if (processedFittingIds.Contains(startFitting.Id))
    //            {
    //                continue;
    //            }
    //            // --- 开始一个新的管件簇搜索 ---
    //            var currentPipeGroup = new List<MEPCurve>();
    //            var fittingQueue = new Queue<FamilyInstance>();
    //            // 初始化搜索
    //            fittingQueue.Enqueue(startFitting);
    //            processedFittingIds.Add(startFitting.Id);
    //            while (fittingQueue.Count > 0)
    //            {
    //                var currentFitting = fittingQueue.Dequeue();
    //                // 遍历当前管件的所有连接器
    //                foreach (var conn in MEPAnalysisExtension.GetConnectors(currentFitting))
    //                {
    //                    if (!conn.IsConnected) continue;
    //                    var connectedNeighbor = conn.AllRefs.OfType<Connector>().FirstOrDefault()?.Owner;
    //                    if (connectedNeighbor == null) continue;
    //                    // Case 1: 邻居是我们要找的水平管道
    //                    if (horizontalPipeIds.Contains(connectedNeighbor.Id))
    //                    {
    //                        var pipe = connectedNeighbor as MEPCurve;
    //                        // 避免在同一个簇内重复添加同一个管道
    //                        if (pipe != null && !currentPipeGroup.Any(p => p.Id == pipe.Id))
    //                        {
    //                            currentPipeGroup.Add(pipe);
    //                        }
    //                    }
    //                    // Case 2: 邻居是另一个管件
    //                    else if (connectedNeighbor is FamilyInstance nextFitting)
    //                    {
    //                        // 如果这个新发现的管件还没被处理过，就加入队列继续搜索，并标记为已处理
    //                        if (processedFittingIds.Add(nextFitting.Id))
    //                        {
    //                            fittingQueue.Enqueue(nextFitting);
    //                        }
    //                    }
    //                    // Case 3: 邻居是立管或其他我们不关心的元素，直接忽略
    //                }
    //            }
    //            // --- 搜索结束，整理结果 ---
    //            int count = currentPipeGroup.Count;
    //            if (count > 1) // 只有连接数大于1的组才有意义
    //            {
    //                if (!result.ContainsKey(count))
    //                {
    //                    result[count] = new List<List<MEPCurve>>();
    //                }
    //                result[count].Add(currentPipeGroup);
    //            }
    //        }
    //        return result;
    //    }
    //    public struct ExportData // 使用 struct 内存占用更紧凑
    //    {
    //        public string Name;
    //        public string ID;
    //    }
    //    private bool ProcessPipe(Document doc, MEPCurve pipe, double height)
    //    {
    //        LocationCurve lc = pipe.Location as LocationCurve;
    //        if (pipe.IsVertical())
    //        {
    //            //TaskDialog.Show("tt", "管道近似立管，压平后长度为0，跳过处理。");
    //            return false;
    //        }
    //        if (lc == null) return false;
    //        Line oldLine = lc.Curve as Line;
    //        if (oldLine == null) return false;
    //        //找管道所在楼层
    //        Level level = doc.GetElement(pipe.ReferenceLevel.Id) as Level;
    //        if (level == null)
    //        {
    //            TaskDialog.Show("tt", "无法获取参考楼层。");
    //            return false;
    //        }
    //        XYZ oldP0 = oldLine.GetEndPoint(0);
    //        XYZ oldP1 = oldLine.GetEndPoint(1);
    //        double targetZ = level.Elevation + height / 304.8;
    //        XYZ newP0 = new XYZ(oldP0.X, oldP0.Y, targetZ);
    //        XYZ newP1 = new XYZ(oldP1.X, oldP1.Y, targetZ);
    //        // 再次确认管还存在
    //        pipe = doc.GetElement(pipe.Id) as MEPCurve;
    //        if (pipe == null)
    //        {
    //            TaskDialog.Show("tt", "原管道在删除相邻管件后失效。");
    //            return false;
    //        }
    //        // 强制改为水平线
    //        Line newLine = Line.CreateBound(newP0, newP1);
    //        lc.Curve = newLine;
    //        return true;
    //    }
    //    //claude 4.8推荐更健壮的两管连接处理，在主流程中执行。
    //    public void connect2MEPCurves(List<MEPCurve> curves, MergeConnectContext ctx = null)
    //    {
    //        if (curves == null || curves.Count != 2) return;
    //        Document doc = curves.FirstOrDefault()?.Document;
    //        if (doc == null) return;

    //        MEPCurve m1 = curves.FirstOrDefault();
    //        MEPCurve m2 = curves.Last();

    //        // 同一根，无需连接（重映射后可能出现）
    //        if (m1.Id == m2.Id) return;

    //        try
    //        {
    //            (Connector c1, Connector c2) = MEPAnalysisExtension.GetClosestConnectorsTuple(
    //                m1.GetConnectors().ToList(), m2.GetConnectors().ToList());
    //            if (c1 == null || c2 == null)
    //            {
    //                ctx?.Errors.Add($"管 {m1.Id} 与 {m2.Id} 找不到可用连接器。");
    //                return;
    //            }

    //            if (m1.Category.Id != m2.Category.Id)
    //            {
    //                ctx?.Errors.Add($"管 {m1.Id} 与 {m2.Id} 类别不同，跳过。");
    //                return;
    //            }

    //            // 高度校验 (Z轴)
    //            if (Math.Abs(c1.Origin.Z - c2.Origin.Z) > 0.001)
    //            {
    //                ctx?.Errors.Add($"管 {m1.Id} 与 {m2.Id} 高度不一致，跳过。");
    //                return;
    //            }

    //            Line l1 = (m1.Location as LocationCurve).Curve as Line;
    //            Line l2 = (m2.Location as LocationCurve).Curve as Line;

    //            bool isParallel = MEPAnalysisExtension.IsParallelTo(l1, l2);
    //            if (isParallel)
    //            {
    //                bool isCollinear = MEPAnalysisExtension.IsCollinear(l1, l2);
    //                if (isCollinear)
    //                {
    //                    double size1 = MEPAnalysisExtension.GetMEPCurveMainSize(m1);
    //                    double size2 = MEPAnalysisExtension.GetMEPCurveMainSize(m2);
    //                    if (Math.Abs(size1 - size2) > 0.001)
    //                    {
    //                        // A: 变径连接
    //                        doc.Create.NewTransitionFitting(c1, c2);
    //                    }
    //                    else
    //                    {
    //                        // B: 等径合并 —— 注意：这里会删除 m2，必须回传重映射
    //                        MEPAnalysisExtension.MergeTwoPipes(doc, m1, c1, m2, c2, ctx);
    //                    }
    //                }
    //                else
    //                {
    //                    ctx?.Errors.Add($"管 {m1.Id} 与 {m2.Id} 平行但错开(不共线)，无法连接。");
    //                    return;
    //                }
    //            }
    //            else
    //            {
    //                // C: 不平行 → 弯头
    //                doc.Create.NewElbowFitting(c1, c2);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // 不再直接 throw，避免中断整个批处理；记录后继续
    //            ctx?.Errors.Add($"管 {m1?.Id} 与 {m2?.Id} 连接异常: {ex.Message}");
    //        }
    //    }
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
}
