using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DuctSystemWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DuctSystemWindow : Window
    {
        public DuctSystemWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 风管系统创建视图模型
    /// </summary>
    public class DuctSystemViewModel : ObserverableObject
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;

        private bool _isProcessing;
        private string _statusMessage;
        private DuctSystemResult _lastResult;
        private ObservableCollection<ElementItem> _availableElements;
        private ElementItem _selectedEquipment;
        private ObservableCollection<ElementItem> _selectedTerminals;
        private string _ductTypeIdInput = "139191";

        public DuctSystemViewModel(ExternalCommandData commandData)
        {
            _uiDoc = commandData.Application.ActiveUIDocument;
            _doc = commandData.Application.ActiveUIDocument.Document;
            _selectedTerminals = new ObservableCollection<ElementItem>();
            _availableElements = new ObservableCollection<ElementItem>();

            // 初始化命令
            CreateSystemCommand = new BaseBindingCommand(_ => CreateSystem(), _ => CanCreateSystem);
            SelectFromModelCommand = new BaseBindingCommand(_ => SelectFromModel());
            AddTerminalCommand = new BaseBindingCommand(_ => AddSelectedTerminal(), _ => SelectedAvailableItem != null);
            RemoveTerminalCommand = new BaseBindingCommand(_ => RemoveSelectedTerminal(), _ => SelectedTerminal != null);
            CancelCommand = new BaseBindingCommand(_ => CloseAction?.Invoke());

            LoadAvailableElements();
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCreateSystem)); }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 最后执行结果
        /// </summary>
        public DuctSystemResult LastResult
        {
            get => _lastResult;
            set { _lastResult = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 可用构件列表
        /// </summary>
        public ObservableCollection<ElementItem> AvailableElements
        {
            get => _availableElements;
            set { _availableElements = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 选中的设备
        /// </summary>
        public ElementItem SelectedEquipment
        {
            get => _selectedEquipment;
            set { _selectedEquipment = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCreateSystem)); }
        }

        /// <summary>
        /// 选中的末端列表
        /// </summary>
        public ObservableCollection<ElementItem> SelectedTerminals
        {
            get => _selectedTerminals;
            set { _selectedTerminals = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的可用构件
        /// </summary>
        public ElementItem SelectedAvailableItem { get; set; }

        /// <summary>
        /// 当前选中的末端
        /// </summary>
        public ElementItem SelectedTerminal { get; set; }

        /// <summary>
        /// 风管类型ID
        /// </summary>
        public string DuctTypeIdInput
        {
            get => _ductTypeIdInput;
            set { _ductTypeIdInput = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否可以创建系统
        /// </summary>
        public bool CanCreateSystem => !IsProcessing && SelectedEquipment != null && SelectedTerminals.Count >= 1;

        public ICommand CreateSystemCommand { get; }
        public ICommand SelectFromModelCommand { get; }
        public ICommand AddTerminalCommand { get; }
        public ICommand RemoveTerminalCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        /// <summary>
        /// 加载可用构件
        /// </summary>
        private void LoadAvailableElements()
        {
            // 收集所有机械设备
            var collector = new FilteredElementCollector(_doc);
            var elements = collector.OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => fi.Category != null &&
                             (fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MechanicalEquipment ||
                              fi.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctTerminal))
                .Select(fi => new ElementItem
                {
                    Id = fi.Id.IntegerValue,
                    Name = $"{fi.Name} ({fi.Id.IntegerValue})",
                    Element = fi
                })
                .ToList();

            foreach (var item in elements)
            {
                AvailableElements.Add(item);
            }
        }

        /// <summary>
        /// 从模型中拾取构件
        /// </summary>
        private void SelectFromModel()
        {
            try
            {
                var picked = _uiDoc.Selection.PickObject(
                    Autodesk.Revit.UI.Selection.ObjectType.Element,
                    "请选择送风设备或末端");

                var element = _doc.GetElement(picked.ElementId);
                var item = new ElementItem
                {
                    Id = element.Id.IntegerValue,
                    Name = $"{element.Name} ({element.Id.IntegerValue})",
                    Element = element
                };

                // 判断是设备还是末端
                if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MechanicalEquipment)
                {
                    SelectedEquipment = item;
                    StatusMessage = $"已选择设备: {element.Name}";
                }
                else
                {
                    if (SelectedTerminals.Count < 2)
                    {
                        SelectedTerminals.Add(item);
                        StatusMessage = $"已添加末端: {element.Name}";
                    }
                    else
                    {
                        StatusMessage = "末端最多选择2个";
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户取消
            }
        }

        /// <summary>
        /// 添加选中的末端
        /// </summary>
        private void AddSelectedTerminal()
        {
            if (SelectedAvailableItem != null && SelectedTerminals.Count < 2)
            {
                SelectedTerminals.Add(SelectedAvailableItem);
                AvailableElements.Remove(SelectedAvailableItem);
                StatusMessage = $"已添加末端: {SelectedAvailableItem.Name}";
            }
        }

        /// <summary>
        /// 移除选中的末端
        /// </summary>
        private void RemoveSelectedTerminal()
        {
            if (SelectedTerminal != null)
            {
                SelectedTerminals.Remove(SelectedTerminal);
                AvailableElements.Add(SelectedTerminal);
                StatusMessage = $"已移除末端: {SelectedTerminal.Name}";
            }
        }

        /// <summary>
        /// 创建风管系统
        /// </summary>
        private void CreateSystem()
        {
            IsProcessing = true;
            StatusMessage = "正在创建风管系统...";
            try
            {
                //// 验证风管类型
                //if (!ElementId.TryParse(DuctTypeIdInput, out var ductTypeId) || ductTypeId == null)
                //{
                //    StatusMessage = "风管类型ID无效";
                //    return;
                //}
                // 验证风管类型
                if (!int.TryParse(DuctTypeIdInput, out int ductTypeIdInt) || ductTypeIdInt <= 0)
                {
                    StatusMessage = "风管类型ID无效";
                    return;
                }
                ElementId ductTypeId = new ElementId(ductTypeIdInt);


                // 构建连接点列表
                var points = new System.Collections.Generic.List<ConnectorPoint>();

                // 添加设备连接点
                var equipmentPoint = GetConnectorPoint(SelectedEquipment.Element);
                if (equipmentPoint == null)
                {
                    StatusMessage = $"无法获取设备 {SelectedEquipment.Name} 的连接器";
                    return;
                }
                points.Add(equipmentPoint);

                // 添加末端连接点
                foreach (var terminal in SelectedTerminals)
                {
                    var terminalPoint = GetConnectorPoint(terminal.Element);
                    if (terminalPoint == null)
                    {
                        StatusMessage = $"无法获取末端 {terminal.Name} 的连接器";
                        return;
                    }
                    points.Add(terminalPoint);
                }

                // 创建服务并执行
                var config = new DuctSystemConfig
                {
                    DuctTypeId = ductTypeId
                };

                var service = new DuctSystemCreationService(_doc, config);
                var result = service.CreateSystem(points);

                LastResult = result;
                StatusMessage = result.IsSuccess ? "风管系统创建成功" : $"创建失败: {result.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
                LastResult = new DuctSystemResult { IsSuccess = false, Message = ex.Message };
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 获取构件的连接点
        /// </summary>
        private ConnectorPoint GetConnectorPoint(Element element)
        {
            var familyInstance = element as FamilyInstance;
            if (familyInstance?.MEPModel == null) return null;

            var connectors = familyInstance.MEPModel.ConnectorManager.Connectors;
            foreach (Connector conn in connectors)
            {
                if (conn.ConnectorType != ConnectorType.Logical)
                {
                    return new ConnectorPoint
                    {
                        ElementId = element.Id.IntegerValue,
                        Location = conn.Origin,
                        Connector = conn,
                        ElementName = element.Name
                    };
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 列表项
    /// </summary>
    public class ElementItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Element Element { get; set; }
    }
    /// <summary>
    /// 风管系统创建服务
    /// </summary>
    public class DuctSystemCreationService
    {
        private readonly Document _doc;
        private readonly DuctSystemConfig _config;
        private Level _workLevel;
        private DuctType _ductType;
        private ElementId _systemTypeId;
        private List<string> _logs;

        public DuctSystemCreationService(Document document, DuctSystemConfig config)
        {
            _doc = document;
            _config = config;
            _logs = new List<string>();

            Initialize();
        }

        public IReadOnlyList<string> Logs => _logs;

        private void Log(string message) => _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

        /// <summary>
        /// 初始化服务（获取系统类型、创建标高、获取风管类型）
        /// </summary>
        private void Initialize()
        {
            // 获取送风系统类型
            var collector = new FilteredElementCollector(_doc);
            var systemType = collector.OfClass(typeof(MEPSystemType))
                .Cast<MEPSystemType>()
                .FirstOrDefault(t => t.SystemClassification == MEPSystemClassification.SupplyAir);

            _systemTypeId = systemType?.Id ?? ElementId.InvalidElementId;
            Log($"获取送风系统类型: {systemType?.Name ?? "未找到"}");

            // 创建临时标高
            _workLevel = Level.Create(_doc, 0.0);
            Log($"创建工作标高: {_workLevel.Name}");

            // 获取风管类型
            _ductType = _doc.GetElement(_config.DuctTypeId) as DuctType;
            Log($"获取风管类型: {_ductType?.Name ?? "未找到"}");
        }

        /// <summary>
        /// 创建风管系统
        /// </summary>
        public DuctSystemResult CreateSystem(List<ConnectorPoint> connectionPoints)
        {
            var result = new DuctSystemResult();

            try
            {
                Log($"开始创建风管系统，共 {connectionPoints.Count} 个连接点");

                // 验证连接点数量
                if (connectionPoints.Count < 2)
                {
                    result.Message = "至少需要2个连接点";
                    return result;
                }

                // 获取设备连接器和末端连接器
                var equipmentConnector = connectionPoints[0];
                var terminalConnectors = connectionPoints.Skip(1).ToList();

                // 创建机械系统
                var mechanicalSystem = CreateMechanicalSystem(equipmentConnector, terminalConnectors);
                result.MechanicalSystem = mechanicalSystem;

                // 计算系统边界
                var bounds = CalculateBounds(connectionPoints);
                Log($"系统边界: X[{bounds.MinX:F2}, {bounds.MaxX:F2}], Y[{bounds.MinY:F2}, {bounds.MaxY:F2}], ZMax={bounds.MaxZ:F2}");

                // 创建设备连接立管
                var baseConnectors = CreateEquipmentRiser(equipmentConnector, bounds.MaxZ);

                // 创建末端立管
                var terminalRisers = CreateTerminalRisers(terminalConnectors, bounds.MaxZ);
                baseConnectors.AddRange(terminalRisers);

                // 连接主干管
                var connected = ConnectTrunkSystem(baseConnectors, bounds);

                result.IsSuccess = connected;
                result.ConnectedComponentsCount = baseConnectors.Count;
                result.Message = connected ? "风管系统创建成功" : "部分连接失败";

                Log($"系统创建完成: {(connected ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                result.Message = $"创建失败: {ex.Message}";
                Log($"错误: {ex.Message}");
            }

            result.LogMessages = new ObservableCollection<string>(_logs);
            return result;
        }

        /// <summary>
        /// 创建机械系统
        /// </summary>
        private MechanicalSystem CreateMechanicalSystem(ConnectorPoint equipment, List<ConnectorPoint> terminals)
        {
            var connectorSet = new ConnectorSet();
            foreach (var terminal in terminals)
            {
                connectorSet.Insert(terminal.Connector);
            }

            return _doc.Create.NewMechanicalSystem(
                equipment.Connector,
                connectorSet,
                DuctSystemType.SupplyAir);
        }

        /// <summary>
        /// 计算系统边界
        /// </summary>
        private (double MinX, double MinY, double MaxX, double MaxY, double MaxZ) CalculateBounds(List<ConnectorPoint> points)
        {
            var xs = points.Select(p => p.Location.X);
            var ys = points.Select(p => p.Location.Y);
            var zs = points.Select(p => p.Location.Z);

            return (xs.Min(), ys.Min(), xs.Max(), ys.Max(), zs.Max());
        }

        /// <summary>
        /// 创建设备连接立管
        /// </summary>
        private List<Connector> CreateEquipmentRiser(ConnectorPoint equipment, double maxZ)
        {
            var connectors = new List<Connector>();
            var startPoint = equipment.Location;
            var direction = GetConnectorDirection(equipment.Connector);

            // 计算连接点
            var elbowPoint = CalculateElbowPoint(startPoint, direction, _config.MinFittingLength);
            var verticalPoint = new XYZ(elbowPoint.X, elbowPoint.Y, maxZ + _config.VerticalTrunkOffset - _config.MinFittingLength);

            // 创建水平管段
            var duct1 = CreateDuct(equipment.Connector, elbowPoint);
            // 创建垂直管段
            var duct2 = CreateDuct(verticalPoint, elbowPoint);

            // 获取连接器并创建弯头
            var conn1 = GetConnectorAtPoint(duct1, elbowPoint);
            var conn2 = GetConnectorAtPoint(duct2, elbowPoint);
            conn1.ConnectTo(conn2);
            _doc.Create.NewElbowFitting(conn1, conn2);

            var topConnector = GetConnectorAtPoint(duct2, verticalPoint);
            connectors.Add(topConnector);

            Log($"创建设备立管: 位置({startPoint.X:F2}, {startPoint.Y:F2})");
            return connectors;
        }

        /// <summary>
        /// 创建末端立管
        /// </summary>
        private List<Connector> CreateTerminalRisers(List<ConnectorPoint> terminals, double maxZ)
        {
            var connectors = new List<Connector>();

            foreach (var terminal in terminals)
            {
                var startPoint = terminal.Location;
                var verticalPoint = new XYZ(startPoint.X, startPoint.Y, maxZ + _config.VerticalTrunkOffset - _config.MinFittingLength);

                var duct = CreateDuct(terminal.Connector, verticalPoint);
                var topConnector = GetConnectorAtPoint(duct, verticalPoint);
                connectors.Add(topConnector);

                Log($"创建设末端立管: 位置({startPoint.X:F2}, {startPoint.Y:F2})");
            }

            return connectors;
        }

        /// <summary>
        /// 获取连接器方向
        /// </summary>
        private XYZ GetConnectorDirection(Connector connector)
        {
            return connector.CoordinateSystem.BasisZ;
        }

        /// <summary>
        /// 计算弯头点
        /// </summary>
        private XYZ CalculateElbowPoint(XYZ startPoint, XYZ direction, double offset)
        {
            if (direction.IsAlmostEqualTo(new XYZ(-1, 0, 0)))
                return new XYZ(startPoint.X - offset, startPoint.Y, startPoint.Z);
            if (direction.IsAlmostEqualTo(new XYZ(1, 0, 0)))
                return new XYZ(startPoint.X + offset, startPoint.Y, startPoint.Z);
            if (direction.IsAlmostEqualTo(new XYZ(0, -1, 0)))
                return new XYZ(startPoint.X, startPoint.Y - offset, startPoint.Z);

            return new XYZ(startPoint.X, startPoint.Y + offset, startPoint.Z);
        }

        /// <summary>
        /// 创建风管
        /// </summary>
        private Duct CreateDuct(Connector connector, XYZ endPoint)
        {
            return Duct.Create(_doc, _systemTypeId, _ductType.Id, _workLevel.Id, connector.Origin, endPoint);
        }

        private Duct CreateDuct(XYZ point1, XYZ point2)
        {
            return Duct.Create(_doc, _systemTypeId, _ductType.Id, _workLevel.Id, point1, point2);
        }

        /// <summary>
        /// 获取风管端点连接器
        /// </summary>
        private Connector GetConnectorAtPoint(Duct duct, XYZ point)
        {
            var connectors = duct.ConnectorManager.Connectors;
            foreach (Connector conn in connectors)
            {
                if (conn.Origin.IsAlmostEqualTo(point))
                    return conn;
            }
            return null;
        }

        /// <summary>
        /// 连接主干管系统
        /// </summary>
        private bool ConnectTrunkSystem(List<Connector> baseConnectors, (double MinX, double MinY, double MaxX, double MaxY, double MaxZ) bounds)
        {
            if (baseConnectors.Count < 2) return false;

            // 按X坐标排序
            var sortedByX = baseConnectors.OrderBy(c => c.Origin.X).ToList();
            var midY = (bounds.MinY + bounds.MaxY) / 2;

            // 尝试沿X轴连接
            if (TryConnectOnXAxis(sortedByX, midY))
                return true;

            // 按Y坐标排序
            var sortedByY = baseConnectors.OrderBy(c => c.Origin.Y).ToList();
            var midX = (bounds.MinX + bounds.MaxX) / 2;

            // 尝试沿Y轴连接
            if (TryConnectOnYAxis(sortedByY, midX))
                return true;

            Log("主干管连接失败");
            return false;
        }

        /// <summary>
        /// 尝试沿X轴连接
        /// </summary>
        private bool TryConnectOnXAxis(List<Connector> connectors, double trunkY)
        {
            try
            {
                var baseZ = connectors[0].Origin.Z + _config.MinFittingLength;
                var trunkStart = new XYZ(connectors[0].Origin.X + _config.MinFittingLength, trunkY, baseZ);
                var trunkEnd = new XYZ(connectors[2].Origin.X - _config.MinFittingLength, trunkY, baseZ);

                var trunkDuct = CreateDuct(trunkStart, trunkEnd);
                var leftConn = GetConnectorAtPoint(trunkDuct, trunkStart);
                var rightConn = GetConnectorAtPoint(trunkDuct, trunkEnd);

                // 连接左右两端
                ConnectWithElbow(connectors[0], leftConn);
                ConnectWithElbow(connectors[2], rightConn);

                // 连接中间点
                ConnectWithTee(connectors[1], trunkDuct, trunkY, baseZ);

                Log($"X轴方向连接成功，主干管Y={trunkY:F2}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试沿Y轴连接
        /// </summary>
        private bool TryConnectOnYAxis(List<Connector> connectors, double trunkX)
        {
            try
            {
                var baseZ = connectors[0].Origin.Z + _config.MinFittingLength;
                var trunkStart = new XYZ(trunkX, connectors[0].Origin.Y + _config.MinFittingLength, baseZ);
                var trunkEnd = new XYZ(trunkX, connectors[2].Origin.Y - _config.MinFittingLength, baseZ);

                var trunkDuct = CreateDuct(trunkStart, trunkEnd);
                var bottomConn = GetConnectorAtPoint(trunkDuct, trunkStart);
                var topConn = GetConnectorAtPoint(trunkDuct, trunkEnd);

                ConnectWithElbow(connectors[0], bottomConn);
                ConnectWithElbow(connectors[2], topConn);
                ConnectWithTee(connectors[1], trunkDuct, trunkX, baseZ, isXAxis: false);

                Log($"Y轴方向连接成功，主干管X={trunkX:F2}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 使用弯头连接
        /// </summary>
        private void ConnectWithElbow(Connector baseConn, Connector trunkConn)
        {
            baseConn.ConnectTo(trunkConn);
            _doc.Create.NewElbowFitting(baseConn, trunkConn);
        }

        /// <summary>
        /// 使用三通连接
        /// </summary>
        private void ConnectWithTee(Connector baseConn, Duct trunkDuct, double trunkPos, double baseZ, bool isXAxis = true)
        {
            var connectors = trunkDuct.ConnectorManager.Connectors;

            Connector conn1 = null, conn2 = null;
            foreach (Connector conn in connectors)
            {
                var pos = isXAxis ? conn.Origin.X : conn.Origin.Y;
                if (Math.Abs(pos - trunkPos) < 0.01)
                {
                    if (conn1 == null) conn1 = conn;
                    else conn2 = conn;
                }
            }

            if (conn1 != null && conn2 != null)
            {
                baseConn.ConnectTo(conn1);
                baseConn.ConnectTo(conn2);
                conn1.ConnectTo(conn2);
                _doc.Create.NewTeeFitting(conn1, conn2, baseConn);
            }
        }
    }
    /// <summary>
    /// 连接点信息
    /// </summary>
    public class ConnectorPoint
    {
        public int ElementId { get; set; }
        public XYZ Location { get; set; }
        public Connector Connector { get; set; }
        public string ElementName { get; set; }
    }

    /// <summary>
    /// 风管系统配置
    /// </summary>
    public class DuctSystemConfig
    {
        public double MinFittingLength { get; set; } = 1.0;
        public double MinDuctLength { get; set; } = 0.5;
        public double VerticalTrunkOffset { get; set; } = 15.0;
        public double HorizontalOptionalOffset { get; set; } = 5.0;
        public ElementId DuctTypeId { get; set; }
    }

    /// <summary>
    /// 创建结果
    /// </summary>
    public class DuctSystemResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public MechanicalSystem MechanicalSystem { get; set; }
        public ObservableCollection<string> LogMessages { get; set; } = new ObservableCollection<string>();
        public int ConnectedComponentsCount { get; set; }
    }

}
