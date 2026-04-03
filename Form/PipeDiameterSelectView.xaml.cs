using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.cmd;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// PipeDiameterSelectView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeDiameterSelectView : Window
    {
        //20260328 参考Gemini改后改善传值
        public PipeSelectViewModel ViewModel => (PipeSelectViewModel)DataContext;
        public List<string> Strings = new List<string>();
        public PipeDiameterSelectView(Pipe pipe)
        {
            InitializeComponent();
            this.DataContext = new PipeSelectViewModel(pipe);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedItems != null && ViewModel.SelectedItems.Count > 0)
            {
                Strings = ViewModel.SelectedItems;
                DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }
        }
    }
    public class PipeSelectViewModel : ObserverableObject
    {
        Document Document { get; set; }
        public PipeSelectViewModel(Pipe pipe)
        {
            Document = pipe.Document;
            // 1. 直接获取系统类型 ID（超级快，不要再去 Collector 里匹配 Name 了）
            Parameter systemParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            ElementId sysTypeId = systemParam.AsElementId();
            if (sysTypeId == ElementId.InvalidElementId) return;
            // 获取系统名称用于展示
            PipingSystemType pipingSystem = Document.GetElement(sysTypeId) as PipingSystemType;
            SelectedPipeSystem = pipingSystem?.Name;
            // 2. 传递系统 ID 去获取管径列表
            dNList = getDNList(sysTypeId);
            items = dNList;
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

        private List<string> dNList;
        public List<string> DNList { get => dNList; set => dNList = value; }

        // 参数改为接收 ElementId
        private List<string> getDNList(ElementId sysTypeId)
        {
            // 【核心修复】：使用 ElementId 过滤！
            ElementParameterFilter filter = new ElementParameterFilter(
                ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), sysTypeId));
            IList<Element> pipes = new FilteredElementCollector(Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            // HashSet 自动去重数字
            HashSet<int> uniqueNumbers = new HashSet<int>();
            foreach (var p in pipes)
            {
                string pipeDN = p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString();
                Match match = Regex.Match(pipeDN ?? "", @"(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    uniqueNumbers.Add(number);
                }
            }
            // 排序并格式化输出
            List<int> sortedNumbers = uniqueNumbers.ToList();
            sortedNumbers.Sort();
            List<string> strings = new List<string>();
            foreach (var num in sortedNumbers)
            {
                strings.Add(num + " mm");
            }
            return strings;
        }
    }
}
