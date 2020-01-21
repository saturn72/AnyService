using System.Collections.Generic;
using System.Linq;
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
        [MemberData(nameof(Build_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA))]
        public void Build_EmptyOrNullFilter_ReturnsNull(IDictionary<string, string> filter)
        {
            ExpressionBuilder.ToFunc<TestClass>(filter).ShouldBeNull();
        }
        public static IEnumerable<object[]> Build_EmptyOrNullOrIncorrectFilter_ReturnsNull_DATA => new[]
        {
            new object[]{null as IDictionary<string, string>},
            new object[]{new Dictionary<string, string>()},
            new object[]{new Dictionary<string, string> { { nameof(TestClass.Value2), "d" } }}
        };

        [Fact]
        public void Build_PropertyNotExists()
        {
            var filter = new Dictionary<string, string> { { "p", "d" } };
            ExpressionBuilder.ToFunc<TestClass>(filter).ShouldBeNull();
        }
        [Fact]
        public void Build_BuildsExpression()
        {
            var col = new[]
            {
                new TestClass{Id = "1", Value2  =1, Value1 = "1"},
                new TestClass{Id = "2", Value2  =1, Value1 = "2"},
                new TestClass{Id = "3", Value2  =3},
            };

            var filter1 = new Dictionary<string, string> { { nameof(TestClass.Value2), "1" } };
            var f1 = ExpressionBuilder.ToFunc<TestClass>(filter1);
            f1.ShouldNotBeNull();
            var res1 = col.Where(f1).ToArray();
            res1.Count().ShouldBe(2);

            var filter2 = new Dictionary<string, string> {
                {nameof(TestClass.Value1), "1" } ,
                {nameof(TestClass.Value2), "1" } ,
                };
            var f2 = ExpressionBuilder.ToFunc<TestClass>(filter2);
            f2.ShouldNotBeNull();
            var res2 = col.Where(f2).ToArray();
            res2.Count().ShouldBe(1);
        }
    }
}
