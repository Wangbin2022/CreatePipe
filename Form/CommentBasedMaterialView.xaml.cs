using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// CommentBasedMaterialManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class CommentBasedMaterialView : Window
    {
        Document Document;
        public CommentBasedMaterialView(UIApplication uIApplication)
        {
            InitializeComponent();
            this.DataContext = new CommentBasedMaterialViewModel(uIApplication);
            Document = uIApplication.ActiveUIDocument.Document;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MateriaManageForm materiaManageForm = new MateriaManageForm(Document);
            materiaManageForm.ShowDialog();
            this.Close();
        }
    }
    public class CommentBasedMaterialViewModel : ObserverableObject, IQueryViewModelWithDelete<SingleMarkModel>
    {
        public Document Doc { get; private set; }
        public UIDocument uiDoc { get; set; }
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        //定义一个常量来表示要使用的 BuiltInParameter
        private const BuiltInParameter CommentsParameterId = BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS;
        // 全局可用的所有材质列表 (ID 和 Name)
        public ObservableCollection<MaterialEntity> AvailableMaterials { get; } = new ObservableCollection<MaterialEntity>();
        // ViewModel 中的核心数据集合，用于 DataGrid
        private ObservableCollection<SingleMarkModel> _collection = new ObservableCollection<SingleMarkModel>();
        public ObservableCollection<SingleMarkModel> Collection { get => _collection; set => SetProperty(ref _collection, value); }
        // 所有实例中找到的材质参数名称列表 (去重排序后，供所有行选择)
        public ObservableCollection<string> MaterialAttrnames { get; set; } = new ObservableCollection<string>();
        // 缓存所有带有 Comments 参数的 FamilyInstance，避免重复查询 DB
        private List<FamilyInstance> _allCommentedInstances = new List<FamilyInstance>();
        // 排除列表的 ElementId
        //排除列表图像1152385，拆除的阶段1012101，设计选项1013201，主体ID 1002108,
        //类别图像1152384，结构顶面1013438,Level 1002062,System Type 1140333
        private readonly HashSet<ElementId> _excludeIds = new HashSet<ElementId>
        {
            new ElementId(-1152385), new ElementId(-1012101), new ElementId(-1013201), new ElementId(-1002108),
            new ElementId(-1152384), new ElementId(-1013438), new ElementId(-1002062), new ElementId(-1140333)
        };

        public CommentBasedMaterialViewModel(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            uiDoc = uIApplication.ActiveUIDocument;
            InitFunc();
        }
        // --- 接口实现: 初始化方法 ---
        public void InitFunc()
        {
            // 1. 清空并加载所有真实材质到 AvailableMaterials
            AvailableMaterials.Clear();
            List<Material> materials = new FilteredElementCollector(Doc).OfClass(typeof(Material)).Cast<Material>()
                .OrderBy(m => m.Name).ToList();
            foreach (Material mat in materials)
            {
                AvailableMaterials.Add(new MaterialEntity(mat, ExternalHandler)); // 假设 MaterialEntity 构造函数接收 Material 和 ExternalHandler
            }
            // 2. 收集所有带有 Comments 参数的 FamilyInstance
            // 这一步只执行一次，结果缓存到 _allCommentedInstances
            _allCommentedInstances = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().OfType<FamilyInstance>()
                .Where(inst => inst.get_Parameter(CommentsParameterId)?.AsString() != null).ToList();
            // 3. 从缓存的实例中提取所有可能的材质参数名称，去重排序后填充 MaterialAttrnames
            // 3. 优化参数扫描逻辑
            HashSet<ParameterEntity> uniqueParamEntities = new HashSet<ParameterEntity>();
            HashSet<InternalDefinition> scannedDefinitions = new HashSet<InternalDefinition>();
            // 限制扫描样本数量：如果实例太多，其实只需要扫描每种类型的一个代表即可
            // 这里通过 Symbol ID 进行分组，每种类型只扫一个实例
            var sampleInstances = _allCommentedInstances.GroupBy(i => i.GetTypeId()).Select(g => g.First());
            foreach (var instance in sampleInstances)
            {
                ScanParameters(instance, uniqueParamEntities, scannedDefinitions);
                if (instance.Symbol != null)
                {
                    ScanParameters(instance.Symbol, uniqueParamEntities, scannedDefinitions);
                }
            }
            MaterialAttrnames.Clear(); // 清空，防止重复添加
            var sortedUniqueNames = uniqueParamEntities.Where(item => !_excludeIds.Contains(item.Id))
                .Select(item => item.Name).Distinct() // 再次去重 (虽然 HashSet 已经保证了 ParameterEntity 唯一，但这里是针对 Name 字符串)
                .OrderBy(name => name).ToList();
            foreach (var name in sortedUniqueNames)
            {
                MaterialAttrnames.Add(name);
            }
            // 首次调用 QueryElement 来填充 Collection
            QueryElement(null);
        }
        // --- 接口实现: 基础逻辑命令 ---
        public HashSet<ParameterEntity> paramSet = new HashSet<ParameterEntity>();
        private void ScanParameters(Element elem, HashSet<ParameterEntity> set, HashSet<InternalDefinition> scannedDefinitions)
        {
            if (elem == null) return;

            // 直接遍历元素当前拥有的参数集合，这比遍历 BuiltInParameter 枚举快几千倍
            foreach (Parameter p in elem.Parameters)
            {
                // 1. 基础过滤：必须是 ElementId 类型
                if (p.StorageType != StorageType.ElementId) continue;

                // 2. 性能核心：如果该参数定义已经处理过，直接跳过
                if (p.Definition is InternalDefinition def)
                {
                    if (scannedDefinitions.Contains(def)) continue;
                    scannedDefinitions.Add(def);
                    // 3. 业务过滤：检查是否为材质参数
                    // 优先检查参数类型是否为材质（Revit 2022+ 使用 SpecTypeId，旧版使用 ParameterType）
#if REVIT2022_OR_GREATER
            bool isMaterialSpec = p.Definition.GetDataType() == SpecTypeId.Reference.Material;
#else
                    bool isMaterialSpec = p.Definition.ParameterType == ParameterType.Material;
#endif
                    if (isMaterialSpec)
                    {
                        set.Add(new ParameterEntity(p));
                    }
                    else
                    {
                        // 兜底方案：如果是 ElementId 但没明确标注为材质，检查当前值或名称
                        ElementId valId = p.AsElementId();
                        if (valId != ElementId.InvalidElementId)
                        {
                            // 仅当 ID 有效时检查一次类型
                            if (Doc.GetElement(valId) is Material)
                            {
                                set.Add(new ParameterEntity(p));
                            }
                        }
                        else if (p.Definition.Name.Contains("材质") || p.Definition.Name.Contains("Material"))
                        {
                            set.Add(new ParameterEntity(p));
                        }
                    }
                }
            }
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            Collection.Clear();
            // 使用之前缓存的 _allCommentedInstances，避免重复查询 DB
            // 如果 text 不为空，则进行筛选
            IEnumerable<FamilyInstance> filteredInstances = string.IsNullOrEmpty(text)
                ? _allCommentedInstances : _allCommentedInstances.Where(inst =>
              inst.get_Parameter(CommentsParameterId)?.AsString()?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
            // <-- 修正点: 使用 BuiltInParameter 替换字符串 "Comments" -->
            var groupedInstances = filteredInstances
                .GroupBy(inst => inst.get_Parameter(CommentsParameterId)?.AsString())
                .OrderBy(g => g.Key);
            foreach (var group in groupedInstances)
            {
                SingleMarkModel model = new SingleMarkModel(group.Key);
                model.Instances.AddRange(group); // 将分组的实例添加到模型中
                // 为每一行设置默认选中的属性名
                if (MaterialAttrnames.Any())
                {
                    model.SelectedMaterial = MaterialAttrnames.FirstOrDefault();
                }
                Collection.Add(model);
            }
        }
        // --- 接口实现: 单个删除逻辑 (清空选定属性赋予的材质，如果未选属性则全部属性id均设置为-1) ---
        public ICommand DeleteElementCommand => new RelayCommand<SingleMarkModel>(DeleteElement);
        public void DeleteElement(SingleMarkModel entity)
        {
            try
            {
                // 使用 ExternalHandler 来安全地执行 Revit API 操作
                ExternalHandler.Run(app =>
                {
                    NewTransaction.Execute(Doc, "清除构件材质", () =>
                    {
                        string selectedAttribute = entity.SelectedMaterial;
                        int modifiedInstanceCount = 0;
                        string transactionMessage;
                        // 遍历该组中的每一个构件实例
                        foreach (var instance in entity.Instances)
                        {
                            bool wasModified = false;
                            // --- 情况一: 用户在该行中选择了特定的材质属性 ---
                            if (!string.IsNullOrEmpty(selectedAttribute))
                            {
                                // 调用辅助方法清除该实例的特定属性材质
                                if (ClearMaterialParameterForInstance(instance, selectedAttribute))
                                {
                                    wasModified = true;
                                }
                            }
                            // --- 情况二: 用户未选择任何属性，则清除所有可识别的材质属性 ---
                            else
                            {
                                // 遍历 ViewModel 中收集到的所有材质属性名称
                                foreach (var attrName in MaterialAttrnames)
                                {
                                    // 只要清除了任意一个属性，就标记该实例为已修改
                                    if (ClearMaterialParameterForInstance(instance, attrName))
                                    {
                                        wasModified = true;
                                    }
                                }
                            }
                            if (wasModified)
                            {
                                modifiedInstanceCount++;
                            }
                        }
                        // 根据执行的操作生成不同的提示信息
                        if (!string.IsNullOrEmpty(selectedAttribute))
                        {
                            transactionMessage = $"已清除 {modifiedInstanceCount} 个构件的 '{selectedAttribute}' 材质。";
                        }
                        else
                        {
                            transactionMessage = $"已清除 {modifiedInstanceCount} 个构件的所有可识别材质属性。";
                        }
                        TaskDialog.Show("操作完成", transactionMessage);
                    });
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("操作失败", ex.Message);
            }
        }
        /// <summary>
        /// 辅助方法：清除单个 FamilyInstance 及其类型的指定材质参数。
        /// </summary>
        /// <param name="instance">要操作的族实例。</param>
        /// <param name="parameterName">要清除的材质参数的名称。</param>
        /// <returns>如果成功清除了至少一个参数，则返回 true。</returns>
        private bool ClearMaterialParameterForInstance(FamilyInstance instance, string parameterName)
        {
            bool cleared = false;
            ElementId invalidId = ElementId.InvalidElementId;
            // 1. 检查并清除实例参数
            Parameter instParam = instance.LookupParameter(parameterName);
            if (instParam != null && !instParam.IsReadOnly && instParam.StorageType == StorageType.ElementId)
            {
                // 只有当当前值不是 InvalidElementId 时才进行设置，避免不必要的操作
                if (instParam.AsElementId() != invalidId)
                {
                    instParam.Set(invalidId);
                    cleared = true;
                }
            }
            // 2. 检查并清除类型参数
            // FamilyInstance.Symbol 属性可以安全地获取类型(ElementType)
            if (instance.Symbol != null)
            {
                Parameter typeParam = instance.Symbol.LookupParameter(parameterName);
                if (typeParam != null && !typeParam.IsReadOnly && typeParam.StorageType == StorageType.ElementId)
                {
                    if (typeParam.AsElementId() != invalidId)
                    {
                        typeParam.Set(invalidId);
                        cleared = true;
                    }
                }
            }
            return cleared;
        }
        // --- 接口实现: 批量删除逻辑 (替代原 DelInstanceInfos) 
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> entities)
        {
            if (entities == null || !entities.Any())
            {
                TaskDialog.Show("提示", "未选中任何构件组。");
                return;
            }
            var selectedItems = entities.OfType<SingleMarkModel>().ToList();
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(Doc, "删除构件注释", () =>
                {
                    int modifiedCount = 0;
                    foreach (SingleMarkModel item in selectedItems)
                    {
                        foreach (FamilyInstance instance in item.Instances)
                        {
                            // <-- 修正点: 使用 BuiltInParameter 替换字符串 "Comments" 和 "注释" -->
                            Parameter paramComment = instance.get_Parameter(CommentsParameterId);
                            if (paramComment != null && !paramComment.IsReadOnly)
                            {
                                paramComment.Set(string.Empty);
                                modifiedCount++;
                            }
                        }
                    }
                    TaskDialog.Show("成功", $"已清除 {modifiedCount} 个构件的注释。");
                });
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    InitFunc();
                });
            });
        }
        public ICommand ModifyMaterialCommand => new RelayCommand<SingleMarkModel>(ModifyMaterial);
        private void ModifyMaterial(SingleMarkModel selectedMarkModel)
        {
            if (selectedMarkModel == null) return;
            ElementId targetMaterialId = selectedMarkModel.SelectedMaterialId;
            if (targetMaterialId == null || targetMaterialId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("提示", "未选择有效材质，操作已取消。");
                return;
            }
            ExternalHandler.Run(app =>
            {
                string materialName = Doc.GetElement(targetMaterialId)?.Name ?? "未知材质";
                NewTransaction.Execute(Doc, "修改构件材质", () => // 确保使用 NewTransaction
                {
                    int modifiedCount = 0;
                    // 获取当前行选中的属性名称
                    string attributeToModify = selectedMarkModel.SelectedMaterial;
                    if (string.IsNullOrEmpty(attributeToModify))
                    {
                        TaskDialog.Show("错误", "请为该构件组选择一个要修改的属性。");
                        return;
                    }
                    foreach (FamilyInstance instance in selectedMarkModel.Instances)
                    {
                        // 尝试在实例级别查找参数
                        Parameter param = instance.LookupParameter(attributeToModify);

                        // 如果实例上找不到，并且有类型，尝试在类型级别查找
                        if (param == null && instance.Symbol != null)
                        {
                            param = instance.Symbol.LookupParameter(attributeToModify);
                        }

                        if (param != null && !param.IsReadOnly && param.StorageType == StorageType.ElementId)
                        {
                            param.Set(targetMaterialId);
                            modifiedCount++;
                        }
                    }
                    TaskDialog.Show("成功", $"已将 {modifiedCount} 个构件的 '{attributeToModify}' 属性修改为材质 '{materialName}'。");
                });
                // 修改材质后，通常不需要重新查询或刷新整个界面，因为材质属性的修改不会改变注释分组
                // 但如果修改的属性是MaterialAttrnames中的某个，可能UI需要更新
            });
        }
        public ICommand SelectElementsCommand => new RelayCommand<string>(SelectElements);
        private void SelectElements(string commentString)
        {
            SingleMarkModel model = Collection.FirstOrDefault(m => m.ComentStr == commentString);
            if (model == null) return;

            List<ElementId> selectedElementIds = model.Instances.Select(x => x.Id).ToList();
            uiDoc.Selection.SetElementIds(selectedElementIds);
        }
    }
}
