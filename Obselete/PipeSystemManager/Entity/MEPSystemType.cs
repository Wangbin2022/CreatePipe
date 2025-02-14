using Autodesk.Revit.DB;

namespace CreatePipe.PipeSystemManager.Entity
{
    public class MEPSystemTypeEntity
    {
        public string Name { get; set; }
        public MEPSystemClassification MEPSystemClassification { get; set; }
    }
}
