namespace CreatePipe.OfficalSamples
{
    ///// <summary>
    ///// 实体切割工具 - 使用一个实体切割另一个实体
    ///// 演示如何使用 SolidSolidCutUtils.AddCutBetweenSolids API
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class Cut : IExternalCommand
    //{
    //    #region 常量定义
    //    // 使用命名常量替代硬编码的ID值，提高代码可读性和可维护性
    //    private const int CubeElementId = 30481;      // 被切割的立方体
    //    private const int SphereElementId = 30809;    // 用于切割的球体
    //    private const string RequiredFamilyFileName = "SolidSolidCut.rfa";
    //    private const string CutTransactionName = "执行实体切割操作";
    //    #endregion

    //    #region IExternalCommand 实现
    //    /// <summary>
    //    /// 执行实体切割命令
    //    /// </summary>
    //    /// <param name="commandData">Revit命令数据，包含应用和文档引用</param>
    //    /// <param name="message">错误消息输出参数</param>
    //    /// <param name="elements">高亮元素集合输出参数</param>
    //    /// <returns>命令执行状态</returns>
    //    public Result Execute(ExternalCommandData commandData,
    //        ref string message,
    //        ElementSet elements)
    //    {
    //        // 获取当前活动的Revit文档
    //        Document activeDoc = GetActiveDocument(commandData);

    //        // 验证文档有效性
    //        if (!ValidateDocument(activeDoc, out string errorMessage))
    //        {
    //            message = errorMessage;
    //            return Result.Failed;
    //        }

    //        // 获取待切割的实体元素
    //        (Element targetElement, Element cuttingElement) = GetCutElements(activeDoc);

    //        // 验证实体元素是否存在
    //        if (!ValidateElementsExist(targetElement, cuttingElement))
    //        {
    //            ShowMissingFamilyWarning();
    //            return Result.Succeeded;
    //        }

    //        // 执行实体切割操作
    //        bool cutSuccess = PerformSolidCut(activeDoc, targetElement, cuttingElement);

    //        return cutSuccess ? Result.Succeeded : Result.Failed;
    //    }
    //    #endregion

    //    #region 私有辅助方法
    //    /// <summary>
    //    /// 从命令数据中获取活动文档
    //    /// </summary>
    //    private Document GetActiveDocument(ExternalCommandData commandData)
    //    {
    //        // 使用空条件运算符和属性表达式简化代码
    //        return commandData?.Application?.ActiveUIDocument?.Document;
    //    }

    //    /// <summary>
    //    /// 验证文档是否有效且可编辑
    //    /// </summary>
    //    private bool ValidateDocument(Document doc, out string errorMessage)
    //    {
    //        errorMessage = string.Empty;

    //        // 使用模式匹配和元组解构简化验证逻辑
    //        if (doc is null)
    //        {
    //            errorMessage = "无法获取有效的Revit文档";
    //            return false;
    //        }

    //        if (doc.IsReadOnly)
    //        {
    //            errorMessage = "文档为只读状态，无法执行切割操作";
    //            return false;
    //        }

    //        if (!doc.IsModifiable)
    //        {
    //            errorMessage = "文档不可修改，请确保没有打开其他事务";
    //            return false;
    //        }

    //        return true;
    //    }

    //    /// <summary>
    //    /// 获取待切割的实体元素
    //    /// 使用元组语法返回多个值
    //    /// </summary>
    //    private (Element target, Element cutter) GetCutElements(Document doc)
    //    {
    //        // 使用C# 7.0的元组语法
    //        var targetElement = doc.GetElement(new ElementId(CubeElementId));
    //        var cuttingElement = doc.GetElement(new ElementId(SphereElementId));

    //        return (targetElement, cuttingElement);
    //    }

    //    /// <summary>
    //    /// 验证两个实体元素是否存在
    //    /// 使用模式匹配简化null检查
    //    /// </summary>
    //    private bool ValidateElementsExist(Element target, Element cutter)
    //    {
    //        // 使用is模式匹配进行null检查
    //        return !(target is null || cutter is null);
    //    }

    //    /// <summary>
    //    /// 显示缺少族文件的警告提示
    //    /// 使用字符串插值
    //    /// </summary>
    //    private void ShowMissingFamilyWarning()
    //    {
    //        string warningMessage = $"请先打开族文件 {RequiredFamilyFileName}，然后重新运行此命令。";
    //        TaskDialog.Show("提示", warningMessage);
    //    }

    //    /// <summary>
    //    /// 执行实体切割操作
    //    /// 使用out变量和内联声明简化代码
    //    /// </summary>
    //    private bool PerformSolidCut(Document doc, Element targetElement, Element cuttingElement)
    //    {
    //        // 内联out变量声明（C# 7.0特性）
    //        bool canCut = SolidSolidCutUtils.CanElementCutElement(
    //            cuttingElement,
    //            targetElement,
    //            out CutFailureReason failureReason);

