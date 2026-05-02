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
    /// AreaReinforcementParameterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AreaReinforcementParameterWindow : Window
    {
        public AreaReinforcementParameterWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 面积配筋参数视图模型
    /// </summary>
    public class AreaReinforcementParameterViewModel : ObserverableObject
    {
        private readonly AreaReinforcementParameterService _service;
        private readonly AreaReinforcement _areaRein;
        private readonly bool _isWallType;

        private object _currentData;
        private bool _isLoading;

        public AreaReinforcementParameterViewModel(ExternalCommandData commandData, AreaReinforcement areaRein)
        {
            _service = new AreaReinforcementParameterService(commandData);
            _areaRein = areaRein;

            // 判断类型并加载数据
            _isWallType = TryLoadWallData() || TryLoadFloorData();

            OKCommand = new BaseBindingCommand(_ => SaveAndClose(), _ => !IsLoading);
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());
        }

        /// <summary>
        /// 当前编辑的数据对象
        /// </summary>
        public object CurrentData
        {
            get => _currentData;
            set { _currentData = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否为墙体类型
        /// </summary>
        public bool IsWallType => _isWallType;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 可用的钢筋类型列表
        /// </summary>
        public Dictionary<string, ElementId> BarTypes => _service.BarTypes;

        /// <summary>
        /// 可用的弯钩类型列表
        /// </summary>
        public Dictionary<string, ElementId> HookTypes => _service.HookTypes;

        public ICommand OKCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 尝试加载墙体数据
        /// </summary>
        private bool TryLoadWallData()
        {
            try
            {
                var wallData = _service.LoadWallData(_areaRein);
                // 验证数据有效性（至少有一个有效参数）
                if (wallData.ExteriorMajor.BarTypeId != null)
                {
                    CurrentData = wallData;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 尝试加载楼板数据
        /// </summary>
        private bool TryLoadFloorData()
        {
            try
            {
                var floorData = _service.LoadFloorData(_areaRein);
                if (floorData.TopMajor.BarTypeId != null)
                {
                    CurrentData = floorData;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 保存并关闭
        /// </summary>
        private void SaveAndClose()
        {
            IsLoading = true;

            try
            {
                if (CurrentData is WallAreaReinforcementData wallData)
                {
                    _service.SaveWallData(_areaRein, wallData);
                }
                else if (CurrentData is FloorAreaReinforcementData floorData)
                {
                    _service.SaveFloorData(_areaRein, floorData);
                }

                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("保存失败", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    /// <summary>
    /// 面积配筋参数服务类
    /// </summary>
    public class AreaReinforcementParameterService
    {
        private readonly Document _doc;
        private readonly Dictionary<string, ElementId> _hookTypes;
        private readonly Dictionary<string, ElementId> _barTypes;

        public AreaReinforcementParameterService(ExternalCommandData commandData)
        {
            _doc = commandData.Application.ActiveUIDocument.Document;
            _hookTypes = LoadHookTypes();
            _barTypes = LoadBarTypes();
        }

        /// <summary>
        /// 获取所有弯钩类型
        /// </summary>
        public Dictionary<string, ElementId> HookTypes => _hookTypes;

        /// <summary>
        /// 获取所有钢筋类型
        /// </summary>
        public Dictionary<string, ElementId> BarTypes => _barTypes;

        /// <summary>
        /// 是否有可用的钢筋类型和弯钩类型
        /// </summary>
        public bool HasRequiredTypes => _hookTypes.Any() && _barTypes.Any();

        /// <summary>
        /// 加载项目中所有弯钩类型
        /// </summary>
        private Dictionary<string, ElementId> LoadHookTypes()
        {
            var collector = new FilteredElementCollector(_doc);
            var hookTypes = collector.OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>()
                .ToDictionary(ht => ht.Name, ht => ht.Id);

            // 添加"无"选项
            hookTypes["None"] = new ElementId(-1);
            return hookTypes;
        }

        /// <summary>
        /// 加载项目中所有钢筋类型
        /// </summary>
        private Dictionary<string, ElementId> LoadBarTypes()
        {
            var collector = new FilteredElementCollector(_doc);
            return collector.OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .ToDictionary(bt => bt.Name, bt => bt.Id);
        }

        /// <summary>
        /// 从墙体面积配筋加载数据
        /// </summary>
        public WallAreaReinforcementData LoadWallData(AreaReinforcement areaRein)
        {
            var data = new WallAreaReinforcementData();

            // 加载布局规则
            var layoutParam = areaRein.get_Parameter(BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE);
            if (layoutParam != null)
                data.LayoutRule = (LayoutRules)layoutParam.AsInteger();

            // 获取所有参数
            var paras = areaRein.Parameters;

            // 外部主方向层
            data.ExteriorMajor.BarTypeId = GetParameterValueId(paras, "Exterior Major Bar Type");
            data.ExteriorMajor.HookTypeId = GetParameterValueId(paras, "Exterior Major Hook Type");
            data.ExteriorMajor.HookOrientation = GetParameterValueInt(paras, "Exterior Major Hook Orientation");

            // 外部次方向层
            data.ExteriorMinor.BarTypeId = GetParameterValueId(paras, "Exterior Minor Bar Type");
            data.ExteriorMinor.HookTypeId = GetParameterValueId(paras, "Exterior Minor Hook Type");
            data.ExteriorMinor.HookOrientation = GetParameterValueInt(paras, "Exterior Minor Hook Orientation");

            // 内部主方向层
            data.InteriorMajor.BarTypeId = GetParameterValueId(paras, "Interior Major Bar Type");
            data.InteriorMajor.HookTypeId = GetParameterValueId(paras, "Interior Major Hook Type");
            data.InteriorMajor.HookOrientation = GetParameterValueInt(paras, "Interior Major Hook Orientation");

            // 内部次方向层
            data.InteriorMinor.BarTypeId = GetParameterValueId(paras, "Interior Minor Bar Type");
            data.InteriorMinor.HookTypeId = GetParameterValueId(paras, "Interior Minor Hook Type");
            data.InteriorMinor.HookOrientation = GetParameterValueInt(paras, "Interior Minor Hook Orientation");

            return data;
        }

        /// <summary>
        /// 从楼板面积配筋加载数据
        /// </summary>
        public FloorAreaReinforcementData LoadFloorData(AreaReinforcement areaRein)
        {
            var data = new FloorAreaReinforcementData();

            // 加载布局规则
            var layoutParam = areaRein.get_Parameter(BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE);
            if (layoutParam != null)
                data.LayoutRule = (LayoutRules)layoutParam.AsInteger();

            // 顶部主方向层
            data.TopMajor.BarTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_1);
            data.TopMajor.HookTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_1);
            data.TopMajor.HookOrientation = GetBuiltInParameterInt(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_TOP_DIR_1);

            // 顶部次方向层
            data.TopMinor.BarTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_2);
            data.TopMinor.HookTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_2);
            data.TopMinor.HookOrientation = GetBuiltInParameterInt(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_TOP_DIR_2);

            // 底部主方向层
            data.BottomMajor.BarTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_1);
            data.BottomMajor.HookTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_1);
            data.BottomMajor.HookOrientation = GetBuiltInParameterInt(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_BOTTOM_DIR_1);

            // 底部次方向层
            data.BottomMinor.BarTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_2);
            data.BottomMinor.HookTypeId = GetBuiltInParameterId(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_2);
            data.BottomMinor.HookOrientation = GetBuiltInParameterInt(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_BOTTOM_DIR_2);

            return data;
        }

        /// <summary>
        /// 保存墙体面积配筋数据
        /// </summary>
        public void SaveWallData(AreaReinforcement areaRein, WallAreaReinforcementData data)
        {
            using (var trans = new Transaction(_doc, "更新面积配筋参数"))
            {
                trans.Start();

                // 保存布局规则
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE, (int)data.LayoutRule);

                // 获取所有参数集合
                var paras = areaRein.Parameters;

                // 保存各层参数
                SetParameterByName(paras, "Exterior Major Bar Type", data.ExteriorMajor.BarTypeId);
                SetParameterByName(paras, "Exterior Major Hook Type", data.ExteriorMajor.HookTypeId);
                SetParameterByName(paras, "Exterior Major Hook Orientation", data.ExteriorMajor.HookOrientation);

                SetParameterByName(paras, "Exterior Minor Bar Type", data.ExteriorMinor.BarTypeId);
                SetParameterByName(paras, "Exterior Minor Hook Type", data.ExteriorMinor.HookTypeId);
                SetParameterByName(paras, "Exterior Minor Hook Orientation", data.ExteriorMinor.HookOrientation);

                SetParameterByName(paras, "Interior Major Bar Type", data.InteriorMajor.BarTypeId);
                SetParameterByName(paras, "Interior Major Hook Type", data.InteriorMajor.HookTypeId);
                SetParameterByName(paras, "Interior Major Hook Orientation", data.InteriorMajor.HookOrientation);

                SetParameterByName(paras, "Interior Minor Bar Type", data.InteriorMinor.BarTypeId);
                SetParameterByName(paras, "Interior Minor Hook Type", data.InteriorMinor.HookTypeId);
                SetParameterByName(paras, "Interior Minor Hook Orientation", data.InteriorMinor.HookOrientation);

                trans.Commit();
            }
        }

        /// <summary>
        /// 保存楼板面积配筋数据
        /// </summary>
        public void SaveFloorData(AreaReinforcement areaRein, FloorAreaReinforcementData data)
        {
            using (var trans = new Transaction(_doc, "更新面积配筋参数"))
            {
                trans.Start();

                // 保存布局规则
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_LAYOUT_RULE, (int)data.LayoutRule);

                // 保存各层参数
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_1, data.TopMajor.BarTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_1, data.TopMajor.HookTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_TOP_DIR_1, data.TopMajor.HookOrientation);

                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_TOP_DIR_2, data.TopMinor.BarTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_2, data.TopMinor.HookTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_TOP_DIR_2, data.TopMinor.HookOrientation);

                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_1, data.BottomMajor.BarTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_1, data.BottomMajor.HookTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_BOTTOM_DIR_1, data.BottomMajor.HookOrientation);

                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_BAR_TYPE_BOTTOM_DIR_2, data.BottomMinor.BarTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_BOTTOM_DIR_2, data.BottomMinor.HookTypeId);
                SetParameterValue(areaRein, BuiltInParameter.REBAR_SYSTEM_HOOK_ORIENT_BOTTOM_DIR_2, data.BottomMinor.HookOrientation);

                trans.Commit();
            }
        }

        #region 辅助方法

        private ElementId GetParameterValueId(ParameterSet paras, string name)
        {
            var param = FindParameterByName(paras, name);
            return param?.AsElementId() ?? new ElementId(-1);
        }

        private int GetParameterValueInt(ParameterSet paras, string name)
        {
            var param = FindParameterByName(paras, name);
            return param?.AsInteger() ?? 0;
        }

        private ElementId GetBuiltInParameterId(Element elem, BuiltInParameter param)
        {
            var p = elem.get_Parameter(param);
            return p?.AsElementId() ?? new ElementId(-1);
        }

        private int GetBuiltInParameterInt(Element elem, BuiltInParameter param)
        {
            var p = elem.get_Parameter(param);
            return p?.AsInteger() ?? 0;
        }

        private Parameter FindParameterByName(ParameterSet paras, string name)
        {
            return paras.Cast<Parameter>().FirstOrDefault(p => p.Definition?.Name == name);
        }

        private void SetParameterValue(Element elem, BuiltInParameter param, int value)
        {
            var p = elem.get_Parameter(param);
            p?.Set(value);
        }

        private void SetParameterValue(Element elem, BuiltInParameter param, ElementId value)
        {
            var p = elem.get_Parameter(param);
            p?.Set(value);
        }

        private void SetParameterByName(ParameterSet paras, string name, int value)
        {
            var param = FindParameterByName(paras, name);
            param?.Set(value);
        }

        private void SetParameterByName(ParameterSet paras, string name, ElementId value)
        {
            var param = FindParameterByName(paras, name);
            param?.Set(value);
        }

        #endregion
    }
    /// <summary>
    /// 钢筋布局规则枚举
    /// </summary>
    public enum LayoutRules
    {
        FixedNumber = 2,
        MaximumSpacing = 3
    }

    /// <summary>
    /// 楼板弯钩方向枚举
    /// </summary>
    public enum FloorHookOrientations
    {
        Up = 0,
        Down = 2
    }

    /// <summary>
    /// 墙体弯钩方向枚举
    /// </summary>
    public enum WallHookOrientations
    {
        TowardsExterior = 0,
        TowardsInterior = 2
    }
    /// <summary>
    /// 图层参数组基类
    /// </summary>
    public abstract class LayerParameters : ObserverableObject
    {
        private ElementId _barTypeId;
        private ElementId _hookTypeId;
        private int _hookOrientation;

        [Category("钢筋设置")]
        [DisplayName("钢筋类型")]
        public ElementId BarTypeId
        {
            get => _barTypeId;
            set { _barTypeId = value; OnPropertyChanged(); }
        }

        [Category("钢筋设置")]
        [DisplayName("弯钩类型")]
        public ElementId HookTypeId
        {
            get => _hookTypeId;
            set { _hookTypeId = value; OnPropertyChanged(); }
        }

        [Category("钢筋设置")]
        [DisplayName("弯钩方向")]
        public int HookOrientation
        {
            get => _hookOrientation;
            set { _hookOrientation = value; OnPropertyChanged(); }
        }
    }
    /// <summary>
    /// 墙体面积配筋数据模型
    /// </summary>
    public class WallAreaReinforcementData : ObserverableObject
    {
        private LayoutRules _layoutRule;
        private LayerParameters _exteriorMajor;
        private LayerParameters _exteriorMinor;
        private LayerParameters _interiorMajor;
        private LayerParameters _interiorMinor;

        public WallAreaReinforcementData()
        {
            _exteriorMajor = new WallLayerParameters();
            _exteriorMinor = new WallLayerParameters();
            _interiorMajor = new WallLayerParameters();
            _interiorMinor = new WallLayerParameters();
        }

        [Category("构造设置")]
        [DisplayName("布局规则")]
        public LayoutRules LayoutRule
        {
            get => _layoutRule;
            set { _layoutRule = value; OnPropertyChanged(); }
        }

        [Category("外部主方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters ExteriorMajor
        {
            get => _exteriorMajor;
            set { _exteriorMajor = value; OnPropertyChanged(); }
        }

        [Category("外部次方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters ExteriorMinor
        {
            get => _exteriorMinor;
            set { _exteriorMinor = value; OnPropertyChanged(); }
        }

        [Category("内部主方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters InteriorMajor
        {
            get => _interiorMajor;
            set { _interiorMajor = value; OnPropertyChanged(); }
        }

        [Category("内部次方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters InteriorMinor
        {
            get => _interiorMinor;
            set { _interiorMinor = value; OnPropertyChanged(); }
        }
    }
    /// <summary>
    /// 墙体图层参数（带方向枚举转换）
    /// </summary>
    public class WallLayerParameters : LayerParameters
    {
        [Category("钢筋设置")]
        [DisplayName("弯钩方向")]
        public new WallHookOrientations HookOrientation
        {
            get => (WallHookOrientations)base.HookOrientation;
            set => base.HookOrientation = (int)value;
        }
    }
    /// <summary>
    /// 楼板面积配筋数据模型
    /// </summary>
    public class FloorAreaReinforcementData : ObserverableObject
    {
        private LayoutRules _layoutRule;
        private LayerParameters _topMajor;
        private LayerParameters _topMinor;
        private LayerParameters _bottomMajor;
        private LayerParameters _bottomMinor;

        public FloorAreaReinforcementData()
        {
            _topMajor = new FloorLayerParameters();
            _topMinor = new FloorLayerParameters();
            _bottomMajor = new FloorLayerParameters();
            _bottomMinor = new FloorLayerParameters();
        }

        [Category("构造设置")]
        [DisplayName("布局规则")]
        public LayoutRules LayoutRule
        {
            get => _layoutRule;
            set { _layoutRule = value; OnPropertyChanged(); }
        }

        [Category("顶部主方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters TopMajor
        {
            get => _topMajor;
            set { _topMajor = value; OnPropertyChanged(); }
        }

        [Category("顶部次方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters TopMinor
        {
            get => _topMinor;
            set { _topMinor = value; OnPropertyChanged(); }
        }

        [Category("底部主方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters BottomMajor
        {
            get => _bottomMajor;
            set { _bottomMajor = value; OnPropertyChanged(); }
        }

        [Category("底部次方向层")]
        [DisplayName("钢筋参数")]
        public LayerParameters BottomMinor
        {
            get => _bottomMinor;
            set { _bottomMinor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    /// <summary>
    /// 楼板图层参数（带方向枚举转换）
    /// </summary>
    public class FloorLayerParameters : LayerParameters
    {
        [Category("钢筋设置")]
        [DisplayName("弯钩方向")]
        public new FloorHookOrientations HookOrientation
        {
            get => (FloorHookOrientations)base.HookOrientation;
            set => base.HookOrientation = (int)value;
        }
    }
}
