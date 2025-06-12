using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static System.Net.WebRequestMethods;

namespace CreatePipe.Form
{
    /// <summary>
    /// SprinklerReplaceView.xaml 的交互逻辑
    /// </summary>
    public partial class SprinklerReplaceView : Window
    {
        public SprinklerReplaceView(UIApplication uIApp)
        {
            InitializeComponent();
            this.DataContext = new SprinklerReplaceViewModel(uIApp);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SprinklerReplaceViewModel : ObserverableObject
    {
        public UIApplication Application { get; set; }
        public Document Document { get; set; }
        public SprinklerReplaceViewModel(UIApplication uiApp)
        {
            Application = uiApp;
            Document = uiApp.ActiveUIDocument.Document;

            FilteredElementCollector collector = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
            foreach (Element elem in collector)
            {
                AllSprinklerCount++;
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elem.Id);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
                bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
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
                if (connector.IsConnected)
                {
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
                            foreach (Connector pipeConn in pipe.ConnectorManager.Connectors)
                            {
                                foreach (Connector refConn2 in pipeConn.AllRefs)
                                {
                                    if (refConn2.Owner?.Id == sprinkler.Id) continue;
                                    if (refConn2.Owner is FamilyInstance fitting2 && refConn2.Owner.Id != refConn.Owner.Id)
                                    {
                                        int result = GetFittingCategory(fitting2);
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
            }
            SelectedDownSp = DownSprinklerType.FirstOrDefault();
            SelectedUpSp = UpSprinklerType.FirstOrDefault();
        }
        public ICommand ConvertUpSprinklerCommand => new BaseBindingCommand(ConvertUpSprinkler);
        private void ConvertUpSprinkler(object obj)
        {
            TaskDialog.Show("tt", "PASS1");
        }
        public ICommand ConvertDownSprinklerCommand => new BaseBindingCommand(ConvertDownSprinkler);
        private void ConvertDownSprinkler(object obj)
        {
            //逻辑梳理，检测喷头类型，未连接的给提示
            //如果是下喷，分端头和中间检测，如果喷头不一致，先调用switch替换，一致的话不操作
            //如果是上喷，按高度差复制构件，删除原构件，连接转换弯头/三通？喷头不一致的替换
            //如果是上下喷，删除上喷构件和立管，分端头和中间改管件样式
            TaskDialog.Show("tt", "PASS2");
        }
        public ICommand ConvertDoubleSprinklerCommand => new BaseBindingCommand(ConvertDoubleSprinkler);
        private void ConvertDoubleSprinkler(object obj)
        {
            Reference r = Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
            FamilyInstance sp = (FamilyInstance)Document.GetElement(r);
            var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
            if (!connector.IsConnected) return;
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("喷头改上下喷");
                Pipe pipe = GetConnectedPipe(sp, out Connector refCo);
                FamilyInstance targetFitting = GetTargetFitting(sp, pipe, refCo);
                var ft1 = GetConnectors(targetFitting);
                //处理三通改四通
                if (ft1.Count == 4)
                {
                    return;
                }
                if (ft1.Count == 3)
                {
                    Pipe pipeHorizontal = GetHorizonPipe(pipe, ft1);
                    //获取三通的水平连接器对,创建旋转轴
                    var horizontalConnectors = ft1.GroupBy(c => c.Origin.Z).Where(g => g.Count() >= 2).FirstOrDefault()?.ToList();
                    if (horizontalConnectors == null || horizontalConnectors.Count != 2) return;
                    Line rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
                    FamilyInstance crossFitting = CreateFitting(targetFitting, pipe, 4);
                    RotateFittingToAlignWithPipe(crossFitting, pipeHorizontal, targetFitting, rotationAxis);
                    //// 7. 转移连接并同步尺寸
                    ProcessConnections(targetFitting, crossFitting);
                    //按方向建立新喷头并连接
                    FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                    NewSprinklerConnect(pipe, crossFitting, newSprinkler, ((LocationPoint)targetFitting.Location).Point);
                }
                //处理弯头转垂直三通
                else if (ft1.Count == 2)
                {
                    Pipe pipeHorizontal = GetHorizonPipe(pipe, ft1);
                    //直接用弯头连接的水平管获取旋转轴
                    Line rotationAxis = Line.CreateBound(((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(0), ((LocationCurve)pipeHorizontal.Location).Curve.GetEndPoint(1));
                    FamilyInstance teeFitting = CreateFitting(targetFitting, pipe, 3);
                    RotateFittingToAlignWithPipe(teeFitting, pipeHorizontal, targetFitting, rotationAxis);
                    ProcessConnections(targetFitting, teeFitting);
                    FamilyInstance newSprinkler = NewSprinklerMethod(sp, targetFitting);
                    NewSprinklerConnect(pipe, teeFitting, newSprinkler, ((LocationPoint)targetFitting.Location).Point);
                }
                Document.Delete(targetFitting.Id);
                if (connectedSpFitting != null)
                {
                    Document.Delete(connectedSpFitting);
                }
                tx.Commit();
            }
        }
        //先断开水平连接再连接
        //private void NewSprinklerConnect(Pipe pipe, FamilyInstance crossFitting, FamilyInstance newSprinkler)
        //{
        //    // 1. 获取连接器
        //    Connector sprinklerConn = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
        //    Connector crossVerticalConn = FindUnusedConnector(crossFitting, XYZ.BasisZ);
        //    // 2. 匹配连接器尺寸（新增逻辑）
        //    MatchConnectorSizes(crossFitting, pipe);
        //    // 3. 记录原始连接关系
        //    Dictionary<Connector, List<Connector>> originalConnections = new Dictionary<Connector, List<Connector>>();
        //    var horizontalConns = crossFitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
        //        .Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.1).ToList();
        //    foreach (Connector hConn in horizontalConns)
        //    {
        //        var connectedConns = hConn.AllRefs.Cast<Connector>().ToList();
        //        originalConnections[hConn] = hConn.AllRefs.Cast<Connector>().ToList();
        //        foreach (Connector c in connectedConns)
        //        {
        //            c.DisconnectFrom(hConn);
        //        }
        //    }
        //    // 4. 创建垂直管道
        //    Pipe verticalPipe = Pipe.Create(Document, pipe.PipeType.Id, pipe.ReferenceLevel.Id, crossVerticalConn, sprinklerConn);
        //    verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe.Diameter);
        //    // 5. 恢复连接关系
        //    RestoreConnections(originalConnections);
        //}
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
            Connector crossVerticalConn = FindUnusedConnector(crossFitting, XYZ.BasisZ);
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
            // 4. 创建垂直管道（可能导致 crossFitting 移动）
            Pipe verticalPipe = Pipe.Create(Document, pipe.PipeType.Id, pipe.ReferenceLevel.Id, crossVerticalConn, sprinklerConn);
            verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe.Diameter);

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
            Connector connector = GetConnectors(sp).FirstOrDefault();
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
                Connector newConn = FindMatchingConnector(newFitting, oldConn.CoordinateSystem.BasisZ);
                if (newConn != null)
                {
                    connectorMap.Add(oldConn, newConn);
                    // 同步连接器尺寸
                    Parameter oldDiamParam = GetAssociatedParameter(oldFitting, oldConn, BuiltInParameter.CONNECTOR_RADIUS);
                    if (oldDiamParam != null)
                    {
                        Parameter newDiamParam = GetAssociatedParameter(newFitting, newConn, BuiltInParameter.CONNECTOR_RADIUS);
                        newDiamParam?.Set(oldDiamParam.AsDouble());
                    }
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
        //通用的"任意接口对正水平管道"先水平旋转再根据轴垂直对正
        private void RotateFittingToAlignWithPipe(FamilyInstance fitting, Pipe pipe, FamilyInstance targetFitting, Line rotationAxis)
        {
            var ft1 = GetConnectors(targetFitting);
            // 获取管道曲线和方向
            var pipeCurve = ((LocationCurve)pipe.Location).Curve as Line;
            if (pipeCurve == null) return;
            XYZ pipeDirection = pipeCurve.Direction;
            // 获取管件所有连接器
            var fittingConns = GetConnectors(fitting);
            // 1. 水平面旋转（对齐管道方向）
            if (ft1.Count == 3) // 四通情况
            {
                // 计算需要在XY平面的旋转角度（使管件与管道走向对齐）
                XYZ projectedDirection = new XYZ(pipeDirection.X, pipeDirection.Y, 0).Normalize();
                double xyAngle = XYZ.BasisX.AngleTo(projectedDirection);

                // 判断旋转方向（顺时针/逆时针）
                if (projectedDirection.Y < 0) xyAngle = -xyAngle;

                // 执行水平面旋转
                Line zAxis = Line.CreateBound(
                    ((LocationPoint)targetFitting.Location).Point,
                    ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);

                ElementTransformUtils.RotateElement(Document, fitting.Id, zAxis, xyAngle);
            }
            else if (ft1.Count == 2) // 三通/弯头情况
            {

                // 6. 旋转三通对齐方向 
                // 获取管道端点和方向
                Line pipeLine = ((LocationCurve)pipe.Location).Curve as Line;
                var teeConns = GetConnectors(fitting);
                // 方法1：通过几何关系精准识别（推荐）
                Connector mainConn1 = null, mainConn2 = null, sideConn = null;
                // 找出距离最远的两个连接器作为主管道接口
                var farthestPair = teeConns.SelectMany(c1 => teeConns.Select(c2 => new { c1, c2, dist = c1.Origin.DistanceTo(c2.Origin) }))
                    .OrderByDescending(x => x.dist).First();
                mainConn1 = farthestPair.c1;
                mainConn2 = farthestPair.c2;
                // 剩余的是侧向接口
                sideConn = teeConns.FirstOrDefault(c => c.Id != mainConn1.Id && c.Id != mainConn2.Id);
                // 计算旋转轴（使用管道方向）
                Line rotationAxis2 = Line.CreateBound(((LocationPoint)targetFitting.Location).Point, ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);
                XYZ targetFittingCenter = ((LocationPoint)targetFitting.Location).Point;
                Connector pipeOpenEnd = pipe.ConnectorManager.Connectors.OfType<Connector>().OrderBy(c => c.Origin.DistanceTo(targetFittingCenter)).FirstOrDefault();
                if (sideConn == null || pipeOpenEnd == null) return;
                // 计算需要旋转的角度，目标与水平管道端头方向相反
                XYZ targetDirection = -pipeOpenEnd.CoordinateSystem.BasisZ;
                double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                // 确定旋转方向
                XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                if (cross.Z < 0) angle = -angle;
                //// 执行旋转
                ElementTransformUtils.RotateElement(Document, fitting.Id, rotationAxis2, angle);
                //ElementTransformUtils.RotateElement(Document, teeFitting.Id, rotationAxis, Math.PI / 2);
                ////处理类似操作


                //// 找出距离最远的两个连接器作为主管道接口
                //var farthestPair = fittingConns.SelectMany(c1 => fittingConns.Select(c2 => new { c1, c2, dist = c1.Origin.DistanceTo(c2.Origin) }))
                //    .OrderByDescending(x => x.dist).First();
                //// 剩余的是侧向接口
                //Connector sideConn = fittingConns.FirstOrDefault(c => c.Id != farthestPair.c1.Id && c.Id != farthestPair.c2.Id);
                //if (sideConn == null) return;
                //// 计算需要旋转的角度，目标与水平管道方向相反
                //XYZ targetDirection = -pipeDirection;
                //double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                //// 确定旋转方向
                //XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                //if (cross.Z < 0) angle = -angle;
                //// 执行Z轴旋转
                //Line zAxis = Line.CreateBound(((LocationPoint)targetFitting.Location).Point, ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);
                //ElementTransformUtils.RotateElement(Document, fitting.Id, zAxis, angle);
            }
            // 2. 绕管道轴的垂直旋转（统一处理）
            ElementTransformUtils.RotateElement(Document, fitting.Id, rotationAxis, Math.PI / 2);
        }
        //在管件原位建立管道默认的新管件样式
        private FamilyInstance CreateFitting(FamilyInstance targetFitting, Pipe pipe, int fittingType)
        {
            FamilySymbol symbol = GetFittingSymbol(pipe, fittingType);
            return Document.Create.NewFamilyInstance(((LocationPoint)targetFitting.Location).Point, symbol, targetFitting.Host, (Level)Document.GetElement(targetFitting.LevelId), StructuralType.NonStructural);
        }
        //找target连接器对应的水平管
        private Pipe GetHorizonPipe(Pipe pipe, List<Connector> ft1)
        {
            Pipe pipe2 = null;
            foreach (Connector hConn in ft1)
            {
                foreach (Connector refConn in hConn.AllRefs)
                {
                    if (!refConn.IsConnected) continue;
                    if (refConn.Owner is Pipe && refConn.Owner.Id != pipe.Id)
                    {
                        if (IsHorizontal((Pipe)refConn.Owner))
                        {
                            pipe2 = (Pipe)refConn.Owner;
                        }
                    }
                    else if (refConn.Owner is FamilyInstance fi)
                    {
                        var fitting = (FamilyInstance)refConn.Owner;
                        connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                        if (IsHorizontal(((Pipe)GetConnectedMEPCurve(fitting, fitting.Id))))
                        {
                            pipe2 = (Pipe)GetConnectedMEPCurve(fitting, fitting.Id);
                        }
                    }
                }
            }
            return pipe2;
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
        private FamilySymbol GetFittingSymbol(Pipe pipe, int connectorNum)
        {
            FamilySymbol familySymbol = null;
            switch (connectorNum)
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
            return familySymbol;
        }
        private Connector FindUnusedConnector(FamilyInstance fitting, XYZ preferredDirection)
        {
            ConnectorSet connectors = fitting.MEPModel.ConnectorManager.Connectors;
            foreach (Connector conn in connectors)
            {
                if (!conn.IsConnected)
                {
                    if (conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(preferredDirection))
                    {
                        return conn;
                    }
                }
            }
            // 如果没有完全匹配的，返回第一个未连接的
            foreach (Connector conn in connectors)
            {
                if (!conn.IsConnected) return conn;
            }
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
        private static List<Connector> GetConnectors(Element element)
        {
            List<Connector> result = new List<Connector>();
            if (element == null) return null;
            try
            {
                FamilyInstance fi = element as FamilyInstance;
                if (fi != null && fi.MEPModel != null)
                {
                    foreach (Connector item in fi.MEPModel.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
                MEPSystem system = element as MEPSystem;
                if (system != null)
                {
                    foreach (Connector item in system.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
                MEPCurve duct = element as MEPCurve;
                if (duct != null)
                {
                    foreach (Connector item in duct.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
            }
            catch (Exception)
            {

            }
            return null;
        }
        private Parameter GetAssociatedParameter(Element element, Connector connector, BuiltInParameter connectorParameter)
        {
            var connectorInfo = connector.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;

            if (connectorInfo == null)
                return null;

            var associatedFamilyParameterId = connectorInfo.GetAssociateFamilyParameterId(new ElementId(connectorParameter));

            if (associatedFamilyParameterId == ElementId.InvalidElementId)
                return null;

            var document = element.Document;

            var parameterElement = document.GetElement(associatedFamilyParameterId) as ParameterElement;

            if (parameterElement == null)
                return null;

            var paramterDefinition = parameterElement.GetDefinition();

            return element.get_Parameter(paramterDefinition);
        }
        private Connector FindMatchingConnector(FamilyInstance fitting, XYZ direction)
        {
            ConnectorSet connectors = fitting.MEPModel.ConnectorManager.Connectors;
            foreach (Connector conn in connectors)
            {
                if (conn.CoordinateSystem.BasisZ.IsAlmostEqualTo(direction))
                {
                    return conn;
                }
            }
            return null;
        }
        private bool IsHorizontal(Pipe pipe)
        {
            //if (!(mepCurve.Location is LocationCurve locationCurve))
            //    return false;
            LocationCurve locationCurve = (LocationCurve)pipe.Location;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);

            // 计算方向向量并归一化
            XYZ direction = (end - start).Normalize();

            // 判断Z分量是否接近0
            return Math.Abs(direction.Z) < 0.1;
        }
        List<ElementId> ConnectedDoubleUpSprinkler = new List<ElementId>();
        List<ElementId> ConnectedUpSprinkler = new List<ElementId>();
        List<ElementId> ConnectedDoubleDownSprinkler = new List<ElementId>();
        List<ElementId> ConnectedDownSprinkler = new List<ElementId>();
        public ICommand SelectUnconnctedUpCommand => new BaseBindingCommand(SelectUnconnctedUp);
        private void SelectUnconnctedUp(object obj)
        {
            Selection select = Application.ActiveUIDocument.Selection;
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
            Selection select = Application.ActiveUIDocument.Selection;
            List<ElementId> unConnectedDown = new List<ElementId>();
            foreach (var elemId in AllDownSprinkler)
            {
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elemId);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                if (!connector.IsConnected) unConnectedDown.Add(elemId);
            }
            select.SetElementIds(unConnectedDown);
        }
        public ICommand DisconnectSpCommand => new BaseBindingCommand(DisconnectSp);
        private Outline CreateSearchOutline(XYZ centerPoint, double searchRadius)
        {
            XYZ min = new XYZ(centerPoint.X - searchRadius, centerPoint.Y - searchRadius, centerPoint.Z - searchRadius);
            XYZ max = new XYZ(centerPoint.X + searchRadius, centerPoint.Y + searchRadius, centerPoint.Z + searchRadius);
            return new Outline(min, max);
        }
        private void DisconnectSp(object obj)
        {
            //处理已连接喷头删除连接的管件，或者已断开但貌似连接管件
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("删除喷头连接的管件");
                try
                {
                    Reference r = Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                    FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(r);
                    var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    if (!connector.IsConnected)
                    {
                        XYZ sprinklerConnectorPoint = connector.Origin;
                        // 创建空间过滤器以提高搜索效率，搜索半径0.3
                        Outline searchOutline = CreateSearchOutline(sprinklerConnectorPoint, 0.3);
                        BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchOutline);
                        // 使用优化的收集器查找附近的管件
                        var nearestUnconnectedFitting = new FilteredElementCollector(Document, Document.ActiveView.Id).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_PipeFitting).WherePasses(bboxFilter).Cast<FamilyInstance>().Where(fi => fi.MEPModel?.ConnectorManager != null).SelectMany(fi => fi.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                                .Where(c => !c.IsConnected)
                                .Select(c => new { Fitting = fi, Connector = c, Distance = c.Origin.DistanceTo(sprinklerConnectorPoint) })).OrderBy(x => x.Distance).FirstOrDefault();
                        if (nearestUnconnectedFitting != null)
                        {
                            connectedSpFitting = nearestUnconnectedFitting.Fitting.Id;
                        }
                        else
                        {
                            TaskDialog.Show("提示", "未找到附近可连接的管件");
                        }
                    }
                    else
                    {
                        foreach (Connector refConn in connector.AllRefs)
                        {
                            if (refConn.Owner?.Id == sprinkler.Id) continue;
                            else if (refConn.Owner is FamilyInstance fi)
                            {
                                var fitting = (FamilyInstance)refConn.Owner;
                                connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                            }
                        }
                    }
                    Document.Delete(connectedSpFitting);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    TaskDialog.Show("错误", "删除喷头连接的管件失败: " + ex.Message);
                }
            }
        }
        public ICommand ConnectSpCommand => new BaseBindingCommand(ConnectSp);
        private void ConnectSp(object obj)
        {
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("喷头连接管道");
                try
                {
                    Reference r = Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                    FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(r);
                    var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    Pipe pipe = null;
                    if (connector.IsConnected) return;
                    XYZ sprinklerConnectorPoint = connector.Origin;
                    // 创建空间过滤器以提高搜索效率，搜索半径0.3
                    Outline searchOutline = CreateSearchOutline(sprinklerConnectorPoint, 0.3);
                    BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchOutline);
                    // 使用优化的收集器查找附近的管 
                    pipe = new FilteredElementCollector(Document).OfClass(typeof(Pipe)).WherePasses(bboxFilter).Cast<Pipe>().FirstOrDefault();
                    //TaskDialog.Show("tt", pipe.Id.ToString());
                    //连接喷头要考虑喷头位置微差，管道是否垂直，上下喷头，尽量封装方法
                    ConnectSprinkler(sprinkler, pipe);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    TaskDialog.Show("错误", "喷头连接管道失败: " + ex.Message);
                }
            }
        }
        public ICommand FixVerticalPipeCommand => new BaseBindingCommand(FixVerticalPipe);
        public ElementId connectedSpFitting { get; set; }
        private void FixVerticalPipe(object obj)
        {
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("调整立管垂直");
                Reference r = Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(r);
                var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                Pipe pipe = null;
                if (!connector.IsConnected)
                {
                    return;
                }
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
                        connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                        pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                    }
                    if (pipe == null) continue;
                }
                Document.Delete(connectedSpFitting);
                ////查找管道开放端头，修改坐标XY
                try
                {
                    ConnectSprinkler(sprinkler, pipe);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    TaskDialog.Show("错误", "调整立管位置失败: " + ex.Message);
                }
            }
        }
        private void ConnectSprinkler(FamilyInstance sprinkler, Pipe pipe)
        {
            var curve = ((LocationCurve)pipe.Location).Curve as Line;
            if (Math.Abs(curve.Direction.Z) != 1)
            {
                XYZ fixedEndPoint = null;
                XYZ moveEndPoint = null;
                var connectors = pipe.ConnectorManager.Connectors.OfType<Connector>().ToList();
                foreach (var connector2 in connectors)
                {
                    if (connector2.IsConnected)
                    {
                        fixedEndPoint = connector2.Origin;
                    }
                    else moveEndPoint = connector2.Origin;
                }

                // 创建新的端点坐标（保持Z值不变，XY取固定端值）
                XYZ newMoveEndPoint = new XYZ(fixedEndPoint.X, fixedEndPoint.Y, moveEndPoint.Z);
                LocationCurve locationCurve = pipe.Location as LocationCurve;
                Line currentLine = locationCurve.Curve as Line;
                double distanceToStart = fixedEndPoint.DistanceTo(currentLine.GetEndPoint(0));
                double distanceToEnd = fixedEndPoint.DistanceTo(currentLine.GetEndPoint(1));
                // 更新曲线端点
                if (distanceToStart < distanceToEnd)
                {
                    // 固定端是起点，移动终点
                    Line newLine = Line.CreateBound(currentLine.GetEndPoint(0), newMoveEndPoint);
                    locationCurve.Curve = newLine;
                }
                else
                {
                    // 固定端是终点，移动起点
                    Line newLine = Line.CreateBound(newMoveEndPoint, currentLine.GetEndPoint(1));
                    locationCurve.Curve = newLine;
                }
            }
            ReconnectSprinklers(new List<ElementId>() { sprinkler.Id });
        }
        public ICommand SwitchSprinklerCommand => new BaseBindingCommand(SwitchSprinkler);
        private void SwitchSprinkler(object obj)
        {
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("替换喷头");
                try
                {
                    Selection sel = Application.ActiveUIDocument.Selection;
                    IList<FamilyInstance> sprinklers = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头").Cast<FamilyInstance>().ToList();
                    //var sprinklers = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                    var removeFittingIds = new List<ElementId>();
                    var connectSpIds = new List<ElementId>();
                    int processSp = 0;
                    // 处理喷头替换
                    foreach (var sprinkler in sprinklers)
                    {
                        var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>()?.FirstOrDefault();
                        if (connector == null) continue;
                        // 处理需要替换的喷头
                        if (connector.IsConnected && sprinkler.Symbol?.Family != null && sprinkler.Symbol.Family.Id != SelectedUpSp.Id && sprinkler.Symbol.Family.Id != SelectedDownSp.Id)
                        {
                            ProcessConnectedSprinkler(sprinkler, connector, removeFittingIds, connectSpIds);
                            processSp++;
                        }
                        // 处理未连接但需要类型调整的喷头
                        if (!connector.IsConnected)
                        {
                            ReplaceSprinklerType(sprinkler, connector);
                            processSp++;
                        }
                    }
                    // 批量删除管件
                    BatchDeleteElements(removeFittingIds);
                    // 重新连接喷头
                    ReconnectSprinklers(connectSpIds);
                    tx.Commit();
                    TaskDialog.Show("tt", $"已完成替换喷头{processSp}个");
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    TaskDialog.Show("错误", ex.Message);
                }
            }
        }
        private void ProcessConnectedSprinkler(FamilyInstance sprinkler, Connector connector, List<ElementId> removeFittingIds, List<ElementId> connectSpIds)
        {
            // 断开连接并记录要删除的管件
            foreach (Connector connectedConnector in connector.AllRefs)
            {
                if (connectedConnector.Owner.Id != connector.MEPSystem?.Id &&
                    connectedConnector.Owner.IsValidObject)
                {
                    connector.DisconnectFrom(connectedConnector);
                    removeFittingIds.Add(connectedConnector.Owner.Id);
                }
            }
            // 替换喷头类型
            ReplaceSprinklerType(sprinkler, connector);
            // 记录需要重新连接的喷头
            connectSpIds.Add(sprinkler.Id);
        }
        private void ReplaceSprinklerType(FamilyInstance sprinkler, Connector connector)
        {
            var direction = GetConnectorDirection(connector);

            if (direction == ConnectorDirection.Up && sprinkler.Symbol.Family.Id != SelectedUpSp.Id)
            {
                sprinkler.ChangeTypeId(SelectedUpSp.GetFamilySymbolIds().First());
            }
            else if (direction == ConnectorDirection.Down && sprinkler.Symbol.Family.Id != SelectedDownSp.Id)
            {
                sprinkler.ChangeTypeId(SelectedDownSp.GetFamilySymbolIds().First());
            }
        }
        private ConnectorDirection GetConnectorDirection(Connector connector)
        {
            double z = connector.CoordinateSystem.BasisZ.Z;
            return z.ToString() == "-1" ? ConnectorDirection.Up : ConnectorDirection.Down;
        }
        private enum ConnectorDirection { Up, Down }
        private void BatchDeleteElements(List<ElementId> elementIds)
        {
            foreach (var id in elementIds.Distinct())
            {
                try
                {
                    if (Document.GetElement(id) != null)
                    {
                        Document.Delete(id);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"删除元素 {id} 失败: {ex.Message}");
                }
            }
        }
        private void ReconnectSprinklers(List<ElementId> sprinklerIds)
        {
            foreach (var id in sprinklerIds)
            {
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(id);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().FirstOrDefault();
                //喷头连接
                IList<Connector> conn1 = new List<Connector>();
                IList<Connector> ppconnlist = new List<Connector>();
                IList<Element> pipe1 = new List<Element>();
                conn1.Add(connector);
                BoundingBoxXYZ box = sprinkler.get_BoundingBox(Document.ActiveView);//用喷头的范围框快速过滤
                double Maxx = box.Max.X;
                double Maxy = box.Max.Y;
                double Minx = box.Min.X;
                double Miny = box.Min.Y;
                double MZz = box.Max.Z;
                Outline myOutLn = null;
                //判断上喷
                if (sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First().CoordinateSystem.BasisZ.Z.ToString() == "-1")
                {
                    myOutLn = new Outline(new XYZ(Minx, Miny, MZz - (200 / 304.8)), new XYZ(Maxx, Maxy, MZz - (200 / 304.8)));
                }
                else
                {
                    myOutLn = new Outline(new XYZ(Minx, Miny, MZz + (50 / 304.8)), new XYZ(Maxx, Maxy, MZz + (50 / 304.8)));
                }
                //用喷头的范围框过滤管道
                BoundingBoxIntersectsFilter boxee = new BoundingBoxIntersectsFilter(myOutLn);
                FilteredElementCollector collector = new FilteredElementCollector(Document, Document.ActiveView.Id);
                collector.OfClass(typeof(Pipe));
                //与喷头范围框相交的管道
                pipe1 = collector.WherePasses(boxee).ToElements();
                foreach (Element elp in pipe1)
                {
                    Pipe pp = elp as Pipe;
                    ConnectorSetIterator ppconn = pp.ConnectorManager.Connectors.ForwardIterator();
                    while (ppconn.MoveNext())
                    {
                        Connector ppconn2 = ppconn.Current as Connector;
                        if (ppconn2.IsConnected == false)
                        {
                            ppconnlist.Add(ppconn2);
                            // 在此处执行连接操作（如NewTransitionFitting）
                            Document.Create.NewTransitionFitting(ppconn2, connector);
                        }
                    }
                }
            }
        }
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
    }
    //自定义比较器实现去重
    public class FamilyComparer : IEqualityComparer<Family>
    {
        public bool Equals(Family x, Family y) => x?.Id == y?.Id;
        public int GetHashCode(Family obj) => obj.Id.GetHashCode();
    }
}
