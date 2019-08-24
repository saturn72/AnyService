using System;
using Xunit;
using Shouldly;

namespace AnyService.Tests.ObjectExtensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("")]
        public void HasValue_returnsFalse(string src)
        {
            src.HasValue().ShouldBeFalse();
        }

        [Theory]
        [InlineData("ddd")]
        [InlineData("   ddd")]
        [InlineData("ddd   ")]
        public void HasValue_ReturnsTrue(string src)
        {
            src.HasValue().ShouldBeTrue();
        }
    }
}
