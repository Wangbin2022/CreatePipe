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
    /// BacthFamilyEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class BacthFamilyEditorView : Window
    {
        public BacthFamilyEditorView(UIApplication uIApplication)
        {
            InitializeComponent();
            var vm = new BacthFamilyEditorViewModel(uIApplication);
            this.DataContext = vm;
            // 窗体初始化时，刷新一次显示状态，隐藏多余的行
            UpdateRowsVisibility();
            // 把 View 的关闭方法交给 ViewModel 的委托
            vm.CloseAction = () => this.Close();
        }
        // 记录当前显示了几行（默认显示1行）
        private int _visibleRowCount = 1;
        // 界面最大允许显示的行数
        private readonly int _maxRowCount = 3;
        // 第一行最后一个按钮（加号）点击事件
        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount < _maxRowCount)
            {
                _visibleRowCount++;
                UpdateRowsVisibility();
            }
        }

        // 第二行最后一个按钮（减号）点击事件
        private void BtnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount > 1)
            {
                _visibleRowCount--;
                UpdateRowsVisibility();
            }
        }
        // 核心逻辑：根据当前的行数，自动隐藏/显示控件
        private void UpdateRowsVisibility()
        {
            // 遍历 Grid 里的所有控件 (UniversialSplitButton 和 CircleImageButton)
            foreach (UIElement child in MainGrid.Children)
            {
                // 获取当前控件属于第几行 (0代表第一行, 1代表第二行...)
                int rowIndex = System.Windows.Controls.Grid.GetRow(child);
                // 如果控件所在行小于当前允许显示的行数，就显示，否则折叠隐藏
                if (rowIndex < _visibleRowCount)
                {
                    child.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    // Collapsed 会让控件完全消失，且不占位
                    child.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }
    //批量处理族的格式转换,批量改类型，批量删除文本属性，批量升级到当前版本
    public class BacthFamilyEditorViewModel : ObserverableObject
    {
        private UIApplication _uiApp;
        private Autodesk.Revit.ApplicationServices.Application _app;
        public BacthFamilyEditorViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _app = uiApp.Application;
        }
        /// <summary>
        /// 提取1：通用选择多文件逻辑
        /// </summary>
        private List<string> SelectRevitFiles(string title, string filter = "Revit 族文件 (*.rfa)|*.rfa")
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            return openFileDialog.ShowDialog() == true ? openFileDialog.FileNames.ToList() : new List<string>();
        }
        /// <summary>
        /// 提取2：通用检查文件是否满足处理条件（防崩溃、防报错）
        /// </summary>
        private bool IsValidForProcessing(string filePath, int currentVersion, out string skipReason)
        {
            skipReason = string.Empty;
            try
            {
                BasicFileInfo fileInfo = BasicFileInfo.Extract(filePath);

                // 检查：是否高于当前 Revit 版本
                if (fileInfo.IsSavedInLaterVersion)
                {
                    skipReason = "文件版本高于当前 Revit 版本";
                    return false;
                }
                if (int.TryParse(fileInfo.Format, out int fileVersion) && fileVersion > currentVersion)
                {
                    skipReason = $"文件版本({fileVersion})高于当前版本";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                skipReason = $"无法读取文件信息: {ex.Message}";
                return false;
            }
        }
        /// <summary>
        /// 提取3：通用批量处理结果报告弹窗
        /// </summary>
        private void ShowBatchReport(string taskName, int totalCount, List<string> successList, List<string> failList)
        {
            string resultMessage = $"共选中 {totalCount} 个文件。\n\n";
            resultMessage += $"处理成功 ({successList.Count} 个)：\n";
            resultMessage += successList.Count > 0 ? string.Join("\n", successList.Take(15)) + (successList.Count > 15 ? "\n... (省略显示更多)" : "") : "无";
            resultMessage += $"\n\n失败或跳过 ({failList.Count} 个)：\n";
            resultMessage += failList.Count > 0 ? string.Join("\n", failList.Take(15)) + (failList.Count > 15 ? "\n... (省略显示更多)" : "") : "无";
            TaskDialog.Show(taskName + " 报告", resultMessage);
        }
        public string TVCommandName1 { get; set; } = "批量升级族版本";
        public ICommand TVCommand1 => new BaseBindingCommand(TVControl1);
        public void TVControl1(object obj)
        {
            ExecuteClose(null);
            var files = SelectRevitFiles("请选择需要升级的 Revit 族文件");
            if (files.Count == 0) return;
            int currentVersion = int.Parse(_app.VersionNumber);
            List<string> successList = new List<string>(), failList = new List<string>();
            NoTransactionWithProgressBarHelper.Execute(files.Count, "批量升级族版本", (progress) =>
            {
                progress.UpdateMax(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    string fileName = Path.GetFileName(filePath);
                    progress.Update(i + 1, fileName);
                    if (!IsValidForProcessing(filePath, currentVersion, out string reason))
                    {
                        failList.Add($"{fileName} [跳过: {reason}]");
                        continue;
                    }
                    try
                    {
                        // 使用后台静默打开，会自动升级到当前版本
                        Document familyDoc = _app.OpenDocumentFile(filePath);
                        if (!familyDoc.IsFamilyDocument) { familyDoc.Close(false); continue; }
                        // 保存覆盖并关闭
                        familyDoc.Close(true);
                        successList.Add(fileName);
                    }
                    catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                }
            });
            ShowBatchReport("批量升级族版本", files.Count, successList, failList);
        }
        public string TVCommandName2 { get; set; } = "批量修改族类型";
        public ICommand TVCommand2 => new BaseBindingCommand(TVControl2);
        public void TVControl2(object obj)
        {
            ExecuteClose(null);
            var files = SelectRevitFiles("请选择需要转换类型的族文件");
            if (files.Count == 0) return;

            var partTypeMap = new Dictionary<CategoryItem, List<PartType>>
        {
                        { new CategoryItem("常规模型", -2000151), new List<PartType> { PartType.Normal} },
                        { new CategoryItem("专用设备", -2001350), new List<PartType> { PartType.Normal} },
            { new CategoryItem("风道末端", -2008013), new List<PartType> { PartType.Normal} },
            { new CategoryItem("桥架配件", -2008126), new List<PartType> { PartType.ChannelCableTrayCross, PartType.ChannelCableTrayElbow, PartType.ChannelCableTrayMultiPort, PartType.ChannelCableTrayOffset, PartType.ChannelCableTrayTee, PartType.ChannelCableTrayTransition, PartType.ChannelCableTrayUnion, PartType.ChannelCableTrayVerticalElbow, PartType.LadderCableTrayCross, PartType.LadderCableTrayElbow, PartType.LadderCableTrayMultiPort, PartType.LadderCableTrayOffset, PartType.LadderCableTrayTee, PartType.LadderCableTrayTransition, PartType.LadderCableTrayUnion, PartType.LadderCableTrayVerticalElbow } },
            { new CategoryItem("通讯设备", -2008081), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItem("线管配件", -2008128), new List<PartType> { PartType.Cap,PartType.Cross, PartType.Elbow, PartType.JunctionBoxElbow, PartType.MultiPort, PartType.Tee, PartType.Transition, PartType.Union } },
            { new CategoryItem("数据设备", -2008083), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItem("风管附件", -2008016), new List<PartType> { PartType.AttachesTo, PartType.BreaksInto, PartType.Damper } },
            { new CategoryItem("风管管件", -2008010), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.LateralCross, PartType.LateralTee, PartType.MultiPort, PartType.Offset, PartType.Pants, PartType.TapAdjustable, PartType.TapPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
            { new CategoryItem("电气设备", -2001040), new List<PartType> { PartType.EquipmentSwitch, PartType.OtherPanel, PartType.PanelBoard, PartType.SwitchBoard, PartType.Transformer } },
            { new CategoryItem("电气装置", -2001060), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItem("火灾报警设备", -2008085), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItem("照明设备（开关）", -2008087), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItem("照明设备 (灯具)", -2001120), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItem("机械设备", -2001140), new List<PartType> { PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor, PartType.Normal, PartType.ValveBreaksInto } },
            { new CategoryItem("护士呼叫设备", -2008077), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItem("管道附件", -2008055), new List<PartType> { PartType.Normal, PartType.AttachesTo, PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor,  PartType.Sensor, PartType.ValveBreaksInto, PartType.ValveNormal } },
            { new CategoryItem("管道管件", -2008049), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.PipeFlange, PartType.LateralCross, PartType.LateralTee, PartType.PipeMechanicalCoupling, PartType.MultiPort, PartType.SpudAdjustable, PartType.SpudPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
            { new CategoryItem("卫浴装置", -2001160), new List<PartType> { PartType.Normal } },
            { new CategoryItem("安防设备", -2008079), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItem("喷头", -2008099), new List<PartType> { PartType.Normal } },
            { new CategoryItem("电话设备", -2008075), new List<PartType> { PartType.Normal, PartType.JunctionBox } }
                    };

            var dialog = new UniversalDoubleComboboxWindow("设置族参数", "1. 请选择族类别:", "2. 请选择零件类型:", partTypeMap);
            if (dialog.ShowDialog() != true) return;

            CategoryItem catItem = (CategoryItem)dialog.SelectedItem1;
            PartType partType = (PartType)dialog.SelectedItem2;

            int currentVersion = int.Parse(_app.VersionNumber);
            List<string> successList = new List<string>(), failList = new List<string>();

            NoTransactionWithProgressBarHelper.Execute(files.Count, "批量修改族类型", (progress) =>
            {
                progress.UpdateMax(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    string fileName = Path.GetFileName(filePath);
                    progress.Update(i + 1, fileName);

                    if (!IsValidForProcessing(filePath, currentVersion, out string reason))
                    {
                        failList.Add($"{fileName} [跳过: {reason}]"); continue;
                    }

                    try
                    {
                        Document familyDoc = _app.OpenDocumentFile(filePath);
                        if (!familyDoc.IsFamilyDocument) { familyDoc.Close(false); failList.Add($"{fileName} [非族文件]"); continue; }
                        // 【修复核心Bug】：必须在正在修改的 familyDoc 内部开启事务，而不是外部的 doc
                        NewTransaction.Execute(familyDoc, "修改类型", () =>
                        {
                            familyDoc.OwnerFamily.FamilyCategory = Category.GetCategory(familyDoc, catItem.BuiltInCategory);
                            Parameter p = familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
                            if (p != null && !p.IsReadOnly) p.Set((int)partType);
                        });
                        // 【修复核心Bug】：执行完毕后必须保存 true
                        familyDoc.Close(true);
                        successList.Add(fileName);
                    }
                    catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                }
            });
            ShowBatchReport("批量修改族类型", files.Count, successList, failList);
        }
        public string TVCommandName3 { get; set; } = "批量转换为FBX格式";
        public ICommand TVCommand3 => new BaseBindingCommand(TVControl3);
        public void TVControl3(object obj)
        {
            ExecuteClose(null);
            var files = SelectRevitFiles("请选择需要导出的 Revit 模型", "Revit 文件 (*.rvt;*.rfa)|*.rvt;*.rfa");
            if (files.Count == 0) return;
            int currentVersion = int.Parse(_app.VersionNumber);
            List<string> successList = new List<string>(), failList = new List<string>();
            NoTransactionWithProgressBarHelper.Execute(files.Count, "准备批量转换 FBX...", (progress) =>
            {
                progress.UpdateMax(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    string fileName = Path.GetFileName(filePath);
                    string fileDir = Path.GetDirectoryName(filePath);
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    progress.Update(i + 1, fileName);
                    if (!IsValidForProcessing(filePath, currentVersion, out string reason))
                    {
                        failList.Add($"{fileName} [跳过: {reason}]"); continue;
                    }
                    try
                    {
                        // 【修复核心Bug】: FBX 导出完全支持后台静默文档处理，不需要在界面中弹窗激活！
                        Document doc = _app.OpenDocumentFile(filePath);
                        View3D exportView = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
                        if (exportView != null)
                        {
                            ViewSet viewSet = new ViewSet();
                            viewSet.Insert(exportView);
                            doc.Export(fileDir, nameWithoutExt, viewSet, new FBXExportOptions());
                            successList.Add(fileName);
                        }
                        else
                        {
                            failList.Add($"{fileName} [跳过: 未找到有效三维视图]");
                        }
                        // 只读操作，关闭时不保存
                        doc.Close(false);
                    }
                    catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                }
            });
            ShowBatchReport("批量转换 FBX", files.Count, successList, failList);
        }
        public string TVCommandName4 { get; set; } = "批量删除族文本属性";
        public ICommand TVCommand4 => new BaseBindingCommand(TVControl4);
        public void TVControl4(object obj)
        {
            ExecuteClose(null);
            var files = SelectRevitFiles("请选择要处理的族文件");
            if (files.Count == 0) return;
            int currentVersion = int.Parse(_app.VersionNumber);
            List<string> successList = new List<string>(), failList = new List<string>();
            NoTransactionWithProgressBarHelper.Execute(files.Count, "批量删除文字属性", (progress) =>
            {
                progress.UpdateMax(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    string fileName = Path.GetFileName(filePath);
                    progress.Update(i + 1, fileName);
                    if (!IsValidForProcessing(filePath, currentVersion, out string reason))
                    {
                        failList.Add($"{fileName} [跳过: {reason}]"); continue;
                    }
                    try
                    {
                        Document familyDoc = _app.OpenDocumentFile(filePath);
                        if (!familyDoc.IsFamilyDocument) { familyDoc.Close(false); continue; }
                        FamilyManager familyManager = familyDoc.FamilyManager;
                        var paramsToDelete = familyManager.GetParameters().Where(p =>
                            p.Definition is InternalDefinition id
                            && id.BuiltInParameter == BuiltInParameter.INVALID
                            && p.Definition.ParameterType.ToString() == "Text"
                        ).ToList();
                        if (paramsToDelete.Count == 0)
                        {
                            familyDoc.Close(false);
                            failList.Add($"{fileName} (跳过：未包含自定义文字属性)");
                            continue;
                        }
                        NewTransaction.Execute(familyDoc, "删除文字属性", () => { foreach (var p in paramsToDelete) familyManager.RemoveParameter(p); });
                        familyDoc.Close(true);
                        successList.Add($"{fileName} (成功：删除了 {paramsToDelete.Count} 个)");
                    }
                    catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                }
            });
            ShowBatchReport("批量删除族文本属性", files.Count, successList, failList);
        }
        // 关闭主窗口方法，定义一个委托
        public Action CloseAction { get; set; }
        private void ExecuteClose(object obj) => CloseAction?.Invoke();
    }
}
