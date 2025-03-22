using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace CreatePipe.WpfDirectoryTreeView
{
    public class FamilyManagerViewModel : ObserverableObject
    {
        UIApplication application;
        public DirectoryInfo Info { get; set; }
        public ObservableCollection<Dirs> RootDirectories { get; set; }
        public ObservableCollection<FileSingle> FilesView { get; set; } = new ObservableCollection<FileSingle>();
        public FamilyManagerViewModel(UIApplication uiApp)
        {
            application = uiApp;
            RootDirectories = new ObservableCollection<Dirs>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    var rootDir = new Dirs(new DirectoryInfo(drive.RootDirectory.FullName));
                    RootDirectories.Add(rootDir);
                }
                catch (Exception ex)
                {
                    // 忽略不可用的驱动器
                    //Debug.WriteLine($"Error accessing drive {drive.Name}: {ex.Message}");
                }
            }
        }
        private string fileCount = "0";
        public string FileCount
        {
            get => fileCount;
            set
            {
                fileCount = value;
                OnPropertyChanged(nameof(FileCount));
            }
        }
        public ICommand UpgradeFamilyCommand => new RelayCommand<Dirs>(UpgradeFamily);
        private void UpgradeFamily(Dirs dirs)
        {
            foreach (var file in FilesView)
            {
                if (file.Version != application.Application.VersionNumber.ToString() && file.Version != "版本太高")
                {
                    try
                    {
                        Document doc = application.Application.OpenDocumentFile(file.fullName);
                        //doc.Save();
                        doc.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        public ICommand MakeThumbCommand => new RelayCommand<Dirs>(MakeThumbView);
        private void MakeThumbView(Dirs dirs)
        {
            if (dirs != null)
            {
                FamilyThumbExportForm familyThumbExportForm = new FamilyThumbExportForm(application, dirs);
                familyThumbExportForm.Show();
            }
        }
        public ICommand LoadFamilyCommand => new RelayCommand<FileSingle>(LoadFamily);
        private void LoadFamily(FileSingle file)
        {
            XmlDoc.Instance.Task.Run(app =>
            {
                application.ActiveUIDocument.Document.NewTransaction(() =>
                {
                    application.ActiveUIDocument.Document.LoadFamily(file.fullName);
                }, "载入族");
            });
        }
        public ICommand OpenFamilyCommand => new RelayCommand<FileSingle>(OpenFile);
        private void OpenFile(FileSingle file)
        {
            application.OpenAndActivateDocument(file.fullName);
        }
        public ICommand OpenThumbCommand => new RelayCommand<FileSingle>(OpenView);
        private void OpenView(FileSingle file)
        {
            string jpgFilePath = Path.ChangeExtension(file.fullName, "jpg");
            try
            {
                Process.Start(jpgFilePath); // 使用默认程序打开 .jpg 文件
            }
            catch (Exception)
            {
            }
        }
        public ICommand DelBackupCommand => new BaseBindingCommand(DelBackup);
        private void DelBackup(object obj)
        {
            List<FileSingle> bakFiles = new List<FileSingle>();
            if (FilesView != null)
            {
                foreach (FileSingle item in FilesView)
                {
                    if (Match(item.name))
                    {
                        bakFiles.Add(item);
                    }
                }
                foreach (var item in bakFiles)
                {
                    try
                    {
                        FileSystem.DeleteFile(item.fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (IOException e)
                    {
                    }
                }
                TaskDialog.Show("tt", $"已清理备份{bakFiles.Count().ToString()}个");
                LoadFilesFromDirectory();
            }
            else TaskDialog.Show("tt", "未找到族文件");
        }
        private static bool Match(string fileName)
        {
            Regex backupPattern = new Regex(@"\.[0-9]{3,4}\.r(vt|fa|te)");
            Match fileMatch = backupPattern.Match(fileName);

            if (fileMatch.Success)
            {
                return true;
            }
            return false;
        }
        public ICommand SaveCsvCommand => new BaseBindingCommand(SaveCsv);
        private void SaveCsv(Object para)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "file_list.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string newPath = saveFileDialog.FileName;
                using (var writer = new StreamWriter(newPath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("文件名称,文件大小,文件版本,文件路径");
                    foreach (var file in FilesView)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file.fullName);
                        var filesize = file.DisplaySize;
                        var fileVersion = file.Version;
                        var filePath = file.directoryName;
                        writer.WriteLine($"{fileName},{filesize},{fileVersion},{filePath}");
                    }
                }
                TaskDialog.Show("tt", "文件清单已成功导出为 CSV 文件！");
            }
        }
        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set
            {
                _keyword = value;
                OnPropertyChanged(nameof(Keyword));
                FilterFilesByKeyword(null); // 关键字变化时触发过滤
            }
        }
        private Dirs _selectedDirectory;
        public Dirs SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                _selectedDirectory = value;
                OnPropertyChanged(nameof(SelectedDirectory));
                LoadFilesFromDirectory(); // 加载文件并缓存
            }
        }
        private string _selectedDirectoryPath;
        public string SelectedDirectoryPath
        {
            get => _selectedDirectoryPath;
            set
            {
                _selectedDirectoryPath = value;
                OnPropertyChanged(nameof(SelectedDirectoryPath));
            }
        }
        private List<FileSingle> _cachedFiles = new List<FileSingle>();  // 缓存文件列表
        public ICommand UpdateFilesViewCommand => new BaseBindingCommand(FilterFilesByKeyword);
        private async void LoadFilesFromDirectory()
        {
            // 如果选中的目录为空，则路径为空字符串
            SelectedDirectoryPath = SelectedDirectory?.Info.FullName ?? "No directory selected";
            // 清空缓存和文件列表
            _cachedFiles.Clear();
            FilesView.Clear();
            if (!string.IsNullOrEmpty(SelectedDirectoryPath))
            {
                // 异步加载文件
                await Task.Run(() =>
                {
                    List<FileSingle> files = Directory.GetFiles(SelectedDirectoryPath, "*.rfa")
                                              .Select(file => new FileInfo(file))
                                              .Select(fileInfo => new FileSingle(fileInfo))
                                              .ToList();
                    _cachedFiles = files;
                });
                // 加载完成后触发过滤
                FilterFilesByKeyword(null);
            }
        }
        private void FilterFilesByKeyword(Object para)
        {
            // 如果没有关键字，则显示所有文件
            if (string.IsNullOrEmpty(Keyword))
            {
                FilesView.Clear();
                foreach (var file in _cachedFiles)
                {
                    FilesView.Add(file);
                }
            }
            else
            {
                // 过滤文件列表
                var filteredFiles = _cachedFiles
                    .Where(file => Path.GetFileNameWithoutExtension(file.name)
                                  .IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                // 更新文件列表
                FilesView.Clear();
                foreach (var file in filteredFiles)
                {
                    FilesView.Add(file);
                }
            }
            FileCount = FilesView.Count().ToString();
        }
        public string docPath { get; set; }
        public ICommand GetNewFolderCommand => new BaseBindingCommand(GetNewFolder);
        private void GetNewFolder(Object para)
        {
            RootDirectories.Clear();
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择一个文件夹";
                folderDialog.ShowNewFolderButton = false; // 是否显示“新建文件夹”按钮
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    DirectoryInfo dir = new DirectoryInfo(selectedPath);
                    RootDirectories.Add(new Dirs(dir));
                }
            }
        }
    }
}
