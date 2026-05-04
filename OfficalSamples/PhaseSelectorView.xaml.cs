using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PhaseSelectorView.xaml 的交互逻辑
    /// </summary>
    public partial class PhaseSelectorView : Window
    {
        public PhaseSelectorView(Document document)
        {
            InitializeComponent();
            var viewModel = new PhaseSelectorViewModel(document);
            viewModel.CloseWindow = Close;
            DataContext = viewModel;
        }
        /// <summary>
        /// 获取选中的元素集合（供命令使用）
        /// </summary>
        public ElementSet GetSelectedElements()
        {
            return ((PhaseSelectorViewModel)DataContext).SelectedElements;
        }
    }
    /// <summary>
    /// 主窗口ViewModel - 管理阶段选择和构件筛选
    /// </summary>
    public class PhaseSelectorViewModel : ObserverableObject
    {
        private readonly Document _document;
        private PhaseType _selectedPhaseType = PhaseType.Created;
        private ObservableCollection<PhaseItemViewModel> _phases;
        private bool _isProcessing;

        public ObservableCollection<PhaseItemViewModel> Phases
        {
            get => _phases;
            set { _phases = value; OnPropertyChanged(); }
        }

        public PhaseType SelectedPhaseType
        {
            get => _selectedPhaseType;
            set { _selectedPhaseType = value; OnPropertyChanged(); }
        }

        public bool IsCreatedSelected
        {
            get => _selectedPhaseType == PhaseType.Created;
            set { if (value) SelectedPhaseType = PhaseType.Created; }
        }

        public bool IsDemolishedSelected
        {
            get => _selectedPhaseType == PhaseType.Demolished;
            set { if (value) SelectedPhaseType = PhaseType.Demolished; }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }

        /// <summary>
        /// 选中元素集合，供外部使用
        /// </summary>
        public ElementSet SelectedElements { get; } = new ElementSet();

        public PhaseSelectorViewModel(Document document)
        {
            _document = document;
            LoadPhases();

            OkCommand = new BaseBindingCommand(_ => ExecuteSelection(), _ => !IsProcessing);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            SelectAllCommand = new BaseBindingCommand(_ => SetAllSelected(true));
            DeselectAllCommand = new BaseBindingCommand(_ => SetAllSelected(false));
        }

        /// <summary>
        /// 加载所有阶段 - 使用LINQ简化
        /// </summary>
        private void LoadPhases()
        {
            Phases = new ObservableCollection<PhaseItemViewModel>(
                from Phase phase in _document.Phases
                select new PhaseItemViewModel(phase.Name, phase.Id)
            );
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        private void SetAllSelected(bool selected)
        {
            foreach (var phase in Phases)
            {
                phase.IsSelected = selected;
            }
        }

        /// <summary>
        /// 执行筛选 - 获取满足条件的构件
        /// </summary>
        private void ExecuteSelection()
        {
            IsProcessing = true;
            SelectedElements.Clear();

            try
            {
                // 获取选中的阶段ID集合
                var selectedPhaseIds = Phases
                    .Where(p => p.IsSelected)
                    .Select(p => p.Id)
                    .ToHashSet();

                if (selectedPhaseIds.Count == 0)
                {
                    TaskDialog.Show("提示", "请至少选择一个阶段。");
                    return;
                }

                // 使用过滤器收集非类型元素
                var elementFilter = new ElementIsElementTypeFilter(true);
                var collector = new FilteredElementCollector(_document);
                var elements = collector.WherePasses(elementFilter).ToElements();

                foreach (Element element in elements)
                {
                    if (MatchesSelectedPhase(element, selectedPhaseIds))
                    {
                        SelectedElements.Insert(element);
                    }
                }

                // 显示结果
                var message = SelectedElements.Size > 0
                    ? $"找到 {SelectedElements.Size} 个符合条件的构件"
                    : "未找到符合条件的构件";

                TaskDialog.Show("完成", message);
                CloseWindow?.Invoke();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 检查元素是否匹配选中的阶段 - 使用switch表达式
        /// </summary>
        private bool MatchesSelectedPhase(Element element, HashSet<ElementId> selectedPhaseIds)
        {
            ElementId phaseId = _selectedPhaseType == PhaseType.Created
                ? element.CreatedPhaseId
                : element.DemolishedPhaseId;

            if (phaseId == null || phaseId == ElementId.InvalidElementId)
                return false;

            var phase = _document.GetElement(phaseId) as Phase;
            return phase != null && selectedPhaseIds.Contains(phase.Id);
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    /// <summary>
    /// 阶段类型枚举
    /// </summary>
    public enum PhaseType
    {
        Created,    // 创建阶段
        Demolished  // 拆除阶段
    }

    /// <summary>
    /// 阶段项ViewModel - 用于列表显示和选择
    /// </summary>
    public class PhaseItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; }
        public ElementId Id { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public PhaseItemViewModel(string name, ElementId id)
        {
            Name = name;
            Id = id;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
