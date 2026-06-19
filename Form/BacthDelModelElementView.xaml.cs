using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// BacthDelModelElement.xaml 的交互逻辑
    /// </summary>
    public partial class BacthDelModelElementView : Window
    {
        public BacthDelModelElementView(UIApplication uIApplication)
        {
            InitializeComponent();
            var vm = new BacthDelModelElementViewModel(uIApplication);
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
    //批量删除线型，族文字属性、分析模型
    //点击按钮关闭当前窗体，开启新的
    public class BacthDelModelElementViewModel : ObserverableObject
    {
        private Document doc;
        private UIDocument uiDoc;
        private UIApplication uIApplication;
        private View activeView;
        public BacthDelModelElementViewModel(UIApplication uiApp)
        {
            uIApplication = uiApp;
            uiDoc = uiApp.ActiveUIDocument;
            doc = uiDoc.Document;
            activeView = uiApp.ActiveUIDocument.ActiveView;
        }
        public string TVCommandName1 { get; set; } = "批量清理线形图案";
        public ICommand TVCommand1 => new BaseBindingCommand(TVControl1);
        public void TVControl1(object obj)
        {
            ExecuteClose(null);
            FilteredElementCollector elements3 = new FilteredElementCollector(doc);
            List<LinePatternElement> linePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            // 准备显示用的字符串列表（显示线型图案名称）
            List<string> linePatternNames = linePatternElements.Select(lpe => lpe.Name).ToList();
            // 创建多选窗体
            UniversalComboBoxMultiSelection selectionDialog = new UniversalComboBoxMultiSelection(
                linePatternNames, "请选要删除的线型图案（多选）");
            // 显示窗体并等待用户确认
            bool? result = selectionDialog.ShowDialog();
            // 处理选择结果
            if (result == true && selectionDialog.SelectedResult.Any())
            {
                // 根据用户选择的名称，获取对应的 LinePatternElement 对象
                List<LinePatternElement> selectedLinePatterns = linePatternElements.Where(x => x.Id.IntegerValue > 0)
                    .Where(lpe => selectionDialog.SelectedResult.Contains(lpe.Name)).ToList();
                TransactionWithProgressBarHelper.Execute(doc, "删除线型", (service) =>
                {
                    service.UpdateMax(selectedLinePatterns.Count());
                    int index = 0;
                    foreach (var item in selectedLinePatterns)
                    {
                        service.Update(++index, item.Name);
                        doc.Delete(item.Id);
                    }
                });
                //// 输出选择结果
                TaskDialog.Show("选择结果", $"已删除 {selectedLinePatterns.Count} 个线型图案。");
            }
            else
            {
                TaskDialog.Show("提示", "未选择任何线型图案或已取消");
            }
        }
        public string TVCommandName2 { get; set; } = "批量清理当前族属性";
        public ICommand TVCommand2 => new BaseBindingCommand(TVControl2);
        public void TVControl2(object obj)
        {
            ExecuteClose(null);
            Document familyDoc = doc;

            if (!familyDoc.IsFamilyDocument)
            {
                TaskDialog.Show("提示", "当前文档不是族文件");
                return;
            }

            FamilyManager familyManager = familyDoc.FamilyManager;
            List<FamilyParameter> parameters = familyManager.GetParameters().ToList();
            List<FamilyParameter> paramsToDelete = new List<FamilyParameter>();

            // 1. 遍历并收集符合条件的参数
            foreach (FamilyParameter param in parameters)
            {
                Definition definition = param.Definition;

                // 【修复 1】：涵盖“普通族参数”和“共享参数”
                bool isCustomParameter = false;

                if (param.IsShared)
                {
                    isCustomParameter = true; // 共享参数必然是自定义的
                }
                else if (definition is InternalDefinition internalDef && internalDef.BuiltInParameter == BuiltInParameter.INVALID)
                {
                    isCustomParameter = true; // 非内置的普通参数
                }

                if (isCustomParameter)
                {
                    bool isTextParam = false;

                    // 🔥 核心修复：使用“特征嗅探”代替版本号判断
                    try
                    {
                        // 方案 A：优先寻找 ParameterType 属性 (适用于 Revit 2022 及更早版本，2022 100% 能走通这里)
                        System.Reflection.PropertyInfo paramTypeProp = definition.GetType().GetProperty("ParameterType");
                        if (paramTypeProp != null)
                        {
                            object val = paramTypeProp.GetValue(definition);
                            string typeName = val?.ToString();
                            if (typeName == "Text" || typeName == "MultilineText")
                            {
                                isTextParam = true;
                            }
                        }

                        // 方案 B：如果 A 方案没找到（比如在 Revit 2023+ 中属性被删），则尝试新版 GetDataType() 方法
                        if (!isTextParam)
                        {
                            System.Reflection.MethodInfo getDataTypeMethod = definition.GetType().GetMethod("GetDataType");
                            if (getDataTypeMethod != null)
                            {
                                object forgeTypeIdObj = getDataTypeMethod.Invoke(definition, null);
                                if (forgeTypeIdObj != null)
                                {
                                    System.Reflection.PropertyInfo typeIdProp = forgeTypeIdObj.GetType().GetProperty("TypeId");
                                    if (typeIdProp != null)
                                    {
                                        string typeIdString = typeIdProp.GetValue(forgeTypeIdObj) as string;
                                        if (!string.IsNullOrEmpty(typeIdString))
                                        {
                                            // 统一转成小写，防止 Revit 底层 API 大小写变化导致匹配失败
                                            typeIdString = typeIdString.ToLower();

                                            if (typeIdString.Contains("string:text") || typeIdString.Contains("multilinetext"))
                                            {
                                                isTextParam = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略个别参数由于各种奇怪原因导致的反射错误，防止程序崩溃
                    }

                    // 如果判定为文字类型，加入待删除列表
                    if (isTextParam)
                    {
                        paramsToDelete.Add(param);
                    }
                }
            }

            // 2. 判断是否包含文字属性
            if (paramsToDelete.Count == 0)
            {
                TaskDialog.Show("提示", "当前文档未找到任何自定义的文字或多行文本属性！");
                return;
            }

            // 3. 找到了文字属性，开启事务进行批量删除
            int successCount = 0;
            using (Transaction trans = new Transaction(familyDoc, "批量删除文字属性"))
            {
                trans.Start();
                foreach (FamilyParameter paramToDelete in paramsToDelete)
                {
                    try
                    {
                        // 加上 Try-Catch 防止某些被公式深度锁定的参数导致程序中断
                        familyManager.RemoveParameter(paramToDelete);
                        successCount++;
                    }
                    catch
                    {
                        // 忽略无法删除的个别异常参数，继续删除下一个
                    }
                }
                trans.Commit();
            }

            TaskDialog.Show("完成", $"共找到 {paramsToDelete.Count} 个自定义文字属性，成功删除 {successCount} 个。");
        }
        public string TVCommandName3 { get; set; } = "批量清理机电后台计算";
        public ICommand TVCommand3 => new BaseBindingCommand(TVControl3);
        public void TVControl3(object obj)
        {
            ExecuteClose(null);
            var ductsToChange = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType)).Cast<MechanicalSystemType>()
           .Where(dst =>
           {
               var p = dst.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_CALCULATION_PARAM);
               return p != null && !p.IsReadOnly && p.AsInteger() != 0;
           }).ToList();
            // 2. 查找当前【未关闭计算】的管道系统类型 (参数值不为 0)
            var pipesToChange = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>()
                .Where(pst =>
                {
                    var p = pst.get_Parameter(BuiltInParameter.RBS_PIPE_SYSTEM_CALCULATION_PARAM);
                    return p != null && !p.IsReadOnly && p.AsInteger() != 0;
                }).ToList();
            // 3. 检查是否有需要修改的系统，没有则直接退出后续逻辑
            // 3. 检查是否有需要修改的系统，没有则直接退出后续逻辑
            if (ductsToChange.Count == 0 && pipesToChange.Count == 0)
            {
                TaskDialog.Show("提示", "当前项目中所有机电系统的计算均已关闭，无需重复执行。");
                return;
            }
            NewTransaction.Execute(doc, "关闭机电系统计算", () =>
            {
                // 修改风管系统
                foreach (var dst in ductsToChange)
                {
                    dst.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_CALCULATION_PARAM).Set(0);
                }

                // 修改管道系统
                foreach (var pst in pipesToChange)
                {
                    pst.get_Parameter(BuiltInParameter.RBS_PIPE_SYSTEM_CALCULATION_PARAM).Set(0);
                }

                // 弹窗提示结果
                TaskDialog.Show("优化完成",
                $"成功关闭了计算：\n{ductsToChange.Count} 个风管系统类型\n{pipesToChange.Count} 个管道系统类型\n\n现在修改机电管线将不会触发后台卡顿。");
            });
        }
        public string TVCommandName4 { get; set; } = "批量清理分析模型";
        public ICommand TVCommand4 => new BaseBindingCommand(TVControl4);
        public void TVControl4(object obj)
        {
            ExecuteClose(null);
            ElementId id0 = new ElementId(-2001300);//基础id
            ElementId id1 = new ElementId(-2001320);//梁id
            ElementId id2 = new ElementId(-2001330);//柱id
            int elemCount = 0;

            NewTransaction.Execute(doc, "取消计算模型", () =>
            {
                elemCount = RemoveAnalyticalModel(doc, id0, elemCount) + RemoveAnalyticalModel(doc, id1, elemCount) + RemoveAnalyticalModel(doc, id2, elemCount);
            });

            TaskDialog.Show("tt", "已修改" + elemCount.ToString());
        }
        public int RemoveAnalyticalModel(Document doc, ElementId id, int count)
        {
            FilteredElementCollector colllector = new FilteredElementCollector(doc);
            ICollection<ElementId> ids = colllector.OfClass(typeof(FamilyInstance)).OfCategoryId(id).ToElementIds();
            foreach (ElementId eId in ids)
            {
                Element element = doc.GetElement(eId) as Element;
                bool parameter = element.get_Parameter(BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL).Set(0);
                count++;
            }
            return count;
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
