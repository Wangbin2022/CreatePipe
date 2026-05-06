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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ReferencePlaneMgrView.xaml 的交互逻辑
    /// </summary>
    public partial class ReferencePlaneMgrView : Window
    {
        public ReferencePlaneMgrView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 参考平面项ViewModel - 用于列表显示
    /// </summary>
    public class ReferencePlaneItem : INotifyPropertyChanged
    {
        private readonly ReferencePlane _refPlane;

        public int Id => _refPlane.Id.IntegerValue;
        public string BubbleEnd => FormatPoint(_refPlane.BubbleEnd);
        public string FreeEnd => FormatPoint(_refPlane.FreeEnd);
        public string Normal => FormatPoint(_refPlane.Normal);

        public ReferencePlaneItem(ReferencePlane refPlane) => _refPlane = refPlane;

        private static string FormatPoint(XYZ point) =>
            $"({Math.Round(point.X, 2)}, {Math.Round(point.Y, 2)}, {Math.Round(point.Z, 2)})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 主窗口ViewModel - 管理参考平面列表和创建操作
    /// </summary>
    public class ReferencePlaneMgrViewModel : ObserverableObject
    {
        private readonly ReferencePlaneManager _manager;
        private ObservableCollection<ReferencePlaneItem> _referencePlanes;
        private bool _isProcessing;

        public ObservableCollection<ReferencePlaneItem> ReferencePlanes
        {
            get => _referencePlanes;
            set { _referencePlanes = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand CreateCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }

        public ReferencePlaneMgrViewModel(UIDocument uidoc)
        {
            _manager = new ReferencePlaneManager(uidoc);
            RefreshReferencePlanes();

            CreateCommand = new BaseBindingCommand(_ => ExecuteCreate(), _ => CanExecute);
            RefreshCommand = new BaseBindingCommand(_ => RefreshReferencePlanes(), _ => CanExecute);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void RefreshReferencePlanes()
        {
            var planes = _manager.GetAllReferencePlanes();
            ReferencePlanes = new ObservableCollection<ReferencePlaneItem>(
                planes.Select(p => new ReferencePlaneItem(p)));
        }

        private void ExecuteCreate()
        {
            IsProcessing = true;
            try
            {
                using (var transaction = new Transaction(_manager.Document, "创建参考平面"))
                {
                    transaction.Start();
                    _manager.CreateReferencePlanes();
                    transaction.Commit();
                }
                RefreshReferencePlanes();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"创建参考平面失败：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public Action CloseWindow { get; set; }
 
    }


    /// <summary>
    /// 参考平面管理器 - 处理墙/楼板参考平面的创建
    /// </summary>
    public class ReferencePlaneManager
    {
        private readonly UIDocument _uidoc;
        private readonly Options _geometryOptions;

        public Document Document => _uidoc.Document;

        public ReferencePlaneManager(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _geometryOptions = new Options
            {
                ComputeReferences = true,
                View = uidoc.Document.ActiveView
            };
        }

        /// <summary>
        /// 获取文档中所有参考平面
        /// </summary>
        public IList<ReferencePlane> GetAllReferencePlanes() =>
            new FilteredElementCollector(Document)
                .OfClass(typeof(ReferencePlane))
                .Cast<ReferencePlane>()
                .ToList();

        /// <summary>
        /// 为选中的元素创建参考平面
        /// </summary>
        public void CreateReferencePlanes()
        {
            foreach (ElementId id in _uidoc.Selection.GetElementIds())
            {
                var element = Document.GetElement(id);
                if (element == null) continue;

                switch (element)
                {
                    case Wall wall:
                        CreateWallReferencePlane(wall);
                        break;
                    case Floor floor:
                        CreateFloorReferencePlane(floor);
                        break;
                }
            }
        }

        /// <summary>
        /// 为墙创建参考平面 - 沿墙外侧偏移半墙厚
        /// </summary>
        private void CreateWallReferencePlane(Wall wall)
        {
            var curve = (wall.Location as LocationCurve)?.Curve;

            if (curve is Autodesk.Revit.DB.Line line)
            {
                var (bubbleEnd, freeEnd) = CalculateWallOffsetPoints(line, wall.Width);
                var cutVec = new XYZ(0, 0, 1);
                Document.Create.NewReferencePlane(bubbleEnd, freeEnd, cutVec, Document.ActiveView);
            }
        }

        /// <summary>
        /// 计算墙外侧偏移点 - 使用元组返回值
        /// </summary>
        private (XYZ bubbleEnd, XYZ freeEnd) CalculateWallOffsetPoints(Autodesk.Revit.DB.Line line, double wallWidth)
        {
            var start = line.GetEndPoint(0);
            var end = line.GetEndPoint(1);

            double halfThickness = wallWidth / 2;
            double length = GeometryHelper.GetLength(start, end);
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            // 计算偏移量
            double xOffset = dy * halfThickness / length;
            double yOffset = dx * halfThickness / length;

            // 根据线段方向调整偏移方向
            (xOffset, yOffset) = AdjustOffsetDirection(start, end, xOffset, yOffset);

            return (
                new XYZ(start.X + xOffset, start.Y + yOffset, start.Z),
                new XYZ(end.X + xOffset, end.Y + yOffset, end.Z)
            );
        }

        /// <summary>
        /// 根据线段方向调整偏移方向 - 使用元组
        /// </summary>
        private (double xOffset, double yOffset) AdjustOffsetDirection(
            XYZ start, XYZ end, double xOffset, double yOffset)
        {
            bool startLessEnd = start.X < end.X && start.Y < end.Y;
            bool startGreaterEnd = start.X > end.X && start.Y > end.Y;
            bool startXGreaterYLess = start.X > end.X && start.Y < end.Y;

            if (startLessEnd)
            {
                return (-xOffset, yOffset);
            }
            else if (startGreaterEnd)
            {
                return (xOffset, -yOffset);
            }
            else if (startXGreaterYLess)
            {
                return (-xOffset, -yOffset);
            }
            else
            {
                return (xOffset, yOffset);
            }
        }

        /// <summary>
        /// 为楼板创建参考平面 - 在底面取三点
        /// </summary>
        private void CreateFloorReferencePlane(Floor floor)
        {
            var geometry = floor.get_Geometry(_geometryOptions);
            Autodesk.Revit.DB.Face bottomFace = FindBottomFace(geometry);

            if (bottomFace != null)
            {
                var mesh = bottomFace.Triangulate();
                var (p1, p2, p3) = GeometryHelper.GetThreePoints(mesh);
                Document.Create.NewReferencePlane2(p1, p2, p3, Document.ActiveView);
            }
        }

        /// <summary>
        /// 查找几何体中的底面 - 使用LINQ
        /// </summary>
        private Autodesk.Revit.DB.Face FindBottomFace(GeometryElement geometry)
        {
            var solids = geometry.OfType<Solid>().Where(s => s != null && s.Volume > 0);

            foreach (var solid in solids)
            {
                List<Autodesk.Revit.DB.Face> faces = solid.Faces.Cast<Autodesk.Revit.DB.Face>().ToList();
                var bottomFace = GeometryHelper.GetBottomFace(faces);
                if (bottomFace != null) return bottomFace;
            }
            return null;
        }
    }
    /// <summary>
    /// 几何辅助类 - 提供面判断、点分布、距离计算等
    /// </summary>
    public static partial class GeometryHelper
    {
        private const double Precision = 0.0001;

        /// <summary>
        /// 获取底面 - 平均高程最低且非垂直的面
        /// </summary>
        public static Autodesk.Revit.DB.Face GetBottomFace(IList<Autodesk.Revit.DB.Face> faces)
        {
            Autodesk.Revit.DB.Face bottomFace = null;
            double minElevation = double.MaxValue;

            foreach (Autodesk.Revit.DB.Face face in faces)
            {
                if (IsVerticalFace(face)) continue;

                var mesh = face.Triangulate();
                if (mesh == null) continue;

                var avgElevation = mesh.Vertices.Cast<XYZ>().Average(v => v.Z);

                if (avgElevation < minElevation)
                {
                    minElevation = avgElevation;
                    bottomFace = face;
                }
            }
            return bottomFace;
        }

        /// <summary>
        /// 获取网格上的三个点（起点、1/3点、2/3点）
        /// </summary>
        public static (XYZ p1, XYZ p2, XYZ p3) GetThreePoints(Mesh mesh)
        {
            var vertices = mesh.Vertices.ToList();
            int count = vertices.Count;

            return (
                vertices[0],
                vertices[count / 3],
                vertices[count / 3 * 2]
            );
        }

        /// <summary>
        /// 判断面是否垂直 - 包含垂直边
        /// </summary>
        private static bool IsVerticalFace(Autodesk.Revit.DB.Face face)
        {
            return face.EdgeLoops.Cast<EdgeArray>()
                .SelectMany(ea => ea.Cast<Edge>())
                .Any(IsVerticalEdge);
        }

        /// <summary>
        /// 判断边是否垂直 - 方向接近Z轴
        /// </summary>
        private static bool IsVerticalEdge(Edge edge)
        {
            var points = edge.Tessellate()?.ToList();
            if (points?.Count < 2) return false;

            for (int i = 1; i < points.Count; i++)
            {
                var vector = points[i] - points[i - 1];
                if (IsVectorVertical(vector)) return true;
            }
            return false;
        }

        /// <summary>
        /// 判断向量是否垂直（X、Y分量接近0）
        /// </summary>
        private static bool IsVectorVertical(XYZ vector) =>
            Math.Abs(vector.X) < Precision && Math.Abs(vector.Y) < Precision;

        /// <summary>
        /// 计算两点间距离 - 使用表达式体
        /// </summary>
        public static double GetLength(XYZ start, XYZ end) =>
            Math.Sqrt(Math.Pow(end.X - start.X, 2) +
                      Math.Pow(end.Y - start.Y, 2) +
                      Math.Pow(end.Z - start.Z, 2));
    }
}
