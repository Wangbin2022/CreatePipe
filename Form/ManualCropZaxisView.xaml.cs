using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
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
        private Document doc;
        private UIDocument uiDoc;
        private View activeView;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();

        // 1. 数据绑定集合
        public List<Level> LevelList { get; set; }

        private Level _selectedLevel;
        public Level SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                OnPropertyChanged(nameof(SelectedLevel));
                UpdateLevelMetrics(); // 切换标高时，更新滑动条上下限
            }
        }

        public ManualCropZaxisViewModel(UIApplication uIApplication)
        {
            uiDoc = uIApplication.ActiveUIDocument;
            doc = uiDoc.Document;
            activeView = uiDoc.ActiveView;

            // 1. 预先获取全项目标高（排序好）
            var allLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            // 2. 确定当前视图关联的标高
            // 注意：有些视图的 GenLevel 可能是 null，需要做兼容
            Level viewLevel = activeView.GenLevel;

            // 3. 根据视图类型填充 LevelList
            if (activeView is ViewPlan)
            {
                // 平面视图：仅加载当前标高
                // 如果 GenLevel 为空（极少见），则加载全部防止报错
                if (viewLevel != null)
                    LevelList = allLevels.Where(l => l.Id == viewLevel.Id).ToList();
                else
                    LevelList = allLevels;
            }
            else
            {
                // 三维视图或其他：加载全部标高
                LevelList = allLevels;
            }

            // 4. 【关键步骤】默认选中逻辑
            // 必须从 LevelList 集合中去找那个对象，否则 ComboBox 无法识别选中态
            if (viewLevel != null)
            {
                SelectedLevel = LevelList.FirstOrDefault(l => l.Id == viewLevel.Id);
            }

            // 如果还没选上（比如在没有关联标高的三维视图里），默认选第一个
            if (SelectedLevel == null && LevelList.Count > 0)
            {
                SelectedLevel = LevelList.FirstOrDefault();
            }

            // 5. 初始化滑块（如果 SelectedLevel 此时已有值，UpdateLevelMetrics 会在 Setter 中触发）
            if (SelectedLevel != null)
            {
                UpdateLevelMetrics();
            }
        }
        private void UpdateLevelMetrics()
        {
            if (SelectedLevel == null) return;

            // 1. 更新当前高度显示 (mm)
            CurrentLevelHeight = SelectedLevel.Elevation * 304.8;
            OnPropertyChanged(nameof(CurrentLevelHeight));

            // 2. 计算与下一层的间距
            Level nextLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(l => l.Elevation > SelectedLevel.Elevation)
                .OrderBy(l => l.Elevation)
                .FirstOrDefault();

            double layerHeightMm = 4000; // 默认值
            if (nextLevel != null)
            {
                layerHeightMm = (nextLevel.Elevation - SelectedLevel.Elevation) * 304.8;
            }

            // 3. 动态调整滑块最大值 (允许用户切到本层以上1米)
            CropZaxisBottomValueMax = layerHeightMm;
            CropZaxisTopValueMax = layerHeightMm + 1000;

            // 4. 默认值设置：底为0，顶为层高
            CropZaxisBottomValue = 0;
            CropZaxisTopValue = layerHeightMm;

            // 5. 通知 UI 更新
            OnPropertyChanged(nameof(CropZaxisBottomValueMax));
            OnPropertyChanged(nameof(CropZaxisTopValueMax));
            OnPropertyChanged(nameof(CropZaxisBottomValue));
            OnPropertyChanged(nameof(CropZaxisTopValue));
        }
        // 当选中的标高改变时，重新计算滑动条限制
        //private void UpdateLevelMetrics()
        //{
        //    if (SelectedLevel == null) return;
        //    // 当前标高高度 (mm)
        //    CurrentLevelHeight = SelectedLevel.Elevation * 304.8;
        //    OnPropertyChanged(nameof(CurrentLevelHeight));
        //    // 查找下一层，计算层高
        //    Level nextLevel = new FilteredElementCollector(doc)
        //        .OfClass(typeof(Level))
        //        .Cast<Level>()
        //        .Where(l => l.Elevation > SelectedLevel.Elevation)
        //        .OrderBy(l => l.Elevation)
        //        .FirstOrDefault();
        //    double DEFAULT_MAX_HEIGHT_MM = 4000.0; // 默认给4米范围
        //    double layerHeight = nextLevel != null
        //        ? (nextLevel.Elevation - SelectedLevel.Elevation) * 304.8
        //        : DEFAULT_MAX_HEIGHT_MM;
        //    // 动态调整滑动条范围：底标高通常在 [0, 层高]，顶标高在 [0, 层高+1000]
        //    CropZaxisBottomValueMax = layerHeight;
        //    CropZaxisTopValueMax = layerHeight + 1000;
        //    // 如果当前值超过了新上限，重置一下
        //    if (CropZaxisBottomValue > CropZaxisBottomValueMax) CropZaxisBottomValue = 0;
        //    if (CropZaxisTopValue > CropZaxisTopValueMax) CropZaxisTopValue = layerHeight;
        //    OnPropertyChanged(nameof(CropZaxisBottomValueMax));
        //    OnPropertyChanged(nameof(CropZaxisTopValueMax));
        //}
        public ICommand PlaceCropViewCommand => new BaseBindingCommand(PlaceCropView);
        private void PlaceCropView(object obj)
        {
            _externalHandler.Run(app =>
            {
                try
                {
                    // 获取选定标高的物理高度
                    double baseElevation = SelectedLevel.ProjectElevation;
                    // 1. 交互拾取点
                    XYZ p1 = uiDoc.Selection.PickPoint("请选择剖面框范围的第一个角点");
                    XYZ p2 = uiDoc.Selection.PickPoint("请选择剖面框范围的对角点");
                    // 2. 计算 Z 轴范围 (基于 SelectedLevel)
                    double minZ = baseElevation + (CropZaxisBottomValue / 304.8);
                    double maxZ = baseElevation + (CropZaxisTopValue / 304.8);
                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ
                    {
                        Min = new XYZ(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), minZ),
                        Max = new XYZ(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y), maxZ)
                    };
                    // 3. 执行生成
                    using (Transaction tx = new Transaction(doc, "手动剖切生成"))
                    {
                        tx.Start();
                        ViewFamilyType vft3D = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);

                        View3D newView = View3D.CreateIsometric(doc, vft3D.Id);
                        newView.Name = GetUniqueViewName(doc, $"剖切_{SelectedLevel.Name}");
                        newView.IsSectionBoxActive = true;
                        newView.SetSectionBox(sectionBox);
                        tx.Commit();
                        uiDoc.ActiveView = newView;
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            });
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
        // 属性定义
        public double CurrentLevelHeight { get; private set; }
        public double CropZaxisBottomValueMax { get; set; }
        public double CropZaxisTopValueMax { get; set; }

        private double _cropZaxisBottomValue = 0;
        public double CropZaxisBottomValue
        {
            get => _cropZaxisBottomValue;
            set { _cropZaxisBottomValue = value; OnPropertyChanged(nameof(CropZaxisBottomValue)); OnPropertyChanged(nameof(CanPlaceCropView)); }
        }

        private double _cropZaxisTopValue = 3000;
        public double CropZaxisTopValue
        {
            get => _cropZaxisTopValue;
            set { _cropZaxisTopValue = value; OnPropertyChanged(nameof(CropZaxisTopValue)); OnPropertyChanged(nameof(CanPlaceCropView)); }
        }

        public bool CanPlaceCropView => CropZaxisTopValue > CropZaxisBottomValue;
        public double SliderTickFrequency => 100;
    }
}
