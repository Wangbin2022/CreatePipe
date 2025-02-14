namespace CreatePipe
{
    //public class PipeCreator
    //{
    //    UIDocument uidoc;

    //    public PipeCreator(UIDocument uidoc)
    //    {
    //        this.uidoc = uidoc;

    //    }

    //    public void GetResult()
    //    { 
    //        //获取第一个参数Document
    //        Document doc = uidoc.Document;
    //        //获取第二个参数systemTypeId
    //        FilteredElementCollector systemTypeCol = new FilteredElementCollector(doc);
    //        PipingSystemType pipingSystemType = (PipingSystemType) systemTypeCol.OfClass (typeof(PipingSystemType)).First();
    //        ElementId systemTypeId = pipingSystemType.Id;
    //        //获取第三个参数pipeTypeId
    //        ElementId pipeTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.PipeType);
    //        //获取第四参数levelId
    //        ElementId levelId= uidoc.ActiveView.GenLevel.Id;
    //        //获取最终两个参数
    //        XYZ startPoint = new XYZ(0,0,0);
    //        XYZ endPoint = new XYZ(1500/304.8,0,0);
    //        //创建管道
    //        Pipe pipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, startPoint, endPoint);
    //    }
    //}
}
