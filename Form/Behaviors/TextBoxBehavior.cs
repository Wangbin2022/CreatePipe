using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CreatePipe.Form.Behaviors
{
    public static class TextBoxBehavior
    {
        private static readonly Dictionary<TextBox, DispatcherTimer> _timers = new Dictionary<TextBox, DispatcherTimer>();
        public static readonly DependencyProperty TextChangedCommandProperty =
            DependencyProperty.RegisterAttached(
                "TextChangedCommand",
                typeof(ICommand),
                typeof(TextBoxBehavior),
                new PropertyMetadata(null, OnTextChangedCommandChanged));
        private static void OnTextChangedCommandChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= OnTextChanged;
                if (e.NewValue != null)
                {
                    textBox.TextChanged += OnTextChanged;
                }
            }
        }
        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var command = GetTextChangedCommand(textBox);

            if (_timers.TryGetValue(textBox, out var timer))
            {
                timer.Stop();
            }
            else
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    if (command?.CanExecute(textBox.Text) == true)
                    {
                        command.Execute(textBox.Text);
                    }
                };
                _timers[textBox] = timer;
            }
            timer.Start();
        }
        public static void SetTextChangedCommand(TextBox element, ICommand value) =>
            element.SetValue(TextChangedCommandProperty, value);

        public static ICommand GetTextChangedCommand(TextBox element) =>
            (ICommand)element.GetValue(TextChangedCommandProperty);
    }
}

