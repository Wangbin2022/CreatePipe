using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;


//private bool IsValidStructuralElement(Element element)
//{
//    return element is FamilyInstance instance &&
//           (instance.StructuralType == StructuralType.Beam ||
//            instance.StructuralType == StructuralType.Brace ||
//            instance.StructuralType == StructuralType.Column);
//}

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// MaterialPropertiesView.xaml 的交互逻辑
    /// </summary>
    public partial class MaterialPropertiesView : Window
    {

        public MaterialPropertiesView(MaterialPropertiesData dataBuffer)
        {
            InitializeComponent();
            var viewModel = new MaterialPropertiesViewModel(dataBuffer);
            viewModel.CloseWindow = () => this.Close();  // 设置关闭回调
            DataContext = viewModel;
        }
    }
    /// <summary>
    /// 材料属性表单的ViewModel - 实现INotifyPropertyChanged接口支持数据绑定
    /// </summary>
    public class MaterialPropertiesViewModel : ObserverableObject
    {
        #region 字段和属性
        private readonly MaterialPropertiesData _dataBuffer;
        private ObservableCollection<string> _materialTypes;
        private string _selectedMaterialType;
        private ObservableCollection<MaterialInfo> _subMaterials;
        private MaterialInfo _selectedSubMaterial;
        private ObservableCollection<MaterialParameter> _parameters;
        private bool _isSubTypeEnabled;
        private bool _isApplyEnabled;
        private bool _isChangeWeightEnabled;

        public event PropertyChangedEventHandler PropertyChanged;

        // 数据绑定属性
        public ObservableCollection<string> MaterialTypes
        {
            get => _materialTypes;
            set { _materialTypes = value; OnPropertyChanged(); }
        }

        public string SelectedMaterialType
        {
            get => _selectedMaterialType;
            set { _selectedMaterialType = value; OnMaterialTypeChanged(); OnPropertyChanged(); }
        }

        public ObservableCollection<MaterialInfo> SubMaterials
        {
            get => _subMaterials;
            set { _subMaterials = value; OnPropertyChanged(); }
        }

        public MaterialInfo SelectedSubMaterial
        {
            get => _selectedSubMaterial;
            set { _selectedSubMaterial = value; OnSubMaterialChanged(); OnPropertyChanged(); }
        }

        public ObservableCollection<MaterialParameter> Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        public bool IsSubTypeEnabled
        {
            get => _isSubTypeEnabled;
            set { _isSubTypeEnabled = value; OnPropertyChanged(); }
        }

        public bool IsApplyEnabled
        {
            get => _isApplyEnabled;
            set { _isApplyEnabled = value; OnPropertyChanged(); }
        }

        public bool IsChangeWeightEnabled
        {
            get => _isChangeWeightEnabled;
            set { _isChangeWeightEnabled = value; OnPropertyChanged(); }
        }

        // 命令
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand ChangeUnitWeightCommand { get; }
        #endregion

        #region 构造函数
        public MaterialPropertiesViewModel(MaterialPropertiesData dataBuffer)
        {
            _dataBuffer = dataBuffer ?? throw new ArgumentNullException(nameof(dataBuffer));

            // 初始化命令 - 使用Lambda表达式简化命令定义
            OkCommand = new BaseBindingCommand(_ => OkButtonClick());
            CancelCommand = new BaseBindingCommand(_ => CancelButtonClick());
            ApplyCommand = new BaseBindingCommand(_ => ApplyButtonClick(), _ => IsApplyEnabled);
            ChangeUnitWeightCommand = new BaseBindingCommand(_ => ChangeUnitWeightClick(), _ => IsChangeWeightEnabled);

            LoadCurrentMaterial();
        }
        #endregion

        #region 方法
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 加载当前选中构件的材料信息
        /// </summary>
        private void LoadCurrentMaterial()
        {
            // 加载材料类型列表
            //MaterialTypes = new ObservableCollection<string>(_dataBuffer.MaterialTypes);
            MaterialTypes = new ObservableCollection<string>();
            foreach (var item in _dataBuffer.MaterialTypes)
            {
                MaterialTypes.Add(item as string);
            }
            // 设置当前选中的材料类型
            SelectedMaterialType = MaterialTypes[(int)_dataBuffer.CurrentType];

            // 如果有当前材料且类型有效，加载子材料列表
            if (_dataBuffer.CurrentMaterial != null &&
                (_dataBuffer.CurrentType == StructuralAssetClass.Metal ||
                 _dataBuffer.CurrentType == StructuralAssetClass.Concrete))
            {
                LoadSubMaterials();
            }
        }

        /// <summary>
        /// 加载子材料列表（钢或混凝土）
        /// </summary>
        private void LoadSubMaterials()
        {
            var collection = _dataBuffer.CurrentType == StructuralAssetClass.Metal
                ? _dataBuffer.SteelCollection
                : _dataBuffer.ConcreteCollection;

            SubMaterials = new ObservableCollection<MaterialInfo>(
                collection.Cast<dynamic>().Select(m => new MaterialInfo
                {
                    Name = m.MaterialName,
                    Material = m.Material
                }));

            // 选中当前材料
            var currentMaterial = _dataBuffer.CurrentMaterial as dynamic;
            SelectedSubMaterial = SubMaterials.FirstOrDefault(m => m.Material.Id == currentMaterial?.Id);

            LoadParameters();
        }

        /// <summary>
        /// 加载材料参数到DataGrid
        /// </summary>
        private void LoadParameters()
        {
            if (SelectedSubMaterial?.Material == null)
            {
                Parameters = new ObservableCollection<MaterialParameter>();
                return;
            }

            var dataTable = _dataBuffer.GetParameterTable(
                SelectedSubMaterial.Material,
                (StructuralAssetClass)Enum.Parse(typeof(StructuralAssetClass), SelectedMaterialType));

            Parameters = new ObservableCollection<MaterialParameter>();
            foreach (DataRow row in dataTable.Rows)
            {
                Parameters.Add(new MaterialParameter
                {
                    ParameterName = row["Parameter"].ToString(),
                    Value = row["Value"].ToString()
                });
            }
        }

        /// <summary>
        /// 材料类型改变时的处理
        /// </summary>
        private void OnMaterialTypeChanged()
        {
            var selectedType = (StructuralAssetClass)Enum.Parse(typeof(StructuralAssetClass), SelectedMaterialType);

            if (selectedType == StructuralAssetClass.Metal || selectedType == StructuralAssetClass.Concrete)
            {
                IsApplyEnabled = true;
                IsChangeWeightEnabled = true;
                IsSubTypeEnabled = true;

                // 根据类型获取对应的材料集合
                var collection = selectedType == StructuralAssetClass.Metal
                    ? _dataBuffer.SteelCollection
                    : _dataBuffer.ConcreteCollection;

                SubMaterials = new ObservableCollection<MaterialInfo>(
                    collection.Cast<dynamic>().Select(m => new MaterialInfo
                    {
                        Name = m.MaterialName,
                        Material = m.Material
                    }));

                if (SubMaterials.Any())
                {
                    SelectedSubMaterial = SubMaterials.First();
                }
            }
            else
            {
                IsApplyEnabled = false;
                IsChangeWeightEnabled = false;
                IsSubTypeEnabled = false;
                SubMaterials = new ObservableCollection<MaterialInfo>();
                Parameters = new ObservableCollection<MaterialParameter>();
            }
        }

        /// <summary>
        /// 子材料改变时的处理
        /// </summary>
        private void OnSubMaterialChanged()
        {
            if (SelectedSubMaterial?.Material != null)
            {
                _dataBuffer.UpdateMaterial(SelectedSubMaterial.Material);
                LoadParameters();
            }
        }

        /// <summary>
        /// 确定按钮点击 - 应用并关闭
        /// </summary>
        private void OkButtonClick()
        {
            ApplyButtonClick();
            CloseWindow();
        }

        /// <summary>
        /// 取消按钮点击 - 直接关闭
        /// </summary>
        private void CancelButtonClick()
        {
            CloseWindow();
        }

        /// <summary>
        /// 应用按钮点击 - 应用材料变更但不关闭
        /// </summary>
        private void ApplyButtonClick()
        {
            if (SelectedSubMaterial?.Material != null)
            {
                _dataBuffer.UpdateMaterial(SelectedSubMaterial.Material);
                _dataBuffer.SetMaterial();
            }
        }

        /// <summary>
        /// 修改单位重量按钮点击
        /// </summary>
        private void ChangeUnitWeightClick()
        {
            var result = _dataBuffer.ChangeUnitWeight();
            // 刷新显示
            LoadParameters();
        }

        /// <summary>
        /// 关闭窗口的委托 - 由View设置
        /// </summary>
        public Action CloseWindow { get; set; }
        #endregion
    }
    /// <summary>
    /// 材料信息模型
    /// </summary>
    public class MaterialInfo
    {
        public string Name { get; set; }
        public dynamic Material { get; set; }
        public StructuralAssetClass MaterialType { get; set; }
    }

    /// <summary>
    /// 材料属性参数模型
    /// </summary>
    public class MaterialParameter
    {
        public string ParameterName { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 材料类型枚举
    /// </summary>
    public enum StructuralAssetClass
    {
        Undefined = 0,
        Basic = 1,
        Generic = 2,
        Metal = 3,
        Concrete = 4,
        Wood = 5,
        Liquid = 6,
        Gas = 7,
        Plastic = 8
    }
    /// <summary>
    /// 材料属性数据服务类 - 负责与Revit API交互
    /// 使用C# 7.3特性：默认字面量、表达式体成员、out变量等
    /// </summary>
    public class MaterialPropertiesData
    {
        #region 常量定义
        private const double ToMetricUnitWeight = 0.010764;      // 内部单位到公制单位的转换系数
        private const double ToMetricStress = 0.334554;         // 应力转换系数
        private const double ToImperialUnitWeight = 6.365827;    // 内部单位到英制单位的转换系数
        private const double ChangedUnitWeight = 14.5;           // 目标单位重量值 (kN/m³)
        #endregion

        #region 私有字段
        private readonly UIApplication _revit;
        private readonly Hashtable _allMaterialMap = new Hashtable();
        private FamilyInstance _selectedComponent;
        private Parameter _currentMaterialParameter;
        private Material _cachedMaterial;
        private readonly ArrayList _steels = new ArrayList();
        private readonly ArrayList _concretes = new ArrayList();
        #endregion

        #region 公开属性
        public StructuralAssetClass CurrentType => GetCurrentMaterialType();
        public object CurrentMaterial => _cachedMaterial = GetCurrentMaterial();
        public ArrayList SteelCollection => _steels;
        public ArrayList ConcreteCollection => _concretes;

        public ArrayList MaterialTypes => new ArrayList
        {
            "Undefined", "Basic", "Generic", "Metal",
            "Concrete", "Wood", "Liquid", "Gas", "Plastic"
        };
        #endregion

        #region 构造函数
        public MaterialPropertiesData(UIApplication revit, ElementId selectedElementId)
        {
            _revit = revit ?? throw new ArgumentNullException(nameof(revit));
            Initialize(selectedElementId);
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化数据 - 使用C# 7.0的out变量和表达式体
        /// </summary>
        private void Initialize(ElementId selectedElementId)
        {
            GetSelectedComponent(selectedElementId);
            GetAllMaterials();
        }

        /// <summary>
        /// 获取选中的结构构件
        /// </summary>
        private void GetSelectedComponent(ElementId elementId)
        {
            var component = _revit.ActiveUIDocument.Document.GetElement(elementId) as FamilyInstance;

            if (component == null) return;

            // 检查是否为结构构件（梁、柱、支撑）
            if (component.StructuralType == StructuralType.Beam ||
                component.StructuralType == StructuralType.Brace ||
                component.StructuralType == StructuralType.Column)
            {
                _selectedComponent = component;

                // 查找结构材料参数 - 使用Lambda表达式简化查询
                _currentMaterialParameter = component.Parameters
                    .Cast<Parameter>()
                    .FirstOrDefault(p => p.Definition.Name == "Structural Material");
            }
        }

        /// <summary>
        /// 获取文档中所有材料
        /// </summary>
        private void GetAllMaterials()
        {
            var doc = _revit.ActiveUIDocument.Document;
            var materials = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>();

            foreach (var material in materials)
            {
                var materialType = GetMaterialType(material);

                // 使用switch表达式（C# 8.0特性，如果环境不支持可改用switch语句）
                switch (materialType)
                {
                    case StructuralAssetClass.Metal:
                        _steels.Add(new MaterialMap(material));
                        break;
                    case StructuralAssetClass.Concrete:
                        _concretes.Add(new MaterialMap(material));
                        break;
                }

                _allMaterialMap[material.Id.IntegerValue] = material;
            }
        }

        /// <summary>
        /// 获取当前构件的材料
        /// </summary>
        private Material GetCurrentMaterial()
        {
            var idValue = _currentMaterialParameter?.AsElementId().IntegerValue ?? 0;
            return idValue > 0 ? _allMaterialMap[idValue] as Material : null;
        }

        /// <summary>
        /// 获取当前材料类型
        /// </summary>
        private StructuralAssetClass GetCurrentMaterialType()
        {
            var material = CurrentMaterial as Material;
            return material?.Id.IntegerValue > 0
                ? GetMaterialType(material)
                : StructuralAssetClass.Generic;
        }

        /// <summary>
        /// 获取指定材料的类型 - 使用C# 7.0的out变量和内联条件
        /// </summary>
        private StructuralAssetClass GetMaterialType(Material material)
        {
            // 使用条件运算符简化代码
            return material.StructuralAssetId != ElementId.InvalidElementId
                ? GetMaterialTypeFromPropertySet(material)
                : StructuralAssetClass.Undefined;
        }

        private StructuralAssetClass GetMaterialTypeFromPropertySet(Material material)
        {
            var propElem = _revit.ActiveUIDocument.Document.GetElement(material.StructuralAssetId) as PropertySetElement;
            var propElemPara = propElem?.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CLASS);

            return propElemPara != null
                ? (StructuralAssetClass)propElemPara.AsInteger()
                : StructuralAssetClass.Undefined;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取材料的参数表格
        /// </summary>
        public DataTable GetParameterTable(object materialObj, StructuralAssetClass substanceKind)
        {
            var parameterTable = CreateTable();

            if (!(materialObj is Material material)) return parameterTable;

            // 使用元组简化参数处理（C# 7.0）
            var parameters = new (BuiltInParameter param, string displayName)[]
            {
                (BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD1, "Young's Modulus X"),
                (BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD1, "Poisson's Ratio X"),
                (BuiltInParameter.PHY_MATERIAL_PARAM_UNIT_WEIGHT, "Unit Weight"),
                //(BuiltInParameter.PHY_MATERIAL_PARAM_DAMPING_RATIO, "Damping Ratio"),
            };

            foreach (var (param, displayName) in parameters)
            {
                var tempParam = material.get_Parameter(param);
                if (tempParam != null)
                {
                    AddDataRow(displayName, tempParam.AsValueString(), parameterTable);
                }
            }

            // 添加材料类型特定参数
            AddTypeSpecificParameters(material, substanceKind, parameterTable);

            return parameterTable;
        }

        /// <summary>
        /// 添加材料类型特定参数
        /// </summary>
        private void AddTypeSpecificParameters(Material material, StructuralAssetClass substanceKind, DataTable table)
        {
            if (substanceKind == StructuralAssetClass.Metal)
            {
                AddParameterIfExists(material, BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS, "Minimum Yield Stress", table);
                AddParameterIfExists(material, BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH, "Minimum Tensile Strength", table);
            }
            else if (substanceKind == StructuralAssetClass.Concrete)
            {
                AddParameterIfExists(material, BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION, "Concrete Compression", table);
                AddParameterIfExists(material, BuiltInParameter.PHY_MATERIAL_PARAM_LIGHT_WEIGHT, "Lightweight", table);
            }
        }

        private void AddParameterIfExists(Material material, BuiltInParameter param, string displayName, DataTable table)
        {
            var parameter = material.get_Parameter(param);
            if (parameter != null)
            {
                AddDataRow(displayName, parameter.AsValueString(), table);
            }
        }

        /// <summary>
        /// 更新缓存的材料
        /// </summary>
        public void UpdateMaterial(object material) => _cachedMaterial = material as Material;

        /// <summary>
        /// 设置构件的材料
        /// </summary>
        public void SetMaterial()
        {
            if (_cachedMaterial == null || _currentMaterialParameter == null) return;
            _currentMaterialParameter.Set(_cachedMaterial.Id);
        }

        /// <summary>
        /// 修改单位重量
        /// </summary>
        public bool ChangeUnitWeight()
        {
            var material = GetCurrentMaterial();
            if (material == null) return false;

            var weightPara = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_UNIT_WEIGHT);
            weightPara?.Set(ChangedUnitWeight / ToMetricUnitWeight);

            return true;
        }
        #endregion

        #region 辅助方法
        private DataTable CreateTable()
        {
            var table = new DataTable("ParameterTable");
            table.Columns.Add("Parameter", typeof(string));
            table.Columns.Add("Value", typeof(string));
            return table;
        }

        private void AddDataRow(string parameterName, string parameterValue, DataTable table)
        {
            var row = table.NewRow();
            row["Parameter"] = parameterName;
            row["Value"] = parameterValue;
            table.Rows.Add(row);
        }
        #endregion
    }

    /// <summary>
    /// 材料映射辅助类
    /// </summary>
    public class MaterialMap
    {
        public string MaterialName { get; }
        public Material Material { get; }

        public MaterialMap(Material material)
        {
            MaterialName = material.Name;
            Material = material;
        }
    }
}
