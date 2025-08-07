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
    public class DrawEvacRouteFunc : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            if (activeView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
                return Result.Failed;
            }
            List<string> routeNames = new List<string>() { "两点确定路线", "三点确定路线", "四点确定路线", "五点确定路线" };
            int pointNum = 0;
            FamilySymbol selectSymbol2p = null;
            FamilySymbol selectSymbol3p = null;
            FamilySymbol selectSymbol4p = null;
            FamilySymbol selectSymbol5p = null;
            string levelName = activeView.GenLevel.Name;
            var routeSymbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(s => s.Name.Contains("确定路线")).ToList();
            if (routeSymbols.Count() != 4)
            {
                TaskDialog.Show("错误", "未找到指定的自适应族");
                return Result.Failed;
            }
            foreach (var routeSymbol in routeSymbols)
            {
                switch (routeSymbol.Name)
                {
                    case ("两点确定路线"):
                        selectSymbol2p = routeSymbol;
                        break;
                    case ("三点确定路线"):
                        selectSymbol3p = routeSymbol;
                        break;
                    case ("四点确定路线"):
                        selectSymbol4p = routeSymbol;
                        break;
                    default:
                        selectSymbol5p = routeSymbol;
                        break;
                }
            }
            FamilySymbol selectSymbol = null;
            UniversalComboBoxSelection subView = null;
            Action<string> onSelected = selectedName =>
            {
                subView.ViewModel.IsCommandRunning = true;
                switch (selectedName)
                {
                    case ("两点确定路线"):
                        pointNum = 2;
                        selectSymbol = selectSymbol2p;
                        break;
                    case ("三点确定路线"):
                        pointNum = 3;
                        selectSymbol = selectSymbol3p;
                        break;
                    case ("四点确定路线"):
                        pointNum = 4;
                        selectSymbol = selectSymbol4p;
                        break;
                    default:
                        pointNum = 5;
                        selectSymbol = selectSymbol5p;
                        break;
                }
                _externalHandler.Run(app =>
                {
                    List<ElementId> tempLines = new List<ElementId>();
                    XYZ prevPoint = null;
                    doc.NewTransaction(() =>
                    {
                        if (!selectSymbol.IsActive)
                        {
                            selectSymbol.Activate();
                            doc.Regenerate();
                        }
                        List<XYZ> placementPoints = new List<XYZ>();
                        for (int i = 0; i < pointNum; i++)
                        {
                            try
                            {
                                XYZ point = app.ActiveUIDocument.Selection.PickPoint($"请选择第{i + 1}个放置点");
                                XYZ point1 = new XYZ(point.X, point.Y, point.Z + 300 / 304.8);
                                placementPoints.Add(point1);

                                if (prevPoint != null)
                                {
                                    Line line = Line.CreateBound(prevPoint, point1);
                                    DetailLine detailLine = doc.Create.NewDetailCurve(activeView, line) as DetailLine;
                                    if (detailLine != null)
                                    {
                                        tempLines.Add(detailLine.Id);
                                    }
                                }
                                prevPoint = point1;
                            }
                            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                            {
                                TaskDialog.Show("取消", "用户取消了点选择");
                                DeleteTempLines(doc, tempLines);
                                return;
                            }
                        }
                        // 创建自适应族实例
                        FamilyInstance adaptiveInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, selectSymbol);
                        adaptiveInstance.LookupParameter("楼层标高").Set(levelName);
                        // 获取自适应点引用
                        IList<ElementId> adaptivePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(
                            adaptiveInstance);
                        // 移动自适应点到指定位置
                        for (int i = 0; i < pointNum; i++)
                        {
                            ReferencePoint adaptivePoint = doc.GetElement(adaptivePointIds[i]) as ReferencePoint;
                            adaptivePoint.Position = placementPoints[i];
                        }
                        DeleteTempLines(doc, tempLines);
                    }, "创建疏散计算线");
                    //bak
                    subView.ViewModel.SetCommandCompleted();
                });
            };
            subView = new UniversalComboBoxSelection(routeNames, $"提示：选择疏散路线由几点连线", onSelected);
            subView.IsModal = false;
            subView.Show();

            return Result.Succeeded;
        }
        private void DeleteTempLines(Document doc, List<ElementId> lineIds)
        {
            _externalHandler.Run(app =>
            {
                using (Transaction trans = new Transaction(doc, "删除临时线"))
                {
                    trans.Start();
                    foreach (var id in lineIds)
                    {
                        try
                        {
                            doc.Delete(id);
                        }
                        catch { }
                    }
                    trans.Commit();
                }
            });
        }
    }
}
