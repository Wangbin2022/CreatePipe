using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static CreatePipe.RevitOperationLogger;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CircuitOperationView.xaml 的交互逻辑
    /// </summary>
    public partial class CircuitOperationView : Window
    {
        public CircuitOperationView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 电路操作主ViewModel - 管理操作选择和执行
    /// </summary>
    public class CircuitOperationViewModel : ObserverableObject
    {
        private readonly CircuitOperationData _operationData;
        private bool _canCreateCircuit;
        private bool _hasCircuit;
        private bool _hasPanel;
        private bool _isProcessing;

        public bool CanCreateCircuit
        {
            get => _canCreateCircuit;
            set { _canCreateCircuit = value; OnPropertyChanged(); }
        }

        public bool HasCircuit
        {
            get => _hasCircuit;
            set { _hasCircuit = value; OnPropertyChanged(); }
        }

        public bool HasPanel
        {
            get => _hasPanel;
            set { _hasPanel = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        // 命令
        public ICommand CreateCircuitCommand { get; }
        public ICommand EditCircuitCommand { get; }
        public ICommand SelectPanelCommand { get; }
        public ICommand DisconnectPanelCommand { get; }
        public ICommand CancelCommand { get; }

        // 事件
        public event Action<OperationType> OperationSelected;
        public event Action<ElectricalSystemItem> CircuitSelectedForEdit;

        public CircuitOperationViewModel(CircuitOperationData operationData)
        {
            _operationData = operationData;
            CanCreateCircuit = _operationData.CanCreateCircuit;
            HasCircuit = _operationData.HasCircuit;
            HasPanel = _operationData.HasPanel;

            CreateCircuitCommand = new BaseBindingCommand(_ => OnCreateCircuit(),
                _ => CanCreateCircuit && !IsProcessing);
            EditCircuitCommand = new BaseBindingCommand(_ => OnEditCircuit(),
                _ => HasCircuit && !IsProcessing);
            SelectPanelCommand = new BaseBindingCommand(_ => OnSelectPanel(),
                _ => HasCircuit && !IsProcessing);
            DisconnectPanelCommand = new BaseBindingCommand(_ => OnDisconnectPanel(),
                _ => HasCircuit && HasPanel && !IsProcessing);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnCreateCircuit()
        {
            OperationSelected?.Invoke(OperationType.CreateCircuit);
            CloseWindow?.Invoke();
        }

        private void OnEditCircuit()
        {
            // 如果有多个电路，先显示选择对话框
            if (_operationData.ElectricalSystemCount > 1)
            {
                ShowCircuitSelectionForEdit();
            }
            else
            {
                OperationSelected?.Invoke(OperationType.EditCircuit);
                CloseWindow?.Invoke();
            }
        }

        private void ShowCircuitSelectionForEdit()
        {
            var selectVm = new SelectCircuitViewModel(_operationData);
            var window = new CircuitSelectWindow { DataContext = selectVm };
            selectVm.CloseWindow = () => window.Close();
            selectVm.CircuitSelected += circuit =>
            {
                _operationData.SelectCircuit(circuit.Id.IntegerValue);
                OperationSelected?.Invoke(OperationType.EditCircuit);
                CloseWindow?.Invoke();
            };
            window.ShowDialog();
        }

        private void OnSelectPanel()
        {
            OperationSelected?.Invoke(OperationType.SelectPanel);
            CloseWindow?.Invoke();
        }

        private void OnDisconnectPanel()
        {
            OperationSelected?.Invoke(OperationType.DisconnectPanel);
            CloseWindow?.Invoke();
        }

        public Action CloseWindow { get; set; }
    }

    /// <summary>
    /// 电路操作类型枚举
    /// </summary>
    public enum OperationType
    {
        CreateCircuit,   // 创建电路
        EditCircuit,     // 编辑电路
        SelectPanel,     // 选择配电盘
        DisconnectPanel  // 断开配电盘
    }

    /// <summary>
    /// 编辑电路选项枚举
    /// </summary>
    public enum EditOptionType
    {
        Add,        // 添加元素到电路
        Remove,     // 从电路移除元素
        SelectPanel // 选择配电盘
    }

    /// <summary>
    /// 电路项ViewModel - 用于列表显示
    /// </summary>
    public class ElectricalSystemItem
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
        public ElectricalSystem ElectricalSystem { get; set; }
    }

    /// <summary>
    /// 电路操作数据类 - 收集设备信息并执行电路操作
    /// 使用C# 7.3语法：表达式体成员、模式匹配、LINQ、nameof
    /// </summary>
    public class CircuitOperationData
    {
        #region 私有字段
        private readonly UIDocument _revitDoc;
        private readonly Selection _selection;
        private OperationType _operation;
        private EditOptionType _editOption;
        private bool _canCreateCircuit;
        private bool _hasCircuit;
        private bool _hasPanel;
        private readonly ElectricalSystemSet _electricalSystemSet;
        private readonly List<ElectricalSystemItem> _electricalSystemItems;
        private ElectricalSystem _selectedElectricalSystem;
        #endregion

        #region 公开属性 - 使用表达式体
        public OperationType Operation { get => _operation; set => _operation = value; }
        public EditOptionType EditOption { get => _editOption; set => _editOption = value; }
        public bool CanCreateCircuit => _canCreateCircuit;
        public bool HasCircuit => _hasCircuit;
        public bool HasPanel => _hasPanel;
        public int ElectricalSystemCount => _electricalSystemSet?.Size ?? 0;

        public ReadOnlyCollection<ElectricalSystemItem> ElectricalSystemItems
        {
            get
            {
                if (_electricalSystemSet == null) return null;

                _electricalSystemItems.Clear();
                foreach (ElectricalSystem es in _electricalSystemSet)
                {
 
                    //_electricalSystemItems.Add(new ElectricalSystemItem(es));
                }
                return new ReadOnlyCollection<ElectricalSystemItem>(_electricalSystemItems);
            }
        }
        #endregion

        #region 构造函数
        public CircuitOperationData(ExternalCommandData commandData)
        {
            _revitDoc = commandData.Application.ActiveUIDocument;
            _selection = _revitDoc.Selection;

            _electricalSystemSet = new ElectricalSystemSet();
            _electricalSystemItems = new List<ElectricalSystemItem>();

            CollectConnectorInfo();
            CollectCircuitInfo();
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 验证族实例是否有未使用的电气连接器
        /// </summary>
        private static bool HasUsableConnector(FamilyInstance fi)
        {
            try
            {
                var mepModel = fi?.MEPModel;
                if (mepModel == null) return false;

                var unusedConnectors = mepModel.ConnectorManager?.UnusedConnectors;
                if (unusedConnectors == null || unusedConnectors.IsEmpty) return false;

                // 使用LINQ检查是否存在电气域连接器
                return unusedConnectors.Cast<Connector>().Any(c => c.Domain == Domain.DomainElectrical);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取元素所属的电路集合
        /// </summary>
        private static ElectricalSystemSet GetElementCircuits(Element element)
        {
            // 使用模式匹配处理不同类型的元素
            switch (element)
            {
                case FamilyInstance fi when fi.MEPModel != null:
                    return fi.MEPModel.ElectricalSystems;
                case ElectricalSystem es when es.SystemType == ElectricalSystemType.PowerCircuit:
                    var set = new ElectricalSystemSet();
                    set.Insert(es);
                    return set;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 检查电路集合是否包含指定电路
        /// </summary>
        private static bool ContainsCircuit(ElectricalSystemSet set, ElectricalSystem circuit) =>
            set?.Cast<ElectricalSystem>().Contains(circuit) == true;

        /// <summary>
        /// 元素是否属于指定电路
        /// </summary>
        private static bool IsElementInCircuit(MEPModel mepModel, ElectricalSystem circuit) =>
            mepModel?.ElectricalSystems?.Cast<ElectricalSystem>().Contains(circuit) == true;
        #endregion

        #region 信息收集方法
        /// <summary>
        /// 收集连接器信息 - 验证所有选中设备是否有可用连接器
        /// </summary>
        private void CollectConnectorInfo()
        {
            _canCreateCircuit = true;
            bool allLightingDevices = true;

            foreach (ElementId id in _selection.GetElementIds())
            {
                var element = _revitDoc.Document.GetElement(id);

                // 使用模式匹配验证元素类型
                if (!(element is FamilyInstance fi))
                {
                    _canCreateCircuit = false;
                    return;
                }

                // 检查是否全是照明设备
                if (fi.Category?.Name != "Lighting Devices")
                {
                    allLightingDevices = false;
                }

                // 验证是否有可用连接器
                if (!HasUsableConnector(fi))
                {
                    _canCreateCircuit = false;
                    return;
                }
            }

            // 全是照明设备时不能创建电路（需要配电盘）
            if (allLightingDevices) _canCreateCircuit = false;
        }

        /// <summary>
        /// 收集电路信息 - 找出所有选中元素共用的电路
        /// </summary>
        private void CollectCircuitInfo()
        {
            bool isInitialized = false;

            foreach (ElementId id in _selection.GetElementIds())
            {
                var element = _revitDoc.Document.GetElement(id);
                var circuits = GetElementCircuits(element);

                // 验证是否获取到有效的电路集合
                if (!ValidateCircuits(circuits)) return;

                // 过滤出电力电路
                var powerCircuits = FilterPowerCircuits(circuits);
                if (powerCircuits == null || powerCircuits.IsEmpty) return;

                // 合并或取交集
                if (!isInitialized)
                {
                    MergeToMasterSet(powerCircuits);
                    isInitialized = true;
                }
                else
                {
                    IntersectWithMasterSet(powerCircuits);
                    if (_electricalSystemSet.IsEmpty) return;
                }
            }

            UpdateCircuitStatus();
        }

        /// <summary>
        /// 验证电路集合是否有效
        /// </summary>
        private bool ValidateCircuits(ElectricalSystemSet circuits)
        {
            if (circuits == null)
            {
                _hasCircuit = false;
                _hasPanel = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 过滤出电力电路
        /// </summary>
        private ElectricalSystemSet FilterPowerCircuits(ElectricalSystemSet circuits)
        {
            var result = new ElectricalSystemSet();
            foreach (ElectricalSystem es in circuits)
            {
                if (es.SystemType == ElectricalSystemType.PowerCircuit)
                {
                    result.Insert(es);
                }
            }
            return result.IsEmpty ? null : result;
        }

        /// <summary>
        /// 合并电路到主集合
        /// </summary>
        private void MergeToMasterSet(ElectricalSystemSet circuits)
        {
            _electricalSystemSet.Clear();
            foreach (ElectricalSystem es in circuits)
            {
                _electricalSystemSet.Insert(es);
            }
        }

        /// <summary>
        /// 取主集合与当前电路的交集
        /// </summary>
        private void IntersectWithMasterSet(ElectricalSystemSet circuits)
        {
            var toRemove = new List<ElectricalSystem>();
            foreach (ElectricalSystem es in _electricalSystemSet)
            {
                if (!ContainsCircuit(circuits, es))
                {
                    toRemove.Add(es);
                }
            }
            foreach (var es in toRemove)
            {
                _electricalSystemSet.Erase(es);
            }
        }

        /// <summary>
        /// 更新电路状态（是否有电路、是否有配电盘）
        /// </summary>
        private void UpdateCircuitStatus()
        {
            if (_electricalSystemSet.IsEmpty)
            {
                _hasCircuit = false;
                _hasPanel = false;
                return;
            }

            _hasCircuit = true;

            // 获取选中的电路（仅一个时）
            if (_electricalSystemSet.Size == 1)
            {
                _selectedElectricalSystem = _electricalSystemSet.Cast<ElectricalSystem>().First();
            }

            // 检查是否有配电盘
            _hasPanel = _electricalSystemSet.Cast<ElectricalSystem>().Any(es => !string.IsNullOrEmpty(es.PanelName));
        }
        #endregion

        #region 操作方法
        /// <summary>
        /// 调度操作 - 根据操作类型执行
        /// </summary>
        public void Operate()
        {
            using (var transaction = new Transaction(_revitDoc.Document, _operation.ToString()))
            {
                transaction.Start();

                switch (_operation)
                {
                    case OperationType.CreateCircuit:
                        CreatePowerCircuit();
                        break;
                    case OperationType.EditCircuit:
                        EditCircuit();
                        break;
                    case OperationType.SelectPanel:
                        SelectPanel();
                        break;
                    case OperationType.DisconnectPanel:
                        DisconnectPanel();
                        break;
                }

                transaction.Commit();
            }

            // 非创建操作，选中修改后的电路
            if (_operation != OperationType.CreateCircuit && _selectedElectricalSystem != null)
            {
                SelectCurrentCircuit();
            }
        }

        /// <summary>
        /// 创建电力电路
        /// </summary>
        public void CreatePowerCircuit()
        {
            var selectedIds = _selection.GetElementIds().ToList();

            try
            {
                var circuit = ElectricalSystem.Create(_revitDoc.Document, selectedIds, ElectricalSystemType.PowerCircuit);

                // 选中新创建的电路
                _selection.SetElementIds(new List<ElementId> { circuit.Id });
                _selectedElectricalSystem = circuit;
            }
            catch
            {
                ShowErrorMessage("FailedToCreateCircuit");
            }
        }

        /// <summary>
        /// 编辑电路 - 根据编辑选项分发
        /// </summary>
        public void EditCircuit()
        {
            switch (_editOption)
            {
                case EditOptionType.Add:
                    AddElementToCircuit();
                    break;
                case EditOptionType.Remove:
                    RemoveElementFromCircuit();
                    break;
                case EditOptionType.SelectPanel:
                    SelectPanel();
                    break;
            }
        }

        /// <summary>
        /// 添加元素到电路
        /// </summary>
        public void AddElementToCircuit()
        {
            // 清空选择并让用户选择元素
            _selection.SetElementIds(new List<ElementId>());
            try
            {
                _selection.PickObject(ObjectType.Element);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }

            // 获取选中的元素
            var selectedId = _selection.GetElementIds().FirstOrDefault();
            if (selectedId == null) return;

            var element = _revitDoc.Document.GetElement(selectedId);

            // 验证元素类型和连接器
            if (!(element is FamilyInstance fi) || fi.MEPModel == null)
            {
                ShowErrorMessage("SelectElectricalComponent");
                return;
            }

            if (!HasUsableConnector(fi))
            {
                ShowErrorMessage("NoUsableConnector");
                return;
            }

            if (IsElementInCircuit(fi.MEPModel, _selectedElectricalSystem))
            {
                ShowErrorMessage("ElementInCircuit");
                return;
            }

            try
            {
                var elementsToAdd = new ElementSet();
                elementsToAdd.Insert(fi);

                if (!_selectedElectricalSystem.AddToCircuit(elementsToAdd))
                {
                    ShowErrorMessage("FailedToAddElement");
                }
            }
            catch
            {
                ShowErrorMessage("FailedToAddElement");
            }
        }

        /// <summary>
        /// 从电路移除元素
        /// </summary>
        public void RemoveElementFromCircuit()
        {
            // 清空选择并让用户选择元素
            _selection.SetElementIds(new List<ElementId>());
            try
            {
                _selection.PickObject(ObjectType.Element);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }

            var selectedId = _selection.GetElementIds().FirstOrDefault();
            if (selectedId == null) return;

            var element = _revitDoc.Document.GetElement(selectedId);

            if (!(element is FamilyInstance fi) || fi.MEPModel == null)
            {
                ShowErrorMessage("SelectElectricalComponent");
                return;
            }

            if (!IsElementInCircuit(fi.MEPModel, _selectedElectricalSystem))
            {
                ShowErrorMessage("ElementNotInCircuit");
                return;
            }

            try
            {
                var elementsToRemove = new ElementSet();
                elementsToRemove.Insert(fi);
                _selectedElectricalSystem.RemoveFromCircuit(elementsToRemove);
            }
            catch
            {
                ShowErrorMessage("FailedToRemoveElement");
            }
        }

        /// <summary>
        /// 为电路选择配电盘
        /// </summary>
        public void SelectPanel()
        {
            _selection.SetElementIds(new List<ElementId>());
            try
            {
                _selection.PickObject(ObjectType.Element);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }

            var selectedId = _selection.GetElementIds().FirstOrDefault();
            if (selectedId == null) return;

            var element = _revitDoc.Document.GetElement(selectedId);

            if (element is FamilyInstance panel)
            {
                try
                {
                    _selectedElectricalSystem.SelectPanel(panel);
                }
                catch
                {
                    ShowErrorMessage("FailedToSelectPanel");
                }
            }
        }

        /// <summary>
        /// 断开电路与配电盘的连接
        /// </summary>
        public void DisconnectPanel()
        {
            try
            {
                _selectedElectricalSystem.DisconnectPanel();
            }
            catch
            {
                ShowErrorMessage("FailedToDisconnectPanel");
            }
        }

        /// <summary>
        /// 根据索引选择电路（用于多电路选择对话框）
        /// </summary>
        public void SelectCircuit(int index)
        {
            if (index < 0 || index >= _electricalSystemItems.Count) return;

            var esi = _electricalSystemItems[index];
            _selectedElectricalSystem = _revitDoc.Document.GetElement(esi.Id) as ElectricalSystem;
            SelectCurrentCircuit();
        }

        /// <summary>
        /// 选中当前电路
        /// </summary>
        public void SelectCurrentCircuit()
        {
            if (_selectedElectricalSystem != null)
            {
                _selection.SetElementIds(new List<ElementId> { _selectedElectricalSystem.Id });
            }
        }

        /// <summary>
        /// 在视图中显示电路（居中）
        /// </summary>
        public void ShowCircuit(int index)
        {
            if (index < 0 || index >= _electricalSystemItems.Count) return;

            var esi = _electricalSystemItems[index];
            _revitDoc.ShowElements(esi.Id);
        }
        #endregion

        #region 错误提示
        private static void ShowErrorMessage(string messageKey)
        {
            TaskDialog.Show("操作失败", GetResourceString(messageKey), TaskDialogCommonButtons.Ok);
        }

        private static string GetResourceString(string key)
        {
            // 简化资源获取，实际使用中应使用ResourceManager
            switch (key)
            {
                case "FailedToCreateCircuit":
                    return "创建电路失败，请检查选中的设备。";
                case "SelectElectricalComponent":
                    return "请选择一个电气设备。";
                case "NoUsableConnector":
                    return "选中的设备没有可用的电气连接器。";
                case "ElementInCircuit":
                    return "该设备已在当前电路中。";
                case "FailedToAddElement":
                    return "添加设备到电路失败。";
                case "ElementNotInCircuit":
                    return "该设备不在当前电路中。";
                case "FailedToRemoveElement":
                    return "从电路移除设备失败。";
                case "FailedToSelectPanel":
                    return "选择配电盘失败。";
                case "FailedToDisconnectPanel":
                    return "断开配电盘失败。";
                default:
                    return "操作失败";
            }
        }
        #endregion
    }
}
