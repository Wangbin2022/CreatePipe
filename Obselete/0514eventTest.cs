using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class _0514eventTest : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Selection sel = uiDoc.Selection;
    //        //View activeView = uiDoc.ActiveView;
    //        UIApplication uIApp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uIApp.Application;

    //        //取消操作回滚事务并说明错误原因例程 0516
    //        //Transaction transaction = new Transaction(doc);
    //        //try
    //        //{
    //        //    transaction.Start("Name");//事务也可以在这里取名
    //        //    transaction.RollBack();
    //        //    transaction.Commit();
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    if (transaction.GetStatus() == TransactionStatus.Started)
    //        //        transaction.RollBack();
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}
    //        //例程结束

    //        //应用级别文件修改事件，似乎适用于记录文件所有变化 0514
    //        //commandData.Application.Application.DocumentChanged += appChange; //是否应加上判断是族文档还是普通文档？
    //        //例程结束
    //        //文档级别文件关闭事件,只对当前文档有效 0514
    //        //commandData.Application.ActiveUIDocument.Document.DocumentClosing += docClosing;
    //        //例程结束

    //        //外部事件
    //        //MainWindow nWin = new MainWindow();
    //        //nWin.Show();    
    //        //例程结束
    //        //闲置事件不做了没有什么应用场景
    //        //居然可以直接修改proj文件加入guid打开 右键新建WPF窗口功能？
    //        //添加VS项目资源还挺麻烦的，居然这里教的相对引用方法


    //        //例程统计选中实例数量 0515
    //        //var doorfilter = new DoorFilter();
    //        //try
    //        //{
    //        //    var reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,doorfilter);
    //        //    var element = doc.GetElement(reference.ElementId);
    //        //    var door = element as FamilyInstance;
    //        //    var geometryObjects = door.GetGeometryObjects();
    //        //    TaskDialog.Show("Title","统计数量为："+geometryObjects.Count.ToString());
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}
    //        //例程结束

    //        //获取房间信息（参数统计）并输出到xls 0516
    //        //var collector = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).ToElements();//此处过滤如果用room会出错
    //        //var roomInfoList = new List<List<string>>();
    //        //foreach (Room item in collector)
    //        //{
    //        //    var name = item.Name;
    //        //    var area = item.Area;
    //        //    var levelName = item.Level.Name;
    //        //    var parameter = item.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
    //        //    var roomHeight = parameter.AsValueString();
    //        //    var roomInfo = new List<string> { name,area.ToString(),levelName,roomHeight};
    //        //    roomInfoList.Add(roomInfo);
    //        //}
    //        ////以下调用NOPI实现输出xls
    //        //var workbook = new HSSFWorkbook();
    //        //var sheet = workbook.CreateSheet("房间信息");
    //        //var headers = new string[] {"房间名称","房间面积","房间所在标高","房间标识高度"};
    //        //var row0 = sheet.CreateRow(0);
    //        //for (int i = 0; i < headers.Count(); i++)
    //        //{
    //        //    var cell = row0.CreateCell(i);
    //        //    cell.SetCellValue(headers[i]);
    //        //}
    //        //for (int i = 0; i < roomInfoList.Count; i++)
    //        //{
    //        //    var row = sheet.CreateRow(i + 1);
    //        //    for (int j = 0; j < roomInfoList[i].Count; j++)
    //        //    { 
    //        //        var cell = row.CreateCell(j);
    //        //        cell.SetCellValue(roomInfoList[i][j]);
    //        //    }
    //        //}
    //        //SaveFileDialog fileDialog = new SaveFileDialog();
    //        //fileDialog.Filter = "(Excel文件)|*.xls";//控制生成文件类型
    //        //fileDialog.FileName = "房间面积统计";//默认文件名

    //        //bool isFileOk = false;
    //        //fileDialog.FileOk += (s, e) => { isFileOk = true; };//匿名委托
    //        //fileDialog.ShowDialog();
    //        //if (isFileOk ) 
    //        //{
    //        //    var path = fileDialog.FileName;
    //        //    using (var fs = File.OpenWrite(path))
    //        //    {
    //        //        workbook.Write(fs);
    //        //        System.Windows.Forms.MessageBox.Show($"文件成功保存到{fileDialog.FileName}","BIMBOX");
    //        //    }
    //        //}
    //        //例程结束

    //        //事务生成墙再开门 0518
    //        //var doc1 = commandData.Application.ActiveUIDocument.Document;
    //        //var line = Line.CreateBound(new XYZ(0,0,0),new XYZ(10,0,0));
    //        //var levelist = from element in new FilteredElementCollector(doc1).OfClass(typeof(Level))
    //        //               where element.Name == "标高 1"
    //        //               select element;
    //        //var level = levelist.FirstOrDefault() as Level;
    //        ////定义开门位置
    //        //var doorLocation = line.Evaluate(0.5, true);
    //        //var doorId = new ElementId(94652);
    //        //var doorSymbol = doc1.GetElement(doorId) as FamilySymbol;
    //        //try
    //        //{
    //        //    Transaction ts = new Transaction(doc1, "创建墙体");
    //        //    ts.Start();
    //        //    var wall =Wall.Create(doc1, line, level.Id, false);
    //        //    if (!doorSymbol.IsActive) doorSymbol.Activate();
    //        //    doc1.Create.NewFamilyInstance(doorLocation, doorSymbol,wall,Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
    //        //    ts.Commit();
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("BIMBOX",ex.Message);
    //        //    return Result.Failed;
    //        //}
    //        // 例程结束

    //        //创建梁并设置错误忽略方法 0518
    //        //var doc1 = commandData.Application.ActiveUIDocument.Document;
    //        //var levelist = from element in new FilteredElementCollector(doc1).OfClass(typeof(Level))
    //        //               where element.Name == "标高 1"
    //        //               select element;
    //        //var level = levelist.FirstOrDefault() as Level;
    //        //try
    //        //{
    //        //    var line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
    //        //    var familySymbol = doc1.GetElement(new ElementId(265299)) as FamilySymbol;
    //        //    Transaction ts = new Transaction(doc1, "创建梁");

    //        //    var options = ts.GetFailureHandlingOptions();//注意防错误提示位置以下4行
    //        //    var processor = new InaccurateFailureProcessor();
    //        //    options.SetFailuresPreprocessor(processor);
    //        //    ts.SetFailureHandlingOptions(options);

    //        //    ts.Start();
    //        //    if (!familySymbol.IsActive) familySymbol.Activate();
    //        //    doc1.Create.NewFamilyInstance(line, familySymbol, level, Autodesk.Revit.DB.Structure.StructuralType.Beam);
    //        //    ts.Commit();
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("BIMBOX", ex.Message);
    //        //    return Result.Failed;
    //        //}
    //        // 例程结束


    //        //几何生成例程 0518 寻找交点坐标
    //        //没有改模型实际上无需事务
    //        //var point1 = new XYZ(2, 0, 0);
    //        //var point2 = new XYZ(0, 2, 0);
    //        //var point3 = new XYZ(3, 3, 0);
    //        //var line1 = Line.CreateBound(point1, point2);
    //        //var line2 = Line.CreateBound(XYZ.Zero, point3);
    //        //IntersectionResultArray results;
    //        //var result = line1.Intersect(line2, out results);
    //        //if (result == SetComparisonResult.Overlap)
    //        //{ 
    //        //    var point = results.get_Item(0).XYZPoint;
    //        //    TaskDialog.Show("BIMBOX",point.ToString());
    //        //}
    //        //例程结束

    //        //建立几何拉伸体 0518   生成directShape并附件属性schema
    //        //var tol = commandData.Application.Application.ShortCurveTolerance;// 防止过短线
    //        //var point1 = new XYZ(0, 0, 0);
    //        //var point2 = new XYZ(5, 0, 0);
    //        //var point3 = new XYZ(5, 8, 0);
    //        //var point4 = new XYZ(0, 8, 0);
    //        //var line1 = Line.CreateBound(point1, point2);
    //        //var line2 = Line.CreateBound(point2, point3);
    //        //var line3 = Line.CreateBound(point3, point4);
    //        //var line4 = Line.CreateBound(point4, point1);
    //        //var curveLoop = new CurveLoop();
    //        //curveLoop.Append(line1);
    //        //curveLoop.Append(line2);
    //        //curveLoop.Append(line3);
    //        //curveLoop.Append(line4);
    //        //var transform = Transform.CreateTranslation(new XYZ(5, 5, 0));
    //        //curveLoop.Transform(transform);
    //        //var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { curveLoop }, XYZ.BasisZ, 10);
    //        //var ts = new Transaction(doc, "几何体创建");
    //        //ts.Start();
    //        //var shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
    //        //shape.SetShape(new GeometryObject[] { solid });

    //        ////几何完成，以下添加属性,需补充guid
    //        ////var schemabuilder = new SchemaBuilder(_schemaGuid);
    //        //var schema = Schema.Lookup(_schemaGuid);//检查是否存在，避免重复
    //        //if (schema == null)
    //        //{
    //        //    var schemabuilder = new SchemaBuilder(_schemaGuid);
    //        //    schemabuilder.SetReadAccessLevel(AccessLevel.Public);
    //        //    schemabuilder.SetWriteAccessLevel(AccessLevel.Public);
    //        //    schemabuilder.SetSchemaName("cacc");
    //        //    schemabuilder.SetDocumentation("UniqueTag");
    //        //    var filedBuilder = schemabuilder.AddSimpleField("name", typeof(string));
    //        //    //定义数据
    //        //    schema = schemabuilder.Finish();
    //        //}

    //        //var entity = new Entity(schema);
    //        //var name = schema.GetField("name");
    //        //entity.Set(name, "cacc_BIM"); // 属性赋值
    //        //shape.SetEntity(entity);


    //        //var dataStorageList = from element in new FilteredElementCollector(doc).OfClass(typeof(DataStorage))
    //        //                      let storage = element as DataStorage
    //        //                      where storage.GetEntitySchemaGuids().Contains(_schemaGuid)
    //        //                      select storage;
    //        //var dataStorage = dataStorageList.FirstOrDefault();
    //        //if (dataStorage == null)
    //        //{
    //        //    dataStorage = DataStorage.Create(doc);//建立文档级别新类型数据
    //        //    dataStorage.SetEntity(entity);
    //        //}

    //        //var dataEntity = dataStorage.GetEntity(schema);
    //        //var field = dataEntity.Schema.GetField("name");
    //        //var result = dataEntity.Get<string>(field);
    //        //TaskDialog.Show("CACC", "名字叫：" + result);

    //        //ts.Commit();
    //        //例程结束

    //        //平面视图，文字，标注样例，应用模板 0518 基于自带建筑样例文件
    //        //try
    //        //{
    //        //    #region 创建楼层平面
    //        //    var ts = new Transaction(doc, "创建图纸");
    //        //    ts.Start();
    //        //    var level = Level.Create(doc, 1);//1英尺位置创建标高
    //        //    var viewTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
    //        //                       let type = element as ViewFamilyType
    //        //                       where type.ViewFamily == ViewFamily.FloorPlan
    //        //                       select type;
    //        //    var viewTypeId = viewTypeList?.FirstOrDefault()?.Id;//加上？增加空值判断
    //        //    if (viewTypeId == null) throw new Exception("没有找到楼层平面");
    //        //    var viewPlan = ViewPlan.Create(doc, viewTypeId, level.Id);
    //        //    #endregion

    //        //    #region 视图中增加文字，先创建类型
    //        //    TextNoteType newTextNoteType;
    //        //    var textFamilyName = "3.5mm ST";
    //        //    var textNoteTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(TextNoteType))
    //        //                           let type = element as TextNoteType
    //        //                           where type.FamilyName == "文字" && type.Name == textFamilyName
    //        //                           select type;
    //        //    if (textNoteTypeList.Count() > 0)
    //        //        newTextNoteType = textNoteTypeList.FirstOrDefault();
    //        //    else
    //        //    {
    //        //        textNoteTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(TextNoteType))
    //        //                           let type = element as TextNoteType
    //        //                           where type.FamilyName == "文字"
    //        //                           select type;
    //        //        var textNoteType = textNoteTypeList.FirstOrDefault();
    //        //        newTextNoteType = textNoteType.Duplicate(textFamilyName) as TextNoteType; //复制类型方法
    //        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(3.5 / 304.8);
    //        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_FONT).Set("宋体");
    //        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);// 设置为透明背景
    //        //    }
    //        //    #endregion

    //        //    #region 创建文字
    //        //    var option = new TextNoteOptions();
    //        //    option.HorizontalAlignment = HorizontalTextAlignment.Center;
    //        //    option.TypeId = newTextNoteType.Id;
    //        //    var textNote = TextNote.Create(doc, viewPlan.Id, new XYZ(0, 0, 0), viewPlan.Name, option);
    //        //    #endregion

    //        //    #region 应用视图样板 //查找，复制
    //        //    var viewTemplateList = from element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan))
    //        //                           let view = element as ViewPlan
    //        //                           where view.IsTemplate && view.Name == "Architectural Plan"
    //        //                           select view;
    //        //    var viewTemplate = viewTemplateList?.FirstOrDefault();
    //        //    if (viewTemplate == null) throw new Exception("没有找到视图样板");
    //        //    viewPlan.ViewTemplateId = viewTemplate.Id;
    //        //    #endregion

    //        //    #region 标注墙对象示例
    //        //    var dimTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(DimensionType))
    //        //                           let type = element as DimensionType
    //        //                           where type.Name == "Feet & Inches"
    //        //                           select type;
    //        //    var targetDimensionType =dimTypeList?.FirstOrDefault();
    //        //    if (targetDimensionType == null) throw new Exception("未找到标注样式");

    //        //    var wall = doc.GetElement(new ElementId(1102865)) as Wall;  
    //        //    var wallLocationCurve = (wall.Location as LocationCurve).Curve;
    //        //    var wallDirection = (wallLocationCurve as Line).Direction;
    //        //    var options = new Options();
    //        //    options.ComputeReferences = true;
    //        //    var wallSolid = wall.GetGeometryObjects(options).FirstOrDefault() as Solid;// 基于自建扩展

    //        //    var references = new ReferenceArray();
    //        //    foreach (Face face in wallSolid.Faces)
    //        //    {
    //        //        if (face is PlanarFace pFace && pFace.FaceNormal.CrossProduct(wallDirection).IsAlmostEqualTo(XYZ.Zero))
    //        //        {
    //        //            references.Append(face.Reference);
    //        //        }
    //        //    }
    //        //    var offset = 1000 / 304.8; //标注偏移距离
    //        //    var line = Line.CreateBound(wallLocationCurve.GetEndPoint(0) + XYZ.BasisY * offset, wallLocationCurve.GetEndPoint(1) + XYZ.BasisY * offset);
    //        //    var dimension = doc.Create.NewDimension(viewPlan, line, references);
    //        //    dimension.DimensionType = targetDimensionType;
    //        //    #endregion

    //        //    #region 创建图纸
    //        //    //创建图纸
    //        //    var sheet = ViewSheet.Create(doc, new ElementId(382615));
    //        //    //将视图添加到图纸
    //        //    if (Viewport.CanAddViewToSheet(doc,sheet.Id,viewPlan.Id))//这里写错为什么会没有添加视图？
    //        //    {
    //        //        var uv = new XYZ(698/304.8/2,522/304.8/2,0);
    //        //        var viewPort = Viewport.Create(doc,sheet.Id,viewPlan.Id,uv);
    //        //    }
    //        //    #endregion
    //        //    ts.Commit();
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("错误报告", ex.Message);
    //        //    return Result.Failed;
    //        //}
    //        //例程结束

    //        //面生面功能 0518
    //        var faceReference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Face, "拾取要复制的墙面");
    //        var wallofFace = doc.GetElement(faceReference) as Wall;
    //        var face = wallofFace.GetGeometryObjectFromReference(faceReference) as Face;
    //        var wallTypes = from element in new FilteredElementCollector(doc).OfClass(typeof(WallType))
    //                        let type = element as WallType
    //                        select type;
    //        var faceConfigWin = new FaceConfig(wallTypes.ToList());//调用xmal生成窗体
    //        var result = faceConfigWin.ShowDialog(); //接收返回
    //        if (result.HasValue && result.Value)
    //        {
    //            var ts = new Transaction(doc, "创建面生面");
    //            ts.Start();
    //            CreateFace(doc, face, wallofFace, faceConfigWin.SelectedWallType);
    //            ts.Commit();
    //            return Result.Succeeded;
    //        }
    //        return Result.Cancelled;
    //        //例程结束

    //        //return Result.Succeeded;
    //    }

    //    //生成面方法 0518
    //    private void CreateFace(Document doc, Face face, Wall wallofFace, WallType selectedWallType)
    //    {
    //        var profile = new List<Curve>();
    //        var openingArrays = new List<CurveArray>();
    //        var width = selectedWallType.Width;
    //        ExtractFaceOutline(face, width, ref profile, ref openingArrays);//提取轮廓线
    //        var wall = Wall.Create(doc, profile, selectedWallType.Id, wallofFace.LevelId, false);
    //        wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);//设置底部偏移
    //        foreach (var item in openingArrays)
    //        {
    //            doc.Create.NewOpening(wall, item.get_Item(0).GetEndPoint(0), item.get_Item(1).GetEndPoint(1));
    //        }
    //    }

    //    private void ExtractFaceOutline(Face face, double width, ref List<Curve> profile, ref List<CurveArray> openingArrays) //提取截面，Curve是墙体，CurveArray是墙体上的洞口
    //    {
    //        var curveLoops = face.GetEdgesAsCurveLoops();
    //        var normal = (face as PlanarFace)?.FaceNormal;
    //        if (normal == null) throw new ArgumentException("非平面不可用");
    //        var translation = Transform.CreateTranslation(normal * width / 2);
    //        int i = 0;
    //        foreach (var curveLoop in curveLoops.OrderByDescending(x => x.GetExactLength()))
    //        {
    //            curveLoop.Transform(translation);
    //            var array = new CurveArray();
    //            foreach (var curve in curveLoop)
    //            {
    //                if (i == 0)
    //                    profile.Add(curve);
    //                else array.Append(curve);
    //            }
    //            if (i != 0) openingArrays.Add(array);
    //            i++;
    //        }
    //    }
    //    //例程结束

    //    //Kevin入门课1 0514 字段与属性关联控制例程
    //    //private int age1;  
    //    //public int age //类成员属性是字段的升级,控制输入输出  默认访问器 get 和set还可以在里面设置规则！
    //    //{
    //    //    get { return age1; }//
    //    //    set 
    //    //    {   if (value < 0) age1 = 18;
    //    //        else age1 = value;
    //    //    }
    //    //}
    //    //public Gender Gender { get; set; }
    //    //例程结束
    //    //类和方法前面加static才能点出来
    //    //构造函数=构造类的同时初始化 ,使用时直接把参数引入
    //    //using引用和程序集引用区别在与是否可访问内部
    //    //数组[]和列表List<>都是同类型数据的有序组合，数组要声明长度且不可改变长度
    //    //List<double> nums = new List<double> { 1, 2, 3 };
    //    //Result 是一种固定的枚举类型，有那几个成员。
    //    //private void docClosing(object sender, DocumentClosingEventArgs e)
    //    //{
    //    //    TaskDialog.Show("关闭", "已关闭");
    //    //}
    //    //private void appChange(object sender, DocumentChangedEventArgs e)
    //    //{
    //    //    TaskDialog.Show("改动", "已改动");//是否应加上判断是族文档还是普通文档？
    //    //}
    //}

    //配合外部事件例程
    //public class ExternalCommand : IExternalEventHandler
    //{
    //    public void Execute(UIApplication app)
    //    {
    //        TaskDialog.Show("测试", "测试中");
    //    }
    //    public string GetName()
    //    {
    //        return "名称";
    //    }
    //}

    //统计所选构建数量例程，要测试还要加上主程序trycatch以及过滤器 0515OK
    public static class GeometryObjectHelper
    {
        /// <summary>
        /// 获取元素几何对象
        /// </summary>
        /// <param name="element">选的元素</param>
        /// <param name="options">限制条件</param>
        /// <returns></returns>
        public static List<GeometryObject> GetGeometryObjects(this Element element, Options options = default(Options))
        {
            List<GeometryObject> results = new List<GeometryObject>();
            options = options ?? new Options();
            GeometryElement geometry = element.get_Geometry(options);
            RecurseObject(geometry, ref results);
            return results;
        }
        /// <summary>
        /// 递归遍历所有几何对象
        /// </summary>
        /// <param name="geometryElement">初始几何对象</param>
        /// <param name="geometryObjects">递归结果</param>
        private static void RecurseObject(this GeometryElement geometryElement, ref List<GeometryObject> geometryObjects)
        {
            if (geometryElement == null) return;
            IEnumerator<GeometryObject> enumerator = geometryElement.GetEnumerator();
            while (enumerator.MoveNext())
            {
                GeometryObject current = enumerator.Current;
                switch (current) // 排除不符合要求的遍历
                {
                    case GeometryInstance instance:
                        instance.SymbolGeometry.RecurseObject(ref geometryObjects);
                        break;
                    case GeometryElement element:
                        element.RecurseObject(ref geometryObjects);
                        break;
                    case Solid solid:
                        if (solid.Faces.Size == 0 || solid.Edges.Size == 0) continue;
                        geometryObjects.Add(solid);
                        break;
                    default:
                        geometryObjects.Add(current);
                        break;
                }
            }
        }
    }
    public class DoorFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors) return true;
            else return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    // 例程结束

    //忽略错误方法使用例程 0518
    public class InaccurateFailureProcessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failList = failuresAccessor.GetFailureMessages();
            foreach (var item in failList)
            {
                var failureId = item.GetFailureDefinitionId();
                if (failureId == BuiltInFailures.InaccurateFailures.InaccurateBeamOrBrace)
                    failuresAccessor.DeleteWarning(item);
            }
            return FailureProcessingResult.Continue;
        }
    }

    // 例程结束
}
