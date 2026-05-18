using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DatumStyleModificationView.xaml 的交互逻辑
    /// </summary>
    public partial class DatumStyleModificationView : Window
    {
        public DatumStyleModificationView(ExternalCommandData commandData)
        {
            InitializeComponent();
            DataContext = new DatumStyleModificationViewModel(commandData);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载后自动居中于父窗口
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.Width - ActualWidth) / 2;
                Top = Owner.Top + (Owner.Height - ActualHeight) / 2;
            }
        }
    }
    /// <summary>
    /// 主视图模型 - 管理基准线样式修改逻辑
    /// </summary>
    public class DatumStyleModificationViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly Document _document;
        private readonly UIApplication _app;
        private readonly UIDocument _uiDoc;

        private DatumEndStyleModel _leftEndStyle;
        private DatumEndStyleModel _rightEndStyle;
        private bool _isProcessing;
        private string _statusMessage;
        private int _selectedCount;
        #endregion

        #region 公开属性
        /// <summary>左端样式</summary>
        public DatumEndStyleModel LeftEndStyle
        {
            get => _leftEndStyle;
            set => SetProperty(ref _leftEndStyle, value);
        }

        /// <summary>右端样式</summary>
        public DatumEndStyleModel RightEndStyle
        {
            get => _rightEndStyle;
            set => SetProperty(ref _rightEndStyle, value);
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

        /// <summary>当前选中的基准线数量</summary>
        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        /// <summary>是否有选中的基准线</summary>
        public bool HasSelectedElements => SelectedCount > 0;

        /// <summary>样式选项列表（用于UI绑定）</summary>
        public ObservableCollection<StyleOptionViewModel> StyleOptions { get; } =
            new ObservableCollection<StyleOptionViewModel>();
        #endregion

        #region 命令
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetLeftCommand { get; }
        public ICommand ResetRightCommand { get; }
        public ICommand RefreshSelectionCommand { get; }
        #endregion

        public DatumStyleModificationViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _app = commandData.Application;
            _uiDoc = _app.ActiveUIDocument;
            _document = _uiDoc.Document;

            // 初始化样式模型
            LeftEndStyle = new DatumEndStyleModel();
            RightEndStyle = new DatumEndStyleModel();

            // 初始化样式选项列表
            StyleOptions.Add(new StyleOptionViewModel(StyleOption.ShowBubble, "显示气泡", "Bubble"));
            StyleOptions.Add(new StyleOptionViewModel(StyleOption.AddElbow, "添加弯头", "Elbow"));
            StyleOptions.Add(new StyleOptionViewModel(StyleOption.Use2DExtents, "2D范围", "2DExtents"));

            // 初始化命令
            ApplyCommand = new BaseBindingCommand(ApplyModifications, _ => !IsProcessing && HasSelectedElements);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow());
            ResetLeftCommand = new BaseBindingCommand(_ => LeftEndStyle.Reset());
            ResetRightCommand = new BaseBindingCommand(_ => RightEndStyle.Reset());
            RefreshSelectionCommand = new BaseBindingCommand(UpdateSelection);

            // 更新选中数量
            UpdateSelection(null);
        }

        /// <summary>
        /// 更新当前选中的基准线数量
        /// </summary>
        private void UpdateSelection(Object obj)
        {
            var selection = _uiDoc.Selection.GetElementIds();
            SelectedCount = selection.Count(e => _document.GetElement(e) is DatumPlane);
            OnPropertyChanged(nameof(HasSelectedElements));

            StatusMessage = HasSelectedElements
                ? $"已选中 {SelectedCount} 个基准线元素（轴线/标高/参照平面）"
                : "请先在Revit中选中基准线元素（轴线、标高或参照平面）";
        }

        /// <summary>
        /// 应用样式修改到选中的基准线
        /// </summary>
        private void ApplyModifications(Object obj)
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "正在应用样式修改...";

                var selection = _uiDoc.Selection.GetElementIds();
                var activeView = _uiDoc.ActiveView;
                var datums = selection
                    .Select(id => _document.GetElement(id))
                    .OfType<DatumPlane>()
                    .ToList();

                if (!datums.Any())
                {
                    StatusMessage = "未找到有效的基准线元素";
                    return;
                }

                using (var trans = new Transaction(_document, "基准线样式修改"))
                {
                    trans.Start();

                    foreach (var datum in datums)
                    {
                        // 处理左端（端点0）
                        ModifyDatumEnd(datum, DatumEnds.End0, activeView, LeftEndStyle);

                        // 处理右端（端点1）
                        ModifyDatumEnd(datum, DatumEnds.End1, activeView, RightEndStyle);
                    }

                    trans.Commit();
                }

                StatusMessage = $"样式修改成功！已处理 {datums.Count} 个基准线元素";
            }
            catch (Exception ex)
            {
                StatusMessage = $"应用失败：{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 修改单个基准线端点的样式
        /// </summary>
        private void ModifyDatumEnd(DatumPlane datum, DatumEnds end, View view, DatumEndStyleModel style)
        {
            // 气泡显示/隐藏
            if (style.ShowBubble)
                datum.ShowBubbleInView(end, view);
            else
                datum.HideBubbleInView(end, view);

            // 2D/3D范围设置
            if (style.Use2DExtents)
                datum.SetDatumExtentType(end, view, DatumExtentType.ViewSpecific);
            else
                datum.SetDatumExtentType(end, view, DatumExtentType.Model);

            // 弯头（肘部）添加/调整
            if (style.AddElbow)
            {
                var leader = datum.GetLeader(end, view);
                if (leader == null)
                {
                    // 没有弯头，添加新弯头
                    datum.AddLeader(end, view);
                }
                else
                {
                    // 已有弯头，调整位置到中点
                    leader = CalculateLeaderWithElbow(leader);
                    datum.SetLeader(end, view, leader);
                }
            }
            else
            {
                // 不添加弯头：移除已有弯头（通过将弯头设置为锚点位置）
                var leader = datum.GetLeader(end, view);
                if (leader != null)
                {
                    leader = RemoveElbow(leader);
                    datum.SetLeader(end, view, leader);
                }
            }
        }

        /// <summary>
        /// 计算带弯头的引线（弯头位置设于锚点与端点中点）
        /// </summary>
        private static Leader CalculateLeaderWithElbow(Leader leader)
        {
            var anchor = leader.Anchor;
            var end = leader.End;

            // 弯头位置：锚点与端点的中点
            var elbow = new XYZ(
                anchor.X + (end.X - anchor.X) / 2,
                anchor.Y + (end.Y - anchor.Y) / 2,
                anchor.Z + (end.Z - anchor.Z) / 2);

            leader.Elbow = elbow;
            return leader;
        }

        /// <summary>
        /// 移除弯头（将弯头设回锚点位置）
        /// </summary>
        private static Leader RemoveElbow(Leader leader)
        {
            leader.Elbow = leader.Anchor;
            return leader;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            if (System.Windows.Application.Current.Windows.Count > 0)
            {
                var window = System.Windows.Application.Current.Windows[0];
                window?.Close();
            }
        }
    }

    /// <summary>
    /// 样式选项视图模型 - 用于CheckBox列表绑定
    /// </summary>
    public class StyleOptionViewModel : ObserverableObject
    {
        private bool _isChecked;

        public StyleOption Option { get; }
        public string DisplayName { get; }
        public string IconKey { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public StyleOptionViewModel(StyleOption option, string displayName, string iconKey)
        {
            Option = option;
            DisplayName = displayName;
            IconKey = iconKey;
        }
    }
    /// <summary>
    /// 基准线端点样式模型
    /// </summary>
    public class DatumEndStyleModel : ObserverableObject
    {
        private bool _showBubble;
        private bool _addElbow;
        private bool _use2DExtents;

        /// <summary>是否显示气泡</summary>
        public bool ShowBubble
        {
            get => _showBubble;
            set => SetProperty(ref _showBubble, value);
        }

        /// <summary>是否添加弯头（肘部）</summary>
        public bool AddElbow
        {
            get => _addElbow;
            set => SetProperty(ref _addElbow, value);
        }

        /// <summary>是否使用2D范围（而非模型范围）</summary>
        public bool Use2DExtents
        {
            get => _use2DExtents;
            set => SetProperty(ref _use2DExtents, value);
        }

        /// <summary>重置所有选项</summary>
        public void Reset()
        {
            ShowBubble = false;
            AddElbow = false;
            Use2DExtents = false;
        }
    }

    /// <summary>
    /// 样式选项枚举 - 用于UI绑定显示
    /// </summary>
    public enum StyleOption
    {
        ShowBubble,
        AddElbow,
        Use2DExtents
    }
}
