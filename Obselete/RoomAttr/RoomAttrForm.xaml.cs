using Autodesk.Revit.UI;
using System.Windows;

namespace CreatePipe.RoomAttr
{
    /// <summary>
    /// RoomAttrForm.xaml 的交互逻辑
    /// </summary>
    public partial class RoomAttrForm : Window
    {
        public RoomAttrForm(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new RoomAttrViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
