using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static System.Windows.Forms.AxHost;


namespace CreatePipe.Form
{
    /// <summary>
    /// StairsManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class StairsManagerView : Window
    {
        public StairsManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new StairsManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public partial class StairsManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<StairsEntity>
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public View ActiveView { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        private readonly StairsWarningService _stairsWarningService; // ViewModel 持有 StairsWarningService 实例
        public StairsManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            _stairsWarningService = new StairsWarningService(Document);
            InitFunc();
        }
        public void InitFunc()
        {
            // 改为初始化时一次性加载全部数据 // 默认显示全部
            LoadAllStairsFromDocument();
            QueryElement(string.Empty);
            // 构建楼梯组合（从 _allStairsCache 中构建
            CollectionStairGroup.Clear();
            if (_allStairsCache == null || _allStairsCache.Count == 0) return;
            List<StairsGroup> groupCollections = new List<StairsGroup>();
            // 第一步：过滤
            var filteredEntities = _allStairsCache.Where(e => e.IsMultiStairs || (e.stairTotalHeight >= 1000 && e.Runs >= 2)).ToList();
            if (!filteredEntities.Any()) return;
            // 第二步：按中心点位置分组
            var remainingEntities = new HashSet<StairsEntity>(filteredEntities);
            while (remainingEntities.Any())
            {
                var baseEntity = remainingEntities.First();
                var currentGroup = new List<StairsEntity> { baseEntity };
                remainingEntities.Remove(baseEntity);
                var baseCenter = baseEntity.stairCenter;
                var matchedEntities = remainingEntities.Where(c => IsPositionMatch(baseCenter, c.stairCenter)).ToList();
                foreach (var matched in matchedEntities)
                {
                    currentGroup.Add(matched);
                    remainingEntities.Remove(matched);
                }
                groupCollections.Add(new StairsGroup(currentGroup, ExternalHandler));
            }
            foreach (var item in groupCollections)
            {
                CollectionStairGroup.Add(item);
            }
        }
        private void LoadAllStairsFromDocument()
        {
            try
            {
                var stairs = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Stairs)
                    .WhereElementIsNotElementType().Cast<Stairs>().ToList();
                // 一次性分析警告
                StairsWarningAnalysisResult analysisResult = _stairsWarningService.AnalyzeStairsWarnings();
                _allStairsCache = new List<StairsEntity>();
                // 分离多层楼梯和单层楼梯
                var multiStairsGroups = new Dictionary<ElementId, List<Stairs>>();
                var singleStairs = new List<Stairs>();
                foreach (var stair in stairs)
                {
                    // 检查是否属于多层楼梯
                    if (stair.MultistoryStairsId != null &&
                        stair.MultistoryStairsId != ElementId.InvalidElementId)
                    {
                        var multiStairsId = stair.MultistoryStairsId;
                        if (!multiStairsGroups.ContainsKey(multiStairsId))
                        {
                            multiStairsGroups[multiStairsId] = new List<Stairs>();
                        }
                        multiStairsGroups[multiStairsId].Add(stair);
                    }
                    else
                    {
                        singleStairs.Add(stair);
                    }
                }
                // 处理单层楼梯
                foreach (var stair in singleStairs)
                {
                    bool hasWarnings = _stairsWarningService.HasWarningsForStairs(stair.Id, analysisResult);
                    var entity = new StairsEntity(stair, hasWarnings);
                    _allStairsCache.Add(entity);
                }
                // 处理多层楼梯组
                foreach (var kvp in multiStairsGroups)
                {
                    var multiStairsId = kvp.Key;
                    var stairComponents = kvp.Value;
                    // 获取多层楼梯组对象
                    MultistoryStairs multiStairs = Document.GetElement(multiStairsId) as MultistoryStairs;
                    if (multiStairs != null)
                    {
                        // 检查这个多层楼梯组是否有警告
                        bool hasWarnings = stairComponents
                            .Any(component => _stairsWarningService.HasWarningsForStairs(component.Id, analysisResult));
                        // 传入 MultistoryStairs 对象
                        var entity = new StairsEntity(multiStairs, hasWarnings);
                        _allStairsCache.Add(entity);
                    }
                }
                // 排序
                _allStairsCache = _allStairsCache.OrderBy(e => e.stairName).ToList();
            }
            catch (Exception ex)
            {
                _allStairsCache = new List<StairsEntity>();
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"加载楼梯失败：{ex.Message}");
            }
        }
        private List<StairsEntity> _allStairsCache;
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string searchText)
        {
            Collection.Clear();
            if (_allStairsCache == null || _allStairsCache.Count == 0) return;
            // 字符串过滤（内存操作，极快）
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allStairsCache : _allStairsCache.Where(e =>
                {
                    string searchLower = searchText.ToLowerInvariant();
                    string stairId = e.Id.IntegerValue.ToString();
                    return (e.stairName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                           (stairId.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }).ToList();
            foreach (var item in filtered)
            {
                Collection.Add(item);
            }
        }
        // 判断两个点是否满足位置匹配条件
        //使用绝对距离容差：不分别比较X和Y，而是计算两点在XY平面上的欧几里得距离：
        private bool IsPositionMatch(XYZ point1, XYZ point2)
        {
            //单向容差设置在50mm左右
            double Tolerance = 0.165;
            //另一侧容差保持2%
            double PercentageDeviation = 0.02;
            // 忽略Z值，只比较X和Y
            double x1 = point1.X;
            double y1 = point1.Y;
            double x2 = point2.X;
            double y2 = point2.Y;
            double dx = Math.Abs(x1 - x2);
            double dy = Math.Abs(y1 - y2);

            // 计算XY平面距离
            double distance = Math.Sqrt(dx * dx + dy * dy);
            // 条件1：距离在容差范围内（适用于任意方向偏移// 0.5 英尺约150mm
            const double distanceTolerance = 0.5;
            if (distance <= distanceTolerance)
                return true;
            // 条件2：X相等（容差0.01）且Y偏差2%
            bool condition1 = dx <= Tolerance &&
                              IsWithinPercentageDeviation(y1, y2, PercentageDeviation);
            // 条件3：Y相等（容差0.01）且X偏差2%
            bool condition2 = dy <= Tolerance &&
                              IsWithinPercentageDeviation(x1, x2, PercentageDeviation);
            return condition1 || condition2;
            //// 检查X是否相等（容差0.01）且Y偏差2%
            //bool condition1 = Math.Abs(x1 - x2) <= Tolerance &&
            //                  IsWithinPercentageDeviation(y1, y2, PercentageDeviation);
            //// 检查Y是否相等（容差0.01）且X偏差2%
            //bool condition2 = Math.Abs(y1 - y2) <= Tolerance &&
            //                  IsWithinPercentageDeviation(x1, x2, PercentageDeviation);
            //return condition1 || condition2;
        }
        // 检查两个值是否在指定的百分比偏差范围内
        private bool IsWithinPercentageDeviation(double value1, double value2, double percentage)
        {
            double Tolerance = 0.01;
            // 处理值为0的特殊情况
            if (Math.Abs(value1) < Tolerance && Math.Abs(value2) < Tolerance)
                return true; // 两个值都为0，视为匹配
            if (Math.Abs(value1) < Tolerance || Math.Abs(value2) < Tolerance)
                return false; // 一个为0，另一个不为0，不匹配
            // 计算偏差百分比（基于两个值中的较大者）
            double deviation = Math.Abs(value1 - value2) / Math.Max(Math.Abs(value1), Math.Abs(value2));
            return deviation <= percentage;
        }
        public ICommand PickElementCommand => new RelayCommand<StairsEntity>(PickElement);
        private void PickElement(StairsEntity entity)
        {
            uIDoc.Selection.SetElementIds(new List<ElementId> { entity.Id });
        }
        public ICommand SubViewCommand => new RelayCommand<StairsGroup>(SubView);
        private static void SubView(StairsGroup group)
        {
            if (group == null) return;
            Dictionary<string, string> stairInstanceCount = group.StairInstanceCount;
            UniversalDictionaryListView universalDictionaryList = new UniversalDictionaryListView(stairInstanceCount, "分层统计");
            universalDictionaryList.ShowDialog();
        }
        public ICommand NewSectionBoxViewCommand => new RelayCommand<StairsGroup>(NewSectionBoxView);
        private void NewSectionBoxView(StairsGroup group)
        {
            if (group == null) return;
            List<Element> stairs = new List<Element>();
            foreach (var item in group.SelectedStairs)
            {
                //stairs.Add(item.Stair);
                if (item.IsMultiStairs && item.MultiStairs != null)
                {
                    stairs.Add(item.MultiStairs);
                }
                else if (item.Stair != null)
                {
                    stairs.Add(item.Stair);
                }
            }
            // 3. 获取或切换到三维视图
            View3D targetView = uIDoc.ActiveView as View3D;
            if (targetView == null || targetView.IsTemplate)
            {
                targetView = new FilteredElementCollector(Document).OfClass(typeof(View3D)).Cast<View3D>()
                    .FirstOrDefault(v => !v.IsTemplate && v.ViewType == ViewType.ThreeD);
            }
            if (targetView == null)
            {
                TaskDialog.Show("错误", "未找到可用的三维视图"); return;
            }
            // 4. 合并所有楼梯的包围框
            BoundingBoxXYZ mergedBBox = MergeBoundingBoxes(Document, stairs, targetView);
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "建立包围框", () =>
                {
                    targetView.SetSectionBox(mergedBBox);

                });
            });
            uIDoc.ActiveView = targetView;
        }
        // 合并多个包围框为一个最大的包含框 通用方法
        private BoundingBoxXYZ MergeBoundingBoxes<T>(Document doc, List<T> elements, View3D view) where T : Element
        {
            if (elements == null || elements.Count == 0) return null;
            BoundingBoxXYZ mergedBBox = elements[0].get_BoundingBox(view);
            if (mergedBBox == null) return null;
            XYZ minPoint = mergedBBox.Min;
            XYZ maxPoint = mergedBBox.Max;
            for (int i = 1; i < elements.Count; i++)
            {
                BoundingBoxXYZ bbox = elements[i].get_BoundingBox(view);
                if (bbox == null) continue;
                minPoint = new XYZ(
                    Math.Min(minPoint.X, bbox.Min.X),
                    Math.Min(minPoint.Y, bbox.Min.Y),
                    Math.Min(minPoint.Z, bbox.Min.Z)
                );
                maxPoint = new XYZ(
                    Math.Max(maxPoint.X, bbox.Max.X),
                    Math.Max(maxPoint.Y, bbox.Max.Y),
                    Math.Max(maxPoint.Z, bbox.Max.Z)
                );
            }
            BoundingBoxXYZ result = new BoundingBoxXYZ();
            result.Min = minPoint;
            result.Max = maxPoint;
            return result;
        }
        public ICommand NewSectionCommand => new RelayCommand<StairsGroup>(NewSection);
        private void NewSection(StairsGroup group)
        {
            if (group == null) return;
            List<Element> stairs = new List<Element>();
            foreach (var item in group.SelectedStairs)
            {
                //stairs.Add(item.Stair);
                if (item.IsMultiStairs && item.MultiStairs != null)
                {
                    stairs.Add(item.MultiStairs);
                }
                else if (item.Stair != null)
                {
                    stairs.Add(item.Stair);
                }
            }
            // 3. 获取或切换到三维视图
            View3D targetView = uIDoc.ActiveView as View3D;
            if (targetView == null || targetView.IsTemplate)
            {
                targetView = new FilteredElementCollector(Document).OfClass(typeof(View3D)).Cast<View3D>()
                    .FirstOrDefault(v => !v.IsTemplate && v.ViewType == ViewType.ThreeD);
            }
            if (targetView == null)
            {
                TaskDialog.Show("错误", "未找到可用的三维视图"); return;
            }
            // 4. 合并所有楼梯的包围框
            BoundingBoxXYZ mergedBBox = MergeBoundingBoxes(Document, stairs, targetView);
            ViewSection createdSection = null;
            // 1. 在事务中创建视图
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "生成剖面", () =>
                {
                    // 使用自动使用长边生成剖面
                    var dir = PickDirectionByLongestEdge(mergedBBox);
                    ViewSection sectionView = SectionViewHelper.CreateSection(Document, mergedBBox, dir, 200);
                    createdSection = sectionView; // 保存创建的视图引用
                });
                // 2. 在事务外部激活视图
                if (createdSection != null)
                {
                    uIDoc.ActiveView = createdSection;
                }
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<StairsEntity>(DeleteElement);
        public void DeleteElement(StairsEntity entity)
        {
            throw new NotImplementedException();
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            var selectedEntities = selectedItems.Cast<StairsEntity>().ToList();
            if (selectedEntities == null || !selectedEntities.Any())
            {
                TaskDialog.Show("提示", "请选择要删除的元素");
                return;
            }
            var idsToDelete = selectedEntities
                .Where(e => e.Id != null && e.Id != ElementId.InvalidElementId)
                .Select(e => e.Id).Distinct().ToList();
            if (!idsToDelete.Any())
            {
                TaskDialog.Show("提示", "没有可删除的有效元素");
                return;
            }
            // 执行删除
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "删除实例", () =>
                {
                    try
                    {
                        Document.Delete(idsToDelete);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("错误", $"删除失败：{ex.Message}");
                    }
                });
                // 刷新列表
                InitFunc();
            });

        }
        private ObservableCollection<StairsEntity> allStairs = new ObservableCollection<StairsEntity>();
        public ObservableCollection<StairsEntity> Collection
        {
            get => allStairs;
            set => SetProperty(ref allStairs, value);
        }
        private ObservableCollection<StairsGroup> allStairGroups = new ObservableCollection<StairsGroup>();
        public ObservableCollection<StairsGroup> CollectionStairGroup
        {
            get => allStairGroups;
            set => SetProperty(ref allStairGroups, value);
        }
        private SectionDirection PickDirectionByLongestEdge(BoundingBoxXYZ bbox)
        {
            double dx = bbox.Max.X - bbox.Min.X;
            double dy = bbox.Max.Y - bbox.Min.Y;
            // 长边为 X → 生成沿 X 观察的剖面（Front，看整个 X 立面）
            // 长边为 Y → 生成沿 Y 观察的剖面（Right/Left）
            return dx >= dy ? SectionDirection.Front : SectionDirection.Right;
        }
        /// <summary>
        /// 剖面观察方向
        /// </summary>
        public enum SectionDirection
        {
            Front,   // 前视：从 +Y 看向 -Y
            Back,    // 后视：从 -Y 看向 +Y
            Left,    // 左视：从 -X 看向 +X
            Right,   // 右视：从 +X 看向 -X
            Top,     // 俯视：从 +Z 看向 -Z
            Bottom   // 仰视：从 -Z 看向 +Z
        }
        public static class SectionViewHelper
        {
            private const double MM_TO_FEET = 1.0 / 304.8;
            /// <summary>
            /// 根据包围盒和指定方向创建剖面视图（裁剪范围紧贴包围盒边界）
            /// ★ 必须在事务(Transaction)内调用
            /// </summary>
            /// <param name="doc">文档</param>
            /// <param name="bbox">包围盒（可带 Transform）</param>
            /// <param name="direction">观察方向</param>
            /// <param name="marginMm">宽/高方向外扩边距(mm)，默认0严格贴合；正值会超出边界</param>
            /// <returns>创建的剖面视图</returns>
            public static ViewSection CreateSection(Document doc, BoundingBoxXYZ bbox, SectionDirection direction, double marginMm = 0)
            {
                if (doc == null || bbox == null) return null;

                // 1. 获取剖面视图类型
                ViewFamilyType vft = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);
                if (vft == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ 未找到剖面视图类型");
                    return null;
                }

                // 2. 将包围盒转换为世界坐标轴对齐的 min/max（兼容带 Transform 的 bbox）
                GetWorldAABB(bbox, out XYZ min, out XYZ max);

                XYZ center = (min + max) * 0.5;
                double hx = (max.X - min.X) * 0.5;  // 世界 X 半尺寸
                double hy = (max.Y - min.Y) * 0.5;  // 世界 Y 半尺寸
                double hz = (max.Z - min.Z) * 0.5;  // 世界 Z 半尺寸

                // 3. 根据方向确定坐标系与局部尺寸
                //    localHalf = (右方向半宽, 上方向半高, 深度半长)
                XYZ basisX, basisY, basisZ;
                double halfW, halfH, halfD;

                switch (direction)
                {
                    case SectionDirection.Front:
                        basisX = -XYZ.BasisX; basisY = XYZ.BasisZ; basisZ = XYZ.BasisY;
                        halfW = hx; halfH = hz; halfD = hy;
                        break;
                    case SectionDirection.Back:
                        basisX = XYZ.BasisX; basisY = XYZ.BasisZ; basisZ = -XYZ.BasisY;
                        halfW = hx; halfH = hz; halfD = hy;
                        break;
                    case SectionDirection.Left:
                        basisX = -XYZ.BasisY; basisY = XYZ.BasisZ; basisZ = -XYZ.BasisX;
                        halfW = hy; halfH = hz; halfD = hx;
                        break;
                    case SectionDirection.Right:
                        basisX = XYZ.BasisY; basisY = XYZ.BasisZ; basisZ = XYZ.BasisX;
                        halfW = hy; halfH = hz; halfD = hx;
                        break;
                    case SectionDirection.Top:
                        basisX = XYZ.BasisX; basisY = XYZ.BasisY; basisZ = XYZ.BasisZ;
                        halfW = hx; halfH = hy; halfD = hz;
                        break;
                    case SectionDirection.Bottom:
                        basisX = -XYZ.BasisX; basisY = XYZ.BasisY; basisZ = -XYZ.BasisZ;
                        halfW = hx; halfH = hy; halfD = hz;
                        break;
                    default:
                        return null;
                }

                double margin = marginMm * MM_TO_FEET;

                // 4. 构建剖面坐标系
                Transform transform = Transform.Identity;
                transform.Origin = center;
                transform.BasisX = basisX;
                transform.BasisY = basisY;
                transform.BasisZ = basisZ;

                // 5. 构建剖面框
                //    宽高可加 margin；深度 Z 不加 margin，保证裁剪不超出包围盒
                BoundingBoxXYZ sectionBox = new BoundingBoxXYZ
                {
                    Transform = transform,
                    Min = new XYZ(-halfW - margin, -halfH - margin, -halfD),
                    Max = new XYZ(halfW + margin, halfH + margin, halfD)
                };

                // 6. 创建剖面视图
                ViewSection section = ViewSection.CreateSection(doc, vft.Id, sectionBox);

                if (section != null)
                {
                    // 确保远裁剪激活，深度严格贴合
                    Parameter farActive = section.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
                    farActive?.Set(1);

                    section.Scale = 100;
                    section.DetailLevel = ViewDetailLevel.Medium;

                    System.Diagnostics.Debug.WriteLine(
                        $"✅ 创建剖面成功: {direction}, 深度={halfD * 2 * 304.8:F0}mm");
                }

                return section;
            }

            /// <summary>
            /// 将（可能带 Transform 的）包围盒转换为世界坐标轴对齐的 Min/Max
            /// </summary>
            private static void GetWorldAABB(BoundingBoxXYZ bbox, out XYZ min, out XYZ max)
            {
                Transform t = bbox.Transform ?? Transform.Identity;
                XYZ bmin = bbox.Min;
                XYZ bmax = bbox.Max;
                // 若为单位变换，直接返回
                if (t.IsIdentity)
                {
                    min = bmin;
                    max = bmax;
                    return;
                }
                // 变换 8 个角点，重新求轴对齐包围盒
                double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                        for (int k = 0; k < 2; k++)
                        {
                            XYZ corner = new XYZ(
                                i == 0 ? bmin.X : bmax.X,
                                j == 0 ? bmin.Y : bmax.Y,
                                k == 0 ? bmin.Z : bmax.Z);
                            XYZ w = t.OfPoint(corner);

                            minX = Math.Min(minX, w.X); maxX = Math.Max(maxX, w.X);
                            minY = Math.Min(minY, w.Y); maxY = Math.Max(maxY, w.Y);
                            minZ = Math.Min(minZ, w.Z); maxZ = Math.Max(maxZ, w.Z);
                        }
                min = new XYZ(minX, minY, minZ);
                max = new XYZ(maxX, maxY, maxZ);
            }
        }
    }
}
