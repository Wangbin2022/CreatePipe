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

                    // ── 1. 校验连接器并用容差判断方向（修复ToString隐患）─────────────────────────────────────
                    var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    if (connector == null || !connector.IsConnected) return;

                    double zValue = connector.CoordinateSystem.BasisZ.Z;
                    // Z == 1 (朝上) -> 下喷 ; Z == -1 (朝下) -> 上喷 (使用 0.01 精度容差)
                    bool isDown = Math.Abs(zValue - 1) < 0.01;
                    bool isUp = Math.Abs(zValue - (-1)) < 0.01;

                    //// ── 2. 单喷转向：方向已一致则无需操作 ───────────────────
                    //if (mode == SprinklerConvertMode.ToUp && isUp) return;
                    //if (mode == SprinklerConvertMode.ToDown && isDown) return;
                    //if (mode != SprinklerConvertMode.ToDouble && !isDown && !isUp) return;

                    // ── 3. 开始事务 ──────────────────────────────────────
                    using (Transaction tx = new Transaction(Document, "喷头方向转换"))
                    {
                        tx.Start();

                        // 找立管和水平转接管件
                        var (pipe, refCo) = GetConnectedPipe(sp);
                        FamilyInstance targetFitting = GetTargetFitting(sp, pipe, refCo);
                        ElementId pipeSystemId = pipe.MEPSystem.GetTypeId();
                        double pipeDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                        if (pipe == null || targetFitting == null) return;

                        var ft1 = MEPAnalysisExtension.GetConnectors(targetFitting).ToList();

                        List<Pipe> pipeHorizontal = new List<Pipe>();
                        List<ElementId> oldConnectFittings = new List<ElementId>();
                        (oldConnectFittings, pipeHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalPipes(connector);
                        if (pipeHorizontal.Count == 0) return;

                        // 水平连接器对 (使用容差提取)
                        List<Connector> horizontalConnectors = ft1.Where(c => MEPAnalysisExtension.IsHorizontalConnector(c)).ToList();

                        var toRemoveElems = new HashSet<ElementId>();
                        Line rotationAxis;
                        FamilyInstance newFitting;

                        // ── 5. 按管件连接数分支 ───────────────────────────
                        switch (ft1.Count)
                        {
                            // 四通
                            case 4:
                                if (mode == SprinklerConvertMode.ToDouble) return; // 已经是四通，无需处理
                                if (horizontalConnectors.Count != 2) return;
                                if (!isUp && !isDown) return; // 既不是上喷也不是下喷，直接退出（等同于原逻辑的 else return）

                                bool isToUp = mode == SprinklerConvertMode.ToUp;
                                bool isToDown = mode == SprinklerConvertMode.ToDown;

                                // 核心差异：如果想转上但点的是下喷，或者想转下但点的是上喷，就需要获取对侧喷头
                                if ((isToUp && isDown) || (isToDown && isUp))
                                {
                                    sp = GetOppositeSprinkler(sp);
                                    connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                                }

                                // ============ 以下为提取出的公共执行逻辑 ============

                                rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
                                newFitting = CreateFitting(targetFitting, pipe, 3); // 3代表三通
                                MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis, 90);
                                RotateFittingVertical(newFitting, pipeHorizontal, isToDown);

                                // 记录旧四通横管较小管径（使用 LINQ 简化遍历查找）
                                var minRadius = MEPAnalysisExtension.GetConnectors(targetFitting)
                                    .Where(c => c.ConnectorType != ConnectorType.Logical && MEPAnalysisExtension.IsHorizontalConnector(c))
                                    .Select(c => c.Radius)
                                    .DefaultIfEmpty(double.MaxValue)
                                    .Min();

                                // 设置为新三通横管管径
                                if (minRadius != double.MaxValue && newFitting != null)
                                {
                                    foreach (var item in MEPAnalysisExtension.GetConnectors(newFitting)
                                        .Where(c => c.ConnectorType != ConnectorType.Logical && MEPAnalysisExtension.IsHorizontalConnector(c)))
                                    {
                                        item.Radius = minRadius;
                                    }
                                }

                                // 收集旧构件并删除
                                (oldConnectFittings, pipeHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalPipes(connector);
                                toRemoveElems.UnionWith(oldConnectFittings);
                                Document.Delete(toRemoveElems.ToList());

                                // 连接三通与横管
                                var connectors1 = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                foreach (var item in pipeHorizontal)
                                {
                                    var connectors2 = MEPAnalysisExtension.GetConnectors(item).ToList();
                                    var (connFit, connPipe) = MEPAnalysisExtension.GetClosestConnectorsTuple(connectors1, connectors2);
                                    MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, connFit);
                                }

                                // 连接三通异端口与喷头，生成立管
                                var sideConn = MEPAnalysisExtension.GetTeeSideConn(newFitting);
                                var firstPipe = pipeHorizontal.FirstOrDefault();
                                if (firstPipe != null)
                                {
                                    Pipe verticalPipe = Pipe.Create(Document, firstPipe.PipeType.Id, firstPipe.ReferenceLevel.Id, sideConn, connector);
                                    verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipeDiameter);
                                }

                                break;
                            // 三通
                            case 3:
                                //判断三通方向，
                                if (horizontalConnectors.Count() == 2)
                                {
                                    //获取三通的水平连接器对,创建旋转轴
                                    rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
                                    //单上或单下喷头改上下喷
                                    if (mode == SprinklerConvertMode.ToDouble)
                                    {
                                        newFitting = CreateFitting(targetFitting, pipe, 4);
                                        RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis, 90);
                                        //按方向建立新喷头并连接
                                        FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                                        MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                        NewSprinklerConnect(pipe, newFitting, newSprinkler,
                                            ((LocationPoint)targetFitting.Location).Point);
                                        ////转移连接并同步尺寸，先删除原有多余接口   
                                        toRemoveElems.UnionWith(oldConnectFittings);
                                        //toRemoveElems.Remove(pipe.Id);
                                        Document.Delete(toRemoveElems.ToList());
                                        //连接三通与横管
                                        connectors1 = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                        foreach (var item in pipeHorizontal)
                                        {
                                            var connectors2 = MEPAnalysisExtension.GetConnectors(item).ToList();
                                            var (connFit, connPipe) = MEPAnalysisExtension.GetClosestConnectorsTuple(connectors1, connectors2);
                                            MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, connFit);
                                        }
                                        var (connFitOld, connSp) = MEPAnalysisExtension.GetClosestConnectorsTuple(connectors1, new List<Connector>(MEPAnalysisExtension.GetConnectors(sp)));
                                        Document.Regenerate();
                                        MEPAnalysisExtension.NewPipeBetweenConnectors(Document, connFitOld, connSp, pipeHorizontal.FirstOrDefault().GetTypeId(), pipeHorizontal.FirstOrDefault().get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId(), pipeSystemId, pipeDiameter);
                                    }
                                    //换为单上或单下喷头
                                    else
                                    {
                                        newFitting = CreateFitting(targetFitting, pipe, 3);
                                        // 2. 获取原三通（targetFitting）的非直通连接器，并计算它的“相反方向”作为目标方向
                                        var origSideConn = MEPAnalysisExtension.GetTeeSideConn(targetFitting);
                                        XYZ origSideDir = origSideConn.CoordinateSystem.BasisZ.Normalize();
                                        // 原分支方向目标方向相反
                                        XYZ targetSideDir = -origSideDir;
                                        var initialNewSideConn = MEPAnalysisExtension.GetTeeSideConn(newFitting);
                                        XYZ currentSideDir = initialNewSideConn.CoordinateSystem.BasisZ.Normalize();
                                        // 4. 提取旋转轴的向量方向 (前提：入参 rotationAxis 是一个通过主管中心的 Line 对象)
                                        XYZ axisDir = rotationAxis.Direction.Normalize();
                                        double rotateAngle = currentSideDir.AngleOnPlaneTo(targetSideDir, axisDir);
                                        // 6. 执行旋转（如果角度差大于 0.001 弧度才旋转，避免微小误差报错）
                                        if (Math.Abs(rotateAngle) > 0.001)
                                        {
                                            ElementTransformUtils.RotateElement(Document, newFitting.Id, rotationAxis, rotateAngle);
                                        }
                                        // 7. 【重要】由于管件发生过旋转，原来的 Connector 对象坐标可能未刷新，必须重新获取！
                                        sideConn = MEPAnalysisExtension.GetTeeSideConn(newFitting);
                                        //按方向建立新喷头并连接
                                        FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                                        MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                        NewSprinklerConnect(pipe, newFitting, newSprinkler,
                                            ((LocationPoint)targetFitting.Location).Point);
                                        //////转移连接并同步尺寸，先删除原有多余接口
                                        toRemoveElems.UnionWith(oldConnectFittings);
                                        toRemoveElems.Add(sp.Id);
                                        Document.Delete(toRemoveElems.ToList());
                                        Document.Regenerate();
                                        connectors1 = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                        foreach (var item in pipeHorizontal)
                                        {

                                            var connectors2 = MEPAnalysisExtension.GetConnectors(item).ToList();
                                            var (connFit, connPipe) = MEPAnalysisExtension.GetClosestConnectorsTuple(connectors1, connectors2);
                                            MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, connFit);
                                        }
                                    }
                                }
                                else if (horizontalConnectors.Count() == 1)
                                {
                                    // 端头上下立管仅保留一方
                                    if (mode == SprinklerConvertMode.ToDouble) return;
                                    // 1. 获取对侧喷头信息
                                    FamilyInstance oppoSp = GetOppositeSprinkler(sp);
                                    if (oppoSp == null) return;
                                    var oppoConnector = oppoSp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                                    if (oppoConnector == null) return;
                                    // 2. 获取横管及中间连接件
                                    (oldConnectFittings, pipeHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalPipes(oppoConnector);
                                    var targetHorizontalPipe = pipeHorizontal.FirstOrDefault();
                                    if (targetHorizontalPipe == null) return;
                                    // 3. 判断逻辑：我们要改成上喷还是下喷？当前点选的喷头是否与目标一致？
                                    bool keepUp = mode == SprinklerConvertMode.ToUp;
                                    bool isKeepingCurrentSp = (keepUp && isUp) || (!keepUp && isDown);
                                    // 4. 根据判断结果分配要操作的元素
                                    if (isKeepingCurrentSp)
                                    {
                                        // 保留当前 sp，删除对侧 oppoSp
                                        toRemoveElems.Add(oppoSp.Id);
                                    }
                                    else
                                    {
                                        // 删除当前 sp，保留对侧 oppoSp。因此执行后续操作的立管需要切换为 oppoSp 的立管
                                        var connectedResult = GetConnectedPipe(oppoSp);
                                        pipe = connectedResult.Item1;
                                    }
                                    // 5. 整理删除列表：加入旧管件，排除需要保留的立管
                                    toRemoveElems.UnionWith(oldConnectFittings);
                                    var keepIds = FindVerticalElementsToRemove(pipe, keepUp);
                                    toRemoveElems.ExceptWith(keepIds);
                                    // 6. 执行删除并创建弯头
                                    Document.Delete(toRemoveElems);
                                    MEPAnalysisExtension.NewElbowBy2MEPCurve(pipe, targetHorizontalPipe);
                                }
                                break;
                            // 弯头
                            case 2:
                                if (mode == SprinklerConvertMode.ToDouble)
                                {
                                    var curve = ((LocationCurve)pipeHorizontal.FirstOrDefault().Location).Curve;
                                    rotationAxis = Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(1));
                                    newFitting = CreateFitting(targetFitting, pipe, 3);
                                    MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
                                    RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis, 90);
                                    FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                                    var closeFitConn = MEPAnalysisExtension.GetClosestConnector(pipe, refCo.Origin);
                                    var farFitConn = MEPAnalysisExtension.GetOppositeConnector(pipe, closeFitConn.Origin);
                                    (oldConnectFittings, pipeHorizontal) = MEPAnalysisExtension.GetAllConnectedElementsAndStopByHorizontalPipes(farFitConn);
                                    toRemoveElems.UnionWith(oldConnectFittings);
                                    Document.Delete(toRemoveElems.ToList());
                                    //新立管连接喷头与三通                                    
                                    var newSPConn = MEPAnalysisExtension.GetConnectors(newSprinkler).FirstOrDefault();
                                    var fitCloseConn = MEPAnalysisExtension.GetClosestConnector(newFitting, newSPConn.Origin);
                                    Pipe verticalPipe = Pipe.Create(Document, pipe.PipeType.Id, pipe.ReferenceLevel.Id, fitCloseConn, newSPConn);
                                    //连接旧立管与三通
                                    var fitCloseConn2 = MEPAnalysisExtension.GetClosestConnector(newFitting, closeFitConn.Origin);
                                    MEPAnalysisExtension.ConnectMEPCurve2FittingConn(pipe, fitCloseConn2);
                                    //连接三通与横管
                                    connectors1 = MEPAnalysisExtension.GetConnectors(newFitting).ToList();
                                    foreach (var item in pipeHorizontal)
                                    {
                                        var connectors2 = MEPAnalysisExtension.GetConnectors(item).ToList();
                                        var (connFit, connPipe) = MEPAnalysisExtension.GetClosestConnectorsTuple(connectors1, connectors2);
                                        MEPAnalysisExtension.ConnectMEPCurve2FittingConn(item, connFit);
                                    }
                                }
                                else
                                {
                                    if (refCo.Owner.Id != pipe.Id) toRemoveElems.Add(refCo.Owner.Id);
                                    FamilyInstance newSprinklerElbow = NewSprinklerMethod(sp, targetFitting);
                                    Connector sprinklerConn = MEPAnalysisExtension.GetConnectors(newSprinklerElbow).FirstOrDefault();
                                    if (sprinklerConn == null) return;
                                    Connector pipeConn = MEPAnalysisExtension.GetClosestConnector(pipe, newSprinklerElbow);
                                    MEPAnalysisExtension.DisconnectConnector(pipeConn);
                                    XYZ offset = sprinklerConn.Origin - pipeConn.Origin;
                                    ElementTransformUtils.MoveElement(Document, pipe.Id, offset);
                                    if (!pipeConn.IsConnected && !sprinklerConn.IsConnected)
                                        pipeConn.ConnectTo(sprinklerConn);
                                    ////删除原变径,喷头，保留立管
                                    toRemoveElems.UnionWith(oldConnectFittings);
                                    toRemoveElems.Remove(pipe.Id);
                                    toRemoveElems.Add(sp.Id);
                                    Document.Delete(toRemoveElems.ToList());
                                    MEPAnalysisExtension.NewElbowBy2MEPCurve(pipe, pipeHorizontal.FirstOrDefault());
                                }
                                break;
                        }
                        tx.Commit();
                    }
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
        private FamilyInstance GetOppositeSprinkler(FamilyInstance sp)
        {
            var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
            if (connector == null || !connector.IsConnected) return null;

            // 1. 获取原喷头的连接器方向、立管和转接管件
            var (pipe, refCo) = GetConnectedPipe(sp);
            if (pipe == null) return null;

            FamilyInstance targetFitting = GetTargetFitting(sp, pipe, refCo);
            if (targetFitting == null) return null;

            // 获取原喷头的坐标和方向
            LocationPoint spLocPoint = sp.Location as LocationPoint;
            if (spLocPoint == null) return null;
            XYZ spPos = spLocPoint.Point;
            XYZ spDirection = connector.CoordinateSystem.BasisZ.Normalize();

            Document doc = sp.Document;

            // 2. 收集文档中所有的喷头（排除自身）
            var allSprinklers = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sprinklers)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Id != sp.Id);

            // 3. 遍历检查符合条件的喷头
            foreach (var otherSp in allSprinklers)
            {
                // 步骤 3.1：检查 XY 坐标是否一致（Revit 容差通常取 1e-5）
                LocationPoint otherLocPoint = otherSp.Location as LocationPoint;
                if (otherLocPoint == null) continue;
                XYZ otherPos = otherLocPoint.Point;

                if (Math.Abs(spPos.X - otherPos.X) > 1e-5 ||
                    Math.Abs(spPos.Y - otherPos.Y) > 1e-5)
                {
                    continue; // XY 位置不同，跳过
                }

                // 步骤 3.2：检查连接器方向是否相反
                var otherConnector = otherSp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                if (otherConnector == null || !otherConnector.IsConnected) continue;

                XYZ otherDirection = otherConnector.CoordinateSystem.BasisZ.Normalize();
                // 原方向 与 目标反方向 应该近乎相等
                if (!spDirection.IsAlmostEqualTo(-otherDirection, 1e-5))
                {
                    continue; // 方向不是完全相反，跳过
                }

                // 步骤 3.3：检查目标管件（TargetFitting）是否为同一个
                // 因为这步计算成本最高，所以放在最后验证
                var (otherPipe, otherRefCo) = GetConnectedPipe(otherSp);
                if (otherPipe == null) continue;

                FamilyInstance otherTargetFitting = GetTargetFitting(otherSp, otherPipe, otherRefCo);

                // 如果找到的目标管件ID与原喷头一致，说明这就是要找的上/下喷头
                if (otherTargetFitting != null && otherTargetFitting.Id == targetFitting.Id)
                {
                    return otherSp;
                }
            }
            // 遍历完都没找到
            return null;
        }
        //寻找管件上与目标管道连接的端口，计算对侧端口，返回对侧端口连接的管道OK
        private Pipe GetOppositePipe(FamilyInstance fitting, Pipe excludePipe)
        {
            var connectors = MEPAnalysisExtension.GetConnectors(fitting).ToList();
            if (connectors == null || connectors.Count < 2) return null;
            // 1. 找到与传入管道 (excludePipe) 相连的管件连接器
            Connector excludeConnector = connectors.FirstOrDefault(conn =>
                conn.AllRefs.OfType<Connector>().Any(refConn => refConn.Owner?.Id == excludePipe.Id));
            if (excludeConnector == null) return null;
            // 替代方案：基于连接器法向量判断对面 (方向正好相反，点积趋近于 -1)
            Connector oppositeConnector = connectors
                .Where(conn => conn.Id != excludeConnector.Id)
                .OrderBy(conn => conn.CoordinateSystem.BasisZ.DotProduct(excludeConnector.CoordinateSystem.BasisZ))
                .FirstOrDefault();
            if (oppositeConnector == null) return null;
            // 3. 获取对侧连接器上连接的其他管道（排除原本的 excludePipe）
            return oppositeConnector.AllRefs.OfType<Connector>()
                .Select(refConn => refConn.Owner as Pipe)
                .FirstOrDefault(pipe => pipe != null && pipe.Id != excludePipe.Id);
        }
        //在垂直面转管件OK
        private void RotateFittingVertical(FamilyInstance fitting, List<Pipe> pipe, bool upDown)
        {
            Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(fitting);
            if (sideConn == null) return;
            // 1. 确定目标方向
            XYZ targetDirection = upDown ? -XYZ.BasisZ : XYZ.BasisZ;
            XYZ currentDirection = sideConn.CoordinateSystem.BasisZ;
            // 2. 检查当前方向是否已经正确
            if (currentDirection.IsAlmostEqualTo(targetDirection, 0.001)) return;
            XYZ rotationAxis;
            double rotationAngle;
            // 3. 计算旋转轴和角度
            if (currentDirection.IsAlmostEqualTo(-targetDirection, 0.001))
            {
                // 【关键修复】方向相反（180度）时，必须绕水平主管道的轴线翻转！
                // 否则会导致主管两端的接口对调，破坏系统连接
                var pipeLine = (pipe.FirstOrDefault()?.Location as LocationCurve)?.Curve as Line;
                rotationAxis = pipeLine.Direction;
                // 180度
                rotationAngle = Math.PI;
            }
            else
            {
                // 90度等情况：叉积能直接得到完美符合右手定则的旋转轴（恰好也是主管道走向）
                rotationAxis = currentDirection.CrossProduct(targetDirection);
                rotationAngle = currentDirection.AngleTo(targetDirection);
            }
            // 4. 执行旋转
            XYZ fittingOrigin = ((LocationPoint)fitting.Location).Point;
            // 确保旋转轴是单位向量
            Line rotationAxisLine = Line.CreateBound(fittingOrigin, fittingOrigin + rotationAxis.Normalize());
            ElementTransformUtils.RotateElement(Document, fitting.Id, rotationAxisLine, rotationAngle);
        }
        //深度优先搜索（DFS）顺着管件/管道寻找连接的垂直元素及其末端喷头，直到找到喷头（单连接器元素）为止，并收集这条路径上的所有元素以便后续删除OK
        public HashSet<ElementId> FindVerticalElementsToRemove(Element startElement, bool searchUpward)
        {
            var toRemove = new HashSet<ElementId>();
            var visited = new HashSet<ElementId>();
            // 内部局部函数，执行递归查找逻辑 (深度优先搜索 DFS)
            bool TraverseVerticalPath(Element currentElem)
            {
                // 1. 防死循环：如果已经访问过，或者是无效元素，跳过
                if (currentElem == null || !visited.Add(currentElem.Id)) return false;
                // 2. 获取当前元素所有连接器
                var connectors = MEPAnalysisExtension.GetConnectors(currentElem).ToList();
                if (connectors.Count == 0) return false;
                // 3. 将当前元素加入待删除列表
                toRemove.Add(currentElem.Id);
                // 4. 如果只有一个连接器（通常是喷头或管帽末端），说明找到终点，终止并确认成功
                if (connectors.Count == 1) return true;
                var verticalConnectors = connectors
    .Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) > 0.5);
                // 5. 根据方向排序连接器，优先遍历目标垂直方向
                // searchUpward = true  -> 优先 +Z (向上)
                // searchUpward = false -> 优先 -Z (向下，即 Z 值从小到大排序)
                var sortedConnectors = searchUpward
                        ? verticalConnectors.OrderByDescending(c => c.CoordinateSystem.BasisZ.Z) // 优先向上找
                        : verticalConnectors.OrderBy(c => c.CoordinateSystem.BasisZ.Z);          // 优先向下找
                // 6. 顺着排序后的连接器往下找
                foreach (var conn in sortedConnectors)
                {
                    var connectedRefs = conn.AllRefs.OfType<Connector>().Where(r => r.Owner?.Id != currentElem.Id);
                    foreach (var refConn in connectedRefs)
                    {
                        // refConn.Owner 直接就是 Element，无需调用 Document.GetElement
                        if (TraverseVerticalPath(refConn.Owner))
                        {
                            // 如果在子分支中成功找到了喷头，一路返回 true，保留路径
                            return true;
                        }
                    }
                }
                // 7. 【关键修复】如果遍历了所有接口都没找到喷头，说明这是一条死分支,必须把当前元素从删除列表中移除，否则会误删平行的主管道或其他无关管件！
                toRemove.Remove(currentElem.Id);
                return false;
            }
            // 启动递归搜索
            TraverseVerticalPath(startElement);
            return toRemove;
        }
        //先断开水平连接再连接喷头，管件、管道逻辑OK
        private void NewSprinklerConnect(Pipe pipe, FamilyInstance crossFitting, FamilyInstance newSprinkler, XYZ originalCrossPosition)
        {
            if (pipe == null || crossFitting == null || newSprinkler == null) return;
            if (crossFitting.MEPModel?.ConnectorManager == null) return;
            if (newSprinkler.MEPModel?.ConnectorManager == null) return;
            Document doc = crossFitting.Document;
            // 1. 获取喷头连接器
            Connector sprinklerConn = newSprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                .FirstOrDefault(c =>
                    c.Domain == Domain.DomainPiping &&
                    c.ConnectorType != ConnectorType.Logical);
            List<Connector> fittingConns = crossFitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                .Where(c => c.Domain == Domain.DomainPiping &&
                            c.ConnectorType != ConnectorType.Logical).ToList();
            //根据喷头和管件的位置自动判断上/下方向。
            XYZ sprinklerOrigin = sprinklerConn.Origin;
            // 优先找未连接的竖向连接器
            List<Connector> verticalConns = fittingConns
                .Where(c => !c.IsConnected &&
                    Math.Abs(Math.Abs(c.CoordinateSystem.BasisZ.Normalize().Z) - 1.0) < 0.1).ToList();
            if (verticalConns.Count == 0) return;
            // 根据喷头在管件上方还是下方选择方向
            Connector crossVerticalConn = verticalConns
                .OrderBy(c => c.Origin.DistanceTo(sprinklerOrigin))
                .FirstOrDefault();
            if (crossVerticalConn == null) return;
            // 3. 记录水平连接器原始连接关系
            Dictionary<Connector, List<Connector>> originalConnData =
                new Dictionary<Connector, List<Connector>>();
            List<Connector> horizontalConns = fittingConns
                .Where(c => MEPAnalysisExtension.IsHorizontalConnector(c)).ToList();
            foreach (Connector hConn in horizontalConns)
            {
                List<Connector> connectedConns = hConn.AllRefs.OfType<Connector>()
                    .Where(c => c.Owner != null &&
                        c.Owner.Id != crossFitting.Id &&
                        c.ConnectorType != ConnectorType.Logical)
                    .ToList();
                if (connectedConns.Count == 0) continue;
                originalConnData[hConn] = connectedConns;
                foreach (Connector refConn in connectedConns)
                {
                    MEPAnalysisExtension.SafeDisconnect(hConn, refConn);
                }
            }
            // 4. 创建竖向管连接喷头
            Pipe verticalPipe = Pipe.Create(doc, pipe.PipeType.Id, pipe.ReferenceLevel.Id, crossVerticalConn, sprinklerConn);
            // 5. 设置管径
            Parameter diaParam = verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (diaParam != null && !diaParam.IsReadOnly)
            {
                diaParam.Set(pipe.Diameter);
            }
            // 6. 设置系统类型，注意该参数有时可能只读
            Parameter sourceSysParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            Parameter targetSysParam = verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            if (sourceSysParam != null &&
                targetSysParam != null &&
                !targetSysParam.IsReadOnly)
            {
                ElementId sysTypeId = sourceSysParam.AsElementId();
                if (sysTypeId != ElementId.InvalidElementId)
                {
                    targetSysParam.Set(sysTypeId);
                }
            }
            // 7. 将管件移回原始位置
            LocationPoint lp = crossFitting.Location as LocationPoint;
            if (lp != null && originalCrossPosition != null)
            {
                XYZ moveVec = originalCrossPosition - lp.Point;
                if (!moveVec.IsAlmostEqualTo(XYZ.Zero))
                {
                    ElementTransformUtils.MoveElement(doc, crossFitting.Id, moveVec);
                }
            }
            // 8. 恢复水平连接
            foreach (var kvp in originalConnData)
            {
                Connector hConn = kvp.Key;
                List<Connector> originalConnectedConns = kvp.Value;
                foreach (Connector originalConn in originalConnectedConns)
                {
                    MEPAnalysisExtension.TryConnectAndVerify(doc, hConn, originalConn);
                }
            }
            doc.Regenerate();
        }
        //根据原喷头和方向距离生成新喷头。OK
        private FamilyInstance NewSprinklerMethod(FamilyInstance sp, FamilyInstance targetFitting)
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
        /// 智能寻找对侧方向的喷头族类型 (优先找模型已有实例，其次文字匹配，兜底提示)OK
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
        /// 对齐管件到水平管道方向，再绕旋转轴做90°垂直旋转.OK
        private void RotateFittingToAlignVertical(FamilyInstance fitting, List<Pipe> pipeHorizontal,
            FamilyInstance targetFitting, Line rotationAxis, double rotateDegree, bool usePipeDirection = false)
        {
            Document doc = fitting.Document;
            // 1. 将角度转换为弧度：弧度 = 角度 * (π / 180)
            double rotationRadians = rotateDegree * (Math.PI / 180.0);
            var pipeCurve = (pipeHorizontal.FirstOrDefault().Location as LocationCurve)?.Curve as Line;
            var locPoint = targetFitting.Location as LocationPoint;
            XYZ targetCenter = locPoint.Point;
            var ft1Count = MEPAnalysisExtension.GetConnectors(targetFitting).ToList().Count;
            // Z轴旋转轴（过原管件中心，竖直向上）
            Line zAxis = BuildZAxisAtPoint(targetCenter);
            // ── 2. 水平面旋转 ──────────────────────────
            // 修复矛盾：如果意图是四通，建议检查 ft1Count >= 4 (根据你的业务逻辑调整)
            if (usePipeDirection && ft1Count == 4)
            {
                // ── 场景A（四通分支）：取管件任意一个水平连接器代表其当前方向 ──
                Connector hConn = fitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                    .FirstOrDefault(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.1);
                if (hConn != null)
                {
                    // 将当前管件方向投影到 XY 平面
                    XYZ currentDir = new XYZ(hConn.CoordinateSystem.BasisZ.X, hConn.CoordinateSystem.BasisZ.Y, 0).Normalize();
                    // 管道走向投影到 XY 平面
                    XYZ targetDir = new XYZ(pipeCurve.Direction.X, pipeCurve.Direction.Y, 0).Normalize();
                    double angle = currentDir.AngleTo(targetDir);
                    XYZ cross = currentDir.CrossProduct(targetDir);
                    if (cross.Z < 0) angle = -angle;

                    if (Math.Abs(angle) > 0.001) // 忽略极小角度
                    {
                        ElementTransformUtils.RotateElement(doc, fitting.Id, zAxis, angle);
                    }
                }
            }
            else
            {
                // ── 场景B（三通：侧口对齐管道端口方向）────────────────
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
            }
            // ── 3. 绕管道轴垂直旋转 90° ────────────
            ElementTransformUtils.RotateElement(doc, fitting.Id, rotationAxis, rotationRadians);
        }
        // 构建过指定点、沿Z轴方向的直线（用于水平面旋转）
        private Line BuildZAxisAtPoint(XYZ point)
            => Line.CreateBound(point, point + XYZ.BasisZ);       
        //获取直接喷头相连的connector和pipe。OK
        private FamilyInstance GetTargetFitting(FamilyInstance sprinkler, Pipe pipe, Connector refConnector)
        {
            if (sprinkler == null || pipe == null || refConnector == null || refConnector.Owner == null) return null;

            Element refOwner = refConnector.Owner;
            FamilyInstance refFitting = refOwner as FamilyInstance;

            // 遍历管道的所有物理连接器（通常是两端）
            foreach (Connector pipeConn in pipe.ConnectorManager.Connectors.OfType<Connector>())
            {
                // 如果 refConnector 属于这根管道，且当前遍历到的正好是 refConnector，跳过，只看远端
                if (refOwner.Id == pipe.Id && pipeConn.Id == refConnector.Id)
                    continue;

                // 遍历与管道端点相连的其他连接器
                foreach (Connector refConn in pipeConn.AllRefs.OfType<Connector>())
                {
                    // 过滤掉：逻辑连接器、没有 Owner 的野连接器、以及管道自身的连接器
                    if (refConn.ConnectorType == ConnectorType.Logical ||
                        refConn.Owner == null ||
                        refConn.Owner.Id == pipe.Id) continue;

                    // 检查目标是否是族实例 (管件/附件等)
                    if (refConn.Owner is FamilyInstance candidateFitting)
                    {
                        // 1. 排除喷头本身,参考对象本身 (例如上游管件)
                        if (candidateFitting.Id == sprinkler.Id || candidateFitting.Id == refOwner.Id) continue;

                        // 2. 排除那些“直接连接了喷头”的管件
                        if (IsDirectSprinklerFitting(candidateFitting, sprinkler, refFitting))
                            continue;

                        // 3. 【新增逻辑】：排除变径/束节等（只有两个物理连接器，且方向相反的构件）
                        if (candidateFitting.MEPModel != null && candidateFitting.MEPModel.ConnectorManager != null)
                        {
                            // 获取所有的非逻辑物理连接器
                            var candidateConns = candidateFitting.MEPModel.ConnectorManager.Connectors
                                .OfType<Connector>()
                                .Where(c => c.ConnectorType != ConnectorType.Logical)
                                .ToList();

                            // 如果只有两个连接器，检查它们的方向
                            if (candidateConns.Count == 2)
                            {
                                XYZ dir1 = candidateConns[0].CoordinateSystem.BasisZ;
                                XYZ dir2 = candidateConns[1].CoordinateSystem.BasisZ;

                                // Revit API 提供了 IsAlmostEqualTo 用于判断向量是否相等（内置了极小容差）
                                // 如果两端连接器法向量完全相反（相加约等于0），说明是直通管件或变径
                                if (dir1.IsAlmostEqualTo(-dir2))
                                {
                                    foreach (var item in candidateConns)
                                    {
                                        // 确保连接器已经连接了其他构件
                                        if (item.IsConnected)
                                        {
                                            foreach (Connector refToOther in item.AllRefs.OfType<Connector>())
                                            {
                                                // 排除逻辑连接器
                                                if (refToOther.ConnectorType == ConnectorType.Logical) continue;

                                                // 排除原管道 (pipe) 以及变径管件自身
                                                if (refToOther.Owner.Id == pipe.Id || refToOther.Owner.Id == candidateFitting.Id)
                                                    continue;

                                                // 情况 A：变径另一头连接的是下一根管道
                                                if (refToOther.Owner is Pipe nextPipe)
                                                {
                                                    // 递归穿透：以下一根管道为起点，refToOther（管道端的连接器）作为防倒退起点继续查找
                                                    FamilyInstance found = GetTargetFitting(sprinkler, nextPipe, refToOther);
                                                    if (found != null)
                                                        return found;
                                                }
                                                // 情况 B：变径另一头直接连接了另一个管件 (例如变径直接套弯头)
                                                else if (refToOther.Owner is FamilyInstance nextFitting)
                                                {
                                                    // 排除喷头本身和最开始的参考管件
                                                    if (nextFitting.Id == sprinkler.Id || nextFitting.Id == refOwner.Id) continue;
                                                    if (IsDirectSprinklerFitting(nextFitting, sprinkler, refFitting)) continue;

                                                    // 返回找到的第一个实体管件
                                                    return nextFitting;
                                                }
                                            }
                                        }
                                    }
                                    // 如果穿过变径后是断头或者没找到，则跳过当前分支，继续寻找其他可能
                                    continue;
                                }
                            }
                        }

                        // 找到符合条件的目标管件
                        return candidateFitting;
                    }
                }
            }
            return null;
        }
        // 查找管道另一端的关键管件targetFitting和Connector.OK
        private (Pipe Pipe, Connector PipeConnector) GetConnectedPipe(FamilyInstance sp)
        {
            // 1. 获取喷头的连接器
            var startConn = MEPAnalysisExtension.GetConnectors(sp).FirstOrDefault();
            if (startConn == null) return (null, null);

            // 2. 遍历喷头连接器所引用的目标
            foreach (Connector linkedConn in startConn.AllRefs.OfType<Connector>())
            {
                // 场景 A: 喷头直接连在管道上
                // linkedConn 此时就是管道那一侧的连接器
                if (linkedConn.Owner is Pipe pipe)
                {
                    return (pipe, linkedConn);
                }

                // 场景 B: 通过管件（变径/束节等）中转
                if (linkedConn.Owner is FamilyInstance fitting)
                {
                    var fittingConns = fitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
                    if (fittingConns == null) continue;

                    foreach (Connector fConn in fittingConns)
                    {
                        // 遍历管件上每个接口连接的外部对象
                        foreach (Connector nextRef in fConn.AllRefs.OfType<Connector>())
                        {
                            // 找到了管道，且不是最初的喷头
                            if (nextRef.Owner is Pipe foundPipe && foundPipe.Id != sp.Id)
                            {
                                return (foundPipe, nextRef);
                            }
                        }
                    }
                }
            }
            // 兜底返回
            return (null, null);
        }
        //// 检查是否是喷头直接连接的管件.OK
        private bool IsDirectSprinklerFitting(FamilyInstance fitting, FamilyInstance sprinkler, FamilyInstance refFitting)
        {
            var fittingConnectors = fitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
            if (fittingConnectors == null) return false;
            foreach (var conn in fittingConnectors)
            {
                foreach (Connector refConn in conn.AllRefs.OfType<Connector>())
                {
                    // 排除逻辑连接
                    if (refConn.ConnectorType == ConnectorType.Logical) continue;
                    if (refConn.Owner?.Id == sprinkler.Id ||
                       (refFitting != null && refConn.Owner?.Id == refFitting.Id))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //在管件原位建立管道默认的新管件样式并指定初始管径 应结合管件参数优化
        private FamilyInstance CreateFitting(FamilyInstance targetFitting, Pipe pipe, int fittingType)
        {
            // 1. 确定管件的布管系统类型 (2=弯头, 3=三通, 其它=四通)
            RoutingPreferenceRuleGroupType groupType;
            switch (fittingType)
            {
                case 2: groupType = RoutingPreferenceRuleGroupType.Elbows; break;
                case 3: groupType = RoutingPreferenceRuleGroupType.Junctions; break;
                default: groupType = RoutingPreferenceRuleGroupType.Crosses; break;
            }
            var routingManager = pipe.PipeType.RoutingPreferenceManager;
            // 【关键修复】防止布管系统中未配置对应管件而导致直接崩溃
            if (routingManager.GetNumberOfRules(groupType) == 0)
            {
                TaskDialog.Show("警告", $"管道类型[{pipe.PipeType.Name}]中未配置对应的布管系统管件，无法自动生成！");
                return null;
            }
            // 2. 统一获取对应的管件族类型
            ElementId mepPartId = pipe.PipeType.RoutingPreferenceManager.GetRule(groupType, 0).MEPPartId;
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
            // 5. 【新增要求】立即为新生成的管件指定主管径
            SetFittingMainSize(newFitting, pipe);
            return newFitting;
        }
        //为未连接的管件强行写入管道尺寸参数,参数应根据场景重新确定 应优化
        private void SetFittingMainSize(FamilyInstance fitting, Pipe pipe)
        {
            // 获取管道的直径参数
            double pipeDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            // 获取管件所有连接器
            var connectors = fitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
            if (connectors == null) return;
            foreach (Connector conn in connectors)
            {
                conn.Radius = pipeDiameter / 2;
            }
        }
        //处理新旧管件接口连接关系
        //private void ProcessConnections(FamilyInstance oldFitting, FamilyInstance newFitting)
        //{
        //    Dictionary<Connector, Connector> connectorMap = new Dictionary<Connector, Connector>();
        //    foreach (Connector oldConn in oldFitting.MEPModel.ConnectorManager.Connectors)
        //    {
        //        Connector newConn = null;
        //        ConnectorSet connectors = newFitting.MEPModel.ConnectorManager.Connectors;
        //        foreach (Connector conn in connectors)
        //        {
        //            if (conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(oldConn.CoordinateSystem.BasisZ))
        //            {
        //                newConn = conn;
        //            }
        //        }
        //        if (newConn == null) return;
        //        connectorMap.Add(oldConn, newConn);
        //        // 同步连接器尺寸
        //        Parameter oldDiamParam = MEPAnalysisExtension.GetAssociatedParameter(oldFitting, oldConn, BuiltInParameter.CONNECTOR_RADIUS);
        //        if (oldDiamParam != null)
        //        {
        //            Parameter newDiamParam = MEPAnalysisExtension.GetAssociatedParameter(newFitting, newConn, BuiltInParameter.CONNECTOR_RADIUS);
        //            newDiamParam?.Set(oldDiamParam.AsDouble());
        //        }

        //    }
        //    // 8. 转移连接关系
        //    foreach (var pair in connectorMap)
        //    {
        //        ConnectorSet refs = pair.Key.AllRefs;
        //        foreach (Connector refConn in refs)
        //        {
        //            if (refConn.Owner is MEPCurve || refConn.Owner is FamilyInstance)
        //            {
        //                refConn.DisconnectFrom(pair.Key);
        //                refConn.ConnectTo(pair.Value);
        //            }
        //        }
        //    }
        //}
        public ElementId connectedSpFitting { get; set; }
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

