using RC.DataPress.Collections;
using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace RC.DataPress.Emit
{
    class query_mapper_graph_emiter : query_mapper_emiter
    {
        public query_mapper_graph_emiter(ILGenerator gen, query_mapper mapper) : base(gen, mapper) { }

        public override void emit()
        {
            initLocals(); // [] -> []

            newResultList(); // [] -> [list]

            beginWhile();

            _gen.Emit(OpCodes.Dup); // [list] -> [list, list]

            { // try get entity from list
                _gen.Emit(OpCodes.Dup); // [list, list] -> [list, list, list]

                getEntityFieldValue(_mapper.key_field.entityField);   // [list, list, list] -> [list, list, list, id]

                var tryGetMethod = _resultType.GetMethod("TryGet");

                _gen.Emit(OpCodes.Ldloca, _mapper.entity_locals.obj); // [list, list, list, id] -> [list, list, list, id, &T]
                _gen.Emit(OpCodes.Callvirt, tryGetMethod);            // [list, list, list, id, &T] -> [list, list, TryGet(id, &T)]

            } // [list] -> [list, TryGet(id, &T)]

            var entityExistsInCollectionLabel = _gen.DefineLabel();
            _gen.Emit(OpCodes.Brtrue, entityExistsInCollectionLabel); // [list, list, TryGet(id, &T)] -> [list, list]

            newEntityOrGetExisting(_mapper); // [list, list] -> [list, list, new T()]

            setEntitySimpleFields(_mapper);  // [list, list, T] -> [list, list, T]

            {
                var entityNotExistsInCollectionLabel = _gen.DefineLabel();
                _gen.Emit(OpCodes.Br_S, entityNotExistsInCollectionLabel);
                _gen.MarkLabel(entityExistsInCollectionLabel);

                stloc_with_cast(_mapper); // [list, list] -> [list, list, T]

                _gen.MarkLabel(entityNotExistsInCollectionLabel);
            }

            setEntityComplexFields(_mapper); // [list, list, T] -> [list, list, T]

            addItemToResult();               // [list, list, T] -> [list]

            endWhile();

            _gen.Emit(OpCodes.Ret); // [list] -> []
        }

        protected override void newResultList()
        {
            _resultType = typeof(ResizableArrayWithKeySet<,,>)
                .MakeGenericType(_mapper.entity.KeyField.ValueType, _mapper.entity_locals.obj.LocalType, _mapper.entity.EntityClass);
        }

        protected override void initLocals()
        {
            var dictGenericType = typeof(Dictionary<,>);
            foreach (var locals in _mapper.all_locals)
            {
                var loce = locals.entity;
                if (locals == _mapper.entity_locals && _mapper.entity_locals.count > 0)
                {
                    var dict_type = dictGenericType.MakeGenericType(loce.KeyField.ValueType, loce.EntityClass);
                    locals.dict = _gen.DeclareLocal(dict_type);
                    EmitHelper.EmitNewObject(_gen, dict_type);
                    _gen.Emit(OpCodes.Stloc, locals.dict);
                }

                locals.obj = _gen.DeclareLocal(loce.EntityClass);
            }
        }

        protected override void setEntityComplexFields(query_entity queryEntity) // [..., T] -> [..., T]
        {
            foreach (var queryEntityField in queryEntity.entities)
            {
                var endSetEntityLabel = _gen.DefineLabel();
                // Check ID is NULL and skip Entity operation if ID == NULL
                if (queryEntityField.key_field.isNullable)
                {
                    EmitHelper.EmitIsDbNull(_gen, queryEntityField.key_field.ordinal); // [..., T] ->  [..., T, id==null]
                    _gen.Emit(OpCodes.Brtrue, endSetEntityLabel); // [..., T, id==null] ->  [..., T]
                }

                if (queryEntityField.entityField.isCollection)
                {
                    // Check is field is NULL
                    getEntityFieldValue(queryEntityField.entityField); // [..., T] ->  [..., T, collection<E>]                   
                    // if not NULL => do not create new collection
                    var collectionExistsLabel = _gen.DefineLabel();
                    {
                        _gen.Emit(OpCodes.Brtrue, collectionExistsLabel);      // [..., T, E] ->  [..., T] 

                        // collection is not exists => create new collection, add new (or from dictionary) Entity.
                        setEntityCollectionField(queryEntityField);        // [..., T] ->  [..., T] 

                        _gen.Emit(OpCodes.Br, endSetEntityLabel);
                    }

                    _gen.MarkLabel(collectionExistsLabel);

                    getEntityFieldValue(queryEntityField.entityField); // [..., T] ->  [..., T, collection<E>]                   
                    tryGetEntity(queryEntityField);                    // [..., T, collection<E>] -> [..., T, collection<E>, TryGet(id, &T)]

                    var entityExistsInCollectionLabel = _gen.DefineLabel();
                    _gen.Emit(OpCodes.Brtrue, entityExistsInCollectionLabel);    // [..., T, collection<E>, TryGet(id, &T)] -> [..., T, collection<E>]  

                    // entity is not exists in collection
                    {
                        getEntityFieldValue(queryEntityField.key_field.entityField); // [..., T, collection<E>] -> [..., T, collection<E>, id]
                        newEntityOrGetExisting(queryEntityField);                    // [..., T, collection<E>, id] -> [..., T, collection<E>, id, E]

                        addToCollection(queryEntityField);                           // [..., T, collection<E>, id, E] -> [..., T]

                        _gen.Emit(OpCodes.Brtrue, endSetEntityLabel);
                    }

                    _gen.MarkLabel(entityExistsInCollectionLabel);  // [..., T, collection<E>]
                    {
                        // entity exists in collection => skip set simple fields
                        _gen.Emit(OpCodes.Pop);                     // [..., T, collection<E>] -> [..., T]
                        stloc_with_cast(queryEntityField);          // [..., T] -> [..., T, E]
                        setEntityComplexFields(queryEntityField);   // [..., T, E] -> [..., T, E]
                        _gen.Emit(OpCodes.Pop);                     // [..., T, E] -> [..., T]

                        //_gen.Emit(OpCodes.Brtrue, endSetEntityLabel);
                    }

                }
                else
                {
                    // Check is field is NULL
                    getEntityFieldValue(queryEntityField.entityField); // [..., T] ->  [..., T, E]                   
                    // if not NULL skip Entity operation
                    _gen.Emit(OpCodes.Brtrue, endSetEntityLabel);      // [..., T, E] ->  [..., T]                   

                    _gen.Emit(OpCodes.Dup);                            // [..., T] ->  [..., T, T]
                    newEntityOrGetExisting(queryEntityField);          // [..., T, T] -> [..., T, T, new E()]
                    setEntityFieldValue(queryEntityField.entityField); // [..., T, T, new E()] -> [..., T]

                }

                _gen.MarkLabel(endSetEntityLabel);
            }
        }

        protected virtual void tryGetEntity(query_entity_field queryEntityField) // [..., collection<E>] -> [..., collection<E>, TryGet(id, &T)]
        {
            _gen.Emit(OpCodes.Dup); // [..., collection<E>] -> // [..., collection<E>, collection<E>]

            getEntityFieldValue(queryEntityField.key_field.entityField);  // [..., collection<E>, collection<E>] -> [..., collection<E>, collection<E>, id]

            var collectionType = getCollectionType(queryEntityField);
            var tryGetMethod = collectionType.GetMethod("TryGet");

            _gen.Emit(OpCodes.Ldloca, queryEntityField.entity_locals.obj); // [..., collection<E>, collection<E>, id] -> [..., collection<E>, collection<E>, id, &T]
            _gen.Emit(OpCodes.Callvirt, tryGetMethod);                     // [..., collection<E>, collection<E>, id, &T] -> [..., collection<E>, TryGet(id, &T)]



        }

        protected override void newEntityOrGetExisting(query_entity queryEntity) // [...] => [..., T]
        {
            LocalBuilder dictLocal = queryEntity.entity_locals?.dict;
            var objLocal = queryEntity.entity_locals.obj;
            var id_field = queryEntity.key_field;

            Label foundLabel = _gen.DefineLabel();
            if (dictLocal != null)
            {
                _gen.Emit(OpCodes.Stloc, dictLocal); // [...] -> [..., dict]

                
                EmitHelper.EmitReadValue(_gen, id_field.entityField.ValueType, id_field.ordinal); // [..., dict] -> [..., dict, id]

                
                _gen.Emit(OpCodes.Ldloca, objLocal); // [..., dict, id] -> [..., dict, id, &T]

                var tryGetValueMethod = dictLocal.LocalType.GetMethod("TryGetValue");
                _gen.Emit(OpCodes.Callvirt, tryGetValueMethod); // [..., dict, id, &T] -> [..., TryGetValue()]

                
                _gen.Emit(OpCodes.Brtrue_S, foundLabel); //[..., TryGetValue()] -> [...]
               
            }


            newEntity(queryEntity);             // [...] -> [..., new T()]

            if (dictLocal != null)
            {

                _gen.Emit(OpCodes.Stloc, objLocal); // [..., T] -> [...]

                _gen.Emit(OpCodes.Stloc, dictLocal); // [...] -> [..., dict]
                EmitHelper.EmitReadValue(_gen, id_field.entityField.ValueType, id_field.ordinal); // [..., dict] -> [..., dict, id]
                _gen.Emit(OpCodes.Stloc, objLocal); // [..., dict, id] -> [..., dict, id, T]
                // Add new object to dictionary
                var addMethod = dictLocal.LocalType.GetMethod("Add");
                _gen.Emit(OpCodes.Callvirt, addMethod); // [..., dict, id, T] -> [...]


                if (queryEntity.entity_locals.count > 0) // always setEntitySimpleFields
                {
                    _gen.MarkLabel(foundLabel);

                    stloc_with_cast(queryEntity);        // [...] -> [..., T]

                    setEntitySimpleFields(queryEntity);  // [..., T] -> [..., T]
                    setEntityComplexFields(queryEntity); // [..., T] -> [..., T]
                }
                else
                {
                    stloc_with_cast(queryEntity);       // [...] -> [..., T]

                    setEntitySimpleFields(queryEntity); // [..., T] -> [..., T]
                    setEntityComplexFields(queryEntity); // [..., T] -> [..., T]

                    Label endLabel = _gen.DefineLabel();
                    _gen.Emit(OpCodes.Br_S, endLabel);  // [..., T] -> [..., T]

                    _gen.MarkLabel(foundLabel); // skip setEntitySimpleFields if entity is founded in dictionary

                    stloc_with_cast(queryEntity);        // [...] -> [..., T]

                    setEntityComplexFields(queryEntity); // [..., T] -> [..., T]

                    _gen.MarkLabel(endLabel);
                }
            }
            
        }

        protected override void addToCollection(query_entity_field queryEntityField) // [..., collection<E>, id, E] -> [...]
        {
            Type collectionType = getCollectionType(queryEntityField);

            var addMethod = collectionType.GetMethod("Add", 
                new Type[] { queryEntityField.key_field.entityField.ValueType, queryEntityField.entityField.ValueType });

            _gen.Emit(OpCodes.Callvirt, addMethod); // [..., collection<E>, id, E] -> [..., collection<E>.Add(E)]
        }

        /// <summary>
        /// Create collection, create new Entity or get existing, 
        /// fill all fields and add entity to the collection
        /// </summary>
        /// <param name="queryEntityField"></param>
        protected override void setEntityCollectionField(query_entity_field queryEntityField) // [..., T] -> [..., T]
        {
            _gen.Emit(OpCodes.Dup);                                  // [..., T] ->  [..., T, T]
            var collectionType = getCollectionType(queryEntityField);
            EmitHelper.EmitNewObject(_gen, collectionType);          // [..., T, T] -> [..., T, T, new collection<E>()]

            _gen.Emit(OpCodes.Dup);                                  // [..., T, T] -> [..., T, T, collection<E>] -> [..., T, T, collection<E>, collection<E>]

            getEntityFieldValue(queryEntityField.key_field.entityField);  // [..., T, T, collection<E>, collection<E>] -> [..., T, T, collection<E>, collection<E>, id]

            newEntityOrGetExisting(queryEntityField);                // [..., T, T, collection<E>, collection<E>, id] -> [..., T, T, collection<E>, collection<E>, id, new E()]

            addToCollection(queryEntityField);                       // [..., T, T, collection<E>, collection<E>, id, new E()] -> [..., T, T, collection<E>]

            setEntityFieldValue(queryEntityField.entityField);       // [..., T, T, collection<E>] -> [..., T]
        }


        protected override Type getCollectionType(query_entity_field queryEntityField)
        {
            var collectionType = queryEntityField.entityField.MemberType;
            Type resultCollectionType;

            if (typeof(List<>).IsAssignableFrom(collectionType))
            {
                resultCollectionType = typeof(ListWithKeySet<,,>);
            }
            else if (typeof(HashSet<>).IsAssignableFrom(collectionType) || typeof(ISet<>).IsAssignableFrom(collectionType))
            {
                resultCollectionType = typeof(HashSetWithKeySet<,,>);
            }
            else
            {
                resultCollectionType = typeof(ResizableArrayWithKeySet<,,>);
            }

            var entity = queryEntityField.entity;
            var entity_locals = queryEntityField.entity_locals;

            return resultCollectionType
                .MakeGenericType(entity.KeyField.ValueType, entity_locals.obj.LocalType, entity.EntityClass);
        }

        protected void stloc_with_cast(query_entity queryEntity) // [...] -> [..., T]
        {
            var objLocal = queryEntity.entity_locals.obj;
            var entityClass = queryEntity.entity.EntityClass;

            _gen.Emit(OpCodes.Stloc, objLocal); // [...] -> [..., T]
            if (objLocal.LocalType != entityClass)
            {
                _gen.Emit(OpCodes.Castclass, entityClass); // [..., T] -> [..., (T)T]
            }
        }
    }
}
