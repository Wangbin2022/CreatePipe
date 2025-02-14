using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace CreatePipe.WpfDirectoryTreeView
{
    public class Save2CsvViewModel : ObserverableObject
    {
        private string myVar;

        public string SelectedFolderPath
        {
            get { return myVar; }
            set
            {
                myVar = value;
                OnPropertyChanged();
            }
        }

        public string docPath { get; set; }
        List<FileInfo> files { get; set; }
        public ICommand SelectFolderCommand => new BaseBindingCommand(SelectFolder);
        public ICommand SaveCommand => new BaseBindingCommand(Save);
        private void Save(object obj)
        {
            //查找备份文件数量.OK
            List<FileInfo> bakFiles = new List<FileInfo>();
            if (files != null)
            {
                foreach (FileInfo item in files)
                {
                    if (Match(item.Name))
                    {
                        bakFiles.Add(item);
                    }                    
                }
                TaskDialog.Show("tt", bakFiles.Count().ToString());
            }

            //保存csv清单.OK
            //var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            //{
            //    Filter = "CSV 文件 (*.csv)|*.csv",
            //    DefaultExt = "csv",
            //    FileName = "file_list.csv"
            //};
            //if (saveFileDialog.ShowDialog() == true)
            //{
            //    docPath = saveFileDialog.FileName;
            //    //var files = GetFilesRecursively(SelectedFolderPath);
            //    SaveFilesToCsv(files);
            //    System.Windows.MessageBox.Show("文件清单已成功导出为 CSV 文件！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }
        // Method to match file pattern
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
        public Save2CsvViewModel()
        {

        }
        private void SelectFolder(Object para)
        {
            var folderDialog = new FolderBrowserDialog();
            folderDialog.ShowDialog();
            SelectedFolderPath = folderDialog.SelectedPath;
            DirectoryInfo directoryInfo = new DirectoryInfo(SelectedFolderPath);
            files = directoryInfo.GetFiles().ToList();
        }
        private void SaveFilesToCsv(List<FileInfo> files)
        {
            using (var writer = new StreamWriter(docPath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("FileName,FilePath");
                foreach (var file in files)
                {
                    //var fileName = file.Name;
                    //var filePath = Path.GetDirectoryName(file.FullName);
                    var fileName = Path.GetFileNameWithoutExtension(file.FullName);
                    var filePath = file.Directory.Name;
                    writer.WriteLine($"{fileName},{filePath}");
                }
            }
        }
    }
}
