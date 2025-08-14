using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CreatePipe.models
{
    public class OpenningEntity : ObserverableObject
    {
        public Document Document { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public OpenningEntity(FamilySymbol symbol, List<FamilyInstance> instances)
        {
            Document = symbol.Document;
            Symbol = symbol;
            Instances = instances;
            foreach (var item in instances)
            {
                InstanceIds.Add(item.Id);
            }
            entityNum = instances.Count;
            entityId = symbol.Id;
            entityTagName = symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID).AsString();
            entityHeight = symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM).AsDouble() * 304.8;
            entityWidth = symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM).AsDouble() * 304.8;
            //
            FloorInstanceCount = instances.GroupBy(fi => fi.Document.GetElement(fi.LevelId)?.Name ?? "未定义楼层").ToDictionary(g => g.Key, g => g.Count().ToString());
            //foreach (var instance in instances)
            //{
            //    Level level = Document.GetElement(instance.LevelId) as Level;
            //    if (level != null)
            //    {
            //        string levelName = level.Name;
            //        if (FloorInstanceCount.ContainsKey(levelName))
            //        {
            //            FloorInstanceCount[levelName]++;
            //        }
            //        else
            //        {
            //            FloorInstanceCount[levelName] = 1;
            //        }
            //    }
            //}
            //relatedViews[item.Id.IntegerValue.ToString()] = item.Name;

            //区别类型
            if (entityTagName.Contains("FM"))
            {
                entityType = "防火门";
                entityCategoty = "M";
            }
            else if (entityTagName.Contains("BM") || entityTagName.Contains("BH") || entityTagName.Contains("BG"))
            {
                entityType = "人防门";
                entityCategoty = "M";
            }
            else if (entityTagName.Contains("WM"))
            {
                entityType = "外门";
                entityCategoty = "M";
            }
            else if (entityTagName.Contains("FJ"))
            {
                entityType = "防火卷帘";
            }
            else if (entityTagName.Contains("D") || entityTagName.Contains("DK"))
            {
                entityType = "洞口";
            }
            else if (entityTagName.Contains("BY") || entityTagName.Contains("BYC"))
            {
                entityType = "百叶窗";
                entityCategoty = "C";
            }
            else if (entityTagName.Contains("C") || entityTagName.Contains("LC"))
            {
                entityType = "普通窗";
                entityCategoty = "C";
            }
            else if (Regex.IsMatch(entityTagName, @"^\d+(\.\d+)?$"))
            {
                entityType = "未识别";
            }
            else
            {
                entityType = "门";
                entityCategoty = "M";
            }
            //检测是否匹配
            if (entityTagName == entityName)
            {
                isMatching = true;
            }
            // 宽度分类
            if (entityWidth > 0 && entityWidth < 900)
            {
                entityWidthType = "A";
            }
            else if (entityWidth >= 900 && entityWidth < 2100)
            {
                entityWidthType = "B";
            }
            else if (entityWidth >= 2100 && entityWidth < 2700)
            {
                entityWidthType = "C";
            }
            else if (entityWidth == 0 && entityWidth == 0)
            {
                entityWidthType = "0";
            }
            else
            {
                entityWidthType = "X"; // 其他情况
            }
        }
        public string entityCategoty { get; set; }
        public string entityWidthType { get; set; }
        public double entityWidth { get; set; }
        public double entityHeight { get; set; }
        public bool isMatching { get; set; } = false;
        public string entityTagName { get; set; }
        public string entityType { get; set; }
        public Dictionary<string, string> FloorInstanceCount { get; set; } = new Dictionary<string, string>();
        public int entityNum { get; set; } = 0;
        public string entityName
        {
            get => Symbol.Name;
            set
            {
                _externalHandler.Run(app =>
                {
                    Document.NewTransaction(() => Symbol.Name = value, "修改名称");
                    OnPropertyChanged();
                });
            }
        }
        public ElementId entityId { get; set; }
        public List<FamilyInstance> Instances { get; set; } = new List<FamilyInstance>();
        public List<ElementId> InstanceIds { get; set; } = new List<ElementId>();
        public FamilySymbol Symbol { get; set; }
    }
}
