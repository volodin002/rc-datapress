using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RC.DataPress.Collections;
using RC.DataPress.Metamodel;

namespace RC.DataPress.Emit
{
    class query_field
    {
        public IEntityField entityField;
        public int ordinal;
        public bool isNullable;
    }

    class query_entity_field : query_entity
    {
        public readonly IEntityField entityField;

        public query_entity_field(IEntityField entityField)
        {
            this.entityField = entityField;
            entity = entityField.ValueEntityType;
        }
    }
    abstract class query_entity
    {
        public query_field key_field;
        public query_field type_field;
        public List<query_field> fields;
        public List<query_entity_field> entities;

        public query_entity parent;
        public entity_locals entity_locals;

        public IEntityType entity;

        public query_entity()
        {
            fields = new List<query_field>();
        }

        public void field(int index, ref field_data data)
        {
            var name = data.names[index];
            if (name == "$type")
            {
                var qField = new query_field() { isNullable = data.allowDBNull, ordinal = data.ordinal };
                type_field = qField;
                return;
            }
            IEntityField field = entity.getField(name);
            if (field == null) return;

            if (field.ValueEntityType != null) // it's an entity field
            {
                query_entity_field entity_field = null;
                if (entities == null) entities = new List<query_entity_field>();
                else
                {
                    entity_field = entities.FirstOrDefault(e => e.entity == field.ValueEntityType);
                }

                if (entity_field == null)
                {
                    entity_field = new query_entity_field(field) { parent = this };
                    entities.Add(entity_field);

                    if (data.entity_locals != null) // graph mapping 
                    {
                        var entity = entity_field.entity;
                        var loclas = data.entity_locals
                            .FirstOrDefault(c => c.entity == entity || entity.isBaseEntity(c.entity));
                        if (loclas == null)
                        {
                            loclas = data.entity_locals.FirstOrDefault(c => c.entity.isBaseEntity(entity));
                            if (loclas != null)
                            {
                                loclas.entity = entity;
                            }
                            loclas.count++;
                        }
                        if (loclas == null)
                        {
                            loclas = new entity_locals() { entity = entity };
                            data.entity_locals.Add(loclas);

                            /*
                            if (field.isCollection && field.isUniqueByKey)
                            {
                                if (cache.parent_ids_cache == null) cache.parent_ids_cache = new List<query_entity>();
                                cache.parent_ids_cache.Add(entity_field);
                            }
                            */

                        }

                        entity_field.entity_locals = loclas;
                    }
                }
                if (++index < data.names.Length)
                    entity_field.field(index, ref data);
            }
            else // it's a simple field
            {
                var qField = new query_field() { entityField = field, isNullable = data.allowDBNull, ordinal = data.ordinal };
                if (field.isKey) key_field = qField;
                else fields.Add(qField);
            }

        }

        public static void setEntitySimpleField(ILGenerator gen, query_field field) // [..., T] => [..., T]
        {
            gen.Emit(OpCodes.Dup); // [..., T] -> [..., T, T]

            var entityField = field.entityField;
            // [..., T, T] -> [..., T, T, value]
            if (field.isNullable)
                EmitHelper.EmitReadValueWithDbNullCheck(gen, entityField.ValueType, field.ordinal);
            else
                EmitHelper.EmitReadValue(gen, entityField.ValueType, field.ordinal);

            if (entityField.isProperty)
            {
                var propInfo = (PropertyInfo)entityField.MemberInfo;
                var propSetter = propInfo.SetMethod;
                gen.Emit(OpCodes.Callvirt, propSetter);
            }
            else
            {
                gen.Emit(OpCodes.Stfld, (FieldInfo)entityField.MemberInfo);
            }
        }

        public static void getEntityField(ILGenerator gen, query_field field) // [..., T] => [..., T, T.Value]
        {
            gen.Emit(OpCodes.Dup); // [..., T] -> [..., T, T]

            var entityField = field.entityField;

            if (entityField.isProperty)
            {
                var propInfo = (PropertyInfo)entityField.MemberInfo;
                var propGetter = propInfo.GetMethod;
                gen.Emit(OpCodes.Callvirt, propGetter);
            }
            else
            {
                gen.Emit(OpCodes.Ldfld, (FieldInfo)entityField.MemberInfo);
            }
        }

        public static void setEntitySimpleFields(ILGenerator gen, query_entity entity) // [..., T] => [..., T]
        {
            if (entity.key_field != null)
                setEntitySimpleField(gen, entity.key_field);

            foreach (var field in entity.fields)
            {
                setEntitySimpleField(gen, field);
            }
        }

