namespace CommonClass
{
    //[Transaction(TransactionMode.Manual)]
    //public class CommonClass : IExternalCommand
    //{
    //    UIDocument uiDoc = null;
    //    Document doc =null;
    //    Application application = null; 
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uiApp = commandData.Application;
    //        application = uiApp.Application;
    //        uiDoc = uiApp.ActiveUIDocument;
    //        doc = uiDoc.Document; //用全局定义，不要重复赋值

    //        //using (Transaction ts = new Transaction(doc,"载入族"))
    //        //{
    //        //    ts.Start();
    //        //    Family family = LoadFamily(@"D:\j1.rfa"); //转义字符报错前面要加@或者双写\
    //        //    TaskDialog.Show("family", family.Name + "已载入");
    //        //    ts.Commit();
    //        //}

    //        //    using (Transaction ts = new Transaction(doc, "放置族"))
    //        //    {
    //        //        ts.Start();                
    //        //        FamilyInstance familyInstance = PlaceFamilyInstanceByHost();
    //        //        TaskDialog.Show("family", familyInstance.Name + "已载入");
    //        //        ts.Commit();
    //        //    }

    //        //从房间选门，不需要新创建，因此不用在事务中执行
    //        //List<ElementId> ids = GetDoorsByRoom(); //执行方法
    //        //uiDoc.Selection.SetElementIds(ids); //高亮选中门

    //        using (Transaction ts = new Transaction(doc, "Title"))
    //        {
    //            ts.Start();
    //            //CreateRooms();
    //            ts.Commit();
    //        }
    //        return Result.Succeeded;
    //    }

    //    //0402 创建房间的方法,根据标高和DB.UVpoint点
    //    //public Room CreateRoom()
    //    //{
    //    //    Level level = doc.ActiveView.GenLevel;
    //    //    XYZ xYZ = uiDoc.Selection.PickPoint();
    //    //    UV uV = new UV(xYZ.X, xYZ.Y);
    //    //    Room room = doc.Create.NewRoom(level, uV);
    //    //    room.Name = "test";
    //    //    //改房间高度偏移
    //    //    room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET).Set(4000.Tofoot());

    //    //    return room;
    //    //}
    //    //所有闭合区域建立房间
    //    //public void CreateRooms()
    //    //{
    //    //    Level level = doc.ActiveView.GenLevel;
    //    //    doc.Create.NewRooms2(level);//此处在建筑样板成功，但机电样板要求提供phase参数才行
    //    //}
    //    //public List<ElementId> GetDoorsByRoom()
    //    //{
    //    //    //选择房间
    //    //    Reference roomRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterRoomClass());
    //    //    Room room = doc.GetElement(roomRef) as  Room;
    //    //    Level level = room.Level;

    //    //    FilteredElementCollector viewFiltered = new FilteredElementCollector(doc);
    //    //    List<ViewPlan> viewPlans = viewFiltered.OfClass(typeof(ViewPlan)).Cast<ViewPlan>().ToList();
    //    //    ViewPlan viewPlan = null;
    //    //    foreach (ViewPlan viewPlan1 in viewPlans)
    //    //    {
    //    //        if (viewPlan1.Name.Equals("Level 1") && viewPlan1.GenLevel.Id == level.Id)
    //    //        {
    //    //            ViewFamilyType viewFamilyType = doc.GetElement(viewPlan1.GetTypeId()) as ViewFamilyType;
    //    //            if (viewFamilyType.Name.Equals("Floor Plan"))//遍历以获取符合条件的并 赋值
    //    //            {
    //    //                viewPlan = viewPlan1;
    //    //                break;
    //    //            }
    //    //        }
    //    //    }

    //    //    //通过过滤器收集器得到门
    //    //    FilteredElementCollector elements = new FilteredElementCollector(doc,viewPlan.Id);
    //    //    List<FamilyInstance> doors = elements.OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();

    //    //    //得到与房间有关系的门
    //    //    List<ElementId> doorIds = new List<ElementId>();
    //    //    foreach (FamilyInstance door in doors)
    //    //    {
    //    //        if ((door.Room!=null && door.Room.Id == roomRef.ElementId)
    //    //            || (door.FromRoom != null && door.FromRoom.Id == roomRef.ElementId)
    //    //            || (door.ToRoom != null && door.ToRoom.Id == roomRef.ElementId)
    //    //                )
    //    //        {
    //    //            doorIds.Add(door.Id);
    //    //        }
    //    //    }
    //    //    return doorIds;
    //    //}

    //    //面积平面，需在面积平面执行，没啥意义
    //    //public Area NewArea()
    //    //{
    //    //    ViewPlan viewPlan = doc.ActiveView as ViewPlan;
    //    //    XYZ xYZ = uiDoc.Selection.PickPoint();
    //    //    UV uV = new UV(xYZ.X,xYZ.Y);

    //    //    Area area = doc.Create.NewArea(viewPlan, uV);

    //    //    return area;
    //    //}


