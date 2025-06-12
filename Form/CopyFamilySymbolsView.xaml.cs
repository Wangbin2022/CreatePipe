using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommandLine;
using CreatePipe.cmd;
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
            // 获取当前文档中的所有 Family
            var allFamilyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>();
            var uniqueFamilyNames = new HashSet<string>();
            foreach (var familyInstance in allFamilyInstances)
            {
                Family family = familyInstance.Symbol.Family;
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

            // 获取源族和目标族的 FamilySymbol 名称列表
            List<string> sourceFamilySymbols = SelectedFamily.FamilySymbols;
            List<string> targetFamilySymbols = SelectedFilteredFamily.FamilySymbols;

            // 找出源族有但目标族没有的 Symbol 名称
            var missingSymbols = sourceFamilySymbols.Except(targetFamilySymbols).ToList();

            if (missingSymbols.Count == 0)
            {
                TaskDialog.Show("提示", "目标族已包含源族的所有类型，无需复制。");
                return;
            }

            // 获取目标族的 Family 对象
            Family targetFamily = SelectedFilteredFamily.Family;

            using (Transaction trans = new Transaction(targetFamily.Document, "复制族类型"))
            {
                trans.Start();
                try
                {
                    foreach (string symbolName in missingSymbols)
                    {
                        var symbolIds = SelectedFilteredFamily.Family.GetFamilySymbolIds();
                        FamilySymbol familySymbol = (FamilySymbol)SelectedFilteredFamily.Family.Document.GetElement(symbolIds.First());
                        familySymbol.Duplicate(symbolName);
                    }
                    trans.Commit();
                    TaskDialog.Show("完成", $"成功复制 {missingSymbols.Count} 个族类型到目标族。");
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("错误", $"复制族类型失败: {ex.Message}");
                }
            }
        }
        private void UpdateFilteredFamilies()
        {
            if (SelectedFamily != null)
            {
                // 根据选中的 Family 的类别过滤其他 Family
                string selectedCategory = SelectedFamily.Category;
                FilteredFamilies.Clear();
                foreach (var family in AllFamilies)
                {
                    if (family.Category == selectedCategory)
                    {
                        FilteredFamilies.Add(family);
                    }
                }
                SelectedFilteredFamily = FilteredFamilies[0];
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
        {
            get => _selectedFilteredFamily;
            set => SetProperty(ref _selectedFilteredFamily, value);
        }
    }
    public class FamilyEntity
    {
        public FamilyEntity(Family family)
        {
            Family = family;
            Name = family.Name;
            Category = family.FamilyCategory.Name;
            SymbolCount = family.GetFamilySymbolIds().Count();
            foreach (var item in family.GetFamilySymbolIds())
            {
                FamilySymbol familySymbol = (FamilySymbol)family.Document.GetElement(item);
                FamilySymbols.Add(familySymbol.Name);
            }
        }
        public Family Family { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int SymbolCount { get; set; }
        public List<string> FamilySymbols { get; set; } = new List<string>();
    }
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
