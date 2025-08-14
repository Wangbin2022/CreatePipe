using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.models
{
    public class SheetEntity : ObserverableObject
    {
        ViewSheet View { get; set; }
        Document Document { get => View.Document; }
        public SheetEntity(ViewSheet sheetView)
        {
            View = sheetView;
            sheetName = sheetView.Name;
            Id = sheetView.Id;
            sheetNum = sheetView.SheetNumber;

            //var viewPorts = new FilteredElementCollector(Document, Id).OfCategory(BuiltInCategory.OST_Viewports);
            var views = sheetView.GetAllPlacedViews();
            viewCount = views.Count();
            foreach (var viewId in views)
            {
                View view = Document.GetElement(viewId) as View;
                relatedViews[viewId.IntegerValue.ToString()] = Document.GetElement(viewId).Name + "+比例1：" + view.Scale;
            }
        }
        public Dictionary<string, string> relatedViews = new Dictionary<string, string>();
        public int viewCount { get; set; } = 0;
        public string sheetNum { get; set; }
        public ElementId Id { get; set; }
        public string sheetName { get; set; }
    }
}
