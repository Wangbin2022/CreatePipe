namespace CreatePipe.Form
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    public class CustomComboBox : ComboBox
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<string>), typeof(CustomComboBox),
                new PropertyMetadata(null));

        public ObservableCollection<string> ItemsSource
        {
            get { return (ObservableCollection<string>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public CustomComboBox()
        {
            SelectionChanged += CustomComboBox_SelectionChanged;
        }

        private void CustomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedItem == "自定义")
            {
                UniversalNewString subView = new UniversalNewString("提示：请输入主文件名");
                if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName)) return;
                ComboBoxHelper.AddCustomItem(ItemsSource, vm.NewName);
                SelectedItem = vm.NewName;
            }
        }
    }
    public static class ComboBoxHelper
    {
        public static void AddCustomItem(ObservableCollection<string> items, string newItem)
        {
            if (!string.IsNullOrWhiteSpace(newItem) && !items.Contains(newItem))
            {
                items.Add(newItem);
            }
        }
        public static void ShowCustomItemDialog(ObservableCollection<string> items, string selectedItem)
        {
            if (selectedItem == "自定义")
            {
                UniversalNewString subView = new UniversalNewString("提示：请输入主文件名");
                if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName)) return;
                AddCustomItem(items, vm.NewName);
            }
        }
    }
}
