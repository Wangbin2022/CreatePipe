using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CreatePipe.models
{
    public class PipeSystemViewModel : ObserverableObject
    {
        private Document _doc;
        public Document Doc { get => _doc; set => _doc = value; }
        public List<Element> pipes = new List<Element>();
        public List<Element> pipefittings = new List<Element>();
        public ElementId insulationID;
        private ObservableCollection<string> items = new ObservableCollection<string>();
        private ObservableCollection<string> selectedItems = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedItems { get => selectedItems; set => SetProperty(ref selectedItems, value); }
        public ObservableCollection<string> Items { get => items; set => SetProperty(ref items, value); }
        //private List<string> items = new List<string>();
        //private List<string> selectedItems = new List<string>();
        //public List<string> SelectedItems { get => selectedItems; set => SetProperty(ref selectedItems, value); }
        //public List<string> Items { get => items; set => SetProperty(ref items, value); }
        public PipeSystemViewModel(Document document)
        {
            Doc = document;
            //收集系统
            FilteredElementCollector elements = new FilteredElementCollector(_doc).OfClass(typeof(PipingSystemType));
            List<PipingSystemType> pipingSystemTypes = elements.OfType<PipingSystemType>().ToList();
            //// 使用 OfType 直接过滤并转换类型转换为 List
            pipeSystemEntitys = new ObservableCollection<PipeSystemEntity>(pipingSystemTypes.ConvertAll(new Converter<PipingSystemType, PipeSystemEntity>(pipingSystemType => new PipeSystemEntity(pipingSystemType))).ToList());
            foreach (var item in pipeSystemEntitys)
            {
                string sysName = item.SystemName;
                systemNames.Add(sysName);
                //items.Add(sysName);
            }
        }
        public BaseBindingCommand TestCommand => new BaseBindingCommand(Test);
        public void Test(Object para)
        {
            TaskDialog.Show("tt", SelectedItems.Count().ToString());
        }
        public RelayCommand<string> AddInsulationCommand => new RelayCommand<string>(AddInsulation);
        public void AddInsulation(string pipingSystem)
        {
            foreach (var SelectedDN in SelectedItems)
            {
                GetInstancesFunc(pipingSystem, SelectedDN);
            }
            insulationID = GetInsulationID();
            double thick = 60 / 304.8;
            //double thick = GetThickness();
            //加保温层实现PipeInsulation.Create(Document, p, insulationID, pipethickness);
            //参数：文档、构件id集，保温id，厚度double)
            AddInsulationFunc(thick);
        }
        public void GetInstancesFunc(string pipingSystem, string singleDN)
        {
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM), pipingSystem, false));
            IList<Element> allpipes = new FilteredElementCollector(Doc).WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves).WherePasses(filter)
                .ToElements();
            foreach (Element p in allpipes) if (p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsValueString() == singleDN)
                {
                    pipes.Add(p);
                }

            IList<Element> allfittings = new FilteredElementCollector(Doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .WherePasses(filter)
                .ToElements();
            if (allfittings.Count == 0)
            {
                // 处理没有找到任何管道配件的情况
                return;
            }
            foreach (Element p in allfittings)
            {
                string parameterName = "公称直径"; // 参数名称
                Parameter diameterParam = p.LookupParameter(parameterName);
                if (diameterParam == null) { diameterParam = p.LookupParameter("公称直径1"); }
                // 检查参数是否存在以及是否有值
                if (diameterParam != null && diameterParam.HasValue && !string.IsNullOrEmpty(diameterParam.AsValueString()))
                {
                    string diameter = diameterParam.AsValueString();
                    if (diameter == singleDN)
                    {
                        pipefittings.Add(p);
                    }
                }
            }
        }
        public ElementId GetInsulationID()
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_PipeInsulations);
            ElementId id = elems.FirstOrDefault().Id;
            return id;
        }
        public bool AllowDNSelect { get { return (SelectedPipeSystem != null); } }
        private PipeSystemEntity selectedSYS;
        public PipeSystemEntity SelectedSYS
        {
            get => selectedSYS;
            set
            {

                // 1. 更新 selectedSYS 字段并触发对 "SelectedSYS" 的通知
                SetProperty(ref selectedSYS, value);
                // 2. 这是最关键的一步：根据新的 SelectedSYS 来更新 Items 集合。
                //    这个赋值操作会调用 Items 属性的 setter，从而通知UI刷新 MultiSelectComboBox。
                if (selectedSYS != null)
                {
                    Items = new ObservableCollection<string>(selectedSYS.DNList);
                }
                else
                {
                    // 如果没有选中的系统，就清空 Items 列表
                    // 确保 Items 不为 null，或者使用 Items?.Clear()
                    if (Items != null)
                    {
                        Items.Clear();
                    }
                }
                //selectedSYS = value;
                //OnPropertyChanged();
                //// 只有在 selectedSYS 不为空时才更新 items
                //if (selectedSYS != null)
                //{
                //    // --- 修改前 ---
                //    //items = selectedSYS.DNList;
                //    items = new ObservableCollection<string>(selectedSYS.DNList);
                //    OnPropertyChanged(nameof(Items));

                //    //// --- 修改后 (最小化修改) ---
                //    //// 用 DNList 的内容来创建一个新的 ObservableCollection 并赋给 Items 属性
                //    //Items = new ObservableCollection<string>(selectedSYS.DNList);
                //}
                //else
                //{
                //    // (建议) 当没有选中系统时，清空列表
                //    //Items.Clear();
                //}
            }
        }
        private string selectedPipeSystem;
        public string SelectedPipeSystem
        {
            get => selectedPipeSystem;
            set
            {
                // 1. 使用您的 void SetProperty 来更新字段并触发对 "SelectedPipeSystem" 的通知
                SetProperty(ref selectedPipeSystem, value);
                // 2. 手动触发任何依赖此属性的其他属性的通知
                OnPropertyChanged(nameof(AllowDNSelect));
                // 3. 更新下一个关联的属性，这将触发它的 setter
                SelectedSYS = PipeSystemEntitys.FirstOrDefault(pse => pse.SystemName == selectedPipeSystem);
                //selectedPipeSystem = value;
                //OnPropertyChanged();
                //OnPropertyChanged(nameof(AllowDNSelect));//有效可用更新
                //// 查找匹配的PipeSystemEntity对象
                //SelectedSYS = PipeSystemEntitys.FirstOrDefault(pse => pse.SystemName == selectedPipeSystem);
            }
        }
        private List<string> systemNames = new List<string>();
        public List<string> SystemNames { get => systemNames; set => systemNames = value; }
        private ObservableCollection<PipeSystemEntity> pipeSystemEntitys;
        public ObservableCollection<PipeSystemEntity> PipeSystemEntitys
        {
            get => pipeSystemEntitys;
            set => SetProperty(ref pipeSystemEntitys, value);
        }
        List<ElementId> pinsidtodelete = new List<ElementId>();
        List<ElementId> ptoreinsulate = new List<ElementId>();
        public List<ElementId> Pinsidtodelete { get => pinsidtodelete; set => pinsidtodelete = value; }
        public List<ElementId> Ptoreinsulate { get => ptoreinsulate; set => ptoreinsulate = value; }

        public void AddInsulationFunc(double pipethickness)
        {
            try
            {
                using (Transaction ts = new Transaction(Doc))
                {
                    ts.Start("Add Insulation to pipes");
                    foreach (Pipe p in pipes)
                    {
                        var ins = PipeInsulation.GetInsulationIds(Doc, p.Id);
                        if (ins.Count() == 0)
                        {
                            PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p.Id, insulationID, pipethickness);
                        }
                        else
                        {
                            foreach (var f in PipeInsulation.GetInsulationIds(Doc, p.Id))
                            {
                                pinsidtodelete.Add(f);
                            }
                            ptoreinsulate.Add(p.Id);
                        }
                    }
                    ts.Commit();

                    if (pipefittings.Count == 0)
                    {
                        ExchangeInsulationFunc(pipethickness);
                        return;
                    }
                    else
                    {
                        ts.Start("Add Insulation to pipe fittings");
                        foreach (var p in pipefittings)
                        {
                            var ins = PipeInsulation.GetInsulationIds(Doc, p.Id);
                            if (ins.Count() == 0)
                            {
                                PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p.Id, insulationID, pipethickness);
                            }
                            else
                            {
                                foreach (var f in PipeInsulation.GetInsulationIds(Doc, p.Id))
                                {
                                    pinsidtodelete.Add(f);
                                }
                                ptoreinsulate.Add(p.Id);
                            }
                        }
                        ts.Commit();
                        ExchangeInsulationFunc(pipethickness);
                    }
                }
                //TaskDialog.Show("Success", "已增加指定管道保温!");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
        private void ExchangeInsulationFunc(double pipethickness)
        {
            try
            {
                using (Transaction ts = new Transaction(Doc))
                {
                    if (pinsidtodelete.Count > 0)
                    {
                        ts.Start("Override pipe insulation");
                        Doc.Delete(pinsidtodelete);
                        foreach (var p in ptoreinsulate)
                        {
                            try
                            {
                                PipeInsulation pipeInsulation = PipeInsulation.Create(Doc, p, insulationID, pipethickness);
                            }
                            catch
                            {
                            }
                        }
                        ts.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
    }
}
