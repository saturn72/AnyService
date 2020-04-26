using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests
{
    public class TestClass
    {
        public string Id { get; set; }
        public string StringValue { get; set; }
        public int NumericValue { get; set; }
    }
    public class ExpressionTreeBuilderTests
    {
        [Theory]
        [InlineData("idsfdsadffffdsfsdf2")] //no equality
        [InlineData("id 2")] // no equality
        [InlineData("id == == 2")] // duplicate equlity
        [InlineData("id == != 2")] // duplicate equlity
        [InlineData("id == <= 2")] // duplicate equlity
        [InlineData("id == < 2")] // duplicate equlity
        [InlineData("id == >= 2")] // duplicate equlity
        [InlineData("id == > 2")] // duplicate equlity
        [InlineData("id != > 2")] // duplicate equlity
        [InlineData("id == 2  value1 ==32")]// missing evaluation
        [InlineData("(id == 2 || value1 ==32) value2 <123")] // missing evaluation
        [InlineData("(id == 2  value1 ==32)")] // missing evaluation
        [InlineData("(id == 2 || value1 ==32) value2 <123 || claue3 = 9898")] // missing evaluation
        [InlineData("id == 2 (value1 ==32 && value2 <123)")] // missing evaluation
        public void ToBinaryTree_FromString_ReturnsNull(string query)
        {
            ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(query).ShouldBeNull();
        }

        [Theory]
        [InlineData("id == 2", "x => (x.Id == \"2\")")]
        [InlineData("(id == 2)", "x => (x.Id == \"2\")")]
        [InlineData("NumericValue > 2", "x => (x.NumericValue > 2)")]
        [InlineData("NumericValue >= 3", "x => (x.NumericValue >= 3)")]
        [InlineData("NumericValue < 3", "x => (x.NumericValue < 3)")]
        [InlineData("NumericValue <= 3", "x => (x.NumericValue <= 3)")]
        [InlineData("id == 2 & numericValue ==32", "x => ((x.Id == \"2\") And (x.NumericValue == 32))")]
        [InlineData("id == 2 && numericValue ==32", "x => ((x.Id == \"2\") AndAlso (x.NumericValue == 32))")]
        [InlineData("id == 2 && numericValue ==32 & stringValue==a", "x => ((x.Id == \"2\") AndAlso ((x.NumericValue == 32) And (x.StringValue == \"a\")))")]
        [InlineData("id == 2 & numericValue ==32 & stringValue==a", "x => ((x.Id == \"2\") And ((x.NumericValue == 32) And (x.StringValue == \"a\")))")]
        [InlineData("id == 2 && numericValue ==32 && stringValue==a", "x => ((x.Id == \"2\") AndAlso ((x.NumericValue == 32) AndAlso (x.StringValue == \"a\")))")]
        [InlineData("id == 2 | numericValue ==32", "x => ((x.Id == \"2\") Or (x.NumericValue == 32))")]
        [InlineData("id == 2 || numericValue ==32", "x => ((x.Id == \"2\") OrElse (x.NumericValue == 32))")]
        [InlineData("id == 2 || numericValue ==32 || stringValue==a", "x => ((x.Id == \"2\") OrElse ((x.NumericValue == 32) OrElse (x.StringValue == \"a\")))")]
        [InlineData("id == 2 || numericValue ==32 |  stringValue==a", "x => ((x.Id == \"2\") OrElse ((x.NumericValue == 32) Or (x.StringValue == \"a\")))")]
        [InlineData("(id == 2 || numericvalue <3 && stringValue ==a)", "x => ((x.Id == \"2\") OrElse ((x.NumericValue < 3) AndAlso (x.StringValue == \"a\")))")]
        [InlineData("(id == 2 || numericvalue <3) && stringValue ==a", "x => (((x.Id == \"2\") OrElse (x.NumericValue < 3)) AndAlso (x.StringValue == \"a\"))")]
        [InlineData("id == 2 || (numericvalue ==32 && stringValue ==a)", "x => ((x.Id == \"2\") OrElse ((x.NumericValue == 32) AndAlso (x.StringValue == \"a\")))")]
        [InlineData("id == 2 || (numericvalue ==32 && stringValue ==a) || stringValue ==b", "x => ((x.Id == \"2\") OrElse (((x.NumericValue == 32) AndAlso (x.StringValue == \"a\")) OrElse (x.StringValue == \"b\")))")]
        public void ToBinaryTree_FromString(string query, string expResult)
        {
            var e = ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(query);
            e.ToString().ShouldBe(expResult);
        }
        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData(">")]
        [InlineData(">=")]
        [InlineData("<")]
        [InlineData("<=")]
        public void BinaryExpressionBuilder_Keys_returnBuilder(string key)
        {
            ExpressionBuilderForTest.GetBinaryExpressionBuilder(key).ShouldNotBeNull();
        }
        [Fact]
        public void BinaryExpressionBuilder_Throws()
        {
            Should.Throw<KeyNotFoundException>(() => ExpressionBuilderForTest.GetBinaryExpressionBuilder("not-exists-key"));
        }

        [Theory]
        [InlineData("&")]
        [InlineData("&&")]
        [InlineData("|")]
        [InlineData("||")]
        public void EvaluationExpressionBuilder_Keys_returnBuilder(string key)
        {
            ExpressionBuilderForTest.GetEvaluationExpressionBuilder(key).ShouldNotBeNull();
        }
        [Fact]
        public void EvaluationExpressionBuilder_Throws()
        {
            Should.Throw<KeyNotFoundException>(() => ExpressionBuilderForTest.GetEvaluationExpressionBuilder("not-exists-key"));
        }

        [Fact]
        public void AllRegExPatterns()
        {
            ExpressionBuilderForTest.HasBracketValue.ShouldBe(@"^\s*(?'leftOperand'[^\|\&]*)\s*(?'evaluator_first'((\|)*|(\&)*))\s*(?'brackets'(\(\s*(.*)s*\)))\s*(?'evaluator_second'((\|{1,2})|(\&{1,2}))*)\s*(?'rightOperand'.*)\s*$");
            ExpressionBuilderForTest.HasSurroundingBracketsOnlyValue.ShouldBe(@"^\s*\(\s*(?'leftOperand'([^\(\)])+)\s*\)\s*$");
            ExpressionBuilderForTest.EvalPatternValue.ShouldBe(@"^(?'leftOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\s*\S{1,})\s*(?'evaluator_first'((\|{1,2})|(\&{1,2})))\s*(?'rightOperand'.*)\s*$");
            ExpressionBuilderForTest.BinaryPatternValue.ShouldBe(@"^\s*(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)\s*$");
            ExpressionBuilderForTest.BinaryWithBracketsPatternValue.ShouldBe(@"^\s*\(\s*(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)\s*\)\s*$");
        }

        internal class ExpressionBuilderForTest : ExpressionTreeBuilder
        {
            internal static Func<MemberExpression, object, Expression> GetBinaryExpressionBuilder(string key) => Core.ExpressionTreeBuilder.BinaryExpressionBuilder[key];
            internal static Func<Expression, Expression, Expression> GetEvaluationExpressionBuilder(string key) => Core.ExpressionTreeBuilder.EvaluationExpressionBuilder[key];
            internal const string EvalPatternValue = EvalPattern;
            internal const string BinaryPatternValue = BinaryPattern;
            internal const string BinaryWithBracketsPatternValue = BinaryWithBracketsPattern;
            internal const string HasBracketValue = HasBrackets;
            internal const string HasSurroundingBracketsOnlyValue = HasSurroundingBracketsOnly;

        }
        [Theory]
        [MemberData(nameof(ToBinaryTree_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA))]
        public void ToBinaryTree_EmptyOrNullFilter_ReturnsNull(IDictionary<string, string> filter)
        {
            ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(filter).ShouldBeNull();
        }

        public static IEnumerable<object[]> ToBinaryTree_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA => new[]
        {
            new object[]{null as IDictionary<string, string>},
            new object[]{new Dictionary<string, string>()},
            new object[]{new Dictionary<string, string> { { nameof(TestClass.NumericValue), "d" } }}
        };

        [Fact]
        public void ToBinaryTree_PropertyNotExists()
        {
            var filter = new Dictionary<string, string> { { "p", "d" } };
            ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(filter).ShouldBeNull();
        }
        [Fact]
        public void ToBinaryTree_BuildsExpression()
        {
            var col = new[]
            {
                new TestClass{Id = "1", NumericValue  =1, StringValue = "1"},
                new TestClass{Id = "2", NumericValue  =1, StringValue = "2"},
                new TestClass{Id = "3", NumericValue  =3},
            };

            var filter1 = new Dictionary<string, string> { { nameof(TestClass.NumericValue), "1" } };
            var f1 = ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(filter1);
            f1.ShouldNotBeNull();
            var res1 = col.Where(f1).ToArray();
            res1.Count().ShouldBe(2);

            var filter2 = new Dictionary<string, string> {
                {nameof(TestClass.StringValue), "1" } ,
                {nameof(TestClass.NumericValue), "1" } ,
                };
            var f2 = ExpressionTreeBuilder.BuildBinaryTreeExpression<TestClass>(filter2);
            f2.ShouldNotBeNull();
            var res2 = col.Where(f2).ToArray();
            res2.Count().ShouldBe(1);
        }
    }
}
