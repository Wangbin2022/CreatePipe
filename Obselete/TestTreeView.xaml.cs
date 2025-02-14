using System.Windows;

namespace CreatePipe.WpfDirectoryTreeView
{
    /// <summary>
    /// TestTreeView.xaml 的交互逻辑
    /// </summary>
    public partial class TestTreeView : Window
    {
        public TestTreeView()
        {
            InitializeComponent();
            this.DataContext = new TestTreeViewModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
