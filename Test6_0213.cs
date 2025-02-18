using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test6_0213 : IExternalCommand
    {

        private XYZ getnormal(Curve curve, Autodesk.Revit.DB.View view)
        {
            XYZ p1 = curve.GetEndPoint(0);
            XYZ p2 = curve.GetEndPoint(1);
            double res = 100;
            double tolerance = 1e-6;
            //if (Math.Abs((p2 - p1).Normalize().Z) < tolerance )
            //if (Math.Abs(p1.X) < tolerance && Math.Abs(p2.X) < tolerance)            
            //if (Math.Abs(p1.X) == Math.Abs(p2.X) && Math.Abs(p1.Y) != Math.Abs(p2.Y) && Math.Abs(p1.Z) != Math.Abs(p2.Z))
            //bool xView = ;
            if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.Z == 1 || view.ViewDirection.Z == -1))
            {
                XYZ t0 = new XYZ(p1.X - (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y + (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                XYZ t1 = new XYZ(p1.X + (p2.Y - p1.Y) * (res / curve.ApproximateLength), p1.Y - (p2.X - p1.X) * (res / curve.ApproximateLength), p1.Z);
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Y 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.X) < tolerance && Math.Abs(view.ViewDirection.Z) < tolerance && (view.ViewDirection.Y == 1 || view.ViewDirection.Y == -1))
            {
                XYZ t0 = new XYZ(p1.X + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z - (p2.X - p1.X) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Y, p1.Z + (p2.X - p1.X) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            // 检查 Z 坐标是否都为 0
            else if (Math.Abs(view.ViewDirection.Z) < tolerance && Math.Abs(view.ViewDirection.Y) < tolerance && (view.ViewDirection.X == 1 || view.ViewDirection.X == -1))
            {
                XYZ t0 = new XYZ(p1.X, p1.Y - (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z + (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                XYZ t1 = new XYZ(p1.X, p1.Y + (p2.Z - p1.Z) * (res / curve.ApproximateLength), p1.Z - (p2.Y - p1.Y) * (res / curve.ApproximateLength));
                return Line.CreateBound(t0, t1).Direction;
            }
            else
            {
                throw new InvalidOperationException("两个端点的坐标不满足条件。");
            }
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();

            //0218 开始窗口
            //TabTest tabTest = new TabTest(uiApp);
            //tabTest.ShowDialog();


            //0218 找参照平面





            ////0217 测试画平行参照平面，OK
            //doc.NewTransaction(() =>
            //{
            //    if (activeView.SketchPlane == null)
            //    {
            //        // 获取视图的视角方向（法线方向）
            //        XYZ normal = activeView.ViewDirection;
            //        // 定义工作平面的原点（通常使用视图的原点）
            //        XYZ origin = activeView.Origin;
            //        Plane workPlane = Plane.CreateByNormalAndOrigin(normal, origin);
            //        activeView.SketchPlane = SketchPlane.Create(doc, workPlane);
            //    }
            //    if (activeView.GetType().Name == "ViewPlan" || activeView.GetType().Name == "ViewSection")
            //    {
            //        XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
            //        XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
            //        XYZ direction = (pt2 - pt1).Normalize();
            //        Curve curve1 = Line.CreateBound(pt1, pt2) as Curve;
            //        XYZ cVec;
            //        double tolerance = 1e-6;
            //        // 计算垂直于视图方向的向量
            //        if (Math.Abs(activeView.ViewDirection.X) < tolerance && Math.Abs(activeView.ViewDirection.Y) < tolerance && (activeView.ViewDirection.Z == 1 || activeView.ViewDirection.Z == -1))
            //        {
            //            cVec = new XYZ(0, 0, 1);
            //        }
            //        else if (Math.Abs(activeView.ViewDirection.X) < tolerance && Math.Abs(activeView.ViewDirection.Z) < tolerance && (activeView.ViewDirection.Y == 1 || activeView.ViewDirection.Y == -1))
            //        {
            //            cVec = new XYZ(0, 1, 0);
            //        }
            //        else if (Math.Abs(activeView.ViewDirection.Z) < tolerance && Math.Abs(activeView.ViewDirection.Y) < tolerance && (activeView.ViewDirection.X == 1 || activeView.ViewDirection.X == -1))
            //        {
            //            cVec = new XYZ(1, 0, 0);
            //        }
            //        else
            //        {
            //            throw new InvalidOperationException("两个端点的坐标不满足条件。");
            //        }
            //        ReferencePlane rp1 = doc.Create.NewReferencePlane(pt1, pt2, cVec, activeView);
            //        XYZ offsetDirection = getnormal(curve1, activeView);
            //        double offsetDistance = 200 / 304.8;
            //        //计算偏移后的起点和终点
            //        XYZ offsetPt1 = pt1 + offsetDirection * offsetDistance;
            //        XYZ offsetPt3= pt1 - offsetDirection * offsetDistance;
            //        XYZ offsetPt2 = pt2 + offsetDirection * offsetDistance;
            //        XYZ offsetPt4 = pt2 - offsetDirection * offsetDistance;
            //        ReferencePlane rp2 = doc.Create.NewReferencePlane(offsetPt1, offsetPt2, cVec, activeView);
            //        ReferencePlane rp3 = doc.Create.NewReferencePlane(offsetPt3, offsetPt4, cVec, activeView);
            //    }
            //}, "画平行参照平面");
            //例程结束

            ////0216 测试画平行详图线，OK
            //doc.NewTransaction(() =>
            //{
            //    if (activeView.SketchPlane == null)
            //    {
            //        //获取视图的视角方向（法线方向）
            //        XYZ normal = activeView.ViewDirection;
            //        //定义工作平面的原点（通常使用视图的原点）
            //        XYZ origin = activeView.Origin;
            //        Plane workPlane = Plane.CreateByNormalAndOrigin(normal, origin);
            //        activeView.SketchPlane = SketchPlane.Create(doc, workPlane);
            //    }
            //    if (activeView.GetType().Name == "ViewPlan" || activeView.GetType().Name == "ViewSection")
            //    {
            //        XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
            //        XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
            //        var line1 = Line.CreateBound(pt1, pt2);
            //        Curve curve1 = line1 as Curve;
            //        //计算垂直于线的偏移方向（法线方向）
            //        XYZ offsetDirection = getnormal(activeView);
            //        double offsetDistance = 200 / 304.8;
            //        //计算偏移后的起点和终点
            //        XYZ offsetPt1 = line1.GetEndPoint(0) + offsetDirection * offsetDistance;
            //        XYZ offsetPt2 = line1.GetEndPoint(1) + offsetDirection * offsetDistance;
            //        //创建偏移线
            //        Line offsetLine = Line.CreateBound(offsetPt1, offsetPt2);
            //        doc.Create.NewDetailCurve(activeView, line1);
            //        doc.Create.NewDetailCurve(activeView, offsetLine);
            //    }
            //}, "画平行详图线");
            ////例程结束

            //0215 测试加属性，要通过共享属性的方式，没有深化
            //要试一下怎么把某个族的属性直接赋值到另一个上
            //bool succeeded = true;
            //string filePath = @"D:\parameters.txt";
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //fDialog.Filter = "RFA 文件 (*.rfa)|*.rfa";
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    FileInfo fileInfo = new FileInfo(fDialog.FileName);
            //    uiApp.Application.OpenDocumentFile(fileInfo.FullName);

            //    FamilyParameterAssigner assigner = new FamilyParameterAssigner(uiApp.Application, doc);
            //    // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
            //    assigner.LoadParametersFromFile();
            //    Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
            //    t.Start();
            //    assigner.AddParameters();
            //    t.Commit();
            //    doc.Save();
            //    doc.Close();
            //    //TaskDialog.Show("tt", fileInfo.Name);
            //}

            //0213 参考平面绘制方法
            // 定义视图坐标系的变换矩阵
            //Transform vTrans = Transform.Identity;
            //vTrans.BasisX = activeView.RightDirection;
            //vTrans.BasisY = activeView.UpDirection;
            //vTrans.BasisZ = activeView.ViewDirection;
            //vTrans.Origin = activeView.Origin;
            //// 定义参考平面的点（在视图坐标系中）
            //double len = 100;
            //XYZ pt1 = new XYZ(-len, 0, 0);  // 水平线起点
            //XYZ pt2 = new XYZ(len * 2, 0, 0);  // 水平线终点
            //XYZ pt3 = new XYZ(0, -len, 0);  // 垂直线起点
            //XYZ pt4 = new XYZ(0, len * 2, 0);  // 垂直线终点
            //// 将点从视图坐标系转换到模型空间
            //XYZ[] pts = new XYZ[] { pt1, pt2, pt3, pt4 };
            //for (int i = 0; i < pts.Length; i++)
            //{
            //    pts[i] = vTrans.OfPoint(pts[i]);
            //}
            //// 创建参考平面
            //using (Transaction tx = new Transaction(doc, "Create Reference Planes"))
            //{
            //    tx.Start();
            //    // 创建第一个参考平面（水平方向）
            //    ReferencePlane rp1 = doc.Create.NewReferencePlane(pts[0], pts[1], activeView.ViewDirection, activeView);
            //    // 创建第二个参考平面（垂直方向）
            //    ReferencePlane rp2 = doc.Create.NewReferencePlane(pts[2], pts[3], activeView.ViewDirection, activeView);
            //    tx.Commit();
            //}
            //例程结束

            return Result.Succeeded;
        }

        private SketchPlane CreateSketchPlane(Document docc, XYZ normal, XYZ origin)
        {
            try
            {
                Plane geometryPlane = Plane.CreateByNormalAndOrigin(normal, origin);
                if (geometryPlane == null)
                {
                    throw new Exception("创建平面失败.");
                }
                SketchPlane plane = SketchPlane.Create(docc, geometryPlane);
                if (plane == null)
                {
                    throw new Exception("创建草图平面失败.");
                }
                return plane;
            }
            catch (Exception ex)
            {
                throw new Exception("无法创建草图平面，出错原因: " + ex.Message);
            }
        }
        private DetailLine CreateDetailLine(Document doc, Autodesk.Revit.DB.View view, SketchPlane sketchPlane, XYZ startPoint, XYZ endPoint)
        {
            try
            {
                Line geometryLine = Line.CreateBound(startPoint, endPoint);
                if (geometryLine == null)
                {
                    throw new Exception("创建几何线失败.");
                }
                DetailLine line = doc.Create.NewDetailCurve(view, geometryLine) as DetailLine;
                if (line == null)
                {
                    throw new Exception("创建详图线失败.");
                }
                return line;
            }
            catch (Exception ex)
            {
                throw new Exception("无法创建详图线，出错原因: " + ex.Message);
            }
        }
        [Transaction(TransactionMode.Manual)]
        public class AddParameterToFamily : IExternalCommand
        {
            private UIApplication m_app;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                m_app = commandData.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = AddParameters();
                    if (succeeded)
                    {
                        return Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Result.Failed;
                }
            }

            private bool AddParameters()
            {
                Document doc = m_app.ActiveUIDocument.Document;
                if (null == doc)
                {
                    MessageManager.MessageBuff.Append("There's no available document. \n");
                    return false;
                }
                if (!doc.IsFamilyDocument)
                {
                    MessageManager.MessageBuff.Append("The active document is not a family document. \n");
                    return false;
                }
                FamilyParameterAssigner assigner = new FamilyParameterAssigner(m_app.Application, doc);
                // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
                bool succeeded = assigner.LoadParametersFromFile();
                if (!succeeded)
                {
                    return false;
                }
                Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
                t.Start();
                succeeded = assigner.AddParameters();
                if (succeeded)
                {
                    t.Commit();
                    return true;
                }
                else
                {
                    t.RollBack();
                    return false;
                }
            }
        } // end of class "AddParameterToFamily"
        [Transaction(TransactionMode.Manual)]
        public class AddParameterToFamilies : IExternalCommand
        {
            private Autodesk.Revit.ApplicationServices.Application application;
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                application = commandData.Application.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = LoadFamiliesAndAddParameters();

                    if (succeeded)
                    {
                        return Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Result.Failed;
                }
            }

            /// <summary>
            /// search for the family files and the corresponding parameter records
            /// load each family file, add parameters and then save and close.
            /// </summary>
            /// <returns>
            /// if succeeded, return true; otherwise false
            /// </returns>
            private bool LoadFamiliesAndAddParameters()
            {
                bool succeeded = true;
                List<string> famFilePaths = new List<string>();
                Environment.SpecialFolder myDocumentsFolder = Environment.SpecialFolder.MyDocuments;
                string myDocs = Environment.GetFolderPath(myDocumentsFolder);
                string families = myDocs + "\\AutoParameter_Families";
                if (!Directory.Exists(families))
                {
                    MessageManager.MessageBuff.Append("The folder [AutoParameter_Families] doesn't exist in [MyDocuments] folder.\n");
                }
                DirectoryInfo familiesDir = new DirectoryInfo(families);
                FileInfo[] files = familiesDir.GetFiles("*.rfa");
                if (0 == files.Length)
                {
                    MessageManager.MessageBuff.Append("No family file exists in [AutoParameter_Families] folder.\n");
                }
                foreach (FileInfo info in files)
                {
                    if (info.IsReadOnly)
                    {
                        MessageManager.MessageBuff.Append("Family file: \"" + info.FullName + "\" is read only. Can not add parameters to it.\n");
                        continue;
                    }

                    string famFilePath = info.FullName;
                    Document doc = application.OpenDocumentFile(famFilePath);

                    if (!doc.IsFamilyDocument)
                    {
                        succeeded = false;
                        MessageManager.MessageBuff.Append("Document: \"" + famFilePath + "\" is not a family document.\n");
                        continue;
                    }

                    // return and report the errors
                    if (!succeeded)
                    {
                        return false;
                    }

                    FamilyParameterAssigner assigner = new FamilyParameterAssigner(application, doc);
                    // the parameters to be added are defined and recorded in a text file, read them from that file and load to memory
                    succeeded = assigner.LoadParametersFromFile();
                    if (!succeeded)
                    {
                        MessageManager.MessageBuff.Append("Failed to load parameters from parameter files.\n");
                        return false;
                    }
                    Transaction t = new Transaction(doc, Guid.NewGuid().GetHashCode().ToString());
                    t.Start();
                    succeeded = assigner.AddParameters();
                    if (succeeded)
                    {
                        t.Commit();
                        doc.Save();
                        doc.Close();
                    }
                    else
                    {
                        t.RollBack();
                        doc.Close();
                        MessageManager.MessageBuff.Append("Failed to add parameters to " + famFilePath + ".\n");
                        return false;
                    }
                }
                return true;
            }
        } // end of class "AddParameterToFamilies"
    }
    static class MessageManager
    {
        static StringBuilder m_messageBuff = new StringBuilder();
        /// <summary>
        /// store the warning/error messages
        /// </summary>
        public static StringBuilder MessageBuff
        {
            get
            {
                return m_messageBuff;
            }
            set
            {
                m_messageBuff = value;
            }
        }
    }
}
