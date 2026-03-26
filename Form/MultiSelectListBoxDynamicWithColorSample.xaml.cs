using CreatePipe.cmd;
using CreatePipe.Obselete;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class MultiSelectListBoxDynamicWithColorSample : UserControl
    {
        // 内部使用的包装类，确保继承正确的通知基类
        public class Node :ObserverableObject
        {
            public string Title { get; set; }
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }
            public Autodesk.Revit.DB.Color LayerColor { get; set; }

            public Node(string title, Autodesk.Revit.DB.Color color = null)
            {
                Title = title;
                LayerColor = color;
            }
        }

        private ObservableCollection<Node> _nodeList = new ObservableCollection<Node>();

        public MultiSelectListBoxDynamicWithColorSample()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable),
            typeof(MultiSelectListBoxDynamicWithColorSample),
            new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(IList),
            typeof(MultiSelectListBoxDynamicWithColorSample),
            new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MultiSelectListBoxDynamicWithColorSample), new UIPropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(MultiSelectListBoxDynamicWithColorSample), new UIPropertyMetadata(string.Empty));

        public string DefaultText
        {
            get => (string)GetValue(DefaultTextProperty);
            set => SetValue(DefaultTextProperty, value);
        }

        #endregion

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectListBoxDynamicWithColorSample control)
                control.DisplayInControl();
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectListBoxDynamicWithColorSample control)
            {
                control.SelectNodes();
                control.SetText();
            }
        }

        private void DisplayInControl()
        {
            _nodeList.Clear();
            if (this.ItemsSource == null) return;

            _nodeList.Add(new Node("All"));

            foreach (var item in this.ItemsSource)
            {
                // 修复点：确保属性名与你的 CadLayerItem 一致 (LayerName)
                if (item is CadLayerItem cadItem)
                {
                    _nodeList.Add(new Node(cadItem.Title, cadItem.LayerColor)
                    {
                        IsSelected = cadItem.IsSelected
                    });
                }
            }
            MultiSelectLsBox.ItemsSource = _nodeList;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox clickedBox = (CheckBox)sender;
            string content = clickedBox.Content as string;

            if (content == "All")
            {
                bool isChecked = clickedBox.IsChecked ?? false;
                foreach (Node node in _nodeList)
                {
                    node.IsSelected = isChecked;
                }
            }
            else
            {
                // 检查非 All 节点的选中情况
                var dataNodes = _nodeList.Where(x => x.Title != "All").ToList();
                var allNode = _nodeList.FirstOrDefault(x => x.Title == "All");
                if (allNode != null)
                {
                    allNode.IsSelected = dataNodes.All(x => x.IsSelected);
                }
            }

            SetSelectedItems();
            SetText();
        }

        private void SetSelectedItems()
        {
            var newList = new List<string>();
            foreach (Node node in _nodeList)
            {
                if (node.IsSelected && node.Title != "All")
                {
                    newList.Add(node.Title);
                }
            }
            // 必须重新赋值整个 List，外部 ViewModel 的 Setter 才会触发
            this.SelectedItems = newList;
            //var selectedList = new List<string>();
            //foreach (Node node in _nodeList)
            //{
            //    if (node.IsSelected && node.Title != "All")
            //    {
            //        selectedList.Add(node.Title);
            //    }
            //}
            //this.SelectedItems = selectedList; // 触发外部 Binding
        }

        private void SelectNodes()
        {
            if (SelectedItems == null) return;
            foreach (var selectedTitle in SelectedItems)
            {
                var node = _nodeList.FirstOrDefault(i => i.Title == selectedTitle.ToString());
                if (node != null) node.IsSelected = true;
            }
        }

        private void SetText()
        {
            if (this.SelectedItems == null || this.SelectedItems.Count == 0)
            {
                this.Text = this.DefaultText;
                return;
            }

            var allNode = _nodeList.FirstOrDefault(x => x.Title == "All");
            if (allNode != null && allNode.IsSelected)
            {
                this.Text = "All";
            }
            else
            {
                var selectedTitles = _nodeList.Where(x => x.IsSelected && x.Title != "All").Select(x => x.Title);
                this.Text = string.Join(",", selectedTitles);
            }
        }
    }
}
// /// <summary>
// /// MultiSelectListBoxDynamicWithColorSample.xaml 的交互逻辑
// /// </summary>
// public partial class MultiSelectListBoxDynamicWithColorSample : UserControl
// {
//     public MultiSelectListBoxDynamicWithColorSample()
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
//         MultiSelectLsBox.ItemsSource = _nodeList;
//     }
//     public static readonly DependencyProperty ItemsSourceProperty =
//DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(MultiSelectListBoxDynamicWithColorSample), new FrameworkPropertyMetadata(null,
//new PropertyChangedCallback(MultiSelectListBoxDynamicWithColorSample.OnItemsSourceChanged)));
//     public static readonly DependencyProperty SelectedItemsProperty =
//      DependencyProperty.Register
//      ("SelectedItems", typeof(List<string>),
//      typeof(MultiSelectListBoxDynamicWithColorSample), new FrameworkPropertyMetadata(null,
//      new PropertyChangedCallback
//      (MultiSelectListBoxDynamicWithColorSample.OnSelectedItemsChanged)));
//     public static readonly DependencyProperty TextProperty =
//        DependencyProperty.Register("Text",
//        typeof(string), typeof(MultiSelectListBoxDynamicWithColorSample),
//        new UIPropertyMetadata(string.Empty));
//     public static readonly DependencyProperty DefaultTextProperty =
//         DependencyProperty.Register("DefaultText", typeof(string),
//         typeof(MultiSelectListBoxDynamicWithColorSample), new UIPropertyMetadata(string.Empty));
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
//         MultiSelectListBoxDynamicWithColorSample control = (MultiSelectListBoxDynamicWithColorSample)d;
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
//         // 创建一个新列表，这样才能触发 DependencyProperty 的 PropertyChanged 事件
//         var newList = new List<string>();
//         foreach (Node node in _nodeList)
//         {
//             if (node.IsSelected && node.Title != "All")
//             {
//                 if (this.ItemsSource.Count > 0)
//                     newList.Add(node.Title);
//             }
//         }
//         // 关键：重新赋值以触发外部 Binding
//         this.SelectedItems = newList;
//     }
//     //private void SetSelectedItems()
//     //{
//     //    if (SelectedItems == null)
//     //        SelectedItems = new List<string>();
//     //    SelectedItems.Clear();
//     //    foreach (Node node in _nodeList)
//     //    {
//     //        if (node.IsSelected && node.Title != "All")
//     //        {
//     //            if (this.ItemsSource.Count > 0)
//     //                SelectedItems.Add(node.Title);
//     //        }
//     //    }
//     //}
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
//         MultiSelectListBoxDynamicWithColorSample control = (MultiSelectListBoxDynamicWithColorSample)d;
//         control.SelectNodes();
//         control.SetText();
//     }
// }
//}
