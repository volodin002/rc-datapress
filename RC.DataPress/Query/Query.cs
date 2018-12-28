using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RC.DataPress.Query
{
    public class Query<T> : QueryExpression
    {
        public Query(IModelManager manager) : base(manager.Entity<T>())
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

            var joinExpr = new JoinExpression<TProp>(field);

            if (_joins == null) _joins = new List<JoinExpression>();
            _joins.Add(joinExpr);

            return joinExpr;
        }

    }

    public class JoinExpression<T> : JoinExpression
    {
        

        public JoinExpression(IEntityField joinField) : base(joinField)
        {
        }

        public JoinExpression<TProp> Join<TProp>(Expression<Func<T, TProp>> expression)
        {
            var propInfo = Helper.Property(expression);
            var entity = _entity.ModelManager.Entity<TProp>();
            var field = entity.getField(propInfo.Name);

            var joinExpr = new JoinExpression<TProp>(field);

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

        public JoinExpression(IEntityField joinField) : base(joinField.EntityType)
        {
            _joinField = joinField;
        }

        public override StringBuilder CompileToSQL(StringBuilder sql)
        {
            throw new NotImplementedException();
        }
    }



    public abstract class QueryExpression : Expression
    {
        protected IEntityType _entity;
        protected List<JoinExpression> _joins;

        public QueryExpression(IEntityType entity)
        {
            _entity = entity;
        }
    }

    public abstract class Expression
    {
        public abstract StringBuilder CompileToSQL(StringBuilder sql);
    }
}
