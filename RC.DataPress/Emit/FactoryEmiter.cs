using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace RC.DataPress.Emit
{
    class FactoryEmiter
    {
        private static int _index;

        //private static string getFactoryMethodName(Type type)
        //{
        //    return "_$DBA_ResultListFactory_" 
        //        + type.Name 
        //        + Interlocked.Increment(ref _index).ToString();
        //}

        public static Func<DbDataReader, T> ValueFactory<T>(IModelManager manager, DbDataReader reader)
        {
            var method = new DynamicMethod("_$DataPress_ValueFactory_" + typeof(T).Name + Interlocked.Increment(ref _index).ToString(),
               typeof(T), new[] { typeof(DbDataReader) }, true);

            var gen = method.GetILGenerator();

            return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
        }

        

        public static Func<DbDataReader, IModelManager, IList<T>> ResultListFactory<T>(IModelManager manager, DbDataReader reader)
        {
            var method = new DynamicMethod("_$DataPress_ResultListFactory_" + typeof(T).Name + Interlocked.Increment(ref _index).ToString(),
               typeof(IList<T>), new[] { typeof(DbDataReader), typeof(IModelManager) }, true);

            var gen = method.GetILGenerator();

            query_mapper mapper = new query_mapper(manager.Entity<T>(), gen);
            mapper.initMetamodel(reader, QUERY_MAPPER_TYPE.AUTO);
            mapper.emit();

            return (Func<DbDataReader, IModelManager, IList<T>>)method.CreateDelegate(typeof(Func<DbDataReader, IModelManager, IList<T>>));
        }

        

    }
}
