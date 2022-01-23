using Xunit;
using System;
using Shouldly;
using System.Linq;

namespace AnyService.Utilities.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void FromDelimitedString_ReturnsSameOnEmptyString(string str)
        {
            str.FromDelimitedString("dd").ShouldBe(Array.Empty<string>());
        }
        [Fact]
        public void FromDelimitedString_ReturnsNullOnNullSource()
        {
            (null as string).FromDelimitedString("d").ShouldBeNull();
        }
        [Theory]
        [InlineData("a", "a", "ssssss")]
        [InlineData("abcd", "abcd", "")]
        public void FromDelimitedString_SingleItemArray(string input, string output, string d)
        {
            var o = input.FromDelimitedString(d);
            o.Count().ShouldBe(1);
            o.First().ShouldBe(output);
        }
        [Fact]
        public void FromDelimitedString_NotNullDelimiter()
        {
            var s = "a x b x c x d ";
            var exp = new[] { "a", "b", "c", "d" };
            var o = s.FromDelimitedString(" x ");
            o.Count().ShouldBe(4);
            for (var i = 0; i < o.Count(); i++)
                exp[i].ShouldBe(o.ElementAt(i));
        }

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