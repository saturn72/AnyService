using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
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
                        StringValue = "32",
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
                        StringValue = "32",
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
                "id == 2 & numericValue ==32",
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
            new object[]
            {
                "id == 2 && numericValue ==32",
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
            new object[]
            {
                "id == 2 | numericValue ==32",
                new[]
                {
                    new TestClass {Id="2",  }, new TestClass { NumericValue = 32 },
                },
                new[]
                {
                    new TestClass {},
                },
            },
            new object[]
            {
                "id == 2 || numericValue ==32",
                new[]
                {
                    new TestClass {Id="2",  }, new TestClass { NumericValue = 32 },
                },
                new[]
                {
                    new TestClass {},
                },
            },
             new object[]
            {
                "id == 2 && numericValue ==32 && stringvalue=a",
                new[]
                {
                    new TestClass {Id="2", NumericValue = 32  , StringValue = "a"},
                },
                new[]
                {
                    new TestClass {},
                    new TestClass { Id="2", },
                    new TestClass { NumericValue = 32  },
                },
            },
             new object[]
            {
                "(id == 2 || stringValue ==a) && numericvalue <3",
                new[]
                {
                    new TestClass {Id="2",  StringValue = "ttt"},
                    new TestClass { StringValue = "a" },
                },
                new[]
                {
                    new TestClass {},
                    new TestClass {Id="2", NumericValue = 22},
                    new TestClass { StringValue = "a", NumericValue = 32 },
                },
            },
               new object[]
            {
                "id == 2 || (numericvalue ==32 && stringValue ==a)",
                new[]
                {
                    new TestClass {Id="2",  StringValue = "ttt"},
                    new TestClass {Id="2", NumericValue = 22},
                    new TestClass { StringValue = "a", NumericValue = 32 },
                },
                new[]
                {
                    new TestClass { StringValue = "a" },
                    new TestClass {},
                },
            },
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

        [Fact]
        public void AllRegExPatterns()
        {
            ExpressionBuilderForTest.StartsWithBracketValue.ShouldBe(@"^(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)$");
            ExpressionBuilderForTest.EndsWithBracketValue.ShouldBe(@"^(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)$");
            ExpressionBuilderForTest.BinaryPatternValue.ShouldBe(@"^(?'leftOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\s*\S{1,})\s*(?'evaluator'((\|{1,2})|(\&{1,2})))\s*(?'rightOperand'\S{1,}\s*(==|!=|<|<=|>|>=)\S{1,})\s*$");
            ExpressionBuilderForTest.EvalPatternValue.ShouldBe(@"^(?'leftOperand'\w+)\s*(?'operator'(==|!=|<|<=|>|>=))\s*(?'rightOperand'\w+)$");
        }

        [Theory]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "does_not_match | (right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "does_not_match || (right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "does_not_match & (right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "does_not_match && (right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "does_not_match | right")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "does_not_match || right")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "does_not_match & right")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "does_not_match && right")]
        public void PatternTests_DoesNotMatch(string pattern, string str)
        {
            Regex.Match(str, pattern).Success.ShouldBeFalse();
        }
        [Theory]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "|", "(right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "|", "right")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "||", "(right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "||", "right")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "&", "(right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "&", "right")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "&&", "(right)")]
        [InlineData(ExpressionBuilderForTest.StartsWithBracketValue, "(left)", "&&", "right")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "(left)", "|", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "left", "|", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "(left)", "||", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "left", "||", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "(left)", "&", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "left", "&", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "(left)", "&&", "(right)")]
        [InlineData(ExpressionBuilderForTest.EndsWithBracketValue, "left", "&&", "(right)")]

        public void PatternTests(string pattern, string left, string ev, string right)
        {
            var m = Regex.Match($"{left} {ev} {right}", pattern);
            m.Success.ShouldBeTrue();
            m.Groups["leftOperand"].Value.ShouldBe(left);
            m.Groups["evaluator"].Value.ShouldBe(ev);
            m.Groups["rightOperand"].Value.ShouldBe(right);
        }
        internal class ExpressionBuilderForTest : ExpressionBuilder
        {
            internal static Func<MemberExpression, object, Expression> GetBinaryExpressionBuilder(string key) => Core.ExpressionBuilder.BinaryExpressionBuilder[key];
            internal static Func<Expression, Expression, Expression> GetEvaluationExpressionBuilder(string key) => Core.ExpressionBuilder.EvaluationExpressionBuilder[key];
            internal const string EvalPatternValue = EvalPattern;
            internal const string BinaryPatternValue = BinaryPattern;
            internal const string StartsWithBracketValue = StartsWithBracketPattern;
            internal const string EndsWithBracketValue = EndsWithBracketPattern;
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
                new TestClass{Id = "1", NumericValue  =1, StringValue = "1"},
                new TestClass{Id = "2", NumericValue  =1, StringValue = "2"},
                new TestClass{Id = "3", NumericValue  =3},
            };

            var filter1 = new Dictionary<string, string> { { nameof(TestClass.NumericValue), "1" } };
            var f1 = ExpressionBuilder.ToBinaryTree<TestClass>(filter1);
            f1.ShouldNotBeNull();
            var res1 = col.Where(f1).ToArray();
            res1.Count().ShouldBe(2);

            var filter2 = new Dictionary<string, string> {
                {nameof(TestClass.StringValue), "1" } ,
                {nameof(TestClass.NumericValue), "1" } ,
                };
            var f2 = ExpressionBuilder.ToBinaryTree<TestClass>(filter2);
            f2.ShouldNotBeNull();
            var res2 = col.Where(f2).ToArray();
            res2.Count().ShouldBe(1);
        }
    }
}
