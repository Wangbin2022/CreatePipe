using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// MultiSelectListBoxDynamicWithItemCount.xaml 的交互逻辑
    /// </summary>
    public partial class MultiSelectListBoxDynamicWithItemCount : UserControl
    {
        private readonly ObservableCollection<Node> _nodeList = new ObservableCollection<Node>();
        private bool _isUpdatingInternally = false;
        public MultiSelectListBoxDynamicWithItemCount()
        {
            InitializeComponent();
            MultiSelectLsBox.ItemsSource = _nodeList;
        }
        #region Dependency Properties
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<CountableItem>), typeof(MultiSelectListBoxDynamicWithItemCount),
                new FrameworkPropertyMetadata(null, OnItemsSourceChanged));
        public readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<string>), typeof(MultiSelectListBoxDynamicWithItemCount),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MultiSelectListBoxDynamicWithItemCount), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register(nameof(DefaultText), typeof(string), typeof(MultiSelectListBoxDynamicWithItemCount), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.Register(nameof(SelectionChangedCommand), typeof(ICommand), typeof(MultiSelectListBoxDynamicWithItemCount), new PropertyMetadata(null));
        public ObservableCollection<CountableItem> ItemsSource
        {
            get => (ObservableCollection<CountableItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        //public ObservableCollection<string> ItemsSource
        //{
        //    get => (ObservableCollection<string>)GetValue(ItemsSourceProperty);
        //    set => SetValue(ItemsSourceProperty, value);
        //}
        public ObservableCollection<string> SelectedItems
        {
            get => (ObservableCollection<string>)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string DefaultText
        {
            get => (string)GetValue(DefaultTextProperty);
            set => SetValue(DefaultTextProperty, value);
        }
        public ICommand SelectionChangedCommand
        {
            get => (ICommand)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }
        #endregion

        #region Synchronization Logic
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiSelectListBoxDynamicWithItemCount)d;
            control.HandleCollectionSubscription(e.OldValue, e.NewValue, control.OnItemsSourceCollectionChanged);
            control.RebuildNodeList();
        }
        //private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var control = (MultiSelectListBoxDynamicWithItemCount)d;
        //    control.HandleCollectionSubscription(e.OldValue, e.NewValue, control.OnItemsSourceCollectionChanged);
        //    control.RebuildNodeList();
        //}
        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiSelectListBoxDynamicWithItemCount)d;
            control.HandleCollectionSubscription(e.OldValue, e.NewValue, control.OnSelectedItemsCollectionChanged);
            control.UpdateNodesSelection();
        }
        private void HandleCollectionSubscription(object oldColl, object newColl, NotifyCollectionChangedEventHandler handler)
        {
            if (oldColl is INotifyCollectionChanged oldIncc) oldIncc.CollectionChanged -= handler;
            if (newColl is INotifyCollectionChanged newIncc) newIncc.CollectionChanged += handler;
        }
        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RebuildNodeList();
        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isUpdatingInternally) UpdateNodesSelection();
        }
        // 重新构建内部 Node 列表
        private void RebuildNodeList()
        {
            // 清理旧的事件订阅，防止内存泄漏
            foreach (var oldNode in _nodeList)
            {
                if (oldNode.OriginalItem != null)
                    oldNode.OriginalItem.PropertyChanged -= ExternalItem_PropertyChanged;
            }

            _nodeList.Clear();
            if (ItemsSource == null) return;

            // 添加 All 节点，Count 设为 null 以便隐藏数量
            _nodeList.Add(new Node { Title = "All", Count = null });

            foreach (var item in ItemsSource)
            {
                var node = new Node { Title = item.Name, Count = item.Count, OriginalItem = item };
                // 订阅外部数据变化：如果外部数量更新，同步到内部节点
                item.PropertyChanged += ExternalItem_PropertyChanged;
                _nodeList.Add(node);
            }
            UpdateNodesSelection();
        }
        //private void RebuildNodeList()
        //{
        //    _nodeList.Clear();
        //    if (ItemsSource == null) return;

        //    _nodeList.Add(new Node("All"));
        //    foreach (var item in ItemsSource)
        //    {
        //        _nodeList.Add(new Node(item));
        //    }
        //    UpdateNodesSelection();
        //}
        // 当外部的 CountableItem 的数量发生变化时，更新内部 UI 的数量
        private void ExternalItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CountableItem.Count) && sender is CountableItem changedItem)
            {
                var targetNode = _nodeList.FirstOrDefault(n => n.OriginalItem == changedItem);
                if (targetNode != null)
                {
                    targetNode.Count = changedItem.Count;
                }
            }
        }
        // 根据外部 SelectedItems 更新内部 CheckBox 状态
        private void UpdateNodesSelection()
        {
            if (_isUpdatingInternally || SelectedItems == null) return;
            _isUpdatingInternally = true;
            foreach (var node in _nodeList)
            {
                if (node.Title == "All") continue;
                node.IsSelected = SelectedItems.Contains(node.Title);
            }
            UpdateAllCheckBoxState();
            SetText();
            _isUpdatingInternally = false;
        }
        // 根据内部 CheckBox 状态更新外部 SelectedItems 集合
        private void UpdateExternalSelectedItems()
        {
            if (SelectedItems == null) return;
            _isUpdatingInternally = true;
            SelectedItems.Clear();
            foreach (var node in _nodeList.Where(n => n.Title != "All" && n.IsSelected))
            {
                SelectedItems.Add(node.Title);
            }
            SetText();
            _isUpdatingInternally = false;
            // 触发外部命令
            if (SelectionChangedCommand != null && SelectionChangedCommand.CanExecute(SelectedItems))
            {
                SelectionChangedCommand.Execute(SelectedItems);
            }
        }
        #endregion

        #region UI Events
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox cb)) return;
            string content = cb.Content.ToString();
            if (content == "All")
            {
                bool isChecked = cb.IsChecked ?? false;
                foreach (var node in _nodeList) node.IsSelected = isChecked;
            }
            else
            {
                UpdateAllCheckBoxState();
            }
            UpdateExternalSelectedItems();
        }
        private void UpdateAllCheckBoxState()
        {
            var allNode = _nodeList.FirstOrDefault(n => n.Title == "All");
            if (allNode == null) return;
            var dataNodes = _nodeList.Where(n => n.Title != "All").ToList();
            if (!dataNodes.Any()) return;
            allNode.IsSelected = dataNodes.All(n => n.IsSelected);
        }
        private void SetText()
        {
            if (SelectedItems == null || !SelectedItems.Any())
            {
                Text = DefaultText;
                return;
            }
            var allNode = _nodeList.FirstOrDefault(n => n.Title == "All");
            if (allNode != null && allNode.IsSelected)
            {
                Text = "All";
            }
            else
            {
                Text = string.Join(",", SelectedItems);
            }
        }
        #endregion
    }
    public class CountableItem : INotifyPropertyChanged
    {
        private string _name;
        private int _count;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public CountableItem(string name, int count = 0)
        {
            Name = name;
            Count = count;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
