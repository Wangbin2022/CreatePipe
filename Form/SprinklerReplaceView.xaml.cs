using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
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
using System.Windows.Shapes;

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

            //取实例
            //FilteredElementCollector collector = new FilteredElementCollector(Document, Document.ActiveView.Id).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
            FilteredElementCollector collector = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance));
            foreach (Element elem in collector)
            {
                AllSprinklerCount++;
                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(elem.Id);
                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().First();
                //下喷 = 1
                if (connector.CoordinateSystem.BasisZ.Z.ToString() == "1")
                {
                    //单独添加?
                    DownSprinklerType.Add(sprinkler.Symbol.Family);
                    AllDownSprinklerCount++;
                    if (connector.IsConnected)
                    {
                        ConnectedDownSprinklerCount += 1;
                    }
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
                            // 获取管道另一端的连接件（非喷头端）
                            ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
                            //List<FamilyInstance> connectedFittings = new List<FamilyInstance>();
                            foreach (Connector pipeConn in pipeConnectors)
                            {
                                foreach (Connector refConn2 in pipeConn.AllRefs)
                                {
                                    // 跳过当前喷头端的连接
                                    if (refConn2.Owner?.Id == sprinkler.Id) continue;
                                    // 如果连接的是管件（FamilyInstance）
                                    if (refConn2.Owner is FamilyInstance fitting2 && refConn2.Owner.Id != refConn.Owner.Id)
                                    {
                                        int result = GetFittingCategory(fitting2);
                                        if (result == 4)
                                        {
                                            ConnectedDoubleSprinklerCount++;
                                        }
                                        //判断横三通还是竖三通
                                        else if (result == 3 && fitting2.HandOrientation.Z == 1)
                                        {
                                            ConnectedDoubleSprinklerCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //上喷 = -1
                else if (connector.CoordinateSystem.BasisZ.Z.ToString() == "-1")
                {
                    UpSprinklerType.Add(sprinkler.Symbol.Family);
                    AllUpSprinklerCount++;
                    if (connector.IsConnected)
                    {
                        ConnectedUpSprinklerCount += 1;
                    }
                }
                else continue;
            }
            SelectedDownSp = DownSprinklerType.FirstOrDefault();
            SelectedUpSp = UpSprinklerType.FirstOrDefault();
        }
        public ICommand SwitchSprinklerCommand => new BaseBindingCommand(SwitchSprinkler);
        private void SwitchSprinkler(object obj)
        {
            using (Transaction tx = new Transaction(Document))
            {
                tx.Start("替换喷头");
                try
                {
                    var sprinklers = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                    var removeFittingIds = new List<ElementId>();
                    var connectSpIds = new List<ElementId>();
                    // 处理喷头替换
                    foreach (var sprinkler in sprinklers)
                    {
                        var connector = sprinkler.MEPModel?.ConnectorManager?.Connectors?.OfType<Connector>()?.FirstOrDefault();
                        if (connector == null) continue;
                        // 处理需要替换的喷头
                        if (connector.IsConnected && sprinkler.Symbol?.Family != null && sprinkler.Symbol.Family.Id != SelectedUpSp.Id && sprinkler.Symbol.Family.Id != SelectedDownSp.Id)
                        {
                            ProcessConnectedSprinkler(sprinkler, connector, removeFittingIds, connectSpIds);
                        }
                        // 处理未连接但需要类型调整的喷头
                        if (!connector.IsConnected)
                        {
                            ReplaceSprinklerType(sprinkler, connector);
                        }
                    }
                    // 批量删除管件
                    BatchDeleteElements(removeFittingIds);
                    // 重新连接喷头
                    ReconnectSprinklers(connectSpIds);
                    tx.Commit();
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
        //private void SwitchSprinkler(object obj)
        //{
        //    using (Transaction tx = new Transaction(Document))
        //    {
        //        tx.Start("替换喷头3");
        //        try
        //        {
        //            var sprinklers = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Sprinklers).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
        //            var removeFittingIds = new List<ElementId>();
        //            var connectSpIds = new List<ElementId>();
        //            //StringBuilder stringBuilder = new StringBuilder();
        //            //先断开再替换，最后重连接
        //            foreach (var sprinkler in sprinklers)
        //            {
        //                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().FirstOrDefault();
        //                if (connector.IsConnected && (sprinkler.Symbol.Family.Id != SelectedUpSp.Id && sprinkler.Symbol.Family.Id != SelectedDownSp.Id))
        //                {
        //                    // 获取所有连接的连接器
        //                    ConnectorSet connectedConnectors = connector.AllRefs;
        //                    foreach (Connector connectedConnector in connectedConnectors)
        //                    {
        //                        // 确保不是自身的连接器
        //                        if (connectedConnector.Owner.Id != connector.MEPSystem.Id)
        //                        {
        //                            //stringBuilder.AppendLine(connectedConnector.Owner.Id.ToString());
        //                            // 尝试从两端都断开,如果第一次断开成功会报错跳过后续
        //                            connector.DisconnectFrom(connectedConnector);
        //                            removeFittingIds.Add(connectedConnector.Owner.Id);
        //                        }
        //                    }
        //                    //下喷 = 1，直接替换
        //                    if (connector.CoordinateSystem.BasisZ.Z.ToString() == "1")
        //                    {
        //                        ElementId newSprinklerType = SelectedDownSp.GetFamilySymbolIds().FirstOrDefault();
        //                        if (sprinkler.Symbol.Family.Id != SelectedDownSp.Id)
        //                        {
        //                            sprinkler.ChangeTypeId(newSprinklerType);
        //                            connectSpIds.Add(sprinkler.Id);
        //                        }
        //                    }
        //                    //上喷 = -1，直接替换
        //                    else if (connector.CoordinateSystem.BasisZ.Z.ToString() == "-1")
        //                    {
        //                        ElementId newSprinklerType = SelectedUpSp.GetFamilySymbolIds().FirstOrDefault();
        //                        if (sprinkler.Symbol.Family.Id != SelectedUpSp.Id)
        //                        {
        //                            sprinkler.ChangeTypeId(newSprinklerType);
        //                            connectSpIds.Add(sprinkler.Id);
        //                        }
        //                    }
        //                }
        //                //下喷 = 1，直接替换
        //                if (connector.CoordinateSystem.BasisZ.Z.ToString() == "1")
        //                {
        //                    ElementId newSprinklerType = SelectedDownSp.GetFamilySymbolIds().FirstOrDefault();
        //                    if (sprinkler.Symbol.Family.Id != SelectedDownSp.Id)
        //                    {
        //                        sprinkler.ChangeTypeId(newSprinklerType);
        //                    }
        //                }
        //                //上喷 = -1，直接替换
        //                else if (connector.CoordinateSystem.BasisZ.Z.ToString() == "-1")
        //                {
        //                    ElementId newSprinklerType = SelectedUpSp.GetFamilySymbolIds().FirstOrDefault();
        //                    if (sprinkler.Symbol.Family.Id != SelectedUpSp.Id)
        //                    {
        //                        sprinkler.ChangeTypeId(newSprinklerType);
        //                    }
        //                }
        //            }
        //            foreach (var item in removeFittingIds)
        //            {
        //                Document.Delete(item);
        //            }
        //            foreach (ElementId item in connectSpIds)
        //            {
        //                FamilyInstance sprinkler = (FamilyInstance)Document.GetElement(item);
        //                var connector = sprinkler.MEPModel.ConnectorManager.Connectors.OfType<Connector>().FirstOrDefault();
        //                //喷头连接
        //                IList<Connector> conn1 = new List<Connector>();
        //                IList<Connector> ppconnlist = new List<Connector>();
        //                IList<Element> pipe1 = new List<Element>();
        //                conn1.Add(connector);
        //                BoundingBoxXYZ box = sprinkler.get_BoundingBox(Document.ActiveView);//用喷头的范围框快速过滤
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
        //                FilteredElementCollector collector = new FilteredElementCollector(Document, Document.ActiveView.Id);
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
        //                            // 在此处执行连接操作（如NewTransitionFitting）
        //                            Document.Create.NewTransitionFitting(ppconn2, connector);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            TaskDialog.Show("tt", ex.Message.ToString());
        //        }
        //        tx.Commit();
        //    }
        //}
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
