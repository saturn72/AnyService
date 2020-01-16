using Xunit;
using System;
using Shouldly;

namespace AnyService.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void StringExtensions_HasValue_ReturnsFalse(string source)
        {
            source.HasValue().ShouldBeFalse();
        }

        [Fact]
        public void StringExtensions_HasValue_ReturnsTrue()
        {
            "test_string".HasValue().ShouldBeTrue();
        }
        private const string Name = "roi";
        private const string ValueAsString = "4";
        private const string Json = "{\"value\":" + ValueAsString + ", \"name\":\"" + Name + "\"}";
        private static readonly int Value = int.Parse(ValueAsString);
        [Fact]
        public void ToObject_FromString()
        {
            var o1 = Json.ToObject<TestClass>();
            o1.Name.ShouldBe(Name);
            o1.Value.ShouldBe(Value);

            var t = Json.ToObject(typeof(TestClass));
            var o2 = t.ShouldBeOfType<TestClass>();
            o2.Name.ShouldBe(Name);
            o2.Value.ShouldBe(Value);
        }
        public class TestClass
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }
    }
}