using Autodesk.Revit.DB;
using System;
using static CreatePipe.Form.CircleGaugePlaceViewModel;

namespace CreatePipe.models
{
    public class PlacementInfo
    {
        public PlacementType Type { get; set; }
        public XYZ Position { get; set; } // 用于角点族
        public double RotationInRadians { get; set; } // 用于角点族
        public Curve GeometryCurve { get; set; } // 用于线性族

        // 构造函数 for 角点族
        public PlacementInfo(PlacementType type, XYZ position, double rotation)
        {
            if (type == PlacementType.Straight)
                throw new ArgumentException("Use the Curve constructor for Straight types.");

            Type = type;
            Position = position;
            RotationInRadians = rotation;
            GeometryCurve = null;
        }

        // 构造函数 for 线性族
        public PlacementInfo(PlacementType type, Curve curve)
        {
            if (type != PlacementType.Straight)
                throw new ArgumentException("Curve constructor is only for Straight types.");

            Type = type;
            GeometryCurve = curve;
            Position = null; // 不适用
            RotationInRadians = 0; // 不适用
        }
    }
}
