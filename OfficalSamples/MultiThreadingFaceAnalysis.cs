using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 

namespace CreatePipe.OfficalSamples
{
    internal class MultiThreadingFaceAnalysis
    {

        public MultiThreadingFaceAnalysis(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                var uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null) return;

                // 使用C# 7.0 out变量内联声明
                var result = PickWallFace(uidoc, out string faceReference);

                if (result == Result.Succeeded)
                {
                    // 启动分析器（实际应用中需要注册Idling事件）
                    var analyzer = new FaceAnalyzer(uidoc.ActiveView, faceReference);
                    analyzer.Initialize();
                    analyzer.StartCalculation();
                }
                else if (result == Result.Failed)
                {
                    message = "未拾取到墙或面墙元素上的面！";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message; return;
            }
        }
        /// <summary>
        /// 让用户拾取墙面 - 使用out参数和条件运算符
        /// </summary>
        private Result PickWallFace(UIDocument uidoc, out string stableReference)
        {
            stableReference = null;

            Reference faceRef;
            try
            {
                faceRef = uidoc.Selection.PickObject(ObjectType.Face, "请拾取墙或面墙元素上的面。");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            // 使用空条件运算符和模式匹配验证元素类型
            if (faceRef == null) return Result.Failed;

            var element = uidoc.Document.GetElement(faceRef.ElementId);

            // 使用C# 7.0的is模式匹配
            if (!(element is Wall) && !(element is FaceWall))
                return Result.Failed;

            // 转换为稳定表示以便跨事务使用
            stableReference = faceRef.ConvertToStableRepresentation(uidoc.Document);
            return Result.Succeeded;
        }
    }
    /// <summary>
    /// 墙面分析器 - 管理空间字段可视化和后台计算线程
    /// </summary>
    public class FaceAnalyzer
    {
        // 使用只读字段和元组初始化
        private readonly View _view;
        private readonly string _stableReference;
        private SpatialFieldManager _spatialFieldManager;
        private int _schemaId = -1;
        private int _fieldId = -1;
        private ThreadAgent _threadAgent;
        private SharedResults _results;
        private bool _needsInitialization = true;

        public FaceAnalyzer(View view, string stableReference) =>
            (_view, _stableReference) = (view, stableReference);

        /// <summary>被分析的元素ID</summary>
        public ElementId AnalyzedElementId
        {
            get
            {
                var faceRef = GetReference();
                return faceRef?.ElementId ?? ElementId.InvalidElementId;
            }
        }

        /// <summary>从稳定表示解析引用</summary>
        private Reference GetReference() =>
            !string.IsNullOrEmpty(_stableReference) && _view != null
                ? Reference.ParseFromStableRepresentation(_view.Document, _stableReference)
                : null;

        /// <summary>获取被引用的面</summary>
        private Autodesk.Revit.DB.Face GetReferencedFace()
        {
            var faceRef = GetReference();
            if (faceRef == null) return null;

            var element = _view.Document.GetElement(faceRef);
            return element?.GetGeometryObjectFromReference(faceRef) as Autodesk.Revit.DB.Face;
        }

        /// <summary>初始化空间字段管理器</summary>
        public void Initialize()
        {
            // 获取或创建空间字段管理器 - 使用条件运算符
            _spatialFieldManager = SpatialFieldManager.GetSpatialFieldManager(_view)
                                  ?? SpatialFieldManager.CreateSpatialFieldManager(_view, 1);

            // 清除之前的结果
            _spatialFieldManager.Clear();

            // 注册分析结果架构 - 使用元组简化
            var schema = new AnalysisResultSchema(
                Guid.NewGuid().ToString(),
                "墙面分析示例");
            _schemaId = _spatialFieldManager.RegisterResult(schema);

            // 添加空间字段基元
            _fieldId = _spatialFieldManager.AddSpatialFieldPrimitive(GetReference());

            _needsInitialization = false;
        }

        /// <summary>更新结果显示 - 由Idling事件调用</summary>
        public bool UpdateResults()
        {
            // 如果需要重新初始化，表示之前的分析被中断
            if (_needsInitialization)
            {
                Initialize();
                return StartCalculation();
            }

            // 获取新产生的计算结果 - 使用out变量
            if (_results?.GetResults(out var points, out var values) == true)
            {
                var fieldPoints = new FieldDomainPointsByUV(points);
                var fieldValues = new FieldValues(values);
                _spatialFieldManager.UpdateSpatialFieldPrimitive(_fieldId, fieldPoints, fieldValues, _schemaId);
            }

            // 线程仍存活则表示还有更多结果待处理
            return _threadAgent?.IsThreadAlive == true;
        }

