using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.models
{
    public class PipeSystemEntity : ObserverableObject
    {
        public PipingSystemType pipingSystemType { get; set; }
        public Document Document;
        private readonly BaseExternalHandler _handler;
        public List<ElementId> selectedElements { get; set; }
        public PipeSystemEntity(PipingSystemType pipingSystem, BaseExternalHandler handler)
        {
            Document = pipingSystem.Document;
            _handler = handler;
            if (pipingSystem != null)
            {
                pipingSystemType = pipingSystem;
                systemName = pipingSystem.Name;
                abbreviation = pipingSystem.Abbreviation;
                _lineColor = pipingSystem.LineColor;
                lineWeight = pipingSystem.LineWeight;
                linePatternElementInfos = LinePatterns();
                linePatternElem = linePatternElementInfos.Find(x => x.Id == pipingSystem.LinePatternId);
                mEPSystemClass = pipingSystem.SystemClassification.ToString();
                MEPSystemClassOrigin = pipingSystem.SystemClassification;
                selectedElements = ElementCount(pipingSystem);
                singleSystemElementCount = selectedElements.Count();
                _material = Document.GetElement(pipingSystem.MaterialId) as Material;

                _materialName=_material.Name;
                //_colorValue = GetColorValue(Material.Color);
                dNList = getDNList(pipingSystem);
                UpdateColorName();
                UpdateLineColorName();
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
        private string _materialName;
        public string MaterialName
        {
            get => _materialName;
            set
            {
                if (_materialName != value)
                {
                    var AllMaterials = new FilteredElementCollector(Document).OfClass(typeof(Material)).Cast<Material>().ToList(); 
                    Dictionary<string, Material> _nameToMaterialMap = AllMaterials.ToDictionary(m => m.Name, m => m);
                    // 获取对应的材质对象以获取 ID
                    var material = _nameToMaterialMap[value];
                    _handler.Run(app =>
                    {
                        using (Transaction t = new Transaction(Document, "修改材质"))
                        {
                            t.Start();
                            var p = pipingSystemType.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            p.Set(material.Id);
                            t.Commit();
                        }
                        _materialName = value;
                        OnPropertyChanged(nameof(Material));
                    });
                }
            }
        }
        private Material _material;
        public Material Material
        {
            get => _material;
            set
            {
                _material = value;
                OnPropertyChanged(nameof(Material));
                //if (_material?.Id != value?.Id)
                //{
                //    try
                //    {
                //        // 切换到 Revit 线程执行更新
                //        _handler.Run(app =>
                //        {
                //            NewTransaction.Execute(Document, "修改系统材质", () =>
                //            {
                //                Parameter p = pipingSystemType.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                //                if (p != null)
                //                {
                //                    p.Set(value.Id);
                //                }
                //            });
                //            _material = value;
                //            OnPropertyChanged(nameof(Material));
                //        });
                //    }
                //    catch (Exception ex)
                //    {
                //        TaskDialog.Show("tt", ex.Message);
                //    }
                //}
            }
        }
        //public string Name
        //{
        //    get => Material.Name;
        //    set
        //    {
        //        _handler.Run(app =>
        //        {
        //            Document.NewTransaction(() => Material.Name = value, "修改名称");
        //        });
        //        OnPropertyChanged();
        //    }
        //}
        //public Material Material { get; set; }
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
        //要加上动态属性才能跟随线改色？
        public Color Color
        {
            get => Material.Color;
            set
            {
                // 如果新值和旧值相同，则不执行任何操作，避免不必要的事务和UI更新
                if (Material.Color != null && Material.Color.IsValid && value != null && value.IsValid &&
                    Material.Color.Red == value.Red && Material.Color.Green == value.Green && Material.Color.Blue == value.Blue)
                {
                    return;
                }
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改材质颜色", () => Material.Color = value);
                    Material.Color = value;
                    OnPropertyChanged();
                    UpdateColorName();
                });
            }
        }
        public ICommand MaterialEditCommand => new RelayCommand<PipeSystemEntity>(MaterialEdit);
        private void MaterialEdit(PipeSystemEntity entity)
        {
            ////TaskDialog.Show("tt", entity.SystemName);
            var initialRevitColor = Material.Color;
            var initialMediaColor = System.Windows.Media.Color.FromRgb(initialRevitColor.Red, initialRevitColor.Green, initialRevitColor.Blue);
            var dialog = new ColorPickerDialog(initialMediaColor);
            if (dialog.ShowDialog() == true)
            {
                var newRevitColor = new Color(dialog.SelectedColor.R, dialog.SelectedColor.G, dialog.SelectedColor.B);
                Color = newRevitColor;
            }
        }
        private string _colorName;
        public string ColorName
        {
            get => _colorName;
            set
            {
                if (_colorName != value)
                {
                    _colorName = value;
                    OnPropertyChanged(nameof(ColorName));
                }
            }
        }
        private void UpdateColorName()
        {
            ColorName = Color != null ? $"{Color.Red}-{Color.Green}-{Color.Blue}" : "无";
        }
        private Autodesk.Revit.DB.Color _lineColor;
        public Autodesk.Revit.DB.Color LineColor
        {
            get => _lineColor;
            set
            {
                // 如果新值和旧值相同，则不执行任何操作，避免不必要的事务和UI更新
                if (_lineColor != null && _lineColor.IsValid && value != null && value.IsValid &&
                    _lineColor.Red == value.Red && _lineColor.Green == value.Green && _lineColor.Blue == value.Blue)
                {
                    return;
                }
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改线颜色", () => pipingSystemType.LineColor = value);
                    _lineColor = value;
                    OnPropertyChanged();
                    UpdateLineColorName();
                });
            }
        }
        public ICommand SetLineColorCommand => new RelayCommand<PipeSystemEntity>(SetLineColor);
        private void SetLineColor(PipeSystemEntity entity)
        {
            //TaskDialog.Show("tt", entity.SystemName);
            var initialRevitColor = entity.LineColor;
            var initialMediaColor = System.Windows.Media.Color.FromRgb(initialRevitColor.Red, initialRevitColor.Green, initialRevitColor.Blue);
            var dialog = new ColorPickerDialog(initialMediaColor);
            if (dialog.ShowDialog() == true)
            {
                var newRevitColor = new Color(dialog.SelectedColor.R, dialog.SelectedColor.G, dialog.SelectedColor.B);
                entity.LineColor = newRevitColor;
            }
        }
        private string _lineColorName;
        public string LineColorName
        {
            get => _lineColorName;
            set
            {
                _lineColorName = value;
                OnPropertyChanged(nameof(LineColorName));
            }
        }
        private void UpdateLineColorName()
        {
            LineColorName = LineColor != null ? $"{LineColor.Red}-{LineColor.Green}-{LineColor.Blue}" : "无";
        }
        private LinePatternElement linePatternElem;
        public LinePatternElement LinePatternElem
        {
            get => linePatternElem;
            set
            {
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改线型", () => pipingSystemType.LinePatternId = value.Id);
                    linePatternElem = value;
                    OnPropertyChanged();
                });
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
            get => lineWeight;
            set
            {
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改线宽", () => pipingSystemType.LineWeight = value);
                    lineWeight = value;
                });
                OnPropertyChanged();
            }
        }
        private string systemName;
        public string SystemName
        {
            get => systemName;
            set
            {
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改名称", () => pipingSystemType.Name = value);
                    systemName = value;
                });
                OnPropertyChanged();
            }
        }
        private string abbreviation;
        public string Abbreviation
        {
            get => abbreviation;
            set
            {
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改缩写", () => pipingSystemType.Abbreviation = value);
                    abbreviation = value;
                    OnPropertyChanged();
                });
            }
        }
        //public string GetColorValue(PipingSystemType systemType)
        //{
        //    Autodesk.Revit.DB.Color color = systemType.LineColor;
        //    //if (color == null || !color.IsValid)//不少模板没有给管道系统线颜色预制值，只能提前赋值否则报错崩溃
        //    //{
        //    //    Document.NewTransaction(() => systemType.LineColor = new Autodesk.Revit.DB.Color(0, 0, 0), "修改线颜色");
        //    //    return null;
        //    //}
        //    //else
        //    //{
        //    try
        //    {
        //        string colorvalue = color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
        //        //string colorvalue = Convert.ToString(color.Red) + "-" + Convert.ToString(color.Green) + "-" + Convert.ToString(color.Blue);
        //        //string colorvalue = $"{color.Red}-{color.Green}-{color.Blue}";
        //        //string colorvalue = String.Concat(color.Red.ToString(), "-", color.Green.ToString(), "-", color.Blue.ToString());
        //        //string colorvalue = string.Format("{0}-{1}-{2}", color.Red, color.Green, color.Blue);
        //        return colorvalue;
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("tt", ex.ToString());
        //    }
        //    //}
        //    return null;
        //}
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
