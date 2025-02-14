namespace CreatePipe.cmd
{
    ///// <summary>
    ///// 安装专业
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //public class InstallCmd : IExternalCommand
    //{
    //    UIDocument uIDocument = null;
    //    Document document = null;
    //    Application application = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {  //ui应用程序
    //        UIApplication uiApp = commandData.Application;
    //        //应用程序
    //        application = uiApp.Application;


    //        //ui文档
    //        uIDocument = uiApp.ActiveUIDocument;

    //        //文档
    //        document = uIDocument.Document;
    //        //GetConnector();
    //        using (Transaction tran = new Transaction(document, "tran"))
    //        {
    //            tran.Start();

    //            Reference reference1 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //            Reference reference2 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //            Reference reference3 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //            Reference reference4 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);

    //            MEPCurve mEPCurve1 = document.GetElement(reference1) as MEPCurve;
    //            MEPCurve mEPCurve2 = document.GetElement(reference2) as MEPCurve;
    //            MEPCurve mEPCurve3 = document.GetElement(reference3) as MEPCurve;
    //            MEPCurve mEPCurve4 = document.GetElement(reference4) as MEPCurve;

    //            CreateCross(mEPCurve1, mEPCurve2, mEPCurve3, mEPCurve4);

    //            tran.Commit();
    //        }


    //        return Result.Succeeded;
    //    }

    //    /// <summary>
    //    /// 创建四通
    //    /// </summary>
    //    /// <param name="mEPCurve"></param>
    //    /// <param name="mEPCurve2"></param>
    //    /// <param name="mEPCurve3"></param>
    //    /// <param name="mEPCurve4"></param>
    //    /// <returns></returns>
    //    public FamilyInstance CreateCross(MEPCurve mEPCurve, MEPCurve mEPCurve2, MEPCurve mEPCurve3, MEPCurve mEPCurve4)
    //    {

    //        List<MEPCurve> mEPCurves = new List<MEPCurve>() { mEPCurve, mEPCurve2, mEPCurve3, mEPCurve4 };

    //        List<Connector> connectors = new List<Connector>();
    //        //循环
    //        for (int i = 0; i < mEPCurves.Count - 1; i++)
    //        {
    //            //拿到两根管
    //            MEPCurve mep1 = mEPCurves[i];
    //            MEPCurve mep2 = mEPCurves[i + 1];


    //            if (connectors.Count > 0)
    //            {
    //                double minDistance = double.MaxValue;
    //                Connector lastCon = connectors.LastOrDefault();

    //                Connector nearCon2 = null;
    //                foreach (Connector connector in mep2.ConnectorManager.Connectors)
    //                {
    //                    double distance = connector.Origin.DistanceTo(lastCon.Origin);
    //                    if (distance < minDistance)
    //                    {
    //                        minDistance = distance;
    //                        nearCon2 = connector;
    //                    }
    //                }
    //                connectors.Add(nearCon2);


    //            }
    //            else
    //            {
    //                //找两根管最近的连接器
    //                double minDistance = double.MaxValue;
    //                Connector nearCon1 = null;
    //                Connector nearCon2 = null;
    //                foreach (Connector con1 in mep1.ConnectorManager.Connectors)
    //                {
    //                    foreach (Connector con2 in mep2.ConnectorManager.Connectors)
    //                    {
    //                        double distance = con1.Origin.DistanceTo(con2.Origin);
    //                        if (distance < minDistance)
    //                        {
    //                            minDistance = distance;
    //                            nearCon1 = con1;
    //                            nearCon2 = con2;
    //                        }
    //                    }
    //                }

    //                connectors.Add(nearCon1);
    //                connectors.Add(nearCon2);
    //            }



    //        }
    //        if (connectors.Count == 4)
    //        {



    //            List<Connector> connectors1 = new List<Connector>();
    //            for (int i = 0; i < connectors.Count; i++)
    //            {
    //                Connector con1 = connectors[i];

    //                if (connectors1.Contains(con1))
    //                {
    //                    continue;
    //                }

    //                //con1方向
    //                XYZ con1Fx = con1.CoordinateSystem.BasisZ;
    //                for (int a = i + 1; a < connectors.Count; a++)
    //                {
    //                    Connector con2 = connectors[a];
    //                    if (connectors1.Contains(con2))
    //                    {
    //                        continue;
    //                    }


    //                    //con2方向
    //                    XYZ con2Fx = con2.CoordinateSystem.BasisZ;

    //                    if (con1Fx.IsAlmostEqualTo(con2Fx) || con1Fx.IsAlmostEqualTo(con2Fx.Negate()))
    //                    {
    //                        connectors1.Add(con1);
    //                        connectors1.Add(con2);
    //                    }

    //                }

    //            }








    //            return document.Create.NewCrossFitting(connectors1[0], connectors1[1], connectors1[2], connectors1[3]);
    //        }




    //        return null;

    //    }



    //    /// <summary>
    //    /// 创建三通
    //    /// </summary>
    //    /// <param name="mEPCurve">管</param>
    //    /// <param name="mEPCurve2">管</param>
    //    /// <param name="mEPCurve3">管</param>
    //    public FamilyInstance CreateTee(MEPCurve mEPCurve, MEPCurve mEPCurve2, MEPCurve mEPCurve3)
    //    {

    //        //找出平行的两根管和一根不平行的管
    //        Line line = (mEPCurve.Location as LocationCurve).Curve as Line;
    //        Line line2 = (mEPCurve2.Location as LocationCurve).Curve as Line;
    //        Line line3 = (mEPCurve3.Location as LocationCurve).Curve as Line;

    //        //line.GetEndPoint(1).Subtract(line.GetEndPoint(0)).Normalize();

    //        //横管1
    //        MEPCurve parallelMEP1 = null;
    //        //横管2
    //        MEPCurve parallelMEP2 = null;
    //        //竖管
    //        MEPCurve verticalMEP = null;

    //        //如果平行
    //        if (line.Direction.IsAlmostEqualTo(line2.Direction) || line.Direction.IsAlmostEqualTo(line2.Direction.Negate()))
    //        {
    //            parallelMEP1 = mEPCurve;
    //            parallelMEP2 = mEPCurve2;

    //            if (!line3.Direction.IsAlmostEqualTo(line.Direction) && !line3.Direction.IsAlmostEqualTo(line.Direction.Negate()))
    //            {
    //                verticalMEP = mEPCurve3;
    //            }
    //        }
    //        else if (line.Direction.IsAlmostEqualTo(line3.Direction) || line.Direction.IsAlmostEqualTo(line3.Direction.Negate()))
    //        {
    //            parallelMEP1 = mEPCurve;
    //            parallelMEP2 = mEPCurve3;
    //            verticalMEP = mEPCurve2;

    //            if (!line2.Direction.IsAlmostEqualTo(line.Direction) && !line2.Direction.IsAlmostEqualTo(line.Direction.Negate()))
    //            {
    //                verticalMEP = mEPCurve2;
    //            }

    //        }
    //        else if (line2.Direction.IsAlmostEqualTo(line3.Direction) || line2.Direction.IsAlmostEqualTo(line3.Direction.Negate()))
    //        {
    //            parallelMEP1 = mEPCurve2;
    //            parallelMEP2 = mEPCurve3;
    //            verticalMEP = mEPCurve;
    //            if (!line.Direction.IsAlmostEqualTo(line2.Direction) && !line.Direction.IsAlmostEqualTo(line2.Direction.Negate()))
    //            {
    //                verticalMEP = mEPCurve;
    //            }
    //        }


    //        if (parallelMEP1 != null && parallelMEP2 != null && verticalMEP != null)
    //        {
    //            double minVal = double.MaxValue;

    //            Connector parallelMEP1Con = null;
    //            Connector parallelMEP2Con = null;
    //            Connector verticalMEPCon = null;

    //            //获取两根平行管的最近两个连接器
    //            foreach (Connector con1 in parallelMEP1.ConnectorManager.Connectors)
    //            {
    //                foreach (Connector con2 in parallelMEP2.ConnectorManager.Connectors)
    //                {
    //                    double distance = con1.Origin.DistanceTo(con2.Origin);
    //                    if (distance < minVal)
    //                    {
    //                        minVal = distance;
    //                        parallelMEP1Con = con1;
    //                        parallelMEP2Con = con2;
    //                    }
    //                }
    //            }

    //            if (parallelMEP1Con != null && parallelMEP2Con != null)
    //            {
    //                //获得竖管的连接器
    //                minVal = double.MaxValue;
    //                foreach (Connector con1 in verticalMEP.ConnectorManager.Connectors)
    //                {
    //                    double distance = con1.Origin.DistanceTo(parallelMEP1Con.Origin);
    //                    if (distance < minVal)
    //                    {
    //                        minVal = distance;
    //                        verticalMEPCon = con1;

    //                    }
    //                }

    //                //创建三通
    //                return document.Create.NewTeeFitting(parallelMEP1Con, parallelMEP2Con, verticalMEPCon);



    //            }


    //        }


    //        return null;
    //    }


    //    /// <summary>
    //    /// 创建三通 两根管情况
    //    /// </summary>
    //    /// <param name="mEPCurve">被垂直的管</param>
    //    /// <param name="mEPCurve2">垂直的管</param>
    //    /// <returns></returns>
    //    public FamilyInstance CreateTeeByTwoMEPCurve(MEPCurve mEPCurve, MEPCurve mEPCurve2)
    //    {
    //        Line mEPCurveLine = (mEPCurve.Location as LocationCurve).Curve as Line;


    //        Line mEPCurve2Line = (mEPCurve2.Location as LocationCurve).Curve as Line;

    //        //取一个离着被垂直管近的一端
    //        double distance1 = mEPCurveLine.Distance(mEPCurve2Line.GetEndPoint(0));
    //        double distance2 = mEPCurveLine.Distance(mEPCurve2Line.GetEndPoint(1));
    //        XYZ nearXYZ = mEPCurve2Line.GetEndPoint(0);
    //        if (distance1 > distance2)
    //        {
    //            nearXYZ = mEPCurve2Line.GetEndPoint(1);
    //        }

    //        //将 nearXYZ映射到 管1上
    //        XYZ projectXYZ = mEPCurveLine.Project(nearXYZ).XYZPoint;

    //        MEPCurve mEPCurve3 = BreakMEPCurve(mEPCurve, projectXYZ);


    //        return CreateTee(mEPCurve, mEPCurve2, mEPCurve3);



    //    }


    //    /// <summary>
    //    /// 一个点打断管
    //    /// </summary>
    //    public MEPCurve BreakMEPCurve(MEPCurve mEPCurve, XYZ breakXYZ)
    //    {

    //        document = mEPCurve.Document;

    //        //拷贝一根管
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(document, mEPCurve.Id, new XYZ(0, 0, 0));
    //        ElementId newId = ids.FirstOrDefault();
    //        MEPCurve mEPCurveCopy = document.GetElement(newId) as MEPCurve;


    //        //原来管的线
    //        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //        XYZ startXYZ = curve.GetEndPoint(0);
    //        XYZ endXYZ = curve.GetEndPoint(1);

    //        //映射点
    //        breakXYZ = curve.Project(breakXYZ).XYZPoint;

    //        //给原来的管用的线
    //        Line line = Line.CreateBound(startXYZ, breakXYZ);

    //        //拷贝管用的线
    //        Line line2 = Line.CreateBound(breakXYZ, endXYZ);

    //        //管1连接的连接器
    //        Connector otherCon = null;
    //        //解除管1连接的连接器 并获得连接的其它连接器
    //        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //        {
    //            bool isBreak = false;
    //            if (con.Id == 1 && con.IsConnected)
    //            {
    //                foreach (Connector con2 in con.AllRefs)
    //                {
    //                    if (con2.Owner is FamilyInstance)
    //                    {
    //                        con.DisconnectFrom(con2);
    //                        otherCon = con2;
    //                        isBreak = true;
    //                        break;
    //                    }
    //                }
    //            }
    //            if (isBreak)
    //            {
    //                break;
    //            }
    //        }


    //        //改原来的管
    //        (mEPCurve.Location as LocationCurve).Curve = line;

    //        //改现在的管
    //        (mEPCurveCopy.Location as LocationCurve).Curve = line2;

    //        //让拷贝的管连接原来管连接的连接器
    //        if (otherCon != null)
    //        {


    //            foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //            {
    //                if (con.Id == 1)
    //                {
    //                    con.ConnectTo(otherCon);
    //                }
    //            }
    //        }
    //        return mEPCurveCopy;

    //    }

    //    /// <summary>
    //    /// 两个个点打断管
    //    /// </summary>
    //    public MEPCurve BreakMEPCurve2(MEPCurve mEPCurve, XYZ breakXYZ, XYZ breakXYZ2)
    //    {

    //        document = mEPCurve.Document;

    //        //拷贝一根管
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(document, mEPCurve.Id, new XYZ(0, 0, 0));
    //        ElementId newId = ids.FirstOrDefault();
    //        MEPCurve mEPCurveCopy = document.GetElement(newId) as MEPCurve;


    //        //原来管的线
    //        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //        XYZ startXYZ = curve.GetEndPoint(0);
    //        XYZ endXYZ = curve.GetEndPoint(1);

    //        //给原来的用
    //        breakXYZ = curve.Project(breakXYZ).XYZPoint;

    //        //给现在的管用
    //        breakXYZ2 = curve.Project(breakXYZ2).XYZPoint;

    //        //如果起始点和breakXYZ2近
    //        if (startXYZ.DistanceTo(breakXYZ) > startXYZ.DistanceTo(breakXYZ2))
    //        {
    //            XYZ xyz = breakXYZ;
    //            breakXYZ = breakXYZ2;
    //            breakXYZ2 = xyz;
    //        }



    //        //给原来的管用的线
    //        Line line = Line.CreateBound(startXYZ, breakXYZ);

    //        //拷贝管用的线
    //        Line line2 = Line.CreateBound(breakXYZ2, endXYZ);

    //        //管1连接的连接器
    //        Connector otherCon = null;
    //        //解除管1连接的连接器 并获得连接的其它连接器
    //        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //        {
    //            bool isBreak = false;
    //            if (con.Id == 1 && con.IsConnected)
    //            {
    //                foreach (Connector con2 in con.AllRefs)
    //                {
    //                    if (con2.Owner is FamilyInstance)
    //                    {
    //                        con.DisconnectFrom(con2);
    //                        otherCon = con2;
    //                        isBreak = true;
    //                        break;
    //                    }
    //                }
    //            }
    //            if (isBreak)
    //            {
    //                break;
    //            }
    //        }


    //        //改原来的管
    //        (mEPCurve.Location as LocationCurve).Curve = line;

    //        //改现在的管
    //        (mEPCurveCopy.Location as LocationCurve).Curve = line2;

    //        //让拷贝的管连接原来管连接的连接器
    //        if (otherCon != null)
    //        {


    //            foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //            {
    //                if (con.Id == 1)
    //                {
    //                    con.ConnectTo(otherCon);
    //                }
    //            }
    //        }
    //        return mEPCurveCopy;
    //    }

    //    /// <summary>
    //    /// 创建弯头
    //    /// </summary>
    //    /// <returns></returns>
    //    public FamilyInstance CreateElow(MEPCurve mEPCurve, MEPCurve mEPCurve2)
    //    {
    //        //获得两根管
    //        //  Reference reference1 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //  Reference reference2 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);          
    //        // MEPCurve mEPCurve = document.GetElement(reference1) as MEPCurve;
    //        //   MEPCurve mEPCurve2 = document.GetElement(reference2) as MEPCurve;

    //        //获得两个管最近的连接器
    //        Connector con1 = null;
    //        Connector con2 = null;
    //        double minDistance = double.MaxValue;
    //        foreach (Connector connector in mEPCurve.ConnectorManager.Connectors)
    //        {
    //            foreach (Connector connector2 in mEPCurve2.ConnectorManager.Connectors)
    //            {
    //                double distance = connector.Origin.DistanceTo(connector2.Origin);
    //                if (distance < minDistance)
    //                {
    //                    minDistance = distance;
    //                    con1 = connector;
    //                    con2 = connector2;
    //                }
    //            }
    //        }
    //        FamilyInstance instance = document.Create.NewElbowFitting(con1, con2);
    //        return instance;
    //    }

    //    /// <summary>
    //    /// 管道连接器
    //    /// </summary>
    //    public void GetConnector()
    //    {

    //        Reference reference = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        MEPCurve pipe = document.GetElement(reference) as MEPCurve;
    //        //获得管的连接器
    //        ConnectorManager connectorManager = pipe.ConnectorManager;
    //        ConnectorSet connectorSet = connectorManager.Connectors;

    //        //遍历
    //        StringBuilder stringBuilder = new StringBuilder();
    //        foreach (Connector connector in connectorSet)
    //        {
    //            XYZ direction = connector.CoordinateSystem.BasisZ;

    //            stringBuilder.AppendLine("id  " + connector.Id + "; 是否连接  " + connector.IsConnected + ";  类型  " + connector.Domain + "  owen的id  " + connector.Owner.Id
    //                + "; 朝向 " + direction);

    //            //返回管连接器的所有连接器
    //            ConnectorSet connectorSet1 = connector.AllRefs;

    //            foreach (Connector connector1 in connectorSet1)
    //            {
    //                if (connector1.Owner is FamilyInstance)
    //                {
    //                    FamilyInstance familyInstance = connector1.Owner as FamilyInstance;
    //                    stringBuilder.AppendLine("连接的元素的id是 " + familyInstance.Id + " 名称是 " + familyInstance.Name);

    //                    //获取弯头的所有连接器
    //                    ConnectorSet connectorSet3 = familyInstance.MEPModel.ConnectorManager.Connectors;
    //                    foreach (Connector connector2 in connectorSet3)
    //                    {
    //                        if (connector2.IsConnected)
    //                        {
    //                            //获取弯头某端连接器连接的东西
    //                            foreach (Connector connector3 in connector2.AllRefs)
    //                            {
    //                                stringBuilder.AppendLine("弯头连接了 " + connector3.Owner.Id + "  name是" + connector3.Owner.Name);

    //                            }
    //                        }

    //                    }


    //                    break;
    //                }
    //            }
    //            stringBuilder.AppendLine();
    //        }
    //        TaskDialog.Show("提示", stringBuilder.ToString());
    //    }

    //    /// <summary>
    //    /// 创建桥架
    //    /// </summary>
    //    /// <returns></returns>
    //    public CableTray CreateCabkeTray()
    //    {
    //        //获取桥架类型
    //        FilteredElementCollector elements = new FilteredElementCollector(document);
    //        List<CableTrayType> cableTrayTypes = elements.OfClass(typeof(CableTrayType)).Cast<CableTrayType>().ToList();
    //        CableTrayType cableTrayType = cableTrayTypes.FirstOrDefault();

    //        XYZ point = uIDocument.Selection.PickPoint();
    //        XYZ point2 = uIDocument.Selection.PickPoint();

    //        Level level = document.ActiveView.GenLevel;


    //        CableTray cableTray = CableTray.Create(document, cableTrayType.Id, point, point2, level.Id);

    //        Parameter parameter = cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM);
    //        parameter.Set(400.Tofoot());
    //        cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(200.Tofoot());
    //        return cableTray;
    //    }

    //    /// <summary>
    //    /// 创建线管
    //    /// </summary>
    //    /// <returns></returns>
    //    public Conduit CreateConduit()
    //    {
    //        //获取线管类型
    //        FilteredElementCollector elements = new FilteredElementCollector(document);
    //        List<ConduitType> conduitTypes = elements.OfClass(typeof(ConduitType)).Cast<ConduitType>().ToList();
    //        // ConduitType conduitType = conduitTypes.FirstOrDefault();
    //        ConduitType conduitType = null;
    //        foreach (ConduitType conduitType2 in conduitTypes)
    //        {
    //            if (conduitType2.FamilyName.Contains("带配件"))
    //            {
    //                conduitType = conduitType2;
    //                break;
    //            }
    //        }

    //        XYZ point = uIDocument.Selection.PickPoint();
    //        XYZ point2 = uIDocument.Selection.PickPoint();
    //        Level level = document.ActiveView.GenLevel;
    //        Conduit conduit = Conduit.Create(document, conduitType.Id, point, point2, level.Id);
    //        conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(20.Tofoot());
    //        return conduit;
    //    }

    //    /// <summary>
    //    /// 创建管道
    //    /// </summary>
    //    /// <returns></returns>
    //    public Pipe CreatePipe()
    //    {
    //        //得到管道系统
    //        FilteredElementCollector elements = new FilteredElementCollector(document);
    //        List<PipingSystemType> pipingSystemTypes = elements.OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>().ToList();
    //        PipingSystemType pipingSystem = null;
    //        foreach (PipingSystemType pipingSystemType in pipingSystemTypes)
    //        {
    //            if ("家用冷水".Equals(pipingSystemType.Name))
    //            {
    //                pipingSystem = pipingSystemType;
    //                break;
    //            }
    //        }
    //        //得到管道类型
    //        FilteredElementCollector elements2 = new FilteredElementCollector(document);
    //        List<PipeType> pipeTypes = elements2.OfClass(typeof(PipeType)).Cast<PipeType>().ToList();
    //        PipeType pipeType = null;
    //        foreach (PipeType pipeType2 in pipeTypes)
    //        {
    //            if (pipeType2.Name.Contains("塑料管"))
    //            {
    //                pipeType = pipeType2;
    //                break;
    //            }
    //        }
    //        //标高
    //        Level level = document.ActiveView.GenLevel;

    //        //点
    //        XYZ startPoint = uIDocument.Selection.PickPoint();
    //        XYZ endPoint = uIDocument.Selection.PickPoint();

    //        Pipe pipe = Pipe.Create(document, pipingSystem.Id, pipeType.Id, level.Id, startPoint, endPoint);


    //        //修改偏移量
    //        pipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM).Set(2000.Tofoot());

    //        //修改直径
    //        pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(20.Tofoot());

    //        //换循环供水
    //        PipingSystemType pipeSystemType2 = null;
    //        foreach (PipingSystemType pipingSystemType in pipingSystemTypes)
    //        {
    //            if ("循环供水".Equals(pipingSystemType.Name))
    //            {
    //                pipeSystemType2 = pipingSystemType;
    //                break;
    //            }
    //        }
    //        pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).Set(pipeSystemType2.Id);
    //        return pipe;
    //    }

    //    /// <summary>
    //    /// 创建风管
    //    /// </summary>
    //    /// <param name="shape">形状</param>
    //    /// <returns>风管</returns>
    //    public Duct CreateDuct(DuctShape shape)
    //    {
    //        //风系统
    //        FilteredElementCollector elements = new FilteredElementCollector(document);
    //        List<MechanicalSystemType> mechanicalSystemTypes = elements.OfClass(typeof(MechanicalSystemType)).Cast<MechanicalSystemType>().ToList();
    //        MechanicalSystemType systemType = null;
    //        foreach (MechanicalSystemType mechanicalSystemType in mechanicalSystemTypes)
    //        {
    //            if (mechanicalSystemType.Name.Contains("送风"))
    //            {
    //                systemType = mechanicalSystemType;
    //                break;
    //            }
    //        }

    //        //管道类型
    //        FilteredElementCollector elements2 = new FilteredElementCollector(document);
    //        List<DuctType> ductTypes = elements2.OfClass(typeof(DuctType)).Cast<DuctType>().ToList();

    //        DuctType ductType = null;
    //        foreach (DuctType ductType2 in ductTypes)
    //        {
    //            switch (shape)
    //            {
    //                case DuctShape.Round:
    //                    if (ductType2.FamilyName.Contains("圆形"))
    //                    {
    //                        ductType = ductType2;

    //                    }
    //                    break;
    //                case DuctShape.Rectangle:
    //                    if (ductType2.FamilyName.Contains("矩形"))
    //                    {
    //                        ductType = ductType2;

    //                    }
    //                    break;
    //                case DuctShape.Oval:
    //                    if (ductType2.FamilyName.Contains("椭圆"))
    //                    {
    //                        ductType = ductType2;

    //                    }
    //                    break;
    //                default:
    //                    break;
    //            }

    //            if (ductType != null)
    //            {
    //                break;
    //            }
    //        }

    //        Level level = document.ActiveView.GenLevel;

    //        XYZ point1 = uIDocument.Selection.PickPoint();
    //        XYZ point2 = uIDocument.Selection.PickPoint();

    //        Duct duct = Duct.Create(document, systemType.Id, ductType.Id, level.Id, point1, point2);
    //        switch (shape)
    //        {
    //            case DuctShape.Round:
    //                duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).Set(600.Tofoot());
    //                break;
    //            case DuctShape.Rectangle:
    //            case DuctShape.Oval:

    //                //改宽度
    //                duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).Set(600.Tofoot());
    //                //改高度
    //                duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).Set(300.Tofoot());
    //                break;
    //        }
    //        return duct;
    //    }

    //    public enum DuctShape
    //    {
    //        /// <summary>
    //        /// 圆形
    //        /// </summary>
    //        Round,
    //        /// <summary>
    //        /// 矩形
    //        /// </summary>
    //        Rectangle,
    //        /// <summary>
    //        /// 椭圆
    //        /// </summary>
    //        Oval

    //    }

    //}
}
