using Xunit;
using System;
using Shouldly;
using System.Linq;
using System.Collections.Generic;

namespace AnyService.Utilities.Tests
{
    public class TestClass
    {
        public int Value { get; set; }
    }
    public class T1 { }
    public class T2 : T1 { }
    public class T3 : T2 { }
    public class ObjectExtensionsTests
    {
        #region ToDynamicObject
        public class TClass
        {
            public int Num1 { get; set; }
            public int Num2 { get; set; }
            public int Num3 { get; set; }
            public int Num4 { get; set; }
        }
        [Fact]
        public void ToDynamicObject_Projects_caseInsensitive()
        {
            var tc = new TClass
            {
                Num1 = 1,
                Num2 = 2,
                Num3 = 3,
                Num4 = 4,
            };
            IDictionary<string, object> d = tc.ToDynamic(new[] { nameof(TClass.Num1).ToLower(), nameof(TClass.Num4) }, ignoreCase: true);
            d["num1"].ShouldBe(tc.Num1);
            d["Num4"].ShouldBe(tc.Num4);

            Should.Throw<KeyNotFoundException>(() => d["Num1"]);
            Should.Throw<KeyNotFoundException>(() => d["Num2"]);
            Should.Throw<KeyNotFoundException>(() => d["Num3"]);
        }
        [Fact]
        public void ToDynamicObject_Projects_caseSensitive()
        {
            var tc = new TClass
            {
                Num1 = 1,
                Num2 = 2,
                Num3 = 3,
                Num4 = 4,
            };
            IDictionary<string, object> d = tc.ToDynamic(new[] { nameof(TClass.Num1).ToLower(), nameof(TClass.Num4) }, ignoreCase: false);
            d["Num4"].ShouldBe(tc.Num4);

            Should.Throw<KeyNotFoundException>(() => d["num1"]);
            Should.Throw<KeyNotFoundException>(() => d["Num1"]);
            Should.Throw<KeyNotFoundException>(() => d["Num2"]);
            Should.Throw<KeyNotFoundException>(() => d["Num3"]);
        }
        #endregion
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
        #region GetPropertyInfo
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
        #endregion
        #region GetPropertyValueByName
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
        #endregion
        #region ToArratytring
        [Fact]
        public void ToJsonArrayString()
        {
            var b = new byte[] { 1, 2, 3, 4, 5 };
            var r = b.ToJsonArrayString();
            r.ShouldBe("[1, 2, 3, 4, 5]");
        }
        #endregion
        #region ToJsonString
        [Fact]
        public void ToJsonString()
        {
            var exp = "{\"value\":123}";
            var t = new TestClass
            {
                Value = 123
            };
            t.ToJsonString().ShouldBe(exp);
        }
        #endregion
        #region DeepClone
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
        #endregion
    }
    public class MyTestClass
    {
        public string StringValue { get; set; }
        public MyTestClass TestClass { get; set; }
    }
}