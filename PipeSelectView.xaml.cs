using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe
{
    /// <summary>
    /// PipeSelectView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSelectView : Window
    {
        public PipeSelectViewModel ViewModel => (PipeSelectViewModel)DataContext;
        public List<string> Strings = new List<string>();
        public PipeSelectView(Pipe pipe)
        {
            InitializeComponent();
            this.DataContext = new PipeSelectViewModel(pipe);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedItems != null)
            {
                Strings = ViewModel.SelectedItems;
                DialogResult = true;
            }
            this.Close();
        }
    }
    public class PipeSelectViewModel : ObserverableObject
    {
        Document Document { get; set; }
        public PipeSelectViewModel(Pipe pipe)
        {
            Document = pipe.Document;
            Parameter systemParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            PipingSystemType pipingSystem = new FilteredElementCollector(Document).OfClass(typeof(PipingSystemType)).OfType<PipingSystemType>()
                .FirstOrDefault(item => item.Name == systemParam.AsValueString());
            if (pipingSystem == null) return;
            SelectedPipeSystem = pipingSystem.Name;
            dNList = getDNList(pipingSystem);
            items = dNList;
        }
        public ICommand ReturnSelectionCommand => new BaseBindingCommand(ReturnSelection);
        private void ReturnSelection(object obj)
        {
            //TaskDialog.Show("tt", SelectedPipeSystem);
        }
        private List<string> items = new List<string>();
        private List<string> selectedItems = new List<string>();
        public List<string> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                OnPropertyChanged();
            }
        }
        public List<string> Items
        {
            get => items;
            set
            {
                items = value;
                OnPropertyChanged();
            }
        }
        public string SelectedPipeSystem { get; set; }
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
        private List<string> dNList;
        public List<string> DNList { get => dNList; set => dNList = value; }
    }
}
