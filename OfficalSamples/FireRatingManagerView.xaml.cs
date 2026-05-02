using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;


namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FireRatingManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class FireRatingManagerView : Window
    {
        public FireRatingManagerView()
        {
            InitializeComponent();
        }
    }
    public class FireRatingManagerViewModel : ObserverableObject
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;
        private readonly SharedParameterService2 _paramService;
        private readonly DoorDataService _doorService;
        //private readonly ExcelService _excelService;

        private bool _isProcessing;
        private string _statusMessage;
        private ObservableCollection<DoorFireRatingData> _doorsData;

        public FireRatingManagerViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _document = uiApp.ActiveUIDocument.Document;
            _paramService = new SharedParameterService2(uiApp.Application);
            _doorService = new DoorDataService(_document);
            //_excelService = new ExcelService();

            _doorsData = new ObservableCollection<DoorFireRatingData>();

            // 初始化命令
            ApplyParameterCommand = new BaseBindingCommand(ExecuteApplyParameter);
            ExportCommand = new BaseBindingCommand(ExecuteExport, _ => !IsProcessing);
            ImportCommand = new BaseBindingCommand(ExecuteImport, _ => !IsProcessing);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
        }

        #region 属性

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ObservableCollection<DoorFireRatingData> DoorsData
        {
            get => _doorsData;
            set
            {
                _doorsData = value;
                OnPropertyChanged(nameof(DoorsData));
            }
        }

        #endregion

        #region 命令

        public ICommand ApplyParameterCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 命令执行

        /// <summary>
        /// 应用共享参数到门类别
        /// </summary>
        private void ExecuteApplyParameter(Object obj)
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "正在应用共享参数...";

                // 获取或创建共享参数文件
                var defFile = _paramService.InitializeSharedParameterFile();
                if (defFile == null)
                {
                    StatusMessage = "无法打开共享参数文件";
                    return;
                }

                // 获取或创建参数组
                var group = _paramService.GetOrCreateGroup(defFile, SharedParameterConfig.GroupName);

                // 获取或创建参数定义
                var definition = _paramService.GetOrCreateDefinition(
                    group,
                    SharedParameterConfig.ParameterName,
                    ParameterType.Integer);

                if (definition == null)
                {
                    StatusMessage = "无法创建参数定义";
                    return;
                }

                // 绑定参数到门类别
                var category = _document.Settings.Categories.get_Item(SharedParameterConfig.CategoryName);
                var categorySet = _uiApp.Application.Create.NewCategorySet();
                categorySet.Insert(category);

                using (var transaction = new Transaction(_document, "应用共享参数"))
                {
                    transaction.Start();

                    var binding = _uiApp.Application.Create.NewInstanceBinding(categorySet);
                    _document.ParameterBindings.Insert(definition, binding);

                    transaction.Commit();
                }

                StatusMessage = "共享参数已成功应用到门类别";
                ExecuteRefresh(null); // 刷新数据
            }
            catch (Exception ex)
            {
                StatusMessage = $"应用参数失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 导出防火等级数据到 Excel
        /// </summary>
        private void ExecuteExport(Object obj)
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "正在导出数据...";

                var paramGuid = _paramService.FindParameterGuid();
                if (paramGuid == Guid.Empty)
                {
                    StatusMessage = "未找到 Fire Rating 参数，请先运行应用参数命令";
                    return;
                }

                var data = _doorService.CollectFireRatingData(paramGuid);
                //_excelService.ExportFireRatingData(data, SharedParameterConfig.ExcelFilePath);

                StatusMessage = $"数据已导出到: {SharedParameterConfig.ExcelFilePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 从 Excel 导入防火等级数据
        /// </summary>
        private void ExecuteImport(Object obj)
        {
            //try
            //{
            //    IsProcessing = true;
            //    StatusMessage = "正在导入数据...";
            //    var paramGuid = _paramService.FindParameterGuid();
            //    if (paramGuid == Guid.Empty)
            //    {
            //        StatusMessage = "未找到 Fire Rating 参数，请先运行应用参数命令";
            //        return;
            //    }
            //    var importedData = _excelService.ImportFireRatingData(SharedParameterConfig.ExcelFilePath);
            //    using (var transaction = new Transaction(_document, "导入防火等级数据"))
            //    {
            //        transaction.Start();
            //        foreach (var item in importedData)
            //        {
            //            var door = _document.GetElement(item.ElementId) as FamilyInstance;
            //            if (door != null)
            //            {
            //                _doorService.SetFireRating(door, paramGuid, item.FireRating);
            //            }
            //        }
            //        transaction.Commit();
            //    }
            //    StatusMessage = $"成功导入 {importedData.Count} 条防火等级数据";
            //    ExecuteRefresh(null); // 刷新显示
            //}
            //catch (Exception ex)
            //{
            //    StatusMessage = $"导入失败: {ex.Message}";
            //}
            //finally
            //{
            //    IsProcessing = false;
            //}
        }

        /// <summary>
        /// 刷新数据显示
        /// </summary>
        private void ExecuteRefresh(Object obj)
        {
            try
            {
                var paramGuid = _paramService.FindParameterGuid();
                if (paramGuid == Guid.Empty)
                {
                    DoorsData.Clear();
                    StatusMessage = "未找到 Fire Rating 参数";
                    return;
                }

                var data = _doorService.CollectFireRatingData(paramGuid);
                DoorsData.Clear();
                foreach (var item in data)
                {
                    DoorsData.Add(item);
                }

                StatusMessage = $"已加载 {DoorsData.Count} 个门";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
            }
        }
        #endregion 
    }
    /// <summary>
    /// 门数据管理服务
    /// </summary>
    public class DoorDataService
    {
        private readonly Document _document;

        public DoorDataService(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// 获取所有门实例
        /// </summary>
        public List<FamilyInstance> GetAllDoors()
        {
            return new FilteredElementCollector(_document)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(e => e.Category?.Name == SharedParameterConfig.CategoryName)
                .ToList();
        }

        ///// <summary>
        ///// 获取门的防火等级参数值
        ///// </summary>
        //public int GetFireRating(FamilyInstance door, Guid paramGuid)
        //{
        //    var param = door.Parameter(paramGuid);
        //    return param?.AsInteger() ?? 0;
        //}
        ///// <summary>
        ///// 设置门的防火等级参数值
        ///// </summary>
        //public void SetFireRating(FamilyInstance door, Guid paramGuid, int value)
        //{
        //    var param = door.Parameter(paramGuid);
        //    param?.Set(value);
        //}
        ///// <summary>
        ///// 获取门的标签（Mark 参数）
        ///// </summary>
        //public string GetDoorMark(FamilyInstance door)
        //{
        //    var param = door.Parameter(BuiltInParameter.ALL_MODEL_MARK);
        //    return param?.AsString();
        //}

        /// <summary>
        /// 获取门的标高名称
        /// </summary>
        public string GetLevelName(FamilyInstance door)
        {
            var level = _document.GetElement(door.LevelId) as Level;
            return level?.Name;
        }

        /// <summary>
        /// 收集所有门的防火等级数据
        /// </summary>
        public List<DoorFireRatingData> CollectFireRatingData(Guid paramGuid)
        {
            var doors = GetAllDoors();
            var result = new List<DoorFireRatingData>();

            //foreach (var door in doors)
            //{
            //    result.Add(new DoorFireRatingData
            //    {
            //        ElementId = door.Id,
            //        IdValue = door.Id.IntegerValue,
            //        LevelName = GetLevelName(door),
            //        Tag = GetDoorMark(door),
            //        FireRating = GetFireRating(door, paramGuid)
            //    });
            //}

            return result;
        }
    }
    /// <summary>
    /// Excel 操作服务
    /// </summary>
    //public class ExcelService
    //{
    //    private Excel.Application _excelApp;
    //    private Excel.Workbook _workbook;
    //    private Excel.Worksheet _worksheet;

    //    /// <summary>
    //    /// 启动 Excel 并创建工作表
    //    /// </summary>
    //    public void StartExcel(string sheetName)
    //    {
    //        _excelApp = new Excel.Application
    //        {
    //            Visible = true
    //        };

    //        _workbook = _excelApp.Workbooks.Add();

    //        // 删除多余的默认工作表，只保留一个
    //        while (_workbook.Sheets.Count > 1)
    //        {
    //            var sheet = _workbook.Sheets[1];
    //            sheet.Delete();
    //        }

    //        _worksheet = _workbook.ActiveSheet;
    //        _worksheet.Name = sheetName;
    //    }

    //    /// <summary>
    //    /// 设置表头
    //    /// </summary>
    //    public void SetHeaders(params string[] headers)
    //    {
    //        for (int i = 0; i < headers.Length; i++)
    //        {
    //            _worksheet.Cells[1, i + 1].Value = headers[i];
    //        }
    //    }

    //    /// <summary>
    //    /// 导出门防火等级数据到 Excel
    //    /// </summary>
    //    public void ExportFireRatingData(IEnumerable<DoorFireRatingData> data, string filePath)
    //    {
    //        StartExcel(SharedParameterConfig.ParameterName);

    //        SetHeaders("ID", "Level", "Tag", SharedParameterConfig.ParameterName);

    //        int row = 2;
    //        foreach (var item in data)
    //        {
    //            _worksheet.Cells[row, 1].Value = item.IdValue;
    //            _worksheet.Cells[row, 2].Value = item.LevelName ?? string.Empty;
    //            _worksheet.Cells[row, 3].Value = item.Tag ?? string.Empty;
    //            _worksheet.Cells[row, 4].Value = item.FireRating;
    //            row++;
    //        }

    //        _worksheet.SaveAs(filePath);
    //    }

    //    /// <summary>
    //    /// 从 Excel 导入防火等级数据
    //    /// </summary>
    //    public List<DoorFireRatingData> ImportFireRatingData(string filePath)
    //    {
    //        var result = new List<DoorFireRatingData>();

    //        var excelApp = new Excel.Application
    //        {
    //            Visible = true
    //        };

    //        var workbook = excelApp.Workbooks.Open(filePath);
    //        var worksheet = workbook.ActiveSheet;

    //        int row = 2;
    //        while (true)
    //        {
    //            var idValue = worksheet.Cells[row, 1].Value2?.ToString();
    //            if (string.IsNullOrEmpty(idValue)) break;

    //            var fireRatingValue = worksheet.Cells[row, 4].Value2?.ToString();

    //            if (int.TryParse(idValue, out var id) && int.TryParse(fireRatingValue, out var rating))
    //            {
    //                result.Add(new DoorFireRatingData
    //                {
    //                    IdValue = id,
    //                    ElementId = new ElementId(id),
    //                    FireRating = rating
    //                });
    //            }

    //            row++;
    //        }

    //        workbook.Close(false);
    //        excelApp.Quit();

    //        return result;
    //    }

    //    /// <summary>
    //    /// 关闭 Excel
    //    /// </summary>
    //    public void Close()
    //    {
    //        _workbook?.Close(false);
    //        _excelApp?.Quit();
    //    }
    //}
    /// <summary>
    /// 门防火等级数据模型
    /// </summary>
    public class DoorFireRatingData
    {
        public ElementId ElementId { get; set; }
        public int IdValue { get; set; }
        public string LevelName { get; set; }
        public string Tag { get; set; }
        public int FireRating { get; set; }

        // 用于 Excel 导入/导出的字符串表示
        public override string ToString() => $"{IdValue}, {LevelName}, {Tag}, {FireRating}";
    }
    /// <summary>
    /// 共享参数管理服务
    /// </summary>
    public class SharedParameterService2
    {
        private readonly Autodesk.Revit.ApplicationServices.Application _app;

        public SharedParameterService2(Autodesk.Revit.ApplicationServices.Application app)
        {
            _app = app;
        }

        /// <summary>
        /// 初始化共享参数文件
        /// </summary>
        public DefinitionFile InitializeSharedParameterFile()
        {
            var filePath = SharedParameterConfig.SharedParamFilePath;

            // 确保文件存在
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            // 设置共享参数文件路径
            _app.SharedParametersFilename = filePath;
            return _app.OpenSharedParameterFile();
        }

        /// <summary>
        /// 获取或创建参数组
        /// </summary>
        public DefinitionGroup GetOrCreateGroup(DefinitionFile defFile, string groupName)
        {
            var group = defFile.Groups.get_Item(groupName);
            return group ?? defFile.Groups.Create(groupName);
        }

        /// <summary>
        /// 获取或创建参数定义
        /// </summary>
        public ExternalDefinition GetOrCreateDefinition(DefinitionGroup group, string paramName, ParameterType paramType)
        {
            var definition = group.Definitions.get_Item(paramName);

            if (definition == null)
            {
                var options = new ExternalDefinitionCreationOptions(paramName, paramType);
                definition = group.Definitions.Create(options);
            }

            return definition as ExternalDefinition;
        }

        /// <summary>
        /// 查找参数的 GUID
        /// </summary>
        public Guid FindParameterGuid()
        {
            var defFile = InitializeSharedParameterFile();
            if (defFile == null) return Guid.Empty;

            var group = defFile.Groups.get_Item(SharedParameterConfig.GroupName);
            if (group == null) return Guid.Empty;

            var definition = group.Definitions.get_Item(SharedParameterConfig.ParameterName);
            return (definition as ExternalDefinition)?.GUID ?? Guid.Empty;
        }
    }
    /// <summary>
    /// 共享参数配置
    /// </summary>
    public static class SharedParameterConfig
    {
        // 文件路径配置（相对于程序集所在目录）
        public static string BasePath => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string SharedParamFileName => "FireRating.txt";
        public static string ExcelFileName => "FireRating.xls";

        public static string SharedParamFilePath => Path.Combine(BasePath, SharedParamFileName);
        public static string ExcelFilePath => Path.Combine(BasePath, ExcelFileName);

        // 参数定义
        public const string GroupName = "Fire";
        public const string ParameterName = "Fire Rating";
        public const string CategoryName = "Doors";
    }
}
