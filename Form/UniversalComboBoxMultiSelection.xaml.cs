using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalComboBoxMultiSelection.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalComboBoxMultiSelection : Window
    {
        // 对外暴露的结果
        public List<string> SelectedResult { get; private set; } = new List<string>();
        public UniversalComboBoxMultiSelection(List<string> collection, string prompt)
        {
            InitializeComponent();
            var vm = new UniversalComboBoxMultiSelectionViewModel(collection, prompt);
            this.DataContext = vm;

            // 【核心】订阅 ViewModel 的关闭请求
            vm.RequestClose += (result) =>
            {
                this.SelectedResult = vm.SelectedItems.ToList();
                this.DialogResult = result;
                this.Close();
            };
        }
    }
    public class UniversalComboBoxMultiSelectionViewModel : ObserverableObject
    {
        public event Action<bool> RequestClose; // 用于通知 View 关闭
        public UniversalComboBoxMultiSelectionViewModel(List<string> collection, string prompt)
        {
            Datasource = new ObservableCollection<string>(collection);
            DisplayText = prompt;
            SelectedItems = new ObservableCollection<string>();

            // 监听集合变化，实时更新按钮可用状态
            SelectedItems.CollectionChanged += (s, e) =>
            {
                ResultExportCommand.RaiseCanExecuteChanged();
            };
        }
        private ObservableCollection<string> _selectedItems;
        public ObservableCollection<string> SelectedItems
        {
            get => _selectedItems;
            set { _selectedItems = value; OnPropertyChanged(); }
        }
        public ObservableCollection<string> Datasource { get; }
        public string DisplayText { get; set; } // 假设 prompt 不会动态变，简写即可
        private BaseBindingCommand _resultExportCommand;
        public BaseBindingCommand ResultExportCommand => _resultExportCommand ??
            (_resultExportCommand = new BaseBindingCommand(
                obj => RequestClose?.Invoke(true), // 执行逻辑：请求关闭并返回 true
                obj => SelectedItems?.Count > 0    // 验证逻辑：至少选一个
            ));
        //// 定义一个事件，用于请求关闭窗口
        //public event Action<bool> RequestClose;
        //public UniversalComboBoxMultiSelectionViewModel(List<string> collection, string prompt)
        //{
        //    // 使用 ObservableCollection 初始化数据源
        //    Datasource = new ObservableCollection<string>(collection);
        //    DisplayText = prompt;

        //    // 初始化时 SelectedItems 也应该是 ObservableCollection
        //    SelectedItems = new ObservableCollection<string>();
        //    // 订阅集合变化事件，以便在每次选择变化时都能更新命令状态
        //    SelectedItems.CollectionChanged += (s, e) => ResultExportCommand.RaiseCanExecuteChanged();
        //}
        //// 将 Command 定义为可设置的属性，并使用自定义的 BaseBindingCommand
        //// 如果您的 BaseBindingCommand 没有 RaiseCanExecuteChanged 方法，请确保它能重新评估 CanExecute
        //public BaseBindingCommand ResultExportCommand => _resultExportCommand ?? (_resultExportCommand = new BaseBindingCommand(ResultExport, CanResultExport));
        //private BaseBindingCommand _resultExportCommand;
        //private bool CanResultExport(object obj)
        //{
        //    // CanExecute 逻辑
        //    return SelectedItems?.Count > 0;
        //}
        //private void ResultExport(object obj)
        //{
        //    // 在命令中触发关闭事件，并传递 DialogResult
        //    RequestClose?.Invoke(true);
        //}
        //private ObservableCollection<string> _selectedItems;
        //public ObservableCollection<string> SelectedItems
        //{
        //    get => _selectedItems;
        //    set
        //    {
        //        _selectedItems = value;
        //        // 属性本身被替换时，也需要发出通知
        //        OnPropertyChanged(nameof(SelectedItems));
        //    }
        //}

        //// Datasource 也应为 ObservableCollection
        //public ObservableCollection<string> Datasource { get; }

        //private string _displayText;
        //public string DisplayText
        //{
        //    get => _displayText;
        //    set
        //    {
        //        _displayText = value;
        //        OnPropertyChanged(nameof(DisplayText)); // 补上通知
        //    }
        //}
    }
    //public class UniversalComboBoxMultiSelectionViewModel : ObserverableObject
    //{
    //    public UniversalComboBoxMultiSelectionViewModel(List<string> collection, string prompt)
    //    {
    //        collection.ForEach(x => Datasource.Add(x));
    //        DisplayText = prompt;
    //    }
    //    public ICommand ResultExportCommand => new BaseBindingCommand(ResultExport, canExecute: obj => SelectedItems?.Count > 0);
    //    private void ResultExport(Object obj)
    //    {
    //        //TaskDialog.Show("tt", SelectedItems.Count().ToString());
    //    }
    //    private List<string> _selectedItems = new List<string>();
    //    public List<string> SelectedItems { get => _selectedItems; set => _selectedItems = value; }
    //    public List<string> Datasource { get; } = new List<string>();
    //    public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
    //}
}
