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
using System.Text;
using System.Text.RegularExpressions;
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
    /// FamilyManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyManagerView : Window
    {
        public FamilyManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new FamilyManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // 假设 directoryTreeView 是您 UI 中的树控件
                if (directoryTreeView?.SelectedItem is Dirs selectedDirectory)
                {
                    string output = selectedDirectory.Info.FullName;
                    System.Windows.Clipboard.SetText(output);
                    System.Windows.Clipboard.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"复制剪贴板失败: {ex.Message}");
            }
            this.Close();
        }
    }
    public class FamilyManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<FileSingle>
    {
        private UIApplication application;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        private List<FileSingle> _cachedFiles = new List<FileSingle>(); // 缓存完整的文件列表

        public DirectoryInfo Info { get; set; }
        public ObservableCollection<Dirs> RootDirectories { get; set; } = new ObservableCollection<Dirs>();

        public FamilyManagerViewModel(UIApplication uiApp)
        {
            application = uiApp;
            InitFunc(); // 接口方法：初始化数据
        }

        #region IQueryViewModelWithDelete<FileSingle> 接口实现

        // 1. 核心数据集合 (替代原 FilesView)
        private ObservableCollection<FileSingle> _collection = new ObservableCollection<FileSingle>();
        public ObservableCollection<FileSingle> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
                OnPropertyChanged(nameof(Collection));
            }
        }

        // 2. 外部事件处理器
        public BaseExternalHandler ExternalHandler => _externalHandler;

        // 3. 基础逻辑命令
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public ICommand DeleteElementCommand => new RelayCommand<FileSingle>(DeleteElement);
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);

        // 4. 初始化方法
        public void InitFunc()
        {
            RootDirectories.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        var rootDir = new Dirs(new DirectoryInfo(drive.RootDirectory.FullName));
                        RootDirectories.Add(rootDir);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"读取驱动器失败 {drive.Name}: {ex.Message}");
                }
            }
        }

        // 5. 过滤查询逻辑
        public void QueryElement(string text)
        {
            if (Collection == null) return;
            Collection.Clear();

            var queryResult = string.IsNullOrWhiteSpace(text)
                ? _cachedFiles
                : _cachedFiles.Where(file => file.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var file in queryResult)
            {
                Collection.Add(file);
            }
            FileCount = Collection.Count.ToString();
        }

        // 6. 单个删除逻辑 (物理文件放入回收站)
        public void DeleteElement(FileSingle entity)
        {
            if (entity == null) return;
            try
            {
                FileSystem.DeleteFile(entity.fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                _cachedFiles.Remove(entity);
                Collection.Remove(entity);
                FileCount = Collection.Count.ToString();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", $"删除文件失败: {ex.Message}");
            }
        }

        // 7. 批量删除逻辑
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null) return;
            var entities = selectedItems.Cast<FileSingle>().ToList();
            if (!entities.Any()) return;

            int count = 0;
            foreach (var entity in entities)
            {
                try
                {
                    FileSystem.DeleteFile(entity.fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    _cachedFiles.Remove(entity);
                    Collection.Remove(entity);
                    count++;
                }
                catch (Exception) { /* 忽略单个删除失败，继续删下一个 */ }
            }
            FileCount = Collection.Count.ToString();
            TaskDialog.Show("提示", $"成功删除 {count} 个文件！");
        }

        #endregion

        #region 属性与UI绑定

        private string _fileCount = "0";
        public string FileCount
        {
            get => _fileCount;
            set { _fileCount = value; OnPropertyChanged(nameof(FileCount)); }
        }

        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                OnPropertyChanged(nameof(Keyword));
                QueryElement(_keyword); // 关键字变化直接触发查询
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
                SelectedDirectoryPath = _selectedDirectory?.Info.FullName ?? string.Empty;
                LoadFilesFromDirectory();
            }
        }

        private string _selectedDirectoryPath;
        public string SelectedDirectoryPath
        {
            get => _selectedDirectoryPath;
            set { _selectedDirectoryPath = value; OnPropertyChanged(nameof(SelectedDirectoryPath)); }
        }

        #endregion

        #region 文件加载核心逻辑

        private async void LoadFilesFromDirectory()
        {
            Collection.Clear();
            _cachedFiles.Clear();
            FileCount = "0";

            if (string.IsNullOrEmpty(SelectedDirectoryPath) || !Directory.Exists(SelectedDirectoryPath)) return;

            try
            {
                // 异步在后台线程加载磁盘文件
                var files = await Task.Run(() =>
                {
                    return Directory.GetFiles(SelectedDirectoryPath, "*.rfa")
                                    .Select(f => new FileInfo(f))
                                    .Select(fi => new FileSingle(fi))
                                    .ToList();
                });

                // await 之后回到 UI 线程，安全赋值
                _cachedFiles = files;
                QueryElement(Keyword); // 加载完后应用当前的关键字过滤
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载文件夹内容失败: {ex.Message}");
            }
        }

        #endregion

        #region 其他业务命令 (升级、打开、载入等)

        public ICommand UpgradeFamilyCommand => new RelayCommand<object>(UpgradeFamily);
        private void UpgradeFamily(object obj)
        {
            _externalHandler.Run(app =>
            {
                foreach (var file in Collection)
                {
                    if (file.Version != application.Application.VersionNumber.ToString() && file.Version != "版本太高")
                    {
                        try
                        {
                            Document doc = application.Application.OpenDocumentFile(file.fullName);
                            doc.Close(); // 打开并立即关闭即视为静默升级触发
                        }
                        catch (Exception ex) { Debug.WriteLine($"升级失败: {file.fullName} - {ex.Message}"); }
                    }
                }
            });
        }

        public ICommand MakeThumbCommand => new RelayCommand<Dirs>(MakeThumbView);
        private void MakeThumbView(Dirs dirs)
        {
            //// 此处需要确保 FamilyThumbExportForm 能够处理 dirs 为 null 的情况
            //FamilyThumbExportForm familyThumbExportForm = new FamilyThumbExportForm(application, dirs ?? SelectedDirectory);
            //familyThumbExportForm.Show();
        }

        public ICommand LoadFamilyCommand => new RelayCommand<FileSingle>(LoadFamily, canExecute: obj => obj != null && obj.Version != "版本太高" && obj.Version != "读取失败");
        private void LoadFamily(FileSingle file)
        {
            _externalHandler.Run(app =>
            {
                application.ActiveUIDocument.Document.NewTransaction(() =>
                {
                    application.ActiveUIDocument.Document.LoadFamily(file.fullName);
                }, "载入族");
            });
        }

        public ICommand OpenFamilyCommand => new RelayCommand<FileSingle>(OpenFile, canExecute: obj => obj != null && obj.Version != "版本太高" && obj.Version != "读取失败");
        private void OpenFile(FileSingle file)
        {
            _externalHandler.Run(app =>
            {
                try { application.OpenAndActivateDocument(file.fullName); }
                catch (Exception ex) { TaskDialog.Show("错误", $"打开失败: {ex.Message}"); }
            });
        }

        public ICommand OpenThumbCommand => new RelayCommand<FileSingle>(OpenView);
        private void OpenView(FileSingle file)
        {
            if (file == null) return;
            string jpgFilePath = System.IO.Path.ChangeExtension(file.fullName, "jpg");
            if (File.Exists(jpgFilePath))
            {
                try { Process.Start(new ProcessStartInfo(jpgFilePath) { UseShellExecute = true }); }
                catch (Exception ex) { Debug.WriteLine($"打开图片失败: {ex.Message}"); }
            }
        }

        // 清理备份（利用复用的 DeleteElements 批量删除）
        public ICommand DelBackupCommand => new RelayCommand<object>(DelBackup);
        private void DelBackup(object obj)
        {
            var backupPattern = new Regex(@"\.[0-9]{3,4}\.r(vt|fa|te)", RegexOptions.IgnoreCase);
            var bakFiles = Collection.Where(f => backupPattern.IsMatch(f.name)).ToList();

            if (bakFiles.Any())
            {
                DeleteElements(bakFiles); // 直接复用接口的批量删除方法
            }
            else
            {
                TaskDialog.Show("提示", "未找到族备份文件");
            }
        }

        public ICommand SaveCsvCommand => new RelayCommand<object>(SaveCsv);
        private void SaveCsv(object para)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "file_list.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("文件名称,文件大小,文件版本,文件路径");
                        foreach (var file in Collection)
                        {
                            var fileName = System.IO.Path.GetFileNameWithoutExtension(file.fullName);
                            writer.WriteLine($"{fileName},{file.DisplaySize},{file.Version},{file.directoryName}");
                        }
                    }
                    TaskDialog.Show("提示", "文件清单已成功导出！");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("错误", $"导出失败: {ex.Message}");
                }
            }
        }

        public ICommand GetNewFolderCommand => new RelayCommand<object>(GetNewFolder);
        private void GetNewFolder(object para)
        {
            //// .NET 8 原生文件夹选择方案
            //var folderDialog = new Microsoft.Win32.OpenFolderDialog
            //{
            //    Title = "请选择一个文件夹",
            //    Multiselect = false
            //};

            //if (folderDialog.ShowDialog() == true)
            //{
            //    RootDirectories.Clear();
            //    string selectedPath = folderDialog.FolderName;
            //    DirectoryInfo dir = new DirectoryInfo(selectedPath);
            //    RootDirectories.Add(new Dirs(dir));
            //    SelectedDirectory = RootDirectories.First(); // 自动选中
            //}
        }

        #endregion
    }
}
