using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CreatePipe.MEPevent
{

    public delegate void External();
    /// <summary>
    /// 外部事件
    /// </summary>
    public class ExternalEventExample : IExternalEventHandler
    {
        public External External { get; set; }

        public ExternalEvent ExternalEvent { get; set; }

        UIDocument uIDocument = null;
        Document document = null;
        Application application = null;

        public ExternalEventExample(ExternalCommandData commandData)
        {

            UIApplication uiApp = commandData.Application;
            application = uiApp.Application;
            uIDocument = uiApp.ActiveUIDocument;
            document = uIDocument.Document;
        }

        // public static int id=-1;
        public void Execute(UIApplication app)
        {

            //委托回调
            External?.Invoke();
        }

        public string GetName()
        {
            return "name";
        }

        //注册事件
        public ExternalEvent CreateExternalEvent(ExternalEventExample externalEventExample)
        {
            //注册外部事件
            ExternalEvent = ExternalEvent.Create(externalEventExample);
            return ExternalEvent;
        }

        /// <summary>
        /// 执行外部事件
        /// </summary>
        public void Implement()
        {
            ExternalEvent.Raise();

        }

        /// <summary>
        /// 清除委托
        /// </summary>
        public void ClearExternal()
        {
            External = null;
        }
    }
}
