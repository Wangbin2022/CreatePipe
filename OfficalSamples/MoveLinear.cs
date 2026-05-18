using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class MoveLinear
    {
        public MoveLinear(ExternalCommandData commandData)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var selection = uiDoc.Selection;
            var doc = uiDoc.Document;

            // 使用using语句确保事务正确释放
            using (var trans = new Transaction(doc, "移动线性构件"))
            {
                string message = string.Empty;
                try
                {
                    // 启动事务
                    if (trans.Start() != TransactionStatus.Started)
                    {
                        message = "无法启动事务";
                        return;
                    }

                    // 获取选中的元素ID列表
                    var selectedIds = selection.GetElementIds();

                    // 验证：检查是否选中了元素
                    if (selectedIds.Count == 0)
                    {
                        TaskDialog.Show("移动线性构件", "请先选中一个构件");
                        trans.RollBack();
                        return;
                    }

                    // 验证：确保只选中一个元素
                    if (selectedIds.Count > 1)
                    {
                        TaskDialog.Show("移动线性构件", "请只选中一个构件");
                        trans.RollBack();
                        return;
                    }

                    // 获取选中的元素
                    var element = doc.GetElement(selectedIds.First());

                    if (element == null)
                    {
                        message = "无法获取选中的元素";
                        trans.RollBack();
                        return;
                    }

                    // 获取元素的位置曲线
                    var locationCurve = element.Location as LocationCurve;

                    // 验证：确保元素是基于曲线的构件
                    if (locationCurve == null)
                    {
                        TaskDialog.Show("移动线性构件", "请选中基于线条的构件（如墙体、梁、管道等）");
                        trans.RollBack();
                        return;
                    }

                    // 执行移动操作
                    bool moveSuccess = MoveCurveElement(locationCurve);

                    if (!moveSuccess)
                    {
                        message = "移动操作失败";
                        trans.RollBack();
                        return;
                    }
                    // 提交事务
                    trans.Commit();
                    TaskDialog.Show("移动线性构件", "构件移动成功");

                }
                catch (Exception ex)
                {
                    // 错误处理：回滚事务并记录错误信息
                    message = ex.Message;
                    trans.RollBack();
                    TaskDialog.Show("移动线性构件", $"操作失败：{ex.Message}");
                    return;
                }
            }
        }
        /// <summary>
        /// 移动曲线构件
        /// </summary>
        /// <param name="locationCurve">构件的位置曲线对象</param>
        /// <returns>移动是否成功</returns>
        private bool MoveCurveElement(LocationCurve locationCurve)
        {
            try
            {
                // 获取原始曲线
                var originalCurve = locationCurve.Curve;

                if (originalCurve == null)
                    return false;

                // 获取曲线的起点和终点
                var startPoint = originalCurve.GetEndPoint(0);
                var endPoint = originalCurve.GetEndPoint(1);

                // 定义偏移向量
                // 方案1：整体平移（推荐）- 将整条线段沿X轴正方向移动100单位
                var offsetVector = new XYZ(100, 0, 0);
                var newStartPoint = startPoint + offsetVector;
                var newEndPoint = endPoint + offsetVector;

                // 方案2：原代码逻辑（起点X+100，终点Y+100）- 会导致线段倾斜
                // 保留原逻辑供参考，但注释掉
                // var originalLogicStart = new XYZ(startPoint.X + 100, startPoint.Y, startPoint.Z);
                // var originalLogicEnd = new XYZ(endPoint.X, endPoint.Y + 100, endPoint.Z);

                // 创建新曲线（使用Line.CreateBound创建直线）
                // 注意：这里假设原曲线是直线，如果需要处理其他曲线类型，需要额外判断
                if (originalCurve is Line)
                {
                    var newLine = Line.CreateBound(newStartPoint, newEndPoint);
                    locationCurve.Curve = newLine;
                    return true;
                }
                else
                {
                    // 对于非直线类型的曲线，可以使用Transform平移整个曲线
                    var transform = Transform.CreateTranslation(offsetVector);
                    var transformedCurve = originalCurve.CreateTransformed(transform);
                    locationCurve.Curve = transformedCurve;
                    return true;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("移动线性构件", $"移动失败：{ex.Message}");
                return false;
            }
        }
    }
}
