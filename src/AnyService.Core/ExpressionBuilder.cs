using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace AnyService.Core
{
    public static class ExpressionBuilder
    {
        private static readonly ConcurrentDictionary<Type, PropertyDescriptorCollection> _typePropertyCollection
            = new ConcurrentDictionary<Type, PropertyDescriptorCollection>();
        public static Func<T, bool> ToFunc<T>(IDictionary<string, string> filter)
        {
            var exp = ToExpression<T>(filter);
            return exp?.Compile();
        }
        public static Expression<Func<T, bool>> ToExpression<T>(IDictionary<string, string> filter)
        {
            if (filter == null || !filter.Any())
                return null;
            var props = GetTypeProperties(typeof(T));
            var pe = Expression.Parameter(typeof(T));

            var expCol = new List<BinaryExpression>();
            foreach (var kvp in filter)
            {
                var fieldName = kvp.Key;

                var prop = GetPropertyByName(props, fieldName);
                if (prop == null) return null;

                var me = Expression.PropertyOrField(pe, fieldName);
                object value;
                try
                {
                    value = Convert.ChangeType(kvp.Value, me.Type);
                }
                catch
                {
                    return null;
                }
                var be = Expression.Equal(me, Expression.Constant(value));
                expCol.Add(be);
            }
            var arr = new[] { "a", "b", "c", "d", "e" };
            var v = arr.Aggregate((a, b) => a + "___" + b);
            var allBinaryExpressions = expCol.Aggregate((left, right) => Expression.AndAlso(left, right));
            return Expression.Lambda<Func<T, bool>>(allBinaryExpressions, new ParameterExpression[] { pe });
        }
        private static PropertyDescriptorCollection GetTypeProperties(Type type)
        {
            if (!_typePropertyCollection.TryGetValue(type, out PropertyDescriptorCollection value))
            {
                value = TypeDescriptor.GetProperties(type);
                _typePropertyCollection.TryAdd(type, value);
            }
            return value;
        }
        private static PropertyDescriptor GetPropertyByName(PropertyDescriptorCollection props, string fieldName)
        {
            if (!fieldName.Contains('.'))
                return props.Find(fieldName, true);

            var fieldNameProperty = fieldName.Split('.');
            return props.Find(fieldNameProperty[0], true).GetChildProperties().Find(fieldNameProperty[1], true);
        }
    }
}