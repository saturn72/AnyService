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
            MappingExtensions.Configure(cfg =>
            {
                cfg.CreateMap<TestClass1, TestClass2>()
                    .ForMember(dest => dest.Id, opts => opts.MapFrom(src => src.Id.ToString()));
            });
        }
    }
}