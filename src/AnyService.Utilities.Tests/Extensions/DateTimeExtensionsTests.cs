using System;
using Shouldly;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void ToIso8601()
        {
            var dt = DateTime.UtcNow;
            dt.ToIso8601().ShouldBe(dt.ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
    }
}