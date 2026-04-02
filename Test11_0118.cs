using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Form.RevitStylePopup;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Annotations;
using System.Windows.Forms;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : IExternalCommand, INotifyPropertyChanged
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
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
        /// <summary>
        /// 兼容性获取构件的偏移参数
        /// </summary>
        //private double GetElementOffset(Element el)
        //{
        //    // 尝试获取常见的偏移参数 ID// 大多数基于标高的族（插座、家具、机械设备等）
        //    BuiltInParameter[] offsetParams = new[]
        //    {
        //        BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM,
        //        BuiltInParameter.WALL_BASE_OFFSET,
        //        BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM
        //    };
        //    foreach (var bip in offsetParams)
        //    {
        //        Parameter p = el.get_Parameter(bip);
        //        if (p != null && p.HasValue)
        //        {
        //            return p.AsDouble();
        //        }
        //    }
        //    return 0;
        //}
        /// <summary>
        /// 兼容性获取构件的标高ID
        /// </summary>
        private ElementId GetElementLevelId(Element el)
        {
            // 方式1：标准属性
            if (el.LevelId != ElementId.InvalidElementId) return el.LevelId;
            // 方式2：常用内置参数 (针对族实例、管道、风管等)
            Parameter p = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
            if (p != null && p.HasValue) return p.AsElementId();
            // 方式3：针对房间/空间
            p = el.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID);
            if (p != null && p.HasValue) return p.AsElementId();
            // 方式4：针对 MEP 管线特定偏移参数
            p = el.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
            if (p != null && p.HasValue) return p.AsElementId();
            return ElementId.InvalidElementId;
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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //0331 过滤器功能升级
            ViewFilterManagerView viewFilterManagerView = new ViewFilterManagerView(uiApp);
            viewFilterManagerView.Show();

            ////0331 集成错误处理测试报告.OK
            //try
            //{
            //    // 1. 实例化所有警告服务
            //    var roomWarningService = new RoomWarningService(doc);
            ////var duplicateMarkService = new DuplicateMarkWarningService(doc); // [新增]
            //    var genericWarningService = new GenericWarningService(doc);
            //    // 2. 定义哪些警告由特定服务处理，以便通用服务可以忽略它们
            //    var handledBySpecializedServices = new List<FailureDefinitionId>
            //    {
            //        BuiltInFailures.RoomFailures.RoomNotEnclosed,
            //        BuiltInFailures.RoomFailures.RoomsInSameRegionRooms
            ////BuiltInFailures.GeneralFailures.DuplicateValue // [新增] 告诉通用服务：这个我处理过了，你别管了
            //        // 未来可以添加更多，例如: BuiltInFailures.OverlapFailures.WallsOverlap
            //    };
            //    // 3. 运行分析
            //    RoomWarningAnalysisResult roomResult = roomWarningService.AnalyzeRoomWarnings();
            ////DuplicateMarkAnalysisResult duplicateResult = duplicateMarkService.AnalyzeWarnings(); // [新增]
            //    GenericWarningAnalysisResult genericResult = genericWarningService.AnalyzeGenericWarnings(handledBySpecializedServices);
            //    // 4. 汇总结果
            //    var allProblemElementIds = new HashSet<ElementId>();
            //    allProblemElementIds.UnionWith(roomResult.AllProblemRoomIds);
            ////allProblemElementIds.UnionWith(duplicateResult.DuplicateElementIds); // [新增]
            //    allProblemElementIds.UnionWith(genericResult.AllProblemElementIds);
            //    // 5. 构建并显示报告
            //    if (!roomResult.HasAnyWarnings && !genericResult.HasAnyWarnings)
            //    {
            //        TaskDialog.Show("模型健康检查", "恭喜！模型中未检测到任何已知类型的警告。");
            //        return Result.Succeeded;
            //    }
            //    var reportBuilder = new StringBuilder();
            //    reportBuilder.AppendLine("模型健康检查报告：\n");
            //    // 添加房间警告信息
            //    if (roomResult.HasAnyWarnings)
            //    {
            //        reportBuilder.AppendLine("--- 房间相关警告 ---");
            //        if (roomResult.UnenclosedRoomIds.Any())
            //            reportBuilder.AppendLine($"  - 房间不在闭合区域: {roomResult.UnenclosedRoomIds.Count} 个");
            //        if (roomResult.RoomsInSameRegionIds.Any())
            //            reportBuilder.AppendLine($"  - 多个房间位于同一区域: {roomResult.RoomsInSameRegionIds.Count} 个");
            //        reportBuilder.AppendLine();
            //    }
            ////// --- 报告：重复标记警告 [新增] ---
            ////if (duplicateResult.HasAnyWarnings)
            ////{
            ////    reportBuilder.AppendLine("--- 标识数据警告 ---");
            ////    reportBuilder.AppendLine($"  - 图元具有重复的标记/类型标记: {duplicateResult.DuplicateElementIds.Count} 个图元");
            ////    reportBuilder.AppendLine();
            ////}
            //    // 添加通用警告信息
            //    if (genericResult.HasAnyWarnings)
            //    {
            //        reportBuilder.AppendLine("--- 其他警告 ---");
            //        foreach (var kvp in genericResult.WarningsByDescription)
            //        {
            //            reportBuilder.AppendLine($"  - {kvp.Key}: 涉及 {kvp.Value.Count} 个元素");
            //        }
            //        reportBuilder.AppendLine();
            //    }
            //    reportBuilder.AppendLine($"总共发现 {allProblemElementIds.Count} 个有问题的元素。");
            //    reportBuilder.AppendLine("\n是否在视图中选中所有这些元素？");
            //    TaskDialogResult userResponse = TaskDialog.Show("模型健康检查", reportBuilder.ToString(),
            //                                                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            //    // 6. 根据用户响应执行操作
            //    if (userResponse == TaskDialogResult.Yes)
            //    {
            //        if (allProblemElementIds.Any())
            //        {
            //            uiDoc.Selection.SetElementIds(allProblemElementIds);
            //            TaskDialog.Show("操作完成", $"已成功选中 {allProblemElementIds.Count} 个有问题的元素。");
            //        }
            //        else
            //        {
            //            TaskDialog.Show("操作提示", "没有可供选择的问题元素。");
            //        }
            //    }
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = "执行错误检查时发生意外：" + ex.Message;
            //    return Result.Failed;
            //}       

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

            ////0331 检测房间不闭合区域警告.OK
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
            ////0331 批量改族类型方法，考虑封装一个类型转化方法
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

            ////////0323 房间管理过程版
            //RoomManagerView roomManagerView = new RoomManagerView(uiApp);
            //roomManagerView.ShowDialog();

            ////////0330 视图样板模块测试
            //ViewTemplateManagerView viewTemplateManagerView = new ViewTemplateManagerView(uiApp);
            //viewTemplateManagerView.Show();
            ////0330 视图模块测试
            //ViewManagerView viewManagerView = new ViewManagerView(uiApp);
            //viewManagerView.Show();
            //////var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate);
            //var views = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
            //List<ElementId> ids = new List<ElementId>();
            //foreach (var view in views)
            //{
            //    if (!(view.ViewTemplateId.IntegerValue == -1))
            //    {
            //        ids.Add(view.Id);
            //    }
            //}
            //TaskDialog.Show("tt", ids.Count().ToString());

            ////////0329 选择当前视图 中对象
            //SelectByWindowView selectByWindowView = new SelectByWindowView(uiApp);
            //selectByWindowView.Show();

            ////0329 选择同层构件，基于通用多选窗口UniversalComboBoxMultiSelection
            //// 1. 初始选择检查
            //ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //if (selectedIds.Count == 0)
            //{
            //    TaskDialog.Show("提示", "请先选择一批构件。");
            //    return Result.Cancelled;
            //}
            //// 2. 标高预处理：按高度分组并处理重叠逻辑
            //// Key: UI显示的名称, Value: 该高度对应的所有标高ID
            //Dictionary<string, List<ElementId>> levelMapping = new Dictionary<string, List<ElementId>>();
            //var levelGroups = new FilteredElementCollector(doc).OfClass(typeof(Level))
            //    .Cast<Level>().GroupBy(l => Math.Round(l.Elevation, 6)); // 精度处理，防止微小浮点误差
            //foreach (var group in levelGroups.OrderBy(g => g.Key))
            //{
            //    List<Level> levelsAtThisHeight = group.ToList();
            //    Level primary = levelsAtThisHeight.First();
            //    string displayName = primary.Name;
            //    // 如果存在重叠标高
            //    if (levelsAtThisHeight.Count > 1)
            //    {
            //        var others = levelsAtThisHeight.Skip(1).Select(l => l.Name);
            //        displayName += $" (重叠: {string.Join(", ", others)})";
            //    }
            //    // 存储映射关系
            //    levelMapping.Add(displayName, levelsAtThisHeight.Select(l => l.Id).ToList());
            //}
            //// 3. 弹出通用多选窗体
            //var dialog = new UniversalComboBoxMultiSelection(levelMapping.Keys.ToList(), "请选择目标楼层（同高已合并）：");
            //if (dialog.ShowDialog() != true) return Result.Cancelled;
            //// 4. 获取所有被选中的物理标高 ID (Flatten 所有的 List)
            //HashSet<ElementId> targetLevelIds = new HashSet<ElementId>();
            //foreach (string uiName in dialog.SelectedResult)
            //{
            //    if (levelMapping.ContainsKey(uiName))
            //    {
            //        foreach (var id in levelMapping[uiName]) targetLevelIds.Add(id);
            //    }
            //}
            //if (targetLevelIds.Count == 0) return Result.Cancelled;
            //// 5. 构件过滤逻辑
            //List<ElementId> finalIds = new List<ElementId>();
            //int noLevelCount = 0;
            //foreach (ElementId id in selectedIds)
            //{
            //    Element el = doc.GetElement(id);
            //    ElementId elLevelId = GetElementLevelId(el);
            //    if (elLevelId == ElementId.InvalidElementId)
            //    {
            //        noLevelCount++;
            //        continue;
            //    }
            //    // 核心判定：构件的标高ID是否在用户选择的“高度组”ID池中
            //    if (targetLevelIds.Contains(elLevelId))
            //    {
            //        finalIds.Add(id);
            //    }
            //}
            //// 6. 结果反馈
            //if (finalIds.Count > 0)
            //{
            //    uiDoc.Selection.SetElementIds(finalIds);
            //    string info = $"过滤完成！\n原始选中：{selectedIds.Count} 个\n属于目标楼层：{finalIds.Count} 个";
            //    if (noLevelCount > 0) info += $"\n排除无标高属性构件：{noLevelCount} 个";
            //    TaskDialog.Show("成功", info);
            //}
            //else
            //{
            //    TaskDialog.Show("结果", "所选构件均不属于指定的楼层。");
            //}

            ////// 1. 准备测试数据
            ////List<string> testData = new List<string> { "选项 A", "选项 B", "选项 C", "选项 D" };
            ////string prompt = "请选择需要处理的构件类型：";
            ////// 2. 实例化并显示窗体
            ////var dialog = new UniversalComboBoxMultiSelection(testData, prompt);
            ////// 3. 逻辑判断
            ////if (dialog.ShowDialog() == true)
            ////{
            ////    // 获取结果
            ////    var result = dialog.SelectedResult;
            ////    string msg = string.Join(", ", result);
            ////    TaskDialog.Show("测试成功", $"你选择了: {msg}");
            ////}
            ////else
            ////{
            ////    TaskDialog.Show("测试取消", "用户关闭了窗口或取消了操作。");
            ////}

            //////0329 关闭水暖系统的后台计算。OK
            //using (Transaction t = new Transaction(doc, "关闭机电系统计算"))
            //{
            //    t.Start();
            //    int ductChangedCount = 0;
            //    int pipeChangedCount = 0;
            //    // 1. 获取并修改所有【风管系统类型】
            //    var ductSystemTypes = new FilteredElementCollector(doc)
            //        .OfClass(typeof(MechanicalSystemType)).Cast<MechanicalSystemType>();
            //    foreach (MechanicalSystemType dst in ductSystemTypes)
            //    {
            //        var para = dst.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_CALCULATION_PARAM);
            //        para.Set(0);
            //        ductChangedCount++;
            //    }
            //    //// 2. 获取并修改所有【管道系统类型】
            //    var pipeSystemTypes = new FilteredElementCollector(doc)
            //        .OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>();
            //    foreach (var pst in pipeSystemTypes)
            //    {
            //        var para = pst.get_Parameter(BuiltInParameter.RBS_PIPE_SYSTEM_CALCULATION_PARAM);
            //        para.Set(0);
            //        pipeChangedCount++;
            //    }
            //    t.Commit();
            //    //// 可选：弹窗提示结果
            //    TaskDialog.Show("优化完成",
            //    $"成功关闭了计算：\n{ductChangedCount} 个风管系统类型\n{pipeChangedCount} 个管道系统类型\n\n现在修改机电管线将不会触发后台卡顿。");
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

            ////0329 视图上下无限设置
            ////一行验证
            //if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return Result.Cancelled;
            //var options = new List<string> { "向上无限 (Top Unlimited)", "向下无限 (Bottom Unlimited)", "上下无限 (Both Unlimited)", "恢复标准设置 (Reset)" };
            //int choice = TaskDialogHelper.ShowCommandLinks("平面视图范围快速切换", 2, "请选择视图可见性预设", "该操作将调整当前平面视图的 View Range 参数。", options);
            //// 如果用户点击右上角X或按ESC
            //if (choice == -1) return Result.Cancelled;
            //ViewPlan viewPlan = uiDoc.ActiveView as ViewPlan;
            //Level currentLevel = viewPlan.GenLevel;
            //using (Transaction tx = new Transaction(doc, "快速设置视图范围"))
            //{
            //    tx.Start();
            //    // 获取视图范围对象
            //    PlanViewRange viewRange = viewPlan.GetViewRange();
            //    if (choice == 0)
            //    {
            //        // 顶部和顶部偏移设为无限
            //        viewRange.SetLevelId(PlanViewPlane.TopClipPlane, PlanViewRange.Unlimited);
            //        viewRange.SetLevelId(PlanViewPlane.ViewDepthPlane, viewRange.GetLevelId(PlanViewPlane.BottomClipPlane)); // 深度保持与底一致
            //    }
            //    if (choice == 1)
            //    {
            //        // 底部和视图深度设为无限
            //        viewRange.SetLevelId(PlanViewPlane.BottomClipPlane, PlanViewRange.Unlimited);
            //        viewRange.SetLevelId(PlanViewPlane.ViewDepthPlane, PlanViewRange.Unlimited);
            //    }
            //    if (choice == 2)
            //    {
            //        viewRange.SetLevelId(PlanViewPlane.TopClipPlane, PlanViewRange.Unlimited);
            //        viewRange.SetLevelId(PlanViewPlane.BottomClipPlane, PlanViewRange.Unlimited);
            //        viewRange.SetLevelId(PlanViewPlane.ViewDepthPlane, PlanViewRange.Unlimited);
            //    }
            //    if (choice == 3)
            //    {
            //        // 1. 查找上一层标高
            //        Level nextLevel = new FilteredElementCollector(doc)
            //            .OfClass(typeof(Level))
            //            .Cast<Level>()
            //            .Where(l => l.Elevation > currentLevel.Elevation)
            //            .OrderBy(l => l.Elevation)
            //            .FirstOrDefault();
            //        double topOffset = 0;
            //        double cutPlaneOffset = 1200 / 304.8; // 默认 1200mm
            //        if (nextLevel != null)
            //        {
            //            double heightDiff = (nextLevel.Elevation - currentLevel.Elevation); // 内部单位 feet
            //            topOffset = heightDiff; // 顶部设在上一层标高位置（偏移0）
            //            // 逻辑：如果层高小于 1200mm (约3.93英尺)，剖切面设在中间
            //            if (heightDiff < cutPlaneOffset)
            //            {
            //                cutPlaneOffset = heightDiff / 2.0;
            //            }
            //        }
            //        else
            //        {
            //            // 如果没有上一层，顶部默认 2300mm
            //            topOffset = 2300 / 304.8;
            //        }
            //        // 设置恢复参数
            //        viewRange.SetLevelId(PlanViewPlane.TopClipPlane, currentLevel.Id);
            //        viewRange.SetOffset(PlanViewPlane.TopClipPlane, topOffset);
            //        viewRange.SetLevelId(PlanViewPlane.CutPlane, currentLevel.Id);
            //        viewRange.SetOffset(PlanViewPlane.CutPlane, cutPlaneOffset);
            //        viewRange.SetLevelId(PlanViewPlane.BottomClipPlane, currentLevel.Id);
            //        viewRange.SetOffset(PlanViewPlane.BottomClipPlane, 0.0);
            //        viewRange.SetLevelId(PlanViewPlane.ViewDepthPlane, currentLevel.Id);
            //        viewRange.SetOffset(PlanViewPlane.ViewDepthPlane, 0.0);
            //    }
            //    viewPlan.SetViewRange(viewRange);
            //    tx.Commit();
            //}

            //ManualCropZaxisView manualCropZaxisView = new ManualCropZaxisView(uiApp);
            //manualCropZaxisView.Show();

            ////0328 选择同位置构件,也是仅支持单个，需要验证名称，XY坐标与当前元素相同，仅Z值不同对象并选中，含原构件

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


            //0323 进度条调用模板，无需单独声明ProgressBar
            //            public void DeleteElements(IEnumerable<object> selectedItems)
            //{
            //    if (selectedItems == null) return;
            //    var toDeleteList = selectedItems.Cast<ViewEntity>().Where(e => e.Id != ActiveView.Id).ToList();
            //    if (toDeleteList.Count == 0) return;
            //    // 【关键】事务前缓存所有 ID
            //    var idsToDelete = toDeleteList.Select(e => e.Id).ToList();
            //    //需要加入批量处理进度条逻辑
            //    ExternalHandler.Run(app =>
            //    {
            //        TransactionWithProgressBarHelper.Execute(app.ActiveUIDocument.Document, "批量删除视图", (service) =>
            //        {
            //            service.UpdateMax(idsToDelete.Count());
            //            int index = 0;
            //            foreach (var id in idsToDelete)
            //            {
            //                service.Update(++index, app.ActiveUIDocument.Document.GetElement(id).Name);
            //                Document.Delete(id);
            //            }
            //        });
            //        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            foreach (var item in toDeleteList)
            //            {
            //                Collection.Remove(item);
            //            }
            //        });
            //    });
            // }

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
}
