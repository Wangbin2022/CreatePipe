using CreatePipe.cmd;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CircuitEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CircuitEditWindow : Window
    {
        public CircuitEditWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 电路编辑ViewModel - 添加/移除设备
    /// </summary>
    public class EditCircuitViewModel : INotifyPropertyChanged
    {
        private EditOptionType _selectedOption = EditOptionType.Add;
        private bool _isProcessing;

        public EditOptionType SelectedOption
        {
            get => _selectedOption;
            set { _selectedOption = value; OnPropertyChanged(); }
        }

        public bool IsAddSelected
        {
            get => _selectedOption == EditOptionType.Add;
            set { if (value) SelectedOption = EditOptionType.Add; }
        }

        public bool IsRemoveSelected
        {
            get => _selectedOption == EditOptionType.Remove;
            set { if (value) SelectedOption = EditOptionType.Remove; }
        }

        public bool IsSelectPanelSelected
        {
            get => _selectedOption == EditOptionType.SelectPanel;
            set { if (value) SelectedOption = EditOptionType.SelectPanel; }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event System.Action<EditOptionType> EditOptionConfirmed;

        public EditCircuitViewModel()
        {
            OkCommand = new BaseBindingCommand(_ => OnOk());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnOk()
        {
            EditOptionConfirmed?.Invoke(_selectedOption);
            CloseWindow?.Invoke();
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
