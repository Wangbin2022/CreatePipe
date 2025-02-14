using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.utils.Bean
{
    /// <summary>
    /// 属性设置
    /// </summary>
 public  class FieldEntity
    {/// <summary>
    /// 属性名
    /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 属性值
        /// </summary>
        public object FieldValue { get; set; }

        /// <summary>
        /// 定义成集合
        /// </summary>
        public List<FieldEntity> FieldEntities { get; set; }
    }
}
