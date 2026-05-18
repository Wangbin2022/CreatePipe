using System.Windows;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ExtensibleStorageDataView.xaml 的交互逻辑
    /// </summary>
    public partial class ExtensibleStorageDataView : Window
    {
        public ExtensibleStorageDataView()
        {
            InitializeComponent();
        }
        public void SetData(string data)
        {
            this.m_tb_Data.Text = data;
        }
    }
}
