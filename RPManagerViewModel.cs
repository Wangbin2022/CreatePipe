using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using Microsoft.Win32;
using NPOI.SS.Formula.PTG;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;



namespace CreatePipe
{
    public class RPManagerViewModel : ObserverableObject
    {
        public UIDocument uiDoc { get; set; }
        public Document Doc { get; set; }
        public int D { get; set; } = 300;
        private View View { get; set; }
        public RPManagerViewModel(UIApplication application)
        {
            uiDoc = application.ActiveUIDocument;
            Doc = application.ActiveUIDocument.Document;
            View = application.ActiveUIDocument.ActiveView;
        }
        public ICommand AllViewRPRemoveCommand => new BaseBindingCommand(AllViewRPRemove);
        private void AllViewRPRemove(object obj)
        {
            List<View> newViews = new FilteredElementCollector(Doc)
                .OfCategory(BuiltInCategory.OST_Views)
                .Cast<View>()
                .Where(view => view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection")
                .ToList();
            XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    int sum = 0;
                    foreach (var view in newViews)
                    {
                        List<ElementId> elemId = GetCurrentViewRP(view);
                        Doc.Delete(elemId);
                        sum += elemId.Count;
                    }
                    TaskDialog.Show("tt", $"已删除参照平面{sum}个");
                }, "删除参照平面");
            });
        }
        public ICommand AllViewRPHideCommand => new BaseBindingCommand(AllViewRPHide);
        private void AllViewRPHide(object obj)
        {
            List<View> newViews = new FilteredElementCollector(Doc)
                .OfCategory(BuiltInCategory.OST_Views)
                .Cast<View>()
                .Where(view => view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection")
                .ToList();
            XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    foreach (var view in newViews)
                    {
                        SingleViewHide(view);
                    }
                }, "开关参照平面显示");
            });
        }
        public ICommand CurrentViewRPExportCommand => new BaseBindingCommand(CurrentViewRPExport);
        private void CurrentViewRPExport(object obj)
        {
            if (isFuncableView(View))
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV 文件 (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = "ReferencePlanes_list.csv"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    string newPath = saveFileDialog.FileName;
                    using (var writer = new StreamWriter(newPath, false, System.Text.Encoding.UTF8))
                    {
                        //writer.WriteLine("面方向,,,起点,,,终点");
                        List<ReferencePlane> visibleReferencePlanes = new FilteredElementCollector(Doc)
                            .OfClass(typeof(ReferencePlane))
                            .Cast<ReferencePlane>()
                            .Where(rp => rp.CanBeVisibleInView(View))
                            .ToList();
                        foreach (var rpe in visibleReferencePlanes)
                        {
                            XYZ p1 = rpe.BubbleEnd;
                            XYZ p2 = rpe.FreeEnd;
                            XYZ xYZ = View.ViewDirection;
                            string p1s = GetXYZString(xYZ) + "," + GetXYZString(p1) + "," + GetXYZString(p2);
                            string[] values = p1s.Split(',');
                            writer.WriteLine($"{values[0]},{values[1]},{values[2]},{values[3]},{values[4]},{values[5]},{values[6]},{values[7]},{values[8]}");
                        }
                    }
                    TaskDialog.Show("tt", "已成功导出为 CSV 文件！");
                }
            }
            else TaskDialog.Show("tt", "不支持非平面视图导入导出，请切换到平面视图再试一下");
        }
        private string GetXYZString(XYZ xYZ)
        {
            string result = xYZ.X.ToString() + "," + xYZ.Y.ToString() + "," + xYZ.Z.ToString();
            return result;
        }
        public ICommand CurrentViewRPImportCommand => new BaseBindingCommand(CurrentViewRPImport);
        private void CurrentViewRPImport(object obj)
        {
            if (View.GetType().Name == "ViewPlan")
            {
                XmlDoc.Instance.Task.Run(app =>
                {
                    //先读取
                    OpenFileDialog opDialog = new OpenFileDialog();
                    opDialog.Title = "导入csv文件";
                    opDialog.Filter = "csv文件（*。csv）|*.csv";
                    opDialog.ShowDialog();
                    string csvPath = opDialog.FileName;
                    try
                    {
                        // 打开文件并创建 StreamReader 对象
                        using (FileStream fileStream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.UTF8))
                            {

                                Doc.NewTransaction(() =>
                                {
                                    int count = 0;
                                    while (!streamReader.EndOfStream)
                                    {
                                        string line = streamReader.ReadLine();
                                        string[] values = line.Split(',');
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            XYZ oriPoint = new XYZ(0, 0, 1);
                                            XYZ pt1 = new XYZ(Double.Parse(values[3]), Double.Parse(values[4]), Double.Parse(values[5]));
                                            XYZ pt2 = new XYZ(Double.Parse(values[6]), Double.Parse(values[7]), Double.Parse(values[8]));
                                            ReferencePlane rp1 = Doc.Create.NewReferencePlane(pt1, pt2, oriPoint, View);
                                            count++; // 确保只计数非空行
                                        }
                                    }
                                    TaskDialog.Show("tt", $"已导入参照平面{count}个");
                                }, "导入参照平面");

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("错误", $"发生异常: {ex.Message}");
                    }
                });
            }
            else TaskDialog.Show("tt", "不支持非平面视图导入导出，请切换到平面视图再试一下");
        }
        public ICommand CurrentViewRPRemoveCommand => new BaseBindingCommand(CurrentViewRPRemove);
        private void CurrentViewRPRemove(object obj)
        {
            if (isFuncableView(View))
            {
                List<ElementId> elemId = GetCurrentViewRP(View);
                XmlDoc.Instance.Task.Run(app =>
                {
                    Doc.NewTransaction(() =>
                    {
                        Doc.Delete(elemId);
                        TaskDialog.Show("tt", $"已删除参照平面{elemId.Count()}个");
                    }, "删除参照平面");
                });
            }
            else TaskDialog.Show("tt", "当前视图无参照平面，请切换到平立剖面视图再试");
        }
        public ICommand CurrentViewRPGetCommand => new BaseBindingCommand(CurrentViewRPGet);
        private void CurrentViewRPGet(object obj)
        {
            if (isFuncableView(View))
            {
                List<ElementId> elemId = GetCurrentViewRP(View);
                uiDoc.Selection.SetElementIds(elemId);
            }
            else TaskDialog.Show("tt", "当前视图无参照平面，请切换到平立剖面视图再试");
        }
        private List<ElementId> GetCurrentViewRP(View view)
        {
            List<ElementId> resultIds = new List<ElementId>();
            List<ReferencePlane> referencePlanes = new FilteredElementCollector(Doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
            foreach (ReferencePlane rp in referencePlanes)
            {
                if (rp.CanBeVisibleInView(view))
                {
                    resultIds.Add(rp.Id);
                }
            }
            return resultIds;
        }
        public ICommand CurrentViewRPHideCommand => new BaseBindingCommand(CurrentViewRPHide);
        private void CurrentViewRPHide(object obj)
        {
            View newView = View;
            if (isFuncableView(newView))
            {
                XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    SingleViewHide(newView);
                }, "开关参照平面显示");
            });
            }
            else TaskDialog.Show("tt", "当前视图无参照平面，请切换到平立剖面视图再试");
        }
        private void SingleViewHide(View newView)
        {
            if (newView.ViewTemplateId.IntegerValue != -1)
            {
                TaskDialog.Show("Title", "请关闭当前视图样板");
            }
            else
            {
                Categories cates = Doc.Settings.Categories;
                Category rpIn = cates.get_Item(BuiltInCategory.OST_CLines);
                if (newView.GetCategoryHidden(rpIn.Id))
                {
                    newView.SetCategoryHidden(rpIn.Id, false);
                }
                else
                {
                    newView.SetCategoryHidden(rpIn.Id, true);
                }
            }
        }
        public ICommand CreateRPCommand => new RelayCommand<string>(CreateReferencePlane);
        private void CreateReferencePlane(string num)
        {
            if (isFuncableView(View))
            {
                XmlDoc.Instance.Task.Run(app =>
                {
                    Doc.NewTransaction(() =>
                {
                    if (View.SketchPlane == null)
                    {
                        // 获取视图的视角方向（法线方向）
                        XYZ normal = View.ViewDirection;
                        // 定义工作平面的原点（通常使用视图的原点）
                        XYZ origin = View.Origin;
                        Plane workPlane = Plane.CreateByNormalAndOrigin(normal, origin);
                        View.SketchPlane = SketchPlane.Create(Doc, workPlane);
                    }
                    if (View.GetType().Name == "ViewPlan" || View.GetType().Name == "ViewSection")
                    {
                        XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
                        XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
                        XYZ direction = (pt2 - pt1).Normalize();
                        Curve curve1 = Line.CreateBound(pt1, pt2) as Curve;
                        XYZ cVec;
                        double tolerance = 1e-6;
                        // 计算垂直于视图方向的向量
                        if (Math.Abs(View.ViewDirection.X) < tolerance && Math.Abs(View.ViewDirection.Y) < tolerance && (View.ViewDirection.Z == 1 || View.ViewDirection.Z == -1))
                        {
                            cVec = new XYZ(0, 0, 1);
                        }
                        else if (Math.Abs(View.ViewDirection.X) < tolerance && Math.Abs(View.ViewDirection.Z) < tolerance && (View.ViewDirection.Y == 1 || View.ViewDirection.Y == -1))
                        {
                            cVec = new XYZ(0, 1, 0);
                        }
                        else if (Math.Abs(View.ViewDirection.Z) < tolerance && Math.Abs(View.ViewDirection.Y) < tolerance && (View.ViewDirection.X == 1 || View.ViewDirection.X == -1))
                        {
                            cVec = new XYZ(1, 0, 0);
                        }
                        else
                        {
                            throw new InvalidOperationException("两个端点的坐标不满足条件。");
                        }
                        if (Doc.IsFamilyDocument)
                        {
                            ReferencePlane rp1 = Doc.FamilyCreate.NewReferencePlane(pt1, pt2, cVec, View);
                        }
                        else { ReferencePlane rp1 = Doc.Create.NewReferencePlane(pt1, pt2, cVec, View); }
                        XYZ offsetDirection = getnormal(curve1, View);
                        double offsetDistance = D / 304.8;

                        if (Doc.IsFamilyDocument)
                        {
                            if (num == "A")
                            {
                                XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance;
                                XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance;
                                ReferencePlane rp2 = Doc.FamilyCreate.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
                            }
                            else
                            {
                                //计算偏移后的起点和终点
                                XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance / 2;
                                XYZ offsetPt3 = pt1 - offsetDirection * offsetDistance / 2;
                                XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance / 2;
                                XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance / 2;
                                ReferencePlane rp2 = Doc.FamilyCreate.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
                                ReferencePlane rp3 = Doc.FamilyCreate.NewReferencePlane(offsetPt3, offsetPt4, cVec, View);
                            }
                        }
                        else
                        {
                            if (num == "A")
                            {
                                XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance;
                                XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance;
                                ReferencePlane rp2 = Doc.Create.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
                            }
                            else
                            {
                                XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance / 2;
                                XYZ offsetPt3 = pt1 - offsetDirection * offsetDistance / 2;
                                XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance / 2;
                                XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance / 2;
                                ReferencePlane rp2 = Doc.Create.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
                                ReferencePlane rp3 = Doc.Create.NewReferencePlane(offsetPt3, offsetPt4, cVec, View);
                            }
                        }
                    }
                }, "画平行参照平面");
                });
            }
            else TaskDialog.Show("tt", "当前视图无法生成参照平面，请切换到平立剖面视图再试");
            //TaskDialog.Show("tt", D.ToString() + "+" + num);
        }
        private XYZ getnormal(Curve curve, Autodesk.Revit.DB.View view)
        {
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);
            double res = 100;
            double tolerance = 1e-6;
            if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.Z == 1 || view.ViewDirection.Z == -1))
            {
                XYZ t0 = new XYZ(p1.X - (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y + (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                XYZ t1 = new XYZ(p1.X + (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y - (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Y 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Z) < tolerance && (view.ViewDirection.Y == 1 || view.ViewDirection.Y == -1))
            {
                XYZ t0 = new XYZ(p1.X + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z - (p2.X - p1.X) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z + (p2.X - p1.X) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Z 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.Z) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.X == 1 || view.ViewDirection.X == -1))
            {
                XYZ t0 = new XYZ(p1.X, p1.Y - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z + (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X, p1.Y + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z - (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            else
            {
                throw new InvalidOperationException("两个端点的坐标不满足条件。");
            }
        }
        private bool isFuncableView(View view)
        {
            if (view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection")
            {
                return true;
            }
            return false;
        }
    }
}
