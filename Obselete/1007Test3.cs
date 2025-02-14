namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]
    //public class _1007Test3 : IExternalCommand
    //{
    //    Document Document;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        Document doc = uiDoc.Document;
    //        Document = doc;
    //        Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        UIApplication uiApp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uiApp.Application;

    //        //FilterTestNew filterTest = new FilterTestNew(doc);
    //        //filterTest.ShowDialog();

    //        //ViewFiltersForm filtersForm = new ViewFiltersForm(uiApp);
    //        //filtersForm.ShowDialog();

    //        //1124 ListFilter等梳理完官方方法再补充
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //List<ParameterFilterElement> pfe = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();

    //        //StringBuilder 
    //        //foreach (var item in pfe)
    //        //{
    //        //    sb.Append(item.Name.ToString()+"\n");
    //        //}
    //        //TaskDialog.Show("tt", sb.ToString());
    //        //List<FilterRule> rules = collector.OfClass(typeof(FilterRule)).Cast<FilterRule>().ToList();
    //        //string test = rules.FirstOrDefault(). ToString();

    //        //TypeSelector selector = new TypeSelector();
    //        //selector.typecombo.ItemsSource = cabletrytypes;
    //        //selector.typecombo.DisplayMemberPath = "Name";
    //        //selector.typecombo.SelectedIndex = 0;
    //        //selector.ShowDialog();

    //        //var targettypeName = selector.typeName.Text;
    //        //var typeNoteText = selector.noteText.Text;
    //        //if (string.IsNullOrWhiteSpace(targettypeName) || targettypeName == "新类型名称")
    //        //{
    //        //    MessageBox.Show("名称错误");
    //        //    return Result.Cancelled;
    //        //}

    //        //var targettype = selector.typecombo.SelectedItem as CableTrayType;

    //        //var elbowpara = targettype.LookupParameter("水平弯头");
    //        //var teepara = targettype.LookupParameter("T 形三通");
    //        //var verticalElbowParaIN = targettype.LookupParameter("垂直内弯头");
    //        //var verticalElbowParaOUT = targettype.LookupParameter("垂直外弯头");
    //        //var transitionPara = targettype.LookupParameter("过渡件");
    //        //var unionPara = targettype.LookupParameter("活接头");
    //        //var elbow = doc.GetElement(elbowpara.AsElementId()) as FamilySymbol;
    //        //var tee = doc.GetElement(teepara.AsElementId()) as FamilySymbol;
    //        //var verticalElbowIn = doc.GetElement(verticalElbowParaIN.AsElementId()) as FamilySymbol;
    //        //var verticalElbowOUT = doc.GetElement(verticalElbowParaOUT.AsElementId()) as FamilySymbol;
    //        //var transition = doc.GetElement(transitionPara.AsElementId()) as FamilySymbol;
    //        //var union = doc.GetElement(unionPara.AsElementId()) as FamilySymbol;

    //        //Transaction ts = new Transaction(doc, "创建新桥架类型");
    //        //ts.Start();
    //        ////创建新的连接件类型
    //        //var newelbow = elbow.Duplicate(targettypeName);
    //        //var newtee = tee.Duplicate(targettypeName);
    //        //var newverticalelbowIn = verticalElbowIn.Duplicate(targettypeName);
    //        //var newverticalelbowOut = verticalElbowOUT.Duplicate(targettypeName);
    //        //var newtransition = transition.Duplicate(targettypeName);
    //        //var newunion = union.Duplicate(targettypeName);
    //        //doc.Regenerate();

    //        //var newtype = targettype.Duplicate(targettypeName);
    //        //var list = new List<Element>() { newelbow, newtee, newverticalelbowOut, newverticalelbowIn, newtransition, newunion, newtype };

    //        //foreach (var element in list)
    //        //{
    //        //    var typeNotePara = element.LookupParameter("类型注释");
    //        //    typeNotePara.Set(typeNoteText);
    //        //}
    //        //var newelbowpara = newtype.LookupParameter("水平弯头");
    //        //var newteepara = newtype.LookupParameter("T 形三通");
    //        //var newverticalElbowParaIN = newtype.LookupParameter("垂直内弯头");
    //        //var newverticalElbowParaOUT = newtype.LookupParameter("垂直外弯头");
    //        //var newtransitionPara = newtype.LookupParameter("过渡件");
    //        //var newunionPara = newtype.LookupParameter("活接头");
    //        //newelbowpara.Set(newelbow.Id);
    //        //newteepara.Set(newtee.Id);
    //        //newverticalElbowParaIN.Set(newverticalelbowIn.Id);
    //        //newverticalElbowParaOUT.Set(newverticalelbowOut.Id);
    //        //newtransitionPara.Set(newtransition.Id);
    //        //newunionPara.Set(newunion.Id);
    //        //ts.Commit();
    //        //selector.Close();


    //        return Result.Succeeded;

    //        //列出所有过滤器及其类别，规则，有缺省
    //        //ListFilters();

    //        //1217 过滤器model测试OK
    //        //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement));
    //        //List<ParameterFilterElement> pfe = elems.OfType<ParameterFilterElement>().ToList();
    //        //List<ViewFilterModel> viewFilterModels = pfe.Select(pfee => new ViewFilterModel(pfee)).ToList();
    //        //ViewFilterModel vf = viewFilterModels[2];
    //        //TaskDialog.Show("tt", vf.ViewFilterName+"\n"+vf.CategoryItems.Count().ToString());

    //        //string ruleData = string.Empty;
    //        //string categories = string.Empty;
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //List<ParameterFilterElement> pfe = collector.OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
    //        //foreach (ElementId catid in pfe.FirstOrDefault().GetCategories())
    //        //{
    //        //    categories += doc.Settings.Categories.get_Item(((BuiltInCategory)catid.IntegerValue)).Name + ",";
    //        //}
    //        ////TaskDialog.Show("tt", categories);
    //        ////1124此处rules替代老语法 pfe.GetRules() 

    //        //foreach (var item in pfe)
    //        //{
    //        //    ElementLogicalFilter elf = item.GetElementFilter() as ElementLogicalFilter;
    //        //    if (elf != null)
    //        //    {
    //        //        IList<ElementFilter> efs = elf.GetFilters();
    //        //        ElementParameterFilter epf = efs.FirstOrDefault() as ElementParameterFilter;
    //        //        FilterRule rule = epf.GetRules().FirstOrDefault();
    //        //        if (rule is FilterInverseRule)
    //        //        {
    //        //            TaskDialog.Show("tt", rule.GetRuleParameter().IntegerValue.ToString());
    //        //        }
    //        //        else TaskDialog.Show("tt", "PASS");
    //        //    }
    //        //    continue;
    //        //}
    //        //if (elf != null)
    //        //{
    //        //    IList<ElementFilter> efs = elf.GetFilters();
    //        //    ElementParameterFilter epf = efs.FirstOrDefault() as ElementParameterFilter;
    //        //    FilterRule rule = epf.GetRules().FirstOrDefault();
    //        //    string comparator = "";
    //        //    string ruleValue = "";                
    //        //    if (rule is FilterDoubleRule)
    //        //    {
    //        //        FilterDoubleRule fdr = rule as FilterDoubleRule;
    //        //        if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
    //        //            comparator = "<";
    //        //        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
    //        //            comparator = ">";
    //        //        ruleValue = fdr.RuleValue.ToString();
    //        //    }
    //        //    else if (rule is FilterStringRule)
    //        //    {
    //        //        FilterStringRule fsr = rule as FilterStringRule;
    //        //        if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
    //        //            comparator = "starts with";
    //        //        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
    //        //            comparator = "ends with";
    //        //        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
    //        //            comparator = "=";
    //        //        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
    //        //            comparator = "contains";
    //        //        ruleValue = fsr.RuleString;
    //        //    }
    //        //    else if (rule is FilterIntegerRule)
    //        //    {
    //        //        FilterIntegerRule fir = rule as FilterIntegerRule;
    //        //        if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
    //        //            comparator = "=";
    //        //        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
    //        //            comparator = ">";
    //        //        if (((BuiltInParameter)rule.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
    //        //        {
    //        //            ruleValue = ((WallFunction)fir.RuleValue).ToString();
    //        //        }
    //        //        else
    //        //            ruleValue = fir.RuleValue.ToString();
    //        //    }
    //        //    string paramName = "";
    //        //    if (rule.GetRuleParameter().IntegerValue < 0)
    //        //        paramName = LabelUtils.GetLabelFor((BuiltInParameter)rule.GetRuleParameter().IntegerValue);
    //        //    else
    //        //        paramName = doc.GetElement(rule.GetRuleParameter()).Name;
    //        //    ruleData += "'" + paramName + "' " + comparator + " " + "'" + ruleValue.ToString() + "'" + Environment.NewLine;
    //        //    TaskDialog td = new TaskDialog("Rule");
    //        //    td.MainInstruction = "Filter name: " + pfe.FirstOrDefault().Name;
    //        //    td.MainContent = "Categories: " + categories + Environment.NewLine + Environment.NewLine + ruleData;
    //        //    td.Show();
    //        //}
    //        //else
    //        //    TaskDialog.Show("tt", pfe.FirstOrDefault().Name + "：未设置过滤器");

    //        //1206 直接查找所有filterrule
    //        //IList<Element> filterList = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).ToElements();
    //        //foreach (Element f in filterList)
    //        //{
    //        //    ParameterFilterElement pfe = f as ParameterFilterElement;
    //        //    ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
    //        //    IList<ElementFilter> efs = elf.GetFilters();
    //        //    foreach (ElementFilter ef in efs)
    //        //    {
    //        //        ElementParameterFilter epf = ef as ElementParameterFilter;
    //        //        string rtype = "";
    //        //        foreach (FilterRule r in epf.GetRules())
    //        //        {
    //        //            if (r is FilterDoubleRule)
    //        //            {
    //        //                rtype = "FilterDoubleRule";
    //        //            }
    //        //            else if (r is FilterStringRule)
    //        //            {
    //        //                rtype = "FilterStringRule";
    //        //            }
    //        //            else if (r is FilterIntegerRule)
    //        //            {
    //        //                rtype = "FilterIntegerRule";
    //        //            }
    //        //        }
    //        //        MessageBox.Show("found a rule <> " + rtype);
    //        //    }
    //        //}
    //        //例程结束

    //        //ICollection<ParameterFilterElement> filters = FiltersUtil.GetViewFilters(doc);
    //        //foreach (ParameterFilterElement filter in filters)
    //        //{
    //        //    ElementLogicalFilter elf = filter.GetElementFilter() as ElementLogicalFilter;
    //        //    //2020版前只有逻辑AND，后来加了OR
    //        //    if (elf != null)
    //        //    {
    //        //        IList<ElementFilter> efs = elf.GetFilters();//统计里面有几条规则 如果>1没法用这个sample
    //        //        TaskDialog.Show("tt",efs.Count().ToString());
    //        //        //ElementParameterFilter epf = efs.FirstOrDefault() as ElementParameterFilter;
    //        //        ////FilterRule rule = epf.GetRules().FirstOrDefault();
    //        //        //FilterRule rule = epf.GetRules().FirstOrDefault();
    //        //    }
    //        //    else
    //        //    {
    //        //        TaskDialog.Show("tt", "NOPASS");
    //        //    }
    //        //}     

    //        //1130  测试过滤器 墙长>5 显示为红色
    //        //OverrideGraphicSettings ogs = new OverrideGraphicSettings();
    //        //Color c = new Color(255, 0, 0);
    //        //FilteredElementCollector fillCollector = new FilteredElementCollector(doc);
    //        //List<Element> fplist = fillCollector.OfClass(typeof(FillPatternElement)).ToList();
    //        //ElementId solidId = fplist.FirstOrDefault(x => (x as FillPatternElement).GetFillPattern().IsSolidFill)?.Id;
    //        //FilteredElementCollector linePatternCollector = new FilteredElementCollector(doc);
    //        //linePatternCollector.OfClass(typeof(LinePatternElement));
    //        //ElementId linePatternId = linePatternCollector.ToElementIds().ToList().First();

    //        //ogs.SetProjectionLinePatternId(linePatternId);
    //        //ogs.SetProjectionLineColor(c);
    //        //ogs.SetProjectionLineWeight(1);
    //        //ogs.SetSurfaceForegroundPatternColor(c);
    //        //ogs.SetSurfaceForegroundPatternId(solidId);
    //        //ogs.SetSurfaceForegroundPatternVisible(true);
    //        //ogs.SetSurfaceTransparency(1);
    //        //ogs.SetHalftone(false);

    //        //Transaction ts = new Transaction(doc, "添加并使用过滤器");
    //        //ts.Start();
    //        //ICollection<ElementId> cglds = new List<ElementId>();
    //        //cglds.Add(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls).Id);
    //        //List<FilterRule> fRules = new List<FilterRule>();
    //        //ElementId lengthParaId = new ElementId(BuiltInParameter.CURVE_ELEM_LENGTH);
    //        //double limit = 8000 / 304.8;
    //        //fRules.Add(ParameterFilterRuleFactory.CreateGreaterRule(lengthParaId, limit, 0));
    //        //string str = "超过8m墙体";
    //        //ParameterFilterElement pfElement = ParameterFilterElement.Create(doc, str, cglds);
    //        //pfElement.SetElementFilter(new ElementParameterFilter(fRules));
    //        //activeView.AddFilter(pfElement.Id);
    //        //activeView.SetFilterVisibility(pfElement.Id, true);
    //        //activeView.SetFilterOverrides(pfElement.Id, ogs);
    //        //ts.Commit();
    //        //例程结束 

    //        ////1201 收集所有平立剖，三维视图，统计使用过滤器
    //        //FilteredElementCollector collector = new FilteredElementCollector(doc);
    //        //IList<Element> Views = collector.OfClass(typeof(View)).ToList();
    //        //string viewStr = string.Empty;
    //        //List<View> allViews =new List<View>();
    //        //foreach (var item in Views)
    //        //{
    //        //    View view = item as View;
    //        //    if (view == null || view.IsTemplate)
    //        //    {
    //        //        continue;
    //        //    }
    //        //    else
    //        //    {
    //        //        // 检查视图类型，排除明细表、图纸、图例和面积平面，仅包含平立剖，三维
    //        //        if (view.ViewType == ViewType.Schedule ||
    //        //            view.ViewType == ViewType.DrawingSheet ||
    //        //            view.ViewType == ViewType.Legend||
    //        //            view.ViewType == ViewType.AreaPlan)
    //        //        {
    //        //            continue;
    //        //        }
    //        //        else
    //        //        {
    //        //            ElementType objType = doc.GetElement(view.GetTypeId()) as ElementType;
    //        //            if (objType == null)
    //        //            {
    //        //                continue;
    //        //            }
    //        //            viewStr += view.Name + "\n";
    //        //            allViews.Add(view);
    //        //        }
    //        //    }
    //        //}
    //        ////TaskDialog.Show("tt", viewStr +"\n"+allViews.Count());
    //        //StringBuilder sb = new StringBuilder();
    //        //HashSet<ElementId> viewFilters = new HashSet<ElementId>();
    //        //foreach (View e in allViews)
    //        //{
    //        //    ICollection<ElementId> ids = e.GetFilters();
    //        //    foreach (var item in ids)
    //        //    {
    //        //        viewFilters.Add(item);
    //        //    }
    //        //}
    //        ////筛选出全部在使用过滤器
    //        //foreach (var item in viewFilters)
    //        //{
    //        //    sb.AppendLine(doc.GetElement(item).Name);
    //        //}
    //        //TaskDialog.Show("tt", sb.ToString());

    //        //List<ParameterFilterElement> collection = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
    //        ////foreach (ParameterFilterElement pfe in collection)
    //        //foreach (ParameterFilterElement pfe in filters)
    //        //{
    //        //    ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
    //        //    if (elf != null)
    //        //    {
    //        //        IList<ElementFilter> efs = elf.GetFilters();
    //        //        ElementParameterFilter epf = efs.FirstOrDefault() as ElementParameterFilter;
    //        //        //FilterRule rule = epf.GetRules().FirstOrDefault();
    //        //        FilterRule rule = epf.GetRules().FirstOrDefault();

    //        //        //TaskDialog.Show("tt", "PASS");
    //        //    }
    //        //    else
    //        //    {
    //        //        TaskDialog.Show("tt", pfe.Name + "NOPASS");
    //        //    }
    //        //}

    //        //StringBuilder sb = new StringBuilder();
    //        //IList<Element> filterList = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).ToElements(); 
    //        //TaskDialog.Show("tt", filterList.Count().ToString());
    //        //以下方法可以获得过滤器，但规则还要往下找GetRuLes
    //        //StringBuilder sb = new StringBuilder();
    //        //List<ParameterFilterElement> pfes = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
    //        //foreach (ParameterFilterElement pfe in pfes)
    //        //{
    //        //    FilterRule elementFilter = pfe.GetElementFilter ;
    //        //    sb.Append(pfe.Name + "\n");
    //        //}
    //        //TaskDialog.Show("tt", sb.ToString());

    //        //1212 线管测试OK，注意要无管件RunId才能用
    //        //Selection sel = uiDoc.Selection;
    //        //Element elem = sel.PickObject(ObjectType.Element, new filterMEPCurveClass(), "Select Start 元素").GetElement(doc);
    //        //Conduit temp = elem as Conduit;
    //        //double runLength = (temp.RunId.GetElement(doc) as ConduitRun).Length * 304.8;
    //        //TaskDialog.Show("tt", runLength.ToString());

    //        //1202 测试过滤 ListBoxCheckBox用的
    //        //FilterTest test = new FilterTest(doc);
    //        //test.ShowDialog();

    //        //_________________________

    //        //BatchFamilyExport_WPF batchFamilyExport = new BatchFamilyExport_WPF(uiapp);
    //        //batchFamilyExport.ShowDialog();
    //        //PipeSystemTest pipeSystem = new PipeSystemTest(doc);
    //        //pipeSystem.ShowDialog();

    //        //ProgressBarTest1 barTest = new ProgressBarTest1();
    //        //barTest.Show();
    //        //ProgressBarTest2 barTest = new ProgressBarTest2();
    //        //barTest.Show();
    //        //Window1 window = new Window1();
    //        //window.Show();
    //        //Window2 window = new Window2();
    //        //window.Show();

    //        //https://www.cnblogs.com/dongweian/p/14182576.html
    //        //public static string OSDescription { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    //        //public static string OSArchitecture { get; } = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
    //        //string sys = GetGPUInfo();
    //        //TaskDialog.Show("tt", sys);
    //        //RevitCommandId commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.RunInterferenceCheck);
    //        //uiapp.PostCommand(commandId);

    //        //MainWindow mainWindow = new MainWindow();
    //        //mainWindow.Show();

    //        //测试数据集
    //        //public ObservableCollection<Students> tests = new ObservableCollection<Students>() {
    //        //    new Students() { Id = 1, Age = 11, Name = "Tom"},
    //        //    new Students() { Id = 2, Age = 12, Name = "Darren"},
    //        //    new Students() { Id = 3, Age = 13, Name = "Jacky"},
    //        //    new Students() { Id = 4, Age = 14, Name = "Andy"}    };
    //        //public ObservableCollection<Students> Test { get => tests; set => tests = value; }
    //    }
    //    public void ListFilters()
    //    {
    //        Document doc = Document;

    //        List<ParameterFilterElement> collection = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>().ToList();
    //        if (collection.Count == 0)
    //        {
    //            TaskDialog.Show("tt", "未找到过滤器");
    //            return;
    //        }
    //        foreach (ParameterFilterElement pfe in collection)
    //        {
    //            string ruleData = "";
    //            string categories = "";

    //            foreach (ElementId catid in pfe.GetCategories())
    //            {
    //                categories += doc.Settings.Categories.get_Item(((BuiltInCategory)catid.IntegerValue)).Name + ",";
    //            }

    //            //1124此处rules替代老语法 pfe.GetRules() 
    //            ElementLogicalFilter elf = pfe.GetElementFilter() as ElementLogicalFilter;
    //            if (elf != null)
    //            {
    //                IList<ElementFilter> efs = elf.GetFilters();

    //                ElementParameterFilter epf = efs.FirstOrDefault() as ElementParameterFilter;
    //                List<FilterRule> rules = epf.GetRules().ToList();
    //                //FilterRule rule = epf.GetRules().FirstOrDefault();

    //                //FilterRule rule =rules.FirstOrDefault() as FilterRule;
    //                foreach (FilterRule rule in rules)
    //                {
    //                    string comparator = "";
    //                    string ruleValue = "";

    //                    if (rule is FilterDoubleRule)
    //                    {
    //                        FilterDoubleRule fdr = rule as FilterDoubleRule;
    //                        if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericLess)))
    //                            comparator = "<";
    //                        else if (fdr.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
    //                            comparator = ">";
    //                        ruleValue = fdr.RuleValue.ToString();
    //                    }
    //                    else if (rule is FilterStringRule)
    //                    {
    //                        FilterStringRule fsr = rule as FilterStringRule;
    //                        if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringBeginsWith)))
    //                            comparator = "starts with";
    //                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEndsWith)))
    //                            comparator = "ends with";
    //                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringEquals)))
    //                            comparator = "=";
    //                        else if (fsr.GetEvaluator().GetType().Equals(typeof(FilterStringContains)))
    //                            comparator = "contains";
    //                        ruleValue = fsr.RuleString;
    //                    }
    //                    else if (rule is FilterIntegerRule)
    //                    {
    //                        FilterIntegerRule fir = rule as FilterIntegerRule;
    //                        if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericEquals)))
    //                            comparator = "=";
    //                        else if (fir.GetEvaluator().GetType().Equals(typeof(FilterNumericGreater)))
    //                            comparator = ">";

    //                        if (((BuiltInParameter)rule.GetRuleParameter().IntegerValue) == BuiltInParameter.FUNCTION_PARAM)
    //                        {
    //                            ruleValue = ((WallFunction)fir.RuleValue).ToString();
    //                        }
    //                        else
    //                            ruleValue = fir.RuleValue.ToString();
    //                    }
    //                    string paramName = "";

    //                    if (rule.GetRuleParameter().IntegerValue < 0)
    //                        paramName = LabelUtils.GetLabelFor((BuiltInParameter)rule.GetRuleParameter().IntegerValue);
    //                    else
    //                        paramName = doc.GetElement(rule.GetRuleParameter()).Name;
    //                    ruleData += "'" + paramName + "' " + comparator + " " + "'" + ruleValue.ToString() + "'" + Environment.NewLine;
    //                    TaskDialog td = new TaskDialog("Rule");
    //                    td.MainInstruction = "Filter name: " + pfe.Name;
    //                    td.MainContent = "Categories: " + categories + Environment.NewLine + Environment.NewLine + ruleData;
    //                    td.Show();
    //                }

    //            }
    //            else
    //                TaskDialog.Show("tt", pfe.Name + "：未设置过滤器");
    //        }
    //        //1124 参来源https://boostyourbim.wordpress.com/2016/05/11/filter-rule-data-where-is-it-hiding/
    //        //1124 https://forums.autodesk.com/t5/revit-api-forum/how-get-filter-rule-info/m-p/8116747#M32147
    //    }
    //    public static string GetComputerVersion()
    //    {
    //        var version = new StringBuilder();
    //        var moc = new ManagementClass("Win32_ComputerSystemProduct").GetInstances();
    //        foreach (ManagementObject mo in moc)
    //        {
    //            foreach (var item in mo.Properties)
    //            {
    //                version.Append($"{item.Name}:{item.Value}\r\n");
    //            }
    //        }
    //        return version.ToString(); ;
    //    }
    //    public static string GetCPUInfo()
    //    {
    //        var cpu = new StringBuilder();
    //        var moc = new ManagementClass("Win32_Processor").GetInstances();
    //        foreach (var mo in moc)
    //        {
    //            foreach (var item in mo.Properties)
    //            {
    //                cpu.Append($"{item.Name}:{item.Value}\r\n");
    //            }
    //        }
    //        return cpu.ToString();
    //    }
    //    public static string GetRAMInfo()
    //    {
    //        var ram = new StringBuilder();
    //        var searcher = new ManagementObjectSearcher()
    //        {
    //            Query = new SelectQuery("Win32_PhysicalMemory"),
    //        }.Get().GetEnumerator();

    //        while (searcher.MoveNext())
    //        {
    //            ManagementBaseObject baseObj = searcher.Current;
    //            foreach (var item in baseObj.Properties)
    //            {
    //                ram.Append($"{item.Name}:{item.Value}\r\n");
    //            }
    //        }

    //        searcher = new ManagementObjectSearcher()
    //        {
    //            Query = new SelectQuery("Win32_PerfRawData_PerfOS_Memory"),
    //        }.Get().GetEnumerator();

    //        while (searcher.MoveNext())
    //        {
    //            ManagementBaseObject baseObj = searcher.Current;
    //            foreach (var item in baseObj.Properties)
    //            {
    //                ram.Append($"{item.Name}:{item.Value}\r\n");
    //            }
    //        }
    //        return ram.ToString();
    //    }
    //    public static string GetGPUInfo()
    //    {
    //        var gpu = new StringBuilder();
    //        var moc = new ManagementObjectSearcher("select * from Win32_VideoController").Get();

    //        foreach (var mo in moc)
    //        {
    //            foreach (var item in mo.Properties)
    //            {
    //                gpu.Append($"{item.Name}:{item.Value}\r\n");
    //            }
    //        }
    //        return gpu.ToString(); ;
    //    }
    //    //测试数据集
    //    //public class Students
    //    //{
    //    //    public int Id { get; set; }
    //    //    public string Name { get; set; }
    //    //    public int Age { get; set; }
    //    //}
    //}
}



