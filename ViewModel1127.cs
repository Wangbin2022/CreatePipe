using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace CreatePipe
{
    public class ViewModel1127 : ObserverableObject
    {
        public Document Doc { get; set; }

        //public ObservableCollection<string> SelectedItems { get; set; }=new ObservableCollection<string>();
        //private List<string> selectedItems = new List<string>();
        //public List<string> SelectedItems
        //{
        //    get => selectedItems;
        //    set
        //    {
        //        selectedItems = value;
        //        OnPropertyChanged();
        //    }
        //}
        ////private bool filter3D =true;
        ////public bool Filter3D
        ////{
        ////    get { return filter3D; }
        ////    set
        ////    {
        ////        if (filter3D != value)
        ////        {
        ////            filter3D = value;
        ////            OnPropertyChanged(nameof(Filter3D));
        ////            Filtered3Ds = Filter3DViews(AllViewNames);
        ////        }
        ////    }
        ////}
        ////public ObservableCollection<string> Filtered3Ds { get; set; } = new ObservableCollection<string>();
        ////private ObservableCollection<string> Filter3DViews(List<string> strings)
        ////{
        ////    //if (Filtered3Ds == null)
        ////    //{
        ////    //    Filtered3Ds = Filter3DViews(AllViewNames);
        ////    //}
        ////    //else Filtered3Ds.Clear();
        ////    ObservableCollection<string> collection = new ObservableCollection<string>();
        ////    if (Filter3D)
        ////    {
        ////        foreach (var item in strings)
        ////        {
        ////            if (item.StartsWith("三维："))
        ////            {
        ////                collection.Add(item);
        ////            }
        ////        }
        ////    }
        ////    else collection.Clear();
        ////    return collection;
        ////}

        ////public ObservableCollection<string> FilteredPlans { get; set; } = new ObservableCollection<string>();
        ////public ObservableCollection<string> FilteredElevs { get; set; } = new ObservableCollection<string>();
        ////public ObservableCollection<string> FilteredItems { get; set; } = new ObservableCollection<string>();

        public ViewModel1127(Document document, ref string message)
        {
            Doc = document;
            string localMessage = message;
            localMessage = "This command must be run in a family document.";
            //GetViewNames();
            //SelectCommand = new RelayCommand<List<string>>(SelectCmd);
            //AllViewNames = new List<string> { "三维：视图1", "二维：视图2", "三维：视图3" };
            //Filtered3Ds = Filter3DViews(AllViewNames);
            //FilteredItems = new ObservableCollection<string>(FilteredPlans.Union(FilteredElevs).Union(Filtered3Ds));
        }
        public ICommand TestCommand => new BaseBindingCommand(Test);
        private void Test(object obj)
        {

        }

        ////private List<string> allViewNames;
        ////public List<string> AllViewNames
        ////{
        ////    get { return allViewNames; }
        ////    set
        ////    {
        ////        if (allViewNames != value)
        ////        {
        ////            allViewNames = value;
        ////            OnPropertyChanged(nameof(AllViewNames));
        ////            // 当AllViewNames更新时，也更新Filtered3Ds
        ////            Filtered3Ds = Filter3DViews(allViewNames);
        ////        }
        ////    }
        ////}
        //private void SelectCmd(List<string> list)
        //{
        //    List<string> modifiedList = new List<string>();
        //    StringBuilder sb = new StringBuilder();
        //    foreach (string item in list)
        //    {
        //        modifiedList.Add(item.Substring(3)); // 移除前3个字符
        //    }
        //    foreach (var item in modifiedList)
        //    {
        //        sb.AppendLine(item);
        //    }
        //    TaskDialog.Show("tt", sb.ToString());
        //    //TaskDialog.Show("tt", list.Count().ToString());
        //    //StringBuilder sb = new StringBuilder();
        //    //foreach (string item in list)
        //    //{
        //    //    sb.AppendLine(item);
        //    //}
        //    //TaskDialog.Show("tt", sb.ToString());
        //}
        //public RelayCommand<List<string>> SelectCommand { get; set; }
        //private List<string> allViewNames = new List<string>();
        //public List<string> AllViewNames
        //{
        //    get { return allViewNames; }
        //    set
        //    {
        //        if (allViewNames != value)
        //        {
        //            allViewNames = value;
        //            OnPropertyChanged(nameof(AllViewNames));
        //        }
        //    }
        //}
        ////public List<string> AllViewNames { get; set; } = new List<string>();
        //public void GetViewNames()
        //{
        //    List<View> allViews = GetAllViews();
        //    foreach (View viewItem in allViews)
        //    {
        //        if (viewItem.ViewType == ViewType.ThreeD)
        //        {
        //            allViewNames.Add("三维：" + viewItem.Name);
        //        }
        //        else if (viewItem.ViewType == ViewType.Section || viewItem.ViewType == ViewType.Elevation)
        //        {
        //            allViewNames.Add("立剖面：" + viewItem.Name);
        //        }
        //        else allViewNames.Add(viewItem.Name);
        //        AllViewNames.Sort();
        //    }
        //}
        //private List<View> GetAllViews()
        //{
        //    List<View> allViews = new List<View>();
        //    FilteredElementCollector collector = new FilteredElementCollector(Doc);
        //    IList<Element> Views = collector.OfClass(typeof(View)).ToList();
        //    foreach (var item in Views)
        //    {
        //        View view = item as View;
        //        if (view == null || view.IsTemplate)
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            // 检查视图类型，排除明细表、图纸、图例和面积平面，仅包含平立剖，三维
        //            if (view.ViewType == ViewType.Schedule ||
        //                view.ViewType == ViewType.DrawingSheet ||
        //                view.ViewType == ViewType.Legend ||
        //                view.ViewType == ViewType.AreaPlan)
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                ElementType objType = Doc.GetElement(view.GetTypeId()) as ElementType;
        //                if (objType == null)
        //                {
        //                    continue;
        //                }
        //                allViews.Add(view);
        //            }
        //        }
        //    }
        //    return allViews;
        //}

        ////是这句的问题么？？
        //public CreatePipe.cmd.RelayCommand<ItemModel> Button1Command => new CreatePipe.cmd.RelayCommand<ItemModel>(OnButton1Clicked);
        //private void OnButton1Clicked(ItemModel item)
        //{
        //    if (item != null)
        //    {
        //        // 将当前行的Button2设置为可用
        //        item.OnButton1Click();
        //    }
        //}
    }
    public class StartsWithConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && parameter is string prefix)
            {
                return text.StartsWith(prefix);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //public class ViewModel1127 : ObserverableObject
    //{
    //    public Document Doc { get; set; }
    //    public ViewModel1127(Document doc)
    //    {
    //        Doc = doc;
    //        TestCommand = new BaseBindingCommand(Test);

    //        FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(ParameterFilterElement));
    //        List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
    //        List<ViewFilterModel> viewFilterModels = pfe.Select(pfee => new ViewFilterModel(pfee)).ToList();
    //        foreach (ViewFilterModel item in viewFilterModels)
    //        {
    //            filterModels.Add(item);
    //            filterModelNames.Add(item.ViewFilterName);
    //            //每个ViewFilter查找其包含的Category，循环加入dictionary
    //            //直接添加失败，字典不能使用相同的key
    //            //item.CategoryItems有问题，会把所有过滤器的元素都放进去，需要改
    //            foreach (string categoryString in item.CategoryItems)
    //            {
    //                categoryItems.Add(categoryString);
    //            }
    //            filterCategoryDic.Add(item.ViewFilterName, CategoryItems);
    //        }
    //        StringBuilder sb = new StringBuilder();
    //        foreach (var item in FilterCategoryDic[viewFilterModels.First().ViewFilterName])
    //        {
    //            sb.Append(item.ToString()+"\n");
    //        }
    //        TaskDialog.Show("tt", sb.ToString());
    //        //displayList = viewFilterModels.FirstOrDefault().CategoryItems;
    //    }
    //    //    <ListBox Grid.Row="1" Margin="10" ItemsSource="{Binding FilterCategoryDic}" SelectedValuePath="Key" DisplayMemberPath="Value" SelectedValue="{Binding selectedFilter,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}" />
    //    private void OnSelectedFilterModelChanged()
    //    {
    //        if (FilterCategoryDic.Keys.Contains(SelectedFilterModel))
    //        {
    //            // 更新 ls_category 的 ItemsSource
    //            // 注意：这里假设 FilterCategoryDic 是一个字典，其值是 List<string>
    //            displayList = FilterCategoryDic[SelectedFilterModel];
    //        }
    //    }
    //    private string _selectedFilterModel;
    //    public string SelectedFilterModel
    //    {
    //        get => _selectedFilterModel;
    //        set
    //        {
    //            if (_selectedFilterModel != value)
    //            {
    //                _selectedFilterModel = value;
    //                OnPropertyChanged();
    //                OnSelectedFilterModelChanged();
    //            }
    //        }
    //    }
    //    private List<string> displayList { get; set; }

    //    //private ObservableDictionary<ElementId, List<string>> filterCategoryDic = new ObservableDictionary<ElementId, List<string>>();
    //    private ObservableDictionary<string, List<string>> filterCategoryDic = new ObservableDictionary<string, List<string>>();
    //    private List<string> categoryItems = new List<string>();
    //    public List<string> CategoryItems
    //    {
    //        get => categoryItems;
    //        set
    //        {
    //            categoryItems = value;
    //            OnPropertyChanged();
    //        }
    //    }
    //    private ObservableCollection<string> filterModelNames = new ObservableCollection<string>();
    //    public ObservableCollection<string> FilterModelNames
    //    {
    //        get => filterModelNames;
    //        set
    //        {
    //            filterModelNames = value;
    //            OnPropertyChanged();
    //        }
    //    }
    //    private ObservableCollection<ViewFilterModel> filterModels = new ObservableCollection<ViewFilterModel>();
    //    public ObservableCollection<ViewFilterModel> FilterModels
    //    {
    //        get => filterModels;
    //        set
    //        {
    //            filterModels = value;
    //        }
    //    }
    //    public BaseBindingCommand TestCommand { get; set; }
    //    //public ObservableDictionary<ElementId, List<string>> FilterCategoryDic
    //    public ObservableDictionary<string, List<string>> FilterCategoryDic
    //    {
    //        get => filterCategoryDic;
    //        set
    //        {
    //            filterCategoryDic = value;
    //            OnPropertyChanged("filterCategory");
    //        }
    //    }

    //    public void Test(Object para)
    //    {
    //        TaskDialog.Show("tt", "PASS");
    //    }
    //}
}
