using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe
{
    public class ConnectorService
    {
        /// <summary>
        /// 获取元素所有的连接器 (支持 MEPCurve 和 FamilyInstance)
        /// </summary>
        public static IEnumerable<Connector> GetConnectors(Element element)
        {
            if (element == null) yield break;

            ConnectorManager cm = null;
            if (element is MEPCurve mepCurve)
            {
                cm = mepCurve.ConnectorManager;
            }
            else if (element is FamilyInstance fi && fi.MEPModel != null)
            {
                cm = fi.MEPModel.ConnectorManager;
            }

            if (cm != null)
            {
                foreach (Connector conn in cm.Connectors)
                {
                    yield return conn;
                }
            }
        }

        /// <summary>
        /// 获取第一个未连接的连接器
        /// </summary>
        public static Connector GetUnusedConnector(Element element)
        {
            return GetConnectors(element).FirstOrDefault(c => !c.IsConnected);
        }

        /// <summary>
        /// 获取与指定点最近的连接器
        /// </summary>
        public static Connector GetClosestConnector(Element element, XYZ point)
        {
            return GetConnectors(element)
                .OrderBy(c => c.Origin.DistanceTo(point))
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取与某个连接器物理连接的其他连接器 (排除自身)
        /// </summary>
        public static IEnumerable<Connector> GetConnectedRefs(Connector connector)
        {
            foreach (Connector refConn in connector.AllRefs)
            {
                // 排除非物理连接（如系统逻辑连接）和 自身所属元素的其他连接器
                if (refConn.ConnectorType == ConnectorType.End ||
                    refConn.ConnectorType == ConnectorType.Curve ||
                    refConn.ConnectorType == ConnectorType.Physical)
                {
                    if (refConn.Owner.Id != connector.Owner.Id)
                    {
                        yield return refConn;
                    }
                }
            }
        }

        /// <summary>
        /// 递归/广度优先获取所有相连的元素ID
        /// </summary>
        public static List<ElementId> GetAllConnectedElementIds(Element startElement)
        {
            List<ElementId> connectedIds = new List<ElementId>();
            Queue<Element> elementsToProcess = new Queue<Element>();
            HashSet<ElementId> visited = new HashSet<ElementId>();

            elementsToProcess.Enqueue(startElement);
            visited.Add(startElement.Id);

            while (elementsToProcess.Count > 0)
            {
                Element current = elementsToProcess.Dequeue();

                foreach (Connector conn in GetConnectors(current))
                {
                    foreach (Connector refConn in GetConnectedRefs(conn))
                    {
                        Element nextElement = refConn.Owner;
                        if (!visited.Contains(nextElement.Id))
                        {
                            visited.Add(nextElement.Id);
                            connectedIds.Add(nextElement.Id);
                            elementsToProcess.Enqueue(nextElement);
                        }
                    }
                }
            }
            return connectedIds;
        }

        /// <summary>
        /// 安全断开连接并返回对方连接器，用于后续恢复连接
        /// </summary>
        public static Connector DisconnectFromVendor(Connector source)
        {
            Connector target = GetConnectedRefs(source).FirstOrDefault();
            if (target != null)
            {
                source.DisconnectFrom(target);
                return target;
            }
            return null;
        }
    }
}
