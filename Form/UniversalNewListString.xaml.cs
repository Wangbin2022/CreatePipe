using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalNewListString.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalNewListString : Window
    {
        public NewListStringViewModel ViewModel => (NewListStringViewModel)DataContext;
        public UniversalNewListString(string prompt, string optionalText = "")
        {
            InitializeComponent();
            DataContext = new NewListStringViewModel(prompt, optionalText);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsValid && ViewModel.NewName.Count > 0)
            {
                DialogResult = true;
                //this.Close();
            }
        }
    }
    public class NewListStringViewModel : ObserverableObject
    {
        private List<string> _newName = new List<string>();
        public List<string> NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }
        private string _inputName;
        public string InputText
        {
            get => _inputName;
            set
            {
                if (_inputName == value) return; // 防止重复触发
                SetProperty(ref _inputName, value);

                // 【关键】：只要文本发生变化，就实时解析并验证
                var tempItems = StringParseHelper.ParseAndRemoveDuplicates(value);

                if (tempItems.Count > 0)
                {
                    NewName = tempItems;          // 缓存解析结果
                    IsValid = true;               // 【解锁按钮】
                    DisplayText = _originalPrompt;// 恢复正常提示
                }
                else
                {
                    NewName.Clear();
                    IsValid = false;              // 【禁用按钮】
                    DisplayText = string.IsNullOrWhiteSpace(value) ? _originalPrompt : "输入无效或包含重复/空项！";
                }
            }
        }
        public string DisplayText { get; set; } = "提示：其它属性请建立后自行更改";
        private bool _isValid = false;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }
        private string _originalPrompt;
        public NewListStringViewModel(string prompt, string optionalText = "")
        {
            _originalPrompt = prompt;
            DisplayText = prompt;
        }
    }
    public static class StringParseHelper
    {
        /// <summary>
        /// 将输入的字符串按分号分割，去除空格，过滤空值，并移除重复项
        /// </summary>
        public static List<string> ParseAndRemoveDuplicates(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();
            return input.Split(new char[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(item => item.Trim())
                        .Where(item => !string.IsNullOrEmpty(item)).Distinct().ToList();
        }
    }
}
