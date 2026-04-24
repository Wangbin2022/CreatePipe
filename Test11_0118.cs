using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.models;
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
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    public class StrucEntity
    {
        Document Document;
        public StrucEntity(Family family)
        {

        }
        public int Count { get; set; }

    }
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
        //族再生测试方法
        /// <summary>
        /// 写入结果摘要到日志
        /// </summary>
        private static void WriteResultSummary(StringBuilder logBuilder, IReadOnlyCollection<string> failedTypes, string logPath)
        {
            logBuilder.AppendLine();
            if (failedTypes.Any())
            {
                logBuilder.AppendLine($"结果: {failedTypes.Count} 个族类型再生失败！");
                logBuilder.AppendLine("失败类型列表:");
                foreach (var type in failedTypes)
                {
                    logBuilder.AppendLine($"  - {type}");
                }
            }
            else
            {
                logBuilder.AppendLine("结果: 所有族类型再生成功！");
            }
            logBuilder.AppendLine($"日志文件位置: {logPath}");
            // 使用 using 确保资源释放
            File.WriteAllText(logPath, logBuilder.ToString());
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


            //////0423 组实例测试.OK
            ////using (Transaction tx = new Transaction(doc, "组编辑"))
            ////{
            ////    tx.Start();
            ////    var gpInstance = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new GroupFilter(), "pick group")) as Group;
            ////    var ids = gpInstance.GetMemberIds();
            ////    TaskDialog.Show("tt", ids.Count.ToString());
            ////    ////分解组。OK
            ////    //var ids = gpInstance.UngroupMembers();
            ////    //uiDoc.Selection.SetElementIds(ids);
            ////    tx.Commit();
            ////}

            //// GroupType 是组的定义/模板，类似于族的类型
            //FilteredElementCollector typeCollector = new FilteredElementCollector(doc);
            //ICollection<Element> groupTypes = typeCollector.OfClass(typeof(GroupType)).ToList();
            //int groupInstanceCount = new FilteredElementCollector(doc).OfClass(typeof(Group)).GetElementCount();
            //int groupTypeCount = new FilteredElementCollector(doc).OfClass(typeof(GroupType)).GetElementCount();
            //string returnMessage = $"当前项目中存在:\n" +
            //                 $"• 组实例（Group）数量: {groupInstanceCount}\n" +
            //                 $"• 组类型（GroupType）数量: {groupTypeCount}";
            //TaskDialog.Show("组统计", returnMessage);
            //ICollection<Element> groupInstances = new FilteredElementCollector(doc).OfClass(typeof(Group)).ToList();
            //var groupTypeStats = groupInstances.Cast<Group>().GroupBy(g => g.GroupType.Id)
            //    .Select(g => new
            //    {
            //        GroupTypeId = g.Key,
            //        GroupTypeName = g.First().GroupType.Name,
            //        InstanceCount = g.Count(),
            //        Instances = g.ToList()
            //    }).OrderByDescending(g => g.InstanceCount);
            //StringBuilder report2 = new StringBuilder();
            //if (groupInstances.Any())
            //{
            //    foreach (var stat in groupTypeStats)
            //    {
            //        report2.AppendLine($"组名称: {stat.GroupTypeName}");
            //        report2.AppendLine($"组ID: {stat.GroupTypeId.IntegerValue}");
            //        report2.AppendLine($"组实例数: {stat.InstanceCount}");
            //        // 获取第一个实例进行分析
            //        var firstInstance = stat.Instances.FirstOrDefault();
            //        if (firstInstance != null)
            //        {
            //            var memberIds = firstInstance.GetMemberIds();
            //            report2.AppendLine($"实例内元素数量: {memberIds.Count}");
            //            // ========== 补充：检查是否有嵌套组 ==========
            //            bool hasNestedGroup = false;
            //            int nestedGroupCount = 0;
            //            List<string> nestedGroupNames = new List<string>();
            //            foreach (ElementId id in memberIds)
            //            {
            //                Element member = doc.GetElement(id);
            //                // 检查成员是否为 Group 类型（嵌套组）
            //                if (member is Group nestedGroup)
            //                {
            //                    hasNestedGroup = true;
            //                    nestedGroupCount++;
            //                    string nestedName = nestedGroup.GroupType?.Name ?? "未命名组";
            //                    if (!nestedGroupNames.Contains(nestedName))
            //                    {
            //                        nestedGroupNames.Add(nestedName);
            //                    }
            //                }
            //            }
            //            // 输出嵌套组信息
            //            if (hasNestedGroup)
            //            {
            //                report2.AppendLine($" ⚠️ 包含嵌套组: 是");
            //                report2.AppendLine($"嵌套组数量: {nestedGroupCount} 个");
            //                report2.AppendLine($"嵌套组类型: {string.Join(", ", nestedGroupNames)}");
            //                report2.AppendLine();
            //            }
            //            else
            //            {
            //                report2.AppendLine($"✓ 包含嵌套组: 否");
            //                report2.AppendLine();
            //            }
            //        }
            //    }
            //    TaskDialog.Show("组ID统计", report2.ToString());
            //}
            //var unusedGroupTypes = groupTypes.Cast<GroupType>().Where(gt => gt.Groups.Size == 0).ToList();
            //StringBuilder report = new StringBuilder();
            //if (unusedGroupTypes.Any())
            //{
            //    report.AppendLine("--- 未使用的组类型（无实例） ---");
            //    foreach (GroupType gt in unusedGroupTypes)
            //    {
            //        report.AppendLine($"  - {gt.Name} (ID: {gt.Id.IntegerValue})");
            //    }
            //    report.AppendLine();
            //    TaskDialog.Show("组统计", report.ToString());
            //}

            ////0423 批量验证Revit族文档中所有族类型的有效性 官方程序 意义不明
            //// 验证是否为族文档
            //if (!doc.IsFamilyDocument)
            //{
            //    message = "请在族文档中运行此命令！";
            //    return Result.Failed;
            //}
            //string LogFileName = "RegenerationLog.txt";
            //var familyManager = doc.FamilyManager;
            //var assemblyPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //var logPath = Path.Combine(assemblyPath ?? Environment.CurrentDirectory, LogFileName);
            //// 使用 StringBuilder 批量写入日志，提高效率
            //var logBuilder = new StringBuilder();
            //logBuilder.AppendLine("Family Type     Result");
            //logBuilder.AppendLine("-------------------------");
            //var failedTypes = new List<string>();
            //// 遍历所有族类型
            //foreach (FamilyType type in familyManager.Types)
            //{
            //    var typeName = type.Name?.Trim();
            //    // 跳过空名称的类型
            //    if (string.IsNullOrEmpty(typeName)) continue;
            //    try
            //    {
            //        // 切换当前类型，触发再生
            //        familyManager.CurrentType = type;
            //        logBuilder.AppendLine($"{typeName,-14} Successful");
            //    }
            //    catch (Exception ex)
            //    {
            //        failedTypes.Add(typeName);
            //        logBuilder.AppendLine($"{typeName,-14} Failed - {ex.Message}");
            //    }
            //}
            //// 写入最终结果
            //WriteResultSummary(logBuilder, failedTypes, logPath);
            //////显示结果（无阻塞对话框，可查看日志）
            //////ShowResult(failedTypes, logPath);
            //var returnMessage = failedTypes.Any() ? $"❌ {failedTypes.Count} 个类型再生失败！\n\n详情请查看日志:\n{logPath}" : $"✅ 所有 {failedTypes.Count} 个类型再生成功！";
            //TaskDialog.Show("族类型验证结果", returnMessage);


            //////0422 结构测试.OK
            //StructuralElementManagerView structuralElementManagerView = new StructuralElementManagerView(uiApp);
            //structuralElementManagerView.Show();

            //var column = doc.GetElement(uiDoc.Selection.PickObject(ObjectType.Element, new ColumnFilter(), "选个柱子")) as FamilyInstance;
            ////TaskDialog.Show("tt", column.Id.IntegerValue.ToString());
            ////TaskDialog.Show("tt", (column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble() * 304.8 / 1000).ToString());
            ////TaskDialog.Show("tt", (column.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble() * 304.8 * 304.8 * 304.8 / (1000 * 1000 * 1000)).ToString());

            ////0421 明细表逗号检查替换测试，ds版本
            //Autodesk.Revit.DB.View schedule = doc.ActiveView;
            //// 确保当前视图是明细表
            //if (!(schedule is ViewSchedule))
            //{
            //    TaskDialog.Show("错误", "请在明细表视图中运行此命令。");
            //    return Result.Cancelled;
            //}
            //// 获取明细表数据
            //TableData tableData = (schedule as ViewSchedule).GetTableData();
            //TableSectionData sectionData = tableData.GetSectionData(SectionType.Body);
            //int nRows = sectionData.NumberOfRows;
            //int nCols = sectionData.NumberOfColumns;
            //var errorCells = new List<string>();
            //for (int row = 0; row < nRows; row++)
            //{
            //    for (int col = 0; col < nCols; col++)
            //    {
            //        string cellValue = sectionData.GetCellText(row, col);
            //        if (!string.IsNullOrEmpty(cellValue) && cellValue.Contains(","))
            //        {
            //            errorCells.Add($"行{row + 1},列{col + 1}: {cellValue}");
            //        }
            //    }
            //}
            //if (errorCells.Any())
            //{
            //    string msg = $"发现 {errorCells.Count} 个单元格包含半角逗号：\n{string.Join("\n", errorCells.Take(20))}";
            //    TaskDialog.Show("检查结果", msg);
            //}
            //else
            //{
            //    TaskDialog.Show("检查结果", "未发现半角逗号。");
            //}

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

            //////0417 线形图案清理.OK
            //FilteredElementCollector elements3 = new FilteredElementCollector(doc);
            //List<LinePatternElement> linePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            //// 准备显示用的字符串列表（显示线型图案名称）
            //List<string> linePatternNames = linePatternElements.Select(lpe => lpe.Name).ToList();
            //// 创建多选窗体
            //UniversalComboBoxMultiSelection selectionDialog = new UniversalComboBoxMultiSelection(
            //    linePatternNames, "请选择要删除的线型图案（支持多选）");
            //// 显示窗体并等待用户确认
            //bool? result = selectionDialog.ShowDialog();
            //// 处理选择结果
            //if (result == true && selectionDialog.SelectedResult.Any())
            //{
            //    // 根据用户选择的名称，获取对应的 LinePatternElement 对象
            //    List<LinePatternElement> selectedLinePatterns = linePatternElements
            //        .Where(lpe => selectionDialog.SelectedResult.Contains(lpe.Name)).ToList();
            //    TransactionWithProgressBarHelper.Execute(doc, "删除线型", (service) =>
            //    {
            //        service.UpdateMax(selectedLinePatterns.Count());
            //        int index = 0;
            //        foreach (var item in selectedLinePatterns)
            //        {
            //            service.Update(++index, item.Name);
            //            doc.Delete(item.Id);
            //        }
            //    });
            //    //// 输出选择结果
            //    TaskDialog.Show("选择结果", $"已删除 {selectedLinePatterns.Count} 个线型图案。");
            //}
            //else
            //{
            //    TaskDialog.Show("提示", "未选择任何线型图案或已取消");
            //}

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

            //////0413 机电坡度检查.OK
            //MEPSlopeCheckView mEPSlopeCheckView = new MEPSlopeCheckView(uiApp);
            //mEPSlopeCheckView.Show();
            ////////0323 房间管理过程版
            //RoomManagerView roomManagerView = new RoomManagerView(uiApp);
            //roomManagerView.ShowDialog();
            /////剪贴文本暂存器。OK 注意需要非模态运行
            //ClipboardCatcher clipboard = new ClipboardCatcher();
            //clipboard.Show();

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
