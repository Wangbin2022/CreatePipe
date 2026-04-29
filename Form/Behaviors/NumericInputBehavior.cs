using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace CreatePipe.Form.Behaviors
{
    //20260426KIMI实现用例
    //    <Window xmlns:local="clr-namespace:YourNamespace">
    //    <StackPanel>
    //        <!-- 纯整数 -->
    //        <TextBox local:NumericInputBehavior.IsNumericEnabled="True"/>
    //        <!-- 允许小数 -->
    //        <TextBox local:NumericInputBehavior.IsNumericEnabled="True"
    //                 local:NumericInputBehavior.AllowDecimal="True"/>
    //        <!-- 允许负数和小数 -->
    //        <TextBox local:NumericInputBehavior.IsNumericEnabled="True"
    //                 local:NumericInputBehavior.AllowDecimal="True"
    //                 local:NumericInputBehavior.AllowNegative="True"/>
    //    </StackPanel>
    //</Window>
    /// <summary>
    /// 数字输入附加行为 - 无需 Interactivity 框架
    /// </summary>
    public static class NumericInputBehavior
    {
        // 是否启用数字输入限制
        public static readonly DependencyProperty IsNumericEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsNumericEnabled",
                typeof(bool),
                typeof(NumericInputBehavior),
                new PropertyMetadata(false, OnIsNumericEnabledChanged));

        // 是否允许小数点
        public static readonly DependencyProperty AllowDecimalProperty =
            DependencyProperty.RegisterAttached(
                "AllowDecimal",
                typeof(bool),
                typeof(NumericInputBehavior),
                new PropertyMetadata(false));

        // 是否允许负数
        public static readonly DependencyProperty AllowNegativeProperty =
            DependencyProperty.RegisterAttached(
                "AllowNegative",
                typeof(bool),
                typeof(NumericInputBehavior),
                new PropertyMetadata(false));

        // 存储订阅信息以便卸载
        private static readonly Dictionary<TextBox, NumericHandlerInfo> _handlers = new Dictionary<TextBox, NumericHandlerInfo>();

        private class NumericHandlerInfo
        {
            public TextBox TextBox { get; set; }
            public bool AllowDecimal { get; set; }
            public bool AllowNegative { get; set; }
        }

        private static void OnIsNumericEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // 清理旧订阅
                if (_handlers.TryGetValue(textBox, out var oldInfo))
                {
                    Unsubscribe(textBox);
                    _handlers.Remove(textBox);
                }

                if ((bool)e.NewValue)
                {
                    // 创建新订阅
                    var info = new NumericHandlerInfo
                    {
                        TextBox = textBox,
                        AllowDecimal = GetAllowDecimal(textBox),
                        AllowNegative = GetAllowNegative(textBox)
                    };

                    Subscribe(textBox, info);
                    _handlers[textBox] = info;
                }
            }
        }

        private static void Subscribe(TextBox textBox, NumericHandlerInfo info)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(textBox, OnPaste);

            // 监听附加属性变化
            DependencyPropertyDescriptor.FromProperty(AllowDecimalProperty, typeof(TextBox))
                .AddValueChanged(textBox, OnAllowPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(AllowNegativeProperty, typeof(TextBox))
                .AddValueChanged(textBox, OnAllowPropertyChanged);
        }

        private static void Unsubscribe(TextBox textBox)
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, OnPaste);

            DependencyPropertyDescriptor.FromProperty(AllowDecimalProperty, typeof(TextBox))
                .RemoveValueChanged(textBox, OnAllowPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(AllowNegativeProperty, typeof(TextBox))
                .RemoveValueChanged(textBox, OnAllowPropertyChanged);
        }

        private static void OnAllowPropertyChanged(object sender, EventArgs e)
        {
            if (sender is TextBox textBox && _handlers.TryGetValue(textBox, out var info))
            {
                info.AllowDecimal = GetAllowDecimal(textBox);
                info.AllowNegative = GetAllowNegative(textBox);
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_handlers.TryGetValue(textBox, out var info))
                return;

            // 检查每个输入字符
            foreach (char c in e.Text)
            {
                if (!IsValidCharacter(c, textBox.Text, textBox.CaretIndex, info))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_handlers.TryGetValue(textBox, out var info))
                return;

            // 处理功能键
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Home || e.Key == Key.End)
            {
                return; // 允许这些键
            }

            // 处理小数点键（键盘小数点或句号）
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                if (!info.AllowDecimal || textBox.Text.Contains("."))
                {
                    e.Handled = true;
                }
                return;
            }

            // 处理负号
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                if (!info.AllowNegative || textBox.CaretIndex != 0 || textBox.Text.StartsWith("-"))
                {
                    e.Handled = true;
                }
                return;
            }

            // 阻止空格
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_handlers.TryGetValue(textBox, out var info))
                return;

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsValidString(text, textBox.Text, textBox.CaretIndex, info))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsValidCharacter(char c, string currentText, int caretIndex, NumericHandlerInfo info)
        {
            if (char.IsDigit(c)) return true;

            if (c == '.' || c == '。')
            {
                return info.AllowDecimal && !currentText.Contains(".");
            }

            if (c == '-')
            {
                return info.AllowNegative && caretIndex == 0 && !currentText.StartsWith("-");
            }

            return false;
        }

        private static bool IsValidString(string input, string currentText, int caretIndex, NumericHandlerInfo info)
        {
            string prospectiveText = currentText.Insert(caretIndex, input);

            // 尝试解析
            if (info.AllowDecimal)
            {
                if (info.AllowNegative)
                {
                    return decimal.TryParse(prospectiveText, out _);
                }
                else
                {
                    return decimal.TryParse(prospectiveText, out decimal result) && result >= 0;
                }
            }
            else
            {
                if (info.AllowNegative)
                {
                    return int.TryParse(prospectiveText, out _);
                }
                else
                {
                    return int.TryParse(prospectiveText, out int result) && result >= 0;
                }
            }
        }

        // Get/Set 方法
        public static bool GetIsNumericEnabled(TextBox element) =>
            (bool)element.GetValue(IsNumericEnabledProperty);

        public static void SetIsNumericEnabled(TextBox element, bool value) =>
            element.SetValue(IsNumericEnabledProperty, value);

        public static bool GetAllowDecimal(TextBox element) =>
            (bool)element.GetValue(AllowDecimalProperty);

        public static void SetAllowDecimal(TextBox element, bool value) =>
            element.SetValue(AllowDecimalProperty, value);

        public static bool GetAllowNegative(TextBox element) =>
            (bool)element.GetValue(AllowNegativeProperty);

        public static void SetAllowNegative(TextBox element, bool value) =>
            element.SetValue(AllowNegativeProperty, value);
    }
}
