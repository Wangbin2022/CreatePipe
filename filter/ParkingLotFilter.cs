using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace CreatePipe.filter
{
    public class ParkingLotFilter : ISelectionFilter
    {
        // 外部通过这个标志通知Filter中断选择
        public bool ShouldStop { get; set; } = false;
        public bool AllowElement(Element elem)
        {
            // 关键：当外部设置ShouldStop时，抛出异常打断PickObject阻塞
            if (ShouldStop)
            {
                TaskDialog.Show("tt", "已停止选择");
                return false;

            }
            else if (elem is FamilyInstance familyInstance)
            {
                //获取 FamilySymbol
                FamilySymbol familySymbol = familyInstance.Symbol;
                if (familySymbol.Family.Name.Contains("车位"))
                {
                    return true;
                }
            }
            return false;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
        //public bool AllowElement(Element elem)
        //{
        //    if (elem is FamilyInstance familyInstance)
        //    {
        //        //获取 FamilySymbol
        //        FamilySymbol familySymbol = familyInstance.Symbol;
        //        if (familySymbol.Family.Name.Contains("车位"))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //public bool AllowReference(Reference reference, XYZ position)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
