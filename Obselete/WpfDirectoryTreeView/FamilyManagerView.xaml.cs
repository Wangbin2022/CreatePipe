using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.WpfDirectoryTreeView
{
    /// <summary>
    /// FileList2Csv.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyManagerView : Window
    {
        public FamilyManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new FamilyManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (directoryTreeView.SelectedItem != null)
                {
                    var selectedDirectory = directoryTreeView.SelectedItem as Dirs;
                    string output = selectedDirectory.Info.FullName;
                    // 将字符串复制到剪贴板
                    Clipboard.SetText(output);
                    Clipboard.Flush(); // 确保数据写入剪贴板
                }
            }
            catch (System.Exception)
            {
            }
            this.Close();
        }
    }
}
