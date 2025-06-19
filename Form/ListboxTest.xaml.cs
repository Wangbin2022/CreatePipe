using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Shapes;

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
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
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
                    Name ="Write Sth",
                },
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
                    Name ="凤姐",
                }
            };
            if (Girls.Count > 0)
            {
                SelectedGirl = Girls[0];
            }
        }
        private BeautifulGirl _selectedGirl;
        public BeautifulGirl SelectedGirl
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
