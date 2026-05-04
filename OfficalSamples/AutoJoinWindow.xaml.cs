using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AutoJoinWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AutoJoinWindow : Window
    {
        public AutoJoinWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 几何相交检测服务类
    /// </summary>
    public class GeometryIntersectionService
    {
        private readonly Options _geometryOptions;

        public GeometryIntersectionService(Autodesk.Revit.ApplicationServices.Application app)
        {
            _geometryOptions = app.Create.NewGeometryOptions();
        }

        /// <summary>
        /// 判断两个几何对象是否重叠
        /// </summary>
        public bool IsOverlapped(GeometryObject geoA, GeometryObject geoB)
        {
            List<Autodesk.Revit.DB.Face> facesA = new List<Autodesk.Revit.DB.Face>();
            var curvesB = new List<Curve>();

            GetAllFaces(geoA, facesA);
            GetAllCurves(geoB, curvesB);

            // 检测面与曲线是否相交
            foreach (Autodesk.Revit.DB.Face face in facesA)
            {
                foreach (var curve in curvesB)
                {
                    if (face.Intersect(curve) == SetComparisonResult.Overlap)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断两个组合元素是否重叠
        /// </summary>
        public bool IsElementsOverlapped(CombinableElement elemA, CombinableElement elemB)
        {
            // 同一个元素不检测
            if (elemA.Id.IntegerValue == elemB.Id.IntegerValue)
                return false;

            var geoA = elemA.get_Geometry(_geometryOptions);
            var geoB = elemB.get_Geometry(_geometryOptions);

            return IsOverlapped(geoA, geoB);
        }

        #region 几何数据提取

        /// <summary>
        /// 递归获取所有面
        /// </summary>
        private void GetAllFaces(GeometryObject geometry, List<Autodesk.Revit.DB.Face> faces)
        {
            switch (geometry)
            {
                case GeometryElement geomElem:
                    GetAllFacesFromElement(geomElem, faces);
                    break;
                case Solid solid:
                    GetAllFacesFromSolid(solid, faces);
                    break;
            }
        }

        private void GetAllFacesFromElement(GeometryElement geoElement, List<Autodesk.Revit.DB.Face> faces)
        {
            foreach (var geoObj in geoElement)
            {
                GetAllFaces(geoObj, faces);
            }
        }

        private void GetAllFacesFromSolid(Solid solid, List<Autodesk.Revit.DB.Face> faces)
        {
            if (solid?.Faces != null)
            {
                faces.AddRange(solid.Faces.Cast<Autodesk.Revit.DB.Face>());
            }
        }

        /// <summary>
        /// 递归获取所有曲线（通过曲面细分）
        /// </summary>
        private void GetAllCurves(GeometryObject geometry, List<Curve> curves)
        {
            switch (geometry)
            {
                case GeometryElement geomElem:
                    GetAllCurvesFromElement(geomElem, curves);
                    break;
                case Solid solid:
                    GetAllCurvesFromSolid(solid, curves);
                    break;
            }
        }

        private void GetAllCurvesFromElement(GeometryElement geoElement, List<Curve> curves)
        {
            foreach (var geoObj in geoElement)
            {
                GetAllCurves(geoObj, curves);
            }
        }

        private void GetAllCurvesFromSolid(Solid solid, List<Curve> curves)
        {
            if (solid?.Faces != null)
            {
                foreach (Autodesk.Revit.DB.Face face in solid.Faces)
                {
                    GetAllCurvesFromFace(face, curves);
                }
            }
        }

        private void GetAllCurvesFromFace(Autodesk.Revit.DB.Face face, List<Curve> curves)
        {
            foreach (EdgeArray edgeLoop in face.EdgeLoops)
            {
                foreach (Edge edge in edgeLoop)
                {
                    var points = edge.Tessellate();
                    // 将细分后的点连接成线段
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var line = Line.CreateBound(points[i], points[i + 1]);
                        curves.Add(line);
                    }
                }
            }
        }

        #endregion
    }
    /// <summary>
    /// 合并结果模型
    /// </summary>
    public class JoinResultModel
    {
        /// <summary>
        /// 合并成功的数量
        /// </summary>
        public int JoinedCount { get; set; }

        /// <summary>
        /// 检测到的重叠组数量
        /// </summary>
        public int OverlapGroupsCount { get; set; }

        /// <summary>
        /// 处理过程中的消息列表
        /// </summary>
        public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 是否成功执行
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
    /// <summary>
    /// 自动合并几何形体服务类
    /// </summary>
    public class AutoJoinService
    {
        private readonly Document _document;
        private readonly GeometryIntersectionService _intersectionService;
        private readonly ObservableCollection<string> _logMessages;

        public AutoJoinService(Document document, GeometryIntersectionService intersectionService)
        {
            _document = document;
            _intersectionService = intersectionService;
            _logMessages = new ObservableCollection<string>();
        }

        /// <summary>
        /// 获取处理日志
        /// </summary>
        public ObservableCollection<string> LogMessages => _logMessages;

        /// <summary>
        /// 合并选中的构件
        /// </summary>
        public JoinResultModel JoinSelectedElements(List<ElementId> selectedIds)
        {
            var result = new JoinResultModel();

            // 筛选可合并的构件（GenericForm或GeomCombination）
            var combinableElements = selectedIds
                .Select(id => _document.GetElement(id))
                .Where(IsCombinableElement)
                .Cast<CombinableElement>()
                .ToList();

            if (combinableElements.Count < 2)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "至少需要选择2个可合并的构件（通用形体或几何组合）";
                return result;
            }

            _logMessages.Add($"已选择 {combinableElements.Count} 个可合并构件");

            using (var trans = new Transaction(_document, "合并选中几何形体"))
            {
                trans.Start();

                try
                {
                    var combinableArray = new CombinableElementArray();
                    foreach (var elem in combinableElements)
                    {
                        combinableArray.Append(elem);
                    }

                    _document.CombineElements(combinableArray);
                    result.JoinedCount = combinableElements.Count;

                    _logMessages.Add($"成功合并 {result.JoinedCount} 个几何形体");

                    trans.Commit();
                    result.IsSuccess = true;
                }
                catch (System.Exception ex)
                {
                    trans.RollBack();
                    result.IsSuccess = false;
                    result.ErrorMessage = $"合并失败: {ex.Message}";
                    _logMessages.Add($"错误: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 自动合并文档中所有重叠的几何形体
        /// </summary>
        public JoinResultModel AutoJoinAllOverlapping()
        {
            var result = new JoinResultModel();
            var allElements = GetAllCombinableElements();

            if (allElements.Count < 2)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "文档中可合并的构件不足2个";
                return result;
            }

            _logMessages.Add($"找到 {allElements.Count} 个可合并构件");
            _logMessages.Add("开始检测重叠关系...");

            using (var trans = new Transaction(_document, "自动合并重叠几何形体"))
            {
                trans.Start();

                try
                {
                    var groups = FindOverlapGroups(allElements);
                    result.OverlapGroupsCount = groups.Count;
                    _logMessages.Add($"检测到 {groups.Count} 个重叠组");

                    int totalJoined = 0;
                    foreach (var group in groups)
                    {
                        if (group.Count < 2) continue;

                        var combinableArray = new CombinableElementArray();
                        foreach (var elem in group)
                        {
                            combinableArray.Append(elem);
                        }

                        _document.CombineElements(combinableArray);
                        totalJoined++;
                        _logMessages.Add($"合并组 {totalJoined}: {group.Count} 个构件");
                    }

                    result.JoinedCount = totalJoined;
                    trans.Commit();
                    result.IsSuccess = true;
                    _logMessages.Add($"全部完成，共执行 {totalJoined} 次合并操作");
                }
                catch (System.Exception ex)
                {
                    trans.RollBack();
                    result.IsSuccess = false;
                    result.ErrorMessage = $"自动合并失败: {ex.Message}";
                    _logMessages.Add($"错误: {ex.Message}");
                }
            }

            return result;
        }

        #region 私有辅助方法

        /// <summary>
        /// 判断是否为可合并的构件
        /// </summary>
        private bool IsCombinableElement(Element element)
        {
            // 跳过非实体的通用形体
            if (element is GenericForm gf && !gf.IsSolid)
                return false;

            return element is CombinableElement;
        }

        /// <summary>
        /// 获取文档中所有可合并的构件
        /// </summary>
        private List<CombinableElement> GetAllCombinableElements()
        {
            var collector = new FilteredElementCollector(_document);

            // 过滤出 GenericForm 和 GeomCombination
            var filter = new LogicalOrFilter(
                new ElementClassFilter(typeof(GenericForm)),
                new ElementClassFilter(typeof(GeomCombination)));

            return collector
                .WherePasses(filter)
                .Cast<Element>()
                .Where(IsCombinableElement)
                .Cast<CombinableElement>()
                .ToList();
        }

        /// <summary>
        /// 查找所有重叠组（使用广度优先搜索）
        /// </summary>
        private List<List<CombinableElement>> FindOverlapGroups(List<CombinableElement> allElements)
        {
            var groups = new List<List<CombinableElement>>();
            var visited = new HashSet<int>(); // 使用ElementId作为标识

            for (int i = 0; i < allElements.Count; i++)
            {
                var current = allElements[i];

                if (visited.Contains(current.Id.IntegerValue))
                    continue;

                // BFS查找所有相连的重叠构件
                var group = new List<CombinableElement>();
                var queue = new Queue<CombinableElement>();

                queue.Enqueue(current);
                visited.Add(current.Id.IntegerValue);

                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    group.Add(item);

                    // 查找与当前构件重叠的其他构件
                    foreach (var other in allElements)
                    {
                        if (visited.Contains(other.Id.IntegerValue))
                            continue;

                        if (_intersectionService.IsElementsOverlapped(item, other))
                        {
                            visited.Add(other.Id.IntegerValue);
                            queue.Enqueue(other);
                        }
                    }
                }

                if (group.Count > 0)
                    groups.Add(group);
            }

            return groups;
        }

        #endregion
    }
    /// <summary>
    /// 自动合并几何形体视图模型
    /// </summary>
    public class AutoJoinViewModel : ObserverableObject
    {
        private readonly UIDocument _uiDoc;
        private readonly AutoJoinService _joinService;

        private bool _isProcessing;
        private string _statusMessage;
        private JoinResultModel _lastResult;
        private bool _isAutoMode = true; // true: 全文档自动, false: 仅选中构件

        public AutoJoinViewModel(ExternalCommandData commandData)
        {
            _uiDoc = commandData.Application.ActiveUIDocument;

            var intersectionService = new GeometryIntersectionService(commandData.Application.Application);
            _joinService = new AutoJoinService(_uiDoc.Document, intersectionService);

            // 初始化命令
            ExecuteCommand = new BaseBindingCommand(_ => ExecuteJoin(), _ => !IsProcessing);
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 订阅日志更新
            LogMessages = _joinService.LogMessages;
        }

        /// <summary>
        /// 是否正在执行合并操作
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
        /// 执行模式（自动/手动）
        /// </summary>
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set
            {
                _isAutoMode = value;
                OnPropertyChanged();
                UpdateModeDescription();
            }
        }

        /// <summary>
        /// 模式描述文本
        /// </summary>
        public string ModeDescription { get; set; } = "自动扫描文档中所有重叠的几何形体并合并";

        /// <summary>
        /// 处理日志
        /// </summary>
        public ObservableCollection<string> LogMessages { get; }

        /// <summary>
        /// 最后执行结果
        /// </summary>
        public JoinResultModel LastResult
        {
            get => _lastResult;
            set { _lastResult = value; OnPropertyChanged(); }
        }

        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 更新模式描述
        /// </summary>
        private void UpdateModeDescription()
        {
            ModeDescription = IsAutoMode
                ? "自动模式：扫描文档中所有重叠的几何形体并合并"
                : "手动模式：仅合并当前选中的构件（至少2个）";
            OnPropertyChanged(nameof(ModeDescription));
        }

        /// <summary>
        /// 执行合并操作
        /// </summary>
        private void ExecuteJoin()
        {
            IsProcessing = true;
            StatusMessage = IsAutoMode ? "正在扫描文档..." : "正在处理选中构件...";

            try
            {
                if (IsAutoMode)
                {
                    LastResult = _joinService.AutoJoinAllOverlapping();
                }
                else
                {
                    var selectedIds = _uiDoc.Selection.GetElementIds().ToList();
                    LastResult = _joinService.JoinSelectedElements(selectedIds);
                }

                StatusMessage = LastResult.IsSuccess ? "操作完成" : "操作失败";
            }
            catch (Exception ex)
            {
                LastResult = new JoinResultModel
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                StatusMessage = "发生异常";
                LogMessages.Add($"异常: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
