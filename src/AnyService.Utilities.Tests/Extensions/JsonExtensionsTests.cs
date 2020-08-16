using Shouldly;
using System;
using System.Reflection.Metadata;
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
        [Fact]
        public void GetValue_SpecificTypes()
        {
            var expDateTime = DateTime.UtcNow;
            var expDateTimeOffset = DateTimeOffset.UtcNow;
            var expGuid = new Guid("72e791db-5223-4e06-9caf-39fd4b593de4");
            var o = new
            {
                DateTime = expDateTime,
                DateTimeOffset = expDateTimeOffset,
                Guid = "72e791db-5223-4e06-9caf-39fd4b593de4",
            };

            var jsonstring = JsonSerializer.Serialize(o);
            using var jDoc = JsonDocument.Parse(jsonstring);
            var root = jDoc.RootElement;
            root.GetValue(expDateTime.GetType(), "DateTime").ShouldBe(expDateTime);
            root.GetValue(expDateTimeOffset.GetType(), "DateTimeOffset").ShouldBe(expDateTimeOffset);
            root.GetValue(expGuid.GetType(), "Guid").ShouldBe(expGuid);
        }

        [Theory]
        [InlineData(typeof(bool), "bo", true)]
        [InlineData(typeof(byte), "by", 1)]
        [InlineData(typeof(decimal), "Decimal", 123)]
        [InlineData(typeof(double), "Double", 123)]
        [InlineData(typeof(short), "Int16", -1)]
        [InlineData(typeof(int), "Int32", -1)]
        [InlineData(typeof(long), "Int64", -1)]
        [InlineData(typeof(sbyte), "SByte", 2)]
        [InlineData(typeof(string), "String", "abcd")]
        [InlineData(typeof(ushort), "UInt16", 1)]
        [InlineData(typeof(uint), "UInt32", 1)]
        [InlineData(typeof(ulong), "UInt64", 1)]
        public void GetJsonValue(Type type, string propertyName, object expValue)
        {
            var o = new
            {
                bo = true,
                by = 1,
                DateTime = default(DateTime),
                DateTimeOffset = default(DateTimeOffset),
                Decimal = 123,
                Double = 123,
                Guid = "72e791db-5223-4e06-9caf-39fd4b593de4",
                Int16 = -1,
                Int32 = -1,
                Int64 = -1,
                SByte = 2,
                String = "abcd",
                UInt16 = 1,
                UInt32 = 1,
                UInt64 = 1,
            };
            var jsonstring = JsonSerializer.Serialize(o);
            using var jDoc = JsonDocument.Parse(jsonstring);
            var root = jDoc.RootElement;
            root.GetValue(type, propertyName).ShouldBe(expValue);
        }

        #region FirstElementOrDefault
        [Fact]
        public void FirstElementOrDefault_ReturnsDefault()
        {
            var json = "{\"values\":[]}";
            var o = JsonSerializer.Deserialize<JsonElement>(json);
            o.GetProperty("values").FirstElementOrDefault(x => x.TryGetProperty("data", out JsonElement je)).ShouldBe(default);
        }
        [Fact]
        public void FirstElementOrDefault_ReturnsJsonelement()
        {
            var json = "{\"values\":[{\"key\":\"key1\", \"value\":\"val1\"}]}";
            var o = JsonSerializer.Deserialize<JsonElement>(json);
            var je = o.GetProperty("values").FirstElementOrDefault(x => x.GetProperty("key").GetString() == "key1");
            je.GetProperty("value").GetString().ShouldBe("val1");
        }
        #endregion
        public class TestClass
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }
    }
}
