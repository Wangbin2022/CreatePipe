using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Collections;

namespace CreatePipe.Form
{
    public class ExtendedComboBox : ComboBox
    {
        public static readonly DependencyProperty EnableCustomItemProperty =
            DependencyProperty.Register(
                "EnableCustomItem",
                typeof(bool),
                typeof(ExtendedComboBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CustomItemPromptProperty =
            DependencyProperty.Register(
                "CustomItemPrompt",
                typeof(string),
                typeof(ExtendedComboBox),
                new PropertyMetadata("请输入自定义项"));

        public static readonly DependencyProperty CustomItemTextProperty =
            DependencyProperty.Register(
                "CustomItemText",
                typeof(string),
                typeof(ExtendedComboBox),
                new PropertyMetadata("自定义"));

        public bool EnableCustomItem
        {
            get => (bool)GetValue(EnableCustomItemProperty);
            set => SetValue(EnableCustomItemProperty, value);
        }

        public string CustomItemPrompt
        {
            get => (string)GetValue(CustomItemPromptProperty);
            set => SetValue(CustomItemPromptProperty, value);
        }

        public string CustomItemText
        {
            get => (string)GetValue(CustomItemTextProperty);
            set => SetValue(CustomItemTextProperty, value);
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (!EnableCustomItem || SelectedItem == null)
                return;

            // 检查当前选中的是否是“自定义”项
            var selectedItemName = SelectedItem.GetType().GetProperty("Name")?.GetValue(SelectedItem)?.ToString();
            if (selectedItemName != CustomItemText)
                return;

            // 弹出输入框
            var dialog = new UniversalNewString(CustomItemPrompt);
            if (dialog.ShowDialog() != true || !(dialog.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            {
                SelectedItem = null; // 恢复之前的选择
                return;
            }

            // 动态创建新对象并添加到集合
            if (ItemsSource is IList items)
            {
                // 检查是否已存在同名项
                bool exists = false;
                foreach (var item in items)
                {
                    var itemName = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString();
                    if (itemName == vm.NewName)
                    {
                        exists = true;
                        SelectedItem = item; // 选择已存在的项
                        break;
                    }
                }

                if (!exists)
                {
                    // 动态创建新对象（假设类型有 Name 属性）
                    var newItem = Activator.CreateInstance(SelectedItem.GetType());
                    var nameProperty = newItem.GetType().GetProperty("Name");
                    if (nameProperty != null)
                    {
                        nameProperty.SetValue(newItem, vm.NewName);
                        items.Add(newItem);
                        SelectedItem = newItem;
                    }
                }
            }
        }
    }
}
