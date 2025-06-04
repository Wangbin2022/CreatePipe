using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
using static Org.BouncyCastle.Crypto.Digests.SkeinEngine;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test08_0520 : IExternalCommand
    {
        public List<FamilyInstance> SelectSprinklers(UIDocument uiDoc)
        {
            var selectedElements = uiDoc.Selection.PickObjects(ObjectType.Element, new SprinklerEntityFilter(), "请选择喷头");
            return selectedElements.Select(r => uiDoc.Document.GetElement(r) as FamilyInstance).Where(r => r != null).ToList();
        }
        public void CheckDuplicateSprinklers(Document doc, List<FamilyInstance> sprinklers)
        {
            // 获取所有喷头的连接点位置
            var sprinklerLocations = new List<(FamilyInstance Element, XYZ Location)>();
            foreach (var sprinkler in sprinklers)
            {
                var connectors = GetConnectors(sprinkler);
                if (connectors.Count > 0)
                {
                    sprinklerLocations.Add((sprinkler, connectors[0].Origin));
                }
            }
            //XYZ xYZ= sprinklerLocations[0].Location;
            // 检查是否存在重复的XY坐标
            bool hasDuplicates = sprinklerLocations
                .GroupBy(s => new
                {
                    X = Math.Round(s.Location.X, 3),
                    Y = Math.Round(s.Location.Y, 3)
                })
                .Any(g => g.Count() > 1);

            if (hasDuplicates)
            {
                TaskDialog.Show("结果", "存在XY坐标相同的喷头");
            }
            else
            {
                TaskDialog.Show("结果", "没有XY坐标相同的喷头");
            }
            //// 按XY坐标分组
            //var groupedByXY = sprinklerLocations.GroupBy(s =>
            //    {
            //        XYZ origin = s.Location;
            //        return new XYZ(Math.Round(origin.X, 3), Math.Round(origin.Y, 3), 0);
            //    }).Where(g => g.Count() > 1);
            //// 检查每组中是否有不同Z值
            //var results = new List<string>();
            //foreach (var group in groupedByXY)
            //{
            //    var zValues = group.Select(s => Math.Round(s.Location.Z, 3))
            //                       .Distinct().Count();

            //    if (zValues > 1)
            //    {
            //        string info = $"在位置 ({group.Key.X}, {group.Key.Y}) 发现 {group.Count()} 个喷头，Z值不同";
            //        results.Add(info);
            //        // 可以在这里高亮显示这些喷头
            //        foreach (var (element, _) in group)
            //        {
            //            // 高亮或其他标记操作
            //        }
            //    }
            //}
            //// 显示结果
            //if (results.Any())
            //{
            //    TaskDialog.Show("检查结果", string.Join("\n", results));
            //}
            //else
            //{
            //    TaskDialog.Show("检查结果", "未发现XY相同但Z不同的喷头");
            //}
        }
        private List<Connector> GetConnectors(FamilyInstance fixture)
        {
            var connectors = new List<Connector>();

            // 从族实例获取连接件管理器
            var connectorManager = fixture.MEPModel?.ConnectorManager;
            if (connectorManager != null)
            {
                // 获取所有未连接的连接件
                var unconnectedConnectors = connectorManager.Connectors
                    .OfType<Connector>()
                    .Where(c => !c.IsConnected)
                    .ToList();

                // 如果没有未连接的，则获取所有连接件
                connectors = unconnectedConnectors.Any() ? unconnectedConnectors :
                    connectorManager.Connectors.OfType<Connector>().ToList();
            }

            return connectors;
        }

        private static IEnumerable<Pipe> FindNearbyPipes(FilteredElementCollector pipeCollector, XYZ connectorOrigin, double searchRadius = 0.5)
        {
            // 创建空间搜索的边界框（以连接件原点为中心，扩展searchRadius）
            Outline searchOutline = new Outline(
                new XYZ(connectorOrigin.X - searchRadius, connectorOrigin.Y - searchRadius, connectorOrigin.Z - searchRadius),
                new XYZ(connectorOrigin.X + searchRadius, connectorOrigin.Y + searchRadius, connectorOrigin.Z + searchRadius)
            );

            // 应用空间过滤器
            BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(searchOutline);
            return pipeCollector
                .WherePasses(bboxFilter)
                .Cast<Pipe>()
                .Where(pipe =>
                {
                    // 可选：进一步检查管道与连接点的实际距离（更精确）
                    Line pipeCurve = (pipe.Location as LocationCurve)?.Curve as Line;
                    if (pipeCurve != null)
                    {
                        double distance = pipeCurve.Distance(connectorOrigin);
                        return distance <= searchRadius;
                    }
                    return false;
                });
        }
        private static bool CanConnect(Connector sprinklerConnector, Connector pipeConnector)
        {
            // 条件1：连接件是否已连接
            if (sprinklerConnector.IsConnected || pipeConnector.IsConnected)
                return false;

            // 条件2：连接件方向是否兼容（允许微小角度偏差）
            double angleTolerance = Math.PI / 6; // 30度容忍
            if (sprinklerConnector.CoordinateSystem.BasisZ.AngleTo(pipeConnector.CoordinateSystem.BasisZ) > angleTolerance)
                return false;

            // 条件3：连接件原点距离是否在允许范围内（单位：英尺）
            double distanceTolerance = 0.5; // 约15cm
            if (sprinklerConnector.Origin.DistanceTo(pipeConnector.Origin) > distanceTolerance)
                return false;
            return true;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


            //0604 连接喷头ds改进 没成功
            //Selection sel = uiDoc.Selection;
            //IList<Element> selectedSprinklers = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头");
            //using (Transaction tx = new Transaction(doc, "自动连接喷头与管道"))
            //{
            //    tx.Start();
            //    try
            //    {
            //        var allPipes = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).WhereElementIsNotElementType();

            //        foreach (Element sprinkler in selectedSprinklers)
            //        {
            //            var connectors = (sprinkler as FamilyInstance)?
            //                .MEPModel?
            //                .ConnectorManager?
            //                .Connectors?
            //                .Cast<Connector>()?
            //                .Where(c => !c.IsConnected);

            //            if (connectors == null) continue;

            //            foreach (Connector sprinklerConn in connectors)
            //            {
            //                var nearbyPipes = FindNearbyPipes(allPipes, sprinklerConn.Origin);
            //                foreach (Pipe pipe in nearbyPipes)
            //                {
            //                    foreach (Connector pipeConn in pipe.ConnectorManager.Connectors)
            //                    {
            //                        if (CanConnect(sprinklerConn, pipeConn))
            //                        {
            //                            doc.Create.NewTransitionFitting(pipeConn, sprinklerConn);
            //                            break; // 找到一个连接即可
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        tx.Commit();
            //    }
            //    catch (Exception ex)
            //    {
            //        tx.RollBack();
            //        TaskDialog.Show("错误", ex.ToString());
            //    }
            //}

            //////0603 连接喷头
            //Selection sel = uiDoc.Selection;
            //List<ElementId> listt = new List<ElementId>();

            ////PTFamilyInstance f = new PTFamilyInstance();
            //IList<Element> ell = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头");
            //IList<Connector> conn1 = new List<Connector>();
            //IList<Connector> ppconnlist = new List<Connector>();
            //IList<Element> pipe1 = new List<Element>();
            //foreach (Element eel in ell)
            //{
            //    FamilyInstance fa = eel as FamilyInstance;
            //    ConnectorSetIterator connector1 = fa.MEPModel.ConnectorManager.Connectors.ForwardIterator();
            //    while (connector1.MoveNext())
            //    {
            //        Connector co = connector1.Current as Connector;
            //        if (co.IsConnected == false)
            //        {
            //            conn1.Add(co);

            //            BoundingBoxXYZ box = eel.get_BoundingBox(doc.ActiveView);//用喷头的范围框快速过滤

            //            double Maxx = box.Max.X;
            //            double Maxy = box.Max.Y;

            //            double Minx = box.Min.X;
            //            double Miny = box.Min.Y;

            //            double MZz = box.Max.Z;
            //            Outline myOutLn = null;
            //            //if (eel.GetParameters("族与类型")[0].AsValueString().Contains("上喷"))
            //            if (fa.Symbol.Family.Name.Contains("上喷"))
            //            {
            //                myOutLn = new Outline(new XYZ(Minx, Miny, MZz - (200 / 304.8)), new XYZ(Maxx, Maxy, MZz - (200 / 304.8)));
            //            }
            //            else
            //            {
            //                myOutLn = new Outline(new XYZ(Minx, Miny, MZz + (50 / 304.8)), new XYZ(Maxx, Maxy, MZz + (50 / 304.8)));
            //            }
            //            //用喷头的范围框过滤管道
            //            BoundingBoxIntersectsFilter boxee = new BoundingBoxIntersectsFilter(myOutLn);
            //            FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
            //            collector.OfClass(typeof(Pipe));
            //            pipe1 = collector.WherePasses(boxee).ToElements();//与喷头范围框相交的管道
            //            foreach (Element elp in pipe1)
            //            {
            //                Pipe pp = elp as Pipe;
            //                ConnectorSetIterator ppconn = pp.ConnectorManager.Connectors.ForwardIterator();
            //                while (ppconn.MoveNext())
            //                {
            //                    Connector ppconn2 = ppconn.Current as Connector;
            //                    if (ppconn2.IsConnected == false)
            //                    {
            //                        ppconnlist.Add(ppconn2);
            //                        using (Transaction tx = new Transaction(doc, "创建喷头连接"))
            //                        {
            //                            tx.Start();
            //                            try
            //                            {
            //                                // 在此处执行连接操作（如NewTransitionFitting）
            //                                doc.Create.NewTransitionFitting(ppconn2, co);
            //                                tx.Commit(); // 提交事务
            //                            }
            //                            catch (Exception ex)
            //                            {
            //                                tx.RollBack(); // 失败时回滚
            //                                TaskDialog.Show("错误", ex.Message);
            //                            }
            //                        }

            //                        //try
            //                        //{
            //                        //    //doc.Create.NewUnionFitting(ppconn2, co);
            //                        //    doc.Create.NewTransitionFitting(ppconn2, co);
            //                        //}
            //                        //catch
            //                        //{
            //                        //}
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            ////0527 断开喷头
            using (Transaction tr = new Transaction(doc))
            {
                tr.Start("Disconnect Sprinklers");
                // 获取当前视图中所有的喷头
                //FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                //    .OfCategory(BuiltInCategory.OST_Sprinklers)
                //    .OfClass(typeof(FamilyInstance));
                Selection sel = uiDoc.Selection;
                IList<FamilyInstance> collector = sel.PickElementsByRectangle(new SprinklerEntityFilter(), "请选择要连接的喷头").Cast<FamilyInstance>().ToList();
                int disconnectedCount = 0;
                foreach (FamilyInstance sprinkler in collector)
                {
                    //// 检查MEPModel是否存在
                    //if (sprinkler.MEPModel == null)
                    //{
                    //    continue;
                    //}
                    //ConnectorManager connectorManager = sprinkler.MEPModel.ConnectorManager;
                    //if (connectorManager != null)
                    //{
                    //    // 获取所有连接器
                    //    ConnectorSet connectors = connectorManager.Connectors;
                    //    foreach (Connector connector in connectors)
                    //    {

                    var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                    if (connector.IsConnected)
                    {
                        // 获取所有连接的连接器
                        ConnectorSet connectedConnectors = connector.AllRefs;
                        foreach (Connector connectedConnector in connectedConnectors)
                        {
                            // 确保不是自身的连接器
                            if (connectedConnector.Owner.Id != sprinkler.Id)
                            {
                                try
                                {
                                    // 尝试从两端都断开
                                    connector.DisconnectFrom(connectedConnector);
                                    //connectedConnector.DisconnectFrom(connector);
                                    disconnectedCount++;
                                    doc.Delete(connectedConnector.Owner.Id);
                                    // 记录断开信息（可选）
                                    Debug.WriteLine($"已断开喷头 {sprinkler.Id} 与 {connectedConnector.Owner.Id} 的连接");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"断开连接失败: {ex.Message}");
                                }
                            }
                        }
                    }
                    //    }
                    //}
                }
                TaskDialog.Show("结果", $"已尝试断开 {disconnectedCount} 个喷头连接");
                tr.Commit();
            }

            //0603 查找连接喷头的横竖管 没成功，再看看
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
            //FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r); 

            //var verticalPipes = new List<ElementId>();
            //var horizontalPipes = new List<ElementId>();
            //var minVerticalDistance = double.MaxValue;
            //var minHorizontalDistance = double.MaxValue;
            //ElementId closestVerticalPipeId = null;
            //ElementId closestHorizontalPipeId = null;

            //// 获取喷头位置
            //var sprinklerLocation = (sprinkler.Location as LocationPoint)?.Point;
            //var sprinklerConnectors = sprinkler.MEPModel.ConnectorManager.Connectors;
            ////取连接最快方案
            //var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
            ////if (sprinklerLocation == null) return;

            ////foreach (var connector in sprinklerConnectors)
            ////{
            //// 获取连接件连接的所有图元
            //foreach (Connector refConn in connector.AllRefs)
            //    {
            //        if (refConn.Owner is Pipe pipe && refConn.Owner.Id != sprinkler.Id)
            //        {
            //            // 计算管件与喷头的距离
            //            var pipeLocation = refConn.Origin;
            //            var distance = pipeLocation.DistanceTo(sprinklerLocation);

            //            // 判断管件方向
            //            var direction = ((LocationCurve)pipe.Location).Curve.GetEndPoint(1) - ((LocationCurve)pipe.Location).Curve.GetEndPoint(0);

            //            // 垂直管判断 (Z方向分量大于0.9)
            //            if (Math.Abs(direction.Normalize().Z) > 0.9)
            //            {
            //                if (distance < minVerticalDistance)
            //                {
            //                    minVerticalDistance = distance;
            //                    closestVerticalPipeId = pipe.Id;
            //                }
            //            }
            //            // 水平管判断 (Z方向分量小于0.1)
            //            else if (Math.Abs(direction.Normalize().Z) < 0.1)
            //            {
            //                if (distance < minHorizontalDistance)
            //                {
            //                    minHorizontalDistance = distance;
            //                    closestHorizontalPipeId = pipe.Id;
            //                }
            //            }
            //        }
            //    //}
            //}

            //// 显示结果
            //var result = new StringBuilder();
            //result.AppendLine($"喷头 ID: {sprinkler.Id}");

            //if (closestVerticalPipeId != null)
            //{
            //    result.AppendLine($"最近垂直管 ID: {closestVerticalPipeId}");
            //    result.AppendLine($"距离: {minVerticalDistance:F2} 米");
            //}
            //else
            //{
            //    result.AppendLine("未找到连接的垂直管");
            //}

            //if (closestHorizontalPipeId != null)
            //{
            //    result.AppendLine($"最近水平管 ID: {closestHorizontalPipeId}");
            //    result.AppendLine($"距离: {minHorizontalDistance:F2} 米");
            //}
            //else
            //{
            //    result.AppendLine("未找到连接的水平管");
            //}

            //TaskDialog.Show("连接管件信息", result.ToString());

            //0603 管件检查
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipeFitting(), "pick something");
            //FamilyInstance fitting = (FamilyInstance)doc.GetElement(r);
            //XYZ xYZ =fitting.GetTransform().Origin;
            //TaskDialog.Show("tt", xYZ.ToString());

            //0602 检查喷头上下，查找基点和连接器对应点
            //连接喷头开放connector与垂直管件

            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new SprinklerEntityFilter(), "pick something");
            //FamilyInstance sprinkler = (FamilyInstance)doc.GetElement(r);
            //ConnectorManager connectorManager = sprinkler.MEPModel.ConnectorManager;
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (Connector connector in connectorManager.Connectors)
            //{
            //    //    //stringBuilder.AppendLine(connector.CoordinateSystem.BasisZ.Z.ToString());
            //    stringBuilder.AppendLine(connector.CoordinateSystem.Origin.ToString());
            //    //    XYZ origin = connector.Origin;
            //}
            //XYZ xYZ = sprinkler.GetTransform().Origin;
            //stringBuilder.AppendLine(xYZ.ToString());
            //TaskDialog.Show("tt", stringBuilder.ToString());

            //查找平面位置相同的喷头
            //try
            //{
            //    // 选择喷头
            //    var sprinklers = SelectSprinklers(uiDoc);
            //    if (sprinklers.Count == 0)
            //    {
            //        TaskDialog.Show("提示", "未选择任何喷头");
            //        return Result.Cancelled;
            //    }
            //    // 检查重复
            //    CheckDuplicateSprinklers(doc, sprinklers);
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


            //List<ElementId> spId = new List<ElementId>();
            //FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
            //    .OfCategory(BuiltInCategory.OST_Sprinklers)
            //    .OfClass(typeof(FamilyInstance));
            //int UpSprinklerCount = 0;
            //int DownSprinklerCount = 0;
            //int OverlappingSprinklerCount = 0;
            //// 定义容差值
            //double tolerance = 0.00001; // 容差值，可以根据需要调整
            //// 用于存储喷头位置的字典
            //Dictionary<Tuple<double, double>, List<FamilyInstance>> sprinklerPositions = new Dictionary<Tuple<double, double>, List<FamilyInstance>>();

            //foreach (FamilyInstance sprinkler in collector)
            //{
            //    ConnectorManager connectorManager = sprinkler.MEPModel.ConnectorManager;
            //    foreach (Connector connector in connectorManager.Connectors)
            //    {
            //        if (connector.CoordinateSystem.BasisZ.Z.ToString() == "1")
            //        {
            //            DownSprinklerCount++;
            //        }
            //        else if (connector.CoordinateSystem.BasisZ.Z.ToString() == "-1")
            //        {
            //            UpSprinklerCount++;
            //        }
            //        // 获取喷头的 Origin 位置
            //        XYZ origin = sprinkler.GetTransform().Origin;
            //        // 检查是否有其他喷头在相同位置
            //        // 创建一个 Tuple 来存储 X 和 Y 值
            //        //Tuple<double, double> positionKey = Tuple.Create(origin.X, origin.Y);
            //        // 使用容差值来确定位置
            //        Tuple<double, double> positionKey = Tuple.Create(Math.Round(origin.X / tolerance) * tolerance, Math.Round(origin.Y / tolerance) * tolerance);
            //        // 检查是否有其他喷头在相同位置
            //        if (sprinklerPositions.ContainsKey(positionKey))
            //        {
            //            sprinklerPositions[positionKey].Add(sprinkler);
            //            spId.Add(sprinkler.Id);
            //        }
            //        else
            //        {
            //            sprinklerPositions[positionKey] = new List<FamilyInstance> { sprinkler };
            //        }
            //    }
            //}
            //// 检查重合的喷头
            //foreach (var position in sprinklerPositions.Values)
            //{
            //    if (position.Count > 1)
            //    {
            //        OverlappingSprinklerCount += position.Count;
            //    }
            //}
            ////TaskDialog.Show("tt", $"上喷头{UpSprinklerCount}个，下喷头{DownSprinklerCount}个,，位置重合的喷头 {OverlappingSprinklerCount} 个");
            //TaskDialog.Show("tt", spId.Count().ToString());

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

            //0531 明细表检查逗号
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

            ////0527 设置系统禁止后台计算
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
