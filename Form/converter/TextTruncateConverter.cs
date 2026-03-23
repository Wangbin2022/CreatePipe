using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CreatePipe.Form.Converter
{
    /// <summary>
    /// 文本截断转换器 - 当文本超过指定长度时显示为...
    /// </summary>
    public class TextTruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 处理空值
            if (value == null) return "NONE";
            // 2. 如果是字符串：执行截断逻辑
            if (value is string text)
            {
                int maxLength = 0;
                if (parameter != null && int.TryParse(parameter.ToString(), out int p))
                    maxLength = p;
                if (text.Length <= maxLength) return text;
                return text.Substring(0, maxLength) + "...";
            }
            // 3. 如果是图片源 (BitmapSource, DrawingImage 等)
            // 从剪贴板读取的图片通常是 BitmapSource
            if (value is ImageSource)
            {
                return value; // 原样返回，XAML 中会用 DataTemplate 处理
            }
            // 4. 如果是已经封装好的 UI 元素 (比如已经在代码里写好的 <Image />)
            if (value is UIElement)
            {
                return value;
            }
            // 5. 其他无法识别的复杂对象
            return "未识别内容";
        }
        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    // 1. 处理空值
        //    if (value == null) return string.Empty;
        //    // 2. 如果传入的不是字符串（比如传入了一个 Image 对象），不进行截断，直接返回原对象
        //    // 这样可以保证你的按钮如果以后要放图标，也不会崩溃
        //    if (!(value is string text)) return value;
        //    // 3. 获取长度参数
        //    int maxLength = 0;
        //    //if (parameter is int) TaskDialog.Show("tt", parameter.ToString());
        //    if (parameter != null && int.TryParse(parameter.ToString(), out int p))
        //    {
        //        maxLength = p;
        //    }
        //    // 4. 执行截断
        //    if (text.Length <= maxLength) return text;
        //    return text.Substring(0, maxLength) + "...";
        //}
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