    //        if (!canCut)
    //        {
    //            // 使用传统switch语句处理失败原因（C# 7.3兼容）
    //            string failureMessage;
    //            switch (failureReason)
    //            {
    //                case CutFailureReason.CutNotAppropriateForElements:
    //                    failureMessage = "几何形状不支持切割操作";
    //                    break;
    //                case CutFailureReason.CutAlreadyExists:
    //                    failureMessage = "已被几何形状切割";
    //                    break;
    //                case CutFailureReason.OppositeCutExists:
    //                    failureMessage = "已被反向切割";
    //                    break;
    //                default:
    //                    failureMessage = "未知原因，错误代码: " + failureReason.ToString();
    //                    break;
    //            }

    //            TaskDialog.Show("切割失败", failureMessage);
    //            return false;
    //        }

    //        // 使用using声明简化资源管理（C# 8.0特性）
    //        using (Transaction transaction = new Transaction(doc, CutTransactionName))
    //        {

    //            // 尝试启动事务
    //            if (transaction.Start() != TransactionStatus.Started)
    //            {
    //                TaskDialog.Show("错误", "无法启动事务，请检查文档状态");
    //                return false;
    //            }

    //            try
    //            {
    //                // 执行实体切割
    //                SolidSolidCutUtils.AddCutBetweenSolids(doc, targetElement, cuttingElement);

    //                // 提交事务
    //                if (transaction.Commit() == TransactionStatus.Committed)
    //                {
    //                    TaskDialog.Show("成功", "实体切割操作已成功完成");
    //                    return true;
    //                }
    //                else
    //                {
    //                    TaskDialog.Show("错误", "提交事务失败");
    //                    return false;
    //                }
    //            }
    //            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
    //            {
    //                // 使用异常过滤器（C# 6.0特性），仅捕获特定类型的异常
    //                transaction.RollBack();
    //                TaskDialog.Show("切割失败", $"执行切割时发生错误: {ex.Message}");
    //                return false;
    //            }
    //            catch (Exception ex)
    //            {
    //                transaction.RollBack();
    //                TaskDialog.Show("意外错误", $"发生未预期的错误: {ex.Message}");
    //                return false;
    //            }
    //        }
    //    }
    //    #endregion
    //}

    ///// <summary>
    ///// 实体切割撤销工具 - 移除两个实体之间的切割关系
    ///// 演示如何使用 SolidSolidCutUtils.RemoveCutBetweenSolids API
    ///// </summary>
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class Uncut : IExternalCommand
    //{
    //    #region 常量定义
    //    private const int CubeElementId = 30481;      // 被切割的立方体
    //    private const int SphereElementId = 30809;    // 用于切割的球体
    //    private const string RequiredFamilyFileName = "SolidSolidCut.rfa";
    //    private const string UncutTransactionName = "移除实体切割关系";
    //    #endregion

    //    #region IExternalCommand 实现
    //    /// <summary>
    //    /// 执行撤销切割命令
    //    /// </summary>
    //    public Result Execute(ExternalCommandData commandData,
    //        ref string message,
    //        ElementSet elements)
    //    {
    //        Document activeDoc = commandData?.Application?.ActiveUIDocument?.Document;

    //        // 使用条件运算符进行验证
    //        if (activeDoc is null)
    //        {
    //            message = "无法获取有效的Revit文档";
    //            return Result.Failed;
    //        }

    //        // 获取实体元素（使用解构语法）
    //        var (targetElement, cuttingElement) = GetCutElements(activeDoc);

    //        // 验证元素存在性
    //        if (targetElement is null || cuttingElement is null)
    //        {
    //            ShowMissingFamilyWarning();
    //            return Result.Succeeded;
    //        }

    //        // 检查两个实体之间是否存在切割关系
    //        if (!HasCutRelationship(activeDoc, targetElement, cuttingElement))
    //        {
    //            TaskDialog.Show("提示", "选中的两个实体之间不存在切割关系");
    //            return Result.Succeeded;
    //        }

    //        // 执行撤销切割操作
    //        return PerformUncut(activeDoc, targetElement, cuttingElement);
    //    }
    //    #endregion

    //    #region 私有辅助方法
    //    /// <summary>
    //    /// 获取待操作的实体元素（使用元组语法）
    //    /// </summary>
    //    private (Element target, Element cutter) GetCutElements(Document doc)
    //    {
    //        return (doc.GetElement(new ElementId(CubeElementId)),
    //                doc.GetElement(new ElementId(SphereElementId)));
    //    }

    //    /// <summary>
    //    /// 显示缺少族文件的警告
    //    /// </summary>
    //    private void ShowMissingFamilyWarning()
    //    {
    //        string message = $"请先打开族文件 {RequiredFamilyFileName}，然后重新运行此命令。";
    //        TaskDialog.Show("提示", message);
    //    }

