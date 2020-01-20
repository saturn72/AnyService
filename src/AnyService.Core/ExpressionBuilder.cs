using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace AnyService.Core
{
    public static class ExpressionBuilder
    {
        private static readonly IDictionary<Type, PropertyDescriptorCollection> _typePropertyCollection = new Dictionary<Type, PropertyDescriptorCollection>();

        //see: https://www.codementor.io/@juliandambrosio/how-to-use-expression-trees-to-build-dynamic-queries-c-xyk1l2l82
        public static Expression<Func<T, bool>> Build<T>(IDictionary<string, string> filter)
        {
            if (filter == null || !filter.Any())
                return null;
            var props = GetTypeProperties(typeof(T));

            Expression<Func<T, bool>> result = x => true;

            foreach (var kvp in filter)
            {
                var fieldName = kvp.Key;

                var prop = GetPropertyByName(props, fieldName);
                if (prop == null) return null;

                var parameter = Expression.Parameter(typeof(T));
                var me = GetMemberExpression<T>(parameter, fieldName);
                result
                result.Body.me.
                allMemberExpressions.Add(me);
                GetCriteriaWhere<T>(fieldName, kvp.Value);
            }
            throw new NotImplementedException();

        }
        private static Expression<Func<T, bool>> GetCriteriaWhere<T>(string fieldName, object fieldValue)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
            PropertyDescriptor prop = GetPropertyByName(props, fieldName);

            var parameter = Expression.Parameter(typeof(T));
            var expressionParameter = GetMemberExpression<T>(parameter, fieldName);
            if (prop != null && fieldValue != null)
            {
                var body = Expression.Equal(expressionParameter, Expression.Constant(fieldValue, prop.PropertyType));
                return Expression.Lambda<Func<T, bool>>(body, parameter);
            }
            else
            {
                Expression<Func<T, bool>> filter = x => true;
                return filter;
            }
        }
        private static MemberExpression GetMemberExpression<T>(ParameterExpression parameter, string propName)
        {
            if (string.IsNullOrEmpty(propName)) return null;
            var propertiesName = propName.Split('.');
            if (propertiesName.Count() == 2)
                return Expression.Property(Expression.Property(parameter, propertiesName[0]), propertiesName[1]);
            return Expression.Property(parameter, propName);
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
            return props.Find(fieldNameProperty[0], true).GetChildProperties().Find(fieldNameProperty[1], true
            );

        }

    }
}