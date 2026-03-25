using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace CreatePipe.Obselete
{
    /// <summary>
    /// PipeSelectFromSelectionView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSelectFromSelectionView : Window
    {
        public PipeSelectFromSelectionViewModel ViewModel => (PipeSelectFromSelectionViewModel)DataContext;
        public List<string> Strings = new List<string>();
        public PipeSelectFromSelectionView(List<Pipe> pipes)
        {
            InitializeComponent();
            this.DataContext = new PipeSelectFromSelectionViewModel(pipes);
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
    public class PipeSelectFromSelectionViewModel : ObserverableObject
    {
        Document Document { get; set; }
        public List<Pipe> AllPipes { get; set; } = new List<Pipe>();
        public PipeSelectFromSelectionViewModel(List<Pipe> pipes)
        {
            if (pipes == null || pipes.Count == 0)
                throw new ArgumentException("管道集合不能为空", nameof(pipes));
            Document = pipes.First().Document;
            AllPipes = pipes;
            // 收集系统名称
            PipeSystemNames = GetPipeSystemNames();
            SelectedPipeSystem = string.Join("，", PipeSystemNames);
            // 收集管径列表
            DNList = GetDNList();
            // 初始化UI绑定项
            Items = DNList;
        }
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
        private List<string> items = new List<string>();
        public List<string> Items
        {
            get => items;
            set
            {
                items = value;
                OnPropertyChanged();
            }
        }
        private List<string> pipeSystemNames = new List<string>();
        public List<string> PipeSystemNames
        {
            get => pipeSystemNames;
            set
            {
                pipeSystemNames = value;
                OnPropertyChanged();
            }
        }
        private List<string> GetDNList()
        {
            HashSet<int> diameters = new HashSet<int>();

            foreach (var pipe in AllPipes)
            {
                var param = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (param != null && param.HasValue)
                {
                    string valueStr = param.AsValueString();
                    if (string.IsNullOrWhiteSpace(valueStr)) continue;

                    // 正则提取数字
                    Match match = Regex.Match(valueStr, @"(\d+)");
                    if (match.Success)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int num))
                            diameters.Add(num);
                    }
                }
            }

            // 排序
            var sorted = diameters.OrderBy(d => d).ToList();
            // 转成带单位的字符串
            return sorted.Select(d => $"{d} mm").ToList();
        }
        public string SelectedPipeSystem { get; set; }
        /// <summary>
        /// 从选中的管道集合中提取所有系统名称（去重）
        /// </summary>
        private List<string> GetPipeSystemNames()
        {
            HashSet<string> systemNames = new HashSet<string>();

            foreach (var pipe in AllPipes)
            {
                var systemParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                if (systemParam != null && systemParam.HasValue)
                {
                    string name = systemParam.AsValueString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        systemNames.Add(name);
                    }
                }
            }
            return systemNames.OrderBy(n => n).ToList(); // 排序方便看
        }

        public List<string> DNList { get; set; }
    }
}
