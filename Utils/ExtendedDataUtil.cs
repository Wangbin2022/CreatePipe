using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using RevitPro.utils.Bean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPro.utils
{
 public   class ExtendedDataUtil
    {
        /// <summary>
        /// 创建扩展数据 注意:调用需开启事务
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="extendedEntity">扩展数据对象</param>
        public static void WriteExtendedData(Element element, ExtendedDataEntity extendedEntity)
        {
            
            //查询是否有entity
            Schema schema1 = GetSchema(element, extendedEntity.SchemaName);
            if (schema1!=null)
            {
                element.DeleteEntity(schema1);
            }

          


            Guid guid = Guid.NewGuid();
            //建立一个框架 相当于类
          
            SchemaBuilder schemaBuilder = new SchemaBuilder(guid);
            //权限设置
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);
            //设置其它信息
            schemaBuilder.SetSchemaName(extendedEntity.SchemaName);
            schemaBuilder.SetDocumentation(extendedEntity.Documentation);
            //创建属性
            foreach (FieldEntity fieldEntity in extendedEntity.FieldEntities)
            {
                SetParameter(fieldEntity, schemaBuilder);

            }
            //FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("wallName", typeof(string));
            //FieldBuilder fieldBuilder2 = schemaBuilder.AddSimpleField("wallWidth", typeof(string));
            //FieldBuilder fieldBuilder3 = schemaBuilder.AddSimpleField("wallOther", typeof(string));
            //得到框架
            Schema schema = schemaBuilder.Finish();
            Entity entity = new Entity(schema);
            
            foreach (FieldEntity fieldEntity in extendedEntity.FieldEntities)
            {
                //Field field1 = schema.GetField(fieldEntity.FieldName);
                if (fieldEntity.FieldEntities!=null)
                {
                    entity.Set(fieldEntity.FieldName, fieldEntity.FieldValue as Entity);
                }
                else
                {
                    entity.Set(fieldEntity.FieldName, fieldEntity.FieldValue as string);
                }
              
               
            }

            //Field field = schema.GetField("wallName");
            //entity.Set(field, wall.Name);
            //Field field2 = schema.GetField("wallWidth");
            //entity.Set(field2, wall.Width.ToMillimeter() + "");
            //Field field3 = schema.GetField("wallOther");
            //entity.Set(field3, document.GetElement(wall.LevelId).Name);
            element.SetEntity(entity);

        }


        /// <summary>
        /// 读取 不用开事务
        /// </summary>
        /// <param name="element"></param>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public static List<FieldEntity> ReadExtendedData(Element element,string entityName)
        {
            List<FieldEntity> fieldEntities = new List<FieldEntity>();
               Schema schema = GetSchema(element, entityName);
            if (schema!=null)
            {
                Entity entity = element.GetEntity(schema);
                if (entity!=null)
                {
                    foreach (Field field in schema.ListFields())
                    {
                        FieldEntity fieldEntity = new FieldEntity();
                        fieldEntity.FieldName = field.FieldName;
                        if (field.SubSchema!=null)
                        {
                            var entity2 = entity.Get<Entity>(field) as Entity;
                            fieldEntity.FieldValue = entity2;
                            fieldEntity.FieldEntities = GetFields(entity2);


                        }
                        else
                        {
                            fieldEntity.FieldValue = entity.Get<string>(field);
                        }
                       
                        fieldEntities.Add(fieldEntity);
                    }
                   
                }
               



            }

            return fieldEntities;
        }
        public static List<FieldEntity> GetFields(Entity entity) {
            List<FieldEntity> fieldEntities = new List<FieldEntity>();
            Schema schema= entity.Schema;
            foreach (Field field in schema.ListFields()) {
                FieldEntity fieldEntity = new FieldEntity();
                fieldEntity.FieldName = field.FieldName;
                if (field.SubSchema != null)
                {
                    var entity2 = entity.Get<Entity>(field) as Entity;
                    fieldEntity.FieldValue = entity2;
                    fieldEntity.FieldEntities = GetFields(entity2);


                        }
                else
                {
                    fieldEntity.FieldValue = entity.Get<string>(field);
                }

                fieldEntities.Add(fieldEntity);
              
            }

            return fieldEntities;
        }

        /// <summary>
        /// 获得框架
        /// </summary>
        /// <param name="element"></param>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public static Schema GetSchema(Element element, string entityName) {
            IList<Guid> guids = element.GetEntitySchemaGuids();
            foreach (Guid guid in guids)
            {
                Schema schema1 = Schema.Lookup(guid);
                if (schema1 != null && entityName.Equals(schema1.SchemaName))
                {
                    return schema1;
                }
            }
            return null;
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="fieldEntity"></param>
        /// <param name="schemaBuilder"></param>
        public static void SetParameter(FieldEntity fieldEntity, SchemaBuilder schemaBuilder) {
            FieldBuilder fieldBuilder = null;

            if (fieldEntity.FieldEntities!=null)
            {
                fieldBuilder = schemaBuilder.AddSimpleField(fieldEntity.FieldName, typeof(Entity));

                //创建子类的属性
                Guid guid = Guid.NewGuid();
                //建立一个框架 相当于类
                fieldBuilder.SetSubSchemaGUID(guid);
                SchemaBuilder schemaBuilder2 = new SchemaBuilder(guid);
               
                //设置其它信息
                schemaBuilder2.SetSchemaName(fieldEntity.FieldName);
                //创建属性
                foreach (FieldEntity fieldEntity2 in fieldEntity.FieldEntities)
                {

                    SetParameter(fieldEntity2, schemaBuilder2);
                }
                Schema schema = schemaBuilder2.Finish();
                Entity entity = new Entity(schema);
                foreach (FieldEntity fieldEntity2 in fieldEntity.FieldEntities)
                {
                    if (fieldEntity2.FieldEntities!=null)
                    {
                        entity.Set(fieldEntity2.FieldName, fieldEntity2.FieldValue as Entity);
                    }
                    else
                    {
                        entity.Set(fieldEntity2.FieldName, fieldEntity2.FieldValue as string);
                    }
                   
                }


                //父类改成entity
                fieldEntity.FieldValue = entity;

            }
            else
            {
                fieldBuilder = schemaBuilder.AddSimpleField(fieldEntity.FieldName, typeof(string));
            }

          //  fieldBuilder.SetSubSchemaGUID()


        }
    }
}
