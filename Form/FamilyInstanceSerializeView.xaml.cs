using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// FamilyInstanceSerializeView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyInstanceSerializeView : Window
    {
        public FamilyInstanceSerializeView(UIApplication uIApp)
        {
            InitializeComponent();
            this.DataContext = new FamilyInstanceSerializeViewModel(uIApp);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class FamilyInstanceSerializeViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public FamilyInstanceSerializeViewModel(UIApplication uIApp)
        {
            Doc = uIApp.ActiveUIDocument.Document;
        }
        public ICommand StartSelectionCommand => new BaseBindingCommand(ToggleSelectionMode);
        private void ToggleSelectionMode(object obj)
        {
            _externalHandler.Run(app =>
            {
                IsSelectionMode = true;
                var reference0 = app.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), $"选择第一个待编号构件");
                var firstElement = app.ActiveUIDocument.Document.GetElement(reference0.ElementId) as FamilyInstance;
                Parameter param;
                if (IsBuiltInParameter(InstanceAttr))
                {
                    TaskDialog.Show("tt", $" 参数'{InstanceAttr}'可能是系统内置参数，请重新选择");
                    return;
                    //var dialogResult = TaskDialog.Show("内置参数警告",
                    //    $"您指定的参数 '{InstanceAttr}' 可能是内置参数。\n\n" +
                    //    "内置参数可能受到系统保护，修改时可能会遇到以下问题：\n" +
                    //    "• 参数为只读\n" +
                    //    "• 修改被系统拒绝\n" +
                    //    "• 产生意外行为\n\n" +
                    //    "是否继续使用此参数？",
                    //    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
                    //if (dialogResult == TaskDialogResult.No)
                    //{
                    //    return;
                    //}
                }
                if (firstElement.LookupParameter(InstanceAttr) == null)
                {
                    TaskDialog.Show("tt", $" 元素不存在属性 '{InstanceAttr}'，请重新选择");
                    return;
                }
                else param = firstElement.LookupParameter(InstanceAttr);
                if (param.IsReadOnly || param.StorageType != StorageType.String)
                {
                    TaskDialog.Show("tt", $" 属性 '{InstanceAttr}'非文本或设置为不可写入，请修改后选择");
                    return;
                }
                Family family = firstElement.Symbol.Family;
                var familyFilter = new FamilyFilterClass(family);
                _collectedElementIds.Add(_currentIndex, reference0.ElementId);
                _currentIndex++;
                // 更新UI状态
                SelectedCount++;

                while (IsSelectionMode)
                {
                    try
                    {
                        var reference = app.ActiveUIDocument.Selection.PickObject(ObjectType.Element,
                            familyFilter, $"选择构件 (已选择 {SelectedCount} 个, ESC退出)");
                        if (reference != null)
                        {
                            //// 检查是否已存在会导致循环出错，建议允许重复，以最终排序号为准
                            //if (_collectedElementIds.Values.Contains(reference.ElementId))
                            //{
                            //    break;
                            //}
                            // 获取元素信息
                            var element = app.ActiveUIDocument.Document.GetElement(reference.ElementId);
                            if (element == null) return;
                            // 添加到字典
                            _collectedElementIds.Add(_currentIndex, reference.ElementId);
                            _currentIndex++;
                            // 更新UI状态
                            SelectedCount++;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // 用户按ESC取消选择，退出循环
                        CanExecute = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 其他异常，显示错误但继续选择
                        TaskDialog.Show("选择错误", $"选择过程中出错: {ex.Message}");
                    }
                }
                IsSelectionMode = false;
            });
        }
        private bool IsBuiltInParameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return false;
            try
            {
                var builtInParameters = Enum.GetValues(typeof(BuiltInParameter)).Cast<BuiltInParameter>()
                    .Where(bip => bip != BuiltInParameter.INVALID).ToList();
                return builtInParameters.Any(bip =>
                {
                    try
                    {
                        string builtInName = LabelUtils.GetLabelFor(bip);
                        return string.Equals(builtInName, parameterName, StringComparison.CurrentCultureIgnoreCase) ||
                               string.Equals(bip.ToString(), parameterName, StringComparison.CurrentCultureIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch
            {
                return false;
            }
        }
        public ICommand ExecuteWriteCommand => new BaseBindingCommand(ExecuteWrite);
        private void ExecuteWrite(object obj)
        {
            //// 直接点选赋码，检查状态退出，功能无效待处理，但收集值是正确的
            _externalHandler.Run(app =>
            {
                using (Transaction trans = new Transaction(Doc, "批量构件编号"))
                {
                    trans.Start();
                    int successCount = 0;
                    int failCount = 0;
                    StringBuilder failMessage = new StringBuilder();
                    // 按序号顺序处理
                    for (int i = 1; i <= _collectedElementIds.Count; i++)
                    {
                        if (!_collectedElementIds.TryGetValue(i, out ElementId elementId))
                            continue;
                        var element = Doc.GetElement(elementId);
                        try
                        {
                            // 生成编码：BeforeCode + SerialCode + BackCode
                            // SerialCode 根据循环序号递增
                            string serialNumber = (SerialCode + i - 1).ToString(); // i从1开始，所以减1
                            string fullCode = BeforeCode + serialNumber + BackCode;

                            // 设置参数值
                            Parameter param = element.LookupParameter(InstanceAttr);
                            if (param.StorageType == StorageType.String)
                            {
                                param.Set(fullCode);
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            failMessage.AppendLine($"序号 {i}: 设置属性时出错 - {ex.Message}");
                        }
                    }
                    trans.Commit();
                    // 显示结果
                    string resultMessage = $"成功为 {successCount} 个构件编号。";
                    if (failCount > 0)
                    {
                        resultMessage += $"\n失败 {failCount} 个构件:\n{failMessage}";
                    }
                    TaskDialog.Show("批量编号结果", resultMessage);
                }
            });
        }
        public string InstanceAttr { get; set; } = "本层编号";
        private string codePreview;
        public string CodePreview
        {
            get => BeforeCode + SerialCode + BackCode;
            set => SetProperty(ref codePreview, value);
        }
        public string BackCode
        {
            get => _backCode;
            set
            {
                _backCode = value;
                OnPropertyChanged(nameof(CodePreview));
            }
        }
        private string _backCode;
        public string BeforeCode
        {
            get => _beforeCode;
            set
            {
                _beforeCode = value;
                OnPropertyChanged(nameof(CodePreview));
            }
        }
        private string _beforeCode;
        public int SerialCode
        {
            get => _serialCode;
            set
            {
                _serialCode = value;
                OnPropertyChanged(nameof(CodePreview));
            }
        }
        private int _serialCode = 1;
        // 选择模式状态
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set => SetProperty(ref _isSelectionMode, value);
        }
        private int _currentIndex = 1;
        private bool _isSelectionMode = false;
        // 存储收集的ElementId，key为序号，value为ElementId
        private Dictionary<int, ElementId> _collectedElementIds = new Dictionary<int, ElementId>();
        private int selectedCount = 0;
        public int SelectedCount
        {
            get => selectedCount;
            set => SetProperty(ref selectedCount, value);
        }
        private bool canExecute = false;
        public bool CanExecute { get => canExecute; set => SetProperty(ref canExecute, value); }
    }
}
