using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Form.RevitStylePopup;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace CreatePipe.Form
{
    /// <summary>
    /// DisplaceModelView.xaml 的交互逻辑
    /// </summary>
    public partial class DisplaceModelView : Window
    {
        public DisplaceModelView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new DisplaceModelViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // 限制输入：只允许一位可选的负号、数字及一个小数点
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
                // 允许输入单负号或合法浮点前缀
                e.Handled = !Regex.IsMatch(newText, @"^-?[0-9]*\.?[0-9]*$");
            }
        }
        // 拦截粘贴非法内容
        private void OnTextBoxPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                string pasteText = Clipboard.GetText();
                if (sender is System.Windows.Controls.TextBox textBox)
                {
                    string newText = textBox.Text.Insert(textBox.SelectionStart, pasteText);
                    if (!Regex.IsMatch(newText, @"^-?[0-9]*\.?[0-9]*$"))
                    {
                        e.Handled = true;
                    }
                }
            }
        }
    }
    public class DisplaceModelViewModel : ObserverableObject
    {
        public static Document Document;
        public UIDocument uiDoc;
        public UIApplication UIApplication;
        private const double Tolerance = 1e-6;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public DisplaceModelViewModel(UIApplication uiApp)
        {
            UIApplication = uiApp;
            uiDoc = uiApp.ActiveUIDocument;
            Document = uiDoc?.Document;
            // 默认禁用执行按钮
            CanExecuteAction = false;
            if (Document == null || Document.IsFamilyDocument || Document.IsModelInCloud || Document.IsWorkshared)
            {
                TaskDialog.Show("拦截", "当前文档无效、为族文档、云模型或工作共享模型，不支持整体移动。");
                return;
            }
            string versionNumber = UIApplication.Application.VersionNumber;
            if (versionNumber.StartsWith("201") || versionNumber == "2020")
            {
                // 2021 及以上版本 API
                TaskDialog.Show("tt", "本功能不支持在当前Revit版本执行，请更换到2020.2及以上版本再执行。");
                return;
            }
            // 执行前置自检，通过后才放行按钮
            if (RunPreflightChecks())
            {
                CanExecuteAction = true;
            }
            ////过滤可移动对象

        }
        public ICommand ReadmeCommand => new BaseBindingCommand(ReadmeDisplacement);
        private void ReadmeDisplacement(object obj)
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    TaskDialog.Show("警告", "剪贴板中不包含文本信息。");
                    return;
                }
                string text = Clipboard.GetText().Trim();
                if (string.IsNullOrEmpty(text)) return;
                // 兼容中英文逗号、制表符(\t)及换行符切分
                string[] items = text.Split(new[] { ',', '，', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim()).ToArray();
                if (items.Length < 4)
                {
                    TaskDialog.Show("格式错误",
                        "剪贴板数值不足 4 项，请确保格式包含 4 个数值（逗号分隔）：\n" +
                        "X平移(mm), Y平移(mm), Z标高(m), 旋转角度(°)\n\n" +
                        $"当前剪贴板内容:\n{text}");
                    return;
                }
                // 校验并解析各段数值
                if (!double.TryParse(items[0], out double x))
                {
                    TaskDialog.Show("解析失败", $"第 1 项 [X平移] 不是有效数值: \"{items[0]}\"");
                    return;
                }
                if (!double.TryParse(items[1], out double y))
                {
                    TaskDialog.Show("解析失败", $"第 2 项 [Y平移] 不是有效数值: \"{items[1]}\"");
                    return;
                }
                if (!double.TryParse(items[2], out double z))
                {
                    TaskDialog.Show("解析失败", $"第 3 项 [Z标高] 不是有效数值: \"{items[2]}\"");
                    return;
                }
                if (!double.TryParse(items[3], out double rot))
                {
                    TaskDialog.Show("解析失败", $"第 4 项 [旋转角度] 不是有效数值: \"{items[3]}\"");
                    return;
                }
                // 还原赋值给绑定的 UI 属性（触发界面自动刷新）
                DistanceX = x;
                DistanceY = y;
                DistanceZ = z;
                RotateAngle = rot;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", "读取剪贴板失败: " + ex.Message);
            }
            //TaskDialog.Show("说明", "1. 本功能将解锁所有被锁定构件，执行后不会恢复锁定。\n" +
            //    "2. 平面顺时针旋转以项目基点 (0,0,0) 为轴心。\n" +
            //    "3. 内建模型 (In-Place) 与 DirectShape 自动跳过，并记录在报告中。\n" +
            //    "4. 标高修改在平移与旋转完成后最后执行。\n" +
            //    "5. 未随标高联动的图元将在最终报告中输出 ElementId。");
        }
        public ICommand PlaceCommand => new BaseBindingCommand(PlaceDisplacement);
        private void PlaceDisplacement(object obj)
        {
            if (!CanExecuteAction) return;
            // 前置检测：若所有输入均为 0，直接终止退出
            if (Math.Abs(DistanceX) < Tolerance && Math.Abs(DistanceY) < Tolerance &&
                Math.Abs(DistanceZ) < Tolerance && Math.Abs(RotateAngle) < Tolerance)
            {
                TaskDialog.Show("提示", "调整距离与旋转角度均为 0，未执行任何操作。");
                return;
            }
            // 单位转换
            double moveXFeet = DistanceX / 304.8;
            double moveYFeet = DistanceY / 304.8;
            double moveZFeet = DistanceZ / 304.8 * 1000;
            // 顺时针转换为弧度并取负
            double radAngle = -(RotateAngle * Math.PI / 180.0);

            // 收集与分类图元
            CollectElements(out List<ElementId> transformIds, out List<Level> levels, out List<ElementId> inPlaceIds, out List<ElementId> unpinnedIds);

            int movedCount = 0;
            int rotatedCount = 0;
            int levelModifiedCount = 0;
            List<ElementId> unconstrainedZIds = new List<ElementId>();
            try
            {
                // 事务 1: 批量解锁所有锁定的目标构件 (不恢复)
                if (unpinnedIds.Count > 0)
                {
                    using (Transaction tUnpin = new Transaction(Document, "解锁可移动图元"))
                    {
                        tUnpin.Start();
                        foreach (ElementId id in unpinnedIds)
                        {
                            Element elem = Document.GetElement(id);
                            if (elem == null || !elem.IsValidObject || !elem.Pinned) continue;
                            // 再次确保非组内构件
                            if (elem.GroupId != ElementId.InvalidElementId) continue;
                            try
                            {
                                elem.Pinned = false;
                            }
                            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                            {
                                // 忽略阵列等特殊受控构件无法直接解锁的异常
                            }
                            catch (Exception)
                            {
                                // 忽略其他不可解锁图元异常，保障事务整体提交
                            }
                        }
                        tUnpin.Commit();
                    }
                }
                // 事务 2: 绕项目基点 (0,0,0) 顺时针旋转
                if (Math.Abs(RotateAngle) > Tolerance && transformIds.Count > 0)
                {
                    using (Transaction tRotate = new Transaction(Document, "全模型平面旋转"))
                    {
                        tRotate.Start();
                        XYZ axisOrigin = XYZ.Zero;
                        Line rotationAxis = Line.CreateBound(axisOrigin, axisOrigin + XYZ.BasisZ);
                        ElementTransformUtils.RotateElements(Document, transformIds, rotationAxis, radAngle);
                        tRotate.Commit();
                        rotatedCount = transformIds.Count;
                    }
                }
                // 事务 3: XY 整体平移
                XYZ moveVectorXY = new XYZ(moveXFeet, moveYFeet, 0);
                if ((Math.Abs(moveXFeet) > Tolerance || Math.Abs(moveYFeet) > Tolerance) && transformIds.Count > 0)
                {
                    using (Transaction tMove = new Transaction(Document, "全模型平面平移"))
                    {
                        tMove.Start();
                        ElementTransformUtils.MoveElements(Document, transformIds, moveVectorXY);
                        tMove.Commit();
                        movedCount = transformIds.Count;
                    }
                }
                // 事务 4: 标高 Z 轴调整 (最后执行)
                if (Math.Abs(moveZFeet) > Tolerance && levels.Count > 0)
                {
                    using (Transaction tLevel = new Transaction(Document, "标高调整"))
                    {
                        tLevel.Start();
                        foreach (Level lvl in levels)
                        {
                            lvl.Elevation += moveZFeet;
                            levelModifiedCount++;
                        }
                        tLevel.Commit();
                    }
                    // 检索不随标高位移的非受控构件
                    unconstrainedZIds = FindUnconstrainedZElements();
                }
                // 弹出执行汇报窗口
                ShowExecutionReport(rotatedCount, movedCount, levelModifiedCount, unpinnedIds.Count, inPlaceIds, unconstrainedZIds);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("执行异常", $"操作执行失败: {ex.Message}");
            }
        }
        public ICommand SwitchCommand => new BaseBindingCommand(SwitchXY);
        private void SwitchXY(object obj)
        {
            double temp = DistanceX;
            DistanceX = DistanceY;
            DistanceY = temp;
        }
        /// <summary>
        /// 过滤全模型图元并按移动规则归类
        /// </summary>
        private void CollectElements(out List<ElementId> transformElementIds,
            out List<Level> levelList, out List<ElementId> inPlaceModelIds, out List<ElementId> unpinnedIds)
        {
            transformElementIds = new List<ElementId>();
            levelList = new List<Level>();
            inPlaceModelIds = new List<ElementId>();
            unpinnedIds = new List<ElementId>();

            // 1. 收集所有标高
            levelList = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>().ToList();
            // 2. 收集物理模型元素及轴网
            FilteredElementCollector modelCollector = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType();
            foreach (Element elem in modelCollector)
            {
                if (elem == null || !elem.IsValidObject) continue;
                // 排除标高 (Level 单独处理 Z)
                if (elem is Level) continue;
                // 排除基点
                if (elem is BasePoint) continue;
                // 排除链接模型实例
                if (elem is RevitLinkInstance) continue;
                // 排除视图专属构件 (文字、详图线、符号等)
                if (elem.ViewSpecific) continue;
                // 排除尺寸标注、独立草图
                if (elem is Dimension || elem is Sketch) continue;
                // 排除装配体子图元
                if (elem.AssemblyInstanceId != ElementId.InvalidElementId) continue;
                // 排除钢筋
                if (elem.Category != null && (BuiltInCategory)elem.Category.Id.IntegerValue == BuiltInCategory.OST_Rebar) continue;
                // 检查内建模型
                if (IsInPlaceElement(elem))
                {
                    inPlaceModelIds.Add(elem.Id);
                    continue; // 跳过内建模型，不纳入移动/旋转集合
                }
                // 排除附着型构件 (Hosted: 门、窗、幕墙嵌板等)，让其随宿主移动
                if (elem is FamilyInstance fi)
                {
                    if (fi.Host != null) continue;
                    if (elem.Category != null)
                    {
                        var bic = (BuiltInCategory)elem.Category.Id.IntegerValue;
                        if (bic == BuiltInCategory.OST_Doors ||
                            bic == BuiltInCategory.OST_Windows ||
                            bic == BuiltInCategory.OST_CurtainWallPanels)
                        {
                            continue;
                        }
                    }
                }
                // 必须具有几何或空间定位特征 (含 Grid)
                if (elem.Location != null || elem is Grid)
                {
                    transformElementIds.Add(elem.Id);

                    if (elem.Pinned)
                    {
                        unpinnedIds.Add(elem.Id);
                    }
                }
            }
            // 标高中被锁定的也需解锁
            foreach (Level lvl in levelList)
            {
                if (lvl.Pinned)
                {
                    unpinnedIds.Add(lvl.Id);
                }
            }
        }
        /// <summary>
        /// 判定是否为内建族或 DirectShape
        /// </summary>
        private bool IsInPlaceElement(Element elem)
        {
            if (elem is DirectShape) return true;

            if (elem is FamilyInstance fi && fi.Symbol != null && fi.Symbol.Family != null)
            {
                return fi.Symbol.Family.IsInPlace;
            }

            return false;
        }
        /// <summary>
        /// 检查无关联标高约束的自由构件
        /// </summary>
        private List<ElementId> FindUnconstrainedZElements()
        {
            List<ElementId> unconstrained = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                if (elem == null || elem.ViewSpecific || elem is Level || elem is Grid || elem is BasePoint) continue;

                if (elem is FamilyInstance fi)
                {
                    // 若无关联标高或参数为空，记为不联动构件
                    Parameter lvlParam = fi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                    if (lvlParam == null || !lvlParam.HasValue || lvlParam.AsElementId() == ElementId.InvalidElementId)
                    {
                        unconstrained.Add(elem.Id);
                    }
                }
            }
            return unconstrained;
        }
        /// <summary>
        /// 弹出结构化报告
        /// </summary>
        private void ShowExecutionReport(int rotatedCount, int movedCount, int levelCount, int unpinCount,
            List<ElementId> inPlaceIds, List<ElementId> unconstrainedZIds)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【执行统计】");
            sb.AppendLine($"• 已解除锁定图元: {unpinCount} 个");
            sb.AppendLine($"• 旋转构件数: {rotatedCount} 个 (顺时针 {RotateAngle}°)");
            sb.AppendLine($"• 平移构件数: {movedCount} 个 (X: {DistanceX} mm, Y: {DistanceY} mm)");
            sb.AppendLine($"• 调整标高数: {levelCount} 个 (Z: {DistanceZ} m)");
            sb.AppendLine();
            sb.AppendLine($" 请注意手动移动项目基点并检查清理未移动构件。");
            sb.AppendLine();
            sb.AppendLine("【特殊图元清单】");
            if (inPlaceIds.Count > 0)
            {
                sb.AppendLine($"• 已跳过的内建模型 ({inPlaceIds.Count} 个):");
                sb.AppendLine(string.Join(", ", inPlaceIds.Take(20).Select(id => id.IntegerValue.ToString())) + (inPlaceIds.Count > 20 ? " 等..." : ""));
            }
            else
            {
                sb.AppendLine("• 未检测到内建模型。");
            }

            if (unconstrainedZIds.Count > 0)
            {
                sb.AppendLine($"• 未绑定标高约束图元 ({unconstrainedZIds.Count} 个):");
                sb.AppendLine(string.Join(", ", unconstrainedZIds.Take(20).Select(id => id.IntegerValue.ToString())) + (unconstrainedZIds.Count > 20 ? " 等..." : ""));
            }

            TaskDialog reportDlg = new TaskDialog("模型移动报告")
            {
                MainInstruction = "模型整体变换已完成",
                MainContent = sb.ToString(),
                CommonButtons = TaskDialogCommonButtons.Ok
            };
            reportDlg.Show();
        }

        /// <summary>
        /// 执行前置所有环境规则校验
        /// </summary>
        private bool RunPreflightChecks()
        {
            // 1. 检查是否存在设计选项
            if (new FilteredElementCollector(Document).OfClass(typeof(DesignOption)).GetElementCount() > 0)
            {
                TaskDialog.Show("拦截", "当前文档存在设计选项，不支持整体移动。");
                return false;
            }
            // 2. 检查项目基点 (PBP) 及内部原点 (0,0,0)
            BasePoint pbp = BasePoint.GetProjectBasePoint(Document);
            if (pbp == null)
            {
                TaskDialog.Show("拦截", "未在模型中找到项目基点 (Project Base Point)。");
                return false;
            }
            XYZ pbpPos = pbp.Position;
            if (Math.Abs(pbpPos.X) > Tolerance || Math.Abs(pbpPos.Y) > Tolerance || Math.Abs(pbpPos.Z) > Tolerance)
            {
                TaskDialog.Show("拦截", $"项目基点未在内部原点 (0,0,0)。\n当前基点位置: ({pbpPos.X:F4}, {pbpPos.Y:F4}, {pbpPos.Z:F4})");
                return false;
            }
            // 3. 检查正北旋转角 (双重检测: ProjectPosition + PBP 的 BASEPOINT_ANGLETON_PARAM)
            if (!ValidateTrueNorthAngle(pbp))
            {
                return false;
            }
            // 4. 检查项目基点是否在直线轴线交点上
            IList<Grid> grids = new FilteredElementCollector(Document).OfClass(typeof(Grid)).Cast<Grid>().ToList();
            if (grids.Count < 2)
            {
                TaskDialog.Show("拦截", "模型中直线轴线数量不足 2 根，无法计算交点。");
                return false;
            }
            List<XYZ> intersectionPoints = FindGridIntersectionPoints(grids);
            bool isBasePointOnGridIntersection = false;
            foreach (XYZ pt in intersectionPoints)
            {
                if (pt == null) continue;
                if (Math.Abs(pt.X - pbpPos.X) < Tolerance && Math.Abs(pt.Y - pbpPos.Y) < Tolerance)
                {
                    isBasePointOnGridIntersection = true;
                    break;
                }
            }
            if (!isBasePointOnGridIntersection)
            {
                TaskDialog.Show("拦截", "未检测到项目基点与直线轴线交点重合，请先核准项目基点与轴网关系。");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 校验正北角度是否为 0
        /// </summary>
        private bool ValidateTrueNorthAngle(BasePoint pbp)
        {
            //// 核查 ProjectPosition
            //FilteredElementCollector collector = new FilteredElementCollector(Document).OfClass(typeof(ProjectPosition));
            //foreach (ProjectPosition pos in collector.Cast<ProjectPosition>())
            //{
            //    if (Math.Abs(pos.Angle) > Tolerance)
            //    {
            //        TaskDialog.Show("拦截", $"检测到项目已设定正北旋转角 ({pos.Angle * 180.0 / Math.PI:F2}°)，整体位移终止，请重置后重试。");
            //        return false;
            //    }
            //}
            // 核查 PBP 自身的 Angle to True North 参数
            Parameter angleParam = pbp.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM);
            if (angleParam != null && angleParam.HasValue)
            {
                double pbpAngle = angleParam.AsDouble();
                if (Math.Abs(pbpAngle) > Tolerance)
                {
                    TaskDialog.Show("拦截", $"项目基点的正北方向角不为 0 ({pbpAngle * 180.0 / Math.PI:F2}°)，无法执行操作，请重置为 0 后重试。");
                    return false;
                }
            }
            return true;
        }
        /// 查找项目中所有直线轴线的交点
        /// </summary>
        private List<XYZ> FindGridIntersectionPoints(IEnumerable<Autodesk.Revit.DB.Grid> grids)
        {
            if (grids.Count() < 2) return new List<XYZ>();
            // 提取并拍平直线轴线
            List<Line> infiniteFlatLines = new List<Line>();
            foreach (Autodesk.Revit.DB.Grid grid in grids)
            {
                if (grid.Curve is Line line)
                {
                    XYZ originFlat = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, 0);
                    XYZ directionFlat = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                    infiniteFlatLines.Add(Line.CreateUnbound(originFlat, directionFlat));
                }
            }
            // 计算交点并去重
            List<XYZ> intersectionPoints = new List<XYZ>();
            double tolerance = 0.001; // 去重容差（约0.3mm）
            for (int i = 0; i < infiniteFlatLines.Count; i++)
            {
                for (int j = i + 1; j < infiniteFlatLines.Count; j++)
                {
                    Line line1 = infiniteFlatLines[i];
                    Line line2 = infiniteFlatLines[j];
                    // 检查是否平行
                    if (line1.Direction.IsAlmostEqualTo(line2.Direction) || line1.Direction.IsAlmostEqualTo(-line2.Direction))
                    {
                        continue;
                    }
                    IntersectionResultArray intersections;
                    if (line1.Intersect(line2, out intersections) == SetComparisonResult.Overlap && intersections != null)
                    {
                        foreach (IntersectionResult iResult in intersections)
                        {
                            XYZ point = iResult.XYZPoint;
                            // 检查是否已存在非常接近的点
                            if (!intersectionPoints.Any(p => p.IsAlmostEqualTo(point, tolerance)))
                            {
                                intersectionPoints.Add(point);
                            }
                        }
                    }
                }
            }
            return intersectionPoints;
        }
        public double _distanceX = 0;
        public double DistanceX
        {
            get => _distanceX;
            set => SetProperty(ref _distanceX, value);
        }
        public double _distanceY = 0;
        public double DistanceY
        {
            get => _distanceY;
            set => SetProperty(ref _distanceY, value);
        }
        public double _distanceZ = 0;
        public double DistanceZ
        {
            get => _distanceZ;
            set => SetProperty(ref _distanceZ, value);
        }
        public double _rotateAngle = 0;
        public double RotateAngle
        {
            get => _rotateAngle;
            set => SetProperty(ref _rotateAngle, value);
        }
        private bool _canExecuteAction = false;
        public bool CanExecuteAction
        {
            get => _canExecuteAction;
            set => SetProperty(ref _canExecuteAction, value);
        }
    }
}
