using Autodesk.Revit.DB;
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

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PatternCreateView.xaml 的交互逻辑
    /// </summary>
    public partial class PatternCreateView : Window
    {
        public PatternCreationParams Parameters { get; private set; }
        public PatternCreateView()
        {
            InitializeComponent();
        }
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PatternNameBox.Text))
            {
                MessageBox.Show("请输入图案名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(SpacingBox.Text, out var spacing) || spacing <= 0)
            {
                MessageBox.Show("请输入有效的间距值（大于0）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double.TryParse(AngleBox.Text, out var angle);

            Parameters = new PatternCreationParams
            {
                Name = PatternNameBox.Text,
                LineSpacing = spacing,
                LineAngle = angle,
                Target = FillPatternTarget.Drafting,
                Orientation = FillPatternHostOrientation.ToHost
            };

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
