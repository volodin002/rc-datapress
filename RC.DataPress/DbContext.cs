using RC.DataPress.Emit;
using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace RC.DataPress
{
    public class DbContext<TConnection> where TConnection : DbConnection, new()
    {
        private readonly IModelManager _manager;
        private readonly string _connectionString;

        public IModelManager ModelManager => _manager;
        public DbContext(IModelManager manager, string connectionString)
        {
            _manager = manager;
            _connectionString = connectionString;
        }


        public static IModelManager CreateModelManager()
        {
            return new ModelManagerImpl();
        }

        public DbConnection GetConnection()
        {
            return new TConnection() { ConnectionString = _connectionString };
        }


        public T GetValue<T>(string sql, ref Func<DbDataReader, T> factory, IEnumerable<DbParameter> parameters = null)
        {
            T res;
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = new TConnection() { ConnectionString = _connectionString };
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                if (parameters != null)
                {
                    var cmdParameters = cmd.Parameters;
                    foreach (var p in parameters)
                        cmdParameters.Add(p);
                }

                con.Open();
                reader = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow);

                if (factory == null)
                    factory = FactoryEmiter.ValueFactory<T>(_manager, reader);

                if (reader.Read())
                    res = factory(reader);
                else
                    res = default(T);
                
            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return res;
        }

        public T GetValue<T>(string sql, ref Func<DbDataReader, T> factory, params DbParameter[] parameters)
        {
            T res;
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = GetConnection();
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                con.Open();
                reader = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow);

                if (factory == null)
                    factory = FactoryEmiter.ValueFactory<T>(_manager, reader);

                if (reader.Read())
                    res = factory(reader);
                else
                    res = default(T);

            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return res;
        }

        public List<T> GetValueList<T>(string sql, ref Func<DbDataReader, T> factory, IEnumerable<DbParameter> parameters = null)
        {
            var list = new List<T>();
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = new TConnection() { ConnectionString = _connectionString };
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                if (parameters != null)
                {
                    var cmdParameters = cmd.Parameters;
                    foreach (var p in parameters)
                        cmdParameters.Add(p);
                }

                con.Open();
                reader = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow);

                if (factory == null)
                    factory = FactoryEmiter.ValueFactory<T>(_manager, reader);

                while (reader.Read())
                    list.Add(factory(reader));

            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return list;
        }

        public List<T> GetValueList<T>(string sql, ref Func<DbDataReader, T> factory, params DbParameter[] parameters)
        {

            var list = new List<T>();
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = GetConnection();
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                con.Open();
                reader = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow);

                if (factory == null)
                    factory = FactoryEmiter.ValueFactory<T>(_manager, reader);

                while (reader.Read())
                    list.Add(factory(reader));

            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return list;
        }

        public IList<T> GetResultList<T>(string sql, ref Func<DbDataReader, IModelManager, IList<T>> factory, IEnumerable<DbParameter> parameters = null)
        {
            IList<T> items;
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = GetConnection();
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                if (parameters != null)
                {
                    var cmdParameters = cmd.Parameters;
                    foreach (var p in parameters)
                        cmdParameters.Add(p);
                }

                con.Open();

                reader = cmd.ExecuteReader();
                if (factory == null)
                    factory = FactoryEmiter.ResultListFactory<T>(_manager, reader);

                items = factory(reader, _manager);
                    
                
            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return items;
        }

        public IList<T> GetResultList<T>(string sql, ref Func<DbDataReader, IModelManager, IList<T>> factory, params DbParameter[] parameters)
        {
            IList<T> items;
            DbConnection con = null; DbCommand cmd = null; DbDataReader reader = null;
            try
            {
                con = GetConnection();
                cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                reader = cmd.ExecuteReader();
                if (factory == null)
                    factory = FactoryEmiter.ResultListFactory<T>(_manager, reader);

                items = factory(reader, _manager);


            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                if (con != null) con.Dispose();
            }

            return items;
        }
    }
}
