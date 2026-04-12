using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace CreatePipe.models
{
    public class FileSingle : ObserverableObject
    {
        private static readonly Regex BackupPattern = new Regex(@"\.[0-9]{3,4}\.r(vt|fa|te)$", RegexOptions.IgnoreCase);
        public string fullName { get; set; }
        public string name { get; set; }
        public string directoryName { get; set; }
        public double length { get; set; }
        public string Version { get; set; } = "读取失败";
        public bool HasJpgFile { get; set; } = false;
        public bool IsBackup => BackupPattern.IsMatch(name);
        private bool _hasLoaded = false;
        public bool HasLoaded
        {
            get => _hasLoaded;
            set => SetProperty(ref _hasLoaded, value);
        }
        public string FamilyName => Path.GetFileNameWithoutExtension(IsBackup ? name.Substring(0, name.LastIndexOf('.', name.LastIndexOf('.') - 1)) : name);
        public FileSingle(FileInfo fileInfo)
        {
            fullName = fileInfo.FullName.ToLower();
            name = fileInfo.Name;
            directoryName = fileInfo.Directory?.Name ?? "";
            length = fileInfo.Length;

            try
            {
                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
                Version = basicFileInfo.IsSavedInLaterVersion ? "版本太高" : basicFileInfo.Format;
            }
            catch (Exception)
            {
                // 解析基本信息失败时默认保持 "读取失败"
            }

            string jpgPath = Path.ChangeExtension(fileInfo.FullName, ".jpg");
            HasJpgFile = File.Exists(jpgPath);
        }

        // Converter逻辑已在模型内封装，XAML中直接绑定此属性即可
        public string DisplaySize
        {
            get
            {
                if (length >= 1048576) // 1024 * 1024
                    return $"{length / 1048576:N1} MB";
                else if (length >= 1024)
                    return $"{length / 1024:N1} KB";
                else
                    return $"{length} Bytes";
            }
        }
    }
    public class Dirs : ObserverableObject
    {
        public DirectoryInfo Info { get; set; }
        public Dirs(DirectoryInfo info)
        {
            Info = info;
        }
        public IEnumerable<FileInfo> Files => Info.GetFiles();
        private ObservableCollection<Dirs> _directories;
        public ObservableCollection<Dirs> Directories
        {
            get
            {
                if (_directories == null)
                {
                    _directories = new ObservableCollection<Dirs>();
                    try
                    {
                        foreach (var dir in Info.GetDirectories("*", SearchOption.TopDirectoryOnly))
                        {
                            _directories.Add(new Dirs(dir));
                        }
                    }
                    catch (UnauthorizedAccessException) { /* 忽略权限受限的文件夹 */ }
                }
                return _directories;
            }
        }
    }

    //public class FileSingle : ObserverableObject
    //{
    //    public string fullName { get; set; }
    //    public string name { get; set; }
    //    public string directoryName { get; set; }
    //    public double length { get; set; }
    //    public string Version { get; set; } = "读取失败";
    //    public bool HasJpgFile { get; set; } = false;
    //    public FileSingle(FileInfo fileInfo)
    //    {
    //        fullName = fileInfo.FullName.ToLower();
    //        name = fileInfo.Name;
    //        directoryName = fileInfo.Directory.Name;
    //        length = fileInfo.Length;
    //        try
    //        {
    //            BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
    //            Version = basicFileInfo.IsSavedInLaterVersion ? "版本太高" : basicFileInfo.Format;
    //        }
    //        catch (Exception)
    //        {
    //        }
    //        string jpgPath = System.IO.Path.ChangeExtension(fileInfo.FullName, ".jpg");
    //        if (File.Exists(jpgPath))
    //        {
    //            HasJpgFile = true;
    //        }
    //    }
    //    // 用于显示文件大小的属性
    //    public string DisplaySize
    //    {
    //        get
    //        {
    //            if (length >= 1024 * 1024) // 大于等于 1 MB
    //            {
    //                return $"{length / (1024 * 1024):N1} MB";
    //            }
    //            else if (length >= 1024) // 大于等于 1 KB
    //            {
    //                return $"{length / 1024:N1} KB";
    //            }
    //            else // 小于 1 KB
    //            {
    //                return $"{length} Bytes";
    //            }
    //        }
    //    }
    //    //要把ViewModel中Converter逻辑放到FileSingleModel属性里
    //}
    //public class Dirs : ObserverableObject
    //{
    //    public DirectoryInfo Info { get; set; }
    //    public Dirs(DirectoryInfo info)
    //    {
    //        Info = info;
    //    }
    //    public IEnumerable<FileInfo> Files
    //    {
    //        get => Info.GetFiles();
    //    }
    //    private ObservableCollection<Dirs> _directories;
    //    public ObservableCollection<Dirs> Directories
    //    {
    //        get
    //        {
    //            if (_directories == null)
    //            {
    //                _directories = new ObservableCollection<Dirs>();
    //                foreach (var dir in Info.GetDirectories("*", SearchOption.TopDirectoryOnly))
    //                {
    //                    _directories.Add(new Dirs(dir));
    //                }
    //            }
    //            return _directories;
    //        }
    //    }
    //}
}
