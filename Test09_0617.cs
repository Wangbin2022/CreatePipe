using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test09_0617 : IExternalCommand
    {
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
        private bool isConnectedPipe(List<Connector> pipe)
        {
            bool result = false;
            foreach (var item in pipe)
            {
                if (item.IsConnected)
                {
                    result = true;
                }
            }
            return !result;
        }
        private ConnectorSet GetConnectorSet(FamilyInstance instance)
        {
            if (instance == null) return null;

            // Get the family instance MEPModel
            MEPModel mepModel = instance.MEPModel;
            if (mepModel == null) return null;

            return mepModel.ConnectorManager?.Connectors;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //0617 实现combobox或Listbox增加选项 item后面添加删除按钮

            ListboxTest listboxTest = new ListboxTest();
            listboxTest.Show();
            ////0618 点击垂直管端生成喷头并连接，管尺寸要有限制
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "Pick something");
            //Pipe pipe = (Pipe)doc.GetElement(r);
            //if (pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8 > 20) return Result.Failed;
            //List<Connector> connectors = GetConnectors(pipe);
            //if (connectors.Count() != 2 || isConnectedPipe(connectors)) return Result.Failed;
            //// Find unconnected connectors
            //Connector unconnectedConnector = connectors.FirstOrDefault(c => !c.IsConnected);
            //if (unconnectedConnector == null) return Result.Failed;

            //// Get the direction of the unconnected connector
            //XYZ connectorDirection = unconnectedConnector.CoordinateSystem.BasisZ;

            //// Collect all sprinkler families in the document
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //ICollection<Element> sprinklers = collector.OfCategory(BuiltInCategory.OST_Sprinklers)
            //                                          .WhereElementIsNotElementType()
            //                                          .ToElements();

            //// Find sprinklers with opposite direction
            //FamilyInstance targetSprinkler = null;
            //foreach (Element elem in sprinklers)
            //{
            //    FamilyInstance sprinkler = elem as FamilyInstance;
            //    if (sprinkler == null) continue;

            //    // Get sprinkler's connector
            //    ConnectorSet sprinklerConnectors = GetConnectorSet(sprinkler);
            //    if (sprinklerConnectors == null || sprinklerConnectors.Size == 0) continue;

            //    Connector sprinklerConnector = sprinklerConnectors.Cast<Connector>().First();

            //    // Check if sprinkler connector is unconnected and has opposite direction
            //    if (!sprinklerConnector.IsConnected &&
            //        sprinklerConnector.CoordinateSystem.BasisZ.IsAlmostEqualTo(-connectorDirection))
            //    {
            //        targetSprinkler = sprinkler;
            //        break;
            //    }
            //}

            //if (targetSprinkler == null) return Result.Failed;

            //// Get the sprinkler's connector
            //ConnectorSet targetConnectors = GetConnectorSet(targetSprinkler);
            //Connector targetConnector = targetConnectors.Cast<Connector>().First();

            //// Connect the pipe to the sprinkler
            //using (Transaction trans = new Transaction(doc, "Connect Pipe to Sprinkler"))
            //{
            //    trans.Start();

            //    try
            //    {
            //        unconnectedConnector.ConnectTo(targetConnector);
            //        // Or use this alternative method:
            //        // doc.Create.NewElbowFitting(unconnectedConnector, targetConnector);

            //        trans.Commit();
            //        return Result.Succeeded;
            //    }
            //    catch (Exception ex)
            //    {
            //        trans.RollBack();
            //        TaskDialog.Show("Error", ex.Message);
            //        return Result.Failed;
            //    }
            //}






            //TaskDialog.Show("tt", connectors.Count().ToString());

            ////0618 修改三通喷头问题.OK
            //SprinklerReplaceAmendView amendView = new SprinklerReplaceAmendView(uiApp);
            //amendView.ShowDialog();

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
            return Result.Succeeded;
        }
    }
}
