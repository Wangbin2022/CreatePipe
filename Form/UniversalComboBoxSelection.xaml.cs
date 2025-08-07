using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalComboBoxSelection.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalComboBoxSelection : Window
    {
        private readonly Action<string> _onSelectionConfirmed;
        public ComboboxStringViewModel ViewModel => (ComboboxStringViewModel)DataContext;
        public UniversalComboBoxSelection(List<string> collection, string prompt, Action<string> onSelectionConfirmed)
        {
            InitializeComponent();
            _onSelectionConfirmed = onSelectionConfirmed ?? throw new ArgumentNullException(nameof(onSelectionConfirmed));
            DataContext = new ComboboxStringViewModel(collection, prompt);
            ViewModel.SelectionConfirmed += OnViewModelSelectionConfirmed;
        }
        private void OnViewModelSelectionConfirmed(string selectedValue)
        {
            if (string.IsNullOrEmpty(selectedValue))
            {
                DialogResult = false;
                Close();
                return;
            }
            _onSelectionConfirmed.Invoke(selectedValue);
            if (IsModal)
            {
                DialogResult = true;
                Close();
            }
        }
        public bool IsModal { get; set; } = true;
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResultExportCommand.Execute(null);
        }
        protected override void OnClosed(EventArgs e)
        {
            ViewModel.SelectionConfirmed -= OnViewModelSelectionConfirmed;
            base.OnClosed(e);
        }
    }
    public class ComboboxStringViewModel : ObserverableObject
    {
        private bool _isCommandRunning;
        public bool IsCommandRunning
        {
            get => _isCommandRunning;
            set => SetProperty(ref _isCommandRunning, value);
        }
        //bak
        public event Action<string> SelectionConfirmed;
        private string _selectName;
        public string SelectName
        {
            get => _selectName;
            set => SetProperty(ref _selectName, value);
        }
        public ObservableCollection<string> datasource { get; } = new ObservableCollection<string>();
        public ComboboxStringViewModel(List<string> collection, string prompt)
        {
            collection.ForEach(x => datasource.Add(x));
            DisplayText = prompt;
            if (datasource.Count > 0)
            {
                SelectName = datasource[0];
            }
        }
        ////bak
        //public ICommand ResultExportCommand => new BaseBindingCommand(ResultExport);
        //private void ResultExport(object obj)
        //{
        //    if (!string.IsNullOrEmpty(SelectName))
        //    {
        //        SelectionConfirmed?.Invoke(SelectName);
        //    }
        //}
        public ICommand ResultExportCommand => new BaseBindingCommand(ResultExport, CanExecuteExport);
        private bool CanExecuteExport(object parameter)
        {
            return !IsCommandRunning; // 命令未运行时才能点击
        }
        private void ResultExport(object parameter)
        {
            IsCommandRunning = true; // 开始命令
            SelectionConfirmed?.Invoke(SelectName); // 触发外部逻辑
        }
        //bak
        public void SetCommandCompleted()
        {
            IsCommandRunning = false; // 命令完成
        }
        public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
        public bool IsValid = true;
    }
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }
}
