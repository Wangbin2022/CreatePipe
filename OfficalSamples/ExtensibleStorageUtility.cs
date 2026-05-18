using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 可扩展存储工具类
    /// 提供查询文档中扩展存储数据的辅助方法
    /// </summary>
    public static class StorageUtility
    {
        //原始方法比DS转译后正常很多
        /// <summary>
        /// Returns true if any extensible storage exists in the document, false otherwise.
        /// </summary>
        public static bool DoesAnyStorageExist(Document doc)
        {
            IList<Schema> schemas = Schema.ListSchemas();
            if (schemas.Count == 0)
                return false;
            else
            {
                foreach (Schema schema in schemas)
                {
                    List<ElementId> ids = ElementsWithStorage(doc, schema);
                    if (ids.Count > 0)
                        return true;
                }
                return false;
            }
        }


        /// <summary>
        /// Returns a formatted string containing schema guids and element info for all elements
        /// containing extensible storage.
        /// </summary>
        public static string GetElementsWithAllSchemas(Document doc)
        {
            StringBuilder sBuilder = new StringBuilder();
            IList<Schema> schemas = Schema.ListSchemas();
            if (schemas.Count == 0)
                return "No schemas or storage.";
            else
            {
                foreach (Schema schema in schemas)
                {
                    sBuilder.Append(StorageUtility.GetElementsWithSchema(doc, schema));
                }
                return sBuilder.ToString();
            }
        }

        /// <summary>
        /// Returns a formatted string containing a schema guid and element info for all elements
        /// containing extensible storage of a given schema.
        /// </summary>
        private static string GetElementsWithSchema(Document doc, Schema schema)
        {
            StringBuilder sBuilder = new StringBuilder();
            sBuilder.AppendLine("Schema: " + schema.GUID.ToString() + ", " + schema.SchemaName);
            List<ElementId> elementsofSchema = ElementsWithStorage(doc, schema);
            if (elementsofSchema.Count == 0)
                sBuilder.AppendLine("No elements.");
            else
            {
                foreach (ElementId id in elementsofSchema)
                {
                    sBuilder.AppendLine(PrintElementInfo(id, doc));
                }
            }
            return sBuilder.ToString();
        }

        /// <summary>
        /// Returns a list of ElementIds that contain extensible storage of a given schema using
        /// the ExtensibleStorageFilter ElementQuickFilter.
        /// </summary>
        private static List<ElementId> ElementsWithStorage(Document doc, Schema schema)
        {
            List<ElementId> ids = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.WherePasses(new ExtensibleStorageFilter(schema.GUID));
            ids.AddRange(collector.ToElementIds());
            return ids;
        }

        /// <summary>
        /// Writes basic element info to a string.
        /// </summary>
        private static string PrintElementInfo(ElementId id, Document document)
        {
            Element element = document.GetElement(id);
            string retval = (element.Id.ToString() + ", " + element.Name + ", " + element.GetType().FullName);
            Debug.WriteLine(retval);
            return retval;
        }
        ///// <summary>
        ///// 检查文档中是否存在任何扩展存储数据
        ///// </summary>
        ///// <param name="doc">Revit 文档</param>
        ///// <returns>存在任何存储数据返回 true，否则返回 false</returns>
        //public static bool DoesAnyStorageExist(Document doc)
        //{
        //    if (doc == null) return false;
        //    var schemas = Schema.ListSchemas();
        //    if (schemas.Count == 0) return false;
        //    // 使用 LINQ 检查是否存在任何包含存储数据的元素
        //    return schemas.Any(schema => GetElementsWithStorage(doc, schema).Any());
        //}
        ///// <summary>
        ///// 获取文档中所有包含扩展存储的元素信息（格式化字符串）
        ///// </summary>
        ///// <param name="doc">Revit 文档</param>
        ///// <returns>格式化的存储信息字符串</returns>
        //public static string GetElementsWithAllSchemas(Document doc)
        //{
        //    if (doc == null) return "无效的文档引用";
        //    var schemas = Schema.ListSchemas();
        //    if (schemas.Count == 0) return "文档中没有 Schema 或存储数据。";
        //    var builder = new StringBuilder();
        //    foreach (var schema in schemas)
        //    {
        //        AppendSchemaInfo(builder, doc, schema);
        //    }
        //    return builder.ToString();
        //}
        ///// <summary>
        ///// 获取指定 Schema 的所有存储元素信息
        ///// </summary>
        //private static string GetElementsWithSchema(Document doc, Schema schema)
        //{
        //    var builder = new StringBuilder();
        //    AppendSchemaInfo(builder, doc, schema);
        //    return builder.ToString();
        //}
        ///// <summary>
        ///// 将 Schema 信息追加到 StringBuilder
        ///// </summary>
        //private static void AppendSchemaInfo(StringBuilder builder, Document doc, Schema schema)
        //{
        //    var elementIds = GetElementsWithStorage(doc, schema);
        //    builder.AppendLine($"Schema: {schema.GUID}, 名称: {schema.SchemaName}");
        //    if (elementIds.Count == 0)
        //    {
        //        builder.AppendLine("  无关联元素");
        //    }
        //    else
        //    {
        //        foreach (var id in elementIds)
        //        {
        //            builder.AppendLine($"  {GetElementInfo(id, doc)}");
        //        }
        //    }
        //}
        ///// <summary>
        ///// 获取包含指定 Schema 存储数据的元素 ID 列表
        ///// 使用 ExtensibleStorageFilter 进行高效过滤
        ///// </summary>
        //private static IList<ElementId> GetElementsWithStorage(Document doc, Schema schema)
        //{
        //    return new FilteredElementCollector(doc).WherePasses(new ExtensibleStorageFilter(schema.GUID)).ToElementIds();
        //}
        ///// <summary>
        ///// 获取元素的基本信息字符串
        ///// </summary>
        //private static string GetElementInfo(ElementId id, Document doc)
        //{
        //    var element = doc.GetElement(id);
        //    if (element == null) return $"ID: {id.IntegerValue} (元素不存在)";
        //    return $"ID: {element.Id.IntegerValue}, 名称: {element.Name ?? "未命名"}, 类型: {element.GetType().Name}";
        //}
    }
}
