using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            //逻辑梳理，检测喷头类型，未连接的给提示
            //如果是上下喷，如果喷头不一致，先调用switch替换，一致的话不操作
            //如果是下喷，分端头和中间检测，按高度差复制构件，连接转换弯头/三通？喷头不一致的替换
            //如果是上喷，逻辑同上
            Reference r = Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
            FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(r);
            using (Transaction trans = new Transaction(Document, "Create Up Sprinkler"))
            {
                trans.Start();
                //已为上下喷的喷头跳过处理
                if (ConnectedDoubleDownSprinkler.Contains(sprinkler.Id) || ConnectedDoubleUpSprinkler.Contains(sprinkler.Id)) return;
                //如果是下喷,检测偏离高度，对应向上复制喷头并改为上喷喷头，连接喷头
                if (ConnectedDownSprinkler.Contains(sprinkler.Id))
                {
                    var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                    foreach (Connector refConn in connector.AllRefs)
                    {
                        if (refConn.Owner?.Id == sprinkler.Id) continue;
                        Pipe pipe = null;
                        if (refConn.Owner is Pipe)
                        {
                            pipe = (Pipe)refConn.Owner;
                        }
                        else if (refConn.Owner is FamilyInstance fi)
                        {
                            var fitting = (FamilyInstance)refConn.Owner;
                            pipe = (Pipe)GetConnectedMEPCurve(fitting, sprinkler.Id);
                        }
                        if (pipe == null) continue;
                        // 获取管道另一端的连接件（非喷头端）
                        ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
                        double maxDeltaHeight = 0;
                        FamilyInstance targetFitting = null;
                        Connector pipeEnd;
                        int fittingResult = 0;
                        foreach (Connector pipeConn in pipeConnectors)
                        {
                            foreach (Connector refConn2 in pipeConn.AllRefs)
                            {
                                if (refConn2.Owner?.Id == sprinkler.Id) continue;
                                if (refConn2.Owner is FamilyInstance fitting && refConn2.Owner.Id != refConn.Owner.Id)
                                {
                                    fittingResult = GetFittingCategory(fitting);
                                    pipeEnd = refConn2;
                                    double deltaHeight = Math.Abs((sprinkler.GetTransform().Origin.Z - fitting.GetTransform().Origin.Z) * 304.8);
                                    if (deltaHeight > maxDeltaHeight)
                                    {
                                        maxDeltaHeight = deltaHeight;
                                        targetFitting = fitting;
                                    }
                                }
                            }
                        }
                        //if (targetFitting != null && maxDeltaHeight > 0)
                        //{
                        //    // 获取原喷头XY坐标
                        //    XYZ originalLocation = sprinkler.GetTransform().Origin;
                        //    XYZ originalXY = new XYZ(originalLocation.X, originalLocation.Y, 0);
                        //    // 计算新喷头位置（保持XY不变，Z轴基于fitting位置向上偏移deltaHeight）
                        //    XYZ fittingLocation = targetFitting.GetTransform().Origin;
                        //    double newZ = fittingLocation.Z + (maxDeltaHeight / 304.8);
                        //    XYZ newLocation = new XYZ(originalLocation.X, originalLocation.Y, newZ);
                        //    // 复制喷头到新位置并修改类型
                        //    var newSprinklerId = ElementTransformUtils.CopyElement(Document, sprinkler.Id, newLocation - originalLocation).FirstOrDefault();
                        //    FamilyInstance newSprinkler = (FamilyInstance)Document.GetElement(newSprinklerId);
                        //    newSprinkler.ChangeTypeId(SelectedUpSp.GetFamilySymbolIds().First());
                        //    var newConnector = newSprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>().FirstOrDefault();
                        //    if (fittingResult == 3)
                        //    {
                        //        ConnectorSet existingConnectors = targetFitting.MEPModel.ConnectorManager.Connectors;
                        //        FamilySymbol crossSymbol = GetCrossSymbol(pipe.PipeType); // 获取匹配的四通族类型
                        //        FamilyInstance crossFitting = Document.Create.NewFamilyInstance(
                        //            newLocation,
                        //            crossSymbol,
                        //            targetFitting.Host,
                        //            targetFitting.Level,
                        //            StructuralType.NonStructural);
                        //    }

                        //    //// 连接新喷头到管道

                        //}
                    }
                }
                trans.Commit();
            }


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
