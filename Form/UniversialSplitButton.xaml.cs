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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversialSplitButton.xaml 的交互逻辑
    /// </summary>
    public partial class UniversialSplitButton : UserControl
    {
        public UniversialSplitButton()
        {
            InitializeComponent();
        }
        // 依赖属性定义

        // 主按钮内容
        public static readonly DependencyProperty MainContentProperty =
            DependencyProperty.Register("MainContent", typeof(object), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public object MainContent
        {
            get { return GetValue(MainContentProperty); }
            set { SetValue(MainContentProperty, value); }
        }

        // 辅助按钮内容
        public static readonly DependencyProperty SecondaryContentProperty =
            DependencyProperty.Register("SecondaryContent", typeof(object), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public object SecondaryContent
        {
            get { return GetValue(SecondaryContentProperty); }
            set { SetValue(SecondaryContentProperty, value); }
        }

        // 主按钮命令
        public static readonly DependencyProperty MainCommandProperty =
            DependencyProperty.Register("MainCommand", typeof(ICommand), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public ICommand MainCommand
        {
            get { return (ICommand)GetValue(MainCommandProperty); }
            set { SetValue(MainCommandProperty, value); }
        }

        // 主按钮命令参数
        public static readonly DependencyProperty MainCommandParameterProperty =
            DependencyProperty.Register("MainCommandParameter", typeof(object), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public object MainCommandParameter
        {
            get { return GetValue(MainCommandParameterProperty); }
            set { SetValue(MainCommandParameterProperty, value); }
        }

        // 辅助按钮命令
        public static readonly DependencyProperty SecondaryCommandProperty =
            DependencyProperty.Register("SecondaryCommand", typeof(ICommand), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public ICommand SecondaryCommand
        {
            get { return (ICommand)GetValue(SecondaryCommandProperty); }
            set { SetValue(SecondaryCommandProperty, value); }
        }

        // 辅助按钮命令参数
        public static readonly DependencyProperty SecondaryCommandParameterProperty =
            DependencyProperty.Register("SecondaryCommandParameter", typeof(object), typeof(UniversialSplitButton),
                new PropertyMetadata(null));

        public object SecondaryCommandParameter
        {
            get { return GetValue(SecondaryCommandParameterProperty); }
            set { SetValue(SecondaryCommandParameterProperty, value); }
        }

        // 悬停颜色
        public static readonly DependencyProperty HoverColorProperty =
            DependencyProperty.Register("HoverColor", typeof(Brush), typeof(UniversialSplitButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(30, 0, 0, 0))));

        public Brush HoverColor
        {
            get { return (Brush)GetValue(HoverColorProperty); }
            set { SetValue(HoverColorProperty, value); }
        }

        // 按下颜色
        public static readonly DependencyProperty PressedColorProperty =
            DependencyProperty.Register("PressedColor", typeof(Brush), typeof(UniversialSplitButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(50, 0, 0, 0))));

        public Brush PressedColor
        {
            get { return (Brush)GetValue(PressedColorProperty); }
            set { SetValue(PressedColorProperty, value); }
        }

        // 分割线颜色
        public static readonly DependencyProperty DividerColorProperty =
            DependencyProperty.Register("DividerColor", typeof(Brush), typeof(UniversialSplitButton),
                new PropertyMetadata(new SolidColorBrush(Colors.Gray)));

        public Brush DividerColor
        {
            get { return (Brush)GetValue(DividerColorProperty); }
            set { SetValue(DividerColorProperty, value); }
        }

        // 圆角半径
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(UniversialSplitButton),
                new PropertyMetadata(new CornerRadius(3)));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        // 是否显示分割线
        public static readonly DependencyProperty ShowDividerProperty =
            DependencyProperty.Register("ShowDivider", typeof(Visibility), typeof(UniversialSplitButton),
                new PropertyMetadata(Visibility.Visible));

        public Visibility ShowDivider
        {
            get { return (Visibility)GetValue(ShowDividerProperty); }
            set { SetValue(ShowDividerProperty, value); }
        }
    }
}
