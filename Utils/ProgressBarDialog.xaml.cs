using CreatePipe.cmd;
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

namespace CreatePipe.Utils
{
    /// <summary>
    /// ProgressBarDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarDialog : Window
    {
        public ProgressBarDialog(ProgressBarDialogViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
    public class ProgressBarDialogViewModel : ObserverableObject
    {
        public ProgressBarDialogViewModel(int maxNum, string initialTitle, int value)
        {
            Maximum = maxNum;
            Value = value;
            Title = initialTitle;
        }
        #region 属性定义
        private int _maximum;
        public int Maximum { get => _maximum; set => SetProperty(ref _maximum, value); }
        private int _value = 0;
        public int Value { get => _value; set => SetProperty(ref _value, value); }
        private string _title;
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        #endregion

        /// <summary>
        /// 动态更新进度和标题
        /// </summary>
        public void UpdateProgress(int currentValue, string itemName)
        {
            Value = currentValue;
            // 实时拼接显示内容，例如： "10/100, 正在处理: 墙1"
            Title = $"进度: {Value}/{Maximum} - 正在处理: {itemName}";
        }
    }
}
