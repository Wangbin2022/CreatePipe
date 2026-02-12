using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe
{
    /// <summary>
    /// ManualCropZaxisView.xaml 的交互逻辑
    /// </summary>
    public partial class ManualCropZaxisView : Window
    {
        public ManualCropZaxisView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new ManualCropZaxisViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class ManualCropZaxisViewModel : ObserverableObject
    {
        Document Document;
        View ActiveView;
        UIDocument uiDoc;
        public ManualCropZaxisViewModel(UIApplication uIApplication)
        {
            uiDoc = uIApplication.ActiveUIDocument;
            Document = uIApplication.ActiveUIDocument.Document;
            ActiveView = uIApplication.ActiveUIDocument.ActiveView;
            if (uIApplication.ActiveUIDocument.ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请在平面视图执行剖面框生成功能");
                IsViewValid = false;
                return;
            }
            Level currentLevel = ActiveView.GenLevel;
            CurrentLevel = currentLevel.Name;
            double levelElevationFt = currentLevel.Elevation;
            CurrentLevelHeight = levelElevationFt * 304.8;
            // 找下一层标高
            Level nextLevel = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>()
                .Where(l => l.Elevation > currentLevel.Elevation).OrderBy(l => l.Elevation)
                .FirstOrDefault();
            double heightDiffMm;
            double DEFAULT_MAX_HEIGHT_MM = 1500.0;
            if (nextLevel != null)
            {
                // 如果找到了下一层，计算实际高差
                heightDiffMm = nextLevel.Elevation * 304.8 - CurrentLevelHeight;
            }
            else
            {
                // 如果没有下一层，使用默认高度
                heightDiffMm = DEFAULT_MAX_HEIGHT_MM;
            }
            CropZaxisBottomValueMax = Math.Max(heightDiffMm, DEFAULT_MAX_HEIGHT_MM);
            CropZaxisTopValueMax = Math.Max(heightDiffMm, DEFAULT_MAX_HEIGHT_MM);
            // 4. 初始化滑块的当前值
            CropZaxisBottomValue = CropZaxisBottomValueMin;
            CropZaxisTopValue = CropZaxisTopValueMax;
        }
        public ICommand PlaceCropViewCommand => new BaseBindingCommand(PlaceCropView);
        private void PlaceCropView(object obj)
        {
            try
            {
                double levelElevation = ActiveView.GenLevel.ProjectElevation;
                //// 3. 让用户框选一个区域
                XYZ p1, p2;
                p1 = uiDoc.Selection.PickPoint("请选择剖面框范围的第一个角点");
                p2 = uiDoc.Selection.PickPoint("请选择剖面框范围的对角点");
                //// 定义剖面框的高度（可以替换为用户输入）
                //// 例如，从标高线下 -0.5 米到标高线上 3 米
                //double bottomOffset = -0.5 / 3.28084; // -0.5m in feet
                double bottomOffset = CropZaxisBottomValue / 304.8; // -0.5m in feet
                double topOffset = CropZaxisTopValue / 304.8;    // 3.0m in feet
                // 4. 创建 BoundingBoxXYZ
                double minX = Math.Min(p1.X, p2.X);
                double minY = Math.Min(p1.Y, p2.Y);
                double maxX = Math.Max(p1.X, p2.X);
                double maxY = Math.Max(p1.Y, p2.Y);
                // Z 坐标基于标高和偏移量
                double minZ = levelElevation + bottomOffset;
                double maxZ = levelElevation + topOffset;
                BoundingBoxXYZ sectionBox = new BoundingBoxXYZ
                {
                    Min = new XYZ(minX, minY, minZ),
                    Max = new XYZ(maxX, maxY, maxZ)
                };
                //// 5. 创建一个新的三维视图并应用剖面框
                using (Transaction tx = new Transaction(Document, "创建指定标高剖面视图"))
                {
                    tx.Start();
                    // 找到一个三维视图类型用于创建新视图
                    ViewFamilyType viewFamilyType3D = new FilteredElementCollector(Document)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);
                    if (viewFamilyType3D == null)
                    {
                        TaskDialog.Show("tt", "项目中没有找到三维视图类型");
                        return;
                    }
                    string uniqueViewName = GetUniqueViewName(Document, $"指定标高剖面{CurrentLevel}");
                    // 创建一个新的三维视图
                    View3D newView = View3D.CreateIsometric(Document, viewFamilyType3D.Id);
                    newView.Name = uniqueViewName;
                    // 启用并设置剖面框
                    newView.IsSectionBoxActive = true;
                    newView.SetSectionBox(sectionBox);
                    // (可选但推荐) 调整相机视角为俯视，并缩放到合适大小
                    //newView.LookupParameter("Display Style").Set("Shaded"); // 设置视觉样式
                    //OrientToTop(newView);
                    newView.CropBoxActive = false; // 关闭裁剪区域，只用剖面框
                    tx.Commit();
                    // 切换到新创建的视图
                    uiDoc.ActiveView = newView;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message.ToString());
            }
        }
        private string GetUniqueViewName(Document doc, string baseName)
        {
            // 1. 获取当前项目中所有的视图名称，以便快速查找。
            // 使用 HashSet 可以提供 O(1) 的平均查找性能。
            var existingViewNames = new FilteredElementCollector(doc).OfClass(typeof(View)).ToElements().Select(v => v.Name).ToHashSet();
            // 2. 直接从后缀 "-1" 开始尝试
            int counter = 1;
            string newName;
            // 3. 循环查找一个可用的名称
            while (true)
            {
                newName = $"{baseName}-{counter}";
                if (!existingViewNames.Contains(newName))
                {
                    // 找到了一个可用的名称，跳出循环
                    return newName; // 直接返回找到的名称
                }
                counter++; // 如果当前名称被占用，则继续尝试下一个数字
            }
            // 注意：因为是 while(true) 循环且总能找到一个名称，所以循环外的 return 是不需要的。
        }
        public string CurrentLevel { get; }
        public double CurrentLevelHeight { get; }
        public double SliderTickFrequency { get; private set; } = 300;
        public double CropZaxisBottomValueMin { get; set; } = 0;
        public double CropZaxisBottomValueMax { get; set; }
        private double _cropZaxisBottomValue = 0;
        public double CropZaxisBottomValue
        {
            get => _cropZaxisBottomValue;
            set
            {
                if (_cropZaxisBottomValue != value)
                {
                    _cropZaxisBottomValue = value;
                    OnPropertyChanged(nameof(CropZaxisBottomValue));
                    OnPropertyChanged(nameof(CanPlaceCropView));
                }
            }
            //set => SetProperty(ref _cropZaxisBottomValue, value);
        }
        public double CropZaxisTopValueMin { get; set; } = 300;
        public double CropZaxisTopValueMax { get; set; }
        private double _cropZaxisTopValue = 300;
        public double CropZaxisTopValue
        {
            get => _cropZaxisTopValue;
            set
            {
                if (_cropZaxisTopValue != value)
                {
                    _cropZaxisTopValue = value;
                    OnPropertyChanged(nameof(CropZaxisTopValue));
                    OnPropertyChanged(nameof(CanPlaceCropView));
                }
            }
        }
        public bool IsViewValid { get; private set; } = true;
        public bool CanPlaceCropView
        {
            get
            {
                return IsViewValid && CropZaxisTopValue > CropZaxisBottomValue;
            }
        }
    }
}
