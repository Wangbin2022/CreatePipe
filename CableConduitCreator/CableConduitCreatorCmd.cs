namespace CreatePipe.CableConduitCreator
{
    //[Transaction(TransactionMode.Manual)]
    //public class CableConduitCreatorCmd : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uidoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uidoc.Document;
    //        Selection sel = uidoc.Selection;
    //        //选择起始和末端
    //        Element startElem = null;
    //        Element endElem = null;
    //        try
    //        {
    //            startElem = sel.PickObject(ObjectType.Element, new MEPElementFilter(), "Select Start 元素").GetElement(doc);
    //            endElem = sel.PickObject(ObjectType.Element, new MEPElementFilter(), "Select End 元素").GetElement(doc);
    //        }
    //        catch (OperationCanceledException e)
    //        {
    //            TaskDialog.Show("tt", e.Message.ToString());
    //            return Result.Cancelled;
    //        }
    //        List<Element> allMepElem = null;
    //       //选所有桥架
    //     SelectAllCableTray:
    //        try
    //        {
    //            allMepElem = sel.PickObjects(ObjectType.Element, new OnlyMepElemFilter(), "Select 所有元素").Select(p => p.GetElement(doc)).ToList();
    //        }
    //        catch (OperationCanceledException e)
    //        {
    //            TaskDialog.Show("tt", e.Message.ToString());
    //            return Result.Cancelled;
    //        }
    //        if (allMepElem.Count == 0)
    //        {
    //            TaskDialog.Show("tt", "请重新选择所有桥架");
    //            goto SelectAllCableTray;
    //        }
    //        var neighborList = GetNeighborList(doc, allMepElem);

    //        //TaskDialog.Show("tt", neighborList.Count().ToString());
    //        //找到邻接表
    //        List<List<ElementId>> allPaths = GetAllPaths(doc, neighborList, startElem.Id, endElem.Id);
    //        //验证方法找到至少一条路径
    //        //sel.SetElementIds(allPaths[0]);
    //        allPaths = allPaths.OrderBy(p => GetPathLength(doc, p)).ToList();
    //        //实例化窗口
    //        List<PathListVM> pathListVMs = new List<PathListVM>();
    //        foreach (var path in allPaths)
    //        {
    //            PathListVM vm = new PathListVM(path, allPaths.IndexOf(path) + 1, GetPathLength(doc, path));
    //            pathListVMs.Add(vm);
    //        }
    //        CableTrayPathForm cableTrayPathForm = new CableTrayPathForm(pathListVMs);
    //        //使用系统接口强制窗口最前
    //        IntPtr myPtr = Autodesk.Windows.ComponentManager.ApplicationWindow;
    //        WindowInteropHelper helper = new WindowInteropHelper(cableTrayPathForm);
    //        helper.Owner = myPtr;
    //        cableTrayPathForm.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

    //        cableTrayPathForm.ShowDialog();
    //        //List中取值
    //        List<ElementId> selectedElem = null;
    //        while (true)
    //        {
    //            if (cableTrayPathForm.isReview)
    //            {
    //                cableTrayPathForm.isReview = false;
    //                selectedElem = (cableTrayPathForm.ls_Path.SelectedItem as PathListVM).InternalPath;
    //                sel.SetElementIds(selectedElem);
    //                uidoc.RefreshActiveView();
    //                cableTrayPathForm.ShowDialog();
    //            }
    //            else if (cableTrayPathForm.isDraw)
    //            {
    //                selectedElem = (cableTrayPathForm.ls_Path.SelectedItem as PathListVM).InternalPath;
    //                break;
    //            }
    //            else if (cableTrayPathForm.isDirectClose)
    //            {
    //                return Result.Cancelled;
    //            }
    //        }

    //        //选取布置模板
    //        List<Element> templateElems = null;
    //     SelectTemplates:
    //        try
    //        {
    //            templateElems = sel.PickObjects(ObjectType.Element, new OnlyMepElemFilter(), "请选择样板组").Select(r => r.GetElement(doc)).ToList();
    //        }
    //        catch (OperationCanceledException e)
    //        {
    //            TaskDialog.Show("tt", e.Message.ToString());
    //            return Result.Cancelled;
    //        }
    //        if (templateElems.Count == 0)
    //        {
    //            TaskDialog.Show("tt", "用户退出");
    //            goto SelectTemplates;
    //        }
    //        //确定线管几何向量关系
    //        Dictionary<Conduit, XYZ> conduitVectorPairs = GetConduitVectorPairs(templateElems);
    //        //找出符合条件id
    //        List<ElementId> mainMepIds = (from i in selectedElem where i.GetElement(doc) is MEPCurve select i).ToList();
    //        List<List<ElementId>> allCreatedIds = new List<List<ElementId>>();
    //        //处于桥架范围外的异常线管
    //        List<ElementId> outOfCableIds = new List<ElementId>();
    //        //
    //        Options conduitOpt = new Options() { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
    //        Options cableTrayOpt = new Options() { ComputeReferences = true, DetailLevel = ViewDetailLevel.Medium };
    //        TransactionGroup transGroup = new TransactionGroup(doc, "自动布置电缆");
    //        transGroup.Start();
    //        //线管连接生成
    //        for (int j = 0; j < conduitVectorPairs.Count; j++)
    //        {
    //            List<ElementId> createdIds = new List<ElementId>();
    //            var cvPair = conduitVectorPairs.ElementAt(j);
    //            var templateConduit = cvPair.Key;
    //            var templateConduitType = templateConduit.GetTypeId().GetElement(doc) as ConduitType;
    //            var relationalVector = cvPair.Value;

    //            List<Conduit> createConduits = new List<Conduit>();
    //            //lookup参数能否查找RBS代替？
    //            //double conduitSize = templateConduit.LookupParameter("直径(公称尺寸)").AsDouble();
    //            double conduitSize = templateConduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble();
    //            Transaction trans = new Transaction(doc, "create");
    //            //创建线管
    //            for (int i = 0; i < mainMepIds.Count; i++)
    //            {
    //                Element elem1;
    //                Element elem2;
    //                bool isLastOne = false;
    //                if (i + 1 == mainMepIds.Count)
    //                {
    //                    isLastOne = true;
    //                    elem1 = mainMepIds[i - 1].GetElement(doc);
    //                    elem2 = mainMepIds[i].GetElement(doc);
    //                }
    //                else
    //                {
    //                    elem1 = mainMepIds[i].GetElement(doc);
    //                    elem2 = mainMepIds[i + 1].GetElement(doc);
    //                }
    //                trans.Start();
    //                //找线管端头就近连接
    //                ElementId hostCableId = null;
    //                var newConduit = CreateOneConduit(doc, elem1, elem2, templateConduitType,
    //                    relationalVector, isLastOne, out hostCableId);
    //                if (newConduit != null)
    //                {
    //                    //此处增加了转换newConduit
    //                    Parameter sizeParam = newConduit.Parameters
    //                        .Cast<Parameter>().First(p => p.Id.IntegerValue == (int)BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
    //                    sizeParam.Set(conduitSize);
    //                    createConduits.Add(newConduit);
    //                    trans.Commit();
    //                    if (IsOutOfCableTray(newConduit, hostCableId.GetElement(doc) as CableTray, conduitOpt, cableTrayOpt))
    //                    {
    //                        outOfCableIds.Add(newConduit.Id);
    //                    }
    //                }
    //                else trans.RollBack();
    //            }
    //            //延长线管
    //            List<ElementId> needToDelete = new List<ElementId>();
    //            List<ElementId> elbowIds = new List<ElementId>();
    //            for (int i = 0; i < createConduits.Count - 1; i++)
    //            {
    //                var conduit1 = createConduits[i];
    //                var conduit2 = createConduits[i + 1];
    //                var pair = ConnectorPair(conduit1, conduit2);
    //                var nearList = pair[0];
    //                var farList = pair[1];
    //                XYZ direction1 = ((conduit1.Location as LocationCurve).Curve as Line).Direction;
    //                XYZ direction2 = ((conduit2.Location as LocationCurve).Curve as Line).Direction;
    //                if (Math.Abs(direction1.DotProduct(direction2)) == 1)
    //                {
    //                    XYZ startPt = farList.First().Origin;
    //                    XYZ endPt = farList.Last().Origin;
    //                    Line locationLine = Line.CreateBound(startPt, endPt);
    //                    trans.Start();
    //                    (conduit1.Location as LocationCurve).Curve = locationLine;
    //                    trans.Commit();
    //                    if (!outOfCableIds.Contains(conduit1.Id) && outOfCableIds.Contains(conduit2.Id))
    //                    {
    //                        outOfCableIds.Add(conduit1.Id);
    //                    }
    //                    createConduits[i + 1] = conduit1;
    //                    needToDelete.Add(conduit2.Id);
    //                }
    //                else
    //                {
    //                    trans.Start();
    //                    //错误处理
    //                    FailureHandlingOptions errorOptions =trans.GetFailureHandlingOptions();
    //                    FailureHandler errorHandler = new FailureHandler();
    //                    errorOptions.SetFailuresPreprocessor(errorHandler);
    //                    errorOptions.SetClearAfterRollback(true);
    //                    trans.SetFailureHandlingOptions(errorOptions);
    //                    //提示弯头生成空间不足
    //                    elbowIds.Add(doc.Create.NewElbowFitting(nearList.First(), nearList.Last()).Id);
    //                    trans.Commit();
    //                    if (errorHandler.HasError)
    //                    {
    //                        TaskDialog.Show("tt", $"空间不足，无法生成弯头。请尝试：\n1、修改桥架间距\n2、减小线管尺寸");
    //                        return Result.Failed;
    //                    }
    //                }
    //                trans.Start();
    //                for (int k = 0; k < needToDelete.Count; k++)
    //                {
    //                    try
    //                    {
    //                        doc.Delete(needToDelete);
    //                    }
    //                    catch  { }
    //                }
    //                trans.Commit();
    //            }
    //            //输出新建的元素统计
    //            createdIds = (from con in createConduits where needToDelete.Contains(con.Id) == false select con.Id).ToList().Union(elbowIds).ToList();
    //            allCreatedIds.Add(createdIds);
    //        }

    //        transGroup.Assimilate();
    //        uidoc.RefreshActiveView();
    //        uidoc.ShowElements(mainMepIds);
    //        //生成输出xls字符串
    //        allCreatedIdsVM allCreatedIdsVM = new allCreatedIdsVM(doc, allCreatedIds);
    //        //显示未放进桥架的异常线管
    //        OutOfCableListVM outList = null;
    //        OutOfCableListForm outWindow = null;
    //        if (outOfCableIds.Count>0)
    //        {
    //            outOfCableIds=outOfCableIds.OrderBy(d=>GetCenterPoint(d.GetElement(doc))
    //            ?.DistanceTo(XYZ.Zero)).ToList();
    //            outList =new OutOfCableListVM(doc, outOfCableIds);
    //            outWindow = new OutOfCableListForm(outList);
    //            //窗口置顶
    //            WindowInteropHelper helper1=new WindowInteropHelper(outWindow);
    //            helper1.Owner=myPtr;
    //            outWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
    //            outWindow.Left = 100;
    //            outWindow.Top = 100;
    //            outWindow.Show();
    //        }
    //        try
    //        {
    //            CreateAndOpenExcelFile(doc, allCreatedIdsVM.AllParams);
    //        }
    //        catch (Exception e)
    //        {
    //            if (outWindow.IsActive)
    //            {
    //                outWindow.Close();
    //            }
    //            TaskDialog.Show("tt", "Excel生成异常"+e.Message);
    //            return Result.Failed;
    //        }
    //        return Result.Succeeded;
    //    }

    //    private void CreateAndOpenExcelFile(Document doc, string allParams)
    //    {
    //        var docPath = doc.PathName;
    //        var docName = doc.Title;
    //        string filePath = docPath.Replace(docName, "") + "CableInfo.xlsx";
    //        //以下使用Epplus
    //        using (ExcelPackage excelPackage = new ExcelPackage())
    //        {
    //            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("电缆明细表");
    //            ExcelTextFormat format = new ExcelTextFormat();
    //            format.Delimiter = '\t';
    //            worksheet.Cells["A1"].LoadFromText(allParams, format);
    //            int maxColumn = worksheet.Dimension.End.Column;
    //            int maxRow=worksheet.Dimension.End.Row;
    //            for (int i = 1; i <= maxColumn; i++)
    //            {
    //                worksheet.Column(i).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
    //                worksheet.Column(i).Style.VerticalAlignment=OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
    //            }
    //            for (int i = 1; i <= maxRow; i++)
    //            { 
    //                ExcelRow row =worksheet.Row(i);
    //                row.Height = 20;
    //                var cell = worksheet.Cells[i, 1];
    //                if (cell.Value == null || cell.Text == "")
    //                {
    //                    worksheet.Cells[i, 1, i, maxColumn].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
    //                }
    //                else 
    //                {
    //                    worksheet.Cells[i, 1, i, maxColumn].Style.Font.Color.SetColor(System.Drawing.Color.White);
    //                    worksheet.Cells[i,1,i,maxColumn].Style.Font.Bold=true;
    //                    worksheet.Cells[i, 1, i, maxColumn].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
    //                    worksheet.Cells[i, 1, i, maxColumn].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.CornflowerBlue);

    //                } 
    //            }
    //            worksheet.Cells.AutoFitColumns();
    //            FileInfo fi = new FileInfo(filePath);
    //            excelPackage.SaveAs(fi);
    //        }
    //        System.Diagnostics.Process.Start(filePath);
    //    }
    //    //返回两个2列表，0是近点，1是远点
    //    private List<List<Connector>> ConnectorPair(Conduit conduit1, Conduit conduit2)
    //    {
    //        List<Connector> nearList = new List<Connector>();
    //        List<Connector> farList = new List<Connector>();
    //        var cons1 = GetConnectors(conduit1);
    //        var cons2 = GetConnectors(conduit2);
    //        Connector con1 = NearConnector(cons1, cons2);
    //        Connector con2 = NearConnector(cons2, cons1);
    //        Connector con3 = FarConnector(cons1, cons2);
    //        Connector con4 = FarConnector(cons2, cons1);
    //        nearList.Add(con1);
    //        nearList.Add(con2);
    //        farList.Add(con3);
    //        farList.Add(con4);
    //        return new List<List<Connector>> { nearList, farList };
    //    }
    //    private bool IsOutOfCableTray(Conduit newConduit, CableTray cableTray, Options conduitOpt, Options cableTrayOpt)
    //    {
    //        var geoConduit = GetSolid(newConduit, conduitOpt);
    //        var geoCable = GetSolid(cableTray, cableTrayOpt);
    //        var geoInterSect = BooleanOperationsUtils.ExecuteBooleanOperation(geoConduit, geoCable, BooleanOperationsType.Intersect);
    //        double geoIntersectVolumn = Math.Round(geoInterSect.Volume, 2);
    //        double conduitVolumn = Math.Round(geoCable.Volume, 2);
    //        if (conduitVolumn != geoIntersectVolumn)
    //        {
    //            return true;
    //        }
    //        else return false;
    //    }
    //    private Solid GetSolid(Element elem, Options opt)
    //    {
    //        var eElem = elem.get_Geometry(opt);
    //        var eObj = eElem.GetEnumerator();
    //        eObj.MoveNext();
    //        var eElemOut = eObj.Current as Solid;
    //        return eElemOut;
    //    }
    //    private Conduit CreateOneConduit(Document doc, Element elem1, Element elem2, ConduitType templateConduitType, XYZ relationalVector, bool isLastOne, out ElementId hostCableId)
    //    {
    //        if (elem1 is CableTray && elem2 is CableTray)
    //        {
    //            CableTray tempCableTray1 = elem1 as CableTray;
    //            CableTray tempCableTray2 = elem2 as CableTray;
    //            CableTray needCableTray = null;
    //            var connector1 = GetConnectors(tempCableTray1);
    //            var connector2 = GetConnectors(tempCableTray2);

    //            var near1 = NearConnector(connector1, connector2);
    //            var far1 = FarConnector(connector1, connector2);
    //            var near2 = NearConnector(connector2, connector1);
    //            var far2 = FarConnector(connector2, connector1);

    //            Line line1 = Line.CreateBound(far1.Origin, near1.Origin);
    //            Line line2 = Line.CreateBound(near2.Origin, far2.Origin);
    //            Line line;
    //            if (isLastOne)
    //            {
    //                line = line2;
    //                needCableTray = tempCableTray2;
    //            }
    //            else
    //            {
    //                line = line1;
    //                needCableTray = tempCableTray1;
    //            }
    //            //确定生成标高
    //            Level level = needCableTray.ReferenceLevel;
    //            XYZ origin = line.GetEndPoint(0);
    //            XYZ y = line.Direction.Normalize();
    //            XYZ z = needCableTray.CurveNormal;
    //            XYZ x = y.CrossProduct(z).Normalize();
    //            //坐标变换类
    //            Transform tf = Transform.Identity;
    //            tf.BasisX = x;
    //            tf.BasisY = y;
    //            tf.BasisZ = z;
    //            tf.Origin = origin;
    //            XYZ newVector = tf.OfPoint(relationalVector);
    //            XYZ spt = newVector;
    //            XYZ ept = spt + y * line.Length;
    //            hostCableId = needCableTray.Id;

    //            return Conduit.Create(doc, templateConduitType.Id, spt, ept, level.Id);
    //        }
    //        else
    //        {
    //            hostCableId = null;
    //            return null;
    //        }
    //    }
    //    private Connector NearConnector(List<Connector> connectors, List<Connector> targetCons)
    //    {
    //        Connector target = targetCons.First();
    //        double distance = double.MaxValue;
    //        Connector nearCon = null;
    //        XYZ targetOrigin = target.Origin;
    //        foreach (Connector con in connectors)
    //        {
    //            if (con.Origin.DistanceTo(targetOrigin) < distance)
    //            {
    //                distance = con.Origin.DistanceTo(targetOrigin);
    //                nearCon = con;
    //            }
    //        }
    //        return nearCon;
    //    }
    //    private Connector FarConnector(List<Connector> connectors, List<Connector> targetCons)
    //    {
    //        Connector target = targetCons.First();
    //        double distance = double.MinValue;
    //        Connector farCon = null;
    //        XYZ targetOrigin = target.Origin;
    //        foreach (Connector con in connectors)
    //        {
    //            if (con.Origin.DistanceTo(targetOrigin) > distance)
    //            {
    //                distance = con.Origin.DistanceTo(targetOrigin);
    //                farCon = con;
    //            }
    //        }
    //        return farCon;
    //    }
    //    private List<Connector> GetConnectors(Element mepElem)
    //    {
    //        List<Connector> outList = new List<Connector>();
    //        ConnctorContains connctorContains = new ConnctorContains();
    //        if (mepElem is MEPCurve)
    //        {
    //            MEPCurve temp = mepElem as MEPCurve;
    //            foreach (Connector con in temp.ConnectorManager.Connectors)
    //            {
    //                if (
    //                    (con.ConnectorType == ConnectorType.End || con.ConnectorType == ConnectorType.Curve
    //                    || con.ConnectorType == ConnectorType.Physical) && !outList.Contains(con, connctorContains)
    //                    )
    //                {
    //                    outList.Add(con);
    //                }
    //            }
    //        }
    //        else if (mepElem is FamilyInstance)
    //        {
    //            FamilyInstance temp = mepElem as FamilyInstance;
    //            if (temp.MEPModel.ConnectorManager != null)
    //            {
    //                foreach (Connector con in temp.MEPModel.ConnectorManager.Connectors)
    //                {
    //                    if (
    //                        (con.ConnectorType == ConnectorType.End || con.ConnectorType == ConnectorType.Curve
    //                        || con.ConnectorType == ConnectorType.Physical) && !outList.Contains(con, connctorContains)
    //                        )
    //                    {
    //                        outList.Add(con);
    //                    }
    //                }
    //            }
    //        }
    //        else return null;
    //        return outList;
    //    }
    //    private Dictionary<Conduit, XYZ> GetConduitVectorPairs(List<Element> templateElems)
    //    {
    //        Dictionary<Conduit, XYZ> outPair = new Dictionary<Conduit, XYZ>();
    //        CableTray cableTray = (from e in templateElems where e is CableTray select e as CableTray).First();
    //        List<Conduit> conduits = (from e in templateElems where e is Conduit select e as Conduit).ToList();
    //        XYZ cableTrayPt = (cableTray.Location as LocationCurve).Curve.GetEndPoint(0);
    //        foreach (var conduit in conduits)
    //        {
    //            XYZ conduitPt;
    //            XYZ pt1 = (conduit.Location as LocationCurve).Curve.GetEndPoint(0);
    //            XYZ pt2 = (conduit.Location as LocationCurve).Curve.GetEndPoint(1);
    //            if (pt1.DistanceTo(cableTrayPt) < pt2.DistanceTo(cableTrayPt))
    //            {
    //                conduitPt = pt1;
    //            }
    //            else conduitPt = pt2;
    //            XYZ relationalVector = conduitPt - cableTrayPt;
    //            outPair.Add(conduit, relationalVector);
    //        }
    //        return outPair;
    //    }
    //    public double GetPathLength(Document doc, List<ElementId> path)
    //    {
    //        double lengthTemp = -1;
    //        for (int i = 0; i < path.Count - 1; i++)
    //        {
    //            lengthTemp += GetCenterPoint(path[i].GetElement(doc)).DistanceTo(GetCenterPoint(path[i + 1].GetElement(doc)));
    //        }
    //        Curve c = (path.First().GetElement(doc).Location as LocationCurve).Curve;
    //        lengthTemp += c.Length / 2;
    //        Curve c1 = (path.Last().GetElement(doc).Location as LocationCurve).Curve;
    //        lengthTemp += c1.Length / 2;
    //        return lengthTemp;
    //    }
    //    private XYZ GetCenterPoint(Element elem)
    //    {
    //        if (elem != null)
    //        {
    //            XYZ centerPoint;
    //            if (elem.Location is LocationCurve)
    //            {
    //                Curve c = (elem.Location as LocationCurve).Curve;
    //                //XYZ spt = c.GetEndPoint(0);
    //                //XYZ ept = c.GetEndPoint(1);
    //                //centerPoint = (spt + ept) / 2;
    //                //等同以上代码
    //                centerPoint = c.Evaluate(0.5, true);
    //            }
    //            else
    //            {
    //                centerPoint = (elem.Location as LocationPoint).Point;
    //            }
    //            return centerPoint;
    //        }
    //        else return null;
    //    }
    //    private List<List<ElementId>> GetAllPaths(Document doc, Dictionary<ElementId, List<ElementId>> neighborList, ElementId startElem, ElementId endElem)
    //    {
    //        List<List<ElementId>> result = new List<List<ElementId>>();
    //        Queue<List<ElementId>> queue = new Queue<List<ElementId>>();
    //        //将初始Id放进列表
    //        List<ElementId> firstList = new List<ElementId> { startElem };
    //        //队列入队
    //        queue.Enqueue(firstList);
    //        while (queue.Count > 0)
    //        {
    //            List<ElementId> path = queue.Dequeue();
    //            ElementId lastNode = path.Last();
    //            if (lastNode == endElem)
    //            {
    //                result.Add(path);
    //            }
    //            else
    //            {
    //                if (!neighborList.Keys.Contains(lastNode))
    //                {
    //                    continue;
    //                }
    //                List<ElementId> neighbors = null;
    //                neighbors = neighborList[lastNode];
    //                foreach (ElementId neighbor in neighbors)
    //                {
    //                    List<ElementId> newList = (from id in path select id).ToList();
    //                    if (!newList.Contains(neighbor))
    //                    {
    //                        newList.Add(neighbor);
    //                        queue.Enqueue(newList);
    //                    }
    //                }
    //            }
    //        }
    //        return result;
    //    }
    //    private Dictionary<ElementId, List<ElementId>> GetNeighborList(Document doc, List<Element> allMepElem)
    //    {
    //        Dictionary<ElementId, List<ElementId>> outDic = new Dictionary<ElementId, List<ElementId>>();
    //        foreach (var elem in allMepElem)
    //        {
    //            List<ElementId> allRefIds = new List<ElementId>();
    //            if (elem is MEPCurve)
    //            {
    //                var elemCons = (elem as MEPCurve).ConnectorManager.Connectors;
    //                foreach (Connector con in elemCons)
    //                {
    //                    var conRefs = con.AllRefs;
    //                    foreach (Connector conTemp in conRefs)
    //                    {
    //                        if (conTemp.ConnectorType == ConnectorType.Logical)
    //                        {
    //                            continue;
    //                        }
    //                        else if (conTemp.Owner.Id == elem.Id)
    //                        {
    //                            continue;
    //                        }
    //                        allRefIds.Add(conTemp.Owner.Id);
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                var elemCons = (elem as FamilyInstance).MEPModel.ConnectorManager.Connectors;
    //                foreach (Connector con in elemCons)
    //                {
    //                    var conRefs = con.AllRefs;
    //                    foreach (Connector conTemp in conRefs)
    //                    {
    //                        if (conTemp.ConnectorType == ConnectorType.Logical)
    //                        {
    //                            continue;
    //                        }
    //                        else if (conTemp.Owner.Id == elem.Id)
    //                        {
    //                            continue;
    //                        }
    //                        allRefIds.Add(conTemp.Owner.Id);
    //                    }
    //                }
    //            }
    //            outDic.Add(elem.Id, allRefIds);
    //        }
    //        return outDic;
    //    }
    //}
}
