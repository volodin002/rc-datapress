using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RC.DataPress
{
    public static class Helper
    {
        #region Linq
        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static bool SafeAny<T>(this IEnumerable<T> items)
        {
            return items != null && items.Any();
        }

        public static bool SafeAny<TSource>(this IEnumerable<TSource> items, Func<TSource, bool> predicate)
        {
            return items != null && items.Any(predicate);
        }

        public static IEnumerable<TSource> SafeExcept<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            return (first ?? Enumerable.Empty<TSource>()).Except(second ?? Enumerable.Empty<TSource>(), comparer);
        }

        #endregion // Linq

        #region Expressions
        public static MemberInfo Member<T, TProp>(Expression<Func<T, TProp>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                return null;

            return memberExpression.Member;
        }

        public static PropertyInfo Property<T, TProp>(Expression<Func<T, TProp>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                return null;

            return memberExpression.Member as PropertyInfo;
        }

        public static FieldInfo Field<T, TField>(Expression<Func<T, TField>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                return null;

            return memberExpression.Member as FieldInfo;
        }

        //public static PropertyInfo PropertyOfTypeCall<T, TProp>(Expression<Func<T, TProp>> expression)
        //{
        //    var callExpression = expression.Body as MethodCallExpression;
        //    if (callExpression != null && callExpression.Method.Name == "OfType")
        //    {
        //        var arg = callExpression.Arguments[0];
        //        var memberExpression = arg as MemberExpression;
        //        if (memberExpression == null) return null;

        //        return memberExpression.Member as PropertyInfo;
        //    }

        //    return null;
        //}

        #endregion // Expressions

        #region Reflection

        /// <summary>
        /// Can value of this type be equal to null.
        /// (Is it reference type of Nullable<T>)
        /// </summary>
        public static bool IsTypeCanBeNull(Type type)
        {
            return !type.IsValueType || IsNullableType(type);
        }

        /// <summary>
        /// Check is type is Nullable<T>
        /// </summary>
        /// <param name="type"></param>
        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// If type is Nullable<T> return typeof(T) else return type himself.
        /// </summary>
        public static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        public static Type GetNullableType(Type type)
        {
            return typeof(Nullable<>).MakeGenericType(new[] { type });
        }

        /// <summary>
        /// Check is type is primitive type and cannot be Entity
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>Is primitive type</returns>
        public static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(DateTime)
                || type == typeof(Decimal)
                || type == typeof(Enum)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type == typeof(DateTimeOffset);
        }

        public static bool IsBaseType(Type type, Type isBase)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType == isBase) return true;
                baseType = baseType.BaseType;
            }

            return false;
        }

        #endregion // Reflection

        #region Hash

        public static int GetHash<T1, T2>(T1 x1, T2 x2)
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17 * 23 + (x1 != null ? x1.GetHashCode() : 0);
                return hash * 23 + (x2 != null ? x1.GetHashCode() : 0);
            }
        }

        public static int GetHash<T1, T2, T3>(T1 x1, T2 x2, T3 x3)
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17 * 23 + (x1 != null ? x1.GetHashCode() : 0);
                hash = hash * 23 + (x2 != null ? x2.GetHashCode() : 0);
                return hash * 23 + (x3 != null ? x3.GetHashCode() : 0);
            }
        }

        #endregion // Hash
    }
}
