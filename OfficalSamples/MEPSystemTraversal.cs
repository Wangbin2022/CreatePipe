using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace CreatePipe.OfficalSamples
{
    internal class MEPSystemTraversal
    {
        private const string XmlIndentChars = "    ";
        private const string DefaultXmlFileName = "traversal.xml";
        public MEPSystemTraversal(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                // 验证活动文档
                if (!TryGetActiveDocument(commandData, out var uiDocument, out string error))
                {
                    message = error; return;
                }
                // 验证选中的元素数量
                var selectedElement = GetSingleSelectedElement(uiDocument);
                if (selectedElement is null)
                {
                    message = "请只选择一个元素"; return;
                }
                // 提取MEP系统
                var system = ExtractMEPSystem(selectedElement);
                if (system is null)
                {
                    message = GetUnsupportedSystemMessage(); return;
                }
                // 遍历系统并导出XML
                var traversalTree = new TraversalTree(uiDocument.Document, system);
                traversalTree.Traverse();
                var xmlPath = GetXmlOutputPath();
                traversalTree.DumpToXml(xmlPath);
                TaskDialog.Show("成功", $"系统遍历完成，结果已保存至:\n{xmlPath}");
            }
            catch (Exception ex)
            {
                message = ex.Message; return;
            }
        }
        #region 辅助方法
        /// <summary>
        /// 尝试获取活动文档
        /// </summary>
        private bool TryGetActiveDocument(ExternalCommandData commandData,
            out UIDocument uiDocument,
            out string error)
        {
            uiDocument = commandData?.Application?.ActiveUIDocument;

            if (uiDocument is null)
            {
                error = "没有活动的Revit文档";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 获取单个选中的元素
        /// </summary>
        private Element GetSingleSelectedElement(UIDocument uiDocument)
        {
            var selectedIds = uiDocument.Selection.GetElementIds().ToList();

            if (selectedIds.Count != 1) return null;

            return uiDocument.Document.GetElement(selectedIds[0]);
        }

        /// <summary>
        /// 提取MEP系统（机械或管道系统）
        /// </summary>
        private MEPSystem ExtractMEPSystem(Element element)
        {
            if (element is MechanicalSystem)
            {
                var ms = (MechanicalSystem)element;
                return ms;
            }
            else if (element is PipingSystem)
            {
                var ps = (PipingSystem)element;
                return ps;
            }
            else if (element is FamilyInstance)
            {
                var fi = (FamilyInstance)element;
                return ExtractSystemFromFamilyInstance(fi);
            }
            else if (element is MEPCurve)
            {
                var curve = (MEPCurve)element;
                return ExtractSystemFromMEPCurve(curve);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 从族实例提取系统
        /// </summary>
        private MEPSystem ExtractSystemFromFamilyInstance(FamilyInstance familyInstance)
        {
            try
            {
                var connectors = familyInstance?.MEPModel?.ConnectorManager?.Connectors;
                return ExtractSystemFromConnectors(connectors);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从MEP曲线提取系统
        /// </summary>
        private MEPSystem ExtractSystemFromMEPCurve(MEPCurve curve)
        {
            var connectors = curve?.ConnectorManager?.Connectors;
            return ExtractSystemFromConnectors(connectors);
        }

        /// <summary>
        /// 从连接器集合中提取系统（选择元素最多的系统）
        /// </summary>
        private MEPSystem ExtractSystemFromConnectors(ConnectorSet connectors)
        {
            if (connectors is null || connectors.Size == 0) return null;

            // 收集所有良好连接的系统
            var validSystems = new List<MEPSystem>();

            foreach (Connector connector in connectors)
            {
                var system = connector.MEPSystem;
                if (system is null) continue;

                bool isWellConnected;
                if (system is MechanicalSystem)
                {
                    var ms = (MechanicalSystem)system;
                    isWellConnected = ms.IsWellConnected;
                }
                else if (system is PipingSystem)
                {
                    var ps = (PipingSystem)system;
                    isWellConnected = ps.IsWellConnected;
                }
                else
                {
                    isWellConnected = false;
                }

                if (isWellConnected)
                {
                    validSystems.Add(system);
                }
            }

            // 返回包含元素最多的系统
            return validSystems
                .OrderByDescending(s => s.Elements.Size)
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取不支持系统的错误消息
        /// </summary>
        private string GetUnsupportedSystemMessage()
        {
            return "选中的元素不属于任何良好连接的机械或管道系统。" +
                   "本示例仅支持良好连接的系统，原因如下：\n" +
                   "- 非良好连接系统中沿流向遍历可能丢失元素\n" +
                   "- 非良好连接系统中元素的流向可能不正确";
        }

        /// <summary>
        /// 获取XML输出路径
        /// </summary>
        private string GetXmlOutputPath()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(directory ?? Environment.CurrentDirectory, DefaultXmlFileName);
        }
        #endregion
    }
    /// <summary>
    /// 树节点 - 表示系统中的单个元素
    /// </summary>
    public class TreeNode
    {
        #region 属性
        public ElementId Id { get; }
        public FlowDirectionType Direction { get; set; }
        public TreeNode Parent { get; set; }
        public Connector InputConnector { get; set; }
        public List<TreeNode> ChildNodes { get; }
        private readonly Document _document;
        #endregion

        #region 构造函数
        public TreeNode(Document document, ElementId id)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            Id = id;
            ChildNodes = new List<TreeNode>();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 将节点导出到XML
        /// </summary>
        public void DumpToXml(XmlWriter writer)
        {
            var element = _document.GetElement(Id);
            if (element is null) return;

            WriteElementStart(writer, element);
            WriteElementAttributes(writer, element);
            writer.WriteEndElement();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 写入元素开始标签
        /// </summary>
        private void WriteElementStart(XmlWriter writer, Element element)
        {
            string elementType = GetElementTypeName(element);
            writer.WriteStartElement(elementType);
        }

        /// <summary>
        /// 获取元素类型名称
        /// </summary>
        private string GetElementTypeName(Element element)
        {
            if (element is FamilyInstance)
            {
                var fi = (FamilyInstance)element;
                if (fi.MEPModel is MechanicalEquipment)
                {
                    return "MechanicalEquipment";
                }
                else if (fi.MEPModel is MechanicalFitting)
                {
                    return "MechanicalFitting";
                }
                else
                {
                    return "FamilyInstance";
                }
            }
            else
            {
                return element.GetType().Name;
            }
        }

        /// <summary>
        /// 写入元素属性
        /// </summary>
        private void WriteElementAttributes(XmlWriter writer, Element element)
        {
            writer.WriteAttributeString("Name", element.Name ?? "Unnamed");
            writer.WriteAttributeString("Id", element.Id.IntegerValue.ToString());
            writer.WriteAttributeString("Direction", Direction.ToString());

            // 为族实例写入额外属性
            if (element is FamilyInstance fi && fi.MEPModel != null)
            {
                writer.WriteAttributeString("Category", element.Category?.Name ?? "Unknown");

                if (fi.MEPModel is MechanicalFitting mf)
                {
                    writer.WriteAttributeString("PartType", mf.PartType.ToString());
                }
            }
            else if (element.Category != null)
            {
                writer.WriteAttributeString("Category", element.Category.Name);
            }
        }
        #endregion
    }

    /// <summary>
    /// 遍历树数据结构
    /// </summary>
    public class TraversalTree
    {
        #region 私有成员
        private readonly Document _document;
        private readonly MEPSystem _system;
        private readonly bool _isMechanicalSystem;
        private TreeNode _rootNode;
        #endregion

        #region 构造函数
        public TraversalTree(Document document, MEPSystem system)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _isMechanicalSystem = system is MechanicalSystem;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 遍历整个系统
        /// </summary>
        public void Traverse()
        {
            _rootNode = GetStartingNode();
            TraverseRecursively(_rootNode);
        }

        /// <summary>
        /// 导出到XML文件
        /// </summary>
        public void DumpToXml(string filePath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                //IndentChars = Command.XmlIndentChars
            };

            var writer = XmlWriter.Create(filePath, settings);

            // 写入根元素
            string rootName = _isMechanicalSystem ? "MechanicalSystem" : "PipingSystem";
            writer.WriteStartElement(rootName);

            // 写入基本信息
            WriteBasicInfo(writer);

            // 写入遍历路径
            writer.WriteStartElement("Path");
            _rootNode?.DumpToXml(writer);
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Flush();
        }
        #endregion

        #region 遍历算法
        /// <summary>
        /// 获取起始节点
        /// </summary>
        private TreeNode GetStartingNode()
        {
            ElementId startId;

            if (_system.BaseEquipment != null)
            {
                startId = _system.BaseEquipment.Id;
            }
            else
            {
                var openConnectorOwner = GetOpenConnectorOwner();
                startId = openConnectorOwner?.Id;
            }

            if (startId is null) return null;

            return new TreeNode(_document, startId)
            {
                Parent = null,
                InputConnector = null
            };
        }

        /// <summary>
        /// 获取开放连接器的所属元素（作为起始点）
        /// </summary>
        private Element GetOpenConnectorOwner()
        {
            // 获取系统中的任意一个元素
            var firstElement = _system.Elements
                .Cast<Element>()
                .FirstOrDefault();

            if (firstElement is null) return null;

            var openConnector = FindOpenConnector(firstElement, null);
            return openConnector?.Owner;
        }

        /// <summary>
        /// 递归查找开放连接器
        /// </summary>
        private Connector FindOpenConnector(Element element, Connector inputConnector)
        {
            var connectors = GetConnectors(element);
            if (connectors is null) return null;

            foreach (Connector connector in connectors)
            {
                // 跳过不属于当前系统的连接器
                if (!IsConnectorInSystem(connector)) continue;

                // 跳过输入连接器
                if (inputConnector != null && connector.IsConnectedTo(inputConnector)) continue;

                // 找到开放连接器
                if (!connector.IsConnected)
                    return connector;

                // 递归查找连接的元件
                var connectedConnector = FindConnectedConnector(connector, inputConnector);
                if (connectedConnector != null)
                {
                    var result = FindOpenConnector(connectedConnector.Owner, connector);
                    if (result != null) return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 递归遍历系统
        /// </summary>
        private void TraverseRecursively(TreeNode node)
        {
            if (node is null) return;

            FindChildNodes(node);

            foreach (var child in node.ChildNodes)
            {
                TraverseRecursively(child);
            }
        }

        /// <summary>
        /// 查找子节点
        /// </summary>
        private void FindChildNodes(TreeNode parentNode)
        {
            var connectors = GetConnectors(parentNode.Id);
            if (connectors is null) return;

            var childNodes = new List<TreeNode>();

            foreach (Connector connector in connectors)
            {
                // 跳过不属于当前系统的连接器
                if (!IsConnectorInSystem(connector)) continue;

                // 设置流向
                if (parentNode.Parent is null)
                {
                    if (connector.IsConnected)
                        parentNode.Direction = connector.Direction;
                }
                else
                {
                    if (connector.IsConnectedTo(parentNode.InputConnector))
                    {
                        parentNode.Direction = connector.Direction;
                        continue;
                    }
                }

                // 获取连接的连接器
                var connectedConnector = FindConnectedConnector(connector, parentNode.InputConnector);
                if (connectedConnector != null)
                {
                    var childNode = new TreeNode(_document, connectedConnector.Owner.Id)
                    {
                        InputConnector = connector,
                        Parent = parentNode
                    };
                    childNodes.Add(childNode);
                }
            }

            // 按ID排序
            parentNode.ChildNodes.AddRange(childNodes.OrderBy(n => n.Id.IntegerValue));
        }

        /// <summary>
        /// 查找连接的连接器
        /// </summary>
        private Connector FindConnectedConnector(Connector connector, Connector ignoreConnector)
        {
            foreach (Connector refConn in connector.AllRefs)
            {
                // 跳过非EndConn连接器和当前元素的连接器
                if (refConn.ConnectorType != ConnectorType.End) continue;
                if (refConn.Owner.Id == connector.Owner.Id) continue;

                // 跳过忽略的连接器
                if (ignoreConnector != null && refConn.Owner.Id == ignoreConnector.Owner.Id) continue;

                return refConn;
            }

            return null;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取元素的连接器集合
        /// </summary>
        private ConnectorSet GetConnectors(Element element)
        {
            if (element is FamilyInstance)
            {
                var fi = (FamilyInstance)element;
                return fi.MEPModel?.ConnectorManager?.Connectors;
            }
            else if (element is MEPCurve)
            {
                var curve = (MEPCurve)element;
                return curve.ConnectorManager?.Connectors;
            }
            return null;
        }

        /// <summary>
        /// 根据ID获取元素
        /// </summary>
        private ConnectorSet GetConnectors(ElementId id)
        {
            var element = _document.GetElement(id);
            return GetConnectors(element);
        }

        /// <summary>
        /// 检查连接器是否属于当前系统
        /// </summary>
        private bool IsConnectorInSystem(Connector connector)
        {
            var system = connector.MEPSystem;
            return system != null && system.Id == _system.Id;
        }

        /// <summary>
        /// 写入系统基本信息到XML
        /// </summary>
        private void WriteBasicInfo(XmlWriter writer)
        {
            writer.WriteStartElement("BasicInformation");

            WriteProperty(writer, "Name", _system.Name);
            WriteProperty(writer, "Id", _system.Id.IntegerValue);
            WriteProperty(writer, "UniqueId", _system.UniqueId);

            // 系统类型
            string systemType;
            if (_system is MechanicalSystem)
            {
                var ms = (MechanicalSystem)_system;
                systemType = ms.SystemType.ToString();
            }
            else if (_system is PipingSystem)
            {
                var ps = (PipingSystem)_system;
                systemType = ps.SystemType.ToString();
            }
            else
            {
                systemType = "Unknown";
            }
            WriteProperty(writer, "SystemType", systemType);

            // 类别信息
            writer.WriteStartElement("Category");
            writer.WriteAttributeString("Id", _system.Category.Id.IntegerValue.ToString());
            writer.WriteAttributeString("Name", _system.Category.Name);
            writer.WriteEndElement();

            // 良好连接状态
            bool isWellConnected;
            if (_system is MechanicalSystem)
            {
                var ms = (MechanicalSystem)_system;
                isWellConnected = ms.IsWellConnected;
            }
            else if (_system is PipingSystem)
            {
                var ps = (PipingSystem)_system;
                isWellConnected = ps.IsWellConnected;
            }
            else
            {
                isWellConnected = false;
            }
            WriteProperty(writer, "IsWellConnected", isWellConnected);
            WriteProperty(writer, "HasBaseEquipment", _system.BaseEquipment != null);
            WriteProperty(writer, "TerminalElementsCount", _system.Elements.Size);

            // 流量
            double flow;
            if (_system is MechanicalSystem)
            {
                var ms = (MechanicalSystem)_system;
                flow = ms.GetFlow();
            }
            else if (_system is PipingSystem)
            {
                var ps = (PipingSystem)_system;
                flow = ps.GetFlow();
            }
            else
            {
                flow = 0;
            }
            WriteProperty(writer, "Flow", flow);

            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入字符串属性
        /// </summary>
        private void WriteProperty(XmlWriter writer, string name, string value)
        {
            writer.WriteStartElement(name);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入数值属性
        /// </summary>
        private void WriteProperty(XmlWriter writer, string name, object value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }
        #endregion
    }

}
