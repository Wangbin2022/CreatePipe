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
    /// DatumPropagationView.xaml 的交互逻辑
    /// </summary>
    public partial class DatumPropagationView : Window
    {
        public DatumPropagationView(ExternalCommandData commandData)
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主视图模型 - 管理基准线范围传播逻辑
    /// </summary>
    public class DatumPropagationViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private readonly UIApplication _app;
        private readonly UIDocument _uiDoc;
        private readonly View _activeView;

        private ObservableCollection<ViewInfoModel> _propagationViews;
        private DatumInfoModel _selectedDatum;
        private bool _isLoading;
        private bool _isPropagating;
        private string _statusMessage;
        private PropagationResult _lastResult;
        private int _selectedCount;
        #endregion

        #region 公开属性
        /// <summary>可传播的视图列表</summary>
        public ObservableCollection<ViewInfoModel> PropagationViews
        {
            get => _propagationViews;
            set => SetProperty(ref _propagationViews, value);
        }

        /// <summary>选中的基准线</summary>
        public DatumInfoModel SelectedDatum
        {
            get => _selectedDatum;
            set => SetProperty(ref _selectedDatum, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>是否正在传播</summary>
        public bool IsPropagating
        {
            get => _isPropagating;
            set => SetProperty(ref _isPropagating, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>上次传播结果</summary>
        public PropagationResult LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }

        /// <summary>选中视图数量</summary>
        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        /// <summary>是否可以执行传播</summary>
        public bool CanPropagate => !IsLoading && !IsPropagating &&
                                     SelectedDatum != null && SelectedCount > 0;

        /// <summary>全选/全不选状态</summary>
        public bool? SelectAllState
        {
            get
            {
                if (PropagationViews == null || PropagationViews.Count == 0) return false;
                var selected = PropagationViews.Count(v => v.IsSelected);
                if (selected == 0) return false;
                if (selected == PropagationViews.Count) return true;
                return null;
            }
            set
            {
                if (value.HasValue && PropagationViews != null)
                {
                    var isSelected = value.Value;
                    foreach (var view in PropagationViews)
                        view.IsSelected = isSelected;
                    UpdateSelectedCount(null);
                }
            }
        }
        #endregion

        #region 命令
        public ICommand PropagateCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand SelectNoneCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        public DatumPropagationViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _app = commandData.Application;
            _uiDoc = _app.ActiveUIDocument;
            _document = _uiDoc.Document;
            _activeView = _uiDoc.ActiveView;

            PropagationViews = new ObservableCollection<ViewInfoModel>();

            // 初始化命令
            PropagateCommand = new BaseBindingCommand(ExecutePropagation, _ => CanPropagate);
            SelectAllCommand = new BaseBindingCommand(_ => SelectAllState = true);
            SelectNoneCommand = new BaseBindingCommand(_ => SelectAllState = false);
            RefreshCommand = new BaseBindingCommand(LoadPropagationViews);
            CancelCommand = new BaseBindingCommand(CloseWindow);

            // 订阅选中变化事件
            if (PropagationViews != null)
                PropagationViews.CollectionChanged += (s, e) => UpdateSelectedCount(null);

            // 加载数据
            LoadSelectedDatum(null);
            LoadPropagationViews(null);
        }

        /// <summary>
        /// 加载选中的基准线
        /// </summary>
        private void LoadSelectedDatum(Object obj)
        {
            try
            {
                var selectionIds = _uiDoc.Selection.GetElementIds();
                if (selectionIds == null || selectionIds.Count == 0)
                {
                    StatusMessage = "未选中任何基准线元素。请先在Revit中选中一个基准线（轴线、标高或参照平面）。";
                    return;
                }

                var firstDatumId = selectionIds.First();
                var element = _document.GetElement(firstDatumId);

                if (element is DatumPlane datum)
                {
                    SelectedDatum = new DatumInfoModel
                    {
                        Name = datum.Name,
                        Datum = datum,
                        DatumId = datum.Id,
                        DatumType = GetDatumTypeName(datum)
                    };
                    StatusMessage = $"已选中基准线：{SelectedDatum.DisplayName}";
                }
                else
                {
                    StatusMessage = "选中的元素不是有效的基准线（轴线、标高或参照平面）。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载基准线失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 获取基准线类型名称
        /// </summary>
        private string GetDatumTypeName(DatumPlane datum)
        {
            if (datum is Autodesk.Revit.DB.Grid)
                return "轴线";
            else if (datum is Level)
                return "标高";
            else if (datum is ReferencePlane)
                return "参照平面";
            else
                return "基准线";
        }

        /// <summary>
        /// 加载可传播的视图列表
        /// </summary>
        private void LoadPropagationViews(Object obj)
        {
            if (SelectedDatum?.Datum == null)
            {
                StatusMessage = "请先选中有效的基准线元素。";
                return;
            }

            try
            {
                IsLoading = true;
                PropagationViews.Clear();

                // 获取基准线可以传播到的视图集合
                var propagationViewIds = SelectedDatum.Datum.GetPropagationViews(_activeView);

                if (propagationViewIds == null || propagationViewIds.Count == 0)
                {
                    StatusMessage = "当前基准线无法传播到任何其他视图。";
                    return;
                }

                foreach (var viewId in propagationViewIds)
                {
                    var view = _document.GetElement(viewId) as View;
                    if (view != null && !view.IsTemplate)
                    {
                        PropagationViews.Add(new ViewInfoModel
                        {
                            ViewId = viewId,
                            ViewType = view.ViewType,
                            ViewName = view.Name,
                            IsSelected = false
                        });
                    }
                }

                // 按视图类型和名称排序
                var sorted = PropagationViews
                    .OrderBy(v => v.ViewType.ToString())
                    .ThenBy(v => v.ViewName)
                    .ToList();

                PropagationViews.Clear();
                foreach (var view in sorted)
                    PropagationViews.Add(view);

                StatusMessage = $"已加载 {PropagationViews.Count} 个可传播的目标视图。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载视图列表失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新选中视图数量
        /// </summary>
        private void UpdateSelectedCount(Object obj)
        {
            SelectedCount = PropagationViews?.Count(v => v.IsSelected) ?? 0;
            OnPropertyChanged(nameof(SelectAllState));
            OnPropertyChanged(nameof(CanPropagate));
        }

        /// <summary>
        /// 执行范围传播
        /// </summary>
        private void ExecutePropagation(Object obj)
        {
            if (SelectedDatum?.Datum == null)
            {
                StatusMessage = "未找到有效的基准线元素。";
                return;
            }

            if (SelectedCount == 0)
            {
                StatusMessage = "请至少选择一个目标视图。";
                return;
            }

            try
            {
                IsPropagating = true;
                StatusMessage = "正在传播基准线范围...";

                // 获取选中的视图ID集合
                var selectedViewIds = PropagationViews
                    .Where(v => v.IsSelected)
                    .Select(v => v.ViewId)
                    .ToHashSet();

                using (var trans = new Transaction(_document, "基准线范围传播"))
                {
                    trans.Start();

                    // 执行范围传播：将当前视图中的基准线范围应用到选中的目标视图
                    SelectedDatum.Datum.PropagateToViews(_activeView, selectedViewIds);

                    trans.Commit();
                }

                LastResult = new PropagationResult
                {
                    Success = true,
                    Message = $"传播成功！已将基准线 \"{SelectedDatum.Name}\" 的范围从当前视图传播到 {SelectedCount} 个目标视图。",
                    PropagatedCount = SelectedCount
                };

                StatusMessage = LastResult.Message;

                // 刷新视图显示
                _uiDoc.RefreshActiveView();
            }
            catch (Exception ex)
            {
                LastResult = new PropagationResult
                {
                    Success = false,
                    Message = $"传播失败：{ex.Message}",
                    PropagatedCount = 0
                };
                StatusMessage = LastResult.Message;
            }
            finally
            {
                IsPropagating = false;
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
    /// 视图信息模型
    /// </summary>
    public class ViewInfoModel : ObserverableObject
    {
        private bool _isSelected;
        private string _displayName;
        private ElementId _viewId;
        private ViewType _viewType;
        private string _viewName;

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>显示名称（视图类型: 视图名称）</summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>视图ID</summary>
        public ElementId ViewId
        {
            get => _viewId;
            set => SetProperty(ref _viewId, value);
        }

        /// <summary>视图类型</summary>
        public ViewType ViewType
        {
            get => _viewType;
            set
            {
                SetProperty(ref _viewType, value);
                UpdateDisplayName();
            }
        }

        /// <summary>视图名称</summary>
        public string ViewName
        {
            get => _viewName;
            set
            {
                SetProperty(ref _viewName, value);
                UpdateDisplayName();
            }
        }

        /// <summary>视图类型图标标识</summary>
        public string ViewTypeIcon
        {
            get
            {
                switch (ViewType)
                {
                    case ViewType.FloorPlan:
                        return "📐";
                    case ViewType.CeilingPlan:
                        return "📈";
                    case ViewType.Elevation:
                        return "📏";
                    case ViewType.Section:
                        return "✂️";
                    case ViewType.ThreeD:
                        return "🧊";
                    case ViewType.DraftingView:
                        return "📄";
                    case ViewType.Legend:
                        return "📖";
                    case ViewType.Schedule:
                        return "📊";
                    default:
                        return "👁️";
                }
            }
        }
        private void UpdateDisplayName() =>
            DisplayName = $"{ViewTypeIcon} {ViewType} : {ViewName}";
    }

    /// <summary>
    /// 基准线信息模型
    /// </summary>
    public partial class DatumInfoModel : ObserverableObject
    {

        private ElementId _datumId;
        private string _datumType;
        public ElementId DatumId
        {
            get => _datumId;
            set => SetProperty(ref _datumId, value);
        }
        public string DatumType
        {
            get => _datumType;
            set => SetProperty(ref _datumType, value);
        }
        public string DisplayName2 => $"{DatumType} : {Name}";
    }

    /// <summary>
    /// 传播操作结果
    /// </summary>
    public class PropagationResult : ObserverableObject
    {
        private bool _success;
        private string _message;
        private int _propagatedCount;
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
        public int PropagatedCount
        {
            get => _propagatedCount;
            set => SetProperty(ref _propagatedCount, value);
        }
    }
    /// <summary>
    /// 数量到布尔值转换器
    /// </summary>
    public class CountToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value as int? ?? 0;
            return count > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    /// <summary>
    /// 视图类型到画刷转换器
    /// </summary>
    public class ViewTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ViewType viewType)
            {
                SolidColorBrush brush;
                switch (viewType)
                {
                    case ViewType.FloorPlan:
                    case ViewType.CeilingPlan:
                        brush = new SolidColorBrush(Color.FromRgb(52, 152, 219));   // 蓝色
                        break;
                    case ViewType.Elevation:
                        brush = new SolidColorBrush(Color.FromRgb(46, 204, 113));   // 绿色
                        break;
                    case ViewType.Section:
                        brush = new SolidColorBrush(Color.FromRgb(231, 76, 60));    // 红色
                        break;
                    case ViewType.ThreeD:
                        brush = new SolidColorBrush(Color.FromRgb(155, 89, 182));   // 紫色
                        break;
                    case ViewType.DraftingView:
                        brush = new SolidColorBrush(Color.FromRgb(241, 196, 15));   // 黄色
                        break;
                    default:
                        brush = new SolidColorBrush(Color.FromRgb(149, 165, 166));  // 灰色
                        break;
                }
                return brush;
            }
            return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    /// <summary>
    /// 成功状态到背景画刷转换器
    /// </summary>
    //public class SuccessToBrushConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is bool success)
    //        {
    //            return success
    //                ? new SolidColorBrush(Color.FromRgb(212, 237, 218))  // 成功绿色
    //                : new SolidColorBrush(Color.FromRgb(248, 215, 218)); // 失败红色
    //        }
    //        return new SolidColorBrush(Colors.Transparent);
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    //        throw new NotImplementedException();
    //}

    ///// <summary>
    ///// 成功状态到前景画刷转换器
    ///// </summary>
    //public class SuccessToForegroundConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is bool success)
    //        {
    //            return success
    //                ? new SolidColorBrush(Color.FromRgb(21, 87, 36))   // 成功深绿
    //                : new SolidColorBrush(Color.FromRgb(114, 28, 36)); // 失败深红
    //        }
    //        return new SolidColorBrush(Colors.Black);
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    //        throw new NotImplementedException();
    //}


}
