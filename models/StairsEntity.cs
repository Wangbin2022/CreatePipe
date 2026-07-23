using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class StairsGroup : ObserverableObject
    {
        public Document Document;
        private readonly BaseExternalHandler _handler;
        public List<StairsEntity> selectedStairs { get; set; } = new List<StairsEntity>();
        public StairsGroup(List<StairsEntity> stairs)
        {
            Document = stairs.FirstOrDefault().Document;

        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改名称", () =>
                    {
                        foreach (var item in selectedStairs)
                        {
                            item.Stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(_name);
                        }
                    });
                    _name = value;
                });
                OnPropertyChanged();
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
                stairName = stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
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
                stairName = stair.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            }
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
        //梯段组合属性
        public string stairName { get; set; }
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
