using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CreatePipe.NCCoding
{
    public class NCCodingViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIApplication uiApp { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public NCCodingViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            uiApp = application;
            QueryELement(null);
        }
        public ICommand ExportCsvCommand => new BaseBindingCommand(ExportCsv);
        private void ExportCsv(object obj)
        {
            UniversalNewString subView = new UniversalNewString("提示：输入主文件名，默认在桌面");
            if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            {
                return;
            }
            string outputBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), vm.NewName);
            string csvPath = outputBasePath + ".csv";
            try
            {
                //使用写入模式打开文件，如果文件存在则覆盖
                using (StreamWriter writer = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    foreach (var revitUnit in Entities)
                    {
                        string line = $"{revitUnit.FamilyName},{revitUnit.projectId}";
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", $"保存CSV文件时发生错误: {ex.Message}");
            }
        }
        public ICommand CodeElementsCommand => new cmd.RelayCommand<NCCodingEntity>(CodeElements);
        private void CodeElements(NCCodingEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Document.NewTransaction(() =>
                {
                    foreach (var item in entity.FamilyCollection)
                    {
                        Parameter para = item.LookupParameter("族ID");
                        para.Set(entity.projectId);
                    }
                }, "修改族ID");
                QueryELement(null);
            });
        }
        private bool _canCoding = false;
        public bool CanCoding
        {
            get => _canCoding;
            set => SetProperty(ref _canCoding, value);
        }
        public ICommand HideElementCommand => new BaseBindingCommand(HideElement);
        private void HideElement(object obj)
        {
            _externalHandler.Run(app =>
            {
                Entities.Clear();
                var allFamilyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                HashSet<Family> FamilyCollection = new HashSet<Family>(new FamilyComparer());
                foreach (var familyInstance in allFamilyInstances)
                {
                    Family family = familyInstance.Symbol.Family;
                    FamilyCollection.Add(family);
                }
                foreach (var item in FamilyCollection)
                {
                    if (string.IsNullOrEmpty(Keyword) || item.Name.Contains(Keyword) || item.Name.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        NCCodingEntity ncObj = new NCCodingEntity(item);
                        if (!ncObj.IsCompliant)
                        {
                            Entities.Add(ncObj);
                        }
                    }
                }
            });
        }
        public ICommand SelectElementsCommand => new cmd.RelayCommand<NCCodingEntity>(SelectElements);
        private void SelectElements(NCCodingEntity entity)
        {
            List<ElementId> selectedElementIds = new List<ElementId>();
            foreach (var item in entity.FamilyCollection)
            {
                selectedElementIds.Add(item.Id);
            }
            Selection select = uiApp.ActiveUIDocument.Selection;
            select.SetElementIds(selectedElementIds);
        }

        public ICommand QueryElementCommand => new cmd.RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            _externalHandler.Run(app =>
            {
                Entities.Clear();
                var allFamilyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                HashSet<Family> FamilyCollection = new HashSet<Family>(new FamilyComparer());
                foreach (var familyInstance in allFamilyInstances)
                {
                    Family family = familyInstance.Symbol.Family;
                    FamilyCollection.Add(family);
                }

                foreach (var item in FamilyCollection)
                {
                    if (string.IsNullOrEmpty(obj) || item.Name.Contains(obj) || item.Name.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        NCCodingEntity ncObj = new NCCodingEntity(item);
                        if (ncObj.canCode)
                        {
                            CanCoding = true;
                        }
                        Entities.Add(ncObj);
                    }
                }
            });
        }
        private class FamilyComparer : IEqualityComparer<Family>
        {
            public bool Equals(Family x, Family y)
            {
                if (x == null || y == null)
                {
                    return false;
                }
                return x.Name == y.Name;
            }
            public int GetHashCode(Family obj)
            {
                return obj.Name.GetHashCode();
            }
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
            get { return _keyword; }
            set => SetProperty(ref _keyword, value);
        }
    }
}
