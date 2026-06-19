using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CreatePipe.models
{
    public class FileSingle : ObserverableObject
    {
        private static readonly Regex BackupPattern = new Regex(@"\.[0-9]{3,4}\.r(vt|fa|te)$", RegexOptions.IgnoreCase);
        public string fullName { get; set; }
        public string name { get; set; }

        // 新增属性，用于绑定UI
        private string _categoryName = "[待加载]";
        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }
        private bool _hasLoadedDetails = false;
        public bool HasLoadedDetails
        {
            get => _hasLoadedDetails;
            private set => SetProperty(ref _hasLoadedDetails, value); // 私有 set，只能内部修改
        }
        //public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();

        private string _partTypeName = "[待加载]";
        public string PartTypeName
        {
            get => _partTypeName;
            set => SetProperty(ref _partTypeName, value);
        }
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
        //public FileSingle(FileInfo fileInfo, Application _app)
        public FileSingle(FileInfo fileInfo)
        {
            fullName = fileInfo.FullName.ToLower();
            name = fileInfo.Name;
            directoryName = fileInfo.Directory?.Name ?? "";
            length = fileInfo.Length;
            string jpgPath = Path.ChangeExtension(fileInfo.FullName, ".jpg");
            HasJpgFile = File.Exists(jpgPath);

            try
            {
                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
                Version = basicFileInfo.Format;
            }
            catch (Exception)
            {
                Version = "版本太高";
            }
        }
        public async Task LoadDetailsAsync(UIApplication uiapp)
        {
            if (HasLoadedDetails || Version == "版本太高")
            {
                return;
            }
            CategoryName = "正在加载...";
            PartTypeName = "正在加载...";
            try
            {
                // await 在这里暂停了 LoadDetailsAsync 的执行，
                // 但把UI线程解放了，使其可以继续响应用户操作。
                var details = await GetFamilyDetailsViaExternalEventAsync(uiapp.Application);
                // 当 GetFamilyDetails... 的 Task 完成后，代码从这里继续执行，
                // 并且是在原始的UI线程上下文中，所以可以直接更新属性。
                CategoryName = details.Category;
                PartTypeName = details.PartType;
            }
            catch (Exception)
            {
                CategoryName = "加载失败";
                PartTypeName = "读取错误";
            }
            finally
            {
                HasLoadedDetails = true;
            }
        }
        // 辅助方法，使用 TaskCompletionSource 作为桥梁
        private Task<(string Category, string PartType)> GetFamilyDetailsViaExternalEventAsync(Application App)
        {
            var tcs = new TaskCompletionSource<(string, string)>();
            Document familyDoc = null;
            try
            {
                // 这部分代码将在Revit主线程中安全执行
                familyDoc = App.OpenDocumentFile(fullName);
                // 1. 获取 Category
                string categoryResult = familyDoc.OwnerFamily.FamilyCategory.Name;
                // 2. 获取 PartType
                string partTypeResult;
                if (familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE) != null)
                {
                    partTypeResult = familyDoc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsValueString();
                    //// 使用 AsValueString() 来获取枚举值的本地化名称，如 "弯头"
                    //PartTypeName = familyManager.CurrentType.AsValueString(partTypeParam) ?? "无";
                }
                else
                {
                    partTypeResult = "不适用"; // 例如，常规模型族没有 PartType
                }
                //{ new CategoryItemWrapper("常规模型", -2000151), new List<PartType> { PartType.Normal } },
                //            { new CategoryItemWrapper("专用设备", -2001350), new List<PartType> { PartType.Normal } },
                //{ new CategoryItemWrapper("风道末端", -2008013), new List<PartType> { PartType.Normal } },
                //{ new CategoryItemWrapper("桥架配件", -2008126), new List<PartType> { PartType.ChannelCableTrayCross, PartType.ChannelCableTrayElbow, PartType.ChannelCableTrayMultiPort, PartType.ChannelCableTrayOffset, PartType.ChannelCableTrayTee, PartType.ChannelCableTrayTransition, PartType.ChannelCableTrayUnion, PartType.ChannelCableTrayVerticalElbow, PartType.LadderCableTrayCross, PartType.LadderCableTrayElbow, PartType.LadderCableTrayMultiPort, PartType.LadderCableTrayOffset, PartType.LadderCableTrayTee, PartType.LadderCableTrayTransition, PartType.LadderCableTrayUnion, PartType.LadderCableTrayVerticalElbow } },
                //{ new CategoryItemWrapper("通讯设备", -2008081), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
                //{ new CategoryItemWrapper("线管配件", -2008128), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.JunctionBoxElbow, PartType.MultiPort, PartType.Tee, PartType.Transition, PartType.Union } },
                //{ new CategoryItemWrapper("数据设备", -2008083), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
                //{ new CategoryItemWrapper("风管附件", -2008016), new List<PartType> { PartType.AttachesTo, PartType.BreaksInto, PartType.Damper } },
                //{ new CategoryItemWrapper("风管管件", -2008010), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.LateralCross, PartType.LateralTee, PartType.MultiPort, PartType.Offset, PartType.Pants, PartType.TapAdjustable, PartType.TapPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
                //{ new CategoryItemWrapper("电气设备", -2001040), new List<PartType> { PartType.EquipmentSwitch, PartType.OtherPanel, PartType.PanelBoard, PartType.SwitchBoard, PartType.Transformer } },
                //{ new CategoryItemWrapper("电气装置", -2001060), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
                //{ new CategoryItemWrapper("火灾报警设备", -2008085), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
                //{ new CategoryItemWrapper("照明设备（开关）", -2008087), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
                //{ new CategoryItemWrapper("照明设备 (灯具)", -2001120), new List<PartType> { PartType.Normal, PartType.JunctionBox } },
                //{ new CategoryItemWrapper("机械设备", -2001140), new List<PartType> { PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor, PartType.Normal, PartType.ValveBreaksInto } },
                //{ new CategoryItemWrapper("护士呼叫设备", -2008077), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
                //{ new CategoryItemWrapper("管道附件", -2008055), new List<PartType> { PartType.Normal, PartType.AttachesTo, PartType.BreaksInto, PartType.EndCap, PartType.InlineSensor, PartType.Sensor, PartType.ValveBreaksInto, PartType.ValveNormal } },
                //{ new CategoryItemWrapper("管道管件", -2008049), new List<PartType> { PartType.Cap, PartType.Cross, PartType.Elbow, PartType.PipeFlange, PartType.LateralCross, PartType.LateralTee, PartType.PipeMechanicalCoupling, PartType.MultiPort, PartType.SpudAdjustable, PartType.SpudPerpendicular, PartType.Tee, PartType.Transition, PartType.Union, PartType.Wye } },
                //{ new CategoryItemWrapper("卫浴装置", -2001160), new List<PartType> { PartType.Normal } },
                //{ new CategoryItemWrapper("安防设备", -2008079), new List<PartType> { PartType.Normal, PartType.JunctionBox, PartType.Switch } },
                //{ new CategoryItemWrapper("喷头", -2008099), new List<PartType> { PartType.Normal } },
                //{ new CategoryItemWrapper("电话设备", -2008075), new List<PartType> { PartType.Normal, PartType.JunctionBox } }
                // 成功后，完成Task并设置结果
                tcs.SetResult((categoryResult, partTypeResult));
            }
            catch (Exception ex)
            {
                // 失败后，将Task设置为异常状态
                tcs.SetException(ex);
            }
            finally
            {
                familyDoc?.Close(false);
            }
            // 返回这个“承诺”会完成的Task
            return tcs.Task;
        }
        // 异步加载族的详细信息 (Category 和 PartType)
        //public async Task LoadDetailsAsync(Application App, BaseExternalHandler externalHandler)
        ////public void LoadDetailsAsync(Application App)
        //{
        //    if (HasLoadedDetails || Version == "版本太高")
        //    {
        //        return;
        //    }
        //    CategoryName = "正在加载...";
        //    PartTypeName = "正在加载...";
        //    try
        //    {
        //        // await 在这里暂停了 LoadDetailsAsync 的执行，
        //        // 但把UI线程解放了，使其可以继续响应用户操作。
        //        var details = await GetFamilyDetailsViaExternalEventAsync(App, externalHandler);
        //        // 当 GetFamilyDetails... 的 Task 完成后，代码从这里继续执行，
        //        // 并且是在原始的UI线程上下文中，所以可以直接更新属性。
        //        CategoryName = details.Category;
        //        PartTypeName = details.PartType;
        //    }
        //    catch (Exception)
        //    {
        //        CategoryName = "加载失败";
        //        PartTypeName = "读取错误";
        //    }
        //    finally
        //    {
        //        HasLoadedDetails = true;
        //    }
        //    //// 如果已经加载过，或者文件版本太高无法打开，则直接返回
        //    //if (HasLoadedDetails || Version == "版本太高")
        //    //{
        //    //    return;
        //    //}
        //    //Document familyDoc = null;
        //    //try
        //    //{
        //    //    await Task.Run(() =>
        //    //    {
        //    //        //ExternalHandler.Run(app =>
        //    //        //{
        //    //        // 打开族文件作为文档
        //    //        familyDoc = App.OpenDocumentFile(fullName);
        //    //        if (familyDoc == null || !familyDoc.IsFamilyDocument)
        //    //        {
        //    //            throw new InvalidDataException("无法作为族文档打开。");
        //    //        }
        //    //        // 找到族本身（在族文档中，族就是所有者）
        //    //        if (familyDoc.OwnerFamily != null)
        //    //        {
        //    //            // 1. 获取 Category
        //    //            //CategoryName = familyDoc.OwnerFamily.FamilyCategory?.Name ?? "未指定";
        //    //            CategoryName = familyDoc.OwnerFamily.FamilyCategory.Name;
        //    //            // 2. 获取 PartType
        //    //            FamilyManager familyManager = familyDoc.FamilyManager;
        //    //            BuiltInParameter partTypeParam = BuiltInParameter.FAMILY_CONTENT_PART_TYPE;
        //    //            if (familyManager.get_Parameter(partTypeParam) != null)
        //    //            {
        //    //                PartTypeName = "test";
        //    //                //// 使用 AsValueString() 来获取枚举值的本地化名称，如 "弯头"
        //    //                //PartTypeName = familyManager.CurrentType.AsValueString(partTypeParam) ?? "无";
        //    //            }
        //    //            else
        //    //            {
        //    //                PartTypeName = "不适用"; // 例如，常规模型族没有 PartType
        //    //            }
        //    //        }
        //    //        else
        //    //        {
        //    //            throw new InvalidDataException("在文档中未找到族定义。");
        //    //        }
        //    //        //});
        //    //    });
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    // 如果加载失败，给用户一个反馈
        //    //    CategoryName = "加载失败";
        //    //    PartTypeName = $"错误: {ex.GetType().Name}";
        //    //}
        //    //finally
        //    //{
        //    //    // 关键一步：无论成功与否，都必须关闭文档，释放文件句柄和内存！
        //    //    familyDoc?.Close(false);
        //    //    HasLoadedDetails = true; // 标记为已尝试加载（无论成功失败）
        //    //}
        //}

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
