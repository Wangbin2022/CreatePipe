namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //public class _0602Test1 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiapp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uiapp.Application;

    //        //从setting中取当前doc存在的categories
    //        // 从当前文档对象中取到Setting对象 对照要素表需逐项对应 预计6月底前完成，当前进度到 
    //        //Settings documentSettings = doc.Settings;
    //        //String prompt = "Number of all categories in current Revit document: " + documentSettings.Categories.Size + "\n";
    //        //// 用BuiltInCategory枚举值取到一个对应的Floor Category，打印其名字
    //        //Category Category = documentSettings.Categories.get_Item(BuiltInCategory.OST_FloorLocalCoordSys);
    //        //prompt += "Get category and show the name: ";
    //        //prompt += Category.Name;
    //        //TaskDialog.Show("Revit", prompt);

    //        //通过字符串划分传递修改项目属性 0619.OK
    //        //还需要判断分割数量，并转化为WPF界面
    //        //获取构件的属性并修改内置参数 0612
    //        /*TaskDialog.Show("Test", proj.OrganizationDescription);*/
    //        //读取到组织描述
    //        //string input = "天津机场三期改造指挥部55,天津机场新建场务 外场指挥中心业务用房建设,总图项目,2401,2024年7月,XX(阶段)";
    //        ////字符数量判断 0621
    //        ////char characterToCount = ','; //  计算字符'，'出现的次数
    //        ////int count = 0;
    //        ////int index = -1;
    //        ////while ((index = input.IndexOf(characterToCount, index + 1)) != -1)
    //        ////{
    //        ////    count++; // 每次找到字符，计数加1
    //        ////}
    //        ////if (count == 5)
    //        ////{
    //        ////    TaskDialog.Show("Revit", "字符串格式正确");
    //        ////}
    //        ////TaskDialog.Show("Revit", "字符串格式不正确");

    //        //string[] items = input.Split(',');
    //        //ProjectInfo proj = doc.ProjectInformation;
    //        //Autodesk.Revit.DB.Parameter projectDesigner = proj.get_Parameter(BuiltInParameter.PROJECT_ORGANIZATION_NAME);
    //        //Autodesk.Revit.DB.Parameter projectClient = proj.get_Parameter(BuiltInParameter.CLIENT_NAME);
    //        //Autodesk.Revit.DB.Parameter projectName = proj.get_Parameter(BuiltInParameter.PROJECT_NAME);
    //        //Autodesk.Revit.DB.Parameter buildingName = proj.get_Parameter(BuiltInParameter.PROJECT_BUILDING_NAME);
    //        //Autodesk.Revit.DB.Parameter projectNumber = proj.get_Parameter(BuiltInParameter.PROJECT_NUMBER);
    //        //Autodesk.Revit.DB.Parameter issueDate = proj.get_Parameter(BuiltInParameter.PROJECT_ISSUE_DATE);
    //        //Autodesk.Revit.DB.Parameter projectStatus = proj.get_Parameter(BuiltInParameter.PROJECT_STATUS);
    //        //try
    //        //{
    //        //    using (Transaction ts = new Transaction(doc, "修改项目信息"))
    //        //    {
    //        //        ts.Start();
    //        //        projectDesigner.Set("CADC");
    //        //        projectClient.Set(items[0]);
    //        //        projectName.Set(items[1]);
    //        //        buildingName.Set(items[2]);
    //        //        projectNumber.Set(items[3]);
    //        //        issueDate.Set(items[4]);
    //        //        projectStatus.Set(items[5]);
    //        //        ts.Commit();
    //        //    }
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    TaskDialog.Show("Title", ex.Message);
    //        //}
    //        //TaskDialog.Show("Revit", "已完成项目信息更新");
    //        ////程序结束

    //        //============代码片段2-19：使用Category ID 取到多选元素的内部类别 OST_
    //        //Element selectedElement = null;
    //        //IList<Reference> reference = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,"提示");
    //        ////多选后需要手动点finish有点烦
    //        //StringBuilder stringBuilder = new StringBuilder();
    //        //foreach (var item in reference) 
    //        //{
    //        //    Element element = doc.GetElement(item) as Element;
    //        //    selectedElement = element;
    //        //    Category category = selectedElement.Category;
    //        //    BuiltInCategory enumCategory = (BuiltInCategory)category.Id.IntegerValue;
    //        //    stringBuilder.Append(enumCategory.ToString()+"\n");
    //        //}
    //        //TaskDialog.Show("Title", stringBuilder.ToString());
    //        //过滤器的新旧对比 0616
    //        //传统写法
    //        //FilteredElementCollector elements1 = new FilteredElementCollector(doc);
    //        //ElementClassFilter classFilter = new ElementClassFilter(typeof(Wall));
    //        //elements1 = elements1.WherePasses(classFilter);
    //        ////现代写法，会列出所有symbol，instance。所以为减少后期处理麻烦，用传统方法似乎更好
    //        ////FilteredElementCollector filteredElements = new FilteredElementCollector(doc);
    //        ////IList<Element> elements1 = filteredElements.OfCategory(BuiltInCategory.OST_Walls).ToElements();
    //        //StringBuilder stringBuilder = new StringBuilder();
    //        //double totalLemngth = 0;
    //        //foreach (Wall wall in elements1) // 注意在现代写法中不通，应该是非示例对象导致
    //        //{
    //        //    // 获取墙类型“功能”参数，它用来指示墙是否为外墙。
    //        //    var functionParameter = wall.WallType.get_Parameter(BuiltInParameter.FUNCTION_PARAM);
    //        //    if (functionParameter != null && functionParameter.StorageType == StorageType.Integer)
    //        //    {
    //        //        if (functionParameter.AsInteger() == (int)WallFunction.Exterior)
    //        //        {
    //        //            stringBuilder.Append(wall.Id + "\n");
    //        //        }
    //        //    }
    //        //    //获取长度
    //        //    Parameter parameterLength = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
    //        //    if (parameterLength != null && parameterLength.StorageType == StorageType.Double)
    //        //    {
    //        //        double length = parameterLength.AsDouble();
    //        //        totalLemngth += length;
    //        //    }
    //        //}
    //        //TaskDialog.Show("Title", stringBuilder.ToString()+"\n"+"总长度："+totalLemngth.ToMillimeter());
    //        //注意此处的格式转换需要考虑版本区分
    //        //修改墙属性，注意获得-取属性和改值的关键操作
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterWallClass(), "选择墙");
    //        //Wall wall = doc.GetElement(reference) as Wall;
    //        //Parameter parameterBaseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
    //        //using (Transaction ts = new Transaction(doc, "修改1"))
    //        //{
    //        //    ts.Start();
    //        //    if (parameterBaseOffset != null && parameterBaseOffset.StorageType == StorageType.Double)
    //        //    {
    //        //        if (!parameterBaseOffset.IsReadOnly)
    //        //        {
    //        //            bool success = parameterBaseOffset.Set(10);
    //        //            if (!success)
    //        //            {
    //        //                //更新错误报告
    //        //            }
    //        //        }
    //        //        else
    //        //        {
    //        //            //参数是只读的
    //        //        }
    //        //        ts.Commit();
    //        //    }
    //        //}
    //        //共享参数新建和管理，似乎比较常用
    //        //============代码片段3-10 判断共享参数和项目参数============
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new FamilyInstanceFilterClass(),"选择元素");
    //        //FamilyInstance familyInstance = doc.GetElement(reference) as FamilyInstance;
    //        //IList<Autodesk.Revit.DB.Parameter> paras = familyInstance.GetOrderedParameters();
    //        ////TaskDialog.Show("Title", paras.Count().ToString());
    //        //StringBuilder stringBuilder1 = new StringBuilder();
    //        //StringBuilder stringBuilder2 = new StringBuilder();
    //        //int sx1 = 0;
    //        //int sx2 = 0;
    //        //foreach (Autodesk.Revit.DB.Parameter item in paras)
    //        //{
    //        //    InternalDefinition definition = item.Definition as InternalDefinition;
    //        //    bool isSharedParameter = item.IsShared;//共享参数
    //        //    bool isProjectParameter = definition.BuiltInParameter == BuiltInParameter.INVALID && !item.IsShared; //项目参数
    //        //    if (isSharedParameter)
    //        //    {
    //        //        stringBuilder1.Append(item.Id + "+");
    //        //        sx1 += 1;
    //        //    }
    //        //    else if (isProjectParameter)
    //        //    {
    //        //        stringBuilder2.Append(item.Id + "-");
    //        //        sx2 += 1;
    //        //    }
    //        //}
    //        ////TaskDialog.Show("Title", stringBuilder1.ToString() + "\n" + stringBuilder2.ToString());
    //        //TaskDialog.Show("Title", sx1.ToString() + "\n" + sx2.ToString()+ "属性总数量"+ paras.Count().ToString());
    //        ////似乎有遗漏和不准确的，需要单独判断，改天继续深化
    //        //Revit可定义选择窗口并关联操作，似乎应独立为方法 0616
    //        //TaskDialogResult result = TaskDialog.Show("Revit2020", "Yes to return Succeed," + "\n" + "No to cancel command.", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No); 
    //        //复制墙板等系统族类型 0616 似乎意义不大
    //        //Wall wall = doc.GetElement(new ElementId(892187)) as Wall;
    //        //WallType wallType = wall.WallType;
    //        //using (Transaction ts = new Transaction(doc, "修改1"))
    //        //{
    //        //    ts.Start();
    //        //    ElementType dupWallType = wallType.Duplicate(wallType.Name + "(Duplicated)");
    //        //    ts.Commit();
    //        //}
    //        //TaskDialog.Show("Title", "Done");
    //        //============代码片段3-28：元素在位批量编辑，未完成。部分内容还需要补充事件（在族里新建属性需要子事务配合）
    //        // 这里是自定义族实例，比如门，窗，桌子…
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new FamilyInstanceFilterClass(), "选择元素");
    //        //FamilyInstance familyInstance = doc.GetElement(reference) as FamilyInstance;
    //        //// 编辑族，拿到族文档
    //        //Document familyDoc = doc.EditFamily(familyInstance.Symbol.Family);
    //        //// 在族文档中添加一个新的参数
    //        //using (Transaction tran = new Transaction(doc, "Edit family Document."))
    //        //{
    //        //    tran.Start();
    //        //    string paramName = "MyParam ";
    //        //    familyDoc.FamilyManager.AddParameter(paramName, BuiltInParameterGroup.PG_TEXT, ParameterType.Text, false);
    //        //    tran.Commit();
    //        //}
    //        //// 将这些修改重新载入到工程文档中
    //        //Family loadedFamily = familyDoc.LoadFamily(doc, new ProjectFamilyLoadOption());
    //        //TaskDialog.Show("Title", "Done");
    //        //从拉伸体量找轮廓方法，跟几何内容很像，可参考 
    //        //void GetSketchFromExtrusion() 
    //        //{
    //        //    Extrusion extrusion = doc.GetElement(new ElementId(3388)) as Extrusion;
    //        //    SketchPlane sketchPlane = extrusion.Sketch.SketchPlane;
    //        //    CurveArrArray sketchProfile = extrusion.Sketch.Profile;
    //        //}
    //        //楼层过滤器
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //collector.WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_Levels));
    //        //collector.OfCategory(BuiltInCategory.OST_Levels);//与上句等同
    //        //
    //        //元素过滤器0617 用方法过滤所有Familysymbol（应该可以限定到专业范围）
    //        //TestElementClassFilter(doc);
    //        //void TestElementClassFilter(Document ddoc)
    //        //{
    //        //    FilteredElementCollector colllector = new FilteredElementCollector(ddoc);
    //        //    ElementClassFilter filter = new ElementClassFilter(typeof(FamilySymbol));
    //        //    ICollection<ElementId> ids = colllector.WherePasses(filter).ToElementIds();
    //        //    //Trace.WriteLine(String.Format("  Found {0} FamilySymbols.", ids.Count));
    //        //    //TaskDialog.Show("tt", "Found" + ids.Count + "FamilySymbols.");
    //        //    StringBuilder stringBuilder1 = new StringBuilder();
    //        //    foreach (ElementId id in ids) 
    //        //    {
    //        //        Element elem = doc.GetElement(id);
    //        //        stringBuilder1.Append(elem.Name);
    //        //    }
    //        //    TaskDialog.Show("tt", stringBuilder1.ToString());
    //        //}
    //        //元素过滤器0617 ，统计所有对象类型总数
    //        /// 使用ElementIsElementTypeFilter过滤元素
    //        /// </summary>
    //        //TestElementIsElementTypeFilter(doc);
    //        //void TestElementIsElementTypeFilter(Document ddoc)
    //        //{
    //        //    // 找到所有属于ElementType的元素（对象）
    //        //    FilteredElementCollector collector = new FilteredElementCollector(ddoc);
    //        //    ElementIsElementTypeFilter filter = new ElementIsElementTypeFilter();
    //        //    ICollection<ElementId> ids = collector.WherePasses(filter).ToElementIds();
    //        //    //Trace.WriteLine(String.Format("  Found {0} ElementTypes.", ids.Count));
    //        //    TaskDialog.Show("tt", "Found" + ids.Count + "ElementTypes.");
    //        //}
    //        //元素过滤器0617 
    //        //TestFamilySymbolFilter(doc);
    //        //void TestFamilySymbolFilter(Document ddoc)
    //        //{
    //        //    // 找到当前文档中族实例所对应的族类型
    //        //    FilteredElementCollector collector = new FilteredElementCollector(ddoc);
    //        //    ICollection<ElementId> famIds = collector.OfClass(typeof(Family)).ToElementIds();
    //        //    foreach (ElementId famId in famIds)
    //        //    {
    //        //        collector = new FilteredElementCollector(ddoc);
    //        //        FamilySymbolFilter filter = new FamilySymbolFilter(famId);
    //        //        int count = collector.WherePasses(filter).ToElementIds().Count;
    //        //        //Trace.WriteLine(String.Format("  {0} FamilySybmols belong to Family {1}.", count, famId.IntegerValue));
    //        //        TaskDialog.Show("tt", count + "FamilySybmols belong to Family"+ famId.IntegerValue);//循环每个族元素对应的symbol数量，慎用
    //        //    }
    //        //}
    //        //元素过滤器0617 ，排除过滤器
    //        //TestExclusionFilter(doc);
    //        //void TestExclusionFilter(Document xdoc)
    //        //{
    //        //    // 找到所有除族类型FamilySymbol外的元素类型ElementType
    //        //    FilteredElementCollector collector = new FilteredElementCollector(xdoc);
    //        //    ICollection<ElementId> excludes = collector.OfClass(typeof(FamilySymbol)).ToElementIds();

    //        //    // 创建一个排除族类型FamilySymbol的过滤器
    //        //    ExclusionFilter filter = new ExclusionFilter(excludes);
    //        //    ICollection<ElementId> ids = collector.WhereElementIsElementType().WherePasses(filter).ToElementIds();
    //        //    Trace.WriteLine(String.Format("  Found {0} ElementTypes which are not FamilySybmols", ids.Count));
    //        //    TaskDialog.Show("tt", "Found" + ids.Count + "ElementTypes which are not FamilySybmols");
    //        //}
    //        //查找当前标高对应z轴数值 0617
    //        //Level level = activeView.GenLevel;
    //        //TaskDialog.Show("tt", Convert.ToInt16(level.Elevation * 304.8).ToString());
    //        //查找所有标高名称和z值 0617
    //        //FilteredElementCollector filteredElements = new FilteredElementCollector(doc).OfClass(typeof(Level));
    //        //List<Level> levels = new List<Level>(filteredElements.Cast<Level>());
    //        //StringBuilder stringBuilder1 = new StringBuilder();
    //        //下面代码列出所有平面视图，方向错了但似乎可以有别的作用
    //        //FilteredElementCollector filteredElements = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).OfCategory(BuiltInCategory.OST_Views);
    //        //StringBuilder stringBuilder1 = new StringBuilder();
    //        //foreach (ViewPlan viewPlan in filteredElements)
    //        //{
    //        //    stringBuilder1.Append(viewPlan.Name + "\n");
    //        //}
    //        //TaskDialog.Show("tt", stringBuilder1.ToString());

    //        //判断立管 0618
    //        //foreach (Level level in levels)
    //        //{
    //        //    stringBuilder1.Append(level.Name + " " + Convert.ToInt16(level.Elevation * 304.8).ToString() + "\n");
    //        //    XYZ pp = new XYZ(startXYZ.X, startXYZ.Y, level.Elevation);
    //        //    BreakMEPCurveByOne(commandData, mEPCurve, pp);
    //        //}
    //        //上述方法失败，因为循环要修改打断对象的id，所以改查找本层所有立管并切断 0618
    //        //前面加判断必须在平面视图操作 有出现断开连接提示提示，是否可绕过？？
    //        //if (activeView.ViewType == ViewType.FloorPlan)
    //        //{
    //        //    FilteredElementCollector filteredElements = new FilteredElementCollector(doc, activeView.Id).OfClass(typeof(MEPCurve));
    //        //    List<MEPCurve> mEPCurves = new List<MEPCurve>(filteredElements.Cast<MEPCurve>());
    //        //    int mepcount1 = 0;
    //        //    int mepcount2 = 0;
    //        //    string versionName = commandData.Application.Application.VersionNumber.ToString();
    //        //    MEPCurveBreakSingle mEPCurveBreak = new MEPCurveBreakSingle();
    //        //    using (Transaction ts = new Transaction(doc, "按标高批量打断"))
    //        //    {
    //        //        ts.Start();
    //        //        foreach (var item in mEPCurves)
    //        //        {
    //        //            Curve curve = (item.Location as LocationCurve).Curve;
    //        //            XYZ startXYZ = curve.GetEndPoint(0);
    //        //            XYZ endXYZ = curve.GetEndPoint(1);
    //        //            double lvz = 0;
    //        //            if (versionName.StartsWith("202") && !versionName.Equals("2020"))
    //        //            {
    //        //                lvz = Lvz2021(commandData);
    //        //            }
    //        //            else
    //        //            {
    //        //                lvz = Lvz2020(commandData);  
    //        //            }
    //        //            int lvzInt = Convert.ToInt16(lvz);
    //        //            XYZ pp = new XYZ(startXYZ.X, startXYZ.Y, lvz);
    //        //            if (Convert.ToInt16(startXYZ.X) == Convert.ToInt16(endXYZ.X) && Convert.ToInt16(startXYZ.Y) == Convert.ToInt16(endXYZ.Y))
    //        //            {
    //        //                mepcount1 += 1;
    //        //                if ((Convert.ToInt16(startXYZ.Z) < lvzInt && Convert.ToInt16(endXYZ.Z) > lvzInt) || (Convert.ToInt16(endXYZ.Z) < lvzInt && Convert.ToInt16(startXYZ.Z) > lvzInt))
    //        //                {
    //        //                    mepcount2 += 1;
    //        //                    mEPCurveBreak.BreakMEPCurveByOne(commandData, item, pp); //引用打断管的现成方法
    //        //                }
    //        //            }
    //        //        }
    //        //        ts.Commit();
    //        //    }
    //        //    TaskDialog.Show("tt", "本层机电立管/桥架数量" + mepcount1 + "\n已打断" + mepcount2);
    //        //}
    //        //else
    //        //{
    //        //    TaskDialog.Show("tt", "请转到平面视图操作");
    //        //    return Result.Cancelled;
    //        //}
    //        //例程结束



    //        //rvt编辑事件记录功能 需要延迟开展 0615
    //        //取当前时间DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    //        //取修改模型次数docEditCount 关闭时取值+1并保存修改
    //        //取开启修改时间docCreatTime 先创建变量，在开启时取值
    //        //取本次模型开启时间docEditTimeStart  开始插件时取值
    //        //求模型累计修改时间docEditTimeDuration 关闭时计算并保存修改
    //        //TaskDialog.Show("Title", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

    //        //string timeStamp = 1655026814.ToString();
    //        //long lTime = long.Parse(timeStamp);
    //        //var DateTimeUnix = DateTimeOffset.FromUnixTimeSeconds(lTime);
    //        //TimeSpan timeSpan = new TimeSpan(DateTimeOffset.UtcNow.Ticks -DateTimeUnix.Ticks);
    //        //TaskDialog.Show("Test", proj.Name+DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()+"\n"+"项目已创建时间"+timeSpan);//读取到项目名称
    //        //涉及到DocumentClosing /DocumentClosed 应用，暂时能力达不到

    //        return Result.Succeeded;
    //    }
    //    public double Lvz2021(ExternalCommandData commandData)
    //    {
    //        BasePoint basePoint = BasePoint.GetProjectBasePoint(commandData.Application.ActiveUIDocument.Document);
    //        Autodesk.Revit.DB.View activeView = commandData.Application.ActiveUIDocument.ActiveView;
    //        return activeView.GenLevel.Elevation + basePoint.Position.Z;
    //    }
    //    public double Lvz2020(ExternalCommandData commandData)
    //    {
    //        FilteredElementCollector elements1 = new FilteredElementCollector(commandData.Application.ActiveUIDocument.Document);
    //        elements1.OfClass(typeof(BasePoint)).OfCategory(BuiltInCategory.OST_ProjectBasePoint);
    //        //elements1.OfClass(typeof(BasePoint)).OfCategory(BuiltInCategory.OST_SharedBasePoint);
    //        BasePoint basePoint = elements1.FirstOrDefault() as BasePoint;
    //        Autodesk.Revit.DB.View activeView = commandData.Application.ActiveUIDocument.ActiveView;
    //        return activeView.GenLevel.Elevation + basePoint.Position.Z;
    //    }
    //    public class ProjectFamilyLoadOption : IFamilyLoadOptions// 批量加载族用？？
    //    {
    //        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    //        {
    //            overwriteParameterValues = true; //覆盖现有族和类型？？
    //            return true;
    //        }

    //        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    //        {
    //            source = FamilySource.Project;//这个来源怎么查看？？
    //            overwriteParameterValues = true;
    //            return true;
    //        }
    //    }
    //    //public class failure_ignore : IFailuresPreprocessor
    //    //{
    //    //    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    //    //    {
    //    //        failuresAccessor.DeleteAllWarnings();
    //    //        //failuresAccessor.DeleteElements(failuresAccessor.el);
    //    //        return FailureProcessingResult.Continue;
    //    //    }
    //    //}

    //    //public class InaccurateFailureProcessor : IFailuresPreprocessor
    //    //{
    //    //    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    //    //    {
    //    //        var failList = failuresAccessor.GetFailureMessages();
    //    //        foreach (var item in failList)
    //    //        {
    //    //            var failureId = item.GetFailureDefinitionId();
    //    //            if (failureId == BuiltInFailures.InaccurateFailures.InaccurateBeamOrBrace)
    //    //                failuresAccessor.DeleteWarning(item);
    //    //        }
    //    //        return FailureProcessingResult.Continue;
    //    //    }
    //    //}
    //}
}