    //    /// <summary>
    //    /// 检查两个实体之间是否存在切割关系
    //    /// </summary>
    //    private bool HasCutRelationship(Document doc, Element target, Element cutter)
    //    {
    //        // 通过检查切割器元素是否在目标元素的切割列表中判断
    //        // 注意：这是一个简化的检查，实际可能需要更复杂的逻辑
    //        if (target is null || cutter is null) return false;

    //        // 尝试获取被切割元素的切割器集合
    //        try
    //        {
    //            // 某些元素类型支持GetCuttingElements方法
    //            // 如果文档不支持，返回false
    //            return cutter.CanBeCut(target);
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 执行撤销切割操作（使用using声明和异常过滤器）
    //    /// </summary>
    //    private Result PerformUncut(Document doc, Element targetElement, Element cuttingElement)
    //    {
    //        // 使用using声明简化事务管理（C# 8.0）
    //        using (Transaction transaction = new Transaction(doc, UncutTransactionName))
    //        {

    //            if (transaction.Start() != TransactionStatus.Started)
    //            {
    //                TaskDialog.Show("错误", "无法启动事务");
    //                return Result.Failed;
    //            }

    //            try
    //            {
    //                // 移除两个实体之间的切割关系
    //                SolidSolidCutUtils.RemoveCutBetweenSolids(doc, targetElement, cuttingElement);

    //                if (transaction.Commit() == TransactionStatus.Committed)
    //                {
    //                    TaskDialog.Show("成功", "已成功移除实体切割关系");
    //                    return Result.Succeeded;
    //                }

    //                TaskDialog.Show("错误", "提交事务失败");
    //                return Result.Failed;
    //            }
    //            catch (Exception ex) when (ex is InvalidOperationException)
    //            {
    //                // 使用异常过滤器，仅捕获无效操作异常
    //                transaction.RollBack();
    //                TaskDialog.Show("撤销失败", $"无法移除切割关系: {ex.Message}");
    //                return Result.Failed;
    //            }
    //            catch (Exception ex)
    //            {
    //                transaction.RollBack();
    //                TaskDialog.Show("意外错误", $"发生错误: {ex.Message}");
    //                return Result.Failed;
    //            }
    //        }
    //    }
    //    #endregion
    //}

    ///// <summary>
    ///// 扩展方法类 - 为Element类型添加辅助方法（使用C#扩展方法特性）
    ///// </summary>
    //public static class ElementExtensions
    //{
    //    /// <summary>
    //    /// 检查元素是否可以被指定元素切割
    //    /// </summary>
    //    public static bool CanBeCut(this Element element, Element cutter)
    //    {
    //        if (element is null || cutter is null) return false;

    //        // 使用out变量简化调用
    //        return SolidSolidCutUtils.CanElementCutElement(cutter, element, out _);
    //    }

    //    /// <summary>
    //    /// 尝试执行切割操作，返回是否成功
    //    /// </summary>
    //    public static bool TryCut(this Document doc, Element target, Element cutter, out string error)
    //    {
    //        error = string.Empty;

    //        if (doc is null)
    //        {
    //            error = "文档无效";
    //            return false;
    //        }

    //        if (target is null || cutter is null)
    //        {
    //            error = "实体元素无效";
    //            return false;
    //        }

    //        try
    //        {
    //            // 使用out变量内联声明
    //            if (!SolidSolidCutUtils.CanElementCutElement(cutter, target, out CutFailureReason reason))
    //            {
    //                error = $"无法执行切割: {reason}";
    //                return false;
    //            }

    //            using (Transaction trans = new Transaction(doc, "尝试切割"))
    //            {
    //                if (trans.Start() != TransactionStatus.Started)
    //                {
    //                    error = "无法启动事务";
    //                    return false;
    //                }

    //                SolidSolidCutUtils.AddCutBetweenSolids(doc, target, cutter);
    //                trans.Commit();
    //            }
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            error = ex.Message;
    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 尝试撤销切割操作，返回是否成功
    //    /// </summary>
    //    public static bool TryUncut(this Document doc, Element target, Element cutter, out string error)
    //    {
    //        error = string.Empty;

    //        if (doc is null || target is null || cutter is null)
    //        {
    //            error = "无效的参数";
    //            return false;
    //        }

    //        try
    //        {
    //            using (Transaction trans = new Transaction(doc, "尝试撤销切割"))
    //            {
    //                if (trans.Start() != TransactionStatus.Started)
    //                {
    //                    error = "无法启动事务";
    //                    return false;
    //                }

    //                SolidSolidCutUtils.RemoveCutBetweenSolids(doc, target, cutter);
    //                trans.Commit();
    //            }
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            error = ex.Message;
    //            return false;
    //        }
    //    }
    //}
    internal class SolidSolidCut
    {
    }
}
