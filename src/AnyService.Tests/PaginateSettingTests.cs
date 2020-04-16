using Xunit;
using Shouldly;
using System;
using AnyService.Services;

namespace AnyService.Tests
{
    public class PaginateSettingTests
    {
        [Fact]
        public void SortOrderOptions()
        {
            PaginateSettings.Asc.ShouldBe("asc");
            PaginateSettings.Desc.ShouldBe("desc");
        }

        [Fact]
        public void ThrowsOnWronSortOrder()
        {
            Should.Throw<InvalidOperationException>(() => new PaginateSettings
            {
                DefaultSortOrder = "some-string"
            });
        }
    }
}