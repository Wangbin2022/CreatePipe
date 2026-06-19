using System.Windows;
using System.Windows.Controls.Primitives;

namespace CreatePipe.Form.UserControls
{
    public class DrawerButton : ToggleButton
    {
        // 静态构造函数，用于覆盖默认样式
        static DrawerButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DrawerButton),
                new FrameworkPropertyMetadata(typeof(DrawerButton)));
        }

        // 依赖属性：用于放置抽屉弹出的内容
        public static readonly DependencyProperty DrawerContentProperty =
            DependencyProperty.Register(
                "DrawerContent",
                typeof(object),
                typeof(DrawerButton),
                new PropertyMetadata(null));

        public object DrawerContent
        {
            get { return GetValue(DrawerContentProperty); }
            set { SetValue(DrawerContentProperty, value); }
        }
    }
}
