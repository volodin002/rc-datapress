using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace RC.DataPress.Query
{
    public class Parameter<T> : Parameter
    {
        public Parameter(string name)
        {
            _Name = name;
        }

        protected Parameter()
        {
        }

        public T Value { get; set; }

        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            if (value == null || value == DBNull.Value)
                Value = default(T);
            else
                Value = (T)value;
        }

        public override Type ParameterType()
        {
            return typeof(T);
        }

        internal override Parameter Copy()
        {
            var p = Create(_Name, Value);
            p.Direction = _Direction;
            return p;
        }
    }

    public abstract class Parameter
    {
        protected string _Name;
        protected System.Data.ParameterDirection _Direction = System.Data.ParameterDirection.Input;

        public String Name => _Name;

        public System.Data.ParameterDirection Direction { get { return _Direction; } set { _Direction = value; } }
        //
        // Summary:
        //     Gets or sets the maximum
        //     number of digits used to represent the System.Data.Common.DbParameter.Value property.
        //
        // Returns:
        //     The maximum number of digits used to represent the System.Data.Common.DbParameter.Value
        //     property.
        public byte Precision { get; set; }
        //
        // Summary:
        //     Gets or sets the number of decimal places to which System.Data.Common.DbParameter.Value
        //     is resolved.
        //
        // Returns:
        //     The number of decimal places to which System.Data.Common.DbParameter.Value is
        //     resolved.
        public byte Scale { get; set; }
        //
        // Summary:
        //     Gets or sets the maximum size, in bytes, of the data within the column.
        //
        // Returns:
        //     The maximum size, in bytes, of the data within the column. The default value
        //     is inferred from the parameter value.
        public int Size { get; set; }

        //int getPosition();
        public abstract object GetValue();

        public abstract void SetValue(object value);

        public abstract Type ParameterType();

        internal abstract Parameter Copy();

        public static Parameter<T> Create<T>(string name, T value)
        {
            return new Parameter<T>(name) { Value = value };
        }

        public DbParameter DbParameter(DbCommand cmd)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = _Name;
            p.Value = GetValue() ?? DBNull.Value;
            p.Direction = (System.Data.ParameterDirection)_Direction;
            return p;
        }

        public DbParameter AddDbParameter(DbCommand cmd)
        {
            var p = DbParameter(cmd);
            cmd.Parameters.Add(p);

            return p;
        }

        public Parameter AddPrefix(string prefix)
        {
            _Name += prefix;
            return this;
        }
    }
}
