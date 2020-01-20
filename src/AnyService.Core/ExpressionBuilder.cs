using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace AnyService.Core
{
    public static class ExpressionBuilder
    {
        //https://stackoverflow.com/questions/16066751/create-funct-bool-from-memberexpression-and-constant
        //see: https://www.codementor.io/@juliandambrosio/how-to-use-expression-trees-to-build-dynamic-queries-c-xyk1l2l82
        private static readonly IDictionary<Type, PropertyDescriptorCollection> _typePropertyCollection = new Dictionary<Type, PropertyDescriptorCollection>();
        public static Func<T, bool> Build<T>(IDictionary<string, string> filter)
        {
            if (filter == null || !filter.Any())
                return null;
            var props = GetTypeProperties(typeof(T));
            var pe = Expression.Parameter(typeof(T));

            var allBinaryExpressions = new List<BinaryExpression>();

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
                allBinaryExpressions.Add(be);
            }


            var exp = Expression.Lambda<Func<T, bool>>(allBinaryExpressions.ElementAt(0), new ParameterExpression[] { pe });
            for (var i = 1; i < allBinaryExpressions.Count; i++)
            {
                var right = allBinaryExpressions.ElementAt(i);
                var also = Expression.AndAlso(exp, right);
                exp = Expression.Lambda<Func<T, bool>>(also);
            }
            return exp.Compile();

        }
        private static PropertyDescriptorCollection GetTypeProperties(Type type)
        {
            if (!_typePropertyCollection.TryGetValue(type, out PropertyDescriptorCollection value))
            {
                value = TypeDescriptor.GetProperties(type);
                _typePropertyCollection[type] = value;
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