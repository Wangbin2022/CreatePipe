namespace Class2
{
    //[Transaction(TransactionMode.Manual)]
    //public class Class2 : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        //20240324
    //        UIApplication uiApp = commandData.Application;
    //        Autodesk.Revit.ApplicationServices.Application application = uiApp.Application;
    //        UIDocument uIDocument = uiApp.ActiveUIDocument;
    //        Autodesk.Revit.DB.Document document = uIDocument.Document;
    //        View view = uIDocument.ActiveView;

    //        //例程归档 0627
    //        //TaskDialog.Show("title1",application.VersionName);
    //        //TaskDialog.Show("title2", view.Name);
    //        //TaskDialog.Show("title3", view.Id.IntegerValue.ToString());//非文字类型还需要强转
    //        //View view1 = document.ActiveView;
    //        ////TaskDialog.Show("title4,当前视图", view.Name);//跟view相比不可修改
    //        //ElementId id = new ElementId(311);//从视图属性中取id INT转化为
    //        //uIDocument.ActiveView = document.GetElement(id) as View; //更改视图名称，不在事务中
    //        //view1 = document.ActiveView;
    //        ////TaskDialog.Show("title5，改后视图", view.Name);
    //        //0326  事务入门
    //        //using (Transaction transaction = new Transaction(document, "新事务"))
    //        //{ 
    //        //    transaction.Start();

    //        //    TransactionStatus transactionStatus = transaction.GetStatus();
    //        //    if (transactionStatus == TransactionStatus.Started)
    //        //    {
    //        //        TaskDialog.Show("提示","开启成功");
    //        //    }
    //        //    Reference reference = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
    //        //    FamilyInstance element = document.GetElement(reference) as FamilyInstance;
    //        //    //找到门的底标高参数（系统自带）
    //        //    Parameter parameter = element.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
    //        //    //找自定义参数需使用lookup parameter
    //        //    Parameter parameter2 = element.LookupParameter("在rfa中定义的参数名称");
    //        //    //内部单位转换，以后可以考虑分单独的方法，100作为变量直接输入
    //        //    //double hh = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);
    //        //    parameter.Set(Unitconvert.Tofoot(100));//设定门底高并转换
    //        //    transaction.Commit();
    //        //    transactionStatus = transaction.GetStatus();
    //        //    if (transactionStatus == TransactionStatus.Committed)
    //        //    {
    //        //        TaskDialog.Show("提示", "提交成功");
    //        //    }
    //        //}
    //        //MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("1", "11", MessageBoxButton.OKCancel);//以后直接用这个
    //        //if (messageBoxResult == MessageBoxResult.OK)
    //        //{       //以下执行事务          
    //        //}
    //        //例程结束

    //        return Result.Succeeded;
    //    }





    //}
}
