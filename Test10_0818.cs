using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.filter;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Media3D;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test10_0818 : IExternalCommand
    {
        /// <summary>
        /// 处理板类元素（楼板、屋顶等），通过创建洞口实现
        /// </summary>
        //private void ProcessSlabElement(Document doc, Element slab, Plane plane1, Plane plane2, XYZ normal, double gapWidth)
        //{
        //    using (Transaction tx = new Transaction(doc, $"为 {slab.Category.Name} {slab.Id} 创建洞口"))
        //    {
        //        tx.Start();
        //        // 创建一个足够大的矩形轮廓来切割整个板
        //        BoundingBoxXYZ bbox = slab.get_BoundingBox(null);
        //        double size = bbox.Max.DistanceTo(bbox.Min) * 2; // 确保尺寸足够大
        //        XYZ p1 = plane1.Origin - plane1.XVec * size - plane1.YVec * size;
        //        XYZ p2 = p1 + plane1.XVec * (2 * size);
        //        XYZ p3 = p2 + plane1.YVec * (2 * size);
        //        XYZ p4 = p1 + plane1.YVec * (2 * size);
        //        Curve c1 = Line.CreateBound(p1, p2);
        //        Curve c2 = Line.CreateBound(p2, p3);
        //        Curve c3 = Line.CreateBound(p3, p4);
        //        Curve c4 = Line.CreateBound(p4, p1);
        //        CurveLoop profile = new CurveLoop();
        //        profile.Append(c1);
        //        profile.Append(c2);
        //        profile.Append(c3);
        //        profile.Append(c4);
        //        // 从中心向一个方向移动轮廓，然后用它创建贯通挤压
        //        Transform extrudeTransform = Transform.CreateTranslation(normal * gapWidth);
        //        CurveLoop extrudeProfile = CurveLoop.CreateViaTransform(profile, extrudeTransform);
        //        Solid openingSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, -normal, gapWidth);
        //        // 检查板的几何体是否与我们创建的洞口实体相交
        //        if (DoesElementIntersectSolid(slab, openingSolid))
        //        {
        //            // 创建洞口
        //            doc.Create.NewOpening(slab, new List<CurveLoop> { profile }, true);
        //        }
        //        tx.Commit();
        //    }
        //}
        ///// <summary>
        ///// 处理结构框架（梁等），通过分割并删除中间段实现
        ///// </summary>
        //private void ProcessFramingElement(Document doc, Element beam, Plane plane1, Plane plane2)
        //{
        //    LocationCurve locationCurve = beam.Location as LocationCurve;
        //    if (locationCurve == null || !(locationCurve.Curve is Line)) return; // 仅处理直线梁
        //    Curve beamCurve = locationCurve.Curve;
        //    // 计算与两个平面的交点
        //    XYZ intersection1 = FindIntersection(beamCurve, plane1);
        //    XYZ intersection2 = FindIntersection(beamCurve, plane2);
        //    if (intersection1 == null || intersection2 == null) return; // 梁不与打断区域相交
        //    using (Transaction tx = new Transaction(doc, $"打断梁 {beam.Id}"))
        //    {
        //        tx.Start();
        //        // 确保分割点在梁的范围内
        //        if (!IsPointOnCurve(beamCurve, intersection1) || !IsPointOnCurve(beamCurve, intersection2))
        //        {
        //            tx.RollBack();
        //            return;
        //        }
        //        ElementId originalBeamId = beam.Id;
        //        // 第一次分割，返回的是新创建的第二段梁的ID
        //        ElementId secondPartId = Autodesk.Revit.DB.Structure.SplitElement.SplitElement(doc, originalBeamId, intersection1);
        //        doc.Regenerate(); // 刷新数据库
        //        // 第二次分割，在第二段上进行
        //        ElementId thirdPartId = Autodesk.Revit.DB.Structure.SplitElement.SplitElement(doc, secondPartId, intersection2);
        //        // secondPartId 现在代表的是中间那段需要被删除的梁
        //        doc.Delete(secondPartId);
        //        tx.Commit();
        //    }
        //}
        //#region Helper Methods
        //private bool DoesElementIntersectSolid(Element elem, Solid solid)
        //{
        //    var opt = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, DetailLevel = ViewDetailLevel.Fine };
        //    var geom = elem.get_Geometry(opt);
        //    if (geom == null) return false;
        //    foreach (var geomObj in geom)
        //    {
        //        if (geomObj is Solid elemSolid && elemSolid.Volume > 1e-9)
        //        {
        //            var intersectSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid, elemSolid, BooleanOperationsType.Intersect);
        //            if (intersectSolid != null && intersectSolid.Volume > 1e-9)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}
        //private XYZ FindIntersection(Curve curve, Plane plane)
        //{
        //    IntersectionResultArray results;
        //    SetComparisonResult result = curve.Intersect(plane, out results);
        //    if (result == SetComparisonResult.Overlap && results != null && !results.IsEmpty)
        //    {
        //        return results.get_Item(0).XYZPoint;
        //    }
        //    return null;
        //}
        //private bool IsPointOnCurve(Curve curve, XYZ point)
        //{
        //    return curve.Distance(point) < 1e-6; // 使用容差判断点是否在曲线上
        //}
        //private bool DoesBoundingBoxIntersectPlane(BoundingBoxXYZ bbox, Plane plane)
        //{
        //    if (bbox == null) return false;
        //    // 检查包围盒的8个角点是否在平面的两侧
        //    bool hasPointOnPositiveSide = false;
        //    bool hasPointOnNegativeSide = false;
        //    for (int i = 0; i < 2; i++)
        //    {
        //        for (int j = 0; j < 2; j++)
        //        {
        //            for (int k = 0; k < 2; k++)
        //            {
        //                XYZ corner = new XYZ(
        //                    i == 0 ? bbox.Min.X : bbox.Max.X,
        //                    j == 0 ? bbox.Min.Y : bbox.Max.Y,
        //                    k == 0 ? bbox.Min.Z : bbox.Max.Z);
        //                double signedDistance = plane.Normal.DotProduct(corner - plane.Origin);
        //                if (signedDistance > 1e-9) hasPointOnPositiveSide = true;
        //                if (signedDistance < -1e-9) hasPointOnNegativeSide = true;
        //            }
        //        }
        //    }
        //    return hasPointOnPositiveSide && hasPointOnNegativeSide;
        //}
        //#endregion

        /// <summary>
        /// 在视图中绘制可见性分析的结果
        /// </summary>
        private void DrawVisibilityResults(Document doc, View view, XYZ observerPoint, List<XYZ> visiblePoints)
        {
            // 创建新的图形样式以便区分
            GraphicsStyle gs = GetOrCreateGraphicsStyle(doc, "可见性分析线");

            if (visiblePoints.Count <= 1) return;

            // 找到可见区域的边界点（一个简化的方法是找到凸包）
            List<XYZ> boundaryPoints = FindConvexHull(visiblePoints);

            // 1. 绘制可见区域在标记牌上的轮廓线 (最大最小范围)
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                XYZ p1 = boundaryPoints[i];
                XYZ p2 = boundaryPoints[(i + 1) % boundaryPoints.Count]; // 连接到下一个点，最后一个点连回第一个
                Line line = Line.CreateBound(p1, p2);
                doc.Create.NewModelCurve(line, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
            }

            // 2. 绘制从观察点到可见区域边界的“视锥”
            foreach (XYZ boundaryPoint in boundaryPoints)
            {
                Line coneLine = Line.CreateBound(observerPoint, boundaryPoint);
                ModelCurve mc = doc.Create.NewModelCurve(coneLine, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin)));
                mc.LineStyle = gs; // 应用自定义图形样式
            }
        }
        /// <summary>
        /// 找到一组点的2D凸包 (投影到XY平面)
        /// 这是一个简化的凸包算法 (Gift wrapping algorithm)
        /// </summary>
        public List<XYZ> FindConvexHull(List<XYZ> points)
        {
            if (points.Count <= 2) return points;

            List<XYZ> hull = new List<XYZ>();

            // 找到最左边的点作为起点
            XYZ startPoint = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();

            XYZ currentPoint = startPoint;
            do
            {
                hull.Add(currentPoint);
                XYZ nextPoint = points[0];

                foreach (XYZ p in points)
                {
                    if (nextPoint == currentPoint || IsLeft(currentPoint, nextPoint, p) > 0)
                    {
                        nextPoint = p;
                    }
                }
                currentPoint = nextPoint;

            } while (currentPoint != startPoint);

            return hull;
        }
        private double IsLeft(XYZ p1, XYZ p2, XYZ p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        }
        /// <summary>
        /// 获取或创建用于可视化的图形样式
        /// </summary>
        private GraphicsStyle GetOrCreateGraphicsStyle(Document doc, string styleName)
        {
            var cat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            var subCat = cat.SubCategories.get_Item(styleName);
            if (subCat == null)
            {
                subCat = doc.Settings.Categories.NewSubcategory(cat, styleName);
                subCat.LineColor = new Color(255, 0, 0); // 红色
                subCat.SetLineWeight(5, GraphicsStyleType.Projection);
            }
            return subCat.GetGraphicsStyle(GraphicsStyleType.Projection);
        }

        /// <summary>
        /// 查找最适合进行射线检测的3D视图
        /// </summary>
        private View3D FindBest3DView(Document doc)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
            return default3DView ?? collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        }
        /// <summary>
        /// 从楼板的实体几何体中提取其底面的所有轮廓环路。
        /// </summary>
        /// <param name="floor">要分析的楼板</param>
        /// <returns>包含所有轮廓的CurveArray列表</returns>
        private List<CurveArray> GetFloorLoopsFromGeometry(Floor floor)
        {
            var loops = new List<CurveArray>();
            Options geomOptions = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, View = floor.Document.ActiveView };
            GeometryElement geoElem = floor.get_Geometry(geomOptions);
            if (geoElem == null) return null;
            Solid solid = geoElem.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);
            if (solid == null) return null;
            PlanarFace bottomFace = null;
            foreach (Face face in solid.Faces)
            {
                PlanarFace pFace = face as PlanarFace;
                if (pFace != null && pFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ.Negate()))
                {
                    bottomFace = pFace;
                    break;
                }
            }
            if (bottomFace == null) return null;
            // *** 这是修正的核心部分 ***
            // bottomFace.EdgeLoops 返回 IList<EdgeArray>
            var edgeLoopList = bottomFace.EdgeLoops;
            // 遍历每个 EdgeArray (每个代表一个闭合环路)
            foreach (EdgeArray edgeArray in edgeLoopList)
            {
                // 为每个环路创建一个新的 CurveArray 来存放曲线
                CurveArray curveArray = new CurveArray();
                // 遍历环路中的每一条边 (Edge)
                foreach (Edge edge in edgeArray)
                {
                    // 从边中提取几何曲线 (Curve) 并添加到 CurveArray 中
                    curveArray.Append(edge.AsCurve());
                }
                // 将转换好的 CurveArray 添加到结果列表中
                loops.Add(curveArray);
            }
            return loops;
        }
        /// <summary>
        /// 确保两个元素被连接，并且第一个元素切割第二个元素。
        /// </summary>
        private void EnsureJoinOrder(Document doc, Element cutter, Element cuttee)
        {
            if (!JoinGeometryUtils.AreElementsJoined(doc, cutter, cuttee))
            {
                try
                {
                    JoinGeometryUtils.JoinGeometry(doc, cutter, cuttee);
                }
                catch (Exception ex)
                {
                    // 记录连接失败的日志，对于调试很重要
                    System.Diagnostics.Debug.WriteLine($"无法连接元素 {cutter.Id} 和 {cuttee.Id}: {ex.Message}");
                }
            }
            else
            {
                // 如果已经连接，检查顺序是否正确
                if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, cutter, cuttee))
                {
                    try
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, cutter, cuttee);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"无法切换元素 {cutter.Id} 和 {cuttee.Id} 的连接顺序: {ex.Message}");
                    }
                }
            }
        }
        /// <summary>
        /// 获取与给定元素包围盒相交的元素（梁和楼板）。
        /// </summary>
        private List<Element> GetIntersectingElements(Document doc, Element element, double expansionAmount)
        {
            // 使用 get_BoundingBox(null) 获取模型空间的完整3D包围盒
            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox == null) return new List<Element>();
            // 扩大包围盒以确保捕捉到所有接触的元素
            Outline outline = new Outline(bbox.Min - new XYZ(expansionAmount, expansionAmount, expansionAmount),
                                          bbox.Max + new XYZ(expansionAmount, expansionAmount, expansionAmount));
            // 使用 BoundingBoxIntersectsFilter 时，公差应非常小
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline, 1e-9);
            // 定义要查找的元素类别
            var categoryFilters = new List<ElementFilter>
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                new ElementCategoryFilter(BuiltInCategory.OST_Floors)
            };
            var logicalOrFilter = new LogicalOrFilter(categoryFilters);
            var finalFilter = new LogicalAndFilter(bbFilter, logicalOrFilter);
            return new FilteredElementCollector(doc).WherePasses(finalFilter)
                .Where(e => e.Id != element.Id).ToList();
        }
        //1003 
        /// <summary>
        /// 查找一个最适合进行射线检测的3D视图。
        /// 优先选择默认的 {3D} 视图，因为它通常包含所有模型元素。
        /// </summary>
        /// 重复方法回头看一下是否去重？
        //private static View3D FindBest3DView(Document doc)
        //{
        //    var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));

        //    // 优先寻找默认的 {3D} 视图
        //    View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
        //    if (default3DView != null)
        //    {
        //        return default3DView;
        //    }

        //    // 如果找不到，再寻找任何一个非模板的3D视图作为备用
        //    return collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        //}
        ///代码解析
        //输入：通过 PickPoint 和 PickObject(ObjectType.Face)，我们精确地获取了观察点和用户想要分析的那个面。这是最可靠的方式。
        //采样：
        //targetFace.GetBoundingBox() 获取了面的 UV 参数范围。
        //通过双重 for 循环，我们在 UV 空间中均匀地创建了一个网格。
        //targetFace.Evaluate(new UV(u, v)) 将二维的 UV 参数转换为了三维世界坐标 XYZ。
        //可见性检测：
        //ReferenceIntersector 的构造函数传入了 targetElement.Id，这是一个优化，它会忽略目标本身，防止射线总是命中自己。但为了逻辑的严谨性，我们还是通过比较距离来判断，这样更通用。
        //distanceToFace 是原始距离，hitDistance 是碰撞距离。Math.Abs(hitDistance - distanceToFace) < tolerance 是判断是否命中了目标本身的关键。
        //可视化：
        //"最大最小可见范围" 的最佳数学表达是可见区域的轮廓。
        //我提供了一个简化的 凸包（Convex Hull） 算法 FindConvexHull 来找到包围所有可见点的最小多边形。这个多边形就是可见区域的边界。
        //DrawVisibilityResults 方法做了两件事：
        //用模型线在标记牌上绘制出这个凸包轮廓。
        //从观察点向凸包的每个顶点发射连线，形成一个视锥（Viewing Frustum），直观地展示了可见范围。
        //为了让绘制的线更醒目，我写了一个 GetOrCreateGraphicsStyle 方法来创建一个红色的、较粗的线样式。
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
        StringBuilder sb = new StringBuilder();
        Application Application;
        public string FindBuiltInFailureByDescription(string searchDescription)
        {
            FailureDefinitionRegistry failureReg = Application.GetFailureDefinitionRegistry();
            Type builtInFailuresType = typeof(BuiltInFailures);
            Type[] nestedTypes = builtInFailuresType.GetNestedTypes(BindingFlags.Public);

            foreach (Type nestedType in nestedTypes)
            {
                foreach (PropertyInfo property in nestedType.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    try
                    {
                        MethodInfo getMethod = property.GetGetMethod();
                        if (getMethod != null)
                        {
                            FailureDefinitionId failureId = getMethod.Invoke(null, null) as FailureDefinitionId;
                            if (failureId != null)
                            {
                                FailureDefinitionAccessor failureAccessor = failureReg.FindFailureDefinition(failureId);
                                if (failureAccessor != null)
                                {
                                    string description = failureAccessor.GetDescriptionText();
                                    if (description.Contains(searchDescription))
                                    {
                                        return $"{nestedType.Name}.{property.Name}";
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略错误继续查找
                    }
                }
            }
            return null;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            Application = uiApp.Application;

            ////1026 按构件机电类别选择
            //FamilyInstance familyInstance;
            //ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds.Count() == 0)
            //{
            //    var reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new FamilyInstanceFilterClass(), "选择元素");
            //    familyInstance = doc.GetElement(reference) as FamilyInstance;
            //}
            //else familyInstance = doc.GetElement(selectedIds.FirstOrDefault()) as FamilyInstance;
            //if (familyInstance.MEPModel.ConnectorManager != null)
            //{
            //    var para = familyInstance.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            //    if (para == null)
            //    {
            //        para = familyInstance.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
            //    }
            //    var paramName = para.AsValueString();
            //    if (!(paramName == "Undefined" || paramName == "未定义"))
            //    {
            //        //TaskDialog.Show("tt", paramName);
            //        ElementId systemTypeId = para.AsElementId();
            //        ElementId familySymbolId = familyInstance.Symbol.Family.Id;
            //        List<ElementId> selectedElementIds = new List<ElementId>();
            //        // 使用FilteredElementCollector获取所有FamilyInstance
            //        FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance));
            //        // 根据参数名称确定是管道系统还是风管系统
            //        BuiltInParameter targetParameter;
            //        if (paramName.Contains("PIPING"))
            //        {
            //            targetParameter = BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM;
            //        }
            //        else
            //        {
            //            targetParameter = BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM;
            //        }
            //        foreach (FamilyInstance instance in collector)
            //        {
            //            // 检查是否有MEP连接
            //            if (instance.MEPModel?.ConnectorManager != null)
            //            {
            //                // 获取系统类型参数
            //                Parameter systemParam = instance.get_Parameter(targetParameter);
            //                if (systemParam != null && systemParam.AsElementId() == systemTypeId && instance.Symbol.Family.Id == familySymbolId)
            //                {
            //                    selectedElementIds.Add(instance.Id);
            //                }
            //            }
            //        }
            //        uiDoc.Selection.SetElementIds(selectedElementIds);
            //        TaskDialog.Show("选择完成", $"已选择 {selectedElementIds.Count} 个相同系统类型的构件");
            //    }
            //    else
            //    {
            //        TaskDialog.Show("提示", "该构件系统类型未定义");
            //    }
            //}
            //else TaskDialog.Show("tt", "该构件没有MEP连接管理器");
            //例程结束
       
            ////0926 批量按选择项写楼板面积属性,临时工具
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
            //例程结束


            ////1025 找项目参数并删除
            //// 检查是否允许全局参数
            //if (GlobalParametersManager.AreGlobalParametersAllowed(doc))
            //{
            //    List<ProjectParameterData> result = new List<ProjectParameterData>();
            //    // 获取文档的BindingMap
            //    BindingMap map = doc.ParameterBindings;
            //    DefinitionBindingMapIterator iterator = map.ForwardIterator();
            //    while (iterator.MoveNext())
            //    {
            //        Definition definition = iterator.Key;
            //        ElementBinding binding = iterator.Current as ElementBinding;
            //        if (definition != null && binding != null)
            //        {
            //            ProjectParameterData paramData = new ProjectParameterData
            //            {
            //                Name = definition.Name,
            //                //ParameterType = GetParameterTypeString(definition),
            //                //ParameterGroup = GetParameterGroupString(definition),
            //                //BindingType = binding is InstanceBinding ? "实例参数" : "类型参数",
            //                //Categories = GetBoundCategories(binding),
            //                //IsShared = definition is ExternalDefinition,
            //                //IsReportable = IsReportableParameter(definition),
            //                //GUID = GetParameterGUID(definition)
            //            };
            //            ////if ((definition is ExternalDefinition))
            //            //if (binding is CategorySetBinding)
            //            //{
            //            //    result.Add(paramData);
            //            //}
            //            result.Add(paramData);
            //        }
            //    }
            //    List<string> parameterNames = new List<string>();
            //    foreach (var item in result)
            //    {
            //        parameterNames.Add(item.Name);
            //    }
            //    //foreach (var item in filters)
            //    //{
            //    //    parameterNames.Add(item.Name);
            //    //}
            //    // 输出所有全局参数名称
            //    //TaskDialog.Show("全局参数", string.Join("\n", parameterNames));
            //    TaskDialog.Show("全局参数", parameterNames.Count().ToString());
            //}
            //else
            //{
            //    TaskDialog.Show("错误", "当前文档不支持全局参数");
            //}
            ////"冷凝水系统"
            ////1024 找遗漏少量id
            //var resultPipes = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            //var resultPipes2 = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
            //StringBuilder ids = new StringBuilder();
            //foreach (var item in resultPipes)
            //{
            //    Parameter parameter = item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            //    if (parameter != null && parameter.HasValue)
            //    {
            //        ElementId systemTypeId = parameter.AsElementId();
            //        if (systemTypeId != ElementId.InvalidElementId)
            //        {
            //            // 通过系统类型ID获取系统类型元素
            //            Element systemType = doc.GetElement(systemTypeId);
            //            if (systemType != null)
            //            {
            //                // 获取系统名称
            //                string systemName = systemType.Name;
            //                if (systemName == "冷凝水系统")
            //                {
            //                    ids.AppendLine($"ElementId: {item.Id.IntegerValue}");
            //                }
            //            }
            //        }
            //    }
            //}
            //foreach (var item in resultPipes2)
            //{
            //    Parameter parameter = item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            //    if (parameter != null && parameter.HasValue)
            //    {
            //        ElementId systemTypeId = parameter.AsElementId();
            //        if (systemTypeId != ElementId.InvalidElementId)
            //        {
            //            // 通过系统类型ID获取系统类型元素
            //            Element systemType = doc.GetElement(systemTypeId);
            //            if (systemType != null)
            //            {
            //                // 获取系统名称
            //                string systemName = systemType.Name;
            //                if (systemName == "冷凝水系统")
            //                {
            //                    ids.AppendLine($"ElementId: {item.Id.IntegerValue}");
            //                }
            //            }
            //        }
            //    }
            //}
            //TaskDialog.Show("tt", ids.ToString());
            ////例程结束
            //var resultPipes = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
            //StringBuilder ids = new StringBuilder();
            //foreach (var item in resultPipes)
            //{
            //    Parameter parameter = item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            //    if (parameter.AsString() == "组合式空调系统供水") ids.AppendLine(item.Id.ToString());
            //}
            //TaskDialog.Show("tt", ids.ToString());
            //var objs = new FilteredElementCollector(doc).OfCategory( BuiltInCategory.OST_DuctFitting).
            //var fitting = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPFitting());
            //Element element = doc.GetElement(fitting);

            ////1010 字符串长度
            //string aa = "Audit_DesignRole//Audit_CheckRole//Audit_DspAppRole//Audit_ReviewRole//Audit_ApproveRole";
            //string aa = "H:\\mango\\整理芒果资源\\BaiduNetdiskDownload\\【24650】2016幼升小数学思维启蒙班【22讲 洪然】\\视频+讲义\\【免费小学学习视频下载www.laixuexi.cc】第21讲 益智天地";
            //TaskDialog.Show("tt", aa.Length.ToString());

            ////1018 改拾取元素递增值代码，1002 通用编码 OK
            //FamilyInstanceSerializeView instanceSerializeView = new FamilyInstanceSerializeView(uiApp);
            //instanceSerializeView.Show();

            //////1014 补充沟体替换
            //CircleGaugePlaceView circleGaugePlaceView = new CircleGaugePlaceView(uiApp);
            //circleGaugePlaceView.Show();

            //var instance = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new AdaptiveFamilyFilter()).ElementId) as FamilyInstance;
            //var loc = instance.Location;
            //if (loc is LocationPoint locationPoint && activeView.ViewType is ViewType.FloorPlan)
            //{
            //    XYZ xYZ = locationPoint.Point;
            //    using (Transaction trans = new Transaction(doc, "绘制基准点圆圈"))
            //    {
            //        trans.Start();
            //        // 创建草图平面（使用当前视图的草图平面）
            //        SketchPlane sketchPlane = activeView.SketchPlane;
            //        if (sketchPlane == null)
            //        {
            //            // 如果没有草图平面，创建一个基于水平面的
            //            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //            sketchPlane = SketchPlane.Create(doc, plane);
            //        }
            //        // 圆的半径（100毫米转换为英尺）
            //        double radius = 100 / 304.8;
            //        // 创建圆
            //        Arc circle = Arc.Create(xYZ, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY);
            //        // 创建详细线
            //        DetailLine detailCircle = doc.Create.NewDetailCurve(activeView, circle) as DetailLine;
            //        trans.Commit();
            //        TaskDialog.Show("完成", "已在基准点位置绘制半径为100mm的圆");
            //    }
            //}



            ////1003 SplitElementsCommand 变形缝、后浇带打断板、梁
            //// 检查当前视图是否为平面、立面或剖面
            //if (!(doc.ActiveView is ViewPlan || doc.ActiveView is ViewSection || doc.ActiveView is ViewElevation))
            //{
            //    message = "请在平面、立面或剖面视图中运行此命令。";
            //    return Result.Failed;
            //}
            //// 1. 让用户选择一个参照平面
            //Reference refPlaneRef;
            //try
            //{
            //    refPlaneRef = uiDoc.Selection.PickObject(ObjectType.Element, new ReferencePlaneSelectionFilter(), "请选择一个用于打断的参照平面");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //ReferencePlane refPlane = doc.GetElement(refPlaneRef) as ReferencePlane;
            //if (refPlane == null) return Result.Failed;
            //// 2. 弹出窗口让用户输入宽度
            //GapWidthWindow widthDialog = new GapWidthWindow();
            //if (widthDialog.ShowDialog() != true)
            //{
            //    return Result.Cancelled;
            //}
            //double gapWidthMm = widthDialog.GapWidth;
            //double gapWidthFeet = UnitUtils.ConvertToInternalUnits(gapWidthMm, UnitTypeId.Millimeters);
            //// 3. 计算两侧的偏移平面
            //Plane centerPlane = refPlane.GetPlane();
            //XYZ normal = centerPlane.Normal;
            //double offset = gapWidthFeet / 2.0;
            //Transform transform1 = Transform.CreateTranslation(normal * offset);
            //Transform transform2 = Transform.CreateTranslation(-normal * offset);
            //Plane plane1 = centerPlane.CreateTransformed(transform1);
            //Plane plane2 = centerPlane.CreateTransformed(transform2);
            //// 4. 查找所有需要被打断的元素
            //var categories = new List<BuiltInCategory>
            //{
            //    BuiltInCategory.OST_Floors,
            //    BuiltInCategory.OST_Roofs,
            //    BuiltInCategory.OST_StructuralFraming,
            //    BuiltInCategory.OST_FoundationSlab
            //};
            //var multiCategoryFilter = new ElementMulticategoryFilter(categories);
            //var elementsToProcess = new FilteredElementCollector(doc)
            //    .WherePasses(multiCategoryFilter)
            //    .WhereElementIsNotElementType()
            //    .ToList();
            //using (TransactionGroup tg = new TransactionGroup(doc, "沿参照平面打断元素"))
            //{
            //    tg.Start();
            //    foreach (Element elem in elementsToProcess)
            //    {
            //        // 检查元素是否与打断区域相交 (一个粗略但快速的检查)
            //        BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
            //        if (bbox == null || !DoesBoundingBoxIntersectPlane(bbox, centerPlane))
            //        {
            //            continue;
            //        }
            //        try
            //        {
            //            if (elem is Floor || elem is RoofBase) // 处理楼板、屋顶、板式基础
            //            {
            //                ProcessSlabElement(doc, elem, plane1, plane2, normal, gapWidthFeet);
            //            }
            //            else if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming) // 处理结构框架
            //            {
            //                ProcessFramingElement(doc, elem, plane1, plane2);
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            // 记录或通知用户某个特定元素处理失败
            //            TaskDialog.Show("处理失败", $"处理元素 {elem.Id} 时发生错误: {ex.Message}");
            //        }
            //    }
            //    if (tg.Assimilate() == TransactionStatus.Committed)
            //    {
            //        TaskDialog.Show("完成", "元素打断操作完成。");
            //    }
            //    else
            //    {
            //        tg.RollBack();
            //        TaskDialog.Show("失败", "操作失败，所有更改已回滚。");
            //    }
            //}

            ////1003 检测A-B点之间可见
            //try
            //{
            //    // --- 步骤 1: 获取用户输入 ---
            //    // 1.1 获取观察点
            //    XYZ observerPoint = uiDoc.Selection.PickPoint("请选择观察点 (眼睛的位置)");
            //    // 1.2 获取目标面
            //    Reference faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "请选择标记牌的正面");
            //    Element targetElement = doc.GetElement(faceRef);
            //    Face targetFace = targetElement.GetGeometryObjectFromReference(faceRef) as Face;
            //    if (targetFace == null)
            //    {
            //        message = "未能获取有效的几何面。";
            //        return Result.Failed;
            //    }
            //    // --- 步骤 2 & 3: 采样并进行可见性测试 ---
            //    // 定义采样网格的密度 (例如 10x10)
            //    int gridResolutionU = 15;
            //    int gridResolutionV = 15;
            //    List<XYZ> visiblePoints = new List<XYZ>();
            //    List<XYZ> occludedPoints = new List<XYZ>();
            //    BoundingBoxUV bbox = targetFace.GetBoundingBox();
            //    UV min = bbox.Min;
            //    UV max = bbox.Max;
            //    // 准备 ReferenceIntersector
            //    View3D view3D = FindBest3DView(doc);
            //    if (view3D == null)
            //    {
            //        message = "需要一个3D视图来进行可见性分析。";
            //        return Result.Failed;
            //    }
            //    ReferenceIntersector intersector = new ReferenceIntersector(targetElement.Id, FindReferenceTarget.Face, view3D);
            //    intersector.FindReferencesInRevitLinks = true;
            //    // 遍历采样网格
            //    for (int i = 0; i <= gridResolutionU; i++)
            //    {
            //        for (int j = 0; j <= gridResolutionV; j++)
            //        {
            //            double u = min.U + (max.U - min.U) * i / gridResolutionU;
            //            double v = min.V + (max.V - min.V) * j / gridResolutionV;
            //            XYZ samplePointOnFace = targetFace.Evaluate(new UV(u, v));
            //            XYZ direction = (samplePointOnFace - observerPoint).Normalize();
            //            double distanceToFace = observerPoint.DistanceTo(samplePointOnFace);
            //            // 执行射线检测
            //            ReferenceWithContext refWithContext = intersector.FindNearest(observerPoint, direction)
            //            bool isVisible = false;
            //            double tolerance = 0.001; // 精度容差 (约0.3mm)
            //            if (refWithContext == null)
            //            {
            //                // 射线未与任何物体相交，说明该点可见 (在开放空间中)
            //                isVisible = true;
            //            }
            //            else
            //            {
            //                double hitDistance = refWithContext.Proximity;
            //                // 如果碰撞点距离非常接近目标点，则认为是可见的
            //                if (Math.Abs(hitDistance - distanceToFace) < tolerance)
            //                {
            //                    isVisible = true;
            //                }
            //            }
            //            if (isVisible)
            //            {
            //                visiblePoints.Add(samplePointOnFace);
            //            }
            //            else
            //            {
            //                occludedPoints.Add(samplePointOnFace);
            //            }
            //        }
            //    }
            //    // --- 步骤 4: 可视化结果 ---
            //    if (visiblePoints.Count == 0)
            //    {
            //        TaskDialog.Show("结果", "标记牌完全被遮挡，不可见。");
            //        return Result.Succeeded;
            //    }
            //    using (Transaction tx = new Transaction(doc, "绘制可见性范围"))
            //    {
            //        tx.Start();
            //        DrawVisibilityResults(doc, activeView, observerPoint, visiblePoints);
            //        tx.Commit();
            //    }
            //    TaskDialog.Show("完成", $"可见性分析完成。\n可见采样点: {visiblePoints.Count}    已在视图中绘制可见范围。");
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////1003 升级二维射线检测方法
            //try
            //{
            //    // 1. 获取用户选择的点作为射线起点
            //    XYZ origin = uiDoc.Selection.PickPoint("请选择扫描中心点");
            //    // 2. 定义扫描高度 (例如 200mm)
            //    double deltaHeightMM = 200.0;
            //    double heightOffset = UnitUtils.ConvertToInternalUnits(deltaHeightMM, UnitTypeId.Millimeters);
            //    XYZ scanOrigin = origin + new XYZ(0, 0, heightOffset);
            //    // 3. 准备结果容器和字符串构建器
            //    HashSet<ElementId> hitElementIds = new HashSet<ElementId>();
            //    StringBuilder stringBuilder = new StringBuilder();
            //    // 4. (性能优化) 在循环外创建 ReferenceIntersector
            //    View3D view3D = FindBest3DView(doc);
            //    if (view3D == null)
            //    {
            //        message = "项目中找不到可用于检测的3D视图。";
            //        return Result.Failed;
            //    }
            //    ReferenceIntersector intersector = new ReferenceIntersector(view3D);
            //    intersector.TargetType = FindReferenceTarget.Face;
            //    intersector.FindReferencesInRevitLinks = true;
            //    // 5. 在XY平面进行360度检测
            //    for (int angle = 0; angle < 360; angle++)
            //    {
            //        double radians = angle * Math.PI / 180.0;
            //        XYZ direction = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            //        // 执行射线检测
            //        ReferenceWithContext refWithContext = intersector.FindNearest(scanOrigin, direction);
            //        if (refWithContext != null)
            //        {
            //            Reference reference = refWithContext.GetReference();
            //            if (reference != null && reference.ElementId != ElementId.InvalidElementId)
            //            {
            //                hitElementIds.Add(reference.ElementId);
            //            }
            //        }
            //    }
            //    // 6. 处理并显示结果
            //    if (hitElementIds.Count == 0)
            //    {
            //        TaskDialog.Show("扫描结果", "在指定高度和范围内没有检测到任何对象。");
            //    }
            //    else
            //    {
            //        foreach (var id in hitElementIds)
            //        {
            //            Element elem = doc.GetElement(id);
            //            stringBuilder.AppendLine($"ID: {id.IntegerValue}, 名称: {elem?.Name ?? "N/A"}");
            //        }
            //        TaskDialog.Show("扫描结果", $"共检测到 {hitElementIds.Count} 个独立对象:{stringBuilder}");
            //        // 高亮显示碰撞到的图元
            //        uiDoc.Selection.SetElementIds(hitElementIds.ToList());
            //        uiDoc.RefreshActiveView();
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户按 ESC 取消，是正常操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    // 其他意外错误
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////1003 管道设色事务合并到事务组
            //    public class PipeSystemManagerViewModel : ObserverableObject
            //{
            //    private Document _doc;
            //    public Document Doc { get => _doc; set => SetProperty(ref _doc, value); }
            //    public ObservableCollection<PipeSystemEntity> PipeSystemEntitys { get; set; }
            //    #region Commands
            //    public ICommand QueryELementCommand { get; }
            //    public RelayCommand<PipingSystemType> SetColorCommand { get; }
            //    public RelayCommand<IEnumerable<object>> DeleteELementCommand { get; }
            //    public RelayCommand<PipeSystemEntity> DeleteELementCommand2 { get; }
            //    public RelayCommand<PipeSystemEntity> AddInsulationCommand { get; }
            //    public ICommand WindowCommand { get; }
            //    #endregion
            //    public PipeSystemManagerViewModel(Document document)
            //    {
            //        _doc = document;
            //        PipeSystemEntitys = new ObservableCollection<PipeSystemEntity>();
            //        // 初始化命令
            //        QueryELementCommand = new BaseBindingCommand(QueryElement);
            //        SetColorCommand = new RelayCommand<PipingSystemType>(SetColor);
            //        // ... 其他命令初始化 ...
            //        // 加载并预处理数据
            //        LoadAndInitializePipeSystems();
            //    }
            //    private void LoadAndInitializePipeSystems()
            //    {
            //        // --- 核心修改部分：使用 TransactionGroup ---
            //        TransactionGroup tg = new TransactionGroup(_doc, "初始化管道系统设置");
            //        try
            //        {
            //            tg.Start();
            //            // 1. 查询所有管道系统类型
            //            var elements = new FilteredElementCollector(_doc).OfClass(typeof(PipingSystemType));
            //            List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
            //            // 2. 在一个单独的事务中设置默认颜色
            //            using (var trans = new Transaction(_doc, "设置默认系统颜色"))
            //            {
            //                trans.Start();
            //                bool changesMade = false;
            //                Random rand = new Random();
            //                foreach (var pst in pipingSystemTypes)
            //                {
            //                    // 检查颜色是否有效，如果无效，则设置一个随机的默认颜色
            //                    if (!pst.LineColor.IsValid)
            //                    {
            //                        byte r = (byte)rand.Next(50, 220); // 避免太深或太浅的颜色
            //                        byte g = (byte)rand.Next(50, 220);
            //                        byte b = (byte)rand.Next(50, 220);
            //                        pst.LineColor = new Autodesk.Revit.DB.Color(r, g, b);
            //                        changesMade = true;
            //                    }
            //                }
            //                if (changesMade) trans.Commit();
            //                else trans.RollBack(); // 如果没有做任何修改，则回滚事务
            //            }
            //            // 3. 将处理后的数据加载到ViewModel中
            //            PipeSystemEntitys.Clear();
            //            var pipeSystems = pipingSystemTypes.Select(pst => new PipeSystemEntity(pst)).ToList();
            //            foreach (var item in pipeSystems)
            //            {
            //                PipeSystemEntitys.Add(item);
            //            }
            //            // 4. 同化事务组，将所有子事务合并成一个撤销操作
            //            tg.Assimilate();
            //        }
            //        catch (Exception ex)
            //        {
            //            // 如果发生任何错误，回滚整个事务组
            //            tg.RollBack();
            //            TaskDialog.Show("错误", "初始化管道系统时出错: " + ex.Message);
            //        }
            //    }
            //    // SetColor 方法保持不变，它处理用户交互，应该有自己的独立事务
            //    private void SetColor(PipingSystemType pipingSystemType)
            //    {
            //        if (pipingSystemType == null) return;
            //        System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            //        dialog.AllowFullOpen = true;
            //        dialog.FullOpen = true;
            //        dialog.ShowHelp = true;
            //        Autodesk.Revit.DB.Color color = pipingSystemType.LineColor;
            //        dialog.Color = System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue);
            //        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //        {
            //            // 使用上面定义的扩展方法，代码更简洁
            //            _doc.NewTransaction(() =>
            //            {
            //                pipingSystemType.LineColor = dialog.Color.ConvertToRevitColor();
            //            }, "修改线颜色");
            //            QueryElement(null); // 刷新界面
            //        }
            //    }
            //    private void QueryElement(object obj)
            //    {
            //        // 刷新逻辑可以简化为重新调用初始化方法
            //        LoadAndInitializePipeSystems();
            //    }
            //    // ... 其他方法 ...
            //}



            ////1002 拆分楼板，读取出所有轮廓并分别保存多个楼板。注意存在逻辑问题，未处理环嵌套的问题，无法维持板内部开洞
            //// 1. 提示用户选择一个楼板
            //Reference selectedRef;
            //try
            //{
            //    selectedRef = uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "请选择一个包含多个轮廓的楼板");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    return Result.Cancelled;
            //}
            //Floor originalFloor = doc.GetElement(selectedRef) as Floor;
            //if (originalFloor == null)
            //{
            //    message = "选择的不是一个有效的楼板。";
            //    return Result.Failed;
            //}
            //// 2. 通过几何体获取楼板的轮廓
            //List<CurveArray> profileLoops = GetFloorLoopsFromGeometry(originalFloor);
            //if (profileLoops == null || profileLoops.Count == 0)
            //{
            //    message = "无法从楼板的几何体中提取轮廓。";
            //    return Result.Failed;
            //}
            //// 3. 检查轮廓数量
            //if (profileLoops.Count <= 1)
            //{
            //    TaskDialog.Show("提示", "所选楼板只包含一个轮廓，无需拆分。");
            //    return Result.Succeeded;
            //}
            //using (TransactionGroup tg = new TransactionGroup(doc, "拆分楼板"))
            //{
            //    tg.Start();
            //    try
            //    {
            //        ElementId floorTypeId = originalFloor.GetTypeId();
            //        ElementId levelId = originalFloor.LevelId;
            //        bool isStructural = originalFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() == 1;
            //        Level level = doc.GetElement(levelId) as Level;
            //        FloorType floorType = doc.GetElement(floorTypeId) as FloorType;
            //        foreach (CurveArray curveLoop in profileLoops)
            //        {
            //            using (Transaction tx = new Transaction(doc, "创建单个楼板"))
            //            {
            //                tx.Start();
            //                doc.Create.NewFloor(curveLoop, floorType, level, isStructural);
            //                tx.Commit();
            //            }
            //        }
            //        using (Transaction tx = new Transaction(doc, "删除原始楼板"))
            //        {
            //            tx.Start();
            //            doc.Delete(originalFloor.Id);
            //            tx.Commit();
            //        }
            //        tg.Assimilate();
            //        TaskDialog.Show("成功", $"已成功将原始楼板拆分为 {profileLoops.Count} 个独立的楼板。");
            //        return Result.Succeeded;
            //    }
            //    catch (System.Exception ex)
            //    {
            //        message = "在拆分楼板时发生错误: " + ex.Message;
            //        tg.RollBack();
            //        return Result.Failed;
            //    }
            //}

            ////1002 临时隐藏非选中类别
            //// 1. 获取用户选择
            //ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds.Count == 0)
            //{
            //    TaskDialog.Show("提示", "请先选择至少一个构件。");
            //    return Result.Cancelled;
            //}
            //// 2. 收集选中构件的类别ID
            //var categoriesToIsolateIds = new HashSet<ElementId>();
            //foreach (ElementId id in selectedIds)
            //{
            //    Element elem = doc.GetElement(id);
            //    if (elem?.Category != null)
            //    {
            //        categoriesToIsolateIds.Add(elem.Category.Id);
            //    }
            //}
            //if (categoriesToIsolateIds.Count == 0)
            //{
            //    message = "选择的构件没有有效的类别。";
            //    return Result.Failed;
            //}
            //// 3. 收集当前视图中所有属于目标类别的元素ID
            //// 创建一个多类别过滤器
            //var categoryFilter = new ElementMulticategoryFilter(categoriesToIsolateIds.ToList());
            //// 使用过滤器在当前视图中查找所有匹配的元素
            //var collector = new FilteredElementCollector(doc, activeView.Id);
            //ICollection<ElementId> elementsToIsolate = collector.WherePasses(categoryFilter).ToElementIds();
            //if (elementsToIsolate.Count == 0)
            //{
            //    TaskDialog.Show("提示", "在当前视图中没有找到属于所选类别的可见构件。");
            //    return Result.Succeeded; // 操作本身是成功的，只是没有元素可隔离
            //}
            //using (Transaction tx = new Transaction(doc, "临时隔离类别"))
            //{
            //    tx.Start();
            //    // 创建用户熟悉的“临时隐藏/隔离”状态（青色边框）,可以通过 HR 快捷键重置
            //    activeView.IsolateElementsTemporary(elementsToIsolate);
            //    tx.Commit();
            //}

            //////0404 升级柱切板和梁，梁切板。使用 BuiltInCategory 枚举，而不是魔术数字
            //var structuralColumns = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralColumns)
            //    .WhereElementIsNotElementType().ToElements();

            //var structuralFraming = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_StructuralFraming)
            //    .WhereElementIsNotElementType().ToElements();

            //using (Transaction transaction = new Transaction(doc, "自动调整几何连接关系"))
            //{
            //    transaction.Start();

            //    // 1. 柱切割梁和楼板
            //    foreach (Element column in structuralColumns)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, column, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            // 使用类型安全的比较
            //            var categoryId = nearbyElem.Category.Id.IntegerValue;
            //            if (categoryId == (int)BuiltInCategory.OST_StructuralFraming || categoryId == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, column, nearbyElem);
            //            }
            //        }
            //    }
            //    // 2. 梁切割楼板
            //    foreach (Element beam in structuralFraming)
            //    {
            //        List<Element> nearbyElements = GetIntersectingElements(doc, beam, 0.1); // 稍微扩大搜索范围
            //        foreach (Element nearbyElem in nearbyElements)
            //        {
            //            if (nearbyElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors)
            //            {
            //                EnsureJoinOrder(doc, beam, nearbyElem);
            //            }
            //        }
            //    }
            //    transaction.Commit();
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
            //    if (hitElementIds == null) { TaskDialog.Show("结果", "没有检测到碰撞对象"); }
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
            ////0930 BuiltInFailures错误列表导出找特定名称
            //var result = FindBuiltInFailureByDescription("The duct/pipe has been modified to be in the opposite direction causing the connections to be invalid.");
            //TaskDialog.Show("tt", result);

            ////0930 BuiltInFailures错误列表导出
            ////https://forums.autodesk.com/t5/revit-api-forum/get-a-list-of-all-the-revit-warnings/m-p/10399203
            //Autodesk.Revit.ApplicationServices.Application app = commandData.Application.Application;
            //FailureDefinitionRegistry failureReg = Autodesk.Revit.ApplicationServices.Application.GetFailureDefinitionRegistry();
            //Type _type = typeof(BuiltInFailures);
            //Type[] _nested = _type.GetNestedTypes(System.Reflection.BindingFlags.Public);
            //Dictionary<Guid, Type> _dict = new Dictionary<Guid, Type>();
            //string _ClassName = string.Empty;
            //foreach (Type nt in _nested)
            //{
            //    try
            //    {
            //        _ClassName = nt.FullName.Replace('+', '.');
            //        sb.AppendLine(string.Format("#### {0} ####", _ClassName));
            //        foreach (System.Reflection.PropertyInfo pInfo in nt.GetProperties())
            //        {
            //            System.Reflection.MethodInfo mInfo = pInfo.GetGetMethod();
            //            FailureDefinitionId res = mInfo.Invoke(nt, null) as FailureDefinitionId;
            //            if (res == null) continue;
            //            if (_dict.ContainsKey(res.Guid)) continue;
            //            _dict.Add(res.Guid, nt);
            //            FailureDefinitionAccessor _acc = failureReg.FindFailureDefinition(res);
            //            if (_acc == null) continue;
            //            sb.AppendLine(string.Format("  * {0} <{1}> {2}", _acc.GetId().Guid, _acc.GetSeverity(), _ClassName));
            //            sb.AppendLine(string.Format("          {0}", _acc.GetDescriptionText()));
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}
            //_ClassName = "user-defined";
            //sb.AppendLine(string.Format("#### {0} ####", _ClassName));
            //foreach (FailureDefinitionAccessor _acc in failureReg.ListAllFailureDefinitions())
            //{
            //    Type DefType = null;
            //    _dict.TryGetValue(_acc.GetId().Guid, out DefType);
            //    if (DefType != null) continue;  // failure already listed
            //    sb.AppendLine(string.Format("  * {0} <{1}> {2}", _acc.GetId().Guid, _acc.GetSeverity(), _ClassName));
            //    sb.AppendLine(string.Format("          {0}", _acc.GetDescriptionText()));
            //}
            ////TaskDialog.Show("res", sb.ToString());
            //// 更完整的 CSV 导出方案
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string fileName = $"RevitFailureDefinitions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            //string filePath = Path.Combine(desktopPath, fileName);
            //try
            //{
            //    // 将 StringBuilder 内容转换为字符串
            //    string content = sb.ToString();
            //    // 处理内容以符合 CSV 格式
            //    content = content.Replace("#### ", "").Replace("####", "");
            //    content = content.Replace("  * ", "");
            //    content = content.Replace("          ", ",");
            //    // 添加 CSV 表头
            //    string header = "Category,GUID,Severity,Class,Description\n";
            //    content = header + content;
            //    File.WriteAllText(filePath, content);
            //    TaskDialog.Show("导出成功", $"故障定义已导出到：\n{filePath}");
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("导出错误", $"导出文件时出错：\n{ex.Message}");
            //}
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
