using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
//using static RC.DBA.SelectQuery<T>;

namespace RC.DataPress
{
    class SelectQuery<T> : IEnumerable<T>, IEnumerator<T>
    {
        private DbConnection _conn;
        private string _sql;
        internal Mapper<T> _mapper;

        public delegate bool Mapper<T0>(DbDataReader reader, ref T0 item);

        public SelectQuery(string sql)
        {
            //_conn = conn;
            _sql = sql;
        }

        public IEnumerable<T> Execute(DbConnection conn, params DbParameter[] parameters)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = _sql;
            cmd.Parameters.AddRange(parameters);

            return new SelectQueryIterator<T>(cmd, ref _mapper);
        }

        public T Current => throw new NotImplementedException();

        object IEnumerator.Current => Current;


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var cmd = _conn.CreateCommand();
            var reader = cmd.ExecuteReader();

            return this;
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class SelectQueryIterator<T> : IEnumerable<T>, IEnumerator<T>
    {
        private DbCommand _cmd;
        private DbDataReader _reader;
        private SelectQuery<T>.Mapper<T> _mapper;
        T _item;

        public SelectQueryIterator(DbCommand cmd, ref SelectQuery<T>.Mapper<T> mapper)
        {
            cmd = _cmd;
            _mapper = mapper;
        }

        public T Current => _item;

        object IEnumerator.Current => _item;

        public void Dispose()
        {
            if(_reader !=null)
            {
                _reader.Dispose();
                _reader = null;
            }
            if (_cmd != null)
            {
                _cmd.Dispose();
                _cmd = null;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            _reader = _cmd.ExecuteReader();

            return this;
        }

        public bool MoveNext()
        {
            return _mapper(_reader, ref _item);
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
