namespace CreatePipe.Utils
{
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.UsingCommandData)]
    //public class ModelClass : IExternalCommand
    //{
    //    UIDocument uidoc = null;
    //    Document doc = null;
    //    //Application application = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        //    UIApplication uIApp = commandData.Application;
    //        //    application = uIApp.Application;
    //        //    uidoc = uIApp.ActiveUIDocument;
    //        //    doc = uidoc.Document;

    //        //    Selection sel = uidoc.Selection;            
    //        //    View activeView = uidoc.ActiveView;

    //        //    using (Transaction ts = new Transaction(doc, "Name"))
    //        //    { 
    //        //        ts.Start();
    //        //        CreateLevel();
    //        //        ts.Commit();
    //        //    }
    //        return Result.Succeeded;
    //    }

    //    public void CreateLevel()
    //    {
    //        Level level = Level.Create(doc, 8000.Tofoot()); //高度相对于项目基点
    //                                                        //获取楼层平面类型
    //        FilteredElementCollector elements1 = new FilteredElementCollector(doc);
    //        var elemnts = elements1.OfClass(typeof(ViewFamilyType)).ToElements();
    //        ViewFamilyType viewFamilyType = null;
    //        foreach (Element item in elemnts)
    //        {
    //            viewFamilyType = item as ViewFamilyType;
    //            if (viewFamilyType.ViewFamily == ViewFamily.FloorPlan)
    //            {
    //                break;
    //            }
    //        }
    //        //创建平面视图
    //        ViewPlan.Create(doc, viewFamilyType.Id, level.Id);
    //        TaskDialog.Show("高度", level.Elevation.ToMillimeter() + "");
    //    }


    //    public Grid CreateLineGrid()
    //    {
    //        XYZ xYZ1 = uidoc.Selection.PickPoint();
    //        XYZ xYZ2 = uidoc.Selection.PickPoint();

    //        Line line = Line.CreateBound(xYZ1, xYZ2);
    //        Grid grid = Grid.Create(doc, line);

    //        return grid;
    //    }
    //    public Grid CreateArcGrid()
    //    {
    //        XYZ xYZ1 = uidoc.Selection.PickPoint();
    //        XYZ xYZ2 = uidoc.Selection.PickPoint();
    //        XYZ xYZ3 = uidoc.Selection.PickPoint();

    //        Arc arc = Arc.Create(xYZ1, xYZ2,xYZ3);
    //        Grid grid = Grid.Create(doc,arc);

    //        return grid;
    //    }
    //}
}
