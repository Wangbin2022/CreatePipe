using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using CreatePipe.ViewFilters;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe
{
    public class ViewTemplateManagerViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public ViewTemplateManagerViewModel(UIApplication application)
        {
            Doc = application.ActiveUIDocument.Document;
            views = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().ToList();
            viewTemplates = GetEntity();
            //GetViewNames();
        }

        public ICommand ApplyToViewCommand => new RelayCommand<IEnumerable<object>>(ApplyToView);
        private void ApplyToView(IEnumerable<object> enumerable)
        {
            Doc.NewTransaction(() =>
            {
                List<View> viewNames = new List<View>();
                foreach (var item in views)
                {
                    if (item.ViewType == selectedView.viewType)
                    {
                        foreach (var item2 in enumerable)
                        {                            
                            if (item.Name == item2.ToString())
                            {
                                item.ViewTemplateId = selectedView.Id;
                            }
                        }
                    }
                }
            }, "给视图添加样板");
            TaskDialog.Show("tt", $"已为{enumerable.Count()}个视图添加该样板");
        }
        private ViewTemplate selectedView;
        public ViewTemplate SelectedView
        {
            get => selectedView;
            set
            {
                if (selectedView != value)
                {
                    selectedView = value;
                    OnPropertyChanged(nameof(SelectedView));
                }
            }
        }
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
            DependencyProperty.RegisterAttached("AttProp", typeof(bool), typeof(ViewTemplateManagerViewModel), new PropertyMetadata(false, OnPropChanged));
        public static bool GetAttProp(DependencyObject obj) => (bool)obj.GetValue(AttPropProperty);
        public static void SetAttProp(DependencyObject obj, bool par) => obj.SetValue(AttPropProperty, par);
        private static void OnPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dg = d as DataGrid;
            if (dg == null) return;
            dg.SelectionChanged += (s, mbe) => ((ViewTemplateManagerViewModel)(dg.DataContext)).RowCount = (int)(dg.SelectedItems).Count;
        }
        #endregion
        public bool IsSelected { get; set; } = false;
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
        public ICommand FindViewsCommand => new RelayCommand<ViewTemplate>(FindViews);
        private void FindViews(ViewTemplate vt)
        {
            List<View> viewsInUse = new List<View>();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (View item in views)
            {
                if (item.ViewTemplateId == vt.Id)
                {
                    stringBuilder.AppendLine(item.Name);
                    viewsInUse.Add(item);
                }
            }
            Doc.NewTransaction(() =>
            {
                TaskDialog td = new TaskDialog("tt");
                td.MainInstruction = stringBuilder.ToString();
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "清除以上视图的样板设置");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "保持视图的样板设置");
                TaskDialogResult dialogResult = td.Show();
                if (dialogResult == TaskDialogResult.CommandLink1)
                {
                    foreach (var item in viewsInUse)
                    {
                        /////1. Using Direct Property of View
                        item.ViewTemplateId = ElementId.InvalidElementId;
                        /////2. Using its Parameter
                        //v.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(ElementId.InvalidElementId);
                    }
                }
            }, "更改视图样板");
        }
        public ICommand DeleteELementCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
        private void DeleteELements(IEnumerable<object> selectedElements)
        {
            List<ViewTemplate> selectedItems = selectedElements.Cast<ViewTemplate>().ToList();
            if (selectedElements == null) return;
            foreach (var item in selectedItems)
            {
                DeleteElement(item);
            }
        }
        public void DeleteElement(ViewTemplate vt)
        {
            if (vt.Num == 0)
            {
                Doc.NewTransaction(() =>
                {
                    Doc.Delete(vt.Id);
                    ViewTemplates.Remove(vt);
                }, "删除视图样板");
                OnPropertyChanged(nameof(ViewTemplateCount));
            }
        }
        public int ViewTemplateCount => ViewTemplates.Count;
        public ICommand QueryELementCommand => new BaseBindingCommand(QueryELement);
        private void QueryELement(object obj)
        {
            ViewTemplates.Clear();
            ViewTemplates = GetEntity();
        }
        private ObservableCollection<ViewTemplate> GetEntity()
        {
            ObservableCollection<ViewTemplate> vts = new ObservableCollection<ViewTemplate>();     
            List<ViewTemplate> cableSystems = views.Select(v => new ViewTemplate(v)).Where(e => e.isTemplate == true && (string.IsNullOrEmpty(Keyword) || e.ViewName.Contains(Keyword) || e.ViewName.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            foreach (var item in cableSystems)
            {
                vts.Add(item);
            }
            return vts;
        }
        public List<View> views =new List<View>();
        private ObservableCollection<ViewTemplate> viewTemplates;
        public ObservableCollection<ViewTemplate> ViewTemplates
        {
            get => viewTemplates;
            set
            {
                viewTemplates = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ViewTemplateCount));
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }
    }

    public class ViewTemplate : ObserverableObject
    {
        public View Viewe { get; set; }
        public Document Document { get => Viewe.Document; }
        public ViewTemplate(View view)
        {
            Viewe = view;
            viewName = view.Name;
            isTemplate = view.IsTemplate;
            FilteredElementCollector collector = new FilteredElementCollector(Document);
            IEnumerable<View> views = collector.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>();
            Num = GetNum(view, views);
            Scale = $"1:{view.Scale}";
            Id = view.Id;
            if (Num != 0)
            {
                hasView = true;
            }
            viewType = view.ViewType;
            categoryItems = GetCategoryList();
        }
        private List<string> GetCategoryList()
        {
            List<string> views = new List<string>();
            List<View> allViews = new FilteredElementCollector(Document).OfClass(typeof(View)).Cast<View>().ToList();
            foreach (View viewItem in allViews)
            {
                if (viewItem.ViewType == Viewe.ViewType && !viewItem.IsTemplate)
                {
                    switch (viewItem.ViewType)
                    {
                        case Autodesk.Revit.DB.ViewType.ThreeD:
                        case Autodesk.Revit.DB.ViewType.Section:
                        case Autodesk.Revit.DB.ViewType.Elevation:
                        case Autodesk.Revit.DB.ViewType.CeilingPlan:
                        default:
                            views.Add(viewItem.Name);
                            break;
                    }
                }
                views.Sort();
            }
            return views;
        }
        public List<string> categoryItems = new List<string>();
        public List<string> CategoryItems
        {
            get => categoryItems;
            set
            {
                categoryItems = value;
                OnPropertyChanged();
            }
        }
        public ViewType viewType { get; set; }
        public bool hasView { get; set; } = false;
        public string ViewDetailElem
        {
            get
            {
                string dsc;
                switch (Viewe.DetailLevel.ToString())
                {
                    case "Fine":
                        dsc = "精细";
                        break;
                    case "Medium":
                        dsc = "中等";
                        break;
                    default:
                        dsc = "粗糙";
                        break;
                }
                return dsc;
            }
            set
            {
                Document.NewTransaction(() =>
                {
                    switch (value)
                    {
                        case "粗糙":
                            Viewe.DetailLevel = ViewDetailLevel.Coarse;
                            break;
                        case "中等":
                            Viewe.DetailLevel = ViewDetailLevel.Medium;
                            break;
                        case "精细":
                            Viewe.DetailLevel = ViewDetailLevel.Fine;
                            break;
                        default:
                            Viewe.DetailLevel = ViewDetailLevel.Fine;
                            break;
                    }
                }, "修改视图细节");
            }
        }
        public List<string> ViewDetails { get; } = new List<string> { "精细", "中等", "粗糙" };
        public string ViewDisplayElem
        {
            get
            {
                string dsc;
                switch (Viewe.DisplayStyle.ToString())
                {
                    case "Wireframe":
                        dsc = "线框";
                        break;
                    case "HLR":
                        dsc = "隐藏线";
                        break;
                    case "ShadingWithEdges":
                        dsc = "着色";
                        break;
                    case "Shading":
                        dsc = "一致的颜色";
                        break;
                    default:
                        dsc = "真实";
                        break;
                }
                return dsc;
            }
            set
            {
                Document.NewTransaction(() =>
                {
                    switch (value)
                    {
                        case "线框":
                            Viewe.DisplayStyle = DisplayStyle.Wireframe;
                            break;
                        case "隐藏线":
                            Viewe.DisplayStyle = DisplayStyle.HLR;
                            break;
                        case "着色":
                            Viewe.DisplayStyle = DisplayStyle.ShadingWithEdges;
                            break;
                        case "一致的颜色":
                            Viewe.DisplayStyle = DisplayStyle.Shading;
                            break;
                        case "真实":
                            Viewe.DisplayStyle = DisplayStyle.Realistic;
                            break;
                        default:
                            Viewe.DisplayStyle = DisplayStyle.Realistic;
                            break;
                    }
                }, "修改视图样式");
            }
        }
        public List<string> ViewDisplay { get; } = new List<string> { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
        public string Scale { get; set; }
        public string ViewDiscipline
        {
            get
            {
                string dsc;
                switch (Viewe.Discipline.ToString())
                {
                    case "Coordination":
                        dsc = "协调";
                        break;
                    case "Architectural":
                        dsc = "建筑";
                        break;
                    case "Plumbing":
                        dsc = "水";
                        break;
                    case "Mechanical":
                        dsc = "暖通";
                        break;
                    case "Electrical":
                        dsc = "电气";
                        break;
                    default:
                        dsc = "结构";
                        break;
                }
                return dsc;
            }
        }
        public ElementId Id { get; set; }
        public string ViewType
        {
            get
            {
                string vt;
                switch (Viewe.ViewType.ToString())
                {
                    case "ThreeD":
                        vt = "三维视图";
                        break;
                    case "Elevation":
                        vt = "立面视图";
                        break;
                    case "Section":
                        vt = "剖面视图";
                        break;
                    case "CeilingPlan":
                        vt = "天顶视图";
                        break;
                    default:
                        vt = "平面视图";
                        break;
                }
                return vt;
            }
        }
        private int GetNum(View view, IEnumerable<View> views)
        {
            int num = 0;
            foreach (View item in views)
            {
                if (item.ViewTemplateId.IntegerValue != -1)
                {
                    ElementId templateId = item.ViewTemplateId;
                    if (templateId == view.Id)
                    {
                        num++;
                    }
                }
            }
            return num;
        }
        public int Num { get; set; }
        public bool isTemplate { get; set; }
        private string viewName;
        public string ViewName
        {
            get { return viewName; }
            set
            {
                Document.NewTransaction(() => Viewe.Name = value, "修改名称");
                viewName = value;
                OnPropertyChanged("ViewName");
                //UpdateSystemCategory(viewName);
            }
        }
    }

}
