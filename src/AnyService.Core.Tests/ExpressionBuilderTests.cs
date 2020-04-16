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
        public string Value1 { get; set; }
        public int Value2 { get; set; }
    }
    public class ExpressionBuilderTests
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
            ExpressionBuilder.ToBinaryTree<TestClass>(query).ShouldBeNull();
        }

        [Theory]
        // [InlineData("id == 2")]
        // [InlineData("id > 2")]
        // [InlineData("id >= 2")]
        // [InlineData("id < 2")]
        // [InlineData("id <= 2")]
        // [InlineData("id == 2 && value1 ==32")]
        [InlineData("id == 2 | value1 ==32")]
        // [InlineData("id == 2 || value1 ==32")]
        // [InlineData("(id == 2 || value1 ==32) && value2 <123")]
        // [InlineData("id == 2 || (value1 ==32 && value2 <123)")]
        public void ToBinaryTree_FromString(string query)
        {
            ExpressionBuilder.ToBinaryTree<TestClass>(query);
            throw new System.NotImplementedException();
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
        internal class ExpressionBuilderForTest : ExpressionBuilder
        {
            internal static Func<MemberExpression, object, BinaryExpression> GetBinaryExpressionBuilder(string key) => ExpressionBuilder.BinaryExpressionBuilder[key];
            internal static Func<BinaryExpression, BinaryExpression, BinaryExpression> GetEvaluationExpressionBuilder(string key) => ExpressionBuilder.EvaluationExpressionBuilder[key];
        }
        [Theory]
        [MemberData(nameof(ToBinaryTree_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA))]
        public void ToBinaryTree_EmptyOrNullFilter_ReturnsNull(IDictionary<string, string> filter)
        {
            ExpressionBuilder.ToBinaryTree<TestClass>(filter).ShouldBeNull();
        }

        public static IEnumerable<object[]> ToBinaryTree_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA => new[]
        {
            new object[]{null as IDictionary<string, string>},
            new object[]{new Dictionary<string, string>()},
            new object[]{new Dictionary<string, string> { { nameof(TestClass.Value2), "d" } }}
        };

        [Fact]
        public void ToBinaryTree_PropertyNotExists()
        {
            var filter = new Dictionary<string, string> { { "p", "d" } };
            ExpressionBuilder.ToBinaryTree<TestClass>(filter).ShouldBeNull();
        }
        [Fact]
        public void ToBinaryTree_BuildsExpression()
        {
            var col = new[]
            {
                new TestClass{Id = "1", Value2  =1, Value1 = "1"},
                new TestClass{Id = "2", Value2  =1, Value1 = "2"},
                new TestClass{Id = "3", Value2  =3},
            };

            var filter1 = new Dictionary<string, string> { { nameof(TestClass.Value2), "1" } };
            var f1 = ExpressionBuilder.ToBinaryTree<TestClass>(filter1);
            f1.ShouldNotBeNull();
            var res1 = col.Where(f1).ToArray();
            res1.Count().ShouldBe(2);

            var filter2 = new Dictionary<string, string> {
                {nameof(TestClass.Value1), "1" } ,
                {nameof(TestClass.Value2), "1" } ,
                };
            var f2 = ExpressionBuilder.ToBinaryTree<TestClass>(filter2);
            f2.ShouldNotBeNull();
            var res2 = col.Where(f2).ToArray();
            res2.Count().ShouldBe(1);
        }
    }
}
