using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class ErrorHandling : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            ////0331 集成错误处理测试报告.OK
            try
            {
                // 1. 实例化所有警告服务
                var roomWarningService = new RoomWarningService(doc);
                var duplicateMarkService = new DuplicateMarkWarningService(doc); // [新增]
                var genericWarningService = new GenericWarningService(doc);
                // 2. 定义哪些警告由特定服务处理，以便通用服务可以忽略它们
                var handledBySpecializedServices = new List<FailureDefinitionId>
                {
                    BuiltInFailures.RoomFailures.RoomNotEnclosed,
                    BuiltInFailures.RoomFailures.RoomsInSameRegionRooms,
                    BuiltInFailures.GeneralFailures.DuplicateValue  
                    // 未来可以添加更多，例如: BuiltInFailures.OverlapFailures.WallsOverlap
                };
                // 3. 运行分析
                RoomWarningAnalysisResult roomResult = roomWarningService.AnalyzeRoomWarnings();
                DuplicateMarkAnalysisResult duplicateResult = duplicateMarkService.AnalyzeWarnings();
                GenericWarningAnalysisResult genericResult = genericWarningService.AnalyzeGenericWarnings(handledBySpecializedServices);
                // 4. 汇总结果
                var allProblemElementIds = new HashSet<ElementId>();
                allProblemElementIds.UnionWith(roomResult.AllProblemRoomIds);
                allProblemElementIds.UnionWith(duplicateResult.DuplicateElementIds);
                allProblemElementIds.UnionWith(genericResult.AllProblemElementIds);
                // 5. 构建并显示报告
                if (!roomResult.HasAnyWarnings && !genericResult.HasAnyWarnings)
                {
                    TaskDialog.Show("模型健康检查", "恭喜！模型中未检测到任何已知类型的警告。");
                    return Result.Succeeded;
                }
                var reportBuilder = new StringBuilder();
                reportBuilder.AppendLine("模型健康检查报告：\n");
                // 添加房间警告信息
                if (roomResult.HasAnyWarnings)
                {
                    reportBuilder.AppendLine("--- 房间相关警告 ---");
                    if (roomResult.UnenclosedRoomIds.Any())
                        reportBuilder.AppendLine($"  - 房间不在闭合区域: {roomResult.UnenclosedRoomIds.Count} 个");
                    if (roomResult.RoomsInSameRegionIds.Any())
                        reportBuilder.AppendLine($"  - 多个房间位于同一区域: {roomResult.RoomsInSameRegionIds.Count} 个");
                    reportBuilder.AppendLine();
                }
                //// --- 报告：重复标记警告 [新增] ---
                if (duplicateResult.HasAnyWarnings)
                {
                    reportBuilder.AppendLine("--- 标识数据警告 ---");
                    reportBuilder.AppendLine($"  - 图元具有重复的标记/类型标记: {duplicateResult.DuplicateElementIds.Count} 个图元");
                    reportBuilder.AppendLine();
                }
                // 添加通用警告信息
                if (genericResult.HasAnyWarnings)
                {
                    reportBuilder.AppendLine("--- 其他警告 ---");
                    foreach (var kvp in genericResult.WarningsByDescription)
                    {
                        reportBuilder.AppendLine($"  - {kvp.Key}: 涉及 {kvp.Value.Count} 个元素");
                    }
                    reportBuilder.AppendLine();
                }
                reportBuilder.AppendLine($"总共发现 {allProblemElementIds.Count} 个有问题的元素。");
                reportBuilder.AppendLine("\n是否在视图中选中所有这些元素？");
                TaskDialogResult userResponse = TaskDialog.Show("模型健康检查", reportBuilder.ToString(),
                                                                TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
                // 6. 根据用户响应执行操作
                if (userResponse == TaskDialogResult.Yes)
                {
                    if (allProblemElementIds.Any())
                    {
                        uiDoc.Selection.SetElementIds(allProblemElementIds);
                        TaskDialog.Show("操作完成", $"已成功选中 {allProblemElementIds.Count} 个有问题的元素。");
                    }
                    else
                    {
                        TaskDialog.Show("操作提示", "没有可供选择的问题元素。");
                    }
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "执行错误检查时发生意外：" + ex.Message;
                return Result.Failed;
            }

            ////0331 检测多个房间位于同一闭合区域中警告。OK
            //try
            //{
            //    // 1. 定义我们要查找的警告类型 ID
            //    FailureDefinitionId roomsInSameRegionId = BuiltInFailures.RoomFailures.RoomsInSameRegionRooms;
            //    // 2. 获取文档中所有未解决的警告
            //    IList<FailureMessage> allWarnings = doc.GetWarnings();
            //    // 3. 过滤出“多个房间位于同一闭合区域中”警告
            //    IEnumerable<FailureMessage> roomsInSameRegionWarnings = allWarnings
            //        .Where(w => w.GetFailureDefinitionId() == roomsInSameRegionId);
            //    // 4. 收集所有受影响的房间 ID
            //    HashSet<ElementId> roomsToSelect = new HashSet<ElementId>();
            //    foreach (FailureMessage warning in roomsInSameRegionWarnings)
            //    {
            //        // GetFailingElements() 会返回与此警告相关的所有元素。
            //        // 对于 RoomsInTheSameRegion 警告，它返回的就是导致冲突的所有房间的 ID。
            //        foreach (ElementId failingElementId in warning.GetFailingElements())
            //        {
            //            // 确保这个 ID 确实代表一个房间 (可选但推荐的验证)
            //            Element elem = doc.GetElement(failingElementId);
            //            if (elem != null && elem is Room) // 使用我们定义的 Room 别名
            //            {
            //                roomsToSelect.Add(failingElementId);
            //            }
            //        }
            //    }
            //    // 5. 将这些房间在 UI 中选中
            //    if (roomsToSelect.Any())
            //    {
            //        uiDoc.Selection.SetElementIds(roomsToSelect);
            //        TaskDialog.Show("警告处理",
            //                        $"已发现并选中 {roomsToSelect.Count} 个房间，它们存在“多个房间位于同一闭合区域”的警告。\n" +
            //                        "这些房间分布在不同的警告组中，请在视图中查看并修正它们（通常需要添加房间分隔符）。");
            //    }
            //    else
            //    {
            //        TaskDialog.Show("警告处理", "模型中没有发现“多个房间位于同一闭合区域”的警告。");
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = "操作失败：" + ex.Message;
            //    return Result.Failed;
            //}

            //////0331 检测房间不闭合区域警告.OK
            //try
            //{
            //    // 1. 定义我们要查找的警告类型 ID
            //    FailureDefinitionId roomNotEnclosedId = BuiltInFailures.RoomFailures.RoomNotEnclosed;
            //    // 2. 获取文档中所有未解决的警告
            //    IList<FailureMessage> allWarnings = doc.GetWarnings();
            //    // 3. 过滤出“房间不在完全闭合的区域”警告
            //    IEnumerable<FailureMessage> unenclosedRoomWarnings = allWarnings
            //        .Where(w => w.GetFailureDefinitionId() == roomNotEnclosedId);
            //    // 4. 收集所有受影响的房间 ID
            //    HashSet<ElementId> unenclosedRoomIds = new HashSet<ElementId>();
            //    foreach (FailureMessage warning in unenclosedRoomWarnings)
            //    {
            //        // GetFailingElements() 会返回与此警告相关的所有元素。
            //        // 对于 RoomNotEnclosed 警告，它返回的就是房间的 ID。
            //        foreach (ElementId failingElementId in warning.GetFailingElements())
            //        {
            //            // 确保这个 ID 确实代表一个房间 (可选但推荐的验证)
            //            Element elem = doc.GetElement(failingElementId);
            //            if (elem != null && elem is Autodesk.Revit.DB.Architecture.Room)
            //            {
            //                unenclosedRoomIds.Add(failingElementId);
            //            }
            //        }
            //    }
            //    // 5. 将这些房间在 UI 中选中
            //    if (unenclosedRoomIds.Any())
            //    {
            //        uiDoc.Selection.SetElementIds(unenclosedRoomIds);
            //        TaskDialog.Show("警告处理",
            //                        $"已发现并选中 {unenclosedRoomIds.Count} 个房间，它们存在“不在完全闭合区域”的警告。\n" +
            //                        "请在视图中查看并修复它们。");
            //    }
            //    else
            //    {
            //        TaskDialog.Show("警告处理", "模型中没有发现“房间不在完全闭合区域”的警告。");
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = "操作失败：" + ex.Message;
            //    return Result.Failed;
            //}
            ////0331 删除重叠构件方法.OK
            //try
            //{
            //    // 1. 实例化服务
            //    var warningService = new WarningManagerService(doc);
            //    // 2. 收集目标警告 (这里以“完全相同的重复实例”为例)
            //    var targetWarningId = BuiltInFailures.OverlapFailures.DuplicateInstances;
            //    var duplicateWarnings = warningService.GetWarningsByType(targetWarningId);
            //    if (duplicateWarnings.Count == 0)
            //    {
            //        TaskDialog.Show("检查结果", "模型很干净，没有发现重复实例的警告。");
            //        return Result.Succeeded;
            //    }
            //    // 3. 交给 Service 进行分析，找出该删的 ID
            //    OverlapAnalysisResult analysisResult = warningService.AnalyzeOverlaps(duplicateWarnings);
            //    if (!analysisResult.HasOverlaps)
            //    {
            //        TaskDialog.Show("检查结果", "虽然存在警告，但无需删除任何构件。");
            //        return Result.Succeeded;
            //    }
            //    // 4. UI 交互：请求用户确认
            //    string prompt = $"发现 {analysisResult.TotalWarningsAnalyzed} 个重复实例警告。\n\n" +
            //                    $"分析结果：\n" +
            //                    $"- 保留构件数: {analysisResult.ElementsToKeep.Count}\n" +
            //                    $"- 待删除多余构件: {analysisResult.ElementsToDelete.Count}\n\n" +
            //                    $"是否立即清理？";
            //    TaskDialogResult userResponse = TaskDialog.Show("确认清理", prompt, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            //    if (userResponse == TaskDialogResult.Yes)
            //    {
            //        // 5. 执行清理
            //        int deletedCount = warningService.ExecuteCleanup(analysisResult.ElementsToDelete);
            //        //TaskDialog.Show("清理完成", $"成功删除了 {deletedCount} 个多余构件！");
            //        TaskDialog.Show("清理完成", $"成功删除了 {analysisResult.ElementsToDelete.Count} 个多余构件！");
            //    }
            //    else
            //    {
            //        TaskDialog.Show("已取消", "清理操作已取消。");
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}
            return Result.Succeeded;
        }
        }
}
