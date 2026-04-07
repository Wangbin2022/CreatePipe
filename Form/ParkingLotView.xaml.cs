using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CreatePipe.Form
{
    /// <summary>
    /// ParkingLotView.xaml 的交互逻辑
    /// </summary>
    public partial class ParkingLotView : Window
    {
        public ParkingLotView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new ParkingLotViewModel(uIApplication);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class ParkingLotViewModel : ObserverableObject
    {
        private Document Document;
        private UIApplication Application;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public ParkingLotViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            Application = uiApp;
            // 初始自动获取当前视图车位
            var collector = new FilteredElementCollector(Document, Document.ActiveView.Id)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Family.Name.Contains("车位")).Select(fi => fi.Id).ToList();
            ParkReference = collector;
            ParkingLotNum = ParkReference.Count;

            _toggleSelectionCommand = new BaseBindingCommand(ToggleSelection);
            //_startSelectionCommand = new BaseBindingCommand(StartSelection, obj => !IsSelecting);
            //_endSelectionCommand = new BaseBindingCommand(EndSelection, obj => !IsSelecting && ParkReference != null && ParkReference.Count > 0);
        }
        public ICommand PickLotCommand => new BaseBindingCommand(PickLot);
        private void PickLot(object obj)
        {
            try
            {
                var refs = Application.ActiveUIDocument.Selection.PickObjects(
                    Autodesk.Revit.UI.Selection.ObjectType.Element,
                    new ParkingLotFilter(), "请框选车位");
                ParkReference = refs.Select(r => r.ElementId).ToList();
                ParkingLotNum = ParkReference.Count;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                TaskDialog.Show("提示", "车位选择已退出");
            }
        }
        public ICommand CodeAllCommand => new BaseBindingCommand(RewriteCode);
        private void RewriteCode(object obj)
        {
            if (ParkReference.Count == 0) return;
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "车位自动编号", () =>
                {
                    for (int i = 0; i < ParkReference.Count; i++)
                    {
                        Element elem = Document.GetElement(ParkReference[i]);
                        Parameter p = elem?.LookupParameter("车位编号");
                        if (p != null && !p.IsReadOnly)
                        {
                            p.Set(FormatLotSn(StartCode + i));
                        }
                    }
                });
            });
        }
        // 统一存储被选中的 ElementId
        private List<ElementId> ParkReference = new List<ElementId>();
        //private bool flag = true;
        // 选择状态锁
        private bool _isSelecting = false;
        public bool IsSelecting
        {
            get => _isSelecting;
            set
            {
                SetProperty(ref _isSelecting, value);
                // 状态改变时同步刷新按钮文字和EndSelection按钮状态
                OnPropertyChanged(nameof(SelectionButtonText));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
        // 按钮文字：根据状态和已选数量动态切换
        public string SelectionButtonText
        {
            get
            {
                if (!IsSelecting) return "开始选择";
                return SelectedCount > 0 ? "停止点选" : "点选中...";
            }
        }
        // 已选数量
        private int _selectedCount = 0;
        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                SetProperty(ref _selectedCount, value);
                // 数量变化时也要刷新按钮文字（从"点选中..."变为"停止点选"）
                OnPropertyChanged(nameof(SelectionButtonText));
            }
        }
        // 核心：保存点选顺序的字典 Key=顺序(1,2,3...) Value=ElementId
        private Dictionary<int, ElementId> _selectionOrderDict = new Dictionary<int, ElementId>();
        public Dictionary<int, ElementId> SelectionOrderDict
        {
            get => _selectionOrderDict; set => SetProperty(ref _selectionOrderDict, value);
        }
        private ICommand _toggleSelectionCommand;
        public ICommand ToggleSelectionCommand => _toggleSelectionCommand;
        private async void ToggleSelection(object obj)
        {
            //===== 分支一：当前未在选择 → 开始选择 =====
            if (!IsSelecting)
            {
                IsSelecting = true; // 按钮文字变为"点选中..."
                // 清空上一次的结果
                SelectionOrderDict = new Dictionary<int, ElementId>();
                SelectedCount = 0;
                await Task.Delay(50); // 让WPF先渲染按钮状态变化
                int order = 1; // 点选顺序计数器
                while (IsSelecting)
                {
                    try
                    {
                        Reference r = Application.ActiveUIDocument.Selection.PickObject(
                            Autodesk.Revit.UI.Selection.ObjectType.Element,
                            new ParkingLotFilter(),
                            $"请按顺序点选车位（已选{SelectedCount}个），再次点击[停止点选]按钮结束");

                        // 检查是否已选过（防重复）
                        if (!SelectionOrderDict.ContainsValue(r.ElementId))
                        {
                            SelectionOrderDict[order] = r.ElementId;
                            order++;
                            SelectedCount = SelectionOrderDict.Count;
                            //SelectedCount变化会触发SelectionButtonText刷新
                            // 第一个选完后按钮文字从"点选中..."变为"停止点选"
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // 用户按了Esc键，视为主动退出选择
                        IsSelecting = false;
                    }
                    catch (Exception ex)
                    {
                        IsSelecting = false;
                        TaskDialog.Show("选择异常", ex.Message);
                    }
                }
                // 退出循环后刷新按钮状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            // ===== 分支二：当前正在选择 → 停止点选 =====
            else
            {
                // 直接把标志位置false，while循环在下一次PickObject抛出异常时会自然退出
                // 但由于PickObject是阻塞的，我们需要用程序模拟Esc来打断它
                IsSelecting = false;

                // 通过Revit API模拟取消当前选择操作，打断PickObject阻塞
                try
                {
                    Application.ActiveUIDocument.Selection.SetElementIds(new List<ElementId>());
                }
                catch { /* 忽略 */ }
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
        // 改为私有字段存储，构造函数中初始化
        //private readonly ICommand _startSelectionCommand;
        //private readonly ICommand _endSelectionCommand;
        //public ICommand StartSelectionCommand => _startSelectionCommand;
        //public ICommand EndSelectionCommand => _endSelectionCommand;
        //private bool _isSelecting = false;
        //public bool IsSelecting { get => _isSelecting; set => SetProperty(ref _isSelecting, value); }
        public ICommand StartSelectionCommand => new BaseBindingCommand(StartSelection, obj => !IsSelecting);
        public ICommand EndSelectionCommand => new BaseBindingCommand(EndSelection, obj => !IsSelecting && ParkReference != null && ParkReference.Count > 0);
        // 持有当前激活的Filter实例，停止时通过它发送中断信号
        private ParkingLotFilter _activeFilter = null;
        private async void StartSelection(object obj)
        {
            //===== 分支一：当前未在选择→ 开始选择 =====
            if (!IsSelecting)
            {
                IsSelecting = true;

                // 清空上一次结果
                SelectionOrderDict = new Dictionary<int, ElementId>();
                SelectedCount = 0;

                await Task.Delay(50); // 让WPF先渲染按钮状态

                int order = 1;

                // 创建可中断Filter实例，并保存引用供停止时使用
                _activeFilter = new ParkingLotFilter();

                while (IsSelecting)
                {
                    try
                    {
                        Reference r = Application.ActiveUIDocument.Selection.PickObject(
                            Autodesk.Revit.UI.Selection.ObjectType.Element,
                            _activeFilter, // 使用可中断Filter
                            $"请按顺序点选车位（已选{SelectedCount} 个），点击[停止点选]按钮结束");

                        // 防重复添加
                        if (!SelectionOrderDict.ContainsValue(r.ElementId))
                        {
                            SelectionOrderDict[order] = r.ElementId;
                            order++;
                            SelectedCount = SelectionOrderDict.Count;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // 两种情况都走这里：
                        // 1. 用户按了Esc键
                        // 2. 外部将 _activeFilter.ShouldStop = true 触发的中断
                        IsSelecting = false;
                    }
                    catch (Exception ex)
                    {
                        IsSelecting = false;
                        TaskDialog.Show("选择异常", ex.Message);
                    }
                }

                // 清理Filter引用
                _activeFilter = null;

                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }// ===== 分支二：当前正在选择 → 停止点选 =====
            else
            {
                // 通过Filter发送中断信号
                // 当用户下一次在视图中移动鼠标触发AllowElement时，Filter会抛出异常打断PickObject
                if (_activeFilter != null)
                {
                    _activeFilter.ShouldStop = true;
                }
                //// 1. 上锁：标记为正在选择
                //IsSelecting = true;
                //// 强制刷新WPF按钮状态（这句非常关键，确保UI立即将开始按钮置灰）
                //System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                //// 3. 【核心修复】等待 50 毫秒，把主线程控制权短暂交还给 WPF，让它把按钮变灰！
                //await Task.Delay(50);
                //// 清空历史数据
                //if (ParkReference != null)
                //{
                //    ParkReference.Clear();
                //}
                //else
                //{
                //    ParkReference = new List<ElementId>();
                //}
                //ParkingLotNum = 0;
                //SelectedCount = 0;
                ////flag = true;
                //List<ElementId> tempIds = new List<ElementId>();
                //// 使用异步循环或直到用户 Esc
                //while (IsSelecting)
                //{
                //    try
                //    {
                //        Reference r = Application.ActiveUIDocument.Selection.PickObject(
                //            Autodesk.Revit.UI.Selection.ObjectType.Element,
                //            new ParkingLotFilter(), "请按顺序点选车位，Esc结束");
                //        if (!tempIds.Contains(r.ElementId))
                //        {
                //            tempIds.Add(r.ElementId);
                //            SelectedCount = tempIds.Count;
                //            // 同步到主集合以便预览
                //            ParkReference = new List<ElementId>(tempIds);
                //            ParkingLotNum = ParkReference.Count;
                //        }
                //    }
                //    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                //    {
                //        //flag = false;
                //        IsSelecting = false;
                //        TaskDialog.Show("提示", "车位选择已退出");
                //    }
                //}
                //// 通知UI刷新按钮状态 (在WPF中，退出拾取后如果按钮状态未变，可调用此方法强制刷新Command)
                //System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
        private void EndSelection(object obj)
        {
            //===== 阶段一：按点选顺序构建有序列表 =====
            // 按Key排序确保顺序正确（Key就是点选的1,2,3...顺序）
            var orderedEntries = SelectionOrderDict.OrderBy(kv => kv.Key).ToList();
            // ===== 阶段二：预检查 =====
            int missingParamCount = 0;
            int readOnlyCount = 0;
            int emptyCount = 0;
            List<string> existingNumbers = new List<string>();
            // 记录检查结果，与orderedEntries保持相同顺序
            // Item1 = Element, Item2 = Parameter, Item3 = 当前编号(可为null)
            var checkedList = new List<(Element Elem, Parameter Param, string CurrentNumber)>();
            foreach (var kv in orderedEntries)
            {
                Element elem = Document.GetElement(kv.Value);
                Parameter param = elem?.LookupParameter("车位编号");

                if (param == null)
                {
                    missingParamCount++;
                    checkedList.Add((elem, null, null));
                    continue;
                }
                if (param.IsReadOnly)
                {
                    readOnlyCount++;
                    checkedList.Add((elem, param, null));
                    continue;
                }
                string currentNum = param.HasValue ? param.AsString() : null;
                if (string.IsNullOrWhiteSpace(currentNum))
                {
                    emptyCount++;
                    checkedList.Add((elem, param, null));
                }
                else
                {
                    existingNumbers.Add(currentNum);
                    checkedList.Add((elem, param, currentNum));
                }
            }
            // 检查重复编号
            var duplicateNumbers = existingNumbers.GroupBy(x => x).Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            //汇总错误信息
            StringBuilder errorMsg = new StringBuilder();
            bool hasError = false;
            if (missingParamCount > 0)
            {
                errorMsg.AppendLine($"• 发现 {missingParamCount} 个车位不包含车位编号参数。");
                hasError = true;
            }
            if (readOnlyCount > 0)
            {
                errorMsg.AppendLine($"• 发现 {readOnlyCount} 个车位的编号参数被锁定（可能因为打组或工作集权限）。");
                hasError = true;
            }
            if (emptyCount > 0)
            {
                errorMsg.AppendLine($"• 发现 {emptyCount} 个车位的编号为空。");
                hasError = true;
            }
            if (duplicateNumbers.Count > 0)
            {
                errorMsg.AppendLine($"• 发现重复的编号：{string.Join(", ", duplicateNumbers)}");
                hasError = true;
            }
            if (hasError)
            {
                errorMsg.AppendLine("\n请先修正上述异常后，再重新执行排序功能！");
                TaskDialog.Show("检测到异常", errorMsg.ToString());
                return;
            }
            if (existingNumbers.Count == 0)
            {
                TaskDialog.Show("提示", "选中的车位均没有有效编号，无法进行重排。");
                return;
            }
            // ===== 阶段三：对编号池进行自然排序（A-2 排在A-10 前面）=====
            var sortedNumbers = existingNumbers.OrderBy(s => Regex.Replace(s ?? "", @"\d+", m => m.Value.PadLeft(10, '0'))).ToList();
            // ===== 阶段四：预览确认 =====
            // 构建预览信息：点选第X个 旧编号 → 新编号
            StringBuilder previewMsg = new StringBuilder();
            previewMsg.AppendLine("即将执行以下重排操作：\n");
            previewMsg.AppendLine($"{"点选顺序",-8} {"原编号",-15} {"新编号",-15}");
            previewMsg.AppendLine(new string('-', 40));
            int sortedIndex = 0;
            for (int i = 0; i < checkedList.Count; i++)
            {
                var (_, param, currentNum) = checkedList[i];
                if (param != null && !param.IsReadOnly && !string.IsNullOrWhiteSpace(currentNum))
                {
                    string newNum = sortedNumbers[sortedIndex];
                    previewMsg.AppendLine($"第{i + 1} 个{currentNum,-15} → {newNum}");
                    sortedIndex++;
                }
            }
            TaskDialogResult confirm = TaskDialog.Show("确认重排", previewMsg.ToString(), TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            if (confirm != TaskDialogResult.Yes) return;
            // ===== 阶段五：执行事务写入 =====
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "车位按选择顺序重排", () =>
                {
                    try
                    {
                        int idx = 0;
                        foreach (var (elem, param, currentNum) in checkedList)
                        {
                            // 只处理有效的（非null、非只读、非空编号）
                            if (param != null && !param.IsReadOnly && !string.IsNullOrWhiteSpace(currentNum))
                            {
                                param.Set(sortedNumbers[idx]);
                                idx++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("执行错误", ex.Message);
                    }
                });
            });
            TaskDialog.Show("完成", $"成功！已将{existingNumbers.Count} 个车位按点选顺序重新分配了编号。");
            // ===== 阶段六：清空状态，重置UI =====
            SelectionOrderDict = new Dictionary<int, ElementId>();
            SelectedCount = 0;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
        //private void EndSelection(object obj)
        //{
        //    // 1. 关闭选择循环开关
        //    //flag = false;
        //    //if (ParkReference == null || ParkReference.Count == 0)
        //    //{
        //    //    TaskDialog.Show("提示", "未选择任何车位。");
        //    //    return;
        //    //}
        //    // === 阶段一：数据预检查 ===
        //    List<string> existingNumbers = new List<string>();
        //    int emptyCount = 0;
        //    int readOnlyCount = 0;
        //    int missingParamCount = 0;
        //    foreach (ElementId id in ParkReference)
        //    {
        //        Element elem = Document.GetElement(id);
        //        Parameter param = elem?.LookupParameter("车位编号");
        //        if (param == null)
        //        {
        //            missingParamCount++;
        //            continue;
        //        }
        //        if (param.IsReadOnly)
        //        {
        //            readOnlyCount++;
        //        }
        //        if (!param.HasValue || string.IsNullOrWhiteSpace(param.AsString()))
        //        {
        //            emptyCount++;
        //        }
        //        else
        //        {
        //            existingNumbers.Add(param.AsString());
        //        }
        //    }

        //    // 检查是否有重复编号
        //    var duplicateNumbers = existingNumbers.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        //    // 汇总异常信息
        //    StringBuilder errorMsg = new StringBuilder();
        //    bool hasError = false;
        //    if (emptyCount > 0)
        //    {
        //        errorMsg.AppendLine($"• 发现 {emptyCount} 个车位的编号为空。");
        //        hasError = true;
        //    }
        //    if (duplicateNumbers.Count > 0)
        //    {
        //        errorMsg.AppendLine($"• 发现重复的编号：{string.Join(", ", duplicateNumbers)}");
        //        hasError = true;
        //    }
        //    if (readOnlyCount > 0)
        //    {
        //        errorMsg.AppendLine($"• 发现 {readOnlyCount} 个车位的编号参数被锁定（可能是因为打组或工作集权限）。");
        //        hasError = true;
        //    }
        //    if (missingParamCount > 0)
        //    {
        //        errorMsg.AppendLine($"• 发现 {missingParamCount} 个车位不包含“车位编号”参数。");
        //        hasError = true;
        //    }
        //    // 如果存在异常，拦截并退出
        //    if (hasError)
        //    {
        //        errorMsg.AppendLine("\n请先修正上述异常后，再重新执行排序功能！");
        //        TaskDialog.Show("检测到异常", errorMsg.ToString());
        //        return;
        //    }
        //    // 如果连一个编号都没有
        //    if (existingNumbers.Count == 0)
        //    {
        //        TaskDialog.Show("提示", "选中的车位均没有有效编号，无法进行重排。");
        //        return;
        //    }
        //    // 将数字补齐到10位进行比对，确保 A-2 排在 A-10 前面
        //    var sortedNumbers = existingNumbers.OrderBy(s => Regex.Replace(s ?? "", @"\d+", m => m.Value.PadLeft(10, '0'))).ToList();
        //    // === 阶段三：执行重排覆写 ===
        //    _externalHandler.Run(app =>
        //    {
        //        NewTransaction.Execute(Document, "车位按顺序重排", () =>
        //        {
        //            try
        //            {
        //                for (int i = 0; i < ParkReference.Count; i++)
        //                {
        //                    Element elem = Document.GetElement(ParkReference[i]);
        //                    Parameter param = elem?.LookupParameter("车位编号");

        //                    if (param != null && !param.IsReadOnly)
        //                    {
        //                        // 按照点选顺序，依次赋予排好序的编号
        //                        param.Set(sortedNumbers[i]);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                TaskDialog.Show("执行错误", ex.Message);
        //            }
        //        });
        //    });
        //    TaskDialog.Show("提示", $"成功！已将 {ParkReference.Count} 个车位按您的点选顺序重新分配了编号。");
        //    // 执行完毕后清空记录，让 ParkReference 归零
        //    // 这样 StartSelectionCommand 就会重新启用，EndSelectionCommand 重新置灰，方便进行下一波车位操作
        //    ParkReference.Clear();
        //    ParkingLotNum = 0;
        //    SelectedCount = 0;

        //    // 同样通知UI刷新按钮状态
        //    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        //    //// 1. 关闭选择循环开关
        //    //flag = false;
        //    //if (ParkReference == null || ParkReference.Count == 0)
        //    //{
        //    //    TaskDialog.Show("提示", "未选择任何车位。");
        //    //    return;
        //    //}
        //    //// 2. 异步执行 Revit API 事务
        //    //_externalHandler.Run(app =>
        //    //{
        //    //    // 步骤A：获取所有点选车位当前的编号
        //    //    List<string> existingNumbers = new List<string>();
        //    //    foreach (ElementId id in ParkReference)
        //    //    {
        //    //        Element elem = Document.GetElement(id);
        //    //        Parameter param = elem?.LookupParameter("车位编号");
        //    //        // 确保有值才收集
        //    //        if (param != null && param.HasValue && !string.IsNullOrWhiteSpace(param.AsString()))
        //    //        {
        //    //            existingNumbers.Add(param.AsString());
        //    //        }
        //    //    }
        //    //    if (existingNumbers.Count == 0)
        //    //    {
        //    //        TaskDialog.Show("提示", "选中的车位均没有编号，无法重排。");
        //    //        return;
        //    //    }
        //    //    // 步骤B：对编号进行“自然排序”从小到大
        //    //    // 使用正则将字符串中的数字部分补齐到10位，确保 "A-2" 排在 "A-10" 前面，而不是标准的字符串排序(A-10会在A-2前面)
        //    //    var sortedNumbers = existingNumbers.OrderBy(s => Regex.Replace(s ?? "", @"\d+", m => m.Value.PadLeft(10, '0'))).ToList();
        //    //    // 步骤C：按点选的顺序 (ParkReference 的顺序) 重新写入排序后的编号
        //    //    NewTransaction.Execute(Document, "车位按顺序重排", () =>
        //    //    {
        //    //        try
        //    //        {
        //    //            // 以选中的有效编号数量为界限，防止出错
        //    //            int count = Math.Min(ParkReference.Count, sortedNumbers.Count);
        //    //            for (int i = 0; i < count; i++)
        //    //            {
        //    //                Element elem = Document.GetElement(ParkReference[i]);
        //    //                Parameter param = elem?.LookupParameter("车位编号");
        //    //                if (param != null && !param.IsReadOnly)
        //    //                {
        //    //                    // 按点选顺序依次赋予排序好的编号
        //    //                    param.Set(sortedNumbers[i]);
        //    //                }
        //    //            }
        //    //        }
        //    //        catch (Exception)
        //    //        {
        //    //            throw;
        //    //        }
        //    //    });
        //    //});
        //    //TaskDialog.Show("提示", $"已成功将 {ParkReference.Count} 个车位按您的点选顺序重新分配了编号。");
        //}
        public ICommand FindLotCommand => new BaseBindingCommand(FindLot);
        private void FindLot(object obj)
        {
            if (string.IsNullOrWhiteSpace(findLotName)) return;
            var results = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Family.Name.Contains("车位"))
                .Where(fi => fi.LookupParameter("车位编号")?.AsString()?.Contains(findLotName) ?? false)
                .Select(fi => fi.Id).ToList();
            if (results.Any())
            {
                Application.ActiveUIDocument.Selection.SetElementIds(results);
                Application.ActiveUIDocument.ShowElements(results); // 自动定位到元素
            }
            else
            {
                TaskDialog.Show("提示", "未找到匹配的车位编号");
            }
        }
        public ICommand FindUnNamedLotCommand => new BaseBindingCommand(FindUnNamedLot);
        private void FindUnNamedLot(object obj)
        {
            // ===== 第一步：收集所有车位构件 =====
            var allParkingElements = new FilteredElementCollector(Document)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Family.Name.Contains("车位")).ToList();
            if (!allParkingElements.Any())
            {
                TaskDialog.Show("提示", "模型中未找到任何车位构件。");
                return;
            }
            // ===== 第二步：筛选编号为空的构件 =====
            var emptyNumberElements = allParkingElements
                .Where(fi => string.IsNullOrWhiteSpace(fi.LookupParameter("车位编号")?.AsString()))
                .Select(fi => fi.Id).ToList();
            // ===== 第三步：无空值时提前返回 =====
            if (!emptyNumberElements.Any())
            {
                TaskDialog.Show("检查结果", $"✅ 检查完成，共检查 {allParkingElements.Count} 个车位，\n" + $"未发现编号为空的车位构件。");
                return;
            }
            // ===== 第四步：弹窗确认并高亮选中 =====
            TaskDialogResult result = TaskDialog.Show("发现未命名车位",
                $"⚠️ 发现 {emptyNumberElements.Count} 个车位编号为空。\n\n" + $"点击[确定]将在视图中高亮选中所有未命名车位构件。",
                TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);
            if (result == TaskDialogResult.Ok)
            {
                Application.ActiveUIDocument.Selection.SetElementIds(emptyNumberElements);
                Application.ActiveUIDocument.ShowElements(emptyNumberElements);
                TaskDialog.Show("已选中", $"已在视图中高亮选中 {emptyNumberElements.Count} 个未命名车位构件。\n" + $"请手动填写编号后，再执行编号重排操作。");
            }
        }
        public ICommand FindDuplicateLotCommand => new BaseBindingCommand(FindDuplicateLot);
        private void FindDuplicateLot(object obj)
        {
            // ===== 第一步：收集所有车位构件 =====
            var allParkingElements = new FilteredElementCollector(Document)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Family.Name.Contains("车位")).ToList();
            if (!allParkingElements.Any())
            {
                TaskDialog.Show("提示", "模型中未找到任何车位构件。");
                return;
            }
            // ===== 第二步：按车位编号分组，找出重复项 =====
            var duplicateGroups = allParkingElements
                .Select(fi => new
                {
                    Id = fi.Id,
                    Number = fi.LookupParameter("车位编号")?.AsString()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Number))
                .GroupBy(x => x.Number).Where(g => g.Count() > 1).ToList();
            // ===== 第三步：无重复时提前返回 =====
            if (!duplicateGroups.Any())
            {
                TaskDialog.Show("检查结果",
                    $"✅ 检查完成，共检查 {allParkingElements.Count} 个车位，\n" +
                    $"未发现重复编号。");
                return;
            }
            // ===== 第四步：汇总重复信息 =====
            var duplicateIds = duplicateGroups.SelectMany(g => g.Select(x => x.Id)).ToList();
            StringBuilder reportMsg = new StringBuilder();
            reportMsg.AppendLine($"⚠️ 发现 {duplicateGroups.Count} 个重复编号，共涉及 {duplicateIds.Count} 个构件：\n");
            reportMsg.AppendLine($"{"编号",-20} {"重复数量"}");
            reportMsg.AppendLine(new string('-', 35));
            foreach (var group in duplicateGroups.OrderBy(g => Regex.Replace(g.Key ?? "", @"\d+", m => m.Value.PadLeft(10, '0'))))
            {
                reportMsg.AppendLine($"{group.Key,-20} {group.Count()} 个");
            }
            reportMsg.AppendLine($"\n点击[确定]将在视图中高亮选中所有重复构件。");
            // ===== 第五步：弹窗确认并高亮选中 =====
            TaskDialogResult result = TaskDialog.Show("发现重复编号", reportMsg.ToString(),
                TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);
            if (result == TaskDialogResult.Ok)
            {
                Application.ActiveUIDocument.Selection.SetElementIds(duplicateIds);
                Application.ActiveUIDocument.ShowElements(duplicateIds);

                TaskDialog.Show("已选中",
                    $"已在视图中高亮选中 {duplicateIds.Count} 个重复车位构件。\n" +
                    $"请手动检查并修正后，再执行编号重排操作。");
            }
        }
        public string findLotName { get; set; }
        // 1. 直接用简单的字符串列表
        public List<string> FormatItems { get; set; } = new List<string> { "默认格式", "补一个0", "补两个0", "补三个0" };
        // 2. 绑定 SelectedIndex 而不是 SelectedValue
        private int _selectedIndex = 0; // 默认选中第0项
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                UpdateCodePreview();
            }
        }
        private int _parkingLotNum;
        public int ParkingLotNum { get => _parkingLotNum; set => SetProperty(ref _parkingLotNum, value); }
        //private int _selectedCount = 0;
        //public int SelectedCount { get => _selectedCount; set => SetProperty(ref _selectedCount, value); }
        private int startCode;
        public int StartCode
        {
            get { return startCode; }
            set
            {
                startCode = value;
                OnPropertyChanged();
                UpdateCodePreview();
            }
        }
        private string prefix;
        public string Prefix
        {
            get => prefix;
            set
            {
                prefix = value;
                OnPropertyChanged();
                UpdateCodePreview();
            }
        }
        private string codePreview;
        public string CodePreview { get => codePreview; set => SetProperty(ref codePreview, value); }
        // 3. 提取格式化逻辑 (极其简练)
        private string FormatLotSn(int currentValue)
        {
            // 索引0对应D1，索引1对应D2，依此类推
            int padding = SelectedIndex + 1;
            return $"{Prefix}{currentValue.ToString($"D{padding}")}";
        }
        private void UpdateCodePreview()
        {
            if (ParkingLotNum <= 0)
            {
                CodePreview = "未选中车位";
                return;
            }
            CodePreview = $"{FormatLotSn(StartCode)} —— {FormatLotSn(StartCode + ParkingLotNum - 1)}";
        }
    }
}
