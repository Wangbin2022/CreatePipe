using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CreatePipe.models
{
    public class CableTraySystemViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public CableTraySystemViewModel(Document document)
        {
            Doc = document;
            cableSystemEntitys = GetEntity();
            QueryELementCommand = new BaseBindingCommand(QueryElement);
            ApplySetupCommand = new RelayCommand<CableTrayEntity>(ApplySetup);
            SetLineColorCommand = new RelayCommand<CableTrayEntity>(SetLineColor);
            DeleteELementCommand = new RelayCommand<IEnumerable<object>>(DeleteELements);
            AddSystemCommand = new BaseBindingCommand(AddSystem);
        }

        private void AddSystem(object obj)
        {
            //验证新系统名称合法性
            if (!CheckName(CableSystemNames))
            {
                return;
            }
            //复制建立新CableTrayType及过滤器
            Doc.NewTransaction(() =>
            {
                FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
                List<CableTrayType> cableSystemTypes = elements.OfType<CableTrayType>().ToList();
                foreach (var item in cableSystemTypes)
                {
                    if (item.IsWithFitting)
                    {
                        cableTrayNewType = item;
                    }
                }
                CableTrayType newType = cableTrayNewType.Duplicate(Keyword) as CableTrayType;
                CableTrayEntity newEntity = new CableTrayEntity(newType);
                List<ElementId> cableTrayCategory = new List<ElementId>();
                ElementId elems1 = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_CableTray).FirstOrDefault().Category.Id;
                ElementId elems2 = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_CableTrayFitting).FirstOrDefault().Category.Id;
                cableTrayCategory.Add(elems1);
                cableTrayCategory.Add(elems2);
                ElementId ParaId = new ElementId(BuiltInParameter.RBS_CTC_SERVICE_TYPE);
                List<FilterRule> fRules = new List<FilterRule>();
                fRules.Add(ParameterFilterRuleFactory.CreateContainsRule(ParaId, Keyword, false));
                ElementParameterFilter pfilter = new ElementParameterFilter(fRules, false);
                IList<ElementFilter> elementfilters = new List<ElementFilter>();
                elementfilters.Add(pfilter);
                LogicalAndFilter wallfilter = new LogicalAndFilter(elementfilters);
                ParameterFilterElement pfElement = ParameterFilterElement.Create(Doc, Keyword + "过滤器", cableTrayCategory, wallfilter);
                //匹配过滤器
                CableSystemEntitys.Clear();
                FilteredElementCollector elems = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
                List<CableTrayType> cableSystems = elems.OfType<CableTrayType>().ToList();
                List<CableTrayEntity> cables = cableSystems
                    .Select(cableSystemType => new CableTrayEntity(cableSystemType))
                    .Where(e => e.IsWithFitting == true)
                    .Where(e => string.IsNullOrEmpty(Keyword)
                    || e.SystemName.Contains(Keyword)
                    || e.Abbreviation.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                foreach (var item in cables)
                {
                    CableSystemEntitys.Add(item);
                    if (item.SystemName == Keyword)
                    {
                        item.FilterName = Keyword + "过滤器";
                        //下句能设置但会报错
                        //item.Abbreviation = Keyword;
                    }
                }
            }, "新建系统");
        }
        CableTrayType cableTrayNewType { get; set; }
        public bool CheckName(ICollection<string> names)
        {
            bool result;
            names = CableSystemNames;
            string newName = Keyword;
            if (String.IsNullOrEmpty(newName))
            {
                return result = false;
            }
            // Check if filter name contains invalid characters
            // These character are different from Path.GetInvalidFileNameChars()
            char[] invalidFileChars = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };
            foreach (char invalidChr in invalidFileChars)
            {
                if (newName.Contains(invalidChr))
                {
                    return result = false;
                }
            }
            // Check if name is used
            // check if name is already used by other filters
            bool inUsed = names.Contains(newName, StringComparer.OrdinalIgnoreCase);
            if (inUsed)
            {
                return result = false;
            }
            //还要验证过滤器名称有无重复
            string pfeName = Keyword + "过滤器";
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(ParameterFilterElement));
            List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
            foreach (var item in pfe)
            {
                if (item.Name == pfeName)
                {
                    return result = false;
                }
            }
            return result = true;
        }
        public BaseBindingCommand AddSystemCommand { get; set; }
        private void DeleteELements(IEnumerable<object> selectedElements)
        {
            List<CableTrayEntity> selectedItems = selectedElements.Cast<CableTrayEntity>().ToList();
            if (selectedElements == null) return;
            foreach (var item in selectedItems)
            {
                DeleteElement(item);
            }
        }
        public void DeleteElement(CableTrayEntity cableTrayEntity)
        {
            Document document = cableTrayEntity.Document;
            isLastSystemEntity = IsLastSystemEntity();
            isEmptySystem = IsEmptySystem(cableTrayEntity);
            if (!isLastSystemEntity)
            {
                if (isEmptySystem)
                {
                    document.NewTransaction(() =>
                    {
                        document.Delete(cableTrayEntity.id);
                        CableSystemEntitys.Remove(cableTrayEntity);
                    }, "删除系统");
                    OnPropertyChanged(nameof(CableTrayCount));
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
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(CableTrayEntity cableTrayEntity)
        {//RBS_PIPING_SYSTEM_TYPE_PARAM
            string typeAbb = null;
            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            List<CableTray> cableTrays = collector.OfClass(typeof(CableTray))
               .Cast<CableTray>().ToList();
            FilteredElementCollector collector1 = new FilteredElementCollector(Doc);
            List<FamilyInstance> cableTrayFittings = collector1
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_CableTrayFitting)
                .Cast<FamilyInstance>().ToList();
            //List<Element> elements = new List<Element>();
            HashSet<ElementId> addedIds = new HashSet<ElementId>();
            foreach (CableTray item in cableTrays)
            {
                if (item.Name == cableTrayEntity.SystemName)
                {
                    addedIds.Add(item.Id);
                    // 确保typeAbb被赋值
                    typeAbb = item.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsString();
                    foreach (FamilyInstance item1 in cableTrayFittings)
                    {
                        if (item1.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsString() == typeAbb)
                        {
                            addedIds.Add(item1.Id);
                        }
                    }
                }
            }
            if (addedIds.Count() > 0)
            {
                return false;
            }
            else return true;
        }
        //检查系统唯一性
        private bool isLastSystemEntity { get; set; }
        public bool IsLastSystemEntity()
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
            List<CableTrayType> cableTrayTypes = elems.OfType<CableTrayType>().ToList();
            List<CableTrayType> CtA = new List<CableTrayType>();
            List<CableTrayType> CtB = new List<CableTrayType>();
            foreach (var item in cableTrayTypes)
            {
                if (item.IsWithFitting)
                {
                    CtA.Add(item);
                }
                else CtB.Add(item);

            }
            if (CtA.Count() == 1 || CtB.Count() == 1)
            {
                return true;
            }
            else return false;
        }
        public RelayCommand<IEnumerable<object>> DeleteELementCommand { get; set; }
        public string CableTrayCount => CableSystemEntitys.Count.ToString();
        public RelayCommand<CableTrayEntity> SetLineColorCommand { get; set; }
        private void SetLineColor(CableTrayEntity select)
        {
            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.AllowFullOpen = true;
            dialog.FullOpen = true;
            dialog.ShowHelp = true;
            dialog.Color = System.Drawing.Color.FromArgb(127, 127, 127);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                select.LineColor = dialog.Color.ConvertToRevitColor();
            }
        }
        ParameterFilterElement viewFilter { get; set; }
        private void ApplySetup(CableTrayEntity select)
        {
            if (select.FilterName != null)
            {

                FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(ParameterFilterElement));
                List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
                foreach (var item in pfe)
                {
                    if (item.Name == select.FilterName)
                    {
                        viewFilter = item;
                    }
                }
                Doc.NewTransaction(() =>
                {
                    List<View> allViews = GetAllViews();
                    foreach (View view in allViews)
                    {
                        if (view.GetFilters().Contains(viewFilter.Id))
                        {
                            view.RemoveFilter(viewFilter.Id);
                        }
                    }
                    foreach (View view in allViews)
                    {
                        if (!view.GetFilters().Contains(viewFilter.Id))
                        {
                            view.AddFilter(viewFilter.Id);
                            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                            ogs.SetSurfaceTransparency(select.TransparencyNum);
                            ogs.SetProjectionLineWeight(select.SelectedLineWeight);
                            FilteredElementCollector linePatternCollector = new FilteredElementCollector(Doc)
                            .OfClass(typeof(LinePatternElement));
                            List<ElementId> linePatternIds = linePatternCollector.ToElementIds().ToList();
                            foreach (var item in linePatternIds)
                            {
                                if (item != null && select.LinePatternElem != null)
                                {
                                    ogs.SetProjectionLinePatternId(item);
                                }
                                else
                                {
                                    //无赋值就是实现，第一个反而是虚线了
                                    //ElementId linePatternId = linePatternCollector.ToElementIds().ToList().First();
                                    //ogs.SetProjectionLinePatternId(linePatternId);
                                }
                            }
                            List<Element> fplist2 = new FilteredElementCollector(Doc).OfClass(typeof(FillPatternElement)).ToList();
                            ElementId solidId2 = fplist2.FirstOrDefault(x => (x as FillPatternElement).GetFillPattern().IsSolidFill)?.Id;
                            ogs.SetSurfaceForegroundPatternId(solidId2);
                            if (select.LineColor != null)
                            {
                                ogs.SetProjectionLineColor(select.LineColor);
                                ogs.SetSurfaceForegroundPatternColor(select.LineColor);
                            }
                            else
                            {
                                ogs.SetProjectionLineColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                                ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(127, 127, 127));
                            }
                            view.SetFilterOverrides(viewFilter.Id, ogs);
                        }
                    }
                }, "所有视图附加过滤器");
            }
            else TaskDialog.Show("tt", "未选择对应的过滤器，请重设置！");
        }
        private List<View> GetAllViews()
        {
            List<View> allViews = new List<View>();
            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            IList<Element> Views = collector.OfClass(typeof(View)).ToList();
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
                        ElementType objType = Doc.GetElement(view.GetTypeId()) as ElementType;
                        if (objType == null)
                        {
                            continue;
                        }
                        allViews.Add(view);
                    }
                }
            }
            return allViews;
        }
        public RelayCommand<CableTrayEntity> ApplySetupCommand { get; set; }
        public ObservableCollection<CableTrayEntity> GetEntity()
        {
            ObservableCollection<CableTrayEntity> entitys = new ObservableCollection<CableTrayEntity>();
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
            List<CableTrayType> cableSystemTypes = elements.OfType<CableTrayType>().ToList();
            //// 使用 OfType 直接过滤并转换类型转换为 List
            List<CableTrayEntity> cableSystems = cableSystemTypes
                .Select(cableSystemType => new CableTrayEntity(cableSystemType))
                .Where(e => e.IsWithFitting == true)
                .ToList();
            foreach (var item in cableSystems)
            {
                entitys.Add(item);
            }
            return entitys;
        }
        public BaseBindingCommand QueryELementCommand { get; set; }
        public void QueryElement(object parameter)
        {
            CableSystemEntitys.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(CableTrayType));
            List<CableTrayType> cableSystemTypes = elements.OfType<CableTrayType>().ToList();
            List<CableTrayEntity> cableSystems = cableSystemTypes
                .Select(cableSystemType => new CableTrayEntity(cableSystemType))
                .Where(e => e.IsWithFitting == true)
                .Where(e => string.IsNullOrEmpty(Keyword)
                || e.SystemName.Contains(Keyword)
                || e.Abbreviation.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            foreach (var item in cableSystems)
            {
                CableSystemEntitys.Add(item);
                CableSystemNames.Add(item.SystemName);
            }
        }
        public List<string> CableSystemNames { get; set; } = new List<string>();
        private ObservableCollection<CableTrayEntity> cableSystemEntitys;
        public ObservableCollection<CableTrayEntity> CableSystemEntitys
        {
            get => cableSystemEntitys;
            set
            {
                cableSystemEntitys = value;
                OnPropertyChanged();
                GetSystemNames();
            }
        }
        private void GetSystemNames()
        {
            cableSystemEntitys?.Clear();
            if (CableSystemEntitys != null)
            {
                foreach (var item in CableSystemEntitys)
                {
                    CableSystemNames.Add(item.SystemName);
                }
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; }
        }
    }
}
