using Xunit;
using Shouldly;
using System;
using AnyService.Services;

namespace AnyService.Tests
{
    public class PaginationSettingTests
    {
        [Fact]
        public void Ctor()
        {
            new PaginationSettings().DefaultOrderBy.ShouldBe(nameof(IDomainObject.Id));
            var so = "sort-order";
            new PaginationSettings
            {
                DefaultOrderBy = so
            }.DefaultOrderBy.ShouldBe(so);

            new PaginationSettings
            {
                DefaultOrderBy = "  " + so + "   "
            }.DefaultOrderBy.ShouldBe(so);
        }
        [Fact]
        public void SortOrderOptions()
        {
            PaginationSettings.Asc.ShouldBe("asc");
            PaginationSettings.Desc.ShouldBe("desc");
        }

        [Fact]
        public void ThrowsOnWrongSortOrder()
        {
            Should.Throw<InvalidOperationException>(() => new PaginationSettings
            {
                DefaultSortOrder = "some-string"
            });
        }
    }
}