using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form.UserControls
{
    public class EnhancedTreeView : TreeView
    {
        public static readonly DependencyProperty CurrentItemProperty = DependencyProperty.Register("CurrentItem", typeof(object), typeof(EnhancedTreeView), new FrameworkPropertyMetadata
        {
            BindsTwoWayByDefault = true
        });
        public object CurrentItem
        {
            get => GetValue(CurrentItemProperty);
            set => SetValue(CurrentItemProperty, value);
        }
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            UpdateLayout();
        }
        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            CurrentItem = SelectedItem;
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
    //        directoryName = fileInfo.Directory?.Name ?? "";
    //        length = fileInfo.Length;

    //        try
    //        {
    //            BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
    //            Version = basicFileInfo.IsSavedInLaterVersion ? "版本太高" : basicFileInfo.Format;
    //        }
    //        catch (Exception)
    //        {
    //            // 解析基本信息失败时默认保持 "读取失败"
    //        }

    //        string jpgPath = Path.ChangeExtension(fileInfo.FullName, ".jpg");
    //        HasJpgFile = File.Exists(jpgPath);
    //    }

    //    // Converter逻辑已在模型内封装，XAML中直接绑定此属性即可
    //    public string DisplaySize
    //    {
    //        get
    //        {
    //            if (length >= 1048576) // 1024 * 1024
    //                return $"{length / 1048576:N1} MB";
    //            else if (length >= 1024)
    //                return $"{length / 1024:N1} KB";
    //            else
    //                return $"{length} Bytes";
    //        }
    //    }
    //}
    //public class Dirs : ObserverableObject
    //{
    //    public DirectoryInfo Info { get; set; }
    //    public Dirs(DirectoryInfo info)
    //    {
    //        Info = info;
    //    }
    //    public IEnumerable<FileInfo> Files => Info.GetFiles();
    //    private ObservableCollection<Dirs> _directories;
    //    public ObservableCollection<Dirs> Directories
    //    {
    //        get
    //        {
    //            if (_directories == null)
    //            {
    //                _directories = new ObservableCollection<Dirs>();
    //                try
    //                {
    //                    foreach (var dir in Info.GetDirectories("*", SearchOption.TopDirectoryOnly))
    //                    {
    //                        _directories.Add(new Dirs(dir));
    //                    }
    //                }
    //                catch (UnauthorizedAccessException) { /* 忽略权限受限的文件夹 */ }
    //            }
    //            return _directories;
    //        }
    //    }
    //}
}
