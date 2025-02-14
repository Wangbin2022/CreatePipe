using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CreatePipe
{
    internal class OnlyProjectFuncHelper : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return !applicationData.ActiveUIDocument.Document.IsFamilyDocument;
        }
    }
    internal class Only3DviewHelper : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return (applicationData.ActiveUIDocument.Document.ActiveView.ViewType == ViewType.ThreeD);
        }
    }
    internal class obselete1021
    {
        //1007 这个代码对所有视图有效么？
        //Autodesk.Revit.DB.Options options = application.Create.NewGeometryOptions();
        //options.DetailLevel =ViewDetailLevel.Fine;

        //1007 反射获得参照平面？
        //ReferencePlane refPlane = GetRefPlane();
        //Transform.mirTrans = Transform.CreateReflection(refPlane.plane);

        ////============代码片段3-7 获取共享参数============
        //// 打开共享参数文件
        //DefinitionFile definitionFile = application.OpenSharedParameterFile();
        //// 获取参数组的集合
        //DefinitionGroups groups = definitionFile.Groups;
        //foreach (DefinitionGroup group in groups)
        //{
        //    // 获取参数组内的参数定义
        //    foreach (Definition definition in group.Definitions)
        //    {
        //        string name = definition.Name;
        //        ParameterType type = definition.ParameterType;
        //        // 对参数定义的其他操作
        //    }
        //}

        ////1007待测试方法
        ////============代码片段3-8 创建共享参数============
        //string sharedParametersFilename = @"C:\shared-parameters.txt";
        //string groupName = "MyGroup";
        //string definitionName = "MyDefinition";
        //ParameterType parameterType = ParameterType.Text;
        //CategorySet categorySet = new CategorySet();
        //Category wallCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls);
        //categorySet.Insert(wallCategory);
        //bool instanceParameter = true;
        //BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.PG_DATA;
        //if (!System.IO.File.Exists(sharedParametersFilename))
        //{
        //    try
        //    {
        //        System.IO.StreamWriter sw = System.IO.File.CreateText(sharedParametersFilename);
        //        sw.Close();
        //    }
        //    catch (Exception)
        //    {
        //        throw new Exception("Can't create shared parameter file: " + sharedParametersFilename);
        //    }
        //}
        //// 设置共享参数文件
        //application.SharedParametersFilename = sharedParametersFilename;
        //// 打开共享参数文件
        //DefinitionFile definitionFile = application.OpenSharedParameterFile();
        //if (definitionFile == null)
        //{
        //    throw new Exception("Can not open shared parameter file!");
        //}
        //// 获取参数组的集合
        //DefinitionGroups groups = definitionFile.Groups;
        //// 获取参数组
        //DefinitionGroup group = groups.get_Item(groupName);
        //if (null == group)
        //{
        //    // 如果参数组不存在，则创建一个
        //    group = groups.Create(groupName);
        //}
        //if (null == group)
        //    throw new Exception("Failed to get or create group: " + groupName);
        //// 获取参数定义
        //Definition definition = group.Definitions.get_Item(definitionName);
        //if (definition == null)
        //{
        //    // 如果参数定义不存在，则创建一个
        //    ExternalDefinitionCreationOptions edco =new ExternalDefinitionCreationOptions(definitionName, parameterType);
        //    definition = group.Definitions.Create(edco);
        //    //definition = group.Definitions.Create(definitionName, parameterType);
        //}
        //// 调用不同的函数创建类型参数或者实例参数
        //ElementBinding binding = null;
        //if (instanceParameter)
        //{
        //    binding = application.Create.NewInstanceBinding(categorySet);
        //}
        //else
        //{
        //    binding = application.Create.NewTypeBinding(categorySet);
        //}
        //// 把参数定义和类别绑定起来（下面的小节会提到“绑定”），元素的新的参数就创建成功了。
        //bool insertSuccess = doc.ParameterBindings.Insert(definition, binding, parameterGroup);
        //if (!insertSuccess)
        //{
        //    throw new Exception("Failed to bind definition to category");
        //}

        ////1007 没看懂用处，后续如何处理这个categoryset？
        ////============代码片段3-9 获取类别和参数的绑定============
        //BindingMap map = doc.ParameterBindings;
        //DefinitionBindingMapIterator dep = map.ForwardIterator();
        //while (dep.MoveNext())
        //{
        //    Definition definition = dep.Key;
        //    // 获取参数定义的基本信息
        //    string definitionName = definition.Name;
        //    ParameterType parameterType = definition.ParameterType;
        //    // 几乎都可以转型为InstanceBinding，笔者没有碰到过其他情况，如有例外，请联系我们。
        //    InstanceBinding instanceBinding = dep.Current as InstanceBinding;
        //    if (instanceBinding != null)
        //    {
        //        // 获取绑定的类别列表
        //        CategorySet categorySet = instanceBinding.Categories;
        //    }
        //}

        //1007 参照平面新建方法，只能在族文件按里操作？而且不可见，来自bim_er的csdn
        //public void CreatReferencePlane()
        //{
        //Document doc = this.ActiveUIDocument.Document;
        //if (!doc.IsFamilyDocument)
        //    return;
        //using (Transaction transaction = new Transaction(doc, "Editing Family"))
        //{
        //    transaction.Start();
        //    XYZ bubbleEnd = new XYZ(0, 5, 5);
        //    XYZ freeEnd = new XYZ(5, 5, 5);
        //    XYZ cutVector = XYZ.BasisY;
        //    View view = doc.ActiveView;
        //    ReferencePlane referencePlane = doc.FamilyCreate.NewReferencePlane(bubbleEnd, freeEnd, cutVector, view);
        //    //referencePlane.Name = "MyReferencePlane";
        //    transaction.Commit();
        //}
        //}

        //1007 视图检验
        //bool yn=activeView.HasViewDiscipline();
        //ViewType vt = activeView.ViewType;
        //TaskDialog.Show("CACC", vt.ToString());

        //1007 材质相关构件，数量统计和查找

        //List<Material> materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
        //// 将Element类型转换为Material类型,等同上句
        //HashSet<string> materialClassSet = new HashSet<string>();
        //foreach (var material in materials)
        //{
        //    string materialClassName = material.MaterialClass.ToString();
        //    // 尝试添加materialClass到HashSet中，如果它不存在，它将成功添加
        //    materialClassSet.Add(materialClassName);
        //}

        //// 如果需要，可以将HashSet转换回List或其他集合类型
        //List<string> uniqueMaterialClasses = new List<string>(materialClassSet);

        //TaskDialog.Show("CACC", material.Id.IntegerValue.ToString());
        //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(Material));
        //StringBuilder stringBuilder =new StringBuilder();
        //foreach (Element element in elems)
        //{
        //    Material material = element as Material;
        //    string t0 = material.MaterialCategory.ToString();//可以通过此方式获取材质Category
        //    //string t1 = material.ThermalAssetId.IntegerValue.ToString();
        //    //string t2= material.AppearanceAssetId.IntegerValue.ToString();
        //    //string t3= material.StructuralAssetId.IntegerValue.ToString();
        //    string t4= material.MaterialClass.ToString();//可以通过此方式获取材质Class
        //    stringBuilder.Append(t0 + "\n");
        //    //TaskDialog.Show("CACC", t1);
        //    //    //stringBuilder.Append(material.Name.ToString() + "\n");
        //    //    ElementId StructAssetId = material.StructuralAssetId;
        //    //    PropertySetElement pse = doc.GetElement(StructAssetId) as PropertySetElement;
        //    //    StructuralAsset asset = pse.GetStructuralAsset();
        //    //    //if (asset.Behavior == StructuralBehavior.Isotropic)
        //    //    //{
        //    //    //    StructuralAssetClass assetClass = asset.StructuralAssetClass;

        //    //    //    //TaskDialog.Show("CACC", assetClass.GetName().FirstOrDefault().ToString());
        //    //    //}
        //    //    //break;
        //}
        //TaskDialog.Show("CACC", stringBuilder.ToString());

        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
        //Element element = doc.GetElement(reference);
        //TaskDialog.Show("test2", element.Name + element.Id);
        //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(Material));
        //TaskDialog.Show("CACC", "材质梳理，数量=" + elems.Count().ToString());
        //IList<Element> elems = new FilteredElementCollector(doc).OfClass(typeof(Material)).ToList();
        //Element materialSample = elems.FirstOrDefault();



        ////几何生成例程 0518 寻找交点坐标
        ////没有改模型实际上无需事务
        ////var point1 = new XYZ(2, 0, 0);
        ////var point2 = new XYZ(0, 2, 0);
        ////var point3 = new XYZ(3, 3, 0);
        ////var line1 = Line.CreateBound(point1, point2);
        ////var line2 = Line.CreateBound(XYZ.Zero, point3);
        ////IntersectionResultArray results;
        ////var result = line1.Intersect(line2, out results);
        ////if (result == SetComparisonResult.Overlap)
        ////{ 
        ////    var point = results.get_Item(0).XYZPoint;
        ////    TaskDialog.Show("BIMBOX",point.ToString());
        ////}
        ////例程结束
        ////建立几何拉伸体 0518
        ////var tol = commandData.Application.Application.ShortCurveTolerance;// 防止过短线
        ////var point1 = new XYZ(0, 0, 0);
        ////var point2 = new XYZ(5, 0, 0);
        ////var point3 = new XYZ(5, 8, 0);
        ////var point4 = new XYZ(0, 8, 0);
        ////var line1 = Line.CreateBound(point1, point2);
        ////var line2 = Line.CreateBound(point2, point3);
        ////var line3 = Line.CreateBound(point3, point4);
        ////var line4 = Line.CreateBound(point4, point1);
        ////var curveLoop = new CurveLoop();
        ////curveLoop.Append(line1);
        ////curveLoop.Append(line2);
        ////curveLoop.Append(line3);
        ////curveLoop.Append(line4);
        ////var transform = Transform.CreateTranslation(new XYZ(5, 5, 0));
        ////curveLoop.Transform(transform);
        ////var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { curveLoop }, XYZ.BasisZ, 10);
        ////var ts = new Transaction(doc, "几何体创建");
        ////ts.Start();
        ////var shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
        ////shape.SetShape(new GeometryObject[] { solid });
        //////几何完成，以下添加属性,需补充guid
        //////var schemabuilder = new SchemaBuilder(_schemaGuid);
        ////var schema = Schema.Lookup(_schemaGuid);//检查是否存在，避免重复
        ////if (schema == null)
        ////{
        ////    var schemabuilder = new SchemaBuilder(_schemaGuid);
        ////    schemabuilder.SetReadAccessLevel(AccessLevel.Public);
        ////    schemabuilder.SetWriteAccessLevel(AccessLevel.Public);
        ////    schemabuilder.SetSchemaName("cacc");
        ////    schemabuilder.SetDocumentation("UniqueTag");
        ////    var filedBuilder = schemabuilder.AddSimpleField("name", typeof(string));
        ////    //定义数据
        ////    schema = schemabuilder.Finish();
        ////}
        ////var entity = new Entity(schema);
        ////var name = schema.GetField("name");
        ////entity.Set(name, "cacc_BIM"); // 属性赋值
        ////shape.SetEntity(entity);
        ////var dataStorageList = from element in new FilteredElementCollector(doc).OfClass(typeof(DataStorage))
        ////                      let storage = element as DataStorage
        ////                      where storage.GetEntitySchemaGuids().Contains(_schemaGuid)
        ////                      select storage;
        ////var dataStorage = dataStorageList.FirstOrDefault();
        ////if (dataStorage == null)
        ////{
        ////    dataStorage = DataStorage.Create(doc);//建立文档级别新类型数据
        ////    dataStorage.SetEntity(entity);
        ////}
        ////var dataEntity = dataStorage.GetEntity(schema);
        ////var field = dataEntity.Schema.GetField("name");
        ////var result = dataEntity.Get<string>(field);
        ////TaskDialog.Show("CACC", "名字叫：" + result);
        ////ts.Commit();
        ////例程结束

        ////平面视图，文字，标注样例，应用模板 0518 基于自带建筑样例文件
        //try
        //{
        //    #region 创建楼层平面
        //    var ts = new Transaction(doc, "创建图纸");
        //    ts.Start();
        //    var level = Level.Create(doc, 1);//1英尺位置创建标高
        //    var viewTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
        //                       let type = element as ViewFamilyType
        //                       where type.ViewFamily == ViewFamily.FloorPlan
        //                       select type;
        //    var viewTypeId = viewTypeList?.FirstOrDefault()?.Id;//加上？增加空值判断
        //    if (viewTypeId == null) throw new Exception("没有找到楼层平面");
        //    var viewPlan = ViewPlan.Create(doc, viewTypeId, level.Id);
        //    #endregion

        //    #region 视图中增加文字，先创建类型
        //    TextNoteType newTextNoteType;
        //    var textFamilyName = "3.5mm ST";
        //    var textNoteTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(TextNoteType))
        //                           let type = element as TextNoteType
        //                           where type.FamilyName=="文字" && type.Name==textFamilyName 
        //                           select type;
        //    if (textNoteTypeList.Count() > 0) 
        //        newTextNoteType = textNoteTypeList.FirstOrDefault();
        //    else
        //    {
        //        textNoteTypeList = from element in new FilteredElementCollector(doc).OfClass(typeof(TextNoteType))
        //                           let type = element as TextNoteType
        //                           where type.FamilyName == "文字" 
        //                           select type;
        //        var textNoteType = textNoteTypeList.FirstOrDefault();
        //        newTextNoteType = textNoteType.Duplicate(textFamilyName) as TextNoteType; //复制类型方法
        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(3.5 / 304.8);
        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_FONT).Set("宋体");
        //        newTextNoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);// 设置为透明背景
        //    }
        //    #endregion

        //    #region 创建文字
        //    var option = new TextNoteOptions();
        //    option.HorizontalAlignment=HorizontalTextAlignment.Center;
        //    option.TypeId = newTextNoteType.Id;
        //    var textNote = TextNote.Create(doc, viewPlan.Id, new XYZ(0, 0, 0), viewPlan.Name, option);
        //    #endregion

        //    #region 应用视图样板 //查找，复制
        //    var viewTemplateList = from element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan))
        //                           let view = element as ViewPlan
        //                           where view.IsTemplate && view.Name == "Architectural"
        //                           select view;
        //    var viewTemplate = viewTemplateList?.FirstOrDefault();
        //    if (viewTemplate == null) throw new Exception("没有找到视图样板");
        //    viewPlan.ViewTemplateId = viewTemplate.Id;
        //    #endregion
        //    #region 标注墙对象示例
        //    #endregion
        //}
        //catch (Exception ex)
        //{
        //    TaskDialog.Show("错误报告", ex.Message);
        //}
        ////例程结束

        //选单个物体
        //Reference reference = uiDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
        //Element element = document.GetElement(reference);
        //TaskDialog.Show("test2", element.Name+element.Id);
        //选多个物体，加过滤器
        //IList<Reference> references = uiDocument.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new DoorFilter());
        //StringBuilder stringBuilder = new StringBuilder();
        //foreach (Reference reference2 in references)
        //{
        //    Element element2 = document.GetElement(reference2);
        //    stringBuilder.AppendLine(element2.Name + "\t\t" + element2.Id);
        //}
        //TaskDialog.Show("test3", stringBuilder.ToString());
        //选点
        //XYZ xYZ  = uiDocument.Selection.PickPoint();
        //TaskDialog.Show("test3", xYZ+"");//显示点三维坐标（x,y,z）
        //Transaction transaction = new Transaction(document,"新事务");

        //兼容先选择或后选择的写法
        //List<ElementId> elemIds = uiDoc.Selection.GetElementIds().ToList();
        ////过滤已有选集中不符合要求的对象
        //for (int i = 0; i < elemIds.Count; i++)
        //{
        //    ElementId id = elemIds[i];
        //    //示例，仅保留墙体
        //    if(!(uiDoc.Document.GetElement(id) is Wall))
        //        elemIds.Remove(id);
        //}
        ////如果选集为空，命令选择对象
        //if (elemIds.Count == 0)
        //{
        //    IList<Reference> refers = new List<Reference>();
        //    //需要补充WallSelectionFilter 定义，
        //    WallSelectionFilter wallFilter = new WallSelectionFilter();
        //    try
        //    {
        //        refers = uiDoc.Selection.PickObjects(ObjectType.Element);
        //    }
        //    catch 
        //    {
        //        return Result.Succeeded; //中断命令退出是否不应该用success？？
        //    }
        //    //将用户选择对象加入选集
        //    foreach (Reference  refer in refers )
        //    {
        //        //前面如果没有WallSelectionfilter限制，此处还应做一次判断
        //        elemIds.Add(refer.ElementId);
        //    }
        //    //执行功能代码
        //    return Result.Succeeded;    
        //}

        //Material material;
        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass(), "选择对象");
        //CableTray cableTray = doc.GetElement(reference) as CableTray;

        ////0508 计算过滤所有型号为1200x1500，且标记为小于5的窗数量。识别并计算门窗总和
        //FilteredElementCollector collector = new FilteredElementCollector(doc);
        ////过滤出所有窗
        //collector = collector.OfCategory(BuiltInCategory.OST_Windows).OfClass(typeof(FamilySymbol));
        //var query = from element in collector
        //            where element.Name == "1200x1500mm"
        //            select element;
        //List<Autodesk.Revit.DB.Element> selWin = query.ToList();
        //ElementId symbolId = selWin[1].Id;//选择不同family中特定名称的类型

        //FamilyInstanceFilter filTilter = new FamilyInstanceFilter(doc, symbolId);
        //FilteredElementCollector c1 = new FilteredElementCollector(doc);
        //ICollection<Element> found = c1.WherePasses(filTilter).ToElements();            
        ////查找特定属性代码有点low
        //ElementId ruleId = new ElementId(-1001200);
        //FilterRule fr = ParameterFilterRuleFactory.CreateLessRule(ruleId, "5", true);//确定规则
        //ElementParameterFilter pFilter =  new ElementParameterFilter(fr); 
        //FilteredElementCollector c2 = new FilteredElementCollector(doc);
        //c2 = c2.OfCategory(BuiltInCategory.OST_Windows).WherePasses(filTilter).WherePasses(pFilter);
        ////LogicalOrFilter应用
        //ElementCategoryFilter doorFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
        //ElementCategoryFilter windowFilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
        //LogicalOrFilter lFilter = new LogicalOrFilter(doorFilter,windowFilter);
        //FilteredElementCollector c3 = new FilteredElementCollector(doc);
        //ICollection<Element> fds = c3.OfClass(typeof(FamilyInstance)).WherePasses(lFilter).ToElements();
        ////输出元素数量统计
        //TaskDialog.Show("查找元素","已找到型号符合元素"+found.Count.ToString()+"个"
        //    +"\n"+"标记小于5"+c2.ToList().Count.ToString()+"个"+"\n"+"门窗总和为："+fds.Count.ToString());

        //0508 ElementIntersectsFilter冲突检测过滤器，需要开启事务
        //Transaction ts = new Transaction(doc, "碰撞检查");
        //ts.Start();
        //Selection select = uiDoc.Selection;
        //Reference reference = select.PickObject(ObjectType.Element, "选择需要检查对象");
        //Element column = doc.GetElement(reference);
        //FilteredElementCollector collect = new FilteredElementCollector(doc);
        ////创建元素间碰撞子类对象过滤器
        //ElementIntersectsElementFilter iFilter = new ElementIntersectsElementFilter(column, false);
        //collect.WherePasses(iFilter);
        //List<ElementId> exclude = new List<ElementId>();
        //exclude.Add(column.Id);
        //collect.Excluding(exclude);
        //List<ElementId> ids = new List<ElementId>();
        //select.SetElementIds(ids);
        //foreach (Element elem in collect)//注意这个集合用的不是ids，Element和ElemntId别弄混
        //{
        //    ids.Add(elem.Id);
        //}
        //select.SetElementIds(ids);
        //ts.Commit();
        ////检查出碰撞对象自动选择
    }

}
