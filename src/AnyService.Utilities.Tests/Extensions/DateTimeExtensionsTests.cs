using System;
using Shouldly;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void DateTime_ToIso8601()
        {
            var dt = DateTime.UtcNow;
            dt.ToIso8601().ShouldBe(dt.ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
        [Fact]
        public void DateTimeOffset_ToIso8601()
        {
            DateTimeOffset dto = DateTime.UtcNow;
            dto.ToIso8601().ShouldBe(dto.ToString("o"));
        }
    }
}