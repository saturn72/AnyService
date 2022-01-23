using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void IsOpenGenericType_ReturnsFalse()
        {
            typeof(string).IsOfOpenGenericType(typeof(List<>)).ShouldBeFalse();
            typeof(string).IsOfOpenGenericType(typeof(IList<>)).ShouldBeFalse();
            typeof(string[]).IsOfOpenGenericType(typeof(List<>)).ShouldBeFalse();
            typeof(string[]).IsOfOpenGenericType(typeof(IEnumerable<>)).ShouldBeFalse();
        }
        [Fact]
        public void IsOpenGenericType_ReturnsTrue()
        {
            typeof(List<string>).IsOfOpenGenericType(typeof(IList<>)).ShouldBeTrue();
            typeof(List<string>).IsOfOpenGenericType(typeof(ICollection<>)).ShouldBeTrue();
            typeof(List<string>).IsOfOpenGenericType(typeof(IEnumerable<>)).ShouldBeTrue();
            typeof(List<string>).IsOfOpenGenericType(typeof(List<>)).ShouldBeTrue();
            typeof(List<string[]>).IsOfOpenGenericType(typeof(List<>)).ShouldBeTrue();
            typeof(List<string[]>).IsOfOpenGenericType(typeof(IList<>)).ShouldBeTrue();
        }
    }
}
