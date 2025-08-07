using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// ListboxTest.xaml 的交互逻辑
    /// </summary>
    public partial class ListboxTest : Window
    {
        public ListboxTest()
        {
            InitializeComponent();
            this.DataContext = new ListboxTestVM();
            //this.DataContext = new mViewModel();
        }
        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var viewModel = (mViewModel)DataContext;
        //    viewModel.OnCustomItemSelected();
        //}
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class mViewModel
    {
        public ObservableCollection<string> Items { get; set; }
        public string SelectedItem { get; set; }
        public mViewModel()
        {
            Items = new ObservableCollection<string> { "选项1", "选项2", "选项3", "自定义" };
        }
        //public void AddCustomItem(string newItem)
        //{
        //    if (!string.IsNullOrWhiteSpace(newItem) && !Items.Contains(newItem))
        //    {
        //        Items.Add(newItem);
        //        SelectedItem = newItem;
        //    }
        //}
        //public void OnCustomItemSelected()
        //{
        //    if (SelectedItem == "自定义")
        //    {
        //        UniversalNewString subView = new UniversalNewString("提示：请输入主文件名");
        //        if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName)) return;
        //        AddCustomItem(vm.NewName);
        //    }
        //}
    }
    public class ListboxTestVM : ObserverableObject
    {
        public ObservableCollection<BeautifulGirl> Girls { get; set; }
        public ListboxTestVM()
        {
            Girls = new ObservableCollection<BeautifulGirl>
            {
                new BeautifulGirl
                {
                    Name ="刘亦菲",
                },
                new BeautifulGirl
                {
                    Name ="高圆圆",
                },
                new BeautifulGirl
                {
                    Name ="自定义",
                }
            };
            if (Girls.Count > 0)
            {
                SelectedItem = Girls[0];
            }
        }
        private BeautifulGirl _selectedGirl;
        public BeautifulGirl SelectedItem
        {
            get => _selectedGirl;
            set => SetProperty(ref _selectedGirl, value);
        }

        public ICommand TestCommand => new BaseBindingCommand(Test);
        private void Test(object obj)
        {
            TaskDialog.Show("tt", Girls.Count().ToString());
        }
        public ICommand DelCommand => new BaseBindingCommand(DelAction);
        private void DelAction(object parameter)
        {
            var girl = parameter as BeautifulGirl;
            if (girl != null)
            {
                Girls.Remove(girl);
            }
        }
    }
    public class BeautifulGirl
    {
        public string Name { get; set; }
    }
}
