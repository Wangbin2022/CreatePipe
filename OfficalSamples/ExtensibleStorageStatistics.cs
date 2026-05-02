using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe.OfficalSamples
{
    internal class ExtensibleStorageStatistics
    {
        public ExtensibleStorageStatistics(ExternalCommandData commandData)
        {
            var document = commandData.Application.ActiveUIDocument.Document;
            var schemas = Schema.ListSchemas();
            string message = string.Empty;
            if (schemas.Count == 0)
            {
                message = "文档中没有 Schema 定义。";
                TaskDialog.Show("存储统计", message);
                return;
            }
            var statistics = BuildStatistics(document, schemas);
            TaskDialog.Show("扩展存储统计", statistics);
        }
        /// <summary>
        /// 构建统计信息
        /// </summary>
        private static string BuildStatistics(Document doc, IList<Schema> schemas)
        {
            var builder = new StringBuilder();
            builder.AppendLine("========== 扩展存储统计信息 ==========");
            builder.AppendLine($"文档: {doc.Title}");
            builder.AppendLine($"Schema 总数: {schemas.Count}");
            builder.AppendLine();

            var totalElementCount = 0;

            foreach (var schema in schemas)
            {
                var elementIds = new FilteredElementCollector(doc)
                    .WherePasses(new ExtensibleStorageFilter(schema.GUID))
                    .ToElementIds();

                var elementCount = elementIds.Count;
                totalElementCount += elementCount;

                builder.AppendLine($"Schema: {schema.SchemaName}");
                builder.AppendLine($"  GUID: {schema.GUID}");
                builder.AppendLine($"  供应商: {schema.VendorId}");
                builder.AppendLine($"  应用 ID: {schema.ApplicationGUID}");
                builder.AppendLine($"  关联元素数: {elementCount}");
                builder.AppendLine($"  读取权限: {schema.ReadAccessLevel}");
                builder.AppendLine($"  写入权限: {schema.WriteAccessLevel}");
                builder.AppendLine();
            }

            builder.AppendLine($"========== 总计 ==========");
            builder.AppendLine($"总关联元素数: {totalElementCount}");

            return builder.ToString();
        }
    }
}
