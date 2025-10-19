using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// CircleGaugePlaceView.xaml 的交互逻辑
    /// </summary>
    public partial class CircleGaugePlaceView : Window
    {
        public CircleGaugePlaceView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new CircleGaugePlaceViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class CircleGaugePlaceViewModel : ObserverableObject
    {
        public static Document Document;
        public View ActiveView;
        public UIDocument uiDoc;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public CircleGaugePlaceViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            ActiveView = uiApp.ActiveUIDocument.ActiveView;
            uiDoc = uiApp.ActiveUIDocument;
            if (ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
                return;
            }
            else LevelCode = ActiveView.GenLevel.Name;
            PlaceGaugeCommand = new BaseBindingCommand(PlaceGauge, canExecute: _ => this.Instance != null);
            GetAdaptiveCircleCommand = new BaseBindingCommand(GetAdaptiveCircle);
        }
        public ICommand PlaceGaugeCommand { get; }
        public ICommand GetAdaptiveCircleCommand { get; }
        private void PlaceGauge(object obj)
        {
            // 3. 提取点的坐标
            List<XYZ> points = new List<XYZ>();
            IList<ElementId> placementPointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(Instance);
            if (placementPointIds.Count <= 2) return;
            foreach (ElementId pointId in placementPointIds)
            {
                ReferencePoint refPoint = Document.GetElement(pointId) as ReferencePoint;
                if (refPoint != null) points.Add(refPoint.Position);
            }
            if (points.Count != placementPointIds.Count)
            {
                //message = "无法成功提取所有放置点的坐标。";
                return;
            }
            // 4. 执行通用几何检查
            bool isSelfIntersecting = CheckPolygonSelfIntersection(points); // 调用新的通用检查方法
            WindingOrder order = WindingOrder.Non;
            if (!isSelfIntersecting) order = CheckWindingOrder(points);
            _externalHandler.Run(app =>
            {
                if (!isSelfIntersecting && order != WindingOrder.Non && order != WindingOrder.Collinear)
                {
                    // 1. 获取纯几何分析计划
                    List<PlacementInfo> placementPlan = GeneratePlacementPlan(points, order);
                    Level level = ActiveView.GenLevel;
                    if (level == null)
                    {
                        TaskDialog.Show("错误", "无法确定有效的标高。");
                        return;
                    }
                    using (Transaction tx = new Transaction(Document, "智能放置并设置参数"))
                    {
                        tx.Start();
                        // 2. 准备族名映射 (逻辑不变)
                        string convexFamilyName, concaveFamilyName;
                        string straightFamilyName = "线性测试2";
                        const string familyA = "角测试4";
                        const string familyB = "角测试4A";
                        if (order == WindingOrder.Clockwise)
                        {
                            //TaskDialog.Show("方向信息", "当前为顺时针环路。");
                            convexFamilyName = familyA;
                            concaveFamilyName = familyB;
                        }
                        else // CounterClockwise
                        {
                            //TaskDialog.Show("方向信息", "当前为逆时针环路。");
                            convexFamilyName = familyB;
                            concaveFamilyName = familyA;
                        }
                        // 用于存储已创建的实例，与 placementPlan 的索引一一对应
                        var createdInstances = new List<FamilyInstance>(new FamilyInstance[placementPlan.Count]);
                        // 创建所有族实例，不设置d0/d1
                        for (int i = 0; i < placementPlan.Count; i++)
                        {
                            var info = placementPlan[i];
                            string familyNameToPlace = "";
                            switch (info.Type)
                            {
                                case PlacementType.Straight: familyNameToPlace = straightFamilyName; break;
                                case PlacementType.ConvexCorner: familyNameToPlace = convexFamilyName; break;
                                case PlacementType.ConcaveCorner: familyNameToPlace = concaveFamilyName; break;
                                case PlacementType.UnhandledCorner: continue;
                            }
                            // 查找和激活族符号 (您的代码)
                            FamilySymbol symbolToPlace = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(q => q.Name == familyNameToPlace);
                            if (symbolToPlace == null)
                            {
                                TaskDialog.Show("错误", $"未找到名为 '{familyNameToPlace}' 的族类型，请先载入。");
                                tx.RollBack(); return;
                            }
                            if (!symbolToPlace.IsActive)
                            {
                                symbolToPlace.Activate(); Document.Regenerate();
                            }
                            FamilyInstance newInstance = null;
                            if (info.Type == PlacementType.Straight)
                            {
                                if (info.GeometryCurve != null)
                                {
                                    newInstance = Document.Create.NewFamilyInstance(info.GeometryCurve, symbolToPlace, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                    newInstance.LookupParameter("width1").Set(GaugeWidth / 304.8);
                                }
                            }
                            else // 角点族
                            {
                                newInstance = Document.Create.NewFamilyInstance(info.Position, symbolToPlace, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                Line rotationAxis = Line.CreateBound(info.Position, info.Position + XYZ.BasisZ);
                                ElementTransformUtils.RotateElement(Document, newInstance.Id, rotationAxis, info.RotationInRadians);
                                newInstance.LookupParameter("width1").Set(GaugeWidth / 304.8);
                                newInstance.LookupParameter("width2").Set(GaugeWidth / 304.8);
                            }
                            newInstance.LookupParameter("depth").Set(GaugeHeight / 304.8);
                            newInstance.LookupParameter("d").Set(GaugeWallThick / 304.8);
                            if (HasBase)
                            {
                                newInstance.LookupParameter("hasBase").Set(1);
                            }
                            else newInstance.LookupParameter("hasBase").Set(0);
                            createdInstances[i] = newInstance;
                        }
                        Document.Regenerate();
                        //根据角点类型，设置相邻直线的 d0/d1 参数
                        double d_mm = GaugeWallThick;
                        double width1_mm = GaugeWidth;
                        double smallRetraction = (d_mm) / 304.8;
                        double largeRetraction = (width1_mm + d_mm) / 304.8;
                        // placementPlan 的结构是 [角0, 直线0, 角1, 直线1, ...]
                        // 我们只遍历角点 (索引 0, 2, 4, ...)
                        for (int i = 0; i < placementPlan.Count; i += 2)
                        {
                            var cornerInfo = placementPlan[i];
                            // 找到这个角点前后的直线段实例
                            // 前一条直线索引: (i - 1 + 总数) % 总数,  处理第一个角点前的环绕情况
                            int incomingLineIndex = (i - 1 + placementPlan.Count) % placementPlan.Count;
                            // 后一条直线索引: i + 1
                            int outgoingLineIndex = i + 1;
                            FamilyInstance incomingLineInst = createdInstances[incomingLineIndex];
                            FamilyInstance outgoingLineInst = createdInstances[outgoingLineIndex];
                            if (incomingLineInst == null || outgoingLineInst == null) continue;
                            if (order == WindingOrder.Clockwise)
                            {
                                // 阳角
                                if (cornerInfo.Type == PlacementType.ConvexCorner)
                                {
                                    incomingLineInst.LookupParameter("d1")?.Set(smallRetraction); // 前一条直线的 d1
                                    outgoingLineInst.LookupParameter("d0")?.Set(smallRetraction); // 后一条直线的 d0
                                }
                                else if (cornerInfo.Type == PlacementType.ConcaveCorner)
                                {
                                    incomingLineInst.LookupParameter("d1")?.Set(largeRetraction); // 前一条直线的 d1
                                    outgoingLineInst.LookupParameter("d0")?.Set(largeRetraction); // 后一条直线的 d0
                                }
                            }
                            else
                            {
                                if (cornerInfo.Type == PlacementType.ConcaveCorner)
                                {
                                    incomingLineInst.LookupParameter("d1")?.Set(smallRetraction);
                                    outgoingLineInst.LookupParameter("d0")?.Set(smallRetraction);
                                }
                                else if (cornerInfo.Type == PlacementType.ConvexCorner)
                                {
                                    incomingLineInst.LookupParameter("d1")?.Set(largeRetraction);
                                    outgoingLineInst.LookupParameter("d0")?.Set(largeRetraction);
                                }
                            }
                        }
                        tx.Commit();
                    }
                }
            });
        }
        //private void GetAdaptiveCircle(object obj)
        //{
        //    TaskDialog.Show("tt", HasBase.ToString());
        //}
        private void GetAdaptiveCircle(object obj)
        {
            try
            {
                var selectedRef = uiDoc.Selection.PickObject(ObjectType.Element, new AdaptiveFamilyFilter(), "请选择一个自适应构件进行检查");
                FamilyInstance inst = Document.GetElement(selectedRef) as FamilyInstance;
                //canPlaceGauge = true;
                this.Instance = inst;
                //TaskDialog.Show("tt", "PASS");
            }
            catch (Exception)
            {
                throw;
            }
        }
        public FamilyInstance _instance;
        public FamilyInstance Instance // 建议属性名大写开头
        {
            get { return _instance; }
            private set
            {
                // 只有当值真正改变时才继续，避免不必要的更新
                if (_instance != value)
                {
                    _instance = value;
                    OnPropertyChanged();
                    // 重要. (必须)通知PlaceGaugeCommand，它的可执行状态可能已改变！
                    (PlaceGaugeCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        /// 定义一个枚举来表示方向
        public enum WindingOrder { Clockwise, CounterClockwise, Collinear, Non }
        public enum PolygonType { Convex, Concave, Non }
        public enum PlacementType { Straight, ConvexCorner, ConcaveCorner, UnhandledCorner }
        /// <summary>
        /// 辅助方法：检查两条【线段】(seg1_p1, seg1_p2) 和 (seg2_p1, seg2_p2) 是否相交。
        /// 关键：只考虑严格的内部相交，忽略在端点处的接触。
        /// </summary>
        private bool DoSegmentsIntersect(XYZ seg1_p1, XYZ seg1_p2, XYZ seg2_p1, XYZ seg2_p2)
        {
            // 创建代表两条线段的有界线
            Line segment1 = Line.CreateBound(seg1_p1, seg1_p2);
            Line segment2 = Line.CreateBound(seg2_p1, seg2_p2);
            // 调用Revit API的相交方法
            IntersectionResultArray resultArray;
            SetComparisonResult result = segment1.Intersect(segment2, out resultArray);
            // 如果结果是 Disjoint (不相交)，则肯定没有交叉
            if (result == SetComparisonResult.Disjoint) return false;
            // 如果结果是 Overlap 或 Subset，则有相交。我们需要确认交点不是端点。
            if (resultArray == null || resultArray.IsEmpty)
            {
                // 这种情况通常意味着它们共线但不重叠，或只在端点接触
                // 为了安全起见，我们认为这不构成“蝴蝶形”交叉
                return false;
            }
            // 遍历所有交点，检查是否存在一个“内部”交点。容差tolerance，用于比较浮点数
            foreach (IntersectionResult intResult in resultArray)
            {
                XYZ intersectionPoint = intResult.XYZPoint;
                double tolerance = 1e-9;
                // 检查交点是否严格位于第一，二条线段的内部
                bool onSegment1Internal = intersectionPoint.DistanceTo(seg1_p1) > tolerance && intersectionPoint.DistanceTo(seg1_p2) > tolerance;
                bool onSegment2Internal = intersectionPoint.DistanceTo(seg2_p1) > tolerance && intersectionPoint.DistanceTo(seg2_p2) > tolerance;
                // 如果交点同时位于两条线段的内部，那么这就是一个真正的自交叉点
                if (onSegment1Internal && onSegment2Internal) return true;
            }
            return false;
        }
        /// <summary>
        /// 检查点的顺序是顺时针还是逆时针（基于鞋带公式）
        /// </summary>
        private WindingOrder CheckWindingOrder(IList<XYZ> points)
        {
            if (points.Count < 3) return WindingOrder.Collinear;
            // 将3D点投影到XY平面进行2D计算
            double signedArea = 0;
            for (int i = 0; i < points.Count; i++)
            {
                XYZ p1 = points[i];
                // 获取下一个点，如果是最后一个点，则下一个是第一个点
                XYZ p2 = points[(i + 1) % points.Count];
                signedArea += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            // 使用容差判断是否共线
            if (Math.Abs(signedArea) < 1e-9) return WindingOrder.Collinear;
            // 在标准的数学坐标系（Y轴向上）中，正面积是逆时针,右手定则，反之为负
            else if (signedArea > 0) return WindingOrder.CounterClockwise;
            else return WindingOrder.Clockwise;
        }
        /// <summary>
        /// 检查一个 N 点多边形是否自交叉。
        /// 采用 O(n^2) 算法，检查每条边是否与其它非相邻边相交。
        /// </summary>
        private bool CheckPolygonSelfIntersection(IList<XYZ> points)
        {
            int n = points.Count;
            if (n <= 3) return false;
            // 外层循环遍历第一条边
            for (int i = 0; i < n; i++)
            {
                XYZ p1 = points[i];
                // 使用 % n 来处理从最后一个点到第一个点的闭合边
                XYZ p2 = points[(i + 1) % n];
                // 内层循环遍历第二条边，从 i+1 开始避免重复检查和检查自身
                for (int j = i + 1; j < n; j++)
                {
                    XYZ p3 = points[j];
                    XYZ p4 = points[(j + 1) % n];
                    // **关键**: 检查两条边是否是相邻的。如果是，则跳过。
                    // 相邻的情况: p2 和 p3 是同一个点，或者 p1 和 p4 是同一个点。
                    if (p2.IsAlmostEqualTo(p3) || p1.IsAlmostEqualTo(p4)) continue;
                    // 调用基础的线段相交检查方法，只要找到一对交叉，就可以立即返回 true
                    if (DoSegmentsIntersect(p1, p2, p3, p4)) return true;
                }
            }
            return false;
        }
        // 用于浮点数比较的容差
        private const double TOLERANCE = 1e-6;
        /// <summary>
        /// 从顶点列表中移除共线的中间点，找到真正的转角。
        /// </summary>
        /// <param name="points">原始顶点列表。</param>
        /// <returns>只包含“真”转角的顶点列表。</returns>
        private static List<XYZ> SimplifyCollinearPoints(List<XYZ> points)
        {
            if (points.Count < 3) return points;
            List<XYZ> simplifiedPoints = new List<XYZ>();
            int numPoints = points.Count;
            for (int i = 0; i < numPoints; i++)
            {
                XYZ pPrev = points[(i - 1 + numPoints) % numPoints];
                XYZ pCurr = points[i];
                XYZ pNext = points[(i + 1) % numPoints];
                // 使用2D叉乘判断三点是否共线 (忽略Z轴)
                double crossProduct = (pCurr.X - pPrev.X) * (pNext.Y - pCurr.Y) -
                                      (pCurr.Y - pPrev.Y) * (pNext.X - pCurr.X);

                if (Math.Abs(crossProduct) > TOLERANCE)
                {
                    // 如果不共线，则当前点是一个转角
                    simplifiedPoints.Add(pCurr);
                }
            }
            return simplifiedPoints;
        }
        public static List<PlacementInfo> GeneratePlacementPlan(List<XYZ> points, WindingOrder order)
        {
            var placementPlan = new List<PlacementInfo>();
            List<XYZ> cornerPoints = SimplifyCollinearPoints(points);
            if (cornerPoints.Count < 3) return placementPlan;
            int numCorners = cornerPoints.Count;
            // 我们的放置计划将严格遵循 角点 -> 直线 -> 角点 -> 直线 的顺序
            for (int i = 0; i < numCorners; i++)
            {
                XYZ pPrev = cornerPoints[(i - 1 + numCorners) % numCorners];
                XYZ pCurr = cornerPoints[i];
                XYZ pNext = cornerPoints[(i + 1) % numCorners];
                // 1. 分析并添加角点构件的放置信息
                XYZ vec1 = (pCurr - pPrev).Normalize();
                XYZ vec2 = (pNext - pCurr).Normalize();
                PlacementType cornerType;
                if (Math.Abs(vec1.DotProduct(vec2)) < 1e-6) // 检查是否为直角
                {
                    double crossProductZ = vec1.X * vec2.Y - vec1.Y * vec2.X;
                    cornerType = ((order == WindingOrder.CounterClockwise && crossProductZ > 0) || (order == WindingOrder.Clockwise && crossProductZ < 0))
                        ? PlacementType.ConvexCorner
                        : PlacementType.ConcaveCorner;

                    XYZ bisectorVec = vec2 - vec1;
                    double cornerRotation = Math.Atan2(bisectorVec.Y, bisectorVec.X);
                    placementPlan.Add(new PlacementInfo(cornerType, pCurr, cornerRotation));
                }
                else
                {
                    placementPlan.Add(new PlacementInfo(PlacementType.UnhandledCorner, pCurr, 0));
                }
                // 2. 添加【全尺寸】直线段构件的放置信息
                Line fullLine = Line.CreateBound(pCurr, pNext);
                placementPlan.Add(new PlacementInfo(PlacementType.Straight, fullLine));
            }
            return placementPlan;
        }
        public string LevelCode { get; set; }
        public int GaugeWidth { get; set; } = 400;
        public int GaugeHeight { get; set; } = 600;
        public int GaugeWallThick { get; set; } = 150;
        private bool _hasBase = true;
        public bool HasBase
        {
            get => _hasBase;
            set => SetProperty(ref _hasBase, value);
        }
    }
}
