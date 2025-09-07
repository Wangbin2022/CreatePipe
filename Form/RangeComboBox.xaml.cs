using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form
{
    /// <summary>
    /// RangeComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class RangeComboBox : UserControl
    {
        public RangeComboBox()
        {
            InitializeComponent();
            RebuildItems();
        }
        #region 对外暴露的三个依赖属性
        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(RangeComboBox),
                new PropertyMetadata(0, (d, _) => ((RangeComboBox)d).RebuildItems()));

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(RangeComboBox),
                new PropertyMetadata(100, (d, _) => ((RangeComboBox)d).RebuildItems()));

        public int Step
        {
            get => (int)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(int), typeof(RangeComboBox),
                new PropertyMetadata(1, (d, _) => ((RangeComboBox)d).RebuildItems()));
        #endregion

        #region 对外暴露的选中值（双向绑定）
        public int SelectedValue
        {
            get => (int)GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(int), typeof(RangeComboBox),
                new FrameworkPropertyMetadata(0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) =>
                    {
                        var uc = (RangeComboBox)d;
                        uc.InnerCombo.SelectedValue = e.NewValue;
                    }));
        #endregion

        private void RebuildItems()
        {
            if (Step <= 0 || Minimum > Maximum) return;

            InnerCombo.ItemsSource = Enumerable
                .Range(0, (Maximum - Minimum) / Step + 1)
                .Select(i => new
                {
                    Display = (Minimum + i * Step).ToString(),
                    Value = Minimum + i * Step
                })
                .ToList();

            // 同步 SelectedValue
            InnerCombo.SelectedValue = SelectedValue;
        }

        private void InnerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InnerCombo.SelectedValue is int v)
                SelectedValue = v;
        }
    }
}
