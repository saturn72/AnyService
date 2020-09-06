using AnyService.Services;
using AnyService.Services.Preparars;
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
                () => c.DefaultPaginationSettings.DefaultOffset.ShouldBe(1),
                () => c.DefaultPaginationSettings.DefaultPageSize.ShouldBe(50),
                () => c.DefaultPaginationSettings.DefaultSortOrder.ShouldBe("asc")
                );
            c.FilterFactoryType.ShouldBeOfType<DefaultFilterFactory>();
            c.ModelPrepararType.ShouldBeOfType(typeof(DummyModelPreparar<>));
            c.ServiceResponseMapperType.ShouldBeOfType<DataOnlyServiceResponseMapper>();

            c.AuditConfig.EntityNameResolver(typeof(string)).ShouldBe(typeof(string).FullName);
        }
    }
}