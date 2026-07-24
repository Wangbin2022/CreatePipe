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
            StairInstanceCount = stairs.ToDictionary( g => g.Id.IntegerValue.ToString(), g => g.startLevelHeight.ToString("F2"));
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
        public StairsEntity(Stairs stair, bool hasWarnings)
        {
            Document = stair.Document;
            Stair = stair;
            Id = stair.Id;
            if (stair.MultistoryStairsId != ElementId.InvalidElementId)
            {
                IsMultiStairs = true;
                MultistoryStairs multiStairs = Document.GetElement(stair.MultistoryStairsId) as MultistoryStairs;
                var ids = multiStairs.GetStairsPlacementLevels(stair);
                Stairs firstStair = multiStairs.GetStairsOnLevel(Document.GetElement(ids.FirstOrDefault()).Id);

                stepHeight = firstStair.ActualRiserHeight * 304.8;
                stepWidth = firstStair.ActualTreadDepth * 304.8;
                Runs = firstStair.GetStairsRuns().Count * ids.Count;
                ActualVerticalSteps = firstStair.ActualRisersNumber * ids.Count;
                ActualHorizontalSteps = firstStair.ActualTreadsNumber * ids.Count;
                stairTotalHeight = firstStair.Height * 304.8 * ids.Count;
                stairRunWidth = (Document.GetElement(firstStair.GetStairsRuns().FirstOrDefault()) as StairsRun).ActualRunWidth * 304.8;
                ////绝对高度底和顶，要计入项目基点高差                
                var basePoint = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
                double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
                startLevelHeight = firstStair.BaseElevation * 304.8 - deltaHeight;
            }
            else
            {
                stepHeight = stair.ActualRiserHeight * 304.8;
                stepWidth = stair.ActualTreadDepth * 304.8;
                ActualVerticalSteps = stair.ActualRisersNumber;
                ActualHorizontalSteps = stair.ActualTreadsNumber;
                Runs = stair.GetStairsRuns().Count;
                stairTotalHeight = stair.Height * 304.8;
                stairRunWidth = (Document.GetElement(stair.GetStairsRuns().FirstOrDefault()) as StairsRun).ActualRunWidth * 304.8;
                ////绝对高度底和顶，要计入项目基点高差                
                var basePoint = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_ProjectBasePoint).Cast<BasePoint>().ToList();
                double deltaHeight = basePoint.FirstOrDefault().Position.Z * 304.8;
                startLevelHeight = stair.BaseElevation * 304.8 - deltaHeight;
            }
            stairName = stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
            //取楼梯中心几何点投影点
            BoundingBoxXYZ bbox = stair.get_BoundingBox(null);
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;
            XYZ center = (min + max) * 0.5;
            stairCenter = new XYZ(center.X, center.Y, 0);
            if (startLevelHeight == 0) isBaseStair = true;

            HasWarnings = hasWarnings;
        }
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
        public bool isBaseStair { get; } = false;
        //以下为单体梯段属性
        public double startLevelHeight { get; }
        public XYZ stairCenter { get; } = new XYZ();
        public double stairRunWidth { get; } = 0;
        public double stairTotalHeight { get; } = 0;
        public double stepHeight { get; } = 0;
        public double stepWidth { get; } = 0;
        public int ActualHorizontalSteps { get; } = 0;
        public int ActualVerticalSteps { get; } = 0;
        public int Runs { get; } = 0;
        public bool HasWarnings { get; private set; } = false; // 新增属性
        public bool IsMultiStairs { get; } = false;
        public ElementId Id { get; set; }
        public Stairs Stair { get; set; }
    }
}
