using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using RC.DataPress.Metamodel;
using System.Reflection;
using RC.DataPress.Collections;

namespace RC.DataPress.Emit
{
    enum QUERY_MAPPER_TYPE
    {
        AUTO,
        SIMPLE,
        GRAPH
    }
    class query_mapper : query_entity
    {
        
        private readonly ILGenerator _gen;
        //private readonly bool _graph;
        Label _whileStartLabel;
        Label _whileEndLabel;

        //public override IEntityType Entity => _entity;


        public List<entity_locals> all_locals;

        public query_mapper(IEntityType entity, ILGenerator gen)
        {
            this.entity = entity;
            _gen = gen;
        }

        public void initMetamodel(DbDataReader rs)
        {
            if (all_locals != null)
            {
                var locals = new entity_locals() { entity = this.entity };
                entity_locals = locals;
                all_locals.Add(locals);
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
                    var data = new field_data(names, ordinal, allowDBNull, all_locals);
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
                    var data = new field_data(names, ordinal, true, all_locals);
                    field(0, ref data);
                }
            }

        }

        public void initMetamodel(DbDataReader rs, QUERY_MAPPER_TYPE type)
        {
            switch(type)
            {
                case QUERY_MAPPER_TYPE.AUTO:
                case QUERY_MAPPER_TYPE.GRAPH:
                    all_locals = new List<entity_locals>();
                    break;
                case QUERY_MAPPER_TYPE.SIMPLE:
                    all_locals = null;
                    break;
            }

            initMetamodel(rs);

            if (type == QUERY_MAPPER_TYPE.AUTO)
            {
                if (entities == null || entities.Count == 0)
                    all_locals = null;
            }
        }

        public void emit()
        {
            if (all_locals == null)
                emitNotGraph();
            else
                emitGrapth();
        }

        public void emitNotGraph()
        {
            var resultType = typeof(List<>).MakeGenericType(entity.EntityClass);

            // [] -> [list]
            EmitHelper.EmitNewObject(_gen, resultType);

            beginWhile();

            _gen.Emit(OpCodes.Dup); //  //[list] -> [list, list]

            EmitHelper.EmitNewObject(_gen, entity.EntityClass); // [...] -> [..., new T()]

            setEntitySimpleFields(_gen, this); // [..., T] -> [..., T]

            setEntityComplexFields_ForNotGraph(_gen, this); // [..., T] -> [..., T]

            var addMethod = resultType.GetMethod("Add");
            _gen.Emit(OpCodes.Callvirt, addMethod); // [..., list, list, new T()] -> [..., list]

            endWhile();

            _gen.Emit(OpCodes.Ret);
        }

