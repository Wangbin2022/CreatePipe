namespace CreatePipe.cmd
{
    //[Transaction(TransactionMode.Manual)]
    //public class BreakMEPCurveWPF : IExternalCommand
    //{
    //    UIDocument uiDoc = null;
    //    Document doc = null;
    //    Application application = null;

    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        //引用
    //        UIApplication uiApp = commandData.Application;
    //        application = uiApp.Application;
    //        uiDoc = uiApp.ActiveUIDocument;
    //        doc = uiDoc.Document; //用全局定义，不要重复赋值

    //        //外部Event放在窗体前,放到主线程中
    //        ExternalEventExample externalEventExample = new ExternalEventExample(commandData);
    //        //注册事件委托前OK
    //        //ExternalEvent externalEvent = ExternalEvent.Create(externalEventExample);
    //        //注册事件委托后,以下改写为example中的方法
    //        //externalEventExample.ExternalEvent = ExternalEvent.Create(externalEventExample);
    //        externalEventExample.CreateExternalEvent(externalEventExample);


    //        //创建窗体对象，使用委托前
    //        //BreakMEPCurveForm breakMEPCurveForm = new BreakMEPCurveForm(this, externalEvent);
    //        //创建窗体对象，使用委托后
    //        BreakMEPCurveForm breakMEPCurveForm = new BreakMEPCurveForm(this, externalEventExample);
    //        //打开WPF窗体
    //        breakMEPCurveForm.Show();

    //        return Result.Succeeded;
    //    }

    //    //    //使用委托后为了满足返回值为void，新建两个方法调用已完成的

    //    //    public void  BreakMEPCurveByTwoV()
    //    //    {
    //    //        BreakMEPCurveByTwo();
    //    //    }
    //    //    public void BreakMEPCurveByOneV()
    //    //    {
    //    //        BreakMEPCurveByOne();
    //    //    }

    //    //    //两点打断管 ,委托后放到WPF内
    //    //    public MEPCurve BreakMEPCurveByTwo()
    //    //    {
    //    //        try
    //    //        {
    //    //            Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass());
    //    //            MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //    //            //选择2个点
    //    //            XYZ breakXYZ1 = reference.GlobalPoint;
    //    //            XYZ breakXYZ2 = uiDoc.Selection.PickPoint();
    //    //            MEPCurve mEPCurveCopy = null;//变量声明放到事务外才能访问

    //    //            using (Transaction ts = new Transaction(doc, "title"))
    //    //            {
    //    //                ts.Start();

    //    //                //拷贝一根管
    //    //                ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
    //    //                ElementId newId = ids.FirstOrDefault();
    //    //                mEPCurveCopy = doc.GetElement(newId) as MEPCurve;
    //    //                //原管的线
    //    //                Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //    //                XYZ startXYZ = curve.GetEndPoint(0);
    //    //                XYZ endXYZ = curve.GetEndPoint(1);
    //    //                //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
    //    //                breakXYZ1 = curve.Project(breakXYZ1).XYZPoint;
    //    //                breakXYZ2 = curve.Project(breakXYZ2).XYZPoint;
    //    //                //增加点选点的距离比较，两点交换
    //    //                if (startXYZ.DistanceTo(breakXYZ1) > startXYZ.DistanceTo(breakXYZ2))
    //    //                {
    //    //                    XYZ xyz = breakXYZ1;
    //    //                    breakXYZ1 = breakXYZ2;
    //    //                    breakXYZ2 = xyz;
    //    //                }

    //    //                //给原管用的线
    //    //                Line line = Line.CreateBound(startXYZ, breakXYZ1);
    //    //                //拷贝管用的线
    //    //                Line line1 = Line.CreateBound(breakXYZ2, endXYZ);

    //    //                //
    //    //                //找管1连接器并取消多余连接，保存连接信息P28
    //    //                Connector othercon = null;
    //    //                foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //    //                {
    //    //                    bool isBreak = false;
    //    //                    //获取id后，找连接的情况，再解除连接
    //    //                    if (con.Id == 1 && con.IsConnected)
    //    //                    {
    //    //                        foreach (Connector con2 in con.AllRefs)
    //    //                        {
    //    //                            if (con2.Owner is FamilyInstance)
    //    //                            {
    //    //                                con.DisconnectFrom(con2);
    //    //                                othercon = con2;
    //    //                                isBreak = true;
    //    //                                break;
    //    //                            }
    //    //                        }
    //    //                    }
    //    //                    if (isBreak)
    //    //                    {
    //    //                        break;
    //    //                    }
    //    //                }

    //    //            //改原管
    //    //            (mEPCurve.Location as LocationCurve).Curve = line;
    //    //                //改新管
    //    //                (mEPCurveCopy.Location as LocationCurve).Curve = line1;
    //    //                //拷贝管连接老管的连接器
    //    //                if (othercon != null)
    //    //                {
    //    //                    foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //    //                    {
    //    //                        if (con.Id == 1)
    //    //                        {
    //    //                            con.ConnectTo(othercon);
    //    //                        }
    //    //                    }
    //    //                }
    //    //                ts.Commit();
    //    //            }
    //    //            return mEPCurveCopy;
    //    //        }
    //    //        catch (Exception ex)
    //    //        {
    //    //            TaskDialog.Show("Title", ex.Message);
    //    //        }
    //    //        return null;
    //    //    }

    //    //    //单点打断管 ,委托前放在这个文件，委托后放到WPF内
    //    //    public MEPCurve BreakMEPCurveByOne()
    //    //    {
    //    //        try
    //    //        {
    //    //            //选择过滤器要在后面新建，不能直接用typeof或ofclass
    //    //            Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass());
    //    //            MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //    //            //XYZ breakXYZ = uiDoc.Selection.PickPoint();
    //    //            XYZ breakXYZ = reference.GlobalPoint;//直接使用选择管的位置点
    //    //            MEPCurve mEPCurveCopy = null;//变量声明放到事务外才能访问

    //    //            using (Transaction ts = new Transaction(doc, "title"))
    //    //            {
    //    //                ts.Start();
    //    //                //拷贝一根管
    //    //                ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
    //    //                ElementId newId = ids.FirstOrDefault();
    //    //                mEPCurveCopy = doc.GetElement(newId) as MEPCurve;
    //    //                //原管的线
    //    //                Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //    //                XYZ startXYZ = curve.GetEndPoint(0);
    //    //                XYZ endXYZ = curve.GetEndPoint(1);
    //    //                //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
    //    //                breakXYZ = curve.Project(breakXYZ).XYZPoint;
    //    //                //给原管用的线
    //    //                Line line = Line.CreateBound(startXYZ, breakXYZ);
    //    //                //找连接器并取消多余连接，保存连接信息P28
    //    //                Connector othercon = null;
    //    //                foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //    //                {
    //    //                    bool isBreak = false;
    //    //                    //获取id后，找连接的情况，再解除连接
    //    //                    if (con.Id == 1 && con.IsConnected)
    //    //                    {
    //    //                        foreach (Connector con2 in con.AllRefs)
    //    //                        {
    //    //                            if (con2.Owner is FamilyInstance)
    //    //                            {
    //    //                                con.DisconnectFrom(con2);
    //    //                                othercon = con2;
    //    //                                isBreak = true;
    //    //                                break;
    //    //                            }
    //    //                        }
    //    //                    }
    //    //                    if (isBreak)
    //    //                    {
    //    //                        break;
    //    //                    }
    //    //                }


    //    //        (mEPCurve.Location as LocationCurve).Curve = line;
    //    //                //拷贝管用的线
    //    //                Line line1 = Line.CreateBound(breakXYZ, endXYZ);
    //    //                (mEPCurveCopy.Location as LocationCurve).Curve = line1;
    //    //                //拷贝管连接老管的连接器
    //    //                if (othercon != null)
    //    //                {
    //    //                    foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //    //                    {
    //    //                        if (con.Id == 1)
    //    //                        {
    //    //                            con.ConnectTo(othercon);
    //    //                        }
    //    //                    }

    //    //                }
    //    //                ts.Commit();
    //    //            }
    //    //            return mEPCurveCopy;
    //    //        }
    //    //        catch (Exception ex)
    //    //        {
    //    //            TaskDialog.Show("Title", ex.Message);
    //    //        }
    //    //        return null;
    //    //    }
    //}
}
