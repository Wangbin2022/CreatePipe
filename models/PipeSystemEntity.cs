using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;

namespace CreatePipe.models
{
    public class PipeSystemEntity : ObserverableObject
    {
        public PipingSystemType pipingSystemType { get; set; }
        public Document Document { get => pipingSystemType.Document; }
        public PipeSystemEntity(PipingSystemType pipingSystem)
        {
            Document document = pipingSystem.Document;
            pipingSystemType = pipingSystem;
            systemName = pipingSystem.Name;

            dNList = getDNList(pipingSystem);
            //diameterNominals = getDNList(pipingSystem);
        }
        //private ObservableCollection<DiameterNominal> getDNList(PipingSystemType pipingSystem)
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
            //ObservableCollection<DiameterNominal> sortedList = new ObservableCollection<DiameterNominal>();
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
                //sortedList.Add(new DiameterNominal(pipingSystem) { DN = withModule });
                strings.Add(withModule);
            }
            //sortedList.Add(new DiameterNominal(pipingSystem) { DN = "All" });
            return strings;
        }
        //private ObservableCollection<DiameterNominal> diameterNominals = new ObservableCollection<DiameterNominal>();
        //public ObservableCollection<DiameterNominal> DiameterNominals
        //{
        //    get => diameterNominals;
        //    set
        //    {
        //        diameterNominals = value;
        //        OnPropertyChanged();
        //    }
        //}
        private List<string> dNList;
        public List<string> DNList { get => dNList; set => dNList = value; }
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


    }
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
