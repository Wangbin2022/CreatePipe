using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// NewRoofView.xaml 的交互逻辑
    /// </summary>
    public partial class NewRoofView : Window
    {
        public NewRoofView(ExternalCommandData commandData, RoofsManager roofsManager)
        {
            InitializeComponent();
            this.DataContext = new NewRoofViewModel(commandData, roofsManager);

        }
    }
    /// <summary>
    /// 主窗体ViewModel - 管理屋顶列表显示和创建操作
    /// </summary>
    public class NewRoofViewModel : ObserverableObject
    {
        #region 字段
        private readonly RoofsManager _roofsManager;
        private readonly ExternalCommandData _commandData;
        private RoofItem _selectedFootPrintRoof;
        private RoofItem _selectedExtrusionRoof;
        private bool _isProcessing;
        #endregion

        #region 属性
        public ObservableCollection<RoofItem> FootPrintRoofs { get; } = new ObservableCollection<RoofItem>();
        public ObservableCollection<RoofItem> ExtrusionRoofs { get; } = new ObservableCollection<RoofItem>();

        public ObservableCollection<Level> Levels { get; } = new ObservableCollection<Level>();
        public ObservableCollection<RoofType> RoofTypes { get; } = new ObservableCollection<RoofType>();
        public ObservableCollection<ReferencePlane> ReferencePlanes { get; } = new ObservableCollection<ReferencePlane>();

        public RoofItem SelectedFootPrintRoof
        {
            get => _selectedFootPrintRoof;
            set { _selectedFootPrintRoof = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEditRoof)); }
        }

        public RoofItem SelectedExtrusionRoof
        {
            get => _selectedExtrusionRoof;
            set { _selectedExtrusionRoof = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEditRoof)); }
        }

        public Level SelectedLevel { get; set; }
        public RoofType SelectedFootPrintRoofType { get; set; }
        public RoofType SelectedExtrusionRoofType { get; set; }
        public ReferencePlane SelectedReferencePlane { get; set; }
        public Level SelectedRefLevel { get; set; }
        public string ExtrusionStart { get; set; } = "0";
        public string ExtrusionEnd { get; set; } = "1000";

        public bool CanEditRoof => SelectedFootPrintRoof != null || SelectedExtrusionRoof != null;
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }
        #endregion

        #region 命令
        public ICommand SelectFootPrintCommand;
        public ICommand SelectProfileCommand;
        public ICommand CreateFootPrintRoofCommand;
        public ICommand CreateExtrusionRoofCommand;
        public ICommand EditRoofCommand;
        public ICommand RefreshCommand;
        public ICommand OkCommand;
        public ICommand CancelCommand;
        #endregion

        public NewRoofViewModel(ExternalCommandData commandData, RoofsManager roofsManager)
        {
            _commandData = commandData;
            _roofsManager = roofsManager ?? throw new ArgumentNullException(nameof(roofsManager));

            InitializeCommands();
            LoadData();
        }

        private void InitializeCommands()
        {
            SelectFootPrintCommand = new BaseBindingCommand(_ => SelectFootPrint(), _ => !IsProcessing);
            SelectProfileCommand = new BaseBindingCommand(_ => SelectProfile(), _ => !IsProcessing);
            CreateFootPrintRoofCommand = new BaseBindingCommand(_ => CreateFootPrintRoof(), _ => !IsProcessing);
            CreateExtrusionRoofCommand = new BaseBindingCommand(_ => CreateExtrusionRoof(), _ => !IsProcessing);
            EditRoofCommand = new BaseBindingCommand(_ => EditRoof(), _ => CanEditRoof && !IsProcessing);
            RefreshCommand = new BaseBindingCommand(_ => RefreshData());
            OkCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        /// <summary>
        /// 加载Revit数据 - 使用LINQ和表达式体简化
        /// </summary>
        private void LoadData()
        {
            // 加载标高
            Levels.Clear();
            foreach (var level in _roofsManager.Levels)
                Levels.Add(level);

            // 加载屋顶类型
            RoofTypes.Clear();
            foreach (var type in _roofsManager.RoofTypes)
                RoofTypes.Add(type);

            // 加载参考平面
            ReferencePlanes.Clear();
            foreach (var plane in _roofsManager.ReferencePlanes)
                ReferencePlanes.Add(plane);

            RefreshData();
        }

        /// <summary>
        /// 刷新屋顶列表
        /// </summary>
        private void RefreshData()
        {
            IsProcessing = true;
            try
            {
                // 更新迹线屋顶列表
                FootPrintRoofs.Clear();
                foreach (var roof in _roofsManager.FootPrintRoofs.Cast<FootPrintRoof>())
                    FootPrintRoofs.Add(new RoofItem(roof));

                // 更新拉伸屋顶列表
                ExtrusionRoofs.Clear();
                foreach (var roof in _roofsManager.ExtrusionRoofs.Cast<ExtrusionRoof>())
                    ExtrusionRoofs.Add(new RoofItem(roof));
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 选择迹线轮廓
        /// </summary>
        private void SelectFootPrint()
        {
            IsProcessing = true;
            try
            {
                _roofsManager.WindowSelect();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 选择拉伸轮廓
        /// </summary>
        private void SelectProfile()
        {
            IsProcessing = true;
            try
            {
                _roofsManager.WindowSelect();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 创建迹线屋顶
        /// </summary>
        private void CreateFootPrintRoof()
        {
            if (SelectedLevel == null || SelectedFootPrintRoofType == null)
            {
                TaskDialog.Show("提示", "请选择标高和屋顶类型");
                return;
            }

            IsProcessing = true;
            try
            {
                _roofsManager.BeginTransaction();
                var roof = _roofsManager.CreateFootPrintRoof(SelectedLevel, SelectedFootPrintRoofType);
                _roofsManager.EndTransaction();

                if (roof != null)
                {
                    RefreshData();
                    TaskDialog.Show("成功", "迹线屋顶创建成功");
                }
            }
            catch (Exception ex)
            {
                _roofsManager.AbortTransaction();
                TaskDialog.Show("错误", $"创建失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 创建拉伸屋顶
        /// </summary>
        private void CreateExtrusionRoof()
        {
            if (SelectedReferencePlane == null || SelectedRefLevel == null || SelectedExtrusionRoofType == null)
            {
                TaskDialog.Show("提示", "请填写所有必需信息");
                return;
            }

            // 使用元组简化参数传递
            if (!(double.TryParse(ExtrusionStart, out double start) && double.TryParse(ExtrusionEnd, out double end)))
            {
                TaskDialog.Show("提示", "拉伸起点和终点必须是有效的数字");
                return;
            }

            IsProcessing = true;
            try
            {
                _roofsManager.BeginTransaction();
                var roof = _roofsManager.CreateExtrusionRoof(SelectedReferencePlane, SelectedRefLevel,
                    SelectedExtrusionRoofType, start, end);
                _roofsManager.EndTransaction();

                if (roof != null)
                {
                    RefreshData();
                    TaskDialog.Show("成功", "拉伸屋顶创建成功");
                }
            }
            catch (Exception ex)
            {
                _roofsManager.AbortTransaction();
                TaskDialog.Show("错误", $"创建失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 编辑选中的屋顶
        /// </summary>
        private void EditRoof()
        {
            var roofToEdit = SelectedFootPrintRoof?.Roof ?? SelectedExtrusionRoof?.Roof;
            if (roofToEdit == null) return;

            IsProcessing = true;
            try
            {
                var editorVm = new NewRoofEditorViewModel(_commandData, roofToEdit);
                var editorWindow = new NewRoofEditorView { DataContext = editorVm };
                editorVm.CloseWindow = () => editorWindow.Close();

                if (editorWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 屋顶项ViewModel - 用于列表显示
    /// </summary>
    public class RoofItem
    {
        public RoofBase Roof { get; }
        public string RoofId => Roof.Id.IntegerValue.ToString();
        public string Name => Roof.Name;
        public string LevelName => GetLevelName();
        public string RoofTypeName => Roof.RoofType?.Name ?? "未知";

        public RoofItem(RoofBase roof) => Roof = roof;

        private string GetLevelName()
        {
            var param = Roof is FootPrintRoof fp
                ? fp.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM)
                : Roof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);

            if (param?.AsElementId() is ElementId id && id != ElementId.InvalidElementId)
            {
                var level = Roof.Document.GetElement(id) as Level;
                return level?.Name ?? "未知";
            }
            return "未知";
        }
    }
}
