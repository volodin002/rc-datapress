using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using RC.DataPress.Metamodel;
using System.Reflection;

namespace RC.DataPress.Emit
{
    class query_mapper : query_entity
    {
        private readonly IEntityType _entity;
        private readonly ILGenerator _gen;
        //private readonly bool _graph;
        Label _whileStartLabel;
        Label _whileEndLabel;

        public override IEntityType Entity => _entity;

        //query_field _key_field;
        //List<query_field> _fields;
        //public List<query_entity> entities;

        List<entity_cache> entity_caches;

        public query_mapper(IEntityType entity, ILGenerator gen, bool graph)
        {
            _entity = entity;
            _gen = gen;
            //_graph = graph;
            if (graph)
                entity_caches = new List<entity_cache>();
        }

        public void initMetamodel(DbDataReader rs)
        {
            if (entity_caches != null)
            {
                var cache = new entity_cache() { entity = _entity };
                entity_cache = cache;
                entity_caches.Add(cache);
            }
                
            var schema = rs.GetSchemaTable();
            var columns = schema.Columns;
            if (columns.Contains("ColumnName") && columns.Contains("ColumnOrdinal") && columns.Contains("AllowDBNull"))
            {
                foreach (var row in schema.Rows.OfType<DataRow>())
                {
                    string name = (string)row["ColumnName"];
                    int ordinal = (int)row["ColumnOrdinal"];

                    bool allowDBNull;
                    if (row["AllowDBNull"] is bool allow)
                        allowDBNull = allow;
                    else
                        allowDBNull = false;

                    var names = name.Split('.');
                    var data = new field_data(names, ordinal, allowDBNull, entity_caches);
                    field(0, ref data);
                }
            }
            else
            {
                int cnt = rs.FieldCount;
                for (int ordinal = 0; ordinal < cnt; ordinal++)
                {
                    string name = rs.GetName(ordinal);
                    var names = name.Split('.');
                    var data = new field_data(names, ordinal, true, entity_caches);
                    field(0, ref data);
                }
            }

        }
        
