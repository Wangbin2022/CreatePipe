using System;
using System.Collections.Generic;
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
    /// CoordinateSystemInputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateSystemInputDialog : Window, INotifyPropertyChanged
    {
        private string _inputText;
        public string TitleText { get; private set; }
        public string Prompt { get; private set; }
        public string InputText
        {
            get { return _inputText; }
            set { _inputText = value; OnPropertyChanged(); }
        }
        public CoordinateSystemInputDialog(string title, string prompt, string defaultValue)
        {
            InitializeComponent();
            DataContext = this;
            TitleText = title;
            Prompt = prompt;
            InputText = defaultValue;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
