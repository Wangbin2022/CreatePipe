using System;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form
{
    /// <summary>
    /// ProgressBarTest2.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarTest2 : Window
    {
        public ProgressBarTest2()
        {
            InitializeComponent();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            Arc.EndAngle = slider.Value * 3.6;
            tbk.Text = $"{Convert.ToInt32(slider.Value).ToString()}%";
        }
    }
}
