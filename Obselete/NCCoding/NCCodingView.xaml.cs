using Autodesk.Revit.UI;
using CreatePipe.NCCoding;
using System.Windows;

namespace CreatePipe.Obselete.NCCoding
{
    /// <summary>
    /// NCCodingView.xaml 的交互逻辑
    /// </summary>
    public partial class NCCodingView : Window
    {
        public NCCodingView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new NCCodingViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
