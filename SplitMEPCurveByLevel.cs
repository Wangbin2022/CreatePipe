using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class SplitMEPCurveByLevel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            try
            {
                List<Level> allLevelsInModel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level)).Cast<Level>().ToList();

                if (!allLevelsInModel.Any())
                {
                    TaskDialog.Show("错误", "模型中未找到任何标高。");
                    return Result.Cancelled;
                }
                // 提取唯一的标高名称用于显示，避免在下拉框中出现重名
                List<string> uniqueLevelNames = allLevelsInModel
                    .Select(item => item.Name).Distinct().OrderBy(name => name).ToList();
                // 创建并配置对话框实例
                UniversalComboBoxMultiSelection boxMultiSelection = new UniversalComboBoxMultiSelection(uniqueLevelNames, "请选择要切分标高，实体不要成组");
                boxMultiSelection.Title = "标高选择";
                bool? dialogResult = boxMultiSelection.ShowDialog();
                if (dialogResult == true)
                {
                    List<string> selectedLevelNames = boxMultiSelection.SelectedResult;
                    if (!selectedLevelNames.Any())
                    {
                        TaskDialog.Show("提示", "您点击了确认，但没有选择任何项。操作已取消。");
                        return Result.Cancelled;
                    }
                    // --- 步骤 3: 将选择的标高名称映射回 Level 对象 ---
                    // 这是关键的衔接步骤
                    List<Level> selectedLevels = allLevelsInModel
                        .Where(level => selectedLevelNames.Contains(level.Name))
                        .ToList();
                    // (可选) 如果存在同名标高，这里的逻辑会选择所有同名的。
                    // 如果只想处理唯一的标高，可以加上GroupBy
                    selectedLevels = selectedLevels.GroupBy(l => l.Elevation).Select(g => g.First())
                        .OrderBy(l => l.Elevation).ToList();
                    // --- 步骤 4: 执行核心的 MEPCurve 切分逻辑 ---
                    // 收集并分类MEPCurve
                    List<MEPCurve> allMepCurves = new FilteredElementCollector(doc)
                        .OfClass(typeof(MEPCurve)).Cast<MEPCurve>().ToList();
                    List<MEPCurve> verticalPipes = new List<MEPCurve>();
                    int slopedPipeCount = 0; // 修改计数器名称，更精确
                    foreach (var pipe in allMepCurves)
                    {
                        if (IsVertical(pipe))
                        {
                            verticalPipes.Add(pipe);
                        }
                        else if (!IsHorizontal(pipe)) // 如果不是垂直的，再判断它是不是水平的
                        {
                            // 既不垂直也不水平，就是倾斜管线
                            slopedPipeCount++;
                        }
                        // 水平管线则直接忽略
                    }
                    int verticalPipeCount = verticalPipes.Count;
                    int breakCount = 0;
                    double basePointZ = GetProjectBasePointZ(doc);

                    var failureHandler = new RobustFailureProcessor();
                    //using (var progressBar = new RevitWpfProgressBar(uiApp, "正在切分...", verticalPipes.Count))
                    //{
                    using (TransactionGroup transGroup = new TransactionGroup(doc, "批量打断立管"))
                    {
                        transGroup.Start();
                        // 外层循环：遍历每一根需要处理的垂直管线
                        foreach (var pipe in verticalPipes)
                        {
                            if (!pipe.IsValidObject) continue;

                            // 为当前管线找出所有需要打断的点，并从低到高排序
                            List<XYZ> breakPointsForThisPipe = selectedLevels.Select(level => level.Elevation + basePointZ)
                                .Where(breakZ => IsPointOnCurve(pipe, breakZ)).OrderBy(breakZ => breakZ)
                                .Select(breakZ => new XYZ((pipe.Location as LocationCurve).Curve.GetEndPoint(0).X, (pipe.Location as LocationCurve).Curve.GetEndPoint(0).Y, breakZ))
                                .ToList();

                            if (breakPointsForThisPipe.Any())
                            {
                                using (Transaction trans = new Transaction(doc, "打断单个MEPCurve"))
                                {
                                    FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                                    options.SetFailuresPreprocessor(failureHandler);
                                    trans.SetFailureHandlingOptions(options);
                                    trans.Start();
                                    try
                                    {
                                        // **核心：递归式循环**
                                        MEPCurve segmentToBreak = pipe; // 初始目标为原始管线
                                        foreach (var breakPoint in breakPointsForThisPipe)
                                        {
                                            if (segmentToBreak == null || !segmentToBreak.IsValidObject)
                                            {
                                                // 如果上一步打断失败，则终止对该管线的后续操作
                                                break;
                                            }
                                            // 调用您的自定义打断逻辑
                                            MEPCurve newUpperSegment = BreakPipeWithCustomLogic(doc, segmentToBreak, breakPoint);

                                            // **关键步骤**：将下一次操作的目标更新为新生成的“上半部分”
                                            segmentToBreak = newUpperSegment;
                                        }
                                        breakCount += breakPointsForThisPipe.Count;
                                        trans.Commit();
                                    }
                                    catch (Exception)
                                    {
                                        trans.RollBack();
                                    }
                                }
                            }
                        }
                        transGroup.Assimilate();
                    }
                    //}
                    string summaryMessage = $"已处理 {verticalPipeCount} 根机电立管/桥架。" +
                                          $"在 {selectedLevels.Count} 个选定标高上，共执行了 {breakCount} 次打断操作。";
                    if (slopedPipeCount > 0)
                    {
                        summaryMessage += $"\n注意：已跳过 {slopedPipeCount} 根非垂直管线未作处理。";
                    }
                    TaskDialog.Show("操作完成", summaryMessage);
                }
                else
                {
                    TaskDialog.Show("操作取消", "用户已取消操作。");
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message.ToString());
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        /// <summary>
        /// 使用您的“复制-修改”逻辑来打断一根MEPCurve。
        /// 此方法会修改传入的mepCurveToBreak，并返回新创建的另一半。
        /// </summary>
        /// <param name="doc">Revit文档</param>
        /// <param name="mepCurveToBreak">要打断的管段</param>
        /// <param name="breakPoint">打断点</param>
        /// <returns>新生成的“上半部分”管段，如果失败则返回null</returns>
        private MEPCurve BreakPipeWithCustomLogic(Document doc, MEPCurve mepCurveToBreak, XYZ breakPoint)
        {
            // 拷贝一根管
            ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mepCurveToBreak.Id, XYZ.Zero);
            ElementId newId = ids.FirstOrDefault();
            if (newId == null || newId == ElementId.InvalidElementId) return null;

            MEPCurve newUpperSegment = doc.GetElement(newId) as MEPCurve;

            // 获取原始管段的几何信息
            Curve originalCurve = (mepCurveToBreak.Location as LocationCurve).Curve;
            XYZ startXYZ = originalCurve.GetEndPoint(0);
            XYZ endXYZ = originalCurve.GetEndPoint(1);

            // 确保起点Z < 终点Z，简化逻辑
            if (startXYZ.Z > endXYZ.Z)
            {
                var temp = startXYZ;
                startXYZ = endXYZ;
                endXYZ = temp;
            }

            // 确保打断点在原始线上，以防浮点数误差
            XYZ projectedBreakPoint = originalCurve.Project(breakPoint).XYZPoint;

            // **修改原始管段** (下半部分)
            Line lowerLine = Line.CreateBound(startXYZ, projectedBreakPoint);
            (mepCurveToBreak.Location as LocationCurve).Curve = lowerLine;

            // **修改拷贝管段** (上半部分)
            Line upperLine = Line.CreateBound(projectedBreakPoint, endXYZ);
            (newUpperSegment.Location as LocationCurve).Curve = upperLine;

            // 您的连接器逻辑 - 这里做了简化和健壮性处理
            // 注意：这个逻辑非常依赖于特定的连接情况，可能需要根据实际模型调整
            // 它假设连接件在Z坐标较大的那一端
            Connector endConnector = GetConnectorAtPoint(mepCurveToBreak, endXYZ);
            if (endConnector != null && endConnector.IsConnected)
            {
                // 找到所有与之相连的连接器
                var connectedPartners = endConnector.AllRefs.Cast<Connector>().ToList();

                // 断开原始连接
                foreach (var partner in connectedPartners)
                {
                    endConnector.DisconnectFrom(partner);
                }

                // 在新管段的上端找到对应连接器并重新连接
                Connector newEndConnector = GetConnectorAtPoint(newUpperSegment, endXYZ);
                if (newEndConnector != null)
                {
                    foreach (var partner in connectedPartners)
                    {
                        newEndConnector.ConnectTo(partner);
                    }
                }
            }

            return newUpperSegment;
        }
        private Connector GetConnectorAtPoint(MEPCurve curve, XYZ point)
        {
            foreach (Connector conn in curve.ConnectorManager.Connectors)
            {
                if (conn.Origin.IsAlmostEqualTo(point, 0.001)) return conn;
            }
            return null;
        }
        private bool IsPointOnCurve(MEPCurve mepCurve, double breakZ)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve)) return false;
            Curve curve = locationCurve.Curve;
            double minZ = Math.Min(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z);
            double maxZ = Math.Max(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z);
            double tolerance = 0.0001;
            return breakZ > minZ + tolerance && breakZ < maxZ - tolerance;
        }
        /// <summary>
        /// 判断MEPCurve是否为垂直的
        /// </summary>
        private bool IsVertical(MEPCurve mepCurve)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve)) return false;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);
            double tolerance = 0.001;
            return Math.Abs(start.X - end.X) < tolerance && Math.Abs(start.Y - end.Y) < tolerance;
        }
        private bool IsHorizontal(MEPCurve mepCurve)
        {
            if (!(mepCurve.Location is LocationCurve locationCurve)) return false;
            Curve curve = locationCurve.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);
            double tolerance = 0.001; // 精度容差
                                      // 如果起点和终点的Z坐标几乎相等，则视为水平
            return Math.Abs(start.Z - end.Z) < tolerance;
        }
        /// <summary>
        /// 获取项目基点的Z坐标
        /// </summary>
        private double GetProjectBasePointZ(Document doc)
        {
            var basePoint = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
                .FirstOrDefault() as BasePoint;
            return basePoint?.Position.Z ?? 0.0;
        }
    }
}
