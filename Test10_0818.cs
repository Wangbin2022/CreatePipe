using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using CreatePipe.ViewFilters;
using CreatePipe.WpfDirectoryTreeView;
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
        /// 定义一个枚举来表示方向
        public enum WindingOrder { Clockwise, CounterClockwise, Collinear, Non }
        public enum PolygonType { Convex, Concave, Non }
        /// 检查四边形是否自交叉（通过判断对角线是否相交）,这里简化为检查对边P1P2和P3P4是否相交
        private bool CheckSelfIntersection(IList<XYZ> points)
        {
            if (points.Count != 4) throw new ArgumentException("需要4个点来检查。");
            XYZ p1 = points[0];
            XYZ p2 = points[1];
            XYZ p3 = points[2];
            XYZ p4 = points[3];
            // 检查第一对对边: (P1-P2) 和 (P3-P4)
            bool pair1Intersects = DoSegmentsIntersect(p1, p2, p3, p4);
            // 检查第二对对边: (P2-P3) 和 (P4-P1)
            bool pair2Intersects = DoSegmentsIntersect(p2, p3, p4, p1);
            return pair1Intersects || pair2Intersects;
        }
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
        /// 查找指定的自适应构件族符号
        /// </summary>
        //private FamilySymbol FindAdaptiveFamilySymbol(Document doc)
        //{
        //    return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_GenericModel).Cast<FamilySymbol>().FirstOrDefault(fs =>
        //            fs.Family.Name == ADAPTIVE_FAMILY_NAME && fs.Name == ADAPTIVE_TYPE_NAME);
        //}
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
        /// <summary>
        /// 检查一个简单多边形是凸多边形还是凹多边形。
        /// 前提：该多边形已知不自相交。
        /// </summary>
        /// <param name="points">多边形的顶点列表</param>
        /// <param name="order">预先计算好的多边形顶点顺序 (CW or CCW)</param>
        /// <returns>返回 PolygonType 枚举 (Convex, Concave, or Collinear)</returns>
        private PolygonType CheckPolygonConvexity(IList<XYZ> points, WindingOrder order)
        {
            int n = points.Count;
            // 遍历每一个顶点以检查其角度
            for (int i = 0; i < n; i++)
            {
                // 获取当前顶点以及其前后两个顶点
                XYZ p_prev = points[(i + n - 1) % n]; // 前一个点
                XYZ p_curr = points[i];               // 当前点
                XYZ p_next = points[(i + 1) % n];     // 下一个点
                // 创建从当前顶点出发的两个向量 (投影到XY平面)
                // v1: 从前一个点到当前点
                double v1_x = p_curr.X - p_prev.X;
                double v1_y = p_curr.Y - p_prev.Y;
                // v2: 从当前点到下一个点
                double v2_x = p_next.X - p_curr.X;
                double v2_y = p_next.Y - p_curr.Y;
                // 计算2D叉积的Z分量
                double crossProductZ = v1_x * v2_y - v1_y * v2_x;
                // 根据顶点顺序 (WindingOrder) 和叉积符号来判断
                // Math.Sign() 返回 -1, 0, or 1，可以简化逻辑
                int turnDirection = Math.Sign(crossProductZ);
                if (turnDirection == 0) continue; // 共线，继续检查下一个点
                if (order == WindingOrder.CounterClockwise)
                {
                    // 对于逆时针多边形，任何一个“右转”(叉积为负)都意味着凹角
                    if (turnDirection < 0)
                    {
                        return PolygonType.Concave; // 找到凹角，立即返回
                    }
                }
                else // WindingOrder is Clockwise
                {
                    // 对于顺时针多边形，任何一个“左转”(叉积为正)都意味着凹角
                    if (turnDirection > 0)
                    {
                        return PolygonType.Concave; // 找到凹角，立即返回
                    }
                }
            }
            // 如果循环完成，没有找到任何凹角，则该多边形是凸的
            return PolygonType.Convex;
        }
        // 首先，定义一个枚举来清晰地表示当前的状态
        private enum OperationMode { Add, Subtract }
        /// 根据特定规则计算多边形的“条件周长”。
        private double CalculateSpecialPerimeter(IList<XYZ> points, WindingOrder order)
        {
            // 如果是共线或点太少，无法形成有效区域，按标准周长处理
            if (points.Count < 3 || order == WindingOrder.Collinear)
            {
                double simplePerimeter = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    simplePerimeter += points[i].DistanceTo(points[(i + 1) % points.Count]);
                }
                return simplePerimeter;
            }
            double totalLength = 0;
            int n = points.Count;
            // 初始状态为加法模式
            OperationMode currentMode = OperationMode.Add;
            // 遍历每一条边
            for (int i = 0; i < n; i++)
            {
                // 1. 获取当前边的端点
                XYZ p_curr = points[i];
                XYZ p_next = points[(i + 1) % n];
                // 2. 计算当前边的长度
                double edgeLength = p_curr.DistanceTo(p_next);
                // 3. 根据当前模式执行加或减
                if (currentMode == OperationMode.Add)
                {
                    totalLength += edgeLength;
                }
                else // currentMode == OperationMode.Subtract
                {
                    totalLength -= edgeLength;
                }
                // --- 4. 决策：决定下一条边的操作模式 ---
                // 我们需要 p_curr, p_next, 和 p_after_next 来判断在 p_next 处的拐角
                XYZ p_after_next = points[(i + 2) % n];
                // 创建从 p_next 顶点出发的两个向量
                // v1: 从 p_curr 到 p_next
                double v1_x = p_next.X - p_curr.X;
                double v1_y = p_next.Y - p_curr.Y;
                // v2: 从 p_next 到 p_after_next
                double v2_x = p_after_next.X - p_next.X;
                double v2_y = p_after_next.Y - p_next.Y;
                // 计算2D叉积
                double crossProductZ = v1_x * v2_y - v1_y * v2_x;
                int turnDirection = Math.Sign(crossProductZ);
                // 如果三点共线，不改变当前模式
                if (turnDirection == 0) continue;
                bool isConcave = false;
                if (order == WindingOrder.CounterClockwise)
                {
                    // 对于逆时针多边形，“右转”是凹角
                    if (turnDirection < 0) isConcave = true;
                }
                else // order == WindingOrder.Clockwise
                {
                    // 对于顺时针多边形，“左转”是凹角
                    if (turnDirection > 0) isConcave = true;
                }
                // 根据拐角性质更新模式
                if (isConcave)
                {
                    currentMode = OperationMode.Subtract;
                }
                else
                {
                    currentMode = OperationMode.Add;
                }
            }
            return totalLength;
        }
        //改方法为拾取自适应环，基于起终点布置线性族 
        private FamilySymbol FindAndActivateFamilySymbol(Document doc, string symbolName)
        {
            // 使用 FilteredElementCollector 查找族类型
            FamilySymbol symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().FirstOrDefault(s => s.Name == symbolName);
            doc.NewTransaction(() =>
            {
                if (symbol != null && !symbol.IsActive)
                {
                    symbol.Activate();
                    doc.Regenerate(); // 激活后最好重生一下文档
                }
            }, "预处理");
            return symbol;
        }
        /// <summary>
        /// 检查一个由三点定义的拐角是否为凹角。
        /// </summary>
        /// <param name="p_prev">前一个点</param>
        /// <param name="p_curr">当前角点</param>
        /// <param name="p_next">后一个点</param>
        /// <param name="order">多边形的环绕方向</param>
        /// <returns>如果为凹角则返回 true，否则返回 false。</returns>
        private bool IsCornerConcave(XYZ p_prev, XYZ p_curr, XYZ p_next, WindingOrder order)
        {
            // 创建向量
            double v1_x = p_curr.X - p_prev.X;
            double v1_y = p_curr.Y - p_prev.Y;
            double v2_x = p_next.X - p_curr.X;
            double v2_y = p_next.Y - p_curr.Y;
            // 计算2D叉积
            double crossProductZ = v1_x * v2_y - v1_y * v2_x;
            int turnDirection = Math.Sign(crossProductZ);
            if (turnDirection == 0) return false;
            if (order == WindingOrder.CounterClockwise)
            {
                // 逆时针时，"右转" (叉积<0) 是凹角
                return turnDirection < 0;
            }
            else // order == WindingOrder.Clockwise
            {
                // 顺时针时，"左转" (叉积>0) 是凹角
                return turnDirection > 0;
            }
        }
        // 0916 新增一个枚举，用于清晰地表示拐角类型，处理加减不变的情况
        public enum CornerTurn { Convex, Concave, Collinear }
        /// <summary>
        /// 新增的辅助方法：判断三个点构成的拐角类型（凸、凹或共线）
        /// </summary>
        /// <param name="p1">前一个点</param>
        /// <param name="p2">当前拐角点</param>
        /// <param name="p3">后一个点</param>
        /// <param name="order">多边形的环绕顺序</param>
        /// <returns>拐角的类型</returns>
        private CornerTurn GetCornerType(XYZ p1, XYZ p2, XYZ p3, WindingOrder order)
        {
            // 将问题简化到XY平面上计算，忽略Z值差异
            XYZ v1 = new XYZ(p2.X - p1.X, p2.Y - p1.Y, 0).Normalize();
            XYZ v2 = new XYZ(p3.X - p2.X, p3.Y - p2.Y, 0).Normalize();
            // 使用二维向量的叉积来判断转向crossZ > 0: 左转< 0: 右转= 0: 共线
            double crossZ = v1.X * v2.Y - v1.Y * v2.X;
            // 使用一个小的容差来处理浮点数精度问题
            double tolerance = 1e-9;
            if (Math.Abs(crossZ) < tolerance) { return CornerTurn.Collinear; }
            // 根据多边形的环绕顺序来判断凹凸
            // 逆时针 (Counter-Clockwise): 左转是凸角, 右转是凹角
            // 顺时针 (Clockwise): 左转是凹角, 右转是凸角
            if (order == WindingOrder.CounterClockwise)
            { return crossZ > 0 ? CornerTurn.Convex : CornerTurn.Concave; }
            else // Clockwise
            { return crossZ > 0 ? CornerTurn.Concave : CornerTurn.Convex; }
        }
        //private const string ADAPTIVE_FAMILY_NAME = "4边环2020";
        //private const string ADAPTIVE_TYPE_NAME = "4边环";
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

            //0917 修改双击设置.没找到双击对话框api
            //RevitCommandId commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.);
            //uiApp.PostCommand(commandId);


            ////0913 拾取自适应环(通用)
            //var selectedRef = uiDoc.Selection.PickObject(ObjectType.Element, new AdaptiveFamilyFilter(), "请选择一个自适应构件进行检查");
            //FamilyInstance instance = doc.GetElement(selectedRef) as FamilyInstance;
            //// 3. 提取点的坐标
            //List<XYZ> points = new List<XYZ>();
            //IList<ElementId> placementPointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
            //if (placementPointIds.Count <= 2) return Result.Failed;
            //foreach (ElementId pointId in placementPointIds)
            //{
            //    ReferencePoint refPoint = doc.GetElement(pointId) as ReferencePoint;
            //    if (refPoint != null) points.Add(refPoint.Position);
            //}
            //if (points.Count != placementPointIds.Count)
            //{
            //    message = "无法成功提取所有放置点的坐标。";
            //    return Result.Failed;
            //}
            //// 4. 执行通用几何检查
            //bool isSelfIntersecting = CheckPolygonSelfIntersection(points); // 调用新的通用检查方法
            //WindingOrder order = WindingOrder.Non;
            //if (!isSelfIntersecting) order = CheckWindingOrder(points);
            ////增加逻辑，检查多边形凸凹
            //PolygonType polygonType = PolygonType.Non;
            ////double specialPerimeter = 0;
            //if (!isSelfIntersecting && order != WindingOrder.Non && order != WindingOrder.Collinear)
            //{
                //    polygonType = CheckPolygonConvexity(points, order);
                //    string convexFamilySymbolName = "Add";
                //    string concaveFamilySymbolName = "Minor";
                //    FamilySymbol symbolA = FindAndActivateFamilySymbol(doc, convexFamilySymbolName);
                //    FamilySymbol symbolB = FindAndActivateFamilySymbol(doc, concaveFamilySymbolName);
                //    if (symbolA == null || symbolB == null)
                //    {
                //        TaskDialog.Show("错误", $"错误：未能找到所需的线性族。请确保项目中已载入名为“{convexFamilySymbolName}”和“{concaveFamilySymbolName}”的族类型。");
                //        return Result.Failed;
                //    }
                //    Level level = doc.GetElement(doc.ActiveView.GenLevel.Id) as Level;
                //    if (level == null)
                //    {
                //        TaskDialog.Show("错误", "错误：无法确定有效的放置标高。");
                //        return Result.Failed;
                //    }
                //    int n = points.Count;
                //    // 阶段一：分析几何 - 确定每个顶点的拐角类型
                //    List<CornerTurn> cornerTypes = new List<CornerTurn>(n);
                //    for (int i = 0; i < n; i++)
                //    {
                //        XYZ prevPoint = points[(i + n - 1) % n]; // 前一个点
                //        XYZ currentPoint = points[i];             // 当前点 (即拐角点)
                //        XYZ nextPoint = points[(i + 1) % n];      // 后一个点
                //        cornerTypes.Add(GetCornerType(prevPoint, currentPoint, nextPoint, order));
                //    }
                //    using (Transaction tx = new Transaction(doc, "根据自适应构件布置线性族"))
                //    {
                //        tx.Start();
                //        try
                //        {
                //            for (int i = 0; i < n; i++)
                //            {
                //                // 定义当前边
                //                XYZ startPoint = points[i];
                //                XYZ endPoint = points[(i + 1) % n];
                //                Line edgeCurve = Line.CreateBound(startPoint, endPoint);
                //                // 获取这条边起点和终点的拐角类型
                //                CornerTurn startTurn = cornerTypes[i];
                //                CornerTurn endTurn = cornerTypes[(i + 1) % n];
                //                // 决策：选择族类型。我们沿用原始逻辑，根据终点拐角选择构件类型。
                //                // 这可能与族的设计有关（例如，凹角构件的特殊几何形状只在一端）。
                //                FamilySymbol symbolToUse = (endTurn == CornerTurn.Concave) ? symbolB : symbolA;
                //                // 创建族实例
                //                var gaugeInstance = doc.Create.NewFamilyInstance(edgeCurve, symbolToUse, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                //                double gaugeWidth = gaugeInstance.LookupParameter("偏移").AsDouble();
                //                // 核心修改：根据边的起点和终点的拐角类型，计算长度调整量
                //                double lengthAdjustment = 0.0;
                //                // 根据起点拐角调整
                //                if (startTurn == CornerTurn.Convex) { lengthAdjustment -= gaugeWidth; }
                //                else if (startTurn == CornerTurn.Concave)
                //                { lengthAdjustment += gaugeWidth; }
                //                // 如果 startTurn 是 Collinear，则 lengthAdjustment 不变 (为0)
                //                // 根据终点拐角调整
                //                if (endTurn == CornerTurn.Convex) { lengthAdjustment -= gaugeWidth; }
                //                else if (endTurn == CornerTurn.Concave) { lengthAdjustment += gaugeWidth; }
                //                // 如果 endTurn 是 Collinear，则 lengthAdjustment 不变
                //                // 设置最终的sideLength
                //                gaugeInstance.LookupParameter("sideLength").Set(edgeCurve.Length + lengthAdjustment);
                //            }
                //            tx.Commit();
                //            TaskDialog.Show("成功", $"已成功沿自适应构件边界布置了 {points.Count} 个线性构件。");
                //        }
                //        catch (Exception ex)
                //        {
                //            message = $"创建族实例时发生错误: {ex.Message}";
                //            tx.RollBack();
                //            TaskDialog.Show("创建失败", message);
                //            return Result.Failed;
                //        }
                //    }
                //    //例程结束
                //polygonType = CheckPolygonConvexity(points, order);
                ////计算条件长度，实际为判断平行边长度是加还是减
                ////specialPerimeter = CalculateSpecialPerimeter(points, order);
                //string convexFamilySymbolName = "Add"; // 用于凸角的族类型名称
                //string concaveFamilySymbolName = "Minor"; // 用于凹角的族类型名称
                //FamilySymbol symbolA = FindAndActivateFamilySymbol(doc, convexFamilySymbolName);
                //FamilySymbol symbolB = FindAndActivateFamilySymbol(doc, concaveFamilySymbolName);
                //// 检查族是否存在
                //if (symbolA == null || symbolB == null)
                //{
                //    TaskDialog.Show("错误", $"错误：未能找到所需的线性族。请确保项目中已载入名为“{convexFamilySymbolName}”和“{concaveFamilySymbolName}”的族类型。");
                //    return Result.Failed;
                //}
                //// 获取一个用于放置的标高 (通常使用活动视图的标高)
                //Level level = doc.GetElement(doc.ActiveView.GenLevel.Id) as Level;
                //if (level == null)
                //{
                //    TaskDialog.Show("错误", "错误：无法确定有效的放置标高。");
                //    return Result.Failed;
                //}
                //// 开启事务来创建模型元素
                //using (Transaction tx = new Transaction(doc, "根据自适应构件布置线性族"))
                //{
                //    tx.Start();
                //    int n = points.Count;
                //    // 2. 遍历每一条边
                //    for (int i = 0; i < n; i++)
                //    {
                //        XYZ startPoint = points[i];
                //        XYZ endPoint = points[(i + 1) % n];
                //        // 3. 决策：检查这条边终点 (endPoint) 处的拐角性质
                //        // 这个拐角由三个点定义：startPoint -> endPoint -> nextPoint
                //        XYZ nextPoint = points[(i + 2) % n];
                //        bool isConcaveCorner = IsCornerConcave(startPoint, endPoint, nextPoint, order);
                //        // 选择要使用的族类型
                //        FamilySymbol symbolToUse = isConcaveCorner ? symbolB : symbolA;
                //        // 4. 创建：创建基于线的族实例
                //        try
                //        {
                //            // 创建一条线作为族实例的驱动线
                //            Line edgeCurve = Line.CreateBound(startPoint, endPoint);
                //            // 创建族实例注意：确保"线性族A"和"线性族B"是基于线或两点放置的常规族
                //            var gaugeInstance = doc.Create.NewFamilyInstance(edgeCurve, symbolToUse, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                //            //double length = gaugeInstance.get_Parameter(BuiltInParameter.FAMILY_LINE_LENGTH_PARAM).AsDouble();
                //            double gaugeWidth = gaugeInstance.LookupParameter("偏移").AsDouble();
                //            if (symbolToUse == symbolB)
                //            {
                //                gaugeInstance.LookupParameter("sideLength").Set(edgeCurve.Length + gaugeWidth * 2);
                //            }
                //            else gaugeInstance.LookupParameter("sideLength").Set(edgeCurve.Length - gaugeWidth * 2);
                //        }
                //        catch (Exception ex)
                //        {
                //            message = $"在边 {i + 1} 处创建族实例失败: {ex.Message}";
                //            tx.RollBack();
                //            TaskDialog.Show("创建失败", message);
                //            return Result.Failed;
                //        }
                //    }
                //    tx.Commit();
                //}
                //TaskDialog.Show("成功", $"已成功沿自适应构件边界布置了 {points.Count} 个线性构件。");
                //}
                //    else
                //    {
                //        // 如果几何形状有问题，则不执行布置
                //        StringBuilder report = new StringBuilder("几何检查未通过，无法进行布置：");
                //        if (isSelfIntersecting) report.AppendLine("- 多边形自交叉。");
                //        if (order == WindingOrder.Collinear) report.AppendLine("- 所有点共线。");
                //        if (order == WindingOrder.Non) report.AppendLine("- 顶点数少于3，无法构成多边形。");
                //        TaskDialog.Show("操作取消", report.ToString());
                //    }
                ////例程结束
                ///
                ////// 5. 向用户报告检查结果
                //StringBuilder report = new StringBuilder();
                //report.AppendLine($"已检查族实例: {instance.Name} (ID: {instance.Id})");
                //report.AppendLine($"构件包含 {points.Count} 个自适应点。");
                //report.AppendLine("几何检查结果: ");
                //report.AppendLine($"  - 是否为简单多边形 (非自交叉): {(isSelfIntersecting ? "否 (存在自交叉)" : "是")}");
                //report.AppendLine($"  - 放置点方向: {order}");
                //report.AppendLine($"  - 凹凸多边形判断: {polygonType}");
                ////report.AppendLine($"  - 多边形条件长度: {specialPerimeter * 304.8:F2}");
                //TaskDialog.Show("通用自适应构件检查结果", report.ToString());
                //例程结束
                ////0913 拾取自适应环(4点) 
                //if (placementPointIds.Count != 4)                 return Result.Failed;
                //foreach (ElementId pointId in placementPointIds)
                //{
                //    ReferencePoint refPoint = doc.GetElement(pointId) as ReferencePoint;
                //    if (refPoint != null){  points.Add(refPoint.Position); }
                //}
                //// 确保成功提取了所有点
                //if (points.Count != 4)
                //{
                //    message = "无法成功提取所有放置点的坐标。";
                //    return Result.Failed;
                //}
                //// 4. 执行几何检查 (调用与之前完全相同的辅助方法)
                //bool isSelfIntersecting = CheckSelfIntersection(points);
                //WindingOrder order = WindingOrder.Non;
                //if (!isSelfIntersecting){ order = CheckWindingOrder(points);}
                //// 5. 向用户报告检查结果
                //StringBuilder report = new StringBuilder();
                //report.AppendLine($"已检查族实例: {instance.Name} (ID: {instance.Id})");
                //report.AppendLine("几何检查结果: ");
                //report.AppendLine($"  - 是否自交叉: {(isSelfIntersecting ? "是 (蝴蝶形)" : "否")}");
                //report.AppendLine($"  - 放置点方向: {order}");
                //TaskDialog.Show("检查结果", report.ToString());
                //例程结束
                //0913 4边自适应环创建并检查
                //// 1. 查找要放置的自适应族符号(FamilySymbol)
                //FamilySymbol symbol = FindAdaptiveFamilySymbol(doc);
                //if (symbol == null)
                //{
                //    message = $"未在项目中找到名为 '{ADAPTIVE_FAMILY_NAME}' 且类型为 '{ADAPTIVE_TYPE_NAME}' 的自适应族。";
                //    return Result.Failed;
                //}
                //// 2. 引导用户拾取四个点
                //IList<XYZ> pickedPoints = new List<XYZ>();
                //try
                //{
                //    for (int i = 0; i < 4; i++)
                //    {
                //        // 使用 PickPoint 来确保用户按顺序拾取
                //        pickedPoints.Add(uiDoc.Selection.PickPoint($"请拾取第 {i + 1} 个点"));
                //    }
                //}
                //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                //{
                //    // 用户按了 ESC 键取消操作
                //    return Result.Cancelled;
                //}
                //catch (Exception ex)
                //{
                //    message = "拾取点时发生错误: " + ex.Message;
                //    return Result.Failed;
                //}
                //// 确保我们确实有4个点
                //if (pickedPoints.Count != 4)
                //{
                //    message = "需要拾取4个点才能继续。";
                //    return Result.Failed;
                //}
                //// 3. 执行几何检查
                //bool isSelfIntersecting = CheckSelfIntersection(pickedPoints);
                //WindingOrder order = WindingOrder.Non;
                //if (!isSelfIntersecting) {  order = CheckWindingOrder(pickedPoints); }
                //// 4. 向用户报告检查结果
                //StringBuilder report = new StringBuilder();
                //report.AppendLine("几何检查结果:");
                //report.AppendLine($"  - 是否自交叉: {(isSelfIntersecting ? "是 (蝴蝶形)" : "否")}");
                //report.AppendLine($"  - 拾取方向: {order}");
                //report.AppendLine("是否继续创建构件？");            
                //TaskDialog mainDialog = new TaskDialog("检查结果");
                //mainDialog.MainInstruction = "检查完成";
                //mainDialog.MainContent = report.ToString();
                //mainDialog.CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
                //mainDialog.DefaultButton = TaskDialogResult.Ok;
                //TaskDialogResult dialogResult = mainDialog.Show();
                //if (dialogResult == TaskDialogResult.Cancel) { return Result.Succeeded; }
                //// 5. 如果自交叉，则阻止创建
                //if (isSelfIntersecting)
                //{
                //    TaskDialog.Show("操作已中止", "由于检测到形状自交叉，无法创建构件。");
                //    return Result.Failed;
                //}
                //// 6. 创建自适应构件
                //try
                //{
                //    using (Transaction t = new Transaction(doc, "创建带检查的自适应构件"))
                //    {
                //        t.Start();
                //        // 确保族符号已激活
                //        if (!symbol.IsActive)
                //        {
                //            symbol.Activate();
                //            doc.Regenerate();
                //        }                    
                //        // 创建实例
                //        FamilyInstance instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);
                //        // 获取实例的放置点引用
                //        IList<ElementId> placementPointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);                    
                //        // 将拾取的点位置赋值给实例的放置点
                //        for (int i = 0; i < placementPointIds.Count; i++)
                //        {
                //            ReferencePoint point = doc.GetElement(placementPointIds[i]) as ReferencePoint;
                //            point.Position = pickedPoints[i];
                //        }                    
                //        t.Commit();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    message = "创建自适应构件时失败: " + ex.Message;
                //    return Result.Failed;
                //}
                //例程结束
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
                ////var stair = doc.GetElement(new ElementId(1929002)) as Stairs;
                //var stair = doc.GetElement(new ElementId(1928367)) as Stairs;
                ////var stair = doc.GetElement(new ElementId(2193116)) as Stairs;
                ////var stair = doc.GetElement(new ElementId(2520656)) as Stairs;
                ////var stair = doc.GetElement(new ElementId(2521425)) as Stairs;
                ////var stair = doc.GetElement(new ElementId(2191119)) as Stairs;
                ////var stair = doc.GetElement(new ElementId(2187406)) as Stairs;
                //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
                //if (isStairInRoom)
                //{  TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。"); }
                //else {   TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。"); }
                //例程结束
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
                //0913 族管理器增加类别
                //FamilyManagerView familyManagerView = new FamilyManagerView(uiApp);
                //familyManagerView.Show();
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
