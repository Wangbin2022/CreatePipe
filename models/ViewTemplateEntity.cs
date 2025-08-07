using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class ViewTemplateEntity : ObserverableObject
    {
        public View Viewe { get; set; }
        public Document Document { get => Viewe.Document; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public ViewTemplateEntity(View view)
        {
            Viewe = view;
            viewName = view.Name;
            isTemplate = view.IsTemplate;
            if (view.ViewTemplateId.IntegerValue != -1) hasTemplate = true;
            FilteredElementCollector collector = new FilteredElementCollector(view.Document);
            collector.OfClass(typeof(ViewSheet)); // 获取所有图纸
            foreach (ViewSheet sheet in collector.ToElements())
            {
                if (!Viewport.CanAddViewToSheet(view.Document, sheet.Id, view.Id))
                {
                    // 如果返回 true，说明当前视图已存在于某张图纸中
                    hasSheet = true;
                }
            }
            viewFilterCount = view.GetFilters().Count();
            FilteredElementCollector collector2 = new FilteredElementCollector(Document);
            IEnumerable<View> views = collector2.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>();
            Num = GetNum(view, views);
            Scale = $"1:{view.Scale}";
            Id = view.Id;
            if (Num != 0)
            {
                hasView = true;
            }
            viewType = view.ViewType;
            categoryItems = GetCategoryList();
            if (Viewe is ViewPlan viewPlan && !Viewe.IsTemplate && (Viewe.ViewType == Autodesk.Revit.DB.ViewType.FloorPlan || Viewe.ViewType == Autodesk.Revit.DB.ViewType.CeilingPlan))
            {
                PlanViewRange viewRange = viewPlan.GetViewRange();
                LevelName = Document.GetElement(viewRange.GetLevelId(PlanViewPlane.CutPlane)).Name;
            }
        }
        public string LevelName { get; private set; } = "NA";
        public int viewFilterCount { get; set; }
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
            set => SetProperty(ref categoryItems, value);
        }
        public ViewType viewType { get; set; }
        public bool hasView { get; set; } = false;
        public bool hasTemplate { get; set; } = false;
        public bool hasSheet { get; set; } = false;
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
                _externalHandler.Run(app =>
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
                });
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
                _externalHandler.Run(app =>
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
                });
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
                _externalHandler.Run(app =>
                {
                    Document.NewTransaction(() => Viewe.Name = value, "修改名称");
                    viewName = value;
                    OnPropertyChanged("ViewName");
                    //UpdateSystemCategory(viewName);
                });
            }
        }
    }
}
