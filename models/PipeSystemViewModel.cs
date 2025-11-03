using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.models
{
    public class PipeSystemViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public UIDocument UIDocument { get; set; }
        public ObservableCollection<PipeSystemEntity> PipeSystemEntitys { get; set; } = new ObservableCollection<PipeSystemEntity>();
        public PipeSystemViewModel(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            UIDocument = uIApplication.ActiveUIDocument as UIDocument;
            LoadAndInitializePipeSystems();
            QueryELements(null);
        }
        private void LoadAndInitializePipeSystems()
        {
            // --- 核心修改部分：使用 TransactionGroup ---
            TransactionGroup tg = new TransactionGroup(Doc, "初始化管道系统设置");
            try
            {
                tg.Start();
                // 1. 查询所有管道系统类型
                var elements = new FilteredElementCollector(Doc).OfClass(typeof(PipingSystemType));
                List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
                var defaultMaterialId = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Materials).FirstOrDefault().Id;
                // 2. 在一个单独的事务中设置默认颜色
                using (var trans = new Transaction(Doc, "设置默认系统颜色材质"))
                {
                    trans.Start();
                    bool changesMade = false;
                    Random rand = new Random();
                    foreach (var pst in pipingSystemTypes)
                    {
                        // 检查颜色是否有效，如果无效，则设置一个随机的默认颜色
                        if (!pst.LineColor.IsValid)
                        {
                            //byte r = (byte)rand.Next(50, 220);  
                            //byte g = (byte)rand.Next(50, 220);
                            //byte b = (byte)rand.Next(50, 220);
                            //pst.LineColor = new Autodesk.Revit.DB.Color(r, g, b);
                            pst.LineColor = new Autodesk.Revit.DB.Color(0, 0, 0);
                            changesMade = true;
                        }
                        if (pst.MaterialId.IntegerValue == -1)
                        {
                            pst.MaterialId = defaultMaterialId;
                            changesMade = true;
                        }
                    }
                    if (changesMade) trans.Commit();
                    else trans.RollBack(); // 如果没有做任何修改，则回滚事务
                }
                // 3. 将处理后的数据加载到ViewModel中
                //PipeSystemEntitys.Clear();
                //var pipeSystems = pipingSystemTypes.Select(pst => new PipeSystemEntity(pst)).ToList();
                //foreach (var item in pipeSystems)
                //{
                //    PipeSystemEntitys.Add(item);
                //}
                // 4. 同化事务组，将所有子事务合并成一个撤销操作
                tg.Assimilate();
            }
            catch (Exception ex)
            {
                // 如果发生任何错误，回滚整个事务组
                tg.RollBack();
                TaskDialog.Show("错误", "初始化管道系统时出错: " + ex.Message);
            }
        }
        public ICommand SelectSystemCommand =>new Form.RelayCommand<IEnumerable<object>>(SelectSystems);
        private void SelectSystems(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count()==0)
            {
                TaskDialog.Show("tt", "未选择任何系统，请重试");
                return;
            }
            List<PipeSystemEntity> selectedItems = selectedElements.Cast<PipeSystemEntity>().ToList();
            if (selectedElements == null) return;
            List<ElementId> ids = new List<ElementId>();
            foreach (var system in selectedItems)
            {
                foreach (var item in system.selectedElements)
                {
                    if (item is ElementId id)
                    {
                        ids.Add(id);
                    }
                }
            }
            Selection select = UIDocument.Selection;
            select.SetElementIds(ids);
        }
        //添加保温命令完结，关闭后需要关闭主窗体，否则窗口会失效
        //还不能简单命令改从后台启动，command绑定属性也不对
        public ICommand AddInsulationCommand => new Form.RelayCommand<PipeSystemEntity>(AddInsulation);
        public void AddInsulation(PipeSystemEntity pipeSystem)
        {
            //PipeInsulationAddView pipeInsulationAdd = new PipeInsulationAddView(pipeSystem);
            //pipeInsulationAdd.ShowDialog();
        }
        public ICommand WindowCommand => new BaseBindingCommand(Window);
        //修改材质直接开系统的吧，怎么接收修改？
        public void Window(object para)
        {
            MainWindow materialManager = new MainWindow(Doc);
            //MaterialManagerView materialManager = new MaterialManagerView(Doc);
            bool? result = materialManager.ShowDialog();
            if (result == true) { QueryELements(null); }
        }
        public void AddElement(Document document)
        {
            //PipeSystemAddView pipeSystemAddView = new PipeSystemAddView(document);
            //pipeSystemAddView.ShowDialog();
        }
        //检查系统是否为空
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(PipeSystemEntity systemType)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), systemType.SystemName, false));
            IList<Element> allpipes = new FilteredElementCollector(systemType.Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            IList<Element> allfamily = new FilteredElementCollector(systemType.Document)
                 .WhereElementIsNotElementType()
                 .OfClass(typeof(FamilyInstance))
                 .WherePasses(filter)
                 .ToElements();

            int countEntity = allpipes.Count() + allfamily.Count();
            if (countEntity == 0) { return true; }
            else return false;
        }
        //检查系统唯一性
        private bool isLastSystemEntity { get; set; }
        public bool IsLastSystemEntity(PipeSystemEntity systemType)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfClass(typeof(MEPSystemType));
            List<MEPSystemType> systemTypes = elems.OfType<MEPSystemType>().ToList();
            int systemCount = 0;
            foreach (MEPSystemType item in systemTypes)
            {
                if (systemType.MEPSystemClassOrigin == item.SystemClassification)
                {
                    systemCount++;
                }
            }
            if (systemCount > 1)
            {
                return false;
            }
            return true;
        }
        public ICommand DeleteELementCommand => new Form.RelayCommand<IEnumerable<object>>(DeleteElements);
        public ICommand DeleteELementCommand2 => new Form.RelayCommand<PipeSystemEntity>(DeleteElement);
        //多选删除
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            List<PipeSystemEntity> selectedItems = selectedElements.Cast<PipeSystemEntity>().ToList();
            if (selectedElements == null) return;
            foreach (var item in selectedItems)
            {
                DeleteElement(item);
            }
        }
        //单选删除
        public void DeleteElement(PipeSystemEntity pipingSystemSingle)
        {
            Document document = pipingSystemSingle.Document;
            isLastSystemEntity = IsLastSystemEntity(pipingSystemSingle);
            isEmptySystem = IsEmptySystem(pipingSystemSingle);
            if (!isLastSystemEntity)
            {
                if (isEmptySystem)
                {
                    document.NewTransaction(() =>
                    {
                        document.Delete(pipingSystemSingle.pipingSystemType.Id);
                        PipeSystemEntitys.Remove(pipingSystemSingle);
                    }, "删除系统");
                    OnPropertyChanged(nameof(PipingSystemCount));
                }
                else
                {
                    TaskDialog td = new TaskDialog("tt");
                    td.MainInstruction = "选定的系统类型正在使用，因此不能删除";
                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    td.Show();
                }
            }
            else
            {
                TaskDialog td = new TaskDialog("tt");
                td.MainInstruction = "不可删除该类型最后一个系统实例";
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.Show();
            }
        }
        public ICommand SetColorCommand => new Form.RelayCommand<PipingSystemType>(SetColor);
        private void SetColor(PipingSystemType pipingSystemType)
        {
            if (pipingSystemType == null) return;
            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.AllowFullOpen = true;
            dialog.FullOpen = true;
            dialog.ShowHelp = true;
            Autodesk.Revit.DB.Color color = pipingSystemType.LineColor;
            dialog.Color = System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pipingSystemType.Document.NewTransaction(() => pipingSystemType.LineColor = dialog.Color.ConvertToRevitColor(), "修改线颜色");
                QueryELements(null);
            }
            ;
        }
        public ICommand QueryELementCommand => new BaseBindingCommand(QueryELements);
        public void QueryELements(object parameter)
        {
            PipeSystemEntitys.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(PipingSystemType));
            List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
            List<PipeSystemEntity> pipeSystems = pipingSystemTypes
                .Select(pipingSystemType => new PipeSystemEntity(pipingSystemType))
                .Where(e => string.IsNullOrEmpty(Keyword)
                || e.SystemName.Contains(Keyword)
                || e.Abbreviation.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            foreach (var item in pipeSystems)
            {
                PipeSystemEntitys.Add(item);
            }
        }
        public string PipingSystemCount => PipeSystemEntitys.Count.ToString();
        private string _keyword;
        public string Keyword
        {
            get => _keyword; set => _keyword = value;
        }
    }
    public class LastSystemTextColor : IValueConverter
    {
        Document Document { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PipingSystemType pipingSystem = value as PipingSystemType;
            PipeSystemEntity pipeSystemEntity = new PipeSystemEntity(pipingSystem);
            Document document = pipeSystemEntity.Document;
            Document = document;

            bool lastSystem = IsLastSystemEntity(pipeSystemEntity);
            if (!lastSystem)
            {
                return new SolidColorBrush(Colors.Black);
            }
            return new SolidColorBrush(Colors.Red);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public bool IsLastSystemEntity(PipeSystemEntity systemType)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Document).OfClass(typeof(MEPSystemType));
            List<MEPSystemType> systemTypes = elems.OfType<MEPSystemType>().ToList();
            int systemCount = 0;
            foreach (MEPSystemType item in systemTypes)
            {
                if (systemType.MEPSystemClassOrigin == item.SystemClassification)
                {
                    systemCount++;
                }
            }
            if (systemCount > 1)
            {
                return false;
            }
            return true;
        }
    }
    public class BackgroundToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Brush brush)                // 确保传入的是Brush类型
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
    //public class PipeSystemViewModel : ObserverableObject
    //{
    //    private Document _doc;
    //    public Document Doc { get => _doc; set => _doc = value; }
    //    public List<Element> pipes = new List<Element>();
    //    public List<Element> pipefittings = new List<Element>();
    //    public ElementId insulationID;
    //    private ObservableCollection<string> items = new ObservableCollection<string>();
    //    private ObservableCollection<string> selectedItems = new ObservableCollection<string>();
    //    public ObservableCollection<string> SelectedItems { get => selectedItems; set => SetProperty(ref selectedItems, value); }
    //    public ObservableCollection<string> Items { get => items; set => SetProperty(ref items, value); }
    //    //private List<string> items = new List<string>();
    //    //private List<string> selectedItems = new List<string>();
    //    //public List<string> SelectedItems { get => selectedItems; set => SetProperty(ref selectedItems, value); }
    //    //public List<string> Items { get => items; set => SetProperty(ref items, value); }
    //    public PipeSystemViewModel(Document document)
    //    {
    //        Doc = document;
    //        //收集系统
    //        FilteredElementCollector elements = new FilteredElementCollector(_doc).OfClass(typeof(PipingSystemType));
    //        List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
    //        //// 使用 OfType 直接过滤并转换类型转换为 List
    //        pipeSystemEntitys = new ObservableCollection<PipeSystemEntity>(pipingSystemTypes.ConvertAll(new Converter<PipingSystemType, PipeSystemEntity>(pipingSystemType => new PipeSystemEntity(pipingSystemType))).ToList());
    //        foreach (var item in pipeSystemEntitys)
    //        {
    //            string sysName = item.SystemName;
    //            systemNames.Add(sysName);
    //            //items.Add(sysName);
    //        }
    //    }
    //    public BaseBindingCommand TestCommand => new BaseBindingCommand(Test);
    //    public void Test(Object para)
    //    {
    //        TaskDialog.Show("tt", SelectedItems.Count().ToString());
    //    }
    //    public RelayCommand<string> AddInsulationCommand => new RelayCommand<string>(AddInsulation);
    //    public void AddInsulation(string pipingSystem)
    //    {
    //        foreach (var SelectedDN in SelectedItems)
    //        {
    //            GetInstancesFunc(pipingSystem, SelectedDN);
    //        }
    //        insulationID = GetInsulationID();
    //        double thick = 60 / 304.8;
    //        //double thick = GetThickness();
    //        //加保温层实现PipeInsulation.Create(Document, p, insulationID, pipethickness);
    //        //参数：文档、构件id集，保温id，厚度double)
    //        AddInsulationFunc(thick);
    //    }
    //    public void GetInstancesFunc(string pipingSystem, string singleDN)
    //    {
    //        ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), pipingSystem, false));
    //        IList<Element> allpipes = new FilteredElementCollector(Doc).WhereElementIsNotElementType()
    //            .OfCategory(BuiltInCategory.OST_PipeCurves).WherePasses(filter)
    //            .ToElements();
    //        foreach (Element p in allpipes) if (p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString() == singleDN)
    //            {
    //                pipes.Add(p);
    //            }

    //        IList<Element> allfittings = new FilteredElementCollector(Doc)
    //            .WhereElementIsNotElementType()
    //            .OfCategory(BuiltInCategory.OST_PipeFitting)
    //            .WherePasses(filter)
    //            .ToElements();
    //        if (allfittings.Count == 0)
    //        {
    //            // 处理没有找到任何管道配件的情况
    //            return;
    //        }
    //        foreach (Element p in allfittings)
    //        {
    //            string parameterName = "公称直径"; // 参数名称
    //            Parameter diameterParam = p.LookupParameter(parameterName);
    //            if (diameterParam == null) { diameterParam = p.LookupParameter("公称直径1"); }
    //            // 检查参数是否存在以及是否有值
    //            if (diameterParam != null && diameterParam.HasValue && !string.IsNullOrEmpty(diameterParam.AsValueString()))
    //            {
    //                string diameter = diameterParam.AsValueString();
    //                if (diameter == singleDN)
    //                {
    //                    pipefittings.Add(p);
    //                }
    //            }
    //        }
    //    }
    //    public ElementId GetInsulationID()
    //    {
    //        FilteredElementCollector elems = new FilteredElementCollector(Doc).WhereElementIsElementType()
    //            .OfCategory(BuiltInCategory.OST_PipeInsulations);
    //        ElementId id = elems.FirstOrDefault().Id;
    //        return id;
    //    }
    //    public bool AllowDNSelect { get { return (SelectedPipeSystem != null); } }
    //    private PipeSystemEntity selectedSYS;
    //    public PipeSystemEntity SelectedSYS
    //    {
    //        get => selectedSYS;
    //        set
    //        {

    //            // 1. 更新 selectedSYS 字段并触发对 "SelectedSYS" 的通知
    //            SetProperty(ref selectedSYS, value);
    //            // 2. 这是最关键的一步：根据新的 SelectedSYS 来更新 Items 集合。
    //            //    这个赋值操作会调用 Items 属性的 setter，从而通知UI刷新 MultiSelectComboBox。
    //            if (selectedSYS != null)
    //            {
    //                Items = new ObservableCollection<string>(selectedSYS.DNList);
    //            }
    //            else
    //            {
    //                // 如果没有选中的系统，就清空 Items 列表
    //                // 确保 Items 不为 null，或者使用 Items?.Clear()
    //                if (Items != null)
    //                {
    //                    Items.Clear();
    //                }
    //            }
    //            //selectedSYS = value;
    //            //OnPropertyChanged();
    //            //// 只有在 selectedSYS 不为空时才更新 items
    //            //if (selectedSYS != null)
    //            //{
    //            //    // --- 修改前 ---
    //            //    //items = selectedSYS.DNList;
    //            //    items = new ObservableCollection<string>(selectedSYS.DNList);
    //            //    OnPropertyChanged(nameof(Items));

    //            //    //// --- 修改后 (最小化修改) ---
    //            //    //// 用 DNList 的内容来创建一个新的 ObservableCollection 并赋给 Items 属性
    //            //    //Items = new ObservableCollection<string>(selectedSYS.DNList);
    //            //}
    //            //else
    //            //{
    //            //    // (建议) 当没有选中系统时，清空列表
    //            //    //Items.Clear();
    //            //}
    //        }
    //    }
    //    private string selectedPipeSystem;
    //    public string SelectedPipeSystem
    //    {
    //        get => selectedPipeSystem;
    //        set
    //        {
    //            // 1. 使用您的 void SetProperty 来更新字段并触发对 "SelectedPipeSystem" 的通知
    //            SetProperty(ref selectedPipeSystem, value);
    //            // 2. 手动触发任何依赖此属性的其他属性的通知
    //            OnPropertyChanged(nameof(AllowDNSelect));
    //            // 3. 更新下一个关联的属性，这将触发它的 setter
    //            SelectedSYS = PipeSystemEntitys.FirstOrDefault(pse => pse.SystemName == selectedPipeSystem);
    //            //selectedPipeSystem = value;
    //            //OnPropertyChanged();
    //            //OnPropertyChanged(nameof(AllowDNSelect));//有效可用更新
    //            //// 查找匹配的PipeSystemEntity对象
    //            //SelectedSYS = PipeSystemEntitys.FirstOrDefault(pse => pse.SystemName == selectedPipeSystem);
    //        }
    //    }
    //    private List<string> systemNames = new List<string>();
    //    public List<string> SystemNames { get => systemNames; set => systemNames = value; }
    //    private ObservableCollection<PipeSystemEntity> pipeSystemEntitys;
    //    public ObservableCollection<PipeSystemEntity> PipeSystemEntitys
    //    {
    //        get => pipeSystemEntitys;
    //        set => SetProperty(ref pipeSystemEntitys, value);
    //    }
    //    List<ElementId> pinsidtodelete = new List<ElementId>();
    //    List<ElementId> ptoreinsulate = new List<ElementId>();
    //    public List<ElementId> Pinsidtodelete { get => pinsidtodelete; set => pinsidtodelete = value; }
    //    public List<ElementId> Ptoreinsulate { get => ptoreinsulate; set => ptoreinsulate = value; }

    //    public void AddInsulationFunc(double pipethickness)
    //    {
    //        try
    //        {
    //            using (Transaction ts = new Transaction(Doc))
    //            {
    //                ts.Start("Add Insulation to pipes");
    //                foreach (Pipe p in pipes)
    //                {
    //                    var ins = PipeInsulation.GetInsulationIds(Doc, p.Id);
    //                    if (ins.Count() == 0)
    //                    {
    //                        PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p.Id, insulationID, pipethickness);
    //                    }
    //                    else
    //                    {
    //                        foreach (var f in PipeInsulation.GetInsulationIds(Doc, p.Id))
    //                        {
    //                            pinsidtodelete.Add(f);
    //                        }
    //                        ptoreinsulate.Add(p.Id);
    //                    }
    //                }
    //                ts.Commit();

    //                if (pipefittings.Count == 0)
    //                {
    //                    ExchangeInsulationFunc(pipethickness);
    //                    return;
    //                }
    //                else
    //                {
    //                    ts.Start("Add Insulation to pipe fittings");
    //                    foreach (var p in pipefittings)
    //                    {
    //                        var ins = PipeInsulation.GetInsulationIds(Doc, p.Id);
    //                        if (ins.Count() == 0)
    //                        {
    //                            PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p.Id, insulationID, pipethickness);
    //                        }
    //                        else
    //                        {
    //                            foreach (var f in PipeInsulation.GetInsulationIds(Doc, p.Id))
    //                            {
    //                                pinsidtodelete.Add(f);
    //                            }
    //                            ptoreinsulate.Add(p.Id);
    //                        }
    //                    }
    //                    ts.Commit();
    //                    ExchangeInsulationFunc(pipethickness);
    //                }
    //            }
    //            //TaskDialog.Show("Success", "已增加指定管道保温!");
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog.Show("Error", ex.Message);
    //        }
    //    }
    //    private void ExchangeInsulationFunc(double pipethickness)
    //    {
    //        try
    //        {
    //            using (Transaction ts = new Transaction(Doc))
    //            {
    //                if (pinsidtodelete.Count > 0)
    //                {
    //                    ts.Start("Override pipe insulation");
    //                    Doc.Delete(pinsidtodelete);
    //                    foreach (var p in ptoreinsulate)
    //                    {
    //                        try
    //                        {
    //                            PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p, insulationID, pipethickness);
    //                        }
    //                        catch
    //                        {
    //                        }
    //                    }
    //                    ts.Commit();
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog.Show("Error", ex.Message);
    //        }
    //    }
    //}
}
