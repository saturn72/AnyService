using AutoMapper;

namespace AnyService.Infrastructure
{
    public interface IMapperFactory
    {
        void AddMapper(string mapperName, IMapper mapper);
        IMapper GetMapper(string mapperName);
    }
}
