using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 管道碰撞解析服务类
    /// 负责解决管道与周边构件的碰撞问题
    /// </summary>
    public class PipeCollisionResolver
    {
        private readonly Document _document;
        private readonly CollisionDetector _detector;
        private readonly PipingSystemType _pipingSystemType;

        private const double MinClearance = 0.5; // 最小间隙（英尺）
        private const double MaxSearchDistance = 1000; // 最大搜索距离

        /// <summary>
        /// 构造函数
        /// </summary>
        public PipeCollisionResolver(ExternalCommandData commandData)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _detector = new CollisionDetector(_document);
            _pipingSystemType = GetPipingSystemType();
        }

        /// <summary>
        /// 获取供暖系统类型
        /// </summary>
        private PipingSystemType GetPipingSystemType()
        {
            var collector = new FilteredElementCollector(_document);
            return collector.OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(t => t.SystemClassification == MEPSystemClassification.SupplyHydronic ||
                                      t.SystemClassification == MEPSystemClassification.ReturnHydronic);
        }

        /// <summary>
        /// 解析所有管道的碰撞问题
        /// </summary>
        public int ResolveAllPipes()
        {
            var collector = new FilteredElementCollector(_document);
            var pipes = collector.OfClass(typeof(Pipe)).Cast<Pipe>().ToList();

            int resolvedCount = 0;
            foreach (var pipe in pipes)
            {
                if (ResolvePipeCollision(pipe))
                    resolvedCount++;
            }

            return resolvedCount;
        }

        /// <summary>
        /// 解析单个管道的碰撞问题
        /// </summary>
        private bool ResolvePipeCollision(Pipe pipe)
        {
            var centerLine = (pipe.Location as LocationCurve)?.Curve as Line;
            if (centerLine == null) return false;

            // 检测碰撞
            var collisions = _detector.DetectAlongLine(centerLine);
            var filteredCollisions = FilterCollisions(pipe, collisions);

            if (!filteredCollisions.Any()) return false;

            var direction = (centerLine.GetEndPoint(1) - centerLine.GetEndPoint(0)).Normalize();

            // 构建碰撞区段
            var sections = CollisionSection.BuildSections(filteredCollisions, direction);
            MergeAdjacentSections(pipe, sections);

            // 为每个区段创建绕行路径
            foreach (var section in sections)
            {
                CreateBypassForSection(pipe, section);
            }

            // 连接各区段
            ConnectSections(pipe, sections);

            // 连接两端
            ConnectEnds(pipe, sections, centerLine);

            // 删除原管道
            _document.Delete(pipe.Id);

            return true;
        }

        /// <summary>
        /// 过滤碰撞结果，只保留需要处理的构件
        /// </summary>
        private List<ReferenceWithContext> FilterCollisions(Pipe pipe, List<ReferenceWithContext> collisions)
        {
            return collisions.Where(c =>
            {
                var element = _document.GetElement(c.GetReference());
                return element.Id != pipe.Id &&
                       (element is Pipe || element is Duct ||
                        element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming);
            }).ToList();
        }

        /// <summary>
        /// 合并相邻的碰撞区段
        /// </summary>
        private void MergeAdjacentSections(Pipe pipe, List<CollisionSection> sections)
        {
            var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            var mergeDistance = diameter * 3;

            for (int i = sections.Count - 2; i >= 0; i--)
            {
                var gap = sections[i].EndPoint - sections[i + 1].StartPoint;
                if (gap.GetLength() < mergeDistance)
                {
                    foreach (var refs in sections[i + 1].References)
                        sections[i].References.ToList().Add(refs);
                    sections.RemoveAt(i + 1);
                }
            }
        }

        /// <summary>
        /// 为碰撞区段创建绕行路径
        /// </summary>
        private void CreateBypassForSection(Pipe pipe, CollisionSection section)
        {
            var offsetLine = FindBypassRoute(pipe, section);
            if (offsetLine == null) return;

            var startPoint = section.StartPoint;
            var endPoint = section.EndPoint;
            var startOffset = offsetLine.GetEndPoint(0);
            var endOffset = offsetLine.GetEndPoint(1);

            var levelId = pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
            var pipeType = pipe.PipeType;

            // 创建U形绕行管段
            var sidePipe1 = Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, startPoint, startOffset);
            var bypassPipe = Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, startOffset, endOffset);
            var sidePipe2 = Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, endOffset, endPoint);

            // 复制参数
            CopyPipeParameters(pipe, sidePipe1);
            CopyPipeParameters(pipe, bypassPipe);
            CopyPipeParameters(pipe, sidePipe2);

            // 添加到区段
            section.AddBypassPipe(sidePipe1);
            section.AddBypassPipe(bypassPipe);
            section.AddBypassPipe(sidePipe2);

            // 创建弯头连接
            CreateElbow(sidePipe1, bypassPipe, startOffset);
            CreateElbow(bypassPipe, sidePipe2, endOffset);
        }

        /// <summary>
        /// 查找绕行路径
        /// </summary>
        private Line FindBypassRoute(Pipe pipe, CollisionSection section)
        {
            var diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            var minOffset = diameter * 2;
            var step = diameter;

            // 计算绕行方向
            var directions = GetBypassDirections(pipe, section);

            var currentOffset = minOffset;
            while (currentOffset <= MaxSearchDistance)
            {
                foreach (var dir in directions)
                {
                    var offset = dir * currentOffset;
                    var bypassLine = Line.CreateBound(section.StartPoint + offset, section.EndPoint + offset);

                    var collisions = _detector.DetectAlongLine(bypassLine);
                    var filtered = FilterCollisions(pipe, collisions);

                    if (!filtered.Any())
                        return bypassLine;
                }
                currentOffset += step;
            }

            return null;
        }

        /// <summary>
        /// 获取绕行方向列表
        /// </summary>
        private List<XYZ> GetBypassDirections(Pipe pipe, CollisionSection section)
        {
            var directions = new List<XYZ>();

            // 从碰撞构件中提取垂直方向
            foreach (var reference in section.References)
            {
                var element = _document.GetElement(reference.GetReference());
                var locationCurve = (element.Location as LocationCurve)?.Curve as Line;
                if (locationCurve == null) continue;

                var elementDir = (locationCurve.GetEndPoint(1) - locationCurve.GetEndPoint(0)).Normalize();
                if (Math.Abs(elementDir.DotProduct(section.PipeDirection)) > 0.99)
                    continue;

                var crossDir = elementDir.CrossProduct(section.PipeDirection).Normalize();
                directions.Add(crossDir);
                directions.Add(-crossDir);
                break;
            }

            // 如果没有找到垂直方向，使用四个基本方向
            if (!directions.Any())
            {
                var perpDirs = GetPerpendicularDirections(section.PipeDirection, 4);
                directions.AddRange(perpDirs);
            }

            return directions.Distinct().ToList();
        }

        /// <summary>
        /// 获取垂直于指定方向的方向
        /// </summary>
        private List<XYZ> GetPerpendicularDirections(XYZ direction, int count)
        {
            var results = new List<XYZ>();
            var plane = Plane.CreateByNormalAndOrigin(direction, XYZ.Zero);
            var arc = Arc.Create(plane, 1.0, 0, 2 * Math.PI);

            for (int i = 1; i <= count; i++)
            {
                results.Add(arc.Evaluate(i / (double)count, true));
            }

            return results;
        }

        /// <summary>
        /// 创建弯头连接
        /// </summary>
        private void CreateElbow(Pipe pipe1, Pipe pipe2, XYZ connectionPoint)
        {
            var conn1 = FindConnectorAtPoint(pipe1, connectionPoint);
            var conn2 = FindConnectorAtPoint(pipe2, connectionPoint);

            if (conn1 != null && conn2 != null)
                _document.Create.NewElbowFitting(conn1, conn2);
        }

        /// <summary>
        /// 连接相邻区段
        /// </summary>
        private void ConnectSections(Pipe originalPipe, List<CollisionSection> sections)
        {
            var levelId = originalPipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
            var pipeType = originalPipe.PipeType;

            for (int i = 1; i < sections.Count; i++)
            {
                var prevEnd = sections[i - 1].EndPoint;
                var currStart = sections[i].StartPoint;

                var connectorPipe = Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, prevEnd, currStart);
                CopyPipeParameters(originalPipe, connectorPipe);

                CreateElbow(sections[i - 1].BypassPipes.Last(), connectorPipe, prevEnd);
                CreateElbow(connectorPipe, sections[i].BypassPipes.First(), currStart);
            }
        }

        /// <summary>
        /// 连接管道两端
        /// </summary>
        private void ConnectEnds(Pipe originalPipe, List<CollisionSection> sections, Line centerLine)
        {
            var levelId = originalPipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
            var pipeType = originalPipe.PipeType;

            var startPoint = centerLine.GetEndPoint(0);
            var endPoint = centerLine.GetEndPoint(1);
            var firstSectionStart = sections.First().StartPoint;
            var lastSectionEnd = sections.Last().EndPoint;

            // 连接起点
            var startConnector = FindConnectedConnector(originalPipe, startPoint);
            var startPipe = startConnector != null
                ? Pipe.Create(_document, pipeType.Id, levelId, startConnector, firstSectionStart)
                : Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, startPoint, firstSectionStart);

            CopyPipeParameters(originalPipe, startPipe);
            CreateElbow(startPipe, sections.First().BypassPipes.First(), firstSectionStart);

            // 连接终点
            var endConnector = FindConnectedConnector(originalPipe, endPoint);
            var endPipe = endConnector != null
                ? Pipe.Create(_document, pipeType.Id, levelId, endConnector, lastSectionEnd)
                : Pipe.Create(_document, _pipingSystemType.Id, pipeType.Id, levelId, lastSectionEnd, endPoint);

            CopyPipeParameters(originalPipe, endPipe);
            CreateElbow(sections.Last().BypassPipes.Last(), endPipe, lastSectionEnd);
        }

        /// <summary>
        /// 查找管道上指定点的连接器
        /// </summary>
        private Connector FindConnectorAtPoint(Pipe pipe, XYZ point)
        {
            return pipe.ConnectorManager.Connectors
                .Cast<Connector>()
                .FirstOrDefault(c => c.Origin.IsAlmostEqualTo(point));
        }

        /// <summary>
        /// 查找管道端点连接的外部连接器
        /// </summary>
        private Connector FindConnectedConnector(Pipe pipe, XYZ point)
        {
            var selfConnector = FindConnectorAtPoint(pipe, point);
            return selfConnector?.AllRefs
                .Cast<Connector>()
                .FirstOrDefault(c => c.Owner.Id != pipe.Id && c.ConnectorType == ConnectorType.End);
        }

        /// <summary>
        /// 复制管道参数
        /// </summary>
        private void CopyPipeParameters(Pipe source, Pipe target)
        {
            var diameterParam = source.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (diameterParam != null && diameterParam.HasValue)
            {
                target.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(diameterParam.AsDouble());
            }
        }
    }
    /// <summary>
    /// 碰撞区段模型
    /// 表示管道上一个连续的碰撞区域
    /// </summary>
    public class CollisionSection
    {
        private readonly XYZ _direction;
        private double _startOffset;
        private double _endOffset;
        private readonly List<ReferenceWithContext> _references;
        private readonly List<Pipe> _bypassPipes;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="direction">管道方向</param>
        private CollisionSection(XYZ direction)
        {
            _direction = direction.Normalize();
            _startOffset = 0;
            _endOffset = 0;
            _references = new List<ReferenceWithContext>();
            _bypassPipes = new List<Pipe>();
        }

        /// <summary>
        /// 管道方向
        /// </summary>
        public XYZ PipeDirection => _direction;

        /// <summary>
        /// 绕行管道列表（U形管的三段）
        /// </summary>
        public IReadOnlyList<Pipe> BypassPipes => _bypassPipes;

        /// <summary>
        /// 区段起点
        /// </summary>
        public XYZ StartPoint => _references.First().GetReference().GlobalPoint + _direction * _startOffset;

        /// <summary>
        /// 区段终点
        /// </summary>
        public XYZ EndPoint => _references.Last().GetReference().GlobalPoint + _direction * _endOffset;

        /// <summary>
        /// 碰撞参考列表
        /// </summary>
        public IReadOnlyList<ReferenceWithContext> References => _references;

        /// <summary>
        /// 添加绕行管道
        /// </summary>
        public void AddBypassPipe(Pipe pipe) => _bypassPipes.Add(pipe);

        /// <summary>
        /// 扩展区段范围
        /// </summary>
        /// <param name="isStart">是否扩展起点</param>
        /// <param name="value">扩展值</param>
        public void Inflate(bool isStart, double value)
        {
            if (isStart)
                _startOffset -= value;
            else
                _endOffset += value;
        }

        /// <summary>
        /// 从碰撞参考构建区段列表
        /// </summary>
        public static List<CollisionSection> BuildSections(IList<ReferenceWithContext> references, XYZ direction)
        {
            var sections = new List<CollisionSection>();
            var activeStack = new List<ReferenceWithContext>();
            var normalizedDir = direction.Normalize();

            foreach (var reference in references.OrderBy(r => r.Proximity))
            {
                if (activeStack.Count == 0)
                {
                    sections.Add(new CollisionSection(normalizedDir));
                }

                var currentSection = sections.Last();
                currentSection._references.Add(reference);

                var existing = FindReference(activeStack, reference);
                if (existing != null)
                    activeStack.Remove(existing);
                else
                    activeStack.Add(reference);
            }

            return sections;
        }

        /// <summary>
        /// 在列表中查找相同元素的参考
        /// </summary>
        private static ReferenceWithContext FindReference(List<ReferenceWithContext> list, ReferenceWithContext target)
        {
            return list.FirstOrDefault(r => r.GetReference().ElementId == target.GetReference().ElementId);
        }
    }

    /// <summary>
    /// 碰撞检测服务类
    /// 负责检测管道与周围构件的碰撞
    /// </summary>
    public class CollisionDetector
    {
        private readonly Document _document;
        private readonly View3D _view3D;
        private const double Tolerance = 1e-9;

        /// <summary>
        /// 构造函数，初始化检测器
        /// </summary>
        /// <param name="document">Revit文档</param>
        public CollisionDetector(Document document)
        {
            _document = document;
            _view3D = GetFirstNonTemplate3DView();
        }

        /// <summary>
        /// 获取第一个非模板3D视图
        /// </summary>
        private View3D GetFirstNonTemplate3DView()
        {
            var collector = new FilteredElementCollector(_document);
            return collector.OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => !v.IsTemplate);
        }

        /// <summary>
        /// 检测射线方向的碰撞
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="direction">射线方向</param>
        /// <returns>碰撞结果列表</returns>
        public List<ReferenceWithContext> DetectAlongRay(XYZ origin, XYZ direction)
        {
            var intersector = new ReferenceIntersector(_view3D)
            {
                TargetType = FindReferenceTarget.Face
            };

            var results = intersector.Find(origin, direction);
            return results.Distinct(new ReferenceWithContextEqualityComparer())
                          .OrderBy(r => r.Proximity)
                          .ToList();
        }

        /// <summary>
        /// 检测线段上的碰撞
        /// </summary>
        /// <param name="line">线段</param>
        /// <returns>碰撞结果列表</returns>
        public List<ReferenceWithContext> DetectAlongLine(Line line)
        {
            var startPoint = line.GetEndPoint(0);
            var endPoint = line.GetEndPoint(1);
            var direction = (endPoint - startPoint).Normalize();

            var intersector = new ReferenceIntersector(_view3D)
            {
                TargetType = FindReferenceTarget.Face
            };

            var allResults = intersector.Find(startPoint, direction);

            return allResults.Where(r => Math.Abs(line.Distance(r.GetReference().GlobalPoint)) < Tolerance)
                             .Distinct(new ReferenceWithContextEqualityComparer())
                             .OrderBy(r => r.Proximity)
                             .ToList();
        }

        /// <summary>
        /// 参考点相等比较器
        /// </summary>
        //private class ReferenceWithContextEqualityComparer : IEqualityComparer<ReferenceWithContext>
        //{
        //    public bool Equals(ReferenceWithContext x, ReferenceWithContext y)
        //    {
        //        if (ReferenceEquals(x, y)) return true;
        //        if (x is null || y is null) return false;

        //        return Math.Abs(x.Proximity - y.Proximity) < Tolerance &&
        //               x.GetReference().ElementId == y.GetReference().ElementId;
        //    }
        //    //HashCode.Combine 是.NET Core 2.1+ / .NET Standard 2.1+ 的特性，在.NET Framework（Revit 使用的版本）中不可用。
        //    public int GetHashCode(ReferenceWithContext obj)
        //    {
        //        return HashCode.Combine(obj.GetReference().ElementId.GetHashCode(),
        //                                obj.Proximity.GetHashCode());
        //    }
        //}
        private class ReferenceWithContextEqualityComparer : IEqualityComparer<ReferenceWithContext>
        {
            public const double Tolerance = 1e-6;
            public bool Equals(ReferenceWithContext x, ReferenceWithContext y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                return Math.Abs(x.Proximity - y.Proximity) < Tolerance &&
                       x.GetReference()?.ElementId == y.GetReference()?.ElementId;
            }
            public int GetHashCode(ReferenceWithContext obj)
            {
                if (obj?.GetReference() == null) return 0;
                // 仅使用 ElementId，确保哈希稳定
                // Proximity 差异在 Equals 中处理
                return obj.GetReference().ElementId.GetHashCode();
            }
        }
    }
}
