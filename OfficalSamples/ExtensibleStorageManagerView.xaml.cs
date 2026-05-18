using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ExtensibleStorageManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class ExtensibleStorageManagerView : Window
    {
        public ExtensibleStorageManagerView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public class ExtensibleStorageManagerViewModel : ObserverableObject
    {
        private readonly SchemaManagerService _schemaService;
        private readonly string _applicationId;

        private SchemaConfig _schemaConfig;
        private bool _isProcessing;
        private string _statusMessage;
        private SchemaDataModel _currentData;

        public ExtensibleStorageManagerViewModel(Document document, string applicationId)
        {
            _schemaService = new SchemaManagerService(document);
            _applicationId = applicationId;

            _schemaConfig = new SchemaConfig
            {
                SchemaId = Guid.NewGuid(),
                ApplicationId = applicationId,
                ReadAccess = AccessLevel.Public,
                WriteAccess = AccessLevel.Public,
                Name = "NewSchema",
                Documentation = "Schema 描述",
                VendorId = "MyVendor"
            };

            //// 初始化命令
            //CreateSimpleCommand = new BaseBindingCommand(CreateSimpleSchema, CanCreateSchema);
            //CreateComplexCommand = new BaseBindingCommand(CreateComplexSchema, CanCreateSchema);
            //LoadFromSchemaCommand = new BaseBindingCommand(LoadFromSchema, CanLoadFromSchema);
            //ImportFromXmlCommand = new BaseBindingCommand(ImportFromXml);
            //QueryEntityCommand = new BaseBindingCommand(QueryEntity, CanLoadFromSchema);
            //EditEntityCommand = new BaseBindingCommand(EditEntity, CanLoadFromSchema);
            //GenerateNewIdCommand = new BaseBindingCommand(GenerateNewId);
            CreateSimpleCommand = new BaseBindingCommand(CreateSimpleSchema);
            CreateComplexCommand = new BaseBindingCommand(CreateComplexSchema);
            LoadFromSchemaCommand = new BaseBindingCommand(LoadFromSchema);
            ImportFromXmlCommand = new BaseBindingCommand(ImportFromXml);
            QueryEntityCommand = new BaseBindingCommand(QueryEntity);
            EditEntityCommand = new BaseBindingCommand(EditEntity);
            GenerateNewIdCommand = new BaseBindingCommand(GenerateNewId);

            // 初始化可访问级别选项
            AccessLevelOptions = new ObservableCollection<AccessLevelOption>
            {
                new AccessLevelOption { Level = AccessLevel.Application, DisplayName = "Application" },
                new AccessLevelOption { Level = AccessLevel.Public, DisplayName = "Public" },
                new AccessLevelOption { Level = AccessLevel.Vendor, DisplayName = "Vendor" }
            };

            StatusMessage = "就绪";
        }

        public SchemaConfig SchemaConfig
        {
            get => _schemaConfig;
            set => SetProperty(ref _schemaConfig, value);
        }

        public ObservableCollection<AccessLevelOption> AccessLevelOptions { get; }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SchemaDataModel CurrentData
        {
            get => _currentData;
            set => SetProperty(ref _currentData, value);
        }

        public ICommand CreateSimpleCommand { get; }
        public ICommand CreateComplexCommand { get; }
        public ICommand LoadFromSchemaCommand { get; }
        public ICommand ImportFromXmlCommand { get; }
        public ICommand QueryEntityCommand { get; }
        public ICommand EditEntityCommand { get; }
        public ICommand GenerateNewIdCommand { get; }

        /// <summary>
        /// 生成新 GUID
        /// </summary>
        private void GenerateNewId(Object obj)
        {
            SchemaConfig.SchemaId = Guid.NewGuid();
        }

        /// <summary>
        /// 创建简单 Schema
        /// </summary>
        private async void CreateSimpleSchema(Object obj)
        {
            await CreateSchema(SampleSchemaComplexity.SimpleExample);
        }

        /// <summary>
        /// 创建复杂 Schema
        /// </summary>
        private async void CreateComplexSchema(Object obj)
        {
            await CreateSchema(SampleSchemaComplexity.ComplexExample);
        }

        /// <summary>
        /// 创建 Schema 的核心逻辑
        /// </summary>
        private async System.Threading.Tasks.Task CreateSchema(SampleSchemaComplexity complexity)
        {
            if (!ValidateConfig()) return;

            var saveDialog = new SaveFileDialog
            {
                DefaultExt = ".xml",
                Filter = "Schema XML 文件 (*.xml)|*.xml",
                FileName = $"{SchemaConfig.Name}_{SchemaConfig.VendorId}_{SchemaConfig.SchemaId.ToString().Substring(0, 8)}.xml"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsProcessing = true;
            StatusMessage = "正在创建 Schema...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, message, data) = _schemaService.CreateAndSave(
                    SchemaConfig, complexity, saveDialog.FileName);

                DispatcherHelper.Invoke(() =>
                {
                    if (success)
                    {
                        CurrentData = data;
                        StatusMessage = message;
                        ShowDataDialog(data);
                    }
                    else
                    {
                        StatusMessage = $"错误: {message}";
                        ShowErrorDialog(message);
                    }
                    IsProcessing = false;
                });
            });
        }

        /// <summary>
        /// 从现有 Schema 加载
        /// </summary>
        private async void LoadFromSchema(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在加载 Schema...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, message) = _schemaService.LoadFromSchema(SchemaConfig.SchemaId);

                DispatcherHelper.Invoke(() =>
                {
                    if (success)
                    {
                        StatusMessage = message;
                        UpdateConfigFromWrapper();
                        ShowSchemaDataDialog();
                    }
                    else
                    {
                        StatusMessage = $"错误: {message}";
                        ShowErrorDialog(message);
                    }
                    IsProcessing = false;
                });
            });
        }

        /// <summary>
        /// 从 XML 导入 Schema
        /// </summary>
        private async void ImportFromXml(Object obj)
        {
            var openDialog = new OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = "Schema XML 文件 (*.xml)|*.xml"
            };

            if (openDialog.ShowDialog() != true) return;

            IsProcessing = true;
            StatusMessage = "正在导入 XML...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, message) = _schemaService.ImportFromXml(openDialog.FileName);

                DispatcherHelper.Invoke(() =>
                {
                    if (success)
                    {
                        StatusMessage = message;
                        UpdateConfigFromWrapper();
                        ShowSchemaDataDialog();
                    }
                    else
                    {
                        StatusMessage = $"错误: {message}";
                        ShowErrorDialog(message);
                    }
                    IsProcessing = false;
                });
            });
        }

        /// <summary>
        /// 查询 Entity 数据
        /// </summary>
        private async void QueryEntity(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在查询 Entity...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, data) = _schemaService.QueryEntity(SchemaConfig.SchemaId);

                DispatcherHelper.Invoke(() =>
                {
                    if (success && data != null)
                    {
                        CurrentData = data;
                        StatusMessage = "查询成功";
                        ShowDataDialog(data);
                    }
                    else
                    {
                        StatusMessage = "未找到 Entity 数据";
                        ShowErrorDialog("未找到指定的 Entity");
                    }
                    IsProcessing = false;
                });
            });
        }

        /// <summary>
        /// 编辑 Entity 数据
        /// </summary>
        private async void EditEntity(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在编辑 Entity...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                var (success, message, data) = _schemaService.EditEntity(SchemaConfig.SchemaId);

                DispatcherHelper.Invoke(() =>
                {
                    if (success)
                    {
                        CurrentData = data;
                        StatusMessage = message;
                        ShowDataDialog(data);
                    }
                    else
                    {
                        StatusMessage = $"错误: {message}";
                        ShowErrorDialog(message);
                    }
                    IsProcessing = false;
                });
            });
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private bool ValidateConfig()
        {
            if (SchemaConfig.SchemaId == Guid.Empty)
            {
                ShowErrorDialog("Schema ID 不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SchemaConfig.Name))
            {
                ShowErrorDialog("Schema 名称不能为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 从 Wrapper 更新配置
        /// </summary>
        private void UpdateConfigFromWrapper()
        {
            if (_schemaService.CurrentWrapper.Metadata == null) return;

            var data = _schemaService.CurrentWrapper.Metadata;
            SchemaConfig = new SchemaConfig
            {
                SchemaId = Guid.TryParse(data.SchemaId, out var id) ? id : Guid.Empty,
                Name = data.Name,
                Documentation = data.Documentation,
                VendorId = data.VendorId,
                ApplicationId = data.ApplicationId,
                ReadAccess = data.ReadAccess,
                WriteAccess = data.WriteAccess,
                XmlPath = _schemaService.CurrentWrapper.GetXmlPath()
            };
        }

        private bool CanCreateSchema() => !IsProcessing;
        private bool CanLoadFromSchema() => !IsProcessing && SchemaConfig.SchemaId != Guid.Empty;

        private static void ShowErrorDialog(string message)
        {
            TaskDialog.Show("错误", message);
        }

        private void ShowSchemaDataDialog()
        {
            if (_schemaService.CurrentWrapper != null)
            {
                ShowDataDialog(new SchemaDataModel
                {
                    SchemaInfo = _schemaService.CurrentWrapper.ToString(),
                    EntityInfo = "请使用查询功能获取 Entity 数据"
                });
            }
        }

        private static void ShowDataDialog(SchemaDataModel data)
        {
            if (data == null) return;

            var dataWindow = new ExtensibleStorageDataView();
            dataWindow.SetData(data.FullInfo);
            dataWindow.ShowDialog();
        }
    }
    /// <summary>
    /// Schema 管理服务 - 封装所有可扩展存储操作
    /// </summary>
    public class SchemaManagerService
    {
        private readonly Document _document;
        private SchemaWrapper _schemaWrapper;

        public SchemaManagerService(Document document)
        {
            _document = document;
        }

        public SchemaWrapper CurrentWrapper => _schemaWrapper;

        /// <summary>
        /// 创建新 Schema 并保存数据
        /// </summary>
        public (bool success, string message, SchemaDataModel data) CreateAndSave(
            SchemaConfig config, SampleSchemaComplexity complexity, string xmlPath)
        {
            try
            {
                // 检查 Schema 是否已存在
                if (Schema.Lookup(config.SchemaId) != null)
                {
                    return (false, $"Schema GUID {config.SchemaId} 已存在", null);
                }

                using (var transaction = new Transaction(_document, "创建扩展存储 Schema"))
                {
                    transaction.Start();

                    // 创建 Schema
                    _schemaWrapper = SchemaWrapper.Create(
                        config.SchemaId, config.ReadAccess, config.WriteAccess,
                        config.VendorId, config.ApplicationId, config.Name, config.Documentation);

                    _schemaWrapper.SetXmlPath(xmlPath);

                    // 根据复杂度添加字段
                    Entity entity = complexity == SampleSchemaComplexity.SimpleExample
                        ? BuildSimpleSchema(_schemaWrapper)
                        : BuildComplexSchema(_schemaWrapper);

                    // 存储 Entity
                    _document.ProjectInformation.SetEntity(entity);

                    // 导出 XML
                    _schemaWrapper.ToXml(xmlPath);

                    transaction.Commit();

                    var dataModel = ExtractDataModel();
                    return (true, "Schema 创建成功", dataModel);
                }
            }
            catch (Exception ex)
            {
                return (false, $"创建失败: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 构建简单 Schema（基本类型）
        /// </summary>
        private static Entity BuildSimpleSchema(SchemaWrapper wrapper)
        {
            // 添加基本类型字段
            wrapper.AddField<int>("IntField", UnitType.UT_Undefined, null);
            wrapper.AddField<double>("DoubleField", UnitType.UT_Length, null);
            wrapper.AddField<bool>("BoolField", UnitType.UT_Undefined, null);
            wrapper.AddField<string>("StringField", UnitType.UT_Undefined, null);
            wrapper.AddField<float>("FloatField", UnitType.UT_Length, null);

            wrapper.FinishSchema();

            var schema = wrapper.GetSchema();
            var entity = new Entity(schema);

            // 设置示例数据
            entity.Set<int>(schema.GetField("IntField"), 42);
            entity.Set<double>(schema.GetField("DoubleField"), 3.14, DisplayUnitType.DUT_METERS);
            entity.Set(schema.GetField("BoolField"), true);
            entity.Set(schema.GetField("StringField"), "示例数据");
            entity.Set<float>(schema.GetField("FloatField"), 2.5f, DisplayUnitType.DUT_METERS);

            return entity;
        }

        /// <summary>
        /// 构建复杂 Schema（包含数组、字典、子实体）
        /// </summary>
        private static Entity BuildComplexSchema(SchemaWrapper wrapper)
        {
            // 添加基本类型
            BuildSimpleSchema(wrapper);

            // 添加 ElementId 和几何类型
            wrapper.AddField<ElementId>("ElementIdField", UnitType.UT_Undefined, null);
            wrapper.AddField<XYZ>("PointField", UnitType.UT_Length, null);
            wrapper.AddField<Guid>("GuidField", UnitType.UT_Undefined, null);

            // 添加集合类型
            wrapper.AddField<IDictionary<string, string>>("DictionaryField", UnitType.UT_Undefined, null);
            wrapper.AddField<IList<bool>>("ArrayField", UnitType.UT_Undefined, null);

            // 添加子实体
            var subSchema = SchemaWrapper.Create(
                Guid.NewGuid(), AccessLevel.Public, AccessLevel.Public,
                "adsk", "appId", "SubEntity", "子实体示例");
            subSchema.AddField<int>("SubIntField", UnitType.UT_Undefined, null);
            subSchema.FinishSchema();

            var subEntity = new Entity(subSchema.GetSchema());
            subEntity.Set<int>(subSchema.GetSchema().GetField("SubIntField"), 100);

            wrapper.AddField<Entity>("SubEntityField", UnitType.UT_Undefined, subSchema);
            wrapper.AddField<IList<Entity>>("EntityArrayField", UnitType.UT_Undefined, subSchema);
            wrapper.AddField<IDictionary<int, Entity>>("EntityMapField", UnitType.UT_Undefined, subSchema);

            wrapper.FinishSchema();

            var schema = wrapper.GetSchema();
            var entity = new Entity(schema);

            // 设置字典数据
            var dict = new Dictionary<string, string> { ["key1"] = "value1", ["key2"] = "value2" };
            entity.Set(schema.GetField("DictionaryField"), dict);

            // 设置数组数据
            var list = new List<bool> { true, false, true };
            entity.Set(schema.GetField("ArrayField"), list);

            // 设置子实体
            entity.Set(schema.GetField("SubEntityField"), subEntity);

            // 设置实体数组
            var entityList = new List<Entity> { subEntity, subEntity };
            entity.Set(schema.GetField("EntityArrayField"), entityList);

            // 设置实体字典
            var entityMap = new Dictionary<int, Entity> { [1] = subEntity, [2] = subEntity };
            entity.Set(schema.GetField("EntityMapField"), entityMap);

            // 设置几何数据
            entity.Set(schema.GetField("PointField"), new XYZ(1, 2, 3), DisplayUnitType.DUT_METERS);
            entity.Set(schema.GetField("GuidField"), Guid.NewGuid());

            return entity;
        }

        /// <summary>
        /// 从现有 Schema 加载
        /// </summary>
        public (bool success, string message) LoadFromSchema(Guid schemaId)
        {
            var schema = Schema.Lookup(schemaId);
            if (schema == null)
            {
                return (false, $"Schema {schemaId} 不存在");
            }

            _schemaWrapper = SchemaWrapper.FromSchema(schema);
            return (true, "加载成功");
        }

        /// <summary>
        /// 从 XML 导入 Schema
        /// </summary>
        public (bool success, string message) ImportFromXml(string xmlPath)
        {
            try
            {
                _schemaWrapper = SchemaWrapper.FromXml(xmlPath);
                _schemaWrapper.SetXmlPath(xmlPath);
                return (true, "导入成功");
            }
            catch (Exception ex)
            {
                return (false, $"导入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询并提取 Entity 数据
        /// </summary>
        public (bool success, SchemaDataModel data) QueryEntity(Guid schemaId)
        {
            var schema = Schema.Lookup(schemaId);
            if (schema == null)
            {
                return (false, null);
            }

            var entity = _document.ProjectInformation.GetEntity(schema);
            if (entity == null || !entity.IsValid())
            {
                return (false, null);
            }

            _schemaWrapper = SchemaWrapper.FromSchema(schema);
            var dataModel = ExtractDataModel();
            return (true, dataModel);
        }

        /// <summary>
        /// 编辑现有 Entity 数据
        /// </summary>
        public (bool success, string message, SchemaDataModel data) EditEntity(Guid schemaId)
        {
            var schema = Schema.Lookup(schemaId);
            if (schema == null)
            {
                return (false, $"Schema {schemaId} 不存在", null);
            }

            using (var transaction = new Transaction(_document, "编辑扩展存储数据"))
            {
                transaction.Start();

                _schemaWrapper = SchemaWrapper.FromSchema(schema);
                var entity = new Entity(schema);

                // 更新数据
                var intField = schema.GetField("IntField");
                if (intField != null)
                {
                    entity.Set<int>(intField, 999);
                }

                var stringField = schema.GetField("StringField");
                if (stringField != null)
                {
                    entity.Set(stringField, "已编辑的数据");
                }

                _document.ProjectInformation.SetEntity(entity);
                transaction.Commit();

                var dataModel = ExtractDataModel();
                return (true, "编辑成功", dataModel);
            }
        }

        /// <summary>
        /// 提取 Schema 和 Entity 数据用于显示
        /// </summary>
        private SchemaDataModel ExtractDataModel()
        {
            if (_schemaWrapper == null) return null;

            var schema = _schemaWrapper.GetSchema();
            var entity = _document.ProjectInformation.GetEntity(schema);

            return new SchemaDataModel
            {
                SchemaInfo = _schemaWrapper.ToString(),
                EntityInfo = entity?.IsValid() == true
                    ? _schemaWrapper.GetEntityData(entity)
                    : "无 Entity 数据"
            };
        }
    }
    /// <summary>
    /// 访问级别选项
    /// </summary>
    public class AccessLevelOption
    {
        public AccessLevel Level { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Schema 配置数据模型
    /// </summary>
    public class SchemaConfig : ObserverableObject
    {
        private Guid _schemaId;
        private string _name;
        private string _documentation;
        private string _vendorId;
        private string _applicationId;
        private AccessLevel _readAccess;
        private AccessLevel _writeAccess;
        private string _xmlPath;

        public Guid SchemaId
        {
            get => _schemaId;
            set => SetProperty(ref _schemaId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Documentation
        {
            get => _documentation;
            set => SetProperty(ref _documentation, value);
        }

        public string VendorId
        {
            get => _vendorId;
            set => SetProperty(ref _vendorId, value);
        }

        public string ApplicationId
        {
            get => _applicationId;
            set => SetProperty(ref _applicationId, value);
        }

        public AccessLevel ReadAccess
        {
            get => _readAccess;
            set => SetProperty(ref _readAccess, value);
        }

        public AccessLevel WriteAccess
        {
            get => _writeAccess;
            set => SetProperty(ref _writeAccess, value);
        }

        public string XmlPath
        {
            get => _xmlPath;
            set => SetProperty(ref _xmlPath, value);
        }
    }

    /// <summary>
    /// Schema 数据展示模型
    /// </summary>
    public class SchemaDataModel
    {
        public string SchemaInfo { get; set; }
        public string EntityInfo { get; set; }
        public string FullInfo => $"Schema:{Environment.NewLine}{SchemaInfo}{Environment.NewLine}{Environment.NewLine}Entity:{Environment.NewLine}{EntityInfo}";
    }

    /// <summary>
    /// An enum to select which sample schema to create.
    /// </summary>
    public enum SampleSchemaComplexity
    {
        SimpleExample = 1,
        ComplexExample = 2
    }
    /// <summary>
    /// 跨线程 UI 更新辅助类
    /// </summary>
    public static class DispatcherHelper
    {
        public static void Invoke(Action action)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
