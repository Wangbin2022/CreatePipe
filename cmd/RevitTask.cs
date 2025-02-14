using Autodesk.Revit.UI;
using System;
using System.Windows;

namespace CreatePipe.cmd
{
    public class RevitTask
    {
        private ExternalEventHandler Handler { get; set; }
        private ExternalEvent ExternalEvent { get; set; }
        public RevitTask()
        {
            Handler = new ExternalEventHandler();
            ExternalEvent = ExternalEvent.Create(Handler);
        }
        public void Run(Action<UIApplication> action)
        {
            Handler.Action = action;
            ExternalEvent.Raise();
        }
        internal class ExternalEventHandler : IExternalEventHandler
        {
            public Action<UIApplication> Action { get; set; }
            public void Execute(UIApplication app)
            {
                try
                {
                    Action(app);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(GetName(), ex.Message);
                }
            }
            public string GetName()
            {
                return "请重试";
            }
        }
    }
    public class XmlDoc
    {
        public XmlDoc() { }
        private static readonly XmlDoc Global = new XmlDoc();
        public static XmlDoc Instance => Global ?? new XmlDoc();
        public UIDocument UIDoc { get; set; }
        public RevitTask Task { get; set; }
    }
}
