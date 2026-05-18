using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace CreatePipe
{

    internal class RoomRoofBoundaryChecker
    {
        private Application _application;
        private Document _document;
        public RoomRoofBoundaryChecker(ExternalCommandData commandData, ElementSet elements)
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var logPath = $"{assemblyLocation}.{DateTime.Now:yyyyMMdd}.log";
            string message = string.Empty;
            // 清理旧日志并创建新的Trace监听器
            if (File.Exists(logPath)) File.Delete(logPath);

            var txtListener = new TextWriterTraceListener(logPath);
            Trace.Listeners.Add(txtListener);

            try
            {
                _application = commandData.Application.Application;
                _document = commandData.Application.ActiveUIDocument.Document;

                // 搜索房间-屋顶关系
                FindRoomBoundingRoofs(ref message, elements);

                // 非回归模式下显示结果对话框
                if (commandData.JournalData.Count == 0)
                {
                    TaskDialog.Show("房间与屋顶", message);
                }

                //// 将结果写入JournalData用于回归测试
                //const string dataKey = "Results";
                //commandData.JournalData[dataKey] = message;
                TaskDialog.Show("tt", message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                message = ex.ToString(); return;
            }
            finally
            {
                Trace.Flush();
                Trace.Listeners.Remove(txtListener);
            }
        }
        /// <summary>
        /// 查找所有房间的边界屋顶 - 核心逻辑
        /// </summary>
        private void FindRoomBoundingRoofs(ref string message, ElementSet elements)
        {
            // 获取所有房间
            var rooms = GetRoomElements().ToList();
            if (!rooms.Any())
            {
                message = "未找到任何房间，请先创建房间！";
                return;
            }

            // 创建屋顶类别过滤器
            var categoryFilter = new LogicalOrFilter(
                new ElementCategoryFilter(BuiltInCategory.OST_Roofs),
                new ElementCategoryFilter(BuiltInCategory.OST_RoofSoffit));

            // 房间几何计算器
            var calculator = new SpatialElementGeometryCalculator(_document);

            // 存储房间与其边界屋顶的映射关系
            var roomsWithRoofs = new Dictionary<Element, List<ElementId>>();

            foreach (var room in rooms.OfType<SpatialElement>())
            {
                var geometryResults = calculator.CalculateSpatialElementGeometry(room);
                var solid = geometryResults.GetGeometry();

                // 遍历每个面查找边界屋顶
                foreach (Face face in solid.Faces)
                {
                    var boundaryFaces = geometryResults.GetBoundaryFaceInfo(face);
                    var foundRoof = FindFirstBoundaryRoof(boundaryFaces, categoryFilter);

                    if (foundRoof.IntegerValue > 0)
                    {
                        AddRoofToRoom(roomsWithRoofs, room, new ElementId(foundRoof.IntegerValue));
                        break; // 找到屋顶后跳出面循环
                    }
                }
            }

            // 格式化输出结果
            message = BuildResultMessage(roomsWithRoofs, rooms.ToList(), elements);
        }

        /// <summary>
        /// 在边界子面中查找第一个符合条件的屋顶
        /// </summary>
        private ElementId FindFirstBoundaryRoof(
            IList<SpatialElementBoundarySubface> boundaryFaces,
            LogicalOrFilter categoryFilter)
        {
            foreach (var boundaryFace in boundaryFaces)
            {
                var boundaryElementId = boundaryFace.SpatialBoundaryElement;
                var localElementId = boundaryElementId.HostElementId;

                if (localElementId != ElementId.InvalidElementId &&
                    categoryFilter.PassesFilter(_document, localElementId))
                {
                    return localElementId;
                }
            }
            return null;
        }

        /// <summary>
        /// 将屋顶添加到房间的映射中
        /// </summary>
        private static void AddRoofToRoom(
            Dictionary<Element, List<ElementId>> roomsWithRoofs,
            Element room,
            ElementId roofId)
        {
            if (roomsWithRoofs.TryGetValue(room, out var roofs))
            {
                if (!roofs.Contains(roofId))
                    roofs.Add(roofId);
            }
            else
            {
                roomsWithRoofs.Add(room, new List<ElementId> { roofId });
            }
        }

        /// <summary>
        /// 构建结果消息 - 使用字符串插值和LINQ
        /// </summary>
        private string BuildResultMessage(
            Dictionary<Element, List<ElementId>> roomsWithRoofs,
            List<Element> rooms,
            ElementSet elements)
        {
            var messageBuilder = new System.Text.StringBuilder();

            //// 有屋顶的房间
            //if (roomsWithRoofs.Any())
            //{
            //    messageBuilder.AppendLine("有边界屋顶的房间:");
            //    Trace.WriteLine("有边界屋顶的房间:");

            //    foreach (var (room, roofIds) in roomsWithRoofs)
            //    {
            //        // 从房间列表中移除（剩余的就是无屋顶的房间）
            //        rooms.Remove(room);

            //        var roofInfo = BuildRoofInfoString(roofIds);
            //        var log = $"  房间: Id = {room.Id.IntegerValue}, 名称 = {room.Name} --> {roofInfo}";
            //        messageBuilder.AppendLine(log);
            //        Trace.WriteLine(log);
            //    }
            //    messageBuilder.AppendLine();
            //}

            // 无屋顶的房间
            Trace.WriteLine("几何关系检查完成...");
            if (rooms.Any())
            {
                messageBuilder.AppendLine("以下房间没有边界屋顶:");
                Trace.WriteLine("以下房间没有边界屋顶:");

                foreach (var room in rooms)
                {
                    elements.Insert(room);
                    var log = $"  房间Id: {room.Id.IntegerValue}, 房间名称: {room.Name}";
                    messageBuilder.AppendLine(log);
                    Trace.WriteLine(log);
                }
            }

            return messageBuilder.ToString();
        }

        /// <summary>
        /// 构建屋顶信息字符串 - 使用字符串插值和LINQ
        /// </summary>
        private string BuildRoofInfoString(List<ElementId> roofIds)
        {
            if (roofIds.Count == 1)
            {
                var roof = _document.GetElement(roofIds[0]);
                return $"屋顶: Id = {roof.Id.IntegerValue}, 名称 = {roof.Name}";
            }

            var idsString = string.Join(", ", roofIds.Select(id => id.IntegerValue));
            return $"屋顶Ids = {idsString}";
        }

        /// <summary>
        /// 获取所有房间元素 - 使用LINQ简化
        /// </summary>
        private IEnumerable<Element> GetRoomElements()
        {
            var roomSpaceFilter = new LogicalOrFilter(new RoomFilter(), new SpaceFilter());
            return new FilteredElementCollector(_document)
                .WherePasses(roomSpaceFilter)
                .ToElements();
        }
    }
}
