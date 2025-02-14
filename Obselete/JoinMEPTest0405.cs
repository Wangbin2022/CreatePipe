namespace Test0405
{
    //[Transaction(TransactionMode.Manual)]
    //public class JoinMEPTest0405 : IExternalCommand
    //{
    //    FamilyInstance fitting = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication application = commandData.Application;
    //        UIDocument activeUIDocument = application.ActiveUIDocument;
    //        Document doc = activeUIDocument.Document;
    //        Selection selection = activeUIDocument.Selection;


    //        //Element element = document.GetElement(selection.PickObject((ObjectType)1));
    //        //MEPCurve val = (MEPCurve)(object)((element is MEPCurve) ? element : null);
    //        //Element element2 = document.GetElement(selection.PickObject((ObjectType)1));
    //        //MEPCurve val2 = (MEPCurve)(object)((element2 is MEPCurve) ? element2 : null);

    //        Reference reference1 = activeUIDocument.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass());
    //        MEPCurve val1 = doc.GetElement(reference1) as MEPCurve;
    //        XYZ xyz1 = reference1.GlobalPoint;
    //        Curve curve1 = (val1.Location as LocationCurve).Curve;
    //        xyz1 = curve1.Project(xyz1).XYZPoint;

    //        Reference reference2 = activeUIDocument.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass());
    //        MEPCurve val2 = doc.GetElement(reference2) as MEPCurve;
    //        XYZ xyz2 = reference2.GlobalPoint;
    //        Curve curve2 = (val2.Location as LocationCurve).Curve;
    //        xyz2 = curve2.Project(xyz2).XYZPoint;

    //        if (val1.Category.Id != val2.Category.Id)
    //        {
    //            TaskDialog.Show("说明", "请勿连接不同类型管线");
    //            return Result.Cancelled;
    //        }
    //        else
    //        {
    //            if (xyz1.Z - xyz2.Z > 0.001)
    //            {
    //                TaskDialog.Show("说明", "命令不支持非相同高度管道连接，请手工连接");
    //                return Result.Cancelled;
    //            }

    //            using (Transaction ts = new Transaction(doc, "两管连接"))
    //            {
    //                ts.Start();
    //                XYZ dir1 = ((val1.Location as LocationCurve).Curve as Line).Direction;
    //                XYZ dir2 = ((val2.Location as LocationCurve).Curve as Line).Direction;


    //                if (dir2.CrossProduct(dir1).GetLength() < 0.001)//检测是否共线
    //                {
    //                    //先判断MEPcurve类型，再判断是否共径
    //                    switch (val1)
    //                    {
    //                        case Pipe pipe:
    //                            Pipe myPipe1 = val1 as Pipe;
    //                            Pipe myPipe2 = val2 as Pipe;
    //                            Double dia1 = myPipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //                            Double dia2 = myPipe2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //                            if (Math.Abs(dia1 - dia2) > 0.001)
    //                            {
    //                                Pipe bigPipe = dia1 > dia2 ? myPipe1 : myPipe2;
    //                                MEPCurve bigMCP = bigPipe;
    //                                MepCurveDiffSizeJoin(doc, val1, val2, bigMCP);
    //                                ts.Commit();
    //                                return Result.Succeeded;
    //                            }
    //                            break;
    //                        case Duct duct:
    //                            Duct duct1 = val1 as Duct;
    //                            Duct duct2 = val2 as Duct;
    //                            Double wc1 = duct1.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
    //                            Double wc2 = duct2.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
    //                            if (Math.Abs(wc1 - wc2) > 0.001)
    //                            {
    //                                Duct bigDuct = wc1 > wc2 ? duct1 : duct2;
    //                                MEPCurve bigMCD = bigDuct;
    //                                MepCurveDiffSizeJoin(doc, val1, val2, bigMCD);
    //                                ts.Commit();
    //                                return Result.Succeeded;
    //                            }
    //                            break;
    //                        case CableTray cableTray:
    //                            CableTray cableTray1 = val1 as CableTray;
    //                            CableTray cableTray2 = val2 as CableTray;
    //                            Double width1 = cableTray1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
    //                            Double width2 = cableTray2.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
    //                            if (Math.Abs(width1 - width2) > 0.001)
    //                            {
    //                                CableTray bigCableTray = width1 > width2 ? cableTray1 : cableTray2;
    //                                MEPCurve bigMCC = bigCableTray;
    //                                MepCurveDiffSizeJoin(doc, val1, val2, bigMCC);
    //                                ts.Commit();
    //                                return Result.Succeeded;
    //                            }
    //                            break;
    //                        default:
    //                            // 如果MEPCurve不是上述任何类型，执行默认操作
    //                            // 可能是其他类型的MEPCurve，或者是一个未知的新类型
    //                            TaskDialog.Show("说明", "不支持该类型管线连接");
    //                            return Result.Cancelled;
    //                    }

    //                    //共线且同径管道直接连接
    //                    {
    //                        List<Connector> list1 = FarConnector(GetConnectorSet((Element)(object)val1), GetConnectorSet((Element)(object)val2));
    //                        List<Connector> list2 = NearConnector(GetConnectorSet((Element)(object)val1), GetConnectorSet((Element)(object)val2));
    //                        //调用FarConnector和NearConnector方法来获取两组连接器，list1近点，list2远点
    //                        Connector val4 = null;
    //                        Connector val5 = null;//声明两个Connector变量，用于存储可能需要断开连接的连接器。
    //                        if (list1[0].IsConnected)
    //                        {
    //                            val4 = GetConnectedCon(list1[0]);
    //                            val4.DisconnectFrom(list1[0]);
    //                        }
    //                        if (list1[1].IsConnected)
    //                        {
    //                            val5 = GetConnectedCon(list1[1]);
    //                            val5.DisconnectFrom(list1[1]);
    //                        }
    //                        //检查list中的第一个和第二个连接器是否已连接。如果是，获取它们连接的连接器，并执行断开连接的操作。
    //                        Location location = ((Element)val1).Location;
    //                        Curve curve = ((LocationCurve)((location is LocationCurve) ? location : null)).Curve;
    //                        Line curve2a = ((!curve.GetEndPoint(0).IsAlmostEqualTo(list1[0].Origin, 0.0001)) ? Line.CreateBound(list1[1].Origin, list1[0].Origin) : Line.CreateBound(list1[0].Origin, list1[1].Origin));
    //                        //根据第一个连接器的原点与曲线的一个端点是否接近，创建一个新的Line对象。
    //                        Location location2 = ((Element)val1).Location;//将新创建的Line曲线赋值给第一个MEPCurve元素的位置。
    //                        ((LocationCurve)((location2 is LocationCurve) ? location2 : null)).Curve = (Curve)(object)curve2a;
    //                        //如果val4或val5不为null，则将它们重新连接到相应的连接器。
    //                        if (val4 != null)
    //                        {
    //                            val4.ConnectTo(list1[0]);
    //                        }
    //                        if (val5 != null)
    //                        {
    //                            val5.ConnectTo(list2[0]);
    //                        }
    //                        doc.Delete(((Element)val2).Id);
    //                    }
    //                }
    //                else //不共线管道直接生成弯头连接件
    //                {
    //                    List<Connector> list1 = NearConnector(GetConnectorSet((Element)(object)val1), GetConnectorSet((Element)(object)val2));
    //                    fitting = doc.Create.NewElbowFitting(list1.First(), list1.ElementAt(1));
    //                }
    //                ts.Commit();
    //            }
    //            return Result.Succeeded;
    //        }

    //    }

    //    public void MepCurveDiffSizeJoin(Document ddoc, MEPCurve mCurve1, MEPCurve mCurve2, MEPCurve mCurveBig)
    //    {
    //        List<Connector> list1 = NearConnector(GetConnectorSet((Element)(object)mCurve1), GetConnectorSet((Element)(object)mCurve2));
    //        try
    //        {
    //            fitting = ddoc.Create.NewTransitionFitting(list1.First(), list1.ElementAt(1));
    //            XYZ mid = (mCurveBig.Location as LocationCurve).Curve.Evaluate(0.5, true);
    //            XYZ dir = (list1.First().Origin - mid).Normalize();
    //            //断点向小管道方向移动
    //            ElementTransformUtils.MoveElement(ddoc, fitting.Id, dir * 50 / 304.8);
    //        }
    //        catch (Exception) { }


    //        return;
    //    }

    //    public Connector GetConnectedCon(Connector connector) //
    //    {
    //        Connector result = null;
    //        ConnectorSet allRefs = connector.AllRefs;
    //        XYZ basisZ = connector.CoordinateSystem.BasisZ;
    //        XYZ origin = connector.Origin;
    //        foreach (Connector item in allRefs)
    //        {
    //            Connector val = item;
    //            if ((int)val.ConnectorType == 1 || (int)val.ConnectorType == 2)
    //            {
    //                XYZ origin2 = val.Origin;
    //                XYZ basisZ2 = val.CoordinateSystem.BasisZ;
    //                if (origin.IsAlmostEqualTo(origin2) && IsOppositeDirection(basisZ, basisZ2))
    //                //如果当前连接器的原点origin2与传入连接器的原点origin非常接近（使用IsAlmostEqualTo方法判断），并且它们的Z轴向量方向相反（使用IsOppositeDirection方法判断），则进入if块
    //                {
    //                    result = val;
    //                }
    //            }
    //        }
    //        return result;//返回找到的连接器result。如果没有找到符合条件的连接器，则返回null
    //    }

    //    public bool IsOppositeDirection(XYZ dir1, XYZ dir2) //检测管道方向返回是否相反，有默认规则？？
    //    {
    //        bool result = false;
    //        double num = dir1.Normalize().DotProduct(dir2.Normalize());
    //        if (Math.Abs(num + 1.0) < 0.0001)
    //        {
    //            result = true;
    //        }
    //        return result;
    //    }
    //    public ConnectorSet GetConnectorSet(Element element) //找元素连接器返回组
    //    {
    //        if (element is Pipe || element is Duct || element is CableTray)
    //        {
    //            return ((MEPCurve)((element is MEPCurve) ? element : null)).ConnectorManager.Connectors;
    //        }
    //        return ((FamilyInstance)((element is FamilyInstance) ? element : null)).MEPModel.ConnectorManager.Connectors;
    //    }
    //    public List<Connector> NearConnector(ConnectorSet conset1, ConnectorSet conset2)//找最近的连接器并返回2个
    //    {
    //        List<Connector> list = new List<Connector>();
    //        double num = double.MaxValue;
    //        Connector item = null;
    //        Connector item2 = null;
    //        foreach (Connector item3 in conset1)
    //        {
    //            Connector val = item3;
    //            foreach (Connector item4 in conset2)
    //            {
    //                Connector val2 = item4;
    //                if (val.Origin.DistanceTo(val2.Origin) <= num)
    //                {
    //                    item = val;
    //                    item2 = val2;
    //                    num = val.Origin.DistanceTo(val2.Origin);
    //                }
    //            }
    //        }
    //        list.Add(item);
    //        list.Add(item2);
    //        return list;
    //    }
    //    public List<Connector> FarConnector(ConnectorSet conset1, ConnectorSet conset2)//找最远的连接器并返回2个
    //    {
    //        List<Connector> list = new List<Connector>();
    //        double num = double.MinValue;
    //        Connector item = null;
    //        Connector item2 = null;
    //        foreach (Connector item3 in conset1)
    //        {
    //            Connector val = item3;
    //            foreach (Connector item4 in conset2)
    //            {
    //                Connector val2 = item4;
    //                if (val.Origin.DistanceTo(val2.Origin) > num)
    //                {
    //                    item = val;
    //                    item2 = val2;
    //                    num = val.Origin.DistanceTo(val2.Origin);
    //                }
    //            }
    //        }
    //        list.Add(item);
    //        list.Add(item2);
    //        return list;
    //    }
    //}
}