    //    //0401载入族
    //    //public Family LoadFamily(string path)
    //    //{
    //    //    bool result =doc.LoadFamily(path,out Family family);
    //    //    if (!result) //如果return false 避免重复载入，让返回值不为null即可能否直接返回1？
    //    //    {

    //    //        //取文件名操作可用 Path.GetFileNameWithoutExtension(fileName)替代
    //    //        //FileInfo fileInfo = new FileInfo(path);
    //    //        //string fileName = fileInfo.Name;
    //    //        //int num = fileName.IndexOf('.');//取文件名
    //    //        //fileName = fileName.Substring(0,num);

    //    //        string fileName =  Path.GetFileNameWithoutExtension(path);

    //    //        FilteredElementCollector element = new FilteredElementCollector(doc);
    //    //        FamilySymbol familySymbol = element.OfClass(typeof(FamilySymbol)).Where(x => (x as FamilySymbol).FamilyName.Equals(fileName)).FirstOrDefault() as FamilySymbol; 
    //    //        family = familySymbol.Family;
    //    //    }
    //    //    return family;
    //    //}

    //    //放置实例，基于点
    //    //public FamilyInstance PlaceFamilyInstance()//一般4-5个参数，点，族类型(从族找symbol)，层，类型
    //    //{
    //    //    XYZ point = uiDoc.Selection.PickPoint();
    //    //    Family family = LoadFamily(@"D:\j1.rfa");
    //    //    ISet<ElementId> ids = family.GetFamilySymbolIds();
    //    //    ElementId id = ids.FirstOrDefault();
    //    //    FamilySymbol familySymbol = doc.GetElement(id) as FamilySymbol;

    //    //    if (!familySymbol.IsActive)//解决族未激活问题，第一次放置
    //    //    {
    //    //        familySymbol.Activate();
    //    //    }
    //    //    Level level =doc.ActiveView.GenLevel;

    //    //    FamilyInstance familyInstance = doc.Create.NewFamilyInstance(point,familySymbol,level,Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
    //    //    return familyInstance;
    //    //}

    //    //放置实例，基于宿主
    //    //public FamilyInstance PlaceFamilyInstanceByHost()
    //    //{
    //    //    Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,new filterWallClass());
    //    //    XYZ point = reference.GlobalPoint;
    //    //    Family family = LoadFamily(@"D:\m1.rfa");
    //    //    ISet<ElementId> ids = family.GetFamilySymbolIds();
    //    //    ElementId id = ids.FirstOrDefault();
    //    //    FamilySymbol familySymbol = doc.GetElement(id) as FamilySymbol;

    //    //    if (!familySymbol.IsActive)//解决族未激活问题，第一次放置
    //    //    {
    //    //        familySymbol.Activate();
    //    //    }
    //    //    //增加宿主
    //    //    Wall wall = doc.GetElement(reference) as Wall;

    //    //    Level level = doc.ActiveView.GenLevel;

    //    //为消除不精确点选导致的基底标高，未正确剪切等问题要如下代码，将点映射到墙的基线上
    //    //Curve wallCurve = (wall.Location as LocationCurve).Curve;
    //    //XYZ poxyz = wallCurve.Project(point).XYZPoint;
    //    //如果基于面的构件可以写基于element的某个面planarFace，然后也要把点映射到面上
    //    //IList<Reference> references = HostObjectUtils.GetBottomFaces(wall);
    //    //Reference reference1= references.FirstOrDefault();
    //    //PlanarFace planarFace = wall.GetGeometryObjectFromReference(reference1) as PlanarFace;
    //    //XYZ poxyz2 = planarFace.Project(point).XYZPoint;

    //    //用几何方法Geometry取特定朝向的面
    //    //GeometryElement geometryElement = wall.get_Geometry(new Options() { ComputeReferences = true});
    //    //foreach (GeometryObject geometryObject in geometryElement)
    //    //{
    //    //    bool isBreak =false;//控制外层循环结束
    //    //    Solid solid = geometryObject as Solid;
    //    //    if (solid != null)
    //    //    {
    //    //        FaceArray faceArray = solid.Faces;
    //    //        foreach (Face face in faceArray)
    //    //        {
    //    //            if (face is PlanarFace)
    //    //            {
    //    //                PlanarFace planar = face as PlanarFace;
    //    //                //面的朝向，无距离
    //    //                XYZ  faceNormal =planar.FaceNormal;
    //    //                if (faceNormal.IsAlmostEqualTo(XYZ.BasisZ.Negate(), 2))//2是阈值，误差可控未转mm？
    //    //                { 
    //    //                    isBreak = true;
    //    //                    break;
    //    //                }
    //    //            }
    //    //        }
    //    //    }
    //    //    if (isBreak)
    //    //    {
    //    //        break;
    //    //    }
    //    //}

    //    //FamilyInstance familyInstance = doc.Create.NewFamilyInstance(poxyz, familySymbol, wall,level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
    //    //return familyInstance;

    ////}


    //}
}
