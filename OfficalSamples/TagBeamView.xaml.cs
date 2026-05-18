using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// TagBeamView.xaml 的交互逻辑
    /// </summary>
    public partial class TagBeamView : Window
    {
        private readonly TagBeamViewModel _viewModel;
        public TagBeamView(TagBeamViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
    }
    /// <summary>
    /// 梁标记视图模型
    /// </summary>
    public class TagBeamViewModel : ObserverableObject
    {
        #region 私有成员
        private readonly TaggingService _service;
        private FamilySymbolWrapper _selectedTagSymbol;
        private TagMode _selectedTagMode;
        private TagOrientation _selectedOrientation;
        private bool _hasLeader = true;
        private string _statusMessage;
        private bool _isProcessing;
        #endregion

        #region 集合属性
        public ObservableCollection<BeamInfo> SelectedBeams { get; set; }
        public ObservableCollection<FamilySymbolWrapper> CategoryTags { get; set; }
        public ObservableCollection<FamilySymbolWrapper> MaterialTags { get; set; }
        public ObservableCollection<FamilySymbolWrapper> MultiCategoryTags { get; set; }
        #endregion

        #region 绑定属性
        public FamilySymbolWrapper SelectedTagSymbol
        {
            get => _selectedTagSymbol;
            set { _selectedTagSymbol = value; OnPropertyChanged(); }
        }

        public TagMode SelectedTagMode
        {
            get => _selectedTagMode;
            set
            {
                _selectedTagMode = value;
                OnPropertyChanged();
                UpdateTagSymbolsList();
            }
        }

        public TagOrientation SelectedOrientation
        {
            get => _selectedOrientation;
            set { _selectedOrientation = value; OnPropertyChanged(); }
        }

        public bool HasLeader
        {
            get => _hasLeader;
            set { _hasLeader = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FamilySymbolWrapper> CurrentTagSymbols { get; set; }
        #endregion

        #region 命令
        public ICommand CreateTagsCommand { get; }
        public ICommand RefreshCommand { get; }
        #endregion

        public TagBeamViewModel(TaggingService service)
        {
            _service = service;

            // 初始化集合
            SelectedBeams = new ObservableCollection<BeamInfo>();
            CategoryTags = new ObservableCollection<FamilySymbolWrapper>();
            MaterialTags = new ObservableCollection<FamilySymbolWrapper>();
            MultiCategoryTags = new ObservableCollection<FamilySymbolWrapper>();
            CurrentTagSymbols = new ObservableCollection<FamilySymbolWrapper>();

            // 初始化命令
            CreateTagsCommand = new BaseBindingCommand(ExecuteCreateTags);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);

            // 加载数据
            LoadData();
        }

        #region 命令执行方法
        private async void ExecuteCreateTags(Object obj)
        {
            if (SelectedTagSymbol is null || !SelectedBeams.Any()) return;

            IsProcessing = true;
            StatusMessage = "正在创建标记...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var successCount = _service.CreateBeamTagsBatch(SelectedBeams,
                    SelectedTagSymbol, HasLeader, SelectedOrientation);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"成功创建了 {successCount} 个梁的标记（共 {SelectedBeams.Count} 个梁）";
                });
            });

            IsProcessing = false;
        }

        private bool CanExecuteCreateTags() =>
            SelectedTagSymbol != null && SelectedBeams.Any() && !IsProcessing;

        private async void ExecuteRefresh(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在刷新数据...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                LoadData();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"数据已刷新，找到 {SelectedBeams.Count} 个梁";
                });
            });

            IsProcessing = false;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            // 获取选中的梁
            var beams = _service.GetSelectedBeams();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedBeams.Clear();
                foreach (var beam in beams)
                    SelectedBeams.Add(beam);
            });

            // 获取标记符号
            var (categoryTags, materialTags, multiCategoryTags) = _service.GetTagSymbols();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateCollection(CategoryTags, categoryTags);
                UpdateCollection(MaterialTags, materialTags);
                UpdateCollection(MultiCategoryTags, multiCategoryTags);
                UpdateTagSymbolsList();
            });
        }

        /// <summary>
        /// 根据选中的标记模式更新符号列表
        /// </summary>
        private void UpdateTagSymbolsList()
        {
            CurrentTagSymbols.Clear();
            ObservableCollection<FamilySymbolWrapper> source;
            switch (SelectedTagMode)
            {
                case TagMode.TM_ADDBY_CATEGORY:
                    source = CategoryTags;
                    break;
                case TagMode.TM_ADDBY_MATERIAL:
                    source = MaterialTags;
                    break;
                case TagMode.TM_ADDBY_MULTICATEGORY:
                    source = MultiCategoryTags;
                    break;
                default:
                    source = CategoryTags;
                    break;
            }

            foreach (var item in source)
                CurrentTagSymbols.Add(item);
        }

        /// <summary>
        /// 更新集合
        /// </summary>
        private void UpdateCollection<T>(ObservableCollection<T> target, ObservableCollection<T> source)
        {
            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }
        #endregion
    }
    /// <summary>
    /// 标记服务类 - 处理所有标记相关的API操作
    /// </summary>
    public class TaggingService
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private readonly View _activeView;

        public TaggingService(ExternalCommandData commandData)
        {
            _uiDocument = commandData?.Application?.ActiveUIDocument;
            _document = _uiDocument?.Document;
            _activeView = _document?.ActiveView;
        }

        #region 梁相关服务
        /// <summary>
        /// 获取选中的梁
        /// </summary>
        public ObservableCollection<BeamInfo> GetSelectedBeams()
        {
            var beams = new ObservableCollection<BeamInfo>();

            var selectedIds = _uiDocument.Selection.GetElementIds();
            foreach (var id in selectedIds)
            {
                if (_document.GetElement(id) is FamilyInstance instance
                    && instance.StructuralType == StructuralType.Beam)
                {
                    beams.Add(new BeamInfo
                    {
                        Beam = instance,
                        Length = GetBeamLength(instance)
                    });
                }
            }

            return beams;
        }

        /// <summary>
        /// 获取梁的长度
        /// </summary>
        private double GetBeamLength(FamilyInstance beam)
        {
            if (beam.Location is LocationCurve location)
            {
                return location.Curve.Length;
            }
            return 0;
        }

        /// <summary>
        /// 获取所有可用的标记族符号
        /// </summary>
        public (ObservableCollection<FamilySymbolWrapper> categoryTags,
                 ObservableCollection<FamilySymbolWrapper> materialTags,
                 ObservableCollection<FamilySymbolWrapper> multiCategoryTags) GetTagSymbols()
        {
            var categoryTags = new ObservableCollection<FamilySymbolWrapper>();
            var materialTags = new ObservableCollection<FamilySymbolWrapper>();
            var multiCategoryTags = new ObservableCollection<FamilySymbolWrapper>();

            var families = new FilteredElementCollector(_document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .ToList();

            foreach (var family in families)
            {
                var symbolIds = family.GetFamilySymbolIds();
                foreach (var symbolId in symbolIds)
                {
                    if (_document.GetElement(symbolId) is FamilySymbol symbol)
                    {
                        var wrapper = new FamilySymbolWrapper { FamilySymbol = symbol };

                        switch (symbol.Category?.Name)
                        {
                            case "Structural Framing Tags":
                                categoryTags.Add(wrapper);
                                break;
                            case "Material Tags":
                                materialTags.Add(wrapper);
                                break;
                            case "Multi-Category Tags":
                                multiCategoryTags.Add(wrapper);
                                break;
                        }
                    }
                }
            }

            return (categoryTags, materialTags, multiCategoryTags);
        }

        /// <summary>
        /// 为梁创建标记
        /// </summary>
        public bool CreateBeamTags(BeamInfo beam, FamilySymbolWrapper tagSymbol,
            bool hasLeader, TagOrientation orientation)
        {
            if (!(beam?.Beam?.Location is LocationCurve location)) return false;

            var curve = location.Curve;
            using (var transaction = new Transaction(_document, "创建梁标记"))
            {
                transaction.Start();

                try
                {
                    // 在梁的两端创建标记
                    var beamRef = new Reference(beam.Beam);
                    var tag1 = CreateIndependentTag(beamRef, curve.GetEndPoint(0),
                        hasLeader, orientation);
                    var tag2 = CreateIndependentTag(beamRef, curve.GetEndPoint(1),
                        hasLeader, orientation);

                    // 设置标记类型
                    tag1?.ChangeTypeId(tagSymbol.FamilySymbol.Id);
                    tag2?.ChangeTypeId(tagSymbol.FamilySymbol.Id);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.RollBack();
                    return false;
                }
            }
        }

        /// <summary>
        /// 批量创建梁标记
        /// </summary>
        public int CreateBeamTagsBatch(IEnumerable<BeamInfo> beams,
            FamilySymbolWrapper tagSymbol, bool hasLeader, TagOrientation orientation)
        {
            int successCount = 0;

            using (var transaction = new Transaction(_document, "批量创建梁标记"))
            {
                transaction.Start();

                foreach (var beam in beams)
                {
                    try
                    {
                        if (CreateBeamTagsInTransaction(beam, tagSymbol, hasLeader, orientation))
                            successCount++;
                    }
                    catch { /* 记录错误但继续处理其他梁 */ }
                }

                transaction.Commit();
            }

            return successCount;
        }

        private bool CreateBeamTagsInTransaction(BeamInfo beam,
            FamilySymbolWrapper tagSymbol, bool hasLeader, TagOrientation orientation)
        {
            if (!(beam?.Beam?.Location is LocationCurve location)) return false;

            var curve = location.Curve;
            var beamRef = new Reference(beam.Beam);

            var tag1 = CreateIndependentTag(beamRef, curve.GetEndPoint(0), hasLeader, orientation);
            var tag2 = CreateIndependentTag(beamRef, curve.GetEndPoint(1), hasLeader, orientation);

            tag1?.ChangeTypeId(tagSymbol.FamilySymbol.Id);
            tag2?.ChangeTypeId(tagSymbol.FamilySymbol.Id);

            return tag1 != null && tag2 != null;
        }
        #endregion

        #region 钢筋相关服务
        /// <summary>
        /// 获取选中的钢筋
        /// </summary>
        public ObservableCollection<RebarInfo> GetSelectedRebars()
        {
            var rebars = new ObservableCollection<RebarInfo>();

            foreach (var id in _uiDocument.Selection.GetElementIds())
            {
                if (_document.GetElement(id) is Rebar rebar)
                {
                    rebars.Add(new RebarInfo { Rebar = rebar });
                }
            }

            return rebars;
        }

        /// <summary>
        /// 为钢筋创建标记
        /// </summary>
        public bool CreateRebarTag(Rebar rebar, bool hasLeader = true)
        {
            if (rebar is null) return false;

            var curves = rebar.GetCenterlineCurves(false, false, false,
                MultiplanarOption.IncludeAllMultiplanarCurves, 0);

            if (!curves.Any()) return false;

            using (var transaction = new Transaction(_document, "创建钢筋标记"))
            {
                transaction.Start();

                try
                {
                    var tag = CreateIndependentTag(new Reference(rebar),
                        curves[0].GetEndPoint(0), hasLeader, TagOrientation.Horizontal);

                    transaction.Commit();
                    return tag != null;
                }
                catch
                {
                    transaction.RollBack();
                    return false;
                }
            }
        }

        /// <summary>
        /// 为钢筋创建文字注释
        /// </summary>
        public bool CreateRebarTextNote(Rebar rebar)
        {
            if (rebar is null) return false;

            var curves = rebar.GetCenterlineCurves(false, false, false,
                MultiplanarOption.IncludeAllMultiplanarCurves, 0);

            if (!curves.Any()) return false;

            var curve = curves[0];
            var origin = new XYZ(
                curve.GetEndPoint(0).X + curve.Length,
                curve.GetEndPoint(0).Y,
                curve.GetEndPoint(0).Z);

            var text = $"这是 {rebar.Category?.Name} : {rebar.Name ?? "钢筋"}";
            var textTypeId = _document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

            using (var transaction = new Transaction(_document, "创建文字注释"))
            {
                transaction.Start();

                try
                {
                    TextNote.Create(_document, _activeView.Id, origin, text, textTypeId);
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.RollBack();
                    return false;
                }
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 创建独立标记
        /// </summary>
        private IndependentTag CreateIndependentTag(Reference elementRef, XYZ position,
            bool hasLeader, TagOrientation orientation)
        {
            //Autodesk.Revit.DB.TagMode tagMode = Autodesk.Revit.DB.TagMode.TM_ADDBY_CATEGORY;
            var tagMode = TagMode.TM_ADDBY_CATEGORY;
            var tagOrientation = orientation == TagOrientation.Horizontal
                ? Autodesk.Revit.DB.TagOrientation.Horizontal
                : Autodesk.Revit.DB.TagOrientation.Vertical;
            return IndependentTag.Create(_document, _activeView.Id, elementRef, hasLeader, tagMode, tagOrientation, position);
        }
        #endregion
    }

    ///// <summary>
    ///// 标记模式枚举
    ///// </summary>
    //public enum TagMode
    //{
    //    TM_ADDBY_CATEGORY,      // 按类别添加
    //    TM_ADDBY_MATERIAL,      // 按材质添加
    //    TM_ADDBY_MULTICATEGORY  // 按多类别添加
    //}
    ///// <summary>
    ///// 标记方向枚举
    ///// </summary>
    //public enum TagOrientation
    //{
    //    Horizontal,  // 水平
    //    Vertical     // 垂直
    //}

    /// <summary>
    /// 族符号包装器
    /// </summary>
    public class FamilySymbolWrapper : INotifyPropertyChanged
    {
        private FamilySymbol _familySymbol;

        public FamilySymbol FamilySymbol
        {
            get => _familySymbol;
            set { _familySymbol = value; OnPropertyChanged(); }
        }

        public string Name => _familySymbol?.Name ?? "Unknown";
        public string Category => _familySymbol?.Category?.Name ?? "Unknown";

        public override string ToString() => $"{Category} - {Name}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 梁信息模型
    /// </summary>
    public class BeamInfo : ObserverableObject
    {
        private FamilyInstance _beam;
        private double _length;
        private string _displayName;
        private string _status;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        public FamilyInstance Beam
        {
            get => _beam;
            set { _beam = value; OnPropertyChanged(); UpdateDisplayName(); }
        }
        public double Length
        {
            get => _length;
            set { _length = value; OnPropertyChanged(); }
        }
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }
        public ElementId Id => _beam?.Id;
        public string BeamType => _beam?.Symbol?.Name ?? "Unknown";

        private void UpdateDisplayName()
        {
            if (_beam != null)
            {
                DisplayName = $"梁 ID:{Id.IntegerValue} - {BeamType} - {Length:F2}'";
            }
        }
    }

    /// <summary>
    /// 钢筋信息模型
    /// </summary>
    public class RebarInfo : INotifyPropertyChanged
    {
        private Rebar _rebar;
        private string _displayName;

        public Rebar Rebar
        {
            get => _rebar;
            set { _rebar = value; OnPropertyChanged(); UpdateDisplayName(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public ElementId Id => _rebar?.Id;
        public string RebarType => _rebar?.GetType().Name ?? "Unknown";

        private void UpdateDisplayName()
        {
            if (_rebar != null)
            {
                DisplayName = $"钢筋 ID:{Id.IntegerValue} - {_rebar.Category?.Name}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
