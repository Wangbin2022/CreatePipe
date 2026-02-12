namespace CreatePipe.PipeSystemManager.Cmd
{
    /// <summary>
    /// 管道系统管理功能
    /// </summary>
    //[Transaction(TransactionMode.Manual)]
    //public class PipeSystemManagerCmd : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        UIApplication uIApplication = commandData.Application;
    //        UIDocument uIDocument = uIApplication.ActiveUIDocument;
    //        Document document = uIDocument.Document;
    //        Autodesk.Revit.ApplicationServices.Application application = uIApplication.Application;

    //        //得到所有的管道系统
    //        FilteredElementCollector elements2 = new FilteredElementCollector(document);
    //        List<PipingSystemType> pipingSystemTypes = elements2.OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>().ToList();

    //        //加载线型，应该放到VM中
    //        FilteredElementCollector elements3 = new FilteredElementCollector(document);
    //        List<LinePatternElement> linePatternElements = elements3.OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().ToList();

    //        //创建pipeSystems，生成列表对象，应该放到VM中
    //        ObservableCollection<PipeSystemEntity> pipeSystems = new ObservableCollection<PipeSystemEntity>();
    //        foreach (PipingSystemType pipingSystemType in pipingSystemTypes)
    //        {
    //            PipeSystemEntity entity = new PipeSystemEntity();
    //            entity.SystemName = pipingSystemType.Name;
    //            entity.Abbreviation = pipingSystemType.Abbreviation;
    //            try
    //            {
    //                entity.SolidColorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(pipingSystemType.LineColor.Red, pipingSystemType.LineColor.Green, pipingSystemType.LineColor.Blue));
    //            }
    //            catch (Exception)
    //            {
    //            }
    //            entity.LineWeight = pipingSystemType.LineWeight;
    //            //查找并返回id与入参相符的element
    //            entity.LinePatternElement = linePatternElements.Find(x => x.Id == pipingSystemType.LinePatternId);

    //            //为传值定义，只传doc在VM里定义是否更实际
    //            entity.PipingSystemType = pipingSystemType;
    //            entity.PipeSystemTypeEntity = new MEPSystemTypeEntity();
    //            string name = "";
    //            MEPSystemClassification mepSt = MEPSystemClassification.SupplyHydronic;
    //            switch (pipingSystemType.SystemClassification)
    //            {
    //                case MEPSystemClassification.SupplyHydronic:
    //                    mepSt = MEPSystemClassification.SupplyHydronic;
    //                    name = "循环供水";
    //                    break;
    //                case MEPSystemClassification.ReturnHydronic:
    //                    mepSt = MEPSystemClassification.ReturnHydronic;
    //                    name = "循环回水";
    //                    break;
    //                case MEPSystemClassification.OtherPipe:
    //                    mepSt = MEPSystemClassification.OtherPipe;
    //                    name = "其他";
    //                    break;
    //                case MEPSystemClassification.FireProtectWet:
    //                    mepSt = MEPSystemClassification.FireProtectWet;
    //                    name = "湿式消防系统";
    //                    break;
    //                case MEPSystemClassification.FireProtectDry:
    //                    mepSt = MEPSystemClassification.FireProtectDry;
    //                    name = "干式消防系统";
    //                    break;
    //                case MEPSystemClassification.DomesticColdWater:
    //                    mepSt = MEPSystemClassification.DomesticColdWater;
    //                    name = "家用冷水";
    //                    break;
    //                case MEPSystemClassification.DomesticHotWater:
    //                    mepSt = MEPSystemClassification.DomesticHotWater;
    //                    name = "家用热水";
    //                    break;
    //                case MEPSystemClassification.Vent:
    //                    mepSt = MEPSystemClassification.Vent;
    //                    name = "通风孔";
    //                    break;
    //                case MEPSystemClassification.FireProtectPreaction:
    //                    mepSt = MEPSystemClassification.FireProtectPreaction;
    //                    name = "预作用消防系统";
    //                    break;
    //                case MEPSystemClassification.FireProtectOther:
    //                    mepSt = MEPSystemClassification.FireProtectOther;
    //                    name = "其他消防系统";
    //                    break;
    //                case MEPSystemClassification.Sanitary:
    //                    mepSt = MEPSystemClassification.Sanitary;
    //                    name = "卫生设备";
    //                    break;
    //            }
    //            entity.PipeSystemTypeEntity.Name = name;
    //            entity.PipeSystemTypeEntity.MEPSystemClassification = mepSt;
    //            pipeSystems.Add(entity);
    //        }

    //        //实例化并显示主窗体 
    //        PipeSystemListForm pipeSystemListForm = new PipeSystemListForm(pipeSystems, document, linePatternElements);
    //        pipeSystemListForm.ShowDialog();
    //        return Result.Succeeded;
    //    }
    //}
}
