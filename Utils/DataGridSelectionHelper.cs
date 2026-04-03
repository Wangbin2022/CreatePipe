using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Utils
{
    //将 DataGrid 选中状态同步到 ViewModel 的逻辑封装成一个 Helper 类，以便在其他 DataGrid 场景中复用，保持代码的整洁和一致性。
    //创建一个静态的 DataGridSelectionHelper 类，其中包含用于处理 DataGrid 选中事件的 Attached Properties。
    //创建 DataGridSelectionHelper 类负责以下职责：
    //绑定 DataGrid 的 SelectedItem 到 ViewModel 的某个属性。
    //绑定 DataGrid 的 SelectedItems.Count 到 ViewModel 的某个属性。
    //在 DataGrid.SelectionChanged 事件发生时，将最新的选中信息推送到 ViewModel。
    public static class DataGridSelectionHelper
    {
        // --- 1. ViewModel 的 SelectedItem 属性绑定 ---
        // 这个 Attached Property 允许 DataGrid 将其 SelectedItem 绑定到 ViewModel 的一个属性
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object), // 类型为 object，因为可以是任何 DataGrid 的 Item 类型
                typeof(DataGridSelectionHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));
        public static object GetSelectedItem(DataGrid obj) => obj.GetValue(SelectedItemProperty);
        public static void SetSelectedItem(DataGrid obj, object value) => obj.SetValue(SelectedItemProperty, value);
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 这个方法在 ViewModel 的 SelectedItem 改变时触发，但我们主要通过 DataGrid 来更新 ViewModel
            // 所以这里可以留空，或者用于额外的同步逻辑
        }
        // --- 2. ViewModel 的 SelectedItemsCount 属性绑定 ---
        // 这个 Attached Property 允许 DataGrid 将其 SelectedItems.Count 绑定到 ViewModel 的一个 int 属性
        public static readonly DependencyProperty SelectedItemsCountProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItemsCount",
                typeof(int),
                typeof(DataGridSelectionHelper),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsCountChanged));
        public static int GetSelectedItemsCount(DataGrid obj) => (int)obj.GetValue(SelectedItemsCountProperty);
        public static void SetSelectedItemsCount(DataGrid obj, int value) => obj.SetValue(SelectedItemsCountProperty, value);
        private static void OnSelectedItemsCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 同样，主要通过 DataGrid 来更新 ViewModel
        }
        // --- 3. 启用 DataGridSelectionHelper 的机制 ---
        // 这是一个触发器，当设置为 true 时，会附加 SelectionChanged 事件处理器
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DataGridSelectionHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));
        public static bool GetIsEnabled(DataGrid obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DataGrid obj, bool value) => obj.SetValue(IsEnabledProperty, value);
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                dataGrid.SelectionChanged -= DataGrid_SelectionChanged; // 移除旧事件，防止重复订阅
                if ((bool)e.NewValue)
                {
                    dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                    // 首次启用时，立即同步一次当前状态
                    UpdateViewModelFromDataGrid(dataGrid);
                }
            }
        }
        // --- 4. SelectionChanged 事件处理器 ---
        private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                UpdateViewModelFromDataGrid(dataGrid);
            }
        }
        // --- 5. 核心同步逻辑 ---
        private static void UpdateViewModelFromDataGrid(DataGrid dataGrid)
        {
            // 更新 SelectedItem
            if (dataGrid.SelectedItem != null)
            {
                SetSelectedItem(dataGrid, dataGrid.SelectedItem);
            }
            else
            {
                SetSelectedItem(dataGrid, null);
            }

            // 更新 SelectedItemsCount
            SetSelectedItemsCount(dataGrid, dataGrid.SelectedItems.Count);
        }
    }
}
