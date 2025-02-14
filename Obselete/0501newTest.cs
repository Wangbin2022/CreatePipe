namespace CreatePipe.Obselete
{
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class _0501newTest : IExternalCommand
    //{
    //    UIDocument uiDoc = null;
    //    Document doc = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uiApp = commandData.Application;
    //        Application application = uiApp.Application;
    //        uiDoc = uiApp.ActiveUIDocument;
    //        doc = uiDoc.Document; //用全局定义，不要重复赋值
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;

    //        ////导出csv例程 P211
    //        ////单位转换系数
    //        //double s = UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Millimeters);
    //        //SaveFileDialog sfDialog = new SaveFileDialog();
    //        //sfDialog.Title = "导出csv文件";
    //        //sfDialog.Filter = "csv文件（*。csv）|*.csv";
    //        //if (DialogResult.OK != sfDialog.ShowDialog())
    //        //{
    //        //    return Result.Cancelled;
    //        //}
    //        //StringBuilder sb = new StringBuilder();
    //        //sb.AppendLine("桩型号，位置，直径，长度");
    //        ////选择导出的桩基础模型，如果必须可以限定选择的类别和族 控制其

    //        //List<Reference> rfs = uiDoc.Selection.PickObjects(ObjectType.Element).ToList();
    //        //foreach (var item in rfs)
    //        //{
    //        //    Element ele = doc.GetElement(item);
    //        //    string eleType = ele.LookupParameter("桩型号").AsString();
    //        //    XYZ point = (ele.Location as LocationPoint).Point;
    //        //    string eleDim = ele.LookupParameter("直径").AsValueString();
    //        //    string eleLength = ele.LookupParameter("长度").AsValueString();
    //        //    //坐标信息
    //        //    sb.AppendLine(eleType+","+point.X*s + point.Y*s +","+eleDim+","+eleLength);
    //        //}
    //        ////写入csv文件
    //        //File.WriteAllText(sfDialog.FileName,sb.ToString(),Encoding .UTF8);
    //        //MessageBox.Show("桩基础导出完成");
    //        ////打开文件夹
    //        //System.Diagnostics.Process.Start(Path.GetDirectoryName(sfDialog.FileName));
    //        ////0501例程结束

    //        ////兼容先选择或后选择的写法 例程P66
    //        //List<ElementId> elemIds = uiDoc.Selection.GetElementIds().ToList();
    //        ////过滤已有选集中不符合要求的对象
    //        //for (int i = 0; i < elemIds.Count; i++)
    //        //{
    //        //    ElementId id = elemIds[i];
    //        //    //示例，仅保留墙体
    //        //    if (!(uiDoc.Document.GetElement(id) is Wall))
    //        //        elemIds.Remove(id);
    //        //}
    //        ////如果选集为空，命令选择对象
    //        //if (elemIds.Count == 0)
    //        //{
    //        //    IList<Reference> refers = new List<Reference>();
    //        //    //需要补充WallSelectionFilter 定义，
    //        //    filterWallClass wallFilter = new filterWallClass();
    //        //    try
    //        //    {
    //        //        refers = uiDoc.Selection.PickObjects(ObjectType.Element);
    //        //    }
    //        //    catch
    //        //    {
    //        //        return Result.Succeeded; //中断命令退出是否不应该用success？？
    //        //    }
    //        //    //将用户选择对象加入选集
    //        //    foreach (Reference refer in refers)
    //        //    {
    //        //        //前面如果没有WallSelectionfilter限制，此处还应做一次判断
    //        //        elemIds.Add(refer.ElementId);
    //        //    }
    //        //    //此后执行功能代码
    //        //    return Result.Succeeded;
    //        //}
    //        ////例程结束

    //        //从doc取element并取Id 例程 0512
    //        //var elemList = uiDoc.Selection.GetElementIds().ToList();
    //        //Element selElem = uiDoc.Document.GetElement(elemList[0]);
    //        //ElementType type = doc.GetElement(selElem.GetTypeId()) as ElementType;
    //        //例程结束


    //        //单个构件碰撞检查 例程 0512 应该加上trycatch以及先后选对象判断
    //        //Transaction trans = new Transaction(doc,"碰撞检测");
    //        //trans.Start();  

    //        //Selection select = uiDoc.Selection;
    //        //Reference r = select.PickObject(ObjectType.Element, "选择要检查的墙");
    //        //Element column = doc.GetElement(r);
    //        //FilteredElementCollector collect = new FilteredElementCollector(doc);
    //        ////冲突检查
    //        //ElementIntersectsElementFilter iFilter = new ElementIntersectsElementFilter(column,false);
    //        //collect.WherePasses(iFilter);
    //        //List<ElementId> excludes =new List<ElementId>();
    //        //excludes.Add(column.Id);
    //        //collect.Excluding(excludes);
    //        //List<ElementId> ids = new List<ElementId>();
    //        //select.SetElementIds(ids);

    //        //foreach (Element elem in collect)
    //        //{
    //        //    ids.Add(elem.Id);
    //        //}
    //        //select.SetElementIds(ids);
    //        //trans.Commit();
    //        //例程结束

    //        //TaskDialog例程 0513
    //        //TaskDialog td = new TaskDialog("title");
    //        //td.MainInstruction = "消息标题";  
    //        //td.MainContent = "主要内容";
    //        //td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;//三角黄色标
    //        //td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;//圆圈蓝色标
    //        //td.MainIcon = TaskDialogIcon.TaskDialogIconError;//错误叉红色标
    //        //td.MainIcon = TaskDialogIcon.TaskDialogIconShield;//盾形彩色标
    //        //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "链接1","说明1");
    //        //td.CommonButtons =TaskDialogCommonButtons.Close|TaskDialogCommonButtons.Yes| TaskDialogCommonButtons.No;//怎么关联这个yes
    //        //td.DefaultButton = TaskDialogResult.Close;
    //        //td.ExpandedContent = "扩展内容说明";
    //        //td.FooterText = "脚注" + "<a href=\"http://www.cacc.com\">" + "点击了解更多</a>";
    //        //td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;//设置图标
    //        //td.TitleAutoPrefix = false;//这是什么用处？
    //        //td.VerificationText = "不再显示该消息";
    //        //var result = td.Show();
    //        //bool ischecked = td.WasVerificationChecked();//关联的动作？？            

    //        //if (TaskDialogResult.CommandLink1 == result)
    //        //{
    //        //    TaskDialog dialog_CommandLink1 = new TaskDialog("版本信息");
    //        //    dialog_CommandLink1.MainInstruction = "版本名" + application.VersionName + "\n" + "版本号：" + application.VersionNumber;
    //        //    dialog_CommandLink1.Show();
    //        //}
    //        //else if (true)
    //        //{
    //        //    TaskDialog.Show("exit","退出");
    //        //}
    //        //用taskdia实现分支选择
    //        //       TaskDialogResult result = TaskDialog.Show("Revit",
    //        //"Yes to return succeeded and delete all selection," +
    //        //"No to cancel all commands.",
    //        //TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
    //        //例程结束

    //        //从元素取实体，从体取面并计算其体积和面积 0513
    //        //ICollection<ElementId> ids = uiDoc.Selection.GetElementIds();//是要先选中才能执行
    //        //Element elem = uiDoc.Document.GetElement(ids.First());//多个只计算第一个
    //        //GeometryElement ge = elem.get_Geometry(new Options());
    //        //double area = 0;
    //        //double volume = 0;
    //        //int triangleCount = 0;

    //        //foreach (GeometryObject geometryObject in ge) 
    //        //{
    //        //    if (geometryObject is Solid)
    //        //    {
    //        //        Solid sd = geometryObject as Solid;
    //        //        foreach (Face face in sd.Faces)
    //        //        {
    //        //            area += face.Area.SquareFeetToSquareMeter();
    //        //            Mesh mesh = face.Triangulate(0.5); //0.5是什么意思？
    //        //            triangleCount += mesh.NumTriangles;
    //        //        }
    //        //        volume += sd.Volume * 0.3048 * 0.3048 * 0.3048;
    //        //    } 
    //        //}
    //        //TaskDialog.Show("计算","结果面积总和=" +area.ToString()+"平方米\n"+"体积为"+volume.ToString("0.000")+"立方米\n"+
    //        //    "三角网格数"+triangleCount.ToString());
    //        //例程结束

    //        //计算轴线交点，布置柱子 0513
    //        //FilteredElementCollector coll = new FilteredElementCollector(doc);
    //        //ElementClassFilter gridFilter = new ElementClassFilter(typeof(Grid));
    //        //List<Element> grid = coll.WherePasses(gridFilter).ToElements().ToList();
    //        //List<Line> gridlines = new List<Line>();
    //        //List<XYZ> intPos = new List<XYZ>();

    //        //foreach (Grid grid1 in grid)
    //        //{
    //        //    gridlines.Add(grid1.Curve as Line);
    //        //}
    //        //foreach (Line line1 in gridlines)
    //        //{
    //        //    foreach (Line line2 in gridlines) 
    //        //    {
    //        //        XYZ normal1 = line1.Direction;
    //        //        XYZ normal2 = line2.Direction;
    //        //        if (normal1.IsAlmostEqualTo(normal2)) continue;
    //        //        IntersectionResultArray results;
    //        //        SetComparisonResult intRst = line1.Intersect(line2,out results);
    //        //        if (intRst == SetComparisonResult.Overlap && results.Size == 1)
    //        //        {
    //        //            XYZ tp = results.get_Item(0).XYZPoint;
    //        //            if (intPos.Where(m =>m.IsAlmostEqualTo(tp)).Count()==0)
    //        //            {
    //        //                intPos.Add(tp); 
    //        //            }
    //        //        }
    //        //    }
    //        //}
    //        ////Level level = doc.GetElement(new ElementId(1040365)) as Level;//此处取标高id，应该为默认的当前视图
    //        //Level level = uiDoc.ActiveView.GenLevel as Level;//此处取标高id，应该为默认的当前视图
    //        ////FamilySymbol familySymbol = doc.GetElement(new ElementId(1411287)) as FamilySymbol; //此处id应取选中的柱子类型typeID

    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);//可以加过滤器强制选柱子             
    //        //Element element = doc.GetElement(reference);
    //        //FamilySymbol familySymbol = doc.GetElement(element.GetTypeId()) as FamilySymbol; //此处id应取选中的柱子类型ID 

    //        //using (Transaction trans = new Transaction(doc))
    //        //{ 
    //        //    trans.Start("轴网生柱");
    //        //    if (!familySymbol.IsActive) familySymbol.Activate();
    //        //    foreach (XYZ p in intPos)
    //        //    {
    //        //        FamilyInstance familyInstance = doc.Create.NewFamilyInstance(p, familySymbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
    //        //    }
    //        //    trans.Commit();
    //        //}
    //        //例程结束

    //        //找元素可见性，有问题 尚未实现 0531
    //        //https://blog.csdn.net/weixin_30487201/article/details/95149618
    //        //ElementId elemId = new ElementId(346605);
    //        //FilteredElementCollector fec = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.View));
    //        //string res="";
    //        //foreach (var view in fec.ToElements())
    //        //{
    //        //    if (!isVisible(view.Id,elemId))
    //        //    {
    //        //        continue;
    //        //    }
    //        //    res += view.Name + view.Id.ToString() + "\n";
    //        //}
    //        //TaskDialog.Show("Title", res);
    //        //例程结束

    //        //显示项目基点和测量点 0531 可在此基础上深化通过一个动作来控制两个开关的四种状态（需要保存状态值，不现实）
    //        //https://blog.csdn.net/weixin_42326676/article/details/127516523?utm_medium=distribute.pc_relevant.none-task-blog-2~default~baidujs_baidulandingword~default-5-127516523-blog-95149618.235^v43^pc_blog_bottom_relevance_base2&spm=1001.2101.3001.4242.4&utm_relevant_index=8

    //        //Categories cates = doc.Settings.Categories;
    //        //Category site = cates.get_Item(BuiltInCategory.OST_Site);
    //        //Category basePoint = cates.get_Item(BuiltInCategory.OST_ProjectBasePoint);
    //        //Category sharePoint = cates.get_Item(BuiltInCategory.OST_SharedBasePoint);
    //        //using (Transaction ts = new Transaction(doc, "显示基点"))
    //        //{
    //        //    ts.Start();
    //        //    doc.ActiveView.SetCategoryHidden(site.Id, false);
    //        //    doc.ActiveView.SetCategoryHidden(basePoint.Id, false);
    //        //    doc.ActiveView.SetCategoryHidden(sharePoint.Id, false);
    //        //    ts.Commit();
    //        //}
    //        //例程结束

    //        //关闭显示项目基点和测量点 0531 
    //        //Categories cates = doc.Settings.Categories;
    //        //Category site = cates.get_Item(BuiltInCategory.OST_Site);
    //        //Category basePoint = cates.get_Item(BuiltInCategory.OST_ProjectBasePoint);
    //        //Category sharePoint = cates.get_Item(BuiltInCategory.OST_SharedBasePoint);
    //        //using (Transaction ts = new Transaction(doc, "显示基点"))
    //        //{
    //        //    ts.Start();
    //        //    doc.ActiveView.SetCategoryHidden(site.Id, false);
    //        //    doc.ActiveView.SetCategoryHidden(basePoint.Id, true);
    //        //    doc.ActiveView.SetCategoryHidden(sharePoint.Id, true);
    //        //    ts.Commit();
    //        //}
    //        //例程结束

    //        //显示各种保温层 0601 
    //        Categories cates = doc.Settings.Categories;
    //        Category pipeInsu = cates.get_Item(BuiltInCategory.OST_PipeInsulations);
    //        Category pipeFitInsu = cates.get_Item(BuiltInCategory.OST_PipeFittingInsulation);
    //        Category ductInsu = cates.get_Item(BuiltInCategory.OST_DuctInsulations);
    //        Category ductFitInsu = cates.get_Item(BuiltInCategory.OST_DuctFittingInsulation);
    //        using (Transaction ts = new Transaction(doc, "显示保温"))
    //        {
    //            ts.Start();
    //            if (activeView.GetCategoryHidden(pipeInsu.Id))
    //            {
    //                doc.ActiveView.SetCategoryHidden(pipeInsu.Id, false);
    //                doc.ActiveView.SetCategoryHidden(ductInsu.Id, false);
    //            }
    //            else
    //            {
    //                doc.ActiveView.SetCategoryHidden(pipeInsu.Id, true);
    //                doc.ActiveView.SetCategoryHidden(ductInsu.Id, true);
    //            }
    //            ts.Commit();
    //        }
    //        doc.ActiveView.SetCategoryHidden(pipeInsu.Id, false);
    //        doc.ActiveView.SetCategoryHidden(pipeFitInsu.Id, false);
    //        doc.ActiveView.SetCategoryHidden(ductInsu.Id, false);
    //        doc.ActiveView.SetCategoryHidden(ductFitInsu.Id, false);

    //        //例程结束

    //        //找视图内同类机电构件 0601 似乎没什么价值
    //        //ElementId elementId = uiDoc.Selection.GetElementIds().FirstOrDefault();
    //        //ElementId viewId = doc.ActiveView.Id;
    //        //try
    //        //{
    //        //    if (elementId == null)
    //        //    {
    //        //        Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //        elementId = doc.GetElement(reference).Id;
    //        //    }
    //        //    Element element = doc.GetElement(elementId);
    //        //    FilteredElementCollector collector = new FilteredElementCollector(doc, viewId);
    //        //    IList<ElementId> ids = collector.OfCategoryId(element.Category.Id).OfClass(typeof(MEPCurve)).ToElementIds().ToList();//只对一种Class过滤

    //        //    uiDoc.Selection.SetElementIds(ids);
    //        //    TaskDialog.Show("element", "找到相同类型构件" + ids.Count() + "个");
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}
    //        //例程结束

    //        //点选看标高，有问题 尚未实现
    //        //try
    //        //{
    //        //    var elementId = uiDoc.Selection.GetElementIds().FirstOrDefault();
    //        //    Element elem = doc.GetElement(elementId);
    //        //    if (elementId == null || elem.type !=MEPCurve)
    //        //    {
    //        //        Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //        elementId = doc.GetElement(reference).Id;
    //        //    }

    //        //    TaskDialog.Show("Main Windows", elem.Location.ToString().Trim());

    //        //    //Reference reference1 = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter());
    //        //    //XYZ xyz1 = reference1.GlobalPoint;
    //        //    //MEPCurve mEPCurve1 = doc.GetElement(reference1) as MEPCurve;
    //        //    //Curve curve = (mEPCurve1.Location as LocationCurve).Curve;
    //        //    //xyz1 = curve.Project(xyz1).XYZPoint;
    //        //    //int xyzz = Convert.ToInt16(xyz1.Z.ToMillimeter());
    //        //    ////TaskDialog.Show("Main Windows", xyzz.ToString());
    //        //    //int xyzzz = Convert.ToInt16(mEPCurve1.LevelOffset.ToMillimeter());
    //        //    //TaskDialog.Show("Main Windows", "偏移值=" + Convert.ToString(xyzzz) + "\n" + "Z轴高程=" + xyzz.ToString());
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}

    //        //点选连接两根管道 0601

    //        //测试例程，找出管上点到两端Connector的较近值
    //        //Reference reference1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe());
    //        //Pipe pipe1 = doc.GetElement(reference1) as Pipe;
    //        //XYZ xyz1 = reference1.GlobalPoint;
    //        //Curve curve = (pipe1.Location as LocationCurve).Curve;
    //        //xyz1 = curve.Project(xyz1).XYZPoint;

    //        ////ConnectorManager connectorManager = pipe1.ConnectorManager;
    //        //ConnectorSetIterator csi = pipe1.ConnectorManager.Connectors.ForwardIterator();

    //        //double d;
    //        //double dd;
    //        //IList<XYZ> xyzList = new List<XYZ>();
    //        //List<double> dis =new List<double>();

    //        //while (csi.MoveNext())
    //        //{
    //        //    Connector conn = csi.Current as Connector;                
    //        //    xyzList.Add(conn.Origin);
    //        //}
    //        //foreach (var item in xyzList)
    //        //{
    //        //    dd= GetPointDistance(item, xyz1);
    //        //    dis.Add(dd);
    //        //}
    //        //for (int i = 0; i < dis.Count; i++)
    //        //{
    //        //    for (int j = i+1; j < dis.Count; j++)
    //        //    {
    //        //        double temp;
    //        //        if (dis[i] > dis[j])
    //        //        {
    //        //            temp = dis[i];
    //        //            dis[i] = dis[j];
    //        //            dis[j] = temp;
    //        //        }
    //        //    }
    //        //} 
    //        //TaskDialog.Show("Title", Convert.ToString(dis[0].ToMillimeter())); //测量值似乎不正确，是不是因为点不在中线上？
    //        //                                                                   //例程结束

    //        //找出距离点选更近的连接器 0601
    //        //try
    //        //{

    //        //    Reference reference1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe());
    //        //    Pipe pipe1 = doc.GetElement(reference1) as Pipe;
    //        //    XYZ xyz1 = reference1.GlobalPoint;
    //        //    Curve curve1 = (pipe1.Location as LocationCurve).Curve;
    //        //    xyz1 = curve1.Project(xyz1).XYZPoint;
    //        //    //获得管1的连接器
    //        //    List<Connector> conns1 = GetConnectors(pipe1);

    //        //    Reference reference2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe());
    //        //    Pipe pipe2 = doc.GetElement(reference2) as Pipe;
    //        //    XYZ xyz2 = reference2.GlobalPoint;
    //        //    Curve curve2 = (pipe2.Location as LocationCurve).Curve;
    //        //    xyz2 = curve2.Project(xyz2).XYZPoint;
    //        //    //获得管2的连接器
    //        //    List<Connector> conns2 = GetConnectors(pipe2);
    //        //    if (xyz1.Z != xyz2.Z)
    //        //    {
    //        //        TaskDialog.Show("说明", "命令不支持非同一高度管道连接，请手工连接");
    //        //        return Result.Cancelled;
    //        //    }

    //        //    using (Transaction ts = new Transaction(doc, "两管连接"))
    //        //    {
    //        //        ts.Start();
    //        //        List<Connector> conn = new List<Connector>();

    //        //        //找出距离较近的连接器
    //        //        foreach (Connector connector1 in conns1)
    //        //        {
    //        //            if (GetPointDistance(connector1.Origin, xyz1) < (curve1.Length / 2) && !connector1.IsConnected)
    //        //                //if (connector1.Origin.DistanceTo(xyz1) < (curve1.Length / 2) && !connector1.IsConnected)
    //        //                conn.Add(connector1);
    //        //            //TaskDialog.Show("说明", conn.FirstOrDefault().Origin.X.ToString());
    //        //        }
    //        //        foreach (Connector connector2 in conns2)
    //        //        {
    //        //            if (connector2.Origin.DistanceTo(xyz2) < (curve2.Length / 2) && !connector2.IsConnected)
    //        //                conn.Add(connector2);
    //        //        }
    //        //        ConnectTwoConns(conn);
    //        //        ts.Commit();
    //        //    }
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}

    //        //foreach (Connector connector in connectorSet)             
    //        //{
    //        //    dd = GetPointDistance(connector.Origin, xyz1);
    //        //    dis.Add(dd);
    //        //}

    //        //XYZ xyz1 = pipe1.ConnectorManager.Connectors
    //        //例程结束

    //        return Result.Succeeded;
    //    }

    //    ////两根管道相连例程 P130
    //    ////三种情况，共线且直径相等，共线直径不等，不共线
    //    public static FamilyInstance ConnectTwoConns(List<Connector> conns)
    //    {
    //        FamilyInstance pipeFitting = null;
    //        Document doc = conns[0].Owner.Document;
    //        ElementId levelId = doc.ActiveView.GenLevel.Id;
    //        ElementId pipeTypeId = conns[0].Owner.GetTypeId();

    //        XYZ dir0 = (((conns[0].Owner as Pipe).Location as LocationCurve).Curve as Line).Direction;
    //        XYZ dir1 = (((conns[1].Owner as Pipe).Location as LocationCurve).Curve as Line).Direction;
    //        //XYZ xYZ1 = conns[1].Origin;

    //        if (dir0.CrossProduct(dir1).GetLength() < 0.001) //检测是否共线
    //        {
    //            Double dia1 = (conns[0].Owner as Pipe).get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //            Double dia2 = (conns[1].Owner as Pipe).get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
    //            Pipe bigPipe = null;
    //            if (dia1 > dia2)
    //            {
    //                bigPipe = conns[0].Owner as Pipe;
    //            }
    //            else if (dia1 < dia2)
    //            {
    //                bigPipe = conns[1].Owner as Pipe;
    //            }
    //            else
    //            {
    //                //能否直接移动连接件让两管自动合并？可行但是中线无法自动消除，GLS是怎么弄得
    //                conns[0].ConnectTo(conns[1]);
    //                //看来只能再生成一根管连接两个连接器，也没法合并两根管？？
    //                //Pipe pipe = Pipe.Create(doc, pipeTypeId, levelId, conns[0], conns[1].Origin);
    //                //ConnectorSet connectorSet = pipe.ConnectorManager.Connectors;
    //                //List<Connector> connectorList = new List<Connector>();
    //                //foreach (Connector connector in connectorSet)
    //                //{
    //                //    connectorList.Add(connector);
    //                //}
    //                //conns[0].ConnectTo(connectorList.FirstOrDefault());
    //                //ElementTransformUtils.MoveElement(doc, conns[0].ConnectTo, conns[1].Origin);
    //                //pipeFitting = doc.Create.NewUnionFitting(conns[0], conns[1]);//会生成连接件
    //                return pipeFitting; // 直径相等时直接连接
    //            }
    //            //处理直径不同情况，改使用NewTRansitFitting方法
    //            try
    //            {
    //                pipeFitting = doc.Create.NewTransitionFitting(conns[0], conns[1]);
    //                XYZ mid = (bigPipe.Location as LocationCurve).Curve.Evaluate(0.5, true);
    //                XYZ dir = (conns[0].Origin - mid).Normalize();
    //                //断点向小管道方向移动
    //                ElementTransformUtils.MoveElement(doc, pipeFitting.Id, dir * 50.Tofoot());
    //            }
    //            catch (Exception) { }
    //        }
    //        else //不共线管道直接生成弯头连接件
    //        {
    //            pipeFitting = doc.Create.NewElbowFitting(conns[0], conns[1]);
    //        }
    //        return pipeFitting;
    //    }
    //    //例程结束

    //    //过滤视图可见性 0531 似乎没有作用用
    //    //public bool isVisible(ElementId viewId, ElementId instId)
    //    //{
    //    //    try
    //    //    {
    //    //        FilteredElementCollector fec = new FilteredElementCollector(doc, viewId).OfClass(typeof(FamilyInstance));
    //    //        foreach (FamilyInstance fi in fec.ToElements())
    //    //        {
    //    //            if (fi.Id == instId)
    //    //            {
    //    //                return true;
    //    //            }
    //    //        }
    //    //    }
    //    //    catch (Exception e)
    //    //    {
    //    //    }
    //    //    return false;
    //    //}
    //    //例程结束

    //    ////限定按钮在指定视图范围内可用 例程 P257
    //    //public class PlanViewRestrcter : IExternalCommandAvailability
    //    //{
    //    //    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    //    //    {
    //    //        UIDocument uiDoc = applicationData.ActiveUIDocument;
    //    //        if (uiDoc.ActiveGraphicalView is ViewPlan)
    //    //        { 
    //    //            return true;
    //    //        }                
    //    //        return false;
    //    //    }
    //    //}
    //    ////使用示例，设置按钮的AvailabilityClassName  当不在平面视图时，按钮会变灰
    //    ////PushButtonData1.AvailabilityClassName = "GeeratePipeFromCAD.PlanViewRestrcter";         
    //    ////例程结束

    //    //取当前标高 例程 0512
    //    //private Level GetLevel(Document doc)
    //    //{
    //    //    FilteredElementCollector lvlFilter = new FilteredElementCollector(doc);
    //    //    lvlFilter.OfClass(typeof(Level));
    //    //    return null;
    //    //}
    //    //例程结束

    //    //点间距计算 0601
    //    private double GetPointDistance(XYZ xYZ1, XYZ xYZ2)
    //    {
    //        double pointDistance = Math.Sqrt((xYZ2.X - xYZ1.X) * (xYZ2.X - xYZ1.X) + (xYZ2.Y - xYZ1.Y) * (xYZ2.Y - xYZ1.Y) + (xYZ2.Z - xYZ1.Z) * (xYZ2.Z - xYZ1.Z));
    //        return pointDistance;
    //    }


    //    //例程结束

    //    //取MEP连接器 例程 0512
    //    //private Connector GetConnector(Pipe pipe, XYZ xYZ)
    //    //{
    //    //    foreach (Connector ct in pipe.ConnectorManager.Connectors)
    //    //    {
    //    //        if (ct.Origin.IsAlmostEqualTo(xYZ))
    //    //            return ct;
    //    //    }
    //    //    return null;
    //    //}

    //    private List<Connector> GetConnectors(Pipe pipe)
    //    {
    //        ConnectorSet connectorSet = pipe.ConnectorManager.Connectors;
    //        List<Connector> connectorList = new List<Connector>();
    //        foreach (Connector connector in connectorSet)
    //        {
    //            connectorList.Add(connector);
    //        }
    //        return connectorList;
    //    }
    //    //private Connector GetConnector(MEPModel mEPModel)
    //    //{
    //    //    foreach (Connector ct in mEPModel.ConnectorManager.Connectors)
    //    //    {
    //    //        if (ct.Origin.IsAlmostEqualTo(XYZ.Zero))
    //    //            return ct;
    //    //    }
    //    //    return null;
    //    //}
    //    //例程结束

    //    //修改对象颜色
    //    //[TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    //    //public class ChangeColor : IExternalCommand
    //    //{
    //    //    public Result Execute(ExternalCommandData commandData, ref string messages, ElementSet elements)
    //    //    {
    //    //        ChangeElementColor(commandData, elements);
    //    //        return Result.Succeeded;
    //    //    }
    //    //    public void ChangeElementColor(ExternalCommandData commandData, ElementSet elements)
    //    //    {
    //    //        UIApplication app = commandData.Application;
    //    //        Document doc = app.ActiveUIDocument.Document;
    //    //        ElementId el = new ElementId(729401);
    //    //        Transaction trans = new Transaction(doc);
    //    //        trans.Start("ChangeColor");
    //    //        Color color = new Color((byte)255, (byte)0, (byte)0);
    //    //        OverrideGraphicSettings ogs = new OverrideGraphicSettings();
    //    //        //设置ElementId为729401的Element的颜色
    //    //        ogs.SetProjectionLineColor(color);//投影表面线的颜色
    //    //        ogs.SetCutFillColor(color);//切割面填充颜色
    //    //        Autodesk.Revit.DB.View view = doc.ActiveView;
    //    //        view.SetElementOverrides(el, ogs);
    //    //        trans.Commit();
    //    //    }
    //    //}
    //    //例程结束
    //}
}
