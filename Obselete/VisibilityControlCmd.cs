using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    //[Transaction(TransactionMode.Manual)]//关闭基点显示
    //public class DirecrMixFunction08 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Document doc = commandData.Application.ActiveUIDocument.Document;
    //        View activeView = doc.ActiveView;
    //        //一行验证
    //        if (!VisibilityHelper.CanModifyViewVisibility(activeView)) return Result.Cancelled;
    //        NewTransaction.Execute(doc, "关闭基点显示", () =>
    //        {
    //            VisibilityHelper.SetCategoriesVisibility(doc, activeView, new[] { BuiltInCategory.OST_Site }, true);
    //            VisibilityHelper.SetCategoriesVisibility(doc, activeView, new[] { BuiltInCategory.OST_ProjectBasePoint, BuiltInCategory.OST_SharedBasePoint }, false);
    //        }); 
    //        //UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        //Document doc = uiDoc.Document;
    //        //View activeView = uiDoc.ActiveView;
    //        //if (activeView.ViewTemplateId.IntegerValue != -1)
    //        //{
    //        //    TaskDialog.Show("Title", "请关闭当前视图样板");
    //        //    return Result.Cancelled;
    //        //}
    //        //else
    //        //{
    //        //    Categories cates = doc.Settings.Categories;
    //        //    Category site = cates.get_Item(BuiltInCategory.OST_Site);
    //        //    Category basePoint = cates.get_Item(BuiltInCategory.OST_ProjectBasePoint);
    //        //    Category sharePoint = cates.get_Item(BuiltInCategory.OST_SharedBasePoint);
    //        //    doc.NewTransaction(() =>
    //        //    {
    //        //        doc.ActiveView.SetCategoryHidden(site.Id, false);
    //        //        doc.ActiveView.SetCategoryHidden(basePoint.Id, true);
    //        //        doc.ActiveView.SetCategoryHidden(sharePoint.Id, true);
    //        //    }, "关闭基点显示");
    //        //}
    //        return Result.Succeeded;
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]//开启基点显示
    //public class DirecrMixFunction09 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Document doc = commandData.Application.ActiveUIDocument.Document;
    //        View activeView = doc.ActiveView;
    //        //一行验证
    //        if (!VisibilityHelper.CanModifyViewVisibility(activeView)) return Result.Cancelled;
    //        NewTransaction.Execute(doc, "显示基点", () =>
    //        {
    //            // 场地、基点、测量点全部开启
    //            VisibilityHelper.SetCategoriesVisibility(doc, activeView,
    //                new[] { BuiltInCategory.OST_Site, BuiltInCategory.OST_ProjectBasePoint, BuiltInCategory.OST_SharedBasePoint }, true);
    //        });
    //        // 获取基点和测量点
    //        BasePoint prjPoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint).FirstOrDefault() as BasePoint;
    //        BasePoint surPoint = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_SharedBasePoint).FirstOrDefault() as BasePoint;
    //        if (prjPoint != null && surPoint != null)
    //        {
    //            // Revit内部单位是英尺，转毫米需乘以 304.8
    //            string msg = $"项目基点坐标：{(int)(prjPoint.Position.X * 304.8)}, {(int)(prjPoint.Position.Y * 304.8)}, {(int)(prjPoint.Position.Z * 304.8)}\n" +
    //                         $"测量点坐标：{(int)(surPoint.Position.X * 304.8)}, {(int)(surPoint.Position.Y * 304.8)}, {(int)(surPoint.Position.Z * 304.8)}";
    //            TaskDialog.Show("CADC", msg);
    //        }
    //        //UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        //Document doc = uiDoc.Document;
    //        //View activeView = uiDoc.ActiveView;
    //        //if (activeView.ViewTemplateId.IntegerValue != -1)
    //        //{
    //        //    TaskDialog.Show("Title", "请关闭当前视图样板");
    //        //    return Result.Cancelled;
    //        //}
    //        //else
    //        //{
    //        //    Categories cates = doc.Settings.Categories;
    //        //    Category site = cates.get_Item(BuiltInCategory.OST_Site);
    //        //    Category basePoint = cates.get_Item(BuiltInCategory.OST_ProjectBasePoint);
    //        //    Category sharePoint = cates.get_Item(BuiltInCategory.OST_SharedBasePoint);
    //        //    doc.NewTransaction(() =>
    //        //    {
    //        //        doc.ActiveView.SetCategoryHidden(site.Id, false);
    //        //        doc.ActiveView.SetCategoryHidden(basePoint.Id, false);
    //        //        doc.ActiveView.SetCategoryHidden(sharePoint.Id, false);
    //        //    }, "显示基点");
    //        //    BasePoint basePoint1 = GetPoint2020(doc, BuiltInCategory.OST_ProjectBasePoint);
    //        //    BasePoint surveyPoint = GetPoint2020(doc, BuiltInCategory.OST_SharedBasePoint);
    //        //TaskDialog.Show("CADC", "项目基点坐标为" + Convert.ToInt32(basePoint1.Position.X * 304.8).ToString() + "," + Convert.ToInt32(basePoint1.Position.Y * 304.8).ToString() + "," + Convert.ToInt32(basePoint1.Position.Z * 304.8).ToString() + "\n" + "项目测量点坐标为" + Convert.ToInt32(surveyPoint.Position.X * 304.8).ToString() + "," + Convert.ToInt32(surveyPoint.Position.Y * 304.8).ToString() + "," + Convert.ToInt32(surveyPoint.Position.Z * 304.8).ToString());
    //        return Result.Succeeded;            
    //    }
    //    public BasePoint GetPoint2020(Document document, BuiltInCategory category)
    //    {
    //        FilteredElementCollector elements1 = new FilteredElementCollector(document);
    //        elements1.OfClass(typeof(BasePoint)).OfCategory(category);
    //        BasePoint basePoint = elements1.FirstOrDefault() as BasePoint;
    //        return basePoint;
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]//开关保温层显示
    //public class DirecrMixFunction11 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Document doc = commandData.Application.ActiveUIDocument.Document;
    //        View activeView = doc.ActiveView;
    //        if (!VisibilityHelper.CanModifyViewVisibility(activeView)) return Result.Cancelled;
    //        NewTransaction.Execute(doc, "开关保温层显示", () =>
    //        {
    //            // 一键智能反转水管和风管保温层的可见性
    //            VisibilityHelper.ToggleCategoriesVisibility(doc, activeView,
    //                new[] { BuiltInCategory.OST_PipeInsulations, BuiltInCategory.OST_DuctInsulations });
    //        });
    //        //UIDocument uiDoc = commandData.Application.ActiveUIDocument;
    //        //Document doc = uiDoc.Document;
    //        //Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
    //        //if (activeView.ViewTemplateId.IntegerValue != -1)
    //        //{
    //        //    TaskDialog.Show("Title", "请关闭当前视图样板");
    //        //    return Result.Cancelled;
    //        //}
    //        //else
    //        //{
    //        //    Categories cates = doc.Settings.Categories;
    //        //    Category pipeInsu = cates.get_Item(BuiltInCategory.OST_PipeInsulations);
    //        //    Category ductInsu = cates.get_Item(BuiltInCategory.OST_DuctInsulations);
    //        //    doc.NewTransaction(() =>
    //        //    {
    //        //        if (activeView.GetCategoryHidden(pipeInsu.Id))
    //        //        {
    //        //            doc.ActiveView.SetCategoryHidden(pipeInsu.Id, false);
    //        //            doc.ActiveView.SetCategoryHidden(ductInsu.Id, false);
    //        //        }
    //        //        else
    //        //        {
    //        //            doc.ActiveView.SetCategoryHidden(pipeInsu.Id, true);
    //        //            doc.ActiveView.SetCategoryHidden(ductInsu.Id, true);
    //        //        }
    //        //    }, "开关保温层显示");
    //        //}
    //        return Result.Succeeded;
    //    }
    //}
}
