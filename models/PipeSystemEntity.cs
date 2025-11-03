using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class PipeSystemEntity : ObserverableObject
    {
        public PipingSystemType pipingSystemType { get; set; }
        public Document Document { get => pipingSystemType.Document; }
        public IEnumerable<object> selectedElements { get; set; }
        public PipeSystemEntity(PipingSystemType pipingSystem)
        {
            Document document = pipingSystem.Document;
            if (pipingSystem != null)
            {
                pipingSystemType = pipingSystem;
                systemName = pipingSystem.Name;
                abbreviation = pipingSystem.Abbreviation;
                colorName = GetColorValue(pipingSystem);
                lineColor = pipingSystem.LineColor;
                lineWeight = pipingSystem.LineWeight;
                linePatternElementInfos = LinePatterns();
                linePatternElem = linePatternElementInfos.Find(x => x.Id == pipingSystem.LinePatternId);
                mEPSystemClass = pipingSystem.SystemClassification.ToString();
                MEPSystemClassOrigin = pipingSystem.SystemClassification;
                selectedElements = ElementCount(pipingSystem);
                singleSystemElementCount = selectedElements.Count();
                Material = GetPipeMaterial(pipingSystem);
                _colorValue = GetColorValue(Material.Color);

                dNList = getDNList(pipingSystem);
            }
        }
        private List<string> dNList;
        public List<string> DNList { get => dNList; set => dNList = value; }
        private List<string> getDNList(PipingSystemType pipingSystem)
        {
            ElementParameterFilter filter = new ElementParameterFilter
                (ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), pipingSystem.Name, false));
            IList<Element> pipes = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            //按管径尺寸进行排序
            HashSet<string> pipeNames = new HashSet<string>();
            List<int> numbers = new List<int>();
            List<string> strings = new List<string>();
            foreach (var c in pipes)
            {
                string pipeDN = c.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString();
                pipeNames.Add(pipeDN);
            }
            foreach (var item in pipeNames)
            {
                string numberAsString = item.Substring(0, item.Length - 3);
                numbers.Add(int.Parse(numberAsString));
            }
            numbers.Sort();
            foreach (var item in numbers)
            {
                string withModule = item + " mm";
                strings.Add(withModule);
            }
            return strings;
        }
        private Material GetPipeMaterial(PipingSystemType pipingSystem)
        {
            //ElementId id = pipingSystem.MaterialId;
            //if (id.IntegerValue == -1)
            //{
            //    FilteredElementCollector collector = new FilteredElementCollector(pipingSystem.Document).OfCategory(BuiltInCategory.OST_Materials);
            //    List<Material> materials = collector.Cast<Material>().ToList();
            //    ElementId materialId = materials.FirstOrDefault().Id;
            //    Document.NewTransaction(() => pipingSystem.MaterialId = materialId, "默认材质赋值");
            //}
            Material material = Document.GetElement(pipingSystem.MaterialId) as Material;
            return material;
        }
        private List<LinePatternElement> LinePatterns()
        {
            FilteredElementCollector elements3 = new FilteredElementCollector(Document);
            List<LinePatternElement> LinePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            return LinePatternElements;
        }
        private List<ElementId> ElementCount(PipingSystemType entity)
        {
            List<ElementId> elementIdResult = new List<ElementId>();
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), entity.Name, false));
            // 获取所有管道和族实例的 ElementId 并添加到结果列表中
            elementIdResult.AddRange(new FilteredElementCollector(Document)
                .WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter).Select(e => e.Id));
            elementIdResult.AddRange(new FilteredElementCollector(Document)
                .WhereElementIsNotElementType().OfClass(typeof(FamilyInstance))
                .WherePasses(filter).Select(e => e.Id));
            return elementIdResult;
        }
        private int singleSystemElementCount;
        public int SingleSystemElementCount { get => singleSystemElementCount; set => singleSystemElementCount = value; }
        public string Name
        {
            get => Material.Name;
            set
            {
                Document.NewTransaction(() => Material.Name = value, "修改名称");
                OnPropertyChanged();
            }
        }
        public Color Color
        {
            get => Material.Color;
            set
            {
                Material.Color = value;
                OnPropertyChanged();
            }
        }
        private string _colorValue;
        public string ColorValue
        {
            get => _colorValue;
            set
            {
                _colorValue = value;
            }
        }
        public Material Material { get; set; }
        public string GetColorValue(Color color)
        {
            string colorvalue;
            colorvalue = color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
            return colorvalue;
        }
        public MEPSystemClassification MEPSystemClassOrigin { get; }

        private string mEPSystemClass;
        public string MEPSystemClass
        {
            get
            {
                // 假设mEPSystemClass的值是某种枚举或特定的字符串，你可以根据这些值来返回对应的中文
                switch (mEPSystemClass)
                {
                    case "Sanitary":
                        return "卫生设备";
                    case "Vent":
                        return "通气管道";
                    case "SupplyHydronic":
                        return "循环供水";
                    case "ReturnHydronic":
                        return "循环回水";
                    case "DomesticHotWater":
                        return "家用热水";
                    case "DomesticColdWater":
                        return "家用冷水";
                    case "FireProtectWet":
                        return "湿式消防";
                    case "FireProtectDry":
                        return "干式消防";
                    case "FireProtectPreaction":
                        return "预作消防";
                    case "FireProtectOther":
                        return "其他消防";
                    default:
                        return "其他管道";
                }
            }
        }
        private Autodesk.Revit.DB.Color lineColor;
        public Autodesk.Revit.DB.Color LineColor
        {
            get => lineColor;
            set
            {
                lineColor = value;
                //Document.NewTransaction(() => pipingSystemType.LineColor = value, "修改线颜色");
                OnPropertyChanged("LineColor");
            }
        }
        private string colorName;
        public string ColorName
        {
            get => colorName;
            set
            {
                colorName = value;
                //colorName = GetColorValue(pipingSystemType.LineColor);
                OnPropertyChanged("ColorName");
            }
        }
        private LinePatternElement linePatternElem;
        public LinePatternElement LinePatternElem
        {
            get => linePatternElem;
            set
            {
                Document.NewTransaction(() => pipingSystemType.LinePatternId = value.Id, "修改线型");
                OnPropertyChanged("LinePatternElem");
            }
        }
        private List<LinePatternElement> linePatternElementInfos;
        public List<LinePatternElement> LinePatternElementInfos
        {
            get => linePatternElementInfos;
            set => linePatternElementInfos = value;
        }
        public List<int> LineWeights
        {
            get
            {
                List<int> ints = new List<int>();
                for (int i = 1; i <= 16; i++)
                {
                    ints.Add(i);
                }
                return ints;
            }
            set { LineWeights = value; }
        }
        private int lineWeight;
        public int LineWeight
        {
            get { return lineWeight; }
            set
            {
                Document.NewTransaction(() => pipingSystemType.LineWeight = value, "修改线宽");
                OnPropertyChanged("LineWeight");
            }
        }
        private string systemName;
        public string SystemName
        {
            get { return systemName; }
            set
            {
                Document.NewTransaction(() => pipingSystemType.Name = value, "修改名称");
                systemName = value;
                OnPropertyChanged("SystemName");
            }
        }
        private string abbreviation;
        public string Abbreviation
        {
            get { return abbreviation; }
            set
            {
                Document.NewTransaction(() => pipingSystemType.Abbreviation = value, "修改缩写");
                abbreviation = value;
                OnPropertyChanged();
            }
        }
        public string GetColorValue(PipingSystemType systemType)
        {
            Autodesk.Revit.DB.Color color = systemType.LineColor;
            //if (color == null || !color.IsValid)//不少模板没有给管道系统线颜色预制值，只能提前赋值否则报错崩溃
            //{
            //    Document.NewTransaction(() => systemType.LineColor = new Autodesk.Revit.DB.Color(0, 0, 0), "修改线颜色");
            //    return null;
            //}
            //else
            //{
            try
            {
                string colorvalue = color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
                //string colorvalue = Convert.ToString(color.Red) + "-" + Convert.ToString(color.Green) + "-" + Convert.ToString(color.Blue);
                //string colorvalue = $"{color.Red}-{color.Green}-{color.Blue}";
                //string colorvalue = String.Concat(color.Red.ToString(), "-", color.Green.ToString(), "-", color.Blue.ToString());
                //string colorvalue = string.Format("{0}-{1}-{2}", color.Red, color.Green, color.Blue);
                return colorvalue;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.ToString());
            }
            //}
            return null;
        }
    }
    //public class PipeSystemEntity : ObserverableObject
    //{
    //    public PipingSystemType pipingSystemType { get; set; }
    //    public Document Document { get => pipingSystemType.Document; }
    //    public PipeSystemEntity(PipingSystemType pipingSystem)
    //    {
    //        Document document = pipingSystem.Document;
    //        pipingSystemType = pipingSystem;
    //        systemName = pipingSystem.Name;

    //        dNList = getDNList(pipingSystem);
    //        //diameterNominals = getDNList(pipingSystem);
    //    }
    //    //private ObservableCollection<DiameterNominal> getDNList(PipingSystemType pipingSystem)
    //    private List<string> getDNList(PipingSystemType pipingSystem)
    //    {
    //        ElementParameterFilter filter = new ElementParameterFilter
    //            (ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), pipingSystem.Name, false));
    //        IList<Element> pipes = new FilteredElementCollector(Document)
    //            .WhereElementIsNotElementType()
    //            .OfCategory(BuiltInCategory.OST_PipeCurves)
    //            .WherePasses(filter)
    //            .ToElements();
    //        //按管径尺寸进行排序
    //        HashSet<string> pipeNames = new HashSet<string>();
    //        List<int> numbers = new List<int>();
    //        List<string> strings = new List<string>();
    //        //ObservableCollection<DiameterNominal> sortedList = new ObservableCollection<DiameterNominal>();
    //        foreach (var c in pipes)
    //        {
    //            string pipeDN = c.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString();
    //            pipeNames.Add(pipeDN);
    //        }
    //        foreach (var item in pipeNames)
    //        {
    //            string numberAsString = item.Substring(0, item.Length - 3);
    //            numbers.Add(int.Parse(numberAsString));
    //        }
    //        numbers.Sort();
    //        foreach (var item in numbers)
    //        {
    //            string withModule = item + " mm";
    //            //sortedList.Add(new DiameterNominal(pipingSystem) { DN = withModule });
    //            strings.Add(withModule);
    //        }
    //        //sortedList.Add(new DiameterNominal(pipingSystem) { DN = "All" });
    //        return strings;
    //    }
    //    //private ObservableCollection<DiameterNominal> diameterNominals = new ObservableCollection<DiameterNominal>();
    //    //public ObservableCollection<DiameterNominal> DiameterNominals
    //    //{
    //    //    get => diameterNominals;
    //    //    set
    //    //    {
    //    //        diameterNominals = value;
    //    //        OnPropertyChanged();
    //    //    }
    //    //}
    //    private List<string> dNList;
    //    public List<string> DNList { get => dNList; set => dNList = value; }
    //    private string systemName;
    //    public string SystemName
    //    {
    //        get { return systemName; }
    //        set
    //        {
    //            Document.NewTransaction(() => pipingSystemType.Name = value, "修改名称");
    //            systemName = value;
    //            OnPropertyChanged("SystemName");
    //        }
    //    }


    //}
    //public class DiameterNominal : ObserverableObject
    //{
    //    public DiameterNominal(PipingSystemType pipeSystem)
    //    {
    //        PipeSystemName = pipeSystem.Name;
    //    }
    //    public string PipeSystemName { get; set; }
    //    public string DN { get; set; }
    //    public bool IsChecked
    //    {
    //        get => _isChecked;
    //        set
    //        {
    //            if (_isChecked != value)
    //            {
    //                _isChecked = value;
    //                OnPropertyChanged();
    //            }
    //        }
    //    }
    //    private bool _isChecked;
    //}
}
