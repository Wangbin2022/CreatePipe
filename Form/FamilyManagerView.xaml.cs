using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;


namespace CreatePipe.Form
{
    /// <summary>
    /// FamilyManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyManagerView : Window
    {
        UIApplication App;
        public FamilyManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new FamilyManagerViewModel(uiApp);
            App = uiApp;
        }
        private void btn_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
        private async void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取当前选中的 FileSingle 对象
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileSingle selectedFile)
            {
                // 异步调用加载方法，UI不会被冻结
                await selectedFile.LoadDetailsAsync(App);
            }
        }
    }
    public class FamilyManagerViewModel : ObserverableObject, IQueryViewModel<FileSingle>
    {
        private readonly UIApplication _application;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        private List<FileSingle> _cachedFiles = new List<FileSingle>();
        public ObservableCollection<FileSingle> Collection { get; set; } = new ObservableCollection<FileSingle>();
        public ObservableCollection<Dirs> RootDirectories { get; set; } = new ObservableCollection<Dirs>();
        private Dirs _selectedDirectory;
        public Dirs SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                SetProperty(ref _selectedDirectory, value);
                _ = LoadFilesFromDirectoryAsync();
            }
        }
        public FamilyManagerViewModel(UIApplication uiApp)
        {
            _application = uiApp;
            InitLayers();
        }
        // IQueryViewModel<FileSingle> 实现
        public void InitLayers()
        {
            RootDirectories.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try { RootDirectories.Add(new Dirs(new DirectoryInfo(drive.RootDirectory.FullName))); }
                catch { }
            }
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        public void QueryELement(string text)
        {
            var loadedNames = GetLoadedFamilyNames();
            var filtered = string.IsNullOrEmpty(text) ? _cachedFiles
                : _cachedFiles.Where(f => f.FamilyName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
            Collection.Clear();
            foreach (var file in filtered)
            {
                file.HasLoaded = loadedNames.Contains(file.FamilyName);
                Collection.Add(file);
            }
        }
        private HashSet<string> GetLoadedFamilyNames()
        {
            return new FilteredElementCollector(_application.ActiveUIDocument.Document).OfClass(typeof(Family))
                .Cast<Family>().Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        private string _selectedDirectoryPath;
        public string SelectedDirectoryPath
        {
            get => _selectedDirectoryPath;
            set => SetProperty(ref _selectedDirectoryPath, value);
        }
        private async Task LoadFilesFromDirectoryAsync()
        {
            if (SelectedDirectory == null) return;
            string path = SelectedDirectory.Info.FullName;
            _cachedFiles = await Task.Run(() =>
                Directory.GetFiles(path, "*.rfa").Select(f => new FileSingle(new FileInfo(f))).ToList());
            QueryELement(null);
        }
        public ICommand GetNewFolderCommand => new BaseBindingCommand(_ =>
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RootDirectories.Clear();
                RootDirectories.Add(new Dirs(new DirectoryInfo(dialog.SelectedPath)));
            }
        });
        public ICommand UpgradeFamilyCommand => new RelayCommand<Dirs>(_ =>
            _externalHandler.Run(app =>
            {
                foreach (var file in Collection.Where(f =>
                    f.Version != _application.Application.VersionNumber.ToString() &&
                    f.Version != "版本太高"))
                {
                    try
                    {
                        var doc = _application.Application.OpenDocumentFile(file.fullName);
                        doc.Close();
                    }
                    catch { }
                }
                QueryELement(null);
            }));
        public ICommand LoadFamilyCommand => new RelayCommand<FileSingle>(LoadFamily);
        private void LoadFamily(FileSingle single)
        {
            if (single.Version == "版本太高" && single.Version == "读取失败") return;
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(app.ActiveUIDocument.Document, "载入族", () =>
                {
                    app.ActiveUIDocument.Document.LoadFamily(single.fullName);
                });
                // 回到UI线程刷新状态
                System.Windows.Application.Current.Dispatcher.Invoke(() => QueryELement(null));
            });
        }
        private bool CanLoadFamily(FileSingle file)
        {
            return file != null && file.Version != "版本太高" && file.Version != "读取失败";
        }
        public ICommand OpenFamilyCommand => new RelayCommand<FileSingle>(
            file => _externalHandler.Run(_ => _application.OpenAndActivateDocument(file.fullName)),
            canExecute: f => f != null && f.Version != "版本太高" && f.Version != "读取失败");
        public ICommand OpenThumbCommand => new RelayCommand<FileSingle>(file =>
        {
            string jpg = Path.ChangeExtension(file.fullName, "jpg");
            try { Process.Start(jpg); } catch { }
        });
        public ICommand MakeThumbCommand => new RelayCommand<Dirs>(MakeThumb);
        private void MakeThumb(Dirs dirs)
        {
            if (dirs == null) return;
            _externalHandler.Run(_ =>
            {
                FamilyManagerSubView familyThumbExportForm = new FamilyManagerSubView(_application, dirs, _externalHandler);
                familyThumbExportForm.ShowDialog();
            });
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;
            var toDeleteList = selectedItems.Cast<FileSingle>().ToList();
            if (toDeleteList.Count == 0) return;
            // 【关键】事务前缓存所有 ID
            try
            {
                NoTransactionWithProgressBarHelper.Execute(toDeleteList.Count, "批量删除族文件", (service) =>
                {
                    service.UpdateMax(toDeleteList.Count());
                    int index = 0;
                    foreach (var id in toDeleteList)
                    {
                        service.Update(++index, id.name);

                        FileSystem.DeleteFile(id.fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                });
                foreach (var item in toDeleteList)
                {
                    _cachedFiles.Remove(item);
                    Collection.Remove(item);
                }
                string resultMessage = $"已成功删除 {toDeleteList.Count} 个文件。";
                TaskDialog.Show("删除完成", resultMessage);
                QueryELement(null);
            }
            catch (IOException) { }
        }
        public ICommand SaveCsvCommand => new RelayCommand<object>(SaveCsv);
        private void SaveCsv(object obj)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "file_list.csv"
            };
            if (dialog.ShowDialog() != true) return;
            var csv = new CsvHelper(dialog.FileName);
            // 写入标题行 + 数据行（覆盖模式）
            csv.WriteAllWithHeaders(new[] { "文件名称", "文件大小", "文件版本", "文件路径" },
                Collection.Select(file => new[]
                {
                        Path.GetFileNameWithoutExtension(file.fullName),
                        file.DisplaySize,
                        file.Version,
                        file.directoryName
                })
            );
            TaskDialog.Show("导出成功", "文件清单已成功导出为 CSV 文件！");
        }
    }
}
