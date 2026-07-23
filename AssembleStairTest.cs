using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class AssembleStairTest : IExternalCommand
    {
        //数字标准差检查方法
        public static bool HasDeviation(List<double> numbers, double threshold = 2.0)
        {
            // 数据量不足 3 时，返回 false
            if (numbers == null || numbers.Count < 3) return false;
            // 计算平均值
            double mean = numbers.Average();
            // 计算总体标准差
            double sumOfSquares = numbers.Sum(x => Math.Pow(x - mean, 2));
            double stdDev = Math.Sqrt(sumOfSquares / numbers.Count);
            // 如果标准差为 0，说明所有数相等，无偏离
            if (stdDev == 0) return false;
            // 检查是否存在任何一个值偏离超过阈值
            return numbers.Any(x => Math.Abs(x - mean) > threshold * stdDev);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //List<double> data = new List<double> { 1712.0, 1712.0, 1710, 1708 };
            //bool hasOutlier = HasDeviation(data); 
            //TaskDialog.Show("tt", $"是否存在偏离值: {hasOutlier}");



            //////0909 取楼梯中心几何点
            //var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            //Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            //BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            //if (bbox == null) return Result.Failed;

            //XYZ min = bbox.Min;
            //XYZ max = bbox.Max;
            //XYZ center = (min + max) * 0.5;
            //// 输出中心点（XY）
            //TaskDialog.Show("楼梯中心", $"楼梯 {stair.Id} 的中心点XY坐标: ({center.X}, {center.Y})");
            ////例程结束

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
            var columnRef = uiDoc.Selection.PickObject(ObjectType.Element, new StairsFilter(), "选择楼梯");
            Stairs stair = doc.GetElement(columnRef.ElementId) as Stairs;
            if (stair.MultistoryStairsId == ElementId.InvalidElementId)
            {
                ICollection<ElementId> runIds = stair.GetStairsRuns();
                TaskDialog.Show("tt", runIds.Count.ToString());
                //TaskDialog.Show("tt", stair.NumberOfStories.ToString());
                ////实际单步高度
                //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
                //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
                ////实际单步深度,踏面数量
                //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());
                //TaskDialog.Show("tt", (stair.ActualTreadsNumber).ToString());
            }
            else
            {
                MultistoryStairs multiStairs = doc.GetElement(stair.MultistoryStairsId) as MultistoryStairs;
                var ids = multiStairs.GetStairsPlacementLevels(stair);
                Stairs firstStair = multiStairs.GetStairsOnLevel(doc.GetElement(ids.FirstOrDefault()).Id);
                ////实际单步高度
                //TaskDialog.Show("tt", (stair.ActualRiserHeight * 304.8).ToString());
                //TaskDialog.Show("tt", (stair.ActualRisersNumber).ToString());
                ICollection<ElementId> runIds = stair.GetStairsRuns();
                TaskDialog.Show("tt", runIds.Count.ToString());

                //TaskDialog.Show("tt", ids.Count().ToString());
                //实际单步深度,踏面数量
                //TaskDialog.Show("tt", (stair.ActualTreadDepth * 304.8).ToString());



            }

            ////绝对高度底和顶，要计入项目基点高差
            //var basePoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
            //double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
            //TaskDialog.Show("tt", (stair.BaseElevation * 304.8 - deltaHeight).ToString("F2"));
            //TaskDialog.Show("tt", (stair.TopElevation * 304.8 - deltaHeight).ToString("F2"));
            ////楼梯总高差
            //TaskDialog.Show("tt", (stair.Height * 304.8).ToString());
            //TaskDialog.Show("tt", (stair.GetStairsRuns().Count()).ToString());
            ////跑数和内部各跑宽度，高度等
            //var runs = stair.GetStairsRuns();
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var item in runs)
            //{
            //    StairsRun stairsRun = doc.GetElement(item) as StairsRun;
            //    stringBuilder.AppendLine((stairsRun.ActualRunWidth * 304.8).ToString());
            //}
            //TaskDialog.Show("tt", runs.Count().ToString());

            //例程结束
            //////0906 房间楼梯关系梳理 ，判断楼梯是否有部分在房间内即可，没必要全匹配
            //var room = doc.GetElement(new ElementId(2006502)) as Room;
            ////var room = doc.GetElement(new ElementId(1295107)) as Room;
            ////var room = doc.GetElement(new ElementId(1295122)) as Room;
            //var boundaryOptions = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
            ////int edges = room.GetBoundarySegments(boundaryOptions).Sum(loop => loop.Count);
            //IList<IList<BoundarySegment>> boundarySegments = room.GetBoundarySegments(boundaryOptions);
            //BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            //XYZ minPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            //XYZ maxPoint = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            //foreach (IList<BoundarySegment> boundaryLoop in boundarySegments)
            //{
            //    CurveLoop curveLoop = new CurveLoop();
            //    foreach (BoundarySegment segment in boundaryLoop)
            //    {
            //        // 获取曲线的起点和终点
            //        Curve curve = segment.GetCurve();
            //        XYZ startPoint = curve.GetEndPoint(0);
            //        XYZ endPoint = curve.GetEndPoint(1);
            //        // 更新最小点
            //        minPoint = new XYZ(
            //            Math.Min(minPoint.X, Math.Min(startPoint.X, endPoint.X)),
            //            Math.Min(minPoint.Y, Math.Min(startPoint.Y, endPoint.Y)),
            //            Math.Min(minPoint.Z, Math.Min(startPoint.Z, endPoint.Z))
            //        );
            //        // 更新最大点
            //        maxPoint = new XYZ(
            //            Math.Max(maxPoint.X, Math.Max(startPoint.X, endPoint.X)),
            //            Math.Max(maxPoint.Y, Math.Max(startPoint.Y, endPoint.Y)),
            //            //Math.Max(maxPoint.Z, Math.Max(startPoint.Z, endPoint.Z))
            //            double.MaxValue);
            //    }
            //}
            //// 设置边界框的最小点和最大点
            //boundingBox.Min = minPoint;
            //boundingBox.Max = maxPoint;
            ////TaskDialog.Show("tt", $"{boundingBox.Max.X.ToString("F2")}+{boundingBox.Max.Y.ToString("F2")}+{boundingBox.Max.Z.ToString("F2")}");
            ////TaskDialog.Show("tt", $"{boundingBox.Min.X.ToString("F2")}+{boundingBox.Min.Y.ToString("F2")}+{boundingBox.Min.Z.ToString("F2")}");
            //例程结束
            ////检查楼梯中心点是否在房间内也可以
            //var stair = doc.GetElement(new ElementId(1926218)) as Stairs;
            //bool isStairInRoom = IsAnyPartOfStairInRoom(stair, room, doc);
            //if (isStairInRoom)
            //{ TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 至少有一部分在房间 '{room.Name}' 内部。"); }
            //else { TaskDialog.Show("检查结果", $"楼梯 '{stair.Id}' 完全不在房间 '{room.Name}' 内部。"); }
            //例程结束
            return Result.Succeeded;
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
    }
}
