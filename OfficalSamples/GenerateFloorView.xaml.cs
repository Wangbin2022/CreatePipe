using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// GenerateFloorView.xaml 的交互逻辑
    /// </summary>
    public partial class GenerateFloorView : Window
    {
        public GenerateFloorView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext=new GenerateFloorViewModel(uiApp);
        }
    }



    public class GenerateFloorViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly FloorGenerationService _service;
        private FloorGenerationData _data;
        private bool _isProcessing;
        private string _statusMessage;
        private ImageSource _previewImage;

        public GenerateFloorViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _service = new FloorGenerationService(uiApp);

            GenerateCommand = new BaseBindingCommand(ExecuteGenerate);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);

            ExecuteRefresh(null);
        }

        #region 属性

        public FloorGenerationData Data
        {
            get => _data;
            set { _data = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ImageSource PreviewImage
        {
            get => _previewImage;
            set { _previewImage = value; OnPropertyChanged(); }
        }

        public string SelectedFloorType
        {
            get => Data?.FloorType?.Name;
            set
            {
                if (Data != null && !string.IsNullOrEmpty(value))
                {
                    _service.SetFloorTypeByName(Data, value);
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 命令

        public ICommand GenerateCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        private bool CanExecuteGenerate() => !IsProcessing && Data != null && Data.Profile != null;

        private void ExecuteRefresh(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在分析墙体轮廓...";

            try
            {
                Data = _service.GetDataFromSelectedWalls();
                GeneratePreviewImage();
                StatusMessage = $"分析完成，找到 {Data.PreviewPoints?.Count ?? 0} 个轮廓点";
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void ExecuteGenerate(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在生成楼板...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var transaction = new Transaction(_uiApp.ActiveUIDocument.Document, "生成楼板"))
                    {
                        transaction.Start();
                        _service.CreateFloor(Data);
                        transaction.Commit();
                    }

                    DispatcherHelper.Invoke(() =>
                    {
                        StatusMessage = "楼板生成成功！";
                        TaskDialog.Show("完成", "楼板已成功生成");
                    });
                }
                catch (Exception ex)
                {
                    DispatcherHelper.Invoke(() => StatusMessage = $"生成失败: {ex.Message}");
                }
                finally
                {
                    DispatcherHelper.Invoke(() => IsProcessing = false);
                }
            });
        }

        /// <summary>
        /// 生成预览图像
        /// </summary>
        private void GeneratePreviewImage()
        {
            if (Data?.PreviewPoints == null || Data.PreviewPoints.Count == 0) return;

            const int width = 256;
            const int height = 208;
            var margin = 10;

            using (var bitmap = new System.Drawing.Bitmap(width, height))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Black);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // 计算缩放和平移
                var scaleX = (width - 2 * margin) / Data.MaxLength;
                var scaleY = (height - 2 * margin) / Data.MaxLength;
                var scale = Math.Min(scaleX, scaleY);

                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 2))
                {
                    for (int i = 0; i < Data.PreviewPoints.Count - 1; i++)
                    {
                        var p1 = Data.PreviewPoints[i];
                        var p2 = Data.PreviewPoints[i + 1];

                        var x1 = (float)(margin + p1.X * scale);
                        var y1 = (float)(margin + p1.Y * scale);
                        var x2 = (float)(margin + p2.X * scale);
                        var y2 = (float)(margin + p2.Y * scale);

                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }

                PreviewImage = ConvertBitmapToSource(bitmap);
            }
        }

        private static ImageSource ConvertBitmapToSource(System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
    /// <summary>
    /// 墙体分析和楼板生成服务
    /// </summary>
    public class FloorGenerationService
    {
        private const double Precision = 0.00000001;
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private readonly Autodesk.Revit.Creation.Application _creator;

        public FloorGenerationService(UIApplication uiApp)
        {
            _uiDoc = uiApp.ActiveUIDocument;
            _doc = _uiDoc.Document;
            _creator = uiApp.Application.Create;
        }
        /// <summary>
        /// 从选中的墙体获取所有数据
        /// </summary>
        public FloorGenerationData GetDataFromSelectedWalls()
        {
            // 获取选中的墙体
            var selectedWalls = GetSelectedWalls();
            if (selectedWalls.Count == 0)
                throw new InvalidOperationException("请先选中构成封闭轮廓的墙体");

            var data = new FloorGenerationData();

            // 获取所有楼板类型
            data.FloorTypeNames = GetFloorTypes();
            if (data.FloorTypeNames.Count > 0)
                data.FloorType = GetFloorTypeByName(data.FloorTypeNames[0]);

            // 获取墙体所在标高（所有墙体必须在同一标高）
            data.Level = GetCommonLevel(selectedWalls);

            // 获取并排序墙体轮廓曲线
            var unsortedCurves = GetWallCurves(selectedWalls);
            data.Profile = SortCurvesToClosedLoop(unsortedCurves);

            // 生成预览点数据
            GeneratePreviewPoints(data);

            data.IsStructural = true;

            return data;
        }

        /// <summary>
        /// 获取选中的墙体
        /// </summary>
        private List<Wall> GetSelectedWalls()
        {
            var walls = new List<Wall>();
            foreach (var id in _uiDoc.Selection.GetElementIds())
            {
                if (_doc.GetElement(id) is Wall wall)
                    walls.Add(wall);
            }
            return walls;
        }

        /// <summary>
        /// 获取所有楼板类型名称
        /// </summary>
        private ObservableCollection<string> GetFloorTypes()
        {
            var names = new ObservableCollection<string>();
            var collector = new FilteredElementCollector(_doc);

            foreach (var element in collector.OfClass(typeof(FloorType)))
            {
                if (element is FloorType ft && ft.Category?.Name == "Floors")
                    names.Add(ft.Name);
            }
            return names;
        }

        /// <summary>
        /// 根据名称获取楼板类型
        /// </summary>
        private FloorType GetFloorTypeByName(string name)
        {
            var collector = new FilteredElementCollector(_doc);
            return collector.OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .FirstOrDefault(ft => ft.Name == name);
        }

        /// <summary>
        /// 获取所有墙体的公共标高
        /// </summary>
        private Level GetCommonLevel(List<Wall> walls)
        {
            var firstLevel = _doc.GetElement(walls[0].LevelId) as Level;
            var firstElevation = firstLevel?.Elevation ?? 0;

            foreach (var wall in walls)
            {
                var level = _doc.GetElement(wall.LevelId) as Level;
                if (Math.Abs((level?.Elevation ?? 0) - firstElevation) > Precision)
                    throw new InvalidOperationException("所有墙体必须在同一标高上");
            }
            return firstLevel;
        }

        /// <summary>
        /// 获取墙体的中心线曲线
        /// </summary>
        private CurveArray GetWallCurves(List<Wall> walls)
        {
            var curves = new CurveArray();
            foreach (var wall in walls)
            {
                if (wall.Location is LocationCurve locationCurve)
                    curves.Append(locationCurve.Curve);
            }
            return curves;
        }

        /// <summary>
        /// 将无序的曲线排序为封闭环
        /// </summary>
        private CurveArray SortCurvesToClosedLoop(CurveArray unsorted)
        {
            var sorted = new CurveArray();
            var current = unsorted.get_Item(0);
            var endPoint = current.GetEndPoint(1);

            sorted.Append(current);

            while (sorted.Size != unsorted.Size)
            {
                current = FindNextCurve(unsorted, endPoint, current);

                // 检查端点匹配，必要时反转曲线
                if (ArePointsEqual(endPoint, current.GetEndPoint(0)))
                {
                    endPoint = current.GetEndPoint(1);
                }
                else
                {
                    endPoint = current.GetEndPoint(0);
                }

                sorted.Append(current);
            }

            // 验证闭合性
            var firstCurve = unsorted.get_Item(0);
            if (!ArePointsEqual(endPoint, firstCurve.GetEndPoint(0)))
                throw new InvalidOperationException("选中的墙体未能形成封闭轮廓");

            return sorted;
        }

        /// <summary>
        /// 查找下一条连接的曲线
        /// </summary>
        private Curve FindNextCurve(CurveArray curves, XYZ connectionPoint, Curve currentCurve)
        {
            foreach (Curve curve in curves)
            {
                if (curve.Equals(currentCurve)) continue;

                // 检查起点连接
                if (ArePointsEqual(curve.GetEndPoint(0), connectionPoint))
                    return curve;

                // 检查终点连接，需要反转曲线
                if (ArePointsEqual(curve.GetEndPoint(1), connectionPoint))
                    return ReverseCurve(curve);
            }
            throw new InvalidOperationException("墙体曲线未能形成连续轮廓");
        }

        /// <summary>
        /// 判断两点是否相等
        /// </summary>
        private static bool ArePointsEqual(XYZ p1, XYZ p2) =>
            Math.Abs(p1.X - p2.X) < Precision &&
            Math.Abs(p1.Y - p2.Y) < Precision &&
            Math.Abs(p1.Z - p2.Z) < Precision;

        /// <summary>
        /// 反转曲线方向
        /// </summary>
        private Curve ReverseCurve(Curve curve)
        {
            if (curve is Line line)
                return Line.CreateBound(line.GetEndPoint(1), line.GetEndPoint(0));

            if (curve is Arc arc)
            {
                var points = arc.Tessellate();
                var start = points.First();
                var end = points.Last();
                var mid = points[points.Count / 2];
                return Arc.Create(end, start, mid);
            }

            throw new NotSupportedException($"不支持的曲线类型: {curve.GetType().Name}");
        }

        /// <summary>
        /// 生成预览图形点集
        /// </summary>
        private void GeneratePreviewPoints(FloorGenerationData data)
        {
            var pointList = new List<XYZ>();
            double xMin = 0, xMax = 0, yMin = 0, yMax = 0;
            bool first = true;

            foreach (Curve curve in data.Profile)
            {
                var points = curve.Tessellate() as List<XYZ>;
                foreach (var p in points)
                {
                    var transformed = new XYZ(p.X, -p.Y, p.Z);
                    pointList.Add(transformed);

                    if (first)
                    {
                        xMin = xMax = transformed.X;
                        yMin = yMax = transformed.Y;
                        first = false;
                    }
                    else
                    {
                        xMin = Math.Min(xMin, transformed.X);
                        xMax = Math.Max(xMax, transformed.X);
                        yMin = Math.Min(yMin, transformed.Y);
                        yMax = Math.Max(yMax, transformed.Y);
                    }
                }
            }

            data.MaxLength = Math.Max(xMax - xMin, yMax - yMin);
            data.PreviewPoints = new List<PointF>();

            for (int i = 0; i < pointList.Count; i += 2)
            {
                data.PreviewPoints.Add(new PointF(
                    (float)(pointList[i].X - xMin),
                    (float)(pointList[i].Y - yMin)));
            }

            // 闭合轮廓：添加第一个点作为终点
            if (data.PreviewPoints.Count > 0)
                data.PreviewPoints.Add(data.PreviewPoints[0]);
        }

        /// <summary>
        /// 创建楼板
        /// </summary>
        public void CreateFloor(FloorGenerationData data)
        {
            _doc.Create.NewFloor(data.Profile, data.FloorType, data.Level, data.IsStructural);
        }

        /// <summary>
        /// 根据名称设置楼板类型
        /// </summary>
        public void SetFloorTypeByName(FloorGenerationData data, string typeName)
        {
            data.FloorType = GetFloorTypeByName(typeName);
        }
    }
    /// <summary>
    /// 楼板生成配置数据模型
    /// </summary>
    public class FloorGenerationData : ObserverableObject
    {
        private FloorType _floorType;
        private Level _level;
        private CurveArray _profile;
        private bool _isStructural = true;
        private List<PointF> _previewPoints;
        private double _maxLength;
        private ObservableCollection<string> _floorTypeNames;

        /// <summary>选择的楼板类型</summary>
        public FloorType FloorType
        {
            get => _floorType;
            set { _floorType = value; OnPropertyChanged(); }
        }

        /// <summary>楼板所在标高</summary>
        public Level Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        /// <summary>楼板轮廓曲线集合</summary>
        public CurveArray Profile
        {
            get => _profile;
            set { _profile = value; OnPropertyChanged(); }
        }

        /// <summary>是否为结构楼板</summary>
        public bool IsStructural
        {
            get => _isStructural;
            set { _isStructural = value; OnPropertyChanged(); }
        }

        /// <summary>预览点集合（用于图形绘制）</summary>
        public List<PointF> PreviewPoints
        {
            get => _previewPoints;
            set { _previewPoints = value; OnPropertyChanged(); }
        }

        /// <summary>预览区域最大边长</summary>
        public double MaxLength
        {
            get => _maxLength;
            set { _maxLength = value; OnPropertyChanged(); }
        }

        /// <summary>可用楼板类型名称列表</summary>
        public ObservableCollection<string> FloorTypeNames
        {
            get => _floorTypeNames;
            set { _floorTypeNames = value; OnPropertyChanged(); }
        }
    }
}
