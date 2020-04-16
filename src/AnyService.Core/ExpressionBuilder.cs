using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AnyService.Core
{
    public class ExpressionBuilder
    {
        private static readonly ConcurrentDictionary<Type, PropertyDescriptorCollection> _typePropertyCollection
            = new ConcurrentDictionary<Type, PropertyDescriptorCollection>();
        protected static readonly IReadOnlyDictionary<string, Func<MemberExpression, object, BinaryExpression>> BinaryExpressionBuilder = new Dictionary<string, Func<MemberExpression, object, BinaryExpression>>
        {
            {"==", (me, value) => Expression.Equal(me, Expression.Constant(value))},
            {"!=", (me, value) => Expression.NotEqual(me, Expression.Constant(value))},
            {">", (me, value) => Expression.GreaterThan(me, Expression.Constant(value))},
            {">=", (me, value) => Expression.GreaterThanOrEqual(me, Expression.Constant(value))},
            {"<", (me, value) => Expression.LessThan(me, Expression.Constant(value))},
            {"<=", (me, value) => Expression.LessThanOrEqual(me, Expression.Constant(value))},
        };
        protected static readonly IReadOnlyDictionary<string, Func<BinaryExpression, BinaryExpression, BinaryExpression>> EvaluationExpressionBuilder = new Dictionary<string, Func<BinaryExpression, BinaryExpression, BinaryExpression>>
        {
            {"&", (left, right) => Expression.And(left,right)},
            {"&&", (left, right) => Expression.AndAlso(left,right)},
            {"|", (left, right) => Expression.Or(left,right)},
            {"||", (left, right) => Expression.OrElse(left,right)},
        };
        private const string EvalPattern = @"^(?'leftOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\s*\S{1,})\s*(?'evaluator'(\|)(?!\1{2}))\s*(?'rightOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\S{1,})\s*$";
        private const string LeftOperand = "leftOperand";
        private const string RightOperand = "rightOperand";
        private const string Evaluator = "evaluator";
        private const string Operator = "operator";

        public static Func<T, bool> ToBinaryTree<T>(string query)
        {
            var binaryExpressions = new List<BinaryExpression>();
            var be = ToBinaryTreeWorker<T>(query);

            if (be == null)
                return null;
            throw new NotImplementedException();
            // return null;

            ///Convert to expression here
        }
        private static BinaryExpression ToBinaryTreeWorker<T>(string query)
        {
            var q = query.Trim();
            var firstIndexOfOpenBracket = q.IndexOf('(');
            if (firstIndexOfOpenBracket < 0)
            {
                var evalMatch = Regex.Match(q, EvalPattern);
                if (evalMatch.Success)
                {
                    var leftBe = ToBinaryTreeWorker<T>(evalMatch.Groups[LeftOperand].Value);
                    var rightBe = ToBinaryTreeWorker<T>(evalMatch.Groups[RightOperand].Value);
                    if (leftBe == null || rightBe == null) return null;

                    var builder = EvaluationExpressionBuilder[evalMatch.Groups[Evaluator].Value];
                    return builder(leftBe, rightBe);

                }
                var binaryOperationData = Regex.Match(q, @"^(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)$");
                if (!binaryOperationData.Success)
                    return null;
                return ToBinaryExpression<T>(
                    binaryOperationData.Groups[LeftOperand].Value,
                    binaryOperationData.Groups[Operator].Value,
                    binaryOperationData.Groups[RightOperand].Value);
            }
            if (firstIndexOfOpenBracket > 0)
            {
                var subQuery = q.Substring(0, firstIndexOfOpenBracket).Trim();
                var lastIndexOfComperar = subQuery.LastIndexOfAny(new[] { '&', '|' });
                if (lastIndexOfComperar < 0) return null;
            }
            if (firstIndexOfOpenBracket == 0)
            {
                var firstIndexOfCloseBracket = q.IndexOf(')');
                var subQuery = q.Substring(firstIndexOfCloseBracket + 1).Trim();
                var firstIndexOfComperar = subQuery.LastIndexOfAny(new[] { '&', '|' });

                if (firstIndexOfComperar < 0 || q.Substring(firstIndexOfCloseBracket + 1, firstIndexOfComperar).Trim().Any())
                    return null;
            }

            throw new System.NotImplementedException();
        }

        public static BinaryExpression ToBinaryExpression<T>(string propertyName, string @operator, string value)
        {
            if (!propertyName.HasValue() || !@operator.HasValue() || !value.HasValue())
                return null;
            var props = GetTypeProperties(typeof(T));
            var pe = Expression.Parameter(typeof(T));

            var prop = GetPropertyByName(props, propertyName);
            if (prop == null) return null;

            var me = Expression.PropertyOrField(pe, propertyName);
            object v;
            try
            {
                v = Convert.ChangeType(value, me.Type);
                return BinaryExpressionBuilder[@operator](me, v);
            }
            catch
            {
                return null;
            }
        }


        public static Func<T, bool> ToBinaryTree<T>(IDictionary<string, string> filter)
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