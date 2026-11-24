using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test12_0704 : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View view = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;


            //1123 原点画交叉轴线
            try
            {
                if (view == null || view.IsTemplate)
                {
                    TaskDialog.Show("提示", "此功能只能在平面视图中使用。");
                    return Result.Cancelled;
                }
                //// Revit 内部长度单位为英尺
                //double offset = UnitUtils.ConvertToInternalUnits(
                //    6000,
                //    DisplayUnitType.DUT_MILLIMETERS);
                XYZ origin = XYZ.Zero;
                XYZ eastWest = view.RightDirection.Normalize();
                XYZ northSouth = view.UpDirection.Normalize();

                XYZ west = origin - eastWest * 6000 / 304.8;
                XYZ east = origin + eastWest * 6000 / 304.8;
                XYZ south = origin - northSouth * 6000 / 304.8;
                XYZ north = origin + northSouth * 6000 / 304.8;

                // 取第一个轴线类型
                GridType firstGridType = new FilteredElementCollector(doc).OfClass(typeof(GridType)).Cast<GridType>().FirstOrDefault();
                if (firstGridType == null)
                {
                    TaskDialog.Show("提示", "当前文档中没有可用的轴线类型。");
                    return Result.Failed;
                }
                using (Transaction transaction = new Transaction(doc, "创建内部原点轴线"))
                {
                    transaction.Start();
                    Grid horizontalGrid = Grid.Create(doc, Line.CreateBound(west, east));
                    Grid verticalGrid = Grid.Create(doc, Line.CreateBound(south, north));
                    // 显式指定第一个轴线类型
                    horizontalGrid.ChangeTypeId(firstGridType.Id);
                    verticalGrid.ChangeTypeId(firstGridType.Id);
                    // 设置连续样式，修改的是轴线类型，已有使用该类型的轴线也会受影响。0=连续显示，1=断开，2=定制
                    firstGridType.get_Parameter(BuiltInParameter.GRID_CENTER_SEGMENT_STYLE).Set(0);
                    // 设置两侧轴号，0=隐藏，1=显示
                    firstGridType.get_Parameter(BuiltInParameter.GRID_BUBBLE_END_1).Set(1);
                    firstGridType.get_Parameter(BuiltInParameter.GRID_BUBBLE_END_2).Set(1);
                    transaction.Commit();
                    TaskDialog.Show("创建完成", $"已创建两条轴线：\n东西向：{horizontalGrid.Name}\n南北向：{verticalGrid.Name}");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("tt", ex.Message);
            }


            //        ////////1207 风口清理和连接
            //        //try
            //        //{
            //        //    // 1. 选择风口
            //        //    using (Transaction trans = new Transaction(doc, "修改风管系统"))
            //        //    {
            //        //        trans.Start();
            //        //        //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new AirTerminalSelectionFilter(), "请选择一个风口");
            //        //        //Element terminal = doc.GetElement(reference);
            //        //        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            //        //        if (selectedIds == null || selectedIds.Count == 0)
            //        //        {
            //        //            TaskDialog.Show("错误", "未选择任意");
            //        //            return Result.Failed;
            //        //        }
            //        //        List<Element> ductTerminals = new List<Element>();
            //        //        foreach (var id in selectedIds)
            //        //        {
            //        //            Element element = doc.GetElement(id);
            //        //            if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal)
            //        //            {
            //        //                ductTerminals.Add(element);
            //        //            }
            //        //        }
            //        //        if (ductTerminals == null)
            //        //        {
            //        //            TaskDialog.Show("错误", "未选择风口");
            //        //            return Result.Failed;
            //        //        }
            //        //        foreach (var item in ductTerminals)
            //        //        {
            //        //            // 2. 获取风口的所有连接器
            //        //            List<Connector> connectors = GetConnectors(item);
            //        //            if (connectors.Count == 0)
            //        //            {
            //        //                TaskDialog.Show("提示", "该风口没有连接器");
            //        //                return Result.Failed;
            //        //            }
            //        //            // 3. 获取所有相连的管件和风管
            //        //            List<ElementId> connectedElements = GetAllConnectedElements(connectors, doc);
            //        //            // 4. 删除所有相连的管件和风管
            //        //            DeleteConnectedElements(doc, connectedElements);
            //        //            // 5. 设置风口高度
            //        //            SetTerminalHeight(item, 3000);
            //        //        }
            //        //        trans.Commit();
            //        //        //TaskDialog.Show("完成",$"已删除 {connectedElements.Count} 个相连元素，并将风口高度设置为4000mm");
            //        //    }
            //        //    return Result.Succeeded;
            //        //}
            //        //catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //        //{
            //        //    return Result.Cancelled;
            //        //}
            //        //catch (Exception ex)
            //        //{
            //        //    message = ex.Message;
            //        //    return Result.Failed;
            //        //}      


            return Result.Succeeded;
        }
    }

    ////0205 查找特定属性风口构建
    //List<FamilyInstance> allInstance = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
    //List<FamilyInstance> terminalNames = new List<FamilyInstance>();
    //foreach (var item in allInstance)
    //{
    //    if ((item.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal) && (item.Name == "风道末端_单层百叶风口"))
    //    {
    //        terminalNames.Add(item);
    //    }
    //}
    //List<ElementId> selectedElementIds = new List<ElementId>();
    ////foreach (var item in terminalNames)
    ////{
    ////    try
    ////    {
    ////        Parameter widthParameter = item.LookupParameter("风口宽度");
    ////        Parameter heightParameter = item.LookupParameter("风口高度");
    ////        //if (widthParameter != null && widthParameter.AsDouble() == 600 / 304.8 && heightParameter != null && heightParameter.AsDouble() == 500 / 304.8)
    ////        if (widthParameter != null && widthParameter.AsDouble() == 1000 / 304.8)
    ////        //if (heightParameter != null && heightParameter.AsDouble() == 600 / 304.8)
    ////        {
    ////            selectedElementIds.Add(item.Id);
    ////        }
    ////    }
    ////    catch (Exception)
    ////    {
    ////        throw;
    ////    }
    ////}
    ////TaskDialog.Show("tt", selectedElementIds.Count().ToString());
    //StringBuilder stringBuilder = new StringBuilder();
    //foreach (var item in terminalNames)
    //{
    //    selectedElementIds.Add(item.Id);
    //    stringBuilder.Append(item.Id.ToString() + ",");
    //}
    //TaskDialog.Show("tt", stringBuilder.ToString());
    ////uiDoc.Selection.SetElementIds(selectedElementIds);

    //////1029 管道属性批量填写,系统族批量可参考.OK
    //using (Transaction tx = new Transaction(doc, "管道属性批写入"))
    //{
    //    tx.Start();
    //    try
    //    {
    //        List<Pipe> allPipesInModel = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>().ToList();
    //        foreach (var pipe in allPipesInModel)
    //        {
    //            //TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
    //            double diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble() * 304.8;
    //            double length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() * 304.8;
    //            // 参数配置字典
    //            var parameterConfigs = new Dictionary<string, string>
    //            {
    //                { "尺寸规格", $"DN{(int)diameter}" },
    //                { "直径", $"DN{(int)diameter}" },
    //                { "材质1", "钢管" },
    //                { "压力等级", "1.6MPa" },
    //                { "长度", $"{(int)length}mm" },
    //                { "系统类型", "喷淋" },
    //                { "坡度", "0" },
    //                { "保温材料", "柔性泡沫橡塑管壳" },
    //                { "保温厚度", "55mm" }
    //            };
    //            foreach (var config in parameterConfigs)
    //            {
    //                Parameter param = pipe.LookupParameter(config.Key);
    //                param?.Set(config.Value);
    //            }
    //            //简化前代码
    //            //Parameter parameter1 = item.LookupParameter("尺寸规格");
    //            //if (parameter1 != null)
    //            //{
    //            //    parameter1.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
    //            //}
    //            //Parameter parameter2 = item.LookupParameter("直径");
    //            //if (parameter2 != null)
    //            //{
    //            //    parameter2.Set($"DN{(int)((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8)}");
    //            //}
    //        }
    //        //////属性测试
    //        ////Pipe item = doc.GetElement(uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterPipe()).ElementId) as Pipe;
    //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble()) * 304.8).ToString());
    //        ////TaskDialog.Show("tt", ((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8).ToString("F0"));
    //        ////TaskDialog.Show("tt", ((int)((item.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()) * 304.8)).ToString());
    //        //TaskDialog.Show("tt", item.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString());
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //    tx.Commit();
    //}
    ////例程结束
}
