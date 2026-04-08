using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// EvacRouteView.xaml 的交互逻辑
    /// </summary>
    public partial class EvacRouteManagerView : Window
    {
        public EvacRouteManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new EvacRouteManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class EvacRouteManagerViewModel : ObserverableObject
    {
        public Document Document { get; }
        public UIDocument UIDoc { get; }
        public View ActiveView { get; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        private ObservableCollection<AdaptiveRouteEntity> _allRoutes = new ObservableCollection<AdaptiveRouteEntity>();
        public ObservableCollection<AdaptiveRouteEntity> AllRoutes
        {
            get => _allRoutes;
            set => SetProperty(ref _allRoutes, value);
        }
        public EvacRouteManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            UIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            QueryElements(null);
        }
        // ==== 核心业务：纯内存计算路线与门的交点 (不再创建实体线，抛弃Transaction) ====
        public ICommand IntersectDoorDistanceCommand => new RelayCommand<AdaptiveRouteEntity>(IntersectDoor);
        private void IntersectDoor(AdaptiveRouteEntity entity)
        {
            _externalHandler.Run(app =>
            {
                if (entity.NearDoors.Count == 0)
                {
                    TaskDialog.Show("结果", "路线附近无相交的门");
                    return;
                }
                List<string> results = new List<string>();
                List<XYZ> adaptivePoints = GetAdaptivePointPath(entity.AdaptiveInstance);
                foreach (var doorId in entity.NearDoors)
                {
                    if (!(Document.GetElement(doorId) is FamilyInstance door)) continue;
                    double width = door.Symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)?.AsDouble() ?? 3.0; // 默认给个宽度防错
                    // 获取门的 2D 内存线段的两端点
                    (XYZ pt1, XYZ pt2) = GetDoorMemoryLine(door, width);
                    // 纯几何判断，无需新建 DetailLine
                    var (segIdx, dist, sum) = CheckIntersectionPureMath(pt1, pt2, adaptivePoints);
                    if (segIdx >= 0)
                    {
                        results.Add($"[{door.Name}]：交点在第 {segIdx + 1} 段，距线段起点 {(dist * 304.8):F0} mm，路线累计距离 {(sum * 304.8):F0} mm");
                    }
                }
                TaskDialog.Show("交点汇总", results.Count == 0 ? "门在附近但未发生有效交叉" : string.Join("\n\n", results));
            });
        }
        // 获取门中心轴线的两个端点 (纯内存点，不进文档)
        private (XYZ, XYZ) GetDoorMemoryLine(FamilyInstance door, double width)
        {
            if (door.Location is LocationCurve locCurve)
            {
                return (locCurve.Curve.GetEndPoint(0), locCurve.Curve.GetEndPoint(1));
            }
            else if (door.Location is LocationPoint locPoint)
            {
                XYZ origin = locPoint.Point;
                XYZ dir = door.FacingOrientation?.Normalize() ?? XYZ.BasisX;
                XYZ perpDir = new XYZ(-dir.Y, dir.X, 0).Normalize();
                return (origin - perpDir * width / 2, origin + perpDir * width / 2);
            }
            return (XYZ.Zero, XYZ.Zero);
        }
        // 纯数学相交检测
        private (int segIndex, double distToStart, double sumToStart) CheckIntersectionPureMath(XYZ doorPt1, XYZ doorPt2, List<XYZ> routePts)
        {
            if (routePts == null || routePts.Count < 2) return (-1, 0, 0);
            double totalLength = 0.0;
            for (int i = 0; i < routePts.Count - 1; i++)
            {
                XYZ p1 = routePts[i];
                XYZ p2 = routePts[i + 1];
                XYZ intersect = Get2DSegmentIntersection(doorPt1, doorPt2, p1, p2);
                if (intersect != null)
                {
                    // 忽略 Z 轴计算平面距离
                    double dist = new XYZ(intersect.X, intersect.Y, 0).DistanceTo(new XYZ(p1.X, p1.Y, 0));
                    return (i, dist, totalLength + dist);
                }
                totalLength += new XYZ(p1.X, p1.Y, 0).DistanceTo(new XYZ(p2.X, p2.Y, 0));
            }
            return (-1, 0, 0);
        }
        private List<XYZ> GetAdaptivePointPath(FamilyInstance adaptiveInstance)
        {
            return AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(adaptiveInstance)
                .Select(id => Document.GetElement(id) as ReferencePoint).Where(rp => rp != null)
                .Select(rp => rp.Position).ToList();
        }
        // 你的原版数学求交公式（非常有效，保留）
        private XYZ Get2DSegmentIntersection(XYZ p1, XYZ p2, XYZ q1, XYZ q2)
        {
            double x1 = p1.X, y1 = p1.Y, x2 = p2.X, y2 = p2.Y;
            double x3 = q1.X, y3 = q1.Y, x4 = q2.X, y4 = q2.Y;
            double denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (Math.Abs(denominator) < 1e-9) return null;
            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denominator;
            if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
                return new XYZ(x1 + ua * (x2 - x1), y1 + ua * (y2 - y1), 0);
            return null;
        }
        // ==== 过滤与选择命令 ====
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElements);
        private void QueryElements(string keyword)
        {
            AllRoutes.Clear();
            // 直接使用族名称过滤提高性能
            var familyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>().Where(e => e.Symbol.Name.Contains("确定路线")).ToList();
            foreach (var item in familyInstances)
            {
                string levelName = item.LookupParameter("楼层标高")?.AsString() ?? "";
                string familyName = item.Symbol.Family.Name;
                if (string.IsNullOrEmpty(keyword) ||
                    levelName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    familyName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AllRoutes.Add(new AdaptiveRouteEntity(item, _externalHandler));
                }
            }
        }
        public ICommand PickRouteCommand => new RelayCommand<AdaptiveRouteEntity>(PickRoute);
        private void PickRoute(AdaptiveRouteEntity entity)
        {
            if (entity != null)
                UIDoc.Selection.SetElementIds(new List<ElementId> { entity.Id });
        }
        public ICommand PickRoutesCommand => new BaseBindingCommand(PickViewRoutes);
        private void PickViewRoutes(object para)
        {
            if (ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("提示", "请在平面操作本功能");
                return;
            }
            // 获取当前视图关联的真实标高 ID
            ElementId currentLevelId = ActiveView.GenLevel?.Id;
            // 防错：如果视图没有关联标高（理论上极少出现）
            if (currentLevelId == null || currentLevelId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("提示", "当前视图没有找到关联的有效标高");
                return;
            }
            // 基于真实的 LevelId 进行精准对比
            var currentLevelInstances = AllRoutes.Where(r => r.LevelId != null && r.LevelId == currentLevelId).Select(r => r.Id).ToList();
            UIDoc.Selection.SetElementIds(currentLevelInstances);
            // 给一点友好反馈（可选）
            if (currentLevelInstances.Count == 0)
            {
                TaskDialog.Show("提示", "当前视图标高下没有找到疏散路线。");
            }
        }
        public ICommand DeleteElementsCommand => new RelayCommand<object>(DeleteElements);
        private void DeleteElements(object parameter)
        {
            // 1. 获取 DataGrid 传过来的 SelectedItems
            if (!(parameter is System.Collections.IList selectedItems) || selectedItems.Count == 0)
            {
                TaskDialog.Show("提示", "请先在表格中选择要删除的路线。");
                return;
            }
            // 2. 必须转换并拷贝到一个新 List 中，防止 WPF 在删除数据源时触发集合修改异常
            var itemsToDelete = selectedItems.Cast<AdaptiveRouteEntity>().ToList();
            if (itemsToDelete.Count == 0) return;
            _externalHandler.Run(app =>
            {
                var idsToDelete = itemsToDelete.Select(e => e.Id).ToList();
                // 使用你封装好的主事务
                NewTransaction.Execute(Document, "批量删除疏散路线", () =>
                {
                    Document.Delete(idsToDelete);
                });
                // 事务成功提交后，同步更新 UI 集合
                foreach (var item in itemsToDelete)
                {
                    AllRoutes.Remove(item);
                }
            });
        }
    }
    //public class EvacRouteManagerViewModel : ObserverableObject
    //{
    //    public Document Document { get; set; }
    //    public UIDocument uIDoc { get; set; }
    //    public Autodesk.Revit.DB.View ActiveView { get; set; }
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public EvacRouteManagerViewModel(UIApplication uiApp)
    //    {
    //        Document = uiApp.ActiveUIDocument.Document;
    //        uIDoc = uiApp.ActiveUIDocument;
    //        ActiveView = Document.ActiveView;
    //        var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
    //        if (routeSymbols.Count() != 4) TaskDialog.Show("错误", "未找到指定的自适应族");
    //        QueryELement(null);
    //        List<Line> doorLines = new List<Line>();

    //    }
    //    public ICommand IntersectDoorDistanceCommand => new RelayCommand<AdaptiveRouteEntity>(InterSectDoor);
    //    private void InterSectDoor(AdaptiveRouteEntity entity)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            using (Transaction trans = new Transaction(Document, "检测路径与门关系"))
    //            {
    //                trans.Start();

    //                IReadOnlyList<ElementId> nearDoors = entity.Doors;
    //                List<string> results = new List<string>();   // 汇总信息
    //                foreach (var item in nearDoors)
    //                {
    //                    FamilyInstance instance = Document.GetElement(item) as FamilyInstance;
    //                    double width = instance.Symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM).AsDouble();
    //                    // 取门/窗线
    //                    LocationCurve locCurve = instance.Location as LocationCurve;
    //                    LocationPoint locPoint = instance.Location as LocationPoint;
    //                    XYZ pt1, pt2;
    //                    if (locCurve != null)
    //                    {
    //                        Curve curve1 = locCurve.Curve;
    //                        pt1 = curve1.GetEndPoint(0);
    //                        pt2 = curve1.GetEndPoint(1);
    //                    }
    //                    else
    //                    {
    //                        XYZ origin = locPoint.Point;
    //                        XYZ dir = instance.FacingOrientation;
    //                        if (dir == null || dir.IsZeroLength()) dir = XYZ.BasisX;
    //                        dir = dir.Normalize();
    //                        XYZ perpDir = new XYZ(-dir.Y, dir.X, 0).Normalize();
    //                        pt1 = origin - perpDir * width / 2;
    //                        pt2 = origin + perpDir * width / 2;
    //                    }
    //                    Line resultLine = Line.CreateBound(pt1, pt2);
    //                    DetailLine detailLine = Document.Create.NewDetailCurve(ActiveView, resultLine) as DetailLine;
    //                    // 判断相交并收集信息
    //                    var (segIdx, dist, sum) = CheckIntersectionWithAdaptive(detailLine, entity.AdaptiveInstance, (ViewPlan)ActiveView);
    //                    if (segIdx >= 0)
    //                    {
    //                        results.Add(
    //                            $"门/窗 {instance.Name}：交点在第 {segIdx + 1} 段，" +
    //                            $"距近点{dist * 304.8:F2} mm，累计距首点{(sum * 304.8):F2} mm");
    //                    }
    //                    Document.Delete(detailLine.Id);
    //                }
    //                // 一次性弹窗
    //                if (results.Count == 0) TaskDialog.Show("结果", "无交点");
    //                else TaskDialog.Show("交点汇总", string.Join("\n", results));
    //                trans.Commit();
    //            }
    //        });
    //    }
    //    private List<XYZ> GetAdaptivePointPath(FamilyInstance adaptiveInstance)
    //    {
    //        IList<ElementId> ids = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(adaptiveInstance);
    //        List<XYZ> coords = new List<XYZ>();
    //        foreach (ElementId id in ids)
    //        {
    //            if (adaptiveInstance.Document.GetElement(id) is ReferencePoint rp)
    //                coords.Add(rp.Position);
    //        }
    //        return coords;
    //    }
    //    private XYZ Get2DSegmentIntersection(XYZ p1, XYZ p2, XYZ q1, XYZ q2)
    //    {
    //        // 2D点 (X,Y)
    //        double x1 = p1.X, y1 = p1.Y;
    //        double x2 = p2.X, y2 = p2.Y;
    //        double x3 = q1.X, y3 = q1.Y;
    //        double x4 = q2.X, y4 = q2.Y;
    //        // 平行或共线判断
    //        double denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
    //        if (Math.Abs(denominator) < 1e-9) return null;
    //        double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator;
    //        double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denominator;
    //        if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
    //        {
    //            // 相交点
    //            double x = x1 + ua * (x2 - x1);
    //            double y = y1 + ua * (y2 - y1);
    //            return new XYZ(x, y, 0);
    //        }
    //        return null;
    //    }
    //    private (int segIndex, double distToStart, double sumToStart) CheckIntersectionWithAdaptive(DetailLine doorLine, FamilyInstance adaptive, ViewPlan view)
    //    {
    //        if (doorLine == null) return (-1, 0, 0);
    //        List<XYZ> adaptivePoints = GetAdaptivePointPath(adaptive);
    //        if (adaptivePoints == null || adaptivePoints.Count < 2) return (-1, 0, 0);

    //        double totalLength = 0.0;
    //        for (int i = 0; i < adaptivePoints.Count - 1; i++)
    //        {
    //            XYZ p1 = adaptivePoints[i];
    //            XYZ p2 = adaptivePoints[i + 1];
    //            XYZ intersect = Get2DSegmentIntersection(doorLine.GeometryCurve.GetEndPoint(0), doorLine.GeometryCurve.GetEndPoint(1), p1, p2);
    //            if (intersect != null)
    //            {
    //                // 计算从p1到交点的距离
    //                double dist = Math.Sqrt((intersect.X - p1.X) * (intersect.X - p1.X) + (intersect.Y - p1.Y) * (intersect.Y - p1.Y));
    //                // 计算累计长度
    //                double sumDist = totalLength + dist;
    //                return (i, dist, sumDist);
    //            }
    //            else
    //            {
    //                totalLength += p1.DistanceTo(p2);
    //            }
    //        }
    //        return (-1, 0, 0);
    //    }
    //    List<ElementId> lineIds = new List<ElementId>();
    //    private Solid GetElementSolid(Element element)
    //    {
    //        return element.get_Geometry(new Options())?.OfType<Solid>().FirstOrDefault(s => s?.Volume > 0);
    //    }
    //    //public ICommand PickRoutesCommand => new RelayCommand<AdaptiveRouteEntity>(PickViewRoutes);
    //    public ICommand PickRoutesCommand => new BaseBindingCommand(PickViewRoutes);
    //    private void PickViewRoutes(Object para)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            Selection select = uIDoc.Selection;
    //            if (ActiveView.GetType().Name != "ViewPlan")
    //            {
    //                TaskDialog.Show("tt", "请在平面操作本功能");
    //                return;
    //            }
    //            Level currentLevel = ActiveView.GenLevel;
    //            if (currentLevel == null) return;
    //            string currentLevelName = currentLevel.Name;
    //            var currentLevelInstances = new List<ElementId>();
    //            foreach (var item in AllRoutes)
    //            {
    //                if (item.levelName == currentLevelName)
    //                {
    //                    currentLevelInstances.Add(item.Id);
    //                }
    //            }
    //            select.SetElementIds(currentLevelInstances);
    //            TaskDialog.Show("tt", $"选中{currentLevelInstances.Count().ToString()}个对象");
    //        });
    //    }
    //    public ICommand PickRouteCommand => new RelayCommand<AdaptiveRouteEntity>(PickRoute);
    //    private void PickRoute(AdaptiveRouteEntity entity)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            Selection select = uIDoc.Selection;
    //            var currentLevelInstances = new List<ElementId>();
    //            currentLevelInstances.Add(entity.Id);
    //            select.SetElementIds(currentLevelInstances);
    //        });
    //    }
    //    public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
    //    private void QueryELement(string obj)
    //    {
    //        AllRoutes.Clear();
    //        var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
    //        var familyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
    //            .Where(e => routeSymbols.Any(s => s.Id == e.Symbol.Id)).ToList();
    //        foreach (var item in familyInstances)
    //        {
    //            string levelName = item.LookupParameter("楼层标高").AsString();
    //            if (string.IsNullOrEmpty(obj) || levelName.Contains(obj) || levelName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || item.Symbol.Family.Name.Contains(obj))
    //            {
    //                AdaptiveRouteEntity routeEntity = new AdaptiveRouteEntity(item);
    //                AllRoutes.Add(routeEntity);
    //            }
    //        }
    //    }
    //    private ObservableCollection<AdaptiveRouteEntity> allRoutes = new ObservableCollection<AdaptiveRouteEntity>();
    //    public ObservableCollection<AdaptiveRouteEntity> AllRoutes
    //    {
    //        get => allRoutes;
    //        set => SetProperty(ref allRoutes, value);
    //    }
    //}
}
