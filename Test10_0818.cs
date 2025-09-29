using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.ViewFilters;
using CreatePipe.WpfDirectoryTreeView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test10_0818 : IExternalCommand
    {

        /// <summary>
        /// 执行射线检测并返回最近的碰撞图元ID
        /// </summary>
        /// <param name="doc">Revit文档</param>
        /// <param name="origin">射线起点</param>
        /// <param name="direction">射线方向</param>
        /// <param name="view">用于检测的视图（可选）</param>
        /// <returns>碰撞到的第一个图元的ElementId，如果没有碰撞则返回ElementId.InvalidElementId</returns>
        public static ElementId RaycastNearest(Document doc, XYZ origin, XYZ direction, double deltaHeight, View view = null)
        {
            // 规范化方向向量
            direction = direction.Normalize();
            // 创建ReferenceIntersector
            ReferenceIntersector intersector;
            if (view != null)
            {
                intersector = new ReferenceIntersector((View3D)view);
            }
            else
            {
                // 使用3D视图设置进行检测
                intersector = new ReferenceIntersector(Find3DView(doc) ?? throw new System.Exception("找不到可用的3D视图"));
            }
            // 设置查找最近的交点
            intersector.TargetType = FindReferenceTarget.Face;
            intersector.FindReferencesInRevitLinks = true;
            XYZ originptWithHeight = new XYZ(origin.X, origin.Y, deltaHeight / 304.8);
            // 执行射线检测
            ReferenceWithContext referenceWithContext = intersector.FindNearest(originptWithHeight, direction);
            //ReferenceWithContext referenceWithContext = intersector.FindNearest(origin, direction);
            if (referenceWithContext == null) return ElementId.InvalidElementId;
            // 获取碰撞图元的ElementId
            Reference reference = referenceWithContext.GetReference();
            return reference?.ElementId ?? ElementId.InvalidElementId;
        }
        private static View3D Find3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(View3D));
            foreach (View3D view in collector)
            {
                if (!view.IsTemplate && view.Name != "{3D}") return view;
            }
            return null;
        }
        Action<string> onSelected = selectedName =>
        {
            Autodesk.Revit.UI.TaskDialog.Show("tt", selectedName);
        };
        public bool IsBoundingBoxContained(BoundingBoxXYZ container, BoundingBoxXYZ contained)
        {
            // 检查 contained 的最小点是否在 container 内
            bool minContained = container.Min.X <= contained.Min.X &&
                                container.Min.Y <= contained.Min.Y &&
                                container.Min.Z <= contained.Min.Z;

            // 检查 contained 的最大点是否在 container 内
            bool maxContained = container.Max.X >= contained.Max.X &&
                                container.Max.Y >= contained.Max.Y &&
                                container.Max.Z >= contained.Max.Z;

            return minContained && maxContained;
        }
        /// <returns>如果在房间内则返回true，否则返回false</returns>
        public bool IsAnyPartOfStairInRoom(Stairs stair, Room room, Document doc)
        {
            // 1. 检查所有梯段 (StairsRun)
            foreach (ElementId runId in stair.GetStairsRuns())
            {
                Element runElem = doc.GetElement(runId);
                if (IsElementCenterInRoom(runElem, room))
                {
                    // TaskDialog.Show("Debug", $"梯段 {runId} 在房间内。"); // 用于调试
                    return true; // 只要有一个梯段在，就返回true
                }
            }
            // 2. 检查所有平台 (StairsLanding)
            foreach (ElementId landingId in stair.GetStairsLandings())
            {
                Element landingElem = doc.GetElement(landingId);
                if (IsElementCenterInRoom(landingElem, room))
                {
                    // TaskDialog.Show("Debug", $"平台 {landingId} 在房间内。"); // 用于调试
                    return true; // 只要有一个平台在，就返回true
                }
            }
            // 如果所有子构件都不在房间内，则认为整个楼梯不在
            return false;
        }
        /// <summary>
        /// 辅助方法：检查一个元素的包围盒中心点是否在房间内。物体与房间关系
        /// </summary>
        private bool IsElementCenterInRoom(Element elem, Room room)
        {
            if (elem == null || room == null) return false;
            BoundingBoxXYZ bbox = elem.get_BoundingBox(null); // 使用全局坐标，不依赖视图
            if (bbox == null || !bbox.Enabled) return false;
            XYZ centerPoint = (bbox.Min + bbox.Max) / 2.0;
            return room.IsPointInRoom(centerPoint);
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


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


            ////0926 批量按选择项写面积属性,临时工具
            //ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //using (Transaction trans = new Transaction(doc, "设置面积"))
            //{
            //    trans.Start();
            //    foreach (var item in selectedIds)
            //    {
            //        Element element = doc.GetElement(item);
            //        double areaValue = element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble();
            //        // 检查自定义参数是否存在
            //        Parameter areaParam = element.LookupParameter("面积1");
            //        if (areaParam != null && !areaParam.IsReadOnly)
            //        {
            //            areaParam.Set(((areaValue * 304.8 * 304.8) / (1000 * 1000)).ToString("F2"));
            //        }
            //        else
            //        {
            //            TaskDialog.Show("提示", "元素ID " + item.IntegerValue + " 不存在'面积1'参数或参数为只读");
            //        }
            //    }
            //    trans.Commit();
            //}
            ////0909 取楼梯中心几何点
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            //BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            //if (bbox == null) return Result.Failed;
            //XYZ min = bbox.Min;
            //XYZ max = bbox.Max;
            //XYZ center = (min + max) * 0.5;
            //// 输出中心点（XY）
            //TaskDialog.Show("楼梯中心", $"楼梯 {stair.Id} 的中心点XY坐标: ({center.X}, {center.Y})");
            //例程结束
            //0906 楼梯应与空间结合，单独设置房间应付异型楼梯等非标情况
            //var instances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Stairs).ToElementIds();
            ////以上收集的包含symbol和实例
            //StringBuilder stringBuilder = new StringBuilder();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in instances)
            //{
            //    //只过滤实例,取得实体和symbol
            //    if (Stairs.IsByComponent(doc, item))
            //    {
            //        stringBuilder.AppendLine(item.IntegerValue.ToString());
            //        var component = doc.GetElement(item);
            //        stringBuilder.AppendLine(doc.GetElement(component.GetTypeId()).Name.ToString());
            //        ids.Add(component.Id);
            //    }
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString() + "+" + ids.Count().ToString());
            //////0906 楼梯entity属性梳理 
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            ////var instance = doc.GetElement(new ElementId(2187406)) as Element;
            ////if (instance is Stairs)
            ////{
            ////    var stair = (Stairs)instance;
            ////    //TaskDialog.Show("tt", stair.NumberOfStories.ToString());
            ////    //实际单步高度
            ////    //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
            ////    //实际单步深度,踏面数量
            ////    //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.ActualTreadsNumber).ToString());
            ////    //绝对高度底和顶，要计入项目基点高差
            //var basePoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
            //double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
            //TaskDialog.Show("tt", (stair.BaseElevation * 304.8 - deltaHeight).ToString("F2"));
            //TaskDialog.Show("tt", (stair.TopElevation * 304.8 - deltaHeight).ToString("F2"));
            ////    //楼梯总高差
            ////    //TaskDialog.Show("tt", (stair.Height * 304.8).ToString());
            ////    //TaskDialog.Show("tt", (stair.GetStairsRuns().Count()).ToString());
            ////    //跑数和内部各跑宽度，高度等
            ////    //var runs = stair.GetStairsRuns();
            ////    //StringBuilder stringBuilder = new StringBuilder();
            ////    //foreach (var item in runs)
            ////    //{
            ////    //    StairsRun stairsRun = doc.GetElement(item) as StairsRun;
            ////    //    stringBuilder.AppendLine((stairsRun.ActualRunWidth * 304.8).ToString());
            ////    //}
            ////    //TaskDialog.Show("tt", runs.Count().ToString());
            ////}
            ////例程结束
            //////0906 房间楼梯关系梳理 ，判断楼梯是否有部分在房间内即可，没必要全匹配
            //var room = doc.GetElement(new ElementId(2006502)) as Room;
            //////var room = doc.GetElement(new ElementId(1295107)) as Room;
            //////var room = doc.GetElement(new ElementId(1295122)) as Room;
            ////var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            //////int edges = room.GetBoundarySegments(boundaryOptions).Sum(loop => loop.Count);
            ////IList<IList<BoundarySegment>> boundarySegments = room.GetBoundarySegments(boundaryOptions);
            ////BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            ////XYZ minPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            ////XYZ maxPoint = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            ////foreach (IList<BoundarySegment> boundaryLoop in boundarySegments)
            ////{
            ////    CurveLoop curveLoop = new CurveLoop();
            ////    foreach (BoundarySegment segment in boundaryLoop)
            ////    {
            ////        // 获取曲线的起点和终点
            ////        Curve curve = segment.GetCurve();
            ////        XYZ startPoint = curve.GetEndPoint(0);
            ////        XYZ endPoint = curve.GetEndPoint(1);
            ////        // 更新最小点
            ////        minPoint = new XYZ(
            ////            Math.Min(minPoint.X, Math.Min(startPoint.X, endPoint.X)),
            ////            Math.Min(minPoint.Y, Math.Min(startPoint.Y, endPoint.Y)),
            ////            Math.Min(minPoint.Z, Math.Min(startPoint.Z, endPoint.Z))
            ////        );
            ////        // 更新最大点
            ////        maxPoint = new XYZ(
            ////            Math.Max(maxPoint.X, Math.Max(startPoint.X, endPoint.X)),
            ////            Math.Max(maxPoint.Y, Math.Max(startPoint.Y, endPoint.Y)),
            ////            //Math.Max(maxPoint.Z, Math.Max(startPoint.Z, endPoint.Z))
            ////            double.MaxValue);
            ////    }
            ////}
            ////// 设置边界框的最小点和最大点
            ////boundingBox.Min = minPoint;
            ////boundingBox.Max = maxPoint;
            //////TaskDialog.Show("tt", $"{boundingBox.Max.X.ToString("F2")}+{boundingBox.Max.Y.ToString("F2")}+{boundingBox.Max.Z.ToString("F2")}");
            //////TaskDialog.Show("tt", $"{boundingBox.Min.X.ToString("F2")}+{boundingBox.Min.Y.ToString("F2")}+{boundingBox.Min.Z.ToString("F2")}");
            //例程结束
            ////检查楼梯中心点是否在房间内也可以
            ////var stair = doc.GetElement(new ElementId(1926218)) as Stairs;
            //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
            //if (isStairInRoom)
            //{  TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。"); }
            //else {   TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。"); }
            //例程结束
            //////0804 房间管理器.OK  
            //RoomManagerView roomManager = new RoomManagerView(uiApp);
            //roomManager.Show();
            //////0829 链接管理
            //var instances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            ////var instances = new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)).Cast<CADLinkType>().ToList();
            ////取链接文件名
            ////TaskDialog.Show("tt", instances.First().GetLinkDocument().Title);
            ////TaskDialog.Show("tt", instances.First().GetLinkDocument().GetWarnings().Count().ToString());
            //TaskDialog.Show("tt", doc.GetWarnings().Count().ToString()); 
            //////0811 射线360扫射检测碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    double deltaHeight = 200;
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 5. 在XY平面进行360度检测（每1度一次）
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        // 计算当前角度方向向量（Z=0）
            //        double radians = angle * Math.PI / 180;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //        if (hitElementId != ElementId.InvalidElementId)
            //        {
            //            hitElementIds.Add(hitElementId);
            //        }
            //    }
            //    foreach (var item in hitElementIds)
            //    {
            //        stringBuilder.AppendLine(item.ToString());
            //    }
            //    if (hitElementIds == null)   {  TaskDialog.Show("结果", "没有检测到碰撞对象"); }
            //    else
            //    {
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElementIds.Count}\n" + $"ID: {stringBuilder.ToString()}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds);
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            ////0811 二维射线法手搓尝试，单点单方向碰撞.OK
            //try
            //{
            //    // 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择射线起点");
            //    // 定义射线方向（这里使用X轴方向）
            //    XYZ direction = XYZ.BasisX;
            //    double deltaHeight = 200;
            //    // 执行射线检测
            //    ElementId hitElementId = RaycastNearest(doc, origin, direction, deltaHeight);
            //    if (hitElementId == ElementId.InvalidElementId)
            //    {
            //        TaskDialog.Show("结果", "没有检测到碰撞对象");
            //    }
            //    else
            //    {
            //        Element hitElement = doc.GetElement(hitElementId);
            //        TaskDialog.Show("结果", $"检测到碰撞对象: {hitElement.Name}\n" + $"ID: {hitElementId.IntegerValue}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(new List<ElementId> { hitElementId });
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", ex.Message.ToString());
            //    return Result.Failed;
            //}
            //例程结束
            //0520 遗留测试
            //——————————————————
            ////0929 多选样例模板
            //List<string> test = new List<string>();
            //test = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(item => item.Name).ToList();
            //// 2. 创建并配置对话框实例
            //UniversalComboBoxMultiSelection boxMultiSelection = new UniversalComboBoxMultiSelection(test, "请选择一个或多个标高：");
            //boxMultiSelection.Title = "标高选择";
            //// 3. 以模态方式显示对话框，程序会在此暂停
            //bool? dialogResult = boxMultiSelection.ShowDialog();
            //// 4. 对话框关闭后，检查返回结果
            //if (dialogResult == true)
            //{
            //    // 如果用户点击了 "确认"，从公共属性中获取选择的列表
            //    List<string> selectedLevels = boxMultiSelection.SelectedResult;
            //    // 5. 处理结果
            //    if (selectedLevels.Any())
            //    {
            //        // 将选择的标高名称拼接成一个字符串用于显示
            //        string resultText = "您选择了: " + string.Join(", ", selectedLevels);
            //        TaskDialog.Show("选择结果", resultText);
            //    }
            //    else
            //    {
            //        TaskDialog.Show("提示", "您点击了确认，但没有选择任何项。");
            //    }
            //}
            //else
            //{
            //    // 用户点击了取消、关闭按钮，或者按了 Esc 键
            //    TaskDialog.Show("操作取消", "用户已取消操作。");
            //}
            //0925 改multiComboBox控件测试
            //PipeSystemTest pipeSystemTest = new PipeSystemTest(doc);
            //pipeSystemTest.ShowDialog();
            //////0925 修改
            ////0903 通用多选窗口实现验证
            //////0925 布置沟代码OK
            //CircleGaugePlaceView circleGaugePlaceView = new CircleGaugePlaceView(uiApp);
            //circleGaugePlaceView.Show();
            //0922 用标高切分墙，柱，机电管线的程序合并界面
            //0922 柱切板和梁，梁切板深化界面
            //0913 拾取自适应环(通用)
            //0913 族管理器增加类别
            //FamilyManagerView familyManagerView = new FamilyManagerView(uiApp);
            //familyManagerView.Show();
            ////////0903 adWindows试验
            ///////似乎无法找到bentley的插件？
            //RibbonControl ribbonControl = adWin.ComponentManager.Ribbon;
            //StringBuilder stringBuilder = new StringBuilder();
            //List<string> test = new List<string>();
            //foreach (RibbonTab tab in ribbonControl.Tabs)
            //{
            //    ////stringBuilder.AppendLine(tab.Name);
            //    //if (!tab.IsContextualTab && !tab.IsMergedContextualTab && tab.KeyTip == null)
            //    if (!tab.IsContextualTab && !tab.IsMergedContextualTab)
            //    {
            //        //tab.IsVisible = !tab.IsVisible;
            //        //stringBuilder.AppendLine(tab.Name);
            //        //test.Add(tab.Name);
            //        test.Add(tab.AutomationName);
            //    }
            //}
            ////Autodesk.Revit.UI.TaskDialog.Show("tt", stringBuilder.ToString());
            //UniversalComboBoxMultiSelection subView = new UniversalComboBoxMultiSelection(test, "test0903");
            //subView.Title = "验证";
            ////boxMultiSelection.ShowDialog();
            //if (subView.ShowDialog() != true || !(subView.DataContext is UniversalComboBoxMultiSelectionViewModel vm) || vm.SelectedItems == null)
            //{
            //    return Result.Failed;
            //}
            //try
            //{
            //    Autodesk.Revit.UI.TaskDialog.Show("tt", vm.SelectedItems.Count().ToString());
            //}
            //catch (Exception ex)
            //{
            //    Autodesk.Revit.UI.TaskDialog.Show("tt", $"发生错误: {ex.Message}");
            //}
            //using (Transaction tx = new Transaction(doc, "改tab可见性"))
            //{
            //    tx.Start();
            //    foreach (RibbonTab tab in ribbonControl.Tabs)
            //    {
            //        foreach (var item in vm.SelectedItems)
            //        {
            //            if (tab.Name == item)
            //            {
            //                tab.IsVisible = !tab.IsVisible;
            //                break;
            //            }
            //        }
            //    }
            //    tx.Commit();
            //}
            ////ViewFiltersForm viewFiltersForm =new ViewFiltersForm(uiApp);
            ////viewFiltersForm.ShowDialog();
            ////0903 过滤器bug测试
            ///model类型为空 导致，处理增加 if (category == null) return null;
            //try
            //{
            //    var instances = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
            //ParameterFilterElement pfe = null;
            //foreach (var item in instances)
            //{
            //    if (item.Name == "地沟结构填充")
            //    {
            //        pfe= item;
            //    }
            //}
            ////TaskDialog.Show("tt", pfe.Name);

            //    ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
            //    if (elf is LogicalAndFilter)
            //    {
            //        TaskDialog.Show("tt", "且"); 
            //    }
            //    else if (elf is LogicalOrFilter)
            //    {
            //        TaskDialog.Show("tt", "或"); 
            //    }
            //    else TaskDialog.Show("tt", "nnPASS"); 
            //    //if (elf == null)
            //    //{
            //    //    TaskDialog.Show("tt", "noPASS");
            //    //}
            //    //TaskDialog.Show("tt", instances.Count().ToString());
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            //////0902 已载入插件查找管理2
            ////var list = uiApp.GetRibbonPanels("DiRootsOne");
            ////var list = uiApp.GetRibbonPanels("FJSKFamily");
            //var list = uiApp.GetRibbonPanels("GLS风");
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in list)
            //{
            //    stringBuilder.AppendLine(item.Name);
            //}
            //Autodesk.Revit.UI.TaskDialog.Show("tt", stringBuilder.ToString());
            //////0830 已载入插件查找管理1
            //var loadedApps = uiApp.LoadedApplications;
            ////var list = loadedApps.Cast<IExternalApplication>().Select(a => a.GetType().FullName).ToList();
            //var list = loadedApps.Cast<IExternalApplication>().ToList();
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in list)
            //{
            //    //stringBuilder.AppendLine(item.GetType().Assembly.Location.ToString());
            //    stringBuilder.AppendLine(item.GetType().FullName.ToString());
            //    //item.GetType().
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString());
            //////exApp.GetType().Assembly.Location

            ////0222 用标高切分结构柱，初步完成 在结构柱分层的高度上仍有问题。。要考虑柱的顶底偏移再设置切分逻辑
            ////新的柱子虽然不用考虑开洞但仍需手动考虑偏移的各种情况给赋值。
            ////斜柱如何处理  
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new ColumnFilter(), "选择结构柱");
            //FamilyInstance column = doc.GetElement(columnRef.ElementId) as FamilyInstance;
            //0222 用标高切分墙的程序，初步完成。倒是切分柱子似乎更合理
            //还需改进的问题：如果有顶标高但顶部偏移更高的话会导致错误逻辑优先
            //还需改进的问题：新建墙的话原有的窗洞口需要考虑放到哪层，工作量可能要判断是否值得
            ////0929 改前代码，门窗会丢失版本 
            //例程结束
            //0822 改视图比例
            //TaskDialog.Show("tt", activeView.Scale.ToString());
            //using (Transaction tx = new Transaction(doc, "改视图比例"))
            //{
            //    tx.Start();
            //    if (activeView.Scale != 200)
            //    {
            //        activeView.Scale = 300;
            //    }
            //    tx.Commit();
            //}
            //EvacRouteManagerView evacRouteManagerView = new EvacRouteManagerView(uiApp);
            //evacRouteManagerView.Show();
            ////0818 字符串分割测试。先检测空字符串，非法字符（半角逗号，多个分割），限制长度
            ////切分正反字符串，移除前标
            ////根据标点检测是否符合数量，统计牌面数
            ////切分内容到各个牌面
            //string text = "正面：C09-C18，C19-C32，国内出发 登机口D||背面：C01-C08，C19-C32";
            //if (string.IsNullOrEmpty(text))
            //{
            //    TaskDialog.Show("tt", "shuru zifuc ");
            //}
            //int count = 0;
            //for (int i = 0; i < text.Length; i++)
            //{
            //    if (text[i] == '|')                
            //    {
            //        count++;

            //        //// 前 2 个字符
            //        //int prevStart = Math.Max(0, i - 2);
            //        //string prev = text.Substring(prevStart, i - prevStart);
            //        //// 后 2 个字符
            //        //int nextEnd = Math.Min(text.Length - 1, i + 2);
            //        //string next = text.Substring(i + 1, nextEnd - i);
            //        //Console.WriteLine($"第 {count} 个逗号：前面=[{prev}], 后面=[{next}]");
            //    }
            //}
            //TaskDialog.Show("tt", text.Length.ToString());
            //TaskDialog.Show("tt", count.ToString());
            //Console.WriteLine($"共检测到 {count} 个半角逗号。");
            //例程结束
            //GuidanceSignManagerView guidanceSignManagerView = new GuidanceSignManagerView(uiApp);
            //guidanceSignManagerView.Show();
            //0815 布置功能原型
            //GuidanceSignPlaceView placeView = new GuidanceSignPlaceView(uiApp);
            //placeView.Show();
            ////////0812 标识标签
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new TagFilter(), "Pick something");
            //IndependentTag tag = (IndependentTag)doc.GetElement(r.ElementId);
            //FamilySymbol tagSymbol = tag.Document.GetElement(tag.GetTypeId()) as FamilySymbol;

            return Result.Succeeded;
        }
    }
}
