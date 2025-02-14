using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;


namespace CreatePipe.ViewFilters
{
    public class ViewFilterViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public ViewFilterViewModel(UIApplication uiApp)
        {
            Doc = uiApp.ActiveUIDocument.Document;
            QueryElement();
            QueryELementCommand = new BaseBindingCommand(ExecuteQueryElementCommand);
            NewFilterCommand = new RelayCommand<ICollection<string>>(NewFilter);
            DeleteFilterCommand = new RelayCommand<IList>(DeleteFilter);
            SetColorCommand = new RelayCommand<ViewFilterModel>(SetColor);
            GetViewNames();
            SelectViewCommand = new RelayCommand<List<string>>(SelectView);
        }

        private void SelectView(List<string> list)
        {
            //StringBuilder sb = new StringBuilder();
            //foreach (string item in list) { sb.AppendLine(item); }
            //TaskDialog.Show("tt", sb.ToString());
            List<View> viewList = new List<View>();
            if (SelectedFilter != null)
            {
                List<View> allViews = GetAllViews();
                foreach (var item in list)
                {
                    foreach (View viewItem in allViews)
                    {
                        if (viewItem.ViewType == ViewType.ThreeD && viewItem.Name == item.Substring(3))
                        {
                            viewList.Add(viewItem);
                        }
                        else if ((viewItem.ViewType == ViewType.Section || viewItem.ViewType == ViewType.Elevation) && viewItem.Name == item.Substring(4))
                        {
                            viewList.Add(viewItem);
                        }
                        else if (viewItem.Name == item)
                        {
                            viewList.Add(viewItem);
                        }
                    }
                }
                Doc.NewTransaction(() =>
                {
                    foreach (View view in viewList)
                    {
                        if (view.GetFilters().Contains(SelectedFilter.Id))
                        {
                            view.RemoveFilter(SelectedFilter.Id);
                        }
                        view.AddFilter(SelectedFilter.Id);
                        OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                        ogs.SetSurfaceTransparency(SelectedFilter.TransparencyNum);
                        ogs.SetProjectionLineWeight(1);
                        List<Element> fplist = new FilteredElementCollector(Doc).OfClass(typeof(FillPatternElement)).ToList();
                        ElementId solidId = fplist.FirstOrDefault(x => (x as FillPatternElement).GetFillPattern().IsSolidFill)?.Id;
                        ogs.SetSurfaceForegroundPatternId(solidId);
                        if (SelectedFilter.Color != null)
                        {
                            ogs.SetProjectionLineColor(SelectedFilter.Color);
                            ogs.SetSurfaceForegroundPatternColor(SelectedFilter.Color);
                        }
                        else
                        {
                            ogs.SetProjectionLineColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                            ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                        }
                        view.SetFilterOverrides(SelectedFilter.Id, ogs);

                    }
                }, "选定视图附加/删除过滤器");
            }
        }
        public RelayCommand<List<string>> SelectViewCommand { get; set; }
        private List<string> selectedItems = new List<string>();
        public List<string> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                OnPropertyChanged();
            }
        }
        private List<string> allViewNames = new List<string>();
        public List<string> AllViewNames
        {
            get { return allViewNames; }
            set
            {
                if (allViewNames != value)
                {
                    allViewNames = value;
                    OnPropertyChanged(nameof(AllViewNames));
                }
            }
        }
        //public List<string> AllViewNames {  get; set; }=new List<string>();
        public void GetViewNames()
        {
            List<View> allViews = GetAllViews();
            foreach (View viewItem in allViews)
            {
                if (viewItem.ViewType == ViewType.ThreeD)
                {
                    allViewNames.Add("三维：" + viewItem.Name);
                }
                else if (viewItem.ViewType == ViewType.Section || viewItem.ViewType == ViewType.Elevation)
                {
                    allViewNames.Add("立剖面：" + viewItem.Name);
                }
                else allViewNames.Add(viewItem.Name);
                AllViewNames.Sort();
            }
        }
        private void SetColor(ViewFilterModel viewFilterModel)
        {
            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.AllowFullOpen = true;
            dialog.FullOpen = true;
            dialog.ShowHelp = true;
            dialog.Color = System.Drawing.Color.FromArgb(127, 127, 127);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                viewFilterModel.Color = dialog.Color.ConvertToRevitColor();
            }
        }
        public RelayCommand<ViewFilterModel> SetColorCommand { get; set; }
        public RelayCommand<ViewFilterModel> SelectCommand => new RelayCommand<ViewFilterModel>(SelectElements);
        private void SelectElements(ViewFilterModel model)
        {
            //待实现
            throw new NotImplementedException();
        }
        public RelayCommand<ViewFilterModel> HideInViewCommand => new RelayCommand<ViewFilterModel>(HideInViews);
        private void HideInViews(ViewFilterModel viewFilter)
        {
            //TaskDialog.Show("tt", viewFilter.ViewFilterName);
            Doc.NewTransaction(() =>
            {
                List<View> allViews = GetAllViews();
                foreach (View view in allViews)
                {
                    if (view.GetFilters().Contains(viewFilter.Id))
                    {
                        view.SetFilterVisibility(viewFilter.Id, false);
                    }
                }
            }, "所有视图隐藏该类");
        }
        public RelayCommand<ViewFilterModel> ApplyToViewsCommand => new RelayCommand<ViewFilterModel>(ApplyToViews);
        private void ApplyToViews(ViewFilterModel viewFilter)
        {
            Doc.NewTransaction(() =>
            {
                List<View> allViews = GetAllViews();
                int usingViews = 0;
                foreach (View view in allViews)
                {
                    //要分两种情况，所有的视图都有或仅部分视图中应用，第一次应该全部应用 
                    //如果所有视图都含的话应用到所有即清除可用
                    if (view.GetFilters().Contains(viewFilter.Id))
                    {
                        usingViews++;
                    }
                }
                if (usingViews == allViews.Count)
                {
                    foreach (View view in allViews)
                    {
                        view.RemoveFilter(viewFilter.Id);
                        viewFilter.IsInUsing = false;
                        viewFilter.IsHideBtn = false;
                    }
                }
                else
                {
                    foreach (View view in allViews)
                    {
                        if (!view.GetFilters().Contains(viewFilter.Id))
                        {
                            view.AddFilter(viewFilter.Id);
                            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                            ogs.SetSurfaceTransparency(viewFilter.TransparencyNum);
                            ogs.SetProjectionLineWeight(1);
                            List<Element> fplist = new FilteredElementCollector(Doc).OfClass(typeof(FillPatternElement)).ToList();
                            ElementId solidId = fplist.FirstOrDefault(x => (x as FillPatternElement).GetFillPattern().IsSolidFill)?.Id;
                            ogs.SetSurfaceForegroundPatternId(solidId);
                            if (viewFilter.Color != null)
                            {
                                ogs.SetProjectionLineColor(viewFilter.Color);
                                ogs.SetSurfaceForegroundPatternColor(viewFilter.Color);
                            }
                            else
                            {
                                ogs.SetProjectionLineColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                                ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                            }
                            view.SetFilterOverrides(viewFilter.Id, ogs);
                            viewFilter.IsInUsing = true;
                            viewFilter.IsHideBtn = true;
                        }
                    }
                }
            }, "所有视图附加/删除过滤器");
        }
        private List<View> GetAllViews()
        {
            List<View> allViews = new List<View>();
            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            IList<Element> Views = collector.OfClass(typeof(View)).ToList();
            foreach (var item in Views)
            {
                View view = item as View;
                if (view == null || view.IsTemplate)
                {
                    continue;
                }
                else
                {
                    // 检查视图类型，排除明细表、图纸、图例和面积平面，仅包含平立剖，三维
                    if (view.ViewType == ViewType.Schedule ||
                        view.ViewType == ViewType.DrawingSheet ||
                        view.ViewType == ViewType.Legend ||
                        view.ViewType == ViewType.AreaPlan)
                    {
                        continue;
                    }
                    else
                    {
                        ElementType objType = Doc.GetElement(view.GetTypeId()) as ElementType;
                        if (objType == null)
                        {
                            continue;
                        }
                        allViews.Add(view);
                    }
                }
            }
            return allViews;
        }
        private ViewFilterModel selectedFilter;
        public ViewFilterModel SelectedFilter
        {
            get => selectedFilter;
            set
            {
                selectedFilter = value;
                OnPropertyChanged();
            }
        }
        private bool _enableCategoryList;
        public bool EnableCategoryList
        {
            get => _enableCategoryList;
            set
            {
                if (_enableCategoryList != value)
                {
                    _enableCategoryList = value;
                    OnPropertyChanged(nameof(EnableCategoryList));
                }
            }
        }
        private int _rowCount;
        public int RowCount
        {
            get => this._rowCount;
            set
            {
                this._rowCount = value;
                EnableCategoryList = value == 1;
                OnPropertyChanged();
            }
        }
        #region attached property
        public static readonly DependencyProperty AttPropProperty =
            DependencyProperty.RegisterAttached("AttProp", typeof(bool), typeof(ViewFilterViewModel), new PropertyMetadata(false, OnPropChanged));
        public static bool GetAttProp(DependencyObject obj) => (bool)obj.GetValue(AttPropProperty);
        public static void SetAttProp(DependencyObject obj, bool par) => obj.SetValue(AttPropProperty, par);
        private static void OnPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dg = d as DataGrid;
            if (dg == null) return;
            dg.SelectionChanged += (s, mbe) => ((ViewFilterViewModel)(dg.DataContext)).RowCount = (int)(dg.SelectedItems).Count;
        }
        #endregion
        public void DeleteFilter(IList selectedElements)
        {
            Doc.NewTransaction(() =>
            {
                for (int i = selectedElements.Count - 1; i >= 0; i--)
                {
                    ViewFilterModel delFilters = selectedElements[i] as ViewFilterModel;
                    Doc.Delete(delFilters.Id);
                    FilterModels.Remove(delFilters);
                }
            }, "删除过滤器");
            QueryElement();
        }
        public RelayCommand<IList> DeleteFilterCommand { get; set; }
        public void NewFilter(ICollection<string> names)
        {
            names = FilterModelNames;
            NewFilterForm newFilterForm;
            // 如果窗口已经存在，此处关闭原窗口并重开~~
            IntPtr maindHwnd = WindowHelper.FindWindow(null, "NewFilterForm");//主窗口标题
            if (maindHwnd != IntPtr.Zero)
            {
                WindowHelper.PostMessage(maindHwnd, WindowHelper.WM_CLOSE, 0, 0);
            }
            else
            {
                newFilterForm = new NewFilterForm(names);
                bool? result = newFilterForm.ShowDialog();
                if (result == true)
                {
                    // 子窗口确认修改，更新主窗口的属性,为什么ListBox里没有实时更新
                    using (Transaction ts = new Transaction(Doc, "新增过滤器"))
                    {
                        ts.Start();
                        ParameterFilterElement.Create(Doc, newFilterForm.FilterName, new List<ElementId>());
                        //Doc.Regenerate();
                        ts.Commit();
                    }
                    QueryElement();
                }
            }
            return;
        }
        public RelayCommand<ICollection<string>> NewFilterCommand { get; set; }
        public BaseBindingCommand QueryELementCommand { get; set; }
        private void ExecuteQueryElementCommand(object parameter)
        {
            QueryElement();
        }
        public void QueryElement()
        {
            FilterModels.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(ParameterFilterElement));
            List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
            List<ViewFilterModel> viewFilterModels = pfe.Select(pfee => new ViewFilterModel(pfee))
                .Where(e => string.IsNullOrEmpty(Keyword) || e.ViewFilterName.Contains(Keyword)).ToList();
            foreach (ViewFilterModel item in viewFilterModels)
            {
                FilterModels.Add(item);
                filterModelNames.Add(item.ViewFilterName);
            }
            OnPropertyChanged(nameof(FilterCount));
        }
        public string FilterCount => FilterModels.Count.ToString();
        private List<string> filterModelNames = new List<string>();
        public List<string> FilterModelNames
        {
            get => filterModelNames;
            set
            {
                filterModelNames = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<ViewFilterModel> filterModels = new ObservableCollection<ViewFilterModel>();
        public ObservableCollection<ViewFilterModel> FilterModels
        {
            get => filterModels;
            set
            {
                filterModels = value;
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }
    }
    public class BackgroundToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Brush brush)                // 确保传入的是Brush类型
            {
                var solidColorBrush = brush as SolidColorBrush;                // 获取颜色的SolidColorBrush
                if (solidColorBrush != null)
                {
                    System.Windows.Media.Color color = solidColorBrush.Color;
                    double red = color.R / 255.0;
                    double green = color.G / 255.0;
                    double blue = color.B / 255.0;
                    double luma = red * 0.2126 + green * 0.7152 + blue * 0.0722;
                    // 如果亮度大于0.5，则返回黑色，否则返回白色
                    return luma > 0.45 ? new SolidColorBrush(System.Windows.Media.Colors.Black) : new SolidColorBrush(System.Windows.Media.Colors.White);
                }
            }
            return new SolidColorBrush(System.Windows.Media.Colors.White); // 默认返回黑色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
