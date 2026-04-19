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
    }
}
