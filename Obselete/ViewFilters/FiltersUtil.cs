namespace CreatePipe.ViewFilters
{
    //public sealed class FiltersUtil
    //{
    //    private FiltersUtil() { }
    //    //找出文档内的所有PFE
    //    public static ICollection<ParameterFilterElement> GetViewFilters(Autodesk.Revit.DB.Document doc)
    //    {
    //        ElementClassFilter filter = new ElementClassFilter(typeof(ParameterFilterElement));
    //        FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        return collector.WherePasses(filter).ToElements()
    //            .Cast<ParameterFilterElement>().ToList<ParameterFilterElement>();
    //    }
    //    //将FilterRule转为FilterRuleBuilder展示到Form
    //    public static FilterRuleBuilder CreateFilterRuleBuilder(BuiltInParameter param, FilterRule rule)
    //    {
    //        // Maybe FilterRule is inverse rule, we need to find its inner rule(FilterValueRule)
    //        // Note that the rule may be inversed more than once.
    //        bool inverted = false;
    //        FilterRule innerRule = ReflectToInnerRule(rule, out inverted);
    //        if (innerRule is FilterStringRule)
    //        {
    //            FilterStringRule strRule = innerRule as FilterStringRule;
    //            FilterStringRuleEvaluator evaluator = strRule.GetEvaluator();
    //            return new FilterRuleBuilder(param, GetEvaluatorCriteriaName(evaluator, inverted), strRule.RuleString, strRule.RuleString.ToLower() == strRule.RuleString ? false : true);
    //        }
    //        else if (innerRule is FilterDoubleRule)
    //        {
    //            FilterDoubleRule dbRule = innerRule as FilterDoubleRule;
    //            FilterNumericRuleEvaluator evaluator = dbRule.GetEvaluator();
    //            return new FilterRuleBuilder(param, GetEvaluatorCriteriaName(evaluator, inverted), dbRule.RuleValue, dbRule.Epsilon);
    //        }
    //        else if (innerRule is FilterIntegerRule)
    //        {
    //            FilterIntegerRule intRule = innerRule as FilterIntegerRule;
    //            FilterNumericRuleEvaluator evaluator = intRule.GetEvaluator();
    //            return new FilterRuleBuilder(param, GetEvaluatorCriteriaName(evaluator, inverted), intRule.RuleValue);
    //        }
    //        else if (innerRule is FilterElementIdRule)
    //        {
    //            FilterElementIdRule idRule = innerRule as FilterElementIdRule;
    //            FilterNumericRuleEvaluator evaluator = idRule.GetEvaluator();
    //            return new FilterRuleBuilder(param, GetEvaluatorCriteriaName(evaluator, inverted), idRule.RuleValue);
    //        }
    //        // 
    //        // for other rule, not supported yet
    //        throw new System.NotImplementedException("The filter rule is not recognizable and supported yet!");
    //    }
    //    //从 String Evaluator 获取判断条件（in string）
    //    static string GetEvaluatorCriteriaName(FilterStringRuleEvaluator fsre, bool inverted)
    //    {
    //        // indicate if inverse criteria should be returned
    //        bool isInverseRule = inverted;
    //        if (fsre is FilterStringBeginsWith)
    //            return (isInverseRule ? RuleCriteraNames.NotBeginWith : RuleCriteraNames.BeginWith);
    //        else if (fsre is FilterStringContains)
    //            return (isInverseRule ? RuleCriteraNames.NotContains : RuleCriteraNames.Contains);
    //        else if (fsre is FilterStringEndsWith)
    //            return (isInverseRule ? RuleCriteraNames.NotEndsWith : RuleCriteraNames.EndsWith);
    //        else if (fsre is FilterStringEquals)
    //            return (isInverseRule ? RuleCriteraNames.NotEquals : RuleCriteraNames.Equals_);
    //        else if (fsre is FilterStringGreater)
    //            return (isInverseRule ? RuleCriteraNames.LessOrEqual : RuleCriteraNames.Greater);
    //        else if (fsre is FilterStringGreaterOrEqual)
    //            return (isInverseRule ? RuleCriteraNames.Less : RuleCriteraNames.GreaterOrEqual);
    //        else if (fsre is FilterStringLess)
    //            return (isInverseRule ? RuleCriteraNames.GreaterOrEqual : RuleCriteraNames.Less);
    //        else if (fsre is FilterStringLessOrEqual)
    //            return (isInverseRule ? RuleCriteraNames.Greater : RuleCriteraNames.LessOrEqual);
    //        else
    //            return RuleCriteraNames.Invalid;
    //    }
    //    //从 Numeric Evaluator 获取判断条件（in string）
    //    static string GetEvaluatorCriteriaName(FilterNumericRuleEvaluator fsre, bool inverted)
    //    {
    //        // indicate if inverse criteria should be returned
    //        bool isInverseRule = inverted;
    //        if (fsre is FilterNumericEquals)
    //            return (isInverseRule ? RuleCriteraNames.NotEquals : RuleCriteraNames.Equals_);
    //        else if (fsre is FilterNumericGreater)
    //            return (isInverseRule ? RuleCriteraNames.LessOrEqual : RuleCriteraNames.Greater);
    //        else if (fsre is FilterNumericGreaterOrEqual)
    //            return (isInverseRule ? RuleCriteraNames.Less : RuleCriteraNames.GreaterOrEqual);
    //        else if (fsre is FilterNumericLess)
    //            return (isInverseRule ? RuleCriteraNames.GreaterOrEqual : RuleCriteraNames.Less);
    //        else if (fsre is FilterNumericLessOrEqual)
    //            return (isInverseRule ? RuleCriteraNames.Greater : RuleCriteraNames.LessOrEqual);
    //        else
    //            return RuleCriteraNames.Invalid;
    //    }
    //    //将筛选规则反映到其内部规则，最后一个内部规则是此示例的 FilterValueRule
    //    public static FilterRule ReflectToInnerRule(FilterRule srcRule, out bool inverted)
    //    {
    //        if (srcRule is FilterInverseRule)
    //        {
    //            inverted = true;
    //            FilterRule innerRule = (srcRule as FilterInverseRule).GetInnerRule();
    //            bool invertedAgain = false;
    //            FilterRule returnRule = ReflectToInnerRule(innerRule, out invertedAgain);
    //            if (invertedAgain)
    //                inverted = false;
    //            return returnRule;
    //        }
    //        else
    //        {
    //            inverted = false;
    //            return srcRule;
    //        }
    //    }
    //}

}
