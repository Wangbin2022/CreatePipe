using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CreatePipe.models
{
    public class OpenningEntity : ObserverableObject
    {
        // 基本属性 (使用 PascalCase)
        public ElementId SymbolId { get; }
        public string TagName { get; set; }     // 类型标记 (WINDOW_TYPE_ID)
        public string Name { get; set; }        // 族类型名称
        public string Type { get; set; }        // 分类名称 (人防门、防火门等)
        public string CategoryCode { get; set; } // M 或 C
        public double Width { get; set; }
        public double Height { get; set; }
        public string WidthType { get; set; }
        public int InstanceCount { get; set; } = 0;
        public bool HaveInstance => InstanceCount > 0;
        public bool IsMatching { get; set; }
        public Dictionary<string, string> FloorInstanceCount { get; set; } = new Dictionary<string, string>();
        // 原始数据引用 (供 ViewModel 逻辑使用)
        public List<ElementId> InstanceIds { get; }
        public OpenningEntity(FamilySymbol symbol, List<FamilyInstance> instances)
        {
            SymbolId = symbol.Id;
            Name = symbol.Name;
            InstanceIds = instances.Select(i => i.Id).ToList();
            InstanceCount = instances.Count;
            // 1. 参数读取 (注意：在构造此 Entity 时，必须已在 Revit 线程中)
            TagName = symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsString() ?? "";
            Height = Math.Round(symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)?.AsDouble() * 304.8 ?? 0, 0);
            Width = Math.Round(symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)?.AsDouble() * 304.8 ?? 0, 0);
            // 2. 楼层统计
            FloorInstanceCount = instances.GroupBy(fi => fi.Document.GetElement(fi.LevelId)?.Name ?? "未定义楼层")
                .ToDictionary(g => g.Key, g => g.Count().ToString());
            // 3. 识别逻辑
            ParseType(TagName);
            IsMatching = (TagName == Name);
            // 4. 宽度分类逻辑
            string calculatedWidthType;
            // 这是一个技巧，通过 switch(true) 来模拟 if-else if 结构
            switch (true)
            {
                case bool _ when Width > 0 && Width < 900:
                    calculatedWidthType = "A";
                    break;
                case bool _ when Width >= 900 && Width < 2100:
                    calculatedWidthType = "B";
                    break;
                case bool _ when Width >= 2100 && Width < 2700:
                    calculatedWidthType = "C";
                    break;
                case bool _ when Width == 0:
                    calculatedWidthType = "0";
                    break;
                default:
                    calculatedWidthType = "X";
                    break;
            }
            WidthType = calculatedWidthType;
        }

        private void ParseType(string tag)
        {
            if (tag.Contains("FM")) { Type = "防火门"; CategoryCode = "M"; }
            // 修正这里：遍历数组，检查 tag 是否包含数组中的某个字符串
            else if (tag.Contains("BM") || tag.Contains("BH") || tag.Contains("BG")) { Type = "人防门"; CategoryCode = "M"; }
            else if (tag.Contains("WM")) { Type = "外门"; CategoryCode = "M"; }
            else if (tag.Contains("FJ")) { Type = "防火卷帘"; }
            else if (tag.Contains("D") || tag.Contains("DK")) { Type = "洞口"; }
            else if (tag.Contains("BY")) { Type = "百叶窗"; CategoryCode = "C"; }
            else if (tag.Contains("C") || tag.Contains("LC")) { Type = "普通窗"; CategoryCode = "C"; }
            else if (Regex.IsMatch(tag, @"^\d+(\.\d+)?$")) { Type = "未识别"; }
            else { Type = "门"; CategoryCode = "M"; }
        }
    }

    //public class OpenningEntity : ObserverableObject
    //{
    //    public Document Document { get; set; }
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public OpenningEntity(FamilySymbol symbol, List<FamilyInstance> instances)
    //    {
    //        Document = symbol.Document;
    //        Symbol = symbol;
    //        Instances = instances;
    //        foreach (var item in instances)
    //        {
    //            InstanceIds.Add(item.Id);
    //        }
    //        entityNum = instances.Count;
    //        entityId = symbol.Id;
    //        entityTagName = symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID).AsString();
    //        entityHeight = symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM).AsDouble() * 304.8;
    //        entityWidth = symbol.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM).AsDouble() * 304.8;
    //        //
    //        FloorInstanceCount = instances.GroupBy(fi => fi.Document.GetElement(fi.LevelId)?.Name ?? "未定义楼层").ToDictionary(g => g.Key, g => g.Count().ToString());
    //        //foreach (var instance in instances)
    //        //{
    //        //    Level level = Document.GetElement(instance.LevelId) as Level;
    //        //    if (level != null)
    //        //    {
    //        //        string levelName = level.Name;
    //        //        if (FloorInstanceCount.ContainsKey(levelName))
    //        //        {
    //        //            FloorInstanceCount[levelName]++;
    //        //        }
    //        //        else
    //        //        {
    //        //            FloorInstanceCount[levelName] = 1;
    //        //        }
    //        //    }
    //        //}
    //        //relatedViews[item.Id.IntegerValue.ToString()] = item.Name;

    //        //区别类型
    //        if (entityTagName.Contains("FM"))
    //        {
    //            entityType = "防火门";
    //            entityCategoty = "M";
    //        }
    //        else if (entityTagName.Contains("BM") || entityTagName.Contains("BH") || entityTagName.Contains("BG"))
    //        {
    //            entityType = "人防门";
    //            entityCategoty = "M";
    //        }
    //        else if (entityTagName.Contains("WM"))
    //        {
    //            entityType = "外门";
    //            entityCategoty = "M";
    //        }
    //        else if (entityTagName.Contains("FJ"))
    //        {
    //            entityType = "防火卷帘";
    //        }
    //        else if (entityTagName.Contains("D") || entityTagName.Contains("DK"))
    //        {
    //            entityType = "洞口";
    //        }
    //        else if (entityTagName.Contains("BY") || entityTagName.Contains("BYC"))
    //        {
    //            entityType = "百叶窗";
    //            entityCategoty = "C";
    //        }
    //        else if (entityTagName.Contains("C") || entityTagName.Contains("LC"))
    //        {
    //            entityType = "普通窗";
    //            entityCategoty = "C";
    //        }
    //        else if (Regex.IsMatch(entityTagName, @"^\d+(\.\d+)?$"))
    //        {
    //            entityType = "未识别";
    //        }
    //        else
    //        {
    //            entityType = "门";
    //            entityCategoty = "M";
    //        }
    //        //检测是否匹配
    //        if (entityTagName == entityName)
    //        {
    //            isMatching = true;
    //        }
    //        // 宽度分类
    //        if (entityWidth > 0 && entityWidth < 900)
    //        {
    //            entityWidthType = "A";
    //        }
    //        else if (entityWidth >= 900 && entityWidth < 2100)
    //        {
    //            entityWidthType = "B";
    //        }
    //        else if (entityWidth >= 2100 && entityWidth < 2700)
    //        {
    //            entityWidthType = "C";
    //        }
    //        else if (entityWidth == 0 && entityWidth == 0)
    //        {
    //            entityWidthType = "0";
    //        }
    //        else
    //        {
    //            entityWidthType = "X"; // 其他情况
    //        }
    //    }
    //    public string entityCategoty { get; set; }
    //    public string entityWidthType { get; set; }
    //    public double entityWidth { get; set; }
    //    public double entityHeight { get; set; }
    //    public bool isMatching { get; set; } = false;
    //    public string entityTagName { get; set; }
    //    public string entityType { get; set; }
    //    public Dictionary<string, string> FloorInstanceCount { get; set; } = new Dictionary<string, string>();
    //    public int entityNum { get; set; } = 0;
    //    public string entityName
    //    {
    //        get => Symbol.Name;
    //        set
    //        {
    //            _externalHandler.Run(app =>
    //            {
    //                Document.NewTransaction(() => Symbol.Name = value, "修改名称");
    //                OnPropertyChanged();
    //            });
    //        }
    //    }
    //    public ElementId entityId { get; set; }
    //    public List<FamilyInstance> Instances { get; set; } = new List<FamilyInstance>();
    //    public List<ElementId> InstanceIds { get; set; } = new List<ElementId>();
    //    public FamilySymbol Symbol { get; set; }
    //}
}
