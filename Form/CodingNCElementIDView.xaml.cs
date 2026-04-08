using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// CodingNCElementIDView.xaml 的交互逻辑
    /// </summary>
    public partial class CodingNCElementIDView : Window
    {
        public CodingNCElementIDView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new CodingNCElementIDViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class CodingNCElementIDViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIApplication UiApp { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public CodingNCElementIDViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            UiApp = application;
            LoadData(false); // 初始加载全部
        }
        private ObservableCollection<NCCodingEntity> _entities = new ObservableCollection<NCCodingEntity>();
        public ObservableCollection<NCCodingEntity> Entities
        {
            get => _entities;
            set => SetProperty(ref _entities, value);
        }
        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set => SetProperty(ref _keyword, value);
        }
        private bool _canCoding = false;
        public bool CanCoding
        {
            get => _canCoding;
            set => SetProperty(ref _canCoding, value);
        }
        // ==== 命令绑定 ====
        public ICommand QueryElementCommand => new RelayCommand<string>(k => LoadData(false, k));
        public ICommand HideElementCommand => new BaseBindingCommand(obj => LoadData(true, Keyword));
        public ICommand SelectElementsCommand => new RelayCommand<NCCodingEntity>(SelectElements);
        public ICommand CodeElementsCommand => new RelayCommand<NCCodingEntity>(CodeElements);
        public ICommand ExportCsvCommand => new BaseBindingCommand(ExportCsv);
        // ==== 核心业务逻辑 ====
        /// <summary>
        /// 统一的数据加载与过滤方法 (核心优化点)
        /// </summary>
        private void LoadData(bool onlyShowNonCompliant, string searchKeyword = null)
        {
            _externalHandler.Run(app =>
            {
                // 1. 一次性获取所有构件，并按 Family 进行分组 (性能提升百倍)
                var allInstances = new FilteredElementCollector(Document)
                .OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                    .Where(fi => fi.Symbol?.Family != null)
                    .GroupBy(fi => fi.Symbol.Family, new FamilyComparer()).ToList();

                Entities.Clear();
                bool hasAnyCanCode = false;
                // 2. 遍历分组生成数据
                foreach (var group in allInstances)
                {
                    Family family = group.Key;
                    // 关键字过滤
                    if (!string.IsNullOrEmpty(searchKeyword) &&
                        family.Name.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    // 实例化包装类
                    var entity = new NCCodingEntity(family, group.ToList());
                    // “隐藏已赋码”逻辑
                    if (onlyShowNonCompliant && entity.IsCompliant)
                    {
                        continue;
                    }
                    if (entity.CanCode) hasAnyCanCode = true;
                    Entities.Add(entity);
                }
                CanCoding = hasAnyCanCode;
            });
        }
        private void CodeElements(NCCodingEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ProjectId))
            {
                TaskDialog.Show("提示", "请输入要赋予的族ID！");
                return;
            }
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "批量修改族ID",() =>
                {
                    foreach (var item in entity.FamilyCollection)
                    {
                        Parameter para = item.LookupParameter("族ID");
                        if (para != null && !para.IsReadOnly)
                        {
                            para.Set(entity.ProjectId);
                        }
                    }
                });
                // 修改后刷新当前视图（保持当前的过滤状态）
                LoadData(false, Keyword);
            });
        }
        private void SelectElements(NCCodingEntity entity)
        {
            if (entity == null || !entity.FamilyCollection.Any()) return;
            var selectedIds = entity.FamilyCollection.Select(x => x.Id).ToList();
            UiApp.ActiveUIDocument.Selection.SetElementIds(selectedIds);
        }
        private void ExportCsv(object obj)
        {
            if (!Entities.Any())
            {
                TaskDialog.Show("提示", "没有可导出的数据！");
                return;
            }
            UniversalNewString subView = new UniversalNewString("提示：输入主文件名，默认在桌面");
            if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            {
                return;
            }
            string csvPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), vm.NewName + ".csv");
            try
            {
                // 1. 实例化 CsvHelper
                CsvHelper csvHelper = new CsvHelper(csvPath);
                // 2. 准备表头集合
                var headers = new List<string> { "族名称", "族ID" };
                // 3. 准备数据行集合 (通过 LINQ 将 ViewModel 集合转换为二维列表)
                var rows = Entities.Select(entity => new List<string>
                {entity.FamilyName ?? string.Empty,entity.ProjectId ?? string.Empty});
                // 4. 调用 WriteAllWithHeaders (此方法会自动覆盖旧文件并写入新内容)
                csvHelper.WriteAllWithHeaders(headers, rows);
                TaskDialog.Show("成功", $"已成功导出至:\n{csvPath}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"保存CSV文件时发生错误: {ex.Message}");
            }
        }
        //private void ExportCsv(object obj)
        //{
        //    if (!Entities.Any())
        //    {
        //        TaskDialog.Show("提示", "没有可导出的数据！");
        //        return;
        //    }
        //    UniversalNewString subView = new UniversalNewString("提示：输入主文件名，默认在桌面");
        //    if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
        //    {
        //        return;
        //    }
        //    string csvPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), vm.NewName + ".csv");
        //    try
        //    {
        //        using (StreamWriter writer = new StreamWriter(csvPath, false, Encoding.UTF8))
        //        {
        //            // 建议加入表头
        //            writer.WriteLine("族名称,族ID");
        //            foreach (var revitUnit in Entities)
        //            {
        //                writer.WriteLine($"{revitUnit.FamilyName},{revitUnit.ProjectId}");
        //            }
        //        }
        //        TaskDialog.Show("成功", $"已成功导出至:\n{csvPath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("错误", $"保存CSV文件时发生错误: {ex.Message}");
        //    }
        //}
        private class FamilyComparer : IEqualityComparer<Family>
        {
            public bool Equals(Family x, Family y) => x?.Id == y?.Id; // 使用Id比较比Name更可靠且高效
            public int GetHashCode(Family obj) => obj.Id.IntegerValue;
        }
    }

    public class NCCodingEntity : ObserverableObject
    {
        public Document Document { get; private set; }
        // 双向绑定属性，必须触发通知
        private string _projectId;
        public string ProjectId
        {
            get => _projectId;
            set => SetProperty(ref _projectId, value);
        }
        private bool _isCompliant = true;
        public bool IsCompliant
        {
            get => _isCompliant;
            set => SetProperty(ref _isCompliant, value);
        }
        public bool CanCode { get; set; } = false;
        public List<FamilyInstance> FamilyCollection { get; set; }
        public int FamilyCount => FamilyCollection.Count;
        public int CompliantFamilyCount { get; private set; } = 0;
        public ElementId CategoryId { get; private set; }
        public string CategoryName { get; private set; }
        public string FamilyName { get; private set; }
        // 构造函数：直接接收分类好的实例集合，不再去查文档
        public NCCodingEntity(Family family, List<FamilyInstance> instances)
        {
            if (family == null || instances == null || !instances.Any()) return;
            Document = family.Document;
            FamilyName = family.Name;
            FamilyCollection = instances;
            // 获取类别 (更安全、更直接的方法)
            Category category = family.FamilyCategory ?? instances.First().Category;
            CategoryId = category?.Id ?? ElementId.InvalidElementId;
            CategoryName = category?.Name ?? "未知类别";
            // 统计合规情况
            foreach (var item in instances)
            {
                Parameter parameter = item.LookupParameter("族ID");
                string paramValue = parameter?.AsString();
                if (string.IsNullOrWhiteSpace(paramValue))
                {
                    IsCompliant = false;
                    CanCode = true;
                }
                else
                {
                    ProjectId = paramValue; // 如果有值，提取出来（假设同族赋予相同的ID）
                    CompliantFamilyCount++;
                }
            }
        }
    }


}
