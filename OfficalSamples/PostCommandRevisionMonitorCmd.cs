using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace CreatePipe.OfficalSamples
{
    internal class PostCommandRevisionMonitorCmd
    {
        private static PostCommandRevisionMonitor _monitor;
        private static PushButton _commandButton;
        public PostCommandRevisionMonitorCmd(ExternalCommandData commandData)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            if (_monitor == null)
            {
                _monitor = new PostCommandRevisionMonitor(doc);
                _monitor.Activate();
                if (_commandButton != null)
                    _commandButton.ItemText = "移除修订监控";
            }
            else
            {
                _monitor.Deactivate();
                _monitor = null;
                if (_commandButton != null)
                    _commandButton.ItemText = "设置修订监控";
            }

        }
        public static void SetPushButton(PushButton pushButton) => _commandButton = pushButton;
    }
    /// <summary>
    /// 修订监控核心类 - 监控保存操作并引导用户添加修订
    /// 使用C# 7.3语法：表达式体成员、nameof、模式匹配
    /// </summary>
    internal class PostCommandRevisionMonitor
    {
        private readonly Document _document;
        private int _storedRevisionCount;
        private ExternalEvent _externalEvent;
        private AddInCommandBinding _binding;

        public PostCommandRevisionMonitor(Document doc) => _document = doc;

        /// <summary>
        /// 激活监控 - 订阅事件并记录初始修订数量
        /// </summary>
        public void Activate()
        {
            _storedRevisionCount = GetRevisionCount(_document);
            _document.DocumentSaving += OnSavingPromptForRevisions;
        }

        /// <summary>
        /// 停用监控 - 取消事件订阅
        /// </summary>
        public void Deactivate() => _document.DocumentSaving -= OnSavingPromptForRevisions;

        /// <summary>
        /// 保存时回调 - 检查修订数量，若未增加则提示用户
        /// </summary>
        private void OnSavingPromptForRevisions(object sender, DocumentSavingEventArgs args)
        {
            var doc = (Document)sender;
            var uiApp = new UIDocument(doc).Application;

            if (!doc.IsModified) return;

            var revisionCount = GetRevisionCount(doc);
            if (revisionCount > _storedRevisionCount)
            {
                _storedRevisionCount = revisionCount;
                return;
            }

            // 显示提示对话框
            var result = ShowWarningDialog();

            switch (result)
            {
                case TaskDialogResult.CommandLink1:  // 立即添加修订
                    args.Cancel();
                    uiApp.DialogBoxShowing += HideDocumentNotSaved;
                    PromptToEditRevisionsAndResave(uiApp);
                    break;

                case TaskDialogResult.CommandLink2:  // 取消保存
                    args.Cancel();
                    break;

                    // CommandLink3: 继续保存 - 不做任何操作
            }
        }

        /// <summary>
        /// 显示警告对话框 - 使用C# 7.3的表达式体和方法链
        /// </summary>
        private static TaskDialogResult ShowWarningDialog()
        {
            var td = new TaskDialog("未创建修订")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = "文档已被修改，但未创建新的修订。",
                ExpandedContent = "由于文档已发布，每次变更通常需要发布新的修订编号。",
                TitleAutoPrefix = false,
                AllowCancellation = false
            };

            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "立即添加修订");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "取消保存");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "继续保存（不推荐）");

            return td.Show();
        }

        /// <summary>
        /// 隐藏默认的"文档未保存"对话框
        /// </summary>
        private static void HideDocumentNotSaved(object sender, DialogBoxShowingEventArgs args)
        {
            if (args is TaskDialogShowingEventArgs tdArgs &&
                tdArgs.Message?.Contains("not saved") == true)
            {
                args.OverrideResult(0x0008);
            }
        }

        /// <summary>
        /// 引导用户编辑修订并重新保存
        /// </summary>
        private void PromptToEditRevisionsAndResave(UIApplication app)
        {
            // 创建外部事件，用于修订命令完成后触发清理
            _externalEvent = ExternalEvent.Create(new PostCommandRevisionMonitorEvent(this));

            // 绑定修订命令，监听其执行前事件
            var cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.SheetIssuesOrRevisions);
            _binding = app.CreateAddInCommandBinding(cmdId);

            _binding.BeforeExecuted -= ReactToRevisionsAndSchedulesCommand; // 避免重复订阅
            _binding.BeforeExecuted += ReactToRevisionsAndSchedulesCommand;

            // 发布修订编辑命令
            app.PostCommand(cmdId);
        }

        /// <summary>
        /// 修订命令执行前回调 - 触发外部事件
        /// </summary>
        private void ReactToRevisionsAndSchedulesCommand(object sender, BeforeExecutedEventArgs args) =>
            _externalEvent?.Raise();

        /// <summary>
        /// 清理并重新保存 - 在修订完成后调用
        /// </summary>
        private void CleanupAfterRevisionEdit(UIApplication uiApp)
        {
            // 移除对话框拦截
            uiApp.DialogBoxShowing -= HideDocumentNotSaved;

            // 移除命令绑定回调
            if (_binding != null)
            {
                _binding.BeforeExecuted -= ReactToRevisionsAndSchedulesCommand;
                _binding = null;
            }

            _externalEvent = null;

            // 重新发布保存命令
            var saveCmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.Save);
            uiApp.PostCommand(saveCmdId);
        }

        /// <summary>
        /// 获取文档中的修订数量 - 使用LINQ简化
        /// </summary>
        private static int GetRevisionCount(Document doc) =>
            new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Revisions)
                .ToElementIds()
                .Count;

        #region 嵌套类：外部事件处理器
        /// <summary>
        /// 外部事件处理器 - 在修订命令完成后执行清理
        /// </summary>
        private class PostCommandRevisionMonitorEvent : IExternalEventHandler
        {
            private readonly PostCommandRevisionMonitor _monitor;

            public PostCommandRevisionMonitorEvent(PostCommandRevisionMonitor monitor) =>
                _monitor = monitor;

            public void Execute(UIApplication app) =>
                _monitor.CleanupAfterRevisionEdit(app);

            public string GetName() => nameof(PostCommandRevisionMonitorEvent);
        }
        #endregion
    }
}
