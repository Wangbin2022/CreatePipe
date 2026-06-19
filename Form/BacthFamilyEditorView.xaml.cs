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
using System.Windows;
using System.Windows.Input;
using Binding = Autodesk.Revit.DB.Binding;

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
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
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

            var partTypeMap = new Dictionary<CategoryItemWrapper, List<PartType>>
        {
                        { new CategoryItemWrapper("常规模型", -2000151), new List<PartType> { PartType.Normal} },
                        { new CategoryItemWrapper("专用设备", -2001350), new List<PartType> { PartType.Normal} },
            { new CategoryItemWrapper("风道末端", -2008013), new List<PartType> { PartType.Normal} },
            { new CategoryItemWrapper("桥架配件", -2008126), new List<PartType> { PartType.ChannelCableTrayCross, PartType.ChannelCableTrayElbow, PartType.ChannelCableTrayMultiPort, PartType.ChannelCableTrayOffset, PartType.ChannelCableTrayTee, PartType.ChannelCableTrayTransition, PartType.ChannelCableTrayUnion, PartType.ChannelCableTrayVerticalElbow, PartType.LadderCableTrayCross, PartType.LadderCableTrayElbow, PartType.LadderCableTrayMultiPort, PartType.LadderCableTrayOffset, PartType.LadderCableTrayTee, PartType.LadderCableTrayTransition, PartType.LadderCableTrayUnion, PartType.LadderCableTrayVerticalElbow } },
            { new CategoryItemWrapper("通讯设备", -2008081), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItemWrapper("线管配件", -2008128), new List<PartType> { PartType.Cap,PartType.Cross, PartType.Elbow, PartType.JunctionBoxElbow, PartType.MultiPort, PartType.Tee, PartType.Transition, PartType.Union } },
            { new CategoryItemWrapper("数据设备", -2008083), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItemWrapper("风管附件", -2008016), new List<PartType> { PartType.AttachesTo, PartType.BreaksInto, PartType.Damper } },
            { new CategoryItemWrapper("风管管件", -2008010), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.LateralCross, PartType.LateralTee, PartType.MultiPort, PartType.Offset, PartType.Pants, PartType.TapAdjustable, PartType.TapPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
            { new CategoryItemWrapper("电气设备", -2001040), new List<PartType> { PartType.EquipmentSwitch, PartType.OtherPanel, PartType.PanelBoard, PartType.SwitchBoard, PartType.Transformer } },
            { new CategoryItemWrapper("电气装置", -2001060), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItemWrapper("火灾报警设备", -2008085), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItemWrapper("照明设备（开关）", -2008087), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItemWrapper("照明设备 (灯具)", -2001120), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
            { new CategoryItemWrapper("机械设备", -2001140), new List<PartType> { PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor, PartType.Normal, PartType.ValveBreaksInto } },
            { new CategoryItemWrapper("护士呼叫设备", -2008077), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItemWrapper("管道附件", -2008055), new List<PartType> { PartType.Normal, PartType.AttachesTo, PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor,  PartType.Sensor, PartType.ValveBreaksInto, PartType.ValveNormal } },
            { new CategoryItemWrapper("管道管件", -2008049), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.PipeFlange, PartType.LateralCross, PartType.LateralTee, PartType.PipeMechanicalCoupling, PartType.MultiPort, PartType.SpudAdjustable, PartType.SpudPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
            { new CategoryItemWrapper("卫浴装置", -2001160), new List<PartType> { PartType.Normal } },
            { new CategoryItemWrapper("安防设备", -2008079), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
            { new CategoryItemWrapper("喷头", -2008099), new List<PartType> { PartType.Normal } },
            { new CategoryItemWrapper("电话设备", -2008075), new List<PartType> { PartType.Normal, PartType.JunctionBox } }
                    };

            var dialog = new UniversalDoubleComboboxWindow("设置族参数", "1. 请选择族类别:", "2. 请选择零件类型:", partTypeMap);
            if (dialog.ShowDialog() != true) return;
            ExternalHandler.Run(app =>
            {
                CategoryItemWrapper catItem = (CategoryItemWrapper)dialog.SelectedItem1;
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
                            using (Transaction tx = new Transaction(familyDoc, "修改类型属性"))
                            {
                                tx.Start();
                                if (!familyDoc.IsFamilyDocument) { familyDoc.Close(false); failList.Add($"{fileName} [非族文件]"); return; }
                                familyDoc.OwnerFamily.FamilyCategory = Category.GetCategory(familyDoc, catItem.BuiltInCategory);
                                Parameter p = familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
                                if (p != null && !p.IsReadOnly) p.Set((int)partType);
                                // 【修复核心Bug】：执行完毕后必须保存 true
                                tx.Commit();
                                familyDoc.Close(true);
                            }
                            successList.Add(fileName);
                        }
                        catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                    }
                });
                ShowBatchReport("批量修改族类型", files.Count, successList, failList);
            });
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
            ExternalHandler.Run(app =>
            {
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
                            // 【关键修改】在这里使用标准的 Transaction，而不是帮助类，以获得更好的控制
                            using (Transaction tx = new Transaction(familyDoc, "删除文字属性"))
                            {
                                tx.Start();

                                FamilyManager familyManager = familyDoc.FamilyManager;
                                var paramsToDelete = familyManager.GetParameters().Where(p =>
                                    p.Definition is InternalDefinition id
                                    && id.BuiltInParameter == BuiltInParameter.INVALID
                                    && p.Definition.ParameterType.ToString() == "Text"
                                ).ToList();

                                if (paramsToDelete.Count == 0)
                                {
                                    failList.Add($"{fileName} (跳过：未包含自定义文字属性)");
                                    tx.RollBack(); // 回滚空事务
                                }
                                else
                                {
                                    foreach (var p in paramsToDelete)
                                    {
                                        familyManager.RemoveParameter(p);
                                    }
                                    successList.Add($"{fileName} (成功：删除了 {paramsToDelete.Count} 个)");
                                    tx.Commit();
                                    familyDoc.Save(); // 只有成功提交后才保存
                                }
                            }
                        }
                        catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                    }
                });
                ShowBatchReport("批量删除族文本属性", files.Count, successList, failList);
            });
        }
        public string TVCommandName5 { get; set; } = "批量添加属性";
        public ICommand TVCommand5 => new BaseBindingCommand(TVControl5);
        public void TVControl5(object obj)
        {
            ExecuteClose(null);
            var files = SelectRevitFiles("请选择需要添加属性的 Revit 模型", "Revit 文件 (*.rvt;*.rfa)|*.rvt;*.rfa");
            if (files.Count == 0) return;
            ////输入参数名称
            UniversalNewListString universalNewListString = new UniversalNewListString("请输入待建立属性，以分号分隔");
            if (universalNewListString.ShowDialog() == false) return;
            List<string> resultNames = universalNewListString.ViewModel.NewName;
            //选择参数类型
            var parameterTypeOptions = new List<ParameterTypeWrapper>
            {
            new ParameterTypeWrapper { DisplayName = "文本", Value = ParameterType.Text },
            new ParameterTypeWrapper { DisplayName = "数值", Value = ParameterType.Number},
            new ParameterTypeWrapper { DisplayName = "材质", Value = ParameterType.Material},
            new ParameterTypeWrapper { DisplayName = "URL", Value = ParameterType.URL},
            new ParameterTypeWrapper { DisplayName = "是否", Value = ParameterType.YesNo }
            };
            var dataMap = new Dictionary<string, List<ParameterTypeWrapper>>
            {                { "类型属性", parameterTypeOptions },            { "实例属性", parameterTypeOptions }        };

            var window = new UniversalDoubleComboboxWindow("设置属性参数", "1. 请选择属性类别:", "2. 请选择参数类型:", dataMap);
            if (window.ShowDialog() != true) return;
            // 获取第一个下拉框的选中项 (这是一个string)
            string selectedScope = window.SelectedItem1 as string;
            bool isInstancePara = (selectedScope == "实例属性");
            // 获取第二个下拉框的选中项 (这是一个ParameterTypeWrapper对象)
            ParameterTypeWrapper selectedWrapper = window.SelectedItem2 as ParameterTypeWrapper;
            // 安全检查
            if (selectedScope == null || selectedWrapper == null) return;
            ParameterType finalParameterType = selectedWrapper.Value;
            ExternalHandler.Run(app =>
            {
                int currentVersion = int.Parse(_uiApp.Application.VersionNumber);
                List<string> successList = new List<string>(), failList = new List<string>();
                NoTransactionWithProgressBarHelper.Execute(files.Count, "准备批量添加属性", (progress) =>
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
                        Document openedDoc = null; // 用于确保文档能被关闭
                        try
                        {
                            openedDoc = _uiApp.Application.OpenDocumentFile(filePath);
                            using (Transaction tx = new Transaction(openedDoc, "新建共享参数属性"))
                            {
                                tx.Start();
                                if (openedDoc.IsFamilyDocument)
                                {
                                    SharedParaFactory.CreateFamilyPara(openedDoc, resultNames, isInstancePara, finalParameterType);
                                }
                                else
                                {
                                    SharedParaFactory.CreateProjectPara(openedDoc, resultNames, isInstancePara, finalParameterType);
                                }
                                tx.Commit();
                                openedDoc.Save();
                            }
                            successList.Add(fileName);
                        }
                        catch (Exception ex) { failList.Add($"{fileName} [失败: {ex.Message}]"); }
                        finally
                        {
                            // 【关键修正】确保无论成功或失败，打开的文档都会被关闭
                            if (openedDoc != null)
                            {
                                openedDoc.Close(false); // false表示不保存更改，因为我们已经在事务成功后手动保存了
                            }
                        }
                    }
                });
                ShowBatchReport("批量新建族属性处理完成", files.Count, successList, failList);
                //string finalReport = $"批量处理完成！\n\n" +
                //         $"成功处理 {successList.Count} 个文件:\n" +
                //         $"{(successList.Any() ? string.Join("\n", successList) : "无")}\n\n" +
                //         $"失败或跳过 {failList.Count} 个文件:\n" +
                //         $"{(failList.Any() ? string.Join("\n", failList) : "无")}";
                //TaskDialog.Show("处理报告", finalReport);
            });
        }
        public static class DefinitionInfo
        {
            private static DefinitionFile _instance;
            public static DefinitionFile GetInstance(Document doc)
            {
                //// 每次修改前最好记录原始的共享参数文件路径（Revit全局唯一的），用完后可以考虑恢复
                try
                {
                    string path = SharedParaFactory.EnsureSharedParameterFile();
                    doc.Application.SharedParametersFilename = path;
                    _instance = doc.Application.OpenSharedParameterFile();
                    return _instance;
                }
                catch
                {
                    return null;
                }
            }
        }
        public static class SharedParaFactory
        {
            public static void CreateFamilyPara(Document doc, List<string> names, bool isInstancePara, ParameterType paraType)
            {
                if (!doc.IsFamilyDocument) return;
                FamilyManager familyManager = doc.FamilyManager;
                // 1. 获取族内所有已存在的参数名称，用于后续检查，效率高
                var existingParaNames = new HashSet<string>(familyManager.Parameters.Cast<FamilyParameter>().Select(p => p.Definition.Name));
                // 2. 准备共享参数文件和组
                DefinitionFile definitionFile = DefinitionInfo.GetInstance(doc);
                if (definitionFile == null)
                {
                    TaskDialog.Show("错误", "无法加载或创建共享参数文件。");
                    return;
                }
                DefinitionGroup group = definitionFile.Groups.get_Item("CACC") ?? definitionFile.Groups.Create("CACC");
                var createdParams = new List<string>();
                var skippedParams = new List<string>();
                // 3. 遍历用户输入的每一个参数名
                foreach (string paraName in names)
                {
                    if (string.IsNullOrWhiteSpace(paraName)) continue;
                    // 4. 【核心检查】检查参数是否已存在于族中
                    if (existingParaNames.Contains(paraName))
                    {
                        skippedParams.Add(paraName);
                        continue; // 如果已存在，则跳过此参数
                    }
                    // 5. 在共享参数文件中查找或创建参数定义 (Definition)
                    ExternalDefinition definition = group.Definitions.get_Item(paraName) as ExternalDefinition;
                    if (definition == null)
                    {
                        var options = new ExternalDefinitionCreationOptions(paraName, paraType);
                        definition = group.Definitions.Create(options) as ExternalDefinition;
                    }
                    // 6. 【核心创建】使用 FamilyManager 将参数添加到族中
                    try
                    {
                        // PG_DATA 是一个通用的好选择
                        familyManager.AddParameter(definition, BuiltInParameterGroup.PG_DATA, isInstancePara);
                        createdParams.Add(paraName);
                    }
                    catch (System.Exception ex)
                    {
                        // 如果添加失败（例如，类型不匹配等），记录并继续
                        skippedParams.Add($"{paraName} (创建失败: {ex.Message})");
                    }
                }
                // 7. 向用户反馈结果
                string report = $"操作完成。\n\n成功创建参数：\n{(createdParams.Any() ? string.Join("\n", createdParams) : "无")}\n\n" +
                                $"跳过或失败的参数(已存在或创建失败)：\n{(skippedParams.Any() ? string.Join("\n", skippedParams) : "无")}";
                TaskDialog.Show("结果报告", report);
            }
            ////重载不考虑类别，仅建立属性
            public static void CreateProjectPara(Document doc, List<string> names, bool isInstancePara, ParameterType paraType)
            {
                if (doc.IsFamilyDocument) return;
                // 1. 准备共享参数文件和组
                DefinitionFile definitionFile = DefinitionInfo.GetInstance(doc);
                if (definitionFile == null)
                {
                    TaskDialog.Show("错误", "无法加载或创建共享参数文件。");
                    return;
                }
                DefinitionGroup group = definitionFile.Groups.get_Item("CACC") ?? definitionFile.Groups.Create("CACC");
                // 2. 一次性获取项目中所有已绑定参数的名称，存入HashSet以提高查询效率
                var existingParams = new HashSet<string>();
                var proIterator = doc.ParameterBindings.ForwardIterator();
                while (proIterator.MoveNext())
                {
                    existingParams.Add(proIterator.Key.Name);
                }
                var categorySet = doc.Application.Create.NewCategorySet();
                Category category = Category.GetCategory(doc, BuiltInCategory.OST_Walls);
                if (category != null)
                {
                    categorySet.Insert(category);
                }
                // 3. 根据'isInstancePara'创建合适的Binding对象。默认绑定给墙，因为空值或BuiltInCategory.Invalid都会失败
                Binding binding = isInstancePara ? (Binding)doc.Application.Create.NewInstanceBinding(categorySet)
                    : (Binding)doc.Application.Create.NewTypeBinding(categorySet);
                var createdParams = new List<string>();
                var skippedParams = new List<string>();
                // 4. 遍历用户输入的每一个参数名
                foreach (var paraName in names)
                {
                    if (string.IsNullOrWhiteSpace(paraName)) continue;
                    // 5. 【核心检查】如果项目中已存在此参数，则直接跳过
                    if (existingParams.Contains(paraName))
                    {
                        skippedParams.Add(paraName);
                        continue;
                    }
                    // 6. 在共享参数文件中查找或创建参数定义 (Definition)
                    Definition definition = group.Definitions.get_Item(paraName);
                    if (definition == null)
                    {
                        // 【关键修复】使用传入的 paraType，并转换为现代API
                        //                // 注意：ParameterType.Text 在 Revit 2022+ 中已过时，若您使用新版需改为 SpecTypeId.String.Text
                        var options = new ExternalDefinitionCreationOptions(paraName, paraType);
                        //var options = new ExternalDefinitionCreationOptions(definitionName, SpecTypeId.String.Text);
                        //ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paraName, GetSpecTypeId(paraType));
                        definition = group.Definitions.Create(options);
                    }
                    // 7. 【核心绑定】将定义与Binding一起插入到项目中
                    try
                    {
                        // PG_DATA 是一个通用的好选择，显示在“数据”组下
                        if (doc.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_DATA))
                        {
                            createdParams.Add(paraName);
                        }
                        else
                        {
                            skippedParams.Add($"{paraName} (绑定失败)");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        skippedParams.Add($"{paraName} (绑定异常: {ex.Message})");
                    }
                }

                // 8. 向用户反馈结果
                string report = $"项目参数操作完成。\n\n成功创建参数：\n{(createdParams.Any() ? string.Join("\n", createdParams) : "无")}\n\n" +
                                $"跳过或失败的参数(已存在或绑定失败)：\n{(skippedParams.Any() ? string.Join("\n", skippedParams) : "无")}";
                TaskDialog.Show("结果报告", report);
            }
            // 确保共享参数文件存在，并返回文件绝对路径
            public static string EnsureSharedParameterFile()
            {
                // 推荐放在插件所在目录 或 系统的 Temp 目录，避免没有写入权限
                string directoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string shareFilePath = Path.Combine(directoryPath, "RevitSharedParameters.txt");
                if (!File.Exists(shareFilePath))
                {
                    //只有当文件不存在时，才创建并写入模板内容 纯净的内容模板
                    string content = "# This is a Revit shared parameter file.\r\n" +
                                 "# Do not edit manually.\r\n" +
                                 "*META\tVERSION\tMINVERSION\r\n" +
                                 "META\t2\t1\r\n" +
                                 "*GROUP\tID\tNAME\r\n" +
                                 "GROUP\t1\tGroup1\r\n" +
                                 "*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\r\n" +
                                 "PARAM\t858bd7ed-5acf-4d20-9d7c-31269a0c0e9a\tShared_Length\tLENGTH\t\t1\t1";
                    // 优化：File.WriteAllText 会自动判断，如果文件不存在则创建，如果存在则直接覆盖。
                    // 无需手动 File.Create -> Close -> File.WriteAllBytes 这么繁琐，且不会抛出流占用的异常。
                    File.WriteAllText(shareFilePath, content, Encoding.UTF8);
                }
                return shareFilePath;
            }

            ///// <summary>
            ///// 删除项目中所有的自定义参数绑定 (警告：危险操作，会清空所有参数)
            ///// </summary>
            //public static void DeletePara(Document doc)
            //{
            //    var proMap = doc.ParameterBindings;
            //    var proIterator = proMap.ForwardIterator();
            //    var defsToDelete = new List<Definition>();
            //    // 先收集所有需要删除的定义
            //    while (proIterator.MoveNext())
            //    {
            //        defsToDelete.Add(proIterator.Key);
            //    }
            //    // 然后集中移除
            //    foreach (var def in defsToDelete)
            //    {
            //        proMap.Remove(def);
            //    }
            //}
        }
        // 关闭主窗口方法，定义一个委托
        public Action CloseAction { get; set; }
        private void ExecuteClose(object obj) => CloseAction?.Invoke();
    }
    //内置类展示
    public class CategoryItemWrapper
    {
        public string Name { get; set; }
        public BuiltInCategory BuiltInCategory { get; set; }
        public CategoryItemWrapper(string name, int categoryId)
        {
            Name = name;
            // 将整数 ID 强转为 Revit 的 BuiltInCategory 枚举
            BuiltInCategory = (BuiltInCategory)categoryId;
        }
        // 【魔法就在这里】：WPF 的 ComboBox 默认会调用对象的 ToString() 作为界面显示的文本
        public override string ToString() { return Name; }
    }
    public class ParameterTypeWrapper
    {
        public string DisplayName { get; set; }
        public ParameterType Value { get; set; }
        public override string ToString()
        {
            // 直接返回DisplayName属性即可
            return DisplayName;
        }
    }
}
