using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.WebRequestMethods;


namespace CreatePipe.Form
{
    /// <summary>
    /// SprinklerReplaceAmendView.xaml 的交互逻辑
    /// </summary>
    public partial class SprinklerReplaceAmendView : Window
    {
        public SprinklerReplaceAmendView(UIApplication uIApp)
        {
            InitializeComponent();
            this.DataContext = new SprinklerReplaceAmendViewModel(uIApp);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class SprinklerReplaceAmendViewModel : ObserverableObject
    {
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        public Document Document;
        public UIDocument uIDocument;
        public SprinklerReplaceAmendViewModel(UIApplication uiApp)
        {
            uIDocument = uiApp.ActiveUIDocument;
            Document = uIDocument.Document;

            // 1. 【性能优化】直接利用 Cast<FamilyInstance>()，跳过二次 Document.GetElement 的巨大开销
            var sprinklers = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
            foreach (var sprinkler in sprinklers)
            {
                AllSprinklerCount++;

                // 2. 【防报错】使用 FirstOrDefault 并结合 ?. 判空
                var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                if (connector == null) continue;
                // 获取 Z 轴方向
                double z = connector.CoordinateSystem.BasisZ.Z;
                bool isDown = Math.Abs(z - 1) < 0.001;
                bool isUp = Math.Abs(z - (-1)) < 0.001;
                // 既非上喷也非下喷，直接跳过
                if (!isDown && !isUp) continue;
                // 3. 基础统计
                if (isDown)
                {
                    AllDownSprinklerCount++;
                    AllDownSprinkler.Add(sprinkler.Id);
                    DownSprinklerType.Add(sprinkler.Symbol.Family); // 建议原定义使用 HashSet<Family> 避免重复
                }
                else
                {
                    AllUpSprinklerCount++;
                    AllUpSprinkler.Add(sprinkler.Id);
                    UpSprinklerType.Add(sprinkler.Symbol.Family);
                }
                // 4. 连接状态及双喷头逻辑统计
                if (!connector.IsConnected) continue;
                if (isDown) ConnectedDownSprinklerCount++; else ConnectedUpSprinklerCount++;
                // 获取与喷头直接相连的有效图元（排除喷头自身和逻辑连接器）
                var connectedElem = connector.AllRefs.OfType<Connector>()
                    .FirstOrDefault(c => c.ConnectorType != ConnectorType.Logical && c.Owner?.Id != sprinkler.Id)?.Owner;
                // 原逻辑中：只处理直接连着管件(FamilyInstance)的情况，连着管道的无后续操作
                if (connectedElem is FamilyInstance fitting1)
                {
                    // 获取立管
                    if (MEPAnalysisExtension.GetVerticalMEPCurve(sprinkler) is Pipe verticalPipe)
                    {
                        // 5. 【性能优化】用 LINQ 一句话替代原来的 3 层 foreach 嵌套，寻找管件远端的另一个管件(fitting2)
                        var fitting2 = verticalPipe.ConnectorManager.Connectors.OfType<Connector>()
                            .SelectMany(c => c.AllRefs.OfType<Connector>())
                            .FirstOrDefault(c => c.Owner is FamilyInstance fi && fi.Id != fitting1.Id && fi.Id != sprinkler.Id)?.Owner as FamilyInstance;
                        if (fitting2 != null)
                        {
                            int result = GetFittingCategory(fitting2);
                            // 判断是否为四通(4) 或 向上三通(3)
                            bool isDoubleSetup = result == 4 || (result == 3 && fitting2.HandOrientation.Z == 1);
                            if (isDoubleSetup)
                            {
                                ConnectedDoubleSprinklerCount++;
                                if (isDown) ConnectedDoubleDownSprinkler.Add(sprinkler.Id);
                                else ConnectedDoubleUpSprinkler.Add(sprinkler.Id);
                            }
                            else // 单喷头连接到了其他普通管件
                            {
                                if (isDown) ConnectedDownSprinkler.Add(sprinkler.Id);
                                else ConnectedUpSprinkler.Add(sprinkler.Id);
                            }
                        }
                    }
                }
            }
            SelectedDownSp = DownSprinklerType.FirstOrDefault();
            SelectedUpSp = UpSprinklerType.FirstOrDefault();
        }
        /// 喷头转换模式
        private enum SprinklerConvertMode { ToUp, ToDown, ToDouble }
        public ICommand ConvertUpSprinklerCommand
            => new BaseBindingCommand(obj => ConvertSprinkler(obj, SprinklerConvertMode.ToUp));
        public ICommand ConvertDownSprinklerCommand
            => new BaseBindingCommand(obj => ConvertSprinkler(obj, SprinklerConvertMode.ToDown));
        public ICommand ConvertDoubleSprinklerCommand
            => new BaseBindingCommand(obj => ConvertSprinkler(obj, SprinklerConvertMode.ToDouble));
        /// 喷头转换核心逻辑（上喷 / 下喷 / 上下双喷） 
        private void ConvertSprinkler(object obj, SprinklerConvertMode mode)
        {
            ExternalHandler.Run(app =>
            {
                try
                {
                    Reference r = uIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
                    FamilyInstance sp = (FamilyInstance)Document.GetElement(r);
                    // 校验连接器并用容差判断方向Z == 1 (朝上) -> 下喷 ; Z == -1 (朝下) -> 上喷
                    var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault(c => c.IsConnected);
                    if (connector == null) return;
                    double zValue = connector.CoordinateSystem.BasisZ.Z;
                    bool isDown = Math.Abs(zValue - 1) < 0.01;
                    bool isUp = Math.Abs(zValue - (-1)) < 0.01;

                    NewTransaction.Execute(Document, "喷头方向转换", () =>
                    {
                        // 获取管道与管件基础信息
                        Pipe pipe = MEPAnalysisExtension.GetVerticalMEPCurve(sp) as Pipe;
                        FamilyInstance targetFitting = GetTargetFitting(sp, pipe, connector);
                        if (pipe == null || targetFitting == null) return;

                        ElementId pipeSystemId = pipe.MEPSystem.GetTypeId();
                        double pipeDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                        var routingManager = pipe.PipeType.RoutingPreferenceManager;
                        ElementId pipeTypeId = pipe.PipeType.Id;
                        ElementId pipeLevelId = pipe.ReferenceLevel.Id;
                        ElementId systemId = pipe.MEPSystem.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsElementId();
                        var ft1Connectors = MEPAnalysisExtension.GetConnectors(targetFitting).ToList();
                        List<Connector> horizontalConnectors = ft1Connectors.Where(c => MEPAnalysisExtension.IsHorizontalConnector(c)).ToList();
                        // 找水平管及旧管件
                        var (oldConnectFittings, CurveHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalCurves(connector);
                        List<Pipe> pipeHorizontal = CurveHorizontal.Cast<Pipe>().ToList();
                        if (pipeHorizontal.Count == 0) return;
                        Line pipeLine = ((LocationCurve)pipeHorizontal.FirstOrDefault().Location).Curve as Line;
                        // 初始化删除集合，直接将初步找到的旧管件加入，省去后续重复 UnionWith
                        var toRemoveElems = new HashSet<ElementId>(oldConnectFittings);
                        FamilyInstance newFitting = null;
                        List<Connector> newFitConns = null; // 用于复用横管连接
                        switch (ft1Connectors.Count)
                        {
                            // 四通处理
                            case 4:
                                if (mode == SprinklerConvertMode.ToDouble || horizontalConnectors.Count != 2 || (!isUp && !isDown)) return;
                                // 检查是否点反了喷头，需要获取对侧
                                if ((mode == SprinklerConvertMode.ToUp && isDown) || (mode == SprinklerConvertMode.ToDown && isUp))
                                {
                                    sp = GetOppositeSprinkler(sp, targetFitting);
                                    connector = sp?.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                                    if (connector == null) return;
                                    // 喷头改变，重新收集旧构件
                                    (oldConnectFittings, CurveHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalCurves(connector);
                                    pipeHorizontal = CurveHorizontal.Cast<Pipe>().ToList();
                                    toRemoveElems.UnionWith(oldConnectFittings);
                                }
                                toRemoveElems.Remove(sp.Id);
                                newFitting = CreateNewFitting(targetFitting, routingManager, pipeDiameter, 3);
                                MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, 90);
                                MEPAnalysisExtension.RotateTeeFittingVertical(newFitting, pipeHorizontal.FirstOrDefault(), mode == SprinklerConvertMode.ToDown);
                                // 继承旧四通管径
                                var minRadius = ft1Connectors.Where(c => c.ConnectorType != ConnectorType.Logical && MEPAnalysisExtension.IsHorizontalConnector(c))
                                                             .Select(c => c.Radius).DefaultIfEmpty(double.MaxValue).Min();
                                if (minRadius != double.MaxValue && newFitting != null)
                                {
                                    foreach (var item in MEPAnalysisExtension.GetConnectors(newFitting).Where(c => c.ConnectorType != ConnectorType.Logical && MEPAnalysisExtension.IsHorizontalConnector(c)))
                                        item.Radius = minRadius;
                                }
                                Document.Delete(toRemoveElems.ToList());
                                // 统一连接横管逻辑
                                newFitConns = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                foreach (var item in pipeHorizontal)
                                    MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, MEPAnalysisExtension.GetClosestConnectorsTuple(newFitConns, MEPAnalysisExtension.GetConnectors(item).ToList()).Item1);
                                // 连接三通与立管
                                var firstPipe = pipeHorizontal.FirstOrDefault();
                                if (firstPipe != null)
                                {
                                    Pipe verticalPipe = Pipe.Create(Document, pipeTypeId, pipeLevelId, MEPAnalysisExtension.GetTeeSideConn(newFitting), connector);
                                    verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipeDiameter);
                                    verticalPipe.MEPSystem.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).Set(systemId);
                                }
                                break;
                            //三通处理
                            case 3:
                                if (horizontalConnectors.Count == 2)
                                {
                                    Line rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
                                    if (mode == SprinklerConvertMode.ToDouble)
                                    {
                                        pipeDiameter = MEPAnalysisExtension.GetMaxOrMinPipeDiameter(pipeHorizontal, true);
                                        newFitting = CreateNewFitting(targetFitting, routingManager, pipeDiameter, 4);

                                        //旋转四通对齐方向 (水平面旋转)
                                        // 计算旋转轴（使用管道方向）
                                        Line rotationAxis2 = Line.CreateBound(((LocationPoint)targetFitting.Location).Point, ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);
                                        XYZ targetFittingCenter = ((LocationPoint)targetFitting.Location).Point;
                                        Connector pipeOpenEnd = pipeHorizontal.FirstOrDefault().ConnectorManager.Connectors.OfType<Connector>().OrderBy(c => c.Origin.DistanceTo(targetFittingCenter)).FirstOrDefault();
                                        if (pipeOpenEnd == null) return;
                                        // 计算需要旋转的角度，目标与水平管道端头方向相反
                                        XYZ targetDirection = -pipeOpenEnd.CoordinateSystem.BasisZ;
                                        Connector sideConn = MEPAnalysisExtension.GetConnectors(newFitting).FirstOrDefault();
                                        double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                                        // 确定旋转方向
                                        XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                                        if (cross.Z < 0) angle = -angle;
                                        ElementTransformUtils.RotateElement(Document, newFitting.Id, rotationAxis2, angle + Math.PI / 2);
                                        Document.Regenerate();
                                        RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, 90);
                                    }
                                    else // 上下互换
                                    {
                                        if ((mode == SprinklerConvertMode.ToUp && isUp) || (mode == SprinklerConvertMode.ToDown && isDown)) return;
                                        newFitting = CreateNewFitting(targetFitting, routingManager, pipeDiameter, 3);
                                        newFitConns = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                        var newHorizConns = newFitConns.Where(c => MEPAnalysisExtension.IsHorizontalConnector(c)).ToList();

                                        //旋转三通对齐方向 (水平面旋转)
                                        Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(newFitting);
                                        // 计算旋转轴（使用管道方向）
                                        Line rotationAxis2 = Line.CreateBound(((LocationPoint)targetFitting.Location).Point, ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);
                                        XYZ targetFittingCenter = ((LocationPoint)targetFitting.Location).Point;
                                        Connector pipeOpenEnd = pipeHorizontal.FirstOrDefault().ConnectorManager.Connectors.OfType<Connector>().OrderBy(c => c.Origin.DistanceTo(targetFittingCenter)).FirstOrDefault();
                                        if (sideConn == null || pipeOpenEnd == null) return;
                                        // 计算需要旋转的角度，目标与水平管道端头方向相反
                                        XYZ targetDirection = -pipeOpenEnd.CoordinateSystem.BasisZ;
                                        double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                                        // 确定旋转方向
                                        XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                                        if (cross.Z < 0) angle = -angle;
                                        ElementTransformUtils.RotateElement(Document, newFitting.Id, rotationAxis2, angle + Math.PI / 2);
                                        Document.Regenerate();
                                        //绕水平管转向
                                        MEPAnalysisExtension.RotateTeeFittingVertical(newFitting, pipeHorizontal.FirstOrDefault(), mode == SprinklerConvertMode.ToDown);
                                        toRemoveElems.Add(sp.Id);
                                    }
                                    FamilyInstance newSprinkler = CreateNewSprinkler(sp, targetFitting);
                                    MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                    Connector newSpConn = MEPAnalysisExtension.GetConnectors(newSprinkler).FirstOrDefault();
                                    MEPAnalysisExtension.NewPipeBetweenConnectors(Document, MEPAnalysisExtension.GetClosestConnector(newFitting, newSpConn.Origin), newSpConn, pipe.GetTypeId(), pipe.ReferenceLevel.Id, pipeSystemId, pipeDiameter);
                                    Document.Delete(toRemoveElems.ToList());
                                    Document.Regenerate();
                                    // 统一连接横管逻辑
                                    newFitConns = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                    foreach (var item in pipeHorizontal)
                                        MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, MEPAnalysisExtension.GetClosestConnectorsTuple(newFitConns, MEPAnalysisExtension.GetConnectors(item).ToList()).Item1);
                                    // 如果是双喷，还要把原喷头接回来
                                    if (mode == SprinklerConvertMode.ToDouble)
                                    {
                                        var (connFitOld, connSp) = MEPAnalysisExtension.GetClosestConnectorsTuple(newFitConns, MEPAnalysisExtension.GetConnectors(sp).ToList());
                                        MEPAnalysisExtension.NewPipeBetweenConnectors(Document, connFitOld, connSp, pipeHorizontal.FirstOrDefault().GetTypeId(), pipeHorizontal.FirstOrDefault().get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pipeSystemId, pipeDiameter);
                                    }
                                }
                                else if (horizontalConnectors.Count == 1) // 端头立管
                                {
                                    if (mode == SprinklerConvertMode.ToDouble) return;
                                    FamilyInstance oppoSp = GetOppositeSprinkler(sp, targetFitting);
                                    var oppoConnector = oppoSp?.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                                    if (oppoConnector == null) return;
                                    var (oppoFittings, oppoCurves) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalCurves(oppoConnector);
                                    var targetHorizontalPipe = oppoCurves.Cast<Pipe>().FirstOrDefault();
                                    if (targetHorizontalPipe == null) return;
                                    bool keepUp = mode == SprinklerConvertMode.ToUp;
                                    if ((keepUp && isUp) || (!keepUp && isDown))
                                    {
                                        toRemoveElems.Add(oppoSp.Id);
                                    }
                                    else
                                    {
                                        toRemoveElems.Add(sp.Id);
                                        HashSet<ElementId> ids = new HashSet<ElementId>();
                                        pipe = MEPAnalysisExtension.GetVerticalMEPCurve(oppoSp) as Pipe;
                                    }
                                    toRemoveElems.UnionWith(oppoFittings);
                                    toRemoveElems.ExceptWith(MEPAnalysisExtension.GetAllConnectedElementsAndStopByVerticalInstance(pipe, keepUp));
                                    Document.Delete(toRemoveElems.ToList());
                                    MEPAnalysisExtension.NewElbowBy2MEPCurve(pipe, targetHorizontalPipe);
                                }
                                break;
                            //弯头处理
                            case 2:
                                if (mode == SprinklerConvertMode.ToDouble)
                                {
                                    newFitting = CreateNewFitting(targetFitting, routingManager, pipeDiameter, 3);
                                    MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                    RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, 90);
                                    FamilyInstance newSprinkler = CreateNewSprinkler(sp, targetFitting);
                                    var closeFitConn = MEPAnalysisExtension.GetClosestConnector(pipe, connector.Origin);
                                    // 重新获取更远的立管段横管
                                    List<ElementId> farOldFittings = new List<ElementId>();
                                    (farOldFittings, CurveHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalCurves(MEPAnalysisExtension.GetOppositeConnector(pipe, closeFitConn));
                                    toRemoveElems.UnionWith(farOldFittings);
                                    Document.Delete(toRemoveElems.ToList());
                                    // 创建立管并连接
                                    var newSPConn = MEPAnalysisExtension.GetConnectors(newSprinkler).FirstOrDefault();
                                    Pipe pipe1 = Pipe.Create(Document, pipeTypeId, pipeLevelId, MEPAnalysisExtension.GetClosestConnector(newFitting, newSPConn.Origin), newSPConn);
                                    Pipe pipe2 = Pipe.Create(Document, pipeTypeId, pipeLevelId, MEPAnalysisExtension.GetClosestConnector(newFitting, connector.Origin), connector);
                                    // 添加系统指定
                                    pipe1.MEPSystem.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).Set(systemId);
                                    pipe2.MEPSystem.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).Set(systemId);
                                    //// 统一连接横管逻辑
                                    newFitConns = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                    foreach (var item in pipeHorizontal)
                                        MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, MEPAnalysisExtension.GetClosestConnectorsTuple(newFitConns, MEPAnalysisExtension.GetConnectors(item).ToList()).Item1);
                                }
                                else // 单喷头方向互换
                                {
                                    if (connector.Owner.Id != pipe.Id) toRemoveElems.Add(connector.Owner.Id);
                                    toRemoveElems.Add(sp.Id);
                                    toRemoveElems.Remove(pipe.Id); // 确保立管不被删
                                    FamilyInstance newSprinklerElbow = CreateNewSprinkler(sp, targetFitting);
                                    Connector sprinklerConn = MEPAnalysisExtension.GetConnectors(newSprinklerElbow).FirstOrDefault();
                                    Connector pipeConn = MEPAnalysisExtension.GetClosestConnector(pipe, newSprinklerElbow);
                                    if (sprinklerConn != null && pipeConn != null)
                                    {
                                        MEPAnalysisExtension.DisconnectConnector(pipeConn);
                                        ElementTransformUtils.MoveElement(Document, pipe.Id, sprinklerConn.Origin - pipeConn.Origin);
                                        if (!pipeConn.IsConnected && !sprinklerConn.IsConnected) pipeConn.ConnectTo(sprinklerConn);
                                    }
                                    Document.Delete(toRemoveElems.ToList());
                                    MEPAnalysisExtension.NewElbowBy2MEPCurve(pipe, pipeHorizontal.FirstOrDefault());
                                }
                                break;
                        }
                    });
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // 用户按 ESC 正常退出循环
                    return;
                }
                catch (Exception)
                {
                    throw;
                }
            });
        }
        //获取与喷头相连的关键转折件，关联 FindTargetFittingRecursive
        private FamilyInstance GetTargetFitting(FamilyInstance sprinkler, Pipe pipe, Connector refConnector)
        {
            // 1. 基础拦截：增加对 refConnector.Owner 的非空校验
            if (sprinkler == null || pipe == null || refConnector?.Owner == null) return null;
            // 遍历管道上的所有连接器
            foreach (Connector pipeConn in pipe.ConnectorManager.Connectors.OfType<Connector>())
            {
                // 2. 核心拦截逻辑优化：精准跳过“靠近起点(喷头)”的那一端
                // 场景 A：传入的 refConnector 就在这根 Pipe 上
                if (refConnector.Owner.Id == pipe.Id && pipeConn.Id == refConnector.Id) continue;
                // 场景 B：传入的 refConnector 在喷头或相邻管件上，当前管道连接器正连着它
                if (pipeConn.IsConnected && pipeConn.AllRefs.OfType<Connector>()
                    .Any(c => c.Owner.Id == refConnector.Owner.Id && c.Id == refConnector.Id)) continue;
                // 走到这里，说明当前的 pipeConn 绝对是“远端”连接器
                foreach (Connector connectedConn in pipeConn.AllRefs.OfType<Connector>())
                {
                    // 过滤逻辑连接及管道自身
                    if (connectedConn.ConnectorType == ConnectorType.Logical ||
                        connectedConn.Owner == null ||
                        connectedConn.Owner.Id == pipe.Id) continue;

                    // 递归查找目标管件
                    FamilyInstance targetFitting = FindTargetFittingRecursive(connectedConn.Owner, sprinkler, refConnector.Owner, connectedConn);
                    if (targetFitting != null) return targetFitting;
                }
            }
            return null;
        }
        //递归查找目标管件（穿透变径/束节）
        private FamilyInstance FindTargetFittingRecursive(Element currentElement, FamilyInstance sprinkler, Element refOwner, Connector entryConnector)
        {
            // 情况 1：当前元素是管道，继续沿管道查找
            if (currentElement is Pipe nextPipe) return GetTargetFitting(sprinkler, nextPipe, entryConnector);
            // 情况 2：当前元素是管件
            if (currentElement is FamilyInstance fitting)
            {
                // 排除喷头本身和参考管件
                if (fitting.Id == sprinkler.Id || fitting.Id == refOwner.Id) return null;
                // 排除直接连接喷头或参考管件（仅当参考是管件时）的管件
                // 【关键修复】：加上 is FamilyInstance
                var fittingConnectors = MEPAnalysisExtension.GetConnectors(fitting);
                bool isDirectConnection = fittingConnectors.Any(conn =>
                    conn.AllRefs.OfType<Connector>().Any(refConn =>
                        refConn.ConnectorType != ConnectorType.Logical &&
                        (refConn.Owner?.Id == sprinkler.Id ||
                         (refOwner is FamilyInstance && refConn.Owner?.Id == refOwner.Id))));
                if (isDirectConnection) return null;
                // 判断是否为直通管件（变径/束节）
                if (MEPAnalysisExtension.IsStraightThroughFitting(fitting))
                {
                    // 穿透直通管件，无有效连接继续查找
                    Connector exitConnector = MEPAnalysisExtension.GetOppositeConnector(fitting, entryConnector);
                    if (exitConnector != null && exitConnector.IsConnected)
                    {
                        foreach (Connector nextConn in exitConnector.AllRefs.OfType<Connector>())
                        {
                            if (nextConn.ConnectorType == ConnectorType.Logical ||
                                nextConn.Owner == null || nextConn.Owner.Id == fitting.Id) continue;
                            FamilyInstance result = FindTargetFittingRecursive(nextConn.Owner, sprinkler, refOwner, nextConn);
                            if (result != null) return result;
                        }
                    }
                    return null;
                }
                // 找到目标管件（非直通管件）
                return fitting;
            }
            return null;
        }
        //根据原喷头和方向距离生成反向新喷头
        private FamilyInstance CreateNewSprinkler(FamilyInstance sp, FamilyInstance targetFitting)
        {
            // 1. 获取连接器并判断当前方向
            var connector = MEPAnalysisExtension.GetConnectors(sp).FirstOrDefault();
            if (connector == null) return null;
            double connZ = connector.CoordinateSystem.BasisZ.Z;
            // Z为正说明连接器向上（喷头本体向下，属于下垂型）为负说明连接器向下
            bool isDown = Math.Abs(connZ - 1) < 0.001;
            bool isUp = Math.Abs(connZ + 1) < 0.001;
            if (!isDown && !isUp)
            {
                TaskDialog.Show("警告", "原喷头方向存在异常（既非正上也非正下），无法处理！"); return null;
            }
            // 2. 【核心前置】先寻找对面方向的喷头族，找不到则直接退出，不产生冗余数据
            FamilySymbol oppositeSymbol = GetOppositeSprinklerSymbol(Document, sp.Symbol, isDown);
            if (oppositeSymbol == null) return null;
            // 3. 几何计算：新喷头Z坐标就是围绕 targetFitting 中心作Z轴对称镜像
            XYZ spOrigin = ((LocationPoint)sp.Location).Point;
            XYZ fitOrigin = ((LocationPoint)targetFitting.Location).Point;
            // 翻转后新Z：fitOrigin.Z - (spOrigin.Z - fitOrigin.Z) = 2 * fitOrigin.Z - spOrigin.Z
            double newZ = 2 * fitOrigin.Z - spOrigin.Z;
            XYZ moveVector = new XYZ(0, 0, newZ - spOrigin.Z);
            // 4. 复制生成新喷头
            var newSprinklerIds = ElementTransformUtils.CopyElement(Document, sp.Id, moveVector);
            FamilyInstance newSprinkler = (FamilyInstance)Document.GetElement(newSprinklerIds.First());
            // 5. 替换为正确的喷头族
            if (newSprinkler.Symbol.Id != oppositeSymbol.Id)
            {
                newSprinkler.ChangeTypeId(oppositeSymbol.Id);
            }
            return newSprinkler;
        }
        //在管件原位建立管道默认的新管件样式并指定初始管径 
        private FamilyInstance CreateNewFitting(FamilyInstance targetFitting, RoutingPreferenceManager routingManager, double pipeDiameter, int fittingType)
        {
            // 1. 确定管件的布管系统类型 (2=弯头, 3=三通, 其它=四通)
            RoutingPreferenceRuleGroupType groupType;
            switch (fittingType)
            {
                case 2: groupType = RoutingPreferenceRuleGroupType.Elbows; break;
                case 3: groupType = RoutingPreferenceRuleGroupType.Junctions; break;
                default: groupType = RoutingPreferenceRuleGroupType.Crosses; break;
            }
            // 【关键修复】防止布管系统中未配置对应管件而导致直接崩溃
            if (routingManager.GetNumberOfRules(groupType) == 0)
            {
                TaskDialog.Show("警告", $"管道系统中未配置对应的布管管件，无法自动生成！");
                return null;
            }
            // 2. 统一获取对应的管件族类型
            ElementId mepPartId = routingManager.GetRule(groupType, 0).MEPPartId;
            FamilySymbol familySymbol = (FamilySymbol)Document.GetElement(mepPartId);
            if (familySymbol == null) return null;
            // 3. 【关键】确保族类型已激活，防止第一次放置该族时报错
            if (!familySymbol.IsActive)
            {
                familySymbol.Activate();
                Document.Regenerate(); // 激活后建议重新生成一下文档
            }
            // 4. 生成新管件
            XYZ locationPoint = ((LocationPoint)targetFitting.Location).Point;
            Level hostLevel = (Level)Document.GetElement(targetFitting.LevelId);
            FamilyInstance newFitting = Document.Create.NewFamilyInstance(locationPoint, familySymbol, targetFitting.Host, hostLevel, StructuralType.NonStructural);
            //// 5. 【新增要求】立即为新生成的管件指定主管径
            //// 获取管道的直径参数
            //double pipeDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            //// 获取管件所有连接器
            var connectors = newFitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
            foreach (Connector conn in connectors)
            {
                conn.Radius = pipeDiameter / 2;
            }
            return newFitting;
        }
        //找对向喷头
        private FamilyInstance GetOppositeSprinkler(FamilyInstance sp, FamilyInstance targetFitting)
        {
            // 1. 基础校验与获取原喷头数据 (使用模式匹配一行搞定类型转换)
            if (targetFitting == null || !(sp.Location is LocationPoint spLoc)) return null;
            var spConn = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault(c => c.IsConnected);
            if (spConn == null) return null;
            XYZ spPos = spLoc.Point;
            XYZ spDir = spConn.CoordinateSystem.BasisZ;
            // 2. 获取文档中的喷头，并使用 LINQ 直接查找符合条件的第一个对象
            return new FilteredElementCollector(sp.Document).OfCategory(BuiltInCategory.OST_Sprinklers)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .FirstOrDefault(other =>
                {
                    // 步骤 2.1：排除自身
                    if (other.Id == sp.Id) return false;
                    // 步骤 2.2：检查 XY 坐标是否一致 (最轻量计算)
                    if (!(other.Location is LocationPoint otherLoc) ||
                        Math.Abs(spPos.X - otherLoc.Point.X) > 1e-5 ||
                        Math.Abs(spPos.Y - otherLoc.Point.Y) > 1e-5) return false;
                    // 步骤 2.3：检查连接器及方向相反 (中等计算)
                    var otherConn = other.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault(c => c.IsConnected);
                    if (otherConn == null || !spDir.IsAlmostEqualTo(-otherConn.CoordinateSystem.BasisZ, 1e-5)) return false;
                    // 步骤 2.4：检查是否连接到同一个目标管件 (最重计算)
                    Pipe otherPipe = MEPAnalysisExtension.GetVerticalMEPCurve(other) as Pipe;
                    Connector otherRefCo = MEPAnalysisExtension.GetClosestConnector(other, otherConn.Origin);
                    return otherPipe != null && GetTargetFitting(other, otherPipe, otherRefCo)?.Id == targetFitting.Id;
                });
        }
        //智能寻找对侧方向的喷头族类型 (优先找模型已有实例，其次文字匹配，兜底提示)
        private FamilySymbol GetOppositeSprinklerSymbol(Document doc, FamilySymbol currentSymbol, bool isCurrentlyDown)
        {
            // 目标连接器的方向：当前是下垂(Z=1)，目标找直立(Z=-1)；当前是直立(Z=-1)，目标找下垂(Z=1)
            double targetConnZ = isCurrentlyDown ? -1.0 : 1.0;
            string targetTypeName = isCurrentlyDown ? "上喷/直立型" : "下喷/下垂型";
            // 优先级 1：直接扫描文档中已放置的喷头实例，检查其真实连接器方向
            var allSpInstances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Sprinklers)
                .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Cast<FamilyInstance>();
            foreach (var spInst in allSpInstances)
            {
                var conn = spInst.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                if (conn != null)
                {
                    // 【关键修复】将精度从 0.001 放宽至 0.05，避免因为轻微坡度或构件安装误差导致匹配失败
                    if (Math.Abs(conn.CoordinateSystem.BasisZ.Z - targetConnZ) < 0.05)
                    {
                        return spInst.Symbol;
                    }
                }
            }
            // 优先级 2：如果模型中没放置过该方向喷头，则遍历后台已加载的族进行文字匹配
            string[] upKeywords = new[] { "上", "直立", "upright", "up" };
            string[] downKeywords = new[] { "下", "下垂", "pendent", "down" };
            string[] targetKeywords = isCurrentlyDown ? upKeywords : downKeywords;
            var allSymbols = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Sprinklers)
                .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().ToList();
            // 2.1 优先找同一个族下的反向类型
            var matchedSameFamily = allSymbols.FirstOrDefault(s =>
                s.FamilyName == currentSymbol.FamilyName &&
                targetKeywords.Any(k => s.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
            if (matchedSameFamily != null) return matchedSameFamily;
            // 2.2 其次在所有喷头族中找名字带反向关键字的
            var matchedAny = allSymbols.FirstOrDefault(s =>
                targetKeywords.Any(k => s.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        s.FamilyName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
            if (matchedAny != null) return matchedAny;
            // 优先级 3：兜底防线。找不到时不瞎猜，明确提醒并阻止生成
            TaskDialog.Show("tt", $"可能缺少喷头族，自动匹配失败！\n\n" + $"未在当前项目中找到【{targetTypeName}】喷头。\n" +
                $"请确保您已经载入该类型的喷头族，或者在模型中手动放置过至少一个该方向的喷头，然后再运行此功能。");
            return null;
        }
        //对齐管件到水平管道方向，再绕旋转轴做90°垂直旋转
        private void RotateFittingToAlignVertical(FamilyInstance fitting, List<Pipe> pipeHorizontal, FamilyInstance targetFitting, double rotateDegree)
        {
            Document doc = fitting.Document;
            // 1. 将角度转换为弧度：弧度 = 角度 * (π / 180)
            double rotationRadians = rotateDegree * (Math.PI / 180.0);
            var pipeCurve = (pipeHorizontal.FirstOrDefault().Location as LocationCurve)?.Curve as Line;
            XYZ targetCenter = (targetFitting.Location as LocationPoint).Point;
            var ft1Count = MEPAnalysisExtension.GetConnectors(targetFitting).ToList().Count;
            var ft1 = MEPAnalysisExtension.GetConnectors(targetFitting).ToList();
            List<Connector> horizontalConnectors = ft1.Where(c => MEPAnalysisExtension.IsHorizontalConnector(c)).ToList();
            Line rotationAxis;
            if (horizontalConnectors.Count == 2)
            {
                rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
            }
            else
                rotationAxis = pipeCurve;
            // Z轴旋转轴（过原管件中心，竖直向上）构建过指定点、沿Z轴方向的直线（用于水平面旋转）
            Line zAxis = Line.CreateBound(targetCenter, targetCenter + XYZ.BasisZ);
            // ── 场景B（三通：侧口对齐管道端口方向）──
            Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(fitting);
            Connector pipeOpenEnd = pipeHorizontal.FirstOrDefault().ConnectorManager.Connectors.OfType<Connector>()
                .OrderBy(c => c.Origin.DistanceTo(targetCenter)).FirstOrDefault();
            if (sideConn != null && pipeOpenEnd != null)
            {
                // 【关键修复】将两个向量都投影到 XY 平面，避免含有 Z 分量导致三维角度误差
                XYZ currentDir = new XYZ(sideConn.CoordinateSystem.BasisZ.X, sideConn.CoordinateSystem.BasisZ.Y, 0);
                XYZ targetDir = new XYZ(-pipeOpenEnd.CoordinateSystem.BasisZ.X, -pipeOpenEnd.CoordinateSystem.BasisZ.Y, 0);
                // 确保投影后向量有意义
                if (!currentDir.IsAlmostEqualTo(XYZ.Zero) && !targetDir.IsAlmostEqualTo(XYZ.Zero))
                {
                    currentDir = currentDir.Normalize();
                    targetDir = targetDir.Normalize();
                    double angle = currentDir.AngleTo(targetDir);
                    XYZ cross = currentDir.CrossProduct(targetDir);
                    if (cross.Z < 0) angle = -angle;
                    if (Math.Abs(angle) > 0.001)
                    {
                        ElementTransformUtils.RotateElement(doc, fitting.Id, zAxis, angle);
                    }
                }
            }
            // ── 3. 绕管道轴垂直旋转 90° ──
            ElementTransformUtils.RotateElement(doc, fitting.Id, rotationAxis, rotationRadians);
        }
        public List<ElementId> AllUpSprinkler { get; set; } = new List<ElementId>();
        public List<ElementId> AllDownSprinkler { get; set; } = new List<ElementId>();
        public int ConnectedDoubleSprinklerCount { get; set; } = 0;
        public int ConnectedDownSprinklerCount { get; set; } = 0;
        public int ConnectedUpSprinklerCount { get; set; } = 0;
        public int AllDownSprinklerCount { get; set; } = 0;
        public int AllUpSprinklerCount { get; set; } = 0;
        public int AllSprinklerCount { get; set; } = 0;
        public Family SelectedDownSp { get; set; }
        public Family SelectedUpSp { get; set; }
        private int GetFittingCategory(FamilyInstance familyInstance)
        {
            var ft1 = familyInstance.MEPModel.ConnectorManager.Connectors?.OfType<Connector>().ToList();
            switch (ft1.Count())
            {
                case 2:
                    return 2;
                case 3:
                    return 3;
                case 4:
                    return 4;
                default:
                    return 0;
            }
        }
        public HashSet<Family> DownSprinklerType { get; set; } = new HashSet<Family>(new FamilyComparer());
        public HashSet<Family> UpSprinklerType { get; set; } = new HashSet<Family>(new FamilyComparer());
        List<ElementId> ConnectedDoubleUpSprinkler = new List<ElementId>();
        List<ElementId> ConnectedUpSprinkler = new List<ElementId>();
        List<ElementId> ConnectedDoubleDownSprinkler = new List<ElementId>();
        List<ElementId> ConnectedDownSprinkler = new List<ElementId>();
        public ICommand SelectUnconnctedUpCommand => new BaseBindingCommand(SelectUnconnctedUp);
        private void SelectUnconnctedUp(object obj)
        {
            Selection select = uIDocument.Selection;
            List<ElementId> unConnectedSp = new List<ElementId>();
            foreach (var elemId in AllUpSprinkler)
            {
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elemId);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                if (!connector.IsConnected) unConnectedSp.Add(elemId);
            }
            select.SetElementIds(unConnectedSp);
        }
        public ICommand SelectUnconnctedDownCommand => new BaseBindingCommand(SelectUnconnctedDown);
        private void SelectUnconnctedDown(object obj)
        {
            Selection select = uIDocument.Selection;
            List<ElementId> unConnectedDown = new List<ElementId>();
            foreach (var elemId in AllDownSprinkler)
            {
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elemId);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                if (!connector.IsConnected) unConnectedDown.Add(elemId);
            }
            select.SetElementIds(unConnectedDown);
        }
    }
}

