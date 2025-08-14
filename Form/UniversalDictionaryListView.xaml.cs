using CreatePipe.cmd;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// UniversalDictionaryListView.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalDictionaryListView : Window
    {
        public UniversalDictionaryListView(IDictionary<string, string> dict, string title = "数据列表")
        {
            InitializeComponent();
            this.Title = title;
            this.DataContext = new UniversalDictionaryListViewModel<string, string>(dict, title);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class UniversalDictionaryListViewModel<TKey, TValue> : ObserverableObject
    {
        public UniversalDictionaryListViewModel(IDictionary<TKey, TValue> dictionary, string title = "数据列表")
        {
            if (dictionary != null)
            {
                foreach (var kv in dictionary)
                {
                    Items.Add(kv);
                }
            }
            Title = title;
        }
        public ObservableCollection<KeyValuePair<TKey, TValue>> Items { get; } = new ObservableCollection<KeyValuePair<TKey, TValue>>();
        public string Title { get; }
    }
}
