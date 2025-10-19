using CreatePipe.cmd;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form
{
    /// <summary>
    /// MultiSelectComboBox.xaml 的交互逻辑
    /// </summary>
    // public partial class MultiSelectComboBox : UserControl
    // {
    //     public MultiSelectComboBox()
    //     {
    //         InitializeComponent();
    //         _nodeList = new ObservableCollection<Node>();
    //     }

    //     private ObservableCollection<Node> _nodeList;
    //     private void DisplayInControl()
    //     {
    //         _nodeList.Clear();
    //         if (this.ItemsSource.Count > 0)
    //             _nodeList.Add(new Node("All"));
    //         foreach (string keyValue in this.ItemsSource)
    //         {
    //             Node node = new Node(keyValue);
    //             _nodeList.Add(node);
    //         }
    //         MultiSelectCombo.ItemsSource = _nodeList;
    //     }
    //     public static readonly DependencyProperty ItemsSourceProperty =
    //DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null,
    //new PropertyChangedCallback(MultiSelectComboBox.OnItemsSourceChanged)));

    //     public static readonly DependencyProperty SelectedItemsProperty =
    //      DependencyProperty.Register
    //      ("SelectedItems", typeof(List<string>),
    //      typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null,
    //      new PropertyChangedCallback
    //      (MultiSelectComboBox.OnSelectedItemsChanged)));

    //     public static readonly DependencyProperty TextProperty =
    //        DependencyProperty.Register("Text",
    //        typeof(string), typeof(MultiSelectComboBox),
    //        new UIPropertyMetadata(string.Empty));

    //     public static readonly DependencyProperty DefaultTextProperty =
    //         DependencyProperty.Register("DefaultText", typeof(string),
    //         typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));

    //     public List<string> ItemsSource
    //     {
    //         get
    //         {
    //             return (List<string>)GetValue(ItemsSourceProperty);
    //         }
    //         set
    //         {
    //             SetValue(ItemsSourceProperty, value);
    //         }
    //     }
    //     public List<string> SelectedItems
    //     {
    //         get
    //         {
    //             return (List<string>)GetValue(SelectedItemsProperty);
    //         }
    //         set
    //         {
    //             SetValue(SelectedItemsProperty, value);
    //         }
    //     }
    //     public string Text
    //     {
    //         get { return (string)GetValue(TextProperty); }
    //         set { SetValue(TextProperty, value); }
    //     }

    //     public string DefaultText
    //     {
    //         get { return (string)GetValue(DefaultTextProperty); }
    //         set { SetValue(DefaultTextProperty, value); }
    //     }
    //     private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //     {
    //         MultiSelectComboBox control = (MultiSelectComboBox)d;
    //         control.DisplayInControl();
    //     }
    //     private void CheckBox_Click(object sender, RoutedEventArgs e)
    //     {
    //         CheckBox clickedBox = (CheckBox)sender;
    //         if (clickedBox.Content == "All")
    //         {
    //             if (clickedBox.IsChecked.Value)
    //             {
    //                 foreach (Node node in _nodeList)
    //                 {
    //                     node.IsSelected = true;
    //                 }
    //             }
    //             else
    //             {
    //                 foreach (Node node in _nodeList)
    //                 {
    //                     node.IsSelected = false;
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             int _selectedCount = 0;
    //             foreach (Node s in _nodeList)
    //             {
    //                 if (s.IsSelected && s.Title != "All")
    //                     _selectedCount++;
    //             }
    //             if (_selectedCount == _nodeList.Count - 1)
    //                 _nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = true;
    //             else
    //                 _nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = false;
    //         }
    //         SetSelectedItems();
    //         SetText();
    //     }
    //     private void SetSelectedItems()
    //     {
    //         if (SelectedItems == null)
    //             SelectedItems = new List<string>();
    //         SelectedItems.Clear();
    //         foreach (Node node in _nodeList)
    //         {
    //             if (node.IsSelected && node.Title != "All")
    //             {
    //                 if (this.ItemsSource.Count > 0)
    //                     SelectedItems.Add(node.Title);
    //             }
    //         }
    //     }
    //     private void SelectNodes()
    //     {
    //         foreach (string keyValue in SelectedItems)
    //         {
    //             Node node = _nodeList.FirstOrDefault(i => i.Title == keyValue);
    //             if (node != null)
    //                 node.IsSelected = true;
    //         }
    //     }
    //     private void SetText()
    //     {
    //         if (this.SelectedItems != null)
    //         {
    //             StringBuilder displayText = new StringBuilder();
    //             foreach (Node s in _nodeList)
    //             {
    //                 if (s.IsSelected == true && s.Title == "All")
    //                 {
    //                     displayText = new StringBuilder();
    //                     displayText.Append("All");
    //                     break;
    //                 }
    //                 else if (s.IsSelected == true && s.Title != "All")
    //                 {
    //                     displayText.Append(s.Title);
    //                     displayText.Append(',');
    //                 }
    //             }
    //             this.Text = displayText.ToString().TrimEnd(new char[] { ',' });
    //         }
    //         if (string.IsNullOrEmpty(this.Text))
    //         {
    //             this.Text = this.DefaultText;
    //         }
    //     }
    //     private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //     {
    //         MultiSelectComboBox control = (MultiSelectComboBox)d;
    //         control.SelectNodes();
    //         control.SetText();
    //     }
    // }
    public partial class MultiSelectComboBox : UserControl
    {
        private readonly ObservableCollection<Node> _nodeList;

        public MultiSelectComboBox()
        {
            InitializeComponent();
            _nodeList = new ObservableCollection<Node>();
            // 确保控件加载完成后，即使 ItemsSource 没变，也刷新一次显示
            this.Loaded += (s, e) => DisplayInControl();
        }

        private void DisplayInControl()
        {
            if (ItemsSource == null) return; // 如果数据源为空，则不进行任何操作

            _nodeList.Clear();

            // 检查是否有数据，并添加 "All" 选项
            if (this.ItemsSource.Count > 0)
            {
                var allNode = new Node("All");
                _nodeList.Add(allNode);
            }

            // 遍历数据源，创建 Node 并添加到内部列表
            foreach (var item in this.ItemsSource)
            {
                var node = new Node(item.ToString());
                _nodeList.Add(node);
            }

            // 将内部列表作为 ComboBox 的数据源
            MultiSelectCombo.ItemsSource = _nodeList;

            // 根据已选项更新勾选状态
            SelectNodes();
            SetText();
        }

        #region Dependency Properties

        // --- 改动 1: 将类型从 List<string> 改为 IList ---
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectComboBox), new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

        // Text 和 DefaultText 保持不变
        public static readonly DependencyProperty TextProperty =
           DependencyProperty.Register("Text", typeof(string), typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(MultiSelectComboBox), new UIPropertyMetadata(string.Empty));


        // --- CLR 属性包装器也同步修改 ---
        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string DefaultText
        {
            get { return (string)GetValue(DefaultTextProperty); }
            set { SetValue(DefaultTextProperty, value); }
        }

        #endregion

        #region Callbacks and Event Handlers

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiSelectComboBox)d;
            control.DisplayInControl();
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiSelectComboBox)d;
            control.SelectNodes();
            control.SetText();
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var clickedBox = (CheckBox)sender;

            // --- 这是关键的修改 ---
            // 通过检查 Content 来识别 "All" 复选框，而不是 Tag
            if (clickedBox.Content?.ToString() == "All")
            {
                // 全选/全不选逻辑
                bool isChecked = clickedBox.IsChecked ?? false;
                foreach (var node in _nodeList)
                {
                    // 将所有节点的选中状态同步为 "All" 的状态
                    node.IsSelected = isChecked;
                }
            }
            else // 单个项目点击逻辑
            {
                var allNode = _nodeList.FirstOrDefault(n => n.Title == "All");
                if (allNode != null)
                {
                    // 检查除了 "All" 之外的所有项目是否都被选中
                    var otherNodes = _nodeList.Where(n => n.Title != "All");

                    // 使用 LINQ 的 .All() 方法判断是否所有项都已选中
                    bool allOthersSelected = otherNodes.Any() && otherNodes.All(n => n.IsSelected);

                    // 更新 "All" 复选框的选中状态
                    allNode.IsSelected = allOthersSelected;
                }
            }

            // 更新外部绑定的 SelectedItems 集合
            SetSelectedItems();

            // 更新 ComboBox 显示的文本
            SetText();
        }
        //private void CheckBox_Click(object sender, RoutedEventArgs e)
        //{
        //    var clickedBox = (CheckBox)sender;

        //    // 全选/全不选逻辑
        //    if (clickedBox.Tag?.ToString() == "All")
        //    {
        //        bool isChecked = clickedBox.IsChecked ?? false;
        //        foreach (var node in _nodeList)
        //        {
        //            node.IsSelected = isChecked;
        //        }
        //    }
        //    else // 单个项目点击逻辑
        //    {
        //        var allNode = _nodeList.FirstOrDefault(n => n.Title == "All");
        //        if (allNode != null)
        //        {
        //            // 检查除了 "All" 之外的所有项目是否都被选中
        //            bool allOthersSelected = _nodeList.Where(n => n.Title != "All").All(n => n.IsSelected);
        //            allNode.IsSelected = allOthersSelected;
        //        }
        //    }

        //    // 更新外部绑定的 SelectedItems
        //    SetSelectedItems();
        //    // 更新显示的文本
        //    SetText();
        //}

        #endregion

        #region Private Helper Methods

        // --- 改动 2: 修正 SetSelectedItems 方法 ---
        private void SetSelectedItems()
        {
            // 如果外部没有绑定 SelectedItems，则不做任何事
            if (SelectedItems == null) return;

            // 直接在 ViewModel 传入的集合上操作，不要创建新实例！
            SelectedItems.Clear();
            foreach (var node in _nodeList)
            {
                if (node.IsSelected && node.Title != "All")
                {
                    SelectedItems.Add(node.Title);
                }
            }
        }

        private void SelectNodes()
        {
            if (SelectedItems == null || _nodeList.Count == 0) return;

            // 先取消所有选择
            foreach (var node in _nodeList)
            {
                node.IsSelected = false;
            }

            // 根据 SelectedItems 勾选对应的项
            foreach (var selectedItem in SelectedItems)
            {
                var node = _nodeList.FirstOrDefault(i => i.Title == selectedItem.ToString());
                if (node != null)
                {
                    node.IsSelected = true;
                }
            }
        }

        // (此方法逻辑基本正确，无需大改)
        private void SetText()
        {
            if (this.SelectedItems != null)
            {
                if (_nodeList.FirstOrDefault(n => n.Title == "All")?.IsSelected == true)
                {
                    this.Text = "All";
                }
                else
                {
                    var selectedTitles = _nodeList
                        .Where(s => s.IsSelected && s.Title != "All")
                        .Select(s => s.Title);
                    this.Text = string.Join(",", selectedTitles);
                }
            }

            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = this.DefaultText;
            }
        }

        #endregion
    }
    public class Node : ObserverableObject
    {
        private string _title;
        private bool _isSelected;
        public Node(string title)
        {
            Title = title;
        }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }
}
