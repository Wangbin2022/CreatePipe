using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 族实例绘制器 - 沿正弦曲线计算点并放置族实例
    /// </summary>
    internal class FamilyInstancePlotter
    {
        private readonly FamilySymbol _familySymbol;
        private readonly Document _document;

        // 默认使用非结构类型
        private const StructuralType DefaultStructuralType = StructuralType.NonStructural;

        public FamilyInstancePlotter(FamilySymbol familySymbol, Document document)
        {
            _familySymbol = familySymbol ?? throw new ArgumentNullException(nameof(familySymbol));
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// 沿正弦曲线放置族实例
        /// </summary>
        /// <param name="partitions">每周期分区数</param>
        /// <param name="period">正弦曲线周期</param>
        /// <param name="amplitude">振幅</param>
        /// <param name="numOfCircles">周期数</param>
        public void PlaceInstancesOnCurve(int partitions, double period, double amplitude, double numOfCircles)
        {
            if (partitions <= 0) throw new ArgumentException("分区数必须大于0", nameof(partitions));

            using (var transGroup = new TransactionGroup(_document, "沿曲线放置族实例"))
            {
                transGroup.Start();

                var points = CalculateCurvePoints(partitions, period, amplitude, numOfCircles);

                foreach (var point in points)
                {
                    PlaceFamilyInstance(point);
                }

                transGroup.Assimilate();
            }
        }

        /// <summary>
        /// 计算正弦曲线上的所有点 - 使用yield return
        /// </summary>
        private static IEnumerable<XYZ> CalculateCurvePoints(int partitions, double period, double amplitude, double numOfCircles)
        {
            var stepsPerCycle = partitions;
            var totalSteps = (int)(stepsPerCycle * numOfCircles);
            var angleIncrement = 2 * Math.PI / stepsPerCycle;

            for (int i = 0; i <= totalSteps; i++)
            {
                var x = i * angleIncrement;
                var y = Math.Sin(period * x) * amplitude;
                yield return new XYZ(x, y, 0);
            }
        }

        /// <summary>
        /// 在指定位置放置族实例
        /// </summary>
        private void PlaceFamilyInstance(XYZ location)
        {
            using (var transaction = new Transaction(_document, "放置族实例"))
            {
                transaction.Start();
                _document.Create.NewFamilyInstance(location, _familySymbol, DefaultStructuralType);
                transaction.Commit();
            }
        }
    }

    /// <summary>
    /// 绘制参数结构体 - 使用元组简化参数传递
    /// </summary>
    internal readonly struct PlottingParameters
    {
        public int Partitions { get; }
        public double Period { get; }
        public double Amplitude { get; }
        public double NumOfCircles { get; }

        public PlottingParameters(int partitions, double period, double amplitude, double numOfCircles)
        {
            Partitions = partitions;
            Period = period;
            Amplitude = amplitude;
            NumOfCircles = numOfCircles;
        }

        public void Deconstruct(out int partitions, out double period, out double amplitude, out double numOfCircles)
        {
            partitions = Partitions;
            period = Period;
            amplitude = Amplitude;
            numOfCircles = NumOfCircles;
        }
    }

    /// <summary>
    /// 应用程序参数配置类 - 示例实现，实际待替换
    /// </summary>
    internal static class Application2
    {
        // 族符号名称
        private const string FamilySymbolName = "MyFamilySymbol";

        // 默认参数
        private const int DefaultPartitions = 20;
        private const double DefaultPeriod = 1.0;
        private const double DefaultAmplitude = 5.0;
        private const double DefaultNumOfCircles = 2.0;

        public static string GetFamilySymbolName() => FamilySymbolName;

        public static PlottingParameters GetPlottingParameters() => new PlottingParameters(
            DefaultPartitions,
            DefaultPeriod,
            DefaultAmplitude,
            DefaultNumOfCircles
        );
    }
}
