using System.ComponentModel;
using System.Windows;

namespace CreatePipe.CountParkingLot
{
    /// <summary>
    /// NumberByPickWPF.xaml 的交互逻辑
    /// </summary>
    public partial class NumberByPickWPF : Window, INotifyPropertyChanged
    {
        private int _selectedCount = 0;

        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                if (_selectedCount != value)
                {
                    _selectedCount = value;
                    OnPropertyChanged(nameof(SelectedCount));
                }
            }
        }
        public NumberByPickWPF()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
