using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
//using CommandLine;
using CreatePipe.cmd;
using CreatePipe.models;
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
    /// CopyFamilySymbolsView.xaml 的交互逻辑
    /// </summary>
    public partial class CopyFamilySymbolsView : Window
    {
        public CopyFamilySymbolsView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new CopyFamilySymbolsViewModel(uiApp);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class CopyFamilySymbolsViewModel : ObserverableObject
    {
        public UIApplication Application { get; set; }
        public Document Document { get; set; }
        public CopyFamilySymbolsViewModel(UIApplication uiApp)
        {
            Application = uiApp;
            Document = uiApp.ActiveUIDocument.Document;
            // 【优化1】直接获取所有已载入的Family，而不是通过Instance反查
            var allFamilies = new FilteredElementCollector(Document).OfClass(typeof(Family)).Cast<Family>()
                .Where(f => f.IsEditable && f.GetFamilySymbolIds().Count > 0); // 排除不可编辑族(如系统族)和空族
            var uniqueFamilyNames = new HashSet<string>();
            foreach (var family in allFamilies)
            {
                if (uniqueFamilyNames.Add(family.Name))
                {
                    AllFamilies.Add(new FamilyEntity(family));
                }
            }
            if (AllFamilies.Count > 0)
            {
                SelectedFamily = AllFamilies[0];
            }
        }
        public ICommand CopyFamilySymbolsCommand => new BaseBindingCommand(CopyFamilySymbols);
        private void CopyFamilySymbols(object obj)
        {
            if (SelectedFamily == null || SelectedFilteredFamily == null)
            {
                TaskDialog.Show("错误", "请先选择源族和目标族！");
                return;
            }
            if (SelectedFamily.Name == SelectedFilteredFamily.Name)
            {
                TaskDialog.Show("提示", "源族和目标族相同，无需操作。");
                return;
            }
            var sourceSymbols = SelectedFamily.SymbolDict;
            var targetSymbols = SelectedFilteredFamily.SymbolDict;
            // 找出源族有，但目标族没有的类型名称
            var missingSymbolNames = sourceSymbols.Keys.Except(targetSymbols.Keys).ToList();
            if (missingSymbolNames.Count == 0)
            {
                TaskDialog.Show("提示", "目标族已包含源族的所有类型，无需复制。");
                return;
            }
            Family targetFamily = SelectedFilteredFamily.Family;
            Document doc = targetFamily.Document;
            // 获取目标族的一个基础类型，用于复制
            ElementId baseSymbolId = targetFamily.GetFamilySymbolIds().FirstOrDefault();
            if (baseSymbolId == ElementId.InvalidElementId) return;
            FamilySymbol baseTargetSymbol = doc.GetElement(baseSymbolId) as FamilySymbol;
            NewTransaction.Execute(doc, "复制族类型及同步参数",()=>
            {
                try
                {
                    int successCount = 0;
                    foreach (string missingName in missingSymbolNames)
                    {
                        // 1. 复制生成新类型
                        FamilySymbol newTargetSymbol = baseTargetSymbol.Duplicate(missingName) as FamilySymbol;
                        // 获取源类型实体
                        FamilySymbol sourceSymbol = sourceSymbols[missingName];
                        // 2. 【优化2】核心功能：同步参数值
                        SyncParameters(sourceSymbol, newTargetSymbol);
                        successCount++;
                    }
                    TaskDialog.Show("完成", $"成功复制并同步 {successCount} 个族类型的参数到目标族。");
                    // 复制完成后最好刷新一下当前界面数据，避免连续点击报错
                    SelectedFilteredFamily = new FamilyEntity(targetFamily);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"复制失败: {ex.Message}");
                }
            });         
        }
        /// <summary>
        /// 同步两个族类型之间的参数值
        /// </summary>
        private void SyncParameters(FamilySymbol source, FamilySymbol target)
        {
            // 遍历源类型的所有参数
            foreach (Parameter sourceParam in source.Parameters)
            {
                // 跳过没有值的、只读的、或者是公式驱动的参数
                if (!sourceParam.HasValue || sourceParam.IsReadOnly) continue;
                // 在目标类型中寻找同名参数
                Parameter targetParam = target.LookupParameter(sourceParam.Definition.Name);
                // 如果目标类型也有这个参数，并且允许写入
                if (targetParam != null && !targetParam.IsReadOnly)
                {
                    // 根据数据类型分别赋值
                    switch (sourceParam.StorageType)
                    {
                        case StorageType.Double:
                            targetParam.Set(sourceParam.AsDouble());
                            break;
                        case StorageType.Integer:
                            targetParam.Set(sourceParam.AsInteger());
                            break;
                        case StorageType.String:
                            string strVal = sourceParam.AsString();
                            if (strVal != null) targetParam.Set(strVal);
                            break;
                        case StorageType.ElementId:
                            targetParam.Set(sourceParam.AsElementId());
                            break;
                    }
                }
            }
        }
        private void UpdateFilteredFamilies()
        {
            if (SelectedFamily != null)
            {
                string selectedCategory = SelectedFamily.Category;
                FilteredFamilies.Clear();
                foreach (var family in AllFamilies)
                {
                    // 过滤出同类别，并且不包含自己
                    if (family.Category == selectedCategory && family.Name != SelectedFamily.Name)
                    {
                        FilteredFamilies.Add(family);
                    }
                }
                if (FilteredFamilies.Count > 0)
                {
                    SelectedFilteredFamily = FilteredFamilies[0];
                }
                else
                {
                    SelectedFilteredFamily = null;
                }
            }
        }
        public ObservableCollection<FamilyEntity> AllFamilies { get; set; } = new ObservableCollection<FamilyEntity>();
        public ObservableCollection<FamilyEntity> FilteredFamilies { get; set; } = new ObservableCollection<FamilyEntity>();
        private FamilyEntity _selectedFamily;
        public FamilyEntity SelectedFamily
        {
            get => _selectedFamily;
            set
            {
                _selectedFamily = value;
                OnPropertyChanged();
                UpdateFilteredFamilies();
            }
        }
        private FamilyEntity _selectedFilteredFamily;
        public FamilyEntity SelectedFilteredFamily
        { get => _selectedFilteredFamily; set => SetProperty(ref _selectedFilteredFamily, value); }
    }

    //public class CopyFamilySymbolsViewModel : ObserverableObject
    //{
    //    public UIApplication Application { get; set; }
    //    public Document Document { get; set; }
    //    public CopyFamilySymbolsViewModel(UIApplication uiApp)
    //    {
    //        Application = uiApp;
    //        Document = uiApp.ActiveUIDocument.Document;
    //        // 获取当前文档中的所有 Family
    //        var allFamilyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance))
    //            .Cast<FamilyInstance>();
    //        var uniqueFamilyNames = new HashSet<string>();
    //        foreach (var familyInstance in allFamilyInstances)
    //        {
    //            Family family = familyInstance.Symbol.Family;
    //            if (uniqueFamilyNames.Add(family.Name))
    //            {
    //                AllFamilies.Add(new FamilyEntity(family));
    //            }
    //        }
    //        if (AllFamilies.Count > 0)
    //        {
    //            SelectedFamily = AllFamilies[0];
    //        }
    //    }
    //    public ICommand CopyFamilySymbolsCommand => new BaseBindingCommand(CopyFamilySymbols);
    //    private void CopyFamilySymbols(object obj)
    //    {
    //        if (SelectedFamily == null || SelectedFilteredFamily == null)
    //        {
    //            TaskDialog.Show("错误", "请先选择源族和目标族！");
    //            return;
    //        }
    //        // 获取源族和目标族的 FamilySymbol 名称列表
    //        List<string> sourceFamilySymbols = SelectedFamily.FamilySymbols;
    //        List<string> targetFamilySymbols = SelectedFilteredFamily.FamilySymbols;
    //        // 找出源族有但目标族没有的 Symbol 名称
    //        var missingSymbols = sourceFamilySymbols.Except(targetFamilySymbols).ToList();
    //        if (missingSymbols.Count == 0)
    //        {
    //            TaskDialog.Show("提示", "目标族已包含源族的所有类型，无需复制。");
    //            return;
    //        }
    //        // 获取目标族的 Family 对象
    //        Family targetFamily = SelectedFilteredFamily.Family;
    //        using (Transaction trans = new Transaction(targetFamily.Document, "复制族类型"))
    //        {
    //            trans.Start();
    //            try
    //            {
    //                foreach (string symbolName in missingSymbols)
    //                {
    //                    var symbolIds = SelectedFilteredFamily.Family.GetFamilySymbolIds();
    //                    FamilySymbol familySymbol = (FamilySymbol)SelectedFilteredFamily.Family.Document.GetElement(symbolIds.First());
    //                    familySymbol.Duplicate(symbolName);
    //                }
    //                trans.Commit();
    //                TaskDialog.Show("完成", $"成功复制 {missingSymbols.Count} 个族类型到目标族。");
    //            }
    //            catch (Exception ex)
    //            {
    //                trans.RollBack();
    //                TaskDialog.Show("错误", $"复制族类型失败: {ex.Message}");
    //            }
    //        }
    //    }
    //    private void UpdateFilteredFamilies()
    //    {
    //        if (SelectedFamily != null)
    //        {
    //            // 根据选中的 Family 的类别过滤其他 Family
    //            string selectedCategory = SelectedFamily.Category;
    //            FilteredFamilies.Clear();
    //            foreach (var family in AllFamilies)
    //            {
    //                if (family.Category == selectedCategory)
    //                {
    //                    FilteredFamilies.Add(family);
    //                }
    //            }
    //            SelectedFilteredFamily = FilteredFamilies[0];
    //        }
    //    }
    //    public ObservableCollection<FamilyEntity> AllFamilies { get; set; } = new ObservableCollection<FamilyEntity>();
    //    public ObservableCollection<FamilyEntity> FilteredFamilies { get; set; } = new ObservableCollection<FamilyEntity>();
    //    private FamilyEntity _selectedFamily;
    //    public FamilyEntity SelectedFamily
    //    {
    //        get => _selectedFamily;
    //        set
    //        {
    //            _selectedFamily = value;
    //            OnPropertyChanged();
    //            UpdateFilteredFamilies();
    //        }
    //    }
    //    private FamilyEntity _selectedFilteredFamily;
    //    public FamilyEntity SelectedFilteredFamily
    //    {
    //        get => _selectedFilteredFamily;
    //        set => SetProperty(ref _selectedFilteredFamily, value);
    //    }
    //}

    //// 自定义比较器，确保 Family 的唯一性基于名称
    //public class FamilyEqualityComparer : IEqualityComparer<Family>
    //{
    //    public bool Equals(Family x, Family y)
    //    {
    //        if (x == null || y == null)
    //        {
    //            return false;
    //        }
    //        return x.Name == y.Name;
    //    }

    //    public int GetHashCode(Family obj)
    //    {
    //        return obj.Name.GetHashCode();
    //    }
    //}
}
