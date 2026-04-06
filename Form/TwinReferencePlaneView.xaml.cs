using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// TwinReferencePlaneView.xaml 的交互逻辑
    /// </summary>
    public partial class TwinReferencePlaneView : Window
    {
        public TwinReferencePlaneView(UIApplication application)
        {
            InitializeComponent();
            this.DataContext = new TwinReferencePlaneViewModel(application);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class TwinReferencePlaneViewModel : ObserverableObject
    {
        public UIDocument uiDoc { get; set; }
        public Document Doc { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        public int D { get; set; } = 300;
        private View activeView { get; set; }
        public TwinReferencePlaneViewModel(UIApplication application)
        {
            uiDoc = application.ActiveUIDocument;
            Doc = application.ActiveUIDocument.Document;
            activeView = application.ActiveUIDocument.ActiveView;
        }
        private bool isFuncableView(View view)
        {
            // 使用枚举替代字符串判断，更安全，支持立面
            return view.ViewType == ViewType.FloorPlan ||
                   view.ViewType == ViewType.CeilingPlan ||
                   view.ViewType == ViewType.Section ||
                   view.ViewType == ViewType.Elevation;
        }
        public ICommand AllViewRPRemoveCommand => new BaseBindingCommand(AllViewRPRemove);
        private void AllViewRPRemove(object obj)
        {
            List<View> newViews = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Views).Cast<View>()
                .Where(view => view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection").ToList();
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "删除参照平面", () =>
                {
                    int sum = 0;
                    foreach (var view in newViews)
                    {
                        List<ElementId> elemId = GetCurrentViewRP(view);
                        Doc.Delete(elemId);
                        sum += elemId.Count;
                    }
                    TaskDialog.Show("tt", $"已删除参照平面{sum}个");
                });
            });
        }
        public ICommand AllViewRPHideCommand => new BaseBindingCommand(AllViewRPHide);
        private void AllViewRPHide(object obj)
        {
            List<View> newViews = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Views).Cast<View>()
                .Where(view => view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection").ToList();
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "开关参照平面显示", () =>
                {
                    foreach (var view in newViews)
                    {
                        SingleViewHide(view);
                    }
                });
            });
        }
        public ICommand CurrentViewRPExportCommand => new BaseBindingCommand(CurrentViewRPExport);
        private void CurrentViewRPExport(object obj)
        {
            if (!isFuncableView(activeView))
            {
                TaskDialog.Show("提示", "不支持非平立剖面视图导出，请切换视图再试一下");
                return;
            }
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "ReferencePlanes_list.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 1. 收集当前视图可见的参照平面
                    var referencePlanes = new FilteredElementCollector(Doc, activeView.Id).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
                    // 2. 将数据转换为 IEnumerable<string[]> 的格式
                    var exportData = referencePlanes.Select(rp => new string[]
                    {
                        activeView.ViewDirection.X.ToString(), activeView.ViewDirection.Y.ToString(), activeView.ViewDirection.Z.ToString(),
                        rp.BubbleEnd.X.ToString(), rp.BubbleEnd.Y.ToString(), rp.BubbleEnd.Z.ToString(),
                        rp.FreeEnd.X.ToString(), rp.FreeEnd.Y.ToString(), rp.FreeEnd.Z.ToString()
                    }).ToList();
                    // 3. 调用自定义的 CsvHelper 一键覆写写入
                    CsvHelper csvHelper = new CsvHelper(saveFileDialog.FileName);
                    csvHelper.WriteAll(exportData);
                    TaskDialog.Show("成功", $"已成功导出 {exportData.Count} 个参照平面！");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"导出失败: {ex.Message}");
                }
            }
        }
        private string GetXYZString(XYZ xYZ)
        {
            string result = xYZ.X.ToString() + "," + xYZ.Y.ToString() + "," + xYZ.Z.ToString();
            return result;
        }
        public ICommand CurrentViewRPImportCommand => new BaseBindingCommand(CurrentViewRPImport);
        private void CurrentViewRPImport(object obj)
        {
            if (!isFuncableView(activeView))
            {
                TaskDialog.Show("提示", "不支持非平立剖面视图导入，请切换视图再试一下");
                return;
            }
            var opDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "导入csv文件",
                Filter = "CSV文件 (*.csv)|*.csv",
                Multiselect = false
            };
            if (opDialog.ShowDialog() != true) return;
            ExternalHandler.Run(app =>
            {
                try
                {
                    // 1. 调用自定义的 CsvHelper 一键读取所有行，直接返回 List<string[]>
                    CsvHelper csvHelper = new CsvHelper(opDialog.FileName);
                    List<string[]> rows = csvHelper.ReadAll();
                    if (rows == null || rows.Count == 0)
                    {
                        TaskDialog.Show("提示", "CSV 文件为空或没有有效数据。");
                        return;
                    }
                    NewTransaction.Execute(Doc, "导入参照平面", () =>
                    {
                        int count = 0;
                        // 2. 遍历数据并创建参照平面
                        foreach (string[] values in rows)
                        {
                            // 长度防错保护：确保这行数据有至少 9 个元素
                            if (values.Length >= 9)
                            {
                                // 根据索引提取起点(3-5)和终点(6-8)坐标
                                XYZ pt1 = new XYZ(double.Parse(values[3]), double.Parse(values[4]), double.Parse(values[5]));
                                XYZ pt2 = new XYZ(double.Parse(values[6]), double.Parse(values[7]), double.Parse(values[8]));
                                // 调用提取好的安全创建 API（兼容项目与族环境）
                                CreateSafeRP(pt1, pt2, activeView.ViewDirection, activeView);
                                count++;
                            }
                        }
                        TaskDialog.Show("完成", $"已成功导入 {count} 个参照平面。");
                    });
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"导入过程中发生异常: {ex.Message}");
                }
            });
        }
        public ICommand CurrentViewRPRemoveCommand => new BaseBindingCommand(CurrentViewRPRemove);
        private void CurrentViewRPRemove(object obj)
        {
            if (isFuncableView(activeView))
            {
                List<ElementId> elemId = GetCurrentViewRP(activeView);
                ExternalHandler.Run(app =>
                {
                    NewTransaction.Execute(Doc, "删除参照平面", () =>
                    {
                        Doc.Delete(elemId);
                        TaskDialog.Show("tt", $"已删除参照平面{elemId.Count()}个");
                    });
                });
            }
            else TaskDialog.Show("tt", "当前视图无参照平面，请切换到平立剖面视图再试");
        }
        public ICommand CurrentViewRPGetCommand => new BaseBindingCommand(CurrentViewRPGet);
        private void CurrentViewRPGet(object obj)
        {
            if (isFuncableView(activeView))
            {
                List<ElementId> elemId = GetCurrentViewRP(activeView);
                uiDoc.Selection.SetElementIds(elemId);
            }
            else TaskDialog.Show("tt", "当前视图无参照平面，请切换到平立剖面视图再试");
        }
        private List<ElementId> GetCurrentViewRP(View view)
        {
            List<ElementId> resultIds = new List<ElementId>();
            // 建议：如果只是获取当前视图可见的，直接传入 view.Id 收集效率更高
            var referencePlanes = new FilteredElementCollector(Doc, view.Id)
                .OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>();
            foreach (ReferencePlane rp in referencePlanes)
            {
                // 【核心防护】：排除重要参照平面，防止误删族内原点参照平面
                if (IsProtectedReferencePlane(rp)) continue;

                if (rp.CanBeVisibleInView(view))
                {
                    resultIds.Add(rp.Id);
                }
            }
            return resultIds;
        }
        /// <summary>
        /// 判断参照平面是否受保护（不可被一键删除）
        /// </summary>
        private bool IsProtectedReferencePlane(ReferencePlane rp)
        {
            // 1. 被图钉锁定的不删
            if (rp.Pinned) return true;
            // 2. 属于“定义原点”的不删
            Parameter definesOrigin = rp.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN);
            if (definesOrigin != null && definesOrigin.AsInteger() == 1) return true;
            // 3. 包含中心线等系统保留关键字的不删
            if (rp.Name.Contains("中心") || rp.Name.Contains("Center")) return true;
            return false;
        }
        //private List<ElementId> GetCurrentViewRP(View view)
        //{
        //    List<ElementId> resultIds = new List<ElementId>();
        //    List<ReferencePlane> referencePlanes = new FilteredElementCollector(Doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
        //    foreach (ReferencePlane rp in referencePlanes)
        //    {
        //        if (rp.CanBeVisibleInView(view))
        //        {
        //            resultIds.Add(rp.Id);
        //        }
        //    }
        //    return resultIds;
        //}
        public ICommand CurrentViewRPHideCommand => new BaseBindingCommand(CurrentViewRPHide);
        private void CurrentViewRPHide(object obj)
        {
            View newView = activeView;
            if (isFuncableView(newView))
            {
                ExternalHandler.Run(app =>
                {
                    NewTransaction.Execute(Doc, "开关参照平面显示", () =>
                    {
                        SingleViewHide(newView);
                    });
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
            if (!isFuncableView(activeView))
            {
                TaskDialog.Show("提示", "当前视图无法生成参照平面，请切换到平立剖面视图再试");
                return;
            }
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "画平行参照平面", () =>
                 {
                     // 设置绘制平面
                     if (activeView.SketchPlane == null)
                     {
                         Plane workPlane = Plane.CreateByNormalAndOrigin(activeView.ViewDirection, activeView.Origin);
                         activeView.SketchPlane = SketchPlane.Create(Doc, workPlane);
                     }
                     XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
                     XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
                     // 【优化点】：直接使用视图方向作为参照平面的拉伸向量 (cVec)
                     XYZ cVec = activeView.ViewDirection;
                     // 画主参照平面
                     CreateSafeRP(pt1, pt2, cVec, activeView);
                     // 【优化点】：利用“叉乘”极其优雅地计算偏移方向，完美支持任何倾斜的剖面/平面
                     XYZ lineDirection = (pt2 - pt1).Normalize();
                     XYZ offsetDirection = cVec.CrossProduct(lineDirection).Normalize();
                     double offsetDistance = D / 304.8;
                     if (num == "A")
                     {
                         // 偏移一条
                         XYZ offsetPt1 = pt1 - offsetDirection * offsetDistance;
                         XYZ offsetPt2 = pt2 - offsetDirection * offsetDistance;
                         CreateSafeRP(offsetPt1, offsetPt2, cVec, activeView);
                         //要想让它在 * *“右侧”**生成，非常简单，只需要把叉乘的两个向量互换位置即可（或者将结果乘以 - 1）。
                         //修改方法
                         //找到计算 offsetDirection 的那行代码：
                         //修改前：
                         //XYZ offsetDirection = cVec.CrossProduct(lineDirection).Normalize();
                         //修改后（调换叉乘顺序）：
                         //XYZ offsetDirection = lineDirection.CrossProduct(cVec).Normalize();
                         //或者另一种改法（反转方向）
                         //如果你不想改上面那句，也可以直接在下方计算偏移点时，把加号改成减号，效果完全一样：
                     }
                     else
                     {
                         // 两侧偏移两条
                         XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance / 2;
                         XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance / 2;
                         XYZ offsetPt3 = pt1 - offsetDirection * offsetDistance / 2;
                         XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance / 2;
                         CreateSafeRP(offsetPt1, offsetPt2, cVec, activeView);
                         CreateSafeRP(offsetPt3, offsetPt4, cVec, activeView);
                     }

                 });
            });
        }
        /// <summary>
        /// 统一处理项目/族环境的参照平面创建API
        /// </summary>
        private ReferencePlane CreateSafeRP(XYZ pt1, XYZ pt2, XYZ cutVec, View view)
        {
            if (Doc.IsFamilyDocument)
            {
                return Doc.FamilyCreate.NewReferencePlane(pt1, pt2, cutVec, view);
            }
            else
            {
                return Doc.Create.NewReferencePlane(pt1, pt2, cutVec, view);
            }
        }
        //private void CreateReferencePlane(string num)
        //{
        //    if (isFuncableView(View))
        //    {
        //        XmlDoc.Instance.Task.Run(app =>
        //        {
        //            Doc.NewTransaction(() =>
        //            {
        //                if (View.SketchPlane == null)
        //                {
        //                    // 获取视图的视角方向（法线方向）
        //                    XYZ normal = View.ViewDirection;
        //                    // 定义工作平面的原点（通常使用视图的原点）
        //                    XYZ origin = View.Origin;
        //                    Plane workPlane = Plane.CreateByNormalAndOrigin(normal, origin);
        //                    View.SketchPlane = SketchPlane.Create(Doc, workPlane);
        //                }
        //                if (View.GetType().Name == "ViewPlan" || View.GetType().Name == "ViewSection")
        //                {
        //                    XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
        //                    XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
        //                    XYZ direction = (pt2 - pt1).Normalize();
        //                    Curve curve1 = Line.CreateBound(pt1, pt2) as Curve;
        //                    XYZ cVec;
        //                    double tolerance = 1e-6;
        //                    // 计算垂直于视图方向的向量
        //                    if (Math.Abs(View.ViewDirection.X) < tolerance && Math.Abs(View.ViewDirection.Y) < tolerance && (View.ViewDirection.Z == 1 || View.ViewDirection.Z == -1))
        //                    {
        //                        cVec = new XYZ(0, 0, 1);
        //                    }
        //                    else if (Math.Abs(View.ViewDirection.X) < tolerance && Math.Abs(View.ViewDirection.Z) < tolerance && (View.ViewDirection.Y == 1 || View.ViewDirection.Y == -1))
        //                    {
        //                        cVec = new XYZ(0, 1, 0);
        //                    }
        //                    else if (Math.Abs(View.ViewDirection.Z) < tolerance && Math.Abs(View.ViewDirection.Y) < tolerance && (View.ViewDirection.X == 1 || View.ViewDirection.X == -1))
        //                    {
        //                        cVec = new XYZ(1, 0, 0);
        //                    }
        //                    else
        //                    {
        //                        throw new InvalidOperationException("两个端点的坐标不满足条件。");
        //                    }
        //                    if (Doc.IsFamilyDocument)
        //                    {
        //                        ReferencePlane rp1 = Doc.FamilyCreate.NewReferencePlane(pt1, pt2, cVec, View);
        //                    }
        //                    else { ReferencePlane rp1 = Doc.Create.NewReferencePlane(pt1, pt2, cVec, View); }
        //                    XYZ offsetDirection = getnormal(curve1, View);
        //                    double offsetDistance = D / 304.8;

        //                    if (Doc.IsFamilyDocument)
        //                    {
        //                        if (num == "A")
        //                        {
        //                            XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance;
        //                            XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance;
        //                            ReferencePlane rp2 = Doc.FamilyCreate.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
        //                        }
        //                        else
        //                        {
        //                            //计算偏移后的起点和终点
        //                            XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt3 = pt1 - offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance / 2;
        //                            ReferencePlane rp2 = Doc.FamilyCreate.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
        //                            ReferencePlane rp3 = Doc.FamilyCreate.NewReferencePlane(offsetPt3, offsetPt4, cVec, View);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (num == "A")
        //                        {
        //                            XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance;
        //                            XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance;
        //                            ReferencePlane rp2 = Doc.Create.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
        //                        }
        //                        else
        //                        {
        //                            XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt3 = pt1 - offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance / 2;
        //                            XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance / 2;
        //                            ReferencePlane rp2 = Doc.Create.NewReferencePlane(offsetPt1, offsetPt2, cVec, View);
        //                            ReferencePlane rp3 = Doc.Create.NewReferencePlane(offsetPt3, offsetPt4, cVec, View);
        //                        }
        //                    }
        //                }
        //            }, "画平行参照平面");
        //        });
        //    }
        //    else TaskDialog.Show("tt", "当前视图无法生成参照平面，请切换到平立剖面视图再试");
        //    //TaskDialog.Show("tt", D.ToString() + "+" + num);
        //}
        //private XYZ getnormal(Curve curve, Autodesk.Revit.DB.View view)
        //{
        //    XYZ p1 = curve.GetEndPoint(0);
        //    XYZ p2 = curve.GetEndPoint(1);
        //    double res = 100;
        //    double tolerance = 1e-6;
        //    if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.Z == 1 || view.ViewDirection.Z == -1))
        //    {
        //        XYZ t0 = new XYZ(p1.X - (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y + (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
        //        XYZ t1 = new XYZ(p1.X + (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y - (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
        //        return Line.CreateBound(t0, t1).Direction;
        //    }
        //    // 检查 Y 坐标是否都为 0
        //    else if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Z) < tolerance && (view.ViewDirection.Y == 1 || view.ViewDirection.Y == -1))
        //    {
        //        XYZ t0 = new XYZ(p1.X + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z - (p2.X - p1.X) * (res / curve.ApproximateLength));
        //        XYZ t1 = new XYZ(p1.X - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z + (p2.X - p1.X) * (res / curve.ApproximateLength));
        //        return Line.CreateBound(t0, t1).Direction;
        //    }
        //    // 检查 Z 坐标是否都为 0
        //    else if (Math.Abs(view.ViewDirection.Z) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.X == 1 || view.ViewDirection.X == -1))
        //    {
        //        XYZ t0 = new XYZ(p1.X, p1.Y - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z + (p2.Y - p1.Y) * (res / curve.ApproximateLength));
        //        XYZ t1 = new XYZ(p1.X, p1.Y + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z - (p2.Y - p1.Y) * (res / curve.ApproximateLength));
        //        return Line.CreateBound(t0, t1).Direction;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("两个端点的坐标不满足条件。");
        //    }
        //}
        //private bool isFuncableView(View view)
        //{
        //    if (view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection")
        //    {
        //        return true;
        //    }
        //    return false;
        //}
    }
}
