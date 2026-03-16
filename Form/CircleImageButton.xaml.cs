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
    public partial class CircleImageButton : UserControl
    {
        public CircleImageButton()
        {
            InitializeComponent();
        }
        // 1. 暴露 Click 事件
        public event RoutedEventHandler Click;
        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
        // 2. 图片源 依赖属性
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(CircleImageButton), new PropertyMetadata(null));
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }
        // 3. Command 依赖属性 (为了支持MVVM)
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CircleImageButton), new PropertyMetadata(null));
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(CircleImageButton), new PropertyMetadata(null));
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }
        // 文本依赖属性
        public static readonly DependencyProperty Content =
            DependencyProperty.Register("Content", typeof(ImageSource), typeof(CircleImageButton), new PropertyMetadata(null));
    }
}
