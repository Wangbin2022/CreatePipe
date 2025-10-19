using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// SequentialSelector.xaml 的交互逻辑
    /// </summary>
    public partial class SequentialSelector : UserControl, IDisposable
    {
        // 保持不变: 接收 Revit Application
        public static readonly DependencyProperty RevitApplicationProperty =
            DependencyProperty.Register("RevitApplication", typeof(UIApplication), typeof(SequentialSelector), new PropertyMetadata(null, OnRevitApplicationChanged));

        // 新增: 接收一个完成后的回调命令
        public static readonly DependencyProperty SelectionFinishedCommandProperty =
            DependencyProperty.Register("SelectionFinishedCommand", typeof(ICommand), typeof(SequentialSelector), new PropertyMetadata(null, OnSelectionFinishedCommandChanged));

        public UIApplication RevitApplication
        {
            get { return (UIApplication)GetValue(RevitApplicationProperty); }
            set { SetValue(RevitApplicationProperty, value); }
        }

        public ICommand SelectionFinishedCommand
        {
            get { return (ICommand)GetValue(SelectionFinishedCommandProperty); }
            set { SetValue(SelectionFinishedCommandProperty, value); }
        }

        public SequentialSelector()
        {
            InitializeComponent();
        }

        private static void OnRevitApplicationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SequentialSelector;
            if (control != null && e.NewValue is UIApplication uiapp)
            {
                // 初始化 ViewModel 并设置 DataContext
                control.DataContext = new SequentialSelectorViewModel(uiapp);
                // 将命令传递给 ViewModel
                PassCommandToViewModel(control);
            }
        }

        private static void OnSelectionFinishedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 当父窗口的命令绑定过来时，将其传递给 ViewModel
            PassCommandToViewModel(d as SequentialSelector);
        }

        private static void PassCommandToViewModel(SequentialSelector control)
        {
            if (control?.DataContext is SequentialSelectorViewModel viewModel)
            {
                viewModel.SelectionFinishedCommand = control.SelectionFinishedCommand;
            }
        }

        public void Dispose()
        {
            if (this.DataContext is IDisposable viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
    public class SequentialSelectorViewModel : ObserverableObject, IDisposable
    {
        #region 私有字段
        private readonly UIApplication _uiapp;
        private readonly UIDocument _uidoc;
        private bool _isSelecting = false;
        private readonly List<ElementId> _orderedSelection = new List<ElementId>();
        #endregion

        #region 公开属性
        public string InstructionText { get; private set; } = "点击“开始选择”按钮，然后在Revit中按顺序点选构件。";
        public int SelectedCount { get; private set; }
        public string StartButtonText { get; private set; } = "开始选择";
        public bool IsFinishButtonEnabled { get; private set; } = false;

        public ICommand SelectionFinishedCommand { get; set; }
        public ICommand ToggleSelectionCommand { get; }
        public ICommand FinishSelectionCommand { get; }
        #endregion

        #region 事件
        // 内部事件，用于通知 UserControl Code-behind
        internal event Action<Dictionary<int, ElementId>> SelectionFinishedInternal;
        #endregion

        public SequentialSelectorViewModel(UIApplication uiapp)
        {
            _uiapp = uiapp;
            _uidoc = uiapp.ActiveUIDocument;

            ToggleSelectionCommand = new BaseBindingCommand(ToggleSelectionMode);
            FinishSelectionCommand = new BaseBindingCommand(FinishSelection, obj => IsFinishButtonEnabled);
        }

        private void ToggleSelectionMode(object obj)
        {
            _isSelecting = !_isSelecting;
            if (_isSelecting) StartSelectionMode();
            else StopSelectionMode();
        }
        private void FinishSelection(object obj)
        {
            StopSelectionMode();

            var result = new Dictionary<int, ElementId>();
            for (int i = 0; i < _orderedSelection.Count; i++)
            {
                result.Add(i, _orderedSelection[i]);
            }

            // 核心修改：执行外部传入的命令，而不是触发内部事件
            if (SelectionFinishedCommand?.CanExecute(result) ?? false)
            {
                SelectionFinishedCommand.Execute(result);
            }

            _orderedSelection.Clear();
            UpdateUIState();
        }
        private void StartSelectionMode()
        {
            _orderedSelection.Clear();
            _uiapp.Idling += OnIdling;
            UpdateUIState();
        }

        private void StopSelectionMode()
        {
            if (_isSelecting)
            {
                _uiapp.Idling -= OnIdling;
                _isSelecting = false;
                UpdateUIState();
            }
        }

        private void OnIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            var currentSelectionIds = _uidoc.Selection.GetElementIds();
            _orderedSelection.RemoveAll(id => !currentSelectionIds.Contains(id));
            foreach (var id in currentSelectionIds)
            {
                if (!_orderedSelection.Contains(id))
                {
                    _orderedSelection.Add(id);
                }
            }
            UpdateUIState();
        }

        // 统一更新UI状态的方法
        private void UpdateUIState()
        {
            SelectedCount = _orderedSelection.Count;
            IsFinishButtonEnabled = SelectedCount > 0;

            if (_isSelecting)
            {
                StartButtonText = "暂停选择";
                InstructionText = "正在选择构件... 按 ESC 可取消选择单个构件。";
            }
            else
            {
                StartButtonText = "开始选择";
                InstructionText = "点击“开始选择”按钮，然后在Revit中按顺序点选构件。";
            }

            // 手动通知UI属性已更改
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(IsFinishButtonEnabled));
            OnPropertyChanged(nameof(StartButtonText));
            OnPropertyChanged(nameof(InstructionText));

            // 刷新命令状态
            ((BaseBindingCommand)FinishSelectionCommand).RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            // 确保在关闭时取消订阅事件，防止内存泄漏
            if (_isSelecting)
            {
                _uiapp.Idling -= OnIdling;
            }
        }
    }

}
