using CreatePipe.cmd;
using CreatePipe.models;
using System.Collections.Generic;
using System.Windows;

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
            this.DataContext = new OpenningManagerSubViewModel(entity);
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
            FloorInstanceCount = entity.FloorInstanceCount;
        }
        public Dictionary<string, string> FloorInstanceCount { get; set; } = new Dictionary<string, string>();
    }
}
