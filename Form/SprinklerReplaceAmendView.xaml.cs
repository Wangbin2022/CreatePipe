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
            FilteredElementCollector collector = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
            foreach (Element elem in collector)
            {
                AllSprinklerCount++;
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elem.Id);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                bool isDown = Math.Abs(connector.CoordinateSystem.BasisZ.Z - 1) < 0.001;
                bool isUp = Math.Abs(connector.CoordinateSystem.BasisZ.Z + 1) < 0.001;
                if (!isDown && !isUp) continue;
                if (isDown)
                {
                    AllDownSprinklerCount++;
                    AllDownSprinkler.Add(sprinkler.Id);
                    DownSprinklerType.Add(sprinkler.Symbol.Family);
                }
                else if (isUp)
                {
                    AllUpSprinklerCount++;
                    AllUpSprinkler.Add(sprinkler.Id);
                    UpSprinklerType.Add(sprinkler.Symbol.Family);
                }
                if (!connector.IsConnected) return;

                if (isDown) { ConnectedDownSprinklerCount++; } else { ConnectedUpSprinklerCount++; }
                Pipe pipe;
                foreach (Connector refConn in connector.AllRefs)
                {
                    if (refConn.Owner?.Id == sprinkler.Id) continue;
                    if (refConn.Owner is Pipe)
                    {
                        pipe = (Pipe)refConn.Owner;
                    }
                    else if (refConn.Owner is FamilyInstance fi)
                    {
                        var fitting = (FamilyInstance)refConn.Owner;
                        pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                        if (pipe == null) continue;
                        foreach (Connector pipeConn in pipe.ConnectorManager.Connectors)
                        {
                            foreach (Connector refConn2 in pipeConn.AllRefs)
                            {
                                if (refConn2.Owner?.Id == sprinkler.Id) continue;
                                if (refConn2.Owner is FamilyInstance fitting2 && refConn2.Owner.Id != refConn.Owner.Id)
                                {
                                    int result = 1;
                                    var ft1 = fitting2.MEPModel.ConnectorManager.Connectors?.OfType<Connector>().ToList();
                                    switch (ft1.Count())
                                    {
                                        case 2:
                                            result = 2;
                                            break;
                                        case 3:
                                            result = 3;
                                            break;
                                        case 4:
                                            result = 4;
                                            break;
                                        default:
                                            result = 0;
                                            break;
                                    }
                                    if (result == 4 || (result == 3 && fitting2.HandOrientation.Z == 1))
                                    {
                                        ConnectedDoubleSprinklerCount++;
                                        if (isDown)
                                        {
                                            ConnectedDoubleDownSprinkler.Add(sprinkler.Id);
                                        }
                                        else ConnectedDoubleUpSprinkler.Add(sprinkler.Id);
                                    }
                                    else
                                    {
                                        if (isDown)
                                        {
                                            ConnectedDownSprinkler.Add(sprinkler.Id);
                                        }
                                        else ConnectedUpSprinkler.Add(sprinkler.Id);
                                    }
                                }
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
                Reference r = uIDocument.Selection.PickObject(
                    ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
                FamilyInstance sp = (FamilyInstance)Document.GetElement(r);
                // ── 2. 校验连接器 ─────────────────────────────────────
                var connector = sp.MEPModel?.ConnectorManager?.Connectors
                                  ?.OfType<Connector>().FirstOrDefault();
                if (connector == null || !connector.IsConnected) return;
                // Z == 1  → 连接器朝上 → 下喷
                // Z == -1 → 连接器朝下 → 上喷
                bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
                bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
                // ── 3. 单喷转向：方向已一致则无需操作 ───────────────────
                if (mode == SprinklerConvertMode.ToUp && isUp) return;
                if (mode == SprinklerConvertMode.ToDown && isDown) return;
                if (mode != SprinklerConvertMode.ToDouble && !isDown && !isUp) return;
                // ── 4. 开始事务 ──────────────────────────────────────
                using (Transaction tx = new Transaction(Document))
                {
                    tx.Start("喷头方向转换");
                    // ── 5. 获取关联元素 ───────────────────────────────
                    Pipe pipe = GetConnectedPipe(sp, out Connector refCo);
                    FamilyInstance targetFitting = GetTargetFitting(sp, pipe, refCo);
                    if (pipe == null || targetFitting == null) return;
                    var ft1 = MEPAnalysisExtension.GetConnectors(targetFitting).ToList();
                    var toRemoveElems = new List<ElementId>();
                    Pipe pipeHorizontal = GetHorizonPipe(pipe, ft1, targetFitting);
                    // 水平连接器对（用于创建旋转轴）
                    List<Connector> horizontalConnectors = ft1.Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.01)
                        .ToList();
                    Line rotationAxis;
                    FamilyInstance newFitting;
                    // ── 6. 按管件连接数分支 ───────────────────────────
                    switch (ft1.Count)
                    {
                        // 四通
                        case 4:
                            switch (mode)
                            {
                                case SprinklerConvertMode.ToDouble:
                                    // 已是四通 = 已是双喷，无需处理
                                    return;
                                case SprinklerConvertMode.ToUp:
                                case SprinklerConvertMode.ToDown:
                                    // 四通 → 三通：删除目标方向的垂直支管
                                    if (pipeHorizontal == null) return;
                                    if (horizontalConnectors == null || horizontalConnectors.Count != 2) return;
                                    // ToUp → 保留上喷，删下喷(false)；ToDown → 保留下喷，删上喷(true)
                                    toRemoveElems = FindVerticalElementsToRemove(
                                        targetFitting, mode == SprinklerConvertMode.ToDown);
                                    rotationAxis = Line.CreateBound(
                                        horizontalConnectors[0].Origin,
                                        horizontalConnectors[1].Origin);
                                    newFitting = CreateFitting(targetFitting, pipe, 3);
                                    ForceCoordFittingZ(targetFitting, newFitting);
                                    RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
                                    // ToUp → 侧口朝上(false)；ToDown → 侧口朝下(true)
                                    RotateFittingVertical(newFitting, pipeHorizontal,
                                        mode == SprinklerConvertMode.ToDown);
                                    MatchConnectorSizes(newFitting, pipeHorizontal);
                                    ProcessConnections(targetFitting, newFitting);
                                    break;
                            }
                            break;

                        // 三通
                        case 3:
                            if (pipeHorizontal == null) return;
                            switch (mode)
                            {
                                case SprinklerConvertMode.ToDouble:
                                    // 三通 → 四通
                                    var hConnsByZ = ft1
                                        .GroupBy(c => c.Origin.Z)
                                        .Where(g => g.Count() >= 2)
                                        .FirstOrDefault()?.ToList();

                                    if (hConnsByZ == null || hConnsByZ.Count != 2) return;

                                    rotationAxis = Line.CreateBound(hConnsByZ[0].Origin, hConnsByZ[1].Origin);

                                    newFitting = CreateFitting(targetFitting, pipe, 4);
                                    ForceCoordFittingZ(targetFitting, newFitting);
                                    RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
                                    ProcessConnections(targetFitting, newFitting);

                                    FamilyInstance newSprinklerDouble = NewSprinklerMethod(sp, targetFitting);
                                    NewSprinklerConnect(pipe, newFitting, newSprinklerDouble,
                                        ((LocationPoint)targetFitting.Location).Point);
                                    break;
                                case SprinklerConvertMode.ToUp:
                                case SprinklerConvertMode.ToDown:
                                    // 判断是"简单分支"（端头垂直三通改弯头）
                                    // 还是"复杂分支"（水平三通旋转+新建喷头）
                                    // ToUp   → 当前是下喷(isDown)触发简单，当前是上喷(isUp)触发复杂
                                    // ToDown → 当前是上喷(isUp)触发简单，当前是下喷(isDown)触发复杂
                                    bool isSimpleBranch = mode == SprinklerConvertMode.ToUp ? isDown : isUp;

                                    if (isSimpleBranch)
                                    {
                                        // 简单分支：垂直三通 → 改弯头
                                        if (horizontalConnectors == null || horizontalConnectors.Count == 2) return;

                                        toRemoveElems = FindVerticalElementsToRemove(
                                            targetFitting, mode == SprinklerConvertMode.ToDown);

                                        foreach (var conn in ft1)
                                            MEPAnalysisExtension.DisconnectConnector(conn);

                                        var (c1, c2) = MEPAnalysisExtension.GetClosestConnectorsTuple(
                                            MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
                                            MEPAnalysisExtension.GetConnectors(pipe).ToList());

                                        Document.Create.NewElbowFitting(c1, c2);
                                    }
                                    else
                                    {
                                        // 复杂分支：水平三通 → 旋转 + 新建喷头
                                        toRemoveElems.AddRange(new[] { sp.Id, pipe.Id, targetFitting.Id });
                                        if (refCo.Owner.Id != pipe.Id)
                                            toRemoveElems.Add(refCo.Owner.Id);

                                        if (horizontalConnectors == null || horizontalConnectors.Count == 2)
                                        {
                                            // 水平三通旋转处理
                                            rotationAxis = Line.CreateBound(
                                                horizontalConnectors[0].Origin,
                                                horizontalConnectors[1].Origin);

                                            newFitting = CreateFitting(targetFitting, pipe, 3);
                                            ForceCoordFittingZ(targetFitting, newFitting);
                                            RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
                                            RotateFittingVertical(newFitting, pipeHorizontal,
                                                mode == SprinklerConvertMode.ToDown);
                                            MatchConnectorSizes(newFitting, pipeHorizontal);

                                            FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                                            NewSprinklerConnect(pipe, newFitting, newSprinkler,
                                                ((LocationPoint)targetFitting.Location).Point);
                                            ProcessConnections(targetFitting, newFitting);
                                        }
                                        else
                                        {
                                            // 退化处理：找对侧管道连弯头
                                            Pipe pipe2 = GetOppositePipe(targetFitting, pipe);
                                            if (pipe2 != null)
                                            {
                                                var (c1, c2) = MEPAnalysisExtension.GetClosestConnectorsTuple(
                                                    MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
                                                    MEPAnalysisExtension.GetConnectors(pipe2).ToList());

                                                Document.Create.NewElbowFitting(c1, c2);
                                            }
                                        }
                                    }
                                    break;
                            }
                            break;
                        // 弯头
                        case 2:
                            if (pipeHorizontal == null) return;
                            switch (mode)
                            {
                                case SprinklerConvertMode.ToDouble:
                                    // 弯头 → 三通（增加一个方向的喷头）
                                    rotationAxis = Line.CreateBound(
                                        ((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(0),
                                        ((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(1));

                                    newFitting = CreateFitting(targetFitting, pipe, 3);
                                    ForceCoordFittingZ(targetFitting, newFitting);
                                    RotateFittingToAlignVertical(newFitting, pipeHorizontal, targetFitting, rotationAxis);
                                    ProcessConnections(targetFitting, newFitting);

                                    FamilyInstance newSprinklerFromElbow = NewSprinklerMethod(sp, targetFitting);
                                    NewSprinklerConnect(pipe, newFitting, newSprinklerFromElbow,
                                        ((LocationPoint)targetFitting.Location).Point);
                                    break;
                                case SprinklerConvertMode.ToUp:
                                case SprinklerConvertMode.ToDown:
                                    // 弯头 → 弯头（喷头换向）
                                    // 步骤3已提前拦截方向一致的情况，此处必然需要操作
                                    toRemoveElems.AddRange(new[] { sp.Id, targetFitting.Id });
                                    if (refCo.Owner.Id != pipe.Id)
                                        toRemoveElems.Add(refCo.Owner.Id);

                                    pipeHorizontal = GetHorizonPipe(pipe, ft1, targetFitting);

                                    FamilyInstance newSprinklerElbow = NewSprinklerMethod(sp, targetFitting);
                                    Connector sprinklerConn = MEPAnalysisExtension
                                        .GetConnectors(newSprinklerElbow).ToList().FirstOrDefault();
                                    if (sprinklerConn == null) return;

                                    Connector pipeConn = MEPAnalysisExtension
                                        .GetClosestConnector(pipe, newSprinklerElbow);
                                    MEPAnalysisExtension.DisconnectConnector(pipeConn);

                                    XYZ offset = sprinklerConn.Origin - pipeConn.Origin;
                                    ElementTransformUtils.MoveElement(Document, pipe.Id, offset);

                                    if (!pipeConn.IsConnected && !sprinklerConn.IsConnected)
                                        pipeConn.ConnectTo(sprinklerConn);

                                    var (c1Elbow, c2Elbow) = MEPAnalysisExtension.GetClosestConnectorsTuple(
                                        MEPAnalysisExtension.GetConnectors(pipeHorizontal).ToList(),
                                        MEPAnalysisExtension.GetConnectors(pipe).ToList());

                                    Document.Create.NewElbowFitting(c1Elbow, c2Elbow);
                                    break;
                            }
                            break;
                    }
                    if (mode == SprinklerConvertMode.ToDouble)
                    {
                        // 双喷模式：只删原管件（新管件已替代）
                        Document.Delete(targetFitting.Id);
                        if (connectedSpFitting != null)
                            Document.Delete(connectedSpFitting);
                    }
                    else if (toRemoveElems.Count > 0)
                    {
                        // 单喷转向：删除收集到的元素
                        Document.Delete(new HashSet<ElementId>(toRemoveElems).ToList());
                    }
                    tx.Commit();
                }
            });
        }
        private Pipe GetOppositePipe(FamilyInstance fitting, Pipe excludePipe)
        {
            var connectors = MEPAnalysisExtension.GetConnectors(fitting).ToList();
            if (connectors == null) return null;
            Connector excludeConnector = null;
            foreach (var conn in connectors)
            {
                foreach (var refConn in conn.AllRefs.OfType<Connector>())
                {
                    if (refConn.Owner is Pipe pipe && pipe.Id == excludePipe.Id)
                    {
                        excludeConnector = conn;
                        break;
                    }
                }
                if (excludeConnector != null) break;
            }
            if (excludeConnector == null)
            {
                return null;
            }
            Connector farthestConnector = null;
            double maxDistance = 0;
            foreach (var conn in connectors)
            {
                if (conn != excludeConnector)
                {
                    double distance = conn.Origin.DistanceTo(excludeConnector.Origin);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farthestConnector = conn;
                    }
                }
            }
            if (farthestConnector == null)
            {
                return null;
            }
            foreach (var refConn in farthestConnector.AllRefs.OfType<Connector>())
            {
                if (refConn.Owner is Pipe pipe && pipe.Id != excludePipe.Id)
                {
                    return pipe;
                }
            }
            return null;
        }
        private void RotateFittingVertical(FamilyInstance fitting, Pipe pipe, bool upDown)
        {
            Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(fitting);
            if (sideConn == null) return;
            // 确定目标方向
            XYZ targetDirection = upDown ? -XYZ.BasisZ : XYZ.BasisZ;
            XYZ currentDirection = sideConn.CoordinateSystem.BasisZ;
            // 检查当前方向是否已经正确
            if (currentDirection.IsAlmostEqualTo(targetDirection, 0.001)) return;
            // 计算旋转轴和角度
            XYZ rotationAxis;
            double rotationAngle;
            // 处理方向完全相反的特殊情况（夹角≈180度）
            if (currentDirection.IsAlmostEqualTo(-targetDirection, 0.001))
            {
                // 当方向相反时，选择任意垂直轴作为旋转轴
                rotationAxis = currentDirection.CrossProduct(Math.Abs(currentDirection.X) > 0.9 ? XYZ.BasisY : XYZ.BasisX);
                rotationAngle = Math.PI;
            }
            else
            {
                rotationAxis = currentDirection.CrossProduct(targetDirection);
                rotationAngle = currentDirection.AngleTo(targetDirection);
            }
            XYZ fittingOrigin = ((LocationPoint)fitting.Location).Point;
            Line rotationAxisLine = Line.CreateBound(fittingOrigin, fittingOrigin + rotationAxis.Normalize());
            ElementTransformUtils.RotateElement(Document, fitting.Id, rotationAxisLine, rotationAngle);
        }
        public List<ElementId> FindVerticalElementsToRemove(Element startElement, bool upDown)
        {
            var toRemove = new List<ElementId>();
            CollectConnectedVerticalElements(startElement, toRemove, new HashSet<ElementId>(), upDown);
            return toRemove;
        }
        private bool CollectConnectedVerticalElements(Element currentElem, List<ElementId> result, HashSet<ElementId> visited, bool upDown)
        {
            // 1. 跳过已处理元素
            if (visited.Contains(currentElem.Id)) return false;
            visited.Add(currentElem.Id);
            // 2. 获取所有有效连接器
            var connectors = MEPAnalysisExtension.GetConnectors(currentElem).ToList();
            if (connectors.Count == 0) return false;
            // 3. 喷头判定（单连接器族实例）
            if (connectors.Count == 1)
            {
                result.Add(currentElem.Id);
                return true;
            }
            // 4. 添加当前元素到结果
            result.Add(currentElem.Id);
            // 5. 递归处理连接元素（优先处理垂直向上连接）
            // 5. 优先处理垂直向上连接器
            //foreach (var conn in connectors.OrderByDescending(c => c.CoordinateSystem.BasisZ.Z))
            // 获取所有连接器
            //var connectors = fitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList();
            //true向上，false向下
            var sortedConnectors = new List<Connector>();
            if (upDown == true)
            {
                sortedConnectors = connectors.OrderByDescending(c => c.CoordinateSystem.BasisZ.Z).ToList();
            }
            else sortedConnectors = connectors.OrderByDescending(c => -c.CoordinateSystem.BasisZ.Z).ToList();
            // 遍历排序后的连接器
            foreach (var conn in sortedConnectors)
            {
                foreach (var refConn in conn.AllRefs.OfType<Connector>())
                {
                    if (refConn.Owner.Id != currentElem.Id)
                    {
                        var nextElem = Document.GetElement(refConn.Owner.Id);
                        if (CollectConnectedVerticalElements(nextElem, result, visited, upDown))
                        {
                            // 如果子递归找到喷头，终止上层递归
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        //强制管件Z轴重合
        private static void ForceCoordFittingZ(FamilyInstance targetFitting, FamilyInstance teeFitting)
        {
            // 获取目标位置和当前实际位置（仅比较 Z 值）
            double targetZ = ((LocationPoint)targetFitting.Location).Point.Z;
            double currentZ = ((LocationPoint)teeFitting.Location).Point.Z;
            // 如果 Z 值不同，仅沿 Z 轴移动
            if (!(Math.Abs(targetZ - currentZ) < 0.001)) // 使用容差比较浮点数
            {
                // 获取目标高程（从 targetFitting）
                Parameter targetElevationParam = targetFitting.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                double targetElevation = targetElevationParam.AsDouble();
                Parameter teeElevationParam = teeFitting.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                teeElevationParam.Set(targetElevation);
            }
        }
        //// 新增方法：匹配管件连接器尺寸与管道一致
        private void MatchConnectorSizes(FamilyInstance fitting, Pipe pipe)
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
        //先断开水平连接再连接
        private void NewSprinklerConnect(Pipe pipe, FamilyInstance crossFitting, FamilyInstance newSprinkler, XYZ originalCrossPosition)
        {
            // 1. 获取连接器
            Connector sprinklerConn = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
            Connector crossVerticalConn = null;
            if (crossFitting?.MEPModel?.ConnectorManager == null) return;
            ConnectorSet connectors = crossFitting.MEPModel.ConnectorManager.Connectors;
            // 第一优先级：完全匹配preferredDirection的连接器
            foreach (Connector conn in connectors) { if (!conn.IsConnected && conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(XYZ.BasisZ)) crossVerticalConn = conn; }
            // 第二优先级：根据upDown参数筛选方向
            XYZ targetDirection = true ? -XYZ.BasisZ : XYZ.BasisZ;
            foreach (Connector conn in connectors) { if (!conn.IsConnected && conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(targetDirection)) crossVerticalConn = conn; }
            // 第三优先级：任意未使用的连接器
            foreach (Connector conn in connectors) { if (!conn.IsConnected) crossVerticalConn = conn; }
            // 2. 匹配连接器尺寸
            MatchConnectorSizes(crossFitting, pipe);
            // 3. 记录原始水平管道的位置和连接关系（关键优化）
            Dictionary<Connector, (XYZ OriginalPos, List<Connector> ConnectedConns)> originalConnData =
                new Dictionary<Connector, (XYZ, List<Connector>)>();
            var horizontalConns = crossFitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                .Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.1).ToList();
            ////// 3. 记录原始水平管道的位置（关键修改）
            foreach (Connector hConn in horizontalConns)
            {
                // 记录水平管道连接点的原始位置
                //originalHorizontalPipePositions[hConn] = hConn.Origin;
                // 存储原始位置和连接关系
                var connectedConns = hConn.AllRefs.Cast<Connector>().ToList();
                originalConnData[hConn] = (hConn.Origin, connectedConns);
                // 断开水平管道连接
                foreach (Connector c in connectedConns)
                {
                    c.DisconnectFrom(hConn);
                }
            }
            // 4. 创建垂直管道（可能导致 crossFitting 移动）设置系统类型和管径
            Pipe verticalPipe = Pipe.Create(Document, pipe.PipeType.Id, pipe.ReferenceLevel.Id, crossVerticalConn, sprinklerConn);
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe.Diameter);
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).Set(pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId());
            Document.Regenerate();
            ElementTransformUtils.MoveElement(Document, crossFitting.Id, originalCrossPosition - (crossFitting.Location as LocationPoint).Point);
            //// 6. 重新连接水平管道（从字典获取原始连接关系）
            foreach (var hConn in horizontalConns)
            {
                if (originalConnData.TryGetValue(hConn, out var data))
                {
                    foreach (var originalConn in data.ConnectedConns)
                    {
                        if (!originalConn.IsConnected && !hConn.IsConnected)
                        {
                            try
                            {
                                originalConn.ConnectTo(hConn);
                            }
                            catch
                            {
                                hConn.ConnectTo(originalConn);  // 反向连接
                            }
                        }
                    }
                }
            }
        }
        //根据原喷头和方向距离生成新喷头
        private FamilyInstance NewSprinklerMethod(FamilyInstance sp, FamilyInstance targetFitting)
        {
            FamilyInstance newSprinkler = null;
            Connector connector = MEPAnalysisExtension.GetConnectors(sp).ToList().FirstOrDefault();
            bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
            bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
            //9. 获取原喷头位置和fitting位置取新喷头定位
            XYZ originalLocation = sp.GetTransform().Origin;
            XYZ fittingLocation = targetFitting.GetTransform().Origin;
            double deltaHeight = Math.Abs((sp.GetTransform().Origin.Z - targetFitting.GetTransform().Origin.Z) * 304.8);
            // 计算新Z坐标（根据上下喷方向决定偏移方向）,创建新位置
            double zOffset = 0;
            if (isUp)
            {
                zOffset = -(deltaHeight / 304.8);
            }
            else if (isDown)
            {
                zOffset = (deltaHeight / 304.8);
            }
            else TaskDialog.Show("tt", "喷头方向存在异常"); ;
            double newZ = fittingLocation.Z + zOffset;
            XYZ newLocation = new XYZ(originalLocation.X, originalLocation.Y, newZ);
            var newSprinklerId = ElementTransformUtils.CopyElement(Document, sp.Id, newLocation - originalLocation).FirstOrDefault();
            newSprinkler = (FamilyInstance)Document.GetElement(newSprinklerId);
            // 根据上下喷方向决定喷头样式
            newSprinkler.ChangeTypeId(isDown ? SelectedUpSp.GetFamilySymbolIds().First() : SelectedDownSp.GetFamilySymbolIds().First());
            return newSprinkler;
        }
        //处理新旧管件接口连接关系
        private void ProcessConnections(FamilyInstance oldFitting, FamilyInstance newFitting)
        {
            Dictionary<Connector, Connector> connectorMap = new Dictionary<Connector, Connector>();
            foreach (Connector oldConn in oldFitting.MEPModel.ConnectorManager.Connectors)
            {
                Connector newConn = null;
                ConnectorSet connectors = newFitting.MEPModel.ConnectorManager.Connectors;
                foreach (Connector conn in connectors)
                {
                    if (conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(oldConn.CoordinateSystem.BasisZ))
                    {
                        newConn = conn;
                    }
                }
                if (newConn == null) return;
                connectorMap.Add(oldConn, newConn);
                // 同步连接器尺寸
                Parameter oldDiamParam = GetAssociatedParameter(oldFitting, oldConn, BuiltInParameter.CONNECTOR_RADIUS);
                if (oldDiamParam != null)
                {
                    Parameter newDiamParam = GetAssociatedParameter(newFitting, newConn, BuiltInParameter.CONNECTOR_RADIUS);
                    newDiamParam?.Set(oldDiamParam.AsDouble());
                }

            }
            // 8. 转移连接关系
            foreach (var pair in connectorMap)
            {
                ConnectorSet refs = pair.Key.AllRefs;
                foreach (Connector refConn in refs)
                {
                    if (refConn.Owner is MEPCurve || refConn.Owner is FamilyInstance)
                    {
                        refConn.DisconnectFrom(pair.Key);
                        refConn.ConnectTo(pair.Value);
                    }
                }
            }
        }

        /// 对齐管件到水平管道方向，再绕旋转轴做90°垂直旋转
        private void RotateFittingToAlignVertical(FamilyInstance fitting, Pipe pipeHorizontal,
            FamilyInstance targetFitting, Line rotationAxis, bool usePipeDirection = false)
        {
            var pipeCurve = ((LocationCurve)pipeHorizontal.Location).Curve as Line;
            if (pipeCurve == null) return;
            XYZ targetCenter = ((LocationPoint)targetFitting.Location).Point;
            var ft1Count = MEPAnalysisExtension.GetConnectors(targetFitting).ToList().Count;
            // Z轴旋转轴（过原管件中心，竖直向上）
            Line zAxis = BuildZAxisAtPoint(targetCenter);
            // ── 2. 水平面旋转（步骤因场景而异）──────────────────────────
            if (usePipeDirection && ft1Count == 3)
            {
                // ── 场景A（原方法1的四通分支）──────────────────────────
                // 用管道XY投影方向直接算角度，使管件主轴与管道走向对齐
                XYZ pipeDir = pipeCurve.Direction;
                XYZ projectedDir = new XYZ(pipeDir.X, pipeDir.Y, 0).Normalize();
                double xyAngle = XYZ.BasisX.AngleTo(projectedDir);
                if (projectedDir.Y < 0) xyAngle = -xyAngle;

                ElementTransformUtils.RotateElement(Document, fitting.Id, zAxis, xyAngle);
            }
            else
            {
                // ── 场景B（三通：侧口对齐管道端口方向）────────────────
                // 原方法1的三通分支 和 原方法2 均走此逻辑，区别仅在于找sideConn的方式
                // 现统一使用优化后的 GetTeeSideConn（内部已处理距离/方向判断）
                Connector sideConn = MEPAnalysisExtension.GetTeeSideConn(fitting);
                //找水平管距离原管件最近的连接器端口
                Connector pipeOpenEnd = pipeHorizontal.ConnectorManager.Connectors.OfType<Connector>()
                    .OrderBy(c => c.Origin.DistanceTo(targetCenter)) .FirstOrDefault();
                if (sideConn == null || pipeOpenEnd == null) return;
                // 目标方向 = 管道端口方向的反向（侧口要对着管道伸出去）
                XYZ targetDirection = -pipeOpenEnd.CoordinateSystem.BasisZ;
                double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                // 叉积Z分量判断旋转方向
                XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                if (cross.Z < 0) angle = -angle;
                ElementTransformUtils.RotateElement(Document, fitting.Id, zAxis, angle);
            }
            // ── 3.绕管道轴垂直旋转 90°（两种场景统一处理）────────────
            ElementTransformUtils.RotateElement(Document, fitting.Id, rotationAxis, Math.PI / 2);
        }
        /// <summary>
        /// 构建过指定点、沿Z轴方向的直线（用于水平面旋转）
        /// </summary>
        private Line BuildZAxisAtPoint(XYZ point)
            => Line.CreateBound(point, point + XYZ.BasisZ);
        //在管件原位建立管道默认的新管件样式
        private FamilyInstance CreateFitting(FamilyInstance targetFitting, Pipe pipe, int fittingType)
        {
            FamilySymbol familySymbol = null;
            switch (fittingType)
            {
                case 2:
                    familySymbol = (FamilySymbol)Document.GetElement(pipe.PipeType.RoutingPreferenceManager
                .GetRule(RoutingPreferenceRuleGroupType.Elbows, 0).MEPPartId);
                    break;
                case 3:
                    familySymbol = (FamilySymbol)Document.GetElement(pipe.PipeType.RoutingPreferenceManager
.GetRule(RoutingPreferenceRuleGroupType.Junctions, 0).MEPPartId);
                    break;
                default:
                    familySymbol = (FamilySymbol)Document.GetElement(pipe.PipeType.RoutingPreferenceManager
.GetRule(RoutingPreferenceRuleGroupType.Crosses, 0).MEPPartId);
                    break;
            }
            return Document.Create.NewFamilyInstance(((LocationPoint)targetFitting.Location).Point, familySymbol, targetFitting.Host, (Level)Document.GetElement(targetFitting.LevelId), StructuralType.NonStructural);
        }
        //找target连接器对应的水平管
        private Pipe GetHorizonPipe(Pipe pipe, List<Connector> ft1, FamilyInstance targetFitting)
        {
            Pipe pipe2 = null;
            try
            {
                foreach (Connector hConn in ft1)
                {
                    foreach (Connector refConn in hConn.AllRefs)
                    {
                        if (!refConn.IsConnected) continue;
                        if (refConn.Owner is Pipe && refConn.Owner.Id != pipe.Id)
                        {
                            if (MEPAnalysisExtension.IsHorizontal((Pipe)refConn.Owner))
                            {
                                pipe2 = (Pipe)refConn.Owner;
                            }
                        }
                        else if (refConn.Owner is FamilyInstance fi)
                        {
                            var fitting = (FamilyInstance)refConn.Owner;
                            connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                            if (MEPAnalysisExtension.IsHorizontal(((Pipe)GetConnectedMEPCurve(fitting, fitting.Id))))
                            {
                                pipe2 = (Pipe)GetConnectedMEPCurve(fitting, fitting.Id);
                            }
                        }
                    }
                }
                return pipe2;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", "未找到水平管，原因" + ex.Message);
                throw;
            }
        }
        //获取直接喷头相连的connector和pipe
        private FamilyInstance GetTargetFitting(FamilyInstance sprinkler, Pipe pipe, Connector refConnector)
        {
            // 1. 首先处理refConnector.Owner为管道的情况
            if (refConnector.Owner is Pipe)
            {
                // 获取管道另一端的连接器
                var farEndConnector = pipe.ConnectorManager.Connectors.OfType<Connector>()
                    .FirstOrDefault(c => c.Id != refConnector.Id);
                if (farEndConnector == null) return null;
                // 在远端连接器上查找合适的管件
                foreach (Connector refConn in farEndConnector.AllRefs)
                {
                    if (refConn.Owner is FamilyInstance fitting && fitting.Id != sprinkler.Id && !IsDirectSprinklerFitting(fitting, sprinkler, null))
                    {
                        return fitting;
                    }
                }
                return null;
            }
            // 2. 原有处理逻辑（refConnector.Owner为FamilyInstance的情况）
            FamilyInstance targetFitting = null;
            foreach (Connector pipeConn in pipe.ConnectorManager.Connectors)
            {
                foreach (Connector refConn2 in pipeConn.AllRefs)
                {
                    // 排除条件
                    if (refConn2.Owner?.Id == sprinkler.Id ||
                        refConn2.Owner?.Id == refConnector.Owner.Id ||
                        !(refConn2.Owner is FamilyInstance))
                        continue;

                    var candidateFitting = (FamilyInstance)refConn2.Owner;

                    // 新增过滤：确保不是喷头直接连接的管件
                    if (!IsDirectSprinklerFitting(candidateFitting, sprinkler, (FamilyInstance)refConnector.Owner))
                    {
                        targetFitting = candidateFitting;
                        break;
                    }
                }
                if (targetFitting != null) break;
            }
            return targetFitting;
        }
        // 查找管道另一端的关键管件targetFitting和Connector
        private Pipe GetConnectedPipe(FamilyInstance sp, out Connector foundConnector)
        {
            var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
            foundConnector = null;
            foreach (Connector refConn in connector.AllRefs)
            {
                if (refConn.Owner?.Id == sp.Id) continue;
                foundConnector = refConn;
                // 获取连接的管道或管件
                if (refConn.Owner is Pipe p)
                {
                    return p;
                }
                else if (refConn.Owner is FamilyInstance fi)
                {
                    // 如果连接的是管件，尝试获取连接到该管件的管道
                    return GetConnectedMEPCurve(fi, sp.Id) as Pipe;
                }
            }
            // 如果没有找到有效连接，清空out参数
            foundConnector = null;
            return null;
        }
        private bool IsDirectSprinklerFitting(FamilyInstance fitting, FamilyInstance sprinkler, FamilyInstance refFitting)
        {
            // 检查是否是喷头直接连接的管件
            var fittingConnectors = fitting.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>();
            if (fittingConnectors == null) return false;
            foreach (var conn in fittingConnectors)
            {
                foreach (Connector refConn in conn.AllRefs)
                {
                    if (refConn.Owner?.Id == sprinkler.Id ||
                       (refFitting != null && refConn.Owner?.Id == refFitting.Id))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private Parameter GetAssociatedParameter(Element element, Connector connector, BuiltInParameter connectorParameter)
        {
            var connectorInfo = connector.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;
            if (connectorInfo == null) return null;
            var associatedFamilyParameterId = connectorInfo.GetAssociateFamilyParameterId(new ElementId(connectorParameter));
            if (associatedFamilyParameterId == ElementId.InvalidElementId) return null;
            var document = element.Document;
            var parameterElement = document.GetElement(associatedFamilyParameterId) as ParameterElement;
            if (parameterElement == null) return null;
            var paramterDefinition = parameterElement.GetDefinition();
            return element.get_Parameter(paramterDefinition);
        }
        private MEPCurve GetConnectedMEPCurve(FamilyInstance fitting, ElementId excludeId)
        {
            foreach (Connector fittingConn in fitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>())
            {
                foreach (Connector linkedConn in fittingConn.AllRefs)
                {
                    if (linkedConn.Owner is MEPCurve pipe && linkedConn.Owner.Id != excludeId)
                        return pipe;
                }
            }
            return null;
        }
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

