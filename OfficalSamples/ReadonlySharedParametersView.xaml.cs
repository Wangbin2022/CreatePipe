using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ReadonlySharedParametersView.xaml 的交互逻辑
    /// </summary>
    public partial class ReadonlySharedParametersView : Window
    {
        public ReadonlySharedParametersView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主窗口ViewModel - 管理共享参数操作
    /// </summary>
    public class ReadonlySharedParametersViewModel : ObserverableObject
    {
        private readonly Document _document;
        private bool _isProcessing;
        private string _statusMessage;

        public ObservableCollection<OperationItemModel> Operations { get; } = new ObservableCollection<OperationItemModel>();

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand ExecuteSelectedCommand { get; }
        public ICommand ExecuteAllCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand CancelCommand { get; }

        public ReadonlySharedParametersViewModel(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeOperations();

            ExecuteSelectedCommand = new BaseBindingCommand(_ => ExecuteSelected(), _ => CanExecute);
            ExecuteAllCommand = new BaseBindingCommand(_ => ExecuteAll(), _ => CanExecute);
            SelectAllCommand = new BaseBindingCommand(_ => SetAllSelected(true));
            DeselectAllCommand = new BaseBindingCommand(_ => SetAllSelected(false));
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void InitializeOperations()
        {
            Operations.Add(new OperationItemModel(
                SharedParameterAction.BindParameters,
                "绑定只读共享参数",
                "创建 ReadonlyId 和 ReadonlyCost 参数并绑定到文档"));

            Operations.Add(new OperationItemModel(
                SharedParameterAction.SetReadonlyCostById,
                "设置只读成本 (基于ID)",
                "使用 ElementId 的后两位计算成本值"));

            Operations.Add(new OperationItemModel(
                SharedParameterAction.SetReadonlyCostByIncrement,
                "设置只读成本 (基于增量)",
                "使用递增序号计算成本值"));

            Operations.Add(new OperationItemModel(
                SharedParameterAction.SetReadonlyIdByUniqueId,
                "设置只读ID (基于UniqueId)",
                "使用元素的 UniqueId 作为只读ID"));

            Operations.Add(new OperationItemModel(
                SharedParameterAction.SetReadonlyIdByTypeAndId,
                "设置只读ID (基于类型+ID)",
                "使用类型名前2字符 + ElementId 作为只读ID"));
        }

        private void SetAllSelected(bool selected)
        {
            foreach (var op in Operations)
            {
                op.IsSelected = selected;
            }
        }

        private void ExecuteSelected()
        {
            IsProcessing = true;
            StatusMessage = "正在执行...";

            try
            {
                using (var transaction = new Transaction(_document, "执行共享参数操作"))
                {
                    transaction.Start();

                    foreach (var op in Operations)
                    {
                        if (op.IsSelected)
                        {
                            ExecuteAction(op.Action);
                        }
                    }

                    transaction.Commit();
                }

                StatusMessage = "所有选中的操作执行成功。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"执行失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteAll()
        {
            SetAllSelected(true);
            ExecuteSelected();
        }

        private void ExecuteAction(SharedParameterAction action)
        {
            switch (action)
            {
                case SharedParameterAction.BindParameters:
                    SharedParameterBinder.BindSharedParameters(_document);
                    break;
                case SharedParameterAction.SetReadonlyCostById:
                    ReadonlyCostSetter.SetCostsById(_document);
                    break;
                case SharedParameterAction.SetReadonlyCostByIncrement:
                    ReadonlyCostSetter.SetCostsByIncrement(_document);
                    break;
                case SharedParameterAction.SetReadonlyIdByUniqueId:
                    ReadonlyIdSetter.SetIdsByUniqueId(_document);
                    break;
                case SharedParameterAction.SetReadonlyIdByTypeAndId:
                    ReadonlyIdSetter.SetIdsByTypeAndId(_document);
                    break;
            }
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    /// <summary>
    /// 只读成本参数设置器 - 使用C# 7.3表达式体
    /// </summary>
    public static class ReadonlyCostSetter
    {
        private const string ParameterName = "ReadonlyCost";

        /// <summary>
        /// 使用元素ID计算成本值
        /// </summary>
        public static void SetCostsById(Document doc) =>
            SetCosts(doc, elem => (elem.Id.IntegerValue % 100) * 100.0 + 0.99);

        /// <summary>
        /// 使用递增序号计算成本值
        /// </summary>
        public static void SetCostsByIncrement(Document doc) =>
            SetCosts(doc, (elem, increment) => increment * 100.0 + 0.88);

        /// <summary>
        /// 设置成本参数的核心方法
        /// </summary>
        private static void SetCosts(Document doc, Func<Element, int, double> valueGetter)
        {
            var elements = GetElementsWithParameter(doc, ParameterName);
            if (!elements.Any()) return;

            using (var transaction = new Transaction(doc, "设置只读成本"))
            {
                transaction.Start();

                int increment = 1;
                foreach (var elem in elements)
                {
                    var param = elem.LookupParameter(ParameterName);
                    param?.Set(valueGetter(elem, increment));
                    increment++;
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// 重载：使用仅元素参数的函数（用于基于ID的计算）
        /// </summary>
        private static void SetCosts(Document doc, Func<Element, double> valueGetter)
        {
            var elements = GetElementsWithParameter(doc, ParameterName);
            if (!elements.Any()) return;

            using (var transaction = new Transaction(doc, "设置只读成本"))
            {
                transaction.Start();

                foreach (var elem in elements)
                {
                    var param = elem.LookupParameter(ParameterName);
                    param?.Set(valueGetter(elem));
                }

                transaction.Commit();
            }
        }

        private static IQueryable<Element> GetElementsWithParameter(Document doc, string paramName)
        {
            var rule = ParameterFilterRuleFactory.CreateSharedParameterApplicableRule(paramName);
            var filter = new ElementParameterFilter(rule);

            return new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .WherePasses(filter)
                .ToElements()
                .AsQueryable();
        }
    }
    /// <summary>
    /// 只读ID参数设置器 - 使用C# 7.3表达式体
    /// </summary>
    public static class ReadonlyIdSetter
    {
        private const string ParameterName = "ReadonlyId";

        /// <summary>
        /// 使用UniqueId作为只读ID
        /// </summary>
        public static void SetIdsByUniqueId(Document doc) =>
            SetIds(doc, elem => elem.UniqueId);

        /// <summary>
        /// 使用类型名前2字符+元素ID作为只读ID
        /// </summary>
        public static void SetIdsByTypeAndId(Document doc) =>
            SetIds(doc, elem =>
            {
                var typeId = elem.GetTypeId();
                if (typeId == ElementId.InvalidElementId) return elem.Id.IntegerValue.ToString();

                var typeElem = doc.GetElement(typeId);
                var prefix = typeElem?.Name.Length >= 2 ? typeElem.Name.Substring(0, 2) : "00";
                return $"{prefix}{elem.Id.IntegerValue}";
            });

        private static void SetIds(Document doc, Func<Element, string> idGetter)
        {
            var elements = GetElementsWithParameter(doc, ParameterName);
            if (!elements.Any()) return;

            using (var transaction = new Transaction(doc, "设置只读ID"))
            {
                transaction.Start();

                foreach (var elem in elements)
                {
                    var param = elem.LookupParameter(ParameterName);
                    param?.Set(idGetter(elem));
                }

                transaction.Commit();
            }
        }

        private static IQueryable<Element> GetElementsWithParameter(Document doc, string paramName)
        {
            var rule = ParameterFilterRuleFactory.CreateSharedParameterApplicableRule(paramName);
            var filter = new ElementParameterFilter(rule);

            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType() // 实例参数，不是类型参数
                .WherePasses(filter)
                .ToElements()
                .AsQueryable();
        }
    }
    /// <summary>
    /// 共享参数绑定器 - 创建并绑定只读共享参数
    /// 使用C# 7.3语法：元组、表达式体
    /// </summary>
    public static class SharedParameterBinder
    {
        /// <summary>
        /// 绑定共享参数到文档的入口方法
        /// </summary>
        public static void BindSharedParameters(Document doc)
        {
            // 创建临时共享参数文件
            var spFilePath = CreateTempSharedParameterFile();
            var app = doc.Application;

            // 设置共享参数文件
            var originalFilePath = app.SharedParametersFilename;
            app.SharedParametersFilename = spFilePath;

            try
            {
                var defFile = app.OpenSharedParameterFile();
                var defGroup = defFile.Groups.Create("RevitAPIDemo_Group");

                // 创建两个共享参数定义
                var paramConfigs = BuildParameterConfigurations();

                using (var transaction = new Transaction(doc, "绑定只读共享参数"))
                {
                    transaction.Start();

                    foreach (var config in paramConfigs)
                    {
                        var definition = defGroup.Definitions.Create(config.CreateOptions);
                        config.AddBindings(doc, definition);
                    }

                    transaction.Commit();
                }
            }
            finally
            {
                // 恢复原始共享参数文件路径
                app.SharedParametersFilename = originalFilePath;
                // 清理临时文件
                if (File.Exists(spFilePath)) File.Delete(spFilePath);
            }
        }

        /// <summary>
        /// 构建参数配置 - 使用元组和集合初始化器
        /// </summary>
        private static List<ParameterBindingConfig> BuildParameterConfigurations() => new List<ParameterBindingConfig>
        {
            new ParameterBindingConfig
            {
                Name = "ReadonlyId",
                Type = ParameterType.Text,
                UserModifiable = false,
                Description = "只读实例参数，用于与外部内容协调。",
                IsInstance = true,
                ParameterGroup = BuiltInParameterGroup.PG_IDENTITY_DATA,
                Categories = new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Roofs
                }
            },
            new ParameterBindingConfig
            {
                Name = "ReadonlyCost",
                Type = ParameterType.Currency,
                UserModifiable = false,
                Description = "只读类型参数，用于列出类型的成本。",
                IsInstance = false,
                ParameterGroup = BuiltInParameterGroup.PG_MATERIALS,
                Categories = new[]
                {
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_Planting
                }
            }
        };

        /// <summary>
        /// 创建临时共享参数文件
        /// </summary>
        private static string CreateTempSharedParameterFile()
        {
            const string tempDir = @"c:\tmp\RevitSharedParameters\";
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            var fileName = System.IO.Path.GetRandomFileName();
            var spFile = System.IO.Path.ChangeExtension(fileName, "txt");
            var filePath = System.IO.Path.Combine(tempDir, spFile);

            File.Create(filePath).Close();
            return filePath;
        }
    }

    /// <summary>
    /// 参数绑定配置类 - 使用自动属性
    /// </summary>
    public class ParameterBindingConfig
    {
        public string Name { get; set; }
        public ParameterType Type { get; set; }
        public bool UserModifiable { get; set; } = true;
        public bool UserVisible { get; set; } = true;
        public string Description { get; set; } = "";
        public bool IsInstance { get; set; } = true;
        public BuiltInParameterGroup ParameterGroup { get; set; } = BuiltInParameterGroup.PG_IDENTITY_DATA;
        public IEnumerable<BuiltInCategory> Categories { get; set; }

        public ExternalDefinitionCreationOptions CreateOptions => new ExternalDefinitionCreationOptions(Name, Type)
        {
            UserModifiable = UserModifiable,
            Visible = UserVisible,
            Description = Description
        };

        public void AddBindings(Document doc, Definition definition)
        {
            var categorySet = new CategorySet();
            var categories = doc.Settings.Categories;

            foreach (var bic in Categories)
            {
                categorySet.Insert(categories.get_Item(bic));
            }

            Autodesk.Revit.DB.Binding binding = IsInstance
                ? (Autodesk.Revit.DB.Binding)new InstanceBinding(categorySet)
                : new TypeBinding(categorySet);

            doc.ParameterBindings.Insert(definition, binding, ParameterGroup);
        }
    }
    /// <summary>
    /// 共享参数操作类型枚举
    /// </summary>
    public enum SharedParameterAction
    {
        BindParameters,      // 绑定共享参数到文档
        SetReadonlyCostById, // 基于ID设置只读成本
        SetReadonlyCostByIncrement, // 基于增量设置只读成本
        SetReadonlyIdByUniqueId,    // 基于UniqueId设置只读ID
        SetReadonlyIdByTypeAndId    // 基于类型名+ID设置只读ID
    }

    /// <summary>
    /// 操作项ViewModel - 用于列表显示
    /// </summary>
    public class OperationItemModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public SharedParameterAction Action { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public OperationItemModel(SharedParameterAction action, string displayName, string description)
        {
            Action = action;
            DisplayName = displayName;
            Description = description;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
