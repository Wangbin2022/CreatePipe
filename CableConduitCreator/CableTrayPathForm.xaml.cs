using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.CableConduitCreator
{
    /// <summary>
    /// CableTrayPathForm.xaml 的交互逻辑
    /// </summary>
    public partial class CableTrayPathForm : Window
    {
        List<PathListVM> vms = new List<PathListVM>();
        public bool isReview { get; set; } = false;
        public bool isDraw { get; set; } = false;
        public bool isDirectClose { get; set; } = false;

        public CableTrayPathForm(List<PathListVM> pathVMS)
        {
            InitializeComponent();
            vms = pathVMS;
            this.DataContext = vms;
        }
        private void ls_Path_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void btn_preview_Click(object sender, RoutedEventArgs e)
        {
            isReview = true;
            this.Close();
        }
        private void btn_Draw_Click(object sender, RoutedEventArgs e)
        {
            isDraw = true;
            this.Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.isReview)
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
            }
            else if (isDraw) { }
            else isDirectClose = true;
        }
    }
}
