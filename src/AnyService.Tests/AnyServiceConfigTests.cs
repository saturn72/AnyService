using Shouldly;

namespace AnyService.Tests
{
    public sealed class AnyServiceConfigTests
    {
        public void Ctor()
        {
            var c = new AnyServiceConfig();
            c.MaxMultipartBoundaryLength.ShouldBe(50);
            c.MaxValueCount.ShouldBe(25);
            c.ManageEntityPermissions.ShouldBeTrue();
            c.UseAuthorizationMiddleware.ShouldBeTrue();
            c.UseExceptionLogging.ShouldBeTrue();
            c.DefaultPaginateSettings.ShouldSatisfyAllConditions(
                () => c.DefaultPaginateSettings.DefaultOffset.ShouldBe((ulong)1),
                () => c.DefaultPaginateSettings.DefaultPageSize.ShouldBe((ulong)50),
                () => c.DefaultPaginateSettings.DefaultSortOrder.ShouldBe("asc")
                );
        }
    }
}