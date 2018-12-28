using System;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Query
{
    public abstract class FilterExpression : Expression
    {
        public static ValueExpression<TValue> Value<TValue>(TValue value)
        {
            return new ValueExpression<TValue>(value);
        }
    }

    public class CompositeFilterExpression : FilterExpression
    {
        bool _isOr;
        protected FilterExpression[] _filters;

        public CompositeFilterExpression(params FilterExpression[] filters)
        {
            _filters = filters;
        }

        public CompositeFilterExpression(bool isOr, params FilterExpression[] filters) : this(filters)
        {
            _isOr = isOr;
        }

        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            int length = _filters.Length;
            string oprt = _isOr ? " OR " : " AND ";
            sql.Append('(');
            for (int i = 0; i < length; i++)
            {
               _filters[i].CompileToSQL(sql.Append(i > 0 ? oprt : " "));   
            }
            sql.Append(')');

            return sql;
        }
    }

    public class ValueExpression<TValue> : Expression
    {
        TValue _value;
        public ValueExpression(TValue value)
        {
            _value = value;
        }


        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            var type = typeof(TValue);
            if (type == typeof(string))
                return sql.Append('\'').Append(_value).Append('\'');
            else if (type == typeof(bool))
                return sql.Append(_value.ToString()[0] == bool.TrueString[0] ? '1' : '0');
            else
                return sql.Append(_value);

        }
    }

    public class ParameterExpression<T> : Expression
    {
        Parameter<T> _param;
        public ParameterExpression(Parameter<T> parameter)
        {
            _param = parameter;
        }
        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            return sql.Append('@').Append(_param.Name);
        }
    }

    public class BinaryOperatorFilterExpression : FilterExpression
    {
        Expression _left;
        Expression _right;
        string _operator;

        public BinaryOperatorFilterExpression(Expression left, string @operator, Expression right)
        {
            _left = left;
            _operator = @operator;
            _right = right;
        }

        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            _left.CompileToSQL(sql).Append(_operator);
            return _right.CompileToSQL(sql);
        }
    }
}
