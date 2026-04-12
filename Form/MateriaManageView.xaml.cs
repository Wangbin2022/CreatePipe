using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
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
    /// MateriaManageView.xaml 的交互逻辑
    /// </summary>
    public partial class MateriaManageView : Window
    {
        public MateriaManageView(Document document)
        {
            InitializeComponent();
            this.DataContext = new MateriaManagerViewModel(document);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class MateriaManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<MaterialEntity>
    {
        private Document _document;
        public BaseExternalHandler ExternalHandler => new BaseExternalHandler();
        //private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        //public BaseExternalHandler ExternalHandler => _externalHandler;

        // 缓存全量材质列表，用于内存级快速搜索
        private List<MaterialEntity> _cachedMaterials = new List<MaterialEntity>();

        public MateriaManagerViewModel(Document document)
        {
            _document = document;
            InitFunc(); // 初始化数据
        }
        private ObservableCollection<MaterialEntity> _collection = new ObservableCollection<MaterialEntity>();
        public ObservableCollection<MaterialEntity> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
                OnPropertyChanged(nameof(Collection));
            }
        }
        // 4. 初始化方法
        public void InitFunc()
        {
            HashSet<ElementId> usedMaterialIds = GetAllUsedMaterialIds(_document);
            // 2. 获取所有的材质并初始化 Entity
            var materials = new FilteredElementCollector(_document).OfClass(typeof(Material)).Cast<Material>()
                .Select(m =>
                {
                    var entity = new MaterialEntity(m, ExternalHandler);
                    // 3. O(1) 极速校验该材质是否被使用
                    entity.IsUsed = usedMaterialIds.Contains(m.Id);
                    return entity;
                }).OrderBy(m => m.Name).ToList();
            _cachedMaterials = materials;
            QueryElement(null); // 加载全部数据到 UI
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            Collection.Clear();
            var queryResult = string.IsNullOrWhiteSpace(text)
                ? _cachedMaterials
                : _cachedMaterials.Where(e => e.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var item in queryResult)
            {
                Collection.Add(item);
            }
        }
        public HashSet<ElementId> GetAllUsedMaterialIds(Document doc)
        {
            HashSet<ElementId> usedMaterialIds = new HashSet<ElementId>();
            // 1. 检查所有类别和子类别的默认材质 (Object Styles)
            foreach (Category cat in doc.Settings.Categories)
            {
                if (cat.Material != null && cat.Material.Id != ElementId.InvalidElementId)
                {
                    usedMaterialIds.Add(cat.Material.Id);
                }
                // 遍历子类别
                foreach (Category subCat in cat.SubCategories)
                {
                    if (subCat.Material != null && subCat.Material.Id != ElementId.InvalidElementId)
                    {
                        usedMaterialIds.Add(subCat.Material.Id);
                    }
                }
            }
            // 2. 收集模型中的所有实例 (Instances) 和 类型 (Types)
            FilteredElementCollector instances = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            FilteredElementCollector types = new FilteredElementCollector(doc).WhereElementIsElementType();
            // 合并实例和类型的集合
            var allElements = instances.UnionWith(types);
            // 3. 遍历所有元素，提取它们使用的材质
            foreach (Element el in allElements)
            {
                // 排除一些没有材质的系统级元素，提升部分性能
                if (el.Category == null) continue;
                // 获取元素本身的材质（包含通过参数赋予的、以及复合结构层中的）
                ICollection<ElementId> baseMatIds = el.GetMaterialIds(false);
                foreach (ElementId matId in baseMatIds)
                {
                    if (matId != ElementId.InvalidElementId) usedMaterialIds.Add(matId);
                }
                // 获取通过"油漆(Paint)"工具涂抹在该元素表面的材质
                ICollection<ElementId> paintedMatIds = el.GetMaterialIds(true);
                foreach (ElementId matId in paintedMatIds)
                {
                    if (matId != ElementId.InvalidElementId) usedMaterialIds.Add(matId);
                }
            }
            return usedMaterialIds;
        }
        public ICommand NewMaterialCommand => new BaseBindingCommand(NewMaterial);
        private void NewMaterial(object obj)
        {
            // 1. 生成默认名称（用于预填充到输入框）
            string baseName = "新材质";
            string defaultName = baseName;
            int counter = 1;
            var existingNames = new HashSet<string>(_cachedMaterials.Select(m => m.Name));
            while (existingNames.Contains(defaultName))
            {
                defaultName = $"{baseName} {counter++}";
            }
            // 2. 在 UI 线程弹出命名对话框（必须在 ExternalHandler.Run 之外）
            var dialog = new UniversalNewString("请输入新材质名称：", defaultName);
            bool? result = dialog.ShowDialog();
            // 3. 用户取消则退出
            if (result != true || string.IsNullOrWhiteSpace(dialog.ViewModel.NewName)) return;
            string finalName = dialog.ViewModel.NewName.Trim();
            // 4. 检查名称是否已存在
            if (existingNames.Contains(finalName))
            {
                System.Windows.MessageBox.Show($"材质名称 {finalName}已存在，请使用其他名称。", "名称重复",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            // 5. 进入 Revit API 上下文执行创建
            ExternalHandler.Run(app =>
            {
                Material newRevitMaterial = null;
                NewTransaction.Execute(_document, "创建新材质", () =>
                {
                    ElementId newId = Autodesk.Revit.DB.Material.Create(_document, finalName);
                    newRevitMaterial = _document.GetElement(newId) as Material;
                });
                if (newRevitMaterial != null)
                {
                    var newMaterialEntity = new MaterialEntity(newRevitMaterial, ExternalHandler)
                    {
                        IsUsed = false
                    };
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _cachedMaterials.Insert(0, newMaterialEntity);
                        Collection.Insert(0, newMaterialEntity);
                    });
                }
            });
        }
        public ICommand SetColorCommand => new RelayCommand<MaterialEntity>(SetColor);
        private void SetColor(MaterialEntity entity)
        {
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(_document, "修改材质", () =>
                {
                    Autodesk.Revit.DB.Color initialColor = entity.Color;
                    var inColor = System.Windows.Media.Color.FromRgb(initialColor.Red, initialColor.Green, initialColor.Blue);
                    var dialog = new ColorPickerDialog(inColor);
                    if (dialog.ShowDialog() == true)
                    {
                        var color = dialog.SelectedColor.ConvertToRevitColor();
                        entity.Color = color;
                        entity.Material.Color = color;
                    }
                    QueryElement(null);
                });
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<MaterialEntity>(DeleteElement);
        public void DeleteElement(MaterialEntity material)
        {
            if (material == null || material.Material == null) return;
            ExternalHandler.Run(app =>
            {
                NewTransaction.Execute(_document, "删除材质", () =>
                {
                    _document.Delete(material.Material.Id);
                });
                // UI 操作需要回到主线程（如果您使用的集合不支持后台线程修改）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _cachedMaterials.Remove(material);
                    Collection.Remove(material);
                });
            });
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            if (selectedElements == null) return;
            var selectedItems = selectedElements.Cast<MaterialEntity>().ToList();
            if (!selectedItems.Any()) return;
            ExternalHandler.Run(app =>
            {
                TransactionWithProgressBarHelper.Execute(_document, "删除多材质", (service) =>
                {
                    // 提取所有要删除的 ID
                    var idsToDelete = selectedItems.Where(m => m.Material != null).Select(m => m.Material.Id).ToList();
                    if (!idsToDelete.Any()) return;
                    service.UpdateMax(idsToDelete.Count);
                    int index = 0;
                    //_document.Delete(idsToDelete); // API 原生支持集合删除，速度极快
                    foreach (var material in selectedItems)
                    {
                        service.Update(++index, material.Name);
                        _document.Delete(material.Id);
                        _cachedMaterials.Remove(material);
                        Collection.Remove(material);
                    }
                });
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    QueryElement(null);
                    TaskDialog.Show("tt", "删除完成");
                });
            });
        }
    }
}
