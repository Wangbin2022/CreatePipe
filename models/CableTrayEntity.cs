using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class CableTrayEntity : ObserverableObject
    {
        public CableTrayType cableTrayType { get; set; }
        public Document Document { get => cableTrayType.Document; }
        public CableTrayEntity(CableTrayType cableTray)
        {
            cableTrayType = cableTray;
            id = cableTray.Id;
            systemName = cableTray.Name;
            IsWithFitting = cableTray.IsWithFitting;
            linePatternElementInfos = LinePatterns();
            systemCategory = GetSystemCategory(cableTray);
            abbreviation = GetAbbreviation(cableTray);
            singleSystemElementCount = GetElementCount(cableTray);
            filters = GetFilters();
            foreach (var item in filters)
            {
                FilterNames.Add(item.Name);
            }
            SelectedLineWeight = LineWeights.FirstOrDefault();
            //LinePatternElem=LinePatternElementInfos.FirstOrDefault();
        }


        public ElementId id { get; set; }
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

        private Autodesk.Revit.DB.Color lineColor;
        public Autodesk.Revit.DB.Color LineColor
        {
            get => lineColor;
            set
            {
                lineColor = value;
                OnPropertyChanged("LineColor");
                ColorName = GetColorValue(LineColor);
            }
        }

        private string colorName = "未指定";
        public string ColorName
        {
            get => colorName;
            set
            {
                if (colorName != value)
                {
                    colorName = value;
                    OnPropertyChanged(nameof(ColorName));
                }
            }
        }
        private List<LinePatternElement> LinePatterns()
        {
            FilteredElementCollector elements3 = new FilteredElementCollector(Document);
            List<LinePatternElement> LinePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();
            return LinePatternElements;
        }
        public LinePatternElement LinePatternElem { get; set; }
        private List<LinePatternElement> linePatternElementInfos;
        public List<LinePatternElement> LinePatternElementInfos
        {
            get => linePatternElementInfos;
            set => linePatternElementInfos = value;
        }
        private List<int> _lineWeights;
        public List<int> LineWeights
        {
            get
            {
                if (_lineWeights == null)
                {
                    _lineWeights = new List<int>();
                    for (int i = 1; i <= 16; i++)
                    {
                        _lineWeights.Add(i);
                    }
                }
                return _lineWeights;
            }
            set { _lineWeights = value; }
        }
        private int _selectedLineWeight;
        public int SelectedLineWeight
        {
            get { return _selectedLineWeight; }
            set
            {
                if (_selectedLineWeight != value)
                {
                    _selectedLineWeight = value;
                    OnPropertyChanged(nameof(SelectedLineWeight));
                }
            }
        }
        public string GetColorValue(Color color)
        {
            return color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
        }
        public string FilterName { get; set; }
        public List<string> FilterNames { get; set; } = new List<string>();
        public List<ParameterFilterElement> filters = new List<ParameterFilterElement>();
        private List<ParameterFilterElement> Filters
        {
            get => filters;
            set
            {
                filters = value;
                OnPropertyChanged(nameof(Filters));
            }
        }
        private List<ParameterFilterElement> GetFilters()
        {
            FilteredElementCollector elements = new FilteredElementCollector(Document).OfClass(typeof(ParameterFilterElement));
            List<ParameterFilterElement> pfe = elements.OfType<ParameterFilterElement>().ToList();
            return pfe;
        }
        public int GetElementCount(CableTrayType cableTray)
        {
            string typeAbb = null;
            FilteredElementCollector collector = new FilteredElementCollector(Document);
            List<CableTray> cableTrays = collector.OfClass(typeof(CableTray))
               .Cast<CableTray>().ToList();
            FilteredElementCollector collector1 = new FilteredElementCollector(Document);
            List<FamilyInstance> cableTrayFittings = collector1
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_CableTrayFitting)
                .Cast<FamilyInstance>().ToList();
            //List<Element> elements = new List<Element>();
            HashSet<ElementId> addedIds = new HashSet<ElementId>();
            foreach (CableTray item in cableTrays)
            {
                if (item.Name == cableTray.Name)
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
            return addedIds.Count();
        }
        private int singleSystemElementCount;
        public int SingleSystemElementCount { get => singleSystemElementCount; set => singleSystemElementCount = value; }
        public bool IsWithFitting;
        public string GetAbbreviation(CableTrayType cableTrayType)
        {
            FilteredElementCollector collector = new FilteredElementCollector(Document);
            List<CableTray> cableTrays = collector.OfClass(typeof(CableTray))
                .Cast<CableTray>().ToList();

            List<string> abbs = new List<string>();
            foreach (CableTray item in cableTrays)
            {
                if (item.Name == SystemName)
                {
                    abbs.Add(item.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsString());
                }
            }
            if (abbs == null || abbs.Count == 0)
            {
                return "";
            }
            // 使用Distinct()方法去除重复项，然后转换为数组
            string[] distinctValues = abbs.Distinct().ToArray();
            // 如果去重后的数组长度为1，说明所有值一致
            if (distinctValues.Length == 1)
            {
                return distinctValues[0]; // 返回那个一致的值
            }
            else
            {
                return "多值"; // 返回不一致的消息
            }
        }
        private void ModifyAbbreviation(string newValue)
        {
            List<CableTray> allCableTrays = new FilteredElementCollector(Document).OfClass(typeof(CableTray)).Cast<CableTray>().ToList();
            List<FamilyInstance> allCableTrayFittings = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_CableTrayFitting).Cast<FamilyInstance>().ToList();
            List<CableTray> selectCableTrays = new List<CableTray>();
            foreach (var item in allCableTrays)
            {
                if (item.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == cableTrayType.Name)
                {
                    selectCableTrays.Add(item);
                }
            }
            //string paraName = selectCableTrays.FirstOrDefault().get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString();
            //UniversalNewString subView = new UniversalNewString("请输入要桥架类型名称", paraName);
            //if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            //{
            //    TaskDialog.Show("tt", "输入属性遇到错误，请重试");
            //    return Result.Cancelled;
            //}
            //paraName = vm.NewName;
            foreach (var item in selectCableTrays)
            {
                item.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).Set(newValue);
            }
            foreach (var instance in allCableTrayFittings)
            {
                ConnectorSet cons = instance.MEPModel.ConnectorManager.Connectors;
                foreach (Connector item in cons)
                {
                    if (!item.IsConnected)
                    {
                        continue;
                    }
                    else
                    {
                        // 获取连接的所有构件
                        ConnectorSet connectedCons = item.AllRefs;
                        foreach (Connector connectedCon in connectedCons)
                        {
                            // 排除自身连接器
                            if (connectedCon == item) continue;

                            // 判断连接对象类型
                            if (connectedCon.Owner is FamilyInstance connectedFamilyInstance)
                            {
                                continue;
                            }
                            else if (connectedCon.Owner is CableTray mepCurve && mepCurve.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString() == newValue)
                            {
                                // 直接连接到桥架的情况
                                instance.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).Set(newValue);
                            }
                        }
                    }
                }
            }
            ////TaskDialog.Show("tt", item.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString());
            //TaskDialog.Show("tt", $"已修改{CableSwitch.Count.ToString()}个构件");
            //Selection select = uiApp.ActiveUIDocument.Selection;
            //select.SetElementIds(CableSwitch);
            ////FilteredElementCollector collector = new FilteredElementCollector(Document);
            ////List<CableTray> cableTrays = collector.OfClass(typeof(CableTray))
            ////    .Cast<CableTray>().ToList();
            ////FilteredElementCollector collector1 = new FilteredElementCollector(Document);
            ////List<FamilyInstance> cableTrayFittings = collector1
            ////    .OfClass(typeof(FamilyInstance))
            ////    .OfCategory(BuiltInCategory.OST_CableTrayFitting)
            ////    .Cast<FamilyInstance>().ToList();
            ////List<string> abbs = new List<string>();
            ////foreach (CableTray item in cableTrays)
            ////{
            ////    Parameter param = item.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE);
            ////    param.Set(newValue);
            ////}
            ////foreach (FamilyInstance item in cableTrayFittings)
            ////{
            ////    Parameter param = item.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE);
            ////    param.Set(newValue);
            ////}
        }
        private string GetSystemCategory(CableTrayType cableTray)
        {
            if (cableTray.Name.Contains("母线"))
            {
                return "母线";
            }
            else if (cableTray.Name.Contains("桥架"))
            {
                return "桥架";
            }
            return "未明确类型";
        }
        private void UpdateSystemCategory(String sysName)
        {
            if (sysName.Contains("母线"))
            {
                systemCategory = "母线";
            }
            else if (sysName.Contains("桥架"))
            {
                systemCategory = "桥架";
            }
            else systemCategory = "未明确类型";
        }
        private string systemCategory;
        public string SystemCategory
        {
            get => systemCategory;
            set
            {
                systemCategory = value;
                OnPropertyChanged();
            }
        }
        private string abbreviation;
        public string Abbreviation
        {
            get { return abbreviation; }
            set
            {
                abbreviation = value;
                Document.NewTransaction(() => ModifyAbbreviation(value), "修改缩写");
                OnPropertyChanged();
            }
        }
        private string systemName;
        public string SystemName
        {
            get { return systemName; }
            set
            {
                Document.NewTransaction(() => cableTrayType.Name = value, "修改名称");
                systemName = value;
                OnPropertyChanged("SystemName");
                UpdateSystemCategory(systemName);
            }
        }
    }
}


//线颜色，线宽，线型，
//过滤器名称，是否填充,填充颜色
//按钮——将过滤器添加到所有视图
////内置系统（是否带配件，检查是否最后）

//桥架没有系统，不分材质，需要查找并新建，编辑过滤器，并创建过滤器到所有视图
//默认过滤器不填充
