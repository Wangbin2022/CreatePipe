using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace CreatePipe.CableConduitCreator
{
    public class PathListVM
    {
        public string PathInfo { get; set; }
        public List<ElementId> InternalPath { get; }
        public double PathLength { get; set; }
        public PathListVM(List<ElementId> path, int pathNumber, double pathLength)
        {
            PathLength = pathLength * 304.8;
            PathInfo = "路径：" + pathNumber.ToString() + "号 =》" + $"路径长度为：{(PathLength / 1000).ToString("#0.00")}m";
            InternalPath = path;
        }
    }
}
