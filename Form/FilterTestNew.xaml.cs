using Autodesk.Revit.DB;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// FilterTestNew.xaml 的交互逻辑
    /// </summary>
    public partial class FilterTestNew : Window
    {
        Document Doc;

        public FilterTestNew(Document document, ref string message)
        {
            InitializeComponent();
            Doc = document;
            // 创建一个局部变量来存储 Message 的值
            string localMessage = message;
            this.DataContext = new ViewModel1127(Doc, ref localMessage);
            //this.DataContext = new ViewModelTest(Doc);
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //内部窗口之间可以这样操作实现互开关和恢复，但是TaskDialog怎么操作？
            FilterTest dialog = new FilterTest(Doc);
            //TaskDialog td = new TaskDialog("test"); 
            this.Hide();
            bool? result = dialog.ShowDialog();
            this.ShowDialog();
        }
    }
}
