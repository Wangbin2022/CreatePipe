using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CreatePipe.Form
{
    /// <summary>
    /// XrefVisibilityView.xaml 的交互逻辑
    /// </summary>
    public partial class XrefVisibilityView : Window
    {
        public XrefVisibilityView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new XrefVisibilityViewModel(uiApp.ActiveUIDocument.Document);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class XrefVisibilityViewModel : ObserverableObject
    {
        private Document _doc;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public XrefVisibilityViewModel(Document doc)
        {
            _doc = doc;
            LoadRvtLinks();
            LoadDwgLinks();
        }
        // 这表示一个实时计算的只读属性
        public string RevitLinkInfo => $"总链接数{TotalRvtLinkTypes},有效链接数{LoadedRvtLinkTypes},数量更新需重启本窗口";
        private void LoadRvtLinks()
        {
            View activeView = _doc.ActiveView;
            var instances = new FilteredElementCollector(_doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            TotalRvtLinkTypes = instances.Count;
            foreach (var instance in instances)
            {
                ElementId typeId = instance.GetTypeId();
                RevitLinkType linkType = instance.Document.GetElement(typeId) as RevitLinkType;
                if (linkType != null & linkType.GetLinkedFileStatus() == LinkedFileStatus.Loaded)
                {
                    LoadedRvtLinkTypes++;
                    string name = linkType.Name;
                    if (_nameToIdMap.ContainsKey(name)) name += $" ({instance.Id})";
                    _nameToIdMap.Add(name, instance.Id);
                    RevitLinkNames.Add(name);
                    if (!instance.IsHidden(activeView))
                    {
                        SelectedRevitLinkNames.Add(name);
                    }
                }
            }
        }
        private List<string> _selectedRevitLinkNames = new List<string>();
        public List<string> SelectedRevitLinkNames
        {
            get => _selectedRevitLinkNames;
            set
            {
                _selectedRevitLinkNames = value;
                OnPropertyChanged();
                // 当勾选改变时，执行显隐逻辑
                UpdateLinkVisibility();
            }
        }
        public List<string> RevitLinkNames { get; set; } = new List<string>();
        public int TotalRvtLinkTypes { get; set; } = 0;
        public int LoadedRvtLinkTypes { get; set; } = 0;
        private Dictionary<string, ElementId> _nameToIdMap = new Dictionary<string, ElementId>();
        public string DwgLinkInfo => $"总链接数{TotalDwgLinkTypes},有效链接数{LoadedDwgLinkTypes},数量更新需重启本窗口";
        public string DwgImportInfo => $"导入DWG数{ImportedDwgLinks},数量更新需重启本窗口";
        private void LoadDwgLinks()
        {
            View activeView = _doc.ActiveView;
            var instances = new FilteredElementCollector(_doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().ToList();
            TotalDwgLinkTypes = instances.Count;
            foreach (var instance in instances)
            {
                ElementId typeId = instance.GetTypeId();
                if (instance.IsLinked)
                {
                    CADLinkType linkType = instance.Document.GetElement(typeId) as CADLinkType;
                    ExternalFileReference extRef = linkType.GetExternalFileReference();
                    if (extRef != null)
                    {
                        // 3. 获取并判断链接状态
                        if (extRef.GetLinkedFileStatus() == LinkedFileStatus.Loaded)
                        {
                            LoadedDwgLinkTypes++;
                            string name = linkType.Name;
                            // 处理重名情况
                            if (_nameToIdMap.ContainsKey(name)) name += $" ({instance.Id})";
                            _nameToIdMap.Add(name, instance.Id);
                            DwgLinkNames.Add(name);
                            if (!instance.IsHidden(activeView))
                            {
                                SelectedDwgLinkNames.Add(name);
                            }
                        }
                    }
                }
                else
                {
                    ImportedDwgLinks++;
                    var typeID = instance.GetTypeId();
                    var name = _doc.GetElement(typeID).Name;
                    if (_nameToIdMap.ContainsKey(name)) name += $" ({instance.Id})";
                    _nameToIdMap.Add(name, instance.Id);
                    DwgImportNames.Add(name);
                    if (!instance.IsHidden(activeView))
                    {
                        SelectedImportDwgNames.Add(name);
                    }
                }
            }
        }
        private List<string> _selectedImportDwgNames = new List<string>();
        public List<string> SelectedImportDwgNames
        {
            get => _selectedImportDwgNames;
            set
            {
                _selectedImportDwgNames = value;
                OnPropertyChanged();
                // 当勾选改变时，执行显隐逻辑
                UpdateLinkVisibility();
            }
        }
        private List<string> _selectedDwgNames = new List<string>();
        public List<string> SelectedDwgLinkNames
        {
            get => _selectedDwgNames;
            set
            {
                _selectedDwgNames = value;
                OnPropertyChanged();
                UpdateLinkVisibility();
            }
        }
        private void UpdateLinkVisibility()
        {
            var rvtSelected = SelectedRevitLinkNames ?? new List<string>();
            var dwgLinkSelected = SelectedDwgLinkNames ?? new List<string>();
            var dwgImportSelected = SelectedImportDwgNames ?? new List<string>();
            _externalHandler.Run(app =>
            {
                Document document = app.ActiveUIDocument.Document;
                View activeView = document.ActiveView;
                using (Transaction trans = new Transaction(document, "更新链接显隐状态"))
                {
                    trans.Start();
                    foreach (var kvp in _nameToIdMap)
                    {
                        ElementId id = kvp.Value;
                        if (id == null || id == ElementId.InvalidElementId) continue;
                        Element elem = document.GetElement(id);
                        if (elem == null) continue;
                        bool shouldBeVisible = false;
                        // --- 核心冲突防止逻辑 ---
                        // 根据元素类型决定去哪个 List 里检查勾选状态
                        if (elem is RevitLinkInstance)
                        {
                            shouldBeVisible = rvtSelected.Contains(kvp.Key);
                        }
                        else if (elem is ImportInstance importInstance)
                        {
                            if (importInstance.IsLinked)
                            {
                                // 是链接的 CAD
                                shouldBeVisible = dwgLinkSelected.Contains(kvp.Key);
                            }
                            else
                            {
                                // 是直接导入的 CAD
                                shouldBeVisible = dwgImportSelected.Contains(kvp.Key);
                            }
                        }
                        else continue;
                        // 执行显隐操作
                        List<ElementId> ids = new List<ElementId> { id };
                        if (shouldBeVisible)
                        {
                            // 取消隐藏 (Unhide)
                            if (elem.IsHidden(activeView))
                            {
                                activeView.UnhideElements(ids);
                            }
                        }
                        else
                        {
                            // 隐藏 (Hide)
                            // 必须满足：视图允许隐藏该元素 且 元素当前未被隐藏
                            if (!elem.IsHidden(activeView))
                            {
                                activeView.HideElements(ids);
                            }
                        }
                    }
                    trans.Commit();
                }
            });
        }
        public List<string> DwgImportNames { get; set; } = new List<string>();
        public List<string> DwgLinkNames { get; set; } = new List<string>();
        public int TotalDwgLinkTypes { get; set; } = 0;
        public int LoadedDwgLinkTypes { get; set; } = 0;
        public int ImportedDwgLinks { get; set; } = 0;
    }
}
