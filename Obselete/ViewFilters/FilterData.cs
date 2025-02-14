namespace CreatePipe.ViewFilters
{
    ////FilterRule的Model
    //public sealed class FilterRuleBuilder
    //{
    //    //FilterRule的参数
    //    public BuiltInParameter Parameter { get; private set; }
    //    //FilterRule的筛选规则条件（字符串类型）
    //    public String RuleCriteria { get; private set; }
    //    //字符串类型的规则值
    //    public String RuleValue { get; private set; }
    //    //当前 FilterRule 的参数存储类型。
    //    public StorageType ParamType { get; private set; }
    //    //双重比较容差，仅在 ParamType 为 double 时有效
    //    public double Epsilon { get; private set; }
    //    //ParamType 为 string时，指示字符串比较是否区分大小写
    //    public bool CaseSensitive { get; private set; }

    //    //为字符串 FilterRule 创建 FilterRuleBuilder
    //    public FilterRuleBuilder(BuiltInParameter param, String ruleCriteria, String ruleValue, bool caseSensitive)
    //    {
    //        InitializeMemebers();
    //        // set data with specified values
    //        ParamType = StorageType.String;
    //        Parameter = param;
    //        RuleCriteria = ruleCriteria;
    //        RuleValue = ruleValue;
    //        CaseSensitive = caseSensitive;
    //    }
    //    //为 double FilterRule 创建 FilterRuleBuilder
    //    public FilterRuleBuilder(BuiltInParameter param, String ruleCriteria, double ruleValue, double tolearance)
    //    {
    //        InitializeMemebers();
    //        ParamType = StorageType.Double;
    //        Parameter = param;
    //        RuleCriteria = ruleCriteria;
    //        RuleValue = ruleValue.ToString();
    //        Epsilon = tolearance;
    //    }
    //    //为 int FilterRule 创建 FilterRuleBuilder
    //    public FilterRuleBuilder(BuiltInParameter param, String ruleCriteria, int ruleValue)
    //    {
    //        InitializeMemebers();
    //        ParamType = StorageType.Integer;
    //        Parameter = param;
    //        RuleCriteria = ruleCriteria;
    //        RuleValue = ruleValue.ToString();
    //    }
    //    //为ElemId FilterRule 创建 FilterRuleBuilder
    //    public FilterRuleBuilder(BuiltInParameter param, String ruleCriteria, ElementId ruleValue)
    //    {
    //        InitializeMemebers();
    //        ParamType = StorageType.ElementId;
    //        Parameter = param;
    //        RuleCriteria = ruleCriteria;
    //        RuleValue = ruleValue.ToString();
    //    }
    //    //根据示例的 FilterRuleBuilder 创建 API FilterRule
    //    public FilterRule AsFilterRule()
    //    {
    //        ElementId paramId = new ElementId(Parameter);
    //        if (ParamType == StorageType.String)
    //        {
    //            switch (RuleCriteria)
    //            {
    //                case RuleCriteraNames.BeginWith:
    //                    return PFRF.CreateBeginsWithRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.Contains:
    //                    return PFRF.CreateContainsRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.EndsWith:
    //                    return PFRF.CreateEndsWithRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.Equals_:
    //                    return PFRF.CreateEqualsRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.Greater:
    //                    return PFRF.CreateGreaterRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.GreaterOrEqual:
    //                    return PFRF.CreateGreaterOrEqualRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.Less:
    //                    return PFRF.CreateLessRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.LessOrEqual:
    //                    return PFRF.CreateLessOrEqualRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.NotBeginWith:
    //                    return PFRF.CreateNotBeginsWithRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.NotContains:
    //                    return PFRF.CreateNotContainsRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.NotEndsWith:
    //                    return PFRF.CreateNotEndsWithRule(paramId, RuleValue, CaseSensitive);
    //                case RuleCriteraNames.NotEquals:
    //                    return PFRF.CreateNotEqualsRule(paramId, RuleValue, CaseSensitive);                    
    //            }
    //        }
    //        else if (ParamType == StorageType.Double)
    //        {
    //            switch (RuleCriteria)
    //            {
    //                case RuleCriteraNames.Equals_:
    //                    return PFRF.CreateEqualsRule(paramId, double.Parse(RuleValue), Epsilon);
    //                case RuleCriteraNames.Greater:
    //                    return PFRF.CreateGreaterRule(paramId, double.Parse(RuleValue), Epsilon);
    //                case RuleCriteraNames.GreaterOrEqual:
    //                    return PFRF.CreateGreaterOrEqualRule(paramId, double.Parse(RuleValue), Epsilon);
    //                case RuleCriteraNames.Less:
    //                    return PFRF.CreateLessRule(paramId, double.Parse(RuleValue), Epsilon);
    //                case RuleCriteraNames.LessOrEqual:
    //                    return PFRF.CreateLessOrEqualRule(paramId, double.Parse(RuleValue), Epsilon);
    //                case RuleCriteraNames.NotEquals:
    //                    return PFRF.CreateNotEqualsRule(paramId, double.Parse(RuleValue), Epsilon);
    //            }
    //        }
    //        else if (ParamType == StorageType.Integer)
    //        {
    //            switch (RuleCriteria)
    //            {
    //                case RuleCriteraNames.Equals_:
    //                    return PFRF.CreateEqualsRule(paramId, int.Parse(RuleValue));
    //                case RuleCriteraNames.Greater:
    //                    return PFRF.CreateGreaterRule(paramId, int.Parse(RuleValue));
    //                case RuleCriteraNames.GreaterOrEqual:
    //                    return PFRF.CreateGreaterOrEqualRule(paramId, int.Parse(RuleValue));
    //                case RuleCriteraNames.Less:
    //                    return PFRF.CreateLessRule(paramId, int.Parse(RuleValue));
    //                case RuleCriteraNames.LessOrEqual:
    //                    return PFRF.CreateLessOrEqualRule(paramId, int.Parse(RuleValue));
    //                case RuleCriteraNames.NotEquals:
    //                    return PFRF.CreateNotEqualsRule(paramId, int.Parse(RuleValue));
    //            }
    //        }
    //        else if (ParamType == StorageType.ElementId)
    //        {
    //            switch (RuleCriteria)
    //            {
    //                case RuleCriteraNames.Equals_:
    //                    return PFRF.CreateEqualsRule(paramId, new ElementId(int.Parse(RuleValue)));
    //                case RuleCriteraNames.Greater:
    //                    return PFRF.CreateGreaterRule(paramId, new ElementId(int.Parse(RuleValue)));
    //                case RuleCriteraNames.GreaterOrEqual:
    //                    return PFRF.CreateGreaterOrEqualRule(paramId, new ElementId(int.Parse(RuleValue)));
    //                case RuleCriteraNames.Less:
    //                    return PFRF.CreateLessRule(paramId, new ElementId(int.Parse(RuleValue)));
    //                case RuleCriteraNames.LessOrEqual:
    //                    return PFRF.CreateLessOrEqualRule(paramId, new ElementId(int.Parse(RuleValue)));
    //                case RuleCriteraNames.NotEquals:
    //                    return PFRF.CreateNotEqualsRule(paramId, new ElementId(int.Parse(RuleValue)));
    //            }
    //        }
    //        throw new System.NotImplementedException("This filter rule or criteria is not implemented yet.");
    //    }
    //    //确保所有成员都使用预期值进行初始化。
    //    private void InitializeMemebers()
    //    {
    //        Parameter = (BuiltInParameter)(ElementId.InvalidElementId.IntegerValue);
    //        RuleCriteria = String.Empty;
    //        RuleValue = String.Empty;
    //        ParamType = StorageType.None;
    //        Epsilon = 0.0f;
    //        CaseSensitive = false;
    //    }
    //}
    ////此类用于表示一个 API 筛选器的数据。它由 BuiltInCategory 和筛选规则组成
    //public sealed class FilterData
    //{
    //    Autodesk.Revit.DB.Document m_doc;
    //    List<BuiltInCategory> m_filterCategories;
    //    List<FilterRuleBuilder> m_filterRules;
    //    //Get BuiltInCategories of filter
    //    public List<BuiltInCategory> FilterCategories
    //    {
    //        get { return m_filterCategories; }
    //    }
    //    //Get BuiltInCategory Ids of filter
    //    public IList<ElementId> GetCategoryIds()
    //    {
    //        List<ElementId> catIds = new List<ElementId>();
    //        foreach (BuiltInCategory cat in m_filterCategories)
    //            catIds.Add(new ElementId(cat));
    //        return catIds;
    //    }
    //    //设置新分类，此方法可能会更新已有的 criteria如果新分类不支持 criteria 的 someone 参数，旧的 criteria 将被清理并设置为空
    //    public bool SetNewCategories(ICollection<ElementId> newCatIds)
    //    {
    //        // do nothing if new categories are equals to old categories
    //        List<BuiltInCategory> newCats = new List<BuiltInCategory>();
    //        foreach (ElementId catId in newCatIds)
    //            newCats.Add((BuiltInCategory)catId.IntegerValue);
    //        if (ListCompareUtility<BuiltInCategory>.Equals(newCats, m_filterCategories))
    //            return false;
    //        m_filterCategories = newCats; // update categories
    //        // Check if need to update file rules:
    //        // . if filer rule is empty, do nothing
    //        // . if some parameters of rules cannot be supported by new categories, clean all old rules
    //        ICollection<ElementId> supportParams =
    //            ParameterFilterUtilities.GetFilterableParametersInCommon(m_doc, newCatIds);
    //        foreach (FilterRuleBuilder rule in m_filterRules)
    //        {
    //            if (!supportParams.Contains(new ElementId(rule.Parameter)))
    //            {
    //                m_filterRules.Clear();
    //                break;
    //            }
    //        }
    //        return true;
    //    }
    //    //获取 API 过滤器规则的 FilterRuleBuilder
    //    public List<FilterRuleBuilder> RuleData
    //    {
    //        get { return m_filterRules; }
    //    }
    //    //创建具有指定类别和 FilterRuleBuilder 的示例自定义 FilterData
    //    public FilterData(Autodesk.Revit.DB.Document doc, ICollection<BuiltInCategory> categories, ICollection<FilterRuleBuilder> filterRules)
    //    {
    //        m_doc = doc;
    //        m_filterCategories = new List<BuiltInCategory>();
    //        m_filterCategories.AddRange(categories);
    //        m_filterRules = new List<FilterRuleBuilder>();
    //        m_filterRules.AddRange(filterRules);
    //    }
    //    //创建具有指定Category ID 和 FilterRuleBuilder 的示例自定义 FilterData
    //    public FilterData(Autodesk.Revit.DB.Document doc, ICollection<ElementId> categories, ICollection<FilterRuleBuilder> filterRules)
    //    {
    //        m_doc = doc;
    //        m_filterCategories = new List<BuiltInCategory>();
    //        foreach (ElementId catId in categories)
    //            m_filterCategories.Add((BuiltInCategory)catId.IntegerValue);
    //        m_filterRules = new List<FilterRuleBuilder>();
    //        m_filterRules.AddRange(filterRules);
    //    }
    //}
    ////此类定义用于映射规则条件的常量字符串
    //public sealed class RuleCriteraNames
    //{
    //    public const String BeginWith = "begins with";
    //    public const String Contains = "contains";
    //    public const String EndsWith = "ends with";
    //    public const String Equals_ = "equals";
    //    public const String Greater = "is greater than";
    //    public const String GreaterOrEqual = "is greater than or equal to";
    //    public const String LessOrEqual = "is less than or equal to";
    //    public const String Less = "is less than";
    //    public const String NotBeginWith = "does not begin with";
    //    public const String NotContains = "does not contain";
    //    public const String NotEndsWith = "does not end with";
    //    public const String NotEquals = "does not equal";
    //    public const String Invalid = "n/a";
    //    private RuleCriteraNames() { }
    //    //根据参数的 StorageType 获取所有支持的 criteria（in string）
    //    public static ICollection<String> Criterions(StorageType paramType)
    //    {
    //        ICollection<String> returns = new List<String>();
    //        //
    //        // all parameter supports following criteria
    //        returns.Add(Equals_);
    //        returns.Add(Greater);
    //        returns.Add(GreaterOrEqual);
    //        returns.Add(LessOrEqual);
    //        returns.Add(Less);
    //        returns.Add(NotEquals);
    //        // 
    //        // Only string parameter support criteria below
    //        if (paramType == StorageType.String)
    //        {
    //            returns.Add(BeginWith);
    //            returns.Add(Contains);
    //            returns.Add(EndsWith);
    //            returns.Add(NotBeginWith);
    //            returns.Add(NotContains);
    //            returns.Add(NotEndsWith);
    //        }
    //        return returns;
    //    }
    //}
}
