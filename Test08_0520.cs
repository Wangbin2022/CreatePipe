using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommandLine;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.NCCoding;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test08_0520 : IExternalCommand
    {
        // 从管件获取连接的管道（排除喷头自身）
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
        // 判断管道是否垂直（Z方向分量接近±1）
        private bool IsVertical(MEPCurve mepCurve)
        {
            var curve = ((LocationCurve)mepCurve.Location).Curve as Line;
            if (curve == null) return false;
            return Math.Abs(curve.Direction.Z) > 0.99;
            //var direction = curve.Direction.Normalize();
            //return Math.Abs(direction.Z) > 0.99; 
        }
        private bool IsHorizontal(MEPCurve mepCurve)
        {
            //if (!(mepCurve.Location is LocationCurve locationCurve))
            //    return false;
            LocationCurve locationCurve = (LocationCurve)mepCurve.Location;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);

            // 计算方向向量并归一化
            XYZ direction = (end - start).Normalize();

            // 判断Z分量是否接近0
            return Math.Abs(direction.Z) < 0.1;
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
        public ElementId connectedSpFitting { get; set; }
        public Document Document { get; set; }
        public UIDocument UIDocument { get; set; }
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
        // 创建搜索范围的方法
        private Outline CreateSearchOutline(XYZ centerPoint, double searchRadius)
        {
            XYZ min = new XYZ(centerPoint.X - searchRadius, centerPoint.Y - searchRadius, centerPoint.Z - searchRadius);
            XYZ max = new XYZ(centerPoint.X + searchRadius, centerPoint.Y + searchRadius, centerPoint.Z + searchRadius);
            return new Outline(min, max);
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
        //Connector FindMatchingConnector(FamilyInstance fitting, Connector sourceConn)
        //{
        //    XYZ sourceDirection = sourceConn.CoordinateSystem.BasisZ;
        //    bool isVertical = Math.Abs(sourceDirection.Z) > 0.9; // 判断是否垂直连接器

        //    return fitting.MEPModel.ConnectorManager.Connectors
        //        .OfType<Connector>()
        //        .OrderBy(c =>
        //        {
        //            // 优先匹配方向
        //            double angle = c.CoordinateSystem.BasisZ.AngleTo(sourceDirection);

        //            // 如果是垂直连接器，额外检查坐标系Y轴方向
        //            if (isVertical)
        //            {
        //                angle += c.CoordinateSystem.BasisY.AngleTo(sourceConn.CoordinateSystem.BasisY);
        //            }
        //            return angle;
        //        })
        //        .FirstOrDefault(c => !c.IsConnected || c == sourceConn); // 允许重用已连接端口
        //}
        // 辅助函数：查找未使用的连接器（用于新喷头连接）
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
        //public void SelectMEPfittingSize(FamilyInstance element)
        //{
        //    string str = "";
        //    var connectors = GetConnectors(element);
        //    if (connectors != null)
        //    {
        //        str += $"Element: {element.Id}\n";
        //        foreach (Connector connector in connectors)
        //        {
        //            var parDId = GetAssociatedParameter(element, connector, BuiltInParameter.CONNECTOR_DIAMETER);
        //            if (parDId != null)
        //            {
        //                double d = parDId.AsDouble() * 304.8;
        //                str += $"\t Diameter: {parDId.Id}={d}\n";
        //            }
        //            var parRId = GetAssociatedParameter(element, connector, BuiltInParameter.CONNECTOR_RADIUS);
        //            if (parRId != null)
        //            {
        //                //str += $"\t Radius: {parRId.Id}\n";
        //                double r = parRId.AsDouble() * 304.8;
        //                str += $"\t Radius: {parRId.Id}={r}\n";
        //            }
        //        }
        //    }
        //    TaskDialog.Show("GetAssociatedParameter", str);
        //}
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
        private Connector defaultConnector;
        public Family SelectedUpSp { get; set; }
        public Family SelectedDownSp { get; set; }
        private bool IsDirectSprinklerFitting(FamilyInstance fitting, FamilyInstance sprinkler, FamilyInstance connSprinkler)
        {
            //// 条件1：检查几何距离（喷头与管件的距离应大于接管长度）
            //double distance = fitting.GetTransform().Origin.DistanceTo(sprinkler.GetTransform().Origin);
            //if (distance < 1.0) // 1英尺≈300mm，可根据实际调整
            //    return true;
            //// 条件2：检查连接器直接连通性
            //var fittingConns = fitting.MEPModel?.ConnectorManager?.Connectors;
            //if (fittingConns != null)
            //{
            //    foreach (Connector fc in fittingConns)
            //    {
            //        if (fc.AllRefs.Cast<Connector>().Any(c => c.Owner?.Id == sprinkler.Id))
            //            return true;
            //    }
            //}
            if (fitting.Id == sprinkler.Id || fitting.Id == connSprinkler.Id)
            {
                return true;
            }
            else return false;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            UIDocument = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            Document = uiDoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
            foreach (Element elem in collector)
            {
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elem.Id);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
                bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
                if (isDown)
                {
                    SelectedDownSp = sprinkler.Symbol.Family;
                }
                else if (isUp)
                {
                    SelectedUpSp = sprinkler.Symbol.Family;
                }
            }

            //0605 XAML
            //SprinklerReplaceView sprinklerReplace = new SprinklerReplaceView(uiApp);
            //sprinklerReplace.ShowDialog();

            ////0610 查找管件连接器尺寸.OK
            //Reference r2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPFitting(), "pick something");
            //var targetFitting = (FamilyInstance)doc.GetElement(r2);
            ////查找连接器尺寸方法，对管道连接器只有半径值没有直径
            //SelectMEPfittingSize(targetFitting);
            ////0609 默认管道三通转四通处理
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("点击改上下喷");
                //0610 拾取喷头识别相关
                Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                FamilyInstance sp = (FamilyInstance)doc.GetElement(r);
                FamilyInstance targetFitting = null;
                double deltaHeight = 0;
                Pipe pipe = null;
                var connector = sp.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                bool isDown = connector.CoordinateSystem.BasisZ.Z.ToString() == "1";
                bool isUp = connector.CoordinateSystem.BasisZ.Z.ToString() == "-1";
                if (!connector.IsConnected) return Result.Cancelled;
                foreach (Connector refConn in connector.AllRefs)
                {
                    if (refConn.Owner?.Id == sp.Id) continue;

                    // 获取连接的管道或管件
                    if (refConn.Owner is Pipe)
                    {
                        pipe = (Pipe)refConn.Owner;
                    }
                    else if (refConn.Owner is FamilyInstance fi)
                    {
                        var fitting = (FamilyInstance)refConn.Owner;
                        defaultConnector = refConn;
                        pipe = (Pipe)GetConnectedMEPCurve(fitting, sp.Id);
                    }

                    if (pipe == null) continue;
                    // 查找管道另一端的有效管件
                    foreach (Connector pipeConn in pipe.ConnectorManager.Connectors)
                    {
                        foreach (Connector refConn2 in pipeConn.AllRefs)
                        {
                            // 排除条件（关键修改点）
                            if (refConn2.Owner?.Id == sp.Id || refConn2.Owner?.Id == refConn.Owner.Id || !(refConn2.Owner is FamilyInstance))
                                continue;
                            var candidateFitting = (FamilyInstance)refConn2.Owner;
                            // 新增过滤：确保不是喷头直接连接的管件
                            if (!IsDirectSprinklerFitting(candidateFitting, sp, (FamilyInstance)refConn.Owner))
                            {
                                targetFitting = candidateFitting;
                                deltaHeight = Math.Abs((sp.GetTransform().Origin.Z - candidateFitting.GetTransform().Origin.Z) * 304.8);
                                break; // 找到后立即退出循环
                            }
                        }
                        if (targetFitting != null) break;
                    }
                    if (targetFitting != null) break;
                }
                // 2. 获取关键连接器
                var ft1 = targetFitting.MEPModel.ConnectorManager.Connectors?.OfType<Connector>().ToList();
                //处理三通改四通
                if (ft1.Count == 3)
                {
                    // 3. 获取水平连接器对,创建旋转轴
                    var horizontalConnectors = ft1.GroupBy(c => c.Origin.Z).Where(g => g.Count() >= 2).FirstOrDefault()?.ToList();
                    if (horizontalConnectors == null || horizontalConnectors.Count != 2) return Result.Failed;
                    Line rotationAxis = Line.CreateBound(horizontalConnectors[0].Origin, horizontalConnectors[1].Origin);
                    // 5. 创建四通
                    FamilySymbol crossSymbol = (FamilySymbol)doc.GetElement(pipe.PipeType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Crosses, 0).MEPPartId);
                    FamilyInstance crossFitting = doc.Create.NewFamilyInstance(((LocationPoint)targetFitting.Location).Point, crossSymbol, targetFitting.Host, (Level)doc.GetElement(targetFitting.LevelId), StructuralType.NonStructural);
                    //// 6. 旋转四通对齐方向，当管线非正交时先XY轴旋转符合管道走向
                    //找三通水平连接器对应的水平管
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
                    // 获取管道曲线方向
                    Line pipeLine = ((LocationCurve)pipe2.Location).Curve as Line;
                    XYZ pipeDirection = pipeLine.Direction;
                    // 6.2 计算需要在XY平面的旋转角度（使四通与管道走向对齐）
                    XYZ projectedDirection = new XYZ(pipeDirection.X, pipeDirection.Y, 0).Normalize();
                    double xyAngle = XYZ.BasisX.AngleTo(projectedDirection);
                    // 判断旋转方向（顺时针/逆时针）
                    if (projectedDirection.Y < 0) xyAngle = -xyAngle;
                    // 6.3 执行水平面旋转
                    Line zAxis = Line.CreateBound(((LocationPoint)targetFitting.Location).Point, ((LocationPoint)targetFitting.Location).Point + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, crossFitting.Id, zAxis, xyAngle);
                    ElementTransformUtils.RotateElement(doc, crossFitting.Id, rotationAxis, Math.PI / 2);
                    // 7. 转移连接并同步尺寸
                    Dictionary<Connector, Connector> connectorMap = new Dictionary<Connector, Connector>();
                    foreach (Connector oldConn in targetFitting.MEPModel.ConnectorManager.Connectors)
                    {
                        // 在四通上找到方向最接近的连接器
                        //Connector newConn = FindMatchingConnector(crossFitting, oldConn);
                        Connector newConn = FindMatchingConnector(crossFitting, oldConn.CoordinateSystem.BasisZ);
                        if (newConn != null)
                        {
                            connectorMap.Add(oldConn, newConn);
                            // 同步连接器尺寸
                            Parameter oldDiamParam = GetAssociatedParameter(targetFitting, oldConn, BuiltInParameter.CONNECTOR_RADIUS);
                            if (oldDiamParam != null)
                            {
                                Parameter newDiamParam = GetAssociatedParameter(crossFitting, newConn, BuiltInParameter.CONNECTOR_RADIUS);
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
                    //9. 获取原喷头位置和fitting位置取新喷头定位
                    XYZ originalLocation = sp.GetTransform().Origin;
                    XYZ fittingLocation = targetFitting.GetTransform().Origin;
                    // 计算新Z坐标（根据上下喷方向决定偏移方向）,创建新位置
                    double zOffset = isDown ? (deltaHeight / 304.8) : -(deltaHeight / 304.8);
                    double newZ = fittingLocation.Z + zOffset;
                    XYZ newLocation = new XYZ(originalLocation.X, originalLocation.Y, newZ);
                    var newSprinklerId = ElementTransformUtils.CopyElement(Document, sp.Id, newLocation - originalLocation).FirstOrDefault();
                    var newSprinkler = (FamilyInstance)Document.GetElement(newSprinklerId);
                    // 根据上下喷方向决定喷头样式
                    newSprinkler.ChangeTypeId(isDown ? SelectedUpSp.GetFamilySymbolIds().First() : SelectedDownSp.GetFamilySymbolIds().First());
                    var newConnector = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    Connector sprinklerConn = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    Connector crossVerticalConn = FindUnusedConnector(crossFitting, XYZ.BasisZ);
                    // 1. 首先修改记录方式 - 记录完整的连接关系
                    Dictionary<Connector, List<Connector>> originalConnections = new Dictionary<Connector, List<Connector>>();
                    // 获取水平连接器
                    var horizontalConns = crossFitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>().Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.1).ToList();
                    // 2. 记录原始连接关系（在断开前）
                    foreach (Connector hConn in horizontalConns)
                    {
                        originalConnections[hConn] = hConn.AllRefs.Cast<Connector>().ToList();
                    }
                    // 3. 创建垂直管（此时水平管网不会移动）
                    Pipe verticalPipe = Pipe.Create(doc, pipe.PipeType.Id, pipe.ReferenceLevel.Id, crossVerticalConn, sprinklerConn);
                    verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe.Diameter);
                    // 4. 恢复连接的完整流程
                    // 第一步：恢复所有元素的位置
                    foreach (var hConn in horizontalConns)
                    {
                        if (originalConnections.TryGetValue(hConn, out var connectedConns))
                        {
                            foreach (var originalConn in connectedConns)
                            {
                                // 恢复位置
                                XYZ offset = originalConn.Origin - hConn.Origin;
                                if (offset.GetLength() > 0.001)
                                {
                                    if (originalConn.Owner is MEPCurve pipe3)
                                    {
                                        ElementTransformUtils.MoveElement(doc, pipe3.Id, offset);
                                    }
                                    else if (originalConn.Owner is FamilyInstance fitting)
                                    {
                                        ElementTransformUtils.MoveElement(doc, fitting.Id, offset);
                                    }
                                }
                            }
                        }
                    }
                    // 第二步：重新建立连接
                    foreach (var hConn in horizontalConns)
                    {
                        if (originalConnections.TryGetValue(hConn, out var connectedConns))
                        {
                            foreach (var originalConn in connectedConns)
                            {
                                if (!originalConn.IsConnected && !hConn.IsConnected)
                                {
                                    try
                                    {
                                        originalConn.ConnectTo(hConn);
                                    }
                                    catch
                                    {
                                        // 如果正向连接失败，尝试反向连接
                                        hConn.ConnectTo(originalConn);
                                    }
                                }
                            }
                        }
                    }
                    doc.Delete(targetFitting.Id);
                }
                //处理弯头转垂直三通
                else if (ft1.Count == 2)
                {
                    Pipe pipe2 = null;
                    bool foundPipe = false;
                    // 1. 从弯头获取水平管道
                    foreach (Connector hConn in ft1)
                    {
                        foreach (Connector refConn in hConn.AllRefs)
                        {
                            if (refConn.IsConnected && refConn.Owner is Pipe p && p.Id != pipe.Id)
                            {
                                pipe2 = p;
                                foundPipe = true;
                                break;
                            }
                            else if (refConn.IsConnected && refConn.Owner is FamilyInstance fi)
                            {
                                var connectedPipe = GetConnectedMEPCurve(fi, sp.Id) as Pipe;
                                connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                                if (connectedPipe != null && connectedPipe.Id != pipe.Id && IsHorizontal(connectedPipe))
                                {
                                    pipe2 = connectedPipe;
                                    foundPipe = true;
                                }
                                break;
                            }
                        }
                        if (foundPipe) break;
                    }
                    if (pipe2 == null)
                    {
                        XYZ searchCenter = ((LocationPoint)targetFitting.Location).Point;
                        Outline searchBox = CreateSearchOutline(searchCenter, 1);
                        BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchBox);
                        var collector2 = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).WherePasses(bboxFilter)
                            .Where(e => e.Id != pipe.Id).Where(p => IsHorizontal((Pipe)p)).FirstOrDefault();
                        pipe2 = (Pipe)collector2;
                        if (collector2 == null)
                        {
                            TaskDialog.Show("错误", "未找到有效的关联管道");
                            return Result.Failed;
                        }
                    }
                    // 3. 安全获取旋转轴
                    Line rotationAxis = null;
                    try
                    {
                        if (pipe2.Location is LocationCurve locationCurve &&
                            locationCurve.Curve is Line line)
                        {
                            rotationAxis = Line.CreateBound(line.GetEndPoint(0), line.GetEndPoint(1));
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("错误", $"获取旋转轴失败: {ex.Message}");
                        return Result.Failed;
                    }
                    if (rotationAxis == null)
                    {
                        TaskDialog.Show("错误", "无法确定旋转轴");
                        return Result.Failed;
                    }
                    Pipe pipeHorizonl = pipe2;
                    FamilySymbol teeSymbol = (FamilySymbol)doc.GetElement(pipe.PipeType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Junctions, 0).MEPPartId);
                    FamilyInstance teeFitting = doc.Create.NewFamilyInstance(((LocationPoint)targetFitting.Location).Point, teeSymbol, targetFitting.Host, (Level)doc.GetElement(targetFitting.LevelId), StructuralType.NonStructural);
                    // 6. 旋转三通对齐方向 
                    // 获取管道端点和方向
                    Line pipeLine = ((LocationCurve)pipeHorizonl.Location).Curve as Line;
                    var teeConns = GetConnectors(teeFitting);
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

                    //先删掉弯头给线头，不然没法寻线？
                    //// 找出管道的开放端连接器，改为距离最近的Connector
                    ///// 获取目标管件的中心点
                    XYZ targetFittingCenter = ((LocationPoint)targetFitting.Location).Point;
                    // 找出管道上距离目标管件最近的连接器（无论是否已连接）
                    Connector pipeOpenEnd = pipeHorizonl.ConnectorManager.Connectors.OfType<Connector>().OrderBy(c => c.Origin.DistanceTo(targetFittingCenter)).FirstOrDefault();
                    //Connector pipeOpenEnd = pipeHorizonl.ConnectorManager.Connectors.OfType<Connector>().FirstOrDefault(c => !c.IsConnected);
                    if (sideConn == null || pipeOpenEnd == null) return Result.Failed;
                    // 计算需要旋转的角度，目标与水平管道端头方向相反
                    XYZ targetDirection = -pipeOpenEnd.CoordinateSystem.BasisZ;
                    double angle = sideConn.CoordinateSystem.BasisZ.AngleTo(targetDirection);
                    // 确定旋转方向
                    XYZ cross = sideConn.CoordinateSystem.BasisZ.CrossProduct(targetDirection);
                    if (cross.Z < 0) angle = -angle;
                    //// 执行旋转
                    ElementTransformUtils.RotateElement(doc, teeFitting.Id, rotationAxis2, angle);
                    ElementTransformUtils.RotateElement(doc, teeFitting.Id, rotationAxis, Math.PI / 2);
                    //处理类似操作

                    // 7. 转移连接并同步尺寸
                    Dictionary<Connector, Connector> connectorMap = new Dictionary<Connector, Connector>();
                    foreach (Connector oldConn in targetFitting.MEPModel.ConnectorManager.Connectors)
                    {
                        // 在新三通上找到方向最接近的连接器
                        Connector newConn = FindMatchingConnector(teeFitting, oldConn.CoordinateSystem.BasisZ);
                        if (newConn != null)
                        {
                            connectorMap.Add(oldConn, newConn);
                            // 同步连接器尺寸
                            Parameter oldDiamParam = GetAssociatedParameter(targetFitting, oldConn, BuiltInParameter.CONNECTOR_RADIUS);
                            if (oldDiamParam != null)
                            {
                                Parameter newDiamParam = GetAssociatedParameter(teeFitting, newConn, BuiltInParameter.CONNECTOR_RADIUS);
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
                    //9. 获取原喷头位置和fitting位置取新喷头定位
                    XYZ originalLocation = sp.GetTransform().Origin;
                    XYZ fittingLocation = targetFitting.GetTransform().Origin;
                    // 计算新Z坐标（根据上下喷方向决定偏移方向）,创建新位置
                    double zOffset = isDown ? (deltaHeight / 304.8) : -(deltaHeight / 304.8);
                    double newZ = fittingLocation.Z + zOffset;
                    XYZ newLocation = new XYZ(originalLocation.X, originalLocation.Y, newZ);
                    var newSprinklerId = ElementTransformUtils.CopyElement(Document, sp.Id, newLocation - originalLocation).FirstOrDefault();
                    var newSprinkler = (FamilyInstance)Document.GetElement(newSprinklerId);
                    // 根据上下喷方向决定喷头样式
                    newSprinkler.ChangeTypeId(isDown ? SelectedUpSp.GetFamilySymbolIds().First() : SelectedDownSp.GetFamilySymbolIds().First());
                    var newConnector = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    Connector sprinklerConn = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    Connector teeVerticalConn = FindUnusedConnector(teeFitting, XYZ.BasisZ);
                    // 1. 首先修改记录方式 - 记录完整的连接关系
                    Dictionary<Connector, List<Connector>> originalConnections = new Dictionary<Connector, List<Connector>>();
                    // 获取水平连接器
                    var horizontalConns = teeFitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>().Where(c => Math.Abs(c.CoordinateSystem.BasisZ.Z) < 0.1).ToList();
                    // 2. 记录原始连接关系（在断开前）
                    foreach (Connector hConn in horizontalConns)
                    {
                        originalConnections[hConn] = hConn.AllRefs.Cast<Connector>().ToList();
                    }
                    // 3. 创建垂直管（此时水平管网不会移动）
                    Pipe verticalPipe = Pipe.Create(doc, pipe.PipeType.Id, pipe.ReferenceLevel.Id, teeVerticalConn, sprinklerConn);
                    verticalPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(pipe.Diameter);
                    //// 4. 恢复连接的完整流程
                    //// 第一步：恢复所有元素的位置
                    foreach (var hConn in horizontalConns)
                    {
                        if (originalConnections.TryGetValue(hConn, out var connectedConns))
                        {
                            foreach (var originalConn in connectedConns)
                            {
                                // 恢复位置
                                XYZ offset = originalConn.Origin - hConn.Origin;
                                if (offset.GetLength() > 0.001)
                                {
                                    if (originalConn.Owner is MEPCurve pipe3)
                                    {
                                        ElementTransformUtils.MoveElement(doc, pipe3.Id, offset);
                                    }
                                    else if (originalConn.Owner is FamilyInstance fitting)
                                    {
                                        ElementTransformUtils.MoveElement(doc, fitting.Id, offset);
                                    }
                                }
                            }
                        }
                    }
                    // 第二步：重新建立连接
                    foreach (var hConn in horizontalConns)
                    {
                        if (originalConnections.TryGetValue(hConn, out var connectedConns))
                        {
                            foreach (var originalConn in connectedConns)
                            {
                                if (!originalConn.IsConnected && !hConn.IsConnected)
                                {
                                    try
                                    {
                                        originalConn.ConnectTo(hConn);
                                    }
                                    catch
                                    {
                                        // 如果正向连接失败，尝试反向连接
                                        hConn.ConnectTo(originalConn);
                                    }
                                }
                            }
                        }
                    }
                    doc.Delete(targetFitting.Id);
                    if (connectedSpFitting != null)
                    {
                        doc.Delete(connectedSpFitting);
                    }
                }
                tx.Commit();
                //0608 连接喷头要考虑喷头位置微差，管道是否垂直，调用其他方法

                ////0608 处理已连接喷头删除连接的管件，或者已断开但貌似连接管件
                //using (Transaction tx = new Transaction(doc))
                //{
                //    tx.Start("删除喷头连接的管件");
                //    try
                //    {
                //        Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                //        FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r);
                //        var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                //        Pipe pipe = null;
                //        if (!connector.IsConnected)
                //        {
                //            XYZ sprinklerConnectorPoint = connector.Origin;
                //            // 创建空间过滤器以提高搜索效率
                //            Outline searchOutline = CreateSearchOutline(sprinklerConnectorPoint, 0.3); // 5米搜索半径
                //            BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchOutline);
                //            // 使用优化的收集器查找附近的管件
                //            var nearestUnconnectedFitting = new FilteredElementCollector(doc, doc.ActiveView.Id)
                //                .OfClass(typeof(FamilyInstance))
                //                .OfCategory(BuiltInCategory.OST_PipeFitting)
                //                .WherePasses(bboxFilter)
                //                .Cast<FamilyInstance>()
                //                .Where(fi => fi.MEPModel?.ConnectorManager != null)
                //                .SelectMany(fi => fi.MEPModel.ConnectorManager.Connectors.OfType<Connector>()
                //                    .Where(c => !c.IsConnected)
                //                    .Select(c => new { Fitting = fi, Connector = c, Distance = c.Origin.DistanceTo(sprinklerConnectorPoint) }))
                //                .OrderBy(x => x.Distance)
                //                .FirstOrDefault();
                //            pipe = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).WherePasses(bboxFilter).Cast<Pipe>().FirstOrDefault();
                //            //TaskDialog.Show("tt", nearestPipeInfo.Id.ToString());
                //            if (nearestUnconnectedFitting != null)
                //            {
                //                connectedSpFitting = nearestUnconnectedFitting.Fitting.Id;
                //            }
                //            else
                //            {
                //                TaskDialog.Show("提示", "未找到附近可连接的管件");
                //            } 
                //        }
                //        else
                //        {
                //            foreach (Connector refConn in connector.AllRefs)
                //            {
                //                if (refConn.Owner?.Id == sprinkler.Id) continue;
                //                else if (refConn.Owner is FamilyInstance fi)
                //                {
                //                    var fitting = (FamilyInstance)refConn.Owner;
                //                    connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                //                }
                //            }
                //        }
                //        doc.Delete(connectedSpFitting);
                //        tx.Commit();
                //    }
                //    catch (Exception ex)
                //    {
                //        tx.RollBack();
                //        TaskDialog.Show("错误", "删除喷头连接的管件失败: " + ex.Message);
                //    }
                //}

                //0606 纠正偏管
                //从喷头找立管，删掉交接管件
                //using (Transaction tx = new Transaction(doc))
                //{
                //    tx.Start("调整立管位置");
                //    Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                //    FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r);
                //    var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                //    Pipe pipe = null;
                //    if (!connector.IsConnected)
                //    {
                //        return Result.Cancelled;
                //    }
                //    foreach (Connector refConn in connector.AllRefs)
                //    {
                //        if (refConn.Owner?.Id == sprinkler.Id) continue;

                //        if (refConn.Owner is Pipe)
                //        {
                //            pipe = (Pipe)refConn.Owner;
                //        }
                //        else if (refConn.Owner is FamilyInstance fi)
                //        {
                //            var fitting = (FamilyInstance)refConn.Owner;
                //            connectedSpFitting = ((FamilyInstance)refConn.Owner).Id;
                //            pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                //        }
                //        if (pipe == null) continue;
                //    }
                //    doc.Delete(connectedSpFitting);

                //    ////查找管道开放端头，修改坐标XY
                //    ////Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "pick something");
                //    ////Pipe pipe = (Pipe)doc.GetElement(r);

                //    var curve = ((LocationCurve)pipe.Location).Curve as Line;
                //    //FamilyInstance connectedFixFitting;
                //    if (Math.Abs(curve.Direction.Z) != 1)
                //    {
                //        XYZ fixedEndPoint = null;
                //        XYZ moveEndPoint = null;
                //        var connectors = pipe.ConnectorManager.Connectors.OfType<Connector>().ToList();
                //        foreach (var connector2 in connectors)
                //        {
                //            if (connector2.IsConnected)
                //            {
                //                fixedEndPoint = connector2.Origin;
                //            }
                //            else moveEndPoint = connector2.Origin;
                //        }

                //        // 确保找到管两个端点
                //        try
                //        {
                //            // 创建新的端点坐标（保持Z值不变，XY取固定端值）
                //            XYZ newMoveEndPoint = new XYZ(fixedEndPoint.X, fixedEndPoint.Y, moveEndPoint.Z);
                //            LocationCurve locationCurve = pipe.Location as LocationCurve;
                //            Line currentLine = locationCurve.Curve as Line;
                //            double distanceToStart = fixedEndPoint.DistanceTo(currentLine.GetEndPoint(0));
                //            double distanceToEnd = fixedEndPoint.DistanceTo(currentLine.GetEndPoint(1));
                //            // 更新曲线端点
                //            if (distanceToStart < distanceToEnd)
                //            {
                //                // 固定端是起点，移动终点
                //                Line newLine = Line.CreateBound(currentLine.GetEndPoint(0), newMoveEndPoint);
                //                locationCurve.Curve = newLine;
                //            }
                //            else
                //            {
                //                // 固定端是终点，移动起点
                //                Line newLine = Line.CreateBound(newMoveEndPoint, currentLine.GetEndPoint(1));
                //                locationCurve.Curve = newLine;
                //            }
                //            ReconnectSprinklers(new List<ElementId>() { sprinkler.Id });
                //            tx.Commit();
                //        }
                //        catch (Exception ex)
                //        {
                //            tx.RollBack();
                //            TaskDialog.Show("错误", "调整立管位置失败: " + ex.Message);
                //        }
                //    }
                //}

                //0604 取得上下喷头的族（按连接器方向），并获得id备用 OK
                //HashSet<ElementId> upSprinkler = new HashSet<ElementId>();
                //HashSet<ElementId> downSprinkler = new HashSet<ElementId>();
                //StringBuilder stringBuilder = new StringBuilder();
                //FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
                //foreach (Element elem in collector)
                //{
                //    FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(elem.Id);
                //    var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                //    //下喷 = 1
                //    if (connector.CoordinateSystem.BasisZ.Z.ToString() == "1")
                //    {
                //        downSprinkler.Add(sprinkler.Symbol.Family.Id);
                //    }
                //    //上喷 = -1
                //    else if (connector.CoordinateSystem.BasisZ.Z.ToString() == "-1")
                //    {
                //        upSprinkler.Add(sprinkler.Symbol.Family.Id);
                //    }
                //    else continue;
                //}
                //stringBuilder.AppendLine(downSprinkler.First().ToString());
                //stringBuilder.AppendLine(upSprinkler.First().ToString());
                //TaskDialog.Show("tt", stringBuilder.ToString());

                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                //FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r);
                //ConnectorManager connectorManager = sprinkler.MEPModel.ConnectorManager;
                //StringBuilder stringBuilder = new StringBuilder();
                //foreach (Connector connector in connectorManager.Connectors)
                //{
                //    stringBuilder.AppendLine(connector.CoordinateSystem.BasisZ.Z.ToString());
                //    //stringBuilder.AppendLine(connector.CoordinateSystem.Origin.ToString());
                //    XYZ origin = connector.Origin;
                //    stringBuilder.AppendLine(origin.ToString());
                //}
                ////XYZ xYZ = sprinkler.GetTransform().Origin;
                ////stringBuilder.AppendLine(xYZ.ToString());
                //TaskDialog.Show("tt", stringBuilder.ToString());

                //0605 判断管件是弯头还是三通或四通
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipeFitting(), "pick something");
                //FamilyInstance ft = (FamilyInstance)doc.GetElement(r);
                //int result = GetFittingCategory(ft);
                //TaskDialog.Show("tt", result.ToString());

                //0605 喷头连接情况统计，判断立管是否垂直
                ////单选取
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
                //FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r);
                //var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                //if (!connector.IsConnected)
                //{
                //    return Result.Cancelled;
                //}
                //foreach (Connector refConn in connector.AllRefs)
                //{
                //    if (refConn.Owner?.Id == sprinkler.Id) continue;
                //    Pipe pipe = null;
                //    if (refConn.Owner is Pipe)
                //    {
                //        pipe = (Pipe)refConn.Owner;
                //    }
                //    else if (refConn.Owner is FamilyInstance fi)
                //    {
                //        var fitting = (FamilyInstance)refConn.Owner;
                //        pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                //    }
                //    if (pipe == null) continue;
                //    // 获取管道另一端的连接件（非喷头端）
                //    ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
                //    //List<FamilyInstance> connectedFittings = new List<FamilyInstance>();
                //    foreach (Connector pipeConn in pipeConnectors)
                //    {
                //        foreach (Connector refConn2 in pipeConn.AllRefs)
                //        {
                //            // 跳过当前喷头端的连接
                //            if (refConn2.Owner?.Id == sprinkler.Id)
                //                continue;
                //            // 如果连接的是管件（FamilyInstance）
                //            if (refConn2.Owner is FamilyInstance fitting && refConn2.Owner.Id != refConn.Owner.Id)
                //            {
                //                int result = GetFittingCategory(fitting);
                //                //TaskDialog.Show("tt", result.ToString());
                //                //TaskDialog.Show("tt", refConn2.Owner.Id.ToString());
                //                double deltaHeight = Math.Abs((sprinkler.GetTransform().Origin.Z - ((FamilyInstance)refConn2.Owner).GetTransform().Origin.Z) * 304.8);
                //                TaskDialog.Show("tt", deltaHeight.ToString());
                //                //connectedFittings.Add(fitting);
                //            }
                //        }
                //    }
                //}
                ////全取
                //var sprinklers = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                //int connectedSprinklerCount = 0;
                //var verticalPipes = new List<Pipe>();
                //var report = new StringBuilder();
                //foreach (var sprinkler in sprinklers)
                //{
                //    var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                //    // 跳过未连接的喷头
                //    if (connector?.IsConnected != true) continue;
                //    connectedSprinklerCount++;
                //    foreach (Connector refConn in connector.AllRefs)
                //    {
                //        if (refConn.Owner?.Id == sprinkler.Id) continue; // 跳过喷头自身
                //        //// 4. 直接处理管道或通过管件间接获取管道
                //        //var pipe = refConn.Owner switch
                //        //{
                //        //    Pipe p => p, // 直接连接管道
                //        //    FamilyInstance fitting => GetConnectedPipe(fitting, sprinkler.Id), // 通过管件查找管道
                //        //    _ => null
                //        //};
                //        // 替代以上Net8代码
                //        Pipe pipe = null;
                //        if (refConn.Owner is Pipe)
                //        {
                //            pipe = (Pipe)refConn.Owner;
                //        }
                //        else if (refConn.Owner is FamilyInstance fi)
                //        {
                //            var fitting = (FamilyInstance)refConn.Owner;
                //            pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                //            var pipeCon = pipe.MEPSystem.ConnectorManager.Connectors.OfType<Connector>();
                //            TaskDialog.Show("tt", pipeCon.Count().ToString());
                //        }
                //        if (pipe == null) continue;

                //        //if (IsVertical(pipe))
                //        //{
                //        //    verticalPipes.Add(pipe);
                //        //    report.AppendLine(pipe.Id.ToString());
                //        //}
                //    }
                //}
                //TaskDialog.Show("结果", $"已连接喷头数: {connectedSprinklerCount}\n" + $"垂直管道数: {verticalPipes.Count}");
                //原始代码
                //FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
                //int connectSprinkler = 0;
                //StringBuilder stringBuilder = new StringBuilder();
                //List<Pipe> pipes = new List<Pipe>();
                //foreach (Element elem in collector)
                //{
                //    FamilyInstance sprinkler = (FamilyInstance)elem;
                //    var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().FirstOrDefault();
                //    if (connector.IsConnected)
                //    {
                //        connectSprinkler++;
                //        foreach (Connector refConn in connector.AllRefs)
                //        {
                //            if (refConn.Owner is FamilyInstance fitting && refConn.Owner.Id != sprinkler.Id)
                //            {
                //                foreach (Connector fittingConn in fitting.MEPModel.ConnectorManager.Connectors.OfType<Connector>())
                //                {
                //                    foreach (Connector linkedConn in fittingConn.AllRefs)
                //                    {
                //                        if (linkedConn.Owner is Pipe linkedPipe && linkedConn.Owner.Id != sprinkler.Id)
                //                        {
                //                            stringBuilder.AppendLine(linkedPipe.Id.ToString());
                //                            var direction = ((LocationCurve)linkedPipe.Location).Curve.GetEndPoint(1) - ((LocationCurve)linkedPipe.Location).Curve.GetEndPoint(0);
                //                            if (Math.Abs(direction.Normalize().Z) == 1 || Math.Abs(direction.Normalize().Z) == -1)
                //                            {
                //                                pipes.Add(linkedPipe);
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
                ////TaskDialog.Show("tt", connectSprinkler.ToString());
                //TaskDialog.Show("tt", stringBuilder.ToString() + "\n" + pipes.Count().ToString());

                ////找未连接喷头
                //Selection sel = uiDoc.Selection;
                //StringBuilder stringBuilder = new StringBuilder();
                //IList<FamilyInstance> collectorSprinkler = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头").Cast<FamilyInstance>().ToList();
                //foreach (var item in collectorSprinkler)
                //{
                //    if (!item.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First().IsConnected)
                //    {
                //        TaskDialog.Show("tt", "NOPASS");
                //        stringBuilder.AppendLine(item.Id.ToString());
                //    }
                //}
                //TaskDialog.Show("tt", stringBuilder.ToString());
                //////0603 连接喷头.OK
                //Selection sel = uiDoc.Selection;
                //IList<FamilyInstance> collectorSprinkler = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头").Cast<FamilyInstance>().ToList();
                //IList<Connector> conn1 = new List<Connector>();
                //IList<Connector> ppconnlist = new List<Connector>();
                //IList<Element> pipe1 = new List<Element>();
                //using (Transaction tx = new Transaction(doc, "创建喷头连接"))
                //{
                //    tx.Start();
                //    foreach (FamilyInstance sprinkler in collectorSprinkler)
                //    {
                //        ConnectorSetIterator connector1 = sprinkler.MEPModel.ConnectorManager.Connectors.ForwardIterator();
                //        while (connector1.MoveNext())
                //        {
                //            Connector co = connector1.Current as Connector;
                //            if (co.IsConnected == false)
                //            {
                //                conn1.Add(co);
                //                BoundingBoxXYZ box = sprinkler.get_BoundingBox(doc.ActiveView);//用喷头的范围框快速过滤
                //                double Maxx = box.Max.X;
                //                double Maxy = box.Max.Y;
                //                double Minx = box.Min.X;
                //                double Miny = box.Min.Y;
                //                double MZz = box.Max.Z;
                //                Outline myOutLn = null;
                //                //判断上喷
                //                if (sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First().CoordinateSystem.BasisZ.Z.ToString() == "-1")
                //                {
                //                    myOutLn = new Outline(new XYZ(Minx, Miny, MZz - (200 / 304.8)), new XYZ(Maxx, Maxy, MZz - (200 / 304.8)));
                //                }
                //                else
                //                {
                //                    myOutLn = new Outline(new XYZ(Minx, Miny, MZz + (50 / 304.8)), new XYZ(Maxx, Maxy, MZz + (50 / 304.8)));
                //                }
                //                //用喷头的范围框过滤管道
                //                BoundingBoxIntersectsFilter boxee = new BoundingBoxIntersectsFilter(myOutLn);
                //                FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
                //                collector.OfClass(typeof(Pipe));
                //                //与喷头范围框相交的管道
                //                pipe1 = collector.WherePasses(boxee).ToElements();
                //                foreach (Element elp in pipe1)
                //                {
                //                    Pipe pp = elp as Pipe;
                //                    ConnectorSetIterator ppconn = pp.ConnectorManager.Connectors.ForwardIterator();
                //                    while (ppconn.MoveNext())
                //                    {
                //                        Connector ppconn2 = ppconn.Current as Connector;
                //                        if (ppconn2.IsConnected == false)
                //                        {
                //                            ppconnlist.Add(ppconn2);
                //                            try
                //                            {
                //                                // 在此处执行连接操作（如NewTransitionFitting）
                //                                doc.Create.NewTransitionFitting(ppconn2, co);
                //                            }
                //                            catch (Exception ex)
                //                            {
                //                                tx.RollBack(); // 失败时回滚
                //                                TaskDialog.Show("错误", ex.Message.ToString());
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    tx.Commit();
                //}
                ////例程结束

                //////0527 断开喷头.OK
                //using (Transaction tr = new Transaction(doc))
                //{
                //    tr.Start("Disconnect Sprinklers");
                //    // 获取当前视图中所有的喷头
                //    //FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                //    //    .OfCategory(BuiltInCategory.OST_Sprinklers)
                //    //    .OfClass(typeof(FamilyInstance));
                //    //多选择喷头
                //    Selection sel = uiDoc.Selection;
                //    IList<FamilyInstance> collector = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头").Cast<FamilyInstance>().ToList();
                //    int disconnectedCount = 0;
                //    //StringBuilder stringBuilder=new StringBuilder();
                //    foreach (FamilyInstance sprinkler in collector)
                //    {
                //        var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                //        if (connector.IsConnected)
                //        {
                //            // 获取所有连接的连接器
                //            ConnectorSet connectedConnectors = connector.AllRefs;
                //            foreach (Connector connectedConnector in connectedConnectors)
                //            {
                //                //stringBuilder.AppendLine(connectedConnector.Owner.Id.ToString());
                //                //stringBuilder.AppendLine(connector.MEPSystem.Id.ToString());
                //                //// 确保不是自身的连接器
                //                if (connectedConnector.Owner.Id != connector.MEPSystem.Id)
                //                {
                //                    try
                //                    {
                //                        // 尝试从两端都断开,如果第一次断开成功会报错跳过后续
                //                        connector.DisconnectFrom(connectedConnector);
                //                        //connectedConnector.DisconnectFrom(connector);
                //                        doc.Delete(connectedConnector.Owner.Id);
                //                    }
                //                    catch (Exception ex)
                //                    {
                //                        TaskDialog.Show("tt", $"断开连接失败: {ex.Message}");
                //                    }
                //                }
                //            }
                //            disconnectedCount++;
                //        }
                //    } 
                //    //TaskDialog.Show("tt", stringBuilder.ToString());
                //    //TaskDialog.Show("结果", $"已尝试断开 {disconnectedCount} 个喷头连接");
                //    tr.Commit();
                //}
                ////例程结束

                //0603 管件检查
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipeFitting(), "pick something");
                //FamilyInstance fitting = (FamilyInstance)doc.GetElement(r);
                //XYZ xYZ =fitting.GetTransform().Origin;
                //TaskDialog.Show("tt", xYZ.ToString());

                //0602 检查喷头上下，查找基点和连接器对应点
                //连接喷头开放connector与垂直管件

                ////0531 房间面积检查
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterRoomClass(), "Pick a room");
                //Room room = (Room)doc.GetElement(r);
                //var doors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors)
                //    .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                //    .Where(elem =>
                //    {
                //        // 安全检查FromRoom和ToRoom
                //        Room fromRoom = elem.FromRoom;
                //        Room toRoom = elem.ToRoom;
                //        return (fromRoom != null && fromRoom.Id == room.Id) ||
                //               (toRoom != null && toRoom.Id == room.Id);
                //    })
                //    .ToList();
                //TaskDialog.Show("Result", $"Number of doors: {doors.Count}");

                //0531 明细表检查逗号 已更新主程序1.3F
                //ViewSchedule schedule = (ViewSchedule)activeView;
                ////ScheduleDefinition scheduleDefinition = vsc.Definition;
                ////var fields = scheduleDefinition.GetFieldOrder();

                //TableData tableData = schedule.GetTableData();
                //TableSectionData bodyData = tableData.GetSectionData(SectionType.Body);
                //int rowCount = bodyData.NumberOfRows;
                //int colCount = bodyData.NumberOfColumns;
                //var results = new List<string>();
                ////TaskDialog.Show("tt", rowCount.ToString()+"\n"+colCount.ToString());
                //// 遍历所有单元格
                //for (int row = 0; row < rowCount; row++)
                //{
                //    for (int col = 0; col < colCount; col++)
                //    {
                //        // 获取单元格文本
                //        string cellText = schedule.GetCellText(SectionType.Body, row, col);
                //        if (!string.IsNullOrEmpty(cellText) && cellText.Contains(","))
                //        {
                //            // 获取整行内容
                //            var rowContents = new List<string>();
                //            for (int c = 0; c < colCount; c++)
                //            {
                //                rowContents.Add(schedule.GetCellText(SectionType.Body, row, c));
                //            }
                //            results.Add($"行 {row + 1}: " + string.Join(" | ", rowContents));
                //            break; // 同一行找到逗号后就不再检查其他列
                //        }
                //    }
                //}
                //// 显示结果
                //if (results.Count == 0)
                //{
                //    TaskDialog.Show("结果", "未找到包含半角逗号的单元格。");
                //}
                //else
                //{
                //    TaskDialog.Show("结果",
                //        $"在明细表 '{schedule.Name}' 中找到 {results.Count} 行包含半角逗号：\n\n" +
                //        string.Join("\n\n", results));
                //}

                ////0527 设置系统禁止后台计算 待放到功能内。
                //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType));
                //List<PipingSystemType> pipingSystemTypes = elems.OfType<PipingSystemType>().ToList();
                //FilteredElementCollector elems2 = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType));
                //List<MechanicalSystemType> ductSystemTypes = elems2.OfType<MechanicalSystemType>().ToList();
                //using (Transaction tr = new Transaction(doc))
                //{
                //    tr.Start("关闭计算");
                //    foreach (PipingSystemType item in pipingSystemTypes)
                //    {
                //        item.CalculationLevel = Autodesk.Revit.DB.Mechanical.SystemCalculationLevel.None;
                //    }
                //    foreach (MechanicalSystemType item2 in ductSystemTypes)
                //    {
                //        item2.CalculationLevel = Autodesk.Revit.DB.Mechanical.SystemCalculationLevel.None;
                //    }
                //    tr.Commit();
                //}     

                //0526 复制族类型
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "pick something");
                //FamilyInstance ele = (FamilyInstance)doc.GetElement(r);
                //Family family = ele.Symbol.Family;
                //ISet<ElementId> symbolIds = family.GetFamilySymbolIds();
                //int typeCount = symbolIds.Count;
                //TaskDialog.Show("tt",family.FamilyCategory.Name);
                //CopyFamilySymbolsView copyFamilySymbols =new CopyFamilySymbolsView(uiApp);
                //copyFamilySymbols.ShowDialog();

                //0523 南昌检查族合规
                //NCCodingView codingView = new NCCodingView(uiApp);
                //codingView.Show();
                //还是先尝试做个项目参数管理器？
                //列举项目参数名称和相关类，似乎没啥必要。
                //StringBuilder stringBuilder = new StringBuilder();
                //BindingMap bindingMap = doc.ParameterBindings;
                //DefinitionBindingMapIterator iterator=bindingMap.ForwardIterator();
                //while (iterator.MoveNext())
                //{
                //    Definition definition = iterator.Key;
                //    ElementBinding binding =iterator.Current as ElementBinding;
                //    CategorySet categorySet = binding.Categories;
                //    int i = 0;
                //    foreach (Category item in categorySet)
                //    {
                //        i++;
                //    }
                //    //Category category=doc.Settings.Categories.get_Item
                //    stringBuilder.AppendLine(definition.Name+"|"+i);
                //}
                //TaskDialog.Show("tt", stringBuilder.ToString());

                //0522
                //ElementId categoryId = new ElementId(-2000601);
                //Category category = Category.GetCategory(doc, categoryId);
                //TaskDialog.Show("tt", category?.Name);
                //TaskDialog.Show("tt", ele.Symbol.Category.Id.ToString());
                //var allFamilyInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                //var FamilyCollection = allFamilyInstances.Where(item => item.Symbol.Family.Name == ele.Symbol.Family.Name).ToList();
                ////var FamilyCollection = new List<FamilyInstance>();
                ////foreach (var item in allFamilyInstances)
                ////{
                ////    if (item.Symbol.FamilyName== ele.Symbol.FamilyName)
                ////    {
                ////        FamilyCollection.Add(item);
                ////    }
                ////}

                //////这是获取了所有实例对应的Family，应该在vm中使用，在model中直接处理不需要强转
                ////var FamilyCollection = allFamilyInstances.Select(fi => fi.Symbol.Family).Distinct().ToList();
                //TaskDialog.Show("tt", FamilyCollection.Count().ToString());
                //
                //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "pick something");
                //Element ele = doc.GetElement(r);
                //Parameter para = ele.LookupParameter("族ID");
                //if (para.AsString() != null)
                //{ 
                //    TaskDialog.Show("tt",para.AsString());
                //}
                ////0521 
                //// 获取所有 FamilyInstance
                //var familyInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                //// 获取所有 Family 的唯一集合
                //List<Family> families = familyInstances.Select(fi => fi.Symbol.Family).Distinct().ToList();
                //StringBuilder stringBuilder = new StringBuilder();
                //List<Family> newFamily = new List<Family>();
                //foreach (var family in families)
                //{
                //    Parameter para= family.LookupParameter("族ID");
                //    if (para.AsString() != null)
                //    {
                //        stringBuilder.Append(family.Name.ToString() + "||");
                //        newFamily.Add(family);
                //    }
                //}
                //// 定义输出文件路径
                //string outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Families.txt");
                //// 将 StringBuilder 的内容写入到文本文件
                //File.WriteAllText(outputFilePath, stringBuilder.ToString());
                ////int familyCount = families.Count;
                //int familyCount = newFamily.Count;
                //TaskDialog.Show("Family Count", $"当前文档中存在的 FamilyInstance 的 Family 数量为: {familyCount}");

                //0521 当前应用程序路径的上级目录？为什么返回的是桌面？
                //TaskDialog.Show("tt", Path.GetFullPath(".."));             
                //0520 遗留测试
                ////0404 切换连接顺序抄网上代码，初步实现柱切板和梁，梁切板。
                //FilteredElementCollector list_column = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance));
                //FilteredElementCollector list_beam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance));
                ////TaskDialog.Show("tt", $"柱{list_column.Count().ToString()}个,梁{list_beam.Count().ToString()}个");
                //Transaction transaction = new Transaction(doc, "连接几何关系");
                //transaction.Start();
                //foreach (Element column in list_column)
                //{
                //    List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, 1.01);
                //    //TaskDialog.Show("柱子", column_box_eles.Count.ToString());
                //    foreach (Element ele in column_box_eles)
                //    {
                //        if (ele.Category.GetHashCode().ToString() == "-2001320" || ele.Category.GetHashCode().ToString() == "-2000032")
                //        {
                //            JudgeConnection(doc, column, ele);
                //        }
                //    }
                //}
                //foreach (Element beam in list_beam)
                //{
                //    List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, 1.01);
                //    //TaskDialog.Show("梁", beam_box_eles.Count.ToString());
                //    foreach (Element ele in beam_box_eles)
                //    {
                //        //if (ele.Category.Name == "楼板")
                //        if (ele.Category.GetHashCode().ToString() == "-2000032")
                //        {
                //            JudgeConnection(doc, beam, ele);
                //        }
                //    }
                //}
                //transaction.Commit();
                ////例程结束
                return Result.Succeeded;
            }
        }
        //public List<FamilyInstance> SelectSprinklers(UIDocument uiDoc)
        //{
        //    var selectedElements = uiDoc.Selection.PickObjects(ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
        //    return selectedElements.Select(r => uiDoc.Document.GetElement(r) as FamilyInstance).Where(r => r != null).ToList();
        //}
        //public void CheckDuplicateSprinklers(Document doc, List<FamilyInstance> sprinklers)
        //{
        //    // 获取所有喷头的连接点位置
        //    var sprinklerLocations = new List<(FamilyInstance Element, XYZ Location)>();
        //    foreach (var sprinkler in sprinklers)
        //    {
        //        var connectors = GetConnectors(sprinkler);
        //        if (connectors.Count > 0)
        //        {
        //            sprinklerLocations.Add((sprinkler, connectors[0].Origin));
        //        }
        //    }
        //    //XYZ xYZ= sprinklerLocations[0].Location;
        //    // 检查是否存在重复的XY坐标
        //    bool hasDuplicates = sprinklerLocations
        //        .GroupBy(s => new
        //        {
        //            X = Math.Round(s.Location.X, 3),
        //            Y = Math.Round(s.Location.Y, 3)
        //        })
        //        .Any(g => g.Count() > 1);
        //    if (hasDuplicates)
        //    {
        //        TaskDialog.Show("结果", "存在XY坐标相同的喷头");
        //    }
        //    else
        //    {
        //        TaskDialog.Show("结果", "没有XY坐标相同的喷头");
        //    }
        //    //// 按XY坐标分组
        //    //var groupedByXY = sprinklerLocations.GroupBy(s =>
        //    //    {
        //    //        XYZ origin = s.Location;
        //    //        return new XYZ(Math.Round(origin.X, 3), Math.Round(origin.Y, 3), 0);
        //    //    }).Where(g => g.Count() > 1);
        //    //// 检查每组中是否有不同Z值
        //    //var results = new List<string>();
        //    //foreach (var group in groupedByXY)
        //    //{
        //    //    var zValues = group.Select(s => Math.Round(s.Location.Z, 3))
        //    //                       .Distinct().Count();
        //    //    if (zValues > 1)
        //    //    {
        //    //        string info = $"在位置 ({group.Key.X}, {group.Key.Y}) 发现 {group.Count()} 个喷头，Z值不同";
        //    //        results.Add(info);
        //    //        // 可以在这里高亮显示这些喷头
        //    //        foreach (var (element, _) in group)
        //    //        {
        //    //            // 高亮或其他标记操作
        //    //        }
        //    //    }
        //    //}
        //    //// 显示结果
        //    //if (results.Any())
        //    //{
        //    //    TaskDialog.Show("检查结果", string.Join("\n", results));
        //    //}
        //    //else
        //    //{
        //    //    TaskDialog.Show("检查结果", "未发现XY相同但Z不同的喷头");
        //    //}
        //}
        //private List<Connector> GetConnectors(FamilyInstance fixture)
        //{
        //    var connectors = new List<Connector>();
        //    // 从族实例获取连接件管理器
        //    var connectorManager = fixture.MEPModel?.ConnectorManager;
        //    if (connectorManager != null)
        //    {
        //        // 获取所有未连接的连接件
        //        var unconnectedConnectors = connectorManager.Connectors
        //            .OfType<Connector>()
        //            .Where(c => !c.IsConnected)
        //            .ToList();
        //        // 如果没有未连接的，则获取所有连接件
        //        connectors = unconnectedConnectors.Any() ? unconnectedConnectors :
        //            connectorManager.Connectors.OfType<Connector>().ToList();
        //    }
        //    return connectors;
        //}
        //private static IEnumerable<Pipe> FindNearbyPipes(FilteredElementCollector pipeCollector, XYZ connectorOrigin, double searchRadius = 0.5)
        //{
        //    // 创建空间搜索的边界框（以连接件原点为中心，扩展searchRadius）
        //    Outline searchOutline = new Outline(
        //        new XYZ(connectorOrigin.X - searchRadius, connectorOrigin.Y - searchRadius, connectorOrigin.Z - searchRadius),
        //        new XYZ(connectorOrigin.X + searchRadius, connectorOrigin.Y + searchRadius, connectorOrigin.Z + searchRadius)
        //    );
        //    // 应用空间过滤器
        //    BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchOutline);
        //    return pipeCollector
        //        .WherePasses(bboxFilter)
        //        .Cast<Pipe>()
        //        .Where(pipe =>
        //        {
        //            // 可选：进一步检查管道与连接点的实际距离（更精确）
        //            Line pipeCurve = (pipe.Location as LocationCurve)?.Curve as Line;
        //            if (pipeCurve != null)
        //            {
        //                double distance = pipeCurve.Distance(connectorOrigin);
        //                return distance <= searchRadius;
        //            }
        //            return false;
        //        });
        //}
        //private static bool CanConnect(Connector sprinklerConnector, Connector pipeConnector)
        //{
        //    // 条件1：连接件是否已连接
        //    if (sprinklerConnector.IsConnected || pipeConnector.IsConnected)
        //        return false;
        //    // 条件2：连接件方向是否兼容（允许微小角度偏差）
        //    double angleTolerance = Math.PI / 6; // 30度容忍
        //    if (sprinklerConnector.CoordinateSystem.BasisZ.AngleTo(pipeConnector.CoordinateSystem.BasisZ) > angleTolerance)
        //        return false;
        //    // 条件3：连接件原点距离是否在允许范围内（单位：英尺）
        //    double distanceTolerance = 0.5; // 约15cm
        //    if (sprinklerConnector.Origin.DistanceTo(pipeConnector.Origin) > distanceTolerance)
        //        return false;
        //    return true;
        //}
        // 自定义比较器，确保 Family 的唯一性基于名称
        private class FamilyComparer : IEqualityComparer<Family>
        {
            public bool Equals(Family x, Family y)
            {
                if (x == null || y == null)
                {
                    return false;
                }
                return x.Name == y.Name;
            }

            public int GetHashCode(Family obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
