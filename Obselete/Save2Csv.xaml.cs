using System.Windows;

namespace CreatePipe.WpfDirectoryTreeView
{
    /// <summary>
    /// Save2Csv.xaml 的交互逻辑
    /// </summary>
    public partial class Save2Csv : Window
    {
        public Save2Csv()
        {
            InitializeComponent();
            this.DataContext = new Save2CsvViewModel();
        }
    }
}
