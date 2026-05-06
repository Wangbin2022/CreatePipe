using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    internal class FloorSpanDirectionProcess
    {
        Document _document;
        public FloorSpanDirectionProcess(Element elem)
        {
            _document = elem.Document;
            Floor floor =_document.GetElement(elem.Id) as Floor;
            AnalyzeFloorSpanDirection(floor);
        }
        /// <summary>
        /// 分析楼板跨度方向
        /// </summary>
        /// <param name="floor">楼板对象</param>
        private void AnalyzeFloorSpanDirection(Floor floor)
        {
            // 使用模式匹配进行null检查
            if (floor is null)
            {
                throw new ArgumentNullException(nameof(floor), "楼板对象为空");
            }

            // 验证楼板是否支持跨度方向分析
            if (!IsFloorSupportedForSpanAnalysis(floor, out string unsupportedReason))
            {
                ShowWarningMessage($"当前楼板不支持跨度方向分析: {unsupportedReason}");
                return;
            }

            // 获取跨度方向信息（使用元组返回多个值）
            var (angle, symbolsInfo) = GetSpanDirectionInfo(floor);

            // 构建并显示结果
            string resultMessage = BuildResultMessage(angle, symbolsInfo);
            ShowResultDialog(resultMessage);
        }

        /// <summary>
        /// 检查楼板是否支持跨度方向分析
        /// </summary>
        private bool IsFloorSupportedForSpanAnalysis(Floor floor, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 尝试获取跨度方向角度，如果楼板非结构性会抛出异常
                double testAngle = floor.SpanDirectionAngle;
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                reason = "楼板为非结构性楼板，不支持跨度方向分析";
                return false;
            }
            catch
            {
                reason = "获取跨度方向信息失败";
                return false;
            }
        }

        /// <summary>
        /// 获取跨度方向信息（使用元组语法）
        /// </summary>
        /// <returns>(跨度方向角度, 符号信息列表)</returns>
        private (double angle, List<string> symbolNames) GetSpanDirectionInfo(Floor floor)
        {
            // 获取跨度方向角度（弧度）
            double angleInRadians = floor.SpanDirectionAngle;

            // 获取跨度方向符号ID集合
            ICollection<ElementId> symbolIds = floor.GetSpanDirectionSymbolIds();

            // 使用LINQ查询符号名称
            var symbolNames = symbolIds
                .Select(id => GetSpanDirectionSymbolName(id))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            return (angleInRadians, symbolNames);
        }

        /// <summary>
        /// 获取跨度方向符号的名称
        /// </summary>
        private string GetSpanDirectionSymbolName(ElementId symbolId)
        {
            Element element = _document.GetElement(symbolId);
            if (element is null) return string.Empty;

            // 获取元素类型信息
            ElementId typeId = element.GetTypeId();
            ElementType elementType = _document.GetElement(typeId) as ElementType;

            return elementType?.Name ?? string.Empty;
        }

        /// <summary>
        /// 构建结果消息（使用字符串插值）
        /// </summary>
        private string BuildResultMessage(double angleInRadians, List<string> symbolNames)
        {
            // 将弧度转换为角度（可选显示）
            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

            // 使用字符串插值和逐字字符串
            string message = $@"=== 楼板跨度方向分析结果 ===

【跨度方向角度】
  弧度值: {angleInRadians:F6} rad
  角度值: {angleInDegrees:F2}°

【跨度方向符号】
  数量: {symbolNames.Count}
  符号列表:";

            if (symbolNames.Any())
            {
                // 使用string.Join构建符号列表
                string symbols = string.Join("\n  • ", symbolNames);
                message += $"\n  • {symbols}";
            }
            else
            {
                message += "\n  无";
            }

            return message;
        }

        /// <summary>
        /// 显示结果对话框
        /// </summary>
        private void ShowResultDialog(string message)
        {
            TaskDialog dialog = new TaskDialog("楼板跨度方向分析结果")
            {
                MainContent = message,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                TitleAutoPrefix = false,
                AllowCancellation = false
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "确定");
            dialog.Show();
        }
 

        #region 辅助方法
        /// <summary>
        /// 从命令数据中获取文档（使用表达式体方法）
        /// </summary>
        private Document GetDocument(ExternalCommandData commandData) =>
            commandData?.Application?.ActiveUIDocument?.Document;

        /// <summary>
        /// 验证文档有效性
        /// </summary>
        private bool ValidateDocument(Document doc, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 使用模式匹配进行null检查
            if (doc is null)
            {
                errorMessage = "无法获取有效的Revit文档";
                return false;
            }

            if (doc.IsReadOnly)
            {
                errorMessage = "文档为只读状态，无法执行分析";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 显示警告消息（使用默认参数简化调用）
        /// </summary>
        private void ShowWarningMessage(string content, string title = "提示")
        {
            TaskDialog.Show(title, content, TaskDialogCommonButtons.Ok);
        }

        /// <summary>
        /// 将弧度转换为角度（扩展方法示例）
        /// </summary>
        private static double RadiansToDegrees(double radians) => radians * (180.0 / Math.PI);
        #endregion
    }

}
