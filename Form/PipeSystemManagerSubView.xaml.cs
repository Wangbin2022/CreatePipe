using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
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
    /// PipeSystemManagerSubView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSystemManagerSubView : Window
    {
        public PipeSystemEntity pipeSystemEntity { get; set; }
        public PipeSystemManagerSubView(Object para, UIDocument uiDoc, BaseExternalHandler handler)
        {
            InitializeComponent();
            this.DataContext = new PipeSystemManagerSubViewModel(para, uiDoc, handler);
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
            if (DataContext is PipeSystemManagerSubViewModel vm)
            {
                pipeSystemEntity = vm.PipeSystem;
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
    public class PipeSystemManagerSubViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        private BaseExternalHandler Handler;
        public List<PipingSystemType> PipeSystems { get; set; } = new List<PipingSystemType>();
        public PipeSystemManagerSubViewModel(object parameter, UIDocument uiDoc, BaseExternalHandler handler)
        {
            Document = uiDoc.Document;
            Handler = handler;
            PipeSystems = new FilteredElementCollector(Document).OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>().ToList();
            switch (parameter)
            {
                case PipeSystemEntity entity:
                    InitializeForAddInsulation(entity);
                    break;
                case string tabName when tabName == "NewTab":
                    InitializeForNewPipingSystem();
                    break;
                default:
                    TaskDialog.Show("tt", $"Unsupported parameter type: {parameter.GetType().Name}");
                    break;
            }
        }
        //新建管道系统部分
        private void InitializeForNewPipingSystem()
        {
            HashSet<string> pipeSystemsSet = new HashSet<string>(); // 使用 HashSet 来避免重复
            foreach (var item in PipeSystems)
            {
                MEPSystemClass = item.SystemClassification.ToString();
                // Add 方法会检查重复，如果元素已存在，则不会添加
                pipeSystemsSet.Add(MEPSystemClass);
            }
            pipeSystemClass = pipeSystemsSet.ToList();
        }
        public ICommand NewSystemCommand => new BaseBindingCommand(NewPipingSystem);
        private void NewPipingSystem(Object para)
        {
            if (string.IsNullOrEmpty(systemName))
            {
                TaskDialog.Show("tt", "管道系统名称不能为空。");
                return;
            }
            if (PipeSystems.Any(p => p.Name.Equals(SystemName, StringComparison.OrdinalIgnoreCase)))
            {
                TaskDialog.Show("错误", "名称与现有管道系统重复，请修改。");
                return;
            }
            NewTransaction.Execute(Document, "新建管道系统", () =>
            {
                if (mEPSystemClass == null) return;

                MEPSystemClassification classification;
                switch (mEPSystemClass)
                {
                    case "卫生设备":
                        classification = MEPSystemClassification.Sanitary;
                        break;
                    case "通气管道":
                        classification = MEPSystemClassification.Vent;
                        break;
                    case "循环供水":
                        classification = MEPSystemClassification.SupplyHydronic;
                        break;
                    case "循环回水":
                        classification = MEPSystemClassification.ReturnHydronic;
                        break;
                    case "家用热水":
                        classification = MEPSystemClassification.DomesticHotWater;
                        break;
                    case "家用冷水":
                        classification = MEPSystemClassification.DomesticColdWater;
                        break;
                    case "湿式消防":
                        classification = MEPSystemClassification.FireProtectWet;
                        break;
                    case "干式消防":
                        classification = MEPSystemClassification.FireProtectDry;
                        break;
                    case "预作消防":
                        classification = MEPSystemClassification.FireProtectPreaction;
                        break;
                    case "其他消防":
                        classification = MEPSystemClassification.FireProtectOther;
                        break;
                    default:
                        classification = MEPSystemClassification.OtherPipe;
                        break;
                }
                PipingSystemType pipingSystem = PipingSystemType.Create(Document, classification, SystemName);
                pipingSystem.Abbreviation = Abbreviation;
                pipingSystem.LineColor = new Autodesk.Revit.DB.Color(127, 127, 127);
                pipingSystem.LineWeight = 1;
                var defaultMaterial = new FilteredElementCollector(pipingSystem.Document).OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList().FirstOrDefault();
                if (defaultMaterial != null)
                {
                    pipingSystem.MaterialId = defaultMaterial.Id;
                }
            });
            TaskDialog.Show("tt", $"已新建管道系统：“{systemName}”");
        }
        private List<string> pipeSystemClass;
        public List<string> PipeSystemClass { get => pipeSystemClass; set => pipeSystemClass = value; }
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
                    case "卫生设备":
                        return "卫生设备";
                    case "通气管道":
                        return "通气管道";
                    case "循环供水":
                        return "循环供水";
                    case "循环回水":
                        return "循环回水";
                    case "家用热水":
                        return "家用热水";
                    case "家用冷水":
                        return "家用冷水";
                    case "湿式消防":
                        return "湿式消防";
                    case "干式消防":
                        return "干式消防";
                    case "预作消防":
                        return "预作消防";
                    case "其他消防":
                        return "其他消防";
                    default:
                        return "其他管道";
                }
            }
            set
            {
                mEPSystemClass = value;
                OnPropertyChanged("MEPSystemClass");
            }
        }
        private string systemName = "新管道系统";
        public string SystemName { get => systemName; set => SetProperty(ref systemName, value); }
        public string Abbreviation { get; set; } = string.Empty;
        //添加保温部分
        public PipeSystemEntity PipeSystem { get; set; }
        private void InitializeForAddInsulation(PipeSystemEntity entity)
        {
            //ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            PipeSystem = entity;
            selectedPipeSystem = PipeSystem.SystemName;
            items = PipeSystem.DNList;
            FilteredElementCollector elems = new FilteredElementCollector(PipeSystem.Document).WhereElementIsElementType().OfCategory(BuiltInCategory.OST_PipeInsulations);
            foreach (Element item in elems)
            {
                _insulations.Add(item.Name);
                insulationIDs.Add(item.Id);
            }
            //SelectedTabIndex = 0; // 打开"修改材质"页
        }
        public List<Element> pipes = new List<Element>();
        public List<Element> pipefittings = new List<Element>();
        public string Insulation { get; set; }
        public List<ElementId> insulationIDs = new List<ElementId>();
        public ElementId insulationID;
        private List<string> _insulations = new List<string>();
        public List<string> Insulations { get => _insulations; set => _insulations = value; }
        private List<string> items = new List<string>();
        private List<string> selectedItems = new List<string>();
        public List<string> SelectedItems { get => selectedItems; set => SetProperty(ref selectedItems, value); }
        public List<string> Items { get => items; set => SetProperty(ref items, value); }
        public double Thickness { get; set; }
        public ICommand AddInsulationCommand => new RelayCommand<string>(AddInsulation);
        public void AddInsulation(string pipingSystem)
        {
            if (PipeSystem is null)
            {
                TaskDialog.Show("tt", "未选中要添加保温的管道类型，请正确选取");
                return;
            }
            if (Thickness <= 0 || SelectedItems == null)
            {
                TaskDialog.Show("tt", "保温层厚度不正确或未选中要修改的管径");
                return;
            }
            foreach (var SelectedDN in SelectedItems)
            {
                GetInstancesFunc(pipingSystem, SelectedDN);
            }
            foreach (var item in insulationIDs)
            {
                if (Document.GetElement(item).Name == Insulation)
                {
                    insulationID = item;
                }
            }
            double thick = Thickness / 304.8;
            AddInsulationFunc(thick);
        }
        public void GetInstancesFunc(string pipingSystem, string singleDN)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory
   .CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), pipingSystem, false));
            IList<Element> allpipes = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            foreach (Element p in allpipes) if (p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString() == singleDN)
                {
                    pipes.Add(p);
                }

            IList<Element> allfittings = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .WherePasses(filter)
                .ToElements();
            if (allfittings.Count == 0)
            {
                // 处理没有找到任何管道配件的情况
                return;
            }
            foreach (Element p in allfittings)
            {
                string parameterName = "公称直径"; // 参数名称
                Parameter diameterParam = p.LookupParameter(parameterName);
                if (diameterParam == null) { diameterParam = p.LookupParameter("公称直径1"); }
                // 检查参数是否存在以及是否有值
                if (diameterParam != null && diameterParam.HasValue && !string.IsNullOrEmpty(diameterParam.AsValueString()))
                {
                    string diameter = diameterParam.AsValueString();
                    if (diameter == singleDN)
                    {
                        pipefittings.Add(p);
                    }
                }
            }
        }
        private string selectedPipeSystem;
        public string SelectedPipeSystem { get => selectedPipeSystem; set => SetProperty(ref selectedPipeSystem, value); }
        List<ElementId> pinsidtodelete = new List<ElementId>();
        List<ElementId> ptoreinsulate = new List<ElementId>();
        public List<ElementId> Pinsidtodelete { get => pinsidtodelete; set => pinsidtodelete = value; }
        public List<ElementId> Ptoreinsulate { get => ptoreinsulate; set => ptoreinsulate = value; }
        public void AddInsulationFunc(double pipethickness)
        {
            try
            {
                using (Transaction ts = new Transaction(Document))
                {
                    ts.Start("Add Insulation to pipes");
                    foreach (Pipe p in pipes)
                    {
                        var ins = PipeInsulation.GetInsulationIds(Document, p.Id);
                        if (ins.Count() == 0)
                        {
                            PipeInsulation pipeInsulation = PipeInsulation.Create(Document, p.Id, insulationID, pipethickness);
                        }
                        else
                        {
                            foreach (var f in PipeInsulation.GetInsulationIds(Document, p.Id))
                            {
                                pinsidtodelete.Add(f);
                            }
                            ptoreinsulate.Add(p.Id);
                        }
                    }
                    ts.Commit();
                    if (pipefittings.Count == 0)
                    {
                        ExchangeInsulationFunc(pipethickness);
                        return;
                    }
                    else
                    {
                        ts.Start("Add Insulation to pipe fittings");
                        foreach (var p in pipefittings)
                        {
                            var ins = PipeInsulation.GetInsulationIds(Document, p.Id);
                            if (ins.Count() == 0)
                            {
                                PipeInsulation pipeInsulation = PipeInsulation.Create(Document, p.Id, insulationID, pipethickness);
                            }
                            else
                            {
                                foreach (var f in PipeInsulation.GetInsulationIds(Document, p.Id))
                                {
                                    pinsidtodelete.Add(f);
                                }
                                ptoreinsulate.Add(p.Id);
                            }
                        }
                        ts.Commit();
                        ExchangeInsulationFunc(pipethickness);
                    }
                }
                TaskDialog.Show("Success", "已增加指定管道保温!");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
        private void ExchangeInsulationFunc(double pipethickness)
        {
            try
            {
                using (Transaction ts = new Transaction(Document))
                {
                    if (pinsidtodelete.Count > 0)
                    {
                        ts.Start("Override pipe insulation");
                        Document.Delete(pinsidtodelete);
                        foreach (var p in ptoreinsulate)
                        {
                            try
                            {
                                PipeInsulation pipeInsulation = PipeInsulation.Create(Document, p, insulationID, pipethickness);
                            }
                            catch
                            {
                            }
                        }
                        ts.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
    }
}
