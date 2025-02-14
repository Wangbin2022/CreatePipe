using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace CreatePipe.CableConduitCreator
{
    public class ConnctorContains : IEqualityComparer<Connector>
    {
        public bool Equals(Connector x, Connector y)
        {
            return x.Owner.Id == y.Owner.Id && x.Origin.IsAlmostEqualTo(y.Origin);
        }
        public int GetHashCode(Connector obj)
        {
            return obj.GetHashCode();
        }
    }
}