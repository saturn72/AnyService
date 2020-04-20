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
        public int NumericValue { get; set; }
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
        [MemberData(nameof(ToBinaryTree_FromString_DATA))]
        public void ToBinaryTree_FromString(string query, IEnumerable<TestClass> willReturned, IEnumerable<TestClass> willNotReturned)
        {
            var func = ExpressionBuilder.ToBinaryTree<TestClass>(query);

            var col = new List<TestClass>(willReturned);
            col.AddRange(willNotReturned);

            var result = col.Where(func);
            result.Count().ShouldBe(willReturned.Count());
            willReturned.All(x => result.Contains(x));
        }

        public static IEnumerable<object[]> ToBinaryTree_FromString_DATA => new[]
        {
            new object[]
            {
                "id == 2",
                new[]{new TestClass{ Id = "2"}},
                new[]
                {
                    new TestClass
                    {
                        Id = "should-never-returned-#1",
                        NumericValue = 2,
                    },
                    new TestClass
                    {
                        Id = "should-never-returned-#2",
                        Value1 = "32",
                    }
                }
            },
            new object[]
            {
                "NumericValue > 2",
                new[]
                {
                    new TestClass { NumericValue = 3 }, new TestClass { NumericValue = 4 },
                },
                new[]
                {
                    new TestClass
                    {
                        Id = "should-never-returned-#1",
                        NumericValue = 2,
                    },
                    new TestClass
                    {
                        Id = "should-never-returned-#2",
                        Value1 = "32",
                    }
                }
            },
            new object[]
            {
                "NumericValue >= 3",
                new[]
                {
                    new TestClass { NumericValue = 3 }, new TestClass { NumericValue = 4 },
                },
                new[]
                {
                    new TestClass
                    {
                        Id = "should-never-returned-#1",
                        NumericValue = 2,
                    },
                },
            },
            new object[]
            {
                "NumericValue < 5",
                new[]
                {
                    new TestClass { NumericValue = 3 }, new TestClass { NumericValue = 4 },
                },
               new TestClass[] { },
            },
            new object[]
            {
                "NumericValue <= 4",
                new[]
                {
                    new TestClass { NumericValue = 3 }, new TestClass { NumericValue = 4 },
                },
                new TestClass[] { },
            },
            new object[]
            {
                "id == 2 && value1 ==32",
                new[]
                {
                    new TestClass {Id="2", NumericValue = 32  }, new TestClass { Id="2", NumericValue = 32 },
                },
                new[]
                {
                    new TestClass {},
                    new TestClass { Id="2", },
                    new TestClass { NumericValue = 32  },
                },
            },
        // [InlineData("")]
        // [InlineData("id == 2 | value1 ==32")]
        // [InlineData("id == 2 || value1 ==32")]
        // [InlineData("(id == 2 || value1 ==32) && value2 <123")]
        // [InlineData("id == 2 || (value1 ==32 && value2 <123)")]
        };

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
            internal static Func<MemberExpression, object, Expression> GetBinaryExpressionBuilder(string key) => Core.ExpressionBuilder.BinaryExpressionBuilder[key];
            // internal static Func<ParameterExpression, object, Expression> GetBinaryExpressionBuilder(string key) => Core.ExpressionBuilder.BinaryExpressionBuilder[key];
            internal static Func<Expression, Expression, Expression> GetEvaluationExpressionBuilder(string key) => Core.ExpressionBuilder.EvaluationExpressionBuilder[key];
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
            new object[]{new Dictionary<string, string> { { nameof(TestClass.NumericValue), "d" } }}
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
                new TestClass{Id = "1", NumericValue  =1, Value1 = "1"},
                new TestClass{Id = "2", NumericValue  =1, Value1 = "2"},
                new TestClass{Id = "3", NumericValue  =3},
            };

            var filter1 = new Dictionary<string, string> { { nameof(TestClass.NumericValue), "1" } };
            var f1 = ExpressionBuilder.ToBinaryTree<TestClass>(filter1);
            f1.ShouldNotBeNull();
            var res1 = col.Where(f1).ToArray();
            res1.Count().ShouldBe(2);

            var filter2 = new Dictionary<string, string> {
                {nameof(TestClass.Value1), "1" } ,
                {nameof(TestClass.NumericValue), "1" } ,
                };
            var f2 = ExpressionBuilder.ToBinaryTree<TestClass>(filter2);
            f2.ShouldNotBeNull();
            var res2 = col.Where(f2).ToArray();
            res2.Count().ShouldBe(1);
        }
    }
}
