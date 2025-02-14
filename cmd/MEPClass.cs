namespace MEPClass
{
    //[Transaction(TransactionMode.Manual)]
    //public class MEPClass : IExternalCommand
    //{
    //    UIDocument uiDoc = null;
    //    Document doc = null;
    //    Application application = null;

    //    //MEPCurve mEPCurve = null;
    //    //XYZ breakXYZ = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uiApp = commandData.Application;
    //        application = uiApp.Application;
    //        uiDoc = uiApp.ActiveUIDocument;
    //        doc = uiDoc.Document; //用全局定义，不要重复赋值

    //        //GetPipeConncector();
    //        using (Transaction ts = new Transaction(doc, "Title"))
    //        {
    //            ts.Start();
    //            try
    //            {
    //                while (true)
    //                {
    //                    //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //                    //MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //                    ////选择一个点
    //                    //XYZ breakXYZ = uiDoc.Selection.PickPoint();

    //                    //BreakMEPCurveByOne(mEPCurve,breakXYZ);

    //                    BreakMEPCurveByTwo();

    //                    doc.Regenerate();
    //                }
    //            }

    //            catch (Exception ex)
    //            {
    //                TaskDialog.Show("提示", ex.Message);
    //            }



    //            ts.Commit();
    //        }


    //        return Result.Succeeded;
    //    }

    //    public class command : IExternalCommand
    //    {
    //        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //        {
    //            message ="messagetest";
    //            return Result.Failed;
    //        }
    //    }


    //    //两点打断管 
    //    public void BreakMEPCurveByTwo()
    //    {
    //        Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //        //选择2个点
    //        XYZ breakXYZ1 = uiDoc.Selection.PickPoint();
    //        XYZ breakXYZ2 = uiDoc.Selection.PickPoint();
    //        //拷贝一根管
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
    //        ElementId newId = ids.FirstOrDefault();
    //        MEPCurve mEPCurveCopy = doc.GetElement(newId) as MEPCurve;
    //        //原管的线
    //        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //        XYZ startXYZ = curve.GetEndPoint(0);
    //        XYZ endXYZ = curve.GetEndPoint(1);
    //        //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
    //        breakXYZ1 = curve.Project(breakXYZ1).XYZPoint;
    //        breakXYZ2 = curve.Project(breakXYZ2).XYZPoint;
    //        //增加点选点的距离比较，两点交换
    //        if (startXYZ.DistanceTo(breakXYZ1) > startXYZ.DistanceTo(breakXYZ2))
    //        {
    //            XYZ xyz = breakXYZ1;
    //            breakXYZ1 = breakXYZ2;
    //            breakXYZ2 = xyz;
    //        }
    //        //给原管用的线
    //        Line line = Line.CreateBound(startXYZ, breakXYZ1);
    //        (mEPCurve.Location as LocationCurve).Curve = line;
    //        //拷贝管用的线
    //        Line line1 = Line.CreateBound(breakXYZ2, endXYZ);
    //        (mEPCurveCopy.Location as LocationCurve).Curve = line1;
    //    }



    //    //单点打断管 
    //    public void BreakMEPCurveByOne(MEPCurve mEPCurve, XYZ breakXYZ)
    //    {

    //        //拷贝一根管
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
    //        ElementId newId = ids.FirstOrDefault();
    //        MEPCurve mEPCurveCopy = doc.GetElement(newId) as MEPCurve;  
    //        //原管的线
    //        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //        XYZ startXYZ = curve.GetEndPoint(0);
    //        XYZ endXYZ = curve.GetEndPoint(1);
    //        //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
    //        breakXYZ = curve.Project(breakXYZ).XYZPoint;
    //        //给原管用的线
    //        Line line = Line.CreateBound(startXYZ,breakXYZ);
    //        //找连接器并取消多余连接，保存连接信息P28
    //        Connector othercon = null;
    //        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //        {
    //            bool isBreak = false;
    //            //获取id后，找连接的情况，再解除连接
    //            if (con.Id == 1 && con.IsConnected)
    //            {
    //                foreach (Connector con2 in con.AllRefs)
    //                {
    //                    if (con2.Owner is FamilyInstance)
    //                    {
    //                        con.DisconnectFrom(con2);
    //                        othercon = con2;
    //                        isBreak =true;
    //                        break;
    //                    }
    //                }
    //            }
    //            if (isBreak) 
    //            {
    //                break;
    //            }
    //        }

    //        (mEPCurve.Location as LocationCurve).Curve = line;
    //        //拷贝管用的线
    //        Line line1 = Line.CreateBound(breakXYZ, endXYZ);
    //        (mEPCurveCopy.Location as LocationCurve).Curve = line1;
    //        //拷贝管连接老管的连接器
    //        foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //        {
    //            if (con.Id ==1)
    //            {
    //                con.ConnectTo(othercon);
    //            }
    //        }            
    //    }

    //    //public void GetPipeConncector() 
    //    //{
    //    //    Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //    //    Pipe pipe = doc.GetElement(reference) as Pipe;

    //    //    ConnectorManager connectorManager = pipe.ConnectorManager;
    //    //    ConnectorSet connectorSet = connectorManager.Connectors;

    //    //    StringBuilder stringBuilder = new StringBuilder();
    //    //    foreach (Connector connector in connectorSet)
    //    //    {
    //    //        XYZ direction = connector.CoordinateSystem.BasisZ;
    //    //        stringBuilder.AppendLine("id"+connector.Id+"是否连接"+connector.IsConnected+"类型"+connector.Domain+"所有者Id"+connector.Owner.Id+"朝向"+direction);
    //    //    }
    //    //    TaskDialog.Show("提示",stringBuilder.ToString());
    //    //}


    //    //找管连接的
    //    //public void GetPipeConncector()
    //    //{
    //    //    Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //    //    MEPCurve pipe = doc.GetElement(reference) as MEPCurve; //改名后MEP类通用

    //    //    ConnectorManager connectorManager = pipe.ConnectorManager;
    //    //    ConnectorSet connectorSet = connectorManager.Connectors;

    //    //    StringBuilder stringBuilder = new StringBuilder();
    //    //    foreach (Connector connector in connectorSet)
    //    //    {
    //    //        XYZ direction = connector.CoordinateSystem.BasisZ;
    //    //        stringBuilder.AppendLine("id" + connector.Id + "是否连接" + connector.IsConnected + "类型" + connector.Domain + "所有者Id" + connector.Owner.Id + "朝向" + direction);

    //    //        ConnectorSet connectorSet1 = connector.AllRefs;
    //    //        foreach (Connector connector1 in connectorSet1)
    //    //        {
    //    //            if (connector1.Owner is FamilyInstance)
    //    //            {
    //    //                FamilyInstance familyInstance = connector1.Owner as FamilyInstance;
    //    //                stringBuilder.AppendLine("连接元素id"+familyInstance.Id+"类型名称"+familyInstance.Name);
    //    //                //从弯头继续找连接到的管
    //    //                ConnectorSet connectorSet2 = familyInstance.MEPModel.ConnectorManager.Connectors;
    //    //                foreach (Connector connector2 in connectorSet2)
    //    //                {
    //    //                    if (connector2.IsConnected)
    //    //                    {
    //    //                        foreach (Connector connector3 in connector2.AllRefs)
    //    //                        {
    //    //                            stringBuilder.AppendLine("弯头连接了" + connector3.Owner.Id+ "名称" + connector3.Owner.Name);
    //    //                        }
    //    //                    }
    //    //                }

    //    //                break;
    //    //            }
    //    //        }
    //    //        stringBuilder.AppendLine();
    //    //    }
    //    //    TaskDialog.Show("提示", stringBuilder.ToString());
    //    //}

    //    //public Pipe CreatePipe()
    //    //{
    //    //    Pipe pipe =new Pipe();
    //    //    //修改管道偏移量
    //    //    pipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM).Set(2000.Tofoot());
    //    //    //修改直径
    //    //    pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(20.Tofoot());

    //    //    return pipe;
    //    //    //参考测试样例和方法
    //    //}
    //}
}
