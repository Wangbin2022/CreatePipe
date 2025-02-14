using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CreatePipe.models
{
    public class BatchFamilyExport_ViewModel : ObserverableObject
    {
        UIApplication application;
        public BatchFamilyExport_ViewModel(UIApplication uiapp)
        {
            application = uiapp;
            ExportSnapCommand = new BaseBindingCommand(ExportSnap);
            GetFolderCommand = new BaseBindingCommand(GetFolder);

            displayStyleList = new List<string>() { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
            detailLevelList = new List<string>() { "粗略", "中等", "详细" };
            isHideHostList = new List<string>() { "是", "否" };
            isWhiteBack = new List<string>() { "白色", "黑色" };
            ImagePixel = 600;
        }
        private void ExportSnap(object para)
        {
            //设置项取值
            List<FileInfo> familyList = GetFamilyList(FamilyPath);
            switch (selectDisplayStyle)
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
            switch (selectDetailLevel)
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
            switch (selectShowHideHost)
            {
                case "是":
                    is_HideHost = true;
                    break;
                default:
                    is_HideHost = false;
                    break;
            }
            switch (selectWhiteBackGroudnd)
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
                    FilteredElementCollector viewCollector = new FilteredElementCollector(newDoc);
                    viewCollector.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View3D));
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
                    newDoc.ExportImage(option);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", "错误信息" + ex.Message);
                }
            }
        }
        private void ThreeExportImage(UIDocument newUIDoc, Document newDoc, FilteredElementCollector viewCollector)
        {
            try
            {
                View3D view = viewCollector.First() as View3D;
                newUIDoc.ActiveView = view;
                using (Transaction ts = new Transaction(newDoc, "三维图片导出"))
                {
                    ts.Start();
                    view.DetailLevel = DetailLevel;
                    view.DisplayStyle = ViewDisplayStyle;

                    view.OrientTo(new XYZ(-0.577350269189626, 0.577350269189626, -0.577350269189626));
                    if (is_HideHost == true)
                    {
                        ICollection<ElementId> list = HostFilter(newDoc, view.Id);
                        if (list.Count > 0)
                        {
                            view.HideElementsTemporary(list);
                        }
                    }
                    ts.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", "错误信息" + ex.Message);
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

        // 导出二维图片 
        //private bool TwoExportImage(UIDocument newUIDoc, Document newDoc)
        //{
        //    //throw new NotImplementedException();
        //    FilteredElementCollector viewCollector = new FilteredElementCollector(newDoc);
        //    viewCollector.OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(ViewPlan));
        //    if (viewCollector.Count() == 0) return false;
        //    Autodesk.Revit.DB.View view = viewCollector.First() as Autodesk.Revit.DB.View;
        //    newUIDoc.ActiveView = view;
        //    using (Transaction trans = new Transaction(newDoc, "二维图片导出"))
        //    {
        //        trans.Start();
        //        view.DetailLevel = DetailLevel;
        //        view.DisplayStyle = ViewDisplayStyle;
        //        if (is_HideHost)
        //        {
        //            ICollection<ElementId> list = HostFilter(newDoc, view.Id);
        //            if (list.Count > 0)
        //            {
        //                view.HideElementsTemporary(list);
        //            }
        //        }
        //        trans.Commit();
        //    }
        //    return true;
        //}
        private List<FileInfo> GetFamilyList(FileSystemInfo familyPath)
        {
            //这里是个遍历操作，使得选定族文件夹下面所有的族都能导出
            //如果族文件版本过高怎是否产生错误？如何trycatch处理？
            List<FileInfo> familyList = new List<FileInfo>();
            if (!familyPath.Exists) return null;
            DirectoryInfo dir = familyPath as DirectoryInfo;
            if (dir == null) return null;
            familyList.AddRange(dir.GetFiles("*.rfa"));
            foreach (DirectoryInfo dInfo in dir.GetDirectories())
            {
                familyList.AddRange(GetFamilyList(dInfo));
            }
            return familyList;
        }
        private void GetFolder(object obj)
        {
            FamilyPath = PickFolderInfo();
            if (FamilyPath != null)
            {
                FolderName = FamilyPath.FullName;
            }
        }
        public FileSystemInfo PickFolderInfo()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择族文件所在地址";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return new DirectoryInfo(dialog.SelectedPath);
            }
            return null;
        }
        private string folderName = "本功能将遍历目标文件夹及其所有子文件夹内所有族文档，请谨慎选择";
        public string FolderName
        {
            get => folderName;
            set
            {
                folderName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllowSelect));
            }
        }
        public DisplayStyle ViewDisplayStyle { get; set; }
        public ViewDetailLevel DetailLevel { get; set; }
        public bool is_WhiteBackGroudnd { get; set; }
        public bool is_HideHost { get; set; }
        private int imagePixel;
        public int ImagePixel { get => imagePixel; set => imagePixel = value; }
        public string selectDisplayStyle { get; set; }
        public string selectDetailLevel { get; set; }
        public string selectShowHideHost { get; set; }
        public string selectWhiteBackGroudnd { get; set; }
        public bool AllowSelect
        {
            get { return IsValidPath(FolderName); }
        }
        public FileSystemInfo FamilyPath { get; set; }
        public BaseBindingCommand ExportSnapCommand { get; set; }
        public BaseBindingCommand GetFolderCommand { get; set; }
        List<string> displayStyleList;
        List<string> detailLevelList;
        List<string> isHideHostList;
        List<string> isWhiteBack;
        public List<string> IsHideHostList { get => isHideHostList; set => isHideHostList = value; }
        public List<string> DisplayStyleList { get => displayStyleList; set => displayStyleList = value; }
        public List<string> DetailLevelList { get => detailLevelList; set => detailLevelList = value; }
        public List<string> IsWhiteBack { get => isWhiteBack; set => isWhiteBack = value; }

        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path)) { return false; }
            try
            {
                string fullPath = Path.GetFullPath(path);
                // 检查路径是否指向一个文件或目录
                return File.Exists(fullPath) || Directory.Exists(fullPath);
            }
            catch (Exception) { return false; }
        }
    }
}
