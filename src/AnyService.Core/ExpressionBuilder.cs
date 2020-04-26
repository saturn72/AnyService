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
        protected const string HasBrackets = @"^\s*(?'leftOperand'[^\|\&]*)\s*(?'evaluator_first'((\|)*|(\&)*))\s*(?'brackets'(\(\s*(.*)s*\)))\s*(?'evaluator_second'((\|{1,2})|(\&{1,2}))*)\s*(?'rightOperand'.*)\s*$";
        protected const string HasSurroundingBracketsOnly = @"^\s*\(\s*(?'leftOperand'([^\(\)])+)\s*\)\s*$";
        protected const string EvalPattern = @"^(?'leftOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\s*\S{1,})\s*(?'evaluator_first'((\|{1,2})|(\&{1,2})))\s*(?'rightOperand'.*)\s*$";
        private const string BinaryPatternCore = @"\s*(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)\s*";
        protected const string BinaryPattern = "^" + BinaryPatternCore + "$";
        protected const string BinaryWithBracketsPattern = @"^\s*\(" + BinaryPatternCore + @"\)\s*$";
        private const string LeftOperand = "leftOperand";
        private const string RightOperand = "rightOperand";
        private const string Brackets = "brackets";
        private const string EvaluatorFirst = "evaluator_first";
        private const string EvaluatorSecond = "evaluator_second";
        private const string Operator = "operator";

        public static Expression<Func<T, bool>> ToBinaryTreeExpression<T>(string query)
        {
            var pe = Expression.Parameter(typeof(T), "x");
            return ToBinaryTreeWorker<T>(query, pe);
        }
        private static Expression<Func<T, bool>> ToBinaryTreeWorker<T>(string query, ParameterExpression parameterExpression)
        {
            var q = query.Trim();
            var m = Regex.Match(q, HasSurroundingBracketsOnly);
            if (m.Success)
                return ToBinaryTreeWorker<T>(q.Substring(1, q.Length - 2), parameterExpression);

            var binaryOperationMatch = GetMatch(q, BinaryPattern, BinaryWithBracketsPattern);

            if (binaryOperationMatch != null && binaryOperationMatch.Success)
            {
                return ToBinaryExpression<T>(
                    binaryOperationMatch.Groups[LeftOperand].Value,
                    binaryOperationMatch.Groups[Operator].Value,
                    binaryOperationMatch.Groups[RightOperand].Value,
                    parameterExpression);
            }
            var hasBrackets = GetMatch(q, HasBrackets); //DO NOT CHANGE EXPRESSIONS ORDER!!!
            if (hasBrackets != null && hasBrackets.Success)
            {
                Group leftOp = hasBrackets.Groups[LeftOperand],
                    evaluatorFirst = hasBrackets.Groups[EvaluatorFirst],
                    brackets = hasBrackets.Groups[Brackets],
                    evaluatorSecond = hasBrackets.Groups[EvaluatorSecond],
                    rightOp = hasBrackets.Groups[RightOperand];

                if (leftOp.Value.HasValue() && rightOp.Value.HasValue() &&
                    evaluatorFirst.Value.HasValue() && evaluatorSecond.Value.HasValue() && brackets.Value.HasValue())
                {
                    var e = evaluatorSecond.Value;
                    var firstEvaluatorIndex = q.IndexOf(e) + e.Length;
                    return SendToEvaluation<T>(leftOp.Value, e, q.Substring(firstEvaluatorIndex), parameterExpression);
                }

                string leftQuery = GetValueOrReplaceIfEmptyOrNull(leftOp.Value, brackets.Value),
                    evaluator = GetValueOrReplaceIfEmptyOrNull(evaluatorFirst.Value, evaluatorSecond.Value),
                    rightQuery = GetValueOrReplaceIfEmptyOrNull(rightOp.Value, brackets.Value);
                return SendToEvaluation<T>(leftQuery, evaluator, rightQuery, parameterExpression);
            }

            var evalMatch = Regex.Match(q, EvalPattern);
            if (evalMatch.Success)
                return SendToEvaluation<T>(evalMatch.Groups[LeftOperand].Value, evalMatch.Groups[EvaluatorFirst].Value, evalMatch.Groups[RightOperand].Value, parameterExpression);

            return null;

            string GetValueOrReplaceIfEmptyOrNull(string source, string onEmptyOrNullValue) => source.HasValue() ? source : onEmptyOrNullValue;
        }

        private static Match GetMatch(string q, params string[] patterns)
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                var m = Regex.Match(q, patterns[i]);
                if (m.Success)
                    return m;
            }
            return null;
        }

        private static Expression<Func<T, bool>> SendToEvaluation<T>(string leftQuery, string evaluator, string rightQuery, ParameterExpression parameterExpression)
        {
            var leftBinaryExpression = ToBinaryTreeWorker<T>(leftQuery, parameterExpression);
            var rightBinaryExpression = ToBinaryTreeWorker<T>(rightQuery, parameterExpression);
            if (leftBinaryExpression == null || rightBinaryExpression == null) return null;

            var builder = EvaluationExpressionBuilder[evaluator];
            var evaluation = builder(leftBinaryExpression.Body, rightBinaryExpression.Body);
            return Expression.Lambda<Func<T, bool>>(evaluation, leftBinaryExpression.Parameters);
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