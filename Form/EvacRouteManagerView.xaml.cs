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
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public Autodesk.Revit.DB.View ActiveView { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public EvacRouteManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            ActiveView = Document.ActiveView;
            var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
            if (routeSymbols.Count() != 4) TaskDialog.Show("错误", "未找到指定的自适应族");
            QueryELement(null);
            List<Line> doorLines = new List<Line>();

        }
        public ICommand IntersectDoorDistanceCommand => new RelayCommand<AdaptiveRouteEntity>(InterSectDoor);
        private void InterSectDoor(AdaptiveRouteEntity entity)
        {
            _externalHandler.Run(app =>
            {
                using (Transaction trans = new Transaction(Document, "检测路径与门关系"))
                {
                    trans.Start();

                    IReadOnlyList<ElementId> nearDoors = entity.Doors;
                    List<string> results = new List<string>();   // 汇总信息
                    foreach (var item in nearDoors)
                    {
                        FamilyInstance instance = Document.GetElement(item) as FamilyInstance;
                        double width = instance.Symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM).AsDouble();
                        // 取门/窗线
                        LocationCurve locCurve = instance.Location as LocationCurve;
                        LocationPoint locPoint = instance.Location as LocationPoint;
                        XYZ pt1, pt2;
                        if (locCurve != null)
                        {
                            Curve curve1 = locCurve.Curve;
                            pt1 = curve1.GetEndPoint(0);
                            pt2 = curve1.GetEndPoint(1);
                        }
                        else
                        {
                            XYZ origin = locPoint.Point;
                            XYZ dir = instance.FacingOrientation;
                            if (dir == null || dir.IsZeroLength()) dir = XYZ.BasisX;
                            dir = dir.Normalize();
                            XYZ perpDir = new XYZ(-dir.Y, dir.X, 0).Normalize();
                            pt1 = origin - perpDir * width / 2;
                            pt2 = origin + perpDir * width / 2;
                        }
                        Line resultLine = Line.CreateBound(pt1, pt2);
                        DetailLine detailLine = Document.Create.NewDetailCurve(ActiveView, resultLine) as DetailLine;
                        // 判断相交并收集信息
                        var (segIdx, dist, sum) = CheckIntersectionWithAdaptive(detailLine, entity.AdaptiveInstance, (ViewPlan)ActiveView);
                        if (segIdx >= 0)
                        {
                            results.Add(
                                $"门/窗 {instance.Name}：交点在第 {segIdx + 1} 段，" +
                                $"距近点{dist * 304.8:F2} mm，累计距首点{(sum * 304.8):F2} mm");
                        }
                        Document.Delete(detailLine.Id);
                    }
                    // 一次性弹窗
                    if (results.Count == 0) TaskDialog.Show("结果", "无交点");
                    else TaskDialog.Show("交点汇总", string.Join("\n", results));
                    trans.Commit();
                }
            });
        }
        private List<XYZ> GetAdaptivePointPath(FamilyInstance adaptiveInstance)
        {
            IList<ElementId> ids = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(adaptiveInstance);
            List<XYZ> coords = new List<XYZ>();
            foreach (ElementId id in ids)
            {
                if (adaptiveInstance.Document.GetElement(id) is ReferencePoint rp)
                    coords.Add(rp.Position);
            }
            return coords;
        }
        private XYZ Get2DSegmentIntersection(XYZ p1, XYZ p2, XYZ q1, XYZ q2)
        {
            // 2D点 (X,Y)
            double x1 = p1.X, y1 = p1.Y;
            double x2 = p2.X, y2 = p2.Y;
            double x3 = q1.X, y3 = q1.Y;
            double x4 = q2.X, y4 = q2.Y;
            // 平行或共线判断
            double denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (Math.Abs(denominator) < 1e-9) return null;
            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denominator;
            if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
            {
                // 相交点
                double x = x1 + ua * (x2 - x1);
                double y = y1 + ua * (y2 - y1);
                return new XYZ(x, y, 0);
            }
            return null;
        }
        private (int segIndex, double distToStart, double sumToStart) CheckIntersectionWithAdaptive(DetailLine doorLine, FamilyInstance adaptive, ViewPlan view)
        {
            if (doorLine == null) return (-1, 0, 0);
            List<XYZ> adaptivePoints = GetAdaptivePointPath(adaptive);
            if (adaptivePoints == null || adaptivePoints.Count < 2) return (-1, 0, 0);

            double totalLength = 0.0;
            for (int i = 0; i < adaptivePoints.Count - 1; i++)
            {
                XYZ p1 = adaptivePoints[i];
                XYZ p2 = adaptivePoints[i + 1];
                XYZ intersect = Get2DSegmentIntersection(doorLine.GeometryCurve.GetEndPoint(0), doorLine.GeometryCurve.GetEndPoint(1), p1, p2);
                if (intersect != null)
                {
                    // 计算从p1到交点的距离
                    double dist = Math.Sqrt((intersect.X - p1.X) * (intersect.X - p1.X) + (intersect.Y - p1.Y) * (intersect.Y - p1.Y));
                    // 计算累计长度
                    double sumDist = totalLength + dist;
                    return (i, dist, sumDist);
                }
                else
                {
                    totalLength += p1.DistanceTo(p2);
                }
            }
            return (-1, 0, 0);
        }
        List<ElementId> lineIds = new List<ElementId>();
        private Solid GetElementSolid(Element element)
        {
            return element.get_Geometry(new Options())?.OfType<Solid>().FirstOrDefault(s => s?.Volume > 0);
        }
        //public ICommand PickRoutesCommand => new RelayCommand<AdaptiveRouteEntity>(PickViewRoutes);
        public ICommand PickRoutesCommand => new BaseBindingCommand(PickViewRoutes);
        private void PickViewRoutes(Object para)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                if (ActiveView.GetType().Name != "ViewPlan")
                {
                    TaskDialog.Show("tt", "请在平面操作本功能");
                    return;
                }
                Level currentLevel = ActiveView.GenLevel;
                if (currentLevel == null) return;
                string currentLevelName = currentLevel.Name;
                var currentLevelInstances = new List<ElementId>();
                foreach (var item in AllRoutes)
                {
                    if (item.levelName == currentLevelName)
                    {
                        currentLevelInstances.Add(item.Id);
                    }
                }
                select.SetElementIds(currentLevelInstances);
                TaskDialog.Show("tt", $"选中{currentLevelInstances.Count().ToString()}个对象");
            });
        }
        public ICommand PickRouteCommand => new RelayCommand<AdaptiveRouteEntity>(PickRoute);
        private void PickRoute(AdaptiveRouteEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                var currentLevelInstances = new List<ElementId>();
                currentLevelInstances.Add(entity.Id);
                select.SetElementIds(currentLevelInstances);
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            AllRoutes.Clear();
            var routeSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
            var familyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(e => routeSymbols.Any(s => s.Id == e.Symbol.Id)).ToList();
            foreach (var item in familyInstances)
            {
                string levelName = item.LookupParameter("楼层标高").AsString();
                if (string.IsNullOrEmpty(obj) || levelName.Contains(obj) || levelName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || item.Symbol.Family.Name.Contains(obj))
                {
                    AdaptiveRouteEntity routeEntity = new AdaptiveRouteEntity(item);
                    AllRoutes.Add(routeEntity);
                }
            }
        }
        private ObservableCollection<AdaptiveRouteEntity> allRoutes = new ObservableCollection<AdaptiveRouteEntity>();
        public ObservableCollection<AdaptiveRouteEntity> AllRoutes
        {
            get => allRoutes;
            set => SetProperty(ref allRoutes, value);
        }
    }
}
