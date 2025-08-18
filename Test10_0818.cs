using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
            TaskDialog.Show("tt", selectedName);
        };
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

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
            GuidanceSignPlaceView placeView = new GuidanceSignPlaceView(uiApp);
            placeView.Show();

            ////////0812 标识标签
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new TagFilter(), "Pick something");
            //IndependentTag tag = (IndependentTag)doc.GetElement(r.ElementId);
            //FamilySymbol tagSymbol = tag.Document.GetElement(tag.GetTypeId()) as FamilySymbol;

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
            return Result.Succeeded;
        }
    }
}
