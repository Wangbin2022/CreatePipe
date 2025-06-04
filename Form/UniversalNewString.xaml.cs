using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalNewString.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalNewString : Window
    {
        public NewStringViewModel ViewModel => (NewStringViewModel)DataContext;
        public UniversalNewString(string prompt)
        {
            InitializeComponent();
            DataContext = new NewStringViewModel(prompt);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewName != null)
            {
                DialogResult = true;
                this.Close();
            }
        }
    }
    public class NewStringViewModel : ObserverableObject
    {
        private string _newName;
        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }
        public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
        public bool IsValid = true;
        public NewStringViewModel(string prompt)
        {
            DisplayText = prompt;
        }
    }
}