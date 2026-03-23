using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new TestWindowViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class TestWindowViewModel : ObserverableObject
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Document _doc;
        public TestWindowViewModel(UIApplication uiApp)
        {
            _uiapp = uiApp;
            _uidoc = uiApp.ActiveUIDocument;
            _doc = _uidoc.Document;

        }
        public ICommand SaveConfigCommand => new BaseBindingCommand(SaveConfig);
        private void SaveConfig(object obj)
        {
        }
    }
}
