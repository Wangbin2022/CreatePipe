namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //public class _0619Test2 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiapp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uiapp.Application;

    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass(), "选择对象");
    //        ////CableTray cableTray = doc.GetElement(reference) as CableTray;
    //        //Duct duct = doc.GetElement(reference) as Duct;
    //        //Parameter parameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
    //        //TaskDialog.Show("tt", "风管宽度=" + parameter.AsValueString());

    //        //遍历获取所选图元参数0701
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "选择对象");
    //        //Element element = doc.GetElement(reference);
    //        //GetElementParamenterInformation(doc,element);
    //        //例程结束，意义不大



    //        //
    //        //结构柱过滤器可用 0621.OK 后续可复用
    //        //Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new ColumnFilter(), "选择对象");
    //        //ElementId elementId = doc.GetElement(reference).Id;
    //        //TaskDialog.Show("Title", elementId.ToString());
    //        //分支选择，删除前确认 0621.OK 后续可复用
    //        //TaskDialogResult result = TaskDialog.Show("Revit","Yes to 删除" + "No to cancel",TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
    //        //if (result == TaskDialogResult.Yes)
    //        //    TaskDialog.Show("tt","OK");
    //        //if (result == TaskDialogResult.No)
    //        //{
    //        //    TaskDialog.Show("tt", "NO");
    //        //}
    //        //获取时间戳并粘贴到剪贴板 0621.OK
    //        //string timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    //        //Clipboard.SetText(timeStamp);
    //        //TaskDialog.Show("tt", timeStamp);
    //        //例程结束

    //        //批量链接RVT 0619.OK 需helper配合
    //        //var opdg = new OpenFileDialog();
    //        //opdg.Multiselect = true;
    //        //opdg.Filter = "(*.rvt)|*.rvt";
    //        //var dialogresult = opdg.ShowDialog();
    //        //var count = opdg.FileNames.Length;
    //        //string[] files = new string[count];

    //        //if (dialogresult == true)
    //        //{
    //        //    files = opdg.FileNames;
    //        //}
    //        //doc.Invoke(m =>
    //        //{
    //        //    foreach (var file in files)
    //        //    {
    //        //        var linktypeId = CreateRevitLink(doc, file);
    //        //        CreateLinkInstances(doc, linktypeId);
    //        //    }
    //        //}, "批量链接");
    //        //程序结束

    //        //批量导入dwg 0619。OK
    //        //DWGImportOptions options = new DWGImportOptions();
    //        //options.Placement = Autodesk.Revit.DB.ImportPlacement.Origin;//插入到原点
    //        //options.OrientToView = true;
    //        //ElementId elementId = null;
    //        //var opdg = new Microsoft.Win32.OpenFileDialog();
    //        //opdg.Multiselect = true;
    //        //opdg.Filter = "(*.dwg)|*.dwg";
    //        //var dialogresult = opdg.ShowDialog();
    //        //var count = opdg.FileNames.Length;
    //        //string[] files = new string[count];

    //        ////private ImportUnit  m_importUnit;
    //        //options.Unit=ImportUnit.Millimeter;

    //        //if (dialogresult == true)
    //        //{
    //        //    files = opdg.FileNames;
    //        //}
    //        //doc.Invoke(m =>
    //        //{
    //        //    foreach (var file in files)
    //        //    {
    //        //        //doc.Import(file, options, activeView, out elementId);//插入dwg
    //        //        doc.Link(file, options, activeView, out elementId);//链接dwg
    //        //    }
    //        //}, "批量导入dwg");

    //        //FilteredElementCollector elements1 = new FilteredElementCollector(doc);
    //        //ElementClassFilter classFilter = new ElementClassFilter(typeof(ImportInstance));
    //        //elements1 = elements1.WherePasses(classFilter);
    //        ////StringBuilder stringBuilder = new StringBuilder();
    //        //Transaction trans = new Transaction(doc, "解锁链接");
    //        //trans.Start();
    //        //foreach (var item in elements1)
    //        //{
    //        //    //stringBuilder.Append(item.Id+"\n");
    //        //    item.Pinned = false;
    //        //}
    //        //trans.Commit();
    //        //TaskDialog.Show("tt", "已导入并解锁" + count + "个对象");
    //        //程序结束
    //        //设置半色调 0619.OK
    //        //FilteredElementCollector eIems1 = new FilteredElementCollector(doc);
    //        //FilteredElementCollector eIems2 = new FilteredElementCollector(doc);
    //        //ElementClassFilter classFilter1 = new ElementClassFilter(typeof(ImportInstance));            
    //        //ElementCategoryFilter categoryFilter2 = new ElementCategoryFilter(BuiltInCategory.OST_RvtLinks);
    //        //eIems1 =eIems1.WherePasses(classFilter1);
    //        //eIems2 =eIems2.WherePasses(categoryFilter2);
    //        //Transaction trans = new Transaction(doc, "开关链接半色调");
    //        //trans.Start();
    //        //LinkInstanceHalftone(doc, eIems1);
    //        //LinkInstanceHalftone(doc, eIems2);
    //        //trans.Commit();
    //        //TaskDialog.Show("tt", "命令已完成");
    //        //程序结束 
    //        //FilteredElementCollector eIems1 = new FilteredElementCollector(doc);
    //        //FilteredElementCollector eIems2 = new FilteredElementCollector(doc);
    //        //ElementClassFilter classFilter1 = new ElementClassFilter(typeof(ImportInstance));
    //        //ElementCategoryFilter categoryFilter2 = new ElementCategoryFilter(BuiltInCategory.OST_RvtLinks);
    //        //eIems1 = eIems1.WherePasses(classFilter1);
    //        //eIems2 = eIems2.WherePasses(categoryFilter2);
    //        //Transaction trans = new Transaction(doc, "删除链接");
    //        //trans.Start();
    //        ////LinkInstanceRemove(doc, eIems1);
    //        //LinkInstanceRemove(doc, eIems2);
    //        //trans.Commit();
    //        //批量删除DWG。唐僧
    //        //var collector = new FilteredElementCollector(doc);
    //        //var elementFilter1 = new ElementClassFilter(typeof(ImportInstance));
    //        //var elementFilter2 = new ElementClassFilter(typeof(CADLinkType));
    //        //var orFilter = new LogicalOrFilter(elementFilter1, elementFilter2);
    //        //var cadsCollector = collector.WherePasses(orFilter);
    //        //doc.Invoke(m =>
    //        //{
    //        //    doc.Delete(cadsCollector.Select(n =>n.Id).ToList());
    //        //}, "批量删除dwg");

    //        //以下代码有问题，会报错，删的不是实体而是类别
    //        //var collector = new FilteredElementCollector(doc);
    //        //var elementFilter1 = new ElementClassFilter(typeof(RevitLinkInstance));
    //        //ElementCategoryFilter categoryFilter2 = new ElementCategoryFilter(BuiltInCategory.OST_RvtLinks);
    //        //collector = collector.WherePasses(elementFilter1);
    //        //Transaction trans = new Transaction(doc, "删除链接");
    //        //trans.Start();
    //        //foreach (var item in collector)
    //        //{
    //        //    doc.Delete(item.Id);
    //        //}
    //        //trans.Commit();
    //        //TaskDialog.Show("tt", "命令已完成"+collector.Count());

    //        //列举内置类别名称并生成文本到剪贴板 0620
    //        // All BuiltInCategory objects as array
    //        //var valueList = Enum.GetValues(typeof(BuiltInCategory));
    //        //// String to store all values
    //        //string data = "name,hashcode\n";
    //        //// Iterate through the array and add comma separated values
    //        //foreach (BuiltInCategory value in valueList)
    //        //{
    //        //    data += value.ToString() + "," + value.GetHashCode().ToString() + "\n";
    //        //}
    //        //// Copy the full CSV to clipboard (to easily paste in text editor)生成到剪贴板
    //        //System.Windows.Forms.Clipboard.SetText(data.ToString());
    //        // Debug: Display text in Revit Dialog
    //        //TaskDialog.Show("Test", data.ToString());

    //        //从FamilySymbol找出所有特定标记 0620
    //        //FilteredElementCollector colllector = new FilteredElementCollector(doc);
    //        ////ElementClassFilter filter = new ElementClassFilter(typeof(FamilyInstance));
    //        ////ElementClassFilter filter = new ElementClassFilter(typeof(FamilySymbol));
    //        ////ElementClassFilter filter = new ElementClassFilter(typeof(Family));
    //        ////ElementClassFilter filter = new ElementClassFilter(typeof(Autodesk.Revit.DB.Ceiling));
    //        //ElementClassFilter filter = new ElementClassFilter(typeof(Autodesk.Revit.DB.Floor));
    //        //ICollection<ElementId> ids = colllector.WherePasses(filter).ToElementIds();
    //        //StringBuilder stringBuilder1 = new StringBuilder();
    //        //int symbolCount1 = 0;
    //        //int symbolCount2 = 0;
    //        //foreach (ElementId id in ids)
    //        //{
    //        //    Element elem = doc.GetElement(id);
    //        //    symbolCount1 += 1;
    //        //    string symbolName = elem.Name.ToString();
    //        //    bool isStartWithAAAAA = symbolName.StartsWith("CADC_");
    //        //    if (isStartWithAAAAA)
    //        //    {
    //        //        stringBuilder1.Append(elem.Id + "\n"); //stringBuilder1.ToString()
    //        //        symbolCount2 += 1;
    //        //    }
    //        //}
    //        //TaskDialog.Show("tt", symbolCount1.ToString() + "\n" + symbolCount2.ToString() + "\n" + stringBuilder1);
    //        //程序结束


    //        // 获取当前DLL的完整路径
    //        //string assemblyLocation = Assembly.GetExecutingAssembly().Location;
    //        //// 获取DLL所在的目录
    //        //string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
    //        //// 构造PDF文件的完整路径
    //        //string PdfFileName = "CADC_Addin_Readme.pdf";
    //        //string pdfPath = Path.Combine(assemblyDirectory, PdfFileName);
    //        //// 检查PDF文件是否存在
    //        //if (File.Exists(pdfPath))
    //        //{
    //        //    // 使用默认的PDF查看器打开文件
    //        //    Process.Start(pdfPath);
    //        //}
    //        //else
    //        //{
    //        //    //MessageBox.Show("PDF文件未找到。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //        //    TaskDialog.Show("tt", "PDF文件未找到");
    //        //}
    //        //程序结束


    //        //删除结构分析模型属性 0620 原理通过但有bug，选择加入非结构对象会报错,如group等        
    //        //ElementId id0 = new ElementId(-2001300);//基础id
    //        //ElementId id1 = new ElementId(-2001320);//梁id
    //        //ElementId id2 = new ElementId(-2001330);//柱id
    //        //int elemCount = 0;

    //        //Transaction trans = new Transaction(doc, "取消计算模型");
    //        //trans.Start();
    //        //elemCount = RemoveAnalyticalModel(doc, id0, elemCount) + RemoveAnalyticalModel(doc, id1, elemCount) + RemoveAnalyticalModel(doc, id2, elemCount);
    //        //trans.Commit();

    //        //TaskDialog.Show("tt", "已修改" + elemCount.ToString());

    //        //以下代码有误，注意不可从AnalyticalModel直接改
    //        //FamilyInstance familyInstance =  doc.GetElement(new ElementId(325143)) as FamilyInstance;
    //        ////TaskDialog.Show("tt",familyInstance.StructuralUsage.ToString());//结构作用分类enum：column,other(斜梁、基础)undefined（梁？），墙板均无此属性
    //        ////TaskDialog.Show("tt",familyInstance.StructuralType.ToString());//结构类型enum：column,footing基础，Beam梁，墙板均无此属性
    //        //if (familyInstance.StructuralType == StructuralType.Footing)
    //        //{                 
    //        //    //AnalyticalModel analyticalModel = familyInstance.GetAnalyticalModel();
    //        //    //bool isEnabled = analyticalModel.IsEnabled();
    //        //    //familyInstance.CanHaveAnalyticalModel(false);
    //        //    ////analyticalModel.Enable(false);
    //        //    ////analyticalModel.IsEnabled = false;
    //        //    ////bool isEnabled = analyticalModel.IsEnabled();
    //        //    //TaskDialog.Show("tt", isEnabled.ToString());
    //        //};

    //        //结构柱，结构框架图元（梁和支撑），结构独立基础，结构楼板，结构墙GetAnanlyticalModel会把门窗等搞进来
    //        ////新建过滤器
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        ////获取类别为OST_StructuralFraming的实例
    //        //collector.OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming);
    //        //IList<Element> beamList = collector.ToElements();

    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //collector.OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns);
    //        //IList<Element> columnList = collector.ToElements();

    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //collector.OfClass(typeof(Wall)).OfCategory(BuiltInCategory.OST_Walls);
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //collector.OfClass(typeof(Floor)).OfCategory(BuiltInCategory.OST_Floors);



    //        return Result.Succeeded;
    //    }
    //    //private const string PdfFileName = "example.pdf";

    //    //遍历获取所选图元参数0701
    //    void GetElementParamenterInformation(Document document, Element element)
    //    {
    //        String prompt = "Show Parameters in selected Element";
    //        //string prompt = null;
    //        StringBuilder st = new StringBuilder();
    //        foreach (Parameter para in element.Parameters)
    //        {
    //            st.AppendLine(GetParameterInformation(para, document));
    //        }
    //        //MessageBox.Show(prompt, "Revit", MessageBoxButtons.OK);
    //        MessageBox.Show(st.ToString(), "Revit", MessageBoxButtons.OK);
    //    }

    //    String GetParameterInformation(Parameter para, Document document)
    //    {
    //        string defName = para.Definition.Name + @"\t";
    //        switch (para.StorageType)
    //        {
    //            case StorageType.Double:
    //                defName += ":" + para.AsValueString();
    //                break;
    //            case StorageType.ElementId:
    //                ElementId id = para.AsElementId();
    //                if (id.IntegerValue > 0)
    //                {
    //                    defName += ":" + document.GetElement(id).Name;
    //                }
    //                else
    //                    defName += ":" + id.IntegerValue.ToString();
    //                break;
    //            case StorageType.Integer:
    //                if (ParameterType.YesNo == para.Definition.ParameterType)
    //                {
    //                    if (para.AsInteger() == 0)
    //                    {
    //                        defName += ":" + "False";
    //                    }
    //                    else
    //                    {
    //                        defName += ":" + "True";
    //                    }
    //                }
    //                else
    //                {
    //                    defName += ":" + para.AsInteger().ToString();
    //                }
    //                break;

    //            case StorageType.String:
    //                defName += ":" + para.AsString();
    //                break;
    //            default:
    //                defName = "Unexpected parameter";
    //                break;
    //        }
    //        return defName;
    //    }

    //    public int RemoveAnalyticalModel(Document doc, ElementId id, int count)
    //    {
    //        FilteredElementCollector colllector = new FilteredElementCollector(doc);
    //        ICollection<ElementId> ids = colllector.OfClass(typeof(FamilyInstance)).OfCategoryId(id).ToElementIds();
    //        foreach (ElementId eId in ids)
    //        {
    //            Element element = doc.GetElement(eId) as Element;
    //            bool parameter = element.get_Parameter(BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL).Set(0);
    //            count++;
    //        }
    //        return count;
    //    }
    //    public void LinkInstanceRemove(Document ddoc, FilteredElementCollector eIds)
    //    {
    //        foreach (var item in eIds)
    //        {
    //            ElementId eid = item.Id;
    //            ddoc.Delete(eid);
    //        }
    //    }
    //    public void LinkInstanceHalftone(Document ddoc, FilteredElementCollector eIds)
    //    {
    //        foreach (var item in eIds)
    //        {
    //            ElementId eid = item.Id;
    //            if (ddoc.ActiveView.GetElementOverrides(eid).Halftone == true)
    //            {
    //                OverrideGraphicSettings ORGS = new OverrideGraphicSettings();
    //                ORGS.SetHalftone(false);
    //                ddoc.ActiveView.SetElementOverrides(eid, ORGS);
    //            }
    //            else if (ddoc.ActiveView.GetElementOverrides(eid).Halftone == false)
    //            {
    //                OverrideGraphicSettings ORGS = new OverrideGraphicSettings();
    //                ORGS.SetHalftone(true);
    //                ddoc.ActiveView.SetElementOverrides(eid, ORGS);
    //            }
    //        }
    //    }
    //    public ElementId CreateRevitLink(Document doc, string pathName)
    //    {
    //        FilePath path = new FilePath(pathName);
    //        RevitLinkOptions options = new RevitLinkOptions(false);
    //        LinkLoadResult result = RevitLinkType.Create(doc, path, options);
    //        return (result.ElementId);
    //    }

    //    public void CreateLinkInstances(Document doc, ElementId linkTypeId)
    //    {
    //        RevitLinkInstance instance2 = RevitLinkInstance.Create(doc, linkTypeId);
    //    }
    //}

}
