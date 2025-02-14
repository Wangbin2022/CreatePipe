using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace CreatePipe.CableConduitCreator
{
    public class ShowElem : IExternalEventHandler
    {
        public ElementId OutOfCableElemId { get; set; }
        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Selection sel = uidoc.Selection;
            sel.SetElementIds(new List<ElementId> { OutOfCableElemId });
            uidoc.ShowElements(OutOfCableElemId);
        }

        public string GetName()
        {
            throw new System.NotImplementedException();
        }
    }
}