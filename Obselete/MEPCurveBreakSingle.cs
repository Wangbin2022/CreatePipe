using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class MEPCurveBreakSingle : IExternalCommand
    {
        UIDocument uiDoc = null;
        Document doc = null;
        Application application = null;

        //接收主程序传参commandData
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //UIApplication uiApp = commandData.Application;
            //application = uiApp.Application;
            //uiDoc = uiApp.ActiveUIDocument;
            //doc = uiDoc.Document; //用全局定义，不要重复赋值
            uiDoc = commandData.Application.ActiveUIDocument;
            doc = commandData.Application.ActiveUIDocument.Document;
            Reference reference = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new filterMEPCurveClass());
            MEPCurve mEPCurve = doc.GetElement(reference) as MEPCurve;
            XYZ breakXYZ = reference.GlobalPoint;
            using (Transaction ts = new Transaction(doc, "单点打断"))
            {
                ts.Start();
                BreakMEPCurveByOne(commandData, mEPCurve, breakXYZ);
                ts.Commit();
            }
            return Result.Succeeded;
        }

        public MEPCurve BreakMEPCurveByOne(ExternalCommandData commandData, MEPCurve mEPCurve, XYZ xYZ)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            try
            {
                XYZ breakXYZ = xYZ;
                MEPCurve mEPCurveCopy = null;//变量声明放到事务外才能访问

                //拷贝一根管
                ICollection<ElementId> ids = ElementTransformUtils.CopyElement(doc, mEPCurve.Id, new XYZ(0, 0, 0));
                ElementId newId = ids.FirstOrDefault();
                mEPCurveCopy = doc.GetElement(newId) as MEPCurve;
                //原管的线
                Curve curve = (mEPCurve.Location as LocationCurve).Curve;
                XYZ startXYZ = curve.GetEndPoint(0);
                XYZ endXYZ = curve.GetEndPoint(1);
                //把点xyz轴映射到线上避免错误 ??这个映射方法没搞懂
                breakXYZ = curve.Project(breakXYZ).XYZPoint;
                //给原管用的线
                Line line = Line.CreateBound(startXYZ, breakXYZ);
                //找连接器并取消多余连接，保存连接信息P28
                Connector othercon = null;
                foreach (Connector con in mEPCurve.ConnectorManager.Connectors)
                {
                    bool isBreak = false;
                    //获取id后，找连接的情况，再解除连接
                    if (con.Id == 1 && con.IsConnected)
                    {
                        foreach (Connector con2 in con.AllRefs)
                        {
                            if (con2.Owner is FamilyInstance)
                            {
                                con.DisconnectFrom(con2);
                                othercon = con2;
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

                        (mEPCurve.Location as LocationCurve).Curve = line;
                //拷贝管用的线
                Line line1 = Line.CreateBound(breakXYZ, endXYZ);
                (mEPCurveCopy.Location as LocationCurve).Curve = line1;
                //拷贝管连接老管的连接器
                if (othercon != null)
                {
                    foreach (Connector con in mEPCurveCopy.ConnectorManager.Connectors)
                    {
                        if (con.Id == 1)
                        {
                            con.ConnectTo(othercon);
                        }
                    }

                }

                return mEPCurveCopy;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("功能退出", ex.Message);
            }
            return null;
        }
    }
}
