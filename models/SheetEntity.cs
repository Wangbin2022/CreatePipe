using Autodesk.Revit.DB;
using CreatePipe.cmd;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class SheetEntity : ObserverableObject
    {
        ViewSheet View { get; set; }
        Document Document { get => View.Document; }
        public SheetEntity(ViewSheet sheetView)
        {
            View = sheetView;
            sheetName= sheetView.Name;
            Id = sheetView.Id;
            sheetNum=sheetView.SheetNumber;

            viewPortCount=new FilteredElementCollector(Document, Id).OfCategory(BuiltInCategory.OST_Viewports).Count();
        }
        public int viewPortCount { get; set; } = 0;
        public string sheetNum { get; set; }
        public ElementId Id { get; set; }
        public string sheetName { get; set; }
    }
}