        public static void setEntityComplexFields(ILGenerator gen, query_entity entity, List<entity_locals> entity_locals)
        {
            if (entity.entities == null) return;

            if (entity_locals == null)
                setEntityComplexFields_ForNotGraph(gen, entity);
            else
                setEntityComplexFields_ForGraph(gen, entity, entity_locals);
        }

        public static void setEntityComplexFields_ForGraph(ILGenerator gen, query_entity entity, List<entity_locals> entity_locals)
        {
            foreach (var child_entity in entity.entities)
            {

            }
        }

        public static void setEntityComplexFields_ForNotGraph(ILGenerator gen, query_entity entity)
        {
            foreach (var child_entity in entity.entities)
            {
                gen.Emit(OpCodes.Dup); // [..., T] -> [..., T, T]

                var entityField = child_entity.entityField;
                var type = entityField.ValueType;

                Type collectionType = null;
                if (entityField.isCollection)
                {
                    collectionType = entityField.isSet ? typeof(HashSet<>).MakeGenericType(type) : typeof(List<>).MakeGenericType(type);
                    EmitHelper.EmitNewObject(gen, collectionType); // [...] -> [..., collection]
                    gen.Emit(OpCodes.Dup);                         // [..., collection] -> [..., collection, collection]
                }

                EmitHelper.EmitNewObject(gen, child_entity.entity.EntityClass); // [...] -> [..., new E()]
                setEntityFields(gen, child_entity, null); // [..., E] -> [..., E]

                if (collectionType != null)
                {
                    var addMethod = collectionType.GetMethod("Add");
                    gen.Emit(OpCodes.Callvirt, addMethod);

                    if (entityField.isSet)
                        gen.Emit(OpCodes.Pop);
                }

                if (entityField.isProperty)
                {
                    var setter = ((PropertyInfo)entityField.MemberInfo).SetMethod;
                    gen.Emit(OpCodes.Callvirt, setter);
                }
                else
                {
                    gen.Emit(OpCodes.Stfld, ((FieldInfo)entityField.MemberInfo));
                }
            }
        }


        public static void setEntityFields(ILGenerator gen, query_entity entity, List<entity_locals> entity_locals)
        {
            setEntitySimpleFields(gen, entity);

            setEntityComplexFields(gen, entity, entity_locals);
        }

        public static void EmitAddToCache(ILGenerator gen, query_entity entity)
        {
            // => [ ... ]
            entity_locals locals = entity.entity_locals;
            query_field id_field = entity.key_field;

            gen.Emit(OpCodes.Ldloc, locals.dict); // [...] => [..., dict]

            // [...] => [..., dict, id]
            EmitHelper.EmitReadValue(gen, id_field.entityField.ValueType, id_field.ordinal);

            //EmitHelper.EmitNewObject(gen, entity.Entity.EntityClass); // [..., dict, id] -> [..., dict, id, new T()]

            gen.Emit(OpCodes.Ldloc, locals.obj); // => [ ..., dict, id, T]
            gen.Emit(OpCodes.Callvirt, locals.dict.LocalType.GetMethod("Add")); // => [ ... ]
        }

        /*
        public static Type GetCollectionType(query_entity_field queryField)
        {
            Type collectionType;
            IEntityField entityField = queryField.entityField;
            if (entityField.isProperty)
            {
                collectionType = ((PropertyInfo)entityField.MemberInfo).PropertyType;   
            }
            else
            {
                collectionType = ((FieldInfo)entityField.MemberInfo).FieldType;
            }

            var entity = entityField.EntityType;
            Type entityType = entity.EntityClass;
            entity_cache cache = queryField.entity_cache;
            if(cache == null)
            {
                if (typeof(ISet<>).IsAssignableFrom(collectionType))
                    return typeof(HashSet<>).MakeGenericType(entityType);
                else
                    return typeof(List<>).MakeGenericType(entityType);

            }

            if (typeof(List<>).IsAssignableFrom(collectionType))
            {
                if (cache.count == 0)
                    return typeof(ListWithKeySet<,>).MakeGenericType(entity.KeyField.ValueType, entityType );
                else
                    return typeof(ListWithKeySet<,,>).MakeGenericType(entity.KeyField.ValueType, cache.obj.LocalType, entityType);
            }
        }
        */
    }

    class entity_locals
    {
        // local variable with cache Dictionary
        public LocalBuilder dict;

        // local variable for cache Dictionary TryGetValue out parameter
        public LocalBuilder obj;

        public IEntityType entity;

        public int count;
    }

    struct field_data
    {
        public string[] names;
        public int ordinal;
        public bool allowDBNull;
        public List<entity_locals> entity_locals;

        public field_data(string[] names, int ordinal, bool allowDBNull, List<entity_locals> all_locals)
        {
            this.names = names;
            this.ordinal = ordinal;
            this.allowDBNull = allowDBNull;
            this.entity_locals = all_locals;
        }

    }
}
