using RC.DataPress.Metamodel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace RC.DataPress.Emit
{
    class query_mapper_emiter
    {
        protected ILGenerator _gen;

        protected Label _whileStartLabel;
        protected Label _whileEndLabel;
        protected Type _resultType;

        protected query_mapper _mapper;

        public query_mapper_emiter(ILGenerator gen, query_mapper mapper)
        {
            _gen = gen;
            _mapper = mapper;
        }

        public virtual void emit()
        {
            newResultList(); // [] -> [list]

            beginWhile();

            _gen.Emit(OpCodes.Dup); //[list] -> [list, list]

            newEntity(_mapper); // [list, list] -> [list, list, new T()]

            setEntitySimpleFields(_mapper); // [list, list, T] -> [list, list, T]

            setEntityComplexFields(_mapper); // [list, list, T] -> [list, list, T]

            addItemToResult(); // [list, list, T] -> [list]

            endWhile();

            _gen.Emit(OpCodes.Ret); // [list] -> []
        }

        /// <summary>
        /// emit: result = new collection<T>();
        /// stack: [...] -> [..., collection<T>]
        /// </summary>
        protected virtual void newResultList() // [...] -> [..., collection<T>]
        {
            _resultType = typeof(List<>).MakeGenericType(_mapper.entity.EntityClass);
            // [] -> [list]
            EmitHelper.EmitNewObject(_gen, _resultType);
        }

        protected virtual void addItemToResult() // [..., list, list, new T()] -> [..., list]
        {
            var addMethod = _resultType.GetMethod("Add");
            _gen.Emit(OpCodes.Callvirt, addMethod); // [..., list, list, new T()] -> [..., list]
        }

        protected virtual void initLocals()
        {
        }

        protected virtual void newEntity(query_entity queryEntity) // [...] -> [..., new T()]
        {
            if (queryEntity.type_field != null)
            {
                var derivedEntities = _mapper.entity.ModelManager
                    .getDerivedEntityTypes(queryEntity.entity.EntityClass).Where(x => !x.isAbstract)
                    .ToList();

                bool firstIf = true;
                var endOfIf = _gen.DefineLabel();
                if (!queryEntity.entity.isAbstract)
                {
                    emitDescriminatorCheck(queryEntity.type_field, queryEntity.entity, ref firstIf);
                }

                foreach (var derivedEntitie in derivedEntities.Where(x => !x.isAbstract))
                {
                    emitDescriminatorCheck(queryEntity.type_field, derivedEntitie, ref firstIf);
                }

                void emitDescriminatorCheck(query_field descriminatorField, IEntityType entityType, ref bool first)
                {
                    //_gen.Emit(OpCodes.Ldarg_0);
                    if (first)
                    {
                        EmitHelper.EmitReadValue(_gen, descriminatorField.entityField.ValueType, descriminatorField.ordinal);
                        first = false;
                    }
                    else
                    {
                        _gen.Emit(OpCodes.Dup);
                    }

                    _gen.Emit(OpCodes.Ldc_I4, entityType.Descriminator);
                    var label = _gen.DefineLabel();
                    _gen.Emit(OpCodes.Bge_Un_S, label);

                    EmitHelper.EmitNewObject(_gen, entityType.EntityClass);

                    _gen.Emit(OpCodes.Br, endOfIf);
                    _gen.MarkLabel(label);
                }
                _gen.MarkLabel(endOfIf);

                if (firstIf)
                    throw new ApplicationException($"Cannot emit new Entity with type: {queryEntity.entity.EntityClass.FullName}");
            }
            else
            {
                EmitHelper.EmitNewObject(_gen, queryEntity.entity.EntityClass); // [...] -> [..., new T()]
            }
        }

        protected void setEntitySimpleField(query_field field) // [..., T] => [..., T]
        {
            _gen.Emit(OpCodes.Dup); // [..., T] -> [..., T, T]

            var entityField = field.entityField;
            // [..., T, T] -> [..., T, T, value]
            if (field.isNullable)
                EmitHelper.EmitReadValueWithDbNullCheck(_gen, entityField.ValueType, field.ordinal);
            else
                EmitHelper.EmitReadValue(_gen, entityField.ValueType, field.ordinal);

            setEntityFieldValue(entityField); //  [..., T, T, value] ->  [..., T]
        }

        protected void setEntityFieldValue(IEntityField entityField) // [..., T, Value] => [...]
        {
            if (entityField.isProperty)
            {
                var propInfo = (PropertyInfo)entityField.MemberInfo;
                var propSetter = propInfo.SetMethod;
                _gen.Emit(OpCodes.Callvirt, propSetter);
            }
            else
            {
                _gen.Emit(OpCodes.Stfld, (FieldInfo)entityField.MemberInfo);
            }
        }

        protected void getEntityFieldValue(IEntityField entityField) // [..., T] => [..., T, Value]
        {
            _gen.Emit(OpCodes.Dup); // [..., T] -> [..., T, T]

            if (entityField.isProperty)
            {
                var propInfo = (PropertyInfo)entityField.MemberInfo;
                var propGetter = propInfo.GetMethod;
                _gen.Emit(OpCodes.Callvirt, propGetter); // [..., T, T] -> [..., T, Value]
            }
            else
            {
                _gen.Emit(OpCodes.Ldfld, (FieldInfo)entityField.MemberInfo); // [..., T, T] -> [..., T, Value]
            }
        }

        protected void setEntitySimpleFields(query_entity entity) // [..., T] => [..., T]
        {
            if (entity.key_field != null)
                setEntitySimpleField(entity.key_field);

            foreach (var field in entity.fields)
            {
                setEntitySimpleField(field);
            }
        }

        protected virtual void setEntityComplexFields(query_entity queryEntity) // [..., T] -> [..., T]
        {
            foreach (var queryEntityField in queryEntity.entities)
            {
                var endSetEntityLabel = _gen.DefineLabel();
                if (queryEntityField.key_field.isNullable)
                {
                    EmitHelper.EmitIsDbNull(_gen, queryEntityField.key_field.ordinal); // [..., T] ->  [..., T, id==null]
                    _gen.Emit(OpCodes.Brtrue, endSetEntityLabel);                      // [..., T, id==null] ->  [..., T]
                }

                if(queryEntityField.entityField.isCollection)
                {
                    setEntityCollectionField(queryEntityField);
                }
                else
                {
                    _gen.Emit(OpCodes.Dup);                            // [..., T] ->  [..., T, T]
                    newEntityOrGetExisting(queryEntityField);          // [..., T, T] -> [..., T, T, new E()]
                    setEntityFieldValue(queryEntityField.entityField); // [..., T, T, new E()] -> [..., T]
                }

                _gen.MarkLabel(endSetEntityLabel);
            }
        }

        /// <summary>
        /// Create collection, create new Entity or get existing, 
        /// fill all fields and add entity to the collection
        /// </summary>
        /// <param name="queryEntityField"></param>
        protected virtual void setEntityCollectionField(query_entity_field queryEntityField) // [..., T] -> [..., T]
        {
            _gen.Emit(OpCodes.Dup);                                  // [..., T] ->  [..., T, T]
            var collectionType = getCollectionType(queryEntityField);
            EmitHelper.EmitNewObject(_gen, collectionType);          // [..., T, T] -> [..., T, T, new collection<E>()]

            _gen.Emit(OpCodes.Dup);                                  // [..., T, T] -> [..., T, T, collection<E>] -> [..., T, T, collection<E>, collection<E>]

            newEntityOrGetExisting(queryEntityField);                // [..., T, T, collection<E>, collection<E>] -> [..., T, T, collection<E>, collection<E>, new E()]

            addToCollection(queryEntityField);                       // [..., T, T, collection<E>, collection<E>, new E()] -> [..., T, T, collection<E>]

            setEntityFieldValue(queryEntityField.entityField);       // [..., T, T, collection<E>] -> [..., T]
        }

        protected virtual void newEntityOrGetExisting(query_entity queryEntity) // [...] -> [..., T]
        {
            newEntity(queryEntity);
        }

        /*
        protected void addToCache(query_entity entity) // [...] => [...]
        {
            // => [ ... ]
            entity_locals locals = entity.entity_locals;
            query_field id_field = entity.key_field;

            _gen.Emit(OpCodes.Ldloc, locals.dict); // [...] => [..., dict]

            // [...] => [..., dict, id]
            EmitHelper.EmitReadValue(_gen, id_field.entityField.ValueType, id_field.ordinal);

            //EmitHelper.EmitNewObject(gen, entity.Entity.EntityClass); // [..., dict, id] -> [..., dict, id, new T()]

            _gen.Emit(OpCodes.Ldloc, locals.obj); // => [ ..., dict, id, T]
            _gen.Emit(OpCodes.Callvirt, locals.dict.LocalType.GetMethod("Add")); // => [ ... ]
        }
        */
        protected virtual Type getCollectionType(query_entity_field queryEntityField)
        {
            var collectionType = queryEntityField.entityField.MemberType;
            var fieldType = queryEntityField.entityField.ValueType;

            if (typeof(List<>).IsAssignableFrom(collectionType))
                return typeof(List<>).MakeGenericType(fieldType);
            else if(typeof(HashSet<>).IsAssignableFrom(collectionType) || typeof(ISet<>).IsAssignableFrom(collectionType))
                return typeof(HashSet<>).MakeGenericType(fieldType);
            else
                return typeof(List<>).MakeGenericType(fieldType);
        }

        protected virtual void addToCollection(query_entity_field queryEntityField) // [..., collection<E>, E] -> [...]
        {
            Type collectionType = getCollectionType(queryEntityField);

            var addMethod = collectionType.GetMethod("Add");
            _gen.Emit(OpCodes.Callvirt, addMethod); // [..., collection<E>, E] -> [..., collection<E>.Add(E)]
            if (addMethod.ReturnType == typeof(bool)) // 
                _gen.Emit(OpCodes.Pop); // [..., collection<E>.Add(E)] -> [...]
    }

        /// <summary>
        /// emit: while(rs.next()) {
        /// stack: [...] -> [...]
        /// </summary>
        protected void beginWhile() // [...] -> [...]
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
        protected void endWhile() // [...] -> [...]
        {
            _gen.Emit(OpCodes.Br, _whileStartLabel); // goto to the start of while(){} loop
            _gen.MarkLabel(_whileEndLabel); // } end while
        }
    }
}
