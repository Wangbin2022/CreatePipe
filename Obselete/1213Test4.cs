using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System.Collections.Generic;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class _1213Test4 : ObserverableObject, IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //FilterTestNew filterTestNew = new FilterTestNew(doc);
            //filterTestNew.ShowDialog();

            ////1229 删除改
            //try
            //{
            //    TaskDialog td = new TaskDialog("重要提示")
            //    {
            //        MainInstruction = "警告",
            //        MainContent = "请谨慎选择要删除对象，操作不可撤销，确定删除请点击下方选项继续",
            //        MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            //        CommonButtons = TaskDialogCommonButtons.Close,
            //        DefaultButton = TaskDialogResult.Close
            //        //ExpandedContent ="test",                    
            //        //CommonButtons = TaskDialogCommonButtons.Close | TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
            //        //DefaultButton = TaskDialogResult.No
            //    };
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "删除链接DWG");
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "删除链接RVT");
            //    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "删除所有链接");
            //    TaskDialogResult result = td.Show();
            //    var collector = new FilteredElementCollector(doc);
            //    var elementFilter1 = new ElementClassFilter(typeof(ImportInstance));
            //    var elementFilter2 = new ElementClassFilter(typeof(CADLinkType));
            //    var elementFilter3 = new ElementClassFilter(typeof(RevitLinkType));
            //    if (TaskDialogResult.CommandLink1 == result)
            //    {

            //        var orFilter = new LogicalOrFilter(elementFilter1, elementFilter2);
            //        var cadsCollector = collector.WherePasses(orFilter);
            //        doc.NewTransaction(() =>
            //        {
            //            doc.Delete(cadsCollector.Select(n => n.Id).ToList());
            //        }, "批量删除链接dwg");
            //    }
            //    else if (TaskDialogResult.CommandLink2 == result)
            //    {
            //        var orFilter = new LogicalOrFilter(elementFilter1, elementFilter3);
            //        var cadsCollector = collector.WherePasses(orFilter);
            //        doc.NewTransaction(() =>
            //        {
            //            doc.Delete(cadsCollector.Select(n => n.Id).ToList());
            //        }, "批量删除链接Revit");
            //    }
            //    else if (TaskDialogResult.CommandLink3 == result)
            //    {
            //        var orFilter1 = new LogicalOrFilter(elementFilter1, elementFilter2);
            //        var orFilter2 = new LogicalOrFilter(elementFilter1, elementFilter3);
            //        var collector2 = new FilteredElementCollector(doc);
            //        var cadsCollector1 = collector.WherePasses(orFilter1);
            //        var cadsCollector2 = collector2.WherePasses(orFilter2);
            //        doc.NewTransaction(() =>
            //        {
            //            doc.Delete(cadsCollector1.Select(n => n.Id).ToList());
            //            doc.Delete(cadsCollector2.Select(n => n.Id).ToList());
            //        }, "批量删除全部链接");
            //    }
            //}
            //catch (Exception)
            //{
            //    return Result.Cancelled;
            //}

            //string docpath=doc.PathName;
            //string path= Path.GetFileNameWithoutExtension(docpath);
            //TaskDialog.Show("tt", path);

            //CableTraySystemTest cableSystem = new CableTraySystemTest(doc);
            //cableSystem.ShowDialog();

            //FilterTestNew filterTestNew = new FilterTestNew(doc);
            //filterTestNew.ShowDialog();
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(CableTrayType));
            //List<CableTrayType> cableTrayTypes=elems.OfType<CableTrayType>().ToList(); 
            //List < CableTrayType > CtA = new List < CableTrayType >();
            //List < CableTrayType > CtB = new List < CableTrayType >();
            //foreach (var item in cableTrayTypes)
            //{
            //    if (item.IsWithFitting)
            //    {
            //        CtA.Add(item);
            //    }
            //    else CtB.Add(item);

            //}
            //TaskDialog.Show("tt", CtA.Count.ToString()+"+"+CtB.Count().ToString());

            //1215 筛选特定过滤器
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement));
            //List<ParameterFilterElement> pfes = elems.OfType<ParameterFilterElement>().ToList();
            //ParameterFilterElement pfe = pfes[0]; //注意不要超过实际取值
            //[2]多类"内部"过滤器没有找到正确规则值，显示为 = interior而不是内部，用 = 说明被设为数值型了，应该是别的
            //[4]全类”工作集 - 建筑外墙“过滤器没有设置工作集，但显示446，449
            #region 
            //[0]单类”结构柱“，启用分析模型 可识别，只返回1，规则值应为是 ，用 = 说明被设为数值型了 
            //[1]单类”结构标高过滤器“，建筑楼层 可识别，返回值0，规则值应为否 可能参数本身就是以数值表示的是否
            //[3]多类"混凝土结构填充"，结构材质 属性可识别 ，没有找到对应规则和规则值
            //[5]多类”人防-排风系统“只找到系统类型，没有找到对应规则和规则值

            //TaskDialog.Show("tt", pfe.Name);
            //ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
            //IList<ElementFilter> efs = elf.GetFilters();
            //foreach (ElementParameterFilter item in efs)
            //{
            //    foreach (FilterRule r in item.GetRules())
            //    {
            //        string comparator = "";
            //        string ruleValue = "";
            //        string rtype = "";
            //        string paramName = "";
            //        if (r.GetRuleParameter().IntegerValue < 0)
            //        {
            //            paramName = LabelUtils.GetLabelFor((BuiltInParameter)r.GetRuleParameter().IntegerValue);
            //        }

            //        if (r is FilterIntegerRule)
            //        {
            //            rtype = "FilterIntegerRule";
            //            FilterIntegerRule fir = r as FilterIntegerRule;
            //            if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
            //                comparator = "属性为";
            //            //要先找出其对应的element，再确定它的BuiltInParameter
            //            //BuiltInParameter para = (BuiltInParameter)r.GetRuleParameter().IntegerValue;
            //            //List<ElementId> categoryIds = pfe.GetCategories().ToList();
            //            //Category category = Category.GetCategory(doc, categoryIds.FirstOrDefault());
            //            //TaskDialog.Show("tt", pfe.Name+"+"+category.Name);
            //            BuiltInParameter para = (BuiltInParameter)r.GetRuleParameter().IntegerValue;
            //            //ruleValue = para.GetName();
            //            ruleValue = fir.RuleValue.ToString();
            //            //TaskDialog.Show("tt", pfe.Name + "+" + ruleValue);
            //            //下段有问题，不能这样直接调
            //            //if (fir.RuleValue == 1)
            //            //{
            //            //    ruleValue = "是";
            //            //}
            //            //else
            //            //{
            //            //    BuiltInParameter para= (BuiltInParameter)r.GetRuleParameter().IntegerValue;
            //            //    ruleValue= para.GetName();
            //            //}
            //            //TaskDialog.Show("tt", paramName + comparator + ruleValue);
            //        }

            //        //[0][1]也要按上面的原则修改 
            //        //if (r is FilterIntegerRule)
            //        //{
            //        //    rtype = "FilterIntegerRule";
            //        //    FilterIntegerRule fir = r as FilterIntegerRule;
            //        //    if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
            //        //        comparator = "属性为";
            //        //    if (fir.RuleValue == 1)
            //        //    {
            //        //        ruleValue = "是";
            //        //    }
            //        //    TaskDialog.Show("tt", paramName + comparator + ruleValue);
            //        //}

            //        //机电系统【5】适用，材料【3】适用，
            //        if (r is FilterElementIdRule)
            //        {
            //            rtype = "FilterElementIdRule";
            //            FilterElementIdRule fer = r as FilterElementIdRule;
            //            if (fer.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
            //                comparator = "是";
            //            ruleValue = doc.GetElement(fer.RuleValue).Name.ToString();
            //            //TaskDialog.Show("tt", comparator + ruleValue);
            //        }
            //    }
            //}

            //检验过滤器是否可见并修改，要注意检查过滤器是否已加载到视图并提示用户
            //if (activeView.GetFilterVisibility(pfe.Id))
            //{
            //    TaskDialog.Show("tt", pfe.Name + "过滤器可见");
            //    doc.NewTransaction(() => activeView.SetFilterVisibility(pfe.Id, false), "修改过滤器可见性");
            //}
            //else
            //{
            //    TaskDialog.Show("tt", pfe.Name + "过滤器不可见");
            //}
            ////找出其对应的类别
            //List<ElementId> categoryIds = pfe.GetCategories().ToList();
            //List<string> categoryList = new List<string>();
            //foreach (ElementId item in categoryIds)
            //{
            //    Category category = Category.GetCategory(doc, item);
            //    string categoryName = category.Name;
            //    categoryList.Add(categoryName);
            //}
            //StringBuilder sb = new StringBuilder();
            //foreach (var item in categoryList)
            //{
            //    sb.AppendLine(item);
            //}
            //TaskDialog.Show("tt", sb.ToString());
            //找过滤规则 和/或 
            //if (elf is LogicalAndFilter)//判断是”和“还是”或“过滤器
            //{
            //    //TaskDialog.Show("tt", "logicalAND");
            //    //IList<ElementFilter> efs = elf.GetFilters();
            //    //if (efs.Count == 1)
            //    //{
            //    //    TaskDialog.Show("tt", "单规则");
            //    //}
            //    //else if (efs.Count == 0)
            //    //{
            //    //    TaskDialog.Show("tt", "未设置规则");
            //    //}
            //    //else
            //    //{
            //    //    TaskDialog.Show("tt", "多规则组合");
            //    //}
            //    //foreach (ElementParameterFilter item in efs)
            //    //{
            //    //    foreach (FilterRule r in item.GetRules())
            //    //    {
            //    //        string comparator = "";
            //    //        string ruleValue = "";
            //    //        string rtype = "";
            //    //        string paramName = "";
            //    //        if (r is FilterInverseRule)//判断规则是否反转
            //    //        {
            //    //            //TaskDialog.Show("tt", "是翻转规则");
            //    //            if (r.GetRuleParameter().IntegerValue < 0)
            //    //                paramName = LabelUtils.GetLabelFor((BuiltInParameter)r.GetRuleParameter().IntegerValue);
            //    //            FilterRule innerRule = (r as FilterInverseRule).GetInnerRule();
            //    //            switch (innerRule)
            //    //            {
            //    //                case FilterStringRule _:
            //    //                    rtype = "FilterStringRule";
            //    //                    FilterStringRule fsr = innerRule as FilterStringRule;
            //    //                    if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
            //    //                        comparator = "开始部分不是";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
            //    //                        comparator = "末尾不是";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
            //    //                        comparator = "不等于";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
            //    //                        comparator = "不包含";
            //    //                    ruleValue = fsr.RuleString;
            //    //                    break;
            //    //                case FilterDoubleRule _:
            //    //                    rtype = "FilterDoubleRule";
            //    //                    FilterDoubleRule fdr = innerRule as FilterDoubleRule;
            //    //                    if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
            //    //                        comparator = ">";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
            //    //                        comparator = "≥";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
            //    //                        comparator = "<";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
            //    //                        comparator = "≤";
            //    //                    ruleValue = (fdr.RuleValue * 304.8).ToString();
            //    //                    break;
            //    //                case FilterIntegerRule _:
            //    //                    rtype = "FilterIntegerRule";
            //    //                    FilterIntegerRule fir = innerRule as FilterIntegerRule;
            //    //                    if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
            //    //                        comparator = "≠";
            //    //                    else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
            //    //                        comparator = "<";
            //    //                    if (((BuiltInParameter)innerRule.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
            //    //                    {
            //    //                        ruleValue = ((WallFunction)fir.RuleValue).ToString();
            //    //                    }
            //    //                    else
            //    //                        ruleValue = fir.RuleValue.ToString();
            //    //                    break;
            //    //                case FilterElementIdRule _:
            //    //                    rtype = "FilterElementIdRule";
            //    //                    break;
            //    //                case FilterGlobalParameterAssociationRule _:
            //    //                    rtype = "FilterGlobalParameterAssociationRule";
            //    //                    break;
            //    //                default:
            //    //                    rtype = "未识别或没有设置Rule";
            //    //                    break;
            //    //            }
            //    //        }
            //    //        else
            //    //        {
            //    //            //TaskDialog.Show("tt", "非翻转规则");
            //    //            if (r.GetRuleParameter().IntegerValue < 0)
            //    //                paramName = LabelUtils.GetLabelFor((BuiltInParameter)r.GetRuleParameter().IntegerValue);
            //    //            switch (r)
            //    //            {
            //    //                case FilterStringRule _:
            //    //                    rtype = "FilterStringRule";
            //    //                    FilterStringRule fsr = r as FilterStringRule;
            //    //                    if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
            //    //                        comparator = "开始部分是";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
            //    //                        comparator = "末尾是";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
            //    //                        comparator = "等于";
            //    //                    else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
            //    //                        comparator = "包含";
            //    //                    ruleValue = fsr.RuleString;
            //    //                    break;
            //    //                case FilterDoubleRule _:
            //    //                    rtype = "FilterDoubleRule";
            //    //                    FilterDoubleRule fdr = r as FilterDoubleRule;
            //    //                    if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
            //    //                        comparator = "<";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLessOrEqual)))
            //    //                        comparator = "≤";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
            //    //                        comparator = ">";
            //    //                    else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreaterOrEqual)))
            //    //                        comparator = "≥";
            //    //                    ruleValue = (fdr.RuleValue * 304.8).ToString();
            //    //                    break;
            //    //                case FilterIntegerRule _:
            //    //                    rtype = "FilterIntegerRule";
            //    //                    FilterIntegerRule fir = r as FilterIntegerRule;
            //    //                    if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
            //    //                        comparator = "=";
            //    //                    else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
            //    //                        comparator = ">";
            //    //                    if (((BuiltInParameter)r.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
            //    //                    {
            //    //                        ruleValue = ((WallFunction)fir.RuleValue).ToString();
            //    //                    }
            //    //                    else
            //    //                        ruleValue = fir.RuleValue.ToString();
            //    //                    break;
            //    //                case FilterElementIdRule _:
            //    //                    rtype = "FilterElementIdRule";
            //    //                    break;
            //    //                case FilterGlobalParameterAssociationRule _:
            //    //                    rtype = "FilterGlobalParameterAssociationRule";
            //    //                    break;
            //    //                default:
            //    //                    rtype = "未识别或没有设置Rule";
            //    //                    break;
            //    //            }
            //    //        }
            //    //        //TaskDialog.Show("tt", efs.Count().ToString() + "\n" + rtype);
            //    //        TaskDialog.Show("tt", rtype + "\n" + "“" + paramName + "”" + comparator + ruleValue);
            //    //    }
            //    //}
            //    //确定规则是否为反转规则
            //    //foreach (ElementFilter filter in efs)
            //    //{
            //    //    ElementParameterFilter epf = filter as ElementParameterFilter;
            //    //    string rtype = "";
            //    //    foreach (FilterRule r in epf.GetRules())
            //    //    {
            //    //        if (r is FilterDoubleRule)
            //    //        {
            //    //            rtype = "FilterDoubleRule";
            //    //        }
            //    //        else if (r is FilterStringRule)
            //    //        {
            //    //            rtype = "FilterStringRule";
            //    //        }
            //    //        else if (r is FilterIntegerRule)
            //    //        {
            //    //            rtype = "FilterIntegerRule";
            //    //        }
            //    //        else rtype = "None";
            //    //        TaskDialog.Show("tt", efs.Count().ToString() + "\n" + rtype);
            //    //    }
            //    //}
            //    //TaskDialog.Show("tt", efs.Count.ToString());
            //}
            //else
            //{
            //    TaskDialog.Show("tt", "OR filter");
            //}
            #endregion


            return Result.Succeeded;

            //GetGroupList();
            //Window1214 window1214 = new Window1214(doc, selGroupList);
            //window1214.Show();
        }
        public void LinkInstanceRemove(Document ddoc, FilteredElementCollector eIds)
        {
            foreach (var item in eIds)
            {
                ElementId eid = item.Id;
                ddoc.Delete(eid);
            }
        }
        /// <summary> 
        /// 初始化分组下拉数据
        /// </summary>
        //public void GetGroupList()
        //{
        //    Dictionary<int, string> dic = new Dictionary<int, string>();
        //    dic.Add(-1, "=请选择=");
        //    dic.Add(1, "AAA");
        //    dic.Add(2, "BBB");
        //    dic.Add(3, "CCC");
        //    //List<sys_Right_Group> groupList = DataBaseService.DataBaseServer<sys_Right_Group>.GetModelList(" IsActive=1 ");
        //    //if (groupList != null)
        //    //{
        //    //    groupList.ForEach(x =>
        //    //    {
        //    //        dic.Add(x.GroupID, x.GroupName);
        //    //    });
        //    //}
        //    selGroupList = dic;
        //    Group = -1; //默认选中第一项 
        //}
        //Dictionary<int, string> _selGroupList;
        //public Dictionary<int, string> selGroupList
        //{
        //    get { return _selGroupList; }
        //    set
        //    {
        //        _selGroupList = value;
        //        OnPropertyChanged("selGroupList");
        //    }
        //}
        //private int _Group;
        //public int Group
        //{
        //    get { return _Group; }
        //    set
        //    {
        //        _Group = value;
        //        //OnPropertyChanged(() => Group);
        //    }
        //}
        public static ElementFilter CreateElementFilterFromFilterRules(IList<FilterRule> filterRules)
        {
            IList<ElementFilter> elemFilters = new List<ElementFilter>();
            foreach (FilterRule filterRule in filterRules)
            {
                ElementParameterFilter elemParamFilter = new ElementParameterFilter(filterRule);
                elemFilters.Add(elemParamFilter);
            }
            LogicalAndFilter elemFilter = new LogicalAndFilter(elemFilters);
            return elemFilter;
        }
    }

    //[Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    //public class Command : IExternalCommand
    //{
    //    #region IExternalCommand Members
    //    public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        // set out default result to failure.
    //        Autodesk.Revit.UI.Result retRes = Autodesk.Revit.UI.Result.Failed;
    //        Autodesk.Revit.UI.UIApplication app = commandData.Application;

    //        // get the elements selected
    //        // The current selection can be retrieved from the active 
    //        // document via the selection object
    //        ElementSet seletion = new ElementSet();
    //        foreach (ElementId elementId in app.ActiveUIDocument.Selection.GetElementIds())
    //        {
    //            seletion.Insert(app.ActiveUIDocument.Document.GetElement(elementId));
    //        }

    //        // we need to make sure that only one element is selected.
    //        if (seletion.Size == 1)
    //        {
    //            // we need to get the first and only element in the selection. Do this by getting 
    //            // an iterator. MoveNext and then get the current element.
    //            ElementSetIterator it = seletion.ForwardIterator();
    //            it.MoveNext();
    //            Element element = it.Current as Element;

    //            // Next we need to iterate through the parameters of the element,
    //            // as we iterating, we will store the strings that are to be displayed
    //            // for the parameters in a string list "parameterItems"
    //            List<string> parameterItems = new List<string>();
    //            ParameterSet parameters = element.Parameters;
    //            foreach (Parameter param in parameters)
    //            {
    //                if (param == null) continue;

    //                // We will make a string that has the following format,
    //                // name type value
    //                // create a StringBuilder object to store the string of one parameter
    //                // using the character '\t' to delimit parameter name, type and value 
    //                StringBuilder sb = new StringBuilder();

    //                // the name of the parameter can be found from its definition.
    //                sb.AppendFormat("{0}\t", param.Definition.Name);

    //                // Revit parameters can be one of 5 different internal storage types:
    //                // double, int, string, Autodesk.Revit.DB.ElementId and None. 
    //                // if it is double then use AsDouble to get the double value
    //                // then int AsInteger, string AsString, None AsStringValue.
    //                // Switch based on the storage type
    //                switch (param.StorageType)
    //                {
    //                    case Autodesk.Revit.DB.StorageType.Double:
    //                        // append the type and value
    //                        sb.AppendFormat("double\t{0}", param.AsDouble());
    //                        break;
    //                    case Autodesk.Revit.DB.StorageType.ElementId:
    //                        // for element ids, we will try and retrieve the element from the 
    //                        // document if it can be found we will display its name.
    //                        sb.Append("Element\t");

    //                        // using ActiveDocument.GetElement(the element id) to 
    //                        // retrieve the element from the active document
    //                        Autodesk.Revit.DB.ElementId elemId = new ElementId(param.AsElementId().IntegerValue);
    //                        Element elem = app.ActiveUIDocument.Document.GetElement(elemId);

    //                        // if there is an element then display its name, 
    //                        // otherwise display the fact that it is not set
    //                        sb.Append(elem != null ? elem.Name : "Not set");
    //                        break;
    //                    case Autodesk.Revit.DB.StorageType.Integer:
    //                        // append the type and value
    //                        sb.AppendFormat("int\t{0}", param.AsInteger());
    //                        break;
    //                    case Autodesk.Revit.DB.StorageType.String:
    //                        // append the type and value
    //                        sb.AppendFormat("string\t{0}", param.AsString());
    //                        break;
    //                    case Autodesk.Revit.DB.StorageType.None:
    //                        // append the type and value
    //                        sb.AppendFormat("none\t{0}", param.AsValueString());
    //                        break;
    //                    default:
    //                        break;
    //                }

    //                // add the completed line to the string list
    //                parameterItems.Add(sb.ToString());
    //            }
    //            // Create our dialog, passing it the parameters array for display.
    //            PropertiesForm propertiesForm = new PropertiesForm(parameterItems.ToArray());
    //            propertiesForm.ShowDialog();
    //            retRes = Autodesk.Revit.UI.Result.Succeeded;
    //        }
    //        else
    //        {
    //            message = "Please select only one element";
    //        }
    //        return retRes;
    //    }
    //    #endregion
    //}

}
