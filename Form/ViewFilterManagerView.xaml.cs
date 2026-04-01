using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
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
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// ViewFilterManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFilterManagerView : Window
    {
        public ViewFilterManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ViewFilterManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class ViewFilterManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<ViewFilterEntity>
    {
        public Document Document { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        List<View> _rawViews = new List<View>();
        private ViewFilterEntity _selectedFilter;
        public ViewFilterEntity SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                if (EnableCategoryList && value != null) // Ensure only one is selected and it's not null
                {
                    UpdateCategoryItems(value);
                }
                else if (value == null) // If no view is selected, clear its CategoryItems
                {
                    // Optionally clear CategoryItems of the previous selectedView if needed
                    // For now, let's assume it's fine to leave it as is if it's not currently bound
                }
            }
        }
        // 计算属性，直接判断 RowCount
        public bool EnableCategoryList => RowCount == 1;

        public ViewFilterManagerViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            InitFunc();
        }
        // 从 Helper 获取 SelectedItems (用于 MultiSelectListBox)
        private List<string> _selectedListBoxItems = new List<string>();
        public List<string> SelectedListBoxItems // 重命名以避免与DataGrid的SelectedItems混淆
        {
            get => _selectedListBoxItems;
            set => SetProperty(ref _selectedListBoxItems, value);
        }
        // 从 Helper 获取 RowCount
        private int _rowCount;
        public int RowCount
        {
            get => _rowCount;
            set
            {
                _rowCount = value;
                OnPropertyChanged(nameof(RowCount));
                OnPropertyChanged(nameof(EnableCategoryList));
            }
        }
        // 加载 MultiSelectListBox 的 ItemsSource 待修补
        private void UpdateCategoryItems(ViewFilterEntity entity)
        {
            ////// 性能优化：如果已经加载过，不再重复查找 (可选)
            ////if (entity.CategoryItems != null && entity.CategoryItems.Count > 0) return;
            ////var viewType = entity.Viewe.ViewType;
            ////var names = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>()
            ////    .Where(v => !v.IsTemplate && v.ViewType == viewType)
            ////    .Select(v => v.Name).OrderBy(n => n).ToList();
            ////entity.CategoryItems = names;
        }
        public void InitFunc()
        {
            _rawViews.Clear();
            var views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate);
            _rawViews.AddRange(views);
            QueryElement(null);

        }
        public ICommand SelectViewCommand => new RelayCommand<List<string>>(SelectView);
        private void SelectView(List<string> list)
        {
            List<View> viewList = new List<View>();
            if (SelectedFilter != null)
            {
                List<View> allViews = GetAllViews();
                foreach (string item in list)
                {
                    foreach (View viewItem in allViews)
                    {
                        if (item.Length > 4)
                        {
                            if (viewItem.ViewType == ViewType.ThreeD && viewItem.Name == item.Substring(3))
                            {
                                viewList.Add(viewItem);
                            }
                            else if (viewItem.ViewType == ViewType.Section && viewItem.Name == item.Substring(4))
                            {
                                viewList.Add(viewItem);
                            }
                            else if (viewItem.ViewType == ViewType.Elevation && viewItem.Name == item.Substring(4))
                            {
                                viewList.Add(viewItem);
                            }
                        }
                        else if (viewItem.Name == item)
                        {
                            viewList.Add(viewItem);
                        }
                    }
                }
                Document.NewTransaction(() =>
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
                        List<Element> fplist = new FilteredElementCollector(Document).OfClass(typeof(FillPatternElement)).ToList();
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
        public ICommand SetColorCommand => new RelayCommand<ViewFilterEntity>(SetColor);
        private void SetColor(ViewFilterEntity viewFilterModel)
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

        public RelayCommand<ViewFilterEntity> SelectCommand => new RelayCommand<ViewFilterEntity>(SelectElements);
        private void SelectElements(ViewFilterEntity model)
        {
            //待实现
            throw new NotImplementedException();
        }
        public RelayCommand<ViewFilterEntity> HideInViewCommand => new RelayCommand<ViewFilterEntity>(HideInViews);
        private void HideInViews(ViewFilterEntity viewFilter)
        {
            //TaskDialog.Show("tt", viewFilter.ViewFilterName);
            Document.NewTransaction(() =>
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
        public RelayCommand<ViewFilterEntity> ApplyToViewsCommand => new RelayCommand<ViewFilterEntity>(ApplyToViews);
        private void ApplyToViews(ViewFilterEntity viewFilter)
        {
            Document.NewTransaction(() =>
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
                            List<Element> fplist = new FilteredElementCollector(Document).OfClass(typeof(FillPatternElement)).ToList();
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
            FilteredElementCollector collector = new FilteredElementCollector(Document);
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
                        ElementType objType = Document.GetElement(view.GetTypeId()) as ElementType;
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
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            throw new NotImplementedException();
        }
        public void DeleteElement(IList selectedElements)
        {
            Document.NewTransaction(() =>
            {
                for (int i = selectedElements.Count - 1; i >= 0; i--)
                {
                    ViewFilterEntity delFilters = selectedElements[i] as ViewFilterEntity;
                    Document.Delete(delFilters.Id);
                    Collection.Remove(delFilters);
                }
            }, "删除过滤器");
            QueryElement(null);
        }
        public ICommand DeleteElementCommand => new RelayCommand<IList>(DeleteElement);
        public void DeleteElement(ViewFilterEntity entity)
        {
            throw new NotImplementedException();
        }
        public bool CheckName(ICollection<string> names)
        {
            bool result;
            //names = FilterModelNames;
            //////string newName = Keyword;
            //if (String.IsNullOrEmpty(newName))
            //{
            //    return result = false;
            //}
            ////// Check if filter name contains invalid characters
            ////// These character are different from Path.GetInvalidFileNameChars()
            //char[] invalidFileChars = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };
            //foreach (char invalidChr in invalidFileChars)
            //{
            //    if (newName.Contains(invalidChr))
            //    {
            //        return result = false;
            //    }
            //}
            //// Check if name is used
            //// check if name is already used by other filters
            //bool inUsed = names.Contains(newName, StringComparer.OrdinalIgnoreCase);
            //if (inUsed)
            //{
            //    return result = false;
            //}
            return result = true;
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            Collection.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(Document).OfClass(typeof(ParameterFilterElement));
            List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
            List<ViewFilterEntity> viewFilterModels = pfe.Select(pfee => new ViewFilterEntity(pfee))
                .Where(e => string.IsNullOrEmpty(text) || e.ViewFilterName.Contains(text)).ToList();
            foreach (ViewFilterEntity item in viewFilterModels)
            {
                Collection.Add(item);
                filterModelNames.Add(item.ViewFilterName);
            }
            OnPropertyChanged(nameof(FilterCount));
        }
        public string FilterCount => Collection.Count.ToString();
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
        public ObservableCollection<ViewFilterEntity> Collection { get; set; } = new ObservableCollection<ViewFilterEntity>();
    }
}
