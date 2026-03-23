using Autodesk.Revit.DB;
using System;

namespace CreatePipe.Utils
{
    public static class MEPSlopeHelper
    {
        /// <summary>
        /// 获取机电管线（管道、风管、桥架等）的坡度（正切值）
        /// </summary>
        /// <param name="mepCurve">机电曲线构件</param>
        /// <returns>坡度值</returns>
        public static double GetSlope(MEPCurve mepCurve)
        {
            if (mepCurve == null) return 0;
            // 1. 获取管线的定位曲线 (LocationCurve)
            LocationCurve locationCurve = mepCurve.Location as LocationCurve;
            if (locationCurve == null || locationCurve.Curve == null)
            {
                return 0.0;
            }
            // 2. 获取起点和终点的 XYZ 坐标
            Curve curve = locationCurve.Curve;
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            // 3. 计算 Z 轴的高度差 (Rise)
            double rise = Math.Abs(endPoint.Z - startPoint.Z);
            // 4. 计算 XY 平面上的水平投影长度 (Run)
            // 忽略 Z 值计算 2D 距离
            XYZ startPoint2D = new XYZ(startPoint.X, startPoint.Y, 0);
            XYZ endPoint2D = new XYZ(endPoint.X, endPoint.Y, 0);
            double run = startPoint2D.DistanceTo(endPoint2D);
            // 5. 防呆处理：如果是绝对垂直的立管（水平距离接近于 0），防止除以零报错
            if (run < 0.000001)
            {
                // 可以根据你的业务需求返回一个极大的值，或者返回 0 忽略立管
                // 这里返回 double.MaxValue 代表垂直（无穷大坡度）
                return double.MaxValue;
            }
            // 6. 坡度 = 高度差 / 水平长度
            // 注意：因为是比值，Revit内部的英尺(feet)单位会自动抵消，得到的就是纯正的坡度小数值
            double slope = rise / run;
            return slope;

            //// 1. 优先尝试读取内置参数 (管道和风管通常自带坡度参数)内置参数有为空或差错（立管与横管都为0.00度）可能
            //if (mepCurve is Pipe pipe)
            //{
            //    Parameter slopeParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE);
            //    if (slopeParam != null) return slopeParam.AsDouble();
            //}
            //else if (mepCurve is Duct duct)
            //{
            //    Parameter slopeParam = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SLOPE);
            //    if (slopeParam != null) return slopeParam.AsDouble();
            //}
            //// 2. 通用几何计算方法（适用于桥架、线管，或找不到参数的特殊情况）
            //Parameter startOffset = mepCurve.get_Parameter(BuiltInParameter.RBS_START_OFFSET_PARAM);
            //Parameter endOffset = mepCurve.get_Parameter(BuiltInParameter.RBS_END_OFFSET_PARAM);
            //Parameter lengthParam = mepCurve.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
            //if (startOffset != null && endOffset != null && lengthParam != null)
            //{
            //    double dh = Math.Abs(startOffset.AsDouble() - endOffset.AsDouble());
            //    double L = lengthParam.AsDouble();
            //    //double horiz = Math.Sqrt(Math.Max(0, L * L - dh * dh));
            //    double horizontalLength = Math.Sqrt(Math.Max(0, Math.Pow(L, 2) - Math.Pow(dh, 2)));
            //    // 避免除以0（垂直管线）
            //    return horizontalLength > 0.0001 ? dh / horizontalLength : 0;
            //}
            //return 0;
        }

        /// <summary>
        /// 检查机电管线坡度是否符合特定条件
        /// </summary>
        /// <param name="mepCurve">机电管线</param>
        /// <param name="symbol">比较符号(大于/小于/等于/不等于)</param>
        /// <param name="targetValue">目标坡度值</param>
        /// <param name="tol">容差，默认 0.00001</param>
        /// <returns>是否符合条件</returns>
        public static bool CheckCondition(MEPCurve mepCurve, string symbol, double targetValue, double tol = 0.00001)
        {
            double actualSlope = GetSlope(mepCurve);
            switch (symbol)
            {
                case "大于": return actualSlope > targetValue + tol;
                case "小于": return actualSlope < targetValue - tol;
                case "等于": return Math.Abs(actualSlope - targetValue) <= tol;
                case "不等于": return Math.Abs(actualSlope - targetValue) > tol;
                default: return false;
            }
        }
    }
}
