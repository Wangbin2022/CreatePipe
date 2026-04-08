using System.Collections.Generic;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// PropertiesWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        // 构造函数接收参数列表
        public PropertiesWindow(List<RevitParameterItem> instanceParams, List<RevitParameterItem> typeParams)
        {
            InitializeComponent();
            // 直接将集合设置为 DataGrid 的数据源
            dgInstance.ItemsSource = instanceParams;
            dgType.ItemsSource = typeParams;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }

    // 参数数据模型
    public class RevitParameterItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
