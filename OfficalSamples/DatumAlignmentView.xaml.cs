using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DatumAlignmentView.xaml 的交互逻辑
    /// </summary>
    public partial class DatumAlignmentView : Window
    {
        public DatumAlignmentView(ExternalCommandData commandData)
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 主视图模型 - 管理基准线对齐逻辑
    /// </summary>
    public class DatumAlignmentViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private readonly UIApplication _app;
        private readonly UIDocument _uiDoc;
        private readonly View _activeView;

        private ObservableCollection<DatumInfoModel> _selectedDatums;
        private DatumInfoModel _selectedReferenceDatum;
        private bool _isProcessing;
        private string _statusMessage;
        private AlignmentResult _lastResult;
        #endregion

        #region 公开属性
        /// <summary>选中的基准线列表</summary>
        public ObservableCollection<DatumInfoModel> SelectedDatums
        {
            get => _selectedDatums;
            set => SetProperty(ref _selectedDatums, value);
        }

        /// <summary>选中的参考基准线</summary>
        public DatumInfoModel SelectedReferenceDatum
        {
            get => _selectedReferenceDatum;
            set
            {
                SetProperty(ref _selectedReferenceDatum, value);
                OnPropertyChanged(nameof(CanAlign));
            }
        }

        /// <summary>是否正在处理</summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>上次对齐结果</summary>
        public AlignmentResult LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }

        /// <summary>是否可以执行对齐</summary>
        public bool CanAlign => !IsProcessing &&
                                SelectedDatums != null &&
                                SelectedDatums.Count > 1 &&
                                SelectedReferenceDatum != null;

        /// <summary>选中的基准线数量（显示用）</summary>
        public int SelectedCount => SelectedDatums?.Count ?? 0;

        /// <summary>将要对齐的基准线数量（排除参考线）</summary>
        public int ToBeAlignedCount => Math.Max(0, (SelectedDatums?.Count ?? 0) - 1);
        #endregion

        #region 命令
        public ICommand AlignCommand { get; }
        public ICommand RefreshSelectionCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        public DatumAlignmentViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _app = commandData.Application;
            _uiDoc = _app.ActiveUIDocument;
            _document = _uiDoc.Document;
            _activeView = _uiDoc.ActiveView;

            SelectedDatums = new ObservableCollection<DatumInfoModel>();

            // 初始化命令
            AlignCommand = new BaseBindingCommand(ExecuteAlignment, _ => CanAlign);
            RefreshSelectionCommand = new BaseBindingCommand(RefreshSelection);
            CancelCommand = new BaseBindingCommand(CloseWindow);

            // 加载选中的基准线
            LoadSelectedDatums(null);
        }

        /// <summary>
        /// 加载当前选中的基准线
        /// </summary>
        private void LoadSelectedDatums(Object obj)
        {
            try
            {
                IsProcessing = true;
                SelectedDatums.Clear();

                var selectionIds = _uiDoc.Selection.GetElementIds();
                if (selectionIds == null || selectionIds.Count == 0)
                {
                    StatusMessage = "未选中任何基准线元素。请在Revit中选中轴线、标高或参照平面后重试。";
                    return;
                }

                foreach (var id in selectionIds)
                {
                    var element = _document.GetElement(id);
                    if (element is DatumPlane datum)
                    {
                        var datumInfo = CreateDatumInfo(datum);
                        SelectedDatums.Add(datumInfo);
                    }
                }

                if (SelectedCount == 0)
                {
                    StatusMessage = $"选中了 {selectionIds.Count} 个元素，但没有有效的基准线（轴线/标高/参照平面）。";
                }
                else
                {
                    StatusMessage = $"已加载 {SelectedCount} 个基准线。请选择一条作为对齐参考线。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 创建基准线信息对象
        /// </summary>
        private DatumInfoModel CreateDatumInfo(DatumPlane datum)
        {
            var info = new DatumInfoModel
            {
                Name = datum.Name,
                Datum = datum
            };

            // 获取基准线在当前视图中的范围类型和曲线
            var extentType = datum.GetDatumExtentTypeInView(DatumEnds.End0, _activeView);
            var curves = datum.GetCurvesInView(extentType, _activeView);

            if (curves != null && curves.Any())
            {
                var curve = curves.ElementAt(0);
                if (curve is Line line)
                {
                    info.Direction = line.Direction;

                    // 确定对齐轴
                    if (Math.Round(info.Direction.X) == 1)
                        info.Axis = AlignmentAxis.XAxis;
                    else if (Math.Round(info.Direction.Y) == 1)
                        info.Axis = AlignmentAxis.YAxis;
                    else if (Math.Round(info.Direction.Z) == 1)
                        info.Axis = AlignmentAxis.ZAxis;
                }
            }

            // 设置显示名称（带方向指示）
            string axisChar;
            switch (info.Axis)
            {
                case AlignmentAxis.XAxis:
                    axisChar = "↔";
                    break;
                case AlignmentAxis.YAxis:
                    axisChar = "↕";
                    break;
                case AlignmentAxis.ZAxis:
                    axisChar = "⟷";
                    break;
                default:
                    axisChar = "?";
                    break;
            }
            info.DisplayName = $"{datum.Name} [{axisChar}]";

            return info;
        }

        /// <summary>
        /// 刷新选中状态（重新从Revit加载）
        /// </summary>
        private void RefreshSelection(Object obj)
        {
            LoadSelectedDatums(null);
            SelectedReferenceDatum = null;
            OnPropertyChanged(nameof(CanAlign));
            OnPropertyChanged(nameof(ToBeAlignedCount));
        }

        /// <summary>
        /// 执行对齐操作
        /// </summary>
        private void ExecuteAlignment(Object obj)
        {
            if (SelectedReferenceDatum == null)
            {
                StatusMessage = "请先选择参考基准线";
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "正在执行对齐操作...";

                // 获取参考基准线的曲线和方向
                var refDatum = SelectedReferenceDatum.Datum;
                var refExtentType = refDatum.GetDatumExtentTypeInView(DatumEnds.End0, _activeView);
                var refCurve = refDatum.GetCurvesInView(refExtentType, _activeView).ElementAt(0);
                var refLine = refCurve as Line;
                var refDirection = refLine.Direction;
                var refStart = refLine.GetEndPoint(0);
                var refEnd = refLine.GetEndPoint(1);

                int alignedCount = 0;

                using (var trans = new Transaction(_document, "基准线对齐"))
                {
                    trans.Start();

                    foreach (var datumInfo in SelectedDatums)
                    {
                        // 跳过参考基准线自身
                        if (datumInfo.Name == SelectedReferenceDatum.Name)
                            continue;

                        var datum = datumInfo.Datum;
                        var extentType = datum.GetDatumExtentTypeInView(DatumEnds.End0, _activeView);
                        var curve = datum.GetCurvesInView(extentType, _activeView).ElementAt(0);
                        var line = curve as Line;
                        var start = line.GetEndPoint(0);
                        var end = line.GetEndPoint(1);

                        // 计算对齐后的新曲线
                        var newCurve = CalculateAlignedCurve(refLine, refDirection, line, start, end);

                        // 应用新曲线
                        datum.SetCurveInView(extentType, _activeView, newCurve);
                        alignedCount++;
                    }

                    trans.Commit();
                }

                LastResult = new AlignmentResult
                {
                    Success = true,
                    Message = $"对齐完成！已将 {alignedCount} 个基准线与 \"{SelectedReferenceDatum.Name}\" 对齐。",
                    AlignedCount = alignedCount
                };

                StatusMessage = LastResult.Message;

                // 刷新视图
                _uiDoc.RefreshActiveView();
            }
            catch (Exception ex)
            {
                LastResult = new AlignmentResult
                {
                    Success = false,
                    Message = $"对齐失败：{ex.Message}",
                    AlignedCount = 0
                };
                StatusMessage = LastResult.Message;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 计算对齐后的曲线
        /// 根据参考线的方向和当前线的端点计算新位置
        /// </summary>
        private Curve CalculateAlignedCurve(Line refLine, XYZ refDirection, Line currentLine, XYZ start, XYZ end)
        {
            // 判断对齐轴（保留参考线坐标，保留当前线其他坐标）
            // X方向对齐：使用参考线的X坐标，保留当前线的Y、Z坐标
            if (Math.Round(refDirection.X) == 1)
            {
                return Line.CreateBound(
                    new XYZ(refLine.GetEndPoint(0).X, start.Y, start.Z),
                    new XYZ(refLine.GetEndPoint(1).X, end.Y, end.Z));
            }
            // Y方向对齐：使用参考线的Y坐标，保留当前线的X、Z坐标
            else if (Math.Round(refDirection.Y) == 1)
            {
                return Line.CreateBound(
                    new XYZ(start.X, refLine.GetEndPoint(0).Y, start.Z),
                    new XYZ(end.X, refLine.GetEndPoint(1).Y, end.Z));
            }
            // Z方向对齐：使用参考线的Z坐标，保留当前线的X、Y坐标
            else
            {
                return Line.CreateBound(
                    new XYZ(start.X, start.Y, refLine.GetEndPoint(0).Z),
                    new XYZ(end.X, end.Y, refLine.GetEndPoint(1).Z));
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(Object obj)
        {
            if (System.Windows.Application.Current.Windows.Count > 0)
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
        }
    }

    /// <summary>
    /// 基准线信息模型
    /// </summary>
    public partial class DatumInfoModel : ObserverableObject
    {
        private string _name;
        private string _displayName;
        private DatumPlane _datum;
        private XYZ _direction;
        private AlignmentAxis _axis;

        /// <summary>基准线名称</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>显示名称（带类型前缀）</summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>基准线对象</summary>
        public DatumPlane Datum
        {
            get => _datum;
            set => SetProperty(ref _datum, value);
        }

        /// <summary>基准线方向向量</summary>
        public XYZ Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        /// <summary>基准线对齐轴（X/Y/Z）</summary>
        public AlignmentAxis Axis
        {
            get => _axis;
            set => SetProperty(ref _axis, value);
        }

        /// <summary>判断X方向基准线（方向向量X≈1）</summary>
        public bool IsXAligned => Direction != null &&
            System.Math.Round(Direction.X) == 1;

        /// <summary>判断Y方向基准线（方向向量Y≈1）</summary>
        public bool IsYAligned => Direction != null &&
            System.Math.Round(Direction.Y) == 1;

        /// <summary>判断Z方向基准线（方向向量Z≈1）</summary>
        public bool IsZAligned => Direction != null &&
            System.Math.Round(Direction.Z) == 1;
    }

    /// <summary>
    /// 对齐轴枚举
    /// </summary>
    public enum AlignmentAxis
    {
        XAxis,  // X方向对齐（水平）
        YAxis,  // Y方向对齐（垂直）
        ZAxis   // Z方向对齐（高度）
    }

    /// <summary>
    /// 对齐操作结果
    /// </summary>
    public class AlignmentResult : ObserverableObject
    {
        private bool _success;
        private string _message;
        private int _alignedCount;

        public bool Success
        {
            get => _success;
            set => SetProperty(ref _success, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public int AlignedCount
        {
            get => _alignedCount;
            set => SetProperty(ref _alignedCount, value);
        }
    }
    /// <summary>
    /// 对齐轴到图标转换器
    /// </summary>
    public class AxisToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlignmentAxis axis)
            {
                switch (axis)
                {
                    case AlignmentAxis.XAxis:
                        return "↔";
                    case AlignmentAxis.YAxis:
                        return "↕";
                    case AlignmentAxis.ZAxis:
                        return "⟷";
                    default:
                        return "?";
                }
            }
            return "?";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    /// <summary>
    /// 成功状态到背景画刷转换器
    /// </summary>
    public class SuccessToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool success)
            {
                return success
                    ? new SolidColorBrush(Color.FromRgb(212, 237, 218))  // 成功绿色
                    : new SolidColorBrush(Color.FromRgb(248, 215, 218)); // 失败红色
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// 成功状态到前景画刷转换器
    /// </summary>
    public class SuccessToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool success)
            {
                return success
                    ? new SolidColorBrush(Color.FromRgb(21, 87, 36))   // 成功深绿
                    : new SolidColorBrush(Color.FromRgb(114, 28, 36)); // 失败深红
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
