using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Utils.UserControls.LabelComboBox
{

    //    <!--Dictionary for style-->
    //<UserControl.Resources>
    //    <ResourceDictionary>
    //        <ResourceDictionary.MergedDictionaries>
    //            <ResourceDictionary Source = "..\..\ResourceDictionaries\DictionaryWindows.xaml" />
    //        </ ResourceDictionary.MergedDictionaries >
    //    </ ResourceDictionary >
    //</ UserControl.Resources >

    //< !--Container-- >
    //< StackPanel DataContext="{Binding ElementName=comboSelection}">
    //    <!--Title-->
    //    <TextBlock Style = "{StaticResource Title}"
    //               Text="{Binding Path=Label}"/>
    //    <!--ComboBox-->
    //    <ComboBox Style = "{StaticResource comboDisplay}"
    //              ItemsSource="{Binding Value}"
    //              SelectedItem="{Binding SelectedComboItemCategories}"/>
    //</StackPanel>



    /// <summary>
    /// UCComboBox.xaml 的交互逻辑
    /// </summary>
    /// <summary>
    /// Interaction logic for UCComboBox.xaml
    /// </summary>
    public partial class UCComboBox : UserControl
    {
        /// <summary>
        /// Get or Set text in TextBlock
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Identify the Label dependency property
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string),
              typeof(UCComboBox), new PropertyMetadata(""));

        /// <summary>
        /// Get or Set value displayed in TextBox
        /// </summary>
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Identify the Value dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object),
              typeof(UCComboBox), new PropertyMetadata(null));

        public UCComboBox()
        {
            InitializeComponent();
        }
    }
}
