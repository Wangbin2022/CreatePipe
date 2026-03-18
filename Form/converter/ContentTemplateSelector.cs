using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CreatePipe.Form.Converter
{
    // 内容模板选择器
    public class ContentTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ImageSource)
            {
                return new DataTemplate
                {
                    VisualTree = new FrameworkElementFactory(typeof(Image))
                };
            }
            return null;
        }
    }
}
