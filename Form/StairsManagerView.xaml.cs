using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

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
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();


        private readonly StairsWarningService _stairsWarningService; // ViewModel 持有 StairsWarningService 实例
        public StairsManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            uIDoc = uiApp.ActiveUIDocument;
            _stairsWarningService = new StairsWarningService(Document);
            QueryElement(string.Empty);
        }
        public void InitFunc() => QueryElement(null);
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string obj)
        {
            Collection.Clear();
            //  优化核心：先对轻量的 Room 对象进行过滤 
            var stairs = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Stairs)
                .WhereElementIsNotElementType().Cast<Stairs>().ToList();
            // 1. 进行一次全局的警告分析。这是最有效率的方式，避免循环查询Revit的警告列表。
            StairsWarningAnalysisResult analysisResult = _stairsWarningService.AnalyzeStairsWarnings();
            // 只为过滤后的结果创建 RoomSingleEntity 对象
            foreach (var stair in stairs)
            {
                // 创建对象时传入缓存数据
                bool hasWarnings = _stairsWarningService.HasWarningsForStairs(stair.Id, analysisResult);
                //bool hasWarnings = _roomWarningService.HasWarningsForRoom(room.Id);
                var entity = new StairsEntity(stair, hasWarnings);
                if (string.IsNullOrEmpty(obj) || (!string.IsNullOrEmpty(entity.stairName) && entity.stairName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Collection.Add(entity);
                }
            }
            //继续构造楼梯组合
            if (Collection == null) return;
            List<List<StairsEntity>> groups = GroupStairsEntities(Collection);


        }
        /// <summary>
        /// 按照指定规则整理楼梯实体集合
        /// </summary>
        /// <param name="entities">原始楼梯实体集合</param>
        /// <returns>整理后的List集合列表</returns>
        public List<List<StairsEntity>> GroupStairsEntities(IEnumerable<StairsEntity> entities)
        {
            if (entities == null || !entities.Any())
                return new List<List<StairsEntity>>();
            // 第一步：过滤
            // IsMultiStairs = true 不受过滤条件限制，直接保留
            // 其他实体需要满足：高度 >= 1000 且 梯段数 >= 2
            var filteredEntities = entities.Where(e => e.IsMultiStairs || (e.stairTotalHeight >= 1000 && e.Runs >= 2)).ToList();
            if (!filteredEntities.Any()) return new List<List<StairsEntity>>();
            // 第二步：按中心点位置分组
            var groups = new List<List<StairsEntity>>();
            var remainingEntities = new HashSet<StairsEntity>(filteredEntities);
            while (remainingEntities.Any())
            {
                // 取出第一个实体作为基准
                var baseEntity = remainingEntities.First();
                var currentGroup = new List<StairsEntity> { baseEntity };
                remainingEntities.Remove(baseEntity);
                // 查找与基准实体位置匹配的其他实体
                List<StairsEntity> matchedEntities = new List<StairsEntity>();
                var baseCenter = baseEntity.stairCenter;
                foreach (var candidate in remainingEntities)
                {
                    var candidateCenter = candidate.stairCenter;
                    if (IsPositionMatch(baseCenter, candidateCenter))
                    {
                        matchedEntities.Add(candidate);
                    }
                }
                foreach (var matched in matchedEntities)
                {
                    currentGroup.Add(matched);
                    remainingEntities.Remove(matched);
                }
                // 即使只有1个实体，也作为独立组加入
                groups.Add(currentGroup);
            }
            TaskDialog.Show("tt", groups.Count.ToString());
            return groups;
        }
        /// <summary>
        /// 判断两个点是否满足位置匹配条件
        /// X相等（容差0.01）且Y偏差2%
        /// 或 Y相等（容差0.01）且X偏差2%
        /// </summary>
        private bool IsPositionMatch(XYZ point1, XYZ point2)
        {
            double Tolerance = 0.01;
            double PercentageDeviation = 0.02;
            // 忽略Z值，只比较X和Y
            double x1 = point1.X;
            double y1 = point1.Y;
            double x2 = point2.X;
            double y2 = point2.Y;
            // 检查X是否相等（容差0.01）且Y偏差2%
            bool condition1 = Math.Abs(x1 - x2) <= Tolerance &&
                              IsWithinPercentageDeviation(y1, y2, PercentageDeviation);
            // 检查Y是否相等（容差0.01）且X偏差2%
            bool condition2 = Math.Abs(y1 - y2) <= Tolerance &&
                              IsWithinPercentageDeviation(x1, x2, PercentageDeviation);
            return condition1 || condition2;
        }
        /// <summary>
        /// 检查两个值是否在指定的百分比偏差范围内
        /// </summary>
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

        public ICommand DeleteElementCommand => new RelayCommand<StairsEntity>(DeleteElement);
        public void DeleteElement(StairsEntity entity)
        {
            throw new NotImplementedException();
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            throw new NotImplementedException();
        }
        private ObservableCollection<StairsEntity> allRooms = new ObservableCollection<StairsEntity>();
        public ObservableCollection<StairsEntity> Collection
        {
            get => allRooms;
            set => SetProperty(ref allRooms, value);
        }
    }
}
