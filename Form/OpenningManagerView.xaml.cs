using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

namespace CreatePipe.Form
{
    /// <summary>
    /// OpenningManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class OpenningManagerView : Window
    {
        public OpenningManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new OpenningManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class OpenningManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public View ActiveView { get; set; }
        public UIApplication uIApp { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public OpenningManagerViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            uIDoc = application.ActiveUIDocument;
            ActiveView = application.ActiveUIDocument.ActiveView;
            uIApp = application;
            QueryElement(null);
        }
        public ICommand MakeElevDetailCommand => new BaseBindingCommand(MakeElevDetail);
        private void MakeElevDetail(object obj)
        {
            if (ActiveView.ViewType != ViewType.Legend && ActiveView.ViewType != ViewType.DraftingView)
            {
                TaskDialog.Show("tt", "请转至图例或详图视图操作");
                return;
            }
            //按门，窗分别统计立面总数，合并同类的，要新建字典属性
            List<OpenningEntity> DoorEntitys = new List<OpenningEntity>();
            List<OpenningEntity> WindowEntitys = new List<OpenningEntity>();
            foreach (var item in AllOpens)
            {
                if (item.entityCategoty == "M")
                {
                    DoorEntitys.Add(item);
                }
                else WindowEntitys.Add(item);
            }
            AnalyzeOpeningDimensions(DoorEntitys);
        }
        public static void AnalyzeOpeningDimensions(List<OpenningEntity> allEntities)
        {
            //// 1. 按门窗分类
            var doorEntities = allEntities.Where(e => e.entityCategoty == "M").ToList();
            //var windowEntities = allEntities.Where(e => e.entityCategoty == "C").ToList();
            //// 2. 创建尺寸字典（Key: (width,height), Value: List<SymbolInfo>）
            var doorSizeDict = new Dictionary<(double width, double height), List<(ElementId id, string name)>>(new SizeComparer());
            //var windowSizeDict = new Dictionary<(double width, double height), List<(ElementId id, string name)>>(new SizeComparer());
            //// 3. 填充门尺寸字典
            foreach (var door in doorEntities)
            {
                var sizeKey = (door.entityWidth, door.entityHeight);
                var symbolInfo = (door.entityId, door.Symbol.Name);

                if (doorSizeDict.ContainsKey(sizeKey))
                {
                    // 避免重复添加相同Symbol
                    if (!doorSizeDict[sizeKey].Any(x => x.id == door.entityId))
                    {
                        doorSizeDict[sizeKey].Add(symbolInfo);
                    }
                }
                else
                {
                    doorSizeDict[sizeKey] = new List<(ElementId id, string name)> { symbolInfo };
                }
            }
            //// 4. 填充窗尺寸字典
            //foreach (var window in windowEntities)
            //{
            //    var sizeKey = (window.entityWidth, window.entityHeight);
            //    var symbolInfo = (window.entityId, window.Symbol.Name);
            //    if (windowSizeDict.ContainsKey(sizeKey))
            //    {
            //        if (!windowSizeDict[sizeKey].Any(x => x.id == window.entityId))
            //        {
            //            windowSizeDict[sizeKey].Add(symbolInfo);
            //        }
            //    }
            //    else
            //    {
            //        windowSizeDict[sizeKey] = new List<(ElementId id, string name)> { symbolInfo };
            //    }
            //}
            // 5. 创建最终输出字典（Key: 尺寸描述, Value: Symbol ID列表）
            var doorResult = new Dictionary<string, List<ElementId>>();
            //var windowResult = new Dictionary<string, List<ElementId>>();
            // 处理门数据
            foreach (var kvp in doorSizeDict)
            {
                var sizeDesc = $"{kvp.Key.width}×{kvp.Key.height}";
                var names = string.Join(", ", kvp.Value.Select(x => x.name));
                var combinedKey = $"{sizeDesc} ({names})";

                doorResult[combinedKey] = kvp.Value.Select(x => x.id).ToList();
            }


            //// 处理窗数据
            //foreach (var kvp in windowSizeDict)
            //{
            //    var sizeDesc = $"{kvp.Key.width}×{kvp.Key.height}";
            //    var names = string.Join(", ", kvp.Value.Select(x => x.name));
            //    var combinedKey = $"{sizeDesc} ({names})";
            //    windowResult[combinedKey] = kvp.Value.Select(x => x.id).ToList();
            //}
            // 6. 导出到CSV
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // 导出门数据
            if (doorResult.Count > 0)
            {
                string doorPath = System.IO.Path.Combine(desktopPath, $"Door_Dimensions_{timestamp}.csv");
                ExportToCsv(doorPath, doorResult, "门");
            }
            //// 导出窗数据
            //if (windowResult.Count > 0)
            //{
            //    string windowPath = Path.Combine(desktopPath, $"Window_Dimensions_{timestamp}.csv");
            //    ExportToCsv(windowPath, windowResult, "窗");
            //}
        }
        private static void ExportToCsv(string filePath, Dictionary<string, List<ElementId>> data, string type)
        {
            var csv = new StringBuilder();
            // 添加 CSV 标题行
            csv.AppendLine("类型,尺寸描述,同尺寸Symbol数量,Symbol ID列表");
            // 添加数据行
            foreach (var kvp in data)
            {
                csv.AppendLine($"\"{type}\",\"{kvp.Key}\",{kvp.Value.Count},\"{string.Join("|", kvp.Value)}\"");
            }
            // 确保目录存在
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            // 写入文件（如果文件存在则覆盖）
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
            TaskDialog.Show("导出完成", $"已导出到: {filePath}");
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
        private void DeleteELements(IEnumerable<object> selectedElements)
        {
            _externalHandler.Run(app =>
            {
                Document.NewTransaction(() =>
                {
                    var elementIds = selectedElements?.Cast<OpenningEntity>()
                        .SelectMany(item => item.InstanceIds).ToList();
                    if (elementIds?.Count > 0)
                    {
                        Document.Delete(elementIds);
                    }
                }, "删除多个门窗实体");
                QueryElement(null);
            });
        }
        public ICommand OpenFamilyCommand => new RelayCommand<OpenningEntity>(OpenFamily);
        private void OpenFamily(OpenningEntity entity)
        {
            if (entity == null) return;
            ElementId familySymbolId = entity.entityId;
            Family family = ((FamilySymbol)Document.GetElement(familySymbolId)).Family;
            string tempDir = @"C:\temp";
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            // 生成唯一文件名
            string familyName = family.Name;
            string tempFilePath = System.IO.Path.Combine(tempDir, $"{familyName}.rfa");
            // 处理文件名冲突
            int counter = 1;
            while (File.Exists(tempFilePath))
            {
                tempFilePath = System.IO.Path.Combine(tempDir, $"{familyName}_{counter}.rfa");
                counter++;
            }
            // 保存族到临时文件
            Document familyDoc = Document.EditFamily(family);
            familyDoc.SaveAs(tempFilePath);
            uIApp.OpenAndActivateDocument(tempFilePath);
            TaskDialog.Show("成功", $"已打开族：{family.Name}");
        }
        public ICommand TagRenameCommand => new RelayCommand<OpenningEntity>(TagRename);
        private void TagRename(OpenningEntity entity)
        {
            if (entity == null) return;
            _externalHandler.Run(app =>
            {
                Document.NewTransaction(() =>
                {
                    UniversalNewString subView = new UniversalNewString($"提示：输入新号，原编号{entity.entityTagName}");
                    if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
                    {
                        return;
                    }
                    try
                    {
                        FamilySymbol symbol = Document.GetElement(entity.entityId) as FamilySymbol;
                        symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID).Set(vm.NewName);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("tt", $"发生错误: {ex.Message}");
                    }
                }, "修改门窗类别编号");
                QueryElement(null);
            });
        }
        public ICommand PickOpenCommand => new RelayCommand<OpenningEntity>(PickOpen);
        private void PickOpen(OpenningEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                if (ActiveView.GetType().Name != "ViewPlan")
                {
                    TaskDialog.Show("tt", "请在平面操作本功能");
                    return;
                }
                Level currentLevel = ActiveView.GenLevel;
                if (currentLevel == null) return;
                string currentLevelName = currentLevel.Name;
                // 获取当前楼层对应的所有实例ID
                // 直接筛选当前楼层的实例
                var currentLevelInstances = entity.Instances
                    .Where(instance =>
                    {
                        Level instanceLevel = Document.GetElement(instance.LevelId) as Level;
                        return instanceLevel?.Name == currentLevelName;
                    })
                    .Select(instance => instance.Id)
                    .ToList();
                select.SetElementIds(currentLevelInstances);
                TaskDialog.Show("tt", $"选中{currentLevelInstances.Count().ToString()}个对象");
            });
        }
        public ICommand SubViewCommand => new RelayCommand<OpenningEntity>(SubView);
        private static void SubView(OpenningEntity entity)
        {
            if (entity == null) return;
            OpenningManagerSubView subView = new OpenningManagerSubView(entity);
            subView.ShowDialog();
            //刷新？
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        private void QueryElement(string obj)
        {
            _externalHandler.Run(app =>
            {
                AllOpens.Clear();
                // 1. 获取所有门窗符号（优化：直接过滤 OST_Doors 和 OST_Windows）
                var doorWindowSymbols = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .Where(s => s.Category != null &&
                           (s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors ||
                            s.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows))
                .ToList();

                // 2. 获取所有门窗实例，并按 Symbol.Id 分组（避免重复查询）
                var allInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                    .Where(fi => doorWindowSymbols.Any(s => s.Id == fi.Symbol.Id))
                    .GroupBy(fi => fi.Symbol.Id)
                    .ToDictionary(g => g.Key, g => g.ToList());
                // 3. 填充 allOpens（仅遍历有效的符号）
                foreach (var symbol in doorWindowSymbols)
                {
                    string tagName = symbol.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID).AsString();
                    List<FamilyInstance> instances = allInstances.TryGetValue(symbol.Id, out var list) ? list : new List<FamilyInstance>();
                    if (string.IsNullOrEmpty(obj) || symbol.Name.Contains(obj) || symbol.Name.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0 || tagName.Contains(obj) || tagName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        allOpens.Add(new OpenningEntity(symbol, instances));
                    }
                }
            });
        }
        private ObservableCollection<OpenningEntity> allOpens = new ObservableCollection<OpenningEntity>();
        public ObservableCollection<OpenningEntity> AllOpens
        {
            get => allOpens;
            set => SetProperty(ref allOpens, value);
        }
    }
    public class SizeComparer : IEqualityComparer<(double width, double height)>
    {
        public bool Equals((double width, double height) x, (double width, double height) y)
        {
            // 考虑浮点数精度问题，使用近似比较
            const double tolerance = 0.001;
            return Math.Abs(x.width - y.width) < tolerance &&
                   Math.Abs(x.height - y.height) < tolerance;
        }
        public int GetHashCode((double width, double height) obj)
        {
            return obj.width.GetHashCode() ^ obj.height.GetHashCode();
        }
    }
}