        public void emitGrapth()
        {
            var genericType = typeof(Dictionary<,>);
            if (entity_locals.count > 0)
            {
                var cache_entity = entity_locals.entity;
                var cache_type = genericType.MakeGenericType(entity.KeyField.ValueType, cache_entity.EntityClass);
                entity_locals.dict = _gen.DeclareLocal(cache_type);
                entity_locals.obj = _gen.DeclareLocal(cache_entity.EntityClass);
                EmitHelper.EmitNewObject(_gen, cache_type);
            }
            foreach (var cache in all_locals.Where(c => c != entity_locals))
            {
                var cache_entity = cache.entity;
                var cache_type = genericType.MakeGenericType(cache_entity.KeyField.ValueType, cache_entity.EntityClass);

                cache.dict = _gen.DeclareLocal(cache_type);
                cache.obj = _gen.DeclareLocal(cache_entity.EntityClass);
                EmitHelper.EmitNewObject(_gen, cache_type);
            }
            Type resultType;
            if (entity_locals.count > 0)
                resultType = typeof(ResizableArrayWithKeySet<,,>).MakeGenericType(entity.KeyField.ValueType, entity_locals.obj.LocalType, entity.EntityClass);
            else
                resultType = typeof(ResizableArrayWithKeySet<,>).MakeGenericType(entity.KeyField.ValueType, entity.EntityClass);

            //[] -> [list]
            EmitHelper.EmitNewObject(_gen, resultType);

            beginWhile();



            endWhile();

            _gen.Emit(OpCodes.Ret);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            //LocalBuilder hashset_loc = null;
            
            //// if Entity caches will be used, define local variables for it
            
            //foreach(var cache in entity_caches)
            //{
            //    var cache_entity = cache.entity;
            //    var cache_type = genericType.MakeGenericType(new[] { cache_entity.KeyField.ValueType, cache_entity.EntityClass });

            //    cache.dict = _gen.DeclareLocal(cache_type);
            //    cache.obj = _gen.DeclareLocal(cache_entity.EntityClass);
            //    EmitHelper.EmitNewObject(_gen, cache_type);
            //}

            //if(entity_cache.count > 0)
            //{
            //    genericType = typeof(HashSet<>);
            //    var cache_type = genericType.MakeGenericType(new[] { _entity.KeyField.ValueType });
            //    hashset_loc = _gen.DeclareLocal(cache_type);
            //    EmitHelper.EmitNewObject(_gen, cache_type);
            //}
            

            //var resultType = typeof(List<>).MakeGenericType(_entity.EntityClass);

            ////[] -> [list]
            //EmitHelper.EmitNewObject(_gen, resultType);

            //beginWhile();

            //_gen.Emit(OpCodes.Ldloc, entity_cache.dict); // => [..., dict]
            //// read key 
            //// [..., dict] => [..., dict, id]
            //EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
            //_gen.Emit(OpCodes.Ldloca, entity_cache.obj); // => [ ..., cache, id, &item]
            //_gen.Emit(OpCodes.Callvirt, entity_cache.dict.LocalType.GetMethod("TryGetValue")); // => [ ..., dic.TryGetValue(id, out &item) => bool]

            //var isInCache = _gen.DefineLabel();
            //_gen.Emit(OpCodes.Brtrue, isInCache); // if in cache => skip object creation

            ///* IF NOT IN CACHE */
            //{
            //    _gen.Emit(OpCodes.Dup); //  //[list] -> [list, list]

            //    EmitHelper.EmitNewObject(_gen, _entity.EntityClass); // [...] -> [..., new T()]

            //    // Add to the cache
            //    {
            //        _gen.Emit(OpCodes.Dup); // [list, list, T] => [list, list, T, T]
            //        _gen.Emit(OpCodes.Stloc, entity_cache.obj); // [list, list, T, T] => [list, list, T]
            //        EmitAddToCache(_gen, this); // [list, list, T] => [list, list, T]
            //    }
            //    setEntityFields(_gen, this, entity_caches); // [..., T] -> [..., T]

            //    // Add to the HashSet
            //    if (hashset_loc != null)
            //    {
            //        _gen.Emit(OpCodes.Ldloc, hashset_loc); // [...] => [..., HashSet]
            //        // [..., HashSet] => [..., HashSet, id]
            //        EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
            //        _gen.Emit(OpCodes.Callvirt, hashset_loc.LocalType.GetMethod("Add")); // [..., HashSet, id] => [..., (bool)HashSet.Add(id)]
            //        _gen.Emit(OpCodes.Pop); // [..., (bool)HashSet.Add(id)] => [...]
            //    }

            //    var addMethod = resultType.GetMethod("Add");
            //    _gen.Emit(OpCodes.Callvirt, addMethod); // [..., list, list, T] -> [..., list]


            //    // goto to the start of while(){} loop
            //    _gen.Emit(OpCodes.Br, _whileStartLabel);
            //}

            ///* ITEM EXISTS IN CACHE */
            //_gen.MarkLabel(isInCache);
                

            //if (hashset_loc != null)
            //{
            //    _gen.Emit(OpCodes.Ldloc, hashset_loc); // [list] => [list, HashSet]
            //    // [..., T, HashSet] => [list, HashSet, id]
            //    EmitHelper.EmitReadValue(_gen, key_field.entityField.ValueType, key_field.ordinal);
            //    _gen.Emit(OpCodes.Callvirt, hashset_loc.LocalType.GetMethod("Add")); // [list, HashSet, id] => [list, (bool)HashSet.Add(id)]

            //    var isInHashSet = _gen.DefineLabel();
            //    _gen.Emit(OpCodes.Brfalse, isInHashSet); // [list, IsContains] => [ list ]

            //    /* NOT EXISTS IN HashSet */
            //    {
            //        // Add item to the result List
            //        _gen.Emit(OpCodes.Dup); // [list] => [list, list]
            //        _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [list, list] => [list, list, item]
            //        if (entity_cache.obj.LocalType != _entity.EntityClass)
            //            _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [list, list, item] => [list, list, (T)item]

            //        _gen.Emit(OpCodes.Callvirt, resultType.GetMethod("Add")); // [list, list, T] -> [list]

            //        _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [list] => [list, item]
            //        if (entity_cache.obj.LocalType != _entity.EntityClass)
            //            _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [list, item] => [list, (T)item]

            //        setEntitySimpleFields(_gen, this); // [..., T] => [..., T]
            //    }
            //    _gen.MarkLabel(isInHashSet);
            //    /* EXISTS IN HashSet */
            //    {
            //        setEntityComplexFields(_gen, this, entity_caches); // [..., T] -> [..., T]
            //    }
            //}
            //else
            //{
            //    _gen.Emit(OpCodes.Ldloc, entity_cache.obj); // [...] => [..., item]

            //    if (entity_cache.obj.LocalType != _entity.EntityClass)
            //        _gen.Emit(OpCodes.Castclass, _entity.EntityClass); // [..., item] => [..., (T)item]

            //    //setEntitySimpleFields(_gen, this); // [..., T] -> [..., T]

            //    setEntityComplexFields(_gen, this, entity_caches); // [..., T] -> [..., T]
            //}

            //_gen.Emit(OpCodes.Pop); // [..., T] -> [...]

            //// try get entity from cache
            ////entity_cache.
            

            //endWhile();

            //_gen.Emit(OpCodes.Ret);
            
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

    
}
