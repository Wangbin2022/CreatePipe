using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DefaultElementTypesView.xaml 的交互逻辑
    /// </summary>
    public partial class DefaultElementTypesView : Page, IDockablePaneProvider
    {
        // 可停靠面板唯一标识符
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(new Guid("{B6579F42-2F4A-4552-92EF-24B3A897757D}"));
        private DefaultElementTypesViewModel _viewModel;
        public DefaultElementTypesView()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 设置视图模型并初始化数据
        /// </summary>
        public void SetViewModel(DefaultElementTypesViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        /// <summary>
        /// 设置当前文档（供外部调用）
        /// </summary>
        public void SetDocument(Document document)
        {
            _viewModel?.SetDocument(document);
        }

        /// <summary>
        /// 设置可停靠面板的初始状态
        /// </summary>
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Top
            };
        }
    }
    /// <summary>
    /// 主视图模型 - 管理默认元素类型面板的业务逻辑
    /// </summary>
    public class DefaultElementTypesViewModel : ObserverableObject, IDisposable
    {
        private readonly UIApplication _uiApp;
        private readonly ExternalEvent _externalEvent;
        private readonly DefaultTypeCommandHandler _commandHandler;
        private ElementTypeModel _model;
        private bool _disposed;

        private ObservableCollection<ElementTypeGroupRecord> _typeGroups;
        public ObservableCollection<ElementTypeGroupRecord> TypeGroups
        {
            get => _typeGroups;
            set => SetProperty(ref _typeGroups, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // 命令定义
        public ICommand RefreshCommand { get; }
        public ICommand TypeSelectionChangedCommand { get; }

        public DefaultElementTypesViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));

            // 初始化命令
            RefreshCommand = new BaseBindingCommand(_ => RefreshData());
            TypeSelectionChangedCommand = new RelayCommand<ElementTypeGroupRecord>(
                record => OnDefaultTypeChanged(record));

            // 创建外部事件处理器
            _commandHandler = new DefaultTypeCommandHandler();
            _externalEvent = ExternalEvent.Create(_commandHandler);

            TypeGroups = new ObservableCollection<ElementTypeGroupRecord>();

            // 初始化数据
            RefreshData();

            // 订阅文档变更事件
            if (_uiApp.ActiveUIDocument?.Document != null)
            {
                //_uiApp.ActiveUIDocument.Document.DocumentChanged += OnDocumentChanged;
            }
        }

        /// <summary>
        /// 设置当前文档并刷新数据
        /// </summary>
        public void SetDocument(Document document)
        {
            if (document == null) return;

            _model = new ElementTypeModel(document);
            RefreshData();
        }

        /// <summary>
        /// 刷新数据 - 重新加载所有类型组
        /// </summary>
        private void RefreshData()
        {
            if (_model?.Document == null) return;

            IsLoading = true;

            try
            {
                TypeGroups.Clear();

                foreach (var group in ElementTypeModel.GetImplementedGroups())
                {
                    var record = CreateGroupRecord(group);
                    TypeGroups.Add(record);
                }

                StatusMessage = $"已加载 {TypeGroups.Count} 个类型组";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 创建类型组记录
        /// </summary>
        private ElementTypeGroupRecord CreateGroupRecord(ElementTypeGroup group)
        {
            var candidates = _model.GetValidTypes(group);
            var defaultId = _model.GetDefaultTypeId(group);

            var record = new ElementTypeGroupRecord
            {
                GroupName = GetGroupDisplayName(group),
                Group = group,
                Candidates = candidates,
                DefaultType = candidates.FirstOrDefault(c => c.Id == defaultId)
            };

            return record;
        }

        /// <summary>
        /// 默认类型变更处理 - 触发外部事件执行修改
        /// </summary>
        private void OnDefaultTypeChanged(ElementTypeGroupRecord record)
        {
            if (record?.DefaultType == null) return;

            // 通过外部事件异步执行修改操作
            _commandHandler.SetData(record.Group, record.DefaultType.Id);
            _externalEvent.Raise();

            StatusMessage = $"已更新 {record.GroupName} 默认类型为：{record.DefaultType.Name}";
        }

        /// <summary>
        /// 获取类型组的友好显示名称
        /// </summary>
        /// <summary>
        /// 获取类型组的友好显示名称
        /// </summary>
        private static string GetGroupDisplayName(ElementTypeGroup group)
        {
            switch (group)
            {
                case ElementTypeGroup.WallType:
                    return "墙类型";
                case ElementTypeGroup.FloorType:
                    return "楼板类型";
                case ElementTypeGroup.RoofType:
                    return "屋顶类型";
                case ElementTypeGroup.CeilingType:
                    return "天花板类型";
                case ElementTypeGroup.LinearDimensionType:
                    return "线性尺寸标注";
                case ElementTypeGroup.RadialDimensionType:
                    return "半径尺寸标注";
                case ElementTypeGroup.AngularDimensionType:
                    return "角度尺寸标注";
                case ElementTypeGroup.LevelType:
                    return "标高类型";
                case ElementTypeGroup.GridType:
                    return "轴网类型";
                case ElementTypeGroup.TextNoteType:
                    return "文字注释";
                case ElementTypeGroup.ModelTextType:
                    return "模型文字";
                default:
                    return group.ToString().Replace("Type", "");
            }
        }

        /// <summary>
        /// 文档变更事件处理
        /// </summary>
        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            // 延迟刷新UI（在UI线程上执行）
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(RefreshData));
        }

        #region IDisposable 实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_uiApp.ActiveUIDocument?.Document != null)
                {
                    //_uiApp.ActiveUIDocument.Document.DocumentChanged -= OnDocumentChanged;
                }
            }

            _disposed = true;
        }

        #endregion
    }
    /// <summary>
    /// 元素类型数据模型 - 封装Revit数据访问层
    /// </summary>
    public class ElementTypeModel
    {
        private Document _document;

        public Document Document
        {
            get => _document;
            set => _document = value ?? throw new ArgumentNullException(nameof(value));
        }

        public ElementTypeModel(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// 获取指定类型组的有效候选类型列表
        /// </summary>
        public List<ElementTypeCandidate> GetValidTypes(ElementTypeGroup group)
        {
            var collector = new FilteredElementCollector(_document)
                .OfClass(typeof(ElementType));

            // 使用LINQ筛选有效的默认类型
            return collector
                .Cast<ElementType>()
                .Where(et => _document.IsDefaultElementTypeIdValid(group, et.Id))
                .Select(et => new ElementTypeCandidate
                {
                    Name = $"{et.FamilyName} - {et.Name}",
                    Id = et.Id,
                    ElementTypeGroup = group,
                    Type = et
                })
                .ToList();
        }

        /// <summary>
        /// 获取当前默认类型ID
        /// </summary>
        public ElementId GetDefaultTypeId(ElementTypeGroup group) =>
            _document.GetDefaultElementTypeId(group);

        /// <summary>
        /// 设置默认类型ID
        /// </summary>
        public void SetDefaultTypeId(ElementTypeGroup group, ElementId typeId) =>
            _document.SetDefaultElementTypeId(group, typeId);

        /// <summary>
        /// 获取所有已实现的类型组（已验证可用的）
        /// </summary>
        public static IReadOnlyList<ElementTypeGroup> GetImplementedGroups() =>
            _implementedGroups.AsReadOnly();

        // 已验证可用的类型组列表
        private static readonly List<ElementTypeGroup> _implementedGroups = new List<ElementTypeGroup>
        {
            ElementTypeGroup.RadialDimensionType,
            ElementTypeGroup.LinearDimensionType,
            ElementTypeGroup.AngularDimensionType,
            ElementTypeGroup.ArcLengthDimensionType,
            ElementTypeGroup.DiameterDimensionType,
            ElementTypeGroup.SpotElevationType,
            ElementTypeGroup.SpotCoordinateType,
            ElementTypeGroup.SpotSlopeType,
            ElementTypeGroup.LevelType,
            ElementTypeGroup.GridType,
            ElementTypeGroup.FasciaType,
            ElementTypeGroup.GutterType,
            ElementTypeGroup.EdgeSlabType,
            ElementTypeGroup.WallType,
            ElementTypeGroup.RoofType,
            ElementTypeGroup.RoofSoffitType,
            ElementTypeGroup.TagNoteType,
            ElementTypeGroup.TextNoteType,
            ElementTypeGroup.ModelTextType,
            ElementTypeGroup.MultiReferenceAnnotationType,
            ElementTypeGroup.FilledRegionType,
            ElementTypeGroup.ColorFillType,
            ElementTypeGroup.DetailGroupType,
            ElementTypeGroup.AttachedDetailGroupType,
            ElementTypeGroup.LineLoadType,
            ElementTypeGroup.AreaLoadType,
            ElementTypeGroup.PointLoadType,
            ElementTypeGroup.StairsBySketchType,
            ElementTypeGroup.RailingsTypeForStairs,
            ElementTypeGroup.RailingsTypeForRamps,
            ElementTypeGroup.RampType,
            ElementTypeGroup.StairsRailingType,
            ElementTypeGroup.StairsType,
            ElementTypeGroup.PipeType,
            ElementTypeGroup.FlexPipeType,
            ElementTypeGroup.PipeInsulationType,
            ElementTypeGroup.WireType,
            ElementTypeGroup.RebarBarType,
            ElementTypeGroup.AreaReinforcementType,
            ElementTypeGroup.PathReinforcementType,
            ElementTypeGroup.FabricAreaType,
            ElementTypeGroup.FabricSheetType,
            ElementTypeGroup.DuctType,
            ElementTypeGroup.FlexDuctType,
            ElementTypeGroup.DuctInsulationType,
            ElementTypeGroup.DuctLiningType,
            ElementTypeGroup.CableTrayType,
            ElementTypeGroup.ConduitType,
            ElementTypeGroup.CeilingType,
            ElementTypeGroup.CorniceType,
            ElementTypeGroup.RevealType,
            ElementTypeGroup.CurtainSystemType,
            ElementTypeGroup.AnalyticalLinkType,
            ElementTypeGroup.FloorType,
            ElementTypeGroup.FootingSlabType,
            ElementTypeGroup.ModelGroupType,
            ElementTypeGroup.BuildingPadType,
            ElementTypeGroup.ContourLabelingType,
            ElementTypeGroup.ReferenceViewerType,
            ElementTypeGroup.RepeatingDetailType,
            ElementTypeGroup.DecalType,
            ElementTypeGroup.BeamSystemType,
            ElementTypeGroup.ViewportType
        };
    }

    /// <summary>
    /// 元素类型候选项 - 数据实体类
    /// </summary>
    public class ElementTypeCandidate
    {
        public string Name { get; set; }
        public ElementTypeGroup ElementTypeGroup { get; set; }
        public ElementId Id { get; set; }
        public ElementType Type { get; set; }

        public override string ToString() => Name;

        // 实现相等比较（用于数据绑定）
        public override bool Equals(object obj) =>
            obj is ElementTypeCandidate other && Id.Equals(other.Id);

        public override int GetHashCode() => Id.GetHashCode();
    }

    /// <summary>
    /// 类型组记录 - 用于UI绑定
    /// </summary>
    public class ElementTypeGroupRecord : ObserverableObject
    {
        private ElementTypeCandidate _defaultType;

        public string GroupName { get; set; }
        public ElementTypeGroup Group { get; set; }

        public List<ElementTypeCandidate> Candidates { get; set; }

        public ElementTypeCandidate DefaultType
        {
            get => _defaultType;
            set => SetProperty(ref _defaultType, value);
        }
    }

    /// <summary>
    /// 默认类型修改命令处理器 - 在Revit API上下文中执行
    /// </summary>
    public class DefaultTypeCommandHandler : IExternalEventHandler
    {
        private ElementTypeGroup _elementTypeGroup;
        private ElementId _defaultTypeId;
        private bool _hasData;

        /// <summary>
        /// 设置要修改的数据
        /// </summary>
        public void SetData(ElementTypeGroup group, ElementId typeId)
        {
            _elementTypeGroup = group;
            _defaultTypeId = typeId;
            _hasData = true;
        }

        public string GetName() => "设置默认元素类型";

        /// <summary>
        /// 执行修改操作（在Revit API上下文中）
        /// </summary>
        public void Execute(UIApplication app)
        {
            if (!_hasData) return;

            var doc = app.ActiveUIDocument.Document;

            // 使用using语句确保事务正确处置
            using (var tran = new Transaction(doc, $"设置默认类型 - {_elementTypeGroup}"))
            {
                tran.Start();

                // 验证类型ID是否有效
                if (doc.IsDefaultElementTypeIdValid(_elementTypeGroup, _defaultTypeId))
                {
                    doc.SetDefaultElementTypeId(_elementTypeGroup, _defaultTypeId);
                }

                tran.Commit();
            }

            // 重置标志
            _hasData = false;
        }
    }
}
