using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RC.DataPress.Query
{
    public class Query<T> : QueryExpression
    {
        public Query(IModelManager manager) : base(manager.Entity<T>(), new AliasExpression("a"))
        {
        }

        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            throw new NotImplementedException();
        }

        public JoinExpression<TProp> Join<TProp>(Expression<Func<T, TProp>> expression)
        {
            var propInfo = Helper.Property(expression);
            var entity = _entity.ModelManager.Entity<TProp>();
            var field = entity.getField(propInfo.Name);

            var joinExpr = new JoinExpression<TProp>(field, _aliasExpression);

            if (_joins == null) _joins = new List<JoinExpression>();
            _joins.Add(joinExpr);

            return joinExpr;
        }

        public Query<T> Where(FilterExpression filter)
        {
            if (_where == null) _where = filter;
            else _where = new CompositeFilterExpression(_where, filter);
            return this;
        }
    }

    public class JoinExpression<T> : JoinExpression
    {
        

        public JoinExpression(IEntityField joinField, AliasExpression alias) : base(joinField, alias)
        {
        }

        public JoinExpression<TProp> Join<TProp>(Expression<Func<T, TProp>> expression)
        {
            var memberInfo = Helper.Member(expression);
            var entity = _entity.ModelManager.Entity<TProp>();
            var field = entity.getField(memberInfo.Name);

            var joinExpr = new JoinExpression<TProp>(field, _aliasExpression);

            if (_joins == null) _joins = new List<JoinExpression>();
            _joins.Add(joinExpr);

            return joinExpr;
        }

        public JoinExpression<TProp> Join<TProp>(Expression<Func<T, TProp>> expression, out JoinExpression<TProp> self)
        {
            self = Join<TProp>(expression);
            return self;
        }
    }

    public class JoinExpression : QueryExpression
    {
        IEntityField _joinField;

        public JoinExpression(IEntityField joinField, AliasExpression alias) : base(joinField.EntityType, alias)
        {
            _joinField = joinField;
        }

        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            throw new NotImplementedException();
            //string left_alias =  _alias + table_alias
            string left_column = _joinField.EntityType.KeyField.ColumnName;
            string right_column = _joinField.ForeignKey.ColumnName;

            return sql.Append('.').Append(left_column).Append('=').Append(right_column);
        }
    }



    public abstract class QueryExpression : Expression
    {
        protected string _alias;
        protected AliasExpression _aliasExpression;
        protected IEntityType _entity;
        protected FilterExpression _where;
        protected List<JoinExpression> _joins;

        public QueryExpression(IEntityType entity, AliasExpression alias)
        {
            _entity = entity;
            _alias = alias.Alias;
            _aliasExpression = alias.Next();
        }
    }

    public class AliasExpression
    {
        protected string _prefix;
        protected int _indx;
        public string Alias => _prefix + _indx.ToString();

        public AliasExpression(string prefix)
        {
            _prefix = prefix;
        }

        public AliasExpression Next()
        {
            _indx++;
            return this;
        }
    }

    public abstract class Expression
    {
        public abstract StringBuilder CompileToSQL(StringBuilder sql);
    }
}
