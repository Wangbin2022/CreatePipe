using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace CreatePipe.WpfDirectoryTreeView
{
    public class TestTreeViewModel : ObserverableObject
    {
        public DirectoryInfo Info { get; set; }
        public ObservableCollection<Dirs> RootDirectories { get; set; }
        public TestTreeViewModel()
        {
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
                    Debug.WriteLine($"Error accessing drive {drive.Name}: {ex.Message}");
                }
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
                UpdateSelectedDirectoryPath();
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
        private void UpdateSelectedDirectoryPath()
        {
            // 如果选中的目录为空，则路径为空字符串
            SelectedDirectoryPath = SelectedDirectory?.Info.FullName ?? "No directory selected";
        }
        public string docPath { get; set; }
        //List<FileInfo> files { get; set; }
        public ICommand SaveCsvCommand => new BaseBindingCommand(SaveCsv);
        private void SaveCsv(Object para)
        {
            if (SelectedDirectory == null)
            {
                System.Windows.MessageBox.Show("No directory selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //输出目标文件夹下所有文档csv
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "file_list.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                docPath = saveFileDialog.FileName;
                SaveFilesToCsv(new DirectoryInfo(SelectedDirectory.Info.FullName).GetFiles().ToList());
                System.Windows.MessageBox.Show("文件清单已成功导出为 CSV 文件！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SaveFilesToCsv(List<FileInfo> files)
        {
            using (var writer = new StreamWriter(docPath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("FileName,FilePath");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.FullName);
                    var filePath = file.Directory.Name;
                    writer.WriteLine($"{fileName},{filePath}");
                }
            }
        }
        public ICommand GetNewFolderCommand => new RelayCommand<FileInfo>(GetNewFolder);
        private void GetNewFolder(FileInfo info)
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
