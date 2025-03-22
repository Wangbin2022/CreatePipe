using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CreatePipe.WpfDirectoryTreeView
{
    public class FamilyThumbExportViewModel : ObserverableObject
    {
        UIApplication application;
        Dirs Dirs;
        public FileSystemInfo FamilyPath { get; set; }
        public FamilyThumbExportViewModel(UIApplication uiapp, Dirs dirs)
        {
            application = uiapp;
            Dirs = dirs;
            DisplayStyleList = new List<string>() { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
            SelectedDisplayStyle = DisplayStyleList[1];
            DetailLevelList = new List<string>() { "粗略", "中等", "详细" };
            SelectDetailLevel = DetailLevelList[2];
            IsHideHostList = new List<string>() { "是", "否" };
            SelectShowHideHost = IsHideHostList[0];
            IsWhiteBack = new List<string>() { "白色", "黑色" };
            SelectWhiteBackGroudnd = IsWhiteBack[0];
            ImagePixel = 600;
        }
        public BaseBindingCommand ExportSnapCommand => new BaseBindingCommand(ExportSnap);

        private void ExportSnap(object obj)
        {
            List<FileInfo> familyList = GetFamilyList();
            switch (SelectedDisplayStyle)
            {
                case "线框":
                    ViewDisplayStyle = DisplayStyle.Wireframe;
                    break;
                case "隐藏线":
                    ViewDisplayStyle = DisplayStyle.HLR;
                    break;
                case "着色":
                    ViewDisplayStyle = DisplayStyle.ShadingWithEdges;
                    break;
                case "一致的颜色":
                    ViewDisplayStyle = DisplayStyle.FlatColors;
                    break;
                default:
                    ViewDisplayStyle = DisplayStyle.RealisticWithEdges;
                    break;
            }
            switch (SelectDetailLevel)
            {
                case "粗略":
                    DetailLevel = ViewDetailLevel.Coarse;
                    break;
                case "中等":
                    DetailLevel = ViewDetailLevel.Medium;
                    break;
                default:
                    DetailLevel = ViewDetailLevel.Fine;
                    break;
            }
            switch (SelectShowHideHost)
            {
                case "是":
                    is_HideHost = true;
                    break;
                default:
                    is_HideHost = false;
                    break;
            }
            switch (SelectWhiteBackGroudnd)
            {
                case "白色":
                    is_WhiteBackGroudnd = true;
                    break;
                default:
                    is_WhiteBackGroudnd = false;
                    break;
            }
            //处理背景
            if (!is_WhiteBackGroudnd)
            {
                application.Application.BackgroundColor = new Color(0, 0, 0);
            }
            else application.Application.BackgroundColor = new Color(255, 255, 255);

            //处理图片
            Document doc = null;
            foreach (FileInfo file in familyList)
            {
                try
                {
                    UIDocument newUIDoc = application.OpenAndActivateDocument(file.FullName);
                    Document newDoc = newUIDoc.Document;
                    if (doc != null)
                        doc.Close(false);
                    doc = newDoc;
                    FilteredElementCollector viewCollector = new FilteredElementCollector(newDoc).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View3D));
                    if (viewCollector.Count() == 0)
                    {
                        return;//省去导出二维族的方法
                    }
                    else
                    {
                        ThreeExportImage(newUIDoc, newDoc, viewCollector);
                    }
                    ImageExportOptions option = new ImageExportOptions();
                    option.FilePath = file.FullName;
                    option.ZoomType = ZoomFitType.FitToPage;
                    option.PixelSize = ImagePixel;
                    option.ImageResolution = ImageResolution.DPI_300;
                    //ThinLinesOptions.AreThinLinesEnabled = true;
                    newDoc.ExportImage(option);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", "错误信息info" + ex.Message);
                }
            }
            TaskDialog.Show("tt", "转换完成，请注意因API缺陷，部分线宽尚无法控制");
        }
        private void ThreeExportImage(UIDocument newUIDoc, Document newDoc, FilteredElementCollector viewCollector)
        {
            try
            {
                View3D view = viewCollector.First() as View3D;
                newUIDoc.ActiveView = view;
                XmlDoc.Instance.Task.Run(app =>
                {
                    application.ActiveUIDocument.Document.NewTransaction(() =>
                    {
                        //view.OrientTo(new XYZ(1, 0, 1));
                        view.OrientTo(new XYZ(-0.577350269189626, 0.577350269189626, -0.577350269189626));
                        view.DetailLevel = DetailLevel;
                        view.DisplayStyle = ViewDisplayStyle;
                        if (is_HideHost == true)
                        {
                            ICollection<ElementId> list = HostFilter(newDoc, view.Id);
                            if (list.Count > 0)
                            {
                                view.HideElementsTemporary(list);
                            }
                        }
                    }, "三维图片导出");
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", "错误信息info" + ex.Message);
            }
        }
        //隐蔽主体
        private ICollection<ElementId> HostFilter(Document doc, ElementId viewId)
        {
            ICollection<ElementId> hostList = new List<ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(doc, viewId);
            collector.OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Wall));
            if (collector.Count() > 0)
            {
                foreach (Element ele in collector)
                {
                    hostList.Add(ele.Id);
                }
            }
            collector = new FilteredElementCollector(doc, viewId);
            collector.OfCategory(BuiltInCategory.OST_Roofs).OfClass(typeof(RoofBase));
            if (collector.Count() > 0)
            {
                foreach (Element ele in collector)
                {
                    hostList.Add(ele.Id);
                }
            }
            collector = new FilteredElementCollector(doc, viewId);
            collector.OfCategory(BuiltInCategory.OST_Ceilings).OfClass(typeof(Ceiling));
            if (collector.Count() > 0)
            {
                foreach (Element ele in collector)
                {
                    hostList.Add(ele.Id);
                }
            }
            collector = new FilteredElementCollector(doc, viewId);
            collector.OfCategory(BuiltInCategory.OST_Floors).OfClass(typeof(Floor));
            if (collector.Count() > 0)
            {
                foreach (Element ele in collector)
                {
                    hostList.Add(ele.Id);
                }
            }
            collector = new FilteredElementCollector(doc, viewId);
            collector.OfClass(typeof(Extrusion));
            if (collector.Count() > 0)
            {
                foreach (Element ele in collector)
                {
                    hostList.Add(ele.Id);
                }
            }
            return hostList;
        }
        private List<FileInfo> GetFamilyList()
        {
            //这里是个遍历操作，使得选定族文件夹下面所有的族都能导出
            DirectoryInfo dir = new DirectoryInfo(Dirs.Info.FullName);
            List<FileInfo> files = new List<FileInfo>();
            string[] filePaths = Directory.GetFiles(dir.FullName, "*.rfa");
            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                try
                {
                    BasicFileInfo basicFileInfo = BasicFileInfo.Extract(fileInfo.FullName);
                    files.Add(fileInfo);
                }
                catch (Exception)
                {
                }
            }
            return files;
        }
        public DisplayStyle ViewDisplayStyle { get; set; }
        public ViewDetailLevel DetailLevel { get; set; }
        public bool is_WhiteBackGroudnd { get; set; }
        public bool is_HideHost { get; set; }
        private int imagePixel;
        public int ImagePixel { get => imagePixel; set => imagePixel = value; }
        private string _selectedDisplayStyle;
        public string SelectedDisplayStyle
        {
            get => _selectedDisplayStyle;
            set
            {
                _selectedDisplayStyle = value;
                OnPropertyChanged(nameof(SelectedDisplayStyle));
            }
        }
        private string selectDetailLevel;
        public string SelectDetailLevel
        {
            get => selectDetailLevel;
            set
            {
                selectDetailLevel = value;
                OnPropertyChanged(nameof(SelectedDisplayStyle));
            }
        }
        private string selectShowHideHost;
        public string SelectShowHideHost
        {
            get => selectShowHideHost;
            set
            {
                selectShowHideHost = value;
                OnPropertyChanged();
            }
        }
        private string selectWhiteBackGroudnd;
        public string SelectWhiteBackGroudnd
        {
            get => selectWhiteBackGroudnd;
            set
            {
                selectWhiteBackGroudnd = value;
                OnPropertyChanged();
            }
        }
        public List<string> IsHideHostList { get; set; }
        public List<string> DisplayStyleList { get; set; }
        public List<string> DetailLevelList { get; set; }
        public List<string> IsWhiteBack { get; set; }
    }
}
