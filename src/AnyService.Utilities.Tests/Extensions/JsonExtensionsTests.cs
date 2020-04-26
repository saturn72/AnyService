using Shouldly;
using System.Text.Json;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class JsonExtensionsTests
    {
        private const string Name = "roi";
        private const string ValueAsString = "4";
        private const string Json = "{\"value\":" + ValueAsString + ", \"name\":\"" + Name + "\"}";
        private static readonly int Value = int.Parse(ValueAsString);

        [Fact]
        public void ToObject_FromJsonElement()
        {
            var je = JsonDocument.Parse(Json).RootElement;
            var o1 = je.ToObject<TestClass>();
            o1.Name.ShouldBe(Name);
            o1.Value.ShouldBe(Value);

            var t = je.ToObject(typeof(TestClass));
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
