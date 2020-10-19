using AnyService.Internals;
using AnyService.Mapping;
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
        static MappingTest()
        {
            var mf = new DefaultMapperFactory();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IMapperFactory))).Returns(mf);
            MappingExtensions.Configure(
                "default",
                cfg =>
            {
                cfg.CreateMap<TestClass1, TestClass2>()
                    .ForMember(dest => dest.Id, opts => opts.MapFrom(src => src.Id.ToString()));
            });

            MappingExtensions.Build(sp.Object, "default");
        }
    }
}