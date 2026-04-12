using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Color = Autodesk.Revit.DB.Color;

namespace CreatePipe.Form
{
    /// <summary>
    /// FamilyManagerSubView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyManagerSubView : Window
    {
        public FamilyManagerSubView(UIApplication uiApp, Dirs dirs, BaseExternalHandler handler)
        {
            InitializeComponent();
            this.DataContext = new FamilyThumbExportViewModel(uiApp, dirs, handler);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void numberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text) || !int.TryParse(textBox.Text, out _))
            {
                textBox.Text = "600";
            }
        }
    }
    public class FamilyThumbExportViewModel : ObserverableObject
    {
        private UIApplication _application;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        private readonly Dirs _dirs;
        public FamilyThumbExportViewModel(UIApplication uiapp, Dirs dirs, BaseExternalHandler handler)
        {
            _application = uiapp;
            _dirs = dirs;
            _externalHandler = handler;
            SelectedDisplayStyle = DisplayStyleList[1];
            SelectDetailLevel = DetailLevelList[2];
            SelectShowHideHost = IsHideHostList[0];
            SelectWhiteBackGroudnd = IsWhiteBackList[0];
            ImagePixel = 600;
        }

        // 绑定属性（使用简化的属性声明）
        public List<string> DisplayStyleList => new List<string> { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
        public List<string> DetailLevelList => new List<string> { "粗略", "中等", "详细" };
        public List<string> IsHideHostList => new List<string> { "是", "否" };
        public List<string> IsWhiteBackList => new List<string> { "白色", "黑色" };
        private string _selectedDisplayStyle;
        public string SelectedDisplayStyle { get => _selectedDisplayStyle; set => SetProperty(ref _selectedDisplayStyle, value); }
        private string _selectDetailLevel;
        public string SelectDetailLevel { get => _selectDetailLevel; set => SetProperty(ref _selectDetailLevel, value); }
        private string _selectShowHideHost;
        public string SelectShowHideHost { get => _selectShowHideHost; set => SetProperty(ref _selectShowHideHost, value); }
        private string _selectWhiteBackGroudnd;
        public string SelectWhiteBackGroudnd { get => _selectWhiteBackGroudnd; set => SetProperty(ref _selectWhiteBackGroudnd, value); }
        private int _imagePixel;
        public int ImagePixel { get => _imagePixel; set => SetProperty(ref _imagePixel, value); }
        public ICommand ExportSnapCommand => new RelayCommand<object>(ExportSnap);
        private void ExportSnap(object obj)
        {
            DisplayStyle viewDisplayStyle;
            switch (SelectedDisplayStyle)
            {
                case "线框":
                    viewDisplayStyle = DisplayStyle.Wireframe;
                    break;
                case "隐藏线":
                    viewDisplayStyle = DisplayStyle.HLR;
                    break;
                case "着色":
                    viewDisplayStyle = DisplayStyle.ShadingWithEdges;
                    break;
                case "一致的颜色":
                    viewDisplayStyle = DisplayStyle.FlatColors;
                    break;
                default:
                    viewDisplayStyle = DisplayStyle.RealisticWithEdges;
                    break;
            }
            ViewDetailLevel detailLevel;
            switch (SelectDetailLevel)
            {
                case "粗略":
                    detailLevel = ViewDetailLevel.Coarse;
                    break;
                case "中等":
                    detailLevel = ViewDetailLevel.Medium;
                    break;
                default:
                    detailLevel = ViewDetailLevel.Fine;
                    break;
            }
            bool isHideHost = SelectShowHideHost == "是";
            bool isWhiteBack = SelectWhiteBackGroudnd == "白色";
            List<FileInfo> familyList = GetFamilyList();
            // 2. 核心：必须包裹在 ExternalHandler 中执行 Revit API
            _externalHandler.Run(app =>
            {
                // 记录用户原有的背景色，执行完毕后恢复
                Color originalColor = app.Application.BackgroundColor;
                app.Application.BackgroundColor = isWhiteBack ? new Color(255, 255, 255) : new Color(0, 0, 0);
                try
                {
                    foreach (FileInfo file in familyList)
                    {
                        Document bgDoc = null;
                        try
                        {
                            // 使用后台静默打开，极大地提升速度且不闪屏
                            bgDoc = app.Application.OpenDocumentFile(file.FullName);
                            // 获取3D视图
                            var view3D = new FilteredElementCollector(bgDoc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
                            if (view3D == null) continue; // 没有三维视图则跳过
                            // 开启后台文档事务
                            using (Transaction t = new Transaction(bgDoc, "设置导出视图"))
                            {
                                t.Start();
                                view3D.DetailLevel = detailLevel;
                                view3D.DisplayStyle = viewDisplayStyle;

                                // 视角调整
                                if (!view3D.IsLocked) view3D.OrientTo(new XYZ(-0.577, 0.577, -0.577));
                                // 隐藏主体
                                if (isHideHost)
                                {
                                    var hostIds = GetHostElementIds(bgDoc, view3D.Id);
                                    if (hostIds.Any())
                                    {
                                        // 因为是后台打开且最后不保存(Close(false))，直接硬隐藏比临时隐藏更安全有效
                                        view3D.HideElements(hostIds);
                                    }
                                }
                                t.Commit();
                            }
                            // 导出配置
                            ImageExportOptions option = new ImageExportOptions
                            {
                                FilePath = file.FullName, // 会自动替换拓展名
                                ZoomType = ZoomFitType.FitToPage,
                                PixelSize = ImagePixel,
                                ImageResolution = ImageResolution.DPI_300,
                                ExportRange = ExportRange.SetOfViews,
                                HLRandWFViewsFileType = ImageFileType.JPEGLossless,
                                ShadowViewsFileType = ImageFileType.JPEGLossless
                            };
                            // 指定只导出我们修改过的视图
                            option.SetViewsAndSheets(new List<ElementId> { view3D.Id });
                            bgDoc.ExportImage(option);
                        }
                        catch (Exception)
                        {
                            // 单个族报错不影响其他族的导出，此处可做日志记录
                        }
                        finally
                        {
                            // 确保每个文档处理完后必然被关闭，且不保存修改
                            bgDoc?.Close(false);
                        }
                    }

                    TaskDialog.Show("提示", "缩略图批量生成完成！");
                }
                finally
                {
                    // 无论成功还是报错，还原用户原本的背景颜色
                    app.Application.BackgroundColor = originalColor;
                }
            });
        }
        // 优化：一次性过滤所有主体元素
        private List<ElementId> GetHostElementIds(Document doc, ElementId viewId)
        {
            var hostIds = new List<ElementId>();
            // 1. 使用多类别过滤器一次性收集（极速）
            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,BuiltInCategory.OST_Roofs,BuiltInCategory.OST_Ceilings,BuiltInCategory.OST_Floors
            };
            var multiCatFilter = new ElementMulticategoryFilter(categories);
            hostIds.AddRange(new FilteredElementCollector(doc, viewId).WherePasses(multiCatFilter).ToElementIds());
            // 2. Extrusion 属于类过滤，单独查一次
            hostIds.AddRange(new FilteredElementCollector(doc, viewId).OfClass(typeof(Extrusion)).ToElementIds());
            return hostIds;
        }
        private List<FileInfo> GetFamilyList()
        {
            List<FileInfo> files = new List<FileInfo>();
            try
            {
                string[] filePaths = Directory.GetFiles(_dirs.Info.FullName, "*.rfa");
                foreach (string filePath in filePaths)
                {
                    // 过滤掉备份文件 .0001.rfa
                    if (System.Text.RegularExpressions.Regex.IsMatch(filePath, @"\.[0-9]{3,4}\.rfa$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        continue;
                    files.Add(new FileInfo(filePath));
                }
            }
            catch { }
            return files;
        }
    }

    //public class FamilyThumbExportViewModel : ObserverableObject
    //{
    //    UIApplication application;
    //    Dirs Dirs;
    //    public FileSystemInfo FamilyPath { get; set; }
    //    public FamilyThumbExportViewModel(UIApplication uiapp, Dirs dirs)
    //    {
    //        application = uiapp;
    //        Dirs = dirs;
    //        DisplayStyleList = new List<string>() { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
    //        SelectedDisplayStyle = DisplayStyleList[1];
    //        DetailLevelList = new List<string>() { "粗略", "中等", "详细" };
    //        SelectDetailLevel = DetailLevelList[2];
    //        IsHideHostList = new List<string>() { "是", "否" };
    //        SelectShowHideHost = IsHideHostList[0];
    //        IsWhiteBack = new List<string>() { "白色", "黑色" };
    //        SelectWhiteBackGroudnd = IsWhiteBack[0];
    //        ImagePixel = 600;
    //    }
    //    public BaseBindingCommand ExportSnapCommand => new BaseBindingCommand(ExportSnap);

    //    private void ExportSnap(object obj)
    //    {
    //        List<FileInfo> familyList = GetFamilyList();
    //        switch (SelectedDisplayStyle)
    //        {
    //            case "线框":
    //                ViewDisplayStyle = DisplayStyle.Wireframe;
    //                break;
    //            case "隐藏线":
    //                ViewDisplayStyle = DisplayStyle.HLR;
    //                break;
    //            case "着色":
    //                ViewDisplayStyle = DisplayStyle.ShadingWithEdges;
    //                break;
    //            case "一致的颜色":
    //                ViewDisplayStyle = DisplayStyle.FlatColors;
    //                break;
    //            default:
    //                ViewDisplayStyle = DisplayStyle.RealisticWithEdges;
    //                break;
    //        }
    //        switch (SelectDetailLevel)
    //        {
    //            case "粗略":
    //                DetailLevel = ViewDetailLevel.Coarse;
    //                break;
    //            case "中等":
    //                DetailLevel = ViewDetailLevel.Medium;
    //                break;
    //            default:
    //                DetailLevel = ViewDetailLevel.Fine;
    //                break;
    //        }
    //        switch (SelectShowHideHost)
    //        {
    //            case "是":
    //                is_HideHost = true;
    //                break;
    //            default:
    //                is_HideHost = false;
    //                break;
    //        }
    //        switch (SelectWhiteBackGroudnd)
    //        {
    //            case "白色":
    //                is_WhiteBackGroudnd = true;
    //                break;
    //            default:
    //                is_WhiteBackGroudnd = false;
    //                break;
    //        }
    //        //处理背景
    //        if (!is_WhiteBackGroudnd)
    //        {
    //            application.Application.BackgroundColor = new Color(0, 0, 0);
    //        }
    //        else application.Application.BackgroundColor = new Color(255, 255, 255);

    //        //处理图片
    //        Document doc = null;
    //        foreach (FileInfo file in familyList)
    //        {
    //            try
    //            {
    //                UIDocument newUIDoc = application.OpenAndActivateDocument(file.FullName);
    //                Document newDoc = newUIDoc.Document;
    //                if (doc != null)
    //                    doc.Close(false);
    //                doc = newDoc;
    //                FilteredElementCollector viewCollector = new FilteredElementCollector(newDoc).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View3D));
    //                if (viewCollector.Count() == 0)
    //                {
    //                    return;//省去导出二维族的方法
    //                }
    //                else
    //                {
    //                    ThreeExportImage(newUIDoc, newDoc, viewCollector);
    //                }
    //                ImageExportOptions option = new ImageExportOptions();
    //                option.FilePath = file.FullName;
    //                option.ZoomType = ZoomFitType.FitToPage;
    //                option.PixelSize = ImagePixel;
    //                option.ImageResolution = ImageResolution.DPI_300;
    //                //ThinLinesOptions.AreThinLinesEnabled = true;
    //                newDoc.ExportImage(option);
    //            }
    //            catch (Exception ex)
    //            {
    //                TaskDialog.Show("tt", "错误信息info" + ex.Message);
    //            }
    //        }
    //        TaskDialog.Show("tt", "转换完成，请注意因API缺陷，部分线宽尚无法控制");
    //    }
    //    private void ThreeExportImage(UIDocument newUIDoc, Document newDoc, FilteredElementCollector viewCollector)
    //    {
    //        try
    //        {
    //            View3D view = (View3D)viewCollector.First();
    //            newUIDoc.ActiveView = view;
    //            application.ActiveUIDocument.Document.NewTransaction(() =>
    //            {
    //                //view.OrientTo(new XYZ(1, 0, 1));
    //                view.OrientTo(new XYZ(-0.577350269189626, 0.577350269189626, -0.577350269189626));
    //                view.DetailLevel = DetailLevel;
    //                view.DisplayStyle = ViewDisplayStyle;
    //                if (is_HideHost == true)
    //                {
    //                    ICollection<ElementId> list = HostFilter(newDoc, view.Id);
    //                    if (list.Count > 0)
    //                    {
    //                        view.HideElementsTemporary(list);
    //                    }
    //                }
    //            }, "三维图片导出");
    //        }
    //        catch (Exception ex)
    //        {
    //            TaskDialog.Show("tt", "错误信息info" + ex.Message);
    //        }
    //    }
    //    //隐蔽主体
    //    private ICollection<ElementId> HostFilter(Document doc, ElementId viewId)
    //    {
    //        ICollection<ElementId> hostList = new List<ElementId>();

    //        FilteredElementCollector collector = new FilteredElementCollector(doc, viewId);
    //        collector.OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Wall));
    //        if (collector.Count() > 0)
    //        {
    //            foreach (Element ele in collector)
    //            {
    //                hostList.Add(ele.Id);
    //            }
    //        }
    //        collector = new FilteredElementCollector(doc, viewId);
    //        collector.OfCategory(BuiltInCategory.OST_Roofs).OfClass(typeof(RoofBase));
    //        if (collector.Count() > 0)
    //        {
    //            foreach (Element ele in collector)
    //            {
    //                hostList.Add(ele.Id);
    //            }
    //        }
    //        collector = new FilteredElementCollector(doc, viewId);
    //        collector.OfCategory(BuiltInCategory.OST_Ceilings).OfClass(typeof(Ceiling));
    //        if (collector.Count() > 0)
    //        {
    //            foreach (Element ele in collector)
    //            {
    //                hostList.Add(ele.Id);
    //            }
    //        }
    //        collector = new FilteredElementCollector(doc, viewId);
    //        collector.OfCategory(BuiltInCategory.OST_Floors).OfClass(typeof(Floor));
    //        if (collector.Count() > 0)
    //        {
    //            foreach (Element ele in collector)
    //            {
    //                hostList.Add(ele.Id);
    //            }
    //        }
    //        collector = new FilteredElementCollector(doc, viewId);
    //        collector.OfClass(typeof(Extrusion));
    //        if (collector.Count() > 0)
    //        {
    //            foreach (Element ele in collector)
    //            {
    //                hostList.Add(ele.Id);
    //            }
    //        }
    //        return hostList;
    //    }
    //    private List<FileInfo> GetFamilyList()
    //    {
    //        //这里是个遍历操作，使得选定族文件夹下面所有的族都能导出
    //        DirectoryInfo dir = new DirectoryInfo(Dirs.Info.FullName);
    //        List<FileInfo> files = new List<FileInfo>();
    //        string[] filePaths = Directory.GetFiles(dir.FullName, "*.rfa");
    //        foreach (string filePath in filePaths)
    //        {
    //            FileInfo fileInfo = new FileInfo(filePath);
    //            try
    //            {
    //                BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
    //                files.Add(fileInfo);
    //            }
    //            catch (Exception)
    //            {
    //            }
    //        }
    //        return files;
    //    }
    //    public DisplayStyle ViewDisplayStyle { get; set; }
    //    public ViewDetailLevel DetailLevel { get; set; }
    //    public bool is_WhiteBackGroudnd { get; set; }
    //    public bool is_HideHost { get; set; }
    //    private int imagePixel;
    //    public int ImagePixel { get => imagePixel; set => imagePixel = value; }
    //    private string _selectedDisplayStyle;
    //    public string SelectedDisplayStyle
    //    {
    //        get => _selectedDisplayStyle;
    //        set => SetProperty(ref _selectedDisplayStyle, value);
    //    }
    //    private string selectDetailLevel;
    //    public string SelectDetailLevel
    //    {
    //        get => selectDetailLevel;
    //        set => SetProperty(ref selectDetailLevel, value);
    //    }
    //    private string selectShowHideHost;
    //    public string SelectShowHideHost
    //    {
    //        get => selectShowHideHost;
    //        set => SetProperty(ref selectShowHideHost, value);
    //    }
    //    private string selectWhiteBackGroudnd;
    //    public string SelectWhiteBackGroudnd
    //    {
    //        get => selectWhiteBackGroudnd;
    //        set => SetProperty(ref selectWhiteBackGroudnd, value);
    //    }
    //    public List<string> IsHideHostList { get; set; }
    //    public List<string> DisplayStyleList { get; set; }
    //    public List<string> DetailLevelList { get; set; }
    //    public List<string> IsWhiteBack { get; set; }
    //}
}
