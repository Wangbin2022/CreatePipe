using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.ViewFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Autodesk.Windows;
//using adWin = Autodesk.Windows;

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
        /// 辅助方法：检查一个元素的包围盒中心点是否在房间内。
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
            ////0906 楼梯entity属性梳理 
            var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            //var instance = doc.GetElement(new ElementId(2187406)) as Element;
            //if (instance is Stairs)
            //{
            //    var stair = (Stairs)instance;
            //    //TaskDialog.Show("tt", stair.NumberOfStories.ToString());
            //    //实际单步高度
            //    //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
            //    //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
            //    //实际单步深度,踏面数量
            //    //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());
            //    //TaskDialog.Show("tt", (stair.ActualTreadsNumber).ToString());
            //    //绝对高度底和顶，要计入项目基点高差
            var basePoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
            double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;

            TaskDialog.Show("tt", (stair.BaseElevation * 304.8 - deltaHeight).ToString("F2"));
            TaskDialog.Show("tt", (stair.TopElevation * 304.8 - deltaHeight).ToString("F2"));
            //    //楼梯总高差
            //    //TaskDialog.Show("tt", (stair.Height * 304.8).ToString());
            //    //TaskDialog.Show("tt", (stair.GetStairsRuns().Count()).ToString());
            //    //跑数和内部各跑宽度，高度等
            //    //var runs = stair.GetStairsRuns();
            //    //StringBuilder stringBuilder = new StringBuilder();
            //    //foreach (var item in runs)
            //    //{
            //    //    StairsRun stairsRun = doc.GetElement(item) as StairsRun;
            //    //    stringBuilder.AppendLine((stairsRun.ActualRunWidth * 304.8).ToString());
            //    //}
            //    //TaskDialog.Show("tt", runs.Count().ToString());
            //}
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

            ////检查楼梯中心点是否在房间内也可以
            ////var stair = doc.GetElement(new ElementId(1926218)) as Stairs;
            ////var stair = doc.GetElement(new ElementId(1929002)) as Stairs;
            //var stair = doc.GetElement(new ElementId(1928367)) as Stairs;

            ////var stair = doc.GetElement(new ElementId(2193116)) as Stairs;
            ////var stair = doc.GetElement(new ElementId(2520656)) as Stairs;
            ////var stair = doc.GetElement(new ElementId(2521425)) as Stairs;
            ////var stair = doc.GetElement(new ElementId(2191119)) as Stairs;
            ////var stair = doc.GetElement(new ElementId(2187406)) as Stairs;
            //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
            //if (isStairInRoom)
            //{
            //    TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。");
            //}
            //else
            //{
            //    TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。");
            //}

            //////0804 房间管理器.OK 还需提高效率，启动排序需要优化
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
            //    if (hitElementIds == null)
            //    {
            //        TaskDialog.Show("结果", "没有检测到碰撞对象");
            //    }
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
            //——————————————————
            //////0903 通用多选窗口实现验证
            //////List<string> test =new List<string> { "1", "22", "33", "44" };
            ////List<string> test = new List<string>();
            ////test = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(item => item.Name).ToList();
            ////UniversalComboBoxMultiSelection boxMultiSelection = new UniversalComboBoxMultiSelection(test, "test0903");
            ////boxMultiSelection.Title = "验证";
            ////boxMultiSelection.ShowDialog();
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
            //        //TaskDialog.Show("tt", "PASS");
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
            //List<Level> levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
            //using (Transaction trans = new Transaction(doc, "切分结构柱"))
            //{
            //    trans.Start();
            //    // 获取柱的顶底标高
            //    Level baseLevel = doc.GetElement(column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId()) as Level;
            //    double baseElevation = baseLevel.Elevation;
            //    Level topLevel = doc.GetElement(column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()) as Level;
            //    double topElevation = topLevel.Elevation;
            //    // 获取柱的底部偏移和顶部偏移
            //    double baseOffset = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble();
            //    double topOffset = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble();
            //    // 计算柱的实际高度
            //    double columnHeight = topElevation + topOffset - (baseElevation + baseOffset);
            //    //// 筛选出与柱相关的标高
            //    List<Level> relevantLevels = levels.Where(l => l.Elevation > (baseElevation + baseOffset) && l.Elevation < (topElevation + topOffset)).OrderBy(l => l.Elevation).ToList();
            //    //TaskDialog.Show("tt", relevantLevels.Count().ToString());
            //    if (relevantLevels.Count == 0)
            //    {
            //        Autodesk.Revit.UI.TaskDialog.Show("提示", "没有合适的标高用于切分结构柱！");
            //        trans.RollBack();
            //        return Result.Failed;
            //    }
            //    // 获取柱的位置
            //    LocationPoint columnLocation = column.Location as LocationPoint;
            //    //// 标高初始化
            //    Level previousLevel = baseLevel;
            //    foreach (Level level in relevantLevels)
            //    {
            //        // 创建新结构柱
            //        FamilyInstance newColumn = doc.Create.NewFamilyInstance(columnLocation.Point, column.Symbol, previousLevel, StructuralType.Column);
            //        // 设置新柱的顶部标高
            //        //newColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Set(level.Elevation);
            //        //newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set((level.Elevation-previousLevel.Elevation));
            //        newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set((level.Elevation - previousLevel.Elevation));
            //        newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(previousLevel.Id);
            //        // 更新底部标高
            //        previousLevel = level;
            //    }
            //    // 删除原始柱
            //    doc.Delete(column.Id);
            //    trans.Commit();
            //}
            //Autodesk.Revit.UI.TaskDialog.Show("提示", "结构柱切分成功！");

            //0222 用标高切分墙的程序，初步完成。倒是切分柱子似乎更合理
            //还需改进的问题：如果有顶标高但顶部偏移更高的话会导致错误逻辑优先
            //还需改进的问题：新建墙的话原有的窗洞口需要考虑放到哪层，工作量可能要判断是否值得
            //var wall = uiDoc.Selection.PickObject(ObjectType.Element, new filterWallClass(), "选墙");
            //Wall elem = doc.GetElement(wall.ElementId) as Wall;
            //// 获取所有标高
            //List<Level> levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
            //// 开始事务
            //using (Transaction trans = new Transaction(doc, "切分墙"))
            //{
            //    trans.Start();
            //    LocationCurve wallCurve = elem.Location as LocationCurve;
            //    XYZ startPoint = wallCurve.Curve.GetEndPoint(0);
            //    XYZ endPoint = wallCurve.Curve.GetEndPoint(1);
            //    // 获取墙的底部和顶部标高
            //    Level baseLevel = doc.GetElement(elem.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId()) as Level;
            //    Level topLevel = doc.GetElement(elem.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()) as Level;
            //    double topElevation;
            //    List<Level> relevantLevels = new List<Level>();
            //    if (topLevel == null)
            //    {
            //        // 如果顶部标高未设置，使用底部标高和顶部偏移计算顶部高度
            //        double baseElevation = baseLevel.Elevation;
            //        double topOffset = elem.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
            //        topElevation = baseElevation + topOffset;
            //        relevantLevels = levels.Where(l => l.Elevation > baseElevation && l.Elevation < topElevation).ToList();
            //    }
            //    else 
            //    {
            //        relevantLevels = levels.Where(l => l.Elevation > baseLevel.Elevation && l.Elevation < topLevel.Elevation).ToList();
            //    }
            //    if (relevantLevels.Count == 0)
            //    {
            //        TaskDialog.Show("提示", "没有合适的标高用于切分墙！");
            //        return Result.Failed;
            //    }
            //    // 按标高切分墙
            //    XYZ previousPoint = startPoint;
            //    Level previousLevel = baseLevel;
            //    foreach (Level level in relevantLevels)
            //    {
            //        // 计算切分点的高度
            //        double elevation = level.Elevation - baseLevel.Elevation;
            //        XYZ splitPoint = startPoint + (endPoint - startPoint).Normalize() * elevation;
            //        // 创建新墙
            //        Wall newWall = Wall.Create(doc, wallCurve.Curve, elem.WallType.Id, previousLevel.Id, level.Elevation - previousLevel.Elevation, 0, false, false);
            //        // 更新起点和底部标高
            //        previousPoint = splitPoint;
            //        previousLevel = level;
            //    }
            //    //// 创建最后一段墙（从最后一个切分点到终点）
            //    if (topLevel == null)
            //    {
            //        // 如果顶部标高未设置，使用底部标高和顶部偏移计算顶部高度
            //        double baseElevation = baseLevel.Elevation;
            //        double topOffset = elem.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
            //        topElevation = baseElevation + topOffset;
            //        Wall lastWall = Wall.Create(doc, wallCurve.Curve, elem.WallType.Id, previousLevel.Id, topElevation - previousLevel.Elevation, 0, false, false);
            //    }
            //    else
            //    {
            //        Wall lastWall = Wall.Create(doc, wallCurve.Curve, elem.WallType.Id, previousLevel.Id, topLevel.Elevation - previousLevel.Elevation, 0, false, false);
            //    }
            //    //// 删除原墙
            //    doc.Delete(elem.Id);
            //    trans.Commit();
            //}
            //TaskDialog.Show("提示", "墙切分成功！");

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
