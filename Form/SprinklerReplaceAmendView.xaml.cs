using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

            //ExternalHandler.Run(app =>
            //{
            //    Reference r = uIDocument.Selection.PickObject(
            //        ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
            //    FamilyInstance sp = (FamilyInstance)Document.GetElement(r);
            //    // ── 2. 校验连接器 ─────────────────────────────────────
            //    var connector = sp.MEPModel?.ConnectorManager?.Connectors
            //                      ?.OfType<Connector>().FirstOrDefault();
            //    if (connector == null || !connector.IsConnected) return;
            //    // Z == 1  → 连接器朝上 → 下喷 ;   -1 → 上喷
            //    bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
            //    bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
            //    // ── 3. 单喷转向：方向已一致则无需操作 ───────────────────
            //    if (mode == SprinklerConvertMode.ToUp && isUp) return;
            //    if (mode == SprinklerConvertMode.ToDown && isDown) return;
            //    if (mode != SprinklerConvertMode.ToDouble && !isDown && !isUp) return;
            //    // ── 4. 开始事务 ──────────────────────────────────────
            //    using (Transaction tx = new Transaction(Document))
            //    {
            //        tx.Start("喷头方向转换");
            //        // ── 5. 获取关联元素 ───────────────────────────────
            //        (Pipe pipe, Connector refCo) = GetConnectedPipe(sp);
            //        FamilyInstance targetFitting = GetTargetFitting(sp, pipe, refCo);
            //        if (pipe == null || targetFitting == null) return;
            //        var ft1 = MEPAnalysisExtension.GetConnectors(targetFitting).ToList();
            //        var toRemoveElems = new List<ElementId>();
            //        Pipe pipeHorizontal = GetHorizonPipe(pipe, ft1);
            //        // 水平连接器对（用于创建旋转轴）
            //        List<Connector> horizontalConnectors = ft1.Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.01)
            //            .ToList();
            //        Line rotationAxis;
            //        FamilyInstance newFitting;
            //        // ── 6. 按管件连接数分支 ───────────────────────────
            //        switch (ft1.Count)
            //        {
            //            // 四通
            //            case 4:
            //                switch (mode)
            //                {
            //                    case SprinklerConvertMode.ToDouble:
            //                        // 已是四通 = 已是双喷，无需处理
            //                        return;
            //                    case SprinklerConvertMode.ToUp:
            //                    case SprinklerConvertMode.ToDown:
            //                        // 四通 → 三通：删除目标方向的垂直支管
            //                        if (pipeHorizontal == null) return;
            //                        if (horizontalConnectors == null || horizontalConnectors.Count != 2) return;
            //                        // ToUp → 保留上喷，删下喷(false)；ToDown → 保留下喷，删上喷(true)
            //                        toRemoveElems = FindVerticalElementsToRemove(
            //                            targetFitting, mode == SprinklerConvertMode.ToDown);
            //                        rotationAxis = Line.CreateBound(
            //                            horizontalConnectors[0].Origin,
            //                            horizontalConnectors[1].Origin);
            //                        newFitting = CreateFitting(targetFitting, pipe, 3);
            //                        MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
            //                        RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
            //                        // ToUp → 侧口朝上(false)；ToDown → 侧口朝下(true)
            //                        RotateFittingVertical(newFitting, pipeHorizontal,
            //                            mode == SprinklerConvertMode.ToDown);
            //                        //ConnectAndMatchSize(newFitting, pipeHorizontal);
            //                        //ProcessConnections(targetFitting, newFitting);
            //                        break;
            //                }
            //                break;

            //            // 三通
            //            case 3:
            //                if (pipeHorizontal == null) return;
            //                switch (mode)
            //                {
            //                    case SprinklerConvertMode.ToDouble:
            //                        // 三通 → 四通
            //                        var hConnsByZ = ft1.GroupBy(c => c.Origin.Z)
            //                            .Where(g => g.Count() >= 2).FirstOrDefault()?.ToList();

            //                        if (hConnsByZ == null || hConnsByZ.Count != 2) return;

            //                        rotationAxis = Line.CreateBound(hConnsByZ[0].Origin, hConnsByZ[1].Origin);

            //                        newFitting = CreateFitting(targetFitting, pipe, 4);
            //                        MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
            //                        RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
            //                        //ProcessConnections(targetFitting, newFitting);

            //                        FamilyInstance newSprinklerDouble = NewSprinklerMethod(sp, targetFitting);
            //                        NewSprinklerConnect(pipe, newFitting, newSprinklerDouble,
            //                            ((LocationPoint)targetFitting.Location).Point);
            //                        break;
            //                    case SprinklerConvertMode.ToUp:
            //                    case SprinklerConvertMode.ToDown:
            //                        // 判断是"简单分支"（端头垂直三通改弯头）
            //                        // 还是"复杂分支"（水平三通旋转+新建喷头）
            //                        // ToUp   → 当前是下喷(isDown)触发简单，当前是上喷(isUp)触发复杂
            //                        // ToDown → 当前是上喷(isUp)触发简单，当前是下喷(isDown)触发复杂
            //                        bool isSimpleBranch = mode == SprinklerConvertMode.ToUp ? isDown : isUp;

            //                        if (isSimpleBranch)
            //                        {
            //                            // 简单分支：垂直三通 → 改弯头
            //                            if (horizontalConnectors == null || horizontalConnectors.Count == 2) return;
            //                            toRemoveElems = FindVerticalElementsToRemove(
            //                                targetFitting, mode == SprinklerConvertMode.ToDown);
            //                            foreach (var conn in ft1)
            //                                MEPAnalysisExtension.DisconnectConnector(conn);
            //                            var (c1, c2) = MEPAnalysisExtension.GetClosestConnectorsTuple(
            //                                MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
            //                                MEPAnalysisExtension.GetConnectors(pipe).ToList());
            //                            Document.Create.NewElbowFitting(c1, c2);
            //                        }
            //                        else
            //                        {
            //                            // 复杂分支：水平三通 → 旋转 + 新建喷头
            //                            toRemoveElems.AddRange(new[] { sp.Id, pipe.Id, targetFitting.Id });
            //                            if (refCo.Owner.Id != pipe.Id)
            //                                toRemoveElems.Add(refCo.Owner.Id);
            //                            if (horizontalConnectors == null || horizontalConnectors.Count == 2)
            //                            {
            //                                // 水平三通旋转处理
            //                                rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin,horizontalConnectors[1].Origin);

            //                                newFitting = CreateFitting(targetFitting, pipe, 3);
            //                                MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
            //                                RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
            //                                RotateFittingVertical(newFitting, pipeHorizontal,
            //                                    mode == SprinklerConvertMode.ToDown);
            //                                //ConnectAndMatchSize(newFitting, pipeHorizontal);

            //                                FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
            //                                NewSprinklerConnect(pipe, newFitting, newSprinkler,
            //                                    ((LocationPoint)targetFitting.Location).Point);
            //                                //ProcessConnections(targetFitting, newFitting);
            //                            }
            //                            else
            //                            {
            //                                // 退化处理：找对侧管道连弯头
            //                                Pipe pipe2 = GetOppositePipe(targetFitting, pipe);
            //                                if (pipe2 != null)
            //                                {
            //                                    var (c1, c2) = MEPAnalysisExtension.GetClosestConnectorsTuple(
            //                                        MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
            //                                        MEPAnalysisExtension.GetConnectors(pipe2).ToList());

            //                                    Document.Create.NewElbowFitting(c1, c2);
            //                                }
            //                            }
            //                        }
            //                        break;
            //                }
            //                break;
            //            // 弯头
            //            case 2:
            //                if (pipeHorizontal == null) return;
            //                switch (mode)
            //                {
            //                    case SprinklerConvertMode.ToDouble:
            //                        // 弯头 → 三通（增加一个方向的喷头）
            //                        rotationAxis = Line.CreateBound(
            //                            ((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(0),
            //                            ((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(1));

            //                        newFitting = CreateFitting(targetFitting, pipe, 3);
            //                        MEPAnalysisExtension.ForceCoordFittingZ(targetFitting, newFitting);
            //                        RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
            //                        //ProcessConnections(targetFitting, newFitting);

            //                        FamilyInstance newSprinklerFromElbow = NewSprinklerMethod(sp, targetFitting);
            //                        NewSprinklerConnect(pipe, newFitting, newSprinklerFromElbow,
            //                            ((LocationPoint)targetFitting.Location).Point);
            //                        break;
            //                    case SprinklerConvertMode.ToUp:
            //                    case SprinklerConvertMode.ToDown:
            //                        // 弯头 → 弯头（喷头换向）
            //                        // 步骤3已提前拦截方向一致的情况，此处必然需要操作
            //                        toRemoveElems.AddRange(new[] { sp.Id, targetFitting.Id });
            //                        if (refCo.Owner.Id != pipe.Id)
            //                            toRemoveElems.Add(refCo.Owner.Id);
            //                        pipeHorizontal = GetHorizonPipe(pipe, ft1);
            //                        FamilyInstance newSprinklerElbow = NewSprinklerMethod(sp, targetFitting);
            //                        Connector sprinklerConn = MEPAnalysisExtension
            //                            .GetConnectors(newSprinklerElbow).ToList().FirstOrDefault();
            //                        if (sprinklerConn == null) return;

            //                        Connector pipeConn = MEPAnalysisExtension
            //                            .GetClosestConnector(pipe, newSprinklerElbow);
            //                        MEPAnalysisExtension.DisconnectConnector(pipeConn);

            //                        XYZ offset = sprinklerConn.Origin - pipeConn.Origin;
            //                        ElementTransformUtils.MoveElement(Document, pipe.Id, offset);

            //                        if (!pipeConn.IsConnected && !sprinklerConn.IsConnected)
            //                            pipeConn.ConnectTo(sprinklerConn);

            //                        var (c1Elbow, c2Elbow) = MEPAnalysisExtension.GetClosestConnectorsTuple(
            //                            MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
            //                            MEPAnalysisExtension.GetConnectors(pipe).ToList());

            //                        Document.Create.NewElbowFitting(c1Elbow, c2Elbow);
            //                        break;
            //                }
            //                break;
            //        }
            //        if (mode == SprinklerConvertMode.ToDouble)
            //        {
            //            // 双喷模式：只删原管件（新管件已替代）
            //            Document.Delete(targetFitting.Id);
            //            if (connectedSpFitting != null)
            //                Document.Delete(connectedSpFitting);
            //        }
            //        else if (toRemoveElems.Count > 0)
            //        {
            //            // 单喷转向：删除收集到的元素
            //            Document.Delete(new HashSet<ElementId>(toRemoveElems).ToList());
            //        }
            //        tx.Commit();
            //    }
            //});
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
        private void RotateFittingVertical(FamilyInstance fitting, Pipe pipe, bool upDown)
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
                var pipeLine = (pipe?.Location as LocationCurve)?.Curve as Line;
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
        public List<ElementId> FindVerticalElementsToRemove(Element startElement, bool searchUpward)
        {
            var toRemove = new List<ElementId>();
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
                // 5. 根据方向排序连接器，优先遍历目标垂直方向
                // searchUpward = true  -> 优先 +Z (向上)
                // searchUpward = false -> 优先 -Z (向下，即 Z 值从小到大排序)
                var sortedConnectors = searchUpward
                    ? connectors.OrderByDescending(c => c.CoordinateSystem.BasisZ.Z)
                    : connectors.OrderBy(c => c.CoordinateSystem.BasisZ.Z);
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
                    // 如果找到一个实例，其连接器的 Z 方向符合我们要求的目标方向
                    if (Math.Abs(conn.CoordinateSystem.BasisZ.Z - targetConnZ) < 0.001)
                    {
                        return spInst.Symbol; // 匹配成功，直接返回它所用的族类型
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
        private void RotateFittingToAlignVertical(FamilyInstance fitting, Pipe pipeHorizontal,
            FamilyInstance targetFitting, Line rotationAxis, bool usePipeDirection = false)
        {
            Document doc = fitting.Document;
            var pipeCurve = (pipeHorizontal.Location as LocationCurve)?.Curve as Line;
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
                Connector pipeOpenEnd = pipeHorizontal.ConnectorManager.Connectors.OfType<Connector>()
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
            ElementTransformUtils.RotateElement(doc, fitting.Id, rotationAxis, Math.PI / 2);        
        }
        // 构建过指定点、沿Z轴方向的直线（用于水平面旋转）
        private Line BuildZAxisAtPoint(XYZ point)
            => Line.CreateBound(point, point + XYZ.BasisZ);
        //递归找target连接器对应的水平管。OK
        private Pipe GetHorizonPipe(Pipe sourcePipe, List<Connector> connectorsToCheck)
        {
            // 用于记录已访问的元素，防止死循环
            HashSet<ElementId> visited = new HashSet<ElementId>();
            visited.Add(sourcePipe.Id);
            foreach (Connector startConn in connectorsToCheck)
            {
                foreach (Connector refConn in startConn.AllRefs.OfType<Connector>())
                {
                    if (refConn.ConnectorType == ConnectorType.Logical) continue;
                    // 开启递归搜索
                    Pipe found = MEPAnalysisExtension.GetHorizontalMEPCurveRecursive(refConn.Owner, visited) as Pipe;
                    if (found != null) return found;
                }
            }
            return null;
        }
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
                        refConn.Owner.Id == pipe.Id)                         continue; 
                    // 检查目标是否是族实例 (管件/附件等)
                    if (refConn.Owner is FamilyInstance candidateFitting)
                    {
                        // 1. 排除喷头本身,参考对象本身 (例如上游管件)
                        if (candidateFitting.Id == sprinkler.Id|| candidateFitting.Id == refOwner.Id) continue;
                        // 3. 排除那些“直接连接了喷头”的管件
                        if (IsDirectSprinklerFitting(candidateFitting, sprinkler, refFitting))
                            continue;
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

