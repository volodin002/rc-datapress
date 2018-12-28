using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace RC.DataPress.Emit
{
    static class EmitHelper
    {
        public static class Reader
        {
            static Dictionary<Type, MethodInfo> _ReaderMethodsCache = InitReaderMethodsCache();

            public static readonly MethodInfo IsDbNullMethod = typeof(DbDataReader).GetMethod("IsDBNull", new Type[] { typeof(int) });
            public static readonly MethodInfo ReadMethod = typeof(DbDataReader).GetMethod("Read", Type.EmptyTypes);


            internal static MethodInfo GetValueMethod(Type type)
            {
                if (_ReaderMethodsCache.TryGetValue(type, out var method))
                    return method;

                throw new NotSupportedException("ReaderMethodFromType: unsupported type " + type.FullName);
            }


            private static Dictionary<Type, MethodInfo> InitReaderMethodsCache()
            {
                var arg = new Type[] { typeof(int) };

                return new Dictionary<Type, MethodInfo>()
                {
                    { typeof(string), typeof(DbDataReader).GetMethod("GetString", arg) },
                    { typeof(int), typeof(DbDataReader).GetMethod("GetInt32", arg) },
                    { typeof(long), typeof(DbDataReader).GetMethod("GetInt64", arg) },
                    { typeof(DateTime), typeof(DbDataReader).GetMethod("GetDateTime", arg) },
                    { typeof(decimal), typeof(DbDataReader).GetMethod("GetDecimal", arg) },
                    { typeof(double), typeof(DbDataReader).GetMethod("GetDouble", arg) },
                    { typeof(bool), typeof(DbDataReader).GetMethod("GetBoolean", arg) },
                    { typeof(byte), typeof(DbDataReader).GetMethod("GetByte", arg) },
                    { typeof(Guid), typeof(DbDataReader).GetMethod("GetGuid", arg) },
                    { typeof(char), typeof(DbDataReader).GetMethod("GetChar", arg) },
                    { typeof(float), typeof(DbDataReader).GetMethod("GetFloat", arg) },
                    { typeof(short), typeof(DbDataReader).GetMethod("GetInt16", arg) },

                };
            }
        }

        /// <summary>
        /// emit: new Nullable<T>(value) 
        /// stack: [..., T] -> [..., Nullable<T>]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        public static void EmitNullableCtor(ILGenerator gen, Type type)
        {
            var ctor = typeof(Nullable<>).MakeGenericType(type).GetConstructor(new Type[] { type });
            gen.Emit(OpCodes.Newobj, ctor);
        }

        /// <summary>
        /// stack: [...] -> [..., new T()]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        public static void EmitNewObject(ILGenerator gen, Type type)
        {
            // [...] -> [..., new T()]
            gen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
        }

        /// <summary>
        /// emit: reader.Get{T}(ordinal)
        /// stack: [...] -> [..., reader.Get{T}(ordinal) ]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        /// <param name="ordinal">Ordinal</param>
        public static void EmitReadValue(ILGenerator gen, Type type, int ordinal)
        {
            var readerMethod = Reader.GetValueMethod(type);

            gen.Emit(OpCodes.Ldarg_0);                // [ ... ] -> [ ...,  reader ]
            gen.Emit(OpCodes.Ldc_I4, ordinal);        // [ ...,  reader ] -> [ ...,  reader, ordinal ]
            gen.Emit(OpCodes.Callvirt, readerMethod); // [ ...,  reader, ordinal ] -> [ ...,  reader.Get{T}(ordinal) ]
        }

        /// <summary>
        /// emit: reader.IsDBNull(ordinal) ? default(T) : reader.Get{T}(ordinal) 
        /// stack: [...] -> [..., value ]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        /// <param name="ordinal">Ordinal</param>
        public static void EmitReadValueWithDbNullCheck(ILGenerator gen, Type type, int ordinal)
        {
            var isNullable = Helper.IsNullableType(type);
            var dbType = isNullable ? Helper.GetNonNullableType(type) : type;

            gen.Emit(OpCodes.Ldarg_0);                         // [ ... ] -> [ ...,  reader ]
            gen.Emit(OpCodes.Ldc_I4, ordinal);                 // [ ...,  reader ] -> [ ...,  reader, ordinal ]
            gen.Emit(OpCodes.Callvirt, Reader.IsDbNullMethod); // [ ...,  reader, ordinal ] -> [ ...,  reader.IsDBNull(ordinal) ]

            Label null_in_reader = gen.DefineLabel();
            Label block_end = gen.DefineLabel();
            gen.Emit(OpCodes.Brtrue, null_in_reader); // [ ...,  reader.IsDBNull(ordinal) ] -> [...]

            // if(!reader.IsDBNull(ordinal)) {

            EmitReadValue(gen, dbType, ordinal);     //   [...] -> [..., reader.Get{T}(ordinal) ]
            if (isNullable)
                EmitNullableCtor(gen, dbType);       //   [...] -> [..., Nullable<T>( reader.Get{T}(ordinal) ) ]
            gen.Emit(OpCodes.Br_S, block_end);
            
            // } else {

            gen.MarkLabel(null_in_reader);
            EmitDefaultValue(gen, type);            //   [..., default(T) ]
            
            // }
            gen.MarkLabel(block_end);

            
        }

        /// <summary>
        /// emit: reader.IsDBNull(ordinal)
        /// stack: [...] -> [..., reader.IsDBNull(ordinal) ]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="ordinal"></param>
        public static void EmitIsDbNull(ILGenerator gen, int ordinal)
        {
            gen.Emit(OpCodes.Ldarg_0); // [ ... ] -> [ ...,  reader ]
            gen.Emit(OpCodes.Ldc_I4, ordinal); // [ ...,  reader ] -> [ ...,  reader, ordinal ]
            gen.Emit(OpCodes.Callvirt, Reader.IsDbNullMethod); // [ ...,  reader, ordinal ] -> [ ...,  reader.IsDBNull(ordinal) ]
        }

        /// <summary>
        /// emit: T.field = value
        /// stack: [..., T, value] -> [...]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="field">Field</param>
        public static void EmitSetField(ILGenerator gen, FieldInfo field)
        {
            gen.Emit(OpCodes.Stfld, field);
        }

        /// <summary>
        /// emit: T.prop = value
        /// stack: [..., T, value] -> [...]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="prop"></param>
        public static void EmitSetProp(ILGenerator gen, PropertyInfo prop)
        {
            gen.Emit(OpCodes.Callvirt, prop.SetMethod);
        }

        private static HashSet<Type> _ceq_types = new HashSet<Type>() {
            typeof(Int32),
            typeof(Int64),
            typeof(Int16),
            typeof(UInt32),
            typeof(UInt64),
            typeof(UInt16),
            typeof(float),
            typeof(double),
            typeof(char),
            typeof(bool)
        };

        /// <summary>
        ///  emit: if(value1 == value2)
        ///  stack: [..., value1, value2 ] -> [..., value1 == value2 ]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        public static void EmitIf(ILGenerator gen, Type type)
        {
            if (_ceq_types.Contains(type))
            {
                gen.Emit(OpCodes.Ceq); // op_Equality
                return;
            }

            var op_Equality_method = type.GetMethod("op_Equality", BindingFlags.Static);
            if (op_Equality_method == null)
                throw new NotSupportedException("Cannot emit compare operator for type:" + type.FullName);

            gen.Emit(OpCodes.Call, op_Equality_method);

        }

        /// <summary>
        /// emit: default(T)
        /// stack: [...] -> [..., default(T) ]
        /// </summary>
        /// <param name="gen">IL Generator</param>
        /// <param name="type">Type</param>
        public static void EmitDefaultValue(ILGenerator gen, Type type) //[...] -> [..., default(T)]
        {
            if(!type.IsValueType)
            {
                gen.Emit(OpCodes.Ldnull);
                return;
            }

            if (type == typeof(Int32) || type == typeof(bool) || type == typeof(Int16) || type == typeof(Byte) || type == typeof(Char))
                gen.Emit(OpCodes.Ldc_I4_0);
            else if (type == typeof(Int64))
                gen.Emit(OpCodes.Ldc_I8, 0L);
            else if (type == typeof(double))
                gen.Emit(OpCodes.Ldc_R8, 0D);
            else if (type == typeof(float))
                gen.Emit(OpCodes.Ldc_R4, 0F);
            else if (type == typeof(DateTime))
                gen.Emit(OpCodes.Ldsfld, typeof(DateTime).GetField("MinValue", BindingFlags.Static | BindingFlags.Public));
            else if (type == typeof(Decimal))
                gen.Emit(OpCodes.Ldsfld, typeof(Decimal).GetField("Zero", BindingFlags.Static | BindingFlags.Public));
            else if (type == typeof(Guid))
                gen.Emit(OpCodes.Ldsfld, typeof(Guid).GetField("Empty", BindingFlags.Static | BindingFlags.Public));
            

            if (Helper.IsNullableType(type)) // [..., T] -> [..., Nullable<T>]
            {
                EmitNullableCtor(gen, type);
            }

        }

        public static void EmitTryAddNew0(ILGenerator gen, Type idType, Type itemType, Type collectionType, LocalBuilder itemLocal)
        {
            // stack: [collection<T>, id, ref item] => [(bool)result]

            /*
            public bool TryAddNew(TKey id, ref T item, Func<T> itemFactory)
            {
                if (TryGet(id, ref item))
                    return false; // already exists

                item = itemFactory();
                
                Add(id, item);
                return true;
            }*/

            var tryGetMethod = collectionType.GetMethod("TryGet");

            gen.Emit(OpCodes.Callvirt, tryGetMethod);
            var idNotExists  = gen.DefineLabel();
            /*
            if(TryGet(id, ref item))
                    return false; // already exists */
            gen.Emit(OpCodes.Brfalse, idNotExists);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            /* item = itemFactory(); */
            gen.MarkLabel(idNotExists);
            EmitNewObject(gen, itemType);
            gen.Emit(OpCodes.Stloc, itemLocal);

            /* Add(id, item); */

            gen.Emit(OpCodes.Ldloc, itemLocal);
            

            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);
        }

        public static void EmitTryAddNew1(ILGenerator gen)
        {
            // stack: [collection<T>, id, ref item, dictionary<TKey, TBase>] => [(bool)result]

            /*
            public bool TryAddNew(TKey id, ref T item, Dictionary<TKey, TBase> dict, Func<T> itemFactory)
            {
                if (TryGet(id, ref item))
                    return false; // already exists

                if (dict.TryGetValue(id, out var d))
                {
                    item = (T)d;
                }
                else
                {
                    item = itemFactory();
                    dict.Add(id, item);
                }

                Add(id, item);
                return true;
            }*/


        }

        public static void EmitTryAddNew2(ILGenerator gen)
        {
            // stack: [collection<T>, id, descriminator, ref item] => [(bool)result]

            /*
            public bool TryAddNew(TKey id, int descriminator, ref T item, Func<int, T> itemFactory)
            {
                if (TryGet(id, ref item))
                    return false; // already exists

                item = itemFactory(descriminator);

                Add(id, item);
                return true;
            }
            */


        }

        public static void EmitTryAddNew3(ILGenerator gen)
        {
            // stack: [collection<T>, id, descriminator, ref item, dictionary<TKey, TBase>] => [(bool)result]

            /*
            public bool TryAddNew(TKey id, int descriminator, ref T item, Dictionary<TKey, TBase> dict, Func<int, T> itemFactory)
            {
                if (TryGet(id, ref item))
                    return false; // already exists

                if (dict.TryGetValue(id, out var d))
                {
                    item = (T)d;
                }
                else
                {
                    item = itemFactory(descriminator);
                    dict.Add(id, item);
                }

                Add(id, item);
                return true;
            }
            */


        }

    }
}
