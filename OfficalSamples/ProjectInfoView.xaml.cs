using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ProjectInfoView.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectInfoView : Window
    {
        public ProjectInfoView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 项目信息主ViewModel - 管理PropertyGrid和事务
    /// </summary>
    public class ProjectInfoViewModel : ObserverableObject
    {
        private readonly Document _document;
        private readonly ProjectInfoWrapper _projectInfoWrapper;
        private bool _isDirty;

        public ProjectInfoWrapper ProjectInfo => _projectInfoWrapper;

        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> CloseRequested; // true=保存, false=取消

        public ProjectInfoViewModel(Document document)
        {
            _document = document;
            var projectInfo = document.ProjectInformation;
            _projectInfoWrapper = new ProjectInfoWrapper(projectInfo);

            OkCommand = new BaseBindingCommand(_ => OnOk());
            CancelCommand = new BaseBindingCommand(_ => OnCancel());

            // 监听属性变化
            _projectInfoWrapper.PropertyChanged += (s, e) => IsDirty = true;
        }

        private void OnOk()
        {
            if (IsDirty)
            {
                // 事务在命令层处理
                CloseRequested?.Invoke(true);
            }
            else
            {
                CloseRequested?.Invoke(false);
            }
        }

        private void OnCancel() => CloseRequested?.Invoke(false);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    /// <summary>
    /// 全局信息存储类 - 使用C# 7.3语法简化字典初始化
    /// </summary>
    public static class RevitStartInfo
    {
        #region 运行时属性
        public static Application RevitApp { get; set; }
        public static Document RevitDoc { get; set; }
        public static ProductType RevitProduct { get; set; }
        #endregion

        #region 静态数据字典 - 使用表达式体和初始化器
        public static ReadOnlyCollection<string> TimeZones { get; } = new List<string>
        {
            "(GMT-12:00) International Date Line West",
            "(GMT-11:00) Midway Island, Samoa",
            "(GMT-10:00) Hawaii",
            "(GMT-09:00) Alaska",
            "(GMT-08:00) Pacific Time (US/Canada)",
            "(GMT-08:00) Tijuana, Baja California",
            "(GMT-07:00) Arizona",
            "(GMT-07:00) Mountain Time (US/Canada)",
            "(GMT-06:00) Central Time (US/Canada)",
            "(GMT-05:00) Eastern Time (US/Canada)",
            "(GMT+00:00) Greenwich Mean Time",
            "(GMT+01:00) Central European Time",
            "(GMT+02:00) Eastern European Time",
            "(GMT+03:00) Moscow Time",
            "(GMT+04:00) Dubai Time",
            "(GMT+05:00) Pakistan Time",
            "(GMT+05:30) India Time",
            "(GMT+06:00) Bangladesh Time",
            "(GMT+07:00) Indochina Time",
            "(GMT+08:00) China Time",
            "(GMT+09:00) Japan Time",
            "(GMT+10:00) Australia Eastern Time",
            "(GMT+11:00) Solomon Is. Time",
            "(GMT+12:00) New Zealand Time"
        }.AsReadOnly();

        public static IReadOnlyDictionary<gbXMLBuildingType, string> BuildingTypeMap { get; } =
            new Dictionary<gbXMLBuildingType, string>
            {
                [gbXMLBuildingType.Office] = "Office",
                [gbXMLBuildingType.Retail] = "Retail",
                [gbXMLBuildingType.SchoolOrUniversity] = "School or University",
                [gbXMLBuildingType.HospitalOrHealthcare] = "Hospital or Healthcare",
                [gbXMLBuildingType.Hotel] = "Hotel",
                [gbXMLBuildingType.MultiFamily] = "Multi Family",
                [gbXMLBuildingType.SingleFamily] = "Single Family",
                [gbXMLBuildingType.Warehouse] = "Warehouse",
                [gbXMLBuildingType.Manufacturing] = "Manufacturing",
                [gbXMLBuildingType.ParkingGarage] = "Parking Garage",
                [gbXMLBuildingType.ReligiousBuilding] = "Religious Building",
                [gbXMLBuildingType.Library] = "Library",
                [gbXMLBuildingType.Museum] = "Museum",
                [gbXMLBuildingType.Courthouse] = "Courthouse",
                [gbXMLBuildingType.ConventionCenter] = "Convention Center",
                [gbXMLBuildingType.PerformingArtsTheater] = "Performing Arts Theater",
                [gbXMLBuildingType.SportsArena] = "Sports Arena",
                [gbXMLBuildingType.Transportation] = "Transportation"
            };

        public static IReadOnlyDictionary<gbXMLServiceType, string> ServiceTypeMap { get; } =
            new Dictionary<gbXMLServiceType, string>
            {
                [gbXMLServiceType.VAVSingleDuct] = "VAV - Single Duct",
                [gbXMLServiceType.VAVTerminalReheat] = "VAV - Terminal Reheat",
                [gbXMLServiceType.ConstantVolumeFixedOA] = "Constant Volume - Fixed OA",
                [gbXMLServiceType.FanCoilSystem] = "Fan Coil System",
                [gbXMLServiceType.WaterLoopHeatPump] = "Water Loop Heat Pump",
                [gbXMLServiceType.VariableRefrigerantFlow] = "Variable Refrigerant Flow",
                [gbXMLServiceType.NoServiceType] = "None"
            };

        public static IReadOnlyDictionary<gbXMLExportComplexity, string> ExportComplexityMap { get; } =
            new Dictionary<gbXMLExportComplexity, string>
            {
                [gbXMLExportComplexity.Simple] = "Simple",
                [gbXMLExportComplexity.Complex] = "Complex",
                [gbXMLExportComplexity.SimpleWithShadingSurfaces] = "Simple With Shading Surfaces",
                [gbXMLExportComplexity.ComplexWithShadingSurfaces] = "Complex With Shading Surfaces",
                [gbXMLExportComplexity.ComplexWithMullionsAndShadingSurfaces] = "Complex With Mullions And Shading Surfaces"
            };

        public static IReadOnlyDictionary<HVACLoadLoadsReportType, string> HVACLoadLoadsReportTypeMap { get; } =
            new Dictionary<HVACLoadLoadsReportType, string>
            {
                [HVACLoadLoadsReportType.NoReport] = "No",
                [HVACLoadLoadsReportType.SimpleReport] = "Simple",
                [HVACLoadLoadsReportType.StandardReport] = "Standard",
                [HVACLoadLoadsReportType.DetailedReport] = "Detailed"
            };

        public static IReadOnlyDictionary<HVACLoadConstructionClass, string> HVACLoadConstructionClassMap { get; } =
            new Dictionary<HVACLoadConstructionClass, string>
            {
                [HVACLoadConstructionClass.NoneConstruction] = "None",
                [HVACLoadConstructionClass.LooseConstruction] = "Loose",
                [HVACLoadConstructionClass.MediumConstruction] = "Medium",
                [HVACLoadConstructionClass.TightConstruction] = "Tight"
            };
        #endregion

        #region 辅助方法
        public static Element GetElement(ElementId elementId) => RevitDoc?.GetElement(elementId);
        public static Element GetElement(int elementId) => GetElement(new ElementId(elementId));

        /// <summary>
        /// 获取枚举的显示名称 - 使用switch表达式
        /// </summary>
        public static string GetEnumDisplayName(object enumValue)
        {
            //if (enumValue is gbXMLBuildingType)
            //{
            //    var bt = (gbXMLBuildingType)enumValue;
            //    return BuildingTypeMap.GetValueOrDefault(bt,bt.ToString());
            //}
            //else if (enumValue is gbXMLServiceType)
            //{
            //    var st = (gbXMLServiceType)enumValue;
            //    return ServiceTypeMap.GetValueOrDefault(st, st.ToString());
            //}
            //else if (enumValue is gbXMLExportComplexity)
            //{
            //    var ec = (gbXMLExportComplexity)enumValue;
            //    return ExportComplexityMap.GetValueOrDefault(ec, ec.ToString());
            //}
            //else if (enumValue is HVACLoadLoadsReportType)
            //{
            //    var rt = (HVACLoadLoadsReportType)enumValue;
            //    return HVACLoadLoadsReportTypeMap.GetValueOrDefault(rt, rt.ToString());
            //}
            //else if (enumValue is HVACLoadConstructionClass)
            //{
            //    var cc = (HVACLoadConstructionClass)enumValue;
            //    return HVACLoadConstructionClassMap.GetValueOrDefault(cc, cc.ToString());
            //}
            //else
            //{
            //    return enumValue?.ToString() ?? "未知";
            //}
            return enumValue?.ToString() ?? "未知";
        }
        #endregion
    }
    /// <summary>
    /// 项目信息包装器 - 用于PropertyGrid显示和编辑
    /// 使用C# 7.3语法：表达式体成员、nameof
    /// </summary>
    public class ProjectInfoWrapper : INotifyPropertyChanged
    {
        private readonly ProjectInfo _projectInfo;

        public ProjectInfoWrapper(ProjectInfo projectInfo)
        {
            _projectInfo = projectInfo ?? throw new ArgumentNullException(nameof(projectInfo));
        }

        #region 实例属性 - 使用表达式体和特性
        [Category("项目标识")]
        [DisplayName("项目编号")]
        [Description("项目的唯一标识编号")]
        public string Number
        {
            get => _projectInfo.Number;
            set { _projectInfo.Number = value; OnPropertyChanged(); }
        }

        [Category("项目标识")]
        [DisplayName("项目名称")]
        [Description("项目的名称")]
        public string Name
        {
            get => _projectInfo.Name;
            set { _projectInfo.Name = value; OnPropertyChanged(); }
        }

        [Category("项目地址")]
        [DisplayName("地址")]
        [Description("项目地址")]
        public string Address
        {
            get => _projectInfo.Address;
            set { _projectInfo.Address = value; OnPropertyChanged(); }
        }

        [Category("项目信息")]
        [DisplayName("作者")]
        [Description("项目作者")]
        public string Author
        {
            get => _projectInfo.Author;
            set { _projectInfo.Author = value; OnPropertyChanged(); }
        }

        [Category("项目信息")]
        [DisplayName("发布日期")]
        [Description("项目发布日期")]
        public string IssueDate
        {
            get => _projectInfo.IssueDate;
            set { _projectInfo.IssueDate = value; OnPropertyChanged(); }
        }

        [Category("项目信息")]
        [DisplayName("状态")]
        [Description("项目状态")]
        public string Status
        {
            get => _projectInfo.Status;
            set { _projectInfo.Status = value; OnPropertyChanged(); }
        }

        //[Category("能耗分析")]
        //[DisplayName("建筑类型")]
        //[Description("gbXML导出的建筑类型")]
        //[TypeConverter(typeof(EnumDescriptionConverter<gbXMLBuildingType>))]
        //public gbXMLBuildingType BuildingType
        //{
        //    get => _projectInfo.BuildingType;
        //    set { _projectInfo.BuildingType = value; OnPropertyChanged(); }
        //}
        //[Category("能耗分析")]
        //[DisplayName("服务类型")]
        //[Description("HVAC服务类型")]
        //[TypeConverter(typeof(EnumDescriptionConverter<gbXMLServiceType>))]
        //public gbXMLServiceType ServiceType
        //{
        //    get => _projectInfo.ServiceType;
        //    set { _projectInfo.ServiceType = value; OnPropertyChanged(); }
        //}
        //[Category("能耗分析")]
        //[DisplayName("导出复杂度")]
        //[Description("gbXML导出几何复杂度")]
        //[TypeConverter(typeof(EnumDescriptionConverter<gbXMLExportComplexity>))]
        //public gbXMLExportComplexity ExportComplexity
        //{
        //    get => _projectInfo.ExportComplexity;
        //    set { _projectInfo.ExportComplexity = value; OnPropertyChanged(); }
        //}
        //[Category("能耗分析")]
        //[DisplayName("负荷报告类型")]
        //[Description("HVAC负荷报告类型")]
        //[TypeConverter(typeof(EnumDescriptionConverter<HVACLoadLoadsReportType>))]
        //public HVACLoadLoadsReportType LoadsReportType
        //{
        //    get => _projectInfo.LoadsReportType;
        //    set { _projectInfo.LoadsReportType = value; OnPropertyChanged(); }
        //}
        //[Category("能耗分析")]
        //[DisplayName("建筑构造等级")]
        //[Description("建筑构造紧密等级")]
        //[TypeConverter(typeof(EnumDescriptionConverter<HVACLoadConstructionClass>))]
        //public HVACLoadConstructionClass ConstructionClass
        //{
        //    get => _projectInfo.ConstructionClass;
        //    set { _projectInfo.ConstructionClass = value; OnPropertyChanged(); }
        //}
        //[Category("地理位置")]
        //[DisplayName("时区")]
        //[Description("项目所在时区")]
        //public string TimeZone
        //{
        //    get => _projectInfo.TimeZone;
        //    set { _projectInfo.TimeZone = value; OnPropertyChanged(); }
        //}
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 枚举描述转换器 - 用于PropertyGrid显示友好名称
    /// </summary>
    public class EnumDescriptionConverter<T> : EnumConverter where T : Enum
    {
        public EnumDescriptionConverter() : base(typeof(T)) { }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value != null)
            {
                return RevitStartInfo.GetEnumDisplayName(value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
