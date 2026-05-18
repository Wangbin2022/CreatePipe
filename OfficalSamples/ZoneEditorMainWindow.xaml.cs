using System.Windows;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ZoneEditorMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ZoneEditorMainWindow : Window
    {
        public ZoneEditorMainWindow()
        {
            InitializeComponent();
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
