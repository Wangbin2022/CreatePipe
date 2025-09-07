using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form.Behaviors
{
    public class ValidationErrorToViewModelBehavior : Behavior<TextBox>
    {
        public object ViewModel { get; set; }
        public string HasErrorPropertyName { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Validation.ErrorEvent, new RoutedEventHandler(OnValidationError));
        }

        private void OnValidationError(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || string.IsNullOrEmpty(HasErrorPropertyName)) return;

            var errorEvent = (ValidationErrorEventArgs)e;
            var hasError = errorEvent.Action == ValidationErrorEventAction.Added;

            var prop = ViewModel.GetType().GetProperty(HasErrorPropertyName);
            prop?.SetValue(ViewModel, hasError);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(Validation.ErrorEvent, new RoutedEventHandler(OnValidationError));
            base.OnDetaching();
        }
    }
}
