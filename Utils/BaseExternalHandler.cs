using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.Utils
{
    public class BaseExternalHandler
    {
        private ExternalEventHandler Handler { get; set; }
        private ExternalEvent ExternalEvent { get; set; }
        public BaseExternalHandler()
        {
            Handler = new ExternalEventHandler();
            ExternalEvent = ExternalEvent.Create(Handler);
        }
        public void Run(Action<UIApplication> action)
        {
            Handler.Action = action;
            ExternalEvent.Raise();
        }
        public class ExternalEventHandler : IExternalEventHandler
        {
            public Action<UIApplication> Action { get; set; }
            public void Execute(UIApplication app)
            {
                try
                {
                    Action?.Invoke(app);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", "Revit外部事件发生错误：" + ex.Message + "\n堆栈信息：" + ex.StackTrace);
                }
            }
            public string GetName()
            {
                return "Revit外部事件";
            }
        }
    } 
}
