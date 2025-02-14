using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.utils.Bean
{
    /// <summary>
    /// 扩展数据对象
    /// </summary>
   public class ExtendedDataEntity
    {
        public ExtendedDataEntity(string schemaName,string documentation, List<FieldEntity> fieldEntities) {
            this.SchemaName = schemaName;
            this.Documentation = documentation;
            this.FieldEntities = fieldEntities;
        }

        /// <summary>
        /// 框架名
        /// </summary>
        public string SchemaName { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// 属性设置集合
        /// </summary>
       public List<FieldEntity> FieldEntities { get; set; }

    }
}
