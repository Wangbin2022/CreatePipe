using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
//using Excel = Microsoft.Office.Interop.Excel;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// ExportToExcelWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExportToExcelWindow : Window
    {
        public ExportToExcelWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 导出到Excel的主视图模型
    /// </summary>
    public class ExportToExcelViewModel : ObserverableObject
    {
        private readonly RevitDataService _dataService;
        private readonly Document _doc;
        private ObservableCollection<CategoryGroup> _categoryGroups;
        private CategoryGroup _selectedCategory;
        private bool _isExporting;
        private string _exportStatus;

        public ExportToExcelViewModel(ExternalCommandData commandData, Document doc)
        {
            _dataService = new RevitDataService(commandData);
            _doc = doc;

            // 初始化命令
            ExportCommand = new BaseBindingCommand(_ => ExportToExcel(), _ => !IsExporting && CategoryGroups?.Any() == true);
            RefreshCommand = new BaseBindingCommand(_ => LoadData());
            CloseCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            // 加载数据
            LoadData();
        }

        /// <summary>
        /// 类别分组列表
        /// </summary>
        public ObservableCollection<CategoryGroup> CategoryGroups
        {
            get => _categoryGroups;
            set { _categoryGroups = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的类别
        /// </summary>
        public CategoryGroup SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否正在导出
        /// </summary>
        public bool IsExporting
        {
            get => _isExporting;
            set { _isExporting = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 导出状态信息
        /// </summary>
        public string ExportStatus
        {
            get => _exportStatus;
            set { _exportStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 导出命令
        /// </summary>
        public ICommand ExportCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// 关闭窗口的回调
        /// </summary>
        public Action CloseAction { get; set; }

        /// <summary>
        /// 加载Revit数据
        /// </summary>
        private void LoadData()
        {
            try
            {
                ExportStatus = "正在加载数据...";

                var groupedElements = _dataService.GetGroupedElements();
                var groups = new ObservableCollection<CategoryGroup>();

                foreach (var kvp in groupedElements)
                {
                    var categoryName = kvp.Key;
                    var elements = kvp.Value;

                    // 获取该类别的公共属性
                    var commonProps = _dataService.GetCommonProperties(elements);

                    var group = new CategoryGroup
                    {
                        CategoryName = categoryName,
                        CommonProperties = commonProps
                    };

                    // 获取每个构件的参数值
                    foreach (var element in elements)
                    {
                        var paramValues = _dataService.GetParameterValues(element, commonProps);
                        group.Elements.Add(new ElementExportInfo
                        {
                            Id = element.Id.IntegerValue,
                            CategoryName = categoryName,
                            ParameterValues = paramValues
                        });
                    }

                    groups.Add(group);
                }

                CategoryGroups = groups;
                ExportStatus = $"加载完成，共 {groups.Count} 个类别，{groups.Sum(g => g.Elements.Count)} 个构件";
            }
            catch (Exception ex)
            {
                ExportStatus = $"加载失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 导出到Excel
        /// </summary>
        private void ExportToExcel()
        {
            IsExporting = true;
            ExportStatus = "正在导出到Excel...";

            try
            {
                var excelService = new ExcelExportService();
                excelService.ExportToExcel(CategoryGroups, _doc.Title);
                ExportStatus = "导出成功！";
            }
            catch (Exception ex)
            {
                ExportStatus = $"导出失败: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
    /// <summary>
    /// Excel导出服务 - 负责创建Excel文件和写入数据
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        /// <param name="categoryGroups">类别分组数据</param>
        /// <param name="documentTitle">文档标题（用作工作簿名称）</param>
        public void ExportToExcel(ObservableCollection<CategoryGroup> categoryGroups, string documentTitle)
        {
            //// 启动Excel应用程序
            //var excelApp = new Excel.Application();
            //excelApp.Visible = true;
            //// 创建新工作簿
            //var workbook = excelApp.Workbooks.Add();
            //// 删除默认的额外工作表，只保留一个
            //while (workbook.Sheets.Count > 1)
            //{
            //    ((Excel.Worksheet)workbook.Sheets[1]).Delete();
            //}
            //bool isFirstSheet = true;
            //foreach (var group in categoryGroups)
            //{
            //    Excel.Worksheet worksheet;
            //    if (isFirstSheet)
            //    {
            //        // 第一个类别使用当前活动工作表
            //        worksheet = (Excel.Worksheet)excelApp.ActiveSheet;
            //        isFirstSheet = false;
            //    }
            //    else
            //    {
            //        // 后续类别添加新工作表
            //        worksheet = (Excel.Worksheet)workbook.Sheets.Add();
            //    }
            //    // 设置工作表名称（Excel限制31个字符）
            //    var sheetName = group.CategoryName.Length > 31
            //        ? group.CategoryName.Substring(0, 31)
            //        : group.CategoryName;
            //    worksheet.Name = sheetName;
            //    // 写入表头
            //    // 第1列：构件ID
            //    worksheet.Cells[1, 1] = "构件ID";
            //    // 后续列：公共属性名称
            //    for (int i = 0; i < group.CommonProperties.Count; i++)
            //    {
            //        worksheet.Cells[1, i + 2] = group.CommonProperties[i];
            //    }
            //    // 写入数据行
            //    int row = 2;
            //    foreach (var element in group.Elements)
            //    {
            //        // 写入构件ID
            //        worksheet.Cells[row, 1] = element.Id;
            //        // 写入各属性值
            //        for (int col = 0; col < group.CommonProperties.Count; col++)
            //        {
            //            var propName = group.CommonProperties[col];
            //            if (element.ParameterValues.ContainsKey(propName))
            //            {
            //                worksheet.Cells[row, col + 2] = element.ParameterValues[propName];
            //            }
            //        }
            //        row++;
            //    }
            //    // 自动调整列宽
            //    worksheet.Columns.AutoFit();
            //}
            //// 激活第一个工作表
            //((Excel.Worksheet)workbook.Sheets[1]).Activate();
        }
    }
    /// <summary>
    /// Revit数据服务类 - 负责从Revit中提取和处理数据
    /// </summary>
    public class RevitDataService
    {
        private readonly UIApplication _uiApp;
        private readonly Document _doc;

        public RevitDataService(ExternalCommandData commandData)
        {
            _uiApp = commandData.Application;
            _doc = _uiApp.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取按类别分组的非类型构件
        /// </summary>
        public Dictionary<string, List<Element>> GetGroupedElements()
        {
            var groupedElements = new Dictionary<string, List<Element>>();

            // 过滤掉ElementType，只获取实例构件
            var filter = new ElementIsElementTypeFilter(true);
            var collector = new FilteredElementCollector(_doc);
            var elements = collector.WherePasses(filter).ToElements();

            foreach (Element element in elements)
            {
                // 跳过ElementType类型
                if (element is ElementType) continue;

                var category = element.Category;
                if (category == null) continue;

                var categoryName = category.Name;
                if (!groupedElements.ContainsKey(categoryName))
                {
                    groupedElements[categoryName] = new List<Element>();
                }
                groupedElements[categoryName].Add(element);
            }

            return groupedElements;
        }

        /// <summary>
        /// 获取一组构件的公共属性名称
        /// </summary>
        public List<string> GetCommonProperties(List<Element> elements)
        {
            if (!elements.Any()) return new List<string>();

            // 获取第一个有效构件的所有参数名作为初始集合
            var commonProps = new HashSet<string>();
            var firstElementProps = GetParameterNames(elements.First());
            if (!firstElementProps.Any()) return new List<string>();

            foreach (var prop in firstElementProps)
            {
                commonProps.Add(prop);
            }

            // 与后续构件的参数取交集
            foreach (var element in elements.Skip(1))
            {
                var currentProps = GetParameterNames(element);
                commonProps.IntersectWith(currentProps);

                if (!commonProps.Any()) break;
            }

            return commonProps.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// 获取单个构件的所有参数名称
        /// </summary>
        private HashSet<string> GetParameterNames(Element element)
        {
            var paramNames = new HashSet<string>();

            foreach (Parameter param in element.Parameters)
            {
                if (param?.Definition != null)
                {
                    paramNames.Add(param.Definition.Name);
                }
            }

            return paramNames;
        }

        /// <summary>
        /// 获取构件的参数值（仅限指定属性名称）
        /// </summary>
        public Dictionary<string, string> GetParameterValues(Element element, List<string> propertyNames)
        {
            var values = new Dictionary<string, string>();

            foreach (Parameter param in element.Parameters)
            {
                if (param?.Definition == null) continue;

                var paramName = param.Definition.Name;
                if (!propertyNames.Contains(paramName)) continue;

                var value = GetParameterValueAsString(param);
                values[paramName] = value;
            }

            return values;
        }

        /// <summary>
        /// 将参数值转换为字符串
        /// </summary>
        private string GetParameterValueAsString(Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.Double:
                    return param.AsDouble().ToString();

                case StorageType.ElementId:
                    var refElement = _doc.GetElement(param.AsElementId());
                    return refElement?.Name ?? param.AsElementId().IntegerValue.ToString();

                case StorageType.Integer:
                    return param.AsInteger().ToString();

                case StorageType.String:
                    return param.AsString() ?? "";

                default:
                    return "";
            }
        }
    }
    /// <summary>
    /// 构件导出信息模型
    /// </summary>
    public class ElementExportInfo
    {
        /// <summary>
        /// 构件ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 类别名称
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// 参数值字典（参数名 -> 参数值）
        /// </summary>
        public Dictionary<string, string> ParameterValues { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 类别分组信息
    /// </summary>
    public class CategoryGroup
    {
        public string CategoryName { get; set; }
        public List<string> CommonProperties { get; set; } = new List<string>();
        public List<ElementExportInfo> Elements { get; set; } = new List<ElementExportInfo>();
    }
}
