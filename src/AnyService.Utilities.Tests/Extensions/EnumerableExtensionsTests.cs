using Xunit;
using Shouldly;
using System.Collections.Generic;

namespace AnyService.Utilities.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void IsNullOrEmpty_ReturnsTrue()
        {
            (null as IEnumerable<string>).IsNullOrEmpty().ShouldBeTrue();
            (new string[] { }).IsNullOrEmpty().ShouldBeTrue();
        }
        [Fact]
        public void IsNullOrEmpty_ReturnsFalse()
        {
            (new[] { 1, 2, 3 }).IsNullOrEmpty().ShouldBeFalse();
        }
    }
}