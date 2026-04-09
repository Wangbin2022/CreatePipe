using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class SheetEntity : ObserverableObject
    {
        public ViewSheet Sheet { get; }
        public ElementId Id { get; }
        private string sheetName;
        public string SheetName
        {
            get => sheetName;
            set
            {
                if (sheetName == value) return; // 防止重复触发
                // 1. 立即在当前 WPF 线程更新内部字段和 UI（让界面响应丝滑，不卡输入法）
                sheetName = value;
                OnPropertyChanged();
                // 2. 后台通知 Revit 去真实修改模型参数
                _handler.Run(app =>
                {
                    try
                    {
                        NewTransaction.Execute(Document, "修改名称", () => Sheet.Name = value);
                    }
                    catch (Exception) { /* 忽略或处理错误 */ }
                });
            }
        }
        private string sheetNum;
        public string SheetNum
        {
            get => sheetNum;
            set
            {
                if (sheetNum == value) return;
                sheetNum = value;
                OnPropertyChanged();
                _handler.Run(app =>
                {
                    try
                    {
                        NewTransaction.Execute(Document, "修改图号", () => Sheet.SheetNumber = value);
                    }
                    catch (Exception) { /* 忽略或处理错误 */ }
                });
            }
        }
        public int ViewCount { get; }
        public string Schedule { get; }
        public Dictionary<string, string> RelatedViews { get; } = new Dictionary<string, string>();
        private readonly BaseExternalHandler _handler;
        public Document Document { get; set; }
        public SheetEntity(ViewSheet sheetView, BaseExternalHandler handler)
        {
            Document = sheetView.Document;
            _handler = handler;
            Sheet = sheetView;
            Id = sheetView.Id;
            SheetName = sheetView.Name;
            SheetNum = sheetView.SheetNumber;
            Document doc = sheetView.Document;
            var views = sheetView.GetAllPlacedViews();
            ViewCount = views.Count;
            foreach (var viewId in views)
            {
                if (doc.GetElement(viewId) is View view)
                {
                    // 统一格式，例如：平面图 + 比例1:100
                    RelatedViews[viewId.ToString()] = $"{view.Name} (比例 1:{view.Scale})";
                }
            }
            var scheduleInstances = new FilteredElementCollector(Document, Id)
            .OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().ToList();
            if (scheduleInstances.Count > 0)
            {
                // 提取所有明细表的名称，并用逗号拼接
                var scheduleNames = scheduleInstances.Select(s => s.Name);
                Schedule = string.Join("，", scheduleNames);
            }
            else
            {
                Schedule = "无明细表"; // 或者设为 string.Empty，视你的 UI 需求而定
            }
        }
    }
    //public class SheetEntity : ObserverableObject
    //{
    //    ViewSheet View { get; set; }
    //    Document Document { get => View.Document; }
    //    public SheetEntity(ViewSheet sheetView)
    //    {
    //        View = sheetView;
    //        sheetName = sheetView.Name;
    //        Id = sheetView.Id;
    //        sheetNum = sheetView.SheetNumber;

    //        //var viewPorts = new FilteredElementCollector(Document, Id).OfCategory(BuiltInCategory.OST_Viewports);
    //        var views = sheetView.GetAllPlacedViews();
    //        viewCount = views.Count();
    //        foreach (var viewId in views)
    //        {
    //            View view = Document.GetElement(viewId) as View;
    //            relatedViews[viewId.IntegerValue.ToString()] = Document.GetElement(viewId).Name + "+比例1：" + view.Scale;
    //        }
    //    }
    //    public Dictionary<string, string> relatedViews = new Dictionary<string, string>();
    //    public int viewCount { get; set; } = 0;
    //    public string sheetNum { get; set; }
    //    public ElementId Id { get; set; }
    //    public string sheetName { get; set; }
    //}
}
