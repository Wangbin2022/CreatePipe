using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// GenericStructuralConnectionView.xaml 的交互逻辑
    /// </summary>
    public partial class GenericStructuralConnectionView : Window
    {
        public GenericStructuralConnectionView(UIDocument uiDoc)
        {
            InitializeComponent();
            DataContext = new GenericStructuralConnectionViewModel(uiDoc);
        }
    }

    /// <summary>
    /// 结构连接操作枚举 - 定义所有支持的命令类型
    /// </summary>
    public enum CommandOption
    {
        CreateGeneric,      // 创建通用连接
        DeleteGeneric,      // 删除通用连接
        ReadGeneric,        // 读取通用连接信息
        UpdateGeneric,      // 更新通用连接
        CreateDetailed,     // 创建详细连接
        ChangeDetailed,     // 更改详细连接类型
        CopyDetailed,       // 复制详细连接
        MatchPropDetailed,  // 匹配详细连接属性
        ResetDetailed       // 重置为通用连接
    }

    /// <summary>
    /// 结构连接视图模型 - 实现INotifyPropertyChanged以支持WPF数据绑定
    /// </summary>
    public class GenericStructuralConnectionViewModel : ObserverableObject
    {
        private readonly UIDocument _uiDoc;
        private readonly StructuralConnectionModel _model;
        private CommandOption _selectedCommand;

        // 用于存储操作结果消息
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // 命令属性 - 使用lambda表达式简化
        public ICommand ExecuteCommand => new BaseBindingCommand(_ => ExecuteSelectedOperation());
        public ICommand CancelCommand => new BaseBindingCommand(_ => Cancel());

        // 当前选中的命令
        public CommandOption SelectedCommand
        {
            get => _selectedCommand;
            set => SetProperty(ref _selectedCommand, value);
        }

        public GenericStructuralConnectionViewModel(UIDocument uiDoc)
        {
            _uiDoc = uiDoc ?? throw new ArgumentNullException(nameof(uiDoc));
            _model = new StructuralConnectionModel(uiDoc);
        }

        #region 核心操作方法

        /// <summary>
        /// 执行选中的操作 - 使用传统方式保持兼容
        /// </summary>
        private void ExecuteSelectedOperation()
        {
            Result ret = Result.Succeeded;
            string message = string.Empty;

            try
            {
                if (SelectedCommand == CommandOption.CreateGeneric)
                {
                    ret = CreateGenericConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.DeleteGeneric)
                {
                    ret = DeleteGenericConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.ReadGeneric)
                {
                    ret = ReadGenericConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.UpdateGeneric)
                {
                    ret = UpdateGenericConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.CreateDetailed)
                {
                    ret = CreateDetailedConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.ChangeDetailed)
                {
                    ret = ChangeDetailedConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.CopyDetailed)
                {
                    ret = CopyDetailedConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.MatchPropDetailed)
                {
                    ret = MatchPropertiesDetailedConnection(ref message);
                }
                else if (SelectedCommand == CommandOption.ResetDetailed)
                {
                    ret = ResetDetailedConnection(ref message);
                }
                else
                {
                    ret = Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"操作失败：{ex.Message}";
                ret = Result.Failed;
            }

            StatusMessage = ret == Result.Succeeded
                ? "操作成功完成！"
                : $"操作失败：{message}";

            if (ret != Result.Succeeded)
            {
                MessageBox.Show(StatusMessage, "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel() => System.Windows.Application.Current.Windows[0]?.Close();

        #endregion

        #region 通用连接操作

        /// <summary>
        /// 创建通用结构连接
        /// </summary>
        private Result CreateGenericConnection(ref string message)
        {
            var ids = _model.PickConnectionElements();
            if (ids.Count == 0)
            {
                message = "未选择任何构件！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "创建通用结构连接"))
            {
                tran.Start();
                StructuralConnectionHandler.Create(_model.Document, ids);
                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 删除通用结构连接
        /// </summary>
        private Result DeleteGenericConnection(ref string message)
        {
            var conn = _model.PickConnection();
            if (conn == null)
            {
                message = "未选择任何连接！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "删除通用结构连接"))
            {
                tran.Start();
                _model.Document.Delete(conn.Id);
                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 读取通用结构连接信息
        /// </summary>
        private Result ReadGenericConnection(ref string message)
        {
            var conn = _model.PickConnection();
            if (conn == null)
            {
                message = "未选择任何连接！";
                return Result.Failed;
            }

            // 使用内插字符串和字符串构建器
            var sb = new StringBuilder();
            sb.AppendLine($"连接 ID：{conn.Id}");

            var connType = _model.Document.GetElement(conn.GetTypeId()) as StructuralConnectionHandlerType;
            if (connType != null)
            {
                sb.AppendLine($"类型：{connType.Name}");
            }

            var connectedIds = conn.GetConnectedElementIds();
            sb.Append($"连接的构件 ID：{string.Join(", ", connectedIds)}");

            MessageBox.Show(sb.ToString(), "连接信息",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return Result.Succeeded;
        }

        /// <summary>
        /// 更新通用结构连接（添加更多构件）
        /// </summary>
        private Result UpdateGenericConnection(ref string message)
        {
            var conn = _model.PickConnection();
            if (conn == null)
            {
                message = "未选择任何连接！";
                return Result.Failed;
            }

            var newIds = _model.PickConnectionElements();
            if (newIds.Count == 0)
            {
                message = "未选择要添加的构件！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "更新通用结构连接"))
            {
                tran.Start();
                conn.AddElementIds(newIds);
                return CommitTransaction(tran, ref message);
            }
        }

        #endregion

        #region 详细连接操作

        /// <summary>
        /// 创建详细结构连接（使用预定义类型）
        /// </summary>
        private Result CreateDetailedConnection(ref string message)
        {
            var ids = _model.PickConnectionElements();
            if (ids.Count == 0)
            {
                message = "未选择任何构件！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "创建详细结构连接"))
            {
                tran.Start();
                // 从配置文件加载连接类型：US夹持角钢
                var connType = StructuralConnectionHandlerType.Create(
                    _model.Document,
                    "usclipangle",
                    new Guid("A42C5CE5-91C5-47E4-B445-D053E5BD66DB"),
                    "usclipangle");

                if (connType != null)
                {
                    StructuralConnectionHandler.Create(_model.Document, ids, connType.Id);
                }
                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 更改详细连接类型
        /// </summary>
        private Result ChangeDetailedConnection(ref string message)
        {
            var conn = _model.PickConnection();
            if (conn == null)
            {
                message = "未选择任何连接！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "更改详细连接类型"))
            {
                tran.Start();
                // 更改为剪切板类型
                var newType = StructuralConnectionHandlerType.Create(
                    _model.Document,
                    "shearplatenew",
                    new Guid("B490A703-5B6D-4B7A-8471-752133527925"),
                    "shearplatenew");

                if (newType != null)
                {
                    conn.ChangeTypeId(newType.Id);
                }
                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 复制详细连接（偏移20个单位）
        /// </summary>
        private Result CopyDetailedConnection(ref string message)
        {
            var ids = _model.GetSelectedElementIds();
            if (ids.Count == 0)
            {
                message = "请先选择要复制的元素！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "复制元素"))
            {
                tran.Start();
                var transform = Autodesk.Revit.DB.Transform.CreateTranslation(new XYZ(0, 20, 0));
                ElementTransformUtils.CopyElements(
                    _model.Document, ids, _model.Document, transform, null);
                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 匹配详细连接属性（复制属性）
        /// </summary>
        private Result MatchPropertiesDetailedConnection(ref string message)
        {
            MessageBox.Show("请先选择源连接，然后选择目标连接", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            var srcConn = _model.PickConnection();
            var destConn = _model.PickConnection();

            if (srcConn == null || destConn == null)
            {
                message = "请确保选择了两个有效的连接！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "匹配连接属性"))
            {
                tran.Start();

                var schema = GetConnectionSchema(srcConn);
                if (schema != null)
                {
                    var entity = srcConn.GetEntity(schema);
                    destConn.SetEntity(entity);
                }

                return CommitTransaction(tran, ref message);
            }
        }

        /// <summary>
        /// 重置详细连接为通用类型
        /// </summary>
        private Result ResetDetailedConnection(ref string message)
        {
            var conn = _model.PickConnection();
            if (conn == null)
            {
                message = "未选择任何连接！";
                return Result.Failed;
            }

            using (var tran = new Transaction(_model.Document, "重置为通用连接"))
            {
                tran.Start();

                var genericId = StructuralConnectionHandlerType.GetDefaultConnectionHandlerType(_model.Document);
                if (genericId == ElementId.InvalidElementId)
                {
                    genericId = StructuralConnectionHandlerType.CreateDefaultStructuralConnectionHandlerType(_model.Document);
                }
                conn.ChangeTypeId(genericId);

                return CommitTransaction(tran, ref message);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 提交事务并返回结果（使用表达式体方法）
        /// </summary>
        private Result CommitTransaction(Transaction tran, ref string message) =>
            tran.Commit() == TransactionStatus.Committed
                ? Result.Succeeded
                : (message = "事务提交失败！", Result.Failed).ToResult();

        /// <summary>
        /// 获取连接的扩展存储架构
        /// </summary>
        private Schema GetConnectionSchema(StructuralConnectionHandler conn)
        {
            var typeId = conn.GetTypeId();
            if (typeId == ElementId.InvalidElementId) return null;

            var connType = _model.Document.GetElement(typeId) as StructuralConnectionHandlerType;
            if (connType?.ConnectionGuid == null) return null;

            return Schema.ListSchemas()
                .FirstOrDefault(s => s.GUID == connType.ConnectionGuid);
        }
        #endregion
    }

    /// <summary>
    /// 结构连接数据模型 - 封装Revit数据操作
    /// </summary>
    public class StructuralConnectionModel
    {
        private UIDocument _uiDoc;

        public StructuralConnectionModel(UIDocument uiDoc)
        {
            _uiDoc = uiDoc;
        }

        public Document Document => _uiDoc.Document;

        /// <summary>
        /// 获取当前选中的元素ID列表
        /// </summary>
        public IList<ElementId> GetSelectedElementIds()
        {
            return _uiDoc.Selection.GetElementIds().ToList();
        }

        /// <summary>
        /// 选择连接元素（框架、柱、基础、楼板、墙）
        /// </summary>
        public List<ElementId> PickConnectionElements()
        {
            var filter = new LogicalOrFilter(new List<ElementFilter>
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFoundation),
                new ElementCategoryFilter(BuiltInCategory.OST_Floors),
                new ElementCategoryFilter(BuiltInCategory.OST_Walls)
            });

            var elemFilter = new StructuralConnectionSelectionFilter(filter);
            var refs = _uiDoc.Selection.PickObjects(
                ObjectType.Element,
                elemFilter,
                "请选择要连接的构件：").ToList();

            return refs.Select(r => r.ElementId).ToList();
        }

        /// <summary>
        /// 选择结构连接
        /// </summary>
        public StructuralConnectionHandler PickConnection()
        {
            //var filter = new LogicalOrFilter(new ElementCategoryFilter(BuiltInCategory.OST_StructConnections));
            //var elemFilter = new StructuralConnectionSelectionFilter(filter);
            LogicalOrFilter types = new LogicalOrFilter(new List<ElementFilter> { new ElementCategoryFilter(BuiltInCategory.OST_StructConnections) });
            StructuralConnectionSelectionFilter elemFilter = new StructuralConnectionSelectionFilter(types);
            var target = _uiDoc.Selection.PickObject(ObjectType.Element, elemFilter, "请选择结构连接：");

            return target != null ? _uiDoc.Document.GetElement(target) as StructuralConnectionHandler : null;
        }
    }
    // 元组扩展方法辅助类
    internal static class TupleResultExtension
    {
        public static Result ToResult(this (string message, Result result) tuple)
        {
            return tuple.result;
        }
    }

    /// <summary>
    /// 枚举到布尔值的转换器 - 用于RadioButton绑定
    /// </summary>
    public class EnumToBooleanConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }
}
