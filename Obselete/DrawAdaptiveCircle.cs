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
    //[Transaction(TransactionMode.Manual)]
    //public class DrawAdaptiveCircle : IExternalCommand
    //{
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;

    //        if (activeView.ViewType != ViewType.FloorPlan)
    //        {
    //            TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
    //            return Result.Failed;
    //        }
    //        List<string> routeNames = new List<string>() { "四点环形沟轮廓", "六点环形沟轮廓", "八点环形沟轮廓" };
    //        int pointNum = 0;
    //        FamilySymbol selectSymbol4p = null;
    //        FamilySymbol selectSymbol6p = null;
    //        FamilySymbol selectSymbol8p = null;
    //        string levelName = activeView.GenLevel.Name;
    //        var circleSymbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("环形沟轮廓")).ToList();
    //        if (circleSymbols.Count() != 3)
    //        {
    //            TaskDialog.Show("错误", "未找到指定的自适应族");
    //            return Result.Failed;
    //        }
    //        foreach (var circleSymbol in circleSymbols)
    //        {
    //            switch (circleSymbol.Name)
    //            {
    //                case ("四点环形沟轮廓"):
    //                    selectSymbol4p = circleSymbol;
    //                    break;
    //                case ("六点环形沟轮廓"):
    //                    selectSymbol6p = circleSymbol;
    //                    break;
    //                default:
    //                    selectSymbol8p = circleSymbol;
    //                    break;
    //            }
    //        }
    //        FamilySymbol selectSymbol = null;
    //        UniversalComboBoxSelection subView = null;
    //        Action<string> onSelected = selectedName =>
    //        {
    //            subView.ViewModel.IsCommandRunning = true;
    //            switch (selectedName)
    //            {
    //                case ("四点环形沟轮廓"):
    //                    pointNum = 4;
    //                    selectSymbol = selectSymbol4p;
    //                    break;
    //                case ("六点环形沟轮廓"):
    //                    pointNum = 6;
    //                    selectSymbol = selectSymbol6p;
    //                    break;
    //                default:
    //                    pointNum = 8;
    //                    selectSymbol = selectSymbol8p;
    //                    break;
    //            }
    //            _externalHandler.Run(app =>
    //            {
    //                // 1. 使用一个外层循环来允许重试点选过程
    //                while (true)
    //                {
    //                    List<XYZ> placementPoints = new List<XYZ>();
    //                    List<ElementId> tempLines = new List<ElementId>();
    //                    XYZ prevPoint = null;
    //                    bool isCancelled = false;
    //                    bool hasError = false;
    //                    // 2. 将点选过程放在一个独立的事务组中
    //                    using (TransactionGroup tg = new TransactionGroup(doc, "拾取点并创建临时线"))
    //                    {
    //                        tg.Start();
    //                        for (int i = 0; i < pointNum; i++)
    //                        {
    //                            try
    //                            {
    //                                XYZ point = app.ActiveUIDocument.Selection.PickPoint($"请选择第{i + 1}/{pointNum}个放置点 (按ESC取消)");
    //                                placementPoints.Add(point);
    //                                if (prevPoint != null)
    //                                {
    //                                    // 3. 为每个临时创建动作包裹一个子事务
    //                                    using (Transaction t = new Transaction(doc, "创建临时线"))
    //                                    {
    //                                        t.Start();
    //                                        Line line = Line.CreateBound(prevPoint, point);
    //                                        DetailLine detailLine = doc.Create.NewDetailCurve(activeView, line) as DetailLine;
    //                                        if (detailLine != null)
    //                                        {
    //                                            tempLines.Add(detailLine.Id);
    //                                        }
    //                                        t.Commit();
    //                                    }
    //                                }
    //                                prevPoint = point;
    //                            }
    //                            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
    //                            {
    //                                // 用户按下了ESC
    //                                TaskDialog.Show("取消", "操作已由用户取消。");
    //                                isCancelled = true;
    //                                break;
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                // 4. 捕获所有其他异常，例如“线太短”
    //                                TaskDialog.Show("错误", $"创建时发生错误: {ex.Message}请重新选择所有点。");
    //                                hasError = true;
    //                                break;
    //                            }
    //                        }
    //                        if (isCancelled || hasError)
    //                        {
    //                            // 如果有错误或取消，回滚整个事务组，所有临时线都会被撤销
    //                            tg.RollBack();
    //                        }
    //                        else
    //                        {
    //                            // 所有点都成功拾取
    //                            tg.Assimilate(); // 提交事务组
    //                        }
    //                    }
    //                    // 5. 根据点选结果决定下一步行动
    //                    if (isCancelled)
    //                    {
    //                        // 用户取消，跳出 while 循环，结束命令
    //                        break;
    //                    }
    //                    if (hasError)
    //                    {
    //                        // 发生错误，继续下一次 while 循环，即“回到起点”
    //                        continue;
    //                    }
    //                    // 6. 在一个全新的、独立的事务中创建最终的自适应构件
    //                    using (Transaction finalTrans = new Transaction(doc, "创建环形地沟轮廓"))
    //                    {
    //                        finalTrans.Start();
    //                        // 激活族类型
    //                        if (!selectSymbol.IsActive)
    //                        {
    //                            selectSymbol.Activate();
    //                            doc.Regenerate();
    //                        }
    //                        // 创建自适应族实例
    //                        FamilyInstance adaptiveInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, selectSymbol);
    //                        IList<ElementId> adaptivePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(adaptiveInstance);

    //                        // 移动自适应点到指定位置
    //                        for (int i = 0; i < pointNum; i++)
    //                        {
    //                            ReferencePoint adaptivePoint = doc.GetElement(adaptivePointIds[i]) as ReferencePoint;
    //                            adaptivePoint.Position = placementPoints[i];
    //                        }
    //                        // 删除临时线
    //                        if (tempLines.Any()) doc.Delete(tempLines);
    //                        finalTrans.Commit();
    //                    }
    //                    break; // 成功创建，跳出 while 循环，结束命令
    //                }
    //                subView.ViewModel.SetCommandCompleted();
    //            });
    //        };
    //        subView = new UniversalComboBoxSelection(routeNames, $"提示：选择环形地沟侧壁角点连线", onSelected);
    //        subView.IsModal = false;
    //        subView.Show();

    //        return Result.Succeeded;
    //    }
    //    private void DeleteTempLines(Document doc, List<ElementId> lineIds)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            using (Transaction trans = new Transaction(doc, "删除临时线"))
    //            {
    //                trans.Start();
    //                foreach (var id in lineIds)
    //                {
    //                    try
    //                    {
    //                        doc.Delete(id);
    //                    }
    //                    catch { }
    //                }
    //                trans.Commit();
    //            }
    //        });
    //    }
    //}
}
