using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using CreatePipe.Utils;
using System.Collections.Generic;

namespace CreatePipe.CableConduitCreator
{
    public class OutOfCableListVM
    {
        //注意diction也要初始化
        public ObservableDictionary<ElementId, string> Ids { get; set; } = new ObservableDictionary<ElementId, string>();
        public OutOfCableListVM(Document doc, List<ElementId> ids)
        {
            foreach (ElementId id in ids)
            {
                Conduit tempCoun = id.GetElement(doc) as Conduit;
                string tempStr = null;
                try
                {
                    //tempStr = (tempCoun.GetTypeId().GetElement(doc) as Conduit).Name + "" + tempCoun.LookupParameter("直径(公称尺寸)").AsValueString();
                    tempStr = (tempCoun.GetTypeId().GetElement(doc) as Conduit).Name + "" + tempCoun.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsValueString();
                }
                catch
                {
                    continue;
                }
                Ids.Add(id, tempStr);
            }
        }
    }
}
