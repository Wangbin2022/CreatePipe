using CreatePipe.models;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// ProgressBarDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarDialog : Window
    {
        public ProgressBarDialog()
        {
            InitializeComponent();
            this.DataContext = new ProgressBarViewModel();
        }
    }
}
