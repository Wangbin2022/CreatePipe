using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CircuitSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CircuitSelectWindow : Window
    {
        public CircuitSelectWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 电路选择ViewModel - 当有多个电路时显示选择列表
    /// </summary>
    public class SelectCircuitViewModel : ObserverableObject
    {
        private readonly CircuitOperationData _operationData;
        private ElectricalSystemItem _selectedCircuit;

        public ObservableCollection<ElectricalSystemItem> Circuits { get; }

        public ElectricalSystemItem SelectedCircuit
        {
            get => _selectedCircuit;
            set { _selectedCircuit = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanConfirm)); }
        }

        public bool CanConfirm => _selectedCircuit != null;

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ShowCircuitCommand { get; }

        public event System.Action<ElectricalSystemItem> CircuitSelected;

        public SelectCircuitViewModel(CircuitOperationData operationData)
        {
            _operationData = operationData;

            Circuits = new ObservableCollection<ElectricalSystemItem>(
                _operationData.ElectricalSystemItems.Select(item => new ElectricalSystemItem
                {
                    Name = item.Name,
                    Id = item.Id,
                    ElectricalSystem = item.ElectricalSystem
                })
            );

            ConfirmCommand = new BaseBindingCommand(_ => OnConfirm());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            ShowCircuitCommand = new BaseBindingCommand(_ => OnShowCircuit());
        }

        private void OnConfirm()
        {
            if (SelectedCircuit != null)
            {
                CircuitSelected?.Invoke(SelectedCircuit);
                CloseWindow?.Invoke();
            }
        }

        private void OnShowCircuit()
        {
            if (SelectedCircuit != null)
            {
                _operationData.ShowCircuit(SelectedCircuit.Id.IntegerValue);
            }
        }

        public Action CloseWindow { get; set; }

    }
}
