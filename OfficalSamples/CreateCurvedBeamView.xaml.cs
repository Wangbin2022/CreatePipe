using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
using static System.Security.Cryptography.ECCurve;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CreateCurvedBeamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CreateCurvedBeamView : Window
    {
        public CreateCurvedBeamView(ExternalCommandData commandData)
        {
            InitializeComponent();
            this.DataContext = new CreateCurvedBeamViewModel(commandData);
        }
    }
    /// <summary>
    /// 主视图模型 - 管理弧形梁创建逻辑
    /// </summary>
    public class CreateCurvedBeamViewModel : ObserverableObject
    {
        #region 私有字段
        private readonly UIApplication _revitApp;
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private SymbolMapModel _selectedBeamType;
        private LevelMapModel _selectedLevel;
        private bool _isBusy;
        private string _statusMessage;
        #endregion

        #region 公开属性
        /// <summary>梁类型列表</summary>
        public ObservableCollection<SymbolMapModel> BeamTypes { get; } = new ObservableCollection<SymbolMapModel>();

        /// <summary>标高列表</summary>
        public ObservableCollection<LevelMapModel> Levels { get; } = new ObservableCollection<LevelMapModel>();

        /// <summary>选中的梁类型</summary>
        public SymbolMapModel SelectedBeamType
        {
            get => _selectedBeamType;
            set => SetProperty(ref _selectedBeamType, value);
        }

        /// <summary>选中的标高</summary>
        public LevelMapModel SelectedLevel
        {
            get => _selectedLevel;
            set => SetProperty(ref _selectedLevel, value);
        }

        /// <summary>是否正在执行操作</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        #endregion

        #region 命令
        public ICommand CreateArcCommand { get; }
        public ICommand CreateEllipseCommand { get; }
        public ICommand CreateSplineCommand { get; }
        public ICommand RefreshCommand { get; }
        #endregion

        public CreateCurvedBeamViewModel(ExternalCommandData commandData)
        {
            _revitApp = commandData.Application;
            _uiDoc = _revitApp.ActiveUIDocument;
            _doc = _uiDoc.Document;

            // 初始化命令
            //CreateArcCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Arc), CanCreateBeam);
            //CreateEllipseCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Ellipse), CanCreateBeam);
            //CreateSplineCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Spline), CanCreateBeam);
            CreateArcCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Arc));
            CreateEllipseCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Ellipse));
            CreateSplineCommand = new BaseBindingCommand(_ => CreateCurvedBeam(CurveType.Spline));
            RefreshCommand = new BaseBindingCommand(LoadData);

            // 加载数据
            LoadData(null);
        }

        /// <summary>
        /// 判断是否可以创建梁（梁类型和标高都已选择）
        /// </summary>
        private bool CanCreateBeam() =>
            !IsBusy && SelectedBeamType != null && SelectedLevel != null;

        /// <summary>
        /// 加载梁类型和标高数据
        /// </summary>
        private void LoadData(Object obj)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在加载数据...";

                // 清空现有数据
                BeamTypes.Clear();
                Levels.Clear();

                // 收集所有标高
                var levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation);

                foreach (var level in levels)
                    Levels.Add(new LevelMapModel(level));

                // 收集所有结构框架族类型
                var familySymbols = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Category != null && fs.Category.Name == "Structural Framing")
                    .OrderBy(fs => fs.Family?.Name)
                    .ThenBy(fs => fs.Name);

                foreach (var symbol in familySymbols)
                    BeamTypes.Add(new SymbolMapModel(symbol));

                StatusMessage = $"加载完成：{BeamTypes.Count} 种梁类型，{Levels.Count} 个标高";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 创建曲线梁
        /// </summary>
        /// <param name="curveType">曲线类型</param>
        private void CreateCurvedBeam(CurveType curveType)
        {
            try
            {
                IsBusy = true;
                StatusMessage = $"正在创建{GetCurveTypeName(curveType)}梁...";

                // 获取标高高度
                double z = SelectedLevel.Level.Elevation;

                // 根据类型创建曲线
                Curve curve = CreateCurveByType(curveType, z);
                if (curve == null)
                {
                    StatusMessage = "曲线创建失败";
                    return;
                }

                // 创建梁实例
                using (var trans = new Transaction(_doc, $"创建{GetCurveTypeName(curveType)}梁"))
                {
                    trans.Start();

                    var beamType = SelectedBeamType.ElementType;
                    if (!beamType.IsActive)
                        beamType.Activate();

                    var beam = _doc.Create.NewFamilyInstance(
                        curve, beamType, SelectedLevel.Level, StructuralType.Beam);

                    if (beam == null)
                    {
                        StatusMessage = "梁实例创建失败";
                        trans.RollBack();
                        return;
                    }

                    trans.Commit();
                }

                StatusMessage = $"{GetCurveTypeName(curveType)}梁创建成功！";

                // 刷新视图以显示新创建的梁
                _uiDoc.RefreshActiveView();
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 根据类型创建曲线
        /// </summary>
        private Curve CreateCurveByType(CurveType type, double z)
        {
            switch (type)
            {
                case CurveType.Arc:
                    return CreateArc(z);
                case CurveType.Ellipse:
                    return CreateEllipse(z);
                case CurveType.Spline:
                    return CreateNurbSpline(z);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 创建圆弧曲线
        /// </summary>
        private Curve CreateArc(double z)
        {
            XYZ center = new XYZ(0, 0, z);
            double radius = 20.0;
            double startAngle = 0.0;
            double endAngle = 5.0;  // 约286度
            XYZ xAxis = new XYZ(1, 0, 0);
            XYZ yAxis = new XYZ(0, 1, 0);

            return Arc.Create(center, radius, startAngle, endAngle, xAxis, yAxis);
        }

        /// <summary>
        /// 创建椭圆弧曲线
        /// </summary>
        private Curve CreateEllipse(double z)
        {
            XYZ center = new XYZ(0, 0, z);
            double radX = 30.0;      // X轴半径
            double radY = 50.0;      // Y轴半径
            XYZ xVec = new XYZ(1, 0, 0);
            XYZ yVec = new XYZ(0, 1, 0);
            double startParam = 0.0;      // 起始参数
            double endParam = Math.PI;     // 结束参数（半椭圆）

            return Ellipse.CreateCurve(center, radX, radY, xVec, yVec, startParam, endParam);
        }

        /// <summary>
        /// 创建样条曲线（贝塞尔曲线）
        /// </summary>
        private Curve CreateNurbSpline(double z)
        {
            // 创建控制点（四个点定义曲线形状）
            var controlPoints = new List<XYZ>
            {
                new XYZ(-41.8875, -9.0291, z),
                new XYZ(-9.2760, 0.3221, z),
                new XYZ(9.2760, 0.3221, z),
                new XYZ(41.8875, 9.0291, z)
            };

            // 权重（所有控制点权重相同为1）
            var weights = new List<double> { 1.0, 1.0, 1.0, 1.0 };

            // 节点向量（定义样条参数化）
            var knots = new List<double> { 0, 0, 0, 0, 34.425, 34.425, 34.425, 34.425 };

            // 创建三次NURBS曲线
            return NurbSpline.CreateCurve(3, knots, controlPoints, weights);
        }

        /// <summary>
        /// 获取曲线类型的中文名称
        /// </summary>
        private static string GetCurveTypeName(CurveType type)
        {
            switch (type)
            {
                case CurveType.Arc:
                    return "圆弧";
                case CurveType.Ellipse:
                    return "椭圆弧";
                case CurveType.Spline:
                    return "样条曲线";
                default:
                    return "未知";
            }
        }
    }
    /// <summary>
    /// 梁类型映射模型 - 用于下拉列表绑定
    /// </summary>
    public class SymbolMapModel : ObserverableObject
    {
        private string _symbolName;
        private FamilySymbol _elementType;

        public string SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        public FamilySymbol ElementType
        {
            get => _elementType;
            set => SetProperty(ref _elementType, value);
        }

        public SymbolMapModel(FamilySymbol symbol)
        {
            ElementType = symbol;
            string familyName = symbol.Family?.Name ?? "";
            SymbolName = $"{familyName} : {symbol.Name}";
        }
    }

    /// <summary>
    /// 标高映射模型 - 用于下拉列表绑定
    /// </summary>
    public class LevelMapModel : ObserverableObject
    {
        private string _levelName;
        private Level _level;

        public string LevelName
        {
            get => _levelName;
            set => SetProperty(ref _levelName, value);
        }

        public Level Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public LevelMapModel(Level level)
        {
            Level = level;
            LevelName = level.Name;
        }
    }

    /// <summary>
    /// 曲线类型枚举
    /// </summary>
    public enum CurveType
    {
        Arc,        // 圆弧
        Ellipse,    // 椭圆弧
        Spline      // 样条曲线
    }
}
