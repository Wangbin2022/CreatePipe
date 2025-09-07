using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreatePipe.ViewFilters
{
    public sealed class ViewFilterModel : ObserverableObject
    {
        public ParameterFilterElement pFilterElement { get; set; }
        public Document Document { get => pFilterElement.Document; }
        private bool isInUsing = false;
        public bool IsInUsing
        {
            get => isInUsing;
            set
            {
                isInUsing = value;
                OnPropertyChanged(nameof(IsInUsing));
            }
        }
        //public String filterName { get; private set; }
        public ElementId Id { get; private set; }
        public string ruleCombineType { get; private set; }
        public string ruleCombine { get; private set; }
        public bool IsSelected { get; set; }
        public bool isHideBtn = false;
        public bool IsHideBtn
        {
            get => isHideBtn;
            set
            {
                if (isHideBtn != value) // 检查新值是否与旧值不同
                {
                    isHideBtn = value;
                    OnPropertyChanged(nameof(IsHideBtn)); // 通知属性值已更改
                }
            }
        }
        public List<int> TransparencySamples
        {
            get
            {
                List<int> ints = new List<int>();
                ints.Add(15);
                ints.Add(30);
                ints.Add(50);
                ints.Add(70);
                ints.Add(85);
                return ints;
            }
            set { TransparencySamples = value; }
        }
        private int transparencyNum;
        public int TransparencyNum
        {
            get { return transparencyNum; }
            set
            {
                transparencyNum = value;
                OnPropertyChanged("TransparencyNum");
            }
        }
        public Autodesk.Revit.DB.Color color;
        public Autodesk.Revit.DB.Color Color
        {
            get => color;
            set
            {
                if (color != value) // 检查新值是否与旧值不同
                {
                    color = value;
                    OnPropertyChanged(nameof(Color));
                    ColorValue = GetColorValue(value);
                }
            }
        }
        private string _colorValue = "无替换";
        public string ColorValue
        {
            get => _colorValue;
            set
            {
                if (_colorValue != value) // 检查新值是否与旧值不同
                {
                    _colorValue = value;
                    OnPropertyChanged(nameof(ColorValue)); // 通知属性值已更改
                }
            }
        }
        public ViewFilterModel(ParameterFilterElement parameterFilterElement)
        {
            pFilterElement = parameterFilterElement;
            viewFilterName = parameterFilterElement.Name;
            Id = parameterFilterElement.Id;
            isInUsing = FindInUsing();
            categoryItems = GetCategoryList(parameterFilterElement);
            ruleCombineType = GetCombineType(parameterFilterElement);
            ruleCombine = GetCombineRules(parameterFilterElement);
            if (IsInUsing == true)
            {
                IsHideBtn = true;
            }
        }
        public string GetColorValue(Autodesk.Revit.DB.Color color)
        {
            string colorvalue;
            colorvalue = color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
            return colorvalue;
        }
        private string GetCombineRules(ParameterFilterElement pfe)
        {
            StringBuilder stringBuilder = new StringBuilder();
            ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
            if (elf == null) return null;

            string combine = GetCombineType(pfe);
            if (elf is LogicalAndFilter)
            {
                IList<ElementFilter> efs = elf.GetFilters();
                if (efs == null)
                {
                    string app = "未设置规则";
                    stringBuilder.Append(app);
                    return stringBuilder.ToString();
                }
                else
                {
                    foreach (ElementParameterFilter item in efs)
                    {
                        foreach (FilterRule r in item.GetRules())
                        {
                            string comparator = "";
                            string ruleValue = "";
                            string rtype = "";
                            string paramName = "";
                            if (r.GetRuleParameter().IntegerValue < 0)
                                paramName = LabelUtils.GetLabelFor((BuiltInParameter)r.GetRuleParameter().IntegerValue);
                            if (r is FilterInverseRule)//判断规则是否反转
                            {
                                //TaskDialog.Show("tt", "是翻转规则");
                                FilterRule innerRule = (r as FilterInverseRule).GetInnerRule();
                                switch (innerRule)
                                {
                                    case FilterStringRule _:
                                        rtype = "FilterStringRule";
                                        FilterStringRule fsr = innerRule as FilterStringRule;
                                        if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
                                            comparator = "开始部分不是";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
                                            comparator = "末尾不是";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
                                            comparator = "不等于";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
                                            comparator = "不包含";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(HasValueFilterRule)))
                                            comparator = "没有值";
                                        ruleValue = fsr.RuleString;
                                        break;
                                    case FilterDoubleRule _:
                                        rtype = "FilterDoubleRule";
                                        FilterDoubleRule fdr = innerRule as FilterDoubleRule;
                                        if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
                                            comparator = ">";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
                                            comparator = "≥";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
                                            comparator = "<";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
                                            comparator = "≤";
                                        ruleValue = (fdr.RuleValue * 304.8).ToString();
                                        break;
                                    case FilterIntegerRule _:
                                        rtype = "FilterIntegerRule";
                                        FilterIntegerRule fir = innerRule as FilterIntegerRule;
                                        if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
                                            comparator = "≠";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
                                            comparator = "<";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
                                            comparator = "≤";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
                                            comparator = ">";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
                                            comparator = "≥";

                                        if (((BuiltInParameter)innerRule.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
                                        {
                                            ruleValue = ((WallFunction)fir.RuleValue).ToString();
                                        }
                                        else
                                            ruleValue = fir.RuleValue.ToString();
                                        break;
                                    case FilterElementIdRule _:
                                        rtype = "FilterElementIdRule";
                                        FilterElementIdRule fer = innerRule as FilterElementIdRule;
                                        if (fer.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
                                            comparator = "不是";
                                        ruleValue = Document.GetElement(fer.RuleValue).Name.ToString();
                                        break;
                                    case FilterGlobalParameterAssociationRule _:
                                        rtype = "FilterGlobalParameterAssociationRule";
                                        break;
                                    default:
                                        rtype = "未识别或没有设置Rule";
                                        break;
                                }
                            }
                            else
                            {
                                //TaskDialog.Show("tt", "非翻转规则");
                                switch (r)
                                {
                                    case FilterStringRule _:
                                        rtype = "FilterStringRule";
                                        FilterStringRule fsr = r as FilterStringRule;
                                        if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
                                            comparator = "开始部分是";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
                                            comparator = "末尾是";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
                                            comparator = "等于";
                                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
                                            comparator = "包含";
                                        ruleValue = fsr.RuleString;
                                        break;
                                    case FilterDoubleRule _:
                                        rtype = "FilterDoubleRule";
                                        FilterDoubleRule fdr = r as FilterDoubleRule;
                                        if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
                                            comparator = "<";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
                                            comparator = "≤";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
                                            comparator = ">";
                                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
                                            comparator = "≥";
                                        ruleValue = (fdr.RuleValue * 304.8).ToString();
                                        break;
                                    case FilterIntegerRule _:
                                        rtype = "FilterIntegerRule";
                                        FilterIntegerRule fir = r as FilterIntegerRule;
                                        if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
                                            comparator = "=";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
                                            comparator = ">";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
                                            comparator = "≥";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
                                            comparator = "<";
                                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
                                            comparator = "≤";

                                        if (((BuiltInParameter)r.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
                                        {
                                            ruleValue = ((WallFunction)fir.RuleValue).ToString();
                                        }
                                        else
                                            ruleValue = fir.RuleValue.ToString();
                                        break;
                                    case FilterElementIdRule _:
                                        rtype = "FilterElementIdRule";
                                        FilterElementIdRule fer = r as FilterElementIdRule;
                                        if (fer.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
                                            comparator = "是";
                                        ruleValue = Document.GetElement(fer.RuleValue).Name.ToString();
                                        break;
                                    case FilterGlobalParameterAssociationRule _:
                                        rtype = "FilterGlobalParameterAssociationRule";
                                        break;
                                    default:
                                        rtype = "未识别或没有设置Rule";
                                        break;
                                }
                                if (r.GetType().Equals(typeof(HasValueFilterRule)))
                                {
                                    comparator = "有一个值";
                                }
                                else if (r.GetType().Equals(typeof(HasNoValueFilterRule)))
                                {
                                    comparator = "没有值";
                                }
                            }
                            string app = "”" + paramName + "”" + comparator + ruleValue + "；";
                            stringBuilder.Append(app);
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }
        private string GetCombineType(ParameterFilterElement pfe)
        {
            ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
            if (elf is LogicalAndFilter)
            {
                return "且";
            }
            else if (elf is LogicalOrFilter)
            {
                return "或";
            }
            else return "";
        }
        private List<string> GetCategoryList(ParameterFilterElement pfe)
        {
            CategoryItems.Clear();
            List<string> categoryList = new List<string>();
            List<ElementId> categoryIds = pfe.GetCategories().ToList();


            foreach (ElementId item in categoryIds)
            {
                Category category = Category.GetCategory(Document, item);
                if (category == null) return null;
                string categoryName = category.Name;
                categoryList.Add(categoryName);
            }
            return categoryList;
        }
        private List<string> categoryItems = new List<string>();
        public List<string> CategoryItems
        {
            get => categoryItems;
            set
            {
                categoryItems = value;
                OnPropertyChanged();
            }
        }
        private bool FindInUsing()
        {
            FilteredElementCollector collector = new FilteredElementCollector(Document);
            IList<Element> Views = collector.OfClass(typeof(View)).ToList();
            List<View> allViews = new List<View>();
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
            HashSet<ElementId> viewFilters = new HashSet<ElementId>();
            foreach (View e in allViews)
            {
                ICollection<ElementId> ids = e.GetFilters();
                foreach (var item in ids)
                {
                    viewFilters.Add(item);
                }
            }
            //筛选出全部在使用过滤器
            foreach (ElementId item in viewFilters)
            {
                if (item.IntegerValue == Id.IntegerValue)
                {
                    return true;
                }
                continue;
            }
            return false;
        }
        private string viewFilterName;
        public string ViewFilterName
        {
            get { return viewFilterName; }
            set
            {
                Document.NewTransaction(() => pFilterElement.Name = value, "修改名称");
                viewFilterName = value;
                OnPropertyChanged(nameof(viewFilterName));
            }
        }
    }

}
