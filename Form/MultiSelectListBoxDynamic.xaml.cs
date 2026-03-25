using System;
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
    /// <summary>
    /// MultiSelectListBoxDynamic.xaml 的交互逻辑
    /// </summary>
    public partial class MultiSelectListBoxDynamic : UserControl
    {
        public MultiSelectListBoxDynamic()
        {
            InitializeComponent();
            _nodeList = new ObservableCollection<Node>();
        }

        private ObservableCollection<Node> _nodeList;
        private void DisplayInControl()
        {
            _nodeList.Clear();
            if (this.ItemsSource.Count > 0)
                _nodeList.Add(new Node("All"));
            foreach (string keyValue in this.ItemsSource)
            {
                Node node = new Node(keyValue);
                _nodeList.Add(node);
            }
            MultiSelectLsBox.ItemsSource = _nodeList;
        }
        public static readonly DependencyProperty ItemsSourceProperty =
   DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(MultiSelectListBoxDynamic), new FrameworkPropertyMetadata(null,
   new PropertyChangedCallback(MultiSelectListBoxDynamic.OnItemsSourceChanged)));

        public static readonly DependencyProperty SelectedItemsProperty =
         DependencyProperty.Register
         ("SelectedItems", typeof(List<string>),
         typeof(MultiSelectListBoxDynamic), new FrameworkPropertyMetadata(null,
         new PropertyChangedCallback
         (MultiSelectListBoxDynamic.OnSelectedItemsChanged)));

        public static readonly DependencyProperty TextProperty =
           DependencyProperty.Register("Text",
           typeof(string), typeof(MultiSelectListBoxDynamic),
           new UIPropertyMetadata(string.Empty));

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string),
            typeof(MultiSelectListBoxDynamic), new UIPropertyMetadata(string.Empty));

        public List<string> ItemsSource
        {
            get
            {
                return (List<string>)GetValue(ItemsSourceProperty);
            }
            set
            {
                SetValue(ItemsSourceProperty, value);
            }
        }
        public List<string> SelectedItems
        {
            get
            {
                return (List<string>)GetValue(SelectedItemsProperty);
            }
            set
            {
                SetValue(SelectedItemsProperty, value);
            }
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
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MultiSelectListBoxDynamic control = (MultiSelectListBoxDynamic)d;
            control.DisplayInControl();
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox clickedBox = (CheckBox)sender;
            if (clickedBox.Content == "All")
            {
                if (clickedBox.IsChecked.Value)
                {
                    foreach (Node node in _nodeList)
                    {
                        node.IsSelected = true;
                    }
                }
                else
                {
                    foreach (Node node in _nodeList)
                    {
                        node.IsSelected = false;
                    }
                }
            }
            else
            {
                int _selectedCount = 0;
                foreach (Node s in _nodeList)
                {
                    if (s.IsSelected && s.Title != "All")
                        _selectedCount++;
                }
                if (_selectedCount == _nodeList.Count - 1)
                    _nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = true;
                else
                    _nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = false;
            }
            SetSelectedItems();
            SetText();
        }
        private void SetSelectedItems()
        {
            // 创建一个新列表，这样才能触发 DependencyProperty 的 PropertyChanged 事件
            var newList = new List<string>();
            foreach (Node node in _nodeList)
            {
                if (node.IsSelected && node.Title != "All")
                {
                    if (this.ItemsSource.Count > 0)
                        newList.Add(node.Title);
                }
            }
            // 关键：重新赋值以触发外部 Binding
            this.SelectedItems = newList;
        }
        //private void SetSelectedItems()
        //{
        //    if (SelectedItems == null)
        //        SelectedItems = new List<string>();
        //    SelectedItems.Clear();
        //    foreach (Node node in _nodeList)
        //    {
        //        if (node.IsSelected && node.Title != "All")
        //        {
        //            if (this.ItemsSource.Count > 0)
        //                SelectedItems.Add(node.Title);
        //        }
        //    }
        //}
        private void SelectNodes()
        {
            foreach (string keyValue in SelectedItems)
            {
                Node node = _nodeList.FirstOrDefault(i => i.Title == keyValue);
                if (node != null)
                    node.IsSelected = true;
            }
        }
        private void SetText()
        {
            if (this.SelectedItems != null)
            {
                StringBuilder displayText = new StringBuilder();
                foreach (Node s in _nodeList)
                {
                    if (s.IsSelected == true && s.Title == "All")
                    {
                        displayText = new StringBuilder();
                        displayText.Append("All");
                        break;
                    }
                    else if (s.IsSelected == true && s.Title != "All")
                    {
                        displayText.Append(s.Title);
                        displayText.Append(',');
                    }
                }
                this.Text = displayText.ToString().TrimEnd(new char[] { ',' });
            }
            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = this.DefaultText;
            }
        }
        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MultiSelectListBoxDynamic control = (MultiSelectListBoxDynamic)d;
            control.SelectNodes();
            control.SetText();
        }
    }
}
