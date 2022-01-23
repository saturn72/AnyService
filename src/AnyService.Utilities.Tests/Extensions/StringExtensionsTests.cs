using Xunit;
using System;
using Shouldly;

namespace AnyService.Utilities.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Fact]
        public void ToDelimitedString_ReturnsNullOnNullCollection()
        {
            string[] list = null;
            list.ToDelimitedString().ShouldBeNull();
        }
        [Fact]
        public void ToDelimitedString_ReturnsEmptyStringOnEmptyCollection()
        {
            var list = Array.Empty<string>();
            list.ToDelimitedString().ShouldBe(string.Empty);
        }
        [Fact]
        public void ToDelimitedString_SingleItemArray()
        {
            var list = new[] { "a" };
            list.ToDelimitedString(null).ShouldBe("a");
        }
        [Fact]
        public void ToDelimitedString_NullDelimiter()
        {
            var list = new[] { "a", "b", "c", "d" };
            list.ToDelimitedString(null).ShouldBe("abcd");
        }
        [Fact]
        public void ToDelimitedString_EmptyDelimiter()
        {
            var list = new[] { "a", "b", "c", "d" };
            list.ToDelimitedString().ShouldBe("abcd");
        }
        [Theory]
        [InlineData(" ")]
        [InlineData(" x ")]
        public void ToDelimitedString_NotNullDelimiter(string delimiter)
        {
            var list = new[] { "a", "b", "c", "d" };
            list.ToDelimitedString(delimiter).ShouldBe("a" + delimiter + "b" + delimiter + "c" + delimiter + "d");
        }
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