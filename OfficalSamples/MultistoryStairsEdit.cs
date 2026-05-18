using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    ///// <summary>
    ///// 创建多层楼梯命令 - 从选中的单层楼梯创建多层楼梯
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.NoCommandData)]
    //public class CreateMultistoryStairsCommand : StairsCommandBase
    //{
    //    protected override Result ExecuteCore(UIDocument uiDoc, Document doc, ref string message, ElementSet elements)
    //    {
    //        // 验证选中的楼梯元素
    //        string errorMsg = "请在运行此命令前选中一个楼梯元素。";
    //        var stairsElem = GetSelectedElement<Stairs>(uiDoc, errorMsg, ref message);
    //        if (stairsElem == null)
    //            return Result.Failed;
    //        // 检查楼梯是否已属于多层楼梯
    //        if (stairsElem.MultistoryStairsId != ElementId.InvalidElementId)
    //        {
    //            TaskDialog.Show("警告", "选中的楼梯已属于一个多层楼梯。");
    //            return Result.Succeeded;
    //        }
    //        // 执行事务创建多层楼梯
    //        return ExecuteInTransaction(doc, "创建多层楼梯", () => MultistoryStairs.Create(stairsElem), ref message);
    //    }
    //}

    ///// <summary>
    ///// 添加楼梯层命令 - 向多层楼梯中添加新的楼层
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.NoCommandData)]
    //public class AddStairsCommand : StairsCommandBase
    //{
    //    protected override Result ExecuteCore(UIDocument uiDoc, Document doc, ref string message, ElementSet elements)
    //    {
    //        // 验证选中的多层楼梯元素
    //        string errorMsg = "请在运行此命令前选中一个多层楼梯。";
    //        var multistoryStairs = GetSelectedElement<MultistoryStairs>(uiDoc, errorMsg, ref message);
    //        if (multistoryStairs == null)  return Result.Failed;
    //        // 验证当前视图是否为立面视图
    //        var currentView = doc.ActiveView;
    //        if (currentView?.ViewType != ViewType.Elevation || currentView?.CanBePrinted != true)
    //        {
    //            message = "当前视图需要是立面视图，以便用户选择标高。";
    //            return Result.Failed;
    //        }
    //        // 使用选择过滤器让用户选取要添加的标高
    //        var selectionFilter = new LevelSelectionFilter(multistoryStairs, OperationAction.Add);
    //        var userSelectedRefs = uiDoc.Selection.PickObjects(ObjectType.Element, selectionFilter);
    //        // 使用LINQ提取选中的ElementId（C# 7.0简化语法）
    //        var selectedLevelIds = userSelectedRefs.Select(refer => refer.ElementId).ToHashSet();
    //        if (!selectedLevelIds.Any())
    //        {
    //            message = "未选中任何有效的标高。";   return Result.Failed;
    //        }
    //        // 执行事务添加楼层
    //        return ExecuteInTransaction(doc, "添加楼梯层",
    //            () => multistoryStairs.AddStairsByLevelIds(selectedLevelIds), ref message);
    //    }
    //}

    ///// <summary>
    ///// 移除楼梯层命令 - 从多层楼梯中移除指定的楼层
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.NoCommandData)]
    //public class RemoveStairsCommand : StairsCommandBase
    //{
    //    protected override Result ExecuteCore(UIDocument uiDoc, Document doc, ref string message, ElementSet elements)
    //    {
    //        // 验证选中的多层楼梯元素
    //        string errorMsg = "请在运行此命令前选中一个多层楼梯。";
    //        var multistoryStairs = GetSelectedElement<MultistoryStairs>(uiDoc, errorMsg, ref message);
    //        if (multistoryStairs == null) return Result.Failed;
    //        // 验证当前视图是否为立面视图
    //        var currentView = doc.ActiveView;
    //        if (currentView?.ViewType != ViewType.Elevation || currentView?.CanBePrinted != true)
    //        {
    //            message = "当前视图需要是立面视图，以便用户选择要移除的楼层标高。";
    //            return Result.Failed;
    //        }
    //        // 使用选择过滤器让用户选取要移除的标高
    //        var selectionFilter = new LevelSelectionFilter(multistoryStairs, OperationAction.Remove);
    //        var userSelectedRefs = uiDoc.Selection.PickObjects(ObjectType.Element, selectionFilter);
    //        // 使用C# 7.0的ToHashSet方法简化集合创建
    //        var selectedLevelIds = userSelectedRefs.Select(refer => refer.ElementId).ToHashSet();
    //        if (!selectedLevelIds.Any())
    //        {
    //            message = "未选中任何有效的标高。";    return Result.Failed;
    //        }
    //        // 执行事务移除楼层
    //        return ExecuteInTransaction(doc, "移除楼梯层",
    //            () => multistoryStairs.RemoveStairsByLevelIds(selectedLevelIds), ref message);
    //    }
    //}

    /// <summary>
    /// 命令执行结果的状态枚举
    /// </summary>
    internal enum ExecutionStatus
    {
        Succeeded,
        Failed,
        Cancelled
    }

    /// <summary>
    /// 选择操作类型枚举 - 定义对多层楼梯的操作类型
    /// </summary>
    internal enum OperationAction
    {
        Add,      // 添加楼梯层
        Remove,   // 移除楼梯层
        Unpin,    // 解固定以创建独立楼梯
        PinBack   // 重新固定
    }

    /// <summary>
    /// 标高选择过滤器 - 用于在立面视图中筛选可选的标高元素
    /// 使用C# 7.0表达式体成员简化代码
    /// </summary>
    internal class LevelSelectionFilter : ISelectionFilter
    {
        private readonly MultistoryStairs _multistoryStairs;
        private readonly OperationAction _action;

        public LevelSelectionFilter(MultistoryStairs ms, OperationAction action) =>
            (_multistoryStairs, _action) = (ms, action);

        /// <summary>
        /// 判断是否允许选中该元素 - 使用传统方式保持兼容
        /// </summary>
        public bool AllowElement(Element elem)
        {
            return true;
            //if (!(elem is Level level) || _multistoryStairs == null)
            //{
            //    return false;
            //}

            //switch (_action)
            //{
            //    case OperationAction.Add:
            //        return _multistoryStairs.CanAddStair(level.Id);
            //    case OperationAction.Remove:
            //        return _multistoryStairs.CanRemoveStair(level.Id);
            //    case OperationAction.Unpin:
            //        return _multistoryStairs.CanUnpin(level.Id);
            //    case OperationAction.PinBack:
            //        return _multistoryStairs.CanPin(level.Id);
            //    default:
            //        return false;
            //}
        }

        /// <summary>
        /// 不允许选中参考点
        /// </summary>
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    /// <summary>
    /// 命令基类 - 提取公共逻辑，使用泛型和抽象方法
    /// </summary>
    public abstract class StairsCommandBase : IExternalCommand
    {
        /// <summary>
        /// 执行命令的模板方法 - 使用C# 7.0 out变量和内联条件
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 获取UIDocument，使用条件运算符简化空值检查
            var uiDoc = commandData.Application.ActiveUIDocument;
            if (uiDoc == null)
            {
                message = "此命令需要在活动文档中运行。";
                return Result.Failed;
            }

            var doc = uiDoc.Document;

            // 执行具体命令逻辑
            return ExecuteCore(uiDoc, doc, ref message, elements);
        }

        /// <summary>
        /// 子类实现的具体命令逻辑 - 使用out参数传递消息
        /// </summary>
        protected abstract Result ExecuteCore(UIDocument uiDoc, Document doc, ref string message, ElementSet elements);

        /// <summary>
        /// 安全执行事务的辅助方法 - 使用C# 7.0元组和表达式体
        /// </summary>
        protected Result ExecuteInTransaction(Document doc, string transactionName, Action action, ref string message)
        {
            using (var trans = new Transaction(doc, transactionName))
            {
                try
                {
                    trans.Start();
                    action();
                    trans.Commit();
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    if (trans.HasStarted() && !trans.HasEnded())
                        trans.RollBack();
                    return Result.Failed;
                }
            }
        }

        /// <summary>
        /// 验证选中的元素是否为指定类型 - 使用泛型类型约束
        /// </summary>
        protected T GetSelectedElement<T>(UIDocument uiDoc, string errorMessage, ref string message) where T : Element
        {
            var selectedIds = uiDoc.Selection.GetElementIds();

            // 验证选中数量，使用条件运算符
            if (selectedIds.Count != 1)
            {
                message = errorMessage;
                return null;
            }

            var element = uiDoc.Document.GetElement(selectedIds.First());

            // 验证元素类型，使用as转换和null检查
            if (!(element is T result))
            {
                message = errorMessage;
                return null;
            }

            return result;
        }
    }
    internal class MultistoryStairsEdit
    {
    }
}
