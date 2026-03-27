using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    //20260326 
    public partial class MultiSelectListBoxDynamic : UserControl
    {
        private readonly ObservableCollection<Node> _nodeList = new ObservableCollection<Node>();
        private bool _isUpdatingInternally = false;
        public MultiSelectListBoxDynamic()
        {
            InitializeComponent();
            MultiSelectLsBox.ItemsSource = _nodeList;
        }
        #region Dependency Properties
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<string>), typeof(MultiSelectListBoxDynamic),
                new FrameworkPropertyMetadata(null, OnItemsSourceChanged));
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<string>), typeof(MultiSelectListBoxDynamic),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MultiSelectListBoxDynamic), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register(nameof(DefaultText), typeof(string), typeof(MultiSelectListBoxDynamic), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.Register(nameof(SelectionChangedCommand), typeof(ICommand), typeof(MultiSelectListBoxDynamic), new PropertyMetadata(null));
        public ObservableCollection<string> ItemsSource
        {
            get => (ObservableCollection<string>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
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
            var control = (MultiSelectListBoxDynamic)d;
            control.HandleCollectionSubscription(e.OldValue, e.NewValue, control.OnItemsSourceCollectionChanged);
            control.RebuildNodeList();
        }
        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiSelectListBoxDynamic)d;
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
            _nodeList.Clear();
            if (ItemsSource == null) return;

            _nodeList.Add(new Node("All"));
            foreach (var item in ItemsSource)
            {
                _nodeList.Add(new Node(item));
            }
            UpdateNodesSelection();
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
    //public partial class MultiSelectListBoxDynamic : UserControl
    //{
    //    private ObservableCollection<Node> _nodeList;
    //    // 添加一个防重入锁，防止 UI 和 ViewModel 互相死循环调用
    //    private bool _isUpdatingInternally = false;
    //    public MultiSelectListBoxDynamic()
    //    {
    //        InitializeComponent();
    //        _nodeList = new ObservableCollection<Node>();
    //        MultiSelectLsBox.ItemsSource = _nodeList; // 在构造函数中绑定即可
    //    }
    //    // 1. 将 List<string> 改为 ObservableCollection<string>
    //    public static readonly DependencyProperty ItemsSourceProperty =
    //        DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<string>), typeof(MultiSelectListBoxDynamic),
    //            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
    //    // 2. 增加 BindsTwoWayByDefault，让 SelectedItems 默认支持双向绑定
    //    public static readonly DependencyProperty SelectedItemsProperty =
    //        DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<string>), typeof(MultiSelectListBoxDynamic),
    //            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedItemsChanged)));
    //    public static readonly DependencyProperty TextProperty =
    //       DependencyProperty.Register("Text", typeof(string), typeof(MultiSelectListBoxDynamic), new UIPropertyMetadata(string.Empty));
    //    public static readonly DependencyProperty DefaultTextProperty =
    //        DependencyProperty.Register("DefaultText", typeof(string), typeof(MultiSelectListBoxDynamic), new UIPropertyMetadata(string.Empty));
    //    public ObservableCollection<string> ItemsSource
    //    {
    //        get { return (ObservableCollection<string>)GetValue(ItemsSourceProperty); }
    //        set { SetValue(ItemsSourceProperty, value); }
    //    }
    //    public ObservableCollection<string> SelectedItems
    //    {
    //        get { return (ObservableCollection<string>)GetValue(SelectedItemsProperty); }
    //        set { SetValue(SelectedItemsProperty, value); }
    //    }
    //    public string Text
    //    {
    //        get { return (string)GetValue(TextProperty); }
    //        set { SetValue(TextProperty, value); }
    //    }
    //    public string DefaultText
    //    {
    //        get { return (string)GetValue(DefaultTextProperty); }
    //        set { SetValue(DefaultTextProperty, value); }
    //    }
    //    private void DisplayInControl()
    //    {
    //        _nodeList.Clear();
    //        if (this.ItemsSource != null && this.ItemsSource.Count > 0)
    //            _nodeList.Add(new Node("All"));
    //        if (this.ItemsSource != null)
    //        {
    //            foreach (string keyValue in this.ItemsSource)
    //            {
    //                _nodeList.Add(new Node(keyValue));
    //            }
    //        }
    //    }
    //    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        MultiSelectListBoxDynamic control = (MultiSelectListBoxDynamic)d;
    //        // 监听数据源集合的增减变化
    //        if (e.OldValue is INotifyCollectionChanged oldCollection)
    //            oldCollection.CollectionChanged -= control.OnItemsSourceCollectionChanged;
    //        if (e.NewValue is INotifyCollectionChanged newCollection)
    //            newCollection.CollectionChanged += control.OnItemsSourceCollectionChanged;
    //        control.DisplayInControl();
    //        control.SelectNodes(); // 重新加载列表后，恢复选中状态
    //        control.SetText();
    //    }
    //    private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        DisplayInControl();
    //        SelectNodes();
    //        SetText();
    //    }
    //    private void CheckBox_Click(object sender, RoutedEventArgs e)
    //    {
    //        CheckBox clickedBox = (CheckBox)sender;
    //        if (clickedBox.Content.ToString() == "All")
    //        {
    //            bool isChecked = clickedBox.IsChecked ?? false;
    //            foreach (Node node in _nodeList)
    //            {
    //                node.IsSelected = isChecked;
    //            }
    //        }
    //        else
    //        {
    //            int _selectedCount = 0;
    //            foreach (Node s in _nodeList)
    //            {
    //                if (s.IsSelected && s.Title != "All")
    //                    _selectedCount++;
    //            }
    //            var allNode = _nodeList.FirstOrDefault(i => i.Title == "All");
    //            if (allNode != null)
    //            {
    //                allNode.IsSelected = (_selectedCount == _nodeList.Count - 1);
    //            }
    //        }
    //        SetSelectedItems();
    //        SetText();
    //        // 【0326新增】: 如果外部绑定了命令，则执行它，并把当前选中的集合作为参数传出
    //        if (SelectionChangedCommand != null && SelectionChangedCommand.CanExecute(SelectedItems))
    //        {
    //            SelectionChangedCommand.Execute(SelectedItems);
    //        }
    //    }
    //    // 3. 核心修改：保持绑定实例不变，只修改集合内部元素
    //    private void SetSelectedItems()
    //    {
    //        if (SelectedItems == null) return;
    //        _isUpdatingInternally = true; // 上锁，防止触发 OnSelectedItemsCollectionChanged
    //        SelectedItems.Clear(); // 清空旧数据
    //        foreach (Node node in _nodeList)
    //        {
    //            if (node.IsSelected && node.Title != "All")
    //            {
    //                if (this.ItemsSource != null && this.ItemsSource.Count > 0)
    //                    SelectedItems.Add(node.Title); // 增加新数据
    //            }
    //        }
    //        _isUpdatingInternally = false; // 解锁
    //    }
    //    private void SelectNodes()
    //    {
    //        if (_isUpdatingInternally || SelectedItems == null) return;
    //        foreach (Node node in _nodeList)
    //        {
    //            if (node.Title != "All")
    //            {
    //                // 根据绑定的 SelectedItems 同步 CheckBox 状态
    //                node.IsSelected = SelectedItems.Contains(node.Title);
    //            }
    //        }
    //        // 更新 "All" 节点的状态
    //        int _selectedCount = _nodeList.Count(s => s.IsSelected && s.Title != "All");
    //        var allNode = _nodeList.FirstOrDefault(i => i.Title == "All");
    //        if (allNode != null)
    //        {
    //            allNode.IsSelected = (_selectedCount == _nodeList.Count - 1 && _nodeList.Count > 1);
    //        }
    //    }
    //    private void SetText()
    //    {
    //        if (this.SelectedItems != null)
    //        {
    //            StringBuilder displayText = new StringBuilder();
    //            foreach (Node s in _nodeList)
    //            {
    //                if (s.IsSelected && s.Title == "All")
    //                {
    //                    displayText.Clear();
    //                    displayText.Append("All");
    //                    break;
    //                }
    //                else if (s.IsSelected && s.Title != "All")
    //                {
    //                    displayText.Append(s.Title);
    //                    displayText.Append(',');
    //                }
    //            }
    //            this.Text = displayText.ToString().TrimEnd(',');
    //        }
    //        if (string.IsNullOrEmpty(this.Text))
    //        {
    //            this.Text = this.DefaultText;
    //        }
    //    }
    //    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        MultiSelectListBoxDynamic control = (MultiSelectListBoxDynamic)d;
    //        // 监听 ViewModel 中对 SelectedItems 集合的修改 (如 .Add, .Remove)
    //        if (e.OldValue is INotifyCollectionChanged oldCollection)
    //            oldCollection.CollectionChanged -= control.OnSelectedItemsCollectionChanged;
    //        if (e.NewValue is INotifyCollectionChanged newCollection)
    //            newCollection.CollectionChanged += control.OnSelectedItemsCollectionChanged;
    //        control.SelectNodes();
    //        control.SetText();
    //    }
    //    private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        // 如果是因为用户在界面上点击触发的，不用重复执行
    //        if (!_isUpdatingInternally)
    //        {
    //            SelectNodes();
    //            SetText();
    //        }
    //    }
    //    //0326添加
    //    // 【新增】: 创建一个依赖属性来接收 ViewModel 的命令
    //    public static readonly DependencyProperty SelectionChangedCommandProperty =
    //        DependencyProperty.Register("SelectionChangedCommand", typeof(ICommand), typeof(MultiSelectListBoxDynamic), new PropertyMetadata(null));
    //    public ICommand SelectionChangedCommand
    //    {
    //        get { return (ICommand)GetValue(SelectionChangedCommandProperty); }
    //        set { SetValue(SelectionChangedCommandProperty, value); }
    //    }
    //}
}
