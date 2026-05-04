using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.models;
using CreatePipe.OfficalSamples;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : Decorator, IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        //0206 应与PipeJoinHorizon合并考虑
        ///// <summary>
        ///// 从风管管件的连接器出发，获取其“外部相邻”的两个端点连接器（不返回管件自身连接器）。
        ///// 只返回两端连接场景（例如：弯头/直接头/变径等）。三通等会返回 !=2 从而被跳过。
        ///// </summary>
        //private List<Connector> GetTwoEndNeighborConnectors(FamilyInstance fitting)
        //{
        //    var result = new List<Connector>();
        //    if (fitting?.MEPModel?.ConnectorManager == null) return result;
        //    ConnectorSet fitConns = fitting.MEPModel.ConnectorManager.Connectors;
        //    if (fitConns == null) return result;
        //    // 用于去重：同一 owner + connector id
        //    var seen = new HashSet<string>();
        //    foreach (Connector fitConn in fitConns)
        //    {
        //        if (fitConn == null || !fitConn.IsConnected) continue;
        //        foreach (Connector refConn in fitConn.AllRefs)
        //        {
        //            if (refConn == null) continue;
        //            // 排除引用到自己（防御）
        //            if (refConn.Owner != null && refConn.Owner.Id == fitting.Id) continue;
        //            // 只接受 MEP 连接器（End/Curve 等；这里不过度限制 Domain，避免附件/设备遗漏）
        //            if (refConn.Owner == null || refConn.Owner.Category == null) continue;
        //            string key = $"{refConn.Owner.Id.IntegerValue}:{refConn.Id}";
        //            if (seen.Add(key))
        //                result.Add(refConn);
        //        }
        //    }
        //    // 仅两端
        //    return result;
        //}
        ///// <summary>
        ///// 尝试 cFrom.ConnectTo(cTo)，并通过 AllRefs/IsConnected 验证两者是否真正互相引用。
        ///// </summary>
        //private bool TryConnectAndVerify(Connector cFrom, Connector cTo, Document doc)
        //{
        //    if (cFrom == null || cTo == null) return false;
        //    // 先短路：如果已经互相连接，则认为成功
        //    if (IsActuallyConnected(cFrom, cTo)) return true;
        //    // ConnectTo 可能抛异常（距离太远/系统不兼容/几何不满足等）
        //    cFrom.ConnectTo(cTo);
        //    doc.Regenerate();
        //    return IsActuallyConnected(cFrom, cTo);
        //}
        ///// <summary>
        ///// 严格判断：c1 的 AllRefs 中是否包含 c2（ownerId + connectorId 匹配）
        ///// </summary>
        //private bool IsActuallyConnected(Connector c1, Connector c2)
        //{
        //    if (c1 == null || c2 == null) return false;
        //    if (!c1.IsConnected || !c2.IsConnected) return false;
        //    int owner2 = c2.Owner?.Id.IntegerValue ?? -1;
        //    int cid2 = c2.Id;
        //    foreach (Connector r in c1.AllRefs)
        //    {
        //        if (r?.Owner == null) continue;
        //        if (r.Owner.Id.IntegerValue == owner2 && r.Id == cid2)
        //            return true;
        //    }
        //    return false;
        //}
        //private bool IsPhysicallyConnected(Connector c1, Connector c2)
        //{
        //    if (!c1.IsConnected || !c2.IsConnected) return false;
        //    foreach (Connector r in c1.AllRefs)
        //    {
        //        if (r.Owner?.Category?.Id.IntegerValue ==
        //            (int)BuiltInCategory.OST_DuctFitting)
        //            return true;
        //    }
        //    return false;
        //}
        //private bool TryCreateFitting(Connector c1, Connector c2, Document doc)
        //{
        //    if (c1 == null || c2 == null) return false;
        //    try
        //    {
        //        // 如果已经是物理连接，直接返回成功
        //        if (IsPhysicallyConnected(c1, c2))
        //            return true;
        //        // ✅ 强制创建风管管件（弯头/变径/直接头）
        //        FamilyInstance newFitting =
        //            MechanicalUtils.CreateDuctFitting(doc, c1, c2);
        //        doc.Regenerate();
        //        return newFitting != null;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private string SafeCatName(Element owner)
        //{
        //    try { return owner?.Category?.Name ?? owner?.GetType().Name ?? "<null>"; }
        //    catch { return "<unknown>"; }
        //}
        private void CheckMEPCurveSlope(Document document, List<ElementId> ids, double angle)
        {
            foreach (ElementId id in ids)
            {
                Element elem = document.GetElement(id);
                if (!(elem is MEPCurve mepCurve)) { continue; }
                switch (mepCurve)
                {
                    case Pipe pipe:
                        if (pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE).AsDouble() == angle)
                        {

                        }
                        TaskDialog.Show("提示", $"检查了x个管道系统,坡度异常管道清单: {pipe.Name}");
                        break;
                    case Duct duct:
                        if (duct.get_Parameter(BuiltInParameter.RBS_DUCT_SLOPE).AsDouble() == angle)
                        {

                        }
                        TaskDialog.Show("提示", $"检查了x个风管系统,坡度异常风管清单: {duct.Name}");
                        break;
                    case CableTray tray:
                        if (tray.get_Parameter(BuiltInParameter.RBS_START_OFFSET_PARAM).AsDouble() != tray.get_Parameter(BuiltInParameter.RBS_END_OFFSET_PARAM).AsDouble())
                        {

                        }
                        TaskDialog.Show("提示", $"检查了x个桥架系统,坡度异常风管清单: {tray.Name}");
                        break;
                    case Conduit conduit:
                        TaskDialog.Show("提示", "暂不支持线管检查");
                        break;
                    default:
                        // 其他未定义的 MEPCurve 类型
                        break;
                }
            }
        }
        public void TestTemporaryMovement(Document doc, ElementId wallId)
        {
            bool isColliding = false;
            // 开启测试事务，设置 rollback: true，无论成功失败最后必定回滚
            doc.NewTransaction(() =>
            {
                // 1. 将墙临时偏移 500mm
                ElementTransformUtils.MoveElement(doc, wallId, new XYZ(500 / 304.8, 0, 0));
                // 2. 重新生成文档以获取最新的几何图形
                doc.Regenerate();
                // 3. 执行干涉检查逻辑，把结果存到外部变量 isColliding 中
                // isColliding = CheckCollision(...);
            }, "临时干涉检查", true); // <--- 注意这里的 true 代表强制回滚
            // 执行到这里时，墙已经恢复到了原来的位置，但你拿到了干涉结果 isColliding
            TaskDialog.Show("检查结果", isColliding ? "发生碰撞" : "未发生碰撞");
        }
        private Category GetElectricalEquipmentCategory(Document doc)
        {
            return Category.GetCategory(doc, BuiltInCategory.OST_ElectricalFixtures);
        }
        private bool ProcessFamilyFile(UIApplication app, string filePath, out string message)
        {
            message = string.Empty;
            Document familyDoc = null;
            try
            {
                familyDoc = app.Application.OpenDocumentFile(filePath);

                if (!familyDoc.IsFamilyDocument)
                {
                    message = "不是有效的族文件";
                    return false;
                }
                Category currentCategory = familyDoc.OwnerFamily.FamilyCategory;
                // 检查是否已经是电气装置
                if (currentCategory?.Id.IntegerValue == (int)BuiltInCategory.OST_ElectricalFixtures)
                {
                    message = "已是电气装置类别";
                    return false;
                }
                // 获取电气装置类别
                Category electricalCategory = GetElectricalEquipmentCategory(familyDoc);
                if (electricalCategory == null)
                {
                    message = "未找到'电气装置'类别";
                    return false;
                }
                using (Transaction trans = new Transaction(familyDoc, "修改族类别"))
                {
                    trans.Start();
                    familyDoc.OwnerFamily.FamilyCategory = electricalCategory;
                    trans.Commit();
                }
                familyDoc.Save();
                message = "修改成功";
                return true;
            }
            catch (Exception ex)
            {
                message = $"错误: {ex.Message}";
                return false;
            }
            finally
            {
                familyDoc?.Close(false);
            }
        }
        private void ShowResult(List<string> success, List<string> failed, List<string> skipped)
        {
            string resultMessage = "";

            if (success.Count > 0)
            {
                resultMessage += $"✅ 成功修改 ({success.Count} 个):\n{string.Join("\n", success)}\n\n";
            }

            if (skipped.Count > 0)
            {
                resultMessage += $"⚠️ 已跳过 ({skipped.Count} 个 - 已是电气装置):\n{string.Join("\n", skipped)}\n\n";
            }

            if (failed.Count > 0)
            {
                resultMessage += $"❌ 修改失败 ({failed.Count} 个):\n{string.Join("\n", failed)}\n\n";
            }

            if (string.IsNullOrEmpty(resultMessage))
            {
                resultMessage = "没有处理任何文件";
            }

            TaskDialog.Show("批量修改完成", resultMessage);
        }
        //找实例共同文字属性列表     
        public Dictionary<string, string> GetCommonStringParameterNames(Document doc)
        {
            // 1. 收集文档中所有的族实例
            var allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
                .Where(e => e.HasPhases()).Cast<FamilyInstance>().ToList();
            if (allInstances.Count == 0) return new Dictionary<string, string>();
            // 分别追踪实例参数和类型参数的交集
            HashSet<string> commonInstanceParams = null;
            HashSet<string> commonSymbolParams = null;
            // 2. 遍历所有实例，分别求交集
            foreach (FamilyInstance instance in allInstances)
            {
                // 收集当前实例的实例参数
                var currentInstanceParams = new HashSet<string>();
                foreach (Parameter param in instance.Parameters)
                {
                    if (param.StorageType == StorageType.String && !param.IsReadOnly)
                    {
                        currentInstanceParams.Add(param.Definition.Name);
                    }
                }
                // 收集当前实例的类型参数
                var currentSymbolParams = new HashSet<string>();
                FamilySymbol symbol = instance.Symbol;
                if (symbol != null)
                {
                    foreach (Parameter param in symbol.Parameters)
                    {
                        if (param.StorageType == StorageType.String && !param.IsReadOnly)
                        {
                            currentSymbolParams.Add(param.Definition.Name);
                        }
                    }
                }
                // 求交集
                if (commonInstanceParams == null)
                {
                    commonInstanceParams = new HashSet<string>(currentInstanceParams);
                }
                else
                {
                    commonInstanceParams.IntersectWith(currentInstanceParams);
                }
                if (commonSymbolParams == null)
                {
                    commonSymbolParams = new HashSet<string>(currentSymbolParams);
                }
                else
                {
                    commonSymbolParams.IntersectWith(currentSymbolParams);
                }
                // 提前退出
                if (commonInstanceParams.Count == 0 && commonSymbolParams.Count == 0)
                    break;
            }
            // 3. 组装字典结果，标记来源
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 标记仅实例共有的参数
            foreach (var name in commonInstanceParams ?? Enumerable.Empty<string>())
            {
                result[name] = "实例";
            }
            // 标记仅类型共有的参数，或两者共有
            foreach (var name in commonSymbolParams ?? Enumerable.Empty<string>())
            {
                if (result.ContainsKey(name))
                {
                    // 实例和类型都有同名参数
                    result[name] = "两者";
                }
                else
                {
                    result[name] = "类型";
                }
            }
            return result;
        }
        //0428 原TEst10方法
        //生成柱子
        // 截断小数位数的方法
        private double CutDecimalWithN(double value, int decimalPlaces)
        {
            double factor = Math.Pow(10, decimalPlaces);
            return Math.Truncate(value * factor) / factor;
        }
        private void CreatColu(Document doc, XYZ point, double b, double h)
        {
            FilteredElementCollector fil = new FilteredElementCollector(doc);
            fil.OfClass(typeof(FamilySymbol));
            string bh = CutDecimalWithN(b * 304.8, 4).ToString() + " " + "x" + " " + CutDecimalWithN(h * 304.8, 4);
            List<FamilySymbol> listFa = new List<FamilySymbol>();
            foreach (FamilySymbol fa in fil)
            {
                // 更安全的参数获取方式
                Parameter familyNameParam = fa.LookupParameter("族名称");
                if (familyNameParam != null && familyNameParam.AsString() == "CADC_柱-混凝土-矩形")
                {
                    listFa.Add(fa);
                }
            }
            if (listFa.Count == 0)
            {
                TaskDialog.Show("错误", "未找到名为'CADC_柱-混凝土-矩形'的族类型");
                return;
            }
            FamilySymbol targetSymbol = null;
            // 查找匹配的族类型
            foreach (FamilySymbol symbol in listFa)
            {
                if (bh == symbol.Name)
                {
                    targetSymbol = symbol;
                    break;
                }
            }
            if (targetSymbol != null)
            {
                // 确保族类型已激活
                if (!targetSymbol.IsActive) targetSymbol.Activate();
                doc.Create.NewFamilyInstance(point, targetSymbol, StructuralType.Column);
            }
            else
            {
                // 复制创建新的族类型
                FamilySymbol fam = listFa[0];
                // 确保族类型已激活
                if (!fam.IsActive) fam.Activate();
                try
                {
                    FamilySymbol newSymbol = fam.Duplicate(bh) as FamilySymbol;
                    // 设置参数 - 使用更安全的参数查找方式
                    Parameter widthParam = newSymbol.LookupParameter("b");
                    Parameter heightParam = newSymbol.LookupParameter("h");
                    if (widthParam != null && heightParam != null)
                    {
                        using (Transaction t = new Transaction(doc, "设置柱参数"))
                        {
                            t.Start();
                            widthParam.Set(b);
                            heightParam.Set(h);
                            t.Commit();
                        }
                        doc.Create.NewFamilyInstance(point, newSymbol, StructuralType.Column);
                    }
                    else
                    {
                        TaskDialog.Show("错误", "找不到截面宽度或截面高度参数");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"创建族类型失败: {ex.Message}");
                }
            }
        }
        //private void CreatColu(Document doc, XYZ point, double b, double h)
        //{
        //    FilteredElementCollector fil = new FilteredElementCollector(doc);
        //    fil.OfClass(typeof(FamilySymbol));
        //    string bh = CutDecimalWithN(b * 304.8, 4).ToString() + " " + "x" + " " + CutDecimalWithN(h * 304.8, 4);
        //    List<FamilySymbol> listFa = new List<FamilySymbol>();
        //    foreach (FamilySymbol fa in fil)
        //    {
        //        if (fa.GetParameters("族名称")[0].AsString() == "砼矩形柱")
        //        {
        //            listFa.Add(fa);
        //        }
        //    }
        //    int i = 0;
        //    bool bo = false;
        //    int j = 0;
        //    for (i = 0; i < listFa.Count; i++)
        //    {
        //        if (bh == listFa[i].Name)
        //        {
        //            bo = true;
        //            j = i;
        //        }
        //    }
        //    if (bo == true)
        //    {
        //        doc.Create.NewFamilyInstance(point, listFa[j], StructuralType.Column);
        //    }
        //    else
        //    {
        //        FamilySymbol fam = listFa[0];
        //        ElementType coluType = fam.Duplicate(bh);
        //        coluType.GetParameters("截面宽度")[0].Set(b);
        //        coluType.GetParameters("截面高度")[0].Set(h);
        //        FamilySymbol fs = coluType as FamilySymbol;
        //        doc.Create.NewFamilyInstance(point, fs, StructuralType.Column);
        //    }
        //}
        ///// <summary>
        ///// 在视图中绘制可见性分析的结果
        ///// </summary>
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
        ///// <summary>
        ///// 找到一组点的2D凸包 (投影到XY平面)
        ///// 这是一个简化的凸包算法 (Gift wrapping algorithm)
        ///// </summary>
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
        ///// <summary>
        ///// 获取或创建用于可视化的图形样式
        ///// </summary>
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
        ///// <summary>
        ///// 查找最适合进行射线检测的3D视图
        ///// </summary>
        //private View3D FindBest3DView(Document doc)
        //{
        //    var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
        //    View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
        //    return default3DView ?? collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        //}
        ///// <summary>
        ///// 从楼板的实体几何体中提取其底面的所有轮廓环路。
        ///// </summary>
        ///// <param name="floor">要分析的楼板</param>
        ///// <returns>包含所有轮廓的CurveArray列表</returns>
        private List<CurveArray> GetFloorLoopsFromGeometry(Floor floor)
        {
            var loops = new List<CurveArray>();
            Options geomOptions = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, View = floor.Document.ActiveView };
            GeometryElement geoElem = floor.get_Geometry(geomOptions);
            if (geoElem == null) return null;
            Solid solid = geoElem.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);
            if (solid == null) return null;
            PlanarFace bottomFace = null;
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
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
        ///// <summary>
        ///// 确保两个元素被连接，并且第一个元素切割第二个元素。
        ///// </summary>
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
        ///// <summary>
        ///// 获取与给定元素包围盒相交的元素（梁和楼板）。
        ///// </summary>
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
        ////1003 
        ///// <summary>
        ///// 查找一个最适合进行射线检测的3D视图。
        ///// 优先选择默认的 {3D} 视图，因为它通常包含所有模型元素。
        ///// </summary>
        ///// 重复方法回头看一下是否去重？
        private static View3D FindBest3DView(Document doc)
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            // 优先寻找默认的 {3D} 视图
            View3D default3DView = collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && v.Name == "{3D}");
            if (default3DView != null)
            {
                return default3DView;
            }
            // 如果找不到，再寻找任何一个非模板的3D视图作为备用
            return collector.Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
        }
        /////代码解析
        ////输入：通过 PickPoint 和 PickObject(ObjectType.Face)，我们精确地获取了观察点和用户想要分析的那个面。这是最可靠的方式。
        ////采样：
        ////targetFace.GetBoundingBox() 获取了面的 UV 参数范围。
        ////通过双重 for 循环，我们在 UV 空间中均匀地创建了一个网格。
        ////targetFace.Evaluate(new UV(u, v)) 将二维的 UV 参数转换为了三维世界坐标 XYZ。
        ////可见性检测：
        ////ReferenceIntersector 的构造函数传入了 targetElement.Id，这是一个优化，它会忽略目标本身，防止射线总是命中自己。但为了逻辑的严谨性，我们还是通过比较距离来判断，这样更通用。
        ////distanceToFace 是原始距离，hitDistance 是碰撞距离。Math.Abs(hitDistance - distanceToFace) < tolerance 是判断是否命中了目标本身的关键。
        ////可视化：
        ////"最大最小可见范围" 的最佳数学表达是可见区域的轮廓。
        ////我提供了一个简化的 凸包（Convex Hull） 算法 FindConvexHull 来找到包围所有可见点的最小多边形。这个多边形就是可见区域的边界。
        ////DrawVisibilityResults 方法做了两件事：
        ////用模型线在标记牌上绘制出这个凸包轮廓。
        ////从观察点向凸包的每个顶点发射连线，形成一个视锥（Viewing Frustum），直观地展示了可见范围。
        ////为了让绘制的线更醒目，我写了一个 GetOrCreateGraphicsStyle 方法来创建一个红色的、较粗的线样式。
        ///// <summary>
        ///// 执行射线检测并返回最近的碰撞图元ID
        ///// </summary>
        ///// <param name="doc">Revit文档</param>
        ///// <param name="origin">射线起点</param>
        ///// <param name="direction">射线方向</param>
        ///// <param name="view">用于检测的视图（可选）</param>
        ///// <returns>碰撞到的第一个图元的ElementId，如果没有碰撞则返回ElementId.InvalidElementId</returns>
        //public static ElementId RaycastNearest(Document doc, XYZ origin, XYZ direction, double deltaHeight, Autodesk.Revit.DB.View view = null)
        //{
        //    // 规范化方向向量
        //    direction = direction.Normalize();
        //    // 创建ReferenceIntersector
        //    ReferenceIntersector intersector;
        //    if (view != null)
        //    {
        //        intersector = new ReferenceIntersector((View3D)view);
        //    }
        //    else
        //    {
        //        // 使用3D视图设置进行检测
        //        intersector = new ReferenceIntersector(Find3DView(doc) ?? throw new System.Exception("找不到可用的3D视图"));
        //    }
        //    // 设置查找最近的交点
        //    intersector.TargetType = FindReferenceTarget.Face;
        //    intersector.FindReferencesInRevitLinks = true;
        //    XYZ originptWithHeight = new XYZ(origin.X, origin.Y, deltaHeight / 304.8);
        //    // 执行射线检测
        //    ReferenceWithContext referenceWithContext = intersector.FindNearest(originptWithHeight, direction);
        //    //ReferenceWithContext referenceWithContext = intersector.FindNearest(origin, direction);
        //    if (referenceWithContext == null) return ElementId.InvalidElementId;
        //    // 获取碰撞图元的ElementId
        //    Reference reference = referenceWithContext.GetReference();
        //    return reference?.ElementId ?? ElementId.InvalidElementId;
        //}
        //private static View3D Find3DView(Document doc)
        //{
        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    collector.OfClass(typeof(View3D));
        //    foreach (View3D view in collector)
        //    {
        //        if (!view.IsTemplate && view.Name != "{3D}") return view;
        //    }
        //    return null;
        //}
        //Action<string> onSelected = selectedName =>
        //{
        //    Autodesk.Revit.UI.TaskDialog.Show("tt", selectedName);
        //};
        //public bool IsBoundingBoxContained(BoundingBoxXYZ container, BoundingBoxXYZ contained)
        //{
        //    // 检查 contained 的最小点是否在 container 内
        //    bool minContained = container.Min.X <= contained.Min.X &&
        //                        container.Min.Y <= contained.Min.Y &&
        //                        container.Min.Z <= contained.Min.Z;

        //    // 检查 contained 的最大点是否在 container 内
        //    bool maxContained = container.Max.X >= contained.Max.X &&
        //                        container.Max.Y >= contained.Max.Y &&
        //                        container.Max.Z >= contained.Max.Z;

        //    return minContained && maxContained;
        //}
        ///// <returns>如果在房间内则返回true，否则返回false</returns>
        //public bool IsAnyPartOfStairInRoom(Stairs stair, Room room, Document doc)
        //{
        //    // 1. 检查所有梯段 (StairsRun)
        //    foreach (ElementId runId in stair.GetStairsRuns())
        //    {
        //        Element runElem = doc.GetElement(runId);
        //        if (IsElementCenterInRoom(runElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"梯段 {runId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个梯段在，就返回true
        //        }
        //    }
        //    // 2. 检查所有平台 (StairsLanding)
        //    foreach (ElementId landingId in stair.GetStairsLandings())
        //    {
        //        Element landingElem = doc.GetElement(landingId);
        //        if (IsElementCenterInRoom(landingElem, room))
        //        {
        //            // TaskDialog.Show("Debug", $"平台 {landingId} 在房间内。"); // 用于调试
        //            return true; // 只要有一个平台在，就返回true
        //        }
        //    }
        //    // 如果所有子构件都不在房间内，则认为整个楼梯不在
        //    return false;
        //}
        ///// <summary>
        ///// 辅助方法：检查一个元素的包围盒中心点是否在房间内。物体与房间关系
        ///// </summary>
        //private bool IsElementCenterInRoom(Element elem, Room room)
        //{
        //    if (elem == null || room == null) return false;
        //    BoundingBoxXYZ bbox = elem.get_BoundingBox(null); // 使用全局坐标，不依赖视图
        //    if (bbox == null || !bbox.Enabled) return false;
        //    XYZ centerPoint = (bbox.Min + bbox.Max) / 2.0;
        //    return room.IsPointInRoom(centerPoint);
        //}

        //private void CreatePipe(Document doc, Connector connector, double length,)
        //{
        //    // 创建管道
        //    Pipe pipe = Pipe.Create(doc, connector., connector.Origin, length, connector.Direction);
        //    pipe.LookupParameter("系统类型").Set(connector.MEPSystem.TypeId);
        //}
        //private void CreateDuct(Document doc, Connector connector, double length)
        //{
        //    // 创建风管
        //    Duct duct = Duct.Create(doc, connector.Level.Id, connector.Origin, length, connector.Direction);
        //    duct.LookupParameter("系统类型").Set(connector.MEPSystem.TypeId);
        //}
        //private void CreateCableTray(Document doc, Connector connector, double length)
        //{
        //    // 创建电缆桥架
        //    CableTray cableTray = CableTray.Create(doc, connector.Level.Id, connector.Origin, length, connector.Direction);
        //    cableTray.LookupParameter("系统类型").Set(connector.MEPSystem.TypeId);
        //}
        // 在指定点查找MEP曲线
        private MEPCurve FindMEPCurveAtPoint(UIDocument uiDoc, double offsetHeight, XYZ point)
        {
            MEPCurve result = null;
            try
            {
                Document doc = uiDoc.Document;
                // 获取当前视图的标高
                View activeView = doc.ActiveView;
                Level currentLevel = doc.GetElement(activeView.GenLevel.Id) as Level;
                if (currentLevel == null)
                {
                    TaskDialog.Show("错误", "无法获取当前视图的标高");
                    return null;
                }
                // 计算目标高度（当前标高 + 2000mm）
                double targetElevation = currentLevel.Elevation + offsetHeight;
                // 收集所有MEP曲线
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(MEPCurve));
                // 遍历所有MEP曲线，检查是否符合条件
                foreach (MEPCurve mepCurve in collector)
                {
                    // 检查MEP曲线的标高
                    Parameter levelParam = mepCurve.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                    if (levelParam == null) continue;

                    ElementId levelId = levelParam.AsElementId();
                    Level mepLevel = doc.GetElement(levelId) as Level;
                    if (mepLevel == null) continue;
                    // 检查是否在当前楼层标高（考虑标高容差）
                    double levelDifference = Math.Abs(mepLevel.Elevation - currentLevel.Elevation);
                    if (levelDifference > 0.1) // 容差约30mm
                        continue;
                    // 获取MEP曲线的位置曲线
                    LocationCurve locationCurve = mepCurve.Location as LocationCurve;
                    if (locationCurve == null) continue;
                    Curve curve = locationCurve.Curve;
                    if (curve == null) continue;
                    // 创建垂直投影线（从目标高度向下）
                    XYZ projectionStart = new XYZ(point.X, point.Y, targetElevation + 1.0); // 稍微高于目标高度
                    XYZ projectionEnd = new XYZ(point.X, point.Y, targetElevation - 1.0);   // 稍微低于目标高度
                    Line verticalLine = Line.CreateBound(projectionStart, projectionEnd);
                    // 检查垂直投影线是否与MEP曲线相交
                    SetComparisonResult setComparison = curve.Intersect(verticalLine, out IntersectionResultArray results);
                    if (setComparison == SetComparisonResult.Disjoint)
                        continue;
                    if (setComparison == SetComparisonResult.Overlap && results != null && results.Size > 0)
                    {
                        // 找到相交的MEP曲线
                        result = mepCurve;
                        break;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("查找管道错误", ex.Message);
                return null;
            }
        }
        //private MEPCurve FindMEPCurveAtPoint(UIDocument doc, XYZ point)
        //{
        //    try
        //    {
        //        // 使用ReferenceIntersector查找与点相交的MEP曲线
        //        ElementClassFilter mepFilter = new ElementClassFilter(typeof(MEPCurve));
        //        ReferenceIntersector refIntersector = new ReferenceIntersector(mepFilter, FindReferenceTarget.Element, doc.ActiveView);

        //        // 在点周围小范围内查找
        //        XYZ searchDirection = new XYZ(0, 0, -1); // 向下搜索
        //        ReferenceWithContext referenceWithContext = refIntersector.FindNearest(point, searchDirection);

        //        if (referenceWithContext != null)
        //        {
        //            Reference reference = referenceWithContext.GetReference();
        //            Element element = doc.GetElement(reference.ElementId);
        //            return element as MEPCurve;
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("查找管道错误", ex.Message);
        //        return null;
        //    }
        //}
        // 管道退后方法
        //private void RetreatMEPCurve(MEPCurve mepCurve, XYZ breakPoint, double retreatDistance)
        //{
        //    try
        //    {
        //        LocationCurve locationCurve = mepCurve.Location as LocationCurve;
        //        if (locationCurve == null) return;
        //        Curve currentCurve = locationCurve.Curve;
        //        XYZ startPoint = currentCurve.GetEndPoint(0);
        //        XYZ endPoint = currentCurve.GetEndPoint(1);
        //        // 确定退后方向
        //        XYZ curveDirection = (endPoint - startPoint).Normalize();
        //        // 根据退后距离的正负决定退后方向
        //        XYZ retreatVector = curveDirection * retreatDistance;
        //        // 判断哪一端靠近打断点
        //        double distanceToStart = breakPoint.DistanceTo(startPoint);
        //        double distanceToEnd = breakPoint.DistanceTo(endPoint);
        //        Curve newCurve;
        //        if (distanceToStart < distanceToEnd)
        //        {
        //            // 靠近起点端，移动起点
        //            XYZ newStartPoint = startPoint + retreatVector;
        //            // 确保新起点在曲线上且不超出范围
        //            if (IsPointOnCurveSegment(currentCurve, newStartPoint))
        //            {
        //                newCurve = Line.CreateBound(newStartPoint, endPoint);
        //            }
        //            else
        //            {
        //                // 如果无法退后，保持原曲线
        //                newCurve = currentCurve;
        //            }
        //        }
        //        else
        //        {
        //            // 靠近终点端，移动终点
        //            XYZ newEndPoint = endPoint + retreatVector;
        //            // 确保新终点在曲线上且不超出范围
        //            if (IsPointOnCurveSegment(currentCurve, newEndPoint))
        //            {
        //                newCurve = Line.CreateBound(startPoint, newEndPoint);
        //            }
        //            else
        //            {
        //                // 如果无法退后，保持原曲线
        //                newCurve = currentCurve;
        //            }
        //        }
        //        locationCurve.Curve = newCurve;
        //    }
        //    catch (Exception ex)
        //    {
        //        // 退后失败时忽略错误，继续执行
        //        System.Diagnostics.Debug.WriteLine($"管道退后失败: {ex.Message}");
        //    }
        //}
        // 检查点是否在曲线段上
        //private bool IsPointOnCurveSegment(Curve curve, XYZ point)
        //{
        //    try
        //    {
        //        // 获取曲线参数范围
        //        double startParam = curve.GetEndParameter(0);
        //        double endParam = curve.GetEndParameter(1);

        //        // 将点投影到曲线上获取参数
        //        IntersectionResult result = curve.Project(point);
        //        if (result != null)
        //        {
        //            double pointParam = result.Parameter;
        //            // 检查参数是否在曲线范围内（包含一定的容差）
        //            return pointParam >= startParam - 0.001 && pointParam <= endParam + 0.001;
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        //public void ConnectPipes(Document doc, ElementId pipeId1, ElementId pipeId2, ElementId pipeId3, ElementId pipeId4 = null)
        public void ConnectPipes(Document doc, ElementId pipeId1, ElementId pipeId2, ElementId pipeId3, ElementId pipeId4 = null)
        {
            using (Transaction trans = new Transaction(doc, "Connect Pipes"))
            {
                trans.Start();
                try
                {
                    Pipe pipe1 = doc.GetElement(pipeId1) as Pipe;
                    Pipe pipe2 = doc.GetElement(pipeId2) as Pipe;
                    Pipe pipe3 = doc.GetElement(pipeId3) as Pipe;
                    // 三管连接（T型）
                    if (pipeId4 == null)
                    {
                        Connector c1 = GetUnusedConnector(pipe1);
                        Connector c2 = GetUnusedConnector(pipe2);
                        Connector c3 = GetUnusedConnector(pipe3);
                        // 创建三通连接件
                        FamilyInstance tee = doc.Create.NewTeeFitting(c1, c2, c3);
                    }
                    // 四管连接（十字型）
                    else
                    {
                        //Pipe pipe4 = doc.GetElement(pipeId4) as Pipe;
                        //Connector c4 = GetUnusedConnector(pipe4);
                        //// 获取连接点并创建四通
                        //ConnectorSet connSet = new ConnectorSet();
                        //connSet.Insert(GetUnusedConnector(pipe1));
                        //connSet.Insert(GetUnusedConnector(pipe2));
                        //connSet.Insert(GetUnusedConnector(pipe3));
                        //connSet.Insert(c4);
                        //FamilyInstance cross = doc.Create.NewCrossFitting(connSet);
                    }
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("Error", ex.Message);
                }
            }
        }
        private Connector GetUnusedConnector(Pipe pipe)
        {
            ConnectorSet connectors = pipe.ConnectorManager.Connectors;
            foreach (Connector conn in connectors)
            {
                if (!conn.IsConnected) return conn;
            }
            return null;
        }
        // 获取元素的所有连接器
        private List<Connector> GetConnectors(Element element)
        {
            List<Connector> connectors = new List<Connector>();
            if (element is FamilyInstance familyInstance)
            {
                ConnectorSet connectorSet = null;
                // 检查是否为风管末端设备
                if (familyInstance.MEPModel != null)
                {
                    connectorSet = familyInstance.MEPModel.ConnectorManager.Connectors;
                }
                if (connectorSet != null)
                {
                    foreach (Connector connector in connectorSet)
                    {
                        connectors.Add(connector);
                    }
                }
            }
            return connectors;
        }
        // 递归获取所有相连的元素
        private List<ElementId> GetAllConnectedElements(List<Connector> startConnectors, Document doc)
        {
            List<ElementId> connectedElements = new List<ElementId>();
            Queue<Connector> connectorsToProcess = new Queue<Connector>(startConnectors);
            HashSet<ElementId> processedElements = new HashSet<ElementId>();

            while (connectorsToProcess.Count > 0)
            {
                Connector currentConnector = connectorsToProcess.Dequeue();
                Element currentElement = currentConnector.Owner;

                if (currentElement == null || processedElements.Contains(currentElement.Id))
                    continue;

                // 标记当前元素已处理
                processedElements.Add(currentElement.Id);

                // 如果不是初始的风口，添加到要删除的列表
                if (!startConnectors.Any(c => c.Owner.Id == currentElement.Id))
                {
                    connectedElements.Add(currentElement.Id);
                }

                // 获取当前连接器的所有连接
                ConnectorSet connectorSet = null;

                if (currentElement is FamilyInstance familyInstance && familyInstance.MEPModel != null)
                {
                    connectorSet = familyInstance.MEPModel.ConnectorManager.Connectors;
                }
                else if (currentElement is MEPCurve mepCurve)
                {
                    connectorSet = mepCurve.ConnectorManager.Connectors;
                }

                if (connectorSet != null)
                {
                    foreach (Connector connector in connectorSet)
                    {
                        // 获取连接器连接到的其他连接器
                        foreach (Connector connectedConnector in connector.AllRefs)
                        {
                            Element connectedElement = connectedConnector.Owner;

                            if (connectedElement != null &&
                                !processedElements.Contains(connectedElement.Id) &&
                                connectedElement.Id != currentElement.Id)
                            {
                                // 添加到待处理队列
                                connectorsToProcess.Enqueue(connectedConnector);
                            }
                        }
                    }
                }
            }

            return connectedElements;
        }
        // 删除所有相连的元素
        private void DeleteConnectedElements(Document doc, List<ElementId> elementIds)
        {
            if (elementIds.Count > 0)
            {
                doc.Delete(elementIds);
            }
        }
        // 设置风口高度
        private void SetTerminalHeight(Element terminal, double heightMm)
        {
            // 将毫米转换为Revit内部单位（英尺）
            double height = heightMm / 304.8;
            // 获取高度参数
            Parameter elevationParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevationParam != null && elevationParam.IsReadOnly == false)
            {
                elevationParam.Set(height);
            }
            else
            {
                // 尝试其他可能的高度参数
                Parameter levelOffsetParam = terminal.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (levelOffsetParam != null && levelOffsetParam.IsReadOnly == false)
                {
                    levelOffsetParam.Set(height);
                }
                else
                {
                    // 尝试通过实例属性设置
                    Parameter offsetParam = terminal.LookupParameter("Offset");
                    if (offsetParam != null && offsetParam.IsReadOnly == false)
                    {
                        offsetParam.Set(height);
                    }
                }
            }
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

          


            //0426 文本框输入数字限定测试
            //TestWindow testWindow = new TestWindow(uiApp);
            //testWindow.ShowDialog();   

            ////0426 生成柱测试改，先基于通用combobox确定柱样式，需要在平面操作自动确定柱上下偏移。再确定圆柱或方柱（暂不考虑旋转角度）
            ////结构族SectionShape参数 symbol参数int值表示
            //var column = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new ColumnFilter(), "findA柱子")) as FamilyInstance;
            //TaskDialog.Show("tt", column.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_SHAPE).AsInteger().ToString());


            //////1102 结构柱翻模测试改造 https://zhuanlan.zhihu.com/p/108750783
            /////改为按标高打断管线,需要增加高度获取和。OK
            //////创建应用程序对象
            //try
            //{
            //    //开始事务
            //    using (Autodesk.Revit.DB.Transaction ts = new Autodesk.Revit.DB.Transaction(doc, "柱子翻模"))
            //    {
            //        ts.Start();
            //        Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement); //获取对象
            //        string ss = r.ConvertToStableRepresentation(doc); //转化为字符串
            //        Element elem = doc.GetElement(r);
            //        // 获取几何图元
            //        GeometryElement geoElem = elem.get_Geometry(new Options());
            //        GeometryObject geoObj = elem.GetGeometryObjectFromReference(r);
            //        //获取选中的cad图层
            //        Category targetCategory = null;
            //        ElementId graphicsStyleId = ElementId.InvalidElementId;
            //        //判断所选取的几何对象样式不为元素无效值
            //        if (geoObj != null && geoObj.GraphicsStyleId != ElementId.InvalidElementId)
            //        {
            //            graphicsStyleId = geoObj.GraphicsStyleId;
            //            GraphicsStyle gs = doc.GetElement(geoObj.GraphicsStyleId) as GraphicsStyle; //获得所选对象图形样式
            //            if (gs != null)
            //            {
            //                //图层及图层名字
            //                targetCategory = gs.GraphicsStyleCategory;
            //                string layerName = gs.GraphicsStyleCategory.Name;
            //            }
            //            double offsetHeight = 2000 / 304.8;
            //            ////隐藏选中的cad图层
            //            if (targetCategory != null)
            //            {
            //                //doc.ActiveView.SetCategoryHidden(targetCategory.Id, true);
            //            }
            //            CurveArray curveArray = new CurveArray();
            //            List<double> listdb = new List<double>();
            //            foreach (var gObj in geoElem)
            //            {
            //                GeometryInstance geomInstance = gObj as GeometryInstance;
            //                if (geomInstance != null)
            //                {
            //                    //坐标转换
            //                    Transform transform = geomInstance.Transform;
            //                    //TaskDialog.Show("tt", geomInstance.SymbolGeometry.Count().ToString());
            //                    //坐标空间
            //                    foreach (var insObj in geomInstance.SymbolGeometry)
            //                    {
            //                        if (insObj == null) continue;
            //                        // 检查图形样式ID是否匹配
            //                        if (insObj.GraphicsStyleId != graphicsStyleId)
            //                            continue;
            //                        //线类型 - 处理PolyLine
            //                        if (insObj is PolyLine polyLine)
            //                        {
            //                            //获取坐标点
            //                            IList<XYZ> points = polyLine.GetCoordinates();
            //                            XYZ pMax = polyLine.GetOutline().MaximumPoint;
            //                            XYZ pMin = polyLine.GetOutline().MinimumPoint;
            //                            //长和宽
            //                            double b = Math.Abs(pMin.X - pMax.X);
            //                            double h = Math.Abs(pMin.Y - pMax.Y);
            //                            //柱子的中点坐标+坐标转换
            //                            XYZ pp = pMax.Add(pMin) / 2;
            //                            pp = transform.OfPoint(pp);
            //                            ////////找到中点，向上找管道，打断并尝试两侧退后各100
            //                            ////MEPCurve mepCurveToBreak = FindMEPCurveAtPoint(uiDoc, offsetHeight, pp);
            //                            ////if (mepCurveToBreak != null)
            //                            ////{
            //                            ////    // 打断管道
            //                            ////    MEPCurve copiedMEPCurve = BreakMEPCurveByOne(doc, mepCurveToBreak, pp);
            //                            ////}
            //                            //CreatColu(doc, pp, b, h); //生成柱子
            //                        }
            //                        else if (insObj is Arc circle)
            //                        {
            //                            //XYZ pp = circle.Center;
            //                            //pp = transform.OfPoint(pp);
            //                            ////// 查找与投影点相交的MEP曲线
            //                            //MEPCurve mepCurveToBreak = FindMEPCurveAtPoint(uiDoc, offsetHeight, pp);
            //                            //if (mepCurveToBreak != null)
            //                            //{
            //                            //    // 打断管道
            //                            //    MEPCurve copiedMEPCurve = BreakMEPCurveByOne(doc, mepCurveToBreak, pp);
            //                            //}
            //                        }
            //                        else if (insObj is GeometryInstance instance)
            //                        {
            //                            //instance.Transform;
            //                            //
            //                        }
            //                        else
            //                        {
            //                            TaskDialog.Show("tt", "未检测到符合条件多段线");
            //                            return Result.Failed;
            //                        }
            //                    }
            //                }
            //            }
            //            ts.Commit();
            //        }
            //        else
            //        {
            //            ts.RollBack();
            //            TaskDialog.Show("错误", "无法获取有效的图形样式信息");
            //            return Result.Failed;
            //        }
            //        return Result.Succeeded;
            //    }
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户取消了选择操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    TaskDialog.Show("错误", $"执行过程中发生错误: {ex.Message}");
            //    return Result.Failed;
            //}
            ////例程结束

            ////////1003 检测A-B点之间可见
            ////try
            ////{
            ////    // --- 步骤 1: 获取用户输入 ---
            ////    // 1.1 获取观察点
            ////    XYZ observerPoint = uiDoc.Selection.PickPoint("请选择观察点 (眼睛的位置)");
            ////    // 1.2 获取目标面
            ////    Reference faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "请选择标记牌的正面");
            ////    Element targetElement = doc.GetElement(faceRef);
            ////    Face targetFace = targetElement.GetGeometryObjectFromReference(faceRef) as Face;
            ////    if (targetFace == null)
            ////    {
            ////        message = "未能获取有效的几何面。";
            ////        return Result.Failed;
            ////    }
            ////    // --- 步骤 2 & 3: 采样并进行可见性测试 ---
            ////    // 定义采样网格的密度 (例如 10x10)
            ////    int gridResolutionU = 15;
            ////    int gridResolutionV = 15;
            ////    List<XYZ> visiblePoints = new List<XYZ>();
            ////    List<XYZ> occludedPoints = new List<XYZ>();
            ////    BoundingBoxUV bbox = targetFace.GetBoundingBox();
            ////    UV min = bbox.Min;
            ////    UV max = bbox.Max;
            ////    // 准备 ReferenceIntersector
            ////    View3D view3D = FindBest3DView(doc);
            ////    if (view3D == null)
            ////    {
            ////        message = "需要一个3D视图来进行可见性分析。";
            ////        return Result.Failed;
            ////    }
            ////    ReferenceIntersector intersector = new ReferenceIntersector(targetElement.Id, FindReferenceTarget.Face, view3D);
            ////    intersector.FindReferencesInRevitLinks = true;
            ////    // 遍历采样网格
            ////    for (int i = 0; i <= gridResolutionU; i++)
            ////    {
            ////        for (int j = 0; j <= gridResolutionV; j++)
            ////        {
            ////            double u = min.U + (max.U - min.U) * i / gridResolutionU;
            ////            double v = min.V + (max.V - min.V) * j / gridResolutionV;
            ////            XYZ samplePointOnFace = targetFace.Evaluate(new UV(u, v));
            ////            XYZ direction = (samplePointOnFace - observerPoint).Normalize();
            ////            double distanceToFace = observerPoint.DistanceTo(samplePointOnFace);
            ////            // 执行射线检测
            ////            ReferenceWithContext refWithContext = intersector.FindNearest(observerPoint, direction);
            ////            bool isVisible = false;
            ////            double tolerance = 0.001; // 精度容差 (约0.3mm)
            ////            if (refWithContext == null)
            ////            {
            ////                // 射线未与任何物体相交，说明该点可见 (在开放空间中)
            ////                isVisible = true;
            ////            }
            ////            else
            ////            {
            ////                double hitDistance = refWithContext.Proximity;
            ////                // 如果碰撞点距离非常接近目标点，则认为是可见的
            ////                if (Math.Abs(hitDistance - distanceToFace) < tolerance)
            ////                {
            ////                    isVisible = true;
            ////                }
            ////            }
            ////            if (isVisible)
            ////            {
            ////                visiblePoints.Add(samplePointOnFace);
            ////            }
            ////            else
            ////            {
            ////                occludedPoints.Add(samplePointOnFace);
            ////            }
            ////        }
            ////    }
            ////    // --- 步骤 4: 可视化结果 ---
            ////    if (visiblePoints.Count == 0)
            ////    {
            ////        TaskDialog.Show("结果", "标记牌完全被遮挡，不可见。");
            ////        return Result.Succeeded;
            ////    }
            ////    using (Transaction tx = new Transaction(doc, "绘制可见性范围"))
            ////    {
            ////        tx.Start();
            ////        DrawVisibilityResults(doc, activeView, observerPoint, visiblePoints);
            ////        tx.Commit();
            ////    }
            ////    TaskDialog.Show("完成", $"可见性分析完成。\n可见采样点: {visiblePoints.Count}    已在视图中绘制可见范围。");
            ////    return Result.Succeeded;
            ////}
            ////catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            ////{
            ////    return Result.Cancelled;
            ////}
            ////catch (Exception ex)
            ////{
            ////    message = ex.Message;
            ////    return Result.Failed;
            ////}
            //////1003 升级二维射线检测方法
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

            //////1002 拆分楼板，读取出所有轮廓并分别保存多个楼板。注意存在逻辑问题，未处理环嵌套的问题，无法维持板内部开洞
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


            //////0830 已载入插件查找管理1，没啥用
            //var loadedApps = uiApp.LoadedApplications;
            ////var list = loadedApps.Cast<IExternalApplication>().Select(a => a.GetType().FullName).ToList();
            //var list = loadedApps.Cast<IExternalApplication>().ToList();
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in list)
            //{
            //    //stringBuilder.AppendLine(item.GetType().Assembly.Location.ToString());
            //    //stringBuilder.AppendLine(item.GetType().FullName.ToString());
            //    ////stringBuilder.AppendLine(item.GetType().AssemblyQualifiedName.ToString());
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString());

            ////1125 三管、四管连接试验,顺序会导致连接失败需要优化X
            //// 1. 拾取第一根管道
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第一根水平管道");
            //Pipe pipe1 = doc.GetElement(ref1) as Pipe;
            //// 2. 拾取第二根管道
            //Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第二根水平管道");
            //Pipe pipe2 = doc.GetElement(ref2) as Pipe;
            //Reference ref3 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第二根水平管道");
            //Pipe pipe3 = doc.GetElement(ref2) as Pipe;
            //ConnectPipes(doc, ref1.ElementId, ref2.ElementId, ref3.ElementId);
            ////1125 查找模型中所有垂直立管,并给出不同管径的管道数量OK
            //try
            //{
            //    // 1. 获取模型中所有管道
            //    FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Pipe));
            //    // 管径统计字典：键=管径（毫米或英寸），值=数量
            //    Dictionary<string, int> diameterCount = new Dictionary<string, int>();
            //    foreach (Pipe pipe in collector)
            //    {
            //        LocationCurve lc = pipe.Location as LocationCurve;
            //        if (lc == null) continue;
            //        Line line = lc.Curve as Line;
            //        if (line == null) continue;
            //        // 2. 判断是否为垂直方向（通过方向向量判断）
            //        XYZ dir = line.Direction.Normalize();
            //        // 容差判断：方向Z分量 ≈ 1 或 ≈ -1
            //        if (Math.Abs(Math.Abs(dir.Z) - 1.0) < 0.001)
            //        {
            //            // 3. 获取管径
            //            double diameterFeet = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            //            // 转为毫米（英尺 * 304.8）
            //            double diameterMM = diameterFeet * 304.8;
            //            string diameterStr = $"{Math.Round(diameterMM, 0)} mm";
            //            if (!diameterCount.ContainsKey(diameterStr))
            //                diameterCount[diameterStr] = 0;
            //            diameterCount[diameterStr]++;
            //        }
            //    }
            //    // 4. 输出结果
            //    string resultMsg = "垂直立管管径统计：\n";
            //    if (diameterCount.Count == 0)
            //    {
            //        resultMsg += "未找到垂直立管。";
            //    }
            //    else
            //    {
            //        foreach (var kvp in diameterCount.OrderBy(k => k.Key))
            //        {
            //            resultMsg += $"{kvp.Key} ： {kvp.Value} 条";
            //        }
            //    }
            //    TaskDialog.Show("立管统计", resultMsg);
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}
            ////////1207 风口清理和连接
            //try
            //{
            //    // 1. 选择风口
            //    using (Transaction trans = new Transaction(doc, "修改风管系统"))
            //    {
            //        trans.Start();
            //        //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new AirTerminalSelectionFilter(), "请选择一个风口");
            //        //Element terminal = doc.GetElement(reference);
            //        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //        if (selectedIds == null || selectedIds.Count == 0)
            //        {
            //            TaskDialog.Show("错误", "未选择任意");
            //            return Result.Failed;
            //        }
            //        List<Element> ductTerminals = new List<Element>();
            //        foreach (var id in selectedIds)
            //        {
            //            Element element = doc.GetElement(id);
            //            if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal)
            //            {
            //                ductTerminals.Add(element);
            //            }
            //        }
            //        if (ductTerminals == null)
            //        {
            //            TaskDialog.Show("错误", "未选择风口");
            //            return Result.Failed;
            //        }
            //        foreach (var item in ductTerminals)
            //        {
            //            // 2. 获取风口的所有连接器
            //            List<Connector> connectors = GetConnectors(item);
            //            if (connectors.Count == 0)
            //            {
            //                TaskDialog.Show("提示", "该风口没有连接器");
            //                return Result.Failed;
            //            }
            //            // 3. 获取所有相连的管件和风管
            //            List<ElementId> connectedElements = GetAllConnectedElements(connectors, doc);
            //            // 4. 删除所有相连的管件和风管
            //            DeleteConnectedElements(doc, connectedElements);
            //            // 5. 设置风口高度
            //            SetTerminalHeight(item, 3000);
            //        }
            //        trans.Commit();
            //        //TaskDialog.Show("完成",$"已删除 {connectedElements.Count} 个相连元素，并将风口高度设置为4000mm");
            //    }
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

            //////1105 批量左右镜像.OK
            //try
            //{
            //    FamilyInstance familyInstance;
            //    ICollection<ElementId> selectedIds = new HashSet<ElementId>();
            //    selectedIds = uiDoc.Selection.GetElementIds();
            //    using (Transaction tx = new Transaction(doc))
            //    {
            //        tx.Start("Mirror Element");
            //        if (selectedIds.Count == 0)
            //        {
            //            var reference = uiDoc.Selection.PickObjects(ObjectType.Element, new FamilyInstanceFilterClass(), "选择元素");
            //            foreach (var item in reference)
            //            {
            //                selectedIds.Add(item.ElementId);
            //            }
            //        }
            //        foreach (var id in selectedIds)
            //        {
            //            familyInstance = doc.GetElement(id) as FamilyInstance;
            //            // 获取构件的中心点
            //            LocationPoint locationPoint = familyInstance.Location as LocationPoint;
            //            XYZ centerPoint = locationPoint.Point;
            //            // 获取构件的局部坐标系
            //            Transform transform = familyInstance.GetTotalTransform();
            //            // 获取局部 X 轴方向（左右方向）
            //            XYZ xAxis = transform.BasisX;
            //            // 创建镜像平面（通过中心点和局部 X 轴方向）
            //            Plane mirrorPlane = Plane.CreateByNormalAndOrigin(xAxis, centerPoint);
            //            // 镜像构件
            //            ElementTransformUtils.MirrorElements(doc, new List<ElementId> { familyInstance.Id }, mirrorPlane, false);
            //        }
            //        tx.Commit();
            //    }
            //    //TaskDialog.Show("成功", "构件已按中心轴线镜像");
            //}
            //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //{
            //    // 用户取消操作
            //    return Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", ex.Message);
            //    return Result.Failed;
            //}
            //例程结束

            //////1029 管道属性批量填写,系统族批量可参考.OK
            //using (Transaction tx = new Transaction(doc, "管道属性批写入"))
            //{
            //    tx.Start();
            //    try
            //    {
            //        List<Pipe> allPipesInModel = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
            //        foreach (var pipe in allPipesInModel)
            //        {
            //            //TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
            //            double diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;
            //            double length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() * 304.8;
            //            // 参数配置字典
            //            var parameterConfigs = new Dictionary<string, string>
            //            {
            //                { "尺寸规格", $"DN{(int)diameter}" },
            //                { "直径", $"DN{(int)diameter}" },
            //                { "材质1", "钢管" },
            //                { "压力等级", "1.6MPa" },
            //                { "长度", $"{(int)length}mm" },
            //                { "系统类型", "喷淋" },
            //                { "坡度", "0" },
            //                { "保温材料", "柔性泡沫橡塑管壳" },
            //                { "保温厚度", "55mm" }
            //            };
            //            foreach (var config in parameterConfigs)
            //            {
            //                Parameter param = pipe.LookupParameter(config.Key);
            //                param?.Set(config.Value);
            //            }
            //            //简化前代码
            //            //Parameter parameter1 = item.LookupParameter("尺寸规格");
            //            //if (parameter1 != null)
            //            //{
            //            //    parameter1.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
            //            //}
            //            //Parameter parameter2 = item.LookupParameter("直径");
            //            //if (parameter2 != null)
            //            //{
            //            //    parameter2.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
            //            //}
            //        }
            //        //////属性测试
            //        ////Pipe item = doc.GetElement(uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterPipe()).ElementId) as Pipe;
            //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
            //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8).ToString("F0"));
            //        ////TaskDialog.Show("tt", ((int)((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8)).ToString());
            //        //TaskDialog.Show("tt", item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString());
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //    tx.Commit();
            //}
            ////例程结束
            //////1014 补充沟体替换
            //CircleGaugePlaceView circleGaugePlaceView = new CircleGaugePlaceView(uiApp);
            //circleGaugePlaceView.Show();

            ////0425 参照平面切割测试
            //// 检查当前视图是否为平面、立面或剖面
            //if (!(doc.ActiveView.ViewType is ViewType.FloorPlan || doc.ActiveView.ViewType is ViewType.Section || doc.ActiveView.ViewType is ViewType.Elevation))
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
            //// 获取选中的参照平面元素
            //ReferencePlane selectedRefPlane = doc.GetElement(refPlaneRef) as ReferencePlane;
            //if (selectedRefPlane == null)
            //{
            //    message = "未选择有效的参照平面。";
            //    return Result.Failed;
            //}
            //// 获取参照平面的几何信息
            //Plane refPlane = selectedRefPlane.GetPlane();
            //XYZ planeOrigin = refPlane.Origin;
            //XYZ planeNormal = refPlane.Normal;
            //// 收集所有目标元素
            //List<Element> targetElements = new List<Element>();
            //// 楼板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Floor)).WhereElementIsNotElementType().ToList());
            //// 天花板
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(Ceiling)).WhereElementIsNotElementType().ToList());
            //// 迹线屋面（通过参数筛选）
            //targetElements.AddRange(new FilteredElementCollector(doc, activeView.Id)
            //    .OfClass(typeof(RoofBase)).WhereElementIsNotElementType().Cast<RoofBase>()
            //    .Where(r =>
            //    {
            //        return r is FootPrintRoof;
            //        //// 迹线屋面有 Footprint 草图，拉伸屋面没有
            //    }).Cast<Element>().ToList());
            //// 存储与参照平面相交且正交的楼板信息
            //List<KeyValuePair<ElementId, string>> intersectingFloors = new List<KeyValuePair<ElementId, string>>();
            //List<ElementId> intersectingFloorIds = new List<ElementId>();
            //foreach (Element floor in targetElements)
            //{
            //    // 获取楼板的边界框（快速筛选）
            //    BoundingBoxXYZ floorBbox = floor.get_BoundingBox(activeView);
            //    if (floorBbox == null) continue;
            //    // 快速检测：检查楼板的边界框是否与参照平面相交（可选，提高性能）
            //    bool bboxIntersects = CheckBoundingBoxIntersectsPlane(floorBbox, refPlane);
            //    if (!bboxIntersects) continue;
            //    // 获取楼板的几何信息进行精确检测
            //    Options geoOptions = new Options();
            //    geoOptions.ComputeReferences = true;
            //    geoOptions.DetailLevel = ViewDetailLevel.Fine;
            //    GeometryElement geoElement = floor.get_Geometry(geoOptions);
            //    if (geoElement == null) continue;
            //    bool isIntersectingAndOrthogonal = false;
            //    // 遍历楼板的几何实体进行精确相交和正交检测
            //    foreach (GeometryObject geoObj in geoElement)
            //    {
            //        Solid solid = geoObj as Solid;
            //        if (solid != null && solid.Faces.Size > 0)
            //        {
            //            // 检查实体是否与平面相交
            //            if (IsSolidIntersectPlane(solid, refPlane))
            //            {
            //                // 进一步检查是否有面的法向量与参照平面正交
            //                foreach (Face face in solid.Faces)
            //                {
            //                    XYZ faceNormal = face.ComputeNormal(UV.Zero);
            //                    if (faceNormal != null)
            //                    {
            //                        double dotProduct = Math.Abs(faceNormal.DotProduct(planeNormal));
            //                        if (dotProduct < 1e-6) // 正交检查
            //                        {
            //                            isIntersectingAndOrthogonal = true;
            //                            break;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
            //        {
            //            // 检查网格是否与平面相正交
            //            if (IsMeshIntersectPlane(mesh, refPlane))
            //            {
            //                for (int i = 0; i < mesh.NumTriangles; i++)
            //                {
            //                    MeshTriangle triangle = mesh.get_Triangle(i);
            //                    // 正确计算三角形法向量（叉积）
            //                    XYZ v0 = triangle.get_Vertex(0);
            //                    XYZ v1 = triangle.get_Vertex(1);
            //                    XYZ v2 = triangle.get_Vertex(2);
            //                    XYZ edge1 = v1 - v0;
            //                    XYZ edge2 = v2 - v0;
            //                    XYZ triangleNormal = edge1.CrossProduct(edge2).Normalize();
            //                    // 判断三角形是否与平面正交（三角形法向量平行于参考平面）
            //                    // 即三角形法向量与平面法向量垂直（点积接近0）
            //                    double dotProduct = Math.Abs(triangleNormal.DotProduct(planeNormal));
            //                    // dotProduct ≈ 0 表示三角形法向量 ⊥ 平面法向量
            //                    // 即三角形平面 ∥ 参考平面（三角形与参考平面正交/垂直）
            //                    if (dotProduct < 1e-3) // 使用稍大的容差
            //                    {
            //                        isIntersectingAndOrthogonal = true;
            //                        break; // 跳出三角形循环
            //                    }
            //                }
            //                // 关键：如果已找到正交三角形，跳出外层 mesh 循环
            //                if (isIntersectingAndOrthogonal)
            //                    break;
            //            }
            //            ////普通相交简化如下
            //            //if (IsMeshIntersectPlane(mesh, refPlane))
            //            //{
            //            //    isIntersectingAndOrthogonal = true;
            //            //    break; // 假设 mesh 相交即视为正交（根据业务需求）
            //            //}
            //        }
            //        if (isIntersectingAndOrthogonal) break;
            //    }
            //    if (isIntersectingAndOrthogonal)
            //    {
            //        intersectingFloorIds.Add(floor.Id);
            //        // 获取楼板信息用于显示
            //        Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            //        string levelName = levelParam != null ? levelParam.AsValueString() : "未知";
            //        string floorTypeName = floor.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString();
            //        string floorInfo = $"楼板 ID:{floor.Id.IntegerValue}, 类型:{floorTypeName}, 标高:{levelName}";
            //        intersectingFloors.Add(new KeyValuePair<ElementId, string>(floor.Id, floorInfo));
            //    }
            //}
            ////// 输出结果
            //int intersectingCount = intersectingFloors.Count;
            //message = $"共找到 {intersectingCount} 个与参照平面相交且正交的平面元素。";
            ////if (intersectingCount > 0)
            ////{
            ////    uiDoc.Selection.SetElementIds(intersectingFloorIds);
            ////}
            ////TaskDialog.Show("tt", message);
            ////// 在找到相交且正交的板之后，创建构造线
            //if (!(intersectingCount > 0 || intersectingFloorIds.Count > 0)) return Result.Cancelled;
            //// 开始一个事务来创建构造线
            //using (Transaction trans = new Transaction(doc, "创建相交构造线"))
            //{
            //    trans.Start();
            //    List<Curve> allIntersectionCurves = new List<Curve>();
            //    foreach (ElementId elementId in intersectingFloorIds)
            //    {
            //        Element element = doc.GetElement(elementId);
            //        if (element == null) continue;
            //        // 重新获取该元素的几何信息
            //        Options geoOptions = new Options();
            //        geoOptions.ComputeReferences = true;
            //        geoOptions.DetailLevel = ViewDetailLevel.Fine;
            //        GeometryElement geoElement = element.get_Geometry(geoOptions);
            //        if (geoElement == null) continue;
            //        // 获取该元素与参照平面的所有交线
            //        List<Curve> intersectionCurves = GetIntersectionCurvesWithPlane(geoElement, refPlane);
            //        allIntersectionCurves.AddRange(intersectionCurves);
            //        message += intersectionCurves.Count().ToString();
            //    }
            //    //// 创建构造线（使用模型线或详图线）
            //    if (allIntersectionCurves.Count > 0)
            //    {
            //        // 选择创建方式：在平面视图中使用详图线，在3D视图中使用模型线
            //        bool useDetailLines = (activeView.ViewType == ViewType.FloorPlan ||
            //                               activeView.ViewType == ViewType.CeilingPlan ||
            //                               activeView.ViewType == ViewType.Section ||
            //                               activeView.ViewType == ViewType.Elevation);
            //        //if (useDetailLines)
            //        //{
            //        //    // 在视图中创建详图线（仅在该视图中可见）
            //        //    foreach (Curve curve in allIntersectionCurves)
            //        //    {
            //        //        // 将曲线投影到视图平面（如果需要）
            //        //        Curve projectedCurve = ProjectCurveToViewPlane(curve, activeView);
            //        //        if (projectedCurve != null)
            //        //        {
            //        //            //TaskDialog.Show("tt", (projectedCurve.Length * 304.8).ToString());
            //        //            //// 创建详图线
            //        //            DetailLine detailLine = doc.Create.NewDetailCurve(activeView, projectedCurve) as DetailLine;
            //        //            if (detailLine != null)
            //        //            {
            //        //                // 设置线型样式（可选）
            //        //                // 注意：需要先获取或创建线型样式
            //        //                SetLineStyle(detailLine, "Dash");
            //        //            }
            //        //        }
            //        //    }
            //        //    message += $"\n已创建 {allIntersectionCurves.Count} 条详图线。";
            //        //}
            //        //else
            //        //{
            //        //// 创建模型线（在所有视图中可见）
            //        //// 需要选择一个工作平面
            //        SketchPlane sketchPlane = SketchPlane.Create(doc, refPlane);
            //        foreach (Curve curve in allIntersectionCurves)
            //        {
            //            ModelCurve modelCurve = doc.Create.NewModelCurve(curve, sketchPlane);
            //            if (modelCurve != null)
            //            {
            //                // 设置线型样式（可选）
            //                SetLineStyle(modelCurve, "Dash");
            //            }
            //        }
            //        message += $"\n已创建 {allIntersectionCurves.Count} 条模型线。";
            //        //}
            //    }
            //    else
            //    {
            //        message += "\n未找到有效的交线。";
            //    }
            //    trans.Commit();
            //}
            //TaskDialog.Show("执行结果", message);
            ////////1003 SplitElementsCommand 变形缝、后浇带打断板、梁 遗留考虑问题较多，板边界，连线方向等等
            
            ////0421 构件分析测试 要排除固定的MEP相关配置项和系统材质、视图等，只管理手动添加的元素
            //var analyzer = new ModelProfessionAnalyzer(doc);
            //string report = analyzer.GetDetailedReport();
            //TaskDialog.Show("分析结果", report);

            ////var fitting = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_DuctFitting).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().FirstOrDefault();
            ////Autodesk.Revit.ApplicationServices.Application app = uiApp.Application;
            //Document familyDoc;
            //if (uiApp.ActiveUIDocument?.Document.IsFamilyDocument != true) return Result.Cancelled;
            //familyDoc = doc;
            //var type =familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsValueString();
            //TaskDialog.Show("tt", type.ToString());        

            //////找出所有有几何instance并分类
            //List<Element> allInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Cast<Element>().ToList();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var item in allInstances)
            //{
            //    if (item.HasPhases())
            //    {
            //        ids.Add(item.Id);
            //    }
            //}
            //uiDoc.Selection.SetElementIds(ids);

            //////0417查找全部实例共同文字属性.OK
            //var list = GetCommonStringParameterNames(doc);
            //// 筛选仅实例参数
            //var instanceOnly = list.Where(x => x.Value == "实例").Select(x => x.Key);
            //// 筛选仅类型参数
            //var symbolOnly = list.Where(x => x.Value == "类型").Select(x => x.Key);
            //var symbolBoth = list.Where(x => x.Value == "两者").Select(x => x.Key);
            //StringBuilder stringBuilderInstance = new StringBuilder();
            //StringBuilder stringBuilderSymbol = new StringBuilder();
            //StringBuilder stringBuilderBoth = new StringBuilder();
            //foreach (var item in instanceOnly)
            //{
            //    stringBuilderInstance.AppendLine(item);
            //}
            //foreach (var item in symbolOnly)
            //{
            //    stringBuilderSymbol.AppendLine(item);
            //}
            //foreach (var item in symbolBoth)
            //{
            //    stringBuilderBoth.AppendLine(item);
            //}
            //TaskDialog.Show("tt", "实例属性：" + stringBuilderInstance.ToString() + "\n" + "类型属性：" + stringBuilderSymbol.ToString() + "\n" + "（待复核）共同属性：" + stringBuilderBoth.ToString());

            //////0331 批量改族类型方法，考虑封装一个类型转化方法
            //try
            //{
            //    // 选择文件
            //    var openFileDialog = new Microsoft.Win32.OpenFileDialog
            //    {
            //        Title = "选择要修改族类别的 RFA 文件",
            //        Filter = "Revit Family Files (*.rfa)|*.rfa",
            //        Multiselect = true
            //    };
            //    if (openFileDialog.ShowDialog() != true) return Result.Cancelled;
            //    string[] selectedFiles = openFileDialog.FileNames;
            //    if (selectedFiles.Length == 0)
            //    {
            //        TaskDialog.Show("提示", "未选择任何文件");
            //        return Result.Cancelled;
            //    }
            //    // 确认操作
            //    var result = TaskDialog.Show(
            //        "确认操作",
            //        $"即将修改 {selectedFiles.Length} 个族文件的类别为\"电气装置\"。\n\n是否继续？",
            //        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            //    if (result != TaskDialogResult.Yes) return Result.Cancelled;
            //    // 处理文件
            //    var successFiles = new List<string>();
            //    var failedFiles = new List<string>();
            //    var skippedFiles = new List<string>();
            //    foreach (string filePath in selectedFiles)
            //    {
            //        string resultMessage;
            //        if (ProcessFamilyFile(uiApp, filePath, out resultMessage))
            //        {
            //            successFiles.Add(Path.GetFileName(filePath));
            //        }
            //        else if (resultMessage.Contains("已是"))
            //        {
            //            skippedFiles.Add(Path.GetFileName(filePath));
            //        }
            //        else
            //        {
            //            failedFiles.Add($"{Path.GetFileName(filePath)} - {resultMessage}");
            //        }
            //    }
            //    ShowResult(successFiles, failedFiles, skippedFiles);
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////0329 选择偏移量（Offset）设置过大构件，逻辑还不够清晰，误差太大
            //// 1. 获取并排序所有标高
            //List<Level> sortedLevels = new FilteredElementCollector(doc).OfClass(typeof(Level))
            //    .Cast<Level>().OrderBy(l => l.Elevation).ToList();
            //if (sortedLevels.Count < 1) return Result.Cancelled;
            //// 2. 收集需要检查的构件（排除链接和类型）
            //List<Element> allElements = new FilteredElementCollector(doc, doc.ActiveView.Id).WhereElementIsNotElementType()
            //    .Where(e => e.Category != null && e.LevelId != ElementId.InvalidElementId).ToList();
            //List<ElementId> anomalyIds = new List<ElementId>();
            //foreach (Element el in allElements)
            //{
            //    // 获取构件关联的标高
            //    Level currentLevel = sortedLevels.FirstOrDefault(l => l.Id == el.LevelId);
            //    if (currentLevel == null) continue;
            //    // 获取当前层的层间高差（寻找上一层）
            //    int currentIndex = sortedLevels.IndexOf(currentLevel);
            //    double heightToNextLevel = double.MaxValue;
            //    double heightToBelowLevel = double.MaxValue;
            //    if (currentIndex < sortedLevels.Count - 1)
            //    {
            //        // 有上一层，计算差值
            //        heightToNextLevel = sortedLevels[currentIndex + 1].Elevation - currentLevel.Elevation;
            //    }
            //    else
            //    {
            //        // 顶层，设定一个经验阈值（如 5米）
            //        heightToNextLevel = 5000 / 304.8;
            //    }
            //    if (currentIndex > 0)
            //    {
            //        // 有下一层
            //        heightToBelowLevel = currentLevel.Elevation - sortedLevels[currentIndex - 1].Elevation;
            //    }
            //    // 3. 提取偏移量参数 (尝试常见的内置参数)
            //    double offset = GetElementOffset(el);
            //    // 4. 判定异常：偏移值大于层高，或者负偏移超过下层高度
            //    if (offset > heightToNextLevel || (offset < 0 && Math.Abs(offset) > heightToBelowLevel))
            //    {
            //        anomalyIds.Add(el.Id);
            //    }
            //}
            //// 5. 反馈结果并选中
            //if (anomalyIds.Any())
            //{
            //    uiDoc.Selection.SetElementIds(anomalyIds);
            //    TaskDialog.Show("检测结果", $"发现 {anomalyIds.Count} 个标高异常构件（偏移量超出层高范围）。已在视图中选中这些构件。");
            //}
            //else
            //{
            //    TaskDialog.Show("检测结果", "未发现明显的标高偏移异常构件。");
            //}

            ////0222 批量改标高1500.OK 待深化
            //var selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds.Count > 1)
            //{
            //    return Result.Cancelled;
            //}
            //try
            //{
            //    if (selectedIds.Count == 0)
            //    {
            //        Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择一根水平管道");
            //        Pipe pipe1 = doc.GetElement(ref1) as Pipe;
            //        Parameter sysParam = pipe1.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            //        if (sysParam == null || !sysParam.HasValue)
            //        {
            //            TaskDialog.Show("提示", "未获取到系统参数");
            //            return Result.Cancelled;
            //        }
            //        using (Transaction tx = new Transaction(doc, "更改标高"))
            //        {
            //            tx.Start();
            //            sysParam.Set(1500 / 304.8);
            //            tx.Commit();
            //        }
            //    }
            //    else
            //    {
            //        Pipe pipe = doc.GetElement(selectedIds.FirstOrDefault()) as Pipe;
            //        Parameter sysParam = pipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            //        if (sysParam == null || !sysParam.HasValue)
            //        {
            //            TaskDialog.Show("提示", "未获取到系统参数");
            //            return Result.Cancelled;
            //        }
            //        using (Transaction tx = new Transaction(doc, "更改标高"))
            //        {
            //            tx.Start();
            //            sysParam.Set(1500 / 304.8);
            //            tx.Commit();
            //        }
            //    }
            //}
            //catch (Exception)
            //{
            //    throw;
            //}

            ////0206 重新连接天圆地方 还是没成功，只能手工替换天圆地方
            //var selIds = uiDoc.Selection.GetElementIds();
            //if (selIds == null || selIds.Count == 0)
            //{
            //    message = "请先在选集中选择风管管件（Duct Fitting）。";
            //    return Result.Failed;
            //}
            //var logs = new List<string>();
            //int ok = 0, skipped = 0, failed = 0;
            //using (var tx = new Transaction(doc, "删除风管管件并重连两端连接器"))
            //{
            //    tx.Start();
            //    foreach (var id in selIds)
            //    {
            //        Element e = doc.GetElement(id);
            //        if (e == null) continue;
            //        // 仅处理风管管件
            //        if (!(e is FamilyInstance fi) ||
            //            fi.Category == null ||
            //            fi.Category.Id.IntegerValue != (int)BuiltInCategory.OST_DuctFitting)
            //        {
            //            continue;
            //        }
            //        using (var st = new SubTransaction(doc))
            //        {
            //            st.Start();
            //            try
            //            {
            //                // 1) 获取该管件两端连接到的“外部连接器”（风管/附件/设备等的连接器）
            //                //    必须在删除管件前取到，因为删除后引用关系会改变
            //                var endConnectors = GetTwoEndNeighborConnectors(fi);
            //                if (endConnectors == null || endConnectors.Count != 2)
            //                {
            //                    skipped++;
            //                    st.RollBack(); // 本次子事务不做任何改变，回滚/不提交都可以；这里统一回滚
            //                    continue;
            //                }
            //                Connector c1 = endConnectors[0];
            //                Connector c2 = endConnectors[1];
            //                // 记录“连接器id”：Revit 的 Connector.Id 是 int（同一 Owner 内唯一）
            //                // 为便于唯一定位，记录 OwnerId + ConnectorId
            //                string c1Key = $"{c1.Owner.Id.IntegerValue}:{c1.Id}";
            //                string c2Key = $"{c2.Owner.Id.IntegerValue}:{c2.Id}";
            //                logs.Add($"Fitting {fi.Id.IntegerValue} -> C1 {c1Key} ({SafeCatName(c1.Owner)}) , C2 {c2Key} ({SafeCatName(c2.Owner)})");
            //                // 2) 删除管件
            //                doc.Delete(fi.Id);
            //                doc.Regenerate();
            //                // 尝试强制生成新管件
            //                bool created = TryCreateFitting(c1, c2, doc);
            //                // 若失败，尝试反向
            //                if (!created)
            //                    created = TryCreateFitting(c2, c1, doc);
            //                if (!created)
            //                    throw new InvalidOperationException("无法生成新的风管管件");
            //                st.Commit();
            //                ok++;
            //            }
            //            catch (Exception ex)
            //            {
            //                failed++;
            //                logs.Add($"Fitting {fi.Id.IntegerValue} FAILED: {ex.GetType().Name} - {ex.Message}");
            //                st.RollBack();
            //            }
            //        }
            //    }
            //    tx.Commit();
            //}
            //TaskDialog.Show("结果",$"成功: {ok}\n跳过(非两端或无法解析): {skipped}\n失败并回滚: {failed}\n" + string.Join("\n", logs.Take(20)) + (logs.Count > 20 ? "\n..." : ""));     

            ////0205 查找特定属性风口构建
            //List<FamilyInstance> allInstance = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            //List<FamilyInstance> terminalNames = new List<FamilyInstance>();
            //foreach (var item in allInstance)
            //{
            //    if ((item.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal) && (item.Name == "风道末端_单层百叶风口"))
            //    {
            //        terminalNames.Add(item);
            //    }
            //}
            //List<ElementId> selectedElementIds = new List<ElementId>();
            ////foreach (var item in terminalNames)
            ////{
            ////    try
            ////    {
            ////        Parameter widthParameter = item.LookupParameter("风口宽度");
            ////        Parameter heightParameter = item.LookupParameter("风口高度");
            ////        //if (widthParameter != null && widthParameter.AsDouble() == 600 / 304.8 && heightParameter != null && heightParameter.AsDouble() == 500 / 304.8)
            ////        if (widthParameter != null && widthParameter.AsDouble() == 1000 / 304.8)
            ////        //if (heightParameter != null && heightParameter.AsDouble() == 600 / 304.8)
            ////        {
            ////            selectedElementIds.Add(item.Id);
            ////        }
            ////    }
            ////    catch (Exception)
            ////    {
            ////        throw;
            ////    }
            ////}
            ////TaskDialog.Show("tt", selectedElementIds.Count().ToString());
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in terminalNames)
            //{
            //    selectedElementIds.Add(item.Id);
            //    stringBuilder.Append(item.Id.ToString() + ",");
            //}
            //TaskDialog.Show("tt", stringBuilder.ToString());
            ////uiDoc.Selection.SetElementIds(selectedElementIds);
            return Result.Succeeded;
        }
        //private int _maximum;
        //public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
        //0323 进度条调用模板，无需单独声明ProgressBar
        //    TransactionWithProgressBarHelper.Execute(doc, "提取构件信息", (service) =>
        //    {
        //        service.UpdateMax(sortedIds.Count());
        //        int index = 0;
        //        foreach (var id in sortedIds)
        //        {
        //            service.Update(++index, id.Value.ToString());
        //        }
        //    });
        //    
        //0425 检查相交并画线
        /// <summary>
        /// 检查边界框是否与平面相交
        /// </summary>
        private bool CheckBoundingBoxIntersectsPlane(BoundingBoxXYZ bbox, Plane plane)
        {
            if (bbox == null || plane == null) return false;
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            // 获取边界框的8个角点
            List<XYZ> corners = new List<XYZ>    {
                new XYZ(min.X, min.Y, min.Z),        new XYZ(max.X, min.Y, min.Z),
                new XYZ(min.X, max.Y, min.Z),        new XYZ(max.X, max.Y, min.Z),
                new XYZ(min.X, min.Y, max.Z),        new XYZ(max.X, min.Y, max.Z),
                new XYZ(min.X, max.Y, max.Z),        new XYZ(max.X, max.Y, max.Z)    };
            // 计算平面方程: ax + by + cz + d = 0
            // 平面法向量 (a, b, c)
            XYZ normal = plane.Normal;
            // 平面上的点
            XYZ origin = plane.Origin;
            // 计算 d = -(a*x0 + b*y0 + c*z0)
            double d = -(normal.X * origin.X + normal.Y * origin.Y + normal.Z * origin.Z);
            // 检查角点是否在平面两侧
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (XYZ point in corners)
            {
                // 计算有符号距离: (a*x + b*y + c*z + d) / sqrt(a^2 + b^2 + c^2)
                // 或者简化为 (a*x + b*y + c*z + d)，因为只需要判断符号
                double signedDistance = normal.X * point.X + normal.Y * point.Y + normal.Z * point.Z + d;
                // 点在平面上（距离接近0）
                if (Math.Abs(signedDistance) < 1e-6) return true;
                if (signedDistance > 0) hasPositive = true;
                else hasNegative = true;
                // 平面穿过边界框（点在两侧）
                if (hasPositive && hasNegative) return true;
            }
            // 所有点在同一侧，不相交
            return false;
        }
        /// <summary>
        /// 检查实体是否与平面相交
        /// </summary>
        private bool IsSolidIntersectPlane(Solid solid, Plane plane)
        {
            if (solid == null || solid.Faces.Size == 0 || plane == null) return false;
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            // 收集所有顶点并检查有符号距离
            List<double> distances = new List<double>();
            // 从边获取顶点
            foreach (Edge edge in solid.Edges)
            {
                Curve curve = edge.AsCurve();
                distances.Add(SignedDistanceTo(curve.GetEndPoint(0), normal, origin));
                distances.Add(SignedDistanceTo(curve.GetEndPoint(1), normal, origin));
            }
            // 从三角化面获取顶点（更密集）
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                Mesh mesh = face.Triangulate();
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    MeshTriangle triangle = mesh.get_Triangle(i);
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(0), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(1), normal, origin));
                    distances.Add(SignedDistanceTo(triangle.get_Vertex(2), normal, origin));
                }
            }
            // 检查距离分布
            bool hasPositive = false;
            bool hasNegative = false;
            foreach (double d in distances)
            {
                if (Math.Abs(d) < 1e-6) return true;
                if (d > 0) hasPositive = true;
                else hasNegative = true;
                if (hasPositive && hasNegative) return true;
            }
            return false;
        }
        /// <summary>
        /// 检查网格是否与平面相交
        /// </summary>
        private bool IsMeshIntersectPlane(Mesh mesh, Plane plane)
        {
            if (mesh == null || mesh.NumTriangles == 0 || plane == null) return false;
            // 预计算平面参数，避免重复计算
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用点积）
                double d0 = SignedDistanceTo(v0, normal, origin);
                double d1 = SignedDistanceTo(v1, normal, origin);
                double d2 = SignedDistanceTo(v2, normal, origin);
                // 检查是否相交：有正有负或等于0（点在平面上）
                bool hasZero = Math.Abs(d0) < 1e-6 || Math.Abs(d1) < 1e-6 || Math.Abs(d2) < 1e-6;
                if (hasZero) return true;
                bool hasPositive = d0 > 0 || d1 > 0 || d2 > 0;
                bool hasNegative = d0 < 0 || d1 < 0 || d2 < 0;
                // 平面穿过三角形（点在两侧）
                if (hasPositive && hasNegative)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算点到平面的有符号距离（内联辅助方法，避免重复代码）
        /// </summary>
        private double SignedDistanceTo(XYZ point, XYZ planeNormal, XYZ planeOrigin)
        {
            XYZ toPoint = point - planeOrigin;
            return toPoint.DotProduct(planeNormal);
        }
        /// <summary>
        /// 获取几何元素与平面的所有交线
        /// </summary>
        private List<Curve> GetIntersectionCurvesWithPlane(GeometryElement geoElement, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (geoElement == null) return intersectionCurves;
            foreach (GeometryObject geoObj in geoElement)
            {
                Solid solid = geoObj as Solid;
                if (solid != null && solid.Faces.Size > 0)
                {
                    //获取实体与平面的交线
                    List<Curve> solidIntersections = GetSolidIntersectionCurvesWithPlane(solid, plane);
                    intersectionCurves.AddRange(solidIntersections);

                }
                else if (geoObj is Mesh mesh && mesh.NumTriangles > 0)
                {
                    //获取网格与平面的交线
                    List<Curve> meshIntersections = GetMeshIntersectionCurvesWithPlane(mesh, plane);
                    intersectionCurves.AddRange(meshIntersections);
                }
            }
            // 合并连接的曲线
            return intersectionCurves;
            //return MergeConnectedCurves(intersectionCurves);
        }
        /// <summary>
        /// 获取实体与平面的交线（最简可靠版）byKIMI
        /// </summary>
        private List<Curve> GetSolidIntersectionCurvesWithPlane(Solid solid, Plane plane)
        {
            var result = new List<Curve>();
            if (solid == null) return result;
            XYZ n = plane.Normal;
            XYZ o = plane.Origin;
            // 收集所有有效交点
            var pointSet = new HashSet<string>(); // 用于去重
            var allPoints = new List<XYZ>();
            Action<XYZ> addPoint = (p) =>
            {
                string key = $"{Math.Round(p.X, 6)},{Math.Round(p.Y, 6)},{Math.Round(p.Z, 6)}";
                if (!pointSet.Contains(key))
                {
                    pointSet.Add(key);
                    allPoints.Add(p);
                }
            };
            foreach (Edge edge in solid.Edges)
            {
                var c = edge.AsCurve();
                XYZ p0 = c.GetEndPoint(0);
                XYZ p1 = c.GetEndPoint(1);
                double d0 = (p0 - o).DotProduct(n);
                double d1 = (p1 - o).DotProduct(n);
                if (Math.Abs(d0) < 1e-6) addPoint(p0);
                if (Math.Abs(d1) < 1e-6) addPoint(p1);
                if (d0 * d1 < -1e-12)
                {
                    double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
                    addPoint(p0 + (p1 - p0) * t);
                }
            }
            if (allPoints.Count < 2) return result;
            // 在平面内排序并连接
            XYZ u = plane.XVec.Normalize();
            XYZ v = plane.YVec.Normalize();
            allPoints = allPoints.OrderBy(p => (p - o).DotProduct(u))
                .ThenBy(p => (p - o).DotProduct(v)).ToList();
            // 连接相邻点形成交线
            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                double dist = allPoints[i].DistanceTo(allPoints[i + 1]);
                if (dist > 1e-6 && dist < solid.GetBoundingBox().Max.DistanceTo(solid.GetBoundingBox().Min))
                {
                    result.Add(Line.CreateBound(allPoints[i], allPoints[i + 1]));
                }
            }
            return result;
        }
        /// <summary>
        /// 在平面坐标系下排序点
        /// </summary>
        private List<XYZ> SortPointsOnPlane(List<XYZ> points, Plane plane)
        {
            XYZ xAxis = plane.XVec.Normalize();
            XYZ yAxis = plane.YVec.Normalize();
            return points
                .Select(p => new
                {
                    Point = p,
                    U = (p - plane.Origin).DotProduct(xAxis),
                    V = (p - plane.Origin).DotProduct(yAxis)
                }).OrderBy(item => item.U)
                .ThenBy(item => item.V).Select(item => item.Point).ToList();
        }
        /// <summary>
        /// 获取网格与平面的交线
        /// </summary>
        private List<Curve> GetMeshIntersectionCurvesWithPlane(Mesh mesh, Plane plane)
        {
            List<Curve> intersectionCurves = new List<Curve>();
            if (mesh == null || mesh.NumTriangles == 0) return intersectionCurves;
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                // 获取三角形的三个顶点
                XYZ v0 = triangle.get_Vertex(0);
                XYZ v1 = triangle.get_Vertex(1);
                XYZ v2 = triangle.get_Vertex(2);
                // 计算三个顶点到平面的有符号距离（使用自定义方法）
                double d0 = SignedDistanceTo(v0, planeNormal, planeOrigin);
                double d1 = SignedDistanceTo(v1, planeNormal, planeOrigin);
                double d2 = SignedDistanceTo(v2, planeNormal, planeOrigin);
                // 检查三角形是否与平面相交
                List<XYZ> intersectionPoints = new List<XYZ>();
                // 检查每条边与平面的交点
                AddIntersectionPoint(v0, v1, d0, d1, plane, intersectionPoints);
                AddIntersectionPoint(v1, v2, d1, d2, plane, intersectionPoints);
                AddIntersectionPoint(v2, v0, d2, d0, plane, intersectionPoints);
                // 如果有两个交点，创建线段
                if (intersectionPoints.Count == 2)
                {
                    Line line = Line.CreateBound(intersectionPoints[0], intersectionPoints[1]);
                    intersectionCurves.Add(line);
                }
            }
            return intersectionCurves;
        }
        /// <summary>
        /// 添加边的交点
        /// </summary>
        private void AddIntersectionPoint(XYZ p1, XYZ p2, double d1, double d2, Plane plane, List<XYZ> points)
        {
            if (Math.Abs(d1) < 1e-9)
            {
                points.Add(p1);
            }
            else if (Math.Abs(d2) < 1e-9)
            {
                points.Add(p2);
            }
            else if (d1 * d2 < 0) // 点在平面两侧
            {
                double t = -d1 / (d2 - d1); // 插值参数
                XYZ intersection = p1 + t * (p2 - p1);
                points.Add(intersection);
            }
        }
        /// <summary>
        /// 获取直线与平面的交线段（仅支持Line）
        /// </summary>
        private List<Curve> GetLinePlaneSegments(Line line, Plane plane)
        {
            List<Curve> segments = new List<Curve>();
            if (line == null || plane == null) return segments;
            XYZ start = line.GetEndPoint(0);
            XYZ end = line.GetEndPoint(1);
            double d0 = (start - plane.Origin).DotProduct(plane.Normal);
            double d1 = (end - plane.Origin).DotProduct(plane.Normal);
            const double eps = 1e-6;
            // 完全在平面内
            if (Math.Abs(d0) < eps && Math.Abs(d1) < eps)
            {
                segments.Add(line.Clone());
                return segments;
            }
            // 无交点（同侧且不接触）
            if (d0 > eps && d1 > eps) return segments;
            if (d0 < -eps && d1 < -eps) return segments;
            // 计算交点
            double t = Math.Abs(d0) / (Math.Abs(d0) + Math.Abs(d1));
            XYZ intersection = start + (end - start) * t;
            // 返回平面内的部分（根据有符号距离判断）
            if (d0 >= -eps && d1 >= -eps)
            {
                // 都在正侧或接触平面
                if (d0 < eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 < eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else if (d0 <= eps && d1 <= eps)
            {
                // 都在负侧或接触平面
                if (d0 > -eps) segments.Add(Line.CreateBound(start, intersection));
                else if (d1 > -eps) segments.Add(Line.CreateBound(intersection, end));
            }
            else
            {
                // 跨平面，返回两侧
                segments.Add(Line.CreateBound(start, intersection));
                segments.Add(Line.CreateBound(intersection, end));
            }
            return segments;
        }
        /// <summary>
        /// 合并连接的曲线
        /// </summary>
        private List<Curve> MergeConnectedCurves(List<Curve> curves)
        {
            if (curves.Count <= 1) return curves;
            List<Curve> mergedCurves = new List<Curve>();
            List<Curve> remaining = new List<Curve>(curves);
            while (remaining.Count > 0)
            {
                Curve current = remaining[0];
                remaining.RemoveAt(0);
                bool merged = true;
                while (merged && remaining.Count > 0)
                {
                    merged = false;
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        if (AreCurvesConnected(current, remaining[i]))
                        {
                            // 合并曲线
                            current = MergeTwoCurves(current, remaining[i]);
                            remaining.RemoveAt(i);
                            merged = true;
                            break;
                        }
                    }
                }
                mergedCurves.Add(current);
            }
            return mergedCurves;
        }
        /// <summary>
        /// 判断两条曲线是否连接
        /// </summary>
        private bool AreCurvesConnected(Curve curve1, Curve curve2, double tolerance = 1e-6)
        {
            XYZ end1 = curve1.GetEndPoint(1);
            XYZ start2 = curve2.GetEndPoint(0);
            return end1.DistanceTo(start2) < tolerance;
        }
        /// <summary>
        /// 合并两条曲线
        /// </summary>
        private Curve MergeTwoCurves(Curve curve1, Curve curve2)
        {
            // 简单实现：创建一条新的直线连接两个端点
            XYZ start = curve1.GetEndPoint(0);
            XYZ end = curve2.GetEndPoint(1);
            return Line.CreateBound(start, end);
        }
        /// <summary>
        /// 将曲线投影到当前视图平面,没使用此方法？？
        /// </summary>
        //private Curve ProjectCurveToViewPlane(Curve curve, View activeView)
        //{
        //    if (curve == null || activeView == null) return curve;
        //    // 获取视图平面（适用于平、立、剖面）
        //    Plane viewPlane = activeView.SketchPlane.GetPlane();
        //    if (viewPlane == null) return curve;
        //    List<XYZ> projectedPoints = new List<XYZ>();
        //    // 投影曲线的关键点
        //    IList<XYZ> points = curve.Tessellate();
        //    foreach (XYZ point in points)
        //    {
        //        XYZ projectedPoint = ProjectPointToPlane(point, viewPlane);
        //        projectedPoints.Add(projectedPoint);
        //    }
        //    if (projectedPoints.Count < 2) return curve;
        //    XYZ start = projectedPoints[0];
        //    XYZ end = projectedPoints[projectedPoints.Count - 1];
        //    // 检查投影后是否有有效长度
        //    double projectedLength = start.DistanceTo(end);
        //    if (projectedLength < 1e-6)
        //    {
        //        // 投影长度为0，曲线与视图平面垂直，返回空（防错）
        //        return null;
        //    }
        //    // 检查所有投影点是否基本重合（更严格的垂直判断）
        //    bool allSame = true;
        //    for (int i = 1; i < projectedPoints.Count; i++)
        //    {
        //        if (!projectedPoints[i].IsAlmostEqualTo(start))
        //        {
        //            allSame = false;
        //            break;
        //        }
        //    }
        //    if (allSame) return curve;
        //    // 根据原始曲线类型创建对应的投影曲线
        //    if (curve is Line) return Line.CreateBound(start, end);
        //    // 弧线投影后可能变为直线或保持弧线简化处理：返回直线段
        //    else if (curve is Arc arc) return Line.CreateBound(start, end);
        //    // 其他复杂曲线，返回首尾投影点连线
        //    else return Line.CreateBound(start, end);
        //}
        /// <summary>
        /// 将点投影到平面
        /// </summary>
        private XYZ ProjectPointToPlane(XYZ point, Plane plane)
        {
            XYZ planeOrigin = plane.Origin;
            XYZ planeNormal = plane.Normal;
            double distance = SignedDistanceTo(point, planeNormal, planeOrigin);
            return point - distance * plane.Normal;
        }
        /// <summary>
        /// 设置线条样式
        /// </summary>
        private void SetLineStyle(dynamic line, string styleName)
        {
            try
            {
                // 获取线型样式
                FilteredElementCollector collector = new FilteredElementCollector(line.Document);
                GraphicsStyle graphicsStyle = collector
                    .OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>()
                    .FirstOrDefault(g => g.Name == styleName);

                if (graphicsStyle != null)
                {
                    line.LineStyle = graphicsStyle;
                }
            }
            catch
            {
                // 如果设置失败，使用默认样式
            }
        }
        ////private bool IsPointOnCurve(Curve curve, XYZ point)
        ////{
        ////    return curve.Distance(point) < 1e-6; // 使用容差判断点是否在曲线上
        ////}
        //0425 体面相交逻辑方法结束
    }
    /// <summary>
    /// 专业统计结果类
    /// </summary>
    public class ProfessionStatistic
    {
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    /// <summary>
    /// 分析结果类
    /// </summary>
    public class ProfessionAnalysisResult
    {
        public int TotalElementCount { get; set; }
        public string PrimaryProfession { get; set; }
        public bool IsMultiDiscipline { get; set; }
        public Dictionary<BuiltInCategory, int> CategoryStatistics { get; set; }
        public Dictionary<string, ProfessionStatistic> ProfessionStatistics { get; set; }

        public ProfessionAnalysisResult()
        {
            CategoryStatistics = new Dictionary<BuiltInCategory, int>();
            ProfessionStatistics = new Dictionary<string, ProfessionStatistic>();
        }
    }
    /// <summary>
    /// 统计模型中各类别构件的数量，并分析模型的专业归属
    /// </summary>
    public class ModelProfessionAnalyzer
    {
        private readonly Document _doc;

        // 专业类别映射字典
        private readonly Dictionary<BuiltInCategory, string> _categoryToProfession;

        public ModelProfessionAnalyzer(Document doc)
        {
            _doc = doc;
            _categoryToProfession = InitializeCategoryMapping();
        }

        /// <summary>
        /// 初始化 BuiltInCategory 到专业的映射关系
        /// </summary>
        private Dictionary<BuiltInCategory, string> InitializeCategoryMapping()
        {
            return new Dictionary<BuiltInCategory, string>
        {
            // 建筑专业
            { BuiltInCategory.OST_Walls, "建筑" },
            { BuiltInCategory.OST_Doors, "建筑" },
            { BuiltInCategory.OST_Windows, "建筑" },
            { BuiltInCategory.OST_Rooms, "建筑" },
            { BuiltInCategory.OST_Floors, "建筑" },
            { BuiltInCategory.OST_Ceilings, "建筑" },
            { BuiltInCategory.OST_Stairs, "建筑" },
            { BuiltInCategory.OST_Ramps, "建筑" },
            { BuiltInCategory.OST_Railings, "建筑" },
            { BuiltInCategory.OST_CurtainWallMullions, "建筑" },
            { BuiltInCategory.OST_CurtainWallPanels, "建筑" },
            
            // 结构专业
            { BuiltInCategory.OST_StructuralColumns, "结构" },
            { BuiltInCategory.OST_StructuralFraming, "结构" },
            { BuiltInCategory.OST_StructuralFoundation, "结构" },
            { BuiltInCategory.OST_Rebar, "结构" },
            { BuiltInCategory.OST_Truss, "结构" },
            { BuiltInCategory.OST_StructuralBracePlanReps, "结构" },
            
            // 给排水专业
            { BuiltInCategory.OST_PipeCurves, "给排水" },
            { BuiltInCategory.OST_PipeFitting, "给排水" },
            { BuiltInCategory.OST_PipeAccessory, "给排水" },
            { BuiltInCategory.OST_PlumbingFixtures, "给排水" },
            { BuiltInCategory.OST_Sprinklers, "给排水" },
            
            // 暖通专业
            { BuiltInCategory.OST_DuctCurves, "暖通" },
            { BuiltInCategory.OST_DuctFitting, "暖通" },
            { BuiltInCategory.OST_DuctAccessory, "暖通" },
            { BuiltInCategory.OST_MechanicalEquipment, "暖通" },
            { BuiltInCategory.OST_DuctTerminal, "暖通" },
            { BuiltInCategory.OST_FlexDuctCurves, "暖通" },
            
            // 电气专业
            { BuiltInCategory.OST_Conduit, "电气" },
            { BuiltInCategory.OST_ConduitFitting, "电气" },
            { BuiltInCategory.OST_CableTray, "电气" },
            { BuiltInCategory.OST_CableTrayFitting, "电气" },
            { BuiltInCategory.OST_LightingFixtures, "电气" },
            { BuiltInCategory.OST_ElectricalEquipment, "电气" },
            { BuiltInCategory.OST_ElectricalFixtures, "电气" },
            { BuiltInCategory.OST_DataDevices, "电气" },
            { BuiltInCategory.OST_FireAlarmDevices, "电气" },
            { BuiltInCategory.OST_SecurityDevices, "电气" },
            { BuiltInCategory.OST_TelephoneDevices, "电气" },
            { BuiltInCategory.OST_Wire, "电气" },
            
            // 工艺专业
            { BuiltInCategory.OST_SpecialityEquipment, "工艺" },
            { BuiltInCategory.OST_GenericModel, "工艺" },
            { BuiltInCategory.OST_Entourage, "工艺" },
            
            // 其他通用类别（归入"其他"）
            { BuiltInCategory.OST_Levels, "其他" },
            { BuiltInCategory.OST_Grids, "其他" },
            { BuiltInCategory.OST_Views, "其他" },
            { BuiltInCategory.OST_Sheets, "其他" },
            { BuiltInCategory.OST_Materials, "其他" },
            { BuiltInCategory.OST_ElectricalLoadClassifications, "其他" },
            { BuiltInCategory.OST_ParamElemElectricalLoadClassification, "其他" },
            { BuiltInCategory.OST_HVAC_Load_Space_Types, "其他" },
            { BuiltInCategory.OST_PreviewLegendComponents, "其他" }
        };
        }

        /// <summary>
        /// 执行分析，返回各专业构件数量及占比
        /// </summary>
        public ProfessionAnalysisResult Analyze()
        {
            var result = new ProfessionAnalysisResult();

            // 获取所有实体元素（排除视图、图纸等非实体类别）
            var allElements = new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()  // 排除类型元素，只取实例
                .WhereElementIsViewIndependent() // 排除视图相关元素
                .ToElements();

            int totalCount = 0;
            var categoryCountMap = new Dictionary<BuiltInCategory, int>();
            var professionCountMap = new Dictionary<string, int>();

            // 初始化专业计数字典
            foreach (var profession in new[] { "建筑", "结构", "给排水", "暖通", "电气", "工艺", "其他" })
            {
                professionCountMap[profession] = 0;
            }

            foreach (var element in allElements)
            {
                // 获取元素的类别
                Category category = element.Category;
                if (category == null) continue;

                // 获取 BuiltInCategory 值
                BuiltInCategory bic = (BuiltInCategory)category.Id.IntegerValue;

                // 统计类别计数
                if (!categoryCountMap.ContainsKey(bic))
                    categoryCountMap[bic] = 0;
                categoryCountMap[bic]++;

                // 统计专业计数
                if (_categoryToProfession.TryGetValue(bic, out string profession))
                {
                    professionCountMap[profession]++;
                }
                else
                {
                    // 未映射的类别归入"其他"
                    professionCountMap["其他"]++;
                }

                totalCount++;
            }

            result.TotalElementCount = totalCount;
            result.CategoryStatistics = categoryCountMap;

            // 计算各专业占比
            foreach (var kvp in professionCountMap)
            {
                double percentage = totalCount > 0 ? (kvp.Value * 100.0 / totalCount) : 0;
                result.ProfessionStatistics.Add(kvp.Key, new ProfessionStatistic
                {
                    Count = kvp.Value,
                    Percentage = percentage
                });
            }

            // 确定模型的主要专业（占比最高的专业）
            result.PrimaryProfession = result.ProfessionStatistics
                .OrderByDescending(x => x.Value.Percentage)
                .First().Key;

            // 判断是否为综合模型（非主导专业占比超过15%）
            double topPercentage = result.ProfessionStatistics.Max(x => x.Value.Percentage);
            result.IsMultiDiscipline = topPercentage < 60;

            return result;
        }

        /// <summary>
        /// 获取详细的类别统计信息
        /// </summary>
        public string GetDetailedReport()
        {
            var result = Analyze();
            var report = new System.Text.StringBuilder();

            report.AppendLine("========== Revit 模型专业分析报告 ==========");
            report.AppendLine($"模型总构件数: {result.TotalElementCount}");
            report.AppendLine($"主要专业: {result.PrimaryProfession}");
            report.AppendLine($"是否综合模型: {(result.IsMultiDiscipline ? "是" : "否")}");
            report.AppendLine();
            report.AppendLine("各专业统计:");
            report.AppendLine("----------------------------------------");

            foreach (var stat in result.ProfessionStatistics.OrderByDescending(x => x.Value.Percentage))
            {
                report.AppendLine($"{stat.Key}: {stat.Value.Count} 个构件 ({stat.Value.Percentage:F2}%)");
            }

            report.AppendLine();
            report.AppendLine("主要类别明细 (Top 10):");
            report.AppendLine("----------------------------------------");

            var topCategories = result.CategoryStatistics
                .OrderByDescending(x => x.Value)
                .Take(10);

            foreach (var kvp in topCategories)
            {
                string categoryName = GetCategoryName(kvp.Key);
                report.AppendLine($"{categoryName}: {kvp.Value} 个");
            }

            return report.ToString();
        }

        /// <summary>
        /// 获取类别的显示名称
        /// </summary>
        private string GetCategoryName(BuiltInCategory bic)
        {
            try
            {
                Category category = Category.GetCategory(_doc, bic);
                return category?.Name ?? bic.ToString();
            }
            catch
            {
                return bic.ToString();
            }
        }
    }
}
