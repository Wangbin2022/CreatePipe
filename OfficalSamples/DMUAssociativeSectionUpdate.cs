using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 关联剖面更新器命令
    /// 功能：将剖面视图与窗户关联，当窗户移动时剖面自动跟随
    /// </summary>
    internal class DMUAssociativeSectionUpdate
    {
        // 全局更新器实例（确保整个会话期间只有一个）
        private static SectionUpdater _sectionUpdater;
        // 当前关联的窗户ID和剖面ID
        private static ElementId _currentWindowId = ElementId.InvalidElementId;
        private static ElementId _currentSectionId = ElementId.InvalidElementId;
        public DMUAssociativeSectionUpdate(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                var uiDoc = commandData.Application.ActiveUIDocument;
                var document = uiDoc.Document;
                var addInId = commandData.Application.ActiveAddInId;
                // 初始化或获取更新器实例
                InitializeUpdater(document, addInId);
                // 提示用户操作
                TaskDialog.Show("提示", "请先选择剖面视图，然后选择要关联的窗户。");
                // 选择剖面视图
                var sectionElement = PickSectionView(uiDoc);
                if (sectionElement == null)
                {
                    return;
                }
                // 选择窗户
                var windowElement = PickWindow(uiDoc);
                if (windowElement == null)
                {
                    return;
                }
                // 查找真正的 ViewSection 对象
                var viewSectionId = FindViewSection(document, sectionElement.Name);
                if (viewSectionId == ElementId.InvalidElementId)
                {
                    message = $"找不到名为 \"{sectionElement.Name}\" 的剖面视图";
                    return;
                }
                // 建立关联关系
                var result = AssociateSectionToWindow(document, windowElement.Id,
                    viewSectionId, sectionElement);
                // 注册文档关闭事件，在关闭时清理更新器
                document.DocumentClosing += OnDocumentClosing;
            }
            catch (OperationCanceledException)
            {
                TaskDialog.Show("提示", "操作已取消。");
                return;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return;
            }
        }
        /// <summary>
        /// 初始化更新器
        /// </summary>
        private static void InitializeUpdater(Document document, AddInId addInId)
        {
            if (_sectionUpdater != null) return;

            using (var transaction = new Transaction(document, "注册剖面更新器"))
            {
                transaction.Start();

                _sectionUpdater = new SectionUpdater(addInId);
                _sectionUpdater.Register(document);

                transaction.Commit();
            }
        }

        /// <summary>
        /// 选择剖面视图
        /// </summary>
        private static Element PickSectionView(UIDocument uiDoc)
        {
            var reference = uiDoc.Selection.PickObject(
                ObjectType.Element, "请选择一个剖面视图");

            return reference != null
                ? uiDoc.Document.GetElement(reference)
                : null;
        }

        /// <summary>
        /// 选择窗户
        /// </summary>
        private static FamilyInstance PickWindow(UIDocument uiDoc)
        {
            var reference = uiDoc.Selection.PickObject(
                ObjectType.Element, "请选择要关联剖面的窗户");

            if (reference == null) return null;

            var element = uiDoc.Document.GetElement(reference);
            return element as FamilyInstance;
        }

        /// <summary>
        /// 查找真正的剖面视图（ViewSection）
        /// </summary>
        private static ElementId FindViewSection(Document document, string viewName)
        {
            var collector = new FilteredElementCollector(document);

            var sectionView = collector
                .OfClass(typeof(ViewSection))
                .Cast<ViewSection>()
                .FirstOrDefault(v => v.Name == viewName);

            return sectionView?.Id ?? ElementId.InvalidElementId;
        }

        /// <summary>
        /// 关联剖面对窗户
        /// </summary>
        private static Result AssociateSectionToWindow(Document document,
            ElementId windowId, ElementId sectionId, Element sectionElement)
        {
            // 检查是否已经关联
            if (_currentWindowId == windowId && _currentSectionId == sectionId)
            {
                TaskDialog.Show("提示", "该窗户已经与当前剖面关联。");
                return Result.Succeeded;
            }

            // 清除旧的触发器并添加新的
            var updaterId = _sectionUpdater.GetUpdaterId();
            UpdaterRegistry.RemoveAllTriggers(updaterId);

            _sectionUpdater.SetAssociation(windowId, sectionId, sectionElement);
            _sectionUpdater.AddTriggerForUpdater(document, windowId, sectionId);

            // 更新全局状态
            _currentWindowId = windowId;
            _currentSectionId = sectionId;

            TaskDialog.Show("提示",
                $"关联成功！\n剖面 ID: {sectionId}\n窗户 ID: {windowId}\n" +
                "您现在可以移动或修改窗户，剖面将自动跟随。");

            return Result.Succeeded;
        }

        /// <summary>
        /// 文档关闭时的清理工作
        /// </summary>
        private static void OnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (_sectionUpdater != null)
            {
                UpdaterRegistry.UnregisterUpdater(_sectionUpdater.GetUpdaterId());
                _sectionUpdater = null;
            }

            _currentWindowId = ElementId.InvalidElementId;
            _currentSectionId = ElementId.InvalidElementId;
        }
    }
    /// <summary>
    /// 剖面更新器
    /// 实现 IUpdater 接口，自动监听窗户变化并更新剖面
    /// </summary>
    public class SectionUpdater : IUpdater
    {
        private readonly UpdaterId _updaterId;
        private ElementId _windowId;
        private ElementId _sectionId;
        private Element _sectionElement;

        // 唯一标识符 GUID
        private static readonly Guid UpdaterGuid =
            new Guid("FBF3F6B2-4C06-42d4-97C1-D1B4EB593EFF");

        public SectionUpdater(AddInId addInId)
        {
            _updaterId = new UpdaterId(addInId, UpdaterGuid);
        }

        /// <summary>
        /// 设置关联关系
        /// </summary>
        public void SetAssociation(ElementId windowId, ElementId sectionId, Element sectionElement)
        {
            _windowId = windowId;
            _sectionId = sectionId;
            _sectionElement = sectionElement;
        }

        /// <summary>
        /// 注册更新器
        /// </summary>
        public void Register(Document document)
        {
            if (!UpdaterRegistry.IsUpdaterRegistered(_updaterId))
            {
                UpdaterRegistry.RegisterUpdater(this, document);
            }
        }

        /// <summary>
        /// 添加触发器
        /// </summary>
        public void AddTriggerForUpdater(Document document, ElementId windowId, ElementId sectionId)
        {
            if (windowId == ElementId.InvalidElementId) return;

            var windowIds = new List<ElementId> { windowId };

            // 监听几何变化
            UpdaterRegistry.AddTrigger(_updaterId, document, windowIds,
                Element.GetChangeTypeGeometry());
        }

        #region IUpdater 接口实现

        public void Execute(UpdaterData data)
        {
            try
            {
                var document = data.GetDocument();
                var modifiedIds = data.GetModifiedElementIds();

                // 检查被修改的元素是否是我们关注的窗户
                if (!modifiedIds.Contains(_windowId)) return;

                // 获取窗户实例
                var window = document.GetElement(_windowId) as FamilyInstance;
                var section = document.GetElement(_sectionId) as ViewSection;

                if (window == null || section == null) return;

                // 调整剖面视图的位置和方向
                AdjustSectionView(document, window, section);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("更新器异常", ex.Message);
            }
        }

        public UpdaterId GetUpdaterId() => _updaterId;

        public string GetUpdaterName() => "关联剖面更新器";

        public string GetAdditionalInformation() =>
            "自动移动剖面视图以保持与窗户的相对位置关系";

        public ChangePriority GetChangePriority() => ChangePriority.Views;

        #endregion

        /// <summary>
        /// 调整剖面视图
        /// 根据窗户的位置和朝向重新计算剖面的位置和旋转角度
        /// </summary>
        private void AdjustSectionView(Document document, FamilyInstance window, ViewSection section)
        {
            // 获取窗户的位置和朝向
            var (windowPosition, facingOrientation) = GetWindowPositionAndOrientation(window);

            // 获取剖面的当前位置和方向
            var sectionOrigin = section.Origin;
            var sectionDirection = section.ViewDirection;

            // 计算窗户的横向方向（垂直于朝向）
            var lateralDirection = facingOrientation.CrossProduct(XYZ.BasisZ);

            // 旋转剖面视图
            RotateSection(document, sectionOrigin, facingOrientation,
                sectionDirection, lateralDirection);

            // 刷新文档
            document.Regenerate();

            // 移动剖面视图
            MoveSection(document, windowPosition, sectionOrigin,
                lateralDirection, section);
        }

        /// <summary>
        /// 获取窗户的位置和朝向
        /// </summary>
        private static (XYZ position, XYZ facingOrientation) GetWindowPositionAndOrientation(
            FamilyInstance window)
        {
            var position = XYZ.Zero;
            var facingOrientation = XYZ.Zero;

            if (window.Location is LocationPoint locationPoint)
            {
                position = locationPoint.Point;
            }

            facingOrientation = window.FacingOrientation;

            return (position, facingOrientation);
        }

        /// <summary>
        /// 旋转剖面视图
        /// </summary>
        private void RotateSection(Document document, XYZ sectionOrigin,
            XYZ facingOrientation, XYZ sectionDirection, XYZ lateralDirection)
        {
            // 计算需要旋转的角度
            var angle = facingOrientation.AngleTo(sectionDirection);

            // 确定旋转方向
            var cross = lateralDirection.CrossProduct(sectionDirection).Normalize();
            var sign = cross.IsAlmostEqualTo(XYZ.BasisZ) ? 1.0 : -1.0;

            // 计算最终旋转角度
            var rotateAngle = CalculateRotationAngle(angle) * sign;

            if (Math.Abs(rotateAngle) > 0)
            {
                var axis = Line.CreateBound(sectionOrigin, sectionOrigin + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(document, _sectionElement.Id, axis, rotateAngle);
            }
        }

        /// <summary>
        /// 计算旋转角度
        /// </summary>
        private static double CalculateRotationAngle(double angle)
        {
            var absAngle = Math.Abs(angle);

            if (absAngle <= 0)
            {
                return 0;
            }
            else if (absAngle <= Math.PI / 2)
            {
                return Math.PI / 2 - angle;
            }
            else
            {
                return angle - Math.PI / 2;
            }
        }

        /// <summary>
        /// 移动剖面视图
        /// </summary>
        private void MoveSection(Document document, XYZ windowPosition,
            XYZ sectionOrigin, XYZ lateralDirection, ViewSection section)
        {
            // 计算窗户在横向方向上的投影
            var windowProjection = windowPosition.DotProduct(lateralDirection);
            var sectionProjection = sectionOrigin.DotProduct(lateralDirection);

            var moveDistance = windowProjection - sectionProjection;

            // 获取旋转后的新方向
            var newSectionDirection = section.ViewDirection;

            // 计算移动向量
            var correction = lateralDirection.DotProduct(newSectionDirection);
            var translation = newSectionDirection * correction * moveDistance;

            if (!translation.IsZeroLength())
            {
                ElementTransformUtils.MoveElement(document, _sectionElement.Id, translation);
            }
        }
    }

}
