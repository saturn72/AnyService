using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
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
            c.DefaultPaginationSettings.ShouldSatisfyAllConditions(
                () => c.DefaultPaginationSettings.DefaultOffset.ShouldBe((ulong)1),
                () => c.DefaultPaginationSettings.DefaultPageSize.ShouldBe((ulong)50),
                () => c.DefaultPaginationSettings.DefaultSortOrder.ShouldBe("asc")
                );
            c.FilterFactoryType.ShouldBeOfType<DefaultFilterFactory>();
            c.ModelPrepararType.ShouldBeOfType(typeof(AudityModelPreparar<>));
            c.ServiceResponseMapperType.ShouldBeOfType<DataOnlyServiceResponseMapper>();
        }
    }
}