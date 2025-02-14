namespace CreatePipe.TurnOver
{
    //[Transaction(TransactionMode.Manual)]
    //public class MEPCurveTurnOverCmd : IExternalCommand
    //{
    //    UIDocument uIDocument = null;
    //    Document document = null;
    //    Autodesk.Revit.ApplicationServices.Application application = null;

    //    TurnOverForm turnOverForm = null; //全局对象，因此提前定义
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uiApp = commandData.Application;
    //        application = uiApp.Application;
    //        uIDocument = uiApp.ActiveUIDocument;
    //        document = uIDocument.Document;

    //        ExternalEventExample externalEventExample = new ExternalEventExample(commandData); //新开外部事件，将参数全部导入
    //        externalEventExample.CreateExternalEvent(externalEventExample);//事件注册

    //        //窗体辅助和打开
    //        turnOverForm = new TurnOverForm(this, externalEventExample);
    //        turnOverForm.Show();
    //        //TurnOver();
    //        MessageBox.Show("翻弯完成");
    //        return Result.Succeeded;
    //    }

    //    //    public void TurnOver()
    //    //    {
    //    //        InstallCmd installCmd = new InstallCmd();
    //    //        using (Transaction transaction = new Transaction(document, "两点翻弯"))
    //    //        {
    //    //            transaction.Start();
    //    //            try
    //    //            {
    //    //                while (true)
    //    //                {
    //    //                    //打断点1
    //    //                    Reference reference = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter());
    //    //                    XYZ xyz1 = reference.GlobalPoint;
    //    //                    //打断点2
    //    //                    Reference reference2 = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter());
    //    //                    XYZ xyz2 = reference2.GlobalPoint;
    //    //                    //XYZ breakXYZ2 = uiDoc.Selection.PickPoint() //直接默认选的相同管会如何

    //    //                    //如果点的不是同一根管则不翻弯
    //    //                    if (reference.ElementId != reference2.ElementId)
    //    //                    {
    //    //                        return;
    //    //                    }
    //    //                    //如果距离过近不翻弯
    //    //                    //if (xyz1.DistanceTo(xyz2) < 0.001)
    //    //                    //{
    //    //                    //    continue;
    //    //                    //}
    //    //                    if (string.IsNullOrEmpty(turnOverForm.turnOverHeight_tb.Text))
    //    //                    {
    //    //                        continue;
    //    //                    }
    //    //                    //从窗口接收翻弯高度 转英尺
    //    //                    double height = Convert.ToDouble(turnOverForm.turnOverHeight_tb.Text).Tofoot();
    //    //                    if (height == 0)
    //    //                    {
    //    //                        continue;
    //    //                    }
    //    //                    if (string.IsNullOrEmpty(turnOverForm.angle_tb.Text))
    //    //                    {
    //    //                        continue;
    //    //                    }
    //    //                    //角度 度
    //    //                    double angle = Convert.ToDouble(turnOverForm.angle_tb.Text);
    //    //                    //度数转弧度
    //    //                    angle = angle.AngleToRadian();
    //    //                    //取管1 
    //    //                    MEPCurve mEPCurve = document.GetElement(reference2) as MEPCurve;

    //    //                    Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //    //                    xyz1 = curve.Project(xyz1).XYZPoint;
    //    //                    xyz2 = curve.Project(xyz2).XYZPoint;

    //    //                    //打断并接收打断后复制出的管2                                                                                                                                                                                                                  
    //    //                    MEPCurve mEPCurve2 = installCmd.BreakMEPCurve2(mEPCurve, xyz1, xyz2);
    //    //                    //翻弯方向
    //    //                    XYZ direction = null;
    //    //                    if (height > 0)
    //    //                    {
    //    //                        direction = XYZ.BasisZ;
    //    //                    }
    //    //                    else
    //    //                    {
    //    //                        direction = XYZ.BasisZ.Negate();
    //    //                    }

    //    //                    //立管1起点
    //    //                    XYZ vStart = xyz1;
    //    //                    //立管1终点，默认90度弯
    //    //                    //XYZ vEnd = new XYZ(vStart.X, vStart.Y, vStart.Z + height);

    //    //                    //非垂直90度上去高度的点
    //    //                    XYZ vEnd = null;
    //    //                    if (height > 0)
    //    //                    {
    //    //                        XYZ vEnd90 = vStart + direction * height;
    //    //                        XYZ direction2 = xyz2.Subtract(vStart).Normalize();
    //    //                        double jl = vEnd90.DistanceTo(vStart);
    //    //                        vEnd = vEnd90 + (jl / Math.Tan(angle)) * direction2;
    //    //                    }
    //    //                    else
    //    //                    {
    //    //                        XYZ vEnd90 = vStart - direction * height;
    //    //                        XYZ direction2 = xyz2.Subtract(vStart).Normalize();
    //    //                        double jl = vEnd90.DistanceTo(vStart);
    //    //                        vEnd = vEnd90 - (jl / Math.Tan(angle)) * direction2;
    //    //                    }

    //    //                    //立管2起点
    //    //                    XYZ vStart2 = xyz2;
    //    //                    //立管2终点
    //    //                    XYZ vEnd2 = new XYZ(vStart2.X, vStart2.Y, vStart2.Z + height);
    //    //                    if (height > 0)
    //    //                    {
    //    //                        XYZ vEnd90 = vStart2 + direction * height;
    //    //                        XYZ direction2 = vStart.Subtract(vStart2).Normalize();
    //    //                        double jl = vEnd90.DistanceTo(vStart2);
    //    //                        vEnd2 = vEnd90 + (jl / Math.Tan(angle)) * direction2;
    //    //                    }
    //    //                    else
    //    //                    {
    //    //                        XYZ vEnd90 = vStart - direction * height;
    //    //                        XYZ direction2 = xyz2.Subtract(vStart2).Normalize();
    //    //                        double jl = vEnd90.DistanceTo(vStart2);
    //    //                        vEnd = vEnd90 - (jl / Math.Tan(angle)) * direction2;
    //    //                    }

    //    //                    //取出系统和类型和标高
    //    //                    ElementId systemId = mEPCurve.MEPSystem.GetTypeId();
    //    //                    ElementId typeId = mEPCurve.GetTypeId();
    //    //                    ElementId levelId = mEPCurve.ReferenceLevel.Id;
    //    //                    Pipe vPipe1 = null;
    //    //                    Pipe vPipe2 = null;
    //    //                    Pipe hPipe1 = null;

    //    //                    CableTray vCableTray1 = null;
    //    //                    CableTray vCableTray2 = null;
    //    //                    CableTray hCableTray = null;


    //    //                    if (mEPCurve is Pipe)
    //    //                    {
    //    //                        //原来管的直径
    //    //                        double diameter = mEPCurve.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //    //                        //创建立管1
    //    //                        vPipe1 = Pipe.Create(document, systemId, typeId, levelId, vStart, vEnd);
    //    //                        //创建立管2
    //    //                        vPipe2 = Pipe.Create(document, systemId, typeId, levelId, vStart2, vEnd2);
    //    //                        //创建横管
    //    //                        hPipe1 = Pipe.Create(document, systemId, typeId, levelId, vEnd, vEnd2);
    //    //                        vPipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //    //                        vPipe2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
    //    //                        hPipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);

    //    //                        //创建弯头
    //    //                        installCmd.CreateElow(vPipe1, hPipe1);
    //    //                        installCmd.CreateElow(vPipe2, hPipe1);
    //    //                    }
    //    //                    else if (mEPCurve is Duct) //按类型补充
    //    //                    {


    //    //                    }
    //    //                    else if (mEPCurve is CableTray) //按类型补充
    //    //                    {
    //    //                        double cableTrayWidth=mEPCurve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
    //    //                        double cableTrayHeight = mEPCurve.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();

    //    //                        ElementId id = mEPCurve.GetTypeId();
    //    //                        vCableTray1 = CableTray.Create(document,id, vStart, vEnd, levelId);
    //    //                        vCableTray2 = CableTray.Create(document,id, vStart2, vEnd2, levelId);
    //    //                        hCableTray = CableTray.Create(document, id, vEnd, vEnd2, levelId);

    //    //                        vCableTray1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).Set(cableTrayWidth);
    //    //                        vCableTray2.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).Set(cableTrayWidth);
    //    //                        hCableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).Set(cableTrayWidth);
    //    //                        vCableTray1.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(cableTrayHeight);
    //    //                        vCableTray2.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(cableTrayHeight);
    //    //                        hCableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).Set(cableTrayHeight);

    //    //                        //installCmd.CreateElow(vCableTray1, hCableTray);
    //    //                        //installCmd.CreateElow(vCableTray2, hCableTray);
    //    //                    }



    //    //                    //取一个和vStart最近的管
    //    //                    List<MEPCurve> mEPCurves = new List<MEPCurve>() { mEPCurve, mEPCurve2 };
    //    //                    double minVal = double.MaxValue;
    //    //                    MEPCurve nearMep = null;
    //    //                    //取距离最小值
    //    //                    foreach (MEPCurve item in mEPCurves)
    //    //                    {
    //    //                        Curve mepCurve = (item.Location as LocationCurve).Curve;
    //    //                        double jl1 = mepCurve.GetEndPoint(0).DistanceTo(vStart);
    //    //                        double jl2 = mepCurve.GetEndPoint(1).DistanceTo(vStart);

    //    //                        if (jl1 < minVal)
    //    //                        {
    //    //                            minVal = jl1;
    //    //                            nearMep = item;
    //    //                        }
    //    //                        if (jl2 < minVal)
    //    //                        {
    //    //                            minVal = jl2;
    //    //                            nearMep = item;
    //    //                        }

    //    //                    }

    //    //                    if (nearMep != null)
    //    //                    {
    //    //                        MEPCurve otherMep = null;
    //    //                        if (nearMep.Id == mEPCurve.Id)
    //    //                        {
    //    //                            otherMep = mEPCurve2;
    //    //                        }
    //    //                        else
    //    //                        {
    //    //                            otherMep = mEPCurve;
    //    //                        }
    //    //                        installCmd.CreateElow(nearMep, vPipe1);
    //    //                        installCmd.CreateElow(otherMep, vPipe2);
    //    //                    }
    //    //                }
    //    //            }
    //    //            catch (Exception)
    //    //            {
    //    //            }
    //    //            transaction.Commit();
    //    //        }
    //    //    }

    //}
}
