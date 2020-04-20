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
        protected static readonly IReadOnlyDictionary<string, Func<MemberExpression, object, BinaryExpression>> BinaryExpressionBuilder =
            new Dictionary<string, Func<MemberExpression, object, BinaryExpression>>
            {
                        {"==", (me, value) => Expression.MakeBinary(ExpressionType.Equal, me, Expression.Constant(value))},
                        {"!=", (me, value) => Expression.MakeBinary(ExpressionType.NotEqual, me, Expression.Constant(value))},
                        {">", (me, value) => Expression.MakeBinary(ExpressionType.GreaterThan, me, Expression.Constant(value))},
                        {">=", (me, value) => Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, me, Expression.Constant(value))},
                        {"<", (me, value) => Expression.MakeBinary(ExpressionType.LessThan, me, Expression.Constant(value))},
                        {"<=", (me, value) => Expression.MakeBinary(ExpressionType.LessThanOrEqual, me, Expression.Constant(value))},
            };
        protected static readonly IReadOnlyDictionary<string, Func<Expression, Expression, Expression>> EvaluationExpressionBuilder =
        new Dictionary<string, Func<Expression, Expression, Expression>>
        {
            {"&", (left, right) => Expression.And(left,right)},
            {"&&", (left, right) => Expression.AndAlso(left,right)},
            {"|", (left, right) => Expression.Or(left,right)},
            {"||", (left, right) => Expression.OrElse(left,right)},
        };

        // protected const string HasNoBrackets = @"^\s*[^(]*$";
        protected const string HasBrackets = @"^\s*(?'leftOperand'[^\|\&]*)\s*(?'evaluator_first'((\|)*|(\&)*))\s*(?'bracket'(\(\s*(.*)s*\)))\s*(?'evaluator_second'((\|{1,2})|(\&{1,2}))*)\s*(?'rightOperand'.*)\s*$";
        // protected const string StartsWithBracketPattern = @"^\s*(?'leftOperand'\(.*\))\s*(?'evaluator'((\|{1,2})|(\&{1,2})))\s*(?'rightOperand'.*)\s*$";
        // protected const string EndsWithBracketPattern = @"^\s*(?'leftOperand'.*)\s*[^\|\&](?'evaluator'(((\|){1,2})|(\&{1,2})))\s*(?'rightOperand'\(.*\))\s*$";
        protected const string EvalPattern = @"^(?'leftOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\s*\S{1,})\s*(?'evaluator'((\|{1,2})|(\&{1,2})))\s*(?'rightOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\S{1,})\s*$";
        protected const string BinaryPattern = @"^(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)$";
        private const string LeftOperand = "leftOperand";
        private const string RightOperand = "rightOperand";
        private const string Evaluator = "evaluator";
        private const string Operator = "operator";

        public static Func<T, bool> ToBinaryTree<T>(string query)
        {
            var pe = Expression.Parameter(typeof(T), "x");
            var bt = ToBinaryTreeWorker<T>(query, pe);
            return bt.Compile();
        }
        private static Expression<Func<T, bool>> ToBinaryTreeWorker<T>(string query, ParameterExpression parameterExpression)
        {
            var q = query.Trim();
            var hasBrackets = Regex.Match(q, HasBrackets);
            if (!hasBrackets.Success)
            {
                var evalMatch = Regex.Match(q, EvalPattern);
                if (evalMatch.Success)
                {
                    var leftBinaryExpression = ToBinaryTreeWorker<T>(evalMatch.Groups[LeftOperand].Value, parameterExpression);
                    var rightBinaryExpression = ToBinaryTreeWorker<T>(evalMatch.Groups[RightOperand].Value, parameterExpression);
                    if (leftBinaryExpression == null || rightBinaryExpression == null) return null;

                    var builder = EvaluationExpressionBuilder[evalMatch.Groups[Evaluator].Value];
                    var evaluation = builder(leftBinaryExpression.Body, rightBinaryExpression.Body);
                    var exp = Expression.Lambda<Func<T, bool>>(evaluation, leftBinaryExpression.Parameters);
                    return exp;
                }
                var binaryOperationData = Regex.Match(q, BinaryPattern);
                if (!binaryOperationData.Success)
                    return null;

                return ToBinaryExpression<T>(
                    binaryOperationData.Groups[LeftOperand].Value,
                    binaryOperationData.Groups[Operator].Value,
                    binaryOperationData.Groups[RightOperand].Value,
                    parameterExpression);
            }
            // if (firstIndexOfOpenBracket > 0)
            // {
            //     var subQuery = q.Substring(0, firstIndexOfOpenBracket).Trim();
            //     var lastIndexOfComperar = subQuery.LastIndexOfAny(new[] { '&', '|' });
            //     if (lastIndexOfComperar < 0) return null;
            // }
            var lef
            var firstIndexOfCloseBracket = q.IndexOf(')');
            var subQuery = q.Substring(firstIndexOfCloseBracket + 1).Trim();
            var firstIndexOfComperar = subQuery.LastIndexOfAny(new[] { '&', '|' });

            if (firstIndexOfComperar < 0 || q.Substring(firstIndexOfCloseBracket + 1, firstIndexOfComperar).Trim().Any())
                return null;

            throw new System.NotImplementedException();
        }
        public static Expression<Func<T, bool>> ToBinaryExpression<T>(string propertyName, string @operator, string value, ParameterExpression parameterExpression)
        {
            if (!propertyName.HasValue() || !@operator.HasValue() || !value.HasValue())
                return null;
            var type = typeof(T);
            var props = GetTypeProperties(type);

            var prop = GetPropertyByName(props, propertyName);
            if (prop == null) return null;

            var me = Expression.PropertyOrField(parameterExpression, propertyName);
            object v;
            try
            {
                v = Convert.ChangeType(value, me.Type);
                var be = BinaryExpressionBuilder[@operator](me, v);
                return Expression.Lambda<Func<T, bool>>(be, parameterExpression);
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