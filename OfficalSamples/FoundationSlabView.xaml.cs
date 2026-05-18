using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
//using Color = Autodesk.Revit.DB.Color;
//using Color = System.Windows.Media.Color;
using Color = System.Drawing.Color;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FoundationSlabView.xaml 的交互逻辑
    /// </summary>
    public partial class FoundationSlabView : Window
    {
        public FoundationSlabView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new FoundationSlabViewModel(uIApplication);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    public class FoundationSlabViewModel : ObserverableObject
    {
        private readonly SlabDataService _dataService;
        private ObservableCollection<RegularSlabModel> _slabs;
        private FloorType _selectedFoundationType;
        private BitmapSource _previewImage;
        private RectangleF _previewBounds;
        private bool _isProcessing;

        public FoundationSlabViewModel(UIApplication uiApp)
        {
            _dataService = new SlabDataService(uiApp);

            // 初始化命令
            CreateCommand = new BaseBindingCommand(ExecuteCreate);
            SelectAllCommand = new BaseBindingCommand(_ => SetAllSelected(true));
            ClearAllCommand = new BaseBindingCommand(_ => SetAllSelected(false));
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);

            // 加载数据
            ExecuteRefresh(null);
        }

        #region 属性

        public ObservableCollection<RegularSlabModel> Slabs
        {
            get => _slabs;
            set
            {
                _slabs = value;
                OnPropertyChanged(nameof(Slabs));
                GeneratePreview(null);
            }
        }

        public ObservableCollection<FloorType> FoundationTypes { get; set; }

        public FloorType SelectedFoundationType
        {
            get => _selectedFoundationType;
            set
            {
                _selectedFoundationType = value;
                OnPropertyChanged(nameof(SelectedFoundationType));
                (CreateCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
            }
        }

        public BitmapSource PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged(nameof(PreviewImage));
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
                (CreateCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int SelectedCount => Slabs?.Count(s => s.IsSelected) ?? 0;

        #endregion

        #region 命令

        public ICommand CreateCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 命令执行

        private void ExecuteRefresh(Object obj)
        {
            try
            {
                IsProcessing = true;

                Slabs = _dataService.GetBaseSlabs();
                FoundationTypes = new ObservableCollection<FloorType>(_dataService.GetFoundationSlabTypes());

                if (FoundationTypes.Any())
                    SelectedFoundationType = FoundationTypes.First();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"加载数据失败: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteCreate() =>
            !IsProcessing && SelectedFoundationType != null && SelectedCount > 0;

        private void ExecuteCreate(Object obj)
        {
            try
            {
                IsProcessing = true;

                var selectedSlabs = Slabs.Where(s => s.IsSelected).ToList();
                var success = _dataService.CreateFoundationSlabs(
                    new ObservableCollection<RegularSlabModel>(selectedSlabs),
                    SelectedFoundationType);

                if (success)
                {
                    TaskDialog.Show("成功", $"成功创建 {selectedSlabs.Count} 个基础筏板");
                    ExecuteRefresh(null); // 刷新数据
                }
                else
                {
                    TaskDialog.Show("失败", "基础筏板创建失败");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"创建失败: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void SetAllSelected(bool value)
        {
            if (Slabs == null) return;
            foreach (var slab in Slabs)
                slab.IsSelected = value;

            OnPropertyChanged(nameof(SelectedCount));
            (CreateCommand as BaseBindingCommand)?.RaiseCanExecuteChanged();
            GeneratePreview(null);
        }

        #endregion

        #region 预览生成

        private void GeneratePreview(Object obj)
        {
            if (Slabs == null || Slabs.Count == 0) return;

            const int width = 400;
            const int height = 400;

            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                _previewBounds = new RectangleF(0, 0, width, height);
                GeometryDrawingService.DrawProfiles(graphics, _previewBounds, Slabs);
                PreviewImage = ConvertBitmapToSource(bitmap);
            }
        }

        private static BitmapSource ConvertBitmapToSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }
    /// <summary>
    /// 楼板数据服务 - 负责从 Revit 获取和处理楼板数据
    /// </summary>
    public class SlabDataService
    {
        private const double PlanarPrecision = 0.00033;
        private readonly UIApplication _uiApp;
        private readonly Document _document;

        private SortedList<double, Level> _levels;
        private List<View> _views;
        private List<Floor> _floors;

        public SlabDataService(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _document = uiApp.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取所有基础楼板类型
        /// </summary>
        public List<FloorType> GetFoundationSlabTypes()
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .Where(t => t.Category?.Name == "Structural Foundations")
                .ToList();
        }

        /// <summary>
        /// 获取所有底层楼板数据
        /// </summary>
        public ObservableCollection<RegularSlabModel> GetBaseSlabs()
        {
            FindElements();

            if (_levels.Count == 0)
                return new ObservableCollection<RegularSlabModel>();

            // 获取最低标高的视图
            var lowestLevel = _levels.Values[0];
            var baseView = _views.FirstOrDefault(v => v.Name == lowestLevel.Name);
            if (baseView == null) return new ObservableCollection<RegularSlabModel>();

            var slabs = new ObservableCollection<RegularSlabModel>();

            foreach (var floor in _floors)
            {
                if (floor.LevelId != lowestLevel.Id) continue;

                var bbox = floor.get_BoundingBox(baseView);
                if (!IsPlanarFloor(bbox, floor)) continue;

                var profile = GetFloorProfile(floor);
                var octagonProfile = GeometryDrawingService.CreateOctagonProfile(bbox);

                slabs.Add(new RegularSlabModel
                {
                    Id = floor.Id,
                    Mark = GetMarkParameter(floor),
                    LevelName = lowestLevel.Name,
                    SlabTypeName = GetFloorTypeName(floor),
                    Profile = profile,
                    OctagonalProfile = octagonProfile,
                    BoundingBox = bbox,
                    IsSelected = true // 默认选中
                });
            }

            return slabs;
        }

        /// <summary>
        /// 创建基础筏板
        /// </summary>
        public bool CreateFoundationSlabs(ObservableCollection<RegularSlabModel> selectedSlabs,
            FloorType foundationType)
        {
            if (selectedSlabs.Count == 0 || foundationType == null) return false;

            var lowestLevel = _levels.Values[0];

            foreach (var slab in selectedSlabs)
            {
                using (var transaction = new Transaction(_document, "创建基础筏板"))
                {
                    transaction.Start();

                    // 创建基础筏板
                    var normal = new XYZ(0, 0, 1);
                    var foundationSlab = _document.Create.NewFoundationSlab(
                        slab.OctagonalProfile, foundationType, lowestLevel, true, normal);

                    if (foundationSlab == null)
                    {
                        transaction.RollBack();
                        return false;
                    }

                    // 删除原楼板
                    _document.Delete(slab.Id);

                    transaction.Commit();
                }
            }

            return true;
        }

        private void FindElements()
        {
            _levels = new SortedList<double, Level>();
            _views = new List<View>();
            _floors = new List<Floor>();

            var collector = new FilteredElementCollector(_document);

            foreach (var element in collector)
            {
                switch (element)
                {
                    case Level level:
                        _levels.Add(level.Elevation, level);
                        break;
                    case View view when !view.IsTemplate:
                        _views.Add(view);
                        break;
                    case Floor floor:
                        _floors.Add(floor);
                        break;
                }
            }
        }

        private bool IsPlanarFloor(BoundingBoxXYZ bbox, Floor floor)
        {
            var floorType = _document.GetElement(floor.GetTypeId()) as FloorType;
            var thicknessParam = floorType?.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
            var floorThickness = thicknessParam?.AsDouble() ?? 0;
            var boundThickness = Math.Abs(bbox.Max.Z - bbox.Min.Z);

            return Math.Abs(boundThickness - floorThickness) < PlanarPrecision;
        }

        private string GetMarkParameter(Floor floor)
        {
            var param = floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            return param?.AsString() ?? string.Empty;
        }

        private string GetFloorTypeName(Floor floor)
        {
            var floorType = _document.GetElement(floor.GetTypeId()) as FloorType;
            return floorType?.Name ?? "未知类型";
        }

        private CurveArray GetFloorProfile(Floor floor)
        {
            var profile = new CurveArray();

            // 尝试从分析模型获取轮廓
            if (floor.GetAnalyticalModel() is AnalyticalModel analyticalModel)
            {
                var curves = analyticalModel.GetCurves(AnalyticalCurveType.ActiveCurves);
                foreach (var curve in curves)
                    profile.Append(curve);
                return profile;
            }

            // 从几何体获取轮廓
            var options = _uiApp.Application.Create.NewGeometryOptions();
            var geometry = floor.get_Geometry(options);

            foreach (GeometryObject geoObj in geometry)
            {
                if (geoObj is Solid solid)
                {
                    for (int i = 0; i < solid.Edges.Size / 3; i++)
                    {
                        var edge = solid.Edges.get_Item(i);
                        var points = edge.Tessellate() as List<XYZ>;

                        if (points != null)
                        {
                            for (int j = 0; j < points.Count - 1; j++)
                            {
                                profile.Append(Autodesk.Revit.DB.Line.CreateBound(points[j], points[j + 1]));
                            }
                        }
                    }
                }
            }

            return profile;
        }
    }
    /// <summary>
    /// 几何图形绘制服务
    /// </summary>
    public static class GeometryDrawingService
    {
        /// <summary>
        /// 绘制楼板轮廓
        /// </summary>
        public static void DrawProfiles(Graphics graphics, RectangleF clipRect,
            IEnumerable<RegularSlabModel> slabs)
        {
            var maxBounds = GetMaxBoundingBox(slabs);
            var transform = GetTransformMatrix(clipRect, maxBounds);

            if (transform == null) return;

            graphics.Clear(Color.Black);
            graphics.Transform = transform;
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            var profilePen = new Pen(Color.Yellow, 0.05f);
            var selectedPen = new Pen(Color.Green, 0.2f);

            foreach (var slab in slabs)
            {
                if (slab.Profile != null)
                    DrawCurveArray(profilePen, graphics, slab.Profile);

                if (slab.IsSelected && slab.OctagonalProfile != null)
                    DrawCurveArray(selectedPen, graphics, slab.OctagonalProfile);
            }
        }

        /// <summary>
        /// 创建八角形轮廓
        /// </summary>
        public static CurveArray CreateOctagonProfile(BoundingBoxXYZ bbox)
        {
            var min = bbox.Min;
            var max = bbox.Max;
            var z = max.Z;

            var xOffset = Math.Abs(max.Y - min.Y) / 8;
            var yOffset = Math.Abs(max.X - min.X) / 8;
            var centerX = (min.X + max.X) / 2;
            var centerY = (min.Y + max.Y) / 2;

            var points = new[]
            {
                new XYZ(min.X, min.Y, z),
                new XYZ(centerX, min.Y - yOffset, z),
                new XYZ(max.X, min.Y, z),
                new XYZ(max.X + xOffset, centerY, z),
                new XYZ(max.X, max.Y, z),
                new XYZ(centerX, max.Y + yOffset, z),
                new XYZ(min.X, max.Y, z),
                new XYZ(min.X - xOffset, centerY, z)
            };

            var curveArray = new CurveArray();
            for (int i = 0; i < 8; i++)
            {
                var start = points[i];
                var end = points[(i + 1) % 8];
                curveArray.Append(Line.CreateBound(start, end));
            }

            return curveArray;
        }

        private static void DrawCurveArray(Pen pen, Graphics graphics, CurveArray curves)
        {
            foreach (Curve curve in curves)
            {
                if (curve is Line line)
                {
                    DrawLine(graphics, pen, line);
                }
                else
                {
                    var points = curve.Tessellate() as List<XYZ>;
                    if (points != null)
                    {
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            DrawLine(graphics, pen,
                                new PointF((float)points[i].X, (float)points[i].Y),
                                new PointF((float)points[i + 1].X, (float)points[i + 1].Y));
                        }
                    }
                }
            }
        }

        private static void DrawLine(Graphics graphics, Pen pen, Line line) =>
            DrawLine(graphics, pen,
                GetPointF(line.GetEndPoint(0)),
                GetPointF(line.GetEndPoint(1)));

        private static void DrawLine(Graphics graphics, Pen pen, PointF start, PointF end) =>
            graphics.DrawLine(pen, start, end);

        private static PointF GetPointF(XYZ point) => new PointF((float)point.X, (float)point.Y);

        private static RectangleF GetMaxBoundingBox(IEnumerable<RegularSlabModel> slabs)
        {
            RectangleF union = RectangleF.Empty;
            var first = true;

            foreach (var slab in slabs)
            {
                var rect = new RectangleF(
                    (float)slab.BoundingBox.Min.X,
                    (float)slab.BoundingBox.Min.Y,
                    (float)(slab.BoundingBox.Max.X - slab.BoundingBox.Min.X),
                    (float)(slab.BoundingBox.Max.Y - slab.BoundingBox.Min.Y));

                union = first ? rect : RectangleF.Union(union, rect);
                first = false;
            }

            return union;
        }

        private static Matrix GetTransformMatrix(RectangleF target, RectangleF source)
        {
            const float shrink = 0.15f;
            const float shrinked = 1.0f - 2 * shrink;

            var drawRect = target;

            if (source.Width * target.Height > source.Height * target.Width)
                drawRect.Inflate(-target.Width * shrink,
                    (target.Width * shrinked * source.Height / source.Width - target.Height) / 2);
            else
                drawRect.Inflate(
                    (target.Height * shrinked * source.Width / source.Height - target.Width) / 2,
                    -target.Height * shrink);

            var points = new[]
            {
                new PointF(drawRect.Left, drawRect.Bottom),
                new PointF(drawRect.Right, drawRect.Bottom),
                new PointF(drawRect.Left, drawRect.Top)
            };

            return new Matrix(source, points);
        }
    }
    /// <summary>
    /// 楼板数据模型（符合 MVVM 模式）
    /// </summary>
    public class RegularSlabModel : ObserverableObject
    {
        private bool _isSelected;

        public ElementId Id { get; set; }
        public string Mark { get; set; }
        public string LevelName { get; set; }
        public string SlabTypeName { get; set; }
        public CurveArray Profile { get; set; }
        public CurveArray OctagonalProfile { get; set; }
        public BoundingBoxXYZ BoundingBox { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}
