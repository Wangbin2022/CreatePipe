using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;

namespace CreatePipe.RoomAttr
{
    [Transaction(TransactionMode.Manual)]
    public class RoomAttrCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            IntPtr maindHwnd = WindowHelper.FindWindow(null, "房间管理器");//主窗口标题
            if (maindHwnd != IntPtr.Zero)
            {
                TaskDialog.Show("标题", "请勿重复创建命令！");
                return Result.Cancelled;
            }
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document Doc = commandData.Application.ActiveUIDocument.Document;
            //外部事件传参0115
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();
            // 显示窗口
            RoomAttrForm window = new RoomAttrForm(uiApp);
            //window.ShowDialog();
            window.Show();
            return Result.Succeeded;
        }
    }
}
