using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// SelectionDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionDialogView : Window
    {
        public SelectionDialogView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 选择类型枚举
    /// </summary>
    public enum SelectionType
    {
        Element,   // 选择元素
        Face,      // 选择面
        Edge,      // 选择边
        Point      // 选择点
    }

    /// <summary>
    /// 选择管理器ViewModel - 处理元素/点的选择
    /// </summary>
    public class SelectionManagerViewModel : INotifyPropertyChanged
    {
        private readonly ExternalCommandData _commandData;
        private readonly UIDocument _uiDoc;
        private SelectionType _selectionType = SelectionType.Element;
        private Element _selectedElement;
        private XYZ _selectedPoint;
        private XYZ _pickedPoint;

        public SelectionType SelectionType
        {
            get => _selectionType;
            set { _selectionType = value; OnPropertyChanged(); }
        }

        public Element SelectedElement
        {
            get => _selectedElement;
            set { _selectedElement = value; OnPropertyChanged(); }
        }

        public XYZ SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                _selectedPoint = value;
                OnPropertyChanged();
                if (SelectedElement != null && value != null)
                {
                    MoveElementToPoint(value);
                }
            }
        }

        public string ElementInfo => SelectedElement != null
            ? $"{SelectedElement.Name} (ID: {SelectedElement.Id.IntegerValue})"
            : "未选择";

        public string PointInfo => SelectedPoint != null
            ? $"({SelectedPoint.X:F2}, {SelectedPoint.Y:F2}, {SelectedPoint.Z:F2})"
            : "未选择";

        public SelectionManagerViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _uiDoc = commandData.Application.ActiveUIDocument;
        }

        /// <summary>
        /// 执行选择 - 根据选择类型调用不同方法
        /// </summary>
        public void SelectObjects()
        {
            switch (_selectionType)
            {
                case SelectionType.Element:
                    PickElement();
                    break;
                case SelectionType.Point:
                    PickPoint();
                    break;
                default:
                    break;
            }
        }

        private void PickElement()
        {
            try
            {
                var refElem = _uiDoc.Selection.PickObject(
                    Autodesk.Revit.UI.Selection.ObjectType.Element,
                    "请选择要移动的元素。");

                if (refElem?.ElementId != ElementId.InvalidElementId)
                {
                    SelectedElement = _uiDoc.Document.GetElement(refElem);
                    _pickedPoint = refElem.GlobalPoint;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                SelectedElement = null;
            }
        }

        private void PickPoint()
        {
            try
            {
                SelectedPoint = _uiDoc.Selection.PickPoint("请选择目标点位置。");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                SelectedPoint = null;
            }
        }

        private void MoveElementToPoint(XYZ targetPoint)
        {
            var delta = targetPoint - _pickedPoint;
            _pickedPoint = targetPoint;
            ElementTransformUtils.MoveElement(_uiDoc.Document, SelectedElement.Id, delta);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 主窗口ViewModel - 管理选择交互
    /// </summary>
    public class SelectionDialogViewModel : ObserverableObject
    {
        private readonly SelectionManagerViewModel _manager;
        private bool _isProcessing;

        public SelectionManagerViewModel Manager => _manager;

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand PickElementCommand { get; }
        public ICommand MoveToCommand { get; }
        public ICommand CloseCommand { get; }

        public SelectionDialogViewModel(ExternalCommandData commandData)
        {
            _manager = new SelectionManagerViewModel(commandData);

            PickElementCommand = new BaseBindingCommand(_ => PickElement(), _ => CanExecute);
            MoveToCommand = new BaseBindingCommand(_ => MoveTo(), _ => CanExecute);
            CloseCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void PickElement()
        {
            IsProcessing = true;
            _manager.SelectObjects();
            IsProcessing = false;
        }

        private void MoveTo()
        {
            IsProcessing = true;
            _manager.SelectObjects();
            IsProcessing = false;
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
