using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// GuidanaceSignPlaceView.xaml 的交互逻辑
    /// </summary>
    public partial class GuidanaceSignPlaceView : Window
    {
        public GuidanaceSignPlaceView()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
