using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
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
    /// DuctSystemManagerSubView.xaml 的交互逻辑
    /// </summary>
    public partial class DuctSystemManagerSubView : Window
    {
        public DuctSystemManagerSubView(Object para, UIDocument uiDoc)
        {
            InitializeComponent();
            this.DataContext = new DuctSystemManagerSubViewModel(para, uiDoc);
        }
        public void SelectTab(string tabName)
        {
            switch (tabName)
            {
                case "EditTab":
                    SubTabControl.SelectedItem = EditTab;
                    break;
                case "NewTab":
                    SubTabControl.SelectedItem = NewTab;
                    break;
                default:
                    throw new ArgumentException("Invalid tab name", nameof(tabName));
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DuctSystemManagerSubViewModel vm)
            {
                //pipeSystemEntity = vm.PipeSystem;
                DialogResult = true;
            }
            this.Close();
        }
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null && comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = comboBox.Items.Count - 1;
            }
        }
    }
    public class DuctSystemManagerSubViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public List<MechanicalSystemType> DuctSystems { get; set; } = new List<MechanicalSystemType>();
        public DuctSystemManagerSubViewModel(object parameter, UIDocument uiDoc)
        {
            Document = uiDoc.Document;
            DuctSystems = new FilteredElementCollector(Document).OfClass(typeof(MechanicalSystemType)).Cast<MechanicalSystemType>().ToList();
            switch (parameter)
            {
                case DuctSystemEntity entity:
                    InitializeForEditMaterial(entity);
                    break;
                case string tabName when tabName == "NewTab":
                    InitializeForNewMaterial();
                    break;
                default:
                    throw new ArgumentException($"Unsupported parameter type: {parameter.GetType().Name}", nameof(parameter));
            }
        }
        //新建管道系统部分
        private void InitializeForNewMaterial()
        {
            HashSet<string> ductSystemsSet = new HashSet<string>(); // 使用 HashSet 来避免重复
            foreach (var item in DuctSystems)
            {
                MEPSystemClass = item.SystemClassification.ToString();
                // Add 方法会检查重复，如果元素已存在，则不会添加
                ductSystemsSet.Add(MEPSystemClass);
            }
            DuctSystemClass = ductSystemsSet.ToList();
            Abbreviation = string.Empty;
        }
        public ICommand NewSystemCommand => new BaseBindingCommand(NewDuctSystem);
        private void NewDuctSystem(Object para)
        {
            List<string> systenNames = new List<string>();
            List<string> systenAbbreviations = new List<string>();
            foreach (MechanicalSystemType ductSystem in DuctSystems)
            {
                systenNames.Add(ductSystem.Name);
                systenAbbreviations.Add(ductSystem.Abbreviation);
            }
            if (string.IsNullOrEmpty(systemName))
            {
                TaskDialog.Show("tt", "风管系统名称不能为空。");
                return;
            }
            // 检查 systenNames 是否已经包含 systemName
            if (systenNames.Contains(systemName))
            {
                TaskDialog.Show("tt", "名称与现有风管系统重复，请修改");
                return;
            }
            Document.NewTransaction(() =>
            {
                if (mEPSystemClass != null)
                {
                    MEPSystemClassification classification;
                    switch (mEPSystemClass)
                    {
                        case "送风风管":
                            classification = MEPSystemClassification.SupplyAir;
                            break;
                        case "回风风管":
                            classification = MEPSystemClassification.ReturnAir;
                            break;
                        case "排风风管":
                            classification = MEPSystemClassification.ExhaustAir;
                            break;
                        default:
                            classification = MEPSystemClassification.OtherAir;
                            break;
                    }
                    MechanicalSystemType ductSystem = MechanicalSystemType.Create(Document, classification, SystemName);
                    ductSystem.Abbreviation = Abbreviation;
                    ductSystem.LineColor = new Autodesk.Revit.DB.Color(128, 128, 128);
                    ductSystem.LineWeight = 12;
                    var defaultMaterial = new FilteredElementCollector(ductSystem.Document).OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList().FirstOrDefault();
                    ductSystem.MaterialId = defaultMaterial.Id;
                }
            }, "新建风管系统");
            TaskDialog.Show("tt", $"已新建风管系统：“{systemName}”");
        }
        public List<string> DuctSystemClass { get; set; }
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
                    case "送风风管":
                        return "送风风管";
                    case "回风风管":
                        return "回风风管";
                    case "排风风管":
                        return "排风风管";
                    default:
                        return "其他风管";
                }
            }
            set
            {
                mEPSystemClass = value;
                OnPropertyChanged("MEPSystemClass");
            }
        }
        private string systemName = "新风管系统";
        public string SystemName { get => systemName; set => SetProperty(ref systemName, value); }
        public string Abbreviation { get; set; }
        //添加保温部分
        public DuctSystemEntity DuctSystem { get; set; }
        private void InitializeForEditMaterial(DuctSystemEntity entity)
        {
            DuctSystem = entity;
            SelectedDuctSystem = entity.SystemName;
            FilteredElementCollector elems = new FilteredElementCollector(Document)
       .WhereElementIsElementType()
       .OfCategory(BuiltInCategory.OST_DuctInsulations);
            foreach (Element item in elems)
            {
                _insulations.Add(item.Name);
                insulationIDs.Add(item.Id);
            }
        }
        public List<Element> ducts = new List<Element>();
        public List<Element> ductfittings = new List<Element>();
        public List<ElementId> insulationIDs = new List<ElementId>();
        public ElementId insulationID;
        public ICommand AddInsulationCommand => new RelayCommand<string>(AddInsulation);
        public void AddInsulation(string ductSystem)
        {
            if (DuctSystem == null)
            {
                TaskDialog.Show("tt", "未选中要添加保温的风管类型，请正确选取");
                return;
            }
            if (thickness == 0)
            {
                TaskDialog.Show("Error", "请输入保温层厚度");
                return;
            }
            GetInstancesFunc(ductSystem);
            GetInsulationID();
            double thick = thickness / 304.8;
            AddInsulationFunc(thick);
        }
        public void GetInsulationID()
        {
            foreach (var item in insulationIDs)
            {
                if (Document.GetElement(item).Name == Insulation)
                {
                    insulationID = item;
                }
            }
        }
        public void GetInstancesFunc(string ductSystem)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory
   .CreateEqualsRule(new ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM), ductSystem, false));
            IList<Element> allpipes = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_DuctCurves)
                .WherePasses(filter)
                .ToElements();
            foreach (Element p in allpipes)
            {
                ducts.Add(p);
            }
            IList<Element> allfittings = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_DuctFitting)
                .WherePasses(filter)
                .ToElements();
            if (allfittings.Count == 0)
            {
                // 处理没有找到任何管道配件的情况
                return;
            }
            foreach (Element p in allfittings)
            {
                ductfittings.Add(p);
            }
        }
        public void AddInsulationFunc(double thickness)
        {
            try
            {
                using (Transaction t = new Transaction(Document))
                {
                    t.Start("Add Insulation to ducts");
                    List<ElementId> dinsidtodelete = new List<ElementId>();
                    List<ElementId> dtoreinsulate = new List<ElementId>();
                    foreach (Duct d in ducts)
                    {
                        var ins = DuctInsulation.GetInsulationIds(Document, d.Id);
                        if (ins.Count() == 0)
                        {
                            DuctInsulation ductInsulation = DuctInsulation.Create(Document, d.Id, insulationID, thickness);

                        }
                        else
                        {
                            foreach (var f in DuctInsulation.GetInsulationIds(Document, d.Id))
                            {
                                dinsidtodelete.Add(f);
                            }
                            dtoreinsulate.Add(d.Id);
                        }
                    }
                    t.Commit();

                    t.Start("Add Insulation to duct fittings");
                    foreach (var d in ductfittings)
                    {
                        var ins = DuctInsulation.GetInsulationIds(Document, d.Id);
                        if (ins.Count() == 0)
                        {
                            DuctInsulation ductInsulation = DuctInsulation.Create(Document, d.Id, insulationID, thickness);
                        }
                        else
                        {
                            foreach (var f in DuctInsulation.GetInsulationIds(Document, d.Id))
                            {
                                dinsidtodelete.Add(f);
                            }
                            dtoreinsulate.Add(d.Id);
                        }
                    }
                    t.Commit();

                    if (dinsidtodelete.Count > 0)
                    {
                        t.Start("Override pipe insulation");
                        Document.Delete(dinsidtodelete);
                        foreach (var d in dtoreinsulate)
                        {
                            try
                            {
                                DuctInsulation ductInsulation = DuctInsulation.Create(Document, d, insulationID, thickness);
                            }
                            catch { }
                        }
                        t.Commit();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        private double thickness;
        public double Thickness { get => thickness; set => thickness = value; }
        public string Insulation { get; set; }
        private List<string> _insulations = new List<string>();
        public List<string> Insulations { get => _insulations; set => _insulations = value; }
        public string SelectedDuctSystem { get; set; }
    }
}
