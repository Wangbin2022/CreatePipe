using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class DuctSystemEntity : ObserverableObject
    {
        public MechanicalSystemType ductSystemSingle { get; set; }
        public Document Document { get => ductSystemSingle.Document; }
        public IEnumerable<object> selectedElements { get; set; }
        public DuctSystemEntity(MechanicalSystemType ductSystem)
        {
            Document document = ductSystem.Document;
            FilteredElementCollector elements3 = new FilteredElementCollector(document);
            List<LinePatternElement> LinePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();

            if (ductSystem != null)
            {
                ductSystemSingle = ductSystem;
                systemName = ductSystem.Name;
                abbreviation = ductSystem.Abbreviation;
                colorName = GetColorValue(ductSystem);
                lineColor = ductSystem.LineColor;
                lineWeight = ductSystem.LineWeight;
                linePatternElementInfos = LinePatternElements;
                linePatternElem = LinePatternElements.Find(x => x.Id == ductSystem.LinePatternId);
                mEPSystemClass = ductSystem.SystemClassification.ToString();
                MEPSystemClassOrigin = ductSystem.SystemClassification;
                //ElementId id = ductSystem.MaterialId;
                //if (id.IntegerValue == -1)
                //{
                //    FilteredElementCollector collector = new FilteredElementCollector(ductSystem.Document).OfCategory(BuiltInCategory.OST_Materials);
                //    List<Material> materials = collector.Cast<Material>().ToList();
                //    ElementId materialId = materials.FirstOrDefault().Id;
                //    Document.NewTransaction(() => ductSystem.MaterialId = materialId, "默认材质赋值");
                //}
                Material material = document.GetElement(ductSystem.MaterialId) as Material;
                Material = material;
                _colorValue = GetColorValue(material.Color);
                //统计部分能否放进VM
                //ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM), systemName, false));
                //IList<Element> allpipes = new FilteredElementCollector(ductSystem.Document)
                //    .WhereElementIsNotElementType()
                //    .OfCategory(BuiltInCategory.OST_DuctCurves)
                //    .WherePasses(filter)
                //    .ToElements();
                //IList<Element> allfamily = new FilteredElementCollector(ductSystem.Document)
                //     .WhereElementIsNotElementType()
                //     .OfClass(typeof(FamilyInstance))
                //     .WherePasses(filter)
                //     .ToElements();
                //singleSystemElementCount = allpipes.Count() + allfamily.Count();
                selectedElements = ElementCount(ductSystem);
                singleSystemElementCount = selectedElements.Count();
            }
            //List<Element> systems = new FilteredElementCollector(document)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_DuctSystem)
            //    .ToList<Element>();
            //MechanicalSystemType ductSystemList;
        }
        private List<ElementId> ElementCount(MechanicalSystemType entity)
        {
            List<ElementId> elementIdResult = new List<ElementId>();
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM), entity.Name, false));
            // 获取所有管道和族实例的 ElementId 并添加到结果列表中
            elementIdResult.AddRange(new FilteredElementCollector(Document)
                .WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_DuctCurves)
                .WherePasses(filter).Select(e => e.Id));
            elementIdResult.AddRange(new FilteredElementCollector(Document)
                .WhereElementIsNotElementType().OfClass(typeof(FamilyInstance))
                .WherePasses(filter).Select(e => e.Id));
            return elementIdResult;
        }
        //数量相关
        private int singleSystemElementCount;
        public int SingleSystemElementCount
        {
            get => singleSystemElementCount;
            set
            {
                singleSystemElementCount = value;
                OnPropertyChanged();
            }
        }
        //材料相关
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
                    case "SupplyAir":
                        return "送风风管";
                    case "ReturnAir":
                        return "回风风管";
                    case "ExhaustAir":
                        return "排风风管";
                    default:
                        return "其他风管";
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
                OnPropertyChanged("ColorName");
            }
        }
        private LinePatternElement linePatternElem;
        public LinePatternElement LinePatternElem
        {
            get => linePatternElem;
            set
            {
                Document.NewTransaction(() => ductSystemSingle.LinePatternId = value.Id, "修改线型");
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
                Document.NewTransaction(() => ductSystemSingle.LineWeight = value, "修改线宽");
                OnPropertyChanged("LineWeight");
            }
        }
        private string systemName;
        public string SystemName
        {
            get { return systemName; }
            set
            {
                Document.NewTransaction(() => ductSystemSingle.Name = value, "修改名称");
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
                Document.NewTransaction(() => ductSystemSingle.Abbreviation = value, "修改缩写");
                abbreviation = value;
                OnPropertyChanged();
            }
        }
        public string GetColorValue(MechanicalSystemType systemType)
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
}
