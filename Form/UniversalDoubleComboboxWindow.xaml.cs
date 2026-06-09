using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalDoubleComboboxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalDoubleComboboxWindow : Window, INotifyPropertyChanged
    {
        // 核心数据源 (非泛型字典，可以接收任何类型的 Dictionary)
        private readonly IDictionary _dataMap;
        public UniversalDoubleComboboxWindow(
            string windowTitle, string header1, string header2, IDictionary dataMap)
        {
            InitializeComponent();

            // 1. 初始化基础显示数据
            WindowTitle = windowTitle;
            Header1 = header1;
            Header2 = header2;
            _dataMap = dataMap;

            // 2. 将数据上下文绑定到窗口自身
            this.DataContext = this;

            // 3. 默认选中第一项以触发联动
            if (ItemsSource1 != null)
            {
                // 用反射/Linq获取第一项
                var firstItem = ItemsSource1.Cast<object>().FirstOrDefault();
                SelectedItem1 = firstItem;
            }
        }
        // --- 绑定的属性 ---
        public string WindowTitle { get; set; }
        public string Header1 { get; set; }
        public string Header2 { get; set; }
        // 数据源1：直接提取字典的 Keys
        public IEnumerable ItemsSource1 => _dataMap.Keys;
        // 选中项1（带联动逻辑）
        private object _selectedItem1;
        public object SelectedItem1
        {
            get => _selectedItem1;
            set
            {
                if (_selectedItem1 != value)
                {
                    _selectedItem1 = value;
                    OnPropertyChanged();

                    // 【核心联动】当项1改变时，从字典提取对应的值作为数据源2
                    if (_selectedItem1 != null && _dataMap.Contains(_selectedItem1))
                    {
                        ItemsSource2 = _dataMap[_selectedItem1] as IEnumerable;

                        // 默认选中下拉框2的第一项
                        SelectedItem2 = ItemsSource2?.Cast<object>().FirstOrDefault();
                    }
                }
            }
        }
        // 数据源2
        private IEnumerable _itemsSource2;
        public IEnumerable ItemsSource2
        {
            get => _itemsSource2;
            set { _itemsSource2 = value; OnPropertyChanged(); }
        }
        // 选中项2
        private object _selectedItem2;
        public object SelectedItem2
        {
            get => _selectedItem2;
            set { _selectedItem2 = value; OnPropertyChanged(); }
        }
        // --- 按钮事件 ---
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // 返回成功
            this.Close();
        }
        // --- 属性更改通知 ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
