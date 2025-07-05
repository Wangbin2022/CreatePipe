using CreatePipe.cmd;
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
    /// OpenningManagerSubView.xaml 的交互逻辑
    /// </summary>
    public partial class OpenningManagerSubView : Window
    {
        public OpenningManagerSubView(OpenningEntity entity)
        {
            InitializeComponent();
            this.DataContext=new OpenningManagerSubViewModel(entity);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class OpenningManagerSubViewModel : ObserverableObject
    {
        public OpenningManagerSubViewModel(OpenningEntity entity)
        {
            FloorInstanceCount=entity.FloorInstanceCount;
        }
        public Dictionary<string, int> FloorInstanceCount { get; set; } = new Dictionary<string, int>();
    }
}
