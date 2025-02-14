using Autodesk.Revit.DB;
using CreatePipe.Models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;


namespace CreatePipe.Form
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Document document)
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(document);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MaterialEntityModel> _materialEntityModels;
        public ObservableCollection<MaterialEntityModel> MaterialEntityModels
        {
            get { return _materialEntityModels; }
            set
            {
                if (_materialEntityModels != value)
                {
                    _materialEntityModels = value;
                    OnPropertyChanged(nameof(MaterialEntityModels));
                }
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }

        public string MaterialCount => MaterialEntityModels.Count.ToString();
        private Document _document;
        public Document Document
        {
            get { return _document; }
            set
            {
                _document = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand<Document> QueryELementCommand { get; set; }

        public ICommand DeleteELementCommand { get; private set; }
        public ICommand DeleteELementCommand2 { get; private set; }
        public MainViewModel(Document document)
        {
            _document = document;
            MaterialEntityModels = new ObservableCollection<MaterialEntityModel>(
                      new FilteredElementCollector(document).OfClass(typeof(Material))
                      .Cast<Material>().Select(material => new MaterialEntityModel(material)));
            QueryELementCommand = new RelayCommand<Document>(QueryElement);
            DeleteELementCommand = new RelayCommand<IEnumerable<object>>(DeleteElements);
            DeleteELementCommand2 = new RelayCommand<MaterialEntityModel>(DeleteElement);
        }

        //多选删除方法
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            Document document = _document;
            List<MaterialEntityModel> selectedItems = selectedElements.Cast<MaterialEntityModel>().ToList();
            if (selectedElements == null) return;
            document.NewTransaction(() =>
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    MaterialEntityModel material = selectedItems[i] as MaterialEntityModel;
                    document.Delete(material.Material.Id);
                    MaterialEntityModels.Remove(material);
                }
            }, "删除多材质");
            OnPropertyChanged(nameof(MaterialCount));
        }
        //单选删除方法
        public void DeleteElement(MaterialEntityModel material)
        {
            Document document = _document;
            document.NewTransaction(() =>
            {
                document.Delete(material.Material.Id);
                MaterialEntityModels.Remove(material);
            }, "删除材质");
            OnPropertyChanged(nameof(MaterialCount));
        }

        public void QueryElement(Document doc)
        {
            MaterialEntityModels.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(doc).OfClass(typeof(Material));
            var materials = elements.ToList()
                .ConvertAll(x => new MaterialEntityModel(x as Material))
                .Where(e => string.IsNullOrEmpty(Keyword) || e.Name.Contains(Keyword));
            foreach (var item in materials)
            {
                MaterialEntityModels.Add(item);
            }
            OnPropertyChanged(nameof(MaterialCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //[Transaction(TransactionMode.Manual)]
    //public class Test3 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;

    //        MainWindow mainWindow = new MainWindow(doc);
    //        mainWindow.ShowDialog();

    //        return Result.Succeeded;
    //    }
    //}

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
