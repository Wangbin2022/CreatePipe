using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitPro.Event;
using RevitPro.Filter;
using RevitPro.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.cmd
{
    /// <summary>
    /// 打断管功能
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class BreakMEPCurveCmd : IExternalCommand
    {   //创建窗体对象
        BreakMEPCurveForm breakMEPCurveForm = null;
        UIDocument uIDocument = null;
        Document document = null;
        Application application = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {//ui应用程序
            UIApplication uiApp = commandData.Application;
            //应用程序
            application = uiApp.Application;


            //ui文档
            uIDocument = uiApp.ActiveUIDocument;

            //文档
            document = uIDocument.Document;

            ExternalEventExample externalEventExample = new ExternalEventExample(commandData);

            //注册事件
            externalEventExample.CreateExternalEvent(externalEventExample);



            //创建窗体对象
            breakMEPCurveForm = new BreakMEPCurveForm(this, externalEventExample);
            //打开窗体
            breakMEPCurveForm.Show();




            return Result.Succeeded;
        }


        public void BreakMEPCurveMethod()
        {





            BreakMEPCurve();




        }

        /// <summary>
        /// 一个点打断管
        /// </summary>
        public List<MEPCurve> BreakMEPCurve()
        {
            List<MEPCurve> mEPCurves = new List<MEPCurve>();

            using (Transaction tran = new Transaction(document, "tran"))
            {
                tran.Start();
                try
                {

                    while (true)
                    {


                        //选择管
                        Reference reference = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter());


                        MEPCurve mEPCurve = document.GetElement(reference) as MEPCurve;

                        //选则打断点
                        XYZ breakXYZ = reference.GlobalPoint;
                        MEPCurve mEPCurveCopy = null;

                        //拷贝一根管
                        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(document, mEPCurve.Id, new XYZ(0, 0, 0));
                        ElementId newId = ids.FirstOrDefault();
                        mEPCurveCopy = document.GetElement(newId) as MEPCurve;


                        //原来管的线
                        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
                        XYZ startXYZ = curve.GetEndPoint(0);
                        XYZ endXYZ = curve.GetEndPoint(1);

                        //映射点
                        breakXYZ = curve.Project(breakXYZ).XYZPoint;

                        //给原来的管用的线
                        Line line = Line.CreateBound(startXYZ, breakXYZ);

                        //拷贝管用的线
                        Line line2 = Line.CreateBound(breakXYZ, endXYZ);

                        //管1连接的连接器
                        Connector otherCon = null;
                        //解除管1连接的连接器 并获得连接的其它连接器
                        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
                        {
                            bool isBreak = false;
                            if (con.Id == 1 && con.IsConnected)
                            {
                                foreach (Connector con2 in con.AllRefs)
                                {
                                    if (con2.Owner is FamilyInstance)
                                    {
                                        con.DisconnectFrom(con2);
                                        otherCon = con2;
                                        isBreak = true;
                                        break;
                                    }
                                }
                            }
                            if (isBreak)
                            {
                                break;
                            }
                        }


                         //改原来的管
                         (mEPCurve.Location as LocationCurve).Curve = line;

                        //改现在的管
                        (mEPCurveCopy.Location as LocationCurve).Curve = line2;

                        //让拷贝的管连接原来管连接的连接器
                        if (otherCon != null)
                        {


                            foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
                            {
                                if (con.Id == 1)
                                {
                                    con.ConnectTo(otherCon);
                                }
                            }
                        }


                        document.Regenerate();
                        mEPCurves.Add(mEPCurveCopy);

                    }

                }
                catch (Exception ex)
                {

                    breakMEPCurveForm.Show();
                  //  TaskDialog.Show("提示", ex.Message);

                }
                tran.Commit();
            }
            return mEPCurves;
        }


        public void BreakMEPCurveMethod2()
        {
            BreakMEPCurve2();


        }
        /// <summary>
        /// 两个个点打断管
        /// </summary>
        public List<MEPCurve> BreakMEPCurve2()
        {
            List<MEPCurve> mEPCurves = new List<MEPCurve>();
            using (Transaction tran = new Transaction(document, "tran"))
            {
                tran.Start();
                try
                {
                    while (true)
                    {



                        //选择管
                        Reference reference = uIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new MEPCurveFilter());


                        MEPCurve mEPCurve = document.GetElement(reference) as MEPCurve;

                        //选择打断点1
                        XYZ breakXYZ = reference.GlobalPoint;
                        //选择打断点2
                        XYZ breakXYZ2 = uIDocument.Selection.PickPoint();

                        MEPCurve mEPCurveCopy = null;





                        //拷贝一根管
                        ICollection<ElementId> ids = ElementTransformUtils.CopyElement(document, mEPCurve.Id, new XYZ(0, 0, 0));
                        ElementId newId = ids.FirstOrDefault();
                        mEPCurveCopy = document.GetElement(newId) as MEPCurve;


                        //原来管的线
                        Curve curve = (mEPCurve.Location as LocationCurve).Curve;
                        XYZ startXYZ = curve.GetEndPoint(0);
                        XYZ endXYZ = curve.GetEndPoint(1);

                        //给原来的用
                        breakXYZ = curve.Project(breakXYZ).XYZPoint;

                        //给现在的管用
                        breakXYZ2 = curve.Project(breakXYZ2).XYZPoint;

                        //如果起始点和breakXYZ2近
                        if (startXYZ.DistanceTo(breakXYZ) > startXYZ.DistanceTo(breakXYZ2))
                        {
                            XYZ xyz = breakXYZ;
                            breakXYZ = breakXYZ2;
                            breakXYZ2 = xyz;
                        }



                        //给原来的管用的线
                        Line line = Line.CreateBound(startXYZ, breakXYZ);

                        //拷贝管用的线
                        Line line2 = Line.CreateBound(breakXYZ2, endXYZ);

                        //管1连接的连接器
                        Connector otherCon = null;
                        //解除管1连接的连接器 并获得连接的其它连接器
                        foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
                        {
                            bool isBreak = false;
                            if (con.Id == 1 && con.IsConnected)
                            {
                                foreach (Connector con2 in con.AllRefs)
                                {
                                    if (con2.Owner is FamilyInstance)
                                    {
                                        con.DisconnectFrom(con2);
                                        otherCon = con2;
                                        isBreak = true;
                                        break;
                                    }
                                }
                            }
                            if (isBreak)
                            {
                                break;
                            }
                        }


            //改原来的管
            (mEPCurve.Location as LocationCurve).Curve = line;

                        //改现在的管
                        (mEPCurveCopy.Location as LocationCurve).Curve = line2;

                        //让拷贝的管连接原来管连接的连接器
                        if (otherCon != null)
                        {


                            foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
                            {
                                if (con.Id == 1)
                                {
                                    con.ConnectTo(otherCon);
                                }
                            }
                        }





                        document.Regenerate();

                        mEPCurves.Add(mEPCurveCopy);
                    }
                }
                catch (Exception ex)
                {
                  breakMEPCurveForm.Show();

                 //   TaskDialog.Show("提示", ex.Message);
                }
                tran.Commit();
            }
            return mEPCurves;
        }

    }
}
