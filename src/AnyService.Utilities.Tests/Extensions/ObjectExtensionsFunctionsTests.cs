using Xunit;
using System;
using Shouldly;

namespace AnyService.Utilities.Tests
{
    public class TestClass
    {
        public int Value { get; set; }
    }
    public class ObjectExtensionsTests
    {
        [Fact]
        public void GetPropertyInfo_ReturnsNull()
        {
            typeof(TestClass).GetPropertyInfo("v").ShouldBeNull();
            typeof(TestClass).GetPropertyInfo("value").ShouldBeNull();
        }

        [Fact]
        public void GetPropertyInfo_ReturnsPropertyInfo()
        {
            typeof(TestClass).GetPropertyInfo("Value").ShouldNotBeNull();
        }

        /*
         public static PropertyInfo GetPropertyInfo(this object obj, string propertyName)
        {
            var type = obj.GetType();
            if (!PropertyInfos.TryGetValue(type, out IDictionary<string, PropertyInfo> curPropertyInfo))
            {
                curPropertyInfo = new Dictionary<string, PropertyInfo>();
                PropertyInfos[type] = curPropertyInfo;
            }
            if (!curPropertyInfo.TryGetValue(propertyName, out PropertyInfo pi))
            {
                pi = obj.GetType().GetProperty(propertyName);
                if (pi != null)
                    curPropertyInfo[propertyName] = pi;
                return pi;
            }
            return null;
        }
        */
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
        public void GetPropertyValueOrDefaultByName_ReturnsDefault()
        {
            var tc = new MyTestClass
            {
                StringValue = "CCCEEE",
                TestClass = new MyTestClass
                { StringValue = "internal value" }
            };

            ObjectExtensionsFunctions.GetPropertyValueOrDefaultByName<string>(tc, "VVV").ShouldBe(default(string));
            ObjectExtensionsFunctions.GetPropertyValueOrDefaultByName<MyTestClass>(tc, "testClass").ShouldBe(default(MyTestClass));
        }
        [Fact]
        public void GetPropertyValueOrDefaultByName()
        {
            var tc = new MyTestClass
            {
                StringValue = "CCCEEE",
                TestClass = new MyTestClass
                { StringValue = "internal value" }
            };

            ObjectExtensionsFunctions.GetPropertyValueOrDefaultByName<string>(tc, "StringValue").ShouldBe(tc.StringValue);
            ObjectExtensionsFunctions.GetPropertyValueOrDefaultByName<MyTestClass>(tc, "TestClass").ShouldBe(tc.TestClass);
        }
    }
    public class MyTestClass
    {
        public string StringValue { get; set; }
        public MyTestClass TestClass { get; set; }
    }
}