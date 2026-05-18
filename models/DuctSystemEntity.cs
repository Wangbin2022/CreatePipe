using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Color = Autodesk.Revit.DB.Color;
using Material = Autodesk.Revit.DB.Material;

namespace CreatePipe.models
{
    public class DuctSystemEntity : ObserverableObject
    {
        public MechanicalSystemType ductSystemType { get; set; }
        public Document Document;
        private readonly BaseExternalHandler _handler;
        public List<ElementId> selectedElements { get; set; } = new List<ElementId>();
        public DuctSystemEntity(MechanicalSystemType ductSystem, BaseExternalHandler handler)
        {
            Document = ductSystem.Document;
            _handler = handler;
            if (ductSystem != null)
            {
                ductSystemType = ductSystem;
                systemName = ductSystem.Name;
                abbreviation = ductSystem.Abbreviation;

                _lineColor = ductSystem.LineColor;
                lineWeight = ductSystem.LineWeight;
                linePatternElementInfos = LinePatterns();
                linePatternElem = LinePatternElementInfos.Find(x => x.Id == ductSystem.LinePatternId);

                mEPSystemClass = ductSystem.SystemClassification.ToString();
                MEPSystemClassOrigin = ductSystem.SystemClassification;
                selectedElements = ElementCount(ductSystem);
                singleSystemElementCount = selectedElements.Count();
                if (singleSystemElementCount > 0)
                {
                    canClear = true;
                }
                _material = Document.GetElement(ductSystem.MaterialId) as Material;
                _materialName = _material.Name;
                UpdateColorName();
                UpdateLineColorName();
            }
        }
        public bool canClear { get; set; } = false;
        private List<LinePatternElement> LinePatterns()
        {
            FilteredElementCollector elements3 = new FilteredElementCollector(Document);
            List<LinePatternElement> LinePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            return LinePatternElements;
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
                            var p = ductSystemType.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
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
            }
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
        public ICommand MaterialEditCommand => new RelayCommand<DuctSystemEntity>(MaterialEdit);
        private void MaterialEdit(DuctSystemEntity entity)
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
                    NewTransaction.Execute(Document, "修改线颜色", () => ductSystemType.LineColor = value);
                    _lineColor = value;
                    OnPropertyChanged();
                    UpdateLineColorName();
                });
            }
        }
        public ICommand SetLineColorCommand => new RelayCommand<DuctSystemEntity>(SetLineColor);
        private void SetLineColor(DuctSystemEntity entity)
        {
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
                    NewTransaction.Execute(Document, "修改线型", () => ductSystemType.LinePatternId = value.Id);
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
                    NewTransaction.Execute(Document, "修改线宽", () => ductSystemType.LineWeight = value);
                    lineWeight = value;
                    OnPropertyChanged();
                });
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
                    NewTransaction.Execute(Document, "修改名称", () => ductSystemType.Name = value);
                    systemName = value;
                    OnPropertyChanged();
                });
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
                    NewTransaction.Execute(Document, "修改缩写", () => ductSystemType.Abbreviation = value);
                    abbreviation = value;
                    OnPropertyChanged();
                });
            }
        }
    }
}
