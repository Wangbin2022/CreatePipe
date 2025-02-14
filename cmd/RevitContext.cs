using Autodesk.Revit.DB;

namespace CreatePipe.cmd
{
    public static class RevitContext
    {
        public static Autodesk.Revit.ApplicationServices.Application Application { get; set; }
        public static Document Document { get; set; }
    }
}
