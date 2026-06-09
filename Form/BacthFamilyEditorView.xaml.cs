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
        private Document doc;
        private UIDocument uiDoc;
        private UIApplication uIApplication;
        private View activeView;
        public BacthFamilyEditorViewModel(UIApplication uiApp)
        {
            uIApplication = uiApp;
            uiDoc = uiApp.ActiveUIDocument;
            doc = uiDoc.Document;
            activeView = uiApp.ActiveUIDocument.ActiveView;
        }
        public string TVCommandName1 { get; set; } = "批量升级族版本";
        public ICommand TVCommand1 => new BaseBindingCommand(TVControl1);
        public void TVControl1(object obj)
        {
            ExecuteClose(null);
            // 1. 使用 Win32.OpenFileDialog 选择多个文件
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "请选择需要转换的 Revit 模型（按住 Ctrl/Shift 可多选）",
                Filter = "Revit 文件 (*.rvt)|*.rvt",
                Multiselect = true, // 关键：允许选中多个文件
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            // 如果用户关闭对话框或取消选择，直接结束命令
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            string[] selectedFiles = openFileDialog.FileNames;
            if (selectedFiles.Length == 0) return;

            for (int i = 0; i < selectedFiles.Length; i++)
            {
                string filePath = selectedFiles[i];
                string fileName = Path.GetFileName(filePath);
                string fileDir = Path.GetDirectoryName(filePath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                try
                {
                    // 在主窗口激活并打开文档
                    UIDocument newUiDoc = uIApplication.OpenAndActivateDocument(filePath);
                    Document currentDoc = newUiDoc.Document;
                    if (!currentDoc.IsFamilyDocument) return;
                    BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileName);
                    if (basicFileInfo.IsSavedInLaterVersion) return;
                    var doc = uIApplication.Application.OpenDocumentFile(fileName);
                    doc.Close();
                }
                catch (Exception) { throw; }
            }
        }
        public string TVCommandName2 { get; set; } = "批量修改族类型";
        public ICommand TVCommand2 => new BaseBindingCommand(TVControl2);
        public void TVControl2(object obj)
        {
            ExecuteClose(null);
            // 1. 使用 Win32.OpenFileDialog 选择多个文件
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "请选择需要转换的 Revit 模型（按住 Ctrl/Shift 可多选）",
                Filter = "Revit 文件 (*.rvt)|*.rvt",
                Multiselect = true, // 关键：允许选中多个文件
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            // 如果用户关闭对话框或取消选择，直接结束命令
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            string[] selectedFiles = openFileDialog.FileNames;
            if (selectedFiles.Length == 0) return;

            //// 1. 准备你的映射字典,构建数据源 (Key 是 CategoryItem 对象，Value 是 PartType 集合)
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
            try
            {

                ////// 2. 实例化通用窗口，并传入标题、提示文本和数据字典
                var dialog = new UniversalDoubleComboboxWindow(
                    windowTitle: "设置族参数", header1: "1. 请选择族类别 (Family Category):",
                    header2: "2. 请选择零件类型 (PartType):", dataMap: partTypeMap);
                if (dialog.ShowDialog() == true)
                {
                    // 1. 获取选中的目标类别和目标 PartType
                    CategoryItem selectedCatItem = (CategoryItem)dialog.SelectedItem1;
                    BuiltInCategory targetRevitCat = selectedCatItem.BuiltInCategory;
                    PartType selectedPartType = (PartType)dialog.SelectedItem2;
                    // 2. 安全地获取当前的 族类别ID 和 PartType值
                    int currentCatId = doc.OwnerFamily.FamilyCategory?.Id.IntegerValue ?? -1;
                    Parameter partTypeParam = doc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
                    // 如果该参数存在，则获取其整型值，否则给个默认值 -1
                    int currentPartType = partTypeParam != null ? partTypeParam.AsInteger() : -1;
                    // 3. 判断是否与当前完全一致
                    if (currentCatId == (int)targetRevitCat && currentPartType == (int)selectedPartType)
                    {
                        TaskDialog.Show("提示", "当前族已是所选择类型，无需转换");
                        return; // 既然无需操作，返回 Cancelled 或 Succeeded 都可以
                    }
                    // 4. 执行转换事务
                    NewTransaction.Execute(doc, "修改族类型", () =>
                    {
                        for (int i = 0; i < selectedFiles.Length; i++)
                        {
                            string filePath = selectedFiles[i];
                            string fileName = Path.GetFileName(filePath);
                            string fileDir = Path.GetDirectoryName(filePath);
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                            try
                            {
                                //// 在主窗口激活并打开文档
                                UIDocument newUiDoc = uIApplication.OpenAndActivateDocument(filePath);
                                Document currentDoc = newUiDoc.Document;
                                if (!currentDoc.IsFamilyDocument) return;
                                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileName);
                                if (basicFileInfo.IsSavedInLaterVersion) return;
                                var doc = uIApplication.Application.OpenDocumentFile(fileName);
                                doc.OwnerFamily.FamilyCategory = Category.GetCategory(doc, targetRevitCat);
                                // 注意：修改类别后，PartType 参数可能才出现，所以必须重新获取一遍！！！
                                Parameter newPartTypeParam = doc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
                                if (newPartTypeParam != null && !newPartTypeParam.IsReadOnly)
                                {
                                    newPartTypeParam.Set((int)selectedPartType);
                                }
                                doc.Close();
                            }
                            catch (Exception) { throw; }
                        }
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }







        }
        public string TVCommandName3 { get; set; } = "批量转换为FBX格式";
        public ICommand TVCommand3 => new BaseBindingCommand(TVControl3);
        public void TVControl3(object obj)
        {
            ExecuteClose(null);
            // 1. 使用 Win32.OpenFileDialog 选择多个文件
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "请选择需要转换的 Revit 模型（按住 Ctrl/Shift 可多选）",
                Filter = "Revit 文件 (*.rvt)|*.rvt",
                Multiselect = true, // 关键：允许选中多个文件
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            // 如果用户关闭对话框或取消选择，直接结束命令
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            string[] selectedFiles = openFileDialog.FileNames;
            if (selectedFiles.Length == 0) return;
            // 用于记录处理结果的清单
            List<string> successList = new List<string>();
            List<string> failList = new List<string>();
            // 2. 使用封装的无事务进度条执行核心逻辑
            Result processResult = NoTransactionWithProgressBarHelper.Execute(selectedFiles.Length, "准备批量转换 FBX...", (progress) =>
            {
                for (int i = 0; i < selectedFiles.Length; i++)
                {
                    string filePath = selectedFiles[i];
                    string fileName = Path.GetFileName(filePath);
                    string fileDir = Path.GetDirectoryName(filePath);
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    try
                    {
                        // 在主窗口激活并打开文档
                        UIDocument newUiDoc = uIApplication.OpenAndActivateDocument(filePath);
                        Document currentDoc = newUiDoc.Document;
                        // 【注意】这里假设你的进度条有 Update 方法。
                        // 如果你的方法叫 SetProgress / StepIt 等，请修改此处
                        progress.Update(i + 1, fileName);
                        // 查找有效的三维视图 (排除视图模板)
                        View3D exportView = new FilteredElementCollector(currentDoc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
                        if (exportView != null)
                        {
                            ViewSet viewSet = new ViewSet();
                            viewSet.Insert(exportView);
                            FBXExportOptions options = new FBXExportOptions();
                            // 导出文件
                            currentDoc.Export(fileDir, nameWithoutExt, viewSet, options);
                            successList.Add(fileName);
                        }
                        else
                        {
                            failList.Add($"{fileName} [跳过: 未找到三维视图]");
                        }
                    }
                    catch (Exception ex)
                    {
                        failList.Add($"{fileName} [失败: {ex.Message}]");
                    }
                }
            });
            if (processResult == Result.Succeeded)
            {
                ShowResultDialog3(selectedFiles.Length, successList, failList);
            }
        }
        private void ShowResultDialog3(int totalCount, List<string> successList, List<string> failList)
        {
            string resultMessage = $"共选中 {totalCount} 个文件。\n\n";

            // 成功清单展示
            resultMessage += $"成功导出 ({successList.Count} 个)：\n";
            if (successList.Count > 0)
            {
                // 为防止选了几百个文件导致对话框文字超限，这里限制最多显示前20个
                var displaySuccess = successList.Take(20).ToList();
                resultMessage += string.Join("\n", displaySuccess);
                if (successList.Count > 20) resultMessage += "\n... (省略显示更多)";
            }
            else
            {
                resultMessage += "无\n";
            }
            resultMessage += "\n\n";
            // 失败/跳过清单展示
            resultMessage += $"失败或跳过 ({failList.Count} 个)：\n";
            if (failList.Count > 0)
            {
                var displayFail = failList.Take(20).ToList();
                resultMessage += string.Join("\n", displayFail);
                if (failList.Count > 20) resultMessage += "\n... (省略显示更多)";
            }
            else
            {
                resultMessage += "无";
            }
            // 调用 Revit 原生 TaskDialog 显示
            TaskDialog resultDialog = new TaskDialog("批量转换 FBX 报告")
            {
                MainInstruction = "批量处理任务已结束",
                MainContent = resultMessage
            };
            resultDialog.Show();
        }
        public string TVCommandName4 { get; set; } = "批量删除族文本属性";
        public ICommand TVCommand4 => new BaseBindingCommand(TVControl4);
        public void TVControl4(object obj)
        {
            ExecuteClose(null);
            Autodesk.Revit.ApplicationServices.Application app = uIApplication.Application;
            // 获取当前 Revit 的版本号 (如 "2024")
            int currentRevitVersion = int.Parse(app.VersionNumber);
            List<string> selectedFiles = new List<string>();
            // 1. 使用 Win32 OpenFileDialog 实现多选文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择要处理的族文件（可按住 Ctrl/Shift 多选）";
            openFileDialog.Filter = "Revit 族文件 (*.rfa)|*.rfa";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() != true)
            {
                return ;
            }
            selectedFiles = openFileDialog.FileNames.ToList();
            if (selectedFiles.Count == 0) return ;
            List<string> successList = new List<string>();
            List<string> failList = new List<string>(); // 包含了失败和跳过的记录
                                                        // 2. 遍历处理文件（后台静默处理）
            NoTransactionWithProgressBarHelper.Execute(selectedFiles.Count, "批量删除文字属性", (service) =>
            {
                service.UpdateMax(selectedFiles.Count());
                int index = 0;
                foreach (string filePath in selectedFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    service.Update(++index, filePath);
                    try
                    {
                        // 3. 版本检查防崩溃
                        BasicFileInfo fileInfo = BasicFileInfo.Extract(filePath);
                        if (int.TryParse(fileInfo.Format, out int fileVersion))
                        {
                            if (fileVersion > currentRevitVersion)
                            {
                                failList.Add($"{fileName} (跳过：文件版本 {fileVersion} 高于当前 Revit 版本)");
                                continue;
                            }
                        }
                        // 4. 后台静默打开文档
                        Document familyDoc = app.OpenDocumentFile(filePath);
                        if (!familyDoc.IsFamilyDocument)
                        {
                            familyDoc.Close(false);
                            failList.Add($"{fileName} (跳过：不是族文件)");
                            continue;
                        }
                        FamilyManager familyManager = familyDoc.FamilyManager;
                        List<FamilyParameter> parameters = familyManager.GetParameters().ToList();
                        // 专门用于存放需要删除的参数的列表
                        List<FamilyParameter> paramsToDelete = new List<FamilyParameter>();
                        // 5. 遍历并收集符合条件的参数
                        foreach (FamilyParameter param in parameters)
                        {
                            Definition definition = param.Definition;
                            if (definition is InternalDefinition internalDef &&
                                internalDef.BuiltInParameter == BuiltInParameter.INVALID)
                            {
                                // 【兼容多版本】判断是否为文字类型
                                bool isTextParam = false;
                                if (currentRevitVersion >= 2022)
                                {
                                    //// Revit 2022 及以上版本的新 API 判定方式
                                    //ForgeTypeId dataType = definition.GetDataType();
                                    //if (dataType == SpecTypeId.String.Text)
                                    //{
                                    //    isTextParam = true;
                                    //}
                                }
                                else
                                {
                                    // Revit 2021 及以下版本的老 API 判定方式 (使用反射或ToString规避编译报错)
#pragma warning disable CS0618
                                    if (definition.ParameterType.ToString() == "Text")
                                    {
                                        isTextParam = true;
                                    }
#pragma warning restore CS0618
                                }
                                if (isTextParam)
                                {
                                    paramsToDelete.Add(param);
                                }
                            }
                        }
                        // 6. 核心逻辑调整：判断是否包含文字属性
                        if (paramsToDelete.Count == 0)
                        {
                            // 没有找到任何需要删除的文字属性，直接不保存关闭！
                            familyDoc.Close(false);
                            failList.Add($"{fileName} (跳过：未包含自定义文字属性)");
                            continue; // 进入下一个文件
                        }
                        // 7. 找到了文字属性，开启事务进行删除
                        using (Transaction trans = new Transaction(familyDoc, "批量删除文字属性"))
                        {
                            trans.Start();
                            foreach (FamilyParameter paramToDelete in paramsToDelete)
                            {
                                familyManager.RemoveParameter(paramToDelete);
                            }
                            trans.Commit();
                        }
                        // 8. 保存并关闭后台文档
                        familyDoc.Close(true);
                        successList.Add($"{fileName} (成功：删除了 {paramsToDelete.Count} 个文字属性)");
                    }
                    catch (Exception ex)
                    {
                        failList.Add($"{fileName} (报错：{ex.Message})");
                    }
                }
            });
            // 9. 调用统计弹窗
            ShowResultDialog4(selectedFiles.Count, successList, failList);
        }
        private void ShowResultDialog4(int totalCount, List<string> successList, List<string> failList)
        {
            string resultMessage = $"共选中 {totalCount} 个文件。\n\n";

            // 成功清单展示
            resultMessage += $"处理成功 ({successList.Count} 个)：\n";
            if (successList.Count > 0)
            {
                var displaySuccess = successList.Take(15).ToList();
                resultMessage += string.Join("\n", displaySuccess);
                if (successList.Count > 15) resultMessage += "\n... (省略显示更多)";
            }
            else
            {
                resultMessage += "无\n";
            }
            resultMessage += "\n\n";

            // 失败/跳过清单展示
            resultMessage += $"失败或跳过 ({failList.Count} 个)：\n";
            if (failList.Count > 0)
            {
                var displayFail = failList.Take(15).ToList();
                resultMessage += string.Join("\n", displayFail);
                if (failList.Count > 15) resultMessage += "\n... (省略显示更多)";
            }
            else
            {
                resultMessage += "无";
            }

            TaskDialog resultDialog = new TaskDialog("批量处理报告")
            {
                MainInstruction = "批量删除族文字属性已完成！",
                MainContent = resultMessage
            };
            resultDialog.Show();
        }
        // 关闭主窗口方法，定义一个委托
        public Action CloseAction { get; set; }
        private void ExecuteClose(Object obj)
        {
            // 执行委托
            CloseAction?.Invoke();
        }
    }
}
