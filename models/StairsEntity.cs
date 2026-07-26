using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class StairsGroup : ObserverableObject
    {
        public Document Document;
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        public StairsGroup(List<StairsEntity> stairs, BaseExternalHandler _handler)
        {
            Document = stairs.FirstOrDefault().Document;
            SelectedStairs = new ObservableCollection<StairsEntity>(stairs);
            ExternalHandler = _handler;
            // 初始化名称
            if (SelectedStairs.Any())
            {
                _name = SelectedStairs.First().stairName;
            }
            // 楼梯统计
            StairInstanceCount = stairs.ToDictionary(g => g.Id.IntegerValue.ToString(), g => g.startLevelHeight.ToString("F2"));
        }
        public Dictionary<string, string> StairInstanceCount { get; set; } = new Dictionary<string, string>();
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                // 避免重复赋值
                if (_name == value) return;
                // ★ 修复：在外部处理器中执行事务
                ExternalHandler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改楼梯组名称", () =>
                    {
                        // ★ 修复：使用 value，而非 _name
                        foreach (var item in SelectedStairs)
                        {
                            var param = item.Stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (param != null && !param.IsReadOnly)
                            {
                                // ★ 关键：Set 的是新值 value
                                param.Set(value);
                            }
                            // ★ 同时更新内存中的 StairsEntity.stairName
                            item.stairName = value;
                        }
                        // ★ 在事务内更新 _name
                        _name = value;
                    });
                });
                // ★ 修复：传参 nameof(Name)，通知 UI 更新
                OnPropertyChanged(nameof(Name));
            }
        }
        private ObservableCollection<StairsEntity> _selectedStairs;
        public ObservableCollection<StairsEntity> SelectedStairs
        {
            get => _selectedStairs;
            set
            {
                if (_selectedStairs == value) return;
                _selectedStairs = value;
                OnPropertyChanged(nameof(SelectedStairs));

                if (_selectedStairs?.Any() == true)
                {
                    _name = _selectedStairs.First().stairName;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
    }
    public class StairsEntity : ObserverableObject
    {
        public Document Document;
        // 楼梯引用
        public Stairs Stair { get; private set; }
        public MultistoryStairs MultiStairs { get; private set; }
        //public Stairs Stair { get; set; }
        public StairsEntity(Element stairElement, bool hasWarnings)
        {
            Document = stairElement.Document;
            Id = stairElement.Id;
            HasWarnings = hasWarnings;

            // 判断类型并初始化
            if (stairElement is MultistoryStairs multiStairs)
            {
                InitializeFromMultiStairs(multiStairs, hasWarnings);
            }
            else if (stairElement is Stairs singleStair)
            {
                InitializeFromSingleStair(singleStair, hasWarnings);
            }
            else
            {
                throw new ArgumentException("元素必须是 Stairs 或 MultistoryStairs 类型");
            }
        }
        // 从单层楼梯初始化
        private void InitializeFromSingleStair(Stairs stair, bool hasWarnings)
        {
            Stair = stair;
            IsMultiStairs = false;
            // 计算楼梯参数
            stepHeight = stair.ActualRiserHeight * 304.8;
            stepWidth = stair.ActualTreadDepth * 304.8;
            ActualVerticalSteps = stair.ActualRisersNumber;
            ActualHorizontalSteps = stair.ActualTreadsNumber;
            Runs = stair.GetStairsRuns().Count;
            stairTotalHeight = stair.Height * 304.8;
            // 获取梯段宽度
            var firstRunId = stair.GetStairsRuns().FirstOrDefault();
            if (firstRunId != null)
            {
                var run = Document.GetElement(firstRunId) as StairsRun;
                stairRunWidth = run?.ActualRunWidth * 304.8 ?? 0;
            }
            // 计算标高
            var basePoint = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().FirstOrDefault();
            double deltaHeight = basePoint?.Position.Z * 304.8 ?? 0;
            startLevelHeight = stair.BaseElevation * 304.8 - deltaHeight;
            // 获取名称
            stairName = stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
            // 获取中心点
            stairCenter = GetStairCenter(stair);
            if (startLevelHeight == 0) isBaseStair = true;
        }
        // 从多层楼梯初始化
        private void InitializeFromMultiStairs(MultistoryStairs multiStairs, bool hasWarnings)
        {
            MultiStairs = multiStairs;
            IsMultiStairs = true;
            // 获取所有组件
            var allStairsIds = multiStairs.GetAllStairsIds();
            StairComponents = new List<Stairs>();
            foreach (var id in allStairsIds)
            {
                var stair = Document.GetElement(id) as Stairs;
                if (stair != null)
                {
                    StairComponents.Add(stair);
                }
            }
            ComponentCount = multiStairs.GetStairsPlacementLevels(StairComponents.FirstOrDefault()).Count;
            if (StairComponents.Any())
            {
                // 使用第一个楼梯作为代表计算参数
                var firstStair = StairComponents.FirstOrDefault();
                if (firstStair != null)
                {
                    // 计算单层参数
                    stepHeight = firstStair.ActualRiserHeight * 304.8;
                    stepWidth = firstStair.ActualTreadDepth * 304.8;
                    Runs = firstStair.GetStairsRuns().Count * ComponentCount;
                    ActualVerticalSteps = firstStair.ActualRisersNumber * ComponentCount;
                    ActualHorizontalSteps = firstStair.ActualTreadsNumber * ComponentCount;
                    stairTotalHeight = firstStair.Height * 304.8 * ComponentCount;
                    // 获取梯段宽度
                    var firstRunId = firstStair.GetStairsRuns().FirstOrDefault();
                    if (firstRunId != null)
                    {
                        var run = Document.GetElement(firstRunId) as StairsRun;
                        stairRunWidth = run?.ActualRunWidth * 304.8 ?? 0;
                    }
                    // 计算标高
                    var basePoint = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().FirstOrDefault();
                    double deltaHeight = basePoint?.Position.Z * 304.8 ?? 0;
                    startLevelHeight = firstStair.BaseElevation * 304.8 - deltaHeight;
                    // 获取名称
                    stairName = firstStair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
                    // 获取中心点（使用第一个楼梯的中心点）
                    stairCenter = GetStairCenter(firstStair);
                }
            }
            else
            {
            }
            if (startLevelHeight == 0) isBaseStair = true;
        }
        // 辅助方法：获取楼梯中心点
        private XYZ GetStairCenter(Stairs stair)
        {
            BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            if (bbox != null)
            {
                XYZ min = bbox.Min;
                XYZ max = bbox.Max;
                XYZ center = (min + max) * 0.5;
                return new XYZ(center.X, center.Y, 0);
            }
            return XYZ.Zero;
        }
        public int ComponentCount { get; private set; }
        public List<Stairs> StairComponents = new List<Stairs>();
        //public double stairArea { get; } = 0;
        //public string startLevelName { get; set; }
        //梯段组合属性  实现属性变更通知
        private string _stairName;
        public string stairName
        {
            get => _stairName;
            set
            {
                if (_stairName != value)
                {
                    _stairName = value;
                    OnPropertyChanged(nameof(stairName));
                }
            }
        }
        public bool isBaseStair { get; set; } = false;
        //以下为单体梯段属性
        public double startLevelHeight { get; set; }
        public XYZ stairCenter { get; set; } = new XYZ();
        public double stairRunWidth { get; set; } = 0;
        public double stairTotalHeight { get; set; } = 0;
        public double stepHeight { get; set; } = 0;
        public double stepWidth { get; set; } = 0;
        public int ActualHorizontalSteps { get; set; } = 0;
        public int ActualVerticalSteps { get; set; } = 0;
        public int Runs { get; set; } = 0;
        public bool HasWarnings { get; private set; } = false; // 新增属性
        public bool IsMultiStairs { get; set; } = false;
        public ElementId Id { get; set; }

    }
}
