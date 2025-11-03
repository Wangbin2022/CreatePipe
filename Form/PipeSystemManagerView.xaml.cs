using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// PipeSystemManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class PipeSystemManagerView : Window
    {
        Document Doc;
        public PipeSystemManagerView(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            InitializeComponent();
            this.DataContext = new PipeSystemViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {
            //PipeSystemViewModel viewModel = new PipeSystemViewModel(Doc);
            //viewModel.AddElement(Doc);
            this.Close();
        }
    }
}
