using Autodesk.Revit.DB;
using System.Windows;

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
