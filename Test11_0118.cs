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
using System.Windows.Forms;
//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test11_0118 : IExternalCommand
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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

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
}
