using Autodesk.Revit.DB;
using System;

namespace CreatePipe.Utils
{
    public static class Unitconvert
    {
        public static double Tofoot(this double val)//mm转英尺
        {
            //double foot = UnitUtils.ConvertToInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            double foot = UnitUtils.ConvertToInternalUnits(val, UnitTypeId.Millimeters);

            return foot;
        }
        public static double Tofoot(this int val)//mm转英尺
        {
            //double foot = UnitUtils.ConvertToInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            double foot = UnitUtils.ConvertToInternalUnits(val, UnitTypeId.Millimeters);
            return foot;
        }
        public static double ToMillimeter(this int val)//英尺转mm
        {
            //double millimeter = UnitUtils.ConvertFromInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            double millimeter = UnitUtils.ConvertFromInternalUnits(val, UnitTypeId.Millimeters);
            return millimeter;
        }

        public static double ToMillimeter(this double val)//英尺转mm
        {
            //double millimeter = UnitUtils.ConvertFromInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            double millimeter = UnitUtils.ConvertFromInternalUnits(val, UnitTypeId.Millimeters);
            return millimeter;
        }
        public static double ToMillimeter2020(this double val)//英尺转mm
        {
            //double millimeter = UnitUtils.ConvertFromInternalUnits(val, DisplayUnitType.DUT_MILLIMETERS);
            double millimeter = UnitUtils.ConvertFromInternalUnits(val, UnitTypeId.Millimeters);
            return millimeter;
        }
        public static double AngleToRadian(this double angle)//角度转弧度,this是扩展的意思 引用更方便
        {
            double radins = angle / 180 * Math.PI;
            return radins;
        }
        /// 平方英尺转平方米

        public static double SquareFeetToSquareMeter(this double value)
        {
            //double k = 1 / 10.7639104;
            //return value * k;

            //double d = UnitUtils.Convert(value, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);
            double d = UnitUtils.Convert(value, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters);

            return d;
        }

    }
}
