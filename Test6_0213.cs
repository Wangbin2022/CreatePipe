using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommandLine;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using EnumsNET;
using NPOI.OpenXmlFormats.Vml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Documents;
using System.Windows.Forms;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test6_0213 : IExternalCommand
    {
        public class SupressWarning : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                try
                {
                    var failures = failuresAccessor.GetFailureMessages();
                    foreach (var fail in failures)
                    {
                        FailureSeverity severity = fail.GetSeverity();
                        string description = fail.GetDescriptionText();
                        FailureDefinitionId fail_id = fail.GetFailureDefinitionId();
                        if (severity == FailureSeverity.Warning)
                        {
                            if (fail_id == BuiltInFailures.GeneralFailures.DuplicateValue)
                            {
                                TaskDialog.Show("tt", $"解决错误{description}");
                                failuresAccessor.DeleteWarning(fail);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                return FailureProcessingResult.Continue;
            }
        }
        private bool IsConnectedAtBothEnds(MEPCurve mEPCurve)
        {
            // 获取连接点
            ConnectorSet connectors = mEPCurve.ConnectorManager.Connectors;
            Connector startConnector = connectors.Cast<Connector>().FirstOrDefault(c => c.Domain == Domain.DomainHvac);
            Connector endConnector = connectors.Cast<Connector>().LastOrDefault(c => c.Domain == Domain.DomainHvac);

            if (startConnector == null || endConnector == null)
            {
                return false; // 如果没有找到连接点，返回 false
            }

            // 检查连接状态
            bool startConnected = startConnector.IsConnected;
            bool endConnected = endConnector.IsConnected;
            bool result = false;
            if (startConnected == true || endConnected == true)
            {
                result = true;
            }
            return result;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();

            //TaskDialog.Show("tt", con1.Size.ToString() );//构件连接器数量
            //0225 喷头转换？
            //先试一下找到的风口上下切换
            Selection sel = uiDoc.Selection;
            var instance = doc.GetElement(sel.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "选取风口末端")) as FamilyInstance;
            using (Transaction ts = new Transaction(doc, "Test"))
            {
                ts.Start();
                ////找出构件的偏移值并设置参数
                //var s1 = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                //s1.Set(3000 / 304.8);
                //构件旋转
                LocationPoint locationPoint = instance.Location as LocationPoint;
                if (locationPoint == null)
                {
                    TaskDialog.Show("错误", "选择的构件没有位置信息！");
                }
                else
                { 
                XYZ rotationCenter = locationPoint.Point;
                XYZ rotationAxis = new XYZ(0, 0, 1);
                // 定义旋转角度（90度，单位为弧度）
                double rotationAngle = Math.PI / 2;
                ElementTransformUtils.RotateElement(doc, instance.Id, rotationAxis, rotationAngle);
                }
                //if (instance.CanRotate)
                //{
                //    TaskDialog.Show("tt", "PASS");
                //}
                //else { TaskDialog.Show("tt", "NOPASS"); }
                //找构件连接器连得是什么对象


                ts.Commit();
            }
            //旋转构件
            //var con1 = instance.MEPModel.ConnectorManager.Connectors;
            //foreach (Connector item in con1)
            //{
            //    if (item.IsConnected)
            //    {
            //        TaskDialog.Show("tt", "PASS");
            //    }
            //    else { TaskDialog.Show("tt", "NOPASS"); }
            //}
            //TaskDialog.Show("tt", instance.Id.ToString());
            //Connector con = null;
            //Connector conn = null;
            //Duct duct = null;
            //Transaction tr = new Transaction(doc);
            //tr.Start("管线微调后连接");
            //try
            //{
            //    var conset = instance.MEPModel.ConnectorManager.Connectors;
            //    foreach (Connector item in conset)
            //    {
            //        con = item;
            //        var itemOR = item.Origin.ToString();
            //        var connset = item.AllRefs;
            //        foreach (Connector item1 in connset)
            //        {
            //            if (!item1.IsConnected) continue;
            //            conn = item1;
            //            duct = conn.Owner as Duct;
            //            con.DisconnectFrom(conn); // 断开风道末端

            //            // 获取风管的中心线作为旋转轴
            //            LocationCurve locationCurve = duct.Location as LocationCurve;
            //            if (locationCurve == null) continue;

            //            Line rotationAxis = locationCurve.Curve as Line;
            //            if (rotationAxis == null) continue;

            //            // 旋转风道末端90度（1.57弧度）
            //            double rotationAngle = Math.PI / 2; // 90度
            //            ElementTransformUtils.RotateElement(doc, instance.Id, rotationAxis, rotationAngle);

            //            // 重新连接风道末端和风管
            //            var mepcurve = doc.GetElement(duct.Id) as MEPCurve;
            //            MechanicalUtils.ConnectAirTerminalOnDuct(doc, instance.Id, mepcurve.Id);
            //        }
            //    }
            //    tr.Commit();
            //}
            //catch (Exception ex)
            //{
            //    tr.RollBack();
            //    TaskDialog.Show("Error", ex.Message);
            //}
            //源程序，只能参考
            //Selection sel = uiDoc.Selection;
            //var instance = doc.GetElement(sel.PickObject(ObjectType.Element, "选取风口末端")) as FamilyInstance;
            //Connector con = null;
            //Connector conn = null;
            //Duct duct = null;
            //Transaction tr = new Transaction(doc);
            //tr.Start("管线微调后连接");
            //var conset = instance.MEPModel.ConnectorManager.Connectors;
            //foreach (Connector item in conset)
            //{
            //    con = item;
            //    var itemOR = item.Origin.ToString();
            //    var connset = item.AllRefs;
            //    foreach (Connector item1 in connset)
            //    {
            //        if (!item1.IsConnected) continue;
            //        conn = item1;
            //        duct = conn.Owner as Duct;
            //        con.DisconnectFrom(conn);//断开风道末端
            //        LocationCurve line = duct.Location as LocationCurve;
            //        XYZ rotationCenter = line.Curve.GetEndPoint(0); // 使用曲线起点作为旋转中心点
            //        XYZ rotationAxis = XYZ.BasisZ; // 假设绕 Z 轌道旋转
            //        var mepcurve = doc.GetElement(duct.Id) as MEPCurve;
            //        //待补充测试，旋转对象方法的参数设置
            //        //ElementTransformUtils.RotateElement(doc, instance.Id, line, 1.57);
            //        ElementTransformUtils.RotateElement(doc, instance.Id, rotationCenter, rotationAxis, 1.57);
            //        //旋转风道末端，需自定一个旋转方法
            //        //MechanicalUtils.ConnectAirTerminalOnDuct(doc, instance.Id, mepcurve.Id);
            //        MechanicalUtils.ConnectAirTerminalOnDuct(doc, instance.Id, duct.Id);

            //    }

            //}



            ////0225 找无机电系统构件
            //var elem = uiDoc.Selection.PickObject(ObjectType.Element, new filterMEPCurveClass(), "选风管或水管");
            //var mEPCurve = doc.GetElement(elem.ElementId) as MEPCurve;
            //if (mEPCurve.MEPSystem == null)
            //{
            //    TaskDialog.Show("tt", "NOPASS");
            //}
            //else
            //{
            //    //TaskDialog.Show("tt", mEPCurve.MEPSystem.Name); 
            //    // 检测两端是否均与其他管或管件相连
            //    //bool isConnectedAtBothEnds = IsConnectedAtBothEnds(mEPCurve);
            //    // 显示结果
            //    string resultMessage = IsConnectedAtBothEnds(mEPCurve) ? "至少有一端与其他管或管件相连。" : "未连接到其他管或管件。";
            //    TaskDialog.Show("检查结果", resultMessage);
            //}
            ////查找两端是否均无连接

            //0224 按官方参考删除Schema，变量不全还需要测试
            //https://thebuildingcoder.typepad.com/blog/2022/11/extensible-storage-schema-deletion.html
            //using (Transaction tErase = new Transaction(doc, "Erase EStorage"))
            //{ 
            //    tErase.Start();
            //    foreach (Schema schema in schemas.Where(sbyte=>sbyte.GUID.ToString()=="xxx"))
            //    {
            //        try
            //        {
            //            doc.EraseSchemaAndAllEntities(schema);
            //            Schema.EraseSchemaAndAllEntities(schema, true);
            //            deleted++;
            //        }
            //        catch (Exception ex)
            //        {
            //            message += ex.Message + "\n";
            //            TaskDialog.Show("tt", ex.Message);
            //        }

            //    }
            //    tErase.Commit();
            //}

            //0223 错误处理器，必须在命令事务中可能出错时才执行，不是对已有错误的解决。。。
            //参考https://learnrevitapi.com/newsletter/how-to-suppress-warnings-in-revit-api
            //var wall = uiDoc.Selection.PickObject(ObjectType.Element, new filterWallClass(), "选墙");
            //Wall elem = doc.GetElement(wall.ElementId) as Wall;
            //以下正式测试重复属性代码
            //List<Wall> elems = new List<Wall>();
            //foreach (Reference refItem in uiDoc.Selection.PickObjects(ObjectType.Element, new filterWallClass(), "选择墙"))
            //{
            //    Element elem = uiDoc.Document.GetElement(refItem);
            //    if (elem is Wall wall)
            //    {
            //        elems.Add(wall);
            //    }
            //}
            //using (Transaction trans = new Transaction(doc, "Update Mark"))
            //{
            //    foreach (var item in elems)
            //    {
            //        trans.Start();
            //        var p_mark = item.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            //        p_mark.Set("Warning 2");
            //        var fail_hand_opts = trans.GetFailureHandlingOptions();
            //        fail_hand_opts.SetFailuresPreprocessor(new SupressWarning());
            //        trans.SetFailureHandlingOptions(fail_hand_opts);
            //        trans.Commit();
            //    }
            //}

            //0222 用标高切分结构柱，初步完成 在结构柱分层的高度上仍有问题。。要考虑柱的顶底偏移再设置切分逻辑
            //新的柱子虽然不用考虑开洞但仍需手动考虑偏移的各种情况给赋值。
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
            //        TaskDialog.Show("提示", "没有合适的标高用于切分结构柱！");
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
            //TaskDialog.Show("提示", "结构柱切分成功！");

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

            //0222 视图批量修改 界面.OK  
            //ModifyViewDisplayForm displayForm = new ModifyViewDisplayForm(uiApp);
            //displayForm.Show();
            ////0222 视图详细程度和样式控制
            ////取消视图样板
            //using (Transaction viewTemplate = new Transaction(doc, "视图样板设置删除"))
            //{
            //    viewTemplate.Start();
            //    if (!activeView.IsTemplate)
            //    {
            //        activeView.ViewTemplateId = ElementId.InvalidElementId;
            //    }
            //    viewTemplate.Commit();
            //}
            //0222 设置视图详细程度，显示样式，规程
            //using (Transaction viewTemplate = new Transaction(doc, "设置详细程度和样式"))
            //{
            //    viewTemplate.Start();
            //    if (!activeView.IsTemplate)
            //    {
            //        //activeView.DisplayStyle = DisplayStyle.Wireframe;
            //        //activeView.DetailLevel = ViewDetailLevel.Fine;
            //        //activeView.Discipline = ViewDiscipline.Mechanical;
            //        //0未指定，
            //    }
            //    viewTemplate.Commit();
            //}

            ////0222 画平行参照平面和管理功能界面.OK  
            //TabTest tabTest = new TabTest(uiApp);
            //tabTest.Show();
            ////0218 找参照平面
            ////FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane));
            ////List<ReferencePlane> referencePlanes = new List<ReferencePlane>();
            ////foreach (var item in elems)
            ////{
            ////    if (item is ReferencePlane)
            ////    {
            ////        referencePlanes.Add( item as ReferencePlane);
            ////    }
            ////}
            ////等同上句
            //List<ReferencePlane> referencePlanes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
            //List<ReferencePlane> visibleReferencePlanes = new List<ReferencePlane>();
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (ReferencePlane rp in referencePlanes)
            //{
            //    if (rp.CanBeVisibleInView(activeView))
            //    {
            //        visibleReferencePlanes.Add(rp);
            //        stringBuilder.AppendLine(rp.Id.ToString());
            //    }
            //}
            //TaskDialog.Show("tt", referencePlanes.Count().ToString() + "++" + visibleReferencePlanes.Count().ToString());
            //以上找所有参照平面并统计。。。
            //TaskDialog.Show("tt", stringBuilder.ToString());
            ////visibleReferencePlanes.在一般项目和族文档中数量统计是正确的？？找到原因是墙生成时的边界
            //为什么附加墙面时会产生参考平面？而且相同墙面转角就一半生成一半不生成。0219 只要有门就会产生，多个门会产生多个。
            //// 统计方向相同的 ReferencePlane 的数量
            ////怎么找出与当前视图方向相同的参照平面？？
            ////在其他视图应弹出不支持并退出
            ////在族里生成参照平面 只需要改一下方法主体名字即可
            ////族视图ViewType三维 ThreeD 平面 FloorPlan/CeilingPlan 立面Elevation 剖面 Section
            ////族视图GetType() View3D ViewPlan ViewSection
            //ReferencePlane referencePlane = doc.Create.NewReferencePlane(startPoint, endPoint, normal, familyActiveView);
            //ReferencePlane referencePlane = doc.FamilyCreate.NewReferencePlane(startPoint, endPoint, normal, familyActiveView);
            ////0216 测试画平行详图线，OK
            //doc.NewTransaction(() =>
            //{
            //    if (activeView.SketchPlane == null)
            //    {
            //        //获取视图的视角方向（法线方向）
            //        XYZ normal = activeView.ViewDirection;
            //        //定义工作平面的原点（通常使用视图的原点）
            //        XYZ origin = activeView.Origin;
            //        Plane workPlane = Plane.CreateByNormalAndOrigin(normal, origin);
            //        activeView.SketchPlane = SketchPlane.Create(doc, workPlane);
            //    }
            //    if (activeView.GetType().Name == "ViewPlan" || activeView.GetType().Name == "ViewSection")
            //    {
            //        XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
            //        XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
            //        var line1 = Line.CreateBound(pt1, pt2);
            //        Curve curve1 = line1 as Curve;
            //        //计算垂直于线的偏移方向（法线方向）
            //        XYZ offsetDirection = getnormal(curve1, activeView);
            //        double offsetDistance = 200 / 304.8;
            //        //计算偏移后的起点和终点
            //        XYZ offsetPt1 = line1.GetEndPoint(0) + offsetDirection * offsetDistance;
            //        XYZ offsetPt2 = line1.GetEndPoint(1) + offsetDirection * offsetDistance;
            //        //创建偏移线
            //        Line offsetLine = Line.CreateBound(offsetPt1, offsetPt2);
            //        doc.Create.NewDetailCurve(activeView, line1);
            //        doc.Create.NewDetailCurve(activeView, offsetLine);
            //    }
            //}, "画平行详图线");
            ////例程结束
            //0213 参考平面绘制方法
            // 定义视图坐标系的变换矩阵
            //Transform vTrans = Transform.Identity;
            //vTrans.BasisX = activeView.RightDirection;
            //vTrans.BasisY = activeView.UpDirection;
            //vTrans.BasisZ = activeView.ViewDirection;
            //vTrans.Origin = activeView.Origin;
            //// 定义参考平面的点（在视图坐标系中）
            //double len = 100;
            //XYZ pt1 = new XYZ(-len, 0, 0);  // 水平线起点
            //XYZ pt2 = new XYZ(len * 2, 0, 0);  // 水平线终点
            //XYZ pt3 = new XYZ(0, -len, 0);  // 垂直线起点
            //XYZ pt4 = new XYZ(0, len * 2, 0);  // 垂直线终点
            //// 将点从视图坐标系转换到模型空间
            //XYZ[] pts = new XYZ[] { pt1, pt2, pt3, pt4 };
            //for (int i = 0; i < pts.Length; i++)
            //{
            //    pts[i] = vTrans.OfPoint(pts[i]);
            //}
            //// 创建参考平面
            //using (Transaction tx = new Transaction(doc, "Create Reference Planes"))
            //{
            //    tx.Start();
            //    // 创建第一个参考平面（水平方向）
            //    ReferencePlane rp1 = doc.Create.NewReferencePlane(pts[0], pts[1], activeView.ViewDirection, activeView);
            //    // 创建第二个参考平面（垂直方向）
            //    ReferencePlane rp2 = doc.Create.NewReferencePlane(pts[2], pts[3], activeView.ViewDirection, activeView);
            //    tx.Commit();
            //}
            //例程结束
            //0215 测试加属性，要通过共享参数的方式，没有深化
            //要试一下怎么把某个族的属性直接赋值到另一个上
            //bool succeeded = true;
            //string filePath = @"D:\parameters.txt";
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //fDialog.Filter = "RFA 文件 (*.rfa)|*.rfa";
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    FileInfo fileInfo = new FileInfo(fDialog.FileName);
            //    uiApp.Application.OpenDocumentFile(fileInfo.FullName);

            //    FamilyParameterAssigner assigner = new FamilyParameterAssigner(uiApp.Application, doc);
            //    // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
            //    assigner.LoadParametersFromFile();
            //    Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
            //    t.Start();
            //    assigner.AddParameters();
            //    t.Commit();
            //    doc.Save();
            //    doc.Close();
            //    //TaskDialog.Show("tt", fileInfo.Name);
            //}

            return Result.Succeeded;
        }
        private XYZ getnormal(Curve curve, Autodesk.Revit.DB.View view)
        {
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);
            double res = 100;
            double tolerance = 1e-6;
            //if (Math.Abs((p2 - p1).Normalize().Z) < tolerance )
            //if (Math.Abs(p1.X) < tolerance && Math.Abs(p2.X) < tolerance)            
            //if (Math.Abs(p1.X) == Math.Abs(p2.X) && Math.Abs(p1.Y) != Math.Abs(p2.Y) && Math.Abs(p1.Z) != Math.Abs(p2.Z))
            //bool xView = ;
            if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.Z == 1 || view.ViewDirection.Z == -1))
            {
                XYZ t0 = new XYZ(p1.X - (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y + (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                XYZ t1 = new XYZ(p1.X + (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y - (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Y 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Z) < tolerance && (view.ViewDirection.Y == 1 || view.ViewDirection.Y == -1))
            {
                XYZ t0 = new XYZ(p1.X + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z - (p2.X - p1.X) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z + (p2.X - p1.X) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Z 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.Z) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.X == 1 || view.ViewDirection.X == -1))
            {
                XYZ t0 = new XYZ(p1.X, p1.Y - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z + (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X, p1.Y + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z - (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            else
            {
                throw new InvalidOperationException("两个端点的坐标不满足条件。");
            }
        }
        private SketchPlane CreateSketchPlane(Document docc, XYZ normal, XYZ origin)
        {
            try
            {
                Plane geometryPlane = Plane.CreateByNormalAndOrigin(normal, origin);
                if (geometryPlane == null)
                {
                    throw new Exception("创建平面失败.");
                }
                SketchPlane plane = SketchPlane.Create(docc, geometryPlane);
                if (plane == null)
                {
                    throw new Exception("创建草图平面失败.");
                }
                return plane;
            }
            catch (Exception ex)
            {
                throw new Exception("无法创建草图平面，出错原因: " + ex.Message);
            }
        }
        [Transaction(TransactionMode.Manual)]
        public class AddParameterToFamily : IExternalCommand
        {
            private UIApplication m_app;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                m_app = commandData.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = AddParameters();
                    if (succeeded)
                    {
                        return Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Result.Failed;
                }
            }

            private bool AddParameters()
            {
                Document doc = m_app.ActiveUIDocument.Document;
                if (null == doc)
                {
                    MessageManager.MessageBuff.Append("There's no available document. \n");
                    return false;
                }
                if (!doc.IsFamilyDocument)
                {
                    MessageManager.MessageBuff.Append("The active document is not a family document. \n");
                    return false;
                }
                FamilyParameterAssigner assigner = new FamilyParameterAssigner(m_app.Application, doc);
                // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
                bool succeeded = assigner.LoadParametersFromFile();
                if (!succeeded)
                {
                    return false;
                }
                Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
                t.Start();
                succeeded = assigner.AddParameters();
                if (succeeded)
                {
                    t.Commit();
                    return true;
                }
                else
                {
                    t.RollBack();
                    return false;
                }
            }
        } // end of class "AddParameterToFamily"
        [Transaction(TransactionMode.Manual)]
        public class AddParameterToFamilies : IExternalCommand
        {
            //在 Revit 的族文件（.rfa）中批量添加参数
            private Autodesk.Revit.ApplicationServices.Application application;
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                application = commandData.Application.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = LoadFamiliesAndAddParameters();

                    if (succeeded)
                    {
                        return Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Result.Failed;
                }
            }

            /// <summary>
            /// search for the family files and the corresponding parameter records
            /// load each family file, add parameters and then save and close.
            /// </summary>
            /// <returns>
            /// if succeeded, return true; otherwise false
            /// </returns>
            private bool LoadFamiliesAndAddParameters()
            {
                bool succeeded = true;
                List<string> famFilePaths = new List<string>();
                Environment.SpecialFolder myDocumentsFolder = Environment.SpecialFolder.MyDocuments;
                string myDocs = Environment.GetFolderPath(myDocumentsFolder);
                string families = myDocs + "\\AutoParameter_Families";
                if (!Directory.Exists(families))
                {
                    MessageManager.MessageBuff.Append("The folder [AutoParameter_Families] doesn't exist in [MyDocuments] folder.\n");
                }
                DirectoryInfo familiesDir = new DirectoryInfo(families);
                FileInfo[] files = familiesDir.GetFiles("*.rfa");
                if (0 == files.Length)
                {
                    MessageManager.MessageBuff.Append("No family file exists in [AutoParameter_Families] folder.\n");
                }
                foreach (FileInfo info in files)
                {
                    if (info.IsReadOnly)
                    {
                        MessageManager.MessageBuff.Append("Family file: \"" + info.FullName + "\" is read only. Can not add parameters to it.\n");
                        continue;
                    }

                    string famFilePath = info.FullName;
                    Document doc = application.OpenDocumentFile(famFilePath);

                    if (!doc.IsFamilyDocument)
                    {
                        succeeded = false;
                        MessageManager.MessageBuff.Append("Document: \"" + famFilePath + "\" is not a family document.\n");
                        continue;
                    }

                    // return and report the errors
                    if (!succeeded)
                    {
                        return false;
                    }

                    FamilyParameterAssigner assigner = new FamilyParameterAssigner(application, doc);
                    // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
                    succeeded = assigner.LoadParametersFromFile();
                    if (!succeeded)
                    {
                        MessageManager.MessageBuff.Append("Failed to load parameters from parameter files.\n");
                        return false;
                    }
                    Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
                    t.Start();
                    succeeded = assigner.AddParameters();
                    if (succeeded)
                    {
                        t.Commit();
                        doc.Save();
                        doc.Close();
                    }
                    else
                    {
                        t.RollBack();
                        doc.Close();
                        MessageManager.MessageBuff.Append("Failed to add parameters to " + famFilePath + ".\n");
                        return false;
                    }
                }
                return true;
            }
        } // end of class "AddParameterToFamilies"
    }
    public class filterReferencePlane : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is ReferencePlane)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    static class MessageManager
    {
        static StringBuilder m_messageBuff = new StringBuilder();
        /// <summary>
        /// store the warning/error messages
        /// </summary>
        public static StringBuilder MessageBuff
        {
            get
            {
                return m_messageBuff;
            }
            set
            {
                m_messageBuff = value;
            }
        }
    }
}
