using Xunit;
using System;
using Shouldly;
using System.Linq;

namespace AnyService.Utilities.Tests
{
    public class TestClass
    {
        public int Value { get; set; }
    }
    public class T1 { }
    public class T2:T1 { }
    public class T3:T2 { }
    public class ObjectExtensionsTests
    {
        #region GetAllBaseTypes
        [Fact]
        public void GetAllBaseTypes_GetEntireTree()
        {
            var bts = typeof(T3).GetAllBaseTypes();
            bts.Count().ShouldBe(3);
            bts.ShouldContain(typeof(T2));
            bts.ShouldContain(typeof(T1));
            bts.ShouldContain(typeof(object));
        }
        [Fact]
        public void GetAllBaseTypes_ExcludeFromT1()
        {
            var bts = typeof(T3).GetAllBaseTypes(typeof(T1));
            bts.Count().ShouldBe(1);
            bts.ShouldContain(typeof(T2));
        }
        #endregion

        #region IsOfType

        [Fact]
        public void IsOfType_ReturnsTrue()
        {
            ObjectExtensionsFunctions.IsOfType<T1>(typeof(T2)).ShouldBeTrue();
            ObjectExtensionsFunctions.IsOfType(typeof(T2), typeof(T1)).ShouldBeTrue();
        }
        [Fact]
        public void IsOfType_ReturnsFalse()
        {
            ObjectExtensionsFunctions.IsOfType<string>(typeof(T1)).ShouldBeFalse();
            ObjectExtensionsFunctions.IsOfType(typeof(T1), typeof(string)).ShouldBeFalse();
        }
        #endregion
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

        #region ToJsonString
        [Fact]
        public void  ToJsonString()
        {
            var exp = "{\"value\":123}";
            var t = new TestClass
            {
                Value = 123
            };
            t.ToJsonString().ShouldBe(exp);
        }
        #endregion

        [Fact]
        public void DeepClone()
        {
            var src = new MyTestClass
            {
                StringValue = "some string",
                TestClass = new MyTestClass
                {
                    StringValue = "inneer string",
                }
            };

            var dest = src.DeepClone();
            dest.StringValue.ShouldBe(src.StringValue);
            dest.TestClass.StringValue.ShouldBe(src.TestClass.StringValue);
            dest.TestClass.GetHashCode().ShouldNotBe(src.TestClass.GetHashCode());
        }
    }
    public class MyTestClass
    {
        public string StringValue { get; set; }
        public MyTestClass TestClass { get; set; }
    }
}