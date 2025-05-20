using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;
using View= Autodesk.Revit.DB.View;

namespace CreatePipe
{
    /// <summary>
    /// PropertiesForm.xaml 的交互逻辑
    /// </summary>
    public partial class PropertiesForm : Window
    {
        public double NumericValue { get; set; }
        public PropertiesForm(UIDocument uiDoc)
        {
            InitializeComponent();
            this.DataContext = new PropertiesViewModel(uiDoc);
            //Bitmap bitmap = new Bitmap(); 
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    //文本框数值
    public class NumberValidationRule : ValidationRule
    {
        public double Minimum { get; set; } = double.MinValue;
        public double Maximum { get; set; } = double.MaxValue;
        public override ValidationResult Validate(object value, CultureInfo culture)
        {
            if (double.TryParse(value.ToString(), out double num))
            {
                if (num >= Minimum && num <= Maximum)
                    return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, $"请输入 {Minimum} 到 {Maximum} 之间的数字");
        }
    }
    public class PropertiesViewModel:ObserverableObject
    {
        Document Document { get; set; }
        public List<View> views = new List<View>();
        public PropertiesViewModel(UIDocument uiDoc)
        {
            Document = uiDoc.Document;
            views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().ToList();
            ViewCount = views.Count;
        }
        public ICommand QueryELementCommand => new BaseBindingCommand(QueryELement);
        private void QueryELement(object obj)
        {
            ViewCount = 0;
            ObservableCollection<ViewTemplate> vts = new ObservableCollection<ViewTemplate>();
            List<ViewTemplate> cableSystems = views.Select(v => new ViewTemplate(v)).Where(e => e.isTemplate == true && (string.IsNullOrEmpty(Keyword) || e.ViewName.Contains(Keyword) || e.ViewName.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            ViewCount = cableSystems.Count;
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }
        public int ViewCount { get; set; }
    }

}