        public void emit()
        {
            LocalBuilder hashset_loc = null;
            if (entity_caches != null)
            {
                // if Entity caches will be used, define local variables for it
                var genericType = typeof(Dictionary<,>);
                foreach(var cache in entity_caches)
                {
                    var cache_entity = cache.entity;
                    var cache_type = genericType.MakeGenericType(new[] { cache_entity.KeyField.ValueType, cache_entity.EntityClass });

                    cache.dict = _gen.DeclareLocal(cache_type);
                    cache.obj = _gen.DeclareLocal(cache_entity.EntityClass);
                    EmitHelper.EmitNewObject(_gen, cache_type);
                }

                if(!entity_cache.single_field)
                {
                    genericType = typeof(HashSet<>);
                    var cache_type = genericType.MakeGenericType(new[] { _entity.KeyField.ValueType });
                    hashset_loc = _gen.DeclareLocal(cache_type);
                    EmitHelper.EmitNewObject(_gen, cache_type);
                }
            }

            var resultType = typeof(List<>).MakeGenericType(_entity.EntityClass);

            //[] -> [list]
            EmitHelper.EmitNewObject(_gen, resultType);

            beginWhile();


            if (entity_cache != null) // Entity Graph
            {
                _gen.Emit(OpCodes.Ldloc, entity_cache.dict); // => [..., dict]
                // read key 
                // [..., dict] => [..., dict, id]
                EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
                _gen.Emit(OpCodes.Ldloca, entity_cache.obj); // => [ ..., cache, id, &item]
                _gen.Emit(OpCodes.Callvirt, entity_cache.dict.LocalType.GetMethod("TryGetValue")); // => [ ..., dic.TryGetValue(id, out &item) => bool]

                var isInCache = _gen.DefineLabel();
                _gen.Emit(OpCodes.Brtrue, isInCache); // if in cache => skip object creation

                /* IF NOT IN CACHE */
                {
                    _gen.Emit(OpCodes.Dup); //  //[list] -> [list, list]

                    EmitHelper.EmitNewObject(_gen, _entity.EntityClass); // [...] -> [..., new T()]

                    // Add to the cache
                    {
                        _gen.Emit(OpCodes.Dup); // [list, list, T] => [list, list, T, T]
                        _gen.Emit(OpCodes.Stloc, entity_cache.obj); // [list, list, T, T] => [list, list, T]
                        EmitAddToCache(_gen, this); // [list, list, T] => [list, list, T]
                    }
                    setEntityFields(_gen, this, entity_caches); // [..., T] -> [..., T]

                    // Add to the HashSet
                    if (hashset_loc != null)
                    {
                        _gen.Emit(OpCodes.Ldloc, hashset_loc); // [...] => [..., HashSet]
                        // [..., HashSet] => [..., HashSet, id]
                        EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
                        _gen.Emit(OpCodes.Callvirt, hashset_loc.LocalType.GetMethod("Add")); // [..., HashSet, id] => [..., (bool)HashSet.Add(id)]
                        _gen.Emit(OpCodes.Pop); // [..., (bool)HashSet.Add(id)] => [...]
                    }

                    var addMethod = resultType.GetMethod("Add");
                    _gen.Emit(OpCodes.Callvirt, addMethod); // [..., list, list, T] -> [..., list]


                    // goto to the start of while(){} loop
                    _gen.Emit(OpCodes.Br, _whileStartLabel);
                }

                /* ITEM EXISTS IN CACHE */
                _gen.MarkLabel(isInCache);
                

                if (hashset_loc != null)
                {
                    _gen.Emit(OpCodes.Ldloc, hashset_loc); // [list] => [list, HashSet]
                    // [..., T, HashSet] => [list, HashSet, id]
                    EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
                    _gen.Emit(OpCodes.Callvirt, hashset_loc.LocalType.GetMethod("Add")); // [list, HashSet, id] => [list, (bool)HashSet.Add(id)]

                    var isInHashSet = _gen.DefineLabel();
                    _gen.Emit(OpCodes.Brfalse, isInHashSet); // [list, IsContains] => [ list ]

                    /* NOT EXISTS IN HashSet */
                    {
                        // Add item to the result List
                        _gen.Emit(OpCodes.Dup); // [list] => [list, list]
                        _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [list, list] => [list, list, item]
                        if (entity_cache.obj.LocalType != _entity.EntityClass)
                            _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [list, list, item] => [list, list, (T)item]

                        _gen.Emit(OpCodes.Callvirt, resultType.GetMethod("Add")); // [list, list, T] -> [list]

                        _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [list] => [list, item]
                        if (entity_cache.obj.LocalType != _entity.EntityClass)
                            _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [list, item] => [list, (T)item]

                        setEntitySimpleFields(_gen, this); // [..., T] => [..., T]
                    }
                    _gen.MarkLabel(isInHashSet);
                    /* EXISTS IN HashSet */
                    {
                        setEntityComplexFields(_gen, this, entity_caches); // [..., T] -> [..., T]
                    }
                }
                else
                {
                    _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [...] => [..., item]

                    if (entity_cache.obj.LocalType != _entity.EntityClass)
                        _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [..., item] => [..., (T)item]

                    //setEntitySimpleFields(_gen, this); // [..., T] -> [..., T]

                    setEntityComplexFields(_gen, this, entity_caches); // [..., T] -> [..., T]
                }

                _gen.Emit(OpCodes.Pop); // [..., T] -> [...]

                // try get entity from cache
                //entity_cache.
            }
            else // Not Entity Graph
            {
                _gen.Emit(OpCodes.Dup); //  //[list] -> [list, list]

                EmitHelper.EmitNewObject(_gen, _entity.EntityClass); // [...] -> [..., new T()]

                setEntityFields(_gen, this, entity_caches); // [..., T] -> [..., T]

                var addMethod = resultType.GetMethod("Add");
                _gen.Emit(OpCodes.Callvirt, addMethod); // [..., list, list, new T()] -> [..., list]
            }

            endWhile();

            _gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// emit: while(rs.next()) {
        /// stack: [...] -> [...]
        /// </summary>
        void beginWhile() 
        {
            _whileStartLabel = _gen.DefineLabel();
            _gen.MarkLabel(_whileStartLabel); // while {

            _gen.Emit(OpCodes.Ldarg_0); // [...] -> [..., r]

            _gen.Emit(OpCodes.Callvirt, EmitHelper.Reader.ReadMethod); //  [..., r] -> [ ..., r.Read()]

            _whileEndLabel = _gen.DefineLabel();
            // goto to the end of while(){} loop 
            _gen.Emit(OpCodes.Brfalse, _whileEndLabel); // [ ..., r.Read()] -> [...]


        }

        /// <summary>
        /// emit:  } end while
        /// stack: [...] -> [...]
        /// </summary>
        void endWhile()
        {
            _gen.Emit(OpCodes.Br, _whileStartLabel); // goto to the start of while(){} loop
            _gen.MarkLabel(_whileEndLabel); // } end while
        }

    }

