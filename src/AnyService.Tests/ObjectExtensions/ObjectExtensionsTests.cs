using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace AnyService.ObjectExtensions
{
    public class ObjectExtensionsTests
    {

        [Fact]
        public void GetPropertyValueByName_ThrowsOnNotExists()
        {
            var tc = new MyTestClass
            {
                StringValue = "CCCEEE",
                TestClass = new MyTestClass
                { StringValue = "internal value" }
            };

            Should.Throw<InvalidOperationException>(() => ObjectExtensionsFunctions.GetPropertyValueByName<string>(tc, "VVV"));
            Should.Throw<InvalidOperationException>(() => ObjectExtensionsFunctions.GetPropertyValueByName<MyTestClass>(tc, "testClass"));
        }

        [Fact]
        public void GetPropertyValueByName()
        {
            var tc = new MyTestClass
            {
                StringValue = "CCCEEE",
                TestClass = new MyTestClass
                { StringValue = "internal value" }
            };

            ObjectExtensionsFunctions.GetPropertyValueByName<string>(tc, "StringValue").ShouldBe(tc.StringValue);
            ObjectExtensionsFunctions.GetPropertyValueByName<MyTestClass>(tc, "TestClass").ShouldBe(tc.TestClass);
        }

        [Fact]
        public void Clone_SimpleObject()
        {
            var o = new MyTestClass
            {
                StringValue = "some-value",
                TestClass = new MyTestClass
                {
                    StringValue = "internal - string-data"
                },
            };

            var c = o.Clone();
            c.StringValue.ShouldBe(o.StringValue);
            c.TestClass.StringValue.ShouldBe(o.TestClass.StringValue);
            (c.GetHashCode() == o.GetHashCode()).ShouldBeFalse();
        }
        [Fact]
        public void Clone_List()
        {
            var l = new List<string> { "a", "b" };
            var cl = l.Clone();
            cl.Count().ShouldBe(2);
            foreach (var item in l)
                cl.ShouldContain(item);
            (cl.GetHashCode() == l.GetHashCode()).ShouldBeFalse();
        }
    }
    public class MyTestClass
    {
        public string StringValue { get; set; }
        public MyTestClass TestClass { get; set; }
    }
}