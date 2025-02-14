using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreatePipe.CableConduitCreator
{
    //输出xls信息，注意\t代表xls的跳格，暂时按原例子改的
    public class allCreatedIdsVM
    {
        public List<List<ElementId>> AllIds { get; set; }
        public string AllParams { get; set; } = string.Empty;
        public allCreatedIdsVM(Document doc, List<List<ElementId>> allIds)
        {
            AllIds = allIds;
            AllParams = GetAllIdsToString(doc, allIds);
        }
        private string GetAllIdsToString(Document doc, List<List<ElementId>> allIds)
        {
            StringBuilder sb = new StringBuilder();
            int times = 1;
            foreach (List<ElementId> ids in allIds)
            {
                Conduit temp = ids.First(i => i.GetElement(doc) is Conduit).GetElement(doc) as Conduit;
                ////double runLength = temp.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                //遗留问题，此处runid始终=-1，无法取得线管全长
                double runLength = (temp.RunId.GetElement(doc) as ConduitRun).Length;
                sb.AppendLine($"第{times}组 =》 线路总长度为：\t{Math.Round(runLength * 304.8, 2)}\tmm");
                foreach (ElementId id in ids)
                {
                    Element elem = id.GetElement(doc);
                    if (elem is Conduit)
                    {
                        Conduit con = elem as Conduit;
                        sb.AppendLine($"\t\t\tID：\t{id.IntegerValue}\t" +
                            $"类型：\t{(con.GetTypeId().GetElement(doc) as ConduitType).Name}\t" +
                            $"尺寸：\t{con.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsValueString()}\t" +
                            $"长度：\t{con.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString()}mm");
                    }
                    else
                    {
                        Parameter elbowLengthParam = null;
                        double elbowLength = -1;
                        try
                        {
                            elbowLengthParam = elem.LookupParameter("弯头长度");
                            elbowLength = elbowLengthParam.AsDouble() * 304.8;
                        }
                        catch
                        {
                            //TaskDialog.Show("tt", "缺少【弯头长度】参数，将使用近似值代替");
                            elbowLengthParam = elem.LookupParameter("中心到端点");
                            elbowLength = elbowLengthParam.AsDouble() * 2 * 304.8;
                        }
                        sb.AppendLine($"\t\t\tID：\t{id.IntegerValue}\t" +
                            $"弯曲角度：\t{elem.LookupParameter("角度").AsValueString()}\t" +
                            $"尺寸：\t{elem.LookupParameter("公称直径").AsValueString()}\t" +
                            $"长度：\t{Math.Round(elbowLength, 2)}mm");
                    }
                }
                times++;
            }
            return sb.ToString();
        }
    }
}
