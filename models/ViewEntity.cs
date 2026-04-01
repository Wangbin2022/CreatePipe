using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using View = Autodesk.Revit.DB.View;

namespace CreatePipe.models
{
    public class ViewEntity : ObserverableObject
    {
        private readonly BaseExternalHandler _handler;
        public View Viewe { get; }
        public ElementId Id { get; }
        public Document Document => Viewe.Document;
        public bool HasSheet { get; private set; } // 标识是否在图纸上
        public ViewEntity(View view, BaseExternalHandler handler, bool onSheet, int usageCount)
        {
            Viewe = view;
            Id = view.Id;
            _handler = handler;
            _viewName = view.Name;
            HasSheet = onSheet; // 由 ViewModel 预处理后传入
            if (view.GenLevel != null)
            {
                LevelName = view.GenLevel.Name;
            }
            viewFilterCount = view.GetFilters().Count().ToString();
            if (view.IsTemplate)
            {
                templateUsageCount = usageCount;
            }
        }
        private List<string> _categoryItems = new List<string>();
        public List<string> CategoryItems
        {
            get => _categoryItems;
            set => SetProperty(ref _categoryItems, value);
        }
        public bool hasView => templateUsageCount > 0;
        public int templateUsageCount { get; set; } = 0;
        public string viewFilterCount { get; set; }
        public string ViewDiscipline
        {
            get
            {
                // 1. 安全检查：判断该视图是否包含“规程”参数
                // 只有包含该参数的视图，才能安全调用 Viewe.Discipline
                Parameter disciplineParam = Viewe.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE);
                if (disciplineParam == null)
                {
                    return "NA"; // 不存在规程属性的视图（如明细表等）直接返回 NA
                }
                // 2. 获取并转换规程
                try
                {
                    // 直接使用 Revit 的 ViewDiscipline 枚举进行判断，性能更好
                    switch (Viewe.Discipline)
                    {
                        case Autodesk.Revit.DB.ViewDiscipline.Coordination:
                            return "协调";
                        case Autodesk.Revit.DB.ViewDiscipline.Architectural:
                            return "建筑";
                        case Autodesk.Revit.DB.ViewDiscipline.Plumbing:
                            return "供排水";
                        case Autodesk.Revit.DB.ViewDiscipline.Mechanical:
                            return "暖通";
                        case Autodesk.Revit.DB.ViewDiscipline.Electrical:
                            return "电气";
                        case Autodesk.Revit.DB.ViewDiscipline.Structural:
                            return "结构";
                        default:
                            return "NA"; // 处理未知或其他情况
                    }
                }
                catch
                {
                    // 兜底防护：防止极个别损坏的视图在获取 Discipline 属性时抛出异常
                    return "NA";
                }
            }
        }
        private string GetViewTypeChinese(ViewType type)
        {
            switch (type)
            {
                case Autodesk.Revit.DB.ViewType.ThreeD: return "三维视图";
                case Autodesk.Revit.DB.ViewType.Section: return "剖面视图";
                case Autodesk.Revit.DB.ViewType.Elevation: return "立面视图";
                case Autodesk.Revit.DB.ViewType.CeilingPlan: return "天花投影";
                case Autodesk.Revit.DB.ViewType.FloorPlan: return "平面视图";
                default: return type.ToString();
            }
        }
        //视图细节属性处理
        // 提供给 ComboBox 的下拉选项列表
        public List<string> ViewDetails { get; } = new List<string> { "粗糙", "中等", "精细" };
        // 判断该视图是否支持修改细节程度（防止明细表、图纸等报错）
        // 1. 提取一个通用的、极其准确的判定方法
        private bool CanModifyParameter(BuiltInParameter bip)
        {
            // a) 检查视图本身有没有这个参数（排除明细表、图纸等）
            Parameter param = Viewe.get_Parameter(bip);
            if (param == null) return false;

            // b) 检查参数是否被系统本身硬性设为只读
            if (param.IsReadOnly) return false;

            // c) 核心填坑：检查是否被【视图样板】锁定
            if (Viewe.ViewTemplateId != ElementId.InvalidElementId)
            {
                View template = Document.GetElement(Viewe.ViewTemplateId) as View;
                if (template != null)
                {
                    // 获取样板中【未勾选】（即不控制）的参数ID列表
                    IList<ElementId> nonControlledIds = template.GetNonControlledTemplateParameterIds().ToList();
                    // 如果我们要查的参数ID，不在“不控制”列表中，说明它被样板勾选控制了！即被锁定。
                    if (!nonControlledIds.Contains(new ElementId(bip)))
                    {
                        return false;
                    }
                }
            }
            return true; // 既有参数，又没被只读，又没被样板控制，则可以修改
        }
        // 2. 你的属性直接调用该方法即可完美生效
        public bool CanModifyDisplayStyle => CanModifyParameter(BuiltInParameter.MODEL_GRAPHICS_STYLE);
        public bool CanModifyDetailLevel => CanModifyParameter(BuiltInParameter.VIEW_DETAIL_LEVEL);
        // 细节级别双向绑定
        public string ViewDetailElem
        {
            get
            {
                try
                {
                    return MapDetailLevelToString(Viewe.DetailLevel);
                }
                catch
                {
                    return "NA"; // 如果不支持细节级别，返回 NA
                }
            }
            set
            {
                // 如果选择没变，或者选到了不支持的状态，直接返回
                if (string.IsNullOrEmpty(value) || value == "NA" || value == ViewDetailElem) return;
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改细节", () =>
                    {
                        try
                        {
                            Viewe.DetailLevel = MapStringToDetailLevel(value);
                        }
                        catch
                        {
                            // 忽略底层修改失败（比如当前视图被样板锁定了显示细节）
                        }

                        // 必须在事务内部、修改完成后调用通知，确保 UI 拿到最新值
                        OnPropertyChanged(nameof(ViewDetailElem));
                    });
                });
            }
        }
        private string MapDetailLevelToString(ViewDetailLevel level)
        {
            switch (level)
            {
                case ViewDetailLevel.Fine: return "精细";
                case ViewDetailLevel.Medium: return "中等";
                case ViewDetailLevel.Coarse: return "粗糙";
                default: return "NA";
            }
        }
        private ViewDetailLevel MapStringToDetailLevel(string s)
        {
            switch (s)
            {
                case "精细": return ViewDetailLevel.Fine;
                case "中等": return ViewDetailLevel.Medium;
                case "粗糙": return ViewDetailLevel.Coarse;
                default: return ViewDetailLevel.Coarse;
            }
        }
        // 1. 提供给 ComboBox 的下拉选项列表
        public List<string> ViewDisplay { get; } = new List<string> { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
        // 3. 显示样式双向绑定
        public string ViewDisplayElem
        {
            get
            {
                // 先检查参数是否存在
                Parameter param = Viewe.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE);
                if (param == null) return "NA";
                switch (Viewe.DisplayStyle)
                {
                    case DisplayStyle.Wireframe: return "线框";
                    case DisplayStyle.HLR: return "隐藏线";
                    case DisplayStyle.ShadingWithEdges: return "着色";
                    case DisplayStyle.Shading: return "一致的颜色";
                    case DisplayStyle.Realistic: return "真实";
                    default: return "其他";
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value) || value == "NA" || value == ViewDisplayElem) return;
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改显示样式", () =>
                    {
                        try
                        {
                            switch (value)
                            {
                                case "线框": Viewe.DisplayStyle = DisplayStyle.Wireframe; break;
                                case "隐藏线": Viewe.DisplayStyle = DisplayStyle.HLR; break;
                                case "着色": Viewe.DisplayStyle = DisplayStyle.ShadingWithEdges; break;
                                case "一致的颜色": Viewe.DisplayStyle = DisplayStyle.Shading; break;
                                case "真实": Viewe.DisplayStyle = DisplayStyle.Realistic; break;
                            }
                        }
                        catch { /* 处理修改失败的情况 */ }
                        // 修改后通知 UI 更新
                        OnPropertyChanged(nameof(ViewDisplayElem));
                    });
                });
            }
        }
        // 核心 UI 显示属性
        public string Scale => $"1:{Viewe.Scale}";
        public string LevelName { get; } = "NA";
        public string ViewTypeName => GetViewTypeChinese(Viewe.ViewType);
        public bool HasTemplate => Viewe.ViewTemplateId != ElementId.InvalidElementId;
        public bool IsTemplate => Viewe.IsTemplate;
        private string _viewName;
        public string ViewName
        {
            get => _viewName;
            set
            {
                if (_viewName == value) return;
                _handler.Run(app =>
                {
                    NewTransaction.Execute(Document, "修改视图名称", () => Viewe.Name = value);
                    _viewName = value;
                    OnPropertyChanged();
                });
            }
        }
    }
}
