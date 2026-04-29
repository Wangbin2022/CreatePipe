using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    /// UniqueIdManagerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UniqueIdManagerWindow : Window
    {
        public UniqueIdManagerWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 唯一ID管理器视图模型
    /// </summary>
    public class UniqueIdManagerViewModel : ObserverableObject
    {
        private readonly UniqueIdManagementService _uniqueIdService;
        private readonly SharedParameterService _parameterService;

        private ObservableCollection<UniqueIdParameterInfo> _uniqueIdList;
        private UniqueIdParameterInfo _selectedItem;
        private bool _isProcessing;
        private string _statusMessage;
        private bool _isParameterAdded;

        public UniqueIdManagerViewModel(ExternalCommandData commandData)
        {
            _uniqueIdService = new UniqueIdManagementService(commandData);
            _parameterService = new SharedParameterService(commandData);
            _uniqueIdList = new ObservableCollection<UniqueIdParameterInfo>();

            // 检查参数是否已添加
            _isParameterAdded = _parameterService.IsParameterExists();

            // 初始化命令
            AddParameterCommand = new BaseBindingCommand(_ => AddParameter(), _ => !IsProcessing && !IsParameterAdded);
            GenerateIdsCommand = new BaseBindingCommand(_ => GenerateUniqueIds(), _ => !IsProcessing && IsParameterAdded);
            RefreshCommand = new BaseBindingCommand(_ => RefreshList(), _ => !IsProcessing);
            FindElementCommand = new BaseBindingCommand(_ => FindAndHighlightElement(), _ => SelectedItem != null);
            CloseCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 加载数据
            RefreshList();
        }

        /// <summary>
        /// 唯一ID列表
        /// </summary>
        public ObservableCollection<UniqueIdParameterInfo> UniqueIdList
        {
            get => _uniqueIdList;
            set { _uniqueIdList = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的项
        /// </summary>
        public UniqueIdParameterInfo SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 参数是否已添加
        /// </summary>
        public bool IsParameterAdded
        {
            get => _isParameterAdded;
            set { _isParameterAdded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ParameterStatusText)); }
        }

        /// <summary>
        /// 参数状态文本
        /// </summary>
        public string ParameterStatusText => IsParameterAdded ? "✓ 共享参数已添加" : "✗ 共享参数未添加";

        public ICommand AddParameterCommand { get; }
        public ICommand GenerateIdsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FindElementCommand { get; }
        public ICommand CloseCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 添加共享参数
        /// </summary>
        private void AddParameter()
        {
            IsProcessing = true;
            StatusMessage = "正在添加共享参数...";

            try
            {
                var success = _parameterService.AddSharedParameter();
                if (success)
                {
                    IsParameterAdded = true;
                    StatusMessage = "共享参数添加成功";
                    RefreshList();
                }
                else
                {
                    StatusMessage = "共享参数添加失败，请检查权限";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"添加失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        private void GenerateUniqueIds()
        {
            IsProcessing = true;
            StatusMessage = "正在生成唯一ID...";

            try
            {
                // 先为选中的构件生成，如果没有选中则为全部生成
                var updatedCount = _uniqueIdService.SetUniqueIdsForSelected();
                if (updatedCount == 0)
                {
                    updatedCount = _uniqueIdService.SetUniqueIdsForAll();
                }

                StatusMessage = $"成功为 {updatedCount} 个构件生成了唯一ID";
                RefreshList();
            }
            catch (Exception ex)
            {
                StatusMessage = $"生成失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        private void RefreshList()
        {
            try
            {
                var items = _uniqueIdService.GetSelectedUniqueIds();
                UniqueIdList.Clear();
                foreach (var item in items)
                {
                    UniqueIdList.Add(item);
                }

                if (!UniqueIdList.Any())
                {
                    StatusMessage = IsParameterAdded
                        ? "当前选中构件中没有梁或楼板，或它们尚未分配唯一ID"
                        : "请先点击'添加共享参数'按钮创建参数";
                }
                else
                {
                    StatusMessage = $"共 {UniqueIdList.Count} 个构件具有唯一ID";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 查找并高亮元素
        /// </summary>
        private void FindAndHighlightElement()
        {
            if (SelectedItem == null) return;

            IsProcessing = true;
            StatusMessage = "正在查找元素...";

            try
            {
                var element = _uniqueIdService.FindElementByUniqueId(SelectedItem.UniqueIdValue);
                if (element != null)
                {
                    _uniqueIdService.HighlightElement(element);
                    StatusMessage = $"已找到并高亮: {SelectedItem.DisplayText}";
                }
                else
                {
                    StatusMessage = "未找到对应的元素";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"查找失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
    /// <summary>
    /// 唯一ID管理服务类
    /// 负责为梁和楼板生成和管理唯一标识
    /// </summary>
    public class UniqueIdManagementService
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _document;
        private const string ParameterName = "Unique ID";


        public UniqueIdManagementService(ExternalCommandData commandData)
        {
            _uiDoc = commandData.Application.ActiveUIDocument;
            _document = _uiDoc.Document;
        }

        /// <summary>
        /// 获取所有梁和楼板类型的过滤器
        /// </summary>
        private LogicalOrFilter GetBeamAndSlabFilter()
        {
            var beamFilter = new LogicalAndFilter(
                new ElementClassFilter(typeof(FamilyInstance)),
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming));

            var slabFilter = new LogicalAndFilter(
                new ElementClassFilter(typeof(Floor)),
                new ElementCategoryFilter(BuiltInCategory.OST_Floors));

            return new LogicalOrFilter(beamFilter, slabFilter);
        }

        /// <summary>
        /// 为所有梁和楼板设置唯一ID
        /// </summary>
        public int SetUniqueIdsForAll()
        {
            var filter = GetBeamAndSlabFilter();
            var collector = new FilteredElementCollector(_document);
            var elements = collector.WherePasses(filter).ToElements();

            int updatedCount = 0;

            //foreach (Element element in elements)
            //{
            //    var param = element.get_Parameter(ParameterName);
            //    if (param != null && string.IsNullOrEmpty(param.AsString()))
            //    {
            //        param.Set(Guid.NewGuid().ToString());
            //        updatedCount++;
            //    }
            //}

            return updatedCount;
        }

        /// <summary>
        /// 为选中的梁和楼板设置唯一ID
        /// </summary>
        public int SetUniqueIdsForSelected()
        {
            var selectedIds = _uiDoc.Selection.GetElementIds();
            var filter = GetBeamAndSlabFilter();

            int updatedCount = 0;

            foreach (var id in selectedIds)
            {
                var element = _document.GetElement(id);
                if (element == null) continue;

                // 检查是否为梁或楼板
                if (element.Category == null) continue;
                var catName = element.Category.Name;
                if (catName != "Structural Framing" && catName != "Floors") continue;

                //var param = element.get_Parameter(ParameterName);
                //if (param != null && string.IsNullOrEmpty(param.AsString()))
                //{
                //    param.Set(Guid.NewGuid().ToString());
                //    updatedCount++;
                //}
            }

            return updatedCount;
        }

        /// <summary>
        /// 获取选中构件的唯一ID列表
        /// </summary>
        public List<UniqueIdParameterInfo> GetSelectedUniqueIds()
        {
            var results = new List<UniqueIdParameterInfo>();
            var selectedIds = _uiDoc.Selection.GetElementIds();

            foreach (var id in selectedIds)
            {
                var element = _document.GetElement(id);
                if (element?.Category == null) continue;

                var catName = element.Category.Name;
                if (catName != "Structural Framing" && catName != "Floors") continue;

                //var param = element.get_Parameter(ParameterName);
                //if (param != null && !string.IsNullOrEmpty(param.AsString()))
                //{
                //    results.Add(new UniqueIdParameterInfo
                //    {
                //        ElementId = element.Id.IntegerValue,
                //        ElementName = element.Name ?? "未命名",
                //        CategoryName = catName,
                //        UniqueIdValue = param.AsString(),
                //        Element = element
                //    });
                //}
            }

            return results;
        }

        /// <summary>
        /// 根据唯一ID查找并高亮元素
        /// </summary>
        public Element FindElementByUniqueId(string uniqueId)
        {
            var filter = GetBeamAndSlabFilter();
            var collector = new FilteredElementCollector(_document);
            var elements = collector.WherePasses(filter).ToElements();

            //foreach (Element element in elements)
            //{
            //    var param = element.get_Parameter(ParameterName);
            //    if (param != null && param.AsString() == uniqueId)
            //    {
            //        return element;
            //    }
            //}

            return null;
        }

        /// <summary>
        /// 高亮显示指定元素
        /// </summary>
        public void HighlightElement(Element element)
        {
            if (element != null)
            {
                _uiDoc.Selection.SetElementIds(new List<ElementId> { element.Id });
                _uiDoc.ShowElements(element);
            }
        }
    }
    /// <summary>
    /// 共享参数服务类
    /// 负责创建和管理共享参数
    /// </summary>
    public class SharedParameterService
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;
        private const string ParameterName = "Unique ID";
        private const string ParameterGroupName = "MyParameters";

        public SharedParameterService(ExternalCommandData commandData)
        {
            _uiApp = commandData.Application;
            _document = _uiApp.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取或创建共享参数文件
        /// </summary>
        private DefinitionFile GetOrCreateSharedParameterFile()
        {
            // 共享参数文件路径（与Revit.exe同目录）
            var revitPath = System.Windows.Forms.Application.ExecutablePath;
            var sharedParamFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(revitPath), "MySharedParameters.txt");

            // 确保文件存在
            if (!File.Exists(sharedParamFile))
            {
                File.Create(sharedParamFile).Close();
            }

            // 设置并打开共享参数文件
            _uiApp.Application.SharedParametersFilename = sharedParamFile;
            return _uiApp.Application.OpenSharedParameterFile();
        }

        /// <summary>
        /// 获取或创建参数定义
        /// </summary>
        private ExternalDefinition GetOrCreateDefinition(DefinitionFile defFile)
        {
            if (defFile == null) return null;

            // 获取或创建参数组
            DefinitionGroup group = defFile.Groups
                .Cast<DefinitionGroup>()
                .FirstOrDefault(g => g.Name == ParameterGroupName)
                ?? defFile.Groups.Create(ParameterGroupName);
            // 查找已有定义（使用 OfType 过滤类型）
            ExternalDefinition definition = group.Definitions
                .Cast<Definition>()
                .OfType<ExternalDefinition>()
                .FirstOrDefault(d => d.Name == ParameterName);
            // 未找到则创建
            if (definition == null)
            {
                var options = new ExternalDefinitionCreationOptions(ParameterName, ParameterType.Text);
                Definition newDef = group.Definitions.Create(options);
                definition = newDef as ExternalDefinition;
            }
            return definition;
        }

        /// <summary>
        /// 创建参数绑定
        /// </summary>
        private InstanceBinding CreateParameterBinding()
        {
            var categories = _uiApp.Application.Create.NewCategorySet();

            var beamCategory = _document.Settings.Categories.get_Item(BuiltInCategory.OST_StructuralFraming);
            var slabCategory = _document.Settings.Categories.get_Item(BuiltInCategory.OST_Floors);

            categories.Insert(beamCategory);
            categories.Insert(slabCategory);

            return _uiApp.Application.Create.NewInstanceBinding(categories);
        }

        /// <summary>
        /// 添加共享参数到文档
        /// </summary>
        public bool AddSharedParameter()
        {
            var defFile = GetOrCreateSharedParameterFile();
            if (defFile == null) return false;

            var definition = GetOrCreateDefinition(defFile);
            var binding = CreateParameterBinding();

            return _document.ParameterBindings.Insert(definition, binding);
        }

        /// <summary>
        /// 检查参数是否已存在
        /// </summary>
        public bool IsParameterExists()
        {
            var defFile = GetOrCreateSharedParameterFile();
            if (defFile == null) return false;

            var group = defFile.Groups.Cast<DefinitionGroup>().FirstOrDefault(g => g.Name == ParameterGroupName);
            if (group == null) return false;

            return group.Definitions.Cast<ExternalDefinition>().Any(d => d.Name == ParameterName);
        }
    }
    /// <summary>
    /// 唯一ID参数信息模型
    /// </summary>
    public class UniqueIdParameterInfo : ObserverableObject
    {
        private int _elementId;
        private string _elementName;
        private string _categoryName;
        private string _uniqueIdValue;
        private Element _element;

        public int ElementId
        {
            get => _elementId;
            set { _elementId = value; OnPropertyChanged(); }
        }

        public string ElementName
        {
            get => _elementName;
            set { _elementName = value; OnPropertyChanged(); }
        }

        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        public string UniqueIdValue
        {
            get => _uniqueIdValue;
            set { _uniqueIdValue = value; OnPropertyChanged(); }
        }

        public Element Element
        {
            get => _element;
            set { _element = value; OnPropertyChanged(); }
        }
        public string DisplayText => $"{CategoryName} - {ElementName} (ID: {ElementId}) - {UniqueIdValue}";
    }

}
