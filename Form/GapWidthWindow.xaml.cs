using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// GapWidthWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GapWidthWindow : Window
    {
        public double GapWidth { get; private set; }

        public GapWidthWindow()
        {
            InitializeComponent();
            WidthTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WidthTextBox.Text, out double width) && width > 0)
            {
                GapWidth = width;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("请输入一个有效的正数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
