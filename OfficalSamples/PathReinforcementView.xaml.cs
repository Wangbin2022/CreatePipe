using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// PathReinforcement.xaml 的交互逻辑
    /// </summary>
    public partial class PathReinforcementView : Window
    {
        private readonly PathReinPropertiesViewModel _viewModel;
        private readonly PreviewViewModel _preview;
        public PathReinforcementView(PathReinforcement pathRein, ExternalCommandData commandData, Hashtable barTypes)
        {
            InitializeComponent();
            _viewModel = new PathReinPropertiesViewModel(pathRein, barTypes);
            _viewModel.CloseWindow = Close;
            //_viewModel.OnUpdateSelected = () => { }; // 刷新PropertyGrid等效

            _preview = new PreviewViewModel(pathRein, commandData);

            DataContext = _viewModel;

            // 预览更新
            _viewModel.PropertyChanged += (s, e) => UpdatePreview();
            UpdatePreview();
        }
        private void UpdatePreview()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                _preview.Draw(dc, new System.Windows.Size((int)PreviewImage.ActualWidth, (int)PreviewImage.ActualHeight));
            }
            PreviewImage.Source = new DrawingImage(visual.Drawing);
        }
    }
    /// <summary>
    /// 布局规则枚举
    /// </summary>
    public enum LayoutRule2
    {
        FixedNumber = 2,
        MaximumSpacing = 3
    }

    /// <summary>
    /// 面位置枚举
    /// </summary>
    public enum Face
    {
        Top = 0,
        Bottom = 1
    }

    /// <summary>
    /// 钢筋类型项 - 用于下拉选择
    /// </summary>
    public class BarTypeItem
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
    }

    /// <summary>
    /// 路径钢筋属性ViewModel - 实现INotifyPropertyChanged
    /// </summary>
    public class PathReinPropertiesViewModel : ObserverableObject
    {
        private readonly PathReinforcement _pathRein;
        private LayoutRule2 _layoutRule;
        private Face _face;
        private int _numberOfBars;
        private string _barSpacing;
        private ElementId _primaryBarType;
        private string _primaryBarLength;
        private ObservableCollection<BarTypeItem> _barTypes;

        public ObservableCollection<BarTypeItem> BarTypes
        {
            get => _barTypes;
            set { _barTypes = value; OnPropertyChanged(); }
        }

        public LayoutRule2 LayoutRule
        {
            get => _layoutRule;
            set
            {
                if (_layoutRule == value) return;
                _layoutRule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBarSpacingReadOnly));
                OnPropertyChanged(nameof(IsNumberOfBarsReadOnly));
                RaiseUpdateSelectedEvent();
            }
        }

        public Face Face
        {
            get => _face;
            set { _face = value; OnPropertyChanged(); }
        }

        public int NumberOfBars
        {
            get => _numberOfBars;
            set { _numberOfBars = value; OnPropertyChanged(); }
        }

        public string BarSpacing
        {
            get => _barSpacing;
            set
            {
                if (ValidateInch(value))
                    _barSpacing = value;
                OnPropertyChanged();
            }
        }

        public ElementId PrimaryBarType
        {
            get => _primaryBarType;
            set { _primaryBarType = value; OnPropertyChanged(); }
        }

        public string PrimaryBarLength
        {
            get => _primaryBarLength;
            set
            {
                if (ValidateInch(value))
                    _primaryBarLength = value;
                OnPropertyChanged();
            }
        }

        public bool IsBarSpacingReadOnly => _layoutRule == LayoutRule2.FixedNumber;
        public bool IsNumberOfBarsReadOnly => _layoutRule == LayoutRule2.MaximumSpacing;

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public PathReinPropertiesViewModel(PathReinforcement pathRein, Hashtable barTypes)
        {
            _pathRein = pathRein;
            LoadBarTypes(barTypes);
            LoadParameters();

            OkCommand = new BaseBindingCommand(_ => UpdateAndClose());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void LoadBarTypes(Hashtable barTypes)
        {
            var BarTypes = new ObservableCollection<BarItem>();
            foreach (DictionaryEntry entry in barTypes)
            {
                BarTypes.Add(new BarItem
                {
                    Name = entry.Key.ToString(),
                    Id = (ElementId)entry.Value
                });
            }
        }

        private void LoadParameters()
        {
            _layoutRule = (LayoutRule2)GetParameter("Layout Rule").AsInteger();
            _face = (Face)GetParameter("Face").AsInteger();
            _numberOfBars = _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_NUMBER_OF_BARS).AsInteger();
            _barSpacing = _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_SPACING).AsValueString() ?? "";
            _primaryBarLength = _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_LENGTH_1).AsValueString() ?? "";
            _primaryBarType = GetParameter("Primary Bar - Type").AsElementId();
        }

        private Parameter GetParameter(string name) =>
            _pathRein.Parameters.Cast<Parameter>().FirstOrDefault(p => p.Definition.Name == name);

        private void UpdateAndClose()
        {
            UpdateParameters();
            CloseWindow?.Invoke();
        }

        private void UpdateParameters()
        {
            GetParameter("Layout Rule")?.Set((int)_layoutRule);
            GetParameter("Face")?.Set((int)_face);
            GetParameter("Primary Bar - Type")?.Set(_primaryBarType);
            _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_LENGTH_1).SetValueString(_primaryBarLength);

            if (_layoutRule == LayoutRule2.MaximumSpacing)
            {
                _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_SPACING).SetValueString(_barSpacing);
                GetParameter("Layout Rule")?.Set((int)LayoutRule2.FixedNumber);
                _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_NUMBER_OF_BARS).Set(_numberOfBars);
                GetParameter("Layout Rule")?.Set((int)_layoutRule);
            }
            else if (_layoutRule == LayoutRule2.FixedNumber)
            {
                _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_NUMBER_OF_BARS).Set(_numberOfBars);
                GetParameter("Layout Rule")?.Set((int)LayoutRule2.MaximumSpacing);
                _pathRein.get_Parameter(BuiltInParameter.PATH_REIN_SPACING).SetValueString(_barSpacing);
                GetParameter("Layout Rule")?.Set((int)_layoutRule);
            }
        }

        private bool ValidateInch(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            if (double.TryParse(input, out _)) return true;

            int number = 0, sQuotation = 0, dQuotation = 0, hLine = 0;
            foreach (char ch in input.Trim())
            {
                if (dQuotation > 0) return false;
                if (ch >= '0' && ch <= '9') number++;
                else if (ch == '\'') { if (sQuotation > 0 || number == 0) return false; sQuotation++; number = 0; }
                else if (ch == '\"') { if (dQuotation > 0 || number == 0) return false; dQuotation++; number = 0; }
                else if (ch == '-') { if (hLine != 0 || sQuotation == 0 || number != 0) return false; hLine++; number = 0; }
                else if (ch != ' ') return false;
            }

            //var last = input.Trim()[^1];
            var last = input.Trim()[1];
            if (dQuotation > 0 && last != '\"') return false;
            if (sQuotation > 0 && dQuotation == 0 && last != '\'') return false;
            return true;
        }

        private void RaiseUpdateSelectedEvent() => OnUpdateSelected?.Invoke();

        public event Action OnUpdateSelected;
        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BarItem
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
    }


    /// <summary>
    /// 几何预览视图模型 - 处理3D到2D转换和绘制
    /// </summary>
    public class PreviewViewModel
    {
        private readonly PathReinforcement _pathRein;
        private readonly ExternalCommandData _commandData;
        private List<List<XYZ>> _curves = new List<List<XYZ>>();
        private List<List<UV>> _points2d = new List<List<UV>>();
        private UV _min, _max;

        public PreviewViewModel(PathReinforcement pathRein, ExternalCommandData commandData)
        {
            _pathRein = pathRein;
            _commandData = commandData;
            Tessellate();
            ComputeBound();
        }

        /// <summary>
        /// 绘制预览到DrawingContext
        /// </summary>
        public void Draw(DrawingContext dc, Size size)
        {
            if (_points2d.Count == 0) return;

            var delta = _max - _min;
            float scaleX = (float)(size.Width / delta.U);
            float scaleY = (float)(size.Width / delta.V);
            float scale = Math.Min(scaleX, scaleY) * 0.9f;

            var center = (_min + _max) / 2;
            var pen = new System.Windows.Media.Pen(Brushes.Black, 1);
            var bluePen = new System.Windows.Media.Pen(Brushes.Blue, 1.5);

            foreach (var arr in _points2d)
            {
                for (int i = 0; i < arr.Count - 1; i++)
                {
                    var p1 = TransformPoint(arr[i], center, scale, size);
                    var p2 = TransformPoint(arr[i + 1], center, scale, size);
                    dc.DrawLine(pen, p1, p2);
                }
            }
        }

        private System.Windows.Point TransformPoint(UV uv, UV center, float scale, Size size)
        {
            return new System.Windows.Point(
                (uv.U - center.U) * scale + size.Width / 2,
                -(uv.V - center.V) * scale + size.Height / 2);
        }

        private void Tessellate()
        {
            var options = new Options { DetailLevel = ViewDetailLevel.Fine };
            var geometry = _pathRein.get_Geometry(options);

            foreach (var geo in geometry)
            {
                if (geo is Curve curve)
                    _curves.Add(curve.Tessellate().ToList());
            }
        }

        private void ComputeBound()
        {
            //var transform = GetActiveViewMatrix().Inverse();
            //bool first = true;

            //foreach (var arr in _curves)
            //{
            //    var uvList = new List<UV>();
            //    foreach (var xyz in arr)
            //    {
            //        var v = transform.Transform(new Vector4(xyz));
            //        var uv = new UV(v.X, v.Y);
            //        uvList.Add(uv);

            //        if (first)
            //        {
            //            _min = new UV(uv.U, uv.V);
            //            _max = new UV(uv.U, uv.V);
            //            first = false;
            //        }
            //        _min = new UV(Math.Min(_min.U, uv.U), Math.Min(_min.V, uv.V));
            //        _max = new UV(Math.Max(_max.U, uv.U), Math.Max(_max.V, uv.V));
            //    }
            //    _points2d.Add(uvList);
            //}
        }

        private Matrix4 GetActiveViewMatrix()
        {
            var view = _commandData.Application.ActiveUIDocument.Document.ActiveView;
            return new Matrix4(
                new Vector4(view.RightDirection),
                new Vector4(view.UpDirection),
                new Vector4(view.ViewDirection));
        }
    }
}
