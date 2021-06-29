using AnyService.Audity;
using AnyService.Infrastructure;
using AnyService.Models;
using AnyService.Services.Audit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class TestClass1
    {
        public int Id { get; set; }
    }
    public class TestClass2
    {
        public string Id { get; set; }
    }

    public abstract class MappingTest
    {
        protected static Mock<IServiceProvider> ServiceProviderMock;
        static MappingTest()
        {
            var mf = new DefaultMapperFactory();
            var sc = new ServiceCollection();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IMapperFactory))).Returns(mf);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(sp.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            sp.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);
            ServiceProviderMock = sp;

            MappingExtensions.AddConfiguration(sc, "default", cfg =>
            {
                cfg.CreateMap<TestClass1, TestClass2>()
                    .ForMember(dest => dest.Id, opts => opts.MapFrom(src => src.Id.ToString()));
                cfg.CreateMap<AuditRecord, AuditRecordModel>();
                cfg.CreateMap<AuditPagination, AuditPaginationModel>();
            }, false);

            MappingExtensions.Build(sp.Object);
        }
    }
}