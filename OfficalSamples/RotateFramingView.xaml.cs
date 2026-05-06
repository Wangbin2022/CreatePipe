using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// RotateFramingView.xaml 的交互逻辑
    /// </summary>
    public partial class RotateFramingView : Window
    {
        public RotateFramingView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 旋转模式枚举
    /// </summary>
    public enum RotationMode
    {
        Relative,   // 相对旋转（增量）
        Absolute    // 绝对旋转（设置为指定角度）
    }

    /// <summary>
    /// 构件旋转ViewModel - 管理旋转参数和执行逻辑
    /// </summary>
    public class RotateFramingViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private double _rotationAngle;
        private RotationMode _selectedMode = RotationMode.Relative;
        private bool _isProcessing;

        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                _rotationAngle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanExecute));
            }
        }

        public RotationMode SelectedMode
        {
            get => _selectedMode;
            set { _selectedMode = value; OnPropertyChanged(); }
        }

        public bool IsRelativeMode
        {
            get => _selectedMode == RotationMode.Relative;
            set { if (value) SelectedMode = RotationMode.Relative; }
        }

        public bool IsAbsoluteMode
        {
            get => _selectedMode == RotationMode.Absolute;
            set { if (value) SelectedMode = RotationMode.Absolute; }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing && Math.Abs(_rotationAngle) > 1e-9;

        public ICommand RotateCommand { get; }
        public ICommand CancelCommand { get; }

        public RotateFramingViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;

            RotateCommand = new BaseBindingCommand(_ => ExecuteRotate(), _ => CanExecute);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        /// <summary>
        /// 执行旋转操作
        /// </summary>
        private void ExecuteRotate()
        {
            IsProcessing = true;
            try
            {
                RotateSelectedElements();
                TaskDialog.Show("成功", $"已旋转 {GetSelectedElements().Count} 个构件。");
                CloseWindow?.Invoke();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"旋转失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 旋转选中的元素
        /// </summary>
        private void RotateSelectedElements()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var selectedIds = _uiApp.ActiveUIDocument.Selection.GetElementIds();

            if (selectedIds.Count == 0) return;

            using (var transaction = new Transaction(doc, "旋转构件"))
            {
                transaction.Start();

                foreach (var id in selectedIds)
                {
                    var element = doc.GetElement(id);
                    if (element is FamilyInstance familyInstance)
                    {
                        RotateFamilyInstance(familyInstance);
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// 旋转单个族实例 - 使用模式匹配
        /// </summary>
        private void RotateFamilyInstance(FamilyInstance instance)
        {
            switch (instance.StructuralType)
            {
                case StructuralType.Beam:
                case StructuralType.Brace:
                    RotateBeamOrBrace(instance);
                    break;
                case StructuralType.Column:
                    RotateColumn(instance);
                    break;
                default:
                    throw new Exception($"不支持的构件类型：{instance.StructuralType}");
            }
        }

        /// <summary>
        /// 旋转梁或支撑 - 通过修改Cross-Section Rotation参数
        /// </summary>
        private void RotateBeamOrBrace(FamilyInstance instance)
        {
            const string paramName = "Cross-Section Rotation";
            var param = instance.LookupParameter(paramName);

            if (param == null || param.StorageType != StorageType.Double)
                throw new Exception($"找不到参数：{paramName}");

            double currentRadians = param.AsDouble();
            double targetDegrees = _rotationAngle;
            double targetRadians = targetDegrees * Math.PI / 180;

            double newValue = _selectedMode == RotationMode.Absolute
                ? targetRadians
                : currentRadians + targetRadians;

            param.Set(newValue);
        }

        /// <summary>
        /// 旋转柱 - 使用LocationPoint.Rotate方法
        /// </summary>
        private void RotateColumn(FamilyInstance instance)
        {
            var location = instance.Location as LocationPoint;
            if (location == null)
                throw new Exception("无法获取柱的位置信息");

            double currentRotation = location.Rotation;
            double targetRadians = _rotationAngle * Math.PI / 180;

            double delta = _selectedMode == RotationMode.Absolute
                ? targetRadians - currentRotation
                : targetRadians;

            if (Math.Abs(delta) < 1e-9) return;

            var axis = Autodesk.Revit.DB.Line.CreateUnbound(location.Point, XYZ.BasisZ);
            var success = location.Rotate(axis, delta);

            if (!success)
                throw new Exception("柱旋转失败");
        }

        /// <summary>
        /// 获取选中的元素列表
        /// </summary>
        private IList<Element> GetSelectedElements()
        {
            var selectedIds = _uiApp.ActiveUIDocument.Selection.GetElementIds();
            return selectedIds.Select(id => _uiApp.ActiveUIDocument.Document.GetElement(id)).ToList();
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
