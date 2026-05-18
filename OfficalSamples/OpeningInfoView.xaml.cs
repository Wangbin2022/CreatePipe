using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// OpeningInfoView.xaml 的交互逻辑
    /// </summary>
    public partial class OpeningInfoView : Window
    {
        private readonly OpeningViewModel _viewModel;
        public OpeningInfoView(OpeningViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _viewModel.CloseWindow = Close;
            DataContext = _viewModel;

            // 绑定预览更新
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OpeningViewModel.SelectedOpening))
                {
                    UpdatePreview();
                }
            };
            if (_viewModel.Openings.Count > 0)
                _viewModel.SelectedOpening = _viewModel.Openings[0];
        }
        private void UpdatePreview()
        {
            if (_viewModel.SelectedOpening?.PreviewVisual != null)
            {
                var drawingImage = new DrawingImage(_viewModel.SelectedOpening.PreviewVisual.Drawing);
                PreviewImage.Source = drawingImage;
            }
        }
    }
    /// <summary>
    /// 主窗体ViewModel - 管理洞口列表和创建操作
    /// </summary>
    public class OpeningViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly Document _doc;
        private OpeningInfoViewModel _selectedOpening;

        public ObservableCollection<OpeningInfoViewModel> Openings { get; } = new ObservableCollection<OpeningInfoViewModel>();

        public OpeningInfoViewModel SelectedOpening
        {
            get => _selectedOpening;
            set { _selectedOpening = value; OnPropertyChanged(); }
        }

        public ICommand CreateModelLinesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public OpeningViewModel(UIApplication uiApp, IEnumerable<OpeningInfo> openingInfos)
        {
            _uiApp = uiApp;
            _doc = uiApp.ActiveUIDocument.Document;

            // 加载洞口列表 - 使用LINQ简化
            foreach (var info in openingInfos)
            {
                Openings.Add(new OpeningInfoViewModel(info.Opening, uiApp));
            }

            CreateModelLinesCommand = new BaseBindingCommand(_ => ShowOptionsAndCreate());
            RefreshCommand = new BaseBindingCommand(_ => RefreshOpenings());
            OkCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void ShowOptionsAndCreate()
        {
            var optionsVm = new OpeningCreateModelLineViewModel();
            var optionsWindow = new OpeningCreateModelLineView { DataContext = optionsVm };

            optionsVm.CreationScopeSelected += scope => CreateModelLines(scope);
            optionsVm.CloseWindow = () => optionsWindow.Close();

            optionsWindow.ShowDialog();
        }

        private void CreateModelLines(CreationScope scope)
        {
            using (var transaction = new Transaction(_doc, "创建X模型线"))
            {
                transaction.Start();

                IEnumerable<OpeningInfoViewModel> openingsToProcess;
                switch (scope)
                {
                    case CreationScope.AllOpenings:
                        openingsToProcess = Openings;
                        break;
                    case CreationScope.ShaftOpeningsOnly:
                        openingsToProcess = Openings.Where(o => o.Type == "竖井洞口");
                        break;
                    case CreationScope.DisplayedOpening:
                        openingsToProcess = SelectedOpening != null
                            ? new[] { SelectedOpening }
                            : Array.Empty<OpeningInfoViewModel>();
                        break;
                    default:
                        openingsToProcess = Array.Empty<OpeningInfoViewModel>();
                        break;
                }

                foreach (var openingVm in openingsToProcess)
                {
                    var opening = _doc.GetElement(openingVm.Id) as Opening;
                    if (opening != null)
                    {
                        CreateBoundingBoxLines(opening);
                    }
                }

                transaction.Commit();
            }
        }

        private void CreateBoundingBoxLines(Opening opening)
        {
            var bbox = opening.get_BoundingBox(null);
            if (bbox == null) return;

            var corners = GetBoundingBoxCorners(bbox);
            var edges = new[] { (0, 1), (1, 2), (2, 3), (3, 0), (4, 5), (5, 6), (6, 7), (7, 4), (0, 4), (1, 5), (2, 6), (3, 7) };

            foreach (var (startIdx, endIdx) in edges)
            {
                CreateModelLine(corners[startIdx], corners[endIdx]);
            }
        }

        private void CreateModelLine(XYZ start, XYZ end)
        {
            try
            {
                var line = Autodesk.Revit.DB.Line.CreateBound(start, end);
                var sketchPlane = CreateSketchPlaneForLine(line);
                _doc.Create.NewModelCurve(line, sketchPlane);
            }
            catch (Exception)
            {
                // 忽略无法创建的线（如零长度线）
            }
        }

        private SketchPlane CreateSketchPlaneForLine(Autodesk.Revit.DB.Line line)
        {
            var norm = line.GetEndPoint(0).X == line.GetEndPoint(1).X ? new XYZ(1, 0, 0) :
                       line.GetEndPoint(0).Y == line.GetEndPoint(1).Y ? new XYZ(0, 1, 0) :
                       new XYZ(0, 0, 1);

            var plane = Plane.CreateByNormalAndOrigin(norm, line.GetEndPoint(0));
            return SketchPlane.Create(_doc, plane);
        }

        private List<XYZ> GetBoundingBoxCorners(BoundingBoxXYZ bbox)
        {
            var min = bbox.Min;
            var max = bbox.Max;

            return new List<XYZ>
            {
                new XYZ(min.X, min.Y, max.Z), new XYZ(min.X, max.Y, max.Z),
                new XYZ(max.X, max.Y, max.Z), new XYZ(max.X, min.Y, max.Z),
                new XYZ(min.X, min.Y, min.Z), new XYZ(min.X, max.Y, min.Z),
                new XYZ(max.X, max.Y, min.Z), new XYZ(max.X, min.Y, min.Z)
            };
        }

        private void RefreshOpenings()
        {
            // 重新加载洞口信息
            var collector = new FilteredElementCollector(_doc);
            var newOpenings = collector.OfClass(typeof(Opening)).Cast<Opening>().ToList();

            Openings.Clear();
            foreach (var opening in newOpenings)
            {
                Openings.Add(new OpeningInfoViewModel(opening, _uiApp));
            }
        }

        public Action CloseWindow { get; set; }
    }
    /// <summary>
    /// 洞口信息ViewModel
    /// </summary>
    public class OpeningInfoViewModel : ObserverableObject
    {
        private readonly Opening _opening;
        private readonly UIApplication _uiApp;
        private bool _isSelected;
        private DrawingVisual _previewVisual;

        public OpeningInfoViewModel(Opening opening, UIApplication uiApp)
        {
            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
            _uiApp = uiApp;

            Name = $"洞口 {opening.Id.IntegerValue}";
            Type = "标准洞口";
            //Type = opening is ShaftOpening ? "竖井洞口" : "标准洞口";

            InitializePreview();
        }

        public string Name { get; }
        public string Type { get; }
        public ElementId Id => _opening.Id;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public DrawingVisual PreviewVisual
        {
            get => _previewVisual;
            set { _previewVisual = value; OnPropertyChanged(); }
        }

        private void InitializePreview()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var options = new Options { ComputeReferences = true };
            var geometry = _opening.get_Geometry(options);

            _previewVisual = new DrawingVisual();
            using (var dc = _previewVisual.RenderOpen())
            {
                DrawGeometry(dc, geometry);
                DrawBoundingBox(dc);
            }
        }

        private void DrawGeometry(DrawingContext dc, GeometryElement geometry)
        {
            var pen = new Pen(Brushes.Blue, 1);
            foreach (var geomObj in geometry ?? Enumerable.Empty<GeometryObject>())
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    DrawSolidEdges(dc, solid, pen);
                }
                else if (geomObj is GeometryInstance instance)
                {
                    DrawInstanceEdges(dc, instance, pen);
                }
            }
        }

        private void DrawSolidEdges(DrawingContext dc, Solid solid, Pen pen)
        {
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                foreach (EdgeArray edgeLoop in face.EdgeLoops)
                {
                    foreach (Edge edge in edgeLoop)
                    {
                        DrawEdge(dc, edge, pen);
                    }
                }
            }
        }

        private void DrawInstanceEdges(DrawingContext dc, GeometryInstance instance, Pen pen)
        {
            var geometry = instance.GetInstanceGeometry();
            foreach (var geomObj in geometry)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                    DrawSolidEdges(dc, solid, pen);
            }
        }

        private void DrawEdge(DrawingContext dc, Edge edge, Pen pen)
        {
            var points = edge.Tessellate().ToList();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = ProjectTo2D(points[i]);
                var p2 = ProjectTo2D(points[i + 1]);
                dc.DrawLine(pen, p1, p2);
            }
        }

        private void DrawBoundingBox(DrawingContext dc)
        {
            var bbox = _opening.get_BoundingBox(null);
            if (bbox == null) return;

            var pen = new Pen(Brushes.Red, 1.5);
            var corners = GetBoundingBoxCorners(bbox);

            // 绘制12条边线
            var edges = new[]
            {
                (0,1), (1,2), (2,3), (3,0),  // 顶面
                (4,5), (5,6), (6,7), (7,4),  // 底面
                (0,4), (1,5), (2,6), (3,7)   // 垂直线
            };

            foreach (var (start, end) in edges)
            {
                var p1 = ProjectTo2D(corners[start]);
                var p2 = ProjectTo2D(corners[end]);
                dc.DrawLine(pen, p1, p2);
            }
        }

        private List<XYZ> GetBoundingBoxCorners(BoundingBoxXYZ bbox)
        {
            var min = bbox.Min;
            var max = bbox.Max;

            return new List<XYZ>
            {
                new XYZ(min.X, min.Y, max.Z), // 0
                new XYZ(min.X, max.Y, max.Z), // 1
                new XYZ(max.X, max.Y, max.Z), // 2
                new XYZ(max.X, min.Y, max.Z), // 3
                new XYZ(min.X, min.Y, min.Z), // 4
                new XYZ(min.X, max.Y, min.Z), // 5
                new XYZ(max.X, max.Y, min.Z), // 6
                new XYZ(max.X, min.Y, min.Z)  // 7
            };
        }

        private System.Windows.Point ProjectTo2D(XYZ point) => new System.Windows.Point(point.X, point.Y);
    }

}