        /// <summary>启动后台计算线程</summary>
        public bool StartCalculation()
        {
            Autodesk.Revit.DB.Face face = GetReferencedFace();
            if (face == null) return false;

            _results = new SharedResults();
            _threadAgent = new ThreadAgent(face.GetBoundingBox(), 10, _results);

            return _threadAgent.Start();
        }

        /// <summary>停止计算</summary>
        public void StopCalculation()
        {
            _results?.SetCompleted();

            if (_threadAgent?.IsThreadAlive == true)
            {
                _threadAgent.WaitToFinish();
                _threadAgent = null;
            }
        }

        /// <summary>重启计算（例如元素变化时）</summary>
        public void RestartCalculation()
        {
            StopCalculation();
            _needsInitialization = true;
        }
    }

    /// <summary>
    /// 线程安全的结果共享容器 - 用于分析器与后台线程间通信
    /// </summary>
    public class SharedResults
    {
        private readonly List<ValueAtPoint> _values = new List<ValueAtPoint>();
        private readonly List<UV> _points = new List<UV>();
        private readonly object _lock = new object();
        private bool _isCompleted = false;
        private int _lastReadCount = 0;

        /// <summary>标记计算已完成</summary>
        public void SetCompleted()
        {
            lock (_lock) _isCompleted = true;
        }

        /// <summary>获取自上次读取以来新增的结果</summary>
        public bool GetResults(out IList<UV> points, out IList<ValueAtPoint> values)
        {
            lock (_lock)
            {
                var hasNewResults = _values.Count != _lastReadCount;

                if (hasNewResults)
                {
                    points = _points;
                    values = _values;
                    _lastReadCount = _values.Count;
                    return true;
                }
            }

            points = null;
            values = null;
            return false;
        }

        /// <summary>添加单个计算结果</summary>
        public bool AddResult(UV point, double value)
        {
            lock (_lock)
            {
                if (_isCompleted) return false;

                _values.Add(new ValueAtPoint(new List<double> { value }));
                _points.Add(point);
                return true;
            }
        }
    }

    /// <summary>
    /// 后台线程代理 - 执行实际的分析计算
    /// </summary>
    public class ThreadAgent
    {
        private Thread _thread;
        private readonly SharedResults _results;
        private readonly BoundingBoxUV _boundingBox;
        private readonly int _density;

        public ThreadAgent(BoundingBoxUV bbox, int density, SharedResults results) =>
            (_boundingBox, _density, _results) = (bbox, density, results);

        public bool IsThreadAlive => _thread?.IsAlive == true;

        /// <summary>启动后台线程</summary>
        public bool Start()
        {
            if (IsThreadAlive) return false;

            _thread = new Thread(Run);
            _thread.Start(_results);
            return true;
        }

        /// <summary>等待线程完成</summary>
        public void WaitToFinish() => _thread?.Join();

        /// <summary>后台计算主方法 - 在UV网格上计算模拟值</summary>
        private void Run(object data)
        {
            var results = data as SharedResults;
            if (results == null) return;

            // 使用元组简化变量计算
            var (uRange, vRange) = (
                _boundingBox.Max.U - _boundingBox.Min.U,
                _boundingBox.Max.V - _boundingBox.Min.V);
            var (uStep, vStep) = (uRange / _density, vRange / _density);

            for (int u = 0; u <= _density; u++)
            {
                double uPos = _boundingBox.Min.U + u * uStep;
                double uVal = u * (_density - u);

                for (int v = 0; v <= _density; v++)
                {
                    double vPos = _boundingBox.Min.V + v * vStep;
                    double vVal = v * (_density - v);

                    var point = new UV(uPos, vPos);
                    double value = Math.Min(uVal, vVal);

                    // 模拟耗时计算
                    Thread.Sleep(100);

                    // 如果添加失败（分析被中断），立即退出线程
                    if (!results.AddResult(point, value)) return;
                }
            }
        }
    }
}
