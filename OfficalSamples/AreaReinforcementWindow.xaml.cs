using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AreaReinforcementWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AreaReinforcementWindow : Window
    {
        public AreaReinforcementWindow()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 面积配筋数据模型
    /// </summary>
    public class AreaReinforcementData
    {
        /// <summary>
        /// 面积配筋实例
        /// </summary>
        public AreaReinforcement AreaReinforcement { get; set; }

        /// <summary>
        /// 边界曲线列表
        /// </summary>
        public List<AreaReinforcementCurve> BoundaryCurves { get; set; } = new List<AreaReinforcementCurve>();

        /// <summary>
        /// 是否为矩形配筋区域
        /// </summary>
        public bool IsRectangular { get; set; }

        /// <summary>
        /// 垂直方向的曲线对
        /// </summary>
        public (AreaReinforcementCurve First, AreaReinforcementCurve Second) VerticalCurves { get; set; }

        /// <summary>
        /// 平行方向的曲线
        /// </summary>
        public AreaReinforcementCurve ParallelCurve { get; set; }
    }
    /// <summary>
    /// 面积配筋处理服务类
    /// </summary>
    public class AreaReinforcementService
    {
        private readonly Document _doc;
        private const double Precision = 0.00001;

        public AreaReinforcementService(ExternalCommandData commandData)
        {
            _doc = commandData.Application.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 验证并准备面积配筋数据
        /// </summary>
        /// <param name="selectedElements">选中的元素集合</param>
        /// <returns>面积配筋数据，验证失败返回null</returns>
        public AreaReinforcementData ValidateAndPrepareData(List<Element> selectedElements)
        {
            // 验证：只能选择一个面积配筋
            if (selectedElements.Count != 1)
                return null;

            var areaRein = selectedElements.First() as AreaReinforcement;
            if (areaRein == null)
                return null;

            // 获取边界曲线
            var curveIds = areaRein.GetBoundaryCurveIds();
            var curves = new List<AreaReinforcementCurve>();

            foreach (var curveId in curveIds)
            {
                var curve = _doc.GetElement(curveId) as AreaReinforcementCurve;
                if (curve == null)
                    throw new ApplicationException("面积配筋边界曲线获取失败");
                curves.Add(curve);
            }

            // 验证是否为矩形
            var lineCurves = curves.Select(c => c.Curve as Line).ToList();
            var isRectangular = IsRectangular(lineCurves);

            if (!isRectangular)
                return null;

            // 查找垂直方向曲线对
            var (verticalCurves, parallelCurve) = FindVerticalAndParallelCurves(lineCurves, curves);

            return new AreaReinforcementData
            {
                AreaReinforcement = areaRein,
                BoundaryCurves = curves,
                IsRectangular = isRectangular,
                VerticalCurves = verticalCurves,
                ParallelCurve = parallelCurve
            };
        }

        /// <summary>
        /// 关闭除主方向外的所有钢筋层
        /// </summary>
        /// <returns>操作是否成功</returns>
        public bool TurnOffLayers(AreaReinforcement areaRein)
        {
            // 尝试使用内部参数（楼板/屋顶等水平构件）
            var success = SetParameterValue(areaRein,
                BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_1, 0);
            success &= SetParameterValue(areaRein,
                BuiltInParameter.REBAR_SYSTEM_ACTIVE_BOTTOM_DIR_2, 0);
            success &= SetParameterValue(areaRein,
                BuiltInParameter.REBAR_SYSTEM_ACTIVE_TOP_DIR_2, 0);

            // 如果上述参数设置失败，尝试使用名称参数（墙体）
            if (!success)
            {
                success = SetParameterValue(areaRein, "Interior Major Direction", 0);
                success &= SetParameterValue(areaRein, "Exterior Minor Direction", 0);
                success &= SetParameterValue(areaRein, "Interior Minor Direction", 0);
            }

            return success;
        }

        /// <summary>
        /// 移除边界曲线的弯钩
        /// </summary>
        /// <returns>操作是否成功</returns>
        public bool RemoveHooks(AreaReinforcementData data)
        {
            var firstVertical = data.VerticalCurves.First;
            var secondVertical = data.VerticalCurves.Second;

            bool success = true;

            // 移除第一条垂直曲线的弯钩
            success &= SetOverrideAndRemoveHook(firstVertical, data.AreaReinforcement);

            // 移除第二条垂直曲线的弯钩
            success &= SetOverrideAndRemoveHook(secondVertical, data.AreaReinforcement);

            return success;
        }

        /// <summary>
        /// 设置曲线覆盖并移除弯钩
        /// </summary>
        private bool SetOverrideAndRemoveHook(AreaReinforcementCurve curve, AreaReinforcement areaRein)
        {
            // 设置覆盖参数
            var success = SetParameterValue(curve,
                BuiltInParameter.REBAR_SYSTEM_OVERRIDE, -1);

            // 获取并清空弯钩参数
            var hookParam = curve.get_Parameter(
                BuiltInParameter.REBAR_SYSTEM_HOOK_TYPE_TOP_DIR_1);

            if (hookParam != null && !hookParam.IsReadOnly)
            {
                hookParam.Set(new ElementId(-1));
                success &= true;
            }

            return success;
        }

        #region 几何计算方法

        /// <summary>
        /// 判断四条线段是否构成矩形
        /// </summary>
        private bool IsRectangular(List<Line> lines)
        {
            if (lines.Count != 4)
                return false;

            var firstLine = lines[0];
            var verticalLines = new List<Line>();
            Line parallelLine = null;

            for (int i = 1; i < 4; i++)
            {
                if (IsVertical(firstLine, lines[i]))
                    verticalLines.Add(lines[i]);
                else
                    parallelLine = lines[i];
            }

            // 必须有两条垂直线，一条平行线
            if (verticalLines.Count != 2 || parallelLine == null)
                return false;

            // 验证平行线与垂直线是否垂直
            return IsVertical(parallelLine, verticalLines[0]);
        }

        /// <summary>
        /// 判断两条线是否垂直
        /// </summary>
        private bool IsVertical(Line line1, Line line2)
        {
            var vector1 = line1.GetEndPoint(0) - line1.GetEndPoint(1);
            var vector2 = line2.GetEndPoint(0) - line2.GetEndPoint(1);

            var dotProduct = vector1.DotProduct(vector2);
            return Math.Abs(dotProduct) < Precision;
        }

        /// <summary>
        /// 查找垂直和平行曲线对
        /// </summary>
        private ((AreaReinforcementCurve First, AreaReinforcementCurve Second) vertical,
                 AreaReinforcementCurve parallel)
            FindVerticalAndParallelCurves(List<Line> lines, List<AreaReinforcementCurve> curves)
        {
            var firstLine = lines[0];
            var firstCurve = curves[0];

            var verticalCurves = new List<AreaReinforcementCurve>();
            AreaReinforcementCurve parallelCurve = null;

            for (int i = 1; i < 4; i++)
            {
                if (IsVertical(firstLine, lines[i]))
                    verticalCurves.Add(curves[i]);
                else
                    parallelCurve = curves[i];
            }

            return ((verticalCurves[0], verticalCurves[1]), parallelCurve);
        }

        #endregion

        #region 参数辅助方法

        /// <summary>
        /// 使用内置参数设置整数值
        /// </summary>
        private bool SetParameterValue(Element element, BuiltInParameter param, int value)
        {
            var parameter = element.get_Parameter(param);
            return parameter != null && !parameter.IsReadOnly && parameter.Set(value);
        }

        /// <summary>
        /// 使用参数名称设置整数值
        /// </summary>
        private bool SetParameterValue(Element element, string paramName, int value)
        {
            var parameter = GetParameterByName(element, paramName);
            return parameter != null && !parameter.IsReadOnly && parameter.Set(value);
        }

        /// <summary>
        /// 按名称查找参数
        /// </summary>
        private Parameter GetParameterByName(Element element, string name)
        {
            foreach (Parameter param in element.Parameters)
            {
                if (param?.Definition?.Name == name)
                    return param;
            }
            return null;
        }

        #endregion
    }
    /// <summary>
    /// 面积配筋处理视图模型
    /// </summary>
    public class AreaReinforcementViewModel : ObserverableObject
    {
        private readonly AreaReinforcementService _service;
        private readonly List<Element> _selectedElements;

        private bool _isProcessing;
        private string _statusMessage;
        private bool _operationSucceeded;
        private string _resultMessage;
        private string _errorMessage;
        private AreaReinforcementData _currentData;

        public AreaReinforcementViewModel(ExternalCommandData commandData, List<ElementId> selectedIds)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            _selectedElements = selectedIds.Select(id => doc.GetElement(id)).ToList();
            _service = new AreaReinforcementService(commandData);

            // 初始化命令
            ProcessCommand = new BaseBindingCommand(_ => ProcessAreaReinforcement(), _ => !IsProcessing);
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 自动开始处理
            ProcessAreaReinforcement();
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool OperationSucceeded
        {
            get => _operationSucceeded;
            set { _operationSucceeded = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        public ICommand ProcessCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 关闭窗口回调
        /// </summary>
        public Action CloseAction { get; set; }

        /// <summary>
        /// 处理面积配筋
        /// </summary>
        private void ProcessAreaReinforcement()
        {
            IsProcessing = true;

            try
            {
                // 步骤1：验证数据
                StatusMessage = "正在验证面积配筋数据...";
                _currentData = _service.ValidateAndPrepareData(_selectedElements);

                if (_currentData == null)
                {
                    ErrorMessage = "请选择一个矩形面积配筋";
                    OperationSucceeded = false;
                    StatusMessage = "验证失败";
                    return;
                }

                StatusMessage = "验证通过，正在处理...";

                // 步骤2：关闭图层
                var turnOffSuccess = _service.TurnOffLayers(_currentData.AreaReinforcement);
                if (!turnOffSuccess)
                {
                    ErrorMessage = "无法关闭钢筋层，请检查相关参数是否存在";
                    OperationSucceeded = false;
                    StatusMessage = "处理失败";
                    return;
                }

                StatusMessage = "钢筋层已关闭，正在移除弯钩...";

                // 步骤3：移除弯钩
                var hookSuccess = _service.RemoveHooks(_currentData);
                if (!hookSuccess)
                {
                    ErrorMessage = "无法移除弯钩，请检查相关参数是否存在";
                    OperationSucceeded = false;
                    StatusMessage = "处理失败";
                    return;
                }

                // 成功
                OperationSucceeded = true;
                ResultMessage = BuildSuccessMessage();
                StatusMessage = "处理完成";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"处理过程中发生异常: {ex.Message}";
                OperationSucceeded = false;
                StatusMessage = "处理失败";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 构建成功消息
        /// </summary>
        private string BuildSuccessMessage()
        {
            var msg = new System.Text.StringBuilder();
            msg.AppendLine("✓ 除主方向层外的所有钢筋层已关闭");
            msg.AppendLine("✓ 主方向层边界曲线的弯钩已移除");
            msg.AppendLine("");
            msg.Append("操作已完成，请检查结果是否符合预期。");
            return msg.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
