using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.models;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test09_0617 : IExternalCommand
    {
        private static List<Connector> GetConnectors(Element element)
        {
            List<Connector> result = new List<Connector>();
            if (element == null) return null;
            try
            {
                FamilyInstance fi = element as FamilyInstance;
                if (fi != null && fi.MEPModel != null)
                {
                    foreach (Connector item in fi.MEPModel.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
                MEPSystem system = element as MEPSystem;
                if (system != null)
                {
                    foreach (Connector item in system.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
                MEPCurve duct = element as MEPCurve;
                if (duct != null)
                {
                    foreach (Connector item in duct.ConnectorManager.Connectors)
                    {
                        result.Add(item);
                    }
                    return result;
                }
            }
            catch (Exception)
            {

            }
            return null;
        }
        private bool isConnectedPipe(List<Connector> pipe)
        {
            bool result = false;
            foreach (var item in pipe)
            {
                if (item.IsConnected)
                {
                    result = true;
                }
            }
            return !result;
        }
        private ConnectorSet GetConnectorSet(FamilyInstance instance)
        {
            if (instance == null) return null;

            // Get the family instance MEPModel
            MEPModel mepModel = instance.MEPModel;
            if (mepModel == null) return null;

            return mepModel.ConnectorManager?.Connectors;
        }
        //0622找符合条件的最近元素 还是没太明白用法 2种用途，似乎还有更多
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="filter">类型</param>
        /// <param name="targetRef">目标（枚举）</param>
        /// <param name="center">射源</param>
        /// <param name="direction">发射方向</param>
        /// <param name="hitElement">被击中的元素</param>
        /// <returns></returns>
        public Line xRayFindNearest(Document doc, ElementFilter filter, FindReferenceTarget targetRef, XYZ center, XYZ direction, ref Element hitElement)
        {
            Line result = null;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Func<View3D, bool> isNotTemplate = v3 => !(v3.IsTemplate);
            View3D view3D = collector.OfClass(typeof(View3D)).Cast<View3D>().First<View3D>(isNotTemplate);
            ReferenceIntersector intersector = new ReferenceIntersector(filter, targetRef, view3D);
            ReferenceWithContext rwc = intersector.FindNearest(center, direction);
            if (rwc == null) return null;
            Reference reference = rwc.GetReference();
            XYZ intersection = reference.GlobalPoint;
            hitElement = doc.GetElement(reference);
            if (!(center.IsAlmostEqualTo(intersection)))
            {
                result = Line.CreateBound(center, intersection);
            }
            else hitElement = null;
            return result;
        }
        public IList<Element> xRayFindAll(Document doc, ElementFilter filter, FindReferenceTarget targetRef, XYZ center, XYZ direction)
        {
            IList<Element> result = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Func<View3D, bool> isNotTemplate = v3 => !(v3.IsTemplate);
            View3D view3D = collector.OfClass(typeof(View3D)).Cast<View3D>().First<View3D>(isNotTemplate);
            ReferenceIntersector intersector = new ReferenceIntersector(filter, targetRef, view3D);
            IList<ReferenceWithContext> refWithContext = intersector.Find(center, direction);
            if (refWithContext == null) return null;
            foreach (var rwc in refWithContext)
            {
                Reference reference = rwc.GetReference();
                Element hitElement = doc.GetElement(reference);
                if (hitElement != null)
                {
                    result.Add(hitElement);
                }
            }
            return result;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0620 房间过滤器
            //RoomManagerView roomManager = new RoomManagerView(uiApp);
            //roomManager.Show();

            ////0628 门窗类型过滤器
            //OpenningManagerView openningManagerView = new OpenningManagerView(uiApp);
            //openningManagerView.Show();

            ////0703检验 门窗立面放置 可以互动放置但不支持直接放，缺api支持
            //if (activeView.ViewType != ViewType.Legend) return Result.Failed;
            //FamilySymbol fsS = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
            //    .FirstOrDefault(x => x.Name == "W=0.7") as FamilySymbol;
            //FamilySymbol fsM = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
            //    .FirstOrDefault(x => x.Name == "W=1") as FamilySymbol;
            //FamilySymbol fsL = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
            //    .FirstOrDefault(x => x.Name == "W=1.25") as FamilySymbol;
            //uiDoc.PromptForFamilyInstancePlacement(fsL);

            ////0701 标高差距获取所有标高并按高程排序
            //var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
            //if (levels.Count < 2)
            //{
            //    TaskDialog.Show("提示", "模型中至少需要两个标高才能计算间距");
            //    return Result.Failed;
            //}
            //// 计算相邻标高间距并找出最大值
            //double maxSpacing = 0;
            //Level lowerLevel = null;
            //Level upperLevel = null;
            //for (int i = 0; i < levels.Count - 1; i++)
            //{
            //    double spacing = levels[i + 1].Elevation - levels[i].Elevation;
            //    if (spacing > maxSpacing)
            //    {
            //        maxSpacing = spacing;
            //        lowerLevel = levels[i];
            //        upperLevel = levels[i + 1];
            //    }
            //}
            //// 显示结果
            //string result = $"最大标高间距: {maxSpacing * 304.8:0.000} 米\n" + $"位于: {lowerLevel?.Name} 与 {upperLevel?.Name} 之间";
            //TaskDialog.Show("标高间距分析", result);

            ////0701 打开文档内部族方式
            //Document familyDoc = doc.EditFamily(family);
            //familyDoc.FamilyManager.Parameters.Equals.
            //实现打开族编辑窗口
            //FamilyInstance openningElement = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new OpenningFilter(), "Pick")) as FamilyInstance;
            //Family family = openningElement.Symbol.Family;
            //string tempDir = @"C:\temp";
            //if (!Directory.Exists(tempDir))
            //{
            //    Directory.CreateDirectory(tempDir);
            //}
            //// 生成唯一文件名
            //string familyName = family.Name;
            //string tempFilePath = Path.Combine(tempDir, $"{familyName}.rfa");
            //// 处理文件名冲突
            //int counter = 1;
            //while (File.Exists(tempFilePath))
            //{
            //    tempFilePath = Path.Combine(tempDir, $"{familyName}_{counter}.rfa");
            //    counter++;
            //}
            //// 保存族到临时文件
            //Document familyDoc = doc.EditFamily(family);
            //familyDoc.SaveAs(tempFilePath);
            //uiApp.OpenAndActivateDocument(tempFilePath);
            //TaskDialog.Show("成功", $"已打开族：{family.Name}");
            //例程结束
            //var familySymbols = new HashSet<ElementId>();
            //FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance));
            //FilteredElementCollector collector2 = new FilteredElementCollector(doc).OfClass(typeof(Family));
            //foreach (Element elem in collector2)
            //{
            //    Family family = elem as Family;
            //    if (family != null)
            //    {
            //        // 遍历族中的所有族符号
            //        foreach (ElementId elementId in family.GetFamilySymbolIds())
            //        {
            //            FamilySymbol symbol = doc.GetElement(elementId) as FamilySymbol;
            //            if (symbol.Category != null &&
            //                (symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors ||
            //                 symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows))
            //            {
            //                familySymbols.Add(symbol.Id);
            //            }
            //        }
            //    }
            //}
            //var usingFamilySymbols = new HashSet<ElementId>();
            //foreach (Element elem in collector)
            //{
            //    FamilyInstance instance = elem as FamilyInstance;
            //    if (instance != null && familySymbols.Contains(instance.Symbol.Id))
            //    {
            //        // 检查是否已经添加过这个FamilySymbol
            //        if (!usingFamilySymbols.Contains(instance.Symbol.Id))
            //        {
            //            usingFamilySymbols.Add(instance.Symbol.Id);
            //        }
            //    }
            //}
            //OpenningEntity openningEntity = new OpenningEntity(usingFamilySymbols.First(), doc);
            ////TaskDialog.Show("tt", openningEntity.entityName+"\n"+openningEntity.entityNum);

            //List<ElementId> symbols = usingFamilySymbols.ToList();
            //TaskDialog.Show("tt", symbols.Count().ToString());

            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var elementId in usingFamilySymbols)
            //{
            //    FamilySymbol symbol = doc.GetElement(elementId) as FamilySymbol;
            //    stringBuilder.AppendLine(symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID).AsString());
            //}
            ////TaskDialog.Show("tt", usingFamilySymbols.Count().ToString()+"\n"+familySymbols.Count().ToString());
            //TaskDialog.Show("tt", stringBuilder.ToString());

            ////0622查找线穿过的所有元素。OK
            //Selection sel = uiApp.ActiveUIDocument.Selection;
            //Reference ref1 = sel.PickObject(ObjectType.Element, "Please pick a duct");
            //Duct duct = doc.GetElement(ref1) as Duct;
            //LocationCurve lc = duct.Location as LocationCurve;
            //Curve curve = lc.Curve;
            ////取得线端点的方法
            //XYZ p1 = curve.GetEndPoint(0);
            //XYZ p2 = curve.GetEndPoint(1);
            //XYZ dir = (p2 - p1).Normalize();
            //double length = duct.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            //ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));
            //////向量偏移的方法，这里向下偏移。需要偏移穿墙
            ////XYZ offset = new XYZ(0, 0, 0.01);
            ////ptStart = ptStart - offset;
            ////ptEnd = ptEnd - offset;
            //View3D view3d = null;
            //view3d = doc.ActiveView as View3D;
            //if (view3d == null)
            //{
            //    TaskDialog.Show("3D view", "current view should be 3D view");
            //    return Result.Failed;
            //}
            //ReferenceIntersector rinter = new ReferenceIntersector(wallFilter, FindReferenceTarget.Element, view3d);
            //IList<ReferenceWithContext> nwc = rinter.Find(p1, dir);
            //int i = 0;
            //List<ElementId> ids = new List<ElementId>();
            //foreach (ReferenceWithContext reference in nwc)
            //{
            //    if (reference.Proximity < length)
            //    {
            //        i++;
            //        ids.Add(reference.GetReference().ElementId);
            //    }
            //}
            //sel.SetElementIds(ids);
            //TaskDialog.Show("tt", $"PASS墙数量{i}");

            ////0622 光线法测试 不是必须在三维视图才能用，但貌似没有检测到中间物体
            //// 1. 获取两扇门
            //List<ElementId> selectedIds = new List<ElementId>();
            //Reference r1 = uiDoc.Selection.PickObject(ObjectType.Element, new DoorFilter(), "Pick something");
            //Reference r2 = uiDoc.Selection.PickObject(ObjectType.Element, new DoorFilter(), "Pick something");
            //selectedIds.Add(r1.ElementId);
            //selectedIds.Add(r2.ElementId);
            //var door1 = doc.GetElement(selectedIds.First()) as FamilyInstance;
            //var door2 = doc.GetElement(selectedIds.Last()) as FamilyInstance;
            ////FamilyInstance door1 = (FamilyInstance)doc.GetElement(new ElementId(892207));
            ////FamilyInstance door2 = (FamilyInstance)doc.GetElement(new ElementId(892208));
            //if (door1 == null || door2 == null || door1.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_Doors ||
            //    door2.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_Doors)
            //{
            //    TaskDialog.Show("错误", "请选择两扇门！");
            //    return Result.Cancelled;
            //}
            //////取门的模型中心
            ////BoundingBoxXYZ bb1 = door1.get_BoundingBox(null);
            ////BoundingBoxXYZ bb2 = door2.get_BoundingBox(null);
            ////XYZ center1 = (bb1.Min + bb1.Max) * 0.5; // 门1的中心点
            ////XYZ center2 = (bb2.Min + bb2.Max) * 0.5; // 门2的中心点
            //////获取门的中心点其他方法，门洞中心最近
            //XYZ center1 = ((LocationPoint)door1.Location).Point;
            //XYZ center2 = ((LocationPoint)door2.Location).Point;
            //// 3. 构造射线方向
            //XYZ direction = (center2 - center1).Normalize();
            //double distance = center1.DistanceTo(center2);
            ////找默认view3D
            //var elems = new FilteredElementCollector(doc).OfClass(typeof(View)).OfCategory(BuiltInCategory.OST_Views).Cast<View>().ToList();
            //List<View> t3D = new List<View>();
            //foreach (var item in elems)
            //{
            //    if (item.ViewType == ViewType.ThreeD && !item.IsTemplate && (item.Name == "{三维}" || item.Name == "{3D}"))
            //    {
            //        t3D.Add(item);
            //    }
            //}
            //if (t3D == null) return Result.Failed;
            //View3D view3D = (View3D)t3D.FirstOrDefault();
            //// 4. 使用 ReferenceIntersector 检测是否直接命中 door2
            //ReferenceIntersector intersector = new ReferenceIntersector(door2.Id, FindReferenceTarget.Element, view3D);
            //intersector.FindReferencesInRevitLinks = true; // 如果需要检测链接模型
            //////List<ElementId> elementIds = new List<ElementId>();
            //////List<string> ids = new List<string>();
            ////StringBuilder stringBuilder = new StringBuilder();
            ////IList<ReferenceWithContext> nwc = intersector.Find(center1, direction);
            ////foreach (var item in nwc)
            ////{
            ////    if (item.GetReference().ElementId != door2.Id)
            ////    {
            ////        stringBuilder.AppendLine(item.GetReference().ElementId.ToString());
            ////    }
            ////}
            ////if (stringBuilder.Length == 0) TaskDialog.Show("tt", "射线直接命中第二扇门");
            ////else TaskDialog.Show("tt", stringBuilder.ToString());
            //StringBuilder stringBuilder = new StringBuilder();
            //ReferenceWithContext hitRef = intersector.FindNearest(center1, direction);
            //if (hitRef != null)
            //{
            //    double hitDistance = hitRef.Proximity;
            //    //using (Transaction tx = new Transaction(doc, "Draw Line"))
            //    //{
            //    //    tx.Start();

            //    //    // 创建一条线
            //    //    Line line = Line.CreateBound(center1, center2);
            //    //    Element lineElement = doc.Create.NewDetailCurve(activeView, line);

            //    //    tx.Commit();
            //    //}
            //    TaskDialog.Show("距离", $"两扇门之间的直线距离: {distance * 304.8:0.00} \n射线检测距离: {hitDistance * 304.8:0.00} ");
            //}
            //else
            //{
            //    TaskDialog.Show("结果", "射线未直接命中第二扇门，可能被遮挡！" + hitRef.GetReference().ElementId.ToString());
            //}

            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new DoorFilter(), "Pick something");
            //FamilyInstance fi = (FamilyInstance)doc.GetElement(r);


            //0621 通用获取输入值方法
            //UniversalNewString subView = new UniversalNewString("提示：请输入主文件名");
            //if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            //{
            //    return Result.Cancelled;
            //}

            ////0617 实现combobox或Listbox增加选项 item后面添加删除按钮.OK
            //ListboxTest listboxTest = new ListboxTest();
            //listboxTest.Show();

            ////0618 点击垂直管端生成喷头并连接，管尺寸要有限制 没完成，视必要性补充
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "Pick something");
            //Pipe pipe = (Pipe)doc.GetElement(r);
            //if (pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8 > 20) return Result.Failed;
            //List<Connector> connectors = GetConnectors(pipe);
            //if (connectors.Count() != 2 || isConnectedPipe(connectors)) return Result.Failed;
            //// Find unconnected connectors
            //Connector unconnectedConnector = connectors.FirstOrDefault(c => !c.IsConnected);
            //if (unconnectedConnector == null) return Result.Failed;

            ////0618 修改三通喷头问题.OK
            //SprinklerReplaceAmendView amendView = new SprinklerReplaceAmendView(uiApp);
            //amendView.ShowDialog();

            ////0527 设置系统禁止后台计算 待放到功能内.OK
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType));
            //List<PipingSystemType> pipingSystemTypes = elems.OfType<PipingSystemType>().ToList();
            //FilteredElementCollector elems2 = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType));
            //List<MechanicalSystemType> ductSystemTypes = elems2.OfType<MechanicalSystemType>().ToList();
            //using (Transaction tr = new Transaction(doc))
            //{
            //    tr.Start("关闭计算");
            //    foreach (PipingSystemType item in pipingSystemTypes)
            //    {
            //        item.CalculationLevel = Autodesk.Revit.DB.Mechanical.SystemCalculationLevel.None;
            //    }
            //    foreach (MechanicalSystemType item2 in ductSystemTypes)
            //    {
            //        item2.CalculationLevel = Autodesk.Revit.DB.Mechanical.SystemCalculationLevel.None;
            //    }
            //    tr.Commit();
            //}     

            //0520 遗留测试
            ////0404 切换连接顺序抄网上代码，初步实现柱切板和梁，梁切板。
            //FilteredElementCollector list_column = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance));
            //FilteredElementCollector list_beam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance));
            ////TaskDialog.Show("tt", $"柱{list_column.Count().ToString()}个,梁{list_beam.Count().ToString()}个");
            //Transaction transaction = new Transaction(doc, "连接几何关系");
            //transaction.Start();
            //foreach (Element column in list_column)
            //{
            //    List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, 1.01);
            //    //TaskDialog.Show("柱子", column_box_eles.Count.ToString());
            //    foreach (Element ele in column_box_eles)
            //    {
            //        if (ele.Category.GetHashCode().ToString() == "-2001320" || ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, column, ele);
            //        }
            //    }
            //}
            //foreach (Element beam in list_beam)
            //{
            //    List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, 1.01);
            //    //TaskDialog.Show("梁", beam_box_eles.Count.ToString());
            //    foreach (Element ele in beam_box_eles)
            //    {
            //        //if (ele.Category.Name == "楼板")
            //        if (ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, beam, ele);
            //        }
            //    }
            //}
            return Result.Succeeded;
        }
    }
}
