using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// MEPSlopeCheckView.xaml 的交互逻辑
    /// </summary>
    public partial class MEPSlopeCheckView : Window
    {
        public MEPSlopeCheckView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new MEPSlopeCheckViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class MEPSlopeCheckViewModel : ObserverableObject
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Document _doc;
        public MEPSlopeCheckViewModel(UIApplication uiApp)
        {
            _uiapp = uiApp;
            _uidoc = uiApp.ActiveUIDocument;
            _doc = _uidoc.Document;
            // 初始化下拉列表
            Items = new ObservableCollection<string> { "管道", "风管", "桥架" };
            SelectedItems = new ObservableCollection<string>();
            // 默认值
            DefaultSymbol = "大于";
            OptionSymbol = "小于";
            DefaultNum = "1%";
            OptionNum = "2%";
        }
        public ICommand SaveConfigCommand => new BaseBindingCommand(SaveConfig);
        private void SaveConfig(object obj)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON 配置文件 (*.json)|*.json",
                    Title = "保存坡度检查条件",
                    FileName = "坡度检查配置.json"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    // 1. 组装要保存的配置对象
                    var config = new SlopeCheckConfig
                    {
                        SelectedCategories = SelectedItems?.ToList() ?? new List<string>(),
                        DefaultSymbol = this.DefaultSymbol ?? "",
                        DefaultNum = this.DefaultNum ?? "",
                        IsOptionChecked = this.IsOptionChecked,
                        OptionSymbol = this.OptionSymbol ?? "",
                        OptionNum = this.OptionNum ?? ""
                    };
                    // 2. 使用 JsonHelper 写入文件
                    JsonHelper.SaveToFile(saveFileDialog.FileName, config);
                    TaskDialog.Show("成功", "搜索条件保存成功！\n文件路径：" + saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", "保存 JSON 失败。\n详情：" + ex.Message);
            }
        }
        //private void SaveConfigCsv(object obj)
        //{
        //    try
        //    {
        //        SaveFileDialog saveFileDialog = new SaveFileDialog
        //        {
        //            Filter = "CSV 配置文件 (*.csv)|*.csv",
        //            Title = "保存坡度检查条件",
        //            FileName = "坡度检查配置.csv"
        //        };
        //        if (saveFileDialog.ShowDialog() == true)
        //        {
        //            string filePath = saveFileDialog.FileName;
        //            CsvHelper csv = new CsvHelper(filePath);
        //            // 定义表头
        //            string[] headers = { "检查类别", "主条件符号", "主条件数值", "启用附加条件", "附加条件符号", "附加条件数值" };
        //            // 由于 SelectedItems 是一个集合，我们在 CSV 中用 "|" 将它们拼接成一个字符串
        //            string categoriesStr = SelectedItems != null ? string.Join("|", SelectedItems) : "";
        //            // 收集当前行数据
        //            string[] row = new string[]
        //            {
        //                categoriesStr,
        //                DefaultSymbol ?? "",
        //                DefaultNum ?? "",
        //                IsOptionChecked.ToString(),
        //                OptionSymbol ?? "",
        //                OptionNum ?? ""
        //            };
        //            // 使用 CsvHelper 写入带表头的数据（会覆盖原有文件）
        //            csv.WriteAllWithHeaders(headers, new List<string[]> { row });
        //            //TaskDialog.Show("成功", "搜索条件保存成功！\n文件路径：" + filePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("错误", "保存失败，文件可能正被其他程序锁定。\n详情：" + ex.Message);
        //    }
        //}
        public ICommand LoadConfigCommand => new BaseBindingCommand(LoadConfig);
        private void LoadConfig(object obj)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON 配置文件 (*.json)|*.json",
                    Title = "加载坡度检查条件"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    // 1. 使用 JsonHelper 读取并反序列化
                    var config = JsonHelper.LoadFromFile<SlopeCheckConfig>(openFileDialog.FileName);
                    if (config != null)
                    {
                        // 2. 恢复选中类别
                        SelectedItems.Clear();
                        if (config.SelectedCategories != null)
                        {
                            foreach (var cat in config.SelectedCategories)
                            {
                                SelectedItems.Add(cat);
                            }
                        }
                        // 3. 恢复主条件和附加条件
                        this.DefaultSymbol = config.DefaultSymbol;
                        this.DefaultNum = config.DefaultNum;
                        this.IsOptionChecked = config.IsOptionChecked;
                        this.OptionSymbol = config.OptionSymbol;
                        this.OptionNum = config.OptionNum;
                        TaskDialog.Show("成功", "搜索条件加载成功！");
                    }
                    else
                    {
                        TaskDialog.Show("提示", "读取到的 JSON 文件为空或格式不正确。");
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", "加载 JSON 失败，请检查文件格式。\n详情：" + ex.Message);
            }
        }
        //private void LoadConfigCsv(object obj)
        //{
        //    try
        //    {
        //        OpenFileDialog openFileDialog = new OpenFileDialog
        //        {
        //            Filter = "CSV 配置文件 (*.csv)|*.csv",
        //            Title = "加载坡度检查条件"
        //        };
        //        if (openFileDialog.ShowDialog() == true)
        //        {
        //            string filePath = openFileDialog.FileName;
        //            CsvHelper csv = new CsvHelper(filePath);
        //            // 以字典形式读取数据（自动匹配表头）
        //            var data = csv.ReadAllWithHeaders();
        //            if (data.Count > 0)
        //            {
        //                var dict = data[0]; // 我们只读取第一行配置数据
        //                // 1. 恢复选中类别
        //                if (dict.TryGetValue("检查类别", out string catStr))
        //                {
        //                    SelectedItems.Clear();
        //                    if (!string.IsNullOrEmpty(catStr))
        //                    {
        //                        string[] cats = catStr.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        //                        foreach (var c in cats)
        //                        {
        //                            SelectedItems.Add(c);
        //                        }
        //                    }
        //                }
        //                // 2. 恢复主条件
        //                if (dict.TryGetValue("主条件符号", out string defaultSym)) DefaultSymbol = defaultSym;
        //                if (dict.TryGetValue("主条件数值", out string defaultNum)) DefaultNum = defaultNum;
        //                // 3. 恢复附加条件
        //                if (dict.TryGetValue("启用附加条件", out string isOptStr) && bool.TryParse(isOptStr, out bool isChecked))
        //                {
        //                    IsOptionChecked = isChecked;
        //                }
        //                if (dict.TryGetValue("附加条件符号", out string optSym)) OptionSymbol = optSym;
        //                if (dict.TryGetValue("附加条件数值", out string optNum)) OptionNum = optNum;
        //                TaskDialog.Show("成功", "搜索条件加载成功！");
        //            }
        //            else
        //            {
        //                TaskDialog.Show("提示", "选中的 CSV 文件中没有数据。");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("错误", "加载失败，请检查文件格式或是否被占用。\n详情：" + ex.Message);
        //    }
        //}
        public ICommand DrawTableCommand => new BaseBindingCommand(ExecuteCheck);
        private void ExecuteCheck(object obj)
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
            {
                TaskDialog.Show("提示", "请选择至少一种检查范围（管道/风管/桥架）");
                return;
            }
            // 1. 解析数字并校验合法性
            if (!TryParseInput(DefaultNum, out double val1)) return;
            double val2 = 0;
            if (IsOptionChecked)
            {
                if (!TryParseInput(OptionNum, out val2)) return;

                // 2. 检查条件是否冲突
                if (IsLogicConflict(DefaultSymbol, val1, OptionSymbol, val2))
                {
                    TaskDialog.Show("错误", "主副条件存在逻辑矛盾，找不到任何符合该区间的值！");
                    return;
                }
            }
            // 3. 执行 Revit 模型筛选
            List<ElementId> matchIds = new List<ElementId>();
            List<Type> targetTypes = new List<Type>();
            if (SelectedItems.Contains("管道")) targetTypes.Add(typeof(Pipe));
            if (SelectedItems.Contains("风管")) targetTypes.Add(typeof(Duct));
            if (SelectedItems.Contains("桥架")) targetTypes.Add(typeof(CableTray));
            // 构建多重类别过滤器
            ElementMulticlassFilter filter = new ElementMulticlassFilter(targetTypes);
            var collector = new FilteredElementCollector(_doc).WherePasses(filter).WhereElementIsNotElementType();
            // 3. 遍历并使用 Helper 进行检查
            foreach (Element elem in collector)
            {
                // 将元素安全转换为 MEPCurve
                if (elem is MEPCurve mepCurve)
                {
                    // 使用 Helper 检查主条件
                    bool pass = MEPSlopeHelper.CheckCondition(mepCurve, DefaultSymbol, val1);

                    // 使用 Helper 检查附加条件
                    if (IsOptionChecked && pass)
                    {
                        pass = MEPSlopeHelper.CheckCondition(mepCurve, OptionSymbol, val2);
                    }

                    if (pass)
                    {
                        matchIds.Add(elem.Id);
                    }
                }
            }
            // 4. 选中并报告
            if (matchIds.Count > 0)
            {
                _uidoc.Selection.SetElementIds(matchIds);
                TaskDialog.Show("完成", $"检查完成！共找到 {matchIds.Count} 个符合坡度条件的构件，已在模型中高亮选中。");
            }
            else
            {
                _uidoc.Selection.SetElementIds(new List<ElementId>()); // 清空选择
                TaskDialog.Show("完成", "检查完成。未找到任何符合条件的构件。");
            }
        }
        #region 辅助方法：解析与计算

        // 解析输入字符串：1%, 0度, 0.05
        private bool TryParseInput(string input, out double result)
        {
            result = 0;
            input = input?.Trim().Replace("％", "%");
            if (string.IsNullOrEmpty(input)) return false;

            try
            {
                if (input.EndsWith("%"))
                {
                    result = double.Parse(input.TrimEnd('%')) / 100.0;
                }
                else if (input.EndsWith("度"))
                {
                    double degree = double.Parse(input.TrimEnd('度'));
                    // Revit 坡度本质是正切值，但针对用户习惯，可将度数转为弧度再算 tan。90度特殊处理。
                    if (degree == 90) result = double.MaxValue;
                    else result = Math.Tan(degree * Math.PI / 180.0);
                }
                else
                {
                    double degree = double.Parse(input);
                    if (degree == 90) result = double.MaxValue;
                    else result = Math.Tan(degree * Math.PI / 180.0);
                }
                if (result < 0)
                {
                    TaskDialog.Show("错误", "输入值不能为负数！");
                    return false;
                }
                return true;
            }
            catch
            {
                TaskDialog.Show("错误", $"数值格式输入错误: {input}\n只能包含数字、%或度。");
                return false;
            }
        }
        /// <summary>
        /// 检查主副条件是否存在数学逻辑上的绝对矛盾（即无解状态）
        /// </summary>
        private bool IsLogicConflict(string sym1, double v1, string sym2, double v2)
        {
            double tol = 0.0001; // 容差，消除浮点数计算误差
            bool isSameVal = Math.Abs(v1 - v2) < tol; // 判断两数是否相等

            // 类别一：当第一个条件是“等于”时
            if (sym1 == "等于")
            {
                if (sym2 == "等于" && !isSameVal) return true;          // 矛盾：=1 且 =2
                if (sym2 == "不等于" && isSameVal) return true;         // 矛盾：=1 且 !=1
                if (sym2 == "大于" && v1 < v2 + tol) return true;       // 矛盾：=1 且 >2 (或 >1)
                if (sym2 == "小于" && v1 > v2 - tol) return true;       // 矛盾：=2 且 <1 (或 <2)
            }
            // 类别二：当第二个条件是“等于”时
            else if (sym2 == "等于")
            {
                if (sym1 == "不等于" && isSameVal) return true;         // 矛盾：!=1 且 =1
                if (sym1 == "大于" && v2 < v1 + tol) return true;       // 矛盾：>2 且 =1 (或 >1 且 =1)
                if (sym1 == "小于" && v2 > v1 - tol) return true;       // 矛盾：<1 且 =2 (或 <2 且 =2)
            }
            // 类别三：大于和小于构成的“死胡同区间”
            else
            {
                // 矛盾：> v1 且 < v2，但 v1 >= v2 (例如 > 2 且 < 1，或者 > 1 且 < 1)
                if (sym1 == "大于" && sym2 == "小于" && v1 > v2 - tol) return true;

                // 矛盾：< v1 且 > v2，但 v1 <= v2 (例如 < 1 且 > 2，或者 < 1 且 > 1)
                if (sym1 == "小于" && sym2 == "大于" && v1 < v2 + tol) return true;
            }
            // 除了上述情况外，其他的组合必定存在有效交集（即能够找到同时满足条件的坡度）
            // 例如：>1 且 >2 (交集为 >2)，!=1 且 !=2 (除了1和2之外的所有数)
            return false;
        }
        #endregion

        #region 绑定的属性
        public ObservableCollection<string> Items { get; set; }
        public ObservableCollection<string> SelectedItems { get; set; }
        private bool _isOptionChecked;
        public bool IsOptionChecked { get => _isOptionChecked; set { _isOptionChecked = value; OnPropertyChanged(); } }
        private string _defaultSymbol;
        public string DefaultSymbol { get => _defaultSymbol; set { _defaultSymbol = value; OnPropertyChanged(); } }
        private string _optionSymbol;
        public string OptionSymbol { get => _optionSymbol; set { _optionSymbol = value; OnPropertyChanged(); } }
        private string _defaultNum;
        public string DefaultNum { get => _defaultNum; set { _defaultNum = value; OnPropertyChanged(); } }
        private string _optionNum;
        public string OptionNum { get => _optionNum; set { _optionNum = value; OnPropertyChanged(); } }
        #endregion    
    }
    /// <summary>
    /// 坡度检查配置数据模型
    /// </summary>
    public class SlopeCheckConfig
    {
        public List<string> SelectedCategories { get; set; } = new List<string>();
        public string DefaultSymbol { get; set; }
        public string DefaultNum { get; set; }
        public bool IsOptionChecked { get; set; }
        public string OptionSymbol { get; set; }
        public string OptionNum { get; set; }
    }
}
