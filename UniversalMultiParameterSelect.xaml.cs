using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
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

namespace CreatePipe
{
    /// <summary>
    /// UniversalMultiParameterSelect.xaml 的交互逻辑
    /// </summary>
    public partial class UniversalMultiParameterSelect : Window
    {
        public List<string> SelectedParameters { get; private set; } = new List<string>();
        public UniversalMultiParameterSelect(FamilyInstance instance, string title = "选择参数")
        {
            InitializeComponent();
            this.Title = title;
            LoadParameters(instance);
        }
        private void LoadParameters(FamilyInstance instance)
        {
            var parameters = new List<ParameterItem>();
            foreach (Parameter param in instance.Parameters)
            {
                if (param.IsReadOnly) continue;

                parameters.Add(new ParameterItem
                {
                    Name = param.Definition.Name,
                    IsSelected = false
                });
            }
            ParametersListView.ItemsSource = parameters;
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ((List<ParameterItem>)ParametersListView.ItemsSource)
                .Where(p => p.IsSelected)
                .Select(p => p.Name)
                .ToList();

            SelectedParameters = selectedItems;
            this.DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
    public class ParameterItem
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
