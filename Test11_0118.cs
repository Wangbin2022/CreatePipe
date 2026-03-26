using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Form.RevitStylePopup;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : IExternalCommand, INotifyPropertyChanged
    {

        //0118 新开规则，
        private bool IsHorizontal(Pipe pipe)
        {
            Line line = (pipe.Location as LocationCurve).Curve as Line;
            return Math.Abs(line.Direction.Z) < 0.001; // 允许微小误差
        }
        /// <summary>
        /// 获取指定位置的连接器
        /// </summary>
        private Connector GetConnectorAtPoint(Pipe pipe, XYZ point)
        {
            ConnectorManager cm = pipe.ConnectorManager;
            foreach (Connector c in cm.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(point))
                {
                    return c;
                }
            }
            return null;
        }
        /// <summary>
        /// 计算两条直线在XY平面上的投影交点
        /// </summary>
        private XYZ GetIntersectionPoint2D(Line line1, Line line2)
        {
            // 提取XY平面的坐标方程: P = Origin + t * Direction
            double x1 = line1.Origin.X;
            double y1 = line1.Origin.Y;
            double dx1 = line1.Direction.X;
            double dy1 = line1.Direction.Y;

            double x2 = line2.Origin.X;
            double y2 = line2.Origin.Y;
            double dx2 = line2.Direction.X;
            double dy2 = line2.Direction.Y;

            // 解线性方程组求解交点
            // x = x1 + t1*dx1
            // y = y1 + t1*dy1
            // x = x2 + t2*dx2
            // y = y2 + t2*dy2

            double det = dx1 * dy2 - dy1 * dx2;

            // 如果行列式为0，说明平行
            if (Math.Abs(det) < 0.00001) return null;

            double t1 = ((x2 - x1) * dy2 - (y2 - y1) * dx2) / det;

            // 计算交点坐标 (Z值这里先暂定为0，后续业务逻辑会覆盖)
            return new XYZ(x1 + t1 * dx1, y1 + t1 * dy1, 0);
        }
        #region
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
        #endregion
        private void CheckPipeSlope(Document document, List<ElementId> ids, double angle)
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
        private bool ProcessRVT(Document doc, string pathName)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathName);
            // 查找现有的 LinkType (而不是 Instance)，因为 Instance 删除后 Type 还在
            RevitLinkType existingType = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>().FirstOrDefault(l => l.Name.Contains(fileName));
            if (existingType != null) doc.Delete(existingType.Id);
            FilePath path = new FilePath(pathName);
            RevitLinkOptions options = new RevitLinkOptions(false);
            LinkLoadResult result = RevitLinkType.Create(doc, path, options);
            if (result.ElementId != ElementId.InvalidElementId)
            {
                RevitLinkInstance.Create(doc, result.ElementId);
                return true;
            }
            return false;
        }
        private bool ProcessDWG(Document doc, string pathName, View activeView)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathName);
            // 查找现有的 ImportInstance
            var existing = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().FirstOrDefault(i => i.IsLinked && i.Category.Name.Contains(fileName));
            if (existing != null) doc.Delete(existing.Id);
            DWGImportOptions options = new DWGImportOptions
            {
                Placement = ImportPlacement.Origin,
                OrientToView = true,
                Unit = ImportUnit.Millimeter
            };
            return doc.Link(pathName, options, activeView, out _);
        }
        private bool ProcessRFA(Document doc, string pathName)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathName);
            // 载入族（Revit 会自动处理同名族，LoadFamily 最后一个参数决定如何处理已存在的族）
            // 这里手动删除旧族以确保完全刷新（可选）
            Family existingFamily = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().FirstOrDefault(f => f.Name == fileName);
            if (existingFamily != null) doc.Delete(existingFamily.Id);
            return doc.LoadFamily(pathName);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


            ////0326 视图显隐测试——》引出viewmodel的释放问题-》批量链接功能卡死测试
            //CategoryVisibilityView categoryVisibilityView = new CategoryVisibilityView(uiApp);
            //categoryVisibilityView.ShowDialog();

            //////0325 链接图层信息测试
            //XrefCADLayerView xrefCADLayerView = new XrefCADLayerView(uiApp);
            //xrefCADLayerView.Show();
            //Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, new CadSelectionFilter(), "请选择一个 CAD 链接或导入文件");
            //ImportInstance cadInstance = doc.GetElement(pickedRef) as ImportInstance;
            //CategoryNameMap subCategories = cadInstance.Category.SubCategories;
            //List<CadLayerItem> layerList = new List<CadLayerItem>();
            //int layerCount = 0;
            //foreach (Category subCat in subCategories)
            //{
            //    // 检查该图层当前是否可以被隐藏
            //    if (activeView.CanCategoryBeHidden(subCat.Id))
            //    {
            //        layerCount++;
            //        //Color color = subCat.LineColor;
            //        //string rgb = color.IsValid ? $"({color.Red}, {color.Green}, {color.Blue})" : "无效颜色";
            //        //bool isHidden = activeView.GetCategoryHidden(subCat.Id);
            //        //layerList.Add(new CadLayerItem
            //        //{
            //        //    LayerName = subCat.Name,
            //        //    CategoryId = subCat.Id,
            //        //    LayerColor = color,
            //        //    IsVisible = !isHidden // 如果未被隐藏，则状态为可见(true)
            //        //});
            //    }
            //}
            //TaskDialog.Show("tt", layerCount.ToString());
            ////TaskDialog.Show("tt", layerList.FirstOrDefault().LayerColor.Red.ToString());

            //////////1111 显示所有隐藏的参照CAD图层
            ////string info = null;
            ////ICollection<ElementId> ids = ExternalFileUtils.GetAllExternalFileReferences(doc);
            ////IList<Subelement> subElements = null;
            ////StringBuilder stringBuilder = new StringBuilder();
            ////foreach (ElementId id in ids)
            ////{
            ////    Element elem = doc.GetElement(id);
            ////    if (elem.Category != null)
            ////        activeView.SetCategoryHidden(elem.Category.Id, false);
            ////    //    info += elem.Category.Name + "\n";
            ////    //foreach (Category item in elem.Category.SubCategories)
            ////    //{
            ////    //    stringBuilder.AppendLine(item.Name);
            ////    //} 
            ////}
            //////TaskDialog.Show("tt", stringBuilder.ToString());
            ////TaskDialog.Show("tt", info);
            //////TaskDialog.Show("tt", ids.Count.ToString());
            //////try
            //////{
            //////    //开始事务
            //////    using (Autodesk.Revit.DB.Transaction ts = new Autodesk.Revit.DB.Transaction(doc, "显示所有隐藏的参照CAD图层"))
            //////    {
            //////        ts.Start();
            //////        // 获取所有导入类别（CAD参照）
            //////        var importCategories = doc.Settings.Categories
            //////            .OfType<Category>()
            //////            .Where(cat => cat.CategoryType != CategoryType.Annotation)
            //////            .ToList();
            //////        StringBuilder stringBuilder = new StringBuilder();
            //////        //TaskDialog.Show("tt", importCategories.Count.ToString());
            //////        foreach (var category in importCategories)
            //////        {
            //////            stringBuilder.Append(category.Name);
            //////        }
            //////        TaskDialog.Show("tt", stringBuilder.ToString());
            //////        //int unhiddenCount = 0;
            //////        //// 遍历所有导入类别，显示被隐藏的图层
            //////        //foreach (Category category in importCategories)
            //////        //{
            //////        //    try
            //////        //    {
            //////        //        // 检查该类别在当前视图中是否被隐藏
            //////        //        if (activeView.GetCategoryHidden(category.Id))
            //////        //        {
            //////        //            // 显示该类别
            //////        //            activeView.SetCategoryHidden(category.Id, false);
            //////        //            unhiddenCount++;
            //////        //        }
            //////        //    }
            //////        //    catch (Exception ex)
            //////        //    {
            //////        //        // 忽略无法处理的类别，继续处理其他类别
            //////        //        continue;
            //////        //    }
            //////        //}
            //////        //ts.Commit();
            //////        //TaskDialog.Show("完成", $"已显示 {unhiddenCount} 个被隐藏的CAD参照图层");
            //////        return Result.Succeeded;
            //////    }
            //////}
            //////catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //////{
            //////    // 用户取消了操作
            //////    return Result.Cancelled;
            //////}
            //////catch (Exception ex)
            //////{
            //////    message = ex.Message;
            //////    TaskDialog.Show("错误", $"执行过程中发生错误: {ex.Message}");
            //////    return Result.Failed;
            //////}
            //////1111 点击隐藏参照图层
            //try
            //{
            //    //开始事务
            //    using (Autodesk.Revit.DB.Transaction ts = new Autodesk.Revit.DB.Transaction(doc, "隐藏参照dwg中图层"))
            //    {
            //        ts.Start();
            //        Reference r = uiDoc.Selection.PickObject(ObjectType.PointOnElement, new DWGReferenceFilter()); //获取对象
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
            //                doc.ActiveView.SetCategoryHidden(targetCategory.Id, true);
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

            ////0323 房间管理过程版
            //RoomManagerView roomManagerView = new RoomManagerView(uiApp);
            //roomManagerView.ShowDialog();
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

            ////0319 风管高宽互换。OK，是否考虑批量处理
            //Duct targetDuct = null;
            //ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds != null && selectedIds.Count > 0)
            //{
            //    foreach (ElementId id in selectedIds)
            //    {
            //        Element elem = doc.GetElement(id);
            //        if (elem is Duct d) // 判断是否为风管
            //        {
            //            targetDuct = d;
            //            break; // 挑出第一根后直接跳出循环
            //        }
            //    }
            //}
            //if (targetDuct == null)
            //{
            //    try
            //    {
            //        // filterDuct 是你自定义的 ISelectionFilter
            //        Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterDuct(), "请选择一根矩形风管");
            //        targetDuct = doc.GetElement(ref1) as Duct;
            //    }
            //    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //    {
            //        return Result.Cancelled;
            //    }
            //}
            //if (targetDuct == null)
            //{
            //    return Result.Failed;
            //}
            //// 2. 获取宽高参数（需判断是否为矩形风管，圆形风管没有宽高）
            //Parameter widthParameter = targetDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
            //Parameter heightParameter = targetDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
            //if (widthParameter == null || heightParameter == null)
            //{
            //    TaskDialog.Show("错误", "所选风管不是矩形风管！");
            //    return Result.Failed;
            //}
            //double originalWidth = widthParameter.AsDouble();
            //double originalHeight = heightParameter.AsDouble();
            //// 准备一个列表，用来临时保存风管两端连接的其他构件的连接器
            //List<Tuple<Connector, Connector>> connectionsToRestore = new List<Tuple<Connector, Connector>>();
            //using (Transaction ts = new Transaction(doc, "风管宽高转换及重连"))
            //{
            //    ts.Start();
            //    // 【关键点2：寻找连接并断开】
            //    ConnectorManager cm = targetDuct.ConnectorManager;
            //    if (cm != null)
            //    {
            //        // 遍历风管自身的每一个连接器
            //        foreach (Connector ductConn in cm.Connectors)
            //        {
            //            if (ductConn.IsConnected)
            //            {
            //                // AllRefs 包含了与当前连接器相连的所有连接器（包括自身）
            //                foreach (Connector refConn in ductConn.AllRefs)
            //                {
            //                    // 排除自身，且排除逻辑连接，只找物理相连的外部构件
            //                    if (refConn.Owner.Id != targetDuct.Id && refConn.ConnectorType != ConnectorType.Logical)
            //                    {
            //                        // 1. 记录这对连接关系 (风管连接器, 外部构件连接器)再断开
            //                        connectionsToRestore.Add(new Tuple<Connector, Connector>(ductConn, refConn));
            //                        ductConn.DisconnectFrom(refConn);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    widthParameter.Set(originalHeight);
            //    heightParameter.Set(originalWidth);
            //    doc.Regenerate();
            //    // 【关键点4：恢复连接】
            //    foreach (var pair in connectionsToRestore)
            //    {
            //        Connector dConn = pair.Item1;
            //        Connector rConn = pair.Item2;
            //        try
            //        {
            //            // 重新连接
            //            dConn.ConnectTo(rConn);
            //        }
            //        catch (Exception ex)
            //        {
            //            // 极端情况下（如尺寸相差过大且没有开启自动管件功能），重连可能会失败
            //            // 这里可以记录日志，防止整个事务崩溃
            //            System.Diagnostics.Debug.WriteLine($"重连失败: {ex.Message}");
            //        }
            //    }
            //    ts.Commit();
            //}
            //TaskDialog.Show("tt", $"已互换选中风管宽高值，其ID为{targetDuct.Id}");
            ////0319 进度条，新事务处理测试
            ////事务撤回模板
            //TestTemporaryMovement(doc, null);
            /////通用子事务处理模板
            //try
            //{
            //    // 1. 开启主事务
            //    doc.NewTransaction(() =>
            //    {
            //        // --- 业务A（主线安全业务） ---
            //        // 例如：创建一条标高
            //        Level newLevel = Level.Create(doc, 10.0);
            //        // 2. 开启子事务处理危险业务
            //        try
            //        {
            //            doc.NewSubTransaction(() =>
            //            {
            //                Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第一根水平管道");
            //                Element instance = doc.GetElement(ref1) as Element;
            //                Parameter markParam = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            //                if (markParam != null && !markParam.IsReadOnly)
            //                {
            //                    markParam.Set("修改后的新标记");
            //                }
            //                // --- 业务B（可能失败的危险业务） ---
            //                // 例如：尝试在此标高上生成一些复杂的几何形体
            //                // 如果这部分报错抛出异常，扩展方法会自动撤销这部分模型更改
            //            }); // 默认 rollback = false，正常执行则提交子事务
            //        }
            //        catch (Exception)
            //        {
            //            // 子事务失败了，但我们拦截了异常。
            //            // 此时业务B的操作已回滚，但业务A（创建标高）依然保留！
            //            // 可以记录日志或忽略，主事务继续往下走
            //        }
            //    }, "包含子事务的复杂操作");
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("错误", $"整个主事务执行失败: {ex.Message}");
            //    return Result.Failed;
            //}
            ///通用事务处理模板
            //// 直接调用扩展方法，传入 Lambda 表达式 () => { 你的业务代码 }
            //doc.NewTransaction(() =>
            //{
            //    Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第一根水平管道");
            //    Element instance = doc.GetElement(ref1) as Element;
            //    Parameter markParam = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            //    if (markParam != null && !markParam.IsReadOnly)
            //    {
            //        markParam.Set("修改后的新标记");
            //    }
            //}, "修改图元标记"); // 传入事务名称
            //////进度条测试2。OK 通用带进度条事务处理
            //TransactionWithProgressBarHelper.Execute(doc, "处理构件", (service) =>
            //{
            //    List<ElementId> elementsToProcess = uiDoc.Selection.GetElementIds().ToList();
            //    int totalCount = elementsToProcess.Count;
            //    service.UpdateMax(totalCount);
            //    //如果使用foreach还需要增加一个处理顺序int值
            //    int index = 0;
            //    foreach (ElementId elem in elementsToProcess)
            //    {
            //        //doc.Delete(elem);
            //        System.Threading.Thread.Sleep(150);
            //        service.Update(++index, elem.IntegerValue.ToString());
            //    }
            //    //for (int i = 0; i < totalCount; i++)
            //    //{
            //    //    ElementId elem = elementsToProcess[i];
            //    //    System.Threading.Thread.Sleep(150);
            //    //    service.Update(i + 1, elem.IntegerValue.ToString());
            //    //}
            //});
            ////进度条测试1。OK
            ////// 假设我们要处理一系列构件
            //List<ElementId> elementsToProcess = uiDoc.Selection.GetElementIds().ToList();
            //int totalCount = elementsToProcess.Count;
            //// 1. 初始化进度条服务
            //ProgressBarService progressService = new ProgressBarService();
            //// 2. 启动进度条 (非模态，Revit 不会卡住)
            //progressService.Start(totalCount, "开始提取数据...");
            //try
            //{
            //    using (Transaction trans = new Transaction(doc, "处理构件"))
            //    {
            //        trans.Start();
            //        // 3. 遍历处理
            //        for (int i = 0; i < totalCount; i++)
            //        {
            //            ElementId elem = elementsToProcess[i];
            //            // --- 你的业务逻辑代码 ---
            //            // 例如修改参数、提取数据等
            //            System.Threading.Thread.Sleep(150); // 模拟耗时操作，测试时可打开
            //            // ------------------------
            //            // 4. 更新进度条 (传入当前是第几个，以及当前处理的构件名称)
            //            progressService.Update(i + 1, elem.IntegerValue.ToString());
            //        }
            //        trans.Commit();
            //    }
            //    TaskDialog.Show("完成", "所有构件处理完毕！");
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}
            //finally
            //{
            //    // 5. 无论成功失败，确保关闭进度条窗口
            //    progressService.Stop();
            //}

            //////0315 窗口及控件测试模板
            //TestWindow testWindow = new TestWindow(uiApp);
            //testWindow.ShowDialog();
            //////369测试窗口。OK
            //Universal369Buttons universal369Buttons = new Universal369Buttons();
            //universal369Buttons.ShowDialog();
            /////剪贴文本暂存器。OK 注意需要非模态运行
            //ClipboardCatcher clipboard = new ClipboardCatcher();
            //clipboard.Show();
            //////双联按钮 圆形按钮。OK
            ////0313//////0131 测试窗口。OK
            //string tt = "测试定时消隐窗口";
            //string myMessage = "使用。。。已完成";
            //ToastManager.ShowToast(tt, myMessage);
            ////var toast = new ToastWindow(tt, myMessage);
            ////toast.Show();

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

            ////0204 复制文字属性？直接用导出导入属性试试
            ////需要简单界面，选择要复制到的类型
            ////查找当前族实例所有实例文字属性，复制到已选择的对象
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "请选择第一根水平管道");
            //FamilyInstance instance = doc.GetElement(ref1) as FamilyInstance;
            //TaskDialog.Show("tt", instance.Name);

            ////////1122 生成交叉中间立管OK
            //// 1. 拾取第一根管道
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第一根水平管道");
            //Pipe pipe1 = doc.GetElement(ref1) as Pipe;
            //// 2. 拾取第二根管道
            //Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择第二根水平管道");
            //Pipe pipe2 = doc.GetElement(ref2) as Pipe;
            //// 校验：确保是水平管道 (Z轴方向分量接近0)
            //if (!IsHorizontal(pipe1) || !IsHorizontal(pipe2))
            //{
            //    TaskDialog.Show("错误", "请选择水平管道。");
            //    return Result.Failed;
            //}
            //using (Transaction trans = new Transaction(doc, "生成垂直立管"))
            //{
            //    trans.Start();
            //    // 3. 获取管道的几何中心线
            //    Line line1 = (pipe1.Location as LocationCurve).Curve as Line;
            //    Line line2 = (pipe2.Location as LocationCurve).Curve as Line;
            //    // 4. 计算XY平面上的投影交点 (无限延伸)
            //    XYZ intersectionPoint2D = GetIntersectionPoint2D(line1, line2);
            //    if (intersectionPoint2D == null)
            //    {
            //        TaskDialog.Show("错误", "两根管道在XY平面平行，无法生成垂直连接管。");
            //        return Result.Failed;
            //    }
            //    // 5. 准备创建立管的坐标,获取两根管各自在交点处的Z高度
            //    double z1 = line1.Origin.Z;
            //    double z2 = line2.Origin.Z;
            //    // 容差处理，如果高度极度接近则不需要立管
            //    if (Math.Abs(z1 - z2) < 0.01) // 0.01 feet
            //    {
            //        TaskDialog.Show("提示", "两根管道高度几乎一致，无需立管。");
            //        return Result.Cancelled;
            //    }
            //    // 确定立管的底点和顶点
            //    XYZ bottomPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Min(z1, z2));
            //    XYZ topPoint = new XYZ(intersectionPoint2D.X, intersectionPoint2D.Y, Math.Max(z1, z2));
            //    // 6. 创建垂直立管
            //    // 使用第一根管的系统类型和管材类型，以及标高
            //    ElementId systemTypeId = pipe1.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
            //    ElementId pipeTypeId = pipe1.PipeType.Id;
            //    ElementId levelId = pipe1.ReferenceLevel.Id;
            //    Pipe riserInfo = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, bottomPoint, topPoint);
            //    // 设置立管直径（这里取较小管径或第一根管径，可视需求调整）
            //    // 注意：Diameter是只读属性，需通过参数设置
            //    double diameter = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            //    riserInfo.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
            //    // 7. 连接管件 (生成三通/机械三通)
            //    // 需要找到立管的上下连接器
            //    Connector topConnector = GetConnectorAtPoint(riserInfo, topPoint);
            //    Connector bottomConnector = GetConnectorAtPoint(riserInfo, bottomPoint);
            //    // 判断哪个现有管道在上方，哪个在下方
            //    Pipe topPipe = z1 > z2 ? pipe1 : pipe2;
            //    Pipe bottomPipe = z1 > z2 ? pipe2 : pipe1;
            //    // 核心API: NewTakeoffFitting
            //    // 这个方法会在现有管道(pipe)上打断并插入三通，或者插入接头，并连接到指定的connector
            //    //try
            //    //{
            //    //    doc.Create.NewTakeoffFitting(topConnector, topPipe);
            //    //    doc.Create.NewTakeoffFitting(bottomConnector, bottomPipe);
            //    //}
            //    //catch (Exception ex)
            //    //{
            //    //    //TaskDialog.Show("警告", "生成管件失败，可能是没有配置路由首选项或空间不足。" + ex.Message);
            //    //    // 即便管件失败，立管可能已生成，视情况决定是否回滚
            //    //}
            //    trans.Commit();
            //}
            ////0313 日志测试和模板.OK
            //// 初始化日志器
            //RevitOperationLogger.Initialize(uiApp);
            //var logger = RevitOperationLogger.Instance;
            //string commandName = "管道标高修改";
            //logger.LogCommandStart(commandName);
            //try
            //{
            //    // 1. 选择操作日志
            //    var selectedIds = uiDoc.Selection.GetElementIds();
            //    logger.LogSelection("获取当前选择", selectedIds.Count, true);
            //    // 2. 验证操作日志 - 检查选择数量
            //    if (selectedIds.Count > 1)
            //    {
            //        logger.LogValidation("选择数量检查", false, "选择了多个元素，应只选择一个", true);
            //        return Result.Cancelled;
            //    }
            //    Pipe targetPipe = null;
            //    if (selectedIds.Count == 0)
            //    {
            //        // 3. 选择操作 - 手动选择
            //        try
            //        {
            //            logger.LogGeneral("等待用户手动选择管道", true);
            //            Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterPipe(), "请选择一根水平管道");
            //            logger.LogSelection("手动选择管道", 1, true);
            //            targetPipe = doc.GetElement(ref1) as Pipe;
            //            // 4. 空值检查
            //            logger.LogNullCheck("选择的管道", targetPipe == null, true);
            //            if (targetPipe == null)
            //            {
            //                return Result.Cancelled;
            //            }
            //        }
            //        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //        {
            //            logger.LogGeneral("用户取消选择操作", false);
            //            return Result.Cancelled;
            //        }
            //    }
            //    else
            //    {
            //        // 处理已选择的管道
            //        ElementId selectedId = selectedIds.FirstOrDefault();
            //        targetPipe = doc.GetElement(selectedId) as Pipe;
            //        // 4. 空值检查
            //        logger.LogNullCheck("选择的管道", targetPipe == null, true);
            //        if (targetPipe == null)
            //        {
            //            return Result.Cancelled;
            //        }
            //    }
            //    // 5. 参数操作 - 获取标高参数
            //    Parameter sysParam = targetPipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            //    // 6. 空值检查 - 参数
            //    logger.LogNullCheck("标高参数", sysParam == null, true);
            //    if (sysParam == null || !sysParam.HasValue)
            //    {
            //        return Result.Cancelled;
            //    }
            //    // 7. 事务操作
            //    using (Transaction tx = new Transaction(doc, "更改标高"))
            //    {
            //        try
            //        {
            //            tx.Start();
            //            logger.LogTransaction("更改标高", true, "事务开始");
            //            // 记录修改前后的值
            //            double oldValue = sysParam.AsDouble() * 304.8; // 转换为mm
            //            double newValue = 1500.0; // mm
            //            sysParam.Set(newValue / 304.8);
            //            // 8. 参数操作日志
            //            logger.LogParameterOperation("RBS_OFFSET_PARAM", $"管道ID:{targetPipe.Id}", $"{oldValue:F1}mm", $"{newValue:F1}mm", true);
            //            tx.Commit();
            //            logger.LogTransaction("更改标高", true, "事务提交成功");
            //            // 9. 验证操作 - 检查修改结果
            //            double actualValue = sysParam.AsDouble() * 304.8;
            //            bool isSuccess = Math.Abs(actualValue - 1500) < 0.1;
            //            logger.LogValidation("标高修改结果验证", isSuccess, $"当前值: {actualValue:F1}mm, 目标值: 1500mm", !isSuccess);
            //            if (isSuccess)
            //            {
            //                TaskDialog.Show("成功", $"管道标高已修改为 1500mm");
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            if (tx.HasStarted())
            //            {
            //                tx.RollBack();
            //                logger.LogTransaction("更改标高", false, "事务回滚");
            //            }
            //            throw; // 重新抛出，由外层catch处理
            //        }
            //    }
            //    logger.LogCommandEnd(commandName, true);
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    // 10. 异常处理日志
            //    logger.LogException(ex, commandName, true);
            //    logger.LogCommandEnd(commandName, false);
            //    return Result.Failed;
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
            ////0310 继续日志测试。依赖有些过多，暂时放弃使用官方ILogger
            //var loggerFactory = LoggerFactory.Create(builder =>
            //    {
            //        //builder.AddJsonConsole();
            //        //builder.AddFilter();
            //    });
            //ILogger logger = loggerFactory.CreateLogger<Test11_0118>();
            //var name = "Nick";
            //var age = 30;
            ////logger.LogInformation($"{name}just turned:{age}");
            //logger.LogInformation("{Name}just turned:{Age}",name,age); 
            //0212 日志功能测试 安装Text.Json 和Externsion.Logging
            //https://www.bilibili.com/video/BV1k7HyzNEpQ
            //默认日志接口使用
            //using var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder.AddConsole();
            //});
            //ILogger logger = loggerFactory.Create();
            //结构化日志不应直接字符串拼接记录变量，而应当适用指定变量与要显示的值挂接
            //ILogger<Test11_0118> logger = null;          
            //////0313 简化日志测试
            ////string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //// 获取当前程序集对象DLL 的完整路径 
            //Assembly assembly = Assembly.GetExecutingAssembly();
            //string dllFullPath = assembly.Location;
            //// 获取 DLL 所在的目录路径 (推荐，通常用于加载配置文件或依赖DLL)
            //string dllFolder = Path.GetDirectoryName(dllFullPath);
            ////TaskDialog.Show("DLL 路径", dllFolder);
            //ToastManager.ShowToast("DLL 路径", dllFolder);
            //////异常字符处理方案，获取当前程序集的位置
            ////string codeBase = Assembly.GetExecutingAssembly().Location;
            ////// 转换为本地文件路径格式 (处理可能的 URI 格式问题)
            ////// UriBuilder 用于处理路径中包含特殊字符或中文的情况
            ////UriBuilder uri = new UriBuilder(codeBase);
            ////string path = Uri.UnescapeDataString(uri.Path);
            //0317 坡度参数测试 
            /////桥架坡度测试 CURVE_ELEM_LENGTH
            //Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterCableTray(), "请选择第一根桥架");
            //CableTray ct = doc.GetElement(ref1) as CableTray;
            //double deltaHeight = ct.get_Parameter(BuiltInParameter.RBS_START_OFFSET_PARAM).AsDouble() - ct.get_Parameter(BuiltInParameter.RBS_END_OFFSET_PARAM).AsDouble();
            //double ctLength = ct.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            //double horizontalLength = Math.Sqrt(Math.Max(0, Math.Pow(ctLength, 2) - Math.Pow(deltaHeight, 2)));
            //double jd =  0;
            //if (horizontalLength > 0.0001) // 避免除以 0 (垂直桥架)
            //{
            //    jd = deltaHeight / horizontalLength;
            //}
            //TaskDialog.Show("tt", (jd * 100).ToString("0.00"));
            ////Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, new filterCableTray(), "请选择第一根桥架");
            ////MEPCurve curve = doc.GetElement(ref1) as MEPCurve;
            ////double jd=MEPSlopeHelper.GetSlope(curve);
            ////TaskDialog.Show("tt", (jd * 100).ToString("0.00"));
            ////0318 csvHelper测试.OK
            ////// 基本CSV操作示例
            //string filePath = @"D:\CACCWPF\test.csv";
            //var csv = new CsvHelper(filePath, ",");
            ////// 写入数据
            //csv.WriteAll(new[]
            //{
            //    new[] { "姓名", "年龄", "城市" },
            //    new[] { "张三", "25", "北京" },
            //    new[] { "李四", "30", "上海" },
            //    new[] { "王五", "28", "广州" }
            //});
            ////// 追加一行
            //csv.AppendLine("赵六", "35", "深圳");
            //////// 读取所有数据
            ////var allData = csv.ReadAll();
            ////Console.WriteLine("所有数据：");
            ////foreach (var row in allData)
            ////{
            ////    TaskDialog.Show("tt", (string.Join(" | ", row)));
            ////}
            ////// 读取带标题的数据，不是自动组合dict标题
            ////var dataWithHeaders = csv.ReadAllWithHeaders();
            //////Console.WriteLine("\n带标题的数据：");
            ////foreach (var dict in dataWithHeaders)
            ////{
            ////    //Console.WriteLine($"姓名: {dict["姓名"]}, 年龄: {dict["年龄"]}, 城市: {dict["城市"]}");
            ////    TaskDialog.Show("tt", $"姓名: {dict["姓名"]}, 年龄: {dict["年龄"]}, 城市: {dict["城市"]}");
            ////}
            ////// 更新数据
            ////csv.UpdateField(2, 1, "31"); // 将第2行（李四）的年龄改为31
            ////// 读取指定行
            //var line = csv.ReadLine(2);
            //TaskDialog.Show("tt", $"\n更新后的第2行: {string.Join(" | ", line)}");
            ////Console.WriteLine($"\n更新后的第2行: {string.Join(" | ", line)}");
            //// 泛型CSV操作示例
            //string filePath = @"C:\temp\persons.csv";
            //var csv = new CsvHelper<Person>(filePath, Encoding.UTF8, ",");
            //// 创建测试数据
            //var persons = new List<Person>
            //{
            //    new Person { Name = "张三", Age = 25, City = "北京", Salary = 8000.50m, IsActive = true, BirthDate = new DateTime(1998, 5, 20) },
            //    new Person { Name = "李四", Age = 30, City = "上海", Salary = 12000.00m, IsActive = true, BirthDate = new DateTime(1993, 8, 15) },
            //    new Person { Name = "王五", Age = 28, City = "广州", Salary = 9500.75m, IsActive = false, BirthDate = new DateTime(1995, 3, 10) }
            //};
            //// 写入数据
            //csv.WriteAll(persons);
            //// 读取数据
            //var loadedPersons = csv.ReadAll();
            //Console.WriteLine("\n读取的人员数据：");
            //foreach (var p in loadedPersons)
            //{
            //    Console.WriteLine($"{p.Name}, {p.Age}岁, {p.City}, 薪资: {p.Salary}, 活跃: {p.IsActive}, 生日: {p.BirthDate:yyyy-MM-dd}");
            //}
            //// 使用自定义标题映射
            //var mapping = new Dictionary<string, string>
            //{
            //    { "Name", "姓名" },
            //    { "Age", "年龄" },
            //    { "City", "城市" },
            //    { "Salary", "薪资" },
            //    { "IsActive", "是否在职" }
            //};
            //csv.WriteAll(persons, mapping);
            //var mappedPersons = csv.ReadAll(mapping);
            //Console.WriteLine("\n使用映射读取的数据：");
            //foreach (var p in mappedPersons)
            //{
            //    Console.WriteLine($"{p.Name}, {p.Age}岁, {p.City}");
            //}
            //// 追加数据
            //csv.Append(new Person { Name = "赵六", Age = 35, City = "深圳", Salary = 15000.00m });
            //////0317  管道异常坡度检测，按系统？全部
            //List<ElementId> selIds = uiDoc.Selection.GetElementIds().ToList();
            //double angle = 0;
            //if (selIds.Count != 0)
            //{
            //    CheckPipeSlope(doc, selIds, angle);
            //}
            //else
            //{
            //    TaskDialog td = new TaskDialog("重要提示")
            //    {
            //        MainInstruction = "请确认检查范围",
            //        MainContent = "未选择任何对象，将检查所有机电系统线性构件坡度，是否继续？",
            //        MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            //        CommonButtons = TaskDialogCommonButtons.Close,
            //        DefaultButton = TaskDialogResult.Close
            //    };
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "检查所有系统");
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "重新选择对象");
            //    TaskDialogResult result = td.Show();
            //    if (result == TaskDialogResult.CommandLink1)
            //    {
            //        var allPipeIds = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).ToElementIds().ToList();
            //        CheckPipeSlope(doc, allPipeIds, angle);
            //    }
            //    else
            //    {
            //        return Result.Cancelled;
            //    }
            //}
            return Result.Succeeded;
        }
        //private int _maximum;
        //public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void SetProperty<T>(ref T store, T v, [CallerMemberName] string propertyName = null)
        {
            store = v;
            this.OnPropertyChanged(propertyName);
        }
    }
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
