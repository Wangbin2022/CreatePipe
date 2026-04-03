using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CreatePipe.models
{
    public class SingleMarkModel : ObserverableObject
    {
        public SingleMarkModel(string comentStr)
        {
            ComentStr = comentStr;
        }
        public string ComentStr { get; set; } // 注释字符串    
        private string _materialName = "默认";
        // 材料名称
        public string MmaterialName { get => _materialName; set => SetProperty(ref _materialName, value); }
        // 材料id
        public ElementId MaterialId { get; set; }
        // 所有是该注释的族实例
        public List<FamilyInstance> Instances { get; set; } = new List<FamilyInstance>();
        public List<FamilyInstance> InstancesNoneMaterial { get; set; } = new List<FamilyInstance>();
        // 实例个数
        public int InstanceCount => Instances.Count;
        public int InstanceNoneMaterialCount => InstancesNoneMaterial.Count;
        private ElementId selectedMaterialId = ElementId.InvalidElementId;
        public ElementId SelectedMaterialId
        {
            get => selectedMaterialId;
            set
            {
                selectedMaterialId = value;
                OnPropertyChanged(nameof(SelectedMaterialId));
            }
        }
        // 每个组（行）独立的选中属性名
        private string _selectedMaterial;
        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set { _selectedMaterial = value; OnPropertyChanged(); }
        }
        public ObservableCollection<MaterialEntity> MaterialsInGroup { get; set; } = new ObservableCollection<MaterialEntity>();
        //// 仅包含那些没有“材质”参数的构件 (用于特殊标记或处理)
        //public List<FamilyInstance> InstancesWithoutMaterialParameter { get; set; } = new List<FamilyInstance>();
        //public int InstanceWithoutMaterialParamCount => InstancesWithoutMaterialParameter.Count;
    }
}
