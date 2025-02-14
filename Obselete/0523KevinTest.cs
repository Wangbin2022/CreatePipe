using Autodesk.Revit.DB;

namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //public class _0523KevinTest : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Selection sel = uiDoc.Selection;

    //        // View activeView = uiDoc.ActiveView;
    //        UIApplication uiapp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uiapp.Application;

    //        //typeof(BuiltInFailures.) 需要实践读取出所有错误提示那个例程

    //        //单个构件碰撞检查 例程 0512  
    //        try
    //        {
    //            Transaction trans = new Transaction(doc, "碰撞检测");


    //            List<ElementId> elemIds = uiDoc.Selection.GetElementIds().ToList();
    //            ElementId elementId = uiDoc.Selection.GetElementIds().FirstOrDefault();
    //            if (elemIds.Count() > 1)
    //            {
    //                TaskDialog.Show("错误提示", "不支持多个构件同时检查碰撞");
    //                trans.RollBack();
    //                return Result.Failed;
    //            }
    //            else if (elementId == null)
    //            {
    //                Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "选择要检查的对象");
    //                elementId = doc.GetElement(reference).Id;
    //            }
    //            Selection select = uiDoc.Selection;
    //            trans.Start();
    //            Element element = doc.GetElement(elementId);
    //            FilteredElementCollector collect = new FilteredElementCollector(doc);
    //            //冲突检查
    //            ElementIntersectsElementFilter iFilter = new ElementIntersectsElementFilter(element, false);
    //            collect.WherePasses(iFilter);
    //            List<ElementId> excludes = new List<ElementId>();
    //            excludes.Add(element.Id); //需要在此把不参与碰撞构件排除掉
    //            collect.Excluding(excludes);
    //            List<ElementId> ids = new List<ElementId>();
    //            select.SetElementIds(ids);

    //            foreach (Element elem in collect)
    //            {
    //                ids.Add(elem.Id);
    //            }
    //            select.SetElementIds(ids);
    //            trans.Commit();
    //            TaskDialog.Show("Main Windows", "发现碰撞" + ids.Count + "处");
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog td = new TaskDialog("title");
    //            td.MainInstruction = "请注意";
    //            td.MainContent = "命令已退出";
    //            td.FooterText = "系统提示：" + ex.Message;
    //            var result = td.Show();
    //            //TaskDialog.Show("Title", ex.Message);

    //        }
    //        //例程结束

    //        //DateTime st = DateTime.Now;
    //        //Thread.Sleep (500);
    //        //DateTime ed = DateTime.Now;
    //        //TimeSpan ts = ed.Subtract(st);
    //        //TaskDialog.Show("Main Windows","TestProgram.OK\n"+ts);

    //        ////过滤器预设
    //        //FilteredElementCollector collector1 = new FilteredElementCollector(doc);
    //        //IList<Element> elements2 = collector1.OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Walls).ToElements();
    //        //ICollection<ElementId> elementIds =collector1.OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Walls).ToElementIds();

    //        ////过滤器与linq语法对比
    //        //foreach (Element element in elements)
    //        //{
    //        //    if (element.Name.Equals("常规"))
    //        //    {
    //        //        //添加Id动作
    //        //    }
    //        //}//返回Id
    //        ////等同以上
    //        //IEnumerable<Element> walls = from element in elements2
    //        //                             where element.Name =="常规"
    //        //                             select element;
    //        //foreach (Element element in elements)
    //        //{
    //        //    //添加Id动作
    //        //}

    //        ////更简化 
    //        //IEnumerable<ElementId> wallIds = from element in elements2
    //        //                             where element.Name == "常规"
    //        //                             select element.Id;
    //        ////直接转为ILIST<>，终极方案
    //        //IList<ElementId> wallId2s = (from element in elements2
    //        //                                 where element.Name == "常规"
    //        //                                 select element.Id).ToList();

    //        ////定义后再转TOList也可以
    //        //var wall2s = from element in elements2
    //        //              where element.Name == "常规"
    //        //              select element.Id;
    //        //IList<ElementId> wallId3s =wall2s.ToList();
    //        ////return wallId3s;

    //        //// Use the rectangle picking tool to identify model elements to select.
    //        //// 选择并统计官方示例
    //        //IList<Element> pickedElements = uiDoc.Selection.PickElementsByRectangle("Select by rectangle");
    //        //if (pickedElements.Count > 0)
    //        //{
    //        //    // Collect Ids of all picked elements
    //        //    IList<ElementId> idsToSelect = new List<ElementId>(pickedElements.Count);
    //        //    foreach (Element element in pickedElements)
    //        //    {
    //        //        idsToSelect.Add(element.Id);
    //        //    }

    //        //    // Update the current selection
    //        //    uiDoc.Selection.SetElementIds(idsToSelect);
    //        //    TaskDialog.Show("Revit", string.Format("{0} elements added to Selection.", idsToSelect.Count));
    //        //}

    //        //兼容先选择或后选择的写法 例程P66
    //        //List<ElementId> elemIds = uiDoc.Selection.GetElementIds().ToList();
    //        //过滤已有选集中不符合要求的对象
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

    //        //点选看ID例程 0530
    //        //try
    //        //{
    //        //    StringBuilder stringBuilder2 = new StringBuilder();
    //        //    List<ElementId> elemIds = uiDoc.Selection.GetElementIds().ToList();
    //        //    if (elemIds.Count == 0)
    //        //    {
    //        //        Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //        ElementId elementId = doc.GetElement(reference).Id;
    //        //        TaskDialog.Show("element", "点选元素ID=" + elementId.ToString());
    //        //        return Result.Succeeded;
    //        //    }
    //        //    else if (elemIds.Count > 6)
    //        //    {
    //        //        TaskDialog.Show("错误提示", "请勿选取超过6个元素");
    //        //        return Result.Failed;
    //        //    }
    //        //    else foreach (var item in elemIds)
    //        //        {
    //        //            stringBuilder2.AppendLine(item + "\\");
    //        //        }
    //        //    TaskDialog.Show("element", "所选元素ID=" + stringBuilder2);
    //        //    //单选可用，显示选元素ID
    //        //    //ElementId elementId = uiDoc.Selection.GetElementIds().FirstOrDefault();
    //        //    //if (elementId == null)
    //        //    //{
    //        //    //    Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //    //    elementId = doc.GetElement(reference).Id;
    //        //    //}
    //        //    //TaskDialog.Show("element", "元素ID=" + elementId.ToString());
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}

    //        //翻转Revit底色 0530
    //        //if (uiapp.Application.BackgroundColor.Red != 0 || uiapp.Application.BackgroundColor.Blue != 0 || uiapp.Application.BackgroundColor.Green != 0)
    //        //{
    //        //    uiapp.Application.BackgroundColor = new Color(0, 0, 0);
    //        //}
    //        //else uiapp.Application.BackgroundColor = uiapp.Application.BackgroundColor.InversColor();

    //        //CADC1.1版修改 0614
    //        //if (doc.IsFamilyDocument)
    //        //{
    //        //    TaskDialog.Show("Title", "NewB");
    //        //}
    //        //else 
    //        //{ 
    //        //TaskDialog td = new TaskDialog("title");
    //        //td.MainInstruction = "请注意";
    //        //td.MainContent = "命令已退出";
    //        //td.FooterText = "系统错误原因" ;
    //        //var result = td.Show();
    //        //}
    //        //修改结束

    //        //设置平面视图底图为无 0615.OK
    //        //if (activeView.ViewType ==ViewType.FloorPlan || activeView.ViewType == ViewType.CeilingPlan) 
    //        //{
    //        //    ElementId elementId = new ElementId(-1); //能将id转为eID但没有绑定到标高类型上会报错，-1代表无
    //        //    ViewPlan viewPlan = activeView as ViewPlan;
    //        //    using (Transaction ts = new Transaction(doc, "修改基线图层"))
    //        //    {
    //        //        ts.Start();
    //        //        viewPlan.SetUnderlayBaseLevel(elementId);
    //        //        ts.Commit();
    //        //    }
    //        //}
    //        //else
    //        //    TaskDialog.Show("Title", "当前视图不支持修改基线图层");
    //        //程序结束

    //        //找出屋顶坡度 0615.OK
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new footPrintRoofFilter(), "选择迹线屋面");
    //        //FootPrintRoof footPrintRoof = doc.GetElement(reference) as FootPrintRoof;
    //        //ModelCurveArrArray modelCurveArrays = footPrintRoof.GetProfiles();

    //        //int mdlsum = 0;
    //        //foreach (ModelCurveArray curveloop in modelCurveArrays)
    //        //{
    //        //    foreach (ModelCurve mCurve in curveloop)
    //        //    {
    //        //        ModelLine modelLine = mCurve as ModelLine;
    //        //        int mdlint = modelLine.get_Parameter(BuiltInParameter.CURVE_IS_SLOPE_DEFINING).AsInteger();
    //        //        if (mdlint != 0)
    //        //        {
    //        //            double jd = modelLine.get_Parameter(BuiltInParameter.ROOF_SLOPE).AsDouble();
    //        //            string jdstring = jd.ToString();
    //        //            int jdint = Convert.ToInt16(jd * 100);
    //        //            string jdarc = Math.Asin(jd).ToString("0.00");
    //        //            string jdangle = (Math.Asin(jd) / Math.PI * 180).ToString("0.00");
    //        //            string jdratio = (1 / jd).ToString("0.00");

    //        //            TaskDialog.Show("Test", "迹线屋顶ID：" + footPrintRoof.Id.ToString() + "\n" + "坡度" + jdint + "%" + "\n" + "弧度值" + jdarc + "\n" + "角度值" + jdangle + "度" + "\n" + "坡比 1：" + jdratio);
    //        //            return Result.Succeeded;
    //        //        }
    //        //        mdlsum += mdlint;
    //        //    }
    //        //    //footPrintRoof.get_SlopeAngle(mCurve); //SlopeAngle已被禁用
    //        //}
    //        //if (mdlsum == 0)
    //        //{
    //        //    TaskDialog.Show("Test", "屋面无坡度");
    //        //    return Result.Cancelled;
    //        //}
    //        //程序结束
    //        //Modifying Floor Slope Programmatically – or Not
    //        //https://jeremytammik.github.io/tbc/a/1121_create_sloped_floor.htm#:~:text=Slope%20defining%20edges%20can%20be%20used%20successfully%20to,and%20sets%20a%20slope%20for%20the%20specified%20edge.
    //        //UIApplication uiapp = commandData.Application; 
    //        //UIDocument uidoc = uiapp.ActiveUIDocument; 
    //        //Document doc = uidoc.Document; 
    //        //Selection sel = uidoc.Selection; 
    //        //Reference ref1 = sel.PickObject(ObjectType.Element, "Please pick a floor."); 
    //        //Floor f = doc.GetElement(ref1) as Floor; if (f == null) return Result.Failed;     
    //        //// Retrieve floor edge model line elements.
    //        //ICollection<ElementId> deleted_ids;     
    //        //using( Transaction tx = new Transaction( doc ) )   
    //        //{     
    //        //    tx.Start( "Temporarily Delete Floor" );       
    //        //    deleted_ids = doc.Delete( f.Id );       
    //        //    tx.RollBack();   }     
    //        //// Grab the first floor edge model line.
    //        //ModelLine ml = null;     
    //        //foreach( 
    //        //    ElementId id in deleted_ids )   
    //        //{     
    //        //    ml = doc.GetElement( id ) as ModelLine;       
    //        //    if( null != ml )     
    //        //    {       
    //        //        break;                 
    //        //    }  
    //        //}    
    //        //if( null != ml ) 
    //        //{     
    //        //    using( Transaction tx = new Transaction( doc ) )     
    //        //    {       
    //        //        tx.Start( "Change Slope Angle" );        
    //        //        // This parameter is read only. Therefore, the change does not work and we cannot change the floor slope angle after the floor is created.
    //        //        ml.get_Parameter(BuiltInParameter.CURVE_IS_SLOPE_DEFINING ) .Set( 1 ); 
    //        //        ml.get_Parameter(BuiltInParameter.ROOF_SLOPE ) .Set( 1.2 ); 
    //        //        tx.Commit();
    //        //    } 
    //        //}   
    //        //return Result.Succeeded;

    //        //这个方法不对，但可以参考获取几何构造，也许以后有用 0615
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Face, new PlanarFaceFilter(doc), "选择面");
    //        //PlanarFace pFace = doc.GetElement(reference).GetGeometryObjectFromReference(reference) as PlanarFace;
    //        //IList<CurveLoop> curveLoops = pFace.GetEdgesAsCurveLoops();
    //        //foreach (CurveLoop curveloop in curveLoops)
    //        //{
    //        //    foreach (Curve item in curveloop)
    //        //    {
    //        //        if (item.IsBound)
    //        //        {
    //        //            Line line= item as Line;                        
    //        //        }
    //        //    }
    //        //}
    //        //提取屋面坡度例程结束



    //        return Result.Succeeded;

    //    }

    //}
    public static class ColorExtension //加入改写的方法
    {
        public static Color InversColor(this Color color)
        {
            var newcolor = default(Color);
            var newR = (byte)(255 - color.Red);
            var newG = (byte)(255 - color.Green);
            var newB = (byte)(255 - color.Blue);

            newcolor = new Color(newR, newG, newB);
            return newcolor;
        }
    }

    //json入门 0614 暂没找到办法测试
    //    DataContractSerializer serializer = new DataContractSerializer(typeof(Student));
    //    // 创建一个流，比如用于文件或网络传输
    //    using (MemoryStream stream = new MemoryStream())
    //    {
    //        // 序列化
    //        serializer.WriteObject(stream, new Student { id = 1, name = "John Doe", age = 20, sex = "Male" });

    //        // 将流转换为字节数组,没搞懂
    //        byte[] data = stream.ToArray();

    //        // 反序列化
    //        stream.Position = 0;
    //        Student deserializedStudent = (Student)serializer.ReadObject(stream);
    //    }
    //    //JsonHelper jsonHelper = new JsonHelper();
    //    //JsonHelper.JsonToObject
    //    TaskDialog.Show("Title", "NewB");
    //    return Result.Succeeded;
    //}
    ////
    //public class JsonHelper
    //{
    //    //对象转换为json例程
    //    public static string ObjectToJson<T>(Object jsonObject, Encoding encoding)
    //    {
    //        string result = String.Empty;
    //        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
    //        using(System.IO.MemoryStream ms =new System.IO.MemoryStream()) 
    //        { 
    //            serializer.WriteObject(ms, jsonObject);
    //            result = encoding.GetString(ms.ToArray());
    //        }               
    //        return result;
    //    }
    //    //json转换为对象例程
    //    public static T JsonToObject<T>(string json, Encoding encoding)
    //    { 
    //        T resultObject = Activator.CreateInstance<T>();
    //        DataContractJsonSerializer serializer = new DataContractJsonSerializer (typeof(T));
    //        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(encoding.GetBytes(json)))
    //        { 
    //            resultObject = (T)serializer.ReadObject(ms);
    //        }
    //        return resultObject;
    //    }
    //}
    ////数据项列举
    //[DataContract]
    //public class Student 
    //{
    //    [DataMember]
    //    public int id { get; set; }
    //    [DataMember] 
    //    public string name { get;set; }
    //    [DataMember]
    //    public int age { get; set; }
    //    [DataMember]
    //    public string sex { get; set; }
    //}
    //例程结束
}
