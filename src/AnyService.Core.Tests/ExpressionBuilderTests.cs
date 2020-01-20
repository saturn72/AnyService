using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests
{
    public class TestClass
    {
        public string Id { get; set; }
        public int Value { get; set; }
    }
    public class ExpressionBuilderTests
    {
        [Theory]
        [MemberData(Build_EmptyOrNullFilter_ReturnsNull_DATA)]
        public void Build_EmptyOrNullFilter_ReturnsNull()
        {
            ExpressionBuilder.Build<TestClass>(null).ShouldBeNull();
        }
        public static IEnumerable<object[]
        [Fact]
        public void Build_PropertyNotExists()
        {
            var filter = new Dictionary<string, string> { { "p", "d" } };
            ExpressionBuilder.Build<TestClass>(filter).ShouldBeNull();
        }
        [Fact]
        public void Build_BuildsExpression()
        {
            var filter = new Dictionary<string, string> { { nameof(TestClass.Value), "d" } };
            ExpressionBuilder.Build<TestClass>(filter).ShouldBeNull();
        }
    }
}
