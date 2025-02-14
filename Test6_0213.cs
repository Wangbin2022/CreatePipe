using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test6_0213 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();


            XYZ pt1 = uiDoc.Selection.PickPoint("请选择参考平面的起点");
            XYZ pt2 = uiDoc.Selection.PickPoint("请选择参考平面的终点");
            XYZ direction = (pt2 - pt1).Normalize();
            //// 定义视图坐标系的变换矩阵
            //Transform vTrans = Transform.Identity;
            //vTrans.BasisX = activeView.RightDirection;
            //vTrans.BasisY = activeView.UpDirection;
            //vTrans.BasisZ = activeView.ViewDirection;
            //vTrans.Origin = activeView.Origin;


            //XYZ globalPt1 = vTrans.OfPoint(pt1);
            //XYZ globalPt2 = vTrans.OfPoint(pt2);


            // 创建参考平面
            using (Transaction tx = new Transaction(doc, "Create Reference Planes"))
            {
                tx.Start();
                // 创建第一个参考平面（水平方向）
                ReferencePlane rp1 = doc.Create.NewReferencePlane(pt1, pt2, direction, activeView);
                tx.Commit();
            }






            return Result.Succeeded;
        }


        [Transaction(TransactionMode.Manual)]
        public class AddParameterToFamily : IExternalCommand
        {
            private Autodesk.Revit.UI.UIApplication m_app;

            public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                m_app = commandData.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = AddParameters();
                    if (succeeded)
                    {
                        return Autodesk.Revit.UI.Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Autodesk.Revit.UI.Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Autodesk.Revit.UI.Result.Failed;
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
            private Autodesk.Revit.ApplicationServices.Application m_app;
            public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                m_app = commandData.Application.Application;
                MessageManager.MessageBuff = new StringBuilder();

                try
                {
                    bool succeeded = LoadFamiliesAndAddParameters();

                    if (succeeded)
                    {
                        return Autodesk.Revit.UI.Result.Succeeded;
                    }
                    else
                    {
                        message = MessageManager.MessageBuff.ToString();
                        return Autodesk.Revit.UI.Result.Failed;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return Autodesk.Revit.UI.Result.Failed;
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
                    Document doc = m_app.OpenDocumentFile(famFilePath);

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

                    FamilyParameterAssigner assigner = new FamilyParameterAssigner(m_app, doc);
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
