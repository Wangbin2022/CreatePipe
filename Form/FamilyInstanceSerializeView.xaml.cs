using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        #region 私有字段
        private readonly UIApplication _uiapp;
        private readonly UIDocument _uidoc;
        private bool _isSelecting = false;
        private readonly List<ElementId> _orderedSelection = new List<ElementId>();
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();

        // 将命令声明为私有只读字段
        private readonly BaseBindingCommand _startSelectionCommand;
        private readonly BaseBindingCommand _executeWriteCommand;
        #endregion

        #region 公开属性
        public string BeforeCode { get => _beforeCode; set => SetProperty(ref _beforeCode, value); }
        private string _beforeCode = "";

        public string SerialCode { get => _serialCode; set => SetProperty(ref _serialCode, value); }
        private string _serialCode = "1";

        public string BackCode { get => _backCode; set => SetProperty(ref _backCode, value); }
        private string _backCode = "";

        public string InstanceAttr { get => _instanceAttr; set => SetProperty(ref _instanceAttr, value); }
        private string _instanceAttr = "编号";

        public int SelectedCount { get => _selectedCount; private set => SetProperty(ref _selectedCount, value); }
        private int _selectedCount = 0;

        public string SelectionButtonText { get => _selectionButtonText; private set => SetProperty(ref _selectionButtonText, value); }
        private string _selectionButtonText = "开始选择";

        // 将属性直接指向已创建的命令实例
        public ICommand StartSelectionCommand => _startSelectionCommand;
        public ICommand ExecuteWriteCommand => _executeWriteCommand;
        #endregion

        #region 构造与析构
        public FamilyInstanceSerializeViewModel(UIApplication uIApp)
        {
            _uiapp = uIApp;
            _uidoc = uIApp.ActiveUIDocument;

            // 在构造函数中只创建一次命令实例
            _startSelectionCommand = new BaseBindingCommand(ToggleSelectionMode);
            _executeWriteCommand = new BaseBindingCommand(ExecuteWrite, obj => SelectedCount > 0);
        }

        public void Dispose()
        {
            StopSelectionMode();
            _externalHandler?.Dispose();
        }
        #endregion

        #region 命令方法
        private void ToggleSelectionMode(object obj)
        {
            _isSelecting = !_isSelecting;
            if (_isSelecting)
            {
                StartSelectionMode();
            }
            else
            {
                StopSelectionMode();
            }
        }

        private void ExecuteWrite(object obj)
        {
            if (_isSelecting)
            {
                StopSelectionMode();
            }

            if (!int.TryParse(SerialCode, out int startNumber))
            {
                TaskDialog.Show("错误", "起始序号必须是一个有效的整数。");
                return;
            }

            var elementsToProcess = new List<ElementId>(_orderedSelection);
            string prefix = this.BeforeCode;
            string suffix = this.BackCode;
            string parameterName = this.InstanceAttr;

            _externalHandler.Run(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                int currentNumber = startNumber;

                using (Transaction trans = new Transaction(doc, "批量构件编号"))
                {
                    trans.Start();
                    foreach (var id in elementsToProcess)
                    {
                        Element elem = doc.GetElement(id);
                        if (elem == null) continue;

                        Parameter param = elem.LookupParameter(parameterName);
                        if (param != null && !param.IsReadOnly)
                        {
                            param.Set($"{prefix}{currentNumber++}{suffix}");
                        }
                    }
                    trans.Commit();
                }
                TaskDialog.Show("成功", $"已成功为 {elementsToProcess.Count} 个构件编号。");
            });

            _orderedSelection.Clear();
            SelectedCount = 0;
        }
        #endregion

        #region 私有辅助方法
        private void StartSelectionMode()
        {
            SelectionButtonText = "停止选择";
            _orderedSelection.Clear();
            UpdateSelection();
            _uiapp.Idling += OnIdling;
        }

        private void StopSelectionMode()
        {
            SelectionButtonText = "开始选择";
            if (_isSelecting) // 避免重复取消订阅
            {
                _isSelecting = false;
                _uiapp.Idling -= OnIdling;
            }
        }

        private void OnIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e) => UpdateSelection();

        private void UpdateSelection()
        {
            ICollection<ElementId> currentSelectionIds = _uidoc.Selection.GetElementIds();
            _orderedSelection.RemoveAll(id => !currentSelectionIds.Contains(id));
            foreach (var id in currentSelectionIds)
            {
                if (!_orderedSelection.Contains(id))
                {
                    _orderedSelection.Add(id);
                }
            }

            SelectedCount = _orderedSelection.Count;
            // 现在这行代码可以正常工作了，因为它操作的是唯一的命令实例
            _executeWriteCommand.RaiseCanExecuteChanged();
        }
        #endregion
    }
}
