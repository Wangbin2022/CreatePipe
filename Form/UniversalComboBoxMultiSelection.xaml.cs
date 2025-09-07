using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalComboBoxMultiSelection.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalComboBoxMultiSelection : Window
    {
        public UniversalComboBoxMultiSelection(List<string> collection, string prompt)
        {
            InitializeComponent();
            this.DataContext = new UniversalComboBoxMultiSelectionViewModel(collection, prompt);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
    public class UniversalComboBoxMultiSelectionViewModel : ObserverableObject
    {
        public UniversalComboBoxMultiSelectionViewModel(List<string> collection, string prompt)
        {
            collection.ForEach(x => Datasource.Add(x));
            DisplayText = prompt;
        }
        public ICommand ResultExportCommand => new BaseBindingCommand(ResultExport, canExecute: obj => SelectedItems?.Count > 0);
        private void ResultExport(Object obj)
        {
            //TaskDialog.Show("tt", SelectedItems.Count().ToString());
        }
        private List<string> _selectedItems = new List<string>();
        public List<string> SelectedItems { get => _selectedItems; set => _selectedItems = value; }
        public List<string> Datasource { get; } = new List<string>();
        public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
    }
}