    class query_field
    {
        public IEntityField entityField;
        public int ordinal;
        public bool isNullable;
    }

    class query_entity_field : query_entity
    {
        public IEntityField entityField;

        public override IEntityType Entity => entityField.ValueEntityType;
    }
    abstract class query_entity
    {

        public query_field key_field;
        public query_field type_field;
        public List<query_field> fields;
        public List<query_entity_field> entities;

        public query_entity parent;
        public entity_cache entity_cache;

        public abstract IEntityType Entity { get; }

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
            IEntityField field = Entity.getField(name);
            if (field == null) return;

            if (field.ValueEntityType != null) // it's an entity field
            {
                query_entity_field entity_field = null;
                if (entities == null) entities = new List<query_entity_field>();
                else
                {
                    entity_field = entities.FirstOrDefault(e => e.Entity == field.ValueEntityType);
                }

                if (entity_field == null)
                {
                    entity_field = new query_entity_field() { entityField = field, parent = this };
                    entities.Add(entity_field);

                    if (data.entity_caches != null) // graph mapping 
                    {
                        var entity = entity_field.Entity;
                        var cache = data.entity_caches
                            .FirstOrDefault(c => c.entity == entity || entity.isBaseEntity(c.entity));
                        if (cache == null)
                        {
                            cache = data.entity_caches.FirstOrDefault(c => c.entity.isBaseEntity(entity));
                            if (cache != null)
                            {
                                cache.entity = entity;
                            }
                            cache.single_field = false;
                        }
                        if (cache == null)
                        {
                            cache = new entity_cache() { entity = entity };
                            data.entity_caches.Add(cache);

                            /*
                            if (field.isCollection && field.isUniqueByKey)
                            {
                                if (cache.parent_ids_cache == null) cache.parent_ids_cache = new List<query_entity>();
                                cache.parent_ids_cache.Add(entity_field);
                            }
                            */

                        }

                        entity_field.entity_cache = cache;
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

        //public static void readFieldValue(ILGenerator gen, query_field field)
        //{

        //}

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

        public static void setEntityComplexFields(ILGenerator gen, query_entity entity, List<entity_cache> entity_caches)
        {
            if (entity.entities == null) return;

            if (entity_caches == null)
                setEntityComplexFields_ForNotGraph(gen, entity);
            else
                setEntityComplexFields_ForGraph(gen, entity, entity_caches);
        }

        public static void setEntityComplexFields_ForGraph(ILGenerator gen, query_entity entity, List<entity_cache> entity_caches)
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

                EmitHelper.EmitNewObject(gen, child_entity.Entity.EntityClass); // [...] -> [..., new E()]
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
            

        public static void setEntityFields(ILGenerator gen, query_entity entity, List<entity_cache> entity_caches)
        {
            setEntitySimpleFields(gen, entity);

            setEntityComplexFields(gen, entity, entity_caches);
        }

        public static void EmitAddToCache(ILGenerator gen, query_entity entity)
        {
            // => [ ... ]
            entity_cache locals = entity.entity_cache;
            query_field id_field = entity.key_field;

            gen.Emit(OpCodes.Ldloc, locals.dict); // [...] => [..., dict]

            // [...] => [..., dict, id]
            EmitHelper.EmitReadValue(gen, id_field.entityField.ValueType, id_field.ordinal);

            //EmitHelper.EmitNewObject(gen, entity.Entity.EntityClass); // [..., dict, id] -> [..., dict, id, new T()]

            gen.Emit(OpCodes.Ldloc, locals.obj); // => [ ..., dict, id, T]
            gen.Emit(OpCodes.Callvirt, locals.dict.LocalType.GetMethod("Add")); // => [ ... ]
        }
    }

    class entity_cache
    {
        //public enum CACHE_TYPE
        //{
        //    GLOBAL // Dictionary<TKey, TEntity>
        //}
        // cache local variable

        // local variable with cache Dictionary
        public LocalBuilder dict;
        // lokal variable for cache Dictionary TryGetValue out parameter
        public LocalBuilder obj;

        public IEntityType entity;

        public bool single_field = true;

        //public List<query_entity> parent_ids_cache; 
        //public CACHE_TYPE cache_type;

    }

    struct field_data
    {
        public string[] names;
        public int ordinal;
        public bool allowDBNull;
        public List<entity_cache> entity_caches;

        public field_data(string[] names, int ordinal, bool allowDBNull, List<entity_cache> entity_caches)
        {
            this.names = names;
            this.ordinal = ordinal;
            this.allowDBNull = allowDBNull;
            this.entity_caches = entity_caches;
        }

    }
}
