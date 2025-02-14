using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.utils
{
    /// <summary>
    /// 单位转换
    /// </summary>
  public static  class UnitConvers
    {


        /// <summary>
        /// 毫米转英尺
        /// </summary>
        /// <param name="val">毫米</param>
        /// <returns></returns>
        public static double Tofoot(this double val)
        {
            double foot = UnitUtils.ConvertToInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            return foot;
        }
        /// <summary>
        /// 毫米转英尺
        /// </summary>
        /// <param name="val">毫米</param>
        /// <returns></returns>
        public static double Tofoot(this int val)
        {
            double foot = UnitUtils.ConvertToInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            return foot;
        }


        /// <summary>
        /// 英尺转毫米
        /// </summary>
        /// <param name="val">毫米</param>
        /// <returns></returns>
        public static double ToMillimeter(this int val)
        {
            double millimeter = UnitUtils.ConvertFromInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            return millimeter;
        }

        /// <summary>
        /// 英尺转毫米
        /// </summary>
        /// <param name="val">毫米</param>
        /// <returns></returns>
        public static double ToMillimeter(this double val)
        {
            double millimeter = UnitUtils.ConvertFromInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            return millimeter;
        }

        /// <summary>
        /// 角度转弧度值
        /// </summary>
        /// <Author>Li Wen Jin</Author>
        /// <param name="angle">角度值</param>
        /// <returns>弧度值</returns>
        public static double AngleToRadian(this double angle)
        {
            double radins = angle / 180 * Math.PI;
            return radins;
        }

        /// <summary>
        /// 平方英尺转平方米
        /// </summary>
        /// <Author>Li Wen Jin</Author>
        /// <param name="value">平方英尺</param>
        /// <returns>平方米</returns>
        public static double SquareFeetToSquareMeter(this double value)
        {
            //double k = 1 / 10.7639104;
            //return value * k;

            double d = UnitUtils.Convert(value, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);

            return d;
        }

    }
}
