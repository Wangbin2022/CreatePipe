using Autodesk.Revit.DB;

namespace CreatePipe.CableConduitCreator
{
    public static class Extension
    {
        public static Element GetElement(this Reference refe, Document doc)
        {
            return doc.GetElement(refe);
        }
        public static Element GetElement(this ElementId id, Document doc)
        {
            return doc.GetElement(id);
        }
    }
}
