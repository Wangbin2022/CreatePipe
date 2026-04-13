using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converters
{
    public class LastMEPSystemTextColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ✅ 支持管道和风管两种类型
            MEPSystemType mepSystem = value as MEPSystemType;
            if (mepSystem == null) return new SolidColorBrush(Colors.Black);

            bool isLastSystem = IsLastSystemOfClassification(mepSystem);
            return isLastSystem
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判断该系统类型是否是其分类下的最后一个（唯一一个）
        /// </summary>
        private bool IsLastSystemOfClassification(MEPSystemType systemType)
        {
            Document doc = systemType.Document;

            // ✅ 获取系统分类
            var classification = systemType.SystemClassification;

            // ✅ 根据系统类型选择正确的过滤器
            Type systemTypeClass = systemType is PipingSystemType
                ? typeof(PipingSystemType)
                : typeof(MechanicalSystemType);

            // ✅ 统计同分类的系统数量
            int sameClassificationCount = new FilteredElementCollector(doc)
                .OfClass(systemTypeClass)
                .Cast<MEPSystemType>()
                .Count(s => s.SystemClassification == classification);

            // 如果只有 1 个，说明是最后一个
            return sameClassificationCount <= 1;
        }
    }
}
