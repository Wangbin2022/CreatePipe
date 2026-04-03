namespace CreatePipe
{
    //////0323 待测试的单点连续打断方法 
    ///原则上成功，但切换文档会强制退出选取模式是否还要深化？？
    //[Transaction(TransactionMode.Manual)]
    //public class MEPCurveBreakSingle : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Document doc = commandData.Application.ActiveUIDocument.Document;
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        // 开启循环实现连续拾取
    //        while (true)
    //        {
    //            try
    //            {
    //                //// 1. 拾取对象和点
    //                //// 注意：这里需要每次循环都提示用户选择
    //                //Reference reference = uiDoc.Selection.PickObject(
    //                //    Autodesk.Revit.UI.Selection.ObjectType.PointOnElement, // 改用 PointOnElement 直接获取点更精准
    //                //    new filterMEPCurveClass(),
    //                //    "请选择管线上的打断点 (按ESC退出)");
    //                Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass(), "请选择管线上的打断点 (按ESC退出)");
    //                if (reference == null) break;
    //                MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //                XYZ breakXYZ = reference.GlobalPoint;

    //                // 2. 执行打断事务
    //                // 建议将 Transaction 放在循环内部，这样每次打断都是独立的撤销步骤
    //                NewTransaction.Execute(doc, "单点打断", () =>
    //                {
    //                    BreakMEPCurveByOne(doc, mEPCurve, breakXYZ);
    //                });
    //            }
    //            // 3. 核心：捕获 ESC 键异常
    //            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //            {
    //                string tt = "提示";
    //                string myMessage = "已退出选取状态，请重试";
    //                RevitStylePopup.Show(tt, myMessage);
    //                break;
    //            }
    //            catch (Exception ex)
    //            {
    //                // 其他异常处理
    //                message = ex.Message;
    //                return Result.Failed;
    //            }
    //        }
    //        return Result.Succeeded;
    //    }
    //    public void BreakMEPCurveByOne(Document doc, MEPCurve mEPCurve, XYZ breakPoint)
    //    {
    //        // 1. 投影点确保在中心线上
    //        LocationCurve locCurve = mEPCurve.Location as LocationCurve;
    //        Curve oriCurve = locCurve.Curve;
    //        IntersectionResult projection = oriCurve.Project(breakPoint);
    //        if (projection == null) return;
    //        breakPoint = projection.XYZPoint;
    //        // 2. 识别原管两端的连接信息
    //        XYZ startPoint = oriCurve.GetEndPoint(0);
    //        XYZ endPoint = oriCurve.GetEndPoint(1);
    //        // 找到原管靠近 End1 (终点) 的连接器并断开，记录它连接的对象
    //        Connector endConnector = ConnectorService.GetClosestConnector(mEPCurve, endPoint);
    //        // 假设 DisconnectFromVendor 是你自己写的逻辑，返回断开前的对方连接器
    //        Connector remotePartner = ConnectorService.DisconnectFromVendor(endConnector);
    //        // 3. 拷贝元素
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, XYZ.Zero);
    //        MEPCurve mEPCurveCopy = doc.GetElement(ids.First()) as MEPCurve;
    //        // 4. 更新几何 (先更新原管，再更新新管)
    //        locCurve.Curve = Line.CreateBound(startPoint, breakPoint);
    //        (mEPCurveCopy.Location as LocationCurve).Curve = Line.CreateBound(breakPoint, endPoint);
    //        // 5. 恢复连接
    //        if (remotePartner != null)
    //        {
    //            Connector copyEndConn = ConnectorService.GetClosestConnector(mEPCurveCopy, endPoint);
    //            if (copyEndConn != null)
    //            {
    //                copyEndConn.ConnectTo(remotePartner);
    //            }
    //        }
    //        //// 6. 额外处理：打断点处的两个连接器通常需要相互连接或放置管件
    //        //Connector connAtBreakOrig = ConnectorService.GetClosestConnector(mEPCurve, breakPoint);
    //        //Connector connAtBreakCopy = ConnectorService.GetClosestConnector(mEPCurveCopy, breakPoint);
    //        //if (connAtBreakOrig != null && connAtBreakCopy != null)
    //        //{
    //        //    // 如果只是物理打断，可以尝试连接它们（Revit会自动生成Union管件，取决于设置）
    //        //    // connAtBreakOrig.ConnectTo(connAtBreakCopy); 
    //        //}
    //    }
    //}

    ////0323 已迁移的单点一次打断方法  
    //[Transaction(TransactionMode.Manual)]
    //public class MEPCurveBreakSingle : IExternalCommand
    //{
    //    Document doc = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        doc = commandData.Application.ActiveUIDocument.Document;
    //        try
    //        {
    //            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //            Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass());
    //            MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
    //            XYZ breakXYZ = reference.GlobalPoint;
    //            NewTransaction.Execute(doc, "单点打断", () =>
    //                {
    //                    BreakMEPCurveByOne(mEPCurve, breakXYZ);
    //                });
    //            return Result.Succeeded;
    //        }
    //        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //        {
    //            string tt = "提示";
    //            string myMessage = "已退出选取状态，请重试";
    //            RevitStylePopup.Show(tt, myMessage);
    //            return Result.Cancelled;
    //        }
    //        catch (Exception)
    //        {
    //            return Result.Failed;
    //        }
    //    }
    //    public MEPCurve BreakMEPCurveByOne(MEPCurve mEPCurve, XYZ breakPoint)
    //    {
    //        // 1. 投影点确保在中心线上
    //        Curve oriCurve = (mEPCurve.Location as LocationCurve).Curve;
    //        breakPoint = oriCurve.Project(breakPoint).XYZPoint;
    //        // 2. 识别原管两端的连接信息 (此处以End0和End1逻辑替代Hardcode的Id)
    //        XYZ startPoint = oriCurve.GetEndPoint(0);
    //        XYZ endPoint = oriCurve.GetEndPoint(1);
    //        // 找到原管靠近 End1 (终点) 的连接器并断开，记录它连接的对象
    //        Connector endConnector = ConnectorService.GetClosestConnector(mEPCurve, endPoint);
    //        Connector remotePartner = ConnectorService.DisconnectFromVendor(endConnector);
    //        // 3. 拷贝元素
    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, XYZ.Zero);
    //        MEPCurve mEPCurveCopy = doc.GetElement(ids.First()) as MEPCurve;
    //        // 4. 更新几何
    //        (mEPCurve.Location as LocationCurve).Curve = Line.CreateBound(startPoint, breakPoint);
    //        (mEPCurveCopy.Location as LocationCurve).Curve = Line.CreateBound(breakPoint, endPoint);
    //        // 5. 恢复连接
    //        if (remotePartner != null)
    //        {
    //            // 在新管上找到对应的端点连接器，连接回原来的 remotePartner
    //            Connector copyEndConn = ConnectorService.GetClosestConnector(mEPCurveCopy, endPoint);
    //            if (copyEndConn != null)
    //            {
    //                copyEndConn.ConnectTo(remotePartner);
    //            }
    //        }
    //        return mEPCurveCopy;
    //    }
    ////修改前原始代码
    //    //public MEPCurve BreakMEPCurveByOne(ExternalCommandData commandData, MEPCurve mEPCurve, XYZ xYZ)
    //    //{
    //    //    Document doc = commandData.Application.ActiveUIDocument.Document;
    //    //    try
    //    //    {
    //    //        XYZ breakXYZ = xYZ;
    //    //        MEPCurve mEPCurveCopy = null;//变量声明放到事务外才能访问

    //    //        //拷贝一根管
    //    //        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
    //    //        ElementId newId = ids.FirstOrDefault();
    //    //        mEPCurveCopy = doc.GetElement(newId) as MEPCurve;
    //    //        //原管的线
    //    //        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
    //    //        XYZ startXYZ = curve.GetEndPoint(0);
    //    //        XYZ endXYZ = curve.GetEndPoint(1);
    //    //        //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
    //    //        breakXYZ = curve.Project(breakXYZ).XYZPoint;
    //    //        //给原管用的线
    //    //        Line line = Line.CreateBound(startXYZ, breakXYZ);
    //    //        //找连接器并取消多余连接，保存连接信息P28
    //    //        Connector othercon = null;
    //    //        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
    //    //        {
    //    //            bool isBreak = false;
    //    //            //获取id后，找连接的情况，再解除连接
    //    //            if (con.Id == 1 && con.IsConnected)
    //    //            {
    //    //                foreach (Connector con2 in con.AllRefs)
    //    //                {
    //    //                    if (con2.Owner is FamilyInstance)
    //    //                    {
    //    //                        con.DisconnectFrom(con2);
    //    //                        othercon = con2;
    //    //                        isBreak = true;
    //    //                        break;
    //    //                    }
    //    //                }
    //    //            }
    //    //            if (isBreak)
    //    //            {
    //    //                break;
    //    //            }
    //    //        }
    //    //                (mEPCurve.Location as LocationCurve).Curve = line;
    //    //        //拷贝管用的线
    //    //        Line line1 = Line.CreateBound(breakXYZ, endXYZ);
    //    //        (mEPCurveCopy.Location as LocationCurve).Curve = line1;
    //    //        //拷贝管连接老管的连接器
    //    //        if (othercon != null)
    //    //        {
    //    //            foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
    //    //            {
    //    //                if (con.Id == 1)
    //    //                {
    //    //                    con.ConnectTo(othercon);
    //    //                }
    //    //            }
    //    //        }
    //    //        return mEPCurveCopy;
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        TaskDialog.Show("功能退出", ex.Message);
    //    //    }
    //    //    return null;
    //    //}
    //}
}
