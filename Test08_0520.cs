using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.NCCoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test08_0520 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


            //0522
            //还是先尝试做个项目参数管理器吧
             
            
            //NCCodingView codingView = new NCCodingView(uiApp);
            //codingView.ShowDialog();
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new FamilyInstanceFilterClass(), "pick something");
            //Element ele = doc.GetElement(r);
            //Parameter para = ele.LookupParameter("族ID");
            //if (para.AsString() != null)
            //{ 
            //    TaskDialog.Show("tt",para.AsString());
            //}
            ////0521 
            //// 获取所有 FamilyInstance
            //var familyInstances = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
            //// 获取所有 Family 的唯一集合
            //List<Family> families = familyInstances.Select(fi => fi.Symbol.Family).Distinct().ToList();
            //StringBuilder stringBuilder = new StringBuilder();
            //List<Family> newFamily = new List<Family>();
            //foreach (var family in families)
            //{
            //    Parameter para= family.LookupParameter("族ID");
            //    if (para.AsString() != null)
            //    {
            //        stringBuilder.Append(family.Name.ToString() + "||");
            //        newFamily.Add(family);
            //    }
            //}
            //// 定义输出文件路径
            //string outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Families.txt");
            //// 将 StringBuilder 的内容写入到文本文件
            //File.WriteAllText(outputFilePath, stringBuilder.ToString());
            ////int familyCount = families.Count;
            //int familyCount = newFamily.Count;
            //TaskDialog.Show("Family Count", $"当前文档中存在的 FamilyInstance 的 Family 数量为: {familyCount}");


            //0521 当前应用程序路径的上级目录？为什么返回的是桌面？
            //TaskDialog.Show("tt", Path.GetFullPath(".."));             
            //0520 遗留测试
            ////0404 切换连接顺序抄网上代码，初步实现柱切板和梁，梁切板。
            //FilteredElementCollector list_column = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance));
            //FilteredElementCollector list_beam = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance));
            ////TaskDialog.Show("tt", $"柱{list_column.Count().ToString()}个,梁{list_beam.Count().ToString()}个");
            //Transaction transaction = new Transaction(doc, "连接几何关系");
            //transaction.Start();
            //foreach (Element column in list_column)
            //{
            //    List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, 1.01);
            //    //TaskDialog.Show("柱子", column_box_eles.Count.ToString());
            //    foreach (Element ele in column_box_eles)
            //    {
            //        if (ele.Category.GetHashCode().ToString() == "-2001320" || ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, column, ele);
            //        }
            //    }
            //}
            //foreach (Element beam in list_beam)
            //{
            //    List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, 1.01);
            //    //TaskDialog.Show("梁", beam_box_eles.Count.ToString());
            //    foreach (Element ele in beam_box_eles)
            //    {
            //        //if (ele.Category.Name == "楼板")
            //        if (ele.Category.GetHashCode().ToString() == "-2000032")
            //        {
            //            JudgeConnection(doc, beam, ele);
            //        }
            //    }
            //}
            //transaction.Commit();
            ////例程结束
            return Result.Succeeded;
        }
    }
}
