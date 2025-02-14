namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.UsingCommandData)]
    //public class MyClass : IExternalCommand
    //{
    //public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements) 
    //{
    //    UIDocument uidoc = commandData.Application.ActiveUIDocument;
    //    Document doc = uidoc.Document;
    //    Selection sel = uidoc.Selection;
    //    View activeView = uidoc.ActiveView;
    //    UIApplication uIApp = commandData.Application;
    //    Application application = uIApp.Application;

    #region 事务测试
    //using (Transaction ts = new Transaction(doc))
    //{
    //    ts.Start("测试");
    //    ts.Commit();
    //    return Result.Succeeded;
    //}
    //using (Transaction ts = new Transaction(doc))
    //{
    //    ts.Start("测试");
    //    PipeCreator pipeCreator = new PipeCreator(uidoc);
    //    pipeCreator.GetResult();
    //    ts.Commit();
    //    return Result.Succeeded;
    //}
    #endregion

    #region 选择和过滤
    //0327 通过id选取构建
    //Wall wall = doc.GetElement(new ElementId(332135)) as Wall;
    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332135)) as FamilyInstance;
    //TaskDialog.Show("element",wall.Name);
    //通过过滤器
    //FamilyInstance familyInstance = new FilteredElementCollector(doc); 
    //TaskDialog.Show("element",familyInstance.Name);
    //通过点选
    //Reference reference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //Element element = doc.GetElement(reference);
    //TaskDialog.Show("element", element.Name);

    //可载入族的读取 族实例——族类型Symbol——族
    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332135)) as FamilyInstance;
    //FamilySymbol familySymbol = familyInstance.Symbol;
    //Family family = familySymbol.Family;
    //反向可以找GetFamilySymbolIds ,族可以有多类型，所以设置返回值为集合 <>
    //ISet<ElementId> ids = family.GetFamilySymbolIds();
    //foreach (ElementId elementId in ids )
    //{
    //    FamilySymbol familySymbol1 = doc.GetElement(elementId) as FamilySymbol;
    //    //通过过滤器获取。取得symbol可以根据这个找所有同类实例
    //}

    //内置族类型先实例化再通过type找
    //Wall wall = doc.GetElement(uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element)) as Wall;
    //WallType wallType = wall.WallType;
    //TaskDialog.Show("wallType",wallType.Name+"+"+wallType.FamilyName);

    //BasePoint basePoint = null;
    //获取点
    //FamilyInstance familyInstance = doc.GetElement(uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element)) as FamilyInstance;
    //XYZ xyz = (familyInstance.Location as LocationPoint).Point;
    //获取墙
    //Wall wall = doc.GetElement(new ElementId(332135)) as Wall;
    //取得墙线
    //Curve curve= (wall.Location as LocationCurve).Curve; //为啥包裹起来再.curve
    //Line wallLine = curve as Line;
    //XYZ startXYZ = curve.GetEndPoint(0);
    //XYZ endXYZ = curve.GetEndPoint(1);

    //0328 收集器FilteredElementCollector
    //简单全过滤
    //FilteredElementCollector collector1 = new FilteredElementCollector(doc);


    //演示案例1,取模型中id相关所有族类型和实例。包括不在视图中的以及重复的
    //IList<Element> elements1 = collector1.OfCategoryId(new ElementId(-2000023)).ToElements();
    //可替换为按ofcategory,按内部类别同时获取族类型和实例
    //IList<Element> elements1 = collector1.OfCategory(BuiltInCategory.OST_Doors).ToElements();
    //StringBuilder stringBuilder1 = new StringBuilder();
    //foreach (Element item in elements1)
    //{
    //    if (item is FamilySymbol)
    //    {
    //        stringBuilder1.AppendLine("族类型\t\t"+item.Name);
    //    }
    //    else if (item is FamilyInstance)
    //    {
    //        stringBuilder1.AppendLine("族实例\t\t" + item.Name+""+item.Id);
    //    }
    //}
    //TaskDialog.Show("title", stringBuilder1.ToString());

    //通过类获取doc所有族实例familyinstance
    //FilteredElementCollector collector1 = new FilteredElementCollector(doc);
    //IList<Element> elements2 = collector1.OfClass(typeof(FamilyInstance)).ToElements();
    //StringBuilder stringBuilder2 = new StringBuilder();
    //foreach (Element item in elements2)
    //{
    //    if (item is FamilySymbol)
    //    {
    //        stringBuilder2.AppendLine("族类型\t\t" + item.Name);
    //    }
    //    else if (item is FamilyInstance)
    //    {
    //        stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId);
    //    }
    //}
    //TaskDialog.Show("title", stringBuilder2.ToString());

    //只获取所有门类型 族实例，多重筛选，运行失败因为搞错了class
    //FilteredElementCollector collector1 = new FilteredElementCollector(doc);
    //IList<Element> elements2 = collector1.OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).ToElements();
    //StringBuilder stringBuilder2 = new StringBuilder();
    //foreach (Element item in elements2)
    //{
    //    //Level level =doc.GetElement(item.LevelId) as Level;
    //    //if (item is FamilySymbol)//筛选条件可以多样
    //    //{
    //    //    stringBuilder2.AppendLine("族类型\t\t" + item.Name);
    //    //}
    //    //else if (item is FamilyInstance)
    //    //{
    //    //    stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId+"标高名"+level.Name);
    //    //}

    //    Level level = doc.GetElement(item.LevelId) as Level;
    //    FamilyInstance instance =  item as FamilyInstance;
    //    string symbolName = instance.Symbol.Name;
    //    if (level.Name.Equals("标高 1") && "750 x 2000mm".Equals(symbolName))
    //    {
    //        stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId + "标高名" + level.Name);
    //    }

    //}
    //TaskDialog.Show("title", stringBuilder2.ToString());

    //0329
    //按id过滤


    //用pick多选门（例） 
    //StringBuilder stringBuilder2 = new StringBuilder();

    //IList<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //List<ElementId> ids = new List<ElementId>();

    //foreach (Reference reference in references)
    //{
    //    ids.Add(reference.ElementId);
    //}   

    //FilteredElementCollector collector2 = new FilteredElementCollector(doc, ids);
    //IList<Element> elements4 = collector2.OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).ToElements();
    //foreach (Element item in elements4)
    //{
    //    stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId);
    //}
    //TaskDialog.Show("title", stringBuilder2.ToString());

    ////按视图过滤
    //ElementId viewId = new ElementId(312); //已知视图id
    //ElementId viewId = doc.ActiveView.Id;
    //FilteredElementCollector collector3 = new FilteredElementCollector(doc, viewId);

    //StringBuilder stringBuilder2 = new StringBuilder();
    //IList<Element> elements3 = collector3.OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).ToElements();
    //List<ElementId> ids1 = new List<ElementId>(); //保存收集到的elementId，//命令结束后仍 高亮选择多构件前提
    //foreach (Element item in elements3)
    //{
    //    stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId);
    //    ids1.Add(item.Id);//命令结束后仍 高亮选择多构件,需保存搜索结果
    //}
    //TaskDialog.Show("title", stringBuilder2.ToString());
    //uidoc.Selection.SetElementIds(ids1); //命令结束后仍 高亮选择多构件

    //0330 获取当前视图内房间，平立剖面都能看
    //StringBuilder stringBuilder2 = new StringBuilder();
    //ElementId viewId = doc.ActiveView.Id;
    //FilteredElementCollector collector3 = new FilteredElementCollector(doc, viewId);
    //RoomFilter roomFilter = new RoomFilter();
    //IList<Room> rooms= collector3.WherePasses(roomFilter).Cast<Room>().ToList(); //返回房间清单，引用linq 注意room类型不能用ofClass
    ////IList<Room> rooms = collector3.WherePasses(roomFilter).Cast<Room>().Where(x=>x.location !=null).ToList(); //增加视图内房间是否存在判断
    //foreach (Room item in rooms)
    //{
    //    //也可以在循环体内筛选符合视图要求的 if (item.level.Name.Equals("标高 2") && item.location !=NULL )
    //    stringBuilder2.AppendLine("族实例\t\t" + item.Name + "" + item.Id + "标高" + item.LevelId);
    //    //不用再从ElementId导回item清单，可以直接用rooms的属性
    //}
    //TaskDialog.Show("title", stringBuilder2.ToString());
    //得到房间边界框相交叉的构件统计,为啥运行不成功（因为混淆了房间和房间名称的ID）
    //StringBuilder stringBuilder2 = new StringBuilder();
    //Room room =doc.GetElement(new ElementId (333036)) as Room;
    //BoundingBoxXYZ boundingBoxXYZ =room.get_BoundingBox(doc.ActiveView);//找到房间边框
    //Outline outline = new Outline(boundingBoxXYZ.Min,boundingBoxXYZ.Max ); //找到边界框
    //BoundingBoxIntersectsFilter boundingBoxIntersectsFilter = new BoundingBoxIntersectsFilter(outline);//生成一个元素过滤器
    //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //IList<Element> elements2 = collector.WherePasses(boundingBoxIntersectsFilter).ToElements();
    //foreach (Element item in elements2)
    //{
    //    if (item.Category.Id.IntegerValue == -2000080) //二次过滤，只找家具 ，来自家具Category的ID 也可以在whereas后面加ofCategory（）
    //    {
    //        stringBuilder2.AppendLine(item.Name);
    //    }

    //}

    //TaskDialog.Show("title", stringBuilder2.ToString());


    #endregion

    #region 移动和旋转
    //0331 
    //XYZ translation = XYZ.BasisX * 1000 / 304.8; //简易转换mm
    //XYZ translation2 = XYZ.BasisX * Unitconvert.Tofoot(1000);
    //XYZ translation3 = XYZ.BasisX.Negate() * Unitconvert.Tofoot(1000);
    //移动选定单个元素
    //ElementId elemId = uidoc.Selection.PickObject(ObjectType.Element).ElementId;
    //using (Transaction ts = new Transaction(doc,"移动1"))
    //{ 
    //    ts.Start();
    //    ElementTransformUtils.MoveElement(doc,elemId,translation3); //移动选定单个元素
    //    //ElementTransformUtils.MoveElement(doc,new ElementId(332350),translation3);
    //    ts.Commit();    
    //}

    //移动选定多个元素
    //XYZ translation3 = XYZ.BasisX.Negate() * Unitconvert.Tofoot(1000);
    //IList<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //List<ElementId> ids = new List<ElementId>();
    //using (Transaction ts = new Transaction(doc, "移动2"))
    //{
    //    ts.Start();
    //    foreach (Reference reference in references)
    //    {
    //        ids.Add(reference.ElementId);
    //    }
    //    ElementTransformUtils.MoveElements(doc, ids, translation3); //移动选定多个元素，注意moveelemnt要加s
    //    //ElementTransformUtils.MoveElement(doc,new ElementId(332350),translation3);
    //    ts.Commit();
    //}

    //用location.move移动单个元素，如移动多个得参考上个方法遍历对象
    //XYZ translation3 = XYZ.BasisX.Negate() * Unitconvert.Tofoot(1000);
    //IList<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //List<ElementId> ids = new List<ElementId>();
    //using (Transaction ts = new Transaction(doc, "移动3"))
    //{
    //    ts.Start();
    //    foreach (Reference reference in references)
    //    {
    //        ids.Add(reference.ElementId);
    //    }
    //    foreach (ElementId id in ids)
    //    {
    //        FamilyInstance familyInstance = doc.GetElement(id) as FamilyInstance;
    //        bool result = familyInstance.Location.Move(translation3); //可有返回值是否成功
    //    }
    //    ElementTransformUtils.MoveElements(doc, ids, translation3); //移动选定多个元素，注意moveelemnt要加s
    //    //ElementTransformUtils.MoveElement(doc,new ElementId(332350),translation3);
    //    ts.Commit();
    //}

    //旋转元素
    //转墙，应该是只能沿Z轴转
    //Wall wall =doc.GetElement(new ElementId(332776)) as Wall;   
    //Curve wallLine = (wall.Location as LocationCurve).Curve; //从墙取定位线
    ////XYZ xYZ1 = wallLine.GetEndPoint(0); //取线端点作为旋转中心
    //XYZ xYZ1 = (wallLine.GetEndPoint(0)+ wallLine.GetEndPoint(1))/2; //取线中点作为旋转中心
    //Line axis = Line.CreateBound(xYZ1, new XYZ(xYZ1.X, xYZ1.Y, xYZ1.Z + 1)); //建立向z轴的垂线作为旋转轴,Z+ 逆时针。Z-逆时针

    //using (Transaction ts = new Transaction(doc, "旋转1"))
    //{
    //    ts.Start();
    //    ElementTransformUtils.RotateElement(doc, new ElementId(332776), axis, 90.0.AngleToRadian());
    //    //等同以下写法,方法有this简化
    //    //ElementTransformUtils.RotateElement(doc, new ElementId(332776), axis, Unitconvert.AngleToRadian(90.0));
    //    ts.Commit();
    //}

    //转普通构件
    //ElementId id = new ElementId(333049);
    //FamilyInstance familyInstance = doc.GetElement(id) as FamilyInstance;
    //XYZ xYZ2 = (familyInstance.Location as LocationPoint).Point; //无需取线，直接用定位点作轴
    //Line axis2 = Line.CreateBound(xYZ2, new XYZ(xYZ2.X, xYZ2.Y, xYZ2.Z + 1));
    //using (Transaction ts = new Transaction(doc, "旋转2"))
    //{
    //    ts.Start();
    //    ElementTransformUtils.RotateElement(doc, id, axis2, 90.0.AngleToRadian());
    //    ts.Commit();
    //}

    //转普通构件多个
    //XYZ xYZ2 = uidoc.Selection.PickPoint(); //直接点选旋转中心点
    //Line axis2 = Line.CreateBound(xYZ2, new XYZ(xYZ2.X, xYZ2.Y, xYZ2.Z + 1));
    //using (Transaction ts = new Transaction(doc, "旋转3"))
    //{
    //    ts.Start();
    //    List<ElementId> ids = new List<ElementId>() { new ElementId(333445), new ElementId(333480)};
    //    ElementTransformUtils.RotateElements(doc, ids, axis2, 90.0.AngleToRadian());
    //    ts.Commit();
    //}

    //用location rotate方式转
    //TaskDialog.Show("报启动", "命令说明");
    //ElementId id = new ElementId(333049);
    //try
    //{
    //    FamilyInstance familyInstance = doc.GetElement(id) as FamilyInstance;
    //    XYZ xYZ2 = uidoc.Selection.PickPoint(); //直接取旋转点
    //    Line axis2 = Line.CreateBound(xYZ2, new XYZ(xYZ2.X, xYZ2.Y, xYZ2.Z + 1));
    //    using (Transaction ts = new Transaction(doc, "旋转4"))
    //    {
    //        ts.Start();
    //        bool result = familyInstance.Location.Rotate(axis2, 90.0.AngleToRadian());
    //        ts.Commit();
    //    }
    //}
    //catch (Autodesk.Revit.Exceptions.OperationCanceledException e)
    //{
    //    TaskDialog.Show("报错", "退出命令");
    //}

    // 点计算作为移动向量
    //XYZ xYZ2 = uidoc.Selection.PickPoint();
    ////XYZ xYZ2 = XYZ.Zero;  //终点设为原点就让点归0
    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332112)) as FamilyInstance;
    //using (Transaction ts = new Transaction(doc, "移动1"))
    //{
    //    ts.Start();
    //    XYZ xYZ3 = (familyInstance.Location as LocationPoint).Point ;
    //    //XYZ translation = xYZ2 - xYZ3; //等同与substract方法
    //    XYZ translation = xYZ2.Subtract( xYZ3);
    //    ElementTransformUtils.MoveElement(doc, familyInstance.Id, translation);
    //    ts.Commit();
    //}


    #endregion

    #region 构件属性修改和删除
    //获取构件的属性并修改内置参数
    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332119)) as FamilyInstance;
    //Parameter parameter = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
    //using (Transaction ts = new Transaction(doc,"修改1"))
    //{
    //    ts.Start();
    //    if (parameter.StorageType == StorageType.Double)
    //    {
    //        //parameter.Set(Unitconvert.Tofoot(100));
    //        parameter.Set(1000.0.Tofoot());
    //    }
    //    ts.Commit();
    //}

    //获取构件的属性并修改自定义参数
    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332119)) as FamilyInstance;
    //using (Transaction ts = new Transaction(doc, "修改2"))
    //{
    //    ts.Start();
    //    //获取自定义参数
    //    Parameter parameter = familyInstance.LookupParameter("备注");
    //    parameter.Set("bbb");
    //    ts.Commit();
    //}

    //0331 通过更改elementID 编辑柱子族类型  错误原因 柱子过滤类型选错了OST_StructuralColumns

    //FamilyInstance familyInstance = doc.GetElement(new ElementId(332119)) as FamilyInstance;
    //using (Transaction ts = new Transaction(doc, "修改3"))
    //{
    //    ts.Start();
    //    //修改柱子类型，先找出实例对应的类型symbol
    //    FilteredElementCollector elements1 = new FilteredElementCollector(doc);
    //    List<FamilySymbol> familySymbols = elements1.OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().ToList();
    //    //以上简写,将id对应所有类型直接成组
    //    foreach (FamilySymbol symbol in familySymbols)
    //    {
    //        if ("11".Equals(symbol.Name))//修改的目标 类型名称
    //        {
    //            familyInstance.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).Set(symbol.Id);
    //            break; //找到符合条件，修改后即退出
    //        }
    //    }
    //    ts.Commit();
    //}

    //删除元素  
    //using (Transaction ts = new Transaction(doc, "删除1"))
    //{
    //    ts.Start();
    //    //得到引用集合
    //    IList<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //    //把集合ref转取ElemntID,foreach遍历生成ids
    //    List<ElementId> ids = new List<ElementId>();
    //    foreach (Reference reference in references)
    //    {
    //        ids.Add(reference.ElementId);
    //    }
    //    //doc.Delete(ids.FirstOrDefault());//删1个
    //    doc.Delete(ids);
    //    ts.Commit();
    //}


    #endregion

    //使用房间的过滤类示例
    //Reference reference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterRoomClass());

    #region 标高和轴网
    //新建标高和视图，自动命名
    //using (Transaction ts = new Transaction(doc, "标高1"))
    //{
    //    ts.Start();
    //    Level level = Level.Create(doc, 8000.Tofoot()); //高度相对于项目基点
    //    //获取楼层平面类型
    //    FilteredElementCollector elements1 = new FilteredElementCollector(doc);
    //    var elemnts = elements1.OfClass(typeof(ViewFamilyType)).ToElements();
    //    ViewFamilyType viewFamilyType = null;
    //    foreach (Element item in elemnts)
    //    {
    //        viewFamilyType = item as ViewFamilyType;
    //        if (viewFamilyType.ViewFamily == ViewFamily.FloorPlan)
    //        {
    //            break;
    //        }
    //    }
    //    //创建平面视图
    //    ViewPlan.Create(doc, viewFamilyType.Id, level.Id);
    //    TaskDialog.Show("高度", level.Elevation.ToMillimeter() + "");
    //    ts.Commit();
    //    //找临近视图，可以为空
    //    ElementId viewId = level.FindAssociatedPlanViewId(); 
    //    View view = doc.GetElement(viewId) as View;
    //    TaskDialog.Show("临近视图", view.Name + "");
    //}

    //使用外部类中的方法建立轴网没成功，尝试改直接在事务中创建有问题，没有提示，立面报错，退出报错，不能保持正交方向
    //为什么无法在方法事务中引用其他类的方法名称？
    //using (Transaction ts = new Transaction(doc, "直线轴网1"))
    //{
    //    ts.Start();
    //    //CreateGrid();
    //    XYZ xYZ1 = uidoc.Selection.PickPoint();
    //    XYZ xYZ2 = uidoc.Selection.PickPoint();
    //    Line line = Line.CreateBound(xYZ1, xYZ2);
    //    Grid grid = Grid.Create(doc, line);
    //    ts.Commit();
    //}
    //using (Transaction ts = new Transaction(doc, "弧线轴网1"))
    //{
    //    ts.Start();
    //    //CreateGrid();
    //    XYZ xYZ1 = uidoc.Selection.PickPoint();
    //    XYZ xYZ2 = uidoc.Selection.PickPoint();
    //    XYZ xYZ3 = uidoc.Selection.PickPoint();

    //    Arc arc = Arc.Create(xYZ1, xYZ2, xYZ3);
    //    Grid grid = Grid.Create(doc, arc);
    //    ts.Commit();
    //}
    #endregion

    #region 族操作

    //    using (Transaction ts = new Transaction(doc, "载入族1"))
    //    {
    //        ts.Start();

    //        ts.Commit();
    //    }


    //    // "D:\\j1.rfa"

    #endregion

    //    return Result.Succeeded;
    //}
    //}
}
