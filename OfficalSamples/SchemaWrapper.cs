using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreatePipe.OfficalSamples
{
    //扩展存储工具类
    /// <summary>
    /// Schema 字段定义
    /// </summary>
    [Serializable]
    public class FieldDefinition
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public UnitType UnitType { get; set; }
        public ContainerType ContainerType { get; set; }
        public string KeyTypeName { get; set; }
        public SchemaWrapper SubSchema { get; set; }

        public override string ToString()
        {
            return ContainerType == ContainerType.Simple
                ? $"{Name}: {TypeName}"
                : $"{Name}: {ContainerType}<{KeyTypeName ?? TypeName}, {TypeName}>";
        }
    }

    /// <summary>
    /// Schema 元数据
    /// </summary>
    [Serializable]
    public class SchemaMetadata
    {
        public string SchemaId { get; set; }
        public string Name { get; set; }
        public string Documentation { get; set; }
        public string VendorId { get; set; }
        public string ApplicationId { get; set; }
        public AccessLevel ReadAccess { get; set; }
        public AccessLevel WriteAccess { get; set; }
        public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
    }

    /// <summary>
    /// Schema 包装器 - 简化 Revit 可扩展存储的操作
    /// </summary>
    [Serializable]
    public class SchemaWrapper
    {
        private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

        [NonSerialized]
        private Schema _schema;

        [NonSerialized]
        private Assembly _revitAssembly;

        [NonSerialized]
        private string _xmlPath;

        /// <summary>
        /// 序列化使用的无参构造函数
        /// </summary>
        internal SchemaWrapper() { }

        /// <summary>
        /// 创建新 Schema
        /// </summary>
        public static SchemaWrapper Create(Guid schemaId, AccessLevel readAccess, AccessLevel writeAccess,
            string vendorId, string applicationId, string name, string description)
        {
            return new SchemaWrapper
            {
                Metadata = new SchemaMetadata
                {
                    SchemaId = schemaId.ToString(),
                    Name = name,
                    Documentation = description,
                    VendorId = vendorId,
                    ApplicationId = applicationId,
                    ReadAccess = readAccess,
                    WriteAccess = writeAccess
                }
            };
        }

        /// <summary>
        /// 从现有 Schema 创建包装器
        /// </summary>
        public static SchemaWrapper FromSchema(Schema schema)
        {
            var wrapper = new SchemaWrapper
            {
                Metadata = new SchemaMetadata
                {
                    SchemaId = schema.GUID.ToString(),
                    Name = schema.SchemaName,
                    Documentation = schema.Documentation,
                    VendorId = schema.VendorId,
                    ApplicationId = schema.ApplicationGUID.ToString(),
                    ReadAccess = schema.ReadAccessLevel,
                    WriteAccess = schema.WriteAccessLevel
                },
                _schema = schema
            };

            foreach (var field in schema.ListFields())
            {
                wrapper.AddFieldFromExisting(field);
            }

            return wrapper;
        }

        /// <summary>
        /// 从 XML 文件创建包装器
        /// </summary>
        public static SchemaWrapper FromXml(string xmlPath)
        {
            var serializer = new XmlSerializer(typeof(SchemaWrapper));

            using (var stream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read))
            {
                var wrapper = serializer.Deserialize(stream) as SchemaWrapper;
                wrapper._xmlPath = xmlPath;
                wrapper?.InitializeRevitAssembly();
                wrapper?.FinishSchema();
                return wrapper;
            }
        }

        /// <summary>
        /// 添加字段
        /// </summary>
        public void AddField<T>(string name, UnitType unitType = UnitType.UT_Undefined, SchemaWrapper subSchema = null)
        {
            var fieldType = typeof(T);
            var isGeneric = fieldType.IsGenericType;

            var definition = new FieldDefinition
            {
                Name = name,
                TypeName = GetTypeName(fieldType),
                UnitType = unitType,
                ContainerType = GetContainerType(fieldType),
                SubSchema = subSchema
            };

            if (isGeneric && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var genericArgs = fieldType.GetGenericArguments();
                definition.KeyTypeName = GetTypeName(genericArgs[0]);
                definition.TypeName = GetTypeName(genericArgs[1]);
            }

            Metadata.Fields.Add(definition);
        }

        /// <summary>
        /// 从现有字段添加（用于加载已有 Schema）
        /// </summary>
        private void AddFieldFromExisting(Field field)
        {
            var definition = new FieldDefinition
            {
                Name = field.FieldName,
                TypeName = GetTypeName(field.ValueType),
                UnitType = field.UnitType,
                ContainerType = field.ContainerType
            };

            if (field.ContainerType == ContainerType.Map)
            {
                definition.KeyTypeName = GetTypeName(field.KeyType);
            }

            if (field.ValueType == typeof(Entity) && field.SubSchemaGUID != Guid.Empty)
            {
                var subSchema = Schema.Lookup(field.SubSchemaGUID);
                if (subSchema != null)
                {
                    definition.SubSchema = FromSchema(subSchema);
                }
            }

            Metadata.Fields.Add(definition);
        }

        /// <summary>
        /// 完成 Schema 创建（生成 Revit Schema 对象）
        /// </summary>
        public void FinishSchema()
        {
            var builder = new SchemaBuilder(new Guid(Metadata.SchemaId));

            foreach (var field in Metadata.Fields)
            {
                var fieldBuilder = CreateFieldBuilder(builder, field);
                ConfigureFieldBuilder(fieldBuilder, field);
            }

            // 设置 Schema 属性
            builder.SetReadAccessLevel(Metadata.ReadAccess);
            builder.SetWriteAccessLevel(Metadata.WriteAccess);
            builder.SetVendorId(Metadata.VendorId);
            builder.SetApplicationGUID(new Guid(Metadata.ApplicationId));
            builder.SetDocumentation(Metadata.Documentation);
            builder.SetSchemaName(Metadata.Name);

            _schema = builder.Finish();
        }

        /// <summary>
        /// 创建字段构建器
        /// </summary>
        private FieldBuilder CreateFieldBuilder(SchemaBuilder builder, FieldDefinition field)
        {
            var fieldType = GetFieldType(field);

            switch (field.ContainerType)
            {
                case ContainerType.Simple:
                    return builder.AddSimpleField(field.Name, fieldType);
                case ContainerType.Array:
                    return builder.AddArrayField(field.Name, fieldType);
                case ContainerType.Map:
                    return builder.AddMapField(field.Name, GetKeyType(field), fieldType);
                default:
                    throw new NotSupportedException($"不支持的容器类型: {field.ContainerType}");
            }
        }

        /// <summary>
        /// 配置字段构建器（单位类型、子 Schema 等）
        /// </summary>
        private void ConfigureFieldBuilder(FieldBuilder builder, FieldDefinition field)
        {
            if (field.UnitType != UnitType.UT_Undefined)
            {
                builder.SetUnitType(field.UnitType);
            }

            if (field.SubSchema != null)
            {
                builder.SetSubSchemaGUID(new Guid(field.SubSchema.Metadata.SchemaId));
                field.SubSchema.FinishSchema();
            }
        }

        /// <summary>
        /// 导出到 XML 文件
        /// </summary>
        public void ToXml(string xmlPath)
        {
            var serializer = new XmlSerializer(typeof(SchemaWrapper));
            using (var stream = new FileStream(xmlPath, FileMode.Create, FileAccess.Write))
            {
                serializer.Serialize(stream, this);
            }
            _xmlPath = xmlPath;
        }

        /// <summary>
        /// 获取 Schema 中所有字段的数据（字符串形式）
        /// </summary>
        public string GetEntityData(Entity entity)
        {
            if (entity == null || !entity.IsValid())
                return "无有效 Entity 数据";

            var builder = new StringBuilder();
            ExtractEntityData(entity, _schema ?? entity.Schema, builder, 0);
            return builder.ToString();
        }

        /// <summary>
        /// 递归提取 Entity 数据
        /// </summary>
        private void ExtractEntityData(Entity entity, Schema schema, StringBuilder builder, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);

            builder.AppendLine($"{indent}Schema: {schema.SchemaName} (ID: {schema.GUID})");

            foreach (var field in schema.ListFields())
            {
                ExtractFieldData(entity, field, builder, indentLevel + 1);
            }

            builder.AppendLine($"{indent}---");
        }

        /// <summary>
        /// 提取单个字段的数据
        /// </summary>
        private void ExtractFieldData(Entity entity, Field field, StringBuilder builder, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);
            var value = GetFieldValue(entity, field);

            builder.Append($"{indent}{field.FieldName} [{field.ContainerType}]");

            switch (field.ContainerType)
            {
                case ContainerType.Simple:
                    builder.AppendLine($" = {FormatValue(value)}");
                    break;

                case ContainerType.Array:
                    builder.AppendLine($" = [数组 共 {GetCollectionCount(value)} 项]");
                    ExtractCollectionData(value as IEnumerable, builder, indentLevel + 1, field);
                    break;

                case ContainerType.Map:
                    builder.AppendLine($" = [字典 共 {GetCollectionCount(value)} 项]");
                    ExtractDictionaryData(value as IDictionary, builder, indentLevel + 1, field);
                    break;
            }
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        private object GetFieldValue(Entity entity, Field field)
        {
            try
            {
                if (field.UnitType != UnitType.UT_Undefined)
                {
                    var method = entity.GetType().GetMethod("Get", new[] { typeof(Field), typeof(DisplayUnitType) });
                    return method?.MakeGenericMethod(field.ValueType)
                        .Invoke(entity, new object[] { field, DisplayUnitType.DUT_METERS });
                }
                else
                {
                    var method = entity.GetType().GetMethod("Get", new[] { typeof(Field) });
                    return method?.MakeGenericMethod(field.ValueType)
                        .Invoke(entity, new[] { field });
                }
            }
            catch
            {
                return "<无法读取>";
            }
        }

        /// <summary>
        /// 提取集合数据（数组/列表）
        /// </summary>
        private void ExtractCollectionData(IEnumerable collection, StringBuilder builder, int indentLevel, Field field)
        {
            if (collection == null) return;

            var indent = new string(' ', indentLevel * 2);
            var index = 0;

            foreach (var item in collection)
            {
                if (item is Entity subEntity)
                {
                    builder.AppendLine($"{indent}[{index}] = 子实体");
                    ExtractEntityData(subEntity, Schema.Lookup(field.SubSchemaGUID), builder, indentLevel + 1);
                }
                else
                {
                    builder.AppendLine($"{indent}[{index}] = {FormatValue(item)}");
                }
                index++;
            }
        }

        /// <summary>
        /// 提取字典数据
        /// </summary>
        private void ExtractDictionaryData(IDictionary dictionary, StringBuilder builder, int indentLevel, Field field)
        {
            if (dictionary == null) return;

            var indent = new string(' ', indentLevel * 2);

            foreach (DictionaryEntry entry in dictionary)
            {
                builder.AppendLine($"{indent}[{entry.Key}] =");

                if (entry.Value is Entity subEntity)
                {
                    ExtractEntityData(subEntity, Schema.Lookup(field.SubSchemaGUID), builder, indentLevel + 1);
                }
                else
                {
                    builder.AppendLine($"{indent}  {FormatValue(entry.Value)}");
                }
            }
        }

        /// <summary>
        /// 格式化值显示
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "<null>";
            }
            else if (value is Entity)
            {
                return "<子实体>";
            }
            else if (value is XYZ)
            {
                var xyz = (XYZ)value;
                return $"({xyz.X:F2}, {xyz.Y:F2}, {xyz.Z:F2})";
            }
            else if (value is UV)
            {
                var uv = (UV)value;
                return $"({uv.U:F2}, {uv.V:F2})";
            }
            else if (value is ElementId)
            {
                var id = (ElementId)value;
                return $"ElementId:{id.IntegerValue}";
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// 获取集合元素数量
        /// </summary>
        private static int GetCollectionCount(object collection)
        {
            if (collection is IList)
            {
                var list = (IList)collection;
                return list.Count;
            }
            else if (collection is ICollection)
            {
                var coll = (ICollection)collection;
                return coll.Count;
            }
            else if (collection is IEnumerable)
            {
                var enumerable = (IEnumerable)collection;
                return enumerable.Cast<object>().Count();
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取字段类型
        /// </summary>
        private Type GetFieldType(FieldDefinition field)
        {
            var typeName = field.TypeName;

            // 处理泛型类型
            if (field.ContainerType == ContainerType.Array)
                return typeof(IList<>).MakeGenericType(GetTypeFromName(typeName));

            if (field.ContainerType == ContainerType.Map)
                return typeof(IDictionary<,>).MakeGenericType(GetTypeFromName(field.KeyTypeName), GetTypeFromName(typeName));

            return GetTypeFromName(typeName);
        }

        /// <summary>
        /// 获取键类型（用于字典）
        /// </summary>
        private Type GetKeyType(FieldDefinition field)
        {
            return GetTypeFromName(field.KeyTypeName);
        }

        /// <summary>
        /// 从类型名称获取 Type 对象
        /// </summary>
        private Type GetTypeFromName(string typeName)
        {
            // 尝试从系统程序集获取
            var type = Type.GetType(typeName, false, true);
            if (type != null) return type;

            // 从 Revit API 程序集获取
            if (_revitAssembly == null) InitializeRevitAssembly();
            return _revitAssembly?.GetType(typeName) ?? typeof(object);
        }

        /// <summary>
        /// 获取类型的字符串表示
        /// </summary>
        private static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                if (genericType == typeof(IList<>))
                    return $"{typeof(IList<>).FullName}[[{GetTypeName(genericArgs[0])}]]";

                if (genericType == typeof(IDictionary<,>))
                    return $"{typeof(IDictionary<,>).FullName}[[{GetTypeName(genericArgs[0])}],[{GetTypeName(genericArgs[1])}]]";
            }

            return type.FullName ?? type.Name;
        }

        /// <summary>
        /// 获取容器类型
        /// </summary>
        private static ContainerType GetContainerType(Type type)
        {
            if (!type.IsGenericType) return ContainerType.Simple;

            var genericType = type.GetGenericTypeDefinition();

            if (genericType == typeof(IList<>))
                return ContainerType.Array;

            if (genericType == typeof(IDictionary<,>))
                return ContainerType.Map;

            return ContainerType.Simple;
        }

        /// <summary>
        /// 初始化 Revit 程序集引用
        /// </summary>
        private void InitializeRevitAssembly()
        {
            _revitAssembly = Assembly.GetAssembly(typeof(XYZ));
        }

        #region 属性

        public SchemaMetadata Metadata { get; set; } = new SchemaMetadata();

        public Schema GetSchema() => _schema;

        public string GetXmlPath() => _xmlPath;

        public void SetXmlPath(string path) => _xmlPath = path;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Schema: {Metadata.Name}");
            builder.AppendLine($"  ID: {Metadata.SchemaId}");
            builder.AppendLine($"  描述: {Metadata.Documentation}");
            builder.AppendLine($"  供应商: {Metadata.VendorId}");
            builder.AppendLine($"  读取权限: {Metadata.ReadAccess}");
            builder.AppendLine($"  写入权限: {Metadata.WriteAccess}");
            builder.AppendLine($"  字段数量: {Metadata.Fields.Count}");

            foreach (var field in Metadata.Fields)
            {
                builder.AppendLine($"    - {field}");
            }

            return builder.ToString();
        }

        #endregion
    }
}
