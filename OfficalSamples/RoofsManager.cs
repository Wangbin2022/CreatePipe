using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 可创建的屋顶类型枚举
    /// </summary>
    public enum CreateRoofKind
    {
        FootPrintRoof,  // 迹线屋顶
        ExtrusionRoof   // 拉伸屋顶
    }
    /// <summary>
    /// 屋顶管理器 - 使用C# 7.3表达式体成员和元组简化代码
    /// </summary>
    public class RoofsManager
    {
        #region 私有字段
        private readonly ExternalCommandData _commandData;
        private readonly List<Level> _levels = new List<Level>();
        private readonly List<RoofType> _roofTypes = new List<RoofType>();
        private readonly FootPrintRoofManager _footPrintRoofManager;
        private readonly ExtrusionRoofManager _extrusionRoofManager;
        private readonly Selection _selection;
        private readonly ElementSet _footPrintRoofs = new ElementSet();
        private readonly ElementSet _extrusionRoofs = new ElementSet();
        private readonly CurveArray _footPrint = new CurveArray();
        private readonly CurveArray _profile = new CurveArray();
        private readonly List<ReferencePlane> _referencePlanes = new List<ReferencePlane>();
        private readonly Transaction _transaction;
        #endregion

        #region 公共属性 - 使用表达式体成员
        public CreateRoofKind RoofKind { get; set; } = CreateRoofKind.FootPrintRoof;
        public ReadOnlyCollection<Level> Levels => new ReadOnlyCollection<Level>(_levels);
        public ReadOnlyCollection<RoofType> RoofTypes => new ReadOnlyCollection<RoofType>(_roofTypes);
        public ReadOnlyCollection<ReferencePlane> ReferencePlanes => new ReadOnlyCollection<ReferencePlane>(_referencePlanes);
        public ElementSet FootPrintRoofs => _footPrintRoofs;
        public ElementSet ExtrusionRoofs => _extrusionRoofs;
        public CurveArray FootPrint => _footPrint;
        public CurveArray Profile => _profile;
        #endregion

        public RoofsManager(ExternalCommandData commandData)
        {
            _commandData = commandData ?? throw new ArgumentNullException(nameof(commandData));

            _footPrintRoofManager = new FootPrintRoofManager(commandData);
            _extrusionRoofManager = new ExtrusionRoofManager(commandData);
            _selection = commandData.Application.ActiveUIDocument.Selection;
            _transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "创建屋顶");

            Initialize();
        }

        /// <summary>
        /// 初始化数据成员 - 使用C# 7.0的of class扩展方法和LINQ简化
        /// </summary>
        private void Initialize()
        {
            var doc = _commandData.Application.ActiveUIDocument.Document;

            // 获取所有标高 - 使用Cast<T>()和ToList()
            _levels.AddRange(new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>());

            // 获取所有屋顶类型
            _roofTypes.AddRange(new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .Cast<RoofType>());

            // 获取所有迹线屋顶
            foreach (var roof in new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof)).Cast<FootPrintRoof>())
                _footPrintRoofs.Insert(roof);

            // 获取所有拉伸屋顶
            foreach (var roof in new FilteredElementCollector(doc).OfClass(typeof(ExtrusionRoof)).Cast<ExtrusionRoof>())
                _extrusionRoofs.Insert(roof);

            // 获取所有垂直参考平面 - 使用元组和解构简化
            var verticalPlanes = new FilteredElementCollector(doc)
                .OfClass(typeof(ReferencePlane))
                .Cast<ReferencePlane>()
                .Where(p => Math.Abs(p.Normal.DotProduct(XYZ.BasisZ)) < 1e-9);

            foreach (var plane in verticalPlanes)
            {
                // 使用条件运算符简化空值检查
                if (string.IsNullOrEmpty(plane.Name) || plane.Name == "Reference Plane")
                    plane.Name = $"Reference Plane ({plane.Id.IntegerValue})";
                _referencePlanes.Add(plane);
            }
        }

        /// <summary>
        /// 根据屋顶类型选择轮廓线 - 使用switch表达式(C# 8.0)
        /// </summary>
        public CurveArray WindowSelect() => RoofKind == CreateRoofKind.FootPrintRoof ? SelectFootPrint() : SelectProfile();

        /// <summary>
        /// 选择迹线屋顶轮廓线 - 墙或模型线组成的闭合环
        /// </summary>
        public CurveArray SelectFootPrint()
        {
            _footPrint.Clear();

            while (true)
            {
                var selectResult = PerformSelection("请选择墙或模型线组成的闭合环", out bool cancelled);
                if (cancelled) break;

                if (selectResult?.Any() == true)
                {
                    foreach (var element in selectResult)
                    {
                        // 使用C# 7.0的模式匹配简化类型判断
                        switch (element)
                        {
                            case Wall wall when wall.Location is LocationCurve wallCurve:
                                _footPrint.Append(wallCurve.Curve);
                                break;
                            case ModelCurve modelCurve:
                                _footPrint.Append(modelCurve.GeometryCurve);
                                break;
                        }
                    }
                    break;
                }
            }
            return _footPrint;
        }

        /// <summary>
        /// 选择拉伸屋顶轮廓线 - 连接的线/弧（不需闭合）
        /// </summary>
        public CurveArray SelectProfile()
        {
            _profile.Clear();

            while (true)
            {
                var selectResult = PerformSelection("请选择连接的线或弧（不需要闭合）", out bool cancelled);
                if (cancelled) break;

                if (selectResult?.Any() == true)
                {
                    foreach (var element in selectResult.OfType<ModelCurve>())
                        _profile.Append(element.GeometryCurve);
                    break;
                }
            }
            return _profile;
        }

        /// <summary>
        /// 执行选择操作的辅助方法 - 使用out参数返回取消状态
        /// </summary>
        private IList<Element> PerformSelection(string warningMessage, out bool cancelled)
        {
            cancelled = false;
            try
            {
                return _selection.PickElementsByRectangle();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                cancelled = true;
                return null;
            }
            catch (Exception ex)
            {
                var result = TaskDialog.Show("警告", $"{warningMessage}\n{ex.Message}",
                    TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);
                if (result == TaskDialogResult.Cancel) cancelled = true;
                return null;
            }
        }

        /// <summary>
        /// 创建迹线屋顶
        /// </summary>
        public FootPrintRoof CreateFootPrintRoof(Level level, RoofType roofType)
        {
            var roof = _footPrintRoofManager.CreateFootPrintRoof(_footPrint, level, roofType);
            if (roof != null) _footPrintRoofs.Insert(roof);
            return roof;
        }

        /// <summary>
        /// 创建拉伸屋顶
        /// </summary>
        public ExtrusionRoof CreateExtrusionRoof(ReferencePlane refPlane, Level level,
            RoofType roofType, double extrusionStart, double extrusionEnd)
        {
            var roof = _extrusionRoofManager.CreateExtrusionRoof(_profile, refPlane, level,
                roofType, extrusionStart, extrusionEnd);
            if (roof != null) _extrusionRoofs.Insert(roof);
            return roof;
        }

        #region 事务管理
        public TransactionStatus BeginTransaction() =>
            _transaction.GetStatus() == TransactionStatus.Started
                ? _transaction.GetStatus()
                : _transaction.Start();

        public TransactionStatus EndTransaction() => _transaction.Commit();
        public TransactionStatus AbortTransaction() => _transaction.RollBack();
        #endregion
    }

    /// <summary>
    /// 拉伸屋顶管理器 - 负责创建ExtrusionRoof
    /// </summary>
    class ExtrusionRoofManager
    {
        private readonly ExternalCommandData _commandData;
        private readonly Autodesk.Revit.Creation.Document _creationDoc;
        private readonly Autodesk.Revit.Creation.Application _creationApp;

        public ExtrusionRoofManager(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _creationDoc = commandData.Application.ActiveUIDocument.Document.Create;
            _creationApp = commandData.Application.Application.Create;
        }

        /// <summary>
        /// 创建拉伸屋顶 - 使用using语句自动管理事务
        /// </summary>
        public ExtrusionRoof CreateExtrusionRoof(CurveArray profile, ReferencePlane refPlane,
            Level level, RoofType roofType, double extrusionStart, double extrusionEnd)
        {
            using (var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "创建拉伸屋顶"))
            {
                transaction.Start();
                try
                {
                    var roof = _creationDoc.NewExtrusionRoof(profile, refPlane, level,
                        roofType, extrusionStart, extrusionEnd);
                    transaction.Commit();
                    return roof;
                }
                catch
                {
                    transaction.RollBack();
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 迹线屋顶管理器 - 负责创建FootPrintRoof
    /// </summary>
    class FootPrintRoofManager
    {
        private readonly ExternalCommandData _commandData;
        private readonly Autodesk.Revit.Creation.Document _creationDoc;

        public FootPrintRoofManager(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _creationDoc = commandData.Application.ActiveUIDocument.Document.Create;
        }

        /// <summary>
        /// 创建迹线屋顶 - 使用out变量和using事务
        /// </summary>
        public FootPrintRoof CreateFootPrintRoof(CurveArray footPrint, Level level, RoofType roofType)
        {
            using (var transaction = new Transaction(_commandData.Application.ActiveUIDocument.Document, "创建迹线屋顶"))
            {
                transaction.Start();
                try
                {
                    // 使用out变量内联声明
                    ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                    var roof = _creationDoc.NewFootPrintRoof(footPrint, level, roofType,
                        out footPrintToModelCurveMapping);
                    transaction.Commit();
                    return roof;
                }
                catch
                {
                    transaction.RollBack();
                    throw;
                }
            }
        }
    }
}
