using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Shapes;

namespace CreatePipe.WpfDirectoryTreeView
{
    public class FileSingle : ObserverableObject
    {
        public string fullName { get; set; }
        public string name { get; set; }
        public string directoryName { get; set; }
        public double length { get; set; }
        public string Version { get; set; }
        public bool HasJpgFile { get; set; } = false;
        public bool IsHighVerFile { get; set; } = false;
        public FileSingle(FileInfo fileInfo)
        {
            fullName = fileInfo.FullName;
            name = fileInfo.Name;
            directoryName = fileInfo.Directory.Name;
            length = fileInfo.Length;
            try
            {
                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
                Version = basicFileInfo.Format;
                IsHighVerFile = true;
            }
            catch (Exception)
            {
                Version = "版本太高";
            }
            string jpgPath = System.IO.Path.ChangeExtension(fileInfo.FullName, ".jpg");
            if (File.Exists(jpgPath))
            {
                HasJpgFile = true;
            }
        }
        // 用于显示文件大小的属性
        public string DisplaySize
        {
            get
            {
                if (length >= 1024 * 1024) // 大于等于 1 MB
                {
                    return $"{length / (1024 * 1024):N1} MB";
                }
                else if (length >= 1024) // 大于等于 1 KB
                {
                    return $"{length / 1024:N1} KB";
                }
                else // 小于 1 KB
                {
                    return $"{length} Bytes";
                }
            }
        }
        //要把ViewModel中Converter逻辑放到FileSingleModel属性里
    }
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long fileSize)
            {
                // 自动选择合适的单位
                if (fileSize >= 1024 * 1024) // 大于等于 1 MB
                {
                    return $"{fileSize / (1024 * 1024):N2} MB";
                }
                else if (fileSize >= 1024) // 大于等于 1 KB
                {
                    return $"{fileSize / 1024:N2} KB";
                }
                else // 小于 1 KB
                {
                    return $"{fileSize} Bytes";
                }
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //public class RevitVersionConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        try
    //        {
    //            if (value is string path)
    //            {
    //                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(path);
    //                int.TryParse(basicFileInfo.Format, out int version);
    //                return version;
    //            }
    //        }
    //        catch (Exception)
    //        {
    //        }
    //        return "版本太高";
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    public class Dirs : ObserverableObject
    {
        public DirectoryInfo Info { get; set; }
        public Dirs(DirectoryInfo info)
        {
            Info = info;
        }
        public IEnumerable<FileInfo> Files
        {
            get => Info.GetFiles();
        }
        private ObservableCollection<Dirs> _directories;
        public ObservableCollection<Dirs> Directories
        {
            get
            {
                if (_directories == null)
                {
                    _directories = new ObservableCollection<Dirs>();
                    foreach (var dir in Info.GetDirectories("*", SearchOption.TopDirectoryOnly))
                    {
                        _directories.Add(new Dirs(dir));
                    }
                }
                return _directories;
            }
        }
    }
}
